// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlBaseWriter : XmlDictionaryWriter, IFragmentCapableXmlDictionaryWriter
    {
        private XmlNodeWriter _writer = null!; // initialized in SetOutput
        private readonly NamespaceManager _nsMgr;
        private Element[]? _elements;
        private int _depth;
        private string? _attributeLocalName;
        private string? _attributeValue;
        private bool _isXmlAttribute;
        private bool _isXmlnsAttribute;
        private WriteState _writeState;
        private DocumentState _documentState;
        private byte[]? _trailBytes;
        private int _trailByteCount;
        private XmlStreamNodeWriter _nodeWriter = null!; // initialized in SetOutput
        private XmlSigningNodeWriter? _signingWriter;
        private XmlUTF8NodeWriter? _textFragmentWriter;
        private XmlNodeWriter? _oldWriter;
        private Stream? _oldStream;
        private int _oldNamespaceBoundary;
        private bool _inList;
        private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private static readonly string[] s_prefixes = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

        protected XmlBaseWriter()
        {
            _nsMgr = new NamespaceManager();
            _writeState = WriteState.Start;
            _documentState = DocumentState.None;
        }

        internal override CancellationToken CancellationToken
        {
            get => _nodeWriter.CancellationToken;
            set
            {
                _nodeWriter.CancellationToken = value;
                if (_signingWriter is not null)
                {
                    _signingWriter.CancellationToken = value;
                }
                if (_textFragmentWriter is not null)
                {
                    _textFragmentWriter.CancellationToken = value;
                }
            }
        }

        protected void SetOutput(XmlStreamNodeWriter writer)
        {
            _inList = false;
            _writer = writer;
            _nodeWriter = writer;
            _writeState = WriteState.Start;
            _documentState = DocumentState.None;
            _nsMgr.Clear();
            if (_depth != 0)
            {
                _elements = null;
                _depth = 0;
            }
            _attributeLocalName = null;
            _attributeValue = null;
            _oldWriter = null;
            _oldStream = null;
        }

        public override void Flush()
        {
            if (IsClosed)
                ThrowClosed();

            _writer.Flush();
        }

        public override Task FlushAsync()
        {
            if (IsClosed)
                ThrowClosed();

            return _writer.FlushAsync();
        }

        public override void Close()
        {
            if (IsClosed)
                return;

            try
            {
                FinishDocument();
                AutoComplete(WriteState.Closed);
                _writer.Flush();
            }
            finally
            {
                _nsMgr.Close();
                if (_depth != 0)
                {
                    _elements = null;
                    _depth = 0;
                }
                _attributeValue = null;
                _attributeLocalName = null;
                _nodeWriter.Close();
                _signingWriter?.Close();
                _textFragmentWriter?.Close();
                _oldWriter = null;
                _oldStream = null;
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (IsClosed)
            {
                return;
            }
            try
            {
                await FinishDocumentAsync().ConfigureAwait(false);
                await AutoCompleteAsync(WriteState.Closed).ConfigureAwait(false);
                await _writer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeState = WriteState.Closed;
                _nsMgr.Close();
                if (_depth != 0)
                {
                    _elements = null;
                    _depth = 0;
                }
                _attributeValue = null;
                _attributeLocalName = null;
                try
                {
                    await _nodeWriter.CloseAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (_signingWriter is not null)
                    {
                        await _signingWriter.CloseAsync().ConfigureAwait(false);
                    }
                    if (_textFragmentWriter is not null)
                    {
                        await _textFragmentWriter.CloseAsync().ConfigureAwait(false);
                    }
                    _oldWriter = null;
                    _oldStream = null;
                }
            }
        }

        protected bool IsClosed
        {
            get { return _writeState == WriteState.Closed; }
        }

        [DoesNotReturn]
        protected static void ThrowClosed()
        {
            throw new InvalidOperationException(SR.XmlWriterClosed);
        }

        public override string? XmlLang
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

        public override WriteState WriteState
        {
            get
            {
                return _writeState;
            }
        }

        public override void WriteXmlnsAttribute(string? prefix, string ns)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(ns);

            if (_writeState != WriteState.Element)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString()));

            if (prefix == null)
            {
                prefix = _nsMgr.LookupPrefix(ns);
                if (prefix == null)
                {
                    GeneratePrefix(ns, null);
                }
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns, null);
            }
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(ns);

            if (_writeState != WriteState.Element)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString()));

            if (prefix == null)
            {
                prefix = _nsMgr.LookupPrefix(ns.Value);
                if (prefix == null)
                {
                    GeneratePrefix(ns.Value, ns);
                }
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns.Value, ns);
            }
        }

        internal override Task WriteXmlnsAttributeAsync(string? prefix, string namespaceUri)
        {
            WriteXmlnsAttribute(prefix, namespaceUri);
            return Task.CompletedTask;
        }

        internal override Task WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString namespaceUri)
        {
            WriteXmlnsAttribute(prefix, namespaceUri);
            return Task.CompletedTask;
        }

        private void StartAttribute([AllowNull] ref string prefix, string localName, string? ns, XmlDictionaryString? xNs)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                WriteEndAttribute();

            if (localName == null || (localName.Length == 0 && prefix != "xmlns"))
                throw new ArgumentNullException(nameof(localName));

            if (_writeState != WriteState.Element)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartAttribute", WriteState.ToString()));

            if (prefix == null)
            {
                if (ns == xmlnsNamespace && localName != "xmlns")
                    prefix = "xmlns";
                else if (ns == xmlNamespace)
                    prefix = "xml";
                else
                    prefix = string.Empty;
            }

            // Normalize a (prefix,localName) of (null, "xmlns") to ("xmlns", string.Empty).
            if (prefix.Length == 0 && localName == "xmlns")
            {
                prefix = "xmlns";
                localName = string.Empty;
            }

            _isXmlnsAttribute = false;
            _isXmlAttribute = false;
            if (prefix == "xml")
            {
                if (ns != null && ns != xmlNamespace)
                    throw new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, "xml", xmlNamespace, ns), nameof(ns));
                _isXmlAttribute = true;
                _attributeValue = string.Empty;
                _attributeLocalName = localName;
            }
            else if (prefix == "xmlns")
            {
                if (ns != null && ns != xmlnsNamespace)
                    throw new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, "xmlns", xmlnsNamespace, ns), nameof(ns));
                _isXmlnsAttribute = true;
                _attributeValue = string.Empty;
                _attributeLocalName = localName;
            }
            else if (ns == null)
            {
                // A null namespace means the namespace of the given prefix.
                // An empty prefix on an attribute means no namespace (not the default namespace)
                if (prefix.Length != 0)
                {
                    ns = _nsMgr.LookupNamespace(prefix);

                    if (ns == null)
                        throw new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix));
                }
            }
            else if (ns.Length == 0)
            {
                // An empty namespace means no namespace; prefix must be empty
                if (prefix.Length != 0)
                    throw new ArgumentException(SR.XmlEmptyNamespaceRequiresNullPrefix, nameof(prefix));
            }
            else if (prefix.Length == 0)
            {
                // No prefix specified - try to find a prefix corresponding to the given namespace
                string? tempPrefix = _nsMgr.LookupAttributePrefix(ns);

                // If we didn't find anything with the right namespace, generate one.
                if (tempPrefix == null)
                {
                    // Watch for special values
                    if (ns.Length == xmlnsNamespace.Length && ns == xmlnsNamespace)
                        throw new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xmlns", ns));
                    if (ns.Length == xmlNamespace.Length && ns == xmlNamespace)
                        throw new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xml", ns));

                    tempPrefix = GeneratePrefix(ns, xNs);
                }
                prefix = tempPrefix;
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
            }
            _writeState = WriteState.Attribute;
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? namespaceUri)
        {
            StartAttribute(ref prefix, localName, namespaceUri, null);
            if (!_isXmlnsAttribute)
            {
                _writer.WriteStartAttribute(prefix, localName);
            }
        }

        public override void WriteStartAttribute(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            StartAttribute(ref prefix, localName?.Value!, namespaceUri?.Value, namespaceUri);
            if (!_isXmlnsAttribute)
            {
                _writer.WriteStartAttribute(prefix, localName!);
            }
        }

        protected override async Task WriteStartAttributeAsync(string? prefix, string localName, string? namespaceUri)
        {
            if (_writeState == WriteState.Attribute)
            {
                await WriteEndAttributeAsync().ConfigureAwait(false);
            }
            StartAttribute(ref prefix, localName, namespaceUri, null);
            if (!_isXmlnsAttribute)
            {
                await _writer.WriteStartAttributeAsync(prefix, localName).ConfigureAwait(false);
            }
        }

        internal override async Task WriteStartAttributeAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            if (_writeState == WriteState.Attribute)
            {
                await WriteEndAttributeAsync().ConfigureAwait(false);
            }
            StartAttribute(ref prefix, localName?.Value!, namespaceUri?.Value, namespaceUri);
            if (!_isXmlnsAttribute)
            {
                await _writer.WriteStartAttributeAsync(prefix, localName!).ConfigureAwait(false);
            }
        }

        public override void WriteEndAttribute()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState != WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString()));

            FlushBase64();
            try
            {
                if (_isXmlAttribute)
                {
                    if (_attributeLocalName == "lang")
                    {
                        _nsMgr.AddLangAttribute(_attributeValue!);
                    }
                    else if (_attributeLocalName == "space")
                    {
                        if (_attributeValue == "preserve")
                        {
                            _nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
                        }
                        else if (_attributeValue == "default")
                        {
                            _nsMgr.AddSpaceAttribute(XmlSpace.Default);
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.XmlInvalidXmlSpace, _attributeValue));
                        }
                    }
                    else
                    {
                        // XmlTextWriter specifically allows for other localNames
                    }
                    _isXmlAttribute = false;
                    _attributeLocalName = null;
                    _attributeValue = null;
                }

                if (_isXmlnsAttribute)
                {
                    _nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName!, _attributeValue!, null);
                    _isXmlnsAttribute = false;
                    _attributeLocalName = null;
                    _attributeValue = null;
                }
                else
                {
                    _writer.WriteEndAttribute();
                }
            }
            finally
            {
                _writeState = WriteState.Element;
            }
        }

        protected override Task WriteEndAttributeAsync()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState != WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString()));

            return WriteEndAttributeAsyncImpl();
        }

        private async Task WriteEndAttributeAsyncImpl()
        {
            await FlushBase64Async().ConfigureAwait(false);
            try
            {
                if (_isXmlAttribute)
                {
                    if (_attributeLocalName == "lang")
                    {
                        _nsMgr.AddLangAttribute(_attributeValue!);
                    }
                    else if (_attributeLocalName == "space")
                    {
                        if (_attributeValue == "preserve")
                        {
                            _nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
                        }
                        else if (_attributeValue == "default")
                        {
                            _nsMgr.AddSpaceAttribute(XmlSpace.Default);
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.XmlInvalidXmlSpace, _attributeValue));
                        }
                    }
                    else
                    {
                        // XmlTextWriter specifically allows for other localNames
                    }
                    _isXmlAttribute = false;
                    _attributeLocalName = null;
                    _attributeValue = null;
                }

                if (_isXmlnsAttribute)
                {
                    _nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName!, _attributeValue!, null);
                    _isXmlnsAttribute = false;
                    _attributeLocalName = null;
                    _attributeValue = null;
                }
                else
                {
                    await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _writeState = WriteState.Element;
            }
        }

        public override void WriteComment(string? text)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteComment", WriteState.ToString()));

            if (text == null)
            {
                text = string.Empty;
            }
            else if (text.Contains("--") || text.EndsWith('-'))
            {
                throw new ArgumentException(SR.XmlInvalidCommentChars, nameof(text));
            }

            StartComment();
            FlushBase64();
            _writer.WriteComment(text);
            EndComment();
        }

        public override async Task WriteCommentAsync(string? text)
        {
            if (IsClosed)
                ThrowClosed();
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, nameof(WriteComment), WriteState.ToString()));
            text ??= string.Empty;
            if (text.Contains("--") || text.EndsWith('-'))
                throw new ArgumentException(SR.XmlInvalidCommentChars, nameof(text));
            await FlushElementAsync().ConfigureAwait(false);
            await FlushBase64Async().ConfigureAwait(false);
            await _writer.WriteCommentAsync(text).ConfigureAwait(false);
        }

        public override void WriteFullEndElement()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                WriteEndAttribute();

            if (_writeState != WriteState.Element && _writeState != WriteState.Content)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteFullEndElement", WriteState.ToString()));

            AutoComplete(WriteState.Content);
            WriteEndElement();
        }

        public override async Task WriteFullEndElementAsync()
        {
            if (IsClosed)
                ThrowClosed();
            if (_writeState == WriteState.Attribute)
                await WriteEndAttributeAsync().ConfigureAwait(false);
            if (_writeState != WriteState.Element && _writeState != WriteState.Content)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, nameof(WriteFullEndElement), WriteState.ToString()));
            await AutoCompleteAsync(WriteState.Content).ConfigureAwait(false);
            await WriteEndElementAsync().ConfigureAwait(false);
        }

        public override void WriteCData(string? text)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteCData", WriteState.ToString()));

            text ??= string.Empty;

            if (text.Length > 0)
            {
                StartContent();
                FlushBase64();
                _writer.WriteCData(text);
                EndContent();
            }
        }

        public override async Task WriteCDataAsync(string? text)
        {
            if (IsClosed)
                ThrowClosed();
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, nameof(WriteCData), WriteState.ToString()));
            if (!string.IsNullOrEmpty(text))
            {
                await StartContentAsync().ConfigureAwait(false);
                await FlushBase64Async().ConfigureAwait(false);
                await _writer.WriteCDataAsync(text).ConfigureAwait(false);
            }
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteDocType"));
        }

        public override Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset) =>
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, nameof(WriteDocType)));

        private void StartElement(ref string? prefix, string localName, string? ns, XmlDictionaryString? xNs)
        {
            if (IsClosed)
                ThrowClosed();

            if (_documentState == DocumentState.Epilog)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            ArgumentNullException.ThrowIfNull(localName);
            if (localName.Length == 0)
                throw new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName));
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString()));

            FlushBase64();
            AutoComplete(WriteState.Element);
            Element element = EnterScope();
            if (ns == null)
            {
                prefix ??= string.Empty;

                ns = _nsMgr.LookupNamespace(prefix);

                if (ns == null)
                    throw new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix));
            }
            else if (prefix == null)
            {
                prefix = _nsMgr.LookupPrefix(ns);

                if (prefix == null)
                {
                    prefix = string.Empty;
                    _nsMgr.AddNamespace(string.Empty, ns, xNs);
                }
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
            }
            element.Prefix = prefix;
            element.LocalName = localName;
        }

        private void PreStartElementAsyncCheck(string localName)
        {
            if (IsClosed)
                ThrowClosed();

            if (_documentState == DocumentState.Epilog)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            ArgumentNullException.ThrowIfNull(localName);
            if (localName.Length == 0)
                throw new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName));
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString()));
        }

        private async Task StartElementAndWriteStartElementAsync(string? prefix, string localName, string? namespaceUri)
        {
            prefix = await StartElementAsync(prefix, localName, namespaceUri, null).ConfigureAwait(false);
            await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(false);
        }

        private async Task<string> StartElementAsync(string? prefix, string localName, string? ns, XmlDictionaryString? xNs)
        {
            await FlushBase64Async().ConfigureAwait(false);
            await AutoCompleteAsync(WriteState.Element).ConfigureAwait(false);
            Element element = EnterScope();
            if (ns == null)
            {
                prefix ??= string.Empty;

                ns = _nsMgr.LookupNamespace(prefix);

                if (ns == null)
                    throw new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix));
            }
            else if (prefix == null)
            {
                prefix = _nsMgr.LookupPrefix(ns);

                if (prefix == null)
                {
                    prefix = string.Empty;
                    _nsMgr.AddNamespace(string.Empty, ns, xNs);
                }
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
            }
            element.Prefix = prefix;
            element.LocalName = localName;

            return prefix;
        }

        public override void WriteStartElement(string? prefix, string localName, string? namespaceUri)
        {
            StartElement(ref prefix, localName, namespaceUri, null);
            _writer.WriteStartElement(prefix, localName);
        }

        public override Task WriteStartElementAsync(string? prefix, string localName, string? namespaceUri)
        {
            PreStartElementAsyncCheck(localName);
            if (_trailByteCount == 0 && _writer is XmlUTF8NodeWriter { CanWriteBufferedContractElements: true } textWriter)
            {
                long maximumBytes = GetStartElementMaximumBytes(textWriter, prefix, localName, namespaceUri);
                if (maximumBytes <= textWriter.StreamBuffer.Length)
                {
                    if (textWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteStartElement(prefix, localName, namespaceUri);
                        return Task.CompletedTask;
                    }

                    return WriteBufferedStartElementAsync(textWriter, prefix, localName, namespaceUri, (int)maximumBytes);
                }
            }

            return StartElementAndWriteStartElementAsync(prefix, localName, namespaceUri);
        }

        private long GetStartElementMaximumBytes(XmlUTF8NodeWriter writer, string? prefix, string localName, string? namespaceUri)
        {
            string actualPrefix = prefix ?? (namespaceUri is null ? string.Empty : _nsMgr.LookupPrefix(namespaceUri)) ?? string.Empty;
            return GetPendingElementMaximumBytes(writer) + 2 +
                (long)XmlStreamNodeWriter.MaxUtf8BytesPerChar * (actualPrefix.Length + (long)localName.Length);
        }

        private long GetPendingElementMaximumBytes(XmlUTF8NodeWriter writer) =>
            _writeState == WriteState.Element ? 1 + _nsMgr.GetPendingNamespaceMaximumBytes(writer) : 0;

        private async Task WriteBufferedStartElementAsync(XmlUTF8NodeWriter writer, string? prefix,
            string localName, string? namespaceUri, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteStartElement(prefix, localName, namespaceUri);
        }

        public override void WriteStartElement(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            StartElement(ref prefix, localName.Value, namespaceUri?.Value, namespaceUri);
            _writer.WriteStartElement(prefix, localName);
        }

        internal override Task WriteStartElementAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            PreStartElementAsyncCheck(localName.Value);
            if (_writer is XmlBinaryNodeWriter { CanWriteBufferedContractElements: true } binaryWriter &&
                _trailByteCount == 0)
            {
                string actualPrefix = prefix ?? (namespaceUri is null ? string.Empty : _nsMgr.LookupPrefix(namespaceUri.Value)) ?? string.Empty;
                byte[] nameBytes = localName.ToUTF8();
                long maximumBytes = 1 + XmlBinaryNodeWriter.GetElementPrefixMaximumBytes(actualPrefix) +
                    (nameBytes.Length == localName.Value.Length ? 5L + nameBytes.Length : XmlBinaryNodeWriter.GetNameMaximumBytes(localName.Value)) +
                    (_writeState == WriteState.Element ? _nsMgr.GetPendingNamespaceMaximumBytes(binaryWriter) : 0);
                if (maximumBytes <= binaryWriter.StreamBuffer.Length)
                {
                    if (binaryWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteBufferedBinaryStartElement(binaryWriter, prefix, localName, namespaceUri);
                        return Task.CompletedTask;
                    }
                    return WriteBufferedBinaryStartElementAsync(binaryWriter, prefix, localName, namespaceUri, (int)maximumBytes);
                }
            }
            if (_trailByteCount == 0 && _writer is XmlUTF8NodeWriter { CanWriteBufferedContractElements: true } textWriter)
            {
                long maximumBytes = GetStartElementMaximumBytes(textWriter, prefix, localName.Value, namespaceUri?.Value);
                if (maximumBytes <= textWriter.StreamBuffer.Length)
                {
                    if (textWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteStartElement(prefix, localName, namespaceUri);
                        return Task.CompletedTask;
                    }

                    return WriteBufferedStartElementAsync(textWriter, prefix, localName, namespaceUri, (int)maximumBytes);
                }
            }

            return WriteStartElementAsyncCore(prefix, localName, namespaceUri);
        }

        private async Task WriteBufferedBinaryStartElementAsync(XmlBinaryNodeWriter writer, string? prefix,
            XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteBufferedBinaryStartElement(writer, prefix, localName, namespaceUri);
        }

        private void WriteBufferedBinaryStartElement(XmlBinaryNodeWriter writer, string? prefix,
            XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            StartElement(ref prefix, localName.Value, namespaceUri?.Value, namespaceUri);
            byte[] nameBytes = localName.ToUTF8();
            if (nameBytes.Length == localName.Value.Length)
                writer.WriteBufferedStartElement(prefix, nameBytes);
            else
                writer.WriteStartElement(prefix, localName);
        }

        private async Task WriteStartElementAsyncCore(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            prefix = await StartElementAsync(prefix, localName.Value, namespaceUri?.Value, namespaceUri).ConfigureAwait(false);
            await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(false);
        }

        private async Task WriteBufferedStartElementAsync(XmlUTF8NodeWriter writer, string? prefix,
            XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteStartElement(prefix, localName, namespaceUri);
        }

        public override void WriteEndElement()
        {
            if (IsClosed)
                ThrowClosed();

            if (_depth == 0)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture)));

            if (_writeState == WriteState.Attribute)
                WriteEndAttribute();

            FlushBase64();
            if (_writeState == WriteState.Element)
            {
                _nsMgr.DeclareNamespaces(_writer);
                _writer.WriteEndStartElement(true);
            }
            else
            {
                Element element = _elements![_depth];
                _writer.WriteEndElement(element.Prefix, element.LocalName!);
            }

            ExitScope();
            _writeState = WriteState.Content;
        }

        public override Task WriteEndElementAsync()
        {
            if (IsClosed)
                ThrowClosed();

            if (_depth == 0)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture)));

            if (_writer is XmlBinaryNodeWriter { CanWriteBufferedContractElements: true } binaryWriter &&
                _trailByteCount == 0 && _writeState != WriteState.Attribute)
            {
                long maximumBytes = 1 + (_writeState == WriteState.Element ? _nsMgr.GetPendingNamespaceMaximumBytes(binaryWriter) : 0);
                if (maximumBytes <= binaryWriter.StreamBuffer.Length)
                {
                    if (binaryWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteEndElement();
                        return Task.CompletedTask;
                    }
                    return WriteBufferedBinaryEndElementAsync(binaryWriter, (int)maximumBytes);
                }
            }
            if (_trailByteCount == 0 && _writeState != WriteState.Attribute && _writer is XmlUTF8NodeWriter { CanWriteBufferedContractElements: true } textWriter)
            {
                Element element = _elements![_depth];
                long maximumBytes = _writeState == WriteState.Element
                    ? 2 + _nsMgr.GetPendingNamespaceMaximumBytes(textWriter)
                    : 4 + (long)XmlStreamNodeWriter.MaxUtf8BytesPerChar * ((element.Prefix?.Length ?? 0) + (long)element.LocalName!.Length);
                if (maximumBytes <= textWriter.StreamBuffer.Length)
                {
                    if (textWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteEndElement();
                        return Task.CompletedTask;
                    }

                    return WriteBufferedEndElementAsync(textWriter, (int)maximumBytes);
                }
            }

            return WriteEndElementAsyncImpl();
        }

        private async Task WriteBufferedBinaryEndElementAsync(XmlBinaryNodeWriter writer, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteEndElement();
        }

        private async Task WriteBufferedEndElementAsync(XmlUTF8NodeWriter writer, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteEndElement();
        }

        private async Task WriteEndElementAsyncImpl()
        {
            if (_writeState == WriteState.Attribute)
                await WriteEndAttributeAsync().ConfigureAwait(false);

            await FlushBase64Async().ConfigureAwait(false);
            if (_writeState == WriteState.Element)
            {
                await _nsMgr.DeclareNamespacesAsync(_writer).ConfigureAwait(false);
                await _writer.WriteEndStartElementAsync(true).ConfigureAwait(false);
            }
            else
            {
                Element element = _elements![_depth];
                await _writer.WriteEndElementAsync(element.Prefix, element.LocalName!).ConfigureAwait(false);
            }

            ExitScope();
            _writeState = WriteState.Content;
        }

        private Element EnterScope()
        {
            _nsMgr.EnterScope();
            _depth++;
            if (_elements == null)
            {
                _elements = new Element[4];
            }
            else if (_elements.Length == _depth)
            {
                Element[] newElementNodes = new Element[_depth * 2];
                Array.Copy(_elements, newElementNodes, _depth);
                _elements = newElementNodes;
            }
            Element element = _elements[_depth];
            if (element == null)
            {
                element = new Element();
                _elements[_depth] = element;
            }
            return element;
        }

        private void ExitScope()
        {
            _elements![_depth].Clear();
            _depth--;
            if (_depth == 0 && _documentState == DocumentState.Document)
                _documentState = DocumentState.Epilog;
            _nsMgr.ExitScope();
        }

        protected void FlushElement()
        {
            if (_writeState == WriteState.Element)
            {
                AutoComplete(WriteState.Content);
            }
        }

        private Task FlushElementAsync()
        {
            return _writeState == WriteState.Element ? AutoCompleteAsync(WriteState.Content) : Task.CompletedTask;
        }

        protected void StartComment()
        {
            FlushElement();
        }

        protected static void EndComment()
        {
        }

        protected void StartContent()
        {
            FlushElement();
            if (_depth == 0)
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
        }

        protected async Task StartContentAsync()
        {
            await FlushElementAsync().ConfigureAwait(false);
            if (_depth == 0)
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
        }

        private async Task StartContentAsync(string value)
        {
            await FlushElementAsync().ConfigureAwait(false);
            if (_depth == 0)
                VerifyWhitespace(value);
        }

        private async Task StartContentAsync(char[] chars, int offset, int count)
        {
            await FlushElementAsync().ConfigureAwait(false);
            if (_depth == 0)
                VerifyWhitespace(chars, offset, count);
        }

        protected void StartContent(char ch)
        {
            FlushElement();
            if (_depth == 0)
                VerifyWhitespace(ch);
        }

        protected void StartContent(string s)
        {
            FlushElement();
            if (_depth == 0)
                VerifyWhitespace(s);
        }

        protected void StartContent(char[] chars, int offset, int count)
        {
            FlushElement();
            if (_depth == 0)
                VerifyWhitespace(chars, offset, count);
        }

        private static void VerifyWhitespace(char ch)
        {
            if (!XmlConverter.IsWhitespace(ch))
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
        }

        private static void VerifyWhitespace(string s)
        {
            if (!XmlConverter.IsWhitespace(s))
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
        }

        private static void VerifyWhitespace(char[] chars, int offset, int count)
        {
            if (!XmlConverter.IsWhitespace(chars.AsSpan(offset, count)))
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
        }

        protected static void EndContent()
        {
        }

        private void AutoComplete(WriteState writeState)
        {
            if (_writeState == WriteState.Element)
            {
                EndStartElement();
            }
            _writeState = writeState;
        }

        private async Task AutoCompleteAsync(WriteState writeState)
        {
            if (_writeState == WriteState.Element)
            {
                await EndStartElementAsync().ConfigureAwait(false);
            }
            _writeState = writeState;
        }

        private void EndStartElement()
        {
            _nsMgr.DeclareNamespaces(_writer);
            _writer.WriteEndStartElement(false);
        }

        private async Task EndStartElementAsync()
        {
            await _nsMgr.DeclareNamespacesAsync(_writer).ConfigureAwait(false);
            await _writer.WriteEndStartElementAsync(false).ConfigureAwait(false);
        }

        public override string? LookupPrefix(string ns)
        {
            ArgumentNullException.ThrowIfNull(ns);

            if (IsClosed)
                ThrowClosed();

            return _nsMgr.LookupPrefix(ns);
        }

        private string GetQualifiedNamePrefix(string namespaceUri, XmlDictionaryString? xNs)
        {
            string? prefix = _nsMgr.LookupPrefix(namespaceUri);
            if (prefix == null)
            {
                if (_writeState != WriteState.Attribute)
                    throw new ArgumentException(SR.Format(SR.XmlNamespaceNotFound, namespaceUri), nameof(namespaceUri));

                prefix = GeneratePrefix(namespaceUri, xNs);
            }
            return prefix;
        }

        public override void WriteQualifiedName(string localName, string? namespaceUri)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentException.ThrowIfNullOrEmpty(localName);
            namespaceUri ??= string.Empty;
            string prefix = GetQualifiedNamePrefix(namespaceUri, null);
            if (prefix.Length != 0)
            {
                WriteString(prefix);
                WriteString(":");
            }
            WriteString(localName);
        }

        public override async Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentException.ThrowIfNullOrEmpty(localName);
            string prefix = GetQualifiedNamePrefix(ns ?? string.Empty, null);
            if (prefix.Length != 0)
            {
                await WriteStringAsync(prefix).ConfigureAwait(false);
                await WriteStringAsync(":").ConfigureAwait(false);
            }
            await WriteStringAsync(localName).ConfigureAwait(false);
        }

        public override void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(localName);
            if (localName.Value.Length == 0)
                throw new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName));
            namespaceUri ??= XmlDictionaryString.Empty;
            string prefix = GetQualifiedNamePrefix(namespaceUri.Value, namespaceUri);

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(string.Concat(prefix, ":", namespaceUri.Value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteQualifiedName(prefix, localName);
                EndContent();
            }
        }

        internal override async Task WriteQualifiedNameAsync(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(localName);
            if (localName.Value.Length == 0)
                throw new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName));
            namespaceUri ??= XmlDictionaryString.Empty;
            string prefix = GetQualifiedNamePrefix(namespaceUri.Value, namespaceUri);
            await FlushBase64Async().ConfigureAwait(false);
            if (_attributeValue is not null)
                WriteAttributeText(string.Concat(prefix, ":", namespaceUri.Value));
            if (!_isXmlnsAttribute)
            {
                await StartContentAsync().ConfigureAwait(false);
                await _writer.WriteQualifiedNameAsync(prefix, localName).ConfigureAwait(false);
            }
        }

        public override void WriteStartDocument()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState != WriteState.Start)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartDocument", WriteState.ToString()));

            _writeState = WriteState.Prolog;
            _documentState = DocumentState.Document;
            _writer.WriteDeclaration();
        }

        public override void WriteStartDocument(bool standalone)
        {
            if (IsClosed)
                ThrowClosed();

            WriteStartDocument();
        }

        public override Task WriteStartDocumentAsync()
        {
            if (IsClosed)
                ThrowClosed();
            if (_writeState != WriteState.Start)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, nameof(WriteStartDocument), WriteState.ToString()));
            _writeState = WriteState.Prolog;
            _documentState = DocumentState.Document;
            return _writer.WriteDeclarationAsync();
        }

        public override Task WriteStartDocumentAsync(bool standalone) => WriteStartDocumentAsync();


        public override void WriteProcessingInstruction(string name, string? text)
        {
            if (IsClosed)
                ThrowClosed();

            if (name != "xml")
                throw new ArgumentException(SR.XmlProcessingInstructionNotSupported, nameof(name));

            if (_writeState != WriteState.Start)
                throw new InvalidOperationException(SR.XmlInvalidDeclaration);

            // The only thing the text can legitimately contain is version, encoding, and standalone.
            // We only support version 1.0, we can only write whatever encoding we were supplied,
            // and we don't support DTDs, so whatever values are supplied in the text argument are irrelevant.
            _writer.WriteDeclaration();
        }

        public override Task WriteProcessingInstructionAsync(string name, string? text)
        {
            if (IsClosed)
                ThrowClosed();
            if (name != "xml")
                throw new ArgumentException(SR.XmlProcessingInstructionNotSupported, nameof(name));
            if (_writeState != WriteState.Start)
                throw new InvalidOperationException(SR.XmlInvalidDeclaration);
            return _writer.WriteDeclarationAsync();
        }

        private void FinishDocument()
        {
            if (_writeState == WriteState.Attribute)
            {
                WriteEndAttribute();
            }

            while (_depth > 0)
            {
                WriteEndElement();
            }
        }

        private async Task FinishDocumentAsync()
        {
            if (_writeState == WriteState.Attribute)
            {
                await WriteEndAttributeAsync().ConfigureAwait(false);
            }
            while (_depth > 0)
            {
                await WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        public override void WriteEndDocument()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Start || _writeState == WriteState.Prolog)
                throw new InvalidOperationException(SR.XmlNoRootElement);

            FinishDocument();
            _writeState = WriteState.Start;
            _documentState = DocumentState.End;
        }

        public override async Task WriteEndDocumentAsync()
        {
            if (IsClosed)
                ThrowClosed();
            if (_writeState == WriteState.Start || _writeState == WriteState.Prolog)
                throw new InvalidOperationException(SR.XmlNoRootElement);
            await FinishDocumentAsync().ConfigureAwait(false);
            _writeState = WriteState.Start;
            _documentState = DocumentState.End;
        }

        protected int NamespaceBoundary
        {
            get
            {
                return _nsMgr.NamespaceBoundary;
            }
            set
            {
                _nsMgr.NamespaceBoundary = value;
            }
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteEntityRef"));
        }

        public override Task WriteEntityRefAsync(string name) =>
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, nameof(WriteEntityRef)));

        public override void WriteName(string name)
        {
            if (IsClosed)
                ThrowClosed();

            WriteString(name);
        }

        public override Task WriteNameAsync(string name) => WriteStringAsync(name);

        public override void WriteNmToken(string name)
        {
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteNmToken"));
        }

        public override Task WriteNmTokenAsync(string name) =>
            throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, nameof(WriteNmToken)));

        public override void WriteWhitespace(string? whitespace)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(whitespace);

            if (whitespace.AsSpan().ContainsAnyExcept(" \t\r\n"))
            {
                throw new ArgumentException(SR.XmlOnlyWhitespace, nameof(whitespace));
            }

            WriteString(whitespace);
        }

        public override Task WriteWhitespaceAsync(string? ws)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(ws);
            if (ws.AsSpan().ContainsAnyExcept(" \t\r\n"))
                throw new ArgumentException(SR.XmlOnlyWhitespace, nameof(ws));
            return WriteStringAsync(ws);
        }

        public override void WriteString(string? value)
        {
            if (IsClosed)
                ThrowClosed();

            value ??= string.Empty;

            if (value.Length > 0 || _inList)
            {
                FlushBase64();

                if (_attributeValue != null)
                    WriteAttributeText(value);

                if (!_isXmlnsAttribute)
                {
                    StartContent(value);
                    _writer.WriteEscapedText(value);
                    EndContent();
                }
            }
        }

        public override void WriteString(XmlDictionaryString? value)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(value);

            if (value.Value.Length > 0)
            {
                FlushBase64();

                if (_attributeValue != null)
                    WriteAttributeText(value.Value);

                if (!_isXmlnsAttribute)
                {
                    StartContent(value.Value);
                    _writer.WriteEscapedText(value);
                    EndContent();
                }
            }
        }

        public override Task WriteStringAsync(string? text)
        {
            if (IsClosed)
                ThrowClosed();
            text ??= string.Empty;
            return text.Length > 0 || _inList ? WriteStringAsyncImpl(text, null, raw: false) : Task.CompletedTask;
        }

        internal override Task WriteStringAsync(XmlDictionaryString? value)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(value);
            return value.Value.Length > 0 ? WriteStringAsyncImpl(value.Value, value, raw: false) : Task.CompletedTask;
        }

        private Task WriteStringAsyncImpl(string value, XmlDictionaryString? dictionaryValue, bool raw)
        {
            if (_trailByteCount == 0 && _writer is XmlUTF8NodeWriter { CanWriteBufferedContractElements: true } textWriter)
            {
                long maximumBytes = GetPendingElementMaximumBytes(textWriter) +
                    (raw ? (long)XmlStreamNodeWriter.MaxUtf8BytesPerChar * value.Length
                        : textWriter.GetEscapedTextMaximumBytes(value, _writeState == WriteState.Attribute));
                if (maximumBytes <= textWriter.StreamBuffer.Length)
                {
                    if (textWriter.HasBufferSpace((int)maximumBytes))
                    {
                        WriteBufferedString(value, dictionaryValue, raw);
                        return Task.CompletedTask;
                    }

                    return WriteBufferedStringAsync(textWriter, value, dictionaryValue, raw, (int)maximumBytes);
                }
            }

            return WriteStringAsyncCore(value, dictionaryValue, raw);
        }

        private void WriteBufferedString(string value, XmlDictionaryString? dictionaryValue, bool raw)
        {
            if (raw)
                WriteRaw(value);
            else if (dictionaryValue is not null)
                WriteString(dictionaryValue);
            else
                WriteString(value);
        }

        private async Task WriteBufferedStringAsync(XmlUTF8NodeWriter writer, string value,
            XmlDictionaryString? dictionaryValue, bool raw, int maximumBytes)
        {
            await writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteBufferedString(value, dictionaryValue, raw);
        }

        private async Task WriteStringAsyncCore(string value, XmlDictionaryString? dictionaryValue, bool raw)
        {
            await FlushBase64Async().ConfigureAwait(false);
            if (_attributeValue is not null)
                WriteAttributeText(value);
            if (!_isXmlnsAttribute)
            {
                await StartContentAsync(value).ConfigureAwait(false);
                if (raw)
                    await _writer.WriteTextAsync(value).ConfigureAwait(false);
                else if (dictionaryValue is not null)
                    await _writer.WriteEscapedTextAsync(dictionaryValue).ConfigureAwait(false);
                else
                    await _writer.WriteEscapedTextAsync(value).ConfigureAwait(false);
            }
        }

        public override void WriteChars(char[] chars, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(chars);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));

            if (count > 0)
            {
                FlushBase64();

                if (_attributeValue != null)
                    WriteAttributeText(new string(chars, offset, count));

                if (!_isXmlnsAttribute)
                {
                    StartContent(chars, offset, count);
                    _writer.WriteEscapedText(chars, offset, count);
                    EndContent();
                }
            }
        }

        public override Task WriteCharsAsync(char[] buffer, int index, int count) =>
            WriteCharsAsyncImpl(buffer, index, count, raw: false);

        private async Task WriteCharsAsyncImpl(char[] chars, int offset, int count, bool raw)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
            if (count > 0)
            {
                await FlushBase64Async().ConfigureAwait(false);
                if (_attributeValue is not null)
                    WriteAttributeText(new string(chars, offset, count));
                if (!_isXmlnsAttribute)
                {
                    await StartContentAsync(chars, offset, count).ConfigureAwait(false);
                    if (raw)
                        await _writer.WriteTextAsync(chars, offset, count).ConfigureAwait(false);
                    else
                        await _writer.WriteEscapedTextAsync(chars, offset, count).ConfigureAwait(false);
                }
            }
        }

        public override void WriteRaw(string value)
        {
            if (IsClosed)
                ThrowClosed();

            value ??= string.Empty;

            if (value.Length > 0)
            {
                FlushBase64();

                if (_attributeValue != null)
                    WriteAttributeText(value);

                if (!_isXmlnsAttribute)
                {
                    StartContent(value);
                    _writer.WriteText(value);
                    EndContent();
                }
            }
        }

        public override void WriteRaw(char[] chars, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(chars);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));

            if (count > 0)
            {
                FlushBase64();

                if (_attributeValue != null)
                    WriteAttributeText(new string(chars, offset, count));

                if (!_isXmlnsAttribute)
                {
                    StartContent(chars, offset, count);
                    _writer.WriteText(chars, offset, count);
                    EndContent();
                }
            }
        }

        public override Task WriteRawAsync(string data)
        {
            if (IsClosed)
                ThrowClosed();
            data ??= string.Empty;
            return data.Length > 0 ? WriteStringAsyncImpl(data, null, raw: true) : Task.CompletedTask;
        }

        public override Task WriteRawAsync(char[] buffer, int index, int count) =>
            WriteCharsAsyncImpl(buffer, index, count, raw: true);

        public override void WriteCharEntity(char ch)
        {
            if (IsClosed)
                ThrowClosed();

            if (ch >= 0xd800 && ch <= 0xdfff)
                throw new ArgumentException(SR.XmlMissingLowSurrogate, nameof(ch));

            if (_attributeValue != null)
                WriteAttributeText(ch.ToString());

            if (!_isXmlnsAttribute)
            {
                StartContent(ch);
                FlushBase64();
                _writer.WriteCharEntity(ch);
                EndContent();
            }
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            if (IsClosed)
                ThrowClosed();

            SurrogateChar ch = new SurrogateChar(lowChar, highChar);

            if (_attributeValue != null)
            {
                WriteAttributeText(new string([highChar, lowChar]));
            }

            if (!_isXmlnsAttribute)
            {
                StartContent();
                FlushBase64();
                _writer.WriteCharEntity(ch.Char);
                EndContent();
            }
        }

        public override async Task WriteCharEntityAsync(char ch)
        {
            if (IsClosed)
                ThrowClosed();
            if (char.IsSurrogate(ch))
                throw new ArgumentException(SR.XmlMissingLowSurrogate, nameof(ch));
            if (_attributeValue is not null)
                WriteAttributeText(ch.ToString());
            if (!_isXmlnsAttribute)
            {
                await FlushElementAsync().ConfigureAwait(false);
                if (_depth == 0)
                    VerifyWhitespace(ch);
                await FlushBase64Async().ConfigureAwait(false);
                await _writer.WriteCharEntityAsync(ch).ConfigureAwait(false);
            }
        }

        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            if (IsClosed)
                ThrowClosed();
            SurrogateChar ch = new SurrogateChar(lowChar, highChar);
            if (_attributeValue is not null)
                WriteAttributeText(new string([highChar, lowChar]));
            if (!_isXmlnsAttribute)
            {
                await StartContentAsync().ConfigureAwait(false);
                await FlushBase64Async().ConfigureAwait(false);
                await _writer.WriteCharEntityAsync(ch.Char).ConfigureAwait(false);
            }
        }

        public override void WriteValue(object value)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(value);

            if (value is object[])
            {
                WriteValue((object[])value);
            }
            else if (value is Array)
            {
                WriteValue((Array)value);
            }
            else if (value is IStreamProvider)
            {
                WriteValue((IStreamProvider)value);
            }
            else
            {
                WritePrimitiveValue(value);
            }
        }

        internal override async Task WriteValueAsync(object value)
        {
            if (IsClosed)
                ThrowClosed();
            ArgumentNullException.ThrowIfNull(value);
            if (value is Array array)
            {
                await WriteListValueAsync(array).ConfigureAwait(false);
            }
            else if (value is string text)
            {
                await WriteStringAsync(text).ConfigureAwait(false);
            }
            else if (value is XmlDictionaryString dictionaryText)
            {
                await WriteStringAsync(dictionaryText).ConfigureAwait(false);
            }
            else if (value is bool or int or long or ulong or float or double or decimal or DateTime or UniqueId or Guid or TimeSpan)
            {
                await FlushBase64Async().ConfigureAwait(false);
                if (_attributeValue is not null)
                    WriteAttributeText(GetValueText(value));
                if (!_isXmlnsAttribute)
                {
                    await StartContentAsync().ConfigureAwait(false);
                    await _writer.WriteValueAsync(value).ConfigureAwait(false);
                }
            }
            else
            {
                await base.WriteValueAsync(value).ConfigureAwait(false);
            }
        }

        internal Task WriteXmlPrimitiveValueAsync<T>(object value) where T : struct =>
            WriteXmlPrimitiveValueAsyncCore<T>(value);

        private async Task WriteXmlPrimitiveValueAsyncCore<T>(object value) where T : struct
        {
            if (IsClosed)
                ThrowClosed();

            await FlushBase64Async().ConfigureAwait(false);
            if (_attributeValue is not null)
                WriteAttributeText(GetPrimitiveValueText<T>(value));
            if (!_isXmlnsAttribute)
            {
                await StartContentAsync().ConfigureAwait(false);
                await _writer.WritePrimitiveValueAsync<T>(value).ConfigureAwait(false);
            }
        }

        private async Task WriteListValueAsync(Array array)
        {
            await FlushBase64Async().ConfigureAwait(false);
            await StartContentAsync().ConfigureAwait(false);
            await _writer.WriteStartListTextAsync().ConfigureAwait(false);
            _inList = true;
            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (i != 0)
                        await _writer.WriteListSeparatorAsync().ConfigureAwait(false);
                    object? value = array.GetValue(i);
                    ArgumentNullException.ThrowIfNull(value);
                    if (value is Array)
                        throw new ArgumentException(SR.XmlNestedArraysNotSupported, nameof(value));
                    await WriteValueAsync(value).ConfigureAwait(false);
                }
            }
            finally
            {
                _inList = false;
            }
            await _writer.WriteEndListTextAsync().ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task WriteXmlPrimitiveElementAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, object value) where T : struct
        {
            if (_writer is XmlBinaryNodeWriter binaryWriter &&
                TryWriteBinaryPrimitiveElement<T, object>(binaryWriter, prefix, localName, namespaceUri, value) is Task binaryOperation)
                return binaryOperation;
            return WriteTextPrimitiveElementAsync<T>(prefix, localName, namespaceUri, value);
        }

        private Task WriteTextPrimitiveElementAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, object value) where T : struct
        {
            long valueSize = XmlUTF8NodeWriter.GetPrimitiveBufferSize<T>();
            if (valueSize != 0 &&
                TryGetBufferedPrimitiveElement(prefix, localName, namespaceUri, valueSize,
                out XmlUTF8NodeWriter? textWriter, out string? actualPrefix, out int maximumBytes))
            {
                if (textWriter.HasBufferSpace(maximumBytes))
                {
                    WriteBufferedPrimitiveElement<T, object>(textWriter, actualPrefix, localName, value);
                    return Task.CompletedTask;
                }

                return WriteBufferedPrimitiveElementAsync<T, object>(textWriter, actualPrefix, localName, value, maximumBytes);
            }

            return WritePrimitiveElementAsyncFallback<T>(prefix, localName, namespaceUri, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal Task WriteTypedPrimitiveElementAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, T value) where T : struct
        {
            if (_writer is XmlBinaryNodeWriter binaryWriter &&
                TryWriteBinaryPrimitiveElement<T, T>(binaryWriter, prefix, localName, namespaceUri, value) is Task binaryOperation)
                return binaryOperation;
            return WriteTypedTextPrimitiveElementAsync(prefix, localName, namespaceUri, value);
        }

        private Task WriteTypedTextPrimitiveElementAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, T value) where T : struct
        {
            int valueSize = XmlUTF8NodeWriter.GetPrimitiveBufferSize<T>();
            if (valueSize != 0 && TryGetBufferedPrimitiveElement(prefix, localName, namespaceUri, valueSize,
                out XmlUTF8NodeWriter? textWriter, out string? actualPrefix, out int maximumBytes))
            {
                if (textWriter.HasBufferSpace(maximumBytes))
                {
                    WriteBufferedPrimitiveElement<T, T>(textWriter, actualPrefix, localName, value);
                    return Task.CompletedTask;
                }

                return WriteBufferedPrimitiveElementAsync<T, T>(textWriter, actualPrefix, localName, value, maximumBytes);
            }

            return WritePrimitiveElementAsyncFallback<T>(prefix, localName, namespaceUri, value);
        }

        private Task? TryWriteBinaryPrimitiveElement<TPrimitive, TValue>(XmlBinaryNodeWriter node,
            string? prefix, XmlDictionaryString name, XmlDictionaryString? ns, TValue value)
            where TPrimitive : struct
        {
            if (!node.CanWriteBufferedContractElements || _trailByteCount != 0 || _depth == 0 ||
                _writeState is not (WriteState.Element or WriteState.Content) || ns is null)
                return null;
            string? actualPrefix = prefix is null ? _nsMgr.LookupPrefix(ns.Value) :
                _nsMgr.LookupNamespace(prefix) == ns.Value ? prefix : null;
            if (actualPrefix is null)
                return null;
            long valueSize = XmlBinaryNodeWriter.GetPrimitiveBufferSize<TPrimitive>();
            if (valueSize == 0)
                return null;
            byte[] nameBytes = name.ToUTF8();
            bool asciiName = nameBytes.Length == name.Value.Length;
            long size = 2 + XmlBinaryNodeWriter.GetElementPrefixMaximumBytes(actualPrefix) +
                (asciiName ? 5L + nameBytes.Length : XmlBinaryNodeWriter.GetNameMaximumBytes(name.Value)) + valueSize +
                (_writeState == WriteState.Element ? _nsMgr.GetPendingNamespaceMaximumBytes(node) : 0);
            if (size > node.StreamBuffer.Length)
                return null;
            if (node.HasBufferSpace((int)size))
            {
                WriteBinaryPrimitiveElement<TPrimitive, TValue>(node, actualPrefix, name, ns, value);
                return Task.CompletedTask;
            }
            return WriteBinaryPrimitiveElementAsync<TPrimitive, TValue>(node, actualPrefix, name, ns, value, (int)size);
        }

        private Task WriteBinaryPrimitiveElementAsync<TPrimitive, TValue>(XmlBinaryNodeWriter node,
            string prefix, XmlDictionaryString name, XmlDictionaryString ns, TValue value, int maximumBytes)
            where TPrimitive : struct
        {
            Task operation = node.EnsureBufferAsync(maximumBytes);
            if (!operation.IsCompletedSuccessfully)
                return AwaitBufferAndWriteBinaryPrimitiveElement<TPrimitive, TValue>(operation, node, prefix, name, ns, value);

            WriteBinaryPrimitiveElement<TPrimitive, TValue>(node, prefix, name, ns, value);
            return Task.CompletedTask;
        }

        private async Task AwaitBufferAndWriteBinaryPrimitiveElement<TPrimitive, TValue>(Task operation, XmlBinaryNodeWriter node,
            string prefix, XmlDictionaryString name, XmlDictionaryString ns, TValue value) where TPrimitive : struct
        {
            await operation.ConfigureAwait(false);
            WriteBinaryPrimitiveElement<TPrimitive, TValue>(node, prefix, name, ns, value);
        }

        protected bool TryGetBufferedBinaryArraySpace(string? prefix, XmlDictionaryString name, XmlDictionaryString? ns,
            long payloadSize, out int maximumBytes)
        {
            maximumBytes = 0;
            if (_writer is not XmlBinaryNodeWriter { CanWriteBufferedContractElements: true } node ||
                _trailByteCount != 0 || _depth == 0 || _writeState is not (WriteState.Element or WriteState.Content) || ns is null)
                return false;
            string? actualPrefix = prefix is null ? _nsMgr.LookupPrefix(ns.Value) :
                _nsMgr.LookupNamespace(prefix) == ns.Value ? prefix : null;
            if (actualPrefix is null)
                return false;
            long size = payloadSize + 3 + XmlBinaryNodeWriter.GetElementPrefixMaximumBytes(actualPrefix) +
                XmlBinaryNodeWriter.GetNameMaximumBytes(name.Value) +
                (_writeState == WriteState.Element ? _nsMgr.GetPendingNamespaceMaximumBytes(node) : 0);
            if (size > node.StreamBuffer.Length)
                return false;
            maximumBytes = (int)size;
            return true;
        }

        private void WriteBinaryPrimitiveElement<TPrimitive, TValue>(XmlBinaryNodeWriter node,
            string prefix, XmlDictionaryString name, XmlDictionaryString ns, TValue value) where TPrimitive : struct
        {
            PreStartElementAsyncCheck(name.Value);
            AutoComplete(WriteState.Element);
            byte[] nameBytes = name.ToUTF8();
            if (nameBytes.Length == name.Value.Length)
                node.WriteBufferedStartElement(prefix, nameBytes);
            else
                node.WriteStartElement(prefix, name);
            node.WritePrimitiveValue<TPrimitive, TValue>(value);
            node.WriteEndElement(prefix, name.Value);
            _writeState = WriteState.Content;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryWriteTypedPrimitiveElement<T>(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, T value)
            where T : struct
        {
            int valueSize = XmlUTF8NodeWriter.GetPrimitiveBufferSize<T>();
            if (valueSize == 0 || !TryGetBufferedPrimitiveElement(null, localName, namespaceUri, valueSize,
                out XmlUTF8NodeWriter? textWriter, out string? actualPrefix, out int maximumBytes))
                return false;

            textWriter.EnsureBuffer(maximumBytes);
            WriteBufferedPrimitiveElement<T, T>(textWriter, actualPrefix, localName, value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBufferedPrimitiveElement(string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, long valueSize, [NotNullWhen(true)] out XmlUTF8NodeWriter? textWriter,
            [NotNullWhen(true)] out string? actualPrefix, out int maximumBytes)
        {
            textWriter = _writer as XmlUTF8NodeWriter;
            actualPrefix = null;
            maximumBytes = 0;
            if (textWriter is null || !textWriter.CanWriteBufferedContractElements || _trailByteCount != 0 || _depth == 0 ||
                _writeState is not (WriteState.Element or WriteState.Content) ||
                namespaceUri is null)
                return false;

            actualPrefix = prefix is null
                ? _nsMgr.LookupPrefix(namespaceUri.Value)
                : _nsMgr.LookupNamespace(prefix) == namespaceUri.Value ? prefix : null;
            if (actualPrefix is null)
                return false;
            long size = GetBufferedPrimitiveElementMaximumBytes(actualPrefix, localName, valueSize) +
                (_writeState == WriteState.Element ? 1 + _nsMgr.GetPendingNamespaceMaximumBytes(textWriter) : 0);
            if (size > textWriter.StreamBuffer.Length)
                return false;
            maximumBytes = (int)size;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetBufferedPrimitiveElementMaximumBytes(string prefix, XmlDictionaryString name, long valueSize)
        {
            long prefixSize = prefix.Length == 0 ? 0 :
                prefix.Length == 1 && prefix[0] < 0x80 ? 2 : 1 + (long)XmlStreamNodeWriter.MaxUtf8BytesPerChar * prefix.Length;
            return "<></>"u8.Length + 2L * (prefixSize + name.ToUTF8().Length) + valueSize;
        }

        protected bool TryWriteBufferedPrimitiveArray<T>(string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, T[] array, int offset, int count, [NotNullWhen(true)] out Task? operation)
            where T : struct
        {
            Debug.Assert(count > 0);
            operation = null;
            int valueSize = XmlUTF8NodeWriter.GetPrimitiveBufferSize<T>();
            if (valueSize == 0 || !TryGetBufferedPrimitiveElement(prefix, localName, namespaceUri, valueSize,
                out XmlUTF8NodeWriter? nodeWriter, out string? actualPrefix, out int maximumBytes) ||
                actualPrefix.Length > 1 || actualPrefix.Length == 1 && actualPrefix[0] >= 0x80)
                return false;

            int elementSize = (int)GetBufferedPrimitiveElementMaximumBytes(actualPrefix, localName, valueSize);
            operation = WriteBufferedPrimitiveArrayItems(nodeWriter, actualPrefix, localName, array,
                offset, offset + count, maximumBytes, elementSize, first: true);
            return true;
        }

        private Task WriteBufferedPrimitiveArrayItems<T>(XmlUTF8NodeWriter nodeWriter, string prefix, XmlDictionaryString name,
            T[] array, int next, int end, int maximumBytes, int elementSize, bool first) where T : struct
        {
            byte[] nameBytes = name.ToUTF8();
            for (int i = next; i < end; i++)
            {
                T value = array[i];
                if (!nodeWriter.HasBufferSpace(maximumBytes))
                {
                    Task pending = nodeWriter.EnsureBufferAsync(maximumBytes);
                    if (!pending.IsCompletedSuccessfully)
                        return AwaitBufferedPrimitiveArrayItems(pending, nodeWriter, prefix, name, value, array, i + 1, end, elementSize);
                }

                if (first)
                    WriteBufferedPrimitiveElement<T, T>(nodeWriter, prefix, name, value);
                else
                    nodeWriter.WriteBufferedPrimitiveElement<T, T>(prefix, nameBytes, value);
                first = false;
                maximumBytes = elementSize;
            }

            return Task.CompletedTask;
        }

        private async Task AwaitBufferedPrimitiveArrayItems<T>(Task pending, XmlUTF8NodeWriter nodeWriter, string prefix, XmlDictionaryString name,
            T value, T[] array, int next, int end, int elementSize) where T : struct
        {
            await pending.ConfigureAwait(false);
            WriteBufferedPrimitiveElement<T, T>(nodeWriter, prefix, name, value);
            byte[] nameBytes = name.ToUTF8();
            for (int i = next; i < end; i++)
            {
                value = array[i];
                await nodeWriter.EnsureBufferAsync(elementSize).ConfigureAwait(false);
                nodeWriter.WriteBufferedPrimitiveElement<T, T>(prefix, nameBytes, value);
            }
        }

        private Task WriteBufferedPrimitiveElementAsync<TPrimitive, TValue>(XmlUTF8NodeWriter writer, string prefix,
            XmlDictionaryString localName, TValue value, int maximumBytes) where TPrimitive : struct
        {
            Task operation = writer.EnsureBufferAsync(maximumBytes);
            if (!operation.IsCompletedSuccessfully)
                return AwaitBufferAndWritePrimitiveElement<TPrimitive, TValue>(operation, writer, prefix, localName, value);

            WriteBufferedPrimitiveElement<TPrimitive, TValue>(writer, prefix, localName, value);
            return Task.CompletedTask;
        }

        private async Task AwaitBufferAndWritePrimitiveElement<TPrimitive, TValue>(Task operation, XmlUTF8NodeWriter writer,
            string prefix, XmlDictionaryString localName, TValue value) where TPrimitive : struct
        {
            await operation.ConfigureAwait(false);
            WriteBufferedPrimitiveElement<TPrimitive, TValue>(writer, prefix, localName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBufferedPrimitiveElement<TPrimitive, TValue>(XmlUTF8NodeWriter writer, string prefix,
            XmlDictionaryString localName, TValue value) where TPrimitive : struct
        {
            PreStartElementAsyncCheck(localName.Value);
            AutoComplete(WriteState.Element);
            // Buffered scalars with an existing ASCII prefix cannot change scope or call user encoders.
            bool needsScope = prefix.Length > 1 || prefix.Length == 1 && prefix[0] >= 0x80;
            byte[] localNameBytes = localName.ToUTF8();
            if (!needsScope)
            {
                _writeState = WriteState.Content;
                writer.WriteBufferedPrimitiveElement<TPrimitive, TValue>(prefix, localNameBytes, value);
                return;
            }
            Element element = EnterScope();
            element.Prefix = prefix;
            element.LocalName = localName.Value;
            writer.WriteBufferedStartElement(prefix, localNameBytes);
            writer.WriteEndStartElement(false);
            _writeState = WriteState.Content;
            writer.WritePrimitiveValue<TPrimitive, TValue>(value);
            writer.WriteBufferedEndElement(prefix, localNameBytes);
            ExitScope();
            _writeState = WriteState.Content;
        }

        protected void WritePrimitiveValue(object value)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(value);

            if (value is ulong)
            {
                WriteValue((ulong)value);
            }
            else if (value is string)
            {
                WriteValue((string)value);
            }
            else if (value is int)
            {
                WriteValue((int)value);
            }
            else if (value is long)
            {
                WriteValue((long)value);
            }
            else if (value is bool)
            {
                WriteValue((bool)value);
            }
            else if (value is double)
            {
                WriteValue((double)value);
            }
            else if (value is DateTime)
            {
                WriteValue((DateTime)value);
            }
            else if (value is float)
            {
                WriteValue((float)value);
            }
            else if (value is decimal)
            {
                WriteValue((decimal)value);
            }
            else if (value is XmlDictionaryString)
            {
                WriteValue((XmlDictionaryString)value);
            }
            else if (value is UniqueId)
            {
                WriteValue((UniqueId)value);
            }
            else if (value is Guid)
            {
                WriteValue((Guid)value);
            }
            else if (value is TimeSpan)
            {
                WriteValue((TimeSpan)value);
            }
            else if (value.GetType().IsArray)
            {
                throw new ArgumentException(SR.XmlNestedArraysNotSupported, nameof(value));
            }
            else
            {
                base.WriteValue(value);
            }
        }

        public override void WriteValue(string? value)
        {
            if (IsClosed)
                ThrowClosed();

            WriteString(value);
        }

        public override void WriteValue(int value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteInt32Text(value);
                EndContent();
            }
        }

        public override void WriteValue(long value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteInt64Text(value);
                EndContent();
            }
        }

        private void WriteValue(ulong value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteUInt64Text(value);
                EndContent();
            }
        }

        public override void WriteValue(bool value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteBoolText(value);
                EndContent();
            }
        }

        public override void WriteValue(decimal value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteDecimalText(value);
                EndContent();
            }
        }

        public override void WriteValue(float value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteFloatText(value);
                EndContent();
            }
        }

        public override void WriteValue(double value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteDoubleText(value);
                EndContent();
            }
        }

        public override void WriteValue(XmlDictionaryString? value)
        {
            WriteString(value);
        }

        public override void WriteValue(DateTime value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteDateTimeText(value);
                EndContent();
            }
        }

        public override void WriteValue(UniqueId value)
        {
            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(value);

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteUniqueIdText(value);
                EndContent();
            }
        }

        public override void WriteValue(Guid value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteGuidText(value);
                EndContent();
            }
        }

        public override void WriteValue(TimeSpan value)
        {
            if (IsClosed)
                ThrowClosed();

            FlushBase64();
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.ToString(value));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteTimeSpanText(value);
                EndContent();
            }
        }

        private static void EnsureBufferBounds(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset));
        }

        public override void WriteBinHex(byte[] buffer, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            EnsureBufferBounds(buffer, offset, count);

            WriteRaw(DataContractSerializer.BinHexEncoding.GetString(buffer, offset, count));
        }

        public override async Task WriteBinHexAsync(byte[] buffer, int index, int count)
        {
            if (IsClosed)
                ThrowClosed();
            EnsureBufferBounds(buffer, index, count);
            const int byteChunkSize = 256;
            while (count > 0)
            {
                int chunk = Math.Min(count, byteChunkSize);
                await WriteRawAsync(DataContractSerializer.BinHexEncoding.GetString(buffer, index, chunk)).ConfigureAwait(false);
                index += chunk;
                count -= chunk;
            }
        }

        public override void WriteBase64(byte[] buffer, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            EnsureBufferBounds(buffer, offset, count);

            if (count > 0)
            {
                if (_trailByteCount > 0)
                {
                    while (_trailByteCount < 3 && count > 0)
                    {
                        _trailBytes![_trailByteCount++] = buffer[offset++];
                        count--;
                    }
                }

                int totalByteCount = _trailByteCount + count;
                int actualByteCount = totalByteCount - (totalByteCount % 3);

                _trailBytes ??= new byte[3];

                if (actualByteCount >= 3)
                {
                    if (_attributeValue != null)
                    {
                        WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
                        WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(buffer, offset, actualByteCount - _trailByteCount));
                    }

                    if (!_isXmlnsAttribute)
                    {
                        StartContent();
                        _writer.WriteBase64Text(_trailBytes, _trailByteCount, buffer, offset, actualByteCount - _trailByteCount);
                        EndContent();
                    }
                    _trailByteCount = (totalByteCount - actualByteCount);

                    if (_trailByteCount > 0)
                    {
                        int trailOffset = offset + count - _trailByteCount;
                        for (int i = 0; i < _trailByteCount; i++)
                            _trailBytes[i] = buffer[trailOffset++];
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _trailBytes, _trailByteCount, count);
                    _trailByteCount += count;
                }
            }
        }

        public override Task WriteBase64Async(byte[] buffer, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            EnsureBufferBounds(buffer, offset, count);

            if (TryReserveBinaryContent(5L + _trailByteCount + count))
            {
                WriteBase64(buffer, offset, count);
                return Task.CompletedTask;
            }

            return WriteBase64AsyncImpl(buffer, offset, count);
        }

        private bool TryReserveBinaryContent(long valueSize)
        {
            if (_writer is not XmlBinaryNodeWriter { CanWriteBufferedContractElements: true } node ||
                _depth == 0 || _writeState is not (WriteState.Element or WriteState.Content))
                return false;
            long maximumBytes = valueSize +
                (_writeState == WriteState.Element ? _nsMgr.GetPendingNamespaceMaximumBytes(node) : 0);
            return maximumBytes <= node.StreamBuffer.Length && node.HasBufferSpace((int)maximumBytes);
        }

        private async Task WriteBase64AsyncImpl(byte[] buffer, int offset, int count)
        {
            if (count > 0)
            {
                if (_trailByteCount > 0)
                {
                    while (_trailByteCount < 3 && count > 0)
                    {
                        _trailBytes![_trailByteCount++] = buffer[offset++];
                        count--;
                    }
                }

                int totalByteCount = _trailByteCount + count;
                int actualByteCount = totalByteCount - (totalByteCount % 3);

                _trailBytes ??= new byte[3];

                if (actualByteCount >= 3)
                {
                    if (_attributeValue != null)
                    {
                        WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
                        WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(buffer, offset, actualByteCount - _trailByteCount));
                    }
                    if (!_isXmlnsAttribute)
                    {
                        await StartContentAsync().ConfigureAwait(false);
                        await _writer.WriteBase64TextAsync(_trailBytes, _trailByteCount, buffer, offset, actualByteCount - _trailByteCount).ConfigureAwait(false);
                        EndContent();
                    }
                    _trailByteCount = (totalByteCount - actualByteCount);
                    if (_trailByteCount > 0)
                    {
                        int trailOffset = offset + count - _trailByteCount;
                        for (int i = 0; i < _trailByteCount; i++)
                            _trailBytes[i] = buffer[trailOffset++];
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _trailBytes, _trailByteCount, count);
                    _trailByteCount += count;
                }
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
                return _writer == _signingWriter;
            }
        }

        public override void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            if (IsClosed)
                ThrowClosed();
            if (Signing)
                throw new InvalidOperationException(SR.XmlCanonicalizationStarted);
            FlushElement();
            _signingWriter ??= CreateSigningNodeWriter();
            _signingWriter.SetOutput(_writer, stream, includeComments, inclusivePrefixes);
            _writer = _signingWriter;
            SignScope(_signingWriter.CanonicalWriter);
        }

        public override void EndCanonicalization()
        {
            if (IsClosed)
                ThrowClosed();
            if (!Signing)
                throw new InvalidOperationException(SR.XmlCanonicalizationNotStarted);
            _signingWriter!.Flush();
            _writer = _signingWriter.NodeWriter;
        }

        internal override async Task StartCanonicalizationAsync(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            if (IsClosed)
                ThrowClosed();
            if (Signing)
                throw new InvalidOperationException(SR.XmlCanonicalizationStarted);
            await FlushElementAsync().ConfigureAwait(false);
            _signingWriter ??= CreateSigningNodeWriter();
            _signingWriter.SetOutput(_writer, stream, includeComments, inclusivePrefixes);
            _signingWriter.CancellationToken = CancellationToken;
            _writer = _signingWriter;
            SignScope(_signingWriter.CanonicalWriter);
        }

        internal override async Task EndCanonicalizationAsync()
        {
            if (IsClosed)
                ThrowClosed();
            if (!Signing)
                throw new InvalidOperationException(SR.XmlCanonicalizationNotStarted);
            await _signingWriter!.FlushAsync().ConfigureAwait(false);
            _writer = _signingWriter.NodeWriter;
        }

        protected abstract XmlSigningNodeWriter CreateSigningNodeWriter();

        public virtual bool CanFragment
        {
            get
            {
                return true;
            }
        }

        public void StartFragment(Stream stream, bool generateSelfContainedTextFragment)
        {
            if (!CanFragment)
                throw new NotSupportedException();

            if (IsClosed)
                ThrowClosed();

            ArgumentNullException.ThrowIfNull(stream);

            if (_oldStream != null || _oldWriter != null)
                throw new InvalidOperationException();
            if (WriteState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "StartFragment", WriteState.ToString()));

            FlushElement();
            _writer.Flush();

            _oldNamespaceBoundary = NamespaceBoundary;

            XmlStreamNodeWriter? fragmentWriter = null;
            if (generateSelfContainedTextFragment)
            {
                this.NamespaceBoundary = _depth + 1;
                _textFragmentWriter ??= new XmlUTF8NodeWriter();
                _textFragmentWriter.SetOutput(stream, false, Encoding.UTF8);
                fragmentWriter = _textFragmentWriter;
            }

            if (Signing)
            {
                if (fragmentWriter != null)
                {
                    _oldWriter = _signingWriter!.NodeWriter;
                    _signingWriter.NodeWriter = fragmentWriter;
                }
                else
                {
                    _oldStream = ((XmlStreamNodeWriter)_signingWriter!.NodeWriter).OutputStream;
                    ((XmlStreamNodeWriter)_signingWriter.NodeWriter).OutputStream = stream;
                }
            }
            else
            {
                if (fragmentWriter != null)
                {
                    _oldWriter = _writer;
                    _writer = fragmentWriter;
                }
                else
                {
                    _oldStream = _nodeWriter.OutputStream;
                    _nodeWriter.OutputStream = stream;
                }
            }
        }

        public void EndFragment()
        {
            if (IsClosed)
                ThrowClosed();

            if (_oldStream == null && _oldWriter == null)
                throw new InvalidOperationException();
            if (WriteState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "EndFragment", WriteState.ToString()));

            FlushElement();
            _writer.Flush();

            if (Signing)
            {
                if (_oldWriter != null)
                    _signingWriter!.NodeWriter = _oldWriter;
                else
                    ((XmlStreamNodeWriter)_signingWriter!.NodeWriter).OutputStream = _oldStream!; // Checked above. _oldStream can't be null if _oldWriter is.
            }
            else
            {
                if (_oldWriter != null)
                    _writer = _oldWriter;
                else
                    _nodeWriter.OutputStream = _oldStream!; // Checked above. _oldStream can't be null if _oldWriter is.
            }
            NamespaceBoundary = _oldNamespaceBoundary;
            _oldWriter = null;
            _oldStream = null;
        }

        public void WriteFragment(byte[] buffer, int offset, int count)
        {
            if (!CanFragment)
                throw new NotSupportedException();

            if (IsClosed)
                ThrowClosed();

            EnsureBufferBounds(buffer, offset, count);

            if (WriteState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteFragment", WriteState.ToString()));

            if (_writer != _nodeWriter)
                throw new InvalidOperationException();

            FlushElement();
            FlushBase64();
            _nodeWriter.Flush();
            _nodeWriter.OutputStream.Write(buffer, offset, count);
        }

        private void FlushBase64()
        {
            if (_trailByteCount > 0)
            {
                FlushTrailBytes();
            }
        }

        private Task FlushBase64Async()
        {
            return _trailByteCount > 0 ? FlushTrailBytesAsync() : Task.CompletedTask;
        }

        private void FlushTrailBytes()
        {
            Debug.Assert(_trailBytes != null);

            if (_attributeValue != null)
                WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteBase64Text(_trailBytes, _trailByteCount, _trailBytes, 0, 0);
                EndContent();
            }
            _trailByteCount = 0;
        }

        private Task FlushTrailBytesAsync()
        {
            if (TryReserveBinaryContent(5L + _trailByteCount))
            {
                FlushTrailBytes();
                return Task.CompletedTask;
            }

            return FlushTrailBytesAsyncCore();
        }

        private async Task FlushTrailBytesAsyncCore()
        {
            Debug.Assert(_trailBytes != null);

            if (_attributeValue != null)
                WriteAttributeText(DataContractSerializer.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));

            if (!_isXmlnsAttribute)
            {
                await StartContentAsync().ConfigureAwait(false);
                await _writer.WriteBase64TextAsync(_trailBytes, _trailByteCount, _trailBytes, 0, 0).ConfigureAwait(false);
                EndContent();
            }
            _trailByteCount = 0;
        }

        private void WriteValue(object[] array)
        {
            FlushBase64();
            StartContent();
            _writer.WriteStartListText();
            _inList = true;
            for (int i = 0; i < array.Length; i++)
            {
                if (i != 0)
                {
                    _writer.WriteListSeparator();
                }
                WritePrimitiveValue(array[i]);
            }
            _inList = false;
            _writer.WriteEndListText();
            EndContent();
        }

        private void WriteValue(Array array)
        {
            FlushBase64();
            StartContent();
            _writer.WriteStartListText();
            _inList = true;
            for (int i = 0; i < array.Length; i++)
            {
                if (i != 0)
                {
                    _writer.WriteListSeparator();
                }
                WritePrimitiveValue(array.GetValue(i)!); // possible bug to log?
            }
            _inList = false;
            _writer.WriteEndListText();
            EndContent();
        }

        protected void StartArray(int count)
        {
            FlushBase64();
            if (_documentState == DocumentState.Epilog)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            if (_documentState == DocumentState.Document && count > 1 && _depth == 0)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString()));
            AutoComplete(WriteState.Content);
        }

        protected async Task StartArrayAsync(int count)
        {
            if (IsClosed)
                ThrowClosed();
            await FlushBase64Async().ConfigureAwait(false);
            if (_documentState == DocumentState.Epilog)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            if (_documentState == DocumentState.Document && count > 1 && _depth == 0)
                throw new InvalidOperationException(SR.XmlOnlyOneRoot);
            if (_writeState == WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, nameof(WriteStartElement), WriteState.ToString()));
            await AutoCompleteAsync(WriteState.Content).ConfigureAwait(false);
        }

        private string GeneratePrefix(string ns, XmlDictionaryString? xNs)
        {
            if (_writeState != WriteState.Element && _writeState != WriteState.Attribute)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidPrefixState, WriteState.ToString()));

            string? prefix = _nsMgr.AddNamespace(ns, xNs);

            if (prefix != null)
                return prefix;

            while (true)
            {
                int prefixId = _elements![_depth].PrefixId++;
                prefix = string.Create(CultureInfo.InvariantCulture, $"d{_depth}p{prefixId}");

                if (_nsMgr.LookupNamespace(prefix) == null)
                {
                    _nsMgr.AddNamespace(prefix, ns, xNs);
                    return prefix;
                }
            }
        }

        protected void SignScope(XmlCanonicalWriter signingWriter)
        {
            _nsMgr.Sign(signingWriter);
        }

        private void WriteAttributeText(string value)
        {
            if (_attributeValue!.Length == 0)
                _attributeValue = value;
            else
                _attributeValue += value;
        }

        private sealed class Element
        {
            private string? _prefix;
            private string? _localName;
            private int _prefixId;

            public string? Prefix
            {
                get
                {
                    return _prefix;
                }
                set
                {
                    _prefix = value;
                }
            }

            public string? LocalName
            {
                get
                {
                    return _localName;
                }
                set
                {
                    _localName = value;
                }
            }

            public int PrefixId
            {
                get
                {
                    return _prefixId;
                }
                set
                {
                    _prefixId = value;
                }
            }

            public void Clear()
            {
                _prefix = null;
                _localName = null;
                _prefixId = 0;
            }
        }

        private enum DocumentState : byte
        {
            None,       // Not inside StartDocument/EndDocument - Allows multiple root elements
            Document,   // Inside StartDocument/EndDocument
            Epilog,     // EndDocument must be called
            End         // Nothing further to write
        }

        private sealed class NamespaceManager
        {
            private Namespace[]? _namespaces;
            private Namespace? _lastNameSpace;
            private int _nsCount;
            private int _depth;
            private XmlAttribute[]? _attributes;
            private int _attributeCount;
            private XmlSpace _space;
            private string? _lang;
            private int _namespaceBoundary;
            private int _nsTop;
            private readonly Namespace _defaultNamespace;

            public NamespaceManager()
            {
                _defaultNamespace = new Namespace();
                _defaultNamespace.Depth = 0;
                _defaultNamespace.Prefix = string.Empty;
                _defaultNamespace.Uri = string.Empty;
                _defaultNamespace.UriDictionaryString = null;
            }

            public string? XmlLang
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
                if (_namespaces == null)
                {
                    _namespaces = new Namespace[4];
                    _namespaces[0] = _defaultNamespace;
                }
                _nsCount = 1;
                _nsTop = 0;
                _depth = 0;
                _attributeCount = 0;
                _space = XmlSpace.None;
                _lang = null;
                _namespaceBoundary = 0;
                _lastNameSpace = null;
            }

            public int NamespaceBoundary
            {
                get
                {
                    return _namespaceBoundary;
                }
                set
                {
                    int i;
                    for (i = 0; i < _nsCount; i++)
                        if (_namespaces![i].Depth >= value)
                            break;

                    _nsTop = i;
                    _namespaceBoundary = value;
                    _lastNameSpace = null;
                }
            }

            public void Close()
            {
                if (_depth == 0)
                {
                    if (_namespaces != null && _namespaces.Length > 32)
                        _namespaces = null;
                    if (_attributes != null && _attributes.Length > 4)
                        _attributes = null;
                }
                else
                {
                    _namespaces = null;
                    _attributes = null;
                }
                _lang = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DeclareNamespaces(XmlNodeWriter writer)
            {
                if (_nsCount > 0 && _namespaces![_nsCount - 1].Depth == _depth)
                    DeclareNamespacesCore(writer);
            }

            private void DeclareNamespacesCore(XmlNodeWriter writer)
            {
                int i = _nsCount;
                while (i > 0)
                {
                    Namespace nameSpace = _namespaces![i - 1];
                    if (nameSpace.Depth != _depth)
                        break;
                    i--;
                }
                while (i < _nsCount)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.UriDictionaryString != null)
                        writer.WriteXmlnsAttribute(nameSpace.Prefix, nameSpace.UriDictionaryString);
                    else
                        writer.WriteXmlnsAttribute(nameSpace.Prefix, nameSpace.Uri!);
                    i++;
                }
            }

            public async Task DeclareNamespacesAsync(XmlNodeWriter writer)
            {
                int i = _nsCount;
                while (i > 0 && _namespaces![i - 1].Depth == _depth)
                {
                    i--;
                }
                while (i < _nsCount)
                {
                    Namespace nameSpace = _namespaces![i++];
                    if (nameSpace.UriDictionaryString is not null)
                        await writer.WriteXmlnsAttributeAsync(nameSpace.Prefix, nameSpace.UriDictionaryString).ConfigureAwait(false);
                    else
                        await writer.WriteXmlnsAttributeAsync(nameSpace.Prefix, nameSpace.Uri!).ConfigureAwait(false);
                }
            }

            internal long GetPendingNamespaceMaximumBytes(XmlUTF8NodeWriter writer)
            {
                long maximumBytes = 0;
                for (int i = _nsCount - 1; i >= 0 && _namespaces![i].Depth == _depth; i--)
                {
                    Namespace ns = _namespaces![i];
                    string uri = ns.UriDictionaryString?.Value ?? ns.Uri!;
                    long uriMaximumBytes = writer.CanWriteBufferedContractElements && ns.UriDictionaryString is XmlDictionaryString dictionaryUri
                        ? dictionaryUri.GetEscapedAttributeMaximumBytes(writer)
                        : writer.GetEscapedTextMaximumBytes(uri, inAttribute: true);
                    maximumBytes += 10 + (long)XmlStreamNodeWriter.MaxUtf8BytesPerChar * (ns.Prefix?.Length ?? 0) + uriMaximumBytes;
                    if (maximumBytes > writer.StreamBuffer.Length)
                        return maximumBytes;
                }

                return maximumBytes;
            }

            internal long GetPendingNamespaceMaximumBytes(XmlBinaryNodeWriter writer)
            {
                long maximumBytes = 0;
                for (int i = _nsCount - 1; i >= 0 && _namespaces![i].Depth == _depth; i--)
                {
                    Namespace ns = _namespaces![i];
                    maximumBytes += 1 + XmlBinaryNodeWriter.GetNameMaximumBytes(ns.Prefix ?? string.Empty) +
                        XmlBinaryNodeWriter.GetNameMaximumBytes(ns.UriDictionaryString?.Value ?? ns.Uri!);
                    if (maximumBytes > writer.StreamBuffer.Length)
                        break;
                }
                return maximumBytes;
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
                    if (_lastNameSpace == nameSpace)
                        _lastNameSpace = null;
                    nameSpace.Clear();
                    _nsCount--;
                }
                while (_attributeCount > 0)
                {
                    XmlAttribute attribute = _attributes![_attributeCount - 1];
                    if (attribute.Depth != _depth)
                        break;
                    _space = attribute.XmlSpace;
                    _lang = attribute.XmlLang;
                    attribute.Clear();
                    _attributeCount--;
                }
                _depth--;
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

            public string? AddNamespace(string uri, XmlDictionaryString? uriDictionaryString)
            {
                if (uri.Length == 0)
                {
                    // Empty namespace can only be bound to the empty prefix
                    AddNamespaceIfNotDeclared(string.Empty, uri, uriDictionaryString);
                    return string.Empty;
                }
                else
                {
                    for (int i = 0; i < s_prefixes.Length; i++)
                    {
                        string prefix = s_prefixes[i];
                        bool declared = false;
                        for (int j = _nsCount - 1; j >= _nsTop; j--)
                        {
                            Namespace nameSpace = _namespaces![j];
                            if (nameSpace.Prefix == prefix)
                            {
                                declared = true;
                                break;
                            }
                        }
                        if (!declared)
                        {
                            AddNamespace(prefix, uri, uriDictionaryString);
                            return prefix;
                        }
                    }
                }
                return null;
            }

            public void AddNamespaceIfNotDeclared(string prefix, string uri, XmlDictionaryString? uriDictionaryString)
            {
                if (LookupNamespace(prefix) != uri)
                {
                    AddNamespace(prefix, uri, uriDictionaryString);
                }
            }

            public void AddNamespace(string prefix, string uri, XmlDictionaryString? uriDictionaryString)
            {
                if (prefix.Length >= 3)
                {
                    // Upper and lower case letter differ by a bit.
                    if ((prefix[0] & ~32) == 'X' && (prefix[1] & ~32) == 'M' && (prefix[2] & ~32) == 'L')
                    {
                        if (prefix == "xml" && uri == xmlNamespace)
                            return;
                        if (prefix == "xmlns" && uri == xmlnsNamespace)
                            return;
                        throw new ArgumentException(SR.XmlReservedPrefix, nameof(prefix));
                    }
                }
                Namespace nameSpace;
                for (int i = _nsCount - 1; i >= 0; i--)
                {
                    nameSpace = _namespaces![i];
                    if (nameSpace.Depth != _depth)
                        break;
                    if (nameSpace.Prefix == prefix)
                    {
                        if (nameSpace.Uri == uri)
                            return;
                        throw new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, prefix, nameSpace.Uri, uri), nameof(prefix));
                    }
                }
                if (prefix.Length != 0 && uri.Length == 0)
                    throw new ArgumentException(SR.XmlEmptyNamespaceRequiresNullPrefix, nameof(prefix));
                if (uri.Length == xmlnsNamespace.Length && uri == xmlnsNamespace)
                    throw new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xmlns", uri));
                // The addressing namespace and the xmlNamespace are the same length, so add a quick check to try to disambiguate
                if (uri.Length == xmlNamespace.Length && uri[18] == 'X' && uri == xmlNamespace)
                    throw new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xml", uri));

                if (_namespaces!.Length == _nsCount)
                {
                    Namespace[] newNamespaces = new Namespace[_nsCount * 2];
                    Array.Copy(_namespaces, newNamespaces, _nsCount);
                    _namespaces = newNamespaces;
                }
                nameSpace = _namespaces[_nsCount];
                if (nameSpace == null)
                {
                    nameSpace = new Namespace();
                    _namespaces[_nsCount] = nameSpace;
                }
                nameSpace.Depth = _depth;
                nameSpace.Prefix = prefix;
                nameSpace.Uri = uri;
                nameSpace.UriDictionaryString = uriDictionaryString;
                _nsCount++;
                _lastNameSpace = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string? LookupPrefix(string ns)
            {
                if (_lastNameSpace != null && _lastNameSpace.Uri == ns)
                    return _lastNameSpace.Prefix;

                return LookupPrefixCore(ns);
            }

            private string? LookupPrefixCore(string ns)
            {
                int nsCount = _nsCount;
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (object.ReferenceEquals(nameSpace.Uri, ns))
                    {
                        string? prefix = nameSpace.Prefix;
                        // Make sure that the prefix refers to the namespace in scope
                        bool declared = false;
                        for (int j = i + 1; j < nsCount; j++)
                        {
                            if (_namespaces[j].Prefix == prefix)
                            {
                                declared = true;
                                break;
                            }
                        }
                        if (!declared)
                        {
                            _lastNameSpace = nameSpace;
                            return prefix;
                        }
                    }
                }
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.Uri == ns)
                    {
                        string? prefix = nameSpace.Prefix;
                        // Make sure that the prefix refers to the namespace in scope
                        bool declared = false;
                        for (int j = i + 1; j < nsCount; j++)
                        {
                            if (_namespaces[j].Prefix == prefix)
                            {
                                declared = true;
                                break;
                            }
                        }
                        if (!declared)
                        {
                            _lastNameSpace = nameSpace;
                            return prefix;
                        }
                    }
                }

                if (ns.Length == 0)
                {
                    // Make sure the default binding is still valid
                    bool emptyPrefixUnassigned = true;
                    for (int i = nsCount - 1; i >= _nsTop; i--)
                    {
                        if (_namespaces![i].Prefix!.Length == 0)
                        {
                            emptyPrefixUnassigned = false;
                            break;
                        }
                    }
                    if (emptyPrefixUnassigned)
                        return string.Empty;
                }

                if (ns == xmlnsNamespace)
                    return "xmlns";
                if (ns == xmlNamespace)
                    return "xml";
                return null;
            }

            public string? LookupAttributePrefix(string ns)
            {
                if (_lastNameSpace != null && _lastNameSpace.Uri == ns && _lastNameSpace.Prefix!.Length != 0)
                    return _lastNameSpace.Prefix;

                int nsCount = _nsCount;
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces![i];

                    if (object.ReferenceEquals(nameSpace.Uri, ns))
                    {
                        string prefix = nameSpace.Prefix!;
                        if (prefix.Length != 0)
                        {
                            // Make sure that the prefix refers to the namespace in scope
                            bool declared = false;
                            for (int j = i + 1; j < nsCount; j++)
                            {
                                if (_namespaces[j].Prefix == prefix)
                                {
                                    declared = true;
                                    break;
                                }
                            }
                            if (!declared)
                            {
                                _lastNameSpace = nameSpace;
                                return prefix;
                            }
                        }
                    }
                }
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.Uri == ns)
                    {
                        string prefix = nameSpace.Prefix!;
                        if (prefix.Length != 0)
                        {
                            // Make sure that the prefix refers to the namespace in scope
                            bool declared = false;
                            for (int j = i + 1; j < nsCount; j++)
                            {
                                if (_namespaces[j].Prefix == prefix)
                                {
                                    declared = true;
                                    break;
                                }
                            }
                            if (!declared)
                            {
                                _lastNameSpace = nameSpace;
                                return prefix;
                            }
                        }
                    }
                }
                if (ns.Length == 0)
                    return string.Empty;
                return null;
            }

            public string? LookupNamespace(string prefix)
            {
                int nsCount = _nsCount;
                if (prefix.Length == 0)
                {
                    for (int i = nsCount - 1; i >= _nsTop; i--)
                    {
                        Namespace nameSpace = _namespaces![i];
                        if (nameSpace.Prefix!.Length == 0)
                            return nameSpace.Uri;
                    }
                    return string.Empty;
                }
                if (prefix.Length == 1)
                {
                    char prefixChar = prefix[0];
                    for (int i = nsCount - 1; i >= _nsTop; i--)
                    {
                        Namespace nameSpace = _namespaces![i];
                        if (nameSpace.PrefixChar == prefixChar)
                            return nameSpace.Uri;
                    }
                    return null;
                }
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces![i];
                    if (nameSpace.Prefix == prefix)
                        return nameSpace.Uri;
                }
                if (prefix == "xmlns")
                    return xmlnsNamespace;
                if (prefix == "xml")
                    return xmlNamespace;
                return null;
            }

            public void Sign(XmlCanonicalWriter signingWriter)
            {
                int nsCount = _nsCount;
                Debug.Assert(nsCount >= 1 && _namespaces![0].Prefix!.Length == 0 && _namespaces[0].Uri!.Length == 0);
                for (int i = 1; i < nsCount; i++)
                {
                    Namespace nameSpace = _namespaces[i];

                    bool found = false;
                    for (int j = i + 1; j < nsCount && !found; j++)
                    {
                        found = (nameSpace.Prefix == _namespaces[j].Prefix);
                    }

                    if (!found)
                    {
                        signingWriter.WriteXmlnsAttribute(nameSpace.Prefix!, nameSpace.Uri!);
                    }
                }
            }

            private sealed class XmlAttribute
            {
                private XmlSpace _space;
                private string? _lang;
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

                public string? XmlLang
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

                public void Clear()
                {
                    _lang = null;
                }
            }

            private sealed class Namespace
            {
                private string? _prefix;
                private string? _ns;
                private XmlDictionaryString? _xNs;
                private int _depth;
                private char _prefixChar;

                public Namespace()
                {
                }

                public void Clear()
                {
                    _prefix = null;
                    _prefixChar = (char)0;
                    _ns = null;
                    _xNs = null;
                    _depth = 0;
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

                public char PrefixChar
                {
                    get
                    {
                        return _prefixChar;
                    }
                }

                [DisallowNull]
                public string? Prefix
                {
                    get
                    {
                        return _prefix;
                    }
                    set
                    {
                        if (value.Length == 1)
                            _prefixChar = value[0];
                        else
                            _prefixChar = (char)0;
                        _prefix = value;
                    }
                }

                public string? Uri
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

                public XmlDictionaryString? UriDictionaryString
                {
                    get
                    {
                        return _xNs;
                    }
                    set
                    {
                        _xNs = value;
                    }
                }
            }
        }
    }
}
