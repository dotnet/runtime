// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: JITinterfaceX86.CPP
//
// ===========================================================================

// This contains JITinterface routines that are tailored for
// X86 platforms. Non-X86 versions of these can be found in
// JITinterfaceGen.cpp


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "eventtrace.h"
#include "threadsuspend.h"

#include <minipal/cpuid.h>

#if defined(_DEBUG) && !defined (WRITE_BARRIER_CHECK)
#define WRITE_BARRIER_CHECK 1
#endif

extern "C" LONG g_global_alloc_lock;

extern "C" void STDCALL JIT_WriteBarrierReg_PreGrow();// JIThelp.asm/JIThelp.s
extern "C" void STDCALL JIT_WriteBarrierReg_PostGrow();// JIThelp.asm/JIThelp.s

#ifdef _DEBUG
extern "C" void STDCALL WriteBarrierAssert(BYTE* ptr, Object* obj)
{
    WRAPPER_NO_CONTRACT;

    static BOOL fVerifyHeap = -1;

    if (fVerifyHeap == -1)
        fVerifyHeap = g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC;

    if (fVerifyHeap)
    {
        if (obj)
        {
            obj->Validate(FALSE);
        }
        if (GCHeapUtilities::GetGCHeap()->IsHeapPointer(ptr))
        {
            Object* pObj = *(Object**)ptr;
            _ASSERTE (pObj == NULL || GCHeapUtilities::GetGCHeap()->IsHeapPointer(pObj));
        }
    }
    else
    {
        _ASSERTE((g_lowest_address <= ptr && ptr < g_highest_address) ||
             ((size_t)ptr < MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT));
    }
}

#endif // _DEBUG

/*********************************************************************/
#ifdef FEATURE_HIJACK
extern "C" void STDCALL JIT_TailCallHelper(Thread * pThread);
void STDCALL JIT_TailCallHelper(Thread * pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    pThread->UnhijackThread();
}
#endif // FEATURE_HIJACK

#define NUM_WRITE_BARRIERS 6

static const BYTE c_rgWriteBarrierRegs[NUM_WRITE_BARRIERS] = {
    0, // EAX
    1, // ECX
    3, // EBX
    6, // ESI
    7, // EDI
    5, // EBP
};

static const void * const c_rgWriteBarriers[NUM_WRITE_BARRIERS] = {
    (void *)JIT_WriteBarrierEAX,
    (void *)JIT_WriteBarrierECX,
    (void *)JIT_WriteBarrierEBX,
    (void *)JIT_WriteBarrierESI,
    (void *)JIT_WriteBarrierEDI,
    (void *)JIT_WriteBarrierEBP,
};

#ifdef WRITE_BARRIER_CHECK
static const void * const c_rgDebugWriteBarriers[NUM_WRITE_BARRIERS] = {
    (void *)JIT_DebugWriteBarrierEAX,
    (void *)JIT_DebugWriteBarrierECX,
    (void *)JIT_DebugWriteBarrierEBX,
    (void *)JIT_DebugWriteBarrierESI,
    (void *)JIT_DebugWriteBarrierEDI,
    (void *)JIT_DebugWriteBarrierEBP,
};
#endif // WRITE_BARRIER_CHECK

/*********************************************************************/
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
#pragma warning (disable : 4731)
void InitJITWriteBarrierHelpers()
{
    STANDARD_VM_CONTRACT;

    // All write barrier helpers should fit into one page.
    // If you hit this assert on retail build, there is most likely problem with BBT script.
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_WriteBarrierGroup_End - (BYTE*)JIT_WriteBarrierGroup < (ptrdiff_t)GetOsPageSize());
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_PatchedWriteBarrierGroup_End - (BYTE*)JIT_PatchedWriteBarrierGroup < (ptrdiff_t)GetOsPageSize());

    // Copy the write barriers to their final resting place.
    if (IsWriteBarrierCopyEnabled())
    {
        for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
        {
            BYTE * pfunc = (BYTE *) JIT_WriteBarrierReg_PreGrow;

            BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);
            int reg = c_rgWriteBarrierRegs[iBarrier];

            BYTE * pBufRW = pBuf;
            ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
            barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 34);
            pBufRW = barrierWriterHolder.GetRW();

            memcpy(pBufRW, pfunc, 34);

            // assert the copied code ends in a ret to make sure we got the right length
            _ASSERTE(pBuf[33] == 0xC3);

            // We need to adjust registers in a couple of instructions
            // It would be nice to have the template contain all zeroes for
            // the register fields (corresponding to EAX), but that doesn't
            // work because then we get a smaller encoding for the compares
            // that only works for EAX but not the other registers.
            // So we always have to clear the register fields before updating them.

            // First instruction to patch is a mov [edx], reg

            _ASSERTE(pBuf[0] == 0x89);
            // Update the reg field (bits 3..5) of the ModR/M byte of this instruction
            pBufRW[1] &= 0xc7;
            pBufRW[1] |= reg << 3;

            // Second instruction to patch is cmp reg, imm32 (low bound)

            _ASSERTE(pBuf[2] == 0x81);
            // Here the lowest three bits in ModR/M field are the register
            pBufRW[3] &= 0xf8;
            pBufRW[3] |= reg;

