//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


// NOTE on Frame Size C_ASSERT usage in this file 
// if the frame size changes then the stubs have to be revisited for correctness
// kindly revist the logic and then update the constants so that the C_ASSERT will again fire
// if someone changes the frame size.  You are expected to keep this hard coded constant
// up to date so that changes in the frame size trigger errors at compile time if the code is not altered

// Precompiled Header

#include "common.h"

#include "field.h"
#include "stublink.h"

#include "tls.h"
#include "frames.h"
#include "excep.h"
#include "dllimport.h"
#include "log.h"
#include "security.h"
#include "comdelegate.h"
#include "array.h"
#include "jitinterface.h"
#include "codeman.h"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif
#include "dbginterface.h"
#include "eeprofinterfaces.h"
#include "eeconfig.h"
#include "securitydeclarative.h"
#ifdef _TARGET_X86_
#include "asmconstants.h"
#endif // _TARGET_X86_
#include "class.h"
#include "stublink.inl"

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "comcache.h"
#include "olevariant.h"
#include "notifyexternals.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
#include <psapi.h>
#endif


#ifndef DACCESS_COMPILE

extern "C" VOID __cdecl StubRareEnable(Thread *pThread);
#ifdef FEATURE_COMINTEROP
extern "C" HRESULT __cdecl StubRareDisableHR(Thread *pThread);
#endif // FEATURE_COMINTEROP
extern "C" VOID __cdecl StubRareDisableTHROW(Thread *pThread, Frame *pFrame);

extern "C" VOID __cdecl ArrayOpStubNullException(void);
extern "C" VOID __cdecl ArrayOpStubRangeException(void);
extern "C" VOID __cdecl ArrayOpStubTypeMismatchException(void);

#if defined(_TARGET_AMD64_)
#define EXCEPTION_HELPERS(base) \
    extern "C" VOID __cdecl base##_RSIRDI_ScratchArea(void); \
    extern "C" VOID __cdecl base##_ScratchArea(void); \
    extern "C" VOID __cdecl base##_RSIRDI(void); \
    extern "C" VOID __cdecl base(void)
EXCEPTION_HELPERS(ArrayOpStubNullException);
EXCEPTION_HELPERS(ArrayOpStubRangeException);
EXCEPTION_HELPERS(ArrayOpStubTypeMismatchException);
#undef EXCEPTION_HELPERS

#if defined(_DEBUG) 
extern "C" VOID __cdecl DebugCheckStubUnwindInfo();
#endif
#endif // _TARGET_AMD64_

// Presumably this code knows what it is doing with TLS.  If we are hiding these
// services from normal code, reveal them here.
#ifdef TlsGetValue
#undef TlsGetValue
#endif

#ifdef FEATURE_COMINTEROP
Thread* __stdcall CreateThreadBlockReturnHr(ComMethodFrame *pFrame);
#endif



#ifdef _TARGET_AMD64_

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

#endif // _TARGET_AMD64_

#ifdef _TARGET_AMD64_
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
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
                UINT64 TargetAddress = (INT64)pOutBuffer + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov eax, imm32  ; zero-extended
                pOutBuffer[0] = 0xB8;
                *((UINT32*)&pOutBuffer[1]) = (UINT32)TargetAddress;
            }
            else if (k64 == refsize)
            {
                // mov rax, imm64
                pOutBuffer[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBuffer[1] = 0xB8;
                *((UINT64*)&pOutBuffer[2]) = (UINT64)(((INT64)pOutBuffer) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT
            if (k8 == refsize)
            {
                pOutBuffer[0] = 0xeb;
                *((__int8*)(pOutBuffer+1)) = (__int8)fixedUpReference;
            }
            else if (k32 == refsize)
            {
                pOutBuffer[0] = 0xe9;
                *((__int32*)(pOutBuffer+1)) = (__int32)fixedUpReference;
            }
            else if (k64Small == refsize)
            {
                // REX.W jmp rax
                pOutBuffer[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBuffer[1] = 0xFF;
                pOutBuffer[2] = 0xE0;
            }
            else if (k64 == refsize)
            {
                // REX.W jmp rax
                pOutBuffer[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBuffer[1] = 0xFF;
                pOutBuffer[2] = 0xE0;
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
#ifdef _TARGET_AMD64_
                                          | InstructionFormat::k64Small | InstructionFormat::k64
#endif // _TARGET_AMD64_
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
#ifdef _TARGET_AMD64_
                case k64Small:
                    return 5 + 2;

                case k64:
                    return 12;
#endif // _TARGET_AMD64_
                default:
                    _ASSERTE(!"unexpected refsize");
                    return 0;

            }
        }

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT
            if (k8 == refsize)
            {
                pOutBuffer[0] = 0xeb;
                *((__int8*)(pOutBuffer+1)) = (__int8)fixedUpReference;
            }
            else if (k32 == refsize)
            {
                pOutBuffer[0] = 0xe9;
                *((__int32*)(pOutBuffer+1)) = (__int32)fixedUpReference;
            }
#ifdef _TARGET_AMD64_
            else if (k64Small == refsize)
            {
                UINT64 TargetAddress = (INT64)pOutBuffer + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov eax, imm32  ; zero-extended
                pOutBuffer[0] = 0xB8;
                *((UINT32*)&pOutBuffer[1]) = (UINT32)TargetAddress;

                // jmp rax
                pOutBuffer[5] = 0xFF;
                pOutBuffer[6] = 0xE0;
            }
            else if (k64 == refsize)
            {
                // mov rax, imm64
                pOutBuffer[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBuffer[1] = 0xB8;
                *((UINT64*)&pOutBuffer[2]) = (UINT64)(((INT64)pOutBuffer) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

                // jmp rax
                pOutBuffer[10] = 0xFF;
                pOutBuffer[11] = 0xE0;
            }
#endif // _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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
#ifdef _TARGET_AMD64_
                    return FitsInI4(offset);
#else
                    return TRUE;
#endif

#ifdef _TARGET_AMD64_
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
        LIMITED_METHOD_CONTRACT
        if (refsize == k8)
        {
                pOutBuffer[0] = static_cast<BYTE>(0x70 | variationCode);
                *((__int8*)(pOutBuffer+1)) = (__int8)fixedUpReference;
        }
        else
        {
                pOutBuffer[0] = 0x0f;
                pOutBuffer[1] = static_cast<BYTE>(0x80 | variationCode);
                *((__int32*)(pOutBuffer+2)) = (__int32)fixedUpReference;
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
#ifdef _TARGET_AMD64_
                                | InstructionFormat::k64Small | InstructionFormat::k64
#endif // _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
            case k64Small:
                return 5 + 2;

            case k64:
                return 10 + 2;
#endif // _TARGET_AMD64_

            default:
                _ASSERTE(!"unexpected refsize");
                return 0;
            }
        }

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            switch (refsize)
            {
            case k32:
                pOutBuffer[0] = 0xE8;
                *((__int32*)(1+pOutBuffer)) = (__int32)fixedUpReference;
                break;

#ifdef _TARGET_AMD64_
            case k64Small:
                UINT64 TargetAddress;

                TargetAddress = (INT64)pOutBuffer + fixedUpReference + GetSizeOfInstruction(refsize, variationCode);
                _ASSERTE(FitsInU4(TargetAddress));

                // mov  eax,<fixedUpReference>  ; zero-extends
                pOutBuffer[0] = 0xB8;
                *((UINT32*)&pOutBuffer[1]) = (UINT32)TargetAddress;

                // call rax
                pOutBuffer[5] = 0xff;
                pOutBuffer[6] = 0xd0;
                break;

            case k64:
                // mov  rax,<fixedUpReference>
                pOutBuffer[0] = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
                pOutBuffer[1] = 0xB8;
                *((UINT64*)&pOutBuffer[2]) = (UINT64)(((INT64)pOutBuffer) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

                // call rax
                pOutBuffer[10] = 0xff;
                pOutBuffer[11] = 0xd0;
                break;
#endif // _TARGET_AMD64_

            default:
                _ASSERTE(!"unreached");
                break;
            }
        }

// For x86, the default CanReach implementation will suffice.  It only needs
// to handle k32.
#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT;

            pOutBuffer[0] = 0x68;
            // only support absolute pushimm32 of the label address. The fixedUpReference is
            // the offset to the label from the current point, so add to get address
            *((__int32*)(1+pOutBuffer)) = (__int32)(fixedUpReference);
        }
};

#if defined(_TARGET_AMD64_)
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT;

            X86Reg reg = (X86Reg)variationCode;
            BYTE rex = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;

            if (reg >= kR8)
            {
                rex |= REX_MODRM_REG_EXT;
                reg = X86RegFromAMD64Reg(reg);
            }

            pOutBuffer[0] = rex;
            pOutBuffer[1] = 0x8D;
            pOutBuffer[2] = 0x05 | (reg << 3);
            // only support absolute pushimm32 of the label address. The fixedUpReference is
            // the offset to the label from the current point, so add to get address
            *((__int32*)(3+pOutBuffer)) = (__int32)(fixedUpReference);
        }
};

#endif // _TARGET_AMD64_

#if defined(_TARGET_AMD64_)
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

#if defined(_TARGET_AMD64_)
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

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
    if (reg >= kR8)
    {
        Emit8(REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT | REX_OPCODE_REG_EXT);
        reg = X86RegFromAMD64Reg(reg);
    }
#endif // _TARGET_AMD64_

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
VOID StubLinkerCPU::X86EmitPushImmPtr(LPVOID value WIN64_ARG(X86Reg tmpReg /*=kR10*/))
{
    STANDARD_VM_CONTRACT;

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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
#else // _TARGET_AMD64_
VOID StubLinkerCPU:: X86EmitCmpRegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32)
#endif // _TARGET_AMD64_
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
#if defined(_TARGET_AMD64_)
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

#if defined(_TARGET_AMD64_)
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
#if defined(_TARGET_AMD64_)
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

#if defined(_TARGET_AMD64_)
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
#ifndef _TARGET_AMD64_
    Pop(iArgBytes);
#endif // !_TARGET_AMD64_
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
#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_



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
                                        __int32 ofs)
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
                                         __int32 ofs,
                                         X86Reg srcreg)
{
    STANDARD_VM_CONTRACT;

    if (dstreg != kESP_Unsafe)
        X86EmitOffsetModRM(0x89, srcreg, dstreg, ofs);
    else
        X86EmitOp(0x89, srcreg, (X86Reg)kESP_Unsafe,  ofs);
}

#if defined(_TARGET_AMD64_)
//---------------------------------------------------------------
// Emits:
//    mov [RSP + <ofs>],<srcreg>
//
// It marks the instruction has 64bit so that the processor
// performs a 8byte data move to a RSP based stack location.
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexRegStoreRSP(__int32 ofs,
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
VOID StubLinkerCPU::X86EmitIndexRegStoreR12(__int32 ofs,
                                         X86Reg srcreg)
{
    STANDARD_VM_CONTRACT;

    X86EmitOp(0x89, srcreg, (X86Reg)kR12,  ofs, (X86Reg)0, 0, k64BitOp);
}
#endif // defined(_TARGET_AMD64_)

//---------------------------------------------------------------
// Emits:
//    push dword ptr [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexPush(X86Reg srcreg, __int32 ofs)
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
        __int32 scale,
        __int32 ofs)
{
    STANDARD_VM_CONTRACT;

    X86EmitOffsetModRmSIB(0xff, (X86Reg)0x6, baseReg, indexReg, scale, ofs);
    Push(sizeof(void*));
}

//---------------------------------------------------------------
// Emits:
//    push dword ptr [ESP + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitSPIndexPush(__int32 ofs)
{
    STANDARD_VM_CONTRACT;

    __int8 ofs8 = (__int8) ofs;
    if (ofs == (__int32) ofs8)
    {
        // The offset can be expressed in a byte (can use the byte
        // form of the push esp instruction)

        BYTE code[] = {0xff, 0x74, 0x24, ofs8};
        EmitBytes(code, sizeof(code));   
    }
    else
    {
        // The offset requires 4 bytes (need to use the long form
        // of the push esp instruction)
 
        BYTE code[] = {0xff, 0xb4, 0x24, 0x0, 0x0, 0x0, 0x0};
        *(__int32 *)(&code[3]) = ofs;        
        EmitBytes(code, sizeof(code));
    }
    
    Push(sizeof(void*));
}


//---------------------------------------------------------------
// Emits:
//    pop dword ptr [<srcreg> + <ofs>]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitIndexPop(X86Reg srcreg, __int32 ofs)
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
VOID StubLinkerCPU::X86EmitIndexLea(X86Reg dstreg, X86Reg srcreg, __int32 ofs)
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

#if defined(_TARGET_AMD64_)
VOID StubLinkerCPU::X86EmitIndexLeaRSP(X86Reg dstreg, X86Reg srcreg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;

    X86EmitOp(0x8d, dstreg, (X86Reg)kESP_Unsafe,  ofs, (X86Reg)0, 0, k64BitOp);
}
#endif // defined(_TARGET_AMD64_)

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

#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
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

#if defined(_TARGET_AMD64_)

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
VOID StubLinkerCPU::X64EmitMovdqaFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0x66, 0x6F, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movdqa [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovdqaToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0x66, 0x7F, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movsd XmmN, [baseReg + offset]
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSDFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF2, 0x10, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movsd [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSDToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF2, 0x11, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movss XmmN, [baseReg + offset]
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSSFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF3, 0x10, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// movss [baseReg + offset], XmmN
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovSSToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;
    X64EmitMovXmmWorker(0xF3, 0x11, Xmmreg, baseReg, ofs);
}

//---------------------------------------------------------------
// Helper method for emitting of XMM from/to memory moves
//---------------------------------------------------------------
VOID StubLinkerCPU::X64EmitMovXmmWorker(BYTE prefix, BYTE opcode, X86Reg Xmmreg, X86Reg baseReg, __int32 ofs)
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
        *((__int32*)(codeBuffer+nBytes)) = ofs;
        nBytes += 4;
    }
    
    _ASSERTE(nBytes <= _countof(codeBuffer));
    
    // Lastly, emit the encoded bytes
    EmitBytes(codeBuffer, nBytes);    
}

#endif // defined(_TARGET_AMD64_)

//---------------------------------------------------------------
// Emits a MOD/RM for accessing a dword at [<indexreg> + ofs32]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitOffsetModRM(BYTE opcode, X86Reg opcodereg, X86Reg indexreg, __int32 ofs)
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[7];
    BYTE*   code    = codeBuffer;
    int     nBytes  = 0;
#ifdef _TARGET_AMD64_
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
        *((__int32*)(2+code)) = ofs;
        nBytes += 5;
        EmitBytes(codeBuffer, nBytes);
    }
}

//---------------------------------------------------------------
// Emits a MOD/RM for accessing a dword at [<baseReg> + <indexReg>*<scale> + ofs32]
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitOffsetModRmSIB(BYTE opcode, X86Reg opcodeOrReg, X86Reg baseReg, X86Reg indexReg, __int32 scale, __int32 ofs)
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

