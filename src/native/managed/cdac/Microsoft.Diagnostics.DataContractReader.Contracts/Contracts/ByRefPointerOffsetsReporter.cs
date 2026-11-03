// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ByRefPointerOffsetsReporter
{
    private const int MaxByRefLikeRecursionDepth = 16;

    private readonly IRuntimeTypeSystem _rts;
    private readonly uint _pointerSize;
    private readonly bool _allowPartialResults;

    public ByRefPointerOffsetsReporter(Target target, bool allowPartialResults = true)
    {
        _rts = target.Contracts.RuntimeTypeSystem;
        _pointerSize = (uint)target.PointerSize;
        _allowPartialResults = allowPartialResults;
    }

    public IEnumerable<ulong> Find(ITypeHandle typeHandle)
    {
        List<ulong> offsets = [];
        try
        {
            foreach (ulong offset in FindCore(typeHandle, baseOffset: 0, depth: 0))
                offsets.Add(offset);
        }
        catch (InvalidOperationException) when (_allowPartialResults)
        {
            return [];
        }
        catch (VirtualReadException) when (_allowPartialResults)
        {
            return [];
        }

        return offsets;
    }

    private IEnumerable<ulong> FindCore(
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
            if (fieldTypeHandle is null && !_allowPartialResults)
                throw new NotImplementedException($"Runtime layout is unavailable for field {fieldDesc}.");

            if (fieldTypeHandle is not null && _rts.IsByRefLike(fieldTypeHandle))
            {
                foreach (ulong slot in FindCore(fieldTypeHandle, baseOffset + fieldOffset, depth + 1))
                    yield return slot;
            }
        }
        else if (fieldType == CorElementType.Byref)
        {
            yield return baseOffset + fieldOffset;
        }
    }

    private IEnumerable<ulong> FindCore(ITypeHandle typeHandle, ulong baseOffset, int depth)
    {
        if (depth > MaxByRefLikeRecursionDepth)
        {
            if (!_allowPartialResults)
                throw new NotImplementedException("Byref-like field nesting exceeds the supported depth.");

            yield break;
        }

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
                {
                    if (!_allowPartialResults)
                        throw new NotImplementedException($"Runtime layout is unavailable for inline-array field {fieldDesc}.");

                    continue;
                }

                uint totalSize = _rts.GetNumInstanceFieldBytes(typeHandle);
                for (uint offset = 0; offset < totalSize; offset += elementSize)
                {
                    foreach (ulong slot in FindCore(fieldDesc, baseOffset + offset, depth, isInlineArrayElement: true))
                        yield return slot;
                }
            }
            else
            {
                foreach (ulong slot in FindCore(fieldDesc, baseOffset, depth, isInlineArrayElement: false))
                    yield return slot;
            }
        }
    }
}
