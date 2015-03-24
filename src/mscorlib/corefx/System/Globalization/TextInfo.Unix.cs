// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Globalization
{
    public partial class TextInfo
    {
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
            this.m_cultureData = cultureData;
            this.m_cultureName = this.m_cultureData.CultureName;
            this.m_textInfoName = this.m_cultureData.STEXTINFO;
        }

        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Contract.Assert(s != null);              
            // TODO: Implement this fully.

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                sb.Append(ChangeCaseAscii(s[i], toUpper));
            }

            return sb.ToString();
        }

        private unsafe char ChangeCase(char c, bool toUpper)
        {
            // TODO: Implement this fully.
            return ChangeCaseAscii(c, toUpper);
        }

        // PAL Methods end here.

        internal static char ChangeCaseAscii(char c, bool toUpper = true)
        {
            if (toUpper && c >= 'a' && c <= 'z')
            {
                return (char)('A' + (c - 'a'));
            }
            else if (!toUpper && c >= 'A' && c <= 'Z')
            {
                return (char)('a' + (c - 'A'));
            }

            return c;
        }
    }
}