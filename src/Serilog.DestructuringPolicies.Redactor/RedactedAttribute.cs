using System;

namespace Serilog.DestructuringPolicies.Redactor
{
    /// <summary>
    /// Indicates that the value of a property or field should be treated as sensitive and redacted from log output.
    /// </summary>
    /// <remarks>Apply this attribute to properties or fields that contain confidential information, such as
    /// passwords or personal data, to prevent their values from being exposed in logging output.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RedactedAttribute : Attribute
    {

    }
}
