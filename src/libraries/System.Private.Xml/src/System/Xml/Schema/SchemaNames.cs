// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Diagnostics;

    internal sealed class SchemaNames
    {
        private XmlNameTable _nameTable;

        public string NsDataType;
        public string NsDataTypeAlias;
        public string NsDataTypeOld;
        public string NsXml;
        public string NsXmlNs;
        public string NsXdr;
        public string NsXdrAlias;
        public string NsXs;
        public string NsXsi;
        public string XsiType;
        public string XsiNil;
        public string XsiSchemaLocation;
        public string XsiNoNamespaceSchemaLocation;
        public string XsdSchema;
        public string XdrSchema;

        public XmlQualifiedName QnPCData;
        public XmlQualifiedName QnXml;
        public XmlQualifiedName QnXmlNs;
        public XmlQualifiedName QnDtDt;
        public XmlQualifiedName QnXmlLang;

        public XmlQualifiedName QnName;
        public XmlQualifiedName QnType;
        public XmlQualifiedName QnMaxOccurs;
        public XmlQualifiedName QnMinOccurs;
        public XmlQualifiedName QnInfinite;
        public XmlQualifiedName QnModel;
        public XmlQualifiedName QnOpen;
        public XmlQualifiedName QnClosed;
        public XmlQualifiedName QnContent;
        public XmlQualifiedName QnMixed;
        public XmlQualifiedName QnEmpty;
        public XmlQualifiedName QnEltOnly;
        public XmlQualifiedName QnTextOnly;
        public XmlQualifiedName QnOrder;
        public XmlQualifiedName QnSeq;
        public XmlQualifiedName QnOne;
        public XmlQualifiedName QnMany;
        public XmlQualifiedName QnRequired;
        public XmlQualifiedName QnYes;
        public XmlQualifiedName QnNo;
        public XmlQualifiedName QnString;
        public XmlQualifiedName QnID;
        public XmlQualifiedName QnIDRef;
        public XmlQualifiedName QnIDRefs;
        public XmlQualifiedName QnEntity;
        public XmlQualifiedName QnEntities;
        public XmlQualifiedName QnNmToken;
        public XmlQualifiedName QnNmTokens;
        public XmlQualifiedName QnEnumeration;
        public XmlQualifiedName QnDefault;
        public XmlQualifiedName QnXdrSchema;
        public XmlQualifiedName QnXdrElementType;
        public XmlQualifiedName QnXdrElement;
        public XmlQualifiedName QnXdrGroup;
        public XmlQualifiedName QnXdrAttributeType;
        public XmlQualifiedName QnXdrAttribute;
        public XmlQualifiedName QnXdrDataType;
        public XmlQualifiedName QnXdrDescription;
        public XmlQualifiedName QnXdrExtends;
        public XmlQualifiedName QnXdrAliasSchema;
        public XmlQualifiedName QnDtType;
        public XmlQualifiedName QnDtValues;
        public XmlQualifiedName QnDtMaxLength;
        public XmlQualifiedName QnDtMinLength;
        public XmlQualifiedName QnDtMax;
        public XmlQualifiedName QnDtMin;
        public XmlQualifiedName QnDtMinExclusive;
        public XmlQualifiedName QnDtMaxExclusive;
        // For XSD Schema
        public XmlQualifiedName QnTargetNamespace;
        public XmlQualifiedName QnVersion;
        public XmlQualifiedName QnFinalDefault;
        public XmlQualifiedName QnBlockDefault;
        public XmlQualifiedName QnFixed;
        public XmlQualifiedName QnAbstract;
        public XmlQualifiedName QnBlock;
        public XmlQualifiedName QnSubstitutionGroup;
        public XmlQualifiedName QnFinal;
        public XmlQualifiedName QnNillable;
        public XmlQualifiedName QnRef;
        public XmlQualifiedName QnBase;
        public XmlQualifiedName QnDerivedBy;
        public XmlQualifiedName QnNamespace;
        public XmlQualifiedName QnProcessContents;
        public XmlQualifiedName QnRefer;
        public XmlQualifiedName QnPublic;
        public XmlQualifiedName QnSystem;
        public XmlQualifiedName QnSchemaLocation;
        public XmlQualifiedName QnValue;
        public XmlQualifiedName QnUse;
        public XmlQualifiedName QnForm;
        public XmlQualifiedName QnElementFormDefault;
        public XmlQualifiedName QnAttributeFormDefault;
        public XmlQualifiedName QnItemType;
        public XmlQualifiedName QnMemberTypes;
        public XmlQualifiedName QnXPath;
        public XmlQualifiedName QnXsdSchema;
        public XmlQualifiedName QnXsdAnnotation;
        public XmlQualifiedName QnXsdInclude;
        public XmlQualifiedName QnXsdImport;
        public XmlQualifiedName QnXsdElement;
        public XmlQualifiedName QnXsdAttribute;
        public XmlQualifiedName QnXsdAttributeGroup;
        public XmlQualifiedName QnXsdAnyAttribute;
        public XmlQualifiedName QnXsdGroup;
        public XmlQualifiedName QnXsdAll;
        public XmlQualifiedName QnXsdChoice;
        public XmlQualifiedName QnXsdSequence;
        public XmlQualifiedName QnXsdAny;
        public XmlQualifiedName QnXsdNotation;
        public XmlQualifiedName QnXsdSimpleType;
        public XmlQualifiedName QnXsdComplexType;
        public XmlQualifiedName QnXsdUnique;
        public XmlQualifiedName QnXsdKey;
        public XmlQualifiedName QnXsdKeyRef;
        public XmlQualifiedName QnXsdSelector;
        public XmlQualifiedName QnXsdField;
        public XmlQualifiedName QnXsdMinExclusive;
        public XmlQualifiedName QnXsdMinInclusive;
        public XmlQualifiedName QnXsdMaxInclusive;
        public XmlQualifiedName QnXsdMaxExclusive;
        public XmlQualifiedName QnXsdTotalDigits;
        public XmlQualifiedName QnXsdFractionDigits;
        public XmlQualifiedName QnXsdLength;
        public XmlQualifiedName QnXsdMinLength;
        public XmlQualifiedName QnXsdMaxLength;
        public XmlQualifiedName QnXsdEnumeration;
        public XmlQualifiedName QnXsdPattern;
        public XmlQualifiedName QnXsdDocumentation;
        public XmlQualifiedName QnXsdAppinfo;
        public XmlQualifiedName QnSource;
        public XmlQualifiedName QnXsdComplexContent;
        public XmlQualifiedName QnXsdSimpleContent;
        public XmlQualifiedName QnXsdRestriction;
        public XmlQualifiedName QnXsdExtension;
        public XmlQualifiedName QnXsdUnion;
        public XmlQualifiedName QnXsdList;
        public XmlQualifiedName QnXsdWhiteSpace;
        public XmlQualifiedName QnXsdRedefine;
        public XmlQualifiedName QnXsdAnyType;

        internal XmlQualifiedName[] TokenToQName = new XmlQualifiedName[(int)Token.XmlLang + 1];

        public SchemaNames(XmlNameTable nameTable)
        {
            _nameTable = nameTable;
            NsDataType = nameTable.Add(XmlReservedNs.NsDataType);
            NsDataTypeAlias = nameTable.Add(XmlReservedNs.NsDataTypeAlias);
            NsDataTypeOld = nameTable.Add(XmlReservedNs.NsDataTypeOld);
            NsXml = nameTable.Add(XmlReservedNs.NsXml);
            NsXmlNs = nameTable.Add(XmlReservedNs.NsXmlNs);
            NsXdr = nameTable.Add(XmlReservedNs.NsXdr);
            NsXdrAlias = nameTable.Add(XmlReservedNs.NsXdrAlias);
            NsXs = nameTable.Add(XmlReservedNs.NsXs);
            NsXsi = nameTable.Add(XmlReservedNs.NsXsi);
            XsiType = nameTable.Add("type");
            XsiNil = nameTable.Add("nil");
            XsiSchemaLocation = nameTable.Add("schemaLocation");
            XsiNoNamespaceSchemaLocation = nameTable.Add("noNamespaceSchemaLocation");
            XsdSchema = nameTable.Add("schema");
            XdrSchema = nameTable.Add("Schema");


            QnPCData = new XmlQualifiedName(nameTable.Add("#PCDATA"));
            QnXml = new XmlQualifiedName(nameTable.Add("xml"));
            QnXmlNs = new XmlQualifiedName(nameTable.Add("xmlns"), NsXmlNs);
            QnDtDt = new XmlQualifiedName(nameTable.Add("dt"), NsDataType);
            QnXmlLang = new XmlQualifiedName(nameTable.Add("lang"), NsXml);

            // Empty namespace
            QnName = new XmlQualifiedName(nameTable.Add("name"));
            QnType = new XmlQualifiedName(nameTable.Add("type"));
            QnMaxOccurs = new XmlQualifiedName(nameTable.Add("maxOccurs"));
            QnMinOccurs = new XmlQualifiedName(nameTable.Add("minOccurs"));
            QnInfinite = new XmlQualifiedName(nameTable.Add("*"));
            QnModel = new XmlQualifiedName(nameTable.Add("model"));
            QnOpen = new XmlQualifiedName(nameTable.Add("open"));
            QnClosed = new XmlQualifiedName(nameTable.Add("closed"));
            QnContent = new XmlQualifiedName(nameTable.Add("content"));
            QnMixed = new XmlQualifiedName(nameTable.Add("mixed"));
            QnEmpty = new XmlQualifiedName(nameTable.Add("empty"));
            QnEltOnly = new XmlQualifiedName(nameTable.Add("eltOnly"));
            QnTextOnly = new XmlQualifiedName(nameTable.Add("textOnly"));
            QnOrder = new XmlQualifiedName(nameTable.Add("order"));
            QnSeq = new XmlQualifiedName(nameTable.Add("seq"));
            QnOne = new XmlQualifiedName(nameTable.Add("one"));
            QnMany = new XmlQualifiedName(nameTable.Add("many"));
            QnRequired = new XmlQualifiedName(nameTable.Add("required"));
            QnYes = new XmlQualifiedName(nameTable.Add("yes"));
            QnNo = new XmlQualifiedName(nameTable.Add("no"));
            QnString = new XmlQualifiedName(nameTable.Add("string"));
            QnID = new XmlQualifiedName(nameTable.Add("id"));
            QnIDRef = new XmlQualifiedName(nameTable.Add("idref"));
            QnIDRefs = new XmlQualifiedName(nameTable.Add("idrefs"));
            QnEntity = new XmlQualifiedName(nameTable.Add("entity"));
            QnEntities = new XmlQualifiedName(nameTable.Add("entities"));
            QnNmToken = new XmlQualifiedName(nameTable.Add("nmtoken"));
            QnNmTokens = new XmlQualifiedName(nameTable.Add("nmtokens"));
            QnEnumeration = new XmlQualifiedName(nameTable.Add("enumeration"));
            QnDefault = new XmlQualifiedName(nameTable.Add("default"));

            //For XSD Schema
            QnTargetNamespace = new XmlQualifiedName(nameTable.Add("targetNamespace"));
            QnVersion = new XmlQualifiedName(nameTable.Add("version"));
            QnFinalDefault = new XmlQualifiedName(nameTable.Add("finalDefault"));
            QnBlockDefault = new XmlQualifiedName(nameTable.Add("blockDefault"));
            QnFixed = new XmlQualifiedName(nameTable.Add("fixed"));
            QnAbstract = new XmlQualifiedName(nameTable.Add("abstract"));
            QnBlock = new XmlQualifiedName(nameTable.Add("block"));
            QnSubstitutionGroup = new XmlQualifiedName(nameTable.Add("substitutionGroup"));
            QnFinal = new XmlQualifiedName(nameTable.Add("final"));
            QnNillable = new XmlQualifiedName(nameTable.Add("nillable"));
            QnRef = new XmlQualifiedName(nameTable.Add("ref"));
            QnBase = new XmlQualifiedName(nameTable.Add("base"));
            QnDerivedBy = new XmlQualifiedName(nameTable.Add("derivedBy"));
            QnNamespace = new XmlQualifiedName(nameTable.Add("namespace"));
            QnProcessContents = new XmlQualifiedName(nameTable.Add("processContents"));
            QnRefer = new XmlQualifiedName(nameTable.Add("refer"));
            QnPublic = new XmlQualifiedName(nameTable.Add("public"));
            QnSystem = new XmlQualifiedName(nameTable.Add("system"));
            QnSchemaLocation = new XmlQualifiedName(nameTable.Add("schemaLocation"));
            QnValue = new XmlQualifiedName(nameTable.Add("value"));
            QnUse = new XmlQualifiedName(nameTable.Add("use"));
            QnForm = new XmlQualifiedName(nameTable.Add("form"));
            QnAttributeFormDefault = new XmlQualifiedName(nameTable.Add("attributeFormDefault"));
            QnElementFormDefault = new XmlQualifiedName(nameTable.Add("elementFormDefault"));
            QnSource = new XmlQualifiedName(nameTable.Add("source"));
            QnMemberTypes = new XmlQualifiedName(nameTable.Add("memberTypes"));
            QnItemType = new XmlQualifiedName(nameTable.Add("itemType"));
            QnXPath = new XmlQualifiedName(nameTable.Add("xpath"));

            // XDR namespace 
            QnXdrSchema = new XmlQualifiedName(XdrSchema, NsXdr);
            QnXdrElementType = new XmlQualifiedName(nameTable.Add("ElementType"), NsXdr);
            QnXdrElement = new XmlQualifiedName(nameTable.Add("element"), NsXdr);
            QnXdrGroup = new XmlQualifiedName(nameTable.Add("group"), NsXdr);
            QnXdrAttributeType = new XmlQualifiedName(nameTable.Add("AttributeType"), NsXdr);
            QnXdrAttribute = new XmlQualifiedName(nameTable.Add("attribute"), NsXdr);
            QnXdrDataType = new XmlQualifiedName(nameTable.Add("datatype"), NsXdr);
            QnXdrDescription = new XmlQualifiedName(nameTable.Add("description"), NsXdr);
            QnXdrExtends = new XmlQualifiedName(nameTable.Add("extends"), NsXdr);

            // XDR alias namespace
            QnXdrAliasSchema = new XmlQualifiedName(nameTable.Add("Schema"), NsDataTypeAlias);

            // DataType namespace
            QnDtType = new XmlQualifiedName(nameTable.Add("type"), NsDataType);
            QnDtValues = new XmlQualifiedName(nameTable.Add("values"), NsDataType);
            QnDtMaxLength = new XmlQualifiedName(nameTable.Add("maxLength"), NsDataType);
            QnDtMinLength = new XmlQualifiedName(nameTable.Add("minLength"), NsDataType);
            QnDtMax = new XmlQualifiedName(nameTable.Add("max"), NsDataType);
            QnDtMin = new XmlQualifiedName(nameTable.Add("min"), NsDataType);
            QnDtMinExclusive = new XmlQualifiedName(nameTable.Add("minExclusive"), NsDataType);
            QnDtMaxExclusive = new XmlQualifiedName(nameTable.Add("maxExclusive"), NsDataType);

            // XSD namespace
            QnXsdSchema = new XmlQualifiedName(XsdSchema, NsXs);
            QnXsdAnnotation = new XmlQualifiedName(nameTable.Add("annotation"), NsXs);
            QnXsdInclude = new XmlQualifiedName(nameTable.Add("include"), NsXs);
            QnXsdImport = new XmlQualifiedName(nameTable.Add("import"), NsXs);
            QnXsdElement = new XmlQualifiedName(nameTable.Add("element"), NsXs);
            QnXsdAttribute = new XmlQualifiedName(nameTable.Add("attribute"), NsXs);
            QnXsdAttributeGroup = new XmlQualifiedName(nameTable.Add("attributeGroup"), NsXs);
            QnXsdAnyAttribute = new XmlQualifiedName(nameTable.Add("anyAttribute"), NsXs);
            QnXsdGroup = new XmlQualifiedName(nameTable.Add("group"), NsXs);
            QnXsdAll = new XmlQualifiedName(nameTable.Add("all"), NsXs);
            QnXsdChoice = new XmlQualifiedName(nameTable.Add("choice"), NsXs);
            QnXsdSequence = new XmlQualifiedName(nameTable.Add("sequence"), NsXs);
            QnXsdAny = new XmlQualifiedName(nameTable.Add("any"), NsXs);
            QnXsdNotation = new XmlQualifiedName(nameTable.Add("notation"), NsXs);
            QnXsdSimpleType = new XmlQualifiedName(nameTable.Add("simpleType"), NsXs);
            QnXsdComplexType = new XmlQualifiedName(nameTable.Add("complexType"), NsXs);
            QnXsdUnique = new XmlQualifiedName(nameTable.Add("unique"), NsXs);
            QnXsdKey = new XmlQualifiedName(nameTable.Add("key"), NsXs);
            QnXsdKeyRef = new XmlQualifiedName(nameTable.Add("keyref"), NsXs);
            QnXsdSelector = new XmlQualifiedName(nameTable.Add("selector"), NsXs);
            QnXsdField = new XmlQualifiedName(nameTable.Add("field"), NsXs);
            QnXsdMinExclusive = new XmlQualifiedName(nameTable.Add("minExclusive"), NsXs);
            QnXsdMinInclusive = new XmlQualifiedName(nameTable.Add("minInclusive"), NsXs);
            QnXsdMaxInclusive = new XmlQualifiedName(nameTable.Add("maxInclusive"), NsXs);
            QnXsdMaxExclusive = new XmlQualifiedName(nameTable.Add("maxExclusive"), NsXs);
            QnXsdTotalDigits = new XmlQualifiedName(nameTable.Add("totalDigits"), NsXs);
            QnXsdFractionDigits = new XmlQualifiedName(nameTable.Add("fractionDigits"), NsXs);
            QnXsdLength = new XmlQualifiedName(nameTable.Add("length"), NsXs);
            QnXsdMinLength = new XmlQualifiedName(nameTable.Add("minLength"), NsXs);
            QnXsdMaxLength = new XmlQualifiedName(nameTable.Add("maxLength"), NsXs);
            QnXsdEnumeration = new XmlQualifiedName(nameTable.Add("enumeration"), NsXs);
            QnXsdPattern = new XmlQualifiedName(nameTable.Add("pattern"), NsXs);
            QnXsdDocumentation = new XmlQualifiedName(nameTable.Add("documentation"), NsXs);
            QnXsdAppinfo = new XmlQualifiedName(nameTable.Add("appinfo"), NsXs);
            QnXsdComplexContent = new XmlQualifiedName(nameTable.Add("complexContent"), NsXs);
            QnXsdSimpleContent = new XmlQualifiedName(nameTable.Add("simpleContent"), NsXs);
            QnXsdRestriction = new XmlQualifiedName(nameTable.Add("restriction"), NsXs);
            QnXsdExtension = new XmlQualifiedName(nameTable.Add("extension"), NsXs);
            QnXsdUnion = new XmlQualifiedName(nameTable.Add("union"), NsXs);
            QnXsdList = new XmlQualifiedName(nameTable.Add("list"), NsXs);
            QnXsdWhiteSpace = new XmlQualifiedName(nameTable.Add("whiteSpace"), NsXs);
            QnXsdRedefine = new XmlQualifiedName(nameTable.Add("redefine"), NsXs);
            QnXsdAnyType = new XmlQualifiedName(nameTable.Add("anyType"), NsXs);

            //Create token to Qname table
            CreateTokenToQNameTable();
        }

        public void CreateTokenToQNameTable()
        {
            TokenToQName[(int)Token.SchemaName] = QnName;
            TokenToQName[(int)Token.SchemaType] = QnType;
            TokenToQName[(int)Token.SchemaMaxOccurs] = QnMaxOccurs;
            TokenToQName[(int)Token.SchemaMinOccurs] = QnMinOccurs;
            TokenToQName[(int)Token.SchemaInfinite] = QnInfinite;
            TokenToQName[(int)Token.SchemaModel] = QnModel;
            TokenToQName[(int)Token.SchemaOpen] = QnOpen;
            TokenToQName[(int)Token.SchemaClosed] = QnClosed;
            TokenToQName[(int)Token.SchemaContent] = QnContent;
            TokenToQName[(int)Token.SchemaMixed] = QnMixed;
            TokenToQName[(int)Token.SchemaEmpty] = QnEmpty;
            TokenToQName[(int)Token.SchemaElementOnly] = QnEltOnly;
            TokenToQName[(int)Token.SchemaTextOnly] = QnTextOnly;
            TokenToQName[(int)Token.SchemaOrder] = QnOrder;
            TokenToQName[(int)Token.SchemaSeq] = QnSeq;
            TokenToQName[(int)Token.SchemaOne] = QnOne;
            TokenToQName[(int)Token.SchemaMany] = QnMany;
            TokenToQName[(int)Token.SchemaRequired] = QnRequired;
            TokenToQName[(int)Token.SchemaYes] = QnYes;
            TokenToQName[(int)Token.SchemaNo] = QnNo;
            TokenToQName[(int)Token.SchemaString] = QnString;
            TokenToQName[(int)Token.SchemaId] = QnID;
            TokenToQName[(int)Token.SchemaIdref] = QnIDRef;
            TokenToQName[(int)Token.SchemaIdrefs] = QnIDRefs;
            TokenToQName[(int)Token.SchemaEntity] = QnEntity;
            TokenToQName[(int)Token.SchemaEntities] = QnEntities;
            TokenToQName[(int)Token.SchemaNmtoken] = QnNmToken;
            TokenToQName[(int)Token.SchemaNmtokens] = QnNmTokens;
            TokenToQName[(int)Token.SchemaEnumeration] = QnEnumeration;
            TokenToQName[(int)Token.SchemaDefault] = QnDefault;
            TokenToQName[(int)Token.XdrRoot] = QnXdrSchema;
            TokenToQName[(int)Token.XdrElementType] = QnXdrElementType;
            TokenToQName[(int)Token.XdrElement] = QnXdrElement;
            TokenToQName[(int)Token.XdrGroup] = QnXdrGroup;
            TokenToQName[(int)Token.XdrAttributeType] = QnXdrAttributeType;
            TokenToQName[(int)Token.XdrAttribute] = QnXdrAttribute;
            TokenToQName[(int)Token.XdrDatatype] = QnXdrDataType;
            TokenToQName[(int)Token.XdrDescription] = QnXdrDescription;
            TokenToQName[(int)Token.XdrExtends] = QnXdrExtends;
            TokenToQName[(int)Token.SchemaXdrRootAlias] = QnXdrAliasSchema;
            TokenToQName[(int)Token.SchemaDtType] = QnDtType;
            TokenToQName[(int)Token.SchemaDtValues] = QnDtValues;
            TokenToQName[(int)Token.SchemaDtMaxLength] = QnDtMaxLength;
            TokenToQName[(int)Token.SchemaDtMinLength] = QnDtMinLength;
            TokenToQName[(int)Token.SchemaDtMax] = QnDtMax;
            TokenToQName[(int)Token.SchemaDtMin] = QnDtMin;
            TokenToQName[(int)Token.SchemaDtMinExclusive] = QnDtMinExclusive;
            TokenToQName[(int)Token.SchemaDtMaxExclusive] = QnDtMaxExclusive;
            TokenToQName[(int)Token.SchemaTargetNamespace] = QnTargetNamespace;
            TokenToQName[(int)Token.SchemaVersion] = QnVersion;
            TokenToQName[(int)Token.SchemaFinalDefault] = QnFinalDefault;
            TokenToQName[(int)Token.SchemaBlockDefault] = QnBlockDefault;
            TokenToQName[(int)Token.SchemaFixed] = QnFixed;
            TokenToQName[(int)Token.SchemaAbstract] = QnAbstract;
            TokenToQName[(int)Token.SchemaBlock] = QnBlock;
            TokenToQName[(int)Token.SchemaSubstitutionGroup] = QnSubstitutionGroup;
            TokenToQName[(int)Token.SchemaFinal] = QnFinal;
            TokenToQName[(int)Token.SchemaNillable] = QnNillable;
            TokenToQName[(int)Token.SchemaRef] = QnRef;
            TokenToQName[(int)Token.SchemaBase] = QnBase;
            TokenToQName[(int)Token.SchemaDerivedBy] = QnDerivedBy;
            TokenToQName[(int)Token.SchemaNamespace] = QnNamespace;
            TokenToQName[(int)Token.SchemaProcessContents] = QnProcessContents;
            TokenToQName[(int)Token.SchemaRefer] = QnRefer;
            TokenToQName[(int)Token.SchemaPublic] = QnPublic;
            TokenToQName[(int)Token.SchemaSystem] = QnSystem;
            TokenToQName[(int)Token.SchemaSchemaLocation] = QnSchemaLocation;
            TokenToQName[(int)Token.SchemaValue] = QnValue;
            TokenToQName[(int)Token.SchemaItemType] = QnItemType;
            TokenToQName[(int)Token.SchemaMemberTypes] = QnMemberTypes;
            TokenToQName[(int)Token.SchemaXPath] = QnXPath;
            TokenToQName[(int)Token.XsdSchema] = QnXsdSchema;
            TokenToQName[(int)Token.XsdAnnotation] = QnXsdAnnotation;
            TokenToQName[(int)Token.XsdInclude] = QnXsdInclude;
            TokenToQName[(int)Token.XsdImport] = QnXsdImport;
            TokenToQName[(int)Token.XsdElement] = QnXsdElement;
            TokenToQName[(int)Token.XsdAttribute] = QnXsdAttribute;
            TokenToQName[(int)Token.xsdAttributeGroup] = QnXsdAttributeGroup;
            TokenToQName[(int)Token.XsdAnyAttribute] = QnXsdAnyAttribute;
            TokenToQName[(int)Token.XsdGroup] = QnXsdGroup;
            TokenToQName[(int)Token.XsdAll] = QnXsdAll;
            TokenToQName[(int)Token.XsdChoice] = QnXsdChoice;
            TokenToQName[(int)Token.XsdSequence] = QnXsdSequence;
            TokenToQName[(int)Token.XsdAny] = QnXsdAny;
            TokenToQName[(int)Token.XsdNotation] = QnXsdNotation;
            TokenToQName[(int)Token.XsdSimpleType] = QnXsdSimpleType;
            TokenToQName[(int)Token.XsdComplexType] = QnXsdComplexType;
            TokenToQName[(int)Token.XsdUnique] = QnXsdUnique;
            TokenToQName[(int)Token.XsdKey] = QnXsdKey;
            TokenToQName[(int)Token.XsdKeyref] = QnXsdKeyRef;
            TokenToQName[(int)Token.XsdSelector] = QnXsdSelector;
            TokenToQName[(int)Token.XsdField] = QnXsdField;
            TokenToQName[(int)Token.XsdMinExclusive] = QnXsdMinExclusive;
            TokenToQName[(int)Token.XsdMinInclusive] = QnXsdMinInclusive;
            TokenToQName[(int)Token.XsdMaxExclusive] = QnXsdMaxExclusive;
            TokenToQName[(int)Token.XsdMaxInclusive] = QnXsdMaxInclusive;
            TokenToQName[(int)Token.XsdTotalDigits] = QnXsdTotalDigits;
            TokenToQName[(int)Token.XsdFractionDigits] = QnXsdFractionDigits;
            TokenToQName[(int)Token.XsdLength] = QnXsdLength;
            TokenToQName[(int)Token.XsdMinLength] = QnXsdMinLength;
            TokenToQName[(int)Token.XsdMaxLength] = QnXsdMaxLength;
            TokenToQName[(int)Token.XsdEnumeration] = QnXsdEnumeration;
            TokenToQName[(int)Token.XsdPattern] = QnXsdPattern;
            TokenToQName[(int)Token.XsdWhitespace] = QnXsdWhiteSpace;
            TokenToQName[(int)Token.XsdDocumentation] = QnXsdDocumentation;
            TokenToQName[(int)Token.XsdAppInfo] = QnXsdAppinfo;
            TokenToQName[(int)Token.XsdComplexContent] = QnXsdComplexContent;
            TokenToQName[(int)Token.XsdComplexContentRestriction] = QnXsdRestriction;
            TokenToQName[(int)Token.XsdSimpleContentRestriction] = QnXsdRestriction;
            TokenToQName[(int)Token.XsdSimpleTypeRestriction] = QnXsdRestriction;
            TokenToQName[(int)Token.XsdComplexContentExtension] = QnXsdExtension;
            TokenToQName[(int)Token.XsdSimpleContentExtension] = QnXsdExtension;
            TokenToQName[(int)Token.XsdSimpleContent] = QnXsdSimpleContent;
            TokenToQName[(int)Token.XsdSimpleTypeUnion] = QnXsdUnion;
            TokenToQName[(int)Token.XsdSimpleTypeList] = QnXsdList;
            TokenToQName[(int)Token.XsdRedefine] = QnXsdRedefine;
            TokenToQName[(int)Token.SchemaSource] = QnSource;
            TokenToQName[(int)Token.SchemaUse] = QnUse;
            TokenToQName[(int)Token.SchemaForm] = QnForm;
            TokenToQName[(int)Token.SchemaElementFormDefault] = QnElementFormDefault;
            TokenToQName[(int)Token.SchemaAttributeFormDefault] = QnAttributeFormDefault;
            TokenToQName[(int)Token.XmlLang] = QnXmlLang;
            TokenToQName[(int)Token.Empty] = XmlQualifiedName.Empty;
        }

        public SchemaType SchemaTypeFromRoot(string localName, string ns)
        {
            if (IsXSDRoot(localName, ns))
            {
                return SchemaType.XSD;
            }
            else if (IsXDRRoot(localName, XmlSchemaDatatype.XdrCanonizeUri(ns, _nameTable, this)))
            {
                return SchemaType.XDR;
            }
            else
            {
                return SchemaType.None;
            }
        }

        public bool IsXSDRoot(string localName, string ns)
        {
            return Ref.Equal(ns, NsXs) && Ref.Equal(localName, XsdSchema);
        }

        public bool IsXDRRoot(string localName, string ns)
        {
            return Ref.Equal(ns, NsXdr) && Ref.Equal(localName, XdrSchema);
        }

        public enum Token
        {
            Empty,
            SchemaName,
            SchemaType,
            SchemaMaxOccurs,
            SchemaMinOccurs,
            SchemaInfinite,
            SchemaModel,
            SchemaOpen,
            SchemaClosed,
            SchemaContent,
            SchemaMixed,
            SchemaEmpty,
            SchemaElementOnly,
            SchemaTextOnly,
            SchemaOrder,
            SchemaSeq,
            SchemaOne,
            SchemaMany,
            SchemaRequired,
            SchemaYes,
            SchemaNo,
            SchemaString,
            SchemaId,
            SchemaIdref,
            SchemaIdrefs,
            SchemaEntity,
            SchemaEntities,
            SchemaNmtoken,
            SchemaNmtokens,
            SchemaEnumeration,
            SchemaDefault,
            XdrRoot,
            XdrElementType,
            XdrElement,
            XdrGroup,
            XdrAttributeType,
            XdrAttribute,
            XdrDatatype,
            XdrDescription,
            XdrExtends,
            SchemaXdrRootAlias,
            SchemaDtType,
            SchemaDtValues,
            SchemaDtMaxLength,
            SchemaDtMinLength,
            SchemaDtMax,
            SchemaDtMin,
            SchemaDtMinExclusive,
            SchemaDtMaxExclusive,
            SchemaTargetNamespace,
            SchemaVersion,
            SchemaFinalDefault,
            SchemaBlockDefault,
            SchemaFixed,
            SchemaAbstract,
            SchemaBlock,
            SchemaSubstitutionGroup,
            SchemaFinal,
            SchemaNillable,
            SchemaRef,
            SchemaBase,
            SchemaDerivedBy,
            SchemaNamespace,
            SchemaProcessContents,
            SchemaRefer,
            SchemaPublic,
            SchemaSystem,
            SchemaSchemaLocation,
            SchemaValue,
            SchemaSource,
            SchemaAttributeFormDefault,
            SchemaElementFormDefault,
            SchemaUse,
            SchemaForm,
            XsdSchema,
            XsdAnnotation,
            XsdInclude,
            XsdImport,
            XsdElement,
            XsdAttribute,
            xsdAttributeGroup,
            XsdAnyAttribute,
            XsdGroup,
            XsdAll,
            XsdChoice,
            XsdSequence,
            XsdAny,
            XsdNotation,
            XsdSimpleType,
            XsdComplexType,
            XsdUnique,
            XsdKey,
            XsdKeyref,
            XsdSelector,
            XsdField,
            XsdMinExclusive,
            XsdMinInclusive,
            XsdMaxExclusive,
            XsdMaxInclusive,
            XsdTotalDigits,
            XsdFractionDigits,
            XsdLength,
            XsdMinLength,
            XsdMaxLength,
            XsdEnumeration,
            XsdPattern,
            XsdDocumentation,
            XsdAppInfo,
            XsdComplexContent,
            XsdComplexContentExtension,
            XsdComplexContentRestriction,
            XsdSimpleContent,
            XsdSimpleContentExtension,
            XsdSimpleContentRestriction,
            XsdSimpleTypeList,
            XsdSimpleTypeRestriction,
            XsdSimpleTypeUnion,
            XsdWhitespace,
            XsdRedefine,
            SchemaItemType,
            SchemaMemberTypes,
            SchemaXPath,
            XmlLang
        };
    };
}
