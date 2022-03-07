// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************
 *                                  GCDump.cpp
 *
 * Defines functions to display the GCInfo as defined by the GC-encoding
 * spec. The GC information may be either dynamically created by a
 * Just-In-Time compiler conforming to the standard code-manager spec,
 * or may be persisted by a managed native code compiler conforming
 * to the standard code-manager spec.
 */
#include "common.h"

#if (defined(_DEBUG) || defined(DACCESS_COMPILE))

#include "gcenv.h"
#include "varint.h"
#include "gcinfo.h"
#include "gcdump.h"

/*****************************************************************************/

#ifdef DACCESS_COMPILE
static void DacNullPrintf(const char* , ...) {}
#endif

GCDump::GCDump()
{
#ifndef DACCESS_COMPILE
    // By default, use the standard printf function to dump
    GCDump::gcPrintf = (printfFtn) ::printf;
#else
    // Default for DAC is a no-op.
    GCDump::gcPrintf = DacNullPrintf;
#endif
}



/*****************************************************************************/

static const char * const calleeSaveRegMaskBitNumberToName[] =
{
#if defined(TARGET_X86)
    "EBX",
    "ESI",
    "EDI",
    "EBP",
#elif defined(TARGET_AMD64)
    "RBX",
    "RSI",
    "RDI",
    "RBP",
    "R12",
    "R13",
    "R14",
    "R15"
#elif defined(TARGET_ARM)
    "R4",
    "R5",
    "R6",
    "R7",
    "R8",
    "R9",
    "R10",
    "R11",
    "LR",
#elif defined(TARGET_ARM64)
    "LR",
    "X19",
    "X20",
    "X21",
    "X22",
    "X23",
    "X24",
    "X25",
    "X26",
    "X27",
    "X28",
    "FP",
#else
#error unknown architecture
#endif
};

char const * GetReturnKindString(GCInfoHeader::MethodReturnKind returnKind)
{
    switch (returnKind)
    {
    case GCInfoHeader::MRK_ReturnsScalar:   return "scalar";
    case GCInfoHeader::MRK_ReturnsObject:   return "object";
    case GCInfoHeader::MRK_ReturnsByref:    return "byref";
    case GCInfoHeader::MRK_ReturnsToNative: return "native";
#if defined(TARGET_ARM64)
    case GCInfoHeader::MRK_Scalar_Obj:      return "{scalar, object}";
    case GCInfoHeader::MRK_Obj_Obj:         return "{object, object}";
    case GCInfoHeader::MRK_Byref_Obj:       return "{byref, object}";
    case GCInfoHeader::MRK_Scalar_Byref:    return "{scalar, byref}";
    case GCInfoHeader::MRK_Obj_Byref:       return "{object, byref}";
    case GCInfoHeader::MRK_Byref_Byref:     return "{byref, byref}";
#endif // defined(TARGET_ARM64)
    default:                                return "???";
    }
}

char const * GetFramePointerRegister()
{
#if defined(TARGET_X86)
    return "EBP";
#elif defined(TARGET_AMD64)
    return "RBP";
#elif defined(TARGET_ARM)
    return "R7";
#elif defined(TARGET_ARM64)
    return "FP";
#else
#error unknown architecture
#endif
}

char const * GetStackPointerRegister()
{
#if defined(TARGET_X86)
    return "ESP";
#elif defined(TARGET_AMD64)
    return "RSP";
#elif defined(TARGET_ARM) || defined(TARGET_ARM64)
    return "SP";
#else
#error unknown architecture
#endif
}

