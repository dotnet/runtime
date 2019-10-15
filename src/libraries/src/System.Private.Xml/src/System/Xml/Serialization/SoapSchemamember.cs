// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Xml;

    public class SoapSchemaMember
    {
        private string _memberName;
        private XmlQualifiedName _type = XmlQualifiedName.Empty;

        public XmlQualifiedName MemberType
        {
            get { return _type; }
            set { _type = value; }
        }

        public string MemberName
        {
            get { return _memberName == null ? string.Empty : _memberName; }
            set { _memberName = value; }
        }
    }
}
