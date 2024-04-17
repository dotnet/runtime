// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader;

public struct TargetPointer
{
    public static TargetPointer Null = new(0);

    public ulong Value;
    public TargetPointer(ulong value) => Value = value;
}

internal sealed unsafe class Target
{
    private const int StackAllocByteThreshold = 1024;

    private readonly delegate* unmanaged<ulong, byte*, uint, void*, int> _readFromTarget;
    private readonly void* _readContext;

    private bool _isLittleEndian;
    private int _pointerSize;

    private TargetPointer[] _pointerData = [];
    private IReadOnlyDictionary<string, int> _contracts = new Dictionary<string, int>();

    public Target(ulong contractDescriptor, delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* readContext)
    {
        _readFromTarget = readFromTarget;
        _readContext = readContext;

        ReadContractDescriptor(contractDescriptor);
    }

    // See docs/design/datacontracts/contract-descriptor.md
    private void ReadContractDescriptor(ulong address)
    {
        // Magic - uint64_t
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        if (ReadFromTarget(address, buffer) < 0)
            throw new InvalidOperationException("Failed to read magic.");

        address += sizeof(ulong);
        ReadOnlySpan<byte> magicLE = "DNCCDAC\0"u8;
        ReadOnlySpan<byte> magicBE = "\0CADCCND"u8;
        _isLittleEndian = buffer.SequenceEqual(magicLE);
        if (!_isLittleEndian && !buffer.SequenceEqual(magicBE))
            throw new InvalidOperationException("Invalid magic.");

        // Flags - uint32_t
        uint flags = ReadUInt32(address);
        address += sizeof(uint);

        // Bit 1 represents the pointer size. 0 = 64-bit, 1 = 32-bit.
        _pointerSize = (int)(flags & 0x2) == 0 ? sizeof(ulong) : sizeof(uint);

        // Descriptor size - uint32_t
        uint descriptorSize = ReadUInt32(address);
        address += sizeof(uint);

        // Descriptor - char*
        TargetPointer descriptor = ReadPointer(address);
        address += (uint)_pointerSize;

        // Pointer data count - uint32_t
        uint pointerDataCount = ReadUInt32(address);
        address += sizeof(uint);

        // Padding
        address += sizeof(uint);

        // Pointer data - uintptr_t*
        TargetPointer pointerData = ReadPointer(address);

        // Read descriptor
        Span<byte> descriptorBuffer = descriptorSize <= StackAllocByteThreshold
            ? stackalloc byte[(int)descriptorSize]
            : new byte[(int)descriptorSize];
        if (ReadFromTarget(descriptor.Value, descriptorBuffer) < 0)
            throw new InvalidOperationException("Failed to read descriptor.");

        ContractDescriptorParser.ContractDescriptor? targetDescriptor = ContractDescriptorParser.ParseCompact(descriptorBuffer);

        if (targetDescriptor is null)
        {
            throw new InvalidOperationException("Failed to parse descriptor.");
        }

        // TODO: [cdac] Read globals and types
        // note: we will probably want to store the globals and types into a more usable form
        _contracts = targetDescriptor.Contracts ?? new Dictionary<string, int>();

        // Read pointer data
        _pointerData = new TargetPointer[pointerDataCount];
        for (int i = 0; i < pointerDataCount; i++)
        {
            _pointerData[i] = ReadPointer(pointerData.Value + (uint)(i * _pointerSize));
        }
    }

    public uint ReadUInt32(ulong address)
    {
        if (!TryReadUInt32(address, out uint value))
            throw new InvalidOperationException($"Failed to read uint32 at 0x{address:x8}.");

        return value;
    }

    public bool TryReadUInt32(ulong address, out uint value)
    {
        value = 0;

        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        if (ReadFromTarget(address, buffer) < 0)
            return false;

        value = _isLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
            : BinaryPrimitives.ReadUInt32BigEndian(buffer);

        return true;
    }

    public TargetPointer ReadPointer(ulong address)
    {
        if (!TryReadPointer(address, out TargetPointer pointer))
            throw new InvalidOperationException($"Failed to read pointer at 0x{address:x8}.");

        return pointer;
    }

    public bool TryReadPointer(ulong address, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;

        Span<byte> buffer = stackalloc byte[_pointerSize];
        if (ReadFromTarget(address, buffer) < 0)
            return false;

        if (_pointerSize == sizeof(uint))
        {
            pointer = new TargetPointer(
                _isLittleEndian
                    ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                    : BinaryPrimitives.ReadUInt32BigEndian(buffer));
        }
        else if (_pointerSize == sizeof(ulong))
        {
            pointer = new TargetPointer(
                _isLittleEndian
                    ? BinaryPrimitives.ReadUInt64LittleEndian(buffer)
                    : BinaryPrimitives.ReadUInt64BigEndian(buffer));
        }

        return true;
    }

    private int ReadFromTarget(ulong address, Span<byte> buffer)
    {
        fixed (byte* bufferPtr = buffer)
        {
            return _readFromTarget(address, bufferPtr, (uint)buffer.Length, _readContext);
        }
    }

    private int ReadFromTarget(ulong address, byte* buffer, uint bytesToRead)
        => _readFromTarget(address, buffer, bytesToRead, _readContext);
}
