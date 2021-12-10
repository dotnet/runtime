// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            // object[] attrs = provider.GetCustomAttributes(false);
            IList<CustomAttributeData> attrs = ((MemberInfo)provider).GetCustomAttributesData();

            // most generic <any/> matches everything
            XmlAnyElementAttribute? wildcard = null;
            foreach (CustomAttributeData attribute in attrs)
            {
                Attribute? attr = CreateAttributeFromCustomAttributeData(attribute);
                if (attr != null)
                {
                    if (attr is XmlIgnoreAttribute || attr is ObsoleteAttribute)
                    {
                        _xmlIgnore = true;
                        break;
                    }
                    else if (attr is XmlElementAttribute)
                    {
                        _xmlElements.Add((XmlElementAttribute)attr);
                    }
                    else if (attr is XmlArrayItemAttribute)
                    {
                        _xmlArrayItems.Add((XmlArrayItemAttribute)attr);
                    }
                    else if (attr is XmlAnyElementAttribute)
                    {
                        XmlAnyElementAttribute any = (XmlAnyElementAttribute)attr;
                        if ((any.Name == null || any.Name.Length == 0) && any.GetNamespaceSpecified() && any.Namespace == null)
                        {
                            // ignore duplicate wildcards
                            wildcard = any;
                        }
                        else
                        {
                            _xmlAnyElements.Add((XmlAnyElementAttribute)attr);
                        }
                    }
                    else if (attr is DefaultValueAttribute)
                    {
                        _xmlDefaultValue = ((DefaultValueAttribute)attr).Value;
                    }
                    else if (attr is XmlAttributeAttribute)
                    {
                        _xmlAttribute = (XmlAttributeAttribute)attr;
                    }
                    else if (attr is XmlArrayAttribute)
                    {
                        _xmlArray = (XmlArrayAttribute)attr;
                    }
                    else if (attr is XmlTextAttribute)
                    {
                        _xmlText = (XmlTextAttribute)attr;
                    }
                    else if (attr is XmlEnumAttribute)
                    {
                        _xmlEnum = (XmlEnumAttribute)attr;
                    }
                    else if (attr is XmlRootAttribute)
                    {
                        _xmlRoot = (XmlRootAttribute)attr;
                    }
                    else if (attr is XmlTypeAttribute)
                    {
                        _xmlType = (XmlTypeAttribute)attr;
                    }
                    else if (attr is XmlAnyAttributeAttribute)
                    {
                        _xmlAnyAttribute = (XmlAnyAttributeAttribute)attr;
                    }
                    else if (attr is XmlChoiceIdentifierAttribute)
                    {
                        _xmlChoiceIdentifier = (XmlChoiceIdentifierAttribute)attr;
                    }
                    else if (attr is XmlNamespaceDeclarationsAttribute)
                    {
                        _xmlns = true;
                    }
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

        private static Attribute? CreateAttributeFromCustomAttributeData(CustomAttributeData cad)
        {
            var attrType = cad.AttributeType;
            var isSystemNS = attrType.Namespace?.StartsWith("System.");
            if (!isSystemNS.HasValue || !isSystemNS.Value)
            {
                return null;
            }

            var rtType = Type.GetType(attrType.AssemblyQualifiedName!);
            int count = cad.ConstructorArguments.Count;
            Type? [] constructorArgsTypes = new Type[count];
            object [] constructorValues = new object[count];
            for (int i = 0; i < count; i++)
            {
                constructorArgsTypes[i] = Type.GetType(cad.ConstructorArguments[i].ArgumentType.AssemblyQualifiedName!) ?? null;
                if (cad.ConstructorArguments[i].ArgumentType.IsEnum)
                {
                    constructorValues[i] = Enum.ToObject(constructorArgsTypes[i]!, cad.ConstructorArguments[i].Value!);
                }
                else
                {
                    constructorValues[i] = cad.ConstructorArguments[i].Value!;
                }
            }

            if (rtType == null  || constructorArgsTypes == null)
            {
                return null;
            }

            var rtConstructor = rtType.GetConstructor(constructorArgsTypes!);
            if (rtConstructor == null)
            {
                return null;
            }

            var attribute = (Attribute)rtConstructor.Invoke(constructorValues);

            if (cad.NamedArguments == null)
            {
                return attribute;
            }

            foreach (var namedArg in cad.NamedArguments)
            {
                var propInfo = namedArg.MemberInfo as PropertyInfo;
                if (propInfo != null)
                {
                    var rtPropDeclaringType = Type.GetType(propInfo.DeclaringType!.AssemblyQualifiedName!) ?? null;
                    if (rtPropDeclaringType != null )
                    {
                        var rtPropInfo = rtPropDeclaringType!.GetProperty(propInfo.Name);
                        var rtArgType = Type.GetType(namedArg.TypedValue.ArgumentType.AssemblyQualifiedName!);
                        object argValue;
                        if (rtArgType!.IsEnum)
                        {
                            argValue = Enum.ToObject(rtArgType, namedArg.TypedValue.Value!);
                        }
                        else
                        {
                            argValue = namedArg.TypedValue.Value!;
                        }
                        rtPropInfo!.SetValue(attribute, argValue, null);
                    }
                }
            }

            return attribute;
        }

        internal static object? GetAttr(MemberInfo memberInfo, Type attrType)
        {
            // object[] attrs = memberInfo.GetCustomAttributes(attrType, false);
            // if (attrs.Length == 0) return null;
            // return attrs[0];
            IList<CustomAttributeData> attrs = memberInfo.GetCustomAttributesData();
            if (!attrs.Any(attr => attr.AttributeType.FullName == attrType.FullName))
            {
                return null;
            }
            CustomAttributeData data = attrs.FirstOrDefault(ca => ca.AttributeType.FullName == attrType.FullName)!;
            return CreateAttributeFromCustomAttributeData(data);
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
