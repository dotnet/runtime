// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.ObjectWriter;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class RuntimeFunctionsTableNode : HeaderTableNode
    {
        private List<MethodWithGCInfo> _methodNodes;
        private Dictionary<MethodWithGCInfo, int> _insertedMethodNodes;
        private Dictionary<(MethodWithGCInfo method, int funcletIdx), uint> _methodToVirtualIP;
        private readonly NodeFactory _nodeFactory;
        private int _tableSize = -1;

        public RuntimeFunctionsTableNode(NodeFactory nodeFactory)
        {
            _nodeFactory = nodeFactory;
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunRuntimeFunctionsTable"u8);
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

        public uint GetWasmVirtualIP(MethodWithGCInfo lookupMethod, int lookupFrameIndex)
        {
            bool isWasm = _nodeFactory.Target.Architecture == TargetArchitecture.Wasm32;

            if (!isWasm)
            {
                // For non-WASM targets, virtual IPs are not used, so throw
                throw new InvalidOperationException("Virtual IPs are not used on non-WASM targets.");
            }

            if (_methodToVirtualIP != null)
            {
                return _methodToVirtualIP[(lookupMethod, lookupFrameIndex)];
            }

            if (_methodNodes == null)
            {
                LayoutRuntimeFunctions();
            }

            Dictionary<(MethodWithGCInfo method, int funcletIdx), uint> methodToVirtualIP = new ();

            // For WASM, track virtual IP index for each RUNTIME_FUNCTION entry
            uint currentVirtualIP = 0;

            foreach (MethodWithGCInfo method in _methodNodes)
            {
                for (int frameIndex = 0; frameIndex < method.FrameInfos.Length; frameIndex++)
                {
                    FrameInfo frameInfo = method.FrameInfos[frameIndex];
                    methodToVirtualIP.Add((method, frameIndex), currentVirtualIP);

                    // Advance the virtual IP by the number of virtual IPs for this frame
                    uint virtualIPCount = GetFunctionLocalVirtualIPCountFromUnwindBlob(frameInfo.BlobData);
                    currentVirtualIP += virtualIPCount;
                }
            }

            _methodToVirtualIP = methodToVirtualIP;
            return _methodToVirtualIP[(lookupMethod, lookupFrameIndex)];
        }

        /// <summary>
        /// Read the virtual IP count from a WASM unwind blob.
        /// The blob format is: ULEB128(frameSize) followed by ULEB128(virtualIPCount).
        /// </summary>
        private static uint GetFunctionLocalVirtualIPCountFromUnwindBlob(byte[] blobData)
        {
            DwarfHelper.ReadULEB128(blobData.AsSpan(), out int offset); // skip frame size
            return (uint)DwarfHelper.ReadULEB128(blobData.AsSpan(offset), out _) * 2; // Multiply by 2 to force all virtual IPs to be an even number.
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            if (_methodNodes == null)
                LayoutRuntimeFunctions();

            ObjectDataBuilder runtimeFunctionsBuilder = new ObjectDataBuilder(factory, relocsOnly);
            runtimeFunctionsBuilder.RequireInitialAlignment(4);

            // Add the symbol representing this object node
            runtimeFunctionsBuilder.AddSymbol(this);

            uint runtimeFunctionIndex = 0;
            List<uint> mapping = new List<uint>();

            bool isWasm = _nodeFactory.Target.Architecture == TargetArchitecture.Wasm32;

            for (int cold = 0; cold < 2; cold++)
            {
                foreach (MethodWithGCInfo method in _methodNodes)
                {
                    int[] funcletOffsets = method.GCInfoNode.CalculateFuncletOffsets(factory);
                    int startIndex;
                    int endIndex;

                    if (cold == 0)
                    {
                        startIndex = 0;
                        endIndex = method.FrameInfos.Length;
                    }
                    else if (method.ColdCodeNode == null)
                    {
                        continue;
                    }
                    else
                    {
                        Debug.Assert((method.FrameInfos.Length + method.ColdFrameInfos.Length) == funcletOffsets.Length);
                        startIndex = method.FrameInfos.Length;
                        endIndex = funcletOffsets.Length;
                    }

                    for (int frameIndex = startIndex; frameIndex < endIndex; frameIndex++)
                    {
                        FrameInfo frameInfo;
                        ISymbolNode symbol;

                        if (frameIndex >= method.FrameInfos.Length)
                        {
                            frameInfo = method.ColdFrameInfos[frameIndex - method.FrameInfos.Length];
                            symbol = method.ColdCodeNode;

                            if (frameIndex == method.FrameInfos.Length)
                            {
                                mapping.Add(runtimeFunctionIndex);
                                mapping.Add((uint)_insertedMethodNodes[method]);
                            }
                        }
                        else
                        {
                            frameInfo = method.FrameInfos[frameIndex];
                            symbol = method;
                        }

                        if (isWasm)
                        {
                            // Set high bit for frame indices greater than 0 to indicate that the RUNTIME_FUNCTION is a funclet
                            runtimeFunctionsBuilder.EmitUInt(GetWasmVirtualIP(method, frameIndex) | (frameIndex != 0 ? 0x80000000 : 0));
                        }
                        else
                        {
                            runtimeFunctionsBuilder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ADDR32NB, frameInfo.StartOffset);
                            if (!relocsOnly && _nodeFactory.Target.Architecture == TargetArchitecture.X64)
                            {
                                // On Amd64, the 2nd word contains the EndOffset of the runtime function
                                Debug.Assert(frameInfo.StartOffset != frameInfo.EndOffset);
                                runtimeFunctionsBuilder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: frameInfo.EndOffset);
                            }
                        }
                        runtimeFunctionsBuilder.EmitReloc(factory.RuntimeFunctionsGCInfo, RelocType.IMAGE_REL_BASED_ADDR32NB, funcletOffsets[frameIndex]);
                        runtimeFunctionIndex++;
                    }
                }
            }

            // HotColdMap should not be null if there is cold code
            if (_nodeFactory.HotColdMap != null)
            {
                _nodeFactory.HotColdMap.Mapping = mapping.ToArray();
            }
            else
            {
                Debug.Assert((mapping.Count == 0), "HotColdMap is null, but mapping is not empty");
            }

            // Emit sentinel entry
            runtimeFunctionsBuilder.EmitUInt(~0u);

            if (isWasm)
            {
                // After sentinel, emit a WASM_TABLE_INDEX_I32 reloc pointing to the first
                // compiled method. This records the min function table index in the file.
                if (_methodNodes.Count > 0)
                {
                    runtimeFunctionsBuilder.EmitReloc(_methodNodes[0], RelocType.WASM_TABLE_INDEX_I32, delta: 0);
                }
                else
                {
                    // If there are no compiled methods, emit a 0 for the min table index.
                    runtimeFunctionsBuilder.EmitUInt(0);
                }
            }

            _tableSize = runtimeFunctionsBuilder.CountBytes;
            return runtimeFunctionsBuilder.ToObjectData();
        }

        /// <summary>
        /// Returns the runtime functions table size and excludes the 4 byte sentinel entry at the end (used by
        /// the runtime in NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod) so that it's not treated as
        /// part of the table itself. For WASM, also excludes the trailing 4-byte min table index.
        /// </summary>
        public int TableSizeExcludingSentinel
        {
            get
            {
                Debug.Assert(_tableSize >= 0);
                return _tableSize + SentinelSizeAdjustment;
            }
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.RuntimeFunctionsTableNode;

        internal int SentinelSizeAdjustment =>
            _nodeFactory.Target.Architecture == TargetArchitecture.Wasm32 ? -8 : -4;
    }
}
