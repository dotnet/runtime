// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Module metadata table.
    /// </summary>
    public sealed class ModuleDefinitionNode : TokenBasedNode
    {
        // The first entry in the TypeDefinition table has a special meaning and is where
        // global fields and methods go.
        private TypeDefinitionHandle GlobalModuleTypeHandle => MetadataTokens.TypeDefinitionHandle(1);

        public ModuleDefinitionNode(EcmaModule module)
            : base(module, EntityHandle.ModuleDefinition)
        {
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            if (_module.MetadataReader.IsAssembly)
                yield return new DependencyListEntry(factory.AssemblyDefinition(_module), "Assembly definition of the module");

            yield return new DependencyListEntry(factory.TypeDefinition(_module, GlobalModuleTypeHandle), "Global module type");

            foreach (CustomAttributeHandle customAttribute in _module.MetadataReader.GetModuleDefinition().GetCustomAttributes())
            {
                yield return new(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a module");
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            ModuleDefinition moduleDefinition = reader.GetModuleDefinition();

            MetadataBuilder mdBuilder = writeContext.MetadataBuilder;

            // The global module type is the type with RID 1. It needs to map back to RID 1.
            Debug.Assert(writeContext.TokenMap.MapToken(GlobalModuleTypeHandle) == GlobalModuleTypeHandle);

            return mdBuilder.AddModule(moduleDefinition.Generation,
                mdBuilder.GetOrAddString(reader.GetString(moduleDefinition.Name)),
                mdBuilder.GetOrAddGuid(reader.GetGuid(moduleDefinition.Mvid)),
                mdBuilder.GetOrAddGuid(reader.GetGuid(moduleDefinition.GenerationId)),
                mdBuilder.GetOrAddGuid(reader.GetGuid(moduleDefinition.BaseGenerationId)));
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetModuleDefinition().Name);
        }
    }
}
