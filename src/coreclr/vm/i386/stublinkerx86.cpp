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

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "comcache.h"
#include "olevariant.h"
#endif // FEATURE_COMINTEROP

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
#include <psapi.h>
#endif


#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
extern "C" HRESULT __cdecl StubRareDisableHR(Thread *pThread);
#endif // FEATURE_COMINTEROP
extern "C" VOID __cdecl StubRareDisableTHROW(Thread *pThread, Frame *pFrame);

#if defined(TARGET_AMD64)
#if defined(_DEBUG)
extern "C" VOID __cdecl DebugCheckStubUnwindInfo();
#endif // _DEBUG
#endif // TARGET_AMD64

#ifdef FEATURE_COMINTEROP
// Use a type alias as MSVC has issues parsing the pointer, the calling convention, and the declspec
// in the same signature.
// Disable ASAN here as this method uses inline assembly and touches registers that ASAN uses.
using ThreadPointer = Thread*;
ThreadPointer DISABLE_ASAN __stdcall CreateThreadBlockReturnHr(ComMethodFrame *pFrame);
#endif



#ifdef TARGET_AMD64

BOOL IsPreservedReg (X86Reg reg)
{
    UINT16 PreservedRegMask =
          (1 << kRBX)
        | (1 << kRBP)
        | (1 << kRSI)
        | (1 << kRDI)
        | (1 << kR12)
        | (1 << kR13)
        | (1 << kR14)
        | (1 << kR15);
    return PreservedRegMask & (1 << reg);
}

#endif // TARGET_AMD64

#ifdef TARGET_AMD64
//-----------------------------------------------------------------------
// InstructionFormat for near Jump and short Jump
//-----------------------------------------------------------------------

//X64EmitTailcallWithRSPAdjust
class X64NearJumpSetup : public InstructionFormat
{
    public:
        X64NearJumpSetup() : InstructionFormat(  InstructionFormat::k8|InstructionFormat::k32
                                                       | InstructionFormat::k64Small | InstructionFormat::k64
                                                      )
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT
            switch (refsize)
            {
                case k8:
                    return 0;

                case k32:
                    return 0;

                case k64Small:
                    return 5;

                case k64:
                    return 10;

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
                // do nothing, X64NearJump will take care of this
            }
            else if (k32 == refsize)
            {
                // do nothing, X64NearJump will take care of this
            }
            else if (k64Small == refsize)
            {
                UINT64 TargetAddress = (INT64)pOutBufferRX + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov eax, imm32  ; zero-extended
                pOutBufferRW[0] = 0xB8;
                *((UINT32*)&pOutBufferRW[1]) = (UINT32)TargetAddress;
            }
            else if (k64 == refsize)
            {
                // mov rax, imm64
                pOutBufferRW[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBufferRW[1] = 0xB8;
                *((UINT64*)&pOutBufferRW[2]) = (UINT64)(((INT64)pOutBufferRX) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));
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

                case InstructionFormat::k64Small:
                    return FitsInI4(offset);

                case InstructionFormat::k64:
                    // intentional fallthru
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
                    return FitsInI4(offset);

                case InstructionFormat::k64Small:
                    // EmitInstruction emits a non-relative jmp for
                    // k64Small.  We don't have enough info to predict the
                    // target address.  (Even if we did, this would only
                    // handle the set of unsigned offsets with bit 31 set
                    // and no higher bits set, too uncommon/hard to test.)
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;
                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
};

class X64NearJumpExecute : public InstructionFormat
{
    public:
        X64NearJumpExecute() : InstructionFormat(  InstructionFormat::k8|InstructionFormat::k32
                                                 | InstructionFormat::k64Small | InstructionFormat::k64
                                                 )
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

                case k64Small:
                    return 3;

