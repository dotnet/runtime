﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump
{
    public class GcInfo
    {
        private enum GcInfoHeaderFlags
        {
            GC_INFO_IS_VARARG = 0x1,
            GC_INFO_HAS_SECURITY_OBJECT = 0x2,
            GC_INFO_HAS_GS_COOKIE = 0x4,
            GC_INFO_HAS_PSP_SYM = 0x8,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK = 0x30,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE = 0x00,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MT = 0x10,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MD = 0x20,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_THIS = 0x30,
            GC_INFO_HAS_STACK_BASE_REGISTER = 0x40,
            GC_INFO_WANTS_REPORT_ONLY_LEAF = 0x80, // GC_INFO_HAS_TAILCALLS = 0x80, for ARM and ARM64
            GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS = 0x100,
            GC_INFO_REVERSE_PINVOKE_FRAME = 0x200,

            GC_INFO_FLAGS_BIT_SIZE_VERSION_1 = 9,
            GC_INFO_FLAGS_BIT_SIZE = 10,
        };

        public struct InterruptibleRange
        {
            [XmlAttribute("Index")]
            public uint Index { get; set; }
            public uint StartOffset { get; set; }
            public uint StopOffset { get; set; }
            public InterruptibleRange(uint index, uint start, uint stop)
            {
                Index = index;
                StartOffset = start;
                StopOffset = stop;
            }
        }

        public class GcTransition
        {
            [XmlAttribute("Index")]
            public int CodeOffset { get; set; }
            public int SlotId { get; set; }
            public bool IsLive { get; set; }
            public int ChunkId { get; set; }

            public GcTransition() { }

            public GcTransition(int codeOffset, int slotId, bool isLive, int chunkId)
            {
                CodeOffset = codeOffset;
                SlotId = slotId;
                IsLive = isLive;
                ChunkId = chunkId;
            }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"\t\tCodeOffset: {CodeOffset}");
                sb.AppendLine($"\t\tSlotId: {SlotId}");
                sb.AppendLine($"\t\tIsLive: {IsLive}");
                sb.AppendLine($"\t\tChunkId: {ChunkId}");
                sb.Append($"\t\t--------------------");

                return sb.ToString();
            }
            public string GetSlotState(GcSlotTable slotTable)
            {
                GcSlotTable.GcSlot slot = slotTable.GcSlots[SlotId];
                string slotStr = "";
                if (slot.StackSlot == null)
                {
                    slotStr = Enum.GetName(typeof(Amd64Registers), slot.RegisterNumber);
                }
                else
                {
                    slotStr = $"sp{slot.StackSlot.SpOffset:+#;-#;+0}";
                }
                string isLiveStr = "live";
                if (!IsLive)
                    isLiveStr = "dead";
                return $"{slotStr} is {isLiveStr}";
            }
        }

        public struct SafePointOffset
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            public uint Value { get; set; }
            public SafePointOffset(int index, uint value)
            {
                Index = index;
                Value = value;
            }
        }

        private const int GCINFO_VERSION = 2;
        private const int MIN_GCINFO_VERSION_WITH_RETURN_KIND = 2;
        private const int MIN_GCINFO_VERSION_WITH_REV_PINVOKE_FRAME = 2;

        private bool _slimHeader;
        private bool _hasSecurityObject;
        private bool _hasGSCookie;
        private bool _hasPSPSym;
        private bool _hasGenericsInstContext;
        private bool _hasStackBaseRegister;
        private bool _hasSizeOfEditAndContinuePreservedArea;
        private bool _hasReversePInvokeFrame;
        private bool _wantsReportOnlyLeaf;

        private Machine _machine;
        private GcInfoTypes _gcInfoTypes;

        public int Version { get; set; }
        public int CodeLength { get; set; }
        public ReturnKinds ReturnKind { get; set; }
        public uint ValidRangeStart { get; set; }
        public uint ValidRangeEnd { get; set; }
        public int SecurityObjectStackSlot { get; set; }
        public int GSCookieStackSlot { get; set; }
        public int PSPSymStackSlot { get; set; }
        public int GenericsInstContextStackSlot { get; set; }
        public uint StackBaseRegister { get; set; }
        public uint SizeOfEditAndContinuePreservedArea { get; set; }
        public int ReversePInvokeFrameStackSlot { get; set; }
        public uint SizeOfStackOutgoingAndScratchArea { get; set; }
        public uint NumSafePoints { get; set; }
        public uint NumInterruptibleRanges { get; set; }
        public List<SafePointOffset> SafePointOffsets { get; set; }
        public List<InterruptibleRange> InterruptibleRanges { get; set; }
        public GcSlotTable SlotTable { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; }

        [XmlIgnore]
        public Dictionary<int, GcTransition> Transitions { get; set; }

        public GcInfo() { }

        public GcInfo(byte[] image, int offset, Machine machine, ushort majorVersion)
        {
            Offset = offset;
            _gcInfoTypes = new GcInfoTypes(machine);

            SecurityObjectStackSlot = -1;
            GSCookieStackSlot = -1;
            PSPSymStackSlot = -1;
            SecurityObjectStackSlot = -1;
            GenericsInstContextStackSlot = -1;
            StackBaseRegister = 0xffffffff;
            SizeOfEditAndContinuePreservedArea = 0xffffffff;
            ReversePInvokeFrameStackSlot = -1;

            Version = ReadyToRunVersionToGcInfoVersion(majorVersion);
            int bitOffset = offset * 8;
            int startBitOffset = bitOffset;
            
            ParseHeaderFlags(image, ref bitOffset);

            if (Version >= MIN_GCINFO_VERSION_WITH_RETURN_KIND) // IsReturnKindAvailable
            {
                int returnKindBits = (_slimHeader) ? _gcInfoTypes.SIZE_OF_RETURN_KIND_SLIM : _gcInfoTypes.SIZE_OF_RETURN_KIND_FAT;
                ReturnKind = (ReturnKinds)NativeReader.ReadBits(image, returnKindBits, ref bitOffset);
            }

            CodeLength = _gcInfoTypes.DenormalizeCodeLength((int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.CODE_LENGTH_ENCBASE, ref bitOffset));

            if (_hasGSCookie)
            {
                uint normPrologSize = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
                uint normEpilogSize = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset);

                ValidRangeStart = normPrologSize;
                ValidRangeEnd = (uint)CodeLength - normEpilogSize;
            }
            else if (_hasSecurityObject || _hasGenericsInstContext)
            {
                ValidRangeStart = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
                ValidRangeEnd = ValidRangeStart + 1;
            }

            if (_hasSecurityObject)
            {
                SecurityObjectStackSlot = _gcInfoTypes.DenormalizeStackSlot(NativeReader.DecodeVarLengthSigned(image, _gcInfoTypes.SECURITY_OBJECT_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if (_hasGSCookie)
            {
                GSCookieStackSlot = _gcInfoTypes.DenormalizeStackSlot(NativeReader.DecodeVarLengthSigned(image, _gcInfoTypes.GS_COOKIE_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if (_hasPSPSym)
            {
                PSPSymStackSlot = _gcInfoTypes.DenormalizeStackSlot(NativeReader.DecodeVarLengthSigned(image, _gcInfoTypes.PSP_SYM_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if (_hasGenericsInstContext)
            {
                GenericsInstContextStackSlot = _gcInfoTypes.DenormalizeStackSlot(NativeReader.DecodeVarLengthSigned(image, _gcInfoTypes.GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if (_hasStackBaseRegister && !_slimHeader)
            {
                StackBaseRegister = _gcInfoTypes.DenormalizeStackBaseRegister(NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.STACK_BASE_REGISTER_ENCBASE, ref bitOffset));
            }

            if (_hasSizeOfEditAndContinuePreservedArea)
            {
                SizeOfEditAndContinuePreservedArea = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, ref bitOffset);
            }

            if (_hasReversePInvokeFrame)
            {
                ReversePInvokeFrameStackSlot = NativeReader.DecodeVarLengthSigned(image, _gcInfoTypes.REVERSE_PINVOKE_FRAME_ENCBASE, ref bitOffset);
            }

            // FIXED_STACK_PARAMETER_SCRATCH_AREA (this macro is always defined in _gcInfoTypes.h)
            if (!_slimHeader)
            {
                SizeOfStackOutgoingAndScratchArea = _gcInfoTypes.DenormalizeSizeOfStackArea(NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.SIZE_OF_STACK_AREA_ENCBASE, ref bitOffset));
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED (this macro is always defined in _gcInfoTypes.h)
            NumSafePoints = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.NUM_SAFE_POINTS_ENCBASE, ref bitOffset);

            if (!_slimHeader)
            {
                NumInterruptibleRanges = NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.NUM_INTERRUPTIBLE_RANGES_ENCBASE, ref bitOffset);
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED (this macro is always defined in _gcInfoTypes.h)
            SafePointOffsets = EnumerateSafePoints(image, ref bitOffset);
            uint numBitsPerOffset = GcInfoTypes.CeilOfLog2(CodeLength);
            bitOffset += (int)(NumSafePoints * numBitsPerOffset);

            InterruptibleRanges = EnumerateInterruptibleRanges(image, _gcInfoTypes.INTERRUPTIBLE_RANGE_DELTA1_ENCBASE, _gcInfoTypes.INTERRUPTIBLE_RANGE_DELTA2_ENCBASE, ref bitOffset);

            SlotTable = new GcSlotTable(image, machine, _gcInfoTypes, ref bitOffset);

            Transitions = GetTranstions(image, ref bitOffset);

            Size = bitOffset - startBitOffset;

            _machine = machine;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\tVersion: {Version}");
            sb.AppendLine($"\tCodeLength: {CodeLength}");
            sb.AppendLine($"\tReturnKind: {Enum.GetName(typeof(ReturnKinds), ReturnKind)}");
            sb.AppendLine($"\tValidRangeStart: {ValidRangeStart}");
            sb.AppendLine($"\tValidRangeEnd: {ValidRangeEnd}");
            if (SecurityObjectStackSlot != -1)
                sb.AppendLine($"\tSecurityObjectStackSlot: caller.sp{SecurityObjectStackSlot:+#;-#;+0}");

            if (GSCookieStackSlot != -1)
            {
                sb.AppendLine($"\tGSCookieStackSlot: caller.sp{GSCookieStackSlot:+#;-#;+0}");
                sb.AppendLine($"GS cookie valid range: [{ValidRangeStart};{ValidRangeEnd})");
            }

            if (PSPSymStackSlot != -1)
            {
                if (_machine == Machine.Amd64)
                {
                    sb.AppendLine($"\tPSPSymStackSlot: initial.sp{PSPSymStackSlot:+#;-#;+0}");
                }
                else
                {
                    sb.AppendLine($"\tPSPSymStackSlot: caller.sp{PSPSymStackSlot:+#;-#;+0}");
                }
            }

            if (GenericsInstContextStackSlot != -1)
            {
                sb.AppendLine($"\tGenericsInstContextStackSlot: caller.sp{GenericsInstContextStackSlot:+#;-#;+0}");
            }

            if (StackBaseRegister != 0xffffffff)
                sb.AppendLine($"\tStackBaseRegister: {(Amd64Registers)StackBaseRegister}");
            if (_machine == Machine.Amd64)
            {
                sb.AppendLine($"\tWants Report Only Leaf: {_wantsReportOnlyLeaf}");
            }
            else if (_machine == Machine.Arm || _machine == Machine.Arm64)
            {
                sb.AppendLine($"\tHas Tailcalls: {_wantsReportOnlyLeaf}");
            }

            sb.AppendLine($"\tSize of parameter area: 0x{SizeOfStackOutgoingAndScratchArea:X}");
            if (SizeOfEditAndContinuePreservedArea != 0xffffffff)
                sb.AppendLine($"\tSizeOfEditAndContinuePreservedArea: 0x{SizeOfEditAndContinuePreservedArea:X}");
            if (ReversePInvokeFrameStackSlot != -1)
                sb.AppendLine($"\tReversePInvokeFrameStackSlot: {ReversePInvokeFrameStackSlot}");
            sb.AppendLine($"\tNumSafePoints: {NumSafePoints}");
            sb.AppendLine($"\tNumInterruptibleRanges: {NumInterruptibleRanges}");
            sb.AppendLine($"\tSafePointOffsets:");
            foreach (SafePointOffset offset in SafePointOffsets)
            {
                sb.AppendLine($"\t\t{offset.Value}");
            }
            sb.AppendLine($"\tInterruptibleRanges:");
            foreach (InterruptibleRange range in InterruptibleRanges)
            {
                sb.AppendLine($"\t\tstart:{range.StartOffset}, end:{range.StopOffset}");
            }
            sb.AppendLine($"\tSlotTable:");
            sb.Append(SlotTable.ToString());
            sb.AppendLine($"\tTransitions:");
            foreach (GcTransition trans in Transitions.Values)
            {
                sb.AppendLine(trans.ToString());
            }
            sb.AppendLine($"\tSize: {Size} bytes");

            return sb.ToString();
        }

        private void ParseHeaderFlags(byte[] image, ref int bitOffset)
        {
            GcInfoHeaderFlags headerFlags;
            _slimHeader = (NativeReader.ReadBits(image, 1, ref bitOffset) == 0);
            if (_slimHeader)
            {
                headerFlags = NativeReader.ReadBits(image, 1, ref bitOffset) == 1 ? GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER : 0;
            }
            else
            {
                int numFlagBits = (int)((Version == 1) ? GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE_VERSION_1 : GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE);
                headerFlags = (GcInfoHeaderFlags)NativeReader.ReadBits(image, numFlagBits, ref bitOffset);
            }

            _hasSecurityObject = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_SECURITY_OBJECT) != 0;
            _hasGSCookie = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) != 0;
            _hasPSPSym = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_PSP_SYM) != 0;
            _hasGenericsInstContext = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE;
            _hasStackBaseRegister = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER) != 0;
            _hasSizeOfEditAndContinuePreservedArea = (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS) != 0;
            if (Version >= MIN_GCINFO_VERSION_WITH_REV_PINVOKE_FRAME) // IsReversePInvokeFrameAvailable
            {
                _hasReversePInvokeFrame = (headerFlags & GcInfoHeaderFlags.GC_INFO_REVERSE_PINVOKE_FRAME) != 0;
            }
            _wantsReportOnlyLeaf = ((headerFlags & GcInfoHeaderFlags.GC_INFO_WANTS_REPORT_ONLY_LEAF) != 0);
        }

        private List<SafePointOffset> EnumerateSafePoints(byte[] image, ref int bitOffset)
        {
            List<SafePointOffset> safePoints = new List<SafePointOffset>();
            uint numBitsPerOffset = GcInfoTypes.CeilOfLog2(CodeLength);
            for (int i = 0; i < NumSafePoints; i++)
            {
                uint normOffset = (uint)NativeReader.ReadBits(image, (int)numBitsPerOffset, ref bitOffset);
                safePoints.Add(new SafePointOffset(i, normOffset));
            }
            return safePoints;
        }

        private List<InterruptibleRange> EnumerateInterruptibleRanges(byte[] image, int interruptibleRangeDelta1EncBase, int interruptibleRangeDelta2EncBase, ref int bitOffset)
        {
            List<InterruptibleRange> ranges = new List<InterruptibleRange>();
            uint lastinterruptibleRangeStopOffset = 0;

            for (uint i = 0; i < NumInterruptibleRanges; i++)
            {
                uint normStartDelta = NativeReader.DecodeVarLengthUnsigned(image, interruptibleRangeDelta1EncBase, ref bitOffset);
                uint normStopDelta = NativeReader.DecodeVarLengthUnsigned(image, interruptibleRangeDelta2EncBase, ref bitOffset) + 1;

                uint rangeStartOffset = lastinterruptibleRangeStopOffset + normStartDelta;
                uint rangeStopOffset = rangeStartOffset + normStopDelta;
                ranges.Add(new InterruptibleRange(i, rangeStartOffset, rangeStopOffset));

                lastinterruptibleRangeStopOffset = rangeStopOffset;
            }
            return ranges;
        }

        /// <summary>
        /// GcInfo version is 1 up to ReadyTorun version 1.x. 
        /// GcInfo version is current from  ReadyToRun version 2.0
        /// </summary>
        private int ReadyToRunVersionToGcInfoVersion(int readyToRunMajorVersion)
        {
            return (readyToRunMajorVersion == 1) ? 1 : GCINFO_VERSION;
        }

        public Dictionary<int, GcTransition> GetTranstions(byte[] image, ref int bitOffset)
        {
            int totalInterruptibleLength = 0;
            if (NumInterruptibleRanges == 0)
            {
                totalInterruptibleLength = CodeLength;
            }
            else
            {
                foreach (InterruptibleRange range in InterruptibleRanges)
                {
                    totalInterruptibleLength += (int)(range.StopOffset - range.StartOffset);
                }
            }

            int numChunks = (totalInterruptibleLength + _gcInfoTypes.NUM_NORM_CODE_OFFSETS_PER_CHUNK - 1) / _gcInfoTypes.NUM_NORM_CODE_OFFSETS_PER_CHUNK; //=2
            int numBitsPerPointer = (int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.POINTER_SIZE_ENCBASE, ref bitOffset);
            if (numBitsPerPointer == 0)
            {
                return new Dictionary<int, GcTransition>();
            }

            int[] chunkPointers = new int[numChunks];
            for (int i = 0; i < numChunks; i++)
            {
                chunkPointers[i] = NativeReader.ReadBits(image, numBitsPerPointer, ref bitOffset);
            }
            int info2Offset = (int)Math.Ceiling(bitOffset / 8.0) * 8;

            List<GcTransition> transitions = new List<GcTransition>();
            bool[] liveAtEnd = new bool[SlotTable.GcSlots.Count - SlotTable.NumUntracked];
            for (int currentChunk = 0; currentChunk < numChunks; currentChunk++)
            {
                if (chunkPointers[currentChunk] == 0)
                {
                    continue;
                }
                else
                {
                    bitOffset = info2Offset + chunkPointers[currentChunk] - 1;
                }

                int couldBeLiveOffset = bitOffset;
                int slotId = 0;
                bool fSimple = (NativeReader.ReadBits(image, 1, ref couldBeLiveOffset) == 0);
                bool fSkipFirst = false;
                int couldBeLiveCnt = 0;
                if (!fSimple)
                {
                    fSkipFirst = (NativeReader.ReadBits(image, 1, ref couldBeLiveOffset) == 0);
                    slotId = -1;
                }

                uint numCouldBeLiveSlots = GetNumCouldBeLiveSlots(image, ref bitOffset);

                int finalStateOffset = bitOffset;
                bitOffset += (int)numCouldBeLiveSlots;

                int normChunkBaseCodeOffset = currentChunk * _gcInfoTypes.NUM_NORM_CODE_OFFSETS_PER_CHUNK;
                for (int i = 0; i < numCouldBeLiveSlots; i++)
                {
                    slotId = GetNextSlotId(image, fSimple, fSkipFirst, slotId, ref couldBeLiveCnt, ref couldBeLiveOffset);

                    bool isLive = !liveAtEnd[slotId];
                    liveAtEnd[slotId] = (NativeReader.ReadBits(image, 1, ref finalStateOffset) != 0);

                    // Read transitions
                    while (NativeReader.ReadBits(image, 1, ref bitOffset) != 0)
                    {
                        int transitionOffset = NativeReader.ReadBits(image, _gcInfoTypes.NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2, ref bitOffset) + normChunkBaseCodeOffset;
                        transitions.Add(new GcTransition(transitionOffset, slotId, isLive, currentChunk));
                        isLive = !isLive;
                    }
                    slotId++;
                }
            }

            transitions.Sort((s1, s2) => s1.CodeOffset.CompareTo(s2.CodeOffset));

            return UpdateTransitionCodeOffset(transitions);
        }

        private uint GetNumCouldBeLiveSlots(byte[] image, ref int bitOffset)
        {
            uint numCouldBeLiveSlots = 0;
            int numTracked = SlotTable.GcSlots.Count - (int)SlotTable.NumUntracked;
            if (NativeReader.ReadBits(image, 1, ref bitOffset) != 0)
            {
                // RLE encoded
                bool fSkip = (NativeReader.ReadBits(image, 1, ref bitOffset) == 0);
                bool fReport = true;
                uint readSlots = NativeReader.DecodeVarLengthUnsigned(image, fSkip ? _gcInfoTypes.LIVESTATE_RLE_SKIP_ENCBASE : _gcInfoTypes.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset);
                fSkip = !fSkip;
                while (readSlots < numTracked)
                {
                    uint cnt = NativeReader.DecodeVarLengthUnsigned(image, fSkip ? _gcInfoTypes.LIVESTATE_RLE_SKIP_ENCBASE : _gcInfoTypes.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset) + 1;
                    if (fReport)
                    {
                        numCouldBeLiveSlots += cnt;
                    }
                    readSlots += cnt;
                    fSkip = !fSkip;
                    fReport = !fReport;
                }
            }
            else
            {
                foreach (var slot in SlotTable.GcSlots)
                {
                    if (slot.Flags == GcSlotFlags.GC_SLOT_UNTRACKED)
                        break;

                    if (NativeReader.ReadBits(image, 1, ref bitOffset) != 0)
                        numCouldBeLiveSlots++;
                }
            }
            return numCouldBeLiveSlots;
        }

        private int GetNextSlotId(byte[] image, bool fSimple, bool fSkipFirst, int slotId, ref int couldBeLiveCnt, ref int couldBeLiveOffset)
        {
            if (fSimple)
            {
                while (NativeReader.ReadBits(image, 1, ref couldBeLiveOffset) == 0)
                    slotId++;
            }
            else if (couldBeLiveCnt > 0)
            {
                // We have more from the last run to report
                couldBeLiveCnt--;
            }
            // We need to find a new run
            else if (fSkipFirst)
            {
                int tmp = (int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.LIVESTATE_RLE_SKIP_ENCBASE, ref couldBeLiveOffset) + 1;
                slotId += tmp;
                couldBeLiveCnt = (int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.LIVESTATE_RLE_RUN_ENCBASE, ref couldBeLiveOffset);
            }
            else
            {
                int tmp = (int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.LIVESTATE_RLE_RUN_ENCBASE, ref couldBeLiveOffset) + 1;
                slotId += tmp;
                couldBeLiveCnt = (int)NativeReader.DecodeVarLengthUnsigned(image, _gcInfoTypes.LIVESTATE_RLE_SKIP_ENCBASE, ref couldBeLiveOffset);
            }
            return slotId;
        }

        private Dictionary<int, GcTransition> UpdateTransitionCodeOffset(List<GcTransition> transitions)
        {
            Dictionary<int, GcTransition> updatedTransitions = new Dictionary<int, GcTransition>();
            int cumInterruptibleLength = 0;
            using (IEnumerator<InterruptibleRange> interruptibleRangesIter = InterruptibleRanges.GetEnumerator())
            {
                interruptibleRangesIter.MoveNext();
                InterruptibleRange currentRange = interruptibleRangesIter.Current;
                int currentRangeLength = (int)(currentRange.StopOffset - currentRange.StartOffset);
                foreach (GcTransition transition in transitions)
                {
                    int codeOffset = transition.CodeOffset + (int)currentRange.StartOffset - cumInterruptibleLength;
                    if (codeOffset > currentRange.StopOffset)
                    {
                        cumInterruptibleLength += currentRangeLength;
                        interruptibleRangesIter.MoveNext();
                        currentRange = interruptibleRangesIter.Current;
                        currentRangeLength = (int)(currentRange.StopOffset - currentRange.StartOffset);
                        codeOffset = transition.CodeOffset + (int)currentRange.StartOffset - cumInterruptibleLength;
                    }
                    transition.CodeOffset = codeOffset;
                    updatedTransitions[codeOffset] = transition;
                }
            }
            return updatedTransitions;
        }
    }
}
