// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Text;
using System;

namespace System.Xml
{
    // Specifies the context that the XmLReader will use for xml fragment
    public class XmlParserContext
    {
        private XmlNameTable _nt = null;
        private XmlNamespaceManager _nsMgr = null;
        private string _docTypeName = string.Empty;
        private string _pubId = string.Empty;
        private string _sysId = string.Empty;
        private string _internalSubset = string.Empty;
        private string _xmlLang = string.Empty;
        private XmlSpace _xmlSpace;
        private string _baseURI = string.Empty;
        private Encoding _encoding = null;

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, string xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, string xmlLang, XmlSpace xmlSpace, Encoding enc)
        : this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace, enc)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, string docTypeName,
                  string pubId, string sysId, string internalSubset, string baseURI,
                  string xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, docTypeName, pubId, sysId, internalSubset, baseURI, xmlLang, xmlSpace, null)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, string docTypeName,
                          string pubId, string sysId, string internalSubset, string baseURI,
                          string xmlLang, XmlSpace xmlSpace, Encoding enc)
        {
            if (nsMgr != null)
            {
                if (nt == null)
                {
                    _nt = nsMgr.NameTable;
                }
                else
                {
                    if ((object)nt != (object)nsMgr.NameTable)
                    {
                        throw new XmlException(SR.Xml_NotSameNametable, string.Empty);
                    }
                    _nt = nt;
                }
            }
            else
            {
                _nt = nt;
            }

            _nsMgr = nsMgr;
            _docTypeName = (null == docTypeName ? string.Empty : docTypeName);
            _pubId = (null == pubId ? string.Empty : pubId);
            _sysId = (null == sysId ? string.Empty : sysId);
            _internalSubset = (null == internalSubset ? string.Empty : internalSubset);
            _baseURI = (null == baseURI ? string.Empty : baseURI);
            _xmlLang = (null == xmlLang ? string.Empty : xmlLang);
            _xmlSpace = xmlSpace;
            _encoding = enc;
        }

        public XmlNameTable NameTable
        {
            get
            {
                return _nt;
            }
            set
            {
                _nt = value;
            }
        }

        public XmlNamespaceManager NamespaceManager
        {
            get
            {
                return _nsMgr;
            }
            set
            {
                _nsMgr = value;
            }
        }

        public string DocTypeName
        {
            get
            {
                return _docTypeName;
            }
            set
            {
                _docTypeName = (null == value ? string.Empty : value);
            }
        }

        public string PublicId
        {
            get
            {
                return _pubId;
            }
            set
            {
                _pubId = (null == value ? string.Empty : value);
            }
        }

        public string SystemId
        {
            get
            {
                return _sysId;
            }
            set
            {
                _sysId = (null == value ? string.Empty : value);
            }
        }

        public string BaseURI
        {
            get
            {
                return _baseURI;
            }
            set
            {
                _baseURI = (null == value ? string.Empty : value);
            }
        }

        public string InternalSubset
        {
            get
            {
                return _internalSubset;
            }
            set
            {
                _internalSubset = (null == value ? string.Empty : value);
            }
        }

        public string XmlLang
        {
            get
            {
                return _xmlLang;
            }
            set
            {
                _xmlLang = (null == value ? string.Empty : value);
            }
        }

        public XmlSpace XmlSpace
        {
            get
            {
                return _xmlSpace;
            }
            set
            {
                _xmlSpace = value;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        internal bool HasDtdInfo
        {
            get
            {
                return (_internalSubset != string.Empty || _pubId != string.Empty || _sysId != string.Empty);
            }
        }
    } // class XmlContext
} // namespace System.Xml



