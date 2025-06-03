// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

/// <summary>
/// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a> InfoHdrSmall
/// </summary>
public struct InfoHdr
{
    /* Fields set by the initial table encoding */
    public byte PrologSize { get; private set; }
    public byte EpilogSize { get; private set; }

    public byte EpilogCount { get; private set; }
    public bool EpilogAtEnd { get; private set; }
    public bool EdiSaved { get; private set; }
    public bool EsiSaved { get; private set; }
    public bool EbxSaved { get; private set; }
    public bool EbpSaved { get; private set; }

    public bool EbpFrame { get; private set; }
    public bool Interruptible { get; private set; }
    public bool DoubleAlign { get; private set; }
    public bool Security { get; private set; }
    public bool Handlers { get; private set; }
    public bool LocalAlloc { get; private set; }
    public bool EditAndContinue { get; private set; }
    public bool VarArgs { get; private set; }

    public bool ProfCallbacks { get; private set; }
    public bool GenericsContext { get; private set; }
    public bool GenericsContextIsMethodDesc { get; private set; }
    public ReturnKinds ReturnKind { get; private set; }

    public ushort ArgCount { get; private set; }
    public uint FrameSize { get; private set; }
    public uint UntrackedCount { get; private set; }
    public uint VarPtrTableSize { get; private set; }

    /* Fields not set by the initial table encoding */
    public uint GsCookieOffset { get; private set; } = 0;
    public uint SyncStartOffset { get; private set; } = 0;
    public uint SyndEndOffset { get; private set; } = 0;
    public uint RevPInvokeOffset { get; private set; } = INVALID_REV_PINVOKE_OFFSET;
    public uint NoGCRegionCount { get; private set; } = 0;

    public bool HasArgTabOffset { get; private set; }
    public uint ArgTabOffset { get; private set; }
    public ImmutableArray<int> Epilogs { get; private set; }

    #region Adjustments

    private const byte SET_FRAMESIZE_MAX = 7;
    private const byte SET_ARGCOUNT_MAX = 8;
    private const byte SET_PROLOGSIZE_MAX = 16;
    private const byte SET_EPILOGSIZE_MAX = 10;
    private const byte SET_EPILOGCNT_MAX = 4;
    private const byte SET_UNTRACKED_MAX = 3;
    private const byte SET_RET_KIND_MAX = 3;        // 2 bits for ReturnKind
    private const byte SET_NOGCREGIONS_MAX = 4;
    private const byte ADJ_ENCODING_MAX = 0x7f;     // Maximum valid encoding in a byte
                                                    // Also used to mask off next bit from each encoding byte.
    private const byte MORE_BYTES_TO_FOLLOW = 0x80; // If the High-bit of a header or adjustment byte
                                                    // is set, then there are more adjustments to follow.

    /// <summary>
    // Enum to define codes that are used to incrementally adjust the InfoHdr structure.
    // First set of opcodes
    /// </summary>
    private enum InfoHdrAdjust : byte
    {

        SET_FRAMESIZE = 0, // 0x00
        SET_ARGCOUNT = SET_FRAMESIZE + SET_FRAMESIZE_MAX + 1, // 0x08
        SET_PROLOGSIZE = SET_ARGCOUNT + SET_ARGCOUNT_MAX + 1, // 0x11
        SET_EPILOGSIZE = SET_PROLOGSIZE + SET_PROLOGSIZE_MAX + 1, // 0x22
        SET_EPILOGCNT = SET_EPILOGSIZE + SET_EPILOGSIZE_MAX + 1, // 0x2d
        SET_UNTRACKED = SET_EPILOGCNT + (SET_EPILOGCNT_MAX + 1) * 2, // 0x37

        FIRST_FLIP = SET_UNTRACKED + SET_UNTRACKED_MAX + 1,

