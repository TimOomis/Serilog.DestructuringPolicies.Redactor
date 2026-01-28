using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace Serilog.DestructuringPolicies.Redactor.Tests.Unit.RedactorDestructuringPolicyTests;

public class WhenDestructuringWithRedactedAttributes : RedactorDestructuringPolicyTestBase
{
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
        Logger.Information("Logging record: {@TestRecord}", testRecord);

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
        Logger.Information("Logging record: {@TestRecord}", testRecord);
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
        Logger.Information("Logging nested record: {@NestedRecord}", nestedRecord);

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

    internal record TestRecord(
        [property: Redacted] string? SensitiveData, string? NonSensitiveData);

    internal record TestRecordWithNesting(TestRecord NestedRecord, TestRecord[] RecordCollection, [property: Redacted] TestRecord SensitiveRecord);
}
