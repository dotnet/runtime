// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "amd64InstrDecode.h"

#ifdef TARGET_AMD64

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

    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: m_ip 0x%p\n", m_ip));

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

    LOG((LF_CORDB, LL_INFO100000, "NW:Decode: ip 0x%p, m_opcode:%0.2x\n", ip, m_opcode));

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
    ModRMByte(BYTE init) :
        rm(init & 0x7),
        reg((init >> 3) & 0x7),
        mod(init >> 6)
    {
    }

    BYTE rm :3;
    BYTE reg:3;
    BYTE mod:2;
};

static bool IsModRm(Amd64InstrDecode::InstrForm form, int pp, bool W, bool L, bool fPrefix66)
{
    bool modrm = true;

    switch (form)
    {
    case Amd64InstrDecode::InstrForm::None:
    case Amd64InstrDecode::InstrForm::I1B:
    case Amd64InstrDecode::InstrForm::I2B:
    case Amd64InstrDecode::InstrForm::I3B:
    case Amd64InstrDecode::InstrForm::I4B:
    case Amd64InstrDecode::InstrForm::I8B:
    case Amd64InstrDecode::InstrForm::WP_I4B_or_I4B_or_I2B:
    case Amd64InstrDecode::InstrForm::WP_I8B_or_I4B_or_I2B:
        modrm = false;
        break;
    case Amd64InstrDecode::InstrForm::I1B_W_None_or_MOp_M16B:
        modrm = W ? false : true;
        break;
    default:
        if (form & Amd64InstrDecode::InstrForm::Extension)
            modrm = true;
        break;
    }
    return modrm;
}

static bool IsWrite(Amd64InstrDecode::InstrForm form, int pp, bool W, bool L, bool fPrefix66)
{
    bool isWrite = false;
    switch (form)
    {
    case Amd64InstrDecode::InstrForm::M1st_I1B_L_M16B_or_M8B:
    case Amd64InstrDecode::InstrForm::M1st_I1B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::M1st_I1B_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::M1st_L_M32B_or_M16B:
    case Amd64InstrDecode::InstrForm::M1st_M16B:
    case Amd64InstrDecode::InstrForm::M1st_M16B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M1B:
    case Amd64InstrDecode::InstrForm::M1st_M1B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M2B:
    case Amd64InstrDecode::InstrForm::M1st_M2B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M4B:
    case Amd64InstrDecode::InstrForm::M1st_M4B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M8B:
    case Amd64InstrDecode::InstrForm::M1st_MUnknown:
    case Amd64InstrDecode::InstrForm::M1st_W_M4B_or_M1B:
    case Amd64InstrDecode::InstrForm::M1st_W_M8B_or_M2B:
    case Amd64InstrDecode::InstrForm::M1st_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B:
    case Amd64InstrDecode::InstrForm::M1st_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOnly_M10B:
    case Amd64InstrDecode::InstrForm::MOnly_M1B:
    case Amd64InstrDecode::InstrForm::MOnly_M2B:
    case Amd64InstrDecode::InstrForm::MOnly_M4B:
    case Amd64InstrDecode::InstrForm::MOnly_M8B:
    case Amd64InstrDecode::InstrForm::MOnly_MUnknown:
    case Amd64InstrDecode::InstrForm::MOnly_P_M6B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOnly_W_M16B_or_M8B:
    case Amd64InstrDecode::InstrForm::MOnly_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOnly_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOnly_WP_M8B_or_M8B_or_M2B:
        isWrite = true;
        break;
    default:
        break;
    }
    return isWrite;
}

