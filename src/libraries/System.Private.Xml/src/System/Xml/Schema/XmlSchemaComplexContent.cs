// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    public class XmlSchemaComplexContent : XmlSchemaContentModel
    {
        private XmlSchemaContent _content;
        private bool _isMixed;
        private bool _hasMixedAttribute;

        [XmlAttribute("mixed")]
        public bool IsMixed
        {
            get { return _isMixed; }
            set { _isMixed = value; _hasMixedAttribute = true; }
        }

        [XmlElement("restriction", typeof(XmlSchemaComplexContentRestriction)),
         XmlElement("extension", typeof(XmlSchemaComplexContentExtension))]
        public override XmlSchemaContent Content
        {
            get { return _content; }
            set { _content = value; }
        }

        [XmlIgnore]
        internal bool HasMixedAttribute
        {
            get { return _hasMixedAttribute; }
        }
    }
}
