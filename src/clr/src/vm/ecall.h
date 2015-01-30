//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ECALL.H -
//
// Handles our private native calling interface.
//




#ifndef _ECALL_H_
#define _ECALL_H_

#include "fcall.h"

class MethodDesc;

#ifndef FEATURE_CORECLR
// Every program tends to use only a subset of ~1000 FCalls. Even big apps like 
// VS do not usually hit more than 300. Pick a size of the hashtable that's sufficient
// for the typical case. It is ok to have some colisions in the rare case. Note that 
// the size of the table should be prime.
#define FCALL_HASH_SIZE 257
#else
// CoreCLR defines fewer FCalls so make the hashtable even smaller.
#define FCALL_HASH_SIZE 127
#endif

typedef DPTR(struct ECHash) PTR_ECHash;

struct ECHash
{
    PTR_ECHash          m_pNext;
    PCODE               m_pImplementation;
    PTR_MethodDesc      m_pMD;               // for reverse mapping
};

#ifdef DACCESS_COMPILE
GVAL_DECL(TADDR, gLowestFCall);
GVAL_DECL(TADDR, gHighestFCall);
GARY_DECL(PTR_ECHash, gFCallMethods, FCALL_HASH_SIZE);
#endif

enum {
    FCFuncFlag_EndOfArray   = 0x01,
    FCFuncFlag_HasSignature = 0x02,
    FCFuncFlag_Unreferenced = 0x04, // Suppress unused fcall check
    FCFuncFlag_QCall        = 0x08, // QCall - mscorlib.dll to mscorwks.dll transition implemented as PInvoke
};

struct ECFunc {
    UINT_PTR            m_dwFlags;

    LPVOID              m_pImplementation;

    LPCSTR              m_szMethodName;
    LPHARDCODEDMETASIG  m_pMethodSig;       // Optional field. It is valid only if HasSignature() is set.

    bool                IsEndOfArray()  { LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_EndOfArray); }
    bool                HasSignature()  { LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_HasSignature); }
    bool                IsUnreferenced(){ LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_Unreferenced); }
    bool                IsQCall()       { LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_QCall); }
    CorInfoIntrinsics   IntrinsicID()   { LIMITED_METHOD_CONTRACT; return (CorInfoIntrinsics)((INT8)(m_dwFlags >> 16)); }
    int                 DynamicID()     { LIMITED_METHOD_CONTRACT; return (int)              ((INT8)(m_dwFlags >> 24)); }

    ECFunc*             NextInArray()
    { 
        LIMITED_METHOD_CONTRACT; 
        
        return (ECFunc*)((BYTE*)this + 
            (HasSignature() ? sizeof(ECFunc) : offsetof(ECFunc, m_pMethodSig)));
    }
};

struct ECClass
{
    LPCSTR      m_szClassName;
    LPCSTR      m_szNameSpace;
    const LPVOID *  m_pECFunc;
};

//=======================================================================
// Collects code and data pertaining to the ECall interface.
//=======================================================================
class ECall
{
    public:
        //---------------------------------------------------------
        // One-time init
        //---------------------------------------------------------
        static void Init();

        static PCODE GetFCallImpl(MethodDesc* pMD, BOOL * pfSharedOrDynamicFCallImpl = NULL);
        static MethodDesc* MapTargetBackToMethod(PCODE pTarg, PCODE * ppAdjustedEntryPoint = NULL);
        static DWORD GetIDForMethod(MethodDesc *pMD);
        static CorInfoIntrinsics GetIntrinsicID(MethodDesc *pMD);

        // Some fcalls (delegate ctors and tlbimpl ctors) shared one implementation.
        // We should never patch vtable for these since they have 1:N mapping between
        // MethodDesc and the actual implementation
        static BOOL IsSharedFCallImpl(PCODE pImpl);

        static BOOL CheckUnusedECalls(SetSHash<DWORD>& usedIDs);

        static void DynamicallyAssignFCallImpl(PCODE impl, DWORD index);

        static void PopulateManagedStringConstructors();
#ifdef DACCESS_COMPILE
        // Enumerates all gFCallMethods for minidumps.
        static void EnumFCallMethods();
#endif // DACCESS_COMPILE

#define DYNAMICALLY_ASSIGNED_FCALLS() \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(FastAllocateString,                FramedAllocateString) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayStartLengthManaged,   NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharCountManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrManaged,                NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrStartLengthManaged,     NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(InternalGetCurrentThread,          NULL) \

        enum
        {
            #undef DYNAMICALLY_ASSIGNED_FCALL_IMPL
            #define DYNAMICALLY_ASSIGNED_FCALL_IMPL(id,defaultimpl) id,

            DYNAMICALLY_ASSIGNED_FCALLS()

            NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS,
            InvalidDynamicFCallId = -1
        };


        static LPVOID GetQCallImpl(MethodDesc * pMD);
};

#ifdef FEATURE_COMINTEROP
extern "C" FCDECL1(VOID, FCComCtor, LPVOID pV);
#endif

#endif // _ECALL_H_
