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
public abstract class LoaderDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    [ConditionalFact]
    public void Loader_CanGetRootAssembly()
    {
        ILoader loader = Target.Contracts.Loader;
        Assert.NotNull(loader);

        TargetPointer rootAssembly = loader.GetRootAssembly();
        Assert.NotEqual(TargetPointer.Null, rootAssembly);
    }

    [ConditionalFact]
    public void Loader_RootAssemblyHasModule()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        TargetPointer modulePtr = loader.GetModule(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);
    }

    [ConditionalFact]
    public void Loader_CanGetModulePath()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        string path = loader.GetPath(moduleHandle);
        Assert.NotNull(path);
        Assert.NotEmpty(path);
    }

    [ConditionalFact]
    public void Loader_AppDomainHasFriendlyName()
    {
        ILoader loader = Target.Contracts.Loader;
        string name = loader.GetAppDomainFriendlyName();
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [ConditionalFact]
    public void Loader_GlobalLoaderAllocatorIsValid()
    {
        ILoader loader = Target.Contracts.Loader;
        TargetPointer globalLA = loader.GetGlobalLoaderAllocator();
        Assert.NotEqual(TargetPointer.Null, globalLA);
    }

    [ConditionalFact]
    public void Loader_RootModuleHasFileName()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        string fileName = loader.GetFileName(moduleHandle);
        Assert.NotNull(fileName);
        Assert.NotEmpty(fileName);
        Assert.Contains("MultiModule", fileName);
    }

    [ConditionalFact]
    public void Loader_RootModuleIsNotDynamic()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        Assert.False(loader.IsDynamic(moduleHandle));
    }

    [ConditionalFact]
    public void Loader_RootModuleHasLoaderAllocator()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        TargetPointer la = loader.GetLoaderAllocator(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, la);
    }

    [ConditionalFact]
    public void Loader_RootModuleHasILBase()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        TargetPointer ilBase = loader.GetILBase(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, ilBase);
    }
}

public class LoaderDumpTests_Local : LoaderDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class LoaderDumpTests_Net10 : LoaderDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
