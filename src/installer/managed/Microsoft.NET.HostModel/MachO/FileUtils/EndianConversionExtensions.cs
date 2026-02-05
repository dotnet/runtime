// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.NET.HostModel.MachO;

public static class EndianConversionExtensions
{
    public static uint ConvertToBigEndian(this uint value)
    {
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public static ulong ConvertToBigEndian(this ulong value)
    {
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public static uint ConvertFromBigEndian(this uint value)
    {
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public static ulong ConvertFromBigEndian(this ulong value)
    {
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
}
