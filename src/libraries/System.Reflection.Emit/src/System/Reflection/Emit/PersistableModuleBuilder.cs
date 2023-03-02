// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using static System.Reflection.Metadata.Experiment.EntityWrappers;

namespace System.Reflection.Metadata.Experiment
{
    public sealed class PersistableModuleBuilder : ModuleBuilder
    {
        internal List<AssemblyReferenceWrapper> _assemblyRefStore = new List<AssemblyReferenceWrapper>();
        internal int _nextAssemblyRefRowId = 1;

        internal List<TypeReferenceWrapper> _typeRefStore = new List<TypeReferenceWrapper>();
        internal int _nextTypeRefRowId = 1;

        internal List<MethodReferenceWrapper> _methodRefStore = new List<MethodReferenceWrapper>();
        internal int _nextMethodRefRowId = 1;


        internal List<PersistableTypeBuilder> _typeDefStore = new List<PersistableTypeBuilder>();
        internal int _nextMethodDefRowId = 1;
        public override string ScopeName { get; }


        #region Internal Data Members

        private readonly PersistableAssemblyBuilder _assemblyBuilder;

        internal const string ManifestModuleName = "RefEmit_InMemoryManifestModule";

        #endregion


        internal PersistableModuleBuilder(string name, PersistableAssemblyBuilder assembly)
        {
            _assemblyBuilder = assembly;
            ScopeName = name;
        }

        internal void AppendMetadata(MetadataBuilder metadata)
        {
            // Add module metadata
            metadata.AddModule(
                generation: 0,
                metadata.GetOrAddString(ScopeName),
                metadata.GetOrAddGuid(Guid.NewGuid()),
                default(GuidHandle),
                default(GuidHandle));

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // Add each assembly reference to metadata table.
            /*foreach (var assemblyRef in _assemblyRefStore)
            {
                MetadataHelper.AddAssemblyReference(assemblyRef.assembly, metadata);
            }*/

            // Add each type reference to metadata table.
            /*foreach (var typeReference in _typeRefStore)
            {
                AssemblyReferenceHandle parent = MetadataTokens.AssemblyReferenceHandle(typeReference.parentToken);
                MetadataHelper.AddTypeReference(metadata, typeReference.type, parent);
            }*/

            // Add each method reference to metadata table.
            /*foreach (var methodRef in _methodRefStore)
            {
                TypeReferenceHandle parent = MetadataTokens.TypeReferenceHandle(methodRef.parentToken);
                MetadataHelper.AddConstructorReference(metadata, parent, methodRef.method);
            }*/

            // Add each type definition to metadata table.
            foreach (PersistableTypeBuilder typeBuilder in _typeDefStore)
            {
                TypeDefinitionHandle typeDefintionHandle = MetadataHelper.AddTypeDef(typeBuilder, metadata, _nextMethodDefRowId);

                // Add each method definition to metadata table.
                foreach (PersistableMethodBuilder method in typeBuilder._methodDefStore)
                {
                    //if (method.)
                    //Debugger.Launch();
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
                    MemberReferenceHandle constructorHandle = MetadataTokens.MemberReferenceHandle(customAttribute.conToken);
                    metadata.AddCustomAttribute(typeDefintionHandle, constructorHandle, metadata.GetOrAddBlob(customAttribute.binaryAttribute));
                }
            }
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
            PersistableTypeBuilder _type = new PersistableTypeBuilder(name, this, attr);
            _typeDefStore.Add(_type);
            return _type;
        }
        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        public override int GetSignatureMetadataToken(SignatureHelper signature) => throw new NotImplementedException();
    }
}
