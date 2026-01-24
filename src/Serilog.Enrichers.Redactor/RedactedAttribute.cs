using System;

namespace Serilog.Enrichers.Redactor
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class RedactedAttribute : Attribute
    {

    }
}
