// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaSimpleContent.uex' path='docs/doc[@for="XmlSchemaSimpleContent"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaSimpleContent : XmlSchemaContentModel
    {
        private XmlSchemaContent _content;

        /// <include file='doc\XmlSchemaSimpleContent.uex' path='docs/doc[@for="XmlSchemaSimpleContent.Content"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("restriction", typeof(XmlSchemaSimpleContentRestriction)),
         XmlElement("extension", typeof(XmlSchemaSimpleContentExtension))]
        public override XmlSchemaContent Content
        {
            get { return _content; }
            set { _content = value; }
        }
    }
}
