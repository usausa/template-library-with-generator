namespace Template.Library;

using System.Diagnostics;
using System.Globalization;

internal static partial class RuntimeTarget
{
    [CustomMethod]
    public static partial void DefaultMessage();

    [CustomMethod("Hello from attribute.")]
    public static partial void AttributeMessage();

    [CustomMethod("Hello from trace.", Output = CustomMethodOutput.Trace)]
    public static partial void TraceMessage();
}

internal static partial class RuntimeOuter
{
    internal partial record struct Inner
    {
        [CustomMethod("Hello from nested record struct.")]
        public static partial void Method();
    }
}

internal partial interface IRuntimeTarget<out T>
{
#pragma warning disable IDE0051
    T Value { get; }
#pragma warning restore IDE0051

    [CustomMethod("Hello from interface.")]
    public static partial void Method();
}

public sealed class RuntimeTests
{
    // ------------------------------------------------------------
    // Console
    // ------------------------------------------------------------

    [Fact]
    public void DefaultMessageIsBuildProperty()
    {
        // Arrange & Act
        var output = CaptureConsole(RuntimeTarget.DefaultMessage);

        // Assert
        Assert.Equal($"Hello from test project.{Environment.NewLine}", output);
    }

    [Fact]
    public void AttributeMessageIsWritten()
    {
        // Arrange & Act
        var output = CaptureConsole(RuntimeTarget.AttributeMessage);

        // Assert
        Assert.Equal($"Hello from attribute.{Environment.NewLine}", output);
    }

    [Fact]
    public void NestedRecordStructMethodIsCallable()
    {
        // Arrange & Act
        var output = CaptureConsole(RuntimeOuter.Inner.Method);

        // Assert
        Assert.Equal($"Hello from nested record struct.{Environment.NewLine}", output);
    }

    [Fact]
    public void InterfaceMethodIsCallable()
    {
        // Arrange & Act
        var output = CaptureConsole(IRuntimeTarget<string>.Method);

        // Assert
        Assert.Equal($"Hello from interface.{Environment.NewLine}", output);
    }

    // ------------------------------------------------------------
    // Trace
    // ------------------------------------------------------------

    [Fact]
    public void TraceOutputIsWrittenToListener()
    {
        // Arrange
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var listener = new TextWriterTraceListener(writer);
        Trace.Listeners.Add(listener);

        // Act
        try
        {
            RuntimeTarget.TraceMessage();
            listener.Flush();
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        // Assert
        Assert.Equal($"Hello from trace.{Environment.NewLine}", writer.ToString());
    }

    // ------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------

    [Fact]
    public void RegisteredMethodIsFoundByName()
    {
        // Arrange & Act
        var method = CustomMethodProvider.FindMethod("Template.Library.RuntimeTarget.AttributeMessage");

        // Assert
        Assert.NotNull(method);
        Assert.Equal($"Hello from attribute.{Environment.NewLine}", CaptureConsole(method));
    }

    [Fact]
    public void NestedTypeMethodIsFoundByName()
    {
        // Arrange & Act
        var method = CustomMethodProvider.FindMethod("Template.Library.RuntimeOuter.Inner.Method");

        // Assert
        Assert.NotNull(method);
        Assert.Equal($"Hello from nested record struct.{Environment.NewLine}", CaptureConsole(method));
    }

    [Fact]
    public void UnknownNameIsNotFound()
    {
        // Arrange & Act
        var method = CustomMethodProvider.FindMethod("Template.Library.RuntimeTarget.Unknown");

        // Assert
        Assert.Null(method);
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string CaptureConsole(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}
