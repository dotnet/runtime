// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define XMLCHARTYPE_GEN_RESOURCE    // generate the character properties into XmlCharType.bin

using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace System.Xml
{
    /// <include file='doc\XmlCharType.uex' path='docs/doc[@for="XmlCharType"]/*' />
    /// <internalonly/>
    /// <devdoc>
    ///  The XmlCharType class is used for quick character type recognition
    ///  which is optimized for the first 127 ascii characters.
    /// </devdoc>
    unsafe internal struct XmlCharType
    {
        // Surrogate constants
        internal const int SurHighStart = 0xd800;    // 1101 10xx
        internal const int SurHighEnd = 0xdbff;
        internal const int SurLowStart = 0xdc00;    // 1101 11xx
        internal const int SurLowEnd = 0xdfff;
        internal const int SurMask = 0xfc00;    // 1111 11xx

        // Characters defined in the XML 1.0 Fourth Edition
        // Whitespace chars -- Section 2.3 [3]
        // Letters -- Appendix B [84]
        // Starting NCName characters -- Section 2.3 [5] (Starting Name characters without ':')
        // NCName characters -- Section 2.3 [4]          (Name characters without ':')
        // Character data characters -- Section 2.2 [2]
        // PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
        internal const int fWhitespace = 1;
        internal const int fLetter = 2;
        internal const int fNCStartNameSC = 4;
        internal const int fNCNameSC = 8;
        internal const int fCharData = 16;
        internal const int fNCNameXml4e = 32;
        internal const int fText = 64;
        internal const int fAttrValue = 128;

        // bitmap for public ID characters - 1 bit per character 0x0 - 0x80; no character > 0x80 is a PUBLIC ID char
        private const string s_PublicIdBitmap = "\u2400\u0000\uffbb\uafff\uffff\u87ff\ufffe\u07ff";

        // size of XmlCharType table
        private const uint CharPropertiesSize = (uint)char.MaxValue + 1;

        // static lock for XmlCharType class
        private static object s_Lock;

        private static object StaticLock
        {
            get
            {
                if (s_Lock == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange<object>(ref s_Lock, o, null);
                }
                return s_Lock;
            }
        }

        private static volatile byte* s_CharProperties;
        internal byte* charProperties;
        private static void InitInstance()
        {
            lock (StaticLock)
            {
                if (s_CharProperties != null)
                {
                    return;
                }

                UnmanagedMemoryStream memStream = (UnmanagedMemoryStream)typeof(XmlWriter).Assembly.GetManifestResourceStream("XmlCharType.bin");
                Debug.Assert(memStream.Length == CharPropertiesSize);

                byte* chProps = memStream.PositionPointer;
                Thread.MemoryBarrier();  // For weak memory models (IA64)
                s_CharProperties = chProps;
            }
        }

        private XmlCharType(byte* charProperties)
        {
            Debug.Assert(s_CharProperties != null);
            this.charProperties = charProperties;
        }

        public static XmlCharType Instance
        {
            get
            {
                if (s_CharProperties == null)
                {
                    InitInstance();
                }
                return new XmlCharType(s_CharProperties);
            }
        }

        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        public bool IsWhiteSpace(char ch)
        {
            return (charProperties[ch] & fWhitespace) != 0;
        }

        public bool IsExtender(char ch)
        {
            return (ch == 0xb7);
        }

        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        public bool IsNCNameSingleChar(char ch)
        {
            return (charProperties[ch] & fNCNameSC) != 0;
        }

        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        public bool IsStartNCNameSingleChar(char ch)
        {
            return (charProperties[ch] & fNCStartNameSC) != 0;
        }

        public bool IsNameSingleChar(char ch)
        {
            return IsNCNameSingleChar(ch) || ch == ':';
        }

        public bool IsStartNameSingleChar(char ch)
        {
            return IsStartNCNameSingleChar(ch) || ch == ':';
        }

        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        public bool IsCharData(char ch)
        {
            return (charProperties[ch] & fCharData) != 0;
        }

        // [13] PubidChar ::=  #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] Section 2.3 of spec
        public bool IsPubidChar(char ch)
        {
            if (ch < (char)0x80)
            {
                return (s_PublicIdBitmap[ch >> 4] & (1 << (ch & 0xF))) != 0;
            }
            return false;
        }

        // TextChar = CharData - { 0xA, 0xD, '<', '&', ']' }
        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        internal bool IsTextChar(char ch)
        {
            return (charProperties[ch] & fText) != 0;
        }

        // AttrValueChar = CharData - { 0xA, 0xD, 0x9, '<', '>', '&', '\'', '"' }
        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        internal bool IsAttributeValueChar(char ch)
        {
            return (charProperties[ch] & fAttrValue) != 0;
        }

        // XML 1.0 Fourth Edition definitions
        //
        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        public bool IsLetter(char ch)
        {
            return (charProperties[ch] & fLetter) != 0;
        }

        // NOTE: This method will not be inlined (because it uses byte* charProperties)
        // This method uses the XML 4th edition name character ranges
        public bool IsNCNameCharXml4e(char ch)
        {
            return (charProperties[ch] & fNCNameXml4e) != 0;
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsStartNCNameCharXml4e(char ch)
        {
            return IsLetter(ch) || ch == '_';
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsNameCharXml4e(char ch)
        {
            return IsNCNameCharXml4e(ch) || ch == ':';
        }

        // This method uses the XML 4th edition name character ranges
        public bool IsStartNameCharXml4e(char ch)
        {
            return IsStartNCNameCharXml4e(ch) || ch == ':';
        }

        // Digit methods
        public static bool IsDigit(char ch)
        {
            return InRange(ch, 0x30, 0x39);
        }

        public static bool IsHexDigit(char ch)
        {
            return InRange(ch, 0x30, 0x39) || InRange(ch, 'a', 'f') || InRange(ch, 'A', 'F');
        }

        // Surrogate methods
        internal static bool IsHighSurrogate(int ch)
        {
            return InRange(ch, SurHighStart, SurHighEnd);
        }

        internal static bool IsLowSurrogate(int ch)
        {
            return InRange(ch, SurLowStart, SurLowEnd);
        }

        internal static bool IsSurrogate(int ch)
        {
            return InRange(ch, SurHighStart, SurLowEnd);
        }

        internal static int CombineSurrogateChar(int lowChar, int highChar)
        {
            return (lowChar - SurLowStart) | ((highChar - SurHighStart) << 10) + 0x10000;
        }

        internal static void SplitSurrogateChar(int combinedChar, out char lowChar, out char highChar)
        {
            int v = combinedChar - 0x10000;
            lowChar = (char)(SurLowStart + v % 1024);
            highChar = (char)(SurHighStart + v / 1024);
        }

        internal bool IsOnlyWhitespace(string str)
        {
            return IsOnlyWhitespaceWithPos(str) == -1;
        }

        // Character checking on strings
        internal int IsOnlyWhitespaceWithPos(string str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if ((charProperties[str[i]] & fWhitespace) == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        internal int IsOnlyCharData(string str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if ((charProperties[str[i]] & fCharData) == 0)
                    {
                        if (i + 1 >= str.Length || !(XmlCharType.IsHighSurrogate(str[i]) && XmlCharType.IsLowSurrogate(str[i + 1])))
                        {
                            return i;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
            return -1;
        }

        static internal bool IsOnlyDigits(string str, int startPos, int len)
        {
            Debug.Assert(str != null);
            Debug.Assert(startPos + len <= str.Length);
            Debug.Assert(startPos <= str.Length);

            for (int i = startPos; i < startPos + len; i++)
            {
                if (!IsDigit(str[i]))
                {
                    return false;
                }
            }
            return true;
        }

        static internal bool IsOnlyDigits(char[] chars, int startPos, int len)
        {
            Debug.Assert(chars != null);
            Debug.Assert(startPos + len <= chars.Length);
            Debug.Assert(startPos <= chars.Length);

            for (int i = startPos; i < startPos + len; i++)
            {
                if (!IsDigit(chars[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal int IsPublicId(string str)
        {
            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (!IsPubidChar(str[i]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        // This method tests whether a value is in a given range with just one test; start and end should be constants
        private static bool InRange(int value, int start, int end)
        {
            Debug.Assert(start <= end);
            return (uint)(value - start) <= (uint)(end - start);
        }

#if XMLCHARTYPE_GEN_RESOURCE
        //
        // Code for generating XmlCharType.bin table and s_PublicIdBitmap
        //
        // build command line:  csc XmlCharType.cs /d:XMLCHARTYPE_GEN_RESOURCE
        //
        public static void Main( string[] args ) {
            try {
                InitInstance();

                // generate PublicId bitmap
                ushort[] bitmap = new ushort[0x80 >> 4];
                for (int i = 0; i < s_PublicID.Length; i += 2) {
                    for (int j = s_PublicID[i], last = s_PublicID[i + 1]; j <= last; j++) {
                        bitmap[j >> 4] |= (ushort)(1 << (j & 0xF));
                    }
                }

                Console.Write("private const string s_PublicIdBitmap = \"");
                for (int i = 0; i < bitmap.Length; i++) {
                    Console.Write("\\u{0:x4}", bitmap[i]);
                }
                Console.WriteLine("\";");
                Console.WriteLine();

                string fileName = ( args.Length == 0 ) ? "XmlCharType.bin" : args[0];
                Console.Write( "Writing XmlCharType character properties to {0}...", fileName );

                FileStream fs = new FileStream( fileName, FileMode.Create );
                for ( int i = 0; i < CharPropertiesSize; i += 4096 ) {
                    fs.Write( s_CharProperties, i, 4096 );
                }
                fs.Close();
                Console.WriteLine( "done." );
            }
            catch ( Exception e ) {
                Console.WriteLine();
                Console.WriteLine( "Exception: {0}", e.Message );
            }
        }
#endif
    }
}
