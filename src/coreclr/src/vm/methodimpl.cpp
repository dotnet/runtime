// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    return GetMethodDesc(slotIndex, defaultReturn);
}

PTR_MethodDesc MethodImpl::GetMethodDesc(DWORD slotIndex, PTR_MethodDesc defaultReturn)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END

    DPTR(RelativePointer<PTR_MethodDesc>) pRelPtrForSlot = GetImpMDsNonNull();
    // The method descs are not offset by one
    TADDR base = dac_cast<TADDR>(pRelPtrForSlot) + slotIndex * sizeof(RelativePointer<MethodDesc *>);
    PTR_MethodDesc result = RelativePointer<PTR_MethodDesc>::GetValueMaybeNullAtPtr(base);

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
        PRECONDITION(!pdwSlots.IsNull());
    }
    CONTRACTL_END

    MethodDesc *result;

    PREFIX_ASSUME(!pdwSlots.IsNull());
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

    pImplementedMD.GetValue()[index].SetValue(result);

    return result;
}

///////////////////////////////////////////////////////////////////////////////////////
void MethodImpl::SetSize(LoaderHeap *pHeap, AllocMemTracker *pamTracker, DWORD size)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(pdwSlots.GetValueMaybeNull()==NULL && pImplementedMD.GetValueMaybeNull()==NULL);
        INJECT_FAULT(ThrowOutOfMemory());
    } CONTRACTL_END;

    if(size > 0) {
        // An array of DWORDs, the first entry representing count, and the rest representing slot numbers
        S_SIZE_T cbCountAndSlots = S_SIZE_T(sizeof(DWORD)) +        // DWORD for the total count of slots
                                    S_SIZE_T(size) * S_SIZE_T(sizeof(DWORD)) + // DWORD each for the slot numbers
                                    S_SIZE_T(size) * S_SIZE_T(sizeof(mdToken)); // Token each for the method tokens

        // MethodDesc* for each of the implemented methods
        S_SIZE_T cbMethodDescs = S_SIZE_T(size) * S_SIZE_T(sizeof(RelativePointer<MethodDesc *>));

        // Need to align-up the slot entries so that the MethodDesc* array starts on a pointer boundary.
        cbCountAndSlots.AlignUp(sizeof(MethodDesc*));
        S_SIZE_T cbTotal =  cbCountAndSlots + cbMethodDescs;
        if(cbCountAndSlots.IsOverflow())
            ThrowOutOfMemory();

        // Allocate the memory.
        LPBYTE pAllocData = (BYTE*)pamTracker->Track(pHeap->AllocMem(cbTotal));

        // Set the count and slot array
        pdwSlots.SetValue((DWORD*)pAllocData);

        // Set the MethodDesc* array. Make sure to adjust for alignment.
        pImplementedMD.SetValue((RelativePointer<MethodDesc*> *)ALIGN_UP(pAllocData + cbCountAndSlots.Value(), sizeof(RelativePointer <MethodDesc*>)));

        // Store the count in the first entry
        *pdwSlots.GetValue() = size;
    }
}

///////////////////////////////////////////////////////////////////////////////////////
void MethodImpl::SetData(DWORD* slots, mdToken* tokens, RelativePointer<MethodDesc*>* md)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(!pdwSlots.IsNull());
    } CONTRACTL_END;

    DWORD *pdwSize = pdwSlots.GetValue();
    DWORD dwSize = *pdwSize;
    memcpy(&(pdwSize[1]), slots, dwSize*sizeof(DWORD));

    // Copy tokens that correspond to the slots above
    memcpy(&(pdwSize[1 + dwSize]), tokens, dwSize*sizeof(mdToken));

    RelativePointer<MethodDesc *> *pImplMD = pImplementedMD.GetValue();

    for (uint32_t i = 0; i < dwSize; ++i)
    {
        pImplMD[i].SetValue(md[i].GetValue());
    }
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
void MethodImpl::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    DWORD size = GetSize();
    _ASSERTE(size > 0);

    image->StoreStructure(pdwSlots.GetValue(), (size+1)*sizeof(DWORD)+size*sizeof(mdToken),
                                    DataImage::ITEM_METHOD_DESC_COLD,
                                    sizeof(DWORD));
    image->StoreStructure(pImplementedMD.GetValue(), size*sizeof(RelativePointer<MethodDesc*>),
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

        RelativePointer<MethodDesc *> *pRelPtr = pImplementedMD.GetValue();
        MethodDesc * pMD = pRelPtr[iMD].GetValueMaybeNull();

        if (image->CanEagerBindToMethodDesc(pMD) &&
            image->CanHardBindToZapModule(pMD->GetLoaderModule()))
        {
            image->FixupRelativePointerField(pImplementedMD.GetValue(), iMD * sizeof(RelativePointer<MethodDesc *>));
        }
        else
        {
            image->ZeroPointerField(pImplementedMD.GetValue(), iMD * sizeof(RelativePointer<MethodDesc *>));
        }
    }

    image->FixupRelativePointerField(p, offset + offsetof(MethodImpl, pdwSlots));
    image->FixupRelativePointerField(p, offset + offsetof(MethodImpl, pImplementedMD));
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

    if (GetSlotsRaw().IsValid() && GetSize())
    {
        ULONG32 numSlots = GetSize();
        DacEnumMemoryRegion(dac_cast<TADDR>(GetSlotsRawNonNull()),
                            (numSlots + 1) * sizeof(DWORD) + numSlots * sizeof(mdToken));

        if (GetImpMDs().IsValid())
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(GetImpMDsNonNull()),
                                numSlots * sizeof(RelativePointer<MethodDesc *>));
            for (DWORD i = 0; i < numSlots; i++)
            {
                DPTR(RelativePointer<PTR_MethodDesc>) pRelPtr = GetImpMDsNonNull();
                PTR_MethodDesc methodDesc = pRelPtr[i].GetValueMaybeNull();
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

