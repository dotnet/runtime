// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
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
        private const int WasmGroupSize = 128;
        private const int WasmGroupEntryCount = 32;
        private const uint WasmUnwindEscapeStart = 0xF0000;
        private readonly List<WasmRuntimeFunction> _wasmRuntimeFunctions = new();
        private readonly Dictionary<(MethodWithGCInfo, int), int> _wasmFrameIndices = new();

        private readonly struct WasmRuntimeFunction
        {
            public WasmRuntimeFunction(uint virtualIP, uint unwindOffset, bool isFunclet, bool isSkip = false)
            {
                VirtualIP = virtualIP;
                UnwindOffset = unwindOffset;
                IsFunclet = isFunclet;
                IsSkip = isSkip;
            }

            public readonly uint VirtualIP;
            public readonly uint UnwindOffset;
            public readonly bool IsFunclet;
            public readonly bool IsSkip;
        }

        public int WasmSkipCount { get; private set; }
        public int WasmAlignmentSkipCount { get; private set; }
        public int WasmRuntimeFunctionCount => _wasmRuntimeFunctions.Count;

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
            if (_nodeFactory.Target.IsWasm)
            {
                _nodeFactory.RuntimeFunctionsGCInfo.GetData(_nodeFactory, relocsOnly: false);
            }
            _methodNodes = new List<MethodWithGCInfo>();
            _insertedMethodNodes = new Dictionary<MethodWithGCInfo, int>();

            int runtimeFunctionIndex = 0;

            foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
            {
                _methodNodes.Add(method);
                _insertedMethodNodes[method] = runtimeFunctionIndex;
                runtimeFunctionIndex += method.FrameInfos.Length;
            }

            if (_nodeFactory.Target.IsWasm)
            {
                LayoutWasmRuntimeFunctions();
            }
        }

        private void LayoutWasmRuntimeFunctions()
        {
            _methodToVirtualIP = new();
            uint virtualIP = 0;
            uint groupBase = 0;
            uint? unwindPrefix = null;
            bool precedingIsFunclet = false;

            foreach (MethodWithGCInfo method in _methodNodes)
            {
                Debug.Assert(method.ColdCodeNode is null);
                int[] unwindOffsets = method.GCInfoNode.CalculateFuncletOffsets(_nodeFactory);
                for (int frame = 0; frame < method.FrameInfos.Length; frame++)
                {
                    uint unwindOffset = checked((uint)unwindOffsets[frame]);
                    int index = _wasmRuntimeFunctions.Count % WasmGroupEntryCount;
                    if (index == 0)
                    {
                        groupBase = virtualIP;
                        unwindPrefix = null;
                    }

                    bool escapes = unwindOffset >= WasmUnwindEscapeStart;
                    long residual = (long)virtualIP - groupBase - index * 4;
                    Debug.Assert(residual >= 0);
                    if (residual < 0)
                    {
                        throw new InvalidOperationException("Wasm runtime functions must have at least four virtual IP units.");
                    }
                    if (residual >= byte.MaxValue ||
                        (index == WasmGroupEntryCount - 1 && (escapes || unwindPrefix.HasValue)) ||
                        (escapes && unwindPrefix.HasValue && unwindPrefix.Value != (unwindOffset >> 16)))
                    {
                        PadGroup();
                        groupBase = virtualIP;
                        unwindPrefix = null;
                    }

                    if (escapes)
                    {
                        unwindPrefix = unwindOffset >> 16;
                    }

                    int ordinal = _wasmRuntimeFunctions.Count;
                    if (frame == 0)
                    {
                        _insertedMethodNodes[method] = ordinal;
                    }
                    _wasmFrameIndices.Add((method, frame), ordinal);
                    _methodToVirtualIP.Add((method, frame), virtualIP);
                    precedingIsFunclet = frame != 0;
                    _wasmRuntimeFunctions.Add(new WasmRuntimeFunction(virtualIP, unwindOffset, precedingIsFunclet));
                    virtualIP = checked(virtualIP + GetFunctionLocalVirtualIPCountFromUnwindBlob(method.FrameInfos[frame].BlobData));
                }
            }

            int skipsBeforeFinalPadding = WasmSkipCount;
            PadGroup();
            WasmAlignmentSkipCount = WasmSkipCount - skipsBeforeFinalPadding;

            void PadGroup()
            {
                while (_wasmRuntimeFunctions.Count % WasmGroupEntryCount != 0)
                {
                    _wasmRuntimeFunctions.Add(new WasmRuntimeFunction(0, 0, precedingIsFunclet, isSkip: true));
                    WasmSkipCount++;
                }
            }
        }

        public int GetWasmPaddingAfter(MethodWithGCInfo method, int frame, out int ordinal)
        {
            GetIndex(method);
            ordinal = _wasmFrameIndices[(method, frame)];
            int next = ordinal + 1;
            int count = 0;
            while (next < _wasmRuntimeFunctions.Count && _wasmRuntimeFunctions[next].IsSkip)
            {
                count++;
                next++;
            }

            return count;
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

            LayoutRuntimeFunctions();
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

            if (factory.Target.IsWasm)
            {
                return GetWasmData(factory);
            }

            ObjectDataBuilder runtimeFunctionsBuilder = new ObjectDataBuilder(factory, relocsOnly);
            runtimeFunctionsBuilder.RequireInitialAlignment(4);

            // Add the symbol representing this object node
            runtimeFunctionsBuilder.AddSymbol(this);

            uint runtimeFunctionIndex = 0;
            List<uint> mapping = new List<uint>();

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

                        runtimeFunctionsBuilder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ADDR32NB, frameInfo.StartOffset);
                        if (!relocsOnly && _nodeFactory.Target.Architecture == TargetArchitecture.X64)
                        {
                            // On Amd64, the 2nd word contains the EndOffset of the runtime function
                            Debug.Assert(frameInfo.StartOffset != frameInfo.EndOffset);
                            runtimeFunctionsBuilder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: frameInfo.EndOffset);
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

            _tableSize = runtimeFunctionsBuilder.CountBytes;
            return runtimeFunctionsBuilder.ToObjectData();
        }

        private ObjectData GetWasmData(NodeFactory factory)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly: false);
            builder.RequireInitialAlignment(WasmGroupSize);
            builder.AddSymbol(this);

            for (int start = 0; start < _wasmRuntimeFunctions.Count; start += WasmGroupEntryCount)
            {
                byte[] group = new byte[WasmGroupSize];
                uint virtualIPBase = _wasmRuntimeFunctions[start].VirtualIP;
                BinaryPrimitives.WriteUInt32LittleEndian(group, virtualIPBase);
                uint funcletBits = 0;
                for (int index = 0; index < WasmGroupEntryCount; index++)
                {
                    WasmRuntimeFunction function = _wasmRuntimeFunctions[start + index];
                    group[4 + index] = function.IsSkip ? byte.MaxValue :
                        checked((byte)(function.VirtualIP - virtualIPBase - (uint)index * 4));
                    if (function.IsFunclet)
                    {
                        funcletBits |= 1u << index;
                    }
                    if (function.IsSkip)
                    {
                        continue;
                    }

                    uint high = function.UnwindOffset >> 16;
                    if (function.UnwindOffset >= WasmUnwindEscapeStart)
                    {
                        Debug.Assert(_wasmRuntimeFunctions[start + WasmGroupEntryCount - 1].IsSkip);
                        group[56 + WasmGroupEntryCount - 1] = (byte)(function.UnwindOffset >> 24);
                        group[88 + WasmGroupEntryCount - 1] = (byte)(function.UnwindOffset >> 16);
                        high = 0xF;
                    }
                    group[40 + index / 2] |= (byte)(high << ((index & 1) * 4));
                    group[56 + index] = (byte)(function.UnwindOffset >> 8);
                    group[88 + index] = (byte)function.UnwindOffset;
                }
                BinaryPrimitives.WriteUInt32LittleEndian(group.AsSpan(36), funcletBits);
                builder.EmitBytes(group);
            }

            builder.EmitUInt(uint.MaxValue);
            if (_methodNodes.Count != 0)
            {
                builder.EmitReloc(_methodNodes[0], RelocType.WASM_TABLE_INDEX_I32);
            }
            else
            {
                builder.EmitUInt(0);
            }
            _tableSize = builder.CountBytes;
            return builder.ToObjectData();
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
