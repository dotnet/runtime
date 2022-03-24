// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/***********************************************************************
*
* File: dis.cpp
*

*
* File Comments:
*
*  This file handles disassembly. It is adapted from the MS linker.
*
***********************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************/
#ifdef LATE_DISASM
/*****************************************************************************/

// Define DISASM_DEBUG to get verbose output of late disassembler inner workings.
//#define DISASM_DEBUG
#ifdef DISASM_DEBUG
#ifdef DEBUG
#define DISASM_DUMP(...)                                                                                               \
    if (VERBOSE)                                                                                                       \
    printf(__VA_ARGS__)
#else // !DEBUG
#define DISASM_DUMP(...) printf(__VA_ARGS__)
#endif // !DEBUG
#else  // !DISASM_DEBUG
#define DISASM_DUMP(...)
#endif // !DISASM_DEBUG

/*****************************************************************************/

#define MAX_CLASSNAME_LENGTH 1024

#if defined(HOST_AMD64)

#pragma comment(linker,                                                                                                \
                "/ALTERNATENAME:__imp_?CchFormatAddr@DIS@@QEBA_K_KPEAG0@Z=__imp_?CchFormatAddr@DIS@@QEBA_K_KPEA_W0@Z")
#pragma comment(linker,                                                                                                \
                "/ALTERNATENAME:__imp_?CchFormatInstr@DIS@@QEBA_KPEAG_K@Z=__imp_?CchFormatInstr@DIS@@QEBA_KPEA_W_K@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchaddrSet@DIS@@QEAAP6A_KPEBV1@_KPEAG1PEA_K@ZP6A_K01213@Z@Z=__imp_?PfncchaddrSet@DIS@@QEAAP6A_KPEBV1@_KPEA_W1PEA_K@ZP6A_K01213@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchregSet@DIS@@QEAAP6A_KPEBV1@W4REGA@1@PEAG_K@ZP6A_K0123@Z@Z=__imp_?PfncchregSet@DIS@@QEAAP6A_KPEBV1@W4REGA@1@PEA_W_K@ZP6A_K0123@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchregrelSet@DIS@@QEAAP6A_KPEBV1@W4REGA@1@KPEAG_KPEAK@ZP6A_K01K234@Z@Z=__imp_?PfncchregrelSet@DIS@@QEAAP6A_KPEBV1@W4REGA@1@KPEA_W_KPEAK@ZP6A_K01K234@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchfixupSet@DIS@@QEAAP6A_KPEBV1@_K1PEAG1PEA_K@ZP6A_K011213@Z@Z=__imp_?PfncchfixupSet@DIS@@QEAAP6A_KPEBV1@_K1PEA_W1PEA_K@ZP6A_K011213@Z@Z")

#elif defined(HOST_X86)

#pragma comment(linker, "/ALTERNATENAME:__imp_?CchFormatAddr@DIS@@QBEI_KPAGI@Z=__imp_?CchFormatAddr@DIS@@QBEI_KPA_WI@Z")
#pragma comment(linker, "/ALTERNATENAME:__imp_?CchFormatInstr@DIS@@QBEIPAGI@Z=__imp_?CchFormatInstr@DIS@@QBEIPA_WI@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchaddrSet@DIS@@QAEP6GIPBV1@_KPAGIPA_K@ZP6GI012I3@Z@Z=__imp_?PfncchaddrSet@DIS@@QAEP6GIPBV1@_KPA_WIPA_K@ZP6GI012I3@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchregSet@DIS@@QAEP6GIPBV1@W4REGA@1@PAGI@ZP6GI012I@Z@Z=__imp_?PfncchregSet@DIS@@QAEP6GIPBV1@W4REGA@1@PA_WI@ZP6GI012I@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchregrelSet@DIS@@QAEP6GIPBV1@W4REGA@1@KPAGIPAK@ZP6GI01K2I3@Z@Z=__imp_?PfncchregrelSet@DIS@@QAEP6GIPBV1@W4REGA@1@KPA_WIPAK@ZP6GI01K2I3@Z@Z")
#pragma comment(                                                                                                       \
    linker,                                                                                                            \
    "/ALTERNATENAME:__imp_?PfncchfixupSet@DIS@@QAEP6GIPBV1@_KIPAGIPA_K@ZP6GI01I2I3@Z@Z=__imp_?PfncchfixupSet@DIS@@QAEP6GIPBV1@_KIPA_WIPA_K@ZP6GI01I2I3@Z@Z")

#endif

/*****************************************************************************
 * Given an absolute address from the beginning of the code
 * find the corresponding emitter block and the relative offset
 * of the current address in that block
 * Was used to get to the fixup list of each block. The new emitter has
 * no such fixups. Something needs to be added for this.
 */

// These structs were defined in emit.h. Fake them here so DisAsm.cpp can compile

typedef struct codeFix
{
    codeFix* cfNext;
    unsigned cfFixup;
} * codeFixPtr;

typedef struct codeBlk
{
    codeFix* cbFixupLst;
} * codeBlkPtr;

/*****************************************************************************
 * The following is the callback for jump label and direct function calls fixups.
 * "addr" represents the address of jump that has to be
 * replaced with a label or function name.
 *
 * Return 1 if a name was written representing the address, 0 otherwise.
 */

/* static */
size_t __stdcall DisAssembler::disCchAddr(
    const DIS* pdis, DIS::ADDR addr, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp)
{
    DisAssembler* pDisAsm = (DisAssembler*)pdis->PvClient();
    assert(pDisAsm);
    return pDisAsm->disCchAddrMember(pdis, addr, wz, cchMax, pdwDisp);
}

