// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubs.cpp
//
// This file contains stub functions for unimplemented features need to
// run on the ARM platform.

#include "common.h"
#include "jitinterface.h"
#include "comdelegate.h"
#include "invokeutil.h"
#include "excep.h"
#include "class.h"
#include "field.h"
#include "dllimportcallback.h"
#include "dllimport.h"
#include "eeconfig.h"
#include "cgensys.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "gcdump.h"
#include "rtlfunctions.h"
#include "codeman.h"
#include "ecall.h"
#include "threadsuspend.h"

// target write barriers
EXTERN_C void JIT_WriteBarrier(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_End();
EXTERN_C void JIT_CheckedWriteBarrier(Object **dst, Object *ref);
EXTERN_C void JIT_CheckedWriteBarrier_End();
EXTERN_C void JIT_ByRefWriteBarrier_End();
EXTERN_C void JIT_ByRefWriteBarrier_SP(Object **dst, Object *ref);

// source write barriers
EXTERN_C void JIT_WriteBarrier_SP_Pre(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SP_Pre_End();
EXTERN_C void JIT_WriteBarrier_SP_Post(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SP_Post_End();
EXTERN_C void JIT_WriteBarrier_MP_Pre(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_MP_Pre_End();
EXTERN_C void JIT_WriteBarrier_MP_Post(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_MP_Post_End();

EXTERN_C void JIT_CheckedWriteBarrier_SP_Pre(Object **dst, Object *ref);
EXTERN_C void JIT_CheckedWriteBarrier_SP_Pre_End();
EXTERN_C void JIT_CheckedWriteBarrier_SP_Post(Object **dst, Object *ref);
EXTERN_C void JIT_CheckedWriteBarrier_SP_Post_End();
EXTERN_C void JIT_CheckedWriteBarrier_MP_Pre(Object **dst, Object *ref);
EXTERN_C void JIT_CheckedWriteBarrier_MP_Pre_End();
EXTERN_C void JIT_CheckedWriteBarrier_MP_Post(Object **dst, Object *ref);
EXTERN_C void JIT_CheckedWriteBarrier_MP_Post_End();

EXTERN_C void JIT_ByRefWriteBarrier_SP_Pre();
EXTERN_C void JIT_ByRefWriteBarrier_SP_Pre_End();
EXTERN_C void JIT_ByRefWriteBarrier_SP_Post();
EXTERN_C void JIT_ByRefWriteBarrier_SP_Post_End();
EXTERN_C void JIT_ByRefWriteBarrier_MP_Pre();
EXTERN_C void JIT_ByRefWriteBarrier_MP_Pre_End();
EXTERN_C void JIT_ByRefWriteBarrier_MP_Post(Object **dst, Object *ref);
EXTERN_C void JIT_ByRefWriteBarrier_MP_Post_End();

EXTERN_C void JIT_PatchedWriteBarrierStart();
EXTERN_C void JIT_PatchedWriteBarrierLast();

#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------
// InstructionFormat for conditional jump.
//-----------------------------------------------------------------------
class ThumbCondJump : public InstructionFormat
{
    public:
        ThumbCondJump() : InstructionFormat(InstructionFormat::k16)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(refsize == InstructionFormat::k16);

            return 2;
        }

        virtual UINT GetHotSpotOffset(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(refsize == InstructionFormat::k16);

            return 4;
        }

        //CB{N}Z Rn, <Label>
        //Encoding 1|0|1|1|op|0|i|1|imm5|Rn
        //op = Bit3(variation)
        //Rn = Bits2-0(variation)
        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(refsize == InstructionFormat::k16);

            if(fixedUpReference <0 || fixedUpReference > 126)
                COMPlusThrow(kNotSupportedException);

            _ASSERTE((fixedUpReference & 0x1) == 0);

            pOutBufferRW[0] = static_cast<BYTE>(((0x3e & fixedUpReference) << 2) | (0x7 & variationCode));
            pOutBufferRW[1] = static_cast<BYTE>(0xb1 | (0x8 & variationCode)| ((0x40 & fixedUpReference)>>5));
        }
};

//-----------------------------------------------------------------------
// InstructionFormat for near Jump and short Jump
//-----------------------------------------------------------------------
class ThumbNearJump : public InstructionFormat
{
    public:
        ThumbNearJump() : InstructionFormat(InstructionFormat::k16|InstructionFormat::k32)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT

            if(refsize == InstructionFormat::k16)
                return 2;
            else if(refsize == InstructionFormat::k32)
                return 4;
            else
                _ASSERTE(!"Unknown refsize");
            return 0;
        }

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT cond, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(cond <15);

            //offsets must be in multiples of 2
            _ASSERTE((fixedUpReference & 0x1) == 0);

            if(cond == 0xe) //Always execute
            {
                if(fixedUpReference >= -2048 && fixedUpReference <= 2046)
                {
                    if(refsize != InstructionFormat::k16)
                        _ASSERTE(!"Expected refSize to be 2");

                    //Emit T2 encoding of B<c> <label> instruction
                    pOutBufferRW[0] = static_cast<BYTE>((fixedUpReference & 0x1fe)>>1);
                    pOutBufferRW[1] = static_cast<BYTE>(0xe0 | ((fixedUpReference & 0xe00)>>9));
                }
                else if(fixedUpReference >= -16777216 && fixedUpReference <= 16777214)
                {
                    if(refsize != InstructionFormat::k32)
                        _ASSERTE(!"Expected refSize to be 4");

                    //Emit T4 encoding of B<c> <label> instruction
                    int s = (fixedUpReference & 0x1000000) >> 24;
                    int i1 = (fixedUpReference & 0x800000) >> 23;
                    int i2 = (fixedUpReference & 0x400000) >> 22;
                    pOutBufferRW[0] = static_cast<BYTE>((fixedUpReference & 0xff000) >> 12);
                    pOutBufferRW[1] = static_cast<BYTE>(0xf0 | (s << 2) |( (fixedUpReference & 0x300000) >>20));
                    pOutBufferRW[2] = static_cast<BYTE>((fixedUpReference & 0x1fe) >> 1);
                    pOutBufferRW[3] = static_cast<BYTE>(0x90 | (~(i1^s)) << 5 | (~(i2^s)) << 3 | (fixedUpReference & 0xe00) >> 9);
                }
                else
                {
                    COMPlusThrow(kNotSupportedException);
                }
            }
            else // conditional branch based on flags
            {
                if(fixedUpReference >= -256 && fixedUpReference <= 254)
                {
                    if(refsize != InstructionFormat::k16)
                        _ASSERTE(!"Expected refSize to be 2");

                    //Emit T1 encoding of B<c> <label> instruction
                    pOutBufferRW[0] = static_cast<BYTE>((fixedUpReference & 0x1fe)>>1);
                    pOutBufferRW[1] = static_cast<BYTE>(0xd0 | (cond & 0xf));
                }
                else if(fixedUpReference >= -1048576 && fixedUpReference <= 1048574)
                {
                    if(refsize != InstructionFormat::k32)
                        _ASSERTE(!"Expected refSize to be 4");

                    //Emit T3 encoding of B<c> <label> instruction
                    pOutBufferRW[0] = static_cast<BYTE>(((cond & 0x3) << 6) | ((fixedUpReference & 0x3f000) >>12));
                    pOutBufferRW[1] = static_cast<BYTE>(0xf0 | ((fixedUpReference & 0x100000) >>18) | ((cond & 0xc) >> 2));
                    pOutBufferRW[2] = static_cast<BYTE>((fixedUpReference & 0x1fe) >> 1);
                    pOutBufferRW[3] = static_cast<BYTE>(0x80 | ((fixedUpReference & 0x40000) >> 13) | ((fixedUpReference & 0x80000) >> 16) | ((fixedUpReference & 0xe00) >> 9));
                }
                else
                {
                    COMPlusThrow(kNotSupportedException);
                }
            }
        }

        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            LIMITED_METHOD_CONTRACT

            if (fExternal)
            {
                _ASSERTE(0);
                return FALSE;
            }
            else
            {
                switch (refsize)
                {
                case InstructionFormat::k16:
                    if(variationCode == 0xe)
                        return  (offset >= -2048 && offset <= 2046 && (offset & 0x1) == 0);
                    else
                        return (offset >= -256 && offset <= 254 && (offset & 0x1) == 0);
                case InstructionFormat::k32:
                    if(variationCode == 0xe)
                        return  ((offset >= -16777216) && (offset <= 16777214) && ((offset & 0x1) == 0));
                    else
                        return  ((offset >= -1048576) && (offset <= 1048574) && ((offset & 0x1) == 0));
                default:
                    _ASSERTE(!"Unknown refsize");
                    return FALSE;
                }
             }
        }

        virtual UINT GetHotSpotOffset(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(refsize == InstructionFormat::k16 || refsize == InstructionFormat::k32);

            return 4;
        }
};


//static conditional jump instruction format object
static BYTE gThumbCondJump[sizeof(ThumbCondJump)];

//static near jump instruction format object
static BYTE gThumbNearJump[sizeof(ThumbNearJump)];

void StubLinkerCPU::Init(void)
{
    //Initialize the object
    new (gThumbCondJump) ThumbCondJump();
    new (gThumbNearJump) ThumbNearJump();
}


// GC write barrier support.
//
// To optimize our write barriers we code the values of several GC globals (e.g. g_lowest_address) directly
// into the barrier function itself, thus avoiding a double memory indirection. Every time the GC modifies one
// of these globals we need to update all of the write barriers accordingly.
//
// In order to keep this process non-brittle we don't hard code the offsets of the instructions that need to
// be changed. Instead the code used to create these barriers is implemented using special macros that record
// the necessary offsets in a descriptor table. Search for "GC write barrier support" in vm\arm\asmhelpers.asm
// for more details.

// Structure describing the layout of a single write barrier descriptor. This must be kept in sync with the
// code in vm\arm\asmhelpers.asm in the WRITE_BARRIER_END macro. Each offset recorded is for one of the
// supported GC globals (an offset of 0xffff is encoded if that global is not used by the particular barrier
// function). We currently only support one usage of each global by any single barrier function. The offset is
// the byte offset from the start of the function at which a movw,movt instruction pair is used to load the
// value of the global into a register.
struct WriteBarrierDescriptor
{
#ifdef TARGET_UNIX
    DWORD   m_funcStartOffset;              // Offset to the start of the barrier function relative to this struct address
    DWORD   m_funcEndOffset;                // Offset to the end of the barrier function relative to this struct address
#else // TARGET_UNIX
    BYTE *  m_pFuncStart;                   // Pointer to the start of the barrier function
    BYTE *  m_pFuncEnd;                     // Pointer to the end of the barrier function
#endif // TARGET_UNIX
    DWORD   m_dw_g_lowest_address_offset;   // Offset of the instruction reading g_lowest_address
    DWORD   m_dw_g_highest_address_offset;  // Offset of the instruction reading g_highest_address
    DWORD   m_dw_g_ephemeral_low_offset;    // Offset of the instruction reading g_ephemeral_low
    DWORD   m_dw_g_ephemeral_high_offset;   // Offset of the instruction reading g_ephemeral_high
    DWORD   m_dw_g_card_table_offset;       // Offset of the instruction reading g_card_table
};

