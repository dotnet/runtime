// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.JitInterface;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis
{
    internal static class PreinitializationExtensions
    {
        extension(NodeFactory factory)
        {
            public ISymbolNode TypeNonGCStaticsSymbol(MetadataType type)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateTypeNonGCStaticsImport(type);

            public ISymbolNode TypeGCStaticsSymbol(MetadataType type)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateTypeGCStaticsImport(type);

            public ISymbolNode TypeClassInitFlagSymbol(MetadataType type)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateTypeClassInitFlagsImport(type);

            public ISymbolNode ConstructedTypeSymbol(TypeDesc type)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateConstructedTypeImport(type);

            public ISymbolNode SerializedStringObject(string data)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateSerializedStringImport(data);

            public ISymbolNode SerializedFrozenObject(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateSerializedFrozenObjectDataNode(owningType, allocationSiteId, data);

            public ISymbolNode SerializedMetadataRuntimeTypeObject(TypeDesc type)
                => factory.ReadyToRunPreinitializationManager.GetOrCreateSerializedRuntimeTypeImport(type);

            public ISymbolNode ExactCallableAddressTakenAddress(MethodDesc method, bool isUnboxingStub = false)
            {
                ModuleToken token = factory.Resolver.GetModuleTokenForMethod(
                    method,
                    allowDynamicallyCreatedReference: true,
                    throwIfNotFound: true);

                MethodWithToken methodWithToken = new MethodWithToken(
                    method,
                    token,
                    constrainedType: null,
                    unboxing: isUnboxingStub,
                    context: null);

                return factory.ReadyToRunPreinitializationManager.GetOrCreateExactCallableAddressImport(
                    methodWithToken,
                    isInstantiatingStub: false);
            }
        }

        extension(MetadataManager metadataManager)
        {
            public void GetDependenciesDueToDelegateCreation(ref CombinedDependencyList dependencies, DependencyAnalysis.NodeFactory factory, TypeDesc delegateType, MethodDesc target)
            {
                // In R2R for preinitialized delegates, codegen dependencies are carried by relocations emitted
                // into the serialized object payload, so no additional conditional dependencies are required.
            }
        }
    }
}