size_t FASTCALL   GCDump::DumpInfoHeader (PTR_UInt8      gcInfo,
                                          Tables *       pTables,
                                          GCInfoHeader * pHeader         /* OUT */
                                          )
{
    size_t    headerSize = 0;
    PTR_UInt8 gcInfoStart = gcInfo;
    PTR_UInt8 pbStackChanges = 0;
    PTR_UInt8 pbUnwindInfo = 0;

    unsigned unwindInfoBlobOffset = VarInt::ReadUnsigned(gcInfo);
    bool    inlineUnwindInfo = (unwindInfoBlobOffset == 0);

    if (inlineUnwindInfo)
    {
        // it is inline..
        pbUnwindInfo = gcInfo;
    }
    else
    {
        // The offset was adjusted by 1 to reserve the 0 encoding for the inline case, so we re-adjust it to
        // the actual offset here.
        pbUnwindInfo = pTables->pbUnwindInfoBlob + unwindInfoBlobOffset - 1;
    }

    // @TODO: decode all funclet headers as well.
    pbStackChanges = pHeader->DecodeHeader(0, pbUnwindInfo, &headerSize );

    if (inlineUnwindInfo)
        gcInfo += headerSize;

    unsigned epilogCount = pHeader->GetEpilogCount();
    bool     epilogAtEnd = pHeader->IsEpilogAtEnd();

    gcPrintf("   prologSize:     %d\n", pHeader->GetPrologSize());
    if (pHeader->HasVaryingEpilogSizes())
        gcPrintf("   epilogSize:     (varies)\n");
    else
        gcPrintf("   epilogSize:     %d\n", pHeader->GetFixedEpilogSize());

    gcPrintf("   epilogCount:    %d %s\n", epilogCount, epilogAtEnd ? "[end]" : "");
    gcPrintf("   returnKind:     %s\n", GetReturnKindString(pHeader->GetReturnKind()));
    gcPrintf("   frameKind:      %s", pHeader->HasFramePointer() ? GetFramePointerRegister() : GetStackPointerRegister());
#ifdef TARGET_AMD64
    if (pHeader->HasFramePointer())
        gcPrintf(" offset: %d", pHeader->GetFramePointerOffset());
#endif // HOST_AMD64
    gcPrintf("\n");
    gcPrintf("   frameSize:      %d\n", pHeader->GetFrameSize());

    if (pHeader->HasDynamicAlignment()) {
        gcPrintf("   alignment:      %d\n", (1 << pHeader->GetDynamicAlignment()));
        if (pHeader->GetParamPointerReg() != RN_NONE) {
            gcPrintf("   paramReg:       %d\n", pHeader->GetParamPointerReg());
        }
    }

    gcPrintf("   savedRegs:      ");
    CalleeSavedRegMask savedRegs = pHeader->GetSavedRegs();
    CalleeSavedRegMask mask = (CalleeSavedRegMask) 1;
    for (int i = 0; i < RBM_CALLEE_SAVED_REG_COUNT; i++)
    {
        if (savedRegs & mask)
        {
            gcPrintf("%s ", calleeSaveRegMaskBitNumberToName[i]);
        }
        mask = (CalleeSavedRegMask)(mask << 1);
    }
    gcPrintf("\n");

#ifdef TARGET_ARM
    gcPrintf("   parmRegsPushedCount: %d\n", pHeader->ParmRegsPushedCount());
#endif

#ifdef TARGET_X86
    gcPrintf("   returnPopSize:  %d\n", pHeader->GetReturnPopSize());
    if (pHeader->HasStackChanges())
    {
        // @TODO: need to read the stack changes string that follows
        ASSERT(!"NYI -- stack changes for ESP frames");
    }
#endif

    if (pHeader->ReturnsToNative())
    {
        gcPrintf("   reversePinvokeFrameOffset: 0x%02x\n", pHeader->GetReversePinvokeFrameOffset());
    }


    if (!epilogAtEnd && !pHeader->IsFunclet())
    {
        gcPrintf("   epilog offsets: ");
        unsigned previousOffset = 0;
        for (unsigned idx = 0; idx < epilogCount; idx++)
        {
            unsigned newOffset = previousOffset + VarInt::ReadUnsigned(gcInfo);
            gcPrintf("0x%04x ", newOffset);
            if (pHeader->HasVaryingEpilogSizes())
                gcPrintf("(%u bytes) ", VarInt::ReadUnsigned(gcInfo));
            previousOffset = newOffset;
        }
        gcPrintf("\n");
    }

    return gcInfo - gcInfoStart;
}

