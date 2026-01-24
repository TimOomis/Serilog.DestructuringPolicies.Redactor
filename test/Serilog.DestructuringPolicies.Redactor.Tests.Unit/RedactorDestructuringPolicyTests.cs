using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Shouldly;

namespace Serilog.DestructuringPolicies.Redactor.Tests.Unit;

public class RedactorDestructuringPolicyTests
{
    private readonly Logger _logger;

    public RedactorDestructuringPolicyTests()
    {
        _logger = new LoggerConfiguration()
            .Destructure.WithRedactor()
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
            .Destructure.WithRedactor(redactText)
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
            ],
            SensitiveRecord: new TestRecord("SensitiveRecordData", "NonSensitiveDataThatShouldAlsoBeHidden"));

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


        AssertStructureValueContainsPropertyWithValue(loggedRecord!, "SensitiveRecord", "[REDACTED]");
        AssertStructureValueContainsPropertyWithValue(nestedRecordValue!, "SensitiveData", "[REDACTED]");

        foreach (var item in nestedCollectionValue!.Elements)
        {
            var structureValue = item as StructureValue;
            AssertStructureValueContainsPropertyWithValue(structureValue!, "SensitiveData", "[REDACTED]");
        }
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

internal record TestRecordWithNesting(TestRecord NestedRecord, TestRecord[] RecordCollection, [property:Redacted] TestRecord SensitiveRecord);
