// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapMetadata.cpp
//

//
// Metadata zapping
//
// ======================================================================================

#include "common.h"

#include "zapmetadata.h"

//-----------------------------------------------------------------------------
//
// ZapMetaData is the barebone ZapNode to save metadata scope
//

void ZapMetaData::SetMetaData(IUnknown * pEmit)
{
    _ASSERTE(m_pEmit == NULL);
    _ASSERTE(pEmit != NULL);

    IfFailThrow(pEmit->QueryInterface(IID_IMetaDataEmit, (void **)&m_pEmit));
}

DWORD ZapMetaData::GetSize()
{
    if (m_dwSize == 0)
    {
        IfFailThrow(m_pEmit->GetSaveSize(cssAccurate, &m_dwSize));
        _ASSERTE(m_dwSize != 0);
    }
    return m_dwSize;
}

void ZapMetaData::Save(ZapWriter * pZapWriter)
{
    IfFailThrow(m_pEmit->SaveToStream(pZapWriter, 0));
}

//-----------------------------------------------------------------------------
//
// ZapILMetaData copies both the metadata and IL to the NGEN image.
//

void ZapILMetaData::Save(ZapWriter * pZapWriter)
{
    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);

    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
    {
        DWORD flags;
        ULONG rva;
        IfFailThrow(pMDImport->GetMethodImplProps(md, &rva, &flags));

        if (!IsMiIL(flags) || (rva == 0))
            continue;

        // Set the actual RVA of the method
        const ILMethod * pILMethod = m_ILMethods.LookupPtr(md);

        IfFailThrow(m_pEmit->SetRVA(md, (pILMethod != NULL) ? pILMethod->m_pIL->GetRVA() : 0));
    }

    if (IsReadyToRunCompilation())
    {
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumAllInit(mdtFieldDef);

        mdFieldDef fd;
        while (pMDImport->EnumNext(&hEnum, &fd))
        {
            DWORD dwRVA = 0;
            if (pMDImport->GetFieldRVA(fd, &dwRVA) == S_OK)
            {
                PVOID pData = NULL;
                DWORD cbSize = 0;
                DWORD cbAlignment = 0;

                m_pImage->m_pPreloader->GetRVAFieldData(fd, &pData, &cbSize, &cbAlignment);

                ZapRVADataNode * pRVADataNode = m_rvaData.Lookup(pData);
                m_pEmit->SetRVA(fd, pRVADataNode->GetRVA());
            }
        }
    }
    else
    {
       ZapImage::GetImage(pZapWriter)->m_pPreloader->SetRVAsForFields(m_pEmit);
    }

    ZapMetaData::Save(pZapWriter);
}

ZapRVADataNode * ZapILMetaData::GetRVAField(void * pData)
{
    ZapRVADataNode * pRVADataNode = m_rvaData.Lookup(pData);

    if (pRVADataNode == NULL)
    {
        pRVADataNode = new (m_pImage->GetHeap()) ZapRVADataNode(pData);

        m_rvaData.Add(pRVADataNode);
    }

    return pRVADataNode;
}

struct RVAField
{
    PVOID pData;
    DWORD cbSize;
    DWORD cbAlignment;
};

// Used by qsort
int __cdecl RVAFieldCmp(const void * a_, const void * b_)
{
    RVAField * a = (RVAField *)a_;
    RVAField * b = (RVAField *)b_;

    if (a->pData != b->pData)
    {
        return (a->pData > b->pData) ? 1 : -1;
    }

    return 0;
}

void ZapILMetaData::CopyRVAFields()
{
    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtFieldDef);

    SArray<RVAField> fields;

    mdFieldDef fd;
    while (pMDImport->EnumNext(&hEnum, &fd))
    {
        DWORD dwRVA = 0;
        if (pMDImport->GetFieldRVA(fd, &dwRVA) == S_OK)
        {
            RVAField field;
            m_pImage->m_pPreloader->GetRVAFieldData(fd, &field.pData, &field.cbSize, &field.cbAlignment);
            fields.Append(field);
        }
    }

    if (fields.GetCount() == 0)
        return;

    // Managed C++ binaries depend on the order of RVA fields
    qsort(&fields[0], fields.GetCount(), sizeof(RVAField), RVAFieldCmp);

