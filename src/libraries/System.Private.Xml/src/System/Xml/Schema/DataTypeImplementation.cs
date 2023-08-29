// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Reflection;

namespace System.Xml.Schema
{
    public enum XmlSchemaDatatypeVariety
    {
        Atomic,
        List,
        Union
    }

    internal sealed class XsdSimpleValue
    { //Wrapper to store XmlType and TypedValue together
        private readonly XmlSchemaSimpleType _xmlType;
        private readonly object _typedValue;

        public XsdSimpleValue(XmlSchemaSimpleType st, object value)
        {
            _xmlType = st;
            _typedValue = value;
        }

        public XmlSchemaSimpleType XmlType
        {
            get
            {
                return _xmlType;
            }
        }

        public object TypedValue
        {
            get
            {
                return _typedValue;
            }
        }
    }


    [Flags]
    internal enum RestrictionFlags
    {
        Length = 0x0001,
        MinLength = 0x0002,
        MaxLength = 0x0004,
        Pattern = 0x0008,
        Enumeration = 0x0010,
        WhiteSpace = 0x0020,
        MaxInclusive = 0x0040,
        MaxExclusive = 0x0080,
        MinInclusive = 0x0100,
        MinExclusive = 0x0200,
        TotalDigits = 0x0400,
        FractionDigits = 0x0800,
    }

    internal enum XmlSchemaWhiteSpace
    {
        Preserve,
        Replace,
        Collapse,
    }

    internal sealed class RestrictionFacets
    {
        internal int Length;
        internal int MinLength;
        internal int MaxLength;
        internal ArrayList? Patterns;
        internal ArrayList? Enumeration;
        internal XmlSchemaWhiteSpace WhiteSpace;
        internal object? MaxInclusive;
        internal object? MaxExclusive;
        internal object? MinInclusive;
        internal object? MinExclusive;
        internal int TotalDigits;
        internal int FractionDigits;
        internal RestrictionFlags Flags;
        internal RestrictionFlags FixedFlags;
    }

    internal abstract class DatatypeImplementation : XmlSchemaDatatype
    {
        private XmlSchemaDatatypeVariety _variety = XmlSchemaDatatypeVariety.Atomic;
        private RestrictionFacets? _restriction;
        private DatatypeImplementation? _baseType;
        private XmlValueConverter? _valueConverter;
        private XmlSchemaType? _parentSchemaType;

        private static readonly Hashtable s_builtinTypes = new Hashtable();
        private static readonly XmlSchemaSimpleType[] s_enumToTypeCode = new XmlSchemaSimpleType[(int)XmlTypeCode.DayTimeDuration + 1];
        private static XmlSchemaSimpleType s__anySimpleType = null!;
        private static XmlSchemaSimpleType s__anyAtomicType = null!;
        private static XmlSchemaSimpleType s__untypedAtomicType = null!;
        private static XmlSchemaSimpleType s_yearMonthDurationType = null!;
        private static XmlSchemaSimpleType s_dayTimeDurationType = null!;
        private static volatile XmlSchemaSimpleType? s_normalizedStringTypeV1Compat;
        private static volatile XmlSchemaSimpleType? s_tokenTypeV1Compat;

        private const int anySimpleTypeIndex = 11;

        internal static XmlQualifiedName QnAnySimpleType = new XmlQualifiedName("anySimpleType", XmlReservedNs.NsXs);
        internal static XmlQualifiedName QnAnyType = new XmlQualifiedName("anyType", XmlReservedNs.NsXs);

        //Create facet checkers
        internal static FacetsChecker stringFacetsChecker = new StringFacetsChecker();
        internal static FacetsChecker miscFacetsChecker = new MiscFacetsChecker();
        internal static FacetsChecker numeric2FacetsChecker = new Numeric2FacetsChecker();
        internal static FacetsChecker binaryFacetsChecker = new BinaryFacetsChecker();
        internal static FacetsChecker dateTimeFacetsChecker = new DateTimeFacetsChecker();
        internal static FacetsChecker durationFacetsChecker = new DurationFacetsChecker();
        internal static FacetsChecker listFacetsChecker = new ListFacetsChecker();
        internal static FacetsChecker qnameFacetsChecker = new QNameFacetsChecker();
        internal static FacetsChecker unionFacetsChecker = new UnionFacetsChecker();

        static DatatypeImplementation()
        {
            CreateBuiltinTypes();
        }

        internal static XmlSchemaSimpleType AnySimpleType { get { return s__anySimpleType; } }

        // Additional built-in XQuery simple types
        internal static XmlSchemaSimpleType AnyAtomicType { get { return s__anyAtomicType; } }
        internal static XmlSchemaSimpleType UntypedAtomicType { get { return s__untypedAtomicType; } }

        internal static new DatatypeImplementation? FromXmlTokenizedType(XmlTokenizedType token)
        {
            return s_tokenizedTypes[(int)token];
        }

        internal static new DatatypeImplementation? FromXmlTokenizedTypeXsd(XmlTokenizedType token)
        {
            return s_tokenizedTypesXsd[(int)token];
        }

        internal static new DatatypeImplementation? FromXdrName(string name)
        {
            int i = Array.BinarySearch(s_xdrTypes, name, null);
            return i < 0 ? null : (DatatypeImplementation)s_xdrTypes[i];
        }

        private static DatatypeImplementation? FromTypeName(string name)
        {
            int i = Array.BinarySearch(s_xsdTypes, name, null);
            return i < 0 ? null : (DatatypeImplementation)s_xsdTypes[i];
        }

        /// <summary>
        /// Begin the creation of an XmlSchemaSimpleType object that will be used to represent a static built-in type.
        /// Once StartBuiltinType has been called for all built-in types, FinishBuiltinType should be called in order
        /// to create links between the types.
        /// </summary>
        internal static XmlSchemaSimpleType StartBuiltinType(XmlQualifiedName qname, XmlSchemaDatatype dataType)
        {
            XmlSchemaSimpleType simpleType;
            Debug.Assert(qname != null && dataType != null);

            simpleType = new XmlSchemaSimpleType();
            simpleType.SetQualifiedName(qname);
            simpleType.SetDatatype(dataType);
            simpleType.ElementDecl = new SchemaElementDecl(dataType);
            simpleType.ElementDecl.SchemaType = simpleType;

            return simpleType;
        }

        /// <summary>
        /// Finish constructing built-in types by setting up derivation and list links.
        /// </summary>
        internal static void FinishBuiltinType(XmlSchemaSimpleType derivedType, XmlSchemaSimpleType baseType)
        {
            Debug.Assert(derivedType != null && baseType != null);

            // Create link from the derived type to the base type
            derivedType.SetBaseSchemaType(baseType);
            derivedType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
            if (derivedType.Datatype!.Variety == XmlSchemaDatatypeVariety.Atomic)
            { //Content is restriction
                XmlSchemaSimpleTypeRestriction restContent = new XmlSchemaSimpleTypeRestriction();
                restContent.BaseTypeName = baseType.QualifiedName;
                derivedType.Content = restContent;
            }

            // Create link from a list type to its member type
            if (derivedType.Datatype.Variety == XmlSchemaDatatypeVariety.List)
            {
                XmlSchemaSimpleTypeList listContent = new XmlSchemaSimpleTypeList();
                derivedType.SetDerivedBy(XmlSchemaDerivationMethod.List);
                switch (derivedType.Datatype.TypeCode)
                {
                    case XmlTypeCode.NmToken:
                        listContent.ItemType = listContent.BaseItemType = s_enumToTypeCode[(int)XmlTypeCode.NmToken];
                        break;

                    case XmlTypeCode.Entity:
                        listContent.ItemType = listContent.BaseItemType = s_enumToTypeCode[(int)XmlTypeCode.Entity];
                        break;

                    case XmlTypeCode.Idref:
                        listContent.ItemType = listContent.BaseItemType = s_enumToTypeCode[(int)XmlTypeCode.Idref];
                        break;
                }
                derivedType.Content = listContent;
            }
        }

        internal static void CreateBuiltinTypes()
        {
            XmlQualifiedName qname;

            //Build anySimpleType
            SchemaDatatypeMap sdm = s_xsdTypes[anySimpleTypeIndex]; //anySimpleType
            qname = new XmlQualifiedName(sdm.Name, XmlReservedNs.NsXs);
            DatatypeImplementation dt = FromTypeName(qname.Name)!;
            s__anySimpleType = StartBuiltinType(qname, dt);
            dt._parentSchemaType = s__anySimpleType;
            s_builtinTypes.Add(qname, s__anySimpleType);

            // Start construction of each built-in Xsd type
            XmlSchemaSimpleType simpleType;
            for (int i = 0; i < s_xsdTypes.Length; i++)
            { //Create all types
                if (i == anySimpleTypeIndex)
                { //anySimpleType
                    continue;
                }
                sdm = s_xsdTypes[i];

                qname = new XmlQualifiedName(sdm.Name, XmlReservedNs.NsXs);
                dt = FromTypeName(qname.Name)!;
                simpleType = StartBuiltinType(qname, dt);
                dt._parentSchemaType = simpleType;

                s_builtinTypes.Add(qname, simpleType);
                if (dt._variety == XmlSchemaDatatypeVariety.Atomic)
                {
                    s_enumToTypeCode[(int)dt.TypeCode] = simpleType;
                }
            }

            // Finish construction of each built-in Xsd type
            for (int i = 0; i < s_xsdTypes.Length; i++)
            {
                if (i == anySimpleTypeIndex)
                { //anySimpleType
                    continue;
                }

                sdm = s_xsdTypes[i];
                XmlSchemaSimpleType derivedType = (XmlSchemaSimpleType)s_builtinTypes[new XmlQualifiedName(sdm.Name, XmlReservedNs.NsXs)]!;
                XmlSchemaSimpleType baseType;

                if (sdm.ParentIndex == anySimpleTypeIndex)
                {
                    FinishBuiltinType(derivedType, s__anySimpleType);
                }
                else
                { //derived types whose index > 0
                    baseType = (XmlSchemaSimpleType)s_builtinTypes[new XmlQualifiedName(((SchemaDatatypeMap)(s_xsdTypes[sdm.ParentIndex])).Name, XmlReservedNs.NsXs)]!;
                    FinishBuiltinType(derivedType, baseType);
                }
            }

            // Construct xdt:anyAtomicType type (derived from xs:anySimpleType)
            qname = new XmlQualifiedName("anyAtomicType", XmlReservedNs.NsXQueryDataType);
            s__anyAtomicType = StartBuiltinType(qname, s_anyAtomicType);
            s_anyAtomicType._parentSchemaType = s__anyAtomicType;
            FinishBuiltinType(s__anyAtomicType, s__anySimpleType);
            s_builtinTypes.Add(qname, s__anyAtomicType);
            s_enumToTypeCode[(int)XmlTypeCode.AnyAtomicType] = s__anyAtomicType;

            // Construct xdt:untypedAtomic type (derived from xdt:anyAtomicType)
            qname = new XmlQualifiedName("untypedAtomic", XmlReservedNs.NsXQueryDataType);
            s__untypedAtomicType = StartBuiltinType(qname, s_untypedAtomicType);
            s_untypedAtomicType._parentSchemaType = s__untypedAtomicType;
            FinishBuiltinType(s__untypedAtomicType, s__anyAtomicType);
            s_builtinTypes.Add(qname, s__untypedAtomicType);
            s_enumToTypeCode[(int)XmlTypeCode.UntypedAtomic] = s__untypedAtomicType;

            // Construct xdt:yearMonthDuration type (derived from xs:duration)
            qname = new XmlQualifiedName("yearMonthDuration", XmlReservedNs.NsXQueryDataType);
            s_yearMonthDurationType = StartBuiltinType(qname, s_yearMonthDuration);
            s_yearMonthDuration._parentSchemaType = s_yearMonthDurationType;
            FinishBuiltinType(s_yearMonthDurationType, s_enumToTypeCode[(int)XmlTypeCode.Duration]);
            s_builtinTypes.Add(qname, s_yearMonthDurationType);
            s_enumToTypeCode[(int)XmlTypeCode.YearMonthDuration] = s_yearMonthDurationType;

            // Construct xdt:dayTimeDuration type (derived from xs:duration)
            qname = new XmlQualifiedName("dayTimeDuration", XmlReservedNs.NsXQueryDataType);
            s_dayTimeDurationType = StartBuiltinType(qname, s_dayTimeDuration);
            s_dayTimeDuration._parentSchemaType = s_dayTimeDurationType;
            FinishBuiltinType(s_dayTimeDurationType, s_enumToTypeCode[(int)XmlTypeCode.Duration]);
            s_builtinTypes.Add(qname, s_dayTimeDurationType);
            s_enumToTypeCode[(int)XmlTypeCode.DayTimeDuration] = s_dayTimeDurationType;
        }

