// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaNotation.uex' path='docs/doc[@for="XmlSchemaNotation"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaNotation : XmlSchemaAnnotated
    {
        private string _name;
        private string _publicId;
        private string _systemId;
        private XmlQualifiedName _qname = XmlQualifiedName.Empty;

        /// <include file='doc\XmlSchemaNotation.uex' path='docs/doc[@for="XmlSchemaNotation.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlSchemaNotation.uex' path='docs/doc[@for="XmlSchemaNotation.Public"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("public")]
        public string Public
        {
            get { return _publicId; }
            set { _publicId = value; }
        }

        /// <include file='doc\XmlSchemaNotation.uex' path='docs/doc[@for="XmlSchemaNotation.System"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("system")]
        public string System
        {
            get { return _systemId; }
            set { _systemId = value; }
        }

        [XmlIgnore]
        internal XmlQualifiedName QualifiedName
        {
            get { return _qname; }
            set { _qname = value; }
        }

        [XmlIgnore]
        internal override string NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }
    }
}
