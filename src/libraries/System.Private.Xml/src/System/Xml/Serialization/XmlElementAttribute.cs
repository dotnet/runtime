// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class XmlElementAttribute : System.Attribute
    {
        private string? _elementName;
        private Type? _type;
        private string? _ns;
        private string? _dataType;
        private bool _nullable;
        private bool _nullableSpecified;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private int _order = -1;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(string? elementName)
        {
            _elementName = elementName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(Type? type)
        {
            _type = type;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(string? elementName, Type? type)
        {
            _elementName = elementName;
            _type = type;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type? Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [AllowNull]
        public string ElementName
        {
            get { return _elementName ?? string.Empty; }
            set { _elementName = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [AllowNull]
        public string DataType
        {
            get { return _dataType ?? string.Empty; }
            set { _dataType = value; }
        }

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

        internal bool GetIsNullableSpecified()
        {
            return IsNullableSpecified;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Order
        {
            get { return _order; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.XmlDisallowNegativeValues, "Order");
                _order = value;
            }
        }
    }
}
