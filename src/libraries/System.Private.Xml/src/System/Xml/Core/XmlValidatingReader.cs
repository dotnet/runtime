// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml.Schema;

namespace System.Xml
{
    [Obsolete("XmlValidatingReader has been deprecated. Use XmlReader created by XmlReader.Create() method using appropriate XmlReaderSettings instead.")]
    public class XmlValidatingReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        //
        // Member fields
        //
        private readonly XmlValidatingReaderImpl _impl;

        //
        // Constructors
        //
        public XmlValidatingReader(XmlReader reader)
        {
            _impl = new XmlValidatingReaderImpl(reader);
            _impl.OuterReader = this;
        }

        public XmlValidatingReader([StringSyntax(StringSyntaxAttribute.Xml)] string xmlFragment, XmlNodeType fragType, XmlParserContext context)
        {
            ArgumentNullException.ThrowIfNull(xmlFragment);

            _impl = new XmlValidatingReaderImpl(xmlFragment, fragType, context);
            _impl.OuterReader = this;
        }

        public XmlValidatingReader(Stream xmlFragment, XmlNodeType fragType, XmlParserContext context)
        {
            ArgumentNullException.ThrowIfNull(xmlFragment);

            _impl = new XmlValidatingReaderImpl(xmlFragment, fragType, context);
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

        public override string? GetAttribute(string name)
        {
            return _impl.GetAttribute(name);
        }

        public override string? GetAttribute(string localName, string? namespaceURI)
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

        public override bool MoveToAttribute(string localName, string? namespaceURI)
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

        public override XmlNameTable NameTable
        {
            get { return _impl.NameTable; }
        }

        public override string? LookupNamespace(string prefix)
        {
            string? ns = _impl.LookupNamespace(prefix);
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

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _impl.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _impl.LookupPrefix(namespaceName);
        }

        //
        // XmlValidatingReader
        //
        public event ValidationEventHandler ValidationEventHandler
        {
            add { _impl.ValidationEventHandler += value; }
            remove { _impl.ValidationEventHandler -= value; }
        }

        public object? SchemaType
        {
            get { return _impl.SchemaType; }
        }

        public XmlReader Reader
        {
            get { return _impl.Reader; }
        }

        public ValidationType ValidationType
        {
            get { return _impl.ValidationType; }
            set { _impl.ValidationType = value; }
        }

        public XmlSchemaCollection Schemas
        {
            get { return _impl.Schemas; }
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

        public bool Namespaces
        {
            get { return _impl.Namespaces; }
            set { _impl.Namespaces = value; }
        }

        public object? ReadTypedValue()
        {
            return _impl.ReadTypedValue();
        }

        public Encoding? Encoding
        {
            get { return _impl.Encoding; }
        }
        //
        // Internal helper methods
        //
        internal XmlValidatingReaderImpl Impl
        {
            get { return _impl; }
        }

        internal override IDtdInfo? DtdInfo
        {
            get { return _impl.DtdInfo; }
        }
    }
}
