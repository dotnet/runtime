// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http.HPack
{
    internal static class HPackEncoder
    {
        // Things we should add:
        // * Static table encoding
        //          This should be based off some sort of "known headers/values" scheme, so we don't need
        //          to re-lookup header names each time.
        //
        // Things we should consider adding:
        // * Dynamic table encoding.
        //          This would make the encoder stateful, which complicates things significantly.
        //          Additionally, it's not clear exactly what strings we would add to the dynamic table
        //          without some additional guidance from the user about this.
        //          So for now, don't do dynamic encoding.

        public static bool EncodeHeader(string name, string value, Span<byte> buffer, out int length)
        {
            var i = 0;
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

            if (!EncodeString(name, buffer.Slice(i), out var nameLength, lowercase: true))
            {
                return false;
            }

            i += nameLength;

            if (i >= buffer.Length)
            {
                return false;
            }

            if (!EncodeString(value, buffer.Slice(i), out var valueLength, lowercase: false))
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

            var i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0;

            if (!IntegerEncoder.Encode(s.Length, 7, buffer, out var nameLength))
            {
                return false;
            }

            i += nameLength;

            // TODO: use huffman encoding
            for (var j = 0; j < s.Length; j++)
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
    }
}
