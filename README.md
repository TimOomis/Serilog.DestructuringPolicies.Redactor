# Serilog.DestructuringPolicies.Redactor

[![NuGet](https://img.shields.io/nuget/v/Serilog.DestructuringPolicies.Redactor.svg)](https://www.nuget.org/packages/Serilog.DestructuringPolicies.Redactor)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Serilog.DestructuringPolicies.Redactor.svg)](https://www.nuget.org/packages/Serilog.DestructuringPolicies.Redactor/)

A Serilog destructuring policy that automatically redacts sensitive properties marked with the included `[Redacted]` attribute or encapsulated in the `RedactedValue<T>` type. This helps prevent sensitive data like passwords, API-keys, credit card numbers, and personal information from appearing in your logs.
 
## Features

- 🔒 **Automatic Redaction**: Mark properties with `[Redacted]` attribute to automatically redact their values in logs
- 🎯 **Selective Protection**: Only redact specific properties, not entire objects
- 🚀 **Zero-config**: Works with Serilog's structured logging out of the box

## Requirements

- .NET Standard 2.1 or higher
- Serilog 4.3.0 or higher


## Installation

Install via NuGet:

```bash
dotnet add package Serilog.DestructuringPolicies.Redactor
```

Or via Package Manager Console:

```powershell
Install-Package Serilog.DestructuringPolicies.Redactor
```

## Usage

### Basic Setup

Configure Serilog to use the redactor destructuring policy:

```csharp
using Serilog;
using Serilog.DestructuringPolicies.Redactor;

Log.Logger = new LoggerConfiguration()
    .Destructure.WithRedactor()
    .WriteTo.Console()
    .CreateLogger();
```

### Mark Sensitive Properties

Use the `[Redacted]` attribute on properties you want to protect:

```csharp
public class Person
{
    public string Name { get; set; }
    
    [Redacted]
    public string SocialSecurityNumber { get; set; }
    
    [Redacted]
    public string Password { get; set; }
}
```

### Log Structured Data

When you log objects with redacted properties, they'll automatically be masked:

```csharp
var person = new Person
{
    Name = "John Doe",
    SocialSecurityNumber = "123-45-6789",
    Password = "SuperSecret123"
};

Log.Information("User created: {@Person}", person);

// Output: User created: { Name: "John Doe", SocialSecurityNumber: "[REDACTED]", Password: "[REDACTED]" }
```

### Using `RedactedValue<T>` for Redaction

As an alternative to the `[Redacted]` attribute, you can use the `RedactedValue<T>` wrapper to redact sensitive data. This works both for properties in destructured objects and for scalar values logged directly:

```csharp
public class User
{
    public string Name { get; set; }
    
    // RedactedValue<T> automatically redacts without needing the [Redacted] attribute
    public RedactedValue<string> Username { get; set; }
    
    public RedactedValue<string> ApiKey { get; set; }
}

var user = new User
{
    Name = "Jane Smith",
    Username = "jsmith",  // Implicit conversion from string to RedactedValue<string>
    ApiKey = "sk_live_abc123xyz"
};

Log.Information("Login attempt: {@User}", user);
// Output: Login attempt: { Name: "Jane Smith", Username: { Value: "[REDACTED]" }, ApiKey: { Value: "[REDACTED]" } }

Log.Information("Direct scalar logging: Username={@Username}, ApiKey={@ApiKey}", user.Username, user.ApiKey);
// Output: Direct scalar logging: Username={ Value: "[REDACTED]" }, ApiKey={ Value: "[REDACTED]" }

// Access the actual value when needed
string actualUsername = user.Username; // Implicit conversion back to string
```

**Why `RedactedValue<T>`?** 

`RedactedValue<T>` provides a type-safe way to mark values as sensitive without using attributes. It works as a standalone solution:
- In destructured objects: The wrapper itself signals that the value should be redacted
- As scalar values: When logged directly, it ensures the value is protected even outside of object properties

This approach gives you flexibility to choose between attribute-based redaction (`[Redacted]`) or type-based redaction (`RedactedValue<T>`) based on your needs.

### Custom Redacted Text

You can customize the redacted placeholder text:

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.WithRedactor("***HIDDEN***")
    .WriteTo.Console()
    .CreateLogger();

var person = new Person { Password = "secret123" };
Log.Information("User: {@Person}", person);
// Output: User: { Password: "***HIDDEN***", ... }
```

## Demo Application

The project includes a demo application that showcases various redaction scenarios. You can find it in `src/Serilog.DestructuringPolicies.Redactor.DemoApp/`.

The demo illustrates:
- Redacting properties in nested objects (Company with Employees)
- Using `RedactedValue<T>` for scalar values
- Comparing redacted vs. non-redacted output

### Running the Demo

```bash
cd src/Serilog.DestructuringPolicies.Redactor.DemoApp
dotnet run
```

Or with Docker:

```bash
docker-compose up
```

The demo logs sample employee data continuously, showing how different types of sensitive information are handled.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [Serilog](https://github.com/serilog/serilog) - The core Serilog logging library

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/TimOomis/Serilog.DestructuringPolicies.Redactor/issues) on GitHub.