#ifdef WRITE_BARRIER_CHECK
            // Don't do the fancy optimization just jump to the old one
            // Use the slow one for write barrier checks build because it has some good asserts
            if (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) {
                pfunc = &pBufRW[0];
                *pfunc++ = 0xE9;                // JMP c_rgDebugWriteBarriers[iBarrier]
                *((DWORD*) pfunc) = (BYTE*) c_rgDebugWriteBarriers[iBarrier] - (&pBuf[1] + sizeof(DWORD));
            }
#endif // WRITE_BARRIER_CHECK
        }

#ifndef CODECOVERAGE
        ValidateWriteBarrierHelpers();
#endif
    }

    // Leave the patched region writable for StompWriteBarrierEphemeral(), StompWriteBarrierResize()
}
#pragma warning (default : 4731)

// these constans are offsets into our write barrier helpers for values that get updated as the bounds of the managed heap change.
// ephemeral region
const int AnyGrow_EphemeralLowerBound = 4; // offset is the same for both pre and post grow functions
const int PostGrow_EphemeralUpperBound = 12;

// card table
const int PreGrow_CardTableFirstLocation = 16;
const int PreGrow_CardTableSecondLocation = 28;
const int PostGrow_CardTableFirstLocation = 24;
const int PostGrow_CardTableSecondLocation = 36;


#ifndef CODECOVERAGE        // Deactivate alignment validation for code coverage builds
                            // because the instrumented binaries will not preserve alignment constraints and we will fail.

void ValidateWriteBarrierHelpers()
{
    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where we update these values while the EE is running

#ifdef WRITE_BARRIER_CHECK
    // write barrier checking uses the slower helpers that we don't bash so there is no need for validation
    if (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK)
        return;
#endif // WRITE_BARRIER_CHECK

    // first validate the PreGrow helper
    BYTE* pWriteBarrierFunc = GetWriteBarrierCodeLocation(reinterpret_cast<BYTE*>(JIT_WriteBarrierEAX));

    // ephemeral region
    DWORD* pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // now validate the PostGrow helper
    pWriteBarrierFunc = reinterpret_cast<BYTE*>(JIT_WriteBarrierReg_PostGrow);

    // ephemeral region
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_EphemeralUpperBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
}

#endif //CODECOVERAGE
/*********************************************************************/

#define WriteBarrierIsPreGrow() ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[10] == 0xc1)


/*********************************************************************/
// When a GC happens, the upper and lower bounds of the ephemeral
// generation change.  This routine updates the WriteBarrier thunks
// with the new values.
int StompWriteBarrierEphemeral(bool /* isRuntimeSuspended */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    int stompWBCompleteActions = SWB_PASS;

    if (!IsWriteBarrierCopyEnabled())
    {
        // If we didn't copy the write barriers, then don't update them.
        return SWB_PASS;
    }

#ifdef WRITE_BARRIER_CHECK
        // Don't do the fancy optimization if we are checking write barrier
    if ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[0] == 0xE9)  // we are using slow write barrier
        return stompWBCompleteActions;
#endif // WRITE_BARRIER_CHECK

    // Update the lower bound.
    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);

        BYTE * pBufRW = pBuf;
        ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
        barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 42);
        pBufRW = barrierWriterHolder.GetRW();

        // assert there is in fact a cmp r/m32, imm32 there
        _ASSERTE(pBuf[2] == 0x81);

        // Update the immediate which is the lower bound of the ephemeral generation
        size_t *pfunc = (size_t *) &pBufRW[AnyGrow_EphemeralLowerBound];
        //avoid trivial self modifying code
        if (*pfunc != (size_t) g_ephemeral_low)
        {
            stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            *pfunc = (size_t) g_ephemeral_low;
        }
        if (!WriteBarrierIsPreGrow())
        {
            // assert there is in fact a cmp r/m32, imm32 there
            _ASSERTE(pBuf[10] == 0x81);

                // Update the upper bound if we are using the PostGrow thunk.
            pfunc = (size_t *) &pBufRW[PostGrow_EphemeralUpperBound];
            //avoid trivial self modifying code
            if (*pfunc != (size_t) g_ephemeral_high)
            {
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                *pfunc = (size_t) g_ephemeral_high;
            }
        }
    }

    return stompWBCompleteActions;
}

/*********************************************************************/
// When the GC heap grows, the ephemeral generation may no longer
// be after the older generations.  If this happens, we need to switch
// to the PostGrow thunk that checks both upper and lower bounds.
// regardless we need to update the thunk with the
// card_table - lowest_address.
int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
    } CONTRACTL_END;

    int stompWBCompleteActions = SWB_PASS;

    if (!IsWriteBarrierCopyEnabled())
    {
        // If we didn't copy the write barriers, then don't update them.
        return SWB_PASS;
    }

