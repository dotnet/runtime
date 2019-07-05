// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            _ASSERTE(refsize == InstructionFormat::k16);

            if(fixedUpReference <0 || fixedUpReference > 126)
                COMPlusThrow(kNotSupportedException); 

            _ASSERTE((fixedUpReference & 0x1) == 0);

            pOutBuffer[0] = static_cast<BYTE>(((0x3e & fixedUpReference) << 2) | (0x7 & variationCode));
            pOutBuffer[1] = static_cast<BYTE>(0xb1 | (0x8 & variationCode)| ((0x40 & fixedUpReference)>>5));
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

        virtual VOID EmitInstruction(UINT refsize, __int64 fixedUpReference, BYTE *pOutBuffer, UINT cond, BYTE *pDataBuffer)
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
                    pOutBuffer[0] = static_cast<BYTE>((fixedUpReference & 0x1fe)>>1);
                    pOutBuffer[1] = static_cast<BYTE>(0xe0 | ((fixedUpReference & 0xe00)>>9));
                }
                else if(fixedUpReference >= -16777216 && fixedUpReference <= 16777214)
                {
                    if(refsize != InstructionFormat::k32)
                        _ASSERTE(!"Expected refSize to be 4");

                    //Emit T4 encoding of B<c> <label> instruction
                    int s = (fixedUpReference & 0x1000000) >> 24;
                    int i1 = (fixedUpReference & 0x800000) >> 23; 
                    int i2 = (fixedUpReference & 0x400000) >> 22; 
                    pOutBuffer[0] = static_cast<BYTE>((fixedUpReference & 0xff000) >> 12);
                    pOutBuffer[1] = static_cast<BYTE>(0xf0 | (s << 2) |( (fixedUpReference & 0x300000) >>20));
                    pOutBuffer[2] = static_cast<BYTE>((fixedUpReference & 0x1fe) >> 1);
                    pOutBuffer[3] = static_cast<BYTE>(0x90 | (~(i1^s)) << 5 | (~(i2^s)) << 3 | (fixedUpReference & 0xe00) >> 9);
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
                    pOutBuffer[0] = static_cast<BYTE>((fixedUpReference & 0x1fe)>>1);
                    pOutBuffer[1] = static_cast<BYTE>(0xd0 | (cond & 0xf));
                }
                else if(fixedUpReference >= -1048576 && fixedUpReference <= 1048574)
                {
                    if(refsize != InstructionFormat::k32)
                        _ASSERTE(!"Expected refSize to be 4");

                    //Emit T3 encoding of B<c> <label> instruction
                    pOutBuffer[0] = static_cast<BYTE>(((cond & 0x3) << 6) | ((fixedUpReference & 0x3f000) >>12));
                    pOutBuffer[1] = static_cast<BYTE>(0xf0 | ((fixedUpReference & 0x100000) >>18) | ((cond & 0xc) >> 2));
                    pOutBuffer[2] = static_cast<BYTE>((fixedUpReference & 0x1fe) >> 1);
                    pOutBuffer[3] = static_cast<BYTE>(0x80 | ((fixedUpReference & 0x40000) >> 13) | ((fixedUpReference & 0x80000) >> 16) | ((fixedUpReference & 0xe00) >> 9));
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

#ifndef CROSSGEN_COMPILE

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
    BYTE *  m_pFuncStart;                   // Pointer to the start of the barrier function
    BYTE *  m_pFuncEnd;                     // Pointer to the end of the barrier function
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
    *pcbLength = size;
}

void CopyWriteBarrier(PCODE dstCode, PCODE srcCode, PCODE endCode)
{
    TADDR dst = PCODEToPINSTR(dstCode);
    TADDR src = PCODEToPINSTR(srcCode);
    TADDR end = PCODEToPINSTR(endCode);

    size_t size = (PBYTE)end - (PBYTE)src;
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
        PutThumb2Mov32((UINT16*)(to + pDesc->m_dw_##_global##_offset - 1), (UINT32)(dac_cast<TADDR>(_global)));

    // Iterate through the write barrier patch table created in the .clrwb section 
    // (see write barrier asm code)
    WriteBarrierDescriptor * pDesc = &g_rgWriteBarrierDescriptors;
    while (pDesc->m_pFuncStart)
    {
        // If the write barrier is being currently used (as in copied over to the patchable site)
        // then read the patch location from the table and use the offset to patch the target asm code
        PBYTE to = FindWBMapping(pDesc->m_pFuncStart);
        if(to) 
        {
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

#endif // CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundstate,
                                    DWORD threadId,
                                    int funCallDepth,
                                    HostCallPreference hostCallPreference)
{
    T_CONTEXT                         ctx;
    T_KNONVOLATILE_CONTEXT_POINTERS   nonVolRegPtrs;

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
#ifndef FEATURE_PAL
        pvControlPc = Thread::VirtualUnwindCallFrame(&ctx, &nonVolRegPtrs);
#else // !FEATURE_PAL
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
#endif // !FEATURE_PAL
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
#endif // !CROSSGEN_COMPILE

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

void StubPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
void StubPrecode::Fixup(DataImage *image)
{
    WRAPPER_NO_CONTRACT;

    image->FixupFieldToNode(this, offsetof(StubPrecode, m_pTarget),
                            image->GetHelperThunk(CORINFO_HELP_EE_PRESTUB),
                            0,
                            IMAGE_REL_BASED_PTR);

    image->FixupField(this, offsetof(StubPrecode, m_pMethodDesc),
                      (void*)GetMethodDesc(),
                      0,
                      IMAGE_REL_BASED_PTR);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

void NDirectImportPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
void NDirectImportPrecode::Fixup(DataImage *image)
{
    WRAPPER_NO_CONTRACT;

    image->FixupField(this, offsetof(NDirectImportPrecode, m_pMethodDesc),
                      (void*)GetMethodDesc(),
                      0,
                      IMAGE_REL_BASED_PTR);

    image->FixupFieldToNode(this, offsetof(NDirectImportPrecode, m_pTarget),
                            image->GetHelperThunk(CORINFO_HELP_EE_PINVOKE_FIXUP),
                            0,
                            IMAGE_REL_BASED_PTR);
}
#endif

void FixupPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator, int iMethodDescChunkIndex /*=0*/, int iPrecodeChunkIndex /*=0*/)
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Partial initialization. Used to save regrouped chunks.
void FixupPrecode::InitForSave(int iPrecodeChunkIndex)
{
    STANDARD_VM_CONTRACT;

    m_rgCode[0] = 0x46fc;   // mov r12, pc
    m_rgCode[1] = 0xf8df;   // ldr pc, [pc, #4]
    m_rgCode[2] = 0xf004;

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

    image->FixupFieldToNode(this, offsetof(FixupPrecode, m_pTarget), pHelperThunk);

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


#ifndef CROSSGEN_COMPILE
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

void  LookupHolder::Initialize(PCODE resolveWorkerTarget, size_t dispatchToken)
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

void  DispatchHolder::Initialize(PCODE implTarget, PCODE failTarget, size_t expectedMT)
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
#define PC_REL_OFFSET(_field) (WORD)(offsetof(DispatchStub, _field) - (offsetof(DispatchStub, _entryPoint[n + 2]) & 0xfffffffc))

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

void ResolveHolder::Initialize(PCODE resolveWorkerTarget, PCODE patcherTarget,
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
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _resolveEntryPoint[n + 2]) & 0xfffffffc))

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
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _slowEntryPoint[n + 2]) & 0xfffffffc))

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
#define PC_REL_OFFSET(_field) (WORD)(offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _failEntryPoint[n + 2]) & 0xfffffffc))

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
    offset = (WORD)(offsetof(ResolveStub, _resolveEntryPoint) - offsetof(ResolveStub, _failEntryPoint[n + 2]));
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

#ifdef FEATURE_PAL
    // Erect frame to perform call to GetThread
    psl->ThumbEmitProlog(1, sizeof(ArgumentRegisters), FALSE); // Save r4 for aligned stack

    // Save argument registers around the GetThread call. Don't bother with using ldm/stm since this inefficient path anyway.
    for (int reg = 0; reg < 4; reg++)
        psl->ThumbEmitStoreRegIndirect(ThumbReg(reg), thumbRegSp, offsetof(ArgumentRegisters, r[reg]));
#endif

    psl->ThumbEmitGetThread(regThread);

#ifdef FEATURE_PAL
    for (int reg = 0; reg < 4; reg++)
        psl->ThumbEmitLoadRegIndirect(ThumbReg(reg), thumbRegSp, offsetof(ArgumentRegisters, r[reg]));
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

    // str FP, [regFrame + FrameInfo.offsetOfCalleeSavedEbp]
    psl->ThumbEmitStoreRegIndirect(thumbRegFp, regFrame, FrameInfo.offsetOfCalleeSavedFP - negSpace);

    // mov [regFrame + FrameInfo.offsetOfReturnAddress], 0
    psl->ThumbEmitMovConstant(regScratch, 0);
    psl->ThumbEmitStoreRegIndirect(regScratch, regFrame, FrameInfo.offsetOfReturnAddress - negSpace);

#ifdef FEATURE_PAL
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

#ifdef FEATURE_PAL
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
#ifdef FEATURE_PAL

    ThumbEmitMovConstant(ThumbReg(0), (TADDR)GetThread);

    ThumbEmitCallRegister(ThumbReg(0));

    if (dest != ThumbReg(0))
    {
        ThumbEmitMovRegReg(dest, ThumbReg(0));
    }

#else // FEATURE_PAL

    // mrc p15, 0, dest, c13, c0, 2
    Emit16(0xee1d);
    Emit16((WORD)(0x0f50 | (dest << 12)));

    ThumbEmitLoadRegIndirect(dest, dest, offsetof(TEB, ThreadLocalStoragePointer));

    ThumbEmitLoadRegIndirect(dest, dest, sizeof(void *) * (g_TlsIndex & 0xFFFF));

    ThumbEmitLoadRegIndirect(dest, dest, (g_TlsIndex & 0x7FFF0000) >> 16);

#endif // FEATURE_PAL
}
#endif // CROSSGEN_COMPILE


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

