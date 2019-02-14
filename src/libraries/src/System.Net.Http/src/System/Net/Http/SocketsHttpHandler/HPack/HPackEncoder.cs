// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.HPack
{
    internal static class HPackEncoder
    {
        // Things we should add:
        // * Static table encoding
        //          This is implemented now for pseudo-headers, but not for regular headers.
        //          This should be based off some sort of "known headers/values" scheme, so we don't need
        //          to re-lookup header names each time.
        // * Huffman encoding
        //      
        // Things we should consider adding:
        // * Dynamic table encoding.
        //          This would make the encoder stateful, which complicates things significantly.
        //          Additionally, it's not clear exactly what strings we would add to the dynamic table
        //          without some additional guidance from the user about this.
        //          So for now, don't do dynamic encoding.

        public static bool EncodeIndexedField(int index, Span<byte> buffer, out int length)
        {
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0x80;   // Literal field
            if (!IntegerEncoder.Encode(index, 7, buffer, out length))
            {
                return false;
            }

            return true;
        }

        public static bool EncodeIndexedName(int index, string value, Span<byte> buffer, out int length)
        {
            int i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0x40;   // Literal name
            if (!IntegerEncoder.Encode(index, 6, buffer, out int indexLength))
            {
                return false;
            }

            i = indexLength;
            if (i == buffer.Length)
            {
                return false;
            }

            if (!EncodeString(value, buffer.Slice(i), out int nameLength, lowercase: false))
            {
                return false;
            }

            i += nameLength;

            length = i;
            return true;
        }

        public static bool EncodeIndexedName(int index, ReadOnlySpan<byte> value, Span<byte> buffer, out int length)
        {
            int i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0x40;   // Literal name
            if (!IntegerEncoder.Encode(index, 6, buffer, out int indexLength))
            {
                return false;
            }

            i = indexLength;
            if (i == buffer.Length)
            {
                return false;
            }

            if (!EncodeAsciiString(value, buffer.Slice(i), out int nameLength))
            {
                return false;
            }

            i += nameLength;

            length = i;
            return true;
        }

        public static bool EncodeHeader(string name, string value, Span<byte> buffer, out int length)
        {
            int i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[i++] = 0;

            if (i == buffer.Length)
            {
                return false;
            }

            if (!EncodeString(name, buffer.Slice(i), out int nameLength, lowercase: true))
            {
                return false;
            }

            i += nameLength;

            if (i >= buffer.Length)
            {
                return false;
            }

            if (!EncodeString(value, buffer.Slice(i), out int valueLength, lowercase: false))
            {
                return false;
            }

            i += valueLength;

            length = i;
            return true;
        }

        private static bool EncodeString(string s, Span<byte> buffer, out int length, bool lowercase)
        {
            const int toLowerMask = 0x20;

            int i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0;

            if (!IntegerEncoder.Encode(s.Length, 7, buffer, out int nameLength))
            {
                return false;
            }

            i += nameLength;

            // TODO: use huffman encoding
            for (int j = 0; j < s.Length; j++)
            {
                if (i >= buffer.Length)
                {
                    return false;
                }

                buffer[i++] = (byte)(s[j] | (lowercase ? toLowerMask : 0));
            }

            length = i;
            return true;
        }

        private static bool EncodeAsciiString(ReadOnlySpan<byte> s, Span<byte> buffer, out int length)
        {
            int i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0;

            if (!IntegerEncoder.Encode(s.Length, 7, buffer, out int nameLength))
            {
                return false;
            }

            i += nameLength;

            if (buffer.Slice(i).Length < s.Length)
            {
                return false;
            }

            s.CopyTo(buffer.Slice(i));

            i += s.Length;

            length = i;
            return true;
        }
    }
}
