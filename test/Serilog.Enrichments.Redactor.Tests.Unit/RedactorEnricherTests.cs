using Serilog.Core;
using Serilog.Enrichers.Redactor;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Shouldly;

namespace Serilog.Enrichments.Redactor.Tests.Unit
{
    public class RedactorEnricherTests
    {
        private readonly Logger _logger;

        public RedactorEnricherTests()
        {
            _logger = new LoggerConfiguration()
                .Enrich.WithRedactor()
                .WriteTo.InMemory()
                .CreateLogger();

            // Clear the in-memory sink before each test.
            InMemorySink.Instance.Dispose();
        }

        [Fact]
        public void GivenACustomRedactTextInConstructor_WhenLoggingDestructuredValue_RedactsPropertiesWithExpectedText()
        {
            // Arrange
            var redactText = "❌❌❌";
            var logger = new LoggerConfiguration()
                .Enrich.With(new RedactorEnricher(redactText))
                .WriteTo.InMemory()
                .CreateLogger();

            var testRecord = new TestRecord("SecretValue", "PublicValue");

            // Act
            logger.Information("Logging record: {@TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "SensitiveData", redactText);
        }

        [Fact]
        public void GivenARedactedProperty_WhenLoggingDestructuredValue_RedactsPropertiesWithAttribute()
        {
            // Arrange
            var testRecord = new TestRecord("SecretValue", "PublicValue");

            // Act
            _logger.Information("Logging record: {@TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "SensitiveData", "[REDACTED]");
            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "NonSensitiveData", "PublicValue");
        }

        [Fact]
        public void GivenARedactedProperty_WhenLoggingDestructuredValueWithNullSensitiveData_RedactsPropertiesAsNull()
        {
            // Arrange
            var testRecord = new TestRecord(null, "PublicValue");

            // Act
            _logger.Information("Logging record: {@TestRecord}", testRecord);
            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            AssertStructureValueContainsPropertyWithValue<string?>(loggedRecord!, "SensitiveData", null);
            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "NonSensitiveData", "PublicValue");
        }

        [Fact]
        public void GivenANestedRedactedProperty_WhenLoggingDestructuredValue_RedactsNestedPropertiesWithAttribute()
        {
            // Arrange
            var nestedRecord = new TestRecordWithNesting(
                NestedRecord: new TestRecord("NestedSecret", "Nested"),
                RecordCollection:
                [
                    new TestRecord("CollectionSecret1", "Collection1"),
                    new TestRecord("CollectionSecret2", "Collection2")
                ]);

            // Act
            _logger.Information("Logging nested record: {@NestedRecord}", nestedRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["NestedRecord"] as StructureValue;

            var nestedRecordValue = loggedRecord!
                .Properties
                .First(prop => prop.Name.Equals("NestedRecord", StringComparison.OrdinalIgnoreCase))
                .Value as StructureValue;

            var nestedCollectionValue = loggedRecord!
                .Properties
                .First(prop => prop.Name.Equals("RecordCollection", StringComparison.OrdinalIgnoreCase))
                .Value as SequenceValue;

            AssertStructureValueContainsPropertyWithValue(nestedRecordValue!, "SensitiveData", "[REDACTED]");

            foreach (var item in nestedCollectionValue!.Elements)
            {
                var structureValue = item as StructureValue;
                AssertStructureValueContainsPropertyWithValue(structureValue!, "SensitiveData", "[REDACTED]");
            }
        }

        [Fact]
        public void GivenARedactedProperty_WhenLoggingStructuredValue_ThenRedactsExpectedProperties()
        {
            // Arrange
            var testRecord = new TestRecord("SecretValue", "PublicValue");

            // Act
            _logger.Information("Logging structured value: {TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();

            AssertStructureValueContainsPropertyWithValue(
                loggedEvent.Properties["TestRecord"] as StructureValue,
                "SensitiveData",
                "[REDACTED]");
        }

        private static void AssertStructureValueContainsPropertyWithValue<T>(StructureValue structureValue, string propertyName, T? value)
        {
            structureValue!
                .Properties
                .ShouldContain(prop =>
                    prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.Equals(new ScalarValue(value)));
        }
    }

    internal record TestRecord(
        [property: Redacted] string? SensitiveData, string? NonSensitiveData);

    internal record TestRecordWithNesting(TestRecord NestedRecord, TestRecord[] RecordCollection);

}
