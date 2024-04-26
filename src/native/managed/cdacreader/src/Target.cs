// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        if (!TryRead(address, isLittleEndian, reader, out uint flags))
            return false;

        address += sizeof(uint);

        // Bit 1 represents the pointer size. 0 = 64-bit, 1 = 32-bit.
        int pointerSize = (int)(flags & 0x2) == 0 ? sizeof(ulong) : sizeof(uint);

        config = new Configuration { IsLittleEndian = isLittleEndian, PointerSize = pointerSize };

        // Descriptor size - uint32_t
        if (!TryRead(address, config.IsLittleEndian, reader, out uint descriptorSize))
            return false;

        address += sizeof(uint);

        // Descriptor - char*
        if (!TryReadPointer(address, config, reader, out TargetPointer descriptorAddr))
            return false;

        address += (uint)pointerSize;

        // Pointer data count - uint32_t
        if (!TryRead(address, config.IsLittleEndian, reader, out uint pointerDataCount))
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

    public T Read<T>(ulong address, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (!TryRead(address, out value))
            throw new InvalidOperationException($"Failed to read {typeof(T)} at 0x{address:x8}.");

        return value;
    }

    public bool TryRead<T>(ulong address, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
        => TryRead(address, _config.IsLittleEndian, _reader, out value);

    private static bool TryRead<T>(ulong address, bool isLittleEndian, Reader reader, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        value = default;
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        if (reader.ReadFromTarget(address, buffer) < 0)
            return false;

        return isLittleEndian
            ? T.TryReadLittleEndian(buffer, !IsSigned<T>(), out value)
            : T.TryReadBigEndian(buffer, !IsSigned<T>(), out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
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

        if (config.PointerSize == sizeof(uint)
            && TryRead(address, config.IsLittleEndian, reader, out uint value32))
        {
            pointer = new TargetPointer(value32);
            return true;
        }
        else if (config.PointerSize == sizeof(ulong)
            && TryRead(address, config.IsLittleEndian, reader, out ulong value64))
        {
            pointer = new TargetPointer(value64);
            return true;
        }

        return false;
    }

    public T ReadGlobal<T>(string name) where T : struct, INumber<T>
    {
        if (!TryReadGlobal(name, out T value))
            throw new InvalidOperationException($"Failed to read global {typeof(T)} '{name}'.");

        return value;
    }

    public bool TryReadGlobal<T>(string name, out T value) where T : struct, INumber<T>, INumberBase<T>
    {
        value = default;
        if (!_globals.TryGetValue(name, out (ulong Value, string? Type) global))
            return false;

        // TODO: [cdac] Move type validation out of the read such that it does not have to happen for every read
        if (global.Type is not null)
        {
            string? expectedType = Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.SByte => "int8",
                TypeCode.Byte => "uint8",
                TypeCode.Int16 => "int16",
                TypeCode.UInt16 => "uint16",
                TypeCode.Int32 => "int32",
                TypeCode.UInt32 => "uint32",
                TypeCode.Int64 => "int64",
                TypeCode.UInt64 => "uint64",
                _ => null,
            };
            if (expectedType is null || global.Type != expectedType)
            {
                return false;
            }
        }

        value = T.CreateChecked(global.Value);
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
        if (!_globals.TryGetValue(name, out (ulong Value, string? Type) global))
            return false;

        if (global.Type is not null && Array.IndexOf(["pointer", "nint", "nuint"], global.Type) == -1)
            return false;

        pointer = new TargetPointer(global.Value);
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