// Infrastructure used for mapping of the source and destination of current WB patching
struct WriteBarrierMapping
{
    PBYTE to;    // Pointer to the write-barrier where it was copied over
    PBYTE from;  // Pointer to write-barrier from which it was copied
};

const int WriteBarrierIndex         = 0;
const int CheckedWriteBarrierIndex  = 1;
const int ByRefWriteBarrierIndex    = 2;
const int MaxWriteBarrierIndex      = 3;

WriteBarrierMapping wbMapping[MaxWriteBarrierIndex] =
                                    {
                                        {(PBYTE)JIT_WriteBarrier, NULL},
                                        {(PBYTE)JIT_CheckedWriteBarrier, NULL},
                                        {(PBYTE)JIT_ByRefWriteBarrier, NULL}
                                    };

PBYTE FindWBMapping(PBYTE from)
{
    for(int i = 0; i < MaxWriteBarrierIndex; ++i)
    {
        if(wbMapping[i].from == from)
            return wbMapping[i].to;
    }
    return NULL;
}

// Pointer to the start of the descriptor table. The end of the table is marked by a sentinel entry
// (m_pFuncStart is NULL).
EXTERN_C WriteBarrierDescriptor g_rgWriteBarrierDescriptors;

// Determine the range of memory containing all the write barrier implementations (these are clustered
// together and should fit in a page or maybe two).
void ComputeWriteBarrierRange(BYTE ** ppbStart, DWORD * pcbLength)
{
    DWORD size = (PBYTE)JIT_PatchedWriteBarrierLast - (PBYTE)JIT_PatchedWriteBarrierStart;
    *ppbStart = (PBYTE)JIT_PatchedWriteBarrierStart;
    if (IsWriteBarrierCopyEnabled())
    {
        *ppbStart = GetWriteBarrierCodeLocation(*ppbStart);
    }
    *pcbLength = size;
}

void CopyWriteBarrier(PCODE dstCode, PCODE srcCode, PCODE endCode)
{
    TADDR dst = (TADDR)PCODEToPINSTR((PCODE)GetWriteBarrierCodeLocation((void*)dstCode));
    TADDR src = PCODEToPINSTR(srcCode);
    TADDR end = PCODEToPINSTR(endCode);

    size_t size = (PBYTE)end - (PBYTE)src;

    ExecutableWriterHolder<void> writeBarrierWriterHolder;
    if (IsWriteBarrierCopyEnabled())
    {
        writeBarrierWriterHolder = ExecutableWriterHolder<void>((void*)dst, size);
        dst = (TADDR)writeBarrierWriterHolder.GetRW();
    }

    memcpy((PVOID)dst, (PVOID)src, size);
}

#if _DEBUG
void ValidateWriteBarriers()
{
    // Post-grow WB are bigger than pre-grow so validating that target WB has space to accomodate those
    _ASSERTE( ((PBYTE)JIT_WriteBarrier_End - (PBYTE)JIT_WriteBarrier) >= ((PBYTE)JIT_WriteBarrier_MP_Post_End - (PBYTE)JIT_WriteBarrier_MP_Post));
    _ASSERTE( ((PBYTE)JIT_WriteBarrier_End - (PBYTE)JIT_WriteBarrier) >= ((PBYTE)JIT_WriteBarrier_SP_Post_End - (PBYTE)JIT_WriteBarrier_SP_Post));

    _ASSERTE( ((PBYTE)JIT_CheckedWriteBarrier_End - (PBYTE)JIT_CheckedWriteBarrier) >= ((PBYTE)JIT_CheckedWriteBarrier_MP_Post_End - (PBYTE)JIT_CheckedWriteBarrier_MP_Post));
    _ASSERTE( ((PBYTE)JIT_CheckedWriteBarrier_End - (PBYTE)JIT_CheckedWriteBarrier) >= ((PBYTE)JIT_CheckedWriteBarrier_SP_Post_End - (PBYTE)JIT_CheckedWriteBarrier_SP_Post));

    _ASSERTE( ((PBYTE)JIT_ByRefWriteBarrier_End - (PBYTE)JIT_ByRefWriteBarrier) >= ((PBYTE)JIT_ByRefWriteBarrier_MP_Post_End - (PBYTE)JIT_ByRefWriteBarrier_MP_Post));
    _ASSERTE( ((PBYTE)JIT_ByRefWriteBarrier_End - (PBYTE)JIT_ByRefWriteBarrier) >= ((PBYTE)JIT_ByRefWriteBarrier_SP_Post_End - (PBYTE)JIT_ByRefWriteBarrier_SP_Post));

}
#endif // _DEBUG

