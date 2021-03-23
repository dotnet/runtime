// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class RuntimeFunctionsTableNode : HeaderTableNode
    {
        private List<MethodWithGCInfo> _methodNodes;
        private Dictionary<MethodWithGCInfo, int> _insertedMethodNodes;
        private readonly NodeFactory _nodeFactory;
        private int _tableSize = -1;

        public RuntimeFunctionsTableNode(NodeFactory nodeFactory)
            : base(nodeFactory.Target)
        {
            _nodeFactory = nodeFactory;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunRuntimeFunctionsTable");
        }

        public int GetIndex(MethodWithGCInfo method)
        {
#if DEBUG
            Debug.Assert(_nodeFactory.MarkingComplete);
            Debug.Assert(method.Marked);
#endif
            if (_methodNodes == null)
                LayoutRuntimeFunctions();

            return _insertedMethodNodes[method];
        }

        private void LayoutRuntimeFunctions()
        {
            _methodNodes = new List<MethodWithGCInfo>();
            _insertedMethodNodes = new Dictionary<MethodWithGCInfo, int>();

            int runtimeFunctionIndex = 0;

            foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
            {
                _methodNodes.Add(method);
                _insertedMethodNodes[method] = runtimeFunctionIndex;
                runtimeFunctionIndex += method.FrameInfos.Length;
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            if (_methodNodes == null)
                LayoutRuntimeFunctions();

            ObjectDataBuilder runtimeFunctionsBuilder = new ObjectDataBuilder(factory, relocsOnly);

            // Add the symbol representing this object node
            runtimeFunctionsBuilder.AddSymbol(this);

            foreach (MethodWithGCInfo method in _methodNodes)
            {
                int[] funcletOffsets = method.GCInfoNode.CalculateFuncletOffsets(factory);

                for (int frameIndex = 0; frameIndex < method.FrameInfos.Length; frameIndex++)
                {
                    FrameInfo frameInfo = method.FrameInfos[frameIndex];

                    // StartOffset of the runtime function
                    int codeDelta = 0;
                    if (Target.Architecture == TargetArchitecture.ARM)
                    {
                        // THUMB_CODE
                        codeDelta = 1;
                    }
                    runtimeFunctionsBuilder.EmitReloc(method, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: frameInfo.StartOffset + codeDelta);
                    if (!relocsOnly && Target.Architecture == TargetArchitecture.X64)
                    {
                        // On Amd64, the 2nd word contains the EndOffset of the runtime function
                        runtimeFunctionsBuilder.EmitReloc(method, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: frameInfo.EndOffset);
                    }
                    runtimeFunctionsBuilder.EmitReloc(factory.RuntimeFunctionsGCInfo.StartSymbol, RelocType.IMAGE_REL_BASED_ADDR32NB, funcletOffsets[frameIndex]);
                }
            }

            // Emit sentinel entry
            runtimeFunctionsBuilder.EmitUInt(~0u);

            _tableSize = runtimeFunctionsBuilder.CountBytes;
            return runtimeFunctionsBuilder.ToObjectData();
        }

        /// <summary>
        /// Returns the runtime functions table size and excludes the 4 byte sentinel entry at the end (used by
        /// the runtime in NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod) so that it's not treated as
        /// part of the table itself.
        /// </summary>
        public int TableSizeExcludingSentinel
        {
            get
            {
                Debug.Assert(_tableSize >= 0);
                return _tableSize + SentinelSizeAdjustment;
            }
        }

        public override int ClassCode => -855231428;

        internal const int SentinelSizeAdjustment = -4;
    }
}
