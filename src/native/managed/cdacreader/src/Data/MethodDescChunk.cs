// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescChunk : IData<MethodDescChunk>
{
    static MethodDescChunk IData<MethodDescChunk>.Create(Target target, TargetPointer address) => new MethodDescChunk(target, address);
#pragma warning disable IDE0060 // Remove unused parameter
    public MethodDescChunk(Target target, TargetPointer address)
    {

    }
#pragma warning restore IDE0060

}