size_t DisAssembler::disCchAddrMember(
    const DIS* pdis, DIS::ADDR addr, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp)
{
    /* First check the termination type of the instruction
     * because this might be a helper or static function call
     * check to see if we have a fixup for the current address */

    size_t retval = 0; // assume we don't know

#if defined(TARGET_XARCH)

    DISX86::TRMTA terminationType = DISX86::TRMTA(pdis->Trmta());

    DISASM_DUMP("AddrMember %p (%p), termType %u\n", addr, disGetLinearAddr((size_t)addr), terminationType);

    switch (terminationType)
    {
        // int disCallSize;

        case DISX86::trmtaJmpShort:
        case DISX86::trmtaJmpCcShort:

            /* We have a short jump in the current code block - generate the label to which we jump */

            assert(0 <= disTarget && disTarget < disTotalCodeSize);
            swprintf_s(wz, cchMax, W("short L_%02u"), disLabels[disTarget]);
            retval = 1;
            break;

        case DISX86::trmtaJmpNear:
        case DISX86::trmtaJmpCcNear:

            /* We have a near jump. Check if is in the current code block.
             * Otherwise we have no target for it. */

            if (0 <= disTarget && disTarget < disTotalCodeSize)
            {
                swprintf_s(wz, cchMax, W("L_%02u"), disLabels[disTarget]);
                retval = 1;
            }
            break;

        case DISX86::trmtaCallNear16:
        case DISX86::trmtaCallNear32:

            /* check for local calls (i.e. CALL label) */

            if (0 <= disTarget && disTarget < disTotalCodeSize)
            {
                /* not a "call ds:[0000]" - go ahead */
                /* disTarget within block boundary -> local call */

                swprintf_s(wz, cchMax, W("short L_%02u"), disLabels[disTarget]);
                retval = 1;
                break;
            }

            /* this is a near call - in our case usually VM helper functions */

            /* find the emitter block and the offset of the call fixup */
            /* for the fixup offset we have to add the opcode size for the call - in the case of a near call is 1 */

            // disCallSize = 1;

            {
                size_t      absoluteTarget = (size_t)disGetLinearAddr(disTarget);
                const char* name           = disGetMethodFullName(absoluteTarget);
                if (name != nullptr)
                {
                    swprintf_s(wz, cchMax, W("%zx %S"), dspAddr(absoluteTarget), name);
                    retval = 1;
                    break;
                }
            }

            break;

#ifdef TARGET_AMD64

        case DISX86::trmtaFallThrough:

            /* memory indirect case. Could be for an LEA for the base address of a switch table, which is an arbitrary
             * address, currently of the first block after the prolog. */

            /* find the emitter block and the offset for the fixup
             * "addr" is the address of the immediate */

            break;

#endif // TARGET_AMD64

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");
            break;
    }

#elif defined(TARGET_ARM64)

    DISARM64::TRMTA terminationType = DISARM64::TRMTA(pdis->Trmta());

    DISASM_DUMP("AddrMember %p (%p), termType %u\n", addr, disGetLinearAddr((size_t)addr), terminationType);

    switch (terminationType)
    {
        // int disCallSize;

        case DISARM64::TRMTA::trmtaBra:
        case DISARM64::TRMTA::trmtaBraCase:
        case DISARM64::TRMTA::trmtaBraCc:
        case DISARM64::TRMTA::trmtaBraCcCase:
        case DISARM64::TRMTA::trmtaBraCcInd:
        case DISARM64::TRMTA::trmtaBraInd:

            /* We have a jump. Check if is in the current code block.
             * Otherwise we have no target for it. */

            if (0 <= disTarget && disTarget < disTotalCodeSize)
            {
                swprintf_s(wz, cchMax, W("L_%02u"), disLabels[disTarget]);
                retval = 1;
            }
            break;

        case DISARM64::trmtaCall:
        case DISARM64::trmtaCallCc:
        case DISARM64::trmtaCallCcInd:
        case DISARM64::trmtaCallInd:

            /* check for local calls (i.e. CALL label) */

            if (0 <= disTarget && disTarget < disTotalCodeSize)
            {
                /* not a "call [0000]" - go ahead */
                /* disTarget within block boundary -> local call */

                swprintf_s(wz, cchMax, W("L_%02u"), disLabels[disTarget]);
                retval = 1;
                break;
            }

            /* this is a near call - in our case usually VM helper functions */

            /* find the emitter block and the offset of the call fixup */
            /* for the fixup offset we have to add the opcode size for the call - in the case of a near call is 1 */

            // disCallSize = 1;

            {
                size_t      absoluteTarget = (size_t)disGetLinearAddr(disTarget);
                const char* name           = disGetMethodFullName(absoluteTarget);
                if (name != nullptr)
                {
                    swprintf_s(wz, cchMax, W("%zx %S"), dspAddr(absoluteTarget), name);
                    retval = 1;
                    break;
                }
            }

            break;

        case DISARM64::trmtaFallThrough:

            /* memory indirect case. Could be for an LEA for the base address of a switch table, which is an arbitrary
             * address, currently of the first block after the prolog. */

            /* find the emitter block and the offset for the fixup
             * "addr" is the address of the immediate */

            {
                DIS::INSTRUCTION instr;
                DIS::OPERAND     ops[DISARM64::coperandMax];
                bool             ok = pdis->FDecode(&instr, ops, ArrLen(ops));
                if (ok)
                {
                    bool isAddress = false;
                    switch ((DISARM64::OPA)instr.opa)
                    {
                        case DISARM64::opaAdr:
                        case DISARM64::opaAdrp:
                            isAddress = true;
                            break;
                        default:
                            break;
                    }

                    if (isAddress && 0 <= addr && addr < disTotalCodeSize)
                    {
                        swprintf_s(wz, cchMax, W("L_%02u"), disLabels[addr]);
                        retval = 1;
                    }
                }
            }
            break;

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");
            break;
    }

#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*

    if (retval == 0)
    {
        if (disDiffable)
        {
            swprintf_s(wz, cchMax, W("%p"), dspAddr((void*)1));
        }
    }
    else
    {
        /* no displacement */

        *pdwDisp = 0x0;
    }

    return retval;
}

