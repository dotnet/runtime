// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class XmlArrayItemAttribute : System.Attribute
    {
        private string _elementName;
        private Type _type;
        private string _ns;
        private string _dataType;
        private bool _nullable;
        private bool _nullableSpecified = false;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private int _nestingLevel;

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.XmlArrayItemAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute()
        {
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.XmlArrayItemAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.XmlArrayItemAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.XmlArrayItemAttribute3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttribute(string elementName, Type type)
        {
            _elementName = elementName;
            _type = type;
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.Type"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.NestingLevel"]/*' />
        public int NestingLevel
        {
            get { return _nestingLevel; }
            set { _nestingLevel = value; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.IsNullable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; _nullableSpecified = true; }
        }

        internal bool IsNullableSpecified
        {
            get { return _nullableSpecified; }
        }

        /// <include file='doc\XmlArrayItemAttribute.uex' path='docs/doc[@for="XmlArrayItemAttribute.Form"]/*' />
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
