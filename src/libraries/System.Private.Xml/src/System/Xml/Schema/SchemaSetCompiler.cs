// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema
{
    internal sealed class Compiler : BaseProcessor
    {
        private string? _restrictionErrorMsg;
        private readonly XmlSchemaObjectTable _attributes = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _elements = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _schemaTypes = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _notations = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _examplars = new XmlSchemaObjectTable();
        private readonly XmlSchemaObjectTable _identityConstraints = new XmlSchemaObjectTable();
        private readonly Stack<XmlSchemaComplexType> _complexTypeStack = new Stack<XmlSchemaComplexType>();
        private readonly Hashtable _schemasToCompile = new Hashtable();

        private readonly XmlSchema? _schemaForSchema;

        public Compiler(XmlNameTable nameTable, ValidationEventHandler? eventHandler, XmlSchema? schemaForSchema, XmlSchemaCompilationSettings? compilationSettings) : base(nameTable, null, eventHandler, compilationSettings)
        {
            _schemaForSchema = schemaForSchema;
        }

        public bool Execute(XmlSchemaSet schemaSet, SchemaInfo schemaCompiledInfo)
        {
            Compile();
            if (!HasErrors)
            {
                Output(schemaCompiledInfo);
                schemaSet.elements = _elements;
                schemaSet.attributes = _attributes;
                schemaSet.schemaTypes = _schemaTypes;
                schemaSet.substitutionGroups = _examplars;
            }
            return !HasErrors;
        }

        internal void Prepare(XmlSchema schema, bool cleanup)
        {
            if (_schemasToCompile[schema] != null)
            {
                return;
            }

            _schemasToCompile.Add(schema, schema);
            foreach (XmlSchemaElement? element in schema.Elements.Values)
            {
                if (cleanup)
                {
                    CleanupElement(element!);
                }

                AddToTable(_elements, element!.QualifiedName, element);
            }

            foreach (XmlSchemaAttribute? attribute in schema.Attributes.Values)
            {
                if (cleanup)
                {
                    CleanupAttribute(attribute!);
                }

                AddToTable(_attributes, attribute!.QualifiedName, attribute);
            }

            foreach (XmlSchemaGroup? group in schema.Groups.Values)
            {
                if (cleanup)
                {
                    CleanupGroup(group!);
                }

                AddToTable(_groups, group!.QualifiedName, group);
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in schema.AttributeGroups.Values)
            {
                if (cleanup)
                {
                    CleanupAttributeGroup(attributeGroup!);
                }

                AddToTable(_attributeGroups, attributeGroup!.QualifiedName, attributeGroup);
            }

            foreach (XmlSchemaType? type in schema.SchemaTypes.Values)
            {
                if (cleanup)
                {
                    XmlSchemaComplexType? ct = type as XmlSchemaComplexType;
                    if (ct != null)
                    {
                        CleanupComplexType(ct);
                    }
                    else
                    {
                        CleanupSimpleType((type as XmlSchemaSimpleType)!);
                    }
                }

                AddToTable(_schemaTypes, type!.QualifiedName, type);
            }

            foreach (XmlSchemaNotation? notation in schema.Notations.Values)
            {
                AddToTable(_notations, notation!.QualifiedName, notation);
            }

            foreach (XmlSchemaIdentityConstraint? ic in schema.IdentityConstraints.Values)
            {
                AddToTable(_identityConstraints, ic!.QualifiedName, ic);
            }
        }

        private void UpdateSForSSimpleTypes()
        {
            Debug.Assert(_schemaForSchema != null);
            XmlSchemaSimpleType[] schemaSimpleTypes = DatatypeImplementation.GetBuiltInTypes();
            XmlSchemaSimpleType builtInType;
            //Using enumToTypeCode array; indexed by XmlTypeCode; Start indexing from 12 since schema types start there and iterate till Length - 2 as the last 2 are xquery types
            int numberOfSchemaTypes = schemaSimpleTypes.Length - 3; //skip last 2 xquery types
            for (int i = 12; i < numberOfSchemaTypes; i++)
            {
                builtInType = schemaSimpleTypes[i];
                _schemaForSchema.SchemaTypes.Replace(builtInType.QualifiedName, builtInType);
                _schemaTypes.Replace(builtInType.QualifiedName, builtInType);
            }
        }

        private void Output(SchemaInfo schemaInfo)
        {
            string? tns;
            foreach (XmlSchema? schema in _schemasToCompile.Values)
            {
                tns = schema!.TargetNamespace;
                if (tns == null)
                {
                    tns = string.Empty;
                }

                schemaInfo.TargetNamespaces[tns] = true;
            }

            foreach (XmlSchemaElement? element in _elements.Values)
            {
                schemaInfo.ElementDecls.Add(element!.QualifiedName, element.ElementDecl!);
            }

            foreach (XmlSchemaAttribute? attribute in _attributes.Values)
            {
                schemaInfo.AttributeDecls.Add(attribute!.QualifiedName, attribute.AttDef!);
            }

            foreach (XmlSchemaType? type in _schemaTypes.Values)
            {
                schemaInfo.ElementDeclsByType.Add(type!.QualifiedName, type.ElementDecl!);
            }

            foreach (XmlSchemaNotation? notation in _notations.Values)
            {
                SchemaNotation no = new SchemaNotation(notation!.QualifiedName);
                no.SystemLiteral = notation.System;
                no.Pubid = notation.Public;
                if (!schemaInfo.Notations.ContainsKey(no.Name.Name))
                {
                    schemaInfo.Notations.Add(no.Name.Name, no);
                }
            }
        }

        internal void ImportAllCompiledSchemas(XmlSchemaSet schemaSet)
        {
            XmlSchema currentSchema;
            int schemaIndex;
            SortedList schemas = schemaSet.SortedSchemas;
            for (schemaIndex = 0; schemaIndex < schemas.Count; schemaIndex++)
            {
                currentSchema = (XmlSchema)schemas.GetByIndex(schemaIndex)!;
                if (currentSchema.IsCompiledBySet)
                { //Import already compiled schemas
                    Prepare(currentSchema, false);
                }
            }
        }

        internal bool Compile()
        {
            _schemaTypes.Insert(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);
            if (_schemaForSchema != null)
            { //Get our built-in types
                _schemaForSchema.SchemaTypes.Replace(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);
                UpdateSForSSimpleTypes();
            }

            foreach (XmlSchemaGroup? group in _groups.Values)
            {
                CompileGroup(group!);
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in _attributeGroups.Values)
            {
                CompileAttributeGroup(attributeGroup!);
            }

            foreach (XmlSchemaType? type in _schemaTypes.Values)
            {
                XmlSchemaComplexType? ct = type as XmlSchemaComplexType;
                if (ct != null)
                {
                    CompileComplexType(ct);
                }
                else
                {
                    CompileSimpleType((XmlSchemaSimpleType)type!);
                }
            }

            foreach (XmlSchemaElement? element in _elements.Values)
            {
                if (element!.ElementDecl == null)
                {
                    CompileElement(element);
                }
            }

            foreach (XmlSchemaAttribute? attribute in _attributes.Values)
            {
                if (attribute!.AttDef == null)
                {
                    CompileAttribute(attribute);
                }
            }

            foreach (XmlSchemaIdentityConstraint? identityConstraint in _identityConstraints.Values)
            {
                if (identityConstraint!.CompiledConstraint == null)
                {
                    CompileIdentityConstraint(identityConstraint);
                }
            }

            while (_complexTypeStack.Count > 0)
            {
                XmlSchemaComplexType type = _complexTypeStack.Pop();
                CompileComplexTypeElements(type);
            }

            ProcessSubstitutionGroups();

            foreach (XmlSchemaType? type in _schemaTypes.Values)
            {
                XmlSchemaComplexType? localType = type as XmlSchemaComplexType;
                if (localType != null)
                {
                    CheckParticleDerivation(localType);
                }
            }

            foreach (XmlSchemaElement? element in _elements.Values)
            {
                XmlSchemaComplexType? localComplexType = element!.ElementSchemaType as XmlSchemaComplexType;
                if (localComplexType != null && element.SchemaTypeName == XmlQualifiedName.Empty)
                { // only local schemaTypes
                    CheckParticleDerivation(localComplexType);
                }
            }

            foreach (XmlSchemaGroup? group in _groups.Values)
            { //Check particle derivation for redefined groups
                XmlSchemaGroup? baseGroup = group!.Redefined;
                if (baseGroup != null)
                {
                    RecursivelyCheckRedefinedGroups(group, baseGroup);
                }
            }

            foreach (XmlSchemaAttributeGroup? attributeGroup in _attributeGroups.Values)
            {
                XmlSchemaAttributeGroup? baseAttributeGroup = attributeGroup!.Redefined;
                if (baseAttributeGroup != null)
                {
                    RecursivelyCheckRedefinedAttributeGroups(attributeGroup, baseAttributeGroup);
                }
            }

            return !HasErrors;
        }

        private void CleanupAttribute(XmlSchemaAttribute attribute)
        {
            if (attribute.SchemaType != null)
            {
                CleanupSimpleType((XmlSchemaSimpleType)attribute.SchemaType);
            }
            attribute.AttDef = null;
        }

        private void CleanupAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
        {
            CleanupAttributes(attributeGroup.Attributes);
            attributeGroup.AttributeUses.Clear();
            attributeGroup.AttributeWildcard = null;
            if (attributeGroup.Redefined != null)
            {
                CleanupAttributeGroup(attributeGroup.Redefined);
            }
        }

        private void CleanupComplexType(XmlSchemaComplexType complexType)
        {
            if (complexType!.QualifiedName == DatatypeImplementation.QnAnyType)
            { //if it is built-in anyType dont clean it.
                return;
            }

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
                        XmlSchemaComplexContentExtension complexExtension = (XmlSchemaComplexContentExtension)complexContent.Content;
                        CleanupParticle(complexExtension.Particle);
                        CleanupAttributes(complexExtension.Attributes);
                    }
                    else
                    { //XmlSchemaComplexContentRestriction
                        XmlSchemaComplexContentRestriction complexRestriction = (XmlSchemaComplexContentRestriction)complexContent.Content!;
                        CleanupParticle(complexRestriction.Particle);
                        CleanupAttributes(complexRestriction.Attributes);
                    }
                }
            }
            else
            { //equals XmlSchemaComplexContent with baseType is anyType
                CleanupParticle(complexType.Particle);
                CleanupAttributes(complexType.Attributes);
            }

            complexType.LocalElements.Clear();
            complexType.AttributeUses.Clear();
            complexType.SetAttributeWildcard(null);
            complexType.SetContentTypeParticle(XmlSchemaParticle.Empty);
            complexType.ElementDecl = null;
            complexType.HasWildCard = false;

            //Clean up the original type if this is a redefined type
            if (complexType.Redefined != null)
            {
                CleanupComplexType((complexType.Redefined as XmlSchemaComplexType)!);
            }
        }

        private void CleanupSimpleType(XmlSchemaSimpleType simpleType)
        {
            if (simpleType == XmlSchemaType.GetBuiltInSimpleType(simpleType.TypeCode))
            { //If it is a built-in simple type dont clean up
                return;
            }
            simpleType.ElementDecl = null;
            //Clean up the original group if this is a redefined group
            if (simpleType.Redefined != null)
            {
                CleanupSimpleType((simpleType.Redefined as XmlSchemaSimpleType)!);
            }
        }

        private void CleanupElement(XmlSchemaElement element)
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
            element.IsLocalTypeDerivationChecked = false; //clear Local element type derivation check
        }

        private void CleanupAttributes(XmlSchemaObjectCollection attributes)
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

        private void CleanupGroup(XmlSchemaGroup group)
        {
            CleanupParticle(group.Particle);
            group.CanonicalParticle = null;
            //Clean up the original group if this is a redefined group
            if (group.Redefined != null)
            {
                CleanupGroup(group.Redefined);
            }
        }

        private void CleanupParticle(XmlSchemaParticle? particle)
        {
            XmlSchemaElement? element = particle as XmlSchemaElement;
            if (element != null)
            {
                CleanupElement(element);
                return;
            }

            XmlSchemaGroupBase? groupBase = particle as XmlSchemaGroupBase;
            if (groupBase != null)
            {
                for (int i = 0; i < groupBase.Items.Count; ++i)
                {
                    CleanupParticle((XmlSchemaParticle)groupBase.Items[i]);
                }
            }
        }

        private void ProcessSubstitutionGroups()
        {
            foreach (XmlSchemaElement? element in _elements.Values)
            {
                if (!element!.SubstitutionGroup.IsEmpty)
                {
                    XmlSchemaElement? headElement = _elements[element.SubstitutionGroup!] as XmlSchemaElement;
                    if (headElement == null)
                    {
                        SendValidationEvent(SR.Sch_NoExamplar, element);
                        continue;
                    }

                    //Check derivation of member's type against head's type
                    if (!XmlSchemaType.IsDerivedFrom(element.ElementSchemaType, headElement.ElementSchemaType, headElement.FinalResolved))
                    {
                        SendValidationEvent(SR.Sch_InvalidSubstitutionMember, (element.QualifiedName).ToString(), (headElement.QualifiedName).ToString(), element);
                    }

                    //Create substitutionGroup
                    XmlSchemaSubstitutionGroup? substitutionGroup = (XmlSchemaSubstitutionGroup?)_examplars[element.SubstitutionGroup];
                    if (substitutionGroup == null)
                    {
                        substitutionGroup = new XmlSchemaSubstitutionGroup();
                        substitutionGroup.Examplar = element.SubstitutionGroup;
                        _examplars.Add(element.SubstitutionGroup, substitutionGroup);
                    }

                    ArrayList members = substitutionGroup.Members;
                    if (!members.Contains(element))
                    { //Members might contain element if the same schema is included and imported through different paths. Imp, hence will be added to set directly
                        members.Add(element);
                    }
                }
            }

            //Check the subst groups that we just built
            foreach (XmlSchemaSubstitutionGroup? substitutionGroup in _examplars.Values)
            {
                CompileSubstitutionGroup(substitutionGroup!);
            }
        }

        private void CompileSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup)
        {
            if (substitutionGroup.IsProcessing && substitutionGroup.Members.Count > 0)
            {
                SendValidationEvent(SR.Sch_SubstitutionCircularRef, (XmlSchemaElement)substitutionGroup.Members[0]!);
                return;
            }

            XmlSchemaElement examplar = (XmlSchemaElement)_elements[substitutionGroup.Examplar]!;
            Debug.Assert(examplar != null); //Headelement presence is checked while building subst groups
            if (substitutionGroup.Members.Contains(examplar))
            {// already checked
                return;
            }

            substitutionGroup.IsProcessing = true;
            try
            {
                if (examplar.FinalResolved == XmlSchemaDerivationMethod.All)
                {
                    SendValidationEvent(SR.Sch_InvalidExamplar, examplar.Name, examplar);
                }

                //Build transitive members
                ArrayList? newMembers = null;
                for (int i = 0; i < substitutionGroup.Members.Count; ++i)
                {
                    XmlSchemaElement element = (XmlSchemaElement)substitutionGroup.Members[i]!;
                    //Chain to other head's that are members of this head's substGroup
                    if ((element.ElementDecl!.Block & XmlSchemaDerivationMethod.Substitution) == 0)
                    { //Chain only if substitution is not blocked
                        XmlSchemaSubstitutionGroup? g = (XmlSchemaSubstitutionGroup?)_examplars[element.QualifiedName];
                        if (g != null)
                        {
                            CompileSubstitutionGroup(g);
                            for (int j = 0; j < g.Members.Count; ++j)
                            {
                                if (g.Members[j] != element)
                                { //Exclude the head
                                    if (newMembers == null)
                                    {
                                        newMembers = new ArrayList();
                                    }
                                    newMembers.Add(g.Members[j]);
                                }
                            }
                        }
                    }
                }

                if (newMembers != null)
                {
                    for (int i = 0; i < newMembers.Count; ++i)
                    {
                        substitutionGroup.Members.Add(newMembers[i]);
                    }
                }

                substitutionGroup.Members.Add(examplar); // Compiled mark
            }
            finally
            {
                substitutionGroup.IsProcessing = false;
            }
        }

        private void RecursivelyCheckRedefinedGroups(XmlSchemaGroup redefinedGroup, XmlSchemaGroup baseGroup)
        {
            if (baseGroup.Redefined != null)
            {
                RecursivelyCheckRedefinedGroups(baseGroup, baseGroup.Redefined);
            }

            if (redefinedGroup.SelfReferenceCount == 0)
            {
                if (baseGroup.CanonicalParticle == null)
                {
                    baseGroup.CanonicalParticle = CannonicalizeParticle(baseGroup.Particle, true);
                }

                if (redefinedGroup.CanonicalParticle == null)
                {
                    redefinedGroup.CanonicalParticle = CannonicalizeParticle(redefinedGroup.Particle, true);
                }

                CompileParticleElements(redefinedGroup.CanonicalParticle);
                CompileParticleElements(baseGroup.CanonicalParticle);
                CheckParticleDerivation(redefinedGroup.CanonicalParticle, baseGroup.CanonicalParticle);
            }
        }

        private void RecursivelyCheckRedefinedAttributeGroups(XmlSchemaAttributeGroup attributeGroup, XmlSchemaAttributeGroup baseAttributeGroup)
        {
            if (baseAttributeGroup.Redefined != null)
            {
                RecursivelyCheckRedefinedAttributeGroups(baseAttributeGroup, baseAttributeGroup.Redefined);
            }
            if (attributeGroup.SelfReferenceCount == 0)
            {
                CompileAttributeGroup(baseAttributeGroup);
                CompileAttributeGroup(attributeGroup);
                CheckAttributeGroupRestriction(baseAttributeGroup, attributeGroup);
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
                    group.CanonicalParticle = CannonicalizeParticle(group.Particle, true);
                }
                Debug.Assert(group.CanonicalParticle != null);
                group.IsProcessing = false; //Not enclosung in try -finally as cannonicalizeParticle will not throw exception
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
                            throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, list.ItemTypeName.ToString(), list);
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
                            XmlSchema parentSchema = Preprocessor.GetParentSchema(simpleType)!;
                            if (parentSchema.TargetNamespace != XmlSchema.Namespace)
                            { //If it is not SForS, then error
                                throw new XmlSchemaException(SR.Sch_InvalidSimpleTypeRestriction, restriction.BaseTypeName.ToString(), simpleType);
                            }
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
                            throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, restriction.BaseTypeName.ToString(), restriction);
                        }
                    }
                    simpleType.SetDatatype(datatype.DeriveByRestriction(restriction.Facets, NameTable, simpleType));
                    simpleType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
                }
                else
                { //simpleType.Content is XmlSchemaSimpleTypeUnion
                    XmlSchemaSimpleType[] baseTypes = CompileBaseMemberTypes(simpleType)!;
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

        private XmlSchemaSimpleType[]? CompileBaseMemberTypes(XmlSchemaSimpleType simpleType)
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
                        throw new XmlSchemaException(SR.Sch_UndeclaredSimpleType, mainMemberTypes[i].ToString(), mainUnion);
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
            return mainUnion.BaseMemberTypes;
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
            try
            {
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
                    complexType.SetContentTypeParticle(CompileContentTypeParticle(complexType.Particle));
                    complexType.SetContentType(GetSchemaContentType(complexType, null, complexType.ContentTypeParticle));
                }
                if (complexType.ContainsIdAttribute(true))
                {
                    SendValidationEvent(SR.Sch_TwoIdAttrUses, complexType);
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
            }
            finally
            {
                complexType.IsProcessing = false;
            }
        }


        private void CompileSimpleContentExtension(XmlSchemaComplexType complexType, XmlSchemaSimpleContentExtension simpleExtension)
        {
            XmlSchemaComplexType? baseType;
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
                    SendValidationEvent(SR.Sch_UndeclaredType, simpleExtension.BaseTypeName.ToString(), simpleExtension);
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
            XmlSchemaComplexType? baseType;
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
            XmlSchemaComplexType? baseType;
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

            if ((baseType.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
            {
                SendValidationEvent(SR.Sch_BaseFinalExtension, complexType);
            }

            CompileLocalAttributes(baseType, complexType, complexExtension.Attributes, complexExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);

            XmlSchemaParticle baseParticle = baseType.ContentTypeParticle;
            XmlSchemaParticle extendedParticle = CannonicalizeParticle(complexExtension.Particle, true);
            if (baseParticle != XmlSchemaParticle.Empty)
            {
                if (extendedParticle != XmlSchemaParticle.Empty)
                {
                    XmlSchemaSequence compiledParticle = new XmlSchemaSequence();
                    compiledParticle.Items.Add(baseParticle);
                    compiledParticle.Items.Add(extendedParticle);
                    complexType.SetContentTypeParticle(CompileContentTypeParticle(compiledParticle));
                }
                else
                {
                    complexType.SetContentTypeParticle(baseParticle);
                }
            }
            else
            {
                complexType.SetContentTypeParticle(extendedParticle);
            }

            XmlSchemaContentType contentType = GetSchemaContentType(complexType, complexContent, extendedParticle);
            if (contentType == XmlSchemaContentType.Empty)
            { //Derived content type is empty, Get ContentType from base
                contentType = baseType.ContentType;
                // In case of a simple base type (content type is TextOnly) the derived type
                //   will be the same as the base type. So set the same content type and then also
                //   set the same data type.
                if (contentType == XmlSchemaContentType.TextOnly)
                {
                    complexType.SetDatatype(baseType.Datatype);
                }
            }

            complexType.SetContentType(contentType);

            if (baseType.ContentType != XmlSchemaContentType.Empty && complexType.ContentType != baseType.ContentType)
            { //If base is empty, do not check
                SendValidationEvent(SR.Sch_DifContentType, complexType);
                return;
            }

            complexType.SetBaseSchemaType(baseType);
            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Extension);
        }

        private void CompileComplexContentRestriction(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaComplexContentRestriction complexRestriction)
        {
            XmlSchemaComplexType? baseType;
            if (complexType.Redefined != null && complexRestriction.BaseTypeName == complexType.Redefined.QualifiedName)
            {
                baseType = (XmlSchemaComplexType)complexType.Redefined;
                CompileComplexType(baseType);
            }
            else
            {
                baseType = GetComplexType(complexRestriction.BaseTypeName!);
                if (baseType == null)
                {
                    SendValidationEvent(SR.Sch_UndefBaseRestriction, complexRestriction.BaseTypeName.ToString(), complexRestriction);
                    return;
                }
            }
            complexType.SetBaseSchemaType(baseType);
            if ((baseType.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
            {
                SendValidationEvent(SR.Sch_BaseFinalRestriction, complexType);
            }
            CompileLocalAttributes(baseType, complexType, complexRestriction.Attributes, complexRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);

            complexType.SetContentTypeParticle(CompileContentTypeParticle(complexRestriction.Particle));
            XmlSchemaContentType derivedContentType = GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle);
            complexType.SetContentType(derivedContentType);
            switch (derivedContentType)
            {
                case XmlSchemaContentType.Empty:
                    if (baseType.ElementDecl != null && !baseType.ElementDecl.ContentValidator!.IsEmptiable)
                    { //base is either TextOnly or its ElementOnly/Mixed and not emptiable
                        SendValidationEvent(SR.Sch_InvalidContentRestrictionDetailed, SR.Sch_InvalidBaseToEmpty, complexType);
                    }
                    break;

                case XmlSchemaContentType.Mixed:
                    if (baseType.ContentType != XmlSchemaContentType.Mixed)
                    {
                        SendValidationEvent(SR.Sch_InvalidContentRestrictionDetailed, SR.Sch_InvalidBaseToMixed, complexType);
                    }
                    break;
            }
            complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
        }

        private void CheckParticleDerivation(XmlSchemaComplexType complexType)
        {
            XmlSchemaComplexType? baseType = complexType.BaseXmlSchemaType as XmlSchemaComplexType;
            _restrictionErrorMsg = null;
            if (baseType != null && baseType != XmlSchemaComplexType.AnyType && complexType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
            {
                XmlSchemaParticle derivedParticle = CannonicalizePointlessRoot(complexType.ContentTypeParticle);
                XmlSchemaParticle baseParticle = CannonicalizePointlessRoot(baseType.ContentTypeParticle);
                if (!IsValidRestriction(derivedParticle, baseParticle))
                {
                    if (_restrictionErrorMsg != null)
                    {
                        SendValidationEvent(SR.Sch_InvalidParticleRestrictionDetailed, _restrictionErrorMsg, complexType);
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_InvalidParticleRestriction, complexType);
                    }
                }
            }
            else if (baseType == XmlSchemaComplexType.AnyType)
            { //The complex type itself is not explicitly derived by restriction but it could have local elements which have anonymous types that are derived by restriction
                foreach (XmlSchemaElement? localElement in complexType.LocalElements.Values)
                {
                    if (localElement!.IsLocalTypeDerivationChecked)
                    { //Element already checked
                        continue;
                    }

                    XmlSchemaComplexType? localComplexType = localElement.ElementSchemaType as XmlSchemaComplexType;
                    if (localComplexType != null && localElement.SchemaTypeName == XmlQualifiedName.Empty && localElement.RefName == XmlQualifiedName.Empty)
                    { //Only local elements
                        localElement.IsLocalTypeDerivationChecked = true; //Not clearing this flag after recursion to make sure this check is not repeated for multiple references of the same local element (through group refs)
                        CheckParticleDerivation(localComplexType);
                    }
                }
            }
        }

        private void CheckParticleDerivation(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
        {
            _restrictionErrorMsg = null;
            derivedParticle = CannonicalizePointlessRoot(derivedParticle);
            baseParticle = CannonicalizePointlessRoot(baseParticle);
            if (!IsValidRestriction(derivedParticle, baseParticle))
            {
                if (_restrictionErrorMsg != null)
                {
                    SendValidationEvent(SR.Sch_InvalidParticleRestrictionDetailed, _restrictionErrorMsg, derivedParticle);
                }
                else
                {
                    SendValidationEvent(SR.Sch_InvalidParticleRestriction, derivedParticle);
                }
            }
        }

        private XmlSchemaParticle CompileContentTypeParticle(XmlSchemaParticle? particle)
        {
            XmlSchemaParticle ctp = CannonicalizeParticle(particle, true);
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

        private XmlSchemaParticle CannonicalizeParticle(XmlSchemaParticle? particle, bool root)
        {
            if (particle == null || particle.IsEmpty)
            {
                return XmlSchemaParticle.Empty;
            }
            else if (particle is XmlSchemaElement)
            {
                //return CannonicalizeElement((XmlSchemaElement)particle, substitution);
                return particle;
            }
            else if (particle is XmlSchemaGroupRef)
            {
                return CannonicalizeGroupRef((XmlSchemaGroupRef)particle, root);
            }
            else if (particle is XmlSchemaAll)
            {
                return CannonicalizeAll((XmlSchemaAll)particle, root);
            }
            else if (particle is XmlSchemaChoice)
            {
                return CannonicalizeChoice((XmlSchemaChoice)particle, root);
            }
            else if (particle is XmlSchemaSequence)
            {
                return CannonicalizeSequence((XmlSchemaSequence)particle, root);
            }
            else
            {
                return particle;
            }
        }

        private XmlSchemaParticle CannonicalizeElement(XmlSchemaElement element)
        {
            if (!element.RefName.IsEmpty && (element.ElementDecl!.Block & XmlSchemaDerivationMethod.Substitution) == 0)
            {
                XmlSchemaSubstitutionGroup? substitutionGroup = (XmlSchemaSubstitutionGroup?)_examplars[element.QualifiedName];
                if (substitutionGroup == null)
                {
                    return element;
                }
                else
                {
                    XmlSchemaChoice choice = new XmlSchemaChoice();
                    for (int i = 0; i < substitutionGroup.Members.Count; ++i)
                    {
                        choice.Items.Add((XmlSchemaElement)substitutionGroup.Members[i]!);
                    }
                    choice.MinOccurs = element.MinOccurs;
                    choice.MaxOccurs = element.MaxOccurs;
                    CopyPosition(choice, element, false);
                    return choice;
                }
            }
            else
            {
                return element;
            }
        }

        private XmlSchemaParticle CannonicalizeGroupRef(XmlSchemaGroupRef groupRef, bool root)
        {
            XmlSchemaGroup? group;
            if (groupRef.Redefined != null)
            {
                group = groupRef.Redefined;
            }
            else
            {
                group = (XmlSchemaGroup?)_groups[groupRef.RefName];
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

                if (groupRef.MinOccurs > decimal.One || groupRef.MaxOccurs != decimal.One)
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
            CopyPosition(groupRefBase, groupRef, true);

            for (int i = 0; i < groupBase!.Items.Count; ++i)
            {
                groupRefBase.Items.Add(groupBase.Items[i]);
            }

            groupRef.SetParticle(groupRefBase);
            return groupRefBase;
        }

        private XmlSchemaParticle CannonicalizeAll(XmlSchemaAll all, bool root)
        {
            if (all.Items.Count > 0)
            {
                XmlSchemaAll newAll = new XmlSchemaAll();
                newAll.MinOccurs = all.MinOccurs;
                newAll.MaxOccurs = all.MaxOccurs;
                CopyPosition(newAll, all, true);
                for (int i = 0; i < all.Items.Count; ++i)
                {
                    XmlSchemaParticle p = CannonicalizeParticle((XmlSchemaElement)all.Items[i], false);
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

        private XmlSchemaParticle CannonicalizeChoice(XmlSchemaChoice choice, bool root)
        {
            XmlSchemaChoice oldChoice = choice;
            if (choice.Items.Count > 0)
            {
                XmlSchemaChoice newChoice = new XmlSchemaChoice();
                newChoice.MinOccurs = choice.MinOccurs;
                newChoice.MaxOccurs = choice.MaxOccurs;
                CopyPosition(newChoice, choice, true);
                for (int i = 0; i < choice.Items.Count; ++i)
                {
                    XmlSchemaParticle p1 = CannonicalizeParticle((XmlSchemaParticle)choice.Items[i], false);
                    if (p1 != XmlSchemaParticle.Empty)
                    {
                        if (p1.MinOccurs == decimal.One && p1.MaxOccurs == decimal.One && p1 is XmlSchemaChoice)
                        {
                            XmlSchemaChoice p1Choice = (p1 as XmlSchemaChoice)!;
                            for (int j = 0; j < p1Choice.Items.Count; ++j)
                            {
                                newChoice.Items.Add(p1Choice.Items[j]);
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

        private XmlSchemaParticle CannonicalizeSequence(XmlSchemaSequence sequence, bool root)
        {
            if (sequence.Items.Count > 0)
            {
                XmlSchemaSequence newSequence = new XmlSchemaSequence();
                newSequence.MinOccurs = sequence.MinOccurs;
                newSequence.MaxOccurs = sequence.MaxOccurs;
                CopyPosition(newSequence, sequence, true);
                for (int i = 0; i < sequence.Items.Count; ++i)
                {
                    XmlSchemaParticle p1 = CannonicalizeParticle((XmlSchemaParticle)sequence.Items[i], false);
                    if (p1 != XmlSchemaParticle.Empty)
                    {
                        XmlSchemaSequence p1Sequence = (p1 as XmlSchemaSequence)!;
                        if (p1.MinOccurs == decimal.One && p1.MaxOccurs == decimal.One && p1Sequence != null)
                        {
                            for (int j = 0; j < p1Sequence.Items.Count; ++j)
                            {
                                newSequence.Items.Add(p1Sequence.Items[j]);
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

        [return: NotNullIfNotNull("particle")]
        private static XmlSchemaParticle? CannonicalizePointlessRoot(XmlSchemaParticle particle)
        {
            if (particle == null)
            {
                return null;
            }

            XmlSchemaSequence? xss;
            XmlSchemaChoice? xsc;
            XmlSchemaAll? xsa;
            decimal one = decimal.One;

            if ((xss = (particle as XmlSchemaSequence)) != null)
            {
                XmlSchemaObjectCollection items = xss.Items;
                int count = items.Count;
                if (count == 1)
                {
                    if (xss.MinOccurs == one && xss.MaxOccurs == one)
                    {
                        return (XmlSchemaParticle)items[0];
                    }
                }
            }
            else if ((xsc = (particle as XmlSchemaChoice)) != null)
            {
                XmlSchemaObjectCollection items = xsc.Items;
                int count = items.Count;

                if (count == 1)
                {
                    if (xsc.MinOccurs == one && xsc.MaxOccurs == one)
                    {
                        return (XmlSchemaParticle)items[0];
                    }
                }
                else if (count == 0)
                {
                    return XmlSchemaParticle.Empty;
                }
            }
            else if ((xsa = (particle as XmlSchemaAll)) != null)
            {
                XmlSchemaObjectCollection items = xsa.Items;
                int count = items.Count;

                if (count == 1)
                {
                    if (xsa.MinOccurs == one && xsa.MaxOccurs == one)
                    {
                        return (XmlSchemaParticle)items[0];
                    }
                }
            }

            return particle;
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
            if (derivedParticle is XmlSchemaElement)
            { //check for derived element being head of substitutionGroup
                XmlSchemaElement derivedElem = (XmlSchemaElement)derivedParticle;
                derivedParticle = CannonicalizeElement(derivedElem);
            }
            if (baseParticle is XmlSchemaElement)
            {
                XmlSchemaElement baseElem = (XmlSchemaElement)baseParticle;
                XmlSchemaParticle newBaseParticle;
                newBaseParticle = CannonicalizeElement(baseElem);
                if (newBaseParticle is XmlSchemaChoice)
                { //Base Element is subs grp head.
                    return IsValidRestriction(derivedParticle, newBaseParticle);
                }
                else if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromElement((XmlSchemaElement)derivedParticle, baseElem);
                }
                else
                {
                    _restrictionErrorMsg = SR.Sch_ForbiddenDerivedParticleForElem;
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
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
                }
                else if (derivedParticle is XmlSchemaAll)
                {
                    if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, true))
                    {
                        return true;
                    }
                }
                else if (derivedParticle is XmlSchemaSequence)
                {
                    if (IsSequenceFromAll((XmlSchemaSequence)derivedParticle, (XmlSchemaAll)baseParticle))
                    {
                        return true;
                    }
                    _restrictionErrorMsg = SR.Format(SR.Sch_SeqFromAll, derivedParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
                }
                else if (derivedParticle is XmlSchemaChoice || derivedParticle is XmlSchemaAny)
                {
                    _restrictionErrorMsg = SR.Sch_ForbiddenDerivedParticleForAll;
                }
                return false;
            }
            else if (baseParticle is XmlSchemaChoice)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
                }
                else if (derivedParticle is XmlSchemaChoice)
                {
                    XmlSchemaChoice baseChoice = (baseParticle as XmlSchemaChoice)!;
                    XmlSchemaChoice derivedChoice = (derivedParticle as XmlSchemaChoice)!;

                    if (baseChoice.Parent == null || derivedChoice.Parent == null)
                    { //using parent property to indicate this choice was created on the fly for substitutionGroup
                        return IsChoiceFromChoiceSubstGroup(derivedChoice!, baseChoice);
                    }
                    if (IsGroupBaseFromGroupBase(derivedChoice, baseChoice, false))
                    {
                        return true;
                    }
                }
                else if (derivedParticle is XmlSchemaSequence)
                {
                    if (IsSequenceFromChoice((XmlSchemaSequence)derivedParticle, (XmlSchemaChoice)baseParticle))
                    {
                        return true;
                    }
                    _restrictionErrorMsg = SR.Format(SR.Sch_SeqFromChoice, derivedParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    _restrictionErrorMsg = SR.Sch_ForbiddenDerivedParticleForChoice;
                }
                return false;
            }
            else if (baseParticle is XmlSchemaSequence)
            {
                if (derivedParticle is XmlSchemaElement)
                {
                    return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
                }
                else if (derivedParticle is XmlSchemaSequence || (derivedParticle is XmlSchemaAll && ((XmlSchemaGroupBase)derivedParticle).Items.Count == 1))
                {
                    if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, true))
                    {
                        return true;
                    }
                }
                else
                {
                    _restrictionErrorMsg = SR.Sch_ForbiddenDerivedParticleForSeq;
                }
                return false;
            }
            else
            {
                Debug.Fail("Unexpected particle");
            }

            return false;
        }

        private bool IsElementFromElement(XmlSchemaElement derivedElement, XmlSchemaElement baseElement)
        {
            // In case of elements the block value '#all' is 100% equivalent to the value of 'substitution restriction extension'.
            // Since below we perform a logical operation on the bit fields we need to convert the #all (0xFF)
            //   to the equivalent set of substitution, restriction and extension, so that the operation works as expected.
            // (The goal of the (derived | base) == derived is to fail if the derived is not a bitwise superset of base)
            XmlSchemaDerivationMethod baseEffectiveBlock =
                baseElement.ElementDecl!.Block == XmlSchemaDerivationMethod.All ?
                    XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension :
                    baseElement.ElementDecl.Block;
            XmlSchemaDerivationMethod derivedEffectiveBlock =
                derivedElement.ElementDecl!.Block == XmlSchemaDerivationMethod.All ?
                    XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension :
                    derivedElement.ElementDecl.Block;

            if (!((derivedElement.QualifiedName == baseElement.QualifiedName) &&
                 (baseElement.IsNillable || !derivedElement.IsNillable) &&
                 IsValidOccurrenceRangeRestriction(derivedElement, baseElement) &&
                 (baseElement.FixedValue == null || IsFixedEqual(baseElement.ElementDecl, derivedElement.ElementDecl)) &&
                 ((derivedEffectiveBlock | baseEffectiveBlock) == derivedEffectiveBlock) &&
                 (derivedElement.ElementSchemaType != null) && (baseElement.ElementSchemaType != null) &&
                 XmlSchemaType.IsDerivedFrom(derivedElement.ElementSchemaType, baseElement.ElementSchemaType, ~(XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union))))
            {
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromElement, derivedElement.QualifiedName, baseElement.QualifiedName);
                return false;
            }
            return true;
        }

        private bool IsElementFromAny(XmlSchemaElement derivedElement, XmlSchemaAny baseAny)
        {
            if (!baseAny.Allows(derivedElement.QualifiedName))
            {
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromAnyRule1, derivedElement.QualifiedName.ToString());
                return false;
            }
            if (!IsValidOccurrenceRangeRestriction(derivedElement, baseAny))
            {
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromAnyRule2, derivedElement.QualifiedName.ToString());
                return false;
            }
            return true;
        }

        private bool IsAnyFromAny(XmlSchemaAny derivedAny, XmlSchemaAny baseAny)
        {
            if (!IsValidOccurrenceRangeRestriction(derivedAny, baseAny))
            {
                _restrictionErrorMsg = SR.Sch_AnyFromAnyRule1;
                return false;
            }
            if (!NamespaceList.IsSubset(derivedAny.NamespaceList!, baseAny.NamespaceList!))
            {
                _restrictionErrorMsg = SR.Sch_AnyFromAnyRule2;
                return false;
            }
            if ((int)derivedAny.ProcessContentsCorrect < (int)baseAny.ProcessContentsCorrect)
            {
                _restrictionErrorMsg = SR.Sch_AnyFromAnyRule3;
                return false;
            }
            return true;
        }

        private bool IsGroupBaseFromAny(XmlSchemaGroupBase derivedGroupBase, XmlSchemaAny baseAny)
        {
            decimal minOccurs, maxOccurs;
            CalculateEffectiveTotalRange(derivedGroupBase, out minOccurs, out maxOccurs);
            if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseAny.MinOccurs, baseAny.MaxOccurs))
            {
                _restrictionErrorMsg = SR.Format(SR.Sch_GroupBaseFromAny2, derivedGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseAny.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseAny.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
                return false;
            }
            // eliminate occurrence range check
            string minOccursAny = baseAny.MinOccursString!;
            baseAny.MinOccurs = decimal.Zero;
            for (int i = 0; i < derivedGroupBase.Items.Count; ++i)
            {
                if (!IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[i], baseAny))
                {
                    _restrictionErrorMsg = SR.Sch_GroupBaseFromAny1;
                    baseAny.MinOccursString = minOccursAny;
                    return false;
                }
            }
            baseAny.MinOccursString = minOccursAny;
            return true;
        }

#if PRIYAL
        private bool IsElementFromGroupBase(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase,  bool skipEmptableOnly) {
            if (!IsRangeSimple(baseGroupBase.MinOccurs, baseGroupBase.MaxOccurs) || !IsRangeSimple(derivedElement.MinOccurs, derivedElement.MaxOccurs)) {
                return IsElementFromGroupBase(derivedElement, baseGroupBase); //SPEC COMPLIANT
            }
            else {
                //Base partilce's and Derived Element's range is simple
                //If all particles in baseParticle also have simple ranges, then can use our algorithm
                //So that we dont break common cases that we used to accept earlier
                //Example Choice -> Element
                if (IsElementFromGroupBaseHack(derivedElement, baseGroupBase, skipEmptableOnly)) {
                    return true;
                }
                else { //Fall back to regular checking
                    return IsElementFromGroupBase(derivedElement, baseGroupBase);
                }
            }
        }

        private bool IsElementFromGroupBaseHack(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase,  bool skipEmptableOnly) {
            bool isMatched = false;

            foreach (XmlSchemaParticle baseParticle in baseGroupBase.Items) {
                if (!isMatched && IsRangeSimple(baseParticle.MinOccurs, baseParticle.MaxOccurs)) {
                    string minOccursElement = baseParticle.MinOccursString;
                    string maxOccursElement = baseParticle.MaxOccursString;
                    baseParticle.MinOccurs *= baseGroupBase.MinOccurs;
                    if ( baseParticle.MaxOccurs != decimal.MaxValue) {
                        if (baseGroupBase.MaxOccurs == decimal.MaxValue)
                             baseParticle.MaxOccurs = decimal.MaxValue;
                        else
                             baseParticle.MaxOccurs *= baseGroupBase.MaxOccurs;
                    }
                    isMatched  = IsValidRestriction(derivedElement, baseParticle);
                    baseParticle.MinOccursString = minOccursElement;
                    baseParticle.MaxOccursString = maxOccursElement;
                }
                if (!isMatched && skipEmptableOnly && !IsParticleEmptiable(baseParticle)) {
                    return false;
                }
            }
            return isMatched;
        }
#endif
        private bool IsElementFromGroupBase(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase)
        {
            if (baseGroupBase is XmlSchemaSequence)
            {
                XmlSchemaSequence virtualSeq = new XmlSchemaSequence();
                virtualSeq.MinOccurs = 1;
                virtualSeq.MaxOccurs = 1;
                virtualSeq.Items.Add(derivedElement);
                if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)virtualSeq, baseGroupBase, true))
                {
                    return true;
                }
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromGroupBase1, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
            }
            else if (baseGroupBase is XmlSchemaChoice)
            {
                XmlSchemaChoice virtualChoice = new XmlSchemaChoice();
                virtualChoice.MinOccurs = 1;
                virtualChoice.MaxOccurs = 1;
                virtualChoice.Items.Add(derivedElement);
                if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)virtualChoice, baseGroupBase, false))
                {
                    return true;
                }
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromGroupBase2, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
            }
            else if (baseGroupBase is XmlSchemaAll)
            {
                XmlSchemaAll virtualAll = new XmlSchemaAll();
                virtualAll.MinOccurs = 1;
                virtualAll.MaxOccurs = 1;
                virtualAll.Items.Add(derivedElement);
                if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)virtualAll, baseGroupBase, true))
                {
                    return true;
                }
                _restrictionErrorMsg = SR.Format(SR.Sch_ElementFromGroupBase3, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
            }
            return false;
        }

        private bool IsChoiceFromChoiceSubstGroup(XmlSchemaChoice derivedChoice, XmlSchemaChoice baseChoice)
        {
            if (!IsValidOccurrenceRangeRestriction(derivedChoice, baseChoice))
            {
                _restrictionErrorMsg = SR.Sch_GroupBaseRestRangeInvalid;
                return false;
            }
            for (int i = 0; i < derivedChoice.Items.Count; ++i)
            {
                if (GetMappingParticle((XmlSchemaParticle)derivedChoice.Items[i], baseChoice.Items) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsGroupBaseFromGroupBase(XmlSchemaGroupBase derivedGroupBase, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
        {
            if (!IsValidOccurrenceRangeRestriction(derivedGroupBase, baseGroupBase))
            {
                _restrictionErrorMsg = SR.Sch_GroupBaseRestRangeInvalid;
                return false;
            }
            if (derivedGroupBase.Items.Count > baseGroupBase.Items.Count)
            {
                _restrictionErrorMsg = SR.Sch_GroupBaseRestNoMap;
                return false;
            }
            int count = 0;
            for (int i = 0; i < baseGroupBase.Items.Count; ++i)
            {
                XmlSchemaParticle baseParticle = (XmlSchemaParticle)baseGroupBase.Items[i];
                if ((count < derivedGroupBase.Items.Count)
                        && IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[count], baseParticle))
                {
                    count++;
                }
                else if (skipEmptableOnly && !IsParticleEmptiable(baseParticle))
                {
                    if (_restrictionErrorMsg == null)
                    { //If restriction failed on previous check, do not overwrite error
                        _restrictionErrorMsg = SR.Sch_GroupBaseRestNotEmptiable;
                    }
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
            minOccurs = derivedSequence.MinOccurs * derivedSequence.Items.Count;
            if (derivedSequence.MaxOccurs == decimal.MaxValue)
            {
                maxOccurs = decimal.MaxValue;
            }
            else
            {
                maxOccurs = derivedSequence.MaxOccurs * derivedSequence.Items.Count;
            }
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

        private static bool IsValidOccurrenceRangeRestriction(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
        {
            return IsValidOccurrenceRangeRestriction(derivedParticle.MinOccurs, derivedParticle.MaxOccurs, baseParticle.MinOccurs, baseParticle.MaxOccurs);
        }

        private static bool IsValidOccurrenceRangeRestriction(decimal minOccurs, decimal maxOccurs, decimal baseMinOccurs, decimal baseMaxOccurs)
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
            decimal minOccurs;
            CalculateEffectiveTotalRange(particle, out minOccurs, out _);
            return minOccurs == decimal.Zero;
        }

        private void CalculateEffectiveTotalRange(XmlSchemaParticle particle, out decimal minOccurs, out decimal maxOccurs)
        {
            XmlSchemaChoice? choice = particle as XmlSchemaChoice;

            if (particle is XmlSchemaElement || particle is XmlSchemaAny)
            {
                minOccurs = particle.MinOccurs;
                maxOccurs = particle.MaxOccurs;
            }
            else if (choice != null)
            {
                if (choice.Items.Count == 0)
                {
                    minOccurs = maxOccurs = decimal.Zero;
                }
                else
                {
                    minOccurs = decimal.MaxValue;
                    maxOccurs = decimal.Zero;
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

        private static XmlSchemaContentType GetSchemaContentType(XmlSchemaComplexType complexType, XmlSchemaComplexContent? complexContent, XmlSchemaParticle particle)
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

            try
            {
                for (int i = 0; i < attributeGroup.Attributes.Count; ++i)
                {
                    XmlSchemaAttribute? attr = attributeGroup.Attributes[i] as XmlSchemaAttribute;
                    if (attr != null)
                    {
                        if (attr.Use == XmlSchemaUse.Prohibited)
                        {
                            continue;
                        }
                        CompileAttribute(attr);
                        if (attributeGroup.AttributeUses[attr.QualifiedName] == null)
                        {
                            attributeGroup.AttributeUses.Add(attr.QualifiedName, attr);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_DupAttributeUse, attr.QualifiedName.ToString(), attr);
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
                            attributeGroupResolved = (XmlSchemaAttributeGroup?)_attributeGroups[attributeGroupRef.RefName];
                        }

                        if (attributeGroupResolved != null)
                        {
                            CompileAttributeGroup(attributeGroupResolved);
                            foreach (XmlSchemaAttribute? attribute in attributeGroupResolved.AttributeUses.Values)
                            {
                                if (attributeGroup.AttributeUses[attribute!.QualifiedName] == null)
                                {
                                    attributeGroup.AttributeUses.Add(attribute.QualifiedName, attribute);
                                }
                                else
                                {
                                    SendValidationEvent(SR.Sch_DupAttributeUse, attribute.QualifiedName.ToString(), attribute);
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
            }
            finally
            {
                attributeGroup.IsProcessing = false;
            }
        }

        private void CompileLocalAttributes(XmlSchemaComplexType? baseType, XmlSchemaComplexType derivedType, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute? anyAttribute, XmlSchemaDerivationMethod derivedBy)
        {
            XmlSchemaAnyAttribute? baseAttributeWildcard = baseType != null ? baseType.AttributeWildcard : null;
            for (int i = 0; i < attributes.Count; ++i)
            {
                XmlSchemaAttribute? attr = attributes[i] as XmlSchemaAttribute;
                if (attr != null)
                {
                    if (attr.Use != XmlSchemaUse.Prohibited)
                    {
                        CompileAttribute(attr);
                    }
                    if (attr.Use != XmlSchemaUse.Prohibited ||
                        (attr.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
                    {
                        if (derivedType.AttributeUses[attr.QualifiedName] == null)
                        {
                            derivedType.AttributeUses.Add(attr.QualifiedName, attr);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_DupAttributeUse, attr.QualifiedName.ToString(), attr);
                        }
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_AttributeIgnored, attr.QualifiedName.ToString(), attr, XmlSeverityType.Warning);
                    }
                }
                else
                { // is XmlSchemaAttributeGroupRef
                    XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
                    XmlSchemaAttributeGroup? attributeGroup = (XmlSchemaAttributeGroup?)_attributeGroups[attributeGroupRef.RefName];
                    if (attributeGroup != null)
                    {
                        CompileAttributeGroup(attributeGroup);
                        foreach (XmlSchemaAttribute? attribute in attributeGroup.AttributeUses.Values)
                        {
                            if (attribute!.Use != XmlSchemaUse.Prohibited ||
                               (attribute.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
                            {
                                if (derivedType.AttributeUses[attribute.QualifiedName] == null)
                                {
                                    derivedType.AttributeUses.Add(attribute.QualifiedName, attribute);
                                }
                                else
                                {
                                    SendValidationEvent(SR.Sch_DupAttributeUse, attribute.QualifiedName.ToString(), attributeGroupRef);
                                }
                            }
                            else
                            {
                                SendValidationEvent(SR.Sch_AttributeIgnored, attribute.QualifiedName.ToString(), attribute, XmlSeverityType.Warning);
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
                        if (attribute == null)
                        {
                            derivedType.AttributeUses.Add(attributeBase.QualifiedName, attributeBase);
                        }
                        else
                        {
                            Debug.Assert(attribute.Use != XmlSchemaUse.Prohibited);
                            if (attributeBase.Use != XmlSchemaUse.Prohibited && attribute.AttributeSchemaType != attributeBase.AttributeSchemaType)
                            { //Extension allows previously prohibited attributes to be re-added,
                                SendValidationEvent(SR.Sch_InvalidAttributeExtension, attribute);
                            }
                        }
                    }
                }
                else
                {  // derivedBy == XmlSchemaDerivationMethod.Restriction
                    // Schema Component Constraint: Derivation Valid (Restriction, Complex)
                    if ((anyAttribute != null) && (baseAttributeWildcard == null || !XmlSchemaAnyAttribute.IsSubset(anyAttribute, baseAttributeWildcard) || !IsProcessContentsRestricted(baseType, anyAttribute, baseAttributeWildcard)))
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
                            else if (attributeBase.Use == XmlSchemaUse.Required && (attribute.Use != XmlSchemaUse.Required))
                            { //If base is required, derived should also be required
                                SendValidationEvent(SR.Sch_AttributeUseInvalid, attribute);
                            }
                            else if (attribute.Use == XmlSchemaUse.Prohibited)
                            {
                                continue;
                            }
                            else if (attributeBase.AttributeSchemaType == null || attribute.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(attribute.AttributeSchemaType, attributeBase.AttributeSchemaType, XmlSchemaDerivationMethod.Empty))
                            {
                                SendValidationEvent(SR.Sch_AttributeRestrictionInvalid, attribute);
                            }
                            else if (!IsFixedEqual(attributeBase.AttDef!, attribute.AttDef!))
                            {
                                SendValidationEvent(SR.Sch_AttributeFixedInvalid, attribute);
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

        private void CheckAttributeGroupRestriction(XmlSchemaAttributeGroup baseAttributeGroup, XmlSchemaAttributeGroup derivedAttributeGroup)
        {
            XmlSchemaAnyAttribute? baseAnyAtt = baseAttributeGroup.AttributeWildcard;
            XmlSchemaAnyAttribute? derivedAnyAtt = derivedAttributeGroup.AttributeWildcard;

            if ((derivedAnyAtt != null) && (baseAnyAtt == null || !XmlSchemaAnyAttribute.IsSubset(derivedAnyAtt, baseAnyAtt) || !IsProcessContentsRestricted(null, derivedAnyAtt, baseAnyAtt)))
            {
                SendValidationEvent(SR.Sch_InvalidAnyAttributeRestriction, derivedAttributeGroup);
            }

            foreach (XmlSchemaAttribute? attributeBase in baseAttributeGroup.AttributeUses.Values)
            {
                XmlSchemaAttribute? attribute = (XmlSchemaAttribute?)derivedAttributeGroup.AttributeUses[attributeBase!.QualifiedName];
                if (attribute != null)
                {
                    if (attributeBase.Use == XmlSchemaUse.Prohibited && attribute.Use != XmlSchemaUse.Prohibited)
                    {
                        SendValidationEvent(SR.Sch_AttributeRestrictionProhibited, attribute);
                    }
                    else if (attributeBase.Use == XmlSchemaUse.Required && attribute.Use != XmlSchemaUse.Required)
                    {
                        SendValidationEvent(SR.Sch_AttributeUseInvalid, attribute);
                    }
                    else if (attribute.Use == XmlSchemaUse.Prohibited)
                    { //If derived att is prohibited, continue
                        continue;
                    }
                    else if (attributeBase.AttributeSchemaType == null || attribute.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(attribute.AttributeSchemaType, attributeBase.AttributeSchemaType, XmlSchemaDerivationMethod.Empty))
                    {
                        SendValidationEvent(SR.Sch_AttributeRestrictionInvalid, attribute);
                    }
                    else if (!IsFixedEqual(attributeBase.AttDef!, attribute.AttDef!))
                    {
                        SendValidationEvent(SR.Sch_AttributeFixedInvalid, attribute);
                    }
                }
                else if (attributeBase.Use == XmlSchemaUse.Required)
                {
                    SendValidationEvent(SR.Sch_NoDerivedAttribute, attributeBase.QualifiedName.ToString(), baseAttributeGroup.QualifiedName.ToString(), derivedAttributeGroup);
                }
            }
            // Check additional ones are valid restriction of base's wildcard
            foreach (XmlSchemaAttribute? attribute in derivedAttributeGroup.AttributeUses.Values)
            {
                XmlSchemaAttribute? attributeBase = (XmlSchemaAttribute?)baseAttributeGroup.AttributeUses[attribute!.QualifiedName];
                if (attributeBase != null)
                {
                    continue;
                }

                if (baseAnyAtt == null || !baseAnyAtt.Allows(attribute.QualifiedName))
                {
                    SendValidationEvent(SR.Sch_AttributeRestrictionInvalidFromWildcard, attribute);
                }
            }
        }

        private static bool IsProcessContentsRestricted(XmlSchemaComplexType? baseType, XmlSchemaAnyAttribute derivedAttributeWildcard, XmlSchemaAnyAttribute baseAttributeWildcard)
        {
            if (baseType == XmlSchemaComplexType.AnyType)
            {
                return true;
            }
            if ((int)derivedAttributeWildcard.ProcessContentsCorrect >= (int)baseAttributeWildcard.ProcessContentsCorrect)
            {
                return true;
            }
            return false;
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
                XmlSchemaAnyAttribute? attribute = XmlSchemaAnyAttribute.Union(a, b, false); //false is for v1Compatd
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
                XmlSchemaAnyAttribute? attribute = XmlSchemaAnyAttribute.Intersection(a, b, false); //false is for v1Compat
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
            SchemaAttDef? decl;
            try
            {
                if (!xa.RefName.IsEmpty)
                {
                    XmlSchemaAttribute? a = (XmlSchemaAttribute?)_attributes[xa.RefName];
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
                    XmlSchemaDatatype datatype = decl.Datatype;

                    if (datatype != null)
                    {
                        if (a.FixedValue == null && a.DefaultValue == null)
                        {
                            SetDefaultFixed(xa, decl);
                        }
                        else if (a.FixedValue != null)
                        {
                            if (xa.DefaultValue != null)
                            {
                                throw new XmlSchemaException(SR.Sch_FixedDefaultInRef, xa.RefName.ToString(), xa);
                            }
                            else if (xa.FixedValue != null)
                            {
                                object refFixedValue = datatype.ParseValue(xa.FixedValue, NameTable, new SchemaNamespaceManager(xa), true);
                                if (!datatype.IsEqual(decl.DefaultValueTyped!, refFixedValue))
                                {
                                    throw new XmlSchemaException(SR.Sch_FixedInRef, xa.RefName.ToString(), xa);
                                }
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
                    //} //Removed this here since the following should be done only if RefName is Empty
                    if (decl.Datatype != null)
                    {
                        decl.Datatype.VerifySchemaValid(_notations, xa);
                    }
                    SetDefaultFixed(xa, decl);
                } //End of Else for !RefName.IsEmpty

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

        private void SetDefaultFixed(XmlSchemaAttribute xa, SchemaAttDef decl)
        {
            if (xa.DefaultValue != null || xa.FixedValue != null)
            {
                if (xa.DefaultValue != null)
                {
                    decl.Presence = SchemaDeclBase.Use.Default;
                    decl.DefaultValueRaw = decl.DefaultValueExpanded = xa.DefaultValue;
                }
                else
                {
                    if (xa.Use == XmlSchemaUse.Required)
                    {
                        decl.Presence = SchemaDeclBase.Use.RequiredFixed;
                    }
                    else
                    {
                        decl.Presence = SchemaDeclBase.Use.Fixed;
                    }

                    decl.DefaultValueRaw = decl.DefaultValueExpanded = xa.FixedValue;
                }

                if (decl.Datatype != null)
                {
                    if (decl.Datatype.TypeCode == XmlTypeCode.Id)
                    {
                        SendValidationEvent(SR.Sch_DefaultIdValue, xa);
                    }
                    else
                    {
                        decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xa), true);
                    }
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
            CompiledIdentityConstraint? compic;
            try
            {
                SchemaNamespaceManager xnmgr = new SchemaNamespaceManager(xi);
                compic = new CompiledIdentityConstraint(xi, xnmgr);
                if (xi is XmlSchemaKeyref)
                {
                    XmlSchemaIdentityConstraint? ic = (XmlSchemaIdentityConstraint?)_identityConstraints[((XmlSchemaKeyref)xi).Refer];
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
                    XmlSchemaElement? e = (XmlSchemaElement?)_elements[xe.RefName];
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
                        XmlSchemaElement? examplar = (XmlSchemaElement?)_elements[xe.SubstitutionGroup];
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
                    Debug.Assert(decl != null);
                    decl.Name = xe.QualifiedName;
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
                    decl.Datatype.VerifySchemaValid(_notations, xe);
                }

                if (xe.DefaultValue != null || xe.FixedValue != null)
                {
                    if (decl.ContentValidator != null)
                    {
                        if (decl.ContentValidator.ContentType == XmlSchemaContentType.TextOnly || (decl.ContentValidator.ContentType == XmlSchemaContentType.Mixed && decl.ContentValidator.IsEmptiable))
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
                                if (decl.Datatype.TypeCode == XmlTypeCode.Id)
                                {
                                    SendValidationEvent(SR.Sch_DefaultIdValue, xe);
                                }
                                else
                                {
                                    decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xe), true);
                                }
                            }
                            else
                            { //Mixed with emptiable particle
                                decl.DefaultValueTyped = DatatypeImplementation.AnySimpleType.Datatype!.ParseValue(decl.DefaultValueRaw, NameTable, new SchemaNamespaceManager(xe));
                            }
                        }
                        else
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
                ParticleContentValidator contentValidator = new ParticleContentValidator(complexType.ContentType, CompilationSettings!.EnableUpaCheck);
                try
                {
                    contentValidator.Start();
                    complexType.HasWildCard = BuildParticleContentModel(contentValidator, particle);
                    return contentValidator.Finish(true);
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
                            SendValidationEvent(SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)e.Particle2!).ResolvedNamespace, ((XmlSchemaElement)e.Particle1).QualifiedName.ToString(), (XmlSchemaAny)e.Particle2);
                        }
                    }
                    else
                    {
                        if (e.Particle2 is XmlSchemaElement)
                        {
                            SendValidationEvent(SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)e.Particle1!).ResolvedNamespace, ((XmlSchemaElement)e.Particle2).QualifiedName.ToString(), (XmlSchemaElement)e.Particle2);
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_NonDeterministicAnyAny, ((XmlSchemaAny)e.Particle1!).ResolvedNamespace, ((XmlSchemaAny)e.Particle2!).ResolvedNamespace, (XmlSchemaAny)e.Particle2);
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

        private bool BuildParticleContentModel(ParticleContentValidator contentValidator, XmlSchemaParticle particle)
        {
            bool hasWildCard = false;
            if (particle is XmlSchemaElement)
            {
                XmlSchemaElement element = (XmlSchemaElement)particle;
                contentValidator.AddName(element.QualifiedName, element);
            }
            else if (particle is XmlSchemaAny)
            {
                hasWildCard = true;
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
                    Debug.Assert(!((XmlSchemaParticle)particles[i]).IsEmpty);
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
                    hasWildCard = BuildParticleContentModel(contentValidator, (XmlSchemaParticle)particles[i]);
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
            return hasWildCard;
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

        private void CompileParticleElements(XmlSchemaParticle particle)
        { //For checking redefined group particle derivation
            if (particle is XmlSchemaElement)
            {
                XmlSchemaElement localElement = (XmlSchemaElement)particle;
                CompileElement(localElement);
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaObjectCollection particles = ((XmlSchemaGroupBase)particle).Items;
                for (int i = 0; i < particles.Count; ++i)
                {
                    CompileParticleElements((XmlSchemaParticle)particles[i]);
                }
            }
        }

        private void CompileComplexTypeElements(XmlSchemaComplexType complexType)
        {
            if (complexType.IsProcessing)
            {
                SendValidationEvent(SR.Sch_TypeCircularRef, complexType);
                return;
            }
            complexType.IsProcessing = true;
            try
            {
                if (complexType.ContentTypeParticle != XmlSchemaParticle.Empty)
                {
                    CompileParticleElements(complexType, complexType.ContentTypeParticle);
                }
            }
            finally
            {
                complexType.IsProcessing = false;
            }
        }

        private XmlSchemaSimpleType? GetSimpleType(XmlQualifiedName name)
        {
            XmlSchemaSimpleType? type = _schemaTypes[name] as XmlSchemaSimpleType;
            if (type != null)
            {
                CompileSimpleType(type);
            }
            else
            {
                type = DatatypeImplementation.GetSimpleTypeFromXsdType(name);
            }
            return type;
        }

        private XmlSchemaComplexType? GetComplexType(XmlQualifiedName name)
        {
            XmlSchemaComplexType? type = _schemaTypes[name] as XmlSchemaComplexType;
            if (type != null)
            {
                CompileComplexType(type);
            }

            return type;
        }

        private XmlSchemaType? GetAnySchemaType(XmlQualifiedName name)
        {
            XmlSchemaType? type = (XmlSchemaType?)_schemaTypes[name];
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

        private static void CopyPosition(XmlSchemaAnnotated to, XmlSchemaAnnotated from, bool copyParent)
        {
            to.SourceUri = from.SourceUri;
            to.LinePosition = from.LinePosition;
            to.LineNumber = from.LineNumber;
            to.SetUnhandledAttributes(from.UnhandledAttributes);
            if (copyParent)
            {
                to.Parent = from.Parent;
            }
        }

        private static bool IsFixedEqual(SchemaDeclBase baseDecl, SchemaDeclBase derivedDecl)
        {
            if (baseDecl.Presence == SchemaDeclBase.Use.Fixed || baseDecl.Presence == SchemaDeclBase.Use.RequiredFixed)
            {
                object baseFixedValue = baseDecl.DefaultValueTyped!;

                Debug.Assert(baseFixedValue != null);
                if (derivedDecl.Presence != SchemaDeclBase.Use.Fixed && derivedDecl.Presence != SchemaDeclBase.Use.RequiredFixed)
                {
                    return false;
                }

                object derivedFixedValue = derivedDecl.DefaultValueTyped!;
                Debug.Assert(derivedFixedValue != null);
                XmlSchemaDatatype baseDatatype = baseDecl.Datatype;
                XmlSchemaDatatype derivedDatatype = derivedDecl.Datatype;

                if (baseDatatype.Variety == XmlSchemaDatatypeVariety.Union)
                {
                    if (derivedDatatype.Variety == XmlSchemaDatatypeVariety.Union)
                    {
                        if (!derivedDatatype.IsEqual(baseFixedValue, derivedFixedValue))
                        {
                            return false;
                        }
                    }
                    else
                    { //Base is union and derived is member of union
                        XsdSimpleValue simpleFixedValue = (baseDecl.DefaultValueTyped as XsdSimpleValue)!;
                        Debug.Assert(simpleFixedValue != null);
                        XmlSchemaDatatype memberType = simpleFixedValue.XmlType.Datatype!;
                        if (!memberType.IsComparable(derivedDatatype) || !derivedDatatype.IsEqual(simpleFixedValue.TypedValue, derivedFixedValue))
                        { //base type {Union of long & string}, derived type {int}
                            return false;
                        }
                    }
                }
                else if (!derivedDatatype.IsEqual(baseFixedValue, derivedFixedValue))
                {
                    return false;
                }
            }
            return true;
        }
    };
} // namespace System.Xml
