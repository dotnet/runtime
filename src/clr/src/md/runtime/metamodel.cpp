// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MetaModel.cpp -- Base portion of compressed COM+ metadata.
// 

//
//*****************************************************************************
#include "stdafx.h"

#include <metamodel.h>
#include <corerror.h>
#include <posterror.h>

//*****************************************************************************
// meta-meta model.
//*****************************************************************************

//-----------------------------------------------------------------------------
// Start of column definitions.
//-----------------------------------------------------------------------------
// Column type, offset, size.
#define SCHEMA_TABLE_START(tbl) static CMiniColDef r##tbl##Cols[] = {
#define SCHEMA_ITEM_NOFIXED()
#define SCHEMA_ITEM_ENTRY(col,typ) {typ, 0,0},
#define SCHEMA_ITEM_ENTRY2(col,typ,ofs,siz) {typ, ofs, siz},
#define SCHEMA_ITEM(tbl,typ,col) SCHEMA_ITEM_ENTRY2(col, i##typ, offsetof(tbl##Rec,m_##col), sizeof(((tbl##Rec*)(0))->m_##col))
#define SCHEMA_ITEM_RID(tbl,col,tbl2) SCHEMA_ITEM_ENTRY(col,TBL_##tbl2)
#define SCHEMA_ITEM_STRING(tbl,col) SCHEMA_ITEM_ENTRY(col,iSTRING)
#define SCHEMA_ITEM_GUID(tbl,col) SCHEMA_ITEM_ENTRY(col,iGUID)
#define SCHEMA_ITEM_BLOB(tbl,col) SCHEMA_ITEM_ENTRY(col,iBLOB)
#define SCHEMA_ITEM_CDTKN(tbl,col,tkns) SCHEMA_ITEM_ENTRY(col,iCodedToken+(CDTKN_##tkns))
#define SCHEMA_TABLE_END(tbl) };
//-----------------------------------------------------------------------------
#include "metamodelcolumndefs.h"
//-----------------------------------------------------------------------------
#undef SCHEMA_TABLE_START
#undef SCHEMA_ITEM_NOFIXED
#undef SCHEMA_ITEM_ENTRY
#undef SCHEMA_ITEM_ENTRY2
#undef SCHEMA_ITEM
#undef SCHEMA_ITEM_RID
#undef SCHEMA_ITEM_STRING
#undef SCHEMA_ITEM_GUID
#undef SCHEMA_ITEM_BLOB
#undef SCHEMA_ITEM_CDTKN
#undef SCHEMA_TABLE_END
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Column names.
#define SCHEMA_TABLE_START(tbl) static const char *r##tbl##ColNames[] = {
#define SCHEMA_ITEM_NOFIXED()
#define SCHEMA_ITEM_ENTRY(col,typ) #col,
#define SCHEMA_ITEM_ENTRY2(col,typ,ofs,siz) #col,
#define SCHEMA_ITEM(tbl,typ,col) SCHEMA_ITEM_ENTRY2(col, i##typ, offsetof(tbl##Rec,m_##col), sizeof(((tbl##Rec*)(0))->m_##col))
#define SCHEMA_ITEM_RID(tbl,col,tbl2) SCHEMA_ITEM_ENTRY(col,TBL_##tbl2)
#define SCHEMA_ITEM_STRING(tbl,col) SCHEMA_ITEM_ENTRY(col,iSTRING)
#define SCHEMA_ITEM_GUID(tbl,col) SCHEMA_ITEM_ENTRY(col,iGUID)
#define SCHEMA_ITEM_BLOB(tbl,col) SCHEMA_ITEM_ENTRY(col,iBLOB)
#define SCHEMA_ITEM_CDTKN(tbl,col,tkns) SCHEMA_ITEM_ENTRY(col,iCodedToken+(CDTKN_##tkns))
#define SCHEMA_TABLE_END(tbl) };
//-----------------------------------------------------------------------------
#include "metamodelcolumndefs.h"
//-----------------------------------------------------------------------------
#undef SCHEMA_TABLE_START
#undef SCHEMA_ITEM_NOFIXED
#undef SCHEMA_ITEM_ENTRY
#undef SCHEMA_ITEM_ENTRY2
#undef SCHEMA_ITEM
#undef SCHEMA_ITEM_RID
#undef SCHEMA_ITEM_STRING
#undef SCHEMA_ITEM_GUID
#undef SCHEMA_ITEM_BLOB
#undef SCHEMA_ITEM_CDTKN
#undef SCHEMA_TABLE_END

//-----------------------------------------------------------------------------
// End of column definitions.
//-----------------------------------------------------------------------------

// Define the array of Coded Token Definitions.
#define MiniMdCodedToken(x) {lengthof(CMiniMdBase::mdt##x), CMiniMdBase::mdt##x, #x},
const CCodedTokenDef g_CodedTokens [] = {
    MiniMdCodedTokens()
};
#undef MiniMdCodedToken

// Define the array of Table Definitions.
#undef MiniMdTable
#define MiniMdTable(x) { { r##x##Cols, lengthof(r##x##Cols), x##Rec::COL_KEY, 0 }, r##x##ColNames, #x},
const CMiniTableDefEx g_Tables[TBL_COUNT] = {
    MiniMdTables()
};

// Define a table descriptor for the obsolete v1.0 GenericParam table definition.
const CMiniTableDefEx g_Table_GenericParamV1_1 = { { rGenericParamV1_1Cols, lengthof(rGenericParamV1_1Cols), GenericParamV1_1Rec::COL_KEY, 0 }, rGenericParamV1_1ColNames, "GenericParamV1_"};



