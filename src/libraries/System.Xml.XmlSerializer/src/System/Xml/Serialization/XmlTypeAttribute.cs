// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class XmlTypeAttribute : System.Attribute
    {
        private bool _includeInSchema = true;
        private bool _anonymousType;
        private string _ns;
        private string _typeName;

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.XmlTypeAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeAttribute()
        {
        }

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.XmlTypeAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.AnonymousType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool AnonymousType
        {
            get { return _anonymousType; }
            set { _anonymousType = value; }
        }

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.IncludeInSchema"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IncludeInSchema
        {
            get { return _includeInSchema; }
            set { _includeInSchema = value; }
        }

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return _typeName == null ? string.Empty : _typeName; }
            set { _typeName = value; }
        }

        /// <include file='doc\XmlTypeAttribute.uex' path='docs/doc[@for="XmlTypeAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }
    }
}
