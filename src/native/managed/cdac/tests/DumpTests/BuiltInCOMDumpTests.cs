// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.Runtime;
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

    private const string ComObjectTypeName = "ComObject";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_HasInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        IObject objectContract = Target.Contracts.Object;

        using ClrRuntime runtime = CreateClrRuntime();
        List<ClrObject> comObjects = runtime.Heap
            .EnumerateObjects()
            .Where(static o => o.Type?.Name == ComObjectTypeName)
            .ToList();

        Assert.True(comObjects.Count > 0, "Expected at least one ComObject instance in the heap");

        bool foundCCW = false;
        foreach (ClrObject clrObj in comObjects)
        {
            bool hasCOM = objectContract.GetBuiltInComData(new TargetPointer(clrObj.Address), out _, out TargetPointer ccwPtr);
            if (!hasCOM || ccwPtr == TargetPointer.Null)
                continue;

            foundCCW = true;
            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(ccwPtr).ToList();

            Assert.True(interfaces.Count > 0,
                $"Expected at least one interface entry for CCW at 0x{ccwPtr:X}");

            // Non-slot-0 entries (from IComTestInterface) should have a valid MethodTable.
            Assert.Contains(interfaces, static i => i.MethodTable != TargetPointer.Null);
        }

        Assert.True(foundCCW, "Expected at least one ComObject with an active CCW in the dump");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_FirstSlotHasNullMethodTable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        IObject objectContract = Target.Contracts.Object;

        using ClrRuntime runtime = CreateClrRuntime();
        bool foundCCW = false;

        foreach (ClrObject clrObj in runtime.Heap.EnumerateObjects().Where(static o => o.Type?.Name == ComObjectTypeName))
        {
            bool hasCOM = objectContract.GetBuiltInComData(new TargetPointer(clrObj.Address), out _, out TargetPointer ccwPtr);
            if (!hasCOM || ccwPtr == TargetPointer.Null)
                continue;

            foundCCW = true;
            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(ccwPtr).ToList();
            Assert.True(interfaces.Count > 0, $"Expected at least one interface for CCW at 0x{ccwPtr:X}");

            // Slot 0 of the first wrapper is IUnknown/IDispatch; per the BuiltInCOM contract
            // it always yields a null MethodTable to match the legacy DAC behavior.
            Assert.Equal(TargetPointer.Null, interfaces[0].MethodTable);
        }

        Assert.True(foundCCW, "Expected at least one ComObject with an active CCW in the dump");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public void BuiltInCOM_CCW_RefCountIsPositive(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        IObject objectContract = Target.Contracts.Object;

        using ClrRuntime runtime = CreateClrRuntime();
        bool foundCCW = false;

        foreach (ClrObject clrObj in runtime.Heap.EnumerateObjects().Where(static o => o.Type?.Name == ComObjectTypeName))
        {
            bool hasCOM = objectContract.GetBuiltInComData(new TargetPointer(clrObj.Address), out _, out TargetPointer ccwPtr);
            if (!hasCOM || ccwPtr == TargetPointer.Null)
                continue;

            foundCCW = true;
            ulong refCount = builtInCOM.GetRefCount(ccwPtr);
            Assert.True(refCount > 0,
                $"Expected positive ref count for CCW at 0x{ccwPtr:X}, got {refCount}");
        }

        Assert.True(foundCCW, "Expected at least one ComObject with an active CCW in the dump");
    }
}
