namespace Template.Library;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CustomMethodAttribute : Attribute
{
    public string? Message { get; }

    public CustomMethodOutput Output { get; set; }

    public CustomMethodAttribute(string? message = null)
    {
        Message = message;
    }
}
