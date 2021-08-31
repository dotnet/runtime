// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// TargetTypes.cpp
//

//
//*****************************************************************************

#include "stdafx.h"
#include "targettypes.h"


Target_CMiniMdSchemaBase::Target_CMiniMdSchemaBase() :
m_ulReserved(0),
m_major(0),
m_minor(0),
m_heaps(0),
m_rid(0),
m_maskvalid(0),
m_sorted(0)
{}

HRESULT Target_CMiniMdSchemaBase::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    reader.Align(8); // this type needs 8 byte alignment from m_maskvalid
    IfFailRet(reader.Read32(&m_ulReserved));
    IfFailRet(reader.Read8(&m_major));
    IfFailRet(reader.Read8(&m_minor));
    IfFailRet(reader.Read8(&m_heaps));
    IfFailRet(reader.Read8(&m_rid));
    IfFailRet(reader.Read64(&m_maskvalid));
    IfFailRet(reader.Read64(&m_sorted));
    return S_OK;
}

Target_CMiniMdSchema::Target_CMiniMdSchema() :
m_ulExtra(0)
{
    memset(&m_cRecs, 0, TBL_COUNT*sizeof(ULONG32));
}

HRESULT Target_CMiniMdSchema::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CMiniMdSchemaBase::ReadFrom(reader));
    reader.AlignBase();
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.Read32(&(m_cRecs[i])));
    IfFailRet(reader.Read32(&m_ulExtra));
    return S_OK;
}

Target_CMiniColDef::Target_CMiniColDef() :
m_Type(0),
m_oColumn(0),
m_cbColumn(0)
{
}

HRESULT Target_CMiniColDef::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.Read8(&m_Type));
    IfFailRet(reader.Read8(&m_oColumn));
    IfFailRet(reader.Read8(&m_cbColumn));
    return S_OK;
}

Target_CMiniTableDef::Target_CMiniTableDef() :
m_pColDefs(NULL),
m_cCols(0),
m_iKey(0),
m_cbRec(0)
{}

HRESULT Target_CMiniTableDef::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    CORDB_ADDRESS pColDefs = NULL;
    IfFailRet(reader.ReadPointer(&pColDefs));
    IfFailRet(reader.Read8(&m_cCols));
    IfFailRet(reader.Read8(&m_iKey));
    IfFailRet(reader.Read8(&m_cbRec));

    // sanity check before allocating
    if (m_cCols > 100)
        return CLDB_E_FILE_CORRUPT;
    m_pColDefs = new (nothrow) Target_CMiniColDef[m_cCols];
    if (m_pColDefs == NULL)
        return E_OUTOFMEMORY;
    DataTargetReader colsReader = reader.CreateReaderAt(pColDefs);
    for (int i = 0; i < m_cCols; i++)
    {
        IfFailRet(colsReader.Read(&m_pColDefs[i]));
    }

    return S_OK;
}

Target_CMiniMdBase::Target_CMiniMdBase() :
m_TblCount(0),
m_fVerifiedByTrustedSource(FALSE),
m_iStringsMask(0),
m_iGuidsMask(0),
m_iBlobsMask(0)
{}

HRESULT Target_CMiniMdBase::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.SkipPointer());     // vtable
    IfFailRet(reader.Read(&m_Schema));
    IfFailRet(reader.Read32(&m_TblCount));
    IfFailRet(reader.Read32((ULONG32*)&m_fVerifiedByTrustedSource));
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.Read(&(m_TableDefs[i])));
    IfFailRet(reader.Read32(&m_iStringsMask));
    IfFailRet(reader.Read32(&m_iGuidsMask));
    IfFailRet(reader.Read32(&m_iBlobsMask));
    return S_OK;
}

Target_MapSHash::Target_MapSHash() :
m_table(0),
m_tableSize(0),
m_tableCount(0),
m_tableOccupied(0),
m_tableMax(0)
{}

HRESULT Target_MapSHash::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.ReadPointer(&m_table));
    IfFailRet(reader.Read32(&m_tableSize));
    IfFailRet(reader.Read32(&m_tableCount));
    IfFailRet(reader.Read32(&m_tableOccupied));
    IfFailRet(reader.Read32(&m_tableMax));
    return S_OK;
}