#ifdef _TARGET_AMD64_
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
        *(__int32*)(&code[3]) = ofs;
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

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_
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
                              __int32 ofs /*=0*/,
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

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_

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
        case 1: Emit8( (__int8)ofs ); break;
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

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_

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
                                     __int32 ofs
                           AMD64_ARG(X86OperandSize OperandSize /*= k64BitOp*/)
                                     )
{
    STANDARD_VM_CONTRACT;

    BYTE    codeBuffer[8];
    BYTE   *code = codeBuffer;
    int     nBytes;

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_
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
        *((__int32*)(3+code)) = ofs;
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

#ifdef _TARGET_AMD64_
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


#ifdef _TARGET_AMD64_
static const X86Reg c_argRegs[] = { 
    #define ARGUMENT_REGISTER(regname) k##regname,
    ENUM_ARGUMENT_REGISTERS()
    #undef ARGUMENT_REGISTER
};
#endif


#ifndef CROSSGEN_COMPILE

#if defined(_DEBUG) && (defined(_TARGET_AMD64_) || defined(_TARGET_X86_)) && !defined(FEATURE_PAL)
void StubLinkerCPU::EmitJITHelperLoggingThunk(PCODE pJitHelper, LPVOID helperFuncCount)
{
    STANDARD_VM_CONTRACT;

    VMHELPCOUNTDEF* pHelperFuncCount = (VMHELPCOUNTDEF*)helperFuncCount;
/*
        push        rcx
        mov         rcx, &(pHelperFuncCount->count)
   lock inc        [rcx]
        pop         rcx
#ifdef _TARGET_AMD64_
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

#if defined(_TARGET_AMD64_)
    // mov      rax, <pJitHelper>
    // pop      rcx
    // jmp      rax
#else
    // pop      rcx
    // jmp      <pJitHelper>
#endif
    X86EmitTailcallWithSinglePop(NewExternalCodeLabel(pJitHelper), kECX);
}
#endif // _DEBUG && (_TARGET_AMD64_ || _TARGET_X86_) && !FEATURE_PAL

#ifndef FEATURE_IMPLICIT_TLS
//---------------------------------------------------------------
// Emit code to store the current Thread structure in dstreg
// preservedRegSet is a set of registers to be preserved
// TRASHES  EAX, EDX, ECX unless they are in preservedRegSet.
// RESULTS  dstreg = current Thread
//---------------------------------------------------------------
VOID StubLinkerCPU::X86EmitTLSFetch(DWORD idx, X86Reg dstreg, unsigned preservedRegSet)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

    // It doesn't make sense to have the destination register be preserved
        PRECONDITION((preservedRegSet & (1<<dstreg)) == 0);
        AMD64_ONLY(PRECONDITION(dstreg < 8)); // code below doesn't support high registers
    }
    CONTRACTL_END;

    TLSACCESSMODE mode = GetTLSAccessMode(idx);

#ifdef _DEBUG
    {
        static BOOL f = TRUE;
        f = !f;
        if (f)
        {
           mode = TLSACCESS_GENERIC;
        }
    }
#endif

    switch (mode)
    {
        case TLSACCESS_WNT: 
            {
                unsigned __int32 tlsofs = offsetof(TEB, TlsSlots) + (idx * sizeof(void*));
#ifdef _TARGET_AMD64_                
                BYTE code[] = {0x65,0x48,0x8b,0x04,0x25};    // mov dstreg, qword ptr gs:[IMM32]
                static const int regByteIndex = 3;
#elif defined(_TARGET_X86_)
                BYTE code[] = {0x64,0x8b,0x05};              // mov dstreg, dword ptr fs:[IMM32]
                static const int regByteIndex = 2;
#endif 
                code[regByteIndex] |= (dstreg << 3);

                EmitBytes(code, sizeof(code));
                Emit32(tlsofs);
            }
            break;

        case TLSACCESS_GENERIC:

            X86EmitPushRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

            X86EmitPushImm32(idx);
#ifdef _TARGET_AMD64_
            X86EmitPopReg (kECX);       // arg in reg
#endif

            // call TLSGetValue
            X86EmitCall(NewExternalCodeLabel((LPVOID) TlsGetValue), sizeof(void*));

            // mov dstreg, eax
            X86EmitMovRegReg(dstreg, kEAX);

            X86EmitPopRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

            break;

        default:
            _ASSERTE(0);
    }

#ifdef _DEBUG
    // Trash caller saved regs that we were not told to preserve, and that aren't the dstreg.
    preservedRegSet |= 1<<dstreg;
    if (!(preservedRegSet & (1<<kEAX)))
        X86EmitDebugTrashReg(kEAX);
    if (!(preservedRegSet & (1<<kEDX)))
        X86EmitDebugTrashReg(kEDX);
    if (!(preservedRegSet & (1<<kECX)))
        X86EmitDebugTrashReg(kECX);
#endif

}
#endif // FEATURE_IMPLICIT_TLS

VOID StubLinkerCPU::X86EmitCurrentThreadFetch(X86Reg dstreg, unsigned preservedRegSet)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

    // It doesn't make sense to have the destination register be preserved
        PRECONDITION((preservedRegSet & (1<<dstreg)) == 0);
        AMD64_ONLY(PRECONDITION(dstreg < 8)); // code below doesn't support high registers
    }
    CONTRACTL_END;

#ifdef FEATURE_IMPLICIT_TLS

    X86EmitPushRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

    //TODO: Inline the instruction instead of a call
    // call GetThread
    X86EmitCall(NewExternalCodeLabel((LPVOID) GetThread), sizeof(void*));

    // mov dstreg, eax
    X86EmitMovRegReg(dstreg, kEAX);

    X86EmitPopRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

#ifdef _DEBUG
    // Trash caller saved regs that we were not told to preserve, and that aren't the dstreg.
    preservedRegSet |= 1<<dstreg;
    if (!(preservedRegSet & (1<<kEAX)))
        X86EmitDebugTrashReg(kEAX);
    if (!(preservedRegSet & (1<<kEDX)))
        X86EmitDebugTrashReg(kEDX);
    if (!(preservedRegSet & (1<<kECX)))
        X86EmitDebugTrashReg(kECX);
#endif // _DEBUG

#else // FEATURE_IMPLICIT_TLS

    X86EmitTLSFetch(GetThreadTLSIndex(), dstreg, preservedRegSet);
    
#endif // FEATURE_IMPLICIT_TLS

}

VOID StubLinkerCPU::X86EmitCurrentAppDomainFetch(X86Reg dstreg, unsigned preservedRegSet)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

    // It doesn't make sense to have the destination register be preserved
        PRECONDITION((preservedRegSet & (1<<dstreg)) == 0);
        AMD64_ONLY(PRECONDITION(dstreg < 8)); // code below doesn't support high registers
    }
    CONTRACTL_END;

#ifdef FEATURE_IMPLICIT_TLS
    X86EmitPushRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

    //TODO: Inline the instruction instead of a call
    // call GetThread
    X86EmitCall(NewExternalCodeLabel((LPVOID) GetAppDomain), sizeof(void*));

    // mov dstreg, eax
    X86EmitMovRegReg(dstreg, kEAX);

    X86EmitPopRegs(preservedRegSet & ((1<<kEAX)|(1<<kEDX)|(1<<kECX)));

#ifdef _DEBUG
    // Trash caller saved regs that we were not told to preserve, and that aren't the dstreg.
    preservedRegSet |= 1<<dstreg;
    if (!(preservedRegSet & (1<<kEAX)))
        X86EmitDebugTrashReg(kEAX);
    if (!(preservedRegSet & (1<<kEDX)))
        X86EmitDebugTrashReg(kEDX);
    if (!(preservedRegSet & (1<<kECX)))
        X86EmitDebugTrashReg(kECX);
#endif

#else // FEATURE_IMPLICIT_TLS

    X86EmitTLSFetch(GetAppDomainTLSIndex(), dstreg, preservedRegSet);

#endif // FEATURE_IMPLICIT_TLS
}

#ifdef _TARGET_X86_

#ifdef PROFILING_SUPPORTED
VOID StubLinkerCPU::EmitProfilerComCallProlog(TADDR pFrameVptr, X86Reg regFrame)
{
    STANDARD_VM_CONTRACT;

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // Load the methoddesc into ECX (UMThkCallFrame->m_pvDatum->m_pMD)
        X86EmitIndexRegLoad(kECX, regFrame, UMThkCallFrame::GetOffsetOfDatum());
        X86EmitIndexRegLoad(kECX, kECX, UMEntryThunk::GetOffsetOfMethodDesc());

        // Push arguments and notify profiler
        X86EmitPushImm32(COR_PRF_TRANSITION_CALL);      // Reason
        X86EmitPushReg(kECX);                           // MethodDesc*
        X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerUnmanagedToManagedTransitionMD), 2*sizeof(void*));
    }

#ifdef FEATURE_COMINTEROP
    else if (pFrameVptr == ComMethodFrame::GetMethodFrameVPtr())
    {
        // Load the methoddesc into ECX (Frame->m_pvDatum->m_pMD)
        X86EmitIndexRegLoad(kECX, regFrame, ComMethodFrame::GetOffsetOfDatum());
        X86EmitIndexRegLoad(kECX, kECX, ComCallMethodDesc::GetOffsetOfMethodDesc());

        // Push arguments and notify profiler
        X86EmitPushImm32(COR_PRF_TRANSITION_CALL);      // Reason
        X86EmitPushReg(kECX);                           // MethodDesc*
        X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerUnmanagedToManagedTransitionMD), 2*sizeof(void*));
    }
#endif // FEATURE_COMINTEROP

    // Unrecognized frame vtbl
    else
    {
        _ASSERTE(!"Unrecognized vtble passed to EmitComMethodStubProlog with profiling turned on.");
    }
}


VOID StubLinkerCPU::EmitProfilerComCallEpilog(TADDR pFrameVptr, X86Reg regFrame)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
#ifdef FEATURE_COMINTEROP
        PRECONDITION(pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr() || pFrameVptr == ComMethodFrame::GetMethodFrameVPtr());
#else 
        PRECONDITION(pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr());
#endif // FEATURE_COMINTEROP
    }
    CONTRACTL_END;

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // Load the methoddesc into ECX (UMThkCallFrame->m_pvDatum->m_pMD)
        X86EmitIndexRegLoad(kECX, regFrame, UMThkCallFrame::GetOffsetOfDatum());
        X86EmitIndexRegLoad(kECX, kECX, UMEntryThunk::GetOffsetOfMethodDesc());

        // Push arguments and notify profiler
        X86EmitPushImm32(COR_PRF_TRANSITION_RETURN);    // Reason
        X86EmitPushReg(kECX);                           // MethodDesc*
        X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerManagedToUnmanagedTransitionMD), 2*sizeof(void*));
    }

#ifdef FEATURE_COMINTEROP
    else if (pFrameVptr == ComMethodFrame::GetMethodFrameVPtr())
    {
        // Load the methoddesc into ECX (Frame->m_pvDatum->m_pMD)
        X86EmitIndexRegLoad(kECX, regFrame, ComMethodFrame::GetOffsetOfDatum());
        X86EmitIndexRegLoad(kECX, kECX, ComCallMethodDesc::GetOffsetOfMethodDesc());

        // Push arguments and notify profiler
        X86EmitPushImm32(COR_PRF_TRANSITION_RETURN);    // Reason
        X86EmitPushReg(kECX);                           // MethodDesc*
        X86EmitCall(NewExternalCodeLabel((LPVOID) ProfilerManagedToUnmanagedTransitionMD), 2*sizeof(void*));
    }
#endif // FEATURE_COMINTEROP

    // Unrecognized frame vtbl
    else
    {
        _ASSERTE(!"Unrecognized vtble passed to EmitComMethodStubEpilog with profiling turned on.");
    }
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
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL && rgRareLabels[2] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL && rgRejoinLabels[2] != NULL);
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

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // Preserve argument registers for thiscall/fastcall
        X86EmitPushReg(kECX);
        X86EmitPushReg(kEDX);
    }

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

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // push UnmanagedToManagedExceptHandler
        X86EmitPushImmPtr((LPVOID)UMThunkPrestubHandler);

        // mov eax, fs:[0]
        static const BYTE codeSEH1[] = { 0x64, 0xA1, 0x0, 0x0, 0x0, 0x0};
        EmitBytes(codeSEH1, sizeof(codeSEH1));

        // push eax
        X86EmitPushReg(kEAX);

        // mov dword ptr fs:[0], esp
        static const BYTE codeSEH2[] = { 0x64, 0x89, 0x25, 0x0, 0x0, 0x0, 0x0};
        EmitBytes(codeSEH2, sizeof(codeSEH2));
    }

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
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL && rgRareLabels[2] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL && rgRejoinLabels[2] != NULL);
    }
    CONTRACTL_END;

    EmitCheckGSCookie(kESI, UnmanagedToManagedFrame::GetOffsetOfGSCookie());

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // if we are using exceptions, unlink the SEH
        // mov ecx,[esp]  ;;pointer to the next exception record
        X86EmitEspOffset(0x8b, kECX, 0);

        // mov dword ptr fs:[0], ecx
        static const BYTE codeSEH[] = { 0x64, 0x89, 0x0D, 0x0, 0x0, 0x0, 0x0 };
        EmitBytes(codeSEH, sizeof(codeSEH));

        X86EmitAddEsp(sizeof(EXCEPTION_REGISTRATION_RECORD));
    }

    // mov [ebx + Thread.GetFrame()], edi  ;; restore previous frame
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kEDI);

    //-----------------------------------------------------------------------
    // Generate the inline part of disabling preemptive GC
    //-----------------------------------------------------------------------
    EmitEnable(rgRareLabels[2]); // rare gc
    EmitLabel(rgRejoinLabels[2]);        // rejoin for rare gc

    if (pFrameVptr == UMThkCallFrame::GetMethodFrameVPtr())
    {
        // Restore argument registers for thiscall/fastcall
        X86EmitPopReg(kEDX);
        X86EmitPopReg(kECX);
    }

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
    // The out-of-line portion of enabling preemptive GC - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[2]);  // label for rare enable gc
    EmitRareEnable(rgRejoinLabels[2]); // emit rare enable gc

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

#ifdef FEATURE_IMPLICIT_TLS
    DWORD idx = 0;
    TLSACCESSMODE mode = TLSACCESS_GENERIC;
#else
    DWORD idx = GetThreadTLSIndex();
    TLSACCESSMODE mode = GetTLSAccessMode(idx);
#endif

#ifdef _DEBUG
    {
        static BOOL f = TRUE;
        f = !f;
        if (f)
        {
           mode = TLSACCESS_GENERIC;
        }
    }
#endif

    switch (mode)
    {
        case TLSACCESS_WNT: 
            {
                unsigned __int32 tlsofs = offsetof(TEB, TlsSlots) + (idx * sizeof(void*));

                static const BYTE code[] = {0x64,0x8b,0x1d};              // mov ebx, dword ptr fs:[IMM32]
                EmitBytes(code, sizeof(code));
                Emit32(tlsofs);
            }
            break;

        case TLSACCESS_GENERIC:
#ifdef FEATURE_IMPLICIT_TLS
            X86EmitCall(NewExternalCodeLabel((LPVOID) GetThread), sizeof(void*));
#else
            X86EmitPushImm32(idx);

            // call TLSGetValue
            X86EmitCall(NewExternalCodeLabel((LPVOID) TlsGetValue), sizeof(void*));
#endif
            // mov ebx,eax
            Emit16(0xc389);
            break;
        default:
            _ASSERTE(0);
    }

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

#ifndef FEATURE_COMINTEROP
    _ASSERTE(fThrow);
#else // !FEATURE_COMINTEROP
    if (!fThrow)
    {
        X86EmitPushReg(kESI);
        X86EmitCall(NewExternalCodeLabel((LPVOID) CreateThreadBlockReturnHr), sizeof(void*));
    }
    else
#endif // !FEATURE_COMINTEROP
    {
        X86EmitCall(NewExternalCodeLabel((LPVOID) CreateThreadBlockThrow), 0);
    }

    // mov ebx,eax
    Emit16(0xc389);
    X86EmitNearJump(pRejoinPoint);
}

//========================================================================
#endif // _TARGET_X86_
//========================================================================
#if defined(FEATURE_COMINTEROP) && defined(_TARGET_X86_)
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
        PRECONDITION(rgRareLabels[0] != NULL && rgRareLabels[1] != NULL && rgRareLabels[2] != NULL);
        PRECONDITION(rgRejoinLabels != NULL);
        PRECONDITION(rgRejoinLabels[0] != NULL && rgRejoinLabels[1] != NULL && rgRejoinLabels[2] != NULL);
    }
    CONTRACTL_END;

    CodeLabel *NoEntryLabel;
    NoEntryLabel = NewCodeLabel();

    EmitCheckGSCookie(kESI, UnmanagedToManagedFrame::GetOffsetOfGSCookie());

    // mov [ebx + Thread.GetFrame()], edi  ;; restore previous frame
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kEDI);

    //-----------------------------------------------------------------------
    // Generate the inline part of enabling preemptive GC
    //-----------------------------------------------------------------------
    EmitLabel(NoEntryLabel);    // need to enable preemp mode even when we fail the disable as rare disable will return in coop mode
    
    EmitEnable(rgRareLabels[2]);     // rare enable gc
    EmitLabel(rgRejoinLabels[2]);        // rejoin for rare enable gc

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
    // The out-of-line portion of enabling preemptive GC - rarely executed
    //-----------------------------------------------------------------------
    EmitLabel(rgRareLabels[2]);  // label for rare enable gc
    EmitRareEnable(rgRejoinLabels[2]); // emit rare enable gc

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

//========================================================================
#endif // defined(FEATURE_COMINTEROP) && defined(_TARGET_X86_)

#ifndef FEATURE_STUBS_AS_IL
/*==============================================================================
    Pushes a TransitionFrame on the stack
    If you make any changes to the prolog instruction sequence, be sure
    to update UpdateRegdisplay, too!!  This service should only be called from
    within the runtime.  It should not be called for any unmanaged -> managed calls in.

    At the end of the generated prolog stub code:
    pFrame is in ESI/RSI.
    the previous pFrame is in EDI/RDI
    The current Thread* is in EBX/RBX.
    For x86, ESP points to TransitionFrame
    For amd64, ESP points to the space reserved for the outgoing argument registers
*/

VOID StubLinkerCPU::EmitMethodStubProlog(TADDR pFrameVptr, int transitionBlockOffset)
{
    STANDARD_VM_CONTRACT;

#ifdef _TARGET_AMD64_
    X86EmitPushReg(kR15);   // CalleeSavedRegisters
    X86EmitPushReg(kR14);
    X86EmitPushReg(kR13);
    X86EmitPushReg(kR12);
    X86EmitPushReg(kRBP);
    X86EmitPushReg(kRBX);
    X86EmitPushReg(kRSI);
    X86EmitPushReg(kRDI);

    // Push m_datum
    X86EmitPushReg(SCRATCH_REGISTER_X86REG);

    // push edx ;leave room for m_next (edx is an arbitrary choice)
    X86EmitPushReg(kEDX);

    // push Frame vptr
    X86EmitPushImmPtr((LPVOID) pFrameVptr);

    // mov rsi, rsp
    X86EmitR2ROp(0x8b, kRSI, (X86Reg)4 /*kESP*/);
    UnwindSetFramePointer(kRSI);

    // Save ArgumentRegisters
    #define ARGUMENT_REGISTER(regname) X86EmitRegSave(k##regname, SecureDelegateFrame::GetOffsetOfTransitionBlock() + \
        sizeof(TransitionBlock) + offsetof(ArgumentRegisters, regname));
    ENUM_ARGUMENT_REGISTERS();
    #undef ARGUMENT_REGISTER

    _ASSERTE(((Frame*)&pFrameVptr)->GetGSCookiePtr() == PTR_GSCookie(PBYTE(&pFrameVptr) - sizeof(GSCookie)));
    X86EmitPushImmPtr((LPVOID)GetProcessGSCookie());

    // sub rsp, 4*sizeof(void*)           ;; allocate callee scratch area and ensure rsp is 16-byte-aligned
    const INT32 padding = sizeof(ArgumentRegisters) + ((sizeof(FramedMethodFrame) % (2 * sizeof(LPVOID))) ? 0 : sizeof(LPVOID));
    X86EmitSubEsp(padding);
#endif // _TARGET_AMD64_

#ifdef _TARGET_X86_
    // push ebp     ;; save callee-saved register
    // mov ebp,esp
    // push ebx     ;; save callee-saved register
    // push esi     ;; save callee-saved register
    // push edi     ;; save callee-saved register
    X86EmitPushEBPframe();

    X86EmitPushReg(kEBX);
    X86EmitPushReg(kESI);
    X86EmitPushReg(kEDI);

    // Push & initialize ArgumentRegisters
    #define ARGUMENT_REGISTER(regname) X86EmitPushReg(k##regname);
    ENUM_ARGUMENT_REGISTERS();
    #undef ARGUMENT_REGISTER

    // Push m_datum
    X86EmitPushReg(kEAX);

    // push edx ;leave room for m_next (edx is an arbitrary choice)
    X86EmitPushReg(kEDX);

    // push Frame vptr
    X86EmitPushImmPtr((LPVOID) pFrameVptr);

    // mov esi,esp
    X86EmitMovRegSP(kESI);

    X86EmitPushImmPtr((LPVOID)GetProcessGSCookie());
#endif // _TARGET_X86_

    // ebx <-- GetThread()
    // Trashes X86TLSFetch_TRASHABLE_REGS
    X86EmitCurrentThreadFetch(kEBX, 0);

#if _DEBUG

    // call ObjectRefFlush
#ifdef _TARGET_AMD64_

    // mov rcx, rbx
    X86EmitR2ROp(0x8b, kECX, kEBX);         // arg in reg

#else // !_TARGET_AMD64_
    X86EmitPushReg(kEBX);                   // arg on stack
#endif // _TARGET_AMD64_

    // Make the call
    X86EmitCall(NewExternalCodeLabel((LPVOID) Thread::ObjectRefFlush), sizeof(void*));

#endif // _DEBUG

    // mov edi,[ebx + Thread.GetFrame()]    ;; get previous frame
    X86EmitIndexRegLoad(kEDI, kEBX, Thread::GetOffsetOfCurrentFrame());

    // mov [esi + Frame.m_next], edi
    X86EmitIndexRegStore(kESI, Frame::GetOffsetOfNextLink(), kEDI);

    // mov [ebx + Thread.GetFrame()], esi
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kESI);

#if _DEBUG

    if (Frame::ShouldLogTransitions())
    {
        // call LogTransition
#ifdef _TARGET_AMD64_

        // mov rcx, rsi
        X86EmitR2ROp(0x8b, kECX, kESI);         // arg in reg

#else // !_TARGET_AMD64_
        X86EmitPushReg(kESI);                   // arg on stack
#endif // _TARGET_AMD64_
         
        X86EmitCall(NewExternalCodeLabel((LPVOID) Frame::LogTransition), sizeof(void*));

#ifdef _TARGET_AMD64_
    // Reload parameter registers
    // mov r, [esp+offs]
    #define ARGUMENT_REGISTER(regname) X86EmitEspOffset(0x8b, k##regname, sizeof(ArgumentRegisters) + \
        sizeof(TransitionFrame) + offsetof(ArgumentRegisters, regname));
    ENUM_ARGUMENT_REGISTERS();
    #undef ARGUMENT_REGISTER

#endif // _TARGET_AMD64_
    }

#endif // _DEBUG


#ifdef _TARGET_AMD64_
    // OK for the debugger to examine the new frame now
    // (Note that if it's not OK yet for some stub, another patch label
    // can be emitted later which will override this one.)    
    EmitPatchLabel();
#else
    // For x86, the patch label can be specified only after the GSCookie is pushed
    // Otherwise the debugger will see a Frame without a valid GSCookie
#endif
}

