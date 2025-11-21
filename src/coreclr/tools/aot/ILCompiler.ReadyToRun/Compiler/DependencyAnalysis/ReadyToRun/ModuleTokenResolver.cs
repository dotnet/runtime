// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;
using System.Diagnostics;

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
            InitializeKnownMethodsAndTypes();
        }

        public ModuleToken GetModuleTokenForType(EcmaType type, bool allowDynamicallyCreatedReference, bool throwIfNotFound = true)
        {
            if (_compilationModuleGroup.VersionsWithType(type))
            {
                return new ModuleToken(type.Module, (mdToken)MetadataTokens.GetToken(type.Handle));
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

        public void AddModuleTokenForMethod(MethodDesc method, ModuleToken token)
        {
            if (token.TokenType == CorTokenType.mdtMethodSpec)
            {
                MethodSpecification methodSpec = token.MetadataReader.GetMethodSpecification((MethodSpecificationHandle)token.Handle);
                DecodeMethodSpecificationSignatureToDiscoverUsedTypeTokens(methodSpec.Signature, token);
                token = new ModuleToken(token.Module, methodSpec.Method);
            }

            if (token.TokenType == CorTokenType.mdtMemberRef)
            {
                MemberReference memberRef = token.MetadataReader.GetMemberReference((MemberReferenceHandle)token.Handle);
                EntityHandle owningTypeHandle = memberRef.Parent;
                TypeDesc owningType = (TypeDesc)token.Module.GetObject(owningTypeHandle, NotFoundBehavior.Throw);
                AddModuleTokenForType(owningType, new ModuleToken(token.Module, owningTypeHandle));
                DecodeMethodSignatureToDiscoverUsedTypeTokens(memberRef.Signature, token);
            }
            if (token.TokenType == CorTokenType.mdtMethodDef)
            {
                MethodDefinition methodDef = token.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)token.Handle);
                TokenResolverProvider rentedProvider = TokenResolverProvider.Rent(this, token.Module);
                DecodeMethodSignatureToDiscoverUsedTypeTokens(methodDef.Signature, token);
            }
        }

        private void DecodeMethodSpecificationSignatureToDiscoverUsedTypeTokens(BlobHandle signatureHandle, ModuleToken token)
        {
            MetadataReader metadataReader = token.MetadataReader;
            TokenResolverProvider rentedProvider = TokenResolverProvider.Rent(this, token.Module);
            SignatureDecoder<DummyTypeInfo, ModuleTokenResolver> sigDecoder = new(rentedProvider, metadataReader, this);

            BlobReader signature = metadataReader.GetBlobReader(signatureHandle);

            SignatureHeader header = signature.ReadSignatureHeader();
            SignatureKind kind = header.Kind;
            if (kind != SignatureKind.MethodSpecification)
            {
                throw new BadImageFormatException();
            }

            int count = signature.ReadCompressedInteger();
            for (int i = 0; i < count; i++)
            {
                sigDecoder.DecodeType(ref signature);
            }
            TokenResolverProvider.ReturnRental(rentedProvider);
        }

        private void DecodeMethodSignatureToDiscoverUsedTypeTokens(BlobHandle signatureHandle, ModuleToken token)
        {
            MetadataReader metadataReader = token.MetadataReader;
            TokenResolverProvider rentedProvider = TokenResolverProvider.Rent(this, token.Module);
            SignatureDecoder<DummyTypeInfo, ModuleTokenResolver> sigDecoder = new (rentedProvider, metadataReader, this);
            BlobReader signature = metadataReader.GetBlobReader(signatureHandle);

            SignatureHeader header = signature.ReadSignatureHeader();
            SignatureKind kind = header.Kind;
            if (kind != SignatureKind.Method && kind != SignatureKind.Property)
            {
                throw new BadImageFormatException();
            }

            int genericParameterCount = 0;
            if (header.IsGeneric)
            {
                genericParameterCount = signature.ReadCompressedInteger();
            }

            int parameterCount = signature.ReadCompressedInteger();
            sigDecoder.DecodeType(ref signature);

            if (parameterCount != 0)
            {
                int parameterIndex;

                for (parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
                {
                    BlobReader sentinelReader = signature;
                    int typeCode = sentinelReader.ReadCompressedInteger();
                    if (typeCode == (int)SignatureTypeCode.Sentinel)
                    {
                        signature = sentinelReader;
                    }
                    sigDecoder.DecodeType(ref signature, allowTypeSpecifications: false);
                }
            }

            TokenResolverProvider.ReturnRental(rentedProvider);
        }

        private void DecodeFieldSignatureToDiscoverUsedTypeTokens(BlobHandle signatureHandle, ModuleToken token)
        {
            MetadataReader metadataReader = token.MetadataReader;
            TokenResolverProvider rentedProvider = TokenResolverProvider.Rent(this, token.Module);
            SignatureDecoder<DummyTypeInfo, ModuleTokenResolver> sigDecoder = new(rentedProvider, metadataReader, this);

            BlobReader signature = metadataReader.GetBlobReader(signatureHandle);

            SignatureHeader header = signature.ReadSignatureHeader();
            SignatureKind kind = header.Kind;
            if (kind != SignatureKind.Field)
            {
                throw new BadImageFormatException();
            }

            sigDecoder.DecodeType(ref signature);
            TokenResolverProvider.ReturnRental(rentedProvider);
        }

        private void AddModuleTokenForFieldReference(TypeDesc owningType, ModuleToken token)
        {
            MemberReference memberRef = token.MetadataReader.GetMemberReference((MemberReferenceHandle)token.Handle);
            EntityHandle owningTypeHandle = memberRef.Parent;
            AddModuleTokenForType(owningType, new ModuleToken(token.Module, owningTypeHandle));
            DecodeFieldSignatureToDiscoverUsedTypeTokens(memberRef.Signature, token);
        }

        // Add TypeSystemEntity -> ModuleToken mapping to a ConcurrentDictionary. Using CompareTo sort the token used, so it will
        // be consistent in all runs of the compiler
        void SetModuleTokenForTypeSystemEntity<T>(ConcurrentDictionary<T, ModuleToken> dictionary, T tse, ModuleToken token)
        {
            // We can only use tokens from the manifest mutable module or from one of the modules that versions
            // with the current compilation. Any other module tokens may change while the code executes
            if (token.Module != _manifestMutableModule && !_compilationModuleGroup.VersionsWithModule((ModuleDesc)token.Module))
            {
                throw new InternalCompilerErrorException("Invalid usage of a token from a module not within the version bubble");
            }

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
                TokenResolverProvider rentedProvider = TokenResolverProvider.Rent(this, token.Module);
                typeSpec.DecodeSignature(rentedProvider, this);
                TokenResolverProvider.ReturnRental(rentedProvider);
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
            int moduleIndex = _moduleIndexLookup(module);
            if (moduleIndex != 0 && !(module is Internal.TypeSystem.Ecma.MutableModule))
            {
                if (!_compilationModuleGroup.VersionsWithModule((ModuleDesc)module))
                {
                    throw new InternalCompilerErrorException("Attempt to use token from a module not within the version bubble");
                }
            }
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

            [ThreadStatic]
            private static TokenResolverProvider _rentalObject;

            public TokenResolverProvider(ModuleTokenResolver resolver, IEcmaModule contextModule)
            {
                _resolver = resolver;
                _contextModule = contextModule;
            }

            public static TokenResolverProvider Rent(ModuleTokenResolver resolver, IEcmaModule contextModule)
            {
                if (_rentalObject != null)
                {
                    TokenResolverProvider result = _rentalObject;
                    _rentalObject = null;
                    result._resolver = resolver;
                    result._contextModule = contextModule;
                    return result;
                }
                return new TokenResolverProvider(resolver, contextModule);
            }

            public static void ReturnRental(TokenResolverProvider rental)
            {
                rental._resolver = null;
                rental._contextModule = null;
                _rentalObject = rental;
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

        private HashSet<MethodDesc> KnownMethods;
        private HashSet<TypeDesc> KnownTypes;

        private void InitializeKnownMethodsAndTypes()
        {
            var knownTypes = new HashSet<TypeDesc>();
            var knownMethods = new HashSet<MethodDesc>();

            var exception = CompilerContext.SystemModule.GetKnownType("System"u8, "Exception"u8);
            knownTypes.Add(exception);

            // AsyncHelpers type and methods
            var asyncHelpers = CompilerContext.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8);
            knownTypes.Add(asyncHelpers);

            knownMethods.Add(asyncHelpers.GetKnownMethod("AsyncCallContinuation"u8, null));
            knownMethods.Add(asyncHelpers.GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8), new TypeDesc[] { exception })));
            knownMethods.Add(asyncHelpers.GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 0, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8), new TypeDesc[] { exception })));
            knownMethods.Add(asyncHelpers.GetKnownMethod("TaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).MakeInstantiatedType(CompilerContext.GetSignatureVariable(0, method: true)), new TypeDesc[] { exception })));
            knownMethods.Add(asyncHelpers.GetKnownMethod("ValueTaskFromException"u8, new MethodSignature(MethodSignatureFlags.Static, 1, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).MakeInstantiatedType(CompilerContext.GetSignatureVariable(0, method: true)), new TypeDesc[] { exception })));
            knownMethods.Add(asyncHelpers.GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8), Array.Empty<TypeDesc>())));
            knownMethods.Add(asyncHelpers.GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 0, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8), Array.Empty<TypeDesc>())));
            knownMethods.Add(asyncHelpers.GetKnownMethod("FinalizeTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8).MakeInstantiatedType(CompilerContext.GetSignatureVariable(0, method: true)), Array.Empty<TypeDesc>())));
            knownMethods.Add(asyncHelpers.GetKnownMethod("FinalizeValueTaskReturningThunk"u8, new MethodSignature(MethodSignatureFlags.Static, 1, CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8).MakeInstantiatedType(CompilerContext.GetSignatureVariable(0, method: true)), Array.Empty<TypeDesc>())));
            knownMethods.Add(asyncHelpers.GetKnownMethod("TransparentAwait"u8, null));
            knownMethods.Add(asyncHelpers.GetKnownMethod("CompletedTask"u8, null));
            knownMethods.Add(asyncHelpers.GetKnownMethod("CompletedTaskResult"u8, null));

            // ExecutionAndSyncBlockStore type and methods
            var executionSyncBlockStore = CompilerContext.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8);
            knownTypes.Add(executionSyncBlockStore);
            knownMethods.Add(executionSyncBlockStore.GetKnownMethod("Push"u8, null));
            knownMethods.Add(executionSyncBlockStore.GetKnownMethod("Pop"u8, null));

            // Task types and methods
            var task = CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task"u8);
            knownTypes.Add(task);
            knownMethods.Add(task.GetKnownMethod("FromResult"u8, null));
            knownMethods.Add(task.GetKnownMethod("get_CompletedTask"u8, null));
            knownMethods.Add(task.GetKnownMethod("get_IsCompleted"u8, null));

            var taskGeneric = CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "Task`1"u8);
            knownTypes.Add(taskGeneric);
            knownMethods.Add(taskGeneric.GetKnownMethod("get_Result"u8, null));

            // ValueTask types and methods
            var valueTask = CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask"u8);
            knownTypes.Add(valueTask);
            knownMethods.Add(valueTask.GetKnownMethod("FromResult"u8, null));
            knownMethods.Add(valueTask.GetKnownMethod("get_CompletedTask"u8, null));
            knownMethods.Add(valueTask.GetKnownMethod("get_IsCompleted"u8, null));
            knownMethods.Add(valueTask.GetKnownMethod("ThrowIfCompletedUnsuccessfully"u8, null));
            knownMethods.Add(valueTask.GetKnownMethod("AsTaskOrNotifier"u8, null));

            var valueTaskGeneric = CompilerContext.SystemModule.GetKnownType("System.Threading.Tasks"u8, "ValueTask`1"u8);
            knownTypes.Add(valueTaskGeneric);
            knownMethods.Add(valueTaskGeneric.GetKnownMethod("get_Result"u8, null));
            knownMethods.Add(valueTaskGeneric.GetKnownMethod("AsTaskOrNotifier"u8, null));

            KnownTypes = knownTypes;
            KnownMethods = knownMethods;

            try
            {
                Debug.Assert(_manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
                _manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = _manifestMutableModule.Context.SystemModule;
                _manifestMutableModule.AddingReferencesToR2RKnownTypesAndMethods = true;
                foreach (var type in KnownTypes)
                {
                    // We can skip types that are already in the version bubble
                    if (_compilationModuleGroup.VersionsWithType(type))
                        continue;

                    EntityHandle? handle = _manifestMutableModule.TryGetEntityHandle(type);
                    if (!handle.HasValue)
                        throw new NotImplementedException($"Entity handle for known type ({type}) not found in manifest module");

                    var token = new ModuleToken(_manifestMutableModule, handle.Value);
                    this.AddModuleTokenForType(type, token);
                    Debug.Assert(!default(ModuleToken).Equals(this.GetModuleTokenForType((EcmaType)type, allowDynamicallyCreatedReference: true, throwIfNotFound: true)));
                }

                foreach (var method in KnownMethods)
                {
                    // We can skip methods that are already in the version bubble
                    if (_compilationModuleGroup.VersionsWithMethodBody((EcmaMethod)method))
                        continue;

                    EntityHandle? handle = _manifestMutableModule.TryGetEntityHandle(method);
                    if (!handle.HasValue)
                        throw new NotImplementedException($"Entity handle for known method ({method}) not found in manifest module");

                    var token = new ModuleToken(_manifestMutableModule, handle.Value);
                    this.AddModuleTokenForMethod(method, token);
                    Debug.Assert(!default(ModuleToken).Equals(this.GetModuleTokenForMethod(method, allowDynamicallyCreatedReference: true, throwIfNotFound: true)));
                }
            }
            finally
            {
                _manifestMutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
                _manifestMutableModule.AddingReferencesToR2RKnownTypesAndMethods = false;
            }
        }

        public bool IsKnownMutableModuleMethod(EcmaMethod method)
        {
            if (KnownMethods is null)
                InitializeKnownMethodsAndTypes();
            return KnownMethods.Contains(method);
        }

        public bool IsKnownMutableModuleType(EcmaType type)
        {
            if (KnownTypes is null)
                InitializeKnownMethodsAndTypes();
            return KnownTypes.Contains(type);
        }
    }
}
