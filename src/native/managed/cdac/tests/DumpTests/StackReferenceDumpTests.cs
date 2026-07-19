// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for WalkStackReferences.
/// Uses the InitializeDumpTest overload to target different debuggees per test.
/// </summary>
public class StackReferenceDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";

    // --- StackWalk debuggee: basic stack walk ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_ReturnsWithoutThrowing(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "heap");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        Assert.NotNull(refs);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_RefsHaveValidSourceInfo(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "heap");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        foreach (StackReferenceData r in refs)
        {
            Assert.True(r.Source != TargetPointer.Null, "Stack reference should have a non-null Source (IP or Frame address)");
            Assert.True(r.StackPointer != TargetPointer.Null, "Stack reference should have a non-null StackPointer");
        }
    }

    // --- GCRoots debuggee: objects kept alive on stack ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GCRoots_WalkStackReferences_FindsRefs(TestConfiguration config)
    {
        InitializeDumpTest(config, "GCRoots", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        Assert.NotNull(refs);
        Assert.True(refs.Count > 0,
            "Expected GCRoots Main thread to have at least one stack reference (objects kept alive via GC.KeepAlive)");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GCRoots_RefsPointToValidObjects(TestConfiguration config)
    {
        InitializeDumpTest(config, "GCRoots", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);

        int validObjectCount = 0;
        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

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

    // --- NestedException debuggee: in-flight exception objects reported as roots ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnArch("x86", "GCInfo decoder does not support x86")]
    public void NestedException_InFlightExceptionsReportedAsRoots(TestConfiguration config)
    {
        InitializeDumpTest(config, "NestedException", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IException exceptionContract = Target.Contracts.Exception;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        // FirstNestedException is the previous tracker on the thread's ExInfo chain
        // (Thread_1.GetThreadData sets it from currentExInfo.PreviousNestedInfo), so it
        // enumerates the superseded / nested in-flight exceptions. These are held only by the
        // runtime's exception-tracking chain (the nested FileNotFoundException lives on the heap
        // as InvalidOperationException.InnerException, not as a stack local), so the ExInfo scan in
        // WalkStackReferences is what surfaces them as roots.
        HashSet<ulong> expected = new();
        HashSet<ulong> seenTrackers = new();
        TargetPointer exInfo = crashingThread.FirstNestedException;
        while (exInfo != TargetPointer.Null && seenTrackers.Add(exInfo))
        {
            exceptionContract.GetNestedExceptionInfo(exInfo, out TargetPointer next, out TargetPointer thrownObjectSlot);
            TargetPointer obj = Target.ReadPointer(thrownObjectSlot);
            if (obj != TargetPointer.Null)
                expected.Add(obj);
            exInfo = next;
        }

        Assert.True(expected.Count >= 1,
            $"NestedException debuggee should hold at least one superseded exception on its ExInfo chain; found {expected.Count}");

        // WalkStackReferences must surface every in-flight exception object as a stack reference,
        // reported with the Other source type (the ExInfo node is not a capital-F Frame).
        HashSet<ulong> reported = new();
        foreach (StackReferenceData r in stackWalk.WalkStackReferences(crashingThread, false))
        {
            if (r.Object == TargetPointer.Null)
                continue;
            if (expected.Contains(r.Object))
                Assert.Equal(StackSourceType.Other, r.SourceType);
            reported.Add(r.Object);
        }

        foreach (ulong exc in expected)
            Assert.True(reported.Contains(exc),
                $"Expected in-flight exception object 0x{exc:x} to be reported as a stack reference");
    }

    // --- GCProtect debuggee: GCFrame (GCPROTECT) protected objects reported as roots ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnArch("x86", "GCInfo decoder does not support x86")]
    public void GCProtect_GCFrameRootsAreReported(TestConfiguration config)
    {
        InitializeDumpTest(config, "GCProtect", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        // The debuggee crashes inside an AppDomain.AssemblyResolve handler the runtime invokes while
        // holding a GCPROTECT frame over the requesting Assembly reference. WalkStackReferences reports
        // each GCFrame-protected object with the GCFrame node address as its Source; the test walks the
        // thread's GCFrame chain and asserts a reported root's Source matches a node in that chain.
        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);

        // Enumerate the thread's GCFrame chain node addresses; each reported GCFrame root carries the
        // GCFrame node address as its Source (UpdateScanContext(frame: pGCFrame)).
        Target.TypeInfo gcFrameType = Target.GetTypeInfo("GCFrame");

        HashSet<ulong> gcFrameNodes = [];
        TargetPointer node = crashingThread.GCFrame;
        while (node != TargetPointer.Null)
        {
            if (!gcFrameNodes.Add(node))
                throw new InvalidOperationException($"Found a cycle when processing ThreadData.GCFrame list.");

            node = Target.ReadPointerField(node, gcFrameType, "Next");
        }

        Assert.True(gcFrameNodes.Count > 0, "GCProtect debuggee should have at least one live GCFrame");

        int gcFrameRoots = 0;
        foreach (StackReferenceData r in refs)
        {
            if (!gcFrameNodes.Contains(r.Source) || r.Object == TargetPointer.Null)
                continue;

            // GCFrame roots are reported with the Other source type.
            Assert.Equal(StackSourceType.Other, r.SourceType);

            // A real heap object held alive by a GCFrame: its MethodTable must be readable. A reported
            // GCFrame root can be an interior pointer or otherwise unreadable, so guard the read.
            try
            {
                TargetPointer methodTable = Target.ReadPointer(r.Object);
                if (methodTable != TargetPointer.Null)
                    gcFrameRoots++;
            }
            catch
            {
                // Interior pointer or otherwise unreadable slot; not a countable heap object.
            }
        }

        Assert.True(gcFrameRoots > 0,
            $"Expected at least one GCFrame (GCPROTECT) protected object to be reported as a stack reference (total refs: {refs.Count})");
    }

    // --- StackRefs debuggee: known objects on stack with verifiable content ---
    // These tests require Frame-based GC root scanning (ScanFrameRoots) which is not yet implemented.

    [Theory(Skip = "Requires Frame-based GC root scanning (ScanFrameRoots) — not yet implemented")]
    [MemberData(nameof(TestConfigurations))]
    public void StackRefs_FindsMarkerString(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackRefs", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IObject objectContract = Target.Contracts.Object;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodWithStackRefs");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        Assert.True(refs.Count > 0, "Expected at least one stack reference from MethodWithStackRefs");

        bool foundMarker = false;
        string expectedMarker = "cDAC-StackRefs-Marker-12345";

        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

            try
            {
                string value = objectContract.GetStringValue(r.Object);
                if (value == expectedMarker)
                {
                    foundMarker = true;
                    break;
                }
            }
            catch
            {
                // Not a string or not readable — skip
            }
        }

        Assert.True(foundMarker,
            $"Expected to find marker string '{expectedMarker}' among {refs.Count} stack references");
    }

    [Theory(Skip = "Requires Frame-based GC root scanning (ScanFrameRoots) — not yet implemented")]
    [MemberData(nameof(TestConfigurations))]
    public void StackRefs_FindsArrayReference(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackRefs", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IObject objectContract = Target.Contracts.Object;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodWithStackRefs");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        Assert.True(refs.Count > 0, "Expected at least one stack reference from MethodWithStackRefs");

        // Look for the int[] { 1, 2, 3, 4, 5 } array using the Object contract.
        bool foundArray = false;

        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

            try
            {
                TargetPointer dataStart = objectContract.GetArrayData(r.Object, out uint count, out _, out _);
                if (count != 5)
                    continue;

                int elem0 = Target.Read<int>(dataStart + sizeof(int) * 0);
                int elem1 = Target.Read<int>(dataStart + sizeof(int) * 1);
                int elem2 = Target.Read<int>(dataStart + sizeof(int) * 2);

                if (elem0 == 1 && elem1 == 2 && elem2 == 3)
                {
                    foundArray = true;
                    break;
                }
            }
            catch
            {
                // Not an array or not readable — skip
            }
        }

        Assert.True(foundArray,
            $"Expected to find int[]{{1,2,3,4,5}} among {refs.Count} stack references");
    }

    // --- PInvokeStub debuggee: Frame-based path ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    [SkipOnOS(IncludeOnly = "windows", Reason = "PInvokeStub debuggee uses msvcrt.dll (Windows only)")]
    public void PInvoke_WalkStackReferences_ReturnsWithoutThrowing(TestConfiguration config)
    {
        InitializeDumpTest(config, "PInvokeStub", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "Main");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread, false);
        Assert.NotNull(refs);
    }
}
