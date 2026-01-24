namespace Serilog.DestructuringPolicies.Redactor.DemoApp.Models;

public class Person
{
    public string? Name { get; init; }

    [Redacted]
    public string? SocialSecurityNumber { get; init; }

    [Redacted]
    public IReadOnlyCollection<string>? Emails { get; init; }

    // Properties we wish to log directly as scalar values must also use RedactedValue<T>
    [Redacted]
    public RedactedValue<string>? Username { get; init; }

    [Redacted]
    public RedactedValue<string>? Password { get; init; }
}