                case k64:
                    return 3;

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
            else if (k64Small == refsize)
            {
                // REX.W jmp rax
                pOutBufferRW[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBufferRW[1] = 0xFF;
                pOutBufferRW[2] = 0xE0;
            }
            else if (k64 == refsize)
            {
                // REX.W jmp rax
                pOutBufferRW[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBufferRW[1] = 0xFF;
                pOutBufferRW[2] = 0xE0;
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

                case InstructionFormat::k64Small:
                    return FitsInI4(offset);

                case InstructionFormat::k64:
                    // intentional fallthru
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
                    return FitsInI4(offset);

                case InstructionFormat::k64Small:
                    // EmitInstruction emits a non-relative jmp for
                    // k64Small.  We don't have enough info to predict the
                    // target address.  (Even if we did, this would only
                    // handle the set of unsigned offsets with bit 31 set
                    // and no higher bits set, too uncommon/hard to test.)
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;
                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
};

#endif

//-----------------------------------------------------------------------
// InstructionFormat for near Jump and short Jump
//-----------------------------------------------------------------------
class X86NearJump : public InstructionFormat
{
    public:
        X86NearJump() : InstructionFormat(  InstructionFormat::k8|InstructionFormat::k32
#ifdef TARGET_AMD64
                                          | InstructionFormat::k64Small | InstructionFormat::k64
#endif // TARGET_AMD64
                                          )
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
#ifdef TARGET_AMD64
                case k64Small:
                    return 5 + 2;

                case k64:
                    return 12;
#endif // TARGET_AMD64
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
#ifdef TARGET_AMD64
            else if (k64Small == refsize)
            {
                UINT64 TargetAddress = (INT64)pOutBufferRX + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov eax, imm32  ; zero-extended
                pOutBufferRW[0] = 0xB8;
                *((UINT32*)&pOutBufferRW[1]) = (UINT32)TargetAddress;

                // jmp rax
                pOutBufferRW[5] = 0xFF;
                pOutBufferRW[6] = 0xE0;
            }
            else if (k64 == refsize)
            {
                // mov rax, imm64
                pOutBufferRW[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBufferRW[1] = 0xB8;
                *((UINT64*)&pOutBufferRW[2]) = (UINT64)(((INT64)pOutBufferRX) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

                // jmp rax
                pOutBufferRW[10] = 0xFF;
                pOutBufferRW[11] = 0xE0;
            }
#endif // TARGET_AMD64
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

#ifdef TARGET_AMD64
                case InstructionFormat::k64Small:
                    return FitsInI4(offset);

                case InstructionFormat::k64:
                    // intentional fallthru
#endif
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
#ifdef TARGET_AMD64
                    return FitsInI4(offset);
#else
                    return TRUE;
#endif

#ifdef TARGET_AMD64
                case InstructionFormat::k64Small:
                    // EmitInstruction emits a non-relative jmp for
                    // k64Small.  We don't have enough info to predict the
                    // target address.  (Even if we did, this would only
                    // handle the set of unsigned offsets with bit 31 set
                    // and no higher bits set, too uncommon/hard to test.)
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
#endif
                case InstructionFormat::kAllowAlways:
                    return TRUE;
                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
};


//-----------------------------------------------------------------------
// InstructionFormat for conditional jump. Set the variationCode
// to members of X86CondCode.
//-----------------------------------------------------------------------
class X86CondJump : public InstructionFormat
{
    public:
        X86CondJump(UINT allowedSizes) : InstructionFormat(allowedSizes)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
        LIMITED_METHOD_CONTRACT
            return (refsize == k8 ? 2 : 6);
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
        LIMITED_METHOD_CONTRACT
        if (refsize == k8)
        {
                pOutBufferRW[0] = static_cast<BYTE>(0x70 | variationCode);
                *((int8_t*)(pOutBufferRW+1)) = (int8_t)fixedUpReference;
        }
        else
        {
                pOutBufferRW[0] = 0x0f;
                pOutBufferRW[1] = static_cast<BYTE>(0x80 | variationCode);
                *((int32_t*)(pOutBufferRW+2)) = (int32_t)fixedUpReference;
            }
        }
};


//-----------------------------------------------------------------------
// InstructionFormat for near call.
//-----------------------------------------------------------------------
class X86Call : public InstructionFormat
{
    public:
        X86Call ()
            : InstructionFormat(  InstructionFormat::k32
#ifdef TARGET_AMD64
                                | InstructionFormat::k64Small | InstructionFormat::k64
#endif // TARGET_AMD64
                                )
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;

            switch (refsize)
            {
            case k32:
                return 5;

#ifdef TARGET_AMD64
            case k64Small:
                return 5 + 2;

            case k64:
                return 10 + 2;
#endif // TARGET_AMD64

            default:
                _ASSERTE(!"unexpected refsize");
                return 0;
            }
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            switch (refsize)
            {
            case k32:
                pOutBufferRW[0] = 0xE8;
                *((int32_t*)(1+pOutBufferRW)) = (int32_t)fixedUpReference;
                break;

#ifdef TARGET_AMD64
            case k64Small:
                UINT64 TargetAddress;

                TargetAddress = (INT64)pOutBufferRX + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov  eax,<fixedUpReference>  ; zero-extends
                pOutBufferRW[0] = 0xB8;
                *((UINT32*)&pOutBufferRW[1]) = (UINT32)TargetAddress;

                // call rax
                pOutBufferRW[5] = 0xff;
                pOutBufferRW[6] = 0xd0;
                break;

            case k64:
                // mov  rax,<fixedUpReference>
                pOutBufferRW[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBufferRW[1] = 0xB8;
                *((UINT64*)&pOutBufferRW[2]) = (UINT64)(((INT64)pOutBufferRX) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

                // call rax
                pOutBufferRW[10] = 0xff;
                pOutBufferRW[11] = 0xd0;
                break;
#endif // TARGET_AMD64

            default:
                _ASSERTE(!"unreached");
                break;
            }
        }

// For x86, the default CanReach implementation will suffice.  It only needs
// to handle k32.
#ifdef TARGET_AMD64
        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            if (fExternal)
            {
                switch (refsize)
                {
                case InstructionFormat::k32:
                    // For external, we don't have enough info to predict
                    // the offset.
                    return FALSE;

                case InstructionFormat::k64Small:
                    return FitsInI4(offset);

                case InstructionFormat::k64:
                    // intentional fallthru
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
                case InstructionFormat::k32:
                    return FitsInI4(offset);

                case InstructionFormat::k64Small:
                    // EmitInstruction emits a non-relative jmp for
                    // k64Small.  We don't have enough info to predict the
                    // target address.  (Even if we did, this would only
                    // handle the set of unsigned offsets with bit 31 set
                    // and no higher bits set, too uncommon/hard to test.)
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;
                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
#endif // TARGET_AMD64
};


//-----------------------------------------------------------------------
// InstructionFormat for push imm32.
//-----------------------------------------------------------------------
class X86PushImm32 : public InstructionFormat
{
    public:
        X86PushImm32(UINT allowedSizes) : InstructionFormat(allowedSizes)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;

            return 5;
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT;

            pOutBufferRW[0] = 0x68;
            // only support absolute pushimm32 of the label address. The fixedUpReference is
            // the offset to the label from the current point, so add to get address
            *((int32_t*)(1+pOutBufferRW)) = (int32_t)(fixedUpReference);
        }
};

#if defined(TARGET_AMD64)
//-----------------------------------------------------------------------
// InstructionFormat for lea reg, [RIP relative].
//-----------------------------------------------------------------------
class X64LeaRIP : public InstructionFormat
{
    public:
        X64LeaRIP() : InstructionFormat(InstructionFormat::k64Small)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;

            return 7;
        }

        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            if (fExternal)
            {
                switch (refsize)
                {
                case InstructionFormat::k64Small:
                    // For external, we don't have enough info to predict
                    // the offset.
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
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
                case InstructionFormat::k64Small:
                    return FitsInI4(offset);

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;

                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT;

            X86Reg reg = (X86Reg)variationCode;
            BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

            if (reg >= kR8)
            {
                rex |= REX_MODRM_REG_EXT;
                reg = X86RegFromAMD64Reg(reg);
            }

            pOutBufferRW[0] = rex;
            pOutBufferRW[1] = 0x8D;
            pOutBufferRW[2] = (BYTE)(0x05 | (reg << 3));
            // only support absolute pushimm32 of the label address. The fixedUpReference is
            // the offset to the label from the current point, so add to get address
            *((int32_t*)(3+pOutBufferRW)) = (int32_t)(fixedUpReference);
        }
};

#endif // TARGET_AMD64

#if defined(TARGET_AMD64)
static BYTE gX64NearJumpSetup[sizeof(X64NearJumpSetup)];
static BYTE gX64NearJumpExecute[sizeof(X64NearJumpExecute)];
static BYTE gX64LeaRIP[sizeof(X64LeaRIP)];
#endif

static BYTE gX86NearJump[sizeof(X86NearJump)];
static BYTE gX86CondJump[sizeof(X86CondJump)];
static BYTE gX86Call[sizeof(X86Call)];
static BYTE gX86PushImm32[sizeof(X86PushImm32)];

/* static */ void StubLinkerCPU::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    new (gX86NearJump) X86NearJump();
    new (gX86CondJump) X86CondJump( InstructionFormat::k8|InstructionFormat::k32);
    new (gX86Call) X86Call();
    new (gX86PushImm32) X86PushImm32(InstructionFormat::k32);

#if defined(TARGET_AMD64)
    new (gX64NearJumpSetup) X64NearJumpSetup();
    new (gX64NearJumpExecute) X64NearJumpExecute();
    new (gX64LeaRIP) X64LeaRIP();
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

//---------------------------------------------------------------

VOID StubLinkerCPU::X86EmitMovSPReg(X86Reg srcReg)
{
    STANDARD_VM_CONTRACT;
    const X86Reg kESP = (X86Reg)4;
    X86EmitMovRegReg(kESP, srcReg);
}

VOID StubLinkerCPU::X86EmitMovRegSP(X86Reg destReg)
{
    STANDARD_VM_CONTRACT;
    const X86Reg kESP = (X86Reg)4;
    X86EmitMovRegReg(destReg, kESP);
}


//---------------------------------------------------------------
// Emits:
//    PUSH <reg32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    X86Reg origReg = reg;
#endif

#ifdef TARGET_AMD64
    if (reg >= kR8)
    {
        Emit8(REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT | REX_OPCODE_REG_EXT);
        reg = X86RegFromAMD64Reg(reg);
    }
#endif
    Emit8(static_cast<UINT8>(0x50 + reg));

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    if (IsPreservedReg(origReg))
    {
        UnwindPushedReg(origReg);
    }
    else
#endif
    {
        Push(sizeof(void*));
    }
}


//---------------------------------------------------------------
// Emits:
//    POP <reg32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPopReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    if (reg >= kR8)
    {
        Emit8(REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT | REX_OPCODE_REG_EXT);
        reg = X86RegFromAMD64Reg(reg);
    }
#endif // TARGET_AMD64

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
//    PUSH <imm32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushImm32(CodeLabel &target)
{
    STANDARD_VM_CONTRACT;

    EmitLabelRef(&target, reinterpret_cast<X86PushImm32&>(gX86PushImm32), 0);
}


//---------------------------------------------------------------
// Emits:
//    PUSH <imm8>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushImm8(BYTE value)
{
    STANDARD_VM_CONTRACT;

    Emit8(0x6a);
    Emit8(value);
    Push(sizeof(void*));
}


//---------------------------------------------------------------
// Emits:
//    PUSH <ptr>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitPushImmPtr(LPVOID value BIT64_ARG(X86Reg tmpReg /*=kR10*/))
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    X86EmitRegLoad(tmpReg, (UINT_PTR) value);
    X86EmitPushReg(tmpReg);
#else
    X86EmitPushImm32((UINT_PTR) value);
#endif
}

//---------------------------------------------------------------
// Emits:
//    XOR <reg32>,<reg32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitZeroOutReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    // 32-bit results are zero-extended, so we only need the REX byte if
    // it's an extended register.
    if (reg >= kR8)
    {
        Emit8(REX_PREFIX_BASE | REX_MODRM_REG_EXT | REX_MODRM_RM_EXT);
        reg = X86RegFromAMD64Reg(reg);
    }
#endif
    Emit8(0x33);
    Emit8(static_cast<UINT8>(0xc0 | (reg << 3) | reg));
}

//---------------------------------------------------------------
// Emits:
//    jmp [reg]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitJumpReg(X86Reg reg)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    Emit8(0xff);
    Emit8(static_cast<BYTE>(0xe0) | static_cast<BYTE>(reg));
}

//---------------------------------------------------------------
// Emits:
//    CMP <reg32>,imm32
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitCmpRegImm32(X86Reg reg, INT32 imm32)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION((int) reg < NumX86Regs);
    }
    CONTRACTL_END;

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
        Emit8(static_cast<UINT8>(0xF8 | reg));
        Emit8((INT8)imm32);
    } else {
        Emit8(0x81);
        Emit8(static_cast<UINT8>(0xF8 | reg));
        Emit32(imm32);
    }
}

#ifdef TARGET_AMD64
//---------------------------------------------------------------
// Emits:
//    CMP [reg+offs], imm32
//    CMP [reg], imm32
//---------------------------------------------------------------
VOID StubLinkerCPU:: X86EmitCmpRegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32)
{
    STANDARD_VM_CONTRACT;

    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

    if (reg >= kR8)
    {
        rex |= REX_OPCODE_REG_EXT;
        reg = X86RegFromAMD64Reg(reg);
    }
    Emit8(rex);

    X64EmitCmp32RegIndexImm32(reg, offs, imm32);
}

