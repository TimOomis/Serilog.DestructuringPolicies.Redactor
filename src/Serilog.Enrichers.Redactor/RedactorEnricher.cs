using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Serilog.Enrichers.Redactor
{
    public class RedactorEnricher : ILogEventEnricher
    {
        private const string DefaultRedactedText = "[REDACTED]";
        private readonly string _redactedText;

        public RedactorEnricher(string redactedText = DefaultRedactedText)
        {
            _redactedText = redactedText ?? DefaultRedactedText;

        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var (propertyName, propertyValue) in logEvent.Properties)
            {
                RedactLogEventPropertyValue(logEvent, propertyName, propertyValue);
            }
        }

        private void RedactLogEventPropertyValue(LogEvent logEvent, string propertyName, LogEventPropertyValue propertyValue)
        {
            if (propertyValue is null)
            {
                return;
            }

            if (propertyValue is ScalarValue scalarValue)
            {
                RedactScalarValueLogProperty(logEvent, scalarValue, propertyName);
            }
            else if (propertyValue is StructureValue structureValue)
            {
                RedactStructureValue(logEvent, structureValue, propertyName);
            }
            else if (propertyValue is SequenceValue sequenceValue)
            {
                RedactSequenceValue(logEvent, sequenceValue, propertyName);
            }
        }

        private static HashSet<string> GetRedactedProperties(Type type) => 
            type.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(RedactedAttribute)))
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private void RedactScalarValueLogProperty(LogEvent logEvent, ScalarValue scalarValue, string propertyKey)
        {
            // If the scalar value is null, nothing to redact.
            if (scalarValue.Value == null)
            {
                return;
            }

            var type = scalarValue.Value.GetType();
            var redactedProperties = GetRedactedProperties(type);

            if (redactedProperties.Count == 0)
            {
                return;
            }

            var redactedLogEventProperty = new LogEventProperty(propertyKey, new ScalarValue(_redactedText));
            logEvent.AddOrUpdateProperty(redactedLogEventProperty);
        }

        private void RedactStructureValue(LogEvent logEvent, StructureValue structureValue, string propertyName)
        {
            var type = ResolveTypeFromAssembly(structureValue.TypeTag);

            if (type == null)
            {
                return;
            }

            var redactedProperties = GetRedactedProperties(type);
            
            var redactedStructure = RedactStructureValueProperties(structureValue, redactedProperties);
            
            var redactedLogEventProperty = new LogEventProperty(propertyName, redactedStructure);
            logEvent.AddOrUpdateProperty(redactedLogEventProperty);
        }

        private StructureValue RedactStructureValueProperties(StructureValue structureValue, HashSet<string> redactedProperties)
        {
            var redactedPropertiesValues = structureValue.Properties
                .Select(prop =>
                {
                    // Null value or Null Scalar value, keep as is.
                    if (prop.Value is null || prop.Value is ScalarValue scalarValue && scalarValue.Value is null)
                    {
                        return new LogEventProperty(prop.Name, new ScalarValue(null));
                    }

                    // If it is a nested structure, recurse.
                    if (prop.Value is StructureValue nestedStructureValue)
                    {
                        var nestedType = ResolveTypeFromAssembly(nestedStructureValue.TypeTag);
                        if (nestedType != null)
                        {
                            var nestedRedactedProperties = GetRedactedProperties(nestedType);
                            var nestedRedactedStructure = RedactStructureValueProperties(nestedStructureValue, nestedRedactedProperties);
                            return new LogEventProperty(prop.Name, nestedRedactedStructure);
                        }
                    }

                    // If it is a sequence (collection/array), process each element.
                    if (prop.Value is SequenceValue sequenceValue)
                    {
                        var redactedSequence = RedactSequenceValueElements(sequenceValue);
                        return new LogEventProperty(prop.Name, redactedSequence);
                    }

                    if (redactedProperties.Contains(prop.Name))
                    {
                        return new LogEventProperty(prop.Name, new ScalarValue(_redactedText));
                    }
                    return prop;
                })
                .ToList();

           return new StructureValue(redactedPropertiesValues, structureValue.TypeTag);
        }

        private void RedactSequenceValue(LogEvent logEvent, SequenceValue sequenceValue, string propertyName)
        {
            var redactedSequence = RedactSequenceValueElements(sequenceValue);
            var redactedLogEventProperty = new LogEventProperty(propertyName, redactedSequence);
            logEvent.AddOrUpdateProperty(redactedLogEventProperty);
        }

        private SequenceValue RedactSequenceValueElements(SequenceValue sequenceValue)
        {
            var redactedElements = sequenceValue.Elements
                .Select(element =>
                {
                    if (element is StructureValue structureValue)
                    {
                        var type = ResolveTypeFromAssembly(structureValue.TypeTag);
                        if (type != null)
                        {
                            var redactedProperties = GetRedactedProperties(type);
                            if (redactedProperties.Count > 0)
                            {
                                return RedactStructureValueProperties(structureValue, redactedProperties);
                            }
                        }
                    }
                    else if (element is SequenceValue nestedSequence)
                    {
                        return RedactSequenceValueElements(nestedSequence);
                    }
                    
                    return element;
                })
                .ToList();

            return new SequenceValue(redactedElements);
        }

        private static Type? ResolveTypeFromAssembly(string? fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.FullName == fullTypeName || t.Name == fullTypeName);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
