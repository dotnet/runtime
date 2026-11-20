// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86;
using CdacX86Transition = Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86.BaseGcTransition;

namespace ILCompiler.Reflection.ReadyToRun.Cdac
{
    /// <summary>
    /// x86 GC info (version 4 and later), decoded with the decoder shared with the cDAC.
    /// </summary>
    public sealed class CdacX86GcInfo : BaseGcInfo
    {
        private const uint byref_OFFSET_FLAG = 0x1;
        private const uint pinned_OFFSET_FLAG = 0x2;

        private readonly X86GCInfo _gcInfo;

        internal CdacX86GcInfo(ImageTarget target, int offset, int version)
        {
            _gcInfo = new X86GCInfo(target, new TargetPointer((ulong)offset), (uint)version);

            Offset = offset;
            Version = version;
            CodeLength = (int)_gcInfo.MethodSize;
            Header = _gcInfo.Header;
            NoGCRegions = _gcInfo.NoGCRegions.Select(r => new CdacX86NoGCRegion(r.Offset, r.Size)).ToList();

            foreach (UntrackedSlot slot in _gcInfo.UntrackedSlots)
            {
                Slots.Add(new CdacX86GcSlot(
                    beginOffset: null,
                    endOffset: null,
                    slot.IsEbpRelative ? "EBP" : "ESP",
                    slot.StackOffset,
                    slot.LowBits,
                    GetSlotFlags(slot.LowBits) | GcSlotFlags.GC_SLOT_UNTRACKED));
            }

            foreach (VarPtrLifetime lifetime in _gcInfo.VarPtrLifetimes)
            {
                Slots.Add(new CdacX86GcSlot(
                    lifetime.BeginOffset,
                    lifetime.EndOffset,
                    Header.EbpFrame ? "EBP" : "ESP",
                    lifetime.StackOffset,
                    lifetime.LowBits,
                    GetSlotFlags(lifetime.LowBits)));
            }

            Transitions = new Dictionary<int, List<BaseGcTransition>>();
            foreach (KeyValuePair<int, List<CdacX86Transition>> entry in _gcInfo.Transitions.OrderBy(e => e.Key))
            {
                Transitions.Add(entry.Key, entry.Value.Select(t => (BaseGcTransition)new CdacX86GcTransition(t)).ToList());
            }

            // The shared decoders read the encoding a byte at a time; once every table has been
            // decoded the furthest byte read is the end of the GC info.
            Size = (int)(target.ReadEnd - (ulong)offset);
        }

        public int Version { get; }
        public InfoHdr Header { get; }
        public List<CdacX86NoGCRegion> NoGCRegions { get; }

        /// <summary>
        /// Untracked (always live) frame slots followed by tracked (VarPtr) frame slots.
        /// </summary>
        public List<CdacX86GcSlot> Slots { get; } = new List<CdacX86GcSlot>();

        private static GcSlotFlags GetSlotFlags(uint lowBits)
        {
            GcSlotFlags flags = GcSlotFlags.GC_SLOT_BASE;
            if ((lowBits & byref_OFFSET_FLAG) != 0)
                flags |= GcSlotFlags.GC_SLOT_INTERIOR;
            if ((lowBits & pinned_OFFSET_FLAG) != 0)
                flags |= GcSlotFlags.GC_SLOT_PINNED;
            return flags;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    CodeLength: {CodeLength} bytes");
            sb.AppendLine($"    InfoHdr:");
            AppendHeader(sb);

            if (NoGCRegions.Count > 0)
            {
                sb.AppendLine($"        No GC regions:");
                foreach (CdacX86NoGCRegion region in NoGCRegions)
                {
                    sb.AppendLine($"            {region}");
                }
            }

            sb.AppendLine($"        GcSlots:");
            sb.AppendLine($"            -------------------------");
            foreach (CdacX86GcSlot slot in Slots)
            {
                sb.Append(slot.ToString());
                sb.AppendLine($"            -------------------------");
            }

            sb.AppendLine($"    Size: {Size} bytes");

            return sb.ToString();
        }

