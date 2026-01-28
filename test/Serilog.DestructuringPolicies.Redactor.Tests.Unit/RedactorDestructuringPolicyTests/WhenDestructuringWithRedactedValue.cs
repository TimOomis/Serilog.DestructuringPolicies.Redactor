using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Shouldly;

namespace Serilog.DestructuringPolicies.Redactor.Tests.Unit.RedactorDestructuringPolicyTests
{
    public class WhenDestructuringWithRedactedValue : RedactorDestructuringPolicyTestBase
    {

        [Fact]
        public void GivenARedactedValue_WhenLoggingDestructuredValue_RedactsTheValue()
        {
            // Arrange
            var redactedValue = new RedactedValue<string>("SensitivePassword");

            // Act
            Logger.Information("Logging redacted value: {@RedactedValue}", redactedValue);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedValue = loggedEvent.Properties["RedactedValue"];

            loggedValue.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)loggedValue).Value.ShouldBe("[REDACTED]");
        }

        [Fact]
        public void GivenARedactedValueWithCustomRedactText_WhenLoggingDestructuredValue_RedactsWithCustomText()
        {
            // Arrange
            var redactText = "***HIDDEN***";
            var logger = new LoggerConfiguration()
                .Destructure.WithRedactor(redactText)
                .WriteTo.InMemory()
                .CreateLogger();

            var redactedValue = new RedactedValue<string>("SensitivePassword");

            // Act
            logger.Information("Logging redacted value: {@RedactedValue}", redactedValue);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedValue = loggedEvent.Properties["RedactedValue"];

            loggedValue.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)loggedValue).Value.ShouldBe(redactText);
        }

        [Fact]
        public void GivenARedactedValueWithIntType_WhenLoggingDestructuredValue_RedactsTheValue()
        {
            // Arrange
            var redactedValue = new RedactedValue<int>(123456);

            // Act
            Logger.Information("Logging redacted value: {@RedactedValue}", redactedValue);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedValue = loggedEvent.Properties["RedactedValue"];

            loggedValue.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)loggedValue).Value.ShouldBe("[REDACTED]");
        }

        [Fact]
        public void GivenARedactedValueWithComplexType_WhenLoggingDestructuredValue_RedactsTheValue()
        {
            // Arrange
            var redactedValue = new RedactedValue<TestRecordWithRedactedValue>(new TestRecordWithRedactedValue("Secret", "Public"));

            // Act
            Logger.Information("Logging redacted value: {@RedactedValue}", redactedValue);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedValue = loggedEvent.Properties["RedactedValue"];

            loggedValue.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)loggedValue).Value.ShouldBe("[REDACTED]");
        }

        [Fact]
        public void GivenAnObjectWithRedactedValueProperty_WhenLoggingDestructuredValue_RedactsTheRedactedValueProperty()
        {
            // Arrange
            var testRecord = new TestRecordWithRedactedValue(
                Password: new RedactedValue<string>("SuperSecret123"),
                Username: "john.doe"
            );

            // Act
            Logger.Information("Logging record: {@TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            var passwordProperty = loggedRecord!
                .Properties
                .First(prop => prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase));

            passwordProperty.Value.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)passwordProperty.Value).Value.ShouldBe("[REDACTED]");

            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "Username", "john.doe");
        }

        [Fact]
        public void GivenACollectionOfRedactedValues_WhenLoggingDestructuredValue_RedactsAllValues()
        {
            // Arrange
            var testRecord = new TestRecordWithRedactedValueCollection(
                Secrets:
                [
                    new RedactedValue<string>("Secret1"),
                new RedactedValue<string>("Secret2"),
                new RedactedValue<string>("Secret3")
                ]
            );

            // Act
            Logger.Information("Logging record: {@TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            var secretsCollection = loggedRecord!
                .Properties
                .First(prop => prop.Name.Equals("Secrets", StringComparison.OrdinalIgnoreCase))
                .Value as SequenceValue;

            secretsCollection.ShouldNotBeNull();
            secretsCollection.Elements.Count.ShouldBe(3);

            foreach (var element in secretsCollection.Elements)
            {
                element.ShouldBeOfType<ScalarValue>();
                ((ScalarValue)element).Value.ShouldBe("[REDACTED]");
            }
        }

        [Fact]
        public void GivenANullRedactedValue_WhenLoggingDestructuredValue_LogsAsNull()
        {
            // Arrange
            var testRecord = new TestRecordWithNullableRedactedValue(
                Password: null,
                Username: "john.doe"
            );

            // Act
            Logger.Information("Logging record: {@TestRecord}", testRecord);

            // Assert
            var loggedEvent = InMemorySink.Instance.LogEvents.First();
            var loggedRecord = loggedEvent.Properties["TestRecord"] as StructureValue;

            var passwordProperty = loggedRecord!
                .Properties
                .First(prop => prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase));

            passwordProperty.Value.ShouldBeOfType<ScalarValue>();
            ((ScalarValue)passwordProperty.Value).Value.ShouldBeNull();

            AssertStructureValueContainsPropertyWithValue(loggedRecord!, "Username", "john.doe");
        }
    }

    internal record TestRecordWithRedactedValue(RedactedValue<string> Password, string Username);

    internal record TestRecordWithRedactedValueCollection(RedactedValue<string>[] Secrets);

    internal record TestRecordWithNullableRedactedValue(RedactedValue<string>? Password, string Username);
}
