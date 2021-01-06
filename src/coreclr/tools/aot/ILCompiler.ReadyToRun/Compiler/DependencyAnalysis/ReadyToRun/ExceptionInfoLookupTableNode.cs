// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class EHInfoNode : ObjectNode, ISymbolDefinitionNode
    {
        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override bool IsShareable => false;

        public override int ClassCode => 354769871;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        private ArrayBuilder<byte> _ehInfoBuilder;

        public EHInfoNode()
        {
            _ehInfoBuilder = new ArrayBuilder<byte>();
        }

        public int AddEHInfo(byte[] ehInfo)
        {
            int offset = _ehInfoBuilder.Count;
            _ehInfoBuilder.Append(ehInfo);
            return offset;
        }

        public int Count => _ehInfoBuilder.Count;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            // EH info node is a singleton in the R2R PE file
            sb.Append("EHInfoNode");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return new ObjectData(_ehInfoBuilder.ToArray(), Array.Empty<Relocation>(), alignment: 4, definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        protected override string GetName(NodeFactory context)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(context.NameMangler, sb);
            return sb.ToString();
        }
    }

    public class ExceptionInfoLookupTableNode : HeaderTableNode
    {
        private List<MethodWithGCInfo> _methodNodes;
        private List<int> _ehInfoOffsets;

        private readonly NodeFactory _nodeFactory;

        private readonly EHInfoNode _ehInfoNode;

        public ExceptionInfoLookupTableNode(NodeFactory nodeFactory)
            : base(nodeFactory.Target)
        {
            _nodeFactory = nodeFactory;
            _ehInfoNode = new EHInfoNode();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunExceptionInfoLookupTable@");
            sb.Append(Offset.ToString());
        }

        internal void LayoutMethodsWithEHInfo()
        {
            if (_methodNodes != null)
            {
                // Already initialized
                return;
            }

            _methodNodes = new List<MethodWithGCInfo>();
            _ehInfoOffsets = new List<int>();

            foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
            {
                ObjectData ehInfo = method.EHInfo;
                if (ehInfo != null && ehInfo.Data.Length != 0)
                {
                    _methodNodes.Add(method);
                    _ehInfoOffsets.Add(_ehInfoNode.AddEHInfo(ehInfo.Data));
                }
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            LayoutMethodsWithEHInfo();

            ObjectDataBuilder exceptionInfoLookupBuilder = new ObjectDataBuilder(factory, relocsOnly);
            exceptionInfoLookupBuilder.RequireInitialAlignment(2 * sizeof(uint));

            // Add the symbol representing this object node
            exceptionInfoLookupBuilder.AddSymbol(this);

            // First, emit the actual EH records in sequence and store map from methods to the EH record symbols
            for (int index = 0; index < _methodNodes.Count; index++)
            {
                exceptionInfoLookupBuilder.EmitReloc(_methodNodes[index], RelocType.IMAGE_REL_BASED_ADDR32NB);
                exceptionInfoLookupBuilder.EmitReloc(_ehInfoNode, RelocType.IMAGE_REL_BASED_ADDR32NB, _ehInfoOffsets[index]);
            }

            // Sentinel record - method RVA = -1, EH info offset = end of the EH info block
            exceptionInfoLookupBuilder.EmitUInt(~0u);
            exceptionInfoLookupBuilder.EmitReloc(_ehInfoNode, RelocType.IMAGE_REL_BASED_ADDR32NB, _ehInfoNode.Count);

            return exceptionInfoLookupBuilder.ToObjectData();
        }

        /// <summary>
        /// CoreCLR runtime asserts that, when the EXCEPTION_INFO table is present, it must have at least
        /// two entries. When we don't have any EH info to emit, we just skip the entire table.
        /// </summary>
        /// <param name="factory">Reference node factory</param>
        /// <returns></returns>
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            LayoutMethodsWithEHInfo();
            return _methodNodes.Count == 0;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            return new DependencyNodeCore<NodeFactory>.DependencyList(new DependencyListEntry[] { new DependencyListEntry(_ehInfoNode, "EH info array") });
        }

        public override int ClassCode => 582513248;
    }
}
