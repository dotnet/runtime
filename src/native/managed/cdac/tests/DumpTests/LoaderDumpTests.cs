// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Loader contract.
/// Uses the MultiModule debuggee dump, which loads assemblies from multiple ALCs.
/// </summary>
public abstract class LoaderDumpTestsBase : DumpTestBase
{
    protected LoaderDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "MultiModule";

    [Fact]
    public void Loader_CanGetRootAssembly()
    {
        ILoader loader = Target.Contracts.Loader;
        Assert.NotNull(loader);

        TargetPointer rootAssembly = loader.GetRootAssembly();
        Assert.NotEqual(TargetPointer.Null, rootAssembly);
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    [SkipOnRuntimeVersion("local", "Assembly type does not include IsLoaded field in current contract descriptor")]
    public void Loader_RootAssemblyHasModule()
    {
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        TargetPointer modulePtr = loader.GetModule(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);
    }

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    [SkipOnRuntimeVersion("local", "Assembly type does not include IsLoaded field in current contract descriptor")]
    public void Loader_CanGetModulePath()
    {
        ILoader loader = Target.Contracts.Loader;
        TargetPointer rootAssembly = loader.GetRootAssembly();

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        string path = loader.GetPath(moduleHandle);
        Assert.NotNull(path);
        Assert.NotEmpty(path);
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
