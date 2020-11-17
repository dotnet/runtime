// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************
 *                               GCDumpX86.cpp
 */

/*****************************************************************************/
#ifdef TARGET_X86
/*****************************************************************************/

#ifndef TARGET_UNIX
#include "utilcode.h"           // For _ASSERTE()
#endif //!TARGET_UNIX
#include "gcdump.h"


/*****************************************************************************/

#define castto(var,typ) (*(typ *)&var)

#define sizeto(typ,mem) (offsetof(typ, mem) + sizeof(((typ*)0)->mem))

#define CALLEE_SAVED_REG_MAXSZ  (4*sizeof(int)) // EBX,ESI,EDI,EBP

/*****************************************************************************/

const char *        RegName(unsigned reg)
{
    static const char * const regNames[] =
    {
        "EAX",
        "ECX",
        "EDX",
        "EBX",

        "ESP",
        "EBP",
        "ESI",
        "EDI"
    };

    _ASSERTE(reg < (sizeof(regNames)/sizeof(regNames[0])));

    return regNames[reg];
}

const char *        CalleeSavedRegName(unsigned reg)
{
    static const char * const regNames[] =
    {
        "EDI",
        "ESI",
        "EBX",
        "EBP"
    };

    _ASSERTE(reg < (sizeof(regNames)/sizeof(regNames[0])));

    return regNames[reg];
}

/*****************************************************************************/

size_t            GCDump::DumpInfoHdr (PTR_CBYTE      gcInfoBlock,
                                       InfoHdr*       header,
                                       unsigned *     methodSize,
                                       bool           verifyGCTables)
{
    unsigned        count;
    PTR_CBYTE       table       = gcInfoBlock;
    PTR_CBYTE       tableStart  = table;
    PTR_CBYTE       bp          = table;

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xFEEF);

    /* Get the method size */

    table += decodeUnsigned(table, methodSize);

    table = decodeHeader(table, gcInfoVersion, header);

    BOOL hasArgTabOffset = FALSE;
    if (header->untrackedCnt == HAS_UNTRACKED)
    {
        hasArgTabOffset = TRUE;
        table += decodeUnsigned(table, &count);
        header->untrackedCnt = count;
    }

    if (header->varPtrTableSize == HAS_VARPTR)
    {
        hasArgTabOffset = TRUE;
        table += decodeUnsigned(table, &count);
        header->varPtrTableSize = count;
    }

    if (header->gsCookieOffset == HAS_GS_COOKIE_OFFSET)
    {
        table += decodeUnsigned(table, &count);
        header->gsCookieOffset = count;
    }

    if (header->syncStartOffset == HAS_SYNC_OFFSET)
    {
        table += decodeUnsigned(table, &count);
        header->syncStartOffset = count;
        table += decodeUnsigned(table, &count);
        header->syncEndOffset = count;
    }

    if (header->revPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET)
    {
        table += decodeUnsigned(table, &count);
        header->revPInvokeOffset = count;
    }

    //
    // First print out all the basic information
    //

    gcPrintf("    method      size   = %04X\n", *methodSize);
    gcPrintf("    prolog      size   = %2u \n", header->prologSize);
    gcPrintf("    epilog      size   = %2u \n", header->epilogSize);
    gcPrintf("    epilog     count   = %2u \n", header->epilogCount);
    gcPrintf("    epilog      end    = %s  \n", header->epilogAtEnd   ? "yes" : "no");

    gcPrintf("    callee-saved regs  = ");
    if (header->ediSaved) gcPrintf("EDI ");
    if (header->esiSaved) gcPrintf("ESI ");
    if (header->ebxSaved) gcPrintf("EBX ");
    if (header->ebpSaved) gcPrintf("EBP ");
    gcPrintf("\n");

    gcPrintf("    ebp frame          = %s  \n", header->ebpFrame      ? "yes" : "no");
    gcPrintf("    fully interruptible= %s  \n", header->interruptible ? "yes" : "no");
    gcPrintf("    double align       = %s  \n", header->doubleAlign   ? "yes" : "no");
    gcPrintf("    arguments size     = %2u DWORDs\n", header->argCount);
    gcPrintf("    stack frame size   = %2u DWORDs\n", header->frameSize);
    gcPrintf("    untracked count    = %2u \n", header->untrackedCnt);
    gcPrintf("    var ptr tab count  = %2u \n", header->varPtrTableSize);

    //
    // Now display optional information
    //

    if (header->security)       gcPrintf("    security check obj = yes\n");
    if (header->handlers)       gcPrintf("    exception handlers = yes\n");
    if (header->localloc)       gcPrintf("    localloc           = yes\n");
    if (header->editNcontinue)  gcPrintf("    edit & continue    = yes\n");
    if (header->profCallbacks)  gcPrintf("    profiler callbacks = yes\n");
    if (header->varargs)        gcPrintf("    varargs            = yes\n");
    if (header->gsCookieOffset != INVALID_GS_COOKIE_OFFSET)
                                gcPrintf("    GuardStack cookie  = [%s%u]\n",
                                          header->ebpFrame ? "EBP-" : "ESP+", header->gsCookieOffset);
    if (header->syncStartOffset != INVALID_SYNC_OFFSET)
                                gcPrintf("    Sync region = [%u,%u]\n",
                                          header->syncStartOffset, header->syncEndOffset);

    if  (header->epilogCount > 1 || (header->epilogCount != 0 &&
                                     header->epilogAtEnd == 0))
    {
        if (verifyGCTables)
            _ASSERTE(*castto(table, unsigned short *)++ == 0xFACE);

        unsigned offs = 0;

        for (unsigned i = 0; i < header->epilogCount; i++)
        {
            table += decodeUDelta(table, &offs, offs);
            gcPrintf("    epilog #%2u    at   %04X\n", i, offs);
        }
    }
    else
    {
        if  (header->epilogCount)
            gcPrintf("    epilog        at   %04X\n", (*methodSize - header->epilogSize));
    }

    if (hasArgTabOffset)
    {
        unsigned argTabOffset;
        table += decodeUnsigned(table, &argTabOffset);
        gcPrintf("    argTabOffset = %x  \n", argTabOffset);
    }

    {
        size_t cur  = 0;
        size_t last = table-bp;
        while (cur < last)
        {
            size_t amount = last - cur;
            if (amount>5)
                amount = 5;

            DumpEncoding(bp+cur, amount);
        gcPrintf("\n");

            cur += amount;
        }
    }

    return  (table - tableStart);
}