#ifndef CROSSGEN_COMPILE

void StubLinkerCPU::ThumbEmitCallManagedMethod(MethodDesc *pMD, bool fTailcall)
{
    bool isRelative = MethodTable::VTableIndir2_t::isRelative
                      && pMD->IsVtableSlot();

#ifndef FEATURE_NGEN_RELOCS_OPTIMIZATIONS
    _ASSERTE(!isRelative);
#endif

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
            if (!fTailcall)
            {
                // str r4, [sp, 0]
                ThumbEmitStoreRegIndirect(ThumbReg(4), thumbRegSp, 0);
            }

            // mov r4, r12
            ThumbEmitMovRegReg(ThumbReg(4), ThumbReg(12));
        }

        // ldr r12, [r12]
        ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(12), 0);

        if (isRelative)
        {
            // add r12, r4
            ThumbEmitAddReg(ThumbReg(12), ThumbReg(4));

            if (!fTailcall)
            {
                // ldr r4, [sp, 0]
                ThumbEmitLoadRegIndirect(ThumbReg(4), thumbRegSp, 0);
            }
        }
    }

    if (fTailcall)
    {
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
    else
    {
        // blx r12
        ThumbEmitCallRegister(ThumbReg(12));
    }
}

// Common code used to generate either an instantiating method stub or an unboxing stub (in the case where the
// unboxing stub also needs to provide a generic instantiation parameter). The stub needs to add the
// instantiation parameter provided in pHiddenArg and re-arrange the rest of the incoming arguments as a
// result (since on ARM this hidden parameter is inserted before explicit user arguments we need a type of
// shuffle thunk in the reverse direction of the type used for static delegates). If pHiddenArg == NULL it
// indicates that we're in the unboxing case and should add sizeof(MethodTable*) to the incoming this pointer
// before dispatching to the target. In this case the instantiating parameter is always the non-shared
// MethodTable pointer we can deduce directly from the incoming 'this' reference.
void StubLinkerCPU::ThumbEmitCallWithGenericInstantiationParameter(MethodDesc *pMD, void *pHiddenArg)
{
    // There is a simple case and a complex case.
    //   1) In the simple case the addition of the hidden arg doesn't push any user args onto the stack. In
    //      this case we only have to re-arrange/initialize some argument registers and tail call to the
    //      target.
    //   2) In the complex case we have to modify the stack by pushing some of the register based user
    //      arguments. We can't tail call in this case because we've altered the size of the stack and our
    //      caller doesn't expect this and can't compensate. Instead we'll need to create a stack frame
    //      (including an explicit Frame to make it crawlable to the runtime) and copy the incoming arguments
    //      over.
    //
    // First we need to analyze the signature of the target method both with and without the extra
    // instantiation argument. We use ArgIterator to determine the difference in location
    // (register or stack offset) for each argument between the two cases. This forms a set instructions that
    // tell us how to copy incoming arguments into outgoing arguments (and if those instructions don't include
    // any writes to stack locations in the outgoing case then we know we can generate a simple thunk).

    SigTypeContext sTypeContext(pMD, TypeHandle());

    // Incoming, source, method signature.
    MetaSig sSrcSig(pMD->GetSignature(),
                    pMD->GetModule(),
                    &sTypeContext,
                    MetaSig::sigMember);

    // Outgoing, destination, method signature.
    MetaSig sDstSig(pMD->GetSignature(),
                    pMD->GetModule(),
                    &sTypeContext,
                    MetaSig::sigMember);      

    sDstSig.SetHasParamTypeArg();

    // Wrap calling convention parsers round the source and destination signatures. These will be responsible
    // for determining where each argument lives in registers or on the stack.
    ArgIterator sSrcArgLocations(&sSrcSig);
    ArgIterator sDstArgLocations(&sDstSig);

    // Define an argument descriptor type that describes how a single 4 byte portion of an argument is mapped
    // in the source and destination signature. We only have to worry about general registers and stack
    // locations here; floating point argument registers are left unmodified by this thunk.
    struct ArgDesc
    {
        int     m_idxSrc;       // Source register or stack offset
        int     m_idxDst;       // Destination register or stack offset
        bool    m_fSrcIsReg;    // Source index is a register number
        bool    m_fDstIsReg;    // Destination index is a register number
    };

    // The number of argument move descriptors we'll need is a function of the number of 4-byte registers or
    // stack slots the arguments occupy. The following calculation will over-estimate in a few side cases, but
    // not by much (it assumes all four argument registers are used plus the number of stack slots that
    // MetaSig calculates are needed for the rest of the arguments).
    DWORD cArgDescriptors = 4 + (sSrcArgLocations.SizeOfArgStack() / 4);

    // Allocate the array of argument descriptors.
    CQuickArray<ArgDesc> rgArgDescs;
    rgArgDescs.AllocThrows(cArgDescriptors);

    // We only need to map translations for arguments that could come after the instantiation parameter we're
    // inserting. On the ARM the only implicit argument that could follow is a vararg signature cookie, but
    // it's disallowed in this case. So we simply walk the user arguments.
    _ASSERTE(!sSrcSig.IsVarArg());

    INT srcOffset;
    INT dstOffset;

    DWORD idxCurrentDesc = 0;
    while ((srcOffset = sSrcArgLocations.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        dstOffset = sDstArgLocations.GetNextOffset();

        // Get the placement for a single argument in the source and destination signatures (may include
        // multiple registers and/or stack locations if the argument is larger than 4 bytes).
        ArgLocDesc sSrcArgLoc;
        sSrcArgLocations.GetArgLoc(srcOffset, &sSrcArgLoc);
        ArgLocDesc sDstArgLoc;
        sDstArgLocations.GetArgLoc(dstOffset, &sDstArgLoc);

        // Fill in as many single-slot descriptors as the argument needs. Note that we ignore any floating
        // point register cases (m_cFloatReg > 0) since these will never change due to the hidden arg
        // insertion.
        while (sSrcArgLoc.m_cGenReg || sSrcArgLoc.m_cStack)
        {
            _ASSERTE(idxCurrentDesc < cArgDescriptors);

            if (sSrcArgLoc.m_cGenReg)
            {
                sSrcArgLoc.m_cGenReg--;
                rgArgDescs[idxCurrentDesc].m_idxSrc = sSrcArgLoc.m_idxGenReg++;
                rgArgDescs[idxCurrentDesc].m_fSrcIsReg = true;
            }
            else
            {
                _ASSERTE(sSrcArgLoc.m_cStack > 0);
                sSrcArgLoc.m_cStack--;
                rgArgDescs[idxCurrentDesc].m_idxSrc = sSrcArgLoc.m_idxStack++;
                rgArgDescs[idxCurrentDesc].m_fSrcIsReg = false;
            }

            if (sDstArgLoc.m_cGenReg)
            {
                sDstArgLoc.m_cGenReg--;
                rgArgDescs[idxCurrentDesc].m_idxDst = sDstArgLoc.m_idxGenReg++;
                rgArgDescs[idxCurrentDesc].m_fDstIsReg = true;
            }
            else
            {
                _ASSERTE(sDstArgLoc.m_cStack > 0);
                sDstArgLoc.m_cStack--;
                rgArgDescs[idxCurrentDesc].m_idxDst = sDstArgLoc.m_idxStack++;
                rgArgDescs[idxCurrentDesc].m_fDstIsReg = false;
            }

            idxCurrentDesc++;
        }
    }

    bool isRelative = MethodTable::VTableIndir2_t::isRelative
                      && pMD->IsVtableSlot();

#ifndef FEATURE_NGEN_RELOCS_OPTIMIZATIONS
    _ASSERTE(!isRelative);
#endif

    // Update descriptor count to the actual number used.
    cArgDescriptors = idxCurrentDesc;

    // Note the position at which we have the first move to a stack location
    DWORD idxFirstMoveToStack = -1;

    // We have a problem where register to register moves are concerned. Since we're adding an argument the
    // moves will be from a lower numbered register to a higher numbered one (e.g. r0 -> r1). But the argument
    // descriptors we just produced will order them starting from the lowest registers. If we emit move
    // instructions in this order we'll end up copying the value of the lowest register into all of the rest
    // (e.g. r0 -> r1, r1 -> r2 etc.). We don't have this problem with stack based arguments since the
    // argument stacks don't overlap in the same fashion. To solve this we'll reverse the order of the
    // descriptors with register destinations (there will be at most four of these so it's fairly cheap).
    if (cArgDescriptors > 1)
    {
        // Start by assuming we have all four register destination descriptors.
        int idxLastRegDesc = min(3, cArgDescriptors - 1);

        // Adjust that count to match reality.
        while (idxLastRegDesc >= 0 && !rgArgDescs[idxLastRegDesc].m_fDstIsReg)
        {
            idxLastRegDesc--;
        }
        
        if (idxLastRegDesc < 0)
        {
            // No register is used to pass any of the parameters. No need to reverse the order of the descriptors
            idxFirstMoveToStack = 0;
        }
        else
        {
            _ASSERTE(idxLastRegDesc >= 0 && ((DWORD)idxLastRegDesc) < cArgDescriptors);
            
            // First move to stack location happens after the last move to register location
            idxFirstMoveToStack = idxLastRegDesc+1;

            // Calculate how many descriptors we'll need to swap.
            DWORD cSwaps = (idxLastRegDesc + 1) / 2;

            // Finally we can swap the descriptors.
            int idxFirstRegDesc = 0;
            while (cSwaps)
            {
                ArgDesc sTempDesc = rgArgDescs[idxLastRegDesc];
                rgArgDescs[idxLastRegDesc] = rgArgDescs[idxFirstRegDesc];
                rgArgDescs[idxFirstRegDesc] = sTempDesc;

                _ASSERTE(idxFirstRegDesc < idxLastRegDesc);
                idxFirstRegDesc++;
                idxLastRegDesc--;
                cSwaps--;
            }
        }
    }

    // If we're ever required to write to the destination stack then we can't implement this case with a
    // simple tail call stub. (That's not technically true: there are edge cases caused by 64-bit alignment
    // requirements that might allow us to use a simple stub since the extra argument fits in a "hole" in the
    // arguments, but these are infrequent enough that it's likely not worth the effort of detecting them).
    ArgDesc *pLastArg = cArgDescriptors ? &rgArgDescs[cArgDescriptors - 1] : NULL;
    if ((pLastArg == NULL) || pLastArg->m_fDstIsReg)
    {
        // Simple case where we can just rearrange a few argument registers and tail call.

        for (idxCurrentDesc = 0; idxCurrentDesc < cArgDescriptors; idxCurrentDesc++)
        {
            // Because we're in the simple case we know we'll never be asked to move a value onto the stack
            // and since we're adding a parameter we should never be required to move a value from the stack
            // to a register either. So all of the descriptors should be register to register moves.
            _ASSERTE(rgArgDescs[idxCurrentDesc].m_fSrcIsReg && rgArgDescs[idxCurrentDesc].m_fDstIsReg);
            ThumbEmitMovRegReg(ThumbReg(rgArgDescs[idxCurrentDesc].m_idxDst),
                               ThumbReg(rgArgDescs[idxCurrentDesc].m_idxSrc));
        }

        // Place instantiation parameter into the correct register.
        ArgLocDesc sInstArgLoc;
        sDstArgLocations.GetParamTypeLoc(&sInstArgLoc);
        int regHidden = sInstArgLoc.m_idxGenReg;
        _ASSERTE(regHidden != -1);
        if (pHiddenArg)
        {
            // mov regHidden, #pHiddenArg
            ThumbEmitMovConstant(ThumbReg(regHidden), (TADDR)pHiddenArg);
        }
        else
        {
            // Extract MethodTable pointer (the hidden arg) from the object instance.
            //  ldr regHidden, [r0]
            ThumbEmitLoadRegIndirect(ThumbReg(regHidden), ThumbReg(0), 0);
        }

        if (pHiddenArg == NULL)
        {
            // Unboxing stub case.

            // Skip over the MethodTable* to find the address of the unboxed value type.
            //  add r0, #sizeof(MethodTable*)
            ThumbEmitIncrement(ThumbReg(0), sizeof(MethodTable*));
        }

        // Emit a tail call to the target method.
        if (isRelative)
        {
            ThumbEmitProlog(1, 0, FALSE);
        }

        ThumbEmitCallManagedMethod(pMD, true);

        if (isRelative)
        {
            ThumbEmitEpilog();
        }
    }
    else
    {
        // Complex case where we need to emit a new stack frame and copy the arguments.

        // Calculate the size of the new stack frame:
        //
        //            +------------+
        //      SP -> |            | <-- Space for helper arg, if isRelative is true
        //            +------------+
        //            |            | <-+
        //            :            :   | Outgoing arguments
        //            |            | <-+
        //            +------------+
        //            | Padding    | <-- Optional, maybe required so that SP is 64-bit aligned
        //            +------------+
        //            | GS Cookie  |
        //            +------------+
        //        +-> | vtable ptr |
        //        |   +------------+
        //        |   | m_Next     |
        //        |   +------------+
        //        |   | R4         | <-+
        //   Stub |   +------------+   |
        // Helper |   :            :   |
        //  Frame |   +------------+   | Callee saved registers
        //        |   | R11        |   |
        //        |   +------------+   |
        //        |   | LR/RetAddr | <-+
        //        |   +------------+
        //        |   | R0         | <-+
        //        |   +------------+   |
        //        |   :            :   | Argument registers
        //        |   +------------+   |
        //        +-> | R3         | <-+
        //            +------------+
        //  Old SP -> |            |
        //
        DWORD cbStackArgs = (pLastArg->m_idxDst + 1) * 4;
        DWORD cbStackFrame = cbStackArgs + sizeof(GSCookie) + sizeof(StubHelperFrame);
        cbStackFrame = ALIGN_UP(cbStackFrame, 8);

        if (isRelative)
        {
            cbStackFrame += 4;
        }

        DWORD cbStackFrameWithoutSavedRegs = cbStackFrame - (13 * 4); // r0-r11,lr

        // Prolog:
        ThumbEmitProlog(8,                          // Save r4-r11,lr (count doesn't include lr)
                        cbStackFrameWithoutSavedRegs, // Additional space in the stack frame required
                        TRUE);                      // Push argument registers

        DWORD offsetOfFrame = cbStackFrame - sizeof(StubHelperFrame);

        // Initialize and link the StubHelperFrame and associated GS cookie.
        EmitStubLinkFrame(StubHelperFrame::GetMethodFrameVPtr(), offsetOfFrame, StubHelperFrame::GetOffsetOfTransitionBlock());

        // Initialize temporary registers used when copying arguments:
        //  r6 == pointer to first incoming stack-based argument
        //  r7 == pointer to first outgoing stack-based argument

        // add r6, sp, #cbStackFrame
        ThumbEmitAdd(ThumbReg(6), thumbRegSp, cbStackFrame);

        // mov r7, sp
        ThumbEmitMovRegReg(ThumbReg(7), thumbRegSp);

        // Copy incoming to outgoing arguments. Stack arguments are generally written consecutively and as
        // such we use post-increment forms of register indirect addressing to keep our input (r6) and output
        // (r7) pointers up to date. But sometimes we'll skip four bytes due to 64-bit alignment requirements
        // and need to bump one or both of the pointers to compensate. We determine
        //
        // At this point, the ArgumentDescriptor array is divied into two parts:
        //
        // 1) Reverse sorted register to register moves (see the comment earlier in the method for details)
        // 2) Register or Stack to Stack moves (if any) in the original order.
        //
        // Its possible that the register to register moves may move to a target register that happens
        // to be a source for the register -> stack move. If this happens, and we emit the argument moves
        // in the current order, then we can lose the contents of the register involved in register->stack
        // move (stack->stack moves are not a problem as the locations dont overlap).
        //
        // To address this, we will emit the argument moves in two loops:
        //
        // 1) First loop will emit the moves that have stack location as the target
        // 2) Second loop will emit moves that have register as the target.
        DWORD idxCurrentLoopBegin = 0, idxCurrentLoopEnd = cArgDescriptors;
        if (idxFirstMoveToStack != -1)
        {
            _ASSERTE(idxFirstMoveToStack < cArgDescriptors);
            idxCurrentLoopBegin = idxFirstMoveToStack;
        
            for (idxCurrentDesc = idxCurrentLoopBegin; idxCurrentDesc < idxCurrentLoopEnd; idxCurrentDesc++)
            {
                ArgDesc *pArgDesc = &rgArgDescs[idxCurrentDesc];

                if (pArgDesc->m_fSrcIsReg)
                {
                    // Source value is in a register.

                    _ASSERTE(!pArgDesc->m_fDstIsReg);
                    // Register to stack. Calculate delta from last stack write; normally it will be 4 bytes
                    // and our pointer has already been set up correctly by the post increment of the last
                    // write. But in some cases we need to skip four bytes due to a 64-bit alignment
                    // requirement. In those cases we need to emit an extra add to keep the pointer correct.
                    // Note that the first stack argument is guaranteed to be 64-bit aligned by the ABI and as
                    // such the first stack slot is never skipped.
                    if ((pArgDesc->m_idxDst > 0) &&
                        (pArgDesc->m_idxDst != (rgArgDescs[idxCurrentDesc - 1].m_idxDst + 1)))
                    {
                        _ASSERTE(pArgDesc->m_idxDst == (rgArgDescs[idxCurrentDesc - 1].m_idxDst + 2));
                        ThumbEmitIncrement(ThumbReg(7), 4);
                    }

                    // str srcReg, [r7], #4
                    ThumbEmitStoreIndirectPostIncrement(pArgDesc->m_idxSrc, ThumbReg(7), 4);
                }
                else
                {
                    // Source value is on the stack. We should have no cases where a stack argument moves back to
                    // a register (because we're adding an argument).
                    _ASSERTE(!pArgDesc->m_fDstIsReg);

                    // Stack to stack move. We need to use register (r6) to store the value temporarily between
                    // the read and the write. See the comments above for why we need to check stack deltas and
                    // possibly insert extra add instructions in some cases.
                    if ((pArgDesc->m_idxSrc > 0) &&
                        (pArgDesc->m_idxSrc != (rgArgDescs[idxCurrentDesc - 1].m_idxSrc + 1)))
                    {
                        _ASSERTE(pArgDesc->m_idxSrc == (rgArgDescs[idxCurrentDesc - 1].m_idxSrc + 2));
                        ThumbEmitIncrement(ThumbReg(6), 4);
                    }
                    if ((pArgDesc->m_idxDst > 0) &&
                        (pArgDesc->m_idxDst != (rgArgDescs[idxCurrentDesc - 1].m_idxDst + 1)))
                    {
                        _ASSERTE(pArgDesc->m_idxDst == (rgArgDescs[idxCurrentDesc - 1].m_idxDst + 2));
                        ThumbEmitIncrement(ThumbReg(7), 4);
                    }

                    // ldr r8, [r6], #4
                    ThumbEmitLoadIndirectPostIncrement(ThumbReg(8), ThumbReg(6), 4);

                    // str r8, [r7], #4
                    ThumbEmitStoreIndirectPostIncrement(ThumbReg(8), ThumbReg(7), 4);
                }
            }

            // Update the indexes to be used for the second loop
            idxCurrentLoopEnd = idxCurrentLoopBegin;
            idxCurrentLoopBegin = 0;
        }

        // Now, perform the register to register moves
        for (idxCurrentDesc = idxCurrentLoopBegin; idxCurrentDesc < idxCurrentLoopEnd; idxCurrentDesc++)
        {
            ArgDesc *pArgDesc = &rgArgDescs[idxCurrentDesc];

            // All moves to stack locations have been done (if applicable).
            // Since we are moving to a register destination, the source
            // will also be a register and cannot be a stack location (refer to the previous loop).
            _ASSERTE(pArgDesc->m_fSrcIsReg && pArgDesc->m_fDstIsReg);

            // Register to register case.
            ThumbEmitMovRegReg(pArgDesc->m_idxDst, pArgDesc->m_idxSrc);
        }


        // Place instantiation parameter into the correct register.
        ArgLocDesc sInstArgLoc;
        sDstArgLocations.GetParamTypeLoc(&sInstArgLoc);
        int regHidden = sInstArgLoc.m_idxGenReg;
        _ASSERTE(regHidden != -1);
        if (pHiddenArg)
        {
            // mov regHidden, #pHiddenArg
            ThumbEmitMovConstant(ThumbReg(regHidden), (TADDR)pHiddenArg);
        }
        else
        {
            // Extract MethodTable pointer (the hidden arg) from the object instance.
            //  ldr regHidden, [r0]
            ThumbEmitLoadRegIndirect(ThumbReg(regHidden), ThumbReg(0), 0);
        }

        if (pHiddenArg == NULL)
        {
            // Unboxing stub case.

            // Skip over the MethodTable* to find the address of the unboxed value type.
            //  add r0, #sizeof(MethodTable*)
            ThumbEmitIncrement(ThumbReg(0), sizeof(MethodTable*));
        }

        // Emit a regular (non-tail) call to the target method.
        ThumbEmitCallManagedMethod(pMD, false);

        // Unlink the StubHelperFrame.
        EmitStubUnlinkFrame();

        // Epilog
        ThumbEmitEpilog();
    }
}

#if defined(FEATURE_SHARE_GENERIC_CODE)
// The stub generated by this method passes an extra dictionary argument before jumping to
// shared-instantiation generic code.
//
// pSharedMD is either
//    * An InstantiatedMethodDesc for a generic method whose code is shared across instantiations.
//      In this case, the extra argument is the InstantiatedMethodDesc for the instantiation-specific stub itself.
// or * A MethodDesc for a static method in a generic class whose code is shared across instantiations.
//      In this case, the extra argument is the MethodTable pointer of the instantiated type.
VOID StubLinkerCPU::EmitInstantiatingMethodStub(MethodDesc* pSharedMD, void* extra)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(pSharedMD->RequiresInstMethodTableArg() || pSharedMD->RequiresInstMethodDescArg());
    }
    CONTRACTL_END;

    // Share code with the instantiating version of the unboxing stub (see below).
    ThumbEmitCallWithGenericInstantiationParameter(pSharedMD, extra);
}
#endif // FEATURE_SHARE_GENERIC_CODE

