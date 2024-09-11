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

    public IException Exception => GetContract<FException, IException>();
    public ILoader Loader => GetContract<FLoader, ILoader>();
    public IEcmaMetadata EcmaMetadata => GetContract<FEcmaMetadata, IEcmaMetadata>();
    public IObject Object => GetContract<FObject, IObject>();
    public IThread Thread => GetContract<FThread, IThread>();
    public IRuntimeTypeSystem RuntimeTypeSystem => GetContract<FRuntimeTypeSystem, IRuntimeTypeSystem>();
    public IDacStreams DacStreams => GetContract<FDacStreams, IDacStreams>();
    public ICodeVersions CodeVersions => GetContract<FCodeVersions, ICodeVersions>();
    public IPrecodeStubs PrecodeStubs => GetContract<FPrecodeStubs, IPrecodeStubs>();
    public IExecutionManager ExecutionManager => GetContract<FExecutionManager, IExecutionManager>();
    public IReJIT ReJIT => GetContract<FReJIT, IReJIT>();

    private TProduct GetContract<TFactory, TProduct>() where TProduct : IContract where TFactory : IContractFactory<TProduct>
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
}
