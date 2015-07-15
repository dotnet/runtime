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
            m_cultureData = cultureData;
            m_cultureName = m_cultureData.CultureName;
            m_textInfoName = m_cultureData.STEXTINFO;
            m_needsTurkishCasing = NeedsTurkishCasing(m_textInfoName);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Contract.Assert(s != null);

            char[] buf = new char[s.Length];

            fixed(char* pBuf = buf)
            {
                Interop.GlobalizationInterop.ChangeCase(s, s.Length, pBuf, buf.Length, toUpper, m_needsTurkishCasing);
            }

            return new string(buf);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe char ChangeCase(char c, bool toUpper)
        {
            char* pSrc = stackalloc char[1];
            char* pDst = stackalloc char[1];

            pSrc[0] = c;

            Interop.GlobalizationInterop.ChangeCase(pSrc, 1, pDst, 1, toUpper, m_needsTurkishCasing);

            return pDst[0];
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private bool NeedsTurkishCasing(string localeName)
        {
            Contract.Assert(localeName != null);
            return CultureInfo.GetCultureInfo(localeName).CompareInfo.Compare("i", "I", CompareOptions.IgnoreCase) != 0;
        }
    }
}
