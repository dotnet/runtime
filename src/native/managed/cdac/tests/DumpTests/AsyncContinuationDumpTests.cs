// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for async continuation support in the RuntimeTypeSystem contract.
/// Uses the AsyncContinuation debuggee dump, which runs an async2 method to trigger
/// continuation MethodTable creation.
/// </summary>
public class AsyncContinuationDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "AsyncContinuation";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Continuation support is not available in .NET 10")]
    public void ContinuationMethodTable_IsNonNull(TestConfiguration config)
    {
        InitializeDumpTest(config);

        TargetPointer continuationMTGlobal = Target.ReadGlobalPointer("ContinuationMethodTable");
        TargetPointer continuationMT = Target.ReadPointer(continuationMTGlobal);
        Assert.NotEqual(TargetPointer.Null, continuationMT);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Continuation support is not available in .NET 10")]
    public void ContinuationBaseClass_IsNotContinuation(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        // The ContinuationMethodTable global points to the Continuation base class itself.
        // IsContinuationWithoutMetadata checks if a type's parent is the Continuation base class
        // and its EEClass matches the singleton continuation EEClass,
        // so the base class itself is NOT considered a continuation (its parent is Object).
        TargetPointer continuationMTGlobal = Target.ReadGlobalPointer("ContinuationMethodTable");
        TargetPointer continuationMT = Target.ReadPointer(continuationMTGlobal);
        Assert.NotEqual(TargetPointer.Null, continuationMT);

        ITypeHandle handle = rts.GetTypeHandle(continuationMT);
        Assert.False(rts.IsContinuationWithoutMetadata(handle));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Continuation support is not available in .NET 10")]
    public void ObjectMethodTable_IsNotContinuation(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        ITypeHandle objectHandle = rts.GetTypeHandle(objectMT);
        Assert.False(rts.IsContinuationWithoutMetadata(objectHandle));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Continuation support is not available in .NET 10")]
    public void ThreadLocalContinuation_IsContinuation(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IThread threadContract = Target.Contracts.Thread;
        IManagedTypeSource mts = Target.Contracts.ManagedTypeSource;

        const string AsyncDispatcherInfoFqn = "System.Runtime.CompilerServices.AsyncDispatcherInfo";
        const string TCurrentFieldName = "t_current";

        // Walk all threads and locate one whose t_current points at a continuation.
        ThreadStoreData threadStore = threadContract.GetThreadStoreData();
        TargetPointer threadPtr = threadStore.FirstThread;

        ulong continuationAddress = 0;
        while (threadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(threadPtr);

            if (mts.TryGetThreadStaticFieldAddress(AsyncDispatcherInfoFqn, TCurrentFieldName, threadPtr, out TargetPointer tCurrentSlot))
            {
                TargetPointer tCurrent = Target.ReadPointer(tCurrentSlot);
                if (tCurrent != TargetPointer.Null)
                {
                    // AsyncDispatcherInfo layout:
                    //   offset 0:            AsyncDispatcherInfo* Next
                    //   offset PointerSize:  Continuation? NextContinuation  (object reference)
                    TargetPointer nextContinuation = Target.ReadPointer(
                        tCurrent.Value + (ulong)Target.PointerSize);

                    if (nextContinuation != TargetPointer.Null)
                    {
                        continuationAddress = nextContinuation.Value;
                        break;
                    }
                }
            }

            threadPtr = threadData.NextThread;
        }

        Assert.NotEqual(0UL, continuationAddress);

        // 4. Verify the object's MethodTable is a continuation subtype via the cDAC.
        TargetPointer objMT = Target.Contracts.Object.GetMethodTableAddress(
            new TargetPointer(continuationAddress));
        ITypeHandle handle = rts.GetTypeHandle(objMT);
        Assert.True(rts.IsContinuationWithoutMetadata(handle));
    }
}
