using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class ConsoleLoggerOptionsSetup : ConfigureFromConfigurationOptions<ConsoleLoggerOptions>
    {
        public ConsoleLoggerOptionsSetup(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}