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
        private String _docTypeName = String.Empty;
        private String _pubId = String.Empty;
        private String _sysId = String.Empty;
        private String _internalSubset = String.Empty;
        private String _xmlLang = String.Empty;
        private XmlSpace _xmlSpace;
        private String _baseURI = String.Empty;
        private Encoding _encoding = null;

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, String xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, null, null, null, null, String.Empty, xmlLang, xmlSpace)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, String xmlLang, XmlSpace xmlSpace, Encoding enc)
        : this(nt, nsMgr, null, null, null, null, String.Empty, xmlLang, xmlSpace, enc)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, String docTypeName,
                  String pubId, String sysId, String internalSubset, String baseURI,
                  String xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, docTypeName, pubId, sysId, internalSubset, baseURI, xmlLang, xmlSpace, null)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable nt, XmlNamespaceManager nsMgr, String docTypeName,
                          String pubId, String sysId, String internalSubset, String baseURI,
                          String xmlLang, XmlSpace xmlSpace, Encoding enc)
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
            _docTypeName = (null == docTypeName ? String.Empty : docTypeName);
            _pubId = (null == pubId ? String.Empty : pubId);
            _sysId = (null == sysId ? String.Empty : sysId);
            _internalSubset = (null == internalSubset ? String.Empty : internalSubset);
            _baseURI = (null == baseURI ? String.Empty : baseURI);
            _xmlLang = (null == xmlLang ? String.Empty : xmlLang);
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

        public String DocTypeName
        {
            get
            {
                return _docTypeName;
            }
            set
            {
                _docTypeName = (null == value ? String.Empty : value);
            }
        }

        public String PublicId
        {
            get
            {
                return _pubId;
            }
            set
            {
                _pubId = (null == value ? String.Empty : value);
            }
        }

        public String SystemId
        {
            get
            {
                return _sysId;
            }
            set
            {
                _sysId = (null == value ? String.Empty : value);
            }
        }

        public String BaseURI
        {
            get
            {
                return _baseURI;
            }
            set
            {
                _baseURI = (null == value ? String.Empty : value);
            }
        }

        public String InternalSubset
        {
            get
            {
                return _internalSubset;
            }
            set
            {
                _internalSubset = (null == value ? String.Empty : value);
            }
        }

        public String XmlLang
        {
            get
            {
                return _xmlLang;
            }
            set
            {
                _xmlLang = (null == value ? String.Empty : value);
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



