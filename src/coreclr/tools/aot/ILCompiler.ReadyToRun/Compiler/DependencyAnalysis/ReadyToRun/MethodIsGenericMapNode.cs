// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using System.Diagnostics;
using System.Reflection.Metadata;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodIsGenericMapNode : ModuleSpecificHeaderTableNode
    {
        private MetadataReader _metadata;

        public MethodIsGenericMapNode(EcmaModule module) : base(module)
        {
            _metadata = module.MetadataReader;
        }

        public static bool IsSupported(MetadataReader metadata)
        {
            // Only support this map with R2R images of some size
            return metadata.MethodDefinitions.Count > 32;
        }

        public override int ClassCode => 606284890;

        protected override string ModuleSpecificName => "__MethodIsGenericMap__";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            builder.EmitInt(_metadata.MethodDefinitions.Count);

            int usedBits = 0;
            byte curByte = 0;
            foreach (var methodDefinitionHandle in _metadata.MethodDefinitions)
            {
                var methodDefinition = _metadata.GetMethodDefinition(methodDefinitionHandle);
                bool isGeneric = methodDefinition.GetGenericParameters().Count > 0;
                curByte |= isGeneric ? (byte)1 : (byte)0;
                usedBits++;
                if (usedBits == 8)
                {
                    builder.EmitByte(curByte);
                    usedBits = 0;
                    curByte = 0;
                }
                else
                {
                    curByte <<= 1;
                }
            }
            if (usedBits != 0)
                builder.EmitByte((byte)(curByte << (7 - usedBits)));

            return builder.ToObjectData();
        }
    }
}
