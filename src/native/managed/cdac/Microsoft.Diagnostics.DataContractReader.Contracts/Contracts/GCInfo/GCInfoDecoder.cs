// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal class GcInfoDecoder<TTraits> : IGCInfoHandle where TTraits : IGCInfoTraits
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
    private int _reversePInvokeFrameStackSlot;
    private uint _fixedStackParameterScratchArea;

    /* Fields */

    private uint _numSafePoints;
    private uint _numInterruptibleRanges;
    private List<InterruptibleRange> _interruptibleRanges = [];

    /* Slot Table Fields */
    private uint _numRegisters;
    private uint _numUntrackedSlots;
    private uint _numSlots;
    private List<GcSlotDesc> _slots = [];

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

        IEnumerable<DecodePoints> slotTable = DecodeSlotTable();
        foreach (DecodePoints dp in slotTable)
            yield return dp;
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
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);
                }
                else
                {
                    normSpOffset += _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset) + 1;
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
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref _bitOffset);
                }
                else
                {
                    normSpOffset += _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref _bitOffset) + 1;
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

            _interruptibleRanges.Add(new(rangeStartOffset, rangeStopOffset));
        }

        yield break;
    }

    private IEnumerable<DecodePoints> DecodeSafePoints()
    {
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

        _sizeOfEnCPreservedArea = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_INFO) ?
            _reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, ref _bitOffset) :
            TTraits.NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
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

    #endregion
    #region Helper Methods

    private static uint CeilOfLog2(ulong value)
    {
        uint result = (uint)Math.Ceiling(Math.Log2(value));
        return result;
    }

    #endregion
}
