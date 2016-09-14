// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaGroupbase.uex' path='docs/doc[@for="XmlSchemaGroupBase"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlSchemaGroupBase : XmlSchemaParticle
    {
        /// <include file='doc\XmlSchemaGroupbase.uex' path='docs/doc[@for="XmlSchemaGroupBase.Items"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public abstract XmlSchemaObjectCollection Items { get; }

        internal abstract void SetItems(XmlSchemaObjectCollection newItems);
    }
}
