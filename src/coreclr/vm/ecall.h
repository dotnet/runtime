// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ECALL.H -
//
// Handles our private native calling interface.
//




#ifndef _ECALL_H_
#define _ECALL_H_

#include "fcall.h"

class MethodDesc;

// CoreCLR defines fewer FCalls so make the hashtable even smaller.
#define FCALL_HASH_SIZE 127

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
};

struct ECFunc {
    UINT_PTR            m_dwFlags;

    LPVOID              m_pImplementation;

    LPCSTR              m_szMethodName;
    LPHARDCODEDMETASIG  m_pMethodSig;       // Optional field. It is valid only if HasSignature() is set.

    bool                IsEndOfArray()  { LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_EndOfArray); }
    bool                HasSignature()  { LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_HasSignature); }
    bool                IsUnreferenced(){ LIMITED_METHOD_CONTRACT; return !!(m_dwFlags & FCFuncFlag_Unreferenced); }
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

        // Some fcalls (delegate ctors and tlbimpl ctors) shared one implementation.
        // We should never patch vtable for these since they have 1:N mapping between
        // MethodDesc and the actual implementation
        static BOOL IsSharedFCallImpl(PCODE pImpl);

        static BOOL CheckUnusedECalls(SetSHash<DWORD>& usedIDs);

        static void DynamicallyAssignFCallImpl(PCODE impl, DWORD index);

        static void PopulateManagedStringConstructors();

        static void PopulateManagedCastHelpers();

#ifdef DACCESS_COMPILE
        // Enumerates all gFCallMethods for minidumps.
        static void EnumFCallMethods();
#endif // DACCESS_COMPILE

#define _DYNAMICALLY_ASSIGNED_FCALLS_BASE() \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(FastAllocateString,                FramedAllocateString) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayStartLengthManaged,   NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharCountManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrManaged,                NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrStartLengthManaged,     NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorReadOnlySpanOfCharManaged,     NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrManaged,               NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrStartLengthManaged,    NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrStartLengthEncodingManaged, NULL) \

#define DYNAMICALLY_ASSIGNED_FCALLS() _DYNAMICALLY_ASSIGNED_FCALLS_BASE()

        enum
        {
            #undef DYNAMICALLY_ASSIGNED_FCALL_IMPL
            #define DYNAMICALLY_ASSIGNED_FCALL_IMPL(id,defaultimpl) id,

            DYNAMICALLY_ASSIGNED_FCALLS()

            NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS,
            InvalidDynamicFCallId = -1
        };
};

extern "C" FCDECL1(VOID, FCComCtor, LPVOID pV);

class GCReporting final
{
public:
    static FCDECL1(void, Register, GCFrame*);
    static FCDECL1(void, Unregister, GCFrame*);
};

#endif // _ECALL_H_
