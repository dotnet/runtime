// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    internal static class Utf8JsonWriterExtensions
    {
        internal static void WriteHexBytesString(this Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName,
            ReadOnlySpan<byte> bytes)
        {
            static byte ToAsciValueHelper(int value)
            {
                if (value < 10)
                    return (byte)('0' + value);
                return (byte)('a' + value - 10);
            }

            Span<byte> hexString = stackalloc byte[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString[2 * i] = ToAsciValueHelper(bytes[i] / 16);
                hexString[2 * i + 1] = ToAsciValueHelper(bytes[i] % 16);
            }

            writer.WriteString(propertyName, hexString);
        }
    }
}
