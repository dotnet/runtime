// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ArrayOfFrozenObjectsNode : DehydratableObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;
        int INodeWithSize.Size => _size.Value;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__FrozenSegmentStart");

        public int Offset => 0;

        private static void AlignNextObject(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            builder.EmitZeros(AlignmentHelper.AlignUp(builder.CountBytes, factory.Target.PointerSize) - builder.CountBytes);
        }

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly)
        {
            // This is a summary node
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            foreach (FrozenObjectNode node in factory.MetadataManager.GetFrozenObjects())
            {
                AlignNextObject(ref builder, factory);

                node.InitializeOffsetFromBeginningOfArray(builder.CountBytes);

                int initialOffset = builder.CountBytes;
                node.EncodeData(ref builder, factory, relocsOnly);
                int objectSize = builder.CountBytes - initialOffset;
                int minimumObjectSize = EETypeNode.GetMinimumObjectSize(factory.TypeSystemContext);
                if (objectSize < minimumObjectSize)
                {
                    builder.EmitZeros(minimumObjectSize - objectSize);
                }

                builder.AddSymbol(node);
            }

            // Terminate with a null pointer as expected by the GC
            AlignNextObject(ref builder, factory);
            builder.EmitZeroPointer();

            _size = builder.CountBytes;

            return builder.ToObjectData();
        }

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory) => ObjectNodeSection.DataSection;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -1771336339;

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;
    }
}
