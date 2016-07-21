// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlIncludeAttribute.uex' path='docs/doc[@for="XmlIncludeAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class XmlIncludeAttribute : System.Attribute
    {
        private Type _type;

        /// <include file='doc\XmlIncludeAttribute.uex' path='docs/doc[@for="XmlIncludeAttribute.XmlIncludeAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlIncludeAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlIncludeAttribute.uex' path='docs/doc[@for="XmlIncludeAttribute.Type"]/*' />
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
