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

public sealed unsafe class Target
{
    private const int StackAllocByteThreshold = 1024;

    private readonly struct Configuration
    {
        public bool IsLittleEndian { get; init; }
        public int PointerSize { get; init; }
    }

    private readonly Configuration _config;
    private readonly Reader _reader;

    private readonly IReadOnlyDictionary<string, int> _contracts = new Dictionary<string, int>();
    private readonly IReadOnlyDictionary<string, (ulong Value, string? Type)> _globals = new Dictionary<string, (ulong, string?)>();

    public static bool TryCreate(ulong contractDescriptor, delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* readContext, out Target? target)
    {
        Reader reader = new Reader(readFromTarget, readContext);
        if (TryReadContractDescriptor(contractDescriptor, reader, out Configuration config, out ContractDescriptorParser.ContractDescriptor? descriptor, out TargetPointer[] pointerData))
        {
            target = new Target(config, descriptor!, pointerData, reader);
            return true;
        }

        target = null;
        return false;
    }

    private Target(Configuration config, ContractDescriptorParser.ContractDescriptor descriptor, TargetPointer[] pointerData, Reader reader)
    {
        _config = config;
        _reader = reader;

        // TODO: [cdac] Read types
        // note: we will probably want to store the globals and types into a more usable form
        _contracts = descriptor.Contracts ?? [];

        // Read globals and map indirect values to pointer data
        if (descriptor.Globals is not null)
        {
            Dictionary<string, (ulong Value, string? Type)> globals = [];
            foreach ((string name, ContractDescriptorParser.GlobalDescriptor global) in descriptor.Globals)
            {
                ulong value = global.Value;
                if (global.Indirect)
                {
                    if (value >= (ulong)pointerData.Length)
                        throw new InvalidOperationException($"Invalid pointer data index {value}.");

                    value = pointerData[value].Value;
                }

                globals[name] = (value, global.Type);
            }

            _globals = globals;
        }
    }

    // See docs/design/datacontracts/contract-descriptor.md
    private static bool TryReadContractDescriptor(
        ulong address,
        Reader reader,
        out Configuration config,
        out ContractDescriptorParser.ContractDescriptor? descriptor,
        out TargetPointer[] pointerData)
    {
        config = default;
        descriptor = null;
        pointerData = [];

        // Magic - uint64_t
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        if (reader.ReadFromTarget(address, buffer) < 0)
            return false;

        address += sizeof(ulong);
        ReadOnlySpan<byte> magicLE = "DNCCDAC\0"u8;
        ReadOnlySpan<byte> magicBE = "\0CADCCND"u8;
        bool isLittleEndian = buffer.SequenceEqual(magicLE);
        if (!isLittleEndian && !buffer.SequenceEqual(magicBE))
            return false;

        // Flags - uint32_t
        if (!TryReadUInt32(address, isLittleEndian, reader, out uint flags))
            return false;

        address += sizeof(uint);

        // Bit 1 represents the pointer size. 0 = 64-bit, 1 = 32-bit.
        int pointerSize = (int)(flags & 0x2) == 0 ? sizeof(ulong) : sizeof(uint);

        config = new Configuration { IsLittleEndian = isLittleEndian, PointerSize = pointerSize };

        // Descriptor size - uint32_t
        if (!TryReadUInt32(address, config.IsLittleEndian, reader, out uint descriptorSize))
            return false;

        address += sizeof(uint);

        // Descriptor - char*
        if (!TryReadPointer(address, config, reader, out TargetPointer descriptorAddr))
            return false;

        address += (uint)pointerSize;

        // Pointer data count - uint32_t
        if (!TryReadUInt32(address, config.IsLittleEndian, reader, out uint pointerDataCount))
            return false;

        address += sizeof(uint);

        // Padding - uint32_t
        address += sizeof(uint);

        // Pointer data - uintptr_t*
        if (!TryReadPointer(address, config, reader, out TargetPointer pointerDataAddr))
            return false;

        // Read descriptor
        Span<byte> descriptorBuffer = descriptorSize <= StackAllocByteThreshold
            ? stackalloc byte[(int)descriptorSize]
            : new byte[(int)descriptorSize];
        if (reader.ReadFromTarget(descriptorAddr.Value, descriptorBuffer) < 0)
            return false;

        descriptor = ContractDescriptorParser.ParseCompact(descriptorBuffer);
        if (descriptor is null)
            return false;

        // Read pointer data
        pointerData = new TargetPointer[pointerDataCount];
        for (int i = 0; i < pointerDataCount; i++)
        {
            if (!TryReadPointer(pointerDataAddr.Value + (uint)(i * pointerSize), config, reader, out pointerData[i]))
                return false;
        }

        return true;
    }

