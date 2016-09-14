// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    //nzeng: if change the enum, have to change xsdbuilder as well.
    /// <include file='doc\XmlSchemaUse.uex' path='docs/doc[@for="XmlSchemaUse"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum XmlSchemaUse
    {
        /// <include file='doc\XmlSchemaUse.uex' path='docs/doc[@for="XmlSchemaUse.None"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        None,
        /// <include file='doc\XmlSchemaUse.uex' path='docs/doc[@for="XmlSchemaUse.Optional"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("optional")]
        Optional,
        /// <include file='doc\XmlSchemaUse.uex' path='docs/doc[@for="XmlSchemaUse.Prohibited"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("prohibited")]
        Prohibited,
        /// <include file='doc\XmlSchemaUse.uex' path='docs/doc[@for="XmlSchemaUse.Required"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("required")]
        Required,
    }
}
