// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlTextAttribute.uex' path='docs/doc[@for="XmlTextAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class XmlTextAttribute : System.Attribute
    {
        private Type _type;
        private string _dataType;

        /// <include file='doc\XmlTextAttribute.uex' path='docs/doc[@for="XmlTextAttribute.XmlTextAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTextAttribute()
        {
        }

        /// <include file='doc\XmlTextAttribute.uex' path='docs/doc[@for="XmlTextAttribute.XmlTextAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTextAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlTextAttribute.uex' path='docs/doc[@for="XmlTextAttribute.Type"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlTextAttribute.uex' path='docs/doc[@for="XmlTextAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }
    }
}