VOID StubLinkerCPU:: X64EmitCmp32RegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32)
#else // TARGET_AMD64
VOID StubLinkerCPU:: X86EmitCmpRegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32)
#endif // TARGET_AMD64
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION((int) reg < NumX86Regs);
    }
    CONTRACTL_END;

    //
    // The binary representation of "cmp [mem], imm32" is :
    // 1000-00sw  mod11-1r/m
    //

    unsigned wBit = (FitsInI1(imm32) ? 0 : 1);
    Emit8(static_cast<UINT8>(0x80 | wBit));

    unsigned modBits;
    if (offs == 0)
        modBits = 0;
    else if (FitsInI1(offs))
        modBits = 1;
    else
        modBits = 2;

    Emit8(static_cast<UINT8>((modBits << 6) | 0x38 | reg));

    if (offs)
    {
        if (FitsInI1(offs))
            Emit8((INT8)offs);
        else
            Emit32(offs);
    }

    if (FitsInI1(imm32))
        Emit8((INT8)imm32);
    else
        Emit32(imm32);
}

//---------------------------------------------------------------
// Emits:
#if defined(TARGET_AMD64)
//  mov     rax, <target>
//  add     rsp, imm32
//  jmp     rax
#else
//  add     rsp, imm32
//  jmp     <target>
#endif
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitTailcallWithESPAdjust(CodeLabel *pTarget, INT32 imm32)
{
    STANDARD_VM_CONTRACT;

#if defined(TARGET_AMD64)
    EmitLabelRef(pTarget, reinterpret_cast<X64NearJumpSetup&>(gX64NearJumpSetup), 0);
    X86EmitAddEsp(imm32);
    EmitLabelRef(pTarget, reinterpret_cast<X64NearJumpExecute&>(gX64NearJumpExecute), 0);
#else
    X86EmitAddEsp(imm32);
    X86EmitNearJump(pTarget);
#endif
}

//---------------------------------------------------------------
// Emits:
#if defined(TARGET_AMD64)
//  mov     rax, <target>
//  pop     reg
//  jmp     rax
#else
//  pop     reg
//  jmp     <target>
#endif
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitTailcallWithSinglePop(CodeLabel *pTarget, X86Reg reg)
{
    STANDARD_VM_CONTRACT;

#if defined(TARGET_AMD64)
    EmitLabelRef(pTarget, reinterpret_cast<X64NearJumpSetup&>(gX64NearJumpSetup), 0);
    X86EmitPopReg(reg);
    EmitLabelRef(pTarget, reinterpret_cast<X64NearJumpExecute&>(gX64NearJumpExecute), 0);
#else
    X86EmitPopReg(reg);
    X86EmitNearJump(pTarget);
#endif
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


//---------------------------------------------------------------
// Emits:
//    Jcc <ofs8> or
//    Jcc <ofs32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitCondJump(CodeLabel *target, X86CondCode::cc condcode)
{
    STANDARD_VM_CONTRACT;
    EmitLabelRef(target, reinterpret_cast<X86CondJump&>(gX86CondJump), condcode);
}


//---------------------------------------------------------------
// Emits:
//    call <ofs32>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitCall(CodeLabel *target, int iArgBytes)
{
    STANDARD_VM_CONTRACT;

    EmitLabelRef(target, reinterpret_cast<X86Call&>(gX86Call), 0);

    INDEBUG(Emit8(0x90));   // Emit a nop after the call in debug so that
                            // we know that this is a call that can directly call
                            // managed code
#ifndef TARGET_AMD64
    Pop(iArgBytes);
#endif // !TARGET_AMD64
}


//---------------------------------------------------------------
// Emits:
//    ret n
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitReturn(WORD wArgBytes)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
#if defined(TARGET_AMD64) || defined(UNIX_X86_ABI)
        PRECONDITION(wArgBytes == 0);
#endif

    }
    CONTRACTL_END;

    if (wArgBytes == 0)
        Emit8(0xc3);
    else
    {
        Emit8(0xc2);
        Emit16(wArgBytes);
    }

    Pop(wArgBytes);
}

#ifdef TARGET_AMD64
//---------------------------------------------------------------
// Emits:
//    JMP <ofs8>   or
//    JMP <ofs32}
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitLeaRIP(CodeLabel *target, X86Reg reg)
{
    STANDARD_VM_CONTRACT;
    EmitLabelRef(target, reinterpret_cast<X64LeaRIP&>(gX64LeaRIP), reg);
}
#endif // TARGET_AMD64



VOID StubLinkerCPU::X86EmitPushRegs(unsigned regSet)
{
    STANDARD_VM_CONTRACT;

    for (X86Reg r = kEAX; r <= NumX86Regs; r = (X86Reg)(r+1))
        if (regSet & (1U<<r))
        {
            X86EmitPushReg(r);
        }
}


VOID StubLinkerCPU::X86EmitPopRegs(unsigned regSet)
{
    STANDARD_VM_CONTRACT;

    for (X86Reg r = NumX86Regs; r >= kEAX; r = (X86Reg)(r-1))
        if (regSet & (1U<<r))
            X86EmitPopReg(r);
}


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

    if (dstreg != kESP_Unsafe)
        X86EmitOffsetModRM(0x89, srcreg, dstreg, ofs);
    else
        X86EmitOp(0x89, srcreg, (X86Reg)kESP_Unsafe,  ofs);
}

#if defined(TARGET_AMD64)
//---------------------------------------------------------------
// Emits:
//    mov [RSP + <ofs>],<srcreg>
//
// It marks the instruction has 64bit so that the processor
// performs a 8byte data move to a RSP based stack location.
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexRegStoreRSP(int32_t ofs,
                                         X86Reg srcreg)
{
    STANDARD_VM_CONTRACT;

    X86EmitOp(0x89, srcreg, (X86Reg)kESP_Unsafe,  ofs, (X86Reg)0, 0, k64BitOp);
}

//---------------------------------------------------------------
// Emits:
//    mov [R12 + <ofs>],<srcreg>
//
// It marks the instruction has 64bit so that the processor
// performs a 8byte data move to a R12 based stack location.
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexRegStoreR12(int32_t ofs,
                                         X86Reg srcreg)
{
    STANDARD_VM_CONTRACT;

    X86EmitOp(0x89, srcreg, (X86Reg)kR12,  ofs, (X86Reg)0, 0, k64BitOp);
}
#endif // defined(TARGET_AMD64)

