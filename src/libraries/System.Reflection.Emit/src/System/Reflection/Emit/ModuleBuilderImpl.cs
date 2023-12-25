// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed class ModuleBuilderImpl : ModuleBuilder
    {
        private readonly Assembly _coreAssembly;
        private readonly string _name;
        private readonly MetadataBuilder _metadataBuilder;
        private readonly AssemblyBuilderImpl _assemblyBuilder;
        private readonly Dictionary<Assembly, AssemblyReferenceHandle> _assemblyReferences = new();
        private readonly Dictionary<Type, EntityHandle> _typeReferences = new();
        private readonly Dictionary<MemberInfo, EntityHandle> _memberReferences = new();
        private readonly List<TypeBuilderImpl> _typeDefinitions = new();
        private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorReferences = new();
        private readonly Guid _moduleVersionId;
        private Dictionary<string, ModuleReferenceHandle>? _moduleReferences;
        private List<CustomAttributeWrapper>? _customAttributes;
        private int _nextTypeDefRowId = 1;
        private int _nextMethodDefRowId = 1;
        private int _nextFieldDefRowId = 1;
        private int _nextParameterRowId = 1;
        private int _nextPropertyRowId = 1;
        private int _nextEventRowId = 1;
        private bool _coreTypesFullyPopulated;
        private Type?[]? _coreTypes;
        private static readonly Type[] s_coreTypes = { typeof(void), typeof(object), typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int),
                                                       typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(nint), typeof(nuint), typeof(TypedReference) };

        internal ModuleBuilderImpl(string name, Assembly coreAssembly, MetadataBuilder builder, AssemblyBuilderImpl assemblyBuilder)
        {
            _coreAssembly = coreAssembly;
            _name = name;
            _metadataBuilder = builder;
            _assemblyBuilder = assemblyBuilder;
            _moduleVersionId = Guid.NewGuid();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved via s_coreTypes")]
        internal Type GetTypeFromCoreAssembly(CoreTypeId typeId)
        {
            if (_coreTypes == null)
            {
                // Use s_coreTypes directly for runtime reflection
                if (_coreAssembly == typeof(object).Assembly)
                {
                    _coreTypes = s_coreTypes;
                    _coreTypesFullyPopulated = true;
                }
                else
                {
                    _coreTypes = new Type[s_coreTypes.Length];
                }
            }

            int index = (int)typeId;
            return _coreTypes[index] ?? (_coreTypes[index] = _coreAssembly.GetType(s_coreTypes[index].FullName!, throwOnError: true)!);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved via s_coreTypes")]
        internal CoreTypeId? GetTypeIdFromCoreTypes(Type type)
        {
            if (_coreTypes == null)
            {
                // Use s_coreTypes directly for runtime reflection
                if (_coreAssembly == typeof(object).Assembly)
                {
                    _coreTypes = s_coreTypes;
                    _coreTypesFullyPopulated = true;
                }
                else
                {
                    _coreTypes = new Type[s_coreTypes.Length];
                }
            }

            if (!_coreTypesFullyPopulated)
            {
                for (int i = 0; i < _coreTypes.Length; i++)
                {
                    if (_coreTypes[i] == null)
                    {
                        _coreTypes[i] = _coreAssembly.GetType(s_coreTypes[i].FullName!, throwOnError: false)!;
                    }
                }
                _coreTypesFullyPopulated = true;
            }

            for (int i = 0; i < _coreTypes.Length; i++)
            {
                if (_coreTypes[i] == type)
                {
                    return (CoreTypeId)i;
                }
            }

            return null;
        }

        internal void AppendMetadata(MethodBodyStreamEncoder methodBodyEncoder)
        {
            // Add module metadata
            ModuleDefinitionHandle moduleHandle = _metadataBuilder.AddModule(
                generation: 0,
                moduleName: _metadataBuilder.GetOrAddString(_name),
                mvid: _metadataBuilder.GetOrAddGuid(_moduleVersionId),
                encId: default,
                encBaseId: default);

            // Create type definition for the special <Module> type that holds global functions
            _metadataBuilder.AddTypeDefinition(
                attributes: default,
                @namespace: default,
                name: _metadataBuilder.GetOrAddString("<Module>"),
                baseType: default,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            WriteCustomAttributes(_customAttributes, moduleHandle);

            _typeDefinitions.Sort((x, y) => x.MetadataToken.CompareTo(y.MetadataToken));

            // All generic parameters for all types and methods should be written in specific order
            List<GenericTypeParameterBuilderImpl> genericParams = new();
            // Add each type definition to metadata table.
            foreach (TypeBuilderImpl typeBuilder in _typeDefinitions)
            {
                typeBuilder.ThrowIfNotCreated();

                EntityHandle parent = default;
                if (typeBuilder.BaseType is not null)
                {
                    parent = GetTypeHandle(typeBuilder.BaseType);
                }

                TypeDefinitionHandle typeHandle = AddTypeDefinition(typeBuilder, parent, typeBuilder._firstMethodToken, typeBuilder._firstFieldToken);
                Debug.Assert(typeBuilder._handle.Equals(typeHandle));

                if (typeBuilder.IsGenericType)
                {
                    foreach (GenericTypeParameterBuilderImpl param in typeBuilder.GenericTypeParameters)
                    {
                        param._parentHandle = typeHandle;
                        genericParams.Add(param);
                    }
                }

                if ((typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
                {
                    _metadataBuilder.AddTypeLayout(typeHandle, (ushort)typeBuilder.PackingSize, (uint)typeBuilder.Size);
                }

                if (typeBuilder.DeclaringType != null)
                {
                    _metadataBuilder.AddNestedType(typeHandle, (TypeDefinitionHandle)GetTypeHandle(typeBuilder.DeclaringType));
                }

                WriteInterfaceImplementations(typeBuilder, typeHandle);
                WriteCustomAttributes(typeBuilder._customAttributes, typeHandle);
                WriteProperties(typeBuilder);
                WriteFields(typeBuilder);
                WriteMethods(typeBuilder._methodDefinitions, genericParams, methodBodyEncoder);
                WriteEvents(typeBuilder);
            }

            // Now write all generic parameters in order
            genericParams.Sort((x, y) => {
                int primary = CodedIndex.TypeOrMethodDef(x._parentHandle).CompareTo(CodedIndex.TypeOrMethodDef(y._parentHandle));
                if (primary != 0)
                    return primary;

                return x.GenericParameterPosition.CompareTo(y.GenericParameterPosition);
            });

            foreach (GenericTypeParameterBuilderImpl param in genericParams)
            {
                AddGenericTypeParametersAndConstraintsCustomAttributes(param._parentHandle, param);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:DynamicallyAccessedMembers", Justification = "Members are retrieved from internal cache")]
        private void WriteInterfaceImplementations(TypeBuilderImpl typeBuilder, TypeDefinitionHandle typeHandle)
        {
            if (typeBuilder._interfaces != null)
            {
                foreach (Type iface in typeBuilder._interfaces)
                {
                    _metadataBuilder.AddInterfaceImplementation(typeHandle, GetTypeHandle(iface));
                }
            }

            // Even there were no interfaces implemented it could have an override for base type abstract method
            if (typeBuilder._methodOverrides != null)
            {
                foreach (List<(MethodInfo ifaceMethod, MethodInfo targetMethod)> mapList in typeBuilder._methodOverrides.Values)
                {
                    foreach ((MethodInfo ifaceMethod, MethodInfo targetMethod) pair in mapList)
                    {
                        _metadataBuilder.AddMethodImplementation(typeHandle, GetMemberHandle(pair.targetMethod), GetMemberHandle(pair.ifaceMethod));
                    }
                }
            }
        }

        private void WriteEvents(TypeBuilderImpl typeBuilder)
        {
            if (typeBuilder._eventDefinitions.Count == 0)
            {
                return;
            }

            AddEventMap(typeBuilder._handle, typeBuilder._firstEventToken);
            foreach (EventBuilderImpl eventBuilder in typeBuilder._eventDefinitions)
            {
                EventDefinitionHandle eventHandle = AddEventDefinition(eventBuilder, GetTypeHandle(eventBuilder.EventType));
                WriteCustomAttributes(eventBuilder._customAttributes, eventHandle);

                if (eventBuilder._addOnMethod is MethodBuilderImpl aMb)
                {
                    AddMethodSemantics(eventHandle, MethodSemanticsAttributes.Adder, aMb._handle);
                }

                if (eventBuilder._raiseMethod is MethodBuilderImpl rMb)
                {
                    AddMethodSemantics(eventHandle, MethodSemanticsAttributes.Raiser, rMb._handle);
                }

                if (eventBuilder._removeMethod is MethodBuilderImpl remMb)
                {
                    AddMethodSemantics(eventHandle, MethodSemanticsAttributes.Remover, remMb._handle);
                }

                if (eventBuilder._otherMethods != null)
                {
                    foreach (MethodBuilderImpl method in eventBuilder._otherMethods)
                    {
                        AddMethodSemantics(eventHandle, MethodSemanticsAttributes.Other, method._handle);
                    }
                }
            }
        }

        private void WriteProperties(TypeBuilderImpl typeBuilder)
        {
            if (typeBuilder._propertyDefinitions.Count == 0)
            {
                return;
            }

            AddPropertyMap(typeBuilder._handle, typeBuilder._firstPropertyToken);
            foreach (PropertyBuilderImpl property in typeBuilder._propertyDefinitions)
            {
                PropertyDefinitionHandle propertyHandle = AddPropertyDefinition(property, MetadataSignatureHelper.PropertySignatureEncoder(property, this));
                WriteCustomAttributes(property._customAttributes, propertyHandle);

                if (property.GetMethod is MethodBuilderImpl gMb)
                {
                    AddMethodSemantics(propertyHandle, MethodSemanticsAttributes.Getter, gMb._handle);
                }

                if (property.SetMethod is MethodBuilderImpl sMb)
                {
                    AddMethodSemantics(propertyHandle, MethodSemanticsAttributes.Setter, sMb._handle);
                }

                if (property._otherMethods != null)
                {
                    foreach (MethodBuilderImpl method in property._otherMethods)
                    {
                        AddMethodSemantics(propertyHandle, MethodSemanticsAttributes.Other, method._handle);
                    }
                }

                if (property._defaultValue != DBNull.Value)
                {
                    AddDefaultValue(propertyHandle, property._defaultValue);
                }
            }
        }

        private void PopulateFieldDefinitionHandles(List<FieldBuilderImpl> fieldDefinitions)
        {
            foreach (FieldBuilderImpl field in fieldDefinitions)
            {
                field._handle = MetadataTokens.FieldDefinitionHandle(_nextFieldDefRowId++);
            }
        }

        private void PopulateMethodDefinitionHandles(List<MethodBuilderImpl> methods)
        {
            foreach (MethodBuilderImpl method in methods)
            {
                method._handle = MetadataTokens.MethodDefinitionHandle(_nextMethodDefRowId++);
            }
        }

        private void PopulatePropertyDefinitionHandles(List<PropertyBuilderImpl> properties)
        {
            foreach (PropertyBuilderImpl property in properties)
            {
                property._handle = MetadataTokens.PropertyDefinitionHandle(_nextPropertyRowId++);
            }
        }

        private void PopulateEventDefinitionHandles(List<EventBuilderImpl> eventDefinitions)
        {
            foreach (EventBuilderImpl eventBuilder in eventDefinitions)
            {
                eventBuilder._handle = MetadataTokens.EventDefinitionHandle(_nextEventRowId++);
            }
        }

        internal void PopulateTypeAndItsMembersTokens(TypeBuilderImpl typeBuilder)
        {
            typeBuilder._handle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
            typeBuilder._firstMethodToken = _nextMethodDefRowId;
            typeBuilder._firstFieldToken = _nextFieldDefRowId;
            typeBuilder._firstPropertyToken = _nextPropertyRowId;
            typeBuilder._firstEventToken = _nextEventRowId;
            PopulateMethodDefinitionHandles(typeBuilder._methodDefinitions);
            PopulateFieldDefinitionHandles(typeBuilder._fieldDefinitions);
            PopulatePropertyDefinitionHandles(typeBuilder._propertyDefinitions);
            PopulateEventDefinitionHandles(typeBuilder._eventDefinitions);
        }

        private void WriteMethods(List<MethodBuilderImpl> methods, List<GenericTypeParameterBuilderImpl> genericParams, MethodBodyStreamEncoder methodBodyEncoder)
        {
            foreach (MethodBuilderImpl method in methods)
            {
                int offset = -1;
                ILGeneratorImpl? il = method.ILGeneratorImpl;
                if (il != null)
                {
                    FillMemberReferences(il);
                    StandaloneSignatureHandle signature = il.Locals.Count == 0 ? default :
                        _metadataBuilder.AddStandaloneSignature(_metadataBuilder.GetOrAddBlob(MetadataSignatureHelper.LocalSignatureEncoder(il.Locals, this)));
                    offset = AddMethodBody(method, il, signature, methodBodyEncoder);
                }

                MethodDefinitionHandle handle = AddMethodDefinition(method, method.GetMethodSignatureBlob(), offset, _nextParameterRowId);
                Debug.Assert(method._handle == handle);
                WriteCustomAttributes(method._customAttributes, handle);

                if (method.IsGenericMethodDefinition)
                {
                    Type[] gParams = method.GetGenericArguments();
                    for (int i = 0; i < gParams.Length; i++)
                    {
                        GenericTypeParameterBuilderImpl param = (GenericTypeParameterBuilderImpl)gParams[i];
                        param._parentHandle = handle;
                        genericParams.Add(param);
                    }
                }

                if (method._parameterBuilders != null)
                {
                    foreach (ParameterBuilderImpl parameter in method._parameterBuilders)
                    {
                        if (parameter != null)
                        {
                            ParameterHandle parameterHandle = AddParameter(parameter);
                            WriteCustomAttributes(parameter._customAttributes, parameterHandle);
                            _nextParameterRowId++;

                            if (parameter._marshallingData != null)
                            {
                                AddMarshalling(parameterHandle, parameter._marshallingData.SerializeMarshallingData());
                            }

                            if (parameter._defaultValue != DBNull.Value)
                            {
                                AddDefaultValue(parameterHandle, parameter._defaultValue);
                            }
                        }
                    }
                }

                if (method._dllImportData != null)
                {
                    AddMethodImport(handle, method._dllImportData.EntryPoint ?? method.Name,
                        method._dllImportData.Flags, GetModuleReference(method._dllImportData.ModuleName));
                }
            }
        }

        private void FillMemberReferences(ILGeneratorImpl il)
        {
            foreach (KeyValuePair<MemberInfo, BlobWriter> pair in il.GetMemberReferences())
            {
                pair.Value.WriteInt32(MetadataTokens.GetToken(GetMemberHandle(pair.Key)));
            }
        }

        private static int AddMethodBody(MethodBuilderImpl method, ILGeneratorImpl il, StandaloneSignatureHandle signature, MethodBodyStreamEncoder bodyEncoder) =>
            bodyEncoder.AddMethodBody(
                instructionEncoder: il.Instructions,
                maxStack: il.GetMaxStackSize(),
                localVariablesSignature: signature,
                attributes: method.InitLocals ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None,
                hasDynamicStackAllocation: il.HasDynamicStackAllocation);

        private void WriteFields(TypeBuilderImpl typeBuilder)
        {
            foreach (FieldBuilderImpl field in typeBuilder._fieldDefinitions)
            {
                FieldDefinitionHandle handle = AddFieldDefinition(field, MetadataSignatureHelper.FieldSignatureEncoder(field.FieldType, this));
                Debug.Assert(field._handle == handle);
                WriteCustomAttributes(field._customAttributes, handle);

                if (field._offset > 0 && (typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
                {
                    AddFieldLayout(handle, field._offset);
                }

                if (field._marshallingData != null)
                {
                    AddMarshalling(handle, field._marshallingData.SerializeMarshallingData());
                }

                if (field._defaultValue != DBNull.Value)
                {
                    AddDefaultValue(handle, field._defaultValue);
                }
            }
        }

        private ModuleReferenceHandle GetModuleReference(string moduleName)
        {
            _moduleReferences ??= new Dictionary<string, ModuleReferenceHandle>();

            if (!_moduleReferences.TryGetValue(moduleName, out var handle))
            {
                handle = AddModuleReference(moduleName);
                _moduleReferences.Add(moduleName, handle);
            }

            return handle;
        }

        internal void WriteCustomAttributes(List<CustomAttributeWrapper>? customAttributes, EntityHandle parent)
        {
            if (customAttributes != null)
            {
                foreach (CustomAttributeWrapper customAttribute in customAttributes)
                {
                    _metadataBuilder.AddCustomAttribute(parent, GetConstructorHandle(customAttribute.Ctor),
                        _metadataBuilder.GetOrAddBlob(customAttribute.Data));
                }
            }
        }

        private MemberReferenceHandle GetConstructorHandle(ConstructorInfo constructorInfo)
        {
            if (!_ctorReferences.TryGetValue(constructorInfo, out var constructorHandle))
            {
                EntityHandle parentHandle = GetTypeReferenceOrSpecificationHandle(constructorInfo.DeclaringType!);
                constructorHandle = AddConstructorReference(parentHandle, constructorInfo);
                _ctorReferences.Add(constructorInfo, constructorHandle);
            }

            return constructorHandle;
        }

        private EntityHandle GetTypeReferenceOrSpecificationHandle(Type type)
        {
            if (!_typeReferences.TryGetValue(type, out var typeHandle))
            {
                if (type.IsArray || type.IsGenericParameter || (type.IsGenericType && !type.IsGenericTypeDefinition))
                {
                    typeHandle = AddTypeSpecification(type);
                }
                else
                {
                    typeHandle = AddTypeReference(type, GetResolutionScopeHandle(type));
                }

                _typeReferences.Add(type, typeHandle);
            }

            return typeHandle;
        }

        private EntityHandle GetResolutionScopeHandle(Type type)
        {
            if (type.IsNested)
            {
                return GetTypeReferenceOrSpecificationHandle(type.DeclaringType!);
            }

            return GetAssemblyReference(type.Assembly);
        }

        private TypeSpecificationHandle AddTypeSpecification(Type type) =>
            _metadataBuilder.AddTypeSpecification(
                signature: _metadataBuilder.GetOrAddBlob(MetadataSignatureHelper.GetTypeSpecificationSignature(type, this)));

        private MethodSpecificationHandle AddMethodSpecification(EntityHandle methodHandle, Type[] genericArgs) =>
            _metadataBuilder.AddMethodSpecification(
                method: methodHandle,
                instantiation: _metadataBuilder.GetOrAddBlob(MetadataSignatureHelper.GetMethodSpecificationSignature(genericArgs, this)));

        private EntityHandle GetMemberReference(MemberInfo member)
        {
            if (!_memberReferences.TryGetValue(member, out var memberHandle))
            {
                if (member is MethodInfo method && method.IsConstructedGenericMethod)
                {
                    memberHandle = AddMethodSpecification(GetMemberReference(method.GetGenericMethodDefinition()), method.GetGenericArguments());
                }
                else
                {
                    memberHandle = AddMemberReference(member.Name, GetTypeHandle(member.DeclaringType!), GetMemberSignature(member));
                }

                _memberReferences.Add(member, memberHandle);
            }

            return memberHandle;
        }

        private BlobBuilder GetMemberSignature(MemberInfo member)
        {
            if (member is MethodInfo method)
            {
                return MetadataSignatureHelper.MethodSignatureEncoder(this, ParameterTypes(method.GetParameters()), method.ReturnType,
                    MethodBuilderImpl.GetSignatureConvention(method.CallingConvention), method.GetGenericArguments().Length, !method.IsStatic);
            }

            if (member is FieldInfo field)
            {
                return MetadataSignatureHelper.FieldSignatureEncoder(field.FieldType, this);
            }

            if (member is ConstructorInfo ctor)
            {
                return MetadataSignatureHelper.ConstructorSignatureEncoder(ctor.GetParameters(), this);
            }

            throw new NotSupportedException();
        }

        private static Type[] ParameterTypes(ParameterInfo[] parameterInfos)
        {
            if (parameterInfos.Length == 0)
            {
                return Type.EmptyTypes;
            }

            Type[] parameterTypes = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            return parameterTypes;
        }

        private AssemblyReferenceHandle GetAssemblyReference(Assembly assembly)
        {
            if (!_assemblyReferences.TryGetValue(assembly, out var handle))
            {
                AssemblyName aName = assembly.GetName();
                handle = AddAssemblyReference(aName.Name, aName.Version, aName.CultureName, aName.GetPublicKeyToken(), aName.Flags, aName.ContentType);
                _assemblyReferences.Add(assembly, handle);
            }

            return handle;
        }

        private void AddGenericTypeParametersAndConstraintsCustomAttributes(EntityHandle parentHandle, GenericTypeParameterBuilderImpl gParam)
        {
            GenericParameterHandle handle = _metadataBuilder.AddGenericParameter(
                parent: parentHandle,
                attributes: gParam.GenericParameterAttributes,
                name: _metadataBuilder.GetOrAddString(gParam.Name),
                index: gParam.GenericParameterPosition);

            WriteCustomAttributes(gParam._customAttributes, handle);
            foreach (Type constraint in gParam.GetGenericParameterConstraints())
            {
                _metadataBuilder.AddGenericParameterConstraint(handle, GetTypeHandle(constraint));
            }
        }

        private void AddDefaultValue(EntityHandle parentHandle, object? defaultValue) =>
            _metadataBuilder.AddConstant(parent: parentHandle, value: defaultValue);

        private void AddMethodSemantics(EntityHandle parentHandle, MethodSemanticsAttributes attribute, MethodDefinitionHandle methodHandle) =>
            _metadataBuilder.AddMethodSemantics(
                association: parentHandle,
                semantics: attribute,
                methodDefinition: methodHandle);

        private PropertyDefinitionHandle AddPropertyDefinition(PropertyBuilderImpl property, BlobBuilder signature) =>
            _metadataBuilder.AddProperty(
                attributes: property.Attributes,
                name: _metadataBuilder.GetOrAddString(property.Name),
                signature: _metadataBuilder.GetOrAddBlob(signature));

        private EventDefinitionHandle AddEventDefinition(EventBuilderImpl eventBuilder, EntityHandle eventType) =>
            _metadataBuilder.AddEvent(
                attributes: eventBuilder.Attributes,
                name: _metadataBuilder.GetOrAddString(eventBuilder.Name),
                type: eventType);

        private void AddEventMap(TypeDefinitionHandle typeHandle, int firstEventToken) =>
            _metadataBuilder.AddEventMap(
                declaringType: typeHandle,
                eventList: MetadataTokens.EventDefinitionHandle(firstEventToken));

        private void AddPropertyMap(TypeDefinitionHandle typeHandle, int firstPropertyToken) =>
            _metadataBuilder.AddPropertyMap(
                declaringType: typeHandle,
                propertyList: MetadataTokens.PropertyDefinitionHandle(firstPropertyToken));

        private FieldDefinitionHandle AddFieldDefinition(FieldBuilderImpl field, BlobBuilder fieldSignature) =>
            _metadataBuilder.AddFieldDefinition(
                attributes: field.Attributes,
                name: _metadataBuilder.GetOrAddString(field.Name),
                signature: _metadataBuilder.GetOrAddBlob(fieldSignature));

        private TypeDefinitionHandle AddTypeDefinition(TypeBuilderImpl type, EntityHandle parent, int methodToken, int fieldToken) =>
            _metadataBuilder.AddTypeDefinition(
                attributes: type.Attributes,
                @namespace: (type.Namespace == null) ? default : _metadataBuilder.GetOrAddString(type.Namespace),
                name: _metadataBuilder.GetOrAddString(type.Name),
                baseType: parent,
                fieldList: MetadataTokens.FieldDefinitionHandle(fieldToken),
                methodList: MetadataTokens.MethodDefinitionHandle(methodToken));

        private MethodDefinitionHandle AddMethodDefinition(MethodBuilderImpl method, BlobBuilder methodSignature, int offset, int parameterToken) =>
            _metadataBuilder.AddMethodDefinition(
                attributes: method.Attributes,
                implAttributes: method.GetMethodImplementationFlags(),
                name: _metadataBuilder.GetOrAddString(method.Name),
                signature: _metadataBuilder.GetOrAddBlob(methodSignature),
                bodyOffset: offset,
                parameterList: MetadataTokens.ParameterHandle(parameterToken));

        private TypeReferenceHandle AddTypeReference(Type type, EntityHandle resolutionScope) =>
            _metadataBuilder.AddTypeReference(
                resolutionScope: resolutionScope,
                @namespace: (type.Namespace == null) ? default : _metadataBuilder.GetOrAddString(type.Namespace),
                name: _metadataBuilder.GetOrAddString(type.Name));

        private MemberReferenceHandle AddMemberReference(string memberName, EntityHandle parent, BlobBuilder signature) =>
            _metadataBuilder.AddMemberReference(
                parent: parent,
                name: _metadataBuilder.GetOrAddString(memberName),
                signature: _metadataBuilder.GetOrAddBlob(signature));

        private MemberReferenceHandle AddConstructorReference(EntityHandle parent, ConstructorInfo method)
        {
            var blob = MetadataSignatureHelper.ConstructorSignatureEncoder(method.GetParameters(), this);
            return _metadataBuilder.AddMemberReference(
                    parent: parent,
                    name: _metadataBuilder.GetOrAddString(method.Name),
                    signature: _metadataBuilder.GetOrAddBlob(blob));
        }

        private void AddMethodImport(MethodDefinitionHandle methodHandle, string name,
            MethodImportAttributes attributes, ModuleReferenceHandle moduleHandle) =>
            _metadataBuilder.AddMethodImport(
                method: methodHandle,
                attributes: attributes,
                name: _metadataBuilder.GetOrAddString(name),
                module: moduleHandle);

        private ModuleReferenceHandle AddModuleReference(string moduleName) =>
            _metadataBuilder.AddModuleReference(moduleName: _metadataBuilder.GetOrAddString(moduleName));

        private void AddFieldLayout(FieldDefinitionHandle fieldHandle, int offset) =>
            _metadataBuilder.AddFieldLayout(field: fieldHandle, offset: offset);

        private void AddMarshalling(EntityHandle parent, BlobBuilder builder) =>
            _metadataBuilder.AddMarshallingDescriptor(parent: parent, descriptor: _metadataBuilder.GetOrAddBlob(builder));

        private ParameterHandle AddParameter(ParameterBuilderImpl parameter) =>
            _metadataBuilder.AddParameter(
                attributes: (ParameterAttributes)parameter.Attributes,
                name: parameter.Name != null ? _metadataBuilder.GetOrAddString(parameter.Name) : default,
                sequenceNumber: parameter.Position);

        private AssemblyReferenceHandle AddAssemblyReference(string? name, Version? version, string? culture,
            byte[]? publicKeyToken, AssemblyNameFlags flags, AssemblyContentType contentType) =>
            _metadataBuilder.AddAssemblyReference(
                name: name == null ? default : _metadataBuilder.GetOrAddString(name),
                version: version ?? new Version(0, 0, 0, 0),
                culture: (culture == null) ? default : _metadataBuilder.GetOrAddString(value: culture),
                publicKeyOrToken: (publicKeyToken == null) ? default : _metadataBuilder.GetOrAddBlob(publicKeyToken), // reference has token, not full public key
                flags: (AssemblyFlags)((int)contentType << 9) | ((flags & AssemblyNameFlags.Retargetable) != 0 ? AssemblyFlags.Retargetable : 0),
                hashValue: default); // .file directive assemblies not supported, no need to handle this value.

        internal EntityHandle GetTypeHandle(Type type)
        {
            if (type is TypeBuilderImpl tb && Equals(tb.Module))
            {
                return tb._handle;
            }

            if (type is EnumBuilderImpl eb && Equals(eb.Module))
            {
                return eb._typeBuilder._handle;
            }

            return GetTypeReferenceOrSpecificationHandle(type);
        }

        internal EntityHandle GetMemberHandle(MemberInfo member)
        {
            if (member is MethodBuilderImpl mb && Equals(mb.Module))
            {
                return mb._handle;
            }

            if (member is FieldBuilderImpl fb && Equals(fb.Module))
            {
                return fb._handle;
            }

            if (member is ConstructorBuilderImpl ctor && Equals(ctor.Module))
            {
                return ctor._methodBuilder._handle;
            }

            return GetMemberReference(member);
        }

        internal TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent,
            Type[]? interfaces, PackingSize packingSize, int typesize, TypeBuilderImpl? enclosingType)
        {
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this, interfaces, packingSize, typesize, enclosingType);
            _typeDefinitions.Add(_type);
            return _type;
        }

        [RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
        public override string Name => "<In Memory Module>";
        public override string ScopeName => _name;
        public override Assembly Assembly => _assemblyBuilder;
        public override Guid ModuleVersionId => _moduleVersionId;
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override int GetFieldMetadataToken(FieldInfo field)
        {
            if (field is FieldBuilderImpl fb && fb._handle != default)
            {
                return MetadataTokens.GetToken(fb._handle);
            }

            return 0;
        }

        public override int GetMethodMetadataToken(ConstructorInfo constructor) => throw new NotImplementedException();

        public override int GetMethodMetadataToken(MethodInfo method)
        {
            if (method is MethodBuilderImpl mb && mb._handle != default)
            {
                return MetadataTokens.GetToken(mb._handle);
            }

            return 0;
        }

        public override int GetStringMetadataToken(string stringConstant) => MetadataTokens.GetToken(_metadataBuilder.GetOrAddUserString(stringConstant));

        public override int GetTypeMetadataToken(Type type) => MetadataTokens.GetToken(GetTypeHandle(type));

        protected override void CreateGlobalFunctionsCore() => throw new NotImplementedException();

        protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType)
        {
            EnumBuilderImpl enumBuilder = new EnumBuilderImpl(name, underlyingType, visibility, this);
            _typeDefinitions.Add(enumBuilder._typeBuilder);
            return enumBuilder;
        }
        protected override MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers) => throw new NotImplementedException();
        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes) => throw new NotImplementedException();
        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet) => throw new NotImplementedException();

        protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize)
        {
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this, interfaces, packingSize, typesize, null);
            _typeDefinitions.Add(_type);
            return _type;
        }

        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }
        public override int GetSignatureMetadataToken(SignatureHelper signature) => throw new NotImplementedException();
    }
}
