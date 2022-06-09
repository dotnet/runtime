// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;

    public class SoapSchemaMember
    {
        private string? _memberName;
        private XmlQualifiedName? _type = XmlQualifiedName.Empty;

        public XmlQualifiedName? MemberType
        {
            get { return _type; }
            set { _type = value; }
        }

        [AllowNull]
        public string MemberName
        {
            get { return _memberName ?? string.Empty; }
            set { _memberName = value; }
        }
    }
}
