// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86;

[Flags]
public enum RegMask
{
    EAX = 0x1,
    ECX = 0x2,
    EDX = 0x4,
    EBX = 0x8,
    ESP = 0x10,
    EBP = 0x20,
    ESI = 0x40,
    EDI = 0x80,

    NONE = 0x00,
    RM_ALL = EAX | ECX | EDX | EBX | ESP | EBP | ESI | EDI,
    RM_CALLEE_SAVED = EBP | EBX | ESI | EDI,
    RM_CALLEE_TRASHED = RM_ALL & ~RM_CALLEE_SAVED,
}

public record X86GCInfo : IGCInfoDecoder
{
    private const uint MINIMUM_SUPPORTED_GCINFO_VERSION = 4;
    private const uint MAXIMUM_SUPPORTED_GCINFO_VERSION = 5;

    private readonly Target _target;

    private readonly TargetPointer _gcInfoAddress;
    private readonly uint _infoHdrSize;

    public uint RelativeOffset { get; set; }
    public uint MethodSize { get; set; }
    public InfoHdr Header { get; set; }

    public bool IsInProlog => PrologOffset != unchecked((uint)-1);
    public uint PrologOffset { get; set; } = unchecked((uint)-1);

    public bool IsInEpilog => EpilogOffset != unchecked((uint)-1);
    public uint EpilogOffset { get; set; } = unchecked((uint)-1);

    public uint RawStackSize { get; set; }


    /// <summary>
    /// Count of the callee-saved registers, excluding the frame pointer.
    /// This does not include EBP for EBP-frames and double-aligned-frames.
    /// </summary>
    public uint SavedRegsCountExclFP { get; set; }
    public RegMask SavedRegsMask { get; set; } = RegMask.NONE;

    /// <summary>
    /// GC Transitions indexed by their relative offset.
    /// </summary>
    public ImmutableDictionary<int, List<BaseGcTransition>> Transitions => _transitions.Value;
    private readonly Lazy<ImmutableDictionary<int, List<BaseGcTransition>>> _transitions = new();

    /// <summary>
    /// Number of bytes of stack space that has been pushed for arguments at the current RelativeOffset.
    /// </summary>
    public uint PushedArgSize => _pushedArgSize.Value;
    private readonly Lazy<uint> _pushedArgSize;

    /// <summary>
    /// The untracked frame variable table, always-live GC frame slots.
    /// Decoded lazily on first access.
    /// </summary>
    public ImmutableArray<UntrackedSlot> UntrackedSlots => _untrackedSlots.Value;
    private readonly Lazy<ImmutableArray<UntrackedSlot>> _untrackedSlots;

    /// <summary>
    /// The frame variable lifetime (VarPtr) table, per-offset-range tracked GC variables.
    /// Decoded lazily on first access. Empty for non-EBP frames (only EBP frames track variables this way).
    /// </summary>
    public ImmutableArray<VarPtrLifetime> VarPtrLifetimes => _varPtrLifetimes.Value;
    private readonly Lazy<ImmutableArray<VarPtrLifetime>> _varPtrLifetimes;

