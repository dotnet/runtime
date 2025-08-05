// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// NOTE on Frame Size C_ASSERT usage in this file
// if the frame size changes then the stubs have to be revisited for correctness
// kindly revist the logic and then update the constants so that the C_ASSERT will again fire
// if someone changes the frame size.  You are expected to keep this hard coded constant
// up to date so that changes in the frame size trigger errors at compile time if the code is not altered

// Precompiled Header

#include "common.h"

#include "field.h"
#include "stublink.h"

#include "frames.h"
#include "excep.h"
#include "dllimport.h"
#include "log.h"
#include "comdelegate.h"
#include "array.h"
#include "jitinterface.h"
#include "codeman.h"
#include "dbginterface.h"
#include "eeprofinterfaces.h"
#include "eeconfig.h"
#ifdef TARGET_X86
#include "asmconstants.h"
#endif // TARGET_X86
#include "class.h"
#include "stublink.inl"


#ifndef DACCESS_COMPILE

#ifdef TARGET_X86
//-----------------------------------------------------------------------
// InstructionFormat for near Jump and short Jump
//-----------------------------------------------------------------------
class X86NearJump : public InstructionFormat
{
    public:
        X86NearJump() : InstructionFormat(InstructionFormat::k8|InstructionFormat::k32)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT
            switch (refsize)
            {
                case k8:
                    return 2;

                case k32:
                    return 5;

                default:
                    _ASSERTE(!"unexpected refsize");
                    return 0;

            }
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT
            if (k8 == refsize)
            {
                pOutBufferRW[0] = 0xeb;
                *((int8_t*)(pOutBufferRW+1)) = (int8_t)fixedUpReference;
            }
            else if (k32 == refsize)
            {
                pOutBufferRW[0] = 0xe9;
                *((int32_t*)(pOutBufferRW+1)) = (int32_t)fixedUpReference;
            }
            else
            {
                _ASSERTE(!"unreached");
            }
        }

        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_GC_NOTRIGGER;
            STATIC_CONTRACT_FORBID_FAULT;


            if (fExternal)
            {
                switch (refsize)
                {
                case InstructionFormat::k8:
                    // For external, we don't have enough info to predict
                    // the offset.
                    return FALSE;

                case InstructionFormat::k32:
                    return sizeof(PVOID) <= sizeof(UINT32);

                case InstructionFormat::kAllowAlways:
                    return TRUE;

                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
            else
            {
                switch (refsize)
                {
                case InstructionFormat::k8:
                    return FitsInI1(offset);

                case InstructionFormat::k32:
                case InstructionFormat::kAllowAlways:
                    return TRUE;

                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
};

static BYTE gX86NearJump[sizeof(X86NearJump)];
#endif // TARGET_X86

/* static */ void StubLinkerCPU::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    new (gX86NearJump) X86NearJump();
#endif
}

//---------------------------------------------------------------
// Emits:
//    mov destReg, srcReg
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitMovRegReg(X86Reg destReg, X86Reg srcReg)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

    if (destReg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        destReg = X86RegFromAMD64Reg(destReg);
    }
    if (srcReg >= kR8)
    {
        rex |= REX_MODRM_REG_EXT;
        srcReg = X86RegFromAMD64Reg(srcReg);
    }
    Emit8(rex);
#endif

    Emit8(0x89);
    Emit8(static_cast<UINT8>(0xC0 | (srcReg << 3) | destReg));
}

#ifdef TARGET_X86
//---------------------------------------------------------------
// Emits:
//    PUSH <reg32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

    Emit8(static_cast<UINT8>(0x50 + reg));
    Push(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    POP <reg32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPopReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

    Emit8(static_cast<UINT8>(0x58 + reg));
    Pop(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    PUSH <imm32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushImm32(UINT32 value)
{
    STANDARD_VM_CONTRACT;

    Emit8(0x68);
    Emit32(value);
    Push(sizeof(void*));
}


//---------------------------------------------------------------
// Emits:
//    PUSH <ptr>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushImmPtr(LPVOID value BIT64_ARG(X86Reg tmpReg /*=kR10*/))
{
    STANDARD_VM_CONTRACT;

    X86EmitPushImm32((UINT_PTR) value);
}


//---------------------------------------------------------------
// Emits:
//    JMP <ofs8>   or
//    JMP <ofs32}
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitNearJump(CodeLabel *target)
{
    STANDARD_VM_CONTRACT;
    EmitLabelRef(target, reinterpret_cast<X86NearJump&>(gX86NearJump), 0);
}
#endif // TARGET_X86


//---------------------------------------------------------------
// Emits:
//    mov <dstreg>, [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexRegLoad(X86Reg dstreg,
                                        X86Reg srcreg,
                                        int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X86EmitOffsetModRM(0x8b, dstreg, srcreg, ofs);
}


//---------------------------------------------------------------
// Emits:
//    mov [<dstreg> + <ofs>],<srcreg>
//
// Note: If you intend to use this to perform 64bit moves to a RSP
//       based offset, then this method may not work. Consider
//       using X86EmitIndexRegStoreRSP.
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexRegStore(X86Reg dstreg,
                                         int32_t ofs,
                                         X86Reg srcreg)
{
    STANDARD_VM_CONTRACT;

    X86EmitOffsetModRM(0x89, srcreg, dstreg, ofs);
}

#ifdef TARGET_X86
//---------------------------------------------------------------
// Emits:
//    push dword ptr [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexPush(X86Reg srcreg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    X86EmitOffsetModRM(0xff, (X86Reg)0x6, srcreg, ofs);
    Push(sizeof(void*));
}


//---------------------------------------------------------------
// Emits:
//   add esp, IMM
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitAddEsp(INT32 imm32)
{
    STANDARD_VM_CONTRACT;

    if (!imm32)
    {
        // nop
    }
    else
    {
        X86_64BitOperands();

        if (FitsInI1(imm32))
        {
            Emit16(0xc483);
            Emit8((INT8)imm32);
        }
        else
        {
            Emit16(0xc481);
            Emit32(imm32);
        }
    }
    Pop(imm32);
}
#endif // TARGET_X86

VOID StubLinkerCPU::X86EmitAddReg(X86Reg reg, INT32 imm32)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION((int) reg < NumX86Regs);
    }
    CONTRACTL_END;

    if (imm32 == 0)
        return;