/*****************************************************************************
 * We annotate some instructions to get info needed to display the symbols
 * for that instruction.
 *
 * Return 1 if a name was written representing the address, 0 otherwise.
 */

/* static */
size_t __stdcall DisAssembler::disCchFixup(
    const DIS* pdis, DIS::ADDR addr, size_t size, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp)
{
    DisAssembler* pDisAsm = (DisAssembler*)pdis->PvClient();
    assert(pDisAsm);

    return pDisAsm->disCchFixupMember(pdis, addr, size, wz, cchMax, pdwDisp);
}

size_t DisAssembler::disCchFixupMember(
    const DIS* pdis, DIS::ADDR addr, size_t size, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORDLONG* pdwDisp)
{
#if defined(TARGET_XARCH)

    DISX86::TRMTA terminationType = DISX86::TRMTA(pdis->Trmta());
    // DIS::ADDR disIndAddr;

    DISASM_DUMP("FixupMember %016I64X (%08IX), size %d, termType %u\n", addr, disGetLinearAddr((size_t)addr), size,
                terminationType);

    // Is there a relocation registered for the address?

    size_t absoluteAddr = (size_t)disGetLinearAddr((size_t)addr);
    size_t targetAddr;
    bool   anyReloc = GetRelocationMap()->Lookup(absoluteAddr, &targetAddr);

    switch (terminationType)
    {
        DIS::ADDR disCallSize;

        case DISX86::trmtaFallThrough:

            /* memory indirect case */

            assert(addr > pdis->Addr());

            /* find the emitter block and the offset for the fixup
             * "addr" is the address of the immediate */

            if (anyReloc)
            {
                // Make instructions like "mov rcx, 7FE8247A638h" diffable.
                swprintf_s(wz, cchMax, W("%IXh"), dspAddr(targetAddr));
                break;
            }

            return 0;

        case DISX86::trmtaJmpInd:

            /* pretty rare case - something like "jmp [eax*4]"
             * not a function call or anything worth annotating */

            return 0;

        case DISX86::trmtaTrap:
        case DISX86::trmtaTrapCc:

            /* some instructions like division have a TRAP termination type - ignore it */

            return 0;

        case DISX86::trmtaJmpShort:
        case DISX86::trmtaJmpCcShort:

        case DISX86::trmtaJmpNear:
        case DISX86::trmtaJmpCcNear:

            /* these are treated by the CchAddr callback - skip them */

            return 0;

        case DISX86::trmtaCallNear16:
        case DISX86::trmtaCallNear32:

            if (anyReloc)
            {
                const char* name = disGetMethodFullName(targetAddr);
                if (name != nullptr)
                {
                    swprintf_s(wz, cchMax, W("%zx %S"), dspAddr(targetAddr), name);
                    break;
                }
            }

            /* these are treated by the CchAddr callback - skip them */

            return 0;

        case DISX86::trmtaCallInd:

            /* here we have an indirect call - find the indirect address */

            // BYTE * code = disGetLinearAddr((size_t)addr);
            // disIndAddr = (DIS::ADDR) (code+0);

            /* find the size of the call opcode - less the immediate */
            /* for the fixup offset we have to add the opcode size for the call */
            /* addr is the address of the immediate, pdis->Addr() returns the address of the disassembled instruction */

            assert(addr > pdis->Addr());
            disCallSize = addr - pdis->Addr();

            /* find the emitter block and the offset of the call fixup */

            return 0;

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");
            break;
    }

#elif defined(TARGET_ARM64)

    DISARM64::TRMTA terminationType = DISARM64::TRMTA(pdis->Trmta());
    // DIS::ADDR disIndAddr;

    DISASM_DUMP("FixupMember %016I64X (%08IX), size %d, termType %u\n", addr, disGetLinearAddr((size_t)addr), size,
                terminationType);

    // Is there a relocation registered for the address?

    size_t absoluteAddr = (size_t)disGetLinearAddr((size_t)addr);
    size_t targetAddr;
    bool   anyReloc = GetRelocationMap()->Lookup(absoluteAddr, &targetAddr);

    switch (terminationType)
    {
        DIS::ADDR disCallSize;

        case DISARM64::TRMTA::trmtaUnknown:
            return 0;

        case DISARM64::TRMTA::trmtaFallThrough:

            if (anyReloc)
            {
                /* memory indirect case */

                assert(addr > pdis->Addr());

                /* find the emitter block and the offset for the fixup
                 * "addr" is the address of the immediate */

                // Make instructions like "mov rcx, 7FE8247A638h" diffable.
                swprintf_s(wz, cchMax, W("%IXh"), dspAddr(targetAddr));
                break;
            }

            return 0;

        case DISARM64::TRMTA::trmtaBraInd:
        case DISARM64::TRMTA::trmtaBraCcInd:

            /* pretty rare case - something like "jmp [eax*4]"
             * not a function call or anything worth annotating */

            return 0;

        case DISARM64::TRMTA::trmtaTrap:
        case DISARM64::TRMTA::trmtaTrapCc:

            /* some instructions like division have a TRAP termination type - ignore it */

            return 0;

        case DISARM64::TRMTA::trmtaBra:
        case DISARM64::TRMTA::trmtaBraCase:
        case DISARM64::TRMTA::trmtaBraCc:
        case DISARM64::TRMTA::trmtaBraCcCase:

            /* these are treated by the CchAddr callback - skip them */

            return 0;

        case DISARM64::TRMTA::trmtaCall:
        case DISARM64::TRMTA::trmtaCallCc:

            if (anyReloc)
            {
                const char* name = disGetMethodFullName(targetAddr);
                if (name != nullptr)
                {
                    swprintf_s(wz, cchMax, W("%zx %S"), dspAddr(targetAddr), name);
                    break;
                }
            }

            /* these are treated by the CchAddr callback - skip them */

            return 0;

        case DISARM64::TRMTA::trmtaCallInd:
        case DISARM64::TRMTA::trmtaCallCcInd:

            /* here we have an indirect call - find the indirect address */

            // BYTE * code = disGetLinearAddr((size_t)addr);
            // disIndAddr = (DIS::ADDR) (code+0);

            /* find the size of the call opcode - less the immediate */
            /* for the fixup offset we have to add the opcode size for the call */
            /* addr is the address of the immediate, pdis->Addr() returns the address of the disassembled instruction */

            assert(addr > pdis->Addr());
            disCallSize = addr - pdis->Addr();

            /* find the emitter block and the offset of the call fixup */

            return 0;

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");
            break;
    }

#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*

    /* no displacement */

    *pdwDisp = 0x0;

    return 1;
}

