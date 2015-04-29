//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// ZapCode.cpp
//

//
// Everything directly related to zapping of native code
//  - The code itself
//  - Code headers
//  - All XXX infos: GC Info, EH Info, Unwind Info, ...
//
// ======================================================================================

#include "common.h"

#include "zapcode.h"

#include "zapimport.h"

#include "zapinnerptr.h"

#ifdef FEATURE_READYTORUN_COMPILER
#include "zapreadytorun.h"
#endif

#ifdef REDHAWK
#include "rhcodeinfo.h"
#include "rhbinder.h"
#include "modulegcinfoencoder.h"
#endif // REDHAWK

//
// The image layout algorithm
//

ZapVirtualSection * ZapImage::GetCodeSection(CodeType codeType)
{
    switch (codeType)
    {
    case ProfiledHot:
        return m_pHotCodeSection;
    case ProfiledCold:
        return m_pColdCodeSection;
    case Unprofiled:
        return m_pCodeSection;
    }

    UNREACHABLE();
}

#if defined(WIN64EXCEPTIONS)
ZapVirtualSection * ZapImage::GetUnwindDataSection(CodeType codeType)
{
#ifdef REDHAWK
    return m_pUnwindDataSection;
#else
    switch (codeType)
    {
    case ProfiledHot:
        return m_pHotUnwindDataSection;
    case ProfiledCold:
        return m_pColdUnwindDataSection;
    case Unprofiled:
        return m_pUnwindDataSection;
    }

#endif // REDHAWK
    UNREACHABLE();
}
#endif // defined(WIN64EXCEPTIONS)

ZapVirtualSection * ZapImage::GetRuntimeFunctionSection(CodeType codeType)
{
    switch (codeType)
    {
    case ProfiledHot:
        return m_pHotRuntimeFunctionSection;
    case ProfiledCold:
        return m_pColdRuntimeFunctionSection;
    case Unprofiled:
        return m_pRuntimeFunctionSection;
    }

    UNREACHABLE();
}

ZapVirtualSection * ZapImage::GetCodeMethodDescSection(CodeType codeType)
{
    switch (codeType)
    {
    case ProfiledHot:
        return m_pHotCodeMethodDescsSection;
    case Unprofiled:
        return m_pCodeMethodDescsSection;
    default:
        UNREACHABLE();
    }
}

ZapVirtualSection* ZapImage::GetUnwindInfoLookupSection(CodeType codeType)
{
    switch(codeType)
    {
    case ProfiledHot:
        return m_pHotRuntimeFunctionLookupSection;
    case Unprofiled:
        return m_pRuntimeFunctionLookupSection;
    default:
        UNREACHABLE();
    }
}

void ZapImage::GetCodeCompilationRange(CodeType codeType, COUNT_T * start, COUNT_T * end)
{
    _ASSERTE(start && end);

#ifdef REDHAWK
    *start = 0;
    *end = m_MethodCompilationOrder.GetCount();
#else
    switch (codeType)
    {
    case ProfiledHot:
        *start = 0;
        *end = m_iUntrainedMethod;
        break;
    case ProfiledCold:
        *start = 0;
        *end = m_MethodCompilationOrder.GetCount();
        break;
    case Unprofiled:
        *start = m_iUntrainedMethod;
        *end = m_MethodCompilationOrder.GetCount();
        break;
    }
#endif // REDHAWK
}

