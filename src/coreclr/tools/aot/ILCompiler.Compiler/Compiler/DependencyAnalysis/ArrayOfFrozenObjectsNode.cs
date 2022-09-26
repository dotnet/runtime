// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ArrayOfFrozenObjectsNode<TEmbedded> : ArrayOfEmbeddedDataNode<TEmbedded>
        where TEmbedded : EmbeddedObjectNode
    {
        public ArrayOfFrozenObjectsNode(string startSymbolMangledName, string endSymbolMangledName, IComparer<TEmbedded> nodeSorter) : base(startSymbolMangledName, endSymbolMangledName, nodeSorter)
        {
        }

        private static void AlignNextObject(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            builder.EmitZeros(AlignmentHelper.AlignUp(builder.CountBytes, factory.Target.PointerSize) - builder.CountBytes);
        }

        protected override void GetElementDataForNodes(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            foreach (EmbeddedObjectNode node in NodesList)
            {
                AlignNextObject(ref builder, factory);

                if (!relocsOnly)
                    node.InitializeOffsetFromBeginningOfArray(builder.CountBytes);

                node.EncodeData(ref builder, factory, relocsOnly);
                if (node is ISymbolDefinitionNode)
                {
                    builder.AddSymbol((ISymbolDefinitionNode)node);
                }
            }

            // Terminate with a null pointer as expected by the GC
            AlignNextObject(ref builder, factory);
            builder.EmitZeroPointer();
        }

        public override int ClassCode => -1771336339;
    }
}
