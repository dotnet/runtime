// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: genericdict.cpp
//

//
// WARNING: Do NOT turn try to save dictionary slots except in the
// hardbind case.  Saving further dictionary slots can lead
// to ComputeNeedsRestore returning TRUE for the dictionary and
// the associated method table (though of course only if some
// entries in the dictionary are prepopulated).  However at
// earlier stages in the NGEN, code may have been compiled
// under the assumption that ComputeNeedsRestore was
// FALSE for the associated method table, and indeed this result
// may have been cached in the ComputeNeedsRestore
// for the MethodTable.  Thus the combination of populating
// the dictionary and saving further dictionary slots could lead
// to inconsistencies and unsoundnesses in compilation.
//

//
// ============================================================================

#include "common.h"
#include "genericdict.h"
#include "typestring.h"
#include "field.h"
#include "typectxt.h"
#include "virtualcallstub.h"
#include "sigbuilder.h"

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
//static
DictionaryLayout* DictionaryLayout::Allocate(WORD              numSlots,
                                             LoaderAllocator * pAllocator,
                                             AllocMemTracker * pamTracker)
{
    CONTRACT(DictionaryLayout*)
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pAllocator));
        PRECONDITION(numSlots > 0);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    S_SIZE_T bytes = S_SIZE_T(sizeof(DictionaryLayout)) + S_SIZE_T(sizeof(DictionaryEntryLayout)) * S_SIZE_T(numSlots-1);

    TaggedMemAllocPtr ptr = pAllocator->GetLowFrequencyHeap()->AllocMem(bytes);

    if (pamTracker != NULL)
        pamTracker->Track(ptr);

    DictionaryLayout * pD = (DictionaryLayout *)(void *)ptr;

    // This is the number of slots excluding the type parameters
    pD->m_numSlots = numSlots;
    pD->m_numInitialSlots = numSlots;

    RETURN pD;
}

#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Total number of bytes for a dictionary with the specified layout (including optional back pointer
// used by expanded dictionaries). The pSlotSize argument is used to return the size
// to be stored in the size slot of the dictionary (not including the optional back pointer).
//
//static
DWORD
DictionaryLayout::GetDictionarySizeFromLayout(
    DWORD                numGenericArgs,
    PTR_DictionaryLayout pDictLayout,
    DWORD*               pSlotSize)
{
    LIMITED_METHOD_DAC_CONTRACT;
    PRECONDITION(numGenericArgs > 0);
    PRECONDITION(CheckPointer(pDictLayout, NULL_OK));
    PRECONDITION(CheckPointer(pSlotSize));

    DWORD slotBytes = numGenericArgs * sizeof(TypeHandle); // Slots for instantiation arguments
    DWORD extraAllocBytes = 0;
    if (pDictLayout != NULL)
    {
        DWORD numSlots = VolatileLoadWithoutBarrier(&pDictLayout->m_numSlots);

        slotBytes += sizeof(TADDR);                        // Slot for dictionary size
        slotBytes += numSlots * sizeof(TADDR);             // Slots for dictionary slots based on a dictionary layout

        if (numSlots > pDictLayout->m_numInitialSlots)
        {
            extraAllocBytes = sizeof(PTR_Dictionary);      // Slot for the back-pointer in expanded dictionaries
        }
    }

    *pSlotSize = slotBytes;
    return slotBytes + extraAllocBytes;
}

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
// Find a token in the dictionary layout and return the offsets of indirections
// required to get to its slot in the actual dictionary
//
// NOTE: We will currently never return more than one indirection. We don't
// cascade dictionaries but we will record overflows in the dictionary layout
// (and cascade that accordingly) so we can prepopulate the overflow hash in
// reliability scenarios.
//
// Optimize the case of a token being !i (for class dictionaries) or !!i (for method dictionaries)
//
/* static */
BOOL DictionaryLayout::FindTokenWorker(LoaderAllocator*                 pAllocator,
                                       DWORD                            numGenericArgs,
                                       DictionaryLayout*                pDictLayout,
                                       SigBuilder*                      pSigBuilder,
                                       BYTE*                            pSig,
                                       DWORD                            cbSig,
                                       int                              nFirstOffset,
                                       DictionaryEntrySignatureSource   signatureSource,
                                       CORINFO_RUNTIME_LOOKUP*          pResult,
                                       WORD*                            pSlotOut,
                                       DWORD                            scanFromSlot /* = 0 */,
                                       BOOL                             useEmptySlotIfFound /* = FALSE */)

{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(numGenericArgs > 0);
        PRECONDITION(scanFromSlot >= 0 && scanFromSlot <= pDictLayout->m_numSlots);
        PRECONDITION(CheckPointer(pDictLayout));
        PRECONDITION(CheckPointer(pResult) && CheckPointer(pSlotOut));
        PRECONDITION(CheckPointer(pSig));
        PRECONDITION((pSigBuilder == NULL && cbSig == -1) || (CheckPointer(pSigBuilder) && cbSig > 0));
    }
    CONTRACTL_END

    // First slots contain the type parameters
    _ASSERTE(FitsIn<WORD>(numGenericArgs + 1 + scanFromSlot));
    WORD slot = static_cast<WORD>(numGenericArgs + 1 + scanFromSlot);

#if _DEBUG
    if (scanFromSlot > 0)
    {
        _ASSERT(useEmptySlotIfFound);

        for (DWORD iSlot = 0; iSlot < scanFromSlot; iSlot++)
        {
            // Verify that no entry before scanFromSlot matches the entry we're searching for
            BYTE* pCandidate = (BYTE*)pDictLayout->m_slots[iSlot].m_signature;
            if (pSigBuilder != NULL)
            {
                if (pDictLayout->m_slots[iSlot].m_signatureSource != FromReadyToRunImage)
                {
                    DWORD j;
                    for (j = 0; j < cbSig; j++)
                    {
                        if (pCandidate[j] != pSig[j])
                            break;
                    }
                    _ASSERT(j != cbSig);
                }
            }
            else
            {
                _ASSERT(pCandidate != pSig);
            }
        }
    }
