// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        internal static ReadOnlySpan<byte> AsSpanParameter(this byte[] array, string paramName)
        {
            if (array == null)
                throw new ArgumentNullException(paramName);

            return new ReadOnlySpan<byte>(array);
        }

        // Encode a byte array as an array of upper-case hex characters.
        public static char[] ToHexArrayUpper(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            HexConverter.EncodeToUtf16(bytes, chars);
            return chars;
        }

        // Encode a byte array as an upper case hex string.
        public static string ToHexStringUpper(this byte[] bytes) =>
            Convert.ToHexString(bytes);

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
                accum |= (byte)HexConverter.FromChar(c);

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

            // .NET Framework compat:
            // The .NET Framework algorithm removed all whitespace before the loop, then went up to length/2
            // of what was left.  This means that in the event of odd-length input the last char is
            // ignored, no exception should be raised.
            Debug.Assert(index == cbHex, "index == cbHex");

            return hex;
        }

        public static bool ContentsEqual(this byte[]? a1, byte[]? a2)
        {
            if (a1 == null)
            {
                return a2 == null;
            }

            if (a2 == null || a1.Length != a2.Length)
            {
                return false;
            }

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
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
            try
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
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
        {
            try
            {
                ReadOnlySpan<byte> input = encodedOctetString.Span;

                if (AsnDecoder.TryReadPrimitiveOctetString(
                    input,
                    AsnEncodingRules.BER,
                    out ReadOnlySpan<byte> primitive,
                    out int consumed))
                {
                    if (consumed != input.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (input.Overlaps(primitive, out int offset))
                    {
                        return encodedOctetString.Slice(offset, primitive.Length);
                    }

                    Debug.Fail("input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
                }

                byte[] ret = AsnDecoder.ReadOctetString(input, AsnEncodingRules.BER, out consumed);

                if (consumed != input.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public static bool AreSamePublicECParameters(ECParameters aParameters, ECParameters bParameters)
        {
            if (aParameters.Curve.CurveType != bParameters.Curve.CurveType)
                return false;

            if (!aParameters.Q.X!.ContentsEqual(bParameters.Q.X!) ||
                !aParameters.Q.Y!.ContentsEqual(bParameters.Q.Y!))
            {
                return false;
            }

            ECCurve aCurve = aParameters.Curve;
            ECCurve bCurve = bParameters.Curve;

            if (aCurve.IsNamed)
            {
                // On Windows we care about FriendlyName, on Unix we care about Value
                return (aCurve.Oid.Value == bCurve.Oid.Value && aCurve.Oid.FriendlyName == bCurve.Oid.FriendlyName);
            }

            if (!aCurve.IsExplicit)
            {
                // Implicit curve, always fail.
                return false;
            }

            // Ignore Cofactor (which is derivable from the prime or polynomial and Order)
            // Ignore Seed and Hash (which are entirely optional, and about how A and B were built)
            if (!aCurve.G.X!.ContentsEqual(bCurve.G.X!) ||
                !aCurve.G.Y!.ContentsEqual(bCurve.G.Y!) ||
                !aCurve.Order.ContentsEqual(bCurve.Order) ||
                !aCurve.A.ContentsEqual(bCurve.A) ||
                !aCurve.B.ContentsEqual(bCurve.B))
            {
                return false;
            }

            if (aCurve.IsPrime)
            {
                return aCurve.Prime.ContentsEqual(bCurve.Prime);
            }

            if (aCurve.IsCharacteristic2)
            {
                return aCurve.Polynomial.ContentsEqual(bCurve.Polynomial);
            }

            Debug.Fail($"Missing match criteria for curve type {aCurve.CurveType}");
            return false;
        }
    }

    internal static class DictionaryStringHelper
    {
        internal static string ReadAnyAsnString(this AsnReader tavReader)
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
                case UniversalTagNumber.NumericString:
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
            Array.Clear(_data);
            _gcHandle.Free();
        }
    }
}
