// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IDacStreams : IContract
{
    static string IContract.Name { get; } = nameof(DacStreams);
    public virtual string? StringFromEEAddress(TargetPointer address) => throw new NotImplementedException();
}

internal readonly struct DacStreams : IDacStreams
{
    // Everything throws NotImplementedException
}
