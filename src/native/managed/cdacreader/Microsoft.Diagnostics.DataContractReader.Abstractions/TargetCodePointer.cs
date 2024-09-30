// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader;

public readonly struct TargetCodePointer : IEquatable<TargetCodePointer>
{
    public static TargetCodePointer Null = new(0);
    public readonly ulong Value;
    public TargetCodePointer(ulong value) => Value = value;

    public static implicit operator ulong(TargetCodePointer p) => p.Value;
    public static implicit operator TargetCodePointer(ulong v) => new TargetCodePointer(v);

    public static bool operator ==(TargetCodePointer left, TargetCodePointer right) => left.Value == right.Value;
    public static bool operator !=(TargetCodePointer left, TargetCodePointer right) => left.Value != right.Value;

    public override bool Equals(object? obj) => obj is TargetCodePointer pointer && Equals(pointer);
    public bool Equals(TargetCodePointer other) => Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public bool Equals(TargetCodePointer x, TargetCodePointer y) => x.Value == y.Value;
    public int GetHashCode(TargetCodePointer obj) => obj.Value.GetHashCode();

    public TargetPointer AsTargetPointer => new(Value);

    public override string ToString() => $"0x{Value:x}";
}
