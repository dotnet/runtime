// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader;

public readonly struct TargetNUInt
{
    public readonly ulong Value;
    public TargetNUInt(ulong value) => Value = value;
}
