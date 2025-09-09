// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Representation of the target under inspection
/// </summary>
/// <remarks>
/// This class provides APIs used by contracts for reading from the target and getting type and globals
/// information based on the target's contract descriptor. Like the contracts themselves in the cdac,
/// these are throwing APIs. Any callers at the boundaries (for example, unmanaged entry points, COM)
/// should handle any exceptions.
/// </remarks>
public sealed unsafe class ContractDescriptorTarget : Target
{
    private const int StackAllocByteThreshold = 1024;

    private readonly struct Configuration
    {
        public bool IsLittleEndian { get; init; }
        public int PointerSize { get; init; }
    }

    private readonly Configuration _config;

    private readonly DataTargetDelegates _dataTargetDelegates;
    private readonly Dictionary<string, int> _contracts = [];
    private readonly IReadOnlyDictionary<string, GlobalValue> _globals = new Dictionary<string, GlobalValue>();
    private readonly Dictionary<DataType, Target.TypeInfo> _knownTypes = [];
    private readonly Dictionary<string, Target.TypeInfo> _types = [];

    public override ContractRegistry Contracts { get; }
    public override DataCache ProcessedData { get; }

    public delegate int ReadFromTargetDelegate(ulong address, Span<byte> bufferToFill);
    public delegate int WriteToTargetDelegate(ulong address, Span<byte> bufferToWrite);
    public delegate int GetTargetThreadContextDelegate(uint threadId, uint contextFlags, Span<byte> bufferToFill);

