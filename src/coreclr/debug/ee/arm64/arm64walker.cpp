// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: Arm64walker.cpp
//

//
// ARM64 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"
#include "walker.h"
#include "frames.h"
#include "openum.h"

#ifdef TARGET_ARM64

PCODE Expand19bitoffset(PCODE opcode)
{
    opcode = opcode >> 5;
    PCODE  offset = (opcode & 0x7FFFF) << 2; //imm19:00 -> 21 bits

    //Sign Extension
    if ((offset & 0x100000)) //Check for 21'st bit
    {
        offset = offset | 0xFFFFFFFFFFE00000;
    }
    return offset;
}

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

    LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decode instruction at %p, opcode: %x\n", m_ip,opcode));



    if (NativeWalker::DecodeCallInst(opcode, RegNum, m_type)) //Unconditional Branch (register) instructions
    {
        if (m_type == WALK_RETURN)
        {
            m_skipIP = NULL;
        }
        m_nextIP = (BYTE*)GetReg(context, RegNum);
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


//When control reaches here m_pSharedPatchBypassBuffer has the original instructions in m_pSharedPatchBypassBuffer->PatchBypass
BYTE*  NativeWalker::SetupOrSimulateInstructionForPatchSkip(T_CONTEXT * context, SharedPatchBypassBuffer* m_pSharedPatchBypassBuffer,  const BYTE *address, PRD_TYPE opcode)
{

    BYTE* patchBypass = m_pSharedPatchBypassBuffer->PatchBypass;
    PCODE offset = 0;
    PCODE ip = 0;
    WALK_TYPE walk = WALK_UNKNOWN;
    int RegNum =-1;


    /*
    Based off ARM DDI 0487F.c (C.4.1)
    Modify the patchBypass if the opcode is IP-relative, otherwise return it
    The following are the instructions that are IP-relative  :
    . ADR and ADRP.
    . The Load register (literal) instruction class.
    . Direct branches that use an immediate offset.
    . The unconditional branch with link instructions, BL and BLR, that use the PC to create the return link
      address.
    */

    _ASSERTE((UINT_PTR)address == context->Pc);

    if ((opcode & 0x1F000000) == 0x10000000)  //ADR & ADRP (PC-Relative)
    {
        TADDR immhigh = ((opcode >> 5) & 0x007FFFF) << 2;
        TADDR immlow  = (opcode & 0x60000000) >> 29;
        offset = immhigh | immlow; //ADR
        RegNum = (opcode & 0x1F);

        //Sign Extension
        if ((offset & 0x100000))  //Check for 21'st bit
        {
            offset = offset | 0xFFFFFFFFFFE00000;
        }

        if ((opcode & 0x80000000) != 0) //ADRP
        {
            offset = offset << 12;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x to ADRP X%d %p\n", opcode, RegNum, offset));
        }
        else
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x to ADR X%d %p\n", opcode, RegNum, offset));
        }
    }

    else if ((opcode & 0x3B000000) == 0x18000000) //LDR Literal (General or SIMD)
    {
        offset = Expand19bitoffset(opcode);
        RegNum = (opcode & 0x1F);
        LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x to LDR[SW] | PRFM X%d %p\n", opcode, RegNum, offset));
    }
    else if (NativeWalker::DecodePCRelativeBranchInst(context,opcode, offset, walk))
    {
        _ASSERTE(RegNum == -1);
    }
    else if (NativeWalker::DecodeCallInst(opcode, RegNum, walk))
    {
        _ASSERTE(offset == 0);
    }
    //else  Just execute the opcodes as is
    //{
    //}

    if (offset != 0) // calculate the next ip from current ip
    {
        ip = (PCODE)address + offset;
    }
    else if(RegNum >= 0)
    {
        ip = GetReg(context, RegNum);
    }

    //Do instruction emulation inplace here

    if (walk == WALK_BRANCH || walk == WALK_CALL || walk == WALK_RETURN)
    {
        CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)patchBypass, 0xd503201f); //Add Nop in buffer

#if defined(HOST_OSX) && defined(HOST_ARM64)
        ExecutableWriterHolder<UINT_PTR> ripTargetFixupWriterHolder(&m_pSharedPatchBypassBuffer->RipTargetFixup, sizeof(UINT_PTR));
        UINT_PTR *pRipTargetFixupRW = ripTargetFixupWriterHolder.GetRW();
#else // HOST_OSX && HOST_ARM64
        UINT_PTR *pRipTargetFixupRW = &m_pSharedPatchBypassBuffer->RipTargetFixup;
