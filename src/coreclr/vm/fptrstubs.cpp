// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"
#include "fptrstubs.h"


// -------------------------------------------------------
// FuncPtr stubs
// -------------------------------------------------------

Precode* FuncPtrStubs::Lookup(MethodDesc * pMD, PrecodeType type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
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
    bool setTargetAfterAddingToHashTable = false;

    if (type != GetDefaultType(pMD) &&
        // Always use stable entrypoint for LCG. If the cached precode pointed directly to JITed code,
        // we would not be able to reuse it when the DynamicMethodDesc got reused for a new DynamicMethod.
        !pMD->IsLCGMethod())
    {
        // Set the target if precode is not of the default type. We are patching the precodes of the default type only.
        target = pMD->GetMultiCallableAddrOfCode();
    }
    else if (pMD->HasStableEntryPoint())
    {
        // Set target
        target = pMD->GetStableEntryPoint();
    }
    else if (pMD->IsVersionableWithVtableSlotBackpatch())
    {
        // The funcptr stub must point to the current entry point after it is created and exposed. Keep the target as null for
        // now. The precode will initially point to the prestub and its target will be updated after the precode is exposed.
        _ASSERTE(target == NULL);
        setTargetAfterAddingToHashTable = true;
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
        Precode* pNewPrecode = Precode::Allocate(type, pMD, pMD->GetLoaderAllocator(), &amt);

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
            else
            {
                setTargetAfterAddingToHashTable = false;
            }
        }
    }

    if (setTargetAfterAddingToHashTable)
    {
        GCX_PREEMP();

        _ASSERTE(pMD->IsVersionableWithVtableSlotBackpatch());

        PCODE temporaryEntryPoint = pMD->GetTemporaryEntryPoint();
        MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;

        // Set the funcptr stub's entry point to the current entry point inside the lock and after the funcptr stub is exposed,
        // to synchronize with backpatching in MethodDesc::BackpatchEntryPointSlots()
        PCODE entryPoint = pMD->GetMethodEntryPoint();
        if (entryPoint != temporaryEntryPoint)
        {
            // Need only patch the precode from the prestub, since if someone else managed to patch the precode already then its
            // target would already be up-to-date
            pPrecode->SetTargetInterlocked(entryPoint, TRUE /* fOnlyRedirectFromPrestub */);
        }
    }

    return pPrecode->GetEntryPoint();
}
#endif // DACCESS_COMPILE
