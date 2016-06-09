// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaGroup.uex' path='docs/doc[@for="XmlSchemaGroup"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaGroup : XmlSchemaAnnotated
    {
        private string _name;
        private XmlSchemaGroupBase _particle;
        private XmlSchemaParticle _canonicalParticle;
        private XmlQualifiedName _qname = XmlQualifiedName.Empty;
        private XmlSchemaGroup _redefined;
        private int _selfReferenceCount;

        /// <include file='doc\XmlSchemaGroup.uex' path='docs/doc[@for="XmlSchemaGroup.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlSchemaGroup.uex' path='docs/doc[@for="XmlSchemaGroup.Particle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("choice", typeof(XmlSchemaChoice)),
         XmlElement("all", typeof(XmlSchemaAll)),
         XmlElement("sequence", typeof(XmlSchemaSequence))]
        public XmlSchemaGroupBase Particle
        {
            get { return _particle; }
            set { _particle = value; }
        }

        [XmlIgnore]
        public XmlQualifiedName QualifiedName
        {
            get { return _qname; }
        }

        [XmlIgnore]
        internal XmlSchemaParticle CanonicalParticle
        {
            get { return _canonicalParticle; }
            set { _canonicalParticle = value; }
        }

        [XmlIgnore]
        internal XmlSchemaGroup Redefined
        {
            get { return _redefined; }
            set { _redefined = value; }
        }

        [XmlIgnore]
        internal int SelfReferenceCount
        {
            get { return _selfReferenceCount; }
            set { _selfReferenceCount = value; }
        }

        [XmlIgnore]
        internal override string NameAttribute
        {
            get { return Name; }
            set { Name = value; }
        }

        internal void SetQualifiedName(XmlQualifiedName value)
        {
            _qname = value;
        }

        internal override XmlSchemaObject Clone()
        {
            System.Diagnostics.Debug.Assert(false, "Should never call Clone() on XmlSchemaGroup. Call Clone(XmlSchema) instead.");
            return Clone(null);
        }

        internal XmlSchemaObject Clone(XmlSchema parentSchema)
        {
            XmlSchemaGroup newGroup = (XmlSchemaGroup)MemberwiseClone();
            if (XmlSchemaComplexType.HasParticleRef(_particle, parentSchema))
            {
                newGroup._particle = XmlSchemaComplexType.CloneParticle(_particle, parentSchema) as XmlSchemaGroupBase;
            }
            newGroup._canonicalParticle = XmlSchemaParticle.Empty;
            return newGroup;
        }
    }
}