void StubLinkerCPU::EmitUnboxMethodStub(MethodDesc *pMD)
{
    if (pMD->RequiresInstMethodTableArg())
    {
        // In this case we also have to add an instantiating parameter (which is always the MethodTable* from
        // the instance we're called on). Most of this code is shared with the instantiating method stub
        // above, the NULL parameter informs the emitter that we're both an unboxing stub and that the extra
        // parameter can be deduced from the 'this' reference.
        ThumbEmitCallWithGenericInstantiationParameter(pMD, NULL);
    }
    else
    {
        // We assume that we'll never see a case where a boxed value type method will require an instantiated
        // method desc as a parameter. The stubs on other platforms make this assumption (and indeed this
        // method isn't even passed an additional instantiation parameter). This is trivially true for the
        // non-interface call case: the only methods callable directly on the boxed instance are the methods
        // of Object, none of which are generic. For the interface dispatch case we're relying on the fact
        // that the jit always provides the instantiating argument explicitly.
        _ASSERTE(!pMD->RequiresInstMethodDescArg());

        // Address of the value type is address of the boxed instance plus four.
        //  add r0, #4
        ThumbEmitIncrement(ThumbReg(0), 4);

        bool isRelative = MethodTable::VTableIndir2_t::isRelative
                          && pMD->IsVtableSlot();

#ifndef FEATURE_NGEN_RELOCS_OPTIMIZATIONS
        _ASSERTE(!isRelative);
#endif

        if (isRelative)
        {
            ThumbEmitProlog(1, 0, FALSE);
        }

        // Tail call the real target.
        ThumbEmitCallManagedMethod(pMD, true /* tail call */);

        if (isRelative)
        {
            ThumbEmitEpilog();
        }
    }
}

