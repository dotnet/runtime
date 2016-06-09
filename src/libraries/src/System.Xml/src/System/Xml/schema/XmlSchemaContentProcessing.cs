// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaContentProcessing.uex' path='docs/doc[@for="XmlSchemaContentProcessing"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum XmlSchemaContentProcessing
    {
        /// <include file='doc\XmlSchemaContentProcessing.uex' path='docs/doc[@for="XmlSchemaContentProcessing.None"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        None,
        /// <include file='doc\XmlSchemaContentProcessing.uex' path='docs/doc[@for="XmlSchemaContentProcessing.XmlEnum"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("skip")]
        Skip,
        /// <include file='doc\XmlSchemaContentProcessing.uex' path='docs/doc[@for="XmlSchemaContentProcessing.XmlEnum1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("lax")]
        Lax,
        /// <include file='doc\XmlSchemaContentProcessing.uex' path='docs/doc[@for="XmlSchemaContentProcessing.XmlEnum2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("strict")]
        Strict
    }
}