        FLIP_EDI_SAVED = FIRST_FLIP, // 0x3b
        FLIP_ESI_SAVED,           // 0x3c
        FLIP_EBX_SAVED,           // 0x3d
        FLIP_EBP_SAVED,           // 0x3e
        FLIP_EBP_FRAME,           // 0x3f
        FLIP_INTERRUPTIBLE,       // 0x40
        FLIP_DOUBLE_ALIGN,        // 0x41
        FLIP_SECURITY,            // 0x42
        FLIP_HANDLERS,            // 0x43
        FLIP_LOCALLOC,            // 0x44
        FLIP_EDITnCONTINUE,       // 0x45
        FLIP_VAR_PTR_TABLE_SZ,    // 0x46 Flip whether a table-size exits after the header encoding
        FFFF_UNTRACKED_CNT,       // 0x47 There is a count (>SET_UNTRACKED_MAX) after the header encoding
        FLIP_VARARGS,             // 0x48
        FLIP_PROF_CALLBACKS,      // 0x49
        FLIP_HAS_GS_COOKIE,       // 0x4A - The offset of the GuardStack cookie follows after the header encoding
        FLIP_SYNC,                // 0x4B
        FLIP_HAS_GENERICS_CONTEXT, // 0x4C
        FLIP_GENERICS_CONTEXT_IS_METHODDESC, // 0x4D
        FLIP_REV_PINVOKE_FRAME,   // 0x4E
        NEXT_OPCODE,              // 0x4F -- see next Adjustment enumeration
        NEXT_FOUR_START = 0x50,
        NEXT_FOUR_FRAMESIZE = 0x50,
        NEXT_FOUR_ARGCOUNT = 0x60,
        NEXT_THREE_PROLOGSIZE = 0x70,
        NEXT_THREE_EPILOGSIZE = 0x78
    };

    /// <summary>
    // Enum to define codes that are used to incrementally adjust the InfoHdr structure.
    // Second set of opcodes, when first code is 0x4F
    /// </summary>
    private enum InfoHdrAdjust2 : uint
    {
        SET_RETURNKIND = 0,  // 0x00-SET_RET_KIND_MAX Set ReturnKind to value
        SET_NOGCREGIONS_CNT = SET_RETURNKIND + SET_RET_KIND_MAX + 1,        // 0x04
        FFFF_NOGCREGION_CNT = SET_NOGCREGIONS_CNT + SET_NOGCREGIONS_MAX + 1 // 0x09 There is a count (>SET_NOGCREGIONS_MAX) after the header encoding
    };

    public enum ReturnKinds
    {
        RT_Scalar = 0,
        RT_Object = 1,
        RT_ByRef = 2,
        RT_Unset = 3, // Encoding 3 means RT_Float on X86
        RT_Scalar_Obj = RT_Object << 2 | RT_Scalar,
        RT_Scalar_ByRef = RT_ByRef << 2 | RT_Scalar,

        RT_Obj_Obj = RT_Object << 2 | RT_Object,
        RT_Obj_ByRef = RT_ByRef << 2 | RT_Object,

        RT_ByRef_Obj = RT_Object << 2 | RT_ByRef,
        RT_ByRef_ByRef = RT_ByRef << 2 | RT_ByRef,

        RT_Illegal = 0xFF
    };

