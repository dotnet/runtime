// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Contract registry that resolves contracts by (type, version) lookup.
/// Has no knowledge of any specific contracts — all implementations are
/// registered from outside via <see cref="Register{TContract}"/>.
/// </summary>
internal sealed class CachingContractRegistry : ContractRegistry
{
    public delegate bool TryGetContractVersionDelegate(string contractName, out int version);

    private readonly Dictionary<Type, IContract> _contracts = [];
    private readonly Dictionary<(Type, int), Func<Target, IContract>> _creators = [];
    private readonly Target _target;
    private readonly TryGetContractVersionDelegate _tryGetContractVersion;

    public CachingContractRegistry(Target target, TryGetContractVersionDelegate tryGetContractVersion, params Action<ContractRegistry>[] contractRegistrations)
    {
        _target = target;
        _tryGetContractVersion = tryGetContractVersion;

        foreach (Action<ContractRegistry> register in contractRegistrations)
        {
            register(this);
        }
    }

    public override void Register<TContract>(int version, Func<Target, TContract> creator)
    {
        _creators[(typeof(TContract), version)] = t => creator(t);
    }

    public override TContract GetContract<TContract>()
    {
        if (_contracts.TryGetValue(typeof(TContract), out IContract? cached))
            return (TContract)cached;

        if (!_tryGetContractVersion(TContract.Name, out int version))
            throw new NotImplementedException($"Contract '{TContract.Name}' is not present in the contract descriptor.");

        if (!_creators.TryGetValue((typeof(TContract), version), out Func<Target, IContract>? creator))
            throw new NotImplementedException($"No implementation registered for contract '{TContract.Name}' version {version}.");

        TContract contract = (TContract)creator(_target);
        if (_contracts.TryAdd(typeof(TContract), contract))
            return contract;

        return (TContract)_contracts[typeof(TContract)];
    }

    public override void Flush()
    {
        foreach (IContract contract in _contracts.Values)
        {
            contract.Flush();
        }
    }
}
