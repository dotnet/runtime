// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem.Ecma;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class EnclosingTypeMapNode : ModuleSpecificHeaderTableNode
    {
        private MetadataReader _metadata;

        public EnclosingTypeMapNode(EcmaModule module) : base(module)
        {
            _metadata = module.MetadataReader;
            // This map is only valid for assemblies with <= ReadyToRunEnclosingTypeMap.MaxTypeCount types defined within
            if (!IsSupported(_metadata))
                throw new InternalCompilerErrorException($"EnclosingTypeMap made for assembly with more than 0x{(uint)ReadyToRunEnclosingTypeMap.MaxTypeCount:x} types");
        }

        public static bool IsSupported(MetadataReader metadata)
        {
            // This map is only valid for assemblies with <= ReadyToRunEnclosingTypeMap.MaxTypeCount types defined within
            // and really shouldn't be generated for tiny assemblies, as the map provides very little to no value
            // in those situations
            int typeDefinitionCount = metadata.TypeDefinitions.Count;

            return ((typeDefinitionCount > 10) && (typeDefinitionCount <= (int)ReadyToRunEnclosingTypeMap.MaxTypeCount));
        }

        public override int ClassCode => 990540812;

        protected override string ModuleSpecificName => "__EnclosingTypeMap__";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            // This map is only valid for assemblies with <= ReadyToRunEnclosingTypeMap.MaxTypeCount types defined within
            Debug.Assert(_metadata.TypeDefinitions.Count <= (int)ReadyToRunEnclosingTypeMap.MaxTypeCount);
            builder.EmitUShort(checked((ushort)_metadata.TypeDefinitions.Count));

            foreach (var typeDefinitionHandle in _metadata.TypeDefinitions)
            {
                var typeDefinition = _metadata.GetTypeDefinition(typeDefinitionHandle);
                if (!typeDefinition.IsNested)
                {
                    builder.EmitUShort(0);
                }
                else
                {
                    builder.EmitUShort(checked((ushort)MetadataTokens.GetRowNumber(typeDefinition.GetDeclaringType())));
                }
            }

            return builder.ToObjectData();
        }
    }
}
