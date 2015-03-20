// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;


namespace System.Xml.Serialization.LegacyNetCF
{
    [System.Security.FrameworkVisibilitySilverlightInternal]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class SoapTypeAttribute : System.Attribute
    {
        private string _ns;
        private string _typeName;

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapTypeAttribute()
        {
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapTypeAttribute(string typeName, string ns)
        {
            _typeName = typeName;
            _ns = ns;
        }


        public string TypeName
        {
            get { return _typeName == null ? string.Empty : _typeName; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _typeName = value; }
        }

        public string Namespace
        {
            get { return _ns; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _ns = value; }
        }
    }
}
