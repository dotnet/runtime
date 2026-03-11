// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract's RCW APIs.
/// Uses the RCWInterfaces debuggee which creates a COM RCW and populates the
/// inline interface entry cache before crashing.
/// </summary>
public class RCWInterfacesDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "RCWInterfaces";
    protected override string DumpType => "full";

    /// <summary>
    /// Walks all strong GC handles and returns the first RCW pointer found.
    /// Returns <see cref="TargetPointer.Null"/> if no RCW-bearing object is found.
    /// </summary>
    private TargetPointer FindFirstRCW()
    {
        IGC gcContract = Target.Contracts.GC;
        IObject objectContract = Target.Contracts.Object;

        foreach (HandleData handleData in gcContract.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            if (objectContract.GetBuiltInComData(objectAddress, out TargetPointer rcw, out _, out _)
                && rcw != TargetPointer.Null)
            {
                return rcw;
            }
        }

        return TargetPointer.Null;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public void GetRCWInterfaces_FindsRCWAndEnumeratesInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        TargetPointer rcwPtr = FindFirstRCW();
        Assert.NotEqual(TargetPointer.Null, rcwPtr);

        // Assert that the cookie is not null
        TargetPointer cookie = builtInCOM.GetRCWContext(rcwPtr);
        Assert.NotEqual(TargetPointer.Null, cookie);

        // Call GetRCWInterfaces on the found RCW — must not throw
        List<(TargetPointer MethodTable, TargetPointer Unknown)> interfaces =
            builtInCOM.GetRCWInterfaces(rcwPtr).ToList();

        // The debuggee interacts with the RCW via IGlobalInterfaceTable / IUnknown,
        // so the entry cache should have at least one cached interface entry
        Assert.True(interfaces.Count >= 1,
            $"Expected at least one cached interface entry in the RCW, got {interfaces.Count}");

        // Every returned entry must have non-null MethodTable and Unknown pointers
        foreach ((TargetPointer mt, TargetPointer unk) in interfaces)
        {
            Assert.NotEqual(TargetPointer.Null, mt);
            Assert.NotEqual(TargetPointer.Null, unk);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public void GetRCWData_ReturnsExpectedData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        TargetPointer rcwPtr = FindFirstRCW();
        Assert.NotEqual(TargetPointer.Null, rcwPtr);

        RCWData data = builtInCOM.GetRCWData(rcwPtr);

        // The RCW wraps a live COM object, so its identity and vtable pointers must be valid.
        Assert.NotEqual(TargetPointer.Null, data.IdentityPointer);
        Assert.NotEqual(TargetPointer.Null, data.VTablePtr);

        // The debuggee pins the managed wrapper in a strong GC handle, so the managed
        // object should be resolvable via the sync block index.
        Assert.NotEqual(TargetPointer.Null, data.ManagedObject);

        // The RCW is alive at the time of the crash — it must not appear disconnected.
        Assert.False(data.IsDisconnected);

        // The StdGlobalInterfaceTable RCW is a plain wrapper, not an aggregation or containment.
        Assert.False(data.IsAggregated);
        Assert.False(data.IsContained);

        // RefCount must be positive — the debuggee held a live reference.
        Assert.True(data.RefCount > 0,
            $"Expected positive RefCount, got {data.RefCount}");
    }
}
