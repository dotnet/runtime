// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define XSLT2

using System.Collections.Generic;
using System.Diagnostics;
using StringConcat = System.Xml.Xsl.Runtime.StringConcat;

namespace System.Xml.Xsl.Xslt
{
    //         a) Forward only, one pass.
    //         b) You should call MoveToFirstChildren on nonempty element node. (or may be skip)

    internal sealed class XsltInput : IErrorHelper
    {
#if DEBUG
        private const int InitRecordsSize = 1;
#else
        private const int InitRecordsSize = 1 + 21;
#endif

        private readonly XmlReader _reader;
        private readonly IXmlLineInfo? _readerLineInfo;
        private readonly bool _topLevelReader;
        private readonly CompilerScopeManager<VarPar> _scopeManager;
        private readonly KeywordsTable _atoms;
        private readonly Compiler _compiler;
        private readonly bool _reatomize;

        // Cached properties. MoveTo* functions set them.
        private XmlNodeType _nodeType;
        private Record[] _records = new Record[InitRecordsSize];
        private int _currentRecord;
        private bool _isEmptyElement;
        private int _lastTextNode;
        private int _numAttributes;
        private ContextInfo? _ctxInfo;
        private bool _attributesRead;

        public XsltInput(XmlReader reader, Compiler compiler, KeywordsTable atoms)
        {
            Debug.Assert(reader != null);
            Debug.Assert(atoms != null);
            EnsureExpandEntities(reader);
            IXmlLineInfo? xmlLineInfo = reader as IXmlLineInfo;

            _atoms = atoms;
            _reader = reader;
            _reatomize = reader.NameTable != atoms.NameTable;
            _readerLineInfo = (xmlLineInfo != null && xmlLineInfo.HasLineInfo()) ? xmlLineInfo : null;
            _topLevelReader = reader.ReadState == ReadState.Initial;
            _scopeManager = new CompilerScopeManager<VarPar>(atoms);
            _compiler = compiler;
            _nodeType = XmlNodeType.Document;
        }

        // Cached properties
        public XmlNodeType NodeType { get { return _nodeType == XmlNodeType.Element && 0 < _currentRecord ? XmlNodeType.Attribute : _nodeType; } }
        public string LocalName { get { return _records[_currentRecord].localName; } }
        public string NamespaceUri { get { return _records[_currentRecord].nsUri; } }
        public string Prefix { get { return _records[_currentRecord].prefix; } }
        public string Value { get { return _records[_currentRecord].value; } }
        public string? BaseUri { get { return _records[_currentRecord].baseUri; } }
        public string QualifiedName { get { return _records[_currentRecord].QualifiedName; } }
        public bool IsEmptyElement { get { return _isEmptyElement; } }

        public string? Uri { get { return _records[_currentRecord].baseUri; } }
        public Location Start { get { return _records[_currentRecord].start; } }
        public Location End { get { return _records[_currentRecord].end; } }

        private static void EnsureExpandEntities(XmlReader reader)
        {
            XmlTextReader? tr = reader as XmlTextReader;
            if (tr != null && tr.EntityHandling != EntityHandling.ExpandEntities)
            {
                Debug.Assert(tr.Settings == null, "XmlReader created with XmlReader.Create should always expand entities.");
                tr.EntityHandling = EntityHandling.ExpandEntities;
            }
        }

        private void ExtendRecordBuffer(int position)
        {
            if (_records.Length <= position)
            {
                int newSize = _records.Length * 2;
                if (newSize <= position)
                {
                    newSize = position + 1;
                }
                Record[] tmp = new Record[newSize];
                Array.Copy(_records, tmp, _records.Length);
                _records = tmp;
            }
        }

        public bool FindStylesheetElement()
        {
            if (!_topLevelReader)
            {
                if (_reader.ReadState != ReadState.Interactive)
                {
                    return false;
                }
            }

            // The stylesheet may be an embedded stylesheet. If this is the case the reader will be in Interactive state and should be
            // positioned on xsl:stylesheet element (or any preceding whitespace) but there also can be namespaces defined on one
            // of the ancestor nodes. These namespace definitions have to be copied to the xsl:stylesheet element scope. Otherwise it
            // will not be possible to resolve them later and loading the stylesheet will end up with throwing an exception.
            IDictionary<string, string>? namespacesInScope = null;
            if (_reader.ReadState == ReadState.Interactive)
            {
                // This may be an embedded stylesheet - store namespaces in scope
                IXmlNamespaceResolver? nsResolver = _reader as IXmlNamespaceResolver;
                if (nsResolver != null)
                {
                    namespacesInScope = nsResolver.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
                }
            }

            while (MoveToNextSibling() && _nodeType == XmlNodeType.Whitespace) ;

            // An Element node was reached. Potentially this is xsl:stylesheet instruction.
            if (_nodeType == XmlNodeType.Element)
            {
                // If namespacesInScope is not null then the stylesheet being read is an embedded stylesheet that can have namespaces
                // defined outside of xsl:stylesheet instruction. In this case the namespace definitions collected above have to be added
                // to the element scope.
                if (namespacesInScope != null)
                {
                    foreach (KeyValuePair<string, string> prefixNamespacePair in namespacesInScope)
                    {
                        // The namespace could be redefined on the element we just read. If this is the case scopeManager already has
                        // namespace definition for this prefix and the old definition must not be added to the scope.
                        if (_scopeManager.LookupNamespace(prefixNamespacePair.Key) == null)
                        {
                            string nsAtomizedValue = _atoms.NameTable.Add(prefixNamespacePair.Value);
                            _scopeManager.AddNsDeclaration(prefixNamespacePair.Key, nsAtomizedValue);
                            _ctxInfo!.AddNamespace(prefixNamespacePair.Key, nsAtomizedValue);
                        }
                    }
                }

                // return true to indicate that we reached XmlNodeType.Element node - potentially xsl:stylesheet element.
                return true;
            }

            // return false to indicate that we did not reach XmlNodeType.Element node so it is not a valid stylesheet.
            return false;
        }

