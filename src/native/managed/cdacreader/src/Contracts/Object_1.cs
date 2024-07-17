// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Object_1 : IObject
{
    private readonly Target _target;
    private readonly ulong _methodTableOffset;
    private readonly TargetPointer _stringMethodTable;
    private readonly byte _objectToMethodTableUnmask;

    internal Object_1(Target target, ulong methodTableOffset, byte objectToMethodTableUnmask, TargetPointer stringMethodTable)
    {
        _target = target;
        _methodTableOffset = methodTableOffset;
        _stringMethodTable = stringMethodTable;
        _objectToMethodTableUnmask = objectToMethodTableUnmask;
    }

    public TargetPointer GetMethodTableAddress(TargetPointer address)
    {
        TargetPointer mt = _target.ReadPointer(address + _methodTableOffset);
        return mt.Value & (ulong)~_objectToMethodTableUnmask;
    }

    string IObject.GetStringValue(TargetPointer address)
    {
        TargetPointer mt = GetMethodTableAddress(address);
        if (mt != _stringMethodTable)
            throw new ArgumentException("Address does not represent a string object", nameof(address));

        // Validates the method table
        _ = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(mt);

        Data.String str = _target.ProcessedData.GetOrAdd<Data.String>(address);
        Span<byte> span = stackalloc byte[(int)str.StringLength * sizeof(char)];
        _target.ReadBuffer(str.FirstChar, span);
        return new string(MemoryMarshal.Cast<byte, char>(span));
    }
}