/*==============================================================================
 EmitMethodStubEpilog generates the part of the stub that will pop off the
 Frame
 
 restoreArgRegs - indicates whether the argument registers need to be
                  restored from m_argumentRegisters
  
 At this point of the stub:
    pFrame is in ESI/RSI.
    the previous pFrame is in EDI/RDI
    The current Thread* is in EBX/RBX.
    For x86, ESP points to the FramedMethodFrame::NegInfo
*/

VOID StubLinkerCPU::EmitMethodStubEpilog(WORD numArgBytes, int transitionBlockOffset)
{
    STANDARD_VM_CONTRACT;

    // mov [ebx + Thread.GetFrame()], edi  ;; restore previous frame
    X86EmitIndexRegStore(kEBX, Thread::GetOffsetOfCurrentFrame(), kEDI);

#ifdef _TARGET_X86_
    // deallocate Frame
    X86EmitAddEsp(sizeof(GSCookie) + transitionBlockOffset + TransitionBlock::GetOffsetOfCalleeSavedRegisters());

#elif defined(_TARGET_AMD64_)
    // lea rsp, [rsi + <offset of preserved registers>]
    X86EmitOffsetModRM(0x8d, (X86Reg)4 /*kRSP*/, kRSI, transitionBlockOffset + TransitionBlock::GetOffsetOfCalleeSavedRegisters());
#endif // _TARGET_AMD64_

    // pop edi        ; restore callee-saved registers
    // pop esi
    // pop ebx
    // pop ebp
    X86EmitPopReg(kEDI);
    X86EmitPopReg(kESI);
    X86EmitPopReg(kEBX);
    X86EmitPopReg(kEBP);

#ifdef _TARGET_AMD64_
    X86EmitPopReg(kR12);
    X86EmitPopReg(kR13);
    X86EmitPopReg(kR14);
    X86EmitPopReg(kR15);
#endif

#ifdef _TARGET_AMD64_
    // Caller deallocates argument space.  (Bypasses ASSERT in
    // X86EmitReturn.)
    numArgBytes = 0;
#endif

    X86EmitReturn(numArgBytes);
}


// On entry, ESI should be pointing to the Frame

VOID StubLinkerCPU::EmitCheckGSCookie(X86Reg frameReg, int gsCookieOffset)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    // cmp dword ptr[frameReg-gsCookieOffset], gsCookie
#ifdef _TARGET_X86_
    X86EmitCmpRegIndexImm32(frameReg, gsCookieOffset, GetProcessGSCookie());
#else
    X64EmitCmp32RegIndexImm32(frameReg, gsCookieOffset, (INT32)GetProcessGSCookie());
#endif
    
    CodeLabel * pLabel = NewCodeLabel();
    X86EmitCondJump(pLabel, X86CondCode::kJE);
    
    X86EmitCall(NewExternalCodeLabel((LPVOID) JIT_FailFast), 0);

    EmitLabel(pLabel);
#endif
}
#endif // !FEATURE_STUBS_AS_IL


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

#ifdef FEATURE_STUBS_AS_IL
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
#ifdef _TARGET_AMD64_
    X86EmitAddReg(THIS_kREG, sizeof(void*));

    // Use direct call if possible
    if (pUnboxMD->HasStableEntryPoint())
    {
        X86EmitRegLoad(kRAX, pUnboxMD->GetStableEntryPoint());// MOV RAX, DWORD
    }
    else
    {
        X86EmitRegLoad(kRAX, (UINT_PTR)pUnboxMD->GetAddrOfSlot()); // MOV RAX, DWORD
        
        X86EmitIndexRegLoad(kRAX, kRAX);                // MOV RAX, [RAX]
    }

    Emit16(X86_INSTR_JMP_EAX);                          // JMP EAX
#else // _TARGET_AMD64_
    X86EmitAddReg(THIS_kREG, sizeof(void*));

    // Use direct call if possible
    if (pUnboxMD->HasStableEntryPoint())
    {
        X86EmitNearJump(NewExternalCodeLabel((LPVOID) pUnboxMD->GetStableEntryPoint()));
    }
    else
    {
        // jmp [slot]
        Emit16(0x25ff);
        Emit32((DWORD)(size_t)pUnboxMD->GetAddrOfSlot());
    }
#endif //_TARGET_AMD64_
}


#if defined(FEATURE_SHARE_GENERIC_CODE) && !defined(FEATURE_STUBS_AS_IL)
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

#ifdef _TARGET_AMD64_
    int paramTypeArgOffset = argit.GetParamTypeArgOffset();
    int paramTypeArgIndex = TransitionBlock::GetArgumentIndexFromOffset(paramTypeArgOffset);

    CorElementType argTypes[5];

    int firstRealArg = paramTypeArgIndex + 1;
    int argNum = firstRealArg;

    //
    // Compute types of the 4 register args and first stack arg
    //

    CorElementType sigType;
    while ((sigType = msig.NextArgNormalized()) != ELEMENT_TYPE_END)
    {
        argTypes[argNum++] = sigType;
        if (argNum > 4)
            break;
    }
    msig.Reset();

    BOOL fUseInstantiatingMethodStubWorker = FALSE;

    if (argNum > 4)
    {
        //
        // We will need to go through assembly helper.
        //
        fUseInstantiatingMethodStubWorker = TRUE;

        // Allocate space for frame before pushing the arguments for the assembly helper
        X86EmitSubEsp((INT32)(AlignUp(sizeof(void *) /* extra stack param */ + sizeof(GSCookie) + sizeof(StubHelperFrame), 16) - sizeof(void *) /* return address */));

        //
        // Store extra arg stack arg param for the helper.
        //
        CorElementType argType = argTypes[--argNum];
        switch (argType)
        {
        case ELEMENT_TYPE_R4:
            // movss dword ptr [rsp], xmm?
            X64EmitMovSSToMem(kXMM3, (X86Reg)4 /*kRSP*/);
            break;
        case ELEMENT_TYPE_R8:
            // movsd qword ptr [rsp], xmm?
            X64EmitMovSDToMem(kXMM3, (X86Reg)4 /*kRSP*/);
            break;
        default:
            X86EmitIndexRegStoreRSP(0, kR9);
            break;
        }
    }

    //
    // Shuffle the register arguments
    //
    while (argNum > firstRealArg)
    {
        CorElementType argType = argTypes[--argNum];

        switch (argType)
        {
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
                // mov xmm#, xmm#-1
                X64EmitMovXmmXmm((X86Reg)argNum, (X86Reg)(argNum - 1));
                break;
            default:
                //mov reg#, reg#-1
                X86EmitMovRegReg(c_argRegs[argNum], c_argRegs[argNum-1]);
                break;
        }
    }

    //
    // Setup the hidden instantiation argument
    //
    if (extra != NULL)
    {
        X86EmitRegLoad(c_argRegs[paramTypeArgIndex], (UINT_PTR)extra);
    }
    else
    {
        X86EmitIndexRegLoad(c_argRegs[paramTypeArgIndex], THIS_kREG);

        X86EmitAddReg(THIS_kREG, sizeof(void*));
    }

    // Use direct call if possible
    if (pMD->HasStableEntryPoint())
    {
        X86EmitRegLoad(kRAX, pMD->GetStableEntryPoint());// MOV RAX, DWORD
    }
    else
    {
        X86EmitRegLoad(kRAX, (UINT_PTR)pMD->GetAddrOfSlot()); // MOV RAX, DWORD
        
        X86EmitIndexRegLoad(kRAX, kRAX);                // MOV RAX, [RAX]
    }

    if (fUseInstantiatingMethodStubWorker)
    {
        X86EmitPushReg(kRAX);

        UINT cbStack = argit.SizeOfArgStack();
        _ASSERTE(cbStack > 0);

        X86EmitPushImm32((AlignUp(cbStack, 16) / sizeof(void*)) - 1);           // -1 for extra stack arg

        X86EmitRegLoad(kRAX, GetEEFuncEntryPoint(InstantiatingMethodStubWorker));// MOV RAX, DWORD
    }
    else
    {
        _ASSERTE(argit.SizeOfArgStack() == 0);
    }

    Emit16(X86_INSTR_JMP_EAX);

#else   
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
#endif //
}
#endif // FEATURE_SHARE_GENERIC_CODE && FEATURE_STUBS_AS_IL


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
    BEGIN_ENTRYPOINT_VOIDRET;

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
        HMODULE hmodPSAPI = WszGetModuleHandle(W("PSAPI.DLL"));

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

    HMODULE hmodKERNEL32 = WszGetModuleHandle(W("KERNEL32"));
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
        RUNTIME_FUNCTION *pFunctionEntry = RtlLookupFunctionEntry(
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
    
    END_ENTRYPOINT_VOIDRET;
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

#ifdef _TARGET_AMD64_
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


#ifdef _TARGET_X86_

//-----------------------------------------------------------------------
// Generates the inline portion of the code to enable preemptive GC. Hopefully,
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
VOID StubLinkerCPU::EmitEnable(CodeLabel *pForwardRef)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(4 == sizeof( ((Thread*)0)->m_State ));
        PRECONDITION(4 == sizeof( ((Thread*)0)->m_fPreemptiveGCDisabled ));
    }
    CONTRACTL_END;

    // move byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],0
    X86EmitOffsetModRM(0xc6, (X86Reg)0, kEBX, Thread::GetOffsetOfGCFlag());
    Emit8(0);

    _ASSERTE(FitsInI1(Thread::TS_CatchAtSafePoint));

    // test byte ptr [ebx + Thread.m_State], TS_CatchAtSafePoint
    X86EmitOffsetModRM(0xf6, (X86Reg)0, kEBX, Thread::GetOffsetOfState());
    Emit8(Thread::TS_CatchAtSafePoint);

    // jnz RarePath
    X86EmitCondJump(pForwardRef, X86CondCode::kJNZ);

#ifdef _DEBUG
    X86EmitDebugTrashReg(kECX);
#endif

}


//-----------------------------------------------------------------------
// Generates the out-of-line portion of the code to enable preemptive GC.
// After the work is done, the code jumps back to the "pRejoinPoint"
// which should be emitted right after the inline part is generated.
//
// Assumptions:
//      ebx = Thread
// Preserves
//      all registers except ecx.
//
//-----------------------------------------------------------------------
VOID StubLinkerCPU::EmitRareEnable(CodeLabel *pRejoinPoint)
{
    STANDARD_VM_CONTRACT;

    X86EmitCall(NewExternalCodeLabel((LPVOID) StubRareEnable), 0);
#ifdef _DEBUG
    X86EmitDebugTrashReg(kECX);
#endif
    if (pRejoinPoint)
    {
        X86EmitNearJump(pRejoinPoint);
    }

}


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

#if defined(FEATURE_COMINTEROP) && defined(MDA_SUPPORTED)
    // If we are checking whether the current thread is already holds the loader lock, vector
    // such cases to the rare disable pathway, where we can check again.
    if (fCallIn && (NULL != MDA_GET_ASSISTANT(Reentrancy)))
    {
        CodeLabel   *pNotReentrantLabel = NewCodeLabel();

        // test byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],1
        X86EmitOffsetModRM(0xf6, (X86Reg)0, ThreadReg, Thread::GetOffsetOfGCFlag());
        Emit8(1);

        // jz NotReentrant
        X86EmitCondJump(pNotReentrantLabel, X86CondCode::kJZ);
        
        X86EmitPushReg(kEAX);
        X86EmitPushReg(kEDX);
        X86EmitPushReg(kECX);

        X86EmitCall(NewExternalCodeLabel((LPVOID) HasIllegalReentrancy), 0);

        // If the probe fires, we go ahead and allow the call anyway.  At this point, there could be
        // GC heap corruptions.  So the probe detects the illegal case, but doesn't prevent it.

        X86EmitPopReg(kECX);
        X86EmitPopReg(kEDX);
        X86EmitPopReg(kEAX);

        EmitLabel(pNotReentrantLabel);
    }
#endif

    // move byte ptr [ebx + Thread.m_fPreemptiveGCDisabled],1
    X86EmitOffsetModRM(0xc6, (X86Reg)0, ThreadReg, Thread::GetOffsetOfGCFlag());
    Emit8(1);

    // cmp dword ptr g_TrapReturningThreads, 0
    Emit16(0x3d83);
    EmitPtr((void *)&g_TrapReturningThreads);
    Emit8(0);

    // jnz RarePath
    X86EmitCondJump(pForwardRef, X86CondCode::kJNZ);

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORESYSTEM)
    // If we are checking whether the current thread holds the loader lock, vector
    // such cases to the rare disable pathway, where we can check again.
    if (fCallIn && ShouldCheckLoaderLock())
    {
        X86EmitPushReg(kEAX);
        X86EmitPushReg(kEDX);

        if (ThreadReg == kECX)
            X86EmitPushReg(kECX);

        // BOOL AuxUlibIsDLLSynchronizationHeld(BOOL *IsHeld)
        //
        // So we need to be sure that both the return value and the passed BOOL are both TRUE.
        // If either is FALSE, then the call failed or the lock is not held.  Either way, the
        // probe should not fire.

        X86EmitPushReg(kEDX);               // BOOL temp
        Emit8(0x54);                        // push ESP because arg is &temp
        X86EmitCall(NewExternalCodeLabel((LPVOID) AuxUlibIsDLLSynchronizationHeld), 0);

        // callee has popped.
        X86EmitPopReg(kEDX);                // recover temp

        CodeLabel   *pPopLabel = NewCodeLabel();

        Emit16(0xc085);                     // test eax, eax
        X86EmitCondJump(pPopLabel, X86CondCode::kJZ);

        Emit16(0xd285);                     // test edx, edx

        EmitLabel(pPopLabel);               // retain the conditional flags across the pops

        if (ThreadReg == kECX)
            X86EmitPopReg(kECX);

        X86EmitPopReg(kEDX);
        X86EmitPopReg(kEAX);

        X86EmitCondJump(pForwardRef, X86CondCode::kJNZ);
    }
#endif

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

#ifdef FEATURE_COMINTEROP
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
#endif // FEATURE_COMINTEROP

#endif // _TARGET_X86_

#endif // CROSSGEN_COMPILE


VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    STANDARD_VM_CONTRACT;

#ifdef _TARGET_AMD64_

    // mov SCRATCHREG,rsp
    X86_64BitOperands();
    Emit8(0x8b);
    Emit8(0304 | (SCRATCH_REGISTER_X86REG << 3));

    // save the real target in r11, will jump to it later.  r10 is used below.
    // Windows: mov r11, rcx
    // Unix: mov r11, rdi
    X86EmitMovRegReg(kR11, THIS_kREG);

#ifdef UNIX_AMD64_ABI
    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        if (pEntry->srcofs & ShuffleEntry::REGMASK)
        {
            // If source is present in register then destination must also be a register
            _ASSERTE(pEntry->dstofs & ShuffleEntry::REGMASK);
            // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
            _ASSERTE((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK));

            int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;
            int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;

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
        else if (pEntry->dstofs & ShuffleEntry::REGMASK)
        {
            // source must be on the stack
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
            // source must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            // dest must be on the stack
            _ASSERTE(!(pEntry->dstofs & ShuffleEntry::REGMASK));

            // mov r10, [rax + src]
            X86EmitIndexRegLoad (kR10, SCRATCH_REGISTER_X86REG, (pEntry->srcofs + 1) * sizeof(void*));

            // mov [rax + dst], r10
            X86EmitIndexRegStore (SCRATCH_REGISTER_X86REG, (pEntry->dstofs + 1) * sizeof(void*), kR10);
        }
    }
#else // UNIX_AMD64_ABI
    UINT step = 1;

    if (pShuffleEntryArray->argtype == ELEMENT_TYPE_END)
    {
        // Special handling of open instance methods with return buffer. Move "this"
        // by two slots, and leave the "retbufptr" between the two slots intact.

        // mov rcx, r8
        X86EmitMovRegReg(kRCX, kR8);

        // Skip this entry
        pShuffleEntryArray++;

        // Skip this entry and leave retbufptr intact
        step += 2;
    }

    // Now shuffle the args by one position:
    //   steps 1-3 : reg args (rcx, rdx, r8)
    //   step  4   : stack->reg arg (r9)
    //   step >4   : stack args

    for(;
        pShuffleEntryArray->srcofs != ShuffleEntry::SENTINEL;
        step++, pShuffleEntryArray++)
    {
        switch (step)
        {
        case 1:
        case 2:
        case 3:
            switch (pShuffleEntryArray->argtype)
            {
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
                // mov xmm-1#, xmm#
                X64EmitMovXmmXmm((X86Reg)(step - 1), (X86Reg)(step));
                break;
            default:
                // mov argRegs[step-1], argRegs[step]
                X86EmitMovRegReg(c_argRegs[step-1], c_argRegs[step]);
                break;
            }
            break;

        case 4:
        {
            switch (pShuffleEntryArray->argtype)
            {
            case ELEMENT_TYPE_R4:
                X64EmitMovSSFromMem(kXMM3, kRAX, 0x28);
                break;

            case ELEMENT_TYPE_R8:
                X64EmitMovSDFromMem(kXMM3, kRAX, 0x28);
                break;

            default:
                // mov r9, [rax + 28h]
                X86EmitIndexRegLoad (kR9, SCRATCH_REGISTER_X86REG, 5*sizeof(void*));
            }
            break;
        }
        default:

            // mov r10, [rax + (step+1)*sizeof(void*)]
            X86EmitIndexRegLoad (kR10, SCRATCH_REGISTER_X86REG, (step+1)*sizeof(void*));

            // mov [rax + step*sizeof(void*)], r10
            X86EmitIndexRegStore (SCRATCH_REGISTER_X86REG, step*sizeof(void*), kR10);
        }
    }
#endif // UNIX_AMD64_ABI

    // mov r10, [r11 + Delegate._methodptraux]
    X86EmitIndexRegLoad(kR10, kR11, DelegateObject::GetOffsetOfMethodPtrAux());
    // add r11, DelegateObject::GetOffsetOfMethodPtrAux() - load the indirection cell into r11
    X86EmitAddReg(kR11, DelegateObject::GetOffsetOfMethodPtrAux());
    // Now jump to real target
    //   jmp r10
    X86EmitR2ROp(0xff, (X86Reg)4, kR10);

#else // _TARGET_AMD64_

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

    if (pWalk->stacksizedelta)
        X86EmitAddEsp(pWalk->stacksizedelta);

    // Now jump to real target
    //   JMP [SCRATCHREG]
    // we need to jump indirect so that for virtual delegates eax contains a pointer to the indirection cell
    X86EmitAddReg(SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtrAux());
    static const BYTE bjmpeax[] = { 0xff, 0x20 };
    EmitBytes(bjmpeax, sizeof(bjmpeax));
    
#endif // _TARGET_AMD64_
}


#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_STUBS_AS_IL)

