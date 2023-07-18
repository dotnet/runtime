// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    internal sealed class ConfigurationSectionDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IConfigurationSection _section;

        public ConfigurationSectionDebugView(IConfigurationSection section, string path, IConfigurationProvider? provider)
        {
            _section = section;
            Path = path;
            Provider = provider;
        }

        public string Path { get; }
        public string Key => _section.Key;
        public string FullPath => _section.Path;
        public string? Value => _section.Value;
        public IConfigurationProvider? Provider { get; }

        public override string ToString()
        {
            var s = $"Path = {Path}";
            if (Value is not null)
            {
                s += $", Value = {Value}";
            }
            if (Provider is not null)
            {
                s += $", Provider = {Provider}";
            }
            return s;
        }

        internal static List<ConfigurationSectionDebugView> FromConfiguration(IConfiguration current, IConfigurationRoot root)
        {
            var data = new List<ConfigurationSectionDebugView>();

            var stack = new Stack<IConfiguration>();
            stack.Push(current);
            int prefixLength = (current is IConfigurationSection rootSection) ? rootSection.Path.Length + 1 : 0;
            while (stack.Count > 0)
            {
                IConfiguration config = stack.Pop();
                // Don't include the sections value if we are removing paths, since it will be an empty key
                if (config is IConfigurationSection section && config != current)
                {
                    IConfigurationProvider? provider = GetValueProvider(root, section.Path);
                    string path = section.Path.Substring(prefixLength);

                    data.Add(new ConfigurationSectionDebugView(section, path, provider));
                }
                foreach (IConfigurationSection child in config.GetChildren())
                {
                    stack.Push(child);
                }
            }

            data.Sort((i1, i2) => ConfigurationKeyComparer.Instance.Compare(i1.Path, i2.Path));
            return data;
        }

        internal static IConfigurationProvider? GetValueProvider(IConfigurationRoot root, string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out _))
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
