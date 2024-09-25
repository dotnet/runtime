// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

/// <summary>
/// A base class implementation of Target that throws NotImplementedException for all methods.
/// </summary>
internal class TestPlaceholderTarget : Target
{
    private protected Contracts.AbstractContractRegistry contractRegistry;
    private protected Target.IDataCache dataCache;
    private protected Dictionary<DataType, Target.TypeInfo> typeInfoCache;

#region Setup
    public TestPlaceholderTarget(MockTarget.Architecture arch)
    {
        IsLittleEndian = arch.IsLittleEndian;
        PointerSize = arch.Is64Bit ? 8 : 4;
        contractRegistry = new TestRegistry();;
        dataCache = new TestDataCache();
        typeInfoCache = null;
    }

    internal void SetContracts(Contracts.AbstractContractRegistry contracts)
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
#endregion Setup

    public override int PointerSize { get; }
    public override bool IsLittleEndian { get; }

    public override bool IsAlignedToPointerSize(TargetPointer pointer)
    {
        return (pointer.Value & (ulong)(PointerSize - 1)) == 0;
    }

    public override TargetPointer ReadGlobalPointer(string global) => throw new NotImplementedException();
    public override TargetPointer ReadPointer(ulong address) => throw new NotImplementedException();
    public override TargetCodePointer ReadCodePointer(ulong address) => throw new NotImplementedException();
    public override void ReadBuffer(ulong address, Span<byte> buffer) => throw new NotImplementedException();
    public override string ReadUtf8String(ulong address) => throw new NotImplementedException();
    public override string ReadUtf16String(ulong address) => throw new NotImplementedException();
    public override TargetNUInt ReadNUInt(ulong address) => throw new NotImplementedException();
    public override T ReadGlobal<T>(string name) => throw new NotImplementedException();
    public override T Read<T>(ulong address) => throw new NotImplementedException();

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
    public override Contracts.AbstractContractRegistry Contracts => contractRegistry;

    internal class TestRegistry : Contracts.AbstractContractRegistry
    {
        public TestRegistry() { }
        internal Contracts.IException? ExceptionContract { get; set; }
        internal Contracts.ILoader? LoaderContract { get; set; }
        internal Contracts.IEcmaMetadata? EcmaMetadataContract { get; set; }
        internal Contracts.IObject? ObjectContract { get; set; }
        internal Contracts.IThread? ThreadContract { get; set; }
        internal Contracts.IRuntimeTypeSystem? RuntimeTypeSystemContract { get; set; }
        internal Contracts.IDacStreams? DacStreamsContract { get; set; }
        internal Contracts.ICodeVersions? CodeVersionsContract { get; set; }
        internal Contracts.IPrecodeStubs? PrecodeStubsContract { get; set; }
        internal Contracts.IExecutionManager? ExecutionManagerContract { get; set; }
        internal Contracts.IReJIT? ReJITContract { get; set; }

        public override Contracts.IException Exception => ExceptionContract ?? throw new NotImplementedException();
        public override Contracts.ILoader Loader => LoaderContract ?? throw new NotImplementedException();
        public override Contracts.IEcmaMetadata EcmaMetadata => EcmaMetadataContract ?? throw new NotImplementedException();
        public override Contracts.IObject Object => ObjectContract ?? throw new NotImplementedException();
        public override Contracts.IThread Thread => ThreadContract ?? throw new NotImplementedException();
        public override Contracts.IRuntimeTypeSystem RuntimeTypeSystem => RuntimeTypeSystemContract ?? throw new NotImplementedException();
        public override Contracts.IDacStreams DacStreams => DacStreamsContract ?? throw new NotImplementedException();
        public override Contracts.ICodeVersions CodeVersions => CodeVersionsContract ?? throw new NotImplementedException();
        public override Contracts.IPrecodeStubs PrecodeStubs => PrecodeStubsContract ?? throw new NotImplementedException();
        public override Contracts.IExecutionManager ExecutionManager => ExecutionManagerContract ?? throw new NotImplementedException();
        public override Contracts.IReJIT ReJIT => ReJITContract ?? throw new NotImplementedException();
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
