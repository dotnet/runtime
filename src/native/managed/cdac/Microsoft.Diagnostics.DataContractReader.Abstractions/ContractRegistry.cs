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
    /// Gets an instance of the Notifications contract for the target.
    /// </summary>
    public virtual INotifications Notifications => GetContract<INotifications>();
    /// <summary>
    /// Gets an instance of the SignatureDecoder contract for the target.
    /// </summary>
    public virtual ISignatureDecoder SignatureDecoder => GetContract<ISignatureDecoder>();

    public abstract TContract GetContract<TContract>() where TContract : IContract;
}
