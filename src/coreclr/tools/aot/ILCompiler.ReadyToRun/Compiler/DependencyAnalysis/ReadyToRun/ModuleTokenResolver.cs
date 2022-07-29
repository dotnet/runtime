// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class is used to back-resolve typesystem elements from
    /// external version bubbles to references relative to the current
    /// versioning bubble.
    /// </summary>
    public class ModuleTokenResolver
    {
        /// <summary>
        /// Reverse lookup table mapping external types to reference tokens in the input modules. The table
        /// gets lazily initialized as various tokens are resolved in CorInfoImpl.
        /// </summary>
        private readonly ConcurrentDictionary<EcmaType, ModuleToken> _typeToRefTokens = new ConcurrentDictionary<EcmaType, ModuleToken>();

        private readonly ConcurrentDictionary<FieldDesc, ModuleToken> _fieldToRefTokens = new ConcurrentDictionary<FieldDesc, ModuleToken>();

        private readonly CompilationModuleGroup _compilationModuleGroup;

        private Func<IEcmaModule, int> _moduleIndexLookup;

        private MutableModule _manifestMutableModule;

        public CompilerTypeSystemContext CompilerContext { get; }

        public ModuleTokenResolver(CompilationModuleGroup compilationModuleGroup, CompilerTypeSystemContext typeSystemContext)
        {
            _compilationModuleGroup = compilationModuleGroup;
            CompilerContext = typeSystemContext;
        }

        public void SetModuleIndexLookup(Func<IEcmaModule, int> moduleIndexLookup)
        {
            _moduleIndexLookup = moduleIndexLookup;
        }

        public void InitManifestMutableModule(MutableModule mutableModule)
        {
            _manifestMutableModule = mutableModule;
        }

        public ModuleToken GetModuleTokenForType(EcmaType type, bool allowDynamicallyCreatedReference, bool throwIfNotFound = true)
        {
            if (_compilationModuleGroup.VersionsWithType(type))
            {
                return new ModuleToken(type.EcmaModule, (mdToken)MetadataTokens.GetToken(type.Handle));
            }

            ModuleToken token;
            if (_typeToRefTokens.TryGetValue(type, out token))
            {
                return token;
            }

            // If the token was not lazily mapped, search the input compilation set for a type reference token
            if (_compilationModuleGroup.TryGetModuleTokenForExternalType(type, out token))
            {
                return token;
            }

            // If that didn't work, it may be in the manifest module used for version resilient cross module inlining
            if (allowDynamicallyCreatedReference)
            {
                var handle = _manifestMutableModule.TryGetExistingEntityHandle(type);

                if (handle.HasValue)
                {
                    return new ModuleToken(_manifestMutableModule, handle.Value);
                }
            }

            // Reverse lookup failed
            if (throwIfNotFound)
            {
                throw new NotImplementedException(type.ToString());
            }
            else
            {
                return default(ModuleToken);
            }
        }

        public ModuleToken GetModuleTokenForMethod(MethodDesc method, bool allowDynamicallyCreatedReference, bool throwIfNotFound)
        {
            method = method.GetCanonMethodTarget(CanonicalFormKind.Specific);

            if (method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
            {
                if (_compilationModuleGroup.VersionsWithMethodBody(ecmaMethod))
                {
                    return new ModuleToken(ecmaMethod.Module, ecmaMethod.Handle);
                }

                // If that didn't work, it may be in the manifest module used for version resilient cross module inlining
                if (allowDynamicallyCreatedReference)
                {
                    var handle = _manifestMutableModule.TryGetExistingEntityHandle(ecmaMethod);
                    if (handle.HasValue)
                    {
                        return new ModuleToken(_manifestMutableModule, handle.Value);
                    }
                }
            }

            // Reverse lookup failed
            if (throwIfNotFound)
            {
                throw new NotImplementedException(method.ToString());
            }
            else
            {
                return default(ModuleToken);
            }
        }

        public ModuleToken GetModuleTokenForField(FieldDesc field, bool throwIfNotFound = true)
        {
            if (_compilationModuleGroup.VersionsWithType(field.OwningType) && field is EcmaField ecmaField)
            {
                return new ModuleToken(ecmaField.Module, ecmaField.Handle);
            }

            TypeDesc owningCanonType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
            FieldDesc canonField = field;
            if (owningCanonType != field.OwningType)
            {
                canonField = CompilerContext.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)owningCanonType);
            }

            ModuleToken token;
            if (_fieldToRefTokens.TryGetValue(canonField, out token))
            {
                return token;
            }

            if (throwIfNotFound)
            {
                throw new NotImplementedException(field.ToString());
            }
            else
            {
                return default(ModuleToken);
            }
        }

        public void AddModuleTokenForMethod(MethodDesc method, ModuleToken token)
        {
            if (token.TokenType == CorTokenType.mdtMethodSpec)
            {
                MethodSpecification methodSpec = token.MetadataReader.GetMethodSpecification((MethodSpecificationHandle)token.Handle);
                methodSpec.DecodeSignature<DummyTypeInfo, ModuleTokenResolver>(new TokenResolverProvider(this, token.Module), this);
                token = new ModuleToken(token.Module, methodSpec.Method);
            }
            if (token.TokenType == CorTokenType.mdtMemberRef)
            {
                MemberReference memberRef = token.MetadataReader.GetMemberReference((MemberReferenceHandle)token.Handle);
                EntityHandle owningTypeHandle = memberRef.Parent;
                TypeDesc owningType = (TypeDesc)token.Module.GetObject(owningTypeHandle, NotFoundBehavior.Throw);
                AddModuleTokenForType(owningType, new ModuleToken(token.Module, owningTypeHandle));
                memberRef.DecodeMethodSignature<DummyTypeInfo, ModuleTokenResolver>(new TokenResolverProvider(this, token.Module), this);
            }
            if (token.TokenType == CorTokenType.mdtMethodDef)
            {
                MethodDefinition methodDef = token.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)token.Handle);
                methodDef.DecodeSignature<DummyTypeInfo, ModuleTokenResolver>(new TokenResolverProvider(this, token.Module), this);
            }
        }

        private void AddModuleTokenForFieldReference(TypeDesc owningType, ModuleToken token)
        {
            MemberReference memberRef = token.MetadataReader.GetMemberReference((MemberReferenceHandle)token.Handle);
            EntityHandle owningTypeHandle = memberRef.Parent;
            AddModuleTokenForType(owningType, new ModuleToken(token.Module, owningTypeHandle));
            memberRef.DecodeFieldSignature<DummyTypeInfo, ModuleTokenResolver>(new TokenResolverProvider(this, token.Module), this);
        }

        public void AddModuleTokenForField(FieldDesc field, ModuleToken token)
        {
            if (_compilationModuleGroup.VersionsWithType(field.OwningType) && field.OwningType is EcmaType)
            {
                // We don't need to store handles within the current compilation group
                // as we can read them directly from the ECMA objects.
                return;
            }

            TypeDesc owningCanonType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
            FieldDesc canonField = field;
            if (owningCanonType != field.OwningType)
            {
                canonField = CompilerContext.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)owningCanonType);
            }

            SetModuleTokenForTypeSystemEntity(_fieldToRefTokens, canonField, token);

            switch (token.TokenType)
            {
                case CorTokenType.mdtMemberRef:
                    AddModuleTokenForFieldReference(owningCanonType, token);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        // Add TypeSystemEntity -> ModuleToken mapping to a ConcurrentDictionary. Using CompareTo sort the token used, so it will
        // be consistent in all runs of the compiler
        void SetModuleTokenForTypeSystemEntity<T>(ConcurrentDictionary<T, ModuleToken> dictionary, T tse, ModuleToken token)
        {
            if (!dictionary.TryAdd(tse, token))
            {
                ModuleToken oldToken;
                do
                {
                    // We will reach here, if the field already has a token
                    if (!dictionary.TryGetValue(tse, out oldToken))
                        throw new InternalCompilerErrorException("TypeSystemEntity both present and not present in emission dictionary.");

                    if (oldToken.CompareTo(token) <= 0)
                        break;
                } while (dictionary.TryUpdate(tse, token, oldToken));
            }
        }

        public void AddModuleTokenForType(TypeDesc type, ModuleToken token)
        {
            bool specialTypeFound = false;
            // Collect underlying type tokens for type specifications
            if (token.TokenType == CorTokenType.mdtTypeSpec)
            {
                TypeSpecification typeSpec = token.MetadataReader.GetTypeSpecification((TypeSpecificationHandle)token.Handle);
                typeSpec.DecodeSignature(new TokenResolverProvider(this, token.Module), this);
                specialTypeFound = true;
            }

            if (_compilationModuleGroup.VersionsWithType(type))
            {
                // We don't need to store handles within the current compilation group
                // as we can read them directly from the ECMA objects.
                return;
            }

            if (type is EcmaType ecmaType)
            {
                // Don't store typespec tokens where a generic parameter resolves to the type in question
                if (token.TokenType == CorTokenType.mdtTypeDef || token.TokenType == CorTokenType.mdtTypeRef)
                {
                    SetModuleTokenForTypeSystemEntity(_typeToRefTokens, ecmaType, token);
                }
            }
            else if (!specialTypeFound)
            {
                throw new NotImplementedException(type.ToString());
            }
        }

        public int GetModuleIndex(IEcmaModule module)
        {
            return _moduleIndexLookup(module);
        }

        /// <summary>
        /// As of 8/20/2018, recursive propagation of type information through
        /// the composite signature tree is not needed for anything. We're adding
        /// a dummy class to clearly indicate what aspects of the resolver need
        /// changing if the propagation becomes necessary.
        /// </summary>
        private class DummyTypeInfo
        {
            public static DummyTypeInfo Instance = new DummyTypeInfo(); 
        }

        private class TokenResolverProvider : ISignatureTypeProvider<DummyTypeInfo, ModuleTokenResolver>
        {
            ModuleTokenResolver _resolver;

            IEcmaModule _contextModule;

            public TokenResolverProvider(ModuleTokenResolver resolver, IEcmaModule contextModule)
            {
                _resolver = resolver;
                _contextModule = contextModule;
            }

            public DummyTypeInfo GetArrayType(DummyTypeInfo elementType, ArrayShape shape)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetByReferenceType(DummyTypeInfo elementType)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetFunctionPointerType(MethodSignature<DummyTypeInfo> signature)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetGenericInstantiation(DummyTypeInfo genericType, ImmutableArray<DummyTypeInfo> typeArguments)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetGenericMethodParameter(ModuleTokenResolver genericContext, int index)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetGenericTypeParameter(ModuleTokenResolver genericContext, int index)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetModifiedType(DummyTypeInfo modifier, DummyTypeInfo unmodifiedType, bool isRequired)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetPinnedType(DummyTypeInfo elementType)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetPointerType(DummyTypeInfo elementType)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetSZArrayType(DummyTypeInfo elementType)
            {
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                // Type definition tokens outside of the versioning bubble are useless.
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                _resolver.AddModuleTokenForType((TypeDesc)_contextModule.GetObject(handle), new ModuleToken(_contextModule, handle));
                return DummyTypeInfo.Instance;
            }

            public DummyTypeInfo GetTypeFromSpecification(MetadataReader reader, ModuleTokenResolver genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                TypeSpecification typeSpec = reader.GetTypeSpecification(handle);
                typeSpec.DecodeSignature(this, genericContext);
                return DummyTypeInfo.Instance;
            }
        }
    }
}

