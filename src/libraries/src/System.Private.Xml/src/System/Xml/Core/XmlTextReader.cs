// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Xml
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class XmlTextReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        //
        // Member fields
        //
        private XmlTextReaderImpl _impl;
        //
        //
        // Constructors
        //
        protected XmlTextReader()
        {
            _impl = new XmlTextReaderImpl();
            _impl.OuterReader = this;
        }

        protected XmlTextReader(XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(nt);
            _impl.OuterReader = this;
        }

        public XmlTextReader(Stream input)
        {
            _impl = new XmlTextReaderImpl(input);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url, Stream input)
        {
            _impl = new XmlTextReaderImpl(url, input);
            _impl.OuterReader = this;
        }

        public XmlTextReader(Stream input, XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(input, nt);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url, Stream input, XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(url, input, nt);
            _impl.OuterReader = this;
        }

        public XmlTextReader(TextReader input)
        {
            _impl = new XmlTextReaderImpl(input);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url, TextReader input)
        {
            _impl = new XmlTextReaderImpl(url, input);
            _impl.OuterReader = this;
        }

        public XmlTextReader(TextReader input, XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(input, nt);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url, TextReader input, XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(url, input, nt);
            _impl.OuterReader = this;
        }

        public XmlTextReader(Stream xmlFragment, XmlNodeType fragType, XmlParserContext context)
        {
            _impl = new XmlTextReaderImpl(xmlFragment, fragType, context);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string xmlFragment, XmlNodeType fragType, XmlParserContext context)
        {
            _impl = new XmlTextReaderImpl(xmlFragment, fragType, context);
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url)
        {
            _impl = new XmlTextReaderImpl(url, new NameTable());
            _impl.OuterReader = this;
        }

        public XmlTextReader(string url, XmlNameTable nt)
        {
            _impl = new XmlTextReaderImpl(url, nt);
            _impl.OuterReader = this;
        }
        //
        // XmlReader members
        //
        public override XmlNodeType NodeType
        {
            get { return _impl.NodeType; }
        }

        public override string Name
        {
            get { return _impl.Name; }
        }

        public override string LocalName
        {
            get { return _impl.LocalName; }
        }

        public override string NamespaceURI
        {
            get { return _impl.NamespaceURI; }
        }

        public override string Prefix
        {
            get { return _impl.Prefix; }
        }

        public override bool HasValue
        {
            get { return _impl.HasValue; }
        }

        public override string Value
        {
            get { return _impl.Value; }
        }

        public override int Depth
        {
            get { return _impl.Depth; }
        }

        public override string BaseURI
        {
            get { return _impl.BaseURI; }
        }

        public override bool IsEmptyElement
        {
            get { return _impl.IsEmptyElement; }
        }

        public override bool IsDefault
        {
            get { return _impl.IsDefault; }
        }

        public override char QuoteChar
        {
            get { return _impl.QuoteChar; }
        }

        public override XmlSpace XmlSpace
        {
            get { return _impl.XmlSpace; }
        }

        public override string XmlLang
        {
            get { return _impl.XmlLang; }
        }

        // XmlTextReader does not override SchemaInfo, ValueType and ReadTypeValue

        public override int AttributeCount { get { return _impl.AttributeCount; } }

        public override string GetAttribute(string name)
        {
            return _impl.GetAttribute(name);
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            return _impl.GetAttribute(localName, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            return _impl.GetAttribute(i);
        }

        public override bool MoveToAttribute(string name)
        {
            return _impl.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            return _impl.MoveToAttribute(localName, namespaceURI);
        }

        public override void MoveToAttribute(int i)
        {
            _impl.MoveToAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            return _impl.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return _impl.MoveToNextAttribute();
        }

        public override bool MoveToElement()
        {
            return _impl.MoveToElement();
        }

        public override bool ReadAttributeValue()
        {
            return _impl.ReadAttributeValue();
        }

        public override bool Read()
        {
            return _impl.Read();
        }

        public override bool EOF
        {
            get { return _impl.EOF; }
        }

        public override void Close()
        {
            _impl.Close();
        }

        public override ReadState ReadState
        {
            get { return _impl.ReadState; }
        }

        public override void Skip()
        {
            _impl.Skip();
        }

        public override XmlNameTable NameTable
        {
            get { return _impl.NameTable; }
        }

        public override string LookupNamespace(string prefix)
        {
            string ns = _impl.LookupNamespace(prefix);
            if (ns != null && ns.Length == 0)
            {
                ns = null;
            }
            return ns;
        }

        public override bool CanResolveEntity
        {
            get { return true; }
        }

        public override void ResolveEntity()
        {
            _impl.ResolveEntity();
        }

        // Binary content access methods
        public override bool CanReadBinaryContent
        {
            get { return true; }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            return _impl.ReadContentAsBase64(buffer, index, count);
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            return _impl.ReadElementContentAsBase64(buffer, index, count);
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            return _impl.ReadContentAsBinHex(buffer, index, count);
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            return _impl.ReadElementContentAsBinHex(buffer, index, count);
        }

        // Text streaming methods

        // XmlTextReader does do support streaming of Value (there are backwards compatibility issues when enabled)
        public override bool CanReadValueChunk
        {
            get { return false; }
        }

        // Overriden helper methods

        public override string ReadString()
        {
            _impl.MoveOffEntityReference();
            return base.ReadString();
        }

        //
        // IXmlLineInfo members
        //
        public bool HasLineInfo() { return true; }

        public int LineNumber { get { return _impl.LineNumber; } }

        public int LinePosition { get { return _impl.LinePosition; } }

        //
        // IXmlNamespaceResolver members
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _impl.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _impl.LookupNamespace(prefix);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _impl.LookupPrefix(namespaceName);
        }

        // This pragma disables a warning that the return type is not CLS-compliant, but generics are part of CLS in Whidbey. 