#endif // HOST_OSX && HOST_ARM64

        *pRipTargetFixupRW = ip; //Control Flow simulation alone is done DebuggerPatchSkip::TriggerExceptionHook
        LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x  is a Control Flow instr \n", opcode));

        if (walk == WALK_CALL) //initialize Lr
        {
            SetLR(context, (PCODE)address + MAX_INSTRUCTION_LENGTH);
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x  is a Call instr, setting LR to %p \n", opcode,GetLR(context)));
        }
    }
    else if(RegNum >= 0)
    {
        CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)patchBypass, 0xd503201f); //Add Nop in buffer

        if ((opcode & 0x3B000000) == 0x18000000) //LDR Literal
        {
            bool isSimd = ((opcode & 0x4000000) != 0); //LDR literal for SIMD
            NEON128 SimdRegContents = { 0 };
            short opc = (opcode >> 30);

            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x (opc: %x, isSimd: %x)\n", opcode, opc, isSimd));

            switch (opc)
            {
            case 0: //load 4 bytes
                SimdRegContents.Low = GetMem(ip, 4, /* signExtend */ false);
                SimdRegContents.High = 0;
                if (isSimd) //LDR St [imm]
                    SetSimdReg(context, RegNum, SimdRegContents);
                else // LDR Wt [imm]
                    SetReg(context, RegNum, SimdRegContents.Low);

                break;
            case 1: //load 8 bytes
                SimdRegContents.Low = GetMem(ip, 8, /* signExtend */ false);
                SimdRegContents.High = 0;
                if (isSimd) //LDR Dt [imm]
                    SetSimdReg(context, RegNum, SimdRegContents);
                else // LDR Xt [imm]
                    SetReg(context, RegNum, SimdRegContents.Low);
                break;
            case 2: //SIMD 16 byte data
                if (isSimd) //LDR Qt [imm]
                {
                    SimdRegContents = GetSimdMem(ip);
                    SetSimdReg(context, RegNum, SimdRegContents);
                }
                else //LDR St [imm] (sign extendeded)
                {
                    SimdRegContents.Low = GetMem(ip, 4, /* signExtend */ true);
                    SetReg(context, RegNum, SimdRegContents.Low);
                }
                break;
            case 3:
                LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate opcode: %x  as PRFM ,but do nothing \n", opcode));
                break;
            default:
                LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate Unknown opcode: %x [LDR(litera,SIMD &FP)]  \n", opcode));
                _ASSERTE(!("Arm64Walker::Simulated Unknown opcode"));
            }
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate loadedMemory [Hi: %ull, lo: %ull]\n", SimdRegContents.High, SimdRegContents.Low));

        }
    }
    //else  Just execute the opcodes as IS
    //{
    //}

    return patchBypass;
}

//Decodes PC Relative Branch Instructions
//This code  is shared between the NativeWalker and DebuggerPatchSkip.
//So ENSURE THIS FUNCTION DOES NOT CHANGE ANY STATE OF THE DEBUGEE
//This Function Decodes :
// BL     offset
// B      offset
// B.Cond offset
// CB[N]Z X<r> offset
// TB[N]Z X<r> offset

//Output of the Function are:
//offset - Offset from current PC to which control will go next
//WALK_TYPE

