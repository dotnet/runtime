// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    internal sealed class ConfigurationItemDebugView
    {
        public ConfigurationItemDebugView(string path, string? value, IConfigurationProvider? provider)
        {
            Path = path;
            Value = value;
            Provider = provider;
        }

        public string Path { get; }
        public string? Value { get; }
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

        internal static List<ConfigurationItemDebugView> FromConfiguration(IConfiguration current, IConfigurationRoot root, bool makePathsRelative = true)
        {
            var data = new List<ConfigurationItemDebugView>();

            var stack = new Stack<IConfiguration>();
            stack.Push(current);
            int prefixLength = (makePathsRelative && current is IConfigurationSection rootSection) ? rootSection.Path.Length + 1 : 0;
            while (stack.Count > 0)
            {
                IConfiguration config = stack.Pop();
                // Don't include the sections value if we are removing paths, since it will be an empty key
                if (config is IConfigurationSection section && (!makePathsRelative || config != current))
                {
                    (string? value, IConfigurationProvider? provider) = GetValueAndProvider(root, section.Path);

                    data.Add(new ConfigurationItemDebugView(section.Path.Substring(prefixLength), value, provider));
                }
                foreach (IConfigurationSection child in config.GetChildren())
                {
                    stack.Push(child);
                }
            }

            data.Sort((i1, i2) => ConfigurationKeyComparer.Instance.Compare(i1.Path, i2.Path));
            return data;
        }

        internal static (string? Value, IConfigurationProvider? Provider) GetValueAndProvider(IConfigurationRoot root, string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out string? value))
                {
                    return (value, provider);
                }
            }

            return (null, null);
        }
    }
}
