// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    public class XmlSchemaInclude : XmlSchemaExternal
    {
        private XmlSchemaAnnotation _annotation;

        public XmlSchemaInclude()
        {
            Compositor = Compositor.Include;
        }

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
