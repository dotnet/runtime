// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class Registry
{
    // Contracts that have already been created for a target.
    // Items should not be removed from this, only added.
    private readonly Dictionary<Type, IContract> _contracts = [];
    private readonly Target _target;

    public Registry(Target target)
    {
        _target = target;
    }

    public IException Exception => GetContract<IFException, IException>();
    public ILoader Loader => GetContract<IFLoader, ILoader>();
    public IEcmaMetadata EcmaMetadata => GetContract<IFEcmaMetadata, IEcmaMetadata>();
    public IObject Object => GetContract<IFObject, IObject>();
    public IThread Thread => GetContract<IFThread, IThread>();
    public IRuntimeTypeSystem RuntimeTypeSystem => GetContract<IFRuntimeTypeSystem, IRuntimeTypeSystem>();
    public IDacStreams DacStreams => GetContract<IFDacStreams, IDacStreams>();
    public ICodeVersions CodeVersions => GetContract<IFCodeVersions, ICodeVersions>();
    public IPrecodeStubs PrecodeStubs => GetContract<IPrecodeStubs>();
    public IExecutionManager ExecutionManager => GetContract<IExecutionManager>();
    public IReJIT ReJIT => GetContract<IReJIT>();

    private TProduct GetContract<TFactory, TProduct>() where TProduct : IContract where TFactory : IContractFactory<TFactory, TProduct>
    {
        if (_contracts.TryGetValue(typeof(TProduct), out IContract? contractMaybe))
            return (TProduct)contractMaybe;

        if (!_target.TryGetContractVersion(TProduct.Name, out int version))
            throw new NotImplementedException();

        // Create and register the contract
        TProduct contract = TFactory.CreateContract(_target, version);
        if (_contracts.TryAdd(typeof(TProduct), contract))
            return contract;

        // Contract was already registered by someone else
        return (TProduct)_contracts[typeof(TProduct)];
    }

    private T GetContract<T>() where T : IContractFactory<T>
    {
        return GetContract<T, T>();
    }
}
