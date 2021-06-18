// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Xml.Schema
{
    internal sealed class SchemaCollectionCompiler : BaseProcessor
    {
        private bool _compileContentModel;
        private readonly XmlSchemaObjectTable _examplars = new XmlSchemaObjectTable();
        private readonly Stack<XmlSchemaComplexType> _complexTypeStack = new Stack<XmlSchemaComplexType>();
        private XmlSchema? _schema;

        public SchemaCollectionCompiler(XmlNameTable nameTable, ValidationEventHandler? eventHandler)
            : base(nameTable, null, eventHandler)
        {
        }

        public bool Execute(XmlSchema schema, SchemaInfo schemaInfo, bool compileContentModel)
        {
            _compileContentModel = compileContentModel;
            _schema = schema;
            Prepare();
            Cleanup();
            Compile();
            if (!HasErrors)
            {
                Output(schemaInfo);
            }
            return !HasErrors;
        }

        private void Prepare()
        {
            foreach (XmlSchemaElement? element in _schema!.Elements.Values)
            {
                if (!element!.SubstitutionGroup.IsEmpty)
                {
                    XmlSchemaSubstitutionGroup? substitutionGroup = (XmlSchemaSubstitutionGroup?)_examplars[element.SubstitutionGroup];
                    if (substitutionGroup == null)
                    {
                        substitutionGroup = new XmlSchemaSubstitutionGroupV1Compat();
                        substitutionGroup.Examplar = element.SubstitutionGroup;
                        _examplars.Add(element.SubstitutionGroup, substitutionGroup);
                    }

                    ArrayList members = substitutionGroup.Members;
                    Debug.Assert(!members.Contains(element));
                    members.Add(element);
                }
            }
        }

        private void Cleanup()
        {
            foreach (XmlSchemaGroup? group in _schema!.Groups.Values)
            {
                CleanupGroup(group!);
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in _schema.AttributeGroups.Values)
            {
                CleanupAttributeGroup(attributeGroup!);
            }

            foreach (XmlSchemaType? type in _schema.SchemaTypes.Values)
            {
                if (type is XmlSchemaComplexType)
                {
                    CleanupComplexType((XmlSchemaComplexType)type);
                }
                else
                {
                    CleanupSimpleType((XmlSchemaSimpleType)type!);
                }
            }

            foreach (XmlSchemaElement? element in _schema.Elements.Values)
            {
                CleanupElement(element!);
            }

            foreach (XmlSchemaAttribute? attribute in _schema.Attributes.Values)
            {
                CleanupAttribute(attribute!);
            }
        }

        internal static void Cleanup(XmlSchema schema)
        {
            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                if (include.Schema != null)
                {
                    Cleanup(include.Schema);
                }
                XmlSchemaRedefine? rdef = include as XmlSchemaRedefine;
                if (rdef != null)
                {
                    rdef.AttributeGroups.Clear();
                    rdef.Groups.Clear();
                    rdef.SchemaTypes.Clear();

                    for (int j = 0; j < rdef.Items.Count; ++j)
                    {
                        object? item = rdef.Items[j];
                        XmlSchemaAttribute? attr;
                        XmlSchemaAttributeGroup? attrgroup;
                        XmlSchemaComplexType? complextype;
                        XmlSchemaSimpleType? simpletype;
                        XmlSchemaElement? element;
                        XmlSchemaGroup? group;

                        if ((attr = item as XmlSchemaAttribute) != null)
                        {
                            CleanupAttribute(attr);
                        }
                        else if ((attrgroup = item as XmlSchemaAttributeGroup) != null)
                        {
                            CleanupAttributeGroup(attrgroup);
                        }
                        else if ((complextype = item as XmlSchemaComplexType) != null)
                        {
                            CleanupComplexType(complextype);
                        }
                        else if ((simpletype = item as XmlSchemaSimpleType) != null)
                        {
                            CleanupSimpleType(simpletype);
                        }
                        else if ((element = item as XmlSchemaElement) != null)
                        {
                            CleanupElement(element);
                        }
                        else if ((group = item as XmlSchemaGroup) != null)
                        {
                            CleanupGroup(group);
                        }
                    }
                }
            }

            for (int i = 0; i < schema.Items.Count; ++i)
            {
                object item = schema.Items[i];
                XmlSchemaAttribute? attr;
                XmlSchemaAttributeGroup? attrgroup;
                XmlSchemaComplexType? complextype;
                XmlSchemaSimpleType? simpletype;
                XmlSchemaElement? element;
                XmlSchemaGroup? group;

                if ((attr = item as XmlSchemaAttribute) != null)
                {
                    CleanupAttribute(attr);
                }
                else if ((attrgroup = schema.Items[i] as XmlSchemaAttributeGroup) != null)
                {
                    CleanupAttributeGroup(attrgroup);
                }
                else if ((complextype = schema.Items[i] as XmlSchemaComplexType) != null)
                {
                    CleanupComplexType(complextype);
                }
                else if ((simpletype = schema.Items[i] as XmlSchemaSimpleType) != null)
                {
                    CleanupSimpleType(simpletype);
                }
                else if ((element = schema.Items[i] as XmlSchemaElement) != null)
                {
                    CleanupElement(element);
                }
                else if ((group = schema.Items[i] as XmlSchemaGroup) != null)
                {
                    CleanupGroup(group);
                }
            }

            schema.Attributes.Clear();
            schema.AttributeGroups.Clear();
            schema.SchemaTypes.Clear();
            schema.Elements.Clear();
            schema.Groups.Clear();
            schema.Notations.Clear();
            schema.Ids.Clear();
            schema.IdentityConstraints.Clear();
        }

        private void Compile()
        {
            _schema!.SchemaTypes.Insert(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);

            foreach (XmlSchemaSubstitutionGroupV1Compat? substitutionGroup in _examplars.Values)
            {
                CompileSubstitutionGroup(substitutionGroup!);
            }

            foreach (XmlSchemaGroup? group in _schema.Groups.Values)
            {
                CompileGroup(group!);
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in _schema.AttributeGroups.Values)
            {
                CompileAttributeGroup(attributeGroup!);
            }

            foreach (XmlSchemaType? type in _schema.SchemaTypes.Values)
            {
                if (type is XmlSchemaComplexType)
                {
                    CompileComplexType((XmlSchemaComplexType)type);
                }
                else
                {
                    CompileSimpleType((XmlSchemaSimpleType)type!);
                }
            }
            foreach (XmlSchemaElement? element in _schema.Elements.Values)
            {
                if (element!.ElementDecl == null)
                {
                    CompileElement(element);
                }
            }

            foreach (XmlSchemaAttribute? attribute in _schema.Attributes.Values)
            {
                if (attribute!.AttDef == null)
                {
                    CompileAttribute(attribute);
                }
            }

            foreach (XmlSchemaIdentityConstraint? identityConstraint in _schema.IdentityConstraints.Values)
            {
                if (identityConstraint!.CompiledConstraint == null)
                {
                    CompileIdentityConstraint(identityConstraint);
                }
            }

            while (_complexTypeStack.Count > 0)
            {
                XmlSchemaComplexType type = _complexTypeStack.Pop();
                CompileCompexTypeElements(type);
            }

            foreach (XmlSchemaType? type in _schema.SchemaTypes.Values)
            {
                if (type is XmlSchemaComplexType)
                {
                    CheckParticleDerivation((XmlSchemaComplexType)type);
                }
            }

            foreach (XmlSchemaElement? element in _schema.Elements.Values)
            {
                if (element!.ElementSchemaType is XmlSchemaComplexType && element.SchemaTypeName == XmlQualifiedName.Empty)
                { // only local schemaTypes
                    CheckParticleDerivation((XmlSchemaComplexType)element.ElementSchemaType);
                }
            }

            foreach (XmlSchemaSubstitutionGroup? substitutionGroup in _examplars.Values)
            {
                CheckSubstitutionGroup(substitutionGroup!);
            }

            _schema.SchemaTypes.Remove(DatatypeImplementation.QnAnyType); //For backward compatibility
        }

        private void Output(SchemaInfo schemaInfo)
        {
            foreach (XmlSchemaElement? element in _schema!.Elements.Values)
            {
                schemaInfo.TargetNamespaces[element!.QualifiedName.Namespace] = true;
                schemaInfo.ElementDecls.Add(element.QualifiedName, element.ElementDecl!);
            }

            foreach (XmlSchemaAttribute? attribute in _schema.Attributes.Values)
            {
                schemaInfo.TargetNamespaces[attribute!.QualifiedName.Namespace] = true;
                schemaInfo.AttributeDecls.Add(attribute.QualifiedName, attribute.AttDef!);
            }

            foreach (XmlSchemaType? type in _schema.SchemaTypes.Values)
            {
                schemaInfo.TargetNamespaces[type!.QualifiedName.Namespace] = true;
                XmlSchemaComplexType? complexType = type as XmlSchemaComplexType;
                if (complexType == null || (!complexType.IsAbstract && type != XmlSchemaComplexType.AnyType))
                {
                    schemaInfo.ElementDeclsByType.Add(type.QualifiedName, type.ElementDecl!);
                }
            }
            foreach (XmlSchemaNotation? notation in _schema.Notations.Values)
            {
                schemaInfo.TargetNamespaces[notation!.QualifiedName.Namespace] = true;
                SchemaNotation no = new SchemaNotation(notation.QualifiedName);
                no.SystemLiteral = notation.System;
                no.Pubid = notation.Public;
                if (!schemaInfo.Notations.ContainsKey(no.Name.Name))
                {
                    schemaInfo.Notations.Add(no.Name.Name, no);
                }
            }
        }

        private static void CleanupAttribute(XmlSchemaAttribute attribute)
        {
            if (attribute.SchemaType != null)
            {
                CleanupSimpleType((XmlSchemaSimpleType)attribute.SchemaType);
            }
            attribute.AttDef = null;
        }

        private static void CleanupAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
        {
            CleanupAttributes(attributeGroup.Attributes);
            attributeGroup.AttributeUses.Clear();
            attributeGroup.AttributeWildcard = null;
        }

        private static void CleanupComplexType(XmlSchemaComplexType complexType)
        {
            if (complexType.ContentModel != null)
            { //simpleContent or complexContent
                if (complexType.ContentModel is XmlSchemaSimpleContent)
                {
                    XmlSchemaSimpleContent simpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
                    if (simpleContent.Content is XmlSchemaSimpleContentExtension)
                    {
                        XmlSchemaSimpleContentExtension simpleExtension = (XmlSchemaSimpleContentExtension)simpleContent.Content;
                        CleanupAttributes(simpleExtension.Attributes);
                    }
                    else
                    { //simpleContent.Content is XmlSchemaSimpleContentRestriction
                        XmlSchemaSimpleContentRestriction simpleRestriction = (XmlSchemaSimpleContentRestriction)simpleContent.Content!;
                        CleanupAttributes(simpleRestriction.Attributes);
                    }
                }
                else
                { // complexType.ContentModel is XmlSchemaComplexContent
                    XmlSchemaComplexContent complexContent = (XmlSchemaComplexContent)complexType.ContentModel;
                    if (complexContent.Content is XmlSchemaComplexContentExtension)
                    {
                        XmlSchemaComplexContentExtension complexExtension = (XmlSchemaComplexContentExtension)complexContent.Content!;
                        CleanupParticle(complexExtension.Particle!);
                        CleanupAttributes(complexExtension.Attributes);
                    }
                    else
                    { //XmlSchemaComplexContentRestriction
                        XmlSchemaComplexContentRestriction complexRestriction = (XmlSchemaComplexContentRestriction)complexContent.Content!;
                        CleanupParticle(complexRestriction.Particle!);
                        CleanupAttributes(complexRestriction.Attributes);
                    }
                }
            }
            else
            { //equals XmlSchemaComplexContent with baseType is anyType
                CleanupParticle(complexType.Particle!);
                CleanupAttributes(complexType.Attributes);
            }
            complexType.LocalElements.Clear();
            complexType.AttributeUses.Clear();
            complexType.SetAttributeWildcard(null);
            complexType.SetContentTypeParticle(XmlSchemaParticle.Empty);
            complexType.ElementDecl = null;
        }

        private static void CleanupSimpleType(XmlSchemaSimpleType simpleType)
        {
            simpleType.ElementDecl = null;
        }

        private static void CleanupElement(XmlSchemaElement element)
        {
            if (element.SchemaType != null)
            {
                XmlSchemaComplexType? complexType = element.SchemaType as XmlSchemaComplexType;
                if (complexType != null)
                {
                    CleanupComplexType(complexType);
                }
                else
                {
                    CleanupSimpleType((XmlSchemaSimpleType)element.SchemaType);
                }
            }
            for (int i = 0; i < element.Constraints.Count; ++i)
            {
                ((XmlSchemaIdentityConstraint)element.Constraints[i]).CompiledConstraint = null;
            }
            element.ElementDecl = null;
        }

        private static void CleanupAttributes(XmlSchemaObjectCollection attributes)
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                XmlSchemaAttribute? attribute = attributes[i] as XmlSchemaAttribute;
                if (attribute != null)
                {
                    CleanupAttribute(attribute);
                }
            }
        }

        private static void CleanupGroup(XmlSchemaGroup group)
        {
            CleanupParticle(group.Particle!);
            group.CanonicalParticle = null;
        }

        private static void CleanupParticle(XmlSchemaParticle particle)
        {
            if (particle is XmlSchemaElement)
            {
                CleanupElement((XmlSchemaElement)particle);
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaObjectCollection particles = ((XmlSchemaGroupBase)particle).Items;
                for (int i = 0; i < particles.Count; ++i)
                {
                    CleanupParticle((XmlSchemaParticle)particles[i]);
                }
            }
        }

        private void CompileSubstitutionGroup(XmlSchemaSubstitutionGroupV1Compat substitutionGroup)
        {
            if (substitutionGroup.IsProcessing && substitutionGroup.Members.Count > 0)
            {
                SendValidationEvent(SR.Sch_SubstitutionCircularRef, (XmlSchemaElement)substitutionGroup.Members[0]!);
                return;
            }

            XmlSchemaElement? examplar = (XmlSchemaElement?)_schema!.Elements[substitutionGroup.Examplar];
            if (substitutionGroup.Members.Contains(examplar))
            {// already checked
                return;
            }

            substitutionGroup.IsProcessing = true;
            if (examplar != null)
            {
                if (examplar.FinalResolved == XmlSchemaDerivationMethod.All)
                {
                    SendValidationEvent(SR.Sch_InvalidExamplar, examplar.Name, examplar);
                }

                for (int i = 0; i < substitutionGroup.Members.Count; ++i)
                {
                    XmlSchemaElement element = (XmlSchemaElement)substitutionGroup.Members[i]!;
                    //Chain to other head's that are members of this head's substGroup
                    XmlSchemaSubstitutionGroupV1Compat? g = (XmlSchemaSubstitutionGroupV1Compat?)_examplars[element!.QualifiedName];
                    if (g != null)
                    {
                        CompileSubstitutionGroup(g);
                        for (int j = 0; j < g.Choice.Items.Count; ++j)
                        {
                            substitutionGroup.Choice.Items.Add(g.Choice.Items[j]);
                        }
                    }
                    else
                    {
                        substitutionGroup.Choice.Items.Add(element);
                    }
                }
                substitutionGroup.Choice.Items.Add(examplar);
                substitutionGroup.Members.Add(examplar); // Compiled mark
            }
            else if (substitutionGroup.Members.Count > 0)
            {
                SendValidationEvent(SR.Sch_NoExamplar, (XmlSchemaElement)substitutionGroup.Members[0]!);
            }
            substitutionGroup.IsProcessing = false;
        }

        private void CheckSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup)
        {
            XmlSchemaElement? examplar = (XmlSchemaElement?)_schema!.Elements[substitutionGroup.Examplar];
            if (examplar != null)
            {
                for (int i = 0; i < substitutionGroup.Members.Count; ++i)
                {
                    XmlSchemaElement? element = (XmlSchemaElement?)substitutionGroup.Members[i];

                    if (element != examplar)
                    {
                        if (!XmlSchemaType.IsDerivedFrom(element!.ElementSchemaType, examplar.ElementSchemaType, examplar.FinalResolved))
                        {
                            SendValidationEvent(SR.Sch_InvalidSubstitutionMember, (element.QualifiedName).ToString(), (examplar.QualifiedName).ToString(), element);
                        }
                    }
                }
            }
        }

        private void CompileGroup(XmlSchemaGroup group)
        {
            if (group.IsProcessing)
            {
                SendValidationEvent(SR.Sch_GroupCircularRef, group);
                group.CanonicalParticle = XmlSchemaParticle.Empty;
            }
            else
            {
                group.IsProcessing = true;
                if (group.CanonicalParticle == null)
                {
                    group.CanonicalParticle = CannonicalizeParticle(group.Particle, true, true);
                }
                Debug.Assert(group.CanonicalParticle != null);
                group.IsProcessing = false;
            }
        }

        private void CompileSimpleType(XmlSchemaSimpleType simpleType)
        {
            if (simpleType.IsProcessing)
            {
                throw new XmlSchemaException(SR.Sch_TypeCircularRef, simpleType);
            }
            if (simpleType.ElementDecl != null)
            { // already compiled
                return;
            }
            simpleType.IsProcessing = true;
            try
            {
                if (simpleType.Content is XmlSchemaSimpleTypeList)
                {
                    XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)simpleType.Content;
                    XmlSchemaDatatype datatype;
                    simpleType.SetBaseSchemaType(DatatypeImplementation.AnySimpleType);
                    if (list.ItemTypeName.IsEmpty)
                    {
                        CompileSimpleType(list.ItemType!);
                        list.BaseItemType = list.ItemType;
                        datatype = list.ItemType!.Datatype!;
                    }
                    else
                    {
                        XmlSchemaSimpleType? type = GetSimpleType(list.ItemTypeName);
                        if (type != null)
                        {
                            if ((type.FinalResolved & XmlSchemaDerivationMethod.List) != 0)
                            {
                                SendValidationEvent(SR.Sch_BaseFinalList, simpleType);
                            }
                            list.BaseItemType = type;
                            datatype = type.Datatype!;
                        }
                        else
                        {
                            throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, list.ItemTypeName.ToString(), simpleType);
                        }
                    }
                    simpleType.SetDatatype(datatype.DeriveByList(simpleType));
                    simpleType.SetDerivedBy(XmlSchemaDerivationMethod.List);
                }
                else if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
                {
                    XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
                    XmlSchemaDatatype datatype;
                    if (restriction.BaseTypeName.IsEmpty)
                    {
                        CompileSimpleType(restriction.BaseType!);
                        simpleType.SetBaseSchemaType(restriction.BaseType);
                        datatype = restriction.BaseType!.Datatype!;
                    }
                    else if (simpleType.Redefined != null && restriction.BaseTypeName == simpleType.Redefined.QualifiedName)
                    {
                        CompileSimpleType((XmlSchemaSimpleType)simpleType.Redefined);
                        simpleType.SetBaseSchemaType(simpleType.Redefined.BaseXmlSchemaType);
                        datatype = simpleType.Redefined.Datatype!;
                    }
                    else
                    {
                        if (restriction.BaseTypeName.Equals(DatatypeImplementation.QnAnySimpleType))
                        {
                            throw new XmlSchemaException(SR.Sch_InvalidSimpleTypeRestriction, restriction.BaseTypeName.ToString(), simpleType);
                        }
                        XmlSchemaSimpleType? type = GetSimpleType(restriction.BaseTypeName);
                        if (type != null)
                        {
                            if ((type.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
                            {
                                SendValidationEvent(SR.Sch_BaseFinalRestriction, simpleType);
                            }
                            simpleType.SetBaseSchemaType(type);
                            datatype = type.Datatype!;
                        }
                        else
                        {
                            throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, restriction.BaseTypeName.ToString(), simpleType);
                        }
                    }

                    simpleType.SetDatatype(datatype.DeriveByRestriction(restriction.Facets, NameTable, simpleType));
                    simpleType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
                }
                else
                { //simpleType.Content is XmlSchemaSimpleTypeUnion
                    XmlSchemaSimpleType[] baseTypes = CompileBaseMemberTypes(simpleType);
                    simpleType.SetBaseSchemaType(DatatypeImplementation.AnySimpleType);
                    simpleType.SetDatatype(XmlSchemaDatatype.DeriveByUnion(baseTypes, simpleType));
                    simpleType.SetDerivedBy(XmlSchemaDerivationMethod.Union);
                }
            }
            catch (XmlSchemaException e)
            {
                if (e.SourceSchemaObject == null)
                {
                    e.SetSource(simpleType);
                }
                SendValidationEvent(e);
                simpleType.SetDatatype(DatatypeImplementation.AnySimpleType.Datatype);
            }
            finally
            {
                SchemaElementDecl decl = new SchemaElementDecl();
                decl.ContentValidator = ContentValidator.TextOnly;
                decl.SchemaType = simpleType;
                decl.Datatype = simpleType.Datatype!;
                simpleType.ElementDecl = decl;
                simpleType.IsProcessing = false;
            }
        }

        private XmlSchemaSimpleType[] CompileBaseMemberTypes(XmlSchemaSimpleType simpleType)
        {
            XmlSchemaSimpleType? unionMember;
            var memberTypeDefinitions = new List<XmlSchemaSimpleType>();

            XmlSchemaSimpleTypeUnion mainUnion = (XmlSchemaSimpleTypeUnion)simpleType.Content!;

            XmlQualifiedName[]? mainMemberTypes = mainUnion.MemberTypes;
            if (mainMemberTypes != null)
            {
                for (int i = 0; i < mainMemberTypes.Length; ++i)
                {
                    unionMember = GetSimpleType(mainMemberTypes[i]);
                    if (unionMember != null)
                    {
                        if (unionMember.Datatype!.Variety == XmlSchemaDatatypeVariety.Union)
                        { //union of union
                            CheckUnionType(unionMember, memberTypeDefinitions, simpleType);
                        }
                        else
                        { //its list or atomic
                            memberTypeDefinitions.Add(unionMember);
                        }
                        //Check derivation method of the member that is referenced
                        if ((unionMember.FinalResolved & XmlSchemaDerivationMethod.Union) != 0)
                        {
                            SendValidationEvent(SR.Sch_BaseFinalUnion, simpleType);
                        }
                    }
                    else
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, mainMemberTypes[i].ToString(), simpleType);
                    }
                }
            }

            //Now add the baseTypes that are defined inside the union itself
            XmlSchemaObjectCollection mainBaseTypes = mainUnion.BaseTypes;
            if (mainBaseTypes != null)
            {
                for (int i = 0; i < mainBaseTypes.Count; ++i)
                {
                    XmlSchemaSimpleType st = (XmlSchemaSimpleType)mainBaseTypes[i];
                    CompileSimpleType(st);
                    if (st.Datatype!.Variety == XmlSchemaDatatypeVariety.Union)
                    { //union of union
                        CheckUnionType(st, memberTypeDefinitions, simpleType);
                    }
                    else
                    {  //its list or atomic
                        memberTypeDefinitions.Add(st);
                    }
                }
            }

            //set all types
            mainUnion.SetBaseMemberTypes(memberTypeDefinitions.ToArray());
            return mainUnion.BaseMemberTypes!;
        }

        private void CheckUnionType(XmlSchemaSimpleType unionMember, List<XmlSchemaSimpleType> memberTypeDefinitions, XmlSchemaSimpleType parentType)
        {
            XmlSchemaDatatype unionDatatype = unionMember.Datatype!;
            if (unionMember.DerivedBy == XmlSchemaDerivationMethod.Restriction && (unionDatatype.HasLexicalFacets || unionDatatype.HasValueFacets))
            {
                SendValidationEvent(SR.Sch_UnionFromUnion, parentType);
            }
            else
            {
                Datatype_union uniondt = (unionMember.Datatype as Datatype_union)!;
                memberTypeDefinitions.AddRange(uniondt.BaseMemberTypes);
            }
        }

        private void CompileComplexType(XmlSchemaComplexType complexType)
        {
            if (complexType.ElementDecl != null)
            { //already compiled
                return;
            }
            if (complexType.IsProcessing)
            {
                SendValidationEvent(SR.Sch_TypeCircularRef, complexType);
                return;
            }
            complexType.IsProcessing = true;
            if (complexType.ContentModel != null)
            { //simpleContent or complexContent
                if (complexType.ContentModel is XmlSchemaSimpleContent)
                {
                    XmlSchemaSimpleContent simpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
                    complexType.SetContentType(XmlSchemaContentType.TextOnly);
                    if (simpleContent.Content is XmlSchemaSimpleContentExtension)
                    {
                        CompileSimpleContentExtension(complexType, (XmlSchemaSimpleContentExtension)simpleContent.Content);
                    }
                    else
                    { //simpleContent.Content is XmlSchemaSimpleContentRestriction
                        CompileSimpleContentRestriction(complexType, (XmlSchemaSimpleContentRestriction)simpleContent.Content!);
                    }
                }
                else
                { // complexType.ContentModel is XmlSchemaComplexContent
                    XmlSchemaComplexContent complexContent = (XmlSchemaComplexContent)complexType.ContentModel;
                    if (complexContent.Content is XmlSchemaComplexContentExtension)
                    {
                        CompileComplexContentExtension(complexType, complexContent, (XmlSchemaComplexContentExtension)complexContent.Content);
                    }
                    else
                    { // complexContent.Content is XmlSchemaComplexContentRestriction
                        CompileComplexContentRestriction(complexType, complexContent, (XmlSchemaComplexContentRestriction)complexContent.Content!);
                    }
                }
            }
            else
            { //equals XmlSchemaComplexContent with baseType is anyType
                complexType.SetBaseSchemaType(XmlSchemaComplexType.AnyType);
                CompileLocalAttributes(XmlSchemaComplexType.AnyType, complexType, complexType.Attributes, complexType.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
                complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
                complexType.SetContentTypeParticle(CompileContentTypeParticle(complexType.Particle, true));
                complexType.SetContentType(GetSchemaContentType(complexType, null, complexType.ContentTypeParticle));
            }
            bool hasID = false;
            foreach (XmlSchemaAttribute? attribute in complexType.AttributeUses.Values)
            {
                if (attribute!.Use != XmlSchemaUse.Prohibited)
                {
                    XmlSchemaDatatype? datatype = attribute.Datatype;
                    if (datatype != null && datatype.TokenizedType == XmlTokenizedType.ID)
                    {
                        if (hasID)
                        {
                            SendValidationEvent(SR.Sch_TwoIdAttrUses, complexType);
                        }
                        else
                        {
                            hasID = true;
                        }
                    }
                }
            }
            SchemaElementDecl decl = new SchemaElementDecl();
            decl.ContentValidator = CompileComplexContent(complexType);
            decl.SchemaType = complexType;
            decl.IsAbstract = complexType.IsAbstract;
            decl.Datatype = complexType.Datatype!;
            decl.Block = complexType.BlockResolved;
            decl.AnyAttribute = complexType.AttributeWildcard;
            foreach (XmlSchemaAttribute? attribute in complexType.AttributeUses.Values)
            {
                if (attribute!.Use == XmlSchemaUse.Prohibited)
                {
                    if (!decl.ProhibitedAttributes.ContainsKey(attribute.QualifiedName))
                    {
                        decl.ProhibitedAttributes.Add(attribute.QualifiedName, attribute.QualifiedName);
                    }
                }
                else
                {
                    if (!decl.AttDefs.ContainsKey(attribute.QualifiedName) && attribute.AttDef != null && attribute.AttDef.Name != XmlQualifiedName.Empty && attribute.AttDef != SchemaAttDef.Empty)
                    {
                        decl.AddAttDef(attribute.AttDef);
                    }
                }
            }

            complexType.ElementDecl = decl;

            complexType.IsProcessing = false;
        }


        private void CompileSimpleContentExtension(XmlSchemaComplexType complexType, XmlSchemaSimpleContentExtension simpleExtension)
        {
            XmlSchemaComplexType? baseType = null;
            if (complexType.Redefined != null && simpleExtension.BaseTypeName == complexType.Redefined.QualifiedName)
            {
                baseType = (XmlSchemaComplexType)complexType.Redefined;
                CompileComplexType(baseType);
                complexType.SetBaseSchemaType(baseType);
                complexType.SetDatatype(baseType.Datatype);
            }
            else
            {
                XmlSchemaType? bto = GetAnySchemaType(simpleExtension.BaseTypeName);
                if (bto == null)
                {
                    SendValidationEvent(SR.Sch_UndeclaredType, simpleExtension.BaseTypeName.ToString(), complexType);
                }
                else
                {
                    complexType.SetBaseSchemaType(bto);
                    complexType.SetDatatype(bto.Datatype);
                }
                baseType = bto as XmlSchemaComplexType;
            }
            if (baseType != null)
            {
                if ((baseType.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
                {
                    SendValidationEvent(SR.Sch_BaseFinalExtension, complexType);
                }
                if (baseType.ContentType != XmlSchemaContentType.TextOnly)
                {
                    SendValidationEvent(SR.Sch_NotSimpleContent, complexType);
                }
            }
            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Extension);
            CompileLocalAttributes(baseType, complexType, simpleExtension.Attributes, simpleExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);
        }

        private void CompileSimpleContentRestriction(XmlSchemaComplexType complexType, XmlSchemaSimpleContentRestriction simpleRestriction)
        {
            XmlSchemaComplexType? baseType = null;
            XmlSchemaDatatype? datatype = null;
            if (complexType.Redefined != null && simpleRestriction.BaseTypeName == complexType.Redefined.QualifiedName)
            {
                baseType = (XmlSchemaComplexType)complexType.Redefined;
                CompileComplexType(baseType);
                datatype = baseType.Datatype;
            }
            else
            {
                baseType = GetComplexType(simpleRestriction.BaseTypeName);
                if (baseType == null)
                {
                    SendValidationEvent(SR.Sch_UndefBaseRestriction, simpleRestriction.BaseTypeName.ToString(), simpleRestriction);
                    return;
                }
                if (baseType.ContentType == XmlSchemaContentType.TextOnly)
                {
                    if (simpleRestriction.BaseType == null)
                    {
                        datatype = baseType.Datatype;
                        //There is a bug here. Need to check if simpleRestriction has facets.
                        //If yes, Need tp apply these facets as well.
                    }
                    else
                    {
                        CompileSimpleType(simpleRestriction.BaseType);
                        if (!XmlSchemaType.IsDerivedFromDatatype(simpleRestriction.BaseType.Datatype!, baseType.Datatype!, XmlSchemaDerivationMethod.None))
                        {
                            SendValidationEvent(SR.Sch_DerivedNotFromBase, simpleRestriction);
                        }
                        datatype = simpleRestriction.BaseType.Datatype;
                    }
                }
                else if (baseType.ContentType == XmlSchemaContentType.Mixed && baseType.ElementDecl!.ContentValidator!.IsEmptiable)
                {
                    if (simpleRestriction.BaseType != null)
                    {
                        CompileSimpleType(simpleRestriction.BaseType);
                        complexType.SetBaseSchemaType(simpleRestriction.BaseType);
                        datatype = simpleRestriction.BaseType.Datatype;
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_NeedSimpleTypeChild, simpleRestriction);
                    }
                }
                else
                {
                    SendValidationEvent(SR.Sch_NotSimpleContent, complexType);
                }
            }
            if (baseType != null && baseType.ElementDecl != null)
            {
                if ((baseType.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
                {
                    SendValidationEvent(SR.Sch_BaseFinalRestriction, complexType);
                }
            }
            if (baseType != null)
            {
                complexType.SetBaseSchemaType(baseType);
            }
            if (datatype != null)
            {
                try
                {
                    complexType.SetDatatype(datatype.DeriveByRestriction(simpleRestriction.Facets, NameTable, complexType));
                }
                catch (XmlSchemaException e)
                {
                    if (e.SourceSchemaObject == null)
                    {
                        e.SetSource(complexType);
                    }
                    SendValidationEvent(e);
                    complexType.SetDatatype(DatatypeImplementation.AnySimpleType.Datatype);
                }
            }
            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
            CompileLocalAttributes(baseType, complexType, simpleRestriction.Attributes, simpleRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
        }

        private void CompileComplexContentExtension(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaComplexContentExtension complexExtension)
        {
            XmlSchemaComplexType? baseType = null;
            if (complexType.Redefined != null && complexExtension.BaseTypeName == complexType.Redefined.QualifiedName)
            {
                baseType = (XmlSchemaComplexType)complexType.Redefined;
                CompileComplexType(baseType);
            }
            else
            {
                baseType = GetComplexType(complexExtension.BaseTypeName);
                if (baseType == null)
                {
                    SendValidationEvent(SR.Sch_UndefBaseExtension, complexExtension.BaseTypeName.ToString(), complexExtension);
                    return;
                }
            }
            if (baseType != null && baseType.ElementDecl != null)
            {
                if (baseType.ContentType == XmlSchemaContentType.TextOnly)
                {
                    SendValidationEvent(SR.Sch_NotComplexContent, complexType);
                    return;
                }
            }
            complexType.SetBaseSchemaType(baseType);
            if ((baseType!.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
            {
                SendValidationEvent(SR.Sch_BaseFinalExtension, complexType);
            }
            CompileLocalAttributes(baseType, complexType, complexExtension.Attributes, complexExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);

            XmlSchemaParticle baseParticle = baseType.ContentTypeParticle;
            XmlSchemaParticle extendedParticle = CannonicalizeParticle(complexExtension.Particle, true, true);
            if (baseParticle != XmlSchemaParticle.Empty)
            {
                if (extendedParticle != XmlSchemaParticle.Empty)
                {
                    XmlSchemaSequence compiledParticle = new XmlSchemaSequence();
                    compiledParticle.Items.Add(baseParticle);
                    compiledParticle.Items.Add(extendedParticle);
                    complexType.SetContentTypeParticle(CompileContentTypeParticle(compiledParticle, false));
                }
                else
                {
                    complexType.SetContentTypeParticle(baseParticle);
                }
                XmlSchemaContentType contentType = GetSchemaContentType(complexType, complexContent, extendedParticle);
                if (contentType == XmlSchemaContentType.Empty)
                { //Derived content type is empty, Get ContentType from base
                    contentType = baseType.ContentType;
                }
                complexType.SetContentType(contentType);
                if (complexType.ContentType != baseType.ContentType)
                {
                    SendValidationEvent(SR.Sch_DifContentType, complexType);
                }
            }
            else
            {
                complexType.SetContentTypeParticle(extendedParticle);
                complexType.SetContentType(GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle));
            }
            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Extension);
        }

        private void CompileComplexContentRestriction(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaComplexContentRestriction complexRestriction)
        {
            XmlSchemaComplexType? baseType = null;
            if (complexType.Redefined != null && complexRestriction.BaseTypeName == complexType.Redefined.QualifiedName)
            {
                baseType = (XmlSchemaComplexType)complexType.Redefined;
                CompileComplexType(baseType);
            }
            else
            {
                baseType = GetComplexType(complexRestriction.BaseTypeName);
                if (baseType == null)
                {
                    SendValidationEvent(SR.Sch_UndefBaseRestriction, complexRestriction.BaseTypeName.ToString(), complexRestriction);
                    return;
                }
            }
            if (baseType != null && baseType.ElementDecl != null)
            {
                if (baseType.ContentType == XmlSchemaContentType.TextOnly)
                {
                    SendValidationEvent(SR.Sch_NotComplexContent, complexType);
                    return;
                }
            }
            complexType.SetBaseSchemaType(baseType);
            if ((baseType!.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
            {
                SendValidationEvent(SR.Sch_BaseFinalRestriction, complexType);
            }

            CompileLocalAttributes(baseType, complexType, complexRestriction.Attributes, complexRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);

            complexType.SetContentTypeParticle(CompileContentTypeParticle(complexRestriction.Particle, true));
            complexType.SetContentType(GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle));
            if (complexType.ContentType == XmlSchemaContentType.Empty)
            {
                if (baseType.ElementDecl != null)
                {
                    Debug.Assert(baseType.ElementDecl.ContentValidator != null);
                }
                if (baseType.ElementDecl != null && !baseType.ElementDecl.ContentValidator!.IsEmptiable)
                {
                    SendValidationEvent(SR.Sch_InvalidContentRestriction, complexType);
                }
            }

            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
        }

        private void CheckParticleDerivation(XmlSchemaComplexType complexType)
        {
            XmlSchemaComplexType? baseType = complexType.BaseXmlSchemaType as XmlSchemaComplexType;
            if (baseType != null && baseType != XmlSchemaComplexType.AnyType && complexType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
            {
                if (!IsValidRestriction(complexType.ContentTypeParticle, baseType.ContentTypeParticle))
                {
                    SendValidationEvent(SR.Sch_InvalidParticleRestriction, complexType);
                }
            }
        }

        private XmlSchemaParticle CompileContentTypeParticle(XmlSchemaParticle? particle, bool substitution)
        {
            XmlSchemaParticle ctp = CannonicalizeParticle(particle, true, substitution);
            XmlSchemaChoice? choice = ctp as XmlSchemaChoice;
            if (choice != null && choice.Items.Count == 0)
            {
                if (choice.MinOccurs != decimal.Zero)
                {
                    SendValidationEvent(SR.Sch_EmptyChoice, choice, XmlSeverityType.Warning);
                }
                return XmlSchemaParticle.Empty;
            }
            return ctp;
        }

        private XmlSchemaParticle CannonicalizeParticle(XmlSchemaParticle? particle, bool root, bool substitution)
        {
            if (particle == null || particle.IsEmpty)
            {
                return XmlSchemaParticle.Empty;
            }
            else if (particle is XmlSchemaElement)
            {
                return CannonicalizeElement((XmlSchemaElement)particle, substitution);
            }
            else if (particle is XmlSchemaGroupRef)
            {
                return CannonicalizeGroupRef((XmlSchemaGroupRef)particle, root, substitution);
            }
            else if (particle is XmlSchemaAll)
            {
                return CannonicalizeAll((XmlSchemaAll)particle, root, substitution);
            }
            else if (particle is XmlSchemaChoice)
            {
                return CannonicalizeChoice((XmlSchemaChoice)particle, root, substitution);
            }
            else if (particle is XmlSchemaSequence)
            {
                return CannonicalizeSequence((XmlSchemaSequence)particle, root, substitution);
            }
            else
            {
                return particle;
            }
        }

        private XmlSchemaParticle CannonicalizeElement(XmlSchemaElement element, bool substitution)
        {
            if (!element.RefName.IsEmpty && substitution && (element.BlockResolved & XmlSchemaDerivationMethod.Substitution) == 0)
            {
                XmlSchemaSubstitutionGroupV1Compat? substitutionGroup = (XmlSchemaSubstitutionGroupV1Compat?)_examplars[element.QualifiedName];
                if (substitutionGroup == null)
                {
                    return element;
                }
                else
                {
                    XmlSchemaChoice choice = (XmlSchemaChoice)substitutionGroup.Choice.Clone();
                    choice.MinOccurs = element.MinOccurs;
                    choice.MaxOccurs = element.MaxOccurs;
                    return choice;
                }
            }
            else
            {
                return element;
            }
        }

        private XmlSchemaParticle CannonicalizeGroupRef(XmlSchemaGroupRef groupRef, bool root, bool substitution)
        {
            XmlSchemaGroup? group;
            if (groupRef.Redefined != null)
            {
                group = groupRef.Redefined;
            }
            else
            {
                group = (XmlSchemaGroup?)_schema!.Groups[groupRef.RefName];
            }
            if (group == null)
            {
                SendValidationEvent(SR.Sch_UndefGroupRef, groupRef.RefName.ToString(), groupRef);
                return XmlSchemaParticle.Empty;
            }
            if (group.CanonicalParticle == null)
            {
                CompileGroup(group);
            }
            if (group.CanonicalParticle == XmlSchemaParticle.Empty)
            {
                return XmlSchemaParticle.Empty;
            }
            XmlSchemaGroupBase? groupBase = (XmlSchemaGroupBase?)group.CanonicalParticle;
            if (groupBase is XmlSchemaAll)
            {
                if (!root)
                {
                    SendValidationEvent(SR.Sch_AllRefNotRoot, "", groupRef);
                    return XmlSchemaParticle.Empty;
                }
                if (groupRef.MinOccurs != decimal.One || groupRef.MaxOccurs != decimal.One)
                {
                    SendValidationEvent(SR.Sch_AllRefMinMax, groupRef);
                    return XmlSchemaParticle.Empty;
                }
            }
            else if (groupBase is XmlSchemaChoice && groupBase.Items.Count == 0)
            {
                if (groupRef.MinOccurs != decimal.Zero)
                {
                    SendValidationEvent(SR.Sch_EmptyChoice, groupRef, XmlSeverityType.Warning);
                }
                return XmlSchemaParticle.Empty;
            }
            XmlSchemaGroupBase groupRefBase = (
                (groupBase is XmlSchemaSequence) ? (XmlSchemaGroupBase)new XmlSchemaSequence() :
                (groupBase is XmlSchemaChoice) ? (XmlSchemaGroupBase)new XmlSchemaChoice() :
                                                   (XmlSchemaGroupBase)new XmlSchemaAll()
            );
            groupRefBase.MinOccurs = groupRef.MinOccurs;
            groupRefBase.MaxOccurs = groupRef.MaxOccurs;
            for (int i = 0; i < groupBase!.Items.Count; ++i)
            {
                groupRefBase.Items.Add((XmlSchemaParticle)groupBase.Items[i]);
            }
            groupRef.SetParticle(groupRefBase);
            return groupRefBase;
        }

        private XmlSchemaParticle CannonicalizeAll(XmlSchemaAll all, bool root, bool substitution)
        {
            if (all.Items.Count > 0)
            {
                XmlSchemaAll newAll = new XmlSchemaAll();
                newAll.MinOccurs = all.MinOccurs;
                newAll.MaxOccurs = all.MaxOccurs;
                newAll.SourceUri = all.SourceUri; // all is the only one that might need and error message
                newAll.LineNumber = all.LineNumber;
                newAll.LinePosition = all.LinePosition;
                for (int i = 0; i < all.Items.Count; ++i)
                {
                    XmlSchemaParticle p = CannonicalizeParticle((XmlSchemaElement)all.Items[i], false, substitution);
                    if (p != XmlSchemaParticle.Empty)
                    {
                        newAll.Items.Add(p);
                    }
                }
                all = newAll;
            }
            if (all.Items.Count == 0)
            {
                return XmlSchemaParticle.Empty;
            }
            else if (root && all.Items.Count == 1)
            {
                XmlSchemaSequence newSequence = new XmlSchemaSequence();
                newSequence.MinOccurs = all.MinOccurs;
                newSequence.MaxOccurs = all.MaxOccurs;
                newSequence.Items.Add((XmlSchemaParticle)all.Items[0]);
                return newSequence;
            }
            else if (!root && all.Items.Count == 1 && all.MinOccurs == decimal.One && all.MaxOccurs == decimal.One)
            {
                return (XmlSchemaParticle)all.Items[0];
            }
            else if (!root)
            {
                SendValidationEvent(SR.Sch_NotAllAlone, all);
                return XmlSchemaParticle.Empty;
            }
            else
            {
                return all;
            }
        }

        private XmlSchemaParticle CannonicalizeChoice(XmlSchemaChoice choice, bool root, bool substitution)
        {
            XmlSchemaChoice oldChoice = choice;
            if (choice.Items.Count > 0)
            {
                XmlSchemaChoice newChoice = new XmlSchemaChoice();
                newChoice.MinOccurs = choice.MinOccurs;
                newChoice.MaxOccurs = choice.MaxOccurs;
                for (int i = 0; i < choice.Items.Count; ++i)
                {
                    XmlSchemaParticle p1 = CannonicalizeParticle((XmlSchemaParticle)choice.Items[i], false, substitution);
                    if (p1 != XmlSchemaParticle.Empty)
                    {
                        if (p1.MinOccurs == decimal.One && p1.MaxOccurs == decimal.One && p1 is XmlSchemaChoice)
                        {
                            XmlSchemaChoice particleChoice = (XmlSchemaChoice)p1;
                            for (int j = 0; j < particleChoice.Items.Count; ++j)
                            {
                                newChoice.Items.Add(particleChoice.Items[j]);
                            }
                        }
                        else
                        {
                            newChoice.Items.Add(p1);
                        }
                    }
                }
                choice = newChoice;
            }
            if (!root && choice.Items.Count == 0)
            {
                if (choice.MinOccurs != decimal.Zero)
                {
                    SendValidationEvent(SR.Sch_EmptyChoice, oldChoice, XmlSeverityType.Warning);
                }
                return XmlSchemaParticle.Empty;
            }
            else if (!root && choice.Items.Count == 1 && choice.MinOccurs == decimal.One && choice.MaxOccurs == decimal.One)
            {
                return (XmlSchemaParticle)choice.Items[0];
            }
            else
            {
                return choice;
            }
        }

        private XmlSchemaParticle CannonicalizeSequence(XmlSchemaSequence sequence, bool root, bool substitution)
        {
            if (sequence.Items.Count > 0)
            {
                XmlSchemaSequence newSequence = new XmlSchemaSequence();
                newSequence.MinOccurs = sequence.MinOccurs;
                newSequence.MaxOccurs = sequence.MaxOccurs;
                for (int i = 0; i < sequence.Items.Count; ++i)
                {
                    XmlSchemaParticle p1 = CannonicalizeParticle((XmlSchemaParticle)sequence.Items[i], false, substitution);
                    if (p1 != XmlSchemaParticle.Empty)
                    {
                        if (p1.MinOccurs == decimal.One && p1.MaxOccurs == decimal.One && p1 is XmlSchemaSequence)
                        {
                            XmlSchemaSequence particleSequence = (XmlSchemaSequence)p1;
                            for (int j = 0; j < particleSequence.Items.Count; ++j)
                            {
                                newSequence.Items.Add(particleSequence.Items[j]);
                            }
                        }
                        else
                        {
                            newSequence.Items.Add(p1);
                        }
                    }
                }
                sequence = newSequence;
            }
            if (sequence.Items.Count == 0)
            {
                return XmlSchemaParticle.Empty;
            }
            else if (!root && sequence.Items.Count == 1 && sequence.MinOccurs == decimal.One && sequence.MaxOccurs == decimal.One)
            {
                return (XmlSchemaParticle)sequence.Items[0];
            }
            else
            {
                return sequence;
            }
        }

        private bool IsValidRestriction(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
        {
            if (derivedParticle == baseParticle)
            {
                return true;
            }
            else if (derivedParticle == null || derivedParticle == XmlSchemaParticle.Empty)
            {
                return IsParticleEmptiable(baseParticle);
            }
            else if (baseParticle == null || baseParticle == XmlSchemaParticle.Empty)
            {
                return false;
            }
            if (baseParticle is XmlSchemaElement)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromElement((XmlSchemaElement)derivedParticle, (XmlSchemaElement)baseParticle);
                }
                else
                {
                    return false;
                }
            }
            else if (baseParticle is XmlSchemaAny)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromAny((XmlSchemaElement)derivedParticle, (XmlSchemaAny)baseParticle);
                }
                else if (derivedParticle is XmlSchemaAny)
                {
                    return IsAnyFromAny((XmlSchemaAny)derivedParticle, (XmlSchemaAny)baseParticle);
                }
                else
                {
                    return IsGroupBaseFromAny((XmlSchemaGroupBase)derivedParticle, (XmlSchemaAny)baseParticle);
                }
            }
            else if (baseParticle is XmlSchemaAll)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, true);
                }
                else if (derivedParticle is XmlSchemaAll)
                {
                    return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, true);
                }
                else if (derivedParticle is XmlSchemaSequence)
                {
                    return IsSequenceFromAll((XmlSchemaSequence)derivedParticle, (XmlSchemaAll)baseParticle);
                }
            }
            else if (baseParticle is XmlSchemaChoice)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, false);
                }
                else if (derivedParticle is XmlSchemaChoice)
                {
                    return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, false);
                }
                else if (derivedParticle is XmlSchemaSequence)
                {
                    return IsSequenceFromChoice((XmlSchemaSequence)derivedParticle, (XmlSchemaChoice)baseParticle);
                }
            }
            else if (baseParticle is XmlSchemaSequence)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, true);
                }
                else if (derivedParticle is XmlSchemaSequence)
                {
                    return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, true);
                }
            }
            else
            {
                Debug.Fail("Unexpected particle");
            }

            return false;
        }

        private bool IsElementFromElement(XmlSchemaElement derivedElement, XmlSchemaElement baseElement)
        {
            return (derivedElement.QualifiedName == baseElement.QualifiedName) &&
                    (derivedElement.IsNillable == baseElement.IsNillable) &&
                    IsValidOccurrenceRangeRestriction(derivedElement, baseElement) &&
                    (baseElement.FixedValue == null || baseElement.FixedValue == derivedElement.FixedValue) &&
                    ((derivedElement.BlockResolved | baseElement.BlockResolved) == derivedElement.BlockResolved) &&
                    (derivedElement.ElementSchemaType != null) && (baseElement.ElementSchemaType != null) &&
                    XmlSchemaType.IsDerivedFrom(derivedElement.ElementSchemaType, baseElement.ElementSchemaType, ~XmlSchemaDerivationMethod.Restriction);
        }

        private bool IsElementFromAny(XmlSchemaElement derivedElement, XmlSchemaAny baseAny)
        {
            return baseAny.Allows(derivedElement.QualifiedName) &&
                IsValidOccurrenceRangeRestriction(derivedElement, baseAny);
        }

        private bool IsAnyFromAny(XmlSchemaAny derivedAny, XmlSchemaAny baseAny)
        {
            return IsValidOccurrenceRangeRestriction(derivedAny, baseAny) &&
                NamespaceList.IsSubset(derivedAny.NamespaceList!, baseAny.NamespaceList!);
        }

        private bool IsGroupBaseFromAny(XmlSchemaGroupBase derivedGroupBase, XmlSchemaAny baseAny)
        {
            decimal minOccurs, maxOccurs;
            CalculateEffectiveTotalRange(derivedGroupBase, out minOccurs, out maxOccurs);
            if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseAny.MinOccurs, baseAny.MaxOccurs))
            {
                return false;
            }
            // eliminate occurrence range check
            string? minOccursAny = baseAny.MinOccursString;
            baseAny.MinOccurs = decimal.Zero;

            for (int i = 0; i < derivedGroupBase.Items.Count; ++i)
            {
                if (!IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[i], baseAny))
                {
                    baseAny.MinOccursString = minOccursAny;
                    return false;
                }
            }
            baseAny.MinOccursString = minOccursAny;
            return true;
        }

        private bool IsElementFromGroupBase(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
        {
            bool isMatched = false;
            for (int i = 0; i < baseGroupBase.Items.Count; ++i)
            {
                XmlSchemaParticle baseParticle = (XmlSchemaParticle)baseGroupBase.Items[i];
                if (!isMatched)
                {
                    string? minOccursElement = baseParticle.MinOccursString;
                    string? maxOccursElement = baseParticle.MaxOccursString;
                    baseParticle.MinOccurs *= baseGroupBase.MinOccurs;
                    if (baseParticle.MaxOccurs != decimal.MaxValue)
                    {
                        if (baseGroupBase.MaxOccurs == decimal.MaxValue)
                            baseParticle.MaxOccurs = decimal.MaxValue;
                        else
                            baseParticle.MaxOccurs *= baseGroupBase.MaxOccurs;
                    }
                    isMatched = IsValidRestriction(derivedElement, baseParticle);
                    baseParticle.MinOccursString = minOccursElement;
                    baseParticle.MaxOccursString = maxOccursElement;
                }
                else if (skipEmptableOnly && !IsParticleEmptiable(baseParticle))
                {
                    return false;
                }
            }
            return isMatched;
        }

        private bool IsGroupBaseFromGroupBase(XmlSchemaGroupBase derivedGroupBase, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
        {
            if (!IsValidOccurrenceRangeRestriction(derivedGroupBase, baseGroupBase) || derivedGroupBase.Items.Count > baseGroupBase.Items.Count)
            {
                return false;
            }
            int count = 0;
            for (int i = 0; i < baseGroupBase.Items.Count; ++i)
            {
                XmlSchemaParticle baseParticle = (XmlSchemaParticle)baseGroupBase.Items[i];
                if ((count < derivedGroupBase.Items.Count) && IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[count], baseParticle))
                {
                    count++;
                }
                else if (skipEmptableOnly && !IsParticleEmptiable(baseParticle))
                {
                    return false;
                }
            }
            if (count < derivedGroupBase.Items.Count)
            {
                return false;
            }
            return true;
        }

        private bool IsSequenceFromAll(XmlSchemaSequence derivedSequence, XmlSchemaAll baseAll)
        {
            if (!IsValidOccurrenceRangeRestriction(derivedSequence, baseAll) || derivedSequence.Items.Count > baseAll.Items.Count)
            {
                return false;
            }
            BitSet map = new BitSet(baseAll.Items.Count);
            for (int j = 0; j < derivedSequence.Items.Count; ++j)
            {
                int i = GetMappingParticle((XmlSchemaParticle)derivedSequence.Items[j], baseAll.Items);
                if (i >= 0)
                {
                    if (map[i])
                    {
                        return false;
                    }
                    else
                    {
                        map.Set(i);
                    }
                }
                else
                {
                    return false;
                }
            }
            for (int i = 0; i < baseAll.Items.Count; i++)
            {
                if (!map[i] && !IsParticleEmptiable((XmlSchemaParticle)baseAll.Items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsSequenceFromChoice(XmlSchemaSequence derivedSequence, XmlSchemaChoice baseChoice)
        {
            decimal minOccurs, maxOccurs;
            CalculateSequenceRange(derivedSequence, out minOccurs, out maxOccurs);
            if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseChoice.MinOccurs, baseChoice.MaxOccurs))
            {
                return false;
            }
            for (int i = 0; i < derivedSequence.Items.Count; ++i)
            {
                if (GetMappingParticle((XmlSchemaParticle)derivedSequence.Items[i], baseChoice.Items) < 0)
                    return false;
            }
            return true;
        }

        private void CalculateSequenceRange(XmlSchemaSequence sequence, out decimal minOccurs, out decimal maxOccurs)
        {
            minOccurs = decimal.Zero; maxOccurs = decimal.Zero;
            for (int i = 0; i < sequence.Items.Count; ++i)
            {
                XmlSchemaParticle p = (XmlSchemaParticle)sequence.Items[i];

                minOccurs += p.MinOccurs;
                if (p.MaxOccurs == decimal.MaxValue)
                    maxOccurs = decimal.MaxValue;
                else if (maxOccurs != decimal.MaxValue)
                    maxOccurs += p.MaxOccurs;
            }
            minOccurs *= sequence.MinOccurs;
            if (sequence.MaxOccurs == decimal.MaxValue)
            {
                maxOccurs = decimal.MaxValue;
            }
            else if (maxOccurs != decimal.MaxValue)
            {
                maxOccurs *= sequence.MaxOccurs;
            }
        }

        private bool IsValidOccurrenceRangeRestriction(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
        {
            return IsValidOccurrenceRangeRestriction(derivedParticle.MinOccurs, derivedParticle.MaxOccurs, baseParticle.MinOccurs, baseParticle.MaxOccurs);
        }

        private bool IsValidOccurrenceRangeRestriction(decimal minOccurs, decimal maxOccurs, decimal baseMinOccurs, decimal baseMaxOccurs)
        {
            return (baseMinOccurs <= minOccurs) && (maxOccurs <= baseMaxOccurs);
        }

        private int GetMappingParticle(XmlSchemaParticle particle, XmlSchemaObjectCollection collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (IsValidRestriction(particle, (XmlSchemaParticle)collection[i]))
                    return i;
            }
            return -1;
        }

        private bool IsParticleEmptiable(XmlSchemaParticle particle)
        {
            decimal minOccurs, maxOccurs;
            CalculateEffectiveTotalRange(particle, out minOccurs, out maxOccurs);
            return minOccurs == decimal.Zero;
        }

        private void CalculateEffectiveTotalRange(XmlSchemaParticle particle, out decimal minOccurs, out decimal maxOccurs)
        {
            if (particle is XmlSchemaElement || particle is XmlSchemaAny)
            {
                minOccurs = particle.MinOccurs;
                maxOccurs = particle.MaxOccurs;
            }
            else if (particle is XmlSchemaChoice)
            {
                if (((XmlSchemaChoice)particle).Items.Count == 0)
                {
                    minOccurs = maxOccurs = decimal.Zero;
                }
                else
                {
                    minOccurs = decimal.MaxValue;
                    maxOccurs = decimal.Zero;
                    XmlSchemaChoice choice = (XmlSchemaChoice)particle;
                    for (int i = 0; i < choice.Items.Count; ++i)
                    {
                        decimal min, max;
                        CalculateEffectiveTotalRange((XmlSchemaParticle)choice.Items[i], out min, out max);
                        if (min < minOccurs)
                        {
                            minOccurs = min;
                        }
                        if (max > maxOccurs)
                        {
                            maxOccurs = max;
                        }
                    }
                    minOccurs *= particle.MinOccurs;
                    if (maxOccurs != decimal.MaxValue)
                    {
                        if (particle.MaxOccurs == decimal.MaxValue)
                            maxOccurs = decimal.MaxValue;
                        else
                            maxOccurs *= particle.MaxOccurs;
                    }
                }
            }
            else
            {
                XmlSchemaObjectCollection collection = ((XmlSchemaGroupBase)particle).Items;
                if (collection.Count == 0)
                {
                    minOccurs = maxOccurs = decimal.Zero;
                }
                else
                {
                    minOccurs = 0;
                    maxOccurs = 0;
                    for (int i = 0; i < collection.Count; ++i)
                    {
                        decimal min, max;
                        CalculateEffectiveTotalRange((XmlSchemaParticle)collection[i], out min, out max);
                        minOccurs += min;
                        if (maxOccurs != decimal.MaxValue)
                        {
                            if (max == decimal.MaxValue)
                                maxOccurs = decimal.MaxValue;
                            else
                                maxOccurs += max;
                        }
                    }
                    minOccurs *= particle.MinOccurs;
                    if (maxOccurs != decimal.MaxValue)
                    {
                        if (particle.MaxOccurs == decimal.MaxValue)
                            maxOccurs = decimal.MaxValue;
                        else
                            maxOccurs *= particle.MaxOccurs;
                    }
                }
            }
        }

        private void PushComplexType(XmlSchemaComplexType complexType)
        {
            _complexTypeStack.Push(complexType);
        }

        private XmlSchemaContentType GetSchemaContentType(XmlSchemaComplexType complexType, XmlSchemaComplexContent? complexContent, XmlSchemaParticle particle)
        {
            if ((complexContent != null && complexContent.IsMixed) ||
                (complexContent == null && complexType.IsMixed))
            {
                return XmlSchemaContentType.Mixed;
            }
            else if (particle != null && !particle.IsEmpty)
            {
                return XmlSchemaContentType.ElementOnly;
            }
            else
            {
                return XmlSchemaContentType.Empty;
            }
        }

        private void CompileAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
        {
            if (attributeGroup.IsProcessing)
            {
                SendValidationEvent(SR.Sch_AttributeGroupCircularRef, attributeGroup);
                return;
            }
            if (attributeGroup.AttributeUses.Count > 0)
            {// already checked
                return;
            }
            attributeGroup.IsProcessing = true;
            XmlSchemaAnyAttribute? anyAttribute = attributeGroup.AnyAttribute;
            for (int i = 0; i < attributeGroup.Attributes.Count; ++i)
            {
                XmlSchemaAttribute? attribute = attributeGroup.Attributes[i] as XmlSchemaAttribute;
                if (attribute != null)
                {
                    if (attribute.Use != XmlSchemaUse.Prohibited)
                    {
                        CompileAttribute(attribute);
                    }
                    if (attributeGroup.AttributeUses[attribute.QualifiedName] == null)
                    {
                        attributeGroup.AttributeUses.Add(attribute.QualifiedName, attribute);
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_DupAttributeUse, attribute.QualifiedName.ToString(), attribute);
                    }
                }
                else
                { // XmlSchemaAttributeGroupRef
                    XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)attributeGroup.Attributes[i];
                    XmlSchemaAttributeGroup? attributeGroupResolved;
                    if (attributeGroup.Redefined != null && attributeGroupRef.RefName == attributeGroup.Redefined.QualifiedName)
                    {
                        attributeGroupResolved = (XmlSchemaAttributeGroup)attributeGroup.Redefined;
                    }
                    else
                    {
                        attributeGroupResolved = (XmlSchemaAttributeGroup?)_schema!.AttributeGroups[attributeGroupRef.RefName];
                    }
                    if (attributeGroupResolved != null)
                    {
                        CompileAttributeGroup(attributeGroupResolved);
                        foreach (XmlSchemaAttribute? attributeValue in attributeGroupResolved.AttributeUses.Values)
                        {
                            if (attributeGroup.AttributeUses[attributeValue!.QualifiedName] == null)
                            {
                                attributeGroup.AttributeUses.Add(attributeValue.QualifiedName, attributeValue);
                            }
                            else
                            {
                                SendValidationEvent(SR.Sch_DupAttributeUse, attributeValue.QualifiedName.ToString(), attributeValue);
                            }
                        }

                        anyAttribute = CompileAnyAttributeIntersection(anyAttribute, attributeGroupResolved.AttributeWildcard);
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_UndefAttributeGroupRef, attributeGroupRef.RefName.ToString(), attributeGroupRef);
                    }
                }
            }
            attributeGroup.AttributeWildcard = anyAttribute;
            attributeGroup.IsProcessing = false;
        }

        private void CompileLocalAttributes(XmlSchemaComplexType? baseType, XmlSchemaComplexType derivedType, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute? anyAttribute, XmlSchemaDerivationMethod derivedBy)
        {
            XmlSchemaAnyAttribute? baseAttributeWildcard = baseType != null ? baseType.AttributeWildcard : null;
            for (int i = 0; i < attributes.Count; ++i)
            {
                XmlSchemaAttribute? attribute = attributes[i] as XmlSchemaAttribute;
                if (attribute != null)
                {
                    if (attribute.Use != XmlSchemaUse.Prohibited)
                    {
                        CompileAttribute(attribute);
                    }
                    if (attribute.Use != XmlSchemaUse.Prohibited ||
                        (attribute.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
                    {
                        if (derivedType.AttributeUses[attribute.QualifiedName] == null)
                        {
                            derivedType.AttributeUses.Add(attribute.QualifiedName, attribute);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_DupAttributeUse, attribute.QualifiedName.ToString(), attribute);
                        }
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_AttributeIgnored, attribute.QualifiedName.ToString(), attribute, XmlSeverityType.Warning);
                    }
                }
                else
                { // is XmlSchemaAttributeGroupRef
                    XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
                    XmlSchemaAttributeGroup? attributeGroup = (XmlSchemaAttributeGroup?)_schema!.AttributeGroups[attributeGroupRef.RefName];
                    if (attributeGroup != null)
                    {
                        CompileAttributeGroup(attributeGroup);
                        foreach (XmlSchemaAttribute? attributeValue in attributeGroup.AttributeUses.Values)
                        {
                            if (attributeValue!.Use != XmlSchemaUse.Prohibited ||
                               (attributeValue.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
                            {
                                if (derivedType.AttributeUses[attributeValue.QualifiedName] == null)
                                {
                                    derivedType.AttributeUses.Add(attributeValue.QualifiedName, attributeValue);
                                }
                                else
                                {
                                    SendValidationEvent(SR.Sch_DupAttributeUse, attributeValue.QualifiedName.ToString(), attributeGroupRef);
                                }
                            }
                            else
                            {
                                SendValidationEvent(SR.Sch_AttributeIgnored, attributeValue.QualifiedName.ToString(), attributeValue, XmlSeverityType.Warning);
                            }
                        }
                        anyAttribute = CompileAnyAttributeIntersection(anyAttribute, attributeGroup.AttributeWildcard);
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_UndefAttributeGroupRef, attributeGroupRef.RefName.ToString(), attributeGroupRef);
                    }
                }
            }

            // check derivation rules
            if (baseType != null)
            {
                if (derivedBy == XmlSchemaDerivationMethod.Extension)
                {
                    derivedType.SetAttributeWildcard(CompileAnyAttributeUnion(anyAttribute, baseAttributeWildcard));
                    foreach (XmlSchemaAttribute? attributeBase in baseType.AttributeUses.Values)
                    {
                        XmlSchemaAttribute? attribute = (XmlSchemaAttribute?)derivedType.AttributeUses[attributeBase!.QualifiedName];
                        if (attribute != null)
                        {
                            Debug.Assert(attribute.Use != XmlSchemaUse.Prohibited);
                            if (attribute.AttributeSchemaType != attributeBase.AttributeSchemaType || attributeBase.Use == XmlSchemaUse.Prohibited)
                            {
                                SendValidationEvent(SR.Sch_InvalidAttributeExtension, attribute);
                            }
                        }
                        else
                        {
                            derivedType.AttributeUses.Add(attributeBase.QualifiedName, attributeBase);
                        }
                    }
                }
                else
                {  // derivedBy == XmlSchemaDerivationMethod.Restriction
                    // Schema Component Constraint: Derivation Valid (Restriction, Complex)
                    if ((anyAttribute != null) && (baseAttributeWildcard == null || !XmlSchemaAnyAttribute.IsSubset(anyAttribute, baseAttributeWildcard)))
                    {
                        SendValidationEvent(SR.Sch_InvalidAnyAttributeRestriction, derivedType);
                    }
                    else
                    {
                        derivedType.SetAttributeWildcard(anyAttribute); //complete wildcard
                    }

                    // Add form the base
                    foreach (XmlSchemaAttribute? attributeBase in baseType.AttributeUses.Values)
                    {
                        XmlSchemaAttribute? attribute = (XmlSchemaAttribute?)derivedType.AttributeUses[attributeBase!.QualifiedName];
                        if (attribute == null)
                        {
                            derivedType.AttributeUses.Add(attributeBase.QualifiedName, attributeBase);
                        }
                        else
                        {
                            if (attributeBase.Use == XmlSchemaUse.Prohibited && attribute.Use != XmlSchemaUse.Prohibited)
                            {
                                SendValidationEvent(SR.Sch_AttributeRestrictionProhibited, attribute);
                            }
                            else if (attribute.Use == XmlSchemaUse.Prohibited)
                            {
                                continue;
                            }
                            else if (attributeBase.AttributeSchemaType == null || attribute.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(attribute.AttributeSchemaType, attributeBase.AttributeSchemaType, XmlSchemaDerivationMethod.Empty))
                            {
                                SendValidationEvent(SR.Sch_AttributeRestrictionInvalid, attribute);
                            }
                        }
                    }

                    // Check additional ones are valid restriction of base's wildcard
                    foreach (XmlSchemaAttribute? attribute in derivedType.AttributeUses.Values)
                    {
                        XmlSchemaAttribute? attributeBase = (XmlSchemaAttribute?)baseType.AttributeUses[attribute!.QualifiedName];
                        if (attributeBase != null)
                        {
                            continue;
                        }

                        if (baseAttributeWildcard == null || !baseAttributeWildcard.Allows(attribute.QualifiedName))
                        {
                            SendValidationEvent(SR.Sch_AttributeRestrictionInvalidFromWildcard, attribute);
                        }
                    }
                }
            }
            else
            {
                derivedType.SetAttributeWildcard(anyAttribute);
            }
        }

        private XmlSchemaAnyAttribute? CompileAnyAttributeUnion(XmlSchemaAnyAttribute? a, XmlSchemaAnyAttribute? b)
        {
            if (a == null)
            {
                return b;
            }
            else if (b == null)
            {
                return a;
            }
            else
            {
                XmlSchemaAnyAttribute? attribute = XmlSchemaAnyAttribute.Union(a, b, true); //true for v1Compat
                if (attribute == null)
                {
                    SendValidationEvent(SR.Sch_UnexpressibleAnyAttribute, a);
                }
                return attribute;
            }
        }

        private XmlSchemaAnyAttribute? CompileAnyAttributeIntersection(XmlSchemaAnyAttribute? a, XmlSchemaAnyAttribute? b)
        {
            if (a == null)
            {
                return b;
            }
            else if (b == null)
            {
                return a;
            }
            else
            {
                XmlSchemaAnyAttribute? attribute = XmlSchemaAnyAttribute.Intersection(a, b, true); //true for v1Compat
                if (attribute == null)
                {
                    SendValidationEvent(SR.Sch_UnexpressibleAnyAttribute, a);
                }
                return attribute;
            }
        }

        private void CompileAttribute(XmlSchemaAttribute xa)
        {
            if (xa.IsProcessing)
            {
                SendValidationEvent(SR.Sch_AttributeCircularRef, xa);
                return;
            }
            if (xa.AttDef != null)
            { //already compiled?
                return;
            }
            xa.IsProcessing = true;
            SchemaAttDef? decl = null;
            try
            {
                if (!xa.RefName.IsEmpty)
                {
                    XmlSchemaAttribute? a = (XmlSchemaAttribute?)_schema!.Attributes[xa.RefName];
                    if (a == null)
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredAttribute, xa.RefName.ToString(), xa);
                    }
                    CompileAttribute(a);
                    if (a.AttDef == null)
                    {
                        throw new XmlSchemaException(SR.Sch_RefInvalidAttribute, xa.RefName.ToString(), xa);
                    }
                    decl = a.AttDef.Clone();
                    if (decl.Datatype != null)
                    {
                        if (a.FixedValue != null)
                        {
                            if (xa.DefaultValue != null)
                            {
                                throw new XmlSchemaException(SR.Sch_FixedDefaultInRef, xa.RefName.ToString(), xa);
                            }
                            else if (xa.FixedValue != null)
                            {
                                if (xa.FixedValue != a.FixedValue)
                                {
                                    throw new XmlSchemaException(SR.Sch_FixedInRef, xa.RefName.ToString(), xa);
                                }
                            }
                            else
                            {
                                decl.Presence = SchemaDeclBase.Use.Fixed;
                                decl.DefaultValueRaw = decl.DefaultValueExpanded = a.FixedValue;
                                decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xa), true);
                            }
                        }
                        else if (a.DefaultValue != null)
                        {
                            if (xa.DefaultValue == null && xa.FixedValue == null)
                            {
                                decl.Presence = SchemaDeclBase.Use.Default;
                                decl.DefaultValueRaw = decl.DefaultValueExpanded = a.DefaultValue;
                                decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xa), true);
                            }
                        }
                    }
                    xa.SetAttributeType(a.AttributeSchemaType);
                }
                else
                {
                    decl = new SchemaAttDef(xa.QualifiedName);
                    if (xa.SchemaType != null)
                    {
                        CompileSimpleType(xa.SchemaType);
                        xa.SetAttributeType(xa.SchemaType);
                        decl.SchemaType = xa.SchemaType;
                        decl.Datatype = xa.SchemaType.Datatype!;
                    }
                    else if (!xa.SchemaTypeName.IsEmpty)
                    {
                        XmlSchemaSimpleType? simpleType = GetSimpleType(xa.SchemaTypeName);
                        if (simpleType != null)
                        {
                            xa.SetAttributeType(simpleType);
                            decl.Datatype = simpleType.Datatype!;
                            decl.SchemaType = simpleType;
                        }
                        else
                        {
                            throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, xa.SchemaTypeName.ToString(), xa);
                        }
                    }
                    else
                    {
                        decl.SchemaType = DatatypeImplementation.AnySimpleType;
                        decl.Datatype = DatatypeImplementation.AnySimpleType.Datatype!;
                        xa.SetAttributeType(DatatypeImplementation.AnySimpleType);
                    }
                }
                if (decl.Datatype != null)
                {
                    decl.Datatype.VerifySchemaValid(_schema!.Notations, xa);
                }
                if (xa.DefaultValue != null || xa.FixedValue != null)
                {
                    if (xa.DefaultValue != null)
                    {
                        decl.Presence = SchemaDeclBase.Use.Default;
                        decl.DefaultValueRaw = decl.DefaultValueExpanded = xa.DefaultValue;
                    }
                    else
                    {
                        decl.Presence = SchemaDeclBase.Use.Fixed;
                        decl.DefaultValueRaw = decl.DefaultValueExpanded = xa.FixedValue;
                    }
                    if (decl.Datatype != null)
                    {
                        decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xa), true);
                    }
                }
                else
                {
                    switch (xa.Use)
                    {
                        case XmlSchemaUse.None:
                        case XmlSchemaUse.Optional:
                            decl.Presence = SchemaDeclBase.Use.Implied;
                            break;
                        case XmlSchemaUse.Required:
                            decl.Presence = SchemaDeclBase.Use.Required;
                            break;
                        case XmlSchemaUse.Prohibited:
                            break;
                    }
                }
                decl.SchemaAttribute = xa; //So this is available for PSVI
                xa.AttDef = decl;
            }
            catch (XmlSchemaException e)
            {
                if (e.SourceSchemaObject == null)
                {
                    e.SetSource(xa);
                }
                SendValidationEvent(e);
                xa.AttDef = SchemaAttDef.Empty;
            }
            finally
            {
                xa.IsProcessing = false;
            }
        }

        private void CompileIdentityConstraint(XmlSchemaIdentityConstraint xi)
        {
            if (xi.IsProcessing)
            {
                xi.CompiledConstraint = CompiledIdentityConstraint.Empty;
                SendValidationEvent(SR.Sch_IdentityConstraintCircularRef, xi);
                return;
            }

            if (xi.CompiledConstraint != null)
            {
                return;
            }

            xi.IsProcessing = true;
            CompiledIdentityConstraint? compic = null;
            try
            {
                SchemaNamespaceManager xnmgr = new SchemaNamespaceManager(xi);
                compic = new CompiledIdentityConstraint(xi, xnmgr);
                if (xi is XmlSchemaKeyref)
                {
                    XmlSchemaIdentityConstraint? ic = (XmlSchemaIdentityConstraint?)_schema!.IdentityConstraints[((XmlSchemaKeyref)xi).Refer];
                    if (ic == null)
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredIdentityConstraint, ((XmlSchemaKeyref)xi).Refer.ToString(), xi);
                    }
                    CompileIdentityConstraint(ic);
                    if (ic.CompiledConstraint == null)
                    {
                        throw new XmlSchemaException(SR.Sch_RefInvalidIdentityConstraint, ((XmlSchemaKeyref)xi).Refer.ToString(), xi);
                    }
                    // keyref has the different cardinality with the key it referred
                    if (ic.Fields.Count != xi.Fields.Count)
                    {
                        throw new XmlSchemaException(SR.Sch_RefInvalidCardin, xi.QualifiedName.ToString(), xi);
                    }
                    // keyref can only refer to key/unique
                    if (ic.CompiledConstraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref)
                    {
                        throw new XmlSchemaException(SR.Sch_ReftoKeyref, xi.QualifiedName.ToString(), xi);
                    }
                }
                xi.CompiledConstraint = compic;
            }
            catch (XmlSchemaException e)
            {
                if (e.SourceSchemaObject == null)
                {
                    e.SetSource(xi);
                }
                SendValidationEvent(e);
                xi.CompiledConstraint = CompiledIdentityConstraint.Empty;
                // empty is better than null here, stop quickly when circle referencing
            }
            finally
            {
                xi.IsProcessing = false;
            }
        }

        private void CompileElement(XmlSchemaElement xe)
        {
            if (xe.IsProcessing)
            {
                SendValidationEvent(SR.Sch_ElementCircularRef, xe);
                return;
            }
            if (xe.ElementDecl != null)
            {
                return;
            }
            xe.IsProcessing = true;
            SchemaElementDecl? decl = null;
            try
            {
                if (!xe.RefName.IsEmpty)
                {
                    XmlSchemaElement? e = (XmlSchemaElement?)_schema!.Elements[xe.RefName];
                    if (e == null)
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredElement, xe.RefName.ToString(), xe);
                    }
                    CompileElement(e);
                    if (e.ElementDecl == null)
                    {
                        throw new XmlSchemaException(SR.Sch_RefInvalidElement, xe.RefName.ToString(), xe);
                    }
                    xe.SetElementType(e.ElementSchemaType);
                    decl = e.ElementDecl.Clone();
                }
                else
                {
                    if (xe.SchemaType != null)
                    {
                        xe.SetElementType(xe.SchemaType);
                    }
                    else if (!xe.SchemaTypeName.IsEmpty)
                    {
                        xe.SetElementType(GetAnySchemaType(xe.SchemaTypeName));
                        if (xe.ElementSchemaType == null)
                        {
                            throw new XmlSchemaException(SR.Sch_UndeclaredType, xe.SchemaTypeName.ToString(), xe);
                        }
                    }
                    else if (!xe.SubstitutionGroup.IsEmpty)
                    {
                        XmlSchemaElement? examplar = (XmlSchemaElement?)_schema!.Elements[xe.SubstitutionGroup];
                        if (examplar == null)
                        {
                            throw new XmlSchemaException(SR.Sch_UndeclaredEquivClass, xe.SubstitutionGroup.Name, xe);
                        }
                        if (examplar.IsProcessing)
                        { //Circular subst group; already detected by now
                            return;
                        }
                        CompileElement(examplar);
                        if (examplar.ElementDecl == null)
                        { //If head is invalid, fall back to AnyType
                            xe.SetElementType(XmlSchemaComplexType.AnyType);
                            decl = XmlSchemaComplexType.AnyType.ElementDecl!.Clone();
                        }
                        else
                        {
                            xe.SetElementType(examplar.ElementSchemaType);
                            decl = examplar.ElementDecl.Clone();
                        }
                    }
                    else
                    {
                        xe.SetElementType(XmlSchemaComplexType.AnyType);
                        decl = XmlSchemaComplexType.AnyType.ElementDecl!.Clone();
                    }

                    if (decl == null)
                    {
                        Debug.Assert(xe.ElementSchemaType != null);
                        if (xe.ElementSchemaType is XmlSchemaComplexType)
                        {
                            XmlSchemaComplexType complexType = (XmlSchemaComplexType)xe.ElementSchemaType;
                            CompileComplexType(complexType);
                            if (complexType.ElementDecl != null)
                            {
                                decl = complexType.ElementDecl.Clone();
                                //                                decl.LocalElements = complexType.LocalElementDecls;
                            }
                        }
                        else if (xe.ElementSchemaType is XmlSchemaSimpleType)
                        {
                            XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)xe.ElementSchemaType;
                            CompileSimpleType(simpleType);
                            if (simpleType.ElementDecl != null)
                            {
                                decl = simpleType.ElementDecl.Clone();
                            }
                        }
                    }

                    decl!.Name = xe.QualifiedName;
                    decl.IsAbstract = xe.IsAbstract;
                    XmlSchemaComplexType? ct = xe.ElementSchemaType as XmlSchemaComplexType;
                    if (ct != null)
                    {
                        decl.IsAbstract |= ct.IsAbstract;
                    }
                    decl.IsNillable = xe.IsNillable;
                    decl.Block |= xe.BlockResolved;
                }
                if (decl.Datatype != null)
                {
                    decl.Datatype.VerifySchemaValid(_schema!.Notations, xe);
                }

                if (xe.DefaultValue != null || xe.FixedValue != null)
                {
                    if (decl.ContentValidator != null)
                    {
                        if (decl.ContentValidator.ContentType == XmlSchemaContentType.TextOnly)
                        {
                            if (xe.DefaultValue != null)
                            {
                                decl.Presence = SchemaDeclBase.Use.Default;
                                decl.DefaultValueRaw = xe.DefaultValue;
                            }
                            else
                            {
                                decl.Presence = SchemaDeclBase.Use.Fixed;
                                decl.DefaultValueRaw = xe.FixedValue!;
                            }
                            if (decl.Datatype != null)
                            {
                                decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xe), true);
                            }
                        }
                        else if (decl.ContentValidator.ContentType != XmlSchemaContentType.Mixed || !decl.ContentValidator.IsEmptiable)
                        {
                            throw new XmlSchemaException(SR.Sch_ElementCannotHaveValue, xe);
                        }
                    }
                }
                if (xe.HasConstraints)
                {
                    XmlSchemaObjectCollection constraints = xe.Constraints;
                    CompiledIdentityConstraint[] compiledConstraints = new CompiledIdentityConstraint[constraints.Count];
                    int idx = 0;
                    for (int i = 0; i < constraints.Count; ++i)
                    {
                        XmlSchemaIdentityConstraint constraint = (XmlSchemaIdentityConstraint)constraints[i];
                        CompileIdentityConstraint(constraint);
                        compiledConstraints[idx++] = constraint.CompiledConstraint!;
                    }
                    decl.Constraints = compiledConstraints;
                }

                decl.SchemaElement = xe; //So this is available for PSVI
                xe.ElementDecl = decl;
            }
            catch (XmlSchemaException e)
            {
                if (e.SourceSchemaObject == null)
                {
                    e.SetSource(xe);
                }
                SendValidationEvent(e);
                xe.ElementDecl = SchemaElementDecl.Empty;
            }
            finally
            {
                xe.IsProcessing = false;
            }
        }

        private ContentValidator CompileComplexContent(XmlSchemaComplexType complexType)
        {
            if (complexType.ContentType == XmlSchemaContentType.Empty)
            {
                return ContentValidator.Empty;
            }
            else if (complexType.ContentType == XmlSchemaContentType.TextOnly)
            {
                return ContentValidator.TextOnly;
            }
            XmlSchemaParticle particle = complexType.ContentTypeParticle;
            if (particle == null || particle == XmlSchemaParticle.Empty)
            {
                if (complexType.ContentType == XmlSchemaContentType.ElementOnly)
                {
                    return ContentValidator.Empty;
                }
                else
                {
                    return ContentValidator.Mixed;
                }
            }
            PushComplexType(complexType);
            if (particle is XmlSchemaAll)
            {
                XmlSchemaAll all = (XmlSchemaAll)particle;
                AllElementsContentValidator contentValidator = new AllElementsContentValidator(complexType.ContentType, all.Items.Count, all.MinOccurs == decimal.Zero);
                for (int i = 0; i < all.Items.Count; ++i)
                {
                    XmlSchemaElement localElement = (XmlSchemaElement)all.Items[i];
                    if (!contentValidator.AddElement(localElement.QualifiedName, localElement, localElement.MinOccurs == decimal.Zero))
                    {
                        SendValidationEvent(SR.Sch_DupElement, localElement.QualifiedName.ToString(), localElement);
                    }
                }
                return contentValidator;
            }
            else
            {
                ParticleContentValidator contentValidator = new ParticleContentValidator(complexType.ContentType);
                try
                {
                    contentValidator.Start();
                    BuildParticleContentModel(contentValidator, particle);
                    return contentValidator.Finish(_compileContentModel);
                }
                catch (UpaException e)
                {
                    if (e.Particle1 is XmlSchemaElement)
                    {
                        if (e.Particle2 is XmlSchemaElement)
                        {
                            SendValidationEvent(SR.Sch_NonDeterministic, ((XmlSchemaElement)e.Particle1).QualifiedName.ToString(), (XmlSchemaElement)e.Particle2);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)e.Particle2!).NamespaceList!.ToString(), ((XmlSchemaElement)e.Particle1).QualifiedName.ToString(), (XmlSchemaAny)e.Particle2);
                        }
                    }
                    else
                    {
                        if (e.Particle2 is XmlSchemaElement)
                        {
                            SendValidationEvent(SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)e.Particle1!).NamespaceList!.ToString(), ((XmlSchemaElement)e.Particle2).QualifiedName.ToString(), (XmlSchemaAny)e.Particle1);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_NonDeterministicAnyAny, ((XmlSchemaAny)e.Particle1!).NamespaceList!.ToString(), ((XmlSchemaAny)e.Particle2!).NamespaceList!.ToString(), (XmlSchemaAny)e.Particle1);
                        }
                    }
                    return XmlSchemaComplexType.AnyTypeContentValidator;
                }
                catch (NotSupportedException)
                {
                    SendValidationEvent(SR.Sch_ComplexContentModel, complexType, XmlSeverityType.Warning);
                    return XmlSchemaComplexType.AnyTypeContentValidator;
                }
            }
        }

