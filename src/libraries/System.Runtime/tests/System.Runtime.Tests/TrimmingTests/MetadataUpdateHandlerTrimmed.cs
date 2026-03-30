// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

/// <summary>
/// Tests that when MetadataUpdater.IsSupported is false, the MetadataUpdateHandlerAttribute
/// instances and the handler types they reference are trimmed from all assemblies.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Hard references to ensure these assemblies are loaded and not entirely removed by the trimmer.
        // We use types that the trimmer cannot remove (public API surface).
        _ = typeof(System.ComponentModel.TypeConverter);
        _ = typeof(System.Text.Json.JsonSerializer);

        // MetadataUpdater.IsSupported should be false
        if (MetadataUpdater.IsSupported)
        {
            Console.WriteLine("Failed: MetadataUpdater.IsSupported should be false");
            return -1;
        }

        // Verify MetadataUpdateHandlerAttribute is not present on any assembly
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var attr in assembly.GetCustomAttributesData())
            {
                if (attr.AttributeType.Name == "MetadataUpdateHandlerAttribute")
                {
                    Console.WriteLine($"Failed: {assembly.GetName().Name} still has MetadataUpdateHandlerAttribute");
                    return -1;
                }
            }
        }

        // Verify the handler types themselves are trimmed
        string[] handlerTypeNames = new[]
        {
            "System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler",
            "System.ComponentModel.ReflectionCachesUpdateHandler",
            "System.Text.Json.JsonSerializerOptionsUpdateHandler",
        };

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var typeName in handlerTypeNames)
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    Console.WriteLine($"Failed: {assembly.GetName().Name} still contains {typeName}");
                    return -1;
                }
            }
        }

        return 100;
    }
}