#endif

    for (DWORD iSlot = scanFromSlot; iSlot < pDictLayout->m_numSlots; iSlot++)
    {
        BYTE* pCandidate = (BYTE*)pDictLayout->m_slots[iSlot].m_signature;
        if (pCandidate != NULL)
        {
            bool signaturesMatch = false;

            if (pSigBuilder != NULL)
            {
                // JIT case: compare signatures by comparing the bytes in them. We exclude
                // any ReadyToRun signatures from the JIT case.

                if (pDictLayout->m_slots[iSlot].m_signatureSource != FromReadyToRunImage)
                {
                    // Compare the signatures. We do not need to worry about the size of pCandidate.
                    // As long as we are comparing one byte at a time we are guaranteed to not overrun.
                    DWORD j;
                    for (j = 0; j < cbSig; j++)
                    {
                        if (pCandidate[j] != pSig[j])
                            break;
                    }
                    signaturesMatch = (j == cbSig);
                }
            }
            else
            {
                // ReadyToRun case: compare signatures by comparing their pointer values
                signaturesMatch = (pCandidate == pSig);
            }

            // We've found it
            if (signaturesMatch)
            {
                pResult->signature = pDictLayout->m_slots[iSlot].m_signature;

                _ASSERTE(FitsIn<WORD>(nFirstOffset + 1));
                pResult->indirections = static_cast<WORD>(nFirstOffset + 1);
                pResult->offsets[nFirstOffset] = slot * sizeof(DictionaryEntry);
                *pSlotOut = slot;
                return TRUE;
            }
        }
        // If we hit an empty slot then there's no more so use it
        else
        {
            if (!useEmptySlotIfFound)
            {
                *pSlotOut = static_cast<WORD>(iSlot);
                return FALSE;
            }

            // A lock should be taken by FindToken before being allowed to use an empty slot in the layout
            _ASSERT(SystemDomain::SystemModule()->m_DictionaryCrst.OwnedByCurrentThread());

            PVOID pResultSignature = pSigBuilder == NULL ? pSig : CreateSignatureWithSlotData(pSigBuilder, pAllocator, slot);
            pDictLayout->m_slots[iSlot].m_signature = pResultSignature;
            pDictLayout->m_slots[iSlot].m_signatureSource = signatureSource;

            pResult->signature = pDictLayout->m_slots[iSlot].m_signature;

            _ASSERTE(FitsIn<WORD>(nFirstOffset + 1));
            pResult->indirections = static_cast<WORD>(nFirstOffset + 1);
            pResult->offsets[nFirstOffset] = slot * sizeof(DictionaryEntry);
            *pSlotOut = slot;
            return TRUE;
        }

        slot++;
    }

    *pSlotOut = pDictLayout->m_numSlots;
    return FALSE;
}

/* static */
DictionaryLayout* DictionaryLayout::ExpandDictionaryLayout(LoaderAllocator*                 pAllocator,
                                                           DictionaryLayout*                pCurrentDictLayout,
                                                           DWORD                            numGenericArgs,
                                                           SigBuilder*                      pSigBuilder,
                                                           BYTE*                            pSig,
                                                           int                              nFirstOffset,
                                                           DictionaryEntrySignatureSource   signatureSource,
                                                           CORINFO_RUNTIME_LOOKUP*          pResult,
                                                           WORD*                            pSlotOut)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        INJECT_FAULT(ThrowOutOfMemory(););
        PRECONDITION(SystemDomain::SystemModule()->m_DictionaryCrst.OwnedByCurrentThread());
        PRECONDITION(CheckPointer(pResult) && CheckPointer(pSlotOut));
    }
    CONTRACTL_END

    // There shouldn't be any empty slots remaining in the current dictionary.
    _ASSERTE(pCurrentDictLayout->m_slots[pCurrentDictLayout->m_numSlots - 1].m_signature != NULL);

#ifdef _DEBUG
    // Stress debug mode by increasing size by only 1 slot for the first 10 slots.
    DWORD newSize = pCurrentDictLayout->m_numSlots > 10 ? (DWORD)pCurrentDictLayout->m_numSlots * 2 : pCurrentDictLayout->m_numSlots + 1;
    if (!FitsIn<WORD>(newSize))
        return NULL;
    DictionaryLayout* pNewDictionaryLayout = Allocate((WORD)newSize, pAllocator, NULL);
#else
    if (!FitsIn<WORD>((DWORD)pCurrentDictLayout->m_numSlots * 2))
        return NULL;
    DictionaryLayout* pNewDictionaryLayout = Allocate(pCurrentDictLayout->m_numSlots * 2, pAllocator, NULL);
#endif

    pNewDictionaryLayout->m_numInitialSlots = pCurrentDictLayout->m_numInitialSlots;

    for (DWORD iSlot = 0; iSlot < pCurrentDictLayout->m_numSlots; iSlot++)
        pNewDictionaryLayout->m_slots[iSlot] = pCurrentDictLayout->m_slots[iSlot];

    WORD layoutSlotIndex = pCurrentDictLayout->m_numSlots;
    WORD slot = static_cast<WORD>(numGenericArgs) + 1 + layoutSlotIndex;

    PVOID pResultSignature = pSigBuilder == NULL ? pSig : CreateSignatureWithSlotData(pSigBuilder, pAllocator, slot);
    pNewDictionaryLayout->m_slots[layoutSlotIndex].m_signature = pResultSignature;
    pNewDictionaryLayout->m_slots[layoutSlotIndex].m_signatureSource = signatureSource;

    pResult->signature = pNewDictionaryLayout->m_slots[layoutSlotIndex].m_signature;

    _ASSERTE(FitsIn<WORD>(nFirstOffset + 1));
    pResult->indirections = static_cast<WORD>(nFirstOffset + 1);
    pResult->offsets[nFirstOffset] = slot * sizeof(DictionaryEntry);
    *pSlotOut = slot;

    return pNewDictionaryLayout;
}