#ifdef TARGET_AMD64
    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

    if (reg >= kR8)
    {
        rex |= REX_OPCODE_REG_EXT;
        reg = X86RegFromAMD64Reg(reg);
    }
    Emit8(rex);
#endif

    if (FitsInI1(imm32)) {
        Emit8(0x83);
        Emit8(static_cast<UINT8>(0xC0 | reg));
        Emit8(static_cast<UINT8>(imm32));
    } else {
        Emit8(0x81);
        Emit8(static_cast<UINT8>(0xC0 | reg));
        Emit32(imm32);
    }
}


#if defined(TARGET_AMD64)

//---------------------------------------------------------------
// movaps destXmmreg, srcXmmReg
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovXmmXmm(X86Reg destXmmreg, X86Reg srcXmmReg)
{
    STANDARD_VM_CONTRACT;

    // There are several that could be used to mov xmm registers. MovAps is
    // what C++ compiler uses so let's use it here too.

    BYTE rex = 0;

    if (srcXmmReg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        srcXmmReg = X86RegFromAMD64Reg(srcXmmReg);
    }

    if (destXmmreg >= kR8)
    {
        rex |= REX_MODRM_REG_EXT;
        destXmmreg = X86RegFromAMD64Reg(destXmmreg);
    }

    if (rex)
        Emit8(REX_PREFIX_BASE | rex);

    Emit16(0x280F);
    Emit8(static_cast<UINT8>(0300 | (destXmmreg << 3) | srcXmmReg));    
}

//---------------------------------------------------------------
// movsd XmmN, [baseReg + offset]
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSDFromMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF2, 0x10, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movsd [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSDToMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF2, 0x11, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movss XmmN, [baseReg + offset]
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSSFromMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF3, 0x10, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movss [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSSToMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF3, 0x11, Xmmreg, baseReg, ofs);
}

VOID StubLinkerCPU::X64EmitMovqRegXmm(X86Reg reg, X86Reg Xmmreg)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovqWorker(0x7e, Xmmreg, reg);
}

VOID StubLinkerCPU::X64EmitMovqXmmReg(X86Reg Xmmreg, X86Reg reg)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovqWorker(0x6e, Xmmreg, reg);
}

//-----------------------------------------------------------------------------
// Helper method for emitting movq between xmm and general purpose reqister
//-----------------------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovqWorker(BYTE opcode, X86Reg Xmmreg, X86Reg reg)
{
    BYTE    codeBuffer[10];
    unsigned int     nBytes  = 0;
    codeBuffer[nBytes++] = 0x66;
    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
    if (reg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        reg = X86RegFromAMD64Reg(reg);
    }
    if (Xmmreg >= kXMM8)
    {
        rex |= REX_MODRM_REG_EXT;
        Xmmreg = X86RegFromAMD64Reg(Xmmreg);
    }
    codeBuffer[nBytes++] = rex;
    codeBuffer[nBytes++] = 0x0f;
    codeBuffer[nBytes++] = opcode;
    BYTE modrm = static_cast<BYTE>((Xmmreg << 3) | reg);
    codeBuffer[nBytes++] = 0xC0|modrm;

    _ASSERTE(nBytes <= ARRAY_SIZE(codeBuffer));

    // Lastly, emit the encoded bytes
    EmitBytes(codeBuffer, nBytes);
}

