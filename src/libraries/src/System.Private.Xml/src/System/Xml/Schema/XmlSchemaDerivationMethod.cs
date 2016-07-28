// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [Flags]
    public enum XmlSchemaDerivationMethod
    {
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.Empty"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("")]
        Empty = 0,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.Substitution"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("substitution")]
        Substitution = 0x0001,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.Extension"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("extension")]
        Extension = 0x0002,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.Restriction"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("restriction")]
        Restriction = 0x0004,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.List"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("list")]
        List = 0x0008,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.Union"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("union")]
        Union = 0x0010,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.All"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("#all")]
        All = 0x00FF,
        /// <include file='doc\XmlSchemaDerivationMethod.uex' path='docs/doc[@for="XmlSchemaDerivationMethod.None"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        None = 0x0100
    }
}