//===========================================================================
// Computes hash code for MulticastDelegate.Invoke()
UINT_PTR StubLinkerCPU::HashMulticastInvoke(MetaSig* pSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    ArgIterator argit(pSig);

    UINT numStackBytes = argit.SizeOfArgStack();

    if (numStackBytes > 0x7FFF) 
        COMPlusThrow(kNotSupportedException, W("NotSupported_TooManyArgs"));

#ifdef _TARGET_AMD64_
    // Generate a hash key as follows:
    //      UINT Arg0Type:2; // R4 (1), R8 (2), other (3)
    //      UINT Arg1Type:2; // R4 (1), R8 (2), other (3)
    //      UINT Arg2Type:2; // R4 (1), R8 (2), other (3)
    //      UINT Arg3Type:2; // R4 (1), R8 (2), other (3)
    //      UINT NumArgs:24; // number of arguments
    // (This should cover all the prestub variations)

    _ASSERTE(!(numStackBytes & 7));
    UINT hash = (numStackBytes / sizeof(void*)) << 8;

    UINT argNum = 0;

    // NextArg() doesn't take into account the "this" pointer.
    // That's why we have to special case it here.
    if (argit.HasThis())
    {
        hash |= 3 << (2*argNum);
        argNum++;
    }

    if (argit.HasRetBuffArg())
    {
        hash |= 3 << (2*argNum);
        argNum++;
    }

    for (; argNum < 4; argNum++)
    {
        switch (pSig->NextArgNormalized())
        {
        case ELEMENT_TYPE_END:
            argNum = 4;
            break;
        case ELEMENT_TYPE_R4:
            hash |= 1 << (2*argNum);
            break;
        case ELEMENT_TYPE_R8:
            hash |= 2 << (2*argNum);
            break;
        default:
            hash |= 3 << (2*argNum);
            break;
        }
    }

#else // _TARGET_AMD64_

    // check if the function is returning a float, in which case the stub has to take
    // care of popping the floating point stack except for the last invocation

    _ASSERTE(!(numStackBytes & 3));

    UINT hash = numStackBytes;

    if (CorTypeInfo::IsFloat(pSig->GetReturnType()))
    {
        hash |= 2;
    }
#endif // _TARGET_AMD64_

    return hash;
}

#ifdef _TARGET_X86_
//===========================================================================
// Emits code for MulticastDelegate.Invoke()
VOID StubLinkerCPU::EmitDelegateInvoke()
{
    STANDARD_VM_CONTRACT;

    CodeLabel *pNullLabel = NewCodeLabel();

    // test THISREG, THISREG
    X86EmitR2ROp(0x85, THIS_kREG, THIS_kREG);

    // jz null
    X86EmitCondJump(pNullLabel, X86CondCode::kJZ);

    // mov SCRATCHREG, [THISREG + Delegate.FP]  ; Save target stub in register
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfMethodPtr());

    // mov THISREG, [THISREG + Delegate.OR]  ; replace "this" pointer
    X86EmitIndexRegLoad(THIS_kREG, THIS_kREG, DelegateObject::GetOffsetOfTarget());

    // jmp SCRATCHREG
    Emit16(0xe0ff | (SCRATCH_REGISTER_X86REG<<8));

    // Do a null throw
    EmitLabel(pNullLabel);

    // mov ECX, CORINFO_NullReferenceException
    Emit8(0xb8+kECX);
    Emit32(CORINFO_NullReferenceException);

    X86EmitCall(NewExternalCodeLabel(GetEEFuncEntryPoint(JIT_InternalThrowFromHelper)), 0);
    
    X86EmitReturn(0);
}
#endif // _TARGET_X86_

VOID StubLinkerCPU::EmitMulticastInvoke(UINT_PTR hash)
{
    STANDARD_VM_CONTRACT;

    int thisRegOffset = MulticastFrame::GetOffsetOfTransitionBlock() + 
        TransitionBlock::GetOffsetOfArgumentRegisters() + offsetof(ArgumentRegisters, THIS_REG);

    // push the methoddesc on the stack
    // mov eax, [ecx + offsetof(_methodAuxPtr)]
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfMethodPtrAux());
    
    // Push a MulticastFrame on the stack.
    EmitMethodStubProlog(MulticastFrame::GetMethodFrameVPtr(), MulticastFrame::GetOffsetOfTransitionBlock());

#ifdef _TARGET_X86_
    // Frame is ready to be inspected by debugger for patch location
    EmitPatchLabel();
#else // _TARGET_AMD64_

    // Save register arguments in their home locations.
    // Non-FP registers are already saved by EmitMethodStubProlog.
    // (Assumes Sig.NextArg() does not enum RetBuffArg or "this".)

    int            argNum      = 0;
    __int32        argOfs      = MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
    CorElementType argTypes[4];
    CorElementType argType;

    // 'this'
    argOfs += sizeof(void*);
    argTypes[argNum] = ELEMENT_TYPE_I8;
    argNum++;

    do
    {
        argType = ELEMENT_TYPE_END;

        switch ((hash >> (2 * argNum)) & 3)
        {
        case 0:
            argType = ELEMENT_TYPE_END;
            break;
        case 1:
            argType = ELEMENT_TYPE_R4;

            // movss dword ptr [rsp + argOfs], xmm?
            X64EmitMovSSToMem((X86Reg)argNum, kRSI, argOfs);
            break;
        case 2:
            argType = ELEMENT_TYPE_R8;

            // movsd qword ptr [rsp + argOfs], xmm?
            X64EmitMovSDToMem((X86Reg)argNum, kRSI, argOfs);
            break;
        default:
            argType = ELEMENT_TYPE_I;
            break;
        }

        argOfs += sizeof(void*);
        argTypes[argNum] = argType;
        argNum++;
    }
    while (argNum < 4 && ELEMENT_TYPE_END != argType);

    _ASSERTE(4 == argNum || ELEMENT_TYPE_END == argTypes[argNum-1]);

#endif // _TARGET_AMD64_

    // TODO: on AMD64, pick different regs for locals so don't need the pushes

    // push edi     ;; Save EDI (want to use it as loop index)
    X86EmitPushReg(kEDI);

    // xor edi,edi  ;; Loop counter: EDI=0,1,2...
    X86EmitZeroOutReg(kEDI);

    CodeLabel *pLoopLabel = NewCodeLabel();
    CodeLabel *pEndLoopLabel = NewCodeLabel();

    EmitLabel(pLoopLabel);

    // Entry:
    //   EDI == iteration counter

    // mov ecx, [esi + this]     ;; get delegate
    X86EmitIndexRegLoad(THIS_kREG, kESI, thisRegOffset);

    // cmp edi,[ecx]._invocationCount
    X86EmitOp(0x3b, kEDI, THIS_kREG, DelegateObject::GetOffsetOfInvocationCount());

    // je ENDLOOP
    X86EmitCondJump(pEndLoopLabel, X86CondCode::kJZ);

#ifdef _TARGET_AMD64_

    INT32 numStackBytes = (INT32)((hash >> 8) * sizeof(void *));

    INT32 stackUsed, numStackArgs, ofs;

    // Push any stack args, plus an extra location
    // for rsp alignment if needed

    numStackArgs = numStackBytes / sizeof(void*);

    // 1 push above, so stack is currently misaligned
    const unsigned STACK_ALIGN_ADJUST = 8;

    if (!numStackArgs)
    {
        // sub rsp, 28h             ;; 4 reg arg home locs + rsp alignment
        stackUsed = 0x20 + STACK_ALIGN_ADJUST;
        X86EmitSubEsp(stackUsed);
    }
    else
    {
        stackUsed = numStackArgs * sizeof(void*);

        // If the stack is misaligned, then an odd number of arguments
        // will naturally align the stack.
        if (   ((numStackArgs & 1) == 0)
            != (STACK_ALIGN_ADJUST == 0))
        {
            X86EmitPushReg(kRAX);
            stackUsed += sizeof(void*);
        }

        ofs = MulticastFrame::GetOffsetOfTransitionBlock() +
            TransitionBlock::GetOffsetOfArgs() + sizeof(ArgumentRegisters) + numStackBytes;

        while (numStackArgs--)
        {
            ofs -= sizeof(void*);

            // push [rsi + ofs]     ;; Push stack args
            X86EmitIndexPush(kESI, ofs);
        }

        // sub rsp, 20h             ;; Create 4 reg arg home locations
        X86EmitSubEsp(0x20);

        stackUsed += 0x20;
    }

    for(
        argNum = 0, argOfs = MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
        argNum < 4 && argTypes[argNum] != ELEMENT_TYPE_END;
        argNum++, argOfs += sizeof(void*)
        )
    {
        switch (argTypes[argNum])
        {
        case ELEMENT_TYPE_R4:
            // movss xmm?, dword ptr [rsi + argOfs]
            X64EmitMovSSFromMem((X86Reg)argNum, kRSI, argOfs);
            break;
        case ELEMENT_TYPE_R8:
            // movsd xmm?, qword ptr [rsi + argOfs]
            X64EmitMovSDFromMem((X86Reg)argNum, kRSI, argOfs);
            break;
        default:
            if (c_argRegs[argNum] != THIS_kREG)
            {
                // mov r*, [rsi + dstOfs]
                X86EmitIndexRegLoad(c_argRegs[argNum], kESI,argOfs);
            }
            break;
        } // switch
    }

    //    mov SCRATCHREG, [rcx+Delegate._invocationList]  ;;fetch invocation list
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfInvocationList());

    //    mov SCRATCHREG, [SCRATCHREG+m_Array+rdi*8]    ;; index into invocation list
    X86EmitOp(0x8b, kEAX, SCRATCH_REGISTER_X86REG, static_cast<int>(PtrArray::GetDataOffset()), kEDI, sizeof(void*), k64BitOp);

    //    mov THISREG, [SCRATCHREG+Delegate.object]  ;;replace "this" pointer
    X86EmitIndexRegLoad(THIS_kREG, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfTarget());

    // call [SCRATCHREG+Delegate.target] ;; call current subscriber
    X86EmitOffsetModRM(0xff, (X86Reg)2, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtr());

    // add rsp, stackUsed           ;; Clean up stack
    X86EmitAddEsp(stackUsed);

    //    inc edi
    Emit16(0xC7FF);

#else // _TARGET_AMD64_

    UINT16 numStackBytes = static_cast<UINT16>(hash & ~3);

    //    ..repush & reenregister args..
    INT32 ofs = numStackBytes + MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
    while (ofs != MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs())
    {
        ofs -= sizeof(void*);
        X86EmitIndexPush(kESI, ofs);
    }

    #define ARGUMENT_REGISTER(regname) if (k##regname != THIS_kREG) { X86EmitIndexRegLoad(k##regname, kESI, \
        offsetof(ArgumentRegisters, regname) + MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters()); }

    ENUM_ARGUMENT_REGISTERS_BACKWARD();

    #undef ARGUMENT_REGISTER

    //    mov SCRATCHREG, [ecx+Delegate._invocationList]  ;;fetch invocation list
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfInvocationList());

    //    mov SCRATCHREG, [SCRATCHREG+m_Array+edi*4]    ;; index into invocation list
    X86EmitOp(0x8b, kEAX, SCRATCH_REGISTER_X86REG, PtrArray::GetDataOffset(), kEDI, sizeof(void*));

    //    mov THISREG, [SCRATCHREG+Delegate.object]  ;;replace "this" pointer
    X86EmitIndexRegLoad(THIS_kREG, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfTarget());

    //    call [SCRATCHREG+Delegate.target] ;; call current subscriber
    X86EmitOffsetModRM(0xff, (X86Reg)2, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtr());
    INDEBUG(Emit8(0x90));       // Emit a nop after the call in debug so that
                                // we know that this is a call that can directly call
                                // managed code

    //    inc edi
    Emit8(0x47);

    if (hash & 2) // CorTypeInfo::IsFloat(pSig->GetReturnType())
    {
        // if the return value is a float/double check if we just did the last call - if not,
        // emit the pop of the float stack

        // mov SCRATCHREG, [esi + this]     ;; get delegate
        X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, kESI, thisRegOffset);

        // cmp edi,[SCRATCHREG]._invocationCount
        X86EmitOffsetModRM(0x3b, kEDI, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfInvocationCount());

        CodeLabel *pNoFloatStackPopLabel = NewCodeLabel();

        // je NOFLOATSTACKPOP
        X86EmitCondJump(pNoFloatStackPopLabel, X86CondCode::kJZ);

        // fstp 0
        Emit16(0xd8dd);

        // NoFloatStackPopLabel:
        EmitLabel(pNoFloatStackPopLabel);
    }

#endif // _TARGET_AMD64_

    // The debugger may need to stop here, so grab the offset of this code.
    EmitPatchLabel();

    // jmp LOOP
    X86EmitNearJump(pLoopLabel);

    //ENDLOOP:
    EmitLabel(pEndLoopLabel);

    // pop edi     ;; Restore edi
    X86EmitPopReg(kEDI);
 
    EmitCheckGSCookie(kESI, MulticastFrame::GetOffsetOfGSCookie());

    // Epilog
    EmitMethodStubEpilog(numStackBytes, MulticastFrame::GetOffsetOfTransitionBlock());
}

VOID StubLinkerCPU::EmitSecureDelegateInvoke(UINT_PTR hash)
{
    STANDARD_VM_CONTRACT;

    int thisRegOffset = SecureDelegateFrame::GetOffsetOfTransitionBlock() +
        TransitionBlock::GetOffsetOfArgumentRegisters() + offsetof(ArgumentRegisters, THIS_REG);

    // push the methoddesc on the stack
    // mov eax, [ecx + offsetof(_invocationCount)]
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfInvocationCount());
    
    // Push a SecureDelegateFrame on the stack.
    EmitMethodStubProlog(SecureDelegateFrame::GetMethodFrameVPtr(), SecureDelegateFrame::GetOffsetOfTransitionBlock());

#ifdef _TARGET_X86_
    // Frame is ready to be inspected by debugger for patch location
    EmitPatchLabel();
#else // _TARGET_AMD64_

    // Save register arguments in their home locations.
    // Non-FP registers are already saved by EmitMethodStubProlog.
    // (Assumes Sig.NextArg() does not enum RetBuffArg or "this".)

    int            argNum      = 0;
    __int32        argOfs      = SecureDelegateFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
    CorElementType argTypes[4];
    CorElementType argType;

    // 'this'
    argOfs += sizeof(void*);
    argTypes[argNum] = ELEMENT_TYPE_I8;
    argNum++;

    do
    {
        argType = ELEMENT_TYPE_END;

        switch ((hash >> (2 * argNum)) & 3)
        {
        case 0:
            argType = ELEMENT_TYPE_END;
            break;
        case 1:
            argType = ELEMENT_TYPE_R4;

            // movss dword ptr [rsp + argOfs], xmm?
            X64EmitMovSSToMem((X86Reg)argNum, kRSI, argOfs);
            break;
        case 2:
            argType = ELEMENT_TYPE_R8;

            // movsd qword ptr [rsp + argOfs], xmm?
            X64EmitMovSSToMem((X86Reg)argNum, kRSI, argOfs);
            break;
        default:
            argType = ELEMENT_TYPE_I;
            break;
        }

        argOfs += sizeof(void*);
        argTypes[argNum] = argType;
        argNum++;
    }
    while (argNum < 4 && ELEMENT_TYPE_END != argType);

    _ASSERTE(4 == argNum || ELEMENT_TYPE_END == argTypes[argNum-1]);

#endif // _TARGET_AMD64_

    // mov ecx, [esi + this]     ;; get delegate
    X86EmitIndexRegLoad(THIS_kREG, kESI, thisRegOffset);

#ifdef _TARGET_AMD64_

    INT32 numStackBytes = (INT32)((hash >> 8) * sizeof(void *));

    INT32 stackUsed, numStackArgs, ofs;

    // Push any stack args, plus an extra location
    // for rsp alignment if needed

    numStackArgs = numStackBytes / sizeof(void*);

    // 1 push above, so stack is currently misaligned
    const unsigned STACK_ALIGN_ADJUST = 0;

    if (!numStackArgs)
    {
        // sub rsp, 28h             ;; 4 reg arg home locs + rsp alignment
        stackUsed = 0x20 + STACK_ALIGN_ADJUST;
        X86EmitSubEsp(stackUsed);
    }
    else
    {
        stackUsed = numStackArgs * sizeof(void*);

        // If the stack is misaligned, then an odd number of arguments
        // will naturally align the stack.
        if (   ((numStackArgs & 1) == 0)
            != (STACK_ALIGN_ADJUST == 0))
        {
            X86EmitPushReg(kRAX);
            stackUsed += sizeof(void*);
        }

        ofs = SecureDelegateFrame::GetOffsetOfTransitionBlock() +
            TransitionBlock::GetOffsetOfArgs() + sizeof(ArgumentRegisters) + numStackBytes;

        while (numStackArgs--)
        {
            ofs -= sizeof(void*);

            // push [rsi + ofs]     ;; Push stack args
            X86EmitIndexPush(kESI, ofs);
        }

        // sub rsp, 20h             ;; Create 4 reg arg home locations
        X86EmitSubEsp(0x20);

        stackUsed += 0x20;
    }

    int thisArgNum = 0;

    for(
        argNum = 0, argOfs = SecureDelegateFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
        argNum < 4 && argTypes[argNum] != ELEMENT_TYPE_END;
        argNum++, argOfs += sizeof(void*)
        )
    {
        switch (argTypes[argNum])
        {
        case ELEMENT_TYPE_R4:
            // movss xmm?, dword ptr [rsi + argOfs]
            X64EmitMovSSFromMem((X86Reg)argNum, kRSI, argOfs);
            break;
        case ELEMENT_TYPE_R8:
            // movsd xmm?, qword ptr [rsi + argOfs]
            X64EmitMovSDFromMem((X86Reg)argNum, kRSI, argOfs);
            break;
        default:
            if (c_argRegs[argNum] != THIS_kREG)
            {
                // mov r*, [rsi + dstOfs]
                X86EmitIndexRegLoad(c_argRegs[argNum], kESI,argOfs);
            }
            break;
        } // switch
    }

    //    mov SCRATCHREG, [rcx+Delegate._invocationList]  ;;fetch the inner delegate
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfInvocationList());

    //    mov THISREG, [SCRATCHREG+Delegate.object]  ;;replace "this" pointer
    X86EmitIndexRegLoad(c_argRegs[thisArgNum], SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfTarget());

    // call [SCRATCHREG+Delegate.target] ;; call current subscriber
    X86EmitOffsetModRM(0xff, (X86Reg)2, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtr());

    // add rsp, stackUsed           ;; Clean up stack
    X86EmitAddEsp(stackUsed);

#else // _TARGET_AMD64_

    UINT16 numStackBytes = static_cast<UINT16>(hash & ~3);

    //    ..repush & reenregister args..
    INT32 ofs = numStackBytes + SecureDelegateFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs();
    while (ofs != SecureDelegateFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs())
    {
        ofs -= sizeof(void*);
        X86EmitIndexPush(kESI, ofs);
    }

    #define ARGUMENT_REGISTER(regname) if (k##regname != THIS_kREG) { X86EmitIndexRegLoad(k##regname, kESI, \
        offsetof(ArgumentRegisters, regname) + SecureDelegateFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters()); }

    ENUM_ARGUMENT_REGISTERS_BACKWARD();

    #undef ARGUMENT_REGISTER

    //    mov SCRATCHREG, [ecx+Delegate._invocationList]  ;;fetch the inner delegate
    X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfInvocationList());

    //    mov THISREG, [SCRATCHREG+Delegate.object]  ;;replace "this" pointer
    X86EmitIndexRegLoad(THIS_kREG, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfTarget());

    //    call [SCRATCHREG+Delegate.target] ;; call current subscriber
    X86EmitOffsetModRM(0xff, (X86Reg)2, SCRATCH_REGISTER_X86REG, DelegateObject::GetOffsetOfMethodPtr());
    INDEBUG(Emit8(0x90));       // Emit a nop after the call in debug so that
                                // we know that this is a call that can directly call
                                // managed code

#endif // _TARGET_AMD64_

    // The debugger may need to stop here, so grab the offset of this code.
    EmitPatchLabel();

    EmitCheckGSCookie(kESI, SecureDelegateFrame::GetOffsetOfGSCookie());

    // Epilog
    EmitMethodStubEpilog(numStackBytes, SecureDelegateFrame::GetOffsetOfTransitionBlock());
}

