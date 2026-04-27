// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

    private IEnumerable<ModuleHandle> GetAllModules()
    {
        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        return loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);
    }

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
    public unsafe void GetModuleForAssembly_ReturnsExpectedModule(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        ILoader loader = Target.Contracts.Loader;

        bool testedAtLeastOne = false;
        foreach (ModuleHandle module in GetAllModules())
        {
            TargetPointer assemblyPtr = loader.GetAssembly(module);
            TargetPointer expectedModulePtr = loader.GetModule(module);

            ulong resultModule;
            int hr = dbi.GetModuleForAssembly(assemblyPtr.Value, &resultModule);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.NotEqual(0UL, resultModule);
            Assert.Equal(expectedModulePtr.Value, resultModule);
            testedAtLeastOne = true;
        }
        Assert.True(testedAtLeastOne, "Expected at least one module in the dump");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetModuleForAssembly_InvalidAssembly(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong resultModule = ulong.MaxValue;
        int hr = dbi.GetModuleForAssembly(0, &resultModule);
        Assert.NotEqual(System.HResults.S_OK, hr);
        Assert.Equal(0UL, resultModule);
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetModuleData_ReturnsValidFields(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        ILoader loader = Target.Contracts.Loader;

        bool testedAtLeastOne = false;
        foreach (ModuleHandle module in GetAllModules())
        {
            TargetPointer moduleAddr = loader.GetModule(module);

            DacDbiModuleInfo data;
            int hr = dbi.GetModuleData(moduleAddr, &data);
            Assert.Equal(System.HResults.S_OK, hr);

            Assert.NotEqual(0UL, data.vmAssembly);
            Assert.NotEqual(0UL, data.vmPEAssembly);

            if (data.fIsDynamic == Interop.BOOL.FALSE && data.fInMemory == Interop.BOOL.FALSE)
            {
                Assert.NotEqual(0UL, data.pPEBaseAddress);
                Assert.NotEqual(0u, data.nPESize);
            }

            testedAtLeastOne = true;
        }
        Assert.True(testedAtLeastOne, "Expected at least one module in the dump");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsModuleMapped_ReturnsValidResult(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        ILoader loader = Target.Contracts.Loader;

        bool testedAtLeastOne = false;
        foreach (ModuleHandle module in GetAllModules())
        {
            TargetPointer moduleAddr = loader.GetModule(module);

            Interop.BOOL isMapped;
            int hr = dbi.IsModuleMapped(moduleAddr, &isMapped);

            Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE,
                $"Expected S_OK or S_FALSE, got 0x{hr:X8}");

            if (hr == System.HResults.S_OK)
            {
                Assert.True(isMapped == Interop.BOOL.TRUE || isMapped == Interop.BOOL.FALSE,
                    "isModuleMapped should be TRUE or FALSE");
            }

            testedAtLeastOne = true;
        }
        Assert.True(testedAtLeastOne, "Expected at least one module in the dump");
    }
}
