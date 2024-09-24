// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// A registry of all the contracts that may be provided by a target.
/// </summary>
internal interface IContractRegistry
{
    /// <summary>
    /// Gets an instance of the Exception contract for the target.
    /// </summary>
    IException Exception { get;}
    /// <summary>
    /// Gets an instance of the Loader contract for the target.
    /// </summary>
    ILoader Loader { get; }
    /// <summary>
    /// Gets an instance of the EcmaMetadata contract for the target.
    /// </summary>
    IEcmaMetadata EcmaMetadata { get; }
    /// <summary>
    /// Gets an instance of the Object contract for the target.
    /// </summary>
    IObject Object { get; }
    /// <summary>
    /// Gets an instance of the Thread contract for the target.
    /// </summary>
    IThread Thread { get; }
    /// <summary>
    /// Gets an instance of the RuntimeTypeSystem contract for the target.
    /// </summary>
    IRuntimeTypeSystem RuntimeTypeSystem { get; }
    /// <summary>
    /// Gets an instance of the DacStreams contract for the target.
    /// </summary>
    IDacStreams DacStreams { get; }
    IExecutionManager ExecutionManager { get; }
    ICodeVersions CodeVersions { get; }
    IPrecodeStubs PrecodeStubs { get; }
    IReJIT ReJIT { get;  }
}
