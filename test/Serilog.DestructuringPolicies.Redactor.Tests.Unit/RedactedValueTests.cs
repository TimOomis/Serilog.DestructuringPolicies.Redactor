using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;

namespace Serilog.DestructuringPolicies.Redactor.Tests.Unit;

public class RedactedValueTests
{
    [Fact]
    public void GivenARedactedValue_WhenAccessingValue_ThenReturnsOriginalValue()
    {
        // Arrange
        var originalValue = "SensitiveInformation";
        RedactedValue<string> redactedValue = originalValue;
        
        // Act
        var accessedValue = redactedValue.Value;
        
        // Assert
        accessedValue.ShouldBe(originalValue);
    }

    [Fact]
    public void GivenARedactedValue_WhenAccessingValueViaImplicitOperator_ThenReturnsOriginalValue()
    {
        // Arrange
        var originalValue = "SensitiveInformation";
        RedactedValue<string> redactedValue = originalValue;
        
        // Act
        string accessedValue = redactedValue;
        
        // Assert
        accessedValue.ShouldBe(originalValue);
    }

    [Fact]
    public void GivenANullRedactedValue_WhenAccessingValue_ThenReturnsNull()
    {
        // Arrange
        RedactedValue<string> redactedValue = null;
        
        // Act
        var accessedValue = redactedValue?.Value;
        
        // Assert
        accessedValue.ShouldBeNull();
    }

    [Fact]
    public void GivenARedactedValue_WhenConvertedToString_ThenReturnsRedactedPlaceholder()
    {
        // Arrange
        var redactedValue = new RedactedValue<string>("SensitiveInformation");
        
        // Act
        var stringRepresentation = redactedValue.ToString();
        
        // Assert
        stringRepresentation.ShouldBe("[REDACTED]");
    }
}
