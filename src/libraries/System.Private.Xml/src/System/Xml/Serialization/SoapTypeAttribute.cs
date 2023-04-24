// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class SoapTypeAttribute : System.Attribute
    {
        private string? _ns;
        private string? _typeName;
        private bool _includeInSchema = true;

        public SoapTypeAttribute()
        {
        }

        public SoapTypeAttribute(string? typeName)
        {
            _typeName = typeName;
        }

        public SoapTypeAttribute(string? typeName, string? ns)
        {
            _typeName = typeName;
            _ns = ns;
        }

        public bool IncludeInSchema
        {
            get { return _includeInSchema; }
            set { _includeInSchema = value; }
        }

        [AllowNull]
        public string TypeName
        {
            get { return _typeName ?? string.Empty; }
            set { _typeName = value; }
        }

        public string? Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }
    }
}
