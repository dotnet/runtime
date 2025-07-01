// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Represents the native CLRDATA_ADDRESS 64-bit type which uses sign extending
/// when converting from 32-bit values to 64-bit values.
/// When marshalled to native code, this type is represented as a 64-bit unsigned integer.
/// </summary>
[NativeMarshalling(typeof(ClrDataAddressMarshaller))]
internal struct ClrDataAddress : IEquatable<ClrDataAddress>
{
    public ulong Value;

    public ClrDataAddress(ulong value) => Value = value;

    public static implicit operator ulong(ClrDataAddress a) => a.Value;
    public static implicit operator ClrDataAddress(ulong v) => new ClrDataAddress(v);

    public override bool Equals(object? obj) => obj is ClrDataAddress address && Equals(address);
    public readonly bool Equals(ClrDataAddress other) => Value == other.Value;
    public override readonly int GetHashCode() => Value.GetHashCode();

    public override readonly string ToString() => $"0x{Value:x}";
}

[CustomMarshaller(typeof(ClrDataAddress), MarshalMode.Default, typeof(ClrDataAddressMarshaller))]
internal static class ClrDataAddressMarshaller
{
    public static ClrDataAddress ConvertToManaged(ulong address) => new ClrDataAddress(address);
    public static ulong ConvertToUnmanaged(ClrDataAddress address) => address.Value;
}
