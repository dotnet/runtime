// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

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
        private string? _encodingName;
        private string? _webName;

        protected EncodingNLS(int codePage) : base(codePage)
        {
        }

        protected EncodingNLS(int codePage, EncoderFallback enc, DecoderFallback dec)
            : base(codePage, enc, dec)
        {
        }

        public abstract unsafe int GetByteCount(char* chars, int count, EncoderNLS? encoder);
        public abstract unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS? encoder);
        public abstract unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? decoder);
        public abstract unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS? decoder);

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.
        // parent method is safe
        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

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
        public override unsafe int GetByteCount(string s)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            fixed (char* pChars = s)
                return GetByteCount(pChars, s.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.
        public override unsafe int GetByteCount(char* chars, int count)
        {
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.

        public override unsafe int GetBytes(string s, int charIndex, int charCount,
                                            byte[] bytes, int byteIndex)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

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
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

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
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

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
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.
        // parent method is safe
        public override unsafe string GetString(byte[] bytes, int index, int count)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid problems with empty input buffer
            if (bytes.Length == 0) return string.Empty;

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

        internal void ThrowBytesOverflow(EncoderNLS? encoder, bool nothingEncoded)
        {
            if (encoder is null || encoder.m_throwOnOverflow || nothingEncoded)
            {
                if (encoder is not null && encoder.InternalHasFallbackBuffer)
                    encoder.FallbackBuffer.Reset();
                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
                ThrowBytesOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            encoder.ClearMustFlush();
        }

        internal void ThrowCharsOverflow(DecoderNLS? decoder, bool nothingDecoded)
        {
            if (decoder is null || decoder.m_throwOnOverflow || nothingDecoded)
            {
                if (decoder is not null && decoder.InternalHasFallbackBuffer)
                    decoder.FallbackBuffer.Reset();

                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
                ThrowCharsOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            decoder.ClearMustFlush();
        }

        [DoesNotReturn]
        internal void ThrowBytesOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowBytes, EncodingName, EncoderFallback.GetType()), "bytes");
        }

        [DoesNotReturn]
        internal void ThrowCharsOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowChars, EncodingName, DecoderFallback.GetType()), "chars");
        }

        public override string EncodingName
        {
            get
            {
                if (_encodingName is null)
                {
                    _encodingName = GetLocalizedEncodingNameResource(CodePage);
                    if (_encodingName is null)
                    {
                        throw new NotSupportedException(
                            SR.Format(SR.MissingEncodingNameResource, WebName, CodePage));
                    }

                    if (_encodingName.StartsWith("Globalization_cp_", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource strings may be stripped from retail builds and replaced by
                        // their identifier names. Since this property is meant to be a localized string,
                        // we specifically need to do something reasonable in this case. This currently
                        // returns the English name of the encoding from a static data table.
                        _encodingName = EncodingTable.GetEnglishNameFromCodePage(CodePage);
                        if (_encodingName is null)
                        {
                            throw new NotSupportedException(
                                SR.Format(SR.MissingEncodingNameResource, WebName, CodePage));
                        }
                    }
                }
                return _encodingName;
            }
        }

        internal static string? GetLocalizedEncodingNameResource(int codePage) =>
            codePage switch
            {
                37 => SR.Globalization_cp_37,
                437 => SR.Globalization_cp_437,
                500 => SR.Globalization_cp_500,
                708 => SR.Globalization_cp_708,
                720 => SR.Globalization_cp_720,
                737 => SR.Globalization_cp_737,
                775 => SR.Globalization_cp_775,
                850 => SR.Globalization_cp_850,
                852 => SR.Globalization_cp_852,
                855 => SR.Globalization_cp_855,
                857 => SR.Globalization_cp_857,
                858 => SR.Globalization_cp_858,
                860 => SR.Globalization_cp_860,
                861 => SR.Globalization_cp_861,
                862 => SR.Globalization_cp_862,
                863 => SR.Globalization_cp_863,
                864 => SR.Globalization_cp_864,
                865 => SR.Globalization_cp_865,
                866 => SR.Globalization_cp_866,
                869 => SR.Globalization_cp_869,
                870 => SR.Globalization_cp_870,
                874 => SR.Globalization_cp_874,
                875 => SR.Globalization_cp_875,
                932 => SR.Globalization_cp_932,
                936 => SR.Globalization_cp_936,
                949 => SR.Globalization_cp_949,
                950 => SR.Globalization_cp_950,
                1026 => SR.Globalization_cp_1026,
                1047 => SR.Globalization_cp_1047,
                1140 => SR.Globalization_cp_1140,
                1141 => SR.Globalization_cp_1141,
                1142 => SR.Globalization_cp_1142,
                1143 => SR.Globalization_cp_1143,
                1144 => SR.Globalization_cp_1144,
                1145 => SR.Globalization_cp_1145,
                1146 => SR.Globalization_cp_1146,
                1147 => SR.Globalization_cp_1147,
                1148 => SR.Globalization_cp_1148,
                1149 => SR.Globalization_cp_1149,
                1250 => SR.Globalization_cp_1250,
                1251 => SR.Globalization_cp_1251,
                1252 => SR.Globalization_cp_1252,
                1253 => SR.Globalization_cp_1253,
                1254 => SR.Globalization_cp_1254,
                1255 => SR.Globalization_cp_1255,
                1256 => SR.Globalization_cp_1256,
                1257 => SR.Globalization_cp_1257,
                1258 => SR.Globalization_cp_1258,
                1361 => SR.Globalization_cp_1361,
                10000 => SR.Globalization_cp_10000,
                10001 => SR.Globalization_cp_10001,
                10002 => SR.Globalization_cp_10002,
                10003 => SR.Globalization_cp_10003,
                10004 => SR.Globalization_cp_10004,
                10005 => SR.Globalization_cp_10005,
                10006 => SR.Globalization_cp_10006,
                10007 => SR.Globalization_cp_10007,
                10008 => SR.Globalization_cp_10008,
                10010 => SR.Globalization_cp_10010,
                10017 => SR.Globalization_cp_10017,
                10021 => SR.Globalization_cp_10021,
                10029 => SR.Globalization_cp_10029,
                10079 => SR.Globalization_cp_10079,
                10081 => SR.Globalization_cp_10081,
                10082 => SR.Globalization_cp_10082,
                20000 => SR.Globalization_cp_20000,
                20001 => SR.Globalization_cp_20001,
                20002 => SR.Globalization_cp_20002,
                20003 => SR.Globalization_cp_20003,
                20004 => SR.Globalization_cp_20004,
                20005 => SR.Globalization_cp_20005,
                20105 => SR.Globalization_cp_20105,
                20106 => SR.Globalization_cp_20106,
                20107 => SR.Globalization_cp_20107,
                20108 => SR.Globalization_cp_20108,
                20261 => SR.Globalization_cp_20261,
                20269 => SR.Globalization_cp_20269,
                20273 => SR.Globalization_cp_20273,
                20277 => SR.Globalization_cp_20277,
                20278 => SR.Globalization_cp_20278,
                20280 => SR.Globalization_cp_20280,
                20284 => SR.Globalization_cp_20284,
                20285 => SR.Globalization_cp_20285,
                20290 => SR.Globalization_cp_20290,
                20297 => SR.Globalization_cp_20297,
                20420 => SR.Globalization_cp_20420,
                20423 => SR.Globalization_cp_20423,
                20424 => SR.Globalization_cp_20424,
                20833 => SR.Globalization_cp_20833,
                20838 => SR.Globalization_cp_20838,
                20866 => SR.Globalization_cp_20866,
                20871 => SR.Globalization_cp_20871,
                20880 => SR.Globalization_cp_20880,
                20905 => SR.Globalization_cp_20905,
                20924 => SR.Globalization_cp_20924,
                20932 => SR.Globalization_cp_20932,
                20936 => SR.Globalization_cp_20936,
                20949 => SR.Globalization_cp_20949,
                21025 => SR.Globalization_cp_21025,
                21027 => SR.Globalization_cp_21027,
                21866 => SR.Globalization_cp_21866,
                28592 => SR.Globalization_cp_28592,
                28593 => SR.Globalization_cp_28593,
                28594 => SR.Globalization_cp_28594,
                28595 => SR.Globalization_cp_28595,
                28596 => SR.Globalization_cp_28596,
                28597 => SR.Globalization_cp_28597,
                28598 => SR.Globalization_cp_28598,
                28599 => SR.Globalization_cp_28599,
                28603 => SR.Globalization_cp_28603,
                28605 => SR.Globalization_cp_28605,
                29001 => SR.Globalization_cp_29001,
                38598 => SR.Globalization_cp_38598,
                50000 => SR.Globalization_cp_50000,
                50220 => SR.Globalization_cp_50220,
                50221 => SR.Globalization_cp_50221,
                50222 => SR.Globalization_cp_50222,
                50225 => SR.Globalization_cp_50225,
                50227 => SR.Globalization_cp_50227,
                50229 => SR.Globalization_cp_50229,
                50930 => SR.Globalization_cp_50930,
                50931 => SR.Globalization_cp_50931,
                50933 => SR.Globalization_cp_50933,
                50935 => SR.Globalization_cp_50935,
                50937 => SR.Globalization_cp_50937,
                50939 => SR.Globalization_cp_50939,
                51932 => SR.Globalization_cp_51932,
                51936 => SR.Globalization_cp_51936,
                51949 => SR.Globalization_cp_51949,
                52936 => SR.Globalization_cp_52936,
                54936 => SR.Globalization_cp_54936,
                57002 => SR.Globalization_cp_57002,
                57003 => SR.Globalization_cp_57003,
                57004 => SR.Globalization_cp_57004,
                57005 => SR.Globalization_cp_57005,
                57006 => SR.Globalization_cp_57006,
                57007 => SR.Globalization_cp_57007,
                57008 => SR.Globalization_cp_57008,
                57009 => SR.Globalization_cp_57009,
                57010 => SR.Globalization_cp_57010,
                57011 => SR.Globalization_cp_57011,
                _ => null,
            };

        // Returns the IANA preferred name for this encoding
        public override string WebName
        {
            get
            {
                if (_webName is null)
                {
                    _webName = EncodingTable.GetWebNameFromCodePage(CodePage);
                    if (_webName is null)
                    {
                        throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, CodePage));
                    }
                }
                return _webName;
            }
        }

        public override string HeaderName =>
            CodePage switch
            {
                932 => "iso-2022-jp",
                50221 => "iso-2022-jp",
                50225 => "euc-kr",
                _ => WebName,
            };

        public override string BodyName =>
            CodePage switch
            {
                932 =>   "iso-2022-jp",
                1250 =>  "iso-8859-2",
                1251 =>  "koi8-r",
                1252 =>  "iso-8859-1",
                1253 =>  "iso-8859-7",
                1254 =>  "iso-8859-9",
                50221 => "iso-2022-jp",
                50225 => "iso-2022-kr",
                _ => WebName,
            };
    }
}
