using System;

namespace Serilog.DestructuringPolicies.Redactor
{
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// Enriches log events with a redactor that replaces sensitive values marked with the <see cref="RedactAttribute"/> with a specified redacted text.
        /// Also adds a destructuring policy to automatically destructure objects with redacted properties.
        /// </summary>
        /// <remarks>
        /// Sensitive values are identified based on the presence of the <see cref="RedactAttribute"/> on properties 
        /// of destructured objects that are being logged. When a property is marked with this attribute, its value is 
        /// replaced with the specified redacted text.
        /// </remarks>
        /// <param name="enrichmentConfiguration">The logger enrichment configuration to apply the redactor to. Cannot be null.</param>
        /// <param name="redactedText">The text to use in place of redacted values. If null, the default "[REDACTED]" is used.</param>
        /// <returns>A logger configuration object enriched with the redactor.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the enrichmentConfiguration parameter provided is null.</exception>
        public static LoggerConfiguration WithRedactor(this Configuration.LoggerDestructuringConfiguration enrichmentConfiguration, string redactedText = null)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration.With(new RedactorDestructuringPolicy(redactedText));
        }
    }
}
