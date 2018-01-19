// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Runtime.Serialization;

namespace System.Text
{
    // This class overrides Encoding with the things we need for our NLS Encodings
    //
    // All of the GetBytes/Chars GetByte/CharCount methods are just wrappers for the pointer
    // plus decoder/encoder method that is our real workhorse.  Note that this is an internal
    // class, so our public classes cannot derive from this class.  Because of this, all of the
    // GetBytes/Chars GetByte/CharCount wrapper methods are duplicated in all of our public
    // encodings.
    // So if you change the wrappers in this class, you must change the wrappers in the other classes
    // as well because they should have the same behavior.
    internal abstract class EncodingNLS : Encoding
    {
        private string _encodingName;
        private string _webName;

        protected EncodingNLS(int codePage) : base(codePage)
        {
        }

        protected EncodingNLS(int codePage, EncoderFallback enc, DecoderFallback dec)
            : base(codePage, enc, dec)
        {
        }

        public unsafe abstract int GetByteCount(char* chars, int count, EncoderNLS encoder);
        public unsafe abstract int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS encoder);
        public unsafe abstract int GetCharCount(byte* bytes, int count, DecoderNLS decoder);
        public unsafe abstract int GetChars(byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS decoder);

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others. 
        // parent method is safe
        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index): nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input, return 0, avoid fixed empty array problem
            if (chars.Length == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = &chars[0])
                return GetByteCount(pChars + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others. 
        // parent method is safe
        public override unsafe int GetByteCount(String s)
        {
            // Validate input
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            fixed (char* pChars = s)
                return GetByteCount(pChars, s.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.
        public override unsafe int GetByteCount(char* chars, int count)
        {
            // Validate Parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.

        public override unsafe int GetBytes(String s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                throw new ArgumentNullException((s == null ? nameof(s): nameof(bytes)), SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex): nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty arrays
            if (bytes.Length == 0)
                bytes = new byte[1];

            fixed (char* pChars = s)
                fixed (byte* pBytes = &bytes[0])
                    return GetBytes(pChars + charIndex, charCount,
                                    pBytes + byteIndex, byteCount, null);
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  
        // parent method is safe
        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? nameof(chars): nameof(bytes)), SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex): nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            // If nothing to encode return 0, avoid fixed problem
            if (chars.Length == 0)
                return 0;

            // Just call pointer version
            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty arrays
            if (bytes.Length == 0)
                bytes = new byte[1];

            fixed (char* pChars = &chars[0])
                fixed (byte* pBytes = &bytes[0])
                    // Remember that byteCount is # to decode, not size of array.
                    return GetBytes(pChars + charIndex, charCount,
                                    pBytes + byteIndex, byteCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others. 
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes): nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount): nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  
        // parent method is safe
        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index): nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input just return 0, fixed doesn't like 0 length arrays
            if (bytes.Length == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = &bytes[0])
                return GetCharCount(pBytes + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  
        // parent method is safe
        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes): nameof(chars), SR.ArgumentNull_Array);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex): nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_Index);

            // If no input, return 0 & avoid fixed problem
            if (bytes.Length == 0)
                return 0;

            // Just call pointer version
            int charCount = chars.Length - charIndex;

            // Fixed doesn't like empty arrays
            if (chars.Length == 0)
                chars = new char[1];

            fixed (byte* pBytes = &bytes[0])
                fixed (char* pChars = &chars[0])
                    // Remember that charCount is # to decode, not size of array
                    return GetChars(pBytes + byteIndex, byteCount,
                                    pChars + charIndex, charCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes): nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount): nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.
        // parent method is safe
        public override unsafe String GetString(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index): nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid problems with empty input buffer
            if (bytes.Length == 0) return String.Empty;

            fixed (byte* pBytes = &bytes[0])
                return GetString(pBytes + index, count);
        }

        public override Decoder GetDecoder()
        {
            return new DecoderNLS(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        internal void ThrowBytesOverflow(EncoderNLS encoder, bool nothingEncoded)
        {
            if (encoder == null || encoder.m_throwOnOverflow || nothingEncoded)
            {
                if (encoder != null && encoder.InternalHasFallbackBuffer)
                    encoder.FallbackBuffer.Reset();
                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
                ThrowBytesOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            encoder.ClearMustFlush();
        }

        internal void ThrowCharsOverflow(DecoderNLS decoder, bool nothingDecoded)
        {
            if (decoder == null || decoder.m_throwOnOverflow || nothingDecoded)
            {
                if (decoder != null && decoder.InternalHasFallbackBuffer)
                    decoder.FallbackBuffer.Reset();

                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
                ThrowCharsOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            decoder.ClearMustFlush();
        }

        internal void ThrowBytesOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowBytes, EncodingName, EncoderFallback.GetType()), "bytes");
        }

        internal void ThrowCharsOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowChars, EncodingName, DecoderFallback.GetType()), "chars");
        }

        public override String EncodingName
        {
            get
            {
                if (_encodingName == null)
                {
                    _encodingName = GetLocalizedEncodingNameResource(CodePage);
                    if (_encodingName == null)
                    {
                        throw new NotSupportedException(
                            SR.Format(SR.MissingEncodingNameResource, WebName, CodePage));
                    }

                    if (_encodingName.StartsWith("Globalization_cp_", StringComparison.OrdinalIgnoreCase))
                    {
                        // On ProjectN, resource strings are stripped from retail builds and replaced by
                        // their identifier names. Since this property is meant to be a localized string,
                        // but we don't localize ProjectN, we specifically need to do something reasonable
                        // in this case. This currently returns the English name of the encoding from a
                        // static data table.
                        _encodingName = EncodingTable.GetEnglishNameFromCodePage(CodePage);
                        if (_encodingName == null)
                        {
                            throw new NotSupportedException(
                                SR.Format(SR.MissingEncodingNameResource, WebName, CodePage));
                        }
                    }
                }
                return _encodingName;
            }
        }

        private static string GetLocalizedEncodingNameResource(int codePage)
        {
            switch (codePage)
            {
                case 37: return SR.Globalization_cp_37;
                case 437: return SR.Globalization_cp_437;
                case 500: return SR.Globalization_cp_500;
                case 708: return SR.Globalization_cp_708;
                case 720: return SR.Globalization_cp_720;
                case 737: return SR.Globalization_cp_737;
                case 775: return SR.Globalization_cp_775;
                case 850: return SR.Globalization_cp_850;
                case 852: return SR.Globalization_cp_852;
                case 855: return SR.Globalization_cp_855;
                case 857: return SR.Globalization_cp_857;
                case 858: return SR.Globalization_cp_858;
                case 860: return SR.Globalization_cp_860;
                case 861: return SR.Globalization_cp_861;
                case 862: return SR.Globalization_cp_862;
                case 863: return SR.Globalization_cp_863;
                case 864: return SR.Globalization_cp_864;
                case 865: return SR.Globalization_cp_865;
                case 866: return SR.Globalization_cp_866;
                case 869: return SR.Globalization_cp_869;
                case 870: return SR.Globalization_cp_870;
                case 874: return SR.Globalization_cp_874;
                case 875: return SR.Globalization_cp_875;
                case 932: return SR.Globalization_cp_932;
                case 936: return SR.Globalization_cp_936;
                case 949: return SR.Globalization_cp_949;
                case 950: return SR.Globalization_cp_950;
                case 1026: return SR.Globalization_cp_1026;
                case 1047: return SR.Globalization_cp_1047;
                case 1140: return SR.Globalization_cp_1140;
                case 1141: return SR.Globalization_cp_1141;
                case 1142: return SR.Globalization_cp_1142;
                case 1143: return SR.Globalization_cp_1143;
                case 1144: return SR.Globalization_cp_1144;
                case 1145: return SR.Globalization_cp_1145;
                case 1146: return SR.Globalization_cp_1146;
                case 1147: return SR.Globalization_cp_1147;
                case 1148: return SR.Globalization_cp_1148;
                case 1149: return SR.Globalization_cp_1149;
                case 1250: return SR.Globalization_cp_1250;
                case 1251: return SR.Globalization_cp_1251;
                case 1252: return SR.Globalization_cp_1252;
                case 1253: return SR.Globalization_cp_1253;
                case 1254: return SR.Globalization_cp_1254;
                case 1255: return SR.Globalization_cp_1255;
                case 1256: return SR.Globalization_cp_1256;
                case 1257: return SR.Globalization_cp_1257;
                case 1258: return SR.Globalization_cp_1258;
                case 1361: return SR.Globalization_cp_1361;
                case 10000: return SR.Globalization_cp_10000;
                case 10001: return SR.Globalization_cp_10001;
                case 10002: return SR.Globalization_cp_10002;
                case 10003: return SR.Globalization_cp_10003;
                case 10004: return SR.Globalization_cp_10004;
                case 10005: return SR.Globalization_cp_10005;
                case 10006: return SR.Globalization_cp_10006;
                case 10007: return SR.Globalization_cp_10007;
                case 10008: return SR.Globalization_cp_10008;
                case 10010: return SR.Globalization_cp_10010;
                case 10017: return SR.Globalization_cp_10017;
                case 10021: return SR.Globalization_cp_10021;
                case 10029: return SR.Globalization_cp_10029;
                case 10079: return SR.Globalization_cp_10079;
                case 10081: return SR.Globalization_cp_10081;
                case 10082: return SR.Globalization_cp_10082;
                case 20000: return SR.Globalization_cp_20000;
                case 20001: return SR.Globalization_cp_20001;
                case 20002: return SR.Globalization_cp_20002;
                case 20003: return SR.Globalization_cp_20003;
                case 20004: return SR.Globalization_cp_20004;
                case 20005: return SR.Globalization_cp_20005;
                case 20105: return SR.Globalization_cp_20105;
                case 20106: return SR.Globalization_cp_20106;
                case 20107: return SR.Globalization_cp_20107;
                case 20108: return SR.Globalization_cp_20108;
                case 20261: return SR.Globalization_cp_20261;
                case 20269: return SR.Globalization_cp_20269;
                case 20273: return SR.Globalization_cp_20273;
                case 20277: return SR.Globalization_cp_20277;
                case 20278: return SR.Globalization_cp_20278;
                case 20280: return SR.Globalization_cp_20280;
                case 20284: return SR.Globalization_cp_20284;
                case 20285: return SR.Globalization_cp_20285;
                case 20290: return SR.Globalization_cp_20290;
                case 20297: return SR.Globalization_cp_20297;
                case 20420: return SR.Globalization_cp_20420;
                case 20423: return SR.Globalization_cp_20423;
                case 20424: return SR.Globalization_cp_20424;
                case 20833: return SR.Globalization_cp_20833;
                case 20838: return SR.Globalization_cp_20838;
                case 20866: return SR.Globalization_cp_20866;
                case 20871: return SR.Globalization_cp_20871;
                case 20880: return SR.Globalization_cp_20880;
                case 20905: return SR.Globalization_cp_20905;
                case 20924: return SR.Globalization_cp_20924;
                case 20932: return SR.Globalization_cp_20932;
                case 20936: return SR.Globalization_cp_20936;
                case 20949: return SR.Globalization_cp_20949;
                case 21025: return SR.Globalization_cp_21025;
                case 21027: return SR.Globalization_cp_21027;
                case 21866: return SR.Globalization_cp_21866;
                case 28592: return SR.Globalization_cp_28592;
                case 28593: return SR.Globalization_cp_28593;
                case 28594: return SR.Globalization_cp_28594;
                case 28595: return SR.Globalization_cp_28595;
                case 28596: return SR.Globalization_cp_28596;
                case 28597: return SR.Globalization_cp_28597;
                case 28598: return SR.Globalization_cp_28598;
                case 28599: return SR.Globalization_cp_28599;
                case 28603: return SR.Globalization_cp_28603;
                case 28605: return SR.Globalization_cp_28605;
                case 29001: return SR.Globalization_cp_29001;
                case 38598: return SR.Globalization_cp_38598;
                case 50000: return SR.Globalization_cp_50000;
                case 50220: return SR.Globalization_cp_50220;
                case 50221: return SR.Globalization_cp_50221;
                case 50222: return SR.Globalization_cp_50222;
                case 50225: return SR.Globalization_cp_50225;
                case 50227: return SR.Globalization_cp_50227;
                case 50229: return SR.Globalization_cp_50229;
                case 50930: return SR.Globalization_cp_50930;
                case 50931: return SR.Globalization_cp_50931;
                case 50933: return SR.Globalization_cp_50933;
                case 50935: return SR.Globalization_cp_50935;
                case 50937: return SR.Globalization_cp_50937;
                case 50939: return SR.Globalization_cp_50939;
                case 51932: return SR.Globalization_cp_51932;
                case 51936: return SR.Globalization_cp_51936;
                case 51949: return SR.Globalization_cp_51949;
                case 52936: return SR.Globalization_cp_52936;
                case 54936: return SR.Globalization_cp_54936;
                case 57002: return SR.Globalization_cp_57002;
                case 57003: return SR.Globalization_cp_57003;
                case 57004: return SR.Globalization_cp_57004;
                case 57005: return SR.Globalization_cp_57005;
                case 57006: return SR.Globalization_cp_57006;
                case 57007: return SR.Globalization_cp_57007;
                case 57008: return SR.Globalization_cp_57008;
                case 57009: return SR.Globalization_cp_57009;
                case 57010: return SR.Globalization_cp_57010;
                case 57011: return SR.Globalization_cp_57011;
                default: return null;
            }
        }

        // Returns the IANA preferred name for this encoding
        public override String WebName
        {
            get
            {
                if (_webName == null)
                {
                    _webName = EncodingTable.GetWebNameFromCodePage(CodePage);
                    if (_webName == null)
                    {
                        throw new NotSupportedException(
                            SR.Format(SR.NotSupported_NoCodepageData, CodePage));
                    }
                }
                return _webName;
            }
        }
    }
}
