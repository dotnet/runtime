// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal struct ClrDataAddress : IEquatable<ClrDataAddress>
{
    public ulong Address;

    public ClrDataAddress(ulong address) => Address = address;

    public static implicit operator ulong(ClrDataAddress a) => a.Address;
    public static implicit operator ClrDataAddress(ulong v) => new ClrDataAddress(v);

    public override bool Equals(object? obj) => obj is ClrDataAddress address && Equals(address);
    public readonly bool Equals(ClrDataAddress other) => Address == other.Address;
    public override readonly int GetHashCode() => Address.GetHashCode();

    public override readonly string ToString() => $"0x{Address:x}";
}
