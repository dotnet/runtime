// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// TargetTypes.h
//

//
//*****************************************************************************

#ifndef _MD_TARGET_TYPES_
#define _MD_TARGET_TYPES_

#include "datatargetreader.h"

class Target_CMiniMdSchemaBase : public TargetObject
{
public:
    Target_CMiniMdSchemaBase();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_ulReserved;
    BYTE m_major;
    BYTE m_minor;
    BYTE m_heaps;
    BYTE m_rid;
    ULONG64 m_maskvalid;
    ULONG64 m_sorted;
};

class Target_CMiniMdSchema : public Target_CMiniMdSchemaBase
{
public:
    Target_CMiniMdSchema();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_cRecs[TBL_COUNT];
    ULONG32 m_ulExtra;
};

class Target_CMiniColDef : public TargetObject
{
public:
    Target_CMiniColDef();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    BYTE m_Type;
    BYTE m_oColumn;
    BYTE m_cbColumn;
};

class Target_CMiniTableDef : public TargetObject
{
public:
    Target_CMiniTableDef();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    NewArrayHolder<Target_CMiniColDef> m_pColDefs;
    BYTE m_cCols;
    BYTE m_iKey;
    BYTE m_cbRec;

private:
    // don't copy this type - avoids needing to deep copy m_pColDefs
    Target_CMiniTableDef(const Target_CMiniTableDef & rhs) { _ASSERTE(!"Don't copy"); }
    Target_CMiniTableDef & operator=(const Target_CMiniTableDef &)
    {
        _ASSERTE(!"Don't copy");
        return *this;
    }
};

class Target_CMiniMdBase : public TargetObject
{
public:
    Target_CMiniMdBase();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CMiniMdSchema  m_Schema;
    ULONG32               m_TblCount;
    BOOL                  m_fVerifiedByTrustedSource;
    Target_CMiniTableDef  m_TableDefs[TBL_COUNT];

    ULONG32               m_iStringsMask;
    ULONG32               m_iGuidsMask;
    ULONG32               m_iBlobsMask;
};

class Target_CMiniMdTemplate_CMiniMdRW : public Target_CMiniMdBase
{
};

class Target_MapSHash : public TargetObject
{
public:
    Target_MapSHash();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_table;
    ULONG32 m_tableSize;
    ULONG32 m_tableCount;
    ULONG32 m_tableOccupied;
    ULONG32 m_tableMax;
};

class Target_CChainedHash : public TargetObject
{
public:
    Target_CChainedHash();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_rgData;
    ULONG32 m_iBuckets;
    ULONG32 m_iSize;
    ULONG32 m_iCount;
    ULONG32 m_iMaxChain;
    ULONG32 m_iFree;
};

class Target_CStringPoolHash : public Target_CChainedHash
{
public:
    Target_CStringPoolHash();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_Pool;
};

class Target_StgPoolSeg : public TargetObject
{
public:
    Target_StgPoolSeg();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_pSegData;
    CORDB_ADDRESS m_pNextSeg;
    ULONG32 m_cbSegSize;
    ULONG32 m_cbSegNext;
};

class Target_StgPoolReadOnly : public Target_StgPoolSeg
{
public:
    virtual HRESULT ReadFrom(DataTargetReader & reader);
};

class Target_StgPool : public Target_StgPoolReadOnly
{
public:
    Target_StgPool();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_ulGrowInc;
    CORDB_ADDRESS m_pCurSeg;
    ULONG32 m_cbCurSegOffset;
    BOOL m_bFree;
    BOOL m_bReadOnly;
    ULONG32 m_nVariableAlignmentMask;
    ULONG32 m_cbStartOffsetOfEdit;
    BOOL m_fValidOffsetOfEdit;
};

class Target_StgStringPool : public Target_StgPool
{
public:
    Target_StgStringPool();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CStringPoolHash m_Hash;
    BOOL m_bHash;
};

class Target_StringHeapRW : public Target_StgStringPool
{
};

class Target_CBlobPoolHash : public Target_CChainedHash
{
public:
    Target_CBlobPoolHash();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_Pool;
};

class Target_StgBlobPool : public Target_StgPool
{
public:
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CBlobPoolHash m_Hash;
};

class Target_BlobHeapRW : public Target_StgBlobPool
{
};

class Target_CGuidPoolHash : public Target_CChainedHash
{
public:
    Target_CGuidPoolHash();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_Pool;
};

class Target_StgGuidPool : public Target_StgPool
{
public:
    Target_StgGuidPool();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CGuidPoolHash m_Hash;
    BOOL m_bHash;
};

class Target_GuidHeapRW : public Target_StgGuidPool
{
};

