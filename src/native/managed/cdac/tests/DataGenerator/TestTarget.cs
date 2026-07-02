// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Data;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.DataGeneratorTests;

/// <summary>
/// A minimal <see cref="Target"/> subclass for testing the IData source
/// generator's emitted code in isolation. Only implements the abstract
/// surface that the generator-produced ctors and write methods actually call;
/// everything else throws <see cref="NotImplementedException"/>.
/// </summary>
/// <remarks>
/// Deliberately does NOT depend on the cdac production test framework
/// (TestPlaceholderTarget, MockMemorySpace, TestContractRegistry, etc.) -- the
/// goal is a small, focused mock targeted at generator output only.
/// </remarks>
internal sealed class TestTarget : Target
{
    private readonly Dictionary<string, TypeInfo> _nativeTypes = new();
    private readonly Dictionary<string, TargetPointer> _globals = new();
    private readonly SortedDictionary<ulong, byte[]> _memory = new();
    private readonly TestContractRegistry _contracts;
    private readonly TestDataCache _processedData;

    public TestTarget(int pointerSize = 8, bool isLittleEndian = true)
    {
        PointerSize = pointerSize;
        IsLittleEndian = isLittleEndian;
        ManagedTypeSourceMock = new Mock<IManagedTypeSource>();
        _contracts = new TestContractRegistry(ManagedTypeSourceMock.Object);
        _processedData = new TestDataCache(this);
    }

    /// <summary>
    /// Configurable mock for the IManagedTypeSource contract. Tests call
    /// <c>Setup(...)</c> on this to register managed-side TypeInfos.
    /// </summary>
    public Mock<IManagedTypeSource> ManagedTypeSourceMock { get; }

    public override int PointerSize { get; }
    public override bool IsLittleEndian { get; }
    public override ContractRegistry Contracts => _contracts;
    public override IDataCache ProcessedData => _processedData;

    // --- Native TypeInfo registration --------------------------------

    /// <summary>
    /// Register a native cdac TypeInfo with the given name, total instance
    /// size, and field offsets.
    /// </summary>
    public TestTarget AddNativeType(string name, uint? size, params (string Field, int Offset)[] fields)
    {
        var dict = new Dictionary<string, FieldInfo>();
        foreach (var (field, offset) in fields)
        {
            dict[field] = new FieldInfo { Offset = offset };
        }
        _nativeTypes[name] = new TypeInfo { Size = size, Fields = dict };
        return this;
    }

    /// <summary>
    /// Register a managed-side TypeInfo on the mocked IManagedTypeSource.
    /// </summary>
    public TestTarget AddManagedType(string fullyQualifiedName, uint? size, params (string Field, int Offset)[] fields)
    {
        var dict = new Dictionary<string, FieldInfo>();
        foreach (var (field, offset) in fields)
        {
            dict[field] = new FieldInfo { Offset = offset };
        }
        var info = new TypeInfo { Size = size, Fields = dict };

        ManagedTypeSourceMock
            .Setup(m => m.TryGetTypeInfo(fullyQualifiedName, out It.Ref<TypeInfo>.IsAny))
            .Returns(new TryGetTypeInfoDelegate((string _, out TypeInfo i) => { i = info; return true; }));
        ManagedTypeSourceMock
            .Setup(m => m.GetTypeInfo(fullyQualifiedName))
            .Returns(info);
        return this;
    }

    private delegate bool TryGetTypeInfoDelegate(string name, out TypeInfo info);

    // --- Global pointer registration ----------------------------------

    /// <summary>
    /// Register a native global pointer value. Used to test static field
    /// resolution via the native descriptor path.
    /// </summary>
    public TestTarget AddGlobal(string name, ulong address)
    {
        _globals[name] = new TargetPointer(address);
        return this;
    }

