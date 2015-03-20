// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class XmlAttributeAttribute : System.Attribute
    {
        private string _attributeName;
        private Type _type;
        private string _ns;
        private string _dataType;
        private XmlSchemaForm _form = XmlSchemaForm.None;

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.XmlAttributeAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute()
        {
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.XmlAttributeAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(string attributeName)
        {
            _attributeName = attributeName;
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.XmlAttributeAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.XmlAttributeAttribute3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(string attributeName, Type type)
        {
            _attributeName = attributeName;
            _type = type;
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.Type"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.AttributeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string AttributeName
        {
            get { return _attributeName == null ? string.Empty : _attributeName; }
            set { _attributeName = value; }
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }

        /// <include file='doc\XmlAttributeAttribute.uex' path='docs/doc[@for="XmlAttributeAttribute.Form"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }
    }
}
