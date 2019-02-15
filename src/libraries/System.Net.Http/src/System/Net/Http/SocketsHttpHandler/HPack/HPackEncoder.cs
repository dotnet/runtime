// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal static class HPackEncoder
    {
        // Things we should add:
        // * Huffman encoding
        //      
        // Things we should consider adding:
        // * Dynamic table encoding:
        //   This would make the encoder stateful, which complicates things significantly.
        //   Additionally, it's not clear exactly what strings we would add to the dynamic table
        //   without some additional guidance from the user about this.
        //   So for now, don't do dynamic encoding.

        public static bool EncodeIndexedField(int index, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                destination[0] = 0x80;   // Literal field
                return IntegerEncoder.Encode(index, 7, destination, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeIndexedName(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                destination[0] = 0x40;   // Literal name
                if (IntegerEncoder.Encode(index, 6, destination, out int indexLength))
                {
                    if (indexLength < destination.Length &&
                        EncodeString(value, destination.Slice(indexLength), out int nameLength, toLower: false))
                    {
                        bytesWritten = indexLength + nameLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeIndexedName(int index, ReadOnlySpan<byte> value, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                destination[0] = 0x40;   // Literal name
                if (IntegerEncoder.Encode(index, 6, destination, out int indexLength))
                {
                    if (indexLength < destination.Length &&
                        EncodeAsciiString(value, destination.Slice(indexLength), out int nameLength))
                    {
                        bytesWritten = indexLength + nameLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static byte[] EncodeIndexedNameToAllocatedArray(int index, ReadOnlySpan<byte> value)
        {
            Span<byte> span = stackalloc byte[256];
            bool success = EncodeIndexedName(index, value, span, out int length);
            Debug.Assert(success, "Stack-allocated space was too small to accomodate known name/value.");
            return span.Slice(0, length).ToArray();
        }

        public static bool EncodeHeaderNameValue(string name, string value, Span<byte> destination, out int bytesWritten)
        {
            if ((uint)destination.Length > 1 &&
                EncodeString(name, destination.Slice(1), out int nameLength, toLower: true))
            {
                destination[0] = 0;
                if (EncodeString(value, destination.Slice(1 + nameLength), out int valueLength, toLower: false))
                {
                    bytesWritten = 1 + nameLength + valueLength;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }

        private static bool EncodeString(string value, Span<byte> destination, out int bytesWritten, bool toLower)
        {
            if (destination.Length != 0)
            {
                destination[0] = 0;
                if (IntegerEncoder.Encode(value.Length, 7, destination, out int integerLength))
                {
                    // TODO: Use Huffman encoding
                    destination = destination.Slice(integerLength);
                    if (value.Length <= destination.Length)
                    {
                        if (toLower)
                        {
                            for (int i = 0; i < value.Length; i++)
                            {
                                char c = value[i];
                                destination[i] = (byte)((uint)(c - 'A') <= ('Z' - 'A') ? c | 0x20 : c);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < value.Length; i++)
                            {
                                destination[i] = (byte)value[i];
                            }
                        }

                        bytesWritten = integerLength + value.Length;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        private static bool EncodeAsciiString(ReadOnlySpan<byte> value, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                destination[0] = 0;
                if (IntegerEncoder.Encode(value.Length, 7, destination, out int integerLength) &&
                    value.TryCopyTo(destination.Slice(integerLength)))
                {
                    bytesWritten = integerLength + value.Length;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }
    }
}
