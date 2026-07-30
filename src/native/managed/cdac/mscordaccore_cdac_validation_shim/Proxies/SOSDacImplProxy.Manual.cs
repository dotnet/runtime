// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Hand-written members of <see cref="SOSDacImplProxy"/> that the generated shape cannot express.
/// </summary>
internal sealed unsafe partial class SOSDacImplProxy
{
    /// <summary>
    /// Flush had no validation comparison in the pre-refactor cDAC; keep the shim as a pure
    /// production cDAC pass-through so cache management does not produce validation noise.
    /// </summary>
    int IXCLRDataProcess.Flush()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.Flush() : HResults.E_NOTIMPL;
        return hr;
    }

    /// <summary>
    /// The caller supplies the notification sink, so it must be invoked exactly once. The cDAC drives
    /// the caller's real sink through a recording proxy; the legacy DAC then drives a replaying proxy
    /// that compares each notification against what the cDAC produced without calling the caller
    /// again (a second round of OnCodeGenerated/OnException callbacks would be observable behavior
    /// the consumer never asked for).
    /// </summary>
    int IXCLRDataProcess.TranslateExceptionRecordToNotification(
        EXCEPTION_RECORD64* record,
        [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IXCLRDataExceptionNotification>))] IXCLRDataExceptionNotification notify)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ComObject notificationObject = (ComObject)(object)notify;

        try
        {
            bool supportsNotification2 = notify is IXCLRDataExceptionNotification2;
            bool supportsNotification3 = notify is IXCLRDataExceptionNotification3;
            bool supportsNotification4 = notify is IXCLRDataExceptionNotification4;
            bool supportsNotification5 = notify is IXCLRDataExceptionNotification5;

            RecordingExceptionNotification recording = new(notify);
            int hr = _cdacProcess is not null
                ? _cdacProcess.TranslateExceptionRecordToNotification(record, recording)
                : HResults.E_NOTIMPL;

            if (_legacyProcess is not null)
            {
                ReplayingExceptionNotification replaying = new(
                    supportsNotification2,
                    supportsNotification3,
                    supportsNotification4,
                    supportsNotification5);
                int hrLocal = _legacyProcess.TranslateExceptionRecordToNotification(record, replaying);
#if DEBUG
                Debug.ValidateHResult(hr, hrLocal);
                Debug.Assert(
                    ShimCall.Current?.ReplayedCallbackCount == ShimCall.Current?.RecordedCallbackCount,
                    $"cDAC raised {ShimCall.Current?.RecordedCallbackCount} notification(s), DAC raised {ShimCall.Current?.ReplayedCallbackCount}");
#endif
            }

            return hr;
        }
        finally
        {
            notificationObject.FinalRelease();
        }
    }


