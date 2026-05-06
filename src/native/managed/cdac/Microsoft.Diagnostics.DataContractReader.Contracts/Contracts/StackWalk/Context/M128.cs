// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

[StructLayout(LayoutKind.Sequential)]
public struct M128A : IEquatable<M128A>
{
    public ulong Low;
    public ulong High;

    public void Clear()
    {
        Low = 0;
        High = 0;
    }

    public static bool operator ==(M128A left, M128A right) => left.Equals(right);

    public static bool operator !=(M128A left, M128A right) => !(left == right);

    public override bool Equals(object? obj) => obj is M128A other && Equals(other);

    public bool Equals(M128A other) => Low == other.Low && High == other.High;

    public override int GetHashCode() => HashCode.Combine(Low, High);
}