    public X86GCInfo(Target target, TargetPointer gcInfoAddress, uint gcInfoVersion, uint relativeOffset = 0)
    {
        if (gcInfoVersion < MINIMUM_SUPPORTED_GCINFO_VERSION)
        {
            throw new NotSupportedException($"GCInfo version {gcInfoVersion} is not supported. Minimum supported version is {MINIMUM_SUPPORTED_GCINFO_VERSION}.");
        }
        if (gcInfoVersion > MAXIMUM_SUPPORTED_GCINFO_VERSION)
        {
            throw new NotSupportedException($"GCInfo version {gcInfoVersion} is not supported. Maximum supported version is {MAXIMUM_SUPPORTED_GCINFO_VERSION}.");
        }

        _target = target;

        _gcInfoAddress = gcInfoAddress;
        TargetPointer offset = gcInfoAddress;
        MethodSize = target.GCDecodeUnsigned(ref offset);
        RelativeOffset = relativeOffset;

        Debug.Assert(relativeOffset >= 0);
        Debug.Assert(relativeOffset <= MethodSize);

        Header = InfoHdr.DecodeHeader(target, ref offset, MethodSize, (int)gcInfoVersion);
        _infoHdrSize = (uint)(offset.Value - gcInfoAddress.Value);

        // Check if we are in the prolog
        if (relativeOffset < Header.PrologSize)
        {
            PrologOffset = relativeOffset;
        }

        // Check if we are in an epilog
        foreach (uint epilogStart in Header.Epilogs)
        {
            if (relativeOffset > epilogStart && relativeOffset < epilogStart + Header.EpilogSize)
            {
                EpilogOffset = relativeOffset - epilogStart;
            }
        }

        // Calculate raw stack size
        uint frameDwordCount = Header.FrameSize;
        RawStackSize = frameDwordCount * (uint)target.PointerSize;

        // Calculate callee saved regs
        uint savedRegsCount = 0;
        RegMask savedRegs = RegMask.NONE;

        if (Header.EdiSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EDI;
        }
        if (Header.EsiSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.ESI;
        }
        if (Header.EbxSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EBX;
        }
        if (Header.EbpSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EBP;
        }

        SavedRegsCountExclFP = savedRegsCount;
        SavedRegsMask = savedRegs;
        if (Header.EbpFrame || Header.DoubleAlign)
        {
            Debug.Assert(Header.EbpSaved);
            SavedRegsCountExclFP--;
        }

        // Lazily decode GC transitions. These values are not present in all Heap dumps. Only when they are required for stack walking.
        // Therefore, we can only read them when they are used by the stack walker.
        _transitions = new(DecodeTransitions);

        // Lazily calculate the pushed argument size. This forces the transitions to be decoded.
        _pushedArgSize = new(CalculatePushedArgSize);

        // Lazily decode the untracked-locals and VarPtr tables
        _untrackedSlots = new(DecodeUntrackedSlots);
        _varPtrLifetimes = new(DecodeVarPtrLifetimes);
    }

    private ImmutableDictionary<int, List<BaseGcTransition>> DecodeTransitions()
    {
        TargetPointer argTabPtr;
        if (Header.HasArgTabOffset)
        {
            // The GCInfo has an explicit argument table offset
            argTabPtr = _gcInfoAddress + _infoHdrSize + Header.ArgTabOffset;
        }
        else
        {
            // The GCInfo does not have an explicit argument table offset, we need to calculate it
            // from the end of the header. The argument table is located after
            // the NoGCRegions table, the UntrackedVariable table, and the FrameVariableLifetime table.
            argTabPtr = _gcInfoAddress + _infoHdrSize;

            /* Skip over the no GC regions table */
            for (int i = 0; i < Header.NoGCRegionCount; i++)
            {
                // The NoGCRegion table has a variable size, each entry is 2 unsigned integers.
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
            }

            /* Skip over the untracked frame variable table */
            for (int i = 0; i < Header.UntrackedCount; i++)
            {
                // The UntrackedVariable table has a variable size, each entry is 1 signed integer.
                _target.GCDecodeSigned(ref argTabPtr);
            }

            /* Skip over the frame variable lifetime table */
            for (int i = 0; i < Header.VarPtrTableSize; i++)
            {
                // The FrameVariableLifetime table has a variable size, each entry is 3 unsigned integer.
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
            }
        }
        GCArgTable argTable = new(_target, Header, argTabPtr);
        return argTable.Transitions.ToImmutableDictionary();
    }

