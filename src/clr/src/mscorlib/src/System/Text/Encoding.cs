// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Text;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using Win32Native = Microsoft.Win32.Win32Native;

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

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public abstract class Encoding : ICloneable
    {
        private static Encoding defaultEncoding;

        //
        // The following values are from mlang.idl.  These values
        // should be in sync with those in mlang.idl.
        //
        internal const int MIMECONTF_MAILNEWS          = 0x00000001;
        internal const int MIMECONTF_BROWSER           = 0x00000002;
        internal const int MIMECONTF_SAVABLE_MAILNEWS  = 0x00000100;
        internal const int MIMECONTF_SAVABLE_BROWSER   = 0x00000200;

        // Special Case Code Pages
        private const int CodePageDefault       = 0;
        private const int CodePageNoOEM         = 1;        // OEM Code page not supported
        private const int CodePageNoMac         = 2;        // MAC code page not supported
        private const int CodePageNoThread      = 3;        // Thread code page not supported
        private const int CodePageNoSymbol      = 42;       // Symbol code page not supported
        private const int CodePageUnicode       = 1200;     // Unicode
        private const int CodePageBigEndian     = 1201;     // Big Endian Unicode
        private const int CodePageWindows1252   = 1252;     // Windows 1252 code page

        // 20936 has same code page as 10008, so we'll special case it
        private const int CodePageMacGB2312 = 10008;
        private const int CodePageGB2312    = 20936;
        private const int CodePageMacKorean = 10003;
        private const int CodePageDLLKorean = 20949;

        // ISO 2022 Code Pages
        private const int ISO2022JP         = 50220;
        private const int ISO2022JPESC      = 50221;
        private const int ISO2022JPSISO     = 50222;
        private const int ISOKorean         = 50225;
        private const int ISOSimplifiedCN   = 50227;
        private const int EUCJP             = 51932;
        private const int ChineseHZ         = 52936;    // HZ has ~}~{~~ sequences

        // 51936 is the same as 936
        private const int DuplicateEUCCN    = 51936;
        private const int EUCCN             = 936;

        private const int EUCKR             = 51949;

        // Latin 1 & ASCII Code Pages
        internal const int CodePageASCII    = 20127;    // ASCII
        internal const int ISO_8859_1       = 28591;    // Latin1

        // ISCII
        private const int ISCIIAssemese     = 57006;
        private const int ISCIIBengali      = 57003;
        private const int ISCIIDevanagari   = 57002;
        private const int ISCIIGujarathi    = 57010;
        private const int ISCIIKannada      = 57008;
        private const int ISCIIMalayalam    = 57009;
        private const int ISCIIOriya        = 57007;
        private const int ISCIIPanjabi      = 57011;
        private const int ISCIITamil        = 57004;
        private const int ISCIITelugu       = 57005;

        // GB18030
        private const int GB18030           = 54936;

        // Other
        private const int ISO_8859_8I       = 38598;
        private const int ISO_8859_8_Visual = 28598;

        // 50229 is currently unsupported // "Chinese Traditional (ISO-2022)"
        private const int ENC50229          = 50229;

        // Special code pages
        private const int CodePageUTF7      = 65000;
        private const int CodePageUTF8      = 65001;
        private const int CodePageUTF32     = 12000;
        private const int CodePageUTF32BE   = 12001;

        internal int m_codePage = 0;

        // dataItem should be internal (not private). otherwise it will break during the deserialization
        // of the data came from Everett
        internal CodePageDataItem dataItem = null;

        [NonSerialized]
        internal bool m_deserializedFromEverett = false;

        // Because of encoders we may be read only
        [OptionalField(VersionAdded = 2)]
        private bool m_isReadOnly = true;

        // Encoding (encoder) fallback
        [OptionalField(VersionAdded = 2)]
        internal EncoderFallback encoderFallback = null;
        [OptionalField(VersionAdded = 2)]
        internal DecoderFallback decoderFallback = null;

        protected Encoding() : this(0)
        {
        }


        protected Encoding(int codePage)
        {
            // Validate code page
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException("codePage");
            }
            Contract.EndContractBlock();

            // Remember code page
            m_codePage = codePage;

            // Use default encoder/decoder fallbacks
            this.SetDefaultFallbacks();
        }

        // This constructor is needed to allow any sub-classing implementation to provide encoder/decoder fallback objects 
        // because the encoding object is always created as read-only object and don't allow setting encoder/decoder fallback 
        // after the creation is done. 
        protected Encoding(int codePage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            // Validate code page
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException("codePage");
            }
            Contract.EndContractBlock();

            // Remember code page
            m_codePage = codePage;

            this.encoderFallback = encoderFallback ?? new InternalEncoderBestFitFallback(this);
            this.decoderFallback = decoderFallback ?? new InternalDecoderBestFitFallback(this);
        }

        // Default fallback that we'll use.
        internal virtual void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an "\xFFFD" string,
            // For ASCII we use "?" replacement fallback, etc.
            this.encoderFallback = new InternalEncoderBestFitFallback(this);
            this.decoderFallback = new InternalDecoderBestFitFallback(this);
        }


