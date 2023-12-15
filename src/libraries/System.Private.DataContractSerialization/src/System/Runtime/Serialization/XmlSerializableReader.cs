// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace System.Runtime.Serialization
{
    internal sealed class XmlSerializableReader : XmlReader, IXmlLineInfo, IXmlTextParser // IXmlTextParser (Normalized, WhitespaceHandling) was added. Is it ever used?
    {
        private XmlReaderDelegator _xmlReader = null!; // initialized in BeginRead
        private int _startDepth;
        private bool _isRootEmptyElement;
        private XmlReader _innerReader = null!; // initialized in BeginRead

        private XmlReader InnerReader
        {
            get { return _innerReader; }
        }

        internal void BeginRead(XmlReaderDelegator xmlReader)
        {
            if (xmlReader.NodeType != XmlNodeType.Element)
                throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, xmlReader);
            _xmlReader = xmlReader;
            _startDepth = xmlReader.Depth;
            _innerReader = xmlReader.UnderlyingReader;
            _isRootEmptyElement = InnerReader.IsEmptyElement;
        }

        internal void EndRead()
        {
            if (_isRootEmptyElement)
                _xmlReader.Read();
            else
            {
                if (_xmlReader.IsStartElement() && _xmlReader.Depth == _startDepth)
                    _xmlReader.Read();
                while (_xmlReader.Depth > _startDepth)
                {
                    if (!_xmlReader.Read())
                        throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.EndElement, _xmlReader);
                }
            }
        }

        public override bool Read()
        {
            XmlReader reader = this.InnerReader;
            if (reader.Depth == _startDepth)
            {
                if (reader.NodeType == XmlNodeType.EndElement ||
                     (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement))
                {
                    return false;
                }
            }
            return reader.Read();
        }

        public override void Close()
        {
            throw XmlObjectSerializer.CreateSerializationException(SR.IXmlSerializableIllegalOperation);
        }

        public override XmlReaderSettings? Settings { get { return InnerReader.Settings; } }
        public override XmlNodeType NodeType { get { return InnerReader.NodeType; } }
        public override string Name { get { return InnerReader.Name; } }
        public override string LocalName { get { return InnerReader.LocalName; } }
        public override string NamespaceURI { get { return InnerReader.NamespaceURI; } }
        public override string Prefix { get { return InnerReader.Prefix; } }
        public override bool HasValue { get { return InnerReader.HasValue; } }
        public override string Value { get { return InnerReader.Value; } }
        public override int Depth { get { return InnerReader.Depth; } }
        public override string BaseURI { get { return InnerReader.BaseURI; } }
        public override bool IsEmptyElement { get { return InnerReader.IsEmptyElement; } }
        public override bool IsDefault { get { return InnerReader.IsDefault; } }
        public override char QuoteChar { get { return InnerReader.QuoteChar; } }
        public override XmlSpace XmlSpace { get { return InnerReader.XmlSpace; } }
        public override string XmlLang { get { return InnerReader.XmlLang; } }
        public override IXmlSchemaInfo? SchemaInfo { get { return InnerReader.SchemaInfo; } }
        public override Type ValueType { get { return InnerReader.ValueType; } }
        public override int AttributeCount { get { return InnerReader.AttributeCount; } }
        public override string this[int i] { get { return InnerReader[i]; } }
        public override string? this[string name] { get { return InnerReader[name]; } }
        public override string? this[string name, string? namespaceURI] { get { return InnerReader[name, namespaceURI]; } }
        public override bool EOF { get { return InnerReader.EOF; } }
        public override ReadState ReadState { get { return InnerReader.ReadState; } }
        public override XmlNameTable NameTable { get { return InnerReader.NameTable; } }
        public override bool CanResolveEntity { get { return InnerReader.CanResolveEntity; } }
        public override bool CanReadBinaryContent { get { return InnerReader.CanReadBinaryContent; } }
        public override bool CanReadValueChunk { get { return InnerReader.CanReadValueChunk; } }
        public override bool HasAttributes { get { return InnerReader.HasAttributes; } }

        public override string? GetAttribute(string name) { return InnerReader.GetAttribute(name); }
        public override string? GetAttribute(string name, string? namespaceURI) { return InnerReader.GetAttribute(name, namespaceURI); }
        public override string GetAttribute(int i) { return InnerReader.GetAttribute(i); }
        public override bool MoveToAttribute(string name) { return InnerReader.MoveToAttribute(name); }
        public override bool MoveToAttribute(string name, string? ns) { return InnerReader.MoveToAttribute(name, ns); }
        public override void MoveToAttribute(int i) { InnerReader.MoveToAttribute(i); }
        public override bool MoveToFirstAttribute() { return InnerReader.MoveToFirstAttribute(); }
        public override bool MoveToNextAttribute() { return InnerReader.MoveToNextAttribute(); }
        public override bool MoveToElement() { return InnerReader.MoveToElement(); }
        public override string? LookupNamespace(string prefix) { return InnerReader.LookupNamespace(prefix); }
        public override bool ReadAttributeValue() { return InnerReader.ReadAttributeValue(); }
        public override void ResolveEntity() { InnerReader.ResolveEntity(); }
        public override bool IsStartElement() { return InnerReader.IsStartElement(); }
        public override bool IsStartElement(string name) { return InnerReader.IsStartElement(name); }
        public override bool IsStartElement(string localname, string ns) { return InnerReader.IsStartElement(localname, ns); }
        public override XmlNodeType MoveToContent() { return InnerReader.MoveToContent(); }

        public override object ReadContentAsObject() { return InnerReader.ReadContentAsObject(); }
        public override bool ReadContentAsBoolean() { return InnerReader.ReadContentAsBoolean(); }
        public override DateTime ReadContentAsDateTime() { return InnerReader.ReadContentAsDateTimeOffset().DateTime; }
        public override double ReadContentAsDouble() { return InnerReader.ReadContentAsDouble(); }
        public override int ReadContentAsInt() { return InnerReader.ReadContentAsInt(); }
        public override long ReadContentAsLong() { return InnerReader.ReadContentAsLong(); }
        public override string ReadContentAsString() { return InnerReader.ReadContentAsString(); }
        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver) { return InnerReader.ReadContentAs(returnType, namespaceResolver); }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { return InnerReader.ReadContentAsBase64(buffer, index, count); }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { return InnerReader.ReadContentAsBinHex(buffer, index, count); }
        public override int ReadValueChunk(char[] buffer, int index, int count) { return InnerReader.ReadValueChunk(buffer, index, count); }
        public override string ReadString() { return InnerReader.ReadString(); }

        // IXmlTextParser members
        bool IXmlTextParser.Normalized
        {
            get
            {
                IXmlTextParser? xmlTextParser = InnerReader as IXmlTextParser;
                return (xmlTextParser == null) ? _xmlReader.Normalized : xmlTextParser.Normalized;
            }
            set
            {
                if (InnerReader is not IXmlTextParser xmlTextParser)
                    _xmlReader.Normalized = value;
                else
                    xmlTextParser.Normalized = value;
            }
        }

        WhitespaceHandling IXmlTextParser.WhitespaceHandling
        {
            get
            {
                IXmlTextParser? xmlTextParser = InnerReader as IXmlTextParser;
                return (xmlTextParser == null) ? _xmlReader.WhitespaceHandling : xmlTextParser.WhitespaceHandling;
            }
            set
            {
                if (InnerReader is not IXmlTextParser xmlTextParser)
                    _xmlReader.WhitespaceHandling = value;
                else
                    xmlTextParser.WhitespaceHandling = value;
            }
        }

        // IXmlLineInfo members
        bool IXmlLineInfo.HasLineInfo()
        {
            IXmlLineInfo? xmlLineInfo = InnerReader as IXmlLineInfo;
            return (xmlLineInfo == null) ? _xmlReader.HasLineInfo() : xmlLineInfo.HasLineInfo();
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                IXmlLineInfo? xmlLineInfo = InnerReader as IXmlLineInfo;
                return (xmlLineInfo == null) ? _xmlReader.LineNumber : xmlLineInfo.LineNumber;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                IXmlLineInfo? xmlLineInfo = InnerReader as IXmlLineInfo;
                return (xmlLineInfo == null) ? _xmlReader.LinePosition : xmlLineInfo.LinePosition;
            }
        }
    }
}