#if DEBUG
    private sealed class TraverseEhInfoRecordingContext
    {
        public delegate* unmanaged<uint, uint, DACEHInfo*, void*, int> Callback;
        public void* Token;
        public List<DACEHInfo> Elements { get; } = [];
        public bool ExpectAbort { get; set; }
        public uint? AbortIndex { get; set; }
    }

    private sealed class TraverseEhInfoExpected
    {
        public TraverseEhInfoExpected(List<DACEHInfo> elements, bool expectAbort, uint? abortIndex = null)
        {
            Elements = elements;
            ExpectAbort = expectAbort;
            AbortIndex = abortIndex;
        }

        public List<DACEHInfo> Elements { get; }
        public bool ExpectAbort { get; }
        public uint? AbortIndex { get; }
        public int CallbackCount { get; set; }
    }

    [UnmanagedCallersOnly]
    private static int RecordingTraverseEHInfoCallback(uint clauseIndex, uint totalClauses, DACEHInfo* pEHInfo, void* contextPtr)
    {
        var context = (TraverseEhInfoRecordingContext)GCHandle.FromIntPtr((nint)contextPtr).Target!;
        context.Elements.Add(*pEHInfo);
        int result = context.Callback(clauseIndex, totalClauses, pEHInfo, context.Token);
        if (result == 0)
        {
            context.ExpectAbort = true;
            context.AbortIndex = clauseIndex;
        }
        return result;
    }

    [UnmanagedCallersOnly]
    private static int TraverseEHInfoCallback(uint clauseIndex, uint totalClauses, DACEHInfo* pEHInfo, void* expectedEhInfo)
    {
        var expected = (TraverseEhInfoExpected)GCHandle.FromIntPtr((nint)expectedEhInfo).Target!;
        Debug.Assert(clauseIndex < totalClauses, $"Invalid clause index {clauseIndex} of {totalClauses}");
        if (clauseIndex < expected.Elements.Count)
        {
            DACEHInfo expectedEhClause = expected.Elements[(int)clauseIndex];
            Debug.Assert(pEHInfo->clauseType == expectedEhClause.clauseType, $"cDAC: {expectedEhClause.clauseType}, DAC: {pEHInfo->clauseType}");
            Debug.Assert(pEHInfo->tryStartOffset == expectedEhClause.tryStartOffset, $"cDAC: {expectedEhClause.tryStartOffset:x}, DAC: {pEHInfo->tryStartOffset:x}");
            Debug.Assert(pEHInfo->tryEndOffset == expectedEhClause.tryEndOffset, $"cDAC: {expectedEhClause.tryEndOffset:x}, DAC: {pEHInfo->tryEndOffset:x}");
            Debug.Assert(pEHInfo->handlerStartOffset == expectedEhClause.handlerStartOffset, $"cDAC: {expectedEhClause.handlerStartOffset:x}, DAC: {pEHInfo->handlerStartOffset:x}");
            Debug.Assert(pEHInfo->handlerEndOffset == expectedEhClause.handlerEndOffset, $"cDAC: {expectedEhClause.handlerEndOffset:x}, DAC: {pEHInfo->handlerEndOffset:x}");
            Debug.Assert(pEHInfo->isDuplicateClause == expectedEhClause.isDuplicateClause, $"cDAC: {expectedEhClause.isDuplicateClause}, DAC: {pEHInfo->isDuplicateClause}");
            Debug.Assert(pEHInfo->filterOffset == expectedEhClause.filterOffset, $"cDAC: {expectedEhClause.filterOffset:x}, DAC: {pEHInfo->filterOffset:x}");
            Debug.Assert(pEHInfo->isCatchAllHandler == expectedEhClause.isCatchAllHandler, $"cDAC: {expectedEhClause.isCatchAllHandler}, DAC: {pEHInfo->isCatchAllHandler}");
            Debug.Assert(pEHInfo->moduleAddr == expectedEhClause.moduleAddr, $"cDAC: {expectedEhClause.moduleAddr:x}, DAC: {pEHInfo->moduleAddr:x}");
            Debug.Assert(pEHInfo->mtCatch == expectedEhClause.mtCatch, $"cDAC: {expectedEhClause.mtCatch:x}, DAC: {pEHInfo->mtCatch:x}");
            Debug.Assert(pEHInfo->tokCatch == expectedEhClause.tokCatch, $"cDAC: {expectedEhClause.tokCatch:x}, DAC: {pEHInfo->tokCatch:x}");
        }
        else
        {
            Debug.Fail($"Received unexpected clause index {clauseIndex} of {totalClauses}");
        }

        expected.CallbackCount++;

        if (expected.ExpectAbort && expected.AbortIndex == clauseIndex)
        {
            return 0;
        }
        return 1;
    }

    private sealed class TraverseModuleMapRecordingContext
    {
        public delegate* unmanaged<uint, ulong, void*, void> Callback;
        public void* Token;
        public Dictionary<ulong, uint> ExpectedElements { get; } = [];
    }

    [UnmanagedCallersOnly]
    private static void RecordingTraverseModuleMapCallback(uint index, ulong moduleAddr, void* contextPtr)
    {
        var context = (TraverseModuleMapRecordingContext)GCHandle.FromIntPtr((nint)contextPtr).Target!;
        context.ExpectedElements[moduleAddr] = index;
        context.Callback(index, moduleAddr, context.Token);
    }

    [UnmanagedCallersOnly]
    private static void TraverseModuleMapCallback(uint index, ulong moduleAddr, void* expectedElements)
    {
        var expectedElementsDict = (Dictionary<ulong, uint>)GCHandle.FromIntPtr((nint)expectedElements).Target!;
        if (expectedElementsDict.TryGetValue(moduleAddr, out uint expectedIndex) && expectedIndex == index)
        {
            expectedElementsDict[default]++;
        }
        else
        {
            Debug.Assert(false, $"Unexpected module address {moduleAddr:x} at index {index}");
        }
    }

    private sealed class TraverseRCWCleanupListRecordingContext
    {
        public delegate* unmanaged<ulong, ulong, ulong, Interop.BOOL, void*, Interop.BOOL> Callback;
        public void* Token;
        public Dictionary<ulong, ulong> ExpectedElements { get; } = [];
    }

    [UnmanagedCallersOnly]
    private static Interop.BOOL RecordingTraverseRCWCleanupListCallback(ulong rcwAddr, ulong ctx, ulong staThread, Interop.BOOL isFreeThreaded, void* contextPtr)
    {
        var context = (TraverseRCWCleanupListRecordingContext)GCHandle.FromIntPtr((nint)contextPtr).Target!;
        context.ExpectedElements[rcwAddr] = ctx;
        return context.Callback(rcwAddr, ctx, staThread, isFreeThreaded, context.Token);
    }

    [UnmanagedCallersOnly]
    private static Interop.BOOL TraverseRCWCleanupListCallback(ulong rcwAddr, ulong ctx, ulong staThread, Interop.BOOL isFreeThreaded, void* expectedElements)
    {
        var expectedElementsDict = (Dictionary<ulong, ulong>)GCHandle.FromIntPtr((nint)expectedElements).Target!;
        if (expectedElementsDict.TryGetValue(rcwAddr, out ulong expectedCtx) && expectedCtx == ctx)
        {
            expectedElementsDict[default]++;
        }
        else
        {
            Debug.Fail($"Unexpected RCW address {rcwAddr:x} or context {ctx:x}");
        }
        return Interop.BOOL.TRUE;
    }

    private sealed class TraverseLoaderHeapRecordingContext
    {
        public delegate* unmanaged<ulong, nuint, Interop.BOOL, void> Callback;
    }

    [ThreadStatic]
    private static TraverseLoaderHeapRecordingContext? _recordingTraverseLoaderHeapContext;
    [ThreadStatic]
    private static List<(ulong VirtualAddress, nuint VirtualSize)>? _debugTraverseLoaderHeapBlocks;
    [ThreadStatic]
    private static uint _debugTraverseLoaderDebugCount;

    private static List<(ulong VirtualAddress, nuint VirtualSize)> DebugTraverseLoaderHeapBlocks
        => _debugTraverseLoaderHeapBlocks ??= [];

    [UnmanagedCallersOnly]
    private static void RecordingTraverseLoaderHeapCallback(ulong virtualAddress, nuint virtualSize, Interop.BOOL isCurrent)
    {
        DebugTraverseLoaderHeapBlocks.Add((virtualAddress, virtualSize));
        _recordingTraverseLoaderHeapContext!.Callback(virtualAddress, virtualSize, isCurrent);
    }

    [UnmanagedCallersOnly]
    private static void TraverseLoaderHeapDebugCallback(ulong virtualAddress, nuint virtualSize, Interop.BOOL _)
    {
        List<(ulong VirtualAddress, nuint VirtualSize)> expected = DebugTraverseLoaderHeapBlocks;
        bool found = expected.Remove((virtualAddress, virtualSize));
        _debugTraverseLoaderDebugCount++;
        Debug.Assert(found, $"Unexpected loader heap block: address={virtualAddress:x}, size={virtualSize:x}");
    }
