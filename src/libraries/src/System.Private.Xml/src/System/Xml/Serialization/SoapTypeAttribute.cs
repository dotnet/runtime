// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class SoapTypeAttribute : System.Attribute
    {
        private string _ns;
        private string _typeName;
        private bool _includeInSchema = true;

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.SoapTypeAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapTypeAttribute()
        {
        }

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.SoapTypeAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.SoapTypeAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapTypeAttribute(string typeName, string ns)
        {
            _typeName = typeName;
            _ns = ns;
        }

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.IncludeInSchema"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IncludeInSchema
        {
            get { return _includeInSchema; }
            set { _includeInSchema = value; }
        }

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return _typeName == null ? string.Empty : _typeName; }
            set { _typeName = value; }
        }

        /// <include file='doc\SoapTypeAttribute.uex' path='docs/doc[@for="SoapTypeAttribute.Namespace"]/*' />
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
