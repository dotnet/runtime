// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class StackWalk_2 : StackWalk_1
{
    private const int MaxValueClassRecursionDepth = 64;

    private readonly Target _target;

    internal StackWalk_2(Target target)
        : base(target)
    {
        _target = target;
    }

    protected override void ReportGCFrameRoots(ThreadData threadData, GcScanContext scanContext)
    {
        ulong pointerSize = (ulong)_target.PointerSize;
        uint valueClassFlag = _target.ReadGlobal<uint>(Constants.Globals.GCFrameValueClassFlag);
        HashSet<TargetPointer> seen = [];
        TargetPointer pGCFrame = threadData.GCFrame;
        while (pGCFrame != TargetPointer.Null)
        {
            if (!seen.Add(pGCFrame))
                throw new InvalidOperationException("Found a cycle when processing ThreadData.GCFrame list.");

            Data.GCFrame gcFrame = _target.ProcessedData.GetOrAdd<Data.GCFrame>(pGCFrame);
            scanContext.UpdateScanContext(pGCFrame, TargetCodePointer.Null, pGCFrame, StackRefData.SourceTypes.StackSourceOther);
            if ((gcFrame.GCFlags & valueClassFlag) != 0)
            {
                ReportValueClassFrameRoots(pGCFrame, scanContext);
            }
            else
            {
                GcScanFlags flags = (GcScanFlags)gcFrame.GCFlags;
                for (uint i = 0; i < gcFrame.NumObjRefs; i++)
                {
                    TargetPointer slot = new(gcFrame.ObjRefs.Value + (ulong)i * pointerSize);
                    scanContext.GCReportCallback(slot, flags);
                }
            }
            pGCFrame = gcFrame.Next;
        }
    }

    private void ReportValueClassFrameRoots(TargetPointer frame, GcScanContext scanContext)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        Data.ProtectValueClassFrame valueClassFrame = _target.ProcessedData.GetOrAdd<Data.ProtectValueClassFrame>(frame);
        HashSet<TargetPointer> seen = [];
        TargetPointer infoAddress = valueClassFrame.ValueClassInfoList;
        while (infoAddress != TargetPointer.Null)
        {
            if (!seen.Add(infoAddress))
                throw new InvalidOperationException("Found a cycle when processing a ProtectValueClassFrame list.");

            Data.ValueClassInfo info = _target.ProcessedData.GetOrAdd<Data.ValueClassInfo>(infoAddress);
            ITypeHandle typeHandle = rts.GetTypeHandle(info.MethodTable);
            if (rts.IsByRefLike(typeHandle))
            {
                ReportByRefLikeValueClassRoots(rts, typeHandle, info.Data, scanContext, 0);
            }

            foreach ((uint offset, uint size) in rts.GetGCDescSeries(typeHandle))
            {
                ulong unboxedOffset = offset - (uint)_target.PointerSize;
                for (ulong innerOffset = 0; innerOffset < size; innerOffset += (uint)_target.PointerSize)
                {
                    scanContext.GCReportCallback(info.Data + unboxedOffset + innerOffset, GcScanFlags.None);
                }
            }

            infoAddress = info.Next;
        }
    }

    private void ReportByRefLikeValueClassRoots(
        IRuntimeTypeSystem rts,
        ITypeHandle typeHandle,
        TargetPointer data,
        GcScanContext scanContext,
        int depth)
    {
        if (depth > MaxValueClassRecursionDepth)
            return;

        uint totalSize = rts.GetNumInstanceFieldBytes(typeHandle);
        if (totalSize > rts.GetBaseSize(typeHandle))
            return;

        bool isInlineArray = IsInlineArray(typeHandle);
        foreach (TargetPointer fieldDesc in rts.GetFieldDescList(typeHandle))
        {
            if (rts.IsFieldDescStatic(fieldDesc))
                continue;

            uint offset = rts.GetFieldDescOffset(fieldDesc, fieldDef: null);
            CorElementType fieldType = rts.GetFieldDescType(fieldDesc);
            ITypeHandle? nestedType = fieldType == CorElementType.ValueType
                ? rts.GetFieldDescApproxTypeHandle(fieldDesc)
                : null;
            bool nestedIsByRefLike = nestedType is not null && rts.IsByRefLike(nestedType);
            if (!ShouldReportByRefLikeField(fieldType, nestedIsByRefLike))
                continue;

            uint elementSize = isInlineArray
                ? GetInlineArrayElementSize(rts, fieldType, nestedType)
                : 0;
            if (isInlineArray && elementSize == 0)
                continue;

            foreach (uint fieldOffset in GetByRefLikeFieldOffsets(
                isInlineArray,
                offset,
                elementSize,
                totalSize))
            {
                TargetPointer fieldData = data + fieldOffset;
                if (fieldType == CorElementType.Byref)
                {
                    scanContext.GCReportCallback(fieldData, GcScanFlags.GC_CALL_INTERIOR);
                }
                else if (nestedType is not null && rts.IsByRefLike(nestedType))
                {
                    ReportByRefLikeValueClassRoots(rts, nestedType, fieldData, scanContext, depth + 1);
                }
            }
        }
    }

    internal static bool ShouldReportByRefLikeField(CorElementType fieldType, bool nestedIsByRefLike) =>
        fieldType == CorElementType.Byref || (fieldType == CorElementType.ValueType && nestedIsByRefLike);

    internal static IEnumerable<uint> GetByRefLikeFieldOffsets(
        bool isInlineArray,
        uint fieldOffset,
        uint elementSize,
        uint totalSize)
    {
        if (!isInlineArray)
        {
            yield return fieldOffset;
            yield break;
        }

        if (elementSize == 0)
            throw new InvalidOperationException("Inline array element size must be non-zero.");

        for (uint repeatedOffset = 0; repeatedOffset < totalSize; repeatedOffset = checked(repeatedOffset + elementSize))
        {
            yield return checked(fieldOffset + repeatedOffset);
        }
    }

    private uint GetInlineArrayElementSize(
        IRuntimeTypeSystem rts,
        CorElementType fieldType,
        ITypeHandle? nestedType)
    {
        if (fieldType == CorElementType.Byref)
            return (uint)_target.PointerSize;

        if (nestedType is not null)
            return rts.GetNumInstanceFieldBytes(nestedType);

        return 0;
    }

    private bool IsInlineArray(ITypeHandle typeHandle)
    {
        Data.MethodTable methodTable = _target.ProcessedData.GetOrAdd<Data.MethodTable>(typeHandle.Address);
        TargetPointer eeClassPointer = methodTable.EEClassOrCanonMT;
        if (MethodTableFlags_1.GetEEClassOrCanonMTBits(eeClassPointer) == MethodTableFlags_1.EEClassOrCanonMTBits.CanonMT)
        {
            TargetPointer canonicalMethodTablePointer = MethodTableFlags_1.UntagEEClassOrCanonMT(eeClassPointer);
            Data.MethodTable canonicalMethodTable = _target.ProcessedData.GetOrAdd<Data.MethodTable>(canonicalMethodTablePointer);
            eeClassPointer = canonicalMethodTable.EEClassOrCanonMT;
        }

        Data.EEClass eeClass = _target.ProcessedData.GetOrAdd<Data.EEClass>(eeClassPointer);
        return eeClass.IsInlineArray;
    }
}
