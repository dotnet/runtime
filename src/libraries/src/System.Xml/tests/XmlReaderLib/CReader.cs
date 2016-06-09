// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class XmlCustomReader : XmlReader
    {
        private bool _iscalled;
        public bool IsCalled
        {
            get { return _iscalled; }
            set { _iscalled = value; }
        }

        private XmlReader _wrappedreader = null;

        public XmlCustomReader(string filename, XmlReaderSettings settings)
        {
            XmlReader w = ReaderHelper.Create(filename, settings);
            XmlReaderSettings wsettings = new XmlReaderSettings();
            wsettings.CheckCharacters = true;
            wsettings.DtdProcessing = DtdProcessing.Ignore;
            wsettings.ConformanceLevel = ConformanceLevel.Auto;
            _wrappedreader = ReaderHelper.Create(w, wsettings);
        }

        public XmlCustomReader(Stream stream, XmlReaderSettings settings, string baseUri)
        {
            XmlReader w = ReaderHelper.Create(stream, settings, baseUri);
            XmlReaderSettings wsettings = new XmlReaderSettings();
            wsettings.CheckCharacters = true;
            wsettings.DtdProcessing = DtdProcessing.Ignore;
            wsettings.ConformanceLevel = ConformanceLevel.Auto;
            _wrappedreader = ReaderHelper.Create(w, wsettings);
        }

        public XmlCustomReader(XmlReader reader, XmlReaderSettings settings)
        {
            XmlReader w = ReaderHelper.Create(reader, settings);
            XmlReaderSettings wsettings = new XmlReaderSettings();
            wsettings.CheckCharacters = true;
            wsettings.DtdProcessing = DtdProcessing.Ignore;
            wsettings.ConformanceLevel = ConformanceLevel.Auto;
            _wrappedreader = ReaderHelper.Create(w, wsettings);
        }

        public XmlCustomReader(TextReader textReader, XmlReaderSettings settings, string baseUri)
        {
            XmlReader w = ReaderHelper.Create(textReader, settings, baseUri);
            XmlReaderSettings wsettings = new XmlReaderSettings();
            wsettings.CheckCharacters = true;
            wsettings.DtdProcessing = DtdProcessing.Ignore;
            wsettings.ConformanceLevel = ConformanceLevel.Auto;
            _wrappedreader = ReaderHelper.Create(w, wsettings);
        }

        public override XmlReaderSettings Settings { get { this.IsCalled = true; return _wrappedreader.Settings; } }

        //  Node Properties 


        public override XmlNodeType NodeType { get { this.IsCalled = true; return _wrappedreader.NodeType; } }
        public override String Name { get { this.IsCalled = true; return _wrappedreader.Name; } }
        public override string LocalName { get { this.IsCalled = true; return _wrappedreader.LocalName; } }
        public override string NamespaceURI { get { this.IsCalled = true; return _wrappedreader.NamespaceURI; } }
        public override string Prefix { get { this.IsCalled = true; return _wrappedreader.Prefix; } }
        public override bool HasValue { get { this.IsCalled = true; return _wrappedreader.HasValue; } }
        public override string Value { get { this.IsCalled = true; return _wrappedreader.Value; } }
        public override int Depth { get { this.IsCalled = true; return _wrappedreader.Depth; } }
        public override string BaseURI { get { this.IsCalled = true; return _wrappedreader.BaseURI; } }
        public override bool IsEmptyElement { get { this.IsCalled = true; return _wrappedreader.IsEmptyElement; } }
        public override bool IsDefault { get { this.IsCalled = true; return _wrappedreader.IsDefault; } }
        public override XmlSpace XmlSpace { get { this.IsCalled = true; return _wrappedreader.XmlSpace; } }
        public override string XmlLang { get { this.IsCalled = true; return _wrappedreader.XmlLang; } }

        //  Reading Typed Content Methods 
        public override System.Type ValueType { get { this.IsCalled = true; return _wrappedreader.ValueType; } }
        public override object ReadContentAsObject() { this.IsCalled = true; return _wrappedreader.ReadContentAsObject(); }
        public override bool ReadContentAsBoolean() { this.IsCalled = true; return _wrappedreader.ReadContentAsBoolean(); }
        public override DateTimeOffset ReadContentAsDateTimeOffset() { this.IsCalled = true; return _wrappedreader.ReadContentAsDateTimeOffset(); }
        public override double ReadContentAsDouble() { this.IsCalled = true; return _wrappedreader.ReadContentAsDouble(); }
        public override float ReadContentAsFloat() { this.IsCalled = true; return _wrappedreader.ReadContentAsFloat(); }
        public override decimal ReadContentAsDecimal() { this.IsCalled = true; return _wrappedreader.ReadContentAsDecimal(); }
        public override long ReadContentAsLong() { this.IsCalled = true; return _wrappedreader.ReadContentAsLong(); }
        public override int ReadContentAsInt() { this.IsCalled = true; return _wrappedreader.ReadContentAsInt(); }
        public override string ReadContentAsString() { this.IsCalled = true; return _wrappedreader.ReadContentAsString(); }
        public override object ReadContentAs(System.Type returnType, IXmlNamespaceResolver namespaceResolver)
        { this.IsCalled = true; return _wrappedreader.ReadContentAs(returnType, namespaceResolver); }

        public override System.Object ReadElementContentAsObject()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsObject(); }

        public override System.Object ReadElementContentAsObject(string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsObject(localName, NamespaceURI); }
        public override bool ReadElementContentAsBoolean()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsBoolean(); }
        public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsBoolean(localName, namespaceURI); }
        public override decimal ReadElementContentAsDecimal()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsDecimal(); }
        public override decimal ReadElementContentAsDecimal(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsDecimal(localname, namespaceURI); }
        public override float ReadElementContentAsFloat()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsFloat(); }
        public override float ReadElementContentAsFloat(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsFloat(localname, namespaceURI); }
        public override double ReadElementContentAsDouble()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsDouble(); }
        public override double ReadElementContentAsDouble(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsDouble(localname, namespaceURI); }
        public override long ReadElementContentAsLong()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsLong(); }
        public override long ReadElementContentAsLong(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsLong(localname, namespaceURI); }
        public override int ReadElementContentAsInt()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsInt(); }
        public override int ReadElementContentAsInt(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsInt(localname, namespaceURI); }
        public override string ReadElementContentAsString()
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsString(); }
        public override string ReadElementContentAsString(string localname, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsString(localname, namespaceURI); }
        public override object ReadElementContentAs(System.Type returnType, IXmlNamespaceResolver namespaceResolver)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAs(returnType, namespaceResolver); }
        public override object ReadElementContentAs(System.Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI); }

        public override bool CanReadValueChunk { get { this.IsCalled = true; return _wrappedreader.CanReadValueChunk; } }
        public override int ReadValueChunk(Char[] buffer, int startIndex, int count)
        { this.IsCalled = true; return _wrappedreader.ReadValueChunk(buffer, startIndex, count); }

        public override bool CanReadBinaryContent { get { this.IsCalled = true; return _wrappedreader.CanReadBinaryContent; } }
        public override int ReadContentAsBase64(byte[] buffer, int startIndex, int count)
        { this.IsCalled = true; return _wrappedreader.ReadContentAsBase64(buffer, startIndex, count); }
        public override int ReadContentAsBinHex(byte[] buffer, int startIndex, int count)
        { this.IsCalled = true; return _wrappedreader.ReadContentAsBinHex(buffer, startIndex, count); }
        public override int ReadElementContentAsBase64(byte[] buffer, int startIndex, int count)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsBase64(buffer, startIndex, count); }
        public override int ReadElementContentAsBinHex(byte[] buffer, int startIndex, int count)
        { this.IsCalled = true; return _wrappedreader.ReadElementContentAsBinHex(buffer, startIndex, count); }

        //  Attribute Accessors 
        public override int AttributeCount { get { this.IsCalled = true; return _wrappedreader.AttributeCount; } }
        public override bool HasAttributes { get { this.IsCalled = true; return _wrappedreader.HasAttributes; } }
        public override string GetAttribute(string name)
        { this.IsCalled = true; return _wrappedreader.GetAttribute(name); }
        public override string GetAttribute(string name, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.GetAttribute(name, namespaceURI); }
        public override string GetAttribute(int i)
        { this.IsCalled = true; return _wrappedreader.GetAttribute(i); }
        public override bool MoveToAttribute(string name)
        { this.IsCalled = true; return _wrappedreader.MoveToAttribute(name); }
        public override bool MoveToAttribute(string name, string ns)
        { this.IsCalled = true; return _wrappedreader.MoveToAttribute(name, ns); }
        public override void MoveToAttribute(int i)
        { this.IsCalled = true; _wrappedreader.MoveToAttribute(i); }
        public override bool MoveToFirstAttribute()
        { this.IsCalled = true; return _wrappedreader.MoveToFirstAttribute(); }
        public override bool MoveToNextAttribute()
        { this.IsCalled = true; return _wrappedreader.MoveToNextAttribute(); }
        public override bool MoveToElement()
        { this.IsCalled = true; return _wrappedreader.MoveToElement(); }

        //  Moving through the Stream 
        public override bool Read()
        { this.IsCalled = true; return _wrappedreader.Read(); }
        public override bool EOF { get { this.IsCalled = true; return _wrappedreader.EOF; } }
        public override ReadState ReadState { get { this.IsCalled = true; return _wrappedreader.ReadState; } }
        public override void Skip()
        { this.IsCalled = true; _wrappedreader.Skip(); }

        public override string ReadInnerXml()
        { this.IsCalled = true; return _wrappedreader.ReadInnerXml(); }
        public override string ReadOuterXml()
        { this.IsCalled = true; return _wrappedreader.ReadOuterXml(); }

        //  Helper Methods 
        public override XmlReader ReadSubtree()
        { this.IsCalled = true; return _wrappedreader.ReadSubtree(); }
        public override XmlNodeType MoveToContent()
        { this.IsCalled = true; return _wrappedreader.MoveToContent(); }
        public override bool IsStartElement()
        { this.IsCalled = true; return _wrappedreader.IsStartElement(); }
        public override bool IsStartElement(string localname)
        { this.IsCalled = true; return _wrappedreader.IsStartElement(localname); }
        public override bool IsStartElement(string name, string ns)
        { this.IsCalled = true; return _wrappedreader.IsStartElement(name, ns); }
        public override void ReadStartElement()
        { this.IsCalled = true; _wrappedreader.ReadStartElement(); }
        public override void ReadStartElement(string name)
        { this.IsCalled = true; _wrappedreader.ReadStartElement(name); }
        public override void ReadStartElement(string localname, string ns)
        { this.IsCalled = true; _wrappedreader.ReadStartElement(localname, ns); }
        public override void ReadEndElement()
        { this.IsCalled = true; _wrappedreader.ReadEndElement(); }
        public override bool ReadToFollowing(string name)
        { this.IsCalled = true; return _wrappedreader.ReadToFollowing(name); }
        public override bool ReadToFollowing(string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadToFollowing(localName, namespaceURI); }
        public override bool ReadToDescendant(string name)
        { this.IsCalled = true; return _wrappedreader.ReadToDescendant(name); }
        public override bool ReadToDescendant(string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadToDescendant(localName, namespaceURI); }
        public override bool ReadToNextSibling(string name)
        { this.IsCalled = true; return _wrappedreader.ReadToNextSibling(name); }
        public override bool ReadToNextSibling(string localName, string namespaceURI)
        { this.IsCalled = true; return _wrappedreader.ReadToNextSibling(localName, namespaceURI); }

        //  Nametable and Namespace Helpers 
        public override XmlNameTable NameTable { get { this.IsCalled = true; return _wrappedreader.NameTable; } }
        public override string LookupNamespace(string prefix)
        { this.IsCalled = true; return _wrappedreader.LookupNamespace(prefix); }

        //  Entity Handling 
        public override bool ReadAttributeValue()
        { this.IsCalled = true; return _wrappedreader.ReadAttributeValue(); }
        public override void ResolveEntity()
        { this.IsCalled = true; _wrappedreader.ResolveEntity(); }
        public override bool CanResolveEntity { get { this.IsCalled = true; return _wrappedreader.CanResolveEntity; } }
    }
}
