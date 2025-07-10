// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader;

public readonly struct TargetPointer : IEquatable<TargetPointer>
{
    public static TargetPointer Null = new(0);
    public static TargetPointer Max32Bit = new(uint.MaxValue);
    public static TargetPointer Max64Bit = new(ulong.MaxValue);

    public readonly ulong Value;
    public TargetPointer(ulong value) => Value = value;

    public static implicit operator ulong(TargetPointer p) => p.Value;
    public static implicit operator TargetPointer(ulong v) => new TargetPointer(v);

    public static bool operator ==(TargetPointer left, TargetPointer right) => left.Value == right.Value;
    public static bool operator !=(TargetPointer left, TargetPointer right) => left.Value != right.Value;

    public override bool Equals(object? obj) => obj is TargetPointer pointer && Equals(pointer);
    public bool Equals(TargetPointer other) => Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"0x{Value:x}";
}
