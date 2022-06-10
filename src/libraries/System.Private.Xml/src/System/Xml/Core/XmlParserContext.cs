// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Text;
using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    // Specifies the context that the XmLReader will use for xml fragment
    public class XmlParserContext
    {
        private XmlNameTable? _nt;
        private XmlNamespaceManager? _nsMgr;
        private string _docTypeName = string.Empty;
        private string _pubId = string.Empty;
        private string _sysId = string.Empty;
        private string _internalSubset = string.Empty;
        private string _xmlLang = string.Empty;
        private XmlSpace _xmlSpace;
        private string _baseURI = string.Empty;
        private Encoding? _encoding;

        public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? xmlLang, XmlSpace xmlSpace, Encoding? enc)
        : this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace, enc)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? docTypeName,
                  string? pubId, string? sysId, string? internalSubset, string? baseURI,
                  string? xmlLang, XmlSpace xmlSpace)
        : this(nt, nsMgr, docTypeName, pubId, sysId, internalSubset, baseURI, xmlLang, xmlSpace, null)
        {
            // Intentionally Empty
        }

        public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? docTypeName,
                          string? pubId, string? sysId, string? internalSubset, string? baseURI,
                          string? xmlLang, XmlSpace xmlSpace, Encoding? enc)
        {
            if (nsMgr != null)
            {
                if (nt == null)
                {
                    _nt = nsMgr.NameTable;
                }
                else
                {
                    if ((object)nt != (object?)nsMgr.NameTable)
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
            _docTypeName = docTypeName ?? string.Empty;
            _pubId = pubId ?? string.Empty;
            _sysId = sysId ?? string.Empty;
            _internalSubset = internalSubset ?? string.Empty;
            _baseURI = baseURI ?? string.Empty;
            _xmlLang = xmlLang ?? string.Empty;
            _xmlSpace = xmlSpace;
            _encoding = enc;
        }

        public XmlNameTable? NameTable
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

        public XmlNamespaceManager? NamespaceManager
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

        [AllowNull]
        public string DocTypeName
        {
            get
            {
                return _docTypeName;
            }
            set
            {
                _docTypeName = value ?? string.Empty;
            }
        }

        [AllowNull]
        public string PublicId
        {
            get
            {
                return _pubId;
            }
            set
            {
                _pubId = value ?? string.Empty;
            }
        }

        [AllowNull]
        public string SystemId
        {
            get
            {
                return _sysId;
            }
            set
            {
                _sysId = value ?? string.Empty;
            }
        }

        [AllowNull]
        public string BaseURI
        {
            get
            {
                return _baseURI;
            }
            set
            {
                _baseURI = value ?? string.Empty;
            }
        }

        [AllowNull]
        public string InternalSubset
        {
            get
            {
                return _internalSubset;
            }
            set
            {
                _internalSubset = value ?? string.Empty;
            }
        }

        [AllowNull]
        public string XmlLang
        {
            get
            {
                return _xmlLang;
            }
            set
            {
                _xmlLang = value ?? string.Empty;
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

        public Encoding? Encoding
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