// Define the array of Ptr Tables.  This is initialized to TBL_COUNT here.
// The correct values will be set in the constructor for MiniMdRW.
#undef MiniMdTable
#define MiniMdTable(x) { TBL_COUNT, 0 },
TblCol g_PtrTableIxs[TBL_COUNT] = {
    MiniMdTables()
};

//*****************************************************************************
// Initialize a new schema.
//*****************************************************************************
HRESULT 
CMiniMdSchema::InitNew(
    MetadataVersion mdVersion)
{
    // Make sure the tables fit in the mask.
    _ASSERTE((sizeof(m_maskvalid) * 8) > TBL_COUNT);
    
    m_ulReserved = 0;
    
    if(mdVersion == MDVersion1)
    {
        m_major = METAMODEL_MAJOR_VER_V1_0;              
        m_minor = METAMODEL_MINOR_VER_V1_0;
    }
    else if (mdVersion == MDVersion2)
    {
        m_major = METAMODEL_MAJOR_VER;              
        m_minor = METAMODEL_MINOR_VER;
    }
    else
    {
        return E_INVALIDARG;
    }
    
    m_heaps = 0;                
    m_rid = 0;                  
    m_maskvalid = 0;            
    m_sorted = 0;               
    memset(m_cRecs, 0, sizeof(m_cRecs));        
    m_ulExtra = 0;
    
    return S_OK;
} // CMiniMdSchema::InitNew

//*****************************************************************************
// Compress a schema into a compressed version of the schema.
//*****************************************************************************
ULONG 
CMiniMdSchema::SaveTo(
    void *pvData)
{
    ULONG ulData;   // Bytes stored.
    CMiniMdSchema *pDest = reinterpret_cast<CMiniMdSchema*>(pvData);
    const unsigned __int64 one = 1;
    
    // Make sure the tables fit in the mask.
    _ASSERTE((sizeof(m_maskvalid) * 8) > TBL_COUNT);
    
    // Set the flag for the extra data.
#if defined(EXTRA_DATA)
    if (m_ulExtra != 0)
    {
        m_heaps |= EXTRA_DATA;
    }
    else
#endif // 0
    {
        m_heaps &= ~EXTRA_DATA;
    }

    // Minor version is preset when we instantiate the MiniMd.

    // Make sure we're saving out a version that Beta1 version can read
    _ASSERTE((m_major == METAMODEL_MAJOR_VER && m_minor == METAMODEL_MINOR_VER) ||
            (m_major == METAMODEL_MAJOR_VER_B1 && m_minor == METAMODEL_MINOR_VER_B1) ||
            (m_major == METAMODEL_MAJOR_VER_V1_0 && m_minor == METAMODEL_MINOR_VER_V1_0));
    
    // Transfer the fixed fields.
    *static_cast<CMiniMdSchemaBase*>(pDest) = *static_cast<CMiniMdSchemaBase*>(this);
    static_cast<CMiniMdSchemaBase*>(pDest)->ConvertEndianness();
    ulData = sizeof(CMiniMdSchemaBase);
    
    // Transfer the variable fields.
    m_maskvalid = 0;
    for (int iSrc=0, iDst=0; iSrc<TBL_COUNT; ++iSrc)
    {
        if (m_cRecs[iSrc] != 0)
        {
            pDest->m_cRecs[iDst++] = VAL32(m_cRecs[iSrc]);
            m_maskvalid |= (one << iSrc);
            ulData += sizeof(m_cRecs[iSrc]);
        }
    }
    // Refresh the mask.
    pDest->m_maskvalid = VAL64(m_maskvalid);
    
#if defined(EXTRA_DATA)
    // Store the extra data.
    if (m_ulExtra != 0)
    {
        *reinterpret_cast<ULONG*>(&pDest->m_cRecs[iDst]) = VAL32(m_ulExtra);
        ulData += sizeof(ULONG);
    }
#endif // 0
    return ulData;
} // CMiniMdSchema::SaveTo

//*****************************************************************************
// Load a schema from a compressed version of the schema.
// Returns count of bytes consumed.  -1 if error.
//*****************************************************************************
ULONG 
CMiniMdSchema::LoadFrom(
    const void *pvData,     // Data to load from.
    ULONG       cbData)     // Amount of data available.
{
    ULONG ulData;   // Bytes consumed.
    
    ulData = sizeof(CMiniMdSchemaBase);
    
    // Be sure we can get the base part.
    if (cbData < ulData)
        return (ULONG)(-1);
    
    // Transfer the fixed fields. The (void*) casts prevents the compiler 
    // from making bad assumptions about the alignment.
    memcpy((void *)this, (void *)pvData, sizeof(CMiniMdSchemaBase));
    static_cast<CMiniMdSchemaBase*>(this)->ConvertEndianness();
    
    unsigned __int64 maskvalid = m_maskvalid;
    
    // Transfer the variable fields.
    memset(m_cRecs, 0, sizeof(m_cRecs));
    int iDst;
    for (iDst = 0; iDst < TBL_COUNT; ++iDst, maskvalid >>= 1)
    {
        if ((maskvalid & 1) != 0)
        {
            // Check integer overflow for: ulData + sizeof(UINT32)
            ULONG ulDataTemp;
            if (!ClrSafeInt<ULONG>::addition(ulData, sizeof(UINT32), ulDataTemp))
            {
                return (ULONG)(-1);
            }
            // Verify that the data is there before touching it.
            if (cbData < (ulData + sizeof(UINT32)))
                return (ULONG)(-1);
            
            m_cRecs[iDst] = GET_UNALIGNED_VAL32((const BYTE *)pvData + ulData);
            // It's safe to sum, because we checked integer overflow above
            ulData += sizeof(UINT32);
        }
    }
    // Also accumulate the sizes of any counters that we don't understand.
    for (iDst = TBL_COUNT; (maskvalid != 0) && (iDst < ((int)sizeof(m_maskvalid) * 8)); ++iDst, maskvalid >>= 1)
    {
        if ((maskvalid & 1) != 0)
        {
            // Check integer overflow for: ulData += sizeof(UINT32);
            if (!ClrSafeInt<ULONG>::addition(ulData, sizeof(UINT32), ulData))
            {
                return (ULONG)(-1);
            }
            // Did we go past end of buffer?
            if (cbData < ulData)
            {
                return (ULONG)(-1);
            }
        }
    }
    
    // Retrieve the extra 4 bytes data.
    if ((m_heaps & EXTRA_DATA) != 0)
    {
        // Check integer overflow for: ulData + sizeof(UINT32)
        ULONG ulDataTemp;
        if (!ClrSafeInt<ULONG>::addition(ulData, sizeof(UINT32), ulDataTemp))
        {
            return (ULONG)(-1);
        }
        // Verify that the 4 bytes data is there before touching it.
        if (cbData < (ulData + sizeof(UINT32)))
            return (ULONG)(-1);
        
        m_ulExtra = GET_UNALIGNED_VAL32((const BYTE *)pvData + ulData);
        // Check the size we used for buffer overflow verification above
        ulData += sizeof(UINT32);
    }
    
    // Did we go past end of buffer?
    if (cbData < ulData)
        return (ULONG)(-1);
    
    return ulData;
} // CMiniMdSchema::LoadFrom


