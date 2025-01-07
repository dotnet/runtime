// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;

namespace System.Xml.Serialization
{
    internal enum SoapAttributeFlags
    {
        Enum = 0x1,
        Type = 0x2,
        Element = 0x4,
        Attribute = 0x8,
    }

    public class SoapAttributes
    {
        private bool _soapIgnore;
        private SoapTypeAttribute? _soapType;
        private SoapElementAttribute? _soapElement;
        private SoapAttributeAttribute? _soapAttribute;
        private SoapEnumAttribute? _soapEnum;
        private object? _soapDefaultValue;

        public SoapAttributes()
        {
        }

        public SoapAttributes(ICustomAttributeProvider provider)
        {
            object[] attrs = provider.GetCustomAttributes(false);
            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is SoapIgnoreAttribute || attrs[i] is ObsoleteAttribute)
                {
                    _soapIgnore = true;
                    break;
                }
                else if (attrs[i] is SoapElementAttribute)
                {
                    _soapElement = (SoapElementAttribute)attrs[i];
                }
                else if (attrs[i] is SoapAttributeAttribute)
                {
                    _soapAttribute = (SoapAttributeAttribute)attrs[i];
                }
                else if (attrs[i] is SoapTypeAttribute)
                {
                    _soapType = (SoapTypeAttribute)attrs[i];
                }
                else if (attrs[i] is SoapEnumAttribute)
                {
                    _soapEnum = (SoapEnumAttribute)attrs[i];
                }
                else if (attrs[i] is DefaultValueAttribute)
                {
                    _soapDefaultValue = ((DefaultValueAttribute)attrs[i]).Value;
                }
            }
            if (_soapIgnore)
            {
                _soapElement = null;
                _soapAttribute = null;
                _soapType = null;
                _soapEnum = null;
                _soapDefaultValue = null;
            }
        }

        internal SoapAttributeFlags SoapFlags
        {
            get
            {
                SoapAttributeFlags flags = 0;
                if (_soapElement != null) flags |= SoapAttributeFlags.Element;
                if (_soapAttribute != null) flags |= SoapAttributeFlags.Attribute;
                if (_soapEnum != null) flags |= SoapAttributeFlags.Enum;
                if (_soapType != null) flags |= SoapAttributeFlags.Type;
                return flags;
            }
        }

        internal SoapAttributeFlags GetSoapFlags()
        {
            return SoapFlags;
        }

        public SoapTypeAttribute? SoapType
        {
            get { return _soapType; }
            set { _soapType = value; }
        }

        public SoapEnumAttribute? SoapEnum
        {
            get { return _soapEnum; }
            set { _soapEnum = value; }
        }

        public bool SoapIgnore
        {
            get { return _soapIgnore; }
            set { _soapIgnore = value; }
        }

        public SoapElementAttribute? SoapElement
        {
            get { return _soapElement; }
            set { _soapElement = value; }
        }

        public SoapAttributeAttribute? SoapAttribute
        {
            get { return _soapAttribute; }
            set { _soapAttribute = value; }
        }

        public object? SoapDefaultValue
        {
            get { return _soapDefaultValue; }
            set { _soapDefaultValue = value; }
        }
    }
}