Target_StgPoolSeg::Target_StgPoolSeg() :
m_pSegData(0),
m_pNextSeg(0),
m_cbSegSize(0),
m_cbSegNext(0)
{}

HRESULT Target_StgPoolSeg::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.ReadPointer(&m_pSegData));
    IfFailRet(reader.ReadPointer(&m_pNextSeg));
    IfFailRet(reader.Read32(&m_cbSegSize));
    IfFailRet(reader.Read32(&m_cbSegNext));
    return S_OK;
}


Target_CChainedHash::Target_CChainedHash() :
m_rgData(0),
m_iBuckets(0),
m_iSize(0),
m_iCount(0),
m_iMaxChain(0),
m_iFree(0)
{}

HRESULT Target_CChainedHash::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.SkipPointer()); // __vfptr
    IfFailRet(reader.ReadPointer(&m_rgData));
    IfFailRet(reader.Read32(&m_iBuckets));
    IfFailRet(reader.Read32(&m_iSize));
    IfFailRet(reader.Read32(&m_iCount));
    IfFailRet(reader.Read32(&m_iMaxChain));
    IfFailRet(reader.Read32(&m_iFree));
    return S_OK;
}

Target_CStringPoolHash::Target_CStringPoolHash() :
m_Pool(0)
{}

HRESULT Target_CStringPoolHash::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CChainedHash::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.ReadPointer(&m_Pool));
    return S_OK;
}

Target_CBlobPoolHash::Target_CBlobPoolHash() :
m_Pool(0)
{}

HRESULT Target_CBlobPoolHash::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CChainedHash::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.ReadPointer(&m_Pool));
    return S_OK;
}


Target_CGuidPoolHash::Target_CGuidPoolHash() :
m_Pool(0)
{ }

HRESULT Target_CGuidPoolHash::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CChainedHash::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.ReadPointer(&m_Pool));
    return S_OK;
}

HRESULT Target_StgPoolReadOnly::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.SkipPointer()); // __vfptr
    IfFailRet(Target_StgPoolSeg::ReadFrom(reader));
    reader.AlignBase();
    return S_OK;
}

Target_StgPool::Target_StgPool() :
m_ulGrowInc(0),
m_pCurSeg(0),
m_cbCurSegOffset(0),
m_bFree(FALSE),
m_bReadOnly(FALSE),
m_nVariableAlignmentMask(0),
m_cbStartOffsetOfEdit(0),
m_fValidOffsetOfEdit(FALSE)
{}

HRESULT Target_StgPool::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_StgPoolReadOnly::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read32(&m_ulGrowInc));
    IfFailRet(reader.ReadPointer(&m_pCurSeg));
    IfFailRet(reader.Read32(&m_cbCurSegOffset));
    ULONG32 bitField;
    IfFailRet(reader.Read32(&bitField));
    m_bFree = (bitField & 0x1) != 0;
    m_bReadOnly = (bitField & 0x2) != 0;
    IfFailRet(reader.Read32(&m_nVariableAlignmentMask));
    IfFailRet(reader.Read32(&m_cbStartOffsetOfEdit));
    IfFailRet(reader.Read8((BYTE*)&m_fValidOffsetOfEdit));
    return S_OK;
}

HRESULT Target_StgBlobPool::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_StgPool::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read(&m_Hash));
    return S_OK;
}

Target_StgStringPool::Target_StgStringPool() :
m_bHash(FALSE)
{
}

HRESULT Target_StgStringPool::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_StgPool::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read(&m_Hash));
    IfFailRet(reader.Read8((BYTE*)&m_bHash));
    return S_OK;
}

Target_StgGuidPool::Target_StgGuidPool() :
m_bHash(FALSE)
{}

HRESULT Target_StgGuidPool::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_StgPool::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read(&m_Hash));
    IfFailRet(reader.Read8((BYTE*)&m_bHash));
    return S_OK;
}

Target_RecordPool::Target_RecordPool() :
m_cbRec(0)
{}

HRESULT Target_RecordPool::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_StgPool::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read32(&m_cbRec));
    return S_OK;
}

Target_OptionValue::Target_OptionValue() :
m_DupCheck(0),
m_RefToDefCheck(0),
m_NotifyRemap(0),
m_UpdateMode(0),
m_ErrorIfEmitOutOfOrder(0),
m_ThreadSafetyOptions(0),
m_ImportOption(0),
m_LinkerOption(0),
m_GenerateTCEAdapters(0),
m_RuntimeVersion(0),
m_MetadataVersion(0),
m_MergeOptions(0),
m_InitialSize(0),
m_LocalRefPreservation(0)
{}

