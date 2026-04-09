// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaImport : XmlSchemaExternal
    {
        private string? _ns;
        private XmlSchemaAnnotation? _annotation;

        public XmlSchemaImport()
        {
            Compositor = Compositor.Import;
        }

        [XmlAttribute("namespace", DataType = "anyURI")]
        public string? Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        [XmlElement("annotation", typeof(XmlSchemaAnnotation))]
        public XmlSchemaAnnotation? Annotation
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
