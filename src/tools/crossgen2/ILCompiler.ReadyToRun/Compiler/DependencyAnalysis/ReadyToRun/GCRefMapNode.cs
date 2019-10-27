// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class GCRefMapNode : ObjectNode, ISymbolDefinitionNode
    {
        /// <summary>
        /// Number of GC ref map records to represent with a single lookup pointer
        /// </summary>
        public const int GCREFMAP_LOOKUP_STRIDE = 1024;

        private readonly ImportSectionNode _importSection;
        private readonly List<IMethodNode> _methods;

        public GCRefMapNode(ImportSectionNode importSection)
        {
            _importSection = importSection;
            _methods = new List<IMethodNode>();
        }

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override bool IsShareable => false;

        public override int ClassCode => 555444333;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public void AddImport(Import import)
        {
            _methods.Add(import as IMethodNode);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("GCRefMap->");
            sb.Append(_importSection.Name);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (_methods.Count == 0 || relocsOnly)
            {
                return new ObjectData(
                    data: Array.Empty<byte>(), 
                    relocs: Array.Empty<Relocation>(), 
                    alignment: 1, 
                    definedSymbols: new ISymbolDefinitionNode[] { this });
            }

            GCRefMapBuilder builder = new GCRefMapBuilder(factory.Target, relocsOnly);
            builder.Builder.RequireInitialAlignment(4);
            builder.Builder.AddSymbol(this);

            // First, emit the initial ref map offset and reserve the offset map entries
            int offsetCount = _methods.Count / GCREFMAP_LOOKUP_STRIDE;
            builder.Builder.EmitInt((offsetCount + 1) * sizeof(int));

            ObjectDataBuilder.Reservation[] offsets = new ObjectDataBuilder.Reservation[offsetCount];
            for (int offsetIndex = 0; offsetIndex < offsetCount; offsetIndex++)
            {
                offsets[offsetIndex] = builder.Builder.ReserveInt();
            }

            // Next, generate the actual method GC ref maps and update the offset map
            int nextOffsetIndex = 0;
            int nextMethodIndex = GCREFMAP_LOOKUP_STRIDE - 1;
            for (int methodIndex = 0; methodIndex < _methods.Count; methodIndex++)
            {
                IMethodNode methodNode = _methods[methodIndex];
                if (methodNode == null || (methodNode is MethodWithGCInfo methodWithGCInfo && methodWithGCInfo.IsEmpty))
                {
                    // Flush an empty GC ref map block to prevent
                    // the indexed records from falling out of sync with methods
                    builder.Flush();
                }
                else
                {
                    builder.GetCallRefMap(methodNode.Method);
                }
                if (methodIndex >= nextMethodIndex)
                {
                    builder.Builder.EmitInt(offsets[nextOffsetIndex], builder.Builder.CountBytes);
                    nextOffsetIndex++;
                    nextMethodIndex += GCREFMAP_LOOKUP_STRIDE;
                }
            }
            Debug.Assert(nextOffsetIndex == offsets.Length);

            return builder.Builder.ToObjectData();
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }
    }
}
