// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.IO;
    using System.Collections;
    using System.Text;
    using System;
    using System.Xml.Schema;
    using System.Xml;

    internal class XmlCountingReader : XmlReader, IXmlTextParser, IXmlLineInfo
    {
        private XmlReader _innerReader;
        private int _advanceCount;

        internal XmlCountingReader(XmlReader xmlReader)
        {
            if (xmlReader == null)
                throw new ArgumentNullException(nameof(xmlReader));
            _innerReader = xmlReader;
            _advanceCount = 0;
        }

        internal int AdvanceCount { get { return _advanceCount; } }

        private void IncrementCount()
        {
            if (_advanceCount == Int32.MaxValue)
                _advanceCount = 0;
            else
                _advanceCount++;
        }

        // Properties (non-advancing)
        public override XmlReaderSettings Settings { get { return _innerReader.Settings; } }
        public override XmlNodeType NodeType { get { return _innerReader.NodeType; } }
        public override string Name { get { return _innerReader.Name; } }
        public override string LocalName { get { return _innerReader.LocalName; } }
        public override string NamespaceURI { get { return _innerReader.NamespaceURI; } }
        public override string Prefix { get { return _innerReader.Prefix; } }
        public override bool HasValue { get { return _innerReader.HasValue; } }
        public override string Value { get { return _innerReader.Value; } }
        public override int Depth { get { return _innerReader.Depth; } }
        public override string BaseURI { get { return _innerReader.BaseURI; } }
        public override bool IsEmptyElement { get { return _innerReader.IsEmptyElement; } }
        public override bool IsDefault { get { return _innerReader.IsDefault; } }
        public override char QuoteChar { get { return _innerReader.QuoteChar; } }
        public override XmlSpace XmlSpace { get { return _innerReader.XmlSpace; } }
        public override string XmlLang { get { return _innerReader.XmlLang; } }
        public override IXmlSchemaInfo SchemaInfo { get { return _innerReader.SchemaInfo; } }
        public override Type ValueType { get { return _innerReader.ValueType; } }
        public override int AttributeCount { get { return _innerReader.AttributeCount; } }
        public override string this[int i] { get { return _innerReader[i]; } }
        public override string this[string name, string namespaceURI] { get { return _innerReader[name, namespaceURI]; } }
        public override bool EOF { get { return _innerReader.EOF; } }
        public override ReadState ReadState { get { return _innerReader.ReadState; } }
        public override XmlNameTable NameTable { get { return _innerReader.NameTable; } }
        public override bool CanResolveEntity { get { return _innerReader.CanResolveEntity; } }
        public override bool CanReadBinaryContent { get { return _innerReader.CanReadBinaryContent; } }
        public override bool CanReadValueChunk { get { return _innerReader.CanReadValueChunk; } }
        public override bool HasAttributes { get { return _innerReader.HasAttributes; } }

        // Methods (non-advancing)
        // Reader tends to under-count rather than over-count 
        public override void Close() { _innerReader.Close(); }
        public override string GetAttribute(string name) { return _innerReader.GetAttribute(name); }
        public override string GetAttribute(string name, string namespaceURI) { return _innerReader.GetAttribute(name, namespaceURI); }
        public override string GetAttribute(int i) { return _innerReader.GetAttribute(i); }
        public override bool MoveToAttribute(string name) { return _innerReader.MoveToAttribute(name); }
        public override bool MoveToAttribute(string name, string ns) { return _innerReader.MoveToAttribute(name, ns); }
        public override void MoveToAttribute(int i) { _innerReader.MoveToAttribute(i); }
        public override bool MoveToFirstAttribute() { return _innerReader.MoveToFirstAttribute(); }
        public override bool MoveToNextAttribute() { return _innerReader.MoveToNextAttribute(); }
        public override bool MoveToElement() { return _innerReader.MoveToElement(); }
        public override string LookupNamespace(string prefix) { return _innerReader.LookupNamespace(prefix); }
        public override bool ReadAttributeValue() { return _innerReader.ReadAttributeValue(); }
        public override void ResolveEntity() { _innerReader.ResolveEntity(); }
        public override bool IsStartElement() { return _innerReader.IsStartElement(); }
        public override bool IsStartElement(string name) { return _innerReader.IsStartElement(name); }
        public override bool IsStartElement(string localname, string ns) { return _innerReader.IsStartElement(localname, ns); }
        public override XmlReader ReadSubtree() { return _innerReader.ReadSubtree(); }
        public override XmlNodeType MoveToContent() { return _innerReader.MoveToContent(); }

        // Methods (advancing)
        public override bool Read()
        {
            IncrementCount();
            return _innerReader.Read();
        }

        public override void Skip()
        {
            IncrementCount();
            _innerReader.Skip();
        }

        public override string ReadInnerXml()
        {
            if (_innerReader.NodeType != XmlNodeType.Attribute)
                IncrementCount();
            return _innerReader.ReadInnerXml();
        }
        public override string ReadOuterXml()
        {
            if (_innerReader.NodeType != XmlNodeType.Attribute)
                IncrementCount();
            return _innerReader.ReadOuterXml();
        }
        public override object ReadContentAsObject()
        {
            IncrementCount();
            return _innerReader.ReadContentAsObject();
        }
        public override bool ReadContentAsBoolean()
        {
            IncrementCount();
            return _innerReader.ReadContentAsBoolean();
        }
        public override DateTime ReadContentAsDateTime()
        {
            IncrementCount();
            return _innerReader.ReadContentAsDateTime();
        }
        public override double ReadContentAsDouble()
        {
            IncrementCount();
            return _innerReader.ReadContentAsDouble();
        }
        public override int ReadContentAsInt()
        {
            IncrementCount();
            return _innerReader.ReadContentAsInt();
        }
        public override long ReadContentAsLong()
        {
            IncrementCount();
            return _innerReader.ReadContentAsLong();
        }
        public override string ReadContentAsString()
        {
            IncrementCount();
            return _innerReader.ReadContentAsString();
        }
        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            IncrementCount();
            return _innerReader.ReadContentAs(returnType, namespaceResolver);
        }
        public override object ReadElementContentAsObject()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsObject();
        }
        public override object ReadElementContentAsObject(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsObject(localName, namespaceURI);
        }
        public override bool ReadElementContentAsBoolean()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsBoolean();
        }
        public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsBoolean(localName, namespaceURI);
        }
        public override DateTime ReadElementContentAsDateTime()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsDateTime();
        }
        public override DateTime ReadElementContentAsDateTime(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsDateTime(localName, namespaceURI);
        }
        public override double ReadElementContentAsDouble()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsDouble();
        }
        public override double ReadElementContentAsDouble(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsDouble(localName, namespaceURI);
        }
        public override int ReadElementContentAsInt()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsInt();
        }
        public override int ReadElementContentAsInt(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsInt(localName, namespaceURI);
        }
        public override long ReadElementContentAsLong()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsLong();
        }
        public override long ReadElementContentAsLong(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsLong(localName, namespaceURI);
        }
        public override string ReadElementContentAsString()
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsString();
        }
        public override string ReadElementContentAsString(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsString(localName, namespaceURI);
        }
        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAs(returnType, namespaceResolver);
        }
        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI);
        }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            IncrementCount();
            return _innerReader.ReadContentAsBase64(buffer, index, count);
        }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsBase64(buffer, index, count);
        }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            IncrementCount();
            return _innerReader.ReadContentAsBinHex(buffer, index, count);
        }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            IncrementCount();
            return _innerReader.ReadElementContentAsBinHex(buffer, index, count);
        }
        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            IncrementCount();
            return _innerReader.ReadValueChunk(buffer, index, count);
        }
        public override string ReadString()
        {
            IncrementCount();
            return _innerReader.ReadString();
        }
        public override void ReadStartElement()
        {
            IncrementCount();
            _innerReader.ReadStartElement();
        }
        public override void ReadStartElement(string name)
        {
            IncrementCount();
            _innerReader.ReadStartElement(name);
        }
        public override void ReadStartElement(string localname, string ns)
        {
            IncrementCount();
            _innerReader.ReadStartElement(localname, ns);
        }
        public override string ReadElementString()
        {
            IncrementCount();
            return _innerReader.ReadElementString();
        }
        public override string ReadElementString(string name)
        {
            IncrementCount();
            return _innerReader.ReadElementString(name);
        }
        public override string ReadElementString(string localname, string ns)
        {
            IncrementCount();
            return _innerReader.ReadElementString(localname, ns);
        }
        public override void ReadEndElement()
        {
            IncrementCount();
            _innerReader.ReadEndElement();
        }
        public override bool ReadToFollowing(string name)
        {
            IncrementCount();
            return ReadToFollowing(name);
        }
        public override bool ReadToFollowing(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadToFollowing(localName, namespaceURI);
        }
        public override bool ReadToDescendant(string name)
        {
            IncrementCount();
            return _innerReader.ReadToDescendant(name);
        }
        public override bool ReadToDescendant(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadToDescendant(localName, namespaceURI);
        }
        public override bool ReadToNextSibling(string name)
        {
            IncrementCount();
            return _innerReader.ReadToNextSibling(name);
        }
        public override bool ReadToNextSibling(string localName, string namespaceURI)
        {
            IncrementCount();
            return _innerReader.ReadToNextSibling(localName, namespaceURI);
        }

        // IDisposable interface
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    IDisposable disposableReader = _innerReader as IDisposable;
                    if (disposableReader != null)
                        disposableReader.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // IXmlTextParser members
        bool IXmlTextParser.Normalized
        {
            get
            {
                XmlTextReader xmlTextReader = _innerReader as XmlTextReader;
                if (xmlTextReader == null)
                {
                    IXmlTextParser xmlTextParser = _innerReader as IXmlTextParser;
                    return (xmlTextParser == null) ? false : xmlTextParser.Normalized;
                }
                else
                    return xmlTextReader.Normalization;
            }
            set
            {
                XmlTextReader xmlTextReader = _innerReader as XmlTextReader;
                if (xmlTextReader == null)
                {
                    IXmlTextParser xmlTextParser = _innerReader as IXmlTextParser;
                    if (xmlTextParser != null)
                        xmlTextParser.Normalized = value;
                }
                else
                    xmlTextReader.Normalization = value;
            }
        }

        WhitespaceHandling IXmlTextParser.WhitespaceHandling
        {
            get
            {
                XmlTextReader xmlTextReader = _innerReader as XmlTextReader;
                if (xmlTextReader == null)
                {
                    IXmlTextParser xmlTextParser = _innerReader as IXmlTextParser;
                    return (xmlTextParser == null) ? WhitespaceHandling.None : xmlTextParser.WhitespaceHandling;
                }
                else
                    return xmlTextReader.WhitespaceHandling;
            }
            set
            {
                XmlTextReader xmlTextReader = _innerReader as XmlTextReader;
                if (xmlTextReader == null)
                {
                    IXmlTextParser xmlTextParser = _innerReader as IXmlTextParser;
                    if (xmlTextParser != null)
                        xmlTextParser.WhitespaceHandling = value;
                }
                else
                    xmlTextReader.WhitespaceHandling = value;
            }
        }

        // IXmlLineInfo members
        bool IXmlLineInfo.HasLineInfo()
        {
            IXmlLineInfo iXmlLineInfo = _innerReader as IXmlLineInfo;
            return (iXmlLineInfo == null) ? false : iXmlLineInfo.HasLineInfo();
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                IXmlLineInfo iXmlLineInfo = _innerReader as IXmlLineInfo;
                return (iXmlLineInfo == null) ? 0 : iXmlLineInfo.LineNumber;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                IXmlLineInfo iXmlLineInfo = _innerReader as IXmlLineInfo;
                return (iXmlLineInfo == null) ? 0 : iXmlLineInfo.LinePosition;
            }
        }
    }
}
