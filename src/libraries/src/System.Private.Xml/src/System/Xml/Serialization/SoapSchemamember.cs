// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System;
    using System.Xml;

    /// <include file='doc\SoapSchemaMember.uex' path='docs/doc[@for="SoapSchemaMember"]/*' />
    /// <internalonly/>
#if XMLSERIALIZERGENERATOR
    internal class SoapSchemaMember
#else
    public class SoapSchemaMember
#endif
    {
        private string _memberName;
        private XmlQualifiedName _type = XmlQualifiedName.Empty;

        /// <include file='doc\SoapSchemaMember.uex' path='docs/doc[@for="SoapSchemaMember.MemberType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName MemberType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\SoapSchemaMember.uex' path='docs/doc[@for="SoapSchemaMember.MemberName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MemberName
        {
            get { return _memberName == null ? string.Empty : _memberName; }
            set { _memberName = value; }
        }
    }
}
