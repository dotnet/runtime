// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IRegistry
{
    public IException Exception { get;}
    public ILoader Loader { get; }
    public IEcmaMetadata EcmaMetadata { get; }
    public IObject Object { get; }
    public IThread Thread { get; }
    public IRuntimeTypeSystem RuntimeTypeSystem { get; }
    public IDacStreams DacStreams { get; }
}
