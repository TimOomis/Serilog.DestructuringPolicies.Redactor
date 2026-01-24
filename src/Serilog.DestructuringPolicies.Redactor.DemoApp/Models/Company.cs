namespace Serilog.DestructuringPolicies.Redactor.DemoApp.Models;

public record Company(
    string Name,
    IReadOnlyCollection<Person> Employees
);
