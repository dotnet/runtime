// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;


namespace System.Xml.Serialization.LegacyNetCF
{
    [System.Security.FrameworkVisibilitySilverlightInternal]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapAttributeAttribute : System.Attribute
    {
        private string _attributeName;
        private string _ns;
        private string _dataType;

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapAttributeAttribute()
        {
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapAttributeAttribute(string attributeName)
        {
            _attributeName = attributeName;
        }

        public string AttributeName
        {
            get { return _attributeName == null ? string.Empty : _attributeName; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _attributeName = value; }
        }

        public string Namespace
        {
            get { return _ns; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _ns = value; }
        }

        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _dataType = value; }
        }
    }
}
