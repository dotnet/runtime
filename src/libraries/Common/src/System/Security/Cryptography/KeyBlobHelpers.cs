// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;

namespace System.Security.Cryptography
{
    internal static partial class KeyBlobHelpers
    {
        internal static byte[] ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span)
        {
            if (span.Length > 1 && span[0] == 0)
            {
                return span.Slice(1).ToArray();
            }

            return span.ToArray();
        }

        internal static void ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span, Span<byte> destination)
        {
            int length = destination.Length;

            if (span.Length == length)
            {
                span.CopyTo(destination);
                return;
            }

            if (span.Length == length + 1)
            {
                if (span[0] == 0)
                {
                    span.Slice(1).CopyTo(destination);
                    return;
                }
            }

            if (span.Length > length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            destination.Slice(0, destination.Length - span.Length).Clear();
            span.CopyTo(destination.Slice(length - span.Length));
        }

        internal static void WriteKeyParameterInteger(this AsnWriter writer, ReadOnlySpan<byte> integer)
        {
            Debug.Assert(!integer.IsEmpty);

            if (integer[0] == 0)
            {
                int newStart = 1;

                while (newStart < integer.Length)
                {
                    if (integer[newStart] >= 0x80)
                    {
                        newStart--;
                        break;
                    }

                    if (integer[newStart] != 0)
                    {
                        break;
                    }

                    newStart++;
                }

                if (newStart == integer.Length)
                {
                    newStart--;
                }

                integer = integer.Slice(newStart);
            }

            writer.WriteIntegerUnsigned(integer);
        }
    }
}
