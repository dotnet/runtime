// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Internal.Text;

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
            ((ReadyToRunCodegenNodeFactory)factory).RuntimeFunctionsGCInfo.AddEmbeddedObject(this);
        }

        public int[] CalculateFuncletOffsets(NodeFactory factory)
        {
            int[] offsets = new int[_methodNode.FrameInfos.Length];
            int offset = OffsetFromBeginningOfArray;
            for (int frameInfoIndex = 0; frameInfoIndex < _methodNode.FrameInfos.Length; frameInfoIndex++)
            {
                offsets[frameInfoIndex] = offset;
                offset += _methodNode.FrameInfos[frameInfoIndex].BlobData.Length;
                offset += (-offset & 3); // 4-alignment for the personality routine
                if (factory.Target.Architecture != Internal.TypeSystem.TargetArchitecture.X86)
                {
                    offset += sizeof(uint); // personality routine
                }
                if (frameInfoIndex == 0 && _methodNode.GCInfo != null)
                {
                    offset += _methodNode.GCInfo.Length;
                    offset += (-offset & 3); // 4-alignment after GC info in 1st funclet
                }
            }
            return offsets;
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
            {
                return;
            }
            for (int frameInfoIndex = 0; frameInfoIndex < _methodNode.FrameInfos.Length; frameInfoIndex++)
            {
                byte[] unwindInfo = _methodNode.FrameInfos[frameInfoIndex].BlobData;

                if (factory.Target.Architecture == Internal.TypeSystem.TargetArchitecture.X64)
                {
                    // On Amd64, patch the first byte of the unwind info by setting the flags to EHANDLER | UHANDLER
                    // as that's what CoreCLR does (zapcode.cpp, ZapUnwindData::Save).
                    const byte UNW_FLAG_EHANDLER = 1;
                    const byte UNW_FLAG_UHANDLER = 2;
                    const byte FlagsShift = 3;

                    unwindInfo[0] |= (byte)((UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER) << FlagsShift);
                }

                dataBuilder.EmitBytes(unwindInfo);
                // 4-align after emitting the unwind info
                dataBuilder.EmitZeros(-unwindInfo.Length & 3);

                if (factory.Target.Architecture != Internal.TypeSystem.TargetArchitecture.X86)
                {
                    bool isFilterFunclet = (_methodNode.FrameInfos[frameInfoIndex].Flags & FrameInfoFlags.Filter) != 0;
                    ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
                    ISymbolNode personalityRoutine = (isFilterFunclet ? r2rFactory.FilterFuncletPersonalityRoutine : r2rFactory.PersonalityRoutine);
                    dataBuilder.EmitReloc(personalityRoutine, RelocType.IMAGE_REL_BASED_ADDR32NB);
                }

                if (frameInfoIndex == 0 && _methodNode.GCInfo != null)
                {
                    dataBuilder.EmitBytes(_methodNode.GCInfo);

                    // Maintain 4-alignment for the next unwind / GC info block
                    int align4Pad = -_methodNode.GCInfo.Length & 3;
                    dataBuilder.EmitZeros(align4Pad);
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
    }
}
