// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for WalkStackReferences.
/// Uses the InitializeDumpTest overload to target different debuggees per test.
/// </summary>
public class StackReferenceDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    // --- StackWalk debuggee: basic stack walk ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void WalkStackReferences_ReturnsWithoutThrowing(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");
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
        InitializeDumpTest(config, "StackWalk", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
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

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
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

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);

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

    // --- StackRefs debuggee: known objects on stack with verifiable content ---

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackRefs_FindsMarkerString(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackRefs", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodWithStackRefs");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.True(refs.Count > 0, "Expected at least one stack reference from MethodWithStackRefs");

        // Search for the marker string "cDAC-StackRefs-Marker-12345" among the object references.
        // A System.String in the CLR has: [MethodTable*][length:int32][chars...]
        // The chars start at offset (pointerSize + 4) from the object start.
        bool foundMarker = false;
        string expectedMarker = "cDAC-StackRefs-Marker-12345";

        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

            try
            {
                // Read the method table pointer to verify it's a valid object
                TargetPointer mt = Target.ReadPointer(r.Object);
                if (mt == TargetPointer.Null)
                    continue;

                // Read string length (int32 at offset pointerSize)
                int strLength = Target.Read<int>(r.Object + (ulong)Target.PointerSize);
                if (strLength <= 0 || strLength > 1024)
                    continue;

                // Read chars (UTF-16, starting at offset pointerSize + 4)
                byte[] charBytes = new byte[strLength * 2];
                Target.ReadBuffer(r.Object + (ulong)Target.PointerSize + 4, charBytes);
                string value = Encoding.Unicode.GetString(charBytes);

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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void StackRefs_FindsArrayReference(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackRefs", "full");
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodWithStackRefs");

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.True(refs.Count > 0, "Expected at least one stack reference from MethodWithStackRefs");

        // Look for the int[] { 1, 2, 3, 4, 5 } array.
        // An array in the CLR has: [MethodTable*][length:pointer-sized][elements...]
        // For int[], elements start at offset (pointerSize + pointerSize).
        bool foundArray = false;

        foreach (StackReferenceData r in refs)
        {
            if (r.Object == TargetPointer.Null)
                continue;

            try
            {
                TargetPointer mt = Target.ReadPointer(r.Object);
                if (mt == TargetPointer.Null)
                    continue;

                // Read array length
                ulong arrayLength = Target.ReadNUInt(r.Object + (ulong)Target.PointerSize).Value;
                if (arrayLength != 5)
                    continue;

                // Read int elements
                ulong elementsOffset = (ulong)Target.PointerSize + (ulong)Target.PointerSize;
                int elem0 = Target.Read<int>(r.Object + elementsOffset);
                int elem1 = Target.Read<int>(r.Object + elementsOffset + 4);
                int elem2 = Target.Read<int>(r.Object + elementsOffset + 8);

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

        IReadOnlyList<StackReferenceData> refs = stackWalk.WalkStackReferences(crashingThread);
        Assert.NotNull(refs);
    }
}
