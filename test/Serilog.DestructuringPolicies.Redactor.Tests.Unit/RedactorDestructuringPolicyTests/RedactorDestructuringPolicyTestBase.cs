using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Shouldly;

namespace Serilog.DestructuringPolicies.Redactor.Tests.Unit.RedactorDestructuringPolicyTests
{
    public class RedactorDestructuringPolicyTestBase
    {
        protected readonly Logger Logger;

        protected RedactorDestructuringPolicyTestBase()
        {
            Logger = new LoggerConfiguration()
                .Destructure.WithRedactor()
                .WriteTo.InMemory()
                .CreateLogger();

            // Clear the in-memory sink before each test.
            InMemorySink.Instance.Dispose();
        }


        protected static void AssertStructureValueContainsPropertyWithValue<T>(StructureValue structureValue, string propertyName, T? value)
        {
            structureValue!
                .Properties
                .ShouldContain(prop =>
                    prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.Equals(new ScalarValue(value)));
        }
    }
}