        internal static XmlSchemaSimpleType GetSimpleTypeFromTypeCode(XmlTypeCode typeCode)
        {
            return s_enumToTypeCode[(int)typeCode];
        }

        internal static XmlSchemaSimpleType? GetSimpleTypeFromXsdType(XmlQualifiedName qname)
        {
            return (XmlSchemaSimpleType?)s_builtinTypes[qname];
        }

        internal static XmlSchemaSimpleType GetNormalizedStringTypeV1Compat()
        {
            if (s_normalizedStringTypeV1Compat == null)
            {
                XmlSchemaSimpleType correctType = GetSimpleTypeFromTypeCode(XmlTypeCode.NormalizedString);
                XmlSchemaSimpleType tempNormalizedStringTypeV1Compat = (correctType.Clone() as XmlSchemaSimpleType)!;
                tempNormalizedStringTypeV1Compat.SetDatatype(c_normalizedStringV1Compat);
                tempNormalizedStringTypeV1Compat.ElementDecl = new SchemaElementDecl(c_normalizedStringV1Compat);
                tempNormalizedStringTypeV1Compat.ElementDecl.SchemaType = tempNormalizedStringTypeV1Compat;
                s_normalizedStringTypeV1Compat = tempNormalizedStringTypeV1Compat;
            }

            return s_normalizedStringTypeV1Compat;
        }

        internal static XmlSchemaSimpleType GetTokenTypeV1Compat()
        {
            if (s_tokenTypeV1Compat == null)
            {
                XmlSchemaSimpleType correctType = GetSimpleTypeFromTypeCode(XmlTypeCode.Token);
                XmlSchemaSimpleType tempTokenTypeV1Compat = (correctType.Clone() as XmlSchemaSimpleType)!;
                tempTokenTypeV1Compat.SetDatatype(c_tokenV1Compat);
                tempTokenTypeV1Compat.ElementDecl = new SchemaElementDecl(c_tokenV1Compat);
                tempTokenTypeV1Compat.ElementDecl.SchemaType = tempTokenTypeV1Compat;
                s_tokenTypeV1Compat = tempTokenTypeV1Compat;
            }
            return s_tokenTypeV1Compat;
        }

        internal static XmlSchemaSimpleType[] GetBuiltInTypes()
        {
            return s_enumToTypeCode;
        }

        internal static XmlTypeCode GetPrimitiveTypeCode(XmlTypeCode typeCode)
        {
            XmlSchemaSimpleType currentType = s_enumToTypeCode[(int)typeCode];
            while (currentType.BaseXmlSchemaType != DatatypeImplementation.AnySimpleType)
            {
                currentType = (currentType.BaseXmlSchemaType as XmlSchemaSimpleType)!;
                Debug.Assert(currentType != null);
            }
            return currentType.TypeCode;
        }

        internal override XmlSchemaDatatype DeriveByRestriction(XmlSchemaObjectCollection facets, XmlNameTable nameTable, XmlSchemaType schemaType)
        {
            DatatypeImplementation dt = (DatatypeImplementation)MemberwiseClone();
            dt._restriction = this.FacetsChecker.ConstructRestriction(this, facets, nameTable);
            dt._baseType = this;
            dt._parentSchemaType = schemaType;
            dt._valueConverter = null; //re-set old datatype's valueconverter
            return dt;
        }

        internal override XmlSchemaDatatype DeriveByList(XmlSchemaType? schemaType)
        {
            return DeriveByList(0, schemaType);
        }

        internal XmlSchemaDatatype DeriveByList(int minSize, XmlSchemaType? schemaType)
        {
            if (_variety == XmlSchemaDatatypeVariety.List)
            {
                throw new XmlSchemaException(SR.Sch_ListFromNonatomic, string.Empty);
            }
            else if (_variety == XmlSchemaDatatypeVariety.Union && !((Datatype_union)this).HasAtomicMembers())
            {
                throw new XmlSchemaException(SR.Sch_ListFromNonatomic, string.Empty);
            }

            DatatypeImplementation dt = new Datatype_List(this, minSize);
            dt._variety = XmlSchemaDatatypeVariety.List;
            dt._restriction = null;
            dt._baseType = s_anySimpleType; //Base type of a union is anySimpleType
            dt._parentSchemaType = schemaType;
            return dt;
        }

        internal static new DatatypeImplementation DeriveByUnion(XmlSchemaSimpleType[] types, XmlSchemaType schemaType)
        {
            DatatypeImplementation dt = new Datatype_union(types);
            dt._baseType = s_anySimpleType; //Base type of a union is anySimpleType
            dt._variety = XmlSchemaDatatypeVariety.Union;
            dt._parentSchemaType = schemaType;
            return dt;
        }

        internal override void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller) {/*noop*/}

        public override bool IsDerivedFrom(XmlSchemaDatatype datatype)
        {
            if (datatype == null)
            {
                return false;
            }

            //Common case - Derived by restriction
            for (DatatypeImplementation? dt = this; dt != null; dt = dt._baseType)
            {
                if (dt == datatype)
                {
                    return true;
                }
            }

            if (((DatatypeImplementation)datatype)._baseType == null)
            { //Both are built-in types
                Type derivedType = this.GetType();
                Type baseType = datatype.GetType();
                return baseType == derivedType || derivedType.IsSubclassOf(baseType);
            }
            else if (datatype.Variety == XmlSchemaDatatypeVariety.Union && !datatype.HasLexicalFacets && !datatype.HasValueFacets && _variety != XmlSchemaDatatypeVariety.Union)
            { //base type is union (not a restriction of union) and derived type is not union
                return ((Datatype_union)datatype).IsUnionBaseOf(this);
            }
            else if ((_variety == XmlSchemaDatatypeVariety.Union || _variety == XmlSchemaDatatypeVariety.List) && _restriction == null)
            { //derived type is union (not a restriction)
                return (datatype == s__anySimpleType.Datatype);
            }
            return false;
        }

        internal override bool IsEqual(object o1, object o2)
        {
            return Compare(o1, o2) == 0;
        }

        internal override bool IsComparable(XmlSchemaDatatype dtype)
        {
            XmlTypeCode thisCode = this.TypeCode;
            XmlTypeCode otherCode = dtype.TypeCode;

            if (thisCode == otherCode)
            { //They are both same built-in type or one is list and the other is list's itemType
                return true;
            }
            if (GetPrimitiveTypeCode(thisCode) == GetPrimitiveTypeCode(otherCode))
            {
                return true;
            }
            if (this.IsDerivedFrom(dtype) || dtype.IsDerivedFrom(this))
            { //One is union and the other is a member of the union
                return true;
            }
            return false;
        }

        internal abstract XmlValueConverter CreateValueConverter(XmlSchemaType schemaType);

        internal override FacetsChecker FacetsChecker { get { return miscFacetsChecker; } }