/* static */
BOOL DictionaryLayout::FindToken(MethodTable*                       pMT,
                                 LoaderAllocator*                   pAllocator,
                                 int                                nFirstOffset,
                                 SigBuilder*                        pSigBuilder,
                                 BYTE*                              pSig,
                                 DictionaryEntrySignatureSource     signatureSource,
                                 CORINFO_RUNTIME_LOOKUP*            pResult,
                                 WORD*                              pSlotOut)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pAllocator));
        PRECONDITION(CheckPointer(pResult));
        PRECONDITION(pMT->HasInstantiation());
    }
    CONTRACTL_END;

    DWORD cbSig = -1;
    pSig = pSigBuilder != NULL ? (BYTE*)pSigBuilder->GetSignature(&cbSig) : pSig;
    if (FindTokenWorker(pAllocator, pMT->GetNumGenericArgs(), pMT->GetClass()->GetDictionaryLayout(), pSigBuilder, pSig, cbSig, nFirstOffset, signatureSource, pResult, pSlotOut, 0, FALSE))
        return TRUE;

    CrstHolder ch(&SystemDomain::SystemModule()->m_DictionaryCrst);
    {
        // Try again under lock in case another thread already expanded the dictionaries or filled an empty slot
        if (FindTokenWorker(pMT->GetLoaderAllocator(), pMT->GetNumGenericArgs(), pMT->GetClass()->GetDictionaryLayout(), pSigBuilder, pSig, cbSig, nFirstOffset, signatureSource, pResult, pSlotOut, *pSlotOut, TRUE))
            return TRUE;

        DictionaryLayout* pOldLayout = pMT->GetClass()->GetDictionaryLayout();
        DictionaryLayout* pNewLayout = ExpandDictionaryLayout(pAllocator, pOldLayout, pMT->GetNumGenericArgs(), pSigBuilder, pSig, nFirstOffset, signatureSource, pResult, pSlotOut);
        if (pNewLayout == NULL)
        {
            pResult->signature = pSigBuilder == NULL ? pSig : CreateSignatureWithSlotData(pSigBuilder, pAllocator, 0);
            return FALSE;
        }

        // Update the dictionary layout pointer. Note that the expansion of the dictionaries of all instantiated types using this layout
        // is done lazily, whenever we attempt to access a slot that is beyond the size of the existing dictionary on that type.
        pMT->GetClass()->SetDictionaryLayout(pNewLayout);

        return TRUE;
    }
}

/* static */
BOOL DictionaryLayout::FindToken(MethodDesc*                        pMD,
                                 LoaderAllocator*                   pAllocator,
                                 int                                nFirstOffset,
                                 SigBuilder*                        pSigBuilder,
                                 BYTE*                              pSig,
                                 DictionaryEntrySignatureSource     signatureSource,
                                 CORINFO_RUNTIME_LOOKUP*            pResult,
                                 WORD*                              pSlotOut)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pAllocator));
        PRECONDITION(CheckPointer(pResult));
        PRECONDITION(pMD->HasMethodInstantiation());
    }
    CONTRACTL_END;

    DWORD cbSig = -1;
    pSig = pSigBuilder != NULL ? (BYTE*)pSigBuilder->GetSignature(&cbSig) : pSig;
    if (FindTokenWorker(pAllocator, pMD->GetNumGenericMethodArgs(), pMD->GetDictionaryLayout(), pSigBuilder, pSig, cbSig, nFirstOffset, signatureSource, pResult, pSlotOut, 0, FALSE))
        return TRUE;

    CrstHolder ch(&SystemDomain::SystemModule()->m_DictionaryCrst);
    {
        // Try again under lock in case another thread already expanded the dictionaries or filled an empty slot
        if (FindTokenWorker(pAllocator, pMD->GetNumGenericMethodArgs(), pMD->GetDictionaryLayout(), pSigBuilder, pSig, cbSig, nFirstOffset, signatureSource, pResult, pSlotOut, *pSlotOut, TRUE))
            return TRUE;

        DictionaryLayout* pOldLayout = pMD->GetDictionaryLayout();
        DictionaryLayout* pNewLayout = ExpandDictionaryLayout(pAllocator, pOldLayout, pMD->GetNumGenericMethodArgs(), pSigBuilder, pSig, nFirstOffset, signatureSource, pResult, pSlotOut);
        if (pNewLayout == NULL)
        {
            pResult->signature = pSigBuilder == NULL ? pSig : CreateSignatureWithSlotData(pSigBuilder, pAllocator, 0);
            return FALSE;
        }

        // Update the dictionary layout pointer. Note that the expansion of the dictionaries of all instantiated methods using this layout
        // is done lazily, whenever we attempt to access a slot that is beyond the size of the existing dictionary on that method.
        pMD->AsInstantiatedMethodDesc()->IMD_SetDictionaryLayout(pNewLayout);

        return TRUE;
    }
}

/* static */
PVOID DictionaryLayout::CreateSignatureWithSlotData(SigBuilder* pSigBuilder, LoaderAllocator* pAllocator, WORD slot)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pSigBuilder) && CheckPointer(pAllocator));
    }
    CONTRACTL_END

    pSigBuilder->AppendData(slot);

    DWORD cbNewSig;
    PVOID pNewSig = pSigBuilder->GetSignature(&cbNewSig);

    PVOID pResultSignature = pAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(cbNewSig));
    _ASSERT(pResultSignature != NULL);

    memcpy(pResultSignature, pNewSig, cbNewSig);

    return pResultSignature;
}


#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
DWORD DictionaryLayout::GetMaxSlots()
{
    LIMITED_METHOD_CONTRACT;
    return m_numSlots;
}

