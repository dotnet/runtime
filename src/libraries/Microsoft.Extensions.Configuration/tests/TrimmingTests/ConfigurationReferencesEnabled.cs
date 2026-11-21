// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ["Pointer"] = "Target",
            ["Probe"] = "$ref({Pointer})",
            ["Section:Sibling"] = "$ref(Target)",
            ["Section:Value"] = "$ref(..:Sibling)",
        };
        using var root = new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new MemoryConfigurationProvider(new MemoryConfigurationSource { InitialData = data }),
        });
        using var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(data);

        foreach (IConfigurationRoot configuration in new IConfigurationRoot[] { root, manager })
        {
            IConfigurationSection section = configuration.GetSection("Section");
            if (configuration["Probe"] != "hit" ||
                section["Value"] != "hit" ||
                section.GetSection("Value").Value != "hit")
            {
                return -1;
            }

            if (section is not ConfigurationSection configurationSection ||
                !configurationSection.TryGetValue("Value", out string? value) || value != "hit")
            {
                return -2;
            }
        }

        return 100;
    }
}
