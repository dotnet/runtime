// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: Amd64walker.cpp
// 

//
// AMD64 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"

#include "walker.h"

#include "frames.h"
#include "openum.h"

#ifdef _TARGET_AMD64_

//
// The AMD64 walker is currently pretty minimal.  It only recognizes call and return opcodes, plus a few jumps.  The rest
// is treated as unknown.
//
void NativeWalker::Decode()
{
    const BYTE *ip = m_ip;

    m_type = WALK_UNKNOWN;
    m_skipIP = NULL;
    m_nextIP = NULL;

    BYTE rex = NULL;
    
    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: m_ip 0x%x\n", m_ip));

    BYTE prefix = *ip;
    if (prefix == 0xcc)
    {
        prefix = (BYTE)DebuggerController::GetPatchedOpcode(m_ip);
        LOG((LF_CORDB, LL_INFO100000, "NW:Decode 1st byte was patched, might have been prefix\n"));
    }

    //
    // Skip instruction prefixes
    //
    do 
    {
        switch (prefix)
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
            LOG((LF_CORDB, LL_INFO10000, "NW:Decode: prefix:%0.2x ", prefix));
            ip++;
            continue;

        // REX register extension prefixes
        case 0x40:            
        case 0x41:
        case 0x42:
        case 0x43:
        case 0x44:
        case 0x45:
        case 0x46:
        case 0x47:
        case 0x48:
        case 0x49:
        case 0x4a:
        case 0x4b:
        case 0x4c:
        case 0x4d:
        case 0x4e:
        case 0x4f:
            LOG((LF_CORDB, LL_INFO10000, "NW:Decode: REX prefix:%0.2x ", prefix));
            // make sure to set rex to prefix, not *ip because *ip still represents the 
            // codestream which has a 0xcc in it.
            rex = prefix;
            ip++;
            continue;

        default:
            break;
        }
    } while (0);

    // Read the opcode
    m_opcode = *ip++;

    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: ip 0x%x, m_opcode:%0.2x\n", ip, m_opcode));

    // Don't remove this, when we did the check above for the prefix we didn't modify the codestream
    // and since m_opcode was just taken directly from the code stream it will be patched if we 
    // didn't have a prefix
    if (m_opcode == 0xcc)
    {
        m_opcode = (BYTE)DebuggerController::GetPatchedOpcode(m_ip);
        LOG((LF_CORDB, LL_INFO100000, "NW:Decode after patch look up: m_opcode:%0.2x\n", m_opcode));
    }

    // Setup rex bits if needed
    BYTE rex_b = 0;
    BYTE rex_x = 0;
    BYTE rex_r = 0;

    if (rex != NULL)
    {
        rex_b = (rex & 0x1);       // high bit to modrm r/m field or SIB base field or OPCODE reg field    -- Hmm, when which?
        rex_x = (rex & 0x2) >> 1;  // high bit to sib index field
        rex_r = (rex & 0x4) >> 2;  // high bit to modrm reg field
    }

    // Analyze what we can of the opcode
    switch (m_opcode)
    {
        case 0xff:
        {
            BYTE modrm = *ip++;

            // Ignore "inc dword ptr [reg]" instructions
            if (modrm == 0)
                break;
            
            BYTE mod = (modrm & 0xC0) >> 6;
            BYTE reg = (modrm & 0x38) >> 3;
            BYTE rm  = (modrm & 0x07);

            reg   |= (rex_r << 3);
            rm    |= (rex_b << 3);

            if ((reg < 2) || (reg > 5 && reg < 8) || (reg > 15)) {
                // not a valid register for a CALL or BRANCH
                return;
            }
            
            BYTE *result;
            WORD displace;

            // See: Tables A-15,16,17 in AMD Dev Manual 3 for information
            //      about how the ModRM/SIB/REX bytes interact.

            switch (mod) 
            {
            case 0:
            case 1:
            case 2:     
                if ((rm & 0x07) == 4) // we have an SIB byte following
                {   
                    //
                    // Get values from the SIB byte
                    //
                    BYTE sib   = *ip;

                    _ASSERT(sib != NULL);
                    
                    BYTE ss    = (sib & 0xC0) >> 6;
                    BYTE index = (sib & 0x38) >> 3;
                    BYTE base  = (sib & 0x07);

                    index |= (rex_x << 3);
                    base  |= (rex_b << 3);

                    ip++;

                    //
                    // Get starting value
                    //
                    if ((mod == 0) && ((base & 0x07) == 5))
                    {
                        result = 0;
                    } 
                    else 
                    {
                        result = (BYTE *)(size_t)GetRegisterValue(base);
                    } 

                    //
                    // Add in the [index]
                    //
                    if (index != 0x4) 
                    {
                        result = result + (GetRegisterValue(index) << ss);
                    }

                    //
                    // Finally add in the offset
                    //
                    if (mod == 0) 
                    {
                        if ((base & 0x07) == 5) 
                        {
                            result = result + *((INT32*)ip);
                            displace = 7;
                        } 
                        else 
                        {
                            displace = 3;
                        }
                    } 
                    else if (mod == 1) 
                    {
                        result = result + *((INT8*)ip);
                        displace = 4;
                    } 
                    else // mod == 2
                    {
                        result = result + *((INT32*)ip);
                        displace = 7;
                    }

                } 
                else 
                {
                    //
                    // Get the value we need from the register.
                    //

                    // Check for RIP-relative addressing mode.
                    if ((mod == 0) && ((rm & 0x07) == 5)) 
                    {
                        displace = 6;   // 1 byte opcode + 1 byte modrm + 4 byte displacement (signed)
                        result = const_cast<BYTE *>(m_ip) + displace + *(reinterpret_cast<const INT32*>(ip));
                    } 
                    else 
                    {
                        result = (BYTE *)GetRegisterValue(rm);

                        if (mod == 0) 
                        {
                            displace = 2;
                        } 
                        else if (mod == 1) 
                        {
                            result = result + *((INT8*)ip);
                            displace = 3;
                        } 
                        else // mod == 2
                        {
                            result = result + *((INT32*)ip);
                            displace = 6;
                        }
                    }
                }

                //
                // Now dereference thru the result to get the resulting IP.
                //
                result = (BYTE *)(*((UINT64*)result));

                break;

            case 3:
            default:            
                // The operand is stored in a register.
                result = (BYTE *)GetRegisterValue(rm);
                displace = 2;

                break;

            }

            // the instruction uses r8-r15, add in the extra byte to the displacement
            // for the REX prefix which was used to specify the extended register
            if (rex != NULL)
            {
                displace++;
            }

            // because we already checked register validity for CALL/BRANCH 
            // instructions above we can assume that there is no other option
            if ((reg == 4) || (reg == 5)) 
            {
                m_type = WALK_BRANCH;
            }
            else 
            {
                m_type = WALK_CALL;
            }
            m_nextIP = result;
            m_skipIP = m_ip + displace;
            break; 
        }
        case 0xe8:
        {
            m_type = WALK_CALL;

            // Sign-extend the displacement is necessary.
            INT32 disp = *((INT32*)ip);
            m_nextIP = ip + 4 + (disp < 0 ? (disp | 0xffffffff00000000) : disp);
            m_skipIP = ip + 4;

            break;
        }
        case 0xe9:
        {
            m_type = WALK_BRANCH;

            // Sign-extend the displacement is necessary.
            INT32 disp = *((INT32*)ip);
            m_nextIP = ip + 4 + (disp < 0 ? (disp | 0xffffffff00000000) : disp);
            m_skipIP = ip + 4;

            break;
        }
        case 0xc2:
        case 0xc3:
        case 0xca:
        case 0xcb:
        {
            m_type = WALK_RETURN;
            break;
        }
        default:
            break;
    }
}