/*****************************************************************************/

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
size_t              GCDump::DumpGCTable(PTR_CBYTE      table,
                                        const InfoHdr& header,
                                        unsigned       methodSize,
                                        bool           verifyGCTables)
{
    int             sz;
    PTR_CBYTE       tableStart = table;
    PTR_CBYTE       bp;

    unsigned        count;
    unsigned        curOffs;

//#if!TGT_x86
//    _ASSERTE(!"NYI");
//#endif

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEF);

    unsigned        calleeSavedRegs = 0;
    if (header.doubleAlign)
    {
        calleeSavedRegs = 0;
        if (header.ediSaved) calleeSavedRegs++;
        if (header.esiSaved) calleeSavedRegs++;
        if (header.ebxSaved) calleeSavedRegs++;
    }

    /* Dump the untracked frame variable table */

    count = header.untrackedCnt;

    int lastStkOffs = 0;
    while (count-- > 0)
    {
        int       stkOffsDelta;
        unsigned  lowBits;

        char      reg = header.ebpFrame ? 'B' : 'S';

        sz    = (unsigned int)decodeSigned(table, &stkOffsDelta);
        int stkOffs = lastStkOffs - stkOffsDelta;
        lastStkOffs = stkOffs;

        table = DumpEncoding(table, sz);

        _ASSERTE(0 == ~OFFSET_MASK % sizeof(uint32_t));

        lowBits  =   OFFSET_MASK & stkOffs;
        stkOffs &=  ~OFFSET_MASK;

        assert(!header.doubleAlign || stkOffs >= 0);

        if  (header.doubleAlign &&
             unsigned(stkOffs) >= sizeof(int)*(header.frameSize+calleeSavedRegs))
        {
            reg = 'B';
            stkOffs -= sizeof(int)*(header.frameSize+calleeSavedRegs);
            _ASSERTE(stkOffs >= (int) (2*sizeof(int)));
        }

        if  (stkOffs < 0)
            gcPrintf("            [E%cP-%02XH] ", reg, -stkOffs);
        else
            gcPrintf("            [E%cP+%02XH] ", reg, +stkOffs);

        gcPrintf("an untracked %s%s local\n",
                    (lowBits & pinned_OFFSET_FLAG)  ? "pinned " : "",
                    (lowBits & byref_OFFSET_FLAG)   ? "byref"   : ""
           );
    }

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xCAFE);

    /* Dump the frame variable lifetime table */

    count   = header.varPtrTableSize;
    curOffs = 0;

    while (count-- > 0)
    {
        unsigned varOffs;
        unsigned begOffs;
        unsigned endOffs;
        unsigned lowBits;

        bp = table;

        table += decodeUnsigned(table, &varOffs);
        table += decodeUDelta  (table, &begOffs, curOffs);
        table += decodeUDelta  (table, &endOffs, begOffs);

        DumpEncoding(bp, table-bp);

        _ASSERTE(0 == ~OFFSET_MASK % sizeof(uint32_t));

        lowBits  = varOffs & 0x3;
        varOffs &= ~OFFSET_MASK;

        // [EBP+0] is the return address - cant be a var
        _ASSERTE(!header.ebpFrame || varOffs);

        curOffs = begOffs;

        DumpOffset(begOffs);
        gcPrintf("..");
        DumpOffset(endOffs);
        gcPrintf("  [E%s%02XH] a ", header.ebpFrame ? "BP-" : "SP+",
                                  varOffs);

        gcPrintf("%s%s pointer\n",
                    (lowBits & byref_OFFSET_FLAG) ? "byref " : "",
#ifndef FEATURE_EH_FUNCLETS
                    (lowBits & this_OFFSET_FLAG)  ? "this"   : ""
#else
                    (lowBits & pinned_OFFSET_FLAG)  ? "pinned"   : ""
#endif
           );

        _ASSERTE(endOffs <= methodSize);
    }

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xBABE);

    /* Dump the pointer table */

    curOffs = 0;
    bp      = table;

    if  (header.interruptible)
    {
        //
        // Dump the Fully Interruptible pointer table
        //
        unsigned argCnt = 0;
        bool     isThis = false;
        bool     iptr   = false;

        for (;;)
        {
            unsigned    isPop;
            unsigned    argOffs;
            unsigned    val = *table++;

            _ASSERTE(curOffs <= methodSize);

            if  (!(val & 0x80))
            {
                /* A small 'regPtr' encoding */

                curOffs += val & 0x7;

                DumpEncoding(bp, table-bp); bp = table;
                DumpOffsetEx(curOffs);
                gcPrintf("        reg %s becoming %s", RegName(((val >> 3) & 7)),
                                                     (val & 0x40) ? "live"
                                                                  : "dead");
                if (isThis)
                    gcPrintf(" 'this'");
                if (iptr)
                    gcPrintf(" (iptr)");
                gcPrintf("\n");

                isThis = false;
                iptr   = false;
                continue;
            }

            /* This is probably an argument push/pop */

            argOffs = (val & 0x38) >> 3;

            /* 6 [110] and 7 [111] are reserved for other encodings */

            if  (argOffs < 6)
            {
                /* A small argument encoding */

                curOffs += (val & 0x07);
                isPop    = (val & 0x40);

            ARG:

                if  (isPop)
                {
                    // A Pop of 0, means little-delta

                    if (argOffs != 0)
                    {
                        _ASSERTE(header.ebpFrame || argOffs <= argCnt);

                        DumpEncoding(bp, table-bp); bp = table;
                        DumpOffsetEx(curOffs);

                        gcPrintf("        pop %2d ", argOffs);
                        if  (!header.ebpFrame)
                        {
                            argCnt -= argOffs;
                            gcPrintf("args (%d)", argCnt);
                        }
                        else
                            gcPrintf("ptrs");

                        gcPrintf("\n");
                    }
                }
                else
                {
                    _ASSERTE(header.ebpFrame || argOffs >= argCnt);

                    DumpEncoding(bp, table-bp); bp = table;
                    DumpOffsetEx(curOffs);

                    gcPrintf("        push ptr %2d", argOffs);
                    if  (!header.ebpFrame)
                    {
                        argCnt = argOffs+1;
                        gcPrintf("  (%d)", argCnt);
                    }
                    if (isThis)
                        gcPrintf(" 'this'");
                    if (iptr)
                        gcPrintf(" (iptr)");
                    gcPrintf("\n");

                    isThis = false;
                    iptr   = false;
                }

                continue;
            }
            else if (argOffs == 6)
            {
                if (val & 0x40)
                {
                    /* Bigger delta  000=8,001=16,010=24,...,111=64 */

                    curOffs += (((val & 0x07) + 1) << 3);
                }
                else
                {
                    /* non-ptr arg push */

                    curOffs += (val & 0x07);
#ifndef UNIX_X86_ABI
                    // For x86/Linux, non-ptr arg pushes can be reported even for EBP frames
                    _ASSERTE(!header.ebpFrame);
#endif // UNIX_X86_ABI
                    argCnt++;

                    DumpEncoding(bp, table-bp); bp = table;
                    DumpOffsetEx(curOffs);

                    gcPrintf("        push non-ptr (%d)\n", argCnt);
                }

                continue;
            }

            /* argOffs was 7 [111] which is reserved for the larger encodings */

            _ASSERTE(argOffs==7);

            switch (val)
            {
            case 0xFF:
                goto DONE_REGTAB;

            case 0xBC:
                isThis = true;
                break;

            case 0xBF:
                iptr = true;
                break;

            case 0xB8:
                table   += decodeUnsigned(table, &val);
                curOffs += val;
                break;

            case 0xF8:
            case 0xFC:
                isPop  = val & 0x04;
                table += decodeUnsigned(table, &argOffs);
                goto ARG;

            case 0xFD:
                table += decodeUnsigned(table, &argOffs);
                assert(argOffs);

                DumpEncoding(bp, table-bp); bp = table;
                DumpOffsetEx(curOffs);

                gcPrintf("        kill args %2d\n", argOffs);
                break;

            case 0xF9:
                table  += decodeUnsigned(table, &argOffs);
                argCnt += argOffs;
                break;

            default:
                gcPrintf("Unexpected special code %04X\n", val);
                _ASSERTE(!"");
            }
        }
    }
    else if (header.ebpFrame)        // interruptible is false
    {
        //
        // Dump the Partially Interruptible, EBP-frame method, pointer table
        //

        for (;;)
        {
            unsigned        argMask = 0, byrefArgMask = 0;
            unsigned        regMask, byrefRegMask = 0;

            unsigned        argCnt = 0;
            PTR_CBYTE       argTab = NULL;
            unsigned        argTabSize;

            unsigned        val, nxt;

            /* Get the next byte and check for a 'special' entry */

            unsigned        encType = *table++;

            _ASSERTE(curOffs <= methodSize);

            switch (encType)
            {

            default:

                /* A tiny or small call entry */

                val = encType;

                if ((val & 0x80) == 0x00)
                {
                    if (val & 0x0F)
                    {
                        /* A tiny call entry */

                        curOffs += (val & 0x0F);
                        regMask  = (val & 0x70) >> 4;
                        argMask  = 0;
                    }
                    else
                    {
                        DumpEncoding(bp, table-bp); bp = table;

                        gcPrintf("            thisptr in ");
                        if (val & 0x10)
                            gcPrintf("EDI\n");
                        else if (val & 0x20)
                            gcPrintf("ESI\n");
                        else if (val & 0x40)
                            gcPrintf("EBX\n");
                        else
                            _ASSERTE(!"Reserved GC encoding");

                        continue;
                    }
                }
                else
                {
                    /* A small call entry */

                    curOffs += (val & 0x7F);
                    val      = *table++;
                    regMask  = val >> 5;
                    argMask  = val & 0x1F;
                }
                break;

            case 0xFD:  // medium encoding

                argMask  = *table++;
                val      = *table++;
                argMask |= (val & 0xF0) << 4;
                nxt      = *table++;
                curOffs += (val & 0x0F) + ((nxt & 0x1F) << 4);
                regMask  = nxt >> 5;                   // EBX,ESI,EDI

                break;

            case 0xF9:  // medium encoding with byrefs

                curOffs += *table++;
                val      = *table++;
                argMask  = val & 0x1F;
                regMask  = val >> 5;
                val      = *table++;
                byrefArgMask    = val & 0x1F;
                byrefRegMask    = val >> 5;

                break;

            case 0xFE:  // large encoding
            case 0xFA:  // large encoding with byrefs

                val         = *table++;
                regMask     = val & 0x7;
                byrefRegMask= val >> 4;
                curOffs    += *PTR_DWORD(table); table += sizeof(DWORD);
                argMask     = *PTR_DWORD(table); table += sizeof(DWORD);
                if (encType == 0xFA) // read byrefArgMask
                    {byrefArgMask = *PTR_DWORD(table); table += sizeof(DWORD);}

                break;

            case 0xFB:  // huge encoding

                val         = *table++;
                regMask     = val & 0x7;
                byrefRegMask= val >> 4;
                curOffs     = *PTR_DWORD(table); table += sizeof(DWORD);
                argCnt      = *PTR_DWORD(table); table += sizeof(DWORD);
                argTabSize  = *PTR_DWORD(table); table += sizeof(DWORD);
                argTab      = table; table += argTabSize;

                break;

            case 0xFF:
                goto DONE_REGTAB;
            }

            /*
                Here we have the following values:

                curOffs      ...    the code offset of the call
                regMask      ...    mask of live pointer register variables
                argMask      ...    bitmask of pushed pointer arguments
                byrefRegMask ...    byref qualifier for regMask
                byrefArgMask ...    byrer qualifier for argMask
             */

            _ASSERTE((byrefArgMask & argMask) == byrefArgMask);
            _ASSERTE((byrefRegMask & regMask) == byrefRegMask);

            DumpEncoding(bp, table-bp); bp = table;
            DumpOffsetEx(curOffs);

            gcPrintf("        call [ ");

            if (regMask & 1)
                gcPrintf("EDI%c", (byrefRegMask & 1) ? '\'' : ' ');
            if (regMask & 2)
                gcPrintf("ESI%c", (byrefRegMask & 2) ? '\'' : ' ');
            if (regMask & 4)
                gcPrintf("EBX%c", (byrefRegMask & 4) ? '\'' : ' ');

            if (!header.ebpFrame)
            {
                if (regMask & 8)
                    gcPrintf("EBP ");
            }

            if  (argCnt)
            {
                gcPrintf("] ptrArgs=[");

                do
                {
                    argTab += decodeUnsigned(argTab, &val);

#ifndef FEATURE_EH_FUNCLETS
                    assert((val & this_OFFSET_FLAG) == 0);
#endif
                    unsigned  stkOffs = val & ~byref_OFFSET_FLAG;
                    unsigned  lowBit  = val &  byref_OFFSET_FLAG;

                    gcPrintf("%u%s", stkOffs, lowBit ? "i" : "");
                    if  (argCnt > 1)
                        gcPrintf(" ");
                }
                while (--argCnt);
                assert(argTab == table);

                gcPrintf("]");
            }
            else
            {
                gcPrintf("] argMask=%02X", argMask);

                if (byrefArgMask) gcPrintf(" (iargs=%02X)", byrefArgMask);
            }

            gcPrintf("\n");
        }
    }
    else // interruptible is false, ebpFrame is false
    {
        //
        // Dump the Partially Interruptible, EBP-less method, pointer table
        //
        unsigned lastSkip = 0;
        unsigned imask    = 0;

        for (;;)
        {
            unsigned    val = *table++;

            _ASSERTE(curOffs <= methodSize);

            if  (!(val & 0x80))
            {
                if (!(val & 0x40))
                {
                    if (!(val & 0x20))
                    {
                        //
                        // push    000DDDDD          push one item, 5-bit delta
                        //

                        curOffs += val & 0x1F;

                        DumpEncoding(bp, table-bp); bp = table;
                        DumpOffsetEx(curOffs);

                        gcPrintf("        push\n");
                    }
                    else
                    {
                        //
                        // push    00100000 [pushCount]     ESP push multiple items
                        //

                        unsigned pushCount;

                        assert(val == 0x20);
                        table    += decodeUnsigned(table, &pushCount);

                        DumpEncoding(bp, table-bp); bp = table;
                        DumpOffsetEx(curOffs);

                        gcPrintf("       push %d\n", pushCount);
                    }
                }
                else
                {
                    unsigned    popSize;
                    unsigned    skip;

                    if ((val & 0x3f) == 0)
                    {
                        //
                        // skip    01000000 [Delta]  Skip arbitrary sized delta
                        //

                        table   += decodeUnsigned(table, &skip);
                        curOffs += skip;
                        lastSkip = skip;
                    }
                    else
                    {
                        //
                        //  pop     01CCDDDD         pop CC items, 4-bit delta
                        //

                        popSize = (val & 0x30) >> 4;
                        skip    =  val & 0x0f;
                        curOffs += skip;

                        if (popSize > 0)
                        {
                            DumpEncoding(bp, table-bp); bp = table;
                            DumpOffsetEx(curOffs);

                            gcPrintf("        pop %d\n", popSize);
                        }
                        else
                            lastSkip = skip;
                    }
                }
            }
            else
            {
                unsigned    callArgCnt;
                unsigned    callRegMask;
                bool        callPndTab = false;
                unsigned    callPndMask = 0;
                unsigned    callPndTabCnt = 0, callPndTabSize = 0;

                switch ((val & 0x70) >> 4)
                {
                default:
                    //
                    // call    1PPPPPPP          Call Pattern, P=[0..79]
                    //
                    decodeCallPattern((val & 0x7f), &callArgCnt,  &callRegMask,
                                                    &callPndMask, &lastSkip);
                    curOffs += lastSkip;

                PRINT_CALL:

                    DumpEncoding(bp, table-bp); bp = table;
                    DumpOffsetEx(curOffs);

                    gcPrintf("        call %d [ ", callArgCnt);

                    unsigned    iregMask, iargMask;

                    iregMask = imask & 0xF;
                    iargMask = imask >> 4;

                    assert((callRegMask & 0x0F) == callRegMask);
                    if (callRegMask & 1)
                        gcPrintf("EDI%c", (iregMask & 1) ? '\'' : ' ');
                    if (callRegMask & 2)
                        gcPrintf("ESI%c", (iregMask & 2) ? '\'' : ' ');
                    if (callRegMask & 4)
                        gcPrintf("EBX%c", (iregMask & 4) ? '\'' : ' ');
                    if (callRegMask & 8)
                        gcPrintf("EBP%c", (iregMask & 8) ? '\'' : ' ');
                    gcPrintf("]");

                    if (callPndTab)
                    {
#if defined(_DEBUG) && !defined(STRIKE)
                // note: _ASSERTE is a no-op for strike
                        PTR_CBYTE offsStart = table;
#endif
                        gcPrintf(" argOffs(%d) =", callPndTabCnt);
                        for (unsigned i=0; i < callPndTabCnt; i++)
                        {
                            unsigned pndOffs;
                            table += decodeUnsigned(table, &pndOffs);
                            gcPrintf(" %4X", pndOffs);
                        }
                        _ASSERTE(offsStart + callPndTabSize == table);
                        bp = table;
                    }
                    else
                    {
                        if (callPndMask)
                            gcPrintf(" argMask=%02X", callPndMask);
                        if (iargMask)
                            gcPrintf(" (iargs=%02X)", iargMask);
                    }
                    gcPrintf("\n");

                    imask = lastSkip = 0;
                    break;

                  case 5:
                    //
                    // call    1101RRRR DDCCCMMM  Call RegMask=RRRR,ArgCnt=CCC,
                    //                        ArgMask=MMM Delta=commonDelta[DD]
                    //
                    callRegMask     = val & 0xf;    // EBP,EBX,ESI,EDI
                    val             = *table++;
                    callPndMask     = (val & 0x7);
                    callArgCnt      = (val >> 3) & 0x7;
                    lastSkip        = callCommonDelta[val>>6];
                    curOffs        += lastSkip;

                    goto PRINT_CALL;

                  case 6:
                    //
                    // call    1110RRRR [ArgCnt] [ArgMask]
                    //                          Call ArgCnt,RegMask=RRR,ArgMask
                    //
                    callRegMask = val & 0xf;    // EBP,EBX,ESI,EDI
                    table += decodeUnsigned(table, &callArgCnt);
                    table += decodeUnsigned(table, &callPndMask);
                    goto PRINT_CALL;

                  case 7:
                    switch (val & 0x0C)
                    {
                    case 0x00:
                        assert(val == 0xF0);
                        /*  iptr 11110000 [IPtrMask] Arbitrary Interior Pointer Mask */
                        table += decodeUnsigned(table, &imask);
                        DumpEncoding(bp, table-bp); bp = table;
                        gcPrintf("            iptrMask = %02X\n", imask);
                        break;

                    case 0x04:
                        DumpEncoding(bp, table-bp); bp = table;
                        gcPrintf("            thisptr in %s\n", CalleeSavedRegName(val&0x3));
                        break;

                    case 0x08:
                        val             = *table++;
                        callRegMask     = val & 0xF;
                        imask           = val >> 4;
                        lastSkip        = *PTR_DWORD(table); table += sizeof(DWORD);
                        curOffs        += lastSkip;
                        callArgCnt      = *PTR_DWORD(table); table += sizeof(DWORD);
                        callPndTabCnt   = *PTR_DWORD(table); table += sizeof(DWORD);
                        callPndTabSize  = *PTR_DWORD(table); table += sizeof(DWORD);
                        callPndTab      = true;
                        goto PRINT_CALL;

                    case 0x0C:
                        assert(val==0xff);
                        goto DONE_REGTAB;
                        break;

                    default:
                        _ASSERTE(!"reserved GC encoding");
                        break;
                    }
                    break;
                }
            }
        }
    }