    /// <summary>
    /// Register a managed static field on the mocked IManagedTypeSource.
    /// When the generated code calls TryGetStaticFieldAddress with the
    /// given type and field name, it will return the specified address.
    /// </summary>
    public TestTarget AddManagedStaticField(string typeName, string fieldName, ulong address)
    {
        var addr = new TargetPointer(address);
        ManagedTypeSourceMock
            .Setup(m => m.TryGetStaticFieldAddress(typeName, fieldName, out It.Ref<TargetPointer>.IsAny))
            .Returns(new TryGetStaticFieldAddressDelegate((string _, string _, out TargetPointer a) =>
            {
                a = addr;
                return true;
            }));
        return this;
    }

    private delegate bool TryGetStaticFieldAddressDelegate(string typeName, string fieldName, out TargetPointer address);

    // --- TypeInfo lookup --------------------------------------------

    public override TypeInfo GetTypeInfo(string typeName)
    {
        if (_nativeTypes.TryGetValue(typeName, out TypeInfo info))
            return info;
        throw new InvalidOperationException($"TestTarget: no native TypeInfo registered for '{typeName}'.");
    }

    public override bool TryGetTypeInfo(string typeName, out TypeInfo info)
        => _nativeTypes.TryGetValue(typeName, out info);

    // --- Memory backing ---------------------------------------------

    /// <summary>
    /// Allocate a chunk of memory at the given address and stage initial
    /// bytes there. Subsequent reads/writes against the chunk use this
    /// byte buffer.
    /// </summary>
    public TestTarget Allocate(ulong address, int size, params (int Offset, byte[] Bytes)[] writes)
    {
        byte[] buffer = new byte[size];
        foreach (var (offset, bytes) in writes)
        {
            bytes.CopyTo(buffer, offset);
        }
        _memory[address] = buffer;
        return this;
    }

    /// <summary>
    /// Inspect the byte buffer at the given allocation address. Useful for
    /// asserting writes hit the right offsets.
    /// </summary>
    public ReadOnlySpan<byte> Bytes(ulong address) => _memory[address];

    private Span<byte> GetSpan(ulong address, int length)
    {
        foreach (var kv in _memory)
        {
            ulong start = kv.Key;
            int chunkLength = kv.Value.Length;
            if (address >= start && address + (ulong)length <= start + (ulong)chunkLength)
            {
                return kv.Value.AsSpan((int)(address - start), length);
            }
        }
        throw new InvalidOperationException($"TestTarget: no allocation covers [0x{address:x}, 0x{address + (ulong)length:x}).");
    }

    // --- Primitive reads called by extension methods ----------------

