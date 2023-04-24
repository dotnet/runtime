// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          GCDecode                                         XX
XX                                                                           XX
XX   Logic to decode the JIT method header and GC pointer tables             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

// ******************************************************************************
// WARNING!!!: This code is also used by SOS in the diagnostics repo. Should be
// updated in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/shared/inc/gcdecoder.cpp
// ******************************************************************************

#ifdef TARGET_X86

/* This file is shared between the VM and JIT/IL and SOS/Strike directories */

#include "gcinfotypes.h"

/*****************************************************************************/
/*
 *   This entire file depends upon GC2_ENCODING being set to 1
 *
 *****************************************************************************/

size_t FASTCALL decodeUnsigned(PTR_CBYTE src, unsigned* val)
{
    LIMITED_METHOD_CONTRACT;

    size_t   size  = 1;
    BYTE     byte  = *src++;
    unsigned value = byte & 0x7f;
    while (byte & 0x80) {
        size++;
        byte    = *src++;
        value <<= 7;
        value  += byte & 0x7f;
    }
    *val = value;
    return size;
}

size_t FASTCALL decodeUDelta(PTR_CBYTE src, unsigned* value, unsigned lastValue)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    unsigned delta;
    size_t size = decodeUnsigned(src, &delta);
    *value = lastValue + delta;
    return size;
}

size_t FASTCALL decodeSigned(PTR_CBYTE src, int* val)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    size_t   size  = 1;
    BYTE     byte  = *src++;
    BYTE     first = byte;
    int      value = byte & 0x3f;
    while (byte & 0x80)
    {
        size++;
        byte = *src++;
        value <<= 7;
        value += byte & 0x7f;
    }
    if (first & 0x40)
        value = -value;
    *val = value;
    return size;
}

/*****************************************************************************/

#if defined(_MSC_VER)
#ifdef HOST_X86
#pragma optimize("tgy", on)
#endif
#endif