void ZapImage::OutputCode(CodeType codeType)
{
    // Note there are three codeTypes: ProfiledHot, Unprofiled and ProfiledCold
#if defined(REDHAWK)
    SectionMethodListGenerator map;
#endif 

    bool fCold = (codeType == ProfiledCold);
    CorInfoRegionKind regionKind = (codeType == ProfiledHot) ? CORINFO_REGION_HOT : CORINFO_REGION_COLD;
    BeginRegion(regionKind);

    ZapVirtualSection * pCodeSection = GetCodeSection(codeType);
    ZapVirtualSection * pRuntimeFunctionSection = GetRuntimeFunctionSection(codeType);

#if defined (WIN64EXCEPTIONS)
    ZapVirtualSection * pUnwindDataSection = GetUnwindDataSection(codeType);
#endif // defined (WIN64EXCEPTIONS)

    DWORD codeSize = 0;

    // We should start with empty code section
    _ASSERTE(pRuntimeFunctionSection->GetNodeCount() == 0);
    _ASSERTE(pCodeSection->GetNodeCount() == 0);

    COUNT_T startMethod, endMethod;
#ifdef REDHAWK  // TritonTBD
    DWORD currentOffset = 0;
#endif // REDHAWK

    GetCodeCompilationRange(codeType, &startMethod, &endMethod);

    DWORD dwStartMethodIndex = (codeType == Unprofiled) ? m_pHotRuntimeFunctionSection->GetNodeCount() : 0;

    for (COUNT_T curMethod = startMethod; curMethod < endMethod; curMethod++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[curMethod];

        ZapBlobWithRelocs * pCode = fCold ? pMethod->m_pColdCode : pMethod->m_pCode;
        if (pCode == NULL)
        {
            continue;
        }

        if (!fCold)
        {
            pMethod->m_methodIndex = dwStartMethodIndex + pRuntimeFunctionSection->GetNodeCount();
        }
        else
        {
            pMethod->m_methodIndex = (DWORD)-1;
        }

        //Count the method size for use by ZapUnwindInfoLookupTable
        codeSize  = AlignUp(codeSize, pCode->GetAlignment());
        codeSize += pCode->GetSize();

        pCodeSection->Place(pCode);

#ifdef  REDHAWK
        DWORD codeOffset = AlignUp(currentOffset, pCode->GetAlignment());
        codeOffset = map.AlignToMethodStartGranularity(codeOffset);
        map.NoticeMethod(codeOffset, pCode->GetSize());
        currentOffset = codeOffset + pCode->GetSize();
#endif

        ZapReloc * pRelocs = pCode->GetRelocs();
        if (pRelocs != NULL)
        {
            for (ZapReloc * pReloc = pRelocs; pReloc->m_type != IMAGE_REL_INVALID; pReloc++)
            {
                ZapNode * pTarget = pReloc->m_pTargetNode;

                ZapNodeType type = pTarget->GetType();
                if (type == ZapNodeType_InnerPtr)
                {
                    pTarget = ((ZapInnerPtr *)pTarget)->GetBase();
                    type = pTarget->GetType();
                }

                switch (type)
                {
                case ZapNodeType_StubDispatchCell:
                    // Optimizations may create redundant references to the StubDispatchCell
                    if (!pTarget->IsPlaced())
                    {
                        m_pStubDispatchDataTable->PlaceStubDispatchCell((ZapImport *)pTarget);
                    }
                    break;
                case ZapNodeType_MethodEntryPoint:
                    pTarget = m_pMethodEntryPoints->CanDirectCall((ZapMethodEntryPoint *)pTarget, pMethod);
                    if (pTarget != NULL)
                    {
                        pReloc->m_pTargetNode = pTarget;
                    }
                    break;
                case ZapNodeType_Stub:
                    if (!pTarget->IsPlaced())
                    {
                        m_pStubsSection->Place(pTarget);
                    }
                    break;
                case ZapNodeType_HelperThunk:
                    if (!pTarget->IsPlaced())
                    {
                        // This should place the most frequently used JIT helpers first and together
                        m_pHelperTableSection->Place(pTarget);
                    }
                    break;
                case ZapNodeType_LazyHelperThunk:
                    if (!pTarget->IsPlaced())
                    {
                        ((ZapLazyHelperThunk *)pTarget)->Place(this);
                    }
                    break;
                case ZapNodeType_Import_ModuleHandle:
                case ZapNodeType_Import_ClassHandle:
                case ZapNodeType_Import_StringHandle:
                case ZapNodeType_Import_Helper:
                    // Place all potentially eager imports
                    if (!pTarget->IsPlaced())
                        m_pImportTable->PlaceImport((ZapImport *)pTarget);
                    break;

                case ZapNodeType_ExternalMethodThunk:
                    if (!pTarget->IsPlaced())
                        m_pExternalMethodDataTable->PlaceExternalMethodThunk((ZapImport *)pTarget);
                    break;

                case ZapNodeType_ExternalMethodCell:
                    if (!pTarget->IsPlaced())
                        m_pExternalMethodDataTable->PlaceExternalMethodCell((ZapImport *)pTarget);
                    break;

#ifdef FEATURE_READYTORUN_COMPILER
                case ZapNodeType_DynamicHelperCell:
                    if (!pTarget->IsPlaced())
                        m_pDynamicHelperDataTable->PlaceDynamicHelperCell((ZapImport *)pTarget);
                    break;

                case ZapNodeType_IndirectHelperThunk:
                    if (!pTarget->IsPlaced())
                        m_pImportTable->PlaceIndirectHelperThunk(pTarget);
                    break;

                case ZapNodeType_RVAFieldData:
                    if (!pTarget->IsPlaced())
                        m_pReadOnlyDataSection->Place(pTarget);
                    break;
#endif

                case ZapNodeType_GenericSignature:
                    if (!pTarget->IsPlaced())
                        m_pImportTable->PlaceBlob((ZapBlob *)pTarget);
                    break;
                default:
                    break;
                }
            }
        }

#if defined (WIN64EXCEPTIONS)
        //
        // Place unwind data
        //

        InlineSArray<ZapUnwindInfo *, 8> unwindInfos;

        ZapUnwindInfo * pFragment;

        // Go over all fragments and append their unwind infos in this section
        for (pFragment = pMethod->m_pUnwindInfoFragments;
             pFragment != NULL;
             pFragment = pFragment->GetNextFragment())
        {
            ZapNode * pFragmentCode = pFragment->GetCode();
            _ASSERTE(pFragmentCode == pMethod->m_pCode || pFragmentCode == pMethod->m_pColdCode);

            if (pFragmentCode == pCode)
            {
                unwindInfos.Append(pFragment);
            }
        }

        // The runtime function section must be ordered correctly relative to code layout
        // in the image. Sort the unwind infos by their offset
        _ASSERTE(unwindInfos.GetCount() > 0);
        qsort(&unwindInfos[0], unwindInfos.GetCount(), sizeof(ZapUnwindInfo *), ZapUnwindInfo::CompareUnwindInfo);

        // Set the initial unwind info for the hot and cold sections
        if (fCold)
        {
            _ASSERTE(pMethod->m_pColdUnwindInfo == NULL);
            pMethod->m_pColdUnwindInfo = unwindInfos[0];
        }
        else
        {
            _ASSERTE(pMethod->m_pUnwindInfo == NULL);
            pMethod->m_pUnwindInfo = unwindInfos[0];
        }

        for (COUNT_T iUnwindInfo = 0; iUnwindInfo < unwindInfos.GetCount(); iUnwindInfo++)
        {
            ZapUnwindInfo * pUnwindInfo = unwindInfos[iUnwindInfo];
            pRuntimeFunctionSection->Place(pUnwindInfo);

            ZapNode * pUnwindData = pUnwindInfo->GetUnwindData();

#if defined(BINDER) && defined(TARGET_THUMB2)
            if (pUnwindData != NULL)
#endif
            {
                if (!pUnwindData->IsPlaced())
                {
                    pUnwindDataSection->Place(pUnwindData);
                }
            }
        }

#else // defined (WIN64EXCEPTIONS)

        ZapUnwindInfo * pUnwindInfo;
        if (fCold)
        {
            // Chained unwind info
            pUnwindInfo = new (GetHeap()) ZapUnwindInfo(pCode, 0, 0, pMethod->m_pUnwindInfo);
            pMethod->m_pColdUnwindInfo = pUnwindInfo;
        }
        else
        {
            pUnwindInfo = new (GetHeap()) ZapUnwindInfo(pCode, 0, 0, pMethod->m_pGCInfo);
            pMethod->m_pUnwindInfo = pUnwindInfo;
        }
        pRuntimeFunctionSection->Place(pUnwindInfo);

#endif // defined (WIN64EXCEPTIONS)

        if (m_stats != NULL)
        {
            CorInfoIndirectCallReason reason;
            BOOL direct = m_pPreloader->CanSkipMethodPreparation(NULL, pMethod->GetHandle(), &reason);

            if (direct && pMethod->m_pFixupList != NULL)
            {
                reason = CORINFO_INDIRECT_CALL_FIXUPS;
                direct = FALSE;
            }

            if (direct)
            {
                m_stats->m_directMethods++;
            }
            else
            {
                m_stats->m_prestubMethods++;
                m_stats->m_indirectMethodReasons[reason]++;
            }
        }
    }

#ifdef REDHAWK
    // Redhawk needs any trailing padding to be 0xcc
    DWORD cbPad = AlignUp(currentOffset, sizeof(DWORD)) - currentOffset;
    if (cbPad != 0)
    {
        ZapBlob * pBlob = ZapBlob::NewBlob(this, NULL, cbPad);
        memset(pBlob->GetData(), DEFAULT_CODE_BUFFER_INIT, cbPad);
        pCodeSection->Place(pBlob);
        currentOffset += cbPad;
    }

    map.Output(this, m_pCodeMgrSection, numMethods);
#else
    COUNT_T nUnwindInfos = pRuntimeFunctionSection->GetNodeCount();

    if (nUnwindInfos != 0)
    {
        if (IsReadyToRunCompilation())
        {
            // TODO: Implement
        }
        else
        if (!fCold)
        {
            ZapVirtualSection * pCodeMethodDescSection = GetCodeMethodDescSection(codeType);
            pCodeMethodDescSection->Place(new (GetHeap()) ZapCodeMethodDescs(startMethod, endMethod, nUnwindInfos));

            ZapVirtualSection* pUnwindInfoLookupSection = GetUnwindInfoLookupSection(codeType);
            pUnwindInfoLookupSection->Place(new (GetHeap()) ZapUnwindInfoLookupTable(pRuntimeFunctionSection, pCodeSection, codeSize));
        }
        else
        {
            m_pColdCodeMapSection->Place(new (GetHeap()) ZapColdCodeMap(pRuntimeFunctionSection));
        }
    }
#endif

    EndRegion(regionKind);
}

void ZapImage::OutputCodeInfo(CodeType codeType)
{
    CorInfoRegionKind regionKind = (codeType == ProfiledHot) ? CORINFO_REGION_HOT : CORINFO_REGION_COLD;
    BeginRegion(regionKind);

    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

        //
        // We are either outputing the ProfiledHot methods
        // or the unprofiled and cold methods
        //
        if ((pMethod->m_ProfilingDataFlags & (1 << ReadMethodCode)) != (codeType == ProfiledHot))
        {
            // Wrong kind so skip
            continue;
        }

        if (pMethod->m_pROData != NULL)
            m_pReadOnlyDataSection->Place(pMethod->m_pROData);

#ifndef REDHAWK
        // Note: for Redhawk we place EH info via OutputEHInfo().
        if (pMethod->m_pExceptionInfo != NULL)
        {
            ZapNode* pCode = pMethod->m_pCode;
            m_pExceptionInfoLookupTable->PlaceExceptionInfoEntry(pCode, pMethod->m_pExceptionInfo);
        }
#endif // REDHAWK

#ifndef BINDER // in the binder, shift this to an earlier phase because of phase ordering problem (FlushPrecodesAndMethodDescs needs this)
        if (pMethod->m_pFixupList != NULL && !IsReadyToRunCompilation())
            pMethod->m_pFixupInfo = m_pImportTable->PlaceFixups(pMethod->m_pFixupList);
#endif
    }

    EndRegion(regionKind);
}

void ZapImage::OutputProfileData()
{
    if (m_pInstrumentSection == NULL)
    {
        return;
    }

    ZapProfileData * pPrevious = NULL;

    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

        if (pMethod->m_pProfileData == NULL)
        {
            continue;
        }

        ZapProfileData * pHeader = new (GetHeap()) ZapProfileData(pMethod);

        m_pInstrumentSection->Place(pHeader);
        m_pInstrumentSection->Place(pMethod->m_pProfileData);

        if (pPrevious != NULL)
        {
            pPrevious->SetNext(pHeader);
        }

        pPrevious = pHeader;
    }
}

void ZapImage::OutputDebugInfo()
{
    m_pDebugInfoTable->PrepareLayout();
    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        m_pDebugInfoTable->PlaceDebugInfo(m_MethodCompilationOrder[i]);
    }
    m_pDebugInfoTable->FinishLayout();
}

