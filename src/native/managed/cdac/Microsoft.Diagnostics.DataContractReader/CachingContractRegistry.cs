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
    private IException? _exception;
    private IExternalMemoryHandles? _externalMemoryHandles;
    private ILoader? _loader;
    private IEcmaMetadata? _ecmaMetadata;
    private IObject? _object;
    private IThread? _thread;
    private IWindowsErrorReporting? _windowsErrorReporting;
    private IRuntimeTypeSystem? _runtimeTypeSystem;
    private IDacStreams? _dacStreams;
    private IExecutionManager? _executionManager;
    private ICodeVersions? _codeVersions;
    private IPlatformMetadata? _platformMetadata;
    private IFeatureFlags? _featureFlags;
    private IPrecodeStubs? _precodeStubs;
    private IReJIT? _reJIT;
    private IStackWalk? _stackWalk;
    private IRuntimeInfo? _runtimeInfo;
    private IComWrappers? _comWrappers;
    private IDebugInfo? _debugInfo;
    private ISHash? _sHash;
    private IGC? _gc;
    private IGCInfo? _gcInfo;
    private INotifications? _notifications;
    private ICallingConvention? _callingConvention;
    private ICodeNotifications? _codeNotifications;
    private ISignature? _signature;
    private ISyncBlock? _syncBlock;
    private IBuiltInCOM? _builtInCOM;
    private IObjectiveCMarshal? _objectiveCMarshal;
    private IConditionalWeakTable? _conditionalWeakTable;
    private IManagedTypeSource? _managedTypeSource;
    private IAuxiliarySymbols? _auxiliarySymbols;
    private IDebugger? _debugger;
    private IStressLog? _stressLog;
    private IRuntimeMutableTypeSystem? _runtimeMutableTypeSystem;

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

    // These built-in contracts are accessed frequently enough that caching them here and avoiding the
    // registry dictionary lookup significantly improves performance.
    public override IException Exception => _exception ??= GetContract<IException>();
    public override IExternalMemoryHandles ExternalMemoryHandles => _externalMemoryHandles ??= GetContract<IExternalMemoryHandles>();
    public override ILoader Loader => _loader ??= GetContract<ILoader>();
    public override IEcmaMetadata EcmaMetadata => _ecmaMetadata ??= GetContract<IEcmaMetadata>();
    public override IObject Object => _object ??= GetContract<IObject>();
    public override IThread Thread => _thread ??= GetContract<IThread>();
    public override IWindowsErrorReporting WindowsErrorReporting => _windowsErrorReporting ??= GetContract<IWindowsErrorReporting>();
    public override IRuntimeTypeSystem RuntimeTypeSystem => _runtimeTypeSystem ??= GetContract<IRuntimeTypeSystem>();
    public override IDacStreams DacStreams => _dacStreams ??= GetContract<IDacStreams>();
    public override IExecutionManager ExecutionManager => _executionManager ??= GetContract<IExecutionManager>();
    public override ICodeVersions CodeVersions => _codeVersions ??= GetContract<ICodeVersions>();
    public override IPlatformMetadata PlatformMetadata => _platformMetadata ??= GetContract<IPlatformMetadata>();
    public override IFeatureFlags FeatureFlags => _featureFlags ??= GetContract<IFeatureFlags>();
    public override IPrecodeStubs PrecodeStubs => _precodeStubs ??= GetContract<IPrecodeStubs>();
    public override IReJIT ReJIT => _reJIT ??= GetContract<IReJIT>();
    public override IStackWalk StackWalk => _stackWalk ??= GetContract<IStackWalk>();
    public override IRuntimeInfo RuntimeInfo => _runtimeInfo ??= GetContract<IRuntimeInfo>();
    public override IComWrappers ComWrappers => _comWrappers ??= GetContract<IComWrappers>();
    public override IDebugInfo DebugInfo => _debugInfo ??= GetContract<IDebugInfo>();
    public override ISHash SHash => _sHash ??= GetContract<ISHash>();
    public override IGC GC => _gc ??= GetContract<IGC>();
    public override IGCInfo GCInfo => _gcInfo ??= GetContract<IGCInfo>();
    public override INotifications Notifications => _notifications ??= GetContract<INotifications>();
    public override ICallingConvention CallingConvention => _callingConvention ??= GetContract<ICallingConvention>();
    public override ICodeNotifications CodeNotifications => _codeNotifications ??= GetContract<ICodeNotifications>();
    public override ISignature Signature => _signature ??= GetContract<ISignature>();
    public override ISyncBlock SyncBlock => _syncBlock ??= GetContract<ISyncBlock>();
    public override IBuiltInCOM BuiltInCOM => _builtInCOM ??= GetContract<IBuiltInCOM>();
    public override IObjectiveCMarshal ObjectiveCMarshal => _objectiveCMarshal ??= GetContract<IObjectiveCMarshal>();
    public override IConditionalWeakTable ConditionalWeakTable => _conditionalWeakTable ??= GetContract<IConditionalWeakTable>();
    public override IManagedTypeSource ManagedTypeSource => _managedTypeSource ??= GetContract<IManagedTypeSource>();
    public override IAuxiliarySymbols AuxiliarySymbols => _auxiliarySymbols ??= GetContract<IAuxiliarySymbols>();
    public override IDebugger Debugger => _debugger ??= GetContract<IDebugger>();
    public override IStressLog StressLog => _stressLog ??= GetContract<IStressLog>();
    public override IRuntimeMutableTypeSystem RuntimeMutableTypeSystem => _runtimeMutableTypeSystem ??= GetContract<IRuntimeMutableTypeSystem>();

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

    public override bool TryGetContractVersion<TContract>([NotNullWhen(true)] out string? version)
        => _tryGetContractVersion(TContract.Name, out version);

    public override bool TryValidate<TContract>([NotNullWhen(false)] out System.Exception? failureException)
    {
        failureException = null;

        // An already-instantiated contract is, by definition, supported.
        if (_contracts.ContainsKey(typeof(TContract)))
        {
            return true;
        }

        // Presence-only validation: confirm this cDAC can provide the contract at the version the
        // target advertises, but never invoke the creator. Instantiating a contract can read target
        // memory and chain into other contracts, and a partial capture (for example a minidump) may
        // legitimately be missing the data a given contract needs while still being fully usable for
        // others (stack walks and similar). Reading target memory here could spuriously fail
        // validation and reject an otherwise serviceable dump, so resolve the creator without running it.
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
