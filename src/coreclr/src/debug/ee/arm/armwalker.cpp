// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: armwalker.cpp
//

//
// ARM instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"

#include "walker.h"

#include "frames.h"
#include "openum.h"


#ifdef TARGET_ARM

void NativeWalker::Decode()
{
    // Set default next and skip instruction pointers.
    m_nextIP = NULL;
    m_skipIP = NULL;
    m_type = WALK_UNKNOWN;

    // We can't walk reliably without registers (because we need to know the IT state to determine whether or
    // not the current instruction will be executed).
    if (m_registers == NULL)
        return;

    // Determine whether we're executing in an IT block. If so, check the condition codes and IT state to see
    // whether we'll execute the current instruction.
    BYTE bITState = (BYTE)((BitExtract((WORD)m_registers->pCurrentContext->Cpsr, 15, 10) << 2) |
                           BitExtract((WORD)(m_registers->pCurrentContext->Cpsr >> 16), 10, 9));
    if ((bITState & 0x1f) && !ConditionHolds(BitExtract(bITState, 7, 4)))
    {
        // We're in an IT block and the state is such that the current instruction is not scheduled to
        // execute. Just return WALK_UNKNOWN so the caller will invoke single-step to update the register
        // context correctly for the next instruction.

        LOG((LF_CORDB, LL_INFO100000, "ArmWalker::Decode: IT block at %x\n", m_ip));
        return;
    }

    // Fetch first word of the current instruction. From this we can determine if we've gotten the whole thing
    // or we're dealing with a 32-bit instruction.  If the current instruction is a break instruction, we'll
    // need to check the patch table to get the correct instruction.
    WORD opcode1 = CORDbgGetInstruction(m_ip);
    PRD_TYPE unpatchedOpcode;
    if (DebuggerController::CheckGetPatchedOpcode(m_ip, &unpatchedOpcode))
    {
        opcode1 = (WORD) unpatchedOpcode;
    }


    if (Is32BitInstruction(opcode1))
    {
        // Fetch second word of 32-bit instruction.
        WORD opcode2 = CORDbgGetInstruction((BYTE*)((DWORD)m_ip) + 2);

        LOG((LF_CORDB, LL_INFO100000, "ArmWalker::Decode 32bit instruction at %x, opcode: %x%x\n", m_ip, (DWORD)opcode1, (DWORD)opcode2));

        // WALK_RETURN
        if (((opcode1 & 0xffd0) == 0xe890) &&
            ((opcode2 & 0x2000) == 0x0000))
        {
            // LDM.W : T2, POP.W : T2
            DWORD registerList = opcode2;

            if (registerList & 0x8000)
            {
                m_type = WALK_RETURN;
                return;
            }
        }

        // WALK_BRANCH
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0x8000) &&
                 ((opcode1 & 0x0380) != 0x0380))
        {
            // B.W : T3
            DWORD S = BitExtract(opcode1, 10, 10);
            DWORD cond = BitExtract(opcode1, 9, 6);
            DWORD imm6 = BitExtract(opcode1, 5, 0);
            DWORD J1 = BitExtract(opcode2, 13, 13);
            DWORD J2 = BitExtract(opcode2, 11, 11);
            DWORD imm11 = BitExtract(opcode2, 10, 0);

            if (ConditionHolds(cond))
            {
                DWORD disp = (S ? 0xfff00000 : 0) | (J2 << 19) | (J1 << 18) | (imm6 << 12) | (imm11 << 1);
                m_nextIP = (BYTE*)((GetReg(15) + disp) | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0x9000))
        {
            // B.W : T4
            DWORD S = BitExtract(opcode1, 10, 10);
            DWORD imm10 = BitExtract(opcode1, 9, 0);
            DWORD J1 = BitExtract(opcode2, 13, 13);
            DWORD J2 = BitExtract(opcode2, 11, 11);
            DWORD imm11 = BitExtract(opcode2, 10, 0);

            DWORD I1 = (J1 ^ S) ^ 1;
            DWORD I2 = (J2 ^ S) ^ 1;

            DWORD disp = (S ? 0xff000000 : 0) | (I1 << 23) | (I2 << 22) | (imm10 << 12) | (imm11 << 1);

            m_nextIP = (BYTE*)((GetReg(15) + disp) | THUMB_CODE);
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
            return;
        }
        else if (((opcode1 & 0xfff0) == 0xf8d0) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR.W (immediate): T3
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD imm12 = BitExtract(opcode2, 11, 0);

            if (Rt == 15)
            {
                DWORD value = *(DWORD*)(GetReg(Rn) + imm12);

                m_nextIP = (BYTE*)(value | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if (((opcode1 & 0xfff0) == 0xf850) &&
                 ((opcode2 & 0x0800) == 0x0800) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR (immediate) : T4, POP : T3
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD P = BitExtract(opcode2, 10, 10);
            DWORD U = BitExtract(opcode2, 9, 9);
            DWORD imm8 = BitExtract(opcode2, 7, 0);

            if (Rt == 15)
            {
                DWORD offset_addr = U ? GetReg(Rn) + imm8 : GetReg(Rn) - imm8;
                DWORD addr = P ? offset_addr : GetReg(Rn);

                DWORD value = *(DWORD*)addr;

                m_nextIP = (BYTE*)(value | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if (((opcode1 & 0xff7f) == 0xf85f))
        {
            // LDR.W (literal) : T2
            DWORD U = BitExtract(opcode1, 7, 7);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD imm12 = BitExtract(opcode2, 11, 0);

            if (Rt == 15)
            {
                DWORD addr = GetReg(15) & ~3;
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value = *(DWORD*)addr;

                m_nextIP = (BYTE*)(value | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if (((opcode1 & 0xfff0) == 0xf850) &&
                 ((opcode2 & 0x0fc0) == 0x0000) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR.W : T2
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD imm2 = BitExtract(opcode2, 5, 4);
            DWORD Rm = BitExtract(opcode2, 3, 0);

            if (Rt == 15)
            {
                DWORD addr = GetReg(Rn) + (GetReg(Rm) << imm2);

                DWORD value = *(DWORD*)addr;

                m_nextIP = (BYTE*)(value | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if (((opcode1 & 0xfff0) == 0xe8d0) &&
                 ((opcode2 & 0xffe0) == 0xf000))
        {
            // TBB/TBH : T1
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD H = BitExtract(opcode2, 4, 4);
            DWORD Rm = BitExtract(opcode2, 3, 0);

            DWORD addr = GetReg(Rn);

            DWORD value;
            if (H)
                value = *(WORD*)(addr + (GetReg(Rm) << 1));
            else
                value = *(BYTE*)(addr + GetReg(Rm));

            m_nextIP = (BYTE*)((GetReg(15) + (value << 1)) | THUMB_CODE);
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
            return;
        }

        // WALK_CALL
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0xd000))
        {
            // BL (immediate) : T1
            DWORD S = BitExtract(opcode1, 10, 10);
            DWORD imm10 = BitExtract(opcode1, 9, 0);
            DWORD J1 = BitExtract(opcode2, 13, 13);
            DWORD J2 = BitExtract(opcode2, 11, 11);
            DWORD imm11 = BitExtract(opcode2, 10, 0);

            DWORD I1 = (J1 ^ S) ^ 1;
            DWORD I2 = (J2 ^ S) ^ 1;

            DWORD disp = (S ? 0xff000000 : 0) | (I1 << 23) | (I2 << 22) | (imm10 << 12) | (imm11 << 1);

            m_nextIP = (BYTE*)((GetReg(15) + disp) | THUMB_CODE);
            m_skipIP =(BYTE*)(((DWORD)m_ip) + 4);
            m_type = WALK_CALL;
            return;
        }
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "ArmWalker::Decode 16bit instruction at %x, opcode: %x\n", m_ip, (DWORD)opcode1));
        // WALK_RETURN
        if ((opcode1 & 0xfe00) == 0xbc00)
        {
            // POP : T1
            DWORD P = BitExtract(opcode1, 8, 8);
            DWORD registerList = (P << 15) | BitExtract(opcode1, 7, 0);

            if (registerList & 0x8000)
            {
                m_type = WALK_RETURN;
                return;
            }
        }

        // WALK_BRANCH
        else if (((opcode1 & 0xf000) == 0xd000) &&
            ((opcode1 & 0x0f00) != 0x0e00) )
        {
            // B : T1
            DWORD cond = BitExtract(opcode1, 11, 8);
            DWORD imm8 = BitExtract(opcode1, 7, 0);

            if (ConditionHolds(cond))
            {
                DWORD disp = (imm8 << 1) | ((imm8 & 0x80) ? 0xffffff00 : 0);

                m_nextIP = (BYTE*)((GetReg(15) + disp) | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }
        else if ((opcode1 & 0xf800) == 0xe000)
        {
            // B : T2
            DWORD imm11 = BitExtract(opcode1, 10, 0);

            DWORD disp = (imm11 << 1) | ((imm11 & 0x400) ? 0xfffff000 : 0);

            m_nextIP = (BYTE*)((GetReg(15) + disp) | THUMB_CODE);
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
            return;
        }
        else if ((opcode1 & 0xff87) == 0x4700)
        {
            // BX : T1
            DWORD Rm = BitExtract(opcode1, 6, 3);

            m_nextIP = (BYTE*)(GetReg(Rm) | THUMB_CODE);
            m_skipIP = m_nextIP;
            m_type = (Rm != 14) ? WALK_BRANCH : WALK_RETURN;
            return;
        }
        else if ((opcode1 & 0xff00) == 0x4600)
        {
            // MOV (register) : T1
            DWORD D = BitExtract(opcode1, 7, 7);
            DWORD Rm = BitExtract(opcode1, 6, 3);
            DWORD Rd = (D << 3) | BitExtract(opcode1, 2, 0);

            if (Rd == 15)
            {
                m_nextIP = (BYTE*)(GetReg(Rm) | THUMB_CODE);
                m_skipIP = m_nextIP;
                m_type = WALK_BRANCH;
                return;
            }
        }

        // WALK_CALL
        else if ((opcode1 & 0xff87) == 0x4780)
        {
            // BLX (register) : T1
            DWORD Rm = BitExtract(opcode1, 6, 3);

            m_nextIP = (BYTE*)(GetReg(Rm) | THUMB_CODE);
            m_skipIP = (BYTE*)(((DWORD)m_ip) + 2);
            m_type = WALK_CALL;
            return;
        }
    }
}

// Get the current value of a register. PC (register 15) is always reported as the current instruction PC + 4
// as per the ARM architecture.
DWORD NativeWalker::GetReg(DWORD reg)
{
    _ASSERTE(reg <= 15);

    if (reg == 15)
        return (m_registers->pCurrentContext->Pc + 4) & ~THUMB_CODE;

    return (&m_registers->pCurrentContext->R0)[reg];
}

// Returns true if the current context indicates the ARM condition specified holds.
bool NativeWalker::ConditionHolds(DWORD cond)
{
    // Bit numbers of the condition flags in the CPSR.
    enum APSRBits
    {
        APSR_N = 31,
        APSR_Z = 30,
        APSR_C = 29,
        APSR_V = 28,
    };

// Return true if the given condition (C, N, Z or V) holds in the current context.
#define GET_FLAG(_flag)                         \
    ((m_registers->pCurrentContext->Cpsr & (1 << APSR_##_flag)) != 0)

    switch (cond)
    {
    case 0:                 // EQ (Z==1)
        return GET_FLAG(Z);
    case 1:                 // NE (Z==0)
        return !GET_FLAG(Z);
    case 2:                 // CS (C==1)
        return GET_FLAG(C);
    case 3:                 // CC (C==0)
        return !GET_FLAG(C);
    case 4:                 // MI (N==1)
        return GET_FLAG(N);
    case 5:                 // PL (N==0)
        return !GET_FLAG(N);
    case 6:                 // VS (V==1)
        return GET_FLAG(V);
    case 7:                 // VC (V==0)
        return !GET_FLAG(V);
    case 8:                 // HI (C==1 && Z==0)
        return GET_FLAG(C) && !GET_FLAG(Z);
    case 9:                 // LS (C==0 || Z==1)
        return !GET_FLAG(C) || GET_FLAG(Z);
    case 10:                // GE (N==V)
        return GET_FLAG(N) == GET_FLAG(V);
    case 11:                // LT (N!=V)
        return GET_FLAG(N) != GET_FLAG(V);
    case 12:                // GT (Z==0 && N==V)
        return !GET_FLAG(Z) && (GET_FLAG(N) == GET_FLAG(V));
    case 13:                // LE (Z==1 || N!=V)
        return GET_FLAG(Z) || (GET_FLAG(N) != GET_FLAG(V));
    case 14:                // AL
        return true;
    case 15:
        _ASSERTE(!"Unsupported condition code: 15");
        return false;
    default:
        UNREACHABLE();
        return false;
    }
}

#endif
