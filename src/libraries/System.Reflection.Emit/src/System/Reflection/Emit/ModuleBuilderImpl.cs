// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        private Type?[]? _coreTypes;
        private readonly Dictionary<Assembly, AssemblyReferenceHandle> _assemblyReferences = new();
        private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _memberReferences = new();
        private readonly Dictionary<Type, TypeReferenceHandle> _typeReferences = new();
        private Dictionary<string, ModuleReferenceHandle>? _moduleReferences;
        private readonly List<TypeBuilderImpl> _typeDefinitions = new();
        private readonly List<CustomAttributeWrapper> _customAttributes = new();
        private int _nextMethodDefRowId = 1;
        private int _nextFieldDefRowId = 1;
        private bool _coreTypesFullPopulated;
        private static readonly Type[] s_coreTypes = { typeof(void), typeof(object), typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int),
                                                        typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(nint), typeof(nuint) };

        internal ModuleBuilderImpl(string name, Assembly coreAssembly)
        {
            _coreAssembly = coreAssembly;
            _name = name;
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
                    _coreTypesFullPopulated = true;
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
                    _coreTypesFullPopulated = true;
                }
                else
                {
                    _coreTypes = new Type[s_coreTypes.Length];
                }
            }

            if (!_coreTypesFullPopulated)
            {
                for (int i = 0; i < _coreTypes.Length; i++)
                {
                    if (_coreTypes[i] == null)
                    {
                        _coreTypes[i] = _coreAssembly.GetType(s_coreTypes[i].FullName!, throwOnError: false)!;
                    }
                }
                _coreTypesFullPopulated = true;
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

        internal void AppendMetadata(MetadataBuilder metadata, AssemblyDefinitionHandle assemblyHandle, List<CustomAttributeWrapper> assemblyAttributes)
        {
            // Add module metadata
            ModuleDefinitionHandle moduleHandle = metadata.AddModule(
                generation: 0,
                moduleName: metadata.GetOrAddString(_name),
                mvid: metadata.GetOrAddGuid(Guid.NewGuid()),
                encId: default,
                encBaseId: default);

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                attributes: default,
                @namespace: default,
                name: metadata.GetOrAddString("<Module>"),
                baseType: default,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            WriteCustomAttributes(metadata, assemblyAttributes, assemblyHandle);
            WriteCustomAttributes(metadata, _customAttributes, moduleHandle);

            // Add each type definition to metadata table.
            foreach (TypeBuilderImpl typeBuilder in _typeDefinitions)
            {
                TypeReferenceHandle parent = default;
                if (typeBuilder.BaseType is not null)
                {
                    // TODO: need to handle the case when the base is from same assembly
                    parent = GetTypeReference(metadata, typeBuilder.BaseType);
                }

                TypeDefinitionHandle typeDefinitionHandle = MetadataHelper.AddTypeDefinition(metadata, typeBuilder, parent, _nextMethodDefRowId, _nextFieldDefRowId);
                WriteCustomAttributes(metadata, typeBuilder._customAttributes, typeDefinitionHandle);

                foreach (MethodBuilderImpl method in typeBuilder._methodDefinitions)
                {
                    MethodDefinitionHandle methodHandle = MetadataHelper.AddMethodDefinition(metadata, method, method.GetMethodSignatureBlob());
                    WriteCustomAttributes(metadata, method._customAttributes, methodHandle);
                    _nextMethodDefRowId++;

                    if (method._dllImportData != null)
                    {
                        MetadataHelper.AddMethodImport(metadata, methodHandle, method._dllImportData.EntryPoint ?? method.Name,
                            method._dllImportData.Flags, GetModuleReference(metadata, method._dllImportData.ModuleName));
                    }
                }

                foreach (FieldBuilderImpl field in typeBuilder._fieldDefinitions)
                {
                    FieldDefinitionHandle fieldHandle = MetadataHelper.AddFieldDefinition(metadata, field, MetadataSignatureHelper.FieldSignatureEncoder(field.FieldType, this));
                    WriteCustomAttributes(metadata, field._customAttributes, fieldHandle);
                    _nextFieldDefRowId++;

                    if (field._offset > 0 && (typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
                    {
                        MetadataHelper.AddFieldLayout(metadata, fieldHandle, field._offset);
                    }
                }
            }
        }

        private ModuleReferenceHandle GetModuleReference(MetadataBuilder metadata, string moduleName)
        {
            _moduleReferences ??= new();

            if (!_moduleReferences.TryGetValue(moduleName, out var handle))
            {
                handle = MetadataHelper.AddModuleReference(metadata, moduleName);
                _moduleReferences.Add(moduleName, handle);
            }

            return handle;
        }

        private void WriteCustomAttributes(MetadataBuilder metadata, List<CustomAttributeWrapper> customAttributes, EntityHandle parent)
        {
            foreach (CustomAttributeWrapper customAttribute in customAttributes)
            {
                metadata.AddCustomAttribute(parent, GetConstructorHandle(metadata, customAttribute.constructorInfo),
                    metadata.GetOrAddBlob(customAttribute.binaryAttribute));
            }
        }

        private MemberReferenceHandle GetConstructorHandle(MetadataBuilder metadata, ConstructorInfo constructorInfo)
        {
            if (!_memberReferences.TryGetValue(constructorInfo, out var constructorHandle))
            {
                TypeReferenceHandle parentHandle = GetTypeReference(metadata, constructorInfo.DeclaringType!);
                constructorHandle = MetadataHelper.AddConstructorReference(this, metadata, parentHandle, constructorInfo);
                _memberReferences.Add(constructorInfo, constructorHandle);
            }

            return constructorHandle;
        }

        private TypeReferenceHandle GetTypeReference(MetadataBuilder metadata, Type type)
        {
            if (!_typeReferences.TryGetValue(type, out var parentHandle))
            {
                parentHandle = MetadataHelper.AddTypeReference(metadata, type, GetAssemblyReference(type.Assembly, metadata));
                _typeReferences.Add(type, parentHandle);
            }

            return parentHandle;
        }

        private AssemblyReferenceHandle GetAssemblyReference(Assembly assembly, MetadataBuilder metadata)
        {
            if (!_assemblyReferences.TryGetValue(assembly, out var handle))
            {
                handle = MetadataHelper.AddAssemblyReference(assembly, metadata);
                _assemblyReferences.Add(assembly, handle);
            }

            return handle;
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
        protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize)
        {
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this, packingSize, typesize);
            _typeDefinitions.Add(_type);
            return _type;
        }
        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute) =>
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        public override int GetSignatureMetadataToken(SignatureHelper signature) => throw new NotImplementedException();
    }
}
