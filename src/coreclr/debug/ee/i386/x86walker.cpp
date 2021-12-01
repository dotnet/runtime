// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: x86walker.cpp
//

//
// x86 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"

#include "walker.h"

#include "frames.h"
#include "openum.h"


#ifdef TARGET_X86

//
// The x86 walker is currently pretty minimal.  It only recognizes call and return opcodes, plus a few jumps.  The rest
// is treated as unknown.
//
void NativeWalker::Decode()
{
    const BYTE *ip = m_ip;

    m_type = WALK_UNKNOWN;
    m_skipIP = NULL;
    m_nextIP = NULL;

    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: m_ip 0x%x\n", m_ip));
    //
    // Skip instruction prefixes
    //
    do
    {
        switch (*ip)
        {
        // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

        // Size overrides
        case 0x66: // Operand-Size
        case 0x67: // Address-Size

        // Lock
        case 0xf0:

        // String REP prefixes
        case 0xf1:
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            LOG((LF_CORDB, LL_INFO10000, "NW:Decode: prefix:%0.2x ", *ip));
            ip++;
            continue;

        default:
            break;
        }
    } while (0);

    // Read the opcode
    m_opcode = *ip++;

    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: ip 0x%x, m_opcode:%0.2x\n", ip, m_opcode));

    if (m_opcode == 0xcc)
    {
        m_opcode = DebuggerController::GetPatchedOpcode(m_ip);
        LOG((LF_CORDB, LL_INFO100000, "NW:Decode after patch look up: m_opcode:%0.2x\n", m_opcode));
    }

    // Analyze what we can of the opcode
    switch (m_opcode)
    {
    case 0xff:
        {

        BYTE modrm = *ip++;
        BYTE mod = (modrm & 0xC0) >> 6;
        BYTE reg = (modrm & 0x38) >> 3;
        BYTE rm  = (modrm & 0x07);

        BYTE *result = 0;
        WORD displace = 0;

        if ((reg != 2) && (reg != 3) && (reg != 4) && (reg != 5)) {
            //
            // This is not a CALL or JMP instruction, return, unknown.
            //
            return;
        }


        if (m_registers != NULL)
        {
            // Only try to decode registers if we actually have reg sets.
            switch (mod) {
            case 0:
            case 1:
            case 2:

                if (rm == 4) {

                    //
                    // Get values from the SIB byte
                    //
                    BYTE ss    = (*ip & 0xC0) >> 6;
                    BYTE index = (*ip & 0x38) >> 3;
                    BYTE base  = (*ip & 0x7);

                    ip++;

                    //
                    // Get starting value
                    //
                    if ((mod == 0) && (base == 5)) {
                        result = 0;
                    } else {
                        result = (BYTE *)(size_t)GetRegisterValue(base);
                    }

                    //
                    // Add in the [index]
                    //
                    if (index != 0x4) {
                        result = result + (GetRegisterValue(index) << ss);
                    }

                    //
                    // Finally add in the offset
                    //
                    if (mod == 0) {

                        if (base == 5) {
                            result = result + *((unsigned int *)ip);
                            displace = 7;
                        } else {
                            displace = 3;
                        }

                    } else if (mod == 1) {

                        result = result + *((char *)ip);
                        displace = 4;

                    } else { // == 2

                        result = result + *((unsigned int *)ip);
                        displace = 7;

                    }

                } else {

                    //
                    // Get the value we need from the register.
                    //

                    if ((mod == 0) && (rm == 5)) {
                        result = 0;
                    } else {
                        result = (BYTE *)GetRegisterValue(rm);
                    }

                    if (mod == 0) {

                        if (rm == 5) {
                            result = result + *((unsigned int *)ip);
                            displace = 6;
                        } else {
                            displace = 2;
                        }

                    } else if (mod == 1) {

                        result = result + *((char *)ip);
                        displace = 3;

                    } else { // == 2

                        result = result + *((unsigned int *)ip);
                        displace = 6;

                    }

                }

                //
                // Now dereference thru the result to get the resulting IP.
                //

                // If result is bad, then this means we can't predict what the nextIP will be.
                // That's ok - we just leave m_nextIp=NULL. We can still provide callers
                // with the proper walk-type.
                // In practice, this shouldn't happen unless the jit emits bad opcodes.
                if (result != NULL)
                {
                    result = (BYTE *)(*((unsigned int *)result));
                }

                break;

            case 3:
            default:

                result = (BYTE *)GetRegisterValue(rm);
                displace = 2;
                break;

            }
        } // have registers

        if ((reg == 2) || (reg == 3)) {
            m_type = WALK_CALL;
        } else if ((reg == 4) || (reg == 5)) {
            m_type = WALK_BRANCH;
        } else {
            break;
        }

        if (m_registers != NULL)
        {
            m_nextIP = result;
            m_skipIP = m_ip + displace;
        }

        break;
        }  // end of 0xFF case

    case 0xe8:
        {
        m_type = WALK_CALL;

        UINT32 disp = *((UINT32*)ip);
        m_nextIP = ip + 4 + disp;
        m_skipIP = ip + 4;

        break;
        }

    case 0xe9:
        {
        m_type = WALK_BRANCH;

        INT32 disp = *((INT32*)ip);
        m_nextIP = ip + 4 + disp;
        m_skipIP = ip + 4;

        break;
        }

    case 0x9a:
        m_type = WALK_CALL;

        m_nextIP = (BYTE*) *((UINT32*)ip);
        m_skipIP = ip + 4;

        break;

    case 0xc2:
    case 0xc3:
    case 0xca:
    case 0xcb:
        m_type = WALK_RETURN;
        break;

    default:
        break;
    }
}


