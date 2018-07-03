// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Schema;

namespace System.Xml
{
    internal sealed partial class XmlSqlBinaryReader : XmlReader, IXmlNamespaceResolver
    {
        internal static readonly Type TypeOfObject = typeof(object);
        internal static readonly Type TypeOfString = typeof(string);

        private static volatile Type[] s_tokenTypeMap = null;

        private static byte[] s_xsdKatmaiTimeScaleToValueLengthMap = new byte[8] {
        // length scale
            3, // 0
            3, // 1
            3, // 2
            4, // 3
            4, // 4
            5, // 5
            5, // 6
            5, // 7
        };

        private enum ScanState
        {
            Doc = 0,
            XmlText = 1,
            Attr = 2,
            AttrVal = 3,
            AttrValPseudoValue = 4,
            Init = 5,
            Error = 6,
            EOF = 7,
            Closed = 8
        }

        private static ReadState[] s_scanState2ReadState = {
            ReadState.Interactive,
            ReadState.Interactive,
            ReadState.Interactive,
            ReadState.Interactive,
            ReadState.Interactive,
            ReadState.Initial,
            ReadState.Error,
            ReadState.EndOfFile,
            ReadState.Closed
        };

        // Note: also used by XmlBinaryWriter
        internal struct QName
        {
            public string prefix;
            public string localname;
            public string namespaceUri;

            public QName(string prefix, string lname, string nsUri)
            {
                this.prefix = prefix; this.localname = lname; this.namespaceUri = nsUri;
            }
            public void Set(string prefix, string lname, string nsUri)
            {
                this.prefix = prefix; this.localname = lname; this.namespaceUri = nsUri;
            }

            public void Clear()
            {
                this.prefix = this.localname = this.namespaceUri = string.Empty;
            }

            public bool MatchNs(string lname, string nsUri)
            {
                return lname == this.localname && nsUri == this.namespaceUri;
            }
            public bool MatchPrefix(string prefix, string lname)
            {
                return lname == this.localname && prefix == this.prefix;
            }

            public void CheckPrefixNS(string prefix, string namespaceUri)
            {
                if (this.prefix == prefix && this.namespaceUri != namespaceUri)
                    throw new XmlException(SR.XmlBinary_NoRemapPrefix, new string[] { prefix, this.namespaceUri, namespaceUri });
            }

            public override int GetHashCode()
            {
                return this.prefix.GetHashCode() ^ this.localname.GetHashCode();
            }

            public int GetNSHashCode(SecureStringHasher hasher)
            {
                return hasher.GetHashCode(this.namespaceUri) ^ hasher.GetHashCode(this.localname);
            }


            public override bool Equals(object other)
            {
                if (other is QName)
                {
                    QName that = (QName)other;
                    return this == that;
                }
                return false;
            }

            public override string ToString()
            {
                if (prefix.Length == 0)
                    return this.localname;
                else
                    return this.prefix + ":" + this.localname;
            }

            public static bool operator ==(QName a, QName b)
            {
                return ((a.prefix == b.prefix)
                    && (a.localname == b.localname)
                    && (a.namespaceUri == b.namespaceUri));
            }

            public static bool operator !=(QName a, QName b)
            {
                return !(a == b);
            }
        };

        private struct ElemInfo
        {
            public QName name;
            public string xmlLang;
            public XmlSpace xmlSpace;
            public bool xmlspacePreserve;
            public NamespaceDecl nsdecls;

            public void Set(QName name, bool xmlspacePreserve)
            {
                this.name = name;
                this.xmlLang = null;
                this.xmlSpace = XmlSpace.None;
                this.xmlspacePreserve = xmlspacePreserve;
            }
            public NamespaceDecl Clear()
            {
                NamespaceDecl nsdecls = this.nsdecls;
                this.nsdecls = null;
                return nsdecls;
            }
        };

        private struct AttrInfo
        {
            public QName name;
            public string val;
            public int contentPos;
            public int hashCode;
            public int prevHash;

            public void Set(QName n, string v)
            {
                this.name = n;
                this.val = v;
                this.contentPos = 0;
                this.hashCode = 0;
                this.prevHash = 0;
            }
            public void Set(QName n, int pos)
            {
                this.name = n;
                this.val = null;
                this.contentPos = pos;
                this.hashCode = 0;
                this.prevHash = 0;
            }

            public void GetLocalnameAndNamespaceUri(out string localname, out string namespaceUri)
            {
                localname = this.name.localname;
                namespaceUri = this.name.namespaceUri;
            }

            public int GetLocalnameAndNamespaceUriAndHash(SecureStringHasher hasher, out string localname, out string namespaceUri)
            {
                localname = this.name.localname;
                namespaceUri = this.name.namespaceUri;
                return this.hashCode = this.name.GetNSHashCode(hasher);
            }

            public bool MatchNS(string localname, string namespaceUri)
            {
                return this.name.MatchNs(localname, namespaceUri);
            }

            public bool MatchHashNS(int hash, string localname, string namespaceUri)
            {
                return this.hashCode == hash && this.name.MatchNs(localname, namespaceUri);
            }

            public void AdjustPosition(int adj)
            {
                if (this.contentPos != 0)
                    this.contentPos += adj;
            }
        }

        private class NamespaceDecl
        {
            public string prefix;
            public string uri;
            public NamespaceDecl scopeLink;
            public NamespaceDecl prevLink;
            public int scope;
            public bool implied;

            public NamespaceDecl(string prefix, string nsuri,
                                NamespaceDecl nextInScope, NamespaceDecl prevDecl,
                                int scope, bool implied)
            {
                this.prefix = prefix;
                this.uri = nsuri;
                this.scopeLink = nextInScope;
                this.prevLink = prevDecl;
                this.scope = scope;
                this.implied = implied;
            }
        }

        // symbol and qname tables
        private struct SymbolTables
        {
            public string[] symtable;
            public int symCount;
            public QName[] qnametable;
            public int qnameCount;
            public void Init()
            {
                this.symtable = new string[64];
                this.qnametable = new QName[16];
                this.symtable[0] = string.Empty;
                this.symCount = 1;
                this.qnameCount = 1;
            }
        }
        private class NestedBinXml
        {
            public SymbolTables symbolTables;
            public int docState;
            public NestedBinXml next;
            public NestedBinXml(SymbolTables symbolTables, int docState, NestedBinXml next)
            {
                this.symbolTables = symbolTables;
                this.docState = docState;
                this.next = next;
            }
        }

        // input data
        private Stream _inStrm;
        private byte[] _data;
        private int _pos;
        private int _mark;
        private int _end;
        private long _offset; // how much read and shift out of buffer
        private bool _eof;
        private bool _sniffed;
        private bool _isEmpty; // short-tag element start tag

        private int _docState; // 0=>auto, 1=>doc/pre-dtd, 2=>doc/pre-elem, 3=>doc/instance -1=>doc/post-elem, 9=>frag

        // symbol and qname tables
        private SymbolTables _symbolTables;

        private XmlNameTable _xnt;
        private bool _xntFromSettings;
        private string _xml;
        private string _xmlns;
        private string _nsxmlns;

        // base uri...
        private string _baseUri;

        // current parse state
        private ScanState _state;
        private XmlNodeType _nodetype;
        private BinXmlToken _token;
        // current attribute
        private int _attrIndex;
        // index of current qname
        private QName _qnameOther;
        // saved qname of element (for MoveToElement)
        private QName _qnameElement;
        private XmlNodeType _parentNodeType; // use for MoveToElement()
        // stack of current open element tags
        private ElemInfo[] _elementStack;
        private int _elemDepth;
        // current attributes
        private AttrInfo[] _attributes;
        private int[] _attrHashTbl;
        private int _attrCount;
        private int _posAfterAttrs;
        // xml:space
        private bool _xmlspacePreserve;
        // position/parse info for current typed token
        private int _tokLen;
        private int _tokDataPos;
        private bool _hasTypedValue;
        private System.Type _valueType;
        // if it is a simple string value, we cache it
        private string _stringValue;
        // hashtable of current namespaces
        private Dictionary<string, NamespaceDecl> _namespaces;
        //Hashtable namespaces;
        // linked list of pushed nametables (to support nested binary-xml documents)
        private NestedBinXml _prevNameInfo;
        // XmlTextReader to handle embeded text blocks
        private XmlReader _textXmlReader;
        // close input flag
        private bool _closeInput;

        private bool _checkCharacters;
        private bool _ignoreWhitespace;
        private bool _ignorePIs;
        private bool _ignoreComments;
        private DtdProcessing _dtdProcessing;

        private SecureStringHasher _hasher;
        private XmlCharType _xmlCharType;
        private Encoding _unicode;

        // current version of the protocol
        private byte _version;

        public XmlSqlBinaryReader(System.IO.Stream stream, byte[] data, int len, string baseUri, bool closeInput, XmlReaderSettings settings)
        {
            _unicode = System.Text.Encoding.Unicode;
            _xmlCharType = XmlCharType.Instance;

            _xnt = settings.NameTable;
            if (_xnt == null)
            {
                _xnt = new NameTable();
                _xntFromSettings = false;
            }
            else
            {
                _xntFromSettings = true;
            }
            _xml = _xnt.Add("xml");
            _xmlns = _xnt.Add("xmlns");
            _nsxmlns = _xnt.Add(XmlReservedNs.NsXmlNs);
            _baseUri = baseUri;
            _state = ScanState.Init;
            _nodetype = XmlNodeType.None;
            _token = BinXmlToken.Error;
            _elementStack = new ElemInfo[16];
            //this.elemDepth = 0;
            _attributes = new AttrInfo[8];
            _attrHashTbl = new int[8];
            //this.attrCount = 0;
            //this.attrIndex = 0;
            _symbolTables.Init();
            _qnameOther.Clear();
            _qnameElement.Clear();
            _xmlspacePreserve = false;
            _hasher = new SecureStringHasher();
            _namespaces = new Dictionary<string, NamespaceDecl>(_hasher);
            AddInitNamespace(string.Empty, string.Empty);
            AddInitNamespace(_xml, _xnt.Add(XmlReservedNs.NsXml));
            AddInitNamespace(_xmlns, _nsxmlns);
            _valueType = TypeOfString;
            // init buffer position, etc
            _inStrm = stream;
            if (data != null)
            {
                Debug.Assert(len >= 2 && (data[0] == 0xdf && data[1] == 0xff));
                _data = data;
                _end = len;
                _pos = 2;
                _sniffed = true;
            }
            else
            {
                _data = new byte[XmlReader.DefaultBufferSize];
                _end = stream.Read(_data, 0, XmlReader.DefaultBufferSize);
                _pos = 0;
                _sniffed = false;
            }

            _mark = -1;
            _eof = (0 == _end);
            _offset = 0;
            _closeInput = closeInput;
            switch (settings.ConformanceLevel)
            {
                case ConformanceLevel.Auto:
                    _docState = 0; break;
                case ConformanceLevel.Fragment:
                    _docState = 9; break;
                case ConformanceLevel.Document:
                    _docState = 1; break;
            }
            _checkCharacters = settings.CheckCharacters;
            _dtdProcessing = settings.DtdProcessing;
            _ignoreWhitespace = settings.IgnoreWhitespace;
            _ignorePIs = settings.IgnoreProcessingInstructions;
            _ignoreComments = settings.IgnoreComments;

            if (s_tokenTypeMap == null)
                GenerateTokenTypeMap();
        }

        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                if (_xntFromSettings)
                {
                    settings.NameTable = _xnt;
                }
                // 0=>auto, 1=>doc/pre-dtd, 2=>doc/pre-elem, 3=>doc/instance -1=>doc/post-elem, 9=>frag
                switch (_docState)
                {
                    case 0:
                        settings.ConformanceLevel = ConformanceLevel.Auto; break;
                    case 9:
                        settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                    default:
                        settings.ConformanceLevel = ConformanceLevel.Document; break;
                }
                settings.CheckCharacters = _checkCharacters;
                settings.IgnoreWhitespace = _ignoreWhitespace;
                settings.IgnoreProcessingInstructions = _ignorePIs;
                settings.IgnoreComments = _ignoreComments;
                settings.DtdProcessing = _dtdProcessing;
                settings.CloseInput = _closeInput;

