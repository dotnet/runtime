// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapElementAttribute : System.Attribute
    {
        private string _elementName;
        private string _dataType;
        private bool _nullable;

        /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute.SoapElementAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapElementAttribute()
        {
        }

        /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute.SoapElementAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapElementAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }

        /// <include file='doc\SoapElementAttribute.uex' path='docs/doc[@for="SoapElementAttribute.IsNullable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; }
        }
    }
}