//---------------------------------------------------------------
// Helper method for emitting of XMM from/to memory moves
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovXmmWorker(BYTE prefix, BYTE opcode, X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[10];
    unsigned int     nBytes  = 0;

    // Setup the legacyPrefix for movsd
    codeBuffer[nBytes++] = prefix;

    // By default, assume we dont have to emit the REX byte.
    bool fEmitRex = false;

    BYTE rex = REX_PREFIX_BASE;

    if (baseReg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        baseReg = X86RegFromAMD64Reg(baseReg);
        fEmitRex = true;
    }
    if (Xmmreg >= kXMM8)
    {
        rex |= REX_MODRM_REG_EXT;
        Xmmreg = X86RegFromAMD64Reg(Xmmreg);
        fEmitRex = true;
    }

    if (fEmitRex == true)
    {
        codeBuffer[nBytes++] = rex;
    }

    // Next, specify the two byte opcode - first byte is always 0x0F.
    codeBuffer[nBytes++] = 0x0F;
    codeBuffer[nBytes++] = opcode;

    BYTE modrm = static_cast<BYTE>((Xmmreg << 3) | baseReg);
    bool fOffsetFitsInSignedByte = FitsInI1(ofs)?true:false;

    if (fOffsetFitsInSignedByte)
        codeBuffer[nBytes++] = 0x40|modrm;
    else
        codeBuffer[nBytes++] = 0x80|modrm;

    // If we are dealing with RSP or R12 as the baseReg, we need to emit the SIB byte.
    if ((baseReg == (X86Reg)4 /*kRSP*/) || (baseReg == kR12))
    {
        codeBuffer[nBytes++] = 0x24;
    }

    // Finally, specify the offset
    if (fOffsetFitsInSignedByte)
    {
        codeBuffer[nBytes++] = (BYTE)ofs;
    }
    else
    {
        *((int32_t*)(codeBuffer+nBytes)) = ofs;
        nBytes += 4;
    }

    _ASSERTE(nBytes <= ARRAY_SIZE(codeBuffer));

    // Lastly, emit the encoded bytes
    EmitBytes(codeBuffer, nBytes);
}

#endif // defined(TARGET_AMD64)

//---------------------------------------------------------------
// Emits a MOD/RM for accessing a dword at [<indexreg> + ofs32]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitOffsetModRM(BYTE opcode, X86Reg opcodereg, X86Reg indexreg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[7];
    BYTE*   code    = codeBuffer;
    int     nBytes  = 0;
#ifdef TARGET_AMD64
    code++;
    //
    // code points to base X86 instruction,
    // codeBuffer points to full AMD64 instruction
    //
    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

    if (indexreg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        indexreg = X86RegFromAMD64Reg(indexreg);
    }
    if (opcodereg >= kR8)
    {
        rex |= REX_MODRM_REG_EXT;
        opcodereg = X86RegFromAMD64Reg(opcodereg);
    }

    nBytes++;
    code[-1] = rex;
#endif
    code[0] = opcode;
    nBytes++;
    BYTE modrm = static_cast<BYTE>((opcodereg << 3) | indexreg);
    if (ofs == 0 && indexreg != kEBP)
    {
        code[1] = modrm;
        nBytes++;
        EmitBytes(codeBuffer, nBytes);
    }
    else if (FitsInI1(ofs))
    {
        code[1] = 0x40|modrm;
        code[2] = (BYTE)ofs;
        nBytes += 2;
        EmitBytes(codeBuffer, nBytes);
    }
    else
    {
        code[1] = 0x80|modrm;
        *((int32_t*)(2+code)) = ofs;
        nBytes += 5;
        EmitBytes(codeBuffer, nBytes);
    }
}