        internal override XmlValueConverter ValueConverter =>
            _valueConverter ??= CreateValueConverter(_parentSchemaType!);

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.None; } }

        public override Type ValueType { get { return typeof(string); } }

        public override XmlSchemaDatatypeVariety Variety { get { return _variety; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.None; } }

        internal override RestrictionFacets? Restriction
        {
            get
            {
                return _restriction;
            }
            set
            {
                _restriction = value;
            }
        }

        internal override bool HasLexicalFacets
        {
            get
            {
                RestrictionFlags flags = _restriction != null ? _restriction.Flags : 0;
                if (flags != 0 && (flags & (RestrictionFlags.Pattern | RestrictionFlags.WhiteSpace | RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits)) != 0)
                {
                    return true;
                }
                return false;
            }
        }
        internal override bool HasValueFacets
        {
            get
            {
                RestrictionFlags flags = _restriction != null ? _restriction.Flags : 0;
                if (flags != 0 && (flags & (RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.MaxExclusive | RestrictionFlags.MaxInclusive | RestrictionFlags.MinExclusive | RestrictionFlags.MinInclusive | RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits | RestrictionFlags.Enumeration)) != 0)
                {
                    return true;
                }
                return false;
            }
        }

        protected DatatypeImplementation? Base { get { return _baseType; } }

        internal abstract RestrictionFlags ValidRestrictionFlags { get; }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Preserve; } }

        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            object? typedValue;
            Exception? exception = TryParseValue(s, nameTable, nsmgr, out typedValue);
            if (exception != null)
            {
                throw new XmlSchemaException(SR.Sch_InvalidValueDetailed, new string[] { s, GetTypeName(), exception.Message }, exception, null, 0, 0, null);
            }

            Debug.Assert(typedValue != null);

            if (this.Variety == XmlSchemaDatatypeVariety.Union)
            {
                XsdSimpleValue simpleValue = (typedValue as XsdSimpleValue)!;
                return simpleValue.TypedValue;
            }
            return typedValue;
        }

        internal override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, bool createAtomicValue)
        {
            if (createAtomicValue)
            {
                object? typedValue;
                Exception? exception = TryParseValue(s, nameTable, nsmgr, out typedValue);
                if (exception != null)
                {
                    throw new XmlSchemaException(SR.Sch_InvalidValueDetailed, new string[] { s, GetTypeName(), exception.Message }, exception, null, 0, 0, null);
                }

                Debug.Assert(typedValue != null);
                return typedValue;
            }
            else
            {
                return ParseValue(s, nameTable, nsmgr);
            }
        }

        internal override Exception? TryParseValue(object value, XmlNameTable? nameTable, IXmlNamespaceResolver? namespaceResolver, out object? typedValue)
        {
            ArgumentNullException.ThrowIfNull(value);

            Exception? exception;
            typedValue = null;

            string? s = value as string;
            if (s != null)
            {
                return TryParseValue(s, nameTable, namespaceResolver, out typedValue);
            }
            try
            {
                object valueToCheck = value;
                if (value.GetType() != this.ValueType)
                {
                    valueToCheck = this.ValueConverter.ChangeType(value, this.ValueType, namespaceResolver);
                }
                if (this.HasLexicalFacets)
                {
                    string s1 = (string)this.ValueConverter.ChangeType(value, typeof(string), namespaceResolver); //Using value here to avoid info loss
                    exception = this.FacetsChecker.CheckLexicalFacets(ref s1, this);
                    if (exception != null) goto Error;
                }
                if (this.HasValueFacets)
                {
                    exception = this.FacetsChecker.CheckValueFacets(valueToCheck, this);
                    if (exception != null) goto Error;
                }
                typedValue = valueToCheck;
                return null;
            }
            catch (FormatException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (InvalidCastException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (OverflowException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (ArgumentException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }

        Error:
            return exception;
        }

        internal string GetTypeName()
        {
            XmlSchemaType? simpleType = _parentSchemaType;
            string typeName;
            if (simpleType == null || simpleType.QualifiedName.IsEmpty)
            { //If no QName, get typecode, no line info since it is not pertinent without file name
                typeName = TypeCodeString;
            }
            else
            {
                typeName = simpleType.QualifiedName.ToString();
            }
            return typeName;
        }

        // XSD types
        private static readonly DatatypeImplementation s_anySimpleType = new Datatype_anySimpleType();
        private static readonly DatatypeImplementation s_anyURI = new Datatype_anyURI();
        private static readonly DatatypeImplementation s_base64Binary = new Datatype_base64Binary();
        private static readonly DatatypeImplementation s_boolean = new Datatype_boolean();
        private static readonly DatatypeImplementation s_byte = new Datatype_byte();
        private static readonly DatatypeImplementation s_char = new Datatype_char(); // XDR
        private static readonly DatatypeImplementation s_date = new Datatype_date();
        private static readonly DatatypeImplementation s_dateTime = new Datatype_dateTime();
        private static readonly DatatypeImplementation s_dateTimeNoTz = new Datatype_dateTimeNoTimeZone(); // XDR
        private static readonly DatatypeImplementation s_dateTimeTz = new Datatype_dateTimeTimeZone(); // XDR
        private static readonly DatatypeImplementation s_day = new Datatype_day();
        private static readonly DatatypeImplementation s_decimal = new Datatype_decimal();
        private static readonly DatatypeImplementation s_double = new Datatype_double();
        private static readonly DatatypeImplementation s_doubleXdr = new Datatype_doubleXdr();     // XDR
        private static readonly DatatypeImplementation s_duration = new Datatype_duration();
        private static readonly DatatypeImplementation s_ENTITY = new Datatype_ENTITY();
        private static readonly DatatypeImplementation s_ENTITIES = (DatatypeImplementation)s_ENTITY.DeriveByList(1, null);
        private static readonly DatatypeImplementation s_ENUMERATION = new Datatype_ENUMERATION(); // XDR
        private static readonly DatatypeImplementation s_fixed = new Datatype_fixed();
        private static readonly DatatypeImplementation s_float = new Datatype_float();
        private static readonly DatatypeImplementation s_floatXdr = new Datatype_floatXdr(); // XDR
        private static readonly DatatypeImplementation s_hexBinary = new Datatype_hexBinary();
        private static readonly DatatypeImplementation s_ID = new Datatype_ID();
        private static readonly DatatypeImplementation s_IDREF = new Datatype_IDREF();
        private static readonly DatatypeImplementation s_IDREFS = (DatatypeImplementation)s_IDREF.DeriveByList(1, null);
        private static readonly DatatypeImplementation s_int = new Datatype_int();
        private static readonly DatatypeImplementation s_integer = new Datatype_integer();
        private static readonly DatatypeImplementation s_language = new Datatype_language();
        private static readonly DatatypeImplementation s_long = new Datatype_long();
        private static readonly DatatypeImplementation s_month = new Datatype_month();
        private static readonly DatatypeImplementation s_monthDay = new Datatype_monthDay();
        private static readonly DatatypeImplementation s_name = new Datatype_Name();
        private static readonly DatatypeImplementation s_NCName = new Datatype_NCName();
        private static readonly DatatypeImplementation s_negativeInteger = new Datatype_negativeInteger();
        private static readonly DatatypeImplementation s_NMTOKEN = new Datatype_NMTOKEN();
        private static readonly DatatypeImplementation s_NMTOKENS = (DatatypeImplementation)s_NMTOKEN.DeriveByList(1, null);
        private static readonly DatatypeImplementation s_nonNegativeInteger = new Datatype_nonNegativeInteger();
        private static readonly DatatypeImplementation s_nonPositiveInteger = new Datatype_nonPositiveInteger();
        private static readonly DatatypeImplementation s_normalizedString = new Datatype_normalizedString();
        private static readonly DatatypeImplementation s_NOTATION = new Datatype_NOTATION();
        private static readonly DatatypeImplementation s_positiveInteger = new Datatype_positiveInteger();
        private static readonly DatatypeImplementation s_QName = new Datatype_QName();
        private static readonly DatatypeImplementation s_QNameXdr = new Datatype_QNameXdr(); //XDR
        private static readonly DatatypeImplementation s_short = new Datatype_short();
        private static readonly DatatypeImplementation s_string = new Datatype_string();
        private static readonly DatatypeImplementation s_time = new Datatype_time();
        private static readonly DatatypeImplementation s_timeNoTz = new Datatype_timeNoTimeZone(); // XDR
        private static readonly DatatypeImplementation s_timeTz = new Datatype_timeTimeZone(); // XDR
        private static readonly DatatypeImplementation s_token = new Datatype_token();
        private static readonly DatatypeImplementation s_unsignedByte = new Datatype_unsignedByte();
        private static readonly DatatypeImplementation s_unsignedInt = new Datatype_unsignedInt();
        private static readonly DatatypeImplementation s_unsignedLong = new Datatype_unsignedLong();
        private static readonly DatatypeImplementation s_unsignedShort = new Datatype_unsignedShort();
        private static readonly DatatypeImplementation s_uuid = new Datatype_uuid(); // XDR
        private static readonly DatatypeImplementation s_year = new Datatype_year();
        private static readonly DatatypeImplementation s_yearMonth = new Datatype_yearMonth();

        //V1 compat types
        internal static readonly DatatypeImplementation c_normalizedStringV1Compat = new Datatype_normalizedStringV1Compat();
        internal static readonly DatatypeImplementation c_tokenV1Compat = new Datatype_tokenV1Compat();

        // XQuery types
        private static readonly DatatypeImplementation s_anyAtomicType = new Datatype_anyAtomicType();
        private static readonly DatatypeImplementation s_dayTimeDuration = new Datatype_dayTimeDuration();
        private static readonly DatatypeImplementation s_untypedAtomicType = new Datatype_untypedAtomicType();
        private static readonly DatatypeImplementation s_yearMonthDuration = new Datatype_yearMonthDuration();


        private sealed class SchemaDatatypeMap : IComparable
        {
            private readonly string _name;
            private readonly DatatypeImplementation _type;
            private readonly int _parentIndex;

            internal SchemaDatatypeMap(string name, DatatypeImplementation type)
            {
                _name = name;
                _type = type;
            }

            internal SchemaDatatypeMap(string name, DatatypeImplementation type, int parentIndex)
            {
                _name = name;
                _type = type;
                _parentIndex = parentIndex;
            }
            public static explicit operator DatatypeImplementation(SchemaDatatypeMap sdm) { return sdm._type; }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public int ParentIndex
            {
                get
                {
                    return _parentIndex;
                }
            }

            public int CompareTo(object? obj) { return string.Compare(_name, (string?)obj, StringComparison.Ordinal); }
        }

        private static readonly DatatypeImplementation?[] s_tokenizedTypes = {
            s_string,               // CDATA
            s_ID,                   // ID
            s_IDREF,                // IDREF
            s_IDREFS,               // IDREFS
            s_ENTITY,               // ENTITY
            s_ENTITIES,             // ENTITIES
            s_NMTOKEN,              // NMTOKEN
            s_NMTOKENS,             // NMTOKENS
            s_NOTATION,             // NOTATION
            s_ENUMERATION,          // ENUMERATION
            s_QNameXdr,             // QName
            s_NCName,               // NCName
            null
        };

        private static readonly DatatypeImplementation?[] s_tokenizedTypesXsd = {
            s_string,               // CDATA
            s_ID,                   // ID
            s_IDREF,                // IDREF
            s_IDREFS,               // IDREFS
            s_ENTITY,               // ENTITY
            s_ENTITIES,             // ENTITIES
            s_NMTOKEN,              // NMTOKEN
            s_NMTOKENS,             // NMTOKENS
            s_NOTATION,             // NOTATION
            s_ENUMERATION,          // ENUMERATION
            s_QName,                // QName
            s_NCName,               // NCName
            null
        };

        private static readonly SchemaDatatypeMap[] s_xdrTypes = {
            new SchemaDatatypeMap("bin.base64",          s_base64Binary),
            new SchemaDatatypeMap("bin.hex",             s_hexBinary),
            new SchemaDatatypeMap("boolean",             s_boolean),
            new SchemaDatatypeMap("char",                s_char),
            new SchemaDatatypeMap("date",                s_date),
            new SchemaDatatypeMap("dateTime",            s_dateTimeNoTz),
            new SchemaDatatypeMap("dateTime.tz",         s_dateTimeTz),
            new SchemaDatatypeMap("decimal",             s_decimal),
            new SchemaDatatypeMap("entities",            s_ENTITIES),
            new SchemaDatatypeMap("entity",              s_ENTITY),
            new SchemaDatatypeMap("enumeration",         s_ENUMERATION),
            new SchemaDatatypeMap("fixed.14.4",          s_fixed),
            new SchemaDatatypeMap("float",               s_doubleXdr),
            new SchemaDatatypeMap("float.ieee.754.32",   s_floatXdr),
            new SchemaDatatypeMap("float.ieee.754.64",   s_doubleXdr),
            new SchemaDatatypeMap("i1",                  s_byte),
            new SchemaDatatypeMap("i2",                  s_short),
            new SchemaDatatypeMap("i4",                  s_int),
            new SchemaDatatypeMap("i8",                  s_long),
            new SchemaDatatypeMap("id",                  s_ID),
            new SchemaDatatypeMap("idref",               s_IDREF),
            new SchemaDatatypeMap("idrefs",              s_IDREFS),
            new SchemaDatatypeMap("int",                 s_int),
            new SchemaDatatypeMap("nmtoken",             s_NMTOKEN),
            new SchemaDatatypeMap("nmtokens",            s_NMTOKENS),
            new SchemaDatatypeMap("notation",            s_NOTATION),
            new SchemaDatatypeMap("number",              s_doubleXdr),
            new SchemaDatatypeMap("r4",                  s_floatXdr),
            new SchemaDatatypeMap("r8",                  s_doubleXdr),
            new SchemaDatatypeMap("string",              s_string),
            new SchemaDatatypeMap("time",                s_timeNoTz),
            new SchemaDatatypeMap("time.tz",             s_timeTz),
            new SchemaDatatypeMap("ui1",                 s_unsignedByte),
            new SchemaDatatypeMap("ui2",                 s_unsignedShort),
            new SchemaDatatypeMap("ui4",                 s_unsignedInt),
            new SchemaDatatypeMap("ui8",                 s_unsignedLong),
            new SchemaDatatypeMap("uri",                 s_anyURI),
            new SchemaDatatypeMap("uuid",                s_uuid)
        };


        private static readonly SchemaDatatypeMap[] s_xsdTypes = {
            new SchemaDatatypeMap("ENTITIES",           s_ENTITIES, 11),
            new SchemaDatatypeMap("ENTITY",             s_ENTITY, 11),
            new SchemaDatatypeMap("ID",                 s_ID, 5),
            new SchemaDatatypeMap("IDREF",              s_IDREF, 5),
            new SchemaDatatypeMap("IDREFS",             s_IDREFS, 11),

            new SchemaDatatypeMap("NCName",             s_NCName, 9),
            new SchemaDatatypeMap("NMTOKEN",            s_NMTOKEN, 40),
            new SchemaDatatypeMap("NMTOKENS",           s_NMTOKENS, 11),
            new SchemaDatatypeMap("NOTATION",           s_NOTATION, 11),

            new SchemaDatatypeMap("Name",               s_name, 40),
            new SchemaDatatypeMap("QName",              s_QName, 11), //-> 10

            new SchemaDatatypeMap("anySimpleType",      s_anySimpleType, -1),
            new SchemaDatatypeMap("anyURI",             s_anyURI, 11),
            new SchemaDatatypeMap("base64Binary",       s_base64Binary, 11),
            new SchemaDatatypeMap("boolean",            s_boolean, 11),
            new SchemaDatatypeMap("byte",               s_byte, 37),
            new SchemaDatatypeMap("date",               s_date, 11),
            new SchemaDatatypeMap("dateTime",           s_dateTime, 11),
            new SchemaDatatypeMap("decimal",            s_decimal, 11),
            new SchemaDatatypeMap("double",             s_double, 11),
            new SchemaDatatypeMap("duration",           s_duration, 11), //->20

            new SchemaDatatypeMap("float",              s_float, 11),
            new SchemaDatatypeMap("gDay",               s_day, 11),
            new SchemaDatatypeMap("gMonth",             s_month, 11),
            new SchemaDatatypeMap("gMonthDay",          s_monthDay, 11),
            new SchemaDatatypeMap("gYear",              s_year, 11),
            new SchemaDatatypeMap("gYearMonth",         s_yearMonth, 11),
            new SchemaDatatypeMap("hexBinary",          s_hexBinary, 11),
            new SchemaDatatypeMap("int",                s_int, 31),
            new SchemaDatatypeMap("integer",            s_integer, 18),
            new SchemaDatatypeMap("language",           s_language, 40), //->30
            new SchemaDatatypeMap("long",               s_long, 29),

            new SchemaDatatypeMap("negativeInteger",    s_negativeInteger, 34),

            new SchemaDatatypeMap("nonNegativeInteger", s_nonNegativeInteger, 29),
            new SchemaDatatypeMap("nonPositiveInteger", s_nonPositiveInteger, 29),
            new SchemaDatatypeMap("normalizedString",   s_normalizedString, 38),

            new SchemaDatatypeMap("positiveInteger",    s_positiveInteger, 33),

            new SchemaDatatypeMap("short",              s_short, 28),
            new SchemaDatatypeMap("string",             s_string, 11),
            new SchemaDatatypeMap("time",               s_time, 11),
            new SchemaDatatypeMap("token",              s_token, 35), //->40
            new SchemaDatatypeMap("unsignedByte",       s_unsignedByte, 44),
            new SchemaDatatypeMap("unsignedInt",        s_unsignedInt, 43),
            new SchemaDatatypeMap("unsignedLong",       s_unsignedLong, 33),
            new SchemaDatatypeMap("unsignedShort",      s_unsignedShort, 42),
        };

        protected static int Compare(byte[] value1, byte[] value2) =>
            value1.AsSpan().SequenceEqual(value2) ? 0 : -1;

#if PRIYAL
        protected object GetValueToCheck(object value, IXmlNamespaceResolver nsmgr) {
            object valueToCheck = value;
            string resId;
            if (CanConvert(value, value.GetType(), this.ValueType, out resId)) {
                valueToCheck = this.ValueConverter.ChangeType(value, this.ValueType, nsmgr);
            }
            else {
                throw new XmlSchemaException(resId, string.Empty);
            }
            return valueToCheck;
        }
#endif
    }


    //List type
    internal sealed class Datatype_List : Datatype_anySimpleType
    {
        private readonly DatatypeImplementation _itemType;
        private readonly int _minListSize;

        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            XmlSchemaType? listItemType = null;
            XmlSchemaSimpleType? simpleType;
            XmlSchemaComplexType? complexType;
            complexType = schemaType as XmlSchemaComplexType;

            if (complexType != null)
            {
                do
                {
                    simpleType = complexType.BaseXmlSchemaType as XmlSchemaSimpleType;
                    if (simpleType != null)
                    {
                        break;
                    }
                    complexType = complexType.BaseXmlSchemaType as XmlSchemaComplexType;
                } while (complexType != null && complexType != XmlSchemaComplexType.AnyType);
            }
            else
            {
                simpleType = schemaType as XmlSchemaSimpleType;
            }
            if (simpleType != null)
            {
                do
                {
                    XmlSchemaSimpleTypeList? listType = simpleType.Content as XmlSchemaSimpleTypeList;
                    if (listType != null)
                    {
                        listItemType = listType.BaseItemType;
                        break;
                    }
                    simpleType = simpleType.BaseXmlSchemaType as XmlSchemaSimpleType;
                } while (simpleType != null && simpleType != DatatypeImplementation.AnySimpleType);
            }

            //Get built-in simple type for the typecode
            listItemType ??= DatatypeImplementation.GetSimpleTypeFromTypeCode(schemaType!.Datatype!.TypeCode);

            return XmlListConverter.Create(listItemType.ValueConverter);
        }
        internal Datatype_List(DatatypeImplementation type, int minListSize)
        {
            _itemType = type;
            _minListSize = minListSize;
        }

        internal override int Compare(object value1, object value2)
        {
            System.Array arr1 = (System.Array)value1;
            System.Array arr2 = (System.Array)value2;

            Debug.Assert(arr1 != null && arr2 != null);
            int length = arr1.Length;
            if (length != arr2.Length)
            {
                return -1;
            }

            XmlAtomicValue[]? atomicValues1 = arr1 as XmlAtomicValue[];
            if (atomicValues1 != null)
            {
                XmlAtomicValue[]? atomicValues2 = arr2 as XmlAtomicValue[];
                Debug.Assert(atomicValues2 != null);
                XmlSchemaType xmlType1;
                for (int i = 0; i < atomicValues1.Length; i++)
                {
                    xmlType1 = atomicValues1[i].XmlType;
                    if (xmlType1 != atomicValues2[i].XmlType || !xmlType1.Datatype!.IsEqual(atomicValues1[i].TypedValue, atomicValues2[i].TypedValue))
                    {
                        return -1;
                    }
                }
                return 0;
            }
            else
            {
                for (int i = 0; i < arr1.Length; i++)
                {
                    if (_itemType.Compare(arr1.GetValue(i)!, arr2.GetValue(i)!) != 0)
                    {
                        return -1;
                    }
                }
                return 0;
            }
        }

        public override Type ValueType { get { return ListValueType; } }

        public override XmlTokenizedType TokenizedType { get { return _itemType.TokenizedType; } }

        internal override Type ListValueType { get { return _itemType.ListValueType; } }

        internal override FacetsChecker FacetsChecker { get { return listFacetsChecker; } }

        public override XmlTypeCode TypeCode
        {
            get
            {
                return _itemType.TypeCode;
            }
        }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace | RestrictionFlags.Pattern;
            }
        }
        internal DatatypeImplementation ItemType { get { return _itemType; } }

        internal override Exception? TryParseValue(object value, XmlNameTable? nameTable, IXmlNamespaceResolver? namespaceResolver, out object? typedValue)
        {
            ArgumentNullException.ThrowIfNull(value);

            Exception? exception;
            string? s = value as string;
            typedValue = null;
            if (s != null)
            {
                return TryParseValue(s, nameTable, namespaceResolver, out typedValue);
            }

            try
            {
                object valueToCheck = this.ValueConverter.ChangeType(value, this.ValueType, namespaceResolver);
                Array valuesToCheck = (valueToCheck as Array)!;
                Debug.Assert(valuesToCheck != null);

                bool checkItemLexical = _itemType.HasLexicalFacets;
                bool checkItemValue = _itemType.HasValueFacets;
                object item;
                FacetsChecker itemFacetsChecker = _itemType.FacetsChecker;
                XmlValueConverter itemValueConverter = _itemType.ValueConverter;

                for (int i = 0; i < valuesToCheck.Length; i++)
                {
                    item = valuesToCheck.GetValue(i)!;
                    if (checkItemLexical)
                    {
                        string s1 = (string)itemValueConverter.ChangeType(item, typeof(string), namespaceResolver);
                        exception = itemFacetsChecker.CheckLexicalFacets(ref s1, _itemType);
                        if (exception != null) goto Error;
                    }
                    if (checkItemValue)
                    {
                        exception = itemFacetsChecker.CheckValueFacets(item, _itemType);
                        if (exception != null) goto Error;
                    }
                }

                //Check facets on the list itself
                if (this.HasLexicalFacets)
                {
                    string s1 = (string)this.ValueConverter.ChangeType(valueToCheck, typeof(string), namespaceResolver);
                    exception = listFacetsChecker.CheckLexicalFacets(ref s1, this);
                    if (exception != null) goto Error;
                }
                if (this.HasValueFacets)
                {
                    exception = listFacetsChecker.CheckValueFacets(valueToCheck, this);
                    if (exception != null) goto Error;
                }
                typedValue = valueToCheck;
                return null;
            }
            catch (FormatException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (InvalidCastException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (OverflowException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (ArgumentException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }

        Error:
            return exception;
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = listFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            ArrayList values = new ArrayList();
            object array;
            if (_itemType.Variety == XmlSchemaDatatypeVariety.Union)
            {
                object? unionTypedValue;
                string[] splitString = XmlConvert.SplitString(s);
                for (int i = 0; i < splitString.Length; ++i)
                {
                    //Parse items in list according to the itemType
                    exception = _itemType.TryParseValue(splitString[i], nameTable, nsmgr, out unionTypedValue);
                    if (exception != null) goto Error;

                    Debug.Assert(unionTypedValue != null);

                    XsdSimpleValue simpleValue = (XsdSimpleValue)unionTypedValue;
                    values.Add(new XmlAtomicValue(simpleValue.XmlType, simpleValue.TypedValue, nsmgr));
                }
                array = ToArray(values, typeof(XmlAtomicValue[]));
            }
            else
            { //Variety == List or Atomic
                string[] splitString = XmlConvert.SplitString(s);
                for (int i = 0; i < splitString.Length; ++i)
                {
                    exception = _itemType.TryParseValue(splitString[i], nameTable, nsmgr, out typedValue);
                    if (exception != null) goto Error;

                    values.Add(typedValue);
                }
                Debug.Assert(_itemType.ListValueType.GetElementType() == _itemType.ValueType);
                array = ToArray(values, _itemType.ListValueType);
            }
            if (values.Count < _minListSize)
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = listFacetsChecker.CheckValueFacets(array, this);
            if (exception != null) goto Error;

            typedValue = array;

            return null;

        Error:
            return exception;

            // TODO: Replace with https://github.com/dotnet/runtime/issues/76478 once available
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
                Justification = "Array type is always present as it is passed in as a parameter.")]
            static Array ToArray(ArrayList values, Type arrayType)
                => values.ToArray(arrayType.GetElementType()!);
        }
    }

    //Union datatype
    internal sealed class Datatype_union : Datatype_anySimpleType
    {
        private readonly XmlSchemaSimpleType[] _types;

        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlUnionConverter.Create(schemaType);
        }

        internal Datatype_union(XmlSchemaSimpleType[] types)
        {
            _types = types;
        }

        internal override int Compare(object value1, object value2)
        {
            XsdSimpleValue? simpleValue1 = value1 as XsdSimpleValue;
            XsdSimpleValue? simpleValue2 = value2 as XsdSimpleValue;

            if (simpleValue1 == null || simpleValue2 == null)
            {
                return -1;
            }

            XmlSchemaType schemaType1 = simpleValue1.XmlType;
            XmlSchemaType schemaType2 = simpleValue2.XmlType;

            if (schemaType1 == schemaType2)
            {
                XmlSchemaDatatype datatype = schemaType1.Datatype!;
                return datatype.Compare(simpleValue1.TypedValue, simpleValue2.TypedValue);
            }

            return -1;
        }

        public override Type ValueType { get { return typeof(object); } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.AnyAtomicType; } }

        internal override FacetsChecker FacetsChecker { get { return unionFacetsChecker; } }

        internal override Type ListValueType { get { return typeof(object[]); } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                    RestrictionFlags.Enumeration;
            }
        }

        internal XmlSchemaSimpleType[] BaseMemberTypes
        {
            get
            {
                return _types;
            }
        }

        internal bool HasAtomicMembers()
        {
            for (int i = 0; i < _types.Length; ++i)
            {
                if (_types[i].Datatype!.Variety == XmlSchemaDatatypeVariety.List)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool IsUnionBaseOf(DatatypeImplementation derivedType)
        {
            for (int i = 0; i < _types.Length; ++i)
            {
                if (derivedType.IsDerivedFrom(_types[i].Datatype!))
                {
                    return true;
                }
            }
            return false;
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            XmlSchemaSimpleType? memberType = null;

            typedValue = null;

            exception = unionFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            //Parse string to CLR value
            for (int i = 0; i < _types.Length; ++i)
            {
                exception = _types[i].Datatype!.TryParseValue(s, nameTable, nsmgr, out typedValue);
                if (exception == null)
                {
                    memberType = _types[i];
                    break;
                }
            }

            Debug.Assert(typedValue != null);

            if (memberType == null)
            {
                exception = new XmlSchemaException(SR.Sch_UnionFailedEx, s);
                goto Error;
            }

            typedValue = new XsdSimpleValue(memberType, typedValue);
            exception = unionFacetsChecker.CheckValueFacets(typedValue, this);
            if (exception != null) goto Error;

            return null;

        Error:
            return exception;
        }

        internal override Exception? TryParseValue(object value, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            ArgumentNullException.ThrowIfNull(value);

            Exception? exception;
            typedValue = null;
            string? s = value as string;
            if (s != null)
            {
                return TryParseValue(s, nameTable, nsmgr, out typedValue);
            }

            object? valueToCheck = null;
            XmlSchemaSimpleType? memberType = null;
            for (int i = 0; i < _types.Length; ++i)
            {
                if (_types[i].Datatype!.TryParseValue(value, nameTable, nsmgr, out valueToCheck) == null)
                { //no error
                    memberType = _types[i];
                    break;
                }
            }

            if (valueToCheck == null)
            {
                exception = new XmlSchemaException(SR.Sch_UnionFailedEx, value.ToString());
                goto Error;
            }

            try
            {
                if (this.HasLexicalFacets)
                {
                    string s1 = (string)this.ValueConverter.ChangeType(valueToCheck, typeof(string), nsmgr); //Using value here to avoid info loss
                    exception = unionFacetsChecker.CheckLexicalFacets(ref s1, this);
                    if (exception != null) goto Error;
                }

                Debug.Assert(memberType != null);
                typedValue = new XsdSimpleValue(memberType, valueToCheck);
                if (this.HasValueFacets)
                {
                    exception = unionFacetsChecker.CheckValueFacets(typedValue, this);
                    if (exception != null) goto Error;
                }
                return null;
            }
            catch (FormatException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (InvalidCastException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (OverflowException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }
            catch (ArgumentException e)
            { //Catching for exceptions thrown by ValueConverter
                exception = e;
            }

        Error:
            return exception;
        }
    }


    // Primitive datatypes
    internal class Datatype_anySimpleType : DatatypeImplementation
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlUntypedConverter.Untyped;
        }

        internal override FacetsChecker FacetsChecker { get { return miscFacetsChecker; } }

        public override Type ValueType { get { return typeof(string); } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.AnyAtomicType; } }

        internal override Type ListValueType { get { return typeof(string[]); } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.None; } }

        internal override RestrictionFlags ValidRestrictionFlags { get { return 0; } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override int Compare(object value1, object value2)
        {
            //Changed StringComparison.CurrentCulture to StringComparison.Ordinal to handle zero-weight code points like the cyrillic E
            return string.Compare(value1.ToString(), value2.ToString(), StringComparison.Ordinal);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            typedValue = XmlComplianceUtil.NonCDataNormalize(s); //Whitespace facet is treated as collapse since thats the way it was in Everett
            return null;
        }
    }

    internal class Datatype_anyAtomicType : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlAnyConverter.AnyAtomic;
        }
        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Preserve; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.AnyAtomicType; } }
    }

    internal sealed class Datatype_untypedAtomicType : Datatype_anyAtomicType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlUntypedConverter.Untyped;
        }
        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Preserve; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.UntypedAtomic; } }
    }


    /*
      <xs:simpleType name="string" id="string">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality" value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
                    source="http://www.w3.org/TR/xmlschema-2/#string"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="preserve" id="string.preserve"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_string : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlStringConverter.Create(schemaType);
        }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Preserve; } }

        internal override FacetsChecker FacetsChecker { get { return stringFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.String; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.CDATA; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = stringFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            exception = stringFacetsChecker.CheckValueFacets(s, this);
            if (exception != null) goto Error;

            typedValue = s;
            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="boolean" id="boolean">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality" value="finite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#boolean"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse" fixed="true"
            id="boolean.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_boolean : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlBooleanConverter.Create(schemaType!);
        }

        internal override FacetsChecker FacetsChecker { get { return miscFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Boolean; } }

        public override Type ValueType { get { return typeof(bool); } }

        internal override Type ListValueType { get { return typeof(bool[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                       RestrictionFlags.WhiteSpace;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((bool)value1).CompareTo((bool)value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            typedValue = null;

            exception = miscFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            bool boolValue;
            exception = XmlConvert.TryToBoolean(s, out boolValue);
            if (exception != null) goto Error;

            typedValue = boolValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="float" id="float">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="total"/>
            <hfp:hasProperty name="bounded" value="true"/>
            <hfp:hasProperty name="cardinality" value="finite"/>
            <hfp:hasProperty name="numeric" value="true"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#float"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse" fixed="true"
            id="float.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_float : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlNumeric2Converter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return numeric2FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Float; } }

        public override Type ValueType { get { return typeof(float); } }

        internal override Type ListValueType { get { return typeof(float[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace |
                       RestrictionFlags.MinExclusive |
                       RestrictionFlags.MinInclusive |
                       RestrictionFlags.MaxExclusive |
                       RestrictionFlags.MaxInclusive;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((float)value1).CompareTo((float)value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = numeric2FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            float singleValue;
            exception = XmlConvert.TryToSingle(s, out singleValue);
            if (exception != null) goto Error;

            exception = numeric2FacetsChecker.CheckValueFacets(singleValue, this);
            if (exception != null) goto Error;

            typedValue = singleValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="double" id="double">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="total"/>
            <hfp:hasProperty name="bounded" value="true"/>
            <hfp:hasProperty name="cardinality" value="finite"/>
            <hfp:hasProperty name="numeric" value="true"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#double"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="double.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_double : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlNumeric2Converter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return numeric2FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Double; } }

        public override Type ValueType { get { return typeof(double); } }

        internal override Type ListValueType { get { return typeof(double[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace |
                       RestrictionFlags.MinExclusive |
                       RestrictionFlags.MinInclusive |
                       RestrictionFlags.MaxExclusive |
                       RestrictionFlags.MaxInclusive;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((double)value1).CompareTo((double)value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            typedValue = null;

            exception = numeric2FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            double doubleValue;
            exception = XmlConvert.TryToDouble(s, out doubleValue);
            if (exception != null) goto Error;

            exception = numeric2FacetsChecker.CheckValueFacets(doubleValue, this);
            if (exception != null) goto Error;

            typedValue = doubleValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="decimal" id="decimal">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="totalDigits"/>
            <hfp:hasFacet name="fractionDigits"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="total"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="true"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#decimal"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="decimal.whiteSpace"/>
        </xs:restriction>
       </xs:simpleType>
    */
    internal class Datatype_decimal : Datatype_anySimpleType
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.MinValue, decimal.MaxValue);

        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlNumeric10Converter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Decimal; } }

        public override Type ValueType { get { return typeof(decimal); } }

        internal override Type ListValueType { get { return typeof(decimal[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.TotalDigits |
                       RestrictionFlags.FractionDigits |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace |
                       RestrictionFlags.MinExclusive |
                       RestrictionFlags.MinInclusive |
                       RestrictionFlags.MaxExclusive |
                       RestrictionFlags.MaxInclusive;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((decimal)value1).CompareTo((decimal)value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            decimal decimalValue;
            exception = XmlConvert.TryToDecimal(s, out decimalValue);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets(decimalValue, this);
            if (exception != null) goto Error;

            typedValue = decimalValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
       <xs:simpleType name="duration" id="duration">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#duration"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="duration.whiteSpace"/>
        </xs:restriction>
       </xs:simpleType>
    */
    internal class Datatype_duration : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return durationFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Duration; } }

        public override Type ValueType { get { return typeof(TimeSpan); } }

        internal override Type ListValueType { get { return typeof(TimeSpan[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace |
                       RestrictionFlags.MinExclusive |
                       RestrictionFlags.MinInclusive |
                       RestrictionFlags.MaxExclusive |
                       RestrictionFlags.MaxInclusive;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((TimeSpan)value1).CompareTo((TimeSpan)value2);
        }


        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            typedValue = null;

            if (string.IsNullOrEmpty(s))
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = durationFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            TimeSpan timeSpanValue;
            exception = XmlConvert.TryToTimeSpan(s, out timeSpanValue);
            if (exception != null) goto Error;

            exception = durationFacetsChecker.CheckValueFacets(timeSpanValue, this);
            if (exception != null) goto Error;

            typedValue = timeSpanValue;

            return null;

        Error:
            return exception;
        }
    }

    internal sealed class Datatype_yearMonthDuration : Datatype_duration
    {
        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            typedValue = null;

            if (string.IsNullOrEmpty(s))
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = durationFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            XsdDuration duration;
            exception = XsdDuration.TryParse(s, XsdDuration.DurationType.YearMonthDuration, out duration);
            if (exception != null) goto Error;

            TimeSpan timeSpanValue;

            exception = duration.TryToTimeSpan(XsdDuration.DurationType.YearMonthDuration, out timeSpanValue);
            if (exception != null) goto Error;

            exception = durationFacetsChecker.CheckValueFacets(timeSpanValue, this);
            if (exception != null) goto Error;

            typedValue = timeSpanValue;

            return null;

        Error:
            return exception;
        }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.YearMonthDuration; } }
    }

    internal sealed class Datatype_dayTimeDuration : Datatype_duration
    {
        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            if (string.IsNullOrEmpty(s))
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = durationFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            XsdDuration duration;
            exception = XsdDuration.TryParse(s, XsdDuration.DurationType.DayTimeDuration, out duration);
            if (exception != null) goto Error;

            TimeSpan timeSpanValue;
            exception = duration.TryToTimeSpan(XsdDuration.DurationType.DayTimeDuration, out timeSpanValue);
            if (exception != null) goto Error;

            exception = durationFacetsChecker.CheckValueFacets(timeSpanValue, this);
            if (exception != null) goto Error;

            typedValue = timeSpanValue;

            return null;

        Error:
            return exception;
        }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.DayTimeDuration; } }
    }

    internal class Datatype_dateTimeBase : Datatype_anySimpleType
    {
        private readonly XsdDateTimeFlags _dateTimeFlags;

        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlDateTimeConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return dateTimeFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.DateTime; } }

        internal Datatype_dateTimeBase(XsdDateTimeFlags dateTimeFlags)
        {
            _dateTimeFlags = dateTimeFlags;
        }

        public override Type ValueType { get { return typeof(DateTime); } }

        internal override Type ListValueType { get { return typeof(DateTime[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace |
                       RestrictionFlags.MinExclusive |
                       RestrictionFlags.MinInclusive |
                       RestrictionFlags.MaxExclusive |
                       RestrictionFlags.MaxInclusive;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            DateTime dateTime1 = (DateTime)value1;
            DateTime dateTime2 = (DateTime)value2;
            if (dateTime1.Kind == DateTimeKind.Unspecified || dateTime2.Kind == DateTimeKind.Unspecified)
            { //If either of them are unspecified, do not convert zones
                return dateTime1.CompareTo(dateTime2);
            }
            dateTime1 = dateTime1.ToUniversalTime();
            return dateTime1.CompareTo(dateTime2.ToUniversalTime());
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;
            typedValue = null;

            exception = dateTimeFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            XsdDateTime dateTime;
            if (!XsdDateTime.TryParse(s, _dateTimeFlags, out dateTime))
            {
                exception = new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, _dateTimeFlags.ToString()));
                goto Error;
            }

            DateTime dateTimeValue;
            try
            {
                dateTimeValue = (DateTime)dateTime;
            }
            catch (ArgumentException e)
            {
                exception = e;
                goto Error;
            }

            exception = dateTimeFacetsChecker.CheckValueFacets(dateTimeValue, this);
            if (exception != null) goto Error;

            typedValue = dateTimeValue;

            return null;

        Error:
            return exception;
        }
    }

    internal sealed class Datatype_dateTimeNoTimeZone : Datatype_dateTimeBase
    {
        internal Datatype_dateTimeNoTimeZone() : base(XsdDateTimeFlags.XdrDateTimeNoTz) { }
    }

    internal sealed class Datatype_dateTimeTimeZone : Datatype_dateTimeBase
    {
        internal Datatype_dateTimeTimeZone() : base(XsdDateTimeFlags.XdrDateTime) { }
    }

    /*
      <xs:simpleType name="dateTime" id="dateTime">
       <xs:annotation>
        <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#dateTime"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="dateTime.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_dateTime : Datatype_dateTimeBase
    {
        internal Datatype_dateTime() : base(XsdDateTimeFlags.DateTime) { }
    }

    internal sealed class Datatype_timeNoTimeZone : Datatype_dateTimeBase
    {
        internal Datatype_timeNoTimeZone() : base(XsdDateTimeFlags.XdrTimeNoTz) { }
    }

    internal sealed class Datatype_timeTimeZone : Datatype_dateTimeBase
    {
        internal Datatype_timeTimeZone() : base(XsdDateTimeFlags.Time) { }
    }

    /*
      <xs:simpleType name="time" id="time">
        <xs:annotation>
        <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#time"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="time.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_time : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Time; } }

        internal Datatype_time() : base(XsdDateTimeFlags.Time) { }
    }

    /*
      <xs:simpleType name="date" id="date">
       <xs:annotation>
        <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#date"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="date.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_date : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Date; } }

        internal Datatype_date() : base(XsdDateTimeFlags.Date) { }
    }

    /*
      <xs:simpleType name="gYearMonth" id="gYearMonth">
       <xs:annotation>
        <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#gYearMonth"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="gYearMonth.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_yearMonth : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.GYearMonth; } }

        internal Datatype_yearMonth() : base(XsdDateTimeFlags.GYearMonth) { }
    }


    /*
      <xs:simpleType name="gYear" id="gYear">
        <xs:annotation>
        <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#gYear"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="gYear.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_year : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.GYear; } }

        internal Datatype_year() : base(XsdDateTimeFlags.GYear) { }
    }

    /*
     <xs:simpleType name="gMonthDay" id="gMonthDay">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
           <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#gMonthDay"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
             <xs:whiteSpace value="collapse" fixed="true"
                    id="gMonthDay.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_monthDay : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.GMonthDay; } }

        internal Datatype_monthDay() : base(XsdDateTimeFlags.GMonthDay) { }
    }

    /*
      <xs:simpleType name="gDay" id="gDay">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#gDay"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
             <xs:whiteSpace value="collapse"  fixed="true"
                    id="gDay.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_day : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.GDay; } }

        internal Datatype_day() : base(XsdDateTimeFlags.GDay) { }
    }


    /*
     <xs:simpleType name="gMonth" id="gMonth">
        <xs:annotation>
      <xs:appinfo>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasFacet name="maxInclusive"/>
            <hfp:hasFacet name="maxExclusive"/>
            <hfp:hasFacet name="minInclusive"/>
            <hfp:hasFacet name="minExclusive"/>
            <hfp:hasProperty name="ordered" value="partial"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#gMonth"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
             <xs:whiteSpace value="collapse"  fixed="true"
                    id="gMonth.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_month : Datatype_dateTimeBase
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.GMonth; } }

        internal Datatype_month() : base(XsdDateTimeFlags.GMonth) { }
    }

    /*
       <xs:simpleType name="hexBinary" id="hexBinary">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#binary"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse" fixed="true"
            id="hexBinary.whiteSpace"/>
        </xs:restriction>
       </xs:simpleType>
    */
    internal sealed class Datatype_hexBinary : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return binaryFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.HexBinary; } }

        public override Type ValueType { get { return typeof(byte[]); } }

        internal override Type ListValueType { get { return typeof(byte[][]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return Compare((byte[])value1, (byte[])value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = binaryFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            byte[]? byteArrayValue;
            try
            {
                byteArrayValue = XmlConvert.FromBinHexString(s, false);
            }
            catch (ArgumentException e)
            {
                exception = e;
                goto Error;
            }
            catch (XmlException e)
            {
                exception = e;
                goto Error;
            }

            exception = binaryFacetsChecker.CheckValueFacets(byteArrayValue, this);
            if (exception != null) goto Error;

            typedValue = byteArrayValue;

            return null;

        Error:
            return exception;
        }
    }


    /*
     <xs:simpleType name="base64Binary" id="base64Binary">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
                    source="http://www.w3.org/TR/xmlschema-2/#base64Binary"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse" fixed="true"
            id="base64Binary.whiteSpace"/>
        </xs:restriction>
       </xs:simpleType>
    */
    internal sealed class Datatype_base64Binary : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return binaryFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Base64Binary; } }

        public override Type ValueType { get { return typeof(byte[]); } }

        internal override Type ListValueType { get { return typeof(byte[][]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return Compare((byte[])value1, (byte[])value2);
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = binaryFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            byte[]? byteArrayValue;
            try
            {
                byteArrayValue = Convert.FromBase64String(s);
            }
            catch (ArgumentException e)
            {
                exception = e;
                goto Error;
            }
            catch (FormatException e)
            {
                exception = e;
                goto Error;
            }

            exception = binaryFacetsChecker.CheckValueFacets(byteArrayValue, this);
            if (exception != null) goto Error;

            typedValue = byteArrayValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
       <xs:simpleType name="anyURI" id="anyURI">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#anyURI"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="anyURI.whiteSpace"/>
        </xs:restriction>
       </xs:simpleType>
    */
    internal sealed class Datatype_anyURI : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return stringFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.AnyUri; } }

        public override Type ValueType { get { return typeof(Uri); } }

        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check validity of Uri
            }
        }
        internal override Type ListValueType { get { return typeof(Uri[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        internal override int Compare(object value1, object value2)
        {
            return ((Uri)value1).Equals((Uri)value2) ? 0 : -1;
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = stringFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            Uri? uri;
            exception = XmlConvert.TryToUri(s, out uri);
            if (exception != null) goto Error;

            Debug.Assert(uri != null);

            string stringValue = uri.OriginalString;
            exception = StringFacetsChecker.CheckValueFacets(stringValue, this, false);
            if (exception != null) goto Error;

            typedValue = uri;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="QName" id="QName">
        <xs:annotation>
            <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#QName"/>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="QName.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_QName : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return qnameFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.QName; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.QName; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        public override Type ValueType { get { return typeof(XmlQualifiedName); } }

        internal override Type ListValueType { get { return typeof(XmlQualifiedName[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            if (string.IsNullOrEmpty(s))
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = qnameFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            XmlQualifiedName? qname;
            try
            {
                string prefix;
                qname = XmlQualifiedName.Parse(s, nsmgr!, out prefix);
            }
            catch (ArgumentException e)
            {
                exception = e;
                goto Error;
            }
            catch (XmlException e)
            {
                exception = e;
                goto Error;
            }

            exception = qnameFacetsChecker.CheckValueFacets(qname, this);
            if (exception != null) goto Error;

            typedValue = qname;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="normalizedString" id="normalizedString">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#normalizedString"/>
        </xs:annotation>
        <xs:restriction base="xs:string">
          <xs:whiteSpace value="replace"
            id="normalizedString.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_normalizedString : Datatype_string
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NormalizedString; } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Replace; } }

        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check validity of NormalizedString
            }
        }
    }

    internal class Datatype_normalizedStringV1Compat : Datatype_string
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NormalizedString; } }
        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check validity of NormalizedString
            }
        }
    }

    /*
      <xs:simpleType name="token" id="token">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#token"/>
        </xs:annotation>
        <xs:restriction base="xs:normalizedString">
          <xs:whiteSpace value="collapse" id="token.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_token : Datatype_normalizedString
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Token; } }
        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }
    }

    internal sealed class Datatype_tokenV1Compat : Datatype_normalizedStringV1Compat
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Token; } }
    }

    /*
      <xs:simpleType name="language" id="language">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#language"/>
        </xs:annotation>
        <xs:restriction base="xs:token">
          <xs:pattern
            value="([a-zA-Z]{2}|[iI]-[a-zA-Z]+|[xX]-[a-zA-Z]{1,8})(-[a-zA-Z]{1,8})*"
                    id="language.pattern">
            <xs:annotation>
              <xs:documentation
                    source="http://www.w3.org/TR/REC-xml#NT-LanguageID">
                pattern specifies the content of section 2.12 of XML 1.0e2
                and RFC 1766
              </xs:documentation>
            </xs:annotation>
          </xs:pattern>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_language : Datatype_token
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Language; } }
    }

    /*
      <xs:simpleType name="NMTOKEN" id="NMTOKEN">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#NMTOKEN"/>
        </xs:annotation>
        <xs:restriction base="xs:token">
          <xs:pattern value="\c+" id="NMTOKEN.pattern">
            <xs:annotation>
              <xs:documentation
                    source="http://www.w3.org/TR/REC-xml#NT-Nmtoken">
                pattern matches production 7 from the XML spec
              </xs:documentation>
            </xs:annotation>
          </xs:pattern>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_NMTOKEN : Datatype_token
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NmToken; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.NMTOKEN; } }
    }

    /*
      <xs:simpleType name="Name" id="Name">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#Name"/>
        </xs:annotation>
        <xs:restriction base="xs:token">
          <xs:pattern value="\i\c*" id="Name.pattern">
            <xs:annotation>
              <xs:documentation
                            source="http://www.w3.org/TR/REC-xml#NT-Name">
                pattern matches production 5 from the XML spec
              </xs:documentation>
            </xs:annotation>
          </xs:pattern>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_Name : Datatype_token
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Name; } }
    }

    /*
      <xs:simpleType name="NCName" id="NCName">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#NCName"/>
        </xs:annotation>
        <xs:restriction base="xs:Name">
          <xs:pattern value="[\i-[:]][\c-[:]]*" id="NCName.pattern">
            <xs:annotation>
              <xs:documentation
                    source="http://www.w3.org/TR/REC-xml-names/#NT-NCName">
                pattern matches production 4 from the Namespaces in XML spec
              </xs:documentation>
            </xs:annotation>
          </xs:pattern>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_NCName : Datatype_Name
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NCName; } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = stringFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            exception = stringFacetsChecker.CheckValueFacets(s, this);
            if (exception != null) goto Error;

            nameTable!.Add(s);

            typedValue = s;
            return null;

        Error:
            return exception;
        }
    }

    /*
       <xs:simpleType name="ID" id="ID">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#ID"/>
        </xs:annotation>
        <xs:restriction base="xs:NCName"/>
       </xs:simpleType>
    */
    internal sealed class Datatype_ID : Datatype_NCName
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Id; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.ID; } }
    }

    /*
       <xs:simpleType name="IDREF" id="IDREF">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#IDREF"/>
        </xs:annotation>
        <xs:restriction base="xs:NCName"/>
       </xs:simpleType>
    */
    internal sealed class Datatype_IDREF : Datatype_NCName
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Idref; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.IDREF; } }
    }

    /*
       <xs:simpleType name="ENTITY" id="ENTITY">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#ENTITY"/>
        </xs:annotation>
        <xs:restriction base="xs:NCName"/>
       </xs:simpleType>
    */
    internal sealed class Datatype_ENTITY : Datatype_NCName
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Entity; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.ENTITY; } }
    }

    /*
       <xs:simpleType name="NOTATION" id="NOTATION">
        <xs:annotation>
            <xs:appinfo>
            <hfp:hasFacet name="length"/>
            <hfp:hasFacet name="minLength"/>
            <hfp:hasFacet name="maxLength"/>
            <hfp:hasFacet name="pattern"/>
            <hfp:hasFacet name="enumeration"/>
            <hfp:hasFacet name="whiteSpace"/>
            <hfp:hasProperty name="ordered" value="false"/>
            <hfp:hasProperty name="bounded" value="false"/>
            <hfp:hasProperty name="cardinality"
                    value="countably infinite"/>
            <hfp:hasProperty name="numeric" value="false"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#NOTATION"/>
          <xs:documentation>
            NOTATION cannot be used directly in a schema; rather a type
            must be derived from it by specifying at least one enumeration
            facet whose value is the name of a NOTATION declared in the
            schema.
          </xs:documentation>
        </xs:annotation>
        <xs:restriction base="xs:anySimpleType">
          <xs:whiteSpace value="collapse"  fixed="true"
            id="NOTATION.whiteSpace"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_NOTATION : Datatype_anySimpleType
    {
        internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
        {
            return XmlMiscConverter.Create(schemaType);
        }

        internal override FacetsChecker FacetsChecker { get { return qnameFacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Notation; } }

        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.NOTATION; } }

        internal override RestrictionFlags ValidRestrictionFlags
        {
            get
            {
                return RestrictionFlags.Length |
                       RestrictionFlags.MinLength |
                       RestrictionFlags.MaxLength |
                       RestrictionFlags.Pattern |
                       RestrictionFlags.Enumeration |
                       RestrictionFlags.WhiteSpace;
            }
        }

        public override Type ValueType { get { return typeof(XmlQualifiedName); } }

        internal override Type ListValueType { get { return typeof(XmlQualifiedName[]); } }

        internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get { return XmlSchemaWhiteSpace.Collapse; } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            if (string.IsNullOrEmpty(s))
            {
                return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }

            exception = qnameFacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            XmlQualifiedName? qname;
            try
            {
                string prefix;
                qname = XmlQualifiedName.Parse(s, nsmgr!, out prefix);
            }
            catch (ArgumentException e)
            {
                exception = e;
                goto Error;
            }
            catch (XmlException e)
            {
                exception = e;
                goto Error;
            }

            exception = qnameFacetsChecker.CheckValueFacets(qname, this);
            if (exception != null) goto Error;

            typedValue = qname;

            return null;

        Error:
            return exception;
        }

        internal override void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller)
        {
            // Only datatypes that are derived from NOTATION by specifying a value for enumeration can be used in a schema.
            // Furthermore, the value of all enumeration facets must match the name of a notation declared in the current schema.                    //
            for (Datatype_NOTATION? dt = this; dt != null; dt = (Datatype_NOTATION?)dt.Base)
            {
                if (dt.Restriction != null && (dt.Restriction.Flags & RestrictionFlags.Enumeration) != 0)
                {
                    for (int i = 0; i < dt.Restriction.Enumeration!.Count; ++i)
                    {
                        XmlQualifiedName notation = (XmlQualifiedName)dt.Restriction.Enumeration[i]!;
                        if (!notations.Contains(notation))
                        {
                            throw new XmlSchemaException(SR.Sch_NotationRequired, caller);
                        }
                    }
                    return;
                }
            }
            throw new XmlSchemaException(SR.Sch_NotationRequired, caller);
        }
    }

    /*
      <xs:simpleType name="integer" id="integer">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#integer"/>
        </xs:annotation>
        <xs:restriction base="xs:decimal">
          <xs:fractionDigits value="0" fixed="true" id="integer.fractionDigits"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_integer : Datatype_decimal
    {
        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Integer; } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            decimal decimalValue;
            exception = XmlConvert.TryToInteger(s, out decimalValue);
            if (exception != null) goto Error;

            exception = FacetsChecker.CheckValueFacets(decimalValue, this);
            if (exception != null) goto Error;

            typedValue = decimalValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="nonPositiveInteger" id="nonPositiveInteger">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#negativeInteger"/>
        </xs:annotation>
        <xs:restriction base="xs:nonPositiveInteger">
          <xs:maxInclusive value="-1" id="negativeInteger.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_nonPositiveInteger : Datatype_integer
    {
        private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.MinValue, decimal.Zero);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NonPositiveInteger; } }

        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check range
            }
        }
    }


    /*
      <xs:simpleType name="negativeInteger" id="negativeInteger">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#negativeInteger"/>
        </xs:annotation>
        <xs:restriction base="xs:nonPositiveInteger">
          <xs:maxInclusive value="-1" id="negativeInteger.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_negativeInteger : Datatype_nonPositiveInteger
    {
        private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.MinValue, decimal.MinusOne);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NegativeInteger; } }
    }


    /*
      <xs:simpleType name="long" id="long">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasProperty name="bounded" value="true"/>
            <hfp:hasProperty name="cardinality" value="finite"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#long"/>
        </xs:annotation>
        <xs:restriction base="xs:integer">
          <xs:minInclusive value="-9223372036854775808" id="long.minInclusive"/>
          <xs:maxInclusive value="9223372036854775807" id="long.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_long : Datatype_integer
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(long.MinValue, long.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check range
            }
        }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Long; } }

        internal override int Compare(object value1, object value2)
        {
            return ((long)value1).CompareTo((long)value2);
        }

        public override Type ValueType { get { return typeof(long); } }

        internal override Type ListValueType { get { return typeof(long[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            long int64Value;
            exception = XmlConvert.TryToInt64(s, out int64Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets(int64Value, this);
            if (exception != null) goto Error;

            typedValue = int64Value;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="int" id="int">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#int"/>
        </xs:annotation>
        <xs:restriction base="xs:long">
          <xs:minInclusive value="-2147483648" id="int.minInclusive"/>
          <xs:maxInclusive value="2147483647" id="int.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_int : Datatype_long
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(int.MinValue, int.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Int; } }

        internal override int Compare(object value1, object value2)
        {
            return ((int)value1).CompareTo((int)value2);
        }

        public override Type ValueType { get { return typeof(int); } }

        internal override Type ListValueType { get { return typeof(int[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            int int32Value;
            exception = XmlConvert.TryToInt32(s, out int32Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets(int32Value, this);
            if (exception != null) goto Error;

            typedValue = int32Value;

            return null;

        Error:
            return exception;
        }
    }


    /*
      <xs:simpleType name="short" id="short">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#short"/>
        </xs:annotation>
        <xs:restriction base="xs:int">
          <xs:minInclusive value="-32768" id="short.minInclusive"/>
          <xs:maxInclusive value="32767" id="short.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_short : Datatype_int
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(short.MinValue, short.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Short; } }

        internal override int Compare(object value1, object value2)
        {
            return ((short)value1).CompareTo((short)value2);
        }

        public override Type ValueType { get { return typeof(short); } }

        internal override Type ListValueType { get { return typeof(short[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            short int16Value;
            exception = XmlConvert.TryToInt16(s, out int16Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets(int16Value, this);
            if (exception != null) goto Error;

            typedValue = int16Value;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="byte" id="byte">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#byte"/>
        </xs:annotation>
        <xs:restriction base="xs:short">
          <xs:minInclusive value="-128" id="byte.minInclusive"/>
          <xs:maxInclusive value="127" id="byte.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_byte : Datatype_short
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(sbyte.MinValue, sbyte.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.Byte; } }

        internal override int Compare(object value1, object value2)
        {
            return ((sbyte)value1).CompareTo((sbyte)value2);
        }

        public override Type ValueType { get { return typeof(sbyte); } }

        internal override Type ListValueType { get { return typeof(sbyte[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            sbyte sbyteValue;
            exception = XmlConvert.TryToSByte(s, out sbyteValue);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets((short)sbyteValue, this);
            if (exception != null) goto Error;

            typedValue = sbyteValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="nonNegativeInteger" id="nonNegativeInteger">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#nonNegativeInteger"/>
        </xs:annotation>
        <xs:restriction base="xs:integer">
          <xs:minInclusive value="0" id="nonNegativeInteger.minInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_nonNegativeInteger : Datatype_integer
    {
        private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.Zero, decimal.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.NonNegativeInteger; } }

        internal override bool HasValueFacets
        {
            get
            {
                return true; //Built-in facet to check range
            }
        }
    }

    /*
      <xs:simpleType name="unsignedLong" id="unsignedLong">
        <xs:annotation>
          <xs:appinfo>
            <hfp:hasProperty name="bounded" value="true"/>
            <hfp:hasProperty name="cardinality" value="finite"/>
          </xs:appinfo>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#unsignedLong"/>
        </xs:annotation>
        <xs:restriction base="xs:nonNegativeInteger">
          <xs:maxInclusive value="18446744073709551615"
            id="unsignedLong.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_unsignedLong : Datatype_nonNegativeInteger
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(ulong.MinValue, ulong.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.UnsignedLong; } }

        internal override int Compare(object value1, object value2)
        {
            return ((ulong)value1).CompareTo((ulong)value2);
        }

        public override Type ValueType { get { return typeof(ulong); } }

        internal override Type ListValueType { get { return typeof(ulong[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            ulong uint64Value;
            exception = XmlConvert.TryToUInt64(s, out uint64Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets((decimal)uint64Value, this);
            if (exception != null) goto Error;

            typedValue = uint64Value;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="unsignedInt" id="unsignedInt">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#unsignedInt"/>
        </xs:annotation>
        <xs:restriction base="xs:unsignedLong">
          <xs:maxInclusive value="4294967295"
            id="unsignedInt.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_unsignedInt : Datatype_unsignedLong
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(uint.MinValue, uint.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.UnsignedInt; } }

        internal override int Compare(object value1, object value2)
        {
            return ((uint)value1).CompareTo((uint)value2);
        }

        public override Type ValueType { get { return typeof(uint); } }

        internal override Type ListValueType { get { return typeof(uint[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            uint uint32Value;
            exception = XmlConvert.TryToUInt32(s, out uint32Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets((long)uint32Value, this);
            if (exception != null) goto Error;

            typedValue = uint32Value;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="unsignedShort" id="unsignedShort">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#unsignedShort"/>
        </xs:annotation>
        <xs:restriction base="xs:unsignedInt">
          <xs:maxInclusive value="65535"
            id="unsignedShort.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal class Datatype_unsignedShort : Datatype_unsignedInt
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(ushort.MinValue, ushort.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.UnsignedShort; } }

        internal override int Compare(object value1, object value2)
        {
            return ((ushort)value1).CompareTo((ushort)value2);
        }

        public override Type ValueType { get { return typeof(ushort); } }

        internal override Type ListValueType { get { return typeof(ushort[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            ushort uint16Value;
            exception = XmlConvert.TryToUInt16(s, out uint16Value);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets((int)uint16Value, this);
            if (exception != null) goto Error;

            typedValue = uint16Value;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="unsignedByte" id="unsignedBtype">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#unsignedByte"/>
        </xs:annotation>
        <xs:restriction base="xs:unsignedShort">
          <xs:maxInclusive value="255" id="unsignedByte.maxInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_unsignedByte : Datatype_unsignedShort
    {
        private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(byte.MinValue, byte.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.UnsignedByte; } }

        internal override int Compare(object value1, object value2)
        {
            return ((byte)value1).CompareTo((byte)value2);
        }

        public override Type ValueType { get { return typeof(byte); } }

        internal override Type ListValueType { get { return typeof(byte[]); } }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            exception = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
            if (exception != null) goto Error;

            byte byteValue;
            exception = XmlConvert.TryToByte(s, out byteValue);
            if (exception != null) goto Error;

            exception = s_numeric10FacetsChecker.CheckValueFacets((short)byteValue, this);
            if (exception != null) goto Error;

            typedValue = byteValue;

            return null;

        Error:
            return exception;
        }
    }

    /*
      <xs:simpleType name="positiveInteger" id="positiveInteger">
        <xs:annotation>
          <xs:documentation
            source="http://www.w3.org/TR/xmlschema-2/#positiveInteger"/>
        </xs:annotation>
        <xs:restriction base="xs:nonNegativeInteger">
          <xs:minInclusive value="1" id="positiveInteger.minInclusive"/>
        </xs:restriction>
      </xs:simpleType>
    */
    internal sealed class Datatype_positiveInteger : Datatype_nonNegativeInteger
    {
        private static readonly FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(decimal.One, decimal.MaxValue);

        internal override FacetsChecker FacetsChecker { get { return s_numeric10FacetsChecker; } }

        public override XmlTypeCode TypeCode { get { return XmlTypeCode.PositiveInteger; } }
    }

    /*
        XDR
    */
    internal sealed class Datatype_doubleXdr : Datatype_double
    {
        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            double value;
            try
            {
                value = XmlConvert.ToDouble(s);
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }

            if (!double.IsFinite(value))
            {
                throw new XmlSchemaException(SR.Sch_InvalidValue, s);
            }
            return value;
        }
    }

    internal sealed class Datatype_floatXdr : Datatype_float
    {
        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            float value;
            try
            {
                value = XmlConvert.ToSingle(s);
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }

            if (!float.IsFinite(value))
            {
                throw new XmlSchemaException(SR.Sch_InvalidValue, s);
            }
            return value;
        }
    }

    internal sealed class Datatype_QNameXdr : Datatype_anySimpleType
    {
        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.QName; } }

        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
            }
            ArgumentNullException.ThrowIfNull(nsmgr);

            string prefix;
            try
            {
                return XmlQualifiedName.Parse(s.Trim(), nsmgr, out prefix);
            }
            catch (XmlSchemaException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }
        }

        public override Type ValueType { get { return typeof(XmlQualifiedName); } }

        internal override Type ListValueType { get { return typeof(XmlQualifiedName[]); } }
    }

    internal sealed class Datatype_ENUMERATION : Datatype_NMTOKEN
    {
        public override XmlTokenizedType TokenizedType { get { return XmlTokenizedType.ENUMERATION; } }
    }

    internal sealed class Datatype_char : Datatype_anySimpleType
    {
        public override Type ValueType { get { return typeof(char); } }

        internal override Type ListValueType { get { return typeof(char[]); } }

        internal override RestrictionFlags ValidRestrictionFlags { get { return 0; } } //XDR only

        internal override int Compare(object value1, object value2)
        {
            // this should be culture sensitive - comparing values
            return ((char)value1).CompareTo((char)value2);
        }

        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            try
            {
                return XmlConvert.ToChar(s);
            }
            catch (XmlSchemaException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            char charValue;
            exception = XmlConvert.TryToChar(s, out charValue);
            if (exception != null) goto Error;

            typedValue = charValue;

            return null;

        Error:
            return exception;
        }
    }

    internal sealed class Datatype_fixed : Datatype_decimal
    {
        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            Exception? exception;

            try
            {
                decimal value = XmlConvert.ToDecimal(s);
                exception = Numeric10FacetsChecker.CheckTotalAndFractionDigits(value, 14 + 4, 4, true, true);
                if (exception != null) goto Error;

                return value;
            }
            catch (XmlSchemaException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }
        Error:
            throw exception;
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            decimal decimalValue;
            exception = XmlConvert.TryToDecimal(s, out decimalValue);
            if (exception != null) goto Error;

            exception = Numeric10FacetsChecker.CheckTotalAndFractionDigits(decimalValue, 14 + 4, 4, true, true);
            if (exception != null) goto Error;

            typedValue = decimalValue;

            return null;

        Error:
            return exception;
        }
    }

    internal sealed class Datatype_uuid : Datatype_anySimpleType
    {
        public override Type ValueType { get { return typeof(Guid); } }

        internal override Type ListValueType { get { return typeof(Guid[]); } }

        internal override RestrictionFlags ValidRestrictionFlags { get { return 0; } }

        internal override int Compare(object value1, object value2)
        {
            return ((Guid)value1).Equals(value2) ? 0 : -1;
        }

        public override object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr)
        {
            try
            {
                return XmlConvert.ToGuid(s);
            }
            catch (XmlSchemaException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XmlSchemaException(SR.Format(SR.Sch_InvalidValue, s), e);
            }
        }

        internal override Exception? TryParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr, out object? typedValue)
        {
            Exception? exception;

            typedValue = null;

            Guid guid;
            exception = XmlConvert.TryToGuid(s, out guid);
            if (exception != null) goto Error;

            typedValue = guid;

            return null;

        Error:
            return exception;
        }
    }
}