void ZapImage::OutputGCInfo()
{
#ifndef REDHAWK
    struct  MaskValue
    {
        DWORD   mask;
        DWORD   value;
    };

    static const MaskValue gcInfoSequence[] =
    {
        {   (1 << CommonReadGCInfo)                  , (1 << CommonReadGCInfo) }, // c flag on, r flag don't care
        {   (1 << CommonReadGCInfo)|(1 << ReadGCInfo), (1 << ReadGCInfo)       }, // r flag on, c flag off
        {   (1 << CommonReadGCInfo)|(1 << ReadGCInfo), 0                       }, // both flags off
        {   0, 0 }
    };

    // Make three passes over the gc infos, emitting them in order of decreasing hotness,
    // and for stuff that wasn't touched by anyone we put it in the cold section
    for (const MaskValue *pMaskValue = gcInfoSequence; pMaskValue->mask; pMaskValue++)
    {
        for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
        {
            ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

            if ((pMethod->m_ProfilingDataFlags & pMaskValue->mask) != pMaskValue->value)
            {
                continue;
            }

            ZapGCInfo * pGCInfo = pMethod->m_pGCInfo;

            // Given that GC Info can be interned it may have been placed already on
            // this or a previous pass through the compiled methods. If it hasn't already
            // been placed then we place it in the appropriate section.
            if (!pGCInfo->IsPlaced())
            {
                //  A) it was touched, and here they are placed in order of flags above
                if (pMaskValue->value)
                {
                    m_pHotTouchedGCSection->Place(pGCInfo);
                }
                //  B) the method that it is attached to is in the trained section
                else if (i<m_iUntrainedMethod)
                {
                    m_pHotGCSection->Place(pGCInfo);
                }
                // C) it wasn't touched _and_ it is related to untrained code
                else
                {
                    m_pGCSection->Place(pGCInfo);
                }
            }
        }

        // Just after placing those touched in an IBC scenario, place those that
        // should be prioritized regardless of the corresponding method's IBC information.
        // (Currently, this is used to pack the gc info of IL stubs that cannot be directly tracked by IBC.)
        if (pMaskValue->value == (1 << ReadGCInfo))
        {
            for (COUNT_T i = 0; i < m_PrioritizedGCInfo.GetCount(); i++)
            {
                ZapGCInfo * pGCInfo = m_PrioritizedGCInfo[i];
                if (!pGCInfo->IsPlaced())
                {
                    m_pHotGCSection->Place(pGCInfo);
                }
            }
        }
    }
#else // REDHAWK
    //
    ModuleGcInfoEncoder * pEncoder = GetGcInfoEncoder();

    m_pUnwindInfoBlob   = pEncoder->ConstructUnwindInfoBlob(this);
    m_pCallsiteInfoBlob = pEncoder->ConstructCallsiteInfoBlob(this);
    ZapBlob * pShortcutMap   = pEncoder->ConstructDeltaShortcutMap(this);

    // @TODO: we could fold this loop into ConstructMethodInfoBlob, but then we'd have to keep a separate
    // list of method infos inside the ModuleGcInfoEncoder..
    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        pEncoder->EncodeMethodInfo(pMethod->m_pGCInfo);
    }
    ZapBlob * pMethodInfos   = pEncoder->ConstructMethodInfoBlob(this);

    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        // At this point pMethod->m_pGCInfo is really a pointer that the encoder owns.
        // We must pass it back to the encoder so it can encode it and pass back a proper ZapBlob *
        pMethod->m_pGCInfo = pEncoder->FindMethodInfo(this, pMethod->m_pGCInfo);
    }

    m_pGCSection->Place(pShortcutMap);
    m_pGCSection->Place(pMethodInfos);
    if (m_pUnwindInfoBlob)
        m_pGCSection->Place(m_pUnwindInfoBlob);

    if (m_pCallsiteInfoBlob)
        m_pGCSection->Place(m_pCallsiteInfoBlob);

    //
    // Create the method-number-to-gc-info table
    //
    UINT32 methodInfoSize = pMethodInfos->GetSize();

    COUNT_T nMethods = m_MethodCompilationOrder.GetCount();
    UINT16 elemSize = 4;

    if (methodInfoSize <= 0x10000)
    {
        elemSize = 2;

        // Remember the element size for this map in the module header
        m_moduleHeaderFlags |= ModuleHeader::SmallGCInfoListEntriesFlag;
    }

    // Create the table
    SIZE_T tableSize = elemSize * nMethods;
    ZapBlob * pMethodToGcInfoMap = ZapBlob::NewBlob(this, NULL, tableSize);

    UINT16* pwTableEntries = (UINT16*) pMethodToGcInfoMap->GetData();
    UINT32* pdwTableEntries = (UINT32*) pwTableEntries;

    for (COUNT_T i = 0; i < nMethods; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        ZapGCInfo * pGCInfo = pMethod->m_pGCInfo;

        UINT32 uOffset = 0; 
        if (pGCInfo->GetType() == ZapNodeType_InnerPtr)
        {
            uOffset = ((ZapInnerPtr*)pGCInfo)->GetOffset();
        }
        else
        {
            assert(ZapNodeType_Blob == pGCInfo->GetType());
            assert(pGCInfo == pMethodInfos);
        }

        if (2 == elemSize)
        {
            assert(uOffset <= 0xFFFF);
            pwTableEntries[i] = uOffset;
        }
        else
        {
            pdwTableEntries[i] = uOffset;
        }
    }

    m_pMethodToGCInfoMap->Place(pMethodToGcInfoMap);
#endif // REDHAWK
}

#ifdef REDHAWK
// Place all ZapExceptionInfo blobs into the exception section, and form the lookup table that we'll
// use at runtime to find EH info for a given method.
void ZapImage::OutputEHInfo()
{
    // For non-REDHAWK builds, we output EH info with the other per-method data in OutputCodeInfo().

    // @TODO: consider emitting EH info in order of increasing hotness, like we do for GC info.

    // Place EH info for every method that has EH.
    for (COUNT_T i = 0; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        ZapExceptionInfo * pEHInfo = pMethod->m_pExceptionInfo;

        if ((pEHInfo != NULL) && !pEHInfo->IsPlaced())
        {
            // We add relocs to the exception info here, at the last possible momement before placing
            // them. That's because adding relocs changes the exception info, but prior to this we
            // want to be able to use the data of the exception info as a hash key for interning.
            AddRelocsForEHClauses(pEHInfo);
            m_pExceptionSection->Place(pEHInfo);
        }
    }

    // Get the offsets for each EH blob that we will emit.
    MapSHash<ZapNode *, UINT32>   ehinfoOffsets;
    UINT32                        ehinfoSize;

    ehinfoSize = m_pExceptionSection->FillInNodeOffsetMap(&ehinfoOffsets);

    // Chose a table entry size.
    COUNT_T nMethods = m_MethodCompilationOrder.GetCount();
    UINT16 elemSize = 4;

    if (ehinfoSize <= 0x10000)
    {
        elemSize = 2;

        // Remember the element size for this map in the module header
        m_moduleHeaderFlags |= ModuleHeader::SmallEHInfoListEntriesFlag;
    }

    // Create the table.
    SIZE_T tableSize = elemSize * nMethods;
    SArray<BYTE> tableData(tableSize);

    UINT16* pwTableEntries = (UINT16*)&tableData[0];
    UINT32* pdwTableEntries = (UINT32*) pwTableEntries;

    // Fill in the offset for each method that has EH info. For methods that have no
    // EH info, we will use a sentinel offset of -1.
    for (COUNT_T i = 0; i < nMethods; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        ZapExceptionInfo * pEHInfo = pMethod->m_pExceptionInfo;

        UINT32 uOffset = -1;

        if (pEHInfo != NULL)
        {
            ehinfoOffsets.Lookup(pEHInfo, &uOffset);
            assert(uOffset != -1); // Can't have a valid offset match the sentinel!
            assert((4 == elemSize) || (uOffset <= 0xFFFF)); // Size must fit in 2 bytes if we're using hte small rep.
        }

        if (2 == elemSize)
        {
            pwTableEntries[i] = uOffset;
        }
        else
        {
            pdwTableEntries[i] = uOffset;
        }
    }

    m_pMethodToEHInfoMap->Place(ZapBlob::NewBlob(this, &tableData[0], tableSize));
}
#endif // REDHAWK

