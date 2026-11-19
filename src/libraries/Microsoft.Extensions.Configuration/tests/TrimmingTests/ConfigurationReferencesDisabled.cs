// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

#nullable enable

internal static class Program
{
    private static int Main()
    {
        var data = new Dictionary<string, string?>
        {
            ["Target"] = "hit",
            ["Probe"] = "$ref(Target)",
            ["Malformed"] = "$ref()",
            ["Null"] = null,
        };
        using var root = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new MemoryConfigurationProvider(new MemoryConfigurationSource { InitialData = data }),
        });
        using var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(data);

        foreach (IConfigurationRoot configuration in new IConfigurationRoot[] { root, manager })
        {
            foreach (KeyValuePair<string, string?> entry in data)
            {
                IConfigurationSection section = configuration.GetSection(entry.Key);
                if (configuration[entry.Key] != entry.Value || section.Value != entry.Value)
                {
                    return -1;
                }

                if (section is not ConfigurationSection configurationSection ||
                    !configurationSection.TryGetValue(null, out string? value) || value != entry.Value)
                {
                    return -2;
                }
            }
        }

        if (GetConfigurationType("Microsoft.Extensions.Configuration.ReferenceResolver") is not null ||
            GetConfigurationType("Microsoft.Extensions.Configuration.ReferenceSyntax") is not null)
        {
            return -3;
        }

        return 100;
    }

    // Keep the type name indirect so the trimming assertion does not preserve the type it checks.
    private static Type? GetConfigurationType(string name) =>
        typeof(ConfigurationRoot).Assembly.GetType(name, throwOnError: false);
}