DONE_REGTAB:

    _ASSERTE(curOffs <= methodSize);

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEB);

    _ASSERTE(table > bp);

    DumpEncoding(bp, table-bp);
//  gcPrintf("     ");
    gcPrintf("\n");

    return  (table - tableStart);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


/*****************************************************************************/

void                GCDump::DumpPtrsInFrame(PTR_CBYTE   gcInfoBlock,
                                            PTR_CBYTE   codeBlock,
                                            unsigned    offs,
                                            bool        verifyGCTables)
{
    PTR_CBYTE       table = gcInfoBlock;

    size_t          methodSize;
    size_t          stackSize;
    size_t          prologSize;
    size_t          epilogSize;
    unsigned        epilogCnt;
    BOOL            epilogEnd;
    size_t          argSize;
    BOOL            secCheck;
    BOOL            dblAlign;

    if (verifyGCTables)
        _ASSERTE(*castto(table, unsigned short *)++ == 0xFEEF);

    /* Get hold of the method size */

    unsigned int methodSizeTemp;
    table += decodeUnsigned(table, &methodSizeTemp);
    methodSize = methodSizeTemp;

    //
    // New style InfoBlk Header
    //
    // Typically only uses one-byte to store everything.
    //
    InfoHdr header;
    table = decodeHeader(table, gcInfoVersion, &header);

    if (header.untrackedCnt == HAS_UNTRACKED)
    {
        unsigned count;
        table += decodeUnsigned(table, &count);
        header.untrackedCnt = count;
    }
    if (header.varPtrTableSize == HAS_VARPTR)
    {
        unsigned count;
        table += decodeUnsigned(table, &count);
        header.varPtrTableSize = count;
    }
    if (header.gsCookieOffset == HAS_GS_COOKIE_OFFSET)
    {
        unsigned offset;
        table += decodeUnsigned(table, &offset);
        header.gsCookieOffset = offset;
        _ASSERTE(offset != INVALID_GS_COOKIE_OFFSET);
    }
    if (header.syncStartOffset == HAS_SYNC_OFFSET)
    {
        unsigned offset;
        table += decodeUnsigned(table, &offset);
        header.syncStartOffset = offset;
        _ASSERTE(offset != INVALID_SYNC_OFFSET);
        table += decodeUnsigned(table, &offset);
        header.syncEndOffset = offset;
        _ASSERTE(offset != INVALID_SYNC_OFFSET);
    }
    if (header.revPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET)
    {
        unsigned offset;
        table += decodeUnsigned(table, &offset);
        header.revPInvokeOffset = offset;
        _ASSERTE(offset != INVALID_REV_PINVOKE_OFFSET);
    }

    prologSize = header.prologSize;
    epilogSize = header.epilogSize;
    epilogCnt  = header.epilogCount;
    epilogEnd  = header.epilogAtEnd;
    secCheck   = header.security;
    dblAlign   = header.doubleAlign;
    argSize    = header.argCount * 4;
    stackSize  = header.frameSize;

#ifdef DEBUG
    if  (offs == 0)
    {
        gcPrintf("    method      size = %04X\n", methodSize);
        gcPrintf("    stack frame size = %3u \n",  stackSize);
        gcPrintf("    prolog      size = %3u \n", prologSize);
        gcPrintf("    epilog      size = %3u \n", epilogSize);
        gcPrintf("    epilog      end  = %s  \n", epilogEnd ? "yes" : "no");
        gcPrintf("    epilog     count = %3u \n", epilogCnt );
        gcPrintf("    security         = %s  \n", secCheck  ? "yes" : "no");
        gcPrintf("    dblAlign         = %s  \n", dblAlign  ? "yes" : "no");
        gcPrintf("    untracked count  = %3u \n", header.untrackedCnt);
        gcPrintf("    var ptr tab count= %3u \n", header.varPtrTableSize);
        gcPrintf("\n");
    }
#endif

    /* Are we within the prolog of the method? */

    if  (offs < prologSize)
    {
        gcPrintf("    Offset %04X is within the method's prolog\n", offs);
        return;
    }

    /* Are we within an epilog of the method? */

    if  (epilogCnt)
    {
        unsigned    eps;

        if  (epilogCnt > 1 || !epilogEnd)
        {
            if (verifyGCTables)
                _ASSERTE(*castto(table, unsigned short *)++ == 0xFACE);

            unsigned prevEps = 0;
            for (unsigned i = 0; i < epilogCnt; i++)
            {
                table += decodeUDelta(table, &eps, prevEps);

                if ((offs >= eps) && (offs <  eps + epilogSize))
                        goto EPILOG_MSG;
            }
        }
        else
        {
            eps = (int)(methodSize - epilogSize);
            if ((offs >= eps) && (offs <  eps + epilogSize))
            {
EPILOG_MSG:     gcPrintf("    Offset %04X is within the method's epilog"
                       " (%02X bytes into it)\n", offs, offs - eps);
                return;
            }
        }
    }
    gcPrintf("    Offset %04X is within the method's body\n", offs);
}

/*****************************************************************************/
#endif // TARGET_X86
/*****************************************************************************/
