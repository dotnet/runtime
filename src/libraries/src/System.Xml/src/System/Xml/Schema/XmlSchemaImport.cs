// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaImport : XmlSchemaExternal
    {
        private string _ns;
        private XmlSchemaAnnotation _annotation;

        /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport.XmlSchemaImport"]/*' />
        public XmlSchemaImport()
        {
            Compositor = Compositor.Import;
        }

        /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport.Namespace"]/*' />
        [XmlAttribute("namespace", DataType = "anyURI")]
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport.Annotation"]/*' />
        [XmlElement("annotation", typeof(XmlSchemaAnnotation))]
        public XmlSchemaAnnotation Annotation
        {
            get { return _annotation; }
            set { _annotation = value; }
        }

        internal override void AddAnnotation(XmlSchemaAnnotation annotation)
        {
            _annotation = annotation;
        }
    }
}
