// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.Reflection.ReadyToRun;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal class GcInfoDecoder<TTraits> : IGCInfoHandle where TTraits : IGCInfoTraits
{
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

        int bitOffset = 0;
        DecodeHeader(ref bitOffset);
        DecodeBody(ref bitOffset);
    }

    #region Decoding Methods

    private void DecodeBody(ref int bitOffset)
    {
        if (PartiallyInterruptibleGCSupported)
        {
            _numSafePoints = _reader.DecodeVarLengthUnsigned(TTraits.NUM_SAFE_POINTS_ENCBASE, ref bitOffset);
        }

        _numInterruptibleRanges = _slimHeader ?
            0 :
            _reader.DecodeVarLengthUnsigned(TTraits.NUM_INTERRUPTIBLE_RANGES_ENCBASE, ref bitOffset);

        DecodeSafePoints(ref bitOffset);
        DecodeInterruptibleRanges(ref bitOffset);
        DecodeSlotTable(ref bitOffset);
    }

    private void DecodeSlotTable(ref int bitOffset)
    {
        if (_reader.ReadBits(1, ref bitOffset) != 0)
        {
            _numRegisters = _reader.DecodeVarLengthUnsigned(TTraits.NUM_REGISTERS_ENCBASE, ref bitOffset);
        }

        uint numStackSlots = 0;
        if (_reader.ReadBits(1, ref bitOffset) != 0)
        {
            numStackSlots = _reader.DecodeVarLengthUnsigned(TTraits.NUM_STACK_SLOTS_ENCBASE, ref bitOffset);
            _numUntrackedSlots = _reader.DecodeVarLengthUnsigned(TTraits.NUM_UNTRACKED_SLOTS_ENCBASE, ref bitOffset);
        }

        _numSlots = _numRegisters + numStackSlots + _numUntrackedSlots;
        _slots = new List<GcSlotDesc>((int)_numSlots);

        // Decode register slots
        if (_numRegisters > 0)
        {
            uint regNum = _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_ENCBASE, ref bitOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);

            _slots.Add(GcSlotDesc.CreateRegisterSlot(regNum, flags));

            for (int i = 1; i < _numRegisters; i++)
            {
                if (flags != 0)
                {
                    regNum = _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_ENCBASE, ref bitOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);
                }
                else
                {
                    regNum += _reader.DecodeVarLengthUnsigned(TTraits.REGISTER_DELTA_ENCBASE, ref bitOffset) + 1;
                }

                _slots.Add(GcSlotDesc.CreateRegisterSlot(regNum, flags));
            }
        }

        // Decode stack slots
        if (numStackSlots > 0)
        {
            GcStackSlotBase spBase = (GcStackSlotBase)_reader.ReadBits(2, ref bitOffset);
            int normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);

            _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));

            for (int i = 1; i < numStackSlots; i++)
            {
                spBase = (GcStackSlotBase)_reader.ReadBits(2, ref bitOffset);

                if (flags != 0)
                {
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);
                }
                else
                {
                    normSpOffset += _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref bitOffset) + 1;
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                }

                _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));
            }
        }

        // Decode untracked slots
        if (_numUntrackedSlots > 0)
        {
            GcStackSlotBase spBase = (GcStackSlotBase)_reader.ReadBits(2, ref bitOffset);
            int normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);

            _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));

            for (int i = 1; i < _numUntrackedSlots; i++)
            {
                spBase = (GcStackSlotBase)_reader.ReadBits(2, ref bitOffset);

                if (flags != 0)
                {
                    normSpOffset = _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)_reader.ReadBits(2, ref bitOffset);
                }
                else
                {
                    normSpOffset += _reader.DecodeVarLengthSigned(TTraits.STACK_SLOT_DELTA_ENCBASE, ref bitOffset) + 1;
                    spOffset = TTraits.DenormalizeStackSlot(normSpOffset);
                }

                _slots.Add(GcSlotDesc.CreateStackSlot(spOffset, spBase, flags));
            }
        }
    }

    private void DecodeInterruptibleRanges(ref int bitOffset)
    {
        if (_numInterruptibleRanges == 0)
            return;

        uint prevEndOffset = 0;

        uint lastInterruptibleRangeStopOffsetNormalized = 0;

        _interruptibleRanges = new List<InterruptibleRange>((int)_numInterruptibleRanges);
        for (uint i = 0; i < _numInterruptibleRanges; i++)
        {
            uint normStartDelta = _reader.DecodeVarLengthUnsigned(TTraits.INTERRUPTIBLE_RANGE_DELTA1_ENCBASE, ref bitOffset);
            uint normStopDelta = _reader.DecodeVarLengthUnsigned(TTraits.INTERRUPTIBLE_RANGE_DELTA2_ENCBASE, ref bitOffset) + 1;

            uint rangeStartOffsetNormalized = lastInterruptibleRangeStopOffsetNormalized + normStartDelta;
            uint rangeStopOffsetNormalized = rangeStartOffsetNormalized + normStopDelta;

            uint rangeStartOffset = TTraits.DenormalizeCodeOffset(rangeStartOffsetNormalized);
            uint rangeStopOffset = TTraits.DenormalizeCodeOffset(rangeStopOffsetNormalized);

            Debug.Assert(rangeStartOffset < rangeStopOffset);
            Debug.Assert(rangeStartOffset >= prevEndOffset);

            lastInterruptibleRangeStopOffsetNormalized = rangeStopOffsetNormalized;

            _interruptibleRanges.Add(new(rangeStartOffset, rangeStopOffset));
        }
    }

    private void DecodeSafePoints(ref int bitOffset)
    {
        // skip over safe point data
        uint numBitsPerOffset = CeilOfLog2(TTraits.NormalizeCodeOffset(_codeLength));
        bitOffset += (int)(numBitsPerOffset * _numSafePoints);
    }

    private void DecodeHeader(ref int bitOffset)
    {
        _slimHeader = _reader.ReadBits(1, ref bitOffset) == 0;

        if (!_slimHeader)
        {
            DecodeFatHeader(ref bitOffset);
        }
        else
        {
            DecodeSlimHeader(ref bitOffset);
        }
    }

    private void DecodeSlimHeader(ref int bitOffset)
    {
        if (_reader.ReadBits(1, ref bitOffset) != 0)
        {
            _headerFlags = GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER;
            _stackBaseRegister = TTraits.DenormalizeStackBaseRegister(0);
        }
        else
        {
            _headerFlags = default;
            _stackBaseRegister = TTraits.NO_STACK_BASE_REGISTER;
        }

        _codeLength = TTraits.DenormalizeCodeLength(_reader.DecodeVarLengthUnsigned(TTraits.CODE_LENGTH_ENCBASE, ref bitOffset));

        // predecoding the rest of slim header does not require any reading.
        _validRangeStart = 0;
        _validRangeEnd = 0;
        _gsCookieStackSlot = TTraits.NO_GS_COOKIE;
        _pspSymStackSlot = TTraits.NO_PSP_SYM;
        _genericsInstContextStackSlot = TTraits.NO_GENERICS_INST_CONTEXT;
        _sizeOfEnCPreservedArea = TTraits.NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
        _reversePInvokeFrameStackSlot = TTraits.NO_REVERSE_PINVOKE_FRAME;

        // on ARM64 there is an extra ENC FixedStackFrame field

        if (TTraits.HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA)
            _fixedStackParameterScratchArea = 0;
    }

    private void DecodeFatHeader(ref int bitOffset)
    {
        _headerFlags = (GcInfoHeaderFlags)_reader.ReadBits((int)GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE, ref bitOffset);

        _codeLength = TTraits.DenormalizeCodeLength(_reader.DecodeVarLengthUnsigned(TTraits.CODE_LENGTH_ENCBASE, ref bitOffset));

        if (_headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE))
        {
            // Note that normalization as a code offset can be different than
            // normalization as code length
            uint normCodeLength = TTraits.NormalizeCodeLength(_codeLength);

            // Decode prolog/epilog information
            uint normPrologSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
            uint normEpilogSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_EPILOG_SIZE_ENCBASE, ref bitOffset);

            _validRangeStart = TTraits.DenormalizeCodeOffset(normPrologSize);
            _validRangeEnd = TTraits.DenormalizeCodeOffset(normCodeLength - normEpilogSize);
            Debug.Assert(_validRangeStart < _validRangeEnd);
        }
        else if ((_headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
        {
            // Decode prolog information
            uint normPrologSize = _reader.DecodeVarLengthUnsigned(TTraits.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
            _validRangeStart = TTraits.DenormalizeCodeOffset(normPrologSize);
            // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
            _validRangeEnd = _validRangeStart + 1;
        }
        else
        {
            _validRangeStart = 0;
            _validRangeEnd = 0;
        }

        _gsCookieStackSlot = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.GS_COOKIE_STACK_SLOT_ENCBASE, ref bitOffset)) :
            TTraits.NO_GS_COOKIE;

        _pspSymStackSlot = TTraits.NO_PSP_SYM;

        _genericsInstContextStackSlot = (_headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE, ref bitOffset)) :
            TTraits.NO_GENERICS_INST_CONTEXT;

        _stackBaseRegister = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER) ?
            TTraits.DenormalizeStackBaseRegister(_reader.DecodeVarLengthUnsigned(TTraits.STACK_BASE_REGISTER_ENCBASE, ref bitOffset)) :
            TTraits.NO_STACK_BASE_REGISTER;

        _sizeOfEnCPreservedArea = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_INFO) ?
            _reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, ref bitOffset) :
            TTraits.NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;

        _reversePInvokeFrameStackSlot = _headerFlags.HasFlag(GcInfoHeaderFlags.GC_INFO_REVERSE_PINVOKE_FRAME) ?
            TTraits.DenormalizeStackSlot(_reader.DecodeVarLengthSigned(TTraits.REVERSE_PINVOKE_FRAME_ENCBASE, ref bitOffset)) :
            TTraits.NO_REVERSE_PINVOKE_FRAME;

        if (TTraits.HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA)
        {
            _fixedStackParameterScratchArea =
                TTraits.DenormalizeSizeOfStackArea(_reader.DecodeVarLengthUnsigned(TTraits.SIZE_OF_STACK_AREA_ENCBASE, ref bitOffset));
        }
    }

    #endregion
    #region Access Methods

    public uint GetCodeLength()
    {
        return _codeLength;
    }

    public IReadOnlyList<InterruptibleRange> GetInterruptibleRanges()
    {
        return _interruptibleRanges;
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
