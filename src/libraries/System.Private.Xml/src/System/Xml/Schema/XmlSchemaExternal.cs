// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public abstract class XmlSchemaExternal : XmlSchemaObject
    {
        private string? _location;
        private Uri? _baseUri;
        private XmlSchema? _schema;
        private string? _id;
        private XmlAttribute[]? _moreAttributes;
        private Compositor _compositor;

        [XmlAttribute("schemaLocation", DataType = "anyURI")]
        public string? SchemaLocation
        {
            get { return _location; }
            set { _location = value; }
        }

        [XmlIgnore]
        public XmlSchema? Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }

        [XmlAttribute("id", DataType = "ID")]
        public string? Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [XmlAnyAttribute]
        public XmlAttribute[]? UnhandledAttributes
        {
            get { return _moreAttributes; }
            set { _moreAttributes = value; }
        }

        [XmlIgnore]
        internal Uri? BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        [XmlIgnore]
        internal override string? IdAttribute
        {
            get { return Id; }
            set { Id = value; }
        }

        internal override void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
        {
            _moreAttributes = moreAttributes;
        }

        internal Compositor Compositor
        {
            get
            {
                return _compositor;
            }
            set
            {
                _compositor = value;
            }
        }
    }
}