#if DEBUG
        private string DumpContentModel(XmlSchemaParticle particle)
        {
            StringBuilder sb = new StringBuilder();
            DumpContentModelTo(sb, particle);
            return sb.ToString();
        }

        private void DumpContentModelTo(StringBuilder sb, XmlSchemaParticle particle)
        {
            if (particle is XmlSchemaElement)
            {
                sb.Append(((XmlSchemaElement)particle).QualifiedName);
            }
            else if (particle is XmlSchemaAny)
            {
                sb.Append('<');
                sb.Append(((XmlSchemaAny)particle!).NamespaceList!.ToString());
                sb.Append('>');
            }
            else if (particle is XmlSchemaAll)
            {
                XmlSchemaAll all = (XmlSchemaAll)particle;
                sb.Append('[');
                bool first = true;
                for (int i = 0; i < all.Items.Count; ++i)
                {
                    XmlSchemaElement localElement = (XmlSchemaElement)all.Items[i];
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(localElement.QualifiedName.Name);
                    if (localElement.MinOccurs == decimal.Zero)
                    {
                        sb.Append('?');
                    }
                }
                sb.Append(']');
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase gb = (XmlSchemaGroupBase)particle;
                sb.Append('(');
                string delimeter = (particle is XmlSchemaChoice) ? " | " : ", ";
                bool first = true;
                for (int i = 0; i < gb.Items.Count; ++i)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(delimeter);
                    }
                    DumpContentModelTo(sb, (XmlSchemaParticle)gb.Items[i]);
                }
                sb.Append(')');
            }
            else
            {
                Debug.Assert(particle == XmlSchemaParticle.Empty);
                sb.Append("<>");
            }
            if (particle.MinOccurs == decimal.One && particle.MaxOccurs == decimal.One)
            {
                // nothing
            }
            else if (particle.MinOccurs == decimal.Zero && particle.MaxOccurs == decimal.One)
            {
                sb.Append('?');
            }
            else if (particle.MinOccurs == decimal.Zero && particle.MaxOccurs == decimal.MaxValue)
            {
                sb.Append('*');
            }
            else if (particle.MinOccurs == decimal.One && particle.MaxOccurs == decimal.MaxValue)
            {
                sb.Append('+');
            }
            else
            {
                sb.Append("{" + particle.MinOccurs.ToString(NumberFormatInfo.InvariantInfo) + ", " + particle.MaxOccurs.ToString(NumberFormatInfo.InvariantInfo) + "}");
            }
        }
