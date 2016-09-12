// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaContentModel.uex' path='docs/doc[@for="XmlSchemaContentModel"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlSchemaContentModel : XmlSchemaAnnotated
    {
        /// <include file='doc\XmlSchemaContentModel.uex' path='docs/doc[@for="XmlSchemaContentModel.Content"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public abstract XmlSchemaContent Content { get; set; }
    }
}

