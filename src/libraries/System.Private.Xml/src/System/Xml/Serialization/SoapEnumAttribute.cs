// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [AttributeUsage(AttributeTargets.Field)]
    public class SoapEnumAttribute : System.Attribute
    {
        private string? _name;

        public SoapEnumAttribute()
        {
        }

        public SoapEnumAttribute(string name)
        {
            _name = name;
        }

        [AllowNull]
        public string Name
        {
            get { return _name ?? string.Empty; }
            set { _name = value; }
        }
    }
}
