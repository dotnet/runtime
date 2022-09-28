// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public class XmlSchemaSimpleContent : XmlSchemaContentModel
    {
        private XmlSchemaContent? _content;

        [XmlElement("restriction", typeof(XmlSchemaSimpleContentRestriction)),
         XmlElement("extension", typeof(XmlSchemaSimpleContentExtension))]
        public override XmlSchemaContent? Content
        {
            get { return _content; }
            set { _content = value; }
        }
    }
}
