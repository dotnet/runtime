// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ByRefPointerOffsetsReporter
{
    private const int MaxByRefLikeRecursionDepth = 16;

    private readonly IRuntimeTypeSystem _rts;
    private readonly uint _pointerSize;

    public ByRefPointerOffsetsReporter(Target target)
    {
        _rts = target.Contracts.RuntimeTypeSystem;
        _pointerSize = (uint)target.PointerSize;
    }

    private IEnumerable<ulong> Find(
        TargetPointer fieldDesc,
        ulong baseOffset,
        int depth,
        bool isInlineArrayElement = false)
    {
        CorElementType fieldType = _rts.GetFieldDescType(fieldDesc);
        uint fieldOffset = isInlineArrayElement
            ? 0
            : _rts.GetFieldDescOffset(fieldDesc, fieldDef: null);

        if (fieldType == CorElementType.ValueType)
        {
            ITypeHandle? fieldTypeHandle = _rts.GetFieldDescApproxTypeHandle(fieldDesc);
            if (fieldTypeHandle is not null && _rts.IsByRefLike(fieldTypeHandle))
            {
                foreach (ulong slot in Find(fieldTypeHandle, baseOffset + fieldOffset, depth + 1))
                    yield return slot;
            }
        }
        else if (fieldType == CorElementType.Byref)
        {
            yield return baseOffset + fieldOffset;
        }
    }

    public IEnumerable<ulong> Find(ITypeHandle typeHandle)
        => Find(typeHandle, baseOffset: 0, depth: 0);

    private IEnumerable<ulong> Find(ITypeHandle typeHandle, ulong baseOffset, int depth)
    {
        if (depth > MaxByRefLikeRecursionDepth)
            yield break;

        bool isInlineArray = _rts.IsInlineArray(typeHandle);

        foreach (TargetPointer fieldDesc in _rts.GetFieldDescList(typeHandle))
        {
            if (_rts.IsFieldDescStatic(fieldDesc))
                continue;

            if (isInlineArray)
            {
                CorElementType fieldType = _rts.GetFieldDescType(fieldDesc);
                ITypeHandle? fieldTypeHandle = fieldType == CorElementType.ValueType
                    ? _rts.GetFieldDescApproxTypeHandle(fieldDesc)
                    : null;
                uint elementSize = fieldType == CorElementType.Byref
                    ? _pointerSize
                    : fieldTypeHandle is not null ? _rts.GetNumInstanceFieldBytes(fieldTypeHandle) : 0;
                if (elementSize == 0)
                    continue;

                uint totalSize = _rts.GetNumInstanceFieldBytes(typeHandle);
                for (uint offset = 0; offset < totalSize; offset += elementSize)
                {
                    foreach (ulong slot in Find(fieldDesc, baseOffset + offset, depth, isInlineArrayElement: true))
                        yield return slot;
                }
            }
            else
            {
                foreach (ulong slot in Find(fieldDesc, baseOffset, depth))
                    yield return slot;
            }
        }
    }
}