#ifdef REDHAWK
// Add relocs for any EEType references in any typed EH clauses for the given EH Info.
void ZapImage::AddRelocsForEHClauses(ZapExceptionInfo * pExceptionInfo)
{
    EE_ILEXCEPTION *pEHInfo = (EE_ILEXCEPTION *)pExceptionInfo->GetData();
    _ASSERTE(pEHInfo != NULL);

    // One set of relocs for the entire set of clauses. Size assuming that every clause has a token.
    ZapReloc * pRelocs = (ZapReloc *)
        new (GetHeap()) BYTE[sizeof(ZapReloc) * pEHInfo->EHCount() + sizeof(ZapRelocationType)];

    DWORD relocIndex = 0;

    // Add relocs for EEType references each typed clause.
    for (int i = 0; i < pEHInfo->EHCount(); i++)
    {
        EE_ILEXCEPTION_CLAUSE *pClause = pEHInfo->EHClause(i);

        if ((pClause->Flags == COR_ILEXCEPTION_CLAUSE_NONE) ||
            (pClause->Flags == COR_ILEXCEPTION_CLAUSE_INDIRECT_TYPE_REFERENCE))
        {
            ZapNode *pEETypeNode = (ZapNode*)pClause->EETypeReference;

            // @TODO: we're using a full pointer for each EEType reference in the EH clause. This will be 
            // 64bits on a 64bit system, though, which is twice as large as it needs to be. We should make 
            // these 32bit RVA's and compute the final address at runtime when we start supporting 64bit 
            // systems. See comments in ZapInfo::setEHinfo() for more details.
            //
            // N.B! If we move to RVAs, then the runtime structure that matches the EE_ILEXCEPTION struct 
            // needs to have a padding field removed.  (The C++ compiler introduced 4 bytes of padding between 
            // 'DataSize' and 'Clauses' because 'Clauses' has a pointer field in it.  This padding will
            // disappear when we change the pointers to RVAs.)
            pRelocs[relocIndex].m_type = IMAGE_REL_BASED_PTR;
            pRelocs[relocIndex].m_pTargetNode = pEETypeNode;
            pRelocs[relocIndex].m_offset = (BYTE*)pClause - (BYTE*)pEHInfo + offsetof(EE_ILEXCEPTION_CLAUSE, EETypeReference);
            pExceptionInfo->ZeroPointer(pRelocs[relocIndex].m_offset);
            relocIndex++;
        }
    }

    // Did we end up with any relocs? If so, then add them to the blob.
    if (relocIndex > 0)
    {
        // Set sentinel
        C_ASSERT(offsetof(ZapReloc, m_type) == 0);
        pRelocs[relocIndex].m_type = IMAGE_REL_INVALID;

        pExceptionInfo->SetRelocs(pRelocs);
    }
}
#endif // REDHAWK

//
// ZapMethodHeader
//

#ifdef _TARGET_ARM_
// Avoid ARM hazard due to QualComm Krait processor bug.
#define ARM_HAZARD_AVOIDANCE
#endif

#ifdef ARM_HAZARD_AVOIDANCE

//
// This code was stolen from the C++ linker (vctools\Link\src\arm.cpp)
// 

bool F32BitInstr(PBYTE pbInstr)
{
    const WORD wInstr = *((const WORD UNALIGNED *) pbInstr);
    return (wInstr >> 11) >= 0x1D;
}

bool FHazardCandidate(PBYTE pbInstr)

/*++

Routine Description:

    If the following 4-instruction/10-byte pattern begins at the 0xFB8 offset
    from a page, then it would cause the hazard.

    Address        Contents
    ------------------------------
    0x*****FB8     T16 instruction
    0x*****FBA     T16 instruction
    0x*****FBC     T16 instruction
    0x*****FBE     T32 instruction that can branch

    Finding all such instruction sequences could be very difficult, because
    the function could have literal's which are mixed with instructions and
    for now we don't have an easy way to find where literal is.

    So instead of detecting such instruction sequence, we assume an instr
    starts at 0x*****FB8 and check whether such sequence exists.  This may
    introduce false positive's. 
    
Arguments:

    pbCon - pointer to the instruction at 0x*****FB8

Return Value:

    false no hazard true otherwise

--*/

{
    // Check whether there are three 16-bit instructions.

    for (int i = 0; i < 3; i++) {
        if (F32BitInstr(pbInstr)) {
            return false;
        }

        pbInstr += sizeof(WORD);
    }

    // Check whether next is a 32-bit unconditional PC relative branch.

    if (!F32BitInstr(pbInstr)) {
        return false;
    }

    // Check 32-bit branch.

    const DWORD dwInstr = *((const DWORD UNALIGNED *) pbInstr);

    return
        // B  (A8.6.16, encoding T3)

        ((dwInstr & 0xD000F800) == 0x8000F000 && (dwInstr & 0x3C0) != 0x380) ||

        // B   (A8.6.16, encoding T4)

        ((dwInstr & 0xD000F800) == 0x9000F000) ||

        // BL  (A8.6.23, encoding T1)

        ((dwInstr & 0xD000F800) == 0xD000F000) ||

        // BLX (A8.6.23, encoding T2)

        ((dwInstr & 0xD001F800) == 0xC000F000) ||

        // BXJ (A8.6.26, encoding T1)

        ((dwInstr & 0xFFFFFFF0) == 0x8F00F3C0) ||

        // LDM/LDMIA/LDMFD, with PC in target reg list (A8.6.53, encoding T2)

        ((dwInstr & 0xA000FFD0) == 0x8000E890 && (dwInstr & 0x02F) != 0x02D) ||

        // LDMDB/LDMEA, with PC in target reg list (A.8.6.55, encoding T1)

        ((dwInstr & 0xA000FFD0) == 0x8000E910) ||

        // LDR immediate, with PC as target reg (A8.6.57, encoding T3)

        ((dwInstr & 0xF000FFF0) == 0xF000F8D0 && (dwInstr & 0x0F) != 0x0F) ||

        // LDR immediate, with PC as target reg (A8.6.57, encoding T4)

        ((dwInstr & 0xF800FFF0) == 0xF800F850 && (dwInstr & 0x0F) != 0x0F) ||

        // LDR literal, with PC as target reg (A8.6.59, encoding T2)

        ((dwInstr & 0xF000FF7F) == 0xF000F85F) ||

        // LDR register, with PC as target reg (A8.6.60, encoding T2)

        ((dwInstr & 0xFFC0FFF0) == 0xF000F850) ||

        // POP, with PC in target reg list (A8.6.122, encoding T2)

        ((dwInstr & 0xA000FFFF) == 0x8000E8BD) ||

        // POP, with PC as target reg (A8.6.122, encoding T3)

        (dwInstr == 0xFB04F85D) ||

        // TBB/TBH (A8.6.226, encoding T1)

        ((dwInstr & 0xFFE0FFF0) == 0xF000E8D0);
}

//
// End of code stolen from the C++ linker
//

#endif // ARM_HAZARD_AVOIDANCE


#if defined(_TARGET_X86_) || defined(ARM_HAZARD_AVOIDANCE)

DWORD ZapCodeBlob::ComputeRVA(ZapWriter * pZapWriter, DWORD dwPos)
{
    void * pData = GetData();
    SIZE_T size = GetSize();
    DWORD dwAlignment = GetAlignment();

    dwPos = AlignUp(dwPos, dwAlignment);

#ifdef _TARGET_X86_
    //
    // Padding for straddler relocations.
    //

    // The maximum size of padding
    const DWORD cbAdjustForDynamicBaseMax = 256;

    // Find padding that gives us minimum number of straddlers
    DWORD nMinStraddlers = MAXDWORD;
    DWORD bestPad = 0;
    for (DWORD pad = 0; pad < cbAdjustForDynamicBaseMax; pad += dwAlignment)
    {
        COUNT_T nStraddlers = GetCountOfStraddlerRelocations(dwPos + pad);
        if (nStraddlers < nMinStraddlers)
        {
            nMinStraddlers = nStraddlers;
            bestPad = pad;

            // It won't get better than this.
            if (nMinStraddlers == 0)
                break;
        }
    }

    DWORD dwPaddedPos = dwPos + bestPad;
    SetRVA(dwPaddedPos);

    return dwPaddedPos + size;
#endif // _TARGET_X86_

#ifdef ARM_HAZARD_AVOIDANCE
    for (DWORD dwPadding = 0; dwPadding < 0x1000; dwPadding += dwAlignment)
    {
        DWORD dwPaddedPos = dwPos + dwPadding;

        BOOL fHasHazard = FALSE;

        //
        // Go through all possible places where the hazard may occur within the data block
        //

        // The possible occurences of the hazard are always at offset 0xFB8 within the 4k page.
        // Start with the first page containing the block.
        DWORD dwFirstHazardPos = AlignDown(dwPaddedPos, 0x1000) + 0xFB8;
        if (dwFirstHazardPos < dwPaddedPos)
            dwFirstHazardPos += 0x1000;

        // The last possible occurence of the hazard is 10 bytes before the end of the block
        DWORD dwLastHazardPos = (dwPaddedPos + size) - 10;

        for (DWORD dwHazardPos = dwFirstHazardPos; dwHazardPos <= dwLastHazardPos; dwHazardPos += 0x1000)
        {
            int offset = dwHazardPos - dwPaddedPos;

            if (FHazardCandidate((BYTE * )pData + offset))
            {
                fHasHazard = TRUE;
                break;
            }
        }

        if (!fHasHazard)
        {
            SetRVA(dwPaddedPos);

            return dwPaddedPos + size;
        }
    }

    // There is a theoretical chance that we may not be able to find a suitable padding
    // to workaround the bug. In this case don't attempt to workaround the bug,
    // and simply place the code at the next natural RVM. It should happen
    // very rarely for very large methods only, and there should be no retail devices
    // with this processor bug.
    SetRVA(dwPos);

    return dwPos + size;
#endif
}