#define UPDATE_WB(_proc,_grow)   \
    CopyWriteBarrier((PCODE)JIT_WriteBarrier, (PCODE)JIT_WriteBarrier_ ## _proc ## _ ## _grow , (PCODE)JIT_WriteBarrier_ ## _proc ## _ ## _grow ## _End); \
    wbMapping[WriteBarrierIndex].from = (PBYTE)JIT_WriteBarrier_ ## _proc ## _ ## _grow ; \
    \
    CopyWriteBarrier((PCODE)JIT_CheckedWriteBarrier, (PCODE)JIT_CheckedWriteBarrier_ ## _proc ## _ ## _grow , (PCODE)JIT_CheckedWriteBarrier_ ## _proc ## _ ## _grow ## _End); \
    wbMapping[CheckedWriteBarrierIndex].from = (PBYTE)JIT_CheckedWriteBarrier_ ## _proc ## _ ## _grow ; \
    \
    CopyWriteBarrier((PCODE)JIT_ByRefWriteBarrier, (PCODE)JIT_ByRefWriteBarrier_ ## _proc ## _ ## _grow , (PCODE)JIT_ByRefWriteBarrier_ ## _proc ## _ ## _grow ## _End); \
    wbMapping[ByRefWriteBarrierIndex].from = (PBYTE)JIT_ByRefWriteBarrier_ ## _proc ## _ ## _grow ; \

// Update the instructions in our various write barrier implementations that refer directly to the values
// of GC globals such as g_lowest_address and g_card_table. We don't particularly care which values have
// changed on each of these callbacks, it's pretty cheap to refresh them all.
void UpdateGCWriteBarriers(bool postGrow = false)
{
    // Define a helper macro that abstracts the minutia of patching the instructions to access the value of a
    // particular GC global.

#if _DEBUG
    ValidateWriteBarriers();
#endif // _DEBUG

    static bool wbCopyRequired = true; // We begin with a wb copy
    static bool wbIsPostGrow = false;  // We begin with pre-Grow write barrier

    if(postGrow && !wbIsPostGrow)
    {
        wbIsPostGrow = true;
        wbCopyRequired = true;
    }

    if(wbCopyRequired)
    {
        BOOL mp = g_SystemInfo.dwNumberOfProcessors > 1;
        if(mp)
        {
            if(wbIsPostGrow)
            {
                UPDATE_WB(MP,Post);
            }
            else
            {
                UPDATE_WB(MP,Pre);
            }
        }
        else
        {
            if(wbIsPostGrow)
            {
                UPDATE_WB(SP,Post);
            }
            else
            {
                UPDATE_WB(SP,Pre);
            }
        }

        wbCopyRequired = false;
    }
#define GWB_PATCH_OFFSET(_global)                                       \
    if (pDesc->m_dw_##_global##_offset != 0xffff)                       \
        PutThumb2Mov32((UINT16*)(to + pDesc->m_dw_##_global##_offset), (UINT32)(dac_cast<TADDR>(_global)));

    // Iterate through the write barrier patch table created in the .clrwb section
    // (see write barrier asm code)
    WriteBarrierDescriptor * pDesc = &g_rgWriteBarrierDescriptors;
#ifdef TARGET_UNIX
    while (pDesc->m_funcStartOffset)
#else // TARGET_UNIX
    while (pDesc->m_pFuncStart)
#endif // TARGET_UNIX
    {
        // If the write barrier is being currently used (as in copied over to the patchable site)
        // then read the patch location from the table and use the offset to patch the target asm code
#ifdef TARGET_UNIX
        PBYTE to = FindWBMapping((BYTE *)pDesc + pDesc->m_funcStartOffset);
        size_t barrierSize = pDesc->m_funcEndOffset - pDesc->m_funcStartOffset;
#else // TARGET_UNIX
        PBYTE to = FindWBMapping(pDesc->m_pFuncStart);
        size_t barrierSize = pDesc->m_pFuncEnd - pDesc->m_pFuncStart;
#endif // TARGET_UNIX
        if(to)
        {
            to = (PBYTE)PCODEToPINSTR((PCODE)GetWriteBarrierCodeLocation(to));
            ExecutableWriterHolder<BYTE> barrierWriterHolder;
            if (IsWriteBarrierCopyEnabled())
            {
                barrierWriterHolder = ExecutableWriterHolder<BYTE>(to, barrierSize);
                to = barrierWriterHolder.GetRW();
            }
            GWB_PATCH_OFFSET(g_lowest_address);
            GWB_PATCH_OFFSET(g_highest_address);
            GWB_PATCH_OFFSET(g_ephemeral_low);
            GWB_PATCH_OFFSET(g_ephemeral_high);
            GWB_PATCH_OFFSET(g_card_table);
        }

        pDesc++;
    }
}

int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    // The runtime is not always suspended when this is called (unlike StompWriteBarrierEphemeral) but we have
    // no way to update the barrier code atomically on ARM since each 32-bit value we change is loaded over
    // two instructions. So we have to suspend the EE (which forces code out of the barrier functions) before
    // proceeding. Luckily the case where the runtime is not already suspended is relatively rare (allocation
    // of a new large object heap segment). Skip the suspend for the case where we're called during runtime
    // startup.

    // suspend/resuming the EE under GC stress will trigger a GC and if we're holding the
    // GC lock due to allocating a LOH segment it will cause a deadlock so disable it here.
    GCStressPolicy::InhibitHolder iholder;
    int stompWBCompleteActions = SWB_ICACHE_FLUSH;

    if (!isRuntimeSuspended)
    {
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
        stompWBCompleteActions |= SWB_EE_RESTART;
    }

    UpdateGCWriteBarriers(bReqUpperBoundsCheck);

    return stompWBCompleteActions;
}

int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    UNREFERENCED_PARAMETER(isRuntimeSuspended);
    _ASSERTE(isRuntimeSuspended);
    UpdateGCWriteBarriers();
    return SWB_ICACHE_FLUSH;
}

void FlushWriteBarrierInstructionCache()
{
    // We've changed code so we must flush the instruction cache.
    BYTE *pbAlteredRange;
    DWORD cbAlteredRange;
    ComputeWriteBarrierRange(&pbAlteredRange, &cbAlteredRange);
    FlushInstructionCache(GetCurrentProcess(), pbAlteredRange, cbAlteredRange);
}


#endif // !DACCESS_COMPILE

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundstate,
                                    DWORD threadId,
                                    int funCallDepth,
                                    HostCallPreference hostCallPreference)
{
    T_CONTEXT                         ctx;
    T_KNONVOLATILE_CONTEXT_POINTERS   nonVolRegPtrs;

    ctx.ContextFlags = 0; // Read by PAL_VirtualUnwind.

    ctx.Pc = baseState->captureIp;
    ctx.Sp = baseState->captureSp;

    ctx.R4 = unwoundstate->captureR4_R11[0] = baseState->captureR4_R11[0];
    ctx.R5 = unwoundstate->captureR4_R11[1] = baseState->captureR4_R11[1];
    ctx.R6 = unwoundstate->captureR4_R11[2] = baseState->captureR4_R11[2];
    ctx.R7 = unwoundstate->captureR4_R11[3] = baseState->captureR4_R11[3];
    ctx.R8 = unwoundstate->captureR4_R11[4] = baseState->captureR4_R11[4];
    ctx.R9 = unwoundstate->captureR4_R11[5] = baseState->captureR4_R11[5];
    ctx.R10 = unwoundstate->captureR4_R11[6] = baseState->captureR4_R11[6];
    ctx.R11 = unwoundstate->captureR4_R11[7] = baseState->captureR4_R11[7];

#if !defined(DACCESS_COMPILE)
    // For DAC, if we get here, it means that the LazyMachState is uninitialized and we have to unwind it.
    // The API we use to unwind in DAC is StackWalk64(), which does not support the context pointers.
    //
    // Restore the integer registers to KNONVOLATILE_CONTEXT_POINTERS to be used for unwinding.
    nonVolRegPtrs.R4 = &unwoundstate->captureR4_R11[0];
    nonVolRegPtrs.R5 = &unwoundstate->captureR4_R11[1];
    nonVolRegPtrs.R6 = &unwoundstate->captureR4_R11[2];
    nonVolRegPtrs.R7 = &unwoundstate->captureR4_R11[3];
    nonVolRegPtrs.R8 = &unwoundstate->captureR4_R11[4];
    nonVolRegPtrs.R9 = &unwoundstate->captureR4_R11[5];
    nonVolRegPtrs.R10 = &unwoundstate->captureR4_R11[6];
    nonVolRegPtrs.R11 = &unwoundstate->captureR4_R11[7];
#endif // DACCESS_COMPILE

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    LazyMachState::unwindLazyState(ip:%p,sp:%p)\n", baseState->captureIp, baseState->captureSp));

    PCODE pvControlPc;

    do
    {
#ifndef TARGET_UNIX
        pvControlPc = Thread::VirtualUnwindCallFrame(&ctx, &nonVolRegPtrs);
#else // !TARGET_UNIX
#ifdef DACCESS_COMPILE
        HRESULT hr = DacVirtualUnwind(threadId, &ctx, &nonVolRegPtrs);
        if (FAILED(hr))
        {
            DacError(hr);
        }
#else // DACCESS_COMPILE
        BOOL success = PAL_VirtualUnwind(&ctx, &nonVolRegPtrs);
        if (!success)
        {
            _ASSERTE(!"unwindLazyState: Unwinding failed");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
#endif // DACCESS_COMPILE
        pvControlPc = GetIP(&ctx);
#endif // !TARGET_UNIX
        if (funCallDepth > 0)
        {
            --funCallDepth;
            if (funCallDepth == 0)
                break;
        }
        else
        {
            // Determine  whether given IP resides in JITted code. (It returns nonzero in that case.)
            // Use it now to see if we've unwound to managed code yet.
            BOOL fFailedReaderLock = FALSE;
            BOOL fIsManagedCode = ExecutionManager::IsManagedCode(pvControlPc, hostCallPreference, &fFailedReaderLock);
            if (fFailedReaderLock)
            {
                // We don't know if we would have been able to find a JIT
                // manager, because we couldn't enter the reader lock without
                // yielding (and our caller doesn't want us to yield).  So abort
                // now.

                // Invalidate the lazyState we're returning, so the caller knows
                // we aborted before we could fully unwind
                unwoundstate->_isValid = false;
                return;
            }

            if (fIsManagedCode)
                break;
        }
    }
    while(TRUE);

    //
    // Update unwoundState so that HelperMethodFrameRestoreState knows which
    // registers have been potentially modified.
    //

    unwoundstate->_pc = ctx.Pc;
    unwoundstate->_sp = ctx.Sp;

#ifdef DACCESS_COMPILE
    // For DAC builds, we update the registers directly since we dont have context pointers
    unwoundstate->captureR4_R11[0] = ctx.R4;
    unwoundstate->captureR4_R11[1] = ctx.R5;
    unwoundstate->captureR4_R11[2] = ctx.R6;
    unwoundstate->captureR4_R11[3] = ctx.R7;
    unwoundstate->captureR4_R11[4] = ctx.R8;
    unwoundstate->captureR4_R11[5] = ctx.R9;
    unwoundstate->captureR4_R11[6] = ctx.R10;
    unwoundstate->captureR4_R11[7] = ctx.R11;
#else // !DACCESS_COMPILE
    // For non-DAC builds, update the register state from context pointers
    unwoundstate->_R4_R11[0] = (PDWORD)nonVolRegPtrs.R4;
    unwoundstate->_R4_R11[1] = (PDWORD)nonVolRegPtrs.R5;
    unwoundstate->_R4_R11[2] = (PDWORD)nonVolRegPtrs.R6;
    unwoundstate->_R4_R11[3] = (PDWORD)nonVolRegPtrs.R7;
    unwoundstate->_R4_R11[4] = (PDWORD)nonVolRegPtrs.R8;
    unwoundstate->_R4_R11[5] = (PDWORD)nonVolRegPtrs.R9;
    unwoundstate->_R4_R11[6] = (PDWORD)nonVolRegPtrs.R10;
    unwoundstate->_R4_R11[7] = (PDWORD)nonVolRegPtrs.R11;
#endif // DACCESS_COMPILE

    unwoundstate->_isValid = true;
}

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    //
    // Copy the saved state from the frame to the current context.
    //

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HelperMethodFrame::UpdateRegDisplay cached ip:%p, sp:%p\n", m_MachState._pc, m_MachState._sp));

 #if defined(DACCESS_COMPILE)
    // For DAC, we may get here when the HMF is still uninitialized.
    // So we may need to unwind here.
    if (!m_MachState.isValid())
    {
        // This allocation throws on OOM.
        MachState* pUnwoundState = (MachState*)DacAllocHostOnlyInstance(sizeof(*pUnwoundState), true);

        InsureInit(false, pUnwoundState);

        pRD->pCurrentContext->Pc = pRD->ControlPC = pUnwoundState->_pc;
        pRD->pCurrentContext->Sp = pRD->SP        = pUnwoundState->_sp;

        pRD->pCurrentContext->R4 = (DWORD)(pUnwoundState->captureR4_R11[0]);
        pRD->pCurrentContext->R5 = (DWORD)(pUnwoundState->captureR4_R11[1]);
        pRD->pCurrentContext->R6 = (DWORD)(pUnwoundState->captureR4_R11[2]);
        pRD->pCurrentContext->R7 = (DWORD)(pUnwoundState->captureR4_R11[3]);
        pRD->pCurrentContext->R8 = (DWORD)(pUnwoundState->captureR4_R11[4]);
        pRD->pCurrentContext->R9 = (DWORD)(pUnwoundState->captureR4_R11[5]);
        pRD->pCurrentContext->R10 = (DWORD)(pUnwoundState->captureR4_R11[6]);
        pRD->pCurrentContext->R11 = (DWORD)(pUnwoundState->captureR4_R11[7]);

        return;
    }
#endif // DACCESS_COMPILE

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;
    pRD->ControlPC = GetReturnAddress();
    pRD->SP = (DWORD)(size_t)m_MachState._sp;

    pRD->pCurrentContext->Pc = pRD->ControlPC;
    pRD->pCurrentContext->Sp = pRD->SP;

    pRD->pCurrentContext->R4 = *m_MachState._R4_R11[0];
    pRD->pCurrentContext->R5 = *m_MachState._R4_R11[1];
    pRD->pCurrentContext->R6 = *m_MachState._R4_R11[2];
    pRD->pCurrentContext->R7 = *m_MachState._R4_R11[3];
    pRD->pCurrentContext->R8 = *m_MachState._R4_R11[4];
    pRD->pCurrentContext->R9 = *m_MachState._R4_R11[5];
    pRD->pCurrentContext->R10 = *m_MachState._R4_R11[6];
    pRD->pCurrentContext->R11 = *m_MachState._R4_R11[7];

    pRD->pCurrentContextPointers->R4 = m_MachState._R4_R11[0];
    pRD->pCurrentContextPointers->R5 = m_MachState._R4_R11[1];
    pRD->pCurrentContextPointers->R6 = m_MachState._R4_R11[2];
    pRD->pCurrentContextPointers->R7 = m_MachState._R4_R11[3];
    pRD->pCurrentContextPointers->R8 = m_MachState._R4_R11[4];
    pRD->pCurrentContextPointers->R9 = m_MachState._R4_R11[5];
    pRD->pCurrentContextPointers->R10 = m_MachState._R4_R11[6];
    pRD->pCurrentContextPointers->R11 = m_MachState._R4_R11[7];
    pRD->pCurrentContextPointers->Lr = NULL;
}

TADDR FixupPrecode::GetMethodDesc()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // This lookup is also manually inlined in PrecodeFixupThunk assembly code
    TADDR base = *PTR_TADDR(GetBase());
    if (base == NULL)
        return NULL;
    return base + (m_MethodDescChunkIndex * MethodDesc::ALIGNMENT);
}

#ifdef DACCESS_COMPILE
void FixupPrecode::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(this), sizeof(FixupPrecode));

    DacEnumMemoryRegion(GetBase(), sizeof(TADDR));
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE

void StubPrecode::Init(StubPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    int n = 0;

    m_rgCode[n++] = 0xf8df; // ldr r12, [pc, #8]
    m_rgCode[n++] = 0xc008;
    m_rgCode[n++] = 0xf8df; // ldr pc, [pc, #0]
    m_rgCode[n++] = 0xf000;

    _ASSERTE(n == _countof(m_rgCode));

    m_pTarget = GetPreStubEntryPoint();
    m_pMethodDesc = (TADDR)pMD;
}

void NDirectImportPrecode::Init(NDirectImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    int n = 0;

    m_rgCode[n++] = 0xf8df; // ldr r12, [pc, #4]
    m_rgCode[n++] = 0xc004;
    m_rgCode[n++] = 0xf8df; // ldr pc, [pc, #4]
    m_rgCode[n++] = 0xf004;

    _ASSERTE(n == _countof(m_rgCode));

    m_pMethodDesc = (TADDR)pMD;
    m_pTarget = GetEEFuncEntryPoint(NDirectImportThunk);
}

void FixupPrecode::Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator, int iMethodDescChunkIndex /*=0*/, int iPrecodeChunkIndex /*=0*/)
{
    WRAPPER_NO_CONTRACT;

    m_rgCode[0] = 0x46fc;   // mov r12, pc
    m_rgCode[1] = 0xf8df;   // ldr pc, [pc, #4]
    m_rgCode[2] = 0xf004;

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
        m_pTarget = GetEEFuncEntryPoint(PrecodeFixupThunk);
    }
}

