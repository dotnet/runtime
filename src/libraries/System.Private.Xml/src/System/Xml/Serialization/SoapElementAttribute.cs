// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapElementAttribute : System.Attribute
    {
        private string? _elementName;
        private string? _dataType;
        private bool _nullable;

        public SoapElementAttribute()
        {
        }

        public SoapElementAttribute(string? elementName)
        {
            _elementName = elementName;
        }

        [AllowNull]
        public string ElementName
        {
            get { return _elementName ?? string.Empty; }
            set { _elementName = value; }
        }

        [AllowNull]
        public string DataType
        {
            get { return _dataType ?? string.Empty; }
            set { _dataType = value; }
        }

        public bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; }
        }
    }
}
