// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Assembly metadata table.
    /// </summary>
    public sealed class AssemblyDefinitionNode : TokenBasedNode
    {
        public AssemblyDefinitionNode(EcmaModule module)
            : base(module, EntityHandle.AssemblyDefinition)
        {
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;

            AssemblyDefinition asmDef = _module.MetadataReader.GetAssemblyDefinition();
            CustomAttributeNode.AddDependenciesDueToCustomAttributes(ref dependencies, factory, _module, asmDef.GetCustomAttributes());

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            AssemblyDefinition assemblyDefinition = reader.GetAssemblyDefinition();

            MetadataBuilder mdBuilder = writeContext.MetadataBuilder;

            return mdBuilder.AddAssembly(
                mdBuilder.GetOrAddString(reader.GetString(assemblyDefinition.Name)),
                assemblyDefinition.Version,
                mdBuilder.GetOrAddString(reader.GetString(assemblyDefinition.Culture)),
                mdBuilder.GetOrAddBlob(reader.GetBlobBytes(assemblyDefinition.PublicKey)),
                assemblyDefinition.Flags,
                assemblyDefinition.HashAlgorithm
                );
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetAssemblyDefinition().Name);
        }
    }
}