template <DWORD alignment>
class ZapCodeBlobConst : public ZapCodeBlob
{
protected:
    ZapCodeBlobConst(SIZE_T cbSize)
        : ZapCodeBlob(cbSize)
    {
    }

public:
    virtual UINT GetAlignment()
    {
        return alignment;
    }

    static ZapCodeBlob * NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
    {
        S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapCodeBlobConst<alignment>)) + S_SIZE_T(cbSize);
        if(cbAllocSize.IsOverflow())
            ThrowHR(COR_E_OVERFLOW);
        
        void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

        ZapCodeBlob * pZapCodeBlob = new (pMemory) ZapCodeBlobConst<alignment>(cbSize);

        if (pData != NULL)
            memcpy(pZapCodeBlob + 1, pData, cbSize);

        return pZapCodeBlob;
    }
};

ZapCodeBlob * ZapCodeBlob::NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment)
{
    switch (cbAlignment)
    {
    case 1:
        return ZapCodeBlobConst<1>::NewBlob(pWriter, pData, cbSize);
    case 2:
        return ZapCodeBlobConst<2>::NewBlob(pWriter, pData, cbSize);
    case 4:
        return ZapCodeBlobConst<4>::NewBlob(pWriter, pData, cbSize);
    case 8:
        return ZapCodeBlobConst<8>::NewBlob(pWriter, pData, cbSize);
    case 16:
        return ZapCodeBlobConst<16>::NewBlob(pWriter, pData, cbSize);

    default:
        _ASSERTE(!"Requested alignment not supported");
        return NULL;
    }
}

#endif


// See function prototype for details on why this iterator is "partial"
BOOL ZapMethodHeader::PartialTargetMethodIterator::GetNext(CORINFO_METHOD_HANDLE *pHnd)
{
    _ASSERTE(pHnd != NULL);

    if (m_pCurReloc == NULL)
    {
        return FALSE;
    }

    while (m_pCurReloc->m_type != IMAGE_REL_INVALID)
    {
        ZapNode * pTarget = m_pCurReloc->m_pTargetNode;
        ZapNodeType type = pTarget->GetType();

        m_pCurReloc++;

        if (type == ZapNodeType_InnerPtr)
        {
            pTarget = ((ZapInnerPtr *)pTarget)->GetBase();
            type = pTarget->GetType();
        }

        if (type == ZapNodeType_MethodEntryPoint)
        {
            *pHnd = ((ZapMethodEntryPoint *)pTarget)->GetHandle();
            return TRUE;
        }
    }

    return FALSE;
}

void ZapCodeMethodDescs::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    COUNT_T nUnwindInfos = 0;
    
    for (COUNT_T curMethod = m_iStartMethod; curMethod < m_iEndMethod; curMethod++)
    {
        ZapMethodHeader * pMethod = pImage->m_MethodCompilationOrder[curMethod];
        DWORD dwRVA = pImage->m_pPreloader->MapMethodHandle(pMethod->GetHandle());

        if (pMethod->m_pExceptionInfo != NULL)
            dwRVA |= HAS_EXCEPTION_INFO_MASK;

        pImage->Write(&dwRVA, sizeof(dwRVA));
        nUnwindInfos++;

#ifdef WIN64EXCEPTIONS
        ZapUnwindInfo * pFragment = pMethod->m_pUnwindInfoFragments;
        while (pFragment != NULL)
        {
            if (pFragment != pMethod->m_pUnwindInfo && pFragment->GetCode() == pMethod->m_pCode)
            {
                dwRVA = 0;
                pImage->Write(&dwRVA, sizeof(dwRVA));
                nUnwindInfos++;
            }

            pFragment = pFragment->GetNextFragment();
        }
#endif
    }
    _ASSERTE(nUnwindInfos == m_nUnwindInfos);
}

//
// ZapMethodEntryPoint
//

void ZapMethodEntryPoint::Resolve(ZapImage * pImage)
{
#ifdef CLR_STANDALONE_BINDER
    if (m_pEntryPoint != NULL)
    {
        if (m_pEntryPoint->GetType() == ZapNodeType_InnerPtr)
        {
            ZapInnerPtr *pInnerPtr = (ZapInnerPtr *)m_pEntryPoint;
            pInnerPtr->Resolve();
        }
        SetRVA(m_pEntryPoint->GetRVA());
    }
    else
#endif
    {
        DWORD rvaValue = pImage->m_pPreloader->MapMethodEntryPoint(GetHandle());
#ifdef _DEBUG
        if (rvaValue == NULL)
        {
            mdMethodDef token;
            pImage->GetCompileInfo()->GetMethodDef(GetHandle(), &token);
            pImage->Error(token, S_OK, W("MapMethodEntryPoint failed"));
        }
        else
#endif
        {
            SetRVA(rvaValue);
        }
    }
}

ZapMethodEntryPoint * ZapMethodEntryPointTable::GetMethodEntryPoint(CORINFO_METHOD_HANDLE handle, CORINFO_ACCESS_FLAGS accessFlags)
{
    ZapMethodEntryPoint * pMethodEntryPoint = m_entries.Lookup(MethodEntryPointKey(handle, accessFlags));

    if (pMethodEntryPoint != NULL)
        return pMethodEntryPoint;

#ifdef _DEBUG
    mdMethodDef token;
    m_pImage->GetCompileInfo()->GetMethodDef(handle, &token);
#endif

    pMethodEntryPoint = new (m_pImage->GetHeap()) ZapMethodEntryPoint(handle, accessFlags);
    m_entries.Add(pMethodEntryPoint);
    return pMethodEntryPoint;
}

void ZapMethodEntryPointTable::Resolve()
{
    for (MethodEntryPointTable::Iterator i = m_entries.Begin(), end = m_entries.End(); i != end; i++)
    {
        ZapMethodEntryPoint * pMethodEntryPoint = *i;

        // Skip unused entrypoints - they may be omitted in the image
        if (!pMethodEntryPoint->IsUsed())
            continue;

        pMethodEntryPoint->Resolve(m_pImage);
    }
}

ZapNode * ZapMethodEntryPointTable::CanDirectCall(ZapMethodEntryPoint * pMethodEntryPoint, ZapMethodHeader * pCaller)
{
    CORINFO_METHOD_HANDLE caller = pCaller->GetHandle();
    CORINFO_METHOD_HANDLE callee = pMethodEntryPoint->GetHandle();

    CorInfoIndirectCallReason reason;
    if (m_pImage->canIntraModuleDirectCall(caller, callee, &reason, pMethodEntryPoint->GetAccessFlags()))
    {
        ZapNode * pCode = m_pImage->GetCompiledMethod(callee)->GetCode();
#ifndef BINDER
#ifdef _TARGET_ARM_
        pCode = m_pImage->GetInnerPtr(pCode, THUMB_CODE);
#endif // _TARGET_ARM_
#endif // BINDER
        return pCode;
    }
    else
    {
        if (!pMethodEntryPoint->IsUsed())
        {
            // This method entry point is going to be used for indirect call.
            // Record this so that later we will assign it an RVA.
            pMethodEntryPoint->SetIsUsed();
        }
        return NULL;
    }
}

#ifdef WIN64EXCEPTIONS
ZapGCInfo * ZapGCInfoTable::GetGCInfo(PVOID pGCInfo, SIZE_T cbGCInfo, PVOID pUnwindInfo, SIZE_T cbUnwindInfo)
{
    ZapGCInfo * pNode = m_blobs.Lookup(GCInfoKey(pGCInfo, cbGCInfo, pUnwindInfo, cbUnwindInfo));

    if (pNode != NULL)
    {
        return pNode;
    }

    pNode = ZapGCInfo::NewGCInfo(m_pImage, pGCInfo, cbGCInfo, pUnwindInfo, cbUnwindInfo);
    m_blobs.Add(pNode);
    return pNode;
}