    public static InfoHdr DecodeHeader(Target target, ref TargetPointer offset, uint codeLength)
    {
        byte nextByte = target.Read<byte>(offset++);
        byte encoding = (byte)(nextByte & 0x7Fu);

        if (encoding < 0 || encoding >= INFO_HDR_TABLE.Length)
        {
            throw new InvalidOperationException("Table encoding is invalid.");
        }

        InfoHdr infoHdr = INFO_HDR_TABLE[encoding];

        while ((nextByte & MORE_BYTES_TO_FOLLOW) != 0)
        {
            nextByte = target.Read<byte>(offset++);
            encoding = (byte)(nextByte & ADJ_ENCODING_MAX);

            if (encoding < (uint)InfoHdrAdjust.NEXT_FOUR_START)
            {
                if (encoding < (uint)InfoHdrAdjust.SET_ARGCOUNT)
                {
                    infoHdr.FrameSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_FRAMESIZE);
                }
                else if (encoding < (uint)InfoHdrAdjust.SET_PROLOGSIZE)
                {
                    infoHdr.ArgCount = (byte)(encoding - (uint)InfoHdrAdjust.SET_ARGCOUNT);
                }
                else if (encoding < (uint)InfoHdrAdjust.SET_EPILOGSIZE)
                {
                    infoHdr.PrologSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_PROLOGSIZE);
                }
                else if (encoding < (uint)InfoHdrAdjust.SET_EPILOGCNT)
                {
                    infoHdr.EpilogSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_EPILOGSIZE);
                }
                else if (encoding < (uint)InfoHdrAdjust.SET_UNTRACKED)
                {
                    infoHdr.EpilogCount = (byte)((encoding - (uint)InfoHdrAdjust.SET_EPILOGCNT) / 2);
                    infoHdr.EpilogAtEnd = ((encoding - (uint)InfoHdrAdjust.SET_EPILOGCNT) & 1) == 1 ? true : false;
                    Debug.Assert(!infoHdr.EpilogAtEnd || infoHdr.EpilogCount == 1);
                }
                else if (encoding < (uint)InfoHdrAdjust.FIRST_FLIP)
                {
                    infoHdr.UntrackedCount = (byte)(encoding - (uint)InfoHdrAdjust.SET_UNTRACKED);
                }
                else
                {
                    switch (encoding)
                    {
                        case (byte)InfoHdrAdjust.FLIP_EDI_SAVED:
                            infoHdr.EdiSaved = !infoHdr.EdiSaved;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_ESI_SAVED:
                            infoHdr.EsiSaved = !infoHdr.EsiSaved;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_EBX_SAVED:
                            infoHdr.EbxSaved = !infoHdr.EbxSaved;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_EBP_SAVED:
                            infoHdr.EbpSaved = !infoHdr.EbpSaved;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_EBP_FRAME:
                            infoHdr.EbpFrame = !infoHdr.EbpFrame;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_INTERRUPTIBLE:
                            infoHdr.Interruptible = !infoHdr.Interruptible;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_DOUBLE_ALIGN:
                            infoHdr.DoubleAlign = !infoHdr.DoubleAlign;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_SECURITY:
                            infoHdr.Security = !infoHdr.Security;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_HANDLERS:
                            infoHdr.Handlers = !infoHdr.Handlers;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_LOCALLOC:
                            infoHdr.LocalAlloc = !infoHdr.LocalAlloc;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_EDITnCONTINUE:
                            infoHdr.EditAndContinue = !infoHdr.EditAndContinue;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_VAR_PTR_TABLE_SZ:
                            infoHdr.VarPtrTableSize ^= HAS_VARPTR;
                            break;
                        case (byte)InfoHdrAdjust.FFFF_UNTRACKED_CNT:
                            infoHdr.UntrackedCount = HAS_UNTRACKED;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_VARARGS:
                            infoHdr.VarArgs = !infoHdr.VarArgs;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_PROF_CALLBACKS:
                            infoHdr.ProfCallbacks = !infoHdr.ProfCallbacks;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_HAS_GENERICS_CONTEXT:
                            infoHdr.GenericsContext = !infoHdr.GenericsContext;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_GENERICS_CONTEXT_IS_METHODDESC:
                            infoHdr.GenericsContextIsMethodDesc = !infoHdr.GenericsContextIsMethodDesc;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_HAS_GS_COOKIE:
                            infoHdr.GsCookieOffset ^= HAS_GS_COOKIE_OFFSET;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_SYNC:
                            infoHdr.SyncStartOffset ^= HAS_SYNC_OFFSET;
                            break;
                        case (byte)InfoHdrAdjust.FLIP_REV_PINVOKE_FRAME:
                            infoHdr.RevPInvokeOffset ^= INVALID_REV_PINVOKE_OFFSET ^ HAS_REV_PINVOKE_FRAME_OFFSET;
                            break;

                        case (byte)InfoHdrAdjust.NEXT_OPCODE:
                            nextByte = target.Read<byte>(offset++);
                            encoding = (byte)(nextByte & ADJ_ENCODING_MAX);

                            // encoding here always corresponds to codes in InfoHdrAdjust2 set
                            if (encoding <= SET_RET_KIND_MAX)
                            {
                                infoHdr.ReturnKind = (ReturnKinds)encoding;
                            }
                            else if (encoding < (int)InfoHdrAdjust2.FFFF_NOGCREGION_CNT)
                            {
                                infoHdr.NoGCRegionCount = (uint)encoding - (uint)InfoHdrAdjust2.SET_NOGCREGIONS_CNT;
                            }
                            else if (encoding == (int)InfoHdrAdjust2.FFFF_NOGCREGION_CNT)
                            {
                                infoHdr.NoGCRegionCount = HAS_NOGCREGIONS;
                            }
                            else
                            {
                                throw new BadImageFormatException("Unexpected gcinfo header encoding");
                            }
                            break;
                        default:
                            throw new BadImageFormatException("Unexpected gcinfo header encoding");
                    }
                }
            }
            else
            {
                byte lowBits;
                switch (encoding >> 4)
                {
                    case 5:
                        lowBits = (byte)(encoding & 0xf);
                        infoHdr.FrameSize <<= 4;
                        infoHdr.FrameSize += lowBits;
                        break;
                    case 6:
                        lowBits = (byte)(encoding & 0xf);
                        infoHdr.ArgCount <<= 4;
                        infoHdr.ArgCount += lowBits;
                        break;
                    case 7:
                        if ((encoding & 0x8) == 0)
                        {
                            lowBits = (byte)(encoding & 0x7);
                            infoHdr.PrologSize <<= 3;
                            infoHdr.PrologSize += lowBits;
                        }
                        else
                        {
                            lowBits = (byte)(encoding & 0x7);
                            infoHdr.EpilogSize <<= 3;
                            infoHdr.EpilogSize += lowBits;
                        }
                        break;
                    default:
                        throw new BadImageFormatException("Unexpected gcinfo header encoding");
                }
            }
        }

        if (infoHdr.UntrackedCount == HAS_UNTRACKED)
        {
            infoHdr.HasArgTabOffset = true;
            infoHdr.UntrackedCount = target.GCDecodeUnsigned(ref offset);
        }
        if (infoHdr.VarPtrTableSize == HAS_VARPTR)
        {
            infoHdr.HasArgTabOffset = true;
            infoHdr.VarPtrTableSize = target.GCDecodeUnsigned(ref offset);
        }
        if (infoHdr.GsCookieOffset == HAS_GS_COOKIE_OFFSET)
        {
            infoHdr.GsCookieOffset = target.GCDecodeUnsigned(ref offset);
        }
        if (infoHdr.SyncStartOffset == HAS_SYNC_OFFSET)
        {
            infoHdr.SyncStartOffset = target.GCDecodeUnsigned(ref offset);
            infoHdr.SyndEndOffset = target.GCDecodeUnsigned(ref offset);
        }
        if (infoHdr.RevPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET)
        {
            infoHdr.RevPInvokeOffset = target.GCDecodeUnsigned(ref offset);
        }
        if (infoHdr.NoGCRegionCount == HAS_NOGCREGIONS)
        {
            infoHdr.HasArgTabOffset = true;
            infoHdr.NoGCRegionCount = target.GCDecodeUnsigned(ref offset);
        }
        else if (infoHdr.NoGCRegionCount > 0)
        {
            infoHdr.HasArgTabOffset = true;
        }

        ImmutableArray<int>.Builder epilogsBuilder = ImmutableArray.CreateBuilder<int>();
        if (infoHdr.EpilogCount > 1 || (infoHdr.EpilogCount != 0 && !infoHdr.EpilogAtEnd))
        {
            uint offs = 0;

            for (int i = 0; i < infoHdr.EpilogCount; i++)
            {
                offs = target.GCDecodeUDelta(ref offset, offs);
                epilogsBuilder.Add((int)offs);
            }
        }
        else
        {
            if (infoHdr.EpilogCount != 0)
                epilogsBuilder.Add((int)(codeLength - infoHdr.EpilogSize));
        }
        infoHdr.Epilogs = epilogsBuilder.ToImmutable();

        if (infoHdr.HasArgTabOffset)
        {
            infoHdr.ArgTabOffset = target.GCDecodeUnsigned(ref offset);
        }

        /* Sanity Checks */
        Debug.Assert(infoHdr.PrologSize + (infoHdr.EpilogCount * infoHdr.EpilogSize) <= codeLength);
        Debug.Assert(infoHdr.EpilogCount == 1 || !infoHdr.EpilogAtEnd);

        Debug.Assert(infoHdr.UntrackedCount <= infoHdr.ArgCount + infoHdr.FrameSize);

        Debug.Assert(infoHdr.EbpSaved || !(infoHdr.EbpFrame || infoHdr.DoubleAlign));
        Debug.Assert(!infoHdr.EbpFrame || !infoHdr.DoubleAlign);
        Debug.Assert(infoHdr.EbpFrame || !infoHdr.Security);
        Debug.Assert(infoHdr.EbpFrame || !infoHdr.Handlers);
        Debug.Assert(infoHdr.EbpFrame || !infoHdr.LocalAlloc);
        Debug.Assert(infoHdr.EbpFrame || !infoHdr.EditAndContinue);

        return infoHdr;
    }

    #endregion
    #region EncodingTable

    public const uint HAS_VARPTR = 0xFFFFFFFF;
    public const uint HAS_UNTRACKED = 0xFFFFFFFF;
    public const uint HAS_GS_COOKIE_OFFSET = 0xFFFFFFFF;
    public const uint HAS_SYNC_OFFSET = 0xFFFFFFFF;
    public const uint INVALID_REV_PINVOKE_OFFSET = unchecked((uint)-1);
    public const uint HAS_REV_PINVOKE_FRAME_OFFSET = unchecked((uint)-2);
    public const uint HAS_NOGCREGIONS = 0xFFFFFFFF;
    private const uint YES = HAS_VARPTR;

    private InfoHdr(
        byte prologSize,
        byte epilogSize,
        byte epilogCount,
        byte epilogAtEnd,
        byte ediSaved,
        byte esiSaved,
        byte ebxSaved,
        byte ebpSaved,
        byte ebpFrame,
        byte interruptible,
        byte doubleAlign,
        byte security,
        byte handlers,
        byte localAlloc,
        byte editAndContinue,
        byte varArgs,
        byte profCallbacks,
        byte genericsContext,
        byte genericsContextIsMethodDesc,
        byte returnKind,
        ushort argCount,
        uint frameSize,
        uint untrackedCount,
        uint varPtrTableSize)
    {
        PrologSize = prologSize;
        EpilogSize = epilogSize;
        EpilogCount = epilogCount;
        EpilogAtEnd = epilogAtEnd == 1;
        EdiSaved = ediSaved == 1;
        EsiSaved = esiSaved == 1;
        EbxSaved = ebxSaved == 1;
        EbpSaved = ebpSaved == 1;

        EbpFrame = ebpFrame == 1;
        Interruptible = interruptible == 1;
        DoubleAlign = doubleAlign == 1;
        Security = security == 1;
        Handlers = handlers == 1;
        LocalAlloc = localAlloc == 1;
        EditAndContinue = editAndContinue == 1;
        VarArgs = varArgs == 1;

        ProfCallbacks = profCallbacks == 1;
        GenericsContext = genericsContext == 1;
        GenericsContextIsMethodDesc = genericsContextIsMethodDesc == 1;
        ReturnKind = (ReturnKinds)returnKind;

        ArgCount = argCount;
        FrameSize = frameSize;
        UntrackedCount = untrackedCount;
        VarPtrTableSize = varPtrTableSize;

        HasArgTabOffset = false;
        ArgTabOffset = 0;
        Epilogs = [];
    }

    private static InfoHdr[] INFO_HDR_TABLE =
    {
    //        Prolog size
    //        |
    //        |   Epilog size
    //        |   |
    //        |   |  Epilog count
    //        |   |  |
    //        |   |  |  Epilog at end
    //        |   |  |  |
    //        |   |  |  |  EDI saved
    //        |   |  |  |  |
    //        |   |  |  |  |  ESI saved
    //        |   |  |  |  |  |
    //        |   |  |  |  |  |  EBX saved
    //        |   |  |  |  |  |  |
    //        |   |  |  |  |  |  |  EBP saved
    //        |   |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  EBP-frame
    //        |   |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  Interruptible method
    //        |   |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  doubleAlign
    //        |   |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  security flag
    //        |   |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  handlers
    //        |   |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  localloc
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  edit and continue
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  varargs
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  ProfCallbacks
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  genericsContext
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  genericsContextIsMethodDesc
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  returnKind
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  Arg count
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |                                 Counted occurrences
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   Frame size                    |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |                             |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   untrackedCnt              |   Header encoding
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |                         |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |  varPtrTable            |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |                     |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |  gsCookieOffs       |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |   |                 |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |   | syncOffs        |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |   |  |  |           |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |   |  |  |           |   |
    //        |   |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |   |   |   |   |  |  |           |   |
    //        v   v  v  v  v  v  v  v  v  v  v  v  v  v  v  v  v  v  v  v  v   v   v   v   v  v  v           v   v
        new(  0,  1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1139  00
        new(  0,  1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //  128738  01
        new(  0,  1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3696  02
        new(  0,  1, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     402  03
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    4259  04
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    3379  05
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //    2058  06
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          ),  //     728  07
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     984  08
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3,  0,  0,  0          ),  //     606  09
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  0,  0          ),  //    1110  0a
        new(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  1,  0          ),  //     414  0b
        new(  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1553  0c
        new(  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         ),  //     584  0d
        new(  1,  2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    2182  0e
        new(  1,  2, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3445  0f
        new(  1,  2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1369  10
        new(  1,  2, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     515  11
        new(  1,  2, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //   21127  12
        new(  1,  2, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3517  13
        new(  1,  2, 3, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     750  14
        new(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1876  15
        new(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1665  16
        new(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     729  17
        new(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     484  18
        new(  1,  4, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     331  19
        new(  2,  3, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     361  1a
        new(  2,  3, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     964  1b
        new(  2,  3, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3713  1c
        new(  2,  3, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     466  1d
        new(  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1325  1e
        new(  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     712  1f
        new(  2,  3, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     588  20
        new(  2,  3, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //   20542  21
        new(  2,  3, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3802  22
        new(  2,  3, 3, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     798  23
        new(  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1900  24
        new(  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     385  25
        new(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1617  26
        new(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1743  27
        new(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     909  28
        new(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          ),  //     602  29
        new(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     352  2a
        new(  2,  6, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     657  2b
        new(  2,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //    1283  2c
        new(  2,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1286  2d
        new(  3,  4, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1495  2e
        new(  3,  4, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1989  2f
        new(  3,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1154  30
        new(  3,  4, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    9300  31
        new(  3,  4, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     392  32
        new(  3,  4, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1720  33
        new(  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1246  34
        new(  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     800  35
        new(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1179  36
        new(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1368  37
        new(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     349  38
        new(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     505  39
        new(  3,  6, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     629  3a
        new(  3,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  2, YES         ),  //     365  3b
        new(  4,  5, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     487  3c
        new(  4,  5, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1752  3d
        new(  4,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1959  3e
        new(  4,  5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    2436  3f
        new(  4,  5, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     861  40
        new(  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1459  41
        new(  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     950  42
        new(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1491  43
        new(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //     879  44
        new(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     408  45
        new(  5,  4, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    4870  46
        new(  5,  6, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     359  47
        new(  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //     915  48
        new(  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          ),  //     412  49
        new(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1288  4a
        new(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1591  4b
        new(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         ),  //     361  4c
        new(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  1,  0,  0          ),  //     623  4d
        new(  5,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0,  0          ),  //    1239  4e
        new(  6,  0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     457  4f
        new(  6,  0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     606  50
        new(  6,  4, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //    1073  51
        new(  6,  4, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //     508  52
        new(  6,  6, 1, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     330  53
        new(  6,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1709  54
        new(  6,  7, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    1164  55
        new(  7,  4, 1, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     556  56
        new(  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //     529  57
        new(  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //    1423  58
        new(  7,  8, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //    2455  59
        new(  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //     956  5a
        new(  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //    1399  5b
        new(  7,  8, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //     587  5c
        new(  7, 10, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         ),  //     743  5d
        new(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  0,  0          ),  //    1004  5e
        new(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  1, YES         ),  //     487  5f
        new(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  2,  0,  0          ),  //     337  60
        new(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  3,  0, YES         ),  //     361  61
        new(  8,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     560  62
        new(  8,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    1377  63
        new(  9,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     877  64
        new(  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    3041  65
        new(  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //     349  66
        new( 10,  5, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          ),  //    2061  67
        new( 10,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     577  68
        new( 11,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          ),  //    1195  69
        new( 12,  5, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     491  6a
        new( 13,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  0, YES         ),  //     627  6b
        new( 13,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  1,  0          ),  //    1099  6c
        new( 13, 10, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         ),  //     488  6d
        new( 14,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     574  6e
        new( 16,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //    1281  6f
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //    1881  70
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     339  71
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0,  0          ),  //    2594  72
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          ),  //     339  73
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //    2107  74
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //    2372  75
        new( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  6,  0, YES         ),  //    1078  76
        new( 16,  7, 2, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //     384  77
        new( 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1,  4,  1, YES         ),  //    1541  78
        new( 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2,  4,  1, YES         ),  //     975  79
        new( 19,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //     546  7a
        new( 24,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //     675  7b
        new( 45,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     902  7c
        new( 51,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13,  0, YES         ),  //     432  7d
        new( 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     361  7e
        new( 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11,  0,  0          ),  //     703  7f
    };

    #endregion
};
