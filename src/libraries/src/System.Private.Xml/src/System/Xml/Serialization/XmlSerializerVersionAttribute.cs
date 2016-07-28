// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;


    /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class XmlSerializerVersionAttribute : System.Attribute
    {
        private string _mvid;
        private string _serializerVersion;
        private string _ns;
        private Type _type;

        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.XmlSerializerVersionAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerVersionAttribute()
        {
        }

        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.XmlSerializerAssemblyAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerVersionAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.ParentAssemblyId"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ParentAssemblyId
        {
            get { return _mvid; }
            set { _mvid = value; }
        }

        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.ParentAssemblyId"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Version
        {
            get { return _serializerVersion; }
            set { _serializerVersion = value; }
        }


        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlSerializerVersionAttribute.uex' path='docs/doc[@for="XmlSerializerVersionAttribute.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}
