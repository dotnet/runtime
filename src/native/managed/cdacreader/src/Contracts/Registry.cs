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

    public IException Exception => GetContract<IException>();
    public ILoader Loader => GetContract<ILoader>();
    public IObject Object => GetContract<IObject>();
    public IThread Thread => GetContract<IThread>();
    public IRuntimeTypeSystem RuntimeTypeSystem => GetContract<IRuntimeTypeSystem>();
    public IDacStreams DacStreams => GetContract<IDacStreams>();
    public IStressLog StressLog => GetContract<IStressLog>();

    private T GetContract<T>() where T : IContract
    {
        if (_contracts.TryGetValue(typeof(T), out IContract? contractMaybe))
            return (T)contractMaybe;

        if (!_target.TryGetContractVersion(T.Name, out int version))
            throw new NotImplementedException();

        // Create and register the contract
        IContract contract = T.Create(_target, version);
        if (_contracts.TryAdd(typeof(T), contract))
            return (T)contract;

        // Contract was already registered by someone else
        return (T)_contracts[typeof(T)];
    }
}
