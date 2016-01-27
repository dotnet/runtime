// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// 
//
// PURPOSE:  Represent an XML document
// 
// 
//---------------------------------------------------------------------------

namespace System.Security
{
    using System;
    using System.Collections;
    using System.Security.Util;
    using System.Text;
    using System.Globalization;
    using System.IO;
    using System.Diagnostics.Contracts;
    using StringMaker = System.Security.Util.Tokenizer.StringMaker;

    [Serializable]
    sealed internal class SecurityDocumentElement : ISecurityElementFactory
    {
        private int m_position;
        private SecurityDocument m_document;

        internal SecurityDocumentElement( SecurityDocument document, int position )
        {
            m_document = document;
            m_position = position;
        }

        SecurityElement ISecurityElementFactory.CreateSecurityElement()
        {
            return m_document.GetElement( m_position, true );
        }

        Object ISecurityElementFactory.Copy()
        {
            return new SecurityDocumentElement( m_document, m_position );
        }

        String ISecurityElementFactory.GetTag()
        {
            return m_document.GetTagForElement( m_position );
        }

        String ISecurityElementFactory.Attribute( String attributeName )
        {
            return m_document.GetAttributeForElement( m_position, attributeName );
        }

    }


    [Serializable]
    sealed internal class SecurityDocument
    {
        internal byte[] m_data;

        internal const byte c_element = 1;
        internal const byte c_attribute = 2;
        internal const byte c_text = 3;
        internal const byte c_children = 4;
        internal const int c_growthSize = 32;

        public SecurityDocument( int numData )
        {
            m_data = new byte[numData];
        }

        public SecurityDocument( byte[] data )
        {
            this.m_data = data;
        }

        public SecurityDocument( SecurityElement elRoot )
        {
            m_data = new byte[c_growthSize];

            int position = 0;
            ConvertElement( elRoot, ref position );
        }

        public void GuaranteeSize( int size )
        {
            if (m_data.Length < size)
            {
                byte[] m_newData = new byte[((size / c_growthSize) + 1) * c_growthSize];
                Array.Copy( m_data, 0, m_newData, 0, m_data.Length );
                m_data = m_newData;
            }
        }

        public void AddString( String str, ref int position )
        {
            GuaranteeSize( position + str.Length * 2 + 2 );

            for (int i = 0; i < str.Length; ++i)
            {
                m_data[position+(2*i)] = (byte)(str[i] >> 8);
                m_data[position+(2*i)+1] = (byte)(str[i] & 0x00FF);
            }
            m_data[position + str.Length * 2] = 0;
            m_data[position + str.Length * 2 + 1] = 0;

            position += str.Length * 2 + 2;
        }

        public void AppendString( String str, ref int position )
        {
            if (position <= 1 ||
                m_data[position - 1] != 0 ||
                m_data[position - 2] != 0 )
                throw new XmlSyntaxException();

            position -= 2;

            AddString( str, ref position );
        }

        public static int EncodedStringSize( String str )
        {
            return str.Length * 2 + 2;
        }

        public String GetString( ref int position )
        {
            return GetString( ref position, true );
        }

        public String GetString( ref int position, bool bCreate )
        {
            int stringEnd;
            bool bFoundEnd = false;
            for (stringEnd = position; stringEnd < m_data.Length-1; stringEnd += 2)
            {
                if (m_data[stringEnd] == 0 && m_data[stringEnd + 1] == 0)
                {
                    bFoundEnd = true;
                    break;
                }
            }

            Contract.Assert(bFoundEnd, "Malformed string in parse data");

            StringMaker m = System.SharedStatics.GetSharedStringMaker();

            try 
            {

                if (bCreate)
                {
                    m._outStringBuilder = null;
                    m._outIndex = 0;

                    for (int i = position; i < stringEnd; i += 2)
                    {
                        char c = (char)(m_data[i] << 8 | m_data[i+1]);

                        // add character  to the string
                        if (m._outIndex < StringMaker.outMaxSize) 
                        {
                            // easy case
                            m._outChars[m._outIndex++] = c;
                        } 
                        else
                        {
                            if (m._outStringBuilder == null) 
                            {
                                // OK, first check if we have to init the StringBuilder
                                m._outStringBuilder = new StringBuilder();
                            }
                    
                            // OK, copy from _outChars to _outStringBuilder
                            m._outStringBuilder.Append(m._outChars, 0, StringMaker.outMaxSize);
                     
                            // reset _outChars pointer
                            m._outChars[0] = c;
                            m._outIndex = 1;
                        }
                    }
                }

                position = stringEnd + 2;

                if (bCreate)
                    return m.MakeString();
                else
                    return null;
            }
            finally
            {
                System.SharedStatics.ReleaseSharedStringMaker(ref m);
            }
        }
                    

