// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.ServiceModel.Syndication.Tests
{
    public class ThrowingXmlReader : XmlReader
    {
        public ThrowingXmlReader(Exception exception) => Exception = exception;

        public Exception Exception { get; }

        public override int AttributeCount => throw Exception;

        public override string BaseURI => throw Exception;

        public override int Depth => throw Exception;

        public override bool EOF => throw Exception;

        public override bool IsEmptyElement => throw Exception;

        public override string LocalName => throw Exception;

        public override string NamespaceURI => throw Exception;

        public override XmlNameTable NameTable => throw Exception;

        public override XmlNodeType NodeType => XmlNodeType.Element;

        public override string Prefix => throw Exception;

        public override ReadState ReadState => throw Exception;

        public override string Value => throw Exception;

        public override string GetAttribute(int i) => throw Exception;

        public override string GetAttribute(string name) => throw Exception;

        public override string GetAttribute(string name, string namespaceURI) => throw Exception;

        public override string LookupNamespace(string prefix) => throw Exception;

        public override bool MoveToAttribute(string name) => throw Exception;

        public override bool MoveToAttribute(string name, string ns) => throw Exception;

        public override bool MoveToElement() => throw Exception;

        public override bool MoveToFirstAttribute() => throw Exception;

        public override bool MoveToNextAttribute() => throw Exception;

        public override bool Read() => throw Exception;

        public override bool ReadAttributeValue() => throw Exception;

        public override void ResolveEntity() => throw Exception;
    }
}