#endif

        private void BuildParticleContentModel(ParticleContentValidator contentValidator, XmlSchemaParticle particle)
        {
            if (particle is XmlSchemaElement)
            {
                XmlSchemaElement element = (XmlSchemaElement)particle;
                contentValidator.AddName(element.QualifiedName, element);
            }
            else if (particle is XmlSchemaAny)
            {
                XmlSchemaAny any = (XmlSchemaAny)particle;
                contentValidator.AddNamespaceList(any.NamespaceList!, any);
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaObjectCollection particles = ((XmlSchemaGroupBase)particle).Items;
                bool isChoice = particle is XmlSchemaChoice;
                contentValidator.OpenGroup();
                bool first = true;
                for (int i = 0; i < particles.Count; ++i)
                {
                    XmlSchemaParticle p = (XmlSchemaParticle)particles[i];
                    Debug.Assert(!p.IsEmpty);
                    if (first)
                    {
                        first = false;
                    }
                    else if (isChoice)
                    {
                        contentValidator.AddChoice();
                    }
                    else
                    {
                        contentValidator.AddSequence();
                    }
                    BuildParticleContentModel(contentValidator, p);
                }
                contentValidator.CloseGroup();
            }
            else
            {
                Debug.Fail("Unexpected particle");
            }
            if (particle.MinOccurs == decimal.One && particle.MaxOccurs == decimal.One)
            {
                // nothing
            }
            else if (particle.MinOccurs == decimal.Zero && particle.MaxOccurs == decimal.One)
            {
                contentValidator.AddQMark();
            }
            else if (particle.MinOccurs == decimal.Zero && particle.MaxOccurs == decimal.MaxValue)
            {
                contentValidator.AddStar();
            }
            else if (particle.MinOccurs == decimal.One && particle.MaxOccurs == decimal.MaxValue)
            {
                contentValidator.AddPlus();
            }
            else
            {
                contentValidator.AddLeafRange(particle.MinOccurs, particle.MaxOccurs);
            }
        }

        private void CompileParticleElements(XmlSchemaComplexType complexType, XmlSchemaParticle particle)
        {
            if (particle is XmlSchemaElement)
            {
                XmlSchemaElement localElement = (XmlSchemaElement)particle;
                CompileElement(localElement);
                if (complexType.LocalElements[localElement.QualifiedName] == null)
                {
                    complexType.LocalElements.Add(localElement.QualifiedName, localElement);
                }
                else
                {
                    XmlSchemaElement element = (XmlSchemaElement)complexType.LocalElements[localElement.QualifiedName]!;
                    if (element.ElementSchemaType != localElement.ElementSchemaType)
                    {
                        SendValidationEvent(SR.Sch_ElementTypeCollision, particle);
                    }
                }
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaObjectCollection particles = ((XmlSchemaGroupBase)particle).Items;
                for (int i = 0; i < particles.Count; ++i)
                {
                    CompileParticleElements(complexType, (XmlSchemaParticle)particles[i]);
                }
            }
        }

        private void CompileCompexTypeElements(XmlSchemaComplexType complexType)
        {
            if (complexType.IsProcessing)
            {
                SendValidationEvent(SR.Sch_TypeCircularRef, complexType);
                return;
            }

            complexType.IsProcessing = true;
            if (complexType.ContentTypeParticle != XmlSchemaParticle.Empty)
            {
                CompileParticleElements(complexType, complexType.ContentTypeParticle);
            }

            complexType.IsProcessing = false;
        }

        private XmlSchemaSimpleType? GetSimpleType(XmlQualifiedName name)
        {
            XmlSchemaSimpleType? type = _schema!.SchemaTypes[name] as XmlSchemaSimpleType;
            if (type != null)
            {
                CompileSimpleType(type);
            }
            else
            {
                type = DatatypeImplementation.GetSimpleTypeFromXsdType(name);
                //Re-assign datatype impl for V1Compat
                if (type != null)
                {
                    if (type.TypeCode == XmlTypeCode.NormalizedString)
                    {
                        type = DatatypeImplementation.GetNormalizedStringTypeV1Compat();
                    }
                    else if (type.TypeCode == XmlTypeCode.Token)
                    {
                        type = DatatypeImplementation.GetTokenTypeV1Compat();
                    }
                }
            }

            return type;
        }

        private XmlSchemaComplexType? GetComplexType(XmlQualifiedName name)
        {
            XmlSchemaComplexType? type = _schema!.SchemaTypes[name] as XmlSchemaComplexType;
            if (type != null)
            {
                CompileComplexType(type);
            }
            return type;
        }

        private XmlSchemaType? GetAnySchemaType(XmlQualifiedName name)
        {
            XmlSchemaType? type = (XmlSchemaType?)_schema!.SchemaTypes[name];
            if (type != null)
            {
                if (type is XmlSchemaComplexType)
                {
                    CompileComplexType((XmlSchemaComplexType)type);
                }
                else
                {
                    CompileSimpleType((XmlSchemaSimpleType)type);
                }
                return type;
            }
            else
            { //Its is a built-in simpleType
                XmlSchemaSimpleType? simpleType = DatatypeImplementation.GetSimpleTypeFromXsdType(name);
                return simpleType;
            }
        }
    };
} // namespace System.Xml