        public void AddToken( byte b, ref int position )
        {
            GuaranteeSize( position + 1 );
            m_data[position++] = b;
        }

        public void ConvertElement( SecurityElement elCurrent, ref int position )
        {
            AddToken( c_element, ref position );
            AddString( elCurrent.m_strTag, ref position );

            if (elCurrent.m_lAttributes != null)
            {
                for (int i = 0; i < elCurrent.m_lAttributes.Count; i+=2)
                {
                    AddToken( c_attribute, ref position );
                    AddString( (String)elCurrent.m_lAttributes[i], ref position );
                    AddString( (String)elCurrent.m_lAttributes[i+1], ref position );
                }
            }

            if (elCurrent.m_strText != null)
            {
                AddToken( c_text, ref position );
                AddString( elCurrent.m_strText, ref position );
            }

            if (elCurrent.InternalChildren != null)
            {
                for (int i = 0; i < elCurrent.InternalChildren.Count; ++i)
                {
                    ConvertElement( (SecurityElement)elCurrent.Children[i], ref position );
                }
            }
            AddToken( c_children, ref position );
        }

        public SecurityElement GetRootElement()
        {
            return GetElement( 0, true );
        }

        public SecurityElement GetElement( int position, bool bCreate )
        {
            SecurityElement elRoot = InternalGetElement( ref position, bCreate );
            return elRoot;
        }

        internal SecurityElement InternalGetElement( ref int position, bool bCreate )
        {
            if (m_data.Length <= position)
                throw new XmlSyntaxException();

            if (m_data[position++] != c_element)
                throw new XmlSyntaxException();

            SecurityElement elCurrent = null;
            String strTag = GetString( ref position, bCreate );
            if (bCreate)
                elCurrent = new SecurityElement( strTag );

            while (m_data[position] == c_attribute)
            {
                position++;
                String strName = GetString( ref position, bCreate );
                String strValue = GetString( ref position, bCreate );
                if (bCreate)
                    elCurrent.AddAttribute( strName, strValue );
            }

            if (m_data[position] == c_text)
            {
                position++;
                String strText = GetString( ref position, bCreate );
                if (bCreate)
                    elCurrent.m_strText = strText;
            }

            while (m_data[position] != c_children)
            {
                SecurityElement elChild = InternalGetElement( ref position, bCreate );
                if (bCreate)
                    elCurrent.AddChild( elChild );
            }
            position++;

            return elCurrent;
        }

        public String GetTagForElement( int position )
        {
            if (m_data.Length <= position)
                throw new XmlSyntaxException();

            if (m_data[position++] != c_element)
                throw new XmlSyntaxException();

            String strTag = GetString( ref position );
            return strTag;
        }

        public ArrayList GetChildrenPositionForElement( int position )
        {
            if (m_data.Length <= position)
                throw new XmlSyntaxException();

            if (m_data[position++] != c_element)
                throw new XmlSyntaxException();

            ArrayList children = new ArrayList();

            // This is to move past the tag string
            GetString( ref position );

            while (m_data[position] == c_attribute)
            {
                position++;
                // Read name and value, then throw them away
                GetString( ref position, false );
                GetString( ref position, false );
            }

            if (m_data[position] == c_text)
            {
                position++;
                // Read text, then throw it away.
                GetString( ref position, false );
            }

            while (m_data[position] != c_children)
            {
                children.Add( position );
                InternalGetElement( ref position, false );
            }
            position++;

            return children;
        }

        public String GetAttributeForElement( int position, String attributeName )
        {
            if (m_data.Length <= position)
                throw new XmlSyntaxException();

            if (m_data[position++] != c_element)
                throw new XmlSyntaxException();

            String strRetValue = null;
            // This is to move past the tag string.
            GetString( ref position, false );
            

            while (m_data[position] == c_attribute)
            {
                position++;
                String strName = GetString( ref position );
                String strValue = GetString( ref position );

                if (String.Equals( strName, attributeName ))
                {
                    strRetValue = strValue;
                    break;
                }
            }


            return strRetValue;
        }
    }
}






