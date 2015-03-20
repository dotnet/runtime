// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;


namespace System.Xml.Serialization.LegacyNetCF
{
    internal enum SoapAttributeFlags
    {
        Enum = 0x1,
        Type = 0x2,
        Element = 0x4,
        Attribute = 0x8,
    }

    [System.Security.FrameworkVisibilitySilverlightInternal]
    public class SoapAttributes
    {
        private bool _soapIgnore;
        private SoapTypeAttribute _soapType;
        private SoapElementAttribute _soapElement;
        private SoapAttributeAttribute _soapAttribute;
        private SoapEnumAttribute _soapEnum;
        private object _soapDefaultValue = null;

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public SoapAttributes()
        {
        }


        public SoapTypeAttribute SoapType
        {
            get { return _soapType; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapType = value; }
        }

        public SoapEnumAttribute SoapEnum
        {
            get { return _soapEnum; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapEnum = value; }
        }

        public bool SoapIgnore
        {
            get { return _soapIgnore; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapIgnore = value; }
        }

        public SoapElementAttribute SoapElement
        {
            get { return _soapElement; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapElement = value; }
        }

        public SoapAttributeAttribute SoapAttribute
        {
            get { return _soapAttribute; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapAttribute = value; }
        }

        public object SoapDefaultValue
        {
            get { return _soapDefaultValue; }
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { _soapDefaultValue = value; }
        }
    }
}