                settings.ReadOnly = true;
                return settings;
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                return _nodetype;
            }
        }

        public override string LocalName
        {
            get
            {
                return _qnameOther.localname;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return _qnameOther.namespaceUri;
            }
        }

        public override string Prefix
        {
            get
            {
                return _qnameOther.prefix;
            }
        }

        public override bool HasValue
        {
            get
            {
                if (ScanState.XmlText == _state)
                    return _textXmlReader.HasValue;
                else
                    return XmlReader.HasValueInternal(_nodetype);
            }
        }

        public override string Value
        {
            get
            {
                if (null != _stringValue)
                    return _stringValue;
                switch (_state)
                {
                    case ScanState.Doc:
                        switch (_nodetype)
                        {
                            case XmlNodeType.DocumentType:
                            case XmlNodeType.ProcessingInstruction:
                            case XmlNodeType.Comment:
                                return _stringValue = GetString(_tokDataPos, _tokLen);

                            case XmlNodeType.CDATA:
                                return _stringValue = CDATAValue();

                            case XmlNodeType.XmlDeclaration:
                                return _stringValue = XmlDeclValue();

                            case XmlNodeType.Text:
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                return _stringValue = ValueAsString(_token);
                        }
                        break;

                    case ScanState.XmlText:
                        return _textXmlReader.Value;

                    case ScanState.Attr:
                    case ScanState.AttrValPseudoValue:
                        return _stringValue = GetAttributeText(_attrIndex - 1);

                    case ScanState.AttrVal:
                        return _stringValue = ValueAsString(_token);
                }
                return string.Empty;
            }
        }

        public override int Depth
        {
            get
            {
                int adj = 0;
                switch (_state)
                {
                    case ScanState.Doc:
                        if (_nodetype == XmlNodeType.Element
                            || _nodetype == XmlNodeType.EndElement)
                            adj = -1;
                        break;

                    case ScanState.XmlText:
                        adj = _textXmlReader.Depth;
                        break;

                    case ScanState.Attr:
                        if (_parentNodeType != XmlNodeType.Element)
                            adj = 1;
                        break;
                    case ScanState.AttrVal:
                    case ScanState.AttrValPseudoValue:
                        if (_parentNodeType != XmlNodeType.Element)
                            adj = 1;
                        adj += 1;
                        break;
                    default:
                        return 0;
                }
                return _elemDepth + adj;
            }
        }

        public override string BaseURI
        {
            get
            {
                return _baseUri;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                switch (_state)
                {
                    case ScanState.Doc:
                    case ScanState.XmlText:
                        return _isEmpty;
                    default:
                        return false;
                }
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                if (ScanState.XmlText != _state)
                {
                    for (int i = _elemDepth; i >= 0; i--)
                    {
                        XmlSpace xs = _elementStack[i].xmlSpace;
                        if (xs != XmlSpace.None)
                            return xs;
                    }
                    return XmlSpace.None;
                }
                else
                {
                    return _textXmlReader.XmlSpace;
                }
            }
        }

        public override string XmlLang
        {
            get
            {
                if (ScanState.XmlText != _state)
                {
                    for (int i = _elemDepth; i >= 0; i--)
                    {
                        string xl = _elementStack[i].xmlLang;
                        if (null != xl)
                            return xl;
                    }
                    return string.Empty;
                }
                else
                {
                    return _textXmlReader.XmlLang;
                }
            }
        }

        public override System.Type ValueType
        {
            get
            {
                return _valueType;
            }
        }

        public override int AttributeCount
        {
            get
            {
                switch (_state)
                {
                    case ScanState.Doc:
                    // for compatibility with XmlTextReader
                    //  we return the attribute count for the element
                    //  when positioned on an attribute under that
                    //  element...
                    case ScanState.Attr:
                    case ScanState.AttrVal:
                    case ScanState.AttrValPseudoValue:
                        return _attrCount;
                    case ScanState.XmlText:
                        return _textXmlReader.AttributeCount;
                    default:
                        return 0;
                }
            }
        }

        public override string GetAttribute(string name, string ns)
        {
            if (ScanState.XmlText == _state)
            {
                return _textXmlReader.GetAttribute(name, ns);
            }
            else
            {
                if (null == name)
                    throw new ArgumentNullException(nameof(name));
                if (null == ns)
                    ns = string.Empty;
                int index = LocateAttribute(name, ns);
                if (-1 == index)
                    return null;
                return GetAttribute(index);
            }
        }

        public override string GetAttribute(string name)
        {
            if (ScanState.XmlText == _state)
            {
                return _textXmlReader.GetAttribute(name);
            }
            else
            {
                int index = LocateAttribute(name);
                if (-1 == index)
                    return null;
                return GetAttribute(index);
            }
        }

        public override string GetAttribute(int i)
        {
            if (ScanState.XmlText == _state)
            {
                return _textXmlReader.GetAttribute(i);
            }
            else
            {
                if (i < 0 || i >= _attrCount)
                    throw new ArgumentOutOfRangeException(nameof(i));
                return GetAttributeText(i);
            }
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            if (ScanState.XmlText == _state)
            {
                return UpdateFromTextReader(_textXmlReader.MoveToAttribute(name, ns));
            }
            else
            {
                if (null == name)
                    throw new ArgumentNullException(nameof(name));
                if (null == ns)
                    ns = string.Empty;
                int index = LocateAttribute(name, ns);
                if ((-1 != index) && (_state < ScanState.Init))
                {
                    PositionOnAttribute(index + 1);
                    return true;
                }
                return false;
            }
        }

        public override bool MoveToAttribute(string name)
        {
            if (ScanState.XmlText == _state)
            {
                return UpdateFromTextReader(_textXmlReader.MoveToAttribute(name));
            }
            else
            {
                int index = LocateAttribute(name);
                if ((-1 != index) && (_state < ScanState.Init))
                {
                    PositionOnAttribute(index + 1);
                    return true;
                }
                return false;
            }
        }

        public override void MoveToAttribute(int i)
        {
            if (ScanState.XmlText == _state)
            {
                _textXmlReader.MoveToAttribute(i);
                UpdateFromTextReader(true);
            }
            else
            {
                if (i < 0 || i >= _attrCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }
                PositionOnAttribute(i + 1);
            }
        }

        public override bool MoveToFirstAttribute()
        {
            if (ScanState.XmlText == _state)
            {
                return UpdateFromTextReader(_textXmlReader.MoveToFirstAttribute());
            }
            else
            {
                if (_attrCount == 0)
                    return false;
                // set up for walking attributes
                PositionOnAttribute(1);
                return true;
            }
        }

        public override bool MoveToNextAttribute()
        {
            switch (_state)
            {
                case ScanState.Doc:
                case ScanState.Attr:
                case ScanState.AttrVal:
                case ScanState.AttrValPseudoValue:
                    if (_attrIndex >= _attrCount)
                        return false;
                    PositionOnAttribute(++_attrIndex);
                    return true;

                case ScanState.XmlText:
                    return UpdateFromTextReader(_textXmlReader.MoveToNextAttribute());

                default:
                    return false;
            }
        }

        public override bool MoveToElement()
        {
            switch (_state)
            {
                case ScanState.Attr:
                case ScanState.AttrVal:
                case ScanState.AttrValPseudoValue:
                    _attrIndex = 0;
                    _qnameOther = _qnameElement;
                    if (XmlNodeType.Element == _parentNodeType)
                        _token = BinXmlToken.Element;
                    else if (XmlNodeType.XmlDeclaration == _parentNodeType)
                        _token = BinXmlToken.XmlDecl;
                    else if (XmlNodeType.DocumentType == _parentNodeType)
                        _token = BinXmlToken.DocType;
                    else
                        Debug.Fail("Unexpected parent NodeType");
                    _nodetype = _parentNodeType;
                    _state = ScanState.Doc;
                    _pos = _posAfterAttrs;
                    _stringValue = null;
                    return true;

                case ScanState.XmlText:
                    return UpdateFromTextReader(_textXmlReader.MoveToElement());

                default:
                    return false;
            }
        }

        public override bool EOF
        {
            get
            {
                return _state == ScanState.EOF;
            }
        }

        public override bool ReadAttributeValue()
        {
            _stringValue = null;
            switch (_state)
            {
                case ScanState.Attr:
                    if (null == _attributes[_attrIndex - 1].val)
                    {
                        _pos = _attributes[_attrIndex - 1].contentPos;
                        BinXmlToken tok = RescanNextToken();
                        if (BinXmlToken.Attr == tok || BinXmlToken.EndAttrs == tok)
                        {
                            return false;
                        }
                        _token = tok;
                        ReScanOverValue(tok);
                        _valueType = GetValueType(tok);
                        _state = ScanState.AttrVal;
                    }
                    else
                    {
                        _token = BinXmlToken.Error;
                        _valueType = TypeOfString;
                        _state = ScanState.AttrValPseudoValue;
                    }
                    _qnameOther.Clear();
                    _nodetype = XmlNodeType.Text;
                    return true;

                case ScanState.AttrVal:
                    return false;

                case ScanState.XmlText:
                    return UpdateFromTextReader(_textXmlReader.ReadAttributeValue());

                default:
                    return false;
            }
        }

        public override void Close()
        {
            _state = ScanState.Closed;
            _nodetype = XmlNodeType.None;
            _token = BinXmlToken.Error;
            _stringValue = null;
            if (null != _textXmlReader)
            {
                _textXmlReader.Close();
                _textXmlReader = null;
            }
            if (null != _inStrm && _closeInput)
                _inStrm.Dispose();
            _inStrm = null;
            _pos = _end = 0;
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return _xnt;
            }
        }

        public override string LookupNamespace(string prefix)
        {
            if (ScanState.XmlText == _state)
                return _textXmlReader.LookupNamespace(prefix);
            NamespaceDecl decl;
            if (prefix != null && _namespaces.TryGetValue(prefix, out decl))
            {
                Debug.Assert(decl != null);
                return decl.uri;
            }
            return null;
        }

        public override void ResolveEntity()
        {
            throw new NotSupportedException();
        }

        public override ReadState ReadState
        {
            get
            {
                return s_scanState2ReadState[(int)_state];
            }
        }

        public override bool Read()
        {
            try
            {
                switch (_state)
                {
                    case ScanState.Init:
                        return ReadInit(false);

                    case ScanState.Doc:
                        return ReadDoc();

                    case ScanState.XmlText:
                        if (_textXmlReader.Read())
                        {
                            return UpdateFromTextReader(true);
                        }
                        _state = ScanState.Doc;
                        _nodetype = XmlNodeType.None;
                        _isEmpty = false;
                        goto case ScanState.Doc;

                    case ScanState.Attr:
                    case ScanState.AttrVal:
                    case ScanState.AttrValPseudoValue:
                        // clean up attribute stuff...
                        MoveToElement();
                        goto case ScanState.Doc;

                    default:
                        return false;
                }
            }
            catch (OverflowException e)
            {
                _state = ScanState.Error;
                throw new XmlException(e.Message, e);
            }
            catch
            {
                _state = ScanState.Error;
                throw;
            }
        }

        // Use default implementation of and ReadContentAsString and ReadElementContentAsString
        // (there is no benefit to providing a custom version)
        // public override bool ReadElementContentAsString( string localName, string namespaceURI )
        // public override bool ReadElementContentAsString()
        // public override bool ReadContentAsString()

        // Do setup work for ReadContentAsXXX methods
        // If ready for a typed value read, returns true, otherwise returns
        //  false to indicate caller should ball back to XmlReader.ReadContentAsXXX
        // Special-Case: returns true and positioned on Element or EndElem to force parse of empty-string
        private bool SetupContentAsXXX(string name)
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(name);
            }
            switch (_state)
            {
                case ScanState.Doc:
                    if (this.NodeType == XmlNodeType.EndElement)
                        return true;
                    if (this.NodeType == XmlNodeType.ProcessingInstruction || this.NodeType == XmlNodeType.Comment)
                    {
                        while (Read() && (this.NodeType == XmlNodeType.ProcessingInstruction || this.NodeType == XmlNodeType.Comment))
                            ;
                        if (this.NodeType == XmlNodeType.EndElement)
                            return true;
                    }
                    if (_hasTypedValue)
                    {
                        return true;
                    }
                    break;
                case ScanState.Attr:
                    _pos = _attributes[_attrIndex - 1].contentPos;
                    BinXmlToken token = RescanNextToken();
                    if (BinXmlToken.Attr == token || BinXmlToken.EndAttrs == token)
                        break;
                    _token = token;
                    ReScanOverValue(token);
                    return true;
                case ScanState.AttrVal:
                    return true;
                default:
                    break;
            }
            return false;
        }

        private int FinishContentAsXXX(int origPos)
        {
            if (_state == ScanState.Doc)
            {
                // if we are already on a tag, then don't move
                if (this.NodeType != XmlNodeType.Element && this.NodeType != XmlNodeType.EndElement)
                {
                // advance over PIs and Comments
                Loop:
                    if (Read())
                    {
                        switch (this.NodeType)
                        {
                            case XmlNodeType.ProcessingInstruction:
                            case XmlNodeType.Comment:
                                goto Loop;

                            case XmlNodeType.Element:
                            case XmlNodeType.EndElement:
                                break;

                            default:
                                throw ThrowNotSupported(SR.XmlBinary_ListsOfValuesNotSupported);
                        }
                    }
                }
                return _pos;
            }
            return origPos;
        }

        public override bool ReadContentAsBoolean()
        {
            int origPos = _pos;
            bool value = false;
            try
            {
                if (SetupContentAsXXX("ReadContentAsBoolean"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.XSD_BOOLEAN:
                                value = 0 != _data[_tokDataPos];
                                break;

                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Boolean"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToBoolean(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, null);
                    }
                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsBoolean();
        }

        public override DateTime ReadContentAsDateTime()
        {
            int origPos = _pos;
            DateTime value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsDateTime"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                                value = ValueAsDateTime();
                                break;

                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "DateTime"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToDateTime(string.Empty, XmlDateTimeSerializationMode.RoundtripKind);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsDateTime();
        }

        public override double ReadContentAsDouble()
        {
            int origPos = _pos;
            double value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsDouble"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                                value = ValueAsDouble();
                                break;

                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Double"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToDouble(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsDouble();
        }

        public override float ReadContentAsFloat()
        {
            int origPos = _pos;
            float value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsFloat"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                                value = checked(((float)ValueAsDouble()));
                                break;

                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Float"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToSingle(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsFloat();
        }

        public override decimal ReadContentAsDecimal()
        {
            int origPos = _pos;
            decimal value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsDecimal"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                                value = ValueAsDecimal();
                                break;

                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Decimal"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToDecimal(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsDecimal();
        }

        public override int ReadContentAsInt()
        {
            int origPos = _pos;
            int value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsInt"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                                value = checked((int)ValueAsLong());
                                break;

                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Int32"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToInt32(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int32", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int32", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int32", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsInt();
        }

        public override long ReadContentAsLong()
        {
            int origPos = _pos;
            long value;
            try
            {
                if (SetupContentAsXXX("ReadContentAsLong"))
                {
                    try
                    {
                        switch (_token)
                        {
                            case BinXmlToken.SQL_BIT:
                            case BinXmlToken.SQL_TINYINT:
                            case BinXmlToken.SQL_SMALLINT:
                            case BinXmlToken.SQL_INT:
                            case BinXmlToken.SQL_BIGINT:
                            case BinXmlToken.SQL_MONEY:
                            case BinXmlToken.SQL_SMALLMONEY:
                            case BinXmlToken.SQL_DECIMAL:
                            case BinXmlToken.SQL_NUMERIC:
                            case BinXmlToken.XSD_DECIMAL:
                            case BinXmlToken.XSD_BYTE:
                            case BinXmlToken.XSD_UNSIGNEDSHORT:
                            case BinXmlToken.XSD_UNSIGNEDINT:
                            case BinXmlToken.XSD_UNSIGNEDLONG:
                                value = ValueAsLong();
                                break;

                            case BinXmlToken.SQL_REAL:
                            case BinXmlToken.SQL_FLOAT:
                            case BinXmlToken.SQL_DATETIME:
                            case BinXmlToken.SQL_SMALLDATETIME:
                            case BinXmlToken.SQL_UUID:
                            case BinXmlToken.SQL_VARBINARY:
                            case BinXmlToken.SQL_BINARY:
                            case BinXmlToken.SQL_IMAGE:
                            case BinXmlToken.SQL_UDT:
                            case BinXmlToken.XSD_KATMAI_DATE:
                            case BinXmlToken.XSD_KATMAI_DATETIME:
                            case BinXmlToken.XSD_KATMAI_TIME:
                            case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                            case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                            case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                            case BinXmlToken.XSD_BINHEX:
                            case BinXmlToken.XSD_BASE64:
                            case BinXmlToken.XSD_BOOLEAN:
                            case BinXmlToken.XSD_TIME:
                            case BinXmlToken.XSD_DATETIME:
                            case BinXmlToken.XSD_DATE:
                            case BinXmlToken.XSD_QNAME:
                                throw new InvalidCastException(SR.Format(SR.XmlBinary_CastNotSupported, _token, "Int64"));

                            case BinXmlToken.SQL_CHAR:
                            case BinXmlToken.SQL_VARCHAR:
                            case BinXmlToken.SQL_TEXT:
                            case BinXmlToken.SQL_NCHAR:
                            case BinXmlToken.SQL_NVARCHAR:
                            case BinXmlToken.SQL_NTEXT:
                                goto Fallback;

                            case BinXmlToken.Element:
                            case BinXmlToken.EndElem:
                                return XmlConvert.ToInt64(string.Empty);

                            default:
                                Debug.Fail("should never happen");
                                goto Fallback;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int64", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int64", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int64", e, null);
                    }

                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
        Fallback:
            return base.ReadContentAsLong();
        }

        public override object ReadContentAsObject()
        {
            int origPos = _pos;
            try
            {
                if (SetupContentAsXXX("ReadContentAsObject"))
                {
                    object value;
                    try
                    {
                        if (this.NodeType == XmlNodeType.Element || this.NodeType == XmlNodeType.EndElement)
                            value = string.Empty;
                        else
                            value = this.ValueAsObject(_token, false);
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Object", e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Object", e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, "Object", e, null);
                    }
                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
            //Fallback:
            return base.ReadContentAsObject();
        }

        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            int origPos = _pos;
            try
            {
                if (SetupContentAsXXX("ReadContentAs"))
                {
                    object value;
                    try
                    {
                        if (this.NodeType == XmlNodeType.Element || this.NodeType == XmlNodeType.EndElement)
                        {
                            value = string.Empty;
                        }
                        else if (returnType == this.ValueType || returnType == typeof(object))
                        {
                            value = this.ValueAsObject(_token, false);
                        }
                        else
                        {
                            value = this.ValueAs(_token, returnType, namespaceResolver);
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, null);
                    }
                    catch (FormatException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, null);
                    }
                    catch (OverflowException e)
                    {
                        throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, null);
                    }
                    origPos = FinishContentAsXXX(origPos);
                    return value;
                }
            }
            finally
            {
                _pos = origPos;
            }
            return base.ReadContentAs(returnType, namespaceResolver);
        }

        //////////
        // IXmlNamespaceResolver

        System.Collections.Generic.IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            if (ScanState.XmlText == _state)
            {
                IXmlNamespaceResolver resolver = (IXmlNamespaceResolver)_textXmlReader;
                return resolver.GetNamespacesInScope(scope);
            }
            else
            {
                Dictionary<string, string> nstable = new Dictionary<string, string>();
                if (XmlNamespaceScope.Local == scope)
                {
                    // are we even inside an element? (depth==0 is where we have xml, and xmlns declared...)
                    if (_elemDepth > 0)
                    {
                        NamespaceDecl nsdecl = _elementStack[_elemDepth].nsdecls;
                        while (null != nsdecl)
                        {
                            nstable.Add(nsdecl.prefix, nsdecl.uri);
                            nsdecl = nsdecl.scopeLink;
                        }
                    }
                }
                else
                {
                    foreach (NamespaceDecl nsdecl in _namespaces.Values)
                    {
                        // don't add predefined decls unless scope == all, then only add 'xml'                       
                        if (nsdecl.scope != -1 || (XmlNamespaceScope.All == scope && "xml" == nsdecl.prefix))
                        {
                            // xmlns="" only ever reported via scope==local
                            if (nsdecl.prefix.Length > 0 || nsdecl.uri.Length > 0)
                                nstable.Add(nsdecl.prefix, nsdecl.uri);
                        }
                    }
                }
                return nstable;
            }
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            if (ScanState.XmlText == _state)
            {
                IXmlNamespaceResolver resolver = (IXmlNamespaceResolver)_textXmlReader;
                return resolver.LookupPrefix(namespaceName);
            }
            else
            {
                if (null == namespaceName)
                    return null;

                namespaceName = _xnt.Get(namespaceName);
                if (null == namespaceName)
                    return null;

                for (int i = _elemDepth; i >= 0; i--)
                {
                    NamespaceDecl nsdecl = _elementStack[i].nsdecls;
                    while (null != nsdecl)
                    {
                        if ((object)nsdecl.uri == (object)namespaceName)
                            return nsdecl.prefix;
                        nsdecl = nsdecl.scopeLink;
                    }
                }
                return null;
            }
        }

        //////////
        // Internal implementation methods

        private void VerifyVersion(int requiredVersion, BinXmlToken token)
        {
            if (_version < requiredVersion)
            {
                throw ThrowUnexpectedToken(token);
            }
        }

        private void AddInitNamespace(string prefix, string uri)
        {
            NamespaceDecl nsdecl = new NamespaceDecl(prefix, uri, _elementStack[0].nsdecls, null, -1, true);
            _elementStack[0].nsdecls = nsdecl;
            _namespaces.Add(prefix, nsdecl);
        }

        private void AddName()
        {
            string txt = ParseText();
            int symNum = _symbolTables.symCount++;
            string[] symtable = _symbolTables.symtable;
            if (symNum == symtable.Length)
            {
                string[] n = new string[checked(symNum * 2)];
                System.Array.Copy(symtable, 0, n, 0, symNum);
                _symbolTables.symtable = symtable = n;
            }
            symtable[symNum] = _xnt.Add(txt);
        }

        private void AddQName()
        {
            int nsUri = ReadNameRef();
            int prefix = ReadNameRef();
            int lname = ReadNameRef();
            int qnameNum = _symbolTables.qnameCount++;
            QName[] qnametable = _symbolTables.qnametable;
            if (qnameNum == qnametable.Length)
            {
                QName[] n = new QName[checked(qnameNum * 2)];
                System.Array.Copy(qnametable, 0, n, 0, qnameNum);
                _symbolTables.qnametable = qnametable = n;
            }
            string[] symtable = _symbolTables.symtable;
            string prefixStr = symtable[prefix];
            string lnameStr;
            string nsUriStr;
            // xmlns attributes are encodes differently...
            if (lname == 0)
            { // xmlns attribute
                // for some reason, sqlserver sometimes generates these...
                if (prefix == 0 && nsUri == 0)
                    return;
                // it is a real namespace decl, make sure it looks valid
                if (!prefixStr.StartsWith("xmlns", StringComparison.Ordinal))
                    goto BadDecl;
                if (5 < prefixStr.Length)
                {
                    if (6 == prefixStr.Length || ':' != prefixStr[5])
                        goto BadDecl;
                    lnameStr = _xnt.Add(prefixStr.Substring(6));
                    prefixStr = _xmlns;
                }
                else
                {
                    lnameStr = prefixStr;
                    prefixStr = string.Empty;
                }
                nsUriStr = _nsxmlns;
            }
            else
            {
                lnameStr = symtable[lname];
                nsUriStr = symtable[nsUri];
            }
            qnametable[qnameNum].Set(prefixStr, lnameStr, nsUriStr);
            return;
        BadDecl:
            throw new XmlException(SR.Xml_BadNamespaceDecl, (string[])null);
        }

        private void NameFlush()
        {
            _symbolTables.symCount = _symbolTables.qnameCount = 1;
            Array.Clear(_symbolTables.symtable, 1, _symbolTables.symtable.Length - 1);
            Array.Clear(_symbolTables.qnametable, 0, _symbolTables.qnametable.Length);
        }

        private void SkipExtn()
        {
            int cb = ParseMB32();
            checked { _pos += cb; }
            Fill(-1);
        }

        private int ReadQNameRef()
        {
            int nameNum = ParseMB32();
            if (nameNum < 0 || nameNum >= _symbolTables.qnameCount)
                throw new XmlException(SR.XmlBin_InvalidQNameID, string.Empty);
            return nameNum;
        }

        private int ReadNameRef()
        {
            int nameNum = ParseMB32();
            if (nameNum < 0 || nameNum >= _symbolTables.symCount)
                throw new XmlException(SR.XmlBin_InvalidQNameID, string.Empty);
            return nameNum;
        }

        // pull more data from input stream
        private bool FillAllowEOF()
        {
            if (_eof)
                return false;
            byte[] data = _data;
            int pos = _pos;
            int mark = _mark;
            int end = _end;
            if (mark == -1)
            {
                mark = pos;
            }
            if (mark >= 0 && mark < end)
            {
                Debug.Assert(_mark <= _end, "Mark should never be past End");
                Debug.Assert(_mark <= _pos, "Mark should never be after Pos");
                int cbKeep = end - mark;
                if (cbKeep > 7 * (data.Length / 8))
                {
                    // grow buffer
                    byte[] newdata = new byte[checked(data.Length * 2)];
                    System.Array.Copy(data, mark, newdata, 0, cbKeep);
                    _data = data = newdata;
                }
                else
                {
                    System.Array.Copy(data, mark, data, 0, cbKeep);
                }
                pos -= mark;
                end -= mark;
                _tokDataPos -= mark;
                for (int i = 0; i < _attrCount; i++)
                {
                    _attributes[i].AdjustPosition(-mark);
                    // make sure it is still a valid range
                    Debug.Assert((_attributes[i].contentPos >= 0) && (_attributes[i].contentPos <= (end)));
                }
                _pos = pos;
                _mark = 0;
                _offset += mark;
            }
            else
            {
                Debug.Assert(_attrCount == 0);
                _pos -= end;
                _mark -= end;
                _offset += end;
                _tokDataPos -= end;
                end = 0;
            }
            int cbFill = data.Length - end;
            int cbRead = _inStrm.Read(data, end, cbFill);
            _end = end + cbRead;
            _eof = !(cbRead > 0);
            return (cbRead > 0);
        }

        // require must be < 1/8 buffer, or else Fill might not actually 
        // grab that much data
        private void Fill_(int require)
        {
            Debug.Assert((_pos + require) >= _end);
            while (FillAllowEOF() && ((_pos + require) >= _end))
                ;
            if ((_pos + require) >= _end)
                throw ThrowXmlException(SR.Xml_UnexpectedEOF1);
        }

        // inline the common case
        private void Fill(int require)
        {
            if ((_pos + require) >= _end)
                Fill_(require);
        }

        private byte ReadByte()
        {
            Fill(0);
            return _data[_pos++];
        }
        private ushort ReadUShort()
        {
            Fill(1);
            int pos = _pos; byte[] data = _data;
            ushort val = (ushort)(data[pos] + (data[pos + 1] << 8));
            _pos += 2;
            return val;
        }

        private int ParseMB32()
        {
            byte b = ReadByte();
            if (b > 127)
                return ParseMB32_(b);
            return b;
        }

        private int ParseMB32_(byte b)
        {
            uint u, t;
            u = (uint)b & (uint)0x7F;
            Debug.Assert(0 != (b & 0x80));
            b = ReadByte();
            t = (uint)b & (uint)0x7F;
            u = u + (t << 7);
            if (b > 127)
            {
                b = ReadByte();
                t = (uint)b & (uint)0x7F;
                u = u + (t << 14);
                if (b > 127)
                {
                    b = ReadByte();
                    t = (uint)b & (uint)0x7F;
                    u = u + (t << 21);
                    if (b > 127)
                    {
                        b = ReadByte();
                        // bottom 4 bits are all that are needed, 
                        // but we are mapping to 'int', which only
                        // actually has space for 3 more bits.
                        t = (uint)b & (uint)0x07;
                        if (b > 7)
                            throw ThrowXmlException(SR.XmlBinary_ValueTooBig);
                        u = u + (t << 28);
                    }
                }
            }
            return (int)u;
        }

        // this assumes that we have already ensured that all
        // necessary bytes are loaded in to the buffer
        private int ParseMB32(int pos)
        {
            uint u, t;
            byte[] data = _data;
            byte b = data[pos++];
            u = (uint)b & (uint)0x7F;
            if (b > 127)
            {
                b = data[pos++];
                t = (uint)b & (uint)0x7F;
                u = u + (t << 7);
                if (b > 127)
                {
                    b = data[pos++];
                    t = (uint)b & (uint)0x7F;
                    u = u + (t << 14);
                    if (b > 127)
                    {
                        b = data[pos++];
                        t = (uint)b & (uint)0x7F;
                        u = u + (t << 21);
                        if (b > 127)
                        {
                            b = data[pos++];
                            // last byte only has 4 significant digits
                            t = (uint)b & (uint)0x07;
                            if (b > 7)
                                throw ThrowXmlException(SR.XmlBinary_ValueTooBig);
                            u = u + (t << 28);
                        }
                    }
                }
            }
            return (int)u;
        }

        // we don't actually support MB64, since we use int for 
        // all our math anyway...
        private int ParseMB64()
        {
            byte b = ReadByte();
            if (b > 127)
                return ParseMB32_(b);
            return b;
        }

        private BinXmlToken PeekToken()
        {
            while ((_pos >= _end) && FillAllowEOF())
                ;
            if (_pos >= _end)
                return BinXmlToken.EOF;
            return (BinXmlToken)_data[_pos];
        }

        private BinXmlToken ReadToken()
        {
            while ((_pos >= _end) && FillAllowEOF())
                ;
            if (_pos >= _end)
                return BinXmlToken.EOF;
            return (BinXmlToken)_data[_pos++];
        }

        private BinXmlToken NextToken2(BinXmlToken token)
        {
            while (true)
            {
                switch (token)
                {
                    case BinXmlToken.Name:
                        AddName();
                        break;
                    case BinXmlToken.QName:
                        AddQName();
                        break;
                    case BinXmlToken.NmFlush:
                        NameFlush();
                        break;
                    case BinXmlToken.Extn:
                        SkipExtn();
                        break;
                    default:
                        return token;
                }
                token = ReadToken();
            }
        }

        private BinXmlToken NextToken1()
        {
            BinXmlToken token;
            int pos = _pos;
            if (pos >= _end)
                token = ReadToken();
            else
            {
                token = (BinXmlToken)_data[pos];
                _pos = pos + 1;
            }
            // BinXmlToken.Name = 0xF0
            // BinXmlToken.QName = 0xEF
            // BinXmlToken.Extn = 0xEA,
            // BinXmlToken.NmFlush = 0xE9,
            if (token >= BinXmlToken.NmFlush
                && token <= BinXmlToken.Name)
                return NextToken2(token);
            return token;
        }

        private BinXmlToken NextToken()
        {
            int pos = _pos;
            if (pos < _end)
            {
                BinXmlToken t = (BinXmlToken)_data[pos];
                if (!(t >= BinXmlToken.NmFlush && t <= BinXmlToken.Name))
                {
                    _pos = pos + 1;
                    return t;
                }
            }
            return NextToken1();
        }

        // peek next non-meta token
        private BinXmlToken PeekNextToken()
        {
            BinXmlToken token = NextToken();
            if (BinXmlToken.EOF != token)
                _pos--;
            return token;
        }

        // like NextToken() but meta-tokens are skipped (not reinterpreted)
        private BinXmlToken RescanNextToken()
        {
            BinXmlToken token;
            while (true)
            {
                token = ReadToken();
                switch (token)
                {
                    case BinXmlToken.Name:
                        {
                            int cb = ParseMB32();
                            checked { _pos += 2 * cb; }
                            break;
                        }
                    case BinXmlToken.QName:
                        ParseMB32();
                        ParseMB32();
                        ParseMB32();
                        break;
                    case BinXmlToken.Extn:
                        {
                            int cb = ParseMB32();
                            checked { _pos += cb; }
                            break;
                        }
                    case BinXmlToken.NmFlush:
                        break;
                    default:
                        return token;
                }
            }
        }

        private string ParseText()
        {
            int oldmark = _mark;
            try
            {
                if (oldmark < 0)
                    _mark = _pos;
                int cch, pos;
                cch = ScanText(out pos);
                return GetString(pos, cch);
            }
            finally
            {
                if (oldmark < 0)
                    _mark = -1;
            }
        }

        private int ScanText(out int start)
        {
            int cch = ParseMB32();
            int oldmark = _mark;
            int begin = _pos;
            checked { _pos += cch * 2; } // cch = num utf-16 chars
            if (_pos > _end)
                Fill(-1);
            // Fill call might have moved buffer
            start = begin - (oldmark - _mark);
            return cch;
        }

        private string GetString(int pos, int cch)
        {
            Debug.Assert(pos >= 0 && cch >= 0);
            if (checked(pos + (cch * 2)) > _end)
                throw new XmlException(SR.Xml_UnexpectedEOF1, (string[])null);
            if (cch == 0)
                return string.Empty;
            // GetStringUnaligned is _significantly_ faster than unicode.GetString()
            // but since IA64 doesn't support unaligned reads, we can't do it if
            // the address is not aligned properly.  Since the byte[] will be aligned,
            // we can detect address alignment my just looking at the offset
            if ((pos & 1) == 0)
                return GetStringAligned(_data, pos, cch);
            else
                return _unicode.GetString(_data, pos, checked(cch * 2));
        }

        private unsafe string GetStringAligned(byte[] data, int offset, int cch)
        {
            Debug.Assert((offset & 1) == 0);
            fixed (byte* pb = data)
            {
                char* p = (char*)(pb + offset);
                return new string(p, 0, cch);
            }
        }

        private string GetAttributeText(int i)
        {
            string val = _attributes[i].val;

            if (null != val)
                return val;
            else
            {
                int origPos = _pos;
                try
                {
                    _pos = _attributes[i].contentPos;
                    BinXmlToken token = RescanNextToken();
                    if (BinXmlToken.Attr == token || BinXmlToken.EndAttrs == token)
                    {
                        return "";
                    }
                    _token = token;
                    ReScanOverValue(token);
                    return ValueAsString(token);
                }
                finally
                {
                    _pos = origPos;
                }
            }
        }

        private int LocateAttribute(string name, string ns)
        {
            for (int i = 0; i < _attrCount; i++)
            {
                if (_attributes[i].name.MatchNs(name, ns))
                    return i;
            }

            return -1;
        }

        private int LocateAttribute(string name)
        {
            string prefix, lname;
            ValidateNames.SplitQName(name, out prefix, out lname);

            for (int i = 0; i < _attrCount; i++)
            {
                if (_attributes[i].name.MatchPrefix(prefix, lname))
                    return i;
            }

            return -1;
        }

        private void PositionOnAttribute(int i)
        {
            // save element's qname
            _attrIndex = i;
            _qnameOther = _attributes[i - 1].name;
            if (_state == ScanState.Doc)
            {
                _parentNodeType = _nodetype;
            }
            _token = BinXmlToken.Attr;
            _nodetype = XmlNodeType.Attribute;
            _state = ScanState.Attr;
            _valueType = TypeOfObject;
            _stringValue = null;
        }

        private void GrowElements()
        {
            int newcount = _elementStack.Length * 2;
            ElemInfo[] n = new ElemInfo[newcount];

            System.Array.Copy(_elementStack, 0, n, 0, _elementStack.Length);
            _elementStack = n;
        }

        private void GrowAttributes()
        {
            int newcount = _attributes.Length * 2;
            AttrInfo[] n = new AttrInfo[newcount];

            System.Array.Copy(_attributes, 0, n, 0, _attrCount);
            _attributes = n;
        }

        private void ClearAttributes()
        {
            if (_attrCount != 0)
                _attrCount = 0;
        }

        private void PushNamespace(string prefix, string ns, bool implied)
        {
            if (prefix == "xml")
                return;
            int elemDepth = _elemDepth;
            NamespaceDecl curDecl;
            _namespaces.TryGetValue(prefix, out curDecl);
            if (null != curDecl)
            {
                if (curDecl.uri == ns)
                {
                    // if we see the nsdecl after we saw the first reference in this scope
                    // fix up 'implied' flag
                    if (!implied && curDecl.implied
                        && (curDecl.scope == elemDepth))
                    {
                        curDecl.implied = false;
                    }
                    return;
                }
                // check that this doesn't conflict
                _qnameElement.CheckPrefixNS(prefix, ns);
                if (prefix.Length != 0)
                {
                    for (int i = 0; i < _attrCount; i++)
                    {
                        if (_attributes[i].name.prefix.Length != 0)
                            _attributes[i].name.CheckPrefixNS(prefix, ns);
                    }
                }
            }
            // actually add ns decl
            NamespaceDecl decl = new NamespaceDecl(prefix, ns,
                _elementStack[elemDepth].nsdecls,
                curDecl, elemDepth, implied);
            _elementStack[elemDepth].nsdecls = decl;
            _namespaces[prefix] = decl;
        }

        private void PopNamespaces(NamespaceDecl firstInScopeChain)
        {
            NamespaceDecl decl = firstInScopeChain;
            while (null != decl)
            {
                if (null == decl.prevLink)
                    _namespaces.Remove(decl.prefix);
                else
                    _namespaces[decl.prefix] = decl.prevLink;
                NamespaceDecl next = decl.scopeLink;
                // unlink chains for better gc behaviour 
                decl.prevLink = null;
                decl.scopeLink = null;
                decl = next;
            }
        }

        private void GenerateImpliedXmlnsAttrs()
        {
            QName name;
            NamespaceDecl decl = _elementStack[_elemDepth].nsdecls;
            while (null != decl)
            {
                if (decl.implied)
                {
                    if (_attrCount == _attributes.Length)
                        GrowAttributes();
                    if (decl.prefix.Length == 0)
                        name = new QName(string.Empty, _xmlns, _nsxmlns);
                    else
                        name = new QName(_xmlns, _xnt.Add(decl.prefix), _nsxmlns);
                    _attributes[_attrCount].Set(name, decl.uri);
                    _attrCount++;
                }
                decl = decl.scopeLink;
            }
        }

        private bool ReadInit(bool skipXmlDecl)
        {
            string err = null;
            if (!_sniffed)
            {
                // check magic header
                ushort magic = ReadUShort();
                if (magic != 0xFFDF)
                {
                    err = SR.XmlBinary_InvalidSignature;
                    goto Error;
                }
            }

            // check protocol version
            _version = ReadByte();
            if (_version != 0x1 && _version != 0x2)
            {
                err = SR.XmlBinary_InvalidProtocolVersion;
                goto Error;
            }

            // check encoding marker, 1200 == utf16
            if (1200 != ReadUShort())
            {
                err = SR.XmlBinary_UnsupportedCodePage;
                goto Error;
            }

            _state = ScanState.Doc;
            if (BinXmlToken.XmlDecl == PeekToken())
            {
                _pos++;
                _attributes[0].Set(new QName(string.Empty, _xnt.Add("version"), string.Empty), ParseText());
                _attrCount = 1;
                if (BinXmlToken.Encoding == PeekToken())
                {
                    _pos++;
                    _attributes[1].Set(new QName(string.Empty, _xnt.Add("encoding"), string.Empty), ParseText());
                    _attrCount++;
                }

                byte standalone = ReadByte();
                switch (standalone)
                {
                    case 0:
                        break;
                    case 1:
                    case 2:
                        _attributes[_attrCount].Set(new QName(string.Empty, _xnt.Add("standalone"), string.Empty), (standalone == 1) ? "yes" : "no");
                        _attrCount++;
                        break;
                    default:
                        err = SR.XmlBinary_InvalidStandalone;
                        goto Error;
                }
                if (!skipXmlDecl)
                {
                    QName xmlDeclQName = new QName(string.Empty, _xnt.Add("xml"), string.Empty);
                    _qnameOther = _qnameElement = xmlDeclQName;
                    _nodetype = XmlNodeType.XmlDeclaration;
                    _posAfterAttrs = _pos;
                    return true;
                }
                // else ReadDoc will clear the attributes for us
            }
            return ReadDoc();

        Error:
            _state = ScanState.Error;
            throw new XmlException(err, (string[])null);
        }

        private void ScanAttributes()
        {
            BinXmlToken token;
            int xmlspace = -1;
            int xmllang = -1;

            _mark = _pos;
            string curDeclPrefix = null;
            bool lastWasValue = false;

            while (BinXmlToken.EndAttrs != (token = NextToken()))
            {
                if (BinXmlToken.Attr == token)
                {
                    // watch out for nsdecl with no actual content
                    if (null != curDeclPrefix)
                    {
                        PushNamespace(curDeclPrefix, string.Empty, false);
                        curDeclPrefix = null;
                    }
                    // do we need to grow the array?
                    if (_attrCount == _attributes.Length)
                        GrowAttributes();
                    // note: ParseMB32 _must_ happen _before_ we grab this.pos...
                    QName n = _symbolTables.qnametable[ReadQNameRef()];
                    _attributes[_attrCount].Set(n, (int)_pos);
                    if (n.prefix == "xml")
                    {
                        if (n.localname == "lang")
                        {
                            xmllang = _attrCount;
                        }
                        else if (n.localname == "space")
                        {
                            xmlspace = _attrCount;
                        }
                    }
                    else if (Ref.Equal(n.namespaceUri, _nsxmlns))
                    {
                        // push namespace when we get the value
                        curDeclPrefix = n.localname;
                        if (curDeclPrefix == "xmlns")
                            curDeclPrefix = string.Empty;
                    }
                    else if (n.prefix.Length != 0)
                    {
                        if (n.namespaceUri.Length == 0)
                            throw new XmlException(SR.Xml_PrefixForEmptyNs, string.Empty);
                        this.PushNamespace(n.prefix, n.namespaceUri, true);
                    }
                    else if (n.namespaceUri.Length != 0)
                    {
                        throw ThrowXmlException(SR.XmlBinary_AttrWithNsNoPrefix, n.localname, n.namespaceUri);
                    }
                    _attrCount++;
                    lastWasValue = false;
                }
                else
                {
                    // first scan over token to make sure it is a value token
                    ScanOverValue(token, true, true);
                    // don't allow lists of values
                    if (lastWasValue)
                    {
                        throw ThrowNotSupported(SR.XmlBinary_ListsOfValuesNotSupported);
                    }

                    // if char checking is on, we need to scan text values to
                    // validate that they don't use invalid CharData, so we
                    // might as well store the saved string for quick attr value access
                    string val = _stringValue;
                    if (null != val)
                    {
                        _attributes[_attrCount - 1].val = val;
                        _stringValue = null;
                    }
                    // namespace decls can only have text values, and should only
                    // have a single value, so we just grab it here...
                    if (null != curDeclPrefix)
                    {
                        string nsuri = _xnt.Add(ValueAsString(token));
                        PushNamespace(curDeclPrefix, nsuri, false);
                        curDeclPrefix = null;
                    }
                    lastWasValue = true;
                }
            }

            if (xmlspace != -1)
            {
                string val = GetAttributeText(xmlspace);
                XmlSpace xs = XmlSpace.None;
                if (val == "preserve")
                    xs = XmlSpace.Preserve;
                else if (val == "default")
                    xs = XmlSpace.Default;
                _elementStack[_elemDepth].xmlSpace = xs;
                _xmlspacePreserve = (XmlSpace.Preserve == xs);
            }
            if (xmllang != -1)
            {
                _elementStack[_elemDepth].xmlLang = GetAttributeText(xmllang);
            }

            if (_attrCount < 200)
                SimpleCheckForDuplicateAttributes();
            else
                HashCheckForDuplicateAttributes();
        }

        private void SimpleCheckForDuplicateAttributes()
        {
            for (int i = 0; i < _attrCount; i++)
            {
                string localname, namespaceUri;
                _attributes[i].GetLocalnameAndNamespaceUri(out localname, out namespaceUri);
                for (int j = i + 1; j < _attrCount; j++)
                {
                    if (_attributes[j].MatchNS(localname, namespaceUri))
                        throw new XmlException(SR.Xml_DupAttributeName, _attributes[i].name.ToString());
                }
            }
        }

        private void HashCheckForDuplicateAttributes()
        {
            int tblSize = 256;
            while (tblSize < _attrCount)
                tblSize = checked(tblSize * 2);
            if (_attrHashTbl.Length < tblSize)
                _attrHashTbl = new int[tblSize];
            for (int i = 0; i < _attrCount; i++)
            {
                string localname, namespaceUri;
                int hash = _attributes[i].GetLocalnameAndNamespaceUriAndHash(_hasher, out localname, out namespaceUri);
                int index = hash & (tblSize - 1);
                int next = _attrHashTbl[index];
                _attrHashTbl[index] = i + 1;
                _attributes[i].prevHash = next;
                while (next != 0)
                {
                    next--;
                    if (_attributes[next].MatchHashNS(hash, localname, namespaceUri))
                    {
                        throw new XmlException(SR.Xml_DupAttributeName, _attributes[i].name.ToString());
                    }
                    next = _attributes[next].prevHash;
                }
            }
            Array.Clear(_attrHashTbl, 0, tblSize);
        }

        private string XmlDeclValue()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _attrCount; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.Append(_attributes[i].name.localname);
                sb.Append("=\"");
                sb.Append(_attributes[i].val);
                sb.Append('"');
            }
            return sb.ToString();
        }

        private string CDATAValue()
        {
            Debug.Assert(_stringValue == null, "this.stringValue == null");
            Debug.Assert(_token == BinXmlToken.CData, "this.token == BinXmlToken.CData");
            string value = GetString(_tokDataPos, _tokLen);
            StringBuilder sb = null;
            while (PeekToken() == BinXmlToken.CData)
            {
                _pos++; // skip over token byte
                if (sb == null)
                {
                    sb = new StringBuilder(value.Length + value.Length / 2);
                    sb.Append(value);
                }
                sb.Append(ParseText());
            }
            if (sb != null)
                value = sb.ToString();
            _stringValue = value;
            return value;
        }

        private void FinishCDATA()
        {
            for (;;)
            {
                switch (PeekToken())
                {
                    case BinXmlToken.CData:
                        // skip
                        _pos++;
                        int pos;
                        ScanText(out pos);
                        // try again
                        break;
                    case BinXmlToken.EndCData:
                        // done... on to next token...
                        _pos++;
                        return;
                    default:
                        throw new XmlException(SR.XmlBin_MissingEndCDATA);
                }
            }
        }

        private void FinishEndElement()
        {
            NamespaceDecl nsdecls = _elementStack[_elemDepth].Clear();
            this.PopNamespaces(nsdecls);
            _elemDepth--;
        }

        private bool ReadDoc()
        {
            switch (_nodetype)
            {
                case XmlNodeType.CDATA:
                    FinishCDATA();
                    break;
                case XmlNodeType.EndElement:
                    FinishEndElement();
                    break;
                case XmlNodeType.Element:
                    if (_isEmpty)
                    {
                        FinishEndElement();
                        _isEmpty = false;
                    }
                    break;
            }

        Read:
            // clear existing state
            _nodetype = XmlNodeType.None;
            _mark = -1;
            if (_qnameOther.localname.Length != 0)
                _qnameOther.Clear();

            ClearAttributes();
            _attrCount = 0;
            _valueType = TypeOfString;
            _stringValue = null;
            _hasTypedValue = false;

            _token = NextToken();
            switch (_token)
            {
                case BinXmlToken.EOF:
                    if (_elemDepth > 0)
                        throw new XmlException(SR.Xml_UnexpectedEOF1, (string[])null);
                    _state = ScanState.EOF;
                    return false;

                case BinXmlToken.Element:
                    ImplReadElement();
                    break;

                case BinXmlToken.EndElem:
                    ImplReadEndElement();
                    break;

                case BinXmlToken.DocType:
                    ImplReadDoctype();
                    if (_dtdProcessing == DtdProcessing.Ignore)
                        goto Read;
                    // nested, don't report doctype
                    if (_prevNameInfo != null)
                        goto Read;
                    break;

                case BinXmlToken.PI:
                    ImplReadPI();
                    if (_ignorePIs)
                        goto Read;
                    break;

                case BinXmlToken.Comment:
                    ImplReadComment();
                    if (_ignoreComments)
                        goto Read;
                    break;

                case BinXmlToken.CData:
                    ImplReadCDATA();
                    break;

                case BinXmlToken.Nest:
                    ImplReadNest();
                    // parse first token in nested document
                    _sniffed = false;
                    return ReadInit(true);

                case BinXmlToken.EndNest:
                    if (null == _prevNameInfo)
                        goto default;
                    ImplReadEndNest();
                    return ReadDoc();

                case BinXmlToken.XmlText:
                    ImplReadXmlText();
                    break;

                // text values
                case BinXmlToken.SQL_BIT:
                case BinXmlToken.SQL_TINYINT:
                case BinXmlToken.SQL_SMALLINT:
                case BinXmlToken.SQL_INT:
                case BinXmlToken.SQL_BIGINT:
                case BinXmlToken.SQL_REAL:
                case BinXmlToken.SQL_FLOAT:
                case BinXmlToken.SQL_MONEY:
                case BinXmlToken.SQL_SMALLMONEY:
                case BinXmlToken.SQL_DATETIME:
                case BinXmlToken.SQL_SMALLDATETIME:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_UUID:
                case BinXmlToken.SQL_VARBINARY:
                case BinXmlToken.SQL_BINARY:
                case BinXmlToken.SQL_IMAGE:
                case BinXmlToken.SQL_UDT:
                case BinXmlToken.XSD_KATMAI_DATE:
                case BinXmlToken.XSD_KATMAI_DATETIME:
                case BinXmlToken.XSD_KATMAI_TIME:
                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                case BinXmlToken.XSD_BINHEX:
                case BinXmlToken.XSD_BASE64:
                case BinXmlToken.SQL_CHAR:
                case BinXmlToken.SQL_VARCHAR:
                case BinXmlToken.SQL_TEXT:
                case BinXmlToken.SQL_NCHAR:
                case BinXmlToken.SQL_NVARCHAR:
                case BinXmlToken.SQL_NTEXT:
                case BinXmlToken.XSD_BOOLEAN:
                case BinXmlToken.XSD_TIME:
                case BinXmlToken.XSD_DATETIME:
                case BinXmlToken.XSD_DATE:
                case BinXmlToken.XSD_BYTE:
                case BinXmlToken.XSD_UNSIGNEDSHORT:
                case BinXmlToken.XSD_UNSIGNEDINT:
                case BinXmlToken.XSD_UNSIGNEDLONG:
                case BinXmlToken.XSD_QNAME:
                    ImplReadData(_token);
                    if (XmlNodeType.Text == _nodetype)
                        CheckAllowContent();
                    else if (_ignoreWhitespace && !_xmlspacePreserve)
                        goto Read; // skip to next token
                    return true;

                default:
                    throw ThrowUnexpectedToken(_token);
            }

            return true;
        }

        private void ImplReadData(BinXmlToken tokenType)
        {
            Debug.Assert(_mark < 0);
            _mark = _pos;

            switch (tokenType)
            {
                case BinXmlToken.SQL_CHAR:
                case BinXmlToken.SQL_VARCHAR:
                case BinXmlToken.SQL_TEXT:
                case BinXmlToken.SQL_NCHAR:
                case BinXmlToken.SQL_NVARCHAR:
                case BinXmlToken.SQL_NTEXT:
                    _valueType = TypeOfString;
                    _hasTypedValue = false;
                    break;
                default:
                    _valueType = GetValueType(_token);
                    _hasTypedValue = true;
                    break;
            }

            _nodetype = ScanOverValue(_token, false, true);

            // we don't support lists of values
            BinXmlToken tNext = PeekNextToken();
            switch (tNext)
            {
                case BinXmlToken.SQL_BIT:
                case BinXmlToken.SQL_TINYINT:
                case BinXmlToken.SQL_SMALLINT:
                case BinXmlToken.SQL_INT:
                case BinXmlToken.SQL_BIGINT:
                case BinXmlToken.SQL_REAL:
                case BinXmlToken.SQL_FLOAT:
                case BinXmlToken.SQL_MONEY:
                case BinXmlToken.SQL_SMALLMONEY:
                case BinXmlToken.SQL_DATETIME:
                case BinXmlToken.SQL_SMALLDATETIME:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_UUID:
                case BinXmlToken.SQL_VARBINARY:
                case BinXmlToken.SQL_BINARY:
                case BinXmlToken.SQL_IMAGE:
                case BinXmlToken.SQL_UDT:
                case BinXmlToken.XSD_KATMAI_DATE:
                case BinXmlToken.XSD_KATMAI_DATETIME:
                case BinXmlToken.XSD_KATMAI_TIME:
                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                case BinXmlToken.XSD_BINHEX:
                case BinXmlToken.XSD_BASE64:
                case BinXmlToken.SQL_CHAR:
                case BinXmlToken.SQL_VARCHAR:
                case BinXmlToken.SQL_TEXT:
                case BinXmlToken.SQL_NCHAR:
                case BinXmlToken.SQL_NVARCHAR:
                case BinXmlToken.SQL_NTEXT:
                case BinXmlToken.XSD_BOOLEAN:
                case BinXmlToken.XSD_TIME:
                case BinXmlToken.XSD_DATETIME:
                case BinXmlToken.XSD_DATE:
                case BinXmlToken.XSD_BYTE:
                case BinXmlToken.XSD_UNSIGNEDSHORT:
                case BinXmlToken.XSD_UNSIGNEDINT:
                case BinXmlToken.XSD_UNSIGNEDLONG:
                case BinXmlToken.XSD_QNAME:
                    throw ThrowNotSupported(SR.XmlBinary_ListsOfValuesNotSupported);
                default:
                    break;
            }
        }

        private void ImplReadElement()
        {
            if (3 != _docState || 9 != _docState)
            {
                switch (_docState)
                {
                    case 0:
                        _docState = 9;
                        break;
                    case 1:
                    case 2:
                        _docState = 3;
                        break;
                    case -1:
                        throw ThrowUnexpectedToken(_token);
                    default:
                        break;
                }
            }
            _elemDepth++;
            if (_elemDepth == _elementStack.Length)
                GrowElements();
            QName qname = _symbolTables.qnametable[ReadQNameRef()];
            _qnameOther = _qnameElement = qname;
            _elementStack[_elemDepth].Set(qname, _xmlspacePreserve);
            this.PushNamespace(qname.prefix, qname.namespaceUri, true);
            BinXmlToken t = PeekNextToken();
            if (BinXmlToken.Attr == t)
            {
                ScanAttributes();
                t = PeekNextToken();
            }
            GenerateImpliedXmlnsAttrs();
            if (BinXmlToken.EndElem == t)
            {
                NextToken(); // move over token...
                _isEmpty = true;
            }
            else if (BinXmlToken.SQL_NVARCHAR == t)
            {
                if (_mark < 0)
                    _mark = _pos;
                // skip over token byte
                _pos++;
                // is this a zero-length string?  if yes, skip it.  
                // (It just indicates that this is _not_ an empty element)
                // Also make sure that the following token is an EndElem
                if (0 == ReadByte())
                {
                    if (BinXmlToken.EndElem != (BinXmlToken)ReadByte())
                    {
                        Debug.Assert(_pos >= 3);
                        _pos -= 3; // jump back to start of NVarChar token
                    }
                    else
                    {
                        Debug.Assert(_pos >= 1);
                        _pos -= 1; // jump back to EndElem token
                    }
                }
                else
                {
                    Debug.Assert(_pos >= 2);
                    _pos -= 2; // jump back to start of NVarChar token
                }
            }
            _nodetype = XmlNodeType.Element;
            _valueType = TypeOfObject;
            _posAfterAttrs = _pos;
        }

        private void ImplReadEndElement()
        {
            if (_elemDepth == 0)
                throw ThrowXmlException(SR.Xml_UnexpectedEndTag);
            int index = _elemDepth;
            if (1 == index && 3 == _docState)
                _docState = -1;
            _qnameOther = _elementStack[index].name;
            _xmlspacePreserve = _elementStack[index].xmlspacePreserve;
            _nodetype = XmlNodeType.EndElement;
        }

        private void ImplReadDoctype()
        {
            if (_dtdProcessing == DtdProcessing.Prohibit)
                throw ThrowXmlException(SR.Xml_DtdIsProhibited);
            // 0=>auto, 1=>doc/pre-dtd, 2=>doc/pre-elem, 3=>doc/instance -1=>doc/post-elem, 9=>frag
            switch (_docState)
            {
                case 0: // 0=>auto
                case 1: // 1=>doc/pre-dtd
                    break;
                case 9: // 9=>frag
                    throw ThrowXmlException(SR.Xml_DtdNotAllowedInFragment);
                default: // 2=>doc/pre-elem, 3=>doc/instance -1=>doc/post-elem
                    throw ThrowXmlException(SR.Xml_BadDTDLocation);
            }
            _docState = 2;
            _qnameOther.localname = ParseText();
            if (BinXmlToken.System == PeekToken())
            {
                _pos++;
                _attributes[_attrCount++].Set(new QName(string.Empty, _xnt.Add("SYSTEM"), string.Empty), ParseText());
            }
            if (BinXmlToken.Public == PeekToken())
            {
                _pos++;
                _attributes[_attrCount++].Set(new QName(string.Empty, _xnt.Add("PUBLIC"), string.Empty), ParseText());
            }
            if (BinXmlToken.Subset == PeekToken())
            {
                _pos++;
                _mark = _pos;
                _tokLen = ScanText(out _tokDataPos);
            }
            else
            {
                _tokLen = _tokDataPos = 0;
            }
            _nodetype = XmlNodeType.DocumentType;
            _posAfterAttrs = _pos;
        }

        private void ImplReadPI()
        {
            _qnameOther.localname = _symbolTables.symtable[ReadNameRef()];
            _mark = _pos;
            _tokLen = ScanText(out _tokDataPos);
            _nodetype = XmlNodeType.ProcessingInstruction;
        }

        private void ImplReadComment()
        {
            _nodetype = XmlNodeType.Comment;
            _mark = _pos;
            _tokLen = ScanText(out _tokDataPos);
        }

        private void ImplReadCDATA()
        {
            CheckAllowContent();
            _nodetype = XmlNodeType.CDATA;
            _mark = _pos;
            _tokLen = ScanText(out _tokDataPos);
        }

        private void ImplReadNest()
        {
            CheckAllowContent();
            // push current nametables
            _prevNameInfo = new NestedBinXml(_symbolTables, _docState, _prevNameInfo);
            _symbolTables.Init();
            _docState = 0; // auto
        }

        private void ImplReadEndNest()
        {
            NestedBinXml nested = _prevNameInfo;
            _symbolTables = nested.symbolTables;
            _docState = nested.docState;
            _prevNameInfo = nested.next;
        }

        private void ImplReadXmlText()
        {
            CheckAllowContent();
            string xmltext = ParseText();
            XmlNamespaceManager xnm = new XmlNamespaceManager(_xnt);
            foreach (NamespaceDecl decl in _namespaces.Values)
            {
                if (decl.scope > 0)
                {
#if DEBUG
                    if ((object)decl.prefix != (object)this._xnt.Get(decl.prefix))
                        throw new Exception("Prefix not interned: \'" + decl.prefix + "\'");
                    if ((object)decl.uri != (object)this._xnt.Get(decl.uri))
                        throw new Exception("Uri not interned: \'" + decl.uri + "\'");
#endif
                    xnm.AddNamespace(decl.prefix, decl.uri);
                }
            }
            XmlReaderSettings settings = this.Settings;
            settings.ReadOnly = false;
            settings.NameTable = _xnt;
            settings.DtdProcessing = DtdProcessing.Prohibit;
            if (0 != _elemDepth)
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
            }
            settings.ReadOnly = true;
            XmlParserContext xpc = new XmlParserContext(_xnt, xnm, this.XmlLang, this.XmlSpace);
            _textXmlReader = new XmlTextReaderImpl(xmltext, xpc, settings);
            if (!_textXmlReader.Read()
                || ((_textXmlReader.NodeType == XmlNodeType.XmlDeclaration)
                    && !_textXmlReader.Read()))
            {
                _state = ScanState.Doc;
                ReadDoc();
            }
            else
            {
                _state = ScanState.XmlText;
                UpdateFromTextReader();
            }
        }

        private void UpdateFromTextReader()
        {
            XmlReader r = _textXmlReader;
            _nodetype = r.NodeType;
            _qnameOther.prefix = r.Prefix;
            _qnameOther.localname = r.LocalName;
            _qnameOther.namespaceUri = r.NamespaceURI;
            _valueType = r.ValueType;
            _isEmpty = r.IsEmptyElement;
        }

        private bool UpdateFromTextReader(bool needUpdate)
        {
            if (needUpdate)
                UpdateFromTextReader();
            return needUpdate;
        }

        private void CheckAllowContent()
        {
            switch (_docState)
            {
                case 0: // auto
                    _docState = 9;
                    break;
                case 9: // conformance = fragment
                case 3:
                    break;
                default:
                    throw ThrowXmlException(SR.Xml_InvalidRootData);
            }
        }

        private void GenerateTokenTypeMap()
        {
            Type[] map = new Type[256];
            map[(int)BinXmlToken.XSD_BOOLEAN] = typeof(bool);
            map[(int)BinXmlToken.SQL_TINYINT] = typeof(byte);
            map[(int)BinXmlToken.XSD_BYTE] = typeof(sbyte);
            map[(int)BinXmlToken.SQL_SMALLINT] = typeof(short);
            map[(int)BinXmlToken.XSD_UNSIGNEDSHORT] = typeof(ushort);
            map[(int)BinXmlToken.XSD_UNSIGNEDINT] = typeof(uint);
            map[(int)BinXmlToken.SQL_REAL] = typeof(float);
            map[(int)BinXmlToken.SQL_FLOAT] = typeof(double);
            map[(int)BinXmlToken.SQL_BIGINT] = typeof(long);
            map[(int)BinXmlToken.XSD_UNSIGNEDLONG] = typeof(ulong);
            map[(int)BinXmlToken.XSD_QNAME] = typeof(XmlQualifiedName);
            Type TypeOfInt32 = typeof(int);
            map[(int)BinXmlToken.SQL_BIT] = TypeOfInt32;
            map[(int)BinXmlToken.SQL_INT] = TypeOfInt32;
            Type TypeOfDecimal = typeof(decimal);
            map[(int)BinXmlToken.SQL_SMALLMONEY] = TypeOfDecimal;
            map[(int)BinXmlToken.SQL_MONEY] = TypeOfDecimal;
            map[(int)BinXmlToken.SQL_DECIMAL] = TypeOfDecimal;
            map[(int)BinXmlToken.SQL_NUMERIC] = TypeOfDecimal;
            map[(int)BinXmlToken.XSD_DECIMAL] = TypeOfDecimal;
            Type TypeOfDateTime = typeof(System.DateTime);
            map[(int)BinXmlToken.SQL_SMALLDATETIME] = TypeOfDateTime;
            map[(int)BinXmlToken.SQL_DATETIME] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_TIME] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_DATETIME] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_DATE] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_KATMAI_DATE] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_KATMAI_DATETIME] = TypeOfDateTime;
            map[(int)BinXmlToken.XSD_KATMAI_TIME] = TypeOfDateTime;
            Type TypeOfDateTimeOffset = typeof(System.DateTimeOffset);
            map[(int)BinXmlToken.XSD_KATMAI_DATEOFFSET] = TypeOfDateTimeOffset;
            map[(int)BinXmlToken.XSD_KATMAI_DATETIMEOFFSET] = TypeOfDateTimeOffset;
            map[(int)BinXmlToken.XSD_KATMAI_TIMEOFFSET] = TypeOfDateTimeOffset;
            Type TypeOfByteArray = typeof(byte[]);
            map[(int)BinXmlToken.SQL_VARBINARY] = TypeOfByteArray;
            map[(int)BinXmlToken.SQL_BINARY] = TypeOfByteArray;
            map[(int)BinXmlToken.SQL_IMAGE] = TypeOfByteArray;
            map[(int)BinXmlToken.SQL_UDT] = TypeOfByteArray;
            map[(int)BinXmlToken.XSD_BINHEX] = TypeOfByteArray;
            map[(int)BinXmlToken.XSD_BASE64] = TypeOfByteArray;
            map[(int)BinXmlToken.SQL_CHAR] = TypeOfString;
            map[(int)BinXmlToken.SQL_VARCHAR] = TypeOfString;
            map[(int)BinXmlToken.SQL_TEXT] = TypeOfString;
            map[(int)BinXmlToken.SQL_NCHAR] = TypeOfString;
            map[(int)BinXmlToken.SQL_NVARCHAR] = TypeOfString;
            map[(int)BinXmlToken.SQL_NTEXT] = TypeOfString;
            map[(int)BinXmlToken.SQL_UUID] = TypeOfString;
            if (s_tokenTypeMap == null)
                s_tokenTypeMap = map;
        }

        private System.Type GetValueType(BinXmlToken token)
        {
            Type t = s_tokenTypeMap[(int)token];
            if (t == null)
                throw ThrowUnexpectedToken(token);
            return t;
        }

        // helper method...
        private void ReScanOverValue(BinXmlToken token)
        {
            ScanOverValue(token, true, false);
        }

        private XmlNodeType ScanOverValue(BinXmlToken token, bool attr, bool checkChars)
        {
            if (token == BinXmlToken.SQL_NVARCHAR)
            {
                if (_mark < 0)
                    _mark = _pos;
                _tokLen = ParseMB32();
                _tokDataPos = _pos;
                checked { _pos += _tokLen * 2; }
                Fill(-1);
                // check chars (if this is the first pass and settings.CheckCharacters was set)
                if (checkChars && _checkCharacters)
                {
                    // check for invalid chardata
                    return CheckText(attr);
                }
                else if (!attr)
                { // attribute values are always reported as Text
                    // check for whitespace-only text
                    return CheckTextIsWS();
                }
                else
                {
                    return XmlNodeType.Text;
                }
            }
            else
            {
                return ScanOverAnyValue(token, attr, checkChars);
            }
        }

        private XmlNodeType ScanOverAnyValue(BinXmlToken token, bool attr, bool checkChars)
        {
            if (_mark < 0)
                _mark = _pos;
            checked
            {
                switch (token)
                {
                    case BinXmlToken.SQL_BIT:
                    case BinXmlToken.SQL_TINYINT:
                    case BinXmlToken.XSD_BOOLEAN:
                    case BinXmlToken.XSD_BYTE:
                        _tokDataPos = _pos;
                        _tokLen = 1;
                        _pos += 1;
                        break;

                    case BinXmlToken.SQL_SMALLINT:
                    case BinXmlToken.XSD_UNSIGNEDSHORT:
                        _tokDataPos = _pos;
                        _tokLen = 2;
                        _pos += 2;
                        break;

                    case BinXmlToken.SQL_INT:
                    case BinXmlToken.XSD_UNSIGNEDINT:
                    case BinXmlToken.SQL_REAL:
                    case BinXmlToken.SQL_SMALLMONEY:
                    case BinXmlToken.SQL_SMALLDATETIME:
                        _tokDataPos = _pos;
                        _tokLen = 4;
                        _pos += 4;
                        break;

                    case BinXmlToken.SQL_BIGINT:
                    case BinXmlToken.XSD_UNSIGNEDLONG:
                    case BinXmlToken.SQL_FLOAT:
                    case BinXmlToken.SQL_MONEY:
                    case BinXmlToken.SQL_DATETIME:
                    case BinXmlToken.XSD_TIME:
                    case BinXmlToken.XSD_DATETIME:
                    case BinXmlToken.XSD_DATE:
                        _tokDataPos = _pos;
                        _tokLen = 8;
                        _pos += 8;
                        break;

                    case BinXmlToken.SQL_UUID:
                        _tokDataPos = _pos;
                        _tokLen = 16;
                        _pos += 16;
                        break;

                    case BinXmlToken.SQL_DECIMAL:
                    case BinXmlToken.SQL_NUMERIC:
                    case BinXmlToken.XSD_DECIMAL:
                        _tokDataPos = _pos;
                        _tokLen = ParseMB64();
                        _pos += _tokLen;
                        break;

                    case BinXmlToken.SQL_VARBINARY:
                    case BinXmlToken.SQL_BINARY:
                    case BinXmlToken.SQL_IMAGE:
                    case BinXmlToken.SQL_UDT:
                    case BinXmlToken.XSD_BINHEX:
                    case BinXmlToken.XSD_BASE64:
                        _tokLen = ParseMB64();
                        _tokDataPos = _pos;
                        _pos += _tokLen;
                        break;

                    case BinXmlToken.SQL_CHAR:
                    case BinXmlToken.SQL_VARCHAR:
                    case BinXmlToken.SQL_TEXT:
                        _tokLen = ParseMB64();
                        _tokDataPos = _pos;
                        _pos += _tokLen;
                        if (checkChars && _checkCharacters)
                        {
                            // check for invalid chardata
                            Fill(-1);
                            string val = ValueAsString(token);
                            XmlConvert.VerifyCharData(val, ExceptionType.ArgumentException, ExceptionType.XmlException);
                            _stringValue = val;
                        }
                        break;

                    case BinXmlToken.SQL_NVARCHAR:
                    case BinXmlToken.SQL_NCHAR:
                    case BinXmlToken.SQL_NTEXT:
                        return ScanOverValue(BinXmlToken.SQL_NVARCHAR, attr, checkChars);

                    case BinXmlToken.XSD_QNAME:
                        _tokDataPos = _pos;
                        ParseMB32();
                        break;

                    case BinXmlToken.XSD_KATMAI_DATE:
                    case BinXmlToken.XSD_KATMAI_DATETIME:
                    case BinXmlToken.XSD_KATMAI_TIME:
                    case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                    case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                        VerifyVersion(2, token);
                        _tokDataPos = _pos;
                        _tokLen = GetXsdKatmaiTokenLength(token);
                        _pos += _tokLen;
                        break;

                    default:
                        throw ThrowUnexpectedToken(token);
                }
            }
            Fill(-1);
            return XmlNodeType.Text;
        }

        private unsafe XmlNodeType CheckText(bool attr)
        {
            Debug.Assert(_checkCharacters, "this.checkCharacters");
            // assert that size is an even number
            Debug.Assert(0 == ((_pos - _tokDataPos) & 1), "Data size should not be odd");
            // grab local copy (perf)
            XmlCharType xmlCharType = _xmlCharType;

            fixed (byte* pb = _data)
            {
                int end = _pos;
                int pos = _tokDataPos;

                if (!attr)
                {
                    // scan if this is whitespace
                    for (;;)
                    {
                        int posNext = pos + 2;
                        if (posNext > end)
                            return _xmlspacePreserve ? XmlNodeType.SignificantWhitespace : XmlNodeType.Whitespace;
                        if (pb[pos + 1] != 0 || !xmlCharType.IsWhiteSpace((char)pb[pos]))
                            break;
                        pos = posNext;
                    }
                }

                for (;;)
                {
                    char ch;
                    for (;;)
                    {
                        int posNext = pos + 2;
                        if (posNext > end)
                            return XmlNodeType.Text;
                        ch = (char)(pb[pos] | ((int)(pb[pos + 1]) << 8));
                        if (!_xmlCharType.IsCharData(ch))
                            break;
                        pos = posNext;
                    }

                    if (!XmlCharType.IsHighSurrogate(ch))
                    {
                        throw XmlConvert.CreateInvalidCharException(ch, '\0', ExceptionType.XmlException);
                    }
                    else
                    {
                        if ((pos + 4) > end)
                        {
                            throw ThrowXmlException(SR.Xml_InvalidSurrogateMissingLowChar);
                        }
                        char chNext = (char)(pb[pos + 2] | ((int)(pb[pos + 3]) << 8));
                        if (!XmlCharType.IsLowSurrogate(chNext))
                        {
                            throw XmlConvert.CreateInvalidSurrogatePairException(ch, chNext);
                        }
                    }
                    pos += 4;
                }
            }
        }

        private XmlNodeType CheckTextIsWS()
        {
            Debug.Assert(!_checkCharacters, "!this.checkCharacters");
            byte[] data = _data;
            // assert that size is an even number
            Debug.Assert(0 == ((_pos - _tokDataPos) & 1), "Data size should not be odd");
            for (int pos = _tokDataPos; pos < _pos; pos += 2)
            {
                if (0 != data[pos + 1])
                    goto NonWSText;
                switch (data[pos])
                {
                    case 0x09: // tab
                    case 0x0A: // nl
                    case 0x0D: // cr
                    case 0x20: // space
                        break;
                    default:
                        goto NonWSText;
                }
            }
            if (_xmlspacePreserve)
                return XmlNodeType.SignificantWhitespace;
            return XmlNodeType.Whitespace;
        NonWSText:
            return XmlNodeType.Text;
        }

        private void CheckValueTokenBounds()
        {
            if ((_end - _tokDataPos) < _tokLen)
                throw ThrowXmlException(SR.Xml_UnexpectedEOF1);
        }

        private int GetXsdKatmaiTokenLength(BinXmlToken token)
        {
            byte scale;
            switch (token)
            {
                case BinXmlToken.XSD_KATMAI_DATE:
                    // SQL Katmai type DATE = date(3b)
                    return 3;
                case BinXmlToken.XSD_KATMAI_TIME:
                case BinXmlToken.XSD_KATMAI_DATETIME:
                    // SQL Katmai type DATETIME2 = scale(1b) + time(3-5b) + date(3b)
                    Fill(0);
                    scale = _data[_pos];
                    return 4 + XsdKatmaiTimeScaleToValueLength(scale);
                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    // SQL Katmai type DATETIMEOFFSET = scale(1b) + time(3-5b) + date(3b) + zone(2b)
                    Fill(0);
                    scale = _data[_pos];
                    return 6 + XsdKatmaiTimeScaleToValueLength(scale);
                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private int XsdKatmaiTimeScaleToValueLength(byte scale)
        {
            if (scale > 7)
            {
                throw new XmlException(SR.SqlTypes_ArithOverflow, (string)null);
            }
            return s_xsdKatmaiTimeScaleToValueLengthMap[scale];
        }

        private long ValueAsLong()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.SQL_BIT:
                case BinXmlToken.SQL_TINYINT:
                    {
                        byte v = _data[_tokDataPos];
                        return v;
                    }

                case BinXmlToken.XSD_BYTE:
                    {
                        sbyte v = unchecked((sbyte)_data[_tokDataPos]);
                        return v;
                    }

                case BinXmlToken.SQL_SMALLINT:
                    return GetInt16(_tokDataPos);

                case BinXmlToken.SQL_INT:
                    return GetInt32(_tokDataPos);

                case BinXmlToken.SQL_BIGINT:
                    return GetInt64(_tokDataPos);

                case BinXmlToken.XSD_UNSIGNEDSHORT:
                    return GetUInt16(_tokDataPos);

                case BinXmlToken.XSD_UNSIGNEDINT:
                    return GetUInt32(_tokDataPos);

                case BinXmlToken.XSD_UNSIGNEDLONG:
                    {
                        ulong v = GetUInt64(_tokDataPos);
                        return checked((long)v);
                    }

                case BinXmlToken.SQL_REAL:
                case BinXmlToken.SQL_FLOAT:
                    {
                        double v = ValueAsDouble();
                        return (long)v;
                    }

                case BinXmlToken.SQL_MONEY:
                case BinXmlToken.SQL_SMALLMONEY:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                case BinXmlToken.XSD_DECIMAL:
                    {
                        decimal v = ValueAsDecimal();
                        return (long)v;
                    }

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private ulong ValueAsULong()
        {
            if (BinXmlToken.XSD_UNSIGNEDLONG == _token)
            {
                CheckValueTokenBounds();
                return GetUInt64(_tokDataPos);
            }
            else
            {
                throw ThrowUnexpectedToken(_token);
            }
        }

        private decimal ValueAsDecimal()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.SQL_BIT:
                case BinXmlToken.SQL_TINYINT:
                case BinXmlToken.SQL_SMALLINT:
                case BinXmlToken.SQL_INT:
                case BinXmlToken.SQL_BIGINT:
                case BinXmlToken.XSD_BYTE:
                case BinXmlToken.XSD_UNSIGNEDSHORT:
                case BinXmlToken.XSD_UNSIGNEDINT:
                    return new decimal(ValueAsLong());

                case BinXmlToken.XSD_UNSIGNEDLONG:
                    return new decimal(ValueAsULong());

                case BinXmlToken.SQL_REAL:
                    return new decimal(GetSingle(_tokDataPos));

                case BinXmlToken.SQL_FLOAT:
                    return new decimal(GetDouble(_tokDataPos));

                case BinXmlToken.SQL_SMALLMONEY:
                    {
                        BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt32(_tokDataPos));
                        return v.ToDecimal();
                    }
                case BinXmlToken.SQL_MONEY:
                    {
                        BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt64(_tokDataPos));
                        return v.ToDecimal();
                    }

                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                    {
                        BinXmlSqlDecimal v = new BinXmlSqlDecimal(_data, _tokDataPos, _token == BinXmlToken.XSD_DECIMAL);
                        return v.ToDecimal();
                    }

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private double ValueAsDouble()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.SQL_BIT:
                case BinXmlToken.SQL_TINYINT:
                case BinXmlToken.SQL_SMALLINT:
                case BinXmlToken.SQL_INT:
                case BinXmlToken.SQL_BIGINT:
                case BinXmlToken.XSD_BYTE:
                case BinXmlToken.XSD_UNSIGNEDSHORT:
                case BinXmlToken.XSD_UNSIGNEDINT:
                    return (double)ValueAsLong();

                case BinXmlToken.XSD_UNSIGNEDLONG:
                    return (double)ValueAsULong();

                case BinXmlToken.SQL_REAL:
                    return GetSingle(_tokDataPos);

                case BinXmlToken.SQL_FLOAT:
                    return GetDouble(_tokDataPos);

                case BinXmlToken.SQL_SMALLMONEY:
                case BinXmlToken.SQL_MONEY:
                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                    return (double)ValueAsDecimal();

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private DateTime ValueAsDateTime()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.SQL_DATETIME:
                    {
                        int pos = _tokDataPos;
                        int dateticks; uint timeticks;
                        dateticks = GetInt32(pos);
                        timeticks = GetUInt32(pos + 4);
                        return BinXmlDateTime.SqlDateTimeToDateTime(dateticks, timeticks);
                    }

                case BinXmlToken.SQL_SMALLDATETIME:
                    {
                        int pos = _tokDataPos;
                        short dateticks; ushort timeticks;
                        dateticks = GetInt16(pos);
                        timeticks = GetUInt16(pos + 2);
                        return BinXmlDateTime.SqlSmallDateTimeToDateTime(dateticks, timeticks);
                    }

                case BinXmlToken.XSD_TIME:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdTimeToDateTime(time);
                    }

                case BinXmlToken.XSD_DATE:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdDateToDateTime(time);
                    }

                case BinXmlToken.XSD_DATETIME:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdDateTimeToDateTime(time);
                    }

                case BinXmlToken.XSD_KATMAI_DATE:
                    return BinXmlDateTime.XsdKatmaiDateToDateTime(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATETIME:
                    return BinXmlDateTime.XsdKatmaiDateTimeToDateTime(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_TIME:
                    return BinXmlDateTime.XsdKatmaiTimeToDateTime(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateOffsetToDateTime(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateTimeOffsetToDateTime(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiTimeOffsetToDateTime(_data, _tokDataPos);

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private DateTimeOffset ValueAsDateTimeOffset()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateOffsetToDateTimeOffset(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateTimeOffsetToDateTimeOffset(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiTimeOffsetToDateTimeOffset(_data, _tokDataPos);

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }


        private string ValueAsDateTimeString()
        {
            CheckValueTokenBounds();
            switch (_token)
            {
                case BinXmlToken.SQL_DATETIME:
                    {
                        int pos = _tokDataPos;
                        int dateticks; uint timeticks;
                        dateticks = GetInt32(pos);
                        timeticks = GetUInt32(pos + 4);
                        return BinXmlDateTime.SqlDateTimeToString(dateticks, timeticks);
                    }

                case BinXmlToken.SQL_SMALLDATETIME:
                    {
                        int pos = _tokDataPos;
                        short dateticks; ushort timeticks;
                        dateticks = GetInt16(pos);
                        timeticks = GetUInt16(pos + 2);
                        return BinXmlDateTime.SqlSmallDateTimeToString(dateticks, timeticks);
                    }

                case BinXmlToken.XSD_TIME:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdTimeToString(time);
                    }

                case BinXmlToken.XSD_DATE:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdDateToString(time);
                    }

                case BinXmlToken.XSD_DATETIME:
                    {
                        long time = GetInt64(_tokDataPos);
                        return BinXmlDateTime.XsdDateTimeToString(time);
                    }

                case BinXmlToken.XSD_KATMAI_DATE:
                    return BinXmlDateTime.XsdKatmaiDateToString(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATETIME:
                    return BinXmlDateTime.XsdKatmaiDateTimeToString(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_TIME:
                    return BinXmlDateTime.XsdKatmaiTimeToString(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateOffsetToString(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiDateTimeOffsetToString(_data, _tokDataPos);

                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                    return BinXmlDateTime.XsdKatmaiTimeOffsetToString(_data, _tokDataPos);

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private string ValueAsString(BinXmlToken token)
        {
            try
            {
                CheckValueTokenBounds();
                switch (token)
                {
                    case BinXmlToken.SQL_NCHAR:
                    case BinXmlToken.SQL_NVARCHAR:
                    case BinXmlToken.SQL_NTEXT:
                        return GetString(_tokDataPos, _tokLen);

                    case BinXmlToken.XSD_BOOLEAN:
                        {
                            if (0 == _data[_tokDataPos])
                                return "false";
                            else
                                return "true";
                        }

                    case BinXmlToken.SQL_BIT:
                    case BinXmlToken.SQL_TINYINT:
                    case BinXmlToken.SQL_SMALLINT:
                    case BinXmlToken.SQL_INT:
                    case BinXmlToken.SQL_BIGINT:
                    case BinXmlToken.XSD_BYTE:
                    case BinXmlToken.XSD_UNSIGNEDSHORT:
                    case BinXmlToken.XSD_UNSIGNEDINT:
                        return ValueAsLong().ToString(CultureInfo.InvariantCulture);

                    case BinXmlToken.XSD_UNSIGNEDLONG:
                        return ValueAsULong().ToString(CultureInfo.InvariantCulture);

                    case BinXmlToken.SQL_REAL:
                        return XmlConvert.ToString(GetSingle(_tokDataPos));

                    case BinXmlToken.SQL_FLOAT:
                        return XmlConvert.ToString(GetDouble(_tokDataPos));

                    case BinXmlToken.SQL_UUID:
                        {
                            int a; short b, c;
                            int pos = _tokDataPos;
                            a = GetInt32(pos);
                            b = GetInt16(pos + 4);
                            c = GetInt16(pos + 6);
                            Guid v = new Guid(a, b, c, _data[pos + 8], _data[pos + 9], _data[pos + 10], _data[pos + 11], _data[pos + 12], _data[pos + 13], _data[pos + 14], _data[pos + 15]);
                            return v.ToString();
                        }

                    case BinXmlToken.SQL_SMALLMONEY:
                        {
                            BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt32(_tokDataPos));
                            return v.ToString();
                        }
                    case BinXmlToken.SQL_MONEY:
                        {
                            BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt64(_tokDataPos));
                            return v.ToString();
                        }

                    case BinXmlToken.XSD_DECIMAL:
                    case BinXmlToken.SQL_DECIMAL:
                    case BinXmlToken.SQL_NUMERIC:
                        {
                            BinXmlSqlDecimal v = new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL);
                            return v.ToString();
                        }

                    case BinXmlToken.SQL_CHAR:
                    case BinXmlToken.SQL_VARCHAR:
                    case BinXmlToken.SQL_TEXT:
                        {
                            int pos = _tokDataPos;
                            int codepage = GetInt32(pos);
                            Encoding enc = System.Text.Encoding.GetEncoding(codepage);
                            return enc.GetString(_data, pos + 4, _tokLen - 4);
                        }

                    case BinXmlToken.SQL_VARBINARY:
                    case BinXmlToken.SQL_BINARY:
                    case BinXmlToken.SQL_IMAGE:
                    case BinXmlToken.SQL_UDT:
                    case BinXmlToken.XSD_BASE64:
                        {
                            return Convert.ToBase64String(_data, _tokDataPos, _tokLen);
                        }

                    case BinXmlToken.XSD_BINHEX:
                        return BinHexEncoder.Encode(_data, _tokDataPos, _tokLen);

                    case BinXmlToken.SQL_DATETIME:
                    case BinXmlToken.SQL_SMALLDATETIME:
                    case BinXmlToken.XSD_TIME:
                    case BinXmlToken.XSD_DATE:
                    case BinXmlToken.XSD_DATETIME:
                    case BinXmlToken.XSD_KATMAI_DATE:
                    case BinXmlToken.XSD_KATMAI_DATETIME:
                    case BinXmlToken.XSD_KATMAI_TIME:
                    case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                    case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                    case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                        return ValueAsDateTimeString();

                    case BinXmlToken.XSD_QNAME:
                        {
                            int nameNum = ParseMB32(_tokDataPos);
                            if (nameNum < 0 || nameNum >= _symbolTables.qnameCount)
                                throw new XmlException(SR.XmlBin_InvalidQNameID, string.Empty);
                            QName qname = _symbolTables.qnametable[nameNum];
                            if (qname.prefix.Length == 0)
                                return qname.localname;
                            else
                                return string.Concat(qname.prefix, ":", qname.localname);
                        }

                    default:
                        throw ThrowUnexpectedToken(_token);
                }
            }
            catch
            {
                _state = ScanState.Error;
                throw;
            }
        }

        private object ValueAsObject(BinXmlToken token, bool returnInternalTypes)
        {
            CheckValueTokenBounds();
            switch (token)
            {
                case BinXmlToken.SQL_NCHAR:
                case BinXmlToken.SQL_NVARCHAR:
                case BinXmlToken.SQL_NTEXT:
                    return GetString(_tokDataPos, _tokLen);

                case BinXmlToken.XSD_BOOLEAN:
                    return (0 != _data[_tokDataPos]);

                case BinXmlToken.SQL_BIT:
                    return (int)_data[_tokDataPos];

                case BinXmlToken.SQL_TINYINT:
                    return _data[_tokDataPos];

                case BinXmlToken.SQL_SMALLINT:
                    return GetInt16(_tokDataPos);

                case BinXmlToken.SQL_INT:
                    return GetInt32(_tokDataPos);

                case BinXmlToken.SQL_BIGINT:
                    return GetInt64(_tokDataPos);

                case BinXmlToken.XSD_BYTE:
                    {
                        sbyte v = unchecked((sbyte)_data[_tokDataPos]);
                        return v;
                    }

                case BinXmlToken.XSD_UNSIGNEDSHORT:
                    return GetUInt16(_tokDataPos);

                case BinXmlToken.XSD_UNSIGNEDINT:
                    return GetUInt32(_tokDataPos);

                case BinXmlToken.XSD_UNSIGNEDLONG:
                    return GetUInt64(_tokDataPos);

                case BinXmlToken.SQL_REAL:
                    return GetSingle(_tokDataPos);

                case BinXmlToken.SQL_FLOAT:
                    return GetDouble(_tokDataPos);

                case BinXmlToken.SQL_UUID:
                    {
                        int a; short b, c;
                        int pos = _tokDataPos;
                        a = GetInt32(pos);
                        b = GetInt16(pos + 4);
                        c = GetInt16(pos + 6);
                        Guid v = new Guid(a, b, c, _data[pos + 8], _data[pos + 9], _data[pos + 10], _data[pos + 11], _data[pos + 12], _data[pos + 13], _data[pos + 14], _data[pos + 15]);
                        return v.ToString();
                    }

                case BinXmlToken.SQL_SMALLMONEY:
                    {
                        BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt32(_tokDataPos));
                        if (returnInternalTypes)
                            return v;
                        else
                            return v.ToDecimal();
                    }

                case BinXmlToken.SQL_MONEY:
                    {
                        BinXmlSqlMoney v = new BinXmlSqlMoney(GetInt64(_tokDataPos));
                        if (returnInternalTypes)
                            return v;
                        else
                            return v.ToDecimal();
                    }

                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                    {
                        BinXmlSqlDecimal v = new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL);
                        if (returnInternalTypes)
                            return v;
                        else
                            return v.ToDecimal();
                    }

                case BinXmlToken.SQL_CHAR:
                case BinXmlToken.SQL_VARCHAR:
                case BinXmlToken.SQL_TEXT:
                    {
                        int pos = _tokDataPos;
                        int codepage = GetInt32(pos);
                        Encoding enc = System.Text.Encoding.GetEncoding(codepage);
                        return enc.GetString(_data, pos + 4, _tokLen - 4);
                    }

                case BinXmlToken.SQL_VARBINARY:
                case BinXmlToken.SQL_BINARY:
                case BinXmlToken.SQL_IMAGE:
                case BinXmlToken.SQL_UDT:
                case BinXmlToken.XSD_BASE64:
                case BinXmlToken.XSD_BINHEX:
                    {
                        byte[] data = new byte[_tokLen];
                        Array.Copy(_data, _tokDataPos, data, 0, _tokLen);
                        return data;
                    }

                case BinXmlToken.SQL_DATETIME:
                case BinXmlToken.SQL_SMALLDATETIME:
                case BinXmlToken.XSD_TIME:
                case BinXmlToken.XSD_DATE:
                case BinXmlToken.XSD_DATETIME:
                case BinXmlToken.XSD_KATMAI_DATE:
                case BinXmlToken.XSD_KATMAI_DATETIME:
                case BinXmlToken.XSD_KATMAI_TIME:
                    return ValueAsDateTime();

                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                    return ValueAsDateTimeOffset();

                case BinXmlToken.XSD_QNAME:
                    {
                        int nameNum = ParseMB32(_tokDataPos);
                        if (nameNum < 0 || nameNum >= _symbolTables.qnameCount)
                            throw new XmlException(SR.XmlBin_InvalidQNameID, string.Empty);
                        QName qname = _symbolTables.qnametable[nameNum];
                        return new XmlQualifiedName(qname.localname, qname.namespaceUri);
                    }

                default:
                    throw ThrowUnexpectedToken(_token);
            }
        }

        private XmlValueConverter GetValueConverter(XmlTypeCode typeCode)
        {
            XmlSchemaSimpleType xsst = DatatypeImplementation.GetSimpleTypeFromTypeCode(typeCode);
            return xsst.ValueConverter;
        }

        private object ValueAs(BinXmlToken token, Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            object value;
            CheckValueTokenBounds();
            switch (token)
            {
                case BinXmlToken.SQL_NCHAR:
                case BinXmlToken.SQL_NVARCHAR:
                case BinXmlToken.SQL_NTEXT:
                    value = GetValueConverter(XmlTypeCode.UntypedAtomic).ChangeType(
                        GetString(_tokDataPos, _tokLen),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_BOOLEAN:
                    value = GetValueConverter(XmlTypeCode.Boolean).ChangeType(
                        (0 != _data[_tokDataPos]),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_BIT:
                    value = GetValueConverter(XmlTypeCode.NonNegativeInteger).ChangeType(
                        (int)_data[_tokDataPos],
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_TINYINT:
                    value = GetValueConverter(XmlTypeCode.UnsignedByte).ChangeType(
                        _data[_tokDataPos],
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_SMALLINT:
                    {
                        int v = GetInt16(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.Short).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.SQL_INT:
                    {
                        int v = GetInt32(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.Int).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.SQL_BIGINT:
                    {
                        long v = GetInt64(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.Long).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.XSD_BYTE:
                    {
                        value = GetValueConverter(XmlTypeCode.Byte).ChangeType(
                            (int)unchecked((sbyte)_data[_tokDataPos]),
                            returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.XSD_UNSIGNEDSHORT:
                    {
                        int v = GetUInt16(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.UnsignedShort).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.XSD_UNSIGNEDINT:
                    {
                        long v = GetUInt32(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.UnsignedInt).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.XSD_UNSIGNEDLONG:
                    {
                        decimal v = (decimal)GetUInt64(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.UnsignedLong).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.SQL_REAL:
                    {
                        float v = GetSingle(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.Float).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.SQL_FLOAT:
                    {
                        double v = GetDouble(_tokDataPos);
                        value = GetValueConverter(XmlTypeCode.Double).ChangeType(
                            v, returnType, namespaceResolver);
                        break;
                    }
                case BinXmlToken.SQL_UUID:
                    value = GetValueConverter(XmlTypeCode.String).ChangeType(
                        this.ValueAsString(token), returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_SMALLMONEY:
                    value = GetValueConverter(XmlTypeCode.Decimal).ChangeType(
                        (new BinXmlSqlMoney(GetInt32(_tokDataPos))).ToDecimal(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_MONEY:
                    value = GetValueConverter(XmlTypeCode.Decimal).ChangeType(
                        (new BinXmlSqlMoney(GetInt64(_tokDataPos))).ToDecimal(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_DECIMAL:
                case BinXmlToken.SQL_DECIMAL:
                case BinXmlToken.SQL_NUMERIC:
                    value = GetValueConverter(XmlTypeCode.Decimal).ChangeType(
                        (new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL)).ToDecimal(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.SQL_CHAR:
                case BinXmlToken.SQL_VARCHAR:
                case BinXmlToken.SQL_TEXT:
                    {
                        int pos = _tokDataPos;
                        int codepage = GetInt32(pos);
                        Encoding enc = System.Text.Encoding.GetEncoding(codepage);
                        value = GetValueConverter(XmlTypeCode.UntypedAtomic).ChangeType(
                            enc.GetString(_data, pos + 4, _tokLen - 4),
                            returnType, namespaceResolver);
                        break;
                    }

                case BinXmlToken.SQL_VARBINARY:
                case BinXmlToken.SQL_BINARY:
                case BinXmlToken.SQL_IMAGE:
                case BinXmlToken.SQL_UDT:
                case BinXmlToken.XSD_BASE64:
                case BinXmlToken.XSD_BINHEX:
                    {
                        byte[] data = new byte[_tokLen];
                        Array.Copy(_data, _tokDataPos, data, 0, _tokLen);
                        value = GetValueConverter(token == BinXmlToken.XSD_BINHEX ? XmlTypeCode.HexBinary : XmlTypeCode.Base64Binary).ChangeType(
                            data, returnType, namespaceResolver);
                        break;
                    }

                case BinXmlToken.SQL_DATETIME:
                case BinXmlToken.SQL_SMALLDATETIME:
                case BinXmlToken.XSD_DATETIME:
                case BinXmlToken.XSD_KATMAI_DATE:
                case BinXmlToken.XSD_KATMAI_DATETIME:
                case BinXmlToken.XSD_KATMAI_TIME:
                    value = GetValueConverter(XmlTypeCode.DateTime).ChangeType(
                        ValueAsDateTime(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_KATMAI_DATEOFFSET:
                case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
                case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
                    value = GetValueConverter(XmlTypeCode.DateTime).ChangeType(
                        ValueAsDateTimeOffset(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_TIME:
                    value = GetValueConverter(XmlTypeCode.Time).ChangeType(
                        ValueAsDateTime(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_DATE:
                    value = GetValueConverter(XmlTypeCode.Date).ChangeType(
                        ValueAsDateTime(),
                        returnType, namespaceResolver);
                    break;

                case BinXmlToken.XSD_QNAME:
                    {
                        int nameNum = ParseMB32(_tokDataPos);
                        if (nameNum < 0 || nameNum >= _symbolTables.qnameCount)
                            throw new XmlException(SR.XmlBin_InvalidQNameID, string.Empty);
                        QName qname = _symbolTables.qnametable[nameNum];
                        value = GetValueConverter(XmlTypeCode.QName).ChangeType(
                            new XmlQualifiedName(qname.localname, qname.namespaceUri),
                            returnType, namespaceResolver);
                        break;
                    }

                default:
                    throw ThrowUnexpectedToken(_token);
            }
            return value;
        }

        private short GetInt16(int pos)
        {
            byte[] data = _data;
            return (short)(data[pos] | data[pos + 1] << 8);
        }

        private ushort GetUInt16(int pos)
        {
            byte[] data = _data;
            return (ushort)(data[pos] | data[pos + 1] << 8);
        }

        private int GetInt32(int pos)
        {
            byte[] data = _data;
            return (int)(data[pos] | data[pos + 1] << 8 | data[pos + 2] << 16 | data[pos + 3] << 24);
        }

        private uint GetUInt32(int pos)
        {
            byte[] data = _data;
            return (uint)(data[pos] | data[pos + 1] << 8 | data[pos + 2] << 16 | data[pos + 3] << 24);
        }

        private long GetInt64(int pos)
        {
            byte[] data = _data;
            uint lo = (uint)(data[pos] | data[pos + 1] << 8 | data[pos + 2] << 16 | data[pos + 3] << 24);
            uint hi = (uint)(data[pos + 4] | data[pos + 5] << 8 | data[pos + 6] << 16 | data[pos + 7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        private ulong GetUInt64(int pos)
        {
            byte[] data = _data;
            uint lo = (uint)(data[pos] | data[pos + 1] << 8 | data[pos + 2] << 16 | data[pos + 3] << 24);
            uint hi = (uint)(data[pos + 4] | data[pos + 5] << 8 | data[pos + 6] << 16 | data[pos + 7] << 24);
            return (ulong)((ulong)hi) << 32 | lo;
        }

        private float GetSingle(int offset)
        {
            byte[] data = _data;
            uint tmp = (uint)(data[offset]
                            | data[offset + 1] << 8
                            | data[offset + 2] << 16
                            | data[offset + 3] << 24);
            unsafe
            {
                return *((float*)&tmp);
            }
        }

        private double GetDouble(int offset)
        {
            uint lo = (uint)(_data[offset + 0]
                            | _data[offset + 1] << 8
                            | _data[offset + 2] << 16
                            | _data[offset + 3] << 24);
            uint hi = (uint)(_data[offset + 4]
                            | _data[offset + 5] << 8
                            | _data[offset + 6] << 16
                            | _data[offset + 7] << 24);
            ulong tmp = ((ulong)hi) << 32 | lo;
            unsafe
            {
                return *((double*)&tmp);
            }
        }

        private Exception ThrowUnexpectedToken(BinXmlToken token)
        {
            System.Diagnostics.Debug.WriteLine("Unhandled token: " + token.ToString());
            return ThrowXmlException(SR.XmlBinary_UnexpectedToken);
        }

        private Exception ThrowXmlException(string res)
        {
            _state = ScanState.Error;
            return new XmlException(res, (string[])null);
        }

        // not currently used...
        //Exception ThrowXmlException(string res, string arg1) {
        //    this.state = ScanState.Error;
        //    return new XmlException(res, new string[] {arg1} );
        //}

        private Exception ThrowXmlException(string res, string arg1, string arg2)
        {
            _state = ScanState.Error;
            return new XmlException(res, new string[] { arg1, arg2 });
        }

        private Exception ThrowNotSupported(string res)
        {
            _state = ScanState.Error;
            return new NotSupportedException(res);
        }
    }
}