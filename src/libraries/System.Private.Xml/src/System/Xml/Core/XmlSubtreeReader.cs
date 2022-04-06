// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

namespace System.Xml
{
    internal sealed partial class XmlSubtreeReader : XmlWrappingReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        //
        // Private types
        //
        private sealed class NodeData
        {
            internal XmlNodeType type;
            internal string localName;
            internal string prefix;
            internal string name;
            internal string namespaceUri;
            internal string value;

            internal NodeData(XmlNodeType nodeType, string localName, string prefix, string name, string namespaceUri, string value)
            {
                this.type = nodeType;
                this.localName = localName;
                this.prefix = prefix;
                this.name = name;
                this.namespaceUri = namespaceUri;
                this.value = value;
            }

            internal void Set(XmlNodeType nodeType, string localName, string prefix, string name, string namespaceUri, string value)
            {
                this.type = nodeType;
                this.localName = localName;
                this.prefix = prefix;
                this.name = name;
                this.namespaceUri = namespaceUri;
                this.value = value;
            }
        }

        private enum State
        {
            Initial = ReadState.Initial,
            Interactive = ReadState.Interactive,
            Error = ReadState.Error,
            EndOfFile = ReadState.EndOfFile,
            Closed = ReadState.Closed,
            PopNamespaceScope,
            ClearNsAttributes,
            ReadElementContentAsBase64,
            ReadElementContentAsBinHex,
            ReadContentAsBase64,
            ReadContentAsBinHex,
        }

        private const int AttributeActiveStates = 0x62; // 00001100010 bin
        private const int NamespaceActiveStates = 0x7E2; // 11111100010 bin

        //
        // Fields
        //
        private readonly int _initialDepth;
        private State _state;

        // namespace management
        private readonly XmlNamespaceManager _nsManager;
        private NodeData[]? _nsAttributes;
        private int _nsAttrCount;
        private int _curNsAttr = -1;

        private readonly string _xmlns;
        private readonly string _xmlnsUri;

        // incremental reading of added xmlns nodes (ReadValueChunk, ReadContentAsBase64, ReadContentAsBinHex)
        private int _nsIncReadOffset;
        private IncrementalReadDecoder? _binDecoder;

        // cached nodes
        private bool _useCurNode;
        private NodeData _curNode = null!;
        // node used for a text node of ReadAttributeValue or as Initial or EOF node
        private readonly NodeData _tmpNode;

        //
        // Constants
        //
        internal int InitialNamespaceAttributeCount = 4;