//---------------------------------------------------------------
// Emits:
//    push dword ptr [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexPush(X86Reg srcreg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    if(srcreg != kESP_Unsafe)
        X86EmitOffsetModRM(0xff, (X86Reg)0x6, srcreg, ofs);
    else
        X86EmitOp(0xff,(X86Reg)0x6, srcreg, ofs);

    Push(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    push dword ptr [<baseReg> + <indexReg>*<scale> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitBaseIndexPush(
        X86Reg baseReg,
        X86Reg indexReg,
        int32_t scale,
        int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    X86EmitOffsetModRmSIB(0xff, (X86Reg)0x6, baseReg, indexReg, scale, ofs);
    Push(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    push dword ptr [ESP + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitSPIndexPush(int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    int8_t ofs8 = (int8_t) ofs;
    if (ofs == (int32_t) ofs8)
    {
        // The offset can be expressed in a byte (can use the byte
        // form of the push esp instruction)

        BYTE code[] = {0xff, 0x74, 0x24, (BYTE)ofs8};
        EmitBytes(code, sizeof(code));
    }
    else
    {
        // The offset requires 4 bytes (need to use the long form
        // of the push esp instruction)

        BYTE code[] = {0xff, 0xb4, 0x24, 0x0, 0x0, 0x0, 0x0};
        *(int32_t *)(&code[3]) = ofs;
        EmitBytes(code, sizeof(code));
    }

    Push(sizeof(void*));
}


//---------------------------------------------------------------
// Emits:
//    pop dword ptr [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexPop(X86Reg srcreg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    if(srcreg != kESP_Unsafe)
        X86EmitOffsetModRM(0x8f, (X86Reg)0x0, srcreg, ofs);
    else
        X86EmitOp(0x8f,(X86Reg)0x0, srcreg, ofs);

    Pop(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    lea <dstreg>, [<srcreg> + <ofs>
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexLea(X86Reg dstreg, X86Reg srcreg, int32_t ofs)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION((int) dstreg < NumX86Regs);
        PRECONDITION((int) srcreg < NumX86Regs);
    }
    CONTRACTL_END;

    X86EmitOffsetModRM(0x8d, dstreg, srcreg, ofs);
}

#if defined(TARGET_AMD64)
VOID StubLinkerCPU::X86EmitIndexLeaRSP(X86Reg dstreg, X86Reg srcreg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;

    X86EmitOp(0x8d, dstreg, (X86Reg)kESP_Unsafe,  ofs, (X86Reg)0, 0, k64BitOp);
}
#endif // defined(TARGET_AMD64)

//---------------------------------------------------------------
// Emits:
//   sub esp, IMM
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitSubEsp(INT32 imm32)
{
    STANDARD_VM_CONTRACT;

    if (imm32 < 0x1000-100)
    {
        // As long as the esp size is less than 1 page plus a small
        // safety fudge factor, we can just bump esp.
        X86EmitSubEspWorker(imm32);
    }
    else
    {
        // Otherwise, must touch at least one byte for each page.
        while (imm32 >= 0x1000)
        {

            X86EmitSubEspWorker(0x1000-4);
            X86EmitPushReg(kEAX);

            imm32 -= 0x1000;
        }
        if (imm32 < 500)
        {
            X86EmitSubEspWorker(imm32);
        }
        else
        {
            // If the remainder is large, touch the last byte - again,
            // as a fudge factor.
            X86EmitSubEspWorker(imm32-4);
            X86EmitPushReg(kEAX);
        }
    }
}


//---------------------------------------------------------------
// Emits:
//   sub esp, IMM
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitSubEspWorker(INT32 imm32)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

    // On Win32, stacks must be faulted in one page at a time.
        PRECONDITION(imm32 < 0x1000);
    }
    CONTRACTL_END;

    if (!imm32)
    {
        // nop
    }
    else
    {
        X86_64BitOperands();

        if (FitsInI1(imm32))
        {
            Emit16(0xec83);
            Emit8((INT8)imm32);
        }
        else
        {
            Emit16(0xec81);
            Emit32(imm32);
        }

        Push(imm32);
    }
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

//---------------------------------------------------------------
// Emits: add destReg, srcReg
//---------------------------------------------------------------

VOID StubLinkerCPU::X86EmitAddRegReg(X86Reg destReg, X86Reg srcReg)
{
    STANDARD_VM_CONTRACT;

    X86EmitR2ROp(0x01, srcReg, destReg);
}




VOID StubLinkerCPU::X86EmitSubReg(X86Reg reg, INT32 imm32)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION((int) reg < NumX86Regs);
    }
    CONTRACTL_END;

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
        Emit8(static_cast<UINT8>(0xE8 | reg));
        Emit8(static_cast<UINT8>(imm32));
    } else {
        Emit8(0x81);
        Emit8(static_cast<UINT8>(0xE8 | reg));
        Emit32(imm32);
    }
}

//---------------------------------------------------------------
// Emits: sub destReg, srcReg
//---------------------------------------------------------------

VOID StubLinkerCPU::X86EmitSubRegReg(X86Reg destReg, X86Reg srcReg)
{
    STANDARD_VM_CONTRACT;

    X86EmitR2ROp(0x29, srcReg, destReg);
}

#if defined(TARGET_AMD64)

//---------------------------------------------------------------
// movdqa destXmmreg, srcXmmReg
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovXmmXmm(X86Reg destXmmreg, X86Reg srcXmmReg)
{
    STANDARD_VM_CONTRACT;
    // There are several that could be used to mov xmm registers. MovAps is
    // what C++ compiler uses so let's use it here too.
    X86EmitR2ROp(X86_INSTR_MOVAPS_R_RM, destXmmreg, srcXmmReg, k32BitOp);
}

//---------------------------------------------------------------
// movdqa XmmN, [baseReg + offset]
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovdqaFromMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0x66, 0x6F, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movdqa [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovdqaToMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0x66, 0x7F, Xmmreg, baseReg, ofs);
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

//---------------------------------------------------------------
// Emits a MOD/RM for accessing a dword at [<baseReg> + <indexReg>*<scale> + ofs32]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitOffsetModRmSIB(BYTE opcode, X86Reg opcodeOrReg, X86Reg baseReg, X86Reg indexReg, int32_t scale, int32_t ofs)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(scale == 1 || scale == 2 || scale == 4 || scale == 8);
        PRECONDITION(indexReg != kESP_Unsafe);
    }
    CONTRACTL_END;

    BYTE    codeBuffer[8];
    BYTE*   code    = codeBuffer;
    int     nBytes  = 0;

#ifdef TARGET_AMD64
    _ASSERTE(!"NYI");
#endif
    code[0] = opcode;
    nBytes++;

    BYTE scaleEnc = 0;
    switch(scale)
    {
        case 1: scaleEnc = 0; break;
        case 2: scaleEnc = 1; break;
        case 4: scaleEnc = 2; break;
        case 8: scaleEnc = 3; break;
        default: _ASSERTE(!"Unexpected");
    }

    BYTE sib = static_cast<BYTE>((scaleEnc << 6) | (indexReg << 3) | baseReg);

    if (FitsInI1(ofs))
    {
        code[1] = static_cast<BYTE>(0x44 | (opcodeOrReg << 3));
        code[2] = sib;
        code[3] = (BYTE)ofs;
        nBytes += 3;
        EmitBytes(codeBuffer, nBytes);
    }
    else
    {
        code[1] = static_cast<BYTE>(0x84 | (opcodeOrReg << 3));
        code[2] = sib;
        *(int32_t*)(&code[3]) = ofs;
        nBytes += 6;
        EmitBytes(codeBuffer, nBytes);
    }
}



VOID StubLinkerCPU::X86EmitRegLoad(X86Reg reg, UINT_PTR imm)
{
    STANDARD_VM_CONTRACT;

    if (!imm)
    {
        X86EmitZeroOutReg(reg);
        return;
    }

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


// Emits
//
//    opcode altreg, modrmreg
//
// or
//
//    opcode modrmreg, altreg
//
// (the opcode determines which one comes first)
//
// For single-operand opcodes, "altreg" actually selects
// an operation rather than a register.

VOID StubLinkerCPU::X86EmitR2ROp (WORD opcode,
                                  X86Reg altreg,
                                  X86Reg modrmreg
                        AMD64_ARG(X86OperandSize OperandSize /*= k64BitOp*/)
                                  )
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        // All 2-byte opcodes start with 0x0f.
        PRECONDITION(!(opcode >> 8) || (opcode & 0xff) == 0x0f);

        PRECONDITION( ((UINT)altreg) < NumX86Regs );
        PRECONDITION( ((UINT)modrmreg) < NumX86Regs );
    }
    CONTRACTL_END;

#ifdef TARGET_AMD64
    BYTE rex = 0;

    if (modrmreg >= kR8)
    {
        rex |= REX_MODRM_RM_EXT;
        modrmreg = X86RegFromAMD64Reg(modrmreg);
    }

    if (altreg >= kR8)
    {
        rex |= REX_MODRM_REG_EXT;
        altreg = X86RegFromAMD64Reg(altreg);
    }

    if (k64BitOp == OperandSize)
        rex |= REX_OPERAND_SIZE_64BIT;

    if (rex)
        Emit8(REX_PREFIX_BASE | rex);
#endif // TARGET_AMD64

    Emit8((BYTE)opcode);

    if (opcode >> 8)
        Emit8(opcode >> 8);

    Emit8(static_cast<UINT8>(0300 | (altreg << 3) | modrmreg));
}


//---------------------------------------------------------------
// Emits:
//   op altreg, [esp+ofs]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitEspOffset(BYTE opcode,
                                     X86Reg altreg,
                                     int32_t ofs
                           AMD64_ARG(X86OperandSize OperandSize /*= k64BitOp*/)
                                     )
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[8];
    BYTE   *code = codeBuffer;
    int     nBytes;

#ifdef TARGET_AMD64
    BYTE rex = 0;

    if (k64BitOp == OperandSize)
        rex |= REX_OPERAND_SIZE_64BIT;

    if (altreg >= kR8)
    {
        rex |= REX_MODRM_REG_EXT;
        altreg = X86RegFromAMD64Reg(altreg);
    }

    if (rex)
    {
        *code = (REX_PREFIX_BASE | rex);
        code++;
        nBytes = 1;
    }
    else
#endif // TARGET_AMD64
    {
        nBytes = 0;
    }

    code[0] = opcode;
    BYTE modrm = static_cast<BYTE>((altreg << 3) | 004);
    if (ofs == 0)
    {
        code[1] = modrm;
        code[2] = 0044;
        EmitBytes(codeBuffer, 3 + nBytes);
    }
    else if (FitsInI1(ofs))
    {
        code[1] = 0x40|modrm;
        code[2] = 0044;
        code[3] = (BYTE)ofs;
        EmitBytes(codeBuffer, 4 + nBytes);
    }
    else
    {
        code[1] = 0x80|modrm;
        code[2] = 0044;
        *((int32_t*)(3+code)) = ofs;
        EmitBytes(codeBuffer, 7 + nBytes);
    }

}

//---------------------------------------------------------------

VOID StubLinkerCPU::X86EmitPushEBPframe()
{
    STANDARD_VM_CONTRACT;

    //  push ebp
    X86EmitPushReg(kEBP);
    // mov ebp,esp
    X86EmitMovRegSP(kEBP);
}

#ifdef _DEBUG
//---------------------------------------------------------------
// Emits:
//     mov <reg32>,0xcccccccc
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitDebugTrashReg(X86Reg reg)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

    if (reg >= kR8)
    {
        rex |= REX_OPCODE_REG_EXT;
        reg = X86RegFromAMD64Reg(reg);
    }
    Emit8(rex);
    Emit8(0xb8|reg);
    Emit64(0xcccccccccccccccc);
#else
    Emit8(static_cast<UINT8>(0xb8 | reg));
    Emit32(0xcccccccc);
