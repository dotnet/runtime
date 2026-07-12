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

        if (!TryResolveCreator(typeof(TContract), TContract.Name, out Func<Target, IContract>? creator, out failureException))
        {
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

    public override bool TryValidate<TContract>([NotNullWhen(false)] out System.Exception? failureException)
    {
        failureException = null;

        // An already-instantiated contract is, by definition, supported.
        if (_contracts.ContainsKey(typeof(TContract)))
        {
            return true;
        }

        // Resolve only — never invoke the creator. Invoking it would read target memory and may
        // chain into other contracts, which must not happen during eager validation.
        return TryResolveCreator(typeof(TContract), TContract.Name, out _, out failureException);
    }

    /// <summary>
    /// Classifies whether a registered creator exists for the target-advertised version of a
    /// contract, without invoking it. Shared by <see cref="TryGetContract{TContract}(out TContract, out System.Exception?)"/>
    /// and <see cref="TryValidate{TContract}(out System.Exception?)"/>.
    /// </summary>
    private bool TryResolveCreator(
        Type contractType,
        string contractName,
        [NotNullWhen(true)] out Func<Target, IContract>? creator,
        [NotNullWhen(false)] out System.Exception? failureException)
    {
        creator = null;
        failureException = null;

        if (!_tryGetContractVersion(contractName, out string? version))
        {
            if (_creators.TryGetValue((contractType, string.Empty), out creator))
            {
                return true;
            }

            failureException = new ContractMissingException(contractName);
            return false;
        }

        if (!_creators.TryGetValue((contractType, version), out creator))
        {
            failureException = _unsupportedVersions.Contains((contractType, version))
                ? new ContractObsoleteException(contractName, version)
                : new ContractUnrecognizedException(contractName, version);
            return false;
        }

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