PTR_CBYTE FASTCALL decodeHeader(PTR_CBYTE table, UINT32 version, InfoHdr* header)
{
    LIMITED_METHOD_DAC_CONTRACT;

    BYTE nextByte = *table++;
    BYTE encoding = nextByte & 0x7f;
    GetInfoHdr(encoding, header);
    while (nextByte & MORE_BYTES_TO_FOLLOW)
    {
        nextByte = *table++;
        encoding = nextByte & ADJ_ENCODING_MAX;
        // encoding here always corresponds to codes in InfoHdrAdjust set

        if (encoding < NEXT_FOUR_START)
        {
            if (encoding < SET_ARGCOUNT)
            {
                header->frameSize = encoding - SET_FRAMESIZE;
            }
            else if (encoding < SET_PROLOGSIZE)
            {
                header->argCount = encoding - SET_ARGCOUNT;
            }
            else if (encoding < SET_EPILOGSIZE)
            {
                header->prologSize = encoding - SET_PROLOGSIZE;
            }
            else if (encoding < SET_EPILOGCNT)
            {
                header->epilogSize = encoding - SET_EPILOGSIZE;
            }
            else if (encoding < SET_UNTRACKED)
            {
                header->epilogCount = (encoding - SET_EPILOGCNT) / 2;
                header->epilogAtEnd = ((encoding - SET_EPILOGCNT) & 1) == 1;
                assert(!header->epilogAtEnd || (header->epilogCount == 1));
            }
            else if (encoding < FIRST_FLIP)
            {
                header->untrackedCnt = encoding - SET_UNTRACKED;
                _ASSERTE(header->untrackedCnt != HAS_UNTRACKED);
            }
            else switch (encoding)
            {
            default:
                assert(!"Unexpected encoding");
                break;
            case FLIP_EDI_SAVED:
                header->ediSaved ^= 1;
                break;
            case FLIP_ESI_SAVED:
                header->esiSaved ^= 1;
                break;
            case FLIP_EBX_SAVED:
                header->ebxSaved ^= 1;
                break;
            case FLIP_EBP_SAVED:
                header->ebpSaved ^= 1;
                break;
            case FLIP_EBP_FRAME:
                header->ebpFrame ^= 1;
                break;
            case FLIP_INTERRUPTIBLE:
                header->interruptible ^= 1;
                break;
            case FLIP_DOUBLE_ALIGN:
                header->doubleAlign ^= 1;
                break;
            case FLIP_SECURITY:
                header->security ^= 1;
                break;
            case FLIP_HANDLERS:
                header->handlers ^= 1;
                break;
            case FLIP_LOCALLOC:
                header->localloc ^= 1;
                break;
            case FLIP_EDITnCONTINUE:
                header->editNcontinue ^= 1;
                break;
            case FLIP_VAR_PTR_TABLE_SZ:
                header->varPtrTableSize ^= HAS_VARPTR;
                break;
            case FFFF_UNTRACKED_CNT:
                header->untrackedCnt = HAS_UNTRACKED;
                break;
            case FLIP_VARARGS:
                header->varargs ^= 1;
                break;
            case FLIP_PROF_CALLBACKS:
                header->profCallbacks ^= 1;
                break;
            case FLIP_HAS_GENERICS_CONTEXT:
                header->genericsContext ^= 1;
                break;
            case FLIP_GENERICS_CONTEXT_IS_METHODDESC:
                header->genericsContextIsMethodDesc ^= 1;
                break;
            case FLIP_HAS_GS_COOKIE:
                header->gsCookieOffset ^= HAS_GS_COOKIE_OFFSET;
                break;
            case FLIP_SYNC:
                header->syncStartOffset ^= HAS_SYNC_OFFSET;
                break;
            case FLIP_REV_PINVOKE_FRAME:
                header->revPInvokeOffset = INVALID_REV_PINVOKE_OFFSET ? HAS_REV_PINVOKE_FRAME_OFFSET : INVALID_REV_PINVOKE_OFFSET;
                break;

            case NEXT_OPCODE:
                _ASSERTE((nextByte & MORE_BYTES_TO_FOLLOW) && "Must have another code");
                nextByte = *table++;
                encoding = nextByte & ADJ_ENCODING_MAX;
                // encoding here always corresponds to codes in InfoHdrAdjust2 set

                _ASSERTE(encoding < SET_RET_KIND_MAX);
                header->returnKind = (ReturnKind)encoding;
                break;
            }
        }
        else
        {
            unsigned char lowBits;
            switch (encoding >> 4)
            {
            default:
                assert(!"Unexpected encoding");
                break;
            case 5:
                assert(NEXT_FOUR_FRAMESIZE == 0x50);
                lowBits = encoding & 0xf;
                header->frameSize <<= 4;
                header->frameSize += lowBits;
                break;
            case 6:
                assert(NEXT_FOUR_ARGCOUNT == 0x60);
                lowBits = encoding & 0xf;
                header->argCount <<= 4;
                header->argCount += lowBits;
                break;
            case 7:
                if ((encoding & 0x8) == 0)
                {
                    assert(NEXT_THREE_PROLOGSIZE == 0x70);
                    lowBits = encoding & 0x7;
                    header->prologSize <<= 3;
                    header->prologSize += lowBits;
                }
                else
                {
                    assert(NEXT_THREE_EPILOGSIZE == 0x78);
                    lowBits = encoding & 0x7;
                    header->epilogSize <<= 3;
                    header->epilogSize += lowBits;
                }
                break;
            }
        }
    }
    return table;
}

void FASTCALL decodeCallPattern(int          pattern,
                                unsigned *   argCnt,
                                unsigned *   regMask,
                                unsigned *   argMask,
                                unsigned *   codeDelta)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    assert((pattern>=0) && (pattern<80));
    CallPattern pat;
    pat.val    = callPatternTable[pattern];
    *argCnt    = pat.fld.argCnt;
    *regMask   = pat.fld.regMask;      // EBP,EBX,ESI,EDI
    *argMask   = pat.fld.argMask;
    *codeDelta = pat.fld.codeDelta;
}

#define YES HAS_VARPTR