    public byte ReadUInt8(ulong address)
    {
        if (!TryReadUInt8(address, out byte value))
            throw new InvalidOperationException($"Failed to read uint8 at 0x{address:x8}.");

        return value;
    }

    public bool TryReadUInt8(ulong address, out byte value)
        => TryReadUInt8(address, _reader, out value);

    private static bool TryReadUInt8(ulong address, Reader reader, out byte value)
    {
        value = 0;
        fixed (byte* ptr = &value)
        {
            if (reader.ReadFromTarget(address, ptr, 1) < 0)
                return false;
        }

        return true;
    }

    public uint ReadUInt32(ulong address)
    {
        if (!TryReadUInt32(address, out uint value))
            throw new InvalidOperationException($"Failed to read uint32 at 0x{address:x8}.");

        return value;
    }

    public bool TryReadUInt32(ulong address, out uint value)
        => TryReadUInt32(address, _config.IsLittleEndian, _reader, out value);

    private static bool TryReadUInt32(ulong address, bool isLittleEndian, Reader reader, out uint value)
    {
        value = 0;

        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        if (reader.ReadFromTarget(address, buffer) < 0)
            return false;

        value = isLittleEndian
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
        => TryReadPointer(address, _config, _reader, out pointer);

    private static bool TryReadPointer(ulong address, Configuration config, Reader reader, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;

        Span<byte> buffer = stackalloc byte[config.PointerSize];
        if (reader.ReadFromTarget(address, buffer) < 0)
            return false;

        if (config.PointerSize == sizeof(uint))
        {
            pointer = new TargetPointer(
                config.IsLittleEndian
                    ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                    : BinaryPrimitives.ReadUInt32BigEndian(buffer));
        }
        else if (config.PointerSize == sizeof(ulong))
        {
            pointer = new TargetPointer(
                config.IsLittleEndian
                    ? BinaryPrimitives.ReadUInt64LittleEndian(buffer)
                    : BinaryPrimitives.ReadUInt64BigEndian(buffer));
        }

        return true;
    }

    public byte ReadGlobalUInt8(string name)
    {
        if (!TryReadGlobalUInt8(name, out byte value))
            throw new InvalidOperationException($"Failed to read global uint8 '{name}'.");

        return value;
    }

    public bool TryReadGlobalUInt8(string name, out byte value)
    {
        value = 0;
        if (!TryReadGlobalValue(name, out ulong globalValue, "uint8"))
            return false;

        value = (byte)globalValue;
        return true;
    }

    public TargetPointer ReadGlobalPointer(string name)
    {
        if (!TryReadGlobalPointer(name, out TargetPointer pointer))
            throw new InvalidOperationException($"Failed to read global pointer '{name}'.");

        return pointer;
    }

    public bool TryReadGlobalPointer(string name, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;
        if (!TryReadGlobalValue(name, out ulong globalValue, "pointer", "nint", "nuint"))
            return false;

        pointer = new TargetPointer(globalValue);
        return true;
    }

    private bool TryReadGlobalValue(string name, out ulong value, params string[] expectedTypes)
    {
        value = 0;
        if (!_globals.TryGetValue(name, out (ulong Value, string? Type) global))
            return false;

        if (global.Type is not null && Array.IndexOf(expectedTypes, global.Type) == -1)
            return false;

        value = global.Value;
        return true;
    }

    private sealed class Reader
    {
        private readonly delegate* unmanaged<ulong, byte*, uint, void*, int> _readFromTarget;
        private readonly void* _context;

        public Reader(delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* context)
        {
            _readFromTarget = readFromTarget;
            _context = context;
        }

        public int ReadFromTarget(ulong address, Span<byte> buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                return _readFromTarget(address, bufferPtr, (uint)buffer.Length, _context);
            }
        }

        public int ReadFromTarget(ulong address, byte* buffer, uint bytesToRead)
            => _readFromTarget(address, buffer, bytesToRead, _context);
    }
}