#ifndef FEATURE_ARRAYSTUB_AS_IL

// Little helper to generate code to move nbytes bytes of non Ref memory

void generate_noref_copy (unsigned nbytes, StubLinkerCPU* sl)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // If the size is pointer-aligned, we'll use movsd
    if (IS_ALIGNED(nbytes, sizeof(void*)))
    {
        // If there are less than 4 pointers to copy, "unroll" the "rep movsd"
        if (nbytes <= 3*sizeof(void*))
        {
            while (nbytes > 0)
            {
                // movsd
                sl->X86_64BitOperands();
                sl->Emit8(0xa5);

                nbytes -= sizeof(void*);
            }
        }
        else
        {
            // mov ECX, size / 4
            sl->Emit8(0xb8+kECX);
            sl->Emit32(nbytes / sizeof(void*));

            // repe movsd
            sl->Emit8(0xf3);
            sl->X86_64BitOperands();
            sl->Emit8(0xa5);
        }
    }
    else
    {
        // mov ECX, size
        sl->Emit8(0xb8+kECX);
        sl->Emit32(nbytes);

        // repe movsb
        sl->Emit16(0xa4f3);
    }
}


X86Reg LoadArrayOpArg (
        UINT32 idxloc,
        StubLinkerCPU *psl,
        X86Reg kRegIfFromMem,
        UINT ofsadjust
        AMD64_ARG(StubLinkerCPU::X86OperandSize OperandSize = StubLinkerCPU::k64BitOp)
        )
{
    STANDARD_VM_CONTRACT;

    if (!TransitionBlock::IsStackArgumentOffset(idxloc))
        return GetX86ArgumentRegisterFromOffset(idxloc - TransitionBlock::GetOffsetOfArgumentRegisters());

    psl->X86EmitEspOffset(0x8b, kRegIfFromMem, idxloc + ofsadjust AMD64_ARG(OperandSize));
    return kRegIfFromMem;
}

VOID StubLinkerCPU::EmitArrayOpStubThrow(unsigned exConst, unsigned cbRetArg)
{
    STANDARD_VM_CONTRACT;

    //ArrayOpStub*Exception
    X86EmitPopReg(kESI);
    X86EmitPopReg(kEDI);

    //mov CORINFO_NullReferenceException_ASM, %ecx
    Emit8(0xb8 | kECX);
    Emit32(exConst);
    //InternalExceptionWorker

    X86EmitPopReg(kEDX);
    // add pArrayOpScript->m_cbretpop, %esp (was add %eax, %esp)
    Emit8(0x81);
    Emit8(0xc0 | 0x4);
    Emit32(cbRetArg);
    X86EmitPushReg(kEDX);
    X86EmitNearJump(NewExternalCodeLabel((PVOID)JIT_InternalThrow));
}

//===========================================================================
// Emits code to do an array operation.
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID StubLinkerCPU::EmitArrayOpStub(const ArrayOpScript* pArrayOpScript)
{
    STANDARD_VM_CONTRACT;

    // This is the offset to the parameters/what's already pushed on the stack:
    // return address.
    const INT  locsize     = sizeof(void*);

    // ArrayOpScript's stack offsets are built using ArgIterator, which
    // assumes a TransitionBlock has been pushed, which is not the case
    // here.  rsp + ofsadjust should point at the first argument.  Any further
    // stack modifications below need to adjust ofsadjust appropriately.
    // baseofsadjust needs to be the stack adjustment at the entry point -
    // this is used further below to compute how much stack space was used.
    
    INT ofsadjust = locsize - (INT)sizeof(TransitionBlock);

    // Register usage
    //
    //                                          x86                 AMD64
    // Inputs:
    //  managed array                           THIS_kREG (ecx)     THIS_kREG (rcx)
    //  index 0                                 edx                 rdx
    //  index 1/value                           <stack>             r8
    //  index 2/value                           <stack>             r9
    //  expected element type for LOADADDR      eax                 rax                 rdx
    // Working registers:
    //  total (accumulates unscaled offset)     edi                 r10
    //  factor (accumulates the slice factor)   esi                 r11
    X86Reg kArrayRefReg = THIS_kREG;
#ifdef _TARGET_AMD64_
    const X86Reg kArrayMTReg  = kR10;
    const X86Reg kTotalReg    = kR10;
    const X86Reg kFactorReg   = kR11;
#else
    const X86Reg kArrayMTReg  = kESI;
    const X86Reg kTotalReg    = kEDI;
    const X86Reg kFactorReg   = kESI;
#endif

#ifdef _TARGET_AMD64_
    // Simplifying assumption for fNeedPrologue.
    _ASSERTE(!pArrayOpScript->m_gcDesc || (pArrayOpScript->m_flags & ArrayOpScript::NEEDSWRITEBARRIER));
    // Simplifying assumption for saving rsi and rdi.
    _ASSERTE(!(pArrayOpScript->m_flags & ArrayOpScript::HASRETVALBUFFER) || ArgIterator::IsArgPassedByRef(pArrayOpScript->m_elemsize));

    // Cases where we need to make calls
    BOOL fNeedScratchArea = (   (pArrayOpScript->m_flags & (ArrayOpScript::NEEDSTYPECHECK | ArrayOpScript::NEEDSWRITEBARRIER))
                             && (   pArrayOpScript->m_op == ArrayOpScript::STORE
                                 || (   pArrayOpScript->m_op == ArrayOpScript::LOAD
                                     && (pArrayOpScript->m_flags & ArrayOpScript::HASRETVALBUFFER))));

    // Cases where we need to copy large values
    BOOL fNeedRSIRDI = (   ArgIterator::IsArgPassedByRef(pArrayOpScript->m_elemsize)
                        && ArrayOpScript::LOADADDR != pArrayOpScript->m_op);

    BOOL fNeedPrologue = (   fNeedScratchArea
                          || fNeedRSIRDI);
#endif

    X86Reg       kValueReg;

    CodeLabel *Epilog = NewCodeLabel();
    CodeLabel *Inner_nullexception = NewCodeLabel();
    CodeLabel *Inner_rangeexception = NewCodeLabel();
    CodeLabel *Inner_typeMismatchexception = NULL;

    //
    // Set up the stack frame.
    //
    //
    // x86:
    //          value
    //          <index n-1>
    //          ...
    //          <index 1>
    //          return address
    //          saved edi
    // esp ->   saved esi
    //
    //
    // AMD64:
    //          value, if rank > 2
    //          ...
    // + 0x48   more indices
    // + 0x40   r9 home
    // + 0x38   r8 home
    // + 0x30   rdx home
    // + 0x28   rcx home
    // + 0x20   return address
    // + 0x18   scratch area (callee's r9)
    // + 0x10   scratch area (callee's r8)
    // +    8   scratch area (callee's rdx)
    // rsp ->   scratch area (callee's rcx)
    //
    // If the element type is a value class w/ object references, then rsi
    // and rdi will also be saved above the scratch area:
    //
    // ...
    // + 0x28   saved rsi
    // + 0x20   saved rdi
    // + 0x18   scratch area (callee's r9)
    // + 0x10   scratch area (callee's r8)
    // +    8   scratch area (callee's rdx)
    // rsp ->   scratch area (callee's rcx)
    //
    // And if no call or movsb is necessary, then the scratch area sits
    // directly under the MethodDesc*.

    BOOL fSavedESI = FALSE;
    BOOL fSavedEDI = FALSE;

#ifdef _TARGET_AMD64_
    if (fNeedPrologue)
    {
        // Save argument registers if we'll be making a call before using
        // them.  Note that in this case the element value will always be an
        // object type, and never be in an xmm register.

        if (   (pArrayOpScript->m_flags & ArrayOpScript::NEEDSTYPECHECK)
            && ArrayOpScript::STORE == pArrayOpScript->m_op)
        {
            // mov      [rsp+0x08], rcx
            X86EmitEspOffset(0x89, kRCX, 0x08);
            X86EmitEspOffset(0x89, kRDX, 0x10);
            X86EmitEspOffset(0x89, kR8,  0x18);

            if (pArrayOpScript->m_rank >= 2)
                X86EmitEspOffset(0x89, kR9, 0x20);
        }

        if (fNeedRSIRDI)
        {
            X86EmitPushReg(kRSI);
            X86EmitPushReg(kRDI);

            fSavedESI = fSavedEDI = TRUE;

            ofsadjust += 0x10;
        }

        if (fNeedScratchArea)
        {
            // Callee scratch area (0x8 for aligned esp)
            X86EmitSubEsp(sizeof(ArgumentRegisters) + 0x8);
            ofsadjust += sizeof(ArgumentRegisters) + 0x8;
        }
    }
#else
    // Preserve the callee-saved registers
    // NOTE: if you change the sequence of these pushes, you must also update:
    //  ArrayOpStubNullException
    //  ArrayOpStubRangeException
    //  ArrayOpStubTypeMismatchException
    _ASSERTE(      kTotalReg == kEDI);
    X86EmitPushReg(kTotalReg);
    _ASSERTE(      kFactorReg == kESI);
    X86EmitPushReg(kFactorReg);

    fSavedESI = fSavedEDI = TRUE;

    ofsadjust += 2*sizeof(void*);
#endif

    // Check for null.
    X86EmitR2ROp(0x85, kArrayRefReg, kArrayRefReg);             //   TEST ECX, ECX
    X86EmitCondJump(Inner_nullexception, X86CondCode::kJZ);     //   jz  Inner_nullexception

    // Do Type Check if needed
    if (pArrayOpScript->m_flags & ArrayOpScript::NEEDSTYPECHECK)
    {
        if (pArrayOpScript->m_op == ArrayOpScript::STORE)
        {
            // Get the value to be stored.
            kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, kEAX, ofsadjust);

            X86EmitR2ROp(0x85, kValueReg, kValueReg);                   // TEST kValueReg, kValueReg
            CodeLabel *CheckPassed = NewCodeLabel();
            X86EmitCondJump(CheckPassed, X86CondCode::kJZ);             // storing NULL is OK

                                                                        // mov EAX, element type ; possibly trashes kValueReg
            X86EmitOp(0x8b, kArrayMTReg, kArrayRefReg, 0 AMD64_ARG(k64BitOp)); // mov ESI/R10, [kArrayRefReg]

            X86EmitOp(0x8b, kEAX, kValueReg, 0 AMD64_ARG(k64BitOp));    // mov EAX, [kValueReg]  ; possibly trashes kValueReg
                                                                        // cmp EAX, [ESI/R10+m_ElementType]

            X86EmitOp(0x3b, kEAX, kArrayMTReg, MethodTable::GetOffsetOfArrayElementTypeHandle() AMD64_ARG(k64BitOp));
            X86EmitCondJump(CheckPassed, X86CondCode::kJZ);             // Exact match is OK

            X86EmitRegLoad(kEAX, (UINT_PTR)g_pObjectClass);             // mov EAX, g_pObjectMethodTable
                                                                        // cmp EAX, [ESI/R10+m_ElementType]

            X86EmitOp(0x3b, kEAX, kArrayMTReg, MethodTable::GetOffsetOfArrayElementTypeHandle() AMD64_ARG(k64BitOp));
            X86EmitCondJump(CheckPassed, X86CondCode::kJZ);             // Assigning to array of object is OK

            // Try to call the fast helper first ( ObjIsInstanceOfNoGC ).
            // If that fails we will fall back to calling the slow helper ( ArrayStoreCheck ) that erects a frame.
            // See also JitInterfaceX86::JIT_Stelem_Ref  
                                   
#ifdef _TARGET_AMD64_
            // RCX contains pointer to object to check (Object*)
            // RDX contains array type handle 
            
            // mov RCX, [rsp+offsetToObject] ; RCX = Object*
            X86EmitEspOffset(0x8b, kRCX, ofsadjust + pArrayOpScript->m_fValLoc); 

            // get Array TypeHandle 
            // mov RDX, [RSP+offsetOfTypeHandle]                       

            X86EmitEspOffset(0x8b, kRDX,   ofsadjust
                                         + TransitionBlock::GetOffsetOfArgumentRegisters()
                                         + FIELD_OFFSET(ArgumentRegisters, THIS_REG));
                                 
            // mov RDX, [kArrayMTReg+offsetof(MethodTable, m_ElementType)]
            X86EmitIndexRegLoad(kRDX, kArrayMTReg, MethodTable::GetOffsetOfArrayElementTypeHandle());
            
#else
            X86EmitPushReg(kEDX);      // Save EDX
            X86EmitPushReg(kECX);      // Pass array object

            X86EmitIndexPush(kArrayMTReg, MethodTable::GetOffsetOfArrayElementTypeHandle()); // push [kArrayMTReg + m_ElementType] ; Array element type handle 

            // get address of value to store
            _ASSERTE(TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc)); // on x86, value will never get a register
            X86EmitSPIndexPush(pArrayOpScript->m_fValLoc + ofsadjust + 3*sizeof(void*)); // push [ESP+offset] ; the object pointer

#endif //_AMD64
                        
          
            // emit a call to the fast helper            
            // One side effect of this is that we are going to generate a "jnz Epilog" and we DON'T need it 
            // in the fast path, however there are no side effects in emitting
            // it in the fast path anyway. the reason for that is that it makes
            // the cleanup code much easier ( we have only 1 place to cleanup the stack and
            // restore it to the original state )
            X86EmitCall(NewExternalCodeLabel((LPVOID)ObjIsInstanceOfNoGC), 0);
            X86EmitCmpRegImm32( kEAX, TypeHandle::CanCast); // CMP EAX, CanCast ; if ObjIsInstanceOfNoGC returns CanCast, we will go the fast path
            CodeLabel * Cleanup = NewCodeLabel();
            X86EmitCondJump(Cleanup, X86CondCode::kJZ);
                                               
#ifdef _TARGET_AMD64_
            // get address of value to store
            // lea rcx, [rsp+offs]
            X86EmitEspOffset(0x8d, kRCX,   ofsadjust + pArrayOpScript->m_fValLoc);

            // get address of 'this'/rcx
            // lea rdx, [rsp+offs]
            X86EmitEspOffset(0x8d, kRDX,   ofsadjust
                                         + TransitionBlock::GetOffsetOfArgumentRegisters()
                                         + FIELD_OFFSET(ArgumentRegisters, THIS_REG));

#else           
            // The stack is already setup correctly for the slow helper.            
            _ASSERTE(TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc)); // on x86, value will never get a register
            X86EmitEspOffset(0x8d, kECX, pArrayOpScript->m_fValLoc + ofsadjust + 2*sizeof(void*));      // lea ECX, [ESP+offset]
            
            // get address of 'this'
            X86EmitEspOffset(0x8d, kEDX, 0);    // lea EDX, [ESP]       ; (address of ECX) 

            
#endif 
            AMD64_ONLY(_ASSERTE(fNeedScratchArea));
            X86EmitCall(NewExternalCodeLabel((LPVOID)ArrayStoreCheck), 0);
          
            EmitLabel(Cleanup);           
#ifdef _TARGET_AMD64_
            X86EmitEspOffset(0x8b, kRCX, 0x00 + ofsadjust + TransitionBlock::GetOffsetOfArgumentRegisters());
            X86EmitEspOffset(0x8b, kRDX, 0x08 + ofsadjust + TransitionBlock::GetOffsetOfArgumentRegisters());
            X86EmitEspOffset(0x8b, kR8, 0x10 + ofsadjust + TransitionBlock::GetOffsetOfArgumentRegisters());

            if (pArrayOpScript->m_rank >= 2)
                X86EmitEspOffset(0x8b, kR9, 0x18 + ofsadjust + TransitionBlock::GetOffsetOfArgumentRegisters());
#else
            X86EmitPopReg(kECX);        // restore regs
            X86EmitPopReg(kEDX);

            
            X86EmitR2ROp(0x3B, kEAX, kEAX);                             //   CMP EAX, EAX
            X86EmitCondJump(Epilog, X86CondCode::kJNZ);         // This branch never taken, but epilog walker uses it
