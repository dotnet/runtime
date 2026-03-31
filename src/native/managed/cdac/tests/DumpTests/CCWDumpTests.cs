// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the CCW (COM Callable Wrapper).
/// Uses the CCW debuggee dump, which creates COM callable wrappers (CCWs)
/// on Windows then crashes via FailFast.
/// All tests are skipped on non-Windows dumps because CCWs require Windows COM support.
/// </summary>
public class CCWDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CCW";
    protected override string DumpType => "full";

    /// <summary>
    /// Enumerates all strong GC handles from the dump, dereferences each one to get
    /// the managed object address, then uses the Object contract to find objects that
    /// have an active CCW. Returns a list of CCW pointers.
    /// </summary>
    private List<TargetPointer> GetCCWPointersFromHandles()
    {
        IGC gcContract = Target.Contracts.GC;
        IObject objectContract = Target.Contracts.Object;

        List<HandleData> strongHandles = gcContract.GetHandles([HandleType.Strong]);
        List<TargetPointer> ccwPtrs = [];

        foreach (HandleData handleData in strongHandles)
        {
            // Dereference the handle slot to get the object address.
            TargetPointer objPtr = Target.ReadPointer(handleData.Handle);
            if (objPtr == TargetPointer.Null)
                continue;

            bool hasCOM = objectContract.GetBuiltInComData(objPtr, out _, out TargetPointer ccwPtr, out _);
            if (hasCOM && ccwPtr != TargetPointer.Null)
                ccwPtrs.Add(ccwPtr);
        }

        return ccwPtrs;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void CCW_HasInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count >= 4, "Expected at least four objects with an active CCW from strong handles");

        int ccwsWithOneInterface = 0;
        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(ccwPtr).ToList();
            if (interfaces.Count == 1)
            {
                ccwsWithOneInterface++;
                // Non-slot-0 entries (from IComTestInterface) should have a valid MethodTable.
                Assert.Contains(interfaces, static i => i.MethodTable != TargetPointer.Null);
            }
        }

        Assert.True(ccwsWithOneInterface >= 3,
            $"Expected at least three CCWs with exactly one interface, got {ccwsWithOneInterface}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void CCW_InterfaceMethodTablesAreReadable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count > 0, "Expected at least one object with an active CCW from strong handles");

        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            foreach (COMInterfacePointerData iface in builtInCOM.GetCCWInterfaces(ccwPtr))
            {
                if (iface.MethodTable == TargetPointer.Null)
                    continue;

                // Verify the MethodTable is readable by resolving it to a TypeHandle.
                TypeHandle typeHandle = rts.GetTypeHandle(iface.MethodTable);
                Assert.False(typeHandle.IsNull,
                    $"Expected non-null TypeHandle for MethodTable 0x{iface.MethodTable:X} in CCW 0x{ccwPtr:X}");
                Assert.True(rts.GetBaseSize(typeHandle) > 0,
                    $"Expected positive base size for MethodTable 0x{iface.MethodTable:X} in CCW 0x{ccwPtr:X}");
            }
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void CCW_GetCCWData_FieldsAreConsistent(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count >= 4, "Expected at least four objects with an active CCW from strong handles");
        bool foundAggregated = false;
        bool foundNonAggregated = false;

        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            SimpleComCallWrapperData sccwData = builtInCOM.GetSimpleComCallWrapperData(ccwPtr);

            // A live CCW (not neutered) should have a positive ref count and a strong ref.
            Assert.False(sccwData.IsNeutered,
                $"Expected non-neutered CCW at 0x{ccwPtr:X}");
            Assert.True(sccwData.RefCount > 0,
                $"Expected positive ref count for CCW at 0x{ccwPtr:X}, got {sccwData.RefCount}");
            Assert.False(sccwData.IsHandleWeak,
                $"Expected strong handle for CCW at 0x{ccwPtr:X}");

            // None of the test objects extend a COM base class.
            Assert.False(sccwData.IsExtendsCOMObject,
                $"Expected IsExtendsCOMObject to be false for CCW at 0x{ccwPtr:X}");

            if (sccwData.IsAggregated)
            {
                foundAggregated = true;
                // Aggregated CCWs should have a non-null outer IUnknown.
                Assert.True(sccwData.OuterIUnknown != TargetPointer.Null,
                    $"Expected non-null OuterIUnknown for aggregated CCW at 0x{ccwPtr:X}");
            }
            else
            {
                foundNonAggregated = true;
                // Non-aggregated CCWs should have a null outer IUnknown.
                Assert.True(sccwData.OuterIUnknown == TargetPointer.Null,
                    $"Expected null OuterIUnknown for non-aggregated CCW at 0x{ccwPtr:X}");
            }

            // The handle should be populated and dereferenceable to a managed object.
            TargetPointer handle = builtInCOM.GetObjectHandle(ccwPtr);
            Assert.True(handle != TargetPointer.Null,
                $"Expected non-null handle for CCW at 0x{ccwPtr:X}");
            TargetPointer objectPtr = Target.ReadPointer(handle);
            Assert.True(objectPtr != TargetPointer.Null,
                $"Expected non-null object pointer for CCW at 0x{ccwPtr:X}");
        }

        // The debuggee creates both regular and aggregated CCWs.
        Assert.True(foundAggregated, "Expected at least one aggregated CCW in the dump");
        Assert.True(foundNonAggregated, "Expected at least one non-aggregated CCW in the dump");
    }
}
