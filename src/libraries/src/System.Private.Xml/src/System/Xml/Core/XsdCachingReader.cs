// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;
using System.Collections;

namespace System.Xml
{
    internal partial class XsdCachingReader : XmlReader, IXmlLineInfo
    {
        private enum CachingReaderState
        {
            None = 0,
            Init = 1,
            Record = 2,
            Replay = 3,
            ReaderClosed = 4,
            Error = 5,
        }

        private XmlReader _coreReader;
        private XmlNameTable _coreReaderNameTable;

        private ValidatingReaderNodeData[] _contentEvents;
        private ValidatingReaderNodeData[] _attributeEvents;

        private ValidatingReaderNodeData _cachedNode;

        private CachingReaderState _cacheState;
        private int _contentIndex;
        private int _attributeCount;

        private bool _returnOriginalStringValues;

        private CachingEventHandler _cacheHandler;

        //current state
        private int _currentAttrIndex;
        private int _currentContentIndex;
        private bool _readAhead;

        //Lineinfo
        private IXmlLineInfo _lineInfo;

        //ReadAttributeValue TextNode
        private ValidatingReaderNodeData _textNode;

        //Constants
        private const int InitialAttributeCount = 8;
        private const int InitialContentCount = 4;

        //Constructor
        internal XsdCachingReader(XmlReader reader, IXmlLineInfo lineInfo, CachingEventHandler handlerMethod)
        {
            _coreReader = reader;
            _lineInfo = lineInfo;
            _cacheHandler = handlerMethod;
            _attributeEvents = new ValidatingReaderNodeData[InitialAttributeCount];
            _contentEvents = new ValidatingReaderNodeData[InitialContentCount];
            Init();
        }

        private void Init()
        {
            _coreReaderNameTable = _coreReader.NameTable;
            _cacheState = CachingReaderState.Init;
            _contentIndex = 0;
            _currentAttrIndex = -1;
            _currentContentIndex = -1;
            _attributeCount = 0;
            _cachedNode = null;
            _readAhead = false;
            //Initialize the cachingReader with start state
            if (_coreReader.NodeType == XmlNodeType.Element)
            {
                ValidatingReaderNodeData element = AddContent(_coreReader.NodeType);
                element.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);  //Only created for element node type
                element.SetLineInfo(_lineInfo);
                RecordAttributes();
            }
        }

        internal void Reset(XmlReader reader)
        {
            _coreReader = reader;
            Init();
        }

        // Settings
        public override XmlReaderSettings Settings
        {
            get
            {
                return _coreReader.Settings;
            }
        }