#ifdef _DEBUG
    for (COUNT_T i = 0; i < fields.GetCount(); i++)
    {
        // Make sure no RVA field node has been placed during compilation. This would mess up the ordering
        // and can potentially break the Managed C++ scenarios.
        _ASSERTE(!GetRVAField(fields[i].pData)->IsPlaced());
    }
#endif

    for (COUNT_T i = 0; i < fields.GetCount(); i++)
    {
        RVAField field = fields[i];

        ZapRVADataNode * pRVADataNode = GetRVAField(field.pData);

        // Handle overlapping fields by reusing blobs based on the address, and just updating size and alignment.
        pRVADataNode->UpdateSizeAndAlignment(field.cbSize, field.cbAlignment);

        if (!pRVADataNode->IsPlaced())
            m_pImage->m_pReadOnlyDataSection->Place(pRVADataNode);
    }
}

void ZapILMetaData::CopyIL()
{
    // The IL is emited into NGen image in the following priority order:
    //  1. Public inlineable method (may be needed by JIT inliner)
    //  2. Generic method (may be needed to compile non-NGened instantiations)
    //  3. Other potentially warm instances (private inlineable methods, methods that failed to NGen)
    //  4. Everything else (should be touched in rare scenarios like reflection or profiling only)

    SArray<ZapBlob *> priorityLists[CORCOMPILE_ILREGION_COUNT];

    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);

    //
    // Build the list for each priority in first pass, and then place
    // the IL blobs in each list. The two passes are needed because of
    // interning of IL blobs (one IL blob can be on multiple lists).
    //

    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
    {
        const ILMethod * pILMethod = m_ILMethods.LookupPtr(md);

        if (pILMethod == NULL)
            continue;

        CorCompileILRegion region = m_pImage->m_pPreloader->GetILRegion(md);
        _ASSERTE(region < CORCOMPILE_ILREGION_COUNT);

        // Preallocate space to avoid wasting too much time by reallocations
        if (priorityLists[region].IsEmpty())
            priorityLists[region].Preallocate(m_ILMethods.GetCount() / 16);

        priorityLists[region].Append(pILMethod->m_pIL);
    }

    for (int iList = 0; iList < CORCOMPILE_ILREGION_COUNT; iList++)
    {
        SArray<ZapBlob *> & priorityList = priorityLists[iList];

        // Use just one section for IL for now. Once the touches of IL for method preparation are fixed change it to:
        // ZapVirtualSection * pSection = (iList == CORCOMPILE_ILREGION_COLD) ? m_pImage->m_pColdILSection : m_pImage->m_pILSection;

        ZapVirtualSection * pSection = m_pImage->m_pILSection;

        COUNT_T nBlobs = priorityList.GetCount();
        for (COUNT_T iBlob = 0; iBlob < nBlobs; iBlob++)
        {
            ZapBlob * pIL = priorityList[iBlob];
            if (!pIL->IsPlaced())
                pSection->Place(pIL);
        }
    }
}

