// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization {
    using System.Text;
    using System.Runtime.Remoting;
    using System;
    using System.Security;

    //
    // Data item for EncodingTable.  Along with EncodingTable, they are used by 
    // System.Text.Encoding.
    // 
    // This class stores a pointer to the internal data and the index into that data
    // where our required information is found.  We load the code page, flags and uiFamilyCodePage
    // immediately because they don't require creating an object.  Creating any of the string
    // names is delayed until somebody actually asks for them and the names are then cached.
    
    [Serializable]
    internal class CodePageDataItem
    {
        internal int    m_dataIndex;
        internal int    m_uiFamilyCodePage;
        internal String m_webName;
        internal String m_headerName;
        internal String m_bodyName;
        internal uint   m_flags;
    
        [SecurityCritical]
        unsafe internal CodePageDataItem(int dataIndex) {
            m_dataIndex = dataIndex;
            m_uiFamilyCodePage = EncodingTable.codePageDataPtr[dataIndex].uiFamilyCodePage;
            m_flags = EncodingTable.codePageDataPtr[dataIndex].flags;
        }

        [System.Security.SecurityCritical]
        unsafe internal static String CreateString(sbyte* pStrings, uint index)
        {
            if (pStrings[0] == '|') // |str1|str2|str3
            {
                int start = 1;
                
                for (int i = 1; true; i ++)
                {
                    sbyte ch = pStrings[i];

                    if ((ch == '|') || (ch == 0))
                    {
                        if (index == 0)
                        {
                            return new String(pStrings, start, i - start);
                        }

                        index --;
                        start = i + 1;

                        if (ch == 0)
                        {
                            break;
                        }
                    }
                }

                throw new ArgumentException("pStrings");
            }
            else
            {
                return new String(pStrings);
            }
        }

        unsafe public String WebName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (m_webName==null) {
                    m_webName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 0);
                }
                return m_webName;
            }
        }
    
        public virtual int UIFamilyCodePage {
            get {
                return m_uiFamilyCodePage;
            }
        }
    
        unsafe public String HeaderName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (m_headerName==null) {
                    m_headerName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 1);
                }
                return m_headerName;
            }
        }
    
        unsafe public String BodyName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (m_bodyName==null) {
                    m_bodyName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 2);
                }
                return m_bodyName;
            }
        }    

        unsafe public uint Flags {
            get {
                return (m_flags);
            }
        }
    }
}