#endif
}
#endif //_DEBUG


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



#if defined(_DEBUG) && !defined(TARGET_UNIX)
void StubLinkerCPU::EmitJITHelperLoggingThunk(PCODE pJitHelper, LPVOID helperFuncCount)
{
    STANDARD_VM_CONTRACT;

    VMHELPCOUNTDEF* pHelperFuncCount = (VMHELPCOUNTDEF*)helperFuncCount;
/*
        push        rcx
        mov         rcx, &(pHelperFuncCount->count)
   lock inc        [rcx]
        pop         rcx
#ifdef TARGET_AMD64
        mov         rax, <pJitHelper>
        jmp         rax
#else
        jmp         <pJitHelper>
#endif
*/

    // push     rcx
    // mov      rcx, &(pHelperFuncCount->count)
    X86EmitPushReg(kECX);
    X86EmitRegLoad(kECX, (UINT_PTR)(&(pHelperFuncCount->count)));

    // lock inc [rcx]
    BYTE lock_inc_RCX[] = { 0xf0, 0xff, 0x01 };
    EmitBytes(lock_inc_RCX, sizeof(lock_inc_RCX));

#if defined(TARGET_AMD64)
    // mov      rax, <pJitHelper>
    // pop      rcx
    // jmp      rax
#else
    // pop      rcx
    // jmp      <pJitHelper>
#endif
    X86EmitTailcallWithSinglePop(NewExternalCodeLabel(pJitHelper), kECX);
}
#endif // _DEBUG && !TARGET_UNIX

VOID StubLinkerCPU::X86EmitCurrentThreadFetch(X86Reg dstreg, unsigned preservedRegSet)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        // It doesn't make sense to have the destination register be preserved
        PRECONDITION((preservedRegSet & (1 << dstreg)) == 0);
        AMD64_ONLY(PRECONDITION(dstreg < 8)); // code below doesn't support high registers
    }
    CONTRACTL_END;

#ifdef TARGET_UNIX

    X86EmitPushRegs(preservedRegSet & ((1 << kEAX) | (1 << kEDX) | (1 << kECX)));

    // call GetThread
    X86EmitCall(NewExternalCodeLabel((LPVOID)GetThreadHelper), sizeof(void*));

    // mov dstreg, eax
    X86EmitMovRegReg(dstreg, kEAX);

    X86EmitPopRegs(preservedRegSet & ((1 << kEAX) | (1 << kEDX) | (1 << kECX)));

#ifdef _DEBUG
    // Trash caller saved regs that we were not told to preserve, and that aren't the dstreg.
    preservedRegSet |= 1 << dstreg;
    if (!(preservedRegSet & (1 << kEAX)))
        X86EmitDebugTrashReg(kEAX);
    if (!(preservedRegSet & (1 << kEDX)))
        X86EmitDebugTrashReg(kEDX);
    if (!(preservedRegSet & (1 << kECX)))
        X86EmitDebugTrashReg(kECX);
#endif // _DEBUG

#else // TARGET_UNIX

#ifdef TARGET_AMD64
    BYTE code[] = { 0x65,0x48,0x8b,0x04,0x25 };    // mov dstreg, qword ptr gs:[IMM32]
    static const int regByteIndex = 3;
#elif defined(TARGET_X86)
    BYTE code[] = { 0x64,0x8b,0x05 };              // mov dstreg, dword ptr fs:[IMM32]
    static const int regByteIndex = 2;
#endif
    code[regByteIndex] |= (dstreg << 3);

    EmitBytes(code, sizeof(code));
    Emit32(offsetof(TEB, ThreadLocalStoragePointer));

    X86EmitIndexRegLoad(dstreg, dstreg, sizeof(void *) * _tls_index);

    X86EmitIndexRegLoad(dstreg, dstreg, (int)Thread::GetOffsetOfThreadStatic(&gCurrentThreadInfo));

#endif // TARGET_UNIX
}

#ifdef TARGET_UNIX
namespace
{
    gc_alloc_context* STDCALL GetAllocContextHelper()
    {
        return &t_runtime_thread_locals.alloc_context;
    }
}
#endif

VOID StubLinkerCPU::X86EmitCurrentThreadAllocContextFetch(X86Reg dstreg, unsigned preservedRegSet)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        // It doesn't make sense to have the destination register be preserved
        PRECONDITION((preservedRegSet & (1 << dstreg)) == 0);
        AMD64_ONLY(PRECONDITION(dstreg < 8)); // code below doesn't support high registers
    }
    CONTRACTL_END;

#ifdef TARGET_UNIX

    X86EmitPushRegs(preservedRegSet & ((1 << kEAX) | (1 << kEDX) | (1 << kECX)));

    // call GetThread
    X86EmitCall(NewExternalCodeLabel((LPVOID)GetAllocContextHelper), sizeof(void*));

    // mov dstreg, eax
    X86EmitMovRegReg(dstreg, kEAX);

    X86EmitPopRegs(preservedRegSet & ((1 << kEAX) | (1 << kEDX) | (1 << kECX)));

#ifdef _DEBUG
    // Trash caller saved regs that we were not told to preserve, and that aren't the dstreg.
    preservedRegSet |= 1 << dstreg;
    if (!(preservedRegSet & (1 << kEAX)))
        X86EmitDebugTrashReg(kEAX);
    if (!(preservedRegSet & (1 << kEDX)))
        X86EmitDebugTrashReg(kEDX);
    if (!(preservedRegSet & (1 << kECX)))
        X86EmitDebugTrashReg(kECX);
#endif // _DEBUG

#else // TARGET_UNIX

#ifdef TARGET_AMD64
    BYTE code[] = { 0x65,0x48,0x8b,0x04,0x25 };    // mov dstreg, qword ptr gs:[IMM32]
    static const int regByteIndex = 3;
#elif defined(TARGET_X86)
    BYTE code[] = { 0x64,0x8b,0x05 };              // mov dstreg, dword ptr fs:[IMM32]
    static const int regByteIndex = 2;
#endif
    code[regByteIndex] |= (dstreg << 3);

    EmitBytes(code, sizeof(code));
    Emit32(offsetof(TEB, ThreadLocalStoragePointer));

    X86EmitIndexRegLoad(dstreg, dstreg, sizeof(void *) * _tls_index);

    _ASSERTE(Thread::GetOffsetOfThreadStatic(&t_runtime_thread_locals.alloc_context) < INT_MAX);
    X86EmitAddReg(dstreg, (int32_t)Thread::GetOffsetOfThreadStatic(&t_runtime_thread_locals.alloc_context));

#endif // TARGET_UNIX
}

#if defined(FEATURE_COMINTEROP) && defined(TARGET_X86)

#if defined(PROFILING_SUPPORTED)
VOID StubLinkerCPU::EmitProfilerComCallProlog(TADDR pFrameVptr, X86Reg regFrame)
{
    STANDARD_VM_CONTRACT;

    // Load the methoddesc into ECX (Frame->m_pvDatum->m_pMD)
    X86EmitIndexRegLoad(kECX, regFrame, ComMethodFrame::GetOffsetOfDatum());
    X86EmitIndexRegLoad(kECX, kECX, ComCallMethodDesc::GetOffsetOfMethodDesc());

    // Push arguments and notify profiler
    X86EmitPushImm32(COR_PRF_TRANSITION_CALL);      // Reason
    X86EmitPushReg(kECX);                           // MethodDesc*
    X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerUnmanagedToManagedTransitionMD), 2*sizeof(void*));
}


VOID StubLinkerCPU::EmitProfilerComCallEpilog(TADDR pFrameVptr, X86Reg regFrame)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pFrameVptr == ComMethodFrame::GetMethodFrameVPtr());
    }
    CONTRACTL_END;

    // Load the methoddesc into ECX (Frame->m_pvDatum->m_pMD)
    X86EmitIndexRegLoad(kECX, regFrame, ComMethodFrame::GetOffsetOfDatum());
    X86EmitIndexRegLoad(kECX, kECX, ComCallMethodDesc::GetOffsetOfMethodDesc());

    // Push arguments and notify profiler
    X86EmitPushImm32(COR_PRF_TRANSITION_RETURN);    // Reason
    X86EmitPushReg(kECX);                           // MethodDesc*
    X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerManagedToUnmanagedTransitionMD), 2*sizeof(void*));
}
#endif // PROFILING_SUPPORTED

//========================================================================
//  Prolog for entering managed code from COM
//  pushes the appropriate frame ptr
//  sets up a thread and returns a label that needs to be emitted by the caller
//  At the end:
//  ESI will hold the pointer to the ComMethodFrame or UMThkCallFrame
//  EBX will hold the result of GetThread()
//  EDI will hold the previous Frame ptr