// TODO: Can we unify this code with ReportLocalSlot in RHCodeMan.cpp?
void GCDump::PrintLocalSlot(uint32_t slotNum, GCInfoHeader const * pHeader)
{
    char const * baseReg;
    int32_t offset;

    if (pHeader->HasFramePointer())
    {
        baseReg = GetFramePointerRegister();
#ifdef TARGET_ARM
        offset = pHeader->GetFrameSize() - ((slotNum + 1) * POINTER_SIZE);
#elif defined(TARGET_ARM64)
        if (pHeader->AreFPLROnTop())
        {
            offset = -(int32_t)((slotNum + 1) * POINTER_SIZE);
        }
        else
        {
            offset = (slotNum + 2) * POINTER_SIZE;
        }
#elif defined(TARGET_X86)
        offset = -pHeader->GetPreservedRegsSaveSize() - (slotNum * POINTER_SIZE);
#elif defined(TARGET_AMD64)
        if (pHeader->GetFramePointerOffset() == 0)
        {
            offset = -pHeader->GetPreservedRegsSaveSize() - (slotNum * POINTER_SIZE);
        }
        else
        {
            offset = (slotNum * POINTER_SIZE);
        }
#else
#error unknown architecture
#endif
    }
    else
    {
        baseReg = GetStackPointerRegister();
        offset = pHeader->GetFrameSize() - ((slotNum + 1) * POINTER_SIZE);
    }

    char const * sign = "+";
    if (offset < 0)
    {
        sign = "-";
        offset = -offset;
    }
    gcPrintf("local slot 0n%d, [%s%s%02X]\n", slotNum, baseReg, sign, offset);
}

// Reads a 7-bit-encoded register mask:
// - 0RRRRRRR for non-ARM64 registers and { x0-x6 } ARM64 registers
// - 1RRRRRRR 0RRRRRRR for { x0-x13 } ARM64 registers
// - 1RRRRRRR 1RRRRRRR 000RRRRR for { x0-x15, xip0, xip1, lr } ARM64 registers
// Returns the number of bytes read.
size_t ReadRegisterMaskBy7Bit(PTR_UInt8 pCursor, uint32_t* pMask)
{
    uint32_t byte0 = *pCursor;
    if (!(byte0 & 0x80))
    {
        *pMask = byte0;
        return 1;
    }

#if defined(TARGET_ARM64)
    uint32_t byte1 = *(pCursor + 1);
    if (!(byte1 & 0x80))
    {
        // XOR with 0x80 discards the most significant bit of byte0
        *pMask = (byte1 << 7) ^ byte0 ^ 0x80;
        return 2;
    }

    uint32_t byte2 = *(pCursor + 2);
    if (!(byte2 & 0x80))
    {
        // XOR with 0x4080 discards the most significant bits of byte0 and byte1
        *pMask = (byte2 << 14) ^ (byte1 << 7) ^ byte0 ^ 0x4080;
        return 3;
    }
#endif

    UNREACHABLE_MSG("Register mask is too long");
}