HRESULT Target_OptionValue::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.Read32(&m_DupCheck));
    IfFailRet(reader.Read32(&m_RefToDefCheck));
    IfFailRet(reader.Read32(&m_NotifyRemap));
    IfFailRet(reader.Read32(&m_UpdateMode));
    IfFailRet(reader.Read32(&m_ErrorIfEmitOutOfOrder));
    IfFailRet(reader.Read32(&m_ThreadSafetyOptions));
    IfFailRet(reader.Read32(&m_ImportOption));
    IfFailRet(reader.Read32(&m_LinkerOption));
    IfFailRet(reader.Read32(&m_GenerateTCEAdapters));
    IfFailRet(reader.ReadPointer(&m_RuntimeVersion));
    IfFailRet(reader.Read32(&m_MetadataVersion));
    IfFailRet(reader.Read32(&m_MergeOptions));
    IfFailRet(reader.Read32(&m_InitialSize));
    IfFailRet(reader.Read32(&m_LocalRefPreservation));
    return S_OK;
}

Target_CMiniMdRW::Target_CMiniMdRW() :
m_pMemberRefHash(0),
m_pMemberDefHash(0),
m_pNamedItemHash(0),
m_maxRid(0),
m_limRid(0),
m_maxIx(0),
m_limIx(0),
m_eGrow(0),
m_pHandler(0),
m_cbSaveSize(0),
m_fIsReadOnly(FALSE),
m_bPreSaveDone(FALSE),
m_bSaveCompressed(FALSE),
m_bPostGSSMod(FALSE),
m_pMethodMap(0),
m_pFieldMap(0),
m_pPropertyMap(0),
m_pEventMap(0),
m_pParamMap(0),
m_pFilterTable(0),
m_pHostFilter(0),
m_pTokenRemapManager(0),
dbg_m_pLock(0),
m_fMinimalDelta(FALSE),
m_rENCRecs(0)
{
    memset(&m_pLookUpHashs, 0, TBL_COUNT*sizeof(CORDB_ADDRESS));
    memset(&m_pVS, 0, TBL_COUNT*sizeof(CORDB_ADDRESS));
    memset(&m_bSortable, 0, TBL_COUNT*sizeof(BOOL));
}

HRESULT Target_CMiniMdRW::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CMiniMdTemplate_CMiniMdRW::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.ReadPointer(&m_pMemberRefHash));
    IfFailRet(reader.ReadPointer(&m_pMemberDefHash));
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.ReadPointer(&m_pLookUpHashs[i]));
    IfFailRet(reader.Read(&m_StringPoolOffsetHash));
    IfFailRet(reader.ReadPointer(&m_pNamedItemHash));
    IfFailRet(reader.Read32(&m_maxRid));
    IfFailRet(reader.Read32(&m_limRid));
    IfFailRet(reader.Read32(&m_maxIx));
    IfFailRet(reader.Read32(&m_limIx));
    IfFailRet(reader.Read32(&m_eGrow));
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.Read(&m_Tables[i]));
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.ReadPointer(&m_pVS[i]));
    IfFailRet(reader.Read(&m_StringHeap));
    IfFailRet(reader.Read(&m_BlobHeap));
    IfFailRet(reader.Read(&m_UserStringHeap));
    IfFailRet(reader.Read(&m_GuidHeap));
    IfFailRet(reader.ReadPointer(&m_pHandler));
    IfFailRet(reader.Read32(&m_cbSaveSize));
    ULONG32 bitField;
    IfFailRet(reader.Read32(&bitField));
    m_fIsReadOnly = (bitField & 0x1) != 0;
    m_bPreSaveDone = (bitField & 0x2) != 0;
    m_bSaveCompressed = (bitField & 0x4) != 0;
    m_bPostGSSMod = (bitField & 0x8) != 0;
    IfFailRet(reader.ReadPointer(&m_pMethodMap));
    IfFailRet(reader.ReadPointer(&m_pFieldMap));
    IfFailRet(reader.ReadPointer(&m_pPropertyMap));
    IfFailRet(reader.ReadPointer(&m_pEventMap));
    IfFailRet(reader.ReadPointer(&m_pParamMap));
    IfFailRet(reader.ReadPointer(&m_pFilterTable));
    IfFailRet(reader.ReadPointer(&m_pHostFilter));
    IfFailRet(reader.ReadPointer(&m_pTokenRemapManager));
    IfFailRet(reader.Read(&m_OptionValue));
    IfFailRet(reader.Read(&m_StartupSchema));
    for (int i = 0; i < TBL_COUNT; i++)
        IfFailRet(reader.Read8((BYTE*)&m_bSortable[i]));
    if (reader.IsDefined(1)) // replace this with DEFINE__DEBUG
    {
        IfFailRet(reader.ReadPointer(&dbg_m_pLock));
    }
    IfFailRet(reader.Read8((BYTE*)&m_fMinimalDelta));
    IfFailRet(reader.ReadPointer(&m_rENCRecs));
    return S_OK;
}