#endif

            EmitLabel(CheckPassed);
        }
        else
        {
            _ASSERTE(pArrayOpScript->m_op == ArrayOpScript::LOADADDR);

            // Load up the hidden type parameter into 'typeReg'
            X86Reg typeReg = LoadArrayOpArg(pArrayOpScript->m_typeParamOffs, this, kEAX, ofsadjust);

            // 'typeReg' holds the typeHandle for the ARRAY.  This must be a ArrayTypeDesc*, so
            // mask off the low two bits to get the TypeDesc*
            X86EmitR2ROp(0x83, (X86Reg)4, typeReg);    //   AND typeReg, 0xFFFFFFFC
            Emit8(0xFC);

            // If 'typeReg' is NULL then we're executing the readonly ::Address and no type check is
            // needed.
            CodeLabel *Inner_passedTypeCheck = NewCodeLabel();

            X86EmitCondJump(Inner_passedTypeCheck, X86CondCode::kJZ);
            
            // Get the parameter of the parameterize type
            // mov typeReg, [typeReg.m_Arg]
            X86EmitOp(0x8b, typeReg, typeReg, offsetof(ParamTypeDesc, m_Arg) AMD64_ARG(k64BitOp));
            
            // Compare this against the element type of the array.
            // mov ESI/R10, [kArrayRefReg]
            X86EmitOp(0x8b, kArrayMTReg, kArrayRefReg, 0 AMD64_ARG(k64BitOp));
            // cmp typeReg, [ESI/R10+m_ElementType];
            X86EmitOp(0x3b, typeReg, kArrayMTReg, MethodTable::GetOffsetOfArrayElementTypeHandle() AMD64_ARG(k64BitOp));

            // Throw error if not equal
            Inner_typeMismatchexception = NewCodeLabel();
            X86EmitCondJump(Inner_typeMismatchexception, X86CondCode::kJNZ);
            EmitLabel(Inner_passedTypeCheck);
        }
    }

    CodeLabel* DoneCheckLabel = 0;
    if (pArrayOpScript->m_rank == 1 && pArrayOpScript->m_fHasLowerBounds)
    {
        DoneCheckLabel = NewCodeLabel();
        CodeLabel* NotSZArrayLabel = NewCodeLabel();

        // for rank1 arrays, we might actually have two different layouts depending on
        // if we are ELEMENT_TYPE_ARRAY or ELEMENT_TYPE_SZARRAY.

            // mov EAX, [ARRAY]          // EAX holds the method table
        X86_64BitOperands();
        X86EmitOp(0x8b, kEAX, kArrayRefReg);

            // test [EAX + m_dwFlags], enum_flag_Category_IfArrayThenSzArray
        X86_64BitOperands();
        X86EmitOffsetModRM(0xf7, (X86Reg)0, kEAX, MethodTable::GetOffsetOfFlags());
        Emit32(MethodTable::GetIfArrayThenSzArrayFlag());

            // jz NotSZArrayLabel
        X86EmitCondJump(NotSZArrayLabel, X86CondCode::kJZ);

            //Load the passed-in index into the scratch register.
        const ArrayOpIndexSpec *pai = pArrayOpScript->GetArrayOpIndexSpecs();
        X86Reg idxReg = LoadArrayOpArg(pai->m_idxloc, this, SCRATCH_REGISTER_X86REG, ofsadjust);

            // cmp idxReg, [kArrayRefReg + LENGTH]
        X86EmitOp(0x3b, idxReg, kArrayRefReg, ArrayBase::GetOffsetOfNumComponents());

            // jae Inner_rangeexception
        X86EmitCondJump(Inner_rangeexception, X86CondCode::kJAE);

            // <TODO> if we cared efficiency of this, this move can be optimized</TODO>
        X86EmitR2ROp(0x8b, kTotalReg, idxReg AMD64_ARG(k32BitOp));

            // sub ARRAY. 8                  // 8 is accounts for the Lower bound and Dim count in the ARRAY
        X86EmitSubReg(kArrayRefReg, 8);      // adjust this pointer so that indexing works out for SZARRAY

        X86EmitNearJump(DoneCheckLabel);
        EmitLabel(NotSZArrayLabel);
    }

    // For each index, range-check and mix into accumulated total.
    UINT idx = pArrayOpScript->m_rank;
    BOOL firstTime = TRUE;
    while (idx--)
    {
        const ArrayOpIndexSpec *pai = pArrayOpScript->GetArrayOpIndexSpecs() + idx;

        //Load the passed-in index into the scratch register.
        X86Reg srcreg = LoadArrayOpArg(pai->m_idxloc, this, SCRATCH_REGISTER_X86REG, ofsadjust AMD64_ARG(k32BitOp));
        if (SCRATCH_REGISTER_X86REG != srcreg)
            X86EmitR2ROp(0x8b, SCRATCH_REGISTER_X86REG, srcreg AMD64_ARG(k32BitOp));

        // sub SCRATCH, dword ptr [kArrayRefReg + LOWERBOUND]
        if (pArrayOpScript->m_fHasLowerBounds)
        {
            X86EmitOp(0x2b, SCRATCH_REGISTER_X86REG, kArrayRefReg, pai->m_lboundofs);
        }

        // cmp SCRATCH, dword ptr [kArrayRefReg + LENGTH]
        X86EmitOp(0x3b, SCRATCH_REGISTER_X86REG, kArrayRefReg, pai->m_lengthofs);

        // jae Inner_rangeexception
        X86EmitCondJump(Inner_rangeexception, X86CondCode::kJAE);


        // SCRATCH == idx - LOWERBOUND
        //
        // imul SCRATCH, FACTOR
        if (!firstTime)
        {
            //Can skip the first time since FACTOR==1
            X86EmitR2ROp(0xaf0f, SCRATCH_REGISTER_X86REG, kFactorReg AMD64_ARG(k32BitOp));
        }

        // TOTAL += SCRATCH
        if (firstTime)
        {
            // First time, we must zero-init TOTAL. Since
            // zero-initing and then adding is just equivalent to a
            // "mov", emit a "mov"
            //    mov  TOTAL, SCRATCH
            X86EmitR2ROp(0x8b, kTotalReg, SCRATCH_REGISTER_X86REG AMD64_ARG(k32BitOp));
        }
        else
        {
            //    add  TOTAL, SCRATCH
            X86EmitR2ROp(0x03, kTotalReg, SCRATCH_REGISTER_X86REG AMD64_ARG(k32BitOp));
        }

        // FACTOR *= [kArrayRefReg + LENGTH]
        if (idx != 0)
        {
            // No need to update FACTOR on the last iteration
            //  since we won't use it again

            if (firstTime)
            {
                // must init FACTOR to 1 first: hence,
                // the "imul" becomes a "mov"
                // mov FACTOR, [kArrayRefReg + LENGTH]
                X86EmitOp(0x8b, kFactorReg, kArrayRefReg, pai->m_lengthofs);
            }
            else
            {
                // imul FACTOR, [kArrayRefReg + LENGTH]
                X86EmitOp(0xaf0f, kFactorReg, kArrayRefReg, pai->m_lengthofs);
            }
        }

        firstTime = FALSE;
    }

    if (DoneCheckLabel != 0)
        EmitLabel(DoneCheckLabel);

    // Pass these values to X86EmitArrayOp() to generate the element address.
    X86Reg elemBaseReg   = kArrayRefReg;
    X86Reg elemScaledReg = kTotalReg;
    UINT32 elemSize      = pArrayOpScript->m_elemsize;
    UINT32 elemOfs       = pArrayOpScript->m_ofsoffirst;

    if (!(elemSize == 1 || elemSize == 2 || elemSize == 4 || elemSize == 8))
    {
        switch (elemSize)
        {
            // No way to express this as a SIB byte. Fold the scale
            // into TOTAL.

            case 16:
                // shl TOTAL,4
                X86EmitR2ROp(0xc1, (X86Reg)4, kTotalReg AMD64_ARG(k32BitOp));
                Emit8(4);
                break;

            case 32:
                // shl TOTAL,5
                X86EmitR2ROp(0xc1, (X86Reg)4, kTotalReg AMD64_ARG(k32BitOp));
                Emit8(5);
                break;

            case 64:
                // shl TOTAL,6
                X86EmitR2ROp(0xc1, (X86Reg)4, kTotalReg AMD64_ARG(k32BitOp));
                Emit8(6);
                break;

            default:
                // imul TOTAL, elemScale
                X86EmitR2ROp(0x69, kTotalReg, kTotalReg AMD64_ARG(k32BitOp));
                Emit32(elemSize);
                break;
        }
        elemSize = 1;
    }

    _ASSERTE(FitsInU1(elemSize));
    BYTE elemScale = static_cast<BYTE>(elemSize);

    // Now, do the operation:

    switch (pArrayOpScript->m_op)
    {
        case ArrayOpScript::LOADADDR:
            // lea eax, ELEMADDR
            X86EmitOp(0x8d, kEAX, elemBaseReg, elemOfs, elemScaledReg, elemScale AMD64_ARG(k64BitOp));
            break;

        case ArrayOpScript::LOAD:
            if (pArrayOpScript->m_flags & ArrayOpScript::HASRETVALBUFFER)
            {
                // Ensure that these registers have been saved!
                _ASSERTE(fSavedESI && fSavedEDI);

                //lea esi, ELEMADDR
                X86EmitOp(0x8d, kESI, elemBaseReg, elemOfs, elemScaledReg, elemScale AMD64_ARG(k64BitOp));

                _ASSERTE(!TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fRetBufLoc));
                // mov edi, retbufptr
                X86EmitR2ROp(0x8b, kEDI, GetX86ArgumentRegisterFromOffset(pArrayOpScript->m_fRetBufLoc - TransitionBlock::GetOffsetOfArgumentRegisters()));

COPY_VALUE_CLASS:
                {
                    size_t size = pArrayOpScript->m_elemsize;
                    size_t total = 0;
                    if(pArrayOpScript->m_gcDesc)
                    {
                        CGCDescSeries* cur = pArrayOpScript->m_gcDesc->GetHighestSeries();
                        if ((cur->startoffset-elemOfs) > 0)
                            generate_noref_copy ((unsigned) (cur->startoffset - elemOfs), this);
                        total += cur->startoffset - elemOfs;

                        SSIZE_T cnt = (SSIZE_T) pArrayOpScript->m_gcDesc->GetNumSeries();
                        // special array encoding
                        _ASSERTE(cnt < 0);

                        for (SSIZE_T __i = 0; __i > cnt; __i--)
                        {
                            HALF_SIZE_T skip =  cur->val_serie[__i].skip;
                            HALF_SIZE_T nptrs = cur->val_serie[__i].nptrs;
                            total += nptrs*sizeof (DWORD*);
                            do
                            {
                                AMD64_ONLY(_ASSERTE(fNeedScratchArea));

                                X86EmitCall(NewExternalCodeLabel((LPVOID) JIT_ByRefWriteBarrier), 0);
                            } while (--nptrs);
                            if (skip > 0)
                            {
                                //check if we are at the end of the series
                                if (__i == (cnt + 1))
                                    skip = skip - (HALF_SIZE_T)(cur->startoffset - elemOfs);
                                if (skip > 0)
                                    generate_noref_copy (skip, this);
                            }
                            total += skip;
                        }

                        _ASSERTE (size == total);
                    }
                    else
                    {
                        // no ref anywhere, just copy the bytes.
                        _ASSERTE (size);
                        generate_noref_copy ((unsigned)size, this);
                    }
                }
            }
            else
            {
                switch (pArrayOpScript->m_elemsize)
                {
                case 1:
                    // mov[zs]x eax, byte ptr ELEMADDR
                    X86EmitOp(pArrayOpScript->m_signed ? 0xbe0f : 0xb60f, kEAX, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                    break;

                case 2:
                    // mov[zs]x eax, word ptr ELEMADDR
                    X86EmitOp(pArrayOpScript->m_signed ? 0xbf0f : 0xb70f, kEAX, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                    break;

                case 4:
                    if (pArrayOpScript->m_flags & ArrayOpScript::ISFPUTYPE)
                    {
#ifdef _TARGET_AMD64_
                        // movss xmm0, dword ptr ELEMADDR
                        Emit8(0xf3);
                        X86EmitOp(0x100f, (X86Reg)0, elemBaseReg, elemOfs, elemScaledReg, elemScale);
#else // !_TARGET_AMD64_
                        // fld dword ptr ELEMADDR
                        X86EmitOp(0xd9, (X86Reg)0, elemBaseReg, elemOfs, elemScaledReg, elemScale);
#endif // !_TARGET_AMD64_
                    }
                    else
                    {
                        // mov eax, ELEMADDR
                        X86EmitOp(0x8b, kEAX, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                    }
                    break;

                case 8:
                    if (pArrayOpScript->m_flags & ArrayOpScript::ISFPUTYPE)
                    {
#ifdef _TARGET_AMD64_
                        // movsd xmm0, qword ptr ELEMADDR
                        Emit8(0xf2);
                        X86EmitOp(0x100f, (X86Reg)0, elemBaseReg, elemOfs, elemScaledReg, elemScale);
#else // !_TARGET_AMD64_
                        // fld qword ptr ELEMADDR
                        X86EmitOp(0xdd, (X86Reg)0, elemBaseReg, elemOfs, elemScaledReg, elemScale);
#endif // !_TARGET_AMD64_
                    }
                    else
                    {
                        // mov eax, ELEMADDR
                        X86EmitOp(0x8b, kEAX, elemBaseReg, elemOfs, elemScaledReg, elemScale AMD64_ARG(k64BitOp));
#ifdef _TARGET_X86_
                        // mov edx, ELEMADDR + 4
                        X86EmitOp(0x8b, kEDX, elemBaseReg, elemOfs + 4, elemScaledReg, elemScale);
#endif
                    }
                    break;

                default:
                    _ASSERTE(0);
                }
            }

            break;

        case ArrayOpScript::STORE:

            switch (pArrayOpScript->m_elemsize)
            {
            case 1:
                // mov SCRATCH, [esp + valoffset]
                kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, SCRATCH_REGISTER_X86REG, ofsadjust);
                // mov byte ptr ELEMADDR, SCRATCH.b
                X86EmitOp(0x88, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                break;
            case 2:
                // mov SCRATCH, [esp + valoffset]
                kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, SCRATCH_REGISTER_X86REG, ofsadjust);
                // mov word ptr ELEMADDR, SCRATCH.w
                Emit8(0x66);
                X86EmitOp(0x89, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                break;
            case 4:
#ifndef _TARGET_AMD64_
                if (pArrayOpScript->m_flags & ArrayOpScript::NEEDSWRITEBARRIER)
                {
                    // mov SCRATCH, [esp + valoffset]
                    kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, SCRATCH_REGISTER_X86REG, ofsadjust);

                    _ASSERTE(SCRATCH_REGISTER_X86REG == kEAX); // value to store is already in EAX where we want it.
                    // lea edx, ELEMADDR
                    X86EmitOp(0x8d, kEDX, elemBaseReg, elemOfs, elemScaledReg, elemScale);

                    // call JIT_Writeable_Thunks_Buf.WriteBarrierReg[0] (== EAX)
                    X86EmitCall(NewExternalCodeLabel((LPVOID) &JIT_WriteBarrierEAX), 0);
                }
                else
#else // _TARGET_AMD64_
                if (pArrayOpScript->m_flags & ArrayOpScript::ISFPUTYPE)
                {
                    if (!TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc))
                    {
                        kValueReg = (X86Reg)TransitionBlock::GetArgumentIndexFromOffset(pArrayOpScript->m_fValLoc);
                    }
                    else
                    {
                        kValueReg = (X86Reg)0;  // xmm0

                        // movss xmm0, dword ptr [rsp+??]
                        Emit8(0xf3);
                        X86EmitOp(0x100f, kValueReg, (X86Reg)4 /*rsp*/, ofsadjust + pArrayOpScript->m_fValLoc);
                    }

                    // movss dword ptr ELEMADDR, xmm?
                    Emit8(0xf3);
                    X86EmitOp(0x110f, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                }
                else
#endif // _TARGET_AMD64_
                {
                    // mov SCRATCH, [esp + valoffset]
                    kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, SCRATCH_REGISTER_X86REG, ofsadjust AMD64_ARG(k32BitOp));

                    // mov ELEMADDR, SCRATCH
                    X86EmitOp(0x89, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                }
                break;

            case 8:

                if (!(pArrayOpScript->m_flags & ArrayOpScript::NEEDSWRITEBARRIER))
                {
#ifdef _TARGET_AMD64_
                    if (pArrayOpScript->m_flags & ArrayOpScript::ISFPUTYPE)
                    {
                        if (!TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc))
                        {
                            kValueReg = (X86Reg)TransitionBlock::GetArgumentIndexFromOffset(pArrayOpScript->m_fValLoc);
                        }
                        else
                        {
                            kValueReg = (X86Reg)0;  // xmm0

                            // movsd xmm0, qword ptr [rsp+??]
                            Emit8(0xf2);
                            X86EmitOp(0x100f, kValueReg, (X86Reg)4 /*rsp*/, ofsadjust + pArrayOpScript->m_fValLoc);
                        }

                        // movsd qword ptr ELEMADDR, xmm?
                        Emit8(0xf2);
                        X86EmitOp(0x110f, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale);
                    }
                    else
                    {
                    // mov SCRATCH, [esp + valoffset]
                        kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, SCRATCH_REGISTER_X86REG, ofsadjust);

                        // mov ELEMADDR, SCRATCH
                        X86EmitOp(0x89, kValueReg, elemBaseReg, elemOfs, elemScaledReg, elemScale, k64BitOp);
                    }
#else // !_TARGET_AMD64_
                    _ASSERTE(TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc)); // on x86, value will never get a register: so too lazy to implement that case
                    // mov SCRATCH, [esp + valoffset]
                    X86EmitEspOffset(0x8b, SCRATCH_REGISTER_X86REG, pArrayOpScript->m_fValLoc + ofsadjust);
                    // mov ELEMADDR, SCRATCH
                    X86EmitOp(0x89, SCRATCH_REGISTER_X86REG, elemBaseReg, elemOfs, elemScaledReg, elemScale);

                    _ASSERTE(TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc)); // on x86, value will never get a register: so too lazy to implement that case
                    // mov SCRATCH, [esp + valoffset + 4]
                    X86EmitEspOffset(0x8b, SCRATCH_REGISTER_X86REG, pArrayOpScript->m_fValLoc + ofsadjust + 4);
                    // mov ELEMADDR+4, SCRATCH
                    X86EmitOp(0x89, SCRATCH_REGISTER_X86REG, elemBaseReg, elemOfs+4, elemScaledReg, elemScale);
#endif // !_TARGET_AMD64_
                    break;
                }
#ifdef _TARGET_AMD64_
                else
                {
                    _ASSERTE(SCRATCH_REGISTER_X86REG == kEAX); // value to store is already in EAX where we want it.
                    // lea rcx, ELEMADDR
                    X86EmitOp(0x8d, kRCX, elemBaseReg, elemOfs, elemScaledReg, elemScale, k64BitOp);

                    // mov rdx, [rsp + valoffset]
                    kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, kRDX, ofsadjust);
                    _ASSERT(kRCX != kValueReg);
                    if (kRDX != kValueReg)
                        X86EmitR2ROp(0x8b, kRDX, kValueReg);

                    _ASSERTE(fNeedScratchArea);
                    X86EmitCall(NewExternalCodeLabel((PVOID)JIT_WriteBarrier), 0);
                    break;
                }
#endif // _TARGET_AMD64_
                    // FALL THROUGH (on x86)
            default:
                // Ensure that these registers have been saved!
                _ASSERTE(fSavedESI && fSavedEDI);

#ifdef _TARGET_AMD64_
                // mov rsi, [rsp + valoffset]
                kValueReg = LoadArrayOpArg(pArrayOpScript->m_fValLoc, this, kRSI, ofsadjust);
                if (kRSI != kValueReg)
                    X86EmitR2ROp(0x8b, kRSI, kValueReg);
#else // !_TARGET_AMD64_
                _ASSERTE(TransitionBlock::IsStackArgumentOffset(pArrayOpScript->m_fValLoc));
                // lea esi, [esp + valoffset]
                X86EmitEspOffset(0x8d, kESI, pArrayOpScript->m_fValLoc + ofsadjust);
#endif // !_TARGET_AMD64_

                // lea edi, ELEMADDR
                X86EmitOp(0x8d, kEDI, elemBaseReg, elemOfs, elemScaledReg, elemScale AMD64_ARG(k64BitOp));
                goto COPY_VALUE_CLASS;
            }
            break;

        default:
            _ASSERTE(0);
    }

    EmitLabel(Epilog);

#ifdef _TARGET_AMD64_
    if (fNeedPrologue)
    {
        if (fNeedScratchArea)
        {
            // Throw away scratch area
            X86EmitAddEsp(sizeof(ArgumentRegisters) + 0x8);
        }

        if (fSavedEDI)
            X86EmitPopReg(kRDI);

        if (fSavedESI)
            X86EmitPopReg(kRSI);
    }

    X86EmitReturn(0);
#else // !_TARGET_AMD64_
    // Restore the callee-saved registers
    X86EmitPopReg(kFactorReg);
    X86EmitPopReg(kTotalReg);

    // ret N
    X86EmitReturn(pArrayOpScript->m_cbretpop);
#endif // !_TARGET_AMD64_

    // Exception points must clean up the stack for all those extra args.
    // kFactorReg and kTotalReg will be popped by the jump targets.

    void *pvExceptionThrowFn;

#if defined(_TARGET_AMD64_)
#define ARRAYOP_EXCEPTION_HELPERS(base)      { (PVOID)base, (PVOID)base##_RSIRDI, (PVOID)base##_ScratchArea, (PVOID)base##_RSIRDI_ScratchArea }
 static void *rgNullExceptionHelpers[]           = ARRAYOP_EXCEPTION_HELPERS(ArrayOpStubNullException);
    static void *rgRangeExceptionHelpers[]          = ARRAYOP_EXCEPTION_HELPERS(ArrayOpStubRangeException);
    static void *rgTypeMismatchExceptionHelpers[]   = ARRAYOP_EXCEPTION_HELPERS(ArrayOpStubTypeMismatchException);
#undef ARRAYOP_EXCEPTION_HELPERS

    UINT iExceptionHelper = (fNeedRSIRDI ? 1 : 0) + (fNeedScratchArea ? 2 : 0);
#endif // defined(_TARGET_AMD64_)

    EmitLabel(Inner_nullexception);

#ifndef _TARGET_AMD64_
    pvExceptionThrowFn = (LPVOID)ArrayOpStubNullException;

    Emit8(0xb8);        // mov EAX, <stack cleanup>
    Emit32(pArrayOpScript->m_cbretpop);
#else //_TARGET_AMD64_
    pvExceptionThrowFn = rgNullExceptionHelpers[iExceptionHelper];
