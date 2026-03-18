// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class KeyBlobHelpers
    {
        internal static byte[] ToUnsignedIntegerBytes(this ReadOnlyMemory<byte> memory)
        {
            if (memory.Length > 1 && memory.Span[0] == 0)
            {
                return memory.Slice(1).ToArray();
            }

            return memory.ToArray();
        }

        internal static void ToUnsignedIntegerBytes(this ReadOnlyMemory<byte> memory, Span<byte> destination)
        {
            int length = destination.Length;

            if (memory.Length == length)
            {
                memory.Span.CopyTo(destination);
                return;
            }

            if (memory.Length == length + 1)
            {
                if (memory.Span[0] == 0)
                {
                    memory.Span.Slice(1).CopyTo(destination);
                    return;
                }
            }

            if (memory.Length > length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            destination.Slice(0, destination.Length - memory.Length).Clear();
            memory.Span.CopyTo(destination.Slice(length - memory.Length));
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
