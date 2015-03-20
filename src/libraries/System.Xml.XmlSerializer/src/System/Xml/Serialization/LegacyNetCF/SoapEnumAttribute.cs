// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;


namespace System.Xml.Serialization.LegacyNetCF
{
    [System.Security.FrameworkVisibilitySilverlightInternal]
    [AttributeUsage(AttributeTargets.Field)]
    public class SoapEnumAttribute : System.Attribute
    {
        private string _name;

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapEnumAttribute()
        {
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapEnumAttribute(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name == null ? string.Empty : _name; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _name = value; }
        }
    }
}
