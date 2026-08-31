// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class XmlRootAttribute : System.Attribute
    {
        private string? _elementName;
        private string? _ns;
        private string? _dataType;
        private bool _nullable = true;
        private bool _nullableSpecified;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlRootAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlRootAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        internal string Key
        {
            get { return $"{_ns ?? string.Empty}:{ElementName}:{_nullable}"; }
        }

        internal string GetKey()
        {
            return this.Key;
        }
    }
}