VOID StubLinkerCPU::X86EmitRegLoad(X86Reg reg, UINT_PTR imm)
{
    STANDARD_VM_CONTRACT;

    UINT cbimm = sizeof(void*);

#ifdef TARGET_AMD64
    // amd64 zero-extends all 32-bit operations.  If the immediate will fit in
    // 32 bits, use the smaller encoding.

    if (reg >= kR8 || !FitsInU4(imm))
    {
        BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
        if (reg >= kR8)
        {
            rex |= REX_MODRM_RM_EXT;
            reg = X86RegFromAMD64Reg(reg);
        }
        Emit8(rex);
    }
    else
    {
        // amd64 is little endian, so the &imm below will correctly read off
        // the low 4 bytes.
        cbimm = sizeof(UINT32);
    }
#endif // TARGET_AMD64
    Emit8(0xB8 | (BYTE)reg);
    EmitBytes((BYTE*)&imm, cbimm);
}


//---------------------------------------------------------------
// Emits the most efficient form of the operation:
//
//    opcode   altreg, [basereg + scaledreg*scale + ofs]
//
// or
//
//    opcode   [basereg + scaledreg*scale + ofs], altreg
//
// (the opcode determines which comes first.)
//
//
// Limitations:
//
//    scale must be 0,1,2,4 or 8.
//    if scale == 0, scaledreg is ignored.
//    basereg and altreg may be equal to 4 (ESP) but scaledreg cannot
//    for some opcodes, "altreg" may actually select an operation
//      rather than a second register argument.
//    if basereg is EBP, scale must be 0.
//
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitOp(WORD    opcode,
                              X86Reg  altreg,
                              X86Reg  basereg,
                              int32_t ofs /*=0*/,
                              X86Reg  scaledreg /*=0*/,
                              BYTE    scale /*=0*/
                    AMD64_ARG(X86OperandSize OperandSize /*= k32BitOp*/))
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        // All 2-byte opcodes start with 0x0f.
        PRECONDITION(!(opcode >> 8) || (opcode & 0xff) == 0x0f);

        PRECONDITION(scale == 0 || scale == 1 || scale == 2 || scale == 4 || scale == 8);
        PRECONDITION(scaledreg != (X86Reg)4);
        PRECONDITION(!(basereg == kEBP && scale != 0));

        PRECONDITION( ((UINT)basereg)   < NumX86Regs );
        PRECONDITION( ((UINT)scaledreg) < NumX86Regs );
        PRECONDITION( ((UINT)altreg)    < NumX86Regs );
    }
    CONTRACTL_END;

#ifdef TARGET_AMD64
    if (   k64BitOp == OperandSize
        || altreg    >= kR8
        || basereg   >= kR8
        || scaledreg >= kR8)
    {
        BYTE rex = REX_PREFIX_BASE;

        if (k64BitOp == OperandSize)
            rex |= REX_OPERAND_SIZE_64BIT;

        if (altreg >= kR8)
        {
            rex |= REX_MODRM_REG_EXT;
            altreg = X86RegFromAMD64Reg(altreg);
        }

        if (basereg >= kR8)
        {
            // basereg might be in the modrm or sib fields.  This will be
            // decided below, but the encodings are the same either way.
            _ASSERTE(REX_SIB_BASE_EXT == REX_MODRM_RM_EXT);
            rex |= REX_SIB_BASE_EXT;
            basereg = X86RegFromAMD64Reg(basereg);
        }

        if (scaledreg >= kR8)
        {
            rex |= REX_SIB_INDEX_EXT;
            scaledreg = X86RegFromAMD64Reg(scaledreg);
        }

        Emit8(rex);
    }
#endif // TARGET_AMD64

    BYTE modrmbyte = static_cast<BYTE>(altreg << 3);
    BOOL fNeedSIB  = FALSE;
    BYTE SIBbyte = 0;
    BYTE ofssize;
    BYTE scaleselect= 0;

    if (ofs == 0 && basereg != kEBP)
    {
        ofssize = 0; // Don't change this constant!
    }
    else if (FitsInI1(ofs))
    {
        ofssize = 1; // Don't change this constant!
    }
    else
    {
        ofssize = 2; // Don't change this constant!
    }

    switch (scale)
    {
        case 1: scaleselect = 0; break;
        case 2: scaleselect = 1; break;
        case 4: scaleselect = 2; break;
        case 8: scaleselect = 3; break;
    }

    if (scale == 0 && basereg != (X86Reg)4 /*ESP*/)
    {
        // [basereg + ofs]
        modrmbyte |= basereg | (ofssize << 6);
    }
    else if (scale == 0)
    {
        // [esp + ofs]
        _ASSERTE(basereg == (X86Reg)4);
        fNeedSIB = TRUE;
        SIBbyte  = 0044;

        modrmbyte |= 4 | (ofssize << 6);
    }
    else
    {

        //[basereg + scaledreg*scale + ofs]

        modrmbyte |= 0004 | (ofssize << 6);
        fNeedSIB = TRUE;
        SIBbyte = static_cast<BYTE>((scaleselect << 6) | (scaledreg << 3) | basereg);

    }

    //Some sanity checks:
    _ASSERTE(!(fNeedSIB && basereg == kEBP)); // EBP not valid as a SIB base register.
    _ASSERTE(!( (!fNeedSIB) && basereg == (X86Reg)4 )) ; // ESP addressing requires SIB byte

    Emit8((BYTE)opcode);

    if (opcode >> 8)
        Emit8(opcode >> 8);

    Emit8(modrmbyte);
    if (fNeedSIB)
    {
        Emit8(SIBbyte);
    }
    switch (ofssize)
    {
        case 0: break;
        case 1: Emit8( (int8_t)ofs ); break;
        case 2: Emit32( ofs ); break;
        default: _ASSERTE(!"Can't get here.");
    }
}