BOOL  NativeWalker::DecodePCRelativeBranchInst(PT_CONTEXT context, const PRD_TYPE& opcode, PCODE& offset, WALK_TYPE& walk)
{
#ifdef _DEBUG
    PCODE incomingoffset = offset;
    WALK_TYPE incomingwalk = walk;
#endif

    if ((opcode & 0x7C000000) == 0x14000000) // Decode B & BL
    {
        offset = (opcode & 0x03FFFFFF) << 2;
        // Sign extension
        if ((offset & 0x4000000)) //Check for 26'st bit
        {
            offset = offset | 0xFFFFFFFFF8000000;
        }

        if ((opcode & 0x80000000) != 0) //BL
        {
            walk = WALK_CALL;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to BL %p \n", opcode, offset));
        }
        else
        {
            walk = WALK_BRANCH; //B
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to B %p \n", opcode, offset));
        }
        return TRUE;
    }

    //Conditional Branches
    _ASSERTE(context != NULL);


    if ((opcode & 0xFF000010) == 0x54000000) // B.cond
    {
        WORD cond = opcode & 0xF;
        bool result = false;
        switch (cond >> 1)
        {
        case 0x0:  result = (context->Cpsr & NZCV_Z) != 0; // EQ or NE
            break;
        case 0x1:  result = (context->Cpsr & NZCV_C) != 0; // CS or CC
            break;
        case 0x2:  result = (context->Cpsr & NZCV_N) != 0; // MI or PL
            break;
        case 0x3:  result = (context->Cpsr & NZCV_V) != 0; // VS or VC
            break;
        case 0x4:  result = ((context->Cpsr & NZCV_C) != 0) && ((context->Cpsr & NZCV_Z) == 0); // HI or LS
            break;
        case 0x5:  result = ((context->Cpsr & NZCV_N) >> NZCV_N_BIT) == ((context->Cpsr & NZCV_V) >> NZCV_V_BIT); // GE or LT
            break;
        case 0x6:  result = ((context->Cpsr & NZCV_N) >> NZCV_N_BIT) == ((context->Cpsr & NZCV_V) >> NZCV_V_BIT) && ((context->Cpsr & NZCV_Z) == 0); // GT or LE
            break;
        case 0x7:  result = true; // AL
            break;
        }

        if ((cond & 0x1) && (cond & 0xF) != 0) { result = !result; }

        if (result)
        {
            walk = WALK_BRANCH;
            offset = Expand19bitoffset(opcode);
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to B.cond %p \n", opcode, offset));
        }
        else // NOP
        {
            walk = WALK_UNKNOWN;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to B.cond but evaluated as NOP \n", opcode));
            offset = MAX_INSTRUCTION_LENGTH;
        }

        return TRUE;

    }


    int RegNum       = opcode & 0x1F;
    PCODE RegContent = GetReg(context, RegNum);

    if ((opcode & 0xFE000000) == 0x34000000) // CBNZ || CBZ
    {
        bool result = false;

        if (!(opcode & 0x80000000)) //if sf == '1' the 64 else 32
        {
            RegContent = 0xFFFFFFFF & RegContent;  //zero the upper 32bit
        }

        if (opcode & 0x01000000) //CBNZ
        {
            result = RegContent != 0;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to CBNZ X%d \n", opcode, RegNum));
        }
        else //CBZ
        {
            result = RegContent == 0;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to CBZ X%d \n", opcode, RegNum));
        }

        if (result)
        {
            walk = WALK_BRANCH;
            offset = Expand19bitoffset(opcode);
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to CB[N]Z X%d %p \n", opcode, RegNum, offset));
        }
        else // NOP
        {
            walk = WALK_UNKNOWN;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to B.cond but evaluated as NOP \n", opcode));
            offset = MAX_INSTRUCTION_LENGTH;
        }


        return TRUE;
    }
    if ((opcode & 0x7E000000) == 0x36000000)    // TBNZ || TBZ
    {
        bool result = false;
        int bit_pos = ((opcode >> 19) & 0x1F);

        if (opcode & 0x80000000)
        {
            bit_pos = bit_pos + 32;
        }

        PCODE bit_val = PCODE{ 1 } << bit_pos;
        if (opcode & 0x01000000) //TBNZ
        {
            result = (RegContent & bit_val) != 0;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to TBNZ X%d \n", opcode, RegNum));
        }
        else //TBZ
        {
            result = (RegContent & bit_val) == 0;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to CB[N]Z X%d \n", opcode, RegNum));
        }
        if (result)
        {
            walk = WALK_BRANCH;
            offset = ((opcode >> 5) & 0x3FFF) << 2; //imm14:00 -> 16 bits
            if (offset & 0x8000) //sign extension check for 16'th bit
            {
                offset = offset | 0xFFFFFFFFFFFF0000;
            }
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to TB[N]Z X%d %p \n", opcode, RegNum, offset));
        }
        else // NOP
        {
            walk = WALK_UNKNOWN;
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to B.cond but evaluated as NOP \n", opcode));
            offset = MAX_INSTRUCTION_LENGTH;
        }

        return TRUE;
    }

    _ASSERTE(offset == incomingoffset);
    _ASSERTE(walk == incomingwalk);
    return FALSE;
}

BOOL  NativeWalker::DecodeCallInst(const PRD_TYPE& opcode, int& RegNum, WALK_TYPE& walk)
{
    if ((opcode & 0xFF9FFC1F) == 0xD61F0000) // BR, BLR or RET -Unconditional Branch (register) instructions
    {

        RegNum = (opcode & 0x3E0) >> 5;


        short op = (opcode & 0x00600000) >> 21;  //Checking for 23 and 22 bits
        switch (op)
        {
        case 0: LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to BR X%d\n", opcode, RegNum));
            walk = WALK_BRANCH;
            break;
        case 1: LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to BLR X%d\n", opcode, RegNum));
            walk = WALK_CALL;
            break;
        case 2: LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Decoded opcode: %x to Ret X%d\n", opcode, RegNum));
            walk = WALK_RETURN;
            break;
        default:
            LOG((LF_CORDB, LL_INFO100000, "Arm64Walker::Simulate Unknown opcode: %x [Branch]  \n", opcode));
            _ASSERTE(!("Arm64Walker::Decoded Unknown opcode"));
        }

        return TRUE;
    }
        return FALSE;
}
#endif
