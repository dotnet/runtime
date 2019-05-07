// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Text;

namespace System.Xml.Schema
{
    public abstract class XmlSchemaDatatype
    {
        public abstract Type ValueType { get; }

        public abstract XmlTokenizedType TokenizedType { get; }

        public abstract object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr);

        public virtual XmlSchemaDatatypeVariety Variety { get { return XmlSchemaDatatypeVariety.Atomic; } }

        internal XmlSchemaDatatype() {}

        public virtual object ChangeType(object value, Type targetType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }
            return ValueConverter.ChangeType(value, targetType);
        }

        public virtual object ChangeType(object value, Type targetType, IXmlNamespaceResolver namespaceResolver)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }
            if (namespaceResolver == null)
            {
                throw new ArgumentNullException(nameof(namespaceResolver));
            }
            return ValueConverter.ChangeType(value, targetType, namespaceResolver);
        }

        public virtual XmlTypeCode TypeCode { get { return XmlTypeCode.None; } }

        public virtual bool IsDerivedFrom(XmlSchemaDatatype datatype)
        {
            return false;
        }

        internal abstract bool HasLexicalFacets { get; }

        internal abstract bool HasValueFacets { get; }

        internal abstract XmlValueConverter ValueConverter { get; }

        internal abstract RestrictionFacets Restriction { get; set; }

        internal abstract int Compare(object value1, object value2);

        internal abstract object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, bool createAtomicValue);

        internal abstract Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue);

        internal abstract Exception TryParseValue(object value, XmlNameTable nameTable, IXmlNamespaceResolver namespaceResolver, out object typedValue);

        internal abstract FacetsChecker FacetsChecker { get; }

        internal abstract XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get; }

        internal abstract XmlSchemaDatatype DeriveByRestriction(XmlSchemaObjectCollection facets, XmlNameTable nameTable, XmlSchemaType schemaType);

        internal abstract XmlSchemaDatatype DeriveByList(XmlSchemaType schemaType);

        internal abstract void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller);

        internal abstract bool IsEqual(object o1, object o2);

        internal abstract bool IsComparable(XmlSchemaDatatype dtype);

        //Error message helper
        internal string TypeCodeString
        {
            get
            {
                string typeCodeString = string.Empty;
                XmlTypeCode typeCode = this.TypeCode;
                switch (this.Variety)
                {
                    case XmlSchemaDatatypeVariety.List:
                        if (typeCode == XmlTypeCode.AnyAtomicType)
                        { //List of union
                            typeCodeString = "List of Union";
                        }
                        else
                        {
                            typeCodeString = "List of " + TypeCodeToString(typeCode);
                        }
                        break;

                    case XmlSchemaDatatypeVariety.Union:
                        typeCodeString = "Union";
                        break;

                    case XmlSchemaDatatypeVariety.Atomic:
                        if (typeCode == XmlTypeCode.AnyAtomicType)
                        {
                            typeCodeString = "anySimpleType";
                        }
                        else
                        {
                            typeCodeString = TypeCodeToString(typeCode);
                        }
                        break;
                }
                return typeCodeString;
            }
        }

        internal string TypeCodeToString(XmlTypeCode typeCode)
        {
            switch (typeCode)
            {
                case XmlTypeCode.None:
                    return "None";
                case XmlTypeCode.Item:
                    return "AnyType";
                case XmlTypeCode.AnyAtomicType:
                    return "AnyAtomicType";
                case XmlTypeCode.String:
                    return "String";
                case XmlTypeCode.Boolean:
                    return "Boolean";
                case XmlTypeCode.Decimal:
                    return "Decimal";
                case XmlTypeCode.Float:
                    return "Float";
                case XmlTypeCode.Double:
                    return "Double";
                case XmlTypeCode.Duration:
                    return "Duration";
                case XmlTypeCode.DateTime:
                    return "DateTime";
                case XmlTypeCode.Time:
                    return "Time";
                case XmlTypeCode.Date:
                    return "Date";
                case XmlTypeCode.GYearMonth:
                    return "GYearMonth";
                case XmlTypeCode.GYear:
                    return "GYear";
                case XmlTypeCode.GMonthDay:
                    return "GMonthDay";
                case XmlTypeCode.GDay:
                    return "GDay";
                case XmlTypeCode.GMonth:
                    return "GMonth";
                case XmlTypeCode.HexBinary:
                    return "HexBinary";
                case XmlTypeCode.Base64Binary:
                    return "Base64Binary";
                case XmlTypeCode.AnyUri:
                    return "AnyUri";
                case XmlTypeCode.QName:
                    return "QName";
                case XmlTypeCode.Notation:
                    return "Notation";
                case XmlTypeCode.NormalizedString:
                    return "NormalizedString";
                case XmlTypeCode.Token:
                    return "Token";
                case XmlTypeCode.Language:
                    return "Language";
                case XmlTypeCode.NmToken:
                    return "NmToken";
                case XmlTypeCode.Name:
                    return "Name";
                case XmlTypeCode.NCName:
                    return "NCName";
                case XmlTypeCode.Id:
                    return "Id";
                case XmlTypeCode.Idref:
                    return "Idref";
                case XmlTypeCode.Entity:
                    return "Entity";
                case XmlTypeCode.Integer:
                    return "Integer";
                case XmlTypeCode.NonPositiveInteger:
                    return "NonPositiveInteger";
                case XmlTypeCode.NegativeInteger:
                    return "NegativeInteger";
                case XmlTypeCode.Long:
                    return "Long";
                case XmlTypeCode.Int:
                    return "Int";
                case XmlTypeCode.Short:
                    return "Short";
                case XmlTypeCode.Byte:
                    return "Byte";
                case XmlTypeCode.NonNegativeInteger:
                    return "NonNegativeInteger";
                case XmlTypeCode.UnsignedLong:
                    return "UnsignedLong";
                case XmlTypeCode.UnsignedInt:
                    return "UnsignedInt";
                case XmlTypeCode.UnsignedShort:
                    return "UnsignedShort";
                case XmlTypeCode.UnsignedByte:
                    return "UnsignedByte";
                case XmlTypeCode.PositiveInteger:
                    return "PositiveInteger";

                default:
                    return typeCode.ToString();
            }
        }

        internal static string ConcatenatedToString(object value)
        {
            Type t = value.GetType();
            string stringValue = string.Empty;
            if (t == typeof(IEnumerable) && t != typeof(string))
            {
                StringBuilder bldr = new StringBuilder();
                IEnumerator enumerator = (value as IEnumerable).GetEnumerator();
                if (enumerator.MoveNext())
                {
                    bldr.Append("{");
                    object cur = enumerator.Current;
                    if (cur is IFormattable)
                    {
                        bldr.Append(((IFormattable)cur).ToString("", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        bldr.Append(cur.ToString());
                    }
                    while (enumerator.MoveNext())
                    {
                        bldr.Append(" , ");
                        cur = enumerator.Current;
                        if (cur is IFormattable)
                        {
                            bldr.Append(((IFormattable)cur).ToString("", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            bldr.Append(cur.ToString());
                        }
                    }
                    bldr.Append("}");
                    stringValue = bldr.ToString();
                }
            }
            else if (value is IFormattable)
            {
                stringValue = ((IFormattable)value).ToString("", CultureInfo.InvariantCulture);
            }
            else
            {
                stringValue = value.ToString();
            }
            return stringValue;
        }

        internal static XmlSchemaDatatype FromXmlTokenizedType(XmlTokenizedType token)
        {
            return DatatypeImplementation.FromXmlTokenizedType(token);
        }

        internal static XmlSchemaDatatype FromXmlTokenizedTypeXsd(XmlTokenizedType token)
        {
            return DatatypeImplementation.FromXmlTokenizedTypeXsd(token);
        }

        internal static XmlSchemaDatatype FromXdrName(string name)
        {
            return DatatypeImplementation.FromXdrName(name);
        }

        internal static XmlSchemaDatatype DeriveByUnion(XmlSchemaSimpleType[] types, XmlSchemaType schemaType)
        {
            return DatatypeImplementation.DeriveByUnion(types, schemaType);
        }

        internal static string XdrCanonizeUri(string uri, XmlNameTable nameTable, SchemaNames schemaNames)
        {
            string canonicalUri;
            int offset = 5;
            bool convert = false;

            if (uri.Length > 5 && uri.StartsWith("uuid:", StringComparison.Ordinal))
            {
                convert = true;
            }
            else if (uri.Length > 9 && uri.StartsWith("urn:uuid:", StringComparison.Ordinal))
            {
                convert = true;
                offset = 9;
            }

            if (convert)
            {
                canonicalUri = nameTable.Add(string.Concat(uri.AsSpan(0, offset), CultureInfo.InvariantCulture.TextInfo.ToUpper(uri.Substring(offset, uri.Length - offset))));
            }
            else
            {
                canonicalUri = uri;
            }

            if (
                Ref.Equal(schemaNames.NsDataTypeAlias, canonicalUri) ||
                Ref.Equal(schemaNames.NsDataTypeOld, canonicalUri)
            )
            {
                canonicalUri = schemaNames.NsDataType;
            }
            else if (Ref.Equal(schemaNames.NsXdrAlias, canonicalUri))
            {
                canonicalUri = schemaNames.NsXdr;
            }

            return canonicalUri;
        }
    }
}

