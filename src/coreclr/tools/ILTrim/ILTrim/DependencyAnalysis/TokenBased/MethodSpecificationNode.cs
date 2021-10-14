// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the MethodSpec metadata table (an instantiated generic method).
    /// </summary>
    public sealed class MethodSpecificationNode : TokenBasedNode
    {
        public MethodSpecificationNode(EcmaModule module, MethodSpecificationHandle handle)
            : base(module, handle)
        {
        }

        private MethodSpecificationHandle Handle => (MethodSpecificationHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MethodSpecification methodSpec = _module.MetadataReader.GetMethodSpecification(Handle);

            DependencyList dependencies = new DependencyList();

            EcmaSignatureAnalyzer.AnalyzeMethodSpecSignature(
                _module,
                _module.MetadataReader.GetBlobReader(methodSpec.Signature),
                factory,
                dependencies);

            dependencies.Add(factory.GetNodeForToken(_module, methodSpec.Method), "Instantiated method");

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            MethodSpecification methodSpec = reader.GetMethodSpecification(Handle);

            var builder = writeContext.MetadataBuilder;

            var signatureBlob = writeContext.GetSharedBlobBuilder();

            EcmaSignatureRewriter.RewriteMethodSpecSignature(
                reader.GetBlobReader(methodSpec.Signature),
                writeContext.TokenMap,
                signatureBlob);

            return builder.AddMethodSpecification(
                writeContext.TokenMap.MapToken(methodSpec.Method),
                builder.GetOrAddBlob(signatureBlob));
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into
            return "MethodSpecification";
        }
    }
}
