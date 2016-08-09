// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaAttributeGroupRef.uex' path='docs/doc[@for="XmlSchemaAttributeGroupRef"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaAttributeGroupRef : XmlSchemaAnnotated
    {
        private XmlQualifiedName _refName = XmlQualifiedName.Empty;

        /// <include file='doc\XmlSchemaAttributeGroupRef.uex' path='docs/doc[@for="XmlSchemaAttributeGroupRef.RefName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("ref")]
        public XmlQualifiedName RefName
        {
            get { return _refName; }
            set { _refName = (value == null ? XmlQualifiedName.Empty : value); }
        }
    }
}
