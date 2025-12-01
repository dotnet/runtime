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

    public CachingContractRegistry(Target target, TryGetContractVersionDelegate tryGetContractVersion, IEnumerable<IContractFactory<IContract>> additionalFactories, Action<Dictionary<Type, IContractFactory<IContract>>>? configureFactories = null)
    {
        _target = target;
        _tryGetContractVersion = tryGetContractVersion;
        _factories = new()
        {
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
            [typeof(IRuntimeInfo)] = new RuntimeInfoFactory(),
            [typeof(IComWrappers)] = new ComWrappersFactory(),
            [typeof(IDebugInfo)] = new DebugInfoFactory(),
            [typeof(ISHash)] = new SHashFactory(),
            [typeof(IGC)] = new GCFactory(),
            [typeof(INotifications)] = new NotificationsFactory(),
            [typeof(ISignatureDecoder)] = new SignatureDecoderFactory(),
        };

        foreach (IContractFactory<IContract> factory in additionalFactories)
        {
            _factories[factory.ContractType] = factory;
        }
        configureFactories?.Invoke(_factories);
    }

    public override TContract GetContract<TContract>()
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
