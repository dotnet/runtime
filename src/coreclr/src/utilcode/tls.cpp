// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*  TLS.CPP:
 *

 *
 *  Encapsulates TLS access for maximum performance. 
 *
 */

#include "stdafx.h"

#include "unsafe.h"
#include "tls.h"
#include "contract.h"
#include "corerror.h"
#include "ex.h"
#include "clrhost.h"

#ifndef SELF_NO_HOST
#include "clrconfig.h"
#endif

#include "clrnt.h"

#ifndef SELF_NO_HOST

//---------------------------------------------------------------------------
// Win95 and WinNT store the TLS in different places relative to the
// fs:[0]. This api reveals which. Can also return TLSACCESS_GENERIC if
// no info is available about the Thread location (you have to use the TlsGetValue
// api.) This is intended for use by stub generators that want to inline TLS
// access.
//---------------------------------------------------------------------------
TLSACCESSMODE GetTLSAccessMode(DWORD tlsIndex)
{
    // Static contracts because this is used by contract infrastructure
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    TLSACCESSMODE tlsAccessMode = TLSACCESS_GENERIC;

#ifdef _DEBUG
    // Debug builds allow user to throw a switch to force use of the generic
    // (non-optimized) Thread/AppDomain getters.  Even if the user doesn't throw
    // the switch, force tests to go down the generic getter code path about 1% of the
    // time so it's exercised a couple dozen times during each devbvt run.
    if ((CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_UseGenericTlsGetters) != 0) || DbgRandomOnExe(.01))
        return TLSACCESS_GENERIC;
#endif

    if (tlsIndex < TLS_MINIMUM_AVAILABLE)
    {
        tlsAccessMode = TLSACCESS_WNT;
    }
    else
    if (tlsIndex < (TLS_MINIMUM_AVAILABLE + TLS_EXPANSION_SLOTS))
    {
        // Expansion slots are lazily created at the first call to
        // TlsGetValue on a thread, and the code we generate
        // assumes that the expansion slots will exist.
        //
        // <TODO> On newer flavors of NT we could use the vectored
        // exception handler to take the AV, call TlsGetValue, and
        // resume execution at the start of the getter. </TODO>
        tlsAccessMode = TLSACCESS_GENERIC;//TLSACCESS_WNT_HIGH;
    }
    else
    {
        //
        // If the app verifier is enabled, TLS indices
        // are faked to help detect invalid handle use.
        //
    }

    return tlsAccessMode;
}

