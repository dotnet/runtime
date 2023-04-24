// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;

using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;

namespace ILCompiler
{
    /// <summary>
    /// This class is responsible for managing stub methods for interop
    /// </summary>
    public abstract class CompilerGeneratedInteropStubManager : InteropStubManager
    {
        private readonly PInvokeILEmitterConfiguration _pInvokeILEmitterConfiguration;
        internal readonly InteropStateManager _interopStateManager;

        public CompilerGeneratedInteropStubManager(InteropStateManager interopStateManager, PInvokeILEmitterConfiguration pInvokeILEmitterConfiguration)
        {
            _interopStateManager = interopStateManager;
            _pInvokeILEmitterConfiguration = pInvokeILEmitterConfiguration;
        }

        public sealed override PInvokeILProvider CreatePInvokeILProvider()
        {
            return new PInvokeILProvider(_pInvokeILEmitterConfiguration, _interopStateManager);
        }

        public sealed override void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            var delegateMapNode = new DelegateMarshallingStubMapNode(commonFixupsTableNode, _interopStateManager);
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.DelegateMarshallingStubMap), delegateMapNode, delegateMapNode, delegateMapNode.EndSymbol);

            var structMapNode = new StructMarshallingStubMapNode(commonFixupsTableNode, _interopStateManager);
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.StructMarshallingStubMap), structMapNode, structMapNode, structMapNode.EndSymbol);
        }
    }
}