#endif

#if DEBUG
    private static void VerifyGCInterestingInfoData(DacpGCInterestingInfoData* cdacData, DacpGCInterestingInfoData* legacyData)
    {
        for (int i = 0; i < GCConstants.DAC_NUM_GC_DATA_POINTS; i++)
        {
            Debug.Assert(cdacData->interestingDataPoints[i] == legacyData->interestingDataPoints[i],
                $"interestingDataPoints[{i}] - cDAC: {cdacData->interestingDataPoints[i]}, DAC: {legacyData->interestingDataPoints[i]}");
        }

        for (int i = 0; i < GCConstants.DAC_MAX_COMPACT_REASONS_COUNT; i++)
        {
            Debug.Assert(cdacData->compactReasons[i] == legacyData->compactReasons[i],
                $"compactReasons[{i}] - cDAC: {cdacData->compactReasons[i]}, DAC: {legacyData->compactReasons[i]}");
        }

        for (int i = 0; i < GCConstants.DAC_MAX_EXPAND_MECHANISMS_COUNT; i++)
        {
            Debug.Assert(cdacData->expandMechanisms[i] == legacyData->expandMechanisms[i],
                $"expandMechanisms[{i}] - cDAC: {cdacData->expandMechanisms[i]}, DAC: {legacyData->expandMechanisms[i]}");
        }

        for (int i = 0; i < GCConstants.DAC_MAX_GC_MECHANISM_BITS_COUNT; i++)
        {
            Debug.Assert(cdacData->bitMechanisms[i] == legacyData->bitMechanisms[i],
                $"bitMechanisms[{i}] - cDAC: {cdacData->bitMechanisms[i]}, DAC: {legacyData->bitMechanisms[i]}");
        }

        for (int i = 0; i < GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
        {
            Debug.Assert(cdacData->globalMechanisms[i] == legacyData->globalMechanisms[i],
                $"globalMechanisms[{i}] - cDAC: {cdacData->globalMechanisms[i]}, DAC: {legacyData->globalMechanisms[i]}");
        }
    }
#endif

}
