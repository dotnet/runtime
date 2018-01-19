// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace System.Text
{
    public sealed partial class CodePagesEncodingProvider : EncodingProvider
    {
        private static readonly EncodingProvider s_singleton = new CodePagesEncodingProvider();
        private Dictionary<int, Encoding> _encodings = new Dictionary<int, Encoding>();
        private ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        internal CodePagesEncodingProvider() { }

        public static EncodingProvider Instance
        {
            get { return s_singleton; }
        }

        public override Encoding GetEncoding(int codepage)
        {
            if (codepage < 0 || codepage > 65535)
                return null;

            if (codepage == 0)
            {
                // Retrieve the system default non-unicode code page if possible, or return null,
                // giving the rest of the EncodingProviders a chance to return a default.
                int systemDefaultCodePage = SystemDefaultCodePage;
                return systemDefaultCodePage != 0 ?
                    GetEncoding(systemDefaultCodePage) :
                    null;
            }

            Encoding result = null;

            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (_encodings.TryGetValue(codepage, out result))
                    return result;

                int i = BaseCodePageEncoding.GetCodePageByteSize(codepage);

                if (i == 1)
                {
                    result = new SBCSCodePageEncoding(codepage);
                }
                else if (i == 2)
                {
                    result = new DBCSCodePageEncoding(codepage);
                }
                else
                {
                    result = GetEncodingRare(codepage);
                    if (result == null)
                        return null;
                }

                _cacheLock.EnterWriteLock();
                try
                {
                    Encoding cachedEncoding;
                    if (_encodings.TryGetValue(codepage, out cachedEncoding))
                        return cachedEncoding;

                    _encodings.Add(codepage, result);
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }

            return result;
        }

        public override Encoding GetEncoding(string name)
        {
            int codepage = EncodingTable.GetCodePageFromName(name);
            if (codepage == 0)
                return null;

            return GetEncoding(codepage);
        }

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

        // ISO 2022 Code Pages
        private const int ISOKorean = 50225;
        private const int ChineseHZ = 52936;    // HZ has ~}~{~~ sequences
        private const int ISO2022JP = 50220;
        private const int ISO2022JPESC = 50221;
        private const int ISO2022JPSISO = 50222;
        private const int ISOSimplifiedCN = 50227;
        private const int EUCJP = 51932;

        // 20936 has same code page as 10008, so we'll special case it
        private const int CodePageMacGB2312 = 10008;
        private const int CodePageMacKorean = 10003;
        private const int CodePageGB2312 = 20936;
        private const int CodePageDLLKorean = 20949;

        // GB18030
        private const int GB18030 = 54936;

        // 51936 is the same as 936
        private const int DuplicateEUCCN = 51936;
        private const int EUCKR = 51949;
        private const int EUCCN = 936;

        // Other
        private const int ISO_8859_8I = 38598;
        private const int ISO_8859_8_Visual = 28598;

        private static Encoding GetEncodingRare(int codepage)
        {
            Encoding result = null;

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
                // so does ISOSimplifiedCN (50227), which has gotta be broken
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
                case ISO_8859_8I:
                    result = new SBCSCodePageEncoding(codepage, ISO_8859_8_Visual);        // Hebrew maps to a different code page
                    break;
            }
            return result;
        }
    }
}