const mdToken CMiniMdBase::mdtTypeDefOrRef[3] = {
    mdtTypeDef, 
    mdtTypeRef,
    mdtTypeSpec
};

// This array needs to be ordered the same as the source tables are processed (currently
//  {field, param, property}) for binary search.
const mdToken CMiniMdBase::mdtHasConstant[3] = {
    mdtFieldDef, 
    mdtParamDef, 
    mdtProperty
};

const mdToken CMiniMdBase::mdtHasCustomAttribute[24] = {
    mdtMethodDef, 
    mdtFieldDef, 
    mdtTypeRef, 
    mdtTypeDef, 
    mdtParamDef, 
    mdtInterfaceImpl, 
    mdtMemberRef, 
    mdtModule,
    mdtPermission,
    mdtProperty,
    mdtEvent,
    mdtSignature,
    mdtModuleRef,
    mdtTypeSpec,
    mdtAssembly,
    mdtAssemblyRef,
    mdtFile,
    mdtExportedType,
    mdtManifestResource,
    mdtGenericParam,
    mdtGenericParamConstraint,
    mdtMethodSpec
};

const mdToken CMiniMdBase::mdtHasFieldMarshal[2] = {
    mdtFieldDef,
    mdtParamDef,
};

const mdToken CMiniMdBase::mdtHasDeclSecurity[3] = {
    mdtTypeDef,
    mdtMethodDef,
    mdtAssembly
};

const mdToken CMiniMdBase::mdtMemberRefParent[5] = {
    mdtTypeDef, 
    mdtTypeRef,
    mdtModuleRef,
    mdtMethodDef,
    mdtTypeSpec
};

const mdToken CMiniMdBase::mdtHasSemantic[2] = {
    mdtEvent,
    mdtProperty,
};

const mdToken CMiniMdBase::mdtMethodDefOrRef[2] = {
    mdtMethodDef, 
    mdtMemberRef
};

const mdToken CMiniMdBase::mdtMemberForwarded[2] = {
    mdtFieldDef,
    mdtMethodDef
};

const mdToken CMiniMdBase::mdtImplementation[3] = {
    mdtFile,
    mdtAssemblyRef,
    mdtExportedType
};

const mdToken CMiniMdBase::mdtCustomAttributeType[5] = {
    0,
    0,
    mdtMethodDef,
    mdtMemberRef,
    0
};

const mdToken CMiniMdBase::mdtResolutionScope[4] = {
    mdtModule,
    mdtModuleRef,
    mdtAssemblyRef,
    mdtTypeRef
};

const mdToken CMiniMdBase::mdtTypeOrMethodDef[2] = {
    mdtTypeDef,
    mdtMethodDef
};

const int CMiniMdBase::m_cb[] = {0,1,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5}; 

//*****************************************************************************
// Function to encode a token into fewer bits.  Looks up token type in array of types.
//*****************************************************************************
//<TODO>@consider whether this could be a binary search.</TODO>
ULONG 
CMiniMdBase::encodeToken(
    RID           rid,          // Rid to encode.
    mdToken       typ,          // Token type to encode.
    const mdToken rTokens[],    // Table of valid token.
    ULONG32       cTokens)      // Size of the table.
{
    mdToken tk = TypeFromToken(typ);
    size_t ix;
    for (ix = 0; ix < cTokens; ++ix)
    {
        if (rTokens[ix] == tk)
            break;
    }
    _ASSERTE(ix < cTokens);
    if (ix >= cTokens)
        return mdTokenNil;
    //<TODO>@FUTURE: make compile-time calculation</TODO>
    return (ULONG)((rid << m_cb[cTokens]) | ix);
} // CMiniMd::encodeToken


//*****************************************************************************
// Helpers for populating the hard-coded schema.
//*****************************************************************************
inline BYTE cbRID(ULONG ixMax) { return ixMax > USHRT_MAX ? (BYTE) sizeof(ULONG) : (BYTE) sizeof(USHORT); }

#define _CBTKN(cRecs,tkns) cbRID(cRecs << m_cb[lengthof(tkns)])

