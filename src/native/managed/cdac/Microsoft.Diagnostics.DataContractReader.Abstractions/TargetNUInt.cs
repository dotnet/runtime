// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader;


[DebuggerDisplay("{Hex}")]
public readonly struct TargetNUInt : IEquatable<TargetNUInt>
{
    public readonly ulong Value;
    public TargetNUInt(ulong value) => Value = value;

    internal string Hex => $"0x{Value:x}";

    public override bool Equals(object? obj) => obj is TargetNUInt other && Equals(other);

    public bool Equals(TargetNUInt t) => Value == t.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(TargetNUInt lhs, TargetNUInt rhs) => lhs.Equals(rhs);

    public static bool operator !=(TargetNUInt lhs, TargetNUInt rhs) => !(lhs == rhs);
}
