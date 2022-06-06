// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: Loongarch64walker.cpp
//

//
// LOONGARCH64 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"
#include "walker.h"
#include "frames.h"
#include "openum.h"

#ifdef TARGET_LOONGARCH64

void NativeWalker::Decode()
{
    PT_CONTEXT context = NULL;
    int RegNum = -1;
    PCODE offset = MAX_INSTRUCTION_LENGTH;

    //Reset so that we do not provide bogus info
    m_type = WALK_UNKNOWN;
    m_skipIP = NULL;
    m_nextIP = NULL;

    if (m_registers == NULL)
    {
       //walker does not use WALK_NEXT
       //Without registers decoding will work only for handful of instructions
       return;
    }

    m_skipIP = m_ip + MAX_INSTRUCTION_LENGTH;

    context = m_registers->pCurrentContext;
    // Fetch first word of the current instruction.If the current instruction is a break instruction, we'll
    // need to check the patch table to get the correct instruction.
    PRD_TYPE opcode = CORDbgGetInstruction(m_ip);
    PRD_TYPE unpatchedOpcode;
    if (DebuggerController::CheckGetPatchedOpcode(m_ip, &unpatchedOpcode))
    {
        opcode =  unpatchedOpcode;
    }

    LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decode instruction at %p, opcode: %x\n", m_ip, opcode));

    if (NativeWalker::DecodeJumpInst(opcode, RegNum, offset, m_type)) //Unconditional jump (register) instructions
    {
        if (m_type == WALK_RETURN)
        {
            m_skipIP = NULL;
        }
        m_nextIP = (BYTE*)GetReg(context, RegNum) + offset;
        return;
     }

    if (NativeWalker::DecodePCRelativeBranchInst(context, opcode, offset, m_type))
    {
        if (m_type == WALK_BRANCH)
        {
            m_skipIP = NULL;
        }
    }

    m_nextIP = m_ip + offset;

    return;
}

// Decodes PC Relative Branch Instructions
//
// This Function Decodes :
//   BL     offset
//   B      offset
//
// Output of the Function are:
//   offset - Offset from current PC to which control will go next
//   WALK_TYPE
BOOL  NativeWalker::DecodePCRelativeBranchInst(PT_CONTEXT context, const PRD_TYPE opcode, PCODE& offset, WALK_TYPE& walk)
{
    if (((opcode >> 24) & 0xFC) == 0x50) // Decode B
    {
        offset = (opcode >> 10) & 0xFFFF; // Get the low-16bits offset field.
        offset |= (opcode & 0x3FF) << 16; // Get the hight-10bits offset field.

        // Check whether sign extension
        if ((opcode & 0x200) != 0)
        {
            offset = ((int)(offset << 6)) >> 4; // Also 4-bytes aligned
        }
        else
        {
            offset <<= 2; // 4-bytes aligned
        }

        walk = WALK_BRANCH; //B
        LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decoded opcode: %x to B %x \n", opcode, offset));

        return TRUE;
    }
    else if (((opcode >> 24) & 0xFC) == 0x54) // Decode BL
    {
        offset = (opcode >> 10) & 0xFFFF; // Get the low-16bits offset field.
        offset |= (opcode & 0x3FF) << 16; // Get the hight-10bits offset field.

        // Check whether sign extension
        if ((opcode & 0x200) != 0)
        {
            offset = ((int)(offset << 6)) >> 4; // Also 4-bytes aligned
        }
        else
        {
            offset <<= 2; // 4-bytes aligned
        }

        walk = WALK_CALL;
        LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decoded opcode: %x to BL %x \n", opcode, offset));

        return TRUE;
    }

    return FALSE;
}

BOOL  NativeWalker::DecodeJumpInst(const PRD_TYPE opcode, int& RegNum, PCODE& offset, WALK_TYPE& walk)
{
    if ((opcode & 0xFC000000) == 0x4C000000) // jirl - Unconditional Jump (register) instructions
    {
        RegNum = (opcode >> 5) & 0x1F; // rj is the target registor.

        short op = opcode & 0x1F; // Checking for linker registor by rd field.
        switch (op)
        {
        case 0:
            if (RegNum == 1)
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decoded opcode: %x to Ret X%d\n", opcode, RegNum));
                walk = WALK_RETURN;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decoded opcode: %x to jr Reg%d\n", opcode, RegNum));
                walk = WALK_BRANCH;
            }
            break;
        case 1: LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Decoded opcode: %x to jirl Reg%d\n", opcode, RegNum));
            walk = WALK_CALL;
            break;
        default:
            LOG((LF_CORDB, LL_INFO100000, "LoongArch64Walker::Simulate Unknown opcode: %x [Branch]  \n", opcode));
            _ASSERTE(!("LoongArch64Walker::Decoded Unknown opcode"));
        }

        offset = (short)(opcode >> 10); // get the offset fields and signExtend.
        offset <<= 2; // 4-bytes aligned.

        return TRUE;
    }

    return FALSE;
}
#endif
