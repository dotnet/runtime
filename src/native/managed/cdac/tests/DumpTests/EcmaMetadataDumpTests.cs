// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the EcmaMetadata contract.
/// Uses the MultiModule debuggee dump, which loads multiple assemblies.
/// </summary>
public class EcmaMetadataDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

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
}
