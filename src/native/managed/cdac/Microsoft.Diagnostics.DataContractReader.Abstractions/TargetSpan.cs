// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

public readonly struct TargetSpan
{
    public TargetSpan(TargetPointer address, ulong size)
    {
        Address = address;
        Size = size;
    }

    public TargetPointer Address { get; }
    public ulong Size { get; }
}