void StubLinkerCPU::EmitComMethodStubProlog(TADDR pFrameVptr,
                                            CodeLabel** rgRareLabels,
                                            CodeLabel** rgRejoinLabels,
                                            BOOL bShouldProfile)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(rgRareLabels != NULL);
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL);
    }
    CONTRACTL_END;

    // push ebp     ;; save callee-saved register
    // push ebx     ;; save callee-saved register
    // push esi     ;; save callee-saved register
    // push edi     ;; save callee-saved register
    X86EmitPushEBPframe();

    X86EmitPushReg(kEBX);
    X86EmitPushReg(kESI);
    X86EmitPushReg(kEDI);

    // push eax ; datum
    X86EmitPushReg(kEAX);

    // push edx ;leave room for m_next (edx is an arbitrary choice)
    X86EmitPushReg(kEDX);

    // push IMM32 ; push Frame vptr
    X86EmitPushImmPtr((LPVOID) pFrameVptr);

    X86EmitPushImmPtr((LPVOID)GetProcessGSCookie());

    // lea esi, [esp+4] ;; set ESI -> new frame
    X86EmitEspOffset(0x8d, kESI, 4);    // lea ESI, [ESP+4]

    // Emit Setup thread
    EmitSetup(rgRareLabels[0]);  // rareLabel for rare setup
    EmitLabel(rgRejoinLabels[0]); // rejoin label for rare setup

#ifdef PROFILING_SUPPORTED
    // If profiling is active, emit code to notify profiler of transition
    // Must do this before preemptive GC is disabled, so no problem if the
    // profiler blocks.
    if (CORProfilerTrackTransitions() && bShouldProfile)
    {
        EmitProfilerComCallProlog(pFrameVptr, /*Frame*/ kESI);
    }
#endif // PROFILING_SUPPORTED

    //-----------------------------------------------------------------------
    // Generate the inline part of disabling preemptive GC.  It is critical
    // that this part happen before we link in the frame.  That's because
    // we won't be able to unlink the frame from preemptive mode.  And during
    // shutdown, we cannot switch to cooperative mode under some circumstances
    //-----------------------------------------------------------------------
    EmitDisable(rgRareLabels[1], /*fCallIn=*/TRUE, kEBX); // rare disable gc
    EmitLabel(rgRejoinLabels[1]);                         // rejoin for rare disable gc

    // If we take an SO after installing the new frame but before getting the exception
    // handlers in place, we will have a corrupt frame stack.  So probe-by-touch first for
    // sufficient stack space to erect the handler.  Because we know we will be touching
    // that stack right away when install the handler, this probe-by-touch will not incur
    // unnecessary cache misses.   And this allows us to do the probe with one instruction.

    // Note that for Win64, the personality routine will handle unlinking the frame, so
    // we don't need to probe in the Win64 stubs.  The exception is ComToCLRWorker
    // where we don't setup a personality routine.  However, we push the frame inside
    // that function and it is probe-protected with an entry point probe first, so we are
    // OK there too.

    // We push two registers to setup the EH handler and none to setup the frame
    // so probe for double that to give ourselves a small margin for error.
    // mov eax, [esp+n] ;; probe for sufficient stack to setup EH
    X86EmitEspOffset(0x8B, kEAX, -0x20);
     // mov edi,[ebx + Thread.GetFrame()]  ;; get previous frame
    X86EmitIndexRegLoad(kEDI, kEBX, Thread::GetOffsetOfCurrentFrame());

    // mov [esi + Frame.m_next], edi
    X86EmitIndexRegStore(kESI, Frame::GetOffsetOfNextLink(), kEDI);

    // mov [ebx + Thread.GetFrame()], esi
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kESI);

#if _DEBUG
    if (Frame::ShouldLogTransitions())
    {
        // call LogTransition
        X86EmitPushReg(kESI);
        X86EmitCall(NewExternalCodeLabel((LPVOID) Frame::LogTransition), sizeof(void*));
    }
#endif
}

//========================================================================
//  Epilog for stubs that enter managed code from COM
//
//  At this point of the stub, the state should be as follows:
//  ESI holds the ComMethodFrame or UMThkCallFrame ptr
//  EBX holds the result of GetThread()
//  EDI holds the previous Frame ptr
//
void StubLinkerCPU::EmitComMethodStubEpilog(TADDR pFrameVptr,
                                            CodeLabel** rgRareLabels,
                                            CodeLabel** rgRejoinLabels,
                                            BOOL bShouldProfile)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(rgRareLabels != NULL);
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL);
        PRECONDITION(4 == sizeof( ((Thread*)0)->m_State ));
        PRECONDITION(4 == sizeof( ((Thread*)0)->m_fPreemptiveGCDisabled ));
    }
    CONTRACTL_END;

    EmitCheckGSCookie(kESI, UnmanagedToManagedFrame::GetOffsetOfGSCookie());

    // mov [ebx + Thread.GetFrame()], edi  ;; restore previous frame
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kEDI);

    // move byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],0
    X86EmitOffsetModRM(0xc6, (X86Reg)0, kEBX, Thread::GetOffsetOfGCFlag());
    Emit8(0);

    // add esp, popstack
    X86EmitAddEsp(sizeof(GSCookie) + UnmanagedToManagedFrame::GetOffsetOfCalleeSavedRegisters());

    // pop edi        ; restore callee-saved registers
    // pop esi
    // pop ebx
    // pop ebp
    X86EmitPopReg(kEDI);
    X86EmitPopReg(kESI);
    X86EmitPopReg(kEBX);
    X86EmitPopReg(kEBP);

    //    jmp eax //reexecute!
    X86EmitR2ROp(0xff, (X86Reg)4, kEAX);

    // ret
    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack. A "ret" instruction
    // indicates that no more code needs to be disassembled, if the stack-walker
    // keeps on going past the previous "jmp eax".
    X86EmitReturn(0);

    //-----------------------------------------------------------------------
    // The out-of-line portion of disabling preemptive GC - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[1]);  // label for rare disable gc
    EmitRareDisable(rgRejoinLabels[1]); // emit rare disable gc

    //-----------------------------------------------------------------------
    // The out-of-line portion of setup thread - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[0]);  // label for rare setup thread
    EmitRareSetup(rgRejoinLabels[0], /*fThrow*/ TRUE); // emit rare setup thread
}

//---------------------------------------------------------------
// Emit code to store the setup current Thread structure in eax.
// TRASHES  eax,ecx&edx.
// RESULTS  ebx = current Thread
//---------------------------------------------------------------
VOID StubLinkerCPU::EmitSetup(CodeLabel *pForwardRef)
{
    STANDARD_VM_CONTRACT;

    X86EmitCurrentThreadFetch(kEBX, 0);

    // cmp ebx, 0
    static const BYTE b[] = { 0x83, 0xFB, 0x0};

    EmitBytes(b, sizeof(b));

    // jz RarePath
    X86EmitCondJump(pForwardRef, X86CondCode::kJZ);

#ifdef _DEBUG
    X86EmitDebugTrashReg(kECX);
    X86EmitDebugTrashReg(kEDX);
#endif
}

VOID StubLinkerCPU::EmitRareSetup(CodeLabel *pRejoinPoint, BOOL fThrow)
{
    STANDARD_VM_CONTRACT;

    if (!fThrow)
    {
        X86EmitPushReg(kESI);
        X86EmitCall(NewExternalCodeLabel((LPVOID) CreateThreadBlockReturnHr), sizeof(void*));
    }
    else
    {
        X86EmitCall(NewExternalCodeLabel((LPVOID) CreateThreadBlockThrow), 0);
    }

    // mov ebx,eax
    Emit16(0xc389);
    X86EmitNearJump(pRejoinPoint);
}

//========================================================================
//  Epilog for stubs that enter managed code from COM
//
//  On entry, ESI points to the Frame
//  ESP points to below FramedMethodFrame::m_vc5Frame
//  EBX hold GetThread()
//  EDI holds the previous Frame

void StubLinkerCPU::EmitSharedComMethodStubEpilog(TADDR pFrameVptr,
                                                  CodeLabel** rgRareLabels,
                                                  CodeLabel** rgRejoinLabels,
                                                  unsigned offsetRetThunk,
                                                  BOOL bShouldProfile)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(rgRareLabels != NULL);
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL);
        PRECONDITION(4 == sizeof( ((Thread*)0)->m_State ));
        PRECONDITION(4 == sizeof( ((Thread*)0)->m_fPreemptiveGCDisabled ));
    }
    CONTRACTL_END;

    CodeLabel *NoEntryLabel;
    NoEntryLabel = NewCodeLabel();

    EmitCheckGSCookie(kESI, UnmanagedToManagedFrame::GetOffsetOfGSCookie());

    // mov [ebx + Thread.GetFrame()], edi  ;; restore previous frame
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kEDI);

    //-----------------------------------------------------------------------
    // Generate enabling preemptive GC
    //-----------------------------------------------------------------------
    EmitLabel(NoEntryLabel);    // need to enable preemp mode even when we fail the disable as rare disable will return in coop mode

    // move byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],0
    X86EmitOffsetModRM(0xc6, (X86Reg)0, kEBX, Thread::GetOffsetOfGCFlag());
    Emit8(0);

