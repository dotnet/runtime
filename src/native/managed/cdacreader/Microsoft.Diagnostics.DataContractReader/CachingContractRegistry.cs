// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Contract registry that caches contracts for a target
/// </summary>
internal sealed class CachingContractRegistry : ContractRegistry
{
    public delegate bool TryGetContractVersionDelegate(string contractName, out int version);
    // Contracts that have already been created for a target.
    // Items should not be removed from this, only added.
    private readonly Dictionary<Type, IContract> _contracts = [];
    private readonly Dictionary<Type, IContractFactory<IContract>> _factories;
    private readonly Target _target;
    private readonly TryGetContractVersionDelegate _tryGetContractVersion;

    public CachingContractRegistry(Target target, TryGetContractVersionDelegate tryGetContractVersion, Action<Dictionary<Type, IContractFactory<IContract>>>? configureFactories = null)
    {
        _target = target;
        _tryGetContractVersion = tryGetContractVersion;
        _factories = new() {
            [typeof(IException)] = new ExceptionFactory(),
            [typeof(ILoader)] = new LoaderFactory(),
            [typeof(IEcmaMetadata)] = new EcmaMetadataFactory(),
            [typeof(IObject)] = new ObjectFactory(),
            [typeof(IThread)] = new ThreadFactory(),
            [typeof(IRuntimeTypeSystem)] = new RuntimeTypeSystemFactory(),
            [typeof(IDacStreams)] = new DacStreamsFactory(),
            [typeof(IExecutionManager)] = new ExecutionManagerFactory(),
            [typeof(ICodeVersions)] = new CodeVersionsFactory(),
            [typeof(IPlatformMetadata)] = new PlatformMetadataFactory(),
            [typeof(IPrecodeStubs)] = new PrecodeStubsFactory(),
            [typeof(IReJIT)] = new ReJITFactory(),
            [typeof(IStackWalk)] = new StackWalkFactory(),
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
    public override IExecutionManager ExecutionManager => GetContract<IExecutionManager>();
    public override ICodeVersions CodeVersions => GetContract<ICodeVersions>();
    public override IPlatformMetadata PlatformMetadata => GetContract<IPlatformMetadata>();
    public override IPrecodeStubs PrecodeStubs => GetContract<IPrecodeStubs>();
    public override IReJIT ReJIT => GetContract<IReJIT>();
    public override IStackWalk StackWalk => GetContract<IStackWalk>();

    private TContract GetContract<TContract>() where TContract : IContract
    {
        if (_contracts.TryGetValue(typeof(TContract), out IContract? contractMaybe))
            return (TContract)contractMaybe;

        if (!_tryGetContractVersion(TContract.Name, out int version))
            throw new NotImplementedException();

        if (!_factories.TryGetValue(typeof(TContract), out IContractFactory<IContract>? factory))
            throw new NotImplementedException();
        // Create and register the contract
        TContract contract = (TContract)factory.CreateContract(_target, version);
        if (_contracts.TryAdd(typeof(TContract), contract))
            return contract;

        // Contract was already registered by someone else
        return (TContract)_contracts[typeof(TContract)];
    }
}