        private void AppendHeader(StringBuilder sb)
        {
            InfoHdr header = Header;
            sb.AppendLine($"        PrologSize: {header.PrologSize}");
            sb.AppendLine($"        EpilogSize: {header.EpilogSize}");
            sb.AppendLine($"        EpilogCount: {header.EpilogCount}");
            sb.AppendLine($"        EpilogAtEnd: {YesNo(header.EpilogAtEnd)}");

            sb.Append($"        Callee-saved regs  =");
            if (header.EdiSaved) sb.Append(" EDI");
            if (header.EsiSaved) sb.Append(" ESI");
            if (header.EbxSaved) sb.Append(" EBX");
            if (header.EbpSaved) sb.Append(" EBP");
            sb.AppendLine();

            sb.AppendLine($"        EbpFrame: {YesNo(header.EbpFrame)}");
            sb.AppendLine($"        Fully Interruptible: {YesNo(header.Interruptible)}");
            sb.AppendLine($"        DoubleAlign: {YesNo(header.DoubleAlign)}");
            sb.AppendLine($"        Arguments Size: {header.ArgCount} DWORDs");
            sb.AppendLine($"        Stack Frame Size: {header.FrameSize} DWORDs");
            sb.AppendLine($"        UntrackedCnt: {header.UntrackedCount}");
            sb.AppendLine($"        VarPtrTableSize: {header.VarPtrTableSize}");

            if (header.Security) sb.AppendLine($"        Security Check Obj: yes");
            if (header.Handlers) sb.AppendLine($"        Handlers: yes");
            if (header.LocalAlloc) sb.AppendLine($"        Localloc: yes");
            if (header.EditAndContinue) sb.AppendLine($"        EditNcontinue: yes");
            if (header.VarArgs) sb.AppendLine($"        Varargs: yes");
            if (header.ProfCallbacks) sb.AppendLine($"        ProfCallbacks: yes");
            if (header.IsAsync) sb.AppendLine($"        IsAsync: yes");

            sb.AppendLine($"        GenericsContext: {header.GenericsContext}");
            sb.AppendLine($"        GenericsContextIsMethodDesc: {header.GenericsContextIsMethodDesc}");
            sb.AppendLine($"        ReturnKind: {header.ReturnKind}");
            if (header.RevPInvokeOffset != InfoHdr.INVALID_REV_PINVOKE_OFFSET)
                sb.AppendLine($"        RevPInvokeOffset: {header.RevPInvokeOffset}");

            if (header.GsCookieOffset != 0)
                sb.AppendLine($"        GuardStack cookie = [{(header.EbpFrame ? "EBP-" : "ESP+")}{header.GsCookieOffset}]");
            if (header.SyncStartOffset != 0)
                sb.AppendLine($"        Sync region = [{header.SyncStartOffset},{header.SyndEndOffset}]");
            if (header.NoGCRegionCount > 0)
                sb.AppendLine($"        No GC region count = {header.NoGCRegionCount}");

            sb.Append($"        Epilogs:");
            foreach (int epilog in header.Epilogs)
            {
                sb.Append($" {epilog}");
            }
            sb.AppendLine();
            if (header.HasArgTabOffset)
                sb.AppendLine($"        ArgTabOffset: {header.ArgTabOffset}");
        }

        private static string YesNo(bool value) => value ? "yes" : "no";
    }

    /// <summary>
    /// An x86 region of code in which GC is not allowed, [Offset, Offset + Size).
    /// </summary>
    public readonly record struct CdacX86NoGCRegion(uint Offset, uint Size)
    {
        public override string ToString() => $"[{Offset:X4}-{Offset + Size:X4})";
    }

    /// <summary>
    /// An x86 frame slot: untracked slots are live for the whole method; tracked slots are live
    /// in [<see cref="BeginOffset"/>, <see cref="EndOffset"/>).
    /// </summary>
    public sealed class CdacX86GcSlot
    {
        internal CdacX86GcSlot(uint? beginOffset, uint? endOffset, string register, int stackOffset, uint lowBits, GcSlotFlags flags)
        {
            BeginOffset = beginOffset;
            EndOffset = endOffset;
            Register = register;
            StackOffset = stackOffset;
            LowBits = lowBits;
            Flags = flags;
        }

        public uint? BeginOffset { get; }
        public uint? EndOffset { get; }

        /// <summary>
        /// Base register of the slot: "EBP" or "ESP".
        /// </summary>
        public string Register { get; }

        /// <summary>
        /// Byte offset of the slot from <see cref="Register"/>.
        /// </summary>
        public int StackOffset { get; }
        public uint LowBits { get; }
        public GcSlotFlags Flags { get; }

        /// <summary>
        /// The slot location, e.g. "[EBP-8]" or "[ESP+4]".
        /// </summary>
        public string Location => StackOffset < 0 ? $"[{Register}-{-(long)StackOffset}]" : $"[{Register}+{StackOffset}]";

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (BeginOffset is uint beginOffset)
                sb.AppendLine($"            BeginOffset: {beginOffset}");
            if (EndOffset is uint endOffset)
                sb.AppendLine($"            EndOffset: {endOffset}");
            sb.AppendLine($"            {Location}");
            sb.AppendLine($"            Flags: {Flags}");
            sb.Append($"            LowBits: ");
            if ((Flags & GcSlotFlags.GC_SLOT_PINNED) != 0) sb.Append("pinned ");
            if ((Flags & GcSlotFlags.GC_SLOT_INTERIOR) != 0) sb.Append("byref ");
            sb.AppendLine();
            return sb.ToString();
        }
    }

    /// <summary>
    /// Wraps an x86 GC transition decoded by the shared decoder.
    /// </summary>
    public sealed class CdacX86GcTransition : BaseGcTransition
    {
        internal CdacX86GcTransition(CdacX86Transition transition)
            : base(transition.CodeOffset)
        {
            Transition = transition;
        }

        /// <summary>
        /// The decoded transition: one of the transition types in
        /// <c>Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86</c>.
        /// </summary>
        public CdacX86Transition Transition { get; }

        public override string ToString() => Transition.ToString();
    }
}