void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    int n = 0;

    m_rgCode[n++] = 0x4684; // mov r12, r0
    m_rgCode[n++] = 0x4608; // mov r0, r1
    m_rgCode[n++] = 0xea4f; // mov r1, r12
    m_rgCode[n++] = 0x010c;
    m_rgCode[n++] = 0xf8df; // ldr pc, [pc, #0]
    m_rgCode[n++] = 0xf000;

    _ASSERTE(n == _countof(m_rgCode));

    m_pTarget = GetPreStubEntryPoint();
    m_pMethodDesc = (TADDR)pMD;
}


/*
Rough pseudo-code of interface dispatching:

  // jitted code sets r0, r4:
  r0 = object;
  r4 = indirectionCell;
  // jitted code calls *indirectionCell
  switch (*indirectionCell)
  {
      case LookupHolder._stub:
          // ResolveWorkerAsmStub:
          *indirectionCell = DispatchHolder._stub;
          call ResolveWorkerStatic, jump to target method;
      case DispatchHolder._stub:
          if (r0.methodTable == expectedMethodTable) jump to target method;
          // ResolveHolder._stub._failEntryPoint:
          jump to case ResolveHolder._stub._resolveEntryPoint;
      case ResolveHolder._stub._resolveEntryPoint:
          if (r0.methodTable in hashTable) jump to target method;
          // ResolveHolder._stub._slowEntryPoint:
          // ResolveWorkerChainLookupAsmStub:
          // ResolveWorkerAsmStub:
          if (_failEntryPoint called too many times) *indirectionCell = ResolveHolder._stub._resolveEntryPoint;
          call ResolveWorkerStatic, jump to target method;
  }

Note that ResolveWorkerChainLookupAsmStub currently points directly
to ResolveWorkerAsmStub; in the future, this could be separate.
*/

void  LookupHolder::Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken)
{
    // Called directly by JITTED code
    // See ResolveWorkerAsmStub

    // ldr r12, [pc + 8]    ; #_token
    _stub._entryPoint[0] = 0xf8df;
    _stub._entryPoint[1] = 0xc008;
    // ldr pc, [pc]         ; #_resolveWorkerTarget
    _stub._entryPoint[2] = 0xf8df;
    _stub._entryPoint[3] = 0xf000;

    _stub._resolveWorkerTarget = resolveWorkerTarget;
    _stub._token               = dispatchToken;
    _ASSERTE(4 == LookupStub::entryPointLen);
}

void  DispatchHolder::Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT)
{
    // Called directly by JITTED code
    // DispatchHolder._stub._entryPoint(r0:object, r1, r2, r3, r4:IndirectionCell)
    // {
    //     if (r0.methodTable == this._expectedMT) (this._implTarget)(r0, r1, r2, r3);
    //     else (this._failTarget)(r0, r1, r2, r3, r4);
    // }

    int n = 0;
    WORD offset;

    // We rely on the stub entry-point being DWORD aligned (so we can tell whether any subsequent WORD is
    // DWORD-aligned or not, which matters in the calculation of PC-relative offsets).
    _ASSERTE(((UINT_PTR)_stub._entryPoint & 0x3) == 0);

// Compute a PC-relative offset for use in an instruction encoding. Must call this prior to emitting the
// instruction halfword to which it applies. For thumb-2 encodings the offset must be computed before emitting
// the first of the halfwords.
#undef PC_REL_OFFSET
#define PC_REL_OFFSET(_field) (WORD)(offsetof(DispatchStub, _field) - ((offsetof(DispatchStub, _entryPoint) + sizeof(*DispatchStub::_entryPoint) * (n + 2)) & 0xfffffffc))

    // r0 : object. It can be null as well.
    // when it is null the code causes an AV. This AV is seen by the VM's personality routine
    // and it converts it into nullRef. We want the AV to happen before modifying the stack so that we can get the
    // call stack in windbg at the point of AV. So therefore "ldr r12, [r0]" should be the first instruction.

    // ldr r12, [r0 + #Object.m_pMethTab]
    _stub._entryPoint[n++] = DISPATCH_STUB_FIRST_WORD;
    _stub._entryPoint[n++] = 0xc000;

    // push {r5}
    _stub._entryPoint[n++] = 0xb420;

    // ldr r5, [pc + #_expectedMT]
    offset = PC_REL_OFFSET(_expectedMT);
    _ASSERTE((offset & 0x3) == 0);
    _stub._entryPoint[n++] = 0x4d00 | (offset >> 2);

    // cmp r5, r12
    _stub._entryPoint[n++] = 0x4565;

    // pop {r5}
    _stub._entryPoint[n++] = 0xbc20;

    // bne failTarget
    _stub._entryPoint[n++] = 0xd101;

    // ldr pc, [pc + #_implTarget]
    offset = PC_REL_OFFSET(_implTarget);
    _stub._entryPoint[n++] = 0xf8df;
    _stub._entryPoint[n++] = 0xf000 | offset;

    // failTarget:
    // ldr pc, [pc + #_failTarget]
    offset = PC_REL_OFFSET(_failTarget);
    _stub._entryPoint[n++] = 0xf8df;
    _stub._entryPoint[n++] = 0xf000 | offset;

    // nop - insert padding
    _stub._entryPoint[n++] = 0xbf00;

    _ASSERTE(n == DispatchStub::entryPointLen);

    // Make sure that the data members below are aligned
    _ASSERTE((n & 1) == 0);

    _stub._expectedMT = DWORD(expectedMT);
    _stub._failTarget = failTarget;
    _stub._implTarget = implTarget;
}