ZapGCInfo * ZapGCInfo::NewGCInfo(ZapWriter * pWriter, PVOID pGCInfo, SIZE_T cbGCInfo, PVOID pUnwindInfo, SIZE_T cbUnwindInfo)
{
    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapGCInfo)) + S_SIZE_T(cbGCInfo) + S_SIZE_T(cbUnwindInfo);
    if(cbAllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

    ZapGCInfo * pZapGCInfo = new (pMemory) ZapGCInfo(cbGCInfo, cbUnwindInfo);
    
    memcpy(pZapGCInfo->GetGCInfo(), pGCInfo, cbGCInfo);
    memcpy(pZapGCInfo->GetUnwindInfo(), pUnwindInfo, cbUnwindInfo);

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)
    // Make sure the personality routine thunk is created
    pZapGCInfo->GetPersonalityRoutine(ZapImage::GetImage(pWriter));
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)
    return pZapGCInfo;
}
#else
ZapGCInfo * ZapGCInfoTable::GetGCInfo(PVOID pBlob, SIZE_T cbBlob)
{
    ZapGCInfo * pNode = m_blobs.Lookup(ZapBlob::SHashKey(pBlob, cbBlob));

    if (pNode != NULL)
    {
        return pNode;
    }

    pNode = ZapBlob::NewBlob(m_pImage, pBlob, cbBlob);
    m_blobs.Add(pNode);
    return pNode;
}
#endif

//
// ZapUnwindInfo
//

void ZapUnwindInfo::Save(ZapWriter * pZapWriter)
{
    RUNTIME_FUNCTION runtimeFunction;

#if defined(_TARGET_ARM_)
    RUNTIME_FUNCTION__SetBeginAddress(&runtimeFunction, GetStartAddress());
    runtimeFunction.UnwindData = m_pUnwindData->GetRVA();
#elif defined(_TARGET_AMD64_)
    runtimeFunction.BeginAddress = GetStartAddress();
    runtimeFunction.EndAddress = GetEndAddress();
    ULONG unwindData = m_pUnwindData->GetRVA();
    if (m_pUnwindData->GetType() == ZapNodeType_UnwindInfo) // Chained unwind info
        unwindData |= RUNTIME_FUNCTION_INDIRECT;
    runtimeFunction.UnwindData = unwindData;
#elif defined(_TARGET_X86_)
    runtimeFunction.BeginAddress = GetStartAddress();
    ULONG unwindData = m_pUnwindData->GetRVA();
    if (m_pUnwindData->GetType() == ZapNodeType_UnwindInfo) // Chained unwind info
        unwindData |= RUNTIME_FUNCTION_INDIRECT;
    runtimeFunction.UnwindData = unwindData;
#else
    PORTABILITY_ASSERT("ZapUnwindInfo");
#endif

    pZapWriter->Write(&runtimeFunction, sizeof(runtimeFunction));
}

#ifdef WIN64EXCEPTIONS
// Compare the unwind infos by their offset
int __cdecl ZapUnwindInfo::CompareUnwindInfo(const void* a_, const void* b_)
{
    ZapUnwindInfo * a = *(ZapUnwindInfo **)a_;
    ZapUnwindInfo * b = *(ZapUnwindInfo **)b_;

    if (a->GetStartOffset() > b->GetStartOffset())
    {
        _ASSERTE(a->GetStartOffset() >= b->GetEndOffset());
        return 1;
    }

    if (a->GetStartOffset() < b->GetStartOffset())
    {
        _ASSERTE(a->GetEndOffset() <= b->GetEndOffset());
        return -1;
    }

    _ASSERTE(a == b);
    return 0;
}

#if defined(_TARGET_AMD64_)

UINT ZapUnwindData::GetAlignment()
{
    return sizeof(ULONG);
}

DWORD ZapUnwindData::GetSize()
{
    DWORD dwSize = ZapBlob::GetSize();

#ifndef REDHAWK
    // Add space for personality routine, it must be 4-byte aligned.
    // Everything in the UNWIND_INFO has already had its size included in size
    dwSize = AlignUp(dwSize, sizeof(ULONG));

    dwSize += sizeof(ULONG);
#endif //REDHAWK

    return dwSize;
}

void ZapUnwindData::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    PVOID pData = GetData();
    DWORD dwSize = GetBlobSize();

    UNWIND_INFO * pUnwindInfo = (UNWIND_INFO *)pData;

    // Check whether the size is what we expect it to be
    _ASSERTE(dwSize == offsetof(UNWIND_INFO, UnwindCode) + pUnwindInfo->CountOfUnwindCodes * sizeof(UNWIND_CODE));
#ifndef REDHAWK
    pUnwindInfo->Flags = UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER;
#endif //REDHAWK

    pZapWriter->Write(pData, dwSize);

#ifndef REDHAWK
    DWORD dwPad = AlignmentPad(dwSize, sizeof(DWORD));
    if (dwPad != 0)
        pZapWriter->WritePad(dwPad);

    ULONG personalityRoutine = GetPersonalityRoutine(pImage)->GetRVA();
    pZapWriter->Write(&personalityRoutine, sizeof(personalityRoutine));
#endif //REDHAWK
}

#elif defined(_TARGET_ARM_)

UINT ZapUnwindData::GetAlignment()
{
    return sizeof(ULONG);
}

DWORD ZapUnwindData::GetSize()
{
    DWORD dwSize = ZapBlob::GetSize();

    // Add space for personality routine, it must be 4-byte aligned.
    // Everything in the UNWIND_INFO has already had its size included in size
    dwSize = AlignUp(dwSize, sizeof(ULONG));
    dwSize += sizeof(ULONG);

    return dwSize;
}

void ZapUnwindData::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    PVOID pData = GetData();
    DWORD dwSize = GetBlobSize();

    UNWIND_INFO * pUnwindInfo = (UNWIND_INFO *)pData;

    // Set the 'X' bit to indicate that there is a personality routine associated with this method
    *(LONG *)pUnwindInfo |= (1<<20);

    pZapWriter->Write(pData, dwSize);

    DWORD dwPad = AlignmentPad(dwSize, sizeof(DWORD));
    if (dwPad != 0)
        pZapWriter->WritePad(dwPad);

    ULONG personalityRoutine = GetPersonalityRoutine(pImage)->GetRVA();
#ifdef BINDER
    _ASSERTE((personalityRoutine & THUMB_CODE) == 0);
    personalityRoutine |= THUMB_CODE;
#endif
    pZapWriter->Write(&personalityRoutine, sizeof(personalityRoutine));
}

#else

UINT ZapUnwindData::GetAlignment()
{
    PORTABILITY_ASSERT("ZapUnwindData::GetAlignment");
    return sizeof(ULONG);
}

DWORD ZapUnwindData::GetSize()
{
    PORTABILITY_ASSERT("ZapUnwindData::GetSize");
    return -1;
}

void ZapUnwindData::Save(ZapWriter * pZapWriter)
{
    PORTABILITY_ASSERT("ZapUnwindData::Save");
}

#endif

ZapNode * ZapUnwindData::GetPersonalityRoutine(ZapImage * pImage)
{
    // Use different personality routine pointer for filter funclets so that we can quickly tell at runtime
    // whether funclet is a filter.
#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        ReadyToRunHelper helperNum = IsFilterFunclet() ? READYTORUN_HELPER_PersonalityRoutineFilterFunclet : READYTORUN_HELPER_PersonalityRoutine;
        return pImage->GetImportTable()->GetPlacedIndirectHelperThunk(helperNum);
    }
#endif
    return pImage->GetHelperThunk(IsFilterFunclet() ? CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET : CORINFO_HELP_EE_PERSONALITY_ROUTINE);
}

ZapUnwindData * ZapUnwindData::NewUnwindData(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, BOOL fIsFilterFunclet)
{
    SIZE_T cbAllocSize = sizeof(ZapUnwindData) + cbSize;

    void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize];

    ZapUnwindData * pZapUnwindData = fIsFilterFunclet ?
        (new (pMemory) ZapFilterFuncletUnwindData(cbSize)) : (new (pMemory) ZapUnwindData(cbSize));

    memcpy((void*)(pZapUnwindData + 1), pData, cbSize);

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)
    // Make sure the personality routine thunk is created
    pZapUnwindData->GetPersonalityRoutine(ZapImage::GetImage(pWriter));
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

    return pZapUnwindData;
}

