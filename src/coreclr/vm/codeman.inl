// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



inline BOOL ExecutionManager::IsCollectibleMethod(const METHODTOKEN& MethodToken)
{
    WRAPPER_NO_CONTRACT;
    return MethodToken.m_pRangeSection->_flags & RangeSection::RANGE_SECTION_COLLECTIBLE;
}

inline TADDR IJitManager::JitTokenToModuleBase(const METHODTOKEN& MethodToken)
{
    return MethodToken.m_pRangeSection->_range.RangeStart();
}

#ifndef DACCESS_COMPILE
template<typename TCodeHeader>
inline BYTE* EECodeGenManager::allocGCInfo(TCodeHeader* pCodeHeader, DWORD blockSize, size_t * pAllocationSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    MethodDesc* pMD = pCodeHeader->GetMethodDesc();
    // sadly for light code gen I need the check in here. We should change GetJitMetaHeap
    if (pMD->IsLCGMethod())
    {
        CrstHolder ch(&m_CodeHeapCritSec);
        pCodeHeader->SetGCInfo((BYTE*)(void*)pMD->AsDynamicMethodDesc()->GetResolver()->GetJitMetaHeap()->New(blockSize));
    }
    else
    {
        pCodeHeader->SetGCInfo((BYTE*) (void*)GetJitMetaHeap(pMD)->AllocMem(S_SIZE_T(blockSize)));
    }
    _ASSERTE(pCodeHeader->GetGCInfo()); // AllocMem throws if there's not enough memory

    * pAllocationSize = blockSize;  // Store the allocation size so we can backout later.

    return(pCodeHeader->GetGCInfo());
}

template<typename TCodeHeader>
inline EE_ILEXCEPTION* EECodeGenManager::allocEHInfo(TCodeHeader* pCodeHeader, unsigned numClauses, size_t * pAllocationSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // Note - pCodeHeader->phdrJitEHInfo - sizeof(size_t) contains the number of EH clauses

    DWORD temp =  EE_ILEXCEPTION::Size(numClauses);
    DWORD blockSize = 0;
    if (!ClrSafeInt<DWORD>::addition(temp, sizeof(size_t), blockSize))
        COMPlusThrowOM();

    BYTE *EHInfo = (BYTE*)allocEHInfoRaw(pCodeHeader->GetMethodDesc(), blockSize, pAllocationSize);

    pCodeHeader->SetEHInfo((EE_ILEXCEPTION*) (EHInfo + sizeof(size_t)));
    pCodeHeader->GetEHInfo()->Init(numClauses);
    *((size_t *)EHInfo) = numClauses;
    return(pCodeHeader->GetEHInfo());
}

template<typename TCodeHeader>
void EEJitManager::RemoveJitData(TCodeHeader * pCHdr, size_t GCinfo_len, size_t EHinfo_len)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    MethodDesc* pMD = pCHdr->GetMethodDesc();

    void * codeStart = (void*)pCHdr->GetCodeStartAddress();

    if (pMD->IsLCGMethod())
    {
        {
            CrstHolder ch(&m_CodeHeapCritSec);

            LCGMethodResolver * pResolver = pMD->AsDynamicMethodDesc()->GetLCGMethodResolver();

            // Clear the pointer only if it matches what we are about to free.
            // There can be cases where the JIT is reentered and we JITed the method multiple times.
            if (pResolver->m_recordCodePointer == codeStart)
                pResolver->m_recordCodePointer = NULL;
        }

        // Remove the unwind information (if applicable)
        UnpublishUnwindInfoForMethod((TADDR)codeStart);

        HostCodeHeap* pHeap = HostCodeHeap::GetCodeHeap((TADDR)codeStart);
        FreeCodeMemory(pHeap, codeStart);

        // We are leaking GCInfo and EHInfo. They will be freed once the dynamic method is destroyed.

        return;
    }

    {
        CrstHolder ch(&m_CodeHeapCritSec);

        HeapList *pHp = GetCodeHeapList();

        while (pHp && ((pHp->startAddress > (TADDR)pCHdr) ||
                        (pHp->endAddress < (TADDR)codeStart)))
        {
            pHp = pHp->GetNext();
        }

        _ASSERTE(pHp && pHp->pHdrMap);

        // Better to just return than AV?
        if (pHp == NULL)
            return;

        NibbleMapDeleteUnlocked(pHp, (TADDR)codeStart);
    }

    // Backout the GCInfo
    if (GCinfo_len > 0) {
        GetJitMetaHeap(pMD)->BackoutMem(pCHdr->GetGCInfo(), GCinfo_len);
    }

    // Backout the EHInfo
    BYTE *EHInfo = (BYTE *)pCHdr->GetEHInfo();
    if (EHInfo) {
        EHInfo -= sizeof(size_t);

        _ASSERTE(EHinfo_len>0);
        GetJitMetaHeap(pMD)->BackoutMem(EHInfo, EHinfo_len);
    }

    // <TODO>
    // TODO: Although we have backout the GCInfo and EHInfo, we haven't actually backout the
    //       code buffer itself. As a result, we might leak the CodeHeap if jitting fails after
    //       the code buffer is allocated.
    //
    //       However, it appears non-trivial to fix this.
    //       Here are some of the reasons:
    //       (1) AllocCode calls in AllocCodeRaw to alloc code buffer in the CodeHeap. The exact size
    //           of the code buffer is not known until the alignment is calculated deep on the stack.
    //       (2) AllocCodeRaw is called in 3 different places. We might need to remember the
    //           information for these places.
    //       (3) AllocCodeRaw might create a new CodeHeap. We should remember exactly which
    //           CodeHeap is used to allocate the code buffer.
    //
    //       Fortunately, this is not a severe leak since the CodeHeap will be reclaimed on appdomain unload.
    //
    // </TODO>
    return;
}

#endif // DACCESS_COMPILE