// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for GetStackReferences / WalkStackReferences.
/// Verifies that the cDAC can enumerate GC references on the managed stack.
/// </summary>
public class StackReferenceDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_ReturnsWithoutThrowing(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.NotNull(refs);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_RefsHaveValidSourceInfo(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        foreach (StackReferenceData r in refs)
        {
            Assert.True(r.Source != TargetPointer.Null, "Stack reference should have a non-null Source (IP or Frame address)");
            Assert.True(r.StackPointer != TargetPointer.Null, "Stack reference should have a non-null StackPointer");
        }
    }
}

/// <summary>
/// Tests using the GCRoots debuggee, which keeps objects alive on the stack
/// via GC.KeepAlive before crashing. Should produce stack references to those objects.
/// </summary>
public class GCRootsStackReferenceDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_FindsRefsOnMainThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.NotNull(refs);
        Assert.True(refs.Count > 0,
            "Expected GCRoots Main thread to have at least one stack reference (objects kept alive via GC.KeepAlive)");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_RefsPointToValidObjects(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);

        int validObjectCount = 0;
        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

            // Each non-null object reference should point to a valid managed object.
            // The object's method table pointer (first pointer-sized field) should be non-null.
            try
            {
                TargetPointer methodTable = Target.ReadPointer(r.Object);
                if (methodTable != TargetPointer.Null)
                    validObjectCount++;
            }
            catch
            {
                // Some refs may be interior pointers or otherwise unreadable
            }
        }

        Assert.True(validObjectCount > 0,
            $"Expected at least one stack ref pointing to a valid object (total refs: {refs.Count})");
    }
}

/// <summary>
/// Tests using the PInvokeStub debuggee, which crashes inside native code
/// during a P/Invoke. The managed stack has an InlinedCallFrame (non-frameless).
/// Frame::GcScanRoots needs to be implemented for these refs to be reported.
/// These tests are expected to fail until frame-gc-scan-roots is implemented.
/// </summary>
public class PInvokeFrameStackReferenceDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "PInvokeStub";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void WalkStackReferences_PInvokeThread_ReturnsWithoutThrowing(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.NotNull(refs);
    }
}
