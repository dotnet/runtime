// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract.
/// Uses the CCWInterfaces debuggee dump, which creates COM callable wrappers (CCWs)
/// on Windows then crashes via FailFast.
/// All tests are skipped on non-Windows dumps because CCWs require Windows COM support.
/// </summary>
public class BuiltInCOMDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CCWInterfaces";
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
        List<TargetPointer> ccwPtrs = new();

        foreach (HandleData handleData in strongHandles)
        {
            // Dereference the handle slot to get the object address.
            TargetPointer objPtr = Target.ReadPointer(handleData.Handle);
            if (objPtr == TargetPointer.Null)
                continue;

            bool hasCOM = objectContract.GetBuiltInComData(objPtr, out _, out TargetPointer ccwPtr);
            if (hasCOM && ccwPtr != TargetPointer.Null)
                ccwPtrs.Add(ccwPtr);
        }

        return ccwPtrs;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_HasInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count > 0, "Expected at least one object with an active CCW from strong handles");

        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(ccwPtr).ToList();

            Assert.True(interfaces.Count > 0,
                $"Expected at least one interface entry for CCW at 0x{ccwPtr:X}");

            // Non-slot-0 entries (from IComTestInterface) should have a valid MethodTable.
            Assert.Contains(interfaces, static i => i.MethodTable != TargetPointer.Null);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_FirstSlotHasNullMethodTable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count > 0, "Expected at least one object with an active CCW from strong handles");

        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(ccwPtr).ToList();
            Assert.True(interfaces.Count > 0, $"Expected at least one interface for CCW at 0x{ccwPtr:X}");

            // Slot 0 of the first wrapper is IUnknown/IDispatch; per the BuiltInCOM contract
            // it always yields a null MethodTable to match the legacy DAC behavior.
            Assert.Equal(TargetPointer.Null, interfaces[0].MethodTable);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_RefCountIsPositive(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        List<TargetPointer> ccwPtrs = GetCCWPointersFromHandles();
        Assert.True(ccwPtrs.Count > 0, "Expected at least one object with an active CCW from strong handles");

        foreach (TargetPointer ccwPtr in ccwPtrs)
        {
            ulong refCount = builtInCOM.GetRefCount(ccwPtr);
            Assert.True(refCount > 0,
                $"Expected positive ref count for CCW at 0x{ccwPtr:X}, got {refCount}");
        }
    }
}