#endif //!_TARGET_AMD64_
    X86EmitNearJump(NewExternalCodeLabel(pvExceptionThrowFn));

    EmitLabel(Inner_rangeexception);
#ifndef _TARGET_AMD64_
    pvExceptionThrowFn = (LPVOID)ArrayOpStubRangeException;
    Emit8(0xb8);        // mov EAX, <stack cleanup>
    Emit32(pArrayOpScript->m_cbretpop);
#else //_TARGET_AMD64_
    pvExceptionThrowFn = rgRangeExceptionHelpers[iExceptionHelper];
#endif //!_TARGET_AMD64_
    X86EmitNearJump(NewExternalCodeLabel(pvExceptionThrowFn));

    if (Inner_typeMismatchexception != NULL)
    {
        EmitLabel(Inner_typeMismatchexception);
#ifndef _TARGET_AMD64_
        pvExceptionThrowFn = (LPVOID)ArrayOpStubTypeMismatchException;
        Emit8(0xb8);        // mov EAX, <stack cleanup>
        Emit32(pArrayOpScript->m_cbretpop);
#else //_TARGET_AMD64_
        pvExceptionThrowFn = rgTypeMismatchExceptionHelpers[iExceptionHelper];
#endif //!_TARGET_AMD64_
        X86EmitNearJump(NewExternalCodeLabel(pvExceptionThrowFn));
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // FEATURE_ARRAYSTUB_AS_IL

//===========================================================================
// Emits code to break into debugger
VOID StubLinkerCPU::EmitDebugBreak()
{
    STANDARD_VM_CONTRACT;

    // int3
    Emit8(0xCC);
}

#if defined(FEATURE_COMINTEROP) && defined(_TARGET_X86_)

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning (disable : 4740) // There is inline asm code in this function, which disables
                                 // global optimizations.
#pragma warning (disable : 4731)
#endif  // _MSC_VER
Thread* __stdcall CreateThreadBlockReturnHr(ComMethodFrame *pFrame)
{

    WRAPPER_NO_CONTRACT;

    Thread *pThread = NULL;

    HRESULT hr = S_OK;

    // This means that a thread is FIRST coming in from outside the EE.
    BEGIN_ENTRYPOINT_THROWS;
    pThread = SetupThreadNoThrow(&hr);
    END_ENTRYPOINT_THROWS;

    if (pThread == NULL) {
        // Unwind stack, and return hr
        // NOTE: assumes __stdcall
        // Note that this code does not handle the rare COM signatures that do not return HRESULT
        // compute the callee pop stack bytes
        UINT numArgStackBytes = pFrame->GetNumCallerStackBytes();
        unsigned frameSize = sizeof(Frame) + sizeof(LPVOID);
        LPBYTE iEsp = ((LPBYTE)pFrame) + ComMethodFrame::GetOffsetOfCalleeSavedRegisters();
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

#endif // defined(FEATURE_COMINTEROP) && defined(_TARGET_X86_)

#endif // !defined(CROSSGEN_COMPILE) && !defined(FEATURE_STUBS_AS_IL)

#endif // !DACCESS_COMPILE


#ifdef _TARGET_AMD64_

//
// TailCallFrame Object Scanning
//
// This handles scanning/promotion of GC objects that were
// protected by the TailCallHelper routine.  Note that the objects
// being protected is somewhat dynamic and is dependent upon the
// the callee...
//

void TailCallFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    if (m_pGCLayout != NULL)
    {
        struct FrameOffsetDecoder {
        private:
            TADDR prevOffset;
            TADDR rangeEnd;
            BOOL   maybeInterior;
            BOOL   atEnd;
            PTR_SBYTE pbOffsets;

            DWORD ReadNumber() {
                signed char i;
                DWORD offset = 0;
                while ((i = *pbOffsets++) >= 0) 
                {
                    offset = (offset << 7) | i;
                }
                offset = (offset << 7) | (i & 0x7F);
                return offset;
            }

        public:
            FrameOffsetDecoder(PTR_GSCookie _base, TADDR offsets)
                : prevOffset(dac_cast<TADDR>(_base)), rangeEnd(~0LL), atEnd(FALSE), pbOffsets(dac_cast<PTR_SBYTE>(offsets)) { maybeInterior = FALSE;}

            bool MoveNext() {
                LIMITED_METHOD_CONTRACT;

                if (rangeEnd < prevOffset)
                {
                    prevOffset -= sizeof(void*);
                    return true;
                }
                if (atEnd) return false;
                DWORD offset = ReadNumber();
                atEnd = (offset & 1);
                BOOL range = (offset & 2);
                maybeInterior = (offset & 0x80000000);

                offset &= 0x7FFFFFFC;
                
#ifdef _WIN64
                offset <<= 1;
#endif
                offset += sizeof(void*);
                _ASSERTE(prevOffset > offset);
                prevOffset -= offset;

                if (range)
                {
                    _ASSERTE(!atEnd);
                    _ASSERTE(!maybeInterior);
                    DWORD offsetEnd = ReadNumber();
                    atEnd = (offsetEnd & 1);
                    offsetEnd = (offsetEnd & ~1) << 1;
                    // range encoding starts with a range of 3 (2 is better to encode as
                    // 2 offsets), so 0 == 3
                    offsetEnd += sizeof(void*) * 3;
                    rangeEnd = prevOffset - offsetEnd;
                }

                return true;
            }

            BOOL MaybeInterior() const { return maybeInterior; }

            PTR_PTR_Object Current() const { return PTR_PTR_Object(prevOffset); }

        } decoder(GetGSCookiePtr(), m_pGCLayout);

        while (decoder.MoveNext())
        {
            PTR_PTR_Object ppRef = decoder.Current();

            LOG((LF_GC, INFO3, "Tail Call Frame Promoting" FMT_ADDR "to",
                 DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(*ppRef)) ));
            if (decoder.MaybeInterior())
                PromoteCarefully(fn, ppRef, sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
            else
                (*fn)(ppRef, sc, 0);
            LOG((LF_GC, INFO3, FMT_ADDR "\n", DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(*ppRef)) ));
        }
    }
}

#ifndef DACCESS_COMPILE
static void EncodeOneGCOffset(CPUSTUBLINKER *pSl, ULONG delta, BOOL maybeInterior, BOOL range, BOOL last)
{
    CONTRACTL
    {
        THROWS; // From the stublinker
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Everything should be pointer aligned
    // but we use a high bit for interior, and the 0 bit to denote the end of the list
    // we use the 1 bit to denote a range
    _ASSERTE((delta % sizeof(void*)) == 0);

#if defined(_WIN64)
    // For 64-bit, we have 3 bits of alignment, so we allow larger frames
    // by shifting and gaining a free high-bit.
    ULONG encodedDelta = delta >> 1;
#else
    // For 32-bit, we just limit our frame size to <2GB. (I know, such a bummer!)
    ULONG encodedDelta = delta;
#endif
    _ASSERTE((encodedDelta & 0x80000003) == 0);
    if (last)
    {
        encodedDelta |= 1;
    }

    if (range)
    {
        encodedDelta |= 2;
    }
    else if (maybeInterior)
    {
        _ASSERTE(!range);
        encodedDelta |= 0x80000000;
    }

    BYTE bytes[5];
    UINT index = 5;
    bytes[--index] = (BYTE)((encodedDelta & 0x7F) | 0x80);
    encodedDelta >>= 7;
    while (encodedDelta > 0)
    {
        bytes[--index] = (BYTE)(encodedDelta & 0x7F);
        encodedDelta >>= 7;
    }
    pSl->EmitBytes(&bytes[index], 5 - index);
}

static void EncodeGCOffsets(CPUSTUBLINKER *pSl, /* const */ ULONGARRAY & gcOffsets)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(gcOffsets.Count() > 0);

    ULONG prevOffset = 0;
    int i = 0;
    BOOL last = FALSE;
    do {
        ULONG offset = gcOffsets[i];
        // Everything should be pointer aligned
        // but we use the 0-bit to mean maybeInterior, for byrefs.
        _ASSERTE(((offset % sizeof(void*)) == 0) || ((offset % sizeof(void*)) == 1));
        BOOL maybeInterior = (offset & 1);
        offset &= ~1;

        // Encode just deltas because they're smaller (and the list should be sorted)
        _ASSERTE(offset >= (prevOffset + sizeof(void*)));
        ULONG delta = offset - (prevOffset + sizeof(void*));
        if (!maybeInterior && gcOffsets.Count() > i + 2)
        {
            // Check for a potential range.
            // Only do it if we have 3 or more pointers in a row
            ULONG rangeOffset = offset;
            int j = i + 1;
            do {
                ULONG nextOffset = gcOffsets[j];
                // interior pointers can't be in ranges
                if (nextOffset & 1)
                     break;
                // ranges must be saturated
                if (nextOffset != (rangeOffset + sizeof(void*)))
                     break;
                j++;
                rangeOffset = nextOffset;
            } while(j < gcOffsets.Count());

            if (j > (i + 2))
            {
                EncodeOneGCOffset(pSl, delta, FALSE, TRUE, last); 
                i = j - 1;
                _ASSERTE(rangeOffset >= (offset + (sizeof(void*) * 3)));
                delta = rangeOffset - (offset + (sizeof(void*) * 3));
                offset = rangeOffset;
            }
        }
        last = (++i == gcOffsets.Count());


        EncodeOneGCOffset(pSl, delta, maybeInterior, FALSE, last); 

        prevOffset = offset;
    } while (!last);
}

static void AppendGCLayout(ULONGARRAY &gcLayout, size_t baseOffset, BOOL fIsTypedRef, TypeHandle VMClsHnd)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((baseOffset % 16) == 0);
    _ASSERTE(FitsInU4(baseOffset));

    if (fIsTypedRef)
    {
        *gcLayout.AppendThrowing() = (ULONG)(baseOffset | 1); // "| 1" to mark it as an interior pointer
    }
    else if (!VMClsHnd.IsNativeValueType())
    {
        MethodTable* pMT = VMClsHnd.GetMethodTable();
        _ASSERTE(pMT);
        _ASSERTE(pMT->IsValueType());

        // walk the GC descriptors, reporting the correct offsets
        if (pMT->ContainsPointers())
        {
            // size of instance when unboxed must be adjusted for the syncblock
            // index and the VTable pointer.
            DWORD       size = pMT->GetBaseSize();

            // we don't include this term in our 'ppstop' calculation below.
            _ASSERTE(pMT->GetComponentSize() == 0);

            CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
            CGCDescSeries* cur = map->GetLowestSeries();
            CGCDescSeries* last = map->GetHighestSeries();

            _ASSERTE(cur <= last);
            do
            {
                // offset to embedded references in this series must be
                // adjusted by the VTable pointer, when in the unboxed state.
                size_t   adjustOffset = cur->GetSeriesOffset() - sizeof(void *);

                _ASSERTE(baseOffset >= adjustOffset);
                size_t start = baseOffset - adjustOffset;
                size_t stop = start - (cur->GetSeriesSize() + size);
                for (size_t off = stop + sizeof(void*); off <= start; off += sizeof(void*))
                {
                    _ASSERTE(gcLayout.Count() == 0 || off > gcLayout[gcLayout.Count() - 1]);
                    _ASSERTE(FitsInU4(off));
                    *gcLayout.AppendThrowing() = (ULONG)off;
                }
                cur++;

            } while (cur <= last);
        }
    }
}

Stub * StubLinkerCPU::CreateTailCallCopyArgsThunk(CORINFO_SIG_INFO * pSig,
                                                  CorInfoHelperTailCallSpecialHandling flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER   sl;
    CPUSTUBLINKER*  pSl = &sl;

    // Generates a function that looks like this:
    // size_t CopyArguments(va_list args,         (RCX)
    //                      CONTEXT *pCtx,        (RDX)
    //                      DWORD64 *pvStack,     (R8)
    //                      size_t cbStack)       (R9)
    // {
    //     if (pCtx != NULL) {
    //         foreach (arg in args) {
    //             copy into pCtx or pvStack
    //         }
    //     }
    //     return <size of stack needed>;
    // }
    //

    CodeLabel *pNullLabel = pSl->NewCodeLabel();

    // test rdx, rdx
    pSl->X86EmitR2ROp(0x85, kRDX, kRDX);

    // jz NullLabel
    pSl->X86EmitCondJump(pNullLabel, X86CondCode::kJZ);

    UINT nArgSlot = 0;
    UINT totalArgs = pSig->totalILArgs() + ((pSig->isVarArg() || pSig->hasTypeArg()) ? 1 : 0);
    bool fR10Loaded = false;
    UINT cbArg;
    static const UINT rgcbArgRegCtxtOffsets[4] = { offsetof(CONTEXT, Rcx), offsetof(CONTEXT, Rdx),
                                                   offsetof(CONTEXT, R8), offsetof(CONTEXT, R9) };
    static const UINT rgcbFpArgRegCtxtOffsets[4] = { offsetof(CONTEXT, Xmm0.Low), offsetof(CONTEXT, Xmm1.Low),
                                                     offsetof(CONTEXT, Xmm2.Low), offsetof(CONTEXT, Xmm3.Low) };

    ULONGARRAY gcLayout;

    // On input to the function R9 contains the size of the buffer
    // The first time this macro runs, R10 is loaded with the 'top' of the Frame
    // and R9 is changed to point to the 'top' of the copy buffer.
    // Then both R9 and R10 are decremented by the size of the struct we're copying
    // So R10 is the value to put in the argument slot, and R9 is where the data
    // should be copied to (or zeroed out in the case of the return buffer).
#define LOAD_STRUCT_OFFSET_IF_NEEDED(cbSize)                                      \
    {                                                                             \
        _ASSERTE(cbSize > 0);                                                     \
        _ASSERTE(FitsInI4(cbSize));                                               \
        __int32 offset = (__int32)cbSize;                                         \
        if (!fR10Loaded) {                                                        \
            /* mov r10, [rdx + offset of RSP] */                                  \
            pSl->X86EmitIndexRegLoad(kR10, kRDX, offsetof(CONTEXT, Rsp));         \
            /* add an extra 8 because RSP is pointing at the return address */    \
            offset -= 8;                                                          \
            /* add r10, r9 */                                                     \
            pSl->X86EmitAddRegReg(kR10, kR9);                                     \
            /* add r9, r8 */                                                      \
            pSl->X86EmitAddRegReg(kR9, kR8);                                      \
            fR10Loaded = true;                                                    \
        }                                                                         \
        /* sub r10, offset */                                                     \
        pSl->X86EmitSubReg(kR10, offset);                                         \
        /* sub r9, cbSize */                                                      \
        pSl->X86EmitSubReg(kR9, cbSize);                                          \
    }


    if (flags & CORINFO_TAILCALL_STUB_DISPATCH_ARG) {
        // This is set for stub dispatch
        // The JIT placed an extra argument in the list that needs to
        // get shoved into R11, and not counted.
        // pCtx->R11 = va_arg(args, DWORD64);

        // mov rax, [rcx]
        pSl->X86EmitIndexRegLoad(kRAX, kRCX, 0);
        // add rcx, 8
        pSl->X86EmitAddReg(kRCX, 8);
        // mov [rdx + offset of R11], rax
        pSl->X86EmitIndexRegStore(kRDX, offsetof(CONTEXT, R11), kRAX);
    }

    ULONG cbStructOffset = 0;

    // First comes the 'this' pointer
    if (pSig->hasThis()) {
        // mov rax, [rcx]
        pSl->X86EmitIndexRegLoad(kRAX, kRCX, 0);
        // add rcx, 8
        pSl->X86EmitAddReg(kRCX, 8);
        // mov [rdx + offset of RCX/RDX], rax
        pSl->X86EmitIndexRegStore(kRDX, rgcbArgRegCtxtOffsets[nArgSlot++], kRAX);
    }

    // Next the return buffer
    cbArg = 0;
    TypeHandle th(pSig->retTypeClass);
    if ((pSig->retType == CORINFO_TYPE_REFANY) || (pSig->retType == CORINFO_TYPE_VALUECLASS)) {
        cbArg = th.GetSize();
    }

    if (ArgIterator::IsArgPassedByRef(cbArg)) {
        totalArgs++;

        // We always reserve space for the return buffer, and we always zero it out,
        // so the GC won't complain, but if it's already pointing above the frame,
        // then we need to pass it in (so it will get passed out).
        // Otherwise we assume the caller is returning void, so we just pass in
        // dummy space to be overwritten.
        UINT cbUsed = (cbArg + 0xF) & ~0xF;
        LOAD_STRUCT_OFFSET_IF_NEEDED(cbUsed);
        // now emit a 'memset(r9, 0, cbUsed)'
        {
            // xorps xmm0, xmm0
            pSl->X86EmitR2ROp(X86_INSTR_XORPS, kXMM0, kXMM0);
            if (cbUsed <= 4 * 16) {
                // movaps [r9], xmm0
                pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 0);
                if (16 < cbUsed) {
                    // movaps [r9 + 16], xmm0
                    pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 16);
                    if (32 < cbUsed) {
                        // movaps [r9 + 32], xmm0
                        pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 32);
                        if (48 < cbUsed) {
                            // movaps [r9 + 48], xmm0
                            pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 48);
                        }
                    }
                }
            }
            else {
                // a loop (one double-quadword at a time)
                pSl->X86EmitZeroOutReg(kR11);
                // LoopLabel:
                CodeLabel *pLoopLabel = pSl->NewCodeLabel();
                pSl->EmitLabel(pLoopLabel);
                // movaps [r9 + r11], xmm0
                pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 0, kR11, 1);
                // add r11, 16
                pSl->X86EmitAddReg(kR11, 16);
                // cmp r11, cbUsed
                pSl->X86EmitCmpRegImm32(kR11, cbUsed);
                // jl LoopLabel
                pSl->X86EmitCondJump(pLoopLabel, X86CondCode::kJL);
            }
        }
        cbStructOffset += cbUsed;
        AppendGCLayout(gcLayout, cbStructOffset, pSig->retType == CORINFO_TYPE_REFANY, th);

        // mov rax, [rcx]
        pSl->X86EmitIndexRegLoad(kRAX, kRCX, 0);
        // add rcx, 8
        pSl->X86EmitAddReg(kRCX, 8);
        // cmp rax, [rdx + offset of R12]
        pSl->X86EmitOffsetModRM(0x3B, kRAX, kRDX, offsetof(CONTEXT, R12));

        CodeLabel *pSkipLabel = pSl->NewCodeLabel();
        // jnb SkipLabel
        pSl->X86EmitCondJump(pSkipLabel, X86CondCode::kJNB);

        // Also check the lower bound of the stack in case the return buffer is on the GC heap
        // and the GC heap is below the stack
        // cmp rax, rsp
        pSl->X86EmitR2ROp(0x3B, kRAX, (X86Reg)4 /*kRSP*/);
        // jna SkipLabel
        pSl->X86EmitCondJump(pSkipLabel, X86CondCode::kJB);
        // mov rax, r10
        pSl->X86EmitMovRegReg(kRAX, kR10);
        // SkipLabel:
        pSl->EmitLabel(pSkipLabel);
        // mov [rdx + offset of RCX], rax
        pSl->X86EmitIndexRegStore(kRDX, rgcbArgRegCtxtOffsets[nArgSlot++], kRAX);
    }

    // VarArgs Cookie *or* Generics Instantiation Parameter
    if (pSig->hasTypeArg() || pSig->isVarArg()) {
        // mov rax, [rcx]
        pSl->X86EmitIndexRegLoad(kRAX, kRCX, 0);
        // add rcx, 8
        pSl->X86EmitAddReg(kRCX, 8);
        // mov [rdx + offset of RCX/RDX], rax
        pSl->X86EmitIndexRegStore(kRDX, rgcbArgRegCtxtOffsets[nArgSlot++], kRAX);
    }

    _ASSERTE(nArgSlot <= 4);

    // Now for *all* the 'real' arguments
    SigPointer ptr((PCCOR_SIGNATURE)pSig->args);
    Module * module = GetModule(pSig->scope);
    Instantiation classInst((TypeHandle*)pSig->sigInst.classInst, pSig->sigInst.classInstCount);
    Instantiation methodInst((TypeHandle*)pSig->sigInst.methInst, pSig->sigInst.methInstCount);
    SigTypeContext typeCtxt(classInst, methodInst);

    for( ;nArgSlot < totalArgs; ptr.SkipExactlyOne()) {
        CorElementType et = ptr.PeekElemTypeNormalized(module, &typeCtxt);
        if (et == ELEMENT_TYPE_SENTINEL)
            continue;

        // mov rax, [rcx]
        pSl->X86EmitIndexRegLoad(kRAX, kRCX, 0);
        // add rcx, 8
        pSl->X86EmitAddReg(kRCX, 8);
        switch (et) {
        case ELEMENT_TYPE_INTERNAL:
            // TODO
            _ASSERTE(!"Shouldn't see ELEMENT_TYPE_INTERNAL");
            break;
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_VALUETYPE:
            th = ptr.GetTypeHandleThrowing(module, &typeCtxt, ClassLoader::LoadTypes, CLASS_LOAD_UNRESTOREDTYPEKEY);
            _ASSERTE(!th.IsNull());
            g_IBCLogger.LogEEClassAndMethodTableAccess(th.GetMethodTable());
            cbArg = (UINT)th.GetSize();
            if (ArgIterator::IsArgPassedByRef(cbArg)) {
                UINT cbUsed = (cbArg + 0xF) & ~0xF;
                LOAD_STRUCT_OFFSET_IF_NEEDED(cbUsed);
                // rax has the source pointer
                // r9 has the intermediate copy location
                // r10 has the final destination
                if (nArgSlot < 4) {
                    pSl->X86EmitIndexRegStore(kRDX, rgcbArgRegCtxtOffsets[nArgSlot++], kR10);
                }
                else {
                    pSl->X86EmitIndexRegStore(kR8, 8 * nArgSlot++, kR10);
                }
                // now emit a 'memcpy(rax, r9, cbUsed)'
                // These structs are supposed to be 16-byte aligned, but
                // Reflection puts them on the GC heap, which is only 8-byte
                // aligned.  It also means we have to be careful about not
                // copying too much (because we might cross a page boundary)
                UINT cbUsed16 = (cbArg + 7) & ~0xF;
                _ASSERTE((cbUsed16 == cbUsed) || ((cbUsed16 + 16) == cbUsed));

                if (cbArg <= 192) {
                    // Unrolled version (6 x 16 bytes in parallel)
                    UINT offset = 0;
                    while (offset < cbUsed16) {
                        // movups xmm0, [rax + offset]
                        pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM0, kRAX, offset);
                        if (offset + 16 < cbUsed16) {
                            // movups xmm1, [rax + offset + 16]
                            pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM1, kRAX, offset + 16);
                            if (offset + 32 < cbUsed16) {
                                // movups xmm2, [rax + offset + 32]
                                pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM2, kRAX, offset + 32);
                                if (offset + 48 < cbUsed16) {
                                    // movups xmm3, [rax + offset + 48]
                                    pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM3, kRAX, offset + 48);
                                    if (offset + 64 < cbUsed16) {
                                        // movups xmm4, [rax + offset + 64]
                                        pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM4, kRAX, offset + 64);
                                        if (offset + 80 < cbUsed16) {
                                            // movups xmm5, [rax + offset + 80]
                                            pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM5, kRAX, offset + 80);
                                        }
                                    }
                                }
                            }
                        }
                        // movaps [r9 + offset], xmm0
                        pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, offset);
                        offset += 16;
                        if (offset < cbUsed16) {
                            // movaps [r9 + 16], xmm1
                            pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM1, kR9, offset);
                            offset += 16;
                            if (offset < cbUsed16) {
                                // movaps [r9 + 32], xmm2
                                pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM2, kR9, offset);
                                offset += 16;
                                if (offset < cbUsed16) {
                                    // movaps [r9 + 48], xmm3
                                    pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM3, kR9, offset);
                                    offset += 16;
                                    if (offset < cbUsed16) {
                                        // movaps [r9 + 64], xmm4
                                        pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM4, kR9, offset);
                                        offset += 16;
                                        if (offset < cbUsed16) {
                                            // movaps [r9 + 80], xmm5
                                            pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM5, kR9, offset);
                                            offset += 16;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // Copy the last 8 bytes if needed
                    if (cbUsed > cbUsed16) {
                        _ASSERTE(cbUsed16 < cbArg);
                        // movlps xmm0, [rax + offset]
                        pSl->X86EmitOp(X86_INSTR_MOVLPS_R_RM, kXMM0, kRAX, offset);
                        // movlps [r9 + offset], xmm0
                        pSl->X86EmitOp(X86_INSTR_MOVLPS_RM_R, kXMM0, kR9, offset);
                    }
                }
                else {
                    // a loop (one double-quadword at a time)
                    pSl->X86EmitZeroOutReg(kR11);
                    // LoopLabel:
                    CodeLabel *pLoopLabel = pSl->NewCodeLabel();
                    pSl->EmitLabel(pLoopLabel);
                    // movups xmm0, [rax + r11]
                    pSl->X86EmitOp(X86_INSTR_MOVUPS_R_RM, kXMM0, kRAX, 0, kR11, 1);
                    // movaps [r9 + r11], xmm0
                    pSl->X86EmitOp(X86_INSTR_MOVAPS_RM_R, kXMM0, kR9, 0, kR11, 1);
                    // add r11, 16
                    pSl->X86EmitAddReg(kR11, 16);
                    // cmp r11, cbUsed16
                    pSl->X86EmitCmpRegImm32(kR11, cbUsed16);
                    // jl LoopLabel
                    pSl->X86EmitCondJump(pLoopLabel, X86CondCode::kJL);
                    if (cbArg > cbUsed16) {
                        _ASSERTE(cbUsed16 + 8 >= cbArg);
                        // movlps xmm0, [rax + r11]
                        pSl->X86EmitOp(X86_INSTR_MOVLPS_R_RM, kXMM0, kRAX, 0, kR11, 1);
                        // movlps [r9 + r11], xmm0
                        pSl->X86EmitOp(X86_INSTR_MOVLPS_RM_R, kXMM0, kR9, 0, kR11, 1);
                    }
                }
                cbStructOffset += cbUsed;
                AppendGCLayout(gcLayout, cbStructOffset, et == ELEMENT_TYPE_TYPEDBYREF, th);
                break;
            }

            //
            // Explicit Fall-Through for non-IsArgPassedByRef
            //

        default:
            if (nArgSlot < 4) {
                pSl->X86EmitIndexRegStore(kRDX, rgcbArgRegCtxtOffsets[nArgSlot], kRAX);
                if ((et == ELEMENT_TYPE_R4) || (et == ELEMENT_TYPE_R8)) {
                    pSl->X86EmitIndexRegStore(kRDX, rgcbFpArgRegCtxtOffsets[nArgSlot], kRAX);
                }
            }
            else {
                pSl->X86EmitIndexRegStore(kR8, 8 * nArgSlot, kRAX);
            }
            nArgSlot++;
            break;
        }
    }

