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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class XmlAttributeAttribute : System.Attribute
    {
        private string? _attributeName;
        private Type? _type;
        private string? _ns;
        private string? _dataType;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private char _separator;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(string? attributeName)
        {
            _attributeName = attributeName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(Type? type)
        {
            _type = type;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute(string? attributeName, Type? type)
        {
            _attributeName = attributeName;
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
        public string AttributeName
        {
            get { return _attributeName ?? string.Empty; }
            set { _attributeName = value; }
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
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <summary>Gets or sets the separator character used when serializing an array as an XML attribute value list.</summary>
        /// <remarks>
        /// When set to the default value of <c>'\0'</c> (null character), the space character is used as separator,
        /// preserving the existing behavior for <see cref="XmlAttributeAttribute"/> on array-typed members.
        /// Set to a non-default value to override the separator.
        /// </remarks>
        public char Separator
        {
            get { return _separator; }
            set { _separator = value; }
        }
    }
}
