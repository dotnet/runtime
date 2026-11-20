// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace ILCompiler.Reflection.ReadyToRun.Cdac
{
    /// <summary>
    /// GC info (version 4 and later) for the non-x86 GcInfoEncoder format, decoded with the
    /// decoder shared with the cDAC.
    /// </summary>
    public sealed class CdacGcInfo : BaseGcInfo
    {
        private readonly Machine _machine;

        public int Version { get; }
        public bool IsSlimHeader { get; private set; }
        public GCInfoHeader Header { get; private set; }
        public uint ValidRangeStart => Header.PrologSize;
        public uint ValidRangeEnd { get; private set; }
        public uint? StackBaseRegister { get; private set; }
        public uint? SizeOfEditAndContinuePreservedArea { get; private set; }
        public uint SizeOfEditAndContinueFixedStackFrame { get; private set; }

        /// <summary>
        /// Reverse P/Invoke frame stack slot as a byte offset, or null if the method has none.
        /// </summary>
        public int? ReversePInvokeFrameStackSlot { get; private set; }
        public IReadOnlyList<uint> SafePoints { get; private set; }
        public IReadOnlyList<Microsoft.Diagnostics.DataContractReader.Contracts.InterruptibleRange> InterruptibleRanges { get; private set; }
        public uint NumRegisters { get; private set; }
        public uint NumStackSlots { get; private set; }
        public uint NumUntrackedSlots { get; private set; }

        /// <summary>
        /// Slot table in encoded order: registers, tracked stack slots, then untracked stack slots.
        /// </summary>
        public List<CdacGcSlot> Slots { get; } = new List<CdacGcSlot>();

        /// <summary>
        /// Lifetimes of all slots. End offsets are exclusive. Untracked slots span the whole method.
        /// </summary>
        public IReadOnlyList<GCSlotLifetime> SlotLifetimes { get; private set; }

        private CdacGcInfo(Machine machine, int offset, int version)
        {
            _machine = machine;
            Offset = offset;
            Version = version;
        }

        /// <summary>
        /// Decodes GC info (version 4 or later) at <paramref name="offset"/> in <paramref name="imageReader"/>.
        /// Returns a <see cref="CdacX86GcInfo"/> for x86 and a <see cref="CdacGcInfo"/> otherwise.
        /// </summary>
        public static BaseGcInfo Decode(NativeReader imageReader, int offset, Machine machine, int version)
        {
            ImageTarget target = new ImageTarget(imageReader, machine);
            TargetPointer address = new TargetPointer((ulong)offset);
            uint gcVersion = (uint)version;
            return machine switch
            {
                Machine.I386 => new CdacX86GcInfo(target, offset, version),
                Machine.Amd64 => Create(new GcInfoDecoder<AMD64GCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                Machine.Arm64 => Create(new GcInfoDecoder<ARM64GCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                Machine.ArmThumb2 => Create(new GcInfoDecoder<ARMGCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                Machine.LoongArch64 => Create(new GcInfoDecoder<LoongArch64GCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                Machine.RiscV64 => Create(new GcInfoDecoder<RISCV64GCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                WasmMachine.Wasm32 => Create(new GcInfoDecoder<WasmGCInfoTraits>(target, address, gcVersion), target, machine, offset, version),
                _ => throw new NotImplementedException(machine.ToString()),
            };
        }

        private static CdacGcInfo Create<TTraits>(GcInfoDecoder<TTraits> decoder, ImageTarget target, Machine machine, int offset, int version)
            where TTraits : IGCInfoTraits
        {
            IGCInfoDecoder gcInfo = decoder;
            CdacGcInfo result = new CdacGcInfo(machine, offset, version)
            {
                Header = gcInfo.GetHeader(),
                IsSlimHeader = decoder.IsSlimHeader,
                ValidRangeEnd = decoder.ValidRangeEnd,
                StackBaseRegister = decoder.HasStackBaseRegister ? gcInfo.GetHeader().StackBaseRegister : null,
                SizeOfEditAndContinuePreservedArea = decoder.SizeOfEditAndContinuePreservedArea,
                SizeOfEditAndContinueFixedStackFrame = decoder.SizeOfEditAndContinueFixedStackFrame,
                ReversePInvokeFrameStackSlot = decoder.ReversePInvokeFrameStackSlot,
                SafePoints = gcInfo.GetSafePoints(),
                InterruptibleRanges = gcInfo.GetInterruptibleRanges(),
                NumRegisters = decoder.NumRegisters,
                NumUntrackedSlots = decoder.NumUntrackedSlots,
            };
            result.CodeLength = (int)result.Header.CodeSize;

            int numTracked = (int)decoder.NumTrackedSlots;
            IReadOnlyList<GcInfoDecoder<TTraits>.GcSlotDesc> slots = decoder.Slots;
            result.NumStackSlots = (uint)slots.Count - result.NumRegisters - result.NumUntrackedSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                GcInfoDecoder<TTraits>.GcSlotDesc slot = slots[i];
                result.Slots.Add(new CdacGcSlot(
                    i,
                    slot.IsRegister,
                    slot.RegisterNumber,
                    slot.SpOffset,
                    (GcStackSlotBase)slot.Base,
                    // The shared decoder does not record GC_SLOT_UNTRACKED on the untracked slots it decodes.
                    i >= numTracked ? (GcSlotFlags)slot.Flags | GcSlotFlags.GC_SLOT_UNTRACKED : (GcSlotFlags)slot.Flags,
                    result));
            }

            result.LiveSlotsAtSafepoints = new List<List<BaseGcSlot>>();
            GcSlotEnumerationOptions options = new GcSlotEnumerationOptions { IsActiveFrame = true, SuppressUntrackedSlots = true };
            foreach (uint safePoint in result.SafePoints)
            {
                List<BaseGcSlot> liveSlots = new List<BaseGcSlot>();
                foreach (LiveSlot liveSlot in gcInfo.EnumerateLiveSlots(safePoint, options))
                {
                    liveSlots.Add(result.FindTrackedSlot(liveSlot.IsRegister, liveSlot.RegisterNumber, liveSlot.SpOffset, liveSlot.SpBase, liveSlot.GcFlags, numTracked));
                }
                result.LiveSlotsAtSafepoints.Add(liveSlots);
            }

            result.SlotLifetimes = gcInfo.GetSlotLifetimes();
            result.Transitions = new Dictionary<int, List<BaseGcTransition>>();
            if (result.InterruptibleRanges.Count > 0)
            {
                // The untracked slots' lifetimes come first and span the whole method; they are not transitions.
                for (int i = (int)result.NumUntrackedSlots; i < result.SlotLifetimes.Count; i++)
                {
                    GCSlotLifetime lifetime = result.SlotLifetimes[i];
                    CdacGcSlot slot = result.FindTrackedSlot(lifetime.IsRegister, lifetime.RegisterNumber, lifetime.SpOffset, lifetime.BaseRegister, lifetime.GcFlags, numTracked);
                    result.AddTransition(new CdacGcTransition((int)lifetime.BeginOffset, slot, isLive: true));
                    if (lifetime.EndOffset < result.Header.CodeSize)
                        result.AddTransition(new CdacGcTransition((int)lifetime.EndOffset, slot, isLive: false));
                }
            }

            // The shared decoders read the encoding a byte at a time; after the full decode above
            // the furthest byte read is the end of the GC info.
            result.Size = (int)(target.ReadEnd - (ulong)offset);
            return result;
        }

        private CdacGcSlot FindTrackedSlot(bool isRegister, uint registerNumber, int spOffset, uint spBase, uint gcFlags, int numTracked)
        {
            // A location can appear in the slot table more than once with different flags (e.g. a register
            // holding an object reference and later an interior pointer), so the flags must match as well.
            const GcSlotFlags FlagsMask = GcSlotFlags.GC_SLOT_INTERIOR | GcSlotFlags.GC_SLOT_PINNED;
            for (int i = 0; i < numTracked; i++)
            {
                CdacGcSlot slot = Slots[i];
                if (slot.IsRegister != isRegister || (slot.Flags & FlagsMask) != ((GcSlotFlags)gcFlags & FlagsMask))
                    continue;
                if (isRegister ? slot.RegisterNumber == registerNumber : slot.StackSlot.SpOffset == spOffset && (uint)slot.StackSlot.Base == spBase)
                    return slot;
            }
            throw new BadImageFormatException($"GC info references a slot that is not in the slot table.");
        }

        private void AddTransition(CdacGcTransition transition)
        {
            if (!Transitions.TryGetValue(transition.CodeOffset, out List<BaseGcTransition> transitions))
            {
                transitions = new List<BaseGcTransition>();
                Transitions.Add(transition.CodeOffset, transitions);
            }
            transitions.Add(transition);
        }

        internal string GetRegisterName(uint registerNumber) => GetRegisterName(_machine, registerNumber);

        internal static string GetRegisterName(Machine machine, uint registerNumber) => machine switch
        {
            Machine.Amd64 => ((Amd64.Registers)registerNumber).ToString(),
            Machine.I386 => ((x86.Registers)registerNumber).ToString(),
            Machine.ArmThumb2 => ((Arm.Registers)registerNumber).ToString(),
            Machine.Arm64 => ((Arm64.Registers)registerNumber).ToString(),
            Machine.LoongArch64 => ((LoongArch64.Registers)registerNumber).ToString(),
            Machine.RiscV64 => ((RiscV64.Registers)registerNumber).ToString(),
            _ => $"r{registerNumber}",
        };

        /// <summary>
        /// Name of the base of a stack slot, e.g. "caller.sp", "sp" or the frame register name.
        /// </summary>
        internal string GetStackBaseName(GcStackSlotBase stackBase) => stackBase switch
        {
            GcStackSlotBase.GC_CALLER_SP_REL => "caller.sp",
            GcStackSlotBase.GC_SP_REL => "sp",
            _ => StackBaseRegister is uint register ? GetRegisterName(register).ToLowerInvariant() : "frame",
        };

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    Version: {Version}");
            sb.AppendLine($"    CodeLength: {CodeLength}");
            sb.AppendLine($"    ValidRangeStart: {ValidRangeStart}");
            sb.AppendLine($"    ValidRangeEnd: {ValidRangeEnd}");

            if (Header.GSCookie is SpecialSlot gsCookie)
            {
                sb.AppendLine($"    GSCookieStackSlot: caller.sp{gsCookie.SpOffset:+#;-#;+0}");
                sb.AppendLine($"    GS cookie valid range: [{Header.GSCookieValidRangeStart};{Header.GSCookieValidRangeEnd})");
            }

            if (Header.GenericsInstContext is SpecialSlot genericsContext)
            {
                sb.AppendLine($"    GenericsInstContextStackSlot: caller.sp{genericsContext.SpOffset:+#;-#;+0}");
                sb.AppendLine($"    GenericsInstContextKind: {Header.GenericsInstContextKind}");
            }

            if (StackBaseRegister is uint stackBaseRegister)
                sb.AppendLine($"    StackBaseRegister: {GetRegisterName(stackBaseRegister)}");

            if (_machine == Machine.Amd64)
                sb.AppendLine($"    Wants Report Only Leaf: {Header.WantsReportOnlyLeaf}");
            else if (_machine is Machine.ArmThumb2 or Machine.Arm64 or Machine.LoongArch64 or Machine.RiscV64)
                sb.AppendLine($"    Has Tailcalls: {Header.HasTailCalls}");

            if (Header.IsVarArg)
                sb.AppendLine($"    IsVarArg: {Header.IsVarArg}");

            sb.AppendLine($"    Size of parameter area: 0x{Header.SizeOfStackParameterArea:X}");
            if (SizeOfEditAndContinuePreservedArea is uint encPreservedArea)
            {
                sb.AppendLine($"    SizeOfEditAndContinuePreservedArea: 0x{encPreservedArea:X}");
                if (_machine == Machine.Arm64)
                    sb.AppendLine($"    SizeOfEditAndContinueFixedStackFrame: 0x{SizeOfEditAndContinueFixedStackFrame:X}");
            }
            if (ReversePInvokeFrameStackSlot is int reversePInvokeFrameStackSlot)
                sb.AppendLine($"    ReversePInvokeFrameStackSlot: {reversePInvokeFrameStackSlot}");
            sb.AppendLine($"    NumSafePoints: {SafePoints.Count}");
            sb.AppendLine($"    NumInterruptibleRanges: {InterruptibleRanges.Count}");
            sb.AppendLine($"    SafePointOffsets:");
            for (int i = 0; i < SafePoints.Count; i++)
            {
                sb.Append($"        0x{SafePoints[i]:X4}: ");
                List<BaseGcSlot> liveSlots = LiveSlotsAtSafepoints[i];
                if (liveSlots.Count == 0)
                {
                    sb.Append("no live slots");
                }
                else
                {
                    GcSlotFlags slotFlags = GcSlotFlags.GC_SLOT_INVALID;
                    for (int j = 0; j < liveSlots.Count; j++)
                    {
                        if (j != 0)
                            sb.Append("; ");
                        slotFlags = liveSlots[j].WriteTo(sb, _machine, slotFlags);
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine($"    InterruptibleRanges:");
            foreach (Microsoft.Diagnostics.DataContractReader.Contracts.InterruptibleRange range in InterruptibleRanges)
            {
                sb.AppendLine($"        start:{range.StartOffset}, end:{range.EndOffset}");
            }
            sb.AppendLine($"    SlotTable:");
            sb.AppendLine($"        NumSlots({Slots.Count}) = NumRegisters({NumRegisters}) + NumStackSlots({NumStackSlots}) + NumUntracked({NumUntrackedSlots})");
            sb.AppendLine($"        GcSlots:");
            foreach (CdacGcSlot slot in Slots)
            {
                sb.Append(' ', 12);
                slot.WriteTo(sb, _machine, GcSlotFlags.GC_SLOT_INVALID);
                sb.AppendLine();
            }
            sb.AppendLine($"    Size: {Size} bytes");

            return sb.ToString();
        }
    }

    /// <summary>
    /// An entry of the GC slot table decoded by <see cref="CdacGcInfo"/>.
    /// </summary>
    public sealed class CdacGcSlot : BaseGcSlot
    {
        private readonly CdacGcInfo _gcInfo;

        internal CdacGcSlot(int index, bool isRegister, uint registerNumber, int spOffset, GcStackSlotBase stackBase, GcSlotFlags flags, CdacGcInfo gcInfo)
        {
            Index = index;
            IsRegister = isRegister;
            RegisterNumber = registerNumber;
            StackSlot = isRegister ? null : new GcStackSlot(spOffset, stackBase);
            Flags = flags;
            _gcInfo = gcInfo;
        }

        public int Index { get; }
        public bool IsRegister { get; }
        public uint RegisterNumber { get; }

        /// <summary>
        /// Stack location of the slot, or null for register slots.
        /// </summary>
        public GcStackSlot StackSlot { get; }
        public GcSlotFlags Flags { get; }

        /// <summary>
        /// The slot location, e.g. "RAX", "caller.sp-16", "sp+8" or "rbp-8".
        /// </summary>
        public string Location => IsRegister
            ? _gcInfo.GetRegisterName(RegisterNumber)
            : $"{_gcInfo.GetStackBaseName(StackSlot.Base)}{StackSlot.SpOffset:+#;-#;+0}";

        public override GcSlotFlags WriteTo(StringBuilder sb, Machine machine, GcSlotFlags prevFlags)
        {
            if (prevFlags != Flags)
            {
                sb.Append(Flags.ToString());
                sb.Append(' ');
            }

            sb.Append(IsRegister ? CdacGcInfo.GetRegisterName(machine, RegisterNumber) : StackSlot.ToString());
            return Flags;
        }

        public override string ToString() => Location;
    }

    /// <summary>
    /// A tracked slot becoming live or dead in fully interruptible code.
    /// </summary>
    public sealed class CdacGcTransition : BaseGcTransition
    {
        internal CdacGcTransition(int codeOffset, CdacGcSlot slot, bool isLive)
            : base(codeOffset)
        {
            Slot = slot;
            IsLive = isLive;
        }

        public CdacGcSlot Slot { get; }
        public bool IsLive { get; }

        public override string ToString() => $"{Slot.Location} is {(IsLive ? "live" : "dead")}";
    }
}