void GCDump::DumpCallsiteString(uint32_t callsiteOffset, PTR_UInt8 pbCallsiteString,
                                GCInfoHeader const * pHeader)
{
    gcPrintf("%04x: ", callsiteOffset);

    int count = 0;
    uint8_t b;
    PTR_UInt8 pCursor = pbCallsiteString;

    bool last = false;
    bool first = true;

    do
    {
        if (!first)
            gcPrintf("      ");

        first = false;

        b = *pCursor++;
        last = ((b & 0x20) == 0x20);

        switch (b & 0xC0)
        {
        case 0x00:
            {
                // case 2 -- "register set"
                gcPrintf("%02x          | 2  ", b);
#ifdef TARGET_ARM
                if (b & CSR_MASK_R4) { gcPrintf("R4 "); count++; }
                if (b & CSR_MASK_R5) { gcPrintf("R5 "); count++; }
                if (b & CSR_MASK_R6) { gcPrintf("R6 "); count++; }
                if (b & CSR_MASK_R7) { gcPrintf("R7 "); count++; }
                if (b & CSR_MASK_R8) { gcPrintf("R8 "); count++; }
#elif defined(TARGET_ARM64)
                uint16_t regs = (b & 0xF);
                if (b & 0x10) { regs |= (*pCursor++ << 4); }

                ASSERT(!(regs & CSR_MASK_LR));
                if (regs & CSR_MASK_X19) { gcPrintf("X19 "); count++; }
                if (regs & CSR_MASK_X20) { gcPrintf("X20 "); count++; }
                if (regs & CSR_MASK_X21) { gcPrintf("X21 "); count++; }
                if (regs & CSR_MASK_X22) { gcPrintf("X22 "); count++; }
                if (regs & CSR_MASK_X23) { gcPrintf("X23 "); count++; }
                if (regs & CSR_MASK_X24) { gcPrintf("X24 "); count++; }
                if (regs & CSR_MASK_X25) { gcPrintf("X25 "); count++; }
                if (regs & CSR_MASK_X26) { gcPrintf("X26 "); count++; }
                if (regs & CSR_MASK_X27) { gcPrintf("X27 "); count++; }
                if (regs & CSR_MASK_X28) { gcPrintf("X28 "); count++; }
                if (regs & CSR_MASK_FP ) { gcPrintf("FP " ); count++; }
#elif defined(TARGET_AMD64)
                if (b & CSR_MASK_RBX) { gcPrintf("RBX "); count++; }
                if (b & CSR_MASK_RSI) { gcPrintf("RSI "); count++; }
                if (b & CSR_MASK_RDI) { gcPrintf("RDI "); count++; }
                if (b & CSR_MASK_RBP) { gcPrintf("RBP "); count++; }
                if (b & CSR_MASK_R12) { gcPrintf("R12 "); count++; }
#elif defined(TARGET_X86)
                if (b & CSR_MASK_RBX) { gcPrintf("EBX "); count++; }
                if (b & CSR_MASK_RSI) { gcPrintf("ESI "); count++; }
                if (b & CSR_MASK_RDI) { gcPrintf("EDI "); count++; }
                if (b & CSR_MASK_RBP) { gcPrintf("EBP "); count++; }
#else
#error unknown architecture
#endif
                gcPrintf("\n");
            }
            break;

        case 0x40:
            {
                // case 3 -- "register"
                const char* regName = "???";
                const char* interior = (b & 0x10) ? "+" : "";
                const char* pinned   = (b & 0x08) ? "!" : "";

                switch (b & 0x7)
                {
#ifdef TARGET_ARM
                case CSR_NUM_R4: regName = "R4"; break;
                case CSR_NUM_R5: regName = "R5"; break;
                case CSR_NUM_R6: regName = "R6"; break;
                case CSR_NUM_R7: regName = "R7"; break;
                case CSR_NUM_R8: regName = "R8"; break;
                case CSR_NUM_R9: regName = "R9"; break;
                case CSR_NUM_R10: regName = "R10"; break;
                case CSR_NUM_R11: regName = "R11"; break;
#elif defined(TARGET_ARM64)
                case CSR_NUM_X19: regName = "X19"; break;
                case CSR_NUM_X20: regName = "X20"; break;
                case CSR_NUM_X21: regName = "X21"; break;
                case CSR_NUM_X22: regName = "X22"; break;
                case CSR_NUM_X23: regName = "X23"; break;
                case CSR_NUM_X24: regName = "X24"; break;
                case CSR_NUM_X25: regName = "X25"; break;
                case 0:
                    switch (*pCursor++)
                    {
                    case CSR_NUM_X26: regName = "X26"; break;
                    case CSR_NUM_X27: regName = "X27"; break;
                    case CSR_NUM_X28: regName = "X28"; break;
                    case CSR_NUM_FP : regName = "FP" ; break;
                    }
                    break;
#elif defined(TARGET_AMD64)
                case CSR_NUM_RBX: regName = "RBX"; break;
                case CSR_NUM_RSI: regName = "RSI"; break;
                case CSR_NUM_RDI: regName = "RDI"; break;
                case CSR_NUM_RBP: regName = "RBP"; break;
                case CSR_NUM_R12: regName = "R12"; break;
                case CSR_NUM_R13: regName = "R13"; break;
                case CSR_NUM_R14: regName = "R14"; break;
                case CSR_NUM_R15: regName = "R15"; break;
#elif defined(TARGET_X86)
                case CSR_NUM_RBX: regName = "EBX"; break;
                case CSR_NUM_RSI: regName = "ESI"; break;
                case CSR_NUM_RDI: regName = "EDI"; break;
                case CSR_NUM_RBP: regName = "EBP"; break;
#else
#error unknown architecture
#endif
                }
                gcPrintf("%02x          | 3  %s%s%s \n", b, regName, interior, pinned);
                count++;
            }
            break;

        case 0x80:
            {
                if (b & 0x10)
                {
                    // case 4 -- "local slot set" or "common var tail"
                    if ((b & 0x0f) != 0)
                    {
                        gcPrintf("%02x          | 4  ", b);
                        bool isFirst = true;

                        int mask = 0x01;
                        int slotNum = 0;
                        while (mask <= 0x08)
                        {
                            if (b & mask)
                            {
                                if (!isFirst)
                                {
                                    if (!first)
                                        gcPrintf("      ");
                                    gcPrintf("            |    ");
                                }

                                PrintLocalSlot(slotNum, pHeader);

                                isFirst = false;
                                count++;
                            }
                            mask <<= 1;
                            slotNum++;
                        }
                    }
                    else
                    {
                        unsigned commonVarInx = 0;
                        if ((b & 0x20) == 0)
                            commonVarInx = VarInt::ReadUnsigned(pCursor);

                        gcPrintf("%02x          | 8  set #%04u\n", b, commonVarInx);
                    }
                }
                else
                {
                    // case 5 -- "local slot"
                    int slotNum = (int)(b & 0xF) + 4;
                    gcPrintf("%02x          | 5  ", b);
                    PrintLocalSlot(slotNum, pHeader);

                    count++;
                }
            }
            break;
        case 0xC0:
            {
                if ((b & 0xC7) == 0xC2)
                {
                    // case 7 - live scratch regs
                    gcPrintf("%02x          | 7  ", b);

                    uint32_t regs, byrefRegs = 0, pinnedRegs = 0;
                    pCursor += ReadRegisterMaskBy7Bit(pCursor, &regs);
                    if (b & 0x10)
                        pCursor += ReadRegisterMaskBy7Bit(pCursor, &byrefRegs);
                    if (b & 0x08)
                        pCursor += ReadRegisterMaskBy7Bit(pCursor, &pinnedRegs);

                    for (uint32_t reg = 0; ; reg++)
                    {
                        uint32_t regMask = (1 << reg);
                        if (regMask > regs)
                            break;

                        if (regs & regMask)
                        {
                            char* pinned = (pinnedRegs & regMask) ? "!" : "";
                            char* interior = (byrefRegs  & regMask) ? "+" : "";
                            char* regStr = "???";

                            switch (reg)
                            {
#if defined(TARGET_ARM)
                            case SR_NUM_R0:   regStr = "R0";   break;
                            case SR_NUM_R1:   regStr = "R1";   break;
                            case SR_NUM_R2:   regStr = "R2";   break;
                            case SR_NUM_R3:   regStr = "R3";   break;
                            case SR_NUM_R12:  regStr = "R12";  break;
                            case SR_NUM_LR:   regStr = "LR";   break;
#elif defined(TARGET_ARM64)
                            case SR_NUM_X0:   regStr = "X0";   break;
                            case SR_NUM_X1:   regStr = "X1";   break;
                            case SR_NUM_X2:   regStr = "X2";   break;
                            case SR_NUM_X3:   regStr = "X3";   break;
                            case SR_NUM_X4:   regStr = "X4";   break;
                            case SR_NUM_X5:   regStr = "X5";   break;
                            case SR_NUM_X6:   regStr = "X6";   break;
                            case SR_NUM_X7:   regStr = "X7";   break;
                            case SR_NUM_X8:   regStr = "X8";   break;
                            case SR_NUM_X9:   regStr = "X9";   break;
                            case SR_NUM_X10:  regStr = "X10";  break;
                            case SR_NUM_X11:  regStr = "X11";  break;
                            case SR_NUM_X12:  regStr = "X12";  break;
                            case SR_NUM_X13:  regStr = "X13";  break;
                            case SR_NUM_X14:  regStr = "X14";  break;
                            case SR_NUM_X15:  regStr = "X15";  break;
                            case SR_NUM_XIP0: regStr = "XIP0"; break;
                            case SR_NUM_XIP1: regStr = "XIP1"; break;
                            case SR_NUM_LR:   regStr = "LR";   break;
#elif defined(TARGET_AMD64)
                            case SR_NUM_RAX:  regStr = "RAX";  break;
                            case SR_NUM_RCX:  regStr = "RCX";  break;
                            case SR_NUM_RDX:  regStr = "RDX";  break;
                            case SR_NUM_R8:   regStr = "R8";   break;
                            case SR_NUM_R9:   regStr = "R9";   break;
                            case SR_NUM_R10:  regStr = "R10";  break;
                            case SR_NUM_R11:  regStr = "R11";  break;
#elif defined(TARGET_X86)
                            case SR_NUM_RAX:  regStr = "EAX";  break;
                            case SR_NUM_RCX:  regStr = "ECX";  break;
                            case SR_NUM_RDX:  regStr = "EDX";  break;
#else
#error unknown architecture
#endif
                            }
                            gcPrintf("%s%s%s ", regStr, interior, pinned);
                            count++;
                        }
                    }
                }
                else
                {
                    // case 6 - stack slot / stack slot set
                    gcPrintf("%02x ", b);
                    unsigned mask = 0;
                    PTR_UInt8 pInts = pCursor;
                    unsigned offset = VarInt::ReadUnsigned(pCursor);
                    const char* interior = (b & 0x10) ? "+" : "";
                    const char* pinned   = (b & 0x08) ? "!" : "";
                    const char* baseReg  = (b & 0x04) ? GetFramePointerRegister() : GetStackPointerRegister();
                    const char* sign     = (b & 0x02) ? "-" : "+";
                    if (b & 0x01)
                    {
                        mask = VarInt::ReadUnsigned(pCursor);
                    }

                    int c = 1;
                    while (pInts != pCursor)
                    {
                        gcPrintf("%02x ", *pInts++);
                        c++;
                    }

                    for (; c < 4; c++)
                    {
                        gcPrintf("   ");
                    }

                    gcPrintf("| 6  [%s%s%02X]%s%s\n", baseReg, sign, offset, interior, pinned);
                    count++;

                    while (mask > 0)
                    {
                        offset += POINTER_SIZE;
                        if (mask & 1)
                        {
                            if (!first)
                                gcPrintf("      ");

                            gcPrintf("            |    [%s%s%02X]%s%s\n", baseReg, sign, offset, interior, pinned);
                            count++;
                        }
                        mask >>= 1;
                    }
                }
            }
            break;
        }
    }
    while (!last);

    //gcPrintf("\n");
}