#region Serialization
        internal void OnDeserializing()
        {
            // intialize the optional Whidbey fields
            encoderFallback = null;
            decoderFallback = null;
            m_isReadOnly    = true;
        }

        internal void OnDeserialized()
        {
            if (encoderFallback == null || decoderFallback == null)
            {
                m_deserializedFromEverett = true;
                SetDefaultFallbacks();
            }

            // dataItem is always recalculated from the code page #
            dataItem = null;              
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            OnDeserializing();
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            // to be consistent with SerializeEncoding
            dataItem = null;
        }

        // the following two methods are used for the inherited classes which implemented ISerializable
        // Deserialization Helper
        internal void DeserializeEncoding(SerializationInfo info, StreamingContext context)
        {
            // Any info?
            if (info==null) throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            // All versions have a code page
            this.m_codePage = (int)info.GetValue("m_codePage", typeof(int));

            // We can get dataItem on the fly if needed, and the index is different between versions
            // so ignore whatever dataItem data we get from Everett.
            this.dataItem   = null;

            // See if we have a code page
            try
            {
                //
                // Try Whidbey V2.0 Fields
                //

                this.m_isReadOnly = (bool)info.GetValue("m_isReadOnly", typeof(bool));

                this.encoderFallback = (EncoderFallback)info.GetValue("encoderFallback", typeof(EncoderFallback));
                this.decoderFallback = (DecoderFallback)info.GetValue("decoderFallback", typeof(DecoderFallback));
            }
            catch (SerializationException)
            {
                //
                // Didn't have Whidbey things, must be Everett
                //
                this.m_deserializedFromEverett = true;

                // May as well be read only
                this.m_isReadOnly = true;
                SetDefaultFallbacks();
            }
        }

        // Serialization Helper
        internal void SerializeEncoding(SerializationInfo info, StreamingContext context)
        {
            // Any Info?
            if (info==null) throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            // These are new V2.0 Whidbey stuff
            info.AddValue("m_isReadOnly", this.m_isReadOnly);
            info.AddValue("encoderFallback", this.EncoderFallback);
            info.AddValue("decoderFallback", this.DecoderFallback);

            // These were in Everett V1.1 as well
            info.AddValue("m_codePage", this.m_codePage);

            // This was unique to Everett V1.1
            info.AddValue("dataItem", null);

            // Everett duplicated these fields, so these are needed for portability
            info.AddValue("Encoding+m_codePage", this.m_codePage);
            info.AddValue("Encoding+dataItem", null);
        }