void ResolveHolder::Initialize(ResolveHolder* pResolveHolderRX,
                               PCODE resolveWorkerTarget, PCODE patcherTarget,
                                size_t dispatchToken, UINT32 hashedToken,
                                void * cacheAddr, INT32 * counterAddr)
{
    // Called directly by JITTED code
    // ResolveStub._resolveEntryPoint(r0:Object*, r1, r2, r3, r4:IndirectionCellAndFlags)
    // {
    //    MethodTable mt = r0.m_pMethTab;
    //    int i = ((mt + mt >> 12) ^ this._hashedToken) & this._cacheMask
    //    ResolveCacheElem e = this._cacheAddress + i
    //    do
    //    {
    //        if (mt == e.pMT && this._token == e.token) (e.target)(r0, r1, r2, r3);
    //        e = e.pNext;
    //    } while (e != null)
    //    (this._slowEntryPoint)(r0, r1, r2, r3, r4);
    // }
    //

    int n = 0;
    WORD offset;

    // We rely on the stub entry-point being DWORD aligned (so we can tell whether any subsequent WORD is
    // DWORD-aligned or not, which matters in the calculation of PC-relative offsets).
    _ASSERTE(((UINT_PTR)_stub._resolveEntryPoint & 0x3) == 0);

// Compute a PC-relative offset for use in an instruction encoding. Must call this prior to emitting the
// instruction halfword to which it applies. For thumb-2 encodings the offset must be computed before emitting
// the first of the halfwords.
#undef PC_REL_OFFSET
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - ((offsetof(ResolveStub, _resolveEntryPoint) + sizeof(*ResolveStub::_resolveEntryPoint) * (n + 2)) & 0xfffffffc))

    // ldr r12, [r0 + #Object.m_pMethTab]
    _stub._resolveEntryPoint[n++] = RESOLVE_STUB_FIRST_WORD;
    _stub._resolveEntryPoint[n++] = 0xc000;

    // ;; We need two scratch registers, r5 and r6
    // push {r5,r6}
    _stub._resolveEntryPoint[n++] = 0xb460;

    // ;; Compute i = ((mt + mt >> 12) ^ this._hashedToken) & this._cacheMask

    // add r6, r12, r12 lsr #12
    _stub._resolveEntryPoint[n++] = 0xeb0c;
    _stub._resolveEntryPoint[n++] = 0x361c;

    // ldr r5, [pc + #_hashedToken]
    offset = PC_REL_OFFSET(_hashedToken);
    _ASSERTE((offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x4d00 | (offset >> 2);

    // eor r6, r6, r5
    _stub._resolveEntryPoint[n++] = 0xea86;
    _stub._resolveEntryPoint[n++] = 0x0605;

    // ldr r5, [pc + #_cacheMask]
    offset = PC_REL_OFFSET(_cacheMask);
    _ASSERTE((offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x4d00 | (offset >> 2);

    // and r6, r6, r5
    _stub._resolveEntryPoint[n++] = 0xea06;
    _stub._resolveEntryPoint[n++] = 0x0605;

    // ;; ResolveCacheElem e = this._cacheAddress + i
    // ldr r5, [pc + #_cacheAddress]
    offset = PC_REL_OFFSET(_cacheAddress);
    _ASSERTE((offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x4d00 | (offset >> 2);

    // ldr r6, [r5 + r6] ;; r6 = e = this._cacheAddress + i
    _stub._resolveEntryPoint[n++] = 0x59ae;

    // ;; do {
    int loop = n;

    // ;; Check mt == e.pMT
    // ldr r5, [r6 + #ResolveCacheElem.pMT]
    offset = offsetof(ResolveCacheElem, pMT);
    _ASSERTE(offset <= 124 && (offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x6835 | (offset<< 4);

    // cmp r12, r5
    _stub._resolveEntryPoint[n++] = 0x45ac;

    // bne nextEntry
    _stub._resolveEntryPoint[n++] = 0xd108;

    // ;; Check this._token == e.token
    // ldr r5, [pc + #_token]
    offset = PC_REL_OFFSET(_token);
    _ASSERTE((offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x4d00 | (offset>>2);

    // ldr r12, [r6 + #ResolveCacheElem.token]
    offset = offsetof(ResolveCacheElem, token);
    _stub._resolveEntryPoint[n++] = 0xf8d6;
    _stub._resolveEntryPoint[n++] = 0xc000 | offset;

    // cmp r12, r5
    _stub._resolveEntryPoint[n++] = 0x45ac;

    // bne nextEntry
    _stub._resolveEntryPoint[n++] = 0xd103;

    // ldr r12, [r6 + #ResolveCacheElem.target] ;; r12 : e.target
    offset = offsetof(ResolveCacheElem, target);
    _stub._resolveEntryPoint[n++] = 0xf8d6;
    _stub._resolveEntryPoint[n++] = 0xc000 | offset;

    // ;; Restore r5 and r6
    // pop {r5,r6}
    _stub._resolveEntryPoint[n++] = 0xbc60;

    // ;; Branch to e.target
    // bx       r12 ;; (e.target)(r0,r1,r2,r3)
    _stub._resolveEntryPoint[n++] = 0x4760;

    // nextEntry:
    // ;; e = e.pNext;
    // ldr r6, [r6 + #ResolveCacheElem.pNext]
    offset = offsetof(ResolveCacheElem, pNext);
    _ASSERTE(offset <=124 && (offset & 0x3) == 0);
    _stub._resolveEntryPoint[n++] = 0x6836 | (offset << 4);

    // ;; } while(e != null);
    // cbz r6, slowEntryPoint
    _stub._resolveEntryPoint[n++] = 0xb116;

    // ldr r12, [r0 + #Object.m_pMethTab]
    _stub._resolveEntryPoint[n++] = 0xf8d0;
    _stub._resolveEntryPoint[n++] = 0xc000;

    // b loop
    offset = (WORD)((loop - (n + 2)) * sizeof(WORD));
    offset = (offset >> 1) & 0x07ff;
    _stub._resolveEntryPoint[n++] = 0xe000 | offset;

    // slowEntryPoint:
    // pop {r5,r6}
    _stub._resolveEntryPoint[n++] = 0xbc60;

    // nop for alignment
    _stub._resolveEntryPoint[n++] = 0xbf00;

    // the slow entry point be DWORD-aligned (see _ASSERTE below) insert nops if necessary .

    // ARMSTUB TODO: promotion

    // fall through to slow case
    _ASSERTE(_stub._resolveEntryPoint + n == _stub._slowEntryPoint);
    _ASSERTE(n == ResolveStub::resolveEntryPointLen);

    // ResolveStub._slowEntryPoint(r0:MethodToken, r1, r2, r3, r4:IndirectionCellAndFlags)
    // {
    //     r12 = this._tokenSlow;
    //     this._resolveWorkerTarget(r0, r1, r2, r3, r4, r12);
    // }

    // The following macro relies on this entry point being DWORD-aligned. We've already asserted that the
    // overall stub is aligned above, just need to check that the preceding stubs occupy an even number of
    // WORD slots.
    _ASSERTE((n & 1) == 0);

#undef PC_REL_OFFSET
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - ((offsetof(ResolveStub, _slowEntryPoint) + sizeof(*ResolveStub::_slowEntryPoint) * (n + 2)) & 0xfffffffc))

    n = 0;

    // ldr r12, [pc + #_tokenSlow]
    offset = PC_REL_OFFSET(_tokenSlow);
    _stub._slowEntryPoint[n++] = 0xf8df;
    _stub._slowEntryPoint[n++] = 0xc000 | offset;

    // ldr pc, [pc + #_resolveWorkerTarget]
    offset = PC_REL_OFFSET(_resolveWorkerTarget);
    _stub._slowEntryPoint[n++] = 0xf8df;
    _stub._slowEntryPoint[n++] = 0xf000 | offset;

    _ASSERTE(n == ResolveStub::slowEntryPointLen);

    // ResolveStub._failEntryPoint(r0:MethodToken, r1, r2, r3, r4:IndirectionCellAndFlags)
    // {
    //     if(--*(this._pCounter) < 0) r4 = r4 | SDF_ResolveBackPatch;
    //     this._resolveEntryPoint(r0, r1, r2, r3, r4);
    // }

    // The following macro relies on this entry point being DWORD-aligned. We've already asserted that the
    // overall stub is aligned above, just need to check that the preceding stubs occupy an even number of
    // WORD slots.
    _ASSERTE((n & 1) == 0);

#undef PC_REL_OFFSET
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - ((offsetof(ResolveStub, _failEntryPoint) + sizeof(*ResolveStub::_failEntryPoint) * (n + 2)) & 0xfffffffc))

    n = 0;

    // push {r5}
    _stub._failEntryPoint[n++] = 0xb420;

    // ldr r5, [pc + #_pCounter]
    offset = PC_REL_OFFSET(_pCounter);
    _ASSERTE((offset & 0x3) == 0);
    _stub._failEntryPoint[n++] = 0x4d00 | (offset >>2);

    // ldr r12, [r5]
    _stub._failEntryPoint[n++] = 0xf8d5;
    _stub._failEntryPoint[n++] = 0xc000;

    // subs r12, r12, #1
    _stub._failEntryPoint[n++] = 0xf1bc;
    _stub._failEntryPoint[n++] = 0x0c01;

    // str r12, [r5]
    _stub._failEntryPoint[n++] = 0xf8c5;
    _stub._failEntryPoint[n++] = 0xc000;

    // pop {r5}
    _stub._failEntryPoint[n++] = 0xbc20;

    // bge resolveEntryPoint
    _stub._failEntryPoint[n++] = 0xda01;

    // or r4, r4, SDF_ResolveBackPatch
    _ASSERTE(SDF_ResolveBackPatch < 256);
    _stub._failEntryPoint[n++] = 0xf044;
    _stub._failEntryPoint[n++] = 0x0400 | SDF_ResolveBackPatch;

    // resolveEntryPoint:
    // b _resolveEntryPoint
    offset = (WORD)(offsetof(ResolveStub, _resolveEntryPoint) - (offsetof(ResolveStub, _failEntryPoint) + sizeof(*ResolveStub::_failEntryPoint) * (n + 2)));
    _ASSERTE((offset & 1) == 0);
    offset = (offset >> 1) & 0x07ff;
    _stub._failEntryPoint[n++] = 0xe000 | offset;

    // nop for alignment
    _stub._failEntryPoint[n++] = 0xbf00;

    _ASSERTE(n == ResolveStub::failEntryPointLen);

    _stub._pCounter            = counterAddr;
    _stub._hashedToken         = hashedToken << LOG2_PTRSIZE;
    _stub._cacheAddress        = (size_t) cacheAddr;
    _stub._token               = dispatchToken;
    _stub._tokenSlow           = dispatchToken;
    _stub._resolveWorkerTarget = resolveWorkerTarget;
    _stub._cacheMask           = CALL_STUB_CACHE_MASK * sizeof(void*);

    _ASSERTE(resolveWorkerTarget == (PCODE)ResolveWorkerChainLookupAsmStub);
    _ASSERTE(patcherTarget == NULL);
}

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    PTR_WORD pInstr = dac_cast<PTR_WORD>(PCODEToPINSTR(pCode));

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (MethodDescChunk::GetMethodDescFromCompactEntryPoint(pCode, TRUE) != NULL)
    {
        return TRUE;
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    // FixupPrecode
    if (pInstr[0] == 0x46fc && // // mov r12, pc
        pInstr[1] == 0xf8df &&
        pInstr[2] == 0xf004)
    {
        PCODE pTarget = dac_cast<PTR_FixupPrecode>(pInstr)->m_pTarget;

        // Check for jump stub (NGen case)
        if (isJump(pTarget))
        {
            pTarget = decodeJump(pTarget);
        }

        return pTarget == (TADDR)PrecodeFixupThunk;
    }

    // StubPrecode
    if (pInstr[0] == 0xf8df && // ldr r12, [pc + 8]
        pInstr[1] == 0xc008 &&
        pInstr[2] == 0xf8df && // ldr pc, [pc]
        pInstr[3] == 0xf000)
    {
        PCODE pTarget = dac_cast<PTR_StubPrecode>(pInstr)->m_pTarget;

        // Check for jump stub (NGen case)
        if (isJump(pTarget))
        {
            pTarget = decodeJump(pTarget);
        }

        return pTarget == GetPreStubEntryPoint();
    }

    return FALSE;
}

Stub *GenerateInitPInvokeFrameHelper()
{
    CONTRACT(Stub*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CPUSTUBLINKER sl;
    CPUSTUBLINKER *psl = &sl;

    CORINFO_EE_INFO::InlinedCallFrameInfo FrameInfo;
    InlinedCallFrame::GetEEInfo(&FrameInfo);

    // R4 contains address of the frame on stack (the frame ptr, not its neg space)
    unsigned negSpace = FrameInfo.offsetOfFrameVptr;

    ThumbReg regFrame   = ThumbReg(4);
    ThumbReg regThread  = ThumbReg(5);
    ThumbReg regScratch = ThumbReg(6);
    ThumbReg regR9 = ThumbReg(9);

#ifdef TARGET_UNIX
    // Erect frame to perform call to GetThread
    psl->ThumbEmitProlog(1, sizeof(ArgumentRegisters), FALSE); // Save r4 for aligned stack

    // Save argument registers around the GetThread call. Don't bother with using ldm/stm since this inefficient path anyway.
    for (int reg = 0; reg < 4; reg++)
        psl->ThumbEmitStoreRegIndirect(ThumbReg(reg), thumbRegSp, offsetof(ArgumentRegisters, r) + sizeof(*ArgumentRegisters::r) * reg);
#endif

    psl->ThumbEmitGetThread(regThread);

#ifdef TARGET_UNIX
    for (int reg = 0; reg < 4; reg++)
        psl->ThumbEmitLoadRegIndirect(ThumbReg(reg), thumbRegSp, offsetof(ArgumentRegisters, r) + sizeof(*ArgumentRegisters::r) * reg);
#endif

    // mov [regFrame + FrameInfo.offsetOfGSCookie], GetProcessGSCookie()
    psl->ThumbEmitMovConstant(regScratch, GetProcessGSCookie());
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfGSCookie - negSpace);

    // mov [regFrame + FrameInfo.offsetOfFrameVptr], InlinedCallFrame::GetMethodFrameVPtr()
    psl->ThumbEmitMovConstant(regScratch, InlinedCallFrame::GetMethodFrameVPtr());
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfFrameVptr - negSpace);

    // ldr regScratch, [regThread + offsetof(Thread, m_pFrame)]
    // str regScratch, [regFrame + FrameInfo.offsetOfFrameLink]
    psl->ThumbEmitLoadRegIndirect(regScratch, regThread, offsetof(Thread, m_pFrame));
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfFrameLink - negSpace);

    // str FP, [regFrame + FrameInfo.offsetOfCalleeSavedFP]
    psl->ThumbEmitStoreRegIndirect(thumbRegFp, regFrame, FrameInfo.offsetOfCalleeSavedFP - negSpace);

    // str R9, [regFrame + FrameInfo.offsetOfSPAfterProlog]
    psl->ThumbEmitStoreRegIndirect(regR9, regFrame, FrameInfo.offsetOfSPAfterProlog - negSpace);

    // mov [regFrame + FrameInfo.offsetOfReturnAddress], 0
    psl->ThumbEmitMovConstant(regScratch, 0);
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfReturnAddress - negSpace);

#ifdef TARGET_UNIX
    DWORD cbSavedRegs = sizeof(ArgumentRegisters) + 2 * 4; // r0-r3, r4, lr
    psl->ThumbEmitAdd(regScratch, thumbRegSp, cbSavedRegs);
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfCallSiteSP - negSpace);
#else
    // str SP, [regFrame + FrameInfo.offsetOfCallSiteSP]
    psl->ThumbEmitStoreRegIndirect(thumbRegSp, regFrame, FrameInfo.offsetOfCallSiteSP - negSpace);
#endif

    // mov [regThread + offsetof(Thread, m_pFrame)], regFrame
    psl->ThumbEmitStoreRegIndirect(regFrame, regThread, offsetof(Thread, m_pFrame));

    // leave current Thread in R4

#ifdef TARGET_UNIX
    psl->ThumbEmitEpilog();
#else
    // Return. The return address has been restored into LR at this point.
    // bx lr
    psl->ThumbEmitJumpRegister(thumbRegLr);
#endif

    // A single process-wide stub that will never unload
    RETURN psl->Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap());
}

void StubLinkerCPU::ThumbEmitGetThread(ThumbReg dest)
{
#ifdef TARGET_UNIX

    ThumbEmitMovConstant(ThumbReg(0), (TADDR)GetThreadHelper);

    ThumbEmitCallRegister(ThumbReg(0));

    if (dest != ThumbReg(0))
    {
        ThumbEmitMovRegReg(dest, ThumbReg(0));
    }

#else // TARGET_UNIX

    // mrc p15, 0, dest, c13, c0, 2
    Emit16(0xee1d);
    Emit16((WORD)(0x0f50 | (dest << 12)));

    ThumbEmitLoadRegIndirect(dest, dest, offsetof(TEB, ThreadLocalStoragePointer));

    ThumbEmitLoadRegIndirect(dest, dest, sizeof(void *) * _tls_index);

    ThumbEmitLoadRegIndirect(dest, dest, (int)Thread::GetOffsetOfThreadStatic(&gCurrentThreadInfo));

#endif // TARGET_UNIX
}


// Emits code to adjust for a static delegate target.
VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    // Scan the shuffle entries to see if there any stack-to-stack operations. If there aren't we can emit a
    // much simpler thunk (simply because we generate code that doesn't require more than one scratch
    // register).
    bool fSimpleCase = true;
    ShuffleEntry *pEntry = pShuffleEntryArray;
    while (pEntry->srcofs != ShuffleEntry::SENTINEL)
    {
        // It's enough to check whether we have a destination stack location (there are no register to stack
        // scenarios).
        if (!(pEntry->dstofs & ShuffleEntry::REGMASK))
        {
            fSimpleCase = false;
            break;
        }
        pEntry++;
    }

    if (fSimpleCase)
    {
        // No real prolog for the simple case, we're a tail call so we shouldn't be on the stack for any walk
        // or unwind.

        // On entry r0 holds the delegate instance. Look up the real target address stored in the MethodPtrAux
        // field and stash it in r12.
        //  ldr r12, [r0, #offsetof(DelegateObject, _methodPtrAux)]
        ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(0), DelegateObject::GetOffsetOfMethodPtrAux());

        // Emit the instructions to rewrite the argument registers. Most will be register-to-register (e.g.
        // move r1 to r0) but one or two of them might move values from the top of the incoming stack
        // arguments into registers r2 and r3. Note that the entries are ordered so that we don't need to
        // worry about a move overwriting a register we'll need to use as input for the next move (i.e. we get
        // move r1 to r0, move r2 to r1 etc.).
        pEntry = pShuffleEntryArray;
        while (pEntry->srcofs != ShuffleEntry::SENTINEL)
        {
            _ASSERTE(pEntry->dstofs & ShuffleEntry::REGMASK);

            if (pEntry->srcofs & ShuffleEntry::REGMASK)
            {
                // Move from register case.
                ThumbEmitMovRegReg(ThumbReg(pEntry->dstofs & ShuffleEntry::OFSMASK),
                                   ThumbReg(pEntry->srcofs & ShuffleEntry::OFSMASK));
            }
            else
            {
                // Move from the stack case.
                //  ldr <dest>, [sp + #source_offset]
                ThumbEmitLoadRegIndirect(ThumbReg(pEntry->dstofs & ShuffleEntry::OFSMASK),
                                         thumbRegSp,
                                         (pEntry->srcofs & ShuffleEntry::OFSMASK) * 4);
            }

            pEntry++;
        }

        // Tail call to real target.
        //  bx r12
        ThumbEmitJumpRegister(ThumbReg(12));

        return;
    }

    // In the more complex case we need to re-write at least some of the arguments on the stack as well as
    // argument registers. We need some temporary registers to perform stack-to-stack copies and we've
    // reserved our one remaining volatile register, r12, to store the eventual target method address. So
    // we're going to generate a hybrid-tail call. Using a tail call has the advantage that we don't need to
    // erect and link an explicit CLR frame to enable crawling of this thunk. Additionally re-writing the
    // stack can be more peformant in some scenarios than copying the stack (in the presence of floating point
    // or arguments requieing 64-bit alignment we might not have to move some or even most of the values).
    // The hybrid nature is that we'll erect a standard native frame (with a proper prolog and epilog) so we
    // can save some non-volatile registers to act as temporaries. Once we've performed the stack re-write
    // we'll poke the saved LR value (which will become a PC value on the pop in the epilog) to return to the
    // target method instead of us, thus atomically removing our frame from the stack and tail-calling the
    // real target.

    // Prolog:
    ThumbEmitProlog(3,      // Save r4-r6,lr (count doesn't include lr)
                    0,      // No additional space in the stack frame required
                    FALSE); // Don't push argument registers

    // On entry r0 holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and stash it in r12.
    //  ldr r12, [r0, #offsetof(DelegateObject, _methodPtrAux)]
    ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(0), DelegateObject::GetOffsetOfMethodPtrAux());

    // As we copy slots from lower in the argument stack to higher we need to keep track of source and
    // destination pointers into those arguments (if we just use offsets from SP we get into trouble with
    // argument frames larger than 4K). We'll use r4 to track the source (original location of an argument
    // from the caller's perspective) and r5 to track the destination (new location of the argument from the
    // callee's perspective). Both start at the current value of SP plus the offset created by pushing our
    // stack frame in the prolog.
    //  add r4, sp, #cbSavedRegs
    //  add r5, sp, #cbSavedRegs
    DWORD cbSavedRegs = 4 * 4; // r4, r5, r6, lr
    ThumbEmitAdd(ThumbReg(4), thumbRegSp, cbSavedRegs);
    ThumbEmitAdd(ThumbReg(5), thumbRegSp, cbSavedRegs);

    // Follow the shuffle array instructions to re-write some subset of r0-r3 and the stacked arguments to
    // remove the unwanted delegate instance in r0. Arguments only ever move from higher registers to lower
    // registers or higher stack addresses to lower stack addresses and are ordered from lowest register to
    // highest stack address. As a result we can do all updates in order and in place and we'll never
    // overwrite a register or stack location needed as a source value in a later iteration.
    DWORD dwLastSrcIndex = (DWORD)-1;
    DWORD dwLastDstIndex = (DWORD)-1;
    pEntry = pShuffleEntryArray;
    while (pEntry->srcofs != ShuffleEntry::SENTINEL)
    {
        // If this is a register-to-register move we can do it in one instruction.
        if ((pEntry->srcofs & ShuffleEntry::REGMASK) && (pEntry->dstofs & ShuffleEntry::REGMASK))
        {
            ThumbEmitMovRegReg(ThumbReg(pEntry->dstofs & ShuffleEntry::OFSMASK),
                               ThumbReg(pEntry->srcofs & ShuffleEntry::OFSMASK));
        }
        else
        {
            // There is no case where a source argument register is moved into a destination stack slot.
            _ASSERTE((pEntry->srcofs & ShuffleEntry::REGMASK) == 0);

            // Source or destination stack offsets might not be contiguous (though they often will be).
            // Floating point arguments and 64-bit aligned values can cause discontinuities. While we copy
            // values we'll use post increment addressing modes to move both source and destination stack
            // pointers forward 4 bytes at a time, the common case. But we'll insert additional add
            // instructions for any holes we find (we detect these by remembering the last source and
            // destination stack offset we used).

            // Add any additional offset to the source pointer (r4) to account for holes in the copy.
            DWORD dwSrcIndex = pEntry->srcofs & ShuffleEntry::OFSMASK;
            if (dwSrcIndex != (dwLastSrcIndex + 1))
            {
                _ASSERTE(dwSrcIndex > dwLastSrcIndex);

                // add r4, #gap_size
                ThumbEmitIncrement(ThumbReg(4), (dwSrcIndex - dwLastSrcIndex - 1) * 4);
            }
            dwLastSrcIndex = dwSrcIndex;

            // Load the source value from the stack and increment our source pointer (r4) in one instruction.
            // If the target is a register we can move the value directly there. Otherwise we move it to the
            // r6 temporary register.
            if (pEntry->dstofs & ShuffleEntry::REGMASK)
            {
                // ldr <regnum>, [r4], #4
                ThumbEmitLoadIndirectPostIncrement(ThumbReg(pEntry->dstofs & ShuffleEntry::OFSMASK), ThumbReg(4), 4);
            }
            else
            {
                // ldr r6, [r4], #4
                ThumbEmitLoadIndirectPostIncrement(ThumbReg(6), ThumbReg(4), 4);

                // Add any additional offset to the destination pointer (r5) to account for holes in the copy.
                DWORD dwDstIndex = pEntry->dstofs & ShuffleEntry::OFSMASK;
                if (dwDstIndex != (dwLastDstIndex + 1))
                {
                    _ASSERTE(dwDstIndex > dwLastDstIndex);

                    // add r5, #gap_size
                    ThumbEmitIncrement(ThumbReg(5), (dwDstIndex - dwLastDstIndex - 1) * 4);
                }
                dwLastDstIndex = dwDstIndex;

                // Write the value in r6 to it's final home on the stack and increment our destination pointer
                // (r5).
                //  str r6, [r5], #4
                ThumbEmitStoreIndirectPostIncrement(ThumbReg(6), ThumbReg(5), 4);
            }
        }

        pEntry++;
    }

    // Arguments are copied. Now we modify the saved value of LR we created in our prolog (which will be
    // popped back off into PC in our epilog) so that it points to the real target address in r12 rather than
    // our return address. We haven't modified LR ourselves, so the net result is that executing our epilog
    // will pop our frame and tail call to the real method.
    //  str r12, [sp + #(cbSavedRegs-4)]
    ThumbEmitStoreRegIndirect(ThumbReg(12), thumbRegSp, cbSavedRegs - 4);

    // Epilog:
    ThumbEmitEpilog();
}


