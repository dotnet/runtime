// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.Diagnostics.DataContractReader;

public struct TargetPointer
{
    public static TargetPointer Null = new(0);

    public ulong Value;
    public TargetPointer(ulong value) => Value = value;
}

internal sealed unsafe class Target
{
    private readonly delegate* unmanaged<ulong, byte*, uint, void*, int> _readFromTarget;
    private readonly void* _readContext;

    private bool _isLittleEndian;
    private int _pointerSize;

    public Target(ulong _, delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* readContext)
    {
        _readFromTarget = readFromTarget;
        _readContext = readContext;

        // TODO: [cdac] Populate from descriptor
        _isLittleEndian = BitConverter.IsLittleEndian;
        _pointerSize = IntPtr.Size;
    }

    public bool TryReadPointer(ulong address, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;

        byte* buffer = stackalloc byte[_pointerSize];
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffer, _pointerSize);
        if (ReadFromTarget(address, buffer, (uint)_pointerSize) < 0)
            return false;

        if (_pointerSize == sizeof(uint))
        {
            pointer = new TargetPointer(
                _isLittleEndian
                    ? BinaryPrimitives.ReadUInt32LittleEndian(span)
                    : BinaryPrimitives.ReadUInt32BigEndian(span));
        }
        else if (_pointerSize == sizeof(ulong))
        {
            pointer = new TargetPointer(
                _isLittleEndian
                    ? BinaryPrimitives.ReadUInt64LittleEndian(span)
                    : BinaryPrimitives.ReadUInt64BigEndian(span));
        }

        return true;
    }

    private int ReadFromTarget(ulong address, byte* buffer, uint bytesToRead)
        => _readFromTarget(address, buffer, bytesToRead, _readContext);
}