#endregion Serialization

        // Converts a byte array from one encoding to another. The bytes in the
        // bytes array are converted from srcEncoding to
        // dstEncoding, and the returned value is a new byte array
        // containing the result of the conversion.
        //
        [Pure]
        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding,
            byte[] bytes) {
            if (bytes==null)
                throw new ArgumentNullException("bytes");
            Contract.Ensures(Contract.Result<byte[]>() != null);
            
            return Convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length);
        }

        // Converts a range of bytes in a byte array from one encoding to another.
        // This method converts count bytes from bytes starting at
        // index index from srcEncoding to dstEncoding, and
        // returns a new byte array containing the result of the conversion.
        //
        [Pure]
        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding,
            byte[] bytes, int index, int count) {
            if (srcEncoding == null || dstEncoding == null) {
                throw new ArgumentNullException((srcEncoding == null ? "srcEncoding" : "dstEncoding"),
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (bytes == null) {
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            Contract.Ensures(Contract.Result<byte[]>() != null);
            
            return dstEncoding.GetBytes(srcEncoding.GetChars(bytes, index, count));
        }

#if FEATURE_CODEPAGES_FILE
        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        // On Desktop, encoding instances that aren't cached in a static field are cached in
        // a hash table by codepage.
        private static volatile Hashtable encodings;
#endif

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]
#endif
        public static void RegisterProvider(EncodingProvider provider) 
        {
            // Parameters validated inside EncodingProvider
            EncodingProvider.AddProvider(provider);
        }

        [Pure]
#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
        public static Encoding GetEncoding(int codepage)
        {
            Encoding result = EncodingProvider.GetEncodingFromProvider(codepage);
            if (result != null)
                return result;

            //
            // NOTE: If you add a new encoding that can be get by codepage, be sure to
            // add the corresponding item in EncodingTable.
            // Otherwise, the code below will throw exception when trying to call
            // EncodingTable.GetDataItem().
            //
            if (codepage < 0 || codepage > 65535) {
                throw new ArgumentOutOfRangeException(
                    "codepage", Environment.GetResourceString("ArgumentOutOfRange_Range",
                        0, 65535));
            }

            Contract.EndContractBlock();

            // Our Encoding

            // See if the encoding is cached in a static field.
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
                    throw new ArgumentException(Environment.GetResourceString(
                        "Argument_CodepageNotSupported", codepage), "codepage");
            }

#if FEATURE_CODEPAGES_FILE
            object key = codepage; // Box once

            // See if we have a hash table with our encoding in it already.
            if (encodings != null) {
                result = (Encoding)encodings[key];
            }

            if (result == null)
            {
                // Don't conflict with ourselves
                lock (InternalSyncObject)
                {
                    // Need a new hash table
                    // in case another thread beat us to creating the Dictionary
                    if (encodings == null) {
                        encodings = new Hashtable();
                    }

                    // Double check that we don't have one in the table (in case another thread beat us here)
                    if ((result = (Encoding)encodings[key]) != null)
                        return result;

                    if (codepage == CodePageWindows1252)
                    {
                        result = new SBCSCodePageEncoding(codepage);
                    }
                    else
                    {
                        result = GetEncodingCodePage(codepage) ?? GetEncodingRare(codepage);
                    }

                    Contract.Assert(result != null, "result != null");

                    encodings.Add(key, result);
                }
            }
            return result;
#else
            // Is it a valid code page?
            if (EncodingTable.GetCodePageDataItem(codepage) == null)
            {
                throw new NotSupportedException(
                    Environment.GetResourceString("NotSupported_NoCodepageData", codepage));
            }

            return UTF8;
#endif // FEATURE_CODEPAGES_FILE
        }

        [Pure]
        public static Encoding GetEncoding(int codepage,
            EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(codepage, encoderFallback, decoderFallback);

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
#if FEATURE_CODEPAGES_FILE
        [System.Security.SecurityCritical]  // auto-generated
        private static Encoding GetEncodingRare(int codepage)
        {
            Contract.Assert(codepage != 0 && codepage != 1200 && codepage != 1201 && codepage != 65001,
                "[Encoding.GetEncodingRare]This code page (" + codepage + ") isn't supported by GetEncodingRare!");
            Encoding result;
            switch (codepage)
            {
                case ISCIIAssemese:
                case ISCIIBengali:
                case ISCIIDevanagari:
                case ISCIIGujarathi:
                case ISCIIKannada:
                case ISCIIMalayalam:
                case ISCIIOriya:
                case ISCIIPanjabi:
                case ISCIITamil:
                case ISCIITelugu:
                    result = new ISCIIEncoding(codepage);
                    break;
                // GB2312-80 uses same code page for 20936 and mac 10008
                case CodePageMacGB2312:
          //     case CodePageGB2312:
          //        result = new DBCSCodePageEncoding(codepage, EUCCN);
                    result = new DBCSCodePageEncoding(CodePageMacGB2312, CodePageGB2312);
                    break;

                // Mac Korean 10003 and 20949 are the same
                case CodePageMacKorean:
                    result = new DBCSCodePageEncoding(CodePageMacKorean, CodePageDLLKorean);
                    break;
                // GB18030 Code Pages
                case GB18030:
                    result = new GB18030Encoding();
                    break;
                // ISO2022 Code Pages
                case ISOKorean:
            //    case ISOSimplifiedCN
                case ChineseHZ:
                case ISO2022JP:         // JIS JP, full-width Katakana mode (no half-width Katakana)
                case ISO2022JPESC:      // JIS JP, esc sequence to do Katakana.
                case ISO2022JPSISO:     // JIS JP with Shift In/ Shift Out Katakana support
                    result = new ISO2022Encoding(codepage);
                    break;
                // Duplicate EUC-CN (51936) just calls a base code page 936,
                // so does ISOSimplifiedCN (50227), which's gotta be broken
                case DuplicateEUCCN:
                case ISOSimplifiedCN:
                    result = new DBCSCodePageEncoding(codepage, EUCCN);    // Just maps to 936
                    break;
                case EUCJP:
                    result = new EUCJPEncoding();
                    break;
                case EUCKR:
                    result = new DBCSCodePageEncoding(codepage, CodePageDLLKorean);    // Maps to 20949
                    break;
                case ENC50229:
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_CodePage50229"));
                case ISO_8859_8I:
                    result = new SBCSCodePageEncoding(codepage, ISO_8859_8_Visual);        // Hebrew maps to a different code page
                    break;
                default:
                    // Not found, already tried codepage table code pages in GetEncoding()
                    throw new NotSupportedException(
                        Environment.GetResourceString("NotSupported_NoCodepageData", codepage));
            }
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static Encoding GetEncodingCodePage(int CodePage)
        {
            // Single Byte or Double Byte Code Page? (0 if not found)
            int i = BaseCodePageEncoding.GetCodePageByteSize(CodePage);
            if (i == 1) return new SBCSCodePageEncoding(CodePage);
            else if (i == 2) return new DBCSCodePageEncoding(CodePage);

            // Return null if we didn't find one.
            return null;
        }
#endif // FEATURE_CODEPAGES_FILE
        // Returns an Encoding object for a given name or a given code page value.
        //
        [Pure]
        public static Encoding GetEncoding(String name)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(name);
            if (baseEncoding != null)
                return baseEncoding;

            //
            // NOTE: If you add a new encoding that can be requested by name, be sure to
            // add the corresponding item in EncodingTable.
            // Otherwise, the code below will throw exception when trying to call
            // EncodingTable.GetCodePageFromName().
            //
            return (GetEncoding(EncodingTable.GetCodePageFromName(name)));
        }

        // Returns an Encoding object for a given name or a given code page value.
        //
        [Pure]
        public static Encoding GetEncoding(String name,
            EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(name, encoderFallback, decoderFallback);
            if (baseEncoding != null)
                return baseEncoding;

            //
            // NOTE: If you add a new encoding that can be requested by name, be sure to
            // add the corresponding item in EncodingTable.
            // Otherwise, the code below will throw exception when trying to call
            // EncodingTable.GetCodePageFromName().
            //
            return (GetEncoding(EncodingTable.GetCodePageFromName(name), encoderFallback, decoderFallback));
        }

        // Return a list of all EncodingInfo objects describing all of our encodings
        [Pure]
        public static EncodingInfo[] GetEncodings()
        {
            return EncodingTable.GetEncodings();
        }

        [Pure]
        public virtual byte[] GetPreamble()
        {
            return EmptyArray<Byte>.Value;
        }

        private void GetDataItem() {
            if (dataItem==null) {
                dataItem = EncodingTable.GetCodePageDataItem(m_codePage);
                if(dataItem==null) {
                    throw new NotSupportedException(
                        Environment.GetResourceString("NotSupported_NoCodepageData", m_codePage));
                }
            }
        }

        // Returns the name for this encoding that can be used with mail agent body tags.
        // If the encoding may not be used, the string is empty.

        public virtual String BodyName
        {
            get
            {
                if (dataItem==null) {
                    GetDataItem();
                }
                return (dataItem.BodyName);
            }
        }

        // Returns the human-readable description of the encoding ( e.g. Hebrew (DOS)).

        public virtual String EncodingName
        {
            get
            {
                return Environment.GetResourceString("Globalization.cp_" + m_codePage.ToString());
            }
        }

        // Returns the name for this encoding that can be used with mail agent header
        // tags.  If the encoding may not be used, the string is empty.

        public virtual String HeaderName
        {
            get
            {
                if (dataItem==null) {
                    GetDataItem();
                }
                return (dataItem.HeaderName);
            }
        }

        // Returns the array of IANA-registered names for this encoding.  If there is an
        // IANA preferred name, it is the first name in the array.

        public virtual String WebName
        {
            get
            {
                if (dataItem==null) {
                    GetDataItem();
                }
                return (dataItem.WebName);
            }
        }

        // Returns the windows code page that most closely corresponds to this encoding.

        public virtual int WindowsCodePage
        {
            get
            {
                if (dataItem==null) {
                    GetDataItem();
                }
                return (dataItem.UIFamilyCodePage);
            }
        }


        // True if and only if the encoding is used for display by browsers clients.

        public virtual bool IsBrowserDisplay {
            get {
                if (dataItem==null) {
                    GetDataItem();
                }
                return ((dataItem.Flags & MIMECONTF_BROWSER) != 0);
            }
        }

        // True if and only if the encoding is used for saving by browsers clients.

        public virtual bool IsBrowserSave {
            get {
                if (dataItem==null) {
                    GetDataItem();
                }
                return ((dataItem.Flags & MIMECONTF_SAVABLE_BROWSER) != 0);
            }
        }

        // True if and only if the encoding is used for display by mail and news clients.

        public virtual bool IsMailNewsDisplay {
            get {
                if (dataItem==null) {
                    GetDataItem();
                }
                return ((dataItem.Flags & MIMECONTF_MAILNEWS) != 0);
            }
        }


        // True if and only if the encoding is used for saving documents by mail and
        // news clients

        public virtual bool IsMailNewsSave {
            get {
                if (dataItem==null) {
                    GetDataItem();
                }
                return ((dataItem.Flags & MIMECONTF_SAVABLE_MAILNEWS) != 0);
            }
        }

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)

        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual bool IsSingleByte
        {
            get
            {
                return false;
            }
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public EncoderFallback EncoderFallback
        {
            get
            {
                return encoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));

                if (value == null)
                    throw new ArgumentNullException("value");
                Contract.EndContractBlock();

                encoderFallback = value;
            }
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public DecoderFallback DecoderFallback
        {
            get
            {
                return decoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));

                if (value == null)
                    throw new ArgumentNullException("value");
                Contract.EndContractBlock();

                decoderFallback = value;
            }
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual Object Clone()
        {
            Encoding newEncoding = (Encoding)this.MemberwiseClone();

            // New one should be readable
            newEncoding.m_isReadOnly = false;
            return newEncoding;
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
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
        [Pure]
        public virtual int GetByteCount(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars",
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            Contract.EndContractBlock();

            return GetByteCount(chars, 0, chars.Length);
        }

        [Pure]
        public virtual int GetByteCount(String s)
        {
            if (s==null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();

            char[] chars = s.ToCharArray();
            return GetByteCount(chars, 0, chars.Length);

        }

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        [Pure]
        public abstract int GetByteCount(char[] chars, int index, int count);

        // We expect this to be the workhorse for NLS encodings
        // unfortunately for existing overrides, it has to call the [] version,
        // which is really slow, so this method should be avoided if you're calling
        // a 3rd party encoding.
        [Pure]
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual unsafe int GetByteCount(char* chars, int count)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException("chars",
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            char[] arrChar = new char[count];
            int index;

            for (index = 0; index < count; index++)
                arrChar[index] = chars[index];

            return GetByteCount(arrChar, 0, count);
        }

        // For NLS Encodings, workhorse takes an encoder (may be null)
        // Always validate parameters before calling internal version, which will only assert.
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual unsafe int GetByteCount(char* chars, int count, EncoderNLS encoder)
        {
            Contract.Requires(chars != null);
            Contract.Requires(count >= 0);

            return GetByteCount(chars, count);
        }

        // Returns a byte array containing the encoded representation of the given
        // character array.
        //
        [Pure]
        public virtual byte[] GetBytes(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars",
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            Contract.EndContractBlock();
            return GetBytes(chars, 0, chars.Length);
        }

        // Returns a byte array containing the encoded representation of a range
        // of characters in a character array.
        //
        [Pure]
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
        [Pure]
        public virtual byte[] GetBytes(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s",
                    Environment.GetResourceString("ArgumentNull_String"));
            Contract.EndContractBlock();

            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];
            int bytesReceived = GetBytes(s, 0, s.Length, bytes, 0);
            Contract.Assert(byteCount == bytesReceived);
            return bytes;
        }

        public virtual int GetBytes(String s, int charIndex, int charCount,
                                       byte[] bytes, int byteIndex)
        {
            if (s==null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        // This is our internal workhorse
        // Always validate parameters before calling internal version, which will only assert.
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            return GetBytes(chars, charCount, bytes, byteCount);
        }

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implimentation)
        //
        // WARNING WARNING WARNING
        //
        // WARNING: If this breaks it could be a security threat.  Obviously we
        // call this internally, so you need to make sure that your pointers, counts
        // and indexes are correct when you call this method.
        //
        // In addition, we have internal code, which will be marked as "safe" calling
        // this code.  However this code is dependent upon the implimentation of an
        // external GetBytes() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the byte[] to our byte* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow byteCount either.

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual unsafe int GetBytes(char* chars, int charCount,
                                              byte* bytes, int byteCount)
        {
            // Validate input parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount<0 ? "charCount" : "byteCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Get the char array to convert
            char[] arrChar = new char[charCount];

            int index;
            for (index = 0; index < charCount; index++)
                arrChar[index] = chars[index];

            // Get the byte array to fill
            byte[] arrByte = new byte[byteCount];

            // Do the work
            int result = GetBytes(arrChar, 0, charCount, arrByte, 0);

            Contract.Assert(result <= byteCount, "[Encoding.GetBytes]Returned more bytes than we have space for");

            // Copy the byte array
            // WARNING: We MUST make sure that we don't copy too many bytes.  We can't
            // rely on result because it could be a 3rd party implimentation.  We need
            // to make sure we never copy more than byteCount bytes no matter the value
            // of result
            if (result < byteCount)
                byteCount = result;

            // Copy the data, don't overrun our array!
            for (index = 0; index < byteCount; index++)
                bytes[index] = arrByte[index];

            return byteCount;
        }

        // Returns the number of characters produced by decoding the given byte
        // array.
        //
        [Pure]
        public virtual int GetCharCount(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            Contract.EndContractBlock();
            return GetCharCount(bytes, 0, bytes.Length);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        [Pure]
        public abstract int GetCharCount(byte[] bytes, int index, int count);

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implimentation)
        [Pure]
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate input parameters
            if (bytes == null)
                throw new ArgumentNullException("bytes",
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            byte[] arrbyte = new byte[count];
            int index;

            for (index = 0; index < count; index++)
                arrbyte[index] = bytes[index];

            return GetCharCount(arrbyte, 0, count);
        }

        // This is our internal workhorse
        // Always validate parameters before calling internal version, which will only assert.
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual unsafe int GetCharCount(byte* bytes, int count, DecoderNLS decoder)
        {
            return GetCharCount(bytes, count);
        }

        // Returns a character array containing the decoded representation of a
        // given byte array.
        //
        [Pure]
        public virtual char[] GetChars(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));
            }
            Contract.EndContractBlock();
            return GetChars(bytes, 0, bytes.Length);
        }

        // Returns a character array containing the decoded representation of a
        // range of bytes in a byte array.
        //
        [Pure]
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
        // determine the maximum number of characterss that will be produced for a
        // given number of bytes, regardless of the actual byte values.
        //

        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                       char[] chars, int charIndex);


        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implimentation)
        //
        // WARNING WARNING WARNING
        //
        // WARNING: If this breaks it could be a security threat.  Obviously we
        // call this internally, so you need to make sure that your pointers, counts
        // and indexes are correct when you call this method.
        //
        // In addition, we have internal code, which will be marked as "safe" calling
        // this code.  However this code is dependent upon the implimentation of an
        // external GetChars() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the char[] to our char* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow charCount either.

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual unsafe int GetChars(byte* bytes, int byteCount,
                                              char* chars, int charCount)
        {
            // Validate input parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? "chars" : "bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount<0 ? "byteCount" : "charCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Get the byte array to convert
            byte[] arrByte = new byte[byteCount];

            int index;
            for (index = 0; index < byteCount; index++)
                arrByte[index] = bytes[index];

            // Get the char array to fill
            char[] arrChar = new char[charCount];

            // Do the work
            int result = GetChars(arrByte, 0, byteCount, arrChar, 0);

            Contract.Assert(result <= charCount, "[Encoding.GetChars]Returned more chars than we have space for");

            // Copy the char array
            // WARNING: We MUST make sure that we don't copy too many chars.  We can't
            // rely on result because it could be a 3rd party implimentation.  We need
            // to make sure we never copy more than charCount chars no matter the value
            // of result
            if (result < charCount)
                charCount = result;

            // Copy the data, don't overrun our array!
            for (index = 0; index < charCount; index++)
                chars[index] = arrChar[index];

            return charCount;
        }


        // This is our internal workhorse
        // Always validate parameters before calling internal version, which will only assert.
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS decoder)
        {
            return GetChars(bytes, byteCount, chars, charCount);
        }


        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public unsafe string GetString(byte* bytes, int byteCount)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));

            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            return String.CreateStringFromEncoding(bytes, byteCount, this);
        }

        // Returns the code page identifier of this encoding. The returned value is
        // an integer between 0 and 65535 if the encoding has a code page
        // identifier, or -1 if the encoding does not represent a code page.
        //

        public virtual int CodePage
        {
            get
            {
                return m_codePage;
            }
        }

        // IsAlwaysNormalized
        // Returns true if the encoding is always normalized for the specified encoding form
        [Pure]
        [System.Runtime.InteropServices.ComVisible(false)]
        public bool IsAlwaysNormalized()
        {
#if !FEATURE_NORM_IDNA_ONLY        
            return this.IsAlwaysNormalized(NormalizationForm.FormC);
#else
            return this.IsAlwaysNormalized((NormalizationForm)ExtendedNormalizationForms.FormIdna);
#endif
        }

        [Pure]
        [System.Runtime.InteropServices.ComVisible(false)]
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

        [System.Security.SecuritySafeCritical]
        private static Encoding CreateDefaultEncoding()
        {
            // defaultEncoding should be null if we get here, but we can't
            // assert that in case another thread beat us to the initialization

            Encoding enc;

#if FEATURE_CODEPAGES_FILE            
            int codePage = Win32Native.GetACP();

            // For US English, we can save some startup working set by not calling
            // GetEncoding(int codePage) since JITting GetEncoding will force us to load
            // all the Encoding classes for ASCII, UTF7 & UTF8, & UnicodeEncoding.

            if (codePage == 1252)
                enc = new SBCSCodePageEncoding(codePage);
            else
                enc = GetEncoding(codePage);
#else // FEATURE_CODEPAGES_FILE            

            // For silverlight we use UTF8 since ANSI isn't available
            enc = UTF8;

#endif // FEATURE_CODEPAGES_FILE

            // This method should only ever return one Encoding instance
            return Interlocked.CompareExchange(ref defaultEncoding, enc, null) ?? enc;
        }

        // Returns an encoding for the system's current ANSI code page.

        public static Encoding Default => defaultEncoding ?? CreateDefaultEncoding();

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
        [Pure]
        public abstract int GetMaxByteCount(int charCount);

        // Returns the maximum number of characters produced by decoding a given
        // number of bytes. This method can be used to determine an appropriate
        // buffer size for character arrays passed to the GetChars method of
        // this encoding or the GetChars method of a Decoder for this
        // encoding. All encodings must guarantee that no buffer overflow
        // exceptions will occur if buffers are sized according to the results of
        // this method.
        //
        [Pure]
        public abstract int GetMaxCharCount(int byteCount);

        // Returns a string containing the decoded representation of a given byte
        // array.
        //
        [Pure]
        public virtual String GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));
            Contract.EndContractBlock();

            return GetString(bytes, 0, bytes.Length);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // Internally we override this for performance
        //
        [Pure]
        public virtual String GetString(byte[] bytes, int index, int count)
        {
            return new String(GetChars(bytes, index, count));
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

        public override bool Equals(Object value) {
            Encoding that = value as Encoding;
            if (that != null)
                return (m_codePage == that.m_codePage) &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            return (false);
        }


        public override int GetHashCode() {
            return m_codePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

        internal virtual char[] GetBestFitUnicodeToBytesData()
        {
            // Normally we don't have any best fit data.
            return EmptyArray<Char>.Value;
        }

        internal virtual char[] GetBestFitBytesToUnicodeData()
        {
            // Normally we don't have any best fit data.
            return EmptyArray<Char>.Value;
        }

        internal void ThrowBytesOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implimented an encoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(
                Environment.GetResourceString("Argument_EncodingConversionOverflowBytes",
                EncodingName, EncoderFallback.GetType()), "bytes");
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void ThrowBytesOverflow(EncoderNLS encoder, bool nothingEncoded)
        {
            if (encoder == null || encoder.m_throwOnOverflow || nothingEncoded)
            {
                if (encoder != null && encoder.InternalHasFallbackBuffer)
                    encoder.FallbackBuffer.InternalReset();
                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implimented an encoder fallback with a broken GetMaxCharCount
                ThrowBytesOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            encoder.ClearMustFlush();
        }

        internal void ThrowCharsOverflow()
        {
            // Special message to include fallback type in case fallback's GetMaxCharCount is broken
            // This happens if user has implimented a decoder fallback with a broken GetMaxCharCount
            throw new ArgumentException(
                Environment.GetResourceString("Argument_EncodingConversionOverflowChars",
                EncodingName, DecoderFallback.GetType()), "chars");
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void ThrowCharsOverflow(DecoderNLS decoder, bool nothingDecoded)
        {
            if (decoder == null || decoder.m_throwOnOverflow || nothingDecoded)
            {
                if (decoder != null && decoder.InternalHasFallbackBuffer)
                    decoder.FallbackBuffer.InternalReset();

                // Special message to include fallback type in case fallback's GetMaxCharCount is broken
                // This happens if user has implimented a decoder fallback with a broken GetMaxCharCount
                ThrowCharsOverflow();
            }

            // If we didn't throw, we are in convert and have to remember our flushing
            decoder.ClearMustFlush();
        }

        [Serializable]
        internal class DefaultEncoder : Encoder, IObjectReference, ISerializable
        {
            private Encoding m_encoding;
            [NonSerialized] private bool m_hasInitializedEncoding;

            [NonSerialized] internal char charLeftOver;

            public DefaultEncoder(Encoding encoding)
            {
                m_encoding = encoding;
                m_hasInitializedEncoding = true;
            }

            // Constructor called by serialization, have to handle deserializing from Everett
            internal DefaultEncoder(SerializationInfo info, StreamingContext context)
            {
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // All we have is our encoding
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof(Encoding));

                try 
                {
                    this.m_fallback     = (EncoderFallback) info.GetValue("m_fallback",   typeof(EncoderFallback));
                    this.charLeftOver   = (Char)            info.GetValue("charLeftOver", typeof(Char));
                }
                catch (SerializationException)
                {
                }
            }

            // Just get it from GetEncoding
            [System.Security.SecurityCritical]  // auto-generated
            public Object GetRealObject(StreamingContext context)
            {
                // upon deserialization since the DefaultEncoder implement IObjectReference the 
                // serialization code tries to do the fixup. The fixup returns another 
                // IObjectReference (the DefaultEncoder) class and hence so on and on. 
                // Finally the deserialization logics fails after following maximum references
                // unless we short circuit with the following
                if (m_hasInitializedEncoding)
                {
                    return this;
                }

                Encoder encoder = m_encoding.GetEncoder();
                if (m_fallback != null)
                    encoder.m_fallback = m_fallback;
                if (charLeftOver != (char) 0)
                {
                    EncoderNLS encoderNls = encoder as EncoderNLS;
                    if (encoderNls != null)
                        encoderNls.charLeftOver = charLeftOver;
                }
                return encoder;
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // All we have is our encoding
                info.AddValue("encoding", this.m_encoding);
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
                return m_encoding.GetByteCount(chars, index, count);
            }

            [System.Security.SecurityCritical]  // auto-generated
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public unsafe override int GetByteCount(char* chars, int count, bool flush)
            {
                return m_encoding.GetByteCount(chars, count);
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
                return m_encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }

            [System.Security.SecurityCritical]  // auto-generated
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public unsafe override int GetBytes(char* chars, int charCount,
                                                 byte* bytes, int byteCount, bool flush)
            {
                return m_encoding.GetBytes(chars, charCount, bytes, byteCount);
            }
        }

        [Serializable]
        internal class DefaultDecoder : Decoder, IObjectReference, ISerializable
        {
            private Encoding m_encoding;
            [NonSerialized]
            private bool m_hasInitializedEncoding;

            public DefaultDecoder(Encoding encoding)
            {
                m_encoding = encoding;
                m_hasInitializedEncoding = true;
           }

            // Constructor called by serialization, have to handle deserializing from Everett
            internal DefaultDecoder(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // All we have is our encoding
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof(Encoding));
                
                try 
                {
                    this.m_fallback = (DecoderFallback) info.GetValue("m_fallback", typeof(DecoderFallback));
                }
                catch (SerializationException)
                {
                    m_fallback = null;
                }
            }

            // Just get it from GetEncoding
            [System.Security.SecurityCritical]  // auto-generated
            public Object GetRealObject(StreamingContext context)
            {
                // upon deserialization since the DefaultEncoder implement IObjectReference the 
                // serialization code tries to do the fixup. The fixup returns another 
                // IObjectReference (the DefaultEncoder) class and hence so on and on. 
                // Finally the deserialization logics fails after following maximum references
                // unless we short circuit with the following
                if (m_hasInitializedEncoding)
                {
                    return this;
                }

                Decoder decoder = m_encoding.GetDecoder();
                if (m_fallback != null)
                    decoder.m_fallback = m_fallback;

                return decoder;
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // All we have is our encoding
                info.AddValue("encoding", this.m_encoding);
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
                return m_encoding.GetCharCount(bytes, index, count);
            }

            [System.Security.SecurityCritical]  // auto-generated
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
            {
                // By default just call the encoding version, no flush by default
                return m_encoding.GetCharCount(bytes, count);
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
                return m_encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }

            [System.Security.SecurityCritical]  // auto-generated
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public unsafe override int GetChars(byte* bytes, int byteCount,
                                                  char* chars, int charCount, bool flush)
            {
                // By default just call the encoding's version
                return m_encoding.GetChars(bytes, byteCount, chars, charCount);
            }
        }

        internal class EncodingCharBuffer
        {
            [SecurityCritical]
            unsafe char* chars;
            [SecurityCritical]
            unsafe char* charStart;
            [SecurityCritical]
            unsafe char* charEnd;
            int          charCountResult = 0;
            Encoding     enc;
            DecoderNLS   decoder;
            [SecurityCritical]
            unsafe byte* byteStart;
            [SecurityCritical]
            unsafe byte* byteEnd;
            [SecurityCritical]
            unsafe byte* bytes;
            DecoderFallbackBuffer fallbackBuffer;

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe EncodingCharBuffer(Encoding enc, DecoderNLS decoder, char* charStart, int charCount,
                                                    byte* byteStart, int byteCount)
            {
                this.enc = enc;
                this.decoder = decoder;

                this.chars = charStart;
                this.charStart = charStart;
                this.charEnd = charStart + charCount;

                this.byteStart = byteStart;
                this.bytes = byteStart;
                this.byteEnd = byteStart + byteCount;

                if (this.decoder == null)
                    this.fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
                else
                    this.fallbackBuffer = this.decoder.FallbackBuffer;

                // If we're getting chars or getting char count we don't expect to have
                // to remember fallbacks between calls (so it should be empty)
                Contract.Assert(fallbackBuffer.Remaining == 0,
                    "[Encoding.EncodingCharBuffer.EncodingCharBuffer]Expected empty fallback buffer for getchars/charcount");
                fallbackBuffer.InternalInitialize(bytes, charEnd);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddChar(char ch, int numBytes)
            {
                if (chars != null)
                {
                    if (chars >= charEnd)
                    {
                        // Throw maybe
                        bytes-=numBytes;                                        // Didn't encode these bytes
                        enc.ThrowCharsOverflow(decoder, bytes <= byteStart);    // Throw?
                        return false;                                           // No throw, but no store either
                    }

                    *(chars++) = ch;
                }
                charCountResult++;
                return true;
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddChar(char ch)
            {
                return AddChar(ch,1);
            }


            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddChar(char ch1, char ch2, int numBytes)
            {
                // Need room for 2 chars
                if (chars >= charEnd - 1)
                {
                    // Throw maybe
                    bytes-=numBytes;                                        // Didn't encode these bytes
                    enc.ThrowCharsOverflow(decoder, bytes <= byteStart);    // Throw?
                    return false;                                           // No throw, but no store either
                }
                return AddChar(ch1, numBytes) && AddChar(ch2, numBytes);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe void AdjustBytes(int count)
            {
                bytes += count;
            }

            internal unsafe bool MoreData
            {
                [System.Security.SecurityCritical]  // auto-generated
                get
                {
                    return bytes < byteEnd;
                }
            }

            // Do we have count more bytes?
            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool EvenMoreData(int count)
            {
                return (bytes <= byteEnd - count);
            }

            // GetNextByte shouldn't be called unless the caller's already checked more data or even more data,
            // but we'll double check just to make sure.
            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe byte GetNextByte()
            {
                Contract.Assert(bytes < byteEnd, "[EncodingCharBuffer.GetNextByte]Expected more date");
                if (bytes >= byteEnd)
                    return 0;
                return *(bytes++);
            }

            internal unsafe int BytesUsed
            {
                [System.Security.SecurityCritical]  // auto-generated
                get
                {
                    return (int)(bytes - byteStart);
                }
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool Fallback(byte fallbackByte)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { fallbackByte };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool Fallback(byte byte1, byte byte2)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { byte1, byte2 };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool Fallback(byte byte1, byte byte2, byte byte3, byte byte4)
            {
                // Build our buffer
                byte[] byteBuffer = new byte[] { byte1, byte2, byte3, byte4 };

                // Do the fallback and add the data.
                return Fallback(byteBuffer);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool Fallback(byte[] byteBuffer)
            {
                // Do the fallback and add the data.
                if (chars != null)
                {
                    char* pTemp = chars;
                    if (fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars) == false)
                    {
                        // Throw maybe
                        bytes -= byteBuffer.Length;                             // Didn't use how many ever bytes we're falling back
                        fallbackBuffer.InternalReset();                         // We didn't use this fallback.
                        enc.ThrowCharsOverflow(decoder, chars == charStart);    // Throw?
                        return false;                                           // No throw, but no store either
                    }
                    charCountResult += unchecked((int)(chars - pTemp));
                }
                else
                {
                    charCountResult += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }

                return true;
            }

            internal unsafe int Count
            {
                get
                {
                    return charCountResult;
                }
            }
        }

        internal class EncodingByteBuffer
        {
            [SecurityCritical]
            unsafe byte* bytes;
            [SecurityCritical]
            unsafe byte* byteStart;
            [SecurityCritical]
            unsafe byte* byteEnd;
            [SecurityCritical]
            unsafe char* chars;
            [SecurityCritical]
            unsafe char* charStart;
            [SecurityCritical]
            unsafe char* charEnd;
            int          byteCountResult = 0;
            Encoding     enc;
            EncoderNLS   encoder;
            internal EncoderFallbackBuffer fallbackBuffer;

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe EncodingByteBuffer(Encoding inEncoding, EncoderNLS inEncoder,
                        byte* inByteStart, int inByteCount, char* inCharStart, int inCharCount)
            {
                this.enc = inEncoding;
                this.encoder = inEncoder;

                this.charStart = inCharStart;
                this.chars = inCharStart;
                this.charEnd = inCharStart + inCharCount;

                this.bytes = inByteStart;
                this.byteStart = inByteStart;
                this.byteEnd = inByteStart + inByteCount;

                if (this.encoder == null)
                    this.fallbackBuffer = enc.EncoderFallback.CreateFallbackBuffer();
                else
                {
                    this.fallbackBuffer = this.encoder.FallbackBuffer;
                    // If we're not converting we must not have data in our fallback buffer
                    if (encoder.m_throwOnOverflow && encoder.InternalHasFallbackBuffer &&
                        this.fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                            encoder.Encoding.EncodingName, encoder.Fallback.GetType()));
                }
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, bytes != null);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b, int moreBytesExpected)
            {
                Contract.Assert(moreBytesExpected >= 0, "[EncodingByteBuffer.AddByte]expected non-negative moreBytesExpected");
                if (bytes != null)
                {
                    if (bytes >= byteEnd - moreBytesExpected)
                    {
                        // Throw maybe.  Check which buffer to back up (only matters if Converting)
                        this.MovePrevious(true);            // Throw if necessary
                        return false;                       // No throw, but no store either
                    }

                    *(bytes++) = b;
                }
                byteCountResult++;
                return true;
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1)
            {
                return (AddByte(b1, 0));
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1, byte b2)
            {
                return (AddByte(b1, b2, 0));
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1, byte b2, int moreBytesExpected)
            {
                return (AddByte(b1, 1 + moreBytesExpected) && AddByte(b2, moreBytesExpected));
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1, byte b2, byte b3)
            {
                return AddByte(b1, b2, b3, (int)0);
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1, byte b2, byte b3, int moreBytesExpected)
            {
                return (AddByte(b1, 2 + moreBytesExpected) &&
                        AddByte(b2, 1 + moreBytesExpected) &&
                        AddByte(b3, moreBytesExpected));
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool AddByte(byte b1, byte b2, byte b3, byte b4)
            {
                return (AddByte(b1, 3) &&
                        AddByte(b2, 2) &&
                        AddByte(b3, 1) &&
                        AddByte(b4, 0));
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe void MovePrevious(bool bThrow)
            {
                if (fallbackBuffer.bFallingBack)
                    fallbackBuffer.MovePrevious();                      // don't use last fallback
                else
                {
                    Contract.Assert(chars > charStart || 
                        ((bThrow == true) && (bytes == byteStart)), 
                        "[EncodingByteBuffer.MovePrevious]expected previous data or throw");
                    if (chars > charStart)
                        chars--;                                        // don't use last char
                }

                if (bThrow)
                    enc.ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw? (and reset fallback if not converting)
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe bool Fallback(char charFallback)
            {
                // Do the fallback
                return fallbackBuffer.InternalFallback(charFallback, ref chars);
            }

            internal unsafe bool MoreData
            {
                [System.Security.SecurityCritical]  // auto-generated
                get
                {
                    // See if fallbackBuffer is not empty or if there's data left in chars buffer.
                    return ((fallbackBuffer.Remaining > 0) || (chars < charEnd));
                }
            }

            [System.Security.SecurityCritical]  // auto-generated
            internal unsafe char GetNextChar()
            {
                 // See if there's something in our fallback buffer
                char cReturn = fallbackBuffer.InternalGetNextChar();

                // Nothing in the fallback buffer, return our normal data.
                if (cReturn == 0)
                {
                    if (chars < charEnd)
                        cReturn = *(chars++);
                }
                
                return cReturn;
             }

            internal unsafe int CharsUsed
            {
                [System.Security.SecurityCritical]  // auto-generated
                get
                {
                    return (int)(chars - charStart);
                }
            }

            internal unsafe int Count
            {
                get
                {
                    return byteCountResult;
                }
            }
        }
    }
}
