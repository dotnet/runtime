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
        private readonly Dictionary<Assembly, AssemblyReferenceHandle> _assemblyReferences = new();
        private readonly Dictionary<Type, TypeReferenceHandle> _typeReferences = new();
        private readonly List<TypeBuilderImpl> _typeDefinitions = new();
        private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorReferences = new();
        private Dictionary<string, ModuleReferenceHandle>? _moduleReferences;
        private List<CustomAttributeWrapper>? _customAttributes;
        private int _nextTypeDefRowId = 1;
        private int _nextMethodDefRowId = 1;
        private int _nextFieldDefRowId = 1;
        private int _nextParameterRowId = 1;
        private bool _coreTypesFullyPopulated;
        private Type?[]? _coreTypes;
        private static readonly Type[] s_coreTypes = { typeof(void), typeof(object), typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int),
                                                       typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(nint), typeof(nuint), typeof(TypedReference) };

        internal ModuleBuilderImpl(string name, Assembly coreAssembly, MetadataBuilder builder)
        {
            _coreAssembly = coreAssembly;
            _name = name;
            _metadataBuilder = builder;
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

        internal void AppendMetadata()
        {
            // Add module metadata
            ModuleDefinitionHandle moduleHandle = _metadataBuilder.AddModule(
                generation: 0,
                moduleName: _metadataBuilder.GetOrAddString(_name),
                mvid: _metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
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

            // All generic parameters for all types and methods should be written in specific order
            List<GenericTypeParameterBuilderImpl> genericParams = new();
            // Add each type definition to metadata table.
            foreach (TypeBuilderImpl typeBuilder in _typeDefinitions)
            {
                EntityHandle parent = default;
                if (typeBuilder.BaseType is not null)
                {
                    parent = GetTypeHandle(typeBuilder.BaseType);
                }

                TypeDefinitionHandle typeHandle = AddTypeDefinition(typeBuilder, parent, _nextMethodDefRowId, _nextFieldDefRowId);
                Debug.Assert(typeBuilder._handle.Equals(typeHandle));

                if (typeBuilder.IsGenericType)
                {
                    foreach (GenericTypeParameterBuilderImpl param in typeBuilder.GenericTypeParameters)
                    {
                        genericParams.Add(param);
                    }
                }

                if ((typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
                {
                    _metadataBuilder.AddTypeLayout(typeHandle, (ushort)typeBuilder.PackingSize, (uint)typeBuilder.Size);
                }

                if (typeBuilder._interfaces != null)
                {
                    foreach (Type iface in typeBuilder._interfaces)
                    {
                        _metadataBuilder.AddInterfaceImplementation(typeHandle, GetTypeHandle(iface));
                        // TODO: need to add interface mapping between interface method and implemented method
                    }
                }

                if (typeBuilder.DeclaringType != null)
                {
                    _metadataBuilder.AddNestedType(typeHandle, (TypeDefinitionHandle)GetTypeHandle(typeBuilder.DeclaringType));
                }

                WriteCustomAttributes(typeBuilder._customAttributes, typeHandle);
                WriteMethods(typeBuilder, genericParams);
                WriteFields(typeBuilder);
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

        private void WriteMethods(TypeBuilderImpl typeBuilder, List<GenericTypeParameterBuilderImpl> genericParams)
        {
            foreach (MethodBuilderImpl method in typeBuilder._methodDefinitions)
            {
                MethodDefinitionHandle methodHandle = AddMethodDefinition(method, method.GetMethodSignatureBlob(), _nextParameterRowId);
                WriteCustomAttributes(method._customAttributes, methodHandle);
                _nextMethodDefRowId++;

                if (method.IsGenericMethodDefinition)
                {
                    Type[] gParams = method.GetGenericArguments();
                    for (int i = 0; i < gParams.Length; i++)
                    {
                        GenericTypeParameterBuilderImpl param = (GenericTypeParameterBuilderImpl)gParams[i];
                        param._parentHandle = methodHandle;
                        genericParams.Add(param);
                    }
                }

                if (method._parameters != null)
                {
                    foreach (ParameterBuilderImpl parameter in method._parameters)
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
                    AddMethodImport(methodHandle, method._dllImportData.EntryPoint ?? method.Name,
                        method._dllImportData.Flags, GetModuleReference(method._dllImportData.ModuleName));
                }
            }
        }

        private void WriteFields(TypeBuilderImpl typeBuilder)
        {
            foreach (FieldBuilderImpl field in typeBuilder._fieldDefinitions)
            {
                FieldDefinitionHandle fieldHandle = AddFieldDefinition(field, MetadataSignatureHelper.FieldSignatureEncoder(field.FieldType, this));
                WriteCustomAttributes(field._customAttributes, fieldHandle);
                _nextFieldDefRowId++;

                if (field._offset > 0 && (typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
                {
                    AddFieldLayout(fieldHandle, field._offset);
                }

                if (field._marshallingData != null)
                {
                    AddMarshalling(fieldHandle, field._marshallingData.SerializeMarshallingData());
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
                TypeReferenceHandle parentHandle = GetTypeReference(constructorInfo.DeclaringType!);
                constructorHandle = AddConstructorReference(parentHandle, constructorInfo);
                _ctorReferences.Add(constructorInfo, constructorHandle);
            }

            return constructorHandle;
        }

        private TypeReferenceHandle GetTypeReference(Type type)
        {
            if (!_typeReferences.TryGetValue(type, out var typeHandle))
            {
                typeHandle = AddTypeReference(type, GetAssemblyReference(type.Assembly));
                _typeReferences.Add(type, typeHandle);
            }

            return typeHandle;
        }

        private AssemblyReferenceHandle GetAssemblyReference(Assembly assembly)
        {
            if (!_assemblyReferences.TryGetValue(assembly, out var handle))
            {
                AssemblyName aName = assembly.GetName();
                handle = AddAssemblyReference(aName.Name!, aName.Version, aName.CultureName, aName.GetPublicKeyToken(), aName.Flags, aName.ContentType);
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

        private void AddDefaultValue(ParameterHandle parameterHandle, object? defaultValue) =>
            _metadataBuilder.AddConstant(parent: parameterHandle, value: defaultValue);

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

        private MethodDefinitionHandle AddMethodDefinition(MethodBuilderImpl method, BlobBuilder methodSignature, int parameterToken) =>
            _metadataBuilder.AddMethodDefinition(
                attributes: method.Attributes,
                implAttributes: method.GetMethodImplementationFlags(),
                name: _metadataBuilder.GetOrAddString(method.Name),
                signature: _metadataBuilder.GetOrAddBlob(methodSignature),
                bodyOffset: -1, // No body supported yet
                parameterList: MetadataTokens.ParameterHandle(parameterToken));

        private TypeReferenceHandle AddTypeReference(Type type, AssemblyReferenceHandle parent) =>
            _metadataBuilder.AddTypeReference(
                resolutionScope: parent,
                @namespace: (type.Namespace == null) ? default : _metadataBuilder.GetOrAddString(type.Namespace),
                name: _metadataBuilder.GetOrAddString(type.Name));

        private MemberReferenceHandle AddConstructorReference(TypeReferenceHandle parent, ConstructorInfo method)
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

        private AssemblyReferenceHandle AddAssemblyReference(string name, Version? version, string? culture,
            byte[]? publicKeyToken, AssemblyNameFlags flags, AssemblyContentType contentType) =>
            _metadataBuilder.AddAssemblyReference(
                name: _metadataBuilder.GetOrAddString(name),
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

            return GetTypeReference(type);
        }

        internal TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent,
            Type[]? interfaces, PackingSize packingSize, int typesize, TypeBuilderImpl? enclosingType)
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this, typeHandle, interfaces, packingSize, typesize, enclosingType);
            _typeDefinitions.Add(_type);
            return _type;
        }

        [RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
        public override string Name => "<In Memory Module>";
        public override string ScopeName => _name;
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override int GetFieldMetadataToken(FieldInfo field) => throw new NotImplementedException();
        public override int GetMethodMetadataToken(ConstructorInfo constructor) => throw new NotImplementedException();
        public override int GetMethodMetadataToken(MethodInfo method) => throw new NotImplementedException();
        public override int GetStringMetadataToken(string stringConstant) => throw new NotImplementedException();
        public override int GetTypeMetadataToken(Type type) => throw new NotImplementedException();
        protected override void CreateGlobalFunctionsCore() => throw new NotImplementedException();
        protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType) => throw new NotImplementedException();
        protected override MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers) => throw new NotImplementedException();
        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes) => throw new NotImplementedException();
        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet) => throw new NotImplementedException();
        protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize)
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this, typeHandle, interfaces, packingSize, typesize, null);
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
