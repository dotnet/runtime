// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Xml.Schema;
    using System.Xml;
    using System.Collections;
    using System.ComponentModel;
    using System.Reflection;
    using System.Diagnostics;
    using System.CodeDom.Compiler;

    /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class SoapSchemaImporter : SchemaImporter
    {
        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.SoapSchemaImporter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapSchemaImporter(XmlSchemas schemas) : base(schemas, CodeGenerationOptions.GenerateProperties, null, new ImportContext()) { }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.SoapSchemaImporter1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapSchemaImporter(XmlSchemas schemas, CodeIdentifiers typeIdentifiers) : base(schemas, CodeGenerationOptions.GenerateProperties, null, new ImportContext(typeIdentifiers, false)) { }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.SoapSchemaImporter2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapSchemaImporter(XmlSchemas schemas, CodeIdentifiers typeIdentifiers, CodeGenerationOptions options) : base(schemas, options, null, new ImportContext(typeIdentifiers, false)) { }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.SoapSchemaImporter3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapSchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, ImportContext context) : base(schemas, options, null, context) { }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.SoapSchemaImporter4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapSchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, CodeDomProvider codeProvider, ImportContext context) : base(schemas, options, codeProvider, context) { }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.ImportDerivedTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportDerivedTypeMapping(XmlQualifiedName name, Type baseType, bool baseTypeCanBeIndirect)
        {
            TypeMapping mapping = ImportType(name, false);
            if (mapping is StructMapping)
            {
                MakeDerived((StructMapping)mapping, baseType, baseTypeCanBeIndirect);
            }
            else if (baseType != null)
                throw new InvalidOperationException(SR.Format(SR.XmlPrimitiveBaseType, name.Name, name.Namespace, baseType.FullName));
            ElementAccessor accessor = new ElementAccessor();
            accessor.IsSoap = true;
            accessor.Name = name.Name;
            accessor.Namespace = name.Namespace;
            accessor.Mapping = mapping;
            accessor.IsNullable = true;
            accessor.Form = XmlSchemaForm.Qualified;

            return new XmlTypeMapping(Scope, accessor);
        }


        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.ImportMembersMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(string name, string ns, SoapSchemaMember member)
        {
            TypeMapping typeMapping = ImportType(member.MemberType, true);
            if (!(typeMapping is StructMapping)) return ImportMembersMapping(name, ns, new SoapSchemaMember[] { member });

            MembersMapping mapping = new MembersMapping();
            mapping.TypeDesc = Scope.GetTypeDesc(typeof(object[]));
            mapping.Members = ((StructMapping)typeMapping).Members;
            mapping.HasWrapperElement = true;

            ElementAccessor accessor = new ElementAccessor();
            accessor.IsSoap = true;
            accessor.Name = name;
            accessor.Namespace = typeMapping.Namespace != null ? typeMapping.Namespace : ns;
            accessor.Mapping = mapping;
            accessor.IsNullable = false;
            accessor.Form = XmlSchemaForm.Qualified;

            return new XmlMembersMapping(Scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.ImportMembersMapping1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(string name, string ns, SoapSchemaMember[] members)
        {
            return ImportMembersMapping(name, ns, members, true);
        }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.ImportMembersMapping2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(string name, string ns, SoapSchemaMember[] members, bool hasWrapperElement)
        {
            return ImportMembersMapping(name, ns, members, hasWrapperElement, null, false);
        }

        /// <include file='doc\SoapSchemaImporter.uex' path='docs/doc[@for="SoapSchemaImporter.ImportMembersMapping3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(string name, string ns, SoapSchemaMember[] members, bool hasWrapperElement, Type baseType, bool baseTypeCanBeIndirect)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            XmlSchemaSequence seq = new XmlSchemaSequence();
            type.Particle = seq;
            foreach (SoapSchemaMember member in members)
            {
                XmlSchemaElement element = new XmlSchemaElement();
                element.Name = member.MemberName;
                element.SchemaTypeName = member.MemberType;
                seq.Items.Add(element);
            }

            CodeIdentifiers identifiers = new CodeIdentifiers();
            identifiers.UseCamelCasing = true;
            MembersMapping mapping = new MembersMapping();
            mapping.TypeDesc = Scope.GetTypeDesc(typeof(object[]));
            mapping.Members = ImportTypeMembers(type, ns, identifiers);
            mapping.HasWrapperElement = hasWrapperElement;

            if (baseType != null)
            {
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    MemberMapping member = mapping.Members[i];
                    if (member.Accessor.Mapping is StructMapping)
                        MakeDerived((StructMapping)member.Accessor.Mapping, baseType, baseTypeCanBeIndirect);
                }
            }
            ElementAccessor accessor = new ElementAccessor();
            accessor.IsSoap = true;
            accessor.Name = name;
            accessor.Namespace = ns;
            accessor.Mapping = mapping;
            accessor.IsNullable = false;
            accessor.Form = XmlSchemaForm.Qualified;

            return new XmlMembersMapping(Scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        private ElementAccessor ImportElement(XmlSchemaElement element, string ns)
        {
            if (!element.RefName.IsEmpty)
            {
                throw new InvalidOperationException(SR.Format(SR.RefSyntaxNotSupportedForElements0, element.RefName.Name, element.RefName.Namespace));
            }

            if (element.Name.Length == 0)
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                throw new InvalidOperationException(SR.Format(SR.XmlElementHasNoName, parentType.Name, parentType.Namespace));
            }
            TypeMapping mapping = ImportElementType(element, ns);
            ElementAccessor accessor = new ElementAccessor();
            accessor.IsSoap = true;
            accessor.Name = element.Name;
            accessor.Namespace = ns;
            accessor.Mapping = mapping;
            accessor.IsNullable = element.IsNillable;
            accessor.Form = XmlSchemaForm.None;

            return accessor;
        }

        private TypeMapping ImportElementType(XmlSchemaElement element, string ns)
        {
            TypeMapping mapping;
            if (!element.SchemaTypeName.IsEmpty)
                mapping = ImportType(element.SchemaTypeName, false);
            else if (element.SchemaType != null)
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                if (element.SchemaType is XmlSchemaComplexType)
                {
                    mapping = ImportType((XmlSchemaComplexType)element.SchemaType, ns, false);
                    if (!(mapping is ArrayMapping))
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidSchemaElementType, parentType.Name, parentType.Namespace, element.Name));
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidSchemaElementType, parentType.Name, parentType.Namespace, element.Name));
                }
            }
            else if (!element.SubstitutionGroup.IsEmpty)
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidSubstitutionGroupUse, parentType.Name, parentType.Namespace));
            }
            else
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                throw new InvalidOperationException(SR.Format(SR.XmlElementMissingType, parentType.Name, parentType.Namespace, element.Name));
            }

            mapping.ReferencedByElement = true;

            return mapping;
        }

        internal override void ImportDerivedTypes(XmlQualifiedName baseName)
        {
            foreach (XmlSchema schema in Schemas)
            {
                if (Schemas.IsReference(schema)) continue;
                if (XmlSchemas.IsDataSet(schema)) continue;
                XmlSchemas.Preprocess(schema);
                foreach (object item in schema.SchemaTypes.Values)
                {
                    if (item is XmlSchemaType)
                    {
                        XmlSchemaType type = (XmlSchemaType)item;
                        if (type.DerivedFrom == baseName)
                        {
                            ImportType(type.QualifiedName, false);
                        }
                    }
                }
            }
        }

        private TypeMapping ImportType(XmlQualifiedName name, bool excludeFromImport)
        {
            if (name.Name == Soap.UrType && name.Namespace == XmlSchema.Namespace)
                return ImportRootMapping();
            object type = FindType(name);
            TypeMapping mapping = (TypeMapping)ImportedMappings[type];
            if (mapping == null)
            {
                if (type is XmlSchemaComplexType)
                    mapping = ImportType((XmlSchemaComplexType)type, name.Namespace, excludeFromImport);
                else if (type is XmlSchemaSimpleType)
                    mapping = ImportDataType((XmlSchemaSimpleType)type, name.Namespace, name.Name, false);
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            if (excludeFromImport)
                mapping.IncludeInSchema = false;
            return mapping;
        }

        private TypeMapping ImportType(XmlSchemaComplexType type, string typeNs, bool excludeFromImport)
        {
            if (type.Redefined != null)
            {
                // we do not support redefine in the current version
                throw new NotSupportedException(SR.Format(SR.XmlUnsupportedRedefine, type.Name, typeNs));
            }
            TypeMapping mapping = ImportAnyType(type, typeNs);
            if (mapping == null)
                mapping = ImportArrayMapping(type, typeNs);
            if (mapping == null)
                mapping = ImportStructType(type, typeNs, excludeFromImport);
            return mapping;
        }
        private TypeMapping ImportAnyType(XmlSchemaComplexType type, string typeNs)
        {
            if (type.Particle == null)
                return null;
            if (!(type.Particle is XmlSchemaAll || type.Particle is XmlSchemaSequence))
                return null;
            XmlSchemaGroupBase group = (XmlSchemaGroupBase)type.Particle;

            if (group.Items.Count != 1 || !(group.Items[0] is XmlSchemaAny))
                return null;
            return ImportRootMapping();
        }

        private StructMapping ImportStructType(XmlSchemaComplexType type, string typeNs, bool excludeFromImport)
        {
            if (type.Name == null)
            {
                XmlSchemaElement element = (XmlSchemaElement)type.Parent;
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidSchemaElementType, parentType.Name, parentType.Namespace, element.Name));
            }

            TypeDesc baseTypeDesc = null;

            Mapping baseMapping = null;
            if (!type.DerivedFrom.IsEmpty)
            {
                baseMapping = ImportType(type.DerivedFrom, excludeFromImport);

                if (baseMapping is StructMapping)
                    baseTypeDesc = ((StructMapping)baseMapping).TypeDesc;
                else
                    baseMapping = null;
            }
            if (baseMapping == null) baseMapping = GetRootMapping();
            Mapping previousMapping = (Mapping)ImportedMappings[type];
            if (previousMapping != null)
            {
                return (StructMapping)previousMapping;
            }
            string typeName = GenerateUniqueTypeName(Accessor.UnescapeName(type.Name));
            StructMapping structMapping = new StructMapping();
            structMapping.IsReference = Schemas.IsReference(type);
            TypeFlags flags = TypeFlags.Reference;
            if (type.IsAbstract) flags |= TypeFlags.Abstract;
            structMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Struct, baseTypeDesc, flags);
            structMapping.Namespace = typeNs;
            structMapping.TypeName = type.Name;
            structMapping.BaseMapping = (StructMapping)baseMapping;
            ImportedMappings.Add(type, structMapping);
            if (excludeFromImport)
                structMapping.IncludeInSchema = false;
            CodeIdentifiers members = new CodeIdentifiers();
            members.AddReserved(typeName);
            AddReservedIdentifiersForDataBinding(members);
            structMapping.Members = ImportTypeMembers(type, typeNs, members);
            Scope.AddTypeMapping(structMapping);
            ImportDerivedTypes(new XmlQualifiedName(type.Name, typeNs));
            return structMapping;
        }

        private MemberMapping[] ImportTypeMembers(XmlSchemaComplexType type, string typeNs, CodeIdentifiers members)
        {
            if (type.AnyAttribute != null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidAnyAttributeUse, type.Name, type.QualifiedName.Namespace));
            }

            XmlSchemaObjectCollection items = type.Attributes;
            for (int i = 0; i < items.Count; i++)
            {
                object item = items[i];
                if (item is XmlSchemaAttributeGroup)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlSoapInvalidAttributeUse, type.Name, type.QualifiedName.Namespace));
                }
                if (item is XmlSchemaAttribute)
                {
                    XmlSchemaAttribute attr = (XmlSchemaAttribute)item;
                    if (attr.Use != XmlSchemaUse.Prohibited) throw new InvalidOperationException(SR.Format(SR.XmlSoapInvalidAttributeUse, type.Name, type.QualifiedName.Namespace));
                }
            }
            if (type.Particle != null)
            {
                ImportGroup(type.Particle, members, typeNs);
            }
            else if (type.ContentModel != null && type.ContentModel is XmlSchemaComplexContent)
            {
                XmlSchemaComplexContent model = (XmlSchemaComplexContent)type.ContentModel;

                if (model.Content is XmlSchemaComplexContentExtension)
                {
                    if (((XmlSchemaComplexContentExtension)model.Content).Particle != null)
                    {
                        ImportGroup(((XmlSchemaComplexContentExtension)model.Content).Particle, members, typeNs);
                    }
                }
                else if (model.Content is XmlSchemaComplexContentRestriction)
                {
                    if (((XmlSchemaComplexContentRestriction)model.Content).Particle != null)
                    {
                        ImportGroup(((XmlSchemaComplexContentRestriction)model.Content).Particle, members, typeNs);
                    }
                }
            }
            return (MemberMapping[])members.ToArray(typeof(MemberMapping));
        }

        private void ImportGroup(XmlSchemaParticle group, CodeIdentifiers members, string ns)
        {
            if (group is XmlSchemaChoice)
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(group);
                throw new InvalidOperationException(SR.Format(SR.XmlSoapInvalidChoice, parentType.Name, parentType.Namespace));
            }
            else
                ImportGroupMembers(group, members, ns);
        }

        private void ImportGroupMembers(XmlSchemaParticle particle, CodeIdentifiers members, string ns)
        {
            XmlQualifiedName parentType = XmlSchemas.GetParentName(particle);
            if (particle is XmlSchemaGroupRef)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlSoapUnsupportedGroupRef, parentType.Name, parentType.Namespace));
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase group = (XmlSchemaGroupBase)particle;
                if (group.IsMultipleOccurrence)
                    throw new InvalidOperationException(SR.Format(SR.XmlSoapUnsupportedGroupRepeat, parentType.Name, parentType.Namespace));
                for (int i = 0; i < group.Items.Count; i++)
                {
                    object item = group.Items[i];
                    if (item is XmlSchemaGroupBase || item is XmlSchemaGroupRef)
                        throw new InvalidOperationException(SR.Format(SR.XmlSoapUnsupportedGroupNested, parentType.Name, parentType.Namespace));
                    else if (item is XmlSchemaElement)
                        ImportElementMember((XmlSchemaElement)item, members, ns);
                    else if (item is XmlSchemaAny)
                        throw new InvalidOperationException(SR.Format(SR.XmlSoapUnsupportedGroupAny, parentType.Name, parentType.Namespace));
                }
            }
        }


        private ElementAccessor ImportArray(XmlSchemaElement element, string ns)
        {
            if (element.SchemaType == null) return null;
            if (!element.IsMultipleOccurrence) return null;
            XmlSchemaType type = element.SchemaType;
            ArrayMapping arrayMapping = ImportArrayMapping(type, ns);
            if (arrayMapping == null) return null;
            ElementAccessor arrayAccessor = new ElementAccessor();
            arrayAccessor.IsSoap = true;
            arrayAccessor.Name = element.Name;
            arrayAccessor.Namespace = ns;
            arrayAccessor.Mapping = arrayMapping;
            arrayAccessor.IsNullable = false;
            arrayAccessor.Form = XmlSchemaForm.None;

            return arrayAccessor;
        }

        private ArrayMapping ImportArrayMapping(XmlSchemaType type, string ns)
        {
            ArrayMapping arrayMapping;
            if (type.Name == Soap.Array && ns == Soap.Encoding)
            {
                arrayMapping = new ArrayMapping();
                TypeMapping mapping = GetRootMapping();
                ElementAccessor itemAccessor = new ElementAccessor();
                itemAccessor.IsSoap = true;
                itemAccessor.Name = Soap.UrType;
                itemAccessor.Namespace = ns;
                itemAccessor.Mapping = mapping;
                itemAccessor.IsNullable = true;
                itemAccessor.Form = XmlSchemaForm.None;

                arrayMapping.Elements = new ElementAccessor[] { itemAccessor };
                arrayMapping.TypeDesc = itemAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
                arrayMapping.TypeName = "ArrayOf" + CodeIdentifier.MakePascal(itemAccessor.Mapping.TypeName);
                return arrayMapping;
            }
            if (!(type.DerivedFrom.Name == Soap.Array && type.DerivedFrom.Namespace == Soap.Encoding)) return null;

            // the type should be a XmlSchemaComplexType
            XmlSchemaContentModel model = ((XmlSchemaComplexType)type).ContentModel;

            // the Content  should be an restriction
            if (!(model.Content is XmlSchemaComplexContentRestriction)) return null;

            arrayMapping = new ArrayMapping();

            XmlSchemaComplexContentRestriction restriction = (XmlSchemaComplexContentRestriction)model.Content;

            for (int i = 0; i < restriction.Attributes.Count; i++)
            {
                XmlSchemaAttribute attribute = restriction.Attributes[i] as XmlSchemaAttribute;
                if (attribute != null && attribute.RefName.Name == Soap.ArrayType && attribute.RefName.Namespace == Soap.Encoding)
                {
                    // read the value of the wsdl:arrayType attribute
                    string arrayType = null;

                    if (attribute.UnhandledAttributes != null)
                    {
                        foreach (XmlAttribute a in attribute.UnhandledAttributes)
                        {
                            if (a.LocalName == Wsdl.ArrayType && a.NamespaceURI == Wsdl.Namespace)
                            {
                                arrayType = a.Value;
                                break;
                            }
                        }
                    }
                    if (arrayType != null)
                    {
                        string dims;
                        XmlQualifiedName typeName = TypeScope.ParseWsdlArrayType(arrayType, out dims, attribute);

                        TypeMapping mapping;
                        TypeDesc td = Scope.GetTypeDesc(typeName.Name, typeName.Namespace);
                        if (td != null && td.IsPrimitive)
                        {
                            mapping = new PrimitiveMapping();
                            mapping.TypeDesc = td;
                            mapping.TypeName = td.DataType.Name;
                        }
                        else
                        {
                            mapping = ImportType(typeName, false);
                        }
                        ElementAccessor itemAccessor = new ElementAccessor();
                        itemAccessor.IsSoap = true;
                        itemAccessor.Name = typeName.Name;
                        itemAccessor.Namespace = ns;
                        itemAccessor.Mapping = mapping;
                        itemAccessor.IsNullable = true;
                        itemAccessor.Form = XmlSchemaForm.None;

                        arrayMapping.Elements = new ElementAccessor[] { itemAccessor };
                        arrayMapping.TypeDesc = itemAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
                        arrayMapping.TypeName = "ArrayOf" + CodeIdentifier.MakePascal(itemAccessor.Mapping.TypeName);

                        return arrayMapping;
                    }
                }
            }

            XmlSchemaParticle particle = restriction.Particle;
            if (particle is XmlSchemaAll || particle is XmlSchemaSequence)
            {
                XmlSchemaGroupBase group = (XmlSchemaGroupBase)particle;
                if (group.Items.Count != 1 || !(group.Items[0] is XmlSchemaElement))
                    return null;
                XmlSchemaElement itemElement = (XmlSchemaElement)group.Items[0];
                if (!itemElement.IsMultipleOccurrence) return null;
                ElementAccessor itemAccessor = ImportElement(itemElement, ns);
                arrayMapping.Elements = new ElementAccessor[] { itemAccessor };
                arrayMapping.TypeDesc = ((TypeMapping)itemAccessor.Mapping).TypeDesc.CreateArrayTypeDesc();
            }
            else
            {
                return null;
            }
            return arrayMapping;
        }

        private void ImportElementMember(XmlSchemaElement element, CodeIdentifiers members, string ns)
        {
            ElementAccessor accessor;
            if ((accessor = ImportArray(element, ns)) == null)
            {
                accessor = ImportElement(element, ns);
            }

            MemberMapping member = new MemberMapping();
            member.Name = CodeIdentifier.MakeValid(Accessor.UnescapeName(accessor.Name));
            member.Name = members.AddUnique(member.Name, member);
            if (member.Name.EndsWith("Specified", StringComparison.Ordinal))
            {
                string name = member.Name;
                member.Name = members.AddUnique(member.Name, member);
                members.Remove(name);
            }
            member.TypeDesc = ((TypeMapping)accessor.Mapping).TypeDesc;
            member.Elements = new ElementAccessor[] { accessor };
            if (element.IsMultipleOccurrence)
                member.TypeDesc = member.TypeDesc.CreateArrayTypeDesc();

            if (element.MinOccurs == 0 && member.TypeDesc.IsValueType && !member.TypeDesc.HasIsEmpty)
            {
                member.CheckSpecified = SpecifiedAccessor.ReadWrite;
            }
        }

        private TypeMapping ImportDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, bool isList)
        {
            TypeMapping mapping = ImportNonXsdPrimitiveDataType(dataType, typeNs);
            if (mapping != null)
                return mapping;

            if (dataType.Content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)dataType.Content;
                foreach (object o in restriction.Facets)
                {
                    if (o is XmlSchemaEnumerationFacet)
                    {
                        return ImportEnumeratedDataType(dataType, typeNs, identifier, isList);
                    }
                }
            }
            else if (dataType.Content is XmlSchemaSimpleTypeList || dataType.Content is XmlSchemaSimpleTypeUnion)
            {
                if (dataType.Content is XmlSchemaSimpleTypeList)
                {
                    // check if we have enumeration list
                    XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)dataType.Content;
                    if (list.ItemType != null)
                    {
                        mapping = ImportDataType(list.ItemType, typeNs, identifier, true);
                        if (mapping != null)
                        {
                            return mapping;
                        }
                    }
                }
                mapping = new PrimitiveMapping();
                mapping.TypeDesc = Scope.GetTypeDesc(typeof(string));
                mapping.TypeName = mapping.TypeDesc.DataType.Name;
                return mapping;
            }
            return ImportPrimitiveDataType(dataType);
        }

        private TypeMapping ImportEnumeratedDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, bool isList)
        {
            TypeMapping mapping = (TypeMapping)ImportedMappings[dataType];
            if (mapping != null)
                return mapping;

            XmlSchemaSimpleType sourceDataType = FindDataType(dataType.DerivedFrom);
            TypeDesc sourceTypeDesc = Scope.GetTypeDesc(sourceDataType);
            if (sourceTypeDesc != null && sourceTypeDesc != Scope.GetTypeDesc(typeof(string)))
                return ImportPrimitiveDataType(dataType);
            identifier = Accessor.UnescapeName(identifier);
            string typeName = GenerateUniqueTypeName(identifier);
            EnumMapping enumMapping = new EnumMapping();
            enumMapping.IsReference = Schemas.IsReference(dataType);
            enumMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Enum, null, 0);
            enumMapping.TypeName = identifier;
            enumMapping.Namespace = typeNs;
            enumMapping.IsFlags = isList;
            CodeIdentifiers constants = new CodeIdentifiers();

            if (!(dataType.Content is XmlSchemaSimpleTypeRestriction))
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidEnumContent, dataType.Content.GetType().Name, identifier));

            XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)dataType.Content;

            for (int i = 0; i < restriction.Facets.Count; i++)
            {
                object facet = restriction.Facets[i];
                if (!(facet is XmlSchemaEnumerationFacet)) continue;
                XmlSchemaEnumerationFacet enumeration = (XmlSchemaEnumerationFacet)facet;
                ConstantMapping constant = new ConstantMapping();
                string constantName = CodeIdentifier.MakeValid(enumeration.Value);
                constant.Name = constants.AddUnique(constantName, constant);
                constant.XmlName = enumeration.Value;
                constant.Value = i;
            }
            enumMapping.Constants = (ConstantMapping[])constants.ToArray(typeof(ConstantMapping));
            if (isList && enumMapping.Constants.Length > 63)
            {
                // if we have 64+ flag constants we cannot map the type to long enum, we will use string mapping instead.
                mapping = new PrimitiveMapping();
                mapping.TypeDesc = Scope.GetTypeDesc(typeof(string));
                mapping.TypeName = mapping.TypeDesc.DataType.Name;
                ImportedMappings.Add(dataType, mapping);
                return mapping;
            }
            ImportedMappings.Add(dataType, enumMapping);
            Scope.AddTypeMapping(enumMapping);
            return enumMapping;
        }

        private PrimitiveMapping ImportPrimitiveDataType(XmlSchemaSimpleType dataType)
        {
            TypeDesc sourceTypeDesc = GetDataTypeSource(dataType);
            PrimitiveMapping mapping = new PrimitiveMapping();
            mapping.TypeDesc = sourceTypeDesc;
            mapping.TypeName = sourceTypeDesc.DataType.Name;
            return mapping;
        }

        private PrimitiveMapping ImportNonXsdPrimitiveDataType(XmlSchemaSimpleType dataType, string ns)
        {
            PrimitiveMapping mapping = null;
            TypeDesc typeDesc = null;
            if (dataType.Name != null && dataType.Name.Length != 0)
            {
                typeDesc = Scope.GetTypeDesc(dataType.Name, ns);
                if (typeDesc != null)
                {
                    mapping = new PrimitiveMapping();
                    mapping.TypeDesc = typeDesc;
                    mapping.TypeName = typeDesc.DataType.Name;
                }
            }
            return mapping;
        }

        private TypeDesc GetDataTypeSource(XmlSchemaSimpleType dataType)
        {
            if (dataType.Name != null && dataType.Name.Length != 0)
            {
                TypeDesc typeDesc = Scope.GetTypeDesc(dataType);
                if (typeDesc != null) return typeDesc;
            }
            if (!dataType.DerivedFrom.IsEmpty)
            {
                return GetDataTypeSource(FindDataType(dataType.DerivedFrom));
            }
            return Scope.GetTypeDesc(typeof(string));
        }

        private XmlSchemaSimpleType FindDataType(XmlQualifiedName name)
        {
            TypeDesc typeDesc = Scope.GetTypeDesc(name.Name, name.Namespace);
            if (typeDesc != null && typeDesc.DataType is XmlSchemaSimpleType)
                return (XmlSchemaSimpleType)typeDesc.DataType;
            XmlSchemaSimpleType dataType = (XmlSchemaSimpleType)Schemas.Find(name, typeof(XmlSchemaSimpleType));
            if (dataType != null)
            {
                return dataType;
            }
            if (name.Namespace == XmlSchema.Namespace)
                return (XmlSchemaSimpleType)Scope.GetTypeDesc(typeof(string)).DataType;
            else
                throw new InvalidOperationException(SR.Format(SR.XmlMissingDataType, name.ToString()));
        }

        private object FindType(XmlQualifiedName name)
        {
            if (name != null && name.Namespace == Soap.Encoding)
            {
                // we have a build-in support fo the encoded types, we need to make sure that we generate the same
                // object model whether http://www.w3.org/2003/05/soap-encoding schema was specified or not.
                object type = Schemas.Find(name, typeof(XmlSchemaComplexType));
                if (type != null)
                {
                    XmlSchemaType encType = (XmlSchemaType)type;
                    XmlQualifiedName baseType = encType.DerivedFrom;
                    if (!baseType.IsEmpty)
                    {
                        return FindType(baseType);
                    }
                    return encType;
                }
                return FindDataType(name);
            }
            else
            {
                object type = Schemas.Find(name, typeof(XmlSchemaComplexType));
                if (type != null)
                {
                    return type;
                }
                return FindDataType(name);
            }
        }
    }
}