static uint8_t opSize(Amd64InstrDecode::InstrForm form, int pp, bool W, bool L, bool fPrefix66)
{
    uint8_t opSize = 0;
    bool P = !((pp == 1) || fPrefix66);
    switch (form)
    {
    // M32B
    case Amd64InstrDecode::InstrForm::MOp_M32B:
    case Amd64InstrDecode::InstrForm::MOp_M32B_I1B:
        opSize = 32;
        break;
    // L_M32B_or_M16B
    case Amd64InstrDecode::InstrForm::M1st_L_M32B_or_M16B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_L_M32B_or_M16B:
    case Amd64InstrDecode::InstrForm::MOp_L_M32B_or_M16B:
        opSize = L ? 32 : 16;
        break;
    // L_M32B_or_M8B
    case Amd64InstrDecode::InstrForm::MOp_L_M32B_or_M8B:
        opSize = L ? 32 : 8;
        break;
    // M16B
    case Amd64InstrDecode::InstrForm::M1st_M16B:
    case Amd64InstrDecode::InstrForm::M1st_M16B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M16B:
    case Amd64InstrDecode::InstrForm::MOp_M16B_I1B:
        opSize = 16;
        break;
    // L_M16B_or_M8B
    case Amd64InstrDecode::InstrForm::M1st_I1B_L_M16B_or_M8B:
    case Amd64InstrDecode::InstrForm::MOp_L_M16B_or_M8B:
        opSize = L ? 16 : 8;
        break;
    // W_M16B_or_M8B
    case Amd64InstrDecode::InstrForm::MOnly_W_M16B_or_M8B:
        opSize = W ? 16 : 8;
        break;
    // W_None_or_MOp_M16B
    case Amd64InstrDecode::InstrForm::I1B_W_None_or_MOp_M16B:
        opSize = W ? 0 : 16;
        break;
    // M10B
    case Amd64InstrDecode::InstrForm::MOnly_M10B:
        opSize = 10;
        break;
    // M8B
    case Amd64InstrDecode::InstrForm::MOp_M8B:
    case Amd64InstrDecode::InstrForm::MOp_M8B_I1B:
        opSize = 8;
        break;
    // L_M8B_or_M4B
    case Amd64InstrDecode::InstrForm::MOp_L_M8B_or_M4B:
        opSize = L ? 8 : 4;
        break;
    // W_M8B_or_M4B
    case Amd64InstrDecode::InstrForm::M1st_I1B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::M1st_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOnly_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOp_I4B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOp_W_M8B_or_M4B:
        opSize = W ? 8 : 4;
        break;
    // WP_M8B_or_M8B_or_M2B
    case Amd64InstrDecode::InstrForm::MOnly_WP_M8B_or_M8B_or_M2B:
        opSize = W ? 8 : P ? 8 : 2;
        break;
    // WP_M8B_or_M4B_or_M2B
    case Amd64InstrDecode::InstrForm::M1st_I1B_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B:
    case Amd64InstrDecode::InstrForm::M1st_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOnly_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOp_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B:
    case Amd64InstrDecode::InstrForm::MOp_WP_M8B_or_M4B_or_M2B:
        opSize = W ? 8 : P ? 4 : 2;
        break;
    // W_M8B_or_M2B
    case Amd64InstrDecode::InstrForm::M1st_W_M8B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOp_W_M8B_or_M2B:
        opSize = W ? 8 : 2;
        break;
    // M8B
    case Amd64InstrDecode::InstrForm::M1st_M8B:
    case Amd64InstrDecode::InstrForm::MOnly_M8B:
        opSize = 8;
        break;
    // M6B
    case Amd64InstrDecode::InstrForm::MOp_M6B:
        opSize = 6;
        break;
    // P_M6B_or_M4B
    case Amd64InstrDecode::InstrForm::MOnly_P_M6B_or_M4B:
        opSize = P ? 6 : 4;
        break;
    // M4B
    case Amd64InstrDecode::InstrForm::M1st_M4B:
    case Amd64InstrDecode::InstrForm::M1st_M4B_I1B:
    case Amd64InstrDecode::InstrForm::MOnly_M4B:
    case Amd64InstrDecode::InstrForm::MOp_M4B:
    case Amd64InstrDecode::InstrForm::MOp_M4B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M4B_I4B:
        opSize = 4;
        break;
    // L_M4B_or_M2B
    case Amd64InstrDecode::InstrForm::MOp_L_M4B_or_M2B:
        opSize = L ? 4 : 2;
        break;
    // W_M4B_or_M1B
    case Amd64InstrDecode::InstrForm::M1st_W_M4B_or_M1B:
    case Amd64InstrDecode::InstrForm::MOp_W_M4B_or_M1B:
        opSize = W ? 4 : 1;
        break;
    // M2B
    case Amd64InstrDecode::InstrForm::M1st_M2B:
    case Amd64InstrDecode::InstrForm::M1st_M2B_I1B:
    case Amd64InstrDecode::InstrForm::MOnly_M2B:
    case Amd64InstrDecode::InstrForm::MOp_M2B:
    case Amd64InstrDecode::InstrForm::MOp_M2B_I1B:
        opSize = 2;
        break;
    // M1B
    case Amd64InstrDecode::InstrForm::M1st_M1B:
    case Amd64InstrDecode::InstrForm::M1st_M1B_I1B:
    case Amd64InstrDecode::InstrForm::MOnly_M1B:
    case Amd64InstrDecode::InstrForm::MOp_M1B:
    case Amd64InstrDecode::InstrForm::MOp_M1B_I1B:
        opSize = 1;
        break;
    // MUnknown
    case Amd64InstrDecode::InstrForm::M1st_MUnknown:
    case Amd64InstrDecode::InstrForm::MOnly_MUnknown:
    case Amd64InstrDecode::InstrForm::MOp_MUnknown:
        // These are not expected/supported. Most/all are not for user code.
        _ASSERT(false);
        break;
    default:
        break;
    }
    return opSize;
}

