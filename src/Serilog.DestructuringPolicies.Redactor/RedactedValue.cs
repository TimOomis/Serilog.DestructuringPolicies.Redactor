namespace Serilog.DestructuringPolicies.Redactor
{
    /// <summary>
    /// Represents a value of type <typeparamref name="T"/> whose contents are intentionally hidden or redacted when
    /// displayed.
    /// </summary>
    /// <remarks>Use this type to encapsulate sensitive information that should not be revealed in logs, user
    /// interfaces, or other output.This type is mandatory if you wish to log scalar values directly instead of as a property of an object, as a scalar value itself cannot be picked up by the destructuring policy.
    /// The actual value can be accessed via the <see cref="Value"/> property or directly per the implicit operators, but calling
    /// <see cref="ToString"/> returns a redacted placeholder.
    /// </remarks>
    /// <typeparam name="T">The type of the value to be redacted.</typeparam>
    public sealed class RedactedValue<T>
    {
        private readonly T _value;

        public RedactedValue(T value) => _value = value;

        [Redacted]
        public T Value => _value;

        public static implicit operator T(RedactedValue<T> redactedValue) => redactedValue._value;

        public static implicit operator RedactedValue<T>(T value) => new RedactedValue<T>(value);

        public override string ToString() => "[REDACTED]";
    }
}
