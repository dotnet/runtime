// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System;
using System.Security;

namespace System.Text
{
    //
    // Data item for EncodingTable.  Along with EncodingTable, they are used by 
    // System.Text.Encoding.
    // 
    // This class stores a pointer to the internal data and the index into that data
    // where our required information is found.  We load the code page, flags and uiFamilyCodePage
    // immediately because they don't require creating an object.  Creating any of the string
    // names is delayed until somebody actually asks for them and the names are then cached.

    internal class CodePageDataItem
    {
        internal int m_dataIndex;
        internal int m_uiFamilyCodePage;
        internal string m_webName;
        internal string m_headerName;
        internal string m_bodyName;
        internal uint m_flags;

        internal unsafe CodePageDataItem(int dataIndex)
        {
            m_dataIndex = dataIndex;
            m_uiFamilyCodePage = EncodingTable.codePageDataPtr[dataIndex].uiFamilyCodePage;
            m_flags = EncodingTable.codePageDataPtr[dataIndex].flags;
        }

        internal static unsafe string CreateString(sbyte* pStrings, uint index)
        {
            if (pStrings[0] == '|') // |str1|str2|str3
            {
                int start = 1;

                for (int i = 1; true; i++)
                {
                    sbyte ch = pStrings[i];

                    if ((ch == '|') || (ch == 0))
                    {
                        if (index == 0)
                        {
                            return new string(pStrings, start, i - start);
                        }

                        index--;
                        start = i + 1;

                        if (ch == 0)
                        {
                            break;
                        }
                    }
                }

                throw new ArgumentException(null, nameof(pStrings));
            }
            else
            {
                return new string(pStrings);
            }
        }

        public unsafe string WebName
        {
            get
            {
                if (m_webName == null)
                {
                    m_webName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 0);
                }
                return m_webName;
            }
        }

        public virtual int UIFamilyCodePage
        {
            get
            {
                return m_uiFamilyCodePage;
            }
        }

        public unsafe string HeaderName
        {
            get
            {
                if (m_headerName == null)
                {
                    m_headerName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 1);
                }
                return m_headerName;
            }
        }

        public unsafe string BodyName
        {
            get
            {
                if (m_bodyName == null)
                {
                    m_bodyName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 2);
                }
                return m_bodyName;
            }
        }

        public unsafe uint Flags
        {
            get
            {
                return (m_flags);
            }
        }
    }
}
