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
    public delegate bool TryGetContractVersionDelegate(string contractName, [NotNullWhen(true)] out string? version);

    private readonly Dictionary<Type, IContract> _contracts = [];
    private readonly Dictionary<(Type, string), Func<Target, IContract>> _creators = [];
    private readonly HashSet<(Type, string)> _unsupportedVersions = [];
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

    public override void Register<TContract>(string version, Func<Target, TContract> creator)
    {
        _creators[(typeof(TContract), version)] = t => creator(t);
    }

    public override void RegisterUnsupported<TContract>(string version)
    {
        _unsupportedVersions.Add((typeof(TContract), version));
    }

    public override bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract, [NotNullWhen(false)] out System.Exception? failureException)
    {
        contract = default!;
        failureException = null;
        if (_contracts.TryGetValue(typeof(TContract), out IContract? cached))
        {
            contract = (TContract)cached;
            return true;
        }

        Func<Target, IContract>? creator;
        if (_tryGetContractVersion(TContract.Name, out string? version))
        {
            if (!_creators.TryGetValue((typeof(TContract), version), out creator))
            {
                failureException = _unsupportedVersions.Contains((typeof(TContract), version))
                    ? new ContractObsoleteException(TContract.Name, version)
                    : new ContractUnrecognizedException(TContract.Name, version);
                return false;
            }
        }
        else if (!_creators.TryGetValue((typeof(TContract), string.Empty), out creator))
        {
            failureException = new ContractMissingException(TContract.Name);
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

    public override void Flush(FlushScope scope)
    {
        foreach (IContract contract in _contracts.Values)
        {
            contract.Flush(scope);
        }
    }
}
