// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaAnnotation.uex' path='docs/doc[@for="XmlSchemaAnnotation"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaAnnotation : XmlSchemaObject
    {
        private string _id;
        private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();
        private XmlAttribute[] _moreAttributes;

        /// <include file='doc\XmlSchemaAnnotation.uex' path='docs/doc[@for="XmlSchemaAnnotation.Id"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("id", DataType = "ID")]
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <include file='doc\XmlSchemaAnnotation.uex' path='docs/doc[@for="XmlSchemaAnnotation.Items"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("documentation", typeof(XmlSchemaDocumentation)),
         XmlElement("appinfo", typeof(XmlSchemaAppInfo))]
        public XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        /// <include file='doc\XmlSchemaAnnotation.uex' path='docs/doc[@for="XmlSchemaAnnotation.UnhandledAttributes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAnyAttribute]
        public XmlAttribute[] UnhandledAttributes
        {
            get { return _moreAttributes; }
            set { _moreAttributes = value; }
        }

        [XmlIgnore]
        internal override string IdAttribute
        {
            get { return Id; }
            set { Id = value; }
        }

        internal override void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
        {
            _moreAttributes = moreAttributes;
        }
    }
}
