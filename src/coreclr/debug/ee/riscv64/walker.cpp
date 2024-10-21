// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
//
// RISCV64 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"
#include "walker.h"
#include "frames.h"
#include "openum.h"

#ifdef TARGET_RISCV64

inline uint64_t SignExtend(uint64_t value, unsigned int signbit)
{
    _ASSERTE(signbit < 64);

    if (signbit == 63)
      return value;

    uint64_t sign = value & (1ull << signbit);

    if (sign)
        return value | (~0ull << signbit);
    else
        return value;
}

inline uint64_t BitExtract(uint64_t value, unsigned int highbit, unsigned int lowbit, bool signExtend = false)
{
    _ASSERTE((highbit < 64) && (lowbit < 64) && (highbit >= lowbit));
    uint64_t extractedValue = (value >> lowbit) & ((1ull << ((highbit - lowbit) + 1)) - 1);

    return signExtend ? SignExtend(extractedValue, highbit - lowbit) : extractedValue;
}

uint64_t NativeWalker::GetReg(uint64_t reg)
{
    _ASSERTE(reg <= 31);
    _ASSERTE(m_registers->pCurrentContext->R0 == 0);

    return (&m_registers->pCurrentContext->R0)[reg];
}

void NativeWalker::Decode()
{
    // Reset so that we do not provide bogus info
    m_nextIP = NULL;
    m_skipIP = NULL;
    m_type = WALK_UNKNOWN;

    if (m_registers == NULL)
    {
       // Walker does not use WALK_NEXT
       // Without registers decoding will work only for handful of instructions
       return;
    }

    // Fetch first word of the current instruction. If the current instruction is a break instruction, we'll
    // need to check the patch table to get the correct instruction.
    PRD_TYPE opcode = CORDbgGetInstruction(m_ip);
    PRD_TYPE unpatchedOpcode;
    if (DebuggerController::CheckGetPatchedOpcode(m_ip, &unpatchedOpcode))
    {
        opcode = unpatchedOpcode;
    }

    LOG((LF_CORDB, LL_INFO100000, "RiscV64Walker::Decode instruction at %p, opcode: %x\n", m_ip, opcode));

    // TODO after "C" Standard Extension support implemented, add C.J, C.JAL, C.JR, C.JALR, C.BEQZ, C.BNEZ

    if ((opcode & 0x7f) == 0x6f) // JAL
    {
        // J-immediate encodes a signed offset in multiples of 2 bytes
        //      20       | 19                                               1 | 0
        // inst[31]/sign | inst[19:12] | inst[20] | inst[30:25] | inst[24:21] | 0
        uint64_t imm = SignExtend((BitExtract(opcode, 30, 21) << 1) | (BitExtract(opcode, 20, 20) << 11) |
                                  (BitExtract(opcode, 19, 12) << 12) | (BitExtract(opcode, 31, 31) << 20), 20);
        uint64_t Rd = BitExtract(opcode, 11, 7);

        m_nextIP = m_ip + imm;
        // The standard software calling convention uses X1 as the return address register and X5 as an alternate link register.
        if (Rd == 1 || Rd == 5)
        {
            m_skipIP = m_ip + 4;
            m_type = WALK_CALL;
        }
        else
        {
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
        }
    }
    else if ((opcode & 0x707f) == 0x67) // JALR
    {
        // I-immediate
        uint64_t imm = BitExtract(opcode, 31, 20, true);
        uint64_t Rs1 = BitExtract(opcode, 19, 15);
        uint64_t Rd = BitExtract(opcode, 11, 7);

        m_nextIP = (BYTE*)((GetReg(Rs1) + imm) & ~1ull);
        // The standard software calling convention uses X1 as the return address register and X5 as an alternate link register.
        if (Rd == 1 || Rd == 5)
        {
            m_skipIP = m_ip + 4;
            m_type = WALK_CALL;
        }
        else if (Rs1 == 1 || Rs1 == 5)
        {
            m_type = WALK_RETURN;
        }
        else
        {
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
        }
    }
    else if (((opcode & 0x707f) == 0x63) ||   // BEQ
             ((opcode & 0x707f) == 0x1063) || // BNE
             ((opcode & 0x707f) == 0x4063) || // BLT
             ((opcode & 0x707f) == 0x5063))   // BGE
    {
        uint64_t Rs1 = BitExtract(opcode, 19, 15);
        uint64_t Rs2 = BitExtract(opcode, 24, 20);
        int64_t Rs1SValue = GetReg(Rs1);
        int64_t Rs2SValue = GetReg(Rs2);

        if ((((opcode & 0x707f) == 0x63) && Rs1SValue == Rs2SValue) ||
            (((opcode & 0x707f) == 0x1063) && Rs1SValue != Rs2SValue) ||
            (((opcode & 0x707f) == 0x4063) && Rs1SValue < Rs2SValue) ||
            (((opcode & 0x707f) == 0x5063) && Rs1SValue >= Rs2SValue))
        {
            // B-immediate encodes a signed offset in multiples of 2 bytes
            //       12      | 11                               1 | 0
            // inst[31]/sign | inst[7] | inst[30:25] | inst[11:8] | 0
            uint64_t imm = SignExtend((BitExtract(opcode, 11, 8) << 1) | (BitExtract(opcode, 30, 25) << 5) |
                                      (BitExtract(opcode, 7, 7) << 11) | (BitExtract(opcode, 31, 31) << 12), 12);

            m_nextIP = m_ip + imm;
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
        }
    }
    else if (((opcode & 0x707f) == 0x6063) || // BLTU
             ((opcode & 0x707f) == 0x7063))   // BGEU
    {
        uint64_t Rs1 = BitExtract(opcode, 19, 15);
        uint64_t Rs2 = BitExtract(opcode, 24, 20);
        uint64_t Rs1Value = GetReg(Rs1);
        uint64_t Rs2Value = GetReg(Rs2);

        if ((((opcode & 0x707f) == 0x6063) && Rs1Value < Rs2Value) ||
            (((opcode & 0x707f) == 0x7063) && Rs1Value >= Rs2Value))
        {
            // B-immediate encodes a signed offset in multiples of 2 bytes
            //       12      | 11                               1 | 0
            // inst[31]/sign | inst[7] | inst[30:25] | inst[11:8] | 0
            uint64_t imm = SignExtend((BitExtract(opcode, 11, 8) << 1) | (BitExtract(opcode, 30, 25) << 5) |
                                      (BitExtract(opcode, 7, 7) << 11) | (BitExtract(opcode, 31, 31) << 12), 12);

            m_nextIP = m_ip + imm;
            m_skipIP = m_nextIP;
            m_type = WALK_BRANCH;
        }
    }
}

#endif