        public void Finish()
        {
            _scopeManager.CheckEmpty();

            if (_topLevelReader)
            {
                while (_reader.ReadState == ReadState.Interactive)
                {
                    _reader.Skip();
                }
            }
        }

        private void FillupRecord(ref Record rec)
        {
            rec.localName = _reader.LocalName;
            rec.nsUri = _reader.NamespaceURI;
            rec.prefix = _reader.Prefix;
            rec.value = _reader.Value;
            rec.baseUri = _reader.BaseURI;

            if (_reatomize)
            {
                rec.localName = _atoms.NameTable.Add(rec.localName);
                rec.nsUri = _atoms.NameTable.Add(rec.nsUri);
                rec.prefix = _atoms.NameTable.Add(rec.prefix);
            }

            if (_readerLineInfo != null)
            {
                rec.start = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - PositionAdjustment(_reader.NodeType));
            }
        }

        private void SetRecordEnd(ref Record rec)
        {
            if (_readerLineInfo != null)
            {
                rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - PositionAdjustment(_reader.NodeType));
                if (_reader.BaseURI != rec.baseUri || rec.end.LessOrEqual(rec.start))
                {
                    rec.end = new Location(rec.start.Line, int.MaxValue);
                }
            }
        }

        private void FillupTextRecord(ref Record rec)
        {
            Debug.Assert(
                _reader.NodeType == XmlNodeType.Whitespace || _reader.NodeType == XmlNodeType.SignificantWhitespace ||
                _reader.NodeType == XmlNodeType.Text || _reader.NodeType == XmlNodeType.CDATA
            );
            rec.localName = string.Empty;
            rec.nsUri = string.Empty;
            rec.prefix = string.Empty;
            rec.value = _reader.Value;
            rec.baseUri = _reader.BaseURI;

            if (_readerLineInfo != null)
            {
                bool isCDATA = (_reader.NodeType == XmlNodeType.CDATA);
                int line = _readerLineInfo.LineNumber;
                int pos = _readerLineInfo.LinePosition;
                rec.start = new Location(line, pos - (isCDATA ? 9 : 0));
                char prevChar = ' ';
                foreach (char ch in rec.value)
                {
                    switch (ch)
                    {
                        case '\n':
                            if (prevChar != '\r')
                            {
                                goto case '\r';
                            }
                            break;
                        case '\r':
                            line++;
                            pos = 1;
                            break;
                        default:
                            pos++;
                            break;
                    }
                    prevChar = ch;
                }
                rec.end = new Location(line, pos + (isCDATA ? 3 : 0));
            }
        }

        private void FillupCharacterEntityRecord(ref Record rec)
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.EntityReference);
            string local = _reader.LocalName;
            Debug.Assert(local[0] == '#' || local == "lt" || local == "gt" || local == "quot" || local == "apos");
            rec.localName = string.Empty;
            rec.nsUri = string.Empty;
            rec.prefix = string.Empty;
            rec.baseUri = _reader.BaseURI;

            if (_readerLineInfo != null)
            {
                rec.start = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - 1);
            }
            _reader.ResolveEntity();
            _reader.Read();
            Debug.Assert(_reader.NodeType == XmlNodeType.Text || _reader.NodeType == XmlNodeType.Whitespace || _reader.NodeType == XmlNodeType.SignificantWhitespace);
            rec.value = _reader.Value;
            _reader.Read();
            Debug.Assert(_reader.NodeType == XmlNodeType.EndEntity);
            if (_readerLineInfo != null)
            {
                rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + 1);
            }
        }

        private StringConcat _strConcat;

        // returns false if attribute is actualy namespace
        private bool ReadAttribute(ref Record rec)
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.Attribute, "reader.NodeType == XmlNodeType.Attribute");
            FillupRecord(ref rec);
            if (Ref.Equal(rec.prefix, _atoms.Xmlns))
            {                                      // xmlns:foo="NS_FOO"
                string atomizedValue = _atoms.NameTable.Add(_reader.Value);
                if (!Ref.Equal(rec.localName, _atoms.Xml))
                {
                    _scopeManager.AddNsDeclaration(rec.localName, atomizedValue);
                    _ctxInfo!.AddNamespace(rec.localName, atomizedValue);
                }
                return false;
            }
            else if (rec.prefix.Length == 0 && Ref.Equal(rec.localName, _atoms.Xmlns))
            {  // xmlns="NS_FOO"
                string atomizedValue = _atoms.NameTable.Add(_reader.Value);
                _scopeManager.AddNsDeclaration(string.Empty, atomizedValue);
                _ctxInfo!.AddNamespace(string.Empty, atomizedValue);
                return false;
            }
            /* Read Attribute Value */
            {
                if (!_reader.ReadAttributeValue())
                {
                    // XmlTextReader never returns false from first call to ReadAttributeValue()
                    rec.value = string.Empty;
                    SetRecordEnd(ref rec);
                    return true;
                }
                if (_readerLineInfo != null)
                {
                    int correction = (_reader.NodeType == XmlNodeType.EntityReference) ? -2 : -1;
                    rec.valueStart = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + correction);
                    if (_reader.BaseURI != rec.baseUri || rec.valueStart.LessOrEqual(rec.start))
                    {
                        int nameLength = ((rec.prefix.Length != 0) ? rec.prefix.Length + 1 : 0) + rec.localName.Length;
                        rec.end = new Location(rec.start.Line, rec.start.Pos + nameLength + 1);
                    }
                }
                string lastText = string.Empty;
                _strConcat.Clear();
                do
                {
                    switch (_reader.NodeType)
                    {
                        case XmlNodeType.EntityReference:
                            _reader.ResolveEntity();
                            break;
                        case XmlNodeType.EndEntity:
                            break;
                        default:
                            Debug.Assert(_reader.NodeType == XmlNodeType.Text, "Unexpected node type inside attribute value");
                            lastText = _reader.Value;
                            _strConcat.Concat(lastText);
                            break;
                    }
                } while (_reader.ReadAttributeValue());
                rec.value = _strConcat.GetResult();
                if (_readerLineInfo != null)
                {
                    Debug.Assert(_reader.NodeType != XmlNodeType.EntityReference);
                    int correction = ((_reader.NodeType == XmlNodeType.EndEntity) ? 1 : lastText.Length) + 1;
                    rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + correction);
                    if (_reader.BaseURI != rec.baseUri || rec.end.LessOrEqual(rec.valueStart))
                    {
                        rec.end = new Location(rec.start.Line, int.MaxValue);
                    }
                }
            }
            return true;
        }

        // --------------------

        public bool MoveToFirstChild()
        {
            Debug.Assert(_nodeType == XmlNodeType.Element, "To call MoveToFirstChild() XsltInut should be positioned on an Element.");
            if (IsEmptyElement)
            {
                return false;
            }
            return ReadNextSibling();
        }

        public bool MoveToNextSibling()
        {
            Debug.Assert(_nodeType != XmlNodeType.Element || IsEmptyElement, "On non-empty elements we should call MoveToFirstChild()");
            if (_nodeType == XmlNodeType.Element || _nodeType == XmlNodeType.EndElement)
            {
                _scopeManager.ExitScope();
            }
            return ReadNextSibling();
        }

        public void SkipNode()
        {
            if (_nodeType == XmlNodeType.Element && MoveToFirstChild())
            {
                do
                {
                    SkipNode();
                } while (MoveToNextSibling());
            }
        }

        private int ReadTextNodes()
        {
            bool textPreserveWS = _reader.XmlSpace == XmlSpace.Preserve;
            bool textIsWhite = true;
            int curTextNode = 0;
            do
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Text:
                    // XLinq reports WS nodes as Text so we need to analyze them here
                    case XmlNodeType.CDATA:
                        if (textIsWhite && !XmlCharType.IsOnlyWhitespace(_reader.Value))
                        {
                            textIsWhite = false;
                        }
                        goto case XmlNodeType.SignificantWhitespace;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        ExtendRecordBuffer(curTextNode);
                        FillupTextRecord(ref _records[curTextNode]);
                        _reader.Read();
                        curTextNode++;
                        break;
                    case XmlNodeType.EntityReference:
                        string local = _reader.LocalName;
                        if (local.Length > 0 && (
                            local[0] == '#' ||
                            local == "lt" || local == "gt" || local == "quot" || local == "apos"
                        ))
                        {
                            // Special treatment for character and built-in entities
                            ExtendRecordBuffer(curTextNode);
                            FillupCharacterEntityRecord(ref _records[curTextNode]);
                            if (textIsWhite && !XmlCharType.IsOnlyWhitespace(_records[curTextNode].value))
                            {
                                textIsWhite = false;
                            }
                            curTextNode++;
                        }
                        else
                        {
                            _reader.ResolveEntity();
                            _reader.Read();
                        }
                        break;
                    case XmlNodeType.EndEntity:
                        _reader.Read();
                        break;
                    default:
                        _nodeType = (
                            !textIsWhite ? XmlNodeType.Text :
                            textPreserveWS ? XmlNodeType.SignificantWhitespace :
                            /*default:    */ XmlNodeType.Whitespace
                        );
                        return curTextNode;
                }
            } while (true);
        }

        private bool ReadNextSibling()
        {
            if (_currentRecord < _lastTextNode)
            {
                Debug.Assert(_nodeType == XmlNodeType.Text || _nodeType == XmlNodeType.Whitespace || _nodeType == XmlNodeType.SignificantWhitespace);
                _currentRecord++;
                if (_currentRecord == _lastTextNode)
                {
                    _lastTextNode = 0;  // we are done with text nodes. Reset this counter
                }
                return true;
            }
            _currentRecord = 0;
            while (!_reader.EOF)
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.EntityReference:
                        int numTextNodes = ReadTextNodes();
                        if (numTextNodes == 0)
                        {
                            // Most likely this was Entity that starts from non-text node
                            continue;
                        }
                        _lastTextNode = numTextNodes - 1;
                        return true;
                    case XmlNodeType.Element:
                        _scopeManager.EnterScope();
                        _numAttributes = ReadElement();
                        return true;
                    case XmlNodeType.EndElement:
                        _nodeType = XmlNodeType.EndElement;
                        _isEmptyElement = false;
                        FillupRecord(ref _records[0]);
                        _reader.Read();
                        SetRecordEnd(ref _records[0]);
                        return false;
                    default:
                        _reader.Read();
                        break;
                }
            }
            return false;
        }

        private int ReadElement()
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.Element);

            _attributesRead = false;
            FillupRecord(ref _records[0]);
            _nodeType = XmlNodeType.Element;
            _isEmptyElement = _reader.IsEmptyElement;
            _ctxInfo = new ContextInfo(this);

            int record = 1;
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    ExtendRecordBuffer(record);
                    if (ReadAttribute(ref _records[record]))
                    {
                        record++;
                    }
                } while (_reader.MoveToNextAttribute());
                _reader.MoveToElement();
            }
            _reader.Read();
            SetRecordEnd(ref _records[0]);
            _ctxInfo.lineInfo = BuildLineInfo();
            _attributes = null;
            return record - 1;
        }

        public void MoveToElement()
        {
            Debug.Assert(_nodeType == XmlNodeType.Element, "For MoveToElement() we should be positioned on Element or Attribute");
            _currentRecord = 0;
        }

        private bool MoveToAttributeBase(int attNum)
        {
            Debug.Assert(_nodeType == XmlNodeType.Element, "For MoveToLiteralAttribute() we should be positioned on Element or Attribute");
            if (0 < attNum && attNum <= _numAttributes)
            {
                _currentRecord = attNum;
                return true;
            }
            else
            {
                _currentRecord = 0;
                return false;
            }
        }

        public bool MoveToLiteralAttribute(int attNum)
        {
            Debug.Assert(_nodeType == XmlNodeType.Element, "For MoveToLiteralAttribute() we should be positioned on Element or Attribute");
            if (0 < attNum && attNum <= _numAttributes)
            {
                _currentRecord = attNum;
                return true;
            }
            else
            {
                _currentRecord = 0;
                return false;
            }
        }

        public bool MoveToXsltAttribute(int attNum, string attName)
        {
            Debug.Assert(_attributes != null && _attributes[attNum].name == attName, "Attribute numbering error.");
            _currentRecord = _xsltAttributeNumber[attNum];
            return _currentRecord != 0;
        }

        public bool IsRequiredAttribute(int attNum)
        {
            return (_attributes![attNum].flags & (_compiler.Version == 2 ? XsltLoader.V2Req : XsltLoader.V1Req)) != 0;
        }

        public bool AttributeExists(int attNum, string attName)
        {
            Debug.Assert(_attributes != null && _attributes[attNum].name == attName, "Attribute numbering error.");
            return _xsltAttributeNumber[attNum] != 0;
        }

        public struct DelayedQName
        {
            private readonly string _prefix;
            private readonly string _localName;
            public DelayedQName(ref Record rec)
            {
                _prefix = rec.prefix;
                _localName = rec.localName;
            }
            public static implicit operator string(DelayedQName qn)
            {
                return qn._prefix.Length == 0 ? qn._localName : (qn._prefix + ':' + qn._localName);
            }
        }

        public DelayedQName ElementName
        {
            get
            {
                Debug.Assert(_nodeType == XmlNodeType.Element || _nodeType == XmlNodeType.EndElement, "Input is positioned on element or attribute");
                return new DelayedQName(ref _records[0]);
            }
        }

        // -------------------- Keywords testing --------------------

        public bool IsNs(string ns) { return Ref.Equal(ns, NamespaceUri); }
        public bool IsKeyword(string kwd) { return Ref.Equal(kwd, LocalName); }
        public bool IsXsltNamespace() { return IsNs(_atoms.UriXsl); }
        public bool IsNullNamespace() { return IsNs(string.Empty); }
        public bool IsXsltKeyword(string kwd) { return IsKeyword(kwd) && IsXsltNamespace(); }

        // -------------------- Scope Management --------------------
        // See private class InputScopeManager bellow.
        // InputScopeManager handles some flags and values with respect of scope level where they as defined.
        // To parse XSLT style sheet we need the folloing values:
        //  BackwardCompatibility -- this flag is set when compiler.version==2 && xsl:version<2.
        //  ForwardCompatibility  -- this flag is set when compiler.version==2 && xsl:version>1 or compiler.version==1 && xsl:version!=1
        //  CanHaveApplyImports  -- we allow xsl:apply-templates instruction to apear in any template with match!=null, but not inside xsl:for-each
        //                          so it can't be inside global variable and has initial value = false
        //  ExtensionNamespace   -- is defined by extension-element-prefixes attribute on LRE or xsl:stylesheet

        public bool CanHaveApplyImports
        {
            get { return _scopeManager.CanHaveApplyImports; }
            set { _scopeManager.CanHaveApplyImports = value; }
        }

        public bool IsExtensionNamespace(string uri)
        {
            Debug.Assert(_nodeType != XmlNodeType.Element || _attributesRead, "Should first read attributes");
            return _scopeManager.IsExNamespace(uri);
        }

        public bool ForwardCompatibility
        {
            get
            {
                Debug.Assert(_nodeType != XmlNodeType.Element || _attributesRead, "Should first read attributes");
                return _scopeManager.ForwardCompatibility;
            }
        }

        public bool BackwardCompatibility
        {
            get
            {
                Debug.Assert(_nodeType != XmlNodeType.Element || _attributesRead, "Should first read attributes");
                return _scopeManager.BackwardCompatibility;
            }
        }

        public XslVersion XslVersion
        {
            get { return _scopeManager.ForwardCompatibility ? XslVersion.ForwardsCompatible : XslVersion.Current; }
        }

        private void SetVersion(int attVersion)
        {
            MoveToLiteralAttribute(attVersion);
            Debug.Assert(IsKeyword(_atoms.Version));
            double version = XPathConvert.StringToDouble(Value);
            if (double.IsNaN(version))
            {
                ReportError(/*[XT0110]*/SR.Xslt_InvalidAttrValue, _atoms.Version, Value);
#if XSLT2
                version = 2.0;
#else
                version = 1.0;
#endif
            }
            SetVersion(version);
        }
        private void SetVersion(double version)
        {
            if (_compiler.Version == 0)
            {
#if XSLT2
                compiler.Version = version < 2.0 ? 1 : 2;
#else
                _compiler.Version = 1;
#endif
            }

            if (_compiler.Version == 1)
            {
                _scopeManager.BackwardCompatibility = false;
                _scopeManager.ForwardCompatibility = (version != 1.0);
            }
            else
            {
                _scopeManager.BackwardCompatibility = version < 2;
                _scopeManager.ForwardCompatibility = 2 < version;
            }
        }

        // --------------- GetAttributes(...) -------------------------
        // All Xslt Instructions allows fixed set of attributes in null-ns, no in XSLT-ns and any in other ns.
        // In ForwardCompatibility mode we should ignore any of this problems.
        // We not use these functions for parseing LiteralResultElement and xsl:stylesheet

        public struct XsltAttribute
        {
            public string name;
            public int flags;
            public XsltAttribute(string name, int flags)
            {
                this.name = name;
                this.flags = flags;
            }
        }

        private XsltAttribute[]? _attributes;
        // Mapping of attribute names as they ordered in 'attributes' array
        // to there's numbers in actual stylesheet as they ordered in 'records' array
        private readonly int[] _xsltAttributeNumber = new int[21];

        public ContextInfo GetAttributes()
        {
            return GetAttributes(Array.Empty<XsltAttribute>());
        }

        public ContextInfo GetAttributes(XsltAttribute[] attributes)
        {
            Debug.Assert(NodeType == XmlNodeType.Element);
            Debug.Assert(attributes.Length <= _xsltAttributeNumber.Length);
            _attributes = attributes;
            // temp hack to fix value? = new AttValue(records[values[?]].value);
            _records[0].value = null!;

            // Standard Attributes:
            int attExtension = 0;
            int attExclude = 0;
            int attNamespace = 0;
            int attCollation = 0;
            int attUseWhen = 0;

            bool isXslOutput = IsXsltNamespace() && IsKeyword(_atoms.Output);
            bool SS = IsXsltNamespace() && (IsKeyword(_atoms.Stylesheet) || IsKeyword(_atoms.Transform));
            bool V2 = _compiler.Version == 2;

            for (int i = 0; i < attributes.Length; i++)
            {
                _xsltAttributeNumber[i] = 0;
            }

            _compiler.EnterForwardsCompatible();
            if (SS || V2 && !isXslOutput)
            {
                for (int i = 1; MoveToAttributeBase(i); i++)
                {
                    if (IsNullNamespace() && IsKeyword(_atoms.Version))
                    {
                        SetVersion(i);
                        break;
                    }
                }
            }
            if (_compiler.Version == 0)
            {
                Debug.Assert(SS, "First we parse xsl:stylesheet element");
#if XSLT2
                SetVersion(2.0);
#else
                SetVersion(1.0);
#endif
            }
            V2 = _compiler.Version == 2;
            int OptOrReq = V2 ? XsltLoader.V2Opt | XsltLoader.V2Req : XsltLoader.V1Opt | XsltLoader.V1Req;

            for (int attNum = 1; MoveToAttributeBase(attNum); attNum++)
            {
                if (IsNullNamespace())
                {
                    string localName = LocalName;
                    int kwd;
                    for (kwd = 0; kwd < attributes.Length; kwd++)
                    {
                        if (Ref.Equal(localName, attributes[kwd].name) && (attributes[kwd].flags & OptOrReq) != 0)
                        {
                            _xsltAttributeNumber[kwd] = attNum;
                            break;
                        }
                    }

                    if (kwd == attributes.Length)
                    {
                        if (Ref.Equal(localName, _atoms.ExcludeResultPrefixes) && (SS || V2)) { attExclude = attNum; }
                        else
                        if (Ref.Equal(localName, _atoms.ExtensionElementPrefixes) && (SS || V2)) { attExtension = attNum; }
                        else
                        if (Ref.Equal(localName, _atoms.XPathDefaultNamespace) && (V2)) { attNamespace = attNum; }
                        else
                        if (Ref.Equal(localName, _atoms.DefaultCollation) && (V2)) { attCollation = attNum; }
                        else
                        if (Ref.Equal(localName, _atoms.UseWhen) && (V2)) { attUseWhen = attNum; }
                        else
                        {
                            ReportError(/*[XT0090]*/SR.Xslt_InvalidAttribute, QualifiedName, _records[0].QualifiedName);
                        }
                    }
                }
                else if (IsXsltNamespace())
                {
                    ReportError(/*[XT0090]*/SR.Xslt_InvalidAttribute, QualifiedName, _records[0].QualifiedName);
                }
                else
                {
                    // Ignore the attribute.
                    // An element from the XSLT namespace may have any attribute not from the XSLT namespace,
                    // provided that the expanded-name of the attribute has a non-null namespace URI.
                    // For example, it may be 'xml:space'.
                }
            }

            _attributesRead = true;

            // Ignore invalid attributes if forwards-compatible behavior is enabled. Note that invalid
            // attributes may encounter before ForwardCompatibility flag is set to true. For example,
            // <xsl:stylesheet unknown="foo" version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"/>
            _compiler.ExitForwardsCompatible(ForwardCompatibility);

            InsertExNamespaces(attExtension, _ctxInfo!, /*extensions:*/ true);
            InsertExNamespaces(attExclude, _ctxInfo!, /*extensions:*/ false);
            SetXPathDefaultNamespace(attNamespace);
            SetDefaultCollation(attCollation);
            if (attUseWhen != 0)
            {
                ReportNYI(_atoms.UseWhen);
            }

            MoveToElement();
            // Report missing mandatory attributes
            for (int i = 0; i < attributes.Length; i++)
            {
                if (_xsltAttributeNumber[i] == 0)
                {
                    int flags = attributes[i].flags;
                    if (
                        _compiler.Version == 2 && (flags & XsltLoader.V2Req) != 0 ||
                        _compiler.Version == 1 && (flags & XsltLoader.V1Req) != 0 && (!ForwardCompatibility || (flags & XsltLoader.V2Req) != 0)
                    )
                    {
                        ReportError(/*[XT_001]*/SR.Xslt_MissingAttribute, attributes[i].name);
                    }
                }
            }

            return _ctxInfo!;
        }

        public ContextInfo GetLiteralAttributes(bool asStylesheet)
        {
            Debug.Assert(NodeType == XmlNodeType.Element);

            // Standard Attributes:
            int attVersion = 0;
            int attExtension = 0;
            int attExclude = 0;
            int attNamespace = 0;
            int attCollation = 0;
            int attUseWhen = 0;

            for (int i = 1; MoveToLiteralAttribute(i); i++)
            {
                if (IsXsltNamespace())
                {
                    string localName = LocalName;
                    if (Ref.Equal(localName, _atoms.Version)) { attVersion = i; }
                    else
                    if (Ref.Equal(localName, _atoms.ExtensionElementPrefixes)) { attExtension = i; }
                    else
                    if (Ref.Equal(localName, _atoms.ExcludeResultPrefixes)) { attExclude = i; }
                    else
                    if (Ref.Equal(localName, _atoms.XPathDefaultNamespace)) { attNamespace = i; }
                    else
                    if (Ref.Equal(localName, _atoms.DefaultCollation)) { attCollation = i; }
                    else
                    if (Ref.Equal(localName, _atoms.UseWhen)) { attUseWhen = i; }
                }
            }

            _attributesRead = true;
            this.MoveToElement();

            if (attVersion != 0)
            {
                // Enable forwards-compatible behavior if version attribute is not "1.0"
                SetVersion(attVersion);
            }
            else
            {
                if (asStylesheet)
                {
                    ReportError(Ref.Equal(NamespaceUri, _atoms.UriWdXsl) && Ref.Equal(LocalName, _atoms.Stylesheet) ?
                        /*[XT_025]*/SR.Xslt_WdXslNamespace : /*[XT0150]*/SR.Xslt_WrongStylesheetElement
                    );
#if XSLT2
                    SetVersion(2.0);
#else
                    SetVersion(1.0);
#endif
                }
            }

            // Parse xsl:extension-element-prefixes attribute (now that forwards-compatible mode is known)
            InsertExNamespaces(attExtension, _ctxInfo!, /*extensions:*/true);

            if (!IsExtensionNamespace(_records[0].nsUri))
            {
                // Parse other attributes (now that it's known this is a literal result element)
                if (_compiler.Version == 2)
                {
                    SetXPathDefaultNamespace(attNamespace);
                    SetDefaultCollation(attCollation);
                    if (attUseWhen != 0)
                    {
                        ReportNYI(_atoms.UseWhen);
                    }
                }

                InsertExNamespaces(attExclude, _ctxInfo!, /*extensions:*/false);
            }

            return _ctxInfo!;
        }

        // Get just the 'version' attribute of an unknown XSLT instruction. All other attributes
        // are ignored since we do not want to report an error on each of them.
        public void GetVersionAttribute()
        {
            Debug.Assert(NodeType == XmlNodeType.Element && IsXsltNamespace());
            bool V2 = _compiler.Version == 2;

            if (V2)
            {
                for (int i = 1; MoveToAttributeBase(i); i++)
                {
                    if (IsNullNamespace() && IsKeyword(_atoms.Version))
                    {
                        SetVersion(i);
                        break;
                    }
                }
            }
            _attributesRead = true;
        }

        private void InsertExNamespaces(int attExPrefixes, ContextInfo ctxInfo, bool extensions)
        {
            // List of Extension namespaces are maintaned by XsltInput's ScopeManager and is used by IsExtensionNamespace() in XsltLoader.LoadLiteralResultElement()
            // Both Extension and Exclusion namespaces will not be coppied by LiteralResultElement. Logic of copping namespaces are in QilGenerator.CompileLiteralElement().
            // At this time we will have different scope manager and need preserve all required information from load time to compile time.
            // Each XslNode contains list of NsDecls (nsList) which stores prefix+namespaces pairs for each namespace decls as well as exclusion namespaces.
            // In addition it also contains Exclusion namespace. They are represented as (null+namespace). Special case is Exlusion "#all" represented as (null+null).
            //and Exclusion namespace
            if (MoveToLiteralAttribute(attExPrefixes))
            {
                Debug.Assert(extensions ? IsKeyword(_atoms.ExtensionElementPrefixes) : IsKeyword(_atoms.ExcludeResultPrefixes));
                string value = Value;
                if (value.Length != 0)
                {
                    if (!extensions && _compiler.Version != 1 && value == "#all")
                    {
                        ctxInfo.nsList = new NsDecl(ctxInfo.nsList, /*prefix:*/null, /*nsUri:*/null);    // null, null means Exlusion #all
                    }
                    else
                    {
                        _compiler.EnterForwardsCompatible();
                        string[] list = XmlConvert.SplitString(value);
                        for (int idx = 0; idx < list.Length; idx++)
                        {
                            if (list[idx] == "#default")
                            {
                                list[idx] = this.LookupXmlNamespace(string.Empty)!;
                                if (list[idx].Length == 0 && _compiler.Version != 1 && !BackwardCompatibility)
                                {
                                    ReportError(/*[XTSE0809]*/SR.Xslt_ExcludeDefault);
                                }
                            }
                            else
                            {
                                list[idx] = this.LookupXmlNamespace(list[idx])!;
                            }
                        }
                        if (!_compiler.ExitForwardsCompatible(this.ForwardCompatibility))
                        {
                            // There were errors in the list, ignore the whole list
                            return;
                        }

                        for (int idx = 0; idx < list.Length; idx++)
                        {
                            if (list[idx] != null)
                            {
                                ctxInfo.nsList = new NsDecl(ctxInfo.nsList, /*prefix:*/null, list[idx]); // null means that this Exlusion NS
                                if (extensions)
                                {
                                    _scopeManager.AddExNamespace(list[idx]);                         // At Load time we need to know Extencion namespaces to ignore such literal elements.
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetXPathDefaultNamespace(int attNamespace)
        {
            if (MoveToLiteralAttribute(attNamespace))
            {
                Debug.Assert(IsKeyword(_atoms.XPathDefaultNamespace));
                if (Value.Length != 0)
                {
                    ReportNYI(_atoms.XPathDefaultNamespace);
                }
            }
        }

        private void SetDefaultCollation(int attCollation)
        {
            if (MoveToLiteralAttribute(attCollation))
            {
                Debug.Assert(IsKeyword(_atoms.DefaultCollation));
                string[] list = XmlConvert.SplitString(Value);
                int col;
                for (col = 0; col < list.Length; col++)
                {
                    if (System.Xml.Xsl.Runtime.XmlCollation.Create(list[col], /*throw:*/false) != null)
                    {
                        break;
                    }
                }
                if (col == list.Length)
                {
                    ReportErrorFC(/*[XTSE0125]*/SR.Xslt_CollationSyntax);
                }
                else
                {
                    if (list[col] != XmlReservedNs.NsCollCodePoint)
                    {
                        ReportNYI(_atoms.DefaultCollation);
                    }
                }
            }
        }

        // ----------------------- ISourceLineInfo -----------------------

        private static int PositionAdjustment(XmlNodeType nt) =>
            nt switch
            {
                XmlNodeType.Element => 1,               // "<"
                XmlNodeType.CDATA => 9,                 // "<![CDATA["
                XmlNodeType.ProcessingInstruction => 2, // "<?"
                XmlNodeType.Comment => 4,               // "<!--"
                XmlNodeType.EndElement => 2,            // "</"
                XmlNodeType.EntityReference => 1,       // "&"
                _ => 0,
            };

        public ISourceLineInfo BuildLineInfo()
        {
            return new SourceLineInfo(Uri, Start, End);
        }

        public ISourceLineInfo BuildNameLineInfo()
        {
            if (_readerLineInfo == null)
            {
                return BuildLineInfo();
            }

            // LocalName is checked against null since it is used to calculate QualifiedName used in turn to
            // calculate end position.
            // LocalName (and other cached properties) can be null only if nothing has been read from the reader.
            // This happens for instance when a reader which has already been closed or a reader positioned
            // on the very last node of the document is passed to the ctor.
            if (LocalName == null)
            {
                // Fill up the current record to set all the properties used below.
                FillupRecord(ref _records[_currentRecord]);
            }

            Location start = Start;
            int line = start.Line;
            int pos = start.Pos + PositionAdjustment(NodeType);
            return new SourceLineInfo(Uri, new Location(line, pos), new Location(line, pos + QualifiedName.Length));
        }

        public ISourceLineInfo BuildReaderLineInfo()
        {
            Location loc;

            if (_readerLineInfo != null)
                loc = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition);
            else
                loc = new Location(0, 0);

            return new SourceLineInfo(_reader.BaseURI!, loc, loc);
        }

        // Resolve prefix, return null and report an error if not found
        public string? LookupXmlNamespace(string prefix)
        {
            Debug.Assert(prefix != null);
            string? nsUri = _scopeManager.LookupNamespace(prefix);
            if (nsUri != null)
            {
                Debug.Assert(Ref.Equal(_atoms.NameTable.Get(nsUri), nsUri), "Namespaces must be atomized");
                return nsUri;
            }
            if (prefix.Length == 0)
            {
                return string.Empty;
            }
            ReportError(/*[XT0280]*/SR.Xslt_InvalidPrefix, prefix);
            return null;
        }

        // ---------------------- Error Handling ----------------------

        public void ReportError(string res, params string?[]? args)
        {
            _compiler.ReportError(BuildNameLineInfo(), res, args);
        }

        public void ReportWarning(string res, params string?[]? args)
        {
            _compiler.ReportWarning(BuildNameLineInfo(), res, args);
        }

        public void ReportErrorFC(string res, params string[] args)
        {
            if (!ForwardCompatibility)
            {
                _compiler.ReportError(BuildNameLineInfo(), res, args);
            }
        }

        private void ReportNYI(string arg)
        {
            ReportErrorFC(SR.Xslt_NotYetImplemented, arg);
        }

        // -------------------------------- ContextInfo ------------------------------------

        internal sealed class ContextInfo
        {
            public NsDecl? nsList;
            public ISourceLineInfo? lineInfo;       // Line info for whole start tag
            public ISourceLineInfo? elemNameLi;     // Line info for element name
            public ISourceLineInfo? endTagLi;       // Line info for end tag or '/>'
            private readonly int _elemNameLength;

            // Create ContextInfo based on existing line info (used during AST rewriting)
            internal ContextInfo(ISourceLineInfo? lineinfo)
            {
                this.elemNameLi = lineinfo;
                this.endTagLi = lineinfo;
                this.lineInfo = lineinfo;
            }

            public ContextInfo(XsltInput input)
            {
                _elemNameLength = input.QualifiedName.Length;
            }

            public void AddNamespace(string prefix, string nsUri)
            {
                nsList = new NsDecl(nsList, prefix, nsUri);
            }

            public void SaveExtendedLineInfo(XsltInput input)
            {
                if (lineInfo!.Start.Line == 0)
                {
                    elemNameLi = endTagLi = null;
                    return;
                }

                elemNameLi = new SourceLineInfo(
                    lineInfo.Uri,
                    lineInfo.Start.Line, lineInfo.Start.Pos + 1,  // "<"
                    lineInfo.Start.Line, lineInfo.Start.Pos + 1 + _elemNameLength
                );

                if (!input.IsEmptyElement)
                {
                    Debug.Assert(input.NodeType == XmlNodeType.EndElement);
                    endTagLi = input.BuildLineInfo();
                }
                else
                {
                    Debug.Assert(input.NodeType == XmlNodeType.Element || input.NodeType == XmlNodeType.Attribute);
                    endTagLi = new EmptyElementEndTag(lineInfo);
                }
            }

            // We need this wrapper class because elementTagLi is not yet calculated
            internal sealed class EmptyElementEndTag : ISourceLineInfo
            {
                private readonly ISourceLineInfo _elementTagLi;

                public EmptyElementEndTag(ISourceLineInfo elementTagLi)
                {
                    _elementTagLi = elementTagLi;
                }

                public string? Uri { get { return _elementTagLi.Uri; } }
                public bool IsNoSource { get { return _elementTagLi.IsNoSource; } }
                public Location Start { get { return new Location(_elementTagLi.End.Line, _elementTagLi.End.Pos - 2); } }
                public Location End { get { return _elementTagLi.End; } }
            }
        }
        internal struct Record
        {
            public string localName;
            public string nsUri;
            public string prefix;
            public string value;
            public string? baseUri;
            public Location start;
            public Location valueStart;
            public Location end;
            public string QualifiedName { get { return prefix.Length == 0 ? localName : $"{prefix}:{localName}"; } }
        }
    }
}