#ifdef PROFILING_SUPPORTED
    // If profiling is active, emit code to notify profiler of transition
    if (CORProfilerTrackTransitions() && bShouldProfile)
    {
        // Save return value
        X86EmitPushReg(kEAX);
        X86EmitPushReg(kEDX);

        EmitProfilerComCallEpilog(pFrameVptr, kESI);

        // Restore return value
        X86EmitPopReg(kEDX);
        X86EmitPopReg(kEAX);
    }
#endif // PROFILING_SUPPORTED

    X86EmitAddEsp(sizeof(GSCookie) + UnmanagedToManagedFrame::GetOffsetOfDatum());

    // pop ecx
    X86EmitPopReg(kECX); // pop the MethodDesc*

    // pop edi        ; restore callee-saved registers
    // pop esi
    // pop ebx
    // pop ebp
    X86EmitPopReg(kEDI);
    X86EmitPopReg(kESI);
    X86EmitPopReg(kEBX);
    X86EmitPopReg(kEBP);

    // add ecx, offsetRetThunk
    X86EmitAddReg(kECX, offsetRetThunk);

    // jmp ecx
    // This will jump to the "ret cbStackArgs" instruction in COMMETHOD_PREPAD.
    static const BYTE bjmpecx[] = { 0xff, 0xe1 };
    EmitBytes(bjmpecx, sizeof(bjmpecx));

    // ret
    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack. A "ret" instruction
    // indicates that no more code needs to be disassembled, if the stack-walker
    // keeps on going past the previous "jmp ecx".
    X86EmitReturn(0);

    //-----------------------------------------------------------------------
    // The out-of-line portion of disabling preemptive GC - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[1]);  // label for rare disable gc
    EmitRareDisableHRESULT(rgRejoinLabels[1], NoEntryLabel);

    //-----------------------------------------------------------------------
    // The out-of-line portion of setup thread - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[0]);  // label for rare setup thread
    EmitRareSetup(rgRejoinLabels[0],/*fThrow*/ FALSE); // emit rare setup thread
}

#endif // defined(FEATURE_COMINTEROP) && defined(TARGET_X86)


#if !defined(FEATURE_STUBS_AS_IL) && defined(TARGET_X86)
VOID StubLinkerCPU::EmitCheckGSCookie(X86Reg frameReg, int gsCookieOffset)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    // cmp dword ptr[frameReg-gsCookieOffset], gsCookie
    X86EmitCmpRegIndexImm32(frameReg, gsCookieOffset, GetProcessGSCookie());

    CodeLabel * pLabel = NewCodeLabel();
    X86EmitCondJump(pLabel, X86CondCode::kJE);

    X86EmitCall(NewExternalCodeLabel((LPVOID) JIT_FailFast), 0);

    EmitLabel(pLabel);
#endif
}
#endif // !defined(FEATURE_STUBS_AS_IL) && defined(TARGET_X86)

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
    if (pMD->HasStableEntryPoint())
    {
        X86EmitRegLoad(kRAX, pMD->GetStableEntryPoint());// MOV RAX, DWORD
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
#ifdef TARGET_AMD64
    EmitLoadMethodAddressIntoAX(pMD);
    Emit16(X86_INSTR_JMP_EAX);
#else
    // Use direct call if possible
    if (pMD->HasStableEntryPoint())
    {
        X86EmitNearJump(NewExternalCodeLabel((LPVOID) pMD->GetStableEntryPoint()));
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


#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)

typedef BOOL GetModuleInformationProc(
  HANDLE hProcess,
  HMODULE hModule,
  LPMODULEINFO lpmodinfo,
  DWORD cb
);

GetModuleInformationProc *g_pfnGetModuleInformation = NULL;

extern "C" VOID __cdecl DebugCheckStubUnwindInfoWorker (CONTEXT *pStubContext)
{
    LOG((LF_STUBS, LL_INFO1000000, "checking stub unwind info:\n"));

    //
    // Make a copy of the CONTEXT.  RtlVirtualUnwind will modify this copy.
    // DebugCheckStubUnwindInfo will need to restore registers from the
    // original CONTEXT.
    //
    CONTEXT ctx = *pStubContext;
    ctx.ContextFlags = (CONTEXT_CONTROL | CONTEXT_INTEGER);

    //
    // Find the upper bound of the stack and address range of KERNEL32.  This
    // is where we expect the unwind to stop.
    //
    void *pvStackTop = GetThread()->GetCachedStackBase();

    if (!g_pfnGetModuleInformation)
    {
        HMODULE hmodPSAPI = GetModuleHandle(W("PSAPI.DLL"));

        if (!hmodPSAPI)
        {
            hmodPSAPI = WszLoadLibrary(W("PSAPI.DLL"));
            if (!hmodPSAPI)
            {
                _ASSERTE(!"unable to load PSAPI.DLL");
                goto ErrExit;
            }
        }

        g_pfnGetModuleInformation = (GetModuleInformationProc*)GetProcAddress(hmodPSAPI, "GetModuleInformation");
        if (!g_pfnGetModuleInformation)
        {
            _ASSERTE(!"can't find PSAPI!GetModuleInformation");
            goto ErrExit;
        }

        // Intentionally leak hmodPSAPI.  We don't want to
        // LoadLibrary/FreeLibrary every time, this is slow + produces lots of
        // debugger spew.  This is just debugging code after all...
    }

    HMODULE hmodKERNEL32 = GetModuleHandle(W("KERNEL32"));
    _ASSERTE(hmodKERNEL32);

    MODULEINFO modinfoKERNEL32;
    if (!g_pfnGetModuleInformation(GetCurrentProcess(), hmodKERNEL32, &modinfoKERNEL32, sizeof(modinfoKERNEL32)))
    {
        _ASSERTE(!"unable to get bounds of KERNEL32");
        goto ErrExit;
    }

    //
    // Unwind until IP is 0, sp is at the stack top, and callee IP is in kernel32.
    //

    for (;;)
    {
        ULONG64 ControlPc = (ULONG64)GetIP(&ctx);

        LOG((LF_STUBS, LL_INFO1000000, "pc %p, sp %p\n", ControlPc, GetSP(&ctx)));

        ULONG64 ImageBase;
        T_RUNTIME_FUNCTION *pFunctionEntry = RtlLookupFunctionEntry(
                ControlPc,
                &ImageBase,
                NULL);
        if (pFunctionEntry)
        {
            PVOID HandlerData;
            ULONG64 EstablisherFrame;

            RtlVirtualUnwind(
                    0,
                    ImageBase,
                    ControlPc,
                    pFunctionEntry,
                    &ctx,
                    &HandlerData,
                    &EstablisherFrame,
                    NULL);

            ULONG64 NewControlPc = (ULONG64)GetIP(&ctx);

            LOG((LF_STUBS, LL_INFO1000000, "function %p, image %p, new pc %p, new sp %p\n", pFunctionEntry, ImageBase, NewControlPc, GetSP(&ctx)));

            if (!NewControlPc)
            {
                if (dac_cast<PTR_BYTE>(GetSP(&ctx)) < (BYTE*)pvStackTop - 0x100)
                {
                    _ASSERTE(!"SP did not end up at top of stack");
                    goto ErrExit;
                }

                if (!(   ControlPc > (ULONG64)modinfoKERNEL32.lpBaseOfDll
                      && ControlPc < (ULONG64)modinfoKERNEL32.lpBaseOfDll + modinfoKERNEL32.SizeOfImage))
                {
                    _ASSERTE(!"PC did not end up in KERNEL32");
                    goto ErrExit;
                }

                break;
            }
        }
        else
        {
            // Nested functions that do not use any stack space or nonvolatile
            // registers are not required to have unwind info (ex.
            // USER32!ZwUserCreateWindowEx).
            ctx.Rip = *(ULONG64*)(ctx.Rsp);
            ctx.Rsp += sizeof(ULONG64);
        }
    }
ErrExit:
    return;
}

//virtual
VOID StubLinkerCPU::EmitUnwindInfoCheckWorker (CodeLabel *pCheckLabel)
{
    STANDARD_VM_CONTRACT;
    X86EmitCall(pCheckLabel, 0);
}

//virtual
VOID StubLinkerCPU::EmitUnwindInfoCheckSubfunction()
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_AMD64
    // X86EmitCall will generate "mov rax, target/jmp rax", so we have to save
    // rax on the stack.  DO NOT use X86EmitPushReg.  That will induce infinite
    // recursion, since the push may require more unwind info.  This "push rax"
    // will be accounted for by DebugCheckStubUnwindInfo's unwind info
    // (considered part of its locals), so there doesn't have to be unwind
    // info for it.
    Emit8(0x50);
#endif

    X86EmitNearJump(NewExternalCodeLabel(DebugCheckStubUnwindInfo));
}

#endif // defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)


#if defined(FEATURE_COMINTEROP) && defined(TARGET_X86)

//-----------------------------------------------------------------------
// Generates the inline portion of the code to disable preemptive GC. Hopefully,
// the inline code is all that will execute most of the time. If this code
// path is entered at certain times, however, it will need to jump out to
// a separate out-of-line path which is more expensive. The "pForwardRef"
// label indicates the start of the out-of-line path.
//
// Assumptions:
//      ebx = Thread
// Preserves
//      all registers except ecx.
//
//-----------------------------------------------------------------------
VOID StubLinkerCPU::EmitDisable(CodeLabel *pForwardRef, BOOL fCallIn, X86Reg ThreadReg)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(4 == sizeof( ((Thread*)0)->m_fPreemptiveGCDisabled ));
        PRECONDITION(4 == sizeof(g_TrapReturningThreads));
    }
    CONTRACTL_END;

    // move byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],1
    X86EmitOffsetModRM(0xc6, (X86Reg)0, ThreadReg, Thread::GetOffsetOfGCFlag());
    Emit8(1);

    // cmp dword ptr g_TrapReturningThreads, 0
    Emit16(0x3d83);
    EmitPtr((void *)&g_TrapReturningThreads);
    Emit8(0);

    // jnz RarePath
    X86EmitCondJump(pForwardRef, X86CondCode::kJNZ);