class Target_RecordPool : public Target_StgPool
{
public:
    Target_RecordPool();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_cbRec;
};

class Target_TableRW : public Target_RecordPool
{
};

class Target_OptionValue : public TargetObject
{
public:
    Target_OptionValue();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_DupCheck;
    ULONG32 m_RefToDefCheck;
    ULONG32 m_NotifyRemap;
    ULONG32 m_UpdateMode;
    ULONG32 m_ErrorIfEmitOutOfOrder;
    ULONG32 m_ThreadSafetyOptions;
    ULONG32 m_ImportOption;
    ULONG32 m_LinkerOption;
    ULONG32 m_GenerateTCEAdapters;
    CORDB_ADDRESS m_RuntimeVersion;
    ULONG32 m_MetadataVersion;
    ULONG32 m_MergeOptions;
    ULONG32 m_InitialSize;
    ULONG32 m_LocalRefPreservation;
};

class Target_CMiniMdRW : public Target_CMiniMdTemplate_CMiniMdRW
{
public:
    Target_CMiniMdRW();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    CORDB_ADDRESS m_pMemberRefHash;
    CORDB_ADDRESS m_pMemberDefHash;
    CORDB_ADDRESS m_pLookUpHashs[TBL_COUNT];
    Target_MapSHash m_StringPoolOffsetHash;
    CORDB_ADDRESS m_pNamedItemHash;
    ULONG32 m_maxRid;
    ULONG32 m_limRid;
    ULONG32 m_maxIx;
    ULONG32 m_limIx;
    ULONG32 m_eGrow;
    Target_TableRW m_Tables[TBL_COUNT];
    CORDB_ADDRESS m_pVS[TBL_COUNT];
    Target_StringHeapRW m_StringHeap;
    Target_BlobHeapRW m_BlobHeap;
    Target_BlobHeapRW m_UserStringHeap;
    Target_GuidHeapRW m_GuidHeap;
    CORDB_ADDRESS m_pHandler;
    ULONG32 m_cbSaveSize;
    BOOL m_fIsReadOnly;
    BOOL m_bPreSaveDone;
    BOOL m_bSaveCompressed;
    BOOL m_bPostGSSMod;
    CORDB_ADDRESS m_pMethodMap;
    CORDB_ADDRESS m_pFieldMap;
    CORDB_ADDRESS m_pPropertyMap;
    CORDB_ADDRESS m_pEventMap;
    CORDB_ADDRESS m_pParamMap;
    CORDB_ADDRESS m_pFilterTable;
    CORDB_ADDRESS m_pHostFilter;
    CORDB_ADDRESS m_pTokenRemapManager;
    Target_OptionValue m_OptionValue;
    Target_CMiniMdSchema m_StartupSchema;
    BYTE m_bSortable[TBL_COUNT];
    CORDB_ADDRESS dbg_m_pLock;
    BOOL m_fMinimalDelta;
    CORDB_ADDRESS m_rENCRecs;
};

class Target_CLiteWeightStgdb_CMiniMdRW : public TargetObject
{
public:
    Target_CLiteWeightStgdb_CMiniMdRW();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CMiniMdRW m_MiniMd;
    CORDB_ADDRESS m_pvMd;
    ULONG32 m_cbMd;
};

class Target_CLiteWeightStgdbRW : public Target_CLiteWeightStgdb_CMiniMdRW
{
public:
    Target_CLiteWeightStgdbRW();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    ULONG32 m_cbSaveSize;
    BOOL m_bSaveCompressed;
    CORDB_ADDRESS m_pImage;
    ULONG32 m_dwImageSize;
    ULONG32 m_dwPEKind;
    ULONG32 m_dwMachine;
    CORDB_ADDRESS m_pStreamList;
    CORDB_ADDRESS m_pNextStgdb;
    ULONG32 m_eFileType;
    CORDB_ADDRESS m_wszFileName;
    ULONG32 m_dwDatabaseLFT;
    ULONG32 m_dwDatabaseLFS;
    CORDB_ADDRESS m_pStgIO;
};

class Target_MDInternalRW : public TargetObject
{
public:
    Target_MDInternalRW();
    virtual HRESULT ReadFrom(DataTargetReader & reader);

    Target_CLiteWeightStgdbRW m_pStgdb;
    ULONG32 m_tdModule;
    ULONG32 m_cRefs;
    BOOL m_fOwnStgdb;
    CORDB_ADDRESS m_pUnk;
    CORDB_ADDRESS m_pUserUnk;
    CORDB_ADDRESS m_pIMetaDataHelper;
    CORDB_ADDRESS m_pSemReadWrite;
    BOOL m_fOwnSem;
};

#endif
