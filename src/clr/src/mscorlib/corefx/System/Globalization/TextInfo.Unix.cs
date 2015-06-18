// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Text;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private readonly bool m_needsTurkishCasing;

        //////////////////////////////////////////////////////////////////////////
        ////
        ////  TextInfo Constructors
        ////
        ////  Implements CultureInfo.TextInfo.
        ////
        //////////////////////////////////////////////////////////////////////////
        internal unsafe TextInfo(CultureData cultureData)
        {
            // TODO: Implement this fully.
            m_cultureData = cultureData;
            m_cultureName = m_cultureData.CultureName;
            m_textInfoName = m_cultureData.STEXTINFO;
            m_needsTurkishCasing = NeedsTurkishCasing(this.m_textInfoName);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Contract.Assert(s != null);

            char[] dstBuf = new char[s.Length];

            fixed (char* pSrc = s, pDst = dstBuf)
            {
                ChangeCaseCore(pSrc, s.Length, pDst, dstBuf.Length, toUpper);
            }

            return new string(dstBuf);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe char ChangeCase(char c, bool toUpper)
        {
            char* pSrc = stackalloc char[1];
            char* pDst = stackalloc char[1];

            pSrc[0] = c;

            ChangeCaseCore(pSrc, 1, pDst, 1, toUpper);

            return pDst[0];
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private unsafe void ChangeCaseCore(char* pSrc, int cchSrc, char* pDst, int cchDst, bool toUpper)
        {
            if (toUpper)
            {
                if (!m_needsTurkishCasing)
                {
                    Interop.GlobalizationInterop.ToUpperSimple(pSrc, cchSrc, pDst, cchDst);
                }
                else
                {
                    Interop.GlobalizationInterop.ToUpperSimpleTurkishAzeri(pSrc, cchSrc, pDst, cchDst);
                }
            }
            else
            {
                if (!m_needsTurkishCasing)
                {
                    Interop.GlobalizationInterop.ToLowerSimple(pSrc, cchSrc, pDst, cchDst);
                }
                else
                {
                    Interop.GlobalizationInterop.ToLowerSimpleTurkishAzeri(pSrc, cchSrc, pDst, cchDst);
                }
            }
        }

        private bool NeedsTurkishCasing(string localeName)
        {
            Contract.Assert(localeName != null);

            string lcName = CultureData.AnsiToLower(localeName);
            return lcName.Length >= 2 && ((lcName[0] == 't' && lcName[1] == 'r') || (lcName[0] == 'a' && lcName[1] == 'z'));
        }
    }
}