#ifdef TARGET_X86
//---------------------------------------------------------------
// Emits:
//   op altreg, [esp+ofs]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitEspOffset(BYTE opcode,
                                     X86Reg altreg,
                                     int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[7];
    BYTE   *code = codeBuffer;

    code[0] = opcode;
    BYTE modrm = static_cast<BYTE>((altreg << 3) | 004);
    if (ofs == 0)
    {
        code[1] = modrm;
        code[2] = 0044;
        EmitBytes(codeBuffer, 3);
    }
    else if (FitsInI1(ofs))
    {
        code[1] = 0x40|modrm;
        code[2] = 0044;
        code[3] = (BYTE)ofs;
        EmitBytes(codeBuffer, 4);
    }
    else
    {
        code[1] = 0x80|modrm;
        code[2] = 0044;
        *((int32_t*)(3+code)) = ofs;
        EmitBytes(codeBuffer, 7);
    }
}
#endif


// Get X86Reg indexes of argument registers based on offset into ArgumentRegister
X86Reg GetX86ArgumentRegisterFromOffset(size_t ofs)
{
    CONTRACT(X86Reg)
    {
        NOTHROW;
        GC_NOTRIGGER;

    }
    CONTRACT_END;

    #define ARGUMENT_REGISTER(reg) if (ofs == offsetof(ArgumentRegisters, reg)) RETURN  k##reg ;
    ENUM_ARGUMENT_REGISTERS();
    #undef ARGUMENT_REGISTER

    _ASSERTE(0);//Can't get here.
    RETURN kEBP;
}


#ifdef TARGET_AMD64
static const X86Reg c_argRegs[] = {
    #define ARGUMENT_REGISTER(regname) k##regname,
    ENUM_ARGUMENT_REGISTERS()
    #undef ARGUMENT_REGISTER
};
#endif

#ifdef TARGET_X86
// This method unboxes the THIS pointer and then calls pRealMD
// If it's shared code for a method in a generic value class, then also extract the vtable pointer
// and pass it as an extra argument.  Thus this stub generator really covers both
//   - Unboxing, non-instantiating stubs
//   - Unboxing, method-table-instantiating stubs
VOID StubLinkerCPU::EmitUnboxMethodStub(MethodDesc* pUnboxMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!pUnboxMD->IsStatic());
    }
    CONTRACTL_END;

#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
    _ASSERTE(!pUnboxMD->RequiresInstMethodTableArg());
#else
    if (pUnboxMD->RequiresInstMethodTableArg())
    {
        EmitInstantiatingMethodStub(pUnboxMD, NULL);
        return;
    }
#endif

    //
    // unboxing a value class simply means adding sizeof(void*) to the THIS pointer
    //
    X86EmitAddReg(THIS_kREG, sizeof(void*));
    EmitTailJumpToMethod(pUnboxMD);
}
#endif //TARGET_X86

