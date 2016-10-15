// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Security;

namespace System.Xml
{
    public interface IXmlMtomReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding[] encodings, string contentType, XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose onClose);
        void SetInput(Stream stream, Encoding[] encodings, string contentType, XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose onClose);
    }

    class XmlMtomReader : XmlDictionaryReader, IXmlLineInfo, IXmlMtomReaderInitializer
    {
        public override int AttributeCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string BaseURI
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool EOF
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineNumber
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LinePosition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string LocalName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string NamespaceURI
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Prefix
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ReadState ReadState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Value
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string GetAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(int i)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public bool HasLineInfo()
        {
            throw new NotImplementedException();
        }

        public override string LookupNamespace(string prefix)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToElement()
        {
            throw new NotImplementedException();
        }

        public override bool MoveToFirstAttribute()
        {
            throw new NotImplementedException();
        }

        public override bool MoveToNextAttribute()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            throw new NotImplementedException();
        }

        public override bool ReadAttributeValue()
        {
            throw new NotImplementedException();
        }

        public override void ResolveEntity()
        {
            throw new NotImplementedException();
        }

        public void SetInput(Stream stream, Encoding[] encodings, string contentType, XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose onClose)
        {
            throw new NotImplementedException();
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding[] encodings, string contentType, XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose onClose)
        {
            throw new NotImplementedException();
        }
    }
}