#ifdef WRITE_BARRIER_CHECK
        // Don't do the fancy optimization if we are checking write barrier
    if ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[0] == 0xE9)  // we are using slow write barrier
        return stompWBCompleteActions;
#endif // WRITE_BARRIER_CHECK

    bool bWriteBarrierIsPreGrow = WriteBarrierIsPreGrow();
    bool bStompWriteBarrierEphemeral = false;

    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);
        int reg = c_rgWriteBarrierRegs[iBarrier];

        size_t *pfunc;

        BYTE * pBufRW = pBuf;
        ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
        barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 42);
        pBufRW = barrierWriterHolder.GetRW();

        // Check if we are still using the pre-grow version of the write barrier.
        if (bWriteBarrierIsPreGrow)
        {
            // Check if we need to use the upper bounds checking barrier stub.
            if (bReqUpperBoundsCheck)
            {
                GCX_MAYBE_COOP_NO_THREAD_BROKEN((GetThreadNULLOk()!=NULL));
                if( !isRuntimeSuspended && !(stompWBCompleteActions & SWB_EE_RESTART) ) {
                    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
                    stompWBCompleteActions |= SWB_EE_RESTART;
                }

                pfunc = (size_t *) JIT_WriteBarrierReg_PostGrow;
                memcpy(pBufRW, pfunc, 42);

                // assert the copied code ends in a ret to make sure we got the right length
                _ASSERTE(pBuf[41] == 0xC3);

                // We need to adjust registers in a couple of instructions
                // It would be nice to have the template contain all zeroes for
                // the register fields (corresponding to EAX), but that doesn't
                // work because then we get a smaller encoding for the compares
                // that only works for EAX but not the other registers
                // So we always have to clear the register fields before updating them.

                // First instruction to patch is a mov [edx], reg

                _ASSERTE(pBuf[0] == 0x89);
                // Update the reg field (bits 3..5) of the ModR/M byte of this instruction
                pBufRW[1] &= 0xc7;
                pBufRW[1] |= reg << 3;

                // Second instruction to patch is cmp reg, imm32 (low bound)

                _ASSERTE(pBuf[2] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBufRW[3] &= 0xf8;
                pBufRW[3] |= reg;

                // Third instruction to patch is another cmp reg, imm32 (high bound)

                _ASSERTE(pBuf[10] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBufRW[11] &= 0xf8;
                pBufRW[11] |= reg;

                bStompWriteBarrierEphemeral = true;
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[22] == 0x80);
                pfunc = (size_t *) &pBufRW[PostGrow_CardTableFirstLocation];
                if (*pfunc != (size_t) g_card_table)
                {
                    stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                    *pfunc = (size_t) g_card_table;
                }

                // What we're trying to update is the offset field of a
                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[34] == 0xC6);
                pfunc = (size_t *) &pBufRW[PostGrow_CardTableSecondLocation];

            }
            else
            {
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[14] == 0x80);
                pfunc = (size_t *) &pBufRW[PreGrow_CardTableFirstLocation];
                if (*pfunc != (size_t) g_card_table)
                {
                    stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                    *pfunc = (size_t) g_card_table;
                }

                // What we're trying to update is the offset field of a

                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[26] == 0xC6);
                pfunc = (size_t *) &pBufRW[PreGrow_CardTableSecondLocation];
            }
        }
        else
        {
            // What we're trying to update is the offset field of a

            // cmp offset[edx], 0ffh instruction
            _ASSERTE(pBuf[22] == 0x80);
            pfunc = (size_t *) &pBufRW[PostGrow_CardTableFirstLocation];
            if (*pfunc != (size_t) g_card_table)
            {
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                *pfunc = (size_t) g_card_table;
            }

            // What we're trying to update is the offset field of a
            // mov offset[edx], 0ffh instruction
            _ASSERTE(pBuf[34] == 0xC6);
            pfunc = (size_t *) &pBufRW[PostGrow_CardTableSecondLocation];
        }

        // Stick in the adjustment value.
        if (*pfunc != (size_t) g_card_table)
        {
            stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            *pfunc = (size_t) g_card_table;
        }
    }

    if (bStompWriteBarrierEphemeral)
    {
        _ASSERTE(isRuntimeSuspended || (stompWBCompleteActions & SWB_EE_RESTART));
        stompWBCompleteActions |= StompWriteBarrierEphemeral(true);
    }
    return stompWBCompleteActions;
}

void FlushWriteBarrierInstructionCache()
{
    ClrFlushInstructionCache(GetWriteBarrierCodeLocation((BYTE*)JIT_PatchedWriteBarrierGroup),
        (BYTE*)JIT_PatchedWriteBarrierGroup_End - (BYTE*)JIT_PatchedWriteBarrierGroup, /* hasCodeExecutedBefore */ true);
}

