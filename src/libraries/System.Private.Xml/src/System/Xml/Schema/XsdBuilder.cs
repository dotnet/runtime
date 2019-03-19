// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Collections.Generic;


    internal sealed class XsdBuilder : SchemaBuilder
    {
        private enum State
        {
            Root,
            Schema,
            Annotation,
            Include,
            Import,
            Element,
            Attribute,
            AttributeGroup,
            AttributeGroupRef,
            AnyAttribute,
            Group,
            GroupRef,
            All,
            Choice,
            Sequence,
            Any,
            Notation,
            SimpleType,
            ComplexType,
            ComplexContent,
            ComplexContentRestriction,
            ComplexContentExtension,
            SimpleContent,
            SimpleContentExtension,
            SimpleContentRestriction,
            SimpleTypeUnion,
            SimpleTypeList,
            SimpleTypeRestriction,
            Unique,
            Key,
            KeyRef,
            Selector,
            Field,
            MinExclusive,
            MinInclusive,
            MaxExclusive,
            MaxInclusive,
            TotalDigits,
            FractionDigits,
            Length,
            MinLength,
            MaxLength,
            Enumeration,
            Pattern,
            WhiteSpace,
            AppInfo,
            Documentation,
            Redefine,
        }
        private const int STACK_INCREMENT = 10;

        private delegate void XsdBuildFunction(XsdBuilder builder, string value);
        private delegate void XsdInitFunction(XsdBuilder builder, string value);
        private delegate void XsdEndChildFunction(XsdBuilder builder);

        private sealed class XsdAttributeEntry
        {
            public SchemaNames.Token Attribute;               // possible attribute names
            public XsdBuildFunction BuildFunc;  // Corresponding build functions for attribute value

            public XsdAttributeEntry(SchemaNames.Token a, XsdBuildFunction build)
            {
                Attribute = a;
                BuildFunc = build;
            }
        };

        //
        // XsdEntry controls the states of parsing a schema document
        // and calls the corresponding "init", "end" and "build" functions when necessary
        //
        private sealed class XsdEntry
        {
            public SchemaNames.Token Name;                  // the name of the object it is comparing to
            public State CurrentState;
            public State[] NextStates;                   // possible next states
            public XsdAttributeEntry[] Attributes;       // allowed attributes
            public XsdInitFunction InitFunc;             // "init" functions in XsdBuilder
            public XsdEndChildFunction EndChildFunc;     // "end" functions in XsdBuilder for EndChildren
            public bool ParseContent;                       // whether text content is allowed  

            public XsdEntry(SchemaNames.Token n,
                            State state,
                            State[] nextStates,
                            XsdAttributeEntry[] attributes,
                            XsdInitFunction init,
                            XsdEndChildFunction end,
                            bool parseContent)
            {
                Name = n;
                CurrentState = state;
                NextStates = nextStates;
                Attributes = attributes;
                InitFunc = init;
                EndChildFunc = end;
                ParseContent = parseContent;
            }
        };


        //required for Parsing QName
        private class BuilderNamespaceManager : XmlNamespaceManager
        {
            private XmlNamespaceManager _nsMgr;
            private XmlReader _reader;

            public BuilderNamespaceManager(XmlNamespaceManager nsMgr, XmlReader reader)
            {
                _nsMgr = nsMgr;
                _reader = reader;
            }

            public override string LookupNamespace(string prefix)
            {
                string ns = _nsMgr.LookupNamespace(prefix);
                if (ns == null)
                {
                    ns = _reader.LookupNamespace(prefix);
                }
                return ns;
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        // Data structures for XSD Schema, Sept 2000 version
        //

        //
        //Elements
        //
        private static readonly State[] s_schemaElement = {
            State.Schema};
        private static readonly State[] s_schemaSubelements = {
            State.Annotation, State.Include, State.Import, State.Redefine,
            State.ComplexType, State.SimpleType, State.Element, State.Attribute,
            State.AttributeGroup, State.Group, State.Notation};
        private static readonly State[] s_attributeSubelements = {
            State.Annotation, State.SimpleType};
        private static readonly State[] s_elementSubelements = {
            State.Annotation, State.SimpleType, State.ComplexType,
            State.Unique, State.Key, State.KeyRef};
        private static readonly State[] s_complexTypeSubelements = {
            State.Annotation, State.SimpleContent, State.ComplexContent,
            State.GroupRef, State.All, State.Choice, State.Sequence,
            State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_simpleContentSubelements = {
            State.Annotation, State.SimpleContentRestriction, State.SimpleContentExtension };
        private static readonly State[] s_simpleContentExtensionSubelements = {
            State.Annotation, State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_simpleContentRestrictionSubelements = {
            State.Annotation, State.SimpleType,
            State.Enumeration, State.Length, State.MaxExclusive, State.MaxInclusive, State.MaxLength, State.MinExclusive,
            State.MinInclusive, State.MinLength, State.Pattern, State.TotalDigits, State.FractionDigits, State.WhiteSpace,
            State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_complexContentSubelements = {
            State.Annotation, State.ComplexContentRestriction, State.ComplexContentExtension };
        private static readonly State[] s_complexContentExtensionSubelements = {
            State.Annotation, State.GroupRef, State.All, State.Choice, State.Sequence,
            State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_complexContentRestrictionSubelements = {
            State.Annotation, State.GroupRef, State.All, State.Choice, State.Sequence,
            State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_simpleTypeSubelements = {
            State.Annotation, State.SimpleTypeList, State.SimpleTypeRestriction, State.SimpleTypeUnion};
        private static readonly State[] s_simpleTypeRestrictionSubelements = {
            State.Annotation, State.SimpleType,
            State.Enumeration, State.Length, State.MaxExclusive, State.MaxInclusive, State.MaxLength, State.MinExclusive,
            State.MinInclusive, State.MinLength, State.Pattern, State.TotalDigits, State.FractionDigits, State.WhiteSpace};
        private static readonly State[] s_simpleTypeListSubelements = {
            State.Annotation, State.SimpleType};
        private static readonly State[] s_simpleTypeUnionSubelements = {
            State.Annotation, State.SimpleType};
        private static readonly State[] s_redefineSubelements = {
            State.Annotation, State.AttributeGroup, State.ComplexType, State.Group, State.SimpleType };
        private static readonly State[] s_attributeGroupSubelements = {
            State.Annotation, State.Attribute, State.AttributeGroupRef, State.AnyAttribute};
        private static readonly State[] s_groupSubelements = {
            State.Annotation, State.All, State.Choice, State.Sequence};
        private static readonly State[] s_allSubelements = {
            State.Annotation, State.Element};
        private static readonly State[] s_choiceSequenceSubelements = {
            State.Annotation, State.Element, State.GroupRef, State.Choice, State.Sequence, State.Any};
        private static readonly State[] s_identityConstraintSubelements = {
            State.Annotation, State.Selector, State.Field};
        private static readonly State[] s_annotationSubelements = {
            State.AppInfo, State.Documentation};
        private static readonly State[] s_annotatedSubelements = {
            State.Annotation};


        //
        //Attributes
        //
        private static readonly XsdAttributeEntry[] s_schemaAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaAttributeFormDefault,    new XsdBuildFunction(BuildSchema_AttributeFormDefault) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaElementFormDefault,      new XsdBuildFunction(BuildSchema_ElementFormDefault) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaTargetNamespace,         new XsdBuildFunction(BuildSchema_TargetNamespace) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaVersion,                 new XsdBuildFunction(BuildSchema_Version) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFinalDefault,            new XsdBuildFunction(BuildSchema_FinalDefault) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaBlockDefault,            new XsdBuildFunction(BuildSchema_BlockDefault) )
        };

        private static readonly XsdAttributeEntry[] s_attributeAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaDefault,                 new XsdBuildFunction(BuildAttribute_Default) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFixed,                   new XsdBuildFunction(BuildAttribute_Fixed) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaForm,                    new XsdBuildFunction(BuildAttribute_Form) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildAttribute_Name) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaRef,                     new XsdBuildFunction(BuildAttribute_Ref) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaType,                    new XsdBuildFunction(BuildAttribute_Type) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaUse,                     new XsdBuildFunction(BuildAttribute_Use) )
        };

        private static readonly XsdAttributeEntry[] s_elementAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaAbstract,                new XsdBuildFunction(BuildElement_Abstract) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaBlock,                   new XsdBuildFunction(BuildElement_Block) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaDefault,                 new XsdBuildFunction(BuildElement_Default) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFinal,                   new XsdBuildFunction(BuildElement_Final) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFixed,                   new XsdBuildFunction(BuildElement_Fixed) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaForm,                    new XsdBuildFunction(BuildElement_Form) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs,               new XsdBuildFunction(BuildElement_MaxOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs,               new XsdBuildFunction(BuildElement_MinOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildElement_Name) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaNillable,                new XsdBuildFunction(BuildElement_Nillable) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaRef,                     new XsdBuildFunction(BuildElement_Ref) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaSubstitutionGroup,       new XsdBuildFunction(BuildElement_SubstitutionGroup) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaType,                    new XsdBuildFunction(BuildElement_Type) )
        };

        private static readonly XsdAttributeEntry[] s_complexTypeAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaAbstract,                new XsdBuildFunction(BuildComplexType_Abstract) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaBlock,                   new XsdBuildFunction(BuildComplexType_Block) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFinal,                   new XsdBuildFunction(BuildComplexType_Final) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMixed,                   new XsdBuildFunction(BuildComplexType_Mixed) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildComplexType_Name) )
        };

        private static readonly XsdAttributeEntry[] s_simpleContentAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };

        private static readonly XsdAttributeEntry[] s_simpleContentExtensionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaBase,                    new XsdBuildFunction(BuildSimpleContentExtension_Base) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) )
        };

        private static readonly XsdAttributeEntry[] s_simpleContentRestrictionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaBase,                    new XsdBuildFunction(BuildSimpleContentRestriction_Base) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };

        private static readonly XsdAttributeEntry[] s_complexContentAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMixed,                   new XsdBuildFunction(BuildComplexContent_Mixed) ),
        };

        private static readonly XsdAttributeEntry[] s_complexContentExtensionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaBase,                    new XsdBuildFunction(BuildComplexContentExtension_Base) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };

        private static readonly XsdAttributeEntry[] s_complexContentRestrictionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaBase,                    new XsdBuildFunction(BuildComplexContentRestriction_Base) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };

        private static readonly XsdAttributeEntry[] s_simpleTypeAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFinal,                   new XsdBuildFunction(BuildSimpleType_Final) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildSimpleType_Name) )
        };

        private static readonly XsdAttributeEntry[] s_simpleTypeRestrictionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaBase,                    new XsdBuildFunction(BuildSimpleTypeRestriction_Base) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };

        private static readonly XsdAttributeEntry[] s_simpleTypeUnionAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMemberTypes,             new XsdBuildFunction(BuildSimpleTypeUnion_MemberTypes) ),
        };

        private static readonly XsdAttributeEntry[] s_simpleTypeListAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaItemType,                new XsdBuildFunction(BuildSimpleTypeList_ItemType) ),
        };

        private static readonly XsdAttributeEntry[] s_attributeGroupAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildAttributeGroup_Name) ),
        };

        private static readonly XsdAttributeEntry[] s_attributeGroupRefAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaRef,                     new XsdBuildFunction(BuildAttributeGroupRef_Ref) )
        };

        private static readonly XsdAttributeEntry[] s_groupAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildGroup_Name) ),
        };

        private static readonly XsdAttributeEntry[] s_groupRefAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs,               new XsdBuildFunction(BuildParticle_MaxOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs,               new XsdBuildFunction(BuildParticle_MinOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaRef,                     new XsdBuildFunction(BuildGroupRef_Ref) )
        };

        private static readonly XsdAttributeEntry[] s_particleAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs,               new XsdBuildFunction(BuildParticle_MaxOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs,               new XsdBuildFunction(BuildParticle_MinOccurs) ),
        };


        private static readonly XsdAttributeEntry[] s_anyAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs,               new XsdBuildFunction(BuildParticle_MaxOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs,               new XsdBuildFunction(BuildParticle_MinOccurs) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace,               new XsdBuildFunction(BuildAny_Namespace) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaProcessContents,         new XsdBuildFunction(BuildAny_ProcessContents) )
        };

        private static readonly XsdAttributeEntry[] s_identityConstraintAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildIdentityConstraint_Name) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaRefer,                   new XsdBuildFunction(BuildIdentityConstraint_Refer) )
        };

        private static readonly XsdAttributeEntry[] s_selectorAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaXPath,                   new XsdBuildFunction(BuildSelector_XPath) )
        };

        private static readonly XsdAttributeEntry[] s_fieldAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaXPath,                   new XsdBuildFunction(BuildField_XPath) )
        };

        private static readonly XsdAttributeEntry[] s_notationAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaName,                    new XsdBuildFunction(BuildNotation_Name) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaPublic,                  new XsdBuildFunction(BuildNotation_Public) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaSystem,                  new XsdBuildFunction(BuildNotation_System) )
        };

        private static readonly XsdAttributeEntry[] s_includeAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation,          new XsdBuildFunction(BuildInclude_SchemaLocation) )
        };

        private static readonly XsdAttributeEntry[] s_importAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace,               new XsdBuildFunction(BuildImport_Namespace) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation,          new XsdBuildFunction(BuildImport_SchemaLocation) )
        };

        private static readonly XsdAttributeEntry[] s_facetAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaFixed,                   new XsdBuildFunction(BuildFacet_Fixed) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaValue,                   new XsdBuildFunction(BuildFacet_Value) )
        };

        private static readonly XsdAttributeEntry[] s_anyAttributeAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace,               new XsdBuildFunction(BuildAnyAttribute_Namespace) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaProcessContents,         new XsdBuildFunction(BuildAnyAttribute_ProcessContents) )
        };

        private static readonly XsdAttributeEntry[] s_documentationAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaSource,                  new XsdBuildFunction(BuildDocumentation_Source) ),
            new XsdAttributeEntry(SchemaNames.Token.XmlLang,                       new XsdBuildFunction(BuildDocumentation_XmlLang) )
        };

        private static readonly XsdAttributeEntry[] s_appinfoAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaSource,                  new XsdBuildFunction(BuildAppinfo_Source) )
        };

        private static readonly XsdAttributeEntry[] s_redefineAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
            new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation,          new XsdBuildFunction(BuildRedefine_SchemaLocation) )
        };

        private static readonly XsdAttributeEntry[] s_annotationAttributes = {
            new XsdAttributeEntry(SchemaNames.Token.SchemaId,                      new XsdBuildFunction(BuildAnnotated_Id) ),
        };
        //
        // XSD Schema entries
        //                        

        private static readonly XsdEntry[] s_schemaEntries = {
       /* Root */                       new XsdEntry( SchemaNames.Token.Empty, State.Root, s_schemaElement, null,
                                                      null,
                                                      null,
                                                      true),
       /* Schema */                     new XsdEntry( SchemaNames.Token.XsdSchema, State.Schema,     s_schemaSubelements, s_schemaAttributes,
                                                      new XsdInitFunction(InitSchema),
                                                      null,
                                                      true),
       /* Annotation */                 new XsdEntry( SchemaNames.Token.XsdAnnotation, State.Annotation,     s_annotationSubelements, s_annotationAttributes,
                                                      new XsdInitFunction(InitAnnotation),
                                                      null,
                                                      true),
       /* Include */                    new XsdEntry( SchemaNames.Token.XsdInclude, State.Include,    s_annotatedSubelements, s_includeAttributes,
                                                      new XsdInitFunction(InitInclude),
                                                      null,
                                                      true),
       /* Import */                     new XsdEntry( SchemaNames.Token.XsdImport, State.Import,     s_annotatedSubelements, s_importAttributes,
                                                      new XsdInitFunction(InitImport),
                                                      null,
                                                      true),
       /* Element */                    new XsdEntry( SchemaNames.Token.XsdElement, State.Element,     s_elementSubelements, s_elementAttributes,
                                                      new XsdInitFunction(InitElement),
                                                      null,
                                                      true),
       /* Attribute */                  new XsdEntry( SchemaNames.Token.XsdAttribute, State.Attribute,     s_attributeSubelements, s_attributeAttributes,
                                                      new XsdInitFunction(InitAttribute),
                                                      null,
                                                      true),
       /* AttributeGroup */             new XsdEntry( SchemaNames.Token.xsdAttributeGroup, State.AttributeGroup,     s_attributeGroupSubelements, s_attributeGroupAttributes,
                                                      new XsdInitFunction(InitAttributeGroup),
                                                      null,
                                                      true),
       /* AttributeGroupRef */          new XsdEntry( SchemaNames.Token.xsdAttributeGroup, State.AttributeGroupRef,  s_annotatedSubelements, s_attributeGroupRefAttributes,
                                                      new XsdInitFunction(InitAttributeGroupRef),
                                                      null,
                                                      true),
       /* AnyAttribute */               new XsdEntry( SchemaNames.Token.XsdAnyAttribute, State.AnyAttribute,     s_annotatedSubelements, s_anyAttributeAttributes,
                                                      new XsdInitFunction(InitAnyAttribute),
                                                      null,
                                                      true),
       /* Group */                      new XsdEntry( SchemaNames.Token.XsdGroup, State.Group,     s_groupSubelements, s_groupAttributes,
                                                      new XsdInitFunction(InitGroup),
                                                      null,
                                                      true),
       /* GroupRef */                   new XsdEntry( SchemaNames.Token.XsdGroup, State.GroupRef,     s_annotatedSubelements, s_groupRefAttributes,
                                                      new XsdInitFunction(InitGroupRef),
                                                      null,
                                                      true),
       /* All */                        new XsdEntry( SchemaNames.Token.XsdAll, State.All,     s_allSubelements, s_particleAttributes,
                                                      new XsdInitFunction(InitAll),
                                                      null,
                                                      true),
       /* Choice */                     new XsdEntry( SchemaNames.Token.XsdChoice, State.Choice,     s_choiceSequenceSubelements, s_particleAttributes,
                                                      new XsdInitFunction(InitChoice),
                                                      null,
                                                      true),
       /* Sequence */                   new XsdEntry( SchemaNames.Token.XsdSequence, State.Sequence,     s_choiceSequenceSubelements, s_particleAttributes,
                                                      new XsdInitFunction(InitSequence),
                                                      null,
                                                      true),
       /* Any */                        new XsdEntry( SchemaNames.Token.XsdAny, State.Any,     s_annotatedSubelements, s_anyAttributes,
                                                      new XsdInitFunction(InitAny),
                                                      null,
                                                      true),
       /* Notation */                   new XsdEntry( SchemaNames.Token.XsdNotation, State.Notation,     s_annotatedSubelements, s_notationAttributes,
                                                      new XsdInitFunction(InitNotation),
                                                      null,
                                                      true),
       /* SimpleType */                 new XsdEntry( SchemaNames.Token.XsdSimpleType, State.SimpleType,     s_simpleTypeSubelements, s_simpleTypeAttributes,
                                                      new XsdInitFunction(InitSimpleType),
                                                      null,
                                                      true),
       /* ComplexType */                new XsdEntry( SchemaNames.Token.XsdComplexType, State.ComplexType,     s_complexTypeSubelements, s_complexTypeAttributes,
                                                      new XsdInitFunction(InitComplexType),
                                                      null,
                                                      true),
       /* ComplexContent */             new XsdEntry( SchemaNames.Token.XsdComplexContent, State.ComplexContent,  s_complexContentSubelements, s_complexContentAttributes,
                                                      new XsdInitFunction(InitComplexContent),
                                                      null,
                                                      true),
       /* ComplexContentRestriction */    new XsdEntry( SchemaNames.Token.XsdComplexContentRestriction, State.ComplexContentRestriction,  s_complexContentRestrictionSubelements, s_complexContentRestrictionAttributes,
                                                      new XsdInitFunction(InitComplexContentRestriction),
                                                      null,
                                                      true),
       /* ComplexContentExtension */  new XsdEntry( SchemaNames.Token.XsdComplexContentExtension, State.ComplexContentExtension,  s_complexContentExtensionSubelements, s_complexContentExtensionAttributes,
                                                      new XsdInitFunction(InitComplexContentExtension),
                                                      null,
                                                      true),
       /* SimpleContent */              new XsdEntry( SchemaNames.Token.XsdSimpleContent, State.SimpleContent,  s_simpleContentSubelements, s_simpleContentAttributes,
                                                      new XsdInitFunction(InitSimpleContent),
                                                      null,
                                                      true),
       /* SimpleContentExtension */     new XsdEntry( SchemaNames.Token.XsdSimpleContentExtension, State.SimpleContentExtension,  s_simpleContentExtensionSubelements, s_simpleContentExtensionAttributes,
                                                      new XsdInitFunction(InitSimpleContentExtension),
                                                      null,
                                                      true),
       /* SimpleContentRestriction */   new XsdEntry( SchemaNames.Token.XsdSimpleContentRestriction, State.SimpleContentRestriction,  s_simpleContentRestrictionSubelements, s_simpleContentRestrictionAttributes,
                                                      new XsdInitFunction(InitSimpleContentRestriction),
                                                      null,
                                                      true),
       /* SimpleTypeUnion */            new XsdEntry( SchemaNames.Token.XsdSimpleTypeUnion, State.SimpleTypeUnion,    s_simpleTypeUnionSubelements, s_simpleTypeUnionAttributes,
                                                      new XsdInitFunction(InitSimpleTypeUnion),
                                                      null,
                                                      true),
       /* SimpleTypeList */             new XsdEntry( SchemaNames.Token.XsdSimpleTypeList, State.SimpleTypeList,     s_simpleTypeListSubelements, s_simpleTypeListAttributes,
                                                      new XsdInitFunction(InitSimpleTypeList),
                                                      null,
                                                      true),
       /* SimpleTypeRestriction */      new XsdEntry( SchemaNames.Token.XsdSimpleTypeRestriction, State.SimpleTypeRestriction,  s_simpleTypeRestrictionSubelements, s_simpleTypeRestrictionAttributes,
                                                      new XsdInitFunction(InitSimpleTypeRestriction),
                                                      null,
                                                      true),
       /* Unique */                     new XsdEntry( SchemaNames.Token.XsdUnique,  State.Unique,    s_identityConstraintSubelements, s_identityConstraintAttributes,
                                                      new XsdInitFunction(InitIdentityConstraint),
                                                      null,
                                                      true),
       /* Key */                        new XsdEntry( SchemaNames.Token.XsdKey, State.Key,        s_identityConstraintSubelements, s_identityConstraintAttributes,
                                                      new XsdInitFunction(InitIdentityConstraint),
                                                      null,
                                                      true),
       /* KeyRef */                     new XsdEntry( SchemaNames.Token.XsdKeyref, State.KeyRef,     s_identityConstraintSubelements, s_identityConstraintAttributes,
                                                      new XsdInitFunction(InitIdentityConstraint),
                                                      null,
                                                      true),
       /* Selector */                   new XsdEntry( SchemaNames.Token.XsdSelector, State.Selector,     s_annotatedSubelements, s_selectorAttributes,
                                                      new XsdInitFunction(InitSelector),
                                                      null,
                                                      true),
       /* Field */                      new XsdEntry( SchemaNames.Token.XsdField, State.Field,     s_annotatedSubelements, s_fieldAttributes,
                                                      new XsdInitFunction(InitField),
                                                      null,
                                                      true),
       /* MinExclusive */               new XsdEntry( SchemaNames.Token.XsdMinExclusive, State.MinExclusive,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* MinInclusive */               new XsdEntry( SchemaNames.Token.XsdMinInclusive, State.MinInclusive,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* MaxExclusive */               new XsdEntry( SchemaNames.Token.XsdMaxExclusive, State.MaxExclusive,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* MaxInclusive */               new XsdEntry( SchemaNames.Token.XsdMaxInclusive, State.MaxInclusive,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* TotalDigits */                new XsdEntry( SchemaNames.Token.XsdTotalDigits, State.TotalDigits,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* FractionDigits */             new XsdEntry( SchemaNames.Token.XsdFractionDigits, State.FractionDigits,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* Length */                     new XsdEntry( SchemaNames.Token.XsdLength, State.Length,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* MinLength */                  new XsdEntry( SchemaNames.Token.XsdMinLength, State.MinLength,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* MaxLength */                  new XsdEntry( SchemaNames.Token.XsdMaxLength, State.MaxLength,     s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* Enumeration */                new XsdEntry( SchemaNames.Token.XsdEnumeration, State.Enumeration,    s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* Pattern */                    new XsdEntry( SchemaNames.Token.XsdPattern, State.Pattern,    s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* WhiteSpace */                 new XsdEntry( SchemaNames.Token.XsdWhitespace, State.WhiteSpace, s_annotatedSubelements, s_facetAttributes,
                                                      new XsdInitFunction(InitFacet),
                                                      null,
                                                      true),
       /* AppInfo */                    new XsdEntry( SchemaNames.Token.XsdAppInfo, State.AppInfo,    null, s_appinfoAttributes,
                                                      new XsdInitFunction(InitAppinfo),
                                                      new XsdEndChildFunction(EndAppinfo),
                                                      false),
       /* Documentation */              new XsdEntry( SchemaNames.Token.XsdDocumentation, State.Documentation,    null, s_documentationAttributes,
                                                      new XsdInitFunction(InitDocumentation),
                                                      new XsdEndChildFunction(EndDocumentation),
                                                      false),
       /* Redefine */                   new XsdEntry( SchemaNames.Token.XsdRedefine, State.Redefine,    s_redefineSubelements, s_redefineAttributes,
                                                      new XsdInitFunction(InitRedefine),
                                                      new XsdEndChildFunction(EndRedefine),
                                                      true)
        };

        //
        // for 'block' and 'final' attribute values
        //
        private static readonly int[] s_derivationMethodValues = {
            (int)XmlSchemaDerivationMethod.Substitution,
            (int)XmlSchemaDerivationMethod.Extension,
            (int)XmlSchemaDerivationMethod.Restriction,
            (int)XmlSchemaDerivationMethod.List,
            (int)XmlSchemaDerivationMethod.Union,
            (int)XmlSchemaDerivationMethod.All,
        };
        private static readonly string[] s_derivationMethodStrings = {
            "substitution",
            "extension",
            "restriction",
            "list",
            "union",
            "#all",
        };

        private static readonly string[] s_formStringValues = { "qualified", "unqualified" };
        private static readonly string[] s_useStringValues = { "optional", "prohibited", "required" };
        private static readonly string[] s_processContentsStringValues = { "skip", "lax", "strict" };

        private XmlReader _reader;
        private PositionInfo _positionInfo;
        private XsdEntry _currentEntry;
        private XsdEntry _nextEntry;
        private bool _hasChild;
        private HWStack _stateHistory = new HWStack(STACK_INCREMENT);
        private Stack _containerStack = new Stack();
        private XmlNameTable _nameTable;
        private SchemaNames _schemaNames;
        private XmlNamespaceManager _namespaceManager;
        private bool _canIncludeImport;

        private XmlSchema _schema;
        private XmlSchemaObject _xso;
        private XmlSchemaElement _element;
        private XmlSchemaAny _anyElement;
        private XmlSchemaAttribute _attribute;
        private XmlSchemaAnyAttribute _anyAttribute;
        private XmlSchemaComplexType _complexType;
        private XmlSchemaSimpleType _simpleType;
        private XmlSchemaComplexContent _complexContent;
        private XmlSchemaComplexContentExtension _complexContentExtension;
        private XmlSchemaComplexContentRestriction _complexContentRestriction;
        private XmlSchemaSimpleContent _simpleContent;
        private XmlSchemaSimpleContentExtension _simpleContentExtension;
        private XmlSchemaSimpleContentRestriction _simpleContentRestriction;
        private XmlSchemaSimpleTypeUnion _simpleTypeUnion;
        private XmlSchemaSimpleTypeList _simpleTypeList;
        private XmlSchemaSimpleTypeRestriction _simpleTypeRestriction;
        private XmlSchemaGroup _group;
        private XmlSchemaGroupRef _groupRef;
        private XmlSchemaAll _all;
        private XmlSchemaChoice _choice;
        private XmlSchemaSequence _sequence;
        private XmlSchemaParticle _particle;
        private XmlSchemaAttributeGroup _attributeGroup;
        private XmlSchemaAttributeGroupRef _attributeGroupRef;
        private XmlSchemaNotation _notation;
        private XmlSchemaIdentityConstraint _identityConstraint;
        private XmlSchemaXPath _xpath;
        private XmlSchemaInclude _include;
        private XmlSchemaImport _import;
        private XmlSchemaAnnotation _annotation;
        private XmlSchemaAppInfo _appInfo;
        private XmlSchemaDocumentation _documentation;
        private XmlSchemaFacet _facet;
        private XmlNode[] _markup;
        private XmlSchemaRedefine _redefine;

        private ValidationEventHandler _validationEventHandler;
        private ArrayList _unhandledAttributes = new ArrayList();
        private Dictionary<string, string> _namespaces;

        internal XsdBuilder(
                           XmlReader reader,
                           XmlNamespaceManager curmgr,
                           XmlSchema schema,
                           XmlNameTable nameTable,
                           SchemaNames schemaNames,
                           ValidationEventHandler eventhandler
                           )
        {
            _reader = reader;
            _xso = _schema = schema;
            _namespaceManager = new BuilderNamespaceManager(curmgr, reader);
            _validationEventHandler = eventhandler;
            _nameTable = nameTable;
            _schemaNames = schemaNames;
            _stateHistory = new HWStack(STACK_INCREMENT);
            _currentEntry = s_schemaEntries[0];
            _positionInfo = PositionInfo.GetPositionInfo(reader);
        }

        internal override bool ProcessElement(string prefix, string name, string ns)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, ns);
            if (GetNextState(qname))
            {
                Push();
                Debug.Assert(_currentEntry.InitFunc != null);
                _xso = null;
                _currentEntry.InitFunc(this, null);
                Debug.Assert(_xso != null);
                RecordPosition();
            }
            else
            {
                if (!IsSkipableElement(qname))
                {
                    SendValidationEvent(SR.Sch_UnsupportedElement, qname.ToString());
                }
                return false;
            }
            return true;
        }

        internal override void ProcessAttribute(string prefix, string name, string ns, string value)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, ns);
            if (_currentEntry.Attributes != null)
            {
                for (int i = 0; i < _currentEntry.Attributes.Length; i++)
                {
                    XsdAttributeEntry a = _currentEntry.Attributes[i];
                    if (_schemaNames.TokenToQName[(int)a.Attribute].Equals(qname))
                    {
                        try
                        {
                            a.BuildFunc(this, value);
                        }
                        catch (XmlSchemaException e)
                        {
                            e.SetSource(_reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
                            SendValidationEvent(SR.Sch_InvalidXsdAttributeDatatypeValue, new string[] { name, e.Message }, XmlSeverityType.Error);
                        }
                        return;
                    }
                }
            }

            // Check non-supported attribute
            if ((ns != _schemaNames.NsXs) && (ns.Length != 0))
            {
                if (ns == _schemaNames.NsXmlNs)
                {
                    if (_namespaces == null)
                    {
                        _namespaces = new Dictionary<string, string>();
                    }
                    _namespaces.Add((name == _schemaNames.QnXmlNs.Name) ? string.Empty : name, value);
                }
                else
                {
                    XmlAttribute attribute = new XmlAttribute(prefix, name, ns, _schema.Document);
                    attribute.Value = value;
                    _unhandledAttributes.Add(attribute);
                }
            }
            else
            {
                SendValidationEvent(SR.Sch_UnsupportedAttribute, qname.ToString());
            }
        }

        internal override bool IsContentParsed()
        {
            return _currentEntry.ParseContent;
        }

        internal override void ProcessMarkup(XmlNode[] markup)
        {
            _markup = markup;
        }

        internal override void ProcessCData(string value)
        {
            SendValidationEvent(SR.Sch_TextNotAllowed, value);
        }

        internal override void StartChildren()
        {
            if (_xso != null)
            {
                if (_namespaces != null && _namespaces.Count > 0)
                {
                    _xso.Namespaces.Namespaces = _namespaces;
                    _namespaces = null;
                }
                if (_unhandledAttributes.Count != 0)
                {
                    _xso.SetUnhandledAttributes((XmlAttribute[])_unhandledAttributes.ToArray(typeof(System.Xml.XmlAttribute)));
                    _unhandledAttributes.Clear();
                }
            }
        }

        internal override void EndChildren()
        {
            if (_currentEntry.EndChildFunc != null)
            {
                (this._currentEntry.EndChildFunc)(this);
            }
            Pop();
        }


        // State stack push & pop
        private void Push()
        {
            _stateHistory.Push();
            _stateHistory[_stateHistory.Length - 1] = _currentEntry;
            _containerStack.Push(GetContainer(_currentEntry.CurrentState));
            _currentEntry = _nextEntry;
            if (_currentEntry.Name != SchemaNames.Token.XsdAnnotation)
            {
                _hasChild = false;
            }
        }

        private void Pop()
        {
            _currentEntry = (XsdEntry)_stateHistory.Pop();
            SetContainer(_currentEntry.CurrentState, _containerStack.Pop());
            _hasChild = true;
        }

        private SchemaNames.Token CurrentElement
        {
            get { return _currentEntry.Name; }
        }

        private SchemaNames.Token ParentElement
        {
            get { return ((XsdEntry)_stateHistory[_stateHistory.Length - 1]).Name; }
        }

        private XmlSchemaObject ParentContainer
        {
            get { return (XmlSchemaObject)_containerStack.Peek(); }
        }

        private XmlSchemaObject GetContainer(State state)
        {
            XmlSchemaObject container = null;
            switch (state)
            {
                case State.Root:
                    break;
                case State.Schema:
                    container = _schema;
                    break;
                case State.Annotation:
                    container = _annotation;
                    break;
                case State.Include:
                    container = _include;
                    break;
                case State.Import:
                    container = _import;
                    break;
                case State.Element:
                    container = _element;
                    break;
                case State.Attribute:
                    container = _attribute;
                    break;
                case State.AttributeGroup:
                    container = _attributeGroup;
                    break;
                case State.AttributeGroupRef:
                    container = _attributeGroupRef;
                    break;
                case State.AnyAttribute:
                    container = _anyAttribute;
                    break;
                case State.Group:
                    container = _group;
                    break;
                case State.GroupRef:
                    container = _groupRef;
                    break;
                case State.All:
                    container = _all;
                    break;
                case State.Choice:
                    container = _choice;
                    break;
                case State.Sequence:
                    container = _sequence;
                    break;
                case State.Any:
                    container = _anyElement;
                    break;
                case State.Notation:
                    container = _notation;
                    break;
                case State.SimpleType:
                    container = _simpleType;
                    break;
                case State.ComplexType:
                    container = _complexType;
                    break;
                case State.ComplexContent:
                    container = _complexContent;
                    break;
                case State.ComplexContentExtension:
                    container = _complexContentExtension;
                    break;
                case State.ComplexContentRestriction:
                    container = _complexContentRestriction;
                    break;
                case State.SimpleContent:
                    container = _simpleContent;
                    break;
                case State.SimpleContentExtension:
                    container = _simpleContentExtension;
                    break;
                case State.SimpleContentRestriction:
                    container = _simpleContentRestriction;
                    break;
                case State.SimpleTypeUnion:
                    container = _simpleTypeUnion;
                    break;
                case State.SimpleTypeList:
                    container = _simpleTypeList;
                    break;
                case State.SimpleTypeRestriction:
                    container = _simpleTypeRestriction;
                    break;
                case State.Unique:
                case State.Key:
                case State.KeyRef:
                    container = _identityConstraint;
                    break;
                case State.Selector:
                case State.Field:
                    container = _xpath;
                    break;
                case State.MinExclusive:
                case State.MinInclusive:
                case State.MaxExclusive:
                case State.MaxInclusive:
                case State.TotalDigits:
                case State.FractionDigits:
                case State.Length:
                case State.MinLength:
                case State.MaxLength:
                case State.Enumeration:
                case State.Pattern:
                case State.WhiteSpace:
                    container = _facet;
                    break;
                case State.AppInfo:
                    container = _appInfo;
                    break;
                case State.Documentation:
                    container = _documentation;
                    break;
                case State.Redefine:
                    container = _redefine;
                    break;
                default:
                    Debug.Fail("State is " + state);
                    break;
            }
            return container;
        }

        private void SetContainer(State state, object container)
        {
            switch (state)
            {
                case State.Root:
                    break;
                case State.Schema:
                    break;
                case State.Annotation:
                    _annotation = (XmlSchemaAnnotation)container;
                    break;
                case State.Include:
                    _include = (XmlSchemaInclude)container;
                    break;
                case State.Import:
                    _import = (XmlSchemaImport)container;
                    break;
                case State.Element:
                    _element = (XmlSchemaElement)container;
                    break;
                case State.Attribute:
                    _attribute = (XmlSchemaAttribute)container;
                    break;
                case State.AttributeGroup:
                    _attributeGroup = (XmlSchemaAttributeGroup)container;
                    break;
                case State.AttributeGroupRef:
                    _attributeGroupRef = (XmlSchemaAttributeGroupRef)container;
                    break;
                case State.AnyAttribute:
                    _anyAttribute = (XmlSchemaAnyAttribute)container;
                    break;
                case State.Group:
                    _group = (XmlSchemaGroup)container;
                    break;
                case State.GroupRef:
                    _groupRef = (XmlSchemaGroupRef)container;
                    break;
                case State.All:
                    _all = (XmlSchemaAll)container;
                    break;
                case State.Choice:
                    _choice = (XmlSchemaChoice)container;
                    break;
                case State.Sequence:
                    _sequence = (XmlSchemaSequence)container;
                    break;
                case State.Any:
                    _anyElement = (XmlSchemaAny)container;
                    break;
                case State.Notation:
                    _notation = (XmlSchemaNotation)container;
                    break;
                case State.SimpleType:
                    _simpleType = (XmlSchemaSimpleType)container;
                    break;
                case State.ComplexType:
                    _complexType = (XmlSchemaComplexType)container;
                    break;
                case State.ComplexContent:
                    _complexContent = (XmlSchemaComplexContent)container;
                    break;
                case State.ComplexContentExtension:
                    _complexContentExtension = (XmlSchemaComplexContentExtension)container;
                    break;
                case State.ComplexContentRestriction:
                    _complexContentRestriction = (XmlSchemaComplexContentRestriction)container;
                    break;
                case State.SimpleContent:
                    _simpleContent = (XmlSchemaSimpleContent)container;
                    break;
                case State.SimpleContentExtension:
                    _simpleContentExtension = (XmlSchemaSimpleContentExtension)container;
                    break;
                case State.SimpleContentRestriction:
                    _simpleContentRestriction = (XmlSchemaSimpleContentRestriction)container;
                    break;
                case State.SimpleTypeUnion:
                    _simpleTypeUnion = (XmlSchemaSimpleTypeUnion)container;
                    break;
                case State.SimpleTypeList:
                    _simpleTypeList = (XmlSchemaSimpleTypeList)container;
                    break;
                case State.SimpleTypeRestriction:
                    _simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)container;
                    break;
                case State.Unique:
                case State.Key:
                case State.KeyRef:
                    _identityConstraint = (XmlSchemaIdentityConstraint)container;
                    break;
                case State.Selector:
                case State.Field:
                    _xpath = (XmlSchemaXPath)container;
                    break;
                case State.MinExclusive:
                case State.MinInclusive:
                case State.MaxExclusive:
                case State.MaxInclusive:
                case State.TotalDigits:
                case State.FractionDigits:
                case State.Length:
                case State.MinLength:
                case State.MaxLength:
                case State.Enumeration:
                case State.Pattern:
                case State.WhiteSpace:
                    _facet = (XmlSchemaFacet)container;
                    break;
                case State.AppInfo:
                    _appInfo = (XmlSchemaAppInfo)container;
                    break;
                case State.Documentation:
                    _documentation = (XmlSchemaDocumentation)container;
                    break;
                case State.Redefine:
                    _redefine = (XmlSchemaRedefine)container;
                    break;
                default:
                    Debug.Fail("State is " + state);
                    break;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // XSD Schema
        //

        private static void BuildAnnotated_Id(XsdBuilder builder, string value)
        {
            builder._xso.IdAttribute = value;
        }

        /*
            <schema 
              attributeFormDefault = qualified | unqualified : unqualified
              blockDefault = #all or (possibly empty) subset of {substitution, extension, restriction} 
              elementFormDefault = qualified | unqualified : unqualified
              finalDefault = #all or (possibly empty) subset of {extension, restriction} 
              id = ID 
              targetNamespace = uriReference 
              version = string 
              {any attributes with non-schema namespace . . .}>
              Content: ((include | import | redefine | annotation)* , ((attribute | attributeGroup | complexType | element | group | notation | simpleType) , annotation*)*)
            </schema>
        */

        private static void BuildSchema_AttributeFormDefault(XsdBuilder builder, string value)
        {
            builder._schema.AttributeFormDefault = (XmlSchemaForm)builder.ParseEnum(value, "attributeFormDefault", s_formStringValues);
        }

        private static void BuildSchema_ElementFormDefault(XsdBuilder builder, string value)
        {
            builder._schema.ElementFormDefault = (XmlSchemaForm)builder.ParseEnum(value, "elementFormDefault", s_formStringValues);
        }

        private static void BuildSchema_TargetNamespace(XsdBuilder builder, string value)
        {
            builder._schema.TargetNamespace = value;
        }

        private static void BuildSchema_Version(XsdBuilder builder, string value)
        {
            builder._schema.Version = value;
        }

        private static void BuildSchema_FinalDefault(XsdBuilder builder, string value)
        {
            builder._schema.FinalDefault = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "finalDefault");
        }

        private static void BuildSchema_BlockDefault(XsdBuilder builder, string value)
        {
            builder._schema.BlockDefault = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "blockDefault");
        }

        private static void InitSchema(XsdBuilder builder, string value)
        {
            builder._canIncludeImport = true;
            builder._xso = builder._schema;
        }

        /*
            <include 
              id = ID 
              schemaLocation = uriReference 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </include>
        */
        private static void InitInclude(XsdBuilder builder, string value)
        {
            if (!builder._canIncludeImport)
            {
                builder.SendValidationEvent(SR.Sch_IncludeLocation, null);
            }
            builder._xso = builder._include = new XmlSchemaInclude();
            builder._schema.Includes.Add(builder._include);
        }

        private static void BuildInclude_SchemaLocation(XsdBuilder builder, string value)
        {
            builder._include.SchemaLocation = value;
        }

        /*
            <import 
              id = ID 
              namespace = uriReference 
              schemaLocation = uriReference 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </import>
        */
        private static void InitImport(XsdBuilder builder, string value)
        {
            if (!builder._canIncludeImport)
            {
                builder.SendValidationEvent(SR.Sch_ImportLocation, null);
            }
            builder._xso = builder._import = new XmlSchemaImport();
            builder._schema.Includes.Add(builder._import);
        }

        private static void BuildImport_Namespace(XsdBuilder builder, string value)
        {
            builder._import.Namespace = value;
        }

        private static void BuildImport_SchemaLocation(XsdBuilder builder, string value)
        {
            builder._import.SchemaLocation = value;
        }

        /*
            <redefine 
              schemaLocation = uriReference 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation | (attributeGroup | complexType | group | simpleType))*
            </redefine>
        */
        private static void InitRedefine(XsdBuilder builder, string value)
        {
            if (!builder._canIncludeImport)
            {
                builder.SendValidationEvent(SR.Sch_RedefineLocation, null);
            }
            builder._xso = builder._redefine = new XmlSchemaRedefine();
            builder._schema.Includes.Add(builder._redefine);
        }

        private static void BuildRedefine_SchemaLocation(XsdBuilder builder, string value)
        {
            builder._redefine.SchemaLocation = value;
        }

        private static void EndRedefine(XsdBuilder builder)
        {
            builder._canIncludeImport = true;
        }

        /*
            <attribute 
              form = qualified | unqualified 
              id = ID 
              name = NCName 
              ref = QName 
              type = QName 
              use = prohibited | optional | required | default | fixed : optional
              value = string 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (simpleType?))
            </attribute>
        */
        private static void InitAttribute(XsdBuilder builder, string value)
        {
            builder._xso = builder._attribute = new XmlSchemaAttribute();
            if (builder.ParentElement == SchemaNames.Token.XsdSchema)
                builder._schema.Items.Add(builder._attribute);
            else
                builder.AddAttribute(builder._attribute);
            builder._canIncludeImport = false;  // disable import and include elements in schema
        }

        private static void BuildAttribute_Default(XsdBuilder builder, string value)
        {
            builder._attribute.DefaultValue = value;
        }

        private static void BuildAttribute_Fixed(XsdBuilder builder, string value)
        {
            builder._attribute.FixedValue = value;
        }

        private static void BuildAttribute_Form(XsdBuilder builder, string value)
        {
            builder._attribute.Form = (XmlSchemaForm)builder.ParseEnum(value, "form", s_formStringValues);
        }

        private static void BuildAttribute_Use(XsdBuilder builder, string value)
        {
            builder._attribute.Use = (XmlSchemaUse)builder.ParseEnum(value, "use", s_useStringValues);
        }

        private static void BuildAttribute_Ref(XsdBuilder builder, string value)
        {
            builder._attribute.RefName = builder.ParseQName(value, "ref");
        }

        private static void BuildAttribute_Name(XsdBuilder builder, string value)
        {
            builder._attribute.Name = value;
        }

        private static void BuildAttribute_Type(XsdBuilder builder, string value)
        {
            builder._attribute.SchemaTypeName = builder.ParseQName(value, "type");
        }

        /*
            <element 
              abstract = boolean : false
              block = #all or (possibly empty) subset of {substitution, extension, restriction} 
              default = string 
              final = #all or (possibly empty) subset of {extension, restriction} 
              fixed = string 
              form = qualified | unqualified 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              name = NCName 
              nillable = boolean : false
              ref = QName 
              substitutionGroup = QName 
              type = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((simpleType | complexType)? , (key | keyref | unique)*))
            </element>
        */
        private static void InitElement(XsdBuilder builder, string value)
        {
            builder._xso = builder._element = new XmlSchemaElement();
            builder._canIncludeImport = false;
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdSchema:
                    builder._schema.Items.Add(builder._element);
                    break;
                case SchemaNames.Token.XsdAll:
                    builder._all.Items.Add(builder._element);
                    break;
                case SchemaNames.Token.XsdChoice:
                    builder._choice.Items.Add(builder._element);
                    break;
                case SchemaNames.Token.XsdSequence:
                    builder._sequence.Items.Add(builder._element);
                    break;
                default:
                    Debug.Fail($"Unexpected parent element {builder.ParentElement}");
                    break;
            }
        }

        private static void BuildElement_Abstract(XsdBuilder builder, string value)
        {
            builder._element.IsAbstract = builder.ParseBoolean(value, "abstract");
        }

        private static void BuildElement_Block(XsdBuilder builder, string value)
        {
            builder._element.Block = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "block");
        }

        private static void BuildElement_Default(XsdBuilder builder, string value)
        {
            builder._element.DefaultValue = value;
        }

        private static void BuildElement_Form(XsdBuilder builder, string value)
        {
            builder._element.Form = (XmlSchemaForm)builder.ParseEnum(value, "form", s_formStringValues);
        }

        private static void BuildElement_SubstitutionGroup(XsdBuilder builder, string value)
        {
            builder._element.SubstitutionGroup = builder.ParseQName(value, "substitutionGroup");
        }

        private static void BuildElement_Final(XsdBuilder builder, string value)
        {
            builder._element.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
        }

        private static void BuildElement_Fixed(XsdBuilder builder, string value)
        {
            builder._element.FixedValue = value;
        }

        private static void BuildElement_MaxOccurs(XsdBuilder builder, string value)
        {
            builder.SetMaxOccurs(builder._element, value);
        }

        private static void BuildElement_MinOccurs(XsdBuilder builder, string value)
        {
            builder.SetMinOccurs(builder._element, value);
        }

        private static void BuildElement_Name(XsdBuilder builder, string value)
        {
            builder._element.Name = value;
        }

        private static void BuildElement_Nillable(XsdBuilder builder, string value)
        {
            builder._element.IsNillable = builder.ParseBoolean(value, "nillable");
        }

        private static void BuildElement_Ref(XsdBuilder builder, string value)
        {
            builder._element.RefName = builder.ParseQName(value, "ref");
        }

        private static void BuildElement_Type(XsdBuilder builder, string value)
        {
            builder._element.SchemaTypeName = builder.ParseQName(value, "type");
        }

        /*
            <simpleType 
              id = ID 
              name = NCName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((list | restriction | union)))
            </simpleType>
        */
        private static void InitSimpleType(XsdBuilder builder, string value)
        {
            builder._xso = builder._simpleType = new XmlSchemaSimpleType();
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdSchema:
                    builder._canIncludeImport = false;  // disable import and include elements in schema
                    builder._schema.Items.Add(builder._simpleType);
                    break;
                case SchemaNames.Token.XsdRedefine:
                    builder._redefine.Items.Add(builder._simpleType);
                    break;
                case SchemaNames.Token.XsdAttribute:
                    if (builder._attribute.SchemaType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupXsdElement, "simpleType");
                    }
                    builder._attribute.SchemaType = builder._simpleType;
                    break;
                case SchemaNames.Token.XsdElement:
                    if (builder._element.SchemaType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupXsdElement, "simpleType");
                    }
                    if (builder._element.Constraints.Count != 0)
                    {
                        builder.SendValidationEvent(SR.Sch_TypeAfterConstraints, null);
                    }
                    builder._element.SchemaType = builder._simpleType;
                    break;
                case SchemaNames.Token.XsdSimpleTypeList:
                    if (builder._simpleTypeList.ItemType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupXsdElement, "simpleType");
                    }
                    builder._simpleTypeList.ItemType = builder._simpleType;
                    break;
                case SchemaNames.Token.XsdSimpleTypeRestriction:
                    if (builder._simpleTypeRestriction.BaseType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupXsdElement, "simpleType");
                    }
                    builder._simpleTypeRestriction.BaseType = builder._simpleType;
                    break;
                case SchemaNames.Token.XsdSimpleContentRestriction:
                    if (builder._simpleContentRestriction.BaseType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupXsdElement, "simpleType");
                    }
                    if (
                        builder._simpleContentRestriction.Attributes.Count != 0 ||
                        builder._simpleContentRestriction.AnyAttribute != null ||
                        builder._simpleContentRestriction.Facets.Count != 0
                    )
                    {
                        builder.SendValidationEvent(SR.Sch_SimpleTypeRestriction, null);
                    }
                    builder._simpleContentRestriction.BaseType = builder._simpleType;
                    break;

                case SchemaNames.Token.XsdSimpleTypeUnion:
                    builder._simpleTypeUnion.BaseTypes.Add(builder._simpleType);
                    break;
            }
        }

        private static void BuildSimpleType_Name(XsdBuilder builder, string value)
        {
            builder._simpleType.Name = value;
        }

        private static void BuildSimpleType_Final(XsdBuilder builder, string value)
        {
            builder._simpleType.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
        }


        /*
            <union 
              id = ID 
              memberTypes = List of [anon]
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (simpleType*))
            </union>
        */
        private static void InitSimpleTypeUnion(XsdBuilder builder, string value)
        {
            if (builder._simpleType.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_DupSimpleTypeChild, null);
            }
            builder._xso = builder._simpleTypeUnion = new XmlSchemaSimpleTypeUnion();
            builder._simpleType.Content = builder._simpleTypeUnion;
        }

        private static void BuildSimpleTypeUnion_MemberTypes(XsdBuilder builder, string value)
        {
            XmlSchemaDatatype dt = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName).DeriveByList(null);
            try
            {
                builder._simpleTypeUnion.MemberTypes = (XmlQualifiedName[])dt.ParseValue(value, builder._nameTable, builder._namespaceManager);
            }
            catch (XmlSchemaException e)
            {
                e.SetSource(builder._reader.BaseURI, builder._positionInfo.LineNumber, builder._positionInfo.LinePosition);
                builder.SendValidationEvent(e);
            }
        }


        /*
            <list 
              id = ID 
              itemType = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (simpleType?))
            </list>
        */
        private static void InitSimpleTypeList(XsdBuilder builder, string value)
        {
            if (builder._simpleType.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_DupSimpleTypeChild, null);
            }
            builder._xso = builder._simpleTypeList = new XmlSchemaSimpleTypeList();
            builder._simpleType.Content = builder._simpleTypeList;
        }

        private static void BuildSimpleTypeList_ItemType(XsdBuilder builder, string value)
        {
            builder._simpleTypeList.ItemTypeName = builder.ParseQName(value, "itemType");
        }

        /*
            <restriction 
              base = QName 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (simpleType? , ((duration | encoding | enumeration | length | maxExclusive | maxInclusive | maxLength | minExclusive | minInclusive | minLength | pattern | period | TotalDigits | FractionDigits)*)))
            </restriction>
        */
        private static void InitSimpleTypeRestriction(XsdBuilder builder, string value)
        {
            if (builder._simpleType.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_DupSimpleTypeChild, null);
            }
            builder._xso = builder._simpleTypeRestriction = new XmlSchemaSimpleTypeRestriction();
            builder._simpleType.Content = builder._simpleTypeRestriction;
        }

        private static void BuildSimpleTypeRestriction_Base(XsdBuilder builder, string value)
        {
            builder._simpleTypeRestriction.BaseTypeName = builder.ParseQName(value, "base");
        }

        /*
            <complexType 
              abstract = boolean : false
              block = #all or (possibly empty) subset of {extension, restriction} 
              final = #all or (possibly empty) subset of {extension, restriction} 
              id = ID 
              mixed = boolean : false
              name = NCName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (simpleContent | complexContent | ((group | all | choice | sequence)? , ((attribute | attributeGroup)* , anyAttribute?))))
            </complexType>
        */
        private static void InitComplexType(XsdBuilder builder, string value)
        {
            builder._xso = builder._complexType = new XmlSchemaComplexType();
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdSchema:
                    builder._canIncludeImport = false;  // disable import and include elements in schema
                    builder._schema.Items.Add(builder._complexType);
                    break;
                case SchemaNames.Token.XsdRedefine:
                    builder._redefine.Items.Add(builder._complexType);
                    break;
                case SchemaNames.Token.XsdElement:
                    if (builder._element.SchemaType != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "complexType");
                    }
                    if (builder._element.Constraints.Count != 0)
                    {
                        builder.SendValidationEvent(SR.Sch_TypeAfterConstraints, null);
                    }
                    builder._element.SchemaType = builder._complexType;
                    break;
            }
        }

        private static void BuildComplexType_Abstract(XsdBuilder builder, string value)
        {
            builder._complexType.IsAbstract = builder.ParseBoolean(value, "abstract");
        }

        private static void BuildComplexType_Block(XsdBuilder builder, string value)
        {
            builder._complexType.Block = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "block");
        }

        private static void BuildComplexType_Final(XsdBuilder builder, string value)
        {
            builder._complexType.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
        }

        private static void BuildComplexType_Mixed(XsdBuilder builder, string value)
        {
            builder._complexType.IsMixed = builder.ParseBoolean(value, "mixed");
        }

        private static void BuildComplexType_Name(XsdBuilder builder, string value)
        {
            builder._complexType.Name = value;
        }

        /*
            <complexContent 
              id = ID 
              mixed = boolean 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (restriction | extension))
            </complexContent>
        */
        private static void InitComplexContent(XsdBuilder builder, string value)
        {
            if ((builder._complexType.ContentModel != null) ||
                 (builder._complexType.Particle != null || builder._complexType.Attributes.Count != 0 || builder._complexType.AnyAttribute != null)
               )
            {
                builder.SendValidationEvent(SR.Sch_ComplexTypeContentModel, "complexContent");
            }
            builder._xso = builder._complexContent = new XmlSchemaComplexContent();
            builder._complexType.ContentModel = builder._complexContent;
        }

        private static void BuildComplexContent_Mixed(XsdBuilder builder, string value)
        {
            builder._complexContent.IsMixed = builder.ParseBoolean(value, "mixed");
        }

        /*
            <extension 
              base = QName 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((group | all | choice | sequence)? , ((attribute | attributeGroup)* , anyAttribute?)))
            </extension>
        */
        private static void InitComplexContentExtension(XsdBuilder builder, string value)
        {
            if (builder._complexContent.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_ComplexContentContentModel, "extension");
            }
            builder._xso = builder._complexContentExtension = new XmlSchemaComplexContentExtension();
            builder._complexContent.Content = builder._complexContentExtension;
        }

        private static void BuildComplexContentExtension_Base(XsdBuilder builder, string value)
        {
            builder._complexContentExtension.BaseTypeName = builder.ParseQName(value, "base");
        }

        /*
            <restriction 
              base = QName 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (group | all | choice | sequence)? , ((attribute | attributeGroup)* , anyAttribute?))
            </restriction>
        */
        private static void InitComplexContentRestriction(XsdBuilder builder, string value)
        {
            builder._xso = builder._complexContentRestriction = new XmlSchemaComplexContentRestriction();
            builder._complexContent.Content = builder._complexContentRestriction;
        }

        private static void BuildComplexContentRestriction_Base(XsdBuilder builder, string value)
        {
            builder._complexContentRestriction.BaseTypeName = builder.ParseQName(value, "base");
        }

        /*
            <simpleContent 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (restriction | extension))
            </simpleContent>
        */
        private static void InitSimpleContent(XsdBuilder builder, string value)
        {
            if ((builder._complexType.ContentModel != null) ||
                 (builder._complexType.Particle != null || builder._complexType.Attributes.Count != 0 || builder._complexType.AnyAttribute != null)
                 )
            {
                builder.SendValidationEvent(SR.Sch_ComplexTypeContentModel, "simpleContent");
            }
            builder._xso = builder._simpleContent = new XmlSchemaSimpleContent();
            builder._complexType.ContentModel = builder._simpleContent;
        }

        /*
            <extension 
              base = QName 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((attribute | attributeGroup)* , anyAttribute?))
            </extension>
        */

        private static void InitSimpleContentExtension(XsdBuilder builder, string value)
        {
            if (builder._simpleContent.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_DupElement, "extension");
            }
            builder._xso = builder._simpleContentExtension = new XmlSchemaSimpleContentExtension();
            builder._simpleContent.Content = builder._simpleContentExtension;
        }

        private static void BuildSimpleContentExtension_Base(XsdBuilder builder, string value)
        {
            builder._simpleContentExtension.BaseTypeName = builder.ParseQName(value, "base");
        }


        /*
            <restriction 
              base = QName 
              id = ID 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((duration | encoding | enumeration | length | maxExclusive | maxInclusive | maxLength | minExclusive | minInclusive | minLength | pattern | period | totalDigits | fractionDigits)*)? , ((attribute | attributeGroup)* , anyAttribute?))
            </restriction>
        */
        private static void InitSimpleContentRestriction(XsdBuilder builder, string value)
        {
            if (builder._simpleContent.Content != null)
            {
                builder.SendValidationEvent(SR.Sch_DupElement, "restriction");
            }
            builder._xso = builder._simpleContentRestriction = new XmlSchemaSimpleContentRestriction();
            builder._simpleContent.Content = builder._simpleContentRestriction;
        }

        private static void BuildSimpleContentRestriction_Base(XsdBuilder builder, string value)
        {
            builder._simpleContentRestriction.BaseTypeName = builder.ParseQName(value, "base");
        }

        /*
            <attributeGroup 
              id = ID 
              name = NCName 
              ref = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , ((attribute | attributeGroup)* , anyAttribute?))
            </attributeGroup>
        */
        private static void InitAttributeGroup(XsdBuilder builder, string value)
        {
            builder._canIncludeImport = false;
            builder._xso = builder._attributeGroup = new XmlSchemaAttributeGroup();
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdSchema:
                    builder._schema.Items.Add(builder._attributeGroup);
                    break;
                case SchemaNames.Token.XsdRedefine:
                    builder._redefine.Items.Add(builder._attributeGroup);
                    break;
            }
        }

        private static void BuildAttributeGroup_Name(XsdBuilder builder, string value)
        {
            builder._attributeGroup.Name = value;
        }

        /*
            <attributeGroup 
              id = ID 
              ref = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </attributeGroup>
        */
        private static void InitAttributeGroupRef(XsdBuilder builder, string value)
        {
            builder._xso = builder._attributeGroupRef = new XmlSchemaAttributeGroupRef();
            builder.AddAttribute(builder._attributeGroupRef);
        }

        private static void BuildAttributeGroupRef_Ref(XsdBuilder builder, string value)
        {
            builder._attributeGroupRef.RefName = builder.ParseQName(value, "ref");
        }

        /*
            <anyAttribute 
              id = ID 
              namespace = ##any | ##other | list of {uri, ##targetNamespace, ##local} : ##any
              processContents = skip | lax | strict : strict
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </anyAttribute>
        */
        private static void InitAnyAttribute(XsdBuilder builder, string value)
        {
            builder._xso = builder._anyAttribute = new XmlSchemaAnyAttribute();
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdComplexType:
                    if (builder._complexType.ContentModel != null)
                    {
                        builder.SendValidationEvent(SR.Sch_AttributeMutuallyExclusive, "anyAttribute");
                    }
                    if (builder._complexType.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._complexType.AnyAttribute = builder._anyAttribute;
                    break;
                case SchemaNames.Token.XsdSimpleContentRestriction:
                    if (builder._simpleContentRestriction.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._simpleContentRestriction.AnyAttribute = builder._anyAttribute;
                    break;
                case SchemaNames.Token.XsdSimpleContentExtension:
                    if (builder._simpleContentExtension.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._simpleContentExtension.AnyAttribute = builder._anyAttribute;
                    break;
                case SchemaNames.Token.XsdComplexContentExtension:
                    if (builder._complexContentExtension.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._complexContentExtension.AnyAttribute = builder._anyAttribute;
                    break;
                case SchemaNames.Token.XsdComplexContentRestriction:
                    if (builder._complexContentRestriction.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._complexContentRestriction.AnyAttribute = builder._anyAttribute;
                    break;
                case SchemaNames.Token.xsdAttributeGroup:
                    if (builder._attributeGroup.AnyAttribute != null)
                    {
                        builder.SendValidationEvent(SR.Sch_DupElement, "anyAttribute");
                    }
                    builder._attributeGroup.AnyAttribute = builder._anyAttribute;
                    break;
            }
        }

        private static void BuildAnyAttribute_Namespace(XsdBuilder builder, string value)
        {
            builder._anyAttribute.Namespace = value;
        }

        private static void BuildAnyAttribute_ProcessContents(XsdBuilder builder, string value)
        {
            builder._anyAttribute.ProcessContents = (XmlSchemaContentProcessing)builder.ParseEnum(value, "processContents", s_processContentsStringValues);
        }

        /*
            <group 
              id = ID 
              name = NCName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (all | choice | sequence)?)
            </group>
        */
        private static void InitGroup(XsdBuilder builder, string value)
        {
            builder._xso = builder._group = new XmlSchemaGroup();
            builder._canIncludeImport = false;  // disable import and include elements in schema
            switch (builder.ParentElement)
            {
                case SchemaNames.Token.XsdSchema:
                    builder._schema.Items.Add(builder._group);
                    break;
                case SchemaNames.Token.XsdRedefine:
                    builder._redefine.Items.Add(builder._group);
                    break;
            }
        }

        private static void BuildGroup_Name(XsdBuilder builder, string value)
        {
            builder._group.Name = value;
        }

        /*
            <group 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              ref = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </group>
        */
        private static void InitGroupRef(XsdBuilder builder, string value)
        {
            builder._xso = builder._particle = builder._groupRef = new XmlSchemaGroupRef();
            builder.AddParticle(builder._groupRef);
        }

        private static void BuildParticle_MaxOccurs(XsdBuilder builder, string value)
        {
            builder.SetMaxOccurs(builder._particle, value);
        }

        private static void BuildParticle_MinOccurs(XsdBuilder builder, string value)
        {
            builder.SetMinOccurs(builder._particle, value);
        }

        private static void BuildGroupRef_Ref(XsdBuilder builder, string value)
        {
            builder._groupRef.RefName = builder.ParseQName(value, "ref");
        }

        /*
            <all 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , element*)
            </all>
        */
        private static void InitAll(XsdBuilder builder, string value)
        {
            builder._xso = builder._particle = builder._all = new XmlSchemaAll();
            builder.AddParticle(builder._all);
        }

        /*
            <choice 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (element | group | choice | sequence | any)*)
            </choice>
        */
        private static void InitChoice(XsdBuilder builder, string value)
        {
            builder._xso = builder._particle = builder._choice = new XmlSchemaChoice();
            builder.AddParticle(builder._choice);
        }

        /*
             <sequence 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (element | group | choice | sequence | any)*)
            </sequence>
        */
        private static void InitSequence(XsdBuilder builder, string value)
        {
            builder._xso = builder._particle = builder._sequence = new XmlSchemaSequence();
            builder.AddParticle(builder._sequence);
        }

        /*
            <any 
              id = ID 
              maxOccurs = for maxOccurs : 1
              minOccurs = nonNegativeInteger : 1
              namespace = ##any | ##other | list of {uri, ##targetNamespace, ##local} : ##any
              processContents = skip | lax | strict : strict
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </any>
        */
        private static void InitAny(XsdBuilder builder, string value)
        {
            builder._xso = builder._particle = builder._anyElement = new XmlSchemaAny();
            builder.AddParticle(builder._anyElement);
        }

        private static void BuildAny_Namespace(XsdBuilder builder, string value)
        {
            builder._anyElement.Namespace = value;
        }

        private static void BuildAny_ProcessContents(XsdBuilder builder, string value)
        {
            builder._anyElement.ProcessContents = (XmlSchemaContentProcessing)builder.ParseEnum(value, "processContents", s_processContentsStringValues);
        }

        /*
            <notation 
              id = ID 
              name = NCName 
              public = A public identifier, per ISO 8879 
              system = uriReference 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </notation>
        */
        private static void InitNotation(XsdBuilder builder, string value)
        {
            builder._xso = builder._notation = new XmlSchemaNotation();
            builder._canIncludeImport = false;
            builder._schema.Items.Add(builder._notation);
        }

        private static void BuildNotation_Name(XsdBuilder builder, string value)
        {
            builder._notation.Name = value;
        }

        private static void BuildNotation_Public(XsdBuilder builder, string value)
        {
            builder._notation.Public = value;
        }

        private static void BuildNotation_System(XsdBuilder builder, string value)
        {
            builder._notation.System = value;
        }

        //
        // Facets
        //
        /*
            <duration 
              id = ID 
              value = timeDuration 
              fixed = boolean : false>
              Content: (annotation?)
            </duration>
        */
        private static void InitFacet(XsdBuilder builder, string value)
        {
            switch (builder.CurrentElement)
            {
                case SchemaNames.Token.XsdEnumeration:
                    builder._facet = new XmlSchemaEnumerationFacet();
                    break;
                case SchemaNames.Token.XsdLength:
                    builder._facet = new XmlSchemaLengthFacet();
                    break;
                case SchemaNames.Token.XsdMaxExclusive:
                    builder._facet = new XmlSchemaMaxExclusiveFacet();
                    break;
                case SchemaNames.Token.XsdMaxInclusive:
                    builder._facet = new XmlSchemaMaxInclusiveFacet();
                    break;
                case SchemaNames.Token.XsdMaxLength:
                    builder._facet = new XmlSchemaMaxLengthFacet();
                    break;
                case SchemaNames.Token.XsdMinExclusive:
                    builder._facet = new XmlSchemaMinExclusiveFacet();
                    break;
                case SchemaNames.Token.XsdMinInclusive:
                    builder._facet = new XmlSchemaMinInclusiveFacet();
                    break;
                case SchemaNames.Token.XsdMinLength:
                    builder._facet = new XmlSchemaMinLengthFacet();
                    break;
                case SchemaNames.Token.XsdPattern:
                    builder._facet = new XmlSchemaPatternFacet();
                    break;
                case SchemaNames.Token.XsdTotalDigits:
                    builder._facet = new XmlSchemaTotalDigitsFacet();
                    break;
                case SchemaNames.Token.XsdFractionDigits:
                    builder._facet = new XmlSchemaFractionDigitsFacet();
                    break;
                case SchemaNames.Token.XsdWhitespace:
                    builder._facet = new XmlSchemaWhiteSpaceFacet();
                    break;
            }
            builder._xso = builder._facet;
            if (SchemaNames.Token.XsdSimpleTypeRestriction == builder.ParentElement)
            {
                builder._simpleTypeRestriction.Facets.Add(builder._facet);
            }
            else
            {
                if (builder._simpleContentRestriction.Attributes.Count != 0 || (builder._simpleContentRestriction.AnyAttribute != null))
                {
                    builder.SendValidationEvent(SR.Sch_InvalidFacetPosition, null);
                }
                builder._simpleContentRestriction.Facets.Add(builder._facet);
            }
        }

        private static void BuildFacet_Fixed(XsdBuilder builder, string value)
        {
            builder._facet.IsFixed = builder.ParseBoolean(value, "fixed");
        }

        private static void BuildFacet_Value(XsdBuilder builder, string value)
        {
            builder._facet.Value = value;
        }

        /*
            <unique 
              id = ID 
              name = NCName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (selector , field+))
            </unique>
 
            <key 
              id = ID 
              name = NCName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (selector , field+))
            </key>
 
            <keyref 
              id = ID 
              name = NCName 
              refer = QName 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation? , (selector , field+))
            </keyref>
        */
        private static void InitIdentityConstraint(XsdBuilder builder, string value)
        {
            if (!builder._element.RefName.IsEmpty)
            {
                builder.SendValidationEvent(SR.Sch_ElementRef, null);
            }

            switch (builder.CurrentElement)
            {
                case SchemaNames.Token.XsdUnique:
                    builder._xso = builder._identityConstraint = new XmlSchemaUnique();
                    break;
                case SchemaNames.Token.XsdKey:
                    builder._xso = builder._identityConstraint = new XmlSchemaKey();
                    break;
                case SchemaNames.Token.XsdKeyref:
                    builder._xso = builder._identityConstraint = new XmlSchemaKeyref();
                    break;
            }
            builder._element.Constraints.Add(builder._identityConstraint);
        }

        private static void BuildIdentityConstraint_Name(XsdBuilder builder, string value)
        {
            builder._identityConstraint.Name = value;
        }

        private static void BuildIdentityConstraint_Refer(XsdBuilder builder, string value)
        {
            if (builder._identityConstraint is XmlSchemaKeyref)
            {
                ((XmlSchemaKeyref)builder._identityConstraint).Refer = builder.ParseQName(value, "refer");
            }
            else
            {
                builder.SendValidationEvent(SR.Sch_UnsupportedAttribute, "refer");
            }
        }

        /*
            <selector 
              id = ID 
              xpath = An XPath expression 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </selector>
        */
        private static void InitSelector(XsdBuilder builder, string value)
        {
            builder._xso = builder._xpath = new XmlSchemaXPath();
            if (builder._identityConstraint.Selector == null)
            {
                builder._identityConstraint.Selector = builder._xpath;
            }
            else
            {
                builder.SendValidationEvent(SR.Sch_DupSelector, builder._identityConstraint.Name);
            }
        }

        private static void BuildSelector_XPath(XsdBuilder builder, string value)
        {
            builder._xpath.XPath = value;
        }

        /*
            <field 
              id = ID 
              xpath = An XPath expression 
              {any attributes with non-schema namespace . . .}>
              Content: (annotation?)
            </field>
        */
        private static void InitField(XsdBuilder builder, string value)
        {
            builder._xso = builder._xpath = new XmlSchemaXPath();
            // no selector before fields?
            if (builder._identityConstraint.Selector == null)
            {
                builder.SendValidationEvent(SR.Sch_SelectorBeforeFields, builder._identityConstraint.Name);
            }
            builder._identityConstraint.Fields.Add(builder._xpath);
        }

        private static void BuildField_XPath(XsdBuilder builder, string value)
        {
            builder._xpath.XPath = value;
        }

        /*
            <annotation>
              Content: (appinfo | documentation)*
            </annotation>
        */
        private static void InitAnnotation(XsdBuilder builder, string value)
        {
            // On most elements annotations are only allowed to be the first child 
            //   (so the element must not have any children by now), and only one annotation is allowed.
            // Exceptions are xs:schema and xs:redefine, these can have any number of annotations
            //   in any place.
            if (builder._hasChild &&
                builder.ParentElement != SchemaNames.Token.XsdSchema &&
                builder.ParentElement != SchemaNames.Token.XsdRedefine)
            {
                builder.SendValidationEvent(SR.Sch_AnnotationLocation, null);
            }
            builder._xso = builder._annotation = new XmlSchemaAnnotation();
            builder.ParentContainer.AddAnnotation(builder._annotation);
        }

        /*
            <appinfo 
              source = uriReference>
              Content: ({any})*
            </appinfo>
        */
        private static void InitAppinfo(XsdBuilder builder, string value)
        {
            builder._xso = builder._appInfo = new XmlSchemaAppInfo();
            builder._annotation.Items.Add(builder._appInfo);
            builder._markup = Array.Empty<XmlNode>();
        }

        private static void BuildAppinfo_Source(XsdBuilder builder, string value)
        {
            builder._appInfo.Source = ParseUriReference(value);
        }

        private static void EndAppinfo(XsdBuilder builder)
        {
            builder._appInfo.Markup = builder._markup;
        }


        /*
            <documentation 
              source = uriReference>
              Content: ({any})*
            </documentation>
        */
        private static void InitDocumentation(XsdBuilder builder, string value)
        {
            builder._xso = builder._documentation = new XmlSchemaDocumentation();
            builder._annotation.Items.Add(builder._documentation);
            builder._markup = Array.Empty<XmlNode>();
        }

        private static void BuildDocumentation_Source(XsdBuilder builder, string value)
        {
            builder._documentation.Source = ParseUriReference(value);
        }

        private static void BuildDocumentation_XmlLang(XsdBuilder builder, string value)
        {
            try
            {
                builder._documentation.Language = value;
            }
            catch (XmlSchemaException e)
            {
                e.SetSource(builder._reader.BaseURI, builder._positionInfo.LineNumber, builder._positionInfo.LinePosition);
                builder.SendValidationEvent(e);
            }
        }

        private static void EndDocumentation(XsdBuilder builder)
        {
            builder._documentation.Markup = builder._markup;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////
        //
        // helper functions

        private void AddAttribute(XmlSchemaObject value)
        {
            switch (this.ParentElement)
            {
                case SchemaNames.Token.XsdComplexType:
                    if (_complexType.ContentModel != null)
                    {
                        SendValidationEvent(SR.Sch_AttributeMutuallyExclusive, "attribute");
                    }
                    if (_complexType.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _complexType.Attributes.Add(value);
                    break;
                case SchemaNames.Token.XsdSimpleContentRestriction:
                    if (_simpleContentRestriction.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _simpleContentRestriction.Attributes.Add(value);
                    break;
                case SchemaNames.Token.XsdSimpleContentExtension:
                    if (_simpleContentExtension.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _simpleContentExtension.Attributes.Add(value);
                    break;
                case SchemaNames.Token.XsdComplexContentExtension:
                    if (_complexContentExtension.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _complexContentExtension.Attributes.Add(value);
                    break;
                case SchemaNames.Token.XsdComplexContentRestriction:
                    if (_complexContentRestriction.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _complexContentRestriction.Attributes.Add(value);
                    break;
                case SchemaNames.Token.xsdAttributeGroup:
                    if (_attributeGroup.AnyAttribute != null)
                    {
                        SendValidationEvent(SR.Sch_AnyAttributeLastChild, null);
                    }
                    _attributeGroup.Attributes.Add(value);
                    break;
                default:
                    Debug.Fail($"Unexpected parent element {this.ParentElement}");
                    break;
            }
        }

        private void AddParticle(XmlSchemaParticle particle)
        {
            switch (this.ParentElement)
            {
                case SchemaNames.Token.XsdComplexType:
                    if ((_complexType.ContentModel != null) ||
                         (_complexType.Attributes.Count != 0 || _complexType.AnyAttribute != null) ||
                         (_complexType.Particle != null)
                         )
                    {
                        SendValidationEvent(SR.Sch_ComplexTypeContentModel, "complexType");
                    }
                    _complexType.Particle = particle;
                    break;
                case SchemaNames.Token.XsdComplexContentExtension:
                    if ((_complexContentExtension.Particle != null) ||
                         (_complexContentExtension.Attributes.Count != 0 || _complexContentExtension.AnyAttribute != null)
                       )
                    {
                        SendValidationEvent(SR.Sch_ComplexContentContentModel, "ComplexContentExtension");
                    }
                    _complexContentExtension.Particle = particle;
                    break;
                case SchemaNames.Token.XsdComplexContentRestriction:
                    if ((_complexContentRestriction.Particle != null) ||
                         (_complexContentRestriction.Attributes.Count != 0 || _complexContentRestriction.AnyAttribute != null)
                       )
                    {
                        SendValidationEvent(SR.Sch_ComplexContentContentModel, "ComplexContentExtension");
                    }
                    _complexContentRestriction.Particle = particle;
                    break;
                case SchemaNames.Token.XsdGroup:
                    if (_group.Particle != null)
                    {
                        SendValidationEvent(SR.Sch_DupGroupParticle, nameof(particle));
                    }
                    _group.Particle = (XmlSchemaGroupBase)particle;
                    break;
                case SchemaNames.Token.XsdChoice:
                case SchemaNames.Token.XsdSequence:
                    ((XmlSchemaGroupBase)this.ParentContainer).Items.Add(particle);
                    break;
                default:
                    Debug.Fail($"Unexpected parent element {this.ParentElement}");
                    break;
            }
        }

        private bool GetNextState(XmlQualifiedName qname)
        {
            if (_currentEntry.NextStates != null)
            {
                for (int i = 0; i < _currentEntry.NextStates.Length; ++i)
                {
                    int state = (int)_currentEntry.NextStates[i];
                    if (_schemaNames.TokenToQName[(int)s_schemaEntries[state].Name].Equals(qname))
                    {
                        _nextEntry = s_schemaEntries[state];
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSkipableElement(XmlQualifiedName qname)
        {
            return ((CurrentElement == SchemaNames.Token.XsdDocumentation) ||
                    (CurrentElement == SchemaNames.Token.XsdAppInfo));
        }

        private void SetMinOccurs(XmlSchemaParticle particle, string value)
        {
            try
            {
                particle.MinOccursString = value;
            }
            catch (Exception)
            {
                SendValidationEvent(SR.Sch_MinOccursInvalidXsd, null);
            }
        }

        private void SetMaxOccurs(XmlSchemaParticle particle, string value)
        {
            try
            {
                particle.MaxOccursString = value;
            }
            catch (Exception)
            {
                SendValidationEvent(SR.Sch_MaxOccursInvalidXsd, null);
            }
        }

        private bool ParseBoolean(string value, string attributeName)
        {
            try
            {
                return XmlConvert.ToBoolean(value);
            }
            catch (Exception)
            {
                SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
                return false;
            }
        }

        private int ParseEnum(string value, string attributeName, string[] values)
        {
            string s = value.Trim();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == s)
                    return i + 1;
            }
            SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, s, null);
            return 0;
        }

        private XmlQualifiedName ParseQName(string value, string attributeName)
        {
            try
            {
                string prefix;
                value = XmlComplianceUtil.NonCDataNormalize(value); //Normalize QName
                return XmlQualifiedName.Parse(value, _namespaceManager, out prefix);
            }
            catch (Exception)
            {
                SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
                return XmlQualifiedName.Empty;
            }
        }

        private int ParseBlockFinalEnum(string value, string attributeName)
        {
            const int HashAllLength = 4; // Length of "#all"
            int r = 0;
            string[] stringValues = XmlConvert.SplitString(value);
            for (int i = 0; i < stringValues.Length; i++)
            {
                bool matched = false;
                for (int j = 0; j < s_derivationMethodStrings.Length; j++)
                {
                    if (stringValues[i] == s_derivationMethodStrings[j])
                    {
                        if ((r & s_derivationMethodValues[j]) != 0 && (r & s_derivationMethodValues[j]) != s_derivationMethodValues[j])
                        {
                            SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
                            return 0;
                        }
                        r |= s_derivationMethodValues[j];
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
                    return 0;
                }
                if (r == (int)XmlSchemaDerivationMethod.All && value.Length > HashAllLength)
                { //#all is not allowed with other values
                    SendValidationEvent(SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
                    return 0;
                }
            }
            return r;
        }

        private static string ParseUriReference(string s)
        {
            return s;
        }

        private void SendValidationEvent(string code, string arg0, string arg1, string arg2)
        {
            SendValidationEvent(new XmlSchemaException(code, new string[] { arg0, arg1, arg2 }, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        private void SendValidationEvent(string code, string msg)
        {
            SendValidationEvent(new XmlSchemaException(code, msg, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        private void SendValidationEvent(string code, string[] args, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, args, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
        }

        private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
        {
            _schema.ErrorCount++;
            e.SetSchemaObject(_schema);
            if (_validationEventHandler != null)
            {
                _validationEventHandler(null, new ValidationEventArgs(e, severity));
            }
            else if (severity == XmlSeverityType.Error)
            {
                throw e;
            }
        }

        private void SendValidationEvent(XmlSchemaException e)
        {
            SendValidationEvent(e, XmlSeverityType.Error);
        }

        private void RecordPosition()
        {
            _xso.SourceUri = _reader.BaseURI;
            _xso.LineNumber = _positionInfo.LineNumber;
            _xso.LinePosition = _positionInfo.LinePosition;
            if (_xso != _schema)
            {
                _xso.Parent = this.ParentContainer;
            }
        }
    }; // class XsdBuilder
} // namespace System.Xml.Schema