//
// Given a regdisplay and a register number, return the value of the register.
//

UINT64 NativeWalker::GetRegisterValue(int registerNumber)
{
    if (m_registers == NULL) {
        return 0;
    }

    switch (registerNumber)
    {
        case 0:
            return m_registers->pCurrentContext->Rax;
            break;
        case 1:
            return m_registers->pCurrentContext->Rcx;
            break;
        case 2:
            return m_registers->pCurrentContext->Rdx;
            break;
        case 3:
            return m_registers->pCurrentContext->Rbx;
            break;
        case 4:
            return m_registers->pCurrentContext->Rsp;
            break;
        case 5:
            return m_registers->pCurrentContext->Rbp;
            break;
        case 6:
            return m_registers->pCurrentContext->Rsi;
            break;
        case 7:
            return m_registers->pCurrentContext->Rdi;
            break;
        case 8:
            return m_registers->pCurrentContext->R8;
            break;
        case 9:
            return m_registers->pCurrentContext->R9;
            break;
        case 10:
            return m_registers->pCurrentContext->R10;
            break;
        case 11:
            return m_registers->pCurrentContext->R11;
            break;
        case 12:
            return m_registers->pCurrentContext->R12;
            break;
        case 13:
            return m_registers->pCurrentContext->R13;
            break;
        case 14:
            return m_registers->pCurrentContext->R14;
            break;
        case 15:
            return m_registers->pCurrentContext->R15;
            break;
        default:
            _ASSERTE(!"Invalid register number!");
    }

    return 0;
}


