// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        // Encode a byte array as an array of upper-case hex characters.
        public static char[] ToHexArrayUpper(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            ToHexArrayUpper(bytes, chars);
            return chars;
        }

        private static void ToHexArrayUpper(byte[] bytes, Span<char> chars)
        {
            Debug.Assert(chars.Length >= bytes.Length * 2);
            int i = 0;
            foreach (byte b in bytes)
            {
                chars[i++] = NibbleToHex((byte)(b >> 4));
                chars[i++] = NibbleToHex((byte)(b & 0xF));
            }
        }

        // Encode a byte array as an upper case hex string.
        public static string ToHexStringUpper(this byte[] bytes) =>
            string.Create(bytes.Length * 2, bytes, (chars, src) => ToHexArrayUpper(src, chars));

        // Decode a hex string-encoded byte array passed to various X509 crypto api.
        // The parsing rules are overly forgiving but for compat reasons, they cannot be tightened.
        public static byte[] DecodeHexString(this string hexString)
        {
            int whitespaceCount = 0;

            ReadOnlySpan<char> s = hexString;

            if (s.Length != 0 && s[0] == '\u200E')
            {
                s = s.Slice(1);
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i]))
                    whitespaceCount++;
            }

            uint cbHex = (uint)(s.Length - whitespaceCount) / 2;
            byte[] hex = new byte[cbHex];
            byte accum = 0;
            bool byteInProgress = false;
            int index = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                accum <<= 4;
                accum |= HexToByte(c);

                byteInProgress = !byteInProgress;

                // If we've flipped from 0 to 1, back to 0, we have a whole byte
                // so add it to the buffer.
                if (!byteInProgress)
                {
                    Debug.Assert(index < cbHex, "index < cbHex");

                    hex[index] = accum;
                    index++;
                }
            }

            // Desktop compat:
            // The desktop algorithm removed all whitespace before the loop, then went up to length/2
            // of what was left.  This means that in the event of odd-length input the last char is
            // ignored, no exception should be raised.
            Debug.Assert(index == cbHex, "index == cbHex");

            return hex;
        }

        private static byte HexToByte(char val)
        {
            if (val <= '9' && val >= '0')
                return (byte)(val - '0');
            else if (val >= 'a' && val <= 'f')
                return (byte)((val - 'a') + 10);
            else if (val >= 'A' && val <= 'F')
                return (byte)((val - 'A') + 10);
            else
                return 0xFF;
        }

        private static char NibbleToHex(byte b)
        {
            Debug.Assert(b >= 0 && b <= 15);
            return (char)(b >= 0 && b <= 9 ? 
                '0' + b : 
                'A' + (b - 10));
        }

        public static bool ContentsEqual(this byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        internal static void AddRange<T>(this ICollection<T> coll, IEnumerable<T> newData)
        {
            foreach (T datum in newData)
            {
                coll.Add(datum);
            }
        }

        //
        // The following group of helpers emulates the non-public Calendar.IsValidDay() method used by X509Certificate.ToString(bool).
        //
        public static bool IsValidDay(this Calendar calendar, int year, int month, int day, int era)
        {
            return (calendar.IsValidMonth(year, month, era) && day >= 1 && day <= calendar.GetDaysInMonth(year, month, era));
        }

        private static bool IsValidMonth(this Calendar calendar, int year, int month, int era)
        {
            return (calendar.IsValidYear(year, era) && month >= 1 && month <= calendar.GetMonthsInYear(year, era));
        }

        private static bool IsValidYear(this Calendar calendar, int year, int era)
        {
            return (year >= calendar.GetYear(calendar.MinSupportedDateTime) && year <= calendar.GetYear(calendar.MaxSupportedDateTime));
        }

        internal static void DisposeAll(this IEnumerable<IDisposable> disposables)
        {
            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public static void ValidateDer(ReadOnlyMemory<byte> encodedValue)
        {
            Asn1Tag tag;
            AsnReader reader = new AsnReader(encodedValue, AsnEncodingRules.DER);

            while (reader.HasData)
            {
                tag = reader.PeekTag();

                // If the tag is in the UNIVERSAL class
                //
                // DER limits the constructed encoding to SEQUENCE and SET, as well as anything which gets
                // a defined encoding as being an IMPLICIT SEQUENCE.
                if (tag.TagClass == TagClass.Universal)
                {
                    switch ((UniversalTagNumber)tag.TagValue)
                    {
                        case UniversalTagNumber.External:
                        case UniversalTagNumber.Embedded:
                        case UniversalTagNumber.Sequence:
                        case UniversalTagNumber.Set:
                        case UniversalTagNumber.UnrestrictedCharacterString:
                            if (!tag.IsConstructed)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }
                            break;

                        default:
                            if (tag.IsConstructed)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }
                            break;
                    }
                }

                if (tag.IsConstructed)
                {
                    ValidateDer(reader.PeekContentBytes());
                }

                // Skip past the current value.
                reader.ReadEncodedValue();
            }
        }
    }

    internal static class DictionaryStringHelper
    {
        internal static string ReadDirectoryOrIA5String(this AsnReader tavReader)
        {
            Asn1Tag tag = tavReader.PeekTag();

            if (tag.TagClass != TagClass.Universal)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            switch ((UniversalTagNumber)tag.TagValue)
            {
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.T61String:
                    // .NET's string comparisons start by checking the length, so a trailing
                    // NULL character which was literally embedded in the DER would cause a
                    // failure in .NET whereas it wouldn't have with strcmp.
                    return tavReader.ReadCharacterString((UniversalTagNumber)tag.TagValue).TrimEnd('\0');
                    
                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }
    }

    internal struct PinAndClear : IDisposable
    {
        private byte[] _data;
        private System.Runtime.InteropServices.GCHandle _gcHandle;

        internal static PinAndClear Track(byte[] data)
        {
            return new PinAndClear
            {
                _gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(
                    data,
                    System.Runtime.InteropServices.GCHandleType.Pinned),
                _data = data,
            };
        }

        public void Dispose()
        {
            Array.Clear(_data, 0, _data.Length);
            _gcHandle.Free();
        }
    }
}
