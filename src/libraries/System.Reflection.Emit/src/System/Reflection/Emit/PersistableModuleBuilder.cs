// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using static System.Reflection.Emit.Experiment.EntityWrappers;

namespace System.Reflection.Emit.Experiment
{
    public sealed class PersistableModuleBuilder : ModuleBuilder
    {
        public override string ScopeName { get; }
        private readonly PersistableAssemblyBuilder _assemblyBuilder;

        #region Internal Data Members

        internal Dictionary<Assembly, AssemblyReferenceHandle> _assemblyRefStore = new Dictionary<Assembly, AssemblyReferenceHandle>();
        internal Dictionary<Type, TypeReferenceHandle> _typeRefStore = new Dictionary<Type, TypeReferenceHandle>();
        internal Dictionary<ConstructorInfo, MemberReferenceHandle> _constructorRefStore = new Dictionary<ConstructorInfo, MemberReferenceHandle>();
        internal List<PersistableTypeBuilder> _typeDefStore = new List<PersistableTypeBuilder>();
        internal List<CustomAttributeWrapper> _customAttributes = new();
        internal int _nextMethodDefRowId = 1;
        internal const string ManifestModuleName = "RefEmit_InMemoryManifestModule";

        #endregion


        internal PersistableModuleBuilder(string name, PersistableAssemblyBuilder assembly)
        {
            _assemblyBuilder = assembly;
            ScopeName = name;
        }

        internal void AppendMetadata(MetadataBuilder metadata, AssemblyDefinitionHandle assemlbyHandle)
        {
            // Add module metadata
            ModuleDefinitionHandle moduleHandle = metadata.AddModule(
                generation: 0,
                metadata.GetOrAddString(ScopeName),
                metadata.GetOrAddGuid(Guid.NewGuid()),
                default,
                default);

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                default,
                default,
                metadata.GetOrAddString("<Module>"),
                baseType: default,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // Add Assembly attributes
            foreach (CustomAttributeWrapper customAttribute in _assemblyBuilder._customAttributes)
            {
                metadata.AddCustomAttribute(assemlbyHandle, GetConstructorHandle(metadata, customAttribute.constructorInfo),
                    metadata.GetOrAddBlob(customAttribute.binaryAttribute));
            }

            // Add Module attributes
            foreach (CustomAttributeWrapper customAttribute in this._customAttributes)
            {
                metadata.AddCustomAttribute(moduleHandle, GetConstructorHandle(metadata, customAttribute.constructorInfo),
                    metadata.GetOrAddBlob(customAttribute.binaryAttribute));
            }

            // Add each type definition to metadata table.
            foreach (PersistableTypeBuilder typeBuilder in _typeDefStore)
            {
                TypeReferenceHandle parent = default;
                if (typeBuilder.BaseType is not null)
                {
                    parent = GetTypeReference(metadata, typeBuilder.BaseType);
                }

                TypeDefinitionHandle typeDefintionHandle = MetadataHelper.AddTypeDef(metadata, typeBuilder, parent, _nextMethodDefRowId);

                // Add each method definition to metadata table.
                foreach (PersistableMethodBuilder method in typeBuilder._methodDefStore)
                {
                    MetadataHelper.AddMethodDefintion(metadata, method);
                    _nextMethodDefRowId++;
                }

                foreach (FieldInfo field in typeBuilder._fieldDefStore)
                {
                    MetadataHelper.AddFieldDefintion(metadata, field);
                }

                // Add each custom attribute to metadata table.
                foreach (CustomAttributeWrapper customAttribute in typeBuilder._customAttributes)
                {
                    metadata.AddCustomAttribute(typeDefintionHandle, GetConstructorHandle(metadata, customAttribute.constructorInfo),
                        metadata.GetOrAddBlob(customAttribute.binaryAttribute));
                }
            }
        }

        private MemberReferenceHandle GetConstructorHandle(MetadataBuilder metadata, ConstructorInfo constructorInfo)
        {
            if (_constructorRefStore.TryGetValue(constructorInfo, out var constructorHandle))
            {
                return constructorHandle;
            }

            TypeReferenceHandle parentHandle = GetTypeReference(metadata, constructorInfo.DeclaringType!);

            constructorHandle = MetadataHelper.AddConstructorReference(metadata, parentHandle, constructorInfo);
            _constructorRefStore.Add(constructorInfo, constructorHandle);
            return constructorHandle;
        }

        private TypeReferenceHandle GetTypeReference(MetadataBuilder metadata, Type type)
        {
            if (!_typeRefStore.TryGetValue(type, out var parentHandle))
            {
                parentHandle = MetadataHelper.AddTypeReference(metadata, type,
                    GetAssemblyReference(type.Assembly, metadata));
            }

            return parentHandle;
        }

        private AssemblyReferenceHandle GetAssemblyReference(Assembly assembly, MetadataBuilder metadata)
        {
            if (_assemblyRefStore.TryGetValue(assembly, out var handle))
            {
                return handle;
            }

            return MetadataHelper.AddAssemblyReference(assembly, metadata);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

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
            PersistableTypeBuilder _type = new PersistableTypeBuilder(name, attr, parent, this);
            _typeDefStore.Add(_type);
            return _type;
        }
        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(nameof(con));
            ArgumentNullException.ThrowIfNull(nameof(binaryAttribute)); // This is incorrect

            CustomAttributeWrapper customAttribute = new CustomAttributeWrapper(con, binaryAttribute);
            _customAttributes.Add(customAttribute);
        }
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder)
        {
            SetCustomAttributeCore(customBuilder.Constructor, customBuilder.Blob);
        }
        public override int GetSignatureMetadataToken(SignatureHelper signature) => throw new NotImplementedException();
    }
}