void ZapILMetaData::CopyMetaData()
{
    //
    // Copy metadata from IL image and open it so we can update IL rva's
    //

    COUNT_T cMeta;
    const void *pMeta = m_pImage->m_ModuleDecoder.GetMetadata(&cMeta);

    IMetaDataDispenserEx * pMetaDataDispenser = m_pImage->m_zapper->m_pMetaDataDispenser;

    //
    // Transfer the metadata version string from IL image to native image
    //
    LPCSTR pRuntimeVersionString;
    IfFailThrow(GetImageRuntimeVersionString((PVOID)pMeta, &pRuntimeVersionString));

    SString ssRuntimeVersion;
    ssRuntimeVersion.SetUTF8(pRuntimeVersionString);

    BSTRHolder strVersion(SysAllocString(ssRuntimeVersion.GetUnicode()));

    VARIANT versionOption;
    V_VT(&versionOption) = VT_BSTR;
    V_BSTR(&versionOption) = strVersion;
    IfFailThrow(pMetaDataDispenser->SetOption(MetaDataRuntimeVersion, &versionOption));

    // Preserve local refs. WinMD adapter depends on them at runtime.
    VARIANT preserveLocalRefsOption;
    V_VT(&preserveLocalRefsOption) = VT_UI4;
    V_UI4(&preserveLocalRefsOption) = MDPreserveLocalTypeRef | MDPreserveLocalMemberRef;
    IfFailThrow(pMetaDataDispenser->SetOption(MetaDataPreserveLocalRefs, &preserveLocalRefsOption));

    // ofNoTransform - Get the raw metadata for WinRT, not the adapter view
    HRESULT hr = pMetaDataDispenser->OpenScopeOnMemory(pMeta, cMeta,
                                                       ofWrite | ofNoTransform,
                                                       IID_IMetaDataEmit,
                                                       (IUnknown **) &m_pEmit);
    if (hr == CLDB_E_BADUPDATEMODE)
    {
        // This must be incrementally-updated metadata. It needs to be opened
        // specially.
        VARIANT incOption;
        V_VT(&incOption) = VT_UI4;
        V_UI4(&incOption) = MDUpdateIncremental;
        IfFailThrow(pMetaDataDispenser->SetOption(MetaDataSetUpdate, &incOption));

        hr = pMetaDataDispenser->OpenScopeOnMemory(pMeta, cMeta,
                                                   ofWrite | ofNoTransform,
                                                   IID_IMetaDataEmit,
                                                   (IUnknown **) &m_pEmit);
    }

    // Check the result of OpenScopeOnMemory()
    IfFailThrow(hr);

    if (!IsReadyToRunCompilation())
    {
        // Communicate the profile data to the meta data emitter so it can hot/cold split it
        NonVMComHolder<IMetaDataCorProfileData> pIMetaDataCorProfileData;
        IfFailThrow(m_pEmit->QueryInterface(IID_IMetaDataCorProfileData,
                                            (void**)&pIMetaDataCorProfileData));

        // unless we're producing an instrumented version - the IBC logging for meta data doesn't
        // work for the hot/cold split version.
        if (m_pImage->m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR))
            IfFailThrow(pIMetaDataCorProfileData->SetCorProfileData(NULL));
        else
            IfFailThrow(pIMetaDataCorProfileData->SetCorProfileData(m_pImage->GetProfileData()));
    }

    // If we are ngening with the tuning option, the IBC data that is
    // generated gets reordered and may be  inconsistent with the
    // metadata in the original IL image. Let's just skip that case.
    if (!m_pImage->m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR))
    {
        // Communicate the reordering option for saving
        NonVMComHolder<IMDInternalMetadataReorderingOptions> pIMDInternalMetadataReorderingOptions;
        IfFailThrow(m_pEmit->QueryInterface(IID_IMDInternalMetadataReorderingOptions,
                                            (void**)&pIMDInternalMetadataReorderingOptions));
        IfFailThrow(pIMDInternalMetadataReorderingOptions->SetMetaDataReorderingOptions(ReArrangeStringPool));
    }
}

// Emit IL for a method def into the ngen image
void ZapILMetaData::EmitMethodIL(mdMethodDef md)
{
    DWORD flags;
    ULONG rva;
    IfFailThrow(m_pImage->m_pMDImport->GetMethodImplProps(md, &rva, &flags));

    if (!IsMiIL(flags) || (rva == 0))
        return;

    if (!m_pImage->m_ModuleDecoder.CheckILMethod(rva))
        IfFailThrow(COR_E_BADIMAGEFORMAT); // BFA_BAD_IL_RANGE

    PVOID pMethod = (PVOID)m_pImage->m_ModuleDecoder.GetRvaData(rva);

    SIZE_T cMethod = PEDecoder::ComputeILMethodSize((TADDR)pMethod);

    //
    // Emit copy of IL method in native image.
    //
    ZapBlob * pIL = m_blobs.Lookup(ZapBlob::SHashKey(pMethod, cMethod));

    if (pIL == NULL)
    {
        pIL = new (m_pImage->GetHeap()) ILBlob(pMethod, cMethod);

        m_blobs.Add(pIL);
    }

    ILMethod ilMethod;
    ilMethod.m_md = md;
    ilMethod.m_pIL = pIL;
    m_ILMethods.Add(ilMethod);
}
