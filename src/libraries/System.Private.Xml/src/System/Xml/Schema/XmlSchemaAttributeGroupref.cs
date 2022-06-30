// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaAttributeGroupRef : XmlSchemaAnnotated
    {
        private XmlQualifiedName _refName = XmlQualifiedName.Empty;

        [XmlAttribute("ref")]
        [AllowNull]
        public XmlQualifiedName RefName
        {
            get { return _refName; }
            set { _refName = value ?? XmlQualifiedName.Empty; }
        }
    }
}
