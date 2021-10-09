// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System;
    using System.Globalization;
    using System.Xml.Schema;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;

    public class SoapReflectionImporter
    {
        private readonly TypeScope _typeScope;
        private readonly SoapAttributeOverrides _attributeOverrides;
        private readonly NameTable _types = new NameTable();      // xmltypename + xmlns -> Mapping
        private readonly NameTable _nullables = new NameTable();  // xmltypename + xmlns -> NullableMapping
        private StructMapping? _root;
        private readonly string _defaultNs;
        private readonly ModelScope _modelScope;

        public SoapReflectionImporter() : this(null, null)
        {
        }

        public SoapReflectionImporter(string? defaultNamespace) : this(null, defaultNamespace)
        {
        }

        public SoapReflectionImporter(SoapAttributeOverrides? attributeOverrides) : this(attributeOverrides, null)
        {
        }

        public SoapReflectionImporter(SoapAttributeOverrides? attributeOverrides, string? defaultNamespace)
        {
            if (defaultNamespace == null)
                defaultNamespace = string.Empty;
            if (attributeOverrides == null)
                attributeOverrides = new SoapAttributeOverrides();
            _attributeOverrides = attributeOverrides;
            _defaultNs = defaultNamespace;
            _typeScope = new TypeScope();
            _modelScope = new ModelScope(_typeScope);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public void IncludeTypes(ICustomAttributeProvider provider)
        {
            IncludeTypes(provider, new RecursionLimiter());
        }

        [RequiresUnreferencedCode("calls IncludeType")]
        private void IncludeTypes(ICustomAttributeProvider provider, RecursionLimiter limiter)
        {
            object[] attrs = provider.GetCustomAttributes(typeof(SoapIncludeAttribute), false);
            for (int i = 0; i < attrs.Length; i++)
            {
                IncludeType(((SoapIncludeAttribute)attrs[i]).Type, limiter);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public void IncludeType(Type type)
        {
            IncludeType(type, new RecursionLimiter());
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void IncludeType(Type type, RecursionLimiter limiter)
        {
            ImportTypeMapping(_modelScope.GetTypeModel(type), limiter);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlTypeMapping ImportTypeMapping(Type type)
        {
            return ImportTypeMapping(type, null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlTypeMapping ImportTypeMapping(Type type, string? defaultNamespace)
        {
            ElementAccessor element = new ElementAccessor();
            element.IsSoap = true;
            element.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(type), new RecursionLimiter());
            element.Name = element.Mapping.DefaultElementName;
            element.Namespace = element.Mapping.Namespace == null ? defaultNamespace : element.Mapping.Namespace;
            element.Form = XmlSchemaForm.Qualified;
            XmlTypeMapping xmlMapping = new XmlTypeMapping(_typeScope, element);
            xmlMapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, defaultNamespace));
            xmlMapping.IsSoap = true;
            xmlMapping.GenerateSerializer = true;
            return xmlMapping;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members)
        {
            return ImportMembersMapping(elementName, ns, members, true, true, false);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors)
        {
            return ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, false);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate)
        {
            return ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, validate, XmlMappingAccess.Read | XmlMappingAccess.Write);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, XmlMappingAccess access)
        {
            ElementAccessor element = new ElementAccessor();
            element.IsSoap = true;
            element.Name = elementName == null || elementName.Length == 0 ? elementName : XmlConvert.EncodeLocalName(elementName);

            element.Mapping = ImportMembersMapping(members, ns, hasWrapperElement, writeAccessors, validate, new RecursionLimiter());
            element.Mapping.TypeName = elementName;
            element.Namespace = element.Mapping.Namespace == null ? ns : element.Mapping.Namespace;
            element.Form = XmlSchemaForm.Qualified;
            XmlMembersMapping xmlMapping = new XmlMembersMapping(_typeScope, element, access);
            xmlMapping.IsSoap = true;
            xmlMapping.GenerateSerializer = true;
            return xmlMapping;
        }

        private Exception ReflectionException(string context, Exception e)
        {
            return new InvalidOperationException(SR.Format(SR.XmlReflectionError, context), e);
        }

        private SoapAttributes GetAttributes(Type type)
        {
            SoapAttributes? attrs = _attributeOverrides[type];
            if (attrs != null) return attrs;
            return new SoapAttributes(type);
        }

        private SoapAttributes GetAttributes(MemberInfo memberInfo)
        {
            SoapAttributes? attrs = _attributeOverrides[memberInfo.DeclaringType!, memberInfo.Name];
            if (attrs != null) return attrs;
            return new SoapAttributes(memberInfo);
        }

        [RequiresUnreferencedCode("calls ImportTypeMapping")]
        private TypeMapping ImportTypeMapping(TypeModel model, RecursionLimiter limiter)
        {
            return ImportTypeMapping(model, string.Empty, limiter);
        }

        [RequiresUnreferencedCode("Calls TypeDesc")]
        private TypeMapping ImportTypeMapping(TypeModel model, string dataType, RecursionLimiter limiter)
        {
            if (dataType.Length > 0)
            {
                if (!model.TypeDesc.IsPrimitive)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidDataTypeUsage, dataType, "SoapElementAttribute.DataType"));
                }
                TypeDesc? td = _typeScope.GetTypeDesc(dataType!, XmlSchema.Namespace);
                if (td == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidXsdDataType, dataType, "SoapElementAttribute.DataType", new XmlQualifiedName(dataType, XmlSchema.Namespace).ToString()));
                }
                if (model.TypeDesc.FullName != td.FullName)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlDataTypeMismatch, dataType, "SoapElementAttribute.DataType", model.TypeDesc.FullName));
                }
            }

            SoapAttributes a = GetAttributes(model.Type);

            if ((a.GetSoapFlags() & ~SoapAttributeFlags.Type) != 0)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidTypeAttributes, model.Type.FullName));

            switch (model.TypeDesc.Kind)
            {
                case TypeKind.Enum:
                    return ImportEnumMapping((EnumModel)model);
                case TypeKind.Primitive:
                    return ImportPrimitiveMapping((PrimitiveModel)model, dataType);
                case TypeKind.Array:
                case TypeKind.Collection:
                case TypeKind.Enumerable:
                    return ImportArrayLikeMapping((ArrayModel)model, limiter);
                case TypeKind.Root:
                case TypeKind.Class:
                case TypeKind.Struct:
                    if (model.TypeDesc.IsOptionalValue)
                    {
                        TypeDesc? baseTypeDesc = model.TypeDesc.BaseTypeDesc;
                        SoapAttributes baseAttributes = GetAttributes(baseTypeDesc!.Type!);
                        string typeNs = _defaultNs;
                        if (baseAttributes.SoapType != null && baseAttributes.SoapType.Namespace != null)
                            typeNs = baseAttributes.SoapType.Namespace;
                        TypeDesc valueTypeDesc = string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc! : _typeScope.GetTypeDesc(dataType, XmlSchema.Namespace)!;
                        string xsdTypeName = string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc!.Name : dataType;
                        TypeMapping? baseMapping = GetTypeMapping(xsdTypeName, typeNs, valueTypeDesc);
                        if (baseMapping == null)
                            baseMapping = ImportTypeMapping(_modelScope.GetTypeModel(baseTypeDesc.Type!), dataType, limiter);
                        return CreateNullableMapping(baseMapping, model.TypeDesc.Type!);
                    }
                    else
                    {
                        return ImportStructLikeMapping((StructModel)model, limiter);
                    }
                default:
                    throw new NotSupportedException(SR.Format(SR.XmlUnsupportedSoapTypeKind, model.TypeDesc.FullName));
            }
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        private StructMapping CreateRootMapping()
        {
            TypeDesc typeDesc = _typeScope.GetTypeDesc(typeof(object));
            StructMapping mapping = new StructMapping();
            mapping.IsSoap = true;
            mapping.TypeDesc = typeDesc;
            mapping.Members = Array.Empty<MemberMapping>();
            mapping.IncludeInSchema = false;
            mapping.TypeName = Soap.UrType;
            mapping.Namespace = XmlSchema.Namespace;
            return mapping;
        }

        [RequiresUnreferencedCode("calls CreateRootMapping")]
        private StructMapping GetRootMapping()
        {
            if (_root == null)
            {
                _root = CreateRootMapping();
                _typeScope.AddTypeMapping(_root);
            }
            return _root;
        }

        private TypeMapping? GetTypeMapping(string typeName, string? ns, TypeDesc typeDesc)
        {
            TypeMapping? mapping = (TypeMapping?)_types[typeName, ns];
            if (mapping == null) return null;
            if (mapping.TypeDesc != typeDesc)
                throw new InvalidOperationException(SR.Format(SR.XmlTypesDuplicate, typeDesc.FullName, mapping.TypeDesc!.FullName, typeName, ns));
            return mapping;
        }

        private NullableMapping CreateNullableMapping(TypeMapping baseMapping,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            TypeDesc typeDesc = baseMapping.TypeDesc!.GetNullableTypeDesc(type);
            TypeMapping? existingMapping = (TypeMapping?)_nullables[baseMapping.TypeName!, baseMapping.Namespace];
            NullableMapping mapping;
            if (existingMapping != null)
            {
                if (existingMapping is NullableMapping)
                {
                    mapping = (NullableMapping)existingMapping;
                    if (mapping.BaseMapping is PrimitiveMapping && baseMapping is PrimitiveMapping)
                        return mapping;
                    else if (mapping.BaseMapping == baseMapping)
                    {
                        return mapping;
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlTypesDuplicate, typeDesc.FullName, existingMapping.TypeDesc!.FullName, typeDesc.Name, existingMapping.Namespace));
                    }
                }
                else if (!(baseMapping is PrimitiveMapping))
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlTypesDuplicate, typeDesc.FullName, existingMapping.TypeDesc!.FullName, typeDesc.Name, existingMapping.Namespace));
                }
            }
            mapping = new NullableMapping();
            mapping.BaseMapping = baseMapping;
            mapping.TypeDesc = typeDesc;
            mapping.TypeName = baseMapping.TypeName;
            mapping.Namespace = baseMapping.Namespace;
            mapping.IncludeInSchema = false; //baseMapping.IncludeInSchema;
            _nullables.Add(baseMapping.TypeName, mapping.Namespace, mapping);
            _typeScope.AddTypeMapping(mapping);
            return mapping;
        }

        [RequiresUnreferencedCode("calls GetRootMapping")]
        private StructMapping ImportStructLikeMapping(StructModel model, RecursionLimiter limiter)
        {
            if (model.TypeDesc.Kind == TypeKind.Root) return GetRootMapping();

            SoapAttributes a = GetAttributes(model.Type);

            string typeNs = _defaultNs;
            if (a.SoapType != null && a.SoapType.Namespace != null)
                typeNs = a.SoapType.Namespace;
            string typeName = XsdTypeName(model.Type, a, model.TypeDesc.Name);
            typeName = XmlConvert.EncodeLocalName(typeName);

            StructMapping? mapping = (StructMapping?)GetTypeMapping(typeName, typeNs, model.TypeDesc);
            if (mapping == null)
            {
                mapping = new StructMapping();
                mapping.IsSoap = true;
                mapping.TypeDesc = model.TypeDesc;
                mapping.Namespace = typeNs;
                mapping.TypeName = typeName;
                if (a.SoapType != null) mapping.IncludeInSchema = a.SoapType.IncludeInSchema;
                _typeScope.AddTypeMapping(mapping);
                _types.Add(typeName, typeNs, mapping);
                if (limiter.IsExceededLimit)
                {
                    limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, mapping));
                    return mapping;
                }

                limiter.Depth++;

                InitializeStructMembers(mapping, model, limiter);
                while (limiter.DeferredWorkItems.Count > 0)
                {
                    int index = limiter.DeferredWorkItems.Count - 1;
                    ImportStructWorkItem item = limiter.DeferredWorkItems[index];
                    if (InitializeStructMembers(item.Mapping, item.Model, limiter))
                    {
                        //
                        // if InitializeStructMembers returns true, then there were *no* changes to the DeferredWorkItems
                        //
#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (index != limiter.DeferredWorkItems.Count - 1)
                            throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "DeferredWorkItems.Count have changed"));
                        if (item != limiter.DeferredWorkItems[index])
                            throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "DeferredWorkItems.Top have changed"));