//*****************************************************************************
// Constructor.
//*****************************************************************************
CMiniMdBase::CMiniMdBase()
{
#undef MiniMdTable
#define MiniMdTable(tbl)                                    \
        m_TableDefs[TBL_##tbl] = g_Tables[TBL_##tbl].m_Def; \
        m_TableDefs[TBL_##tbl].m_pColDefs = BYTEARRAY_TO_COLDES(s_##tbl##Col);
    MiniMdTables()

    m_TblCount = TBL_COUNT;
    _ASSERTE(TBL_COUNT == TBL_COUNT_V2); // v2 counts.

    m_fVerifiedByTrustedSource = FALSE;

    // Validator depends on the Table Ids and the Token Ids being identical.
    // Catch it if this ever breaks.
    _ASSERTE((TypeFromToken(mdtModule) >> 24)           == TBL_Module);
    _ASSERTE((TypeFromToken(mdtTypeRef) >> 24)          == TBL_TypeRef);
    _ASSERTE((TypeFromToken(mdtTypeDef) >> 24)          == TBL_TypeDef);
    _ASSERTE((TypeFromToken(mdtFieldDef) >> 24)         == TBL_Field);
    _ASSERTE((TypeFromToken(mdtMethodDef) >> 24)        == TBL_Method);
    _ASSERTE((TypeFromToken(mdtParamDef) >> 24)         == TBL_Param);
    _ASSERTE((TypeFromToken(mdtInterfaceImpl) >> 24)    == TBL_InterfaceImpl);
    _ASSERTE((TypeFromToken(mdtMemberRef) >> 24)        == TBL_MemberRef);
    _ASSERTE((TypeFromToken(mdtCustomAttribute) >> 24)  == TBL_CustomAttribute);
    _ASSERTE((TypeFromToken(mdtPermission) >> 24)       == TBL_DeclSecurity);
    _ASSERTE((TypeFromToken(mdtSignature) >> 24)        == TBL_StandAloneSig);
    _ASSERTE((TypeFromToken(mdtEvent) >> 24)            == TBL_Event);
    _ASSERTE((TypeFromToken(mdtProperty) >> 24)         == TBL_Property);
    _ASSERTE((TypeFromToken(mdtModuleRef) >> 24)        == TBL_ModuleRef);
    _ASSERTE((TypeFromToken(mdtTypeSpec) >> 24)         == TBL_TypeSpec);
    _ASSERTE((TypeFromToken(mdtAssembly) >> 24)         == TBL_Assembly);
    _ASSERTE((TypeFromToken(mdtAssemblyRef) >> 24)      == TBL_AssemblyRef);
    _ASSERTE((TypeFromToken(mdtFile) >> 24)             == TBL_File);
    _ASSERTE((TypeFromToken(mdtExportedType) >> 24)     == TBL_ExportedType);
    _ASSERTE((TypeFromToken(mdtManifestResource) >> 24) == TBL_ManifestResource);
    _ASSERTE((TypeFromToken(mdtGenericParam) >> 24)     == TBL_GenericParam);
    _ASSERTE((TypeFromToken(mdtMethodSpec) >> 24)       == TBL_MethodSpec);
    _ASSERTE((TypeFromToken(mdtGenericParamConstraint) >> 24) == TBL_GenericParamConstraint);
} // CMiniMdBase::CMiniMdBase


//*****************************************************************************
// Destructor.
//*****************************************************************************
CMiniMdBase::~CMiniMdBase()
{
    for (ULONG i = 0; i < m_TblCount; i++)
    {
        if ((m_TableDefs[i].m_pColDefs != NULL) && UsesAllocatedMemory(m_TableDefs[i].m_pColDefs))
        {
            delete[] COLDES_TO_BYTEARRAY(m_TableDefs[i].m_pColDefs);
            m_TableDefs[i].m_pColDefs = NULL;
        }
    }
} // CMiniMdBase::~CMiniMdBase
    
//*****************************************************************************
// Build the schema based on the header data provided.
// Handle all supported versions, and adjust data structures appropriately.
//*****************************************************************************
HRESULT 
CMiniMdBase::SchemaPopulate(
    const void *pvData,     // Pointer to the buffer.
    ULONG       cbData,     // Size of the buffer.
    ULONG      *pcbUsed)    // Put size of the header here.
{
    HRESULT hr;
    ULONG   cb;         // Bytes read for header.
    ULONG   cbTables;   // Bytes needed for tables.
    ULONG   cbTotal;    // Bytes read for header + needed for tables.
    
    // Uncompress the schema from the buffer into our structures.
    cb = m_Schema.LoadFrom(pvData, cbData);
    
    if ((cb > cbData) || (cb == (ULONG)(-1)))
    {
        Debug_ReportError("Schema is not in MetaData block.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }
    
    // Is this the "native" version of the metadata for this runtime?
    if ((m_Schema.m_major != METAMODEL_MAJOR_VER) || (m_Schema.m_minor != METAMODEL_MINOR_VER))
    {
        // No it's not. Is this an older version that we support?
        
        // Is this v1.0?
        if ((m_Schema.m_major == METAMODEL_MAJOR_VER_V1_0) && 
            (m_Schema.m_minor == METAMODEL_MINOR_VER_V1_0))
        {   
            // Older version has fewer tables.
            m_TblCount = TBL_COUNT_V1;
        }
        else if ((m_Schema.m_major == METAMODEL_MAJOR_VER_B1) && 
                 (m_Schema.m_minor == METAMODEL_MINOR_VER_B1))
        {
            // 1.1 had a different type of GenericParam table
            m_TableDefs[TBL_GenericParam] = g_Table_GenericParamV1_1.m_Def;
            m_TableDefs[TBL_GenericParam].m_pColDefs = BYTEARRAY_TO_COLDES(s_GenericParamCol);
        }
        else
        {   // We don't support this version of the metadata
            Debug_ReportError("Unsupported version of MetaData.");
            return PostError(CLDB_E_FILE_OLDVER, m_Schema.m_major, m_Schema.m_minor);
        }
    }
    
    // Populate the schema, based on the row counts and heap sizes.
    IfFailRet(SchemaPopulate2(&cbTables));
    
    // Check that header plus tables fits within the size given.
    if (!ClrSafeInt<ULONG>::addition(cb, cbTables, cbTotal) || (cbTotal > cbData))
    {
        Debug_ReportError("Tables are not within MetaData block.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }
    
    *pcbUsed = cb;
    return S_OK;
} // CMiniMdBase::SchemaPopulate

//*****************************************************************************
// Initialize from another MD
//*****************************************************************************
HRESULT 
CMiniMdBase::SchemaPopulate(
    const CMiniMdBase &that)
{
    HRESULT hr;
    // Copy over the schema.
    m_Schema = that.m_Schema;
    
    // Adjust for prior versions.
    if (m_Schema.m_major != METAMODEL_MAJOR_VER || m_Schema.m_minor != METAMODEL_MINOR_VER)
    {
        if ((m_Schema.m_major == METAMODEL_MAJOR_VER_V1_0) && (m_Schema.m_minor == METAMODEL_MINOR_VER_V1_0))
        {   // Older version has fewer tables.
            m_TblCount = that.m_TblCount;
            _ASSERTE(m_TblCount == TBL_COUNT_V1);
        }
        else if (m_Schema.m_major == METAMODEL_MAJOR_VER_B1 && m_Schema.m_minor == METAMODEL_MINOR_VER_B1)
        {
            // 1.1 had a different type of GenericParam table
            m_TableDefs[TBL_GenericParam] = g_Table_GenericParamV1_1.m_Def;
            m_TableDefs[TBL_GenericParam].m_pColDefs = BYTEARRAY_TO_COLDES(s_GenericParamCol);
        }
        // Is it a supported old version?  This should never fail!
        else 
        {
            Debug_ReportError("Initializing on an unknown schema version");
            return PostError(CLDB_E_FILE_OLDVER, m_Schema.m_major,m_Schema.m_minor);
        }
    }
    
    IfFailRet(SchemaPopulate2(NULL));
    
    return S_OK;
} // CMiniMdBase::SchemaPopulate

//*****************************************************************************
// Iterate the tables, and fix the column sizes, based on size of data.
//*****************************************************************************
HRESULT CMiniMdBase::SchemaPopulate2(
    ULONG *pcbTables,       // [out, optional] Put size needed for tables here.
    int    bExtra)          // Reserve an extra bit for rid columns?
{
    HRESULT hr;                 // A result.
    ULONG   cbTotal = 0;        // Total size of all tables.
    
    // How big are the various pool inidices?
    m_iStringsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_STRING_4) ? 0xffffffff : 0xffff;
    m_iGuidsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_GUID_4) ? 0xffffffff : 0xffff;
    m_iBlobsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_BLOB_4) ? 0xffffffff : 0xffff;
    
    // Make extra bits exactly zero or one bit.
    if (bExtra)
        bExtra = 1;
    
    // Until ENC, make extra bits exactly zero.
    bExtra = 0;
    
    // For each table...
    for (int ixTbl = 0; ixTbl < (int)m_TblCount; ++ixTbl)
    {
        IfFailRet(InitColsForTable(m_Schema, ixTbl, &m_TableDefs[ixTbl], bExtra, TRUE));
        
        // Accumulate size of this table.
        // Check integer overflow for table size: USHORT * ULONG: m_TableDefs[ixTbl].m_cbRec * GetCountRecs(ixTbl)
        ULONG cbTable;
        if (!ClrSafeInt<ULONG>::multiply(m_TableDefs[ixTbl].m_cbRec, GetCountRecs(ixTbl), cbTable))
        {
            Debug_ReportError("Table is too large - size overflow.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
        // Check integer overflow for all tables so far: cbTotal += cbTable
        if (!ClrSafeInt<ULONG>::addition(cbTotal, cbTable, cbTotal))
        {
            Debug_ReportError("Tables are too large - size overflow.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
    }
    // Check that unused table (e.g. generic tables in v1 format) are empty
    for (ULONG ixTbl = m_TblCount; ixTbl < TBL_COUNT; ixTbl++)
    {
        // All unused tables have to be empty - malicious assemblies can have v1 format version, but can 
        // contain non-empty v2-only tables, this will catch it and refuse to load such assemblies
        if (GetCountRecs(ixTbl) != 0)
        {
            Debug_ReportError("Invalid table present - 2.0 table in v1.x image.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
    }
    
    // Let caller know sizes required.
    if (pcbTables != NULL)
        *pcbTables = cbTotal;
    
    return S_OK;
} // CMiniMdBase::SchemaPopulate2

//*****************************************************************************
// Get the template table definition for a given table.
//*****************************************************************************
const CMiniTableDef * 
CMiniMdBase::GetTableDefTemplate(
    int ixTbl)
{
    const CMiniTableDef *pTemplate;           // the return value.

    // Return the table definition for the given table.  Account for version of schema.
    if ((m_Schema.m_major == METAMODEL_MAJOR_VER_B1) && (m_Schema.m_minor == METAMODEL_MINOR_VER_B1) && (ixTbl == TBL_GenericParam))
    {
        pTemplate = &g_Table_GenericParamV1_1.m_Def;
    }
    else
    {
        pTemplate = &g_Tables[ixTbl].m_Def;
    }

    return pTemplate;
} // CMiniMdBase::GetTableDefTemplate

//*****************************************************************************
// Initialize the column defs for a table, based on their types and sizes.
//*****************************************************************************
HRESULT 
CMiniMdBase::InitColsForTable(
    CMiniMdSchema &Schema,          // Schema with sizes.
    int            ixTbl,           // Index of table to init.                                 
    CMiniTableDef *pTable,          // Table to init.
    int            bExtra,          // Extra bits for rid column.
    BOOL           fUsePointers)    // Should we have pTable point to it's Column Descriptors, or
                                    // should we write the data into the structure
{
    const CMiniTableDef *pTemplate;     // Template table definition.
    CMiniColDef pCols[9];               // The col defs to init.
    BYTE        iOffset;                // Running size of a record.
    BYTE        iSize;                  // Size of a field.
    HRESULT     hr = S_OK;
    
    _ASSERTE((bExtra == 0) || (bExtra == 1));
    _ASSERTE(NumItems(pCols) >= pTable->m_cCols);
    
    bExtra = 0;//<TODO>@FUTURE: save in schema header.  until then use 0.</TODO>
    
    iOffset = 0;
    
    pTemplate = GetTableDefTemplate(ixTbl);
    
    PREFIX_ASSUME(pTemplate->m_pColDefs != NULL);
    
    // For each column in the table...
    for (ULONG ixCol = 0; ixCol < pTable->m_cCols; ++ixCol)
    {
        // Initialize from the template values (type, maybe offset, size).
        pCols[ixCol] = pTemplate->m_pColDefs[ixCol];

        // Is the field a RID into a table?
        if (pCols[ixCol].m_Type <= iRidMax)
        {
            iSize = cbRID(Schema.m_cRecs[pCols[ixCol].m_Type] << bExtra);
        }
        else
        // Is the field a coded token?
        if (pCols[ixCol].m_Type <= iCodedTokenMax)
        {
            ULONG iCdTkn = pCols[ixCol].m_Type - iCodedToken;
            ULONG cRecs = 0;

            _ASSERTE(iCdTkn < lengthof(g_CodedTokens));
            CCodedTokenDef const *pCTD = &g_CodedTokens[iCdTkn];

            // Iterate the token list of this coded token.
            for (ULONG ixToken=0; ixToken<pCTD->m_cTokens; ++ixToken)
            {   // Ignore string tokens.
                if (pCTD->m_pTokens[ixToken] != mdtString)
                {
                    // Get the table for the token.
                    ULONG nTokenTable = CMiniMdRW::GetTableForToken(pCTD->m_pTokens[ixToken]);
                    _ASSERTE(nTokenTable < TBL_COUNT);
                    // If largest token seen so far, remember it.
                    if (Schema.m_cRecs[nTokenTable] > cRecs)
                        cRecs = Schema.m_cRecs[nTokenTable];
                }
            }

            iSize = cbRID(cRecs << (bExtra + m_cb[pCTD->m_cTokens]));

        }
        else
        {   // Fixed type.
            switch (pCols[ixCol].m_Type)
            {
            case iBYTE:
                iSize = 1;
                _ASSERTE(pCols[ixCol].m_cbColumn == iSize);
                _ASSERTE(pCols[ixCol].m_oColumn == iOffset);
                break;
            case iSHORT:
            case iUSHORT:
                iSize = 2;
                _ASSERTE(pCols[ixCol].m_cbColumn == iSize);
                _ASSERTE(pCols[ixCol].m_oColumn == iOffset);
                break;
            case iLONG:
            case iULONG:
                iSize = 4;
                _ASSERTE(pCols[ixCol].m_cbColumn == iSize);
                _ASSERTE(pCols[ixCol].m_oColumn == iOffset);
                break;
            case iSTRING:
                iSize = (Schema.m_heaps & CMiniMdSchema::HEAP_STRING_4) ? 4 : 2;
                break;
            case iGUID:
                iSize = (Schema.m_heaps & CMiniMdSchema::HEAP_GUID_4) ? 4 : 2;
                break;
            case iBLOB:
                iSize = (Schema.m_heaps & CMiniMdSchema::HEAP_BLOB_4) ? 4 : 2;
                break;
            default:
                _ASSERTE(!"Unexpected schema type");
                iSize = 0;
                break;
            }
        }

        // Now save the size and offset.
        pCols[ixCol].m_oColumn = iOffset;
        pCols[ixCol].m_cbColumn = iSize;

        // Align to 2 bytes.
        iSize += iSize & 1;
         
        iOffset += iSize;
    }
    // Record size of entire record.
    pTable->m_cbRec = iOffset;

    _ASSERTE(pTable->m_pColDefs != NULL);

    // Can we write to the memory
    if (!fUsePointers)
    {
        memcpy(pTable->m_pColDefs, pCols, sizeof(CMiniColDef)*pTable->m_cCols);
    }
    else
    {
        // We'll need to have pTable->m_pColDefs point to some data instead
        hr = SetNewColumnDefinition(pTable, pCols, ixTbl);
    }        
    // If no key, set to a distinct value.
    if (pTable->m_iKey >= pTable->m_cCols)
        pTable->m_iKey = (BYTE) -1;

    return hr;
} // CMiniMdBase::InitColsForTable

//*****************************************************************************
// Place a new Column Definition into the metadata.
//*****************************************************************************
HRESULT 
CMiniMdBase::SetNewColumnDefinition(
    CMiniTableDef *pTable, 
    CMiniColDef   *pCols, 
    DWORD          ixTbl)
{
    // Look up the global cache to see if we can use a cached copy
    if (UsesAllocatedMemory(pCols) || 
        !FindSharedColDefs(pTable, pCols, ixTbl))
    {
        // See if we've already allocated memory for this item
        
        if (!UsesAllocatedMemory(pTable->m_pColDefs))
        {
            // We don't have this column definition cached. Allocate new memory for it.
            // Notice, we allocate one more byte than necessary, so we can 'mark' this chunk of memory
            // as allocated so we can free it later.
            
            BYTE *newMemory = new (nothrow) BYTE[(sizeof(CMiniColDef)*pTable->m_cCols)+1];

            if (newMemory == NULL)
                return E_OUTOFMEMORY;
            
            // Mark the first byte in this as with the "allocated memory marker"
            *newMemory = ALLOCATED_MEMORY_MARKER;

            // Have the pointer point to the first Column Descriptor
            pTable->m_pColDefs = BYTEARRAY_TO_COLDES(newMemory);
        }
        
        memcpy(pTable->m_pColDefs, pCols, sizeof(CMiniColDef)*pTable->m_cCols);
    }
    
    return S_OK;
} // CMiniMdBase::SetNewColumnDefinition


//*****************************************************************************
// Get the count of records in a table.  Virtual.
//*****************************************************************************
ULONG 
CMiniMdBase::GetCountRecs(
    ULONG ixTbl)
{
    _ASSERTE(ixTbl < TBL_COUNT);
    return m_Schema.m_cRecs[ixTbl];
} // CMiniMdBase::GetCountRecs


#if BIGENDIAN
// Endian Swaps the passed in blob representing a constant into the passed in StgPool
HRESULT 
CMiniMdBase::SwapConstant(
    const void *pBlobValue,     // Original Value pointer
    DWORD       dwType,         // Type of the constant
    void       *pConstant,      // [Out] Location to store constant into
    ULONG       ValueLength)    // [In] Length of constant
{ 
    HRESULT hr = NOERROR;
    
    switch (dwType)
    {
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_VOID:
        // Just return the value
        *(BYTE *)pConstant = *(BYTE *)pBlobValue;
        return NOERROR;
        
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        _ASSERTE(ValueLength == 2);
        *(SHORT *)pConstant = GET_UNALIGNED_VAL16(pBlobValue);
        break;  
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
        _ASSERTE(ValueLength == 4);
        *(__int32 *)pConstant = GET_UNALIGNED_VAL32(pBlobValue);
        break;
    case ELEMENT_TYPE_R4:
        {
            __int32 Value = GET_UNALIGNED_VAL32(pBlobValue);
            *(float *)pConstant = (float &)Value;
        }
        break;
        
    case ELEMENT_TYPE_R8:
        {
            __int64 Value = GET_UNALIGNED_VAL64(pBlobValue);
            *(double *)pConstant = (double &) Value;
        }
        break;
        
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
        _ASSERTE(ValueLength == 8);
        *(__int64 *)pConstant = GET_UNALIGNED_VAL64(pBlobValue);
        break;
    case ELEMENT_TYPE_STRING:
        memcpy(pConstant, pBlobValue, ValueLength);
        SwapStringLength((WCHAR *)pConstant, (ValueLength)/sizeof(WCHAR));
        break;  
    default:
        _ASSERTE(!"BAD TYPE!");
        return E_INVALIDARG;
        break;
    }
    return hr;
} // CMiniMdBase::SwapConstant
#endif //BIGENDIAN

//*****************************************************************************
// It is non-trivial to sort propertymap. VB is generating properties in 
// non-sorted order!!!
//*****************************************************************************
HRESULT 
CMiniMdBase::FindPropertyMapFor(
    RID  ridParent, 
    RID *pFoundRid)
{
    HRESULT hr;
    ULONG   i;
    ULONG   iCount;
    void   *pRec;
    RID     rid;

    // If the table is sorted, use binary search.  However we can only trust
    // the sorted bit if we have verified it (see definition in MetaModel.h)
    if (IsVerified() && m_Schema.IsSorted(TBL_PropertyMap))
    {
        return vSearchTable(TBL_PropertyMap, 
                            _COLDEF(PropertyMap,Parent),
                            ridParent,
                            pFoundRid);
    }
    else
    {
        iCount = GetCountRecs(TBL_PropertyMap);

        // loop through all LocalVar
        for (i = 1; i <= iCount; i++)
        {
            IfFailRet(vGetRow(TBL_PropertyMap, i, &pRec));

            // linear search for propertymap record
            rid = getIX(pRec, _COLDEF(PropertyMap,Parent));
            if (rid == ridParent)
            {
                *pFoundRid = i;
                return S_OK;
            }
        }

        *pFoundRid = 0;
        return S_OK;
    }

} // CMiniMdBase::FindPropertyMapFor


//*****************************************************************************
// It is non-trivial to sort eventmap. VB is generating events in 
// non-sorted order!!!
//*****************************************************************************
__checkReturn 
HRESULT 
CMiniMdBase::FindEventMapFor(
    RID  ridParent, 
    RID *pFoundRid)
{
    HRESULT hr;
    ULONG   i;
    ULONG   iCount;
    void   *pRec;
    RID     rid;

    // If the table is sorted, use binary search.  However we can only trust
    // the sorted bit if we have verified it (see definition in MetaModel.h)
    if (IsVerified() && m_Schema.IsSorted(TBL_EventMap))
    {
        return vSearchTable(TBL_EventMap,
                            _COLDEF(EventMap,Parent),
                            ridParent,
                            pFoundRid);
    }
    else
    {    
        iCount = GetCountRecs(TBL_EventMap);
    
        // loop through all LocalVar
        for (i = 1; i <= iCount; i++)
        {
            IfFailRet(vGetRow(TBL_EventMap, i, &pRec));
    
            // linear search for propertymap record
            rid = getIX(pRec, _COLDEF(EventMap,Parent));
            if (rid == ridParent)
            {
                *pFoundRid = i;
                return S_OK;
            }
        }
    
        *pFoundRid = 0;
        return S_OK;
    }
} // CMiniMdBase::FindEventMapFor


//*****************************************************************************
// Search for a custom value with a given type.
//*****************************************************************************
__checkReturn 
HRESULT 
CMiniMdBase::FindCustomAttributeFor(
    RID     rid,        // The object's rid.
    mdToken tkObj,      // The object's type.
    mdToken tkType,     // Type of custom value.
    RID    *pFoundRid)  // RID of custom value, or 0.
{
    HRESULT hr;
    int     ixFound;                // index of some custom value row.
    ULONG   ulTarget = encodeToken(rid,tkObj,mdtHasCustomAttribute,lengthof(mdtHasCustomAttribute)); // encoded token representing target.
    ULONG   ixCur;                  // Current row being examined.
    mdToken tkFound;                // Type of some custom value row.
    void   *pCur;                   // A custom value entry.

    // Search for any entry in CustomAttribute table.  Convert to RID.
    IfFailRet(vSearchTable(TBL_CustomAttribute, _COLDEF(CustomAttribute,Parent), ulTarget, (RID *)&ixFound));
    if (ixFound == 0)
    {
        *pFoundRid = 0;
        return S_OK;
    }
    
    // Found an entry that matches the item.  Could be anywhere in a range of 
    //  custom values for the item, somewhat at random.  Search for a match
    //  on name.  On entry to the first loop, we know the object is the desired
    //  one, so the object test is at the bottom.
    ixCur = ixFound;
    IfFailRet(vGetRow(TBL_CustomAttribute, ixCur, &pCur));
    for(;;)
    {
        // Test the type of the current row.
        tkFound = getIX(pCur, _COLDEF(CustomAttribute,Type));
        tkFound = decodeToken(tkFound, mdtCustomAttributeType, lengthof(mdtCustomAttributeType));
        if (tkFound == tkType)
        {
            *pFoundRid = ixCur;
            return S_OK;
        }
        // Was this the last row of the CustomAttribute table?
        if (ixCur == GetCountRecs(TBL_CustomAttribute))
            break;
        // No match, more rows, try for the next row.
        ++ixCur;
        // Get the row and see if it is for the same object.
        IfFailRet(vGetRow(TBL_CustomAttribute, ixCur, &pCur));
        if (getIX(pCur, _COLDEF(CustomAttribute,Parent)) != ulTarget)
            break;
    }
    // Didn't find the name looking up.  Try looking down.
    ixCur = ixFound - 1;
    for(;;)
    {
        // Run out of table yet?
        if (ixCur == 0)
            break;
        // Get the row and see if it is for the same object.
        IfFailRet(vGetRow(TBL_CustomAttribute, ixCur, &pCur));
        // still looking at the same object?
        if (getIX(pCur, _COLDEF(CustomAttribute,Parent)) != ulTarget)
            break;
        // Test the type of the current row.
        tkFound = getIX(pCur, _COLDEF(CustomAttribute,Type));
        tkFound = decodeToken(tkFound, mdtCustomAttributeType, lengthof(mdtCustomAttributeType));
        if (tkFound == tkType)
        {
            *pFoundRid = ixCur;
            return S_OK;
        }
        // No match, try for the previous row.
        --ixCur;
    }
    // Didn't find anything.
    *pFoundRid = 0;
    return S_OK;
} // CMiniMdBase::FindCustomAttributeFor

//*****************************************************************************
// See if we can find a globally shared Column Def Array for this table
//*****************************************************************************
BOOL 
CMiniMdBase::FindSharedColDefs(
    CMiniTableDef *pTable, 
    CMiniColDef   *pColsToMatch, 
    DWORD          ixTbl)
{
    // The majority of the time, m_pColDefs will point to the correct Column Definition Array.
    if (!memcmp(pTable->m_pColDefs, pColsToMatch, sizeof(CMiniColDef)*(pTable->m_cCols)))
        return TRUE;
   
    else
    {
        // m_pColDefs points to a set of Column Def Arrays, with the byte previous to it the number 
        // of column descriptors that we have.
        CMiniColDef *pListOfColumnDefs = BYTEARRAY_TO_COLDES(s_TableColumnDescriptors[ixTbl]);

        BYTE nNumColDes = *(s_TableColumnDescriptors[ixTbl]);

        // Start at '1' since we already compared the first set of column definitions above.
        for (int i = 1; i < nNumColDes; i++)
        {
            pListOfColumnDefs += pTable->m_cCols;

            if (!memcmp(pListOfColumnDefs, pColsToMatch, sizeof(CMiniColDef)*(pTable->m_cCols)))
            {
                pTable->m_pColDefs = pListOfColumnDefs;
                return TRUE;
            }
        }
    }

    // We weren't able to find a shared column definition        
    return FALSE;
}// CMiniMdBase::FindSharedColDefs

//*****************************************************************************
// Determines where the Table Def's Column Definitions used shared memory or
// allocated memory
//*****************************************************************************
BOOL 
CMiniMdBase::UsesAllocatedMemory(
    CMiniColDef *pCols)
{
    BYTE *pMem = COLDES_TO_BYTEARRAY(pCols);
    
    // If the byte preceding this pointer is -1, then we allocated it and it must be freed
    return (*pMem == ALLOCATED_MEMORY_MARKER);
}// CMiniMdBase::UsesAllocatedMemory
