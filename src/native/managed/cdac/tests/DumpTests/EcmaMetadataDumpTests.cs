// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the EcmaMetadata contract.
/// Uses the MultiModule debuggee dump, which loads multiple assemblies.
/// </summary>
public class EcmaMetadataDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void EcmaMetadata_RootModuleHasMetadataAddress(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;

        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        TargetSpan metadataSpan = ecmaMetadata.GetReadOnlyMetadataAddress(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, metadataSpan.Address);
        Assert.True(metadataSpan.Size > 0, "Expected metadata size > 0");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void EcmaMetadata_CanGetMetadataReader(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;

        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        MetadataReader? reader = ecmaMetadata.GetMetadata(moduleHandle);
        Assert.NotNull(reader);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void EcmaMetadata_MetadataReaderHasTypeDefs(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;

        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        MetadataReader? reader = ecmaMetadata.GetMetadata(moduleHandle);
        Assert.NotNull(reader);

        // The MultiModule debuggee defines at least the Program class
        int typeDefCount = 0;
        foreach (TypeDefinitionHandle tdh in reader.TypeDefinitions)
        {
            typeDefCount++;
        }
        Assert.True(typeDefCount > 0, "Expected at least one TypeDef in module metadata");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "DNMD metadata descriptors require the local runtime")]
    public void EcmaMetadata_DynamicModulePublishesDNMDDescriptor(TestConfiguration config)
    {
        InitializeDumpTest(config, "DNMDMetadata", "full");
        ILoader loader = Target.Contracts.Loader;
        IEcmaMetadata metadata = Target.Contracts.EcmaMetadata;

        ModuleHandle module = Assert.Single(loader.GetModuleHandles(
            loader.GetAppDomain(), AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution)
            .Where(handle => loader.IsDynamic(handle)));
        MetadataReader reader = Assert.IsType<MetadataReader>(metadata.GetMetadata(module));
        Assert.Contains(reader.TypeDefinitions, handle =>
            reader.GetString(reader.GetTypeDefinition(handle).Name) == "Sample");

        Target.TypeInfo peAssemblyType = Target.GetTypeInfo(DataType.PEAssembly);
        TargetPointer peAssembly = loader.GetPEAssembly(module);
        TargetPointer slot = Target.ReadPointer(
            peAssembly + (ulong)peAssemblyType.Fields["DNMDMetadataHandleSlot"].Offset);
        Assert.NotEqual(TargetPointer.Null, slot);

        TargetPointer context = Target.ReadPointer(slot);
        Target.TypeInfo contextType = Target.GetTypeInfo(DataType.DNMDContext);
        uint magic = Target.Read<uint>(context + (ulong)contextType.Fields["Magic"].Offset);
        Assert.Equal(Target.ReadGlobal<uint>(Constants.Globals.DNMDContextMagic), magic);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "DNMD metadata descriptors require the local runtime")]
    public void EcmaMetadata_DenseDeltaReadsLiveDNMDMetadata(TestConfiguration config)
    {
        InitializeDumpTest(config, "DNMDMetadata", "full");
        ILoader loader = Target.Contracts.Loader;
        ModuleHandle module = Assert.Single(loader.GetModuleHandles(
            loader.GetAppDomain(), AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution)
            .Where(handle => loader.GetSimpleName(handle) == "DNMDMetadata"));

        TargetPointer peAssembly = loader.GetPEAssembly(module);
        Assert.True(Target.Contracts.EcmaMetadata.HasReadWriteMetadata(peAssembly));

        byte[] image = Target.Contracts.EcmaMetadata.GetReadWriteMetadata(module);
        Assert.Equal(-1, image.AsSpan().IndexOf("#JTD"u8));
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(
            ImmutableCollectionsMarshal.AsImmutableArray(image));
        MetadataReader reader = provider.GetMetadataReader();
        Assert.Contains(reader.TypeReferences, handle =>
        {
            TypeReference type = reader.GetTypeReference(handle);
            return reader.GetString(type.Namespace) == "Example"
                && reader.GetString(type.Name) == "Added";
        });

        MetadataReader? cachedReader = Target.Contracts.EcmaMetadata.GetMetadata(module);
        Assert.NotNull(cachedReader);
        Assert.Contains(cachedReader.TypeReferences, handle =>
            cachedReader.GetString(cachedReader.GetTypeReference(handle).Name) == "Added");
    }
}
