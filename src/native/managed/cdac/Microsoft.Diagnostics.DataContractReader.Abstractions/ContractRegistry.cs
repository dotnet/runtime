// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public IException Exception => GetContract<IException>();
    /// <summary>
    /// Gets an instance of the Loader contract for the target.
    /// </summary>
    public ILoader Loader => GetContract<ILoader>();
    /// <summary>
    /// Gets an instance of the EcmaMetadata contract for the target.
    /// </summary>
    public IEcmaMetadata EcmaMetadata => GetContract<IEcmaMetadata>();
    /// <summary>
    /// Gets an instance of the Object contract for the target.
    /// </summary>
    public IObject Object => GetContract<IObject>();
    /// <summary>
    /// Gets an instance of the Thread contract for the target.
    /// </summary>
    public IThread Thread => GetContract<IThread>();
    /// <summary>
    /// Gets an instance of the RuntimeTypeSystem contract for the target.
    /// </summary>
    public IRuntimeTypeSystem RuntimeTypeSystem => GetContract<IRuntimeTypeSystem>();
    /// <summary>
    /// Gets an instance of the DacStreams contract for the target.
    /// </summary>
    public IDacStreams DacStreams => GetContract<IDacStreams>();
    /// <summary>
    /// Gets an instance of the ExecutionManager contract for the target.
    /// </summary>
    public IExecutionManager ExecutionManager => GetContract<IExecutionManager>();
    /// <summary>
    /// Gets an instance of the CodeVersions contract for the target.
    /// </summary>
    public ICodeVersions CodeVersions => GetContract<ICodeVersions>();
    /// <summary>
    /// Gets an instance of the PlatformMetadata contract for the target.
    /// </summary>
    public IPlatformMetadata PlatformMetadata => GetContract<IPlatformMetadata>();
    /// <summary>
    /// Gets an instance of the PrecodeStubs contract for the target.
    /// </summary>
    public IPrecodeStubs PrecodeStubs => GetContract<IPrecodeStubs>();
    /// <summary>
    /// Gets an instance of the ReJIT contract for the target.
    /// </summary>
    public IReJIT ReJIT => GetContract<IReJIT>();
    /// <summary>
    /// Gets an instance of the StackWalk contract for the target.
    /// </summary>
    public IStackWalk StackWalk => GetContract<IStackWalk>();
    /// <summary>
    /// Gets an instance of the RuntimeInfo contract for the target.
    /// </summary>
    public IRuntimeInfo RuntimeInfo => GetContract<IRuntimeInfo>();
    /// <summary>
    /// Gets an instance of the ComWrappers contract for the target.
    /// </summary>
    public IComWrappers ComWrappers => GetContract<IComWrappers>();
    /// Gets an instance of the DebugInfo contract for the target.
    /// </summary>
    public IDebugInfo DebugInfo => GetContract<IDebugInfo>();
    /// <summary>
    /// Gets an instance of the SHash contract for the target.
    /// </summary>
    public ISHash SHash => GetContract<ISHash>();
    /// <summary>
    /// Gets an instance of the GC contract for the target.
    /// </summary>
    public IGC GC => GetContract<IGC>();
    /// <summary>
    /// Gets an instance of the Notifications contract for the target.
    /// </summary>
    public INotifications Notifications => GetContract<INotifications>();
    /// <summary>
    /// Gets an instance of the SignatureDecoder contract for the target.
    /// </summary>
    public ISignatureDecoder SignatureDecoder => GetContract<ISignatureDecoder>();

    public abstract TContract GetContract<TContract>() where TContract : IContract;
}
