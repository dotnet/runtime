// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        using var holder = new NativeStringHolder();
        int hr = dbi.GetAppDomainFullName(appDomain, holder.Ptr);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.False(string.IsNullOrEmpty(holder.Value), "AppDomain name should not be empty");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetModuleForAssembly_ReturnsNonNullModule(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        ILoader loader = Target.Contracts.Loader;

        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        var modules = loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ModuleHandle module in modules)
        {
            TargetPointer assemblyPtr = loader.GetAssembly(module);
            TargetPointer expectedModulePtr = loader.GetModule(module);

            ulong resultModule;
            int hr = dbi.GetModuleForAssembly(assemblyPtr.Value, &resultModule);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.NotEqual(0UL, resultModule);
            Assert.Equal(expectedModulePtr.Value, resultModule);
            break;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetTypeHandle_ReturnsMethodTableForTypeDef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        // Get the well-known System.Object MethodTable
        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle objectHandle = rts.GetTypeHandle(objectMT);

        // Get its TypeDef token and module pointer
        uint token = rts.GetTypeDefToken(objectHandle);
        Assert.Equal(0x02000000u, token & 0xFF000000u);

        TargetPointer modulePtr = rts.GetModule(objectHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);

        // DacDbi GetTypeHandle should resolve the same token back to the same MethodTable
        ulong result;
        int hr = dbi.GetTypeHandle(modulePtr.Value, token, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(objectMT.Value, result);
    }
}
