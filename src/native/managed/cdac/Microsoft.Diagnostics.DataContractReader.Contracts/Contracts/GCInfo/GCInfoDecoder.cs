// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal class GcInfoDecoder<TTraits> : IGCInfoDecoder where TTraits : IGCInfoTraits
{
    private enum DecodePoints
    {
        CodeLength,
        ReturnKind,
        VarArg,
        PrologLength,
        GSCookie,
        PSPSym,
        GenericInstContext,
        EditAndContinue,
        ReversePInvoke,
        InterruptibleRanges,
        SlotTable,
        Complete,
    }

    [Flags]
    internal enum GcInfoHeaderFlags : uint
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
        GC_INFO_HAS_EDIT_AND_CONTINUE_INFO = 0x100,
        GC_INFO_REVERSE_PINVOKE_FRAME = 0x200,

        GC_INFO_FLAGS_BIT_SIZE_VERSION_1 = 9,
        GC_INFO_FLAGS_BIT_SIZE = 10,
    };

    [Flags]
    internal enum GcSlotFlags : uint
    {
        GC_SLOT_BASE = 0x0,
        GC_SLOT_INTERIOR = 0x1,
        GC_SLOT_PINNED = 0x2,
        GC_SLOT_UNTRACKED = 0x4,
    }

    [Flags]
    internal enum GcStackSlotBase : uint
    {
        GC_CALLER_SP_REL = 0x0,
        GC_SP_REL = 0x1,
        GC_FRAMEREG_REL = 0x2,

        GC_SPBASE_FIRST = GC_CALLER_SP_REL,
        GC_SPBASE_LAST = GC_FRAMEREG_REL,
    }

    public readonly record struct InterruptibleRange(uint StartOffset, uint EndOffset);

    public readonly record struct GcSlotDesc
    {
        /* Register Slot */
        public readonly uint RegisterNumber;

        /* Stack Slot */
        public readonly int SpOffset;
        public readonly GcStackSlotBase Base;

        /* Shared fields */
        public readonly GcSlotFlags Flags;
        public readonly bool IsRegister;

        private GcSlotDesc(uint registerNumber, int spOffset, GcStackSlotBase slotBase, GcSlotFlags flags, bool isRegister = false)
        {
            RegisterNumber = registerNumber;
            SpOffset = spOffset;
            Base = slotBase;
            Flags = flags;
            IsRegister = isRegister;
        }

        public static GcSlotDesc CreateRegisterSlot(uint registerNumber, GcSlotFlags flags)
            => new GcSlotDesc(registerNumber, 0, 0, flags, isRegister: true);

        public static GcSlotDesc CreateStackSlot(int spOffset, GcStackSlotBase slotBase, GcSlotFlags flags)
            => new GcSlotDesc(0, spOffset, slotBase, flags, isRegister: false);
    }

    private readonly Target _target;
    private readonly TargetPointer _pGcInfo;
    private readonly uint _gcVersion;
    private readonly NativeReader _reader;
    private readonly RuntimeInfoArchitecture _arch;
    private readonly bool PartiallyInterruptibleGCSupported = true;

    /* Decode State */
    private int _bitOffset;
    private IEnumerator<DecodePoints> _decodePoints;
    private List<DecodePoints> _completedDecodePoints = [];

    /* Header Fields */
    private bool _slimHeader;
    private GcInfoHeaderFlags _headerFlags;
    private uint _stackBaseRegister;
    private uint _codeLength;
    private uint _validRangeStart;
    private uint _validRangeEnd;
    private int _gsCookieStackSlot;
    private int _pspSymStackSlot;
    private int _genericsInstContextStackSlot;
    private uint _sizeOfEnCPreservedArea;
    private uint _sizeOfEnCFixedStackFrame;
    private int _reversePInvokeFrameStackSlot;
    private uint _fixedStackParameterScratchArea;

    /* Fields */

    private uint _numSafePoints;
    private uint _numInterruptibleRanges;
    private List<InterruptibleRange> _interruptibleRanges = [];
    private int _safePointBitOffset;

    /* Slot Table Fields */
    private uint _numRegisters;
    private uint _numUntrackedSlots;
    private uint _numSlots;
    private List<GcSlotDesc> _slots = [];
    private int _liveStateBitOffset;

    public GcInfoDecoder(Target target, TargetPointer gcInfoAddress, uint gcVersion)
    {
        _target = target;
        _pGcInfo = gcInfoAddress;
        _gcVersion = gcVersion;

        TargetStream targetStream = new TargetStream(_target, _pGcInfo, /*arbitrary*/ 10000);
        _reader = new NativeReader(targetStream, _target.IsLittleEndian);

        _arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();

        _decodePoints = Decode().GetEnumerator();
    }

    #region Decoding Methods

    private IEnumerable<DecodePoints> Decode()
    {
        IEnumerable<DecodePoints> headerDecodePoints = DecodeHeader();
        foreach (DecodePoints dp in headerDecodePoints)
            yield return dp;

        IEnumerable<DecodePoints> bodyDecodePoints = DecodeBody();
        foreach (DecodePoints dp in bodyDecodePoints)
            yield return dp;

        yield return DecodePoints.Complete;
    }

    private IEnumerable<DecodePoints> DecodeBody()
    {
        IEnumerable<DecodePoints> safePoints = DecodeSafePoints();
        foreach (DecodePoints dp in safePoints)
            yield return dp;

        IEnumerable<DecodePoints> interruptibleRanges = DecodeInterruptibleRanges();
        foreach (DecodePoints dp in interruptibleRanges)
            yield return dp;

        yield return DecodePoints.InterruptibleRanges;

        IEnumerable<DecodePoints> slotTable = DecodeSlotTable();
        foreach (DecodePoints dp in slotTable)
            yield return dp;

        // Save the bit offset for EnumerateLiveSlots — the live state data follows immediately
        _liveStateBitOffset = _bitOffset;

        yield return DecodePoints.SlotTable;
    }

    private IEnumerable<DecodePoints> DecodeSlotTable()
    {
        if (_reader.ReadBits(1, ref _bitOffset) != 0)
        {
            _numRegisters = _reader.DecodeVarLengthUnsigned(TTraits.NUM_REGISTERS_ENCBASE, ref _bitOffset);
        }

        uint numStackSlots = 0;
        if (_reader.ReadBits(1, ref _bitOffset) != 0)
        {
            numStackSlots = _reader.DecodeVarLengthUnsigned(TTraits.NUM_STACK_SLOTS_ENCBASE, ref _bitOffset);
            _numUntrackedSlots = _reader.DecodeVarLengthUnsigned(TTraits.NUM_UNTRACKED_SLOTS_ENCBASE, ref _bitOffset);
        }

        _numSlots = _numRegisters + numStackSlots + _numUntrackedSlots;
        _slots = new List<GcSlotDesc>((int)_numSlots);

        // Decode register slots
        if (_numRegisters > 0)
        {
            uint regNum = _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_ENCBASE, ref _bitOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);

            _slots.Add(GcSlotDesc.CreateRegisterSlot(regNum, flags));

            for (int i = 1; i < _numRegisters; i++)
            {
                if (flags != 0)
                {
                    regNum = _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_ENCBASE, ref _bitOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);
                }
                else
                {
                    regNum += _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_DELTA_ENCBASE, ref _bitOffset) + 1;
                }

                _slots.Add(GcSlotDesc.CreateRegisterSlot(regNum, flags));
            }
        }

        // Decode stack slots
        if (numStackSlots > 0)
        {
            GcStackSlotBase spBase = (GcStackSlotBase)_reader.ReadBits(2, ref _bitOffset);
            int normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref _bitOffset);
            int spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);

            _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));

            for (int i = 1; i < numStackSlots; i++)
            {
                spBase = (GcStackSlotBase)_reader.ReadBits(2, ref _bitOffset);

                if (flags != 0)
                {
                    // When previous flags were non-zero, the next slot uses a FULL offset (not delta)
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref _bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);
                }
                else
                {
                    int normSpOffsetDelta = (int)_reader.DecodeVarLengthUnsigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset);
                    normSpOffset += normSpOffsetDelta;
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                }

                _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));
            }
        }

        // Decode untracked slots
        if (_numUntrackedSlots > 0)
        {
            GcStackSlotBase spBase = (GcStackSlotBase)_reader.ReadBits(2, ref _bitOffset);
            int normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref _bitOffset);
            int spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);

            _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));

            for (int i = 1; i < _numUntrackedSlots; i++)
            {
                spBase = (GcStackSlotBase)_reader.ReadBits(2, ref _bitOffset);

                if (flags != 0)
                {
                    // When previous flags were non-zero, the next slot uses a FULL offset (not delta)
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref _bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);
                }
                else
                {
                    int normSpOffsetDelta = (int)_reader.DecodeVarLengthUnsigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset);
                    normSpOffset += normSpOffsetDelta;
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                }

                _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));
            }
        }

        yield break;
    }

    private IEnumerable<DecodePoints> DecodeInterruptibleRanges()
    {
        if (_numInterruptibleRanges == 0)
            yield break;

        uint prevEndOffset = 0;

        uint lastInterruptibleRangeStopOffsetNormalized = 0;

        _interruptibleRanges = new List<InterruptibleRange>((int)_numInterruptibleRanges);
        for (uint i = 0; i < _numInterruptibleRanges; i++)
        {
            uint normStartDelta = _reader.DecodeVarLengthUnsigned(TTraits.INTERRUPTIBLE_RANGE_DELTA1_ENCBASE, ref _bitOffset);
            uint normStopDelta = _reader.DecodeVarLengthUnsigned(TTraits.INTERRUPTIBLE_RANGE_DELTA2_ENCBASE, ref _bitOffset) + 1;

            uint rangeStartOffsetNormalized = lastInterruptibleRangeStopOffsetNormalized + normStartDelta;
            uint rangeStopOffsetNormalized = rangeStartOffsetNormalized + normStopDelta;

            uint rangeStartOffset = TTraits.DenormalizeCodeOffset(rangeStartOffsetNormalized);
            uint rangeStopOffset = TTraits.DenormalizeCodeOffset(rangeStopOffsetNormalized);

            Debug.Assert(rangeStartOffset < rangeStopOffset);
            Debug.Assert(rangeStartOffset >= prevEndOffset);

            lastInterruptibleRangeStopOffsetNormalized = rangeStopOffsetNormalized;
            prevEndOffset = rangeStopOffset;

            _interruptibleRanges.Add(new(rangeStartOffset, rangeStopOffset));
        }

        yield break;
    }

    private IEnumerable<DecodePoints> DecodeSafePoints()
    {
        // Save the position of the safe point data for FindSafePoint
        _safePointBitOffset = _bitOffset;
        // skip over safe point data
        uint numBitsPerOffset = CeilOfLog2(TTraits.NormalizeCodeOffset(_codeLength));
        _bitOffset += (int)(numBitsPerOffset * _numSafePoints);
        yield break;
    }

    private IEnumerable<DecodePoints> DecodeHeader()
    {
        _slimHeader = _reader.ReadBits(1, ref _bitOffset) == 0;

        if (!_slimHeader)
        {
            return DecodeFatHeader();
        }
        else
        {
            return DecodeSlimHeader();
        }
    }

    private IEnumerable<DecodePoints> DecodeSlimHeader()
    {
        if (_reader.ReadBits(1, ref _bitOffset) != 0)
        {
            _headerFlags = GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER;
            _stackBaseRegister = TTraits.DenormalizeStackBaseRegister(0);
        }
        else
        {
            _headerFlags = default;
            _stackBaseRegister = TTraits.NO_STACK_BASE_REGISTER;
        }
        yield return DecodePoints.ReturnKind;
        yield return DecodePoints.VarArg;

        _codeLength = TTraits.DenormalizeCodeLength(_reader.DecodeVarLengthUnsigned(TTraits.CODE_LENGTH_ENCBASE, ref _bitOffset));

        // predecoding the rest of slim header does not require any reading.
        _validRangeStart = 0;
        _validRangeEnd = 0;
        _gsCookieStackSlot = TTraits.NO_GS_COOKIE;
        _pspSymStackSlot = TTraits.NO_PSP_SYM;
        _genericsInstContextStackSlot = TTraits.NO_GENERICS_INST_CONTEXT;
        _sizeOfEnCPreservedArea = TTraits.NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
        _sizeOfEnCFixedStackFrame = 0;
        _reversePInvokeFrameStackSlot = TTraits.NO_REVERSE_PINVOKE_FRAME;

        if (TTraits.HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA)
            _fixedStackParameterScratchArea = 0;

        yield return DecodePoints.CodeLength;
        yield return DecodePoints.PrologLength;
        yield return DecodePoints.GSCookie;
        yield return DecodePoints.PSPSym;
        yield return DecodePoints.GenericInstContext;
        yield return DecodePoints.EditAndContinue;
        yield return DecodePoints.ReversePInvoke;

        if (PartiallyInterruptibleGCSupported)
        {
            _numSafePoints = _reader.DecodeVarLengthUnsigned(TTraits.NUM_SAFE_POINTS_ENCBASE, ref _bitOffset);
        }

        _numInterruptibleRanges = 0;
    }

    private IEnumerable<DecodePoints> DecodeFatHeader()
    {
        _headerFlags = (GcInfoHeaderFlags)_reader.ReadBits((int)GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE, ref _bitOffset);
        yield return DecodePoints.ReturnKind;
        yield return DecodePoints.VarArg;

        _codeLength = TTraits.DenormalizeCodeLength(_reader.DecodeVarLengthUnsigned(TTraits.CODE_LENGTH_ENCBASE, ref _bitOffset));
        yield return DecodePoints.CodeLength;

        if (_headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE))
        {
            // Note that normalization as a code offset can be different than
            // normalization as code length
            uint normCodeLength = TTraits.NormalizeCodeLength(_codeLength);

            // Decode prolog/epilog information
            uint normPrologSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_PROLOG_SIZE_ENCBASE, ref _bitOffset) + 1;
            uint normEpilogSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_EPILOG_SIZE_ENCBASE, ref _bitOffset);

            _validRangeStart = TTraits.DenormalizeCodeOffset(normPrologSize);
            _validRangeEnd = TTraits.DenormalizeCodeOffset(normCodeLength - normEpilogSize);
            Debug.Assert(_validRangeStart < _validRangeEnd);
        }
        else if ((_headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
        {
            // Decode prolog information
            uint normPrologSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_PROLOG_SIZE_ENCBASE, ref _bitOffset) + 1;
            _validRangeStart = TTraits.DenormalizeCodeOffset(normPrologSize);
            // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
            _validRangeEnd = _validRangeStart + 1;
        }
        else
        {
            _validRangeStart = 0;
            _validRangeEnd = 0;
        }
        yield return DecodePoints.PrologLength;

        _gsCookieStackSlot = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.GS_COOKIE_STACK_SLOT_ENCBASE, ref _bitOffset)) :
            TTraits.NO_GS_COOKIE;
        yield return DecodePoints.GSCookie;

        _pspSymStackSlot = TTraits.NO_PSP_SYM;
        yield return DecodePoints.PSPSym;

        _genericsInstContextStackSlot = (_headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE, ref _bitOffset)) :
            TTraits.NO_GENERICS_INST_CONTEXT;
        yield return DecodePoints.GenericInstContext;

        _stackBaseRegister = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER) ?
            TTraits.DenormalizeStackBaseRegister(_reader.DecodeVarLengthUnsigned(TTraits.STACK_BASE_REGISTER_ENCBASE, ref _bitOffset)) :
            TTraits.NO_STACK_BASE_REGISTER;

        if (_headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_INFO))
        {
            _sizeOfEnCPreservedArea = _reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, ref _bitOffset);

            // Arm64 has an additional field for EnC fixed stack frame size
            // This is controlled by target architecture rather than on the traits because it impacts the interpreter
            if (_arch == RuntimeInfoArchitecture.Arm64)
            {
                _sizeOfEnCFixedStackFrame = _reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE, ref _bitOffset);
            }
        }
        else
        {
            _sizeOfEnCPreservedArea = TTraits.NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
            _sizeOfEnCFixedStackFrame = 0;
        }
        yield return DecodePoints.EditAndContinue;

        _reversePInvokeFrameStackSlot = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_REVERSE_PINVOKE_FRAME) ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.REVERSE_PINVOKE_FRAME_ENCBASE, ref _bitOffset)) :
            TTraits.NO_REVERSE_PINVOKE_FRAME;
        yield return DecodePoints.ReversePInvoke;

        if (TTraits.HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA)
        {
            _fixedStackParameterScratchArea =
                TTraits.DenormalizeSizeOfStackArea(_reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_STACK_AREA_ENCBASE, ref _bitOffset));
        }

        if (PartiallyInterruptibleGCSupported)
        {
            _numSafePoints = _reader.DecodeVarLengthUnsigned(TTraits.NUM_SAFE_POINTS_ENCBASE, ref _bitOffset);
        }

        _numInterruptibleRanges = _reader.DecodeVarLengthUnsigned(TTraits.NUM_INTERRUPTIBLE_RANGES_ENCBASE, ref _bitOffset);
    }

    private void EnsureDecodedTo(DecodePoints point)
    {
        while (!_completedDecodePoints.Contains(point))
        {
            if (!_decodePoints.MoveNext())
                return; // nothing more to decode

            _completedDecodePoints.Add(_decodePoints.Current);
        }
    }

    #endregion
    #region Access Methods

    public uint GetCodeLength()
    {
        EnsureDecodedTo(DecodePoints.CodeLength);
        return _codeLength;
    }

    public IReadOnlyList<InterruptibleRange> GetInterruptibleRanges()
    {
        EnsureDecodedTo(DecodePoints.InterruptibleRanges);
        return _interruptibleRanges;
    }

    public uint StackBaseRegister
    {
        get
        {
            EnsureDecodedTo(DecodePoints.ReversePInvoke);
            return _stackBaseRegister;
        }
    }

    public uint NumTrackedSlots => _numSlots - _numUntrackedSlots;

    bool IGCInfoDecoder.EnumerateLiveSlots(
        uint instructionOffset,
        CodeManagerFlags flags,
        LiveSlotCallback reportSlot)
    {
        return EnumerateLiveSlots(instructionOffset, flags,
            (uint slotIndex, GcSlotDesc slot, uint gcFlags) =>
            {
                reportSlot(slot.IsRegister, slot.RegisterNumber, slot.SpOffset, (uint)slot.Base, gcFlags);
            });
    }

    /// <summary>
    /// Enumerates all GC slots that are live at the given instruction offset, invoking the callback for each.
    /// This is the managed equivalent of the native GcInfoDecoder::EnumerateLiveSlots.
    /// </summary>
    /// <param name="instructionOffset">The current instruction offset (relative to method start).</param>
    /// <param name="flags">CodeManagerFlags controlling reporting behavior.</param>
    /// <param name="reportSlot">Called for each live slot with (slotIndex, slotDesc, gcFlags).
    /// gcFlags contains GC_SLOT_INTERIOR/GC_SLOT_PINNED from the slot descriptor.</param>
    /// <returns>True if enumeration succeeded.</returns>
    public bool EnumerateLiveSlots(
        uint instructionOffset,
        CodeManagerFlags flags,
        Action<uint, GcSlotDesc, uint> reportSlot)
    {
        EnsureDecodedTo(DecodePoints.SlotTable);

        bool executionAborted = flags.HasFlag(CodeManagerFlags.ExecutionAborted);
        bool reportScratchSlots = flags.HasFlag(CodeManagerFlags.ActiveStackFrame);
        bool reportFpBasedSlotsOnly = flags.HasFlag(CodeManagerFlags.ReportFPBasedSlotsOnly);

        // WantsReportOnlyLeaf is always true for non-legacy formats
        if (flags.HasFlag(CodeManagerFlags.ParentOfFuncletStackFrame))
            return true;

        uint numTracked = NumTrackedSlots;
        if (numTracked == 0)
            goto ReportUntracked;

        uint normBreakOffset = TTraits.NormalizeCodeOffset(instructionOffset);

        // Find safe point index
        uint safePointIndex = _numSafePoints;
        if (_numSafePoints > 0)
        {
            safePointIndex = FindSafePoint(instructionOffset);
        }

        // Use a local bit offset starting from the saved live state position
        // so we don't disturb the decoder's main _bitOffset.
        int bitOffset = _liveStateBitOffset;

        if (PartiallyInterruptibleGCSupported)
        {
            uint pseudoBreakOffset = 0;
            uint numInterruptibleLength = 0;

            if (safePointIndex < _numSafePoints && !executionAborted)
            {
                // We have a safe point match — skip interruptible range computation
            }
            else
            {
                // Compute pseudoBreakOffset from interruptible ranges
                int countIntersections = 0;
                for (int i = 0; i < _interruptibleRanges.Count; i++)
                {
                    uint normStart = TTraits.NormalizeCodeOffset(_interruptibleRanges[i].StartOffset);
                    uint normStop = TTraits.NormalizeCodeOffset(_interruptibleRanges[i].EndOffset);

                    if (normBreakOffset >= normStart && normBreakOffset < normStop)
                    {
                        Debug.Assert(pseudoBreakOffset == 0);
                        countIntersections++;
                        pseudoBreakOffset = numInterruptibleLength + normBreakOffset - normStart;
                    }
                    numInterruptibleLength += normStop - normStart;
                }
                Debug.Assert(countIntersections <= 1);
                if (countIntersections == 0 && executionAborted)
                    return true; // Native: goto ExitSuccess (skip all reporting including untracked)
            }

            // Read the indirect live state table header (if present)
            uint numBitsPerOffset = 0;
            if (_numSafePoints > 0 && _reader.ReadBits(1, ref bitOffset) != 0)
            {
                numBitsPerOffset = (uint)_reader.DecodeVarLengthUnsigned(TTraits.POINTER_SIZE_ENCBASE, ref bitOffset) + 1;
            }

            // ---- Try partially interruptible first ----
            if (!executionAborted && safePointIndex != _numSafePoints)
            {
                if (numBitsPerOffset != 0)
                {
                    int offsetTablePos = bitOffset;
                    bitOffset += (int)(safePointIndex * numBitsPerOffset);
                    uint liveStatesOffset = (uint)_reader.ReadBits((int)numBitsPerOffset, ref bitOffset);
                    int liveStatesStart = (int)(((uint)offsetTablePos + _numSafePoints * numBitsPerOffset + 7) & (~7u));
                    bitOffset = (int)(liveStatesStart + liveStatesOffset);

                    if (_reader.ReadBits(1, ref bitOffset) != 0)
                    {
                        // RLE encoded
                        bool fSkip = _reader.ReadBits(1, ref bitOffset) == 0;
                        bool fReport = true;
                        uint readSlots = (uint)_reader.DecodeVarLengthUnsigned(
                            fSkip ? TTraits.LIVESTATE_RLE_SKIP_ENCBASE : TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset);
                        fSkip = !fSkip;
                        while (readSlots < numTracked)
                        {
                            uint cnt = (uint)_reader.DecodeVarLengthUnsigned(
                                fSkip ? TTraits.LIVESTATE_RLE_SKIP_ENCBASE : TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset) + 1;
                            if (fReport)
                            {
                                for (uint slotIndex = readSlots; slotIndex < readSlots + cnt; slotIndex++)
                                    ReportSlot(slotIndex, reportScratchSlots, reportFpBasedSlotsOnly, reportSlot);
                            }
                            readSlots += cnt;
                            fSkip = !fSkip;
                            fReport = !fReport;
                        }
                        Debug.Assert(readSlots == numTracked);
                        goto ReportUntracked;
                    }
                    // Normal 1-bit-per-slot encoding follows
                }
                else
                {
                    bitOffset += (int)(safePointIndex * numTracked);
                }

                for (uint slotIndex = 0; slotIndex < numTracked; slotIndex++)
                {
                    if (_reader.ReadBits(1, ref bitOffset) != 0)
                        ReportSlot(slotIndex, reportScratchSlots, reportFpBasedSlotsOnly, reportSlot);
                }
                goto ReportUntracked;
            }
            else
            {
                // Skip over safe point live state data.
                // NOTE: The native code always skips numSafePoints * numTracked here,
                // even when numBitsPerOffset != 0 (indirect table). This is technically
                // wrong for the indirect case, but the encoder never produces both
                // indirect safe points AND interruptible ranges, so it's unreachable.
                // Match the native behavior for consistency.
                bitOffset += (int)(_numSafePoints * numTracked);

                if (_numInterruptibleRanges == 0)
                    goto ReportUntracked;
            }

            // ---- Fully-interruptible path ----
            Debug.Assert(_numInterruptibleRanges > 0);
            Debug.Assert(numInterruptibleLength > 0);

            uint numChunks = (numInterruptibleLength + TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK - 1) / TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK;
            uint breakChunk = pseudoBreakOffset / TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK;
            Debug.Assert(breakChunk < numChunks);

            uint numBitsPerPointer = (uint)_reader.DecodeVarLengthUnsigned(TTraits.POINTER_SIZE_ENCBASE, ref bitOffset);
            if (numBitsPerPointer == 0)
                goto ReportUntracked;

            int pointerTablePos = bitOffset;

            // Find the chunk pointer (walk backwards if current chunk has no data)
            uint chunkPointer;
            uint chunk = breakChunk;
            for (; ; )
            {
                bitOffset = pointerTablePos + (int)(chunk * numBitsPerPointer);
                chunkPointer = (uint)_reader.ReadBits((int)numBitsPerPointer, ref bitOffset);
                if (chunkPointer != 0)
                    break;
                if (chunk-- == 0)
                    goto ReportUntracked;
            }

            int chunksStartPos = (int)(((uint)pointerTablePos + numChunks * numBitsPerPointer + 7) & (~7u));
            int chunkPos = (int)(chunksStartPos + chunkPointer - 1);
            bitOffset = chunkPos;

            // Read "couldBeLive" bitvector — first pass to count
            int couldBeLiveBitOffset = bitOffset;
            uint numCouldBeLiveSlots = 0;

            if (_reader.ReadBits(1, ref bitOffset) != 0)
            {
                // RLE encoded
                bool fSkipCBL = _reader.ReadBits(1, ref bitOffset) == 0;
                bool fReportCBL = true;
                uint readSlots = (uint)_reader.DecodeVarLengthUnsigned(
                    fSkipCBL ? TTraits.LIVESTATE_RLE_SKIP_ENCBASE : TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset);
                fSkipCBL = !fSkipCBL;
                while (readSlots < numTracked)
                {
                    uint cnt = (uint)_reader.DecodeVarLengthUnsigned(
                        fSkipCBL ? TTraits.LIVESTATE_RLE_SKIP_ENCBASE : TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref bitOffset) + 1;
                    if (fReportCBL)
                        numCouldBeLiveSlots += cnt;
                    readSlots += cnt;
                    fSkipCBL = !fSkipCBL;
                    fReportCBL = !fReportCBL;
                }
                Debug.Assert(readSlots == numTracked);
            }
            else
            {
                for (uint i = 0; i < numTracked; i++)
                {
                    if (_reader.ReadBits(1, ref bitOffset) != 0)
                        numCouldBeLiveSlots++;
                }
            }
            Debug.Assert(numCouldBeLiveSlots > 0);

            // "finalState" bits follow couldBeLive
            int finalStateBitOffset = bitOffset;
            // Transition data follows final state bits
            int transitionBitOffset = bitOffset + (int)numCouldBeLiveSlots;

            // Re-read couldBeLive to iterate slot indices (second pass)
            int cblOffset = couldBeLiveBitOffset;
            bool cblSimple = _reader.ReadBits(1, ref cblOffset) == 0;
            bool cblSkipFirst = false;
            uint cblCnt = 0;
            uint slotIdx = 0;
            if (!cblSimple)
            {
                cblSkipFirst = _reader.ReadBits(1, ref cblOffset) == 0;
                slotIdx = unchecked((uint)-1);
            }

            for (uint i = 0; i < numCouldBeLiveSlots; i++)
            {
                if (cblSimple)
                {
                    while (_reader.ReadBits(1, ref cblOffset) == 0)
                        slotIdx++;
                }
                else if (cblCnt > 0)
                {
                    cblCnt--;
                }
                else if (cblSkipFirst)
                {
                    uint tmp = (uint)_reader.DecodeVarLengthUnsigned(TTraits.LIVESTATE_RLE_SKIP_ENCBASE, ref cblOffset) + 1;
                    slotIdx += tmp;
                    cblCnt = (uint)_reader.DecodeVarLengthUnsigned(TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref cblOffset);
                }
                else
                {
                    uint tmp = (uint)_reader.DecodeVarLengthUnsigned(TTraits.LIVESTATE_RLE_RUN_ENCBASE, ref cblOffset) + 1;
                    slotIdx += tmp;
                    cblCnt = (uint)_reader.DecodeVarLengthUnsigned(TTraits.LIVESTATE_RLE_SKIP_ENCBASE, ref cblOffset);
                }

                uint isLive = (uint)_reader.ReadBits(1, ref finalStateBitOffset);

                if (chunk == breakChunk)
                {
                    uint normBreakOffsetDelta = pseudoBreakOffset % TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK;
                    for (; ; )
                    {
                        if (_reader.ReadBits(1, ref transitionBitOffset) == 0)
                            break;

                        uint transitionOffset = (uint)_reader.ReadBits(TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2, ref transitionBitOffset);
                        Debug.Assert(transitionOffset > 0 && transitionOffset < TTraits.NUM_NORM_CODE_OFFSETS_PER_CHUNK);
                        if (transitionOffset > normBreakOffsetDelta)
                            isLive ^= 1;
                    }
                }

                if (isLive != 0)
                    ReportSlot(slotIdx, reportScratchSlots, reportFpBasedSlotsOnly, reportSlot);

                slotIdx++;
            }
        }

    ReportUntracked:
        if (_numUntrackedSlots > 0 && (flags & (CodeManagerFlags.ParentOfFuncletStackFrame | CodeManagerFlags.NoReportUntracked)) == 0)
        {
            for (uint slotIndex = numTracked; slotIndex < _numSlots; slotIndex++)
                ReportSlot(slotIndex, reportScratchSlots, reportFpBasedSlotsOnly, reportSlot);
        }

        return true;
    }

    private void ReportSlot(uint slotIndex, bool reportScratchSlots, bool reportFpBasedSlotsOnly, Action<uint, GcSlotDesc, uint> reportSlot)
    {
        Debug.Assert(slotIndex < _slots.Count);
        GcSlotDesc slot = _slots[(int)slotIndex];
        uint gcFlags = (uint)slot.Flags & ((uint)GcSlotFlags.GC_SLOT_INTERIOR | (uint)GcSlotFlags.GC_SLOT_PINNED);

        if (slot.IsRegister)
        {
            // Skip scratch registers for non-leaf frames
            if (!reportScratchSlots && TTraits.IsScratchRegister(slot.RegisterNumber))
                return;
            // FP-based-only mode skips all register slots
            if (reportFpBasedSlotsOnly)
                return;
        }
        else
        {
            // Skip scratch stack slots for non-leaf frames (slots in the outgoing/scratch area)
            if (!reportScratchSlots && TTraits.IsScratchStackSlot(slot.SpOffset, (uint)slot.Base, _fixedStackParameterScratchArea))
                return;
            // FP-based-only mode: only report GC_FRAMEREG_REL slots
            if (reportFpBasedSlotsOnly && slot.Base != GcStackSlotBase.GC_FRAMEREG_REL)
                return;
        }

        reportSlot(slotIndex, slot, gcFlags);
    }

    private uint FindSafePoint(uint codeOffset)
    {
        EnsureDecodedTo(DecodePoints.InterruptibleRanges);

        uint normBreakOffset = TTraits.NormalizeCodeOffset(codeOffset);
        uint numBitsPerOffset = CeilOfLog2(TTraits.NormalizeCodeOffset(_codeLength));

        // TODO(stackref): The native FindSafePoint uses binary search (NarrowSafePointSearch)
        // when numSafePoints > 32. This is a performance optimization only — no correctness impact.
        // Linear scan through safe point offsets from the saved position
        int scanOffset = _safePointBitOffset;
        for (uint i = 0; i < _numSafePoints; i++)
        {
            uint spOffset = (uint)_reader.ReadBits((int)numBitsPerOffset, ref scanOffset);
            if (spOffset == normBreakOffset)
                return i;
            if (spOffset > normBreakOffset)
                break;
        }

        return _numSafePoints; // not found
    }

    #endregion
    #region Helper Methods

    private static uint CeilOfLog2(ulong value)
    {
        Debug.Assert(value > 0);
        value = (value << 1) - 1;
        return (uint)(63 - BitOperations.LeadingZeroCount(value));
    }

    #endregion
}
