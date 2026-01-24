using Serilog;
using Serilog.DestructuringPolicies.Redactor;
using Serilog.DestructuringPolicies.Redactor.DemoApp.Models;

using var log = new LoggerConfiguration()
    .Destructure.WithRedactor()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

while (!cancellationTokenSource.Token.IsCancellationRequested)
{
    var company = CreateCompany();
    log.Information("Logging company {@Company}", company);

    log.Information("Logging people individually:");

    foreach (var person in company.Employees)
    {
        log.Information("Logging person {@Person}", person);
        log.Information("Directly passing a redacted scalar value requires the RedactedValue<T> type:");
        log.Information("With RedactedValue: {@Username}, {@Password}", person.Username, person.Password);
        log.Information("With Structured RedactedValue: {Username}, {Password}", person.Username, person.Password);
        log.Information("Without RedactedValue, properties accessed directly are not redacted: {@SSN}, {Emails}", person.SocialSecurityNumber, person.Emails);
    }   

    await Task.Delay(2000);
}

static Company CreateCompany()
{
    return new Company("Fake Inc.",
    [
        new Person
        {
            Name = "John Doe",
            SocialSecurityNumber = "123-45-6789",
            Emails = [
                "john.doe@fake.com",
                "j.doe@fake.com"],
            Username = "j-doe",
            Password = "P@ssw0rd!"
        },
        new Person
        {
            Name = "Jane Doe",
            SocialSecurityNumber = null,
        }
    ]);
}