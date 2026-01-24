using System;

namespace Serilog.Enrichers.Redactor
{
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// Enriches log events with a redactor that replaces sensitive values marked with the <see cref="RedactAttribute"/> with a specified redacted text.
        /// </summary>
        /// <remarks>
        /// Sensitive values are identified based on the presence of the <see cref="RedactAttribute"/> on properties 
        /// of (de-)structured objects that are being logged. When a property is marked with this attribute, its value is 
        /// </remarks>
        /// <param name="enrichmentConfiguration">The logger enrichment configuration to apply the redactor to. Cannot be null.</param>
        /// <param name="redactedText">The text to use in place of redacted values. If null, the default "[REDACTED]" is used.</param>
        /// <returns>A logger configuration object enriched with the redactor.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the enrichmentConfiguration parameter provided is null.</exception>
        public static LoggerConfiguration WithRedactor(this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration, string redactedText = null)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            return enrichmentConfiguration.With(new RedactorEnricher(redactedText));
        }
    }
}