void StubLinkerCPU::ThumbEmitTailCallManagedMethod(MethodDesc *pMD)
{
    bool isRelative = MethodTable::VTableIndir2_t::isRelative
                      && pMD->IsVtableSlot();

    _ASSERTE(!isRelative);

    // Use direct call if possible.
    if (pMD->HasStableEntryPoint())
    {
        // mov r12, #entry_point
        ThumbEmitMovConstant(ThumbReg(12), (TADDR)pMD->GetStableEntryPoint());
    }
    else
    {
        // mov r12, #slotaddress
        ThumbEmitMovConstant(ThumbReg(12), (TADDR)pMD->GetAddrOfSlot());

        if (isRelative)
        {
            // mov r4, r12
            ThumbEmitMovRegReg(ThumbReg(4), ThumbReg(12));
        }

        // ldr r12, [r12]
        ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(12), 0);

        if (isRelative)
        {
            // add r12, r4
            ThumbEmitAddReg(ThumbReg(12), ThumbReg(4));
        }
    }

    if (!isRelative)
    {
        // bx r12
        ThumbEmitJumpRegister(ThumbReg(12));
    }
    else
    {
        // Replace LR with R12 on stack: hybrid-tail call, same as for EmitShuffleThunk
        // str r12, [sp, 4]
        ThumbEmitStoreRegIndirect(ThumbReg(12), thumbRegSp, 4);
    }
}

VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    STANDARD_VM_CONTRACT;

    struct ShuffleEntry *pEntry = pShuffleEntryArray;
    while (pEntry->srcofs != ShuffleEntry::SENTINEL)
    {
        _ASSERTE(pEntry->dstofs & ShuffleEntry::REGMASK);
        _ASSERTE(pEntry->srcofs & ShuffleEntry::REGMASK);
        _ASSERTE(!(pEntry->dstofs & ShuffleEntry::FPREGMASK));
        _ASSERTE(!(pEntry->srcofs & ShuffleEntry::FPREGMASK));
        _ASSERTE(pEntry->dstofs != ShuffleEntry::HELPERREG);
        _ASSERTE(pEntry->srcofs != ShuffleEntry::HELPERREG);

        ThumbEmitMovRegReg(ThumbReg(pEntry->dstofs & ShuffleEntry::OFSMASK),
                            ThumbReg(pEntry->srcofs & ShuffleEntry::OFSMASK));

        pEntry++;
    }

    MetaSig msig(pSharedMD);
    ArgIterator argit(&msig);

    if (argit.HasParamType())
    {
        // Place instantiation parameter into the correct register.
        ArgLocDesc sInstArgLoc;
        argit.GetParamTypeLoc(&sInstArgLoc);
        int regHidden = sInstArgLoc.m_idxGenReg;
        _ASSERTE(regHidden != -1);
        if (extraArg == NULL)
        {
            if (pSharedMD->RequiresInstMethodTableArg())
            {
                // Unboxing stub case
                // Extract MethodTable pointer (the hidden arg) from the object instance.
                //  ldr regHidden, [r0]
                ThumbEmitLoadRegIndirect(ThumbReg(regHidden), ThumbReg(0), 0);
            }
        }
        else
        {
            // mov regHidden, #pHiddenArg
            ThumbEmitMovConstant(ThumbReg(regHidden), (TADDR)extraArg);
        }
    }

    if (extraArg == NULL)
    {
        // Unboxing stub case
        // Skip over the MethodTable* to find the address of the unboxed value type.
        //  add r0, #sizeof(MethodTable*)
        ThumbEmitIncrement(ThumbReg(0), sizeof(MethodTable*));
    }

    bool isRelative = MethodTable::VTableIndir2_t::isRelative
                      && pSharedMD->IsVtableSlot();

    _ASSERTE(!isRelative);

    if (isRelative)
    {
        ThumbEmitProlog(1, 0, FALSE);
    }

    ThumbEmitTailCallManagedMethod(pSharedMD);

    if (isRelative)
    {
        ThumbEmitEpilog();
    }
}


#endif // !DACCESS_COMPILE

LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
    return EXCEPTION_CONTINUE_SEARCH;
}

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pRegs)
{
    LIMITED_METHOD_CONTRACT;

    T_CONTEXT * pContext = pRD->pCurrentContext;
    pContext->R4 = pRegs->r4;
    pContext->R5 = pRegs->r5;
    pContext->R6 = pRegs->r6;
    pContext->R7 = pRegs->r7;
    pContext->R8 = pRegs->r8;
    pContext->R9 = pRegs->r9;
    pContext->R10 = pRegs->r10;
    pContext->R11 = pRegs->r11;
    pContext->Lr = pRegs->r14;

    T_KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
    pRD->pCurrentContextPointers->R4 = (PDWORD)&pRegs->r4;
    pRD->pCurrentContextPointers->R5 = (PDWORD)&pRegs->r5;
    pRD->pCurrentContextPointers->R6 = (PDWORD)&pRegs->r6;
    pRD->pCurrentContextPointers->R7 = (PDWORD)&pRegs->r7;
    pRD->pCurrentContextPointers->R8 = (PDWORD)&pRegs->r8;
    pRD->pCurrentContextPointers->R9 = (PDWORD)&pRegs->r9;
    pRD->pCurrentContextPointers->R10 = (PDWORD)&pRegs->r10;
    pRD->pCurrentContextPointers->R11 = (PDWORD)&pRegs->r11;
    pRD->pCurrentContextPointers->Lr = NULL;
}

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    // Copy the saved argument registers into the current context
    ArgumentRegisters * pArgRegs = GetArgumentRegisters();
    pRD->pCurrentContext->R0 = pArgRegs->r[0];
    pRD->pCurrentContext->R1 = pArgRegs->r[1];
    pRD->pCurrentContext->R2 = pArgRegs->r[2];
    pRD->pCurrentContext->R3 = pArgRegs->r[3];

    // Next, copy all the callee saved registers
    UpdateRegDisplayFromCalleeSavedRegisters(pRD, GetCalleeSavedRegisters());

    // Set ControlPC to be the same as the saved "return address"
    // value, which is actually a ControlPC in the frameless method (e.g.
    // faulting address incase of AV or TAE).
    pRD->pCurrentContext->Pc = GetReturnAddress();

    // Set the caller SP
    pRD->pCurrentContext->Sp = this->GetSP();

    // Finally, syncup the regdisplay with the context
    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Copy the context to regdisplay
    memcpy(pRD->pCurrentContext, &m_ctx, sizeof(T_CONTEXT));

    pRD->ControlPC = ::GetIP(&m_ctx);
    pRD->SP = ::GetSP(&m_ctx);

    // Update the integer registers in KNONVOLATILE_CONTEXT_POINTERS from
    // the exception context we have.
    pRD->pCurrentContextPointers->R4 = (PDWORD)&m_ctx.R4;
    pRD->pCurrentContextPointers->R5 = (PDWORD)&m_ctx.R5;
    pRD->pCurrentContextPointers->R6 = (PDWORD)&m_ctx.R6;
    pRD->pCurrentContextPointers->R7 = (PDWORD)&m_ctx.R7;
    pRD->pCurrentContextPointers->R8 = (PDWORD)&m_ctx.R8;
    pRD->pCurrentContextPointers->R9 = (PDWORD)&m_ctx.R9;
    pRD->pCurrentContextPointers->R10 = (PDWORD)&m_ctx.R10;
    pRD->pCurrentContextPointers->R11 = (PDWORD)&m_ctx.R11;
    pRD->pCurrentContextPointers->Lr = NULL;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        // We should skip over InlinedCallFrame if it is not active.
        // It will be part of a JITed method's frame, and the stack-walker
        // can handle such a case.
#ifdef PROFILING_SUPPORTED
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        HOST_NOCALLS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // @TODO: Remove this after the debugger is fixed to avoid stack-walks from bad places
    // @TODO: This may be still needed for sampling profilers
    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

    *(pRD->pPC) = m_pCallerReturnAddress;
    pRD->SP = (DWORD) dac_cast<TADDR>(m_pCallSiteSP);

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Pc = *(pRD->pPC);
    pRD->pCurrentContext->Sp = pRD->SP;

    // Update the frame pointer in the current context.
    pRD->pCurrentContext->R11 = m_pCalleeSavedFP;
    pRD->pCurrentContextPointers->R11 = &m_pCalleeSavedFP;

    // This is necessary to unwind methods with alloca. This needs to stay
    // in sync with definition of REG_SAVED_LOCALLOC_SP in the JIT.
    pRD->pCurrentContext->R9 = (DWORD) dac_cast<TADDR>(m_pSPAfterProlog);
    pRD->pCurrentContextPointers->R9 = (DWORD *)&m_pSPAfterProlog;

    RETURN;
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr(void)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(T_CONTEXT, Pc);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CopyMemory(pRD->pCurrentContext, m_Regs, sizeof(T_CONTEXT));

    pRD->ControlPC = m_Regs->Pc;
    pRD->SP = m_Regs->Sp;

    pRD->pCurrentContextPointers->R4 = &m_Regs->R4;
    pRD->pCurrentContextPointers->R5 = &m_Regs->R5;
    pRD->pCurrentContextPointers->R6 = &m_Regs->R6;
    pRD->pCurrentContextPointers->R7 = &m_Regs->R7;
    pRD->pCurrentContextPointers->R8 = &m_Regs->R8;
    pRD->pCurrentContextPointers->R9 = &m_Regs->R9;
    pRD->pCurrentContextPointers->R10 = &m_Regs->R10;
    pRD->pCurrentContextPointers->R11 = &m_Regs->R11;
    pRD->pCurrentContextPointers->Lr = &m_Regs->Lr;

    pRD->volatileCurrContextPointers.R0 = &m_Regs->R0;
    pRD->volatileCurrContextPointers.R1 = &m_Regs->R1;
    pRD->volatileCurrContextPointers.R2 = &m_Regs->R2;
    pRD->volatileCurrContextPointers.R3 = &m_Regs->R3;
    pRD->volatileCurrContextPointers.R12 = &m_Regs->R12;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
}

void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
     CONTRACTL {
         NOTHROW;
         GC_NOTRIGGER;
         SUPPORTS_DAC;
     }
     CONTRACTL_END;

     pRD->IsCallerContextValid = FALSE;
     pRD->IsCallerSPValid      = FALSE;

     pRD->pCurrentContext->Pc = m_ReturnAddress;
     pRD->pCurrentContext->Sp = PTR_TO_TADDR(m_Args) + sizeof(struct HijackArgs);

     pRD->pCurrentContext->R0 = m_Args->R0;

     pRD->pCurrentContext->R4 = m_Args->R4;
     pRD->pCurrentContext->R5 = m_Args->R5;
     pRD->pCurrentContext->R6 = m_Args->R6;
     pRD->pCurrentContext->R7 = m_Args->R7;
     pRD->pCurrentContext->R8 = m_Args->R8;
     pRD->pCurrentContext->R9 = m_Args->R9;
     pRD->pCurrentContext->R10 = m_Args->R10;
     pRD->pCurrentContext->R11 = m_Args->R11;

     pRD->pCurrentContextPointers->R4 = &m_Args->R4;
     pRD->pCurrentContextPointers->R5 = &m_Args->R5;
     pRD->pCurrentContextPointers->R6 = &m_Args->R6;
     pRD->pCurrentContextPointers->R7 = &m_Args->R7;
     pRD->pCurrentContextPointers->R8 = &m_Args->R8;
     pRD->pCurrentContextPointers->R9 = &m_Args->R9;
     pRD->pCurrentContextPointers->R10 = &m_Args->R10;
     pRD->pCurrentContextPointers->R11 = &m_Args->R11;
     pRD->pCurrentContextPointers->Lr = NULL;

     SyncRegDisplayToCurrentContext(pRD);
}
#endif // FEATURE_HIJACK

