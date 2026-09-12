// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// The writable provider behind <see cref="WritableIniConfigurationSource"/>.
    /// <see cref="Load"/> parses the INI into configuration data (identical keys to
    /// the built-in provider); <see cref="Save"/> writes the current data back via
    /// <see cref="IniDocument"/>, so unmanaged sections/keys and value quoting are
    /// preserved across a read-modify-write.
    /// </summary>
    public sealed class WritableIniConfigurationProvider : ConfigurationProvider
    {
        private readonly WritableIniConfigurationSource _source;
        private readonly Dictionary<string, bool> _quoting = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Initializes a new instance for the given source.</summary>
        public WritableIniConfigurationProvider(WritableIniConfigurationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            _source = source;
        }

        /// <summary>The INI file this provider reads from and writes to.</summary>
        public string Path => _source.Path;

        /// <summary>
        /// Loads the INI file into configuration data. A missing file yields empty
        /// data when the source is optional, otherwise throws.
        /// </summary>
        public override void Load()
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_source.Path))
            {
                var document = IniDocument.Parse(File.ReadAllText(_source.Path));
                foreach (var section in document.Sections)
                {
                    foreach (var (key, value) in document.Entries(section))
                        data[ToConfigKey(section, key)] = value;
                }
            }
            else if (!_source.Optional)
            {
                throw new FileNotFoundException($"Required INI file not found: {_source.Path}", _source.Path);
            }

            Data = data;
        }

        /// <summary>
        /// Sets a resource. <paramref name="quote"/> controls how <see cref="Save"/>
        /// writes it: true = double-quoted, false = bare, null = preserve the
        /// existing entry's quoting (or bare if new).
        /// </summary>
        public void SetValue(string section, string key, string value, bool? quote = null)
        {
            var configKey = ToConfigKey(section, key);
            Set(configKey, value);
            if (quote.HasValue)
                _quoting[configKey] = quote.Value;
        }

        /// <summary>
        /// Writes the current configuration back to the INI file via a
        /// read-modify-write: existing resources keep their quoting and any
        /// resources not present in the data are preserved verbatim.
        /// </summary>
        public void Save()
        {
            var document = File.Exists(_source.Path)
                ? IniDocument.Parse(File.ReadAllText(_source.Path))
                : new IniDocument();

            foreach (var (configKey, value) in Data)
            {
                if (value is null || !TrySplitConfigKey(configKey, out var section, out var resource))
                    continue;

                var quote = _quoting.TryGetValue(configKey, out var q) ? q : (bool?)null;
                document.Set(section, resource, value, quote);
            }

            var directory = System.IO.Path.GetDirectoryName(_source.Path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_source.Path, document.ToIniString());
        }

        private static string ToConfigKey(string section, string key) =>
            $"{section}{ConfigurationPath.KeyDelimiter}{key}";

        private static bool TrySplitConfigKey(string configKey, out string section, out string resource)
        {
            var index = configKey.IndexOf(ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            if (index <= 0 || index >= configKey.Length - 1)
            {
                section = string.Empty;
                resource = string.Empty;
                return false;
            }

            section = configKey[..index];
            resource = configKey[(index + 1)..];
            return true;
        }
    }
}