//          mod     reg     r/m
// bits     7-6     5-3     2-0 
struct ModRMByte
{
    BYTE rm :3;
    BYTE reg:3;
    BYTE mod:2;
}; 

//         fixed    W       R       X       B
// bits    7-4      3       2       1       0
struct RexByte
{
    BYTE b:1;
    BYTE x:1;
    BYTE r:1;
    BYTE w:1;
    BYTE fixed:4;
};

// static
void NativeWalker::DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
{
    //
    // Skip instruction prefixes
    //

    LOG((LF_CORDB, LL_INFO10000, "Patch decode: "));

    // for reads and writes where the destination is a RIP-relative address pInstrAttrib->m_cOperandSize will contain the size in bytes of the pointee; in all other
    // cases it will be zero.  if the RIP-relative address is being written to then pInstrAttrib->m_fIsWrite will be true; in all other cases it will be false.
    // similar to cbImmedSize in some cases we'll set pInstrAttrib->m_cOperandSize to 0x3 meaning that the prefix will determine the size if one is specified.
    pInstrAttrib->m_cOperandSize = 0;
    pInstrAttrib->m_fIsWrite = false;

    if (pInstrAttrib == NULL)
    {
        return;
    }

    // These three legacy prefixes are used to modify some of the two-byte opcodes.
    bool  fPrefix66 = false;
    bool  fPrefixF2 = false;
    bool  fPrefixF3 = false;

    bool  fRex      = false;
    bool  fModRM    = false;

    RexByte   rex   = {0};
    ModRMByte modrm = {0};

    // We use 0x3 to indicate that we need to look at the operand-size override and the rex byte 
    // to determine whether the immediate size is 2 bytes or 4 bytes.
    BYTE      cbImmedSize = 0; 

    const BYTE* originalAddr = address;
    
    do
    {
        switch (*address)
        {
        // Operand-Size override
        case 0x66:
            fPrefix66 = true;
            goto LLegacyPrefix;

        // Repeat (REP/REPE/REPZ)
        case 0xf2:
            fPrefixF2 = true;
            goto LLegacyPrefix;

        // Repeat (REPNE/REPNZ)
        case 0xf3:
            fPrefixF3 = true;
            goto LLegacyPrefix;

        // Address-Size override
        case 0x67:          // fall through

        // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS    // fall through

        // Lock
        case 0xf0:
LLegacyPrefix:
            LOG((LF_CORDB, LL_INFO10000, "prefix:%0.2x ", *address));
            address++;
            continue;

        // REX register extension prefixes
        case 0x40:            
        case 0x41:
        case 0x42:
        case 0x43:
        case 0x44:
        case 0x45:
        case 0x46:
        case 0x47:
        case 0x48:
        case 0x49:
        case 0x4a:
        case 0x4b:
        case 0x4c:
        case 0x4d:
        case 0x4e:
        case 0x4f:
            LOG((LF_CORDB, LL_INFO10000, "prefix:%0.2x ", *address));
            fRex = true;
            rex  = *(RexByte*)address;
            address++;
            continue;

        default:
            break;
        }
    } while (0);

    pInstrAttrib->Reset();

    BYTE opcode0 = *address;
    BYTE opcode1 = *(address + 1);      // this is only valid if the first opcode byte is 0x0F

    // Handle AVX encodings.  Note that these can mostly be handled as if they are aliases
    // for a corresponding SSE encoding.
    // See Figure 2-9 in "Intel 64 and IA-32 Architectures Software Developer's Manual".

    if (opcode0 == 0xC4 || opcode0 == 0xC5)
    {
        BYTE pp;
        if (opcode0 == 0xC4)
        {
            BYTE opcode2 = *(address + 2);
            address++;

            // REX bits are encoded in inverted form.
            // R,X, and B are the top bits (in that order) of opcode1.
            // W is the top bit of opcode2.
            if ((opcode1 & 0x80) != 0)
            {
                rex.b = 1;
                fRex = true;
            }
            if ((opcode1 & 0x40) == 0)
            {
                rex.x = 1;
                fRex = true;
            }
            if ((opcode1 & 0x20) == 0)
            {
                rex.b = 1;
                fRex = true;
            }
            if ((opcode2 & 0x80) != 0)
            {
                rex.w = 1;
                fRex = true;
            }

            pp = opcode2 & 0x3;

            BYTE mmBits = opcode1 & 0x1f;
            BYTE impliedOpcode1 = 0;
            switch(mmBits)
            {
            case 1:  break;     // No implied leading byte.
            case 2:  impliedOpcode1 = 0x38;                   break;
            case 3:  impliedOpcode1 = 0x3A;                   break;
            default: _ASSERTE(!"NW::DIFPS - invalid opcode"); break;
            }

            if (impliedOpcode1 != 0)
            {
                opcode1 = impliedOpcode1;
            }
            else
            {
                opcode1 = *address;
                address++;
            }
        }
        else
        {
            pp = opcode1 & 0x3;
            if ((opcode1 & 0x80) == 0)
            {
                // The two-byte VEX encoding only encodes the 'R' bit.
                fRex = true;
                rex.r = 1;
            }
            opcode1 = *address;
            address++;
        }
        opcode0 = 0x0f;
        switch (pp)
        {
        case 1: fPrefix66 = true; break;
        case 2: fPrefixF3 = true; break;
        case 3: fPrefixF2 = true; break;
        }
    }

    // The following opcode decoding follows the tables in "Appendix A Opcode and Operand Encodings" of 
    // "AMD64 Architecture Programmer's Manual Volume 3" 

    // one-byte opcodes
    if (opcode0 != 0x0F)
    {
        BYTE highNibble = (opcode0 & 0xF0) >> 4;
        BYTE lowNibble  = (opcode0 & 0x0F);

        switch (highNibble)
        {
        case 0x0:
        case 0x1:
        case 0x2:
        case 0x3:
            if ((lowNibble == 0x6) || (lowNibble == 0x7) || (lowNibble == 0xE) || (lowNibble == 0xF))
            {
                _ASSERTE(!"NW::DIFPS - invalid opcode");
            }

            // CMP
            if ( (lowNibble <= 0x3) ||
                 ((lowNibble >= 0x8) && (lowNibble <= 0xB)) )
            {
                fModRM = true;    
            }

            // ADD/XOR reg/mem, reg
            if (lowNibble == 0x0)
            {
                pInstrAttrib->m_cOperandSize = 0x1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x1)
            {
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // XOR reg, reg/mem
            else if (lowNibble == 0x2)
            {
                pInstrAttrib->m_cOperandSize = 0x1;
            }
            else if (lowNibble == 0x3)
            {
                pInstrAttrib->m_cOperandSize = 0x3;
            }

            break;

        case 0x4:
        case 0x5:
            break;

        case 0x6:                
            // IMUL
            if (lowNibble == 0x9)
            {
                fModRM = true;
                cbImmedSize = 0x3;
            }
            else if (lowNibble == 0xB)
            {
                fModRM = true;
                cbImmedSize = 0x1;
            }
            else if (lowNibble == 0x3)
            {                 
                if (fRex)
                {
                    // MOVSXD
                    fModRM = true;
                }
            }
            break;

        case 0x7:
            break;

        case 0x8:
            fModRM = true;

            // Group 1: lowNibble in [0x0, 0x3]
            _ASSERTE(lowNibble != 0x2);

            // ADD/XOR reg/mem, imm
            if (lowNibble == 0x0)
            {
                cbImmedSize = 1;
                pInstrAttrib->m_cOperandSize = 1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x1)
            {
                cbImmedSize = 3;
                pInstrAttrib->m_cOperandSize = 3;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x3)
            {
                cbImmedSize = 1;
                pInstrAttrib->m_cOperandSize = 3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // MOV reg/mem, reg
            else if (lowNibble == 0x8)
            {
                pInstrAttrib->m_cOperandSize = 0x1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x9)
            {
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // MOV reg, reg/mem
            else if (lowNibble == 0xA)
            {
                pInstrAttrib->m_cOperandSize = 0x1;
            }
            else if (lowNibble == 0xB)
            {
                pInstrAttrib->m_cOperandSize = 0x3;
            }

            break;

        case 0x9:
        case 0xA:
        case 0xB:
            break;

        case 0xC:
            if ((lowNibble == 0x4) || (lowNibble == 0x5) || (lowNibble == 0xE))
            {
                _ASSERTE(!"NW::DIFPS - invalid opcode");
            }

            // RET
            if ((lowNibble == 0x2) || (lowNibble == 0x3))
            {
                break;
            }

            // Group 2 (part 1): lowNibble in [0x0, 0x1]
            // RCL reg/mem, imm
            if (lowNibble == 0x0)
            {
                fModRM = true;
                cbImmedSize = 0x1;
                pInstrAttrib->m_cOperandSize = 0x1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x1)
            {
                fModRM = true;
                cbImmedSize = 0x1;
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // Group 11: lowNibble in [0x6, 0x7]
            // MOV reg/mem, imm
            else if (lowNibble == 0x6)
            {
                fModRM = true;
                cbImmedSize = 1;
                pInstrAttrib->m_cOperandSize = 1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x7)
            {
                fModRM = true;
                cbImmedSize = 3;
                pInstrAttrib->m_cOperandSize = 3;
                pInstrAttrib->m_fIsWrite = true;
            }
            break;

        case 0xD:
            // Group 2 (part 2): lowNibble in [0x0, 0x3] 
            // RCL reg/mem, 1/reg
            if (lowNibble == 0x0 || lowNibble == 0x2)
            {
                fModRM = true;
                pInstrAttrib->m_cOperandSize = 0x1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x1 || lowNibble == 0x3)
            {
                fModRM = true;
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }

            // x87 instructions: lowNibble in [0x8, 0xF]
            // - the entire ModRM byte is used to modify the opcode, 
            //   so the ModRM byte cannot be used in RIP-relative addressing
            break;

        case 0xE:
            break;

        case 0xF:
            // Group 3: lowNibble in [0x6, 0x7]
            // TEST
            if  ((lowNibble == 0x6) || (lowNibble == 0x7))
            {
                fModRM = true;

                modrm = *(ModRMByte*)(address + 1);
                if ((modrm.reg == 0x0) || (modrm.reg == 0x1))
                {
                    if (lowNibble == 0x6)
                    {
                        cbImmedSize = 0x1;
                    }
                    else
                    {
                        cbImmedSize = 0x3;
                    }
                }
            }
            // Group 4: lowNibble == 0xE
            // INC reg/mem
            else if (lowNibble == 0xE)
            {
                fModRM = true;
                pInstrAttrib->m_cOperandSize = 1;
                pInstrAttrib->m_fIsWrite = true;
            }
            // Group 5: lowNibble == 0xF
            else if (lowNibble == 0xF)
            {
                fModRM = true;
                pInstrAttrib->m_cOperandSize = 3;
                pInstrAttrib->m_fIsWrite = true;
            }
            break;
        }

        address += 1;
        if (fModRM)
        {
            modrm = *(ModRMByte*)address;
            address += 1;
        }
    }
    // two-byte opcodes
    else
    {
        BYTE highNibble = (opcode1 & 0xF0) >> 4;
        BYTE lowNibble  = (opcode1 & 0x0F);

        switch (highNibble)
        {
        case 0x0:
            // Group 6: lowNibble == 0x0
            if (lowNibble == 0x0)
            {
                fModRM = true;
            }
            // Group 7: lowNibble == 0x1
            else if (lowNibble == 0x1)
            {
                fModRM = true;
            }
            else if ((lowNibble == 0x2) || (lowNibble == 0x3))
            {
                fModRM = true;
            }
            // Group p: lowNibble == 0xD
            else if (lowNibble == 0xD)
            {
                fModRM = true;
            }
            // 3DNow! instructions: lowNibble == 0xF
            // - all 3DNow! instructions use the ModRM byte
            else if (lowNibble == 0xF)
            {
                fModRM = true;
                cbImmedSize = 0x1;
            }
            break;

        case 0x1:   // Group 16: lowNibble == 0x8
            // MOVSS xmm, xmm/mem (low nibble 0x0)
            // MOVSS xmm/mem, xmm (low nibble 0x1)
            if (lowNibble <= 0x1)
            {
                fModRM = true;
                if (fPrefixF2 || fPrefixF3)
                    pInstrAttrib->m_cOperandSize = 0x8;
                else
                    pInstrAttrib->m_cOperandSize = 0x10;

                if (lowNibble == 0x1)
                    pInstrAttrib->m_fIsWrite = true;

                break;
            }
        case 0x2:   // fall through
            fModRM = true;
            if (lowNibble == 0x8 || lowNibble == 0x9)
            {
                pInstrAttrib->m_cOperandSize = 0x10;

                if (lowNibble == 0x9)
                    pInstrAttrib->m_fIsWrite = true;
            }
            break;

        case 0x3:
            break;

        case 0x4:
        case 0x5:
        case 0x6:   // fall through
            fModRM = true;
            break;

        case 0x7:
            if (lowNibble == 0x0)
            {
                fModRM = true;
                cbImmedSize = 0x1;
            }
            else if ((lowNibble >= 0x1) && (lowNibble <= 0x3))
            {
                _ASSERTE(!fPrefixF2 && !fPrefixF3);

                // Group 12: lowNibble == 0x1
                // Group 13: lowNibble == 0x2
                // Group 14: lowNibble == 0x3
                fModRM = true;
                cbImmedSize = 0x1;
            }
            else if ((lowNibble >= 0x4) && (lowNibble <= 0x6))
            {
                fModRM = true;
            }
            // MOVD reg/mem, mmx for 0F 7E
            else if ((lowNibble == 0xE) || (lowNibble == 0xF))
            {
                _ASSERTE(!fPrefixF2);

                fModRM = true;
            }
            break;

        case 0x8:
            break;

        case 0x9:
            fModRM = true;
            break;

        case 0xA:
            if ((lowNibble >= 0x3) && (lowNibble <= 0x5))
            {
                // BT reg/mem, reg
                fModRM = true;
                if (lowNibble == 0x3)
                {
                    pInstrAttrib->m_cOperandSize = 0x3;
                    pInstrAttrib->m_fIsWrite = true;
                }
                // SHLD reg/mem, imm
                else if (lowNibble == 0x4)
                {
                    cbImmedSize = 0x1;
                }
            }
            else if (lowNibble >= 0xB)
            {
                fModRM = true;
                // BTS reg/mem, reg
                if (lowNibble == 0xB)
                {
                    pInstrAttrib->m_cOperandSize = 0x3;
                    pInstrAttrib->m_fIsWrite = true;
                }
                // SHRD reg/mem, imm
                else if (lowNibble == 0xC)
                {
                    cbImmedSize = 0x1;
                }
                // Group 15: lowNibble == 0xE
            }
            break;

        case 0xB:
            // Group 10: lowNibble == 0x9
            // - this entire group is invalid
            _ASSERTE((lowNibble != 0x8) && (lowNibble != 0x9));

            fModRM = true;
            // CMPXCHG reg/mem, reg
            if (lowNibble == 0x0)
            {
                pInstrAttrib->m_cOperandSize = 0x1;
                pInstrAttrib->m_fIsWrite = true;
            }
            else if (lowNibble == 0x1)
            {
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // Group 8: lowNibble == 0xA
            // BTS reg/mem, imm
            else if (lowNibble == 0xA)
            {
                cbImmedSize = 0x1;
                pInstrAttrib->m_cOperandSize = 0x3;
                pInstrAttrib->m_fIsWrite = true;
            }
            // MOVSX reg, reg/mem
            else if (lowNibble == 0xE)
            {
                pInstrAttrib->m_cOperandSize = 1;
            }
            else if (lowNibble == 0xF)
            {
                pInstrAttrib->m_cOperandSize = 2;
            }
            break;

        case 0xC:
            if (lowNibble <= 0x7)
            {
                fModRM = true;
                // XADD reg/mem, reg
                if (lowNibble == 0x0)
                {
                    pInstrAttrib->m_cOperandSize = 0x1;
                    pInstrAttrib->m_fIsWrite = true;
                }
                else if (lowNibble == 0x1)
                {
                    pInstrAttrib->m_cOperandSize = 0x3;
                    pInstrAttrib->m_fIsWrite = true;
                }
                else if ( (lowNibble == 0x2) || 
                     ((lowNibble >= 0x4) && (lowNibble <= 0x6)) )
                {
                    cbImmedSize = 0x1;
                }
            }
            break;

        case 0xD:
        case 0xE:
        case 0xF:   // fall through
            fModRM = true;
            break;
        }

        address += 2;
        if (fModRM)
        {
            modrm = *(ModRMByte*)address; 
            address += 1;
        }
    }

    // Check for RIP-relative addressing
    if (fModRM && (modrm.mod == 0x0) && (modrm.rm == 0x5))
    {
        // SIB byte cannot be present with RIP-relative addressing.

        pInstrAttrib->m_dwOffsetToDisp = (DWORD)(address - originalAddr);
        _ASSERTE(pInstrAttrib->m_dwOffsetToDisp <= MAX_INSTRUCTION_LENGTH);

        // Add 4 to the address for the displacement.
        address += 4;

        // Further adjust the address by the size of the cbImmedSize (if any).
        if (cbImmedSize == 0x3)
        {
            // The size of the cbImmedSizeiate depends on the effective operand size:
            // 2 bytes if the effective operand size is 16-bit, or
            // 4 bytes if the effective operand size is 32- or 64-bit.
            if (fPrefix66)
            {
                cbImmedSize = 0x2;
            }
            else
            {
                cbImmedSize = 0x4;
            }
        }
        address += cbImmedSize;

        // if this is a read or write to a RIP-relative address then update pInstrAttrib->m_cOperandSize with the size of the pointee.
        if (pInstrAttrib->m_cOperandSize == 0x3)
        {
            if (fPrefix66)
                pInstrAttrib->m_cOperandSize = 0x2; // WORD*
            else
                pInstrAttrib->m_cOperandSize = 0x4; // DWORD*

            if (fRex && rex.w == 0x1)
            {
                _ASSERTE(pInstrAttrib->m_cOperandSize == 0x4);
                pInstrAttrib->m_cOperandSize = 0x8; // QWORD*
            }
        }

        pInstrAttrib->m_cbInstr = (DWORD)(address - originalAddr);
        _ASSERTE(pInstrAttrib->m_cbInstr <= MAX_INSTRUCTION_LENGTH);
    }
    else
    {
        // not a RIP-relative address so set to default values
        pInstrAttrib->m_cOperandSize = 0;
        pInstrAttrib->m_fIsWrite = false;
    }

    //
    // Look at opcode to tell if it's a call or an
    // absolute branch.
    //
    switch (opcode0)
    {
        case 0xC2: // RET
        case 0xC3: // RET N
            pInstrAttrib->m_fIsAbsBranch = true;
            LOG((LF_CORDB, LL_INFO10000, "ABS:%0.2x\n", opcode0));
            break;

        case 0xE8: // CALL relative
            pInstrAttrib->m_fIsCall = true;
            LOG((LF_CORDB, LL_INFO10000, "CALL REL:%0.2x\n", opcode0));
            break;

        case 0xC8: // ENTER
            pInstrAttrib->m_fIsCall = true;
            pInstrAttrib->m_fIsAbsBranch = true;
            LOG((LF_CORDB, LL_INFO10000, "CALL ABS:%0.2x\n", opcode0));
            break;

        case 0xFF: // CALL/JMP modr/m
            //
            // Read opcode modifier from modr/m
            //

            _ASSERTE(fModRM);
            switch (modrm.reg)
            {
                case 2:
                case 3:
                    pInstrAttrib->m_fIsCall = true;
                    // fall through
                case 4:
                case 5:
                    pInstrAttrib->m_fIsAbsBranch = true;
            }
            LOG((LF_CORDB, LL_INFO10000, "CALL/JMP modr/m:%0.2x\n", opcode0));
            break;

        default:
            LOG((LF_CORDB, LL_INFO10000, "NORMAL:%0.2x\n", opcode0));
    }

    if (pInstrAttrib->m_cOperandSize == 0x0)
    {
        // if an operand size wasn't computed (likely because the decoder didn't understand the instruction) then set
        // the size to the max buffer size.  this is a fall-back to the dev10 behavior and is applicable for reads only.
        _ASSERTE(!pInstrAttrib->m_fIsWrite);
        pInstrAttrib->m_cOperandSize = SharedPatchBypassBuffer::cbBufferBypass;
    }
}


#endif
