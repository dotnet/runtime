// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    internal static class StringBuilderExtension
    {
        internal static void AppendHexBytesString(this StringBuilder builder, ReadOnlySpan<byte> bytes)
        {
            static char ToAsciValueHelper(int value)
            {
                if (value < 10)
                    return (char)('0' + value);
                return (char)('a' + value - 10);
            }

            Span<char> hexString = stackalloc char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString[2 * i] = ToAsciValueHelper(bytes[i] / 16);
                hexString[2 * i + 1] = ToAsciValueHelper(bytes[i] % 16);
            }

            builder.Append(hexString);
        }
    }
}