//
// Given a regdisplay and a register number, return the value of the register.
//

DWORD NativeWalker::GetRegisterValue(int registerNumber)
{
    // If we're going to decode a register, then we'd better have a valid register set.
    PREFIX_ASSUME(m_registers != NULL);

    switch (registerNumber)
    {
    case 0:
        return *m_registers->GetEaxLocation();
        break;
    case 1:
        return *m_registers->GetEcxLocation();
        break;
    case 2:
        return *m_registers->GetEdxLocation();
        break;
    case 3:
        return *m_registers->GetEbxLocation();
        break;
    case 4:
        return m_registers->SP;
        break;
    case 5:
        return GetRegdisplayFP(m_registers);
        break;
    case 6:
        return *m_registers->GetEsiLocation();
        break;
    case 7:
        return *m_registers->GetEdiLocation();
        break;
    default:
        _ASSERTE(!"Invalid register number!");
    }

    return 0;
}


// static
void NativeWalker::DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
{
    //
    // Skip instruction prefixes
    //

    LOG((LF_CORDB, LL_INFO10000, "Patch decode: "));

    if (pInstrAttrib == NULL)
        return;

    const BYTE * origAddr = address;

    do
    {
        switch (*address)
        {
        // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

        // Size overrides
        case 0x66: // Operand-Size
        case 0x67: // Address-Size

        // Lock
        case 0xf0:

        // String REP prefixes
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            LOG((LF_CORDB, LL_INFO10000, "prefix:%0.2x ", *address));
            address++;
            continue;

        default:
            break;
        }
    } while (0);

    // There can be at most 4 prefixes.
    _ASSERTE(((address - origAddr) <= 4));

    //
    // Look at opcode to tell if it's a call or an
    // absolute branch.
    //

    pInstrAttrib->Reset();

    // Note that we only care about m_cbInstr, m_cbDisp, and m_dwOffsetToDisp for relative branches
    // (either call or jump instructions).

    switch (*address)
    {
    case 0xEA: // JMP far
    case 0xC2: // RET
    case 0xC3: // RET N
        pInstrAttrib->m_fIsAbsBranch = true;
        LOG((LF_CORDB, LL_INFO10000, "ABS:%0.2x\n", *address));
        break;

    case 0xE8: // CALL relative
        pInstrAttrib->m_fIsCall      = true;
        pInstrAttrib->m_fIsRelBranch = true;
        LOG((LF_CORDB, LL_INFO10000, "CALL REL:%0.2x\n", *address));

        address += 1;
        pInstrAttrib->m_cbDisp = 4;
        break;

    case 0xC8: // ENTER
        pInstrAttrib->m_fIsCall = true;
        pInstrAttrib->m_fIsAbsBranch = true;
        LOG((LF_CORDB, LL_INFO10000, "CALL ABS:%0.2x\n", *address));
        break;

    case 0xFF: // CALL/JMP modr/m

        //
        // Read opcode modifier from modr/m
        //

        switch ((address[1]&0x38)>>3)
        {
        case 2:
        case 3:
            pInstrAttrib->m_fIsCall = true;
            FALLTHROUGH;
        case 4:
        case 5:
            pInstrAttrib->m_fIsAbsBranch = true;
        }
        LOG((LF_CORDB, LL_INFO10000, "CALL/JMP modr/m:%0.2x\n", *address));
        break;

    case 0x9A: // CALL ptr16:32
        pInstrAttrib->m_fIsCall      = true;
        pInstrAttrib->m_fIsAbsBranch = true;
        break;

    case 0xEB: // JMP rel8
        pInstrAttrib->m_fIsRelBranch = true;

        address += 1;
        pInstrAttrib->m_cbDisp = 1;
        break;

    case 0xE9: // JMP rel32
        pInstrAttrib->m_fIsRelBranch = true;

        address += 1;
        pInstrAttrib->m_cbDisp = 4;
        break;

    case 0x0F: // Jcc (conditional jump)
        // If the second opcode byte is betwen 0x80 and 0x8F, then it's a conditional jump.
        // Conditional jumps are always relative.
        if ((address[1] & 0xF0) == 0x80)
        {
            pInstrAttrib->m_fIsCond      = true;
            pInstrAttrib->m_fIsRelBranch = true;

            address += 2;   // 2-byte opcode
            pInstrAttrib->m_cbDisp = 4;
        }
        break;

    case 0x70:
    case 0x71:
    case 0x72:
    case 0x73:
    case 0x74:
    case 0x75:
    case 0x76:
    case 0x77:
    case 0x78:
    case 0x79:
    case 0x7A:
    case 0x7B:
    case 0x7C:
    case 0x7D:
    case 0x7E:
    case 0x7F: // Jcc (conditional jump)
    case 0xE3: // JCXZ/JECXZ (jump on CX/ECX zero)
        pInstrAttrib->m_fIsCond      = true;
        pInstrAttrib->m_fIsRelBranch = true;

        address += 1;
        pInstrAttrib->m_cbDisp = 1;
        break;

    default:
        LOG((LF_CORDB, LL_INFO10000, "NORMAL:%0.2x\n", *address));
    }

    // Get additional information for relative branches.
    if (pInstrAttrib->m_fIsRelBranch)
    {
        _ASSERTE(pInstrAttrib->m_cbDisp != 0);
        pInstrAttrib->m_dwOffsetToDisp = (address - origAddr);

        // Relative jump and call instructions don't use the SIB byte, and there is no immediate value.
        // So the instruction size is just the offset to the displacement plus the size of the displacement.
        pInstrAttrib->m_cbInstr = pInstrAttrib->m_dwOffsetToDisp + pInstrAttrib->m_cbDisp;
    }
}


#endif
