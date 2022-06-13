// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapAttributeAttribute : System.Attribute
    {
        private string? _attributeName;
        private string? _ns;
        private string? _dataType;

        public SoapAttributeAttribute()
        {
        }

        public SoapAttributeAttribute(string attributeName)
        {
            _attributeName = attributeName;
        }

        [AllowNull]
        public string AttributeName
        {
            get { return _attributeName ?? string.Empty; }
            set { _attributeName = value; }
        }

        public string? Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        [AllowNull]
        public string DataType
        {
            get { return _dataType ?? string.Empty; }
            set { _dataType = value; }
        }
    }
}
