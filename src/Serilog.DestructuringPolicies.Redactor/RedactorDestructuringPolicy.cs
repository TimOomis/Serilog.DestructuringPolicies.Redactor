using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serilog.DestructuringPolicies.Redactor
{
    public class RedactorDestructuringPolicy : IDestructuringPolicy
    {
        private const string DefaultRedactedText = "[REDACTED]";
        private readonly string _redactedText;

        public RedactorDestructuringPolicy(string? redactedText = DefaultRedactedText)
        {
            _redactedText = redactedText ?? DefaultRedactedText;
        }

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value == null)
            {
                result = null!;
                return false;
            }

            var type = value.GetType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RedactedValue<>))
            {
                // For redacted values, if value is null, keep it as null to avoid confusion due to obscuring it.
                if (value is RedactedValue<object> redactedValue && redactedValue.Value == null)
                {
                    result = new ScalarValue(null);
                    return true;
                }

                result = new ScalarValue(_redactedText);
                return true;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            var redactedPropertyNames = properties
                .Where(p => Attribute.IsDefined(p, typeof(RedactedAttribute)))
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (redactedPropertyNames.Count == 0)
            {
                result = null!;
                return false;
            }

            var logEventProperties = new List<LogEventProperty>();
            
            foreach (var prop in properties)
            {
                try
                {
                    var propValue = prop.GetValue(value);
                    
                    if (redactedPropertyNames.Contains(prop.Name))
                    {
                        // For redacted properties, if value is null, keep it as null to avoid confusion due to obscuring it.
                        if (propValue == null)
                        {
                            logEventProperties.Add(new LogEventProperty(prop.Name, new ScalarValue(null)));
                        }
                        else
                        {
                            logEventProperties.Add(new LogEventProperty(prop.Name, new ScalarValue(_redactedText)));
                        }
                    }
                    else
                    {
                        var destructuredValue = propertyValueFactory.CreatePropertyValue(propValue, true);
                        logEventProperties.Add(new LogEventProperty(prop.Name, destructuredValue));
                    }
                }
                catch
                {
                    // If we can't read a property, skip it
                }
            }

            result = new StructureValue(logEventProperties, type.Name);
            return true;
        }
    }
}