#endif
                        // Remove the last work item
                        limiter.DeferredWorkItems.RemoveAt(index);
                    }
                }
                limiter.Depth--;
            }
            return mapping;
        }

        [RequiresUnreferencedCode("calls GetTypeModel")]
        private bool InitializeStructMembers(StructMapping mapping, StructModel model, RecursionLimiter limiter)
        {
            if (mapping.IsFullyInitialized)
                return true;
            if (model.TypeDesc.BaseTypeDesc != null)
            {
                StructMapping baseMapping = ImportStructLikeMapping((StructModel)_modelScope.GetTypeModel(model.Type.BaseType!, false), limiter);

                // check to see if the import of the baseMapping was deferred
                int baseIndex = limiter.DeferredWorkItems.IndexOf(mapping.BaseMapping!);
                if (baseIndex < 0)
                {
                    mapping.BaseMapping = baseMapping;
                }
                else
                {
                    // the import of the baseMapping was deferred, make sure that the derived mappings is deferred as well
                    if (!limiter.DeferredWorkItems.Contains(mapping))
                    {
                        limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, mapping));
                    }
                    // make sure that baseMapping get processed before the derived
                    int top = limiter.DeferredWorkItems.Count - 1;
                    if (baseIndex < top)
                    {
                        ImportStructWorkItem baseMappingWorkItem = limiter.DeferredWorkItems[baseIndex];
                        limiter.DeferredWorkItems[baseIndex] = limiter.DeferredWorkItems[top];
                        limiter.DeferredWorkItems[top] = baseMappingWorkItem;
                    }
                    return false;
                }
            }
            var members = new List<MemberMapping>();
            foreach (MemberInfo memberInfo in model.GetMemberInfos())
            {
                if (!(memberInfo is FieldInfo) && !(memberInfo is PropertyInfo))
                    continue;
                SoapAttributes memberAttrs = GetAttributes(memberInfo);
                if (memberAttrs.SoapIgnore) continue;
                FieldModel? fieldModel = model.GetFieldModel(memberInfo);
                if (fieldModel == null) continue;
                MemberMapping? member = ImportFieldMapping(fieldModel, memberAttrs, mapping.Namespace!, limiter);
                if (member == null) continue;

                if (!member.TypeDesc!.IsPrimitive && !member.TypeDesc.IsEnum && !member.TypeDesc.IsOptionalValue)
                {
                    if (model.TypeDesc.IsValueType)
                        throw new NotSupportedException(SR.Format(SR.XmlRpcRefsInValueType, model.TypeDesc.FullName));
                    if (member.TypeDesc.IsValueType)
                        throw new NotSupportedException(SR.Format(SR.XmlRpcNestedValueType, member.TypeDesc.FullName));
                }
                if (mapping.BaseMapping != null)
                {
                    if (mapping.BaseMapping.Declares(member, mapping.TypeName!)) continue;
                }
                members.Add(member);
            }
            mapping.Members = members.ToArray();
            if (mapping.BaseMapping == null) mapping.BaseMapping = GetRootMapping();
            IncludeTypes(model.Type, limiter);

            return true;
        }

        [RequiresUnreferencedCode("calls IncludeTypes")]
        private ArrayMapping ImportArrayLikeMapping(ArrayModel model, RecursionLimiter limiter)
        {
            ArrayMapping mapping = new ArrayMapping();
            mapping.IsSoap = true;
            TypeMapping itemTypeMapping = ImportTypeMapping(model.Element, limiter);

            if (itemTypeMapping.TypeDesc!.IsValueType && !itemTypeMapping.TypeDesc.IsPrimitive && !itemTypeMapping.TypeDesc.IsEnum)
                throw new NotSupportedException(SR.Format(SR.XmlRpcArrayOfValueTypes, model.TypeDesc.FullName));

            mapping.TypeDesc = model.TypeDesc;
            mapping.Elements = new ElementAccessor[] {
                CreateElementAccessor(itemTypeMapping, mapping.Namespace) };
            SetArrayMappingType(mapping);

            // in the case of an ArrayMapping we can have more that one mapping correspond to a type
            // examples of that are ArrayList and object[] both will map tp ArrayOfur-type
            // so we create a link list for all mappings of the same XSD type
            ArrayMapping? existingMapping = (ArrayMapping?)_types[mapping.TypeName!, mapping.Namespace];
            if (existingMapping != null)
            {
                ArrayMapping first = existingMapping;
                while (existingMapping != null)
                {
                    if (existingMapping.TypeDesc == model.TypeDesc)
                        return existingMapping;
                    existingMapping = existingMapping.Next;
                }
                mapping.Next = first;
                _types[mapping.TypeName!, mapping.Namespace] = mapping;
                return mapping;
            }
            _typeScope.AddTypeMapping(mapping);
            _types.Add(mapping.TypeName, mapping.Namespace, mapping);
            IncludeTypes(model.Type);
            return mapping;
        }

        // UNDONE Nullable
        private void SetArrayMappingType(ArrayMapping mapping)
        {
            bool useDefaultNs = false;

            string itemTypeName;
            string itemTypeNamespace;

            TypeMapping? itemTypeMapping;
            if (mapping.Elements!.Length == 1)
                itemTypeMapping = mapping.Elements[0].Mapping;
            else
                itemTypeMapping = null;

            if (itemTypeMapping is EnumMapping)
            {
                itemTypeNamespace = itemTypeMapping.Namespace!;
                itemTypeName = itemTypeMapping.TypeName!;
            }
            else if (itemTypeMapping is PrimitiveMapping)
            {
                itemTypeNamespace = itemTypeMapping.TypeDesc!.IsXsdType ? XmlSchema.Namespace : UrtTypes.Namespace;
                itemTypeName = itemTypeMapping.TypeDesc.DataType!.Name!;
                useDefaultNs = true;
            }
            else if (itemTypeMapping is StructMapping)
            {
                if (itemTypeMapping.TypeDesc!.IsRoot)
                {
                    itemTypeNamespace = XmlSchema.Namespace;
                    itemTypeName = Soap.UrType;
                    useDefaultNs = true;
                }
                else
                {
                    itemTypeNamespace = itemTypeMapping.Namespace!;
                    itemTypeName = itemTypeMapping.TypeName!;
                }
            }
            else if (itemTypeMapping is ArrayMapping)
            {
                itemTypeNamespace = itemTypeMapping.Namespace!;
                itemTypeName = itemTypeMapping.TypeName!;
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidSoapArray, mapping.TypeDesc!.FullName));
            }

            itemTypeName = CodeIdentifier.MakePascal(itemTypeName);
            string uniqueName = "ArrayOf" + itemTypeName;
            string ns = useDefaultNs ? _defaultNs : itemTypeNamespace;
            int i = 1;
            TypeMapping? existingMapping = (TypeMapping?)_types[uniqueName, ns];
            while (existingMapping != null)
            {
                if (existingMapping is ArrayMapping)
                {
                    ArrayMapping arrayMapping = (ArrayMapping)existingMapping;
                    if (AccessorMapping.ElementsMatch(arrayMapping.Elements, mapping.Elements))
                    {
                        break;
                    }
                }
                // need to re-name the mapping
                uniqueName = itemTypeName + i.ToString(CultureInfo.InvariantCulture);
                existingMapping = (TypeMapping?)_types[uniqueName, ns];
                i++;
            }
            mapping.Namespace = ns;
            mapping.TypeName = uniqueName;
        }

        private PrimitiveMapping ImportPrimitiveMapping(PrimitiveModel model, string dataType)
        {
            PrimitiveMapping mapping = new PrimitiveMapping();
            mapping.IsSoap = true;
            if (dataType.Length > 0)
            {
                mapping.TypeDesc = _typeScope.GetTypeDesc(dataType, XmlSchema.Namespace);
                if (mapping.TypeDesc == null)
                {
                    // try it as a non-Xsd type
                    mapping.TypeDesc = _typeScope.GetTypeDesc(dataType, UrtTypes.Namespace);
                    if (mapping.TypeDesc == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUdeclaredXsdType, dataType));
                    }
                }
            }
            else
            {
                mapping.TypeDesc = model.TypeDesc;
            }
            mapping.TypeName = mapping.TypeDesc.DataType!.Name;
            mapping.Namespace = mapping.TypeDesc.IsXsdType ? XmlSchema.Namespace : UrtTypes.Namespace;
            return mapping;
        }

        [RequiresUnreferencedCode("calls XsdTypeName")]
        private EnumMapping ImportEnumMapping(EnumModel model)
        {
            SoapAttributes a = GetAttributes(model.Type);
            string typeNs = _defaultNs;
            if (a.SoapType != null && a.SoapType.Namespace != null)
                typeNs = a.SoapType.Namespace;
            string typeName = XsdTypeName(model.Type, a, model.TypeDesc.Name);
            typeName = XmlConvert.EncodeLocalName(typeName);

            EnumMapping? mapping = (EnumMapping?)GetTypeMapping(typeName, typeNs, model.TypeDesc);
            if (mapping == null)
            {
                mapping = new EnumMapping();
                mapping.IsSoap = true;
                mapping.TypeDesc = model.TypeDesc;
                mapping.TypeName = typeName;
                mapping.Namespace = typeNs;
                mapping.IsFlags = model.Type.IsDefined(typeof(FlagsAttribute), false);
                _typeScope.AddTypeMapping(mapping);
                _types.Add(typeName, typeNs, mapping);
                var constants = new List<ConstantMapping>();
                for (int i = 0; i < model.Constants.Length; i++)
                {
                    ConstantMapping? constant = ImportConstantMapping(model.Constants[i]);
                    if (constant != null) constants.Add(constant);
                }
                if (constants.Count == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlNoSerializableMembers, model.TypeDesc.FullName));
                }
                mapping.Constants = constants.ToArray();
            }
            return mapping;
        }

        private ConstantMapping? ImportConstantMapping(ConstantModel model)
        {
            SoapAttributes a = GetAttributes(model.FieldInfo);
            if (a.SoapIgnore) return null;
            if ((a.GetSoapFlags() & ~SoapAttributeFlags.Enum) != 0)
                throw new InvalidOperationException(SR.XmlInvalidEnumAttribute);
            if (a.SoapEnum == null)
                a.SoapEnum = new SoapEnumAttribute();

            ConstantMapping constant = new ConstantMapping();
            constant.XmlName = a.SoapEnum.Name.Length == 0 ? model.Name : a.SoapEnum.Name;
            constant.Name = model.Name;
            constant.Value = model.Value;
            return constant;
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        private MembersMapping ImportMembersMapping(XmlReflectionMember[] xmlReflectionMembers, string? ns, bool hasWrapperElement, bool writeAccessors, bool validateWrapperElement, RecursionLimiter limiter)
        {
            MembersMapping members = new MembersMapping();
            members.TypeDesc = _typeScope.GetTypeDesc(typeof(object[]));
            MemberMapping[] mappings = new MemberMapping[xmlReflectionMembers.Length];
            for (int i = 0; i < mappings.Length; i++)
            {
                try
                {
                    XmlReflectionMember member = xmlReflectionMembers[i];
                    MemberMapping? mapping = ImportMemberMapping(member, ns, xmlReflectionMembers, hasWrapperElement ? XmlSchemaForm.Unqualified : XmlSchemaForm.Qualified, limiter);
                    if (member.IsReturnValue && writeAccessors)
                    { // no special treatment for return values with doc/enc
                        if (i > 0) throw new InvalidOperationException(SR.XmlInvalidReturnPosition);
                        mapping!.IsReturnValue = true;
                    }
                    mappings[i] = mapping!;
                }
                catch (Exception e)
                {
                    if (e is OutOfMemoryException)
                    {
                        throw;
                    }
                    throw ReflectionException(xmlReflectionMembers[i].MemberName, e);
                }
            }
            members.Members = mappings;
            members.HasWrapperElement = hasWrapperElement;
            if (hasWrapperElement)
            {
                members.ValidateRpcWrapperElement = validateWrapperElement;
            }
            members.WriteAccessors = writeAccessors;
            members.IsSoap = true;
            if (hasWrapperElement && !writeAccessors)
                members.Namespace = ns;
            return members;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private MemberMapping? ImportMemberMapping(XmlReflectionMember xmlReflectionMember, string? ns, XmlReflectionMember[] xmlReflectionMembers, XmlSchemaForm form, RecursionLimiter limiter)
        {
            SoapAttributes a = xmlReflectionMember.SoapAttributes;
            if (a.SoapIgnore) return null;
            MemberMapping member = new MemberMapping();
            member.IsSoap = true;
            member.Name = xmlReflectionMember.MemberName;
            bool checkSpecified = XmlReflectionImporter.FindSpecifiedMember(xmlReflectionMember.MemberName, xmlReflectionMembers) != null;
            FieldModel model = new FieldModel(xmlReflectionMember.MemberName, xmlReflectionMember.MemberType!, _typeScope.GetTypeDesc(xmlReflectionMember.MemberType!), checkSpecified, false);
            member.CheckShouldPersist = model.CheckShouldPersist;
            member.CheckSpecified = model.CheckSpecified;
            member.ReadOnly = model.ReadOnly; // || !model.FieldTypeDesc.HasDefaultConstructor;
            ImportAccessorMapping(member, model, a, ns, form, limiter);
            if (xmlReflectionMember.OverrideIsNullable)
                member.Elements![0].IsNullable = false;
            return member;
        }

        [RequiresUnreferencedCode("calls ImportAccessorMapping")]
        private MemberMapping? ImportFieldMapping(FieldModel model, SoapAttributes a, string ns, RecursionLimiter limiter)
        {
            if (a.SoapIgnore) return null;
            MemberMapping member = new MemberMapping();
            member.IsSoap = true;
            member.Name = model.Name;
            member.CheckShouldPersist = model.CheckShouldPersist;
            member.CheckSpecified = model.CheckSpecified;
            member.MemberInfo = model.MemberInfo;
            member.CheckSpecifiedMemberInfo = model.CheckSpecifiedMemberInfo;
            member.CheckShouldPersistMethodInfo = model.CheckShouldPersistMethodInfo;
            member.ReadOnly = model.ReadOnly; // || !model.FieldTypeDesc.HasDefaultConstructor;
            ImportAccessorMapping(member, model, a, ns, XmlSchemaForm.Unqualified, limiter);
            return member;
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        private void ImportAccessorMapping(MemberMapping accessor, FieldModel model, SoapAttributes a, string? ns, XmlSchemaForm form, RecursionLimiter limiter)
        {
            Type accessorType = model.FieldType;
            string accessorName = model.Name;
            accessor.TypeDesc = _typeScope.GetTypeDesc(accessorType);
            if (accessor.TypeDesc.IsVoid)
            {
                throw new InvalidOperationException(SR.XmlInvalidVoid);
            }

            SoapAttributeFlags flags = a.GetSoapFlags();
            if ((flags & SoapAttributeFlags.Attribute) == SoapAttributeFlags.Attribute)
            {
                if (!accessor.TypeDesc.IsPrimitive && !accessor.TypeDesc.IsEnum)
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalSoapAttribute, accessorName, accessor.TypeDesc.FullName));

                if ((flags & SoapAttributeFlags.Attribute) != flags)
                    throw new InvalidOperationException(SR.XmlInvalidElementAttribute);

                AttributeAccessor attribute = new AttributeAccessor();
                attribute.Name = Accessor.EscapeQName(a.SoapAttribute == null || a.SoapAttribute.AttributeName.Length == 0 ? accessorName : a.SoapAttribute.AttributeName);
                attribute.Namespace = a.SoapAttribute == null || a.SoapAttribute.Namespace == null ? ns : a.SoapAttribute.Namespace;
                attribute.Form = XmlSchemaForm.Qualified; // attributes are always qualified since they're only used for encoded soap headers
                attribute.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(accessorType), (a.SoapAttribute == null ? string.Empty : a.SoapAttribute.DataType), limiter);
                attribute.Default = GetDefaultValue(model.FieldTypeDesc, a);
                accessor.Attribute = attribute;
                accessor.Elements = Array.Empty<ElementAccessor>();
            }
            else
            {
                if ((flags & SoapAttributeFlags.Element) != flags)
                    throw new InvalidOperationException(SR.XmlInvalidElementAttribute);

                ElementAccessor element = new ElementAccessor();
                element.IsSoap = true;
                element.Name = XmlConvert.EncodeLocalName(a.SoapElement == null || a.SoapElement.ElementName.Length == 0 ? accessorName : a.SoapElement.ElementName);
                element.Namespace = ns;
                element.Form = form;
                element.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(accessorType), (a.SoapElement == null ? string.Empty : a.SoapElement.DataType), limiter);
                if (a.SoapElement != null)
                    element.IsNullable = a.SoapElement.IsNullable;
                accessor.Elements = new ElementAccessor[] { element };
            }
        }

        private static ElementAccessor CreateElementAccessor(TypeMapping mapping, string? ns)
        {
            ElementAccessor element = new ElementAccessor();
            element.IsSoap = true;
            element.Name = mapping.TypeName; //XmlConvert.EncodeLocalName(name == null || name.Length == 0 ? mapping.TypeName : name);
            element.Namespace = ns;
            element.Mapping = mapping;
            return element;
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        private object? GetDefaultValue(TypeDesc fieldTypeDesc, SoapAttributes a)
        {
            if (a.SoapDefaultValue == null || a.SoapDefaultValue == DBNull.Value) return null;
            if (!(fieldTypeDesc.Kind == TypeKind.Primitive || fieldTypeDesc.Kind == TypeKind.Enum))
            {
                a.SoapDefaultValue = null;
                return a.SoapDefaultValue;
            }
            // for enums validate and return a string representation
            if (fieldTypeDesc.Kind == TypeKind.Enum)
            {
                if (fieldTypeDesc != _typeScope.GetTypeDesc(a.SoapDefaultValue.GetType()))
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidDefaultEnumValue, a.SoapDefaultValue.GetType().FullName, fieldTypeDesc.FullName));
                string strValue = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "G").Replace(",", " ");
                string numValue = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "D");
                if (strValue == numValue) // means enum value wasn't recognized
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidDefaultValue, strValue, a.SoapDefaultValue.GetType().FullName));
                return strValue;
            }
            return a.SoapDefaultValue;
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        internal string XsdTypeName(Type type)
        {
            if (type == typeof(object)) return Soap.UrType;
            TypeDesc typeDesc = _typeScope.GetTypeDesc(type);
            if (typeDesc.IsPrimitive && typeDesc.DataType != null && typeDesc.DataType.Name != null && typeDesc.DataType.Name.Length > 0)
                return typeDesc.DataType.Name;
            return XsdTypeName(type, GetAttributes(type), typeDesc.Name);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        internal string XsdTypeName(Type type, SoapAttributes a, string name)
        {
            string typeName = name;
            if (a.SoapType != null && a.SoapType.TypeName.Length > 0)
                typeName = a.SoapType.TypeName;

            if (type.IsGenericType && typeName.Contains('{'))
            {
                Type genType = type.GetGenericTypeDefinition();
                Type[] names = genType.GetGenericArguments();
                Type[] types = type.GetGenericArguments();

                for (int i = 0; i < names.Length; i++)
                {
                    string argument = "{" + names[i] + "}";
                    if (typeName.Contains(argument))
                    {
                        typeName = typeName.Replace(argument, XsdTypeName(types[i]));
                        if (!typeName.Contains('{'))
                        {
                            break;
                        }
                    }
                }
            }
            // CONSIDER: throw if not all parameters were filled
            return typeName;
        }
    }
}