#if defined(FEATURE_SHARE_GENERIC_CODE) && defined(TARGET_AMD64)
VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    STANDARD_VM_CONTRACT;

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        _ASSERTE((pEntry->srcofs & ShuffleEntry::REGMASK) && (pEntry->dstofs & ShuffleEntry::REGMASK));
        // Source in a general purpose or float register, destination in the same kind of a register or on stack
        int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;

        // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
        _ASSERTE((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK));
        int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;

        if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
        {
            // movdqa dstReg, srcReg
            X64EmitMovXmmXmm((X86Reg)(kXMM0 + dstRegIndex), (X86Reg)(kXMM0 + srcRegIndex));
        }
        else
        {
            // mov dstReg, srcReg
            X86EmitMovRegReg(c_argRegs[dstRegIndex], c_argRegs[srcRegIndex]);
        }
    }

    MetaSig msig(pSharedMD);
    ArgIterator argit(&msig);

    if (argit.HasParamType())
    {
        int paramTypeArgOffset = argit.GetParamTypeArgOffset();
        int paramTypeArgIndex = TransitionBlock::GetArgumentIndexFromOffset(paramTypeArgOffset);

        if (extraArg == NULL)
        {
            if (pSharedMD->RequiresInstMethodTableArg())
            {
                // Unboxing stub case
                // Extract MethodTable pointer (the hidden arg) from the object instance.
                X86EmitIndexRegLoad(c_argRegs[paramTypeArgIndex], THIS_kREG);
            }
        }
        else
        {
            X86EmitRegLoad(c_argRegs[paramTypeArgIndex], (UINT_PTR)extraArg);
        }
    }

    if (extraArg == NULL)
    {
        // Unboxing stub case
        // Skip over the MethodTable* to find the address of the unboxed value type.
        X86EmitAddReg(THIS_kREG, sizeof(void*));
    }

    EmitTailJumpToMethod(pSharedMD);
    SetTargetMethod(pSharedMD);
}
#endif // defined(FEATURE_SHARE_GENERIC_CODE) && defined(TARGET_AMD64)

#ifdef TARGET_AMD64
VOID StubLinkerCPU::EmitLoadMethodAddressIntoAX(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    PCODE multiCallableAddr = pMD->TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_PREFER_SLOT_OVER_TEMPORARY_ENTRYPOINT);

    if (multiCallableAddr != (PCODE)NULL)
    {
        X86EmitRegLoad(kRAX, multiCallableAddr);// MOV RAX, DWORD
    }
    else
    {
        X86EmitRegLoad(kRAX, (UINT_PTR)pMD->GetAddrOfSlot()); // MOV RAX, DWORD

        X86EmitIndexRegLoad(kRAX, kRAX);                // MOV RAX, [RAX]
    }
}
#endif

VOID StubLinkerCPU::EmitTailJumpToMethod(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    EmitLoadMethodAddressIntoAX(pMD);
    Emit16(X86_INSTR_JMP_EAX);
#else
    PCODE multiCallableAddr = pMD->TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_PREFER_SLOT_OVER_TEMPORARY_ENTRYPOINT);
    // Use direct call if possible
    if (multiCallableAddr != (PCODE)NULL)
    {
        X86EmitNearJump(NewExternalCodeLabel((LPVOID)multiCallableAddr));
    }
    else
    {
        // jmp [slot]
        Emit16(0x25ff);
        Emit32((DWORD)(size_t)pMD->GetAddrOfSlot());
    }
#endif
}

#if defined(FEATURE_SHARE_GENERIC_CODE) && !defined(FEATURE_INSTANTIATINGSTUB_AS_IL) && defined(TARGET_X86)
// The stub generated by this method passes an extra dictionary argument before jumping to
// shared-instantiation generic code.
//
// pMD is either
//    * An InstantiatedMethodDesc for a generic method whose code is shared across instantiations.
//      In this case, the extra argument is the InstantiatedMethodDesc for the instantiation-specific stub itself.
// or * A MethodDesc for a static method in a generic class whose code is shared across instantiations.
//      In this case, the extra argument is the MethodTable pointer of the instantiated type.
// or * A MethodDesc for unboxing stub. In this case, the extra argument is null.
VOID StubLinkerCPU::EmitInstantiatingMethodStub(MethodDesc* pMD, void* extra)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pMD->RequiresInstArg());
    }
    CONTRACTL_END;

    MetaSig msig(pMD);
    ArgIterator argit(&msig);

    int paramTypeArgOffset = argit.GetParamTypeArgOffset();

    // It's on the stack
    if (TransitionBlock::IsStackArgumentOffset(paramTypeArgOffset))
    {
        // Pop return address into AX
        X86EmitPopReg(kEAX);

        if (extra != NULL)
        {
            // Push extra dictionary argument
            X86EmitPushImmPtr(extra);
        }
        else
        {
            // Push the vtable pointer from "this"
            X86EmitIndexPush(THIS_kREG, 0);
        }

        // Put return address back
        X86EmitPushReg(kEAX);
    }
    // It's in a register
    else
    {
        X86Reg paramReg = GetX86ArgumentRegisterFromOffset(paramTypeArgOffset - TransitionBlock::GetOffsetOfArgumentRegisters());

        if (extra != NULL)
        {
            X86EmitRegLoad(paramReg, (UINT_PTR)extra);
        }
        else
        {
            // Just extract the vtable pointer from "this"
            X86EmitIndexRegLoad(paramReg, THIS_kREG);
        }
    }

    if (extra == NULL)
    {
        // Unboxing stub case.
        X86EmitAddReg(THIS_kREG, sizeof(void*));
    }

    EmitTailJumpToMethod(pMD);
}
#endif // defined(FEATURE_SHARE_GENERIC_CODE) && !defined(FEATURE_INSTANTIATINGSTUB_AS_IL) && defined(TARGET_X86)

VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64

    // mov SCRATCHREG,rsp
    X86_64BitOperands();
    Emit8(0x8b);
    Emit8(0304 | (SCRATCH_REGISTER_X86REG << 3));

    // save the real target in r11, will jump to it later.  r10 is used below.
    // Windows: mov r11, rcx
    // Unix: mov r11, rdi
    X86EmitMovRegReg(kR11, THIS_kREG);

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        if (pEntry->srcofs == ShuffleEntry::HELPERREG)
        {
            if (pEntry->dstofs & ShuffleEntry::REGMASK)
            {
                // movq dstReg, xmm8
                int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;
                X64EmitMovqRegXmm(c_argRegs[dstRegIndex], (X86Reg)kXMM8);
            }
            else
            {
                // movsd [rax + dst], xmm8
                int dstOffset = (pEntry->dstofs + 1) * sizeof(void*);
                X64EmitMovSDToMem((X86Reg)kXMM8, SCRATCH_REGISTER_X86REG, dstOffset);
            }
        }
        else if (pEntry->dstofs == ShuffleEntry::HELPERREG)
        {
            if (pEntry->srcofs & ShuffleEntry::REGMASK)
            {
                // movq xmm8, srcReg
                int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;
                X64EmitMovqXmmReg((X86Reg)kXMM8, c_argRegs[srcRegIndex]);
            }
            else
            {
                // movsd xmm8, [rax + src]
                int srcOffset = (pEntry->srcofs + 1) * sizeof(void*);
                X64EmitMovSDFromMem((X86Reg)(kXMM8), SCRATCH_REGISTER_X86REG, srcOffset);
            }
        }
        else if (pEntry->srcofs & ShuffleEntry::REGMASK)
        {
            // Source in a general purpose or float register, destination in the same kind of a register or on stack
            int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;

            if (pEntry->dstofs & ShuffleEntry::REGMASK)
            {
                // Source in register, destination in register

                // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
                _ASSERTE((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK));
                int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;

                if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
                {
                    // movdqa dstReg, srcReg
                    X64EmitMovXmmXmm((X86Reg)(kXMM0 + dstRegIndex), (X86Reg)(kXMM0 + srcRegIndex));
                }
                else
                {
                    // mov dstReg, srcReg
                    X86EmitMovRegReg(c_argRegs[dstRegIndex], c_argRegs[srcRegIndex]);
                }
            }
            else
            {
                // Source in register, destination on stack
                int dstOffset = (pEntry->dstofs + 1) * sizeof(void*);

                if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
                {
                    if (pEntry->dstofs & ShuffleEntry::FPSINGLEMASK)
                    {
                        // movss [rax + dst], srcReg
                        X64EmitMovSSToMem((X86Reg)(kXMM0 + srcRegIndex), SCRATCH_REGISTER_X86REG, dstOffset);
                    }
                    else
                    {
                        // movsd [rax + dst], srcReg
                        X64EmitMovSDToMem((X86Reg)(kXMM0 + srcRegIndex), SCRATCH_REGISTER_X86REG, dstOffset);
                    }
                }
                else
                {
                    // mov [rax + dst], srcReg
                    X86EmitIndexRegStore (SCRATCH_REGISTER_X86REG, dstOffset, c_argRegs[srcRegIndex]);
                }
            }
        }
        else if (pEntry->dstofs & ShuffleEntry::REGMASK)
        {
            // Source on stack, destination in register
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;
            int srcOffset = (pEntry->srcofs + 1) * sizeof(void*);

            if (pEntry->dstofs & ShuffleEntry::FPREGMASK)
            {
                if (pEntry->dstofs & ShuffleEntry::FPSINGLEMASK)
                {
                    // movss dstReg, [rax + src]
                    X64EmitMovSSFromMem((X86Reg)(kXMM0 + dstRegIndex), SCRATCH_REGISTER_X86REG, srcOffset);
                }
                else
                {
                    // movsd dstReg, [rax + src]
                    X64EmitMovSDFromMem((X86Reg)(kXMM0 + dstRegIndex), SCRATCH_REGISTER_X86REG, srcOffset);
                }
            }
            else
            {
                // mov dstreg, [rax + src]
                X86EmitIndexRegLoad(c_argRegs[dstRegIndex], SCRATCH_REGISTER_X86REG, srcOffset);
            }
        }
        else
        {
            // Source on stack, destination on stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));
            _ASSERTE(!(pEntry->dstofs & ShuffleEntry::REGMASK));

            // mov r10, [rax + src]
            X86EmitIndexRegLoad (kR10, SCRATCH_REGISTER_X86REG, (pEntry->srcofs + 1) * sizeof(void*));

            // mov [rax + dst], r10
            X86EmitIndexRegStore (SCRATCH_REGISTER_X86REG, (pEntry->dstofs + 1) * sizeof(void*), kR10);
        }
    }

    // mov r10, [r11 + Delegate._methodptraux]
    X86EmitIndexRegLoad(kR10, kR11, DelegateObject::GetOffsetOfMethodPtrAux());
    // add r11, DelegateObject::GetOffsetOfMethodPtrAux() - load the indirection cell into r11
    X86EmitAddReg(kR11, DelegateObject::GetOffsetOfMethodPtrAux());
    // Now jump to real target
    //   jmp r10
    static const BYTE bjmpr10[] = { 0x41, 0xff, 0xe2 };
    EmitBytes(bjmpr10, sizeof(bjmpr10));

