// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    // if change the enum, have to change xsdbuilder as well.
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum XmlSchemaForm
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        None,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("qualified")]
        Qualified,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlEnum("unqualified")]
        Unqualified,
    }
}