#ifdef _DEBUG
    if (ThreadReg != kECX)
        X86EmitDebugTrashReg(kECX);
#endif

}


//-----------------------------------------------------------------------
// Generates the out-of-line portion of the code to disable preemptive GC.
// After the work is done, the code jumps back to the "pRejoinPoint"
// which should be emitted right after the inline part is generated.  However,
// if we cannot execute managed code at this time, an exception is thrown
// which cannot be caught by managed code.
//
// Assumptions:
//      ebx = Thread
// Preserves
//      all registers except ecx, eax.
//
//-----------------------------------------------------------------------
VOID StubLinkerCPU::EmitRareDisable(CodeLabel *pRejoinPoint)
{
    STANDARD_VM_CONTRACT;

    X86EmitCall(NewExternalCodeLabel((LPVOID) StubRareDisableTHROW), 0);

#ifdef _DEBUG
    X86EmitDebugTrashReg(kECX);
#endif
    X86EmitNearJump(pRejoinPoint);
}

//-----------------------------------------------------------------------
// Generates the out-of-line portion of the code to disable preemptive GC.
// After the work is done, the code normally jumps back to the "pRejoinPoint"
// which should be emitted right after the inline part is generated.  However,
// if we cannot execute managed code at this time, an HRESULT is returned
// via the ExitPoint.
//
// Assumptions:
//      ebx = Thread
// Preserves
//      all registers except ecx, eax.
//
//-----------------------------------------------------------------------
VOID StubLinkerCPU::EmitRareDisableHRESULT(CodeLabel *pRejoinPoint, CodeLabel *pExitPoint)
{
    STANDARD_VM_CONTRACT;

    X86EmitCall(NewExternalCodeLabel((LPVOID) StubRareDisableHR), 0);

#ifdef _DEBUG
    X86EmitDebugTrashReg(kECX);
#endif

    // test eax, eax  ;; test the result of StubRareDisableHR
    Emit16(0xc085);

    // JZ pRejoinPoint
    X86EmitCondJump(pRejoinPoint, X86CondCode::kJZ);

    X86EmitNearJump(pExitPoint);
}

#endif // defined(FEATURE_COMINTEROP) && defined(TARGET_X86)


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
    X86EmitR2ROp(0xff, (X86Reg)4, kR10);

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


#if !defined(FEATURE_STUBS_AS_IL)
//===========================================================================
// Emits code to break into debugger
VOID StubLinkerCPU::EmitDebugBreak()
{
    STANDARD_VM_CONTRACT;

    // int3
    Emit8(0xCC);
}

#if defined(FEATURE_COMINTEROP) && defined(TARGET_X86)

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning (disable : 4740) // There is inline asm code in this function, which disables
                                 // global optimizations.
#pragma warning (disable : 4731)
#endif  // _MSC_VER
ThreadPointer __stdcall CreateThreadBlockReturnHr(ComMethodFrame *pFrame)
{

    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;

    // This means that a thread is FIRST coming in from outside the EE.
    Thread* pThread = SetupThreadNoThrow(&hr);

    if (pThread == NULL) {
        // Unwind stack, and return hr
        // NOTE: assumes __stdcall
        // Note that this code does not handle the rare COM signatures that do not return HRESULT
        // compute the callee pop stack bytes
        UINT numArgStackBytes = pFrame->GetNumCallerStackBytes();
        unsigned frameSize = sizeof(Frame) + sizeof(LPVOID);
        LPBYTE iEsp = ((LPBYTE)pFrame) + ComMethodFrame::GetOffsetOfCalleeSavedRegisters();

        // Let ASAN that we aren't going to return so it can do some cleanup
        __asan_handle_no_return();

        __asm
        {
            mov eax, hr
            mov edx, numArgStackBytes
            //*****************************************
            // reset the stack pointer
            // none of the locals above can be used in the asm below
            // if we wack the stack pointer
            mov esp, iEsp
            // pop callee saved registers
            pop edi
            pop esi
            pop ebx
            pop ebp
            pop ecx         ; //return address
            // pop the callee cleanup stack args
            add esp, edx    ;// callee cleanup of args
            jmp ecx;        // jump to the address to continue execution

            // We will never get here. This "ret" is just so that code-disassembling
            // profilers know to stop disassembling any further
            ret
        }
    }

    return pThread;
}
#if defined(_MSC_VER)
#pragma warning(pop)
#endif

#endif // FEATURE_COMINTEROP && TARGET_X86

#endif // !FEATURE_STUBS_AS_IL

#endif // !DACCESS_COMPILE

#ifdef HAS_THISPTR_RETBUF_PRECODE

// rel32 jmp target that points back to the jump (infinite loop).
// Used to mark uninitialized ThisPtrRetBufPrecode target
#define REL32_JMP_SELF (-5)

#ifndef DACCESS_COMPILE
void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    IN_TARGET_64BIT(m_nop1 = X86_INSTR_NOP;)   // nop
#ifdef UNIX_AMD64_ABI
    m_prefix1 = 0x48;
    m_movScratchArg0 = 0xC78B;          // mov rax,rdi
    m_prefix2 = 0x48;
    m_movArg0Arg1 = 0xFE8B;             // mov rdi,rsi
    m_prefix3 = 0x48;
    m_movArg1Scratch = 0xF08B;          // mov rsi,rax
#else
    IN_TARGET_64BIT(m_prefix1 = 0x48;)
    m_movScratchArg0 = 0xC889;          // mov r/eax,r/ecx
    IN_TARGET_64BIT(m_prefix2 = 0x48;)
    m_movArg0Arg1 = 0xD189;             // mov r/ecx,r/edx
    IN_TARGET_64BIT(m_prefix3 = 0x48;)
    m_movArg1Scratch = 0xC289;          // mov r/edx,r/eax
#endif
    m_nop2 = X86_INSTR_NOP;             // nop
    m_jmp = X86_INSTR_JMP_REL32;        // jmp rel32
    m_pMethodDesc = (TADDR)pMD;

    // This precode is never patched lazily - avoid unnecessary jump stub allocation
    m_rel32 = REL32_JMP_SELF;

    _ASSERTE(*((BYTE*)this + OFFSETOF_PRECODE_TYPE) == ThisPtrRetBufPrecode::Type);
}

IN_TARGET_32BIT(static_assert_no_msg(offsetof(ThisPtrRetBufPrecode, m_movScratchArg0) == OFFSETOF_PRECODE_TYPE);)

BOOL ThisPtrRetBufPrecode::SetTargetInterlocked(TADDR target, TADDR expected)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // This precode is never patched lazily - the interlocked semantics is not required.
    _ASSERTE(m_rel32 == REL32_JMP_SELF);

    // Use pMD == NULL to allocate the jump stub in non-dynamic heap that has the same lifetime as the precode itself
    INT32 newRel32 = rel32UsingJumpStub(&m_rel32, target, NULL /* pMD */, ((MethodDesc *)GetMethodDesc())->GetLoaderAllocator());

    _ASSERTE(IS_ALIGNED(&m_rel32, sizeof(INT32)));
    ExecutableWriterHolder<INT32> rel32WriterHolder(&m_rel32, sizeof(INT32));
    InterlockedExchange((LONG*)rel32WriterHolder.GetRW(), (LONG)newRel32);

    return TRUE;
}
#endif // !DACCESS_COMPILE

PCODE ThisPtrRetBufPrecode::GetTarget()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // This precode is never patched lazily - pretend that the uninitialized m_rel32 points to prestub
    if (m_rel32 == REL32_JMP_SELF)
        return GetPreStubEntryPoint();

    return rel32Decode(PTR_HOST_MEMBER_TADDR(ThisPtrRetBufPrecode, this, m_rel32));
}

#endif // HAS_THISPTR_RETBUF_PRECODE
