// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal interface IPrefixGenerator
    {
        string GetPrefix(string namespaceUri, int depth, bool isForAttribute);
    }

    internal enum XmlDocumentPosition
    {
        BeforeRootElement,
        InRootElement,
        AfterRootElement
    }

    internal sealed class CanonicalWriter : XmlDictionaryWriter
    {
        private const int InitialElementStackSize = 10;
        private readonly CanonicalAttributeManager _attributesToRender;
        private CanonicalEncoder _encoder;
        private bool _includeComments;
        private string[] _inclusivePrefixes;
        private IPrefixGenerator _prefixGenerator;
        private ExclusiveCanonicalNamespaceManager _manager;
        private int _startingDepthForAttributePrefixGeneration;
        private WriteState _state;
        private XmlDocumentPosition _docPos;
        private ElementEntry[] _elements;
        private int _elementCount;
        private int _depth;
        private int _attributePrefixGenerationIndex;
        private bool _bufferOnlyRootElementContents;
        private string _currentAttributeName;
        private string _currentAttributePrefix;
        private string _currentAttributeNamespace;
        private string _currentAttributeValue;
        private XmlAttributeHolder[] _rootElementAttributes;
        private string _rootElementPrefix;
        private string _pendingBaseDeclarationPrefix;
        private string _pendingBaseDeclarationNamespace;
        private byte[] _base64Remainder;
        private int _base64RemainderSize;
        private char[] _conversionBuffer;
        private const int ConversionBufferSize = 4 * 128;
        private const int Base64ByteBufferSize = 3 * 128;
        private XmlDictionaryWriter _bufferingWriter;
        private char[] _auxBuffer;
        private ICanonicalWriterEndRootElementCallback _endOfRootElementCallback;
        private IAncestralNamespaceContextProvider _contextProvider;

        public CanonicalWriter(CanonicalEncoder encoder)
            : this(encoder, null, false, null, 0)
        {
        }

        public CanonicalWriter(CanonicalEncoder encoder,
            string[] inclusivePrefixes, bool includeComments, IPrefixGenerator prefixGenerator, int startingDepthForAttributePrefixGeneration)
        {
            _attributesToRender = new CanonicalAttributeManager();
            _encoder = encoder;
            _includeComments = includeComments;
            _prefixGenerator = prefixGenerator;
            _startingDepthForAttributePrefixGeneration = startingDepthForAttributePrefixGeneration;
            _manager = new ExclusiveCanonicalNamespaceManager();
            _elements = new ElementEntry[InitialElementStackSize];
            _inclusivePrefixes = inclusivePrefixes;

            Reset();
        }

        public XmlDictionaryWriter BufferingWriter
        {
            get { return _bufferingWriter; }
            set
            {
                ThrowIfNotInStartState();
                _bufferingWriter = value; // allow null
            }
        }

        public bool BufferOnlyRootElementContents
        {
            get { return _bufferOnlyRootElementContents; }
            set
            {
                ThrowIfNotInStartState();
                _bufferOnlyRootElementContents = value;
            }
        }

        public IAncestralNamespaceContextProvider ContextProvider
        {
            get { return _contextProvider; }
            set
            {
                ThrowIfNotInStartState();
                _contextProvider = value;
            }
        }

        public CanonicalEncoder Encoder
        {
            get { return _encoder; }
            set
            {
                ThrowIfNotInStartState();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _encoder = value;
            }
        }

        internal ICanonicalWriterEndRootElementCallback EndRootElementCallback
        {
            get { return _endOfRootElementCallback; }
            set
            {
                ThrowIfNotInStartState();
                _endOfRootElementCallback = value; // allow null
            }
        }

        public bool IncludeComments
        {
            get { return _includeComments; }
            set
            {
                ThrowIfNotInStartState();
                _includeComments = value;
            }
        }

        public IPrefixGenerator PrefixGenerator
        {
            get { return _prefixGenerator; }
            set
            {
                ThrowIfNotInStartState();
                _prefixGenerator = value;
            }
        }

        internal XmlAttributeHolder[] RootElementAttributes
        {
            get { return _rootElementAttributes; }
        }

        public string RootElementPrefix
        {
            get { return _rootElementPrefix; }
        }

        private bool ShouldDelegate
        {
            get { return _bufferingWriter != null && (!_bufferOnlyRootElementContents || _depth > 0); }
        }

        public int StartingDepthForAttributePrefixGeneration
        {
            get { return _startingDepthForAttributePrefixGeneration; }
            set
            {
                ThrowIfNotInStartState();
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be non-negative.");
                }

                _startingDepthForAttributePrefixGeneration = value;
            }
        }

        public override WriteState WriteState
        {
            get { return _state; }
        }

        private string AutoGeneratePrefix(string ns, bool isForAttribute)
        {
            string prefix = null;
            if (_prefixGenerator != null)
            {
                prefix = _prefixGenerator.GetPrefix(ns, _depth + _startingDepthForAttributePrefixGeneration, isForAttribute);
            }

            if (prefix != null && LookupNamespace(prefix) == null)
            {
                return prefix;
            }

            if (!isForAttribute)
            {
                return string.Empty;
            }

            do
            {
                prefix = "d" + (_depth + _startingDepthForAttributePrefixGeneration) + "p" + ++_attributePrefixGenerationIndex;
            }
            while (LookupNamespace(prefix) != null);
            return prefix;
        }

        public override void Close()
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.Close();
            }
        }

        public override void Flush()
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.Flush();
            }
        }

        public void FlushWriterAndEncoder()
        {
            Flush();
            _encoder.Flush();
        }

        public string[] GetInclusivePrefixes()
        {
            return _inclusivePrefixes;
        }

        private string LookupNamespace(string prefix)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }

            string ns = _manager.LookupNamespace(prefix);
            if (ns == null && prefix.Length == 0)
            {
                ns = string.Empty;
            }

            return ns;
        }

        public override string LookupPrefix(string ns)
        {
            return _manager.LookupPrefix(ns, false);
        }

        private void OnEndElement()
        {
            if (_state != WriteState.Content && _state != WriteState.Element)
            {
                ThrowBadStateException("EndElement");
            }

            OnPossibleEndOfStartTag(WriteState.Content);
            _encoder.EncodeEndElement(_elements[_elementCount - 1].prefix, _elements[_elementCount - 1].localName);
            PopElement();
            _manager.ExitElementContext();
            _depth--;
            if (_depth == 0)
            {
                _docPos = XmlDocumentPosition.AfterRootElement;
                if (EndRootElementCallback != null)
                {
                    EndRootElementCallback.OnEndOfRootElement(this);
                }
            }
        }

        private void OnPossibleEndOfBase64Content()
        {
            if (_base64RemainderSize > 0)
            {
                WriteBase64Core(_base64Remainder, 0, _base64RemainderSize);
                _base64RemainderSize = 0;
            }
        }

        private void OnPossibleEndOfStartTag(WriteState newState)
        {
            if (_state == WriteState.Element)
            {
                OnEndStartElement();
                _depth++;
            }
            _state = newState;
        }

        private void OnEndStartElement()
        {
            if (_inclusivePrefixes != null)
            {
                for (int i = 0; i < _inclusivePrefixes.Length; i++)
                {
                    _manager.MarkToRenderForInclusivePrefix(_inclusivePrefixes[i], _depth == 0, _contextProvider);
                }
            }

            _attributesToRender.Sort();

            _encoder.EncodeStartElementOpen(_elements[_elementCount - 1].prefix, _elements[_elementCount - 1].localName);
            _manager.Render(_encoder);
            _attributesToRender.Encode(_encoder);
            _encoder.EncodeStartElementClose();

            if (BufferOnlyRootElementContents && _elementCount == 1)
            {
                _rootElementAttributes = _attributesToRender.Copy();
                _rootElementPrefix = _elements[0].prefix;
            }

            _attributesToRender.Clear();
        }

        private void PopElement()
        {
            _elements[--_elementCount].Clear();
        }

        private void PushElement(string prefix, string localName)
        {
            if (_elementCount == _elements.Length)
            {
                ElementEntry[] newElements = new ElementEntry[_elements.Length * 2];
                Array.Copy(_elements, 0, newElements, 0, _elementCount);
                _elements = newElements;
            }
            _elements[_elementCount++].Set(prefix, localName);
        }

        public void Reset()
        {
            _state = WriteState.Start;
            _attributesToRender.Clear();
            if (_encoder != null)
            {
                _encoder.Reset();
            }

            _manager.Reset();
            for (int i = _elementCount - 1; i >= 0; i--)
            {
                _elements[i].Clear();
            }

            _elementCount = 0;
            _depth = 0;
            _docPos = XmlDocumentPosition.BeforeRootElement;
            _attributePrefixGenerationIndex = 0;
            _rootElementAttributes = null;
            _rootElementPrefix = null;
            _base64RemainderSize = 0;
            _pendingBaseDeclarationPrefix = null;
            _pendingBaseDeclarationNamespace = null;
        }

        public void RestoreDefaultSettings()
        {
            BufferingWriter = null;
            BufferOnlyRootElementContents = false;
            ContextProvider = null;
            PrefixGenerator = null;
            StartingDepthForAttributePrefixGeneration = 0;
        }

        public void SetInclusivePrefixes(string[] inclusivePrefixes)
        {
            ThrowIfNotInStartState();
            _inclusivePrefixes = inclusivePrefixes;
        }

        private void ThrowBadStateException(string call)
        {
            throw new InvalidOperationException(string.Format("Invalid Operation for writer state: {0}, {1}", call, _state));
        }

        private void ThrowIfNotInStartState()
        {
            if (_state != WriteState.Start)
            {
                throw new InvalidOperationException("Setting may be modified only when the writer is in Start State.");
            }
        }

        public override void WriteBase64(byte[] buffer, int offset, int count)
        {
            CanonicalEncoder.ValidateBufferBounds(buffer, offset, count);
            int originalOffset = offset;
            int originalCount = count;

            if (_state == WriteState.Element)
            {
                OnPossibleEndOfStartTag(WriteState.Content);
            }

            // complete previous left overs
            if (_base64RemainderSize > 0)
            {
                int nBytes = Math.Min(3 - _base64RemainderSize, count);
                Buffer.BlockCopy(buffer, offset, _base64Remainder, _base64RemainderSize, nBytes);
                _base64RemainderSize += nBytes;
                offset += nBytes;
                count -= nBytes;
                if (_base64RemainderSize == 3)
                {
                    WriteBase64Core(_base64Remainder, 0, 3);
                    _base64RemainderSize = 0;
                }
            }

            if (count > 0)
            {
                // save new left over
                _base64RemainderSize = count % 3;
                if (_base64RemainderSize > 0)
                {
                    if (_base64Remainder == null)
                    {
                        _base64Remainder = new byte[3];
                    }

                    Buffer.BlockCopy(buffer, offset + count - _base64RemainderSize, _base64Remainder, 0, _base64RemainderSize);
                    count -= _base64RemainderSize;
                }

                // write the middle
                WriteBase64Core(buffer, offset, count);
            }
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteBase64(buffer, originalOffset, originalCount);
            }
        }

        private void WriteBase64Core(byte[] buffer, int offset, int count)
        {
            if (_conversionBuffer == null)
            {
                _conversionBuffer = new char[ConversionBufferSize];
            }

            while (count > 0)
            {
                int nBytes = Math.Min(count, Base64ByteBufferSize);
                int nChars = Convert.ToBase64CharArray(buffer, offset, nBytes, _conversionBuffer, 0);
                WriteStringCore(_conversionBuffer, 0, nChars, true, true);
                offset += nBytes;
                count -= nBytes;
            }
        }

        public override void WriteCData(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            OnPossibleEndOfBase64Content();

            if (_state == WriteState.Element)
            {
                OnPossibleEndOfStartTag(WriteState.Content);
            }
            else if (_state != WriteState.Content)
            {
                ThrowBadStateException("WriteCData");
            }

            _encoder.EncodeWithTranslation(s, CanonicalEncoder.XmlStringType.CDataContent);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteCData(s);
            }
        }

        public override void WriteCharEntity(char ch)
        {
            switch (ch)
            {
                case '<':
                    WriteRaw("&lt;");
                    break;
                case '>':
                    WriteRaw("&gt;");
                    break;
                case '&':
                    WriteRaw("&amp;");
                    break;
                default:
                    if (_auxBuffer == null)
                    {
                        _auxBuffer = new char[1];
                    }

                    _auxBuffer[0] = ch;
                    WriteStringCore(_auxBuffer, 0, 1, false, false);
                    if (ShouldDelegate)
                    {
                        _bufferingWriter.WriteCharEntity(ch);
                    }

                    break;
            }
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteStringCore(buffer, index, count, false, false);
            if (ShouldDelegate)
            {
                CanonicalEncoder.WriteEscapedChars(_bufferingWriter, buffer, index, count);
            }
        }

        public override void WriteComment(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (_state == WriteState.Element)
            {
                OnPossibleEndOfStartTag(WriteState.Content);
            }
            else if (_state == WriteState.Attribute)
            {
                ThrowBadStateException("WriteComment");
            }

            if (_includeComments)
            {
                _encoder.EncodeComment(text, _docPos);
            }

            if (ShouldDelegate)
            {
                _bufferingWriter.WriteComment(text);
            }
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteDocType(name, pubid, sysid, subset);
            }
        }

        public override void WriteEndAttribute()
        {
            if (_state != WriteState.Attribute)
            {
                ThrowBadStateException("WriteEndAttribute");
            }

            OnPossibleEndOfBase64Content();
            if (_currentAttributePrefix == "xmlns")
            {
                _manager.AddLocalNamespaceIfNotRedundant(_currentAttributeName, _currentAttributeValue);
            }
            else
            {
                _attributesToRender.Add(_currentAttributePrefix, _currentAttributeName,
                    _currentAttributeNamespace, _currentAttributeValue);
                if (_currentAttributePrefix.Length > 0)
                {
                    _manager.AddLocalNamespaceIfNotRedundant(_currentAttributePrefix, _currentAttributeNamespace);
                    _manager.MarkToRenderForVisiblyUsedPrefix(_currentAttributePrefix, true, _contextProvider);
                }
            }

            _currentAttributeName = null;
            _currentAttributePrefix = null;
            _currentAttributeNamespace = null;
            _currentAttributeValue = null;
            _state = WriteState.Element;
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteEndAttribute();
                if (_pendingBaseDeclarationNamespace != null)
                {
                    _bufferingWriter.WriteStartAttribute("xmlns", _pendingBaseDeclarationPrefix, null);
                    _bufferingWriter.WriteString(_pendingBaseDeclarationNamespace);
                    _bufferingWriter.WriteEndAttribute();
                    _pendingBaseDeclarationNamespace = null;
                    _pendingBaseDeclarationPrefix = null;
                }
            }
        }

        public override void WriteEndDocument()
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteEndDocument();
            }
        }

        public override void WriteEndElement()
        {
            OnPossibleEndOfBase64Content();
            OnEndElement();
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteEndElement();
            }
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException();
        }

        public override void WriteFullEndElement()
        {
            OnPossibleEndOfBase64Content();
            OnEndElement();
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteFullEndElement();
            }
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_state == WriteState.Attribute)
            {
                ThrowBadStateException("WriteProcessingInstruction");
            }

            OnPossibleEndOfBase64Content();
            OnPossibleEndOfStartTag(WriteState.Content);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteProcessingInstruction(name, text);
            }
        }

        public override void WriteQualifiedName(string localName, string ns)
        {
            if (localName == null || localName.Length == 0)
            {
                throw new ArgumentNullException(nameof(localName));
            }

            if (ns == null)
            {
                ns = string.Empty;
            }

            string prefix = _manager.LookupPrefix(ns, false);
            if (ns.Length == 0)
            {
                if (prefix == null)
                {
                    prefix = string.Empty;
                }

                else if (prefix.Length > 0)
                {
                    if (_state != WriteState.Attribute || LookupNamespace(string.Empty) != null)
                    {
                        throw new InvalidOperationException(string.Format("Prefix not defined for namespace {0}", ns));
                    }

                    prefix = string.Empty;
                    _pendingBaseDeclarationNamespace = ns;
                    _pendingBaseDeclarationPrefix = prefix;
                }
            }
            else if (prefix == null)
            {
                if (_state != WriteState.Attribute)
                {
                    throw new InvalidOperationException(string.Format("Prefix not defined for namespace: {0}", ns));
                }

                if (BufferingWriter != null)
                {
                    prefix = BufferingWriter.LookupPrefix(ns);
                }

                if (prefix == null)
                {
                    prefix = AutoGeneratePrefix(ns, true);
                    _pendingBaseDeclarationNamespace = ns;
                    _pendingBaseDeclarationPrefix = prefix;
                }
            }

            _manager.AddLocalNamespaceIfNotRedundant(prefix, ns);
            // not visibly utilized; so don't force rendering

            if (prefix.Length != 0)
            {
                WriteStringCore(prefix);
                WriteStringCore(":");
            }

            WriteStringCore(localName);
            if (ShouldDelegate)
            {
                if (prefix.Length != 0)
                {
                    _bufferingWriter.WriteString(prefix);
                    _bufferingWriter.WriteString(":");
                }

                _bufferingWriter.WriteString(localName);
            }
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteStringCore(buffer, index, count, true, false);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteRaw(buffer, index, count);
            }
        }

        public override void WriteRaw(string data)
        {
            WriteStringCore(data, true);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteRaw(data);
            }
        }

        public override void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            WriteStartAttributeCore(ref prefix, localName == null ? null : localName.Value, ns == null ? null : ns.Value);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartAttribute(prefix, localName, ns);
            }
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            WriteStartAttributeCore(ref prefix, localName, ns);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartAttribute(prefix, localName, ns);
            }
        }

        private void WriteStartAttributeCore(ref string prefix, string localName, string ns)
        {
            if (_state != WriteState.Element)
            {
                ThrowBadStateException("WriteStartAttribute");
            }

            if (localName == null)
            {
                throw new ArgumentNullException(nameof(localName));
            }

            if (prefix == null)
            {
                prefix = string.Empty;
            }

            if (prefix.Length == 0 && localName == "xmlns")
            {
                _currentAttributePrefix = "xmlns";
                _currentAttributeName = string.Empty;
                _currentAttributeValue = string.Empty;
            }
            else if (prefix == "xmlns")
            {
                _currentAttributePrefix = "xmlns";
                _currentAttributeName = localName;
                _currentAttributeValue = string.Empty;
            }

            else
            {
                if (localName.Length == 0)
                {
                    throw new ArgumentOutOfRangeException("localName", "Length must be greater than zero.");
                }

                // non-namespace declaration attribute
                if (ns == null)
                {
                    if (prefix.Length == 0)
                    {
                        ns = string.Empty;
                    }
                    else
                    {
                        ns = LookupNamespace(prefix);
                        if (ns == null)
                        {
                            throw new InvalidOperationException(string.Format("Undefined use of prefix at attribute: {0}, {1}", prefix, localName));
                        }
                    }
                }
                else if (ns.Length == 0)
                {
                    if (prefix.Length != 0)
                    {
                        throw new InvalidOperationException("Invalid Namespace for empty Prefix.");
                    }
                }
                else if (prefix.Length == 0)
                {
                    prefix = _manager.LookupPrefix(ns, true);
                    if (prefix == null)
                    {
                        prefix = AutoGeneratePrefix(ns, true);
                    }
                }

                _currentAttributePrefix = prefix;
                _currentAttributeNamespace = ns;
                _currentAttributeName = localName;
                _currentAttributeValue = string.Empty;
            }

            _state = WriteState.Attribute;
        }

        public override void WriteStartDocument()
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartDocument();
            }
        }

        public override void WriteStartDocument(bool standalone)
        {
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartDocument(standalone);
            }
        }

        public override void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            WriteStartElementCore(ref prefix, localName == null ? null : localName.Value, ns == null ? null : ns.Value);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartElement(prefix, localName, ns);
            }
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            WriteStartElementCore(ref prefix, localName, ns);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteStartElement(prefix, localName, ns);
            }
        }

        private void WriteStartElementCore(ref string prefix, string localName, string ns)
        {
            if (_state == WriteState.Attribute)
            {
                ThrowBadStateException("WriteStartElement");
            }

            if (localName == null || localName.Length == 0)
            {
                throw new ArgumentNullException(nameof(localName));
            }

            OnPossibleEndOfBase64Content();
            OnPossibleEndOfStartTag(WriteState.Element);
            if (_docPos == XmlDocumentPosition.BeforeRootElement)
            {
                _docPos = XmlDocumentPosition.InRootElement;
            }

            _manager.EnterElementContext();

            if (ns == null)
            {
                if (prefix == null)
                {
                    prefix = string.Empty;
                }

                ns = LookupNamespace(prefix);
                if (ns == null)
                {
                    throw new InvalidOperationException(string.Format("Undefined use of prefix at Element: {0}, {1}", prefix, localName));
                }
            }
            else if (prefix == null)
            {
                prefix = LookupPrefix(ns);
                if (prefix == null)
                {
                    prefix = AutoGeneratePrefix(ns, false);
                }
            }

            _manager.AddLocalNamespaceIfNotRedundant(prefix, ns);
            _manager.MarkToRenderForVisiblyUsedPrefix(prefix, true, _contextProvider);
            PushElement(prefix, localName);
            _attributePrefixGenerationIndex = 0;
        }

        public override void WriteString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            WriteStringCore(s);
            if (ShouldDelegate)
            {
                CanonicalEncoder.WriteEscapedString(_bufferingWriter, s);
            }
        }

        public override void WriteString(XmlDictionaryString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            WriteStringCore(s.Value);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteString(s);
            }
        }

        private void WriteStringCore(string s)
        {
            WriteStringCore(s, false);
        }

        private void WriteStringCore(string s, bool isRaw)
        {
            OnPossibleEndOfBase64Content();
            if (_state == WriteState.Attribute)
            {
                if (_currentAttributeValue.Length == 0)
                {
                    _currentAttributeValue = s;
                }
                else
                {
                    // attribute value concatenation path unlikely to be hit in most scenarios
                    _currentAttributeValue += s;
                }
            }
            else
            {
                if (_state == WriteState.Element)
                {
                    OnPossibleEndOfStartTag(WriteState.Content);
                }
                else if (_state != WriteState.Content)
                {
                    ThrowBadStateException("WriteString");
                }

                if (isRaw)
                {
                    _encoder.Encode(s);
                }
                else
                {
                    _encoder.EncodeWithTranslation(s, CanonicalEncoder.XmlStringType.TextContent);
                }
            }
        }

        private void WriteStringCore(char[] buffer, int offset, int count, bool isRaw, bool isBase64Generated)
        {
            if (!isBase64Generated)
            {
                OnPossibleEndOfBase64Content();
            }

            if (_state == WriteState.Attribute)
            {
                if (_currentAttributeValue.Length == 0)
                {
                    _currentAttributeValue = new string(buffer, offset, count);
                }
                else
                {
                    // attribute value concatenation path unlikely to be hit in most scenarios
                    _currentAttributeValue += new string(buffer, offset, count);
                }
            }
            else
            {
                if (_state == WriteState.Element)
                {
                    OnPossibleEndOfStartTag(WriteState.Content);
                }
                else if (_state != WriteState.Content)
                {
                    ThrowBadStateException("WriteString");
                }

                if (isRaw || isBase64Generated)
                {
                    _encoder.Encode(buffer, offset, count);
                }
                else
                {
                    _encoder.EncodeWithTranslation(buffer, offset, count, CanonicalEncoder.XmlStringType.TextContent);
                }
            }
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            throw new NotSupportedException();
        }

        public override void WriteValue(string value)
        {
            WriteStringCore(value);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteValue(value);
            }
        }

        public override void WriteWhitespace(string ws)
        {
            WriteStringCore(ws);
            if (ShouldDelegate)
            {
                _bufferingWriter.WriteWhitespace(ws);
            }
        }

        private struct ElementEntry
        {
            internal string prefix;
            internal string localName;

            public void Clear()
            {
                prefix = null;
                localName = null;
            }

            public void Set(string prefix, string localName)
            {
                this.prefix = prefix;
                this.localName = localName;
            }
        }
    }

    internal interface ICanonicalWriterEndRootElementCallback
    {
        void OnEndOfRootElement(CanonicalWriter writer);
    }

    // accumulates attributes of one element
    internal sealed class CanonicalAttributeManager
    {
        private const int InitialListSize = 8;
        private const int MaxPoolSize = 16;
        private AttributeEntry[] _list = new AttributeEntry[InitialListSize];
        private int _count;
        private readonly Pool<AttributeEntry> _pool = new Pool<AttributeEntry>(MaxPoolSize);

        public CanonicalAttributeManager()
        {
            Clear();
        }

        public void Add(string prefix, string localName, string namespaceUri, string value)
        {
            AttributeEntry entry = _pool.Take();
            if (entry == null)
            {
                entry = new AttributeEntry();
            }

            entry.Init(prefix, localName, namespaceUri, value);
            if (_count == _list.Length)
            {
                AttributeEntry[] newList = new AttributeEntry[_list.Length * 2];
                Array.Copy(_list, 0, newList, 0, _count);
                _list = newList;
            }

            _list[_count++] = entry;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                AttributeEntry entry = _list[i];
                _list[i] = null;
                entry.Clear();
                _pool.Return(entry);
            }

            _count = 0;
        }

        public XmlAttributeHolder[] Copy()
        {
            XmlAttributeHolder[] holder = new XmlAttributeHolder[_count];
            for (int i = 0; i < _count; i++)
            {
                AttributeEntry a = _list[i];
                holder[i] = new XmlAttributeHolder(a.prefix, a.localName, a.namespaceUri, a.value);
            }

            return holder;
        }

        public void Encode(CanonicalEncoder encoder)
        {
            for (int i = 0; i < _count; i++)
            {
                _list[i].Encode(encoder);
            }
        }

        public void Sort()
        {
            Array.Sort(_list, 0, _count, AttributeEntrySortOrder.Instance);
        }

        private sealed class AttributeEntry
        {
            internal string prefix;
            internal string localName;
            internal string namespaceUri;
            internal string value;

            public void Init(string prefix, string localName, string namespaceUri, string value)
            {
                this.prefix = prefix;
                this.localName = localName;
                this.namespaceUri = namespaceUri;
                this.value = value;
            }

            public void Clear()
            {
                prefix = null;
                localName = null;
                namespaceUri = null;
                value = null;
            }

            public void Encode(CanonicalEncoder encoder)
            {
                encoder.EncodeAttribute(prefix, localName, value);
            }

            public void WriteTo(XmlDictionaryWriter writer)
            {
                writer.WriteAttributeString(prefix, localName, namespaceUri, value);
            }
        }

        private sealed class AttributeEntrySortOrder : IComparer<AttributeEntry>
        {
            private static AttributeEntrySortOrder s_instance = new AttributeEntrySortOrder();

            private AttributeEntrySortOrder() { }

            public static AttributeEntrySortOrder Instance
            {
                get { return s_instance; }
            }

            public int Compare(AttributeEntry x, AttributeEntry y)
            {
                int namespaceCompareResult = string.Compare(x.namespaceUri, y.namespaceUri, StringComparison.Ordinal);
                if (namespaceCompareResult != 0)
                {
                    return namespaceCompareResult;
                }

                return string.Compare(x.localName, y.localName, StringComparison.Ordinal);
            }
        }
    }

    internal struct XmlAttributeHolder
    {
        private string _prefix;
        private string _ns;
        private string _localName;
        private string _value;

        public static XmlAttributeHolder[] emptyArray = new XmlAttributeHolder[0];

        public XmlAttributeHolder(string prefix, string localName, string ns, string value)
        {
            _prefix = prefix;
            _localName = localName;
            _ns = ns;
            _value = value;
        }
    }
}
