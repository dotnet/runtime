// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl loader, assembly, and module methods.
/// Uses the MultiModule debuggee (full dump), which loads assemblies from multiple ALCs.
/// </summary>
public class DacDbiLoaderDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetAppDomainFullName_ReturnsNonEmpty(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);

        var holder = new TestStringHolder();
        int hr = dbi.GetAppDomainFullName(appDomain, holder);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.False(string.IsNullOrEmpty(holder.Value), "AppDomain name should not be empty");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetModuleSimpleName_ReturnsNonEmpty(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        int checkedCount = 0;
        foreach (ModuleHandle module in modules)
        {
            TargetPointer moduleAddr = loader.GetModule(module);
            var holder = new TestStringHolder();
            int hr = dbi.GetModuleSimpleName(moduleAddr, holder);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.False(string.IsNullOrEmpty(holder.Value), $"Module name should not be empty for module at {moduleAddr}");
            checkedCount++;
        }
        Assert.True(checkedCount > 0, "Should have checked at least one module");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetAssemblyFromDomainAssembly_CrossValidate(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer moduleAddr = loader.GetModule(module);
            TargetPointer expectedAssembly = loader.GetAssembly(module);

            ulong assembly;
            int hr = dbi.GetAssemblyFromDomainAssembly(moduleAddr, &assembly);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(expectedAssembly.Value, assembly);
            break;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetModuleForDomainAssembly_CrossValidate(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer expectedModule = loader.GetModule(module);

            ulong result;
            int hr = dbi.GetModuleForDomainAssembly(expectedModule, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(expectedModule.Value, result);
            break;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetDomainAssemblyFromModule_IsIdentity(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer moduleAddr = loader.GetModule(module);

            ulong domainAssembly;
            int hr = dbi.GetDomainAssemblyFromModule(moduleAddr, &domainAssembly);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(moduleAddr.Value, domainAssembly);
            break;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void EnumerateAssembliesInAppDomain_HasAssemblies(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);

        int count = 0;
        delegate* unmanaged<ulong, nint, void> callback = &CountCallback;
        int hr = dbi.EnumerateAssembliesInAppDomain(appDomain, (nint)callback, (nint)(&count));
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(count > 0, "Should have at least one assembly");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void EnumerateModulesInAssembly_ReturnsOneModule(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        IEnumerable<ModuleHandle> modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer assemblyAddr = loader.GetAssembly(module);
            int count = 0;
            delegate* unmanaged<ulong, nint, void> callback = &CountCallback;
            int hr = dbi.EnumerateModulesInAssembly(assemblyAddr, (nint)callback, (nint)(&count));
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(1, count);
            break;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void CountCallback(ulong addr, nint userData)
    {
        (*(int*)userData)++;
    }

    /// <summary>
    /// Managed implementation of <see cref="IStringHolder"/> for testing string-returning
    /// DacDbiImpl methods. Works because DacDbiImpl is called directly (not via COM interop),
    /// so the interface parameter is a regular managed interface reference.
    /// </summary>
    private class TestStringHolder : IStringHolder
    {
        public string? Value { get; private set; }

        public int AssignCopy(string psz)
        {
            Value = psz;

            return System.HResults.S_OK;
        }
    }
}
