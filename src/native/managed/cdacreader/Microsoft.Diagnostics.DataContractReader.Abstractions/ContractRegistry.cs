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
    public abstract IException Exception { get;}
    /// <summary>
    /// Gets an instance of the Loader contract for the target.
    /// </summary>
    public abstract ILoader Loader { get; }
    /// <summary>
    /// Gets an instance of the EcmaMetadata contract for the target.
    /// </summary>
    public abstract IEcmaMetadata EcmaMetadata { get; }
    /// <summary>
    /// Gets an instance of the Object contract for the target.
    /// </summary>
    public abstract IObject Object { get; }
    /// <summary>
    /// Gets an instance of the Thread contract for the target.
    /// </summary>
    public abstract IThread Thread { get; }
    /// <summary>
    /// Gets an instance of the RuntimeTypeSystem contract for the target.
    /// </summary>
    public abstract IRuntimeTypeSystem RuntimeTypeSystem { get; }
    /// <summary>
    /// Gets an instance of the DacStreams contract for the target.
    /// </summary>
    public abstract IDacStreams DacStreams { get; }
    /// <summary>
    /// Gets an instance of the ExecutionManager contract for the target.
    /// </summary>
    public abstract IExecutionManager ExecutionManager { get; }
    /// <summary>
    /// Gets an instance of the CodeVersions contract for the target.
    /// </summary>
    public abstract ICodeVersions CodeVersions { get; }
    /// <summary>
    /// Gets an instance of the PlatformMetadata contract for the target.
    /// </summary>
    public abstract IPlatformMetadata PlatformMetadata { get; }
    /// <summary>
    /// Gets an instance of the PrecodeStubs contract for the target.
    /// </summary>
    public abstract IPrecodeStubs PrecodeStubs { get; }
    /// <summary>
    /// Gets an instance of the ReJIT contract for the target.
    /// </summary>
    public abstract IReJIT ReJIT { get; }
    /// <summary>
    /// Gets an instance of the StackWalk contract for the target.
    /// </summary>
    public abstract IStackWalk StackWalk { get; }
}
