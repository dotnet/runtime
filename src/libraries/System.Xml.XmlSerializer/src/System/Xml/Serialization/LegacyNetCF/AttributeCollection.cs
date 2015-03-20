// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal enum LiteralAttributeFlags
    {
        Enum = 0x1,
        Array = 0x2,
        Text = 0x4,
        ArrayItems = 0x8,
        Elements = 0x10,
        Attribute = 0x20,
        Root = 0x40,
        Type = 0x80,
        AnyElements = 0x100,
        AnyAttribute = 0x200,
        ChoiceIdentifier = 0x400,
        XmlnsDeclarations = 0x800,
    }




    internal enum EncodedAttributeFlags
    {
        Enum = 0x1,
        Type = 0x2,
        Element = 0x4,
        Attribute = 0x8,
    }

    internal abstract class SharedAttributes
    {
        private static Type s_xmlIncludeType = typeof(XmlIncludeAttribute);
#if !FEATURE_LEGACYNETCF
        private static Type k_soapIncludeType = typeof(SoapIncludeAttribute);
#endif
        private static Type s_defaultValueType = typeof(DefaultValueAttribute);

        public List<Type> LiteralIncludedTypes;
        public List<Type> EncodedIncludedTypes;
        public DefaultValueAttribute DefaultValue;

        public SharedAttributes() { }

        protected void processAttribute(object attr)
        {
            Type type = attr.GetType();
            if (type == s_xmlIncludeType)
            {
                if (LiteralIncludedTypes == null)
                    LiteralIncludedTypes = new List<Type>(4);
                LiteralIncludedTypes.Add(((XmlIncludeAttribute)attr).Type);
            }
#if !FEATURE_LEGACYNETCF
            else if( type == k_soapIncludeType ) {
                if( EncodedIncludedTypes == null )
                    EncodedIncludedTypes = new List<Type>(4);
                EncodedIncludedTypes.Add( ((SoapIncludeAttribute)attr).Type );
            }
#endif
            else if (type == s_defaultValueType)
            {
                Debug.Assert(DefaultValue == null, "Too many DefaultValues");
                DefaultValue = (DefaultValueAttribute)attr;
            }
        }
    }

    internal class LiteralAttributes : SharedAttributes
    {
        private static Type s_ignoreType = typeof(XmlIgnoreAttribute);
        private static Type s_choiceType = typeof(XmlChoiceIdentifierAttribute);
        private static Type s_elementType = typeof(XmlElementAttribute);
        private static Type s_attributeType = typeof(XmlAttributeAttribute);
        private static Type s_arrayType = typeof(XmlArrayAttribute);
        private static Type s_arrayItemType = typeof(XmlArrayItemAttribute);
        private static Type s_nsDeclType = typeof(XmlNamespaceDeclarationsAttribute);
        private static Type s_textType = typeof(XmlTextAttribute);
        private static Type s_anyAttrType = typeof(XmlAnyAttributeAttribute);
        private static Type s_anyEltType = typeof(XmlAnyElementAttribute);
        private static Type s_enumType = typeof(XmlEnumAttribute);

        public bool Ignore;
        public XmlChoiceIdentifierAttribute XmlChoiceIdentifier;
        public XmlElementAttributes XmlElements;
        public XmlAttributeAttribute XmlAttribute;
        public XmlArrayAttribute XmlArray;
        public XmlArrayItemAttributes XmlArrayItems;
        public XmlNamespaceDeclarationsAttribute XmlNamespaceDeclaration;
        public XmlTextAttribute XmlText;
        public XmlAnyAttributeAttribute XmlAnyAttribute;
        public XmlAnyElementAttributes XmlAnyElements;
        public bool Xmlns;
        public XmlEnumAttribute XmlEnum;

        internal LiteralAttributeFlags XmlFlags
        {
            get
            {
                LiteralAttributeFlags flags = 0;
                if (XmlElements != null && XmlElements.Count > 0) flags |= LiteralAttributeFlags.Elements;
                if (XmlArrayItems != null && XmlArrayItems.Count > 0) flags |= LiteralAttributeFlags.ArrayItems;
                if (XmlAnyElements != null && XmlAnyElements.Count > 0) flags |= LiteralAttributeFlags.AnyElements;
                if (XmlArray != null) flags |= LiteralAttributeFlags.Array;
                if (XmlAttribute != null) flags |= LiteralAttributeFlags.Attribute;
                if (XmlText != null) flags |= LiteralAttributeFlags.Text;
                if (XmlEnum != null) flags |= LiteralAttributeFlags.Enum;
                if (XmlAnyAttribute != null) flags |= LiteralAttributeFlags.AnyAttribute;
                if (XmlChoiceIdentifier != null) flags |= LiteralAttributeFlags.ChoiceIdentifier;
                if (Xmlns) flags |= LiteralAttributeFlags.XmlnsDeclarations;
                return flags;
            }
        }

        internal const LiteralAttributeFlags XmlElementFlags = LiteralAttributeFlags.Elements | LiteralAttributeFlags.Text | LiteralAttributeFlags.AnyElements | LiteralAttributeFlags.ChoiceIdentifier;
        internal const LiteralAttributeFlags XmlAttributeFlags = LiteralAttributeFlags.Attribute | LiteralAttributeFlags.AnyAttribute;
        internal const LiteralAttributeFlags XmlArrayFlags = LiteralAttributeFlags.Array | LiteralAttributeFlags.ArrayItems;

        public LiteralAttributes(MemberInfo memberInfo)
        {
            foreach (object attr in memberInfo.GetCustomAttributes(false))
            {
                Type type = attr.GetType();
                if (type == s_ignoreType)
                {
                    Debug.Assert(false == Ignore, "Too many ignores");
                    Ignore = true;
                }
                else if (type == s_choiceType)
                {
                    Debug.Assert(XmlChoiceIdentifier == null, "Too many XCIA");
                    XmlChoiceIdentifier = (XmlChoiceIdentifierAttribute)attr;
                }
                else if (type == s_elementType)
                {
                    if (XmlElements == null)
                        XmlElements = new XmlElementAttributes();
                    XmlElements.Add((XmlElementAttribute)attr);
                }
                else if (type == s_attributeType)
                {
                    Debug.Assert(XmlAttribute == null, "Too many XAAs");
                    XmlAttribute = (XmlAttributeAttribute)attr;
                }
                else if (type == s_arrayType)
                {
                    Debug.Assert(XmlArray == null, "Too many XAAs");
                    XmlArray = (XmlArrayAttribute)attr;
                }
                else if (type == s_arrayItemType)
                {
                    if (XmlArrayItems == null)
                        XmlArrayItems = new XmlArrayItemAttributes();
                    XmlArrayItems.Add((XmlArrayItemAttribute)attr);
                }
                else if (type == s_nsDeclType)
                {
                    Debug.Assert(XmlNamespaceDeclaration == null, "Too many XNDAs");
                    XmlNamespaceDeclaration = (XmlNamespaceDeclarationsAttribute)attr;
                    Xmlns = true;
                }
                else if (type == s_textType)
                {
                    Debug.Assert(XmlText == null, "Too many XTAs");
                    XmlText = (XmlTextAttribute)attr;
                }
                else if (type == s_anyAttrType)
                {
                    Debug.Assert(XmlAnyAttribute == null, "Too many XAAAs");
                    XmlAnyAttribute = (XmlAnyAttributeAttribute)attr;
                }
                else if (type == s_anyEltType)
                {
                    if (XmlAnyElements == null)
                        XmlAnyElements = new XmlAnyElementAttributes();
                    XmlAnyElements.Add((XmlAnyElementAttribute)attr);
                }
                else if (type == s_enumType)
                {
                    XmlEnum = (XmlEnumAttribute)attr;
                }
                else
                    base.processAttribute(attr);
            }
        }

        public LiteralAttributes(MemberInfo memberInfo, XmlAttributes xmlAtts) :
            this(memberInfo)
        {
            if (xmlAtts == null)
                return;

            Ignore = xmlAtts.XmlIgnore;
            if (!Ignore)
            {
                XmlChoiceIdentifier = xmlAtts.XmlChoiceIdentifier;
                XmlAttribute = xmlAtts.XmlAttribute;
                XmlArray = xmlAtts.XmlArray;
                XmlText = xmlAtts.XmlText;
                XmlEnum = xmlAtts.XmlEnum;
                DefaultValue = (xmlAtts.XmlDefaultValue == null) ?
                    null :
                    new DefaultValueAttribute(xmlAtts.XmlDefaultValue);

                Xmlns = xmlAtts.Xmlns;
                if (Xmlns)
                {
                    object[] attrs = memberInfo.GetCustomAttributes(s_nsDeclType, false).ToArray();
                    XmlNamespaceDeclaration = (XmlNamespaceDeclarationsAttribute)(attrs.Length > 0 ? attrs[0] : null);
                }
                else
                {
                    XmlNamespaceDeclaration = null;
                }

                // Use if statements here so that the XmlElements collection populated by reflection
                // is eliminated only if the app developer has provided substitute XmlElementAttribute's.
                // Ditto for the XmlArrayItems and XmlAnyElements.
                if (xmlAtts.XmlElements.Count > 0)
                {
                    XmlElements = xmlAtts.XmlElements;
                }

                if (xmlAtts.XmlArrayItems.Count > 0)
                {
                    XmlArrayItems = xmlAtts.XmlArrayItems;
                }

                XmlAnyAttribute = xmlAtts.XmlAnyAttribute;

                if (xmlAtts.XmlAnyElements.Count > 0)
                {
                    XmlAnyElements = xmlAtts.XmlAnyElements;
                }
            }
        }
    }

    internal class EncodedAttributes : SharedAttributes
    {
#if !FEATURE_LEGACYNETCF
        private static Type k_ignoreType = typeof(SoapIgnoreAttribute);
#endif
        private static Type s_eltType = typeof(SoapElementAttribute);
        private static Type s_attrType = typeof(SoapAttributeAttribute);
        private static Type s_enumType = typeof(SoapEnumAttribute);
        public bool Ignore;
        public SoapElementAttribute SoapElement;
        public SoapAttributeAttribute SoapAttribute;
        public SoapEnumAttribute SoapEnum;

        internal EncodedAttributeFlags SoapFlags
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                EncodedAttributeFlags flags = 0;
                if (SoapElement != null) flags |= EncodedAttributeFlags.Element;
                if (SoapAttribute != null) flags |= EncodedAttributeFlags.Attribute;
                if (SoapEnum != null) flags |= EncodedAttributeFlags.Enum;
                return flags;
            }
        }

        public EncodedAttributes(MemberInfo memberInfo)
        {
            foreach (object attr in memberInfo.GetCustomAttributes(false))
            {
                Type type = attr.GetType();
#if !FEATURE_LEGACYNETCF
                if( type == k_ignoreType ) {
                    Debug.Assert( Ignore == false, "Too many SIAs" );
                    Ignore = true;
                }
                else
#endif
                if (type == s_eltType)
                {
                    Debug.Assert(SoapElement == null, "Too many SEAs");
                    SoapElement = (SoapElementAttribute)attr;
                }
                else if (type == s_attrType)
                {
                    Debug.Assert(SoapAttribute == null, "Too many SAAs");
                    SoapAttribute = (SoapAttributeAttribute)attr;
                }
                else if (type == s_enumType)
                {
                    SoapEnum = (SoapEnumAttribute)attr;
                }
                else
                    base.processAttribute(attr);
            }
        }

        public EncodedAttributes(MemberInfo memberInfo, SoapAttributes soapAtts) :
            this(memberInfo)
        {
            if (soapAtts == null)
                return;

            Ignore = soapAtts.SoapIgnore;
            if (!Ignore)
            {
                SoapElement = soapAtts.SoapElement;
                SoapAttribute = soapAtts.SoapAttribute;
                SoapEnum = soapAtts.SoapEnum;
                DefaultValue = (soapAtts.SoapDefaultValue == null) ?
                    null :
                    new DefaultValueAttribute(soapAtts.SoapDefaultValue);
            }
        }
    }

    internal class TypeAttributes : SharedAttributes
    {
        private static Type s_xTypeAttr = typeof(XmlTypeAttribute);
        private static Type s_xRootAttr = typeof(XmlRootAttribute);
        private static Type s_sTypeAttr = typeof(SoapTypeAttribute);
        private static Type s_flagsAttr = typeof(FlagsAttribute);
        public XmlTypeAttribute XmlType;
        public XmlRootAttribute XmlRoot;
        public SoapTypeAttribute SoapType;
        public bool IsFlag;

        internal LiteralAttributeFlags XmlFlags
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                LiteralAttributeFlags flags = 0;
                if (XmlRoot != null) flags |= LiteralAttributeFlags.Root;
                if (XmlType != null) flags |= LiteralAttributeFlags.Type;
                return flags;
            }
        }

        internal EncodedAttributeFlags SoapFlags
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                EncodedAttributeFlags flags = 0;
                if (SoapType != null) flags |= EncodedAttributeFlags.Type;
                return flags;
            }
        }

        public TypeAttributes(MemberInfo memberInfo)
        {
            foreach (object attr in memberInfo.GetCustomAttributes(false))
            {
                Type type = attr.GetType();
                if (type == s_xTypeAttr)
                {
                    Debug.Assert(XmlType == null, "Too many XTAs");
                    XmlType = (XmlTypeAttribute)attr;
                }
                else if (type == s_xRootAttr)
                {
                    Debug.Assert(XmlRoot == null, "Too many XRAs");
                    XmlRoot = (XmlRootAttribute)attr;
                }
                else if (type == s_sTypeAttr)
                {
                    Debug.Assert(SoapType == null, "Too many STAs");
                    SoapType = (SoapTypeAttribute)attr;
                }
                else if (type == s_flagsAttr)
                {
                    Debug.Assert(IsFlag == false, "too many Flags");
                    IsFlag = true;
                }
                else
                    base.processAttribute(attr);
            }
        }

        public TypeAttributes(MemberInfo memberInfo, XmlAttributes xmlAtts) :
            this(memberInfo)
        {
            if (xmlAtts == null)
                return;

            XmlType = xmlAtts.XmlType;
            XmlRoot = xmlAtts.XmlRoot;
        }

        public TypeAttributes(MemberInfo memberInfo, SoapAttributes soapAtts) :
            this(memberInfo)
        {
            if (soapAtts == null)
                return;

            SoapType = soapAtts.SoapType;
        }
    }
}
