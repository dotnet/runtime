// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlBaseWriter : XmlDictionaryWriter
    {
        private XmlNodeWriter _writer;
        private NamespaceManager _nsMgr;
        private Element[] _elements;
        private int _depth;
        private string _attributeLocalName;
        private string _attributeValue;
        private bool _isXmlAttribute;
        private bool _isXmlnsAttribute;
        private WriteState _writeState;
        private DocumentState _documentState;
        private byte[] _trailBytes;
        private int _trailByteCount;
        private XmlStreamNodeWriter _nodeWriter;
        private XmlSigningNodeWriter _signingWriter;
        private bool _inList;
        private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private static BinHexEncoding _binhexEncoding;
        private static string[] s_prefixes = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

        protected XmlBaseWriter()
        {
            _nsMgr = new NamespaceManager();
            _writeState = WriteState.Start;
            _documentState = DocumentState.None;
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
                if (_signingWriter != null)
                {
                    _signingWriter.Close();
                }
            }
        }

        protected bool IsClosed
        {
            get { return _writeState == WriteState.Closed; }
        }

        protected void ThrowClosed()
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlWriterClosed));
        }

        private static BinHexEncoding BinHexEncoding
        {
            get
            {
                if (_binhexEncoding == null)
                    _binhexEncoding = new BinHexEncoding();
                return _binhexEncoding;
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

        public override WriteState WriteState
        {
            get
            {
                return _writeState;
            }
        }

        public override void WriteXmlnsAttribute(string prefix, string ns)
        {
            if (IsClosed)
                ThrowClosed();

            if (ns == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(ns));

            if (_writeState != WriteState.Element)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString())));

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

        public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
        {
            if (IsClosed)
                ThrowClosed();

            if (ns == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(ns));

            if (_writeState != WriteState.Element)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString())));

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

        private void StartAttribute(ref string prefix, string localName, string ns, XmlDictionaryString xNs)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                WriteEndAttribute();

            if (localName == null || (localName.Length == 0 && prefix != "xmlns"))
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));

            if (_writeState != WriteState.Element)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartAttribute", WriteState.ToString())));

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
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, "xml", xmlNamespace, ns), nameof(ns)));
                _isXmlAttribute = true;
                _attributeValue = string.Empty;
                _attributeLocalName = localName;
            }
            else if (prefix == "xmlns")
            {
                if (ns != null && ns != xmlnsNamespace)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, "xmlns", xmlnsNamespace, ns), nameof(ns)));
                _isXmlnsAttribute = true;
                _attributeValue = string.Empty;
                _attributeLocalName = localName;
            }
            else if (ns == null)
            {
                // A null namespace means the namespace of the given prefix.
                if (prefix.Length == 0)
                {
                    // An empty prefix on an attribute means no namespace (not the default namespace)
                    ns = string.Empty;
                }
                else
                {
                    ns = _nsMgr.LookupNamespace(prefix);

                    if (ns == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix)));
                }
            }
            else if (ns.Length == 0)
            {
                // An empty namespace means no namespace; prefix must be empty
                if (prefix.Length != 0)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlEmptyNamespaceRequiresNullPrefix, nameof(prefix)));
            }
            else if (prefix.Length == 0)
            {
                // No prefix specified - try to find a prefix corresponding to the given namespace
                prefix = _nsMgr.LookupAttributePrefix(ns);

                // If we didn't find anything with the right namespace, generate one.
                if (prefix == null)
                {
                    // Watch for special values
                    if (ns.Length == xmlnsNamespace.Length && ns == xmlnsNamespace)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xmlns", ns)));
                    if (ns.Length == xmlNamespace.Length && ns == xmlNamespace)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xml", ns)));

                    prefix = GeneratePrefix(ns, xNs);
                }
            }
            else
            {
                _nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
            }
            _writeState = WriteState.Attribute;
        }

        public override void WriteStartAttribute(string prefix, string localName, string namespaceUri)
        {
            StartAttribute(ref prefix, localName, namespaceUri, null);
            if (!_isXmlnsAttribute)
            {
                _writer.WriteStartAttribute(prefix, localName);
            }
        }

        public override void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            StartAttribute(ref prefix, (localName != null ? localName.Value : null), (namespaceUri != null ? namespaceUri.Value : null), namespaceUri);
            if (!_isXmlnsAttribute)
            {
                _writer.WriteStartAttribute(prefix, localName);
            }
        }

        public override void WriteEndAttribute()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState != WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString())));

            FlushBase64();
            try
            {
                if (_isXmlAttribute)
                {
                    if (_attributeLocalName == "lang")
                    {
                        _nsMgr.AddLangAttribute(_attributeValue);
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
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlInvalidXmlSpace, _attributeValue)));
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
                    _nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName, _attributeValue, null);
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString())));

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
                        _nsMgr.AddLangAttribute(_attributeValue);
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
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlInvalidXmlSpace, _attributeValue)));
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
                    _nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName, _attributeValue, null);
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

        public override void WriteComment(string text)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteComment", WriteState.ToString())));

            if (text == null)
            {
                text = string.Empty;
            }
            else if (text.IndexOf("--", StringComparison.Ordinal) != -1 || (text.Length > 0 && text[text.Length - 1] == '-'))
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlInvalidCommentChars, nameof(text)));
            }

            StartComment();
            FlushBase64();
            _writer.WriteComment(text);
            EndComment();
        }

        public override void WriteFullEndElement()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                WriteEndAttribute();

            if (_writeState != WriteState.Element && _writeState != WriteState.Content)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteFullEndElement", WriteState.ToString())));

            AutoComplete(WriteState.Content);
            WriteEndElement();
        }

        public override void WriteCData(string text)
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteCData", WriteState.ToString())));

            if (text == null)
                text = string.Empty;

            if (text.Length > 0)
            {
                StartContent();
                FlushBase64();
                _writer.WriteCData(text);
                EndContent();
            }
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteDocType")));
        }

        private void StartElement(ref string prefix, string localName, string ns, XmlDictionaryString xNs)
        {
            if (IsClosed)
                ThrowClosed();

            if (_documentState == DocumentState.Epilog)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlOnlyOneRoot));
            if (localName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));
            if (localName.Length == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName)));
            if (_writeState == WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));

            FlushBase64();
            AutoComplete(WriteState.Element);
            Element element = EnterScope();
            if (ns == null)
            {
                if (prefix == null)
                    prefix = string.Empty;

                ns = _nsMgr.LookupNamespace(prefix);

                if (ns == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix)));
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

        private void PreStartElementAsyncCheck(string prefix, string localName, string ns, XmlDictionaryString xNs)
        {
            if (IsClosed)
                ThrowClosed();

            if (_documentState == DocumentState.Epilog)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlOnlyOneRoot));
            if (localName == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));
            if (localName.Length == 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName)));
            if (_writeState == WriteState.Attribute)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));
        }

        private async Task StartElementAndWriteStartElementAsync(string prefix, string localName, string namespaceUri)
        {
            prefix = await StartElementAsync(prefix, localName, namespaceUri, null).ConfigureAwait(false);
            await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(false);
        }

        private async Task<string> StartElementAsync(string prefix, string localName, string ns, XmlDictionaryString xNs)
        {
            await FlushBase64Async().ConfigureAwait(false);
            await AutoCompleteAsync(WriteState.Element).ConfigureAwait(false);
            Element element = EnterScope();
            if (ns == null)
            {
                if (prefix == null)
                    prefix = string.Empty;

                ns = _nsMgr.LookupNamespace(prefix);

                if (ns == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlUndefinedPrefix, prefix), nameof(prefix)));
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

        public override void WriteStartElement(string prefix, string localName, string namespaceUri)
        {
            StartElement(ref prefix, localName, namespaceUri, null);
            _writer.WriteStartElement(prefix, localName);
        }

        public override Task WriteStartElementAsync(string prefix, string localName, string namespaceUri)
        {
            PreStartElementAsyncCheck(prefix, localName, namespaceUri, null);
            return StartElementAndWriteStartElementAsync(prefix, localName, namespaceUri);
        }

        public override void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            StartElement(ref prefix, (localName != null ? localName.Value : null), (namespaceUri != null ? namespaceUri.Value : null), namespaceUri);
            _writer.WriteStartElement(prefix, localName);
        }

        public override void WriteEndElement()
        {
            if (IsClosed)
                ThrowClosed();

            if (_depth == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture))));

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
                Element element = _elements[_depth];
                _writer.WriteEndElement(element.Prefix, element.LocalName);
            }

            ExitScope();
            _writeState = WriteState.Content;
        }

        public override Task WriteEndElementAsync()
        {
            if (IsClosed)
                ThrowClosed();

            if (_depth == 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture))));

            return WriteEndElementAsyncImpl();
        }

        private async Task WriteEndElementAsyncImpl()
        {
            if (_writeState == WriteState.Attribute)
                await WriteEndAttributeAsync().ConfigureAwait(false);

            FlushBase64();
            if (_writeState == WriteState.Element)
            {
                _nsMgr.DeclareNamespaces(_writer);
                await _writer.WriteEndStartElementAsync(true).ConfigureAwait(false);
            }
            else
            {
                Element element = _elements[_depth];
                await _writer.WriteEndElementAsync(element.Prefix, element.LocalName).ConfigureAwait(false);
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
                Array.Copy(_elements, 0, newElementNodes, 0, _depth);
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
            _elements[_depth].Clear();
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

        protected void EndComment()
        {
        }

        protected void StartContent()
        {
            FlushElement();
            if (_depth == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIllegalOutsideRoot));
        }

        protected async Task StartContentAsync()
        {
            await FlushElementAsync().ConfigureAwait(false);
            if (_depth == 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIllegalOutsideRoot));
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

        private void VerifyWhitespace(char ch)
        {
            if (!IsWhitespace(ch))
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIllegalOutsideRoot));
        }

        private void VerifyWhitespace(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (!IsWhitespace(s[i]))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIllegalOutsideRoot));
        }

        private void VerifyWhitespace(char[] chars, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                if (!IsWhitespace(chars[offset + i]))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIllegalOutsideRoot));
        }

        private bool IsWhitespace(char ch)
        {
            return (ch == ' ' || ch == '\n' || ch == '\r' || ch == 't');
        }

        protected void EndContent()
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

        private Task EndStartElementAsync()
        {
            _nsMgr.DeclareNamespaces(_writer);
            return _writer.WriteEndStartElementAsync(false);
        }

        public override string LookupPrefix(string ns)
        {
            if (ns == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(ns)));

            if (IsClosed)
                ThrowClosed();

            return _nsMgr.LookupPrefix(ns);
        }

        private string GetQualifiedNamePrefix(string namespaceUri, XmlDictionaryString xNs)
        {
            string prefix = _nsMgr.LookupPrefix(namespaceUri);
            if (prefix == null)
            {
                if (_writeState != WriteState.Attribute)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlNamespaceNotFound, namespaceUri), nameof(namespaceUri)));

                prefix = GeneratePrefix(namespaceUri, xNs);
            }
            return prefix;
        }

        public override void WriteQualifiedName(string localName, string namespaceUri)
        {
            if (IsClosed)
                ThrowClosed();
            if (localName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));
            if (localName.Length == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName)));
            if (namespaceUri == null)
                namespaceUri = string.Empty;
            string prefix = GetQualifiedNamePrefix(namespaceUri, null);
            if (prefix.Length != 0)
            {
                WriteString(prefix);
                WriteString(":");
            }
            WriteString(localName);
        }

        public override void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (IsClosed)
                ThrowClosed();
            if (localName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));
            if (localName.Value.Length == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.InvalidLocalNameEmpty, nameof(localName)));
            if (namespaceUri == null)
                namespaceUri = XmlDictionaryString.Empty;
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

        public override void WriteStartDocument()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState != WriteState.Start)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartDocument", WriteState.ToString())));

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


        public override void WriteProcessingInstruction(string name, string text)
        {
            if (IsClosed)
                ThrowClosed();

            if (name != "xml")
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlProcessingInstructionNotSupported, nameof(name)));

            if (_writeState != WriteState.Start)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlInvalidDeclaration));

            // The only thing the text can legitimately contain is version, encoding, and standalone.
            // We only support version 1.0, we can only write whatever encoding we were supplied,
            // and we don't support DTDs, so whatever values are supplied in the text argument are irrelevant.
            _writer.WriteDeclaration();
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

        public override void WriteEndDocument()
        {
            if (IsClosed)
                ThrowClosed();

            if (_writeState == WriteState.Start || _writeState == WriteState.Prolog)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlNoRootElement));

            FinishDocument();
            _writeState = WriteState.Start;
            _documentState = DocumentState.End;
        }

        public override void WriteEntityRef(string name)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteEntityRef")));
        }

        public override void WriteName(string name)
        {
            if (IsClosed)
                ThrowClosed();

            WriteString(name);
        }

        public override void WriteNmToken(string name)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, "WriteNmToken")));
        }

        public override void WriteWhitespace(string whitespace)
        {
            if (IsClosed)
                ThrowClosed();

            if (whitespace == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(whitespace));

            for (int i = 0; i < whitespace.Length; ++i)
            {
                char c = whitespace[i];
                if (c != ' ' &&
                    c != '\t' &&
                    c != '\n' &&
                    c != '\r')
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlOnlyWhitespace, nameof(whitespace)));
            }

            WriteString(whitespace);
        }

        public override void WriteString(string value)
        {
            if (IsClosed)
                ThrowClosed();

            if (value == null)
                value = string.Empty;

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

        public override void WriteString(XmlDictionaryString value)
        {
            if (IsClosed)
                ThrowClosed();

            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));

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

        public override void WriteChars(char[] chars, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            if (chars == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(chars)));

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));

            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > chars.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));

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

        public override void WriteRaw(string value)
        {
            if (IsClosed)
                ThrowClosed();

            if (value == null)
                value = string.Empty;

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

            if (chars == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(chars)));

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));

            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > chars.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));

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

        public override void WriteCharEntity(char ch)
        {
            if (IsClosed)
                ThrowClosed();

            if (ch >= 0xd800 && ch <= 0xdfff)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlMissingLowSurrogate, nameof(ch)));

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
                char[] chars = new char[2] { highChar, lowChar };
                WriteAttributeText(new string(chars));
            }

            if (!_isXmlnsAttribute)
            {
                StartContent();
                FlushBase64();
                _writer.WriteCharEntity(ch.Char);
                EndContent();
            }
        }

        public override void WriteValue(object value)
        {
            if (IsClosed)
                ThrowClosed();

            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(value)));

            else if (value is object[])
            {
                WriteValue((object[])value);
            }
            else if (value is Array)
            {
                WriteValue((Array)value);
            }
            else
            {
                WritePrimitiveValue(value);
            }
        }

        protected void WritePrimitiveValue(object value)
        {
            if (IsClosed)
                ThrowClosed();

            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(value)));

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
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlNestedArraysNotSupported, nameof(value)));
            }
            else
            {
                base.WriteValue(value);
            }
        }

        public override void WriteValue(string value)
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

        public override void WriteValue(XmlDictionaryString value)
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

            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));

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

        public override void WriteBinHex(byte[] buffer, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            WriteRaw(BinHexEncoding.GetString(buffer, offset, count));
        }

        public override void WriteBase64(byte[] buffer, int offset, int count)
        {
            if (IsClosed)
                ThrowClosed();

            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(buffer)));

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));

            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));

            if (count > 0)
            {
                if (_trailByteCount > 0)
                {
                    while (_trailByteCount < 3 && count > 0)
                    {
                        _trailBytes[_trailByteCount++] = buffer[offset++];
                        count--;
                    }
                }

                int totalByteCount = _trailByteCount + count;
                int actualByteCount = totalByteCount - (totalByteCount % 3);

                if (_trailBytes == null)
                {
                    _trailBytes = new byte[3];
                }

                if (actualByteCount >= 3)
                {
                    if (_attributeValue != null)
                    {
                        WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
                        WriteAttributeText(XmlConverter.Base64Encoding.GetString(buffer, offset, actualByteCount - _trailByteCount));
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

            if (buffer == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(buffer)));

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (offset < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));

            if (count < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));

            return WriteBase64AsyncImpl(buffer, offset, count);
        }

        private async Task WriteBase64AsyncImpl(byte[] buffer, int offset, int count)
        {
            if (count > 0)
            {
                if (_trailByteCount > 0)
                {
                    while (_trailByteCount < 3 && count > 0)
                    {
                        _trailBytes[_trailByteCount++] = buffer[offset++];
                        count--;
                    }
                }

                int totalByteCount = _trailByteCount + count;
                int actualByteCount = totalByteCount - (totalByteCount % 3);

                if (_trailBytes == null)
                {
                    _trailBytes = new byte[3];
                }

                if (actualByteCount >= 3)
                {
                    if (_attributeValue != null)
                    {
                        WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
                        WriteAttributeText(XmlConverter.Base64Encoding.GetString(buffer, offset, actualByteCount - _trailByteCount));
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

        public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
        {
            if (IsClosed)
                ThrowClosed();
            if (Signing)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlCanonicalizationStarted));
            FlushElement();
            if (_signingWriter == null)
                _signingWriter = CreateSigningNodeWriter();
            _signingWriter.SetOutput(_writer, stream, includeComments, inclusivePrefixes);
            _writer = _signingWriter;
            SignScope(_signingWriter.CanonicalWriter);
        }

        public override void EndCanonicalization()
        {
            if (IsClosed)
                ThrowClosed();
            if (!Signing)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlCanonicalizationNotStarted));
            _signingWriter.Flush();
            _writer = _signingWriter.NodeWriter;
        }

        protected abstract XmlSigningNodeWriter CreateSigningNodeWriter();

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
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));

            if (!_isXmlnsAttribute)
            {
                StartContent();
                _writer.WriteBase64Text(_trailBytes, _trailByteCount, _trailBytes, 0, 0);
                EndContent();
            }
            _trailByteCount = 0;
        }

        private async Task FlushTrailBytesAsync()
        {
            if (_attributeValue != null)
                WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));

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
                WritePrimitiveValue(array.GetValue(i));
            }
            _inList = false;
            _writer.WriteEndListText();
            EndContent();
        }

        protected void StartArray(int count)
        {
            FlushBase64();
            if (_documentState == DocumentState.Epilog)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlOnlyOneRoot));
            if (_documentState == DocumentState.Document && count > 1 && _depth == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlOnlyOneRoot));
            if (_writeState == WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));
            AutoComplete(WriteState.Content);
        }

        protected void EndArray()
        {
        }

        private string GeneratePrefix(string ns, XmlDictionaryString xNs)
        {
            if (_writeState != WriteState.Element && _writeState != WriteState.Attribute)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.XmlInvalidPrefixState, WriteState.ToString())));

            string prefix = _nsMgr.AddNamespace(ns, xNs);

            if (prefix != null)
                return prefix;

            while (true)
            {
                int prefixId = _elements[_depth].PrefixId++;
                prefix = string.Concat("d", _depth.ToString(CultureInfo.InvariantCulture), "p", prefixId.ToString(CultureInfo.InvariantCulture));

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
            if (_attributeValue.Length == 0)
                _attributeValue = value;
            else
                _attributeValue += value;
        }

        private class Element
        {
            private string _prefix;
            private string _localName;
            private int _prefixId;

            public string Prefix
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

            public string LocalName
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

        private class NamespaceManager
        {
            private Namespace[] _namespaces;
            private Namespace _lastNameSpace;
            private int _nsCount;
            private int _depth;
            private XmlAttribute[] _attributes;
            private int _attributeCount;
            private XmlSpace _space;
            private string _lang;
            private int _nsTop;
            private Namespace _defaultNamespace;

            public NamespaceManager()
            {
                _defaultNamespace = new Namespace();
                _defaultNamespace.Depth = 0;
                _defaultNamespace.Prefix = string.Empty;
                _defaultNamespace.Uri = string.Empty;
                _defaultNamespace.UriDictionaryString = null;
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
                _lastNameSpace = null;
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

            public void DeclareNamespaces(XmlNodeWriter writer)
            {
                int i = _nsCount;
                while (i > 0)
                {
                    Namespace nameSpace = _namespaces[i - 1];
                    if (nameSpace.Depth != _depth)
                        break;
                    i--;
                }
                while (i < _nsCount)
                {
                    Namespace nameSpace = _namespaces[i];
                    if (nameSpace.UriDictionaryString != null)
                        writer.WriteXmlnsAttribute(nameSpace.Prefix, nameSpace.UriDictionaryString);
                    else
                        writer.WriteXmlnsAttribute(nameSpace.Prefix, nameSpace.Uri);
                    i++;
                }
            }

            public void EnterScope()
            {
                _depth++;
            }

            public void ExitScope()
            {
                while (_nsCount > 0)
                {
                    Namespace nameSpace = _namespaces[_nsCount - 1];
                    if (nameSpace.Depth != _depth)
                        break;
                    if (_lastNameSpace == nameSpace)
                        _lastNameSpace = null;
                    nameSpace.Clear();
                    _nsCount--;
                }
                while (_attributeCount > 0)
                {
                    XmlAttribute attribute = _attributes[_attributeCount - 1];
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
                    Array.Copy(_attributes, 0, newAttributes, 0, _attributeCount);
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

            public string AddNamespace(string uri, XmlDictionaryString uriDictionaryString)
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
                            Namespace nameSpace = _namespaces[j];
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

            public void AddNamespaceIfNotDeclared(string prefix, string uri, XmlDictionaryString uriDictionaryString)
            {
                if (LookupNamespace(prefix) != uri)
                {
                    AddNamespace(prefix, uri, uriDictionaryString);
                }
            }

            public void AddNamespace(string prefix, string uri, XmlDictionaryString uriDictionaryString)
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
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlReservedPrefix, nameof(prefix)));
                    }
                }
                Namespace nameSpace;
                for (int i = _nsCount - 1; i >= 0; i--)
                {
                    nameSpace = _namespaces[i];
                    if (nameSpace.Depth != _depth)
                        break;
                    if (nameSpace.Prefix == prefix)
                    {
                        if (nameSpace.Uri == uri)
                            return;
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, prefix, nameSpace.Uri, uri), nameof(prefix)));
                    }
                }
                if (prefix.Length != 0 && uri.Length == 0)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.XmlEmptyNamespaceRequiresNullPrefix, nameof(prefix)));
                if (uri.Length == xmlnsNamespace.Length && uri == xmlnsNamespace)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xmlns", uri)));
                // The addressing namespace and the xmlNamespace are the same length, so add a quick check to try to disambiguate
                if (uri.Length == xmlNamespace.Length && uri[18] == 'X' && uri == xmlNamespace)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.XmlSpecificBindingNamespace, "xml", uri)));

                if (_namespaces.Length == _nsCount)
                {
                    Namespace[] newNamespaces = new Namespace[_nsCount * 2];
                    Array.Copy(_namespaces, 0, newNamespaces, 0, _nsCount);
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

            public string LookupPrefix(string ns)
            {
                if (_lastNameSpace != null && _lastNameSpace.Uri == ns)
                    return _lastNameSpace.Prefix;
                int nsCount = _nsCount;
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces[i];
                    if (object.ReferenceEquals(nameSpace.Uri, ns))
                    {
                        string prefix = nameSpace.Prefix;
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
                    Namespace nameSpace = _namespaces[i];
                    if (nameSpace.Uri == ns)
                    {
                        string prefix = nameSpace.Prefix;
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
                        if (_namespaces[i].Prefix.Length == 0)
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

            public string LookupAttributePrefix(string ns)
            {
                if (_lastNameSpace != null && _lastNameSpace.Uri == ns && _lastNameSpace.Prefix.Length != 0)
                    return _lastNameSpace.Prefix;

                int nsCount = _nsCount;
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces[i];

                    if (object.ReferenceEquals(nameSpace.Uri, ns))
                    {
                        string prefix = nameSpace.Prefix;
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
                    Namespace nameSpace = _namespaces[i];
                    if (nameSpace.Uri == ns)
                    {
                        string prefix = nameSpace.Prefix;
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

            public string LookupNamespace(string prefix)
            {
                int nsCount = _nsCount;
                if (prefix.Length == 0)
                {
                    for (int i = nsCount - 1; i >= _nsTop; i--)
                    {
                        Namespace nameSpace = _namespaces[i];
                        if (nameSpace.Prefix.Length == 0)
                            return nameSpace.Uri;
                    }
                    return string.Empty;
                }
                if (prefix.Length == 1)
                {
                    char prefixChar = prefix[0];
                    for (int i = nsCount - 1; i >= _nsTop; i--)
                    {
                        Namespace nameSpace = _namespaces[i];
                        if (nameSpace.PrefixChar == prefixChar)
                            return nameSpace.Uri;
                    }
                    return null;
                }
                for (int i = nsCount - 1; i >= _nsTop; i--)
                {
                    Namespace nameSpace = _namespaces[i];
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
                Fx.Assert(nsCount >= 1 && _namespaces[0].Prefix.Length == 0 && _namespaces[0].Uri.Length == 0, "");
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
                        signingWriter.WriteXmlnsAttribute(nameSpace.Prefix, nameSpace.Uri);
                    }
                }
            }

            private class XmlAttribute
            {
                private XmlSpace _space;
                private string _lang;
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

                public void Clear()
                {
                    _lang = null;
                }
            }

            private class Namespace
            {
                private string _prefix;
                private string _ns;
                private XmlDictionaryString _xNs;
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

                public string Prefix
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

                public string Uri
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

                public XmlDictionaryString UriDictionaryString
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