        //
        // Constructor
        //
        internal XmlSubtreeReader(XmlReader reader) : base(reader)
        {
            _initialDepth = reader.Depth;
            _state = State.Initial;
            _nsManager = new XmlNamespaceManager(reader.NameTable);
            _xmlns = reader.NameTable.Add("xmlns");
            _xmlnsUri = reader.NameTable.Add(XmlReservedNs.NsXmlNs);

            _tmpNode = new NodeData(XmlNodeType.None, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            SetCurrentNode(_tmpNode);
        }

        //
        // XmlReader implementation
        //
        public override XmlNodeType NodeType
        {
            get
            {
                return _useCurNode ? _curNode.type : reader.NodeType;
            }
        }

        public override string Name
        {
            get
            {
                return _useCurNode ? _curNode.name : reader.Name;
            }
        }

        public override string LocalName
        {
            get
            {
                return _useCurNode ? _curNode.localName : reader.LocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return _useCurNode ? _curNode.namespaceUri : reader.NamespaceURI;
            }
        }

        public override string Prefix
        {
            get
            {
                return _useCurNode ? _curNode.prefix : reader.Prefix;
            }
        }

        public override string Value
        {
            get
            {
                return _useCurNode ? _curNode.value : reader.Value;
            }
        }

        public override int Depth
        {
            get
            {
                int depth = reader.Depth - _initialDepth;
                if (_curNsAttr != -1)
                {
                    if (_curNode.type == XmlNodeType.Text)
                    { // we are on namespace attribute value
                        depth += 2;
                    }
                    else
                    {
                        depth++;
                    }
                }
                return depth;
            }
        }

        public override string BaseURI
        {
            get
            {
                return reader.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return reader.IsEmptyElement;
            }
        }

        public override bool EOF
        {
            get
            {
                return _state == State.EndOfFile || _state == State.Closed;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                if (reader.ReadState == ReadState.Error)
                {
                    return ReadState.Error;
                }
                else
                {
                    if ((int)_state <= (int)State.Closed)
                    {
                        return (ReadState)(int)_state;
                    }
                    else
                    {
                        return ReadState.Interactive;
                    }
                }
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return reader.NameTable;
            }
        }

        public override int AttributeCount
        {
            get
            {
                return InAttributeActiveState ? reader.AttributeCount + _nsAttrCount : 0;
            }
        }

        public override string? GetAttribute(string name)
        {
            if (!InAttributeActiveState)
            {
                return null;
            }
            string? attr = reader.GetAttribute(name);
            if (attr != null)
            {
                return attr;
            }

            for (int i = 0; i < _nsAttrCount; i++)
            {
                if (name == _nsAttributes![i].name)
                {
                    return _nsAttributes[i].value;
                }
            }
            return null;
        }

        public override string? GetAttribute(string name, string? namespaceURI)
        {
            if (!InAttributeActiveState)
            {
                return null;
            }

            string? attr = reader.GetAttribute(name, namespaceURI);
            if (attr != null)
            {
                return attr;
            }

            for (int i = 0; i < _nsAttrCount; i++)
            {
                if (name == _nsAttributes![i].localName && namespaceURI == _xmlnsUri)
                {
                    return _nsAttributes[i].value;
                }
            }

            return null;
        }

        public override string GetAttribute(int i)
        {
            if (!InAttributeActiveState)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            int n = reader.AttributeCount;
            if (i < n)
            {
                return reader.GetAttribute(i);
            }
            else if (i - n < _nsAttrCount)
            {
                return _nsAttributes![i - n].value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
        }

        public override bool MoveToAttribute(string name)
        {
            if (!InAttributeActiveState)
            {
                return false;
            }
            if (reader.MoveToAttribute(name))
            {
                _curNsAttr = -1;
                _useCurNode = false;
                return true;
            }

            for (int i = 0; i < _nsAttrCount; i++)
            {
                if (name == _nsAttributes![i].name)
                {
                    MoveToNsAttribute(i);
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToAttribute(string name, string? ns)
        {
            if (!InAttributeActiveState)
            {
                return false;
            }
            if (reader.MoveToAttribute(name, ns))
            {
                _curNsAttr = -1;
                _useCurNode = false;
                return true;
            }

            for (int i = 0; i < _nsAttrCount; i++)
            {
                if (name == _nsAttributes![i].localName && ns == _xmlnsUri)
                {
                    MoveToNsAttribute(i);
                    return true;
                }
            }
            return false;
        }

        public override void MoveToAttribute(int i)
        {
            if (!InAttributeActiveState)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            int n = reader.AttributeCount;
            if (i < n)
            {
                reader.MoveToAttribute(i);
                _curNsAttr = -1;
                _useCurNode = false;
            }
            else if (i - n < _nsAttrCount)
            {
                MoveToNsAttribute(i - n);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
        }

        public override bool MoveToFirstAttribute()
        {
            if (!InAttributeActiveState)
            {
                return false;
            }
            if (reader.MoveToFirstAttribute())
            {
                _useCurNode = false;
                return true;
            }
            if (_nsAttrCount > 0)
            {
                MoveToNsAttribute(0);
                return true;
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            if (!InAttributeActiveState)
            {
                return false;
            }
            if (_curNsAttr == -1 && reader.MoveToNextAttribute())
            {
                return true;
            }
            if (_curNsAttr + 1 < _nsAttrCount)
            {
                MoveToNsAttribute(_curNsAttr + 1);
                return true;
            }
            return false;
        }

        public override bool MoveToElement()
        {
            if (!InAttributeActiveState)
            {
                return false;
            }

            _useCurNode = false;
            //If on Namespace attribute, the base reader is already on Element node.
            if (_curNsAttr >= 0)
            {
                _curNsAttr = -1;
                Debug.Assert(reader.NodeType == XmlNodeType.Element);
                return true;
            }
            else
            {
                return reader.MoveToElement();
            }
        }

        public override bool ReadAttributeValue()
        {
            if (!InAttributeActiveState)
            {
                return false;
            }
            if (_curNsAttr == -1)
            {
                return reader.ReadAttributeValue();
            }
            else if (_curNode.type == XmlNodeType.Text)
            { // we are on namespace attribute value
                return false;
            }
            else
            {
                Debug.Assert(_curNode.type == XmlNodeType.Attribute);
                _tmpNode.type = XmlNodeType.Text;
                _tmpNode.value = _curNode.value;
                SetCurrentNode(_tmpNode);
                return true;
            }
        }

        public override bool Read()
        {
            switch (_state)
            {
                case State.Initial:
                    _useCurNode = false;
                    _state = State.Interactive;
                    ProcessNamespaces();
                    return true;

                case State.Interactive:
                    _curNsAttr = -1;
                    _useCurNode = false;
                    reader.MoveToElement();
                    Debug.Assert(reader.Depth >= _initialDepth);
                    if (reader.Depth == _initialDepth)
                    {
                        if (reader.NodeType == XmlNodeType.EndElement ||
                            (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement))
                        {
                            _state = State.EndOfFile;
                            SetEmptyNode();
                            return false;
                        }
                        Debug.Assert(reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement);
                    }
                    if (reader.Read())
                    {
                        ProcessNamespaces();
                        return true;
                    }
                    else
                    {
                        SetEmptyNode();
                        return false;
                    }

                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return false;

                case State.PopNamespaceScope:
                    _nsManager.PopScope();
                    goto case State.ClearNsAttributes;

                case State.ClearNsAttributes:
                    _nsAttrCount = 0;
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    if (!FinishReadElementContentAsBinary())
                    {
                        return false;
                    }
                    return Read();

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    if (!FinishReadContentAsBinary())
                    {
                        return false;
                    }
                    return Read();

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return false;
            }
        }

        public override void Close()
        {
            if (_state == State.Closed)
            {
                return;
            }
            try
            {
                // move the underlying reader to the next sibling
                if (_state != State.EndOfFile)
                {
                    reader.MoveToElement();
                    Debug.Assert(reader.Depth >= _initialDepth);
                    // move off the root of the subtree
                    if (reader.Depth == _initialDepth && reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    // move to the end of the subtree, do nothing if on empty root element
                    while (reader.Depth > _initialDepth && reader.Read())
                    {
                        /* intentionally empty */
                    }
                }
            }
            catch
            { // never fail...
            }
            finally
            {
                _curNsAttr = -1;
                _useCurNode = false;
                _state = State.Closed;
                SetEmptyNode();
            }
        }

        public override void Skip()
        {
            switch (_state)
            {
                case State.Initial:
                    Read();
                    return;

                case State.Interactive:
                    _curNsAttr = -1;
                    _useCurNode = false;
                    reader.MoveToElement();
                    Debug.Assert(reader.Depth >= _initialDepth);
                    if (reader.Depth == _initialDepth)
                    {
                        if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                        {
                            // we are on root of the subtree -> skip to the end element and set to Eof state
                            if (reader.Read())
                            {
                                while (reader.NodeType != XmlNodeType.EndElement && reader.Depth > _initialDepth)
                                {
                                    reader.Skip();
                                }
                            }
                        }
                        Debug.Assert(reader.NodeType == XmlNodeType.EndElement ||
                                      reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement ||
                                      reader.ReadState != ReadState.Interactive);
                        _state = State.EndOfFile;
                        SetEmptyNode();
                        return;
                    }

                    if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                    {
                        _nsManager.PopScope();
                    }
                    reader.Skip();
                    ProcessNamespaces();

                    Debug.Assert(reader.Depth >= _initialDepth);
                    return;

                case State.Closed:
                case State.EndOfFile:
                    return;

                case State.PopNamespaceScope:
                    _nsManager.PopScope();
                    goto case State.ClearNsAttributes;

                case State.ClearNsAttributes:
                    _nsAttrCount = 0;
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    if (FinishReadElementContentAsBinary())
                    {
                        Skip();
                    }
                    break;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    if (FinishReadContentAsBinary())
                    {
                        Skip();
                    }
                    break;

                case State.Error:
                    return;

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return;
            }
        }

        public override object ReadContentAsObject()
        {
            try
            {
                InitReadContentAsType("ReadContentAsObject");
                object value = reader.ReadContentAsObject();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override bool ReadContentAsBoolean()
        {
            try
            {
                InitReadContentAsType("ReadContentAsBoolean");
                bool value = reader.ReadContentAsBoolean();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override DateTime ReadContentAsDateTime()
        {
            try
            {
                InitReadContentAsType("ReadContentAsDateTime");
                DateTime value = reader.ReadContentAsDateTime();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override double ReadContentAsDouble()
        {
            try
            {
                InitReadContentAsType("ReadContentAsDouble");
                double value = reader.ReadContentAsDouble();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override float ReadContentAsFloat()
        {
            try
            {
                InitReadContentAsType("ReadContentAsFloat");
                float value = reader.ReadContentAsFloat();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override decimal ReadContentAsDecimal()
        {
            try
            {
                InitReadContentAsType("ReadContentAsDecimal");
                decimal value = reader.ReadContentAsDecimal();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override int ReadContentAsInt()
        {
            try
            {
                InitReadContentAsType("ReadContentAsInt");
                int value = reader.ReadContentAsInt();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override long ReadContentAsLong()
        {
            try
            {
                InitReadContentAsType("ReadContentAsLong");
                long value = reader.ReadContentAsLong();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override string ReadContentAsString()
        {
            try
            {
                InitReadContentAsType("ReadContentAsString");
                string value = reader.ReadContentAsString();
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver)
        {
            try
            {
                InitReadContentAsType("ReadContentAs");
                object value = reader.ReadContentAs(returnType, namespaceResolver);
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return reader.CanReadBinaryContent;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    switch (NodeType)
                    {
                        case XmlNodeType.Element:
                            throw CreateReadContentAsException(nameof(ReadContentAsBase64));
                        case XmlNodeType.EndElement:
                            return 0;
                        case XmlNodeType.Attribute:
                            if (_curNsAttr != -1 && reader.CanReadBinaryContent)
                            {
                                CheckBuffer(buffer, index, count);
                                if (count == 0)
                                {
                                    return 0;
                                }
                                if (_nsIncReadOffset == 0)
                                {
                                    // called first time on this ns attribute
                                    if (_binDecoder != null && _binDecoder is Base64Decoder)
                                    {
                                        _binDecoder.Reset();
                                    }
                                    else
                                    {
                                        _binDecoder = new Base64Decoder();
                                    }
                                }
                                if (_nsIncReadOffset == _curNode.value.Length)
                                {
                                    return 0;
                                }

                                Debug.Assert(_binDecoder != null);
                                _binDecoder.SetNextOutputBuffer(buffer, index, count);
                                _nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
                                return _binDecoder.DecodedCount;
                            }
                            goto case XmlNodeType.Text;
                        case XmlNodeType.Text:
                            Debug.Assert(AttributeCount > 0);
                            return reader.ReadContentAsBase64(buffer, index, count);
                        default:
                            Debug.Fail($"Unexpected state {_state}");
                            return 0;
                    }

                case State.Interactive:
                    _state = State.ReadContentAsBase64;
                    goto case State.ReadContentAsBase64;

                case State.ReadContentAsBase64:
                    int read = reader.ReadContentAsBase64(buffer, index, count);
                    if (read == 0)
                    {
                        _state = State.Interactive;
                        ProcessNamespaces();
                    }
                    return read;

                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.Interactive:
                case State.PopNamespaceScope:
                case State.ClearNsAttributes:
                    if (!InitReadElementContentAsBinary(State.ReadElementContentAsBase64))
                    {
                        return 0;
                    }
                    goto case State.ReadElementContentAsBase64;

                case State.ReadElementContentAsBase64:
                    int read = reader.ReadContentAsBase64(buffer, index, count);
                    if (read > 0 || count == 0)
                    {
                        return read;
                    }
                    if (NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                    }

                    // pop namespace scope
                    _state = State.Interactive;
                    ProcessNamespaces();

                    // set eof state or move off the end element
                    if (reader.Depth == _initialDepth)
                    {
                        _state = State.EndOfFile;
                        SetEmptyNode();
                    }
                    else
                    {
                        Read();
                    }
                    return 0;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    switch (NodeType)
                    {
                        case XmlNodeType.Element:
                            throw CreateReadContentAsException(nameof(ReadContentAsBinHex));
                        case XmlNodeType.EndElement:
                            return 0;
                        case XmlNodeType.Attribute:
                            if (_curNsAttr != -1 && reader.CanReadBinaryContent)
                            {
                                CheckBuffer(buffer, index, count);
                                if (count == 0)
                                {
                                    return 0;
                                }
                                if (_nsIncReadOffset == 0)
                                {
                                    // called first time on this ns attribute
                                    if (_binDecoder != null && _binDecoder is BinHexDecoder)
                                    {
                                        _binDecoder.Reset();
                                    }
                                    else
                                    {
                                        _binDecoder = new BinHexDecoder();
                                    }
                                }
                                if (_nsIncReadOffset == _curNode.value.Length)
                                {
                                    return 0;
                                }

                                Debug.Assert(_binDecoder != null);
                                _binDecoder.SetNextOutputBuffer(buffer, index, count);
                                _nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
                                return _binDecoder.DecodedCount;
                            }
                            goto case XmlNodeType.Text;
                        case XmlNodeType.Text:
                            Debug.Assert(AttributeCount > 0);
                            return reader.ReadContentAsBinHex(buffer, index, count);
                        default:
                            Debug.Fail($"Unexpected state {_state}");
                            return 0;
                    }

                case State.Interactive:
                    _state = State.ReadContentAsBinHex;
                    goto case State.ReadContentAsBinHex;

                case State.ReadContentAsBinHex:
                    int read = reader.ReadContentAsBinHex(buffer, index, count);
                    if (read == 0)
                    {
                        _state = State.Interactive;
                        ProcessNamespaces();
                    }
                    return read;

                case State.ReadContentAsBase64:
                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.Interactive:
                case State.PopNamespaceScope:
                case State.ClearNsAttributes:
                    if (!InitReadElementContentAsBinary(State.ReadElementContentAsBinHex))
                    {
                        return 0;
                    }
                    goto case State.ReadElementContentAsBinHex;
                case State.ReadElementContentAsBinHex:
                    int read = reader.ReadContentAsBinHex(buffer, index, count);
                    if (read > 0 || count == 0)
                    {
                        return read;
                    }
                    if (NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                    }

                    // pop namespace scope
                    _state = State.Interactive;
                    ProcessNamespaces();

                    // set eof state or move off the end element
                    if (reader.Depth == _initialDepth)
                    {
                        _state = State.EndOfFile;
                        SetEmptyNode();
                    }
                    else
                    {
                        Read();
                    }
                    return 0;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBase64:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override bool CanReadValueChunk
        {
            get
            {
                return reader.CanReadValueChunk;
            }
        }

        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    // ReadValueChunk implementation on added xmlns attributes
                    if (_curNsAttr != -1 && reader.CanReadValueChunk)
                    {
                        CheckBuffer(buffer, index, count);
                        int copyCount = _curNode.value.Length - _nsIncReadOffset;
                        if (copyCount > count)
                        {
                            copyCount = count;
                        }
                        if (copyCount > 0)
                        {
                            _curNode.value.CopyTo(_nsIncReadOffset, buffer, index, copyCount);
                        }
                        _nsIncReadOffset += copyCount;
                        return copyCount;
                    }
                    // Otherwise fall back to the case State.Interactive.
                    // No need to clean ns attributes or pop scope because the reader when ReadValueChunk is called
                    // - on Element errors
                    // - on EndElement errors
                    // - on Attribute does not move
                    // and that's all where State.ClearNsAttributes or State.PopnamespaceScope can be set
                    goto case State.Interactive;

                case State.Interactive:
                    return reader.ReadValueChunk(buffer, index, count);

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingReadValueChunkWithBinary);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override string? LookupNamespace(string prefix)
        {
            return ((IXmlNamespaceResolver)this).LookupNamespace(prefix);
        }

        //
        // IDisposable interface
        //
        protected override void Dispose(bool disposing)
        {
            // note: we do not want to dispose the underlying reader
            this.Close();
        }

        //
        // IXmlLineInfo implementation
        //
        int IXmlLineInfo.LineNumber
        {
            get
            {
                if (!_useCurNode)
                {
                    if (reader is IXmlLineInfo lineInfo)
                    {
                        return lineInfo.LineNumber;
                    }
                }
                return 0;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                if (!_useCurNode)
                {
                    if (reader is IXmlLineInfo lineInfo)
                    {
                        return lineInfo.LinePosition;
                    }
                }
                return 0;
            }
        }

        bool IXmlLineInfo.HasLineInfo()
        {
            return reader is IXmlLineInfo;
        }

        //
        // IXmlNamespaceResolver implementation
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            if (!InNamespaceActiveState)
            {
                return new Dictionary<string, string>();
            }
            return _nsManager.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            if (!InNamespaceActiveState)
            {
                return null;
            }
            return _nsManager.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            if (!InNamespaceActiveState)
            {
                return null;
            }
            return _nsManager.LookupPrefix(namespaceName);
        }

        //
        // Private methods
        //
        private void ProcessNamespaces()
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    _nsManager.PushScope();

                    string prefix = reader.Prefix;
                    string ns = reader.NamespaceURI;
                    if (_nsManager.LookupNamespace(prefix) != ns)
                    {
                        AddNamespace(prefix, ns);
                    }

                    if (reader.MoveToFirstAttribute())
                    {
                        do
                        {
                            prefix = reader.Prefix;
                            ns = reader.NamespaceURI;

                            if (Ref.Equal(ns, _xmlnsUri))
                            {
                                if (prefix.Length == 0)
                                {
                                    _nsManager.AddNamespace(string.Empty, reader.Value);
                                    RemoveNamespace(string.Empty, _xmlns);
                                }
                                else
                                {
                                    prefix = reader.LocalName;
                                    _nsManager.AddNamespace(prefix, reader.Value);
                                    RemoveNamespace(_xmlns, prefix);
                                }
                            }
                            else if (prefix.Length != 0 && _nsManager.LookupNamespace(prefix) != ns)
                            {
                                AddNamespace(prefix, ns);
                            }
                        } while (reader.MoveToNextAttribute());
                        reader.MoveToElement();
                    }

                    if (reader.IsEmptyElement)
                    {
                        _state = State.PopNamespaceScope;
                    }
                    break;
                case XmlNodeType.EndElement:
                    _state = State.PopNamespaceScope;
                    break;
            }
        }

        private void AddNamespace(string prefix, string ns)
        {
            _nsManager.AddNamespace(prefix, ns);

            int index = _nsAttrCount++;
            if (_nsAttributes == null)
            {
                _nsAttributes = new NodeData[InitialNamespaceAttributeCount];
            }

            if (index == _nsAttributes.Length)
            {
                NodeData[] newNsAttrs = new NodeData[_nsAttributes.Length * 2];
                Array.Copy(_nsAttributes, newNsAttrs, index);
                _nsAttributes = newNsAttrs;
            }

            string localName;
            string attrPrefix;
            string name;

            if (prefix.Length == 0)
            {
                localName = _xmlns;
                attrPrefix = string.Empty;
                name = _xmlns;
            }
            else
            {
                localName = prefix;
                attrPrefix = _xmlns;
                name = reader.NameTable.Add($"{_xmlns}:{prefix}");
            }

            if (_nsAttributes[index] == null)
            {
                _nsAttributes[index] = new NodeData(XmlNodeType.Attribute, localName, attrPrefix, name, _xmlnsUri, ns);
            }
            else
            {
                _nsAttributes[index].Set(XmlNodeType.Attribute, localName, attrPrefix, name, _xmlnsUri, ns);
            }

            Debug.Assert(_state == State.ClearNsAttributes || _state == State.Interactive || _state == State.PopNamespaceScope);
            _state = State.ClearNsAttributes;

            _curNsAttr = -1;
        }

        private void RemoveNamespace(string prefix, string localName)
        {
            for (int i = 0; i < _nsAttrCount; i++)
            {
                if (Ref.Equal(prefix, _nsAttributes![i].prefix) &&
                    Ref.Equal(localName, _nsAttributes[i].localName))
                {
                    if (i < _nsAttrCount - 1)
                    {
                        // swap
                        NodeData tmpNodeData = _nsAttributes[i];
                        _nsAttributes[i] = _nsAttributes[_nsAttrCount - 1];
                        _nsAttributes[_nsAttrCount - 1] = tmpNodeData;
                    }
                    _nsAttrCount--;
                    break;
                }
            }
        }

        private void MoveToNsAttribute(int index)
        {
            Debug.Assert(index >= 0 && index <= _nsAttrCount);
            reader.MoveToElement();
            _curNsAttr = index;
            _nsIncReadOffset = 0;
            SetCurrentNode(_nsAttributes![index]);
        }

        private bool InitReadElementContentAsBinary(State binaryState)
        {
            if (NodeType != XmlNodeType.Element)
            {
                throw reader.CreateReadElementContentAsException(nameof(ReadElementContentAsBase64));
            }

            bool isEmpty = IsEmptyElement;

            // move to content or off the empty element
            if (!Read() || isEmpty)
            {
                return false;
            }
            // special-case child element and end element
            switch (NodeType)
            {
                case XmlNodeType.Element:
                    throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                case XmlNodeType.EndElement:
                    // pop scope & move off end element
                    ProcessNamespaces();
                    Read();
                    return false;
            }

            Debug.Assert(_state == State.Interactive);
            _state = binaryState;
            return true;
        }

        private bool FinishReadElementContentAsBinary()
        {
            Debug.Assert(_state == State.ReadElementContentAsBase64 || _state == State.ReadElementContentAsBinHex);

            byte[] bytes = new byte[256];
            if (_state == State.ReadElementContentAsBase64)
            {
                while (reader.ReadContentAsBase64(bytes, 0, 256) > 0) ;
            }
            else
            {
                while (reader.ReadContentAsBinHex(bytes, 0, 256) > 0) ;
            }

            if (NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
            }

            // pop namespace scope
            _state = State.Interactive;
            ProcessNamespaces();

            // check eof
            if (reader.Depth == _initialDepth)
            {
                _state = State.EndOfFile;
                SetEmptyNode();
                return false;
            }
            // move off end element
            return Read();
        }

        private bool FinishReadContentAsBinary()
        {
            Debug.Assert(_state == State.ReadContentAsBase64 || _state == State.ReadContentAsBinHex);

            byte[] bytes = new byte[256];
            if (_state == State.ReadContentAsBase64)
            {
                while (reader.ReadContentAsBase64(bytes, 0, 256) > 0) ;
            }
            else
            {
                while (reader.ReadContentAsBinHex(bytes, 0, 256) > 0) ;
            }

            _state = State.Interactive;
            ProcessNamespaces();

            // check eof
            if (reader.Depth == _initialDepth)
            {
                _state = State.EndOfFile;
                SetEmptyNode();
                return false;
            }
            return true;
        }

        private bool InAttributeActiveState
        {
            get
            {
#if DEBUG
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.Initial)));
                Debug.Assert(0 != (AttributeActiveStates & (1 << (int)State.Interactive)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.Error)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.EndOfFile)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.Closed)));
                Debug.Assert(0 != (AttributeActiveStates & (1 << (int)State.PopNamespaceScope)));
                Debug.Assert(0 != (AttributeActiveStates & (1 << (int)State.ClearNsAttributes)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.ReadElementContentAsBase64)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.ReadElementContentAsBinHex)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.ReadContentAsBase64)));
                Debug.Assert(0 == (AttributeActiveStates & (1 << (int)State.ReadContentAsBinHex)));
#endif
                return 0 != (AttributeActiveStates & (1 << (int)_state));
            }
        }

        private bool InNamespaceActiveState
        {
            get
            {
#if DEBUG
                Debug.Assert(0 == (NamespaceActiveStates & (1 << (int)State.Initial)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.Interactive)));
                Debug.Assert(0 == (NamespaceActiveStates & (1 << (int)State.Error)));
                Debug.Assert(0 == (NamespaceActiveStates & (1 << (int)State.EndOfFile)));
                Debug.Assert(0 == (NamespaceActiveStates & (1 << (int)State.Closed)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.PopNamespaceScope)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.ClearNsAttributes)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.ReadElementContentAsBase64)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.ReadElementContentAsBinHex)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.ReadContentAsBase64)));
                Debug.Assert(0 != (NamespaceActiveStates & (1 << (int)State.ReadContentAsBinHex)));
#endif
                return 0 != (NamespaceActiveStates & (1 << (int)_state));
            }
        }

        private void SetEmptyNode()
        {
            Debug.Assert(_tmpNode.localName == string.Empty && _tmpNode.prefix == string.Empty && _tmpNode.name == string.Empty && _tmpNode.namespaceUri == string.Empty);
            _tmpNode.type = XmlNodeType.None;
            _tmpNode.value = string.Empty;

            _curNode = _tmpNode;
            _useCurNode = true;
        }

        private void SetCurrentNode(NodeData node)
        {
            _curNode = node;
            _useCurNode = true;
        }

        private void InitReadContentAsType(string methodName)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    throw new InvalidOperationException(SR.Xml_ClosedOrErrorReader);

                case State.Interactive:
                    return;

                case State.PopNamespaceScope:
                case State.ClearNsAttributes:
                    // no need to clean ns attributes or pop scope because the reader when ReadContentAs is called
                    // - on Element errors
                    // - on Attribute does not move
                    // - on EndElement does not move
                    // and that's all where State.ClearNsAttributes or State.PopNamespacScope can be set
                    return;

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingReadValueChunkWithBinary);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    break;
            }
            throw CreateReadContentAsException(methodName);
        }

        private void FinishReadContentAsType()
        {
            Debug.Assert(_state == State.Interactive ||
                          _state == State.PopNamespaceScope ||
                          _state == State.ClearNsAttributes);

            switch (NodeType)
            {
                case XmlNodeType.Element:
                    // new element we moved to - process namespaces
                    ProcessNamespaces();
                    break;
                case XmlNodeType.EndElement:
                    // end element we've stayed on or have been moved to
                    _state = State.PopNamespaceScope;
                    break;
                case XmlNodeType.Attribute:
                    // stayed on attribute, do nothing
                    break;
            }
        }

        private static void CheckBuffer(Array buffer!!, int index, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
    }
}