ZapUnwindData * ZapUnwindDataTable::GetUnwindData(PVOID pBlob, SIZE_T cbBlob, BOOL fIsFilterFunclet)
{
    ZapUnwindData * pNode = (ZapUnwindData *)m_blobs.Lookup(ZapUnwindDataKey(pBlob, cbBlob, fIsFilterFunclet));

    if (pNode != NULL)
    {
        return pNode;
    }

    pNode = ZapUnwindData::NewUnwindData(m_pImage, pBlob, cbBlob, fIsFilterFunclet);
    m_blobs.Add(pNode);
    return pNode;
}
#endif // WIN64EXCEPTIONS

//
// ZapDebugInfo
//

ZapDebugInfo * ZapDebugInfoTable::GetDebugInfo(PVOID pBlob, SIZE_T cbBlob)
{
    ZapDebugInfo * pNode = m_blobs.Lookup(ZapBlob::SHashKey(pBlob, cbBlob));
    m_nCount++;

    if (pNode != NULL)
    {
        return pNode;
    }

    pNode = ZapBlob::NewBlob(m_pImage, pBlob, cbBlob);
    m_blobs.Add(pNode);
    return pNode;
}

void ZapDebugInfoTable::PrepareLayout()
{
    if (m_nCount == 0)
        return;

    // Make sure that the number of methods is odd number
    m_nCount |= 1;

    m_pTable = new (m_pImage->GetHeap()) ZapNode * [m_nCount];
}

void ZapDebugInfoTable::PlaceDebugInfo(ZapMethodHeader * pMethod)
{
    // Place the debug info blob if it is not placed yet
    ZapBlob * pDebugInfo = pMethod->GetDebugInfo();
    if (pDebugInfo == NULL)
    {
        return;
    }

    if (!pDebugInfo->IsPlaced())
    {
        m_pImage->m_pDebugSection->Place(pDebugInfo);
    }

    mdMethodDef md;
    IfFailThrow(m_pImage->GetCompileInfo()->GetMethodDef(pMethod->GetHandle(), &md));

    COUNT_T index = GetDebugRidEntryHash(md) % m_nCount;

    ZapNode * pHead = m_pTable[index];
    if (pHead == NULL)
    {
        // The common case - single rid entry.
        m_pTable[index] = pMethod;
        return;
    }

    // Create linked list of labelled entries if we do not have one yet
    if (pHead->GetType() != ZapNodeType_DebugInfoLabelledEntry)
    {
        m_pTable[index] = new (m_pImage->GetHeap()) LabelledEntry((ZapMethodHeader *)pHead);
    }

    // Insert the method at the end of the linked list
    LabelledEntry * pEntry = (LabelledEntry *)m_pTable[index];
    while (pEntry->m_pNext != NULL)
        pEntry = pEntry->m_pNext;

    pEntry->m_pNext = new (m_pImage->GetHeap()) LabelledEntry(pMethod);
}

void ZapDebugInfoTable::FinishLayout()
{
    // Go over the table again and place all labelled entries
    for (COUNT_T i = 0; i < m_nCount; i++)
    {
        ZapNode * pNode = m_pTable[i];

        if (pNode == NULL || pNode->GetType() != ZapNodeType_DebugInfoLabelledEntry)
            continue;

        LabelledEntry * pEntry = (LabelledEntry *)pNode;

        while (pEntry != NULL)
        {
            m_pImage->m_pDebugSection->Place(pEntry);
            pEntry = pEntry->m_pNext;
        }
    }
}

void ZapDebugInfoTable::Save(ZapWriter * pZapWriter)
{
    for (COUNT_T i = 0; i < m_nCount; i++)
    {
        CORCOMPILE_DEBUG_ENTRY entry = 0;

        ZapNode * pNode = m_pTable[i];

        if (pNode != NULL)
        {
            if (pNode->GetType() == ZapNodeType_DebugInfoLabelledEntry)
                entry |= pNode->GetRVA() | CORCOMPILE_DEBUG_MULTIPLE_ENTRIES;
            else
                entry = ((ZapMethodHeader *)pNode)->GetDebugInfo()->GetRVA();
        }

        pZapWriter->Write(&entry, sizeof(entry));
    }
}

void ZapDebugInfoTable::LabelledEntry::Save(ZapWriter * pZapWriter)
{
    CORCOMPILE_DEBUG_LABELLED_ENTRY entry;

    entry.nativeCodeRVA = m_pMethod->GetCode()->GetRVA();
    entry.debugInfoOffset = m_pMethod->GetDebugInfo()->GetRVA();

    if (m_pNext != NULL)
        entry.debugInfoOffset |= CORCOMPILE_DEBUG_MULTIPLE_ENTRIES;

    pZapWriter->Write(&entry, sizeof(entry));
}

#ifdef MDIL
const MdilDebugInfoTable::DebugInfo *MdilDebugInfoTable::GetDebugInfo(COUNT_T offset, COUNT_T cbBlob, const SArray<BYTE> *pBuf)
{
    DebugInfo info(offset, cbBlob, pBuf);
    DebugInfo *pNode = m_blobs.Lookup(&info);
    if (pNode != NULL)
    {
        return pNode;
    }

    pNode = new (m_pImage->GetHeap()) DebugInfo(offset, cbBlob, pBuf);
    m_blobs.Add(pNode);
    return pNode;
}
#endif // MDIL

//
// ZapProfileData
//
void ZapProfileData::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    CORCOMPILE_METHOD_PROFILE_LIST profileData;

    ZeroMemory(&profileData, sizeof(CORCOMPILE_METHOD_PROFILE_LIST));

    if (m_pNext != NULL)
        pImage->WriteReloc(&profileData,
                           offsetof(CORCOMPILE_METHOD_PROFILE_LIST, next),
                           m_pNext, 0, IMAGE_REL_BASED_PTR);

    pZapWriter->Write(&profileData, sizeof(CORCOMPILE_METHOD_PROFILE_LIST));
}


// Zapping of ExeptionInfoTable
ZapExceptionInfoLookupTable::ZapExceptionInfoLookupTable(ZapImage *pImage) : m_pImage(pImage)
{
    _ASSERTE(m_pImage->m_pExceptionSection != NULL);
    m_pImage->m_pExceptionSection->Place(this);
}

void ZapExceptionInfoLookupTable::PlaceExceptionInfoEntry(ZapNode* pCode, ZapExceptionInfo* pExceptionInfo)
{
    ExceptionInfoEntry entry;
    entry.m_pCode = pCode;
    entry.m_pExceptionInfo = pExceptionInfo;
    m_exceptionInfoEntries.Append(entry);
    m_pImage->m_pExceptionSection->Place(pExceptionInfo);
}

DWORD ZapExceptionInfoLookupTable::GetSize()
{
    if (m_exceptionInfoEntries.GetCount() == 0)
        return 0;

    DWORD numExceptionInfoEntries = m_exceptionInfoEntries.GetCount();
    // 1 sentential entry at the end of the table.
    return (numExceptionInfoEntries + 1) * sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY);
}

void ZapExceptionInfoLookupTable::Save(ZapWriter* pZapWriter) 
{

    if(m_exceptionInfoEntries.GetCount() == 0)
        return;

    for(COUNT_T i = 0; i < m_exceptionInfoEntries.GetCount(); ++i)
    {
        DWORD methodStartRVA = m_exceptionInfoEntries[i].m_pCode->GetRVA();

        ZapExceptionInfo* pExceptionInfo = m_exceptionInfoEntries[i].m_pExceptionInfo;

        CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY lookupEntry;

        lookupEntry.MethodStartRVA = methodStartRVA;
        lookupEntry.ExceptionInfoRVA = pExceptionInfo->GetRVA();

        pZapWriter->Write(&lookupEntry, sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));

#ifdef _DEBUG
        // Make sure there are no gaps between 2 consecutive CORCOMPILE_EXCEPTION_CLAUSE
        // We use pointer arithmatic to calculate the number of EHClause for a method.
        if (i != 0)
        {
            ZapExceptionInfo* pPreviousExceptionInfo =  m_exceptionInfoEntries[i-1].m_pExceptionInfo;
            DWORD size = pExceptionInfo->GetRVA() - pPreviousExceptionInfo->GetRVA();
            DWORD ehClauseSize = size % sizeof(CORCOMPILE_EXCEPTION_CLAUSE);
            CONSISTENCY_CHECK_MSG(ehClauseSize == 0, "There must be no gaps between 2 successive clause arrays, please check ZapExceptionInfo alignment");
        }
#endif 
    }

    // write a sentinal entry.. this entry helps to find the number of EHClauses for the last entry
    CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY sentinalEntry;

    ExceptionInfoEntry lastEntry = m_exceptionInfoEntries[m_exceptionInfoEntries.GetCount() -1];

    ZapExceptionInfo* pLastExceptionInfo = lastEntry.m_pExceptionInfo;

    sentinalEntry.MethodStartRVA = (DWORD)-1;

    // points just after the end of the Exception table
    // the sentinal node m_pExceptionInfo pointer actually points to an invalid CORCOMPILE_EXCEPTION_CLAUSE 
    // area.  The lookup algorithm will never dereference the sentinal pointer, and hence this is safe
    sentinalEntry.ExceptionInfoRVA = pLastExceptionInfo->GetRVA() + pLastExceptionInfo->GetSize();

    pZapWriter->Write(&sentinalEntry, sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
}


