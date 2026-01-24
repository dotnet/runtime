// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Resources;
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

    private static readonly Guid s_guid = new Guid("97F4DBD4-F6D1-4FAD-91B3-1001F92068E5");
    private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

    private static byte[] GenerateAssemblyWithResources((string Name, byte[] Data)[] resources)
    {
        var metadataBuilder = new MetadataBuilder();
        var ilBuilder = new BlobBuilder();

        // Build metadata
        metadataBuilder.AddModule(
            0,
            metadataBuilder.GetOrAddString("TestAssembly.dll"),
            metadataBuilder.GetOrAddGuid(s_guid),
            default,
            default);

        metadataBuilder.AddAssembly(
            metadataBuilder.GetOrAddString("TestAssembly"),
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKey: default,
            flags: default,
            hashAlgorithm: AssemblyHashAlgorithm.Sha1);

        // Add mscorlib reference
        var mscorlibAssemblyRef = metadataBuilder.AddAssemblyReference(
            name: metadataBuilder.GetOrAddString("mscorlib"),
            version: new Version(4, 0, 0, 0),
            culture: default,
            publicKeyOrToken: metadataBuilder.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
            flags: default,
            hashValue: default);

        var systemObjectTypeRef = metadataBuilder.AddTypeReference(
            mscorlibAssemblyRef,
            metadataBuilder.GetOrAddString("System"),
            metadataBuilder.GetOrAddString("Object"));

        // Build managed resources
        var resourcesBuilder = new BlobBuilder();
        var resourceOffsets = new Dictionary<string, uint>();

        foreach (var (name, data) in resources)
        {
            resourceOffsets[name] = (uint)resourcesBuilder.Count;
            resourcesBuilder.WriteInt32(data.Length);
            resourcesBuilder.WriteBytes(data);
        }

        // Add ManifestResource entries
        foreach (var (name, _) in resources)
        {
            metadataBuilder.AddManifestResource(
                ManifestResourceAttributes.Public,
                metadataBuilder.GetOrAddString(name),
                default,
                resourceOffsets[name]);
        }

        // Create simple method signature and IL
        var parameterlessCtorSignature = new BlobBuilder();
        new BlobEncoder(parameterlessCtorSignature)
            .MethodSignature(isInstanceMethod: true)
            .Parameters(0, returnType => returnType.Void(), parameters => { });

        var parameterlessCtorBlobIndex = metadataBuilder.GetOrAddBlob(parameterlessCtorSignature);

        var objectCtorMemberRef = metadataBuilder.AddMemberReference(
            systemObjectTypeRef,
            metadataBuilder.GetOrAddString(".ctor"),
            parameterlessCtorBlobIndex);

        var mainSignature = new BlobBuilder();
        new BlobEncoder(mainSignature)
            .MethodSignature()
            .Parameters(1,
                returnType => returnType.Type().Int32(),
                parameters => parameters.AddParameter().Type().SZArray().String());

        var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);
        var codeBuilder = new BlobBuilder();

        // .ctor IL
        var ctorIl = new InstructionEncoder(codeBuilder);
        ctorIl.LoadArgument(0);
        ctorIl.Call(objectCtorMemberRef);
        ctorIl.OpCode(ILOpCode.Ret);
        int ctorBodyOffset = methodBodyStream.AddMethodBody(ctorIl);
        codeBuilder.Clear();

        // Main IL
        var mainIl = new InstructionEncoder(codeBuilder);
        mainIl.LoadConstantI4(42);
        mainIl.OpCode(ILOpCode.Ret);
        int mainBodyOffset = methodBodyStream.AddMethodBody(mainIl);
        codeBuilder.Clear();

        var mainMethodDef = metadataBuilder.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            metadataBuilder.GetOrAddString("Main"),
            metadataBuilder.GetOrAddBlob(mainSignature),
            mainBodyOffset,
            parameterList: default);

        var ctorDef = metadataBuilder.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            metadataBuilder.GetOrAddString(".ctor"),
            parameterlessCtorBlobIndex,
            ctorBodyOffset,
            parameterList: default);

        metadataBuilder.AddTypeDefinition(
            default,
            default,
            metadataBuilder.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList: mainMethodDef);

        metadataBuilder.AddTypeDefinition(
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
            metadataBuilder.GetOrAddString("TestAssembly"),
            metadataBuilder.GetOrAddString("TestClass"),
            systemObjectTypeRef,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList: mainMethodDef);

        // Build PE
        using var peStream = new MemoryStream();
        WritePEImage(peStream, metadataBuilder, ilBuilder, mainMethodDef, resourcesBuilder);
        return peStream.ToArray();
    }

    private static void WritePEImage(
        Stream peStream,
        MetadataBuilder metadataBuilder,
        BlobBuilder ilBuilder,
        MethodDefinitionHandle entryPointHandle,
        BlobBuilder? managedResources = null)
    {
        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: Characteristics.Dll);

        var peBuilder = new ManagedPEBuilder(
            peHeaderBuilder,
            new MetadataRootBuilder(metadataBuilder),
            ilBuilder,
            managedResources: managedResources,
            entryPoint: entryPointHandle,
            flags: CorFlags.ILOnly,
            deterministicIdProvider: content => s_contentId);

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        peBlob.WriteContentTo(peStream);
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

                if (resource.Offset < 0 || resource.Offset > int.MaxValue)
                {
                    throw new BadImageFormatException($"Resource offset out of range: {resource.Offset}");
                }

                int offset = (int)resource.Offset;
                var blobReader = resourceDirectory.GetReader(offset, resourceDirectory.Length - offset);
                int length = blobReader.ReadInt32();

                if (length < 0 || length > blobReader.RemainingBytes)
                {
                    throw new BadImageFormatException($"Invalid resource length: {length}");
                }

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

        if (managedResource.Offset < 0 || managedResource.Offset > int.MaxValue)
        {
            throw new BadImageFormatException($"Resource offset out of range: {managedResource.Offset}");
        }

        int offset = (int)managedResource.Offset;
        var blobReader = resourceDirectory.GetReader(offset, resourceDirectory.Length - offset);
        int length = blobReader.ReadInt32();

        if (length < 0 || length > blobReader.RemainingBytes)
        {
            throw new BadImageFormatException($"Invalid resource length: {length}");
        }

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
