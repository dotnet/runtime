// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaAppInfo.uex' path='docs/doc[@for="XmlSchemaAppInfo"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaAppInfo : XmlSchemaObject
    {
        private string _source;
        private XmlNode[] _markup;

        /// <include file='doc\XmlSchemaAppInfo.uex' path='docs/doc[@for="XmlSchemaAppInfo.Source"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("source", DataType = "anyURI")]
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        /// <include file='doc\XmlSchemaAppInfo.uex' path='docs/doc[@for="XmlSchemaAppInfo.Markup"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlText(), XmlAnyElement]
        public XmlNode[] Markup
        {
            get { return _markup; }
            set { _markup = value; }
        }
    }
}
