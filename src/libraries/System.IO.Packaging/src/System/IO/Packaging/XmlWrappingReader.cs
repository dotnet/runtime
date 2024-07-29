// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;

namespace System.IO.Packaging
{
    internal class XmlWrappingReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        protected XmlReader _reader;
        protected IXmlLineInfo? _readerAsIXmlLineInfo;
        protected IXmlNamespaceResolver? _readerAsResolver;

        internal XmlWrappingReader(XmlReader baseReader)
        {
            Debug.Assert(baseReader != null);
            Reader = baseReader;
        }

        public override XmlReaderSettings? Settings { get { return _reader.Settings; } }
        public override XmlNodeType NodeType { get { return _reader.NodeType; } }
        public override string Name { get { return _reader.Name; } }
        public override string LocalName { get { return _reader.LocalName; } }
        public override string NamespaceURI { get { return _reader.NamespaceURI; } }
        public override string Prefix { get { return _reader.Prefix; } }
        public override bool HasValue { get { return _reader.HasValue; } }
        public override string Value { get { return _reader.Value; } }
        public override int Depth { get { return _reader.Depth; } }
        public override string BaseURI { get { return _reader.BaseURI; } }
        public override bool IsEmptyElement { get { return _reader.IsEmptyElement; } }
        public override bool IsDefault { get { return _reader.IsDefault; } }
        public override XmlSpace XmlSpace { get { return _reader.XmlSpace; } }
        public override string XmlLang { get { return _reader.XmlLang; } }
        public override System.Type ValueType { get { return _reader.ValueType; } }
        public override int AttributeCount { get { return _reader.AttributeCount; } }
        public override string this[int i] { get { return _reader[i]; } }
        public override string? this[string name] { get { return _reader[name]; } }
        public override string? this[string name, string? namespaceURI] { get { return _reader[name, namespaceURI]; } }
        public override bool CanResolveEntity { get { return _reader.CanResolveEntity; } }
        public override bool EOF { get { return _reader.EOF; } }
        public override ReadState ReadState { get { return _reader.ReadState; } }
        public override bool HasAttributes { get { return _reader.HasAttributes; } }
        public override XmlNameTable NameTable { get { return _reader.NameTable; } }

        public override string? GetAttribute(string name)
        {
            return _reader.GetAttribute(name);
        }

        public override string? GetAttribute(string name, string? namespaceURI)
        {
            return _reader.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            return _reader.GetAttribute(i);
        }

        public override bool MoveToAttribute(string name)
        {
            return _reader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string? ns)
        {
            return _reader.MoveToAttribute(name, ns);
        }

        public override void MoveToAttribute(int i)
        {
            _reader.MoveToAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            return _reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return _reader.MoveToNextAttribute();
        }

        public override bool MoveToElement()
        {
            return _reader.MoveToElement();
        }

        public override bool Read()
        {
            return _reader.Read();
        }

        public override void Skip()
        {
            _reader.Skip();
        }

        public override string? LookupNamespace(string prefix)
        {
            return _reader.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _readerAsResolver?.LookupPrefix(namespaceName);
        }

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _readerAsResolver?.GetNamespacesInScope(scope)!;
        }

        public override void ResolveEntity()
        {
            _reader.ResolveEntity();
        }

        public override bool ReadAttributeValue()
        {
            return _reader.ReadAttributeValue();
        }

        //
        // IDisposable interface
        //
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    ((IDisposable)_reader).Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //
        // IXmlLineInfo members
        //
        public virtual bool HasLineInfo()
        {
            return (_readerAsIXmlLineInfo == null) ? false : _readerAsIXmlLineInfo.HasLineInfo();
        }

        public virtual int LineNumber
        {
            get
            {
                return (_readerAsIXmlLineInfo == null) ? 0 : _readerAsIXmlLineInfo.LineNumber;
            }
        }

        public virtual int LinePosition
        {
            get
            {
                return (_readerAsIXmlLineInfo == null) ? 0 : _readerAsIXmlLineInfo.LinePosition;
            }
        }

        protected XmlReader Reader
        {
            get
            {
                return _reader;
            }
            [MemberNotNull(nameof(_reader))]
            set
            {
                _reader = value;
                _readerAsIXmlLineInfo = value as IXmlLineInfo;
                _readerAsResolver = value as IXmlNamespaceResolver;
            }
        }
    }
}