#pragma warning disable 3002
        // FXCOP: ExplicitMethodImplementationsInUnsealedClassesHaveVisibleAlternates
        // public versions of IXmlNamespaceResolver methods, so that XmlTextReader subclasses can access them
        public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _impl.GetNamespacesInScope(scope);
        }
#pragma warning restore 3002

        //
        // XmlTextReader 
        //
        public bool Namespaces
        {
            get { return _impl.Namespaces; }
            set { _impl.Namespaces = value; }
        }

        public bool Normalization
        {
            get { return _impl.Normalization; }
            set { _impl.Normalization = value; }
        }

        public Encoding Encoding
        {
            get { return _impl.Encoding; }
        }

        public WhitespaceHandling WhitespaceHandling
        {
            get { return _impl.WhitespaceHandling; }
            set { _impl.WhitespaceHandling = value; }
        }

        [Obsolete("Use DtdProcessing property instead.")]
        public bool ProhibitDtd
        {
            get { return _impl.DtdProcessing == DtdProcessing.Prohibit; }
            set { _impl.DtdProcessing = value ? DtdProcessing.Prohibit : DtdProcessing.Parse; }
        }

        public DtdProcessing DtdProcessing
        {
            get { return _impl.DtdProcessing; }
            set { _impl.DtdProcessing = value; }
        }

        public EntityHandling EntityHandling
        {
            get { return _impl.EntityHandling; }
            set { _impl.EntityHandling = value; }
        }

        public XmlResolver XmlResolver
        {
            set { _impl.XmlResolver = value; }
        }

        public void ResetState()
        {
            _impl.ResetState();
        }

        public TextReader GetRemainder()
        {
            return _impl.GetRemainder();
        }

        public int ReadChars(char[] buffer, int index, int count)
        {
            return _impl.ReadChars(buffer, index, count);
        }

        public int ReadBase64(byte[] array, int offset, int len)
        {
            return _impl.ReadBase64(array, offset, len);
        }

        public int ReadBinHex(byte[] array, int offset, int len)
        {
            return _impl.ReadBinHex(array, offset, len);
        }
        //
        // Internal helper methods
        //
        internal XmlTextReaderImpl Impl
        {
            get { return _impl; }
        }

        internal override XmlNamespaceManager NamespaceManager
        {
            get { return _impl.NamespaceManager; }
        }

        // NOTE: System.Data.SqlXml.XmlDataSourceResolver accesses this property via reflection
        internal bool XmlValidatingReaderCompatibilityMode
        {
            set { _impl.XmlValidatingReaderCompatibilityMode = value; }
        }

        internal override IDtdInfo DtdInfo
        {
            get { return _impl.DtdInfo; }
        }
    }
}

