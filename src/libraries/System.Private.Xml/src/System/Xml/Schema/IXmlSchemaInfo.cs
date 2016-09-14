// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Collections;

namespace System.Xml.Schema
{
    /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo"]/*' />
    public interface IXmlSchemaInfo
    {
        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.Validity"]/*' />
        XmlSchemaValidity Validity { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.IsDefault"]/*' />
        bool IsDefault { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.IsNil"]/*' />
        bool IsNil { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.MemberType"]/*' />
        XmlSchemaSimpleType MemberType { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.SchemaType"]/*' />
        XmlSchemaType SchemaType { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.SchemaElement"]/*' />
        XmlSchemaElement SchemaElement { get; }

        /// <include file='doc\IXmlSchemaInfo.uex' path='docs/doc[@for="IXmlSchemaInfo.SchemaAttribute"]/*' />
        XmlSchemaAttribute SchemaAttribute { get; }
    }
}
