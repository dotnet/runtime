// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: methodimpl.cpp
//


//

//
// ============================================================================

#include "common.h"
#include "methodimpl.h"

DWORD MethodImpl::FindSlotIndex(DWORD slot)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(GetSlots()));
    } CONTRACTL_END;

    DWORD dwSize = GetSize();
    if(dwSize == 0) {
        return INVALID_INDEX;
    }

    // Simple binary search
    PTR_DWORD rgSlots = GetSlots();
    INT32     l       = 0;
    INT32     r       = dwSize - 1;
    INT32     pivot;

    while(1) {
        pivot =  (l + r) / 2;

        if(rgSlots[pivot] == slot) {
            break; // found it
        }
        else if(rgSlots[pivot] < slot) {
            l = pivot + 1;
        }
        else {
            r = pivot - 1;
        }

        if(l > r) {
            return INVALID_INDEX; // Not here
        }
    }

    CONSISTENCY_CHECK(pivot >= 0);
    return (DWORD)pivot;
}

PTR_MethodDesc MethodImpl::FindMethodDesc(DWORD slot, PTR_MethodDesc defaultReturn)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END

    DWORD slotIndex = FindSlotIndex(slot);
    if (slotIndex == INVALID_INDEX) {
        return defaultReturn;
    }

    PTR_MethodDesc result = pImplementedMD[slotIndex]; // The method descs are not offset by one

    // Prejitted images may leave NULL in this table if
    // the methoddesc is declared in another module.
    // In this case we need to manually compute & restore it
    // from the slot number.

    if (result == NULL)
#ifndef DACCESS_COMPILE 
        result = RestoreSlot(slotIndex, defaultReturn->GetMethodTable());
#else // DACCESS_COMPILE
        DacNotImpl();
#endif // DACCESS_COMPILE

    return result;
}

#ifndef DACCESS_COMPILE 

MethodDesc *MethodImpl::RestoreSlot(DWORD index, MethodTable *pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pdwSlots));
    }
    CONTRACTL_END

    MethodDesc *result;

    PREFIX_ASSUME(pdwSlots != NULL);
    DWORD slot = GetSlots()[index];

    // Since the overridden method is in a different module, we
    // are guaranteed that it is from a different class.  It is
    // either an override of a parent virtual method or parent-implemented
    // interface, or of an interface that this class has introduced.

    // In the former 2 cases, the slot number will be in the parent's
    // vtable section, and we can retrieve the implemented MethodDesc from
    // there.  In the latter case, we can search through our interface
    // map to determine which interface it is from.

    MethodTable *pParentMT = pMT->GetParentMethodTable();
    CONSISTENCY_CHECK(pParentMT != NULL && slot < pParentMT->GetNumVirtuals());
    {
        result = pParentMT->GetMethodDescForSlot(slot);
    }

    _ASSERTE(result != NULL);

    // Don't worry about races since we would all be setting the same result
    if (EnsureWritableExecutablePagesNoThrow(&pImplementedMD[index], sizeof(pImplementedMD[index])))
        pImplementedMD[index] = result;

    return result;
}

///////////////////////////////////////////////////////////////////////////////////////
void MethodImpl::SetSize(LoaderHeap *pHeap, AllocMemTracker *pamTracker, DWORD size)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(pdwSlots==NULL && pImplementedMD==NULL);
        INJECT_FAULT(ThrowOutOfMemory());
    } CONTRACTL_END;

    if(size > 0) {
        // An array of DWORDs, the first entry representing count, and the rest representing slot numbers
        S_SIZE_T cbCountAndSlots = S_SIZE_T(sizeof(DWORD)) +        // DWORD for the total count of slots
                                    S_SIZE_T(size) * S_SIZE_T(sizeof(DWORD)); // DWORD each for the slot numbers

        // MethodDesc* for each of the implemented methods
        S_SIZE_T cbMethodDescs = S_SIZE_T(size) * S_SIZE_T(sizeof(MethodDesc *));

        // Need to align-up the slot entries so that the MethodDesc* array starts on a pointer boundary.
        cbCountAndSlots.AlignUp(sizeof(MethodDesc*));
        S_SIZE_T cbTotal =  cbCountAndSlots + cbMethodDescs;
        if(cbCountAndSlots.IsOverflow())
            ThrowOutOfMemory();

        // Allocate the memory.
        LPBYTE pAllocData = (BYTE*)pamTracker->Track(pHeap->AllocMem(cbTotal));

        // Set the count and slot array
        pdwSlots = (DWORD*)pAllocData;

        // Set the MethodDesc* array. Make sure to adjust for alignment.
        pImplementedMD = (MethodDesc**)ALIGN_UP(pAllocData + cbCountAndSlots.Value(), sizeof(MethodDesc*));

        // Store the count in the first entry
        *pdwSlots = size;
    }
}

