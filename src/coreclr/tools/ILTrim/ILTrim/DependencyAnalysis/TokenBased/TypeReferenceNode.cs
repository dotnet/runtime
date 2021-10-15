// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Type Reference metadata table.
    /// </summary>
    public sealed class TypeReferenceNode : TokenBasedNode
    {
        public TypeReferenceNode(EcmaModule module, TypeReferenceHandle handle)
            : base(module, handle)
        {
        }

        private TypeReferenceHandle Handle => (TypeReferenceHandle)_handle;

        TokenWriterNode GetResolutionScopeNode(NodeFactory factory)
        {
            TypeReference typeRef = _module.MetadataReader.GetTypeReference(Handle);

            if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                // Resolve to an EcmaType to go through any forwarders.
                var ecmaType = (EcmaType)_module.GetObject(Handle);
                EcmaAssembly referencedAssembly = (EcmaAssembly)ecmaType.EcmaModule;
                return factory.AssemblyReference(_module, referencedAssembly);
            }
            else
            {
                return factory.GetNodeForToken(_module, typeRef.ResolutionScope);
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            yield return new(GetResolutionScopeNode(factory), "Resolution Scope of a type reference");

            var typeDescObject = _module.GetObject(Handle);
            if (typeDescObject is EcmaType typeDef && factory.IsModuleTrimmed(typeDef.EcmaModule))
            {
                yield return new(factory.GetNodeForToken(typeDef.EcmaModule, typeDef.Handle), "Target of a type reference");
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            TypeReference typeRef = reader.GetTypeReference(Handle);

            var builder = writeContext.MetadataBuilder;
            TokenWriterNode resolutionScopeNode = GetResolutionScopeNode(writeContext.Factory);
            EntityHandle targetResolutionScopeToken;
            if (resolutionScopeNode is AssemblyReferenceNode assemblyRefNode)
            {
                Debug.Assert(assemblyRefNode.TargetToken.HasValue);
                targetResolutionScopeToken = (EntityHandle)assemblyRefNode.TargetToken.Value;
            }
            else
            {
                targetResolutionScopeToken = writeContext.TokenMap.MapToken(typeRef.ResolutionScope);
            }

            return builder.AddTypeReference(targetResolutionScopeToken,
                builder.GetOrAddString(reader.GetString(typeRef.Namespace)),
                builder.GetOrAddString(reader.GetString(typeRef.Name)));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetTypeReference(Handle).Name);
        }
    }
}