#endif // CROSSGEN_COMPILE

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

#ifndef CROSSGEN_COMPILE
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

void TailCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD) 
{ 
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    // Next, copy all the callee saved registers
    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &m_calleeSavedRegisters);
    
    // Set ControlPC to be the same as the saved "return address"
    // value, which is actually a ControlPC in the frameless method (e.g.
    // faulting address incase of AV or TAE).
    pRD->pCurrentContext->Pc = m_ReturnAddress;
    
    // Set the caller SP
    pRD->pCurrentContext->Sp = dac_cast<TADDR>(this) + sizeof(*this);
    
    // Finally, syncup the regdisplay with the context
    SyncRegDisplayToCurrentContext(pRD);
    
    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

#ifndef DACCESS_COMPILE

void TailCallFrame::InitFromContext(T_CONTEXT * pContext)
{
    WRAPPER_NO_CONTRACT;

    r4  = pContext->R4;
    r5  = pContext->R5;
    r6  = pContext->R6;
    r7  = pContext->R7;
    r8  = pContext->R8;
    r9  = pContext->R9;
    r10 = pContext->R10;
    r11 = pContext->R11;
    m_ReturnAddress = pContext->Lr;
}

#endif // !DACCESS_COMPILE

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
    pRD->pCurrentContext->R9 = (DWORD) dac_cast<TADDR>(m_pCallSiteSP);
    pRD->pCurrentContextPointers->R9 = (DWORD *)&m_pCallSiteSP;

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
#endif // !CROSSGEN_COMPILE

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

void UMEntryThunkCode::Encode(BYTE* pTargetCode, void* pvSecretParam)
{
    // ldr r12, [pc + 8]
    m_code[0] = 0xf8df;
    m_code[1] = 0xc008;
    // ldr pc, [pc]
    m_code[2] = 0xf8df;
    m_code[3] = 0xf000;

    m_pTargetCode = (TADDR)pTargetCode;
    m_pvSecretParam = (TADDR)pvSecretParam;

    FlushInstructionCache(GetCurrentProcess(),&m_code,sizeof(m_code));
}

#ifndef DACCESS_COMPILE

void UMEntryThunkCode::Poison()
{
    m_pTargetCode = (TADDR)UMEntryThunk::ReportViolation;

    // ldr r0, [pc + 8]
    m_code[0] = 0x4802;
    // nop
    m_code[1] = 0xbf00;

    ClrFlushInstructionCache(&m_code,sizeof(m_code));
}

#endif // DACCESS_COMPILE

///////////////////////////// UNIMPLEMENTED //////////////////////////////////

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE

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