Target_CLiteWeightStgdb_CMiniMdRW::Target_CLiteWeightStgdb_CMiniMdRW() :
m_pvMd(0),
m_cbMd(0)
{
}

HRESULT Target_CLiteWeightStgdb_CMiniMdRW::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.Read(&m_MiniMd));
    IfFailRet(reader.ReadPointer(&m_pvMd));
    IfFailRet(reader.Read32(&m_cbMd));
    return S_OK;
}

Target_CLiteWeightStgdbRW::Target_CLiteWeightStgdbRW() :
m_cbSaveSize(0),
m_bSaveCompressed(FALSE),
m_pImage(0),
m_dwImageSize(0),
m_dwPEKind(0),
m_dwMachine(0),
m_pStreamList(0),
m_pNextStgdb(0),
m_eFileType(0),
m_wszFileName(0),
m_dwDatabaseLFT(0),
m_dwDatabaseLFS(0)
{}

HRESULT Target_CLiteWeightStgdbRW::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(Target_CLiteWeightStgdb_CMiniMdRW::ReadFrom(reader));
    reader.AlignBase();
    IfFailRet(reader.Read32(&m_cbSaveSize));
    IfFailRet(reader.Read8((BYTE*)&m_bSaveCompressed));
    IfFailRet(reader.ReadPointer(&m_pImage));
    IfFailRet(reader.Read32(&m_dwImageSize));
    IfFailRet(reader.Read32(&m_dwPEKind));
    IfFailRet(reader.Read32(&m_dwMachine));
    IfFailRet(reader.ReadPointer(&m_pStreamList));
    IfFailRet(reader.ReadPointer(&m_pNextStgdb));
    IfFailRet(reader.Read32(&m_eFileType));
    IfFailRet(reader.ReadPointer(&m_wszFileName));
    IfFailRet(reader.Read32(&m_dwDatabaseLFT));
    IfFailRet(reader.Read32(&m_dwDatabaseLFS));
    IfFailRet(reader.ReadPointer(&m_pStgIO));
    return S_OK;
}



Target_MDInternalRW::Target_MDInternalRW() :
m_tdModule(0),
m_cRefs(0),
m_fOwnStgdb(FALSE),
m_pUnk(0),
m_pUserUnk(0),
m_pIMetaDataHelper(0),
m_pSemReadWrite(0),
m_fOwnSem(FALSE)
{
}

HRESULT Target_MDInternalRW::ReadFrom(DataTargetReader & reader)
{
    HRESULT hr = S_OK;
    IfFailRet(reader.SkipPointer()); // IMDInternalImportENC vtable
    IfFailRet(reader.SkipPointer()); // IMDCommon vtable
    IfFailRet(reader.ReadPointer(&m_pStgdb));
    IfFailRet(reader.Read32(&m_tdModule));
    IfFailRet(reader.Read32(&m_cRefs));
    IfFailRet(reader.Read8((BYTE*)&m_fOwnStgdb));
    IfFailRet(reader.ReadPointer(&m_pUnk));
    IfFailRet(reader.ReadPointer(&m_pUserUnk));
    IfFailRet(reader.ReadPointer(&m_pIMetaDataHelper));
    IfFailRet(reader.ReadPointer(&m_pSemReadWrite));
    IfFailRet(reader.Read8((BYTE*)&m_fOwnSem));
    return S_OK;
}