DWORD DictionaryLayout::GetNumInitialSlots()
{
    LIMITED_METHOD_CONTRACT;
    return m_numInitialSlots;
}

//---------------------------------------------------------------------------------------
//
DWORD
DictionaryLayout::GetNumUsedSlots()
{
    LIMITED_METHOD_CONTRACT;

    DWORD numUsedSlots = 0;
    for (DWORD i = 0; i < m_numSlots; i++)
    {
        if (GetEntryLayout(i)->m_signature != NULL)
            numUsedSlots++;
    }
    return numUsedSlots;
}


//---------------------------------------------------------------------------------------
//
DictionaryEntryKind
DictionaryEntryLayout::GetKind()
{
    STANDARD_VM_CONTRACT;

    if (m_signature == NULL)
        return EmptySlot;

    SigPointer ptr((PCCOR_SIGNATURE)dac_cast<TADDR>(m_signature));

    uint32_t kind; // DictionaryEntryKind
    IfFailThrow(ptr.GetData(&kind));

    return (DictionaryEntryKind)kind;
}

#ifndef DACCESS_COMPILE
Dictionary* Dictionary::GetMethodDictionaryWithSizeCheck(MethodDesc* pMD, ULONG slotIndex)
{
    CONTRACT(Dictionary*)
    {
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    DWORD numGenericArgs = pMD->GetNumGenericMethodArgs();

    Dictionary* pDictionary = pMD->GetMethodDictionary();
    DWORD currentDictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);

    if (currentDictionarySize <= (slotIndex * sizeof(DictionaryEntry)))
    {
        // Only expand the dictionary if the current slot we're trying to use is beyond the size of the dictionary

        // Take lock and check for size again, just in case another thread already resized the dictionary
        CrstHolder ch(&SystemDomain::SystemModule()->m_DictionaryCrst);

        pDictionary = pMD->GetMethodDictionary();
        currentDictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);

        if (currentDictionarySize <= (slotIndex * sizeof(DictionaryEntry)))
        {
            DictionaryLayout* pDictLayout = pMD->GetDictionaryLayout();
            InstantiatedMethodDesc* pIMD = pMD->AsInstantiatedMethodDesc();
            _ASSERTE(pDictLayout != NULL && pDictLayout->GetMaxSlots() > 0);

            DWORD expectedDictionarySlotSize;
            DWORD expectedDictionaryAllocSize = DictionaryLayout::GetDictionarySizeFromLayout(numGenericArgs, pDictLayout, &expectedDictionarySlotSize);
            _ASSERT(currentDictionarySize < expectedDictionarySlotSize);

            Dictionary* pNewDictionary = (Dictionary*)(void*)pIMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(expectedDictionaryAllocSize));

            // Copy old dictionary entry contents
            for (DWORD i = 0; i < currentDictionarySize / sizeof(DictionaryEntry); i++)
            {
                // Use VolatileLoadWithoutBarrier to ensure that the compiler won't turn this into memcpy that is not guaranteed to copy pointers atomically
                *((DictionaryEntry*)pNewDictionary + i) = VolatileLoadWithoutBarrier((DictionaryEntry*)pDictionary + i);
            }

            DWORD* pSizeSlot = (DWORD*)(pNewDictionary + numGenericArgs);
            *pSizeSlot = expectedDictionarySlotSize;
            *pNewDictionary->GetBackPointerSlot(numGenericArgs) = pDictionary;

            // Publish the new dictionary slots to the type.
            InterlockedExchangeT(&pIMD->m_pPerInstInfo, pNewDictionary);

            pDictionary = pNewDictionary;
        }
    }

    RETURN pDictionary;
}

Dictionary* Dictionary::GetTypeDictionaryWithSizeCheck(MethodTable* pMT, ULONG slotIndex)
{
    CONTRACT(Dictionary*)
    {
       THROWS;
       GC_TRIGGERS;
       POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    DWORD numGenericArgs = pMT->GetNumGenericArgs();

    Dictionary* pDictionary = pMT->GetDictionary();
    DWORD currentDictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);

    if (currentDictionarySize <= (slotIndex * sizeof(DictionaryEntry)))
    {
        // Only expand the dictionary if the current slot we're trying to use is beyond the size of the dictionary

        // Take lock and check for size again, just in case another thread already resized the dictionary
        CrstHolder ch(&SystemDomain::SystemModule()->m_DictionaryCrst);

        pDictionary = pMT->GetDictionary();
        currentDictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);

        if (currentDictionarySize <= (slotIndex * sizeof(DictionaryEntry)))
        {
            DictionaryLayout* pDictLayout = pMT->GetClass()->GetDictionaryLayout();
            _ASSERTE(pDictLayout != NULL && pDictLayout->GetMaxSlots() > 0);

            DWORD expectedDictionarySlotSize;
            DWORD expectedDictionaryAllocSize = DictionaryLayout::GetDictionarySizeFromLayout(numGenericArgs, pDictLayout, &expectedDictionarySlotSize);
            _ASSERT(currentDictionarySize < expectedDictionarySlotSize);

            // Expand type dictionary
            Dictionary* pNewDictionary = (Dictionary*)(void*)pMT->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(expectedDictionaryAllocSize));

            // Copy old dictionary entry contents
            for (DWORD i = 0; i < currentDictionarySize / sizeof(DictionaryEntry); i++)
            {
                // Use VolatileLoadWithoutBarrier to ensure that the compiler won't turn this into memcpy that is not guaranteed to copy pointers atomically
                *((DictionaryEntry*)pNewDictionary + i) = VolatileLoadWithoutBarrier((DictionaryEntry*)pDictionary + i);
            }

            DWORD* pSizeSlot = (DWORD*)(pNewDictionary + numGenericArgs);
            *pSizeSlot = expectedDictionarySlotSize;
            *pNewDictionary->GetBackPointerSlot(numGenericArgs) = pDictionary;

            // Publish the new dictionary slots to the type.
            ULONG dictionaryIndex = pMT->GetNumDicts() - 1;
            Dictionary** pPerInstInfo = pMT->GetPerInstInfo();
            InterlockedExchangeT(pPerInstInfo + dictionaryIndex, pNewDictionary);

            pDictionary = pNewDictionary;
        }
    }

    RETURN pDictionary;
}