    public override T Read<T>(ulong address)
    {
        Span<byte> span = GetSpan(address, System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
        return System.Runtime.InteropServices.MemoryMarshal.Read<T>(span);
    }

    public override T ReadLittleEndian<T>(ulong address)
        => Read<T>(address); // tests use little-endian only.

    public override TargetPointer ReadPointer(ulong address)
    {
        return PointerSize switch
        {
            8 => new TargetPointer(Read<ulong>(address)),
            4 => new TargetPointer(Read<uint>(address)),
            _ => throw new InvalidOperationException($"Unsupported pointer size {PointerSize}."),
        };
    }

    public override TargetCodePointer ReadCodePointer(ulong address)
        => new TargetCodePointer(ReadPointer(address));

    public override TargetNUInt ReadNUInt(ulong address)
        => new TargetNUInt(PointerSize == 8 ? Read<ulong>(address) : Read<uint>(address));

    public override TargetNInt ReadNInt(ulong address)
        => new TargetNInt(PointerSize == 8 ? Read<long>(address) : Read<int>(address));

    public override void Write<T>(ulong address, T value)
    {
        Span<byte> span = GetSpan(address, System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
        System.Runtime.InteropServices.MemoryMarshal.Write(span, in value);
    }

    public override void WritePointer(ulong address, TargetPointer value)
    {
        if (PointerSize == 8) Write<ulong>(address, value.Value);
        else Write<uint>(address, (uint)value.Value);
    }

    public override void WriteNUInt(ulong address, TargetNUInt value)
    {
        if (PointerSize == 8) Write<ulong>(address, value.Value);
        else Write<uint>(address, (uint)value.Value);
    }

    // --- Global pointer reads ----------------------------------------

    public override TargetPointer ReadGlobalPointer(string global)
    {
        if (_globals.TryGetValue(global, out TargetPointer value))
            return value;
        throw new InvalidOperationException($"TestTarget: no global registered for '{global}'.");
    }

    public override bool TryReadGlobalPointer(string name, [NotNullWhen(true)] out TargetPointer? value)
    {
        if (_globals.TryGetValue(name, out TargetPointer v))
        {
            value = v;
            return true;
        }
        value = null;
        return false;
    }

    // --- Everything else throws --------------------------------------

    public override bool TryReadPointer(ulong address, out TargetPointer value) => throw new NotImplementedException();
    public override bool TryReadCodePointer(ulong address, out TargetCodePointer value) => throw new NotImplementedException();
    public override void ReadBuffer(ulong address, Span<byte> buffer) => throw new NotImplementedException();
    public override void WriteBuffer(ulong address, Span<byte> buffer) => throw new NotImplementedException();
    public override string ReadUtf8String(ulong address, bool strict = false) => throw new NotImplementedException();
    public override string ReadUtf16String(ulong address) => throw new NotImplementedException();
    public override bool TryReadGlobalString(string name, [NotNullWhen(true)] out string? value) => throw new NotImplementedException();
    public override string ReadGlobalString(string name) => throw new NotImplementedException();
    public override T ReadGlobal<T>(string name) => throw new NotImplementedException();
    public override bool TryReadGlobal<T>(string name, [NotNullWhen(true)] out T? value) => throw new NotImplementedException();
    public override bool TryRead<T>(ulong address, out T value) => throw new NotImplementedException();
    public override TargetPointer ReadPointerFromSpan(ReadOnlySpan<byte> bytes) => throw new NotImplementedException();
    public override bool IsAlignedToPointerSize(TargetPointer pointer) => throw new NotImplementedException();
    public override bool TryGetThreadContext(ulong threadId, uint contextFlags, Span<byte> buffer) => throw new NotImplementedException();
    public override bool TrySetThreadContext(ulong threadId, ReadOnlySpan<byte> context) => throw new NotImplementedException();

    // --- Stub ContractRegistry -------------------------------------

    private sealed class TestContractRegistry : ContractRegistry
    {
        private readonly IManagedTypeSource _managedTypeSource;

        public TestContractRegistry(IManagedTypeSource managedTypeSource)
        {
            _managedTypeSource = managedTypeSource;
        }

        public override IManagedTypeSource ManagedTypeSource => _managedTypeSource;

        public override bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract, out string? failureReason)
        {
            if (typeof(TContract) == typeof(IManagedTypeSource))
            {
                contract = (TContract)_managedTypeSource;
                failureReason = null;
                return true;
            }
            contract = default!;
            failureReason = "Not registered in TestContractRegistry.";
            return false;
        }

        public override void Register<TContract>(string version, Func<Target, TContract> creator)
            => throw new NotImplementedException();

        public override void Flush(FlushScope scope) { }
    }

    // --- Trivial IDataCache for ReadDataField support ----------------

    private sealed class TestDataCache : IDataCache
    {
        private readonly Target _target;
        private readonly Dictionary<(ulong, Type), object?> _cache = new();

        public TestDataCache(Target target) { _target = target; }

        public T GetOrAdd<T>(TargetPointer address) where T : IData<T>
        {
            var key = (address.Value, typeof(T));
            if (_cache.TryGetValue(key, out object? cached))
                return (T)cached!;
            T value = T.Create(_target, address);
            _cache[key] = value;
            return value;
        }

        public bool TryGet<T>(ulong address, [NotNullWhen(true)] out T? data)
        {
            if (_cache.TryGetValue((address, typeof(T)), out object? cached))
            {
                data = (T)cached!;
                return true;
            }
            data = default;
            return false;
        }

        public void Clear() => _cache.Clear();
    }
}
