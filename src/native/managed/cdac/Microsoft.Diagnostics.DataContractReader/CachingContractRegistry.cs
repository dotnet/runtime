// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    public override bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract, out string? failureReason)
    {
        contract = default!;
        failureReason = null;
        if (_contracts.TryGetValue(typeof(TContract), out IContract? cached))
        {
            contract = (TContract)cached;
            return true;
        }

        if (!_tryGetContractVersion(TContract.Name, out int version))
        {
             failureReason = $"Target does not support contract '{typeof(TContract).Name}'.";
            return false;
        }

        if (!_creators.TryGetValue((typeof(TContract), version), out Func<Target, IContract>? creator))
        {
            failureReason = $"Target supports contract '{typeof(TContract).Name}' version {version}, but no implementation is registered for that version.";
            return false;
        }

        contract = (TContract)creator(_target);
        if (_contracts.TryAdd(typeof(TContract), contract))
        {
            return true;
        }

        contract = (TContract)_contracts[typeof(TContract)];
        return true;
    }

    public override void Flush()
    {
        foreach (IContract contract in _contracts.Values)
        {
            contract.Flush();
        }
    }
}
