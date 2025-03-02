// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// A mock implementation of Target that has basic implementations of getting types/globals and reading data
/// </summary>
internal class TestPlaceholderTarget : Target
{
    private ContractRegistry _contractRegistry;
    private readonly Target.IDataCache _dataCache;
    private readonly Dictionary<DataType, Target.TypeInfo> _typeInfoCache;
    private readonly (string Name, ulong Value)[] _globals;

    internal delegate int ReadFromTargetDelegate(ulong address, Span<byte> buffer);

    private readonly ReadFromTargetDelegate _dataReader;

    public TestPlaceholderTarget(MockTarget.Architecture arch, ReadFromTargetDelegate reader, Dictionary<DataType, Target.TypeInfo> types = null, (string Name, ulong Value)[] globals = null)
    {
        IsLittleEndian = arch.IsLittleEndian;
        PointerSize = arch.Is64Bit ? 8 : 4;
        Platform = Target.CorDebugPlatform.CORDB_PLATFORM_MAC_AMD64;
        _contractRegistry = new Mock<ContractRegistry>().Object;
        _dataCache = new DefaultDataCache(this);
        _typeInfoCache = types ?? [];
        _dataReader = reader;
        _globals = globals ?? [];
    }

    internal void SetContracts(ContractRegistry contracts)
    {
        _contractRegistry = contracts;
    }

    public override int PointerSize { get; }
    public override bool IsLittleEndian { get; }
    public override CorDebugPlatform Platform { get; }

    public override bool IsAlignedToPointerSize(TargetPointer pointer)
    {
        return (pointer.Value & (ulong)(PointerSize - 1)) == 0;
    }

    public override TargetPointer ReadGlobalPointer(string name)
    {
        foreach (var global in _globals)
        {
            if (global.Name == name)
                return new TargetPointer(global.Value);
        }

        throw new NotImplementedException();
    }

    public override TargetPointer ReadPointer(ulong address) => DefaultReadPointer(address);
    public override TargetCodePointer ReadCodePointer(ulong address) => DefaultReadCodePointer(address);
    public override void ReadBuffer(ulong address, Span<byte> buffer)
    {
        if (_dataReader(address, buffer) < 0)
            throw new InvalidOperationException($"Failed to read {buffer.Length} bytes at 0x{address:x8}.");
    }

    public override string ReadUtf8String(ulong address) => throw new NotImplementedException();
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

        Span<byte> span = new byte[length];
        ReadBuffer(address, span);
        string result = IsLittleEndian
            ? Encoding.Unicode.GetString(span)
            : Encoding.BigEndianUnicode.GetString(span);
        return result;
    }

    public override TargetNUInt ReadNUInt(ulong address) => DefaultReadNUInt(address);
    public override T ReadGlobal<T>(string name)
    {
        foreach (var global in _globals)
        {
            if (global.Name == name)
                return T.CreateChecked(global.Value);
        }

        throw new NotImplementedException();
    }

    public override T Read<T>(ulong address) => DefaultRead<T>(address);

#region subclass reader helpers

    /// <summary>
    /// Basic utility to read a value from memory, all the DefaultReadXXX methods call this.
    /// </summary>
    protected unsafe bool DefaultTryRead<T>(ulong address, out T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        value = default;
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        if (_dataReader(address, buffer) < 0)
            return false;

        value = ReadFromSpan<T>(buffer, IsLittleEndian);
        return true;
    }

    internal unsafe static T ReadFromSpan<T>(ReadOnlySpan<byte> bytes, bool isLittleEndian) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
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

    internal unsafe static void WriteToSpan<T>(T value, bool isLittleEndian, Span<byte> dest) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (sizeof(T) != dest.Length)
            throw new ArgumentException(nameof(dest));

        if (isLittleEndian)
        {
            value.WriteLittleEndian(dest);
        }
        else
        {
            value.WriteBigEndian(dest);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool IsSigned<T>() where T : struct, INumberBase<T>, IMinMaxValue<T>
    {
        return T.IsNegative(T.MinValue);
    }

    protected T DefaultRead<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        if (!DefaultTryRead(address, out T value))
            throw new InvalidOperationException($"Failed to read {typeof(T)} at 0x{address:x8}.");
        return value;
    }

    protected TargetPointer DefaultReadPointer (ulong address)
    {
        if (!DefaultTryReadPointer(address, out TargetPointer pointer))
            throw new InvalidOperationException($"Failed to read pointer at 0x{address:x8}.");

        return pointer;
    }

    protected bool DefaultTryReadPointer(ulong address, out TargetPointer pointer)
    {
        pointer = TargetPointer.Null;
        if (!DefaultTryReadNUInt(address, out ulong value))
            return false;

        pointer = new TargetPointer(value);
        return true;
    }

    protected bool DefaultTryReadNUInt(ulong address, out ulong value)
    {
        value = 0;
        if (PointerSize == sizeof(uint)
            && DefaultTryRead(address, out uint value32))
        {
            value = value32;
            return true;
        }
        else if (PointerSize == sizeof(ulong)
            && DefaultTryRead(address, out ulong value64))
        {
            value = value64;
            return true;
        }

        return false;
    }

    protected TargetNUInt DefaultReadNUInt(ulong address)
    {
        if (!DefaultTryReadNUInt(address, out ulong value))
            throw new InvalidOperationException($"Failed to read nuint at 0x{address:x8}.");

        return new TargetNUInt(value);
    }

    protected TargetCodePointer DefaultReadCodePointer(ulong address)
    {
        return new TargetCodePointer(DefaultReadPointer(address));
    }
#endregion subclass reader helpers

    public override TargetPointer ReadPointerFromSpan(ReadOnlySpan<byte> bytes) => throw new NotImplementedException();

    public override Target.TypeInfo GetTypeInfo(DataType dataType)
    {
        if (_typeInfoCache.TryGetValue(dataType, out var info))
            return info;

        throw new NotImplementedException();
    }

    public override bool TryGetThreadContext(ulong threadId, uint contextFlags, Span<byte> bufferToFill) => throw new NotImplementedException();

    public override Target.IDataCache ProcessedData => _dataCache;
    public override ContractRegistry Contracts => _contractRegistry;

    // A data cache that stores data in a dictionary and calls IData.Create to construct the data.
    private sealed class DefaultDataCache : Target.IDataCache
    {
        private readonly Target _target;
        private readonly Dictionary<(ulong, Type), object?> _readDataByAddress = [];

        public DefaultDataCache(Target target)
        {
            _target = target;
        }

        public T GetOrAdd<T>(TargetPointer address) where T : Data.IData<T>
        {
            if (TryGet(address, out T? result))
                return result;

            T constructed = T.Create(_target, address);
            if (_readDataByAddress.TryAdd((address, typeof(T)), constructed))
                return constructed;

            bool found = TryGet(address, out result);
            if (!found) {
                throw new InvalidOperationException($"Failed to add {typeof(T)} at 0x{address:x8}.");
            }
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

}
