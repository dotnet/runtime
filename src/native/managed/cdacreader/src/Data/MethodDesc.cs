// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDesc : IData<MethodDesc>
{
    static MethodDesc IData<MethodDesc>.Create(Target target, TargetPointer address) => new MethodDesc(target, address);
#pragma warning disable IDE0060 // Remove unused parameter
    public MethodDesc(Target target, TargetPointer address)
    {

    }
#pragma warning restore IDE0060

    public byte ChunkIndex { get; init; }
}
