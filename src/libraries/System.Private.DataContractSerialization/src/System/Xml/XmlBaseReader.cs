// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    // Large numbers of attributes
    // Use delimiter on node for figuring out Element/EndElement?
    // Optimize StringHandle.CompareTo
    // Fix FixXmlAttribute - Temporary until we actually write an XmlAttribute node

    internal abstract class XmlBaseReader : XmlDictionaryReader
    {
        private readonly XmlBufferReader _bufferReader;
        private XmlNode _node;
        private readonly NamespaceManager _nsMgr;
        private XmlElementNode[]? _elementNodes;
        private XmlAttributeNode[]? _attributeNodes;
        private readonly XmlAtomicTextNode _atomicTextNode;
        private int _depth;
        private int _attributeCount;
        private int _attributeStart;    // Starting index for searching
        private readonly XmlDictionaryReaderQuotas _quotas;
        private XmlNameTable? _nameTable;
        private XmlDeclarationNode? _declarationNode;
        private XmlComplexTextNode? _complexTextNode;
        private XmlWhitespaceTextNode? _whitespaceTextNode;
        private XmlCDataNode? _cdataNode;
        private XmlCommentNode? _commentNode;
        private readonly XmlElementNode _rootElementNode;
        private int _attributeIndex;    // Index for iteration
        private char[]? _chars;
        private string? _prefix;
        private string? _localName;
        private string? _ns;
        private string? _value;
        private int _trailCharCount;
        private int _trailByteCount;
        private char[]? _trailChars;
        private byte[]? _trailBytes;
        private bool _rootElement;
        private bool _readingElement;
        private AttributeSorter? _attributeSorter;
        private static readonly XmlInitialNode s_initialNode = new XmlInitialNode(XmlBufferReader.Empty);
        private static readonly XmlEndOfFileNode s_endOfFileNode = new XmlEndOfFileNode(XmlBufferReader.Empty);
        private static readonly XmlClosedNode s_closedNode = new XmlClosedNode(XmlBufferReader.Empty);
        private static Base64Encoding? s_base64Encoding;
        private static BinHexEncoding? s_binHexEncoding;
        private const string xmlns = "xmlns";
        private const string xml = "xml";
        private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private XmlSigningNodeWriter? _signingWriter;
        private bool _signing;

        protected XmlBaseReader()
        {
            _bufferReader = new XmlBufferReader(this);
            _nsMgr = new NamespaceManager(_bufferReader);
            _quotas = new XmlDictionaryReaderQuotas();
            _rootElementNode = new XmlElementNode(_bufferReader);
            _atomicTextNode = new XmlAtomicTextNode(_bufferReader);
            _node = s_closedNode;
        }


        private static Base64Encoding Base64Encoding => s_base64Encoding ??= new Base64Encoding();

        private static BinHexEncoding BinHexEncoding => s_binHexEncoding ??= new BinHexEncoding();

        protected XmlBufferReader BufferReader
        {
            get
            {
                return _bufferReader;
            }
        }

        public override XmlDictionaryReaderQuotas Quotas
        {
            get
            {
                return _quotas;
            }
        }

        protected XmlNode Node
        {
            get
            {
                return _node;
            }
        }

        protected void MoveToNode(XmlNode node)
        {
            _node = node;
            _ns = null;
            _localName = null;
            _prefix = null;
            _value = null;
        }

        protected void MoveToInitial(XmlDictionaryReaderQuotas quotas)
        {
            ArgumentNullException.ThrowIfNull(quotas);

            quotas.InternalCopyTo(_quotas);
            _quotas.MakeReadOnly();
            _nsMgr.Clear();
            _depth = 0;
            _attributeCount = 0;
            _attributeStart = -1;
            _attributeIndex = -1;
            _rootElement = false;
            _readingElement = false;
            MoveToNode(s_initialNode);
        }

        protected XmlDeclarationNode MoveToDeclaration()
        {
            if (_attributeCount < 1)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlDeclMissingVersion));

            if (_attributeCount > 3)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlMalformedDecl));

            // version
            if (!CheckDeclAttribute(0, "version", "1.0", false, SR.XmlInvalidVersion))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlDeclMissingVersion));

            // encoding/standalone
            // We only validate that they are the only attributes that exist.  Encoding can have any value.
            if (_attributeCount > 1)
            {
                if (CheckDeclAttribute(1, "encoding", null, true, SR.XmlInvalidEncoding_UTF8))
                {
                    if (_attributeCount == 3 && !CheckStandalone(2))
                        XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlMalformedDecl));
                }
                else if (!CheckStandalone(1) || _attributeCount > 2)
                {
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlMalformedDecl));
                }
            }

            _declarationNode ??= new XmlDeclarationNode(_bufferReader);
            MoveToNode(_declarationNode);
            return _declarationNode;
        }

        private bool CheckStandalone(int attr)
        {
            XmlAttributeNode node = _attributeNodes![attr];
            if (!node.Prefix.IsEmpty)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlMalformedDecl));

            if (node.LocalName != "standalone")
                return false;

            if (!node.Value.Equals2("yes", false) && !node.Value.Equals2("no", false))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlInvalidStandalone));

            return true;
        }

        private bool CheckDeclAttribute(int index, string localName, string? value, bool checkLower, string valueSR)
        {
            XmlAttributeNode node = _attributeNodes![index];
            if (!node.Prefix.IsEmpty)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlMalformedDecl));

            if (node.LocalName != localName)
                return false;

            if (value != null && !node.Value.Equals2(value, checkLower))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.Format(valueSR)));

            return true;
        }
        protected XmlCommentNode MoveToComment()
        {
            _commentNode ??= new XmlCommentNode(_bufferReader);
            MoveToNode(_commentNode);
            return _commentNode;
        }

        protected XmlCDataNode MoveToCData()
        {
            _cdataNode ??= new XmlCDataNode(_bufferReader);
            MoveToNode(_cdataNode);
            return _cdataNode;
        }

        protected XmlAtomicTextNode MoveToAtomicText()
        {
            XmlAtomicTextNode textNode = _atomicTextNode;
            MoveToNode(textNode);
            return textNode;
        }

        protected XmlComplexTextNode MoveToComplexText()
        {
            _complexTextNode ??= new XmlComplexTextNode(_bufferReader);
            MoveToNode(_complexTextNode);
            return _complexTextNode;
        }

        protected XmlTextNode MoveToWhitespaceText()
        {
            _whitespaceTextNode ??= new XmlWhitespaceTextNode(_bufferReader);
            if (_nsMgr.XmlSpace == XmlSpace.Preserve)
                _whitespaceTextNode.NodeType = XmlNodeType.SignificantWhitespace;
            else
                _whitespaceTextNode.NodeType = XmlNodeType.Whitespace;
            MoveToNode(_whitespaceTextNode);
            return _whitespaceTextNode;
        }
        protected XmlElementNode ElementNode
        {
            get
            {
                if (_depth == 0)
                    return _rootElementNode;
                else
                    return _elementNodes![_depth];
            }
        }

        protected void MoveToEndElement()
        {
            if (_depth == 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            XmlElementNode elementNode = _elementNodes![_depth];
            XmlEndElementNode endElementNode = elementNode.EndElement;
            endElementNode.Namespace = elementNode.Namespace;
            MoveToNode(endElementNode);
        }

        protected void MoveToEndOfFile()
        {
            if (_depth != 0)
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(this);
            MoveToNode(s_endOfFileNode);
        }

        protected XmlElementNode EnterScope()
        {
            if (_depth == 0)
            {
                if (_rootElement)
                    XmlExceptionHelper.ThrowMultipleRootElements(this);
                _rootElement = true;
            }
            _nsMgr.EnterScope();
            _depth++;
            if (_depth > _quotas.MaxDepth)
                XmlExceptionHelper.ThrowMaxDepthExceeded(this, _quotas.MaxDepth);
            if (_elementNodes == null)
            {
                _elementNodes = new XmlElementNode[4];
            }
            else if (_elementNodes.Length == _depth)
            {
                XmlElementNode[] newElementNodes = new XmlElementNode[_depth * 2];
                Array.Copy(_elementNodes, newElementNodes, _depth);
                _elementNodes = newElementNodes;
            }
            XmlElementNode elementNode = _elementNodes[_depth];
            if (elementNode == null)
            {
                elementNode = new XmlElementNode(_bufferReader);
                _elementNodes[_depth] = elementNode;
            }
            _attributeCount = 0;
            _attributeStart = -1;
            _attributeIndex = -1;
            MoveToNode(elementNode);
            return elementNode;
        }

        protected void ExitScope()
        {
            if (_depth == 0)
                XmlExceptionHelper.ThrowUnexpectedEndElement(this);
            _depth--;
            _nsMgr.ExitScope();
        }

        private XmlAttributeNode AddAttribute(QNameType qnameType, bool isAtomicValue)
        {
            int attributeIndex = _attributeCount;
            if (_attributeNodes == null)
            {
                _attributeNodes = new XmlAttributeNode[4];
            }
            else if (_attributeNodes.Length == attributeIndex)
            {
                XmlAttributeNode[] newAttributeNodes = new XmlAttributeNode[attributeIndex * 2];
                Array.Copy(_attributeNodes, newAttributeNodes, attributeIndex);
                _attributeNodes = newAttributeNodes;
            }
            XmlAttributeNode attributeNode = _attributeNodes[attributeIndex];
            if (attributeNode == null)
            {
                attributeNode = new XmlAttributeNode(_bufferReader);
                _attributeNodes[attributeIndex] = attributeNode;
            }
            attributeNode.QNameType = qnameType;
            attributeNode.IsAtomicValue = isAtomicValue;
            attributeNode.AttributeText!.QNameType = qnameType;
            attributeNode.AttributeText.IsAtomicValue = isAtomicValue;
            _attributeCount++;
            return attributeNode;
        }

        protected Namespace AddNamespace()
        {
            return _nsMgr.AddNamespace();
        }

        protected XmlAttributeNode AddAttribute()
        {
            return AddAttribute(QNameType.Normal, true);
        }

        protected XmlAttributeNode AddXmlAttribute()
        {
            return AddAttribute(QNameType.Normal, true);
        }
        protected XmlAttributeNode AddXmlnsAttribute(Namespace ns)
        {
            if (!ns.Prefix.IsEmpty && ns.Uri.IsEmpty)
                XmlExceptionHelper.ThrowEmptyNamespace(this);

            // Some prefixes can only be bound to a particular namespace
            if (ns.Prefix.IsXml && ns.Uri != xmlNamespace)
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.Format(SR.XmlSpecificBindingPrefix, "xml", xmlNamespace)));
            }
            else if (ns.Prefix.IsXmlns && ns.Uri != xmlnsNamespace)
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.Format(SR.XmlSpecificBindingPrefix, "xmlns", xmlnsNamespace)));
            }

            _nsMgr.Register(ns);
            XmlAttributeNode attributeNode = AddAttribute(QNameType.Xmlns, false);
            attributeNode.Namespace = ns;
            attributeNode.AttributeText!.Namespace = ns;
            return attributeNode;
        }

        protected void FixXmlAttribute(XmlAttributeNode attributeNode)
        {
            if (attributeNode.Prefix == xml)
            {
                if (attributeNode.LocalName == "lang")
                {
                    _nsMgr.AddLangAttribute(attributeNode.Value.GetString());
                }
                else if (attributeNode.LocalName == "space")
                {
                    string value = attributeNode.Value.GetString();
                    if (value == "preserve")
                    {
                        _nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
                    }
                    else if (value == "default")
                    {
                        _nsMgr.AddSpaceAttribute(XmlSpace.Default);
                    }
                }
            }
        }
        protected bool OutsideRootElement
        {
            get
            {
                return _depth == 0;
            }
        }
        public override bool CanReadBinaryContent
        {
            get { return true; }
        }

        public override bool CanReadValueChunk
        {
            get { return true; }
        }

        public override string BaseURI
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool HasValue
        {
            get
            {
                return _node.HasValue;
            }
        }

        public override bool IsDefault
        {
            get
            {
                return false;
            }
        }

        public override string this[int index]
        {
            get
            {
                return GetAttribute(index);
            }
        }

        public override string? this[string name]
        {
            get
            {
                return GetAttribute(name);
            }
        }

        public override string? this[string localName, string? namespaceUri]
        {
            get
            {
                return GetAttribute(localName, namespaceUri);
            }
        }

        public override int AttributeCount
        {
            get
            {
                if (_node.CanGetAttribute)
                    return _attributeCount;
                return 0;
            }
        }

        public override void Close()
        {
            MoveToNode(s_closedNode);
            _nameTable = null;
            if (_attributeNodes != null && _attributeNodes.Length > 16)
                _attributeNodes = null;
            if (_elementNodes != null && _elementNodes.Length > 16)
                _elementNodes = null;
            _nsMgr.Close();
            _bufferReader.Close();
            _signingWriter?.Close();
            _attributeSorter?.Close();
        }

        public sealed override int Depth
        {
            get
            {
                // Internally, depth is simply measured by Element/EndElement.  What XmlReader exposes is a little different
                // so we need to account for this with some minor adjustments.

                // We increment depth immediately when we see an element, but XmlTextReader waits until its consumed
                // We decrement depth when its consumed, but XmlTextReader decrements depth immediately

                // If we're on Attribute Text (i.e. ReadAttributeValue), then its considered a level deeper
                return _depth + _node.DepthDelta;
            }
        }

        public override bool EOF
        {
            get
            {
                return _node.ReadState == ReadState.EndOfFile;
            }
        }

        private XmlAttributeNode GetAttributeNode(int index)
        {
            if (!_node.CanGetAttribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(index), SR.XmlElementAttributes));
            if (index < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(index), SR.ValueMustBeNonNegative));
            if (index >= _attributeCount)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(index), SR.Format(SR.OffsetExceedsBufferSize, _attributeCount)));
            return _attributeNodes![index];
        }

        private XmlAttributeNode? GetAttributeNode(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!_node.CanGetAttribute)
                return null;
            int index = name.IndexOf(':');
            string prefix;
            string localName;
            if (index == -1)
            {
                if (name == xmlns)
                {
                    prefix = xmlns;
                    localName = string.Empty;
                }
                else
                {
                    prefix = string.Empty;
                    localName = name;
                }
            }
            else
            {
                // If this function becomes a performance issue because of the allocated strings then we can
                // make a version of Equals that takes an offset and count into the string.
                prefix = name.Substring(0, index);
                localName = name.Substring(index + 1);
            }
            XmlAttributeNode[]? attributeNodes = _attributeNodes;
            int attributeCount = _attributeCount;
            int attributeIndex = _attributeStart;
            for (int i = 0; i < attributeCount; i++)
            {
                if (++attributeIndex >= attributeCount)
                {
                    attributeIndex = 0;
                }
                XmlAttributeNode attributeNode = attributeNodes![attributeIndex];
                if (attributeNode.IsPrefixAndLocalName(prefix, localName))
                {
                    _attributeStart = attributeIndex;
                    return attributeNode;
                }
            }
            return null;
        }

        private XmlAttributeNode? GetAttributeNode(string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);

            namespaceUri ??= string.Empty;
            if (!_node.CanGetAttribute)
                return null;

            XmlAttributeNode[]? attributeNodes = _attributeNodes;
            int attributeCount = _attributeCount;
            int attributeIndex = _attributeStart;
            for (int i = 0; i < attributeCount; i++)
            {
                if (++attributeIndex >= attributeCount)
                {
                    attributeIndex = 0;
                }
                XmlAttributeNode attributeNode = attributeNodes![attributeIndex];
                if (attributeNode.IsLocalNameAndNamespaceUri(localName, namespaceUri))
                {
                    _attributeStart = attributeIndex;
                    return attributeNode;
                }
            }
            return null;
        }

        private XmlAttributeNode? GetAttributeNode(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);

            namespaceUri ??= XmlDictionaryString.Empty;
            if (!_node.CanGetAttribute)
                return null;

            XmlAttributeNode[]? attributeNodes = _attributeNodes;
            int attributeCount = _attributeCount;
            int attributeIndex = _attributeStart;
            for (int i = 0; i < attributeCount; i++)
            {
                if (++attributeIndex >= attributeCount)
                {
                    attributeIndex = 0;
                }
                XmlAttributeNode attributeNode = attributeNodes![attributeIndex];
                if (attributeNode.IsLocalNameAndNamespaceUri(localName, namespaceUri))
                {
                    _attributeStart = attributeIndex;
                    return attributeNode;
                }
            }
            return null;
        }


        public override string GetAttribute(int index)
        {
            return GetAttributeNode(index).ValueAsString;
        }

        public override string? GetAttribute(string name)
        {
            XmlAttributeNode? attributeNode = GetAttributeNode(name);
            if (attributeNode == null)
                return null;
            return attributeNode.ValueAsString;
        }

        public override string? GetAttribute(string localName, string? namespaceUri)
        {
            XmlAttributeNode? attributeNode = GetAttributeNode(localName, namespaceUri);
            if (attributeNode == null)
                return null;
            return attributeNode.ValueAsString;
        }

        public override string? GetAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            XmlAttributeNode? attributeNode = GetAttributeNode(localName, namespaceUri);
            if (attributeNode == null)
                return null;
            return attributeNode.ValueAsString;
        }
        public sealed override bool IsEmptyElement
        {
            get
            {
                return _node.IsEmptyElement;
            }
        }

        public override string LocalName
        {
            get
            {
                if (_localName == null)
                {
                    QNameType qnameType = _node.QNameType;
                    if (qnameType == QNameType.Normal)
                    {
                        _localName = _node.LocalName.GetString(NameTable);
                    }
                    else
                    {
                        DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                        if (_node.Namespace.Prefix.IsEmpty)
                            _localName = xmlns;
                        else
                            _localName = _node.Namespace.Prefix.GetString(NameTable);
                    }
                }

                return _localName;
            }
        }

        public override string? LookupNamespace(string prefix)
        {
            Namespace? ns = _nsMgr.LookupNamespace(prefix);
            if (ns != null)
                return ns.Uri.GetString(NameTable);
            if (prefix == xmlns)
                return xmlnsNamespace;
            return null;
        }

        protected Namespace LookupNamespace(PrefixHandleType prefix)
        {
            Namespace? ns = _nsMgr.LookupNamespace(prefix);
            if (ns == null)
                XmlExceptionHelper.ThrowUndefinedPrefix(this, PrefixHandle.GetString(prefix));
            return ns;
        }

        protected Namespace LookupNamespace(PrefixHandle prefix)
        {
            Namespace? ns = _nsMgr.LookupNamespace(prefix);
            if (ns == null)
                XmlExceptionHelper.ThrowUndefinedPrefix(this, prefix.GetString());
            return ns;
        }

        protected void ProcessAttributes()
        {
            if (_attributeCount > 0)
            {
                ProcessAttributes(_attributeNodes!, _attributeCount);
            }
        }

        private void ProcessAttributes(XmlAttributeNode[] attributeNodes, int attributeCount)
        {
            for (int i = 0; i < attributeCount; i++)
            {
                XmlAttributeNode attributeNode = attributeNodes[i];
                if (attributeNode.QNameType == QNameType.Normal)
                {
                    PrefixHandle prefix = attributeNode.Prefix;
                    if (!prefix.IsEmpty)
                    {
                        attributeNode.Namespace = LookupNamespace(prefix);
                    }
                    else
                    {
                        attributeNode.Namespace = NamespaceManager.EmptyNamespace;
                    }
                    attributeNode.AttributeText!.Namespace = attributeNode.Namespace;
                }
            }

            if (attributeCount > 1)
            {
                if (attributeCount < 12)
                {
                    // For small numbers of attributes, a naive n * (n - 1) / 2 comparisons to check for uniqueness is faster
                    for (int i = 0; i < attributeCount - 1; i++)
                    {
                        XmlAttributeNode attributeNode1 = attributeNodes[i];
                        QNameType qnameType = attributeNode1.QNameType;
                        if (qnameType == QNameType.Normal)
                        {
                            for (int j = i + 1; j < attributeCount; j++)
                            {
                                XmlAttributeNode attributeNode2 = attributeNodes[j];
                                if (attributeNode2.QNameType == QNameType.Normal && attributeNode1.LocalName == attributeNode2.LocalName && attributeNode1.Namespace.Uri == attributeNode2.Namespace.Uri)
                                {
                                    XmlExceptionHelper.ThrowDuplicateAttribute(this, attributeNode1.Prefix.GetString(), attributeNode2.Prefix.GetString(), attributeNode1.LocalName.GetString(), attributeNode1.Namespace.Uri.GetString());
                                }
                            }
                        }
                        else
                        {
                            DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                            for (int j = i + 1; j < attributeCount; j++)
                            {
                                XmlAttributeNode attributeNode2 = attributeNodes[j];
                                if (attributeNode2.QNameType == QNameType.Xmlns && attributeNode1.Namespace.Prefix == attributeNode2.Namespace.Prefix)
                                    XmlExceptionHelper.ThrowDuplicateAttribute(this, xmlns, xmlns, attributeNode1.Namespace.Prefix.GetString(), xmlnsNamespace);
                            }
                        }
                    }
                }
                else
                {
                    CheckAttributes(attributeNodes, attributeCount);
                }
            }
        }

        private void CheckAttributes(XmlAttributeNode[] attributeNodes, int attributeCount)
        {
            // For large numbers of attributes, sorting the attributes (n * lg(n)) is faster
            _attributeSorter ??= new AttributeSorter();

            if (!_attributeSorter.Sort(attributeNodes, attributeCount))
            {
                int attribute1, attribute2;
                _attributeSorter.GetIndices(out attribute1, out attribute2);
                if (attributeNodes[attribute1].QNameType == QNameType.Xmlns)
                    XmlExceptionHelper.ThrowDuplicateXmlnsAttribute(this, attributeNodes[attribute1].Namespace.Prefix.GetString(), xmlnsNamespace);
                else
                    XmlExceptionHelper.ThrowDuplicateAttribute(this, attributeNodes[attribute1].Prefix.GetString(), attributeNodes[attribute2].Prefix.GetString(), attributeNodes[attribute1].LocalName.GetString(), attributeNodes[attribute1].Namespace.Uri.GetString());
            }
        }


        public override void MoveToAttribute(int index)
        {
            MoveToNode(GetAttributeNode(index));
        }
        public override bool MoveToAttribute(string name)
        {
            XmlNode? attributeNode = GetAttributeNode(name);
            if (attributeNode == null)
                return false;
            MoveToNode(attributeNode);
            return true;
        }

        public override bool MoveToAttribute(string localName, string? namespaceUri)
        {
            XmlNode? attributeNode = GetAttributeNode(localName, namespaceUri);
            if (attributeNode == null)
                return false;
            MoveToNode(attributeNode);
            return true;
        }

        public override bool MoveToElement()
        {
            if (!_node.CanMoveToElement)
                return false;
            if (_depth == 0)
                MoveToDeclaration();
            else
                MoveToNode(_elementNodes![_depth]);
            _attributeIndex = -1;
            return true;
        }

        public override XmlNodeType MoveToContent()
        {
            do
            {
                if (_node.HasContent)
                {
                    if (_node.NodeType != XmlNodeType.Text && _node.NodeType != XmlNodeType.CDATA)
                        break;

                    if (_trailByteCount > 0)
                    {
                        break;
                    }

                    if (_value == null)
                    {
                        if (!_node.Value.IsWhitespace())
                            break;
                    }
                    else
                    {
                        if (!XmlConverter.IsWhitespace(_value))
                            break;
                    }
                }
                else
                {
                    if (_node.NodeType == XmlNodeType.Attribute)
                    {
                        MoveToElement();
                        break;
                    }
                }
            }
            while (Read());
            return _node.NodeType;
        }

        public override bool MoveToFirstAttribute()
        {
            if (!_node.CanGetAttribute || _attributeCount == 0)
                return false;
            MoveToNode(GetAttributeNode(0));
            _attributeIndex = 0;
            return true;
        }

        public override bool MoveToNextAttribute()
        {
            if (!_node.CanGetAttribute)
                return false;
            int attributeIndex = _attributeIndex + 1;
            if (attributeIndex >= _attributeCount)
                return false;
            MoveToNode(GetAttributeNode(attributeIndex));
            _attributeIndex = attributeIndex;
            return true;
        }

        public override string NamespaceURI
        {
            get
            {
                if (_ns == null)
                {
                    QNameType qnameType = _node.QNameType;
                    if (qnameType == QNameType.Normal)
                    {
                        _ns = _node.Namespace.Uri.GetString(NameTable);
                    }
                    else
                    {
                        DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                        _ns = xmlnsNamespace;
                    }
                }
                return _ns;
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                if (_nameTable == null)
                {
                    _nameTable = new NameTable();
                    _nameTable.Add(xml);
                    _nameTable.Add(xmlns);
                    _nameTable.Add(xmlnsNamespace);
                    _nameTable.Add(xmlNamespace);
                    for (PrefixHandleType i = PrefixHandleType.A; i <= PrefixHandleType.Z; i++)
                    {
                        _nameTable.Add(PrefixHandle.GetString(i));
                    }
                }

                return _nameTable;
            }
        }

        public sealed override XmlNodeType NodeType
        {
            get
            {
                return _node.NodeType;
            }
        }

        public override string Prefix
        {
            get
            {
                if (_prefix == null)
                {
                    QNameType qnameType = _node.QNameType;
                    if (qnameType == QNameType.Normal)
                    {
                        _prefix = _node.Prefix.GetString(NameTable);
                    }
                    else if (qnameType == QNameType.Xmlns)
                    {
                        if (_node.Namespace.Prefix.IsEmpty)
                            _prefix = string.Empty;
                        else
                            _prefix = xmlns;
                    }
                    else
                    {
                        _prefix = xml;
                    }
                }

                return _prefix;
            }
        }


        public override bool IsLocalName(string localName)
        {
            ArgumentNullException.ThrowIfNull(localName);

            return _node.IsLocalName(localName);
        }

        public override bool IsLocalName(XmlDictionaryString localName)
        {
            ArgumentNullException.ThrowIfNull(localName);

            return _node.IsLocalName(localName);
        }
        public override bool IsNamespaceUri(string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(namespaceUri);

            return _node.IsNamespaceUri(namespaceUri);
        }

        public override bool IsNamespaceUri(XmlDictionaryString namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(namespaceUri);

            return _node.IsNamespaceUri(namespaceUri);
        }
        public sealed override bool IsStartElement()
        {
            XmlNodeType nodeType = _node.NodeType;
            if (nodeType == XmlNodeType.Element)
                return true;
            if (nodeType == XmlNodeType.EndElement)
                return false;
            if (nodeType == XmlNodeType.None)
            {
                Read();
                if (_node.NodeType == XmlNodeType.Element)
                    return true;
            }
            return (MoveToContent() == XmlNodeType.Element);
        }

        public override bool IsStartElement(string name)
        {
            if (name == null)
                return false;
            int index = name.IndexOf(':');
            string prefix;
            string localName;
            if (index == -1)
            {
                prefix = string.Empty;
                localName = name;
            }
            else
            {
                prefix = name.Substring(0, index);
                localName = name.Substring(index + 1);
            }
            return (_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.Prefix == prefix && _node.LocalName == localName;
        }

        public override bool IsStartElement(string localName, string namespaceUri)
        {
            if (localName == null)
                return false;
            if (namespaceUri == null)
                return false;
            return (_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.LocalName == localName && _node.IsNamespaceUri(namespaceUri);
        }

        public override bool IsStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            ArgumentNullException.ThrowIfNull(namespaceUri);

            return (_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.LocalName == localName && _node.IsNamespaceUri(namespaceUri);
        }

        public override int IndexOfLocalName(string[] localNames, string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localNames);
            ArgumentNullException.ThrowIfNull(namespaceUri);

            QNameType qnameType = _node.QNameType;
            if (_node.IsNamespaceUri(namespaceUri))
            {
                if (qnameType == QNameType.Normal)
                {
                    StringHandle localName = _node.LocalName;
                    for (int i = 0; i < localNames.Length; i++)
                    {
                        string value = localNames[i];
                        if (value == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
                        if (localName == value)
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                    PrefixHandle prefix = _node.Namespace.Prefix;
                    for (int i = 0; i < localNames.Length; i++)
                    {
                        string value = localNames[i];
                        if (value == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
                        if (prefix == value)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public override int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localNames);
            ArgumentNullException.ThrowIfNull(namespaceUri);

            QNameType qnameType = _node.QNameType;
            if (_node.IsNamespaceUri(namespaceUri))
            {
                if (qnameType == QNameType.Normal)
                {
                    StringHandle localName = _node.LocalName;
                    for (int i = 0; i < localNames.Length; i++)
                    {
                        XmlDictionaryString value = localNames[i];
                        if (value == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
                        if (localName == value)
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                    PrefixHandle prefix = _node.Namespace.Prefix;
                    for (int i = 0; i < localNames.Length; i++)
                    {
                        XmlDictionaryString value = localNames[i];
                        if (value == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
                        if (prefix == value)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public override int ReadValueChunk(char[] chars, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(chars);

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > chars.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > chars.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));
            int actual;

            if (_value == null)
            {
                if (_node.QNameType == QNameType.Normal)
                {
                    if (_node.Value.TryReadChars(chars, offset, count, out actual))
                        return actual;
                }
            }

            string value = this.Value;
            actual = Math.Min(count, value.Length);
            value.CopyTo(0, chars, offset, actual);
            _value = value.Substring(actual);
            return actual;
        }

        public override int ReadValueAsBase64(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
            if (count == 0)
                return 0;
            int actual;
            if (_value == null)
            {
                if (_trailByteCount == 0 && _trailCharCount == 0)
                {
                    if (_node.QNameType == QNameType.Normal)
                    {
                        if (_node.Value.TryReadBase64(buffer, offset, count, out actual))
                            return actual;
                    }
                }
            }
            return ReadBytes(Base64Encoding, 3, 4, buffer, offset, Math.Min(count, 512), false);
        }

        public override string ReadElementContentAsString()
        {
            if (_node.NodeType != XmlNodeType.Element)
                MoveToStartElement();

            if (_node.IsEmptyElement)
            {
                Read();
                return string.Empty;
            }
            else
            {
                Read();
                string s = ReadContentAsString();
                ReadEndElement();
                return s;
            }
        }
        public override void ReadStartElement()
        {
            if (_node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            Read();
        }

        public override void ReadStartElement(string name)
        {
            MoveToStartElement(name);
            Read();
        }

        public override void ReadStartElement(string localName, string namespaceUri)
        {
            MoveToStartElement(localName, namespaceUri);
            Read();
        }

        public override void ReadEndElement()
        {
            if (_node.NodeType != XmlNodeType.EndElement && MoveToContent() != XmlNodeType.EndElement)
            {
                int nodeDepth = _node.NodeType == XmlNodeType.Element ? _depth - 1 : _depth;
                if (nodeDepth == 0)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlEndElementNoOpenNodes));
                // If depth is non-zero, then the document isn't what was expected
                XmlElementNode elementNode = _elementNodes![nodeDepth];
                XmlExceptionHelper.ThrowEndElementExpected(this, elementNode.LocalName.GetString(), elementNode.Namespace.Uri.GetString());
            }
            Read();
        }

        public override bool ReadAttributeValue()
        {
            XmlAttributeTextNode? attributeTextNode = _node.AttributeText;
            if (attributeTextNode == null)
                return false;
            MoveToNode(attributeTextNode);
            return true;
        }

        public override ReadState ReadState
        {
            get
            {
                return _node.ReadState;
            }
        }

        private void SkipValue(XmlNode node)
        {
            if (node.SkipValue)
                Read();
        }

        public override bool TryGetBase64ContentLength(out int length)
        {
            if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null)
            {
                XmlNode node = this.Node;
                if (node.IsAtomicValue)
                    return node.Value.TryGetByteArrayLength(out length);
            }
            return base.TryGetBase64ContentLength(out length);
        }

        public override byte[] ReadContentAsBase64()
        {
            if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null)
            {
                XmlNode node = this.Node;
                if (node.IsAtomicValue)
                {
                    byte[] value = node.Value.ToByteArray();
                    if (value.Length > _quotas.MaxArrayLength)
                        XmlExceptionHelper.ThrowMaxArrayLengthExceeded(this, _quotas.MaxArrayLength);
                    SkipValue(node);
                    return value;
                }
            }

            if (!_bufferReader.IsStreamed)
                return ReadContentAsBase64(_quotas.MaxArrayLength, _bufferReader.Buffer.Length);

            return ReadContentAsBase64(_quotas.MaxArrayLength, XmlDictionaryReader.MaxInitialArrayLength);  // Initial count will get ignored
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int offset, int count)
        {
            if (!_readingElement)
            {
                if (IsEmptyElement)
                {
                    Read();
                    return 0;
                }

                ReadStartElement();
                _readingElement = true;
            }

            int i = ReadContentAsBase64(buffer, offset, count);

            if (i == 0)
            {
                ReadEndElement();
                _readingElement = false;
            }

            return i;
        }

        public override int ReadContentAsBase64(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
            if (count == 0)
                return 0;
            int actual;
            if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null)
            {
                if (_node.QNameType == QNameType.Normal)
                {
                    while (_node.NodeType != XmlNodeType.Comment && _node.Value.TryReadBase64(buffer, offset, count, out actual))
                    {
                        if (actual != 0)
                            return actual;
                        Read();
                    }
                }
            }
            XmlNodeType nodeType = _node.NodeType;
            if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
                return 0;
            return ReadBytes(Base64Encoding, 3, 4, buffer, offset, Math.Min(count, 512), true);
        }

        public override byte[] ReadContentAsBinHex()
        {
            return ReadContentAsBinHex(_quotas.MaxArrayLength);
        }

        public override int ReadContentAsBinHex(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
            if (count == 0)
                return 0;
            return ReadBytes(BinHexEncoding, 1, 2, buffer, offset, Math.Min(count, 512), true);
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int offset, int count)
        {
            if (!_readingElement)
            {
                if (IsEmptyElement)
                {
                    Read();
                    return 0;
                }

                ReadStartElement();
                _readingElement = true;
            }

            int i = ReadContentAsBinHex(buffer, offset, count);

            if (i == 0)
            {
                ReadEndElement();
                _readingElement = false;
            }

            return i;
        }

        private int ReadBytes(Encoding encoding, int byteBlock, int charBlock, byte[] buffer, int offset, int byteCount, bool readContent)
        {
            // If there are any trailing buffer return them.
            if (_trailByteCount > 0)
            {
                Debug.Assert(_trailBytes != null);

                int actual = Math.Min(_trailByteCount, byteCount);
                Buffer.BlockCopy(_trailBytes, 0, buffer, offset, actual);
                _trailByteCount -= actual;
                Buffer.BlockCopy(_trailBytes, actual, _trailBytes, 0, _trailByteCount);
                return actual;
            }
            XmlNodeType nodeType = _node.NodeType;
            if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
                return 0;
            int maxCharCount;
            if (byteCount < byteBlock)
            {
                // Convert at least charBlock chars
                maxCharCount = charBlock;
            }
            else
            {
                // Round down to the nearest multiple of charBlock
                maxCharCount = byteCount / byteBlock * charBlock;
            }
            char[] chars = GetCharBuffer(maxCharCount);
            int charCount = 0;
            while (true)
            {
                // If we didn't align on the boundary, then we might have some remaining characters
                if (_trailCharCount > 0)
                {
                    Array.Copy(_trailChars!, 0, chars, charCount, _trailCharCount);
                    charCount += _trailCharCount;
                    _trailCharCount = 0;
                }
                // Read until we at least get a charBlock
                while (charCount < charBlock)
                {
                    int actualCharCount;
                    if (readContent)
                    {
                        actualCharCount = ReadContentAsChars(chars, charCount, maxCharCount - charCount);
                        // When deserializing base64 content which contains new line chars (CR, LF) chars from ReadObject, the reader reads in chunks of base64 content, LF char, base64 content, LF char and so on
                        // Relying on encoding.GetBytes' exception to handle LF char would result in performance degradation so skipping LF char here
                        if (actualCharCount == 1 && chars[charCount] == '\n')
                            continue;
                    }
                    else
                    {
                        actualCharCount = ReadValueChunk(chars, charCount, maxCharCount - charCount);
                    }
                    if (actualCharCount == 0)
                        break;
                    charCount += actualCharCount;
                }
                // Trim so its a multiple of charBlock
                if (charCount >= charBlock)
                {
                    _trailCharCount = (charCount % charBlock);
                    if (_trailCharCount > 0)
                    {
                        _trailChars ??= new char[4];
                        charCount -= _trailCharCount;
                        Array.Copy(chars, charCount, _trailChars, 0, _trailCharCount);
                    }
                }
                try
                {
                    if (byteCount < byteBlock)
                    {
                        _trailBytes ??= new byte[3];
                        _trailByteCount = encoding.GetBytes(chars, 0, charCount, _trailBytes, 0);
                        int actual = Math.Min(_trailByteCount, byteCount);
                        Buffer.BlockCopy(_trailBytes, 0, buffer, offset, actual);
                        _trailByteCount -= actual;
                        Buffer.BlockCopy(_trailBytes, actual, _trailBytes, 0, _trailByteCount);
                        return actual;
                    }
                    else
                    {
                        // charCount is a multiple of charBlock and we have enough room to convert everything
                        return encoding.GetBytes(chars, 0, charCount, buffer, offset);
                    }
                }
                catch (FormatException exception)
                {
                    // Something was wrong with the format, see if we can strip the spaces
                    int i = 0;
                    int j = 0;
                    while (true)
                    {
                        while (j < charCount && XmlConverter.IsWhitespace(chars[j]))
                            j++;
                        if (j == charCount)
                            break;
                        chars[i++] = chars[j++];
                    }
                    // No spaces, so don't try again
                    if (i == charCount)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(exception.Message, exception.InnerException));
                    charCount = i;
                }
            }
        }

        public override string ReadContentAsString()
        {
            string value;
            XmlNode node = this.Node;
            if (node.IsAtomicValue)
            {
                if (_value != null)
                {
                    value = _value;
                    if (node.AttributeText == null)
                        _value = string.Empty;
                }
                else
                {
                    value = node.Value.GetString();
                    SkipValue(node);
                    if (value.Length > _quotas.MaxStringContentLength)
                        XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, _quotas.MaxStringContentLength);
                }
                return value;
            }
            return base.ReadContentAsString(_quotas.MaxStringContentLength);
        }

        public override bool ReadContentAsBoolean()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                bool value = node.Value.ToBoolean();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToBoolean(ReadContentAsString());
        }

        public override long ReadContentAsLong()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                long value = node.Value.ToLong();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToInt64(ReadContentAsString());
        }

        public override int ReadContentAsInt()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                int value = node.Value.ToInt();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToInt32(ReadContentAsString());
        }

        public override DateTime ReadContentAsDateTime()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                DateTime value = node.Value.ToDateTime();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToDateTime(ReadContentAsString());
        }

        public override double ReadContentAsDouble()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                double value = node.Value.ToDouble();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToDouble(ReadContentAsString());
        }

        public override float ReadContentAsFloat()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                float value = node.Value.ToSingle();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToSingle(ReadContentAsString());
        }

        public override decimal ReadContentAsDecimal()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                decimal value = node.Value.ToDecimal();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToDecimal(ReadContentAsString());
        }

        public override UniqueId ReadContentAsUniqueId()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                UniqueId value = node.Value.ToUniqueId();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToUniqueId(ReadContentAsString());
        }

        public override TimeSpan ReadContentAsTimeSpan()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                TimeSpan value = node.Value.ToTimeSpan();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToTimeSpan(ReadContentAsString());
        }

        public override Guid ReadContentAsGuid()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                Guid value = node.Value.ToGuid();
                SkipValue(node);
                return value;
            }
            return XmlConverter.ToGuid(ReadContentAsString());
        }

        public override object ReadContentAsObject()
        {
            XmlNode node = this.Node;
            if (_value == null && node.IsAtomicValue)
            {
                object obj = node.Value.ToObject();
                SkipValue(node);
                return obj;
            }
            return ReadContentAsString();
        }

        public override object ReadContentAs(Type type, IXmlNamespaceResolver? namespaceResolver)
        {
            if (type == typeof(ulong))
            {
                if (_value == null && _node.IsAtomicValue)
                {
                    ulong value = _node.Value.ToULong();
                    SkipValue(_node);
                    return value;
                }
                else
                {
                    return XmlConverter.ToUInt64(ReadContentAsString());
                }
            }
            else if (type == typeof(bool))
                return ReadContentAsBoolean();
            else if (type == typeof(int))
                return ReadContentAsInt();
            else if (type == typeof(long))
                return ReadContentAsLong();
            else if (type == typeof(float))
                return ReadContentAsFloat();
            else if (type == typeof(double))
                return ReadContentAsDouble();
            else if (type == typeof(decimal))
                return ReadContentAsDecimal();
            else if (type == typeof(DateTime))
                return ReadContentAsDateTime();
            else if (type == typeof(UniqueId))
                return ReadContentAsUniqueId();
            else if (type == typeof(Guid))
                return ReadContentAsGuid();
            else if (type == typeof(TimeSpan))
                return ReadContentAsTimeSpan();
            else if (type == typeof(object))
                return ReadContentAsObject();
            else
                return base.ReadContentAs(type, namespaceResolver);
        }

        public override void ResolveEntity()
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlInvalidOperation));
        }

        public override void Skip()
        {
            if (_node.ReadState != ReadState.Interactive)
                return;
            if ((_node.NodeType == XmlNodeType.Element || MoveToElement()) && !IsEmptyElement)
            {
                int depth = Depth;
                while (Read() && depth < Depth)
                {
                    // Nothing, just read on
                }
                // consume end tag
                if (_node.NodeType == XmlNodeType.EndElement)
                    Read();
            }
            else
            {
                Read();
            }
        }

        public override string Value => _value ??= _node.ValueAsString;

        public override Type ValueType
        {
            get
            {
                if (_value == null && _node.QNameType == QNameType.Normal)
                {
                    Type type = _node.Value.ToType();
                    if (_node.IsAtomicValue)
                        return type;
                    if (type == typeof(byte[]))
                        return type;
                }
                return typeof(string);
            }
        }

        public override string XmlLang
        {
            get
            {
                return _nsMgr.XmlLang;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return _nsMgr.XmlSpace;
            }
        }

        public override bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? localName)
        {
            return _node.TryGetLocalNameAsDictionaryString(out localName);
        }

        public override bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? localName)
        {
            return _node.TryGetNamespaceUriAsDictionaryString(out localName);
        }

        public override bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? value)
        {
            return _node.TryGetValueAsDictionaryString(out value);
        }

        public override short[] ReadInt16Array(string localName, string namespaceUri)
        {
            return Int16ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override short[] ReadInt16Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int16ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override int[] ReadInt32Array(string localName, string namespaceUri)
        {
            return Int32ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override int[] ReadInt32Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int32ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override long[] ReadInt64Array(string localName, string namespaceUri)
        {
            return Int64ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override long[] ReadInt64Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int64ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override float[] ReadSingleArray(string localName, string namespaceUri)
        {
            return SingleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override float[] ReadSingleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return SingleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override double[] ReadDoubleArray(string localName, string namespaceUri)
        {
            return DoubleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override double[] ReadDoubleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DoubleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override decimal[] ReadDecimalArray(string localName, string namespaceUri)
        {
            return DecimalArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override decimal[] ReadDecimalArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DecimalArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override DateTime[] ReadDateTimeArray(string localName, string namespaceUri)
        {
            return DateTimeArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override DateTime[] ReadDateTimeArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DateTimeArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override Guid[] ReadGuidArray(string localName, string namespaceUri)
        {
            return GuidArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override Guid[] ReadGuidArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return GuidArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override TimeSpan[] ReadTimeSpanArray(string localName, string namespaceUri)
        {
            return TimeSpanArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public override TimeSpan[] ReadTimeSpanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return TimeSpanArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
        }

        public string GetOpenElements()
        {
            string s = string.Empty;
            for (int i = _depth; i > 0; i--)
            {
                string localName = _elementNodes![i].LocalName.GetString();
                if (i != _depth)
                    s += ", ";
                s += localName;
            }
            return s;
        }

        private char[] GetCharBuffer(int count)
        {
            if (count > 1024)
                return new char[count];

            if (_chars == null || _chars.Length < count)
                _chars = new char[count];

            return _chars;
        }

        private void SignStartElement(XmlSigningNodeWriter writer)
        {
            int prefixOffset, prefixLength;
            byte[] prefixBuffer = _node.Prefix.GetString(out prefixOffset, out prefixLength);
            int localNameOffset, localNameLength;
            byte[] localNameBuffer = _node.LocalName.GetString(out localNameOffset, out localNameLength);
            writer.WriteStartElement(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
        }

        private static void SignAttribute(XmlSigningNodeWriter writer, XmlAttributeNode attributeNode)
        {
            QNameType qnameType = attributeNode.QNameType;
            if (qnameType == QNameType.Normal)
            {
                int prefixOffset, prefixLength;
                byte[] prefixBuffer = attributeNode.Prefix.GetString(out prefixOffset, out prefixLength);
                int localNameOffset, localNameLength;
                byte[] localNameBuffer = attributeNode.LocalName.GetString(out localNameOffset, out localNameLength);
                writer.WriteStartAttribute(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
                attributeNode.Value.Sign(writer);
                writer.WriteEndAttribute();
            }
            else
            {
                Fx.Assert(qnameType == QNameType.Xmlns, "");
                int prefixOffset, prefixLength;
                byte[] prefixBuffer = attributeNode.Namespace.Prefix.GetString(out prefixOffset, out prefixLength);
                int nsOffset, nsLength;
                byte[] nsBuffer = attributeNode.Namespace.Uri.GetString(out nsOffset, out nsLength);
                writer.WriteXmlnsAttribute(prefixBuffer, prefixOffset, prefixLength, nsBuffer, nsOffset, nsLength);
            }
        }

        private void SignEndElement(XmlSigningNodeWriter writer)
        {
            int prefixOffset, prefixLength;
            byte[] prefixBuffer = _node.Prefix.GetString(out prefixOffset, out prefixLength);
            int localNameOffset, localNameLength;
            byte[] localNameBuffer = _node.LocalName.GetString(out localNameOffset, out localNameLength);
            writer.WriteEndElement(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
        }

        private void SignNode(XmlSigningNodeWriter writer)
        {
            switch (_node.NodeType)
            {
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Element:
                    SignStartElement(writer);
                    for (int i = 0; i < _attributeCount; i++)
                        SignAttribute(writer, _attributeNodes![i]);
                    writer.WriteEndStartElement(_node.IsEmptyElement);
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                    _node.Value.Sign(writer);
                    break;
                case XmlNodeType.XmlDeclaration:
                    writer.WriteDeclaration();
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(_node.Value.GetString());
                    break;
                case XmlNodeType.EndElement:
                    SignEndElement(writer);
                    break;
                default:
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
            }
        }

        public override bool CanCanonicalize
        {
            get
            {
                return true;
            }
        }

        protected bool Signing
        {
            get
            {
                return _signing;
            }
        }

        protected void SignNode()
        {
            if (_signing)
            {
                SignNode(_signingWriter!);
            }
        }

        public override void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            if (_signing)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlCanonicalizationStarted));

            _signingWriter ??= CreateSigningNodeWriter();

            _signingWriter.SetOutput(XmlNodeWriter.Null, stream, includeComments, inclusivePrefixes);
            _nsMgr.Sign(_signingWriter);
            _signing = true;
        }

        public override void EndCanonicalization()
        {
            if (!_signing)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlCanonicalizationNotStarted));

            _signingWriter!.Flush();
            _signingWriter.Close();
            _signing = false;
        }

        protected abstract XmlSigningNodeWriter CreateSigningNodeWriter();

        protected enum QNameType
        {
            Normal,
            Xmlns,
        }

        protected class XmlNode
        {
            private XmlNodeType _nodeType;
            private readonly PrefixHandle _prefix;
            private readonly StringHandle _localName;
            private readonly ValueHandle _value;
            private Namespace _ns;
            private readonly bool _hasValue;
            private readonly bool _canGetAttribute;
            private readonly bool _canMoveToElement;
            private readonly ReadState _readState;
            private readonly XmlAttributeTextNode? _attributeTextNode;
            private bool _exitScope;
            private readonly int _depthDelta;
            private bool _isAtomicValue;
            private readonly bool _skipValue;
            private QNameType _qnameType;
            private readonly bool _hasContent;
            private bool _isEmptyElement;
            private char _quoteChar;

            protected enum XmlNodeFlags
            {
                None = 0x00,
                CanGetAttribute = 0x01,
                CanMoveToElement = 0x02,
                HasValue = 0x04,
                AtomicValue = 0x08,
                SkipValue = 0x10,
                HasContent = 0x20
            }

            protected XmlNode(XmlNodeType nodeType,
                              PrefixHandle prefix,
                              StringHandle localName,
                              ValueHandle value,
                              XmlNodeFlags nodeFlags,
                              ReadState readState,
                              XmlAttributeTextNode? attributeTextNode,
                              int depthDelta)
            {
                _nodeType = nodeType;
                _prefix = prefix;
                _localName = localName;
                _value = value;
                _ns = NamespaceManager.EmptyNamespace;
                _hasValue = ((nodeFlags & XmlNodeFlags.HasValue) != 0);
                _canGetAttribute = ((nodeFlags & XmlNodeFlags.CanGetAttribute) != 0);
                _canMoveToElement = ((nodeFlags & XmlNodeFlags.CanMoveToElement) != 0);
                _isAtomicValue = ((nodeFlags & XmlNodeFlags.AtomicValue) != 0);
                _skipValue = ((nodeFlags & XmlNodeFlags.SkipValue) != 0);
                _hasContent = ((nodeFlags & XmlNodeFlags.HasContent) != 0);
                _readState = readState;
                _attributeTextNode = attributeTextNode;
                _exitScope = (nodeType == XmlNodeType.EndElement);
                _depthDelta = depthDelta;
                _isEmptyElement = false;
                _quoteChar = '"';

                _qnameType = QNameType.Normal;
            }

            // Most nodes are read-only and fixed for the particular node type, but a few need to be tweaked
            // QNameType needs to get set for all nodes with a qname (Element/Attribute)
            // NodeType gets set for WhiteSpace vs. SignificantWhitespace
            // ExitScope/IsEmptyElement is only updated by text for empty elements
            // QuoteChar is only updated by text for attributes
            // IsAtomicValue is set to false for XmlnsAttributes so we don't have to check QNameType

            public bool HasValue { get { return _hasValue; } }
            public ReadState ReadState { get { return _readState; } }
            public StringHandle LocalName { get { DiagnosticUtility.DebugAssert(_qnameType != QNameType.Xmlns, ""); return _localName; } }
            public PrefixHandle Prefix { get { DiagnosticUtility.DebugAssert(_qnameType != QNameType.Xmlns, ""); return _prefix; } }
            public bool CanGetAttribute { get { return _canGetAttribute; } }
            public bool CanMoveToElement { get { return _canMoveToElement; } }
            public XmlAttributeTextNode? AttributeText { get { return _attributeTextNode; } }
            public bool SkipValue { get { return _skipValue; } }
            public ValueHandle Value { get { DiagnosticUtility.DebugAssert(_qnameType != QNameType.Xmlns, ""); return _value; } }
            public int DepthDelta { get { return _depthDelta; } }
            public bool HasContent { get { return _hasContent; } }

            public XmlNodeType NodeType
            {
                get
                {
                    return _nodeType;
                }
                set
                {
                    _nodeType = value;
                }
            }

            public QNameType QNameType
            {
                get
                {
                    return _qnameType;
                }
                set
                {
                    _qnameType = value;
                }
            }

            public Namespace Namespace
            {
                get
                {
                    return _ns;
                }
                set
                {
                    _ns = value;
                }
            }

            public bool IsAtomicValue
            {
                get
                {
                    return _isAtomicValue;
                }
                set
                {
                    _isAtomicValue = value;
                }
            }

            public bool ExitScope
            {
                get
                {
                    return _exitScope;
                }
                set
                {
                    _exitScope = value;
                }
            }

            public bool IsEmptyElement
            {
                get
                {
                    return _isEmptyElement;
                }
                set
                {
                    _isEmptyElement = value;
                }
            }

            public char QuoteChar
            {
                get
                {
                    return _quoteChar;
                }
                set
                {
                    _quoteChar = value;
                }
            }

            public bool IsLocalName(string localName)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.LocalName == localName;
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return this.Namespace.Prefix == localName;
                }
            }

            public bool IsLocalName(XmlDictionaryString localName)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.LocalName == localName;
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return this.Namespace.Prefix == localName;
                }
            }

            public bool IsNamespaceUri(string ns)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.Namespace.IsUri(ns);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return ns == xmlnsNamespace;
                }
            }

            public bool IsNamespaceUri(XmlDictionaryString ns)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.Namespace.IsUri(ns);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return ns.Value == xmlnsNamespace;
                }
            }

            public bool IsLocalNameAndNamespaceUri(string localName, string ns)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.LocalName == localName && this.Namespace.IsUri(ns);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return this.Namespace.Prefix == localName && ns == xmlnsNamespace;
                }
            }

            public bool IsLocalNameAndNamespaceUri(XmlDictionaryString localName, XmlDictionaryString ns)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.LocalName == localName && this.Namespace.IsUri(ns);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return this.Namespace.Prefix == localName && ns.Value == xmlnsNamespace;
                }
            }
            public bool IsPrefixAndLocalName(string prefix, string localName)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.Prefix == prefix && this.LocalName == localName;
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    return prefix == xmlns && this.Namespace.Prefix == localName;
                }
            }

            public bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? localName)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.LocalName.TryGetDictionaryString(out localName);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    localName = null;
                    return false;
                }
            }

            public bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? ns)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.Namespace.Uri.TryGetDictionaryString(out ns);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    ns = null;
                    return false;
                }
            }

            public bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? value)
            {
                if (_qnameType == QNameType.Normal)
                {
                    return this.Value.TryGetDictionaryString(out value);
                }
                else
                {
                    DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                    value = null;
                    return false;
                }
            }
            public string ValueAsString
            {
                get
                {
                    if (_qnameType == QNameType.Normal)
                    {
                        return Value.GetString();
                    }
                    else
                    {
                        DiagnosticUtility.DebugAssert(_qnameType == QNameType.Xmlns, "");
                        return Namespace.Uri.GetString();
                    }
                }
            }
        }

        protected sealed class XmlElementNode : XmlNode
        {
            private readonly XmlEndElementNode _endElementNode;
            private int _bufferOffset;

            public XmlElementNode(XmlBufferReader bufferReader)
                : this(new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader))
            {
            }

            private XmlElementNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
                : base(XmlNodeType.Element,
                       prefix,
                       localName,
                       value,
                       XmlNodeFlags.CanGetAttribute | XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       -1)
            {
                _endElementNode = new XmlEndElementNode(prefix, localName, value);
            }

            public XmlEndElementNode EndElement
            {
                get
                {
                    return _endElementNode;
                }
            }

            public int BufferOffset
            {
                get
                {
                    return _bufferOffset;
                }
                set
                {
                    _bufferOffset = value;
                }
            }

            public int NameOffset;
            public int NameLength;
        }

        protected sealed class XmlAttributeNode : XmlNode
        {
            public XmlAttributeNode(XmlBufferReader bufferReader)
                : this(new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader))
            {
            }

            private XmlAttributeNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
                : base(XmlNodeType.Attribute,
                       prefix,
                       localName,
                       value,
                       XmlNodeFlags.CanGetAttribute | XmlNodeFlags.CanMoveToElement | XmlNodeFlags.HasValue | XmlNodeFlags.AtomicValue,
                       ReadState.Interactive,
                       new XmlAttributeTextNode(prefix, localName, value),
                       0)
            {
            }
        }

        protected sealed class XmlEndElementNode : XmlNode
        {
            public XmlEndElementNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
                : base(XmlNodeType.EndElement,
                       prefix,
                       localName,
                       value,
                       XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       -1)
            {
            }
        }

        protected class XmlTextNode : XmlNode
        {
            protected XmlTextNode(XmlNodeType nodeType,
                              PrefixHandle prefix,
                              StringHandle localName,
                              ValueHandle value,
                              XmlNodeFlags nodeFlags,
                              ReadState readState,
                              XmlAttributeTextNode? attributeTextNode,
                              int depthDelta)
                :
                base(nodeType, prefix, localName, value, nodeFlags, readState, attributeTextNode, depthDelta)
            {
            }
        }

        protected  sealed class XmlAtomicTextNode : XmlTextNode
        {
            public XmlAtomicTextNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.Text,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.HasValue | XmlNodeFlags.AtomicValue | XmlNodeFlags.SkipValue | XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlComplexTextNode : XmlTextNode
        {
            public XmlComplexTextNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.Text,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.HasValue | XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlWhitespaceTextNode : XmlTextNode
        {
            public XmlWhitespaceTextNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.Whitespace,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.HasValue,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlCDataNode : XmlTextNode
        {
            public XmlCDataNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.CDATA,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.HasValue | XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlAttributeTextNode : XmlTextNode
        {
            public XmlAttributeTextNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
                : base(XmlNodeType.Text,
                       prefix,
                       localName,
                       value,
                       XmlNodeFlags.HasValue | XmlNodeFlags.CanGetAttribute | XmlNodeFlags.CanMoveToElement | XmlNodeFlags.AtomicValue | XmlNodeFlags.HasContent,
                       ReadState.Interactive,
                       null,
                       1)
            {
            }
        }

        protected sealed class XmlInitialNode : XmlNode
        {
            public XmlInitialNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.None,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.None,
                       ReadState.Initial,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlDeclarationNode : XmlNode
        {
            public XmlDeclarationNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.XmlDeclaration,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.CanGetAttribute,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlCommentNode : XmlNode
        {
            public XmlCommentNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.Comment,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.HasValue,
                       ReadState.Interactive,
                       null,
                       0)
            {
            }
        }
        protected sealed class XmlEndOfFileNode : XmlNode
        {
            public XmlEndOfFileNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.None,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.None,
                       ReadState.EndOfFile,
                       null,
                       0)
            {
            }
        }

        protected sealed class XmlClosedNode : XmlNode
        {
            public XmlClosedNode(XmlBufferReader bufferReader)
                : base(XmlNodeType.None,
                       new PrefixHandle(bufferReader),
                       new StringHandle(bufferReader),
                       new ValueHandle(bufferReader),
                       XmlNodeFlags.None,
                       ReadState.Closed,
                       null,
                       0)
            {
            }
        }

        private sealed class AttributeSorter : IComparer
        {
            private object[]? _indices;
            private XmlAttributeNode[]? _attributeNodes;
            private int _attributeCount;
            private int _attributeIndex1;
            private int _attributeIndex2;

            public bool Sort(XmlAttributeNode[] attributeNodes, int attributeCount)
            {
                _attributeIndex1 = -1;
                _attributeIndex2 = -1;
                _attributeNodes = attributeNodes;
                _attributeCount = attributeCount;
                bool sorted = Sort();
                _attributeNodes = null;
                _attributeCount = 0;
                return sorted;
            }

            public void GetIndices(out int attributeIndex1, out int attributeIndex2)
            {
                attributeIndex1 = _attributeIndex1;
                attributeIndex2 = _attributeIndex2;
            }

            public void Close()
            {
                if (_indices != null && _indices.Length > 32)
                {
                    _indices = null;
                }
            }

            private bool Sort()
            {
                // Optimistically use the last sort order and check to see if that works.  This helps the case
                // where elements with large numbers of attributes are repeated.
                if (_indices != null && _indices.Length == _attributeCount && IsSorted())
                    return true;

                object[] newIndices = new object[_attributeCount];
                for (int i = 0; i < newIndices.Length; i++)
                    newIndices[i] = i;
                _indices = newIndices;
                Array.Sort(_indices, 0, _attributeCount, this);
                return IsSorted();
            }

            private bool IsSorted()
            {
                for (int i = 0; i < _indices!.Length - 1; i++)
                {
                    if (Compare(_indices[i], _indices[i + 1]) >= 0)
                    {
                        _attributeIndex1 = (int)_indices[i];
                        _attributeIndex2 = (int)_indices[i + 1];
                        return false;
                    }
                }
                return true;
            }

            public int Compare(object? obj1, object? obj2)
            {
                int index1 = (int)obj1!;
                int index2 = (int)obj2!;
                XmlAttributeNode attribute1 = _attributeNodes![index1];
                XmlAttributeNode attribute2 = _attributeNodes![index2];

                int i = CompareQNameType(attribute1.QNameType, attribute2.QNameType);
                if (i == 0)
                {
                    QNameType qnameType = attribute1.QNameType;
                    if (qnameType == QNameType.Normal)
                    {
                        i = attribute1.LocalName.CompareTo(attribute2.LocalName);
                        if (i == 0)
                        {
                            i = attribute1.Namespace.Uri.CompareTo(attribute2.Namespace.Uri);
                        }
                    }
                    else
                    {
                        DiagnosticUtility.DebugAssert(qnameType == QNameType.Xmlns, "");
                        i = attribute1.Namespace.Prefix.CompareTo(attribute2.Namespace.Prefix);
                    }
                }

                return i;
            }

            public static int CompareQNameType(QNameType type1, QNameType type2)
            {
                return (int)type1 - (int)type2;
            }
        }

        private sealed class NamespaceManager
        {
            private readonly XmlBufferReader _bufferReader;
            private Namespace[]? _namespaces;
            private int _nsCount;
            private int _depth;
            private readonly Namespace?[] _shortPrefixUri;
            private static readonly Namespace s_emptyNamespace = new Namespace(XmlBufferReader.Empty);
            private static Namespace? s_xmlNamespace;
            private XmlAttribute[]? _attributes;
            private int _attributeCount;
            private XmlSpace _space;
            private string _lang;

            public NamespaceManager(XmlBufferReader bufferReader)
            {
                _bufferReader = bufferReader;
                _shortPrefixUri = new Namespace[(int)PrefixHandleType.Max];
                _shortPrefixUri[(int)PrefixHandleType.Empty] = s_emptyNamespace;
                _namespaces = null;
                _nsCount = 0;
                _attributes = null;
                _attributeCount = 0;
                _space = XmlSpace.None;
                _lang = string.Empty;
                _depth = 0;
            }

            public void Close()
            {
                if (_namespaces != null && _namespaces.Length > 32)
                    _namespaces = null;
                if (_attributes != null && _attributes.Length > 4)
                    _attributes = null;
                _lang = string.Empty;
            }

            public static Namespace XmlNamespace
            {
                get
                {
                    if (s_xmlNamespace == null)
                    {
                        byte[] xmlBuffer = "xmlhttp://www.w3.org/XML/1998/namespace"u8.ToArray();
                        Namespace nameSpace = new Namespace(new XmlBufferReader(xmlBuffer));
                        nameSpace.Prefix.SetValue(0, 3);
                        nameSpace.Uri.SetValue(3, xmlBuffer.Length - 3);
                        s_xmlNamespace = nameSpace;
                    }
                    return s_xmlNamespace;
                }
            }

            public static Namespace EmptyNamespace
            {
                get
                {
                    return s_emptyNamespace;
                }
            }

            public string XmlLang
            {
                get
                {
                    return _lang;
                }
            }

            public XmlSpace XmlSpace
            {
                get
                {
                    return _space;
                }
            }

            public void Clear()
            {
                if (_nsCount != 0)
                {
                    for (int i = 0; i < _shortPrefixUri.Length; i++)
                    {
                        _shortPrefixUri[i] = null;
                    }
                    _shortPrefixUri[(int)PrefixHandleType.Empty] = s_emptyNamespace;
                    _nsCount = 0;
                }
                _attributeCount = 0;
                _space = XmlSpace.None;
                _lang = string.Empty;
                _depth = 0;
            }

            public void EnterScope()
            {
                _depth++;
            }

            public void ExitScope()
            {
                while (_nsCount > 0)
                {
                    Namespace nameSpace = _namespaces![_nsCount - 1];
                    if (nameSpace.Depth != _depth)
                        break;
                    PrefixHandleType shortPrefix;
                    if (nameSpace.Prefix.TryGetShortPrefix(out shortPrefix))
                    {
                        _shortPrefixUri[(int)shortPrefix] = nameSpace.OuterUri;
                    }
                    _nsCount--;
                }
                while (_attributeCount > 0)
                {
                    XmlAttribute attribute = _attributes![_attributeCount - 1];
                    if (attribute.Depth != _depth)
                        break;
                    _space = attribute.XmlSpace;
                    _lang = attribute.XmlLang;
                    _attributeCount--;
                }
                _depth--;
            }

            public void Sign(XmlSigningNodeWriter writer)
            {
                for (int i = 0; i < _nsCount; i++)
                {
                    PrefixHandle prefix = _namespaces![i].Prefix;
                    bool found = false;
                    for (int j = i + 1; j < _nsCount; j++)
                    {
                        if (Equals(prefix, _namespaces[j].Prefix))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        int prefixOffset, prefixLength;
                        byte[] prefixBuffer = prefix.GetString(out prefixOffset, out prefixLength);
                        int nsOffset, nsLength;
                        byte[] nsBuffer = _namespaces[i].Uri.GetString(out nsOffset, out nsLength);
                        writer.WriteXmlnsAttribute(prefixBuffer, prefixOffset, prefixLength, nsBuffer, nsOffset, nsLength);
                    }
                }
            }

            public void AddLangAttribute(string lang)
            {
                AddAttribute();
                _lang = lang;
            }

            public void AddSpaceAttribute(XmlSpace space)
            {
                AddAttribute();
                _space = space;
            }

            private void AddAttribute()
            {
                if (_attributes == null)
                {
                    _attributes = new XmlAttribute[1];
                }
                else if (_attributes.Length == _attributeCount)
                {
                    XmlAttribute[] newAttributes = new XmlAttribute[_attributeCount * 2];
                    Array.Copy(_attributes, newAttributes, _attributeCount);
                    _attributes = newAttributes;
                }
                XmlAttribute attribute = _attributes[_attributeCount];
                if (attribute == null)
                {
                    attribute = new XmlAttribute();
                    _attributes[_attributeCount] = attribute;
                }
                attribute.XmlLang = _lang;
                attribute.XmlSpace = _space;
                attribute.Depth = _depth;
                _attributeCount++;
            }

            public void Register(Namespace nameSpace)
            {
                PrefixHandleType shortPrefix;
                if (nameSpace.Prefix.TryGetShortPrefix(out shortPrefix))
                {
                    nameSpace.OuterUri = _shortPrefixUri[(int)shortPrefix];
                    _shortPrefixUri[(int)shortPrefix] = nameSpace;
                }
                else
                {
                    nameSpace.OuterUri = null;
                }
            }

            public Namespace AddNamespace()
            {
                if (_namespaces == null)
                {
                    _namespaces = new Namespace[4];
                }
                else if (_namespaces.Length == _nsCount)
                {
                    Namespace[] newNamespaces = new Namespace[_nsCount * 2];
                    Array.Copy(_namespaces, newNamespaces, _nsCount);
                    _namespaces = newNamespaces;
                }
                Namespace nameSpace = _namespaces[_nsCount];
                if (nameSpace == null)
                {
                    nameSpace = new Namespace(_bufferReader);
                    _namespaces[_nsCount] = nameSpace;
                }
                nameSpace.Clear();
                nameSpace.Depth = _depth;
                _nsCount++;
                return nameSpace;
            }

            public Namespace? LookupNamespace(PrefixHandleType prefix)
            {
                return _shortPrefixUri[(int)prefix];
            }

            public Namespace? LookupNamespace(PrefixHandle prefix)
            {
                PrefixHandleType shortPrefix;
                if (prefix.TryGetShortPrefix(out shortPrefix))
                    return LookupNamespace(shortPrefix);
                for (int i = _nsCount - 1; i >= 0; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.Prefix == prefix)
                        return nameSpace;
                }
                if (prefix.IsXml)
                    return XmlNamespace;
                return null;
            }
            public Namespace? LookupNamespace(string prefix)
            {
                PrefixHandleType shortPrefix;
                if (TryGetShortPrefix(prefix, out shortPrefix))
                    return LookupNamespace(shortPrefix);
                for (int i = _nsCount - 1; i >= 0; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.Prefix == prefix)
                        return nameSpace;
                }
                if (prefix == "xml")
                    return XmlNamespace;
                return null;
            }

            private static bool TryGetShortPrefix(string s, out PrefixHandleType shortPrefix)
            {
                int length = s.Length;
                if (length == 0)
                {
                    shortPrefix = PrefixHandleType.Empty;
                    return true;
                }
                if (length == 1)
                {
                    char ch = s[0];
                    if (char.IsAsciiLetterLower(ch))
                    {
                        shortPrefix = PrefixHandle.GetAlphaPrefix(ch - 'a');
                        return true;
                    }
                }
                shortPrefix = PrefixHandleType.Empty;
                return false;
            }

            private sealed class XmlAttribute
            {
                private XmlSpace _space;
                private string _lang = string.Empty;
                private int _depth;

                public XmlAttribute()
                {
                }

                public int Depth
                {
                    get
                    {
                        return _depth;
                    }
                    set
                    {
                        _depth = value;
                    }
                }

                public string XmlLang
                {
                    get
                    {
                        return _lang;
                    }
                    set
                    {
                        _lang = value;
                    }
                }

                public XmlSpace XmlSpace
                {
                    get
                    {
                        return _space;
                    }
                    set
                    {
                        _space = value;
                    }
                }
            }
        }
        protected sealed class Namespace
        {
            private readonly PrefixHandle _prefix;
            private readonly StringHandle _uri;
            private int _depth;
            private Namespace? _outerUri;
            private string? _uriString;

            public Namespace(XmlBufferReader bufferReader)
            {
                _prefix = new PrefixHandle(bufferReader);
                _uri = new StringHandle(bufferReader);
                _outerUri = null;
                _uriString = null;
            }

            public void Clear()
            {
                _uriString = null;
            }

            public int Depth
            {
                get
                {
                    return _depth;
                }
                set
                {
                    _depth = value;
                }
            }

            public PrefixHandle Prefix
            {
                get
                {
                    return _prefix;
                }
            }

            public bool IsUri(string s)
            {
                DiagnosticUtility.DebugAssert(s != null, "");
                if (object.ReferenceEquals(s, _uriString))
                    return true;
                if (_uri == s)
                {
                    _uriString = s;
                    return true;
                }
                return false;
            }

            public bool IsUri(XmlDictionaryString s)
            {
                if (object.ReferenceEquals(s.Value, _uriString))
                    return true;
                if (_uri == s)
                {
                    _uriString = s.Value;
                    return true;
                }
                return false;
            }
            public StringHandle Uri
            {
                get
                {
                    return _uri;
                }
            }

            public Namespace? OuterUri
            {
                get
                {
                    return _outerUri;
                }
                set
                {
                    _outerUri = value;
                }
            }
        }
    }
}
