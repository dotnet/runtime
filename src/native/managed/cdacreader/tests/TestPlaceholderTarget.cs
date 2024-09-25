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
public class TestPlaceholderTarget : Target
{
    private protected Contracts.IContractRegistry contractRegistry;
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

    internal void SetContracts(Contracts.IContractRegistry contracts)
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

    public int PointerSize { get; }
    public bool IsLittleEndian { get; }

    public bool IsAlignedToPointerSize(TargetPointer pointer)
    {
        return (pointer.Value & (ulong)(PointerSize - 1)) == 0;
    }

    public virtual TargetPointer ReadGlobalPointer(string global) => throw new NotImplementedException();
    public virtual TargetPointer ReadPointer(ulong address) => throw new NotImplementedException();
    public virtual TargetCodePointer ReadCodePointer(ulong address) => throw new NotImplementedException();
    public virtual void ReadBuffer(ulong address, Span<byte> buffer) => throw new NotImplementedException();
    public virtual string ReadUtf8String(ulong address) => throw new NotImplementedException();
    public virtual string ReadUtf16String(ulong address) => throw new NotImplementedException();
    public virtual TargetNUInt ReadNUInt(ulong address) => throw new NotImplementedException();
    public virtual T ReadGlobal<T>(string name) where T : struct, INumber<T> => throw new NotImplementedException();
    public virtual T Read<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> => throw new NotImplementedException();

    Target.TypeInfo Target.GetTypeInfo(DataType dataType) => typeInfoCache != null ? GetTypeInfoImpl(dataType) : throw new NotImplementedException();

    private protected virtual Target.TypeInfo GetTypeInfoImpl(DataType dataType)
    {
        if (typeInfoCache!.TryGetValue(dataType, out var info))
        {
            return info;
        }
        throw new NotImplementedException();
    }

    Target.IDataCache Target.ProcessedData => dataCache;
    Contracts.IContractRegistry Target.Contracts => contractRegistry;

    internal class TestRegistry : Contracts.IContractRegistry
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

        Contracts.IException Contracts.IContractRegistry.Exception => ExceptionContract ?? throw new NotImplementedException();
        Contracts.ILoader Contracts.IContractRegistry.Loader => LoaderContract ?? throw new NotImplementedException();
        Contracts.IEcmaMetadata Contracts.IContractRegistry.EcmaMetadata => EcmaMetadataContract ?? throw new NotImplementedException();
        Contracts.IObject Contracts.IContractRegistry.Object => ObjectContract ?? throw new NotImplementedException();
        Contracts.IThread Contracts.IContractRegistry.Thread => ThreadContract ?? throw new NotImplementedException();
        Contracts.IRuntimeTypeSystem Contracts.IContractRegistry.RuntimeTypeSystem => RuntimeTypeSystemContract ?? throw new NotImplementedException();
        Contracts.IDacStreams Contracts.IContractRegistry.DacStreams => DacStreamsContract ?? throw new NotImplementedException();
        Contracts.ICodeVersions Contracts.IContractRegistry.CodeVersions => CodeVersionsContract ?? throw new NotImplementedException();
        Contracts.IPrecodeStubs Contracts.IContractRegistry.PrecodeStubs => PrecodeStubsContract ?? throw new NotImplementedException();
        Contracts.IExecutionManager Contracts.IContractRegistry.ExecutionManager => ExecutionManagerContract ?? throw new NotImplementedException();
        Contracts.IReJIT Contracts.IContractRegistry.ReJIT => ReJITContract ?? throw new NotImplementedException();
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
