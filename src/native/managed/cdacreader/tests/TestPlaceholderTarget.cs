// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

/// <summary>
/// A base class implementation of Target that throws NotImplementedException for all methods.
/// </summary>
internal class TestPlaceholderTarget : Target
{
    private protected ContractRegistry contractRegistry;
    private protected Target.IDataCache dataCache;
    private protected Dictionary<DataType, Target.TypeInfo> typeInfoCache;

    internal delegate int ReadFromTargetDelegate(ulong address, Span<byte> buffer);

    protected ReadFromTargetDelegate _dataReader = (address, buffer) => throw new NotImplementedException();

#region Setup
    public TestPlaceholderTarget(MockTarget.Architecture arch)
    {
        IsLittleEndian = arch.IsLittleEndian;
        PointerSize = arch.Is64Bit ? 8 : 4;
        contractRegistry = new TestRegistry();;
        dataCache = new TestDataCache();
        typeInfoCache = null;
    }

    internal void SetContracts(ContractRegistry contracts)
    {
        contractRegistry = contracts;
    }

    internal void SetDataCache(Target.IDataCache cache)
    {
        dataCache = cache;
    }

    internal void SetTypeInfoCache(Dictionary<DataType, Target.TypeInfo> cache)
    {
        typeInfoCache = cache;
    }

    internal void SetDataReader(ReadFromTargetDelegate reader)
    {
        _dataReader = reader;
    }
#endregion Setup

    public override int PointerSize { get; }
    public override bool IsLittleEndian { get; }

    public override bool IsAlignedToPointerSize(TargetPointer pointer)
    {
        return (pointer.Value & (ulong)(PointerSize - 1)) == 0;
    }

    public override TargetPointer ReadGlobalPointer(string global) => throw new NotImplementedException();
    public override TargetPointer ReadPointer(ulong address) => DefaultReadPointer(address);
    public override TargetCodePointer ReadCodePointer(ulong address) => throw new NotImplementedException();
    public override void ReadBuffer(ulong address, Span<byte> buffer) => throw new NotImplementedException();
    public override string ReadUtf8String(ulong address) => throw new NotImplementedException();
    public override string ReadUtf16String(ulong address) => throw new NotImplementedException();
    public override TargetNUInt ReadNUInt(ulong address) => DefaultReadNUInt(address);
    public override T ReadGlobal<T>(string name) => throw new NotImplementedException();
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
#endregion subclass reader helpers

    public override TargetPointer ReadPointerFromSpan(ReadOnlySpan<byte> bytes) => throw new NotImplementedException();

    public override Target.TypeInfo GetTypeInfo(DataType dataType) => typeInfoCache != null ? GetTypeInfoImpl(dataType) : throw new NotImplementedException();

    private protected virtual Target.TypeInfo GetTypeInfoImpl(DataType dataType)
    {
        if (typeInfoCache!.TryGetValue(dataType, out var info))
        {
            return info;
        }
        throw new NotImplementedException();
    }

    public override Target.IDataCache ProcessedData => dataCache;
    public override ContractRegistry Contracts => contractRegistry;

    internal class TestRegistry : ContractRegistry
    {
        public TestRegistry() { }
        internal Contracts.IException? ExceptionContract { get; set; }
        internal Contracts.ILoader? LoaderContract { get; set; }
        internal Contracts.IEcmaMetadata? EcmaMetadataContract { get; set; }
        internal Contracts.IObject? ObjectContract { get; set; }
        internal Contracts.IThread? ThreadContract { get; set; }
        internal Contracts.IRuntimeTypeSystem? RuntimeTypeSystemContract { get; set; }
        internal Contracts.IDacStreams? DacStreamsContract { get; set; }

        public override Contracts.IException Exception => ExceptionContract ?? throw new NotImplementedException();
        public override Contracts.ILoader Loader => LoaderContract ?? throw new NotImplementedException();
        public override Contracts.IEcmaMetadata EcmaMetadata => EcmaMetadataContract ?? throw new NotImplementedException();
        public override Contracts.IObject Object => ObjectContract ?? throw new NotImplementedException();
        public override Contracts.IThread Thread => ThreadContract ?? throw new NotImplementedException();
        public override Contracts.IRuntimeTypeSystem RuntimeTypeSystem => RuntimeTypeSystemContract ?? throw new NotImplementedException();
        public override Contracts.IDacStreams DacStreams => DacStreamsContract ?? throw new NotImplementedException();
    }

    internal class TestDataCache : Target.IDataCache
    {
        public TestDataCache() {}

        public virtual T GetOrAdd<T>(TargetPointer address) where T : Data.IData<T>
        {
            if (TryGet(address.Value, out T? data))
            {
                return data;
            }
            return Add<T>(address.Value);
        }

        public virtual bool TryGet<T>(ulong address, [NotNullWhen(true)] out T? data)
        {
            throw new NotImplementedException();
        }

        protected virtual T Add<T>(ulong address) where T : Data.IData<T>
        {
            throw new NotImplementedException();
        }
    }
}