//              +64     stack-based arguments here
//      -- MulticastFrame end
//              +48     r0-r3 argument registers
//              +44     lr return address
//              +40     fp frame pointer
//              +12     r4-r10 callee saved registers
//              +8      datum (typically a MethodDesc*)
//              +4      m_Next
//              +0      the frame vptr
//      -- MulticastFrame start
//              -4      gs cookie
//              -...    floating point argument registers
void StubLinkerCPU::EmitMulticastInvoke(UINT_PTR hash) 
{
    //Decode Multicast Delegate hash
    unsigned int numStackBytes = hash >> 8;
    _ASSERTE(numStackBytes <= 0x7fff);

    unsigned int numFPRegs = (hash & 0xf8) >> 3;
    _ASSERTE(numFPRegs <= 16);

    unsigned int numGenRegs = hash & 0x7;
    _ASSERTE(numGenRegs <= 4);

    DWORD offsetOfFPRegs = 0;

    DWORD cbStackFrame = numStackBytes;
    if (numFPRegs)
    {
        cbStackFrame = ALIGN_UP(cbStackFrame, 8);
        offsetOfFPRegs = cbStackFrame;
        cbStackFrame += 4 * numFPRegs;
    }
    cbStackFrame += sizeof(GSCookie) + sizeof(MulticastFrame);
    cbStackFrame = ALIGN_UP(cbStackFrame, 8);
    DWORD cbStackFrameWithoutSavedRegs = cbStackFrame - (13 * 4); // r0-r11,lr

    // Prolog:
    ThumbEmitProlog(8,                          // Save r4-r11,lr (count doesn't include lr)
                    cbStackFrameWithoutSavedRegs, // Additional space in the stack frame required
                    TRUE);                      // Push argument registers

    DWORD offsetOfFrame = cbStackFrame - sizeof(MulticastFrame);

    // Move the MethodDesc* we're calling to r12.
    //  ldr r12, [r0, #offsetof(DelegateObject, _methodPtrAux)]
    ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(0), DelegateObject::GetOffsetOfMethodPtrAux());

    // Initialize MulticastFrame::m_pMD to the MethodDesc* we're calling
    //  str r12, [sp + #(offsetOfFrame + offsetof(MulticastFrame, m_pMD))]
    ThumbEmitStoreRegIndirect(ThumbReg(12), thumbRegSp, offsetOfFrame + MulticastFrame::GetOffsetOfDatum());

    if (numFPRegs)
    {
        ThumbEmitAdd(ThumbReg(4), thumbRegSp, offsetOfFPRegs);

        // save floating point arguments at offsetOfFPRegs
        //vstm{IA} R4,{s0-s(numFPRegs -1)}
        Emit16(0xec84);
        Emit16(0x0a00 | (WORD)numFPRegs);
    }

    // Initialize and link the MulticastFrame and associated GS cookie.
    EmitStubLinkFrame(MulticastFrame::GetMethodFrameVPtr(), offsetOfFrame, MulticastFrame::GetOffsetOfTransitionBlock());

    //r7 as counter. Initialize it to 0.
    // mov r7, 0
    ThumbEmitMovConstant(ThumbReg(7), 0);
    
    //initialize r9 to _invocationCount
    ThumbEmitLoadRegIndirect(ThumbReg(9), ThumbReg(0), DelegateObject::GetOffsetOfInvocationCount());

    CodeLabel *pLoopLabel = NewCodeLabel();
    CodeLabel *pEndLoopLabel = NewCodeLabel();

    //loop:
    EmitLabel(pLoopLabel);

    // cmp r7, r9
    ThumbEmitCmpReg(ThumbReg(7), ThumbReg(9));

    // if equal goto endloop
    // beq endloop
    ThumbEmitCondFlagJump(pEndLoopLabel, 0);

    UINT32 count = 0;
    if(numStackBytes)
    {
        //r1 = pos for stack args in Frame
        ThumbEmitAdd(ThumbReg(1), ThumbReg(4), MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs());

        //r2 = stack pos for args of calling func
        ThumbEmitMovRegReg(ThumbReg(2), thumbRegSp);

        //    ..move stack args..
        _ASSERTE(numStackBytes%4 == 0);
        while (count != numStackBytes)
        {
            ThumbEmitLoadIndirectPostIncrement(ThumbReg(0), ThumbReg(1), 4);
            ThumbEmitStoreIndirectPostIncrement(ThumbReg(0), ThumbReg(2), 4);
            count += 4;
        }
    }

    count = 1;
    while(count < numGenRegs)
    {
        ThumbEmitLoadRegIndirect(ThumbReg(count), ThumbReg(4), MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters() + count*4);
        count++;
    }

    if(numFPRegs)
    {
        ThumbEmitAdd(ThumbReg(0), thumbRegSp, offsetOfFPRegs);
        //vldm{IA}.32 R0, s0-s(numFPRegs-1) 
        Emit16(0xec90);
        Emit16(0x0a00 | (WORD)numFPRegs);
    }    

    //ldr r0, [r4+0x30] // get the first argument
    ThumbEmitLoadRegIndirect(ThumbReg(0),ThumbReg(4), MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters());

    //  ldr r6, [r0+0x14] //invocationList
    ThumbEmitLoadRegIndirect(ThumbReg(6), ThumbReg(0), DelegateObject::GetOffsetOfInvocationList());

    // r6 - address of first delegate in invocation list
    // add r6,r6,0xC
    ThumbEmitAdd(ThumbReg(6), ThumbReg(6), PtrArray::GetDataOffset());

    //ldr r8,[r6+r7*4] //get delegate object
    ThumbEmitLoadOffsetScaledReg(ThumbReg(8), ThumbReg(6), ThumbReg(7), 2);

    // ldr r0, [r8+0x04] //_target from the delegate
    ThumbEmitLoadRegIndirect(ThumbReg(0), ThumbReg(8), DelegateObject::GetOffsetOfTarget());
    
    // ldr r8, [r8+0xC] // methodPtr from the delegate
    ThumbEmitLoadRegIndirect(ThumbReg(8), ThumbReg(8), DelegateObject::GetOffsetOfMethodPtr());

    //call delegate
    ThumbEmitCallRegister(ThumbReg(8));

    //increment counter
    ThumbEmitAdd(ThumbReg(7), ThumbReg(7), 1);

    // The debugger may need to stop here, so grab the offset of this code.
    EmitPatchLabel();

    //goto loop
    ThumbEmitNearJump(pLoopLabel);

    //endloop:
    EmitLabel(pEndLoopLabel);

  
    //At this point of the stub:
    //r4 must point to Frame
    //and r5 must be current Thread*
    
    EmitStubUnlinkFrame();

    // Epilog
    ThumbEmitEpilog();
}

void StubLinkerCPU::EmitSecureDelegateInvoke(UINT_PTR hash)
{
    //Decode Multicast Delegate hash
    unsigned int numStackBytes = hash >> 8;
    _ASSERTE(numStackBytes <= 0x7fff);

    DWORD cbStackFrame = numStackBytes + sizeof(GSCookie) + sizeof(SecureDelegateFrame);
    cbStackFrame = ALIGN_UP(cbStackFrame, 8);
    DWORD cbStackFrameWithoutSavedRegs = cbStackFrame - (13 * 4); // r0-r11,lr

    // Prolog:
    ThumbEmitProlog(8,                          // Save r4-r11,lr (count doesn't include lr)
                    cbStackFrameWithoutSavedRegs, // Additional space in the stack frame required
                    TRUE);                      // Push argument registers

    DWORD offsetOfFrame = cbStackFrame - sizeof(SecureDelegateFrame);

    // Move the MethodDesc* we're calling to r12.
    //  ldr r12, [r0, #offsetof(DelegateObject, _invocationCount)]
    ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(0), DelegateObject::GetOffsetOfInvocationCount());

    // Initialize SecureDelegateFrame::m_pMD to the MethodDesc* we're calling
    //  str r12, [sp + #(offsetOfFrame + offsetof(SecureDelegateFrame, m_pMD))]
    ThumbEmitStoreRegIndirect(ThumbReg(12), thumbRegSp, offsetOfFrame + SecureDelegateFrame::GetOffsetOfDatum());

    // Initialize and link the SecureDelegateFrame and associated GS cookie.
    EmitStubLinkFrame(SecureDelegateFrame::GetMethodFrameVPtr(), offsetOfFrame, SecureDelegateFrame::GetOffsetOfTransitionBlock());

    // At this point:
    //  r0 : secure delegate
    //  r4 : SecureDelegateFrame *
    //  r5 : Thread *

    if (numStackBytes)
    {
        // Copy stack based arguments from the calling frame into this one. Use the following registers:
        //  r6 : pointer to source arguments
        //  r7 : pointer to destination arguments
        //  r8 : temporary storage during copy operation

        // add r6, r4, #MulticastFrame::GetOffsetOfArgs()
        ThumbEmitAdd(ThumbReg(6), ThumbReg(4), MulticastFrame::GetOffsetOfTransitionBlock() + TransitionBlock::GetOffsetOfArgs());

        // mov r7, sp
        ThumbEmitMovRegReg(ThumbReg(7), thumbRegSp);

        // Unrolled loop to copy the stack based arguments. Might want to consider a second path with a loop
        // for large argument lists if anyone complains about this.
        _ASSERTE((numStackBytes % 4) == 0);
        for (unsigned int i = 0; i < numStackBytes; i += 4)
        {
            // Read one 4-byte value from the source stack and copy it to the new stack, post-incrementing
            // both source and destination as we go.
            //  ldr r8, [r6], #4
            //  str r8, [r7], #4
            ThumbEmitLoadIndirectPostIncrement(ThumbReg(8), ThumbReg(6), 4);
            ThumbEmitStoreIndirectPostIncrement(ThumbReg(8), ThumbReg(7), 4);
        }
    }

    // Stack-based arguments are copied. Floating point argument registers and r1-r3 are all still correct.
    // All we need to do now is calculate the real value for r0 and the target address. Secure delegates wrap
    // an inner delegate (kept in _invocationList). We retrieve this inner delegate and then perform the usual
    // delegate invocation pattern on that.

    // Get "real" delegate.
    //  ldr r0, [r0, #offsetof(DelegateObject, _invocationList)]
    ThumbEmitLoadRegIndirect(ThumbReg(0), ThumbReg(0), DelegateObject::GetOffsetOfInvocationList());

    // Load the destination address from the inner delegate.
    //  ldr r12, [r0, #offsetof(DelegateObject, _methodPtr)]
    ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(0), DelegateObject::GetOffsetOfMethodPtr());

    // This is only required for unbound delegates which use VSD stubs..but does not harm if done unconditionally
    // add r4, r0+#offsetof(DelegateObject, _methodPtrAux) ; // r4 now contains indirection cell
    ThumbEmitAdd(ThumbReg(4), ThumbReg(0), DelegateObject::GetOffsetOfMethodPtrAux());

    // Replace the delegate reference with the object cached as the delegate's target.
    //  ldr r0, [r0, #offsetof(DelegateObject, _target)]
    ThumbEmitLoadRegIndirect(ThumbReg(0), ThumbReg(0), DelegateObject::GetOffsetOfTarget());

    // Perform the call.
    //  blx r12
    ThumbEmitCallRegister(ThumbReg(12));

    // restore frame pointer in r4
    ThumbEmitAdd(ThumbReg(4), thumbRegSp, offsetOfFrame);

    // Unlink SecureDelegateFrame. This requires the frame pointer in r4 and the thread pointer in r5.
    EmitStubUnlinkFrame();

    // Epilog
    ThumbEmitEpilog();
}