const InfoHdrSmall infoHdrShortcut[128] = {
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
       {  0,  1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    1139  00
       {  0,  1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //  128738  01
       {  0,  1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    3696  02
       {  0,  1, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     402  03
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    4259  04
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          },  //    3379  05
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //    2058  06
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          },  //     728  07
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          },  //     984  08
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3,  0,  0,  0          },  //     606  09
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  0,  0          },  //    1110  0a
       {  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  1,  0          },  //     414  0b
       {  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //    1553  0c
       {  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         },  //     584  0d
       {  1,  2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //    2182  0e
       {  1,  2, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    3445  0f
       {  1,  2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1369  10
       {  1,  2, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     515  11
       {  1,  2, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //   21127  12
       {  1,  2, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    3517  13
       {  1,  2, 3, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     750  14
       {  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1876  15
       {  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          },  //    1665  16
       {  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     729  17
       {  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          },  //     484  18
       {  1,  4, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //     331  19
       {  2,  3, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     361  1a
       {  2,  3, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     964  1b
       {  2,  3, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    3713  1c
       {  2,  3, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     466  1d
       {  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1325  1e
       {  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     712  1f
       {  2,  3, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     588  20
       {  2,  3, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //   20542  21
       {  2,  3, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    3802  22
       {  2,  3, 3, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     798  23
       {  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1900  24
       {  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     385  25
       {  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1617  26
       {  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          },  //    1743  27
       {  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     909  28
       {  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          },  //     602  29
       {  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          },  //     352  2a
       {  2,  6, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     657  2b
       {  2,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         },  //    1283  2c
       {  2,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //    1286  2d
       {  3,  4, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1495  2e
       {  3,  4, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    1989  2f
       {  3,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1154  30
       {  3,  4, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    9300  31
       {  3,  4, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //     392  32
       {  3,  4, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    1720  33
       {  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1246  34
       {  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     800  35
       {  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1179  36
       {  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          },  //    1368  37
       {  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     349  38
       {  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          },  //     505  39
       {  3,  6, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //     629  3a
       {  3,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  2, YES         },  //     365  3b
       {  4,  5, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //     487  3c
       {  4,  5, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    1752  3d
       {  4,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1959  3e
       {  4,  5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    2436  3f
       {  4,  5, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     861  40
       {  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1459  41
       {  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     950  42
       {  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //    1491  43
       {  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          },  //     879  44
       {  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          },  //     408  45
       {  5,  4, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //    4870  46
       {  5,  6, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //     359  47
       {  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          },  //     915  48
       {  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          },  //     412  49
       {  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1288  4a
       {  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //    1591  4b
       {  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         },  //     361  4c
       {  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  1,  0,  0          },  //     623  4d
       {  5,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0,  0          },  //    1239  4e
       {  6,  0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     457  4f
       {  6,  0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     606  50
       {  6,  4, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         },  //    1073  51
       {  6,  4, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         },  //     508  52
       {  6,  6, 1, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //     330  53
       {  6,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //    1709  54
       {  6,  7, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          },  //    1164  55
       {  7,  4, 1, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          },  //     556  56
       {  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         },  //     529  57
       {  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         },  //    1423  58
       {  7,  8, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         },  //    2455  59
       {  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          },  //     956  5a
       {  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         },  //    1399  5b
       {  7,  8, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         },  //     587  5c
       {  7, 10, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         },  //     743  5d
       {  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  0,  0          },  //    1004  5e
       {  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  1, YES         },  //     487  5f
       {  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  2,  0,  0          },  //     337  60
       {  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  3,  0, YES         },  //     361  61
       {  8,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          },  //     560  62
       {  8,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          },  //    1377  63
       {  9,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          },  //     877  64
       {  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          },  //    3041  65
       {  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         },  //     349  66
       { 10,  5, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          },  //    2061  67
       { 10,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          },  //     577  68
       { 11,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          },  //    1195  69
       { 12,  5, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          },  //     491  6a
       { 13,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  0, YES         },  //     627  6b
       { 13,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  1,  0          },  //    1099  6c
       { 13, 10, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         },  //     488  6d
       { 14,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     574  6e
       { 16,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         },  //    1281  6f
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         },  //    1881  70
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     339  71
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0,  0          },  //    2594  72
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          },  //     339  73
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         },  //    2107  74
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         },  //    2372  75
       { 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  6,  0, YES         },  //    1078  76
       { 16,  7, 2, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         },  //     384  77
       { 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1,  4,  1, YES         },  //    1541  78
       { 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2,  4,  1, YES         },  //     975  79
       { 19,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         },  //     546  7a
       { 24,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         },  //     675  7b
       { 45,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          },  //     902  7c
       { 51,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13,  0, YES         },  //     432  7d
       { 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         },  //     361  7e
       { 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11,  0,  0          },  //     703  7f
};

bool InfoHdrSmall::isHeaderMatch(const InfoHdr& target) const
{
#ifdef _ASSERTE
    // target cannot have place-holder values.
    _ASSERTE(target.untrackedCnt != HAS_UNTRACKED &&
                target.varPtrTableSize != HAS_VARPTR &&
                target.gsCookieOffset != HAS_GS_COOKIE_OFFSET &&
                target.syncStartOffset != HAS_SYNC_OFFSET &&
                target.revPInvokeOffset != HAS_REV_PINVOKE_FRAME_OFFSET);
#endif

    // compare two InfoHdr's up to but not including the untrackCnt field
    if (memcmp(this, &target, offsetof(InfoHdr, untrackedCnt)) != 0)
        return false;

    if (untrackedCnt != target.untrackedCnt) {
        if (target.untrackedCnt <= SET_UNTRACKED_MAX)
            return false;
        else if (untrackedCnt != HAS_UNTRACKED)
            return false;
    }

    if (varPtrTableSize != target.varPtrTableSize) {
        if ((varPtrTableSize != 0) != (target.varPtrTableSize != 0))
            return false;
    }

    if (target.gsCookieOffset != INVALID_GS_COOKIE_OFFSET)
        return false;

    if (target.syncStartOffset != INVALID_SYNC_OFFSET)
        return false;

    if (target.revPInvokeOffset!= INVALID_REV_PINVOKE_OFFSET)
        return false;

    return true;
}


const unsigned callCommonDelta[4] = { 6,8,10,12 };

/*
 *  In the callPatternTable each 32-bit unsigned value represents four bytes:
 *
 *  byte0,byte1,byte2,byte3 => codeDelta,argMask,regMask,argCnt
 *  for example 0x0c000301  => codeDelta of 12, argMask of 0,
 *                             regMask of 0x3,  argCnt of 1
 *
 *  Furthermore within the table the following maximum values are in place:
 *
 *  codeDelta <= CP_MAX_CODE_DELTA  // (0x23)
 *  argCnt    <= CP_MAX_ARG_CNT     // (0x02)
 *  argMask   <= CP_MAX_ARG_MASK    // (0x00)
 *
 *  Note that ARG_CNT is the count of pushed args for a nested call site.
 *   And since the first two arguments are always passed in registers
 *   an ARG_CNT of 1 would mean that the nested call site had three arguments
 *
 *  Note that ARG_MASK is the mask of pushed args that contain GC pointers
 *   since the first two arguments are always passed in registers it is
 *   a fairly rare occurrence to push a GC pointer as an argument, since it
 *   only occurs for nested calls, when the third or later argument for the
 *   outer call contains a GC ref.
 *
 *  Additionally the encoding of the regMask uses the following bits:
 *   EDI = 0x1, ESI = 0x2, EBX = 0x4, EBP = 0x8
 *
 */
const unsigned callPatternTable[80] = {               // # of occurrences
    0x0a000200, //   30109
    0x0c000200, //   22970
    0x0c000201, //   19005
    0x0a000300, //   12193
    0x0c000300, //   10614
    0x0e000200, //   10253
    0x10000200, //    9746
    0x0b000200, //    9698
    0x0d000200, //    9625
    0x08000200, //    8909
    0x0c000301, //    8522
    0x11000200, //    7382
    0x0e000300, //    7357
    0x12000200, //    7139
    0x10000300, //    7062
    0x11000300, //    6970
    0x0a000201, //    6842
    0x0a000100, //    6803
    0x0f000200, //    6795
    0x13000200, //    6559
    0x08000300, //    6079
    0x15000200, //    5874
    0x0d000201, //    5492
    0x0c000100, //    5193
    0x0d000300, //    5165
    0x23000200, //    5143
    0x1b000200, //    5035
    0x14000200, //    4872
    0x0f000300, //    4850
    0x0a000700, //    4781
    0x09000200, //    4560
    0x12000300, //    4496
    0x16000200, //    4180
    0x07000200, //    4021
    0x09000300, //    4012
    0x0c000700, //    3988
    0x0c000600, //    3946
    0x0e000100, //    3823
    0x1a000200, //    3764
    0x18000200, //    3744
    0x17000200, //    3736
    0x1f000200, //    3671
    0x13000300, //    3559
    0x0a000600, //    3214
    0x0e000600, //    3109
    0x08000201, //    2984
    0x0b000300, //    2928
    0x0a000301, //    2859
    0x07000100, //    2826
    0x13000100, //    2782
    0x09000301, //    2644
    0x19000200, //    2638
    0x11000700, //    2618
    0x21000200, //    2518
    0x0d000202, //    2484
    0x10000100, //    2480
    0x0f000600, //    2413
    0x14000300, //    2363
    0x0c000500, //    2362
    0x08000301, //    2285
    0x20000200, //    2245
    0x10000700, //    2240
    0x0f000100, //    2236
    0x1e000200, //    2214
    0x0c000400, //    2193
    0x16000300, //    2171
    0x12000600, //    2132
    0x22000200, //    2011
    0x1d000200, //    2011
    0x0c000f00, //    1996
    0x0e000700, //    1971
    0x0a000400, //    1970
    0x09000201, //    1932
    0x10000600, //    1903
    0x15000300, //    1847
    0x0a000101, //    1814
    0x0a000b00, //    1771
    0x0c000601, //    1737
    0x09000700, //    1737
    0x07000300, //    1684
};

#endif // TARGET_X86
