// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Text
{
    // This abstract base class represents a character encoding. The class provides
    // methods to convert arrays and strings of Unicode characters to and from
    // arrays of bytes. A number of Encoding implementations are provided in
    // the System.Text package, including:
    //
    // ASCIIEncoding, which encodes Unicode characters as single 7-bit
    // ASCII characters. This encoding only supports character values between 0x00
    //     and 0x7F.
    // BaseCodePageEncoding, which encapsulates a Windows code page. Any
    //     installed code page can be accessed through this encoding, and conversions
    //     are performed using the WideCharToMultiByte and
    //     MultiByteToWideChar Windows API functions.
    // UnicodeEncoding, which encodes each Unicode character as two
    //    consecutive bytes. Both little-endian (code page 1200) and big-endian (code
    //    page 1201) encodings are recognized.
    // UTF7Encoding, which encodes Unicode characters using the UTF-7
    //     encoding (UTF-7 stands for UCS Transformation Format, 7-bit form). This
    //     encoding supports all Unicode character values, and can also be accessed
    //     as code page 65000.
    // UTF8Encoding, which encodes Unicode characters using the UTF-8
    //     encoding (UTF-8 stands for UCS Transformation Format, 8-bit form). This
    //     encoding supports all Unicode character values, and can also be accessed
    //     as code page 65001.
    // UTF32Encoding, both 12000 (little endian) & 12001 (big endian)
    //
    // In addition to directly instantiating Encoding objects, an
    // application can use the ForCodePage, GetASCII,
    // GetDefault, GetUnicode, GetUTF7, and GetUTF8
    // methods in this class to obtain encodings.
    //
    // Through an encoding, the GetBytes method is used to convert arrays
    // of characters to arrays of bytes, and the GetChars method is used to
    // convert arrays of bytes to arrays of characters. The GetBytes and
    // GetChars methods maintain no state between conversions, and are
    // generally intended for conversions of complete blocks of bytes and
    // characters in one operation. When the data to be converted is only available
    // in sequential blocks (such as data read from a stream) or when the amount of
    // data is so large that it needs to be divided into smaller blocks, an
    // application may choose to use a Decoder or an Encoder to
    // perform the conversion. Decoders and encoders allow sequential blocks of
    // data to be converted and they maintain the state required to support
    // conversions of data that spans adjacent blocks. Decoders and encoders are
    // obtained using the GetDecoder and GetEncoder methods.
    //
    // The core GetBytes and GetChars methods require the caller
    // to provide the destination buffer and ensure that the buffer is large enough
    // to hold the entire result of the conversion. When using these methods,
    // either directly on an Encoding object or on an associated
    // Decoder or Encoder, an application can use one of two methods
    // to allocate destination buffers.
    //
    // The GetByteCount and GetCharCount methods can be used to
    // compute the exact size of the result of a particular conversion, and an
    // appropriately sized buffer for that conversion can then be allocated.
    // The GetMaxByteCount and GetMaxCharCount methods can be
    // be used to compute the maximum possible size of a conversion of a given
    // number of bytes or characters, and a buffer of that size can then be reused
    // for multiple conversions.
    //
    // The first method generally uses less memory, whereas the second method
    // generally executes faster.
    //

    public abstract partial class Encoding : ICloneable
    {
        // For netcore we use UTF8 as default encoding since ANSI isn't available
        private static readonly UTF8Encoding.UTF8EncodingSealed s_defaultEncoding  = new UTF8Encoding.UTF8EncodingSealed(encoderShouldEmitUTF8Identifier: false);

        // Returns an encoding for the system's current ANSI code page.
        public static Encoding Default => s_defaultEncoding;

        //
        // The following values are from mlang.idl.  These values
        // should be in sync with those in mlang.idl.
        //
        internal const int MIMECONTF_MAILNEWS = 0x00000001;
        internal const int MIMECONTF_BROWSER = 0x00000002;
        internal const int MIMECONTF_SAVABLE_MAILNEWS = 0x00000100;
        internal const int MIMECONTF_SAVABLE_BROWSER = 0x00000200;

        // Special Case Code Pages
        private const int CodePageDefault = 0;
        private const int CodePageNoOEM = 1;        // OEM Code page not supported
        private const int CodePageNoMac = 2;        // MAC code page not supported
        private const int CodePageNoThread = 3;        // Thread code page not supported
        private const int CodePageNoSymbol = 42;       // Symbol code page not supported
        private const int CodePageUnicode = 1200;     // Unicode
        private const int CodePageBigEndian = 1201;     // Big Endian Unicode
        private const int CodePageWindows1252 = 1252;     // Windows 1252 code page

        // 20936 has same code page as 10008, so we'll special case it
        private const int CodePageMacGB2312 = 10008;
        private const int CodePageGB2312 = 20936;
        private const int CodePageMacKorean = 10003;
        private const int CodePageDLLKorean = 20949;

        // ISO 2022 Code Pages
        private const int ISO2022JP = 50220;
        private const int ISO2022JPESC = 50221;
        private const int ISO2022JPSISO = 50222;
        private const int ISOKorean = 50225;
        private const int ISOSimplifiedCN = 50227;
        private const int EUCJP = 51932;
        private const int ChineseHZ = 52936;    // HZ has ~}~{~~ sequences

        // 51936 is the same as 936
        private const int DuplicateEUCCN = 51936;
        private const int EUCCN = 936;

        private const int EUCKR = 51949;

        // Latin 1 & ASCII Code Pages
        internal const int CodePageASCII = 20127;    // ASCII
        internal const int ISO_8859_1 = 28591;    // Latin1

        // ISCII
        private const int ISCIIAssemese = 57006;
        private const int ISCIIBengali = 57003;
        private const int ISCIIDevanagari = 57002;
        private const int ISCIIGujarathi = 57010;
        private const int ISCIIKannada = 57008;
        private const int ISCIIMalayalam = 57009;
        private const int ISCIIOriya = 57007;
        private const int ISCIIPanjabi = 57011;
        private const int ISCIITamil = 57004;
        private const int ISCIITelugu = 57005;

        // GB18030
        private const int GB18030 = 54936;

        // Other
        private const int ISO_8859_8I = 38598;
        private const int ISO_8859_8_Visual = 28598;

        // 50229 is currently unsupported // "Chinese Traditional (ISO-2022)"
        private const int ENC50229 = 50229;

        // Special code pages
        private const int CodePageUTF7 = 65000;
        private const int CodePageUTF8 = 65001;
        private const int CodePageUTF32 = 12000;
        private const int CodePageUTF32BE = 12001;

        internal int _codePage = 0;

        internal CodePageDataItem? _dataItem = null;

        // Because of encoders we may be read only
        [OptionalField(VersionAdded = 2)]
        private bool _isReadOnly = true;

        // Encoding (encoder) fallback
        internal EncoderFallback encoderFallback = null!;
        internal DecoderFallback decoderFallback = null!;

        protected Encoding() : this(0)
        {
        }


        protected Encoding(int codePage)
        {
            // Validate code page
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(codePage));
            }

            // Remember code page
            _codePage = codePage;

            // Use default encoder/decoder fallbacks
            this.SetDefaultFallbacks();
        }

        // This constructor is needed to allow any sub-classing implementation to provide encoder/decoder fallback objects 
        // because the encoding object is always created as read-only object and don't allow setting encoder/decoder fallback 
        // after the creation is done. 
        protected Encoding(int codePage, EncoderFallback? encoderFallback, DecoderFallback? decoderFallback)
        {
            // Validate code page
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(codePage));
            }

            // Remember code page
            _codePage = codePage;

            this.encoderFallback = encoderFallback ?? new InternalEncoderBestFitFallback(this);
            this.decoderFallback = decoderFallback ?? new InternalDecoderBestFitFallback(this);
        }

        // Default fallback that we'll use.
        internal virtual void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an "\xFFFD" string,
            // For ASCII we use "?" replacement fallback, etc.
            encoderFallback = new InternalEncoderBestFitFallback(this);
            decoderFallback = new InternalDecoderBestFitFallback(this);
        }

        // Converts a byte array from one encoding to another. The bytes in the
        // bytes array are converted from srcEncoding to
        // dstEncoding, and the returned value is a new byte array
        // containing the result of the conversion.
        //
        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding,
            byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return Convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length);
        }

        // Converts a range of bytes in a byte array from one encoding to another.
        // This method converts count bytes from bytes starting at
        // index index from srcEncoding to dstEncoding, and
        // returns a new byte array containing the result of the conversion.
        //
        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding,
            byte[] bytes, int index, int count)
        {
            if (srcEncoding == null || dstEncoding == null)
            {
                throw new ArgumentNullException((srcEncoding == null ? nameof(srcEncoding) : nameof(dstEncoding)),
                    SR.ArgumentNull_Array);
            }
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes),
                    SR.ArgumentNull_Array);
            }

            return dstEncoding.GetBytes(srcEncoding.GetChars(bytes, index, count));
        }

        public static void RegisterProvider(EncodingProvider provider)
        {
            // Parameters validated inside EncodingProvider
            EncodingProvider.AddProvider(provider);
        }

        public static Encoding GetEncoding(int codepage)
        {
            Encoding? result = EncodingProvider.GetEncodingFromProvider(codepage);
            if (result != null)
                return result;

            switch (codepage)
            {
                case CodePageDefault: return Default;            // 0
                case CodePageUnicode: return Unicode;            // 1200
                case CodePageBigEndian: return BigEndianUnicode; // 1201
                case CodePageUTF32: return UTF32;                // 12000
                case CodePageUTF32BE: return BigEndianUTF32;     // 12001
                case CodePageUTF7: return UTF7;                  // 65000
                case CodePageUTF8: return UTF8;                  // 65001
                case CodePageASCII: return ASCII;                // 20127
                case ISO_8859_1: return Latin1;                  // 28591

                // We don't allow the following special code page values that Win32 allows.
                case CodePageNoOEM:                              // 1 CP_OEMCP
                case CodePageNoMac:                              // 2 CP_MACCP
                case CodePageNoThread:                           // 3 CP_THREAD_ACP
                case CodePageNoSymbol:                           // 42 CP_SYMBOL
                    throw new ArgumentException(SR.Format(SR.Argument_CodepageNotSupported, codepage), nameof(codepage));
            }

            if (codepage < 0 || codepage > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(codepage), SR.Format(SR.ArgumentOutOfRange_Range, 0, 65535));
            }

            throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, codepage));
        }

        public static Encoding GetEncoding(int codepage,
            EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding? baseEncoding = EncodingProvider.GetEncodingFromProvider(codepage, encoderFallback, decoderFallback);

            if (baseEncoding != null)
                return baseEncoding;

            // Get the default encoding (which is cached and read only)
            baseEncoding = GetEncoding(codepage);

            // Clone it and set the fallback
            Encoding fallbackEncoding = (Encoding)baseEncoding.Clone();
            fallbackEncoding.EncoderFallback = encoderFallback;
            fallbackEncoding.DecoderFallback = decoderFallback;

            return fallbackEncoding;
        }

        // Returns an Encoding object for a given name or a given code page value.
        //
        public static Encoding GetEncoding(string name)
        {
            Encoding? baseEncoding = EncodingProvider.GetEncodingFromProvider(name);
            if (baseEncoding != null)
                return baseEncoding;

            //
            // NOTE: If you add a new encoding that can be requested by name, be sure to
            // add the corresponding item in EncodingTable.
            // Otherwise, the code below will throw exception when trying to call
            // EncodingTable.GetCodePageFromName().
            //
            return GetEncoding(EncodingTable.GetCodePageFromName(name));
        }

        // Returns an Encoding object for a given name or a given code page value.
        //
        public static Encoding GetEncoding(string name,
            EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding? baseEncoding = EncodingProvider.GetEncodingFromProvider(name, encoderFallback, decoderFallback);
            if (baseEncoding != null)
                return baseEncoding;

            //
            // NOTE: If you add a new encoding that can be requested by name, be sure to
            // add the corresponding item in EncodingTable.
            // Otherwise, the code below will throw exception when trying to call
            // EncodingTable.GetCodePageFromName().
            //
            return GetEncoding(EncodingTable.GetCodePageFromName(name), encoderFallback, decoderFallback);
        }

        // Return a list of all EncodingInfo objects describing all of our encodings
        public static EncodingInfo[] GetEncodings()
        {
            return EncodingTable.GetEncodings();
        }

        public virtual byte[] GetPreamble()
        {
            return Array.Empty<byte>();
        }

        public virtual ReadOnlySpan<byte> Preamble => GetPreamble();

        private void GetDataItem()
        {
            if (_dataItem == null)
            {
                _dataItem = EncodingTable.GetCodePageDataItem(_codePage);
                if (_dataItem == null)
                {
                    throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, _codePage));
                }
            }
        }

        // Returns the name for this encoding that can be used with mail agent body tags.
        // If the encoding may not be used, the string is empty.

        public virtual string? BodyName
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return _dataItem!.BodyName;
            }
        }

        // Returns the human-readable description of the encoding ( e.g. Hebrew (DOS)).
        public virtual string? EncodingName
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                
                return _dataItem!.DisplayName;
            }
        }

        // Returns the name for this encoding that can be used with mail agent header
        // tags.  If the encoding may not be used, the string is empty.

        public virtual string? HeaderName
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return _dataItem!.HeaderName;
            }
        }

        // Returns the IANA preferred name for this encoding.
        public virtual string? WebName
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return _dataItem!.WebName;
            }
        }

        // Returns the windows code page that most closely corresponds to this encoding.

        public virtual int WindowsCodePage
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return _dataItem!.UIFamilyCodePage;
            }
        }


        // True if and only if the encoding is used for display by browsers clients.

        public virtual bool IsBrowserDisplay
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return (_dataItem!.Flags & MIMECONTF_BROWSER) != 0;
            }
        }

        // True if and only if the encoding is used for saving by browsers clients.

        public virtual bool IsBrowserSave
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return (_dataItem!.Flags & MIMECONTF_SAVABLE_BROWSER) != 0;
            }
        }

        // True if and only if the encoding is used for display by mail and news clients.

        public virtual bool IsMailNewsDisplay
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return (_dataItem!.Flags & MIMECONTF_MAILNEWS) != 0;
            }
        }


        // True if and only if the encoding is used for saving documents by mail and
        // news clients

        public virtual bool IsMailNewsSave
        {
            get
            {
                if (_dataItem == null)
                {
                    GetDataItem();
                }
                return (_dataItem!.Flags & MIMECONTF_SAVABLE_MAILNEWS) != 0;
            }
        }

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)

        public virtual bool IsSingleByte
        {
            get
            {
                return false;
            }
        }


        public EncoderFallback EncoderFallback
        {
            get
            {
                return encoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                encoderFallback = value;
            }
        }


        public DecoderFallback DecoderFallback
        {
            get
            {
                return decoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                decoderFallback = value;
            }
        }


        public virtual object Clone()
        {
            Encoding newEncoding = (Encoding)this.MemberwiseClone();

            // New one should be readable
            newEncoding._isReadOnly = false;
            return newEncoding;
        }

        public bool IsReadOnly
        {
            get
            {
                return (_isReadOnly);
            }
            private protected set
            {
                _isReadOnly = value;
            }
        }

        // Returns an encoding for the ASCII character set. The returned encoding
        // will be an instance of the ASCIIEncoding class.

        public static Encoding ASCII => ASCIIEncoding.s_default;

        // Returns an encoding for the Latin1 character set. The returned encoding
        // will be an instance of the Latin1Encoding class.
        //
        // This is for our optimizations
        private static Encoding Latin1 => Latin1Encoding.s_default;

        // Returns the number of bytes required to encode the given character
        // array.
        //
        public virtual int GetByteCount(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars),
                    SR.ArgumentNull_Array);
            }

            return GetByteCount(chars, 0, chars.Length);
        }

        public virtual int GetByteCount(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            char[] chars = s.ToCharArray();
            return GetByteCount(chars, 0, chars.Length);
        }

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        public abstract int GetByteCount(char[] chars, int index, int count);

        // Returns the number of bytes required to encode a string range.
        //
        public int GetByteCount(string s, int index, int count)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s),
                    SR.ArgumentNull_String);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index),
                      SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > s.Length - count)
                throw new ArgumentOutOfRangeException(nameof(index),
                      SR.ArgumentOutOfRange_IndexCount);

            unsafe
            {
                fixed (char* pChar = s)
                {
                    return GetByteCount(pChar + index, count);
                }
            }
        }

        // We expect this to be the workhorse for NLS encodings
        // unfortunately for existing overrides, it has to call the [] version,
        // which is really slow, so this method should be avoided if you're calling
        // a 3rd party encoding.
        [CLSCompliant(false)]
        public virtual unsafe int GetByteCount(char* chars, int count)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            char[] arrChar = new char[count];
            int index;

            for (index = 0; index < count; index++)
                arrChar[index] = chars[index];

            return GetByteCount(arrChar, 0, count);
        }

        public virtual unsafe int GetByteCount(ReadOnlySpan<char> chars)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            {
                return GetByteCount(charsPtr, chars.Length);
            }
        }

        // Returns a byte array containing the encoded representation of the given
        // character array.
        //
        public virtual byte[] GetBytes(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars),
                    SR.ArgumentNull_Array);
            }
            return GetBytes(chars, 0, chars.Length);
        }

        // Returns a byte array containing the encoded representation of a range
        // of characters in a character array.
        //
        public virtual byte[] GetBytes(char[] chars, int index, int count)
        {
            byte[] result = new byte[GetByteCount(chars, index, count)];
            GetBytes(chars, index, count, result, 0);
            return result;
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
        public abstract int GetBytes(char[] chars, int charIndex, int charCount,
            byte[] bytes, int byteIndex);

        // Returns a byte array containing the encoded representation of the given
        // string.
        //
        public virtual byte[] GetBytes(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s),
                    SR.ArgumentNull_String);

            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];
            int bytesReceived = GetBytes(s, 0, s.Length, bytes, 0);
            Debug.Assert(byteCount == bytesReceived);
            return bytes;
        }

        // Returns a byte array containing the encoded representation of the given
        // string range.
        //
        public byte[] GetBytes(string s, int index, int count)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s),
                    SR.ArgumentNull_String);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index),
                      SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > s.Length - count)
                throw new ArgumentOutOfRangeException(nameof(index),
                      SR.ArgumentOutOfRange_IndexCount);

            unsafe
            {
                fixed (char* pChar = s)
                {
                    int byteCount = GetByteCount(pChar + index, count);
                    if (byteCount == 0)
                        return Array.Empty<byte>();

                    byte[] bytes = new byte[byteCount];
                    fixed (byte* pBytes = &bytes[0])
                    {
                        int bytesReceived = GetBytes(pChar + index, count, pBytes, byteCount);
                        Debug.Assert(byteCount == bytesReceived);
                    }
                    return bytes;
                }
            }
        }

        public virtual int GetBytes(string s, int charIndex, int charCount,
                                       byte[] bytes, int byteIndex)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implementation)
        //
        // WARNING WARNING WARNING
        //
        // WARNING: If this breaks it could be a security threat.  Obviously we
        // call this internally, so you need to make sure that your pointers, counts
        // and indexes are correct when you call this method.
        //
        // In addition, we have internal code, which will be marked as "safe" calling
        // this code.  However this code is dependent upon the implementation of an
        // external GetBytes() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the byte[] to our byte* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow byteCount either.

        [CLSCompliant(false)]
        public virtual unsafe int GetBytes(char* chars, int charCount,
                                              byte* bytes, int byteCount)
        {
            // Validate input parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars),
                    SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // Get the char array to convert
            char[] arrChar = new char[charCount];

            int index;
            for (index = 0; index < charCount; index++)
                arrChar[index] = chars[index];

            // Get the byte array to fill
            byte[] arrByte = new byte[byteCount];

            // Do the work
            int result = GetBytes(arrChar, 0, charCount, arrByte, 0);

            Debug.Assert(result <= byteCount, "[Encoding.GetBytes]Returned more bytes than we have space for");

            // Copy the byte array
            // WARNING: We MUST make sure that we don't copy too many bytes.  We can't
            // rely on result because it could be a 3rd party implementation.  We need
            // to make sure we never copy more than byteCount bytes no matter the value
            // of result
            if (result < byteCount)
                byteCount = result;

            // Copy the data, don't overrun our array!
            for (index = 0; index < byteCount; index++)
                bytes[index] = arrByte[index];

            return byteCount;
        }

        public virtual unsafe int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            {
                return GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }

        // Returns the number of characters produced by decoding the given byte
        // array.
        //
        public virtual int GetCharCount(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes),
                    SR.ArgumentNull_Array);
            }
            return GetCharCount(bytes, 0, bytes.Length);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        public abstract int GetCharCount(byte[] bytes, int index, int count);

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implementation)
        [CLSCompliant(false)]
        public virtual unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate input parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] arrbyte = new byte[count];
            int index;

            for (index = 0; index < count; index++)
                arrbyte[index] = bytes[index];

            return GetCharCount(arrbyte, 0, count);
        }

        public virtual unsafe int GetCharCount(ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            {
                return GetCharCount(bytesPtr, bytes.Length);
            }
        }

        // Returns a character array containing the decoded representation of a
        // given byte array.
        //
        public virtual char[] GetChars(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes),
                    SR.ArgumentNull_Array);
            }
            return GetChars(bytes, 0, bytes.Length);
        }

        // Returns a character array containing the decoded representation of a
        // range of bytes in a byte array.
        //
        public virtual char[] GetChars(byte[] bytes, int index, int count)
        {
            char[] result = new char[GetCharCount(bytes, index, count)];
            GetChars(bytes, index, count, result, 0);
            return result;
        }

        // Decodes a range of bytes in a byte array into a range of characters in a
        // character array. An exception occurs if the character array is not large
        // enough to hold the complete decoding of the bytes. The
        // GetCharCount method can be used to determine the exact number of
        // characters that will be produced for a given range of bytes.
        // Alternatively, the GetMaxCharCount method can be used to
        // determine the maximum number of characters that will be produced for a
        // given number of bytes, regardless of the actual byte values.
        //

        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                       char[] chars, int charIndex);


        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implementation)
        //
        // WARNING WARNING WARNING
        //
        // WARNING: If this breaks it could be a security threat.  Obviously we
        // call this internally, so you need to make sure that your pointers, counts
        // and indexes are correct when you call this method.
        //
        // In addition, we have internal code, which will be marked as "safe" calling
        // this code.  However this code is dependent upon the implementation of an
        // external GetChars() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the char[] to our char* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow charCount either.

        [CLSCompliant(false)]
        public virtual unsafe int GetChars(byte* bytes, int byteCount,
                                              char* chars, int charCount)
        {
            // Validate input parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes),
                    SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // Get the byte array to convert
            byte[] arrByte = new byte[byteCount];

            int index;
            for (index = 0; index < byteCount; index++)
                arrByte[index] = bytes[index];

            // Get the char array to fill
            char[] arrChar = new char[charCount];

            // Do the work
            int result = GetChars(arrByte, 0, byteCount, arrChar, 0);

            Debug.Assert(result <= charCount, "[Encoding.GetChars]Returned more chars than we have space for");

            // Copy the char array
            // WARNING: We MUST make sure that we don't copy too many chars.  We can't
            // rely on result because it could be a 3rd party implementation.  We need
            // to make sure we never copy more than charCount chars no matter the value
            // of result
            if (result < charCount)
                charCount = result;

            // Copy the data, don't overrun our array!
            for (index = 0; index < charCount; index++)
                chars[index] = arrChar[index];

            return charCount;
        }

        public virtual unsafe int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            {
                return GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
            }
        }

        [CLSCompliant(false)]
        public unsafe string GetString(byte* bytes, int byteCount)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return string.CreateStringFromEncoding(bytes, byteCount, this);
        }

        public unsafe string GetString(ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            {
                return string.CreateStringFromEncoding(bytesPtr, bytes.Length, this);
            }
        }


        // Returns the code page identifier of this encoding. The returned value is
        // an integer between 0 and 65535 if the encoding has a code page
        // identifier, or -1 if the encoding does not represent a code page.
        //

        public virtual int CodePage
        {
            get
            {
                return _codePage;
            }
        }

        // IsAlwaysNormalized
        // Returns true if the encoding is always normalized for the specified encoding form
        public bool IsAlwaysNormalized()
        {
            return this.IsAlwaysNormalized(NormalizationForm.FormC);
        }

        public virtual bool IsAlwaysNormalized(NormalizationForm form)
        {
            // Assume false unless the encoding knows otherwise
            return false;
        }

        // Returns a Decoder object for this encoding. The returned object
        // can be used to decode a sequence of bytes into a sequence of characters.
        // Contrary to the GetChars family of methods, a Decoder can
        // convert partial sequences of bytes into partial sequences of characters
        // by maintaining the appropriate state between the conversions.
        //
        // This default implementation returns a Decoder that simply
        // forwards calls to the GetCharCount and GetChars methods to
        // the corresponding methods of this encoding. Encodings that require state
        // to be maintained between successive conversions should override this
        // method and return an instance of an appropriate Decoder
        // implementation.
        //

        public virtual Decoder GetDecoder()
        {
            return new DefaultDecoder(this);
        }

        // Returns an Encoder object for this encoding. The returned object
        // can be used to encode a sequence of characters into a sequence of bytes.
        // Contrary to the GetBytes family of methods, an Encoder can
        // convert partial sequences of characters into partial sequences of bytes
        // by maintaining the appropriate state between the conversions.
        //
        // This default implementation returns an Encoder that simply
        // forwards calls to the GetByteCount and GetBytes methods to
        // the corresponding methods of this encoding. Encodings that require state
        // to be maintained between successive conversions should override this
        // method and return an instance of an appropriate Encoder
        // implementation.
        //

        public virtual Encoder GetEncoder()
        {
            return new DefaultEncoder(this);
        }

        // Returns the maximum number of bytes required to encode a given number of
        // characters. This method can be used to determine an appropriate buffer
        // size for byte arrays passed to the GetBytes method of this
        // encoding or the GetBytes method of an Encoder for this
        // encoding. All encodings must guarantee that no buffer overflow
        // exceptions will occur if buffers are sized according to the results of
        // this method.
        //
        // WARNING: If you're using something besides the default replacement encoder fallback,
        // then you could have more bytes than this returned from an actual call to GetBytes().
        //
        public abstract int GetMaxByteCount(int charCount);

        // Returns the maximum number of characters produced by decoding a given
        // number of bytes. This method can be used to determine an appropriate
        // buffer size for character arrays passed to the GetChars method of
        // this encoding or the GetChars method of a Decoder for this
        // encoding. All encodings must guarantee that no buffer overflow
        // exceptions will occur if buffers are sized according to the results of
        // this method.
        //
        public abstract int GetMaxCharCount(int byteCount);

        // Returns a string containing the decoded representation of a given byte
        // array.
        //
        public virtual string GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes),
                    SR.ArgumentNull_Array);

            return GetString(bytes, 0, bytes.Length);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // Internally we override this for performance
        //
        public virtual string GetString(byte[] bytes, int index, int count)
        {
            return new string(GetChars(bytes, index, count));
        }

        // Returns an encoding for Unicode format. The returned encoding will be
        // an instance of the UnicodeEncoding class.
        //
        // It will use little endian byte order, but will detect
        // input in big endian if it finds a byte order mark per Unicode 2.0.

        public static Encoding Unicode => UnicodeEncoding.s_littleEndianDefault;

        // Returns an encoding for Unicode format. The returned encoding will be
        // an instance of the UnicodeEncoding class.
        //
        // It will use big endian byte order, but will detect
        // input in little endian if it finds a byte order mark per Unicode 2.0.

        public static Encoding BigEndianUnicode => UnicodeEncoding.s_bigEndianDefault;

        // Returns an encoding for the UTF-7 format. The returned encoding will be
        // an instance of the UTF7Encoding class.

        public static Encoding UTF7 => UTF7Encoding.s_default;

        // Returns an encoding for the UTF-8 format. The returned encoding will be
        // an instance of the UTF8Encoding class.

        public static Encoding UTF8 => UTF8Encoding.s_default;

        // Returns an encoding for the UTF-32 format. The returned encoding will be
        // an instance of the UTF32Encoding class.

        public static Encoding UTF32 => UTF32Encoding.s_default;

        // Returns an encoding for the UTF-32 format. The returned encoding will be
        // an instance of the UTF32Encoding class.
        //
        // It will use big endian byte order.

        private static Encoding BigEndianUTF32 => UTF32Encoding.s_bigEndianDefault;

        public override bool Equals(object? value)
        {
            if (value is Encoding that)
                return (_codePage == that._codePage) &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            return (false);
        }


        public override int GetHashCode()
        {
            return _codePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

        internal virtual char[] GetBestFitUnicodeToBytesData()
        {
            // Normally we don't have any best fit data.
            return Array.Empty<char>();
        }

        internal virtual char[] GetBestFitBytesToUnicodeData()
        {
            // Normally we don't have any best fit data.
            return Array.Empty<char>();
        }

        [DoesNotReturn]
        internal void ThrowBytesOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(
                SR.Format(SR.Argument_EncodingConversionOverflowBytes, EncodingName, EncoderFallback.GetType()), "bytes");
        }

        internal void ThrowBytesOverflow(EncoderNLS? encoder, bool nothingEncoded)
        {
            if (encoder == null || encoder._throwOnOverflow || nothingEncoded)
            {
                if (encoder != null && encoder.InternalHasFallbackBuffer)
                    encoder.FallbackBuffer.InternalReset();
                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented an encoder fallback with a broken GetMaxCharCount
                ThrowBytesOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            encoder!.ClearMustFlush();
        }

        [DoesNotReturn]
        [StackTraceHidden]
        internal static void ThrowConversionOverflow()
        {
            throw new ArgumentException(SR.Argument_ConversionOverflow);
        }

        [DoesNotReturn]
        [StackTraceHidden]
        internal void ThrowCharsOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(
                SR.Format(SR.Argument_EncodingConversionOverflowChars, EncodingName, DecoderFallback.GetType()), "chars");
        }

        internal void ThrowCharsOverflow(DecoderNLS? decoder, bool nothingDecoded)
        {
            if (decoder == null || decoder._throwOnOverflow || nothingDecoded)
            {
                if (decoder != null && decoder.InternalHasFallbackBuffer)
                    decoder.FallbackBuffer.InternalReset();

                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implemented a decoder fallback with a broken GetMaxCharCount
                ThrowCharsOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            decoder!.ClearMustFlush();
        }

        internal sealed class DefaultEncoder : Encoder, IObjectReference
        {
            private Encoding _encoding;

            public DefaultEncoder(Encoding encoding)
            {
                _encoding = encoding;
            }
            
            public object GetRealObject(StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }

            // Returns the number of bytes the next call to GetBytes will
            // produce if presented with the given range of characters and the given
            // value of the flush parameter. The returned value takes into
            // account the state in which the encoder was left following the last call
            // to GetBytes. The state of the encoder is not affected by a call
            // to this method.
            //

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                return _encoding.GetByteCount(chars, index, count);
            }

            public unsafe override int GetByteCount(char* chars, int count, bool flush)
            {
                return _encoding.GetByteCount(chars, count);
            }

            // Encodes a range of characters in a character array into a range of bytes
            // in a byte array. The method encodes charCount characters from
            // chars starting at index charIndex, storing the resulting
            // bytes in bytes starting at index byteIndex. The encoding
            // takes into account the state in which the encoder was left following the
            // last call to this method. The flush parameter indicates whether
            // the encoder should flush any shift-states and partial characters at the
            // end of the conversion. To ensure correct termination of a sequence of
            // blocks of encoded bytes, the last call to GetBytes should specify
            // a value of true for the flush parameter.
            //
            // An exception occurs if the byte array is not large enough to hold the
            // complete encoding of the characters. The GetByteCount method can
            // be used to determine the exact number of bytes that will be produced for
            // a given range of characters. Alternatively, the GetMaxByteCount
            // method of the Encoding that produced this encoder can be used to
            // determine the maximum number of bytes that will be produced for a given
            // number of characters, regardless of the actual character values.
            //

            public override int GetBytes(char[] chars, int charIndex, int charCount,
                                          byte[] bytes, int byteIndex, bool flush)
            {
                return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }

            public unsafe override int GetBytes(char* chars, int charCount,
                                                 byte* bytes, int byteCount, bool flush)
            {
                return _encoding.GetBytes(chars, charCount, bytes, byteCount);
            }
        }

        internal sealed class DefaultDecoder : Decoder, IObjectReference
        {
            private Encoding _encoding;

            public DefaultDecoder(Encoding encoding)
            {
                _encoding = encoding;
            }

            public object GetRealObject(StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }

            // Returns the number of characters the next call to GetChars will
            // produce if presented with the given range of bytes. The returned value
            // takes into account the state in which the decoder was left following the
            // last call to GetChars. The state of the decoder is not affected
            // by a call to this method.
            //

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return GetCharCount(bytes, index, count, false);
            }

            public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
            {
                return _encoding.GetCharCount(bytes, index, count);
            }

            public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
            {
                // By default just call the encoding version, no flush by default
                return _encoding.GetCharCount(bytes, count);
            }

            // Decodes a range of bytes in a byte array into a range of characters
            // in a character array. The method decodes byteCount bytes from
            // bytes starting at index byteIndex, storing the resulting
            // characters in chars starting at index charIndex. The
            // decoding takes into account the state in which the decoder was left
            // following the last call to this method.
            //
            // An exception occurs if the character array is not large enough to
            // hold the complete decoding of the bytes. The GetCharCount method
            // can be used to determine the exact number of characters that will be
            // produced for a given range of bytes. Alternatively, the
            // GetMaxCharCount method of the Encoding that produced this
            // decoder can be used to determine the maximum number of characters that
            // will be produced for a given number of bytes, regardless of the actual
            // byte values.
            //

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                           char[] chars, int charIndex)
            {
                return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                           char[] chars, int charIndex, bool flush)
            {
                return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }

            public unsafe override int GetChars(byte* bytes, int byteCount,
                                                  char* chars, int charCount, bool flush)
            {
                // By default just call the encoding's version
                return _encoding.GetChars(bytes, byteCount, chars, charCount);
            }
        }

        internal class EncodingCharBuffer
        {
            private unsafe char* _chars;
            private unsafe char* _charStart;
            private unsafe char* _charEnd;
            private int _charCountResult = 0;
            private Encoding _enc;
            private DecoderNLS? _decoder;
            private unsafe byte* _byteStart;
            private unsafe byte* _byteEnd;
            private unsafe byte* _bytes;
            private DecoderFallbackBuffer _fallbackBuffer;

            internal unsafe EncodingCharBuffer(Encoding enc, DecoderNLS? decoder, char* charStart, int charCount,
                                                    byte* byteStart, int byteCount)
            {
                _enc = enc;
                _decoder = decoder;

                _chars = charStart;
                _charStart = charStart;
                _charEnd = charStart + charCount;

                _byteStart = byteStart;
                _bytes = byteStart;
                _byteEnd = byteStart + byteCount;

                if (_decoder == null)
                    _fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
                else
                    _fallbackBuffer = _decoder.FallbackBuffer;

                // If we're getting chars or getting char count we don't expect to have
                // to remember fallbacks between calls (so it should be empty)
                Debug.Assert(_fallbackBuffer.Remaining == 0,
                    "[Encoding.EncodingCharBuffer.EncodingCharBuffer]Expected empty fallback buffer for getchars/charcount");
                _fallbackBuffer.InternalInitialize(_bytes, _charEnd);
            }

            internal unsafe bool AddChar(char ch, int numBytes)
            {
                if (_chars != null)
                {
                    if (_chars >= _charEnd)
                    {
                        // Throw maybe
                        _bytes -= numBytes;                                        // Didn't encode these bytes
                        _enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);    // Throw?
                        return false;                                           // No throw, but no store either
                    }

                    *(_chars++) = ch;
                }
                _charCountResult++;
                return true;
            }

            internal bool AddChar(char ch)
            {
                return AddChar(ch, 1);
            }


            internal unsafe bool AddChar(char ch1, char ch2, int numBytes)
            {
                // Need room for 2 chars
                if (_chars >= _charEnd - 1)
                {
                    // Throw maybe
                    _bytes -= numBytes;                                        // Didn't encode these bytes
                    _enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);    // Throw?
                    return false;                                           // No throw, but no store either
                }
                return AddChar(ch1, numBytes) && AddChar(ch2, numBytes);
            }

            internal unsafe void AdjustBytes(int count)
            {
                _bytes += count;
            }

            internal unsafe bool MoreData
            {
                get
                {
                    return _bytes < _byteEnd;
                }
            }

            // Do we have count more bytes?
            internal unsafe bool EvenMoreData(int count)
            {
                return (_bytes <= _byteEnd - count);
            }

            // GetNextByte shouldn't be called unless the caller's already checked more data or even more data,
            // but we'll double check just to make sure.
            internal unsafe byte GetNextByte()
            {
                Debug.Assert(_bytes < _byteEnd, "[EncodingCharBuffer.GetNextByte]Expected more date");
                if (_bytes >= _byteEnd)
                    return 0;
                return *(_bytes++);
            }

            internal unsafe int BytesUsed
            {
                get
                {
                    return (int)(_bytes - _byteStart);
                }
            }

            internal bool Fallback(byte fallbackByte)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { fallbackByte };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            internal bool Fallback(byte byte1, byte byte2)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { byte1, byte2 };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            internal bool Fallback(byte byte1, byte byte2, byte byte3, byte byte4)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { byte1, byte2, byte3, byte4 };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            internal unsafe bool Fallback(byte[] byteBuffer)
            {
                // Do the fallback and add the data.
                if (_chars != null)
                {
                    char* pTemp = _chars;
                    if (_fallbackBuffer.InternalFallback(byteBuffer, _bytes, ref _chars) == false)
                    {
                        // Throw maybe
                        _bytes -= byteBuffer.Length;                             // Didn't use how many ever bytes we're falling back
                        _fallbackBuffer.InternalReset();                         // We didn't use this fallback.
                        _enc.ThrowCharsOverflow(_decoder, _chars == _charStart);    // Throw?
                        return false;                                           // No throw, but no store either
                    }
                    _charCountResult += unchecked((int)(_chars - pTemp));
                }
                else
                {
                    _charCountResult += _fallbackBuffer.InternalFallback(byteBuffer, _bytes);
                }

                return true;
            }

            internal int Count
            {
                get
                {
                    return _charCountResult;
                }
            }
        }

        internal class EncodingByteBuffer
        {
            private unsafe byte* _bytes;
            private unsafe byte* _byteStart;
            private unsafe byte* _byteEnd;
            private unsafe char* _chars;
            private unsafe char* _charStart;
            private unsafe char* _charEnd;
            private int _byteCountResult = 0;
            private Encoding _enc;
            private EncoderNLS? _encoder;
            internal EncoderFallbackBuffer fallbackBuffer;

            internal unsafe EncodingByteBuffer(Encoding inEncoding, EncoderNLS? inEncoder,
                        byte* inByteStart, int inByteCount, char* inCharStart, int inCharCount)
            {
                _enc = inEncoding;
                _encoder = inEncoder;

                _charStart = inCharStart;
                _chars = inCharStart;
                _charEnd = inCharStart + inCharCount;

                _bytes = inByteStart;
                _byteStart = inByteStart;
                _byteEnd = inByteStart + inByteCount;

                if (_encoder == null)
                    this.fallbackBuffer = _enc.EncoderFallback.CreateFallbackBuffer();
                else
                {
                    this.fallbackBuffer = _encoder.FallbackBuffer;
                    // If we're not converting we must not have data in our fallback buffer
                    if (_encoder._throwOnOverflow && _encoder.InternalHasFallbackBuffer &&
                        this.fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty,
                            _encoder.Encoding.EncodingName, _encoder.Fallback!.GetType()));
                }
                fallbackBuffer.InternalInitialize(_chars, _charEnd, _encoder, _bytes != null);
            }

            internal unsafe bool AddByte(byte b, int moreBytesExpected)
            {
                Debug.Assert(moreBytesExpected >= 0, "[EncodingByteBuffer.AddByte]expected non-negative moreBytesExpected");
                if (_bytes != null)
                {
                    if (_bytes >= _byteEnd - moreBytesExpected)
                    {
                        // Throw maybe.  Check which buffer to back up (only matters if Converting)
                        this.MovePrevious(true);            // Throw if necessary
                        return false;                       // No throw, but no store either
                    }

                    *(_bytes++) = b;
                }
                _byteCountResult++;
                return true;
            }

            internal bool AddByte(byte b1)
            {
                return AddByte(b1, 0);
            }

            internal bool AddByte(byte b1, byte b2)
            {
                return AddByte(b1, b2, 0);
            }

            internal bool AddByte(byte b1, byte b2, int moreBytesExpected)
            {
                return AddByte(b1, 1 + moreBytesExpected) && AddByte(b2, moreBytesExpected);
            }

            internal bool AddByte(byte b1, byte b2, byte b3)
            {
                return AddByte(b1, b2, b3, (int)0);
            }

            internal bool AddByte(byte b1, byte b2, byte b3, int moreBytesExpected)
            {
                return AddByte(b1, 2 + moreBytesExpected) &&
                        AddByte(b2, 1 + moreBytesExpected) &&
                        AddByte(b3, moreBytesExpected);
            }

            internal bool AddByte(byte b1, byte b2, byte b3, byte b4)
            {
                return AddByte(b1, 3) &&
                        AddByte(b2, 2) &&
                        AddByte(b3, 1) &&
                        AddByte(b4, 0);
            }

            internal unsafe void MovePrevious(bool bThrow)
            {
                if (fallbackBuffer.bFallingBack)
                    fallbackBuffer.MovePrevious();                      // don't use last fallback
                else
                {
                    Debug.Assert(_chars > _charStart ||
                        ((bThrow == true) && (_bytes == _byteStart)),
                        "[EncodingByteBuffer.MovePrevious]expected previous data or throw");
                    if (_chars > _charStart)
                        _chars--;                                        // don't use last char
                }

                if (bThrow)
                    _enc.ThrowBytesOverflow(_encoder, _bytes == _byteStart);    // Throw? (and reset fallback if not converting)
            }

            internal unsafe bool Fallback(char charFallback)
            {
                // Do the fallback
                return fallbackBuffer.InternalFallback(charFallback, ref _chars);
            }

            internal unsafe bool MoreData
            {
                get
                {
                    // See if fallbackBuffer is not empty or if there's data left in chars buffer.
                    return (fallbackBuffer.Remaining > 0) || (_chars < _charEnd);
                }
            }

            internal unsafe char GetNextChar()
            {
                // See if there's something in our fallback buffer
                char cReturn = fallbackBuffer.InternalGetNextChar();

                // Nothing in the fallback buffer, return our normal data.
                if (cReturn == 0)
                {
                    if (_chars < _charEnd)
                        cReturn = *(_chars++);
                }

                return cReturn;
            }

            internal unsafe int CharsUsed
            {
                get
                {
                    return (int)(_chars - _charStart);
                }
            }

            internal int Count
            {
                get
                {
                    return _byteCountResult;
                }
            }
        }
    }
}
