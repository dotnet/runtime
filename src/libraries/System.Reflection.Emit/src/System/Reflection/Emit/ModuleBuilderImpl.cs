﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly AssemblyBuilderImpl _assemblyBuilder;
        private readonly string _name;

        #region Internal Data Members

        internal Dictionary<Assembly, AssemblyReferenceHandle> _assemblyRefStore = new Dictionary<Assembly, AssemblyReferenceHandle>();
        internal Dictionary<Type, TypeReferenceHandle> _typeRefStore = new Dictionary<Type, TypeReferenceHandle>();
        internal List<TypeBuilderImpl> _typeDefStore = new List<TypeBuilderImpl>();
        internal int _nextMethodDefRowId = 1;
        internal int _nextFieldDefRowId = 1;

        #endregion

        internal ModuleBuilderImpl(string name, AssemblyBuilderImpl assembly)
        {
            _assemblyBuilder = assembly;
            _name = name;
        }

        internal void AppendMetadata(MetadataBuilder metadata)
        {
            // Add module metadata
            metadata.AddModule(
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

            // Add each type definition to metadata table.
            foreach (TypeBuilderImpl typeBuilder in _typeDefStore)
            {
                TypeReferenceHandle parent = default;
                if (typeBuilder.BaseType is not null)
                {
                    // TODO: need to handle the case when the base is from same assembly
                    parent = GetTypeReference(metadata, typeBuilder.BaseType);
                }

                TypeDefinitionHandle typeDefinitionHandle = MetadataHelper.AddTypeDefinition(metadata, typeBuilder, parent, _nextMethodDefRowId, _nextFieldDefRowId);

                // Add each method definition to metadata table.
                foreach (MethodBuilderImpl method in typeBuilder._methodDefStore)
                {
                    MetadataHelper.AddMethodDefinition(metadata, method, method.GetMethodSignatureBlob());
                    _nextMethodDefRowId++;
                }

                foreach (FieldBuilderImpl field in typeBuilder._fieldDefStore)
                {
                    MetadataHelper.AddFieldDefinition(metadata, field);
                    _nextFieldDefRowId++;
                }
            }
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
        [RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
        public override string Name => _name;
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
            TypeBuilderImpl _type = new TypeBuilderImpl(name, attr, parent, this);
            _typeDefStore.Add(_type);
            return _type;
        }
        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotSupportedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotSupportedException();
        public override int GetSignatureMetadataToken(SignatureHelper signature) => throw new NotImplementedException();
    }
}
