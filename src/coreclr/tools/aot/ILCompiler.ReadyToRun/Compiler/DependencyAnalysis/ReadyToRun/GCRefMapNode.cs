// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class GCRefMapNode : ObjectNode, ISymbolDefinitionNode
    {
        /// <summary>
        /// Number of GC ref map records to represent with a single lookup pointer
        /// </summary>
        public const int GCREFMAP_LOOKUP_STRIDE = 1024;

        private readonly ImportSectionNode _importSection;
        private readonly List<Import> _imports;

        public GCRefMapNode(ImportSectionNode importSection)
        {
            _importSection = importSection;
            _imports = new List<Import>();
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override bool IsShareable => false;

        public override int ClassCode => 555444333;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public bool IsEmpty => _imports.Count == 0;

        public void AddImport(Import import)
        {
            lock (_imports)
            {
                _imports.Add(import);
            }
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("GCRefMap->"u8);
            sb.Append(_importSection.Name);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (_imports.Count == 0 || relocsOnly)
            {
                return new ObjectData(
                    data: Array.Empty<byte>(),
                    relocs: Array.Empty<Relocation>(),
                    alignment: 1,
                    definedSymbols: new ISymbolDefinitionNode[] { this });
            }

            // Preserve a record for every cell, including non-method cells, in physical import order.
            _imports.MergeSort(new EmbeddedObjectNodeComparer(CompilerComparer.Instance));
            GCRefMapBuilder builder = new GCRefMapBuilder(factory.Target, relocsOnly);
            builder.Builder.RequireInitialAlignment(4);
            builder.Builder.AddSymbol(this);

            // First, emit the initial ref map offset and reserve the offset map entries
            int offsetCount = _imports.Count / GCREFMAP_LOOKUP_STRIDE;
            builder.Builder.EmitInt((offsetCount + 1) * sizeof(int));

            ObjectDataBuilder.Reservation[] offsets = new ObjectDataBuilder.Reservation[offsetCount];
            for (int offsetIndex = 0; offsetIndex < offsetCount; offsetIndex++)
            {
                offsets[offsetIndex] = builder.Builder.ReserveInt();
            }

            // Next, generate the actual method GC ref maps and update the offset map
            int nextOffsetIndex = 0;
            int nextMethodIndex = GCREFMAP_LOOKUP_STRIDE - 1;
            for (int methodIndex = 0; methodIndex < _imports.Count; methodIndex++)
            {
                IMethodNode methodNode = _imports[methodIndex] as IMethodNode;
                if (methodNode == null)
                {
                    // Flush an empty GC ref map block to prevent
                    // the indexed records from falling out of sync with methods
                    builder.Flush();
                }
                else
                {
                    bool isStub = false;
                    if (methodNode is DelayLoadHelperImport methodImport)
                    {
                        MethodFixupSignature signature = (MethodFixupSignature)methodImport.ImportSignature.Target;
                        isStub = signature.IsUnboxingStub || signature.IsInstantiatingStub;
                    }
                    builder.GetCallRefMap(methodNode.Method, isStub);
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

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_importSection, ((GCRefMapNode)other)._importSection);
        }
    }
}