//---------------------------------------------------------------------------
// Creates a platform-optimized version of TlsGetValue compiled
// for a particular index. Can return NULL.
//---------------------------------------------------------------------------
// A target for the optimized getter can be passed in, this is 
// useful so that code can avoid an indirect call for the GetThread
// and GetAppDomain calls for instance. If NULL is passed then
// we will allocate from the executeable heap.
POPTIMIZEDTLSGETTER MakeOptimizedTlsGetter(DWORD tlsIndex, LPVOID pBuffer, SIZE_T cbBuffer, POPTIMIZEDTLSGETTER pGenericImpl, BOOL fForceGeneric)
{
    // Static contracts because this is used by contract infrastructure
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    ARM_ONLY(pBuffer = ThumbCodeToDataPointer<BYTE*>(pBuffer));

    // Buffer that should be big enough to encode the TLS getter on any reasonable platform
    TADDR patch[4 INDEBUG(+4 /* last error trashing */)];

    PBYTE pPatch = (PBYTE)&patch;

    TLSACCESSMODE mode = fForceGeneric ? TLSACCESS_GENERIC : GetTLSAccessMode(tlsIndex);

#if defined(_DEBUG)
    if (mode != TLSACCESS_GENERIC)
    {
        //
        // Trash last error in debug builds
        //

#ifdef _TARGET_X86_
        *((DWORD*) (pPatch + 0))  = 0x05c764;    //  mov dword ptr fs:[offsetof(TEB, LastErrorValue)], LAST_ERROR_TRASH_VALUE
        *((DWORD*) (pPatch + 3))  = offsetof(TEB, LastErrorValue);
        *((DWORD*) (pPatch + 7))  = LAST_ERROR_TRASH_VALUE;
        pPatch += 11;
#endif // _TARGET_X86_

#ifdef _TARGET_AMD64_
        // iDNA doesn't like writing directly to gs:[nn]
        *((UINT64*)(pPatch + 0))  = 0x25048b4865;         //  mov rax, gs:[offsetof(TEB, NtTib.Self)]
        *((DWORD*) (pPatch + 5))  = offsetof(TEB, NtTib.Self);
        *((WORD*)  (pPatch + 9))  = 0x80c7;               //  mov dword ptr [rax + offsetof(TEB, LastErrorValue)], LAST_ERROR_TRASH_VALUE
        *((DWORD*) (pPatch + 11)) = offsetof(TEB, LastErrorValue);
        *((DWORD*) (pPatch + 15)) = LAST_ERROR_TRASH_VALUE;
        pPatch += 19;
#endif
    }
#endif // _DEBUG 

    switch (mode) 
    {
#ifdef _TARGET_X86_
        case TLSACCESS_WNT:
            *((WORD*)  (pPatch + 0)) = 0xa164;               //  mov  eax, fs:[IMM32]
            *((DWORD*) (pPatch + 2)) = offsetof(TEB, TlsSlots) + tlsIndex * sizeof(void*);
            *((BYTE*)  (pPatch + 6)) = 0xc3;                 //  retn
            pPatch += 7;
            break;

        case TLSACCESS_GENERIC:
            if (pGenericImpl == NULL)
                return NULL;

            _ASSERTE(pBuffer != NULL);
            *((BYTE*)   (pPatch + 0)) = 0xE9;        // jmp pGenericImpl
            TADDR rel32 = ((TADDR)pGenericImpl - ((TADDR)pBuffer + 1 + sizeof(INT32)));
            *((INT32*)  (pPatch + 1)) = (INT32)rel32;
            pPatch += 5;
            break;
#endif // _TARGET_X86_

#ifdef _TARGET_AMD64_
        case TLSACCESS_WNT:
            *((UINT64*)(pPatch + 0)) = 0x25048b4865; //  mov  rax, gs:[IMM32]
            *((DWORD*) (pPatch + 5)) = offsetof(TEB, TlsSlots) + (tlsIndex * sizeof(void*));
            *((BYTE*)  (pPatch + 9)) = 0xc3;         //  return                
            pPatch += 10;
            break;

        case TLSACCESS_GENERIC:
            if (pGenericImpl == NULL)
                return NULL;

            _ASSERTE(pBuffer != NULL);
            *((BYTE*)   (pPatch + 0)) = 0xE9;        // jmp pGenericImpl
            TADDR rel32 = ((TADDR)pGenericImpl - ((TADDR)pBuffer + 1 + sizeof(INT32)));
            _ASSERTE((INT64)(INT32)rel32 == (INT64)rel32);
            *((INT32*)  (pPatch + 1)) = (INT32)rel32;
            pPatch += 5;

            *pPatch++ = 0xCC; // Make sure there is full 8 bytes worth of data
            *pPatch++ = 0xCC;
            *pPatch++ = 0xCC;
            break;

#endif // _TARGET_AMD64_

#ifdef _TARGET_ARM_
        case TLSACCESS_WNT:
            {
                WORD slotOffset = (WORD)(offsetof(TEB, TlsSlots) + tlsIndex * sizeof(void*));
                _ASSERTE(slotOffset < 4096);

                WORD *pInstr = (WORD*)pPatch;

                *pInstr++ = 0xee1d;     // mrc p15, 0, r0, c13, c0, 2
                *pInstr++ = 0x0f50;
                *pInstr++ = 0xf8d0;     // ldr r0, [r0, #slotOffset]
                *pInstr++ = slotOffset;
                *pInstr++ = 0x4770;     // bx lr

                pPatch = (PBYTE)pInstr;
            }
            break;

        case TLSACCESS_GENERIC:
            {
                if (pGenericImpl == NULL)
                    return NULL;

                _ASSERTE(pBuffer != NULL);

                *(DWORD *)pPatch = 0x9000F000;  // b pGenericImpl
                PutThumb2BlRel24((WORD*)pPatch, (TADDR)pGenericImpl - ((TADDR)pBuffer + 4 + THUMB_CODE));

                pPatch += 4;
            }
            break;
#endif // _TARGET_ARM_
    }

    SIZE_T cbCode = (TADDR)pPatch - (TADDR)&patch;
    _ASSERTE(cbCode <= sizeof(patch));

    if (pBuffer != NULL)
    {
        _ASSERTE_ALL_BUILDS("clr/src/utilcode/tls.cpp", cbCode <= cbBuffer);

        // We assume that the first instruction of the buffer is a short jump to dummy helper 
        // that can be atomically overwritten to avoid races with other threads executing the code.
        // It is the same basic technique as hot patching.

        // Assert on all builds to make sure that retail optimizations are not affecting the alignment.
        _ASSERTE_ALL_BUILDS("clr/src/utilcode/tls.cpp", IS_ALIGNED((void*)pBuffer, sizeof(TADDR)));

        // Size of short jump that gets patched last.
        if (cbCode > sizeof(TADDR))
        {
            memcpy((BYTE *)pBuffer + sizeof(TADDR), &patch[1], cbCode - sizeof(TADDR));
            FlushInstructionCache(GetCurrentProcess(), (BYTE *)pBuffer + sizeof(TADDR), cbCode - sizeof(TADDR));
        }

        // Make sure that the the dummy implementation still works.
        _ASSERTE(((POPTIMIZEDTLSGETTER)ARM_ONLY(DataPointerToThumbCode<BYTE*>)(pBuffer))() == NULL);

        // It is important for this write to happen atomically     
        VolatileStore<TADDR>((TADDR *)pBuffer, patch[0]);

        FlushInstructionCache(GetCurrentProcess(), (BYTE *)pBuffer, sizeof(TADDR));
    }
    else
    {
        pBuffer = (BYTE*) new (executable, nothrow) BYTE[cbCode];
        if (pBuffer == NULL)
            return NULL;

        memcpy(pBuffer, &patch, cbCode);

        FlushInstructionCache(GetCurrentProcess(), pBuffer, cbCode);
    }

    return (POPTIMIZEDTLSGETTER)ARM_ONLY(DataPointerToThumbCode<BYTE*>)(pBuffer);
}


//---------------------------------------------------------------------------
// Frees a function created by MakeOptimizedTlsGetter().
//---------------------------------------------------------------------------
VOID FreeOptimizedTlsGetter(POPTIMIZEDTLSGETTER pOptimizedTlsGetter)
{
    // Static contracts because this is used by contract infrastructure
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    BYTE* pGetter = (BYTE*)pOptimizedTlsGetter;
#ifdef _TARGET_ARM_
    pGetter = ThumbCodeToDataPointer<BYTE*>(pGetter);
#endif
    DeleteExecutable(pGetter);
}

#endif  // !SELF_NO_HOST