size_t   FASTCALL   GCDump::DumpGCTable (PTR_UInt8              gcInfo,
                                         Tables *               pTables,
                                         const GCInfoHeader&    header)
{
    PTR_UInt8 pCursor = gcInfo;

    if (header.HasCommonVars())
    {
        uint32_t commonVarCount = VarInt::ReadUnsigned(pCursor);
        for (uint32_t i = 0; i < commonVarCount; i++)
        {
            VarInt::SkipUnsigned(pCursor);
        }
    }

    //
    // Decode the method GC info
    //
    // 0ddddccc -- SMALL ENCODING
    //
    //              -- dddd is an index into the delta shortcut table
    //              -- ccc is an offset into the callsite strings blob
    //
    // 1ddddddd { info offset } -- BIG ENCODING
    //
    //              -- ddddddd is a 7-bit delta
    //              -- { info offset } is a variable-length unsigned encoding of the offset into the callsite
    //                 strings blob for this callsite.
    //
    // 10000000 { delta } -- FORWARDER
    //
    //              -- { delta } is a variable-length unsigned encoding of the offset to the next callsite
    //
    // 11111111 -- STRING TERMINATOR
    //

    uint32_t curOffset = 0;

    for (;;)
    {
        uint8_t b = *pCursor++;
        unsigned infoOffset;

        if (b & 0x80)
        {
            uint8_t lowBits = (b & 0x7F);
            // FORWARDER
            if (lowBits == 0)
            {
                curOffset += VarInt::ReadUnsigned(pCursor);
                continue;
            }
            else
            if (lowBits == 0x7F) // STRING TERMINATOR
                break;

            // BIG ENCODING
            curOffset += lowBits;
            infoOffset = VarInt::ReadUnsigned(pCursor);
        }
        else
        {
            // SMALL ENCODING
            infoOffset = (b & 0x7);
            curOffset += pTables->pbDeltaShortcutTable[b >> 3];
        }

        DumpCallsiteString(curOffset, pTables->pbCallsiteInfoBlob + infoOffset, &header);
    }

    gcPrintf("-------\n");

    return 0;
}

#endif // _DEBUG || DACCESS_COMPILE
