// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodGCInfoNode : EmbeddedObjectNode
    {
        private readonly MethodWithGCInfo _methodNode;

        public MethodGCInfoNode(MethodWithGCInfo methodNode)
        {
            _methodNode = methodNode;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override int ClassCode => 892356612;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("MethodGCInfoNode->");
            _methodNode.AppendMangledName(nameMangler, sb);
        }

        protected override void OnMarked(NodeFactory factory)
        {
            if (factory.RuntimeFunctionsGCInfo.Deduplicator == null)
            {
                factory.RuntimeFunctionsGCInfo.Deduplicator = new HashSet<MethodGCInfoNode>(new MethodGCInfoNodeDeduplicatingComparer(factory));
            }
            factory.RuntimeFunctionsGCInfo.AddEmbeddedObject(this);
        }

        public int[] CalculateFuncletOffsets(NodeFactory factory)
        {
            int coldCodeUnwindInfoCount = 0;
            if (_methodNode.ColdCodeNode != null)
            {
                coldCodeUnwindInfoCount = _methodNode.ColdFrameInfos.Length;
            }

            int[] offsets = new int[_methodNode.FrameInfos.Length + coldCodeUnwindInfoCount];
            if (!factory.RuntimeFunctionsGCInfo.Deduplicator.TryGetValue(this, out var deduplicatedResult))
            {
                throw new Exception("Did not properly initialize deduplicator");
            }

            int offset = deduplicatedResult.OffsetFromBeginningOfArray;
            for (int frameInfoIndex = 0; frameInfoIndex < deduplicatedResult._methodNode.FrameInfos.Length; frameInfoIndex++)
            {
                offsets[frameInfoIndex] = offset;
                offset += deduplicatedResult._methodNode.FrameInfos[frameInfoIndex].BlobData.Length;
                offset += (-offset & 3); // 4-alignment for the personality routine
                if (factory.Target.Architecture != TargetArchitecture.X86)
                {
                    offset += sizeof(uint); // personality routine
                }
                if (frameInfoIndex == 0 && deduplicatedResult._methodNode.GCInfo != null)
                {
                    offset += deduplicatedResult._methodNode.GCInfo.Length;
                    offset += (-offset & 3); // 4-alignment after GC info in 1st funclet
                }
            }

            if (_methodNode.ColdCodeNode != null)
            {
                // TODO: Take a look at deduplicatedResult
                for (int frameInfoIndex = 0; frameInfoIndex < _methodNode.ColdFrameInfos.Length; frameInfoIndex++)
                {
                    offsets[frameInfoIndex + _methodNode.FrameInfos.Length] = offset;
                    byte[] blobData = _methodNode.ColdFrameInfos[frameInfoIndex].BlobData;
                    if (blobData != null)
                    {
                        offset += blobData.Length;
                        offset += (-offset & 3); // 4-alignment for the personality routine
                        if (factory.Target.Architecture != TargetArchitecture.X86)
                        {
                            offset += sizeof(uint); // personality routine
                        }
                    }
                    else
                    {
                        // TODO: Update/Verify this amount
                        offset += 16;
                    }
                }
            }

            return offsets;
        }

        struct GCInfoComponent : IEquatable<GCInfoComponent>
        {
            public GCInfoComponent(byte[] bytes)
            {
                Bytes = bytes;
                Symbol = null;
                SymbolDelta = 0;
            }

            public GCInfoComponent(ISymbolNode symbol, int symbolDelta)
            {
                Bytes = null;
                Symbol = symbol;
                SymbolDelta = symbolDelta;
            }

            public readonly byte[] Bytes;
            public readonly ISymbolNode Symbol;
            public readonly int SymbolDelta;

            public override int GetHashCode()
            {
                HashCode hashCode = new HashCode();
                if (Bytes != null)
                {
                    foreach (byte b in Bytes)
                        hashCode.Add(b);
                }
                else
                {
                    hashCode.Add(Symbol);
                    hashCode.Add(SymbolDelta);
                }
                return hashCode.ToHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is GCInfoComponent other && other.Equals(this);
            }

            public bool Equals(GCInfoComponent other)
            {
                if (Bytes != null)
                {
                    if (other.Bytes == null)
                        return false;
                    
                    return Bytes.SequenceEqual(other.Bytes);
                }
                else
                {
                    return Symbol == other.Symbol && SymbolDelta == other.SymbolDelta;
                }
            }
        }

        private IEnumerable<GCInfoComponent> EncodeDataCore(NodeFactory factory)
        {
            TargetArchitecture targetArch = factory.Target.Architecture;

            int numHotFrameInfos = _methodNode.FrameInfos.Length;
            int numFrameInfos = numHotFrameInfos;
            if (_methodNode.ColdCodeNode != null)
            {
                numFrameInfos += _methodNode.ColdFrameInfos.Length;
            }

            const byte UNW_FLAG_EHANDLER = 1;
            const byte UNW_FLAG_UHANDLER = 2;
            const byte UNW_FLAG_CHAININFO = 4;
            const byte FlagsShift = 3;
            
            for (int frameInfoIndex = 0; frameInfoIndex < numFrameInfos; frameInfoIndex++)
            {
                FrameInfo frameInfo = (frameInfoIndex >= numHotFrameInfos) ?
                    _methodNode.ColdFrameInfos[frameInfoIndex - numHotFrameInfos] :
                    _methodNode.FrameInfos[frameInfoIndex];
                byte[] unwindInfo = frameInfo.BlobData;
                if (unwindInfo == null)
                {
                    // Chain unwind info
                    byte[] header = new byte[4];
                    int i = 0;
                    header[i++] = 1 + (UNW_FLAG_CHAININFO << FlagsShift); // Version = 1, UNW_FLAG_CHAININFO
                    header[i++] = 0; // SizeOfProlog = 0
                    header[i++] = 0; // CountOfCode = 0
                    header[i++] = _methodNode.FrameInfos[0].BlobData[3]; // Copying frame and frame offset from main function
                    yield return new GCInfoComponent(header);
                    yield return new GCInfoComponent(_methodNode, 0);
                    yield return new GCInfoComponent(_methodNode, _methodNode.Size);
                    // TODO: Is this correct? 
                    yield return new GCInfoComponent(factory.RuntimeFunctionsGCInfo.StartSymbol, this.OffsetFromBeginningOfArray);
                }
                else
                {
                    if (targetArch == TargetArchitecture.X64)
                    {
                        // On Amd64, patch the first byte of the unwind info by setting the flags to EHANDLER | UHANDLER
                        // as that's what CoreCLR does (zapcode.cpp, ZapUnwindData::Save).
                        unwindInfo[0] |= (byte)((UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER) << FlagsShift);
                    }
                    else if ((targetArch == TargetArchitecture.ARM) || (targetArch == TargetArchitecture.ARM64) || (targetArch == TargetArchitecture.LoongArch64))
                    {
                        // Set the 'X' bit to indicate that there is a personality routine associated with this method
                        unwindInfo[2] |= 1 << 4;
                    }

                    yield return new GCInfoComponent(unwindInfo);

                    if (targetArch != TargetArchitecture.X86)
                    {
                        bool isFilterFunclet = (frameInfo.Flags & FrameInfoFlags.Filter) != 0;
                        ISymbolNode personalityRoutine = (isFilterFunclet ? factory.FilterFuncletPersonalityRoutine : factory.PersonalityRoutine);
                        yield return new GCInfoComponent(personalityRoutine, factory.Target.CodeDelta);
                    }

                    if (frameInfoIndex == 0 && _methodNode.GCInfo != null)
                    {
                        yield return new GCInfoComponent(_methodNode.GCInfo);
                    }
                }
            }
        }

        class MethodGCInfoNodeDeduplicatingComparer : IEqualityComparer<MethodGCInfoNode>
        {
            public MethodGCInfoNodeDeduplicatingComparer(NodeFactory factory)
            {
                _factory = factory;
            }

            NodeFactory _factory;
            public bool Equals(MethodGCInfoNode a, MethodGCInfoNode b)
            {
                return a.EncodeDataCore(_factory).SequenceEqual(b.EncodeDataCore(_factory));
            }
            public int GetHashCode(MethodGCInfoNode node)
            {
                HashCode hashcode = new HashCode();
                foreach (var item in node.EncodeDataCore(_factory))
                {
                    hashcode.Add(item);
                }
                return hashcode.ToHashCode();
            }
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
            {
                return;
            }

            bool isFound = factory.RuntimeFunctionsGCInfo.Deduplicator.TryGetValue(this, out var found);

            if (isFound && (found != this))
            {
                return;
            }

            factory.RuntimeFunctionsGCInfo.Deduplicator.Add(this);

            foreach (var item in EncodeDataCore(factory))
            {
                if (item.Bytes != null)
                {
                    dataBuilder.EmitBytes(item.Bytes);
                    // Maintain 4-alignment for the next unwind / GC info block
                    int align4Pad = -item.Bytes.Length & 3;
                    dataBuilder.EmitZeros(align4Pad);
                }
                else
                {
                    dataBuilder.EmitReloc(item.Symbol, RelocType.IMAGE_REL_BASED_ADDR32NB, item.SymbolDelta);
                }
            }
        }

        protected override string GetName(NodeFactory context)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append("MethodGCInfo->");
            _methodNode.AppendMangledName(context.NameMangler, sb);
            return sb.ToString();
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_methodNode, ((MethodGCInfoNode)other)._methodNode);
        }
    }
}