class UMEntryThunk * UMEntryThunk::Decode(void *pCallback)
{
    _ASSERTE(offsetof(UMEntryThunkCode, m_code) == 0);
    UMEntryThunkCode * pCode = (UMEntryThunkCode*)((ULONG_PTR)pCallback & ~THUMB_CODE);

    // We may be called with an unmanaged external code pointer instead. So if it doesn't look like one of our
    // stubs (see UMEntryThunkCode::Encode below) then we'll return NULL. Luckily in these scenarios our
    // caller will perform a hash lookup on successful return to verify our result in case random unmanaged
    // code happens to look like ours.
    if ((pCode->m_code[0] == 0xf8df) &&
        (pCode->m_code[1] == 0xc008) &&
        (pCode->m_code[2] == 0xf8df) &&
        (pCode->m_code[3] == 0xf000))
    {
        return (UMEntryThunk*)pCode->m_pvSecretParam;
    }

    return NULL;
}

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    // ldr r12, [pc + 8]
    m_code[0] = 0xf8df;
    m_code[1] = 0xc008;
    // ldr pc, [pc]
    m_code[2] = 0xf8df;
    m_code[3] = 0xf000;

    m_pTargetCode = (TADDR)pTargetCode;
    m_pvSecretParam = (TADDR)pvSecretParam;

    FlushInstructionCache(GetCurrentProcess(),&pEntryThunkCodeRX->m_code,sizeof(m_code));
}

#ifndef DACCESS_COMPILE

void UMEntryThunkCode::Poison()
{
    ExecutableWriterHolder<UMEntryThunkCode> thunkWriterHolder(this, sizeof(UMEntryThunkCode));
    UMEntryThunkCode *pThisRW = thunkWriterHolder.GetRW();

    pThisRW->m_pTargetCode = (TADDR)UMEntryThunk::ReportViolation;

    // ldr r0, [pc + 8]
    pThisRW->m_code[0] = 0x4802;
    // nop
    pThisRW->m_code[1] = 0xbf00;

    ClrFlushInstructionCache(&m_code,sizeof(m_code));
}

#endif // DACCESS_COMPILE

///////////////////////////// UNIMPLEMENTED //////////////////////////////////

#ifndef DACCESS_COMPILE


extern "C" void STDCALL JIT_PatchedCodeStart();
extern "C" void STDCALL JIT_PatchedCodeLast();

void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

    // Allocation helpers, faster but non-logging.
    if (!(TrackAllocationsEnabled()
          || LoggingOn(LF_GCALLOC, LL_INFO10)
#ifdef _DEBUG
          || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif // _DEBUG
        ))
    {
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

        SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_NewS_MP_FastPortable);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_FastPortable);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_FastPortable);

        ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateString_MP_FastPortable), ECall::FastAllocateString);
    }
}


VOID ResetCurrentContext()
{
    LIMITED_METHOD_CONTRACT;
}
#endif // !DACCESS_COMPILE


#ifdef FEATURE_COMINTEROP
void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target)
{
    WRAPPER_NO_CONTRACT;

    // mov r12, pc
    // ldr pc, [pc, #0]
    // dcd 0
    // dcd target
    WORD rgCode[] = {
        0x46fc,
        0xf8df, 0xf004
    };

    BYTE *pBufferRX = (BYTE*)pCOMMethodRX - COMMETHOD_CALL_PRESTUB_SIZE;
    BYTE *pBufferRW = (BYTE*)pCOMMethodRW - COMMETHOD_CALL_PRESTUB_SIZE;

    memcpy(pBufferRW, rgCode, sizeof(rgCode));
    *((PCODE*)(pBufferRW + sizeof(rgCode) + 2)) = target;

    // Ensure that the updated instructions get actually written
    ClrFlushInstructionCache(pBufferRX, COMMETHOD_CALL_PRESTUB_SIZE);

    _ASSERTE(IS_ALIGNED(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET, sizeof(void*)) &&
             *((PCODE*)(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET)) == target);
}
#endif // FEATURE_COMINTEROP

void MovRegImm(BYTE* p, int reg, TADDR imm)
{
    LIMITED_METHOD_CONTRACT;
    *(WORD *)(p + 0) = 0xF240;
    *(WORD *)(p + 2) = (UINT16)(reg << 8);
    *(WORD *)(p + 4) = 0xF2C0;
    *(WORD *)(p + 6) = (UINT16)(reg << 8);
    PutThumb2Mov32((UINT16 *)p, imm);
}

#ifndef DACCESS_COMPILE


#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStartRX = (BYTE *)(void*)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    ExecutableWriterHolder<BYTE> startWriterHolder(pStartRX, cbAligned); \
    BYTE * pStart = startWriterHolder.GetRW(); \
    size_t rxOffset = pStartRX - pStart; \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) { *(WORD *)p = 0xdefe; p += 2; } \
    ClrFlushInstructionCache(pStartRX, cbAligned); \
    return (PCODE)((TADDR)pStartRX | THUMB_CODE)

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(18);

    // mov r0, arg
    MovRegImm(p, 0, arg);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    // mov r1, arg
    MovRegImm(p, 1, arg);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(18);

    EmitHelperWithArg(p, rxOffset, pAllocator, arg, target);

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(26);

    // mov r0, arg
    MovRegImm(p, 0, arg);
    p += 8;

    // mov r1, arg2
    MovRegImm(p, 1, arg2);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(20);

    // mov r1, r0
    *(WORD *)p = 0x4601;
    p += 2;

    // mov r0, arg
    MovRegImm(p, 0, arg);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    BEGIN_DYNAMIC_HELPER_EMIT(2);

    *(WORD *)p = 0x4770; // bx lr
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    BEGIN_DYNAMIC_HELPER_EMIT(10);

    // mov r0, arg
    MovRegImm(p, 0, arg);
    p += 8;

    // bx lr
    *(WORD *)p = 0x4770;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    BEGIN_DYNAMIC_HELPER_EMIT((offset != 0) ? 16 : 12);

    // mov r0, arg
    MovRegImm(p, 0, arg);
    p += 8;

    // ldr r0, [r0]
    *(WORD *)p = 0x6800;
    p += 2;

    if (offset != 0)
    {
        // add r0, r0, <offset>
        *(WORD *)(p + 0) = 0xF100;
        *(WORD *)(p + 2) = offset;
        p += 4;
    }

    // bx lr
    *(WORD *)p = 0x4770;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(18);

    // mov r2, arg
    MovRegImm(p, 2, arg);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(26);

    // mov r2, arg
    MovRegImm(p, 2, arg);
    p += 8;

    // mov r3, arg
    MovRegImm(p, 3, arg2);
    p += 8;

    // mov r12, target
    MovRegImm(p, 12, target);
    p += 8;

    // bx r12
    *(WORD *)p = 0x4760;
    p += 2;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(!MethodTable::IsPerInstInfoRelative());

    PCODE helperAddress = (pLookup->helper == CORINFO_HELP_RUNTIMEHANDLE_METHOD ?
        GetEEFuncEntryPoint(JIT_GenericHandleMethodWithSlotAndModule) :
        GetEEFuncEntryPoint(JIT_GenericHandleClassWithSlotAndModule));

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    ExecutableWriterHolder<GenericHandleArgs> argsWriterHolder(pArgs, sizeof(GenericHandleArgs));
    argsWriterHolder.GetRW()->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    argsWriterHolder.GetRW()->signature = pLookup->signature;
    argsWriterHolder.GetRW()->module = (CORINFO_MODULE_HANDLE)pModule;

    WORD slotOffset = (WORD)(dictionaryIndexAndSlot & 0xFFFF) * sizeof(Dictionary*);

    // It's available only via the run-time helper function,

    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(18);

        EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int indirectionsSize = 0;
        if (pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
        {
            indirectionsSize += (pLookup->sizeOffset >= 0xFFF ? 10 : 4);
            indirectionsSize += 12;
        }
        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            indirectionsSize += (pLookup->offsets[i] >= 0xFFF ? 10 : 4);
        }

        int codeSize = indirectionsSize + (pLookup->testForNull ? 26 : 2);

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        if (pLookup->testForNull)
        {
            // mov r3, r0
            *(WORD *)p = 0x4603;
            p += 2;
        }

        BYTE* pBLECall = NULL;

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                _ASSERTE(pLookup->testForNull && i > 0);

                if (pLookup->sizeOffset >= 0xFFF)
                {
                    // mov r2, offset
                    MovRegImm(p, 2, pLookup->sizeOffset); p += 8;
                    // ldr r1, [r0, r2]
                    *(WORD*)p = 0x5881; p += 2;
                }
                else
                {
                    // ldr r1, [r0 + offset]
                    *(WORD*)p = 0xF8D0; p += 2;
                    *(WORD*)p = (WORD)(0xFFF & pLookup->sizeOffset) | 0x1000; p += 2;
                }

                // mov r2, slotOffset
                MovRegImm(p, 2, slotOffset); p += 8;

                // cmp r1,r2
                *(WORD*)p = 0x4291; p += 2;

                // ble 'CALL HELPER'
                pBLECall = p;       // Offset filled later
                *(WORD*)p = 0xdd00; p += 2;
            }
            if (pLookup->offsets[i] >= 0xFFF)
            {
                // mov r2, offset
                MovRegImm(p, 2, pLookup->offsets[i]);
                p += 8;

                // ldr r0, [r0, r2]
                *(WORD *)p = 0x5880;
                p += 2;
            }
            else
            {
                // ldr r0, [r0 + offset]
                *(WORD *)p = 0xF8D0;
                p += 2;
                *(WORD *)p = (WORD)(0xFFF & pLookup->offsets[i]);
                p += 2;
            }
        }

        // No null test required
        if (!pLookup->testForNull)
        {
            _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);

            // mov pc, lr
            *(WORD *)p = 0x46F7;
            p += 2;
        }
        else
        {
            // cbz r0, 'CALL HELPER'
            *(WORD *)p = 0xB100;
            p += 2;
            // mov pc, lr
            *(WORD *)p = 0x46F7;
            p += 2;

            // CALL HELPER:
            if (pBLECall != NULL)
                *(WORD*)pBLECall |= (((BYTE)(p - pBLECall) - 4) >> 1);

            // mov r0, r3
            *(WORD *)p = 0x4618;
            p += 2;

            EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}
#endif // FEATURE_READYTORUN


#endif // !DACCESS_COMPILE