static int immSize(Amd64InstrDecode::InstrForm form, int pp, bool W, bool L, bool fPrefix66)
{
    int immSize = 0;
    bool P = !((pp == 1) || fPrefix66);
    switch (form)
    {
    case Amd64InstrDecode::InstrForm::I1B:
    case Amd64InstrDecode::InstrForm::I1B_W_None_or_MOp_M16B:
    case Amd64InstrDecode::InstrForm::M1st_I1B_L_M16B_or_M8B:
    case Amd64InstrDecode::InstrForm::M1st_I1B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::M1st_I1B_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::M1st_M16B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M1B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M2B_I1B:
    case Amd64InstrDecode::InstrForm::M1st_M4B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_L_M32B_or_M16B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOp_I1B_WP_M8B_or_M4B_or_M2B:
    case Amd64InstrDecode::InstrForm::MOp_M16B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M1B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M2B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M32B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M4B_I1B:
    case Amd64InstrDecode::InstrForm::MOp_M8B_I1B:
        immSize = 1;
        break;
    case Amd64InstrDecode::InstrForm::I2B:
        immSize = 2;
        break;
    case Amd64InstrDecode::InstrForm::I3B:
        immSize = 3;
        break;
    case Amd64InstrDecode::InstrForm::I4B:
    case Amd64InstrDecode::InstrForm::MOp_I4B_W_M8B_or_M4B:
    case Amd64InstrDecode::InstrForm::MOp_M4B_I4B:
        immSize = 4;
        break;
    case Amd64InstrDecode::InstrForm::I8B:
        immSize = 8;
        break;
    case Amd64InstrDecode::InstrForm::M1st_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B:
    case Amd64InstrDecode::InstrForm::MOp_WP_M8B_I4B_or_M4B_I4B_or_M2B_I2B:
    case Amd64InstrDecode::InstrForm::WP_I4B_or_I4B_or_I2B:
        immSize = W ? 4 : P ? 4 : 2;
        break;
    case Amd64InstrDecode::InstrForm::WP_I8B_or_I4B_or_I2B:
        immSize = W ? 8 : P ? 4 : 2;
        break;
        break;
    default:
        break;
    }
    return immSize;
}

