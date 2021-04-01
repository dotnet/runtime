using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    internal class FakeConfigurationSource : MemoryConfigurationSource, IConfigurationSource
    {
        internal IConfigurationProvider Provider { get; private set; } = null!;

        public new IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            Provider = new FakeConfigurationProvider(this);
            return Provider;
        }
    }
}
