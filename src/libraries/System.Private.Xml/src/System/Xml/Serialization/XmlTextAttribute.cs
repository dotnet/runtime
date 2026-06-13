// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class XmlTextAttribute : System.Attribute
    {
        private Type? _type;
        private string? _dataType;
        private char _separator;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTextAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTextAttribute(Type? type)
        {
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
        public string DataType
        {
            get { return _dataType ?? string.Empty; }
            set { _dataType = value; }
        }

        /// <summary>Gets or sets the separator character used when serializing an array of strings to XML text content.</summary>
        /// <remarks>
        /// When set to a non-default value (i.e., not the null character <c>'\0'</c>), string array items are serialized
        /// with this character as separator, and deserialization splits the text content on this character.
        /// The default value of <c>'\0'</c> means no separator: array items are concatenated without any separator,
        /// preserving the existing behavior.
        /// </remarks>
        public char Separator
        {
            get { return _separator; }
            set { _separator = value; }
        }
    }
}
