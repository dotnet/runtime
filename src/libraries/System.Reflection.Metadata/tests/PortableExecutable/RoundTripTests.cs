// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests;

public class RoundTripTests
{
    [Fact]
    public void RoundTrip_Assembly_WithEmbeddedResources()
    {
        var random = new Random(42);
        var randomData100 = new byte[100];
        var randomData1KB = new byte[1024];
        random.NextBytes(randomData100);
        random.NextBytes(randomData1KB);

        var resources = new (string Name, byte[] Data)[]
        {
            ("Resource_100bytes", randomData100),
            ("Resource_1KB", randomData1KB),
            ("Resource_Empty", Array.Empty<byte>()),
            ("Ресурс", new byte[] { 1, 2, 3, 4, 5 }),
            ("リソース", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }),
            ("Resource_🎉", new byte[] { 0xCA, 0xFE, 0xBA, 0xBE })
        };

        byte[] originalAssembly = GenerateAssemblyWithResources(resources);

        ValidateEmbeddedResources(originalAssembly, resources);
    }

    [Fact]
    public void RoundTrip_Assembly_WithManagedResourcesPayload()
    {
        var resourceEntries = new Dictionary<string, object>
        {
            { "StringResource", "Hello, World!" },
            { "IntResource", 42 },
            { "ByteArrayResource", new byte[] { 1, 2, 3, 4, 5 } },
            { "UnicodeString", "こんにちは世界" },
            { "EmptyString", "" }
        };

        byte[] managedResourceData = CreateManagedResource(resourceEntries);

        var resources = new (string Name, byte[] Data)[]
        {
            ("TestResources.resources", managedResourceData)
        };

        byte[] originalAssembly = GenerateAssemblyWithResources(resources);

        ValidateManagedResources(originalAssembly, "TestResources.resources", resourceEntries);
    }

    [Fact]
    public void RoundTrip_Assembly_WithMixedResources()
    {
        var resourceEntries = new Dictionary<string, object>
        {
            { "MixedString", "Test Data" },
            { "MixedInt", 123 }
        };

        byte[] managedResourceData = CreateManagedResource(resourceEntries);

        var resources = new (string Name, byte[] Data)[]
        {
            ("MixedResources.resources", managedResourceData),
            ("BinaryResource1", new byte[] { 0x01, 0x02, 0x03 }),
            ("BinaryResource2", new byte[] { 0xFF, 0xFE, 0xFD }),
            ("EmptyResource", Array.Empty<byte>())
        };

        byte[] originalAssembly = GenerateAssemblyWithResources(resources);

        ValidateManagedResources(originalAssembly, "MixedResources.resources", resourceEntries);
        ValidateEmbeddedResources(originalAssembly, resources);
    }

    private static byte[] GenerateAssemblyWithResources((string Name, byte[] Data)[] resources)
    {
        var resourceDescriptions = new List<ResourceDescription>();
        var streamsToDispose = new List<MemoryStream>();

        try
        {
            foreach (var (name, data) in resources)
            {
                var memoryStream = new MemoryStream(data, writable: false);
                streamsToDispose.Add(memoryStream);
                resourceDescriptions.Add(new ResourceDescription(
                    resourceName: name,
                    dataProvider: () => memoryStream,
                    isPublic: true));
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(@"
            public class TestClass
            {
                public static int Main()
                {
                    return 42;
                }
            }
        ");

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var assemblyStream = new MemoryStream();
            EmitResult result = compilation.Emit(
                peStream: assemblyStream,
                manifestResources: resourceDescriptions);

            Assert.True(result.Success, "Compilation failed: " + string.Join(", ", result.Diagnostics));

            return assemblyStream.ToArray();
        }
        finally
        {
            foreach (var stream in streamsToDispose)
            {
                stream.Dispose();
            }
        }
    }

    private static byte[] CreateManagedResource(Dictionary<string, object> entries)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new ResourceWriter(memoryStream))
        {
            foreach (var kvp in entries)
            {
                writer.AddResource(kvp.Key, kvp.Value);
            }
        }

        return memoryStream.ToArray();
    }

    private static void ValidateEmbeddedResources(byte[] assemblyBytes, (string Name, byte[] Data)[] expectedResources)
    {
        using var peReader = new PEReader(ImmutableArray.Create(assemblyBytes));
        MetadataReader reader = peReader.GetMetadataReader();

        var actualResources = new Dictionary<string, byte[]>();

        foreach (ManifestResourceHandle handle in reader.ManifestResources)
        {
            ManifestResource resource = reader.GetManifestResource(handle);
            string name = reader.GetString(resource.Name);

            if (resource.Implementation.IsNil)
            {
                PEMemoryBlock resourceDirectory = peReader.GetSectionData(peReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
                var blobReader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                int length = blobReader.ReadInt32();
                byte[] data = blobReader.ReadBytes(length);

                actualResources[name] = data;
            }
        }

        Assert.Equal(expectedResources.Length, actualResources.Count);

        foreach (var (name, expectedData) in expectedResources)
        {
            Assert.True(actualResources.ContainsKey(name), $"Resource '{name}' not found in round-tripped assembly");
            byte[] actualData = actualResources[name];
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal(expectedData, actualData);
        }
    }

    private static void ValidateManagedResources(byte[] assemblyBytes, string resourceName, Dictionary<string, object> expectedEntries)
    {
        using var peReader = new PEReader(ImmutableArray.Create(assemblyBytes));
        MetadataReader metadataReader = peReader.GetMetadataReader();

        ManifestResourceHandle? resourceHandle = null;

        foreach (ManifestResourceHandle handle in metadataReader.ManifestResources)
        {
            ManifestResource resource = metadataReader.GetManifestResource(handle);
            if (metadataReader.GetString(resource.Name) == resourceName)
            {
                resourceHandle = handle;
                break;
            }
        }

        Assert.True(resourceHandle.HasValue, $"Managed resource '{resourceName}' not found");

        ManifestResource managedResource = metadataReader.GetManifestResource(resourceHandle.Value);
        PEMemoryBlock resourceDirectory = peReader.GetSectionData(peReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
        var blobReader = resourceDirectory.GetReader((int)managedResource.Offset, resourceDirectory.Length - (int)managedResource.Offset);
        int length = blobReader.ReadInt32();
        byte[] resourceData = blobReader.ReadBytes(length);

        using var resourceStream = new MemoryStream(resourceData);
        using var resourceReader = new ResourceReader(resourceStream);

        var actualEntries = new Dictionary<string, object>();
        foreach (System.Collections.DictionaryEntry entry in resourceReader)
        {
            actualEntries[(string)entry.Key] = entry.Value;
        }

        Assert.Equal(expectedEntries.Count, actualEntries.Count);

        foreach (var kvp in expectedEntries)
        {
            Assert.True(actualEntries.ContainsKey(kvp.Key), $"Resource entry '{kvp.Key}' not found");

            object actualValue = actualEntries[kvp.Key];
            object expectedValue = kvp.Value;

            if (expectedValue is byte[] expectedBytes)
            {
                Assert.True(actualValue is byte[], $"Expected byte array for '{kvp.Key}'");
                Assert.Equal(expectedBytes, (byte[])actualValue);
            }
            else
            {
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }
}