/*****************************************************************************
 * This the callback for register-relative operands in an instruction.
 * If the register is ESP or EBP, the operand may be a local variable
 * or a parameter, else the operand may be an instance variable
 *
 * Return 1 if a name was written representing the register-relative operand, 0 otherwise.
 */

/* static */
size_t __stdcall DisAssembler::disCchRegRel(
    const DIS* pdis, DIS::REGA reg, DWORD disp, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORD* pdwDisp)
{
    DisAssembler* pDisAsm = (DisAssembler*)pdis->PvClient();
    assert(pDisAsm);

    return pDisAsm->disCchRegRelMember(pdis, reg, disp, wz, cchMax, pdwDisp);
}

size_t DisAssembler::disCchRegRelMember(
    const DIS* pdis, DIS::REGA reg, DWORD disp, _In_reads_(cchMax) wchar_t* wz, size_t cchMax, DWORD* pdwDisp)
{
#if defined(TARGET_XARCH)

    DISX86::TRMTA terminationType = DISX86::TRMTA(pdis->Trmta());
    // DIS::ADDR disIndAddr;

    DISASM_DUMP("RegRelMember reg %u, disp %u, termType %u\n", reg, disp, terminationType);

    switch (terminationType)
    {
        int         disOpcodeSize;
        const char* var;

        case DISX86::trmtaFallThrough:

        /* some instructions like division have a TRAP termination type - ignore it */

        case DISX86::trmtaTrap:
        case DISX86::trmtaTrapCc:

            var = disComp->codeGen->siStackVarName((size_t)(pdis->Addr() - disStartAddr), pdis->Cb(), reg, disp);
            if (var)
            {
                swprintf_s(wz, cchMax, W("%hs+%Xh '%hs'"), getRegName(reg), disp, var);
                *pdwDisp = 0;

                return 1;
            }

            /* This case consists of non-static members */

            /* find the emitter block and the offset for the fixup
             * fixup is emited after the coding of the instruction - size = word (2 bytes)
             * GRRRR!!! - for the 16 bit case we have to check for the address size prefix = 0x66
             */

            if (*disGetLinearAddr(disCurOffset) == 0x66)
            {
                disOpcodeSize = 3;
            }
            else
            {
                disOpcodeSize = 2;
            }

            return 0;

        case DISX86::trmtaCallNear16:
        case DISX86::trmtaCallNear32:
        case DISX86::trmtaJmpInd:

            break;

        case DISX86::trmtaCallInd:

            /* check if this is a one byte displacement */

            if ((signed char)disp == (int)disp)
            {
                /* we have a one byte displacement -> there were no previous callbacks */

                /* find the size of the call opcode - less the immediate */
                /* this is a call R/M indirect -> opcode size is 2 */

                disOpcodeSize = 2;

                /* find the emitter block and the offset of the call fixup */

                return 0;
            }
            else
            {
                /* check if we already have a symbol name as replacement */

                if (disHasName)
                {
                    /* CchFixup has been called before - we have a symbol name saved in global var disFuncTempBuf */

                    swprintf_s(wz, cchMax, W("%hs+%u '%hs'"), getRegName(reg), disp, disFuncTempBuf);
                    *pdwDisp   = 0;
                    disHasName = false;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");

            break;
    }

#elif defined(TARGET_ARM64)

    DISARM64::TRMTA terminationType = DISARM64::TRMTA(pdis->Trmta());

    DISASM_DUMP("RegRelMember reg %u, disp %u, termType %u\n", reg, disp, terminationType);

    switch (terminationType)
    {
        int         disOpcodeSize;
        const char* var;

        case DISARM64::TRMTA::trmtaFallThrough:

        /* some instructions like division have a TRAP termination type - ignore it */

        case DISARM64::TRMTA::trmtaTrap:
        case DISARM64::TRMTA::trmtaTrapCc:

            var = disComp->codeGen->siStackVarName((size_t)(pdis->Addr() - disStartAddr), pdis->Cb(), reg, disp);
            if (var)
            {
                swprintf_s(wz, cchMax, W("%hs+%Xh '%hs'"), getRegName(reg), disp, var);
                *pdwDisp = 0;

                return 1;
            }

            /* This case consists of non-static members */

            // TODO-ARM64-Bug?: Is this correct?
            disOpcodeSize = 2;
            return 0;

        case DISARM64::TRMTA::trmtaCall:
        case DISARM64::TRMTA::trmtaCallCc:
        case DISARM64::TRMTA::trmtaBraInd:
        case DISARM64::TRMTA::trmtaBraCcInd:
            break;

        case DISARM64::TRMTA::trmtaCallInd:
        case DISARM64::TRMTA::trmtaCallCcInd:

            /* check if this is a one byte displacement */

            if ((signed char)disp == (int)disp)
            {
                /* we have a one byte displacement -> there were no previous callbacks */

                /* find the size of the call opcode - less the immediate */
                /* this is a call R/M indirect -> opcode size is 2 */

                // TODO-ARM64-Bug?: Is this correct?
                disOpcodeSize = 2;

                /* find the emitter block and the offset of the call fixup */

                return 0;
            }
            else
            {
                /* check if we already have a symbol name as replacement */

                if (disHasName)
                {
                    /* CchFixup has been called before - we have a symbol name saved in global var disFuncTempBuf */

                    swprintf_s(wz, cchMax, W("%hs+%u '%hs'"), getRegName(reg), disp, disFuncTempBuf);
                    *pdwDisp   = 0;
                    disHasName = false;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

        default:

            printf("Termination type is %d\n", (int)terminationType);
            assert(!"treat this case\n");

            break;
    }

#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*

    /* save displacement */

    *pdwDisp = disp;

    return 1;
}

/*****************************************************************************
 *
 * Callback for register operands. Most probably, this is a local variable or
 * a parameter
 *
 * Return 1 if a name was written representing the register, 0 otherwise.
 */

/* static */
size_t __stdcall DisAssembler::disCchReg(const DIS* pdis, DIS::REGA reg, _In_reads_(cchMax) wchar_t* wz, size_t cchMax)
{
    DisAssembler* pDisAsm = (DisAssembler*)pdis->PvClient();
    assert(pDisAsm);

    return pDisAsm->disCchRegMember(pdis, reg, wz, cchMax);
}

size_t DisAssembler::disCchRegMember(const DIS* pdis, DIS::REGA reg, _In_reads_(cchMax) wchar_t* wz, size_t cchMax)
{
    // TODO-Review: DIS::REGA does not directly map to our regNumber! E.g., look at DISARM64::REGA --
    // the Wt registers come first (and do map to our regNumber), but the Xt registers follow.
    // Until this is fixed, don't use this function!
    disHasName = false;
    return 0;

#if 0
    const char * var = disComp->codeGen->siRegVarName(
                                            (size_t)(pdis->Addr() - disStartAddr),
                                            pdis->Cb(),
                                            reg);

    if (var)
    {
        if (disHasName)
        {
            /* CchRegRel has been called before - we have a symbol name saved in global var disFuncTempBuf */

            swprintf_s(wz, cchMax, W("%hs'%hs.%hs'"), getRegName(reg), var, disFuncTempBuf);
            disHasName = false;
            return 1;
        }
        else
        {
            swprintf_s(wz, cchMax, W("%hs'%hs'"), getRegName(reg), var);
            return 1;
        }
    }
    else
    {
        if (disHasName)
        {
            /* this is the ugly case when a variable is incorrectly presumed dead */

            swprintf_s(wz, cchMax, W("%hs'%hs.%hs'"), getRegName(reg), "<InstVar>", disFuncTempBuf);
            disHasName = false;
            return 1;
        }

        /* just to make sure we didn't bungle if var returns NULL */
        disHasName = false;
        return 0;
    }
#endif // 0
}

/*****************************************************************************
 * Helper function to lazily create a map from code address to CORINFO_METHOD_HANDLE.
 */
AddrToMethodHandleMap* DisAssembler::GetAddrToMethodHandleMap()
{
    if (disAddrToMethodHandleMap == nullptr)
    {
        disAddrToMethodHandleMap = new (disComp->getAllocator()) AddrToMethodHandleMap(disComp->getAllocator());
    }
    return disAddrToMethodHandleMap;
}

/*****************************************************************************
 * Helper function to lazily create a map from code address to CORINFO_METHOD_HANDLE.
 */
AddrToMethodHandleMap* DisAssembler::GetHelperAddrToMethodHandleMap()
{
    if (disHelperAddrToMethodHandleMap == nullptr)
    {
        disHelperAddrToMethodHandleMap = new (disComp->getAllocator()) AddrToMethodHandleMap(disComp->getAllocator());
    }
    return disHelperAddrToMethodHandleMap;
}

/*****************************************************************************
 * Helper function to lazily create a map from relocation address to relocation target address.
 */
AddrToAddrMap* DisAssembler::GetRelocationMap()
{
    if (disRelocationMap == nullptr)
    {
        disRelocationMap = new (disComp->getAllocator()) AddrToAddrMap(disComp->getAllocator());
    }
    return disRelocationMap;
}

/*****************************************************************************
 * Return the count of bytes disassembled.
 */

size_t DisAssembler::CbDisassemble(DIS*        pdis,
                                   size_t      offs,
                                   DIS::ADDR   addr,
                                   const BYTE* pb,
                                   size_t      cbMax,
                                   FILE*       pfile,
                                   bool        findLabels,
                                   bool        printit /* = false */,
                                   bool        dispOffs /* = false */,
                                   bool        dispCodeBytes /* = false */)
{
    assert(pdis);

    size_t cb = pdis->CbDisassemble(addr, pb, cbMax);

    if (cb == 0)
    {
        DISASM_DUMP("CbDisassemble offs %Iu addr %I64u\n", offs, addr);
        // assert(!"can't disassemble instruction!!!");
        fprintf(pfile, "MSVCDIS can't disassemble instruction @ offset %Iu (0x%02x)!!!\n", offs, offs);
#if defined(TARGET_ARM64)
        fprintf(pfile, "%08Xh\n", *(unsigned int*)pb);
        return 4;
#else
        fprintf(pfile, "%02Xh\n", *pb);
        return 1;
#endif
    }

#if defined(TARGET_ARM64)
    assert(cb == 4); // all instructions are 4 bytes!
#endif               // TARGET_ARM64

    /* remember current offset and instruction size */

    disCurOffset = (size_t)addr;
    disInstSize  = cb;

    /* Set the disTarget address */

    disTarget = (size_t)pdis->AddrTarget();

    if (findLabels)
    {
#if defined(TARGET_XARCH)
        DISX86::TRMTA terminationType = DISX86::TRMTA(pdis->Trmta());

        /* check the termination type of the instruction */

        switch (terminationType)
        {
            case DISX86::trmtaCallNear16:
            case DISX86::trmtaCallNear32:
            case DISX86::trmtaCallFar:

            {
                // Don't count addresses in the relocation table
                size_t targetAddr;
                size_t absoluteAddr =
                    (size_t)disGetLinearAddr((size_t)pdis->AddrAddress(1)); // Get the address in the instruction of the
                                                                            // call target address (the address the
                                                                            // reloc is applied to).
                if (GetRelocationMap()->Lookup(absoluteAddr, &targetAddr))
                {
                    break;
                }
            }

                FALLTHROUGH;

            case DISX86::trmtaJmpShort:
            case DISX86::trmtaJmpNear:
            case DISX86::trmtaJmpFar:
            case DISX86::trmtaJmpCcShort:
            case DISX86::trmtaJmpCcNear:

                /* a CALL is local iff the disTarget is within the block boundary */

                /* mark the jump label in the disTarget vector and return */

                if (disTarget != DIS::addrNil) // There seems to be an assumption that you can't branch to the first
                                               // address of the function (prolog).
                {
                    if (0 <= disTarget && disTarget < disTotalCodeSize)
                    {
                        /* we're OK, disTarget within block boundary */

                        disLabels[disTarget] = 1;
                    }
                }
                break;

            case DISX86::trmtaFallThrough:
                // We'd like to be able to get a label for code like "lea rcx, [4]" that we use for jump tables, but I
                // can't figure out how.
                break;

            default:

                /* jump is not in the current code block */
                break;

        } // end switch
#elif defined(TARGET_ARM64)
        DISARM64::TRMTA terminationType = DISARM64::TRMTA(pdis->Trmta());

        /* check the termination type of the instruction */

        switch (terminationType)
        {
            case DISARM64::TRMTA::trmtaCall:
            case DISARM64::TRMTA::trmtaCallCc:

            {
                // Don't count addresses in the relocation table
                size_t targetAddr;
                size_t absoluteAddr =
                    (size_t)disGetLinearAddr((size_t)pdis->AddrAddress(1)); // Get the address in the instruction of the
                                                                            // call target address (the address the
                                                                            // reloc is applied to).
                if (GetRelocationMap()->Lookup(absoluteAddr, &targetAddr))
                {
                    break;
                }
            }

                FALLTHROUGH;

            case DISARM64::TRMTA::trmtaBra:
            case DISARM64::TRMTA::trmtaBraCase:
            case DISARM64::TRMTA::trmtaBraCc:
            case DISARM64::TRMTA::trmtaBraCcCase:

                /* a CALL is local iff the disTarget is within the block boundary */

                /* mark the jump label in the disTarget vector and return */

                if (disTarget != DIS::addrNil) // There seems to be an assumption that you can't branch to the first
                                               // address of the function (prolog).
                {
                    if (0 <= disTarget && disTarget < disTotalCodeSize)
                    {
                        /* we're OK, disTarget within block boundary */

                        disLabels[disTarget] = 1;
                    }
                }
                break;

            case DISARM64::TRMTA::trmtaFallThrough:
            {
                DIS::INSTRUCTION instr;
                DIS::OPERAND     ops[DISARM64::coperandMax];
                bool             ok = pdis->FDecode(&instr, ops, ArrLen(ops));
                if (ok)
                {
                    switch ((DISARM64::OPA)instr.opa)
                    {
                        case DISARM64::opaAdr:
                        case DISARM64::opaAdrp:
                            // operand 1 is an address
                            assert(instr.coperand >= 2);
                            assert(ops[1].opcls == DIS::opclsImmediate);
                            assert(ops[1].imcls == DIS::imclsAddress);
                            disTarget = ops[1].dwl;
                            break;
                        default:
                            break;
                    }

                    if (0 <= disTarget && disTarget < disTotalCodeSize)
                    {
                        /* we're OK, disTarget within block boundary */

                        disLabels[disTarget] = 1;
                    }
                }
            }
            break;

            default:

                /* jump is not in the current code block */
                break;

        } // end switch
#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*

        return cb;
    } // end if

    /* check if we have a label here */

    if (printit)
    {
        if (disLabels[addr])
        {
            /* print the label and the offset */

            fprintf(pfile, "L_%02u:\n", disLabels[addr]);
        }
    }

    wchar_t wz[MAX_CLASSNAME_LENGTH];
    pdis->CchFormatInstr(wz, ArrLen(wz));

    if (printit)
    {
        if (dispOffs)
        {
            fprintf(pfile, "%03X", offs);
        }

#ifdef TARGET_ARM64
#define CCH_INDENT 8 // fixed sized instructions, always 8 characters
#elif defined(TARGET_AMD64)
#define CCH_INDENT 30 // large constants sometimes
#else
#define CCH_INDENT 24
#endif

        size_t cchIndent = CCH_INDENT;

        if (dispCodeBytes)
        {
            static size_t cchBytesMax = -1;

            if (cchBytesMax == -1)
            {
                cchBytesMax = pdis->CchFormatBytesMax();
            }

            wchar_t wzBytes[MAX_CLASSNAME_LENGTH];
            assert(cchBytesMax < MAX_CLASSNAME_LENGTH);

            size_t cchBytes = pdis->CchFormatBytes(wzBytes, ArrLen(wzBytes));

            if (cchBytes > CCH_INDENT)
            {
                // Truncate the bytes if they are too long

                static const wchar_t* elipses    = W("...\0");
                const size_t          cchElipses = 4;

                memcpy(&wzBytes[CCH_INDENT - cchElipses], elipses, cchElipses * sizeof(wchar_t));

                cchBytes = CCH_INDENT;
            }

            fprintf(pfile, "  %ls", wzBytes);
            cchIndent = CCH_INDENT - cchBytes;
        }

        // print the dis-assembled instruction

        fprintf(pfile, "%*c %ls\n", cchIndent, ' ', wz);
    }

    return cb;
}

// TODO-Cleanup: this is currently unused, unreferenced.
size_t CbDisassembleWithBytes(DIS* pdis, DIS::ADDR addr, const BYTE* pb, size_t cbMax, FILE* pfile)
{
    assert(pdis);
    DisAssembler* pDisAsm = (DisAssembler*)pdis->PvClient();
    assert(pDisAsm);

    wchar_t wz[MAX_CLASSNAME_LENGTH];

    pdis->CchFormatAddr(addr, wz, ArrLen(wz));

    size_t cchIndent = (size_t)fprintf(pfile, "  %ls: ", wz);

    size_t cb = pdis->CbDisassemble(addr, pb, cbMax);

    if (cb == 0)
    {
        fprintf(pfile, "%02Xh\n", *pb);
        return (1);
    }

    size_t cchBytesMax = pdis->CchFormatBytesMax();

    if (cchBytesMax > 18)
    {
        // Limit bytes coded to 18 characters

        cchBytesMax = 18;
    }

    wchar_t wzBytes[64];
    size_t  cchBytes = pdis->CchFormatBytes(wzBytes, ArrLen(wzBytes));

    wchar_t* pwzBytes;
    wchar_t* pwzNext;

    for (pwzBytes = wzBytes; pwzBytes != NULL; pwzBytes = pwzNext)
    {
        bool fFirst = (pwzBytes == wzBytes);

        cchBytes = wcslen(pwzBytes);

        if (cchBytes <= cchBytesMax)
        {
            pwzNext = NULL;
        }

        else
        {
            wchar_t ch            = pwzBytes[cchBytesMax];
            pwzBytes[cchBytesMax] = '\0';

            if (ch == W(' '))
            {
                pwzNext = pwzBytes + cchBytesMax + 1;
            }

            else
            {
                pwzNext = wcsrchr(pwzBytes, W(' '));
                assert(pwzNext);

                pwzBytes[cchBytesMax] = ch;
                *pwzNext++            = '\0';
            }
        }

        if (fFirst)
        {
            pdis->CchFormatInstr(wz, ArrLen(wz));
            fprintf(pfile, "%-*ls %ls\n", cchBytesMax, pwzBytes, wz);
        }

        else
        {
            fprintf(pfile, "%*c%ls\n", cchIndent, ' ', pwzBytes);
        }
    }

    return (cb);
}

void DisAssembler::DisasmBuffer(FILE* pfile, bool printit)
{
    DIS* pdis = NULL;

#ifdef TARGET_X86
    pdis = DIS::PdisNew(DIS::distX86);
#elif defined(TARGET_AMD64)
    pdis = DIS::PdisNew(DIS::distX8664);
#elif defined(TARGET_ARM64)
    pdis = DIS::PdisNew(DIS::distArm64);
#else // TARGET*
#error Unsupported or unset target architecture
#endif

    if (pdis == NULL)
    {
        assert(!"out of memory in disassembler?");
        return;
    }

#ifdef TARGET_64BIT
    pdis->SetAddr64(true);
#endif

    // Store a pointer to the DisAssembler so that the callback functions
    // can get to it.

    pdis->PvClientSet((void*)this);

    /* Calculate addresses */

    size_t    ibCur = 0;
    DIS::ADDR addr  = 0; // Always emit code with respect to a "0" base address.

    /* First walk the code to find all jump targets */

    while (ibCur < disTotalCodeSize)
    {
        size_t cb;

        cb = CbDisassemble(pdis, ibCur, addr + ibCur, disGetLinearAddr(ibCur), disGetBufferSize(ibCur), pfile,
                           true); // find labels

        // CbDisassemble returning > MAX_INT... give me a break.
        ibCur += cb;
    }

    /* reset the label counter and start assigning consecutive number labels to the label locations */

    BYTE label = 0;
    for (unsigned i = 0; i < disTotalCodeSize; i++)
    {
        if (disLabels[i] != 0)
        {
            disLabels[i] = ++label;
        }
    }

    /* Re-initialize addresses for disassemble phase */

    ibCur = 0;
    addr  = 0;

    // Set callbacks only if we are displaying it. Else, the scheduler has called it

    if (printit)
    {
        /* Set the callback functions for symbol lookup */

        pdis->PfncchaddrSet(disCchAddr);
        pdis->PfncchfixupSet(disCchFixup);
        pdis->PfncchregrelSet(disCchRegRel);
        pdis->PfncchregSet(disCchReg);
    }

    while (ibCur < disTotalCodeSize)
    {
        size_t cb;

        cb = CbDisassemble(pdis, ibCur, addr + ibCur, disGetLinearAddr(ibCur), disGetBufferSize(ibCur), pfile,
                           false, // find labels
                           printit,
                           !disDiffable, // display relative offset
#ifdef DEBUG
                           !disDiffable // Display code bytes?
#else
                           false // Display code bytes?
#endif
                           );

        ibCur += (unsigned)cb;
    }

    delete pdis;
}

/*****************************************************************************
 * Given a linear offset into the code, find a pointer to the actual code (either in the hot or cold section)
 *
 * Arguments:
 *      offset  - The linear offset into the code. It must point within the code.
 */

const BYTE* DisAssembler::disGetLinearAddr(size_t offset)
{
    if (offset < disHotCodeSize)
    {
        return (const BYTE*)disHotCodeBlock + offset;
    }
    else
    {
        return (const BYTE*)disColdCodeBlock + offset - disHotCodeSize;
    }
}

/*****************************************************************************
 * Given a linear offset into the code, determine how many bytes are remaining in the buffer.
 * This will only return the number of bytes left in either the hot or cold buffer. This is used
 * to avoid walking off the end of the buffer.
 *
 * Arguments:
 *      offset  - The linear offset into the code. It must point within the code.
 */

size_t DisAssembler::disGetBufferSize(size_t offset)
{
    if (offset < disHotCodeSize)
    {
        return disHotCodeSize - offset;
    }
    else
    {
        return disHotCodeSize + disColdCodeSize - offset;
    }
}

/*****************************************************************************
 * Get the function name for a given absolute address.
 */

const char* DisAssembler::disGetMethodFullName(size_t addr)
{
    CORINFO_METHOD_HANDLE res;

    // First check the JIT helper table: they're very common.
    if (GetHelperAddrToMethodHandleMap()->Lookup(addr, &res))
    {
        return disComp->eeGetMethodFullName(res);
    }

    // Next check the "normal" registered call targets
    if (GetAddrToMethodHandleMap()->Lookup(addr, &res))
    {
        return disComp->eeGetMethodFullName(res);
    }

    return nullptr;
}

/*****************************************************************************
 * Register a called function address as associated with a CORINFO_METHOD_HANDLE.
 *
 * Arguments:
 *      addr    - The absolute address of the target function.
 *      methHnd - The method handle associated with 'addr'.
 */

void DisAssembler::disSetMethod(size_t addr, CORINFO_METHOD_HANDLE methHnd)
{
    if (!disComp->opts.doLateDisasm)
    {
        return;
    }

    if (disComp->eeGetHelperNum(methHnd))
    {
        DISASM_DUMP("Helper function: %p => %p\n", addr, methHnd);
        GetHelperAddrToMethodHandleMap()->Set(addr, methHnd);
    }
    else
    {
        DISASM_DUMP("Function: %p => %p\n", addr, methHnd);
        GetAddrToMethodHandleMap()->Set(addr, methHnd);
    }
}

/*****************************************************************************
 * Register a relocation.
 *
 * Arguments:
 *      relocAddr   - The absolute address the relocation applies to.
 *      targetAddr  - The absolute address the relocation points to.
 */

void DisAssembler::disRecordRelocation(size_t relocAddr, size_t targetAddr)
{
    if (!disComp->opts.doLateDisasm)
    {
        return;
    }

    DISASM_DUMP("Relocation %p => %p\n", relocAddr, targetAddr);
    GetRelocationMap()->Set(relocAddr, targetAddr);
}

/*****************************************************************************
 *
 * Disassemble the code which has been generated
 */

void DisAssembler::disAsmCode(BYTE* hotCodePtr, size_t hotCodeSize, BYTE* coldCodePtr, size_t coldCodeSize)
{
    if (!disComp->opts.doLateDisasm)
    {
        return;
    }

#ifdef DEBUG
    // Should we make it diffable?
    disDiffable = disComp->opts.dspDiffable;
#else  // !DEBUG
    // NOTE: non-debug builds are always diffable!
    disDiffable = true;
#endif // !DEBUG

#ifdef DEBUG
    const wchar_t* fileName = JitConfig.JitLateDisasmTo();
    if (fileName != nullptr)
    {
        errno_t ec = _wfopen_s(&disAsmFile, fileName, W("a+"));
        if (ec != 0)
        {
            disAsmFile = nullptr;
        }
    }
#else  // !DEBUG
    // NOTE: non-DEBUG builds always use jitstdout currently!
    disAsmFile = jitstdout;
#endif // !DEBUG

    if (disAsmFile == nullptr)
    {
        disAsmFile = jitstdout;
    }

    // As this writes to a common file, this is not reentrant.

    assert(hotCodeSize > 0);
    if (coldCodeSize == 0)
    {
        fprintf(disAsmFile, "************************** %hs:%hs size 0x%04IX **************************\n\n",
                disCurClassName, disCurMethodName, hotCodeSize);

        fprintf(disAsmFile, "Base address : %ph\n", dspAddr(hotCodePtr));
    }
    else
    {
        fprintf(disAsmFile,
                "************************** %hs:%hs hot size 0x%04IX cold size 0x%04IX **************************\n\n",
                disCurClassName, disCurMethodName, hotCodeSize, coldCodeSize);

        fprintf(disAsmFile, "Hot  address : %ph\n", dspAddr(hotCodePtr));
        fprintf(disAsmFile, "Cold address : %ph\n", dspAddr(coldCodePtr));
    }

    disStartAddr     = 0;
    disHotCodeBlock  = (size_t)hotCodePtr;
    disHotCodeSize   = hotCodeSize;
    disColdCodeBlock = (size_t)coldCodePtr;
    disColdCodeSize  = coldCodeSize;

    disTotalCodeSize = disHotCodeSize + disColdCodeSize;

    disLabels = new (disComp, CMK_DebugOnly) BYTE[disTotalCodeSize]();

    DisasmBuffer(disAsmFile, /* printIt */ true);
    fprintf(disAsmFile, "\n");

    if (disAsmFile != jitstdout)
    {
        fclose(disAsmFile);
    }
    else
    {
        fflush(disAsmFile);
    }
}

/*****************************************************************************/
// This function is called for every method. Checks if we are supposed to disassemble
// the method, and where to send the disassembly output.

void DisAssembler::disOpenForLateDisAsm(const char* curMethodName, const char* curClassName, PCCOR_SIGNATURE sig)
{
    if (!disComp->opts.doLateDisasm)
    {
        return;
    }

    disCurMethodName = curMethodName;
    disCurClassName  = curClassName;
}

/*****************************************************************************/

void DisAssembler::disInit(Compiler* pComp)
{
    assert(pComp);
    disComp                        = pComp;
    disHasName                     = false;
    disLabels                      = nullptr;
    disAddrToMethodHandleMap       = nullptr;
    disHelperAddrToMethodHandleMap = nullptr;
    disRelocationMap               = nullptr;
    disDiffable                    = false;
    disAsmFile                     = nullptr;
}

/*****************************************************************************/
#endif // LATE_DISASM
/*****************************************************************************/
