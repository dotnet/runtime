// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the ObjectiveCMarshal contract's tagged memory APIs.
/// Uses the ObjectiveCMarshal debuggee which creates a tracked reference object with
/// tagged memory allocated before crashing.
/// These tests only run on macOS dumps, as FEATURE_OBJCMARSHAL requires macOS.
/// </summary>
public class ObjectiveCMarshalDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ObjectiveCMarshal";
    protected override string DumpType => "full";

    /// <summary>
    /// Walks all strong GC handles and returns the addresses of tracked objects
    /// (those for which <see cref="IRuntimeTypeSystem.IsTrackedReferenceWithFinalizer"/> returns true).
    /// </summary>
    private List<TargetPointer> FindTrackedObjects()
    {
        IGC gcContract = Target.Contracts.GC;
        IObject objectContract = Target.Contracts.Object;
        IRuntimeTypeSystem rtsContract = Target.Contracts.RuntimeTypeSystem;
        var results = new List<TargetPointer>();

        foreach (HandleData handleData in gcContract.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            TargetPointer mt = objectContract.GetMethodTableAddress(objectAddress);
            if (mt == TargetPointer.Null)
                continue;

            TypeHandle typeHandle = rtsContract.GetTypeHandle(mt);
            if (rtsContract.IsTrackedReferenceWithFinalizer(typeHandle))
                results.Add(objectAddress);
        }

        return results;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "osx", Reason = "Objective-C interop (tagged memory) is only supported on macOS")]
    public void GetTaggedMemory_TrackedObject_HasTaggedMemory(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IObjectiveCMarshal objcContract = Target.Contracts.ObjectiveCMarshal;

        List<TargetPointer> trackedObjects = FindTrackedObjects();
        Assert.NotEmpty(trackedObjects);

        // Every tracked object found should have tagged memory
        foreach (TargetPointer objPtr in trackedObjects)
        {
            TargetPointer taggedMemory = objcContract.GetTaggedMemory(objPtr, out TargetNUInt size);
            Assert.NotEqual(TargetPointer.Null, taggedMemory);
            // Tagged memory size is always 2 * pointer size
            Assert.Equal(2ul * (ulong)Target.PointerSize, size.Value);
        }
    }
}