        // Node Properties

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                return _cachedNode.NodeType;
            }
        }

        // Gets the name of the current node, including the namespace prefix.
        public override string Name
        {
            get
            {
                return _cachedNode.GetAtomizedNameWPrefix(_coreReaderNameTable);
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get
            {
                return _cachedNode.LocalName;
            }
        }

        // Gets the namespace URN (as defined in the W3C Namespace Specification) of the current namespace scope.
        public override string NamespaceURI
        {
            get
            {
                return _cachedNode.Namespace;
            }
        }

        // Gets the namespace prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                return _cachedNode.Prefix;
            }
        }

        // Gets a value indicating whether the current node can have a non-empty Value.
        public override bool HasValue
        {
            get
            {
                return XmlReader.HasValueInternal(_cachedNode.NodeType);
            }
        }

        // Gets the text value of the current node.
        public override string Value
        {
            get
            {
                return _returnOriginalStringValues ? _cachedNode.OriginalStringValue : _cachedNode.RawValue;
            }
        }

        // Gets the depth of the current node in the XML element stack.
        public override int Depth
        {
            get
            {
                return _cachedNode.Depth;
            }
        }

        // Gets the base URI of the current node.
        public override string BaseURI
        {
            get
            {
                return _coreReader.BaseURI;
            }
        }

        // Gets a value indicating whether the current node is an empty element (for example, <MyElement/>).
        public override bool IsEmptyElement
        {
            get
            {
                return false;
            }
        }

        // Gets a value indicating whether the current node is an attribute that was generated from the default value defined
        // in the DTD or schema.
        public override bool IsDefault
        {
            get
            {
                return false;
            }
        }

        // Gets the quotation mark character used to enclose the value of an attribute node.
        public override char QuoteChar
        {
            get
            {
                return _coreReader.QuoteChar;
            }
        }

        // Gets the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                return _coreReader.XmlSpace;
            }
        }

        // Gets the current xml:lang scope.
        public override string XmlLang
        {
            get
            {
                return _coreReader.XmlLang;
            }
        }

        // Attribute Accessors

        // The number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                return _attributeCount;
            }
        }

        // Gets the value of the attribute with the specified Name.
        public override string GetAttribute(string name)
        {
            int i;
            if (name.IndexOf(':') == -1)
            {
                i = GetAttributeIndexWithoutPrefix(name);
            }
            else
            {
                i = GetAttributeIndexWithPrefix(name);
            }
            return (i >= 0) ? _attributeEvents[i].RawValue : null;
        }

        // Gets the value of the attribute with the specified LocalName and NamespaceURI.
        public override string GetAttribute(string name, string namespaceURI)
        {
            namespaceURI = (namespaceURI == null) ? string.Empty : _coreReaderNameTable.Get(namespaceURI);
            name = _coreReaderNameTable.Get(name);
            ValidatingReaderNodeData attribute;
            for (int i = 0; i < _attributeCount; i++)
            {
                attribute = _attributeEvents[i];
                if (Ref.Equal(attribute.LocalName, name) && Ref.Equal(attribute.Namespace, namespaceURI))
                {
                    return attribute.RawValue;
                }
            }
            return null;
        }

        // Gets the value of the attribute with the specified index.
        public override string GetAttribute(int i)
        {
            if (i < 0 || i >= _attributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            return _attributeEvents[i].RawValue;
        }

        // Gets the value of the attribute with the specified index.
        public override string this[int i]
        {
            get
            {
                return GetAttribute(i);
            }
        }

        // Gets the value of the attribute with the specified Name.
        public override string this[string name]
        {
            get
            {
                return GetAttribute(name);
            }
        }

        // Gets the value of the attribute with the specified LocalName and NamespaceURI.
        public override string this[string name, string namespaceURI]
        {
            get
            {
                return GetAttribute(name, namespaceURI);
            }
        }

        // Moves to the attribute with the specified Name.
        public override bool MoveToAttribute(string name)
        {
            int i;
            if (name.IndexOf(':') == -1)
            {
                i = GetAttributeIndexWithoutPrefix(name);
            }
            else
            {
                i = GetAttributeIndexWithPrefix(name);
            }

            if (i >= 0)
            {
                _currentAttrIndex = i;
                _cachedNode = _attributeEvents[i];
                return true;
            }
            else
            {
                return false;
            }
        }

        // Moves to the attribute with the specified LocalName and NamespaceURI
        public override bool MoveToAttribute(string name, string ns)
        {
            ns = (ns == null) ? string.Empty : _coreReaderNameTable.Get(ns);
            name = _coreReaderNameTable.Get(name);
            ValidatingReaderNodeData attribute;
            for (int i = 0; i < _attributeCount; i++)
            {
                attribute = _attributeEvents[i];
                if (Ref.Equal(attribute.LocalName, name) &&
                     Ref.Equal(attribute.Namespace, ns))
                {
                    _currentAttrIndex = i;
                    _cachedNode = _attributeEvents[i];
                    return true;
                }
            }
            return false;
        }

        // Moves to the attribute with the specified index.
        public override void MoveToAttribute(int i)
        {
            if (i < 0 || i >= _attributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            _currentAttrIndex = i;
            _cachedNode = _attributeEvents[i];
        }

        // Moves to the first attribute.
        public override bool MoveToFirstAttribute()
        {
            if (_attributeCount == 0)
            {
                return false;
            }
            _currentAttrIndex = 0;
            _cachedNode = _attributeEvents[0];
            return true;
        }

        // Moves to the next attribute.
        public override bool MoveToNextAttribute()
        {
            if (_currentAttrIndex + 1 < _attributeCount)
            {
                _cachedNode = _attributeEvents[++_currentAttrIndex];
                return true;
            }
            return false;
        }

        // Moves to the element that contains the current attribute node.
        public override bool MoveToElement()
        {
            if (_cacheState != CachingReaderState.Replay || _cachedNode.NodeType != XmlNodeType.Attribute)
            {
                return false;
            }
            _currentContentIndex = 0;
            _currentAttrIndex = -1;
            Read();
            return true;
        }

        // Reads the next node from the stream/TextReader.
        public override bool Read()
        {
            switch (_cacheState)
            {
                case CachingReaderState.Init:
                    _cacheState = CachingReaderState.Record;
                    goto case CachingReaderState.Record;

                case CachingReaderState.Record:
                    ValidatingReaderNodeData recordedNode = null;
                    if (_coreReader.Read())
                    {
                        switch (_coreReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                //Dont record element within the content of a union type since the main reader will break on this and the underlying coreReader will be positioned on this node
                                _cacheState = CachingReaderState.ReaderClosed;
                                return false;

                            case XmlNodeType.EndElement:
                                recordedNode = AddContent(_coreReader.NodeType);
                                recordedNode.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);  //Only created for element node type
                                recordedNode.SetLineInfo(_lineInfo);
                                break;

                            case XmlNodeType.Comment:
                            case XmlNodeType.ProcessingInstruction:
                            case XmlNodeType.Text:
                            case XmlNodeType.CDATA:
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                recordedNode = AddContent(_coreReader.NodeType);
                                recordedNode.SetItemData(_coreReader.Value);
                                recordedNode.SetLineInfo(_lineInfo);
                                recordedNode.Depth = _coreReader.Depth;
                                break;

                            default:
                                break;
                        }
                        _cachedNode = recordedNode;
                        return true;
                    }
                    else
                    {
                        _cacheState = CachingReaderState.ReaderClosed;
                        return false;
                    }

                case CachingReaderState.Replay:
                    if (_currentContentIndex >= _contentIndex)
                    { //When positioned on the last cached node, switch back as the underlying coreReader is still positioned on this node
                        _cacheState = CachingReaderState.ReaderClosed;
                        _cacheHandler(this);
                        if (_coreReader.NodeType != XmlNodeType.Element || _readAhead)
                        { //Only when coreReader not positioned on Element node, read ahead, otherwise it is on the next element node already, since this was not cached
                            return _coreReader.Read();
                        }
                        return true;
                    }
                    _cachedNode = _contentEvents[_currentContentIndex];
                    if (_currentContentIndex > 0)
                    {
                        ClearAttributesInfo();
                    }
                    _currentContentIndex++;
                    return true;

                default:
                    return false;
            }
        }

        internal ValidatingReaderNodeData RecordTextNode(string textValue, string originalStringValue, int depth, int lineNo, int linePos)
        {
            ValidatingReaderNodeData textNode = AddContent(XmlNodeType.Text);
            textNode.SetItemData(textValue, originalStringValue);
            textNode.SetLineInfo(lineNo, linePos);
            textNode.Depth = depth;
            return textNode;
        }

        internal void SwitchTextNodeAndEndElement(string textValue, string originalStringValue)
        {
            Debug.Assert(_coreReader.NodeType == XmlNodeType.EndElement || (_coreReader.NodeType == XmlNodeType.Element && _coreReader.IsEmptyElement));

            ValidatingReaderNodeData textNode = RecordTextNode(textValue, originalStringValue, _coreReader.Depth + 1, 0, 0);
            int endElementIndex = _contentIndex - 2;
            ValidatingReaderNodeData endElementNode = _contentEvents[endElementIndex];
            Debug.Assert(endElementNode.NodeType == XmlNodeType.EndElement);
            _contentEvents[endElementIndex] = textNode;
            _contentEvents[_contentIndex - 1] = endElementNode;
        }

        internal void RecordEndElementNode()
        {
            ValidatingReaderNodeData recordedNode = AddContent(XmlNodeType.EndElement);
            Debug.Assert(_coreReader.NodeType == XmlNodeType.EndElement || (_coreReader.NodeType == XmlNodeType.Element && _coreReader.IsEmptyElement));
            recordedNode.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
            recordedNode.SetLineInfo(_coreReader as IXmlLineInfo);
            if (_coreReader.IsEmptyElement)
            { //Simulated endElement node for <e/>, the coreReader is on cached Element node itself.
                _readAhead = true;
            }
        }

        internal string ReadOriginalContentAsString()
        {
            _returnOriginalStringValues = true;
            string strValue = InternalReadContentAsString();
            _returnOriginalStringValues = false;
            return strValue;
        }

        // Gets a value indicating whether XmlReader is positioned at the end of the stream.
        public override bool EOF
        {
            get
            {
                return _cacheState == CachingReaderState.ReaderClosed && _coreReader.EOF;
            }
        }

        // Closes the stream, changes the ReadState to Closed, and sets all the properties back to zero.
        public override void Close()
        {
            _coreReader.Close();
            _cacheState = CachingReaderState.ReaderClosed;
        }

        // Returns the read state of the stream.
        public override ReadState ReadState
        {
            get
            {
                return _coreReader.ReadState;
            }
        }

        // Skips to the end tag of the current element.
        public override void Skip()
        {
            //Skip on caching reader should move to the end of the subtree, past all cached events
            switch (_cachedNode.NodeType)
            {
                case XmlNodeType.Element:
                    if (_coreReader.NodeType != XmlNodeType.EndElement && !_readAhead)
                    { //will be true for IsDefault cases where we peek only one node ahead
                        int startDepth = _coreReader.Depth - 1;
                        while (_coreReader.Read() && _coreReader.Depth > startDepth)
                            ;
                    }
                    _coreReader.Read();
                    _cacheState = CachingReaderState.ReaderClosed;
                    _cacheHandler(this);
                    break;

                case XmlNodeType.Attribute:
                    MoveToElement();
                    goto case XmlNodeType.Element;

                default:
                    Debug.Assert(_cacheState == CachingReaderState.Replay);
                    Read();
                    break;
            }
        }

        // Gets the XmlNameTable associated with this implementation.
        public override XmlNameTable NameTable
        {
            get
            {
                return _coreReaderNameTable;
            }
        }

        // Resolves a namespace prefix in the current element's scope.
        public override string LookupNamespace(string prefix)
        {
            return _coreReader.LookupNamespace(prefix);
        }

        // Resolves the entity reference for nodes of NodeType EntityReference.
        public override void ResolveEntity()
        {
            throw new InvalidOperationException();
        }

        // Parses the attribute value into one or more Text and/or EntityReference node types.
        public override bool ReadAttributeValue()
        {
            Debug.Assert(_cacheState == CachingReaderState.Replay);
            if (_cachedNode.NodeType != XmlNodeType.Attribute)
            {
                return false;
            }
            _cachedNode = CreateDummyTextNode(_cachedNode.RawValue, _cachedNode.Depth + 1);
            return true;
        }

        //
        // IXmlLineInfo members
        //

        bool IXmlLineInfo.HasLineInfo()
        {
            return true;
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                return _cachedNode.LineNumber;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                return _cachedNode.LinePosition;
            }
        }

        //Private methods
        internal void SetToReplayMode()
        {
            _cacheState = CachingReaderState.Replay;
            _currentContentIndex = 0;
            _currentAttrIndex = -1;
            Read(); //Position on first node recorded to begin replaying
        }

        internal XmlReader GetCoreReader()
        {
            return _coreReader;
        }

        internal IXmlLineInfo GetLineInfo()
        {
            return _lineInfo;
        }

        private void ClearAttributesInfo()
        {
            _attributeCount = 0;
            _currentAttrIndex = -1;
        }

        private ValidatingReaderNodeData AddAttribute(int attIndex)
        {
            Debug.Assert(attIndex <= _attributeEvents.Length);
            ValidatingReaderNodeData attInfo = _attributeEvents[attIndex];
            if (attInfo != null)
            {
                attInfo.Clear(XmlNodeType.Attribute);
                return attInfo;
            }
            if (attIndex >= _attributeEvents.Length - 1)
            { //reached capacity of array, Need to increase capacity to twice the initial
                ValidatingReaderNodeData[] newAttributeEvents = new ValidatingReaderNodeData[_attributeEvents.Length * 2];
                Array.Copy(_attributeEvents, 0, newAttributeEvents, 0, _attributeEvents.Length);
                _attributeEvents = newAttributeEvents;
            }
            attInfo = _attributeEvents[attIndex];
            if (attInfo == null)
            {
                attInfo = new ValidatingReaderNodeData(XmlNodeType.Attribute);
                _attributeEvents[attIndex] = attInfo;
            }
            return attInfo;
        }

        private ValidatingReaderNodeData AddContent(XmlNodeType nodeType)
        {
            Debug.Assert(_contentIndex <= _contentEvents.Length);
            ValidatingReaderNodeData contentInfo = _contentEvents[_contentIndex];
            if (contentInfo != null)
            {
                contentInfo.Clear(nodeType);
                _contentIndex++;
                return contentInfo;
            }
            if (_contentIndex >= _contentEvents.Length - 1)
            { //reached capacity of array, Need to increase capacity to twice the initial
                ValidatingReaderNodeData[] newContentEvents = new ValidatingReaderNodeData[_contentEvents.Length * 2];
                Array.Copy(_contentEvents, 0, newContentEvents, 0, _contentEvents.Length);
                _contentEvents = newContentEvents;
            }
            contentInfo = _contentEvents[_contentIndex];
            if (contentInfo == null)
            {
                contentInfo = new ValidatingReaderNodeData(nodeType);
                _contentEvents[_contentIndex] = contentInfo;
            }
            _contentIndex++;
            return contentInfo;
        }

        private void RecordAttributes()
        {
            Debug.Assert(_coreReader.NodeType == XmlNodeType.Element);
            ValidatingReaderNodeData attInfo;
            _attributeCount = _coreReader.AttributeCount;
            if (_coreReader.MoveToFirstAttribute())
            {
                int attIndex = 0;
                do
                {
                    attInfo = AddAttribute(attIndex);
                    attInfo.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
                    attInfo.SetLineInfo(_lineInfo);
                    attInfo.RawValue = _coreReader.Value;
                    attIndex++;
                } while (_coreReader.MoveToNextAttribute());
                _coreReader.MoveToElement();
            }
        }

        private int GetAttributeIndexWithoutPrefix(string name)
        {
            name = _coreReaderNameTable.Get(name);
            if (name == null)
            {
                return -1;
            }
            ValidatingReaderNodeData attribute;
            for (int i = 0; i < _attributeCount; i++)
            {
                attribute = _attributeEvents[i];
                if (Ref.Equal(attribute.LocalName, name) && attribute.Prefix.Length == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetAttributeIndexWithPrefix(string name)
        {
            name = _coreReaderNameTable.Get(name);
            if (name == null)
            {
                return -1;
            }
            ValidatingReaderNodeData attribute;
            for (int i = 0; i < _attributeCount; i++)
            {
                attribute = _attributeEvents[i];
                if (Ref.Equal(attribute.GetAtomizedNameWPrefix(_coreReaderNameTable), name))
                {
                    return i;
                }
            }
            return -1;
        }

        private ValidatingReaderNodeData CreateDummyTextNode(string attributeValue, int depth)
        {
            if (_textNode == null)
            {
                _textNode = new ValidatingReaderNodeData(XmlNodeType.Text);
            }
            _textNode.Depth = depth;
            _textNode.RawValue = attributeValue;
            return _textNode;
        }
    }
}
