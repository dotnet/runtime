// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
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
    private readonly (string Name, string Value)[] _globalStrings;

    internal delegate int ReadFromTargetDelegate(ulong address, Span<byte> buffer);
    internal delegate int WriteToTargetDelegate(ulong address, Span<byte> buffer);

    private readonly ReadFromTargetDelegate _dataReader;
    private readonly WriteToTargetDelegate? _dataWriter;
    private static readonly UTF8Encoding strictUTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding looseUTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public TestPlaceholderTarget(MockTarget.Architecture arch, ReadFromTargetDelegate reader, Dictionary<DataType, Target.TypeInfo> types = null, (string Name, ulong Value)[] globals = null, (string Name, string Value)[] globalStrings = null, WriteToTargetDelegate? writer = null)
    {
        IsLittleEndian = arch.IsLittleEndian;
        PointerSize = arch.Is64Bit ? 8 : 4;
        _contractRegistry = new TestContractRegistry();
        _dataCache = new DefaultDataCache(this);
        _typeInfoCache = types ?? [];
        _dataReader = reader;
        _dataWriter = writer;
        _globals = globals ?? [];
        _globalStrings = globalStrings ?? [];
    }

    internal void SetContracts(ContractRegistry contracts)
    {
        _contractRegistry = contracts;
    }

    /// <summary>
    /// Creates a <see cref="TestContractRegistry"/> with the given registration action
    /// (defaulting to <see cref="CoreCLRContracts.Register"/>), and sets it as the
    /// contract registry for this target. Returns the registry so callers can call
    /// <see cref="TestContractRegistry.SetVersion{TContract}"/> and
    /// <see cref="TestContractRegistry.SetMock{TContract}"/>.
    /// </summary>
    internal TestContractRegistry SetupContractRegistry(Action<ContractRegistry>? registrations = null)
    {
        var registry = new TestContractRegistry();
        registry.SetTarget(this);
        (registrations ?? CoreCLRContracts.Register)(registry);
        _contractRegistry = registry;
        return registry;
    }

    /// <summary>
    /// Fluent builder for <see cref="TestPlaceholderTarget"/>. Accumulates types,
    /// globals, and contract factories from mock descriptors, then materializes the
    /// target and wires contracts in <see cref="Build"/>.
    /// </summary>
    internal class Builder
    {
        private readonly MockTarget.Architecture _arch;
        private readonly MockMemorySpace.Builder _memBuilder;
        private readonly Dictionary<DataType, Target.TypeInfo> _types = new();
        private readonly List<(string Name, ulong Value)> _globals = new();
        private readonly List<(string Name, string Value)> _globalStrings = new();
        private readonly List<Action<TestContractRegistry>> _contractSetups = new();
        private Action<ContractRegistry> _registrations = CoreCLRContracts.Register;
        private ReadFromTargetDelegate? _readerOverride;

        public Builder(MockTarget.Architecture arch)
        {
            _arch = arch;
            _memBuilder = new MockMemorySpace.Builder(new TargetTestHelpers(arch));
        }

        internal MockMemorySpace.Builder MemoryBuilder => _memBuilder;

        public Builder AddTypes(Dictionary<DataType, Target.TypeInfo> types)
        {
            foreach (var kvp in types)
                _types[kvp.Key] = kvp.Value;
            return this;
        }

        public Builder AddGlobals(params (string Name, ulong Value)[] globals)
        {
            _globals.AddRange(globals);
            return this;
        }

        public Builder AddGlobalStrings(params (string Name, string Value)[] globalStrings)
        {
            _globalStrings.AddRange(globalStrings);
            return this;
        }

        public Builder UseReader(ReadFromTargetDelegate reader)
        {
            _readerOverride = reader;
            return this;
        }

        public Builder UseRegistrations(Action<ContractRegistry> registrations)
        {
            _registrations = registrations;
            return this;
        }

        public Builder AddContract<TContract>(int version) where TContract : IContract
        {
            _contractSetups.Add(registry => registry.SetVersion<TContract>(version));
            return this;
        }

        public Builder AddMockContract<TContract>(TContract mock) where TContract : IContract
        {
            _contractSetups.Add(registry => registry.SetMock(mock));
            return this;
        }

        public Builder AddMockContract<TContract>(Mock<TContract> mock) where TContract : class, IContract
        {
            _contractSetups.Add(registry => registry.SetMock(mock.Object));
            return this;
        }

        public TestPlaceholderTarget Build()
        {
            var memoryContext = _memBuilder.GetMemoryContext();
            var target = new TestPlaceholderTarget(
                _arch,
                _readerOverride ?? memoryContext.ReadFromTarget,
                _types,
                _globals.ToArray(),
                _globalStrings.ToArray(),
                memoryContext.WriteToTarget);

            var registry = new TestContractRegistry();
            registry.SetTarget(target);
            _registrations(registry);

            foreach (var setup in _contractSetups)
                setup(registry);

            target.SetContracts(registry);

            return target;
        }
    }

    public override int PointerSize { get; }
    public override bool IsLittleEndian { get; }

    public override bool IsAlignedToPointerSize(TargetPointer pointer)
    {
        return (pointer.Value & (ulong)(PointerSize - 1)) == 0;
    }

    public override bool TryReadGlobalPointer(string name, [NotNullWhen(true)] out TargetPointer? value)
    {
        value = null;
        foreach (var global in _globals)
        {
            if (global.Name == name)
            {
                value = new TargetPointer(global.Value);
                return true;
            }
        }
        return false;
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
    public override bool TryReadPointer(ulong address, out TargetPointer value) => DefaultTryReadPointer(address, out value);
    public override TargetCodePointer ReadCodePointer(ulong address) => DefaultReadCodePointer(address);
    public override bool TryReadCodePointer(ulong address, out TargetCodePointer value)
    {
        value = default;
        if (!DefaultTryReadPointer(address, out TargetPointer ptr))
            return false;
        value = new TargetCodePointer(ptr);
        return true;
    }
    public override void ReadBuffer(ulong address, Span<byte> buffer)
    {
        if (_dataReader(address, buffer) < 0)
            throw new VirtualReadException($"Failed to read {buffer.Length} bytes at 0x{address:x8}.");
    }
    public override void WriteBuffer(ulong address, Span<byte> buffer)
    {
        if (_dataWriter is null)
            throw new NotImplementedException();
        if (_dataWriter(address, buffer) < 0)
            throw new InvalidOperationException($"Failed to write {buffer.Length} bytes at 0x{address:x8}.");
    }

    public override string ReadUtf8String(ulong address, bool strict = false)
    {
        // Read bytes until we find the null terminator
        ulong end = address;
        while (Read<byte>(end) != 0)
        {
            end += sizeof(byte);
        }

        int length = (int)(end - address);
        if (length == 0)
            return string.Empty;

        Span<byte> span = new byte[length];
        ReadBuffer(address, span);
        return strict ? strictUTF8Encoding.GetString(span) : looseUTF8Encoding.GetString(span);
    }
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

    public override bool TryReadGlobal<T>(string name, [NotNullWhen(true)] out T? value)
    {
        value = default;
        foreach (var global in _globals)
        {
            if (global.Name == name)
            {
                value = T.CreateChecked(global.Value);
                return true;
            }
        }
        return false;
    }
    public override T ReadGlobal<T>(string name)
    {
        foreach (var global in _globals)
        {
            if (global.Name == name)
                return T.CreateChecked(global.Value);
        }

        throw new NotImplementedException();
    }

    public override string ReadGlobalString(string name)
    {
        if (TryReadGlobalString(name, out string? value))
        {
            return value;
        }

        throw new NotImplementedException();
    }

    public override bool TryReadGlobalString(string name, [NotNullWhen(true)] out string? value)
    {
        value = null;

        // first check global strings
        foreach (var global in _globalStrings)
        {
            if (global.Name == name)
            {
                value = global.Value;
                return true;
            }
        }

        return false;
    }

    public override T Read<T>(ulong address) => DefaultRead<T>(address);

    public override T ReadLittleEndian<T>(ulong address)
    {
        T value = default;
        unsafe
        {
            Span<byte> buffer = stackalloc byte[sizeof(T)];
            if (_dataReader(address, buffer) < 0)
                throw new VirtualReadException($"Failed to read {typeof(T)} at 0x{address:x8}.");

            T.TryReadLittleEndian(buffer, !IsSigned<T>(), out value);
        }
        return value;
    }

    public override bool TryRead<T>(ulong address, out T value)
    {
        value = default;
        if (!DefaultTryRead(address, out T readValue))
            return false;

        value = readValue;
        return true;
    }

    public override void Write<T>(ulong address, T value)
    {
        if (_dataWriter is null)
            throw new NotImplementedException();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
        bool success = IsLittleEndian
            ? value.TryWriteLittleEndian(buffer, out int bytesWritten)
            : value.TryWriteBigEndian(buffer, out bytesWritten);

        if (!success || bytesWritten != buffer.Length)
            throw new InvalidOperationException($"Failed to write {typeof(T)} to buffer.");
        WriteBuffer(address, buffer);
    }

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
            throw new VirtualReadException($"Failed to read {typeof(T)} at 0x{address:x8}.");
        return value;
    }

    protected TargetPointer DefaultReadPointer(ulong address)
    {
        if (!DefaultTryReadPointer(address, out TargetPointer pointer))
            throw new VirtualReadException($"Failed to read pointer at 0x{address:x8}.");

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
            throw new VirtualReadException($"Failed to read nuint at 0x{address:x8}.");

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
            if (!found)
            {
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

    internal sealed class TestContractRegistry : ContractRegistry
    {
        private readonly Dictionary<(Type, int), Func<Target, IContract>> _creators = new();
        private readonly Dictionary<Type, int> _versions = new();
        private readonly Dictionary<Type, IContract> _mocks = new();
        private readonly Dictionary<Type, IContract> _resolved = new();
        private Target _target = null!;

        public void SetTarget(Target target) => _target = target;

        public void SetVersion<TContract>(int version) where TContract : IContract
            => _versions[typeof(TContract)] = version;

        public void SetMock<TContract>(TContract mock) where TContract : IContract
            => _mocks[typeof(TContract)] = mock;

        public override void Register<TContract>(int version, Func<Target, TContract> creator)
            => _creators[(typeof(TContract), version)] = t => creator(t);

        public override bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract, out string? failureReason)
        {
            contract = default!;
            failureReason = null;
            if (_resolved.TryGetValue(typeof(TContract), out var cached))
            {
                contract = (TContract)cached;
                return true;
            }

            IContract resolved;
            if (_mocks.TryGetValue(typeof(TContract), out var mock))
            {
                resolved = mock;
            }
            else if (_versions.TryGetValue(typeof(TContract), out int version))
            {
                if (!_creators.TryGetValue((typeof(TContract), version), out var creator))
                {
                    failureReason = $"Target supports contract '{typeof(TContract).Name}' version {version}, but no implementation is registered for that version.";
                    return false;
                }

                resolved = creator(_target);
            }
            else
            {
                failureReason = $"Contract '{typeof(TContract).Name}' is not supported by the target.";
                return false;
            }

            _resolved[typeof(TContract)] = resolved;
            contract = (TContract)resolved;
            return true;
        }

        public override void Flush() { }
    }

}