//---------------------------------------------------------------------------------------
//
DictionaryEntry
Dictionary::PopulateEntry(
    MethodDesc *       pMD,
    MethodTable *      pMT,
    LPVOID             signature,
    BOOL               nonExpansive,
    DictionaryEntry ** ppSlot,
    DWORD              dictionaryIndexAndSlot, /* = -1 */
    Module *           pModule /* = NULL */)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    CORINFO_GENERIC_HANDLE result = NULL;
    *ppSlot = NULL;

    bool isReadyToRunModule = (pModule != NULL && pModule->IsReadyToRun());

    ZapSig::Context zapSigContext(NULL, NULL, ZapSig::NormalTokens);
    ZapSig::Context * pZapSigContext = NULL;

    uint32_t kind = DictionaryEntryKind::EmptySlot;

    SigPointer ptr((PCCOR_SIGNATURE)signature);

    if (isReadyToRunModule)
    {
        PCCOR_SIGNATURE pBlob = (PCCOR_SIGNATURE)signature;

        BYTE fixupKind = *pBlob++;

        ModuleBase * pInfoModule = pModule;
        if (fixupKind & ENCODE_MODULE_OVERRIDE)
        {
            DWORD moduleIndex = CorSigUncompressData(pBlob);
            pInfoModule = pModule->GetModuleFromIndex(moduleIndex);
            fixupKind &= ~ENCODE_MODULE_OVERRIDE;
        }

        _ASSERTE(fixupKind == ENCODE_DICTIONARY_LOOKUP_THISOBJ ||
                 fixupKind == ENCODE_DICTIONARY_LOOKUP_TYPE ||
                 fixupKind == ENCODE_DICTIONARY_LOOKUP_METHOD);

        if (fixupKind == ENCODE_DICTIONARY_LOOKUP_THISOBJ)
        {
            SigPointer p(pBlob);
            p.SkipExactlyOne();
            pBlob = p.GetPtr();
        }

        BYTE signatureKind = *pBlob++;
        if (signatureKind & ENCODE_MODULE_OVERRIDE)
        {
            DWORD moduleIndex = CorSigUncompressData(pBlob);
            ModuleBase * pSignatureModule = pModule->GetModuleFromIndex(moduleIndex);
            if (pInfoModule == pModule)
            {
                pInfoModule = pSignatureModule;
            }
            _ASSERTE(pInfoModule == pSignatureModule);
            signatureKind &= ~ENCODE_MODULE_OVERRIDE;
        }

        switch ((CORCOMPILE_FIXUP_BLOB_KIND) signatureKind)
        {
            case ENCODE_DECLARINGTYPE_HANDLE:   kind = DeclaringTypeHandleSlot; break;
            case ENCODE_TYPE_HANDLE:            kind = TypeHandleSlot; break;
            case ENCODE_FIELD_HANDLE:           kind = FieldDescSlot; break;
            case ENCODE_METHOD_HANDLE:          kind = MethodDescSlot; break;
            case ENCODE_METHOD_ENTRY:           kind = MethodEntrySlot; break;
            case ENCODE_VIRTUAL_ENTRY:          kind = DispatchStubAddrSlot; break;

            default:
                _ASSERTE(!"Unexpected CORCOMPILE_FIXUP_BLOB_KIND");
                ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        ptr = SigPointer(pBlob);

        zapSigContext = ZapSig::Context(pInfoModule, pModule, ZapSig::NormalTokens);
        pZapSigContext = &zapSigContext;
    }
    else
    {
        ptr = SigPointer((PCCOR_SIGNATURE)signature);
        IfFailThrow(ptr.GetData(&kind));

        pZapSigContext = NULL;
    }

    ModuleBase * pLookupModule = (isReadyToRunModule) ? pZapSigContext->pInfoModule : CoreLibBinder::GetModule();

    if (pMT != NULL)
    {
        // We need to normalize the class passed in (if any) for reliability purposes. That's because preparation of a code region that
        // contains these handle lookups depends on being able to predict exactly which lookups are required (so we can pre-cache the
        // answers and remove any possibility of failure at runtime). This is hard to do if the lookup (in this case the lookup of the
        // dictionary overflow cache) is keyed off the somewhat arbitrary type of the instance on which the call is made (we'd need to
        // prepare for every possible derived type of the type containing the method). So instead we have to locate the exactly
        // instantiated (non-shared) super-type of the class passed in.


        uint32_t dictionaryIndex = 0;

        if (isReadyToRunModule)
        {
            dictionaryIndex = dictionaryIndexAndSlot >> 16;
        }
        else
        {
            IfFailThrow(ptr.GetData(&dictionaryIndex));
        }

#if _DEBUG
        // Lock is needed because dictionary pointers can get updated during dictionary size expansion
        CrstHolder ch(&SystemDomain::SystemModule()->m_DictionaryCrst);

        // MethodTable is expected to be normalized
        Dictionary* pDictionary = pMT->GetDictionary();
        _ASSERTE(pDictionary == pMT->GetPerInstInfo()[dictionaryIndex]);
#endif
    }

    {
        SigTypeContext typeContext;

        if (pMT != NULL)
        {
            SigTypeContext::InitTypeContext(pMT, &typeContext);
        }
        else
        {
            SigTypeContext::InitTypeContext(pMD, &typeContext);
        }

        TypeHandle constraintType;
        TypeHandle declaringType;

        switch (kind)
        {
        case DeclaringTypeHandleSlot:
        {
            declaringType = ptr.GetTypeHandleThrowing(
                pLookupModule,
                &typeContext,
                (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                CLASS_LOADED,
                FALSE,
                NULL,
                pZapSigContext);
            if (declaringType.IsNull())
            {
                _ASSERTE(nonExpansive);
                return NULL;
            }
            IfFailThrow(ptr.SkipExactlyOne());

            FALLTHROUGH;
        }

        case TypeHandleSlot:
        {
            TypeHandle th = ptr.GetTypeHandleThrowing(
                pLookupModule,
                &typeContext,
                (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                CLASS_LOADED,
                FALSE,
                NULL,
                pZapSigContext);
            if (th.IsNull())
            {
                _ASSERTE(nonExpansive);
                return NULL;
            }
            IfFailThrow(ptr.SkipExactlyOne());

            if (!declaringType.IsNull())
            {
                th = th.GetMethodTable()->GetMethodTableMatchingParentClass(declaringType.AsMethodTable());
            }

            th.GetMethodTable()->EnsureInstanceActive();

            result = (CORINFO_GENERIC_HANDLE)th.AsPtr();
            break;
        }

        case ConstrainedMethodEntrySlot:
        {
            constraintType = ptr.GetTypeHandleThrowing(
                pLookupModule,
                &typeContext,
                (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                CLASS_LOADED,
                FALSE,
                NULL,
                pZapSigContext);
            if (constraintType.IsNull())
            {
                _ASSERTE(nonExpansive);
                return NULL;
            }
            IfFailThrow(ptr.SkipExactlyOne());

            FALLTHROUGH;
        }

        case MethodDescSlot:
        case DispatchStubAddrSlot:
        case MethodEntrySlot:
        {
            TypeHandle ownerType;
            MethodTable * pOwnerMT = NULL;
            MethodDesc * pMethod = NULL;

            uint32_t methodFlags = 0;
            BOOL isInstantiatingStub = 0;
            BOOL isUnboxingStub = 0;
            BOOL fMethodNeedsInstantiation = 0;

            uint32_t methodSlot = -1;
            BOOL fRequiresDispatchStub = 0;

            if (isReadyToRunModule)
            {
                IfFailThrow(ptr.GetData(&methodFlags));

                if (methodFlags & ENCODE_METHOD_SIG_Constrained)
                    kind = ConstrainedMethodEntrySlot;

                isInstantiatingStub = ((methodFlags & ENCODE_METHOD_SIG_InstantiatingStub) != 0) || (kind == MethodEntrySlot);
                isUnboxingStub = ((methodFlags & ENCODE_METHOD_SIG_UnboxingStub) != 0);
                fMethodNeedsInstantiation = ((methodFlags & ENCODE_METHOD_SIG_MethodInstantiation) != 0);

                if (methodFlags & ENCODE_METHOD_SIG_OwnerType)
                {
                    ownerType = ptr.GetTypeHandleThrowing(
                        pZapSigContext->pInfoModule,
                        &typeContext,
                        ClassLoader::LoadTypes,
                        CLASS_LOADED,
                        FALSE,
                        NULL,
                        pZapSigContext);

                    IfFailThrow(ptr.SkipExactlyOne());
                }

                if (methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken)
                {
                    // get the method desc using slot number
                    IfFailThrow(ptr.GetData(&methodSlot));

                    _ASSERTE(!ownerType.IsNull());
                    pMethod = ownerType.GetMethodTable()->GetMethodDescForSlot(methodSlot);
                }
                else
                {
                    //
                    // decode method token
                    //
                    RID rid;
                    IfFailThrow(ptr.GetData(&rid));

                    if (methodFlags & ENCODE_METHOD_SIG_MemberRefToken)
                    {
                        if (ownerType.IsNull())
                        {
                            FieldDesc * pFDDummy = NULL;

                            MemberLoader::GetDescFromMemberRef(pZapSigContext->pInfoModule, TokenFromRid(rid, mdtMemberRef), &pMethod, &pFDDummy, NULL, FALSE, &ownerType);
                            _ASSERTE(pMethod != NULL && pFDDummy == NULL);
                        }
                        else
                        {
                            pMethod = MemberLoader::GetMethodDescFromMemberRefAndType(pZapSigContext->pInfoModule, TokenFromRid(rid, mdtMemberRef), ownerType.GetMethodTable());
                        }
                    }
                    else
                    {
                        _ASSERTE(pZapSigContext->pInfoModule->IsFullModule());
                        pMethod = MemberLoader::GetMethodDescFromMethodDef(static_cast<Module*>(pZapSigContext->pInfoModule), TokenFromRid(rid, mdtMethodDef), FALSE);
                    }
                }

                if (ownerType.IsNull())
                    ownerType = pMethod->GetMethodTable();

                _ASSERT(!ownerType.IsNull() && !nonExpansive);
                pOwnerMT = ownerType.GetMethodTable();

                if (kind == DispatchStubAddrSlot && pMethod->IsVtableMethod())
                {
                    fRequiresDispatchStub = TRUE;
                    methodSlot = pMethod->GetSlot();
                }
            }
            else
            {
                ownerType = ptr.GetTypeHandleThrowing(
                    pLookupModule,
                    &typeContext,
                    (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                    CLASS_LOADED,
                    FALSE,
                    NULL,
                    pZapSigContext);
                if (ownerType.IsNull())
                {
                    _ASSERTE(nonExpansive);
                    return NULL;
                }
                IfFailThrow(ptr.SkipExactlyOne());

                // <NICE> wsperf: Create a path that doesn't load types or create new handles if nonExpansive is set </NICE>
                if (nonExpansive)
                    return NULL;

                pOwnerMT = ownerType.GetMethodTable();
                _ASSERTE(pOwnerMT != NULL);

                IfFailThrow(ptr.GetData(&methodFlags));

                isInstantiatingStub = ((methodFlags & ENCODE_METHOD_SIG_InstantiatingStub) != 0);
                isUnboxingStub = ((methodFlags & ENCODE_METHOD_SIG_UnboxingStub) != 0);
                fMethodNeedsInstantiation = ((methodFlags & ENCODE_METHOD_SIG_MethodInstantiation) != 0);

                if ((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) != 0)
                {
                    // get the method desc using slot number
                    IfFailThrow(ptr.GetData(&methodSlot));

                    if (kind == DispatchStubAddrSlot)
                    {
                        fRequiresDispatchStub = TRUE;
                    }

                    if (!fRequiresDispatchStub)
                        pMethod = pOwnerMT->GetMethodDescForSlot(methodSlot);
                }
                else
                {
                    // Decode type where the method token is defined
                    TypeHandle thMethodDefType = ptr.GetTypeHandleThrowing(
                        pLookupModule,
                        &typeContext,
                        (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                        CLASS_LOADED,
                        FALSE,
                        NULL,
                        pZapSigContext);
                    if (thMethodDefType.IsNull())
                    {
                        _ASSERTE(nonExpansive);
                        return NULL;
                    }
                    IfFailThrow(ptr.SkipExactlyOne());
                    MethodTable * pMethodDefMT = thMethodDefType.GetMethodTable();
                    _ASSERTE(pMethodDefMT != NULL);

                    // decode method token
                    RID rid;
                    IfFailThrow(ptr.GetData(&rid));
                    mdMethodDef token = TokenFromRid(rid, mdtMethodDef);

                    // The RID map should have been filled out if we fully loaded the class
                    pMethod = pMethodDefMT->GetModule()->LookupMethodDef(token);
                    _ASSERTE(pMethod != NULL);
                    pMethod->CheckRestore();
                }
            }

            if (fRequiresDispatchStub)
            {
                // Generate a dispatch stub and store it in the dictionary.
                //
                // We generate an indirection so we don't have to write to the dictionary
                // when we do updates, and to simplify stub indirect callsites.  Stubs stored in
                // dictionaries use "RegisterIndirect" stub calling, e.g. "call [eax]",
                // i.e. here the register "eax" would contain the value fetched from the dictionary,
                // which in turn points to the stub indirection which holds the value the current stub
                // address itself. If we just used "call eax" then we wouldn't know which stub indirection
                // to update.  If we really wanted to avoid the extra indirection we could return the _address_ of the
                // dictionary entry to the  caller, still using "call [eax]", and then the
                // stub dispatch mechanism can update the dictitonary itself and we don't
                // need an indirection.
                LoaderAllocator * pDictLoaderAllocator = (pMT != NULL) ? pMT->GetLoaderAllocator() : pMD->GetLoaderAllocator();

                VirtualCallStubManager * pMgr = pDictLoaderAllocator->GetVirtualCallStubManager();

                // We indirect through a cell so that updates can take place atomically.
                // The call stub and the indirection cell have the same lifetime as the dictionary itself, i.e.
                // are allocated in the domain of the dicitonary.
                //
                // In the case of overflow (where there is no dictionary, just a global hash table) then
                // the entry will be placed in the overflow hash table (JitGenericHandleCache).  This
                // is partitioned according to domain, i.e. is scraped each time an AppDomain gets unloaded.
                PCODE addr = pMgr->GetCallStub(ownerType, methodSlot);

                result = (CORINFO_GENERIC_HANDLE)pMgr->GenerateStubIndirection(addr);
                break;
            }

            Instantiation inst;

            // Instantiate the method if needed, or create a stub to a static method in a generic class.
            if (fMethodNeedsInstantiation)
            {
                uint32_t nargs;
                IfFailThrow(ptr.GetData(&nargs));

                SIZE_T cbMem;

                if (!ClrSafeInt<SIZE_T>::multiply(nargs, sizeof(TypeHandle), cbMem/* passed by ref */))
                    ThrowHR(COR_E_OVERFLOW);

                TypeHandle * pInst = (TypeHandle*)_alloca(cbMem);
                for (uint32_t i = 0; i < nargs; i++)
                {
                    pInst[i] = ptr.GetTypeHandleThrowing(
                        pLookupModule,
                        &typeContext,
                        ClassLoader::LoadTypes,
                        CLASS_LOADED,
                        FALSE,
                        NULL,
                        pZapSigContext);
                    IfFailThrow(ptr.SkipExactlyOne());
                }

                inst = Instantiation(pInst, nargs);
            }
            else
            {
                inst = pMethod->GetMethodInstantiation();
            }

            // This must be called even if nargs == 0, in order to create an instantiating
            // stub for static methods in generic classees if needed, also for BoxedEntryPointStubs
            // in non-generic structs.
            pMethod = MethodDesc::FindOrCreateAssociatedMethodDesc(
                pMethod,
                pOwnerMT,
                isUnboxingStub,
                inst,
                (!isInstantiatingStub && !isUnboxingStub));

            if (kind == ConstrainedMethodEntrySlot)
            {
                if (isReadyToRunModule)
                {
                    _ASSERTE((methodFlags & ENCODE_METHOD_SIG_Constrained) == ENCODE_METHOD_SIG_Constrained);

                    constraintType = ptr.GetTypeHandleThrowing(
                        pZapSigContext->pInfoModule,
                        &typeContext,
                        ClassLoader::LoadTypes,
                        CLASS_LOADED,
                        FALSE,
                        NULL,
                        pZapSigContext);
                }
                _ASSERTE(!constraintType.IsNull());

                MethodDesc *pResolvedMD = constraintType.GetMethodTable()->TryResolveConstraintMethodApprox(ownerType, pMethod);

                // All such calls should be resolvable.  If not then for now just throw an error.
                _ASSERTE(pResolvedMD);
                INDEBUG(if (!pResolvedMD) constraintType.GetMethodTable()->TryResolveConstraintMethodApprox(ownerType, pMethod);)
                if (!pResolvedMD)
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

#if FEATURE_DEFAULT_INTERFACES
                // If we resolved the constrained call on a value type into a method on a reference type, this is a
                // default interface method implementation.
                // If the method is a static method, this is ok, but for instance methods there are boxing issues.
                // In such case we would need to box the value type before we can dispatch to the implementation.
                // This would require us to make a "boxing stub". For now we leave the boxing stubs unimplemented.
                // It's not clear if anyone would need them and the implementation complexity is not worth it at this time.
                if (!pResolvedMD->IsStatic() && !pResolvedMD->GetMethodTable()->IsValueType() && constraintType.GetMethodTable()->IsValueType())
                {
                    SString assemblyName;

                    constraintType.GetMethodTable()->GetAssembly()->GetDisplayName(assemblyName);

                    SString strInterfaceName;
                    TypeString::AppendType(strInterfaceName, ownerType);

                    SString strMethodName;
                    TypeString::AppendMethod(strMethodName, pMethod, pMethod->GetMethodInstantiation());

                    SString strTargetClassName;
                    TypeString::AppendType(strTargetClassName, constraintType.GetMethodTable());

                    COMPlusThrow(
                        kNotSupportedException,
                        IDS_CLASSLOAD_UNSUPPORTED_DISPATCH,
                        strMethodName,
                        strInterfaceName,
                        strTargetClassName,
                        assemblyName);
                }
#endif

                result = (CORINFO_GENERIC_HANDLE)pResolvedMD->GetMultiCallableAddrOfCode();
            }
            else
            if (kind == MethodEntrySlot)
            {
                result = (CORINFO_GENERIC_HANDLE)pMethod->GetMultiCallableAddrOfCode();
            }
            else
            if (kind == DispatchStubAddrSlot)
            {
                _ASSERTE((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0);
                PCODE *ppCode = (PCODE*)(void*)pMethod->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(PCODE)));
                *ppCode = pMethod->GetMultiCallableAddrOfCode();
                result = (CORINFO_GENERIC_HANDLE)ppCode;
            }
            else
            {
                _ASSERTE(kind == MethodDescSlot);
                result = (CORINFO_GENERIC_HANDLE)pMethod;
            }
            break;
        }

        case FieldDescSlot:
        {
            TypeHandle ownerType;

            if (isReadyToRunModule)
            {
                FieldDesc* pField = ZapSig::DecodeField((Module*)pZapSigContext->pModuleContext, pZapSigContext->pInfoModule, ptr.GetPtr(), &typeContext, &ownerType);
                _ASSERTE(!ownerType.IsNull());

                ownerType.AsMethodTable()->EnsureInstanceActive();

                result = (CORINFO_GENERIC_HANDLE)pField;
            }
            else
            {
                ownerType = ptr.GetTypeHandleThrowing(
                    pLookupModule,
                    &typeContext,
                    (nonExpansive ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes),
                    CLASS_LOADED,
                    FALSE,
                    NULL,
                    pZapSigContext);
                if (ownerType.IsNull())
                {
                    _ASSERTE(nonExpansive);
                    return NULL;
                }
                IfFailThrow(ptr.SkipExactlyOne());

                // Computed by MethodTable::GetIndexForFieldDesc().
                uint32_t fieldIndex;
                IfFailThrow(ptr.GetData(&fieldIndex));

                ownerType.AsMethodTable()->EnsureInstanceActive();

                result = (CORINFO_GENERIC_HANDLE)ownerType.AsMethodTable()->GetFieldDescByIndex(fieldIndex);
            }
            break;
        }

        default:
            _ASSERTE(!"Invalid DictionaryEntryKind");
            break;
        }

        uint32_t slotIndex;
        if (isReadyToRunModule)
        {
            _ASSERT(dictionaryIndexAndSlot != (uint32_t)-1);
            slotIndex = (uint32_t)(dictionaryIndexAndSlot & 0xFFFF);
        }
        else
        {
            IfFailThrow(ptr.GetData(&slotIndex));
        }

        MemoryBarrier();

        if (slotIndex != 0)
        {
            Dictionary* pDictionary;
            DWORD numGenericArgs;
            DictionaryLayout * pDictLayout;
            if (pMT != NULL)
            {
                pDictionary = GetTypeDictionaryWithSizeCheck(pMT, slotIndex);
                numGenericArgs = pMT->GetNumGenericArgs();
                pDictLayout = pMT->GetClass()->GetDictionaryLayout();
            }
            else
            {
                pDictionary = GetMethodDictionaryWithSizeCheck(pMD, slotIndex);
                numGenericArgs = pMD->GetNumGenericMethodArgs();
                pDictLayout = pMD->GetDictionaryLayout();
            }
            DWORD minimumSizeOfDictionaryToPatch = (slotIndex + 1) * sizeof(DictionaryEntry *);
            DWORD sizeOfInitialDictionary = (numGenericArgs + 1 + pDictLayout->GetNumInitialSlots()) * sizeof(DictionaryEntry *);

            DictionaryEntry *slot = pDictionary->GetSlotAddr(0, slotIndex);
            VolatileStoreWithoutBarrier(slot, (DictionaryEntry)result);
            *ppSlot = slot;

            // Backpatch previous versions of the generic dictionary
            DWORD dictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);
            while (dictionarySize > sizeOfInitialDictionary)
            {
                pDictionary = *pDictionary->GetBackPointerSlot(numGenericArgs);
                if (pDictionary == nullptr)
                {
                    // Initial dictionary allocated with higher number of slots than the initial layout slot count
                    break;
                }
                dictionarySize = pDictionary->GetDictionarySlotsSize(numGenericArgs);
                if (dictionarySize < minimumSizeOfDictionaryToPatch)
                {
                    // Previous dictionary is too short to patch, end iteration
                    break;
                }
                VolatileStoreWithoutBarrier(pDictionary->GetSlotAddr(0, slotIndex), (DictionaryEntry)result);
            }
        }
    }

    return result;
} // Dictionary::PopulateEntry

#endif //!DACCESS_COMPILE