    /// <summary>
    /// Create a new target instance from a contract descriptor embedded in the target memory.
    /// </summary>
    /// <param name="contractDescriptor">The offset of the contract descriptor in the target memory</param>
    /// <param name="readFromTarget">A callback to read memory blocks at a given address from the target</param>
    /// <param name="getThreadContext">A callback to fetch a thread's context</param>
    /// <param name="target">The target object.</param>
    /// <returns>If a target instance could be created, <c>true</c>; otherwise, <c>false</c>.</returns>
    public static bool TryCreate(
        ulong contractDescriptor,
        ReadFromTargetDelegate readFromTarget,
        WriteToTargetDelegate writeToTarget,
        GetTargetThreadContextDelegate getThreadContext,
        [NotNullWhen(true)] out ContractDescriptorTarget? target)
    {
        DataTargetDelegates dataTargetDelegates = new DataTargetDelegates(readFromTarget, writeToTarget, getThreadContext);
        if (TryReadAllContractDescriptors(
            contractDescriptor,
            dataTargetDelegates,
            out Descriptor[] descriptors))
        {
            target = new ContractDescriptorTarget(descriptors, dataTargetDelegates);
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
    /// <param name="getThreadContext">A callback to fetch a thread's context</param>
    /// <param name="isLittleEndian">Whether the target is little-endian</param>
    /// <param name="pointerSize">The size of a pointer in bytes in the target process.</param>
    /// <returns>The target object.</returns>
    public static ContractDescriptorTarget Create(
        ContractDescriptorParser.ContractDescriptor contractDescriptor,
        TargetPointer[] globalPointerValues,
        ReadFromTargetDelegate readFromTarget,
        WriteToTargetDelegate writeToTarget,
        GetTargetThreadContextDelegate getThreadContext,
        bool isLittleEndian,
        int pointerSize)
    {
        return new ContractDescriptorTarget(
            [
                new Descriptor
                {
                    Config = new Configuration { IsLittleEndian = isLittleEndian, PointerSize = pointerSize },
                    ContractDescriptor = contractDescriptor,
                    PointerData = globalPointerValues
                }
            ],
            new DataTargetDelegates(readFromTarget, writeToTarget, getThreadContext));
    }

    private ContractDescriptorTarget(Descriptor[] descriptors, DataTargetDelegates dataTargetDelegates)
    {
        Contracts = new CachingContractRegistry(this, this.TryGetContractVersion);
        ProcessedData = new DataCache(this);
        _config = descriptors[0].Config;
        _dataTargetDelegates = dataTargetDelegates;

        _contracts = [];

        // Set pointer type size
        _knownTypes[DataType.pointer] = new TypeInfo { Size = (uint)_config.PointerSize };

        HashSet<string> seenTypeNames = new HashSet<string>();
        HashSet<string> seenGlobalNames = new HashSet<string>();

        Dictionary<string, GlobalValue> globalValues = [];


        foreach (Descriptor descriptor in descriptors)
        {
            if (descriptor.Config.IsLittleEndian != _config.IsLittleEndian ||
                descriptor.Config.PointerSize != _config.PointerSize)
                throw new InvalidOperationException("All descriptors must have the same endianness and pointer size.");

            // Read contracts and add to map
            foreach ((string name, int version) in descriptor.ContractDescriptor.Contracts ?? [])
            {
                if (_contracts.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Duplicate contract name '{name}' found in contract descriptor.");
                }
                _contracts[name] = version;
            }

            // Read types and map to known data types
            if (descriptor.ContractDescriptor.Types is not null)
            {
                foreach ((string name, ContractDescriptorParser.TypeDescriptor type) in descriptor.ContractDescriptor.Types)
                {
                    Dictionary<string, Target.FieldInfo> fieldInfos = [];
                    if (type.Fields is not null)
                    {
                        foreach ((string fieldName, ContractDescriptorParser.FieldDescriptor field) in type.Fields)
                        {
                            fieldInfos[fieldName] = new Target.FieldInfo()
                            {
                                Offset = field.Offset,
                                Type = field.Type is null ? DataType.Unknown : GetDataType(field.Type),
                                TypeName = field.Type
                            };
                        }
                    }
                    Target.TypeInfo typeInfo = new() { Size = type.Size, Fields = fieldInfos };

                    if (seenTypeNames.Contains(name))
                    {
                        throw new InvalidOperationException($"Duplicate type name '{name}' found in contract descriptor.");
                    }
                    seenTypeNames.Add(name);

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
            if (descriptor.ContractDescriptor.Globals is not null)
            {
                foreach ((string name, ContractDescriptorParser.GlobalDescriptor global) in descriptor.ContractDescriptor.Globals)
                {
                    if (seenGlobalNames.Contains(name))
                        throw new InvalidOperationException($"Duplicate global name '{name}' found in contract descriptor.");

                    seenGlobalNames.Add(name);

                    if (global.Indirect)
                    {
                        if (global.NumericValue.Value >= (ulong)descriptor.PointerData.Length)
                            throw new InvalidOperationException($"Invalid pointer data index {global.NumericValue.Value}.");

                        globalValues[name] = new GlobalValue
                        {
                            NumericValue = descriptor.PointerData[global.NumericValue.Value].Value,
                            StringValue = global.StringValue,
                            Type = global.Type
                        };
                    }
                    else // direct
                    {
                        globalValues[name] = new GlobalValue
                        {
                            NumericValue = global.NumericValue,
                            StringValue = global.StringValue,
                            Type = global.Type
                        };
                    }
                }
            }

            _globals = globalValues.AsReadOnly();
        }
    }

    private struct GlobalValue
    {
        public ulong? NumericValue;
        public string? StringValue;
        public string? Type;
    }

    private struct Descriptor
    {
        public Configuration Config { get; init; }
        public ContractDescriptorParser.ContractDescriptor ContractDescriptor { get; init; }
        public TargetPointer[] PointerData { get; init; }
    }

    private static IEnumerable<TargetPointer> GetSubDescriptors(Descriptor descriptor)
    {
        foreach (KeyValuePair<string, ContractDescriptorParser.GlobalDescriptor> subDescriptor in descriptor.ContractDescriptor?.SubDescriptors ?? [])
        {
            if (subDescriptor.Value.Indirect)
            {
                if (subDescriptor.Value.NumericValue.Value >= (ulong)descriptor.PointerData.Length)
                    throw new InvalidOperationException($"Invalid pointer data index {subDescriptor.Value.NumericValue.Value}.");

                yield return descriptor.PointerData[(int)subDescriptor.Value.NumericValue];
            }
        }
    }

    private static bool TryReadAllContractDescriptors(
        ulong address,
        DataTargetDelegates dataTargetDelegates,
        out Descriptor[] descriptors)
    {
        if (!TryReadContractDescriptor(address, dataTargetDelegates, out Descriptor mainDescriptor))
        {
            descriptors = [];
            return false;
        }

        List<Descriptor> allDescriptors = [mainDescriptor];

        foreach (TargetPointer pSubDescriptor in GetSubDescriptors(mainDescriptor))
        {
            if (pSubDescriptor == TargetPointer.Null)
                continue;

            if (!TryReadPointer(pSubDescriptor.Value, mainDescriptor.Config, dataTargetDelegates, out TargetPointer subDescriptorAddress))
                continue;

            if (subDescriptorAddress == TargetPointer.Null)
                continue;

            TryReadAllContractDescriptors(
                subDescriptorAddress.Value,
                dataTargetDelegates,
                out Descriptor[] subDescriptors);

            allDescriptors.AddRange(subDescriptors);
        }

        descriptors = [.. allDescriptors];
        return true;
    }

    // See docs/design/datacontracts/contract-descriptor.md
    private static bool TryReadContractDescriptor(
        ulong address,
        DataTargetDelegates dataTargetDelegates,
        out Descriptor descriptor)
    {
        descriptor = default;

        // Magic - uint64_t
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        if (dataTargetDelegates.ReadFromTarget(address, buffer) < 0)
            return false;

        address += sizeof(ulong);
        ReadOnlySpan<byte> magicLE = "DNCCDAC\0"u8;
        ReadOnlySpan<byte> magicBE = "\0CADCCND"u8;
        bool isLittleEndian = buffer.SequenceEqual(magicLE);
        if (!isLittleEndian && !buffer.SequenceEqual(magicBE))
            return false;

        // Flags - uint32_t
        if (!TryRead(address, isLittleEndian, dataTargetDelegates, out uint flags))
            return false;

        address += sizeof(uint);

        // Bit 1 represents the pointer size. 0 = 64-bit, 1 = 32-bit.
        int pointerSize = (int)(flags & 0x2) == 0 ? sizeof(ulong) : sizeof(uint);

        Configuration config = new Configuration { IsLittleEndian = isLittleEndian, PointerSize = pointerSize };

        // Descriptor size - uint32_t
        if (!TryRead(address, config.IsLittleEndian, dataTargetDelegates, out uint descriptorSize))
            return false;

        address += sizeof(uint);

        // Descriptor - char*
        if (!TryReadPointer(address, config, dataTargetDelegates, out TargetPointer descriptorAddr))
            return false;

        address += (uint)pointerSize;

        // Pointer data count - uint32_t
        if (!TryRead(address, config.IsLittleEndian, dataTargetDelegates, out uint pointerDataCount))
            return false;

        address += sizeof(uint);

        // Padding - uint32_t
        address += sizeof(uint);

        // Pointer data - uintptr_t*
        if (!TryReadPointer(address, config, dataTargetDelegates, out TargetPointer pointerDataAddr))
            return false;

        // Read descriptor
        Span<byte> descriptorBuffer = descriptorSize <= StackAllocByteThreshold
            ? stackalloc byte[(int)descriptorSize]
            : new byte[(int)descriptorSize];
        if (dataTargetDelegates.ReadFromTarget(descriptorAddr.Value, descriptorBuffer) < 0)
            return false;

        ContractDescriptorParser.ContractDescriptor? contractDescriptor = ContractDescriptorParser.ParseCompact(descriptorBuffer);
        if (contractDescriptor is null)
            return false;

        // Read pointer data
        TargetPointer[] pointerData = new TargetPointer[pointerDataCount];
        for (int i = 0; i < pointerDataCount; i++)
        {
            if (!TryReadPointer(pointerDataAddr.Value + (uint)(i * pointerSize), config, dataTargetDelegates, out pointerData[i]))
                return false;
        }

        descriptor = new Descriptor
        {
            Config = config,
            ContractDescriptor = contractDescriptor,
            PointerData = pointerData
        };

        return true;
    }

    private static DataType GetDataType(string type)
    {
        if (Enum.TryParse(type, false, out DataType dataType) && Enum.IsDefined(dataType))
            return dataType;

        return DataType.Unknown;
    }

    public override int PointerSize => _config.PointerSize;
    public override bool IsLittleEndian => _config.IsLittleEndian;

    public override bool TryGetThreadContext(ulong threadId, uint contextFlags, Span<byte> buffer)
    {
        // Underlying API only supports 32-bit thread IDs, mask off top 32 bits
        int hr = _dataTargetDelegates.GetThreadContext((uint)(threadId & uint.MaxValue), contextFlags, buffer);
        return hr == 0;
    }

    /// <summary>
    /// Read a value from the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    /// <exception cref="VirtualReadException">Thrown when the read operation fails</exception>
    public override T Read<T>(ulong address)
    {
        if (!TryRead(address, _config.IsLittleEndian, _dataTargetDelegates, out T value))
            throw new VirtualReadException($"Failed to read {typeof(T)} at 0x{address:x8}.");

        return value;
    }

    /// <summary>
    /// Read a value from the target in little endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    public override T ReadLittleEndian<T>(ulong address)
    {
        if (!TryRead(address, true, _dataTargetDelegates, out T value))
            throw new VirtualReadException($"Failed to read {typeof(T)} at 0x{address:x8}.");

        return value;
    }

    /// <summary>
    /// Read a value from the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>True if read succeeds, false otherwise.</returns>
    public override bool TryRead<T>(ulong address, out T value)
    {
        value = default;
        if (!TryRead(address, _config.IsLittleEndian, _dataTargetDelegates, out T readValue))
            return false;

        value = readValue;
        return true;
    }

    private static bool TryRead<T>(ulong address, bool isLittleEndian, DataTargetDelegates dataTargetDelegates, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        value = default;
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        if (dataTargetDelegates.ReadFromTarget(address, buffer) < 0)
            return false;

        return isLittleEndian
            ? T.TryReadLittleEndian(buffer, !IsSigned<T>(), out value)
            : T.TryReadBigEndian(buffer, !IsSigned<T>(), out value);
    }

    /// <summary>
    /// Write a value to the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to write</typeparam>
    /// <param name="address">Address to start writing to</param>
    public override void Write<T>(ulong address, T value)
    {
        if (!TryWrite(address, _config.IsLittleEndian, _dataTargetDelegates, value))
            throw new InvalidOperationException($"Failed to write {typeof(T)} at 0x{address:x8}.");
    }

    private static bool TryWrite<T>(ulong address, bool isLittleEndian, DataTargetDelegates dataTargetDelegates, T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        int bytesWritten = default;
        bool success = isLittleEndian
            ? value.TryWriteLittleEndian(buffer, out bytesWritten)
            : value.TryWriteBigEndian(buffer, out bytesWritten);
        if (!success || bytesWritten != buffer.Length || dataTargetDelegates.WriteToTarget(address, buffer) < 0)
            return false;

        return true;
    }

    private static T Read<T>(ReadOnlySpan<byte> bytes, bool isLittleEndian) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (sizeof(T) != bytes.Length)
            throw new ArgumentException(nameof(bytes));

        T value;
        if (isLittleEndian)
        {
            T.TryReadLittleEndian(bytes, !IsSigned<T>(), out value);
        }
        else
        {
            T.TryReadBigEndian(bytes, !IsSigned<T>(), out value);
        }
        return value;
    }

    public override void ReadBuffer(ulong address, Span<byte> buffer)
    {
        if (!TryReadBuffer(address, buffer))
            throw new VirtualReadException($"Failed to read {buffer.Length} bytes at 0x{address:x8}.");
    }

    private bool TryReadBuffer(ulong address, Span<byte> buffer)
    {
        return _dataTargetDelegates.ReadFromTarget(address, buffer) >= 0;
    }

    public override void WriteBuffer(ulong address, Span<byte> buffer)
    {
        if (!TryWriteBuffer(address, buffer))
            throw new InvalidOperationException($"Failed to write {buffer.Length} bytes at 0x{address:x8}.");
    }

    private bool TryWriteBuffer(ulong address, Span<byte> buffer)
    {
        return _dataTargetDelegates.WriteToTarget(address, buffer) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
    }

    /// <summary>
    /// Read a pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Pointer read from the target</returns>
    public override TargetPointer ReadPointer(ulong address)
    {
        if (!TryReadPointer(address, _config, _dataTargetDelegates, out TargetPointer pointer))
            throw new VirtualReadException($"Failed to read pointer at 0x{address:x8}.");

        return pointer;
    }

    public override TargetPointer ReadPointerFromSpan(ReadOnlySpan<byte> bytes)
    {
        if (_config.PointerSize == sizeof(uint))
        {
            return new TargetPointer(Read<uint>(bytes.Slice(0, sizeof(uint)), _config.IsLittleEndian));
        }
        else
        {
            return new TargetPointer(Read<ulong>(bytes.Slice(0, sizeof(ulong)), _config.IsLittleEndian));
        }
    }

    public override TargetCodePointer ReadCodePointer(ulong address)
    {
        TypeInfo codePointerTypeInfo = GetTypeInfo(DataType.CodePointer);
        if (codePointerTypeInfo.Size is sizeof(uint))
        {
            return new TargetCodePointer(Read<uint>(address));
        }
        else if (codePointerTypeInfo.Size is sizeof(ulong))
        {
            return new TargetCodePointer(Read<ulong>(address));
        }
        throw new VirtualReadException($"Failed to read code pointer at 0x{address:x8} because CodePointer size is not 4 or 8");
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

    /// <summary>
    /// Read a null-terminated UTF-8 string from the target
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>
    public override string ReadUtf8String(ulong address)
    {
        // Read characters until we find the null terminator
        ulong end = address;
        while (Read<byte>(end) != 0)
        {
            end += sizeof(byte);
        }

        int length = (int)(end - address);
        if (length == 0)
            return string.Empty;

        Span<byte> span = length <= StackAllocByteThreshold
            ? stackalloc byte[length]
            : new byte[length];
        ReadBuffer(address, span);
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>
    /// Read a null-terminated UTF-16 string from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>
    public override string ReadUtf16String(ulong address)
    {
        // Read characters until we find the null terminator
        ulong end = address;
        while (Read<char>(end) != 0)
        {
            end += sizeof(char);
        }

        int length = (int)(end - address);
        if (length == 0)
            return string.Empty;

        Span<byte> span = length <= StackAllocByteThreshold
            ? stackalloc byte[length]
            : new byte[length];
        ReadBuffer(address, span);
        string result = _config.IsLittleEndian
            ? Encoding.Unicode.GetString(span)
            : Encoding.BigEndianUnicode.GetString(span);
        return result;
    }

    /// <summary>
    /// Read a native unsigned integer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    public override TargetNUInt ReadNUInt(ulong address)
    {
        if (!TryReadNUInt(address, _config, _dataTargetDelegates, out ulong value))
            throw new VirtualReadException($"Failed to read nuint at 0x{address:x8}.");

        return new TargetNUInt(value);
    }

    private static bool TryReadPointer(ulong address, Configuration config, DataTargetDelegates dataTargetDelegates, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;
        if (!TryReadNUInt(address, config, dataTargetDelegates, out ulong value))
            return false;

        pointer = new TargetPointer(value);
        return true;
    }

    private static bool TryReadNUInt(ulong address, Configuration config, DataTargetDelegates dataTargetDelegates, out ulong value)
    {
        value = 0;
        if (config.PointerSize == sizeof(uint)
            && TryRead(address, config.IsLittleEndian, dataTargetDelegates, out uint value32))
        {
            value = value32;
            return true;
        }
        else if (config.PointerSize == sizeof(ulong)
            && TryRead(address, config.IsLittleEndian, dataTargetDelegates, out ulong value64))
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
    public override bool IsAlignedToPointerSize(TargetPointer pointer)
        => IsAligned(pointer.Value, _config.PointerSize);

    #region reading globals

    public override bool TryReadGlobal<T>(string name, [NotNullWhen(true)] out T? value)
        => TryReadGlobal<T>(name, out value, out _);

    public bool TryReadGlobal<T>(string name, [NotNullWhen(true)] out T? value, out string? type) where T : struct, INumber<T>
    {
        value = null;
        type = null;
        if (!_globals.TryGetValue(name, out GlobalValue global) || global.NumericValue is null)
        {
            // Not found or does not contain a numeric value
            return false;
        }
        type = global.Type;
        value = T.CreateChecked(global.NumericValue.Value);
        return true;
    }

    public override T ReadGlobal<T>(string name)
        => ReadGlobal<T>(name, out _);

    public T ReadGlobal<T>(string name, out string? type) where T : struct, INumber<T>
    {
        if (!TryReadGlobal(name, out T? value, out type))
            throw new InvalidOperationException($"Failed to read global {typeof(T)} '{name}'.");

        return value.Value;
    }

    public override bool TryReadGlobalPointer(string name, [NotNullWhen(true)] out TargetPointer? value)
        => TryReadGlobalPointer(name, out value, out _);

    public bool TryReadGlobalPointer(string name, [NotNullWhen(true)] out TargetPointer? value, out string? type)
    {
        value = null;
        if (!TryReadGlobal(name, out ulong? innerValue, out type))
            return false;

        value = new TargetPointer(innerValue.Value);
        return true;
    }

    public override TargetPointer ReadGlobalPointer(string name)
        => ReadGlobalPointer(name, out _);

    public TargetPointer ReadGlobalPointer(string name, out string? type)
    {
        if (!TryReadGlobalPointer(name, out TargetPointer? value, out type))
            throw new InvalidOperationException($"Failed to read global pointer '{name}'.");

        return value.Value;
    }

    public override string ReadGlobalString(string name)
        => ReadStringGlobal(name, out _);

    public string ReadStringGlobal(string name, out string? type)
    {
        if (!TryReadStringGlobal(name, out string? value, out type))
            throw new InvalidOperationException($"Failed to read string global '{name}'.");

        return value;
    }

    public override bool TryReadGlobalString(string name, [NotNullWhen(true)] out string? value)
        => TryReadStringGlobal(name, out value, out _);

    public bool TryReadStringGlobal(string name, [NotNullWhen(true)] out string? value, out string? type)
    {
        value = null;
        type = null;
        if (!_globals.TryGetValue(name, out GlobalValue global) || global.StringValue is null)
        {
            // Not found or does not contain a string value
            return false;
        }
        type = global.Type;
        value = global.StringValue;
        return true;
    }

    #endregion

    public override TypeInfo GetTypeInfo(DataType type)
    {
        if (!_knownTypes.TryGetValue(type, out Target.TypeInfo typeInfo))
            throw new InvalidOperationException($"Failed to get type info for '{type}'");

        return typeInfo;
    }

    public Target.TypeInfo GetTypeInfo(string type)
    {
        if (_types.TryGetValue(type, out Target.TypeInfo typeInfo))
            return typeInfo;

        DataType dataType = GetDataType(type);
        if (dataType is not DataType.Unknown)
            return GetTypeInfo(dataType);

        throw new InvalidOperationException($"Failed to get type info for '{type}'");
    }

    internal bool TryGetContractVersion(string contractName, out int version)
    {
        foreach (var kvp in _contracts)
        {
            var name = kvp.Key;
            var value = kvp.Value;
            Console.WriteLine($"Contract: {name}, Version: {value}");
        }
        return _contracts.TryGetValue(contractName, out version);
    }

    /// <summary>
    /// Store of addresses that have already been read into corresponding data models.
    /// This is simply used to avoid re-processing data on every request.
    /// </summary>
    public sealed class DataCache : Target.IDataCache
    {
        private readonly ContractDescriptorTarget _target;
        private readonly Dictionary<(ulong, Type), object?> _readDataByAddress = [];

        public DataCache(ContractDescriptorTarget target)
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

        public void Clear()
        {
            _readDataByAddress.Clear();
        }
    }

    private readonly struct DataTargetDelegates(
        ReadFromTargetDelegate readFromTarget,
        WriteToTargetDelegate writeToTarget,
        GetTargetThreadContextDelegate getThreadContext)
    {
        public int ReadFromTarget(ulong address, Span<byte> buffer)
        {
            return readFromTarget(address, buffer);
        }

        public int ReadFromTarget(ulong address, byte* buffer, uint bytesToRead)
            => readFromTarget(address, new Span<byte>(buffer, checked((int)bytesToRead)));

        public int GetThreadContext(uint threadId, uint contextFlags, Span<byte> buffer)
        {
            return getThreadContext(threadId, contextFlags, buffer);
        }
        public int WriteToTarget(ulong address, Span<byte> buffer)
        {
            return writeToTarget(address, buffer);
        }
    }
}
