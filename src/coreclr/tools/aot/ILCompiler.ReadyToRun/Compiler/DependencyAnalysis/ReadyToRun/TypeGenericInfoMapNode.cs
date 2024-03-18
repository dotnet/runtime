// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class TypeGenericInfoMapNode : ModuleSpecificHeaderTableNode
    {
        private MetadataReader _metadata;

        public TypeGenericInfoMapNode(EcmaModule module) : base (module)
        {
            _metadata = module.MetadataReader;
        }

        public static bool IsSupported(MetadataReader metadata)
        {
            // Only support this map with R2R images of some size
            return metadata.TypeDefinitions.Count > 16;
        }

        public override int ClassCode => 1329419084;

        protected override string ModuleSpecificName => "__TypeGenericInfoMapNode__";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            builder.EmitUInt((uint)_metadata.TypeDefinitions.Count);

            int usedBits = 0;
            byte curByte = 0;
            foreach (var typeDefinitionHandle in _metadata.TypeDefinitions)
            {
                var typeDefinition = _metadata.GetTypeDefinition(typeDefinitionHandle);
                bool isGeneric = typeDefinition.GetGenericParameters().Count > 0;
                ReadyToRunTypeGenericInfo genericInfo = default(ReadyToRunTypeGenericInfo);
                bool hasVariance = false;
                bool hasConstraints = false;

                foreach (var genericParameterHandle in typeDefinition.GetGenericParameters())
                {
                    var genericParameter = _metadata.GetGenericParameter(genericParameterHandle);
                    if ((genericParameter.Attributes & GenericParameterAttributes.VarianceMask) != GenericParameterAttributes.None)
                        hasVariance = true;

                    if ((genericParameter.Attributes & (GenericParameterAttributes.SpecialConstraintMask | (GenericParameterAttributes)GenericConstraints.AllowByRefLike)) != default(GenericParameterAttributes) ||
                        (genericParameter.GetConstraints().Count > 0))
                    {
                        hasConstraints = true;
                    }
                }

                ReadyToRunGenericInfoGenericCount count;
                switch (typeDefinition.GetGenericParameters().Count)
                {
                    case 0:
                        count = ReadyToRunGenericInfoGenericCount.Zero; break;
                    case 1:
                        count = ReadyToRunGenericInfoGenericCount.One; break;
                    case 2:
                        count = ReadyToRunGenericInfoGenericCount.Two; break;
                    default:
                        count = ReadyToRunGenericInfoGenericCount.MoreThanTwo; break;
                }

                genericInfo = (ReadyToRunTypeGenericInfo)count |
                              (hasVariance ? ReadyToRunTypeGenericInfo.HasVariance : default(ReadyToRunTypeGenericInfo)) |
                              (hasConstraints ? ReadyToRunTypeGenericInfo.HasConstraints : default(ReadyToRunTypeGenericInfo));

                curByte |= (byte)genericInfo;
                usedBits += 4;
                if (usedBits == 8)
                {
                    builder.EmitByte(curByte);
                    usedBits = 0;
                    curByte = 0;
                }
                else
                {
                    curByte <<= 4;
                }
            }
            if (usedBits != 0)
                builder.EmitByte(curByte);

            return builder.ToObjectData();
        }
    }
}
