// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
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
    protected override string DumpType => "full";

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
        // IsContinuation checks if a type's parent is the Continuation base class,
        // so the base class itself is NOT considered a continuation (its parent is Object).
        TargetPointer continuationMTGlobal = Target.ReadGlobalPointer("ContinuationMethodTable");
        TargetPointer continuationMT = Target.ReadPointer(continuationMTGlobal);
        Assert.NotEqual(TargetPointer.Null, continuationMT);

        TypeHandle handle = rts.GetTypeHandle(continuationMT);
        Assert.False(rts.IsContinuation(handle));
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
        TypeHandle objectHandle = rts.GetTypeHandle(objectMT);
        Assert.False(rts.IsContinuation(objectHandle));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Continuation support is not available in .NET 10")]
    public void ThreadLocalContinuation_IsContinuation(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ILoader loader = Target.Contracts.Loader;
        IThread threadContract = Target.Contracts.Thread;
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;

        // 1. Locate the AsyncDispatcherInfo type in System.Private.CoreLib.
        TargetPointer systemAssembly = loader.GetSystemAssembly();
        ModuleHandle coreLibModule = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);
        TypeHandle asyncDispatcherInfoHandle = rts.GetTypeByNameAndModule(
            "AsyncDispatcherInfo",
            "System.Runtime.CompilerServices",
            coreLibModule);
        Assert.True(asyncDispatcherInfoHandle.Address != 0,
            "Could not find AsyncDispatcherInfo type in CoreLib");

        // 2. Find the t_current field's offset within the non-GC thread statics block.
        //    Walk the FieldDescList to find the ThreadStatic field named "t_current".
        System.Reflection.Metadata.MetadataReader? md = ecmaMetadata.GetMetadata(coreLibModule);
        Assert.NotNull(md);

        TargetPointer fieldDescList = rts.GetFieldDescList(asyncDispatcherInfoHandle);
        ushort numStaticFields = rts.GetNumStaticFields(asyncDispatcherInfoHandle);
        ushort numThreadStaticFields = rts.GetNumThreadStaticFields(asyncDispatcherInfoHandle);
        ushort numInstanceFields = rts.GetNumInstanceFields(asyncDispatcherInfoHandle);

        // FieldDescList has instance fields first, then static fields.
        // Thread-static fields are among the static fields.
        uint tCurrentOffset = 0;
        bool foundField = false;
        int totalFields = numInstanceFields + numStaticFields;
        uint fieldDescSize = Target.GetTypeInfo(DataType.FieldDesc).Size!.Value;

        for (int i = numInstanceFields; i < totalFields; i++)
        {
            TargetPointer fieldDesc = fieldDescList + (ulong)(i * (int)fieldDescSize);
            if (!rts.IsFieldDescThreadStatic(fieldDesc))
                continue;

            uint memberDef = rts.GetFieldDescMemberDef(fieldDesc);
            var fieldDefHandle = (FieldDefinitionHandle)MetadataTokens.Handle((int)memberDef);
            var fieldDef = md.GetFieldDefinition(fieldDefHandle);
            string fieldName = md.GetString(fieldDef.Name);

            if (fieldName == "t_current")
            {
                tCurrentOffset = rts.GetFieldDescOffset(fieldDesc, fieldDef);
                foundField = true;
                break;
            }
        }

        Assert.True(foundField, $"Could not find t_current field. numStatic={numStaticFields} numThreadStatic={numThreadStaticFields} numInstance={numInstanceFields}");

        // 3. Walk all threads and read t_current at the discovered offset.
        ThreadStoreData threadStore = threadContract.GetThreadStoreData();
        TargetPointer threadPtr = threadStore.FirstThread;

        ulong continuationAddress = 0;
        while (threadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(threadPtr);

            TargetPointer nonGCBase = rts.GetNonGCThreadStaticsBasePointer(
                asyncDispatcherInfoHandle, threadPtr);

            if (nonGCBase != TargetPointer.Null)
            {
                TargetPointer tCurrent = Target.ReadPointer(nonGCBase + tCurrentOffset);

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
        TypeHandle handle = rts.GetTypeHandle(objMT);
        Assert.True(rts.IsContinuation(handle));
    }
}