// static
void NativeWalker::DecodeInstructionForPatchSkip(const BYTE *address, InstructionAttribute * pInstrAttrib)
{
    LOG((LF_CORDB, LL_INFO10000, "Patch decode: "));

    if (pInstrAttrib == NULL)
    {
        return;
    }

    pInstrAttrib->Reset();

    // These three legacy prefixes are used to modify some of the two-byte opcodes.
    bool  fPrefix66 = false;
    bool  fPrefixF2 = false;
    bool  fPrefixF3 = false;

    bool  W     = false;
    bool  L     = false;

    int pp = 0;

    const BYTE* originalAddr = address;

    // Code below doesn't handle patched opcodes
    _ASSERT((*address != 0xcc) || ((BYTE)DebuggerController::GetPatchedOpcode(address) == 0xcc));

    //
    // Skip instruction prefixes
    //
    do
    {
        bool done = false;
        switch (*address)
        {
        // Operand-Size override
        case 0x66:
            fPrefix66 = true;
            break;

        // Repeat (REP/REPE/REPZ)
        case 0xf2:
            fPrefixF2 = true;
            break;

        // Repeat (REPNE/REPNZ)
        case 0xf3:
            fPrefixF3 = true;
            break;

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
            break;

        // REX register extension prefixes w/o W
        case 0x40:
        case 0x41:
        case 0x42:
        case 0x43:
        case 0x44:
        case 0x45:
        case 0x46:
        case 0x47:
            break;

        // REX register extension prefixes with W
        case 0x48:
        case 0x49:
        case 0x4a:
        case 0x4b:
        case 0x4c:
        case 0x4d:
        case 0x4e:
        case 0x4f:
            W = true;
            break;

        default:
            done = true;
            break;
        }

        if (done)
            break;
        LOG((LF_CORDB, LL_INFO10000, "prefix:%0.2x ", *address));
        address++;
    } while (true);

    // See "AMD64 Architecture Programmer's Manual Volume 3 Rev 3.26 Figure 1-1 Instruction encoding syntax"
    enum OpcodeMap
    {
        Primary = 0x0,
        Secondary = 0xF,
        Escape0F_0F = 0x0F0F, // 3D Now
        Escape0F_38 = 0x0F38,
        Escape0F_3A = 0x0F3A,
        VexMap1 = 0xc401,
        VexMap2 = 0xc402,
        VexMap3 = 0xc403,
        XopMap8 = 0x8f08,
        XopMap9 = 0x8f09,
        XopMapA = 0x8f0A
    } opCodeMap;

    switch (*address)
    {
        case 0xf:
           switch (address[1])
           {
           case 0xF:
               opCodeMap = Escape0F_0F;
               address += 2;
               break;
            case 0x38:
               opCodeMap = Escape0F_38;
               address += 2;
               break;
            case 0x3A:
               opCodeMap = Escape0F_3A;
               address += 2;
               break;
            default:
                opCodeMap = Secondary;
                address += 1;
            }
            if (fPrefix66)
                pp = 0x1;

            if (fPrefixF2)
                pp = 0x3;
            else if (fPrefixF3)
                pp = 0x2;
            break;

        case 0x8f: // XOP
            if ((address[1] & 0x38) == 0)
            {
                opCodeMap = Primary;
                break;
            }
            FALLTHROUGH;
        case 0xc4: // Vex 3-byte
            opCodeMap = (OpcodeMap)(int(address[0]) << 8 | (address[1] & 0x1f));
            // W is the top bit of opcode2.
            if ((address[2] & 0x80) != 0)
            {
                W = true;
            }
            if ((address[2] & 0x04) != 0)
            {
                L = true;
            }

            pp = address[2] & 0x3;
            address += 3;
            break;
        case 0xc5: // Vex 2-byte
            opCodeMap = VexMap1;
            W = true;
            if ((address[1] & 0x04) != 0)
            {
                L = true;
            }

            pp = address[1] & 0x3;
            address += 2;
            break;
        default:
            opCodeMap = Primary;
    }

    Amd64InstrDecode::InstrForm form = Amd64InstrDecode::InstrForm::None;
    switch (opCodeMap)
    {
    case Primary:
        form = Amd64InstrDecode::instrFormPrimary[*address];
        break;
    case Secondary:
        form = Amd64InstrDecode::instrFormSecondary[(size_t(*address) << 2)| pp];
        break;
    case Escape0F_0F: // 3DNow
        form = Amd64InstrDecode::InstrForm::MOp_M8B_I1B;
        break;
    case Escape0F_38:
        form = Amd64InstrDecode::instrFormF38[(size_t(*address) << 2)| pp];
        break;
    case Escape0F_3A:
        form = Amd64InstrDecode::instrFormF3A[(size_t(*address) << 2)| pp];
        break;
    case VexMap1:
        form = Amd64InstrDecode::instrFormVex1[(size_t(*address) << 2)| pp];
        break;
    case VexMap2:
        form = Amd64InstrDecode::instrFormVex2[(size_t(*address) << 2)| pp];
        break;
    case VexMap3:
        form = Amd64InstrDecode::instrFormVex3[(size_t(*address) << 2)| pp];
        break;
    case XopMap8:
        form = Amd64InstrDecode::instrFormXOP8[(size_t(*address) << 2)| pp];
        break;
    case XopMap9:
        form = Amd64InstrDecode::instrFormXOP9[(size_t(*address) << 2)| pp];
        break;
    case XopMapA:
        form = Amd64InstrDecode::instrFormXOPA[(size_t(*address) << 2)| pp];
        break;
    default:
        _ASSERTE(false);
    }

    bool fModRM = IsModRm(form, pp, W, L, fPrefix66);
    ModRMByte modrm = ModRMByte(address[1]);

    if (fModRM && (modrm.mod == 0x0) && (modrm.rm == 0x5))
    {
        // RIP-relative addressing.
        if (form & Amd64InstrDecode::InstrForm::Extension)
        {
            form = Amd64InstrDecode::instrFormExtension[(size_t(form ^ Amd64InstrDecode::InstrForm::Extension) << 3) | modrm.reg];
        }

        pInstrAttrib->m_dwOffsetToDisp = (DWORD)(address - originalAddr) + 1 /* op */ + 1 /* modrm */;
        _ASSERTE(pInstrAttrib->m_dwOffsetToDisp <= MAX_INSTRUCTION_LENGTH);

        const int dispBytes = 4;
        const int immBytes = immSize(form, pp, W, L, fPrefix66);

        pInstrAttrib->m_cbInstr = pInstrAttrib->m_dwOffsetToDisp + dispBytes + immBytes;
        _ASSERTE(pInstrAttrib->m_cbInstr <= MAX_INSTRUCTION_LENGTH);

        pInstrAttrib->m_fIsWrite = IsWrite(form, pp, W, L, fPrefix66);
        pInstrAttrib->m_cOperandSize = opSize(form, pp, W, L, fPrefix66);
    }

    if (opCodeMap == Primary)
    {
        BYTE opcode0 = *address;
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
                        FALLTHROUGH;
                    case 4:
                    case 5:
                        pInstrAttrib->m_fIsAbsBranch = true;
                }
                LOG((LF_CORDB, LL_INFO10000, "CALL/JMP modr/m:%0.2x\n", opcode0));
                break;

            default:
                LOG((LF_CORDB, LL_INFO10000, "NORMAL:%0.2x\n", opcode0));
        }
    }
}


#endif
