//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#include "common.h"
#include "fptrstubs.h"

#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif

// -------------------------------------------------------
// FuncPtr stubs
// -------------------------------------------------------

Precode* FuncPtrStubs::Lookup(MethodDesc * pMD, PrecodeType type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    Precode* pPrecode = NULL;
    {
        CrstHolder ch(&m_hashTableCrst);
        pPrecode = m_hashTable.Lookup(PrecodeKey(pMD, type));
    }
    return pPrecode;
}


#ifndef DACCESS_COMPILE
//
// FuncPtrStubs
//

FuncPtrStubs::FuncPtrStubs()
    : m_hashTableCrst(CrstFuncPtrStubs, CRST_UNSAFE_ANYMODE)
{
    WRAPPER_NO_CONTRACT;
}

PrecodeType FuncPtrStubs::GetDefaultType(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT;

    PrecodeType type = PRECODE_STUB;
 
#ifdef HAS_FIXUP_PRECODE
    // Use the faster fixup precode if it is available
    type = PRECODE_FIXUP;
#endif // HAS_FIXUP_PRECODE

#ifdef HAS_REMOTING_PRECODE
    if (pMD->IsRemotingInterceptedViaVirtualDispatch())
    {
        type = PRECODE_REMOTING;
    }
#endif // HAS_REMOTING_PRECODE

    return type;
}

//
// Returns an existing stub, or creates a new one
//

PCODE FuncPtrStubs::GetFuncPtrStub(MethodDesc * pMD, PrecodeType type)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END

    Precode* pPrecode = NULL;
    {
        CrstHolder ch(&m_hashTableCrst);
        pPrecode = m_hashTable.Lookup(PrecodeKey(pMD, type));
    }

    if (pPrecode != NULL)
    {
        return pPrecode->GetEntryPoint();
    }

    PCODE target = NULL;

#ifdef FEATURE_REMOTING
    if (pMD->IsInterface() && !pMD->IsStatic())
    {
        // FuncPtrStubs on interface virtuals are used to transition
        // into the remoting system with the exact interface method.

        _ASSERTE(type == PRECODE_STUB);

        target = CRemotingServices::GetDispatchInterfaceHelper(pMD);
    }
    else
#endif // FEATURE_REMOTING
    if (type != GetDefaultType(pMD) &&
        // Always use stable entrypoint for LCG. If the cached precode pointed directly to JITed code,
        // we would not be able to reuse it when the DynamicMethodDesc got reused for a new DynamicMethod.
        !pMD->IsLCGMethod())
    {
        // Set the target if precode is not of the default type. We are patching the precodes of the default type only.
        target = pMD->GetMultiCallableAddrOfCode();
    }
    else
    if (pMD->HasStableEntryPoint())
    {
        // Set target
        target = pMD->GetStableEntryPoint();
    }
    else
    {
        // Set the target if method is methodimpled. We would not get to patch it otherwise.
        MethodDesc* pMDImpl = MethodTable::MapMethodDeclToMethodImpl(pMD);

        if (pMDImpl != pMD)
            target = pMDImpl->GetMultiCallableAddrOfCode();
    }

    //
    // We currently do not have a precode for this MethodDesc, so we will allocate one.
    // We allocate outside of the lock and then take the lock (m_hashTableCrst) and 
    // if we still do not have a precode we Add the one that we just allocated and 
    // call SuppressRelease to keep our allocation
    // If another thread beat us in adding the precode we don't call SuppressRelease
    // so the AllocMemTracker destructor will free the memory that we allocated
    // 
    {
        AllocMemTracker amt;
        Precode* pNewPrecode = Precode::Allocate(type, pMD, pMD->GetLoaderAllocatorForCode(), &amt);
   
        if (target != NULL)
        {
            pNewPrecode->SetTargetInterlocked(target);
        }

        {
            CrstHolder ch(&m_hashTableCrst);

            // Was an entry added in the meantime?
            // Is the entry still NULL?
            pPrecode = m_hashTable.Lookup(PrecodeKey(pMD, type));

            if (pPrecode == NULL)
            {
                // Use the one we allocated above
                pPrecode = pNewPrecode;
                m_hashTable.Add(pPrecode);
                amt.SuppressRelease();
            }
        }
    }

    return pPrecode->GetEntryPoint();
}
#endif // DACCESS_COMPILE
