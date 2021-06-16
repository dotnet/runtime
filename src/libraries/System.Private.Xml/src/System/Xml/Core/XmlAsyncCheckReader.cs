// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml
{
    internal class XmlAsyncCheckReader : XmlReader
    {
        private readonly XmlReader _coreReader;
        private Task _lastTask = Task.CompletedTask;

        internal XmlReader CoreReader
        {
            get
            {
                return _coreReader;
            }
        }

        public static XmlAsyncCheckReader CreateAsyncCheckWrapper(XmlReader reader)
        {
            if (reader is IXmlLineInfo)
            {
                if (reader is IXmlNamespaceResolver)
                {
                    if (reader is IXmlSchemaInfo)
                    {
                        return new XmlAsyncCheckReaderWithLineInfoNSSchema(reader);
                    }
                    return new XmlAsyncCheckReaderWithLineInfoNS(reader);
                }
                Debug.Assert(!(reader is IXmlSchemaInfo));
                return new XmlAsyncCheckReaderWithLineInfo(reader);
            }
            else if (reader is IXmlNamespaceResolver)
            {
                Debug.Assert(!(reader is IXmlSchemaInfo));
                return new XmlAsyncCheckReaderWithNS(reader);
            }
            Debug.Assert(!(reader is IXmlSchemaInfo));
            return new XmlAsyncCheckReader(reader);
        }

        public XmlAsyncCheckReader(XmlReader reader)
        {
            _coreReader = reader;
        }

        private void CheckAsync()
        {
            if (!_lastTask.IsCompleted)
            {
                throw new InvalidOperationException(SR.Xml_AsyncIsRunningException);
            }
        }

        #region Sync Methods, Properties Check

        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings? settings = _coreReader.Settings;
                if (null != settings)
                {
                    settings = settings.Clone();
                }
                else
                {
                    settings = new XmlReaderSettings();
                }
                settings.Async = true;
                settings.ReadOnly = true;
                return settings;
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                CheckAsync();
                return _coreReader.NodeType;
            }
        }

        public override string Name
        {
            get
            {
                CheckAsync();
                return _coreReader.Name;
            }
        }

        public override string LocalName
        {
            get
            {
                CheckAsync();
                return _coreReader.LocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                CheckAsync();
                return _coreReader.NamespaceURI;
            }
        }

        public override string Prefix
        {
            get
            {
                CheckAsync();
                return _coreReader.Prefix;
            }
        }

        public override bool HasValue
        {
            get
            {
                CheckAsync();
                return _coreReader.HasValue;
            }
        }

        public override string Value
        {
            get
            {
                CheckAsync();
                return _coreReader.Value;
            }
        }

        public override int Depth
        {
            get
            {
                CheckAsync();
                return _coreReader.Depth;
            }
        }

        public override string BaseURI
        {
            get
            {
                CheckAsync();
                return _coreReader.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                CheckAsync();
                return _coreReader.IsEmptyElement;
            }
        }

        public override bool IsDefault
        {
            get
            {
                CheckAsync();
                return _coreReader.IsDefault;
            }
        }

        public override char QuoteChar
        {
            get
            {
                CheckAsync();
                return _coreReader.QuoteChar;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                CheckAsync();
                return _coreReader.XmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                CheckAsync();
                return _coreReader.XmlLang;
            }
        }

        public override IXmlSchemaInfo? SchemaInfo
        {
            get
            {
                CheckAsync();
                return _coreReader.SchemaInfo;
            }
        }

        public override System.Type ValueType
        {
            get
            {
                CheckAsync();
                return _coreReader.ValueType;
            }
        }

        public override object ReadContentAsObject()
        {
            CheckAsync();
            return _coreReader.ReadContentAsObject();
        }

        public override bool ReadContentAsBoolean()
        {
            CheckAsync();
            return _coreReader.ReadContentAsBoolean();
        }

        public override DateTime ReadContentAsDateTime()
        {
            CheckAsync();
            return _coreReader.ReadContentAsDateTime();
        }

        public override double ReadContentAsDouble()
        {
            CheckAsync();
            return _coreReader.ReadContentAsDouble();
        }

        public override float ReadContentAsFloat()
        {
            CheckAsync();
            return _coreReader.ReadContentAsFloat();
        }

        public override decimal ReadContentAsDecimal()
        {
            CheckAsync();
            return _coreReader.ReadContentAsDecimal();
        }

        public override int ReadContentAsInt()
        {
            CheckAsync();
            return _coreReader.ReadContentAsInt();
        }

        public override long ReadContentAsLong()
        {
            CheckAsync();
            return _coreReader.ReadContentAsLong();
        }

        public override string ReadContentAsString()
        {
            CheckAsync();
            return _coreReader.ReadContentAsString();
        }

        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver)
        {
            CheckAsync();
            return _coreReader.ReadContentAs(returnType, namespaceResolver);
        }

        public override object ReadElementContentAsObject()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsObject();
        }

        public override object ReadElementContentAsObject(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsObject(localName, namespaceURI);
        }

        public override bool ReadElementContentAsBoolean()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsBoolean();
        }

        public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsBoolean(localName, namespaceURI);
        }

        public override DateTime ReadElementContentAsDateTime()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDateTime();
        }

        public override DateTime ReadElementContentAsDateTime(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDateTime(localName, namespaceURI);
        }

        public override DateTimeOffset ReadContentAsDateTimeOffset()
        {
            CheckAsync();
            return _coreReader.ReadContentAsDateTimeOffset();
        }

        public override double ReadElementContentAsDouble()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDouble();
        }

        public override double ReadElementContentAsDouble(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDouble(localName, namespaceURI);
        }

        public override float ReadElementContentAsFloat()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsFloat();
        }

        public override float ReadElementContentAsFloat(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsFloat(localName, namespaceURI);
        }

        public override decimal ReadElementContentAsDecimal()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDecimal();
        }

        public override decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsDecimal(localName, namespaceURI);
        }

        public override int ReadElementContentAsInt()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsInt();
        }

        public override int ReadElementContentAsInt(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsInt(localName, namespaceURI);
        }

        public override long ReadElementContentAsLong()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsLong();
        }

        public override long ReadElementContentAsLong(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsLong(localName, namespaceURI);
        }

        public override string ReadElementContentAsString()
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsString();
        }

        public override string ReadElementContentAsString(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsString(localName, namespaceURI);
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAs(returnType, namespaceResolver);
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI);
        }

        public override int AttributeCount
        {
            get
            {
                CheckAsync();
                return _coreReader.AttributeCount;
            }
        }

        public override string? GetAttribute(string name)
        {
            CheckAsync();
            return _coreReader.GetAttribute(name);
        }

        public override string? GetAttribute(string name, string? namespaceURI)
        {
            CheckAsync();
            return _coreReader.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            CheckAsync();
            return _coreReader.GetAttribute(i);
        }

        public override string this[int i]
        {
            get
            {
                CheckAsync();
                return _coreReader[i];
            }
        }

        public override string? this[string name]
        {
            get
            {
                CheckAsync();
                return _coreReader[name];
            }
        }

        public override bool MoveToAttribute(string name)
        {
            CheckAsync();
            return _coreReader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string? ns)
        {
            CheckAsync();
            return _coreReader.MoveToAttribute(name, ns);
        }

        public override void MoveToAttribute(int i)
        {
            CheckAsync();
            _coreReader.MoveToAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            CheckAsync();
            return _coreReader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            CheckAsync();
            return _coreReader.MoveToNextAttribute();
        }

        public override bool MoveToElement()
        {
            CheckAsync();
            return _coreReader.MoveToElement();
        }

        public override bool ReadAttributeValue()
        {
            CheckAsync();
            return _coreReader.ReadAttributeValue();
        }

        public override bool Read()
        {
            CheckAsync();
            return _coreReader.Read();
        }

        public override bool EOF
        {
            get
            {
                CheckAsync();
                return _coreReader.EOF;
            }
        }

        public override void Close()
        {
            CheckAsync();
            _coreReader.Close();
        }

        public override ReadState ReadState
        {
            get
            {
                CheckAsync();
                return _coreReader.ReadState;
            }
        }

        public override void Skip()
        {
            CheckAsync();
            _coreReader.Skip();
        }

        public override XmlNameTable NameTable
        {
            get
            {
                CheckAsync();
                return _coreReader.NameTable;
            }
        }

        public override string? LookupNamespace(string prefix)
        {
            CheckAsync();
            return _coreReader.LookupNamespace(prefix);
        }

        public override bool CanResolveEntity
        {
            get
            {
                CheckAsync();
                return _coreReader.CanResolveEntity;
            }
        }

        public override void ResolveEntity()
        {
            CheckAsync();
            _coreReader.ResolveEntity();
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                CheckAsync();
                return _coreReader.CanReadBinaryContent;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            CheckAsync();
            return _coreReader.ReadContentAsBase64(buffer, index, count);
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsBase64(buffer, index, count);
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            CheckAsync();
            return _coreReader.ReadContentAsBinHex(buffer, index, count);
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            CheckAsync();
            return _coreReader.ReadElementContentAsBinHex(buffer, index, count);
        }

        public override bool CanReadValueChunk
        {
            get
            {
                CheckAsync();
                return _coreReader.CanReadValueChunk;
            }
        }

        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            CheckAsync();
            return _coreReader.ReadValueChunk(buffer, index, count);
        }

        public override string ReadString()
        {
            CheckAsync();
            return _coreReader.ReadString();
        }

        public override XmlNodeType MoveToContent()
        {
            CheckAsync();
            return _coreReader.MoveToContent();
        }

        public override void ReadStartElement()
        {
            CheckAsync();
            _coreReader.ReadStartElement();
        }

        public override void ReadStartElement(string name)
        {
            CheckAsync();
            _coreReader.ReadStartElement(name);
        }

        public override void ReadStartElement(string localname, string ns)
        {
            CheckAsync();
            _coreReader.ReadStartElement(localname, ns);
        }

        public override string ReadElementString()
        {
            CheckAsync();
            return _coreReader.ReadElementString();
        }

        public override string ReadElementString(string name)
        {
            CheckAsync();
            return _coreReader.ReadElementString(name);
        }

        public override string ReadElementString(string localname, string ns)
        {
            CheckAsync();
            return _coreReader.ReadElementString(localname, ns);
        }

        public override void ReadEndElement()
        {
            CheckAsync();
            _coreReader.ReadEndElement();
        }

        public override bool IsStartElement()
        {
            CheckAsync();
            return _coreReader.IsStartElement();
        }

        public override bool IsStartElement(string name)
        {
            CheckAsync();
            return _coreReader.IsStartElement(name);
        }

        public override bool IsStartElement(string localname, string ns)
        {
            CheckAsync();
            return _coreReader.IsStartElement(localname, ns);
        }

        public override bool ReadToFollowing(string name)
        {
            CheckAsync();
            return _coreReader.ReadToFollowing(name);
        }

        public override bool ReadToFollowing(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadToFollowing(localName, namespaceURI);
        }

        public override bool ReadToDescendant(string name)
        {
            CheckAsync();
            return _coreReader.ReadToDescendant(name);
        }

        public override bool ReadToDescendant(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadToDescendant(localName, namespaceURI);
        }

        public override bool ReadToNextSibling(string name)
        {
            CheckAsync();
            return _coreReader.ReadToNextSibling(name);
        }

        public override bool ReadToNextSibling(string localName, string namespaceURI)
        {
            CheckAsync();
            return _coreReader.ReadToNextSibling(localName, namespaceURI);
        }

        public override string ReadInnerXml()
        {
            CheckAsync();
            return _coreReader.ReadInnerXml();
        }

        public override string ReadOuterXml()
        {
            CheckAsync();
            return _coreReader.ReadOuterXml();
        }

        public override XmlReader ReadSubtree()
        {
            CheckAsync();
            XmlReader subtreeReader = _coreReader.ReadSubtree();
            return CreateAsyncCheckWrapper(subtreeReader);
        }

        public override bool HasAttributes
        {
            get
            {
                CheckAsync();
                return _coreReader.HasAttributes;
            }
        }

        protected override void Dispose(bool disposing)
        {
            CheckAsync();
            //since it is protected method, we can't call coreReader.Dispose(disposing).
            //Internal, it is always called to Dispose(true). So call coreReader.Dispose() is OK.
            _coreReader.Dispose();
        }

        internal override XmlNamespaceManager? NamespaceManager
        {
            get
            {
                CheckAsync();
                return _coreReader.NamespaceManager;
            }
        }

        internal override IDtdInfo? DtdInfo
        {
            get
            {
                CheckAsync();
                return _coreReader.DtdInfo;
            }
        }

        #endregion

        #region Async Methods

        public override Task<string> GetValueAsync()
        {
            CheckAsync();
            var task = _coreReader.GetValueAsync();
            _lastTask = task;
            return task;
        }

        public override Task<object> ReadContentAsObjectAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadContentAsObjectAsync();
            _lastTask = task;
            return task;
        }

        public override Task<string> ReadContentAsStringAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadContentAsStringAsync();
            _lastTask = task;
            return task;
        }

        public override Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver? namespaceResolver)
        {
            CheckAsync();
            var task = _coreReader.ReadContentAsAsync(returnType, namespaceResolver);
            _lastTask = task;
            return task;
        }

        public override Task<object> ReadElementContentAsObjectAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadElementContentAsObjectAsync();
            _lastTask = task;
            return task;
        }

        public override Task<string> ReadElementContentAsStringAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadElementContentAsStringAsync();
            _lastTask = task;
            return task;
        }

        public override Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            CheckAsync();
            var task = _coreReader.ReadElementContentAsAsync(returnType, namespaceResolver);
            _lastTask = task;
            return task;
        }

        public override Task<bool> ReadAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadAsync();
            _lastTask = task;
            return task;
        }

        public override Task SkipAsync()
        {
            CheckAsync();
            var task = _coreReader.SkipAsync();
            _lastTask = task;
            return task;
        }

        public override Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreReader.ReadContentAsBase64Async(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreReader.ReadElementContentAsBase64Async(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreReader.ReadContentAsBinHexAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreReader.ReadElementContentAsBinHexAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            CheckAsync();
            var task = _coreReader.ReadValueChunkAsync(buffer, index, count);
            _lastTask = task;
            return task;
        }

        public override Task<XmlNodeType> MoveToContentAsync()
        {
            CheckAsync();
            var task = _coreReader.MoveToContentAsync();
            _lastTask = task;
            return task;
        }

        public override Task<string> ReadInnerXmlAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadInnerXmlAsync();
            _lastTask = task;
            return task;
        }

        public override Task<string> ReadOuterXmlAsync()
        {
            CheckAsync();
            var task = _coreReader.ReadOuterXmlAsync();
            _lastTask = task;
            return task;
        }
        #endregion
    }

    internal sealed class XmlAsyncCheckReaderWithNS : XmlAsyncCheckReader, IXmlNamespaceResolver
    {
        private readonly IXmlNamespaceResolver _readerAsIXmlNamespaceResolver;

        public XmlAsyncCheckReaderWithNS(XmlReader reader)
            : base(reader)
        {
            _readerAsIXmlNamespaceResolver = (IXmlNamespaceResolver)reader;
        }

        #region IXmlNamespaceResolver members
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _readerAsIXmlNamespaceResolver.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _readerAsIXmlNamespaceResolver.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _readerAsIXmlNamespaceResolver.LookupPrefix(namespaceName);
        }
        #endregion
    }

    internal class XmlAsyncCheckReaderWithLineInfo : XmlAsyncCheckReader, IXmlLineInfo
    {
        private readonly IXmlLineInfo _readerAsIXmlLineInfo;

        public XmlAsyncCheckReaderWithLineInfo(XmlReader reader)
            : base(reader)
        {
            _readerAsIXmlLineInfo = (IXmlLineInfo)reader;
        }

        #region IXmlLineInfo members
        public virtual bool HasLineInfo()
        {
            return _readerAsIXmlLineInfo.HasLineInfo();
        }

        public virtual int LineNumber
        {
            get
            {
                return _readerAsIXmlLineInfo.LineNumber;
            }
        }

        public virtual int LinePosition
        {
            get
            {
                return _readerAsIXmlLineInfo.LinePosition;
            }
        }
        #endregion
    }

    internal class XmlAsyncCheckReaderWithLineInfoNS : XmlAsyncCheckReaderWithLineInfo, IXmlNamespaceResolver
    {
        private readonly IXmlNamespaceResolver _readerAsIXmlNamespaceResolver;

        public XmlAsyncCheckReaderWithLineInfoNS(XmlReader reader)
            : base(reader)
        {
            _readerAsIXmlNamespaceResolver = (IXmlNamespaceResolver)reader;
        }

        #region IXmlNamespaceResolver members
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _readerAsIXmlNamespaceResolver.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _readerAsIXmlNamespaceResolver.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _readerAsIXmlNamespaceResolver.LookupPrefix(namespaceName);
        }
        #endregion
    }

    internal sealed class XmlAsyncCheckReaderWithLineInfoNSSchema : XmlAsyncCheckReaderWithLineInfoNS, IXmlSchemaInfo
    {
        private readonly IXmlSchemaInfo _readerAsIXmlSchemaInfo;

        public XmlAsyncCheckReaderWithLineInfoNSSchema(XmlReader reader)
            : base(reader)
        {
            _readerAsIXmlSchemaInfo = (IXmlSchemaInfo)reader;
        }

        #region IXmlSchemaInfo members

        XmlSchemaValidity IXmlSchemaInfo.Validity
        {
            get
            {
                return _readerAsIXmlSchemaInfo.Validity;
            }
        }

        bool IXmlSchemaInfo.IsDefault
        {
            get
            {
                return _readerAsIXmlSchemaInfo.IsDefault;
            }
        }

        bool IXmlSchemaInfo.IsNil
        {
            get
            {
                return _readerAsIXmlSchemaInfo.IsNil;
            }
        }

        XmlSchemaSimpleType? IXmlSchemaInfo.MemberType
        {
            get
            {
                return _readerAsIXmlSchemaInfo.MemberType;
            }
        }

        XmlSchemaType? IXmlSchemaInfo.SchemaType
        {
            get
            {
                return _readerAsIXmlSchemaInfo.SchemaType;
            }
        }

        XmlSchemaElement? IXmlSchemaInfo.SchemaElement
        {
            get
            {
                return _readerAsIXmlSchemaInfo.SchemaElement;
            }
        }

        XmlSchemaAttribute? IXmlSchemaInfo.SchemaAttribute
        {
            get
            {
                return _readerAsIXmlSchemaInfo.SchemaAttribute;
            }
        }
        #endregion
    }
}
