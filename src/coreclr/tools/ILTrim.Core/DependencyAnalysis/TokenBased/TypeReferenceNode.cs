// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
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
                if (_module.GetObject(Handle, NotFoundBehavior.ReturnNull) is EcmaType ecmaType)
                    return factory.AssemblyReference(_module, (EcmaAssembly)ecmaType.Module);

                AssemblyReference assemblyReference = _module.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
                AssemblyNameInfo referenceName = new AssemblyNameInfo(
                    name: _module.MetadataReader.GetString(assemblyReference.Name),
                    version: assemblyReference.Version,
                    cultureName: _module.MetadataReader.GetString(assemblyReference.Culture),
                    flags: (AssemblyNameFlags)assemblyReference.Flags,
                    publicKeyOrToken: _module.MetadataReader.GetBlobContent(assemblyReference.PublicKeyOrToken));
                return factory.AssemblyReference(_module, referenceName);
            }
            else
            {
                return typeRef.ResolutionScope.Kind switch
                {
                    HandleKind.TypeReference => factory.TypeReference(_module, (TypeReferenceHandle)typeRef.ResolutionScope),
                    HandleKind.ModuleReference => factory.ModuleReference(_module, (ModuleReferenceHandle)typeRef.ResolutionScope),
                    _ => throw new InvalidOperationException(typeRef.ResolutionScope.Kind.ToString()),
                };
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            TokenWriterNode resolutionScopeNode = GetResolutionScopeNode(factory);
            if (resolutionScopeNode is not null)
                yield return new(resolutionScopeNode, "Resolution Scope of a type reference");

            var typeDescObject = _module.GetObject(Handle, NotFoundBehavior.ReturnNull);
            if (typeDescObject is EcmaType typeDef && factory.IsModuleTrimmed(typeDef.Module))
            {
                yield return new(factory.TypeDefinition(typeDef.Module, typeDef.Handle), "Target of a type reference");
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