//The function expects r4 to point to frame 
//and r5 must be current Thread*
void StubLinkerCPU::EmitStubUnlinkFrame()
{
#ifdef _DEBUG
    // EmitStubUnlinkFrame is emitted just before the epilog.
    // Thus, at this point, all other callee-saved registers
    // could be used since we are anyways going to restore them
    // via epilog execution.
    
    // Ensure that GSCookie is valid
    //
    // ldr r6, [r4-4]; Load the value of GSCookie
    ThumbEmitSub(ThumbReg(6), ThumbReg(4), 4);
    ThumbEmitLoadRegIndirect(ThumbReg(6), ThumbReg(6), 0);
    
    // mov r7, s_gsCookie
    ThumbEmitMovConstant(ThumbReg(7), GetProcessGSCookie());
    
    // cmp r6, r7 ; Are the GSCookie values in sync?
    ThumbEmitCmpReg(ThumbReg(6), ThumbReg(7));
    
    CodeLabel *pAllDoneLabel = NewCodeLabel();

    // beq AllDone; yes, GSCookie is good.
    ThumbEmitCondFlagJump(pAllDoneLabel, 0);
    
    // If we are here, then GSCookie was bad.
    // Call into DoJITFailFast.
    //
    // mov r12, DoJITFailFast
    ThumbEmitMovConstant(ThumbReg(12), (int)DoJITFailFast);
    // bl r12
    ThumbEmitCallRegister(ThumbReg(12));
    // Emit a breakpoint - we are not expected to come here at all
    // if we performed a FailFast.
    ThumbEmitBreakpoint();
    
    //AllDone:
    EmitLabel(pAllDoneLabel);
#endif // _DEBUG

    // Unlink the MulticastFrame.
    //  ldr r6, [r4 + #offsetof(MulticastFrame, m_Next)]
    //  str r6, [r5 + #offsetof(Thread, m_pFrame)]
    ThumbEmitLoadRegIndirect(ThumbReg(6), ThumbReg(4), Frame::GetOffsetOfNextLink());
    ThumbEmitStoreRegIndirect(ThumbReg(6), ThumbReg(5), offsetof(Thread, m_pFrame));

}

//pFrameVptr = vtable ptr of Frame
//offsetOfFrame = Frame offset in bytes from sp
//After this method: r4 points to the Frame on stack
// and r5 has current Thread*
void StubLinkerCPU::EmitStubLinkFrame(TADDR pFrameVptr, int offsetOfFrame, int offsetOfTransitionBlock)
{
    // Initialize r4 to point to where we start filling the frame.
    ThumbEmitAdd(ThumbReg(4), thumbRegSp, offsetOfFrame - sizeof(GSCookie));

    // Write the initial GS cookie value
    //  mov r5, s_gsCookie
    //  str r5, [r4]
    ThumbEmitMovConstant(ThumbReg(5), s_gsCookie);
    ThumbEmitStoreIndirectPostIncrement(ThumbReg(5), ThumbReg(4), 4);

    // Initialize the vtable pointer.
    //  mov r5, #vfptr
    //  str r5, [r4 + #offsetof(Frame, _vfptr)]
    ThumbEmitMovConstant(ThumbReg(5), pFrameVptr);
    ThumbEmitStoreRegIndirect(ThumbReg(5), ThumbReg(4), 0);

    // Link the frame to the thread's frame chain.
    //  r5 <- current Thread*
    //  ldr r6, [r5 + #offsetof(Thread, m_pFrame)]
    //  str r6, [r4 + #offsetof(MulticastFrame, m_Next)]
    //  str r4, [r5 + #offsetof(Thread, m_pFrame)]

    ThumbEmitGetThread(ThumbReg(5));
#ifdef FEATURE_PAL
    // reload argument registers that could have been corrupted by the call
    for (int reg = 0; reg < 4; reg++)
        ThumbEmitLoadRegIndirect(ThumbReg(reg), ThumbReg(4), 
            offsetOfTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters() + offsetof(ArgumentRegisters, r[reg]));
#endif

    ThumbEmitLoadRegIndirect(ThumbReg(6), ThumbReg(5), Thread::GetOffsetOfCurrentFrame());
    ThumbEmitStoreRegIndirect(ThumbReg(6), ThumbReg(4), Frame::GetOffsetOfNextLink());
    ThumbEmitStoreRegIndirect(ThumbReg(4), ThumbReg(5), Thread::GetOffsetOfCurrentFrame());
}

#endif // CROSSGEN_COMPILE

void StubLinkerCPU::ThumbEmitNearJump(CodeLabel *target)
{
    WRAPPER_NO_CONTRACT;
    EmitLabelRef(target, reinterpret_cast<ThumbNearJump&>(gThumbNearJump), 0xe);
}

void StubLinkerCPU::ThumbEmitCondFlagJump(CodeLabel *target, UINT cond)
{
    WRAPPER_NO_CONTRACT;
    EmitLabelRef(target, reinterpret_cast<ThumbNearJump&>(gThumbNearJump), cond);
}

void StubLinkerCPU::ThumbEmitCondRegJump(CodeLabel *target, BOOL nonzero, ThumbReg reg)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(reg <= 7);
    UINT variation = reg;
    if(nonzero)
        variation = variation | 0x8;
    EmitLabelRef(target, reinterpret_cast<ThumbCondJump&>(gThumbCondJump), variation);
}

