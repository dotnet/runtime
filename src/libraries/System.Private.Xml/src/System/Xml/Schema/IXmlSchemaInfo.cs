// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Xml;

namespace System.Xml.Schema
{
    public interface IXmlSchemaInfo
    {
        XmlSchemaValidity Validity { get; }

        bool IsDefault { get; }

        bool IsNil { get; }

        XmlSchemaSimpleType? MemberType { get; }

        XmlSchemaType? SchemaType { get; }

        XmlSchemaElement? SchemaElement { get; }

        XmlSchemaAttribute? SchemaAttribute { get; }
    }
}
