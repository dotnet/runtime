// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Loader contract.
/// Uses the MultiModule debuggee dump, which loads assemblies from multiple ALCs.
/// </summary>
public class LoaderDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Loader_CanGetRootAssembly(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        Assert.NotNull(loader);

        TargetPointer rootAssembly = loader.GetRootAssembly();
        Assert.NotEqual(TargetPointer.Null, rootAssembly);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_RootAssemblyHasModule(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        TargetPointer modulePtr = loader.GetModule(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_CanGetModulePath(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        string path = loader.GetPath(moduleHandle);
        Assert.NotNull(path);
        Assert.NotEmpty(path);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Loader_AppDomainHasFriendlyName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        string name = loader.GetAppDomainFriendlyName();
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Loader_GlobalLoaderAllocatorIsValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer globalLA = loader.GetGlobalLoaderAllocator();
        Assert.NotEqual(TargetPointer.Null, globalLA);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_RootModuleHasFileName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        string fileName = loader.GetFileName(moduleHandle);
        Assert.NotNull(fileName);
        Assert.NotEmpty(fileName);
        Assert.Contains("MultiModule", fileName);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_RootModuleIsNotDynamic(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        Assert.False(loader.IsDynamic(moduleHandle));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_RootModuleHasLoaderAllocator(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        TargetPointer la = loader.GetLoaderAllocator(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, la);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public void Loader_RootModuleHasILBase(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        TargetPointer ilBase = loader.GetILBase(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, ilBase);
    }
}
