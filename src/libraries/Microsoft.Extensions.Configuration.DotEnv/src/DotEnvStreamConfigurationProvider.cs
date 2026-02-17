using System.IO;

namespace Microsoft.Extensions.Configuration.DotEnv;

/// <summary>
/// Provides configuration key-value pairs that are obtained from a .env stream.
/// </summary>
public class DotEnvStreamConfigurationProvider(DotEnvStreamConfigurationSource source) : StreamConfigurationProvider(source)
{
    /// <summary>
    /// Loads .env configuration key-value pairs from a stream into a provider.
    /// </summary>
    /// <param name="stream">The .env <see cref="Stream"/> to load configuration data from.</param>
    public override void Load(Stream stream)
    {
        Data = DotEnvConfigurationFileParser.Parse(stream);
    }

    /// <summary>
    /// Used for testing purposes to retrieve a value by its key.
    /// </summary>
    internal string? Get(string key)
    {
        return Data.TryGetValue(key, out var value) ? value : null;
    }
}
