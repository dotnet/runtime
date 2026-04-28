// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract's RCW APIs.
/// Uses the RCW debuggee which creates both a plain and a contained
/// COM RCW with populated interface entry caches before crashing.
/// </summary>
public class RCWDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "RCW";

    /// <summary>
    /// Walks all strong GC handles and returns all RCW pointers found,
    /// paired with their <see cref="RCWData"/>.
    /// </summary>
    private List<(TargetPointer Rcw, RCWData Data)> FindAllRCWs()
    {
        IGC gcContract = Target.Contracts.GC;
        IObject objectContract = Target.Contracts.Object;
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        var results = new List<(TargetPointer, RCWData)>();

        foreach (HandleData handleData in gcContract.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            if (objectContract.GetBuiltInComData(objectAddress, out TargetPointer rcw, out _, out _)
                && rcw != TargetPointer.Null)
            {
                RCWData data = builtInCOM.GetRCWData(rcw);
                results.Add((rcw, data));
            }
        }

        return results;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public void GetRCWInterfaces_FindsRCWAndEnumeratesInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        var allRcws = FindAllRCWs();
        // Find the plain (non-contained) RCW
        (TargetPointer rcwPtr, _) = allRcws.First(r => !r.Data.IsContained);

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
    public void GetRCWData_PlainRCW_ReturnsExpectedData(TestConfiguration config)
    {
        InitializeDumpTest(config);

        var allRcws = FindAllRCWs();
        // Find the plain (non-contained) RCW
        (_, RCWData data) = allRcws.First(r => !r.Data.IsContained);

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

        // RefCount must be 1 — the debuggee held a live reference.
        Assert.True(data.RefCount == 1,
            $"Expected RefCount of 1, got {data.RefCount}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public void GetRCWData_ContainedRCW_IsMarkedContained(TestConfiguration config)
    {
        InitializeDumpTest(config);

        var allRcws = FindAllRCWs();
        // Find the contained RCW (created from ContainedGlobalInterfaceTable).
        // The GIT singleton doesn't support aggregation, so the runtime falls back
        // to containment when an extensible RCW is created for it.
        (_, RCWData data) = allRcws.First(r => r.Data.IsContained);

        Assert.True(data.IsContained);
        Assert.False(data.IsAggregated);

        // The contained RCW wraps a live COM object.
        Assert.NotEqual(TargetPointer.Null, data.IdentityPointer);

        // The managed object should be resolvable via the sync block index.
        Assert.NotEqual(TargetPointer.Null, data.ManagedObject);

        // The RCW is alive at the time of the crash.
        Assert.False(data.IsDisconnected);

        // RefCount must be 1 — the debuggee held a live reference.
        Assert.True(data.RefCount == 1,
            $"Expected RefCount of 1, got {data.RefCount}");
    }
}
