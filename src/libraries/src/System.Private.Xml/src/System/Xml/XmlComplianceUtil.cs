// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace System.Xml
{
    using System.Text;

    internal static class XmlComplianceUtil
    {
        // Replaces \r\n, \n, \r and \t with single space (0x20) and then removes spaces
        // at the beggining and the end of the string and replaces sequences of spaces
        // with a single space.
        public static string NonCDataNormalize(string value)
        {
            int len = value.Length;
            if (len <= 0)
            {
                return string.Empty;
            }

            int startPos = 0;
            StringBuilder norValue = null;
            XmlCharType xmlCharType = XmlCharType.Instance;
            while (xmlCharType.IsWhiteSpace(value[startPos]))
            {
                startPos++;
                if (startPos == len)
                {
                    return " ";
                }
            }

            int i = startPos;
            while (i < len)
            {
                if (!xmlCharType.IsWhiteSpace(value[i]))
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                while (j < len && xmlCharType.IsWhiteSpace(value[j]))
                {
                    j++;
                }
                if (j == len)
                {
                    if (norValue == null)
                    {
                        return value.Substring(startPos, i - startPos);
                    }
                    else
                    {
                        norValue.Append(value, startPos, i - startPos);
                        return norValue.ToString();
                    }
                }
                if (j > i + 1 || value[i] != 0x20)
                {
                    if (norValue == null)
                    {
                        norValue = new StringBuilder(len);
                    }
                    norValue.Append(value, startPos, i - startPos);
                    norValue.Append((char)0x20);
                    startPos = j;
                    i = j;
                }
                else
                {
                    i++;
                }
            }

            if (norValue != null)
            {
                if (startPos < i)
                {
                    norValue.Append(value, startPos, i - startPos);
                }
                return norValue.ToString();
            }
            else
            {
                if (startPos > 0)
                {
                    return value.Substring(startPos, len - startPos);
                }
                else
                {
                    return value;
                }
            }
        }

        // Replaces \r\n, \n, \r and \t with single space (0x20) 
        public static string CDataNormalize(string value)
        {
            int len = value.Length;

            if (len <= 0)
            {
                return string.Empty;
            }

            int i = 0;
            int startPos = 0;
            StringBuilder norValue = null;

            while (i < len)
            {
                char ch = value[i];
                if (ch >= 0x20 || (ch != 0x9 && ch != 0xA && ch != 0xD))
                {
                    i++;
                    continue;
                }

                if (norValue == null)
                {
                    norValue = new StringBuilder(len);
                }
                if (startPos < i)
                {
                    norValue.Append(value, startPos, i - startPos);
                }
                norValue.Append((char)0x20);

                if (ch == 0xD && (i + 1 < len && value[i + 1] == 0xA))
                {
                    i += 2;
                }
                else
                {
                    i++;
                }
                startPos = i;
            }

            if (norValue == null)
            {
                return value;
            }
            else
            {
                if (i > startPos)
                {
                    norValue.Append(value, startPos, i - startPos);
                }
                return norValue.ToString();
            }
        }

        public static bool IsValidLanguageID(char[] value, int startPos, int length)
        {
            int len = length;
            if (len < 2)
            {
                return false;
            }

            bool fSeenLetter = false;
            int i = startPos;
            XmlCharType xmlCharType = XmlCharType.Instance;

            char ch = value[i];
            if (xmlCharType.IsLetter(ch))
            {
                if (xmlCharType.IsLetter(value[++i]))
                {
                    if (len == 2)
                    {
                        return true;
                    }
                    len--;
                    i++;
                }
                else if (('I' != ch && 'i' != ch) && ('X' != ch && 'x' != ch))
                {  //IANA or custom Code
                    return false;
                }

                if (value[i] != '-')
                {
                    return false;
                }

                len -= 2;
                while (len-- > 0)
                {
                    ch = value[++i];
                    if (xmlCharType.IsLetter(ch))
                    {
                        fSeenLetter = true;
                    }
                    else if (ch == '-' && fSeenLetter)
                    {
                        fSeenLetter = false;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (fSeenLetter)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
