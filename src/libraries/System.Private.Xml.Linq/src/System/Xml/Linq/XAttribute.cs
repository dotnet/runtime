// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using CultureInfo = System.Globalization.CultureInfo;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML attribute.
    /// </summary>
    /// <remarks>
    /// An XML attribute is a name/value pair associated with an XML element.
    /// </remarks>
    [System.ComponentModel.TypeDescriptionProvider("MS.Internal.Xml.Linq.ComponentModel.XTypeDescriptionProvider`1[[System.Xml.Linq.XAttribute, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]],System.ComponentModel.TypeConverter")]
    public class XAttribute : XObject
    {
        /// <summary>
        /// Gets an empty collection of attributes.
        /// </summary>
        public static IEnumerable<XAttribute> EmptySequence
        {
            get
            {
                return Array.Empty<XAttribute>();
            }
        }

        internal XAttribute? next;
        internal XName name;
        internal string value;

        /// <overloads>
        /// Initializes a new instance of the <see cref="XAttribute"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="XAttribute"/> class from
        /// the specified name and value.
        /// </summary>
        /// <param name="name">
        /// The name of the attribute.
        /// </param>
        /// <param name="value">
        /// The value of the attribute.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the passed in name or value are null.
        /// </exception>
        public XAttribute(XName name, object value)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            string s = XContainer.GetStringValue(value);
            ValidateAttribute(name, s);
            this.name = name;
            this.value = s;
        }

        /// <summary>
        /// Initializes an instance of the XAttribute class
        /// from another XAttribute object.
        /// </summary>
        /// <param name="other"><see cref="XAttribute"/> object to copy from.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified <see cref="XAttribute"/> is null.
        /// </exception>
        public XAttribute(XAttribute other)
        {
            ArgumentNullException.ThrowIfNull(other);

            name = other.name;
            value = other.value;
        }

        /// <summary>
        /// Gets a value indicating if this attribute is a namespace declaration.
        /// </summary>
        public bool IsNamespaceDeclaration
        {
            get
            {
                string namespaceName = name.NamespaceName;
                if (namespaceName.Length == 0)
                {
                    return name.LocalName == "xmlns";
                }
                return (object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace;
            }
        }

        /// <summary>
        /// Gets the name of this attribute.
        /// </summary>
        public XName Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets the next attribute of the parent element.
        /// </summary>
        /// <remarks>
        /// If this attribute does not have a parent, or if there is no next attribute,
        /// then this property returns null.
        /// </remarks>
        public XAttribute? NextAttribute
        {
            get { return parent != null && ((XElement)parent).lastAttr != this ? next : null; }
        }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.Attribute.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.Attribute;
            }
        }

        /// <summary>
        /// Gets the previous attribute of the parent element.
        /// </summary>
        /// <remarks>
        /// If this attribute does not have a parent, or if there is no previous attribute,
        /// then this property returns null.
        /// </remarks>
        public XAttribute? PreviousAttribute
        {
            get
            {
                if (parent == null) return null;
                XAttribute a = ((XElement)parent).lastAttr!;
                while (a.next != this)
                {
                    a = a.next!;
                }
                return a != ((XElement)parent).lastAttr ? a : null;
            }
        }

        /// <summary>
        /// Gets or sets the value of this attribute.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the value set is null.
        /// </exception>
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                ValidateAttribute(name, value);
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                this.value = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Value);
            }
        }

        /// <summary>
        /// Deletes this XAttribute.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parent element is null.
        /// </exception>
        public void Remove()
        {
            if (parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
            ((XElement)parent).RemoveAttribute(this);
        }

        /// <summary>
        /// Sets the value of this <see cref="XAttribute"/>.
        /// <seealso cref="XElement.SetValue"/>
        /// <seealso cref="XElement.SetAttributeValue"/>
        /// <seealso cref="XElement.SetElementValue"/>
        /// </summary>
        /// <param name="value">
        /// The value to assign to this attribute. The value is converted to its string
        /// representation and assigned to the <see cref="Value"/> property.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified value is null.
        /// </exception>
        public void SetValue(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            Value = XContainer.GetStringValue(value);
        }

        /// <summary>
        /// Override for <see cref="ToString()"/> on <see cref="XAttribute"/>
        /// </summary>
        /// <returns>XML text representation of an attribute and its value</returns>
        public override string ToString()
        {
            using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.ConformanceLevel = ConformanceLevel.Fragment;
                using (XmlWriter w = XmlWriter.Create(sw, ws))
                {
                    w.WriteAttributeString(GetPrefixOfNamespace(name.Namespace), name.LocalName, name.NamespaceName, value);
                }
                return sw.ToString().Trim();
            }
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="string"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="string"/>.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator string?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return attribute.value;
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="bool"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="bool"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator bool(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToBoolean(attribute.value.ToLowerInvariant());
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="bool"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="bool"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="bool"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator bool?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToBoolean(attribute.value.ToLowerInvariant());
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="int"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="int"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="int"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator int(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToInt32(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="int"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="int"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="int"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator int?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToInt32(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="uint"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="uint"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="uint"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator uint(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToUInt32(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="uint"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="uint"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="uint"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator uint?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToUInt32(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="long"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="long"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="long"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator long(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToInt64(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="long"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="long"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="long"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator long?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToInt64(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="ulong"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="ulong"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="ulong"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator ulong(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToUInt64(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to an <see cref="ulong"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="ulong"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as an <see cref="ulong"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator ulong?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToUInt64(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="float"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="float"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="float"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator float(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToSingle(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="float"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="float"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="float"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator float?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToSingle(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="double"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="double"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="double"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator double(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToDouble(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="double"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="double"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="double"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator double?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToDouble(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="decimal"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="decimal"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="decimal"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator decimal(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToDecimal(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="decimal"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="decimal"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="decimal"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator decimal?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToDecimal(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="DateTime"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="DateTime"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator DateTime(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return DateTime.Parse(attribute.value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="DateTime"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="DateTime"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="DateTime"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator DateTime?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return DateTime.Parse(attribute.value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="DateTimeOffset"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="DateTimeOffset"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator DateTimeOffset(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToDateTimeOffset(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="DateTimeOffset"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="DateTimeOffset"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="DateTimeOffset"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator DateTimeOffset?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToDateTimeOffset(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="TimeSpan"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="TimeSpan"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator TimeSpan(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToTimeSpan(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="TimeSpan"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="TimeSpan"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="TimeSpan"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator TimeSpan?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToTimeSpan(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="Guid"/>.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="Guid"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="Guid"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified attribute is null.
        /// </exception>
        [CLSCompliant(false)]
        public static explicit operator Guid(XAttribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            return XmlConvert.ToGuid(attribute.value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XAttribute"/> to a <see cref="Guid"/>?.
        /// </summary>
        /// <param name="attribute">
        /// The <see cref="XAttribute"/> to cast to <see cref="Guid"/>?. Can be null.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XAttribute"/> as a <see cref="Guid"/>?.
        /// </returns>
        [CLSCompliant(false)]
        [return: NotNullIfNotNull("attribute")]
        public static explicit operator Guid?(XAttribute? attribute)
        {
            if (attribute == null) return null;
            return XmlConvert.ToGuid(attribute.value);
        }

        internal int GetDeepHashCode()
        {
            return name.GetHashCode() ^ value.GetHashCode();
        }

        internal string? GetPrefixOfNamespace(XNamespace ns)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            if (parent != null) return ((XElement)parent).GetPrefixOfNamespace(ns);
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }

        private static void ValidateAttribute(XName name, string value)
        {
            // The following constraints apply for namespace declarations:
            string namespaceName = name.NamespaceName;
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace)
            {
                if (value.Length == 0)
                {
                    // The empty namespace name can only be declared by
                    // the default namespace declaration
                    throw new ArgumentException(SR.Format(SR.Argument_NamespaceDeclarationPrefixed, name.LocalName));
                }
                else if (value == XNamespace.xmlPrefixNamespace)
                {
                    // 'http://www.w3.org/XML/1998/namespace' can only
                    // be declared by the 'xml' prefix namespace declaration.
                    if (name.LocalName != "xml") throw new ArgumentException(SR.Argument_NamespaceDeclarationXml);
                }
                else if (value == XNamespace.xmlnsPrefixNamespace)
                {
                    // 'http://www.w3.org/2000/xmlns/' must not be declared
                    // by any namespace declaration.
                    throw new ArgumentException(SR.Argument_NamespaceDeclarationXmlns);
                }
                else
                {
                    string localName = name.LocalName;
                    if (localName == "xml")
                    {
                        // No other namespace name can be declared by the 'xml'
                        // prefix namespace declaration.
                        throw new ArgumentException(SR.Argument_NamespaceDeclarationXml);
                    }
                    else if (localName == "xmlns")
                    {
                        // The 'xmlns' prefix must not be declared.
                        throw new ArgumentException(SR.Argument_NamespaceDeclarationXmlns);
                    }
                }
            }
            else if (namespaceName.Length == 0 && name.LocalName == "xmlns")
            {
                if (value == XNamespace.xmlPrefixNamespace)
                {
                    // 'http://www.w3.org/XML/1998/namespace' can only
                    // be declared by the 'xml' prefix namespace declaration.
                    throw new ArgumentException(SR.Argument_NamespaceDeclarationXml);
                }
                else if (value == XNamespace.xmlnsPrefixNamespace)
                {
                    // 'http://www.w3.org/2000/xmlns/' must not be declared
                    // by any namespace declaration.
                    throw new ArgumentException(SR.Argument_NamespaceDeclarationXmlns);
                }
            }
        }
    }
}