    private uint CalculatePushedArgSize()
    {
        int depth = 0;
        foreach (int offset in Transitions.Keys.OrderBy(i => i))
        {
            if (offset > RelativeOffset)
                break; // calculate only to current offset
            foreach (BaseGcTransition gcTransition in Transitions[offset])
            {
                switch (gcTransition)
                {
                    case GcTransitionRegister gcTransitionRegister:
                        if (gcTransitionRegister.IsLive == Action.PUSH)
                        {
                            depth += gcTransitionRegister.PushCountOrPopSize;
                        }
                        else if (gcTransitionRegister.IsLive == Action.POP)
                        {
                            depth -= gcTransitionRegister.PushCountOrPopSize;
                        }
                        break;
                    case StackDepthTransition stackDepthTransition:
                        depth += stackDepthTransition.StackDepthChange;
                        break;
                    case GcTransitionPointer gcTransitionPointer:
                        if (gcTransitionPointer.Act == Action.PUSH)
                        {
                            // when there is fullArgInfo, the current depth is incremented by the number of pushed arguments
                            depth++;
                        }
                        else if (gcTransitionPointer.Act == Action.POP)
                        {
                            depth -= (int)gcTransitionPointer.ArgOffset;
                        }
                        break;
                    case IPtrMask:
                    case GcTransitionCall:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported gc transition type");
                }
            }
        }

        return (uint)(depth * _target.PointerSize);
    }

