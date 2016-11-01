// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Xml.Schema;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Xml.Serialization.Configuration;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Xml.Serialization.Advanced;

#if DEBUG
    using System.Diagnostics;
#endif

    /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaImporter : SchemaImporter
    {
        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.XmlSchemaImporter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaImporter(XmlSchemas schemas) : base(schemas, CodeGenerationOptions.GenerateProperties, null, new ImportContext()) { }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.XmlSchemaImporter1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaImporter(XmlSchemas schemas, CodeIdentifiers typeIdentifiers) : base(schemas, CodeGenerationOptions.GenerateProperties, null, new ImportContext(typeIdentifiers, false)) { }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.XmlSchemaImporter2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaImporter(XmlSchemas schemas, CodeIdentifiers typeIdentifiers, CodeGenerationOptions options) : base(schemas, options, null, new ImportContext(typeIdentifiers, false)) { }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.XmlSchemaImporter3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, ImportContext context) : base(schemas, options, null, context) { }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.XmlSchemaImporter4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlSchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, CodeDomProvider codeProvider, ImportContext context) : base(schemas, options, codeProvider, context)
        {
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportDerivedTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportDerivedTypeMapping(XmlQualifiedName name, Type baseType)
        {
            return ImportDerivedTypeMapping(name, baseType, false);
        }

        internal bool GenerateOrder
        {
            get { return (Options & CodeGenerationOptions.GenerateOrder) != 0; }
        }

        internal TypeMapping GetDefaultMapping(TypeFlags flags)
        {
            PrimitiveMapping mapping = new PrimitiveMapping();
            mapping.TypeDesc = Scope.GetTypeDesc("string", XmlSchema.Namespace, flags);
            mapping.TypeName = mapping.TypeDesc.DataType.Name;
            mapping.Namespace = XmlSchema.Namespace;
            return mapping;
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportDerivedTypeMapping1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportDerivedTypeMapping(XmlQualifiedName name, Type baseType, bool baseTypeCanBeIndirect)
        {
            ElementAccessor element = ImportElement(name, typeof(TypeMapping), baseType);

            if (element.Mapping is StructMapping)
            {
                MakeDerived((StructMapping)element.Mapping, baseType, baseTypeCanBeIndirect);
            }
            else if (baseType != null)
            {
                if (element.Mapping is ArrayMapping)
                {
                    // in the case of the ArrayMapping we can use the top-level StructMapping, because it does not have base base type
                    element.Mapping = ((ArrayMapping)element.Mapping).TopLevelMapping;
                    MakeDerived((StructMapping)element.Mapping, baseType, baseTypeCanBeIndirect);
                }
                else
                {
                    // Element '{0}' from namespace '{1}' is not a complex type and cannot be used as a {2}.
                    throw new InvalidOperationException(SR.Format(SR.XmlBadBaseElement, name.Name, name.Namespace, baseType.FullName));
                }
            }
            return new XmlTypeMapping(Scope, element);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportSchemaType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName)
        {
            return ImportSchemaType(typeName, null, false);
        }


        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportSchemaType1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName, Type baseType)
        {
            return ImportSchemaType(typeName, baseType, false);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportSchemaType2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName, Type baseType, bool baseTypeCanBeIndirect)
        {
            TypeMapping typeMapping = ImportType(typeName, typeof(TypeMapping), baseType, TypeFlags.CanBeElementValue, true);
            typeMapping.ReferencedByElement = false;

            ElementAccessor accessor = new ElementAccessor();
            accessor.IsTopLevelInSchema = true; // false
            accessor.Name = typeName.Name;
            accessor.Namespace = typeName.Namespace;
            accessor.Mapping = typeMapping;

            if (typeMapping is SpecialMapping && ((SpecialMapping)typeMapping).NamedAny)
                accessor.Any = true;
            accessor.IsNullable = typeMapping.TypeDesc.IsNullable;
            accessor.Form = XmlSchemaForm.Qualified;

            if (accessor.Mapping is StructMapping)
            {
                MakeDerived((StructMapping)accessor.Mapping, baseType, baseTypeCanBeIndirect);
            }
            else if (baseType != null)
            {
                if (accessor.Mapping is ArrayMapping)
                {
                    // in the case of the ArrayMapping we can use the top-level StructMapping, because it does not have base base type
                    accessor.Mapping = ((ArrayMapping)accessor.Mapping).TopLevelMapping;
                    MakeDerived((StructMapping)accessor.Mapping, baseType, baseTypeCanBeIndirect);
                }
                else
                {
                    // Type '{0}' from namespace '{1}' is not a complex type and cannot be used as a {2}.
                    throw new InvalidOperationException(SR.Format(SR.XmlBadBaseType, typeName.Name, typeName.Namespace, baseType.FullName));
                }
            }
            return new XmlTypeMapping(Scope, accessor);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeMapping ImportTypeMapping(XmlQualifiedName name)
        {
            return ImportDerivedTypeMapping(name, null);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportMembersMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(XmlQualifiedName name)
        {
            return new XmlMembersMapping(Scope, ImportElement(name, typeof(MembersMapping), null), XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportAnyType"]/*' />
        public XmlMembersMapping ImportAnyType(XmlQualifiedName typeName, string elementName)
        {
            TypeMapping typeMapping = ImportType(typeName, typeof(MembersMapping), null, TypeFlags.CanBeElementValue, true);
            MembersMapping mapping = typeMapping as MembersMapping;

            if (mapping == null)
            {
                XmlSchemaComplexType type = new XmlSchemaComplexType();
                XmlSchemaSequence seq = new XmlSchemaSequence();
                type.Particle = seq;
                XmlSchemaElement element = new XmlSchemaElement();
                element.Name = elementName;
                element.SchemaTypeName = typeName;
                seq.Items.Add(element);
                mapping = ImportMembersType(type, typeName.Namespace, elementName);
            }

            if (mapping.Members.Length != 1 || !mapping.Members[0].Accessor.Any)
                return null;
            mapping.Members[0].Name = elementName;
            ElementAccessor accessor = new ElementAccessor();
            accessor.Name = elementName;
            accessor.Namespace = typeName.Namespace;
            accessor.Mapping = mapping;
            accessor.Any = true;

            XmlSchemaObject xso = Schemas.SchemaSet.GlobalTypes[typeName];
            if (xso != null)
            {
                XmlSchema schema = xso.Parent as XmlSchema;
                if (schema != null)
                {
                    accessor.Form = schema.ElementFormDefault == XmlSchemaForm.None ? XmlSchemaForm.Unqualified : schema.ElementFormDefault;
                }
            }
            XmlMembersMapping members = new XmlMembersMapping(Scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
            return members;
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportMembersMapping1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(XmlQualifiedName[] names)
        {
            return ImportMembersMapping(names, null, false);
        }

        /// <include file='doc\XmlSchemaImporter.uex' path='docs/doc[@for="XmlSchemaImporter.ImportMembersMapping2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMembersMapping ImportMembersMapping(XmlQualifiedName[] names, Type baseType, bool baseTypeCanBeIndirect)
        {
            CodeIdentifiers memberScope = new CodeIdentifiers();
            memberScope.UseCamelCasing = true;
            MemberMapping[] members = new MemberMapping[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                XmlQualifiedName name = names[i];
                ElementAccessor accessor = ImportElement(name, typeof(TypeMapping), baseType);
                if (baseType != null && accessor.Mapping is StructMapping)
                    MakeDerived((StructMapping)accessor.Mapping, baseType, baseTypeCanBeIndirect);

                MemberMapping member = new MemberMapping();
                member.Name = CodeIdentifier.MakeValid(Accessor.UnescapeName(accessor.Name));
                member.Name = memberScope.AddUnique(member.Name, member);
                member.TypeDesc = accessor.Mapping.TypeDesc;
                member.Elements = new ElementAccessor[] { accessor };
                members[i] = member;
            }
            MembersMapping mapping = new MembersMapping();
            mapping.HasWrapperElement = false;
            mapping.TypeDesc = Scope.GetTypeDesc(typeof(object[]));
            mapping.Members = members;
            ElementAccessor element = new ElementAccessor();
            element.Mapping = mapping;
            return new XmlMembersMapping(Scope, element, XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        public XmlMembersMapping ImportMembersMapping(string name, string ns, SoapSchemaMember[] members)
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
            MembersMapping mapping = ImportMembersType(type, null, name);

            ElementAccessor accessor = new ElementAccessor();
            accessor.Name = Accessor.EscapeName(name);
            accessor.Namespace = ns;
            accessor.Mapping = mapping;
            accessor.IsNullable = false;
            accessor.Form = XmlSchemaForm.Qualified;
            return new XmlMembersMapping(Scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        private ElementAccessor ImportElement(XmlQualifiedName name, Type desiredMappingType, Type baseType)
        {
            XmlSchemaElement element = FindElement(name);
            ElementAccessor accessor = (ElementAccessor)ImportedElements[element];
            if (accessor != null) return accessor;
            accessor = ImportElement(element, string.Empty, desiredMappingType, baseType, name.Namespace, true);
            ElementAccessor existing = (ElementAccessor)ImportedElements[element];
            if (existing != null)
            {
                return existing;
            }
            ImportedElements.Add(element, accessor);
            return accessor;
        }

        private ElementAccessor ImportElement(XmlSchemaElement element, string identifier, Type desiredMappingType, Type baseType, string ns, bool topLevelElement)
        {
            if (!element.RefName.IsEmpty)
            {
                // we cannot re-use the accessor for the element refs
                ElementAccessor topAccessor = ImportElement(element.RefName, desiredMappingType, baseType);
                if (element.IsMultipleOccurrence && topAccessor.Mapping is ArrayMapping)
                {
                    ElementAccessor refAccessor = topAccessor.Clone();
                    refAccessor.IsTopLevelInSchema = false;
                    refAccessor.Mapping.ReferencedByElement = true;
                    return refAccessor;
                }
                return topAccessor;
            }

            if (element.Name.Length == 0)
            {
                XmlQualifiedName parentType = XmlSchemas.GetParentName(element);
                throw new InvalidOperationException(SR.Format(SR.XmlElementHasNoName, parentType.Name, parentType.Namespace));
            }
            string unescapedName = Accessor.UnescapeName(element.Name);
            if (identifier.Length == 0)
                identifier = CodeIdentifier.MakeValid(unescapedName);
            else
                identifier += CodeIdentifier.MakePascal(unescapedName);
            TypeMapping mapping = ImportElementType(element, identifier, desiredMappingType, baseType, ns);
            ElementAccessor accessor = new ElementAccessor();
            accessor.IsTopLevelInSchema = element.Parent is XmlSchema;
            accessor.Name = element.Name;
            accessor.Namespace = ns;
            accessor.Mapping = mapping;
            accessor.IsOptional = element.MinOccurs == 0m;

            if (element.DefaultValue != null)
            {
                accessor.Default = element.DefaultValue;
            }
            else if (element.FixedValue != null)
            {
                accessor.Default = element.FixedValue;
                accessor.IsFixed = true;
            }

            if (mapping is SpecialMapping && ((SpecialMapping)mapping).NamedAny)
                accessor.Any = true;
            accessor.IsNullable = element.IsNillable;
            if (topLevelElement)
            {
                accessor.Form = XmlSchemaForm.Qualified;
            }
            else
            {
                accessor.Form = ElementForm(ns, element);
            }
            return accessor;
        }

        private TypeMapping ImportElementType(XmlSchemaElement element, string identifier, Type desiredMappingType, Type baseType, string ns)
        {
            TypeMapping mapping;
            if (!element.SchemaTypeName.IsEmpty)
            {
                mapping = ImportType(element.SchemaTypeName, desiredMappingType, baseType, TypeFlags.CanBeElementValue, false);
                if (!mapping.ReferencedByElement)
                {
                    object type = FindType(element.SchemaTypeName, TypeFlags.CanBeElementValue);
                    XmlSchemaObject parent = element;
                    while (parent.Parent != null && type != parent)
                    {
                        parent = parent.Parent;
                    }
                    mapping.ReferencedByElement = (type != parent);
                }
            }
            else if (element.SchemaType != null)
            {
                if (element.SchemaType is XmlSchemaComplexType)
                    mapping = ImportType((XmlSchemaComplexType)element.SchemaType, ns, identifier, desiredMappingType, baseType, TypeFlags.CanBeElementValue);
                else
                    mapping = ImportDataType((XmlSchemaSimpleType)element.SchemaType, ns, identifier, baseType, TypeFlags.CanBeElementValue | TypeFlags.CanBeAttributeValue | TypeFlags.CanBeTextValue, false);
                mapping.ReferencedByElement = true;
            }
            else if (!element.SubstitutionGroup.IsEmpty)
                mapping = ImportElementType(FindElement(element.SubstitutionGroup), identifier, desiredMappingType, baseType, ns);
            else
            {
                if (desiredMappingType == typeof(MembersMapping))
                {
                    mapping = ImportMembersType(new XmlSchemaType(), ns, identifier);
                }
                else
                {
                    mapping = ImportRootMapping();
                }
            }
            if (!(desiredMappingType.IsAssignableFrom(mapping.GetType())))
                throw new InvalidOperationException(SR.Format(SR.XmlElementImportedTwice, element.Name, ns, mapping.GetType().Name, desiredMappingType.Name));

            // let the extensions to run
            if (!mapping.TypeDesc.IsMappedType)
            {
                RunSchemaExtensions(mapping, element.SchemaTypeName, element.SchemaType, element, TypeFlags.CanBeElementValue);
            }
            return mapping;
        }

        private void RunSchemaExtensions(TypeMapping mapping, XmlQualifiedName qname, XmlSchemaType type, XmlSchemaObject context, TypeFlags flags)
        {
            string typeName = null;
            SchemaImporterExtension typeOwner = null;
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace mainNamespace = new CodeNamespace();
            compileUnit.Namespaces.Add(mainNamespace);

            if (!qname.IsEmpty)
            {
                typeName = FindExtendedType(qname.Name, qname.Namespace, context, compileUnit, mainNamespace, out typeOwner);
            }
            else if (type != null)
            {
                typeName = FindExtendedType(type, context, compileUnit, mainNamespace, out typeOwner);
            }
            else if (context is XmlSchemaAny)
            {
                typeName = FindExtendedAnyElement((XmlSchemaAny)context, ((flags & TypeFlags.CanBeTextValue) != 0), compileUnit, mainNamespace, out typeOwner);
            }

            if (typeName != null && typeName.Length > 0)
            {
                // check if the type name is valid 
                typeName = typeName.Replace('+', '.');
                try
                {
                    CodeGenerator.ValidateIdentifiers(new CodeTypeReference(typeName));
                }
                catch (ArgumentException)
                {
                    if (qname.IsEmpty)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlImporterExtensionBadLocalTypeName, typeOwner.GetType().FullName, typeName));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlImporterExtensionBadTypeName, typeOwner.GetType().FullName, qname.Name, qname.Namespace, typeName));
                    }
                }
                // UNDONE: check if it in use
                //CodeIdentifiers.IsInUse
                //TypeIdentifiers.AddUnique(typeName, typeName);
                foreach (CodeNamespace ns in compileUnit.Namespaces)
                {
                    CodeGenerator.ValidateIdentifiers(ns);
                }
                // UNDONE compile
                //mapping.TypeName = typeName;
                mapping.TypeDesc = mapping.TypeDesc.CreateMappedTypeDesc(new MappedTypeDesc(typeName, qname.Name, qname.Namespace, type, context, typeOwner, mainNamespace, compileUnit.ReferencedAssemblies));

                if (mapping is ArrayMapping)
                {
                    TypeMapping top = ((ArrayMapping)mapping).TopLevelMapping;
                    top.TypeName = mapping.TypeName;
                    top.TypeDesc = mapping.TypeDesc;
                }
                else
                {
                    mapping.TypeName = qname.IsEmpty ? null : typeName;
                }
            }
        }
        private string GenerateUniqueTypeName(string desiredName, string ns)
        {
            int i = 1;

            string typeName = desiredName;
            while (true)
            {
                XmlQualifiedName qname = new XmlQualifiedName(typeName, ns);

                object type = Schemas.Find(qname, typeof(XmlSchemaType));
                if (type == null)
                {
                    break;
                }
                typeName = desiredName + i.ToString(CultureInfo.InvariantCulture);
                i++;
            }
            typeName = CodeIdentifier.MakeValid(typeName);
            return TypeIdentifiers.AddUnique(typeName, typeName);
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
                        if (type.DerivedFrom == baseName && TypesInUse[type.Name, schema.TargetNamespace] == null)
                        {
                            ImportType(type.QualifiedName, typeof(TypeMapping), null, TypeFlags.CanBeElementValue, false);
                        }
                    }
                }
            }
        }

        private TypeMapping ImportType(XmlQualifiedName name, Type desiredMappingType, Type baseType, TypeFlags flags, bool addref)
        {
            if (name.Name == Soap.UrType && name.Namespace == XmlSchema.Namespace)
                return ImportRootMapping();
            object type = FindType(name, flags);

            TypeMapping mapping = (TypeMapping)ImportedMappings[type];
            if (mapping != null && desiredMappingType.IsAssignableFrom(mapping.GetType()))
                return mapping;

            if (addref)
                AddReference(name, TypesInUse, SR.XmlCircularTypeReference);
            if (type is XmlSchemaComplexType)
            {
                mapping = ImportType((XmlSchemaComplexType)type, name.Namespace, name.Name, desiredMappingType, baseType, flags);
            }
            else if (type is XmlSchemaSimpleType)
                mapping = ImportDataType((XmlSchemaSimpleType)type, name.Namespace, name.Name, baseType, flags, false);
            else
                throw new InvalidOperationException(SR.XmlInternalError);

            if (addref && name.Namespace != XmlSchema.Namespace)
                RemoveReference(name, TypesInUse);

            return mapping;
        }

        private TypeMapping ImportType(XmlSchemaComplexType type, string typeNs, string identifier, Type desiredMappingType, Type baseType, TypeFlags flags)
        {
            if (type.Redefined != null)
            {
                // we do not support redefine in the current version
                throw new NotSupportedException(SR.Format(SR.XmlUnsupportedRedefine, type.Name, typeNs));
            }
            if (desiredMappingType == typeof(TypeMapping))
            {
                TypeMapping mapping = null;

                if (baseType == null)
                {
                    if ((mapping = ImportArrayMapping(type, identifier, typeNs, false)) == null)
                    {
                        mapping = ImportAnyMapping(type, identifier, typeNs, false);
                    }
                }
                if (mapping == null)
                {
                    mapping = ImportStructType(type, typeNs, identifier, baseType, false);

                    if (mapping != null && type.Name != null && type.Name.Length != 0)
                        ImportDerivedTypes(new XmlQualifiedName(identifier, typeNs));
                }
                return mapping;
            }
            else if (desiredMappingType == typeof(MembersMapping))
                return ImportMembersType(type, typeNs, identifier);
            else
                throw new ArgumentException(SR.XmlInternalError, nameof(desiredMappingType));
        }

        private MembersMapping ImportMembersType(XmlSchemaType type, string typeNs, string identifier)
        {
            if (!type.DerivedFrom.IsEmpty) throw new InvalidOperationException(SR.XmlMembersDeriveError);
            CodeIdentifiers memberScope = new CodeIdentifiers();
            memberScope.UseCamelCasing = true;
            bool needExplicitOrder = false;
            MemberMapping[] members = ImportTypeMembers(type, typeNs, identifier, memberScope, new CodeIdentifiers(), new NameTable(), ref needExplicitOrder, false, false);
            MembersMapping mappings = new MembersMapping();
            mappings.HasWrapperElement = true;
            mappings.TypeDesc = Scope.GetTypeDesc(typeof(object[]));
            mappings.Members = members;
            return mappings;
        }

        private StructMapping ImportStructType(XmlSchemaType type, string typeNs, string identifier, Type baseType, bool arrayLike)
        {
            TypeDesc baseTypeDesc = null;
            TypeMapping baseMapping = null;

            bool isRootType = false;
            if (!type.DerivedFrom.IsEmpty)
            {
                baseMapping = ImportType(type.DerivedFrom, typeof(TypeMapping), null, TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue, false);

                if (baseMapping is StructMapping)
                    baseTypeDesc = ((StructMapping)baseMapping).TypeDesc;
                else if (baseMapping is ArrayMapping)
                {
                    baseMapping = ((ArrayMapping)baseMapping).TopLevelMapping;
                    if (baseMapping != null)
                    {
                        baseMapping.ReferencedByTopLevelElement = false;
                        baseMapping.ReferencedByElement = true;
                        baseTypeDesc = baseMapping.TypeDesc;
                    }
                }
                else
                    baseMapping = null;
            }
            if (baseTypeDesc == null && baseType != null)
                baseTypeDesc = Scope.GetTypeDesc(baseType);
            if (baseMapping == null)
            {
                baseMapping = GetRootMapping();
                isRootType = true;
            }
            Mapping previousMapping = (Mapping)ImportedMappings[type];
            if (previousMapping != null)
            {
                if (previousMapping is StructMapping)
                {
                    return (StructMapping)previousMapping;
                }
                else if (arrayLike && previousMapping is ArrayMapping)
                {
                    ArrayMapping arrayMapping = (ArrayMapping)previousMapping;
                    if (arrayMapping.TopLevelMapping != null)
                    {
                        return arrayMapping.TopLevelMapping;
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlTypeUsedTwice, type.QualifiedName.Name, type.QualifiedName.Namespace));
                }
            }
            StructMapping structMapping = new StructMapping();
            structMapping.IsReference = Schemas.IsReference(type);
            TypeFlags flags = TypeFlags.Reference;
            if (type is XmlSchemaComplexType)
            {
                if (((XmlSchemaComplexType)type).IsAbstract)
                    flags |= TypeFlags.Abstract;
            }

            identifier = Accessor.UnescapeName(identifier);
            string typeName = type.Name == null || type.Name.Length == 0 ? GenerateUniqueTypeName(identifier, typeNs) : GenerateUniqueTypeName(identifier);
            structMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Struct, baseTypeDesc, flags);
            structMapping.Namespace = typeNs;
            structMapping.TypeName = type.Name == null || type.Name.Length == 0 ? null : identifier;
            structMapping.BaseMapping = (StructMapping)baseMapping;
            if (!arrayLike)
                ImportedMappings.Add(type, structMapping);
            CodeIdentifiers members = new CodeIdentifiers();
            CodeIdentifiers membersScope = structMapping.BaseMapping.Scope.Clone();
            members.AddReserved(typeName);
            membersScope.AddReserved(typeName);
            AddReservedIdentifiersForDataBinding(members);
            if (isRootType)
                AddReservedIdentifiersForDataBinding(membersScope);
            bool needExplicitOrder = false;
            structMapping.Members = ImportTypeMembers(type, typeNs, identifier, members, membersScope, structMapping, ref needExplicitOrder, true, true);

            if (!IsAllGroup(type))
            {
                if (needExplicitOrder && !GenerateOrder)
                {
                    structMapping.SetSequence();
                }
                else if (GenerateOrder)
                {
                    structMapping.IsSequence = true;
                }
            }

            for (int i = 0; i < structMapping.Members.Length; i++)
            {
                StructMapping declaringMapping;
                MemberMapping baseMember = ((StructMapping)baseMapping).FindDeclaringMapping(structMapping.Members[i], out declaringMapping, structMapping.TypeName);
                if (baseMember != null && baseMember.TypeDesc != structMapping.Members[i].TypeDesc)
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalOverride, type.Name, baseMember.Name, baseMember.TypeDesc.FullName, structMapping.Members[i].TypeDesc.FullName, declaringMapping.TypeDesc.FullName));
            }
            structMapping.Scope = membersScope;
            Scope.AddTypeMapping(structMapping);
            return structMapping;
        }

        private bool IsAllGroup(XmlSchemaType type)
        {
            TypeItems items = GetTypeItems(type);
            return (items.Particle != null) && (items.Particle is XmlSchemaAll);
        }

        private StructMapping ImportStructDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, Type baseType)
        {
            identifier = Accessor.UnescapeName(identifier);
            string typeName = GenerateUniqueTypeName(identifier);
            StructMapping structMapping = new StructMapping();
            structMapping.IsReference = Schemas.IsReference(dataType);
            TypeFlags flags = TypeFlags.Reference;
            TypeDesc baseTypeDesc = Scope.GetTypeDesc(baseType);
            structMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Struct, baseTypeDesc, flags);
            structMapping.Namespace = typeNs;
            structMapping.TypeName = identifier;
            CodeIdentifiers members = new CodeIdentifiers();
            members.AddReserved(typeName);
            AddReservedIdentifiersForDataBinding(members);
            ImportTextMember(members, new CodeIdentifiers(), null);
            structMapping.Members = (MemberMapping[])members.ToArray(typeof(MemberMapping));
            structMapping.Scope = members;
            Scope.AddTypeMapping(structMapping);
            return structMapping;
        }

        private class TypeItems
        {
            internal XmlSchemaObjectCollection Attributes = new XmlSchemaObjectCollection();
            internal XmlSchemaAnyAttribute AnyAttribute;
            internal XmlSchemaGroupBase Particle;
            internal XmlQualifiedName baseSimpleType;
            internal bool IsUnbounded;
        }

        private MemberMapping[] ImportTypeMembers(XmlSchemaType type, string typeNs, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, ref bool needExplicitOrder, bool order, bool allowUnboundedElements)
        {
            TypeItems items = GetTypeItems(type);
            bool mixed = IsMixed(type);

            if (mixed)
            {
                // check if we can transfer the attribute to the base class
                XmlSchemaType t = type;
                while (!t.DerivedFrom.IsEmpty)
                {
                    t = FindType(t.DerivedFrom, TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue);
                    if (IsMixed(t))
                    {
                        // keep the mixed attribute on the base class
                        mixed = false;
                        break;
                    }
                }
            }

            if (items.Particle != null)
            {
                ImportGroup(items.Particle, identifier, members, membersScope, elementsScope, typeNs, mixed, ref needExplicitOrder, order, items.IsUnbounded, allowUnboundedElements);
            }
            for (int i = 0; i < items.Attributes.Count; i++)
            {
                object item = items.Attributes[i];
                if (item is XmlSchemaAttribute)
                {
                    ImportAttributeMember((XmlSchemaAttribute)item, identifier, members, membersScope, typeNs);
                }
                else if (item is XmlSchemaAttributeGroupRef)
                {
                    XmlQualifiedName groupName = ((XmlSchemaAttributeGroupRef)item).RefName;
                    ImportAttributeGroupMembers(FindAttributeGroup(groupName), identifier, members, membersScope, groupName.Namespace);
                }
            }
            if (items.AnyAttribute != null)
            {
                ImportAnyAttributeMember(items.AnyAttribute, members, membersScope);
            }

            if (items.baseSimpleType != null || (items.Particle == null && mixed))
            {
                ImportTextMember(members, membersScope, mixed ? null : items.baseSimpleType);
            }

            ImportXmlnsDeclarationsMember(type, members, membersScope);
            MemberMapping[] typeMembers = (MemberMapping[])members.ToArray(typeof(MemberMapping));
            return typeMembers;
        }

        internal static bool IsMixed(XmlSchemaType type)
        {
            if (!(type is XmlSchemaComplexType))
                return false;

            XmlSchemaComplexType ct = (XmlSchemaComplexType)type;
            bool mixed = ct.IsMixed;

            // check the mixed attribute on the complexContent
            if (!mixed)
            {
                if (ct.ContentModel != null && ct.ContentModel is XmlSchemaComplexContent)
                {
                    mixed = ((XmlSchemaComplexContent)ct.ContentModel).IsMixed;
                }
            }
            return mixed;
        }

        private TypeItems GetTypeItems(XmlSchemaType type)
        {
            TypeItems items = new TypeItems();
            if (type is XmlSchemaComplexType)
            {
                XmlSchemaParticle particle = null;
                XmlSchemaComplexType ct = (XmlSchemaComplexType)type;
                if (ct.ContentModel != null)
                {
                    XmlSchemaContent content = ct.ContentModel.Content;
                    if (content is XmlSchemaComplexContentExtension)
                    {
                        XmlSchemaComplexContentExtension extension = (XmlSchemaComplexContentExtension)content;
                        items.Attributes = extension.Attributes;
                        items.AnyAttribute = extension.AnyAttribute;
                        particle = extension.Particle;
                    }
                    else if (content is XmlSchemaSimpleContentExtension)
                    {
                        XmlSchemaSimpleContentExtension extension = (XmlSchemaSimpleContentExtension)content;
                        items.Attributes = extension.Attributes;
                        items.AnyAttribute = extension.AnyAttribute;
                        items.baseSimpleType = extension.BaseTypeName;
                    }
                }
                else
                {
                    items.Attributes = ct.Attributes;
                    items.AnyAttribute = ct.AnyAttribute;
                    particle = ct.Particle;
                }
                if (particle is XmlSchemaGroupRef)
                {
                    XmlSchemaGroupRef refGroup = (XmlSchemaGroupRef)particle;
                    items.Particle = FindGroup(refGroup.RefName).Particle;
                    items.IsUnbounded = particle.IsMultipleOccurrence;
                }
                else if (particle is XmlSchemaGroupBase)
                {
                    items.Particle = (XmlSchemaGroupBase)particle;
                    items.IsUnbounded = particle.IsMultipleOccurrence;
                }
            }
            return items;
        }

        private void ImportGroup(XmlSchemaGroupBase group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool mixed, ref bool needExplicitOrder, bool allowDuplicates, bool groupRepeats, bool allowUnboundedElements)
        {
            if (group is XmlSchemaChoice)
                ImportChoiceGroup((XmlSchemaChoice)group, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
            else
                ImportGroupMembers(group, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref mixed, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);

            if (mixed)
            {
                ImportTextMember(members, membersScope, null);
            }
        }

        private MemberMapping ImportChoiceGroup(XmlSchemaGroupBase group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool groupRepeats, ref bool needExplicitOrder, bool allowDuplicates)
        {
            NameTable choiceElements = new NameTable();
            if (GatherGroupChoices(group, choiceElements, identifier, ns, ref needExplicitOrder, allowDuplicates))
                groupRepeats = true;
            MemberMapping member = new MemberMapping();
            member.Elements = (ElementAccessor[])choiceElements.ToArray(typeof(ElementAccessor));
            Array.Sort(member.Elements, new ElementComparer());

            AddScopeElements(elementsScope, member.Elements, ref needExplicitOrder, allowDuplicates);
            bool duplicateTypes = false;
            bool nullableMismatch = false;
            Hashtable uniqueTypeDescs = new Hashtable(member.Elements.Length);

            for (int i = 0; i < member.Elements.Length; i++)
            {
                ElementAccessor element = member.Elements[i];
                string tdFullName = element.Mapping.TypeDesc.FullName;
                object val = uniqueTypeDescs[tdFullName];
                if (val != null)
                {
                    duplicateTypes = true;
                    ElementAccessor existingElement = (ElementAccessor)val;
                    if (!nullableMismatch && existingElement.IsNullable != element.IsNullable)
                        nullableMismatch = true;
                }
                else
                {
                    uniqueTypeDescs.Add(tdFullName, element);
                }

                ArrayMapping arrayMapping = element.Mapping as ArrayMapping;
                if (arrayMapping != null)
                {
                    if (IsNeedXmlSerializationAttributes(arrayMapping))
                    {
                        // we cannot use ArrayMapping in choice if additional custom 
                        // serialization attributes are needed to serialize it
                        element.Mapping = arrayMapping.TopLevelMapping;
                        element.Mapping.ReferencedByTopLevelElement = false;
                        element.Mapping.ReferencedByElement = true;
                    }
                }
            }
            if (nullableMismatch)
                member.TypeDesc = Scope.GetTypeDesc(typeof(object));
            else
            {
                TypeDesc[] typeDescs = new TypeDesc[uniqueTypeDescs.Count];
                IEnumerator enumerator = uniqueTypeDescs.Values.GetEnumerator();
                for (int i = 0; i < typeDescs.Length; i++)
                {
                    if (!enumerator.MoveNext())
                        break;
                    typeDescs[i] = ((ElementAccessor)enumerator.Current).Mapping.TypeDesc;
                }
                member.TypeDesc = TypeDesc.FindCommonBaseTypeDesc(typeDescs);
                if (member.TypeDesc == null) member.TypeDesc = Scope.GetTypeDesc(typeof(object));
            }

            if (groupRepeats)
                member.TypeDesc = member.TypeDesc.CreateArrayTypeDesc();

            if (membersScope != null)
            {
                member.Name = membersScope.AddUnique(groupRepeats ? "Items" : "Item", member);
                if (members != null)
                {
                    members.Add(member.Name, member);
                }
            }

            if (duplicateTypes)
            {
                member.ChoiceIdentifier = new ChoiceIdentifierAccessor();
                member.ChoiceIdentifier.MemberName = member.Name + "ElementName";
                // we need to create the EnumMapping to store all of the element names
                member.ChoiceIdentifier.Mapping = ImportEnumeratedChoice(member.Elements, ns, member.Name + "ChoiceType");
                member.ChoiceIdentifier.MemberIds = new string[member.Elements.Length];
                ConstantMapping[] constants = ((EnumMapping)member.ChoiceIdentifier.Mapping).Constants;
                for (int i = 0; i < member.Elements.Length; i++)
                {
                    member.ChoiceIdentifier.MemberIds[i] = constants[i].Name;
                }
                MemberMapping choiceIdentifier = new MemberMapping();
                choiceIdentifier.Ignore = true;
                choiceIdentifier.Name = member.ChoiceIdentifier.MemberName;
                if (groupRepeats)
                {
                    choiceIdentifier.TypeDesc = member.ChoiceIdentifier.Mapping.TypeDesc.CreateArrayTypeDesc();
                }
                else
                {
                    choiceIdentifier.TypeDesc = member.ChoiceIdentifier.Mapping.TypeDesc;
                }

                // create element accessor for the choiceIdentifier

                ElementAccessor choiceAccessor = new ElementAccessor();
                choiceAccessor.Name = choiceIdentifier.Name;
                choiceAccessor.Namespace = ns;
                choiceAccessor.Mapping = member.ChoiceIdentifier.Mapping;
                choiceIdentifier.Elements = new ElementAccessor[] { choiceAccessor };

                if (membersScope != null)
                {
                    choiceAccessor.Name = choiceIdentifier.Name = member.ChoiceIdentifier.MemberName = membersScope.AddUnique(member.ChoiceIdentifier.MemberName, choiceIdentifier);
                    if (members != null)
                    {
                        members.Add(choiceAccessor.Name, choiceIdentifier);
                    }
                }
            }
            return member;
        }

        private bool IsNeedXmlSerializationAttributes(ArrayMapping arrayMapping)
        {
            if (arrayMapping.Elements.Length != 1)
                return true;

            ElementAccessor item = arrayMapping.Elements[0];
            TypeMapping itemMapping = item.Mapping;

            if (item.Name != itemMapping.DefaultElementName)
                return true;

            if (item.Form != XmlSchemaForm.None && item.Form != XmlSchemaExporter.elementFormDefault)
                return true;

            if (item.Mapping.TypeDesc != null)
            {
                if (item.IsNullable != item.Mapping.TypeDesc.IsNullable)
                    return true;

                if (item.Mapping.TypeDesc.IsAmbiguousDataType)
                    return true;
            }
            return false;
        }

        private bool GatherGroupChoices(XmlSchemaGroup group, NameTable choiceElements, string identifier, string ns, ref bool needExplicitOrder, bool allowDuplicates)
        {
            return GatherGroupChoices(group.Particle, choiceElements, identifier, ns, ref needExplicitOrder, allowDuplicates);
        }

        private bool GatherGroupChoices(XmlSchemaParticle particle, NameTable choiceElements, string identifier, string ns, ref bool needExplicitOrder, bool allowDuplicates)
        {
            if (particle is XmlSchemaGroupRef)
            {
                XmlSchemaGroupRef refGroup = (XmlSchemaGroupRef)particle;
                if (!refGroup.RefName.IsEmpty)
                {
                    AddReference(refGroup.RefName, GroupsInUse, SR.XmlCircularGroupReference);
                    if (GatherGroupChoices(FindGroup(refGroup.RefName), choiceElements, identifier, refGroup.RefName.Namespace, ref needExplicitOrder, allowDuplicates))
                    {
                        RemoveReference(refGroup.RefName, GroupsInUse);
                        return true;
                    }
                    RemoveReference(refGroup.RefName, GroupsInUse);
                }
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase group = (XmlSchemaGroupBase)particle;
                bool groupRepeats = group.IsMultipleOccurrence;
                XmlSchemaAny any = null;
                bool duplicateElements = false;
                for (int i = 0; i < group.Items.Count; i++)
                {
                    object item = group.Items[i];
                    if (item is XmlSchemaGroupBase || item is XmlSchemaGroupRef)
                    {
                        if (GatherGroupChoices((XmlSchemaParticle)item, choiceElements, identifier, ns, ref needExplicitOrder, allowDuplicates))
                            groupRepeats = true;
                    }
                    else if (item is XmlSchemaAny)
                    {
                        if (GenerateOrder)
                        {
                            AddScopeElements(choiceElements, ImportAny((XmlSchemaAny)item, true, ns), ref duplicateElements, allowDuplicates);
                        }
                        else
                        {
                            any = (XmlSchemaAny)item;
                        }
                    }
                    else if (item is XmlSchemaElement)
                    {
                        XmlSchemaElement element = (XmlSchemaElement)item;
                        XmlSchemaElement headElement = GetTopLevelElement(element);
                        if (headElement != null)
                        {
                            XmlSchemaElement[] elements = GetEquivalentElements(headElement);
                            for (int j = 0; j < elements.Length; j++)
                            {
                                if (elements[j].IsMultipleOccurrence) groupRepeats = true;
                                AddScopeElement(choiceElements, ImportElement(elements[j], identifier, typeof(TypeMapping), null, elements[j].QualifiedName.Namespace, true), ref duplicateElements, allowDuplicates);
                            }
                        }
                        if (element.IsMultipleOccurrence) groupRepeats = true;
                        AddScopeElement(choiceElements, ImportElement(element, identifier, typeof(TypeMapping), null, element.QualifiedName.Namespace, false), ref duplicateElements, allowDuplicates);
                    }
                }
                if (any != null)
                {
                    AddScopeElements(choiceElements, ImportAny(any, true, ns), ref duplicateElements, allowDuplicates);
                }
                if (!groupRepeats && !(group is XmlSchemaChoice) && group.Items.Count > 1)
                {
                    groupRepeats = true;
                }
                return groupRepeats;
            }
            return false;
        }

        private void AddScopeElement(INameScope scope, ElementAccessor element, ref bool duplicateElements, bool allowDuplicates)
        {
            if (scope == null)
                return;

            ElementAccessor scopeElement = (ElementAccessor)scope[element.Name, element.Namespace];
            if (scopeElement != null)
            {
                if (!allowDuplicates)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlDuplicateElementInScope, element.Name, element.Namespace));
                }
                if (scopeElement.Mapping.TypeDesc != element.Mapping.TypeDesc)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlDuplicateElementInScope1, element.Name, element.Namespace));
                }
                duplicateElements = true;
            }
            else
            {
                scope[element.Name, element.Namespace] = element;
            }
        }

        private void AddScopeElements(INameScope scope, ElementAccessor[] elements, ref bool duplicateElements, bool allowDuplicates)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                AddScopeElement(scope, elements[i], ref duplicateElements, allowDuplicates);
            }
        }

        private void ImportGroupMembers(XmlSchemaParticle particle, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool groupRepeats, ref bool mixed, ref bool needExplicitOrder, bool allowDuplicates, bool allowUnboundedElements)
        {
            if (particle is XmlSchemaGroupRef)
            {
                XmlSchemaGroupRef refGroup = (XmlSchemaGroupRef)particle;
                if (!refGroup.RefName.IsEmpty)
                {
                    AddReference(refGroup.RefName, GroupsInUse, SR.XmlCircularGroupReference);
                    ImportGroupMembers(FindGroup(refGroup.RefName).Particle, identifier, members, membersScope, elementsScope, refGroup.RefName.Namespace, groupRepeats | refGroup.IsMultipleOccurrence, ref mixed, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);
                    RemoveReference(refGroup.RefName, GroupsInUse);
                }
            }
            else if (particle is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase group = (XmlSchemaGroupBase)particle;

                if (group.IsMultipleOccurrence)
                    groupRepeats = true;

                if (GenerateOrder && groupRepeats && group.Items.Count > 1)
                {
                    ImportChoiceGroup(group, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
                }
                else
                {
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        object item = group.Items[i];
                        if (item is XmlSchemaChoice)
                            ImportChoiceGroup((XmlSchemaGroupBase)item, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
                        else if (item is XmlSchemaElement)
                            ImportElementMember((XmlSchemaElement)item, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);
                        else if (item is XmlSchemaAny)
                        {
                            ImportAnyMember((XmlSchemaAny)item, identifier, members, membersScope, elementsScope, ns, ref mixed, ref needExplicitOrder, allowDuplicates);
                        }
                        else if (item is XmlSchemaParticle)
                        {
                            ImportGroupMembers((XmlSchemaParticle)item, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref mixed, ref needExplicitOrder, allowDuplicates, true);
                        }
                    }
                }
            }
        }

        private XmlSchemaElement GetTopLevelElement(XmlSchemaElement element)
        {
            if (!element.RefName.IsEmpty)
                return FindElement(element.RefName);
            return null;
        }

        private XmlSchemaElement[] GetEquivalentElements(XmlSchemaElement element)
        {
            ArrayList equivalentElements = new ArrayList();

            foreach (XmlSchema schema in Schemas.SchemaSet.Schemas())
            {
                for (int j = 0; j < schema.Items.Count; j++)
                {
                    object item = schema.Items[j];
                    if (item is XmlSchemaElement)
                    {
                        XmlSchemaElement equivalentElement = (XmlSchemaElement)item;
                        if (!equivalentElement.IsAbstract &&
                            equivalentElement.SubstitutionGroup.Namespace == schema.TargetNamespace &&
                            equivalentElement.SubstitutionGroup.Name == element.Name)
                        {
                            equivalentElements.Add(equivalentElement);
                        }
                    }
                }
            }

            return (XmlSchemaElement[])equivalentElements.ToArray(typeof(XmlSchemaElement));
        }

        private bool ImportSubstitutionGroupMember(XmlSchemaElement element, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns, bool repeats, ref bool needExplicitOrder, bool allowDuplicates)
        {
            XmlSchemaElement[] elements = GetEquivalentElements(element);
            if (elements.Length == 0)
                return false;
            XmlSchemaChoice choice = new XmlSchemaChoice();
            for (int i = 0; i < elements.Length; i++)
                choice.Items.Add(elements[i]);
            if (!element.IsAbstract)
                choice.Items.Add(element);
            if (identifier.Length == 0)
                identifier = CodeIdentifier.MakeValid(Accessor.UnescapeName(element.Name));
            else
                identifier += CodeIdentifier.MakePascal(Accessor.UnescapeName(element.Name));
            ImportChoiceGroup(choice, identifier, members, membersScope, null, ns, repeats, ref needExplicitOrder, allowDuplicates);

            return true;
        }

        private void ImportTextMember(CodeIdentifiers members, CodeIdentifiers membersScope, XmlQualifiedName simpleContentType)
        {
            TypeMapping mapping;
            bool isMixed = false;

            if (simpleContentType != null)
            {
                // allow to use all primitive types
                mapping = ImportType(simpleContentType, typeof(TypeMapping), null, TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue, false);
                if (!(mapping is PrimitiveMapping || mapping.TypeDesc.CanBeTextValue))
                {
                    return;
                }
            }
            else
            {
                // this is a case of the mixed content type, just generate string typeDesc
                isMixed = true;
                mapping = GetDefaultMapping(TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue);
            }

            TextAccessor accessor = new TextAccessor();
            accessor.Mapping = mapping;

            MemberMapping member = new MemberMapping();
            member.Elements = new ElementAccessor[0];
            member.Text = accessor;
            if (isMixed)
            {
                // just generate code for the standard mixed case (string[] text)
                member.TypeDesc = accessor.Mapping.TypeDesc.CreateArrayTypeDesc();
                member.Name = members.MakeRightCase("Text");
            }
            else
            {
                // import mapping for the simpleContent
                PrimitiveMapping pm = (PrimitiveMapping)accessor.Mapping;
                if (pm.IsList)
                {
                    member.TypeDesc = accessor.Mapping.TypeDesc.CreateArrayTypeDesc();
                    member.Name = members.MakeRightCase("Text");
                }
                else
                {
                    member.TypeDesc = accessor.Mapping.TypeDesc;
                    member.Name = members.MakeRightCase("Value");
                }
            }
            member.Name = membersScope.AddUnique(member.Name, member);
            members.Add(member.Name, member);
        }

        private MemberMapping ImportAnyMember(XmlSchemaAny any, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, ref bool mixed, ref bool needExplicitOrder, bool allowDuplicates)
        {
            ElementAccessor[] accessors = ImportAny(any, !mixed, ns);
            AddScopeElements(elementsScope, accessors, ref needExplicitOrder, allowDuplicates);
            MemberMapping member = new MemberMapping();
            member.Elements = accessors;
            member.Name = membersScope.MakeRightCase("Any");
            member.Name = membersScope.AddUnique(member.Name, member);
            members.Add(member.Name, member);
            member.TypeDesc = ((TypeMapping)accessors[0].Mapping).TypeDesc;

            bool repeats = any.IsMultipleOccurrence;

            if (mixed)
            {
                SpecialMapping textMapping = new SpecialMapping();
                textMapping.TypeDesc = Scope.GetTypeDesc(typeof(XmlNode));
                textMapping.TypeName = textMapping.TypeDesc.Name;
                member.TypeDesc = textMapping.TypeDesc;
                TextAccessor text = new TextAccessor();
                text.Mapping = textMapping;
                member.Text = text;
                repeats = true;
                mixed = false;
            }

            if (repeats)
            {
                member.TypeDesc = member.TypeDesc.CreateArrayTypeDesc();
            }
            return member;
        }
        private ElementAccessor[] ImportAny(XmlSchemaAny any, bool makeElement, string targetNamespace)
        {
            SpecialMapping mapping = new SpecialMapping();

            mapping.TypeDesc = Scope.GetTypeDesc(makeElement ? typeof(XmlElement) : typeof(XmlNode));
            mapping.TypeName = mapping.TypeDesc.Name;


            TypeFlags flags = TypeFlags.CanBeElementValue;
            if (makeElement)
                flags |= TypeFlags.CanBeTextValue;

            // let the extensions to run
            RunSchemaExtensions(mapping, XmlQualifiedName.Empty, null, any, flags);

            if (GenerateOrder && any.Namespace != null)
            {
                NamespaceList list = new NamespaceList(any.Namespace, targetNamespace);

                if (list.Type == NamespaceList.ListType.Set)
                {
                    ICollection namespaces = list.Enumerate;
                    ElementAccessor[] accessors = new ElementAccessor[namespaces.Count == 0 ? 1 : namespaces.Count];
                    int count = 0;
                    foreach (string ns in list.Enumerate)
                    {
                        ElementAccessor accessor = new ElementAccessor();
                        accessor.Mapping = mapping;
                        accessor.Any = true;
                        accessor.Namespace = ns;
                        accessors[count++] = accessor;
                    }
                    if (count > 0)
                    {
                        return accessors;
                    }
                }
            }

            ElementAccessor anyAccessor = new ElementAccessor();
            anyAccessor.Mapping = mapping;
            anyAccessor.Any = true;
            return new ElementAccessor[] { anyAccessor };
        }

        private ElementAccessor ImportArray(XmlSchemaElement element, string identifier, string ns, bool repeats)
        {
            if (repeats) return null;
            if (element.SchemaType == null) return null;
            if (element.IsMultipleOccurrence) return null;
            XmlSchemaType type = element.SchemaType;
            ArrayMapping arrayMapping = ImportArrayMapping(type, identifier, ns, repeats);
            if (arrayMapping == null) return null;
            ElementAccessor arrayAccessor = new ElementAccessor();
            arrayAccessor.Name = element.Name;
            arrayAccessor.Namespace = ns;
            arrayAccessor.Mapping = arrayMapping;
            if (arrayMapping.TypeDesc.IsNullable)
                arrayAccessor.IsNullable = element.IsNillable;
            arrayAccessor.Form = ElementForm(ns, element);
            return arrayAccessor;
        }

        private ArrayMapping ImportArrayMapping(XmlSchemaType type, string identifier, string ns, bool repeats)
        {
            if (!(type is XmlSchemaComplexType)) return null;
            if (!type.DerivedFrom.IsEmpty) return null;
            if (IsMixed(type)) return null;

            Mapping previousMapping = (Mapping)ImportedMappings[type];
            if (previousMapping != null)
            {
                if (previousMapping is ArrayMapping)
                    return (ArrayMapping)previousMapping;
                else
                    return null;
            }

            TypeItems items = GetTypeItems(type);

            if (items.Attributes != null && items.Attributes.Count > 0) return null;
            if (items.AnyAttribute != null) return null;
            if (items.Particle == null) return null;

            XmlSchemaGroupBase item = items.Particle;
            ArrayMapping arrayMapping = new ArrayMapping();

            arrayMapping.TypeName = identifier;
            arrayMapping.Namespace = ns;

            if (item is XmlSchemaChoice)
            {
                XmlSchemaChoice choice = (XmlSchemaChoice)item;
                if (!choice.IsMultipleOccurrence)
                    return null;
                bool needExplicitOrder = false;
                MemberMapping choiceMember = ImportChoiceGroup(choice, identifier, null, null, null, ns, true, ref needExplicitOrder, false);
                if (choiceMember.ChoiceIdentifier != null) return null;
                arrayMapping.TypeDesc = choiceMember.TypeDesc;
                arrayMapping.Elements = choiceMember.Elements;
                arrayMapping.TypeName = (type.Name == null || type.Name.Length == 0) ? "ArrayOf" + CodeIdentifier.MakePascal(arrayMapping.TypeDesc.Name) : type.Name;
            }
            else if (item is XmlSchemaAll || item is XmlSchemaSequence)
            {
                if (item.Items.Count != 1 || !(item.Items[0] is XmlSchemaElement)) return null;
                XmlSchemaElement itemElement = (XmlSchemaElement)item.Items[0];
                if (!itemElement.IsMultipleOccurrence) return null;
                if (IsCyclicReferencedType(itemElement, new List<string>(1) { identifier }))
                {
                    return null;
                }

                ElementAccessor itemAccessor = ImportElement(itemElement, identifier, typeof(TypeMapping), null, ns, false);
                if (itemAccessor.Any)
                    return null;
                arrayMapping.Elements = new ElementAccessor[] { itemAccessor };
                arrayMapping.TypeDesc = ((TypeMapping)itemAccessor.Mapping).TypeDesc.CreateArrayTypeDesc();
                arrayMapping.TypeName = (type.Name == null || type.Name.Length == 0) ? "ArrayOf" + CodeIdentifier.MakePascal(itemAccessor.Mapping.TypeDesc.Name) : type.Name;
            }
            else
            {
                return null;
            }

            ImportedMappings[type] = arrayMapping;
            Scope.AddTypeMapping(arrayMapping);
            // for the array-like mappings we need to create a struct mapping for the case when it referenced by the top-level element
            arrayMapping.TopLevelMapping = ImportStructType(type, ns, identifier, null, true);
            arrayMapping.TopLevelMapping.ReferencedByTopLevelElement = true;
            if (type.Name != null && type.Name.Length != 0)
                ImportDerivedTypes(new XmlQualifiedName(identifier, ns));

            return arrayMapping;
        }

        private bool IsCyclicReferencedType(XmlSchemaElement element, List<string> identifiers)
        {
            if (!element.RefName.IsEmpty)
            {
                XmlSchemaElement refElement = FindElement(element.RefName);
                string refElementIdentifier = CodeIdentifier.MakeValid(Accessor.UnescapeName(refElement.Name));
                foreach (string identifier in identifiers)
                {
                    if (refElementIdentifier == identifier)
                    {
                        return true;
                    }
                }
                identifiers.Add(refElementIdentifier);

                XmlSchemaType refType = refElement.SchemaType;
                if (refType is XmlSchemaComplexType)
                {
                    TypeItems items = GetTypeItems(refType);
                    if ((items.Particle is XmlSchemaSequence || items.Particle is XmlSchemaAll) && items.Particle.Items.Count == 1 && items.Particle.Items[0] is XmlSchemaElement)
                    {
                        XmlSchemaElement innerRefElement = (XmlSchemaElement)items.Particle.Items[0];
                        if (innerRefElement.IsMultipleOccurrence)
                        {
                            return IsCyclicReferencedType(innerRefElement, identifiers);
                        }
                    }
                }
            }
            return false;
        }

        private SpecialMapping ImportAnyMapping(XmlSchemaType type, string identifier, string ns, bool repeats)
        {
            if (type == null) return null;
            if (!type.DerivedFrom.IsEmpty) return null;

            bool mixed = IsMixed(type);
            TypeItems items = GetTypeItems(type);
            if (items.Particle == null) return null;
            if (!(items.Particle is XmlSchemaAll || items.Particle is XmlSchemaSequence)) return null;
            if (items.Attributes != null && items.Attributes.Count > 0) return null;
            XmlSchemaGroupBase group = (XmlSchemaGroupBase)items.Particle;

            if (group.Items.Count != 1 || !(group.Items[0] is XmlSchemaAny)) return null;
            XmlSchemaAny any = (XmlSchemaAny)group.Items[0];

            SpecialMapping mapping = new SpecialMapping();
            // check for special named any case
            if (items.AnyAttribute != null && any.IsMultipleOccurrence && mixed)
            {
                mapping.NamedAny = true;
                mapping.TypeDesc = Scope.GetTypeDesc(typeof(XmlElement));
            }
            else if (items.AnyAttribute != null || any.IsMultipleOccurrence)
            {
                // these only work for named any case -- otherwise import as struct
                return null;
            }
            else
            {
                mapping.TypeDesc = Scope.GetTypeDesc(mixed ? typeof(XmlNode) : typeof(XmlElement));
            }

            TypeFlags flags = TypeFlags.CanBeElementValue;
            if (items.AnyAttribute != null || mixed)
                flags |= TypeFlags.CanBeTextValue;

            // let the extensions to run
            RunSchemaExtensions(mapping, XmlQualifiedName.Empty, null, any, flags);

            mapping.TypeName = mapping.TypeDesc.Name;
            if (repeats)
                mapping.TypeDesc = mapping.TypeDesc.CreateArrayTypeDesc();

            return mapping;
        }

        private void ImportElementMember(XmlSchemaElement element, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool repeats, ref bool needExplicitOrder, bool allowDuplicates, bool allowUnboundedElements)
        {
            repeats = repeats | element.IsMultipleOccurrence;
            XmlSchemaElement headElement = GetTopLevelElement(element);
            if (headElement != null && ImportSubstitutionGroupMember(headElement, identifier, members, membersScope, ns, repeats, ref needExplicitOrder, allowDuplicates))
            {
                return;
            }
            ElementAccessor accessor;
            if ((accessor = ImportArray(element, identifier, ns, repeats)) == null)
            {
                accessor = ImportElement(element, identifier, typeof(TypeMapping), null, ns, false);
            }

            MemberMapping member = new MemberMapping();
            string name = CodeIdentifier.MakeValid(Accessor.UnescapeName(accessor.Name));
            member.Name = membersScope.AddUnique(name, member);

            if (member.Name.EndsWith("Specified", StringComparison.Ordinal))
            {
                name = member.Name;
                member.Name = membersScope.AddUnique(member.Name, member);
                membersScope.Remove(name);
            }
            members.Add(member.Name, member);
            // we do not support lists for elements
            if (accessor.Mapping.IsList)
            {
                accessor.Mapping = GetDefaultMapping(TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue);
                member.TypeDesc = accessor.Mapping.TypeDesc;
            }
            else
            {
                member.TypeDesc = accessor.Mapping.TypeDesc;
            }

            AddScopeElement(elementsScope, accessor, ref needExplicitOrder, allowDuplicates);
            member.Elements = new ElementAccessor[] { accessor };

            if (element.IsMultipleOccurrence || repeats)
            {
                if (!allowUnboundedElements && accessor.Mapping is ArrayMapping)
                {
                    accessor.Mapping = ((ArrayMapping)accessor.Mapping).TopLevelMapping;
                    accessor.Mapping.ReferencedByTopLevelElement = false;
                    accessor.Mapping.ReferencedByElement = true;
                }
                member.TypeDesc = accessor.Mapping.TypeDesc.CreateArrayTypeDesc();
            }

            if (element.MinOccurs == 0 && member.TypeDesc.IsValueType && !element.HasDefault && !member.TypeDesc.HasIsEmpty)
            {
                member.CheckSpecified = SpecifiedAccessor.ReadWrite;
            }
        }

        private void ImportAttributeMember(XmlSchemaAttribute attribute, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns)
        {
            AttributeAccessor accessor = ImportAttribute(attribute, identifier, ns, attribute);
            if (accessor == null) return;
            MemberMapping member = new MemberMapping();
            member.Elements = new ElementAccessor[0];
            member.Attribute = accessor;
            member.Name = CodeIdentifier.MakeValid(Accessor.UnescapeName(accessor.Name));
            member.Name = membersScope.AddUnique(member.Name, member);
            if (member.Name.EndsWith("Specified", StringComparison.Ordinal))
            {
                string name = member.Name;
                member.Name = membersScope.AddUnique(member.Name, member);
                membersScope.Remove(name);
            }
            members.Add(member.Name, member);
            member.TypeDesc = accessor.IsList ? accessor.Mapping.TypeDesc.CreateArrayTypeDesc() : accessor.Mapping.TypeDesc;

            if ((attribute.Use == XmlSchemaUse.Optional || attribute.Use == XmlSchemaUse.None) && member.TypeDesc.IsValueType && !attribute.HasDefault && !member.TypeDesc.HasIsEmpty)
            {
                member.CheckSpecified = SpecifiedAccessor.ReadWrite;
            }
        }

        private void ImportAnyAttributeMember(XmlSchemaAnyAttribute any, CodeIdentifiers members, CodeIdentifiers membersScope)
        {
            SpecialMapping mapping = new SpecialMapping();
            mapping.TypeDesc = Scope.GetTypeDesc(typeof(XmlAttribute));
            mapping.TypeName = mapping.TypeDesc.Name;

            AttributeAccessor accessor = new AttributeAccessor();
            accessor.Any = true;
            accessor.Mapping = mapping;

            MemberMapping member = new MemberMapping();
            member.Elements = new ElementAccessor[0];
            member.Attribute = accessor;
            member.Name = membersScope.MakeRightCase("AnyAttr");
            member.Name = membersScope.AddUnique(member.Name, member);
            members.Add(member.Name, member);
            member.TypeDesc = ((TypeMapping)accessor.Mapping).TypeDesc;
            member.TypeDesc = member.TypeDesc.CreateArrayTypeDesc();
        }

        private bool KeepXmlnsDeclarations(XmlSchemaType type, out string xmlnsMemberName)
        {
            xmlnsMemberName = null;
            if (type.Annotation == null)
                return false;
            if (type.Annotation.Items == null || type.Annotation.Items.Count == 0)
                return false;

            foreach (XmlSchemaObject o in type.Annotation.Items)
            {
                if (o is XmlSchemaAppInfo)
                {
                    XmlNode[] nodes = ((XmlSchemaAppInfo)o).Markup;
                    if (nodes != null && nodes.Length > 0)
                    {
                        foreach (XmlNode node in nodes)
                        {
                            if (node is XmlElement)
                            {
                                XmlElement e = (XmlElement)node;
                                if (e.Name == "keepNamespaceDeclarations")
                                {
                                    if (e.LastNode is XmlText)
                                    {
                                        xmlnsMemberName = (((XmlText)e.LastNode).Value).Trim(null);
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void ImportXmlnsDeclarationsMember(XmlSchemaType type, CodeIdentifiers members, CodeIdentifiers membersScope)
        {
            string xmlnsMemberName;
            if (!KeepXmlnsDeclarations(type, out xmlnsMemberName))
                return;
            TypeDesc xmlnsTypeDesc = Scope.GetTypeDesc(typeof(XmlSerializerNamespaces));
            StructMapping xmlnsMapping = new StructMapping();

            xmlnsMapping.TypeDesc = xmlnsTypeDesc;
            xmlnsMapping.TypeName = xmlnsMapping.TypeDesc.Name;
            xmlnsMapping.Members = new MemberMapping[0];
            xmlnsMapping.IncludeInSchema = false;
            xmlnsMapping.ReferencedByTopLevelElement = true;

            ElementAccessor xmlns = new ElementAccessor();
            xmlns.Mapping = xmlnsMapping;

            MemberMapping member = new MemberMapping();
            member.Elements = new ElementAccessor[] { xmlns };
            member.Name = CodeIdentifier.MakeValid(xmlnsMemberName == null ? "Namespaces" : xmlnsMemberName);
            member.Name = membersScope.AddUnique(member.Name, member);
            members.Add(member.Name, member);
            member.TypeDesc = xmlnsTypeDesc;
            member.Xmlns = new XmlnsAccessor();
            member.Ignore = true;
        }

        private void ImportAttributeGroupMembers(XmlSchemaAttributeGroup group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns)
        {
            for (int i = 0; i < group.Attributes.Count; i++)
            {
                object item = group.Attributes[i];
                if (item is XmlSchemaAttributeGroup)
                    ImportAttributeGroupMembers((XmlSchemaAttributeGroup)item, identifier, members, membersScope, ns);
                else if (item is XmlSchemaAttribute)
                    ImportAttributeMember((XmlSchemaAttribute)item, identifier, members, membersScope, ns);
            }
            if (group.AnyAttribute != null)
                ImportAnyAttributeMember(group.AnyAttribute, members, membersScope);
        }

        private AttributeAccessor ImportSpecialAttribute(XmlQualifiedName name, string identifier)
        {
            PrimitiveMapping mapping = new PrimitiveMapping();
            mapping.TypeDesc = Scope.GetTypeDesc(typeof(string));
            mapping.TypeName = mapping.TypeDesc.DataType.Name;
            AttributeAccessor accessor = new AttributeAccessor();
            accessor.Name = name.Name;
            accessor.Namespace = XmlReservedNs.NsXml;
            accessor.CheckSpecial();
            accessor.Mapping = mapping;
            return accessor;
        }

        private AttributeAccessor ImportAttribute(XmlSchemaAttribute attribute, string identifier, string ns, XmlSchemaAttribute defaultValueProvider)
        {
            if (attribute.Use == XmlSchemaUse.Prohibited) return null;
            if (!attribute.RefName.IsEmpty)
            {
                if (attribute.RefName.Namespace == XmlReservedNs.NsXml)
                    return ImportSpecialAttribute(attribute.RefName, identifier);
                else
                    return ImportAttribute(FindAttribute(attribute.RefName), identifier, attribute.RefName.Namespace, defaultValueProvider);
            }
            TypeMapping mapping;
            if (attribute.Name.Length == 0) throw new InvalidOperationException(SR.XmlAttributeHasNoName);
            if (identifier.Length == 0)
                identifier = CodeIdentifier.MakeValid(attribute.Name);
            else
                identifier += CodeIdentifier.MakePascal(attribute.Name);
            if (!attribute.SchemaTypeName.IsEmpty)
                mapping = (TypeMapping)ImportType(attribute.SchemaTypeName, typeof(TypeMapping), null, TypeFlags.CanBeAttributeValue, false);
            else if (attribute.SchemaType != null)
                mapping = ImportDataType((XmlSchemaSimpleType)attribute.SchemaType, ns, identifier, null, TypeFlags.CanBeAttributeValue, false);
            else
            {
                mapping = GetDefaultMapping(TypeFlags.CanBeAttributeValue);
            }

            // let the extensions to run
            if (mapping != null && !mapping.TypeDesc.IsMappedType)
            {
                RunSchemaExtensions(mapping, attribute.SchemaTypeName, attribute.SchemaType, attribute, TypeFlags.CanBeElementValue | TypeFlags.CanBeAttributeValue | TypeFlags.CanBeTextValue);
            }
            AttributeAccessor accessor = new AttributeAccessor();
            accessor.Name = attribute.Name;
            accessor.Namespace = ns;
            accessor.Form = AttributeForm(ns, attribute);
            accessor.CheckSpecial();
            accessor.Mapping = mapping;
            accessor.IsList = mapping.IsList;
            accessor.IsOptional = attribute.Use != XmlSchemaUse.Required;

            if (defaultValueProvider.DefaultValue != null)
            {
                accessor.Default = defaultValueProvider.DefaultValue;
            }
            else if (defaultValueProvider.FixedValue != null)
            {
                accessor.Default = defaultValueProvider.FixedValue;
                accessor.IsFixed = true;
            }
            else if (attribute != defaultValueProvider)
            {
                if (attribute.DefaultValue != null)
                {
                    accessor.Default = attribute.DefaultValue;
                }
                else if (attribute.FixedValue != null)
                {
                    accessor.Default = attribute.FixedValue;
                    accessor.IsFixed = true;
                }
            }
            return accessor;
        }

        private TypeMapping ImportDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, Type baseType, TypeFlags flags, bool isList)
        {
            if (baseType != null)
                return ImportStructDataType(dataType, typeNs, identifier, baseType);

            TypeMapping mapping = ImportNonXsdPrimitiveDataType(dataType, typeNs, flags);
            if (mapping != null)
                return mapping;

            if (dataType.Content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)dataType.Content;
                foreach (object o in restriction.Facets)
                {
                    if (o is XmlSchemaEnumerationFacet)
                    {
                        return ImportEnumeratedDataType(dataType, typeNs, identifier, flags, isList);
                    }
                }
                if (restriction.BaseType != null)
                {
                    return ImportDataType(restriction.BaseType, typeNs, identifier, null, flags, false);
                }
                else
                {
                    AddReference(restriction.BaseTypeName, TypesInUse, SR.XmlCircularTypeReference);
                    mapping = ImportDataType(FindDataType(restriction.BaseTypeName, flags), restriction.BaseTypeName.Namespace, identifier, null, flags, false);
                    if (restriction.BaseTypeName.Namespace != XmlSchema.Namespace)
                        RemoveReference(restriction.BaseTypeName, TypesInUse);
                    return mapping;
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
                        mapping = ImportDataType(list.ItemType, typeNs, identifier, null, flags, true);
                        if (mapping != null)
                        {
                            mapping.TypeName = dataType.Name;
                            return mapping;
                        }
                    }
                    else if (list.ItemTypeName != null && !list.ItemTypeName.IsEmpty)
                    {
                        mapping = ImportType(list.ItemTypeName, typeof(TypeMapping), null, TypeFlags.CanBeAttributeValue, true);
                        if (mapping != null && mapping is PrimitiveMapping)
                        {
                            ((PrimitiveMapping)mapping).IsList = true;
                            return mapping;
                        }
                    }
                }
                return GetDefaultMapping(flags);
            }
            return ImportPrimitiveDataType(dataType, flags);
        }

        private TypeMapping ImportEnumeratedDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, TypeFlags flags, bool isList)
        {
            TypeMapping mapping = (TypeMapping)ImportedMappings[dataType];
            if (mapping != null)
                return mapping;

            XmlSchemaType sourceType = dataType;
            while (!sourceType.DerivedFrom.IsEmpty)
            {
                sourceType = FindType(sourceType.DerivedFrom, TypeFlags.CanBeElementValue | TypeFlags.CanBeAttributeValue);
            }
            if (sourceType is XmlSchemaComplexType) return null;
            TypeDesc sourceTypeDesc = Scope.GetTypeDesc((XmlSchemaSimpleType)sourceType);
            if (sourceTypeDesc != null && sourceTypeDesc.FullName != typeof(string).FullName)
                return ImportPrimitiveDataType(dataType, flags);
            identifier = Accessor.UnescapeName(identifier);
            string typeName = GenerateUniqueTypeName(identifier);
            EnumMapping enumMapping = new EnumMapping();
            enumMapping.IsReference = Schemas.IsReference(dataType);
            enumMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Enum, null, 0);
            if (dataType.Name != null && dataType.Name.Length > 0)
                enumMapping.TypeName = identifier;
            enumMapping.Namespace = typeNs;
            enumMapping.IsFlags = isList;

            CodeIdentifiers constants = new CodeIdentifiers();
            XmlSchemaSimpleTypeContent content = dataType.Content;

            if (content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)content;
                for (int i = 0; i < restriction.Facets.Count; i++)
                {
                    object facet = restriction.Facets[i];
                    if (!(facet is XmlSchemaEnumerationFacet)) continue;
                    XmlSchemaEnumerationFacet enumeration = (XmlSchemaEnumerationFacet)facet;
                    // validate the enumeration value
                    if (sourceTypeDesc != null && sourceTypeDesc.HasCustomFormatter)
                    {
                        XmlCustomFormatter.ToDefaultValue(enumeration.Value, sourceTypeDesc.FormatterName);
                    }
                    ConstantMapping constant = new ConstantMapping();
                    string constantName = CodeIdentifier.MakeValid(enumeration.Value);
                    constant.Name = constants.AddUnique(constantName, constant);
                    constant.XmlName = enumeration.Value;
                    constant.Value = i;
                }
            }
            enumMapping.Constants = (ConstantMapping[])constants.ToArray(typeof(ConstantMapping));
            if (isList && enumMapping.Constants.Length > 63)
            {
                // if we have 64+ flag constants we cannot map the type to long enum, we will use string mapping instead.
                mapping = GetDefaultMapping(TypeFlags.CanBeElementValue | TypeFlags.CanBeTextValue | TypeFlags.CanBeAttributeValue);
                ImportedMappings.Add(dataType, mapping);
                return mapping;
            }
            ImportedMappings.Add(dataType, enumMapping);
            Scope.AddTypeMapping(enumMapping);
            return enumMapping;
        }

        internal class ElementComparer : IComparer
        {
            public int Compare(object o1, object o2)
            {
                ElementAccessor e1 = (ElementAccessor)o1;
                ElementAccessor e2 = (ElementAccessor)o2;
                return String.Compare(e1.ToString(string.Empty), e2.ToString(string.Empty), StringComparison.Ordinal);
            }
        }

        private EnumMapping ImportEnumeratedChoice(ElementAccessor[] choice, string typeNs, string typeName)
        {
            typeName = GenerateUniqueTypeName(Accessor.UnescapeName(typeName), typeNs);
            EnumMapping enumMapping = new EnumMapping();
            enumMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Enum, null, 0);
            enumMapping.TypeName = typeName;
            enumMapping.Namespace = typeNs;
            enumMapping.IsFlags = false;
            enumMapping.IncludeInSchema = false;

            if (GenerateOrder)
            {
                Array.Sort(choice, new ElementComparer());
            }
            CodeIdentifiers constants = new CodeIdentifiers();
            for (int i = 0; i < choice.Length; i++)
            {
                ElementAccessor element = choice[i];
                ConstantMapping constant = new ConstantMapping();
                string constantName = CodeIdentifier.MakeValid(element.Name);
                constant.Name = constants.AddUnique(constantName, constant);
                constant.XmlName = element.ToString(typeNs);
                constant.Value = i;
            }
            enumMapping.Constants = (ConstantMapping[])constants.ToArray(typeof(ConstantMapping));
            Scope.AddTypeMapping(enumMapping);
            return enumMapping;
        }

        private PrimitiveMapping ImportPrimitiveDataType(XmlSchemaSimpleType dataType, TypeFlags flags)
        {
            TypeDesc sourceTypeDesc = GetDataTypeSource(dataType, flags);
            PrimitiveMapping mapping = new PrimitiveMapping();
            mapping.TypeDesc = sourceTypeDesc;
            mapping.TypeName = sourceTypeDesc.DataType.Name;
            mapping.Namespace = mapping.TypeDesc.IsXsdType ? XmlSchema.Namespace : UrtTypes.Namespace;
            return mapping;
        }

        private PrimitiveMapping ImportNonXsdPrimitiveDataType(XmlSchemaSimpleType dataType, string ns, TypeFlags flags)
        {
            PrimitiveMapping mapping = null;
            TypeDesc typeDesc = null;
            if (dataType.Name != null && dataType.Name.Length != 0)
            {
                typeDesc = Scope.GetTypeDesc(dataType.Name, ns, flags);
                if (typeDesc != null)
                {
                    mapping = new PrimitiveMapping();
                    mapping.TypeDesc = typeDesc;
                    mapping.TypeName = typeDesc.DataType.Name;
                    mapping.Namespace = mapping.TypeDesc.IsXsdType ? XmlSchema.Namespace : ns;
                }
            }
            return mapping;
        }

        private XmlSchemaGroup FindGroup(XmlQualifiedName name)
        {
            XmlSchemaGroup group = (XmlSchemaGroup)Schemas.Find(name, typeof(XmlSchemaGroup));
            if (group == null)
                throw new InvalidOperationException(SR.Format(SR.XmlMissingGroup, name.Name));

            return group;
        }

        private XmlSchemaAttributeGroup FindAttributeGroup(XmlQualifiedName name)
        {
            XmlSchemaAttributeGroup group = (XmlSchemaAttributeGroup)Schemas.Find(name, typeof(XmlSchemaAttributeGroup));
            if (group == null)
                throw new InvalidOperationException(SR.Format(SR.XmlMissingAttributeGroup, name.Name));

            return group;
        }

        internal static XmlQualifiedName BaseTypeName(XmlSchemaSimpleType dataType)
        {
            XmlSchemaSimpleTypeContent content = dataType.Content;
            if (content is XmlSchemaSimpleTypeRestriction)
            {
                return ((XmlSchemaSimpleTypeRestriction)content).BaseTypeName;
            }
            else if (content is XmlSchemaSimpleTypeList)
            {
                XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)content;
                if (list.ItemTypeName != null && !list.ItemTypeName.IsEmpty)
                    return list.ItemTypeName;
                if (list.ItemType != null)
                {
                    return BaseTypeName(list.ItemType);
                }
            }
            return new XmlQualifiedName("string", XmlSchema.Namespace);
        }

        private TypeDesc GetDataTypeSource(XmlSchemaSimpleType dataType, TypeFlags flags)
        {
            TypeDesc typeDesc = null;
            if (dataType.Name != null && dataType.Name.Length != 0)
            {
                typeDesc = Scope.GetTypeDesc(dataType);
                if (typeDesc != null) return typeDesc;
            }
            XmlQualifiedName qname = BaseTypeName(dataType);
            AddReference(qname, TypesInUse, SR.XmlCircularTypeReference);
            typeDesc = GetDataTypeSource(FindDataType(qname, flags), flags);
            if (qname.Namespace != XmlSchema.Namespace)
                RemoveReference(qname, TypesInUse);

            return typeDesc;
        }

        private XmlSchemaSimpleType FindDataType(XmlQualifiedName name, TypeFlags flags)
        {
            if (name == null || name.IsEmpty)
            {
                return (XmlSchemaSimpleType)Scope.GetTypeDesc(typeof(string)).DataType;
            }
            TypeDesc typeDesc = Scope.GetTypeDesc(name.Name, name.Namespace, flags);
            if (typeDesc != null && typeDesc.DataType is XmlSchemaSimpleType)
                return (XmlSchemaSimpleType)typeDesc.DataType;
            XmlSchemaSimpleType dataType = (XmlSchemaSimpleType)Schemas.Find(name, typeof(XmlSchemaSimpleType));
            if (dataType != null)
            {
                return dataType;
            }
            if (name.Namespace == XmlSchema.Namespace)
                return (XmlSchemaSimpleType)Scope.GetTypeDesc("string", XmlSchema.Namespace, flags).DataType;
            else
            {
                if (name.Name == Soap.Array && name.Namespace == Soap.Encoding)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncoding, name.ToString()));
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlMissingDataType, name.ToString()));
                }
            }
        }

        private XmlSchemaType FindType(XmlQualifiedName name, TypeFlags flags)
        {
            if (name == null || name.IsEmpty)
            {
                return Scope.GetTypeDesc(typeof(string)).DataType;
            }
            object type = Schemas.Find(name, typeof(XmlSchemaComplexType));
            if (type != null)
            {
                return (XmlSchemaComplexType)type;
            }
            return FindDataType(name, flags);
        }

        private XmlSchemaElement FindElement(XmlQualifiedName name)
        {
            XmlSchemaElement element = (XmlSchemaElement)Schemas.Find(name, typeof(XmlSchemaElement));
            if (element == null)
                throw new InvalidOperationException(SR.Format(SR.XmlMissingElement, name.ToString()));
            return element;
        }

        private XmlSchemaAttribute FindAttribute(XmlQualifiedName name)
        {
            XmlSchemaAttribute attribute = (XmlSchemaAttribute)Schemas.Find(name, typeof(XmlSchemaAttribute));
            if (attribute == null)
                throw new InvalidOperationException(SR.Format(SR.XmlMissingAttribute, name.Name));

            return attribute;
        }

        private XmlSchemaForm ElementForm(string ns, XmlSchemaElement element)
        {
            if (element.Form == XmlSchemaForm.None)
            {
                XmlSchemaObject parent = element;
                while (parent.Parent != null)
                {
                    parent = parent.Parent;
                }
                XmlSchema schema = parent as XmlSchema;

                if (schema != null)
                {
                    if (ns == null || ns.Length == 0)
                    {
                        return schema.ElementFormDefault == XmlSchemaForm.None ? XmlSchemaForm.Unqualified : schema.ElementFormDefault;
                    }
                    else
                    {
                        XmlSchemas.Preprocess(schema);
                        return element.QualifiedName.Namespace == null || element.QualifiedName.Namespace.Length == 0 ? XmlSchemaForm.Unqualified : XmlSchemaForm.Qualified;
                    }
                }
                return XmlSchemaForm.Qualified;
            }
            return element.Form;
        }

        internal string FindExtendedAnyElement(XmlSchemaAny any, bool mixed, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, out SchemaImporterExtension extension)
        {
            extension = null;
            foreach (SchemaImporterExtension ex in Extensions)
            {
                string typeName = ex.ImportAnyElement(any, mixed, Schemas, this, compileUnit, mainNamespace, Options, CodeProvider);
                if (typeName != null && typeName.Length > 0)
                {
                    extension = ex;
                    return typeName;
                }
            }
            return null;
        }

        internal string FindExtendedType(string name, string ns, XmlSchemaObject context, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, out SchemaImporterExtension extension)
        {
            extension = null;
            foreach (SchemaImporterExtension ex in Extensions)
            {
                string typeName = ex.ImportSchemaType(name, ns, context, Schemas, this, compileUnit, mainNamespace, Options, CodeProvider);
                if (typeName != null && typeName.Length > 0)
                {
                    extension = ex;
                    return typeName;
                }
            }
            return null;
        }

        internal string FindExtendedType(XmlSchemaType type, XmlSchemaObject context, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, out SchemaImporterExtension extension)
        {
            extension = null;
            foreach (SchemaImporterExtension ex in Extensions)
            {
                string typeName = ex.ImportSchemaType(type, context, Schemas, this, compileUnit, mainNamespace, Options, CodeProvider);
                if (typeName != null && typeName.Length > 0)
                {
                    extension = ex;
                    return typeName;
                }
            }
            return null;
        }

        private XmlSchemaForm AttributeForm(string ns, XmlSchemaAttribute attribute)
        {
            if (attribute.Form == XmlSchemaForm.None)
            {
                XmlSchemaObject parent = attribute;
                while (parent.Parent != null)
                {
                    parent = parent.Parent;
                }
                XmlSchema schema = parent as XmlSchema;
                if (schema != null)
                {
                    if (ns == null || ns.Length == 0)
                    {
                        return schema.AttributeFormDefault == XmlSchemaForm.None ? XmlSchemaForm.Unqualified : schema.AttributeFormDefault;
                    }
                    else
                    {
                        XmlSchemas.Preprocess(schema);
                        return attribute.QualifiedName.Namespace == null || attribute.QualifiedName.Namespace.Length == 0 ? XmlSchemaForm.Unqualified : XmlSchemaForm.Qualified;
                    }
                }
                return XmlSchemaForm.Unqualified;
            }
            return attribute.Form;
        }
    }
}
