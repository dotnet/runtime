// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ContractRegistry : AbstractContractRegistry
{
    // Contracts that have already been created for a target.
    // Items should not be removed from this, only added.
    private readonly Dictionary<Type, IContract> _contracts = [];
    private readonly Dictionary<Type, IContractFactory<IContract>> _factories;
    private readonly ContractDescriptorTarget _target;

    public ContractRegistry(ContractDescriptorTarget target, Action<Dictionary<Type, IContractFactory<IContract>>>? configureFactories = null)
    {
        _target = target;
        _factories = new () {
            [typeof(IException)] = new ExceptionFactory(),
            [typeof(ILoader)] = new LoaderFactory(),
            [typeof(IEcmaMetadata)] = new EcmaMetadataFactory(),
            [typeof(IObject)] = new ObjectFactory(),
            [typeof(IThread)] = new ThreadFactory(),
            [typeof(IRuntimeTypeSystem)] = new RuntimeTypeSystemFactory(),
            [typeof(IDacStreams)] = new DacStreamsFactory(),
        };
        configureFactories?.Invoke(_factories);
    }

    public override IException Exception => GetContract<IException>();
    public override ILoader Loader => GetContract<ILoader>();
    public override IEcmaMetadata EcmaMetadata => GetContract<IEcmaMetadata>();
    public override IObject Object => GetContract<IObject>();
    public override IThread Thread => GetContract<IThread>();
    public override IRuntimeTypeSystem RuntimeTypeSystem => GetContract<IRuntimeTypeSystem>();
    public override IDacStreams DacStreams => GetContract<IDacStreams>();

    private TProduct GetContract<TProduct>() where TProduct : IContract
    {
        if (_contracts.TryGetValue(typeof(TProduct), out IContract? contractMaybe))
            return (TProduct)contractMaybe;

        if (!_target.TryGetContractVersion(TProduct.Name, out int version))
            throw new NotImplementedException();

        if (!_factories.TryGetValue(typeof(TProduct), out IContractFactory<IContract>? factory))
            throw new NotImplementedException();
        // Create and register the contract
        TProduct contract = (TProduct)factory.CreateContract(_target, version);
        if (_contracts.TryAdd(typeof(TProduct), contract))
            return contract;

        // Contract was already registered by someone else
        return (TProduct)_contracts[typeof(TProduct)];
    }
}
