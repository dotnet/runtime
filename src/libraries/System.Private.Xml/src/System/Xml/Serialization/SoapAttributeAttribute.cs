// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapAttributeAttribute : System.Attribute
    {
        private string _attributeName;
        private string _ns;
        private string _dataType;

        public SoapAttributeAttribute()
        {
        }

        public SoapAttributeAttribute(string attributeName)
        {
            _attributeName = attributeName;
        }

        public string AttributeName
        {
            get { return _attributeName == null ? string.Empty : _attributeName; }
            set { _attributeName = value; }
        }

        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }
    }
}