#undef LOAD_STRUCT_OFFSET_IF_NEEDED

    // Keep our 4 shadow slots and even number of slots (to keep 16-byte aligned)
    if (nArgSlot < 4) 
        nArgSlot = 4;
    else if (nArgSlot & 1)
        nArgSlot++;

    _ASSERTE((cbStructOffset % 16) == 0);

    // xor eax, eax
    pSl->X86EmitZeroOutReg(kRAX);
    // ret
    pSl->X86EmitReturn(0);

    // NullLabel:
    pSl->EmitLabel(pNullLabel);

    CodeLabel *pGCLayoutLabel = NULL;
    if (gcLayout.Count() == 0) {
        // xor eax, eax
        pSl->X86EmitZeroOutReg(kRAX);
    }
    else {
        // lea rax, [rip + offset to gclayout]
        pGCLayoutLabel = pSl->NewCodeLabel();
        pSl->X86EmitLeaRIP(pGCLayoutLabel, kRAX);
    }
    // mov [r9], rax
    pSl->X86EmitIndexRegStore(kR9, 0, kRAX);
    // mov rax, cbStackNeeded
    pSl->X86EmitRegLoad(kRAX, cbStructOffset + nArgSlot * 8);
    // ret
    pSl->X86EmitReturn(0);

    if (gcLayout.Count() > 0) {
        // GCLayout:
        pSl->EmitLabel(pGCLayoutLabel);
        EncodeGCOffsets(pSl, gcLayout);
    }

    return pSl->Link();
}
#endif // DACCESS_COMPILE

#endif // _TARGET_AMD64_


#ifdef HAS_FIXUP_PRECODE

#ifdef HAS_FIXUP_PRECODE_CHUNKS
TADDR FixupPrecode::GetMethodDesc()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // This lookup is also manually inlined in PrecodeFixupThunk assembly code
    TADDR base = *PTR_TADDR(GetBase());
    if (base == NULL)
        return NULL;
    return base + (m_MethodDescChunkIndex * MethodDesc::ALIGNMENT);
}
#endif

#ifdef DACCESS_COMPILE
void FixupPrecode::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(this), sizeof(FixupPrecode));

    DacEnumMemoryRegion(GetBase(), sizeof(TADDR));
}
#endif // DACCESS_COMPILE

#endif // HAS_FIXUP_PRECODE

#ifndef DACCESS_COMPILE

BOOL rel32SetInterlocked(/*PINT32*/ PVOID pRel32, TADDR target, TADDR expected, MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;         // Creating a JumpStub could throw OutOfMemory
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BYTE* callAddrAdj = (BYTE*)pRel32 + 4;
    INT32 expectedRel32 = static_cast<INT32>((BYTE*)expected - callAddrAdj);

    INT32 targetRel32 = rel32UsingJumpStub((INT32*)pRel32, target, pMD);

    _ASSERTE(IS_ALIGNED(pRel32, sizeof(INT32)));
    return FastInterlockCompareExchange((LONG*)pRel32, (LONG)targetRel32, (LONG)expectedRel32) == (LONG)expectedRel32;
}

void StubPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator /* = NULL */,
    BYTE type /* = StubPrecode::Type */, TADDR target /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    IN_WIN64(m_movR10 = X86_INSTR_MOV_R10_IMM64);   // mov r10, pMethodDesc
    IN_WIN32(m_movEAX = X86_INSTR_MOV_EAX_IMM32);   // mov eax, pMethodDesc
    m_pMethodDesc = (TADDR)pMD;
    IN_WIN32(m_mov_rm_r = X86_INSTR_MOV_RM_R);      // mov reg,reg
    m_type = type;
    m_jmp = X86_INSTR_JMP_REL32;        // jmp rel32

    if (pLoaderAllocator != NULL)
    {
        // Use pMD == NULL in all precode initialization methods to allocate the initial jump stub in non-dynamic heap
        // that has the same lifetime like as the precode itself
        if (target == NULL)
            target = GetPreStubEntryPoint();
        m_rel32 = rel32UsingJumpStub(&m_rel32, target, NULL /* pMD */, pLoaderAllocator);
    }
}

#ifdef HAS_NDIRECT_IMPORT_PRECODE

void NDirectImportPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;
    StubPrecode::Init(pMD, pLoaderAllocator, NDirectImportPrecode::Type, GetEEFuncEntryPoint(NDirectImportThunk));
}

#endif // HAS_NDIRECT_IMPORT_PRECODE


#ifdef HAS_REMOTING_PRECODE

void RemotingPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    IN_WIN64(m_movR10 = X86_INSTR_MOV_R10_IMM64);   // mov r10, pMethodDesc
    IN_WIN32(m_movEAX = X86_INSTR_MOV_EAX_IMM32);   // mov eax, pMethodDesc
    m_pMethodDesc = (TADDR)pMD;
    m_type = PRECODE_REMOTING;          // nop
    m_call = X86_INSTR_CALL_REL32;
    m_jmp = X86_INSTR_JMP_REL32;        // jmp rel32

    if (pLoaderAllocator != NULL)
    {
        m_callRel32 = rel32UsingJumpStub(&m_callRel32,
            GetEEFuncEntryPoint(PrecodeRemotingThunk), NULL /* pMD */, pLoaderAllocator);
        m_rel32 = rel32UsingJumpStub(&m_rel32,
            GetPreStubEntryPoint(), NULL /* pMD */, pLoaderAllocator);
    }
}

#endif // HAS_REMOTING_PRECODE


#ifdef HAS_FIXUP_PRECODE
void FixupPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator, int iMethodDescChunkIndex /*=0*/, int iPrecodeChunkIndex /*=0*/)
{
    WRAPPER_NO_CONTRACT;

    m_op   = X86_INSTR_CALL_REL32;       // call PrecodeFixupThunk
    m_type = FixupPrecode::TypePrestub;

    // Initialize chunk indices only if they are not initialized yet. This is necessary to make MethodDesc::Reset work.
    if (m_PrecodeChunkIndex == 0)
    {
        _ASSERTE(FitsInU1(iPrecodeChunkIndex));
        m_PrecodeChunkIndex = static_cast<BYTE>(iPrecodeChunkIndex);
    }

    if (iMethodDescChunkIndex != -1)
    {
        if (m_MethodDescChunkIndex == 0)
        {
            _ASSERTE(FitsInU1(iMethodDescChunkIndex));
            m_MethodDescChunkIndex = static_cast<BYTE>(iMethodDescChunkIndex);
        }

        if (*(void**)GetBase() == NULL)
            *(void**)GetBase() = (BYTE*)pMD - (iMethodDescChunkIndex * MethodDesc::ALIGNMENT);
    }

    _ASSERTE(GetMethodDesc() == (TADDR)pMD);

    if (pLoaderAllocator != NULL)
    {
        m_rel32 = rel32UsingJumpStub(&m_rel32,
            GetEEFuncEntryPoint(PrecodeFixupThunk), NULL /* pMD */, pLoaderAllocator);
    }
}

BOOL FixupPrecode::SetTargetInterlocked(TADDR target, TADDR expected)
{
    CONTRACTL
    {
        THROWS;         // Creating a JumpStub could throw OutOfMemory
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    INT64 oldValue = *(INT64*)this;
    BYTE* pOldValue = (BYTE*)&oldValue;

    if (pOldValue[OFFSETOF_PRECODE_TYPE_CALL_OR_JMP] != FixupPrecode::TypePrestub)
        return FALSE;

    MethodDesc * pMD = (MethodDesc*)GetMethodDesc();
    g_IBCLogger.LogMethodPrecodeWriteAccess(pMD);
    
    INT64 newValue = oldValue;
    BYTE* pNewValue = (BYTE*)&newValue;

    pNewValue[OFFSETOF_PRECODE_TYPE_CALL_OR_JMP] = FixupPrecode::Type;

    pOldValue[offsetof(FixupPrecode,m_op)] = X86_INSTR_CALL_REL32;
    pNewValue[offsetof(FixupPrecode,m_op)] = X86_INSTR_JMP_REL32;

    *(INT32*)(&pNewValue[offsetof(FixupPrecode,m_rel32)]) = rel32UsingJumpStub(&m_rel32, target, pMD);

    _ASSERTE(IS_ALIGNED(this, sizeof(INT64)));
    EnsureWritableExecutablePages(this, sizeof(INT64));
    return FastInterlockCompareExchangeLong((INT64*) this, newValue, oldValue) == oldValue;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Partial initialization. Used to save regrouped chunks.
void FixupPrecode::InitForSave(int iPrecodeChunkIndex)
{
    m_op   = X86_INSTR_CALL_REL32;       // call PrecodeFixupThunk
    m_type = FixupPrecode::TypePrestub;

    _ASSERTE(FitsInU1(iPrecodeChunkIndex));
    m_PrecodeChunkIndex = static_cast<BYTE>(iPrecodeChunkIndex);

    // The rest is initialized in code:FixupPrecode::Fixup
}

void FixupPrecode::Fixup(DataImage *image, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    // Note that GetMethodDesc() does not return the correct value because of 
    // regrouping of MethodDescs into hot and cold blocks. That's why the caller
    // has to supply the actual MethodDesc

    SSIZE_T mdChunkOffset;
    ZapNode * pMDChunkNode = image->GetNodeForStructure(pMD, &mdChunkOffset);
    ZapNode * pHelperThunk = image->GetHelperThunk(CORINFO_HELP_EE_PRECODE_FIXUP);

    image->FixupFieldToNode(this, offsetof(FixupPrecode, m_rel32),
                            pHelperThunk, 0, IMAGE_REL_BASED_REL32);

    // Set the actual chunk index
    FixupPrecode * pNewPrecode = (FixupPrecode *)image->GetImagePointer(this);

    size_t mdOffset   = mdChunkOffset - sizeof(MethodDescChunk);
    size_t chunkIndex = mdOffset / MethodDesc::ALIGNMENT;
    _ASSERTE(FitsInU1(chunkIndex));
    pNewPrecode->m_MethodDescChunkIndex = (BYTE) chunkIndex;

    // Fixup the base of MethodDescChunk
    if (m_PrecodeChunkIndex == 0)
    {
        image->FixupFieldToNode(this, (BYTE *)GetBase() - (BYTE *)this,
            pMDChunkNode, sizeof(MethodDescChunk));
    }
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // HAS_FIXUP_PRECODE

#endif // !DACCESS_COMPILE


#ifdef HAS_THISPTR_RETBUF_PRECODE

// rel32 jmp target that points back to the jump (infinite loop).
// Used to mark uninitialized ThisPtrRetBufPrecode target
#define REL32_JMP_SELF (-5)

#ifndef DACCESS_COMPILE
void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    IN_WIN64(m_nop1 = X86_INSTR_NOP;)   // nop
#ifdef UNIX_AMD64_ABI
    m_prefix1 = 0x48;
    m_movScratchArg0 = 0xC78B;          // mov rax,rdi
    m_prefix2 = 0x48;
    m_movArg0Arg1 = 0xFE8B;             // mov rdi,rsi
    m_prefix3 = 0x48;
    m_movArg1Scratch = 0xF08B;          // mov rsi,rax
#else
    IN_WIN64(m_prefix1 = 0x48;)
    m_movScratchArg0 = 0xC889;          // mov r/eax,r/ecx
    IN_WIN64(m_prefix2 = 0x48;)
    m_movArg0Arg1 = 0xD189;             // mov r/ecx,r/edx
    IN_WIN64(m_prefix3 = 0x48;)
    m_movArg1Scratch = 0xC289;          // mov r/edx,r/eax
#endif
    m_nop2 = X86_INSTR_NOP;             // nop
    m_jmp = X86_INSTR_JMP_REL32;        // jmp rel32
    m_pMethodDesc = (TADDR)pMD;

    // This precode is never patched lazily - avoid unnecessary jump stub allocation
    m_rel32 = REL32_JMP_SELF;
}

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
    m_rel32 = rel32UsingJumpStub(&m_rel32, target, NULL /* pMD */, ((MethodDesc *)GetMethodDesc())->GetLoaderAllocatorForCode());

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
