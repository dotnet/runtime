// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    internal class OutKeywords
    {
#if DEBUG
        private XmlNameTable _NameTable;
#endif
        internal OutKeywords(XmlNameTable nameTable)
        {
            Debug.Assert(nameTable != null);
#if DEBUG
            _NameTable = nameTable;
#endif

            _AtomEmpty = nameTable.Add(string.Empty);
            _AtomLang = nameTable.Add("lang");
            _AtomSpace = nameTable.Add("space");
            _AtomXmlns = nameTable.Add("xmlns");
            _AtomXml = nameTable.Add("xml");
            _AtomXmlNamespace = nameTable.Add(XmlReservedNs.NsXml);
            _AtomXmlnsNamespace = nameTable.Add(XmlReservedNs.NsXmlNs);

            CheckKeyword(_AtomEmpty);
            CheckKeyword(_AtomLang);
            CheckKeyword(_AtomSpace);
            CheckKeyword(_AtomXmlns);
            CheckKeyword(_AtomXml);
            CheckKeyword(_AtomXmlNamespace);
            CheckKeyword(_AtomXmlnsNamespace);
        }

        private string _AtomEmpty;
        private string _AtomLang;
        private string _AtomSpace;
        private string _AtomXmlns;
        private string _AtomXml;
        private string _AtomXmlNamespace;
        private string _AtomXmlnsNamespace;

        internal string Empty
        {
            get
            {
                CheckKeyword(_AtomEmpty);
                return _AtomEmpty;
            }
        }

        internal string Lang
        {
            get
            {
                CheckKeyword(_AtomLang);
                return _AtomLang;
            }
        }

        internal string Space
        {
            get
            {
                CheckKeyword(_AtomSpace);
                return _AtomSpace;
            }
        }

        internal string Xmlns
        {
            get
            {
                CheckKeyword(_AtomXmlns);
                return _AtomXmlns;
            }
        }

        internal string Xml
        {
            get
            {
                CheckKeyword(_AtomXml);
                return _AtomXml;
            }
        }

        internal string XmlNamespace
        {
            get
            {
                CheckKeyword(_AtomXmlNamespace);
                return _AtomXmlNamespace;       // http://www.w3.org/XML/1998/namespace
            }
        }

        internal string XmlnsNamespace
        {
            get
            {
                CheckKeyword(_AtomXmlnsNamespace);
                return _AtomXmlnsNamespace;               // http://www.w3.org/XML/2000/xmlns
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckKeyword(string keyword)
        {
#if DEBUG
            Debug.Assert(keyword != null);
            Debug.Assert((object)keyword == (object)_NameTable.Get(keyword));
#endif
        }
    }
}