UINT_PTR StubLinkerCPU::HashMulticastInvoke(MetaSig *pSig)
{
    // Generate a hash key as follows:
    // Bit0-2   : num of general purpose registers used 
    // Bit3-7   : num of FP regs used (counting in terms of s0,s1...)
    // Bit8-22 : num of stack bytes used

    ArgIterator delegateCallConv(pSig);

    UINT numStackBytes = delegateCallConv.SizeOfArgStack();

    if (numStackBytes > 0x7FFF) 
        COMPlusThrow(kNotSupportedException, W("NotSupported_TooManyArgs"));

    int cGenReg = 1; // r0 is always used for this pointer
    int cFPReg = 0;

    // if it has a return buffer argument r1 is also used
    if(delegateCallConv.HasRetBuffArg())
        cGenReg = 2;

    int argOffset;
    while ((argOffset = delegateCallConv.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        ArgLocDesc currArgLoc;
        delegateCallConv.GetArgLoc(argOffset, &currArgLoc);

        if(currArgLoc.m_idxGenReg != -1)
            cGenReg = currArgLoc.m_idxGenReg + currArgLoc.m_cGenReg;

        if(currArgLoc.m_idxFloatReg != -1)
            cFPReg = currArgLoc.m_idxFloatReg + currArgLoc.m_cFloatReg;
    }

    // only r0-r3 can be used for arguments
    _ASSERTE(cGenReg <= 4);

    // only s0-s15 can be used for arguments
    _ASSERTE(cFPReg <= 16);

    return (numStackBytes << 8 | cFPReg << 3 | cGenReg);
}

void StubLinkerCPU::ThumbCopyOneTailCallArg(UINT * pnSrcAlign, const ArgLocDesc * pArgLoc, UINT * pcbStackSpace)
{
    if (pArgLoc->m_fRequires64BitAlignment && (*pnSrcAlign & 1)) {
        // ADD R0, #4
        ThumbEmitIncrement(ThumbReg(0), 4);
        *pnSrcAlign = 0;
    }

    // Integer register arguments
    if (pArgLoc->m_cGenReg > 0) {
        int iReg = pArgLoc->m_idxGenReg;
        int maxReg = iReg + pArgLoc->m_cGenReg;
        while (iReg + 2 <= maxReg) {
            // LDM r0!, {r4,r5} ; Post incremented loads (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(0), true, ThumbReg(4).Mask() | ThumbReg(5).Mask());
            // STR r4, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(4), ThumbReg(1), offsetof(T_CONTEXT, R0) + (iReg * sizeof(DWORD)));
            iReg++;
            // STR r5, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(5), ThumbReg(1), offsetof(T_CONTEXT, R0) + (iReg * sizeof(DWORD)));
            iReg++;
        }
        if (iReg < maxReg) {
            // LDR r3, [R0], #+4 ; Post incremented load (4 bytes)
            ThumbEmitLoadIndirectPostIncrement(ThumbReg(3), ThumbReg(0), 4);
            (*pnSrcAlign)++;

            // STR r3, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(3), ThumbReg(1), offsetof(T_CONTEXT, R0) + (iReg * sizeof(DWORD)));
        }
    }
    if (pArgLoc->m_cFloatReg > 0) {
        int iReg = pArgLoc->m_idxFloatReg;
        int maxReg = iReg + pArgLoc->m_cFloatReg;
        while (iReg + 2 <= maxReg) {
            // LDM r0!, {r4,r5} ; Post incremented loads (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(0), true, ThumbReg(4).Mask() | ThumbReg(5).Mask());
            // STR r4, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(4), ThumbReg(1), offsetof(T_CONTEXT, S) + (iReg * sizeof(DWORD)));
            iReg++;
            // STR r5, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(5), ThumbReg(1), offsetof(T_CONTEXT, S) + (iReg * sizeof(DWORD)));
            iReg++;
        }
        if (iReg < maxReg) {
            // LDR r3, [R0], #+4 ; Post incremented load (4 bytes)
            ThumbEmitLoadIndirectPostIncrement(ThumbReg(3), ThumbReg(0), 4);
            (*pnSrcAlign)++;

            // STR r3, [R1, #offset of arg reg] ; (2 bytes)
            ThumbEmitStoreRegIndirect(ThumbReg(3), ThumbReg(1), offsetof(T_CONTEXT, S) + (iReg * sizeof(DWORD)));
        }
    }

    if (pArgLoc->m_cStack > 0) {
        // Copy to the stack
        // Be careful because this can get big and ugly.
        _ASSERTE(*pcbStackSpace <= (pArgLoc->m_idxStack * sizeof(DWORD)));

        // Pad the output
        if (*pcbStackSpace < (pArgLoc->m_idxStack * sizeof(DWORD)))
        {
            const UINT cbPad = ((pArgLoc->m_idxStack * sizeof(DWORD)) - *pcbStackSpace);
            _ASSERTE(cbPad == 4);
            // ADD R2, #4
            ThumbEmitIncrement(ThumbReg(2), cbPad);
            *pcbStackSpace += cbPad;
        }
        int cStack = pArgLoc->m_cStack;
        *pcbStackSpace += (cStack * sizeof(DWORD));

        // Now start the copying
        if (cStack > 8) {
            // Loop to copy in 16-byte chunks per loop.
            // Sacrifice r3 for the loop counter
            ThumbEmitMovConstant(ThumbReg(3), pArgLoc->m_cStack & ~3);
            // LoopLabel:
            CodeLabel *pLoopLabel = NewCodeLabel();
            EmitLabel(pLoopLabel);
            const WORD mask = ThumbReg(4).Mask() | ThumbReg(5).Mask() | ThumbReg(6).Mask() | ThumbReg(7).Mask();
            // LDM r0!, {r4,r5,r6,r7} ; Post incremented loads (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(0), true, mask);
            // STM r2!, {r4,r5,r6,r7} ; Post incremented stores (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(2), false, mask);
            // SUBS r3, #4
            Emit16((WORD)(0x3800 | (ThumbReg(3) << 8) | 4));
            // BNZ LoopLabel
            ThumbEmitCondFlagJump(pLoopLabel, thumbCondNe.cond);

            cStack = cStack % 4;
            // Now deal with the tail if any
        }
        _ASSERTE(cStack <= 8);

        while (cStack > 1) {
            _ASSERTE(cStack >= 2);
            WORD mask = ThumbReg(4).Mask() | ThumbReg(5).Mask();
            cStack -= 2;
            if (cStack > 0) {
                mask |= ThumbReg(6).Mask();
                cStack--;
                // Instead of copying 4 slots and leaving a single slot remainder
                // which would require us to use the bigger opcodes for the tail
                // Only copy 3 slots this loop, saving 2 for next time. :)
                if (cStack == 1 || cStack > 2) {
                    mask |= ThumbReg(7).Mask();
                    cStack--;
                }
                else {
                    // We're reading an odd amount from the stack
                    (*pnSrcAlign)++;
                }
            }

            // LDM r0!, {r4,r5,r6,r7} ; Post incremented loads (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(0), true, mask);
            // STM r2!, {r4,r5,r6,r7} ; Post incremented stores (2 bytes)
            ThumbEmitLoadStoreMultiple(ThumbReg(2), false, mask);
            _ASSERTE((cStack == 0) || (cStack >= 2));
        }
        if (cStack > 0) {
            _ASSERTE(cStack == 1);
            // We're reading an odd amount from the stack
            (*pnSrcAlign)++;
            // LDR r12, [R0], #+4 ; Post incremented load (4 bytes)
            ThumbEmitLoadIndirectPostIncrement(ThumbReg(12), ThumbReg(0), 4);
            // STR r12, [R2], #+4 ; Post incremented store (4 bytes)
            ThumbEmitStoreIndirectPostIncrement(ThumbReg(12), ThumbReg(2), 4);
        }
    }
}


Stub * StubLinkerCPU::CreateTailCallCopyArgsThunk(CORINFO_SIG_INFO * pSig,
                                                  MethodDesc* pMD,
                                                  CorInfoHelperTailCallSpecialHandling flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER   sl;
    CPUSTUBLINKER*  pSl = &sl;

    // Generates a function that looks like this:
    // size_t CopyArguments(va_list args,         (R0)
    //                      CONTEXT *pCtx,        (R1)
    //                      DWORD   *pvStack,     (R2)
    //                      size_t  cbStack)      (R3)
    // {
    //     if (pCtx != NULL) {
    //         foreach (arg in args) {
    //             copy into pCtx or pvStack
    //         }
    //     }
    //     return <size of stack needed>;
    // }
    //

    Module * module = GetModule(pSig->scope);
    Instantiation classInst((TypeHandle*)pSig->sigInst.classInst, pSig->sigInst.classInstCount);
    Instantiation methodInst((TypeHandle*)pSig->sigInst.methInst, pSig->sigInst.methInstCount);
    SigTypeContext typeCtxt(classInst, methodInst);

    // The -8 is because R11 points at the pushed {R11, LR} pair, and it is aligned.
    // This is the magic distance, between the frame pointer and the Frame.
    const UINT cbFrameOffset = (sizeof(FrameWithCookie<TailCallFrame>) - 8);

    bool fNeedExtraRegs = false;
    UINT copyEstimate = 0;
    {
        // Do a quick scan of the arguments looking for ones that will probably need extra registers
        // and guestimating the size of the method
        if (flags & CORINFO_TAILCALL_STUB_DISPATCH_ARG)
            copyEstimate += 6;

        if (pSig->hasThis())
            copyEstimate += 6;

        MetaSig msig(pSig->pSig, pSig->cbSig, module, &typeCtxt);
        if (pSig->hasTypeArg())
            msig.SetHasParamTypeArg();
        ArgIterator argPlacer(&msig);

        if (argPlacer.HasRetBuffArg()) {
            copyEstimate += 24;
        }

        if (pSig->hasTypeArg() || pSig->isVarArg())
            copyEstimate += 6;

        int argOffset;
        while ((argOffset = argPlacer.GetNextOffset()) != TransitionBlock::InvalidOffset)
        {
            ArgLocDesc argLoc;
            argPlacer.GetArgLoc(argOffset, &argLoc);

            if (argLoc.m_cStack  > 1 || argLoc.m_cGenReg > 1 || argLoc.m_cFloatReg > 1) {
                fNeedExtraRegs = true;
            }
            else {
                copyEstimate += 8;
            }
        }
    }

    if (fNeedExtraRegs) {
        // Inject a proper prolog
        // push {r4-r7,lr}
        pSl->ThumbEmitProlog(4, 0, false);
    }

    CodeLabel *pNullLabel = pSl->NewCodeLabel();

    if (!fNeedExtraRegs && copyEstimate < 100) {
        // The real range of BCZ is 0-126, but that's hard to estimate that precisely
        // and we don't want to do that much work just to save a few bytes

        // BCZ R1, NullLabel
        pSl->ThumbEmitCondRegJump(pNullLabel, false, ThumbReg(1));
    }
    else {
        // CMP R1, 0 ; T1 encoding
        pSl->Emit16((WORD)(0x2900));

        // BEQ NullLabel
        pSl->ThumbEmitCondFlagJump(pNullLabel, thumbCondEq.cond);
    }

    UINT cbStackSpace = 0;
    UINT cbReturnBufferSpace = 0;
    UINT nSrcAlign = 0;

    if (flags & CORINFO_TAILCALL_STUB_DISPATCH_ARG) {
        // This is set for stub dispatch or 'thisInSecretRegister'
        // The JIT placed an extra argument in the list that needs to
        // get shoved into R4, and not counted.
        // pCtx->R4 = va_arg(args, DWORD);

        // LDR r3, [R0], #+4 ; Post incremented load (4 bytes)
        pSl->ThumbEmitLoadIndirectPostIncrement(ThumbReg(3), ThumbReg(0), 4);
        // STR r3, [R1, #offset of R4] ; (2 bytes)
        pSl->ThumbEmitStoreRegIndirect(ThumbReg(3), ThumbReg(1), offsetof(T_CONTEXT, R4));
        nSrcAlign++;
    }


    MetaSig msig(pSig->pSig, pSig->cbSig, module, &typeCtxt);
    if (pSig->hasTypeArg())
        msig.SetHasParamTypeArg();
    ArgIterator argPlacer(&msig);
    ArgLocDesc argLoc;

    // First comes the 'this' pointer
    if (argPlacer.HasThis()) {
        argPlacer.GetThisLoc(&argLoc);
        pSl->ThumbCopyOneTailCallArg(&nSrcAlign, &argLoc, &cbStackSpace);
    }

    // Next comes the return buffer
    if (argPlacer.HasRetBuffArg()) {
        // We always reserve space for the return buffer, but we never zero it out,
        // and we never report it.  Thus the callee shouldn't do RVO and expect
        // to be able to read GC pointers from it.
        // If the passed in return buffer is already pointing above the frame,
        // then we need to pass it along (so it will get passed out).
        // Otherwise we assume the caller is returning void, so we just pass in
        // dummy space to be overwritten.

        argPlacer.GetRetBuffArgLoc(&argLoc);
        _ASSERTE(argLoc.m_cStack == 0);
        _ASSERTE(argLoc.m_cFloatReg == 0);
        _ASSERTE(argLoc.m_cGenReg == 1);

        // Grab some space from the top of the frame and pass that in as a dummy
        // buffer if needed. Align to 8-byte boundary (after taking in account the Frame).
        // Do this by adding the Frame size, align, then remove the Frame size...
        _ASSERTE((pSig->retType == CORINFO_TYPE_REFANY) || (pSig->retType == CORINFO_TYPE_VALUECLASS));
        TypeHandle th(pSig->retTypeClass);
        UINT cbUsed = ((th.GetSize() + cbFrameOffset + 0x7) & ~0x7) - cbFrameOffset;
        _ASSERTE(cbUsed >= th.GetSize());
        cbReturnBufferSpace += cbUsed;

        // LDR r3, [R0], #+4 ; Post incremented load (4 bytes)
        pSl->ThumbEmitLoadIndirectPostIncrement(ThumbReg(3), ThumbReg(0), 4);

        // LDR r12, [R1, #offset of R11] ; (2 bytes)
        pSl->ThumbEmitLoadRegIndirect(ThumbReg(12), ThumbReg(1), offsetof(T_CONTEXT, R11));

        // CMP r3, r12 ; (2 bytes)
        pSl->ThumbEmitCmpReg(ThumbReg(3), ThumbReg(12));

        CodeLabel *pSkipLabel = pSl->NewCodeLabel();
        // BHI NullLabel ; skip if R3 > R12 unsigned (2 bytes)
        pSl->ThumbEmitCondFlagJump(pSkipLabel, thumbCondHi.cond);

        // Also check the lower bound of the stack in case the return buffer is on the GC heap
        // and the GC heap is below the stack
        // CMP r3, sp ; (2 bytes)
        pSl->ThumbEmitCmpReg(ThumbReg(3), thumbRegSp);
        // BLO NullLabel ; skip if r3 < sp unsigned (2 bytes)
        pSl->ThumbEmitCondFlagJump(pSkipLabel, thumbCondCc.cond);

        // If the caller is expecting us to simulate a return buffer for the callee
        // pass that pointer in now, by subtracting from R11 space for the Frame
        // and space for the return buffer.
        UINT offset = cbUsed + cbFrameOffset;
        if (offset < 4096) {
            // SUB r3, r12, #offset ; (4 bytes)
            pSl->ThumbEmitSub(ThumbReg(3), ThumbReg(12), offset);
        }
        else {
            offset = UINT(-int(offset)); // Silence the @#$%^ warning
            // MOVW/MOVT (4-8 bytes)
            // ADD r3, r12; (2 bytes)
            pSl->ThumbEmitAdd(ThumbReg(3), ThumbReg(12), offset);
        }
        // SkipLabel:
        pSl->EmitLabel(pSkipLabel);
        // STR r3, [R1, #offset of arg reg] ; (2 bytes)
        pSl->ThumbEmitStoreRegIndirect(ThumbReg(3), ThumbReg(1), offsetof(T_CONTEXT, R0) + (argLoc.m_idxGenReg * sizeof(DWORD)));

        nSrcAlign++;
    }

    // Generics Instantiation Parameter
    if (pSig->hasTypeArg()) {
        argPlacer.GetParamTypeLoc(&argLoc);
        pSl->ThumbCopyOneTailCallArg(&nSrcAlign, &argLoc, &cbStackSpace);
    }

    // VarArgs Cookie Parameter
    if (pSig->isVarArg()) {
        argPlacer.GetVASigCookieLoc(&argLoc);
        pSl->ThumbCopyOneTailCallArg(&nSrcAlign, &argLoc, &cbStackSpace);
    }

    // Now for *all* the 'real' arguments
    int argOffset;
    while ((argOffset = argPlacer.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        argPlacer.GetArgLoc(argOffset, &argLoc);

        pSl->ThumbCopyOneTailCallArg(&nSrcAlign, &argLoc, &cbStackSpace);
    }

    // Now that we are done moving arguments, add back in the stack space we reserved
    // for the return buffer.
    cbStackSpace += cbReturnBufferSpace;

    // Keep the stack space 8-byte aligned
    if ((cbStackSpace + cbFrameOffset) & 7) {
        cbStackSpace += 4;
    }
    _ASSERTE(((cbStackSpace + cbFrameOffset) & 7) == 0);

    CodeLabel *pReturnLabel = pSl->NewCodeLabel();
    // B ReturnLabel:
    pSl->ThumbEmitNearJump(pReturnLabel);

    // NullLabel:
    pSl->EmitLabel(pNullLabel);
    // MOVW/MOVT r0, 0 ; No GCLayout info
    pSl->ThumbEmitMovConstant(ThumbReg(0), 0);
    // STR r0, [r3]
    pSl->ThumbEmitStoreRegIndirect(ThumbReg(0), ThumbReg(3), 0);

    // ReturnLabel:
    pSl->EmitLabel(pReturnLabel);

    // MOVW/MOVT r0, #cbStackSpace
    pSl->ThumbEmitMovConstant(ThumbReg(0), cbStackSpace);

    if (fNeedExtraRegs) {
        // Inject a proper prolog
        // pop {r4-r7,pc}
        pSl->ThumbEmitEpilog();
    }
    else {
        // bx lr
        pSl->ThumbEmitJumpRegister(thumbRegLr);
    }

    LoaderHeap* pHeap = pMD->GetLoaderAllocator()->GetStubHeap();
    return pSl->Link(pHeap);
}


VOID ResetCurrentContext()
{
    LIMITED_METHOD_CONTRACT;
}
#endif // !DACCESS_COMPILE


#ifdef FEATURE_COMINTEROP
void emitCOMStubCall (ComCallMethodDesc *pCOMMethod, PCODE target)
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

    BYTE *pBuffer = (BYTE*)pCOMMethod - COMMETHOD_CALL_PRESTUB_SIZE;

    memcpy(pBuffer, rgCode, sizeof(rgCode));
    *((PCODE*)(pBuffer + sizeof(rgCode) + 2)) = target;

    // Ensure that the updated instructions get actually written
    ClrFlushInstructionCache(pBuffer, COMMETHOD_CALL_PRESTUB_SIZE);

    _ASSERTE(IS_ALIGNED(pBuffer + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET, sizeof(void*)) &&
             *((PCODE*)(pBuffer + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET)) == target);
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

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStart = (BYTE *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) { *(WORD *)p = 0xdefe; p += 2; } \
    ClrFlushInstructionCache(pStart, cbAligned); \
    return (PCODE)((TADDR)pStart | THUMB_CODE)

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

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
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

    EmitHelperWithArg(p, pAllocator, arg, target);

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

    PCODE helperAddress = (pLookup->helper == CORINFO_HELP_RUNTIMEHANDLE_METHOD ?
        GetEEFuncEntryPoint(JIT_GenericHandleMethodWithSlotAndModule) :
        GetEEFuncEntryPoint(JIT_GenericHandleClassWithSlotAndModule));

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    pArgs->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    pArgs->signature = pLookup->signature;
    pArgs->module = (CORINFO_MODULE_HANDLE)pModule;

    // It's available only via the run-time helper function,

    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(18);

        EmitHelperWithArg(p, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int indirectionsSize = 0;
        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if ((i == 0 && pLookup->indirectFirstOffset) || (i == 1 && pLookup->indirectSecondOffset))
            {
                indirectionsSize += (pLookup->offsets[i] >= 0xFFF ? 10 : 2);
                indirectionsSize += 4;
            }
            else
            {
                indirectionsSize += (pLookup->offsets[i] >= 0xFFF ? 10 : 4);
            }
        }

        int codeSize = indirectionsSize + (pLookup->testForNull ? 26 : 2);

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        if (pLookup->testForNull)
        {
            // mov r3, r0
            *(WORD *)p = 0x4603;
            p += 2;
        }

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if ((i == 0 && pLookup->indirectFirstOffset) || (i == 1 && pLookup->indirectSecondOffset))
            {
                if (pLookup->offsets[i] >= 0xFF)
                {
                    // mov r2, offset
                    MovRegImm(p, 2, pLookup->offsets[i]);
                    p += 8;

                    // add r0, r2
                    *(WORD *)p = 0x4410;
                    p += 2;
                }
                else
                {
                    // add r0, <offset>
                   *(WORD *)p = (WORD)((WORD)0x3000 | (WORD)((0x00FF) & pLookup->offsets[i]));
                   p += 2;
                }

                // r0 is pointer + offset[0]
                // ldr r2, [r0]
                *(WORD *)p = 0x6802;
                p += 2;

                // r2 is offset1
                // add r0, r2
                *(WORD *)p = 0x4410;
                p += 2;
            }
            else
            {
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
        }

        // No null test required
        if (!pLookup->testForNull)
        {
            // mov pc, lr
            *(WORD *)p = 0x46F7;
            p += 2;
        }
        else
        {
            // cbz r0, nullvaluelabel
            *(WORD *)p = 0xB100;
            p += 2;
            // mov pc, lr
            *(WORD *)p = 0x46F7;
            p += 2;
            // nullvaluelabel:
            // mov r0, r3
            *(WORD *)p = 0x4618;
            p += 2;

            EmitHelperWithArg(p, pAllocator, (TADDR)pArgs, helperAddress);
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}
#endif // FEATURE_READYTORUN

#endif // CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE
