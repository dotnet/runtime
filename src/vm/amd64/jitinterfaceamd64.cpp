// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ===========================================================================
// File: JITinterfaceCpu.CPP
// ===========================================================================

// This contains JITinterface routines that are specific to the
// AMD64 platform. They are modeled after the X86 specific routines
// found in JITinterfaceX86.cpp or JIThelp.asm


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "threadsuspend.h"

extern uint8_t* g_ephemeral_low;
extern uint8_t* g_ephemeral_high;
extern uint32_t* g_card_table;

// Patch Labels for the various write barriers
EXTERN_C void JIT_WriteBarrier_End();

EXTERN_C void JIT_WriteBarrier_PreGrow32(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow32_PatchLabel_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow32_PatchLabel_CardTable_Check();
EXTERN_C void JIT_WriteBarrier_PreGrow32_PatchLabel_CardTable_Update();
EXTERN_C void JIT_WriteBarrier_PreGrow32_End();

EXTERN_C void JIT_WriteBarrier_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
EXTERN_C void JIT_WriteBarrier_PreGrow64_End();

EXTERN_C void JIT_WriteBarrier_PostGrow32(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow32_PatchLabel_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow32_PatchLabel_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow32_PatchLabel_CheckCardTable();
EXTERN_C void JIT_WriteBarrier_PostGrow32_PatchLabel_UpdateCardTable();
EXTERN_C void JIT_WriteBarrier_PostGrow32_End();

EXTERN_C void JIT_WriteBarrier_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
EXTERN_C void JIT_WriteBarrier_PostGrow64_End();

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR32(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR32_PatchLabel_CheckCardTable();
EXTERN_C void JIT_WriteBarrier_SVR32_PatchLabel_UpdateCardTable();
EXTERN_C void JIT_WriteBarrier_SVR32_End();

EXTERN_C void JIT_WriteBarrier_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
EXTERN_C void JIT_WriteBarrier_SVR64_End();
#endif

WriteBarrierManager g_WriteBarrierManager;

// Use this somewhat hokey macro to concantonate the function start with the patch 
// label, this allows the code below to look relatively nice, but relies on the 
// naming convention which we have established for these helpers.
#define CALC_PATCH_LOCATION(func,label,offset)      CalculatePatchLocation((PVOID)func, (PVOID)func##_##label, offset)

WriteBarrierManager::WriteBarrierManager() : 
    m_currentWriteBarrier(WRITE_BARRIER_UNINITIALIZED)
{
    LIMITED_METHOD_CONTRACT;
}

#ifndef CODECOVERAGE        // Deactivate alignment validation for code coverage builds 
                            // because the instrumentation tool will not preserve alignmant constraits and we will fail.

void WriteBarrierManager::Validate()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where these values are updated while the EE is running
    // NOTE: we can't call this from the ctor since our infrastructure isn't ready for assert dialogs

    PBYTE pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_Lower, 3);
    PBYTE pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_CardTable_Check, 2);
    PBYTE pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_CardTable_Update, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate2) & 0x3) == 0);

    pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

    PBYTE pUpperBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_Upper, 3);
    pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_Lower, 3);
    pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_CheckCardTable, 2);
    pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_UpdateCardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate2) & 0x3) == 0);


    pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_SVR_GC
    pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR32, PatchLabel_CheckCardTable, 2);
    pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR32, PatchLabel_UpdateCardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate2) & 0x3) == 0);

    pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);
#endif
}

#endif // CODECOVERAGE


PCODE WriteBarrierManager::GetCurrentWriteBarrierCode()
{
    LIMITED_METHOD_CONTRACT;
    
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW32:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PreGrow32);
        case WRITE_BARRIER_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW32:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PostGrow32);
        case WRITE_BARRIER_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR32:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_SVR32);
        case WRITE_BARRIER_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_SVR64);
#endif
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
}

size_t WriteBarrierManager::GetSpecificWriteBarrierSize(WriteBarrierType writeBarrier)
{
// marked asm functions are those which use the LEAF_END_MARKED macro to end them which
// creates a public Name_End label which can be used to figure out their size without
// having to create unwind info.
#define MARKED_FUNCTION_SIZE(pfn)    (size_t)((LPBYTE)GetEEFuncEntryPoint(pfn##_End) - (LPBYTE)GetEEFuncEntryPoint(pfn))

    switch (writeBarrier)
    {
        case WRITE_BARRIER_PREGROW32:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PreGrow32);
        case WRITE_BARRIER_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW32:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PostGrow32);
        case WRITE_BARRIER_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR32:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_SVR32);
        case WRITE_BARRIER_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_SVR64);
#endif
        case WRITE_BARRIER_BUFFER:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier);
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };    
#undef MARKED_FUNCTION_SIZE    
}

size_t WriteBarrierManager::GetCurrentWriteBarrierSize()
{
    return GetSpecificWriteBarrierSize(m_currentWriteBarrier);
}

PBYTE WriteBarrierManager::CalculatePatchLocation(LPVOID base, LPVOID label, int offset)
{
    // the label should always come after the entrypoint for this funtion
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (LPBYTE)label > (LPBYTE)base);

    return ((LPBYTE)GetEEFuncEntryPoint(JIT_WriteBarrier) + ((LPBYTE)GetEEFuncEntryPoint(label) - (LPBYTE)GetEEFuncEntryPoint(base) + offset));
}

void WriteBarrierManager::ChangeWriteBarrierTo(WriteBarrierType newWriteBarrier)
{  
    GCX_MAYBE_COOP_NO_THREAD_BROKEN((GetThread() != NULL));
    BOOL bEESuspended = FALSE;
    if(m_currentWriteBarrier != WRITE_BARRIER_UNINITIALIZED && !IsGCThread())
    {
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
        bEESuspended = TRUE;
    }    
 
    _ASSERTE(m_currentWriteBarrier != newWriteBarrier);
    m_currentWriteBarrier = newWriteBarrier;
 
    // the memcpy must come before the switch statment because the asserts inside the switch 
    // are actually looking into the JIT_WriteBarrier buffer
    memcpy((PVOID)JIT_WriteBarrier, (LPVOID)GetCurrentWriteBarrierCode(), GetCurrentWriteBarrierSize());
    
    switch (newWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW32:
        {
            m_pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_Lower, 3);
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_CardTable_Check, 2);
            m_pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow32, PatchLabel_CardTable_Update, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate2);
            break;
        }

        case WRITE_BARRIER_PREGROW64:
        {
            m_pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            break;
        }
        
        case WRITE_BARRIER_POSTGROW32:
        {
            m_pUpperBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_Upper, 3);
            m_pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_Lower, 3);
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_CheckCardTable, 2);
            m_pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow32, PatchLabel_UpdateCardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate2);
            break;
        }
        
        case WRITE_BARRIER_POSTGROW64:
        {
            m_pLowerBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR32:
        {
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR32, PatchLabel_CheckCardTable, 2);
            m_pCardTableImmediate2  = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR32, PatchLabel_UpdateCardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0 == *(DWORD*)m_pCardTableImmediate2);
            break;
        }

        case WRITE_BARRIER_SVR64:
        {
            m_pCardTableImmediate   = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
                        break;
        }
#endif

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
    }

    UpdateEphemeralBounds();        
    UpdateCardTableLocation(FALSE);

    if(bEESuspended)
    {
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }
}

#undef CALC_PATCH_LOCATION

void WriteBarrierManager::Initialize()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;


    // Ensure that the generic JIT_WriteBarrier function buffer is large enough to hold any of the more specific
    // write barrier implementations.
    size_t cbWriteBarrierBuffer = GetSpecificWriteBarrierSize(WRITE_BARRIER_BUFFER);

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_PREGROW32));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_PREGROW64));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_POSTGROW32));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_SVR32));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_SVR64));
#endif

#if !defined(CODECOVERAGE)
    Validate();
#endif
}

bool WriteBarrierManager::NeedDifferentWriteBarrier(BOOL bReqUpperBoundsCheck, WriteBarrierType* pNewWriteBarrierType)
{
    // Init code for the JIT_WriteBarrier assembly routine.  Since it will be bashed everytime the GC Heap 
    // changes size, we want to do most of the work just once.
    //
    // The actual JIT_WriteBarrier routine will only be called in free builds, but we keep this code (that 
    // modifies it) around in debug builds to check that it works (with assertions).


    WriteBarrierType writeBarrierType = m_currentWriteBarrier;

    for(;;)
    {
        switch (writeBarrierType)
        {
        case WRITE_BARRIER_UNINITIALIZED:
#ifdef _DEBUG
            // Use the default slow write barrier some of the time in debug builds because of of contains some good asserts
            if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) || DbgRandomOnExe(0.5)) {                
                break;
            }
#endif

            writeBarrierType = GCHeap::IsServerHeap() ? WRITE_BARRIER_SVR32 : WRITE_BARRIER_PREGROW32;
            continue;

        case WRITE_BARRIER_PREGROW32:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW32;
                continue;
            }

            if (!FitsInI4((size_t)g_card_table) || !FitsInI4((size_t)g_ephemeral_low))
            {
                writeBarrierType = WRITE_BARRIER_PREGROW64;
            }
            break;

        case WRITE_BARRIER_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_POSTGROW32:
            if (!FitsInI4((size_t)g_card_table) || !FitsInI4((size_t)g_ephemeral_low) || !FitsInI4((size_t)g_ephemeral_high))
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR32:
            if (!FitsInI4((size_t)g_card_table))
            {
                writeBarrierType = WRITE_BARRIER_SVR64;
            }
            break;

        case WRITE_BARRIER_SVR64:
            break;
#endif

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
        }
        break;
    }

    *pNewWriteBarrierType = writeBarrierType;
    return m_currentWriteBarrier != writeBarrierType;
}

void WriteBarrierManager::UpdateEphemeralBounds()
{
    bool needToFlushCache = false;

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(FALSE, &newType))
    {
        ChangeWriteBarrierTo(newType);
        return; 
    }

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return;
#endif

    switch (m_currentWriteBarrier)
    {

        case WRITE_BARRIER_POSTGROW32:
        {
            // Change immediate if different from new g_ephermeral_high.
            if (*(INT32*)m_pUpperBoundImmediate != (INT32)(size_t)g_ephemeral_high)
            {
                *(INT32*)m_pUpperBoundImmediate = (INT32)(size_t)g_ephemeral_high;
                needToFlushCache = true;
            }
        }
        //
        // INTENTIONAL FALL-THROUGH!
        //
        case WRITE_BARRIER_PREGROW32:
        {
            // Change immediate if different from new g_ephermeral_low.
            if (*(INT32*)m_pLowerBoundImmediate != (INT32)(size_t)g_ephemeral_low)
            {
                *(INT32*)m_pLowerBoundImmediate = (INT32)(size_t)g_ephemeral_low;
                needToFlushCache = true;
            }
            break;
        }

        case WRITE_BARRIER_POSTGROW64:
        {
            // Change immediate if different from new g_ephermeral_high.
            if (*(UINT64*)m_pUpperBoundImmediate != (size_t)g_ephemeral_high)
            {
                *(UINT64*)m_pUpperBoundImmediate = (size_t)g_ephemeral_high;
                needToFlushCache = true;
            }
        }
        //
        // INTENTIONAL FALL-THROUGH!
        //
        case WRITE_BARRIER_PREGROW64:
        {
            // Change immediate if different from new g_ephermeral_low.
            if (*(UINT64*)m_pLowerBoundImmediate != (size_t)g_ephemeral_low)
            {
                *(UINT64*)m_pLowerBoundImmediate = (size_t)g_ephemeral_low;
                needToFlushCache = true;
            }
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR32:
        case WRITE_BARRIER_SVR64:
        {
            break;
        }
#endif

        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier in UpdateEphemeralBounds");
    }

    if (needToFlushCache)
    {
        FlushInstructionCache(GetCurrentProcess(), (PVOID)JIT_WriteBarrier, GetCurrentWriteBarrierSize());
    }
}

void WriteBarrierManager::UpdateCardTableLocation(BOOL bReqUpperBoundsCheck)
{
    // If we are told that we require an upper bounds check (GC did some heap
    // reshuffling), we need to switch to the WriteBarrier_PostGrow function for
    // good.

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(bReqUpperBoundsCheck, &newType))
    {
        ChangeWriteBarrierTo(newType);
        return; 
    }

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return;
#endif

    bool fFlushCache = false;
    
    if (m_currentWriteBarrier == WRITE_BARRIER_PREGROW32 || 
        m_currentWriteBarrier == WRITE_BARRIER_POSTGROW32 ||
        m_currentWriteBarrier == WRITE_BARRIER_SVR32)
    {
        if (*(INT32*)m_pCardTableImmediate != (INT32)(size_t)g_card_table)
        {
            *(INT32*)m_pCardTableImmediate = (INT32)(size_t)g_card_table;
            *(INT32*)m_pCardTableImmediate2 = (INT32)(size_t)g_card_table;
            fFlushCache = true;
        }
    }
    else
    {
        if (*(UINT64*)m_pCardTableImmediate != (size_t)g_card_table)
        {
            *(UINT64*)m_pCardTableImmediate = (size_t)g_card_table;
            fFlushCache = true;
        }
    }

    if (fFlushCache)
    {
        FlushInstructionCache(GetCurrentProcess(), (LPVOID)JIT_WriteBarrier, GetCurrentWriteBarrierSize());
    }
}


// This function bashes the super fast amd64 version of the JIT_WriteBarrier
// helper.  It should be called by the GC whenever the ephermeral region 
// bounds get changed, but still remain on the top of the GC Heap. 
void StompWriteBarrierEphemeral()
{
    WRAPPER_NO_CONTRACT;

    g_WriteBarrierManager.UpdateEphemeralBounds();
}

// This function bashes the super fast amd64 versions of the JIT_WriteBarrier
// helpers.  It should be called by the GC whenever the ephermeral region gets moved
// from being at the top of the GC Heap, and/or when the cards table gets moved.
void StompWriteBarrierResize(BOOL bReqUpperBoundsCheck)
{
    WRAPPER_NO_CONTRACT;

    g_WriteBarrierManager.UpdateCardTableLocation(bReqUpperBoundsCheck);
}
