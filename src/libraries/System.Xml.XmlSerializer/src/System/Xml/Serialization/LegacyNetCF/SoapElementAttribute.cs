// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;


namespace System.Xml.Serialization.LegacyNetCF
{
    [System.Security.FrameworkVisibilitySilverlightInternal]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapElementAttribute : System.Attribute
    {
        private string _elementName;
        private string _dataType;
        private bool _nullable;

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapElementAttribute()
        {
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapElementAttribute(string elementName)
        {
            _elementName = elementName;
        }

        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _elementName = value; }
        }

        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _dataType = value; }
        }

        public bool IsNullable
        {
            get { return _nullable; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _nullable = value; }
        }
    }
}
