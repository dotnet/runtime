// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public class XmlSchemaIdentityConstraint : XmlSchemaAnnotated
    {
        private string _name;
        private XmlSchemaXPath _selector;
        private XmlSchemaObjectCollection _fields = new XmlSchemaObjectCollection();
        private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;
        private CompiledIdentityConstraint _compiledConstraint = null;

        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [XmlElement("selector", typeof(XmlSchemaXPath))]
        public XmlSchemaXPath Selector
        {
            get { return _selector; }
            set { _selector = value; }
        }

        [XmlElement("field", typeof(XmlSchemaXPath))]
        public XmlSchemaObjectCollection Fields
        {
            get { return _fields; }
        }

        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qualifiedName; }
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qualifiedName = value;
        }

        [XmlIgnore]
        internal CompiledIdentityConstraint CompiledConstraint
        {
            get { return _compiledConstraint; }
            set { _compiledConstraint = value; }
        }

        [XmlIgnore]
        internal override string NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }
    }

    public class XmlSchemaXPath : XmlSchemaAnnotated
    {
        private string _xpath;

        [XmlAttribute("xpath"), DefaultValue("")]
        public string XPath
        {
            get { return _xpath; }
            set { _xpath = value; }
        }
    }

    public class XmlSchemaUnique : XmlSchemaIdentityConstraint
    {
    }

    public class XmlSchemaKey : XmlSchemaIdentityConstraint
    {
    }

    public class XmlSchemaKeyref : XmlSchemaIdentityConstraint
    {
        private XmlQualifiedName _refer = XmlQualifiedName.Empty;

        [XmlAttribute("refer")]
        public XmlQualifiedName Refer
        {
            get { return _refer; }
            set { _refer = (value == null ? XmlQualifiedName.Empty : value); }
        }
    }
}
