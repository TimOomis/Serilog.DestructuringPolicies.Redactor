using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Serilog.DestructuringPolicies.Redactor
{
    public class RedactorDestructuringPolicy : IDestructuringPolicy
    {
        private const string DefaultRedactedText = "[REDACTED]";
        private readonly string _redactedText;

        private static readonly ConditionalWeakTable<Type, object> _typesWithoutRedactedProps = new ConditionalWeakTable<Type, object>();
        private static readonly ConditionalWeakTable<Type, PropertyInfo[]> _cachedRedactedTypeProps = new ConditionalWeakTable<Type, PropertyInfo[]>();
        private static readonly object _marker = new object();
        private static readonly ScalarValue nullScalerValue = new ScalarValue(null);

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
                    result = nullScalerValue;
                    return true;
                }

                result = new ScalarValue(_redactedText);
                return true;
            }

            // Known non-redacted types
            if (_typesWithoutRedactedProps.TryGetValue(type, out _))
            {
                result = null!;
                return false;
            }

            // Thread-safe lazy initialization using GetValue()
            // Factory may run more than once under contention; only one value is stored.
            var properties = _cachedRedactedTypeProps.GetValue(type, t =>
            {
                var allProps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // Avoid indexers (they throw TargetParameterCountException when calling GetValue(obj) without args)
                var nonIndexerProps = allProps
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToArray();

                // Only cache props for types that actually have any [Redacted] property.
                var hasAnyRedacted = nonIndexerProps.Any(p => Attribute.IsDefined(p, typeof(RedactedAttribute)));
                if (!hasAnyRedacted)
                {
                    _typesWithoutRedactedProps.GetValue(t, _ => _marker);

                    // We must return *something* to satisfy ConditionalWeakTable's factory contract.
                    // We'll return the computed props array, but the caller will immediately return false.
                    return nonIndexerProps;
                }

                return nonIndexerProps;
            });

            if (_typesWithoutRedactedProps.TryGetValue(type, out _))
            {
                result = null!;
                return false;
            }

            var redactedPropertyNames = properties
                .Where(p => Attribute.IsDefined(p, typeof(RedactedAttribute)))
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (redactedPropertyNames.Count == 0)
            {
                // Defensive: treat as non-redacted
                _typesWithoutRedactedProps.GetValue(type, _ => _marker);
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
                            logEventProperties.Add(new LogEventProperty(prop.Name, nullScalerValue));
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
