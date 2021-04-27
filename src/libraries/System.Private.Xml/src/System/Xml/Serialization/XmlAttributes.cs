// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.ComponentModel;

namespace System.Xml.Serialization
{
    internal enum XmlAttributeFlags
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

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlAttributes
    {
        private readonly XmlElementAttributes _xmlElements = new XmlElementAttributes();
        private readonly XmlArrayItemAttributes _xmlArrayItems = new XmlArrayItemAttributes();
        private readonly XmlAnyElementAttributes _xmlAnyElements = new XmlAnyElementAttributes();
        private XmlArrayAttribute? _xmlArray;
        private XmlAttributeAttribute? _xmlAttribute;
        private XmlTextAttribute? _xmlText;
        private XmlEnumAttribute? _xmlEnum;
        private bool _xmlIgnore;
        private bool _xmlns;
        private object? _xmlDefaultValue;
        private XmlRootAttribute? _xmlRoot;
        private XmlTypeAttribute? _xmlType;
        private XmlAnyAttributeAttribute? _xmlAnyAttribute;
        private readonly XmlChoiceIdentifierAttribute? _xmlChoiceIdentifier;


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes()
        {
        }

        internal XmlAttributeFlags XmlFlags
        {
            get
            {
                XmlAttributeFlags flags = 0;
                if (_xmlElements.Count > 0) flags |= XmlAttributeFlags.Elements;
                if (_xmlArrayItems.Count > 0) flags |= XmlAttributeFlags.ArrayItems;
                if (_xmlAnyElements.Count > 0) flags |= XmlAttributeFlags.AnyElements;
                if (_xmlArray != null) flags |= XmlAttributeFlags.Array;
                if (_xmlAttribute != null) flags |= XmlAttributeFlags.Attribute;
                if (_xmlText != null) flags |= XmlAttributeFlags.Text;
                if (_xmlEnum != null) flags |= XmlAttributeFlags.Enum;
                if (_xmlRoot != null) flags |= XmlAttributeFlags.Root;
                if (_xmlType != null) flags |= XmlAttributeFlags.Type;
                if (_xmlAnyAttribute != null) flags |= XmlAttributeFlags.AnyAttribute;
                if (_xmlChoiceIdentifier != null) flags |= XmlAttributeFlags.ChoiceIdentifier;
                if (_xmlns) flags |= XmlAttributeFlags.XmlnsDeclarations;
                return flags;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes(ICustomAttributeProvider provider)
        {
            object[] attrs = provider.GetCustomAttributes(false);

            // most generic <any/> matches everything
            XmlAnyElementAttribute? wildcard = null;
            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is XmlIgnoreAttribute || attrs[i] is ObsoleteAttribute)
                {
                    _xmlIgnore = true;
                    break;
                }
                else if (attrs[i] is XmlElementAttribute)
                {
                    _xmlElements.Add((XmlElementAttribute)attrs[i]);
                }
                else if (attrs[i] is XmlArrayItemAttribute)
                {
                    _xmlArrayItems.Add((XmlArrayItemAttribute)attrs[i]);
                }
                else if (attrs[i] is XmlAnyElementAttribute)
                {
                    XmlAnyElementAttribute any = (XmlAnyElementAttribute)attrs[i];
                    if ((any.Name == null || any.Name.Length == 0) && any.GetNamespaceSpecified() && any.Namespace == null)
                    {
                        // ignore duplicate wildcards
                        wildcard = any;
                    }
                    else
                    {
                        _xmlAnyElements.Add((XmlAnyElementAttribute)attrs[i]);
                    }
                }
                else if (attrs[i] is DefaultValueAttribute)
                {
                    _xmlDefaultValue = ((DefaultValueAttribute)attrs[i]).Value;
                }
                else if (attrs[i] is XmlAttributeAttribute)
                {
                    _xmlAttribute = (XmlAttributeAttribute)attrs[i];
                }
                else if (attrs[i] is XmlArrayAttribute)
                {
                    _xmlArray = (XmlArrayAttribute)attrs[i];
                }
                else if (attrs[i] is XmlTextAttribute)
                {
                    _xmlText = (XmlTextAttribute)attrs[i];
                }
                else if (attrs[i] is XmlEnumAttribute)
                {
                    _xmlEnum = (XmlEnumAttribute)attrs[i];
                }
                else if (attrs[i] is XmlRootAttribute)
                {
                    _xmlRoot = (XmlRootAttribute)attrs[i];
                }
                else if (attrs[i] is XmlTypeAttribute)
                {
                    _xmlType = (XmlTypeAttribute)attrs[i];
                }
                else if (attrs[i] is XmlAnyAttributeAttribute)
                {
                    _xmlAnyAttribute = (XmlAnyAttributeAttribute)attrs[i];
                }
                else if (attrs[i] is XmlChoiceIdentifierAttribute)
                {
                    _xmlChoiceIdentifier = (XmlChoiceIdentifierAttribute)attrs[i];
                }
                else if (attrs[i] is XmlNamespaceDeclarationsAttribute)
                {
                    _xmlns = true;
                }
            }
            if (_xmlIgnore)
            {
                _xmlElements.Clear();
                _xmlArrayItems.Clear();
                _xmlAnyElements.Clear();
                _xmlDefaultValue = null;
                _xmlAttribute = null;
                _xmlArray = null;
                _xmlText = null;
                _xmlEnum = null;
                _xmlType = null;
                _xmlAnyAttribute = null;
                _xmlChoiceIdentifier = null;
                _xmlns = false;
            }
            else
            {
                if (wildcard != null)
                {
                    _xmlAnyElements.Add(wildcard);
                }
            }
        }

        internal static object? GetAttr(MemberInfo memberInfo, Type attrType)
        {
            object[] attrs = memberInfo.GetCustomAttributes(attrType, false);
            if (attrs.Length == 0) return null;
            return attrs[0];
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttributes XmlElements
        {
            get { return _xmlElements; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributeAttribute? XmlAttribute
        {
            get { return _xmlAttribute; }
            set { _xmlAttribute = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlEnumAttribute? XmlEnum
        {
            get { return _xmlEnum; }
            set { _xmlEnum = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTextAttribute? XmlText
        {
            get { return _xmlText; }
            set { _xmlText = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayAttribute? XmlArray
        {
            get { return _xmlArray; }
            set { _xmlArray = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayItemAttributes XmlArrayItems
        {
            get { return _xmlArrayItems; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object? XmlDefaultValue
        {
            get { return _xmlDefaultValue; }
            set { _xmlDefaultValue = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool XmlIgnore
        {
            get { return _xmlIgnore; }
            set { _xmlIgnore = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeAttribute? XmlType
        {
            get { return _xmlType; }
            set { _xmlType = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlRootAttribute? XmlRoot
        {
            get { return _xmlRoot; }
            set { _xmlRoot = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttributes XmlAnyElements
        {
            get { return _xmlAnyElements; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyAttributeAttribute? XmlAnyAttribute
        {
            get { return _xmlAnyAttribute; }
            set { _xmlAnyAttribute = value; }
        }

        public XmlChoiceIdentifierAttribute? XmlChoiceIdentifier
        {
            get { return _xmlChoiceIdentifier; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Xmlns
        {
            get { return _xmlns; }
            set { _xmlns = value; }
        }
    }
}
