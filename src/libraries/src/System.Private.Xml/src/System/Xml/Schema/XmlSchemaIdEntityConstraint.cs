// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaIdentityConstraint"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaIdentityConstraint : XmlSchemaAnnotated
    {
        private string _name;
        private XmlSchemaXPath _selector;
        private XmlSchemaObjectCollection _fields = new XmlSchemaObjectCollection();
        private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;
        private CompiledIdentityConstraint _compiledConstraint = null;

        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaIdentityConstraint.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaIdentityConstraint.Selector"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("selector", typeof(XmlSchemaXPath))]
        public XmlSchemaXPath Selector
        {
            get { return _selector; }
            set { _selector = value; }
        }

        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaIdentityConstraint.Fields"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("field", typeof(XmlSchemaXPath))]
        public XmlSchemaObjectCollection Fields
        {
            get { return _fields; }
        }

        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaIdentityConstraint.QualifiedName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

    /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaXPath"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaXPath : XmlSchemaAnnotated
    {
        private string _xpath;
        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaXPath.XPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("xpath"), DefaultValue("")]
        public string XPath
        {
            get { return _xpath; }
            set { _xpath = value; }
        }
    }

    /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaUnique"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaUnique : XmlSchemaIdentityConstraint
    {
    }

    /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaKey"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaKey : XmlSchemaIdentityConstraint
    {
    }

    /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaKeyref"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaKeyref : XmlSchemaIdentityConstraint
    {
        private XmlQualifiedName _refer = XmlQualifiedName.Empty;

        /// <include file='doc\XmlSchemaIdentityConstraint.uex' path='docs/doc[@for="XmlSchemaKeyref.Refer"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("refer")]
        public XmlQualifiedName Refer
        {
            get { return _refer; }
            set { _refer = (value == null ? XmlQualifiedName.Empty : value); }
        }
    }
}
