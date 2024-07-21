// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader;

public readonly struct TargetPointer : IEquatable<TargetPointer>
{
    public static TargetPointer Null = new(0);
    public static TargetPointer Max32Bit = new(uint.MaxValue);
    public static TargetPointer Max64Bit = new(ulong.MaxValue);

    public readonly ulong Value;
    public TargetPointer(ulong value) => Value = value;

    public static implicit operator ulong(TargetPointer p) => p.Value;
    public static implicit operator TargetPointer(ulong v) => new TargetPointer(v);

    public static bool operator ==(TargetPointer left, TargetPointer right) => left.Value == right.Value;
    public static bool operator !=(TargetPointer left, TargetPointer right) => left.Value != right.Value;

    public override bool Equals(object? obj) => obj is TargetPointer pointer && Equals(pointer);
    public bool Equals(TargetPointer other) => Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

public readonly struct TargetNUInt
{
    public readonly ulong Value;
    public TargetNUInt(ulong value) => Value = value;
}

public readonly struct TargetSpan
{
    public TargetSpan(TargetPointer address, ulong size)
    {
        Address = address;
        Size = size;
    }

    public TargetPointer Address { get; }
    public ulong Size { get; }
}

/// <summary>
/// Representation of the target under inspection
/// </summary>
/// <remarks>
/// This class provides APIs used by contracts for reading from the target and getting type and globals
/// information based on the target's contract descriptor. Like the contracts themselves in cdacreader,
/// these are throwing APIs. Any callers at the boundaries (for example, unmanaged entry points, COM)
/// should handle any exceptions.
/// </remarks>
public sealed unsafe class Target
{
    public record struct TypeInfo
    {
        public uint? Size;
        public Dictionary<string, FieldInfo> Fields = [];

        public TypeInfo() { }
    }

    public record struct FieldInfo
    {
        public int Offset;
        public DataType Type;
        public string? TypeName;
    }

    private const int StackAllocByteThreshold = 1024;

    private readonly struct Configuration
    {
        public bool IsLittleEndian { get; init; }
        public int PointerSize { get; init; }
    }

    private readonly Configuration _config;
    private readonly Reader _reader;

    private readonly Dictionary<string, int> _contracts = [];
    private readonly IReadOnlyDictionary<string, (ulong Value, string? Type)> _globals = new Dictionary<string, (ulong, string?)>();
    private readonly Dictionary<DataType, TypeInfo> _knownTypes = [];
    private readonly Dictionary<string, TypeInfo> _types = [];

    internal Contracts.Registry Contracts { get; }
    internal DataCache ProcessedData { get; }
    internal Helpers.Metadata Metadata { get; }

    public delegate int ReadFromTargetDelegate(ulong address, Span<byte> bufferToFill);

    /// <summary>
    /// Create a new target instance from a contract descriptor embedded in the target memory.
    /// </summary>
    /// <param name="contractDescriptor">The offset of the contract descriptor in the target memory</param>
    /// <param name="readFromTarget">A callback to read memory blocks at a given address from the target</param>
    /// <param name="target">The target object.</param>
    /// <returns>If a target instance could be created, <c>true</c>; otherwise, <c>false</c>.</returns>
    public static bool TryCreate(ulong contractDescriptor, ReadFromTargetDelegate readFromTarget, out Target? target)
    {
        Reader reader = new Reader(readFromTarget);
        if (TryReadContractDescriptor(contractDescriptor, reader, out Configuration config, out ContractDescriptorParser.ContractDescriptor? descriptor, out TargetPointer[] pointerData))
        {
            target = new Target(config, descriptor!, pointerData, reader);
            return true;
        }

        target = null;
        return false;
    }

    /// <summary>
    /// Create a new target instance from an externally-provided contract descriptor.
    /// </summary>
    /// <param name="contractDescriptor">The contract descriptor to use for this target</param>
    /// <param name="globalPointerValues">The values for any global pointers specified in the contract descriptor.</param>
    /// <param name="readFromTarget">A callback to read memory blocks at a given address from the target</param>
    /// <param name="isLittleEndian">Whether the target is little-endian</param>
    /// <param name="pointerSize">The size of a pointer in bytes in the target process.</param>
    /// <returns>The target object.</returns>
    public static Target Create(ContractDescriptorParser.ContractDescriptor contractDescriptor, TargetPointer[] globalPointerValues, ReadFromTargetDelegate readFromTarget, bool isLittleEndian, int pointerSize)
    {
        return new Target(new Configuration { IsLittleEndian = isLittleEndian, PointerSize = pointerSize }, contractDescriptor, globalPointerValues, new Reader(readFromTarget));
    }

    private Target(Configuration config, ContractDescriptorParser.ContractDescriptor descriptor, TargetPointer[] pointerData, Reader reader)
    {
        Contracts = new Contracts.Registry(this);
        ProcessedData = new DataCache(this);
        Metadata = new Helpers.Metadata(this);
        _config = config;
        _reader = reader;

        _contracts = descriptor.Contracts ?? [];

        // Set pointer type size
        _knownTypes[DataType.pointer] = new TypeInfo { Size = (uint)_config.PointerSize };

        // Read types and map to known data types
        if (descriptor.Types is not null)
        {
            foreach ((string name, ContractDescriptorParser.TypeDescriptor type) in descriptor.Types)
            {
                TypeInfo typeInfo = new() { Size = type.Size };
                if (type.Fields is not null)
                {
                    foreach ((string fieldName, ContractDescriptorParser.FieldDescriptor field) in type.Fields)
                    {
                        typeInfo.Fields[fieldName] = new FieldInfo()
                        {
                            Offset = field.Offset,
                            Type = field.Type is null ? DataType.Unknown : GetDataType(field.Type),
                            TypeName = field.Type
                        };
                    }
                }

                DataType dataType = GetDataType(name);
                if (dataType is not DataType.Unknown)
                {
                    _knownTypes[dataType] = typeInfo;
                }
                else
                {
                    _types[name] = typeInfo;
                }
            }
        }

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

    private static DataType GetDataType(string type)
    {
        if (Enum.TryParse(type, false, out DataType dataType) && Enum.IsDefined(dataType))
            return dataType;

        return DataType.Unknown;
    }

    public int PointerSize => _config.PointerSize;

    public T Read<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (!TryRead(address, _config.IsLittleEndian, _reader, out T value))
            throw new InvalidOperationException($"Failed to read {typeof(T)} at 0x{address:x8}.");

        return value;
    }

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

    public void ReadBuffer(ulong address, Span<byte> buffer)
    {
        if (!TryReadBuffer(address, buffer))
            throw new InvalidOperationException($"Failed to read {buffer.Length} bytes at 0x{address:x8}.");
    }

    private bool TryReadBuffer(ulong address, Span<byte> buffer)
    {
        return _reader.ReadFromTarget(address, buffer) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
    }

    public TargetPointer ReadPointer(ulong address)
    {
        if (!TryReadPointer(address, _config, _reader, out TargetPointer pointer))
            throw new InvalidOperationException($"Failed to read pointer at 0x{address:x8}.");

        return pointer;
    }

    public void ReadPointers(ulong address, Span<TargetPointer> buffer)
    {
        // TODO(cdac) - This could do a single read, and then swizzle in place if it is useful for performance
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = ReadPointer(address);
            checked
            {
                address += (ulong)_config.PointerSize;
            }
        }
    }

    public TargetNUInt ReadNUInt(ulong address)
    {
        if (!TryReadNUInt(address, _config, _reader, out ulong value))
            throw new InvalidOperationException($"Failed to read nuint at 0x{address:x8}.");

        return new TargetNUInt(value);
    }

    private static bool TryReadPointer(ulong address, Configuration config, Reader reader, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;
        if (!TryReadNUInt(address, config, reader, out ulong value))
            return false;

        pointer = new TargetPointer(value);
        return true;
    }

    private static bool TryReadNUInt(ulong address, Configuration config, Reader reader, out ulong value)
    {
        value = 0;
        if (config.PointerSize == sizeof(uint)
            && TryRead(address, config.IsLittleEndian, reader, out uint value32))
        {
            value = value32;
            return true;
        }
        else if (config.PointerSize == sizeof(ulong)
            && TryRead(address, config.IsLittleEndian, reader, out ulong value64))
        {
            value = value64;
            return true;
        }

        return false;
    }

    public static bool IsAligned(ulong value, int alignment)
        => (value & (ulong)(alignment - 1)) == 0;

    public bool IsAlignedToPointerSize(uint value)
        => IsAligned(value, _config.PointerSize);
    public bool IsAlignedToPointerSize(ulong value)
        => IsAligned(value, _config.PointerSize);
    public bool IsAlignedToPointerSize(TargetPointer pointer)
        => IsAligned(pointer.Value, _config.PointerSize);

    public T ReadGlobal<T>(string name) where T : struct, INumber<T>
        => ReadGlobal<T>(name, out _);

    public T ReadGlobal<T>(string name, out string? type) where T : struct, INumber<T>
    {
        if (!_globals.TryGetValue(name, out (ulong Value, string? Type) global))
            throw new InvalidOperationException($"Failed to read global {typeof(T)} '{name}'.");

        type = global.Type;
        return T.CreateChecked(global.Value);
    }

    public TargetPointer ReadGlobalPointer(string name)
        => ReadGlobalPointer(name, out _);

    public TargetPointer ReadGlobalPointer(string name, out string? type)
    {
        if (!_globals.TryGetValue(name, out (ulong Value, string? Type) global))
            throw new InvalidOperationException($"Failed to read global pointer '{name}'.");

        type = global.Type;
        return new TargetPointer(global.Value);
    }

    public TypeInfo GetTypeInfo(DataType type)
    {
        if (!_knownTypes.TryGetValue(type, out TypeInfo typeInfo))
            throw new InvalidOperationException($"Failed to get type info for '{type}'");

        return typeInfo;
    }

    public TypeInfo GetTypeInfo(string type)
    {
        if (_types.TryGetValue(type, out TypeInfo typeInfo))
        return typeInfo;

        DataType dataType = GetDataType(type);
        if (dataType is not DataType.Unknown)
            return GetTypeInfo(dataType);

        throw new InvalidOperationException($"Failed to get type info for '{type}'");
    }

    internal bool TryGetContractVersion(string contractName, out int version)
        => _contracts.TryGetValue(contractName, out version);

    /// <summary>
    /// Store of addresses that have already been read into corresponding data models.
    /// This is simply used to avoid re-processing data on every request.
    /// </summary>
    internal sealed class DataCache
    {
        private readonly Target _target;
        private readonly ConcurrentDictionary<(ulong, Type), object?> _readDataByAddress = [];

        public DataCache(Target target)
        {
            _target = target;
        }

        public T GetOrAdd<T>(TargetPointer address) where T : IData<T>
        {
            if (TryGet(address, out T? result))
                return result;

            T constructed = T.Create(_target, address);
            if (_readDataByAddress.TryAdd((address, typeof(T)), constructed))
                return constructed;

            bool found = TryGet(address, out result);
            Debug.Assert(found);
            return result!;
        }

        public bool TryGet<T>(ulong address, [NotNullWhen(true)] out T? data)
        {
            data = default;
            if (!_readDataByAddress.TryGetValue((address, typeof(T)), out object? dataObj))
                return false;

            if (dataObj is T dataMaybe)
            {
                data = dataMaybe;
                return true;
            }

            return false;
        }
    }

    private readonly struct Reader(ReadFromTargetDelegate readFromTarget)
    {
        public int ReadFromTarget(ulong address, Span<byte> buffer)
        {
            return readFromTarget(address, buffer);
        }

        public int ReadFromTarget(ulong address, byte* buffer, uint bytesToRead)
            => readFromTarget(address, new Span<byte>(buffer, checked((int)bytesToRead)));
    }
}
