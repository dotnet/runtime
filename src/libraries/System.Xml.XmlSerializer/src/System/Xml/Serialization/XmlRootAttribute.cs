// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class XmlRootAttribute : System.Attribute
    {
        private string _elementName;
        private string _ns;
        private string _dataType;
        private bool _nullable = true;
        private bool _nullableSpecified;

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.XmlRootAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlRootAttribute()
        {
        }

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.XmlRootAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlRootAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }

        /// <include file='doc\XmlRootAttribute.uex' path='docs/doc[@for="XmlRootAttribute.IsNullable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set
            {
                _nullable = value;
                _nullableSpecified = true;
            }
        }

        internal bool IsNullableSpecified
        {
            get { return _nullableSpecified; }
        }

        internal string Key
        {
            get { return (_ns == null ? String.Empty : _ns) + ":" + ElementName + ":" + _nullable.ToString(); }
        }
    }
}
