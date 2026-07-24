// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Formats.Asn1
{
    internal abstract class RestrictedAsciiRangeEncoding : RestrictedAsciiStringEncoding
    {
        protected RestrictedAsciiRangeEncoding(byte minCharAllowed, byte maxCharAllowed)
            : base(minCharAllowed, maxCharAllowed)
        {
        }
    }

    internal sealed partial class BMPEncoding
    {
        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
        {
            if (chars.IsEmpty)
            {
                return 0;
            }

            int writeIdx = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (char.IsSurrogate(c))
                {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, i);

                    Debug.Fail("Fallback should have thrown");
                    throw new InvalidOperationException();
                }

                ushort val16 = c;

                if (write)
                {
                    bytes[writeIdx + 1] = (byte)val16;
                    bytes[writeIdx] = (byte)(val16 >> 8);
                }

                writeIdx += 2;
            }

            return writeIdx;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
        {
            if (bytes.IsEmpty)
            {
                return 0;
            }

            if (bytes.Length % 2 != 0)
            {
                DecoderFallback.CreateFallbackBuffer().Fallback(
                    bytes.Slice(bytes.Length - 1).ToArray(),
                    bytes.Length - 1);

                Debug.Fail("Fallback should have thrown");
                throw new InvalidOperationException();
            }

            int writeIdx = 0;

            for (int i = 0; i < bytes.Length; i += 2)
            {
                char c = (char)BinaryPrimitives.ReadInt16BigEndian(bytes.Slice(i));

                if (char.IsSurrogate(c))
                {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        bytes.Slice(i, 2).ToArray(),
                        i);

                    Debug.Fail("Fallback should have thrown");
                    throw new InvalidOperationException();
                }

                if (write)
                {
                    chars[writeIdx] = c;
                }

                writeIdx++;
            }

            return writeIdx;
        }
    }
}
