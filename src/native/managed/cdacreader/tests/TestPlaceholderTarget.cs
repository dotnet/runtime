// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

/// <summary>
/// A base class implementation of ITarget that throws NotImplementedException for all methods.
/// </summary>
public class TestPlaceholderTarget : ITarget
{
    private protected Contracts.IRegistry contractRegistry;
    private protected ITarget.IDataCache dataCache;
    private protected Dictionary<DataType, ITarget.TypeInfo> typeInfoCache;

#region Setup
    public TestPlaceholderTarget(MockTarget.Architecture arch)
    {
        IsLittleEndian = arch.IsLittleEndian;
        PointerSize = arch.Is64Bit ? 8 : 4;
        contractRegistry = new TestRegistry();;
        dataCache = new TestDataCache();
        typeInfoCache = null;
    }

    internal void SetContracts(Contracts.IRegistry contracts)
    {
        contractRegistry = contracts;
    }

    internal void SetDataCache(ITarget.IDataCache cache)
    {
        dataCache = cache;
    }

    internal void SetTypeInfoCache(Dictionary<DataType, ITarget.TypeInfo> cache)
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

    ITarget.TypeInfo ITarget.GetTypeInfo(DataType dataType) => typeInfoCache != null ? GetTypeInfoImpl(dataType) : throw new NotImplementedException();

    private protected virtual ITarget.TypeInfo GetTypeInfoImpl(DataType dataType)
    {
        if (typeInfoCache!.TryGetValue(dataType, out var info))
        {
            return info;
        }
        throw new NotImplementedException();
    }

    ITarget.IDataCache ITarget.ProcessedData => dataCache;
    Contracts.IRegistry ITarget.Contracts => contractRegistry;

    internal class TestRegistry : Contracts.IRegistry
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

        Contracts.IException Contracts.IRegistry.Exception => ExceptionContract ?? throw new NotImplementedException();
        Contracts.ILoader Contracts.IRegistry.Loader => LoaderContract ?? throw new NotImplementedException();
        Contracts.IEcmaMetadata Contracts.IRegistry.EcmaMetadata => EcmaMetadataContract ?? throw new NotImplementedException();
        Contracts.IObject Contracts.IRegistry.Object => ObjectContract ?? throw new NotImplementedException();
        Contracts.IThread Contracts.IRegistry.Thread => ThreadContract ?? throw new NotImplementedException();
        Contracts.IRuntimeTypeSystem Contracts.IRegistry.RuntimeTypeSystem => RuntimeTypeSystemContract ?? throw new NotImplementedException();
        Contracts.IDacStreams Contracts.IRegistry.DacStreams => DacStreamsContract ?? throw new NotImplementedException();
        Contracts.ICodeVersions Contracts.IRegistry.CodeVersions => CodeVersionsContract ?? throw new NotImplementedException();
        Contracts.IPrecodeStubs Contracts.IRegistry.PrecodeStubs => PrecodeStubsContract ?? throw new NotImplementedException();
        Contracts.IExecutionManager Contracts.IRegistry.ExecutionManager => ExecutionManagerContract ?? throw new NotImplementedException();
        Contracts.IReJIT Contracts.IRegistry.ReJIT => ReJITContract ?? throw new NotImplementedException();
    }

    internal class TestDataCache : ITarget.IDataCache
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