    private ImmutableArray<UntrackedSlot> DecodeUntrackedSlots()
    {
        if (Header.UntrackedCount == 0)
            return ImmutableArray<UntrackedSlot>.Empty;

        // The untracked-locals table follows the NoGCRegions table in the bitstream
        // (see DecodeTransitions for the section layout).
        TargetPointer offset = _gcInfoAddress + _infoHdrSize;
        for (int i = 0; i < Header.NoGCRegionCount; i++)
        {
            _target.GCDecodeUnsigned(ref offset);
            _target.GCDecodeUnsigned(ref offset);
        }

        // Each entry is a signed varint, delta-encoded against the previous entry.
        // Low 2 bits hold flags (byref=0x1, pinned=0x2); the remainder is the frame-relative
        // stack offset. On EBP-frames the offset is EBP-relative; on ESP-frames it is
        // ESP-relative. Double-aligned frames use a hybrid encoding: offsets that lie
        // above the frame are EBP-relative even when the rest of the frame is ESP-based.
        // Reference: gc_unwind_x86.inl (EnumGcRefsX86 untracked path) and
        // ILCompiler.Reflection.ReadyToRun/x86/GcSlotTable.cs (DecodeUntracked).
        uint calleeSavedRegsCount = 0;
        if (Header.DoubleAlign)
        {
            if (Header.EdiSaved) calleeSavedRegsCount++;
            if (Header.EsiSaved) calleeSavedRegsCount++;
            if (Header.EbxSaved) calleeSavedRegsCount++;
        }

        ImmutableArray<UntrackedSlot>.Builder builder = ImmutableArray.CreateBuilder<UntrackedSlot>((int)Header.UntrackedCount);
        int lastStkOffs = 0;
        for (uint i = 0; i < Header.UntrackedCount; i++)
        {
            int delta = _target.GCDecodeSigned(ref offset);
            int stkOffs = lastStkOffs - delta;
            lastStkOffs = stkOffs;

            uint lowBits = OFFSET_MASK & (uint)stkOffs;
            stkOffs = (int)((uint)stkOffs & ~OFFSET_MASK);

            bool isEbpRelative = Header.EbpFrame;
            if (Header.DoubleAlign &&
                (uint)stkOffs >= _target.PointerSize * (Header.FrameSize + calleeSavedRegsCount))
            {
                // Double-aligned frame: offsets above the frame proper are EBP-relative.
                isEbpRelative = true;
                stkOffs -= (int)(_target.PointerSize * (Header.FrameSize + calleeSavedRegsCount));
            }

            builder.Add(new UntrackedSlot(stkOffs, isEbpRelative, lowBits));
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<VarPtrLifetime> DecodeVarPtrLifetimes()
    {
        if (Header.VarPtrTableSize == 0)
            return ImmutableArray<VarPtrLifetime>.Empty;

        // The VarPtr table follows the untracked-locals table in the bitstream.
        TargetPointer offset = _gcInfoAddress + _infoHdrSize;
        for (int i = 0; i < Header.NoGCRegionCount; i++)
        {
            _target.GCDecodeUnsigned(ref offset);
            _target.GCDecodeUnsigned(ref offset);
        }
        for (int i = 0; i < Header.UntrackedCount; i++)
        {
            _target.GCDecodeSigned(ref offset);
        }

        // Each entry is three unsigned varints: (varOffs, begOffs, endOffs).
        // varOffs is absolute; begOffs is delta-from-previous-begOffs; endOffs is delta-from-begOffs.
        // Low 2 bits of varOffs hold flags (byref=0x1, this=0x2 -- NOT pinned for tracked locals).
        // Reference: gc_unwind_x86.inl varPtrTable processing and
        // ILCompiler.Reflection.ReadyToRun/x86/GcSlotTable.cs (DecodeFrameVariableLifetimeTable).
        ImmutableArray<VarPtrLifetime>.Builder builder = ImmutableArray.CreateBuilder<VarPtrLifetime>((int)Header.VarPtrTableSize);
        uint curOffs = 0;
        for (uint i = 0; i < Header.VarPtrTableSize; i++)
        {
            uint varOffsRaw = _target.GCDecodeUnsigned(ref offset);
            uint begOffs = _target.GCDecodeUDelta(ref offset, curOffs);
            uint endOffs = _target.GCDecodeUDelta(ref offset, begOffs);

            uint lowBits = varOffsRaw & OFFSET_MASK;
            int stkOffs = (int)(varOffsRaw & ~OFFSET_MASK);

            curOffs = begOffs;

            builder.Add(new VarPtrLifetime(begOffs, endOffs, stkOffs, lowBits));
        }

        return builder.MoveToImmutable();
    }

    private const uint OFFSET_MASK = 0x3;

    /// <summary>
    /// Returns true if <paramref name="codeOffset"/> falls within the method's prolog.
    /// </summary>
    private bool IsCodeOffsetInProlog(uint codeOffset)
        => codeOffset < Header.PrologSize;

    /// <summary>
    /// Returns true if <paramref name="codeOffset"/> falls within any epilog.
    /// </summary>
    private bool IsCodeOffsetInEpilog(uint codeOffset)
    {
        foreach (uint epilogStart in Header.Epilogs)
        {
            if (codeOffset > epilogStart && codeOffset < epilogStart + Header.EpilogSize)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Converts a single-bit <see cref="RegMask"/> value to the platform-agnostic
    /// register number used by <c>X86Context.TryReadRegister</c> and by <see cref="LiveSlot.RegisterNumber"/>.
    /// EAX=0, ECX=1, EDX=2, EBX=3, ESP=4, EBP=5, ESI=6, EDI=7 -- matches the x86 ModRM encoding.
    /// </summary>
    private static uint RegMaskToRegisterNumber(RegMask reg)
    {
        // RegMask is a flags enum where each register sits on its own bit
        // (EAX=0x1, ECX=0x2, ..., EDI=0x80). Log2 yields the register number.
        return (uint)System.Numerics.BitOperations.Log2((uint)reg);
    }

    uint IGCInfoDecoder.GetCodeLength() => MethodSize;

    uint IGCInfoDecoder.GetStackBaseRegister()
    {
        // x86 ModRM register encoding: ESP = 4, EBP = 5. EBP is the stack base for
        // EBP-frames and double-aligned frames; otherwise stack base is ESP.
        const uint REG_ESP = 4;
        const uint REG_EBP = 5;
        return (Header.EbpFrame || Header.DoubleAlign) ? REG_EBP : REG_ESP;
    }

    uint IGCInfoDecoder.GetSizeOfStackParameterArea()
    {
        // x86 GC info does not encode a separate outgoing-argument scratch area; the
        // per-offset transitions report pushed argument pointers directly at each offset.
        // Returning 0 disables the GcScanner's scratch-area filter on x86, which is the
        // correct behaviour: the live state at a given offset (call site or fully-interruptible
        // point) already excludes any args that have been popped by the time we resume there.
        return 0;
    }

    uint IGCInfoDecoder.GetCalleePoppedArgumentsSize()
    {
        // Mirrors native ::GetStackParameterSize(hdrInfo) in gc_unwind_x86.inl: varargs are
        // caller-popped (return 0); other methods report the argument size from the GC info
        // header. Used by EECodeManager::GetStackParameterSize on x86.
        return Header.VarArgs ? 0u : Header.ArgCount * (uint)_target.PointerSize;
    }

    IReadOnlyList<InterruptibleRange> IGCInfoDecoder.GetInterruptibleRanges()
    {
        // The x86 GC info `interruptible` header bit divides methods into two encodings:
        //
        // * Fully interruptible (`Header.Interruptible == true`): every offset in the
        //   method body (post-prolog, pre-epilog) is GC-safe. The C++ walker
        //   (`EnumGcRefsX86` in gc_unwind_x86.inl) explicitly returns without
        //   reporting refs when the queried offset falls inside the prolog or any
        //   epilog, so we exclude those regions here too.
        // * Partially interruptible (`Header.Interruptible == false`): only call sites
        //   are GC-safe. Each call site appears as a `GcTransitionCall` at its code
        //   offset. We surface each as a single-byte range so the only consumer
        //   (the catch-handler PC override in `StackWalk_1.WalkStackReferences`) can
        //   pick the first call-site offset at or after the clause start.
        if (Header.Interruptible)
        {
            // Body minus prolog minus all epilogs. Epilogs are stored as code offsets
            // (start of each epilog); each spans `EpilogSize` bytes.
            uint cursor = Header.PrologSize;
            uint methodSize = MethodSize;
            List<InterruptibleRange> ranges = [];
            foreach (int epilogStart in Header.Epilogs.OrderBy(e => e))
            {
                uint eStart = (uint)epilogStart;
                uint eEnd = eStart + Header.EpilogSize;
                if (eStart > cursor)
                    ranges.Add(new InterruptibleRange(cursor, eStart));
                cursor = Math.Max(cursor, eEnd);
            }
            if (cursor < methodSize)
                ranges.Add(new InterruptibleRange(cursor, methodSize));
            return ranges;
        }

        // Partially interruptible: emit each call-site offset as a (offset, offset+1) range.
        List<InterruptibleRange> callRanges = [];
        foreach (int offset in Transitions.Keys.OrderBy(o => o))
        {
            if ((uint)offset < Header.PrologSize)
                continue;

            foreach (BaseGcTransition transition in Transitions[offset])
            {
                if (transition is GcTransitionCall)
                {
                    callRanges.Add(new InterruptibleRange((uint)offset, (uint)offset + 1));
                    break;
                }
            }
        }
        return callRanges;
    }

    IReadOnlyList<LiveSlot> IGCInfoDecoder.EnumerateLiveSlots(uint instructionOffset, GcSlotEnumerationOptions options)
    {
        // x86 stack base encoding for LiveSlot.SpBase: 1 = SP-relative, 2 = FRAMEREG (EBP/EBP-double-aligned) relative.
        // (See IGCInfo.cs LiveSlot docs and GcScanner.EnumGcRefsForManagedFrame for how these get resolved.)
        const uint SP_REL = 1;
        const uint FRAMEREG_REL = 2;

        // In the prolog (before all locals are initialised) and in the epilog (after they've
        // been torn down) the GC info doesn't accurately describe live slots. The runtime
        // never suspends a thread inside the prolog/epilog under normal circumstances; the only
        // path that reaches here is ExecutionAborted (thread abort or stack overflow). In that
        // case we still drop reporting -- this matches native EnumGcRefsX86 in gc_unwind_x86.inl
        // which returns true without enumerating when we're in the prolog/epilog.
        if (IsCodeOffsetInProlog(instructionOffset) || IsCodeOffsetInEpilog(instructionOffset))
            return Array.Empty<LiveSlot>();

        // For non-interruptible methods, an ExecutionAborted offset that isn't at a recorded
        // safe point yields no reliable GC info; skip reporting as the native walker does
        // (gc_unwind_x86.inl).
        if (options.IsExecutionAborted && !Header.Interruptible)
            return Array.Empty<LiveSlot>();

        List<LiveSlot> result = [];

        // (1) Untracked frame locals -- always live for the entire method body.
        // Filter funclets suppress untracked reporting because the parent frame already
        // reports them (matches native EnumGcRefsX86 isFilterFunclet path).
        if (!options.SuppressUntrackedSlots)
        {
            foreach (UntrackedSlot us in UntrackedSlots)
            {
                // LowBits encoding matches LiveSlot.GcFlags exactly: 0x1 = interior, 0x2 = pinned.
                uint spBase = us.IsEbpRelative ? FRAMEREG_REL : SP_REL;
                result.Add(new LiveSlot(IsRegister: false, RegisterNumber: 0, SpOffset: us.StackOffset, SpBase: spBase, GcFlags: us.LowBits));
            }
        }

        // (2) VarPtr-tracked frame locals -- live when instructionOffset is within [Begin, End).
        // Only EBP-frames produce entries here; the table is empty for ESP-frames.
        {
            uint spBase = Header.EbpFrame ? FRAMEREG_REL : SP_REL;
            foreach (VarPtrLifetime vp in VarPtrLifetimes)
            {
                if (instructionOffset < vp.BeginOffset || instructionOffset >= vp.EndOffset)
                    continue;

                // LowBits encoding matches LiveSlot.GcFlags exactly: 0x1 = interior, 0x2 = pinned.
                result.Add(new LiveSlot(IsRegister: false, RegisterNumber: 0, SpOffset: vp.StackOffset, SpBase: spBase, GcFlags: vp.LowBits));
            }
        }

        // (3) Live registers and pushed pointer args from the transition stream.
        EnumerateTransitionLiveSlots(instructionOffset, options, result, SP_REL);

        return result;
    }

    /// <summary>
    /// Walks <see cref="Transitions"/> up to and including <paramref name="instructionOffset"/>,
    /// accumulating live register state and currently-pushed pointer arguments, and emits a
    /// <see cref="LiveSlot"/> per live register / pushed pointer.
    /// </summary>
    /// <remarks>
    /// For fully-interruptible methods, every transition strictly before
    /// <paramref name="instructionOffset"/> contributes to the current state. For partially-
    /// interruptible methods, the JIT only emits transitions at call sites (and at LIVE/DEAD
    /// markers); the live state at the exact <paramref name="instructionOffset"/> is whatever
    /// the most-recent call-site transition described.
    /// </remarks>
    private void EnumerateTransitionLiveSlots(
        uint instructionOffset,
        GcSlotEnumerationOptions options,
        List<LiveSlot> result,
        uint spRelBase)
    {
        // Set of registers currently holding live GC refs at the walked offset.
        RegMask liveRegs = RegMask.NONE;
        RegMask liveIptrRegs = RegMask.NONE;

        // Pushed pointer args on the stack, keyed by ESP-relative byte offset (positive).
        // Value: GcFlags (0x1 = interior). Use a sorted dictionary for deterministic output.
        SortedDictionary<int, uint> pushedPtrs = new();

        // Stack depth in pointer-size units, used by GcTransitionPointer offsets which are
        // expressed as "argOffs from top of pushed args". Mirrors the depth bookkeeping in
        // CalculatePushedArgSize.
        int depthSlots = 0;

        // For partially-interruptible methods, only the call-site state matters -- we collect
        // the most-recent call site at-or-before instructionOffset and emit its registers/args.
        GcTransitionCall? activeCallSite = null;

        foreach (int offset in Transitions.Keys.OrderBy(o => o))
        {
            // Stop AFTER processing transitions at instructionOffset for fully-interruptible
            // code. For partially-interruptible code, GcTransitionCall transitions can also
            // contain register/arg state describing the call site.
            if (offset > instructionOffset)
                break;

            foreach (BaseGcTransition transition in Transitions[offset])
            {
                switch (transition)
                {
                    case GcTransitionRegister regT:
                        ApplyRegisterTransition(regT, ref liveRegs, ref liveIptrRegs, ref depthSlots, pushedPtrs);
                        break;
                    case GcTransitionPointer ptrT:
                        ApplyPointerTransition(ptrT, ref depthSlots, pushedPtrs);
                        break;
                    case StackDepthTransition stackT:
                        depthSlots += stackT.StackDepthChange;
                        if (depthSlots < 0) depthSlots = 0;
                        break;
                    case GcTransitionCall callT when offset == (int)instructionOffset:
                        // Partially-interruptible: this is the only kind of transition that
                        // directly describes the call-site live state. For fully-interruptible
                        // code, GcTransitionCall is informational only -- the surrounding
                        // PUSH/POP/LIVE/DEAD transitions already maintain the state.
                        activeCallSite = callT;
                        break;
                    case IPtrMask:
                    case CalleeSavedRegister:
                    case GcTransitionCall:
                        // CalleeSavedRegister is purely informational for the stack walker.
                        // IPtrMask is reserved for future interior-pointer-bitmap support;
                        // the current MVP decoder does not propagate it onto pushed slots.
                        // GcTransitionCall at offset != instructionOffset is also ignored.
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported x86 GC transition: {transition.GetType().Name}");
                }
            }
        }

        // Emit live registers from accumulated state.
        // The IsActiveFrame option controls whether scratch (callee-trashed) registers are
        // reported. On non-leaf frames, the live registers are by definition scratch from the
        // caller's perspective, so they should not be reported. Native EnumGcRefsX86 handles
        // this via the willContinueExecution / ExecutionAborted flags interacting with the
        // CHK_AND_REPORT_REG macro family.
        if (options.IsActiveFrame || activeCallSite is not null)
        {
            // Iterate single-bit register values via RM_ALL.
            foreach (RegMask r in EnumerateSingleRegs())
            {
                if ((liveRegs & r) == 0) continue;
                uint gcFlags = (liveIptrRegs & r) != 0 ? 0x1u : 0u;
                result.Add(new LiveSlot(IsRegister: true, RegisterNumber: RegMaskToRegisterNumber(r), SpOffset: 0, SpBase: 0, GcFlags: gcFlags));
            }
        }

        // Emit pushed pointer args as SP-relative stack slots.
        foreach (KeyValuePair<int, uint> pushed in pushedPtrs)
        {
            result.Add(new LiveSlot(IsRegister: false, RegisterNumber: 0, SpOffset: pushed.Key, SpBase: spRelBase, GcFlags: pushed.Value));
        }

        // Partially-interruptible call-site: emit its register set and pointer args directly.
        if (activeCallSite is not null)
        {
            foreach (GcTransitionCall.CallRegister cr in activeCallSite.CallRegisters)
            {
                uint gcFlags = cr.IsByRef ? 0x1u : 0u;
                result.Add(new LiveSlot(IsRegister: true, RegisterNumber: RegMaskToRegisterNumber(cr.Register), SpOffset: 0, SpBase: 0, GcFlags: gcFlags));
            }
            foreach (GcTransitionCall.PtrArg pa in activeCallSite.PtrArgs)
            {
                uint gcFlags = pa.LowBit != 0 ? 0x1u : 0u;
                result.Add(new LiveSlot(IsRegister: false, RegisterNumber: 0, SpOffset: (int)pa.StackOffset, SpBase: spRelBase, GcFlags: gcFlags));
            }
        }
    }

    private static void ApplyRegisterTransition(
        GcTransitionRegister regT,
        ref RegMask liveRegs,
        ref RegMask liveIptrRegs,
        ref int depthSlots,
        SortedDictionary<int, uint> pushedPtrs)
    {
        switch (regT.IsLive)
        {
            case Action.LIVE:
                liveRegs |= regT.Register;
                if (regT.Iptr) liveIptrRegs |= regT.Register;
                else liveIptrRegs &= ~regT.Register;
                break;
            case Action.DEAD:
                liveRegs &= ~regT.Register;
                liveIptrRegs &= ~regT.Register;
                break;
            case Action.PUSH:
                // The register's value is pushed onto the stack as a callee argument.
                // Each push advances depth; record the slot at the new top-of-stack.
                for (int i = 0; i < regT.PushCountOrPopSize; i++)
                {
                    depthSlots++;
                    int sp = -depthSlots * 4; // x86 grows down; offset relative to call-site SP
                    pushedPtrs[sp] = regT.Iptr ? 0x1u : 0u;
                }
                break;
            case Action.POP:
                // Pop unrolls the most recently pushed slots.
                for (int i = 0; i < regT.PushCountOrPopSize && depthSlots > 0; i++)
                {
                    int sp = -depthSlots * 4;
                    pushedPtrs.Remove(sp);
                    depthSlots--;
                }
                break;
            case Action.KILL:
                // Used by EBP-frame partial-interrupt encoding to invalidate pushed args
                // (kill all currently-tracked pushed pointers up to argOffset).
                pushedPtrs.Clear();
                depthSlots = 0;
                break;
        }
    }

    private static void ApplyPointerTransition(
        GcTransitionPointer ptrT,
        ref int depthSlots,
        SortedDictionary<int, uint> pushedPtrs)
    {
        switch (ptrT.Act)
        {
            case Action.PUSH:
                depthSlots++;
                int spPush = -depthSlots * 4;
                pushedPtrs[spPush] = ptrT.Iptr ? 0x1u : 0u;
                break;
            case Action.POP:
                // ArgOffset slots are popped from the top of the pushed-args stack.
                for (uint i = 0; i < ptrT.ArgOffset && depthSlots > 0; i++)
                {
                    int sp = -depthSlots * 4;
                    pushedPtrs.Remove(sp);
                    depthSlots--;
                }
                break;
            case Action.KILL:
                pushedPtrs.Clear();
                depthSlots = 0;
                break;
        }
    }

    private static IEnumerable<RegMask> EnumerateSingleRegs()
    {
        yield return RegMask.EAX;
        yield return RegMask.ECX;
        yield return RegMask.EDX;
        yield return RegMask.EBX;
        yield return RegMask.EBP;
        yield return RegMask.ESI;
        yield return RegMask.EDI;
        // ESP is intentionally excluded -- it's never a live GC ref holder.
    }
}

/// <summary>
/// An always-live GC frame slot (entry of the untracked-locals table).
/// The slot is live for the entire method body (post-prolog, pre-epilog).
/// </summary>
/// <param name="StackOffset">Frame-relative byte offset of the slot.</param>
/// <param name="IsEbpRelative">True if <see cref="StackOffset"/> is EBP-relative; false if ESP-relative.</param>
/// <param name="LowBits">Raw flag bits from the encoded offset (0x1 = byref/interior, 0x2 = pinned).</param>
public readonly record struct UntrackedSlot(int StackOffset, bool IsEbpRelative, uint LowBits);

/// <summary>
/// A tracked GC frame variable with a per-offset lifetime range (entry of the
/// FrameVariableLifetime / VarPtr table). The slot is live while the executing
/// instruction offset lies in <c>[BeginOffset, EndOffset)</c>.
/// VarPtr-tracked variables only exist on EBP-based frames.
/// </summary>
/// <param name="BeginOffset">Inclusive code offset (relative to method start) at which the slot becomes live.</param>
/// <param name="EndOffset">Exclusive code offset at which the slot becomes dead.</param>
/// <param name="StackOffset">Frame-relative byte offset of the slot (EBP-relative on EBP frames, ESP-relative otherwise).</param>
/// <param name="LowBits">Raw flag bits from the encoded offset (0x1 = byref/interior, 0x2 = pinned).</param>
public readonly record struct VarPtrLifetime(uint BeginOffset, uint EndOffset, int StackOffset, uint LowBits);