///////////////////////////////////////////////////////////////////////////////////////
void MethodImpl::SetData(DWORD* slots, MethodDesc** md)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(pdwSlots));
    } CONTRACTL_END;

    DWORD dwSize = *pdwSlots;
    memcpy(&(pdwSlots[1]), slots, dwSize*sizeof(DWORD));
    memcpy(pImplementedMD, md, dwSize*sizeof(MethodDesc*));
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
void MethodImpl::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    DWORD size = GetSize();
    _ASSERTE(size > 0);

    image->StoreStructure(pdwSlots, (size+1)*sizeof(DWORD),
                                    DataImage::ITEM_METHOD_DESC_COLD,
                                    sizeof(DWORD));
    image->StoreStructure(pImplementedMD, size*sizeof(MethodDesc*),
                                    DataImage::ITEM_METHOD_DESC_COLD,
                                    sizeof(MethodDesc*));
}

void MethodImpl::Fixup(DataImage *image, PVOID p, SSIZE_T offset)
{
    STANDARD_VM_CONTRACT;

    DWORD size = GetSize();
    _ASSERTE(size > 0);

    for (DWORD iMD = 0; iMD < size; iMD++)
    {
        // <TODO> Why not use FixupMethodDescPointer? </TODO>
        // <TODO> Does it matter if the MethodDesc needs a restore? </TODO>              

        MethodDesc * pMD = pImplementedMD[iMD];

        if (image->CanEagerBindToMethodDesc(pMD) &&
            image->CanHardBindToZapModule(pMD->GetLoaderModule()))
        {
            image->FixupPointerField(pImplementedMD, iMD * sizeof(MethodDesc *));
        }
        else
        {
            image->ZeroPointerField(pImplementedMD, iMD * sizeof(MethodDesc *));
        }
    }

    image->FixupPointerField(p, offset + offsetof(MethodImpl, pdwSlots));
    image->FixupPointerField(p, offset + offsetof(MethodImpl, pImplementedMD));
}

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif //!DACCESS_COMPILE

#ifdef DACCESS_COMPILE 

void
MethodImpl::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
#ifndef STUB_DISPATCH_ALL 
    CONSISTENCY_CHECK_MSG(FALSE, "Stub Dispatch forbidden code");
#else // STUB_DISPATCH_ALL
    // 'this' memory should already be enumerated as
    // part of the base MethodDesc.

    if (pdwSlots.IsValid() && GetSize())
    {
        ULONG32 numSlots = GetSize();
        DacEnumMemoryRegion(dac_cast<TADDR>(pdwSlots),
                            (numSlots + 1) * sizeof(DWORD));

        if (pImplementedMD.IsValid())
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(pImplementedMD),
                                numSlots * sizeof(PTR_MethodDesc));
            for (DWORD i = 0; i < numSlots; i++)
            {
                PTR_MethodDesc methodDesc = pImplementedMD[i];
                if (methodDesc.IsValid())
                {
                    methodDesc->EnumMemoryRegions(flags);
                }
            }
        }
    }
#endif // STUB_DISPATCH_ALL
}

#endif //DACCESS_COMPILE

#ifndef DACCESS_COMPILE 
MethodImpl::Iterator::Iterator(MethodDesc *pMD) : m_pMD(pMD), m_pImpl(NULL), m_iCur(0)
{
    LIMITED_METHOD_CONTRACT;
    if (pMD->IsMethodImpl())
    {
        m_pImpl = pMD->GetMethodImpl();
    }
}
#endif //!DACCESS_COMPILE

