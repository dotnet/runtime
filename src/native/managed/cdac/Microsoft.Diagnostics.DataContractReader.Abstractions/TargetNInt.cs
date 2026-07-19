// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader;


[DebuggerDisplay("{Hex}")]
public readonly struct TargetNInt : IEquatable<TargetNInt>
{
    public readonly long Value;
    public TargetNInt(long value) => Value = value;

    internal string Hex => $"0x{Value:x}";

    public override bool Equals(object? obj) => obj is TargetNInt other && Equals(other);

    public bool Equals(TargetNInt t) => Value == t.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(TargetNInt lhs, TargetNInt rhs) => lhs.Equals(rhs);

    public static bool operator !=(TargetNInt lhs, TargetNInt rhs) => !(lhs == rhs);
}
