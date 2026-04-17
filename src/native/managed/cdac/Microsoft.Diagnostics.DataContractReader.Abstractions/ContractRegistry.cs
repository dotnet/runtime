// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.Contracts;


namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// A registry of all the contracts that may be provided by a target.
/// </summary>
public abstract class ContractRegistry
{
    /// <summary>
    /// Gets an instance of the Exception contract for the target.
    /// </summary>
    public virtual IException Exception => GetContract<IException>();
    /// <summary>
    /// Gets an instance of the Loader contract for the target.
    /// </summary>
    public virtual ILoader Loader => GetContract<ILoader>();
    /// <summary>
    /// Gets an instance of the EcmaMetadata contract for the target.
    /// </summary>
    public virtual IEcmaMetadata EcmaMetadata => GetContract<IEcmaMetadata>();
    /// <summary>
    /// Gets an instance of the Object contract for the target.
    /// </summary>
    public virtual IObject Object => GetContract<IObject>();
    /// <summary>
    /// Gets an instance of the Thread contract for the target.
    /// </summary>
    public virtual IThread Thread => GetContract<IThread>();
    /// <summary>
    /// Gets an instance of the RuntimeTypeSystem contract for the target.
    /// </summary>
    public virtual IRuntimeTypeSystem RuntimeTypeSystem => GetContract<IRuntimeTypeSystem>();
    /// <summary>
    /// Gets an instance of the DacStreams contract for the target.
    /// </summary>
    public virtual IDacStreams DacStreams => GetContract<IDacStreams>();
    /// <summary>
    /// Gets an instance of the ExecutionManager contract for the target.
    /// </summary>
    public virtual IExecutionManager ExecutionManager => GetContract<IExecutionManager>();
    /// <summary>
    /// Gets an instance of the CodeVersions contract for the target.
    /// </summary>
    public virtual ICodeVersions CodeVersions => GetContract<ICodeVersions>();
    /// <summary>
    /// Gets an instance of the PlatformMetadata contract for the target.
    /// </summary>
    public virtual IPlatformMetadata PlatformMetadata => GetContract<IPlatformMetadata>();
    /// <summary>
    /// Gets an instance of the PrecodeStubs contract for the target.
    /// </summary>
    public virtual IPrecodeStubs PrecodeStubs => GetContract<IPrecodeStubs>();
    /// <summary>
    /// Gets an instance of the ReJIT contract for the target.
    /// </summary>
    public virtual IReJIT ReJIT => GetContract<IReJIT>();
    /// <summary>
    /// Gets an instance of the StackWalk contract for the target.
    /// </summary>
    public virtual IStackWalk StackWalk => GetContract<IStackWalk>();
    /// <summary>
    /// Gets an instance of the RuntimeInfo contract for the target.
    /// </summary>
    public virtual IRuntimeInfo RuntimeInfo => GetContract<IRuntimeInfo>();
    /// <summary>
    /// Gets an instance of the ComWrappers contract for the target.
    /// </summary>
    public virtual IComWrappers ComWrappers => GetContract<IComWrappers>();
    /// Gets an instance of the DebugInfo contract for the target.
    /// </summary>
    public virtual IDebugInfo DebugInfo => GetContract<IDebugInfo>();
    /// <summary>
    /// Gets an instance of the SHash contract for the target.
    /// </summary>
    public virtual ISHash SHash => GetContract<ISHash>();
    /// <summary>
    /// Gets an instance of the GC contract for the target.
    /// </summary>
    public virtual IGC GC => GetContract<IGC>();
    /// <summary>
    /// Gets an instance of the GCInfo contract for the target.
    /// </summary>
    public virtual IGCInfo GCInfo => GetContract<IGCInfo>();
    /// <summary>
    /// Gets an instance of the Notifications contract for the target.
    /// </summary>
    public virtual INotifications Notifications => GetContract<INotifications>();
    /// <summary>
    /// Gets an instance of the SignatureDecoder contract for the target.
    /// </summary>
    public virtual ISignatureDecoder SignatureDecoder => GetContract<ISignatureDecoder>();
    /// <summary>
    /// Gets an instance of the SyncBlock contract for the target.
    /// </summary>
    public virtual ISyncBlock SyncBlock => GetContract<ISyncBlock>();
    /// <summary>
    /// Gets an instance of the BuiltInCOM contract for the target.
    /// </summary>
    public virtual IBuiltInCOM BuiltInCOM => GetContract<IBuiltInCOM>();
    /// <summary>
    /// Gets an instance of the ConditionalWeakTable contract for the target.
    /// </summary>
    public virtual IConditionalWeakTable ConditionalWeakTable => GetContract<IConditionalWeakTable>();
    /// <summary>
    /// Gets an instance of the AuxiliarySymbols contract for the target.
    /// </summary>
    public virtual IAuxiliarySymbols AuxiliarySymbols => GetContract<IAuxiliarySymbols>();
    /// <summary>
    /// Gets an instance of the Debugger contract for the target.
    /// </summary>
    public virtual IDebugger Debugger => GetContract<IDebugger>();

    /// <summary>
    /// Attempts to get an instance of the requested contract for the target.
    /// </summary>
    /// <typeparam name="TContract">The contract type to retrieve.</typeparam>
    /// <param name="contract">
    /// When this method returns <see langword="true"/>, contains the requested contract instance; otherwise, <see langword="null"/>.
    /// </param>
    /// <param name="failureReason">
    /// When this method returns <see langword="false"/>, contains a human-readable explanation of why the contract could not be retrieved; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the requested contract is present and was retrieved successfully; <see langword="false"/> if the contract is not present or registered"/>.
    /// </returns>
    public abstract bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract, out string? failureReason) where TContract : IContract;

    public TContract GetContract<TContract>() where TContract : IContract
    {
        if (!TryGetContract(out TContract contract, out string? failureReason))
        {
            throw new NotImplementedException($"Contract '{typeof(TContract).Name}' is not supported by the target. Reason: {failureReason ?? "no reason provided"}");
        }
        return contract;
    }

    public bool TryGetContract<TContract>([NotNullWhen(true)] out TContract contract) where TContract : IContract
    {
        return TryGetContract(out contract, out _);
    }

    /// <summary>
    /// Register a contract implementation for a specific version.
    /// External packages use this to add contract versions or entirely new contract interfaces.
    /// </summary>
    public abstract void Register<TContract>(int version, Func<Target, TContract> creator)
        where TContract : IContract;

    /// <summary>
    /// Flush all cached data held by contracts in this registry.
    /// Called when the target process state may have changed (e.g. on resume).
    /// </summary>
    public abstract void Flush();
}