#else // TARGET_AMD64

    UINT espadjust = 0;
    BOOL haveMemMemMove = FALSE;

    ShuffleEntry *pWalk = NULL;
    for (pWalk = pShuffleEntryArray; pWalk->srcofs != ShuffleEntry::SENTINEL; pWalk++)
    {
        if (!(pWalk->dstofs & ShuffleEntry::REGMASK) &&
            !(pWalk->srcofs & ShuffleEntry::REGMASK) &&
              pWalk->srcofs != pWalk->dstofs)
        {
            haveMemMemMove = TRUE;
            espadjust = sizeof(void*);
            break;
        }
    }

    if (haveMemMemMove)
    {
        // push ecx
        X86EmitPushReg(THIS_kREG);
    }
    else
    {
        // mov eax, ecx
        Emit8(0x8b);
        Emit8(0300 | SCRATCH_REGISTER_X86REG << 3 | THIS_kREG);
    }

    UINT16 emptySpot = 0x4 | ShuffleEntry::REGMASK;

    while (true)
    {
        for (pWalk = pShuffleEntryArray; pWalk->srcofs != ShuffleEntry::SENTINEL; pWalk++)
            if (pWalk->dstofs == emptySpot)
                break;

        if (pWalk->srcofs == ShuffleEntry::SENTINEL)
            break;

        if ((pWalk->dstofs & ShuffleEntry::REGMASK))
        {
            if (pWalk->srcofs & ShuffleEntry::REGMASK)
            {
                // mov <dstReg>,<srcReg>
                Emit8(0x8b);
                Emit8(static_cast<UINT8>(0300 |
                        (GetX86ArgumentRegisterFromOffset( pWalk->dstofs & ShuffleEntry::OFSMASK ) << 3) |
                        (GetX86ArgumentRegisterFromOffset( pWalk->srcofs & ShuffleEntry::OFSMASK ))));
            }
            else
            {
                X86EmitEspOffset(0x8b, GetX86ArgumentRegisterFromOffset( pWalk->dstofs & ShuffleEntry::OFSMASK ), pWalk->srcofs+espadjust);
            }
        }
        else
        {
            // if the destination is not a register, the source shouldn't be either.
            _ASSERTE(!(pWalk->srcofs & ShuffleEntry::REGMASK));
            if (pWalk->srcofs != pWalk->dstofs)
            {
               X86EmitEspOffset(0x8b, kEAX, pWalk->srcofs+espadjust);
               X86EmitEspOffset(0x89, kEAX, pWalk->dstofs+espadjust);
            }
        }
        emptySpot = pWalk->srcofs;
    }

    // Capture the stacksizedelta while we're at the end of the list.
    _ASSERTE(pWalk->srcofs == ShuffleEntry::SENTINEL);

    if (haveMemMemMove)
        X86EmitPopReg(SCRATCH_REGISTER_X86REG);

#ifdef UNIX_X86_ABI
    _ASSERTE(pWalk->stacksizedelta == 0);
#endif

    if (pWalk->stacksizedelta)
        X86EmitAddEsp(pWalk->stacksizedelta);

    // Now jump to real target
    //   JMP [SCRATCHREG]
    // we need to jump indirect so that for virtual delegates eax contains a pointer to the indirection cell
    X86EmitAddReg(SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtrAux());
    static const BYTE bjmpeax[] = { 0xff, 0x20 };
    EmitBytes(bjmpeax, sizeof(bjmpeax));

#endif // TARGET_AMD64
}

#endif // !DACCESS_COMPILE