DWORD ZapUnwindInfoLookupTable::GetSize()
{
    // Sentinal entry at the end
    return (GetNumEntries() + 1) * sizeof (DWORD);
}

void ZapUnwindInfoLookupTable::Save(ZapWriter* pZapWriter)
{
    ZapVirtualSection * pRuntimeFunctionSection = m_pRuntimeFunctionSection;

    // Create Lookup entries.
    // 1 lookup entry for each RUNTIME_FUNCTION_LOOKUP_STRIDE K of code.
    COUNT_T nUnwindInfos = pRuntimeFunctionSection->GetNodeCount();

    DWORD dwCodeSectionStartAddress = m_pCodeSection->GetRVA();

    DWORD nLookupEntries = 0;
    DWORD entry;

    DWORD nTotalLookupEntries = GetNumEntries();

    // write out the first entry
    entry = 0;
    pZapWriter->Write(&entry, sizeof(DWORD));
    nLookupEntries++;
    if (nLookupEntries == nTotalLookupEntries)
        goto WriteSentinel;

    for (COUNT_T i = 1; i < nUnwindInfos; ++i)
    {
        ZapUnwindInfo* pUnwindInfo = (ZapUnwindInfo*)pRuntimeFunctionSection->GetNode(i);
        DWORD RelativePC = pUnwindInfo->GetStartAddress() - dwCodeSectionStartAddress;

        COUNT_T iCurrentIndex = RelativePC / RUNTIME_FUNCTION_LOOKUP_STRIDE;

        // Note that we should not be using pUnwindInfo->GetEndAddress() here. The binary search 
        // in the VM that's accelerated by this table does not look at the EndAddress either, and 
        // so not using EndAddress here assures consistency.
        COUNT_T iPreviousIndex = (RelativePC - 1)/ RUNTIME_FUNCTION_LOOKUP_STRIDE;

        while(iPreviousIndex >= nLookupEntries)
        {
            entry = i - 1;
            pZapWriter->Write(&entry, sizeof(DWORD));
            nLookupEntries++;
            if (nLookupEntries == nTotalLookupEntries)
                goto WriteSentinel;
        }

        if (iCurrentIndex == nLookupEntries)
        {
            entry = i;
            pZapWriter->Write(&entry, sizeof(DWORD));
            nLookupEntries++;
            if (nLookupEntries == nTotalLookupEntries)
                goto WriteSentinel;
        }
    }

WriteSentinel:
    // There should always be one sentinel entry at the end. The sentinel entry will
    // be good to cover the rest of the section to account for extra padding.
    _ASSERTE(nLookupEntries <= nTotalLookupEntries);

    while (nLookupEntries <= nTotalLookupEntries)
    {
        entry = nUnwindInfos - 1;
        pZapWriter->Write(&entry, sizeof (DWORD));
        nLookupEntries ++;
    }
}

DWORD ZapColdCodeMap::GetSize()
{
    return m_pRuntimeFunctionSection->GetNodeCount() * sizeof(CORCOMPILE_COLD_METHOD_ENTRY);
}

void ZapColdCodeMap::Save(ZapWriter* pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    ZapNode * pPendingCode = NULL;
    COUNT_T curMethod = 0;

    COUNT_T nUnwindInfos = m_pRuntimeFunctionSection->GetNodeCount();
    for (COUNT_T i = 0; i < nUnwindInfos; ++i)
    {
        CORCOMPILE_COLD_METHOD_ENTRY entry;

        ZapUnwindInfo* pUnwindInfo = (ZapUnwindInfo*)m_pRuntimeFunctionSection->GetNode(i);

#ifdef WIN64EXCEPTIONS
        if (pUnwindInfo->GetCode() == pPendingCode)
        {
            entry.mainFunctionEntryRVA = 0;
            entry.hotCodeSize = 0;
        }
        else
#endif
        {
            pPendingCode = pUnwindInfo->GetCode();

            ZapMethodHeader * pMethod;

            for (;;)
            {
                pMethod = pImage->m_MethodCompilationOrder[curMethod];
                if (pMethod->m_pColdCode == pPendingCode)
                    break;
                curMethod++;
            }

#ifdef WIN64EXCEPTIONS
            entry.mainFunctionEntryRVA = pMethod->m_pUnwindInfo->GetRVA();
#endif

            entry.hotCodeSize = pMethod->m_pCode->GetSize();
        }

        pZapWriter->Write(&entry, sizeof(entry));
    }
}

DWORD ZapHelperThunk::GetSize()
{
    return (m_dwHelper & CORCOMPILE_HELPER_PTR) ? sizeof(TADDR) : HELPER_TABLE_ENTRY_LEN;
}

void ZapHelperThunk::Save(ZapWriter * pZapWriter)
{
#ifdef _DEBUG
    LOG((LF_ZAP, LL_INFO1000000, "Emitting JIT helper table entry for helper %3d (%s)\n",
        (USHORT) m_dwHelper, s_rgHelperNames[(USHORT) m_dwHelper]));
#endif // _DEBUG

    // Save the index of the helper, the actual code for the thunk will be generated at runtime
    pZapWriter->Write(&m_dwHelper, sizeof(DWORD));

    DWORD pad = GetSize() - sizeof(DWORD);
    if (pad > 0)
    {
        void * pPad = _alloca(pad);
        memset(pPad, DEFAULT_CODE_BUFFER_INIT, pad);
        pZapWriter->Write(pPad, pad);
    }
}

void ZapLazyHelperThunk::Place(ZapImage * pImage)
{
    m_pArg = pImage->m_pPreloadSections[CORCOMPILE_SECTION_MODULE];

    m_pTarget = pImage->GetHelperThunk(m_dwHelper);

    pImage->m_pLazyHelperSection->Place(this);
}

DWORD ZapLazyHelperThunk::GetSize()
{
    return SaveWorker(NULL);
}

void ZapLazyHelperThunk::Save(ZapWriter * pZapWriter)
{
    SaveWorker(pZapWriter);
}

DWORD ZapLazyHelperThunk::SaveWorker(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    BYTE buffer[42]; // Buffer big enough to hold any reasonable helper thunk sequence
    BYTE * p = buffer;

#if defined(_TARGET_X86_)
    // mov edx, module
    *p++ = 0xBA;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pArg, 0, IMAGE_REL_BASED_PTR);
    p += 4;

    // jmp JIT_StrCns
    *p++ = 0xE9;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pTarget, 0, IMAGE_REL_BASED_REL32);
    p += 4;
#elif defined(_TARGET_AMD64_)
    // lea rdx, module
    *p++ = 0x48;
    *p++ = 0x8D;
    *p++ = 0x15;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pArg, 0, IMAGE_REL_BASED_REL32);
    p += 4;

    // jmp JIT_StrCns
    *p++ = 0xE9;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pTarget, 0, IMAGE_REL_BASED_REL32);
    p += 4;
#elif defined(_TARGET_ARM_)
    // movw r1, module
    *(WORD *)(p + 0) = 0xf240;
    *(WORD *)(p + 2) = 1 << 8;
    // movt r1, module
    *(WORD *)(p + 4) = 0xf2c0;
    *(WORD *)(p + 6) = 1 << 8;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pArg, 0, IMAGE_REL_BASED_THUMB_MOV32);
    p += 8;

    // b JIT_StrCns
    *(WORD *)(p + 0) = 0xf000;
    *(WORD *)(p + 2) = 0xb800;
    if (pImage != NULL)
        pImage->WriteReloc(buffer, (int)(p - buffer), m_pTarget, 0, IMAGE_REL_BASED_THUMB_BRANCH24);
    p += 4;
#else
    PORTABILITY_ASSERT("ZapLazyHelperThunk::Save");
#endif

    _ASSERTE(p - buffer <= sizeof(buffer));

    if (pZapWriter != NULL)
        pZapWriter->Write(&buffer, (int)(p - buffer));

    return (DWORD) (p - buffer);
}
