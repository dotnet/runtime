// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModel.h -- header file for compressed COM+ metadata.
//

//
//*****************************************************************************
#ifndef _METAMODEL_H_
#define _METAMODEL_H_

#if _MSC_VER >= 1100
#pragma once
#endif

#include <cor.h>
#include <stgpool.h>
#include <metamodelpub.h>
#include "metadatatracker.h"

#include "../datablob.h"
#include "../debug_metadata.h"

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
#include "portablepdbmdds.h"
#include "portablepdbmdi.h"
#endif

#define ALLOCATED_MEMORY_MARKER 0xff

// Version numbers for metadata format.

#define METAMODEL_MAJOR_VER_V1_0 1      // Major version for v1.0
#define METAMODEL_MINOR_VER_V1_0 0      // Minor version for v1.0

#define METAMODEL_MAJOR_VER_V2_0 2      // Major version for v2.0
#define METAMODEL_MINOR_VER_V2_0 0      // Minor version for v2.0

#define METAMODEL_MAJOR_VER 2
#define METAMODEL_MINOR_VER 0

// Metadata version number up through Whidbey Beta2
#define METAMODEL_MAJOR_VER_B1 1
#define METAMODEL_MINOR_VER_B1 1


typedef enum MetadataVersion
{
    MDVersion1          = 0x00000001,
    MDVersion2          = 0x00000002,

    // @TODO - this value should be updated when we increase the version number
    MDDefaultVersion      = 0x00000002
} MetadataVersion;



struct HENUMInternal;
extern const CCodedTokenDef     g_CodedTokens[CDTKN_COUNT];
extern const CMiniTableDefEx    g_Tables[TBL_COUNT];    // The table definitions.

struct TblCol
{
    ULONG       m_ixtbl;                // Table ID.
    ULONG       m_ixcol;                // Column ID.
};
extern TblCol g_PtrTableIxs[TBL_COUNT];

// This abstract defines the common functions that can be used for RW and RO internally
// (The primary user for this is Compiler\ImportHelper.cpp)
class IMetaModelCommon
{
public:
    __checkReturn
    virtual HRESULT CommonGetScopeProps(
        LPCUTF8     *pszName,
        GUID        *pMvid) = 0;

    __checkReturn
    virtual HRESULT CommonGetTypeRefProps(
        mdTypeRef tr,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkResolution) = 0;

    __checkReturn
    virtual HRESULT CommonGetTypeDefProps(
        mdTypeDef td,
        LPCUTF8     *pszNameSpace,
        LPCUTF8     *pszName,
        DWORD       *pdwFlags,
        mdToken     *pdwExtends,
        ULONG       *pMethodList) = 0;

    __checkReturn
    virtual HRESULT CommonGetTypeSpecProps(
        mdTypeSpec ts,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) = 0;

    __checkReturn
    virtual HRESULT CommonGetEnclosingClassOfTypeDef(
        mdTypeDef  td,
        mdTypeDef *ptkEnclosingTypeDef) = 0;

    __checkReturn
    virtual HRESULT CommonGetAssemblyProps(
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKey,
        ULONG       *pcbPublicKey,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale) = 0;

    __checkReturn
    virtual HRESULT CommonGetAssemblyRefProps(
        mdAssemblyRef tkAssemRef,
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKeyOrToken,
        ULONG       *pcbPublicKeyOrToken,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale,
        const void  **ppbHashValue,
        ULONG       *pcbHashValue) = 0;

    __checkReturn
    virtual HRESULT CommonGetModuleRefProps(
        mdModuleRef tkModuleRef,
        LPCUTF8     *pszName) = 0;

    __checkReturn
    virtual HRESULT CommonFindExportedType(
        LPCUTF8     szNamespace,
        LPCUTF8     szName,
        mdToken     tkEnclosingType,
        mdExportedType   *ptkExportedType) = 0;

    __checkReturn
    virtual HRESULT CommonGetExportedTypeProps(
        mdToken     tkExportedType,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkImpl) = 0;

    virtual int CommonIsRo() = 0;

    __checkReturn
    HRESULT CommonGetCustomAttributeByName( // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData)               // [OUT] Put size of data here.
    {
        return CommonGetCustomAttributeByNameEx(tkObj, szName, NULL, ppData, pcbData);
    }

    __checkReturn
    virtual HRESULT CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,            // [IN] Object with Custom Attribute.
        LPCUTF8            szName,           // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,            // [OUT] put custom attribute token here
        const void       **ppData,           // [OUT] Put pointer to data here.
        ULONG             *pcbData) = 0;     // [OUT] Put size of data here.

    __checkReturn
    virtual HRESULT FindParentOfMethodHelper(mdMethodDef md, mdTypeDef *ptd) = 0;

};  // class IMetaModelCommon



// An extension of IMetaModelCommon, exposed by read-only importers only.
//
// These methods were separated from IMetaModelCommon as
// Enc-aware versions of these methods haven't been needed
// and we don't want the maintainence and code-coverage cost
// of providing Enc-aware versions of these methods.
class IMetaModelCommonRO : public IMetaModelCommon
{
public:
    virtual HRESULT CommonGetMethodDefProps(
        mdMethodDef      tkMethodDef,
        LPCUTF8         *pszName,
        DWORD           *pdwFlags,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG           *pcbSigBlob
        ) = 0;

    virtual HRESULT CommonGetMemberRefProps(
        mdMemberRef      tkMemberRef,
        mdToken         *pParentToken
        ) = 0;

    virtual ULONG CommonGetRowCount(        // return hresult
        DWORD       tkKind) = 0;            // [IN] pass in the kind of token.

    virtual HRESULT CommonGetMethodImpls(
        mdTypeDef   tkTypeDef,              // [IN] typeDef to scope search
        mdToken    *ptkMethodImplFirst,     // [OUT] returns first methodImpl token
        ULONG      *pMethodImplCount        // [OUT] returns # of methodImpl tokens scoped to type
        ) = 0;

    virtual HRESULT CommonGetMethodImplProps(
        mdToken     tkMethodImpl,           // [IN] methodImpl
        mdToken    *pBody,                  // [OUT] returns body token
        mdToken    *pDecl                   // [OUT] returns decl token
        ) = 0;

    virtual HRESULT CommonGetCustomAttributeProps(
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken          *ptkObj,           // [OUT, OPTIONAL] Put object token here.
        mdToken          *ptkType,          // [OUT, OPTIONAL] Put AttrType token here.
        const void      **ppBlob,           // [OUT, OPTIONAL] Put pointer to data here.
        ULONG            *pcbSize) = 0;     // [OUT, OPTIONAL] Put size of date here.

    virtual HRESULT CommonGetFieldDefProps(
        mdFieldDef      tkFieldDef,
        mdTypeDef       *ptkParent,
        LPCUTF8         *pszName,
        DWORD           *pdwFlags
        ) = 0;
}; // class IMetaModelCommonRO


//*****************************************************************************
// The mini, hard-coded schema.  For each table, we persist the count of
//  records.  We also persist the size of string, blob, guid, and rid
//  columns.  From this information, we can calculate the record sizes, and
//  then the sizes of the tables.
//*****************************************************************************

class CMiniMdSchemaBase
{
public:
    ULONG       m_ulReserved;           // Reserved, must be zero.
    BYTE        m_major;                // Version numbers.
    BYTE        m_minor;
    BYTE        m_heaps;                // Bits for heap sizes.
    BYTE        m_rid;                  // log-base-2 of largest rid.

    // Bits for heap sizes.
    enum {
        HEAP_STRING_4   =   0x01,
        HEAP_GUID_4     =   0x02,
        HEAP_BLOB_4     =   0x04,

        PADDING_BIT     =   0x08,       // Tables can be created with an extra bit in columns, for growth.

        DELTA_ONLY      =   0x20,       // If set, only deltas were persisted.
        EXTRA_DATA      =   0x40,       // If set, schema persists an extra 4 bytes of data.
        HAS_DELETE      =   0x80,       // If set, this metadata can contain _Delete tokens.
    };

    unsigned __int64    m_maskvalid;            // Bit mask of present table counts.

    unsigned __int64    m_sorted;               // Bit mask of sorted tables.
    FORCEINLINE bool IsSorted(ULONG ixTbl)
        { return m_sorted & BIT(ixTbl) ? true : false; }
    void SetSorted(ULONG ixTbl, int bVal)
        { if (bVal) m_sorted |= BIT(ixTbl);
          else      m_sorted &= ~BIT(ixTbl); }

#if BIGENDIAN
    // Verify that the size hasn't changed (Update if necessary)
    void ConvertEndianness()
    {
        _ASSERTE(sizeof(CMiniMdSchemaBase) == 0x18);
        m_ulReserved = VAL32(m_ulReserved);
        m_maskvalid = VAL64(m_maskvalid);
        m_sorted = VAL64(m_sorted);
    }
#else
    // Nothing to do on little endian machine
    void ConvertEndianness() {return ;}
#endif

private:
    FORCEINLINE unsigned __int64 BIT(ULONG ixBit)
    {   _ASSERTE(ixBit < (sizeof(__int64)*CHAR_BIT));
        return UI64(1) << ixBit; }

};

class CMiniMdSchema : public CMiniMdSchemaBase
{
public:
    // These are not all persisted to disk.  See LoadFrom() for details.
    ULONG       m_cRecs[TBL_COUNT];     // Counts of various tables.

    ULONG       m_ulExtra;              // Extra data, only persisted if non-zero.  (m_heaps&EXTRA_DATA flags)

    ULONG LoadFrom(const void*, ULONG); // Load from a compressed version.  Return bytes consumed.
    ULONG SaveTo(void *);               // Store a compressed version.  Return bytes used in buffer.
    __checkReturn
    HRESULT InitNew(MetadataVersion);
};

//*****************************************************************************
// Helper macros and inline functions for navigating through the data.  Many
//  of the macros are used to define inline accessor functions.  Everything
//  is based on the naming conventions outlined at the top of the file.
//*****************************************************************************
#define _GETTER(tbl,fld) get##fld##Of##tbl(tbl##Rec *pRec)
#define _GETTER2(tbl,fld,x) get##fld##Of##tbl(tbl##Rec *pRec, x)
#define _GETTER3(tbl,fld,x,y) get##fld##Of##tbl(tbl##Rec *pRec, x, y)
#define _GETTER4(tbl,fld,x,y,z) get##fld##Of##tbl(tbl##Rec *pRec, x, y,z)

// Direct getter for a field.  Defines an inline function like:
//    getSomeFieldOfXyz(XyzRec *pRec) { return pRec->m_SomeField;}
//  Note that the returned value declaration is NOT included.
#if METADATATRACKER_ENABLED
#define _GETFLD(tbl,fld) _GETTER(tbl,fld){ PVOID pVal = (BYTE*)pRec + offsetof(tbl##Rec, m_##fld); \
    pVal = MetaDataTracker::NoteAccess(pVal); \
    return ((tbl##Rec*)((BYTE*)pVal - offsetof(tbl##Rec, m_##fld)))->Get##fld(); }
#else
#define _GETFLD(tbl,fld) _GETTER(tbl,fld){  return pRec->Get##fld();}
#endif

// These functions call the helper function getIX to get a two or four byte value from a record,
//  and then use that value as an index into the appropriate pool.
//    getSomeFieldOfXyz(XyzRec *pRec) { return m_pStrings->GetString(getIX(pRec, _COLDEF(tbl,fld))); }
//  Note that the returned value declaration is NOT included.

// Column definition of a field:  Looks like:
//    m_XyzCol[XyzRec::COL_SomeField]
#define _COLDEF(tbl,fld) m_TableDefs[TBL_##tbl].m_pColDefs[tbl##Rec::COL_##fld]
#define _COLPAIR(tbl,fld) _COLDEF(tbl,fld), tbl##Rec::COL_##fld
// Size of a record.
#define _CBREC(tbl) m_TableDefs[TBL_##tbl].m_cbRec
// Count of records in a table.
#define _TBLCNT(tbl) m_Schema.m_cRecs[TBL_##tbl]

#define _GETSTRA(tbl,fld) _GETTER2(tbl, fld, LPCSTR *pszString) \
{ return getString(getI4(pRec, _COLDEF(tbl,fld)) & m_iStringsMask, pszString); }

#define _GETSTRW(tbl,fld) _GETTER4(tbl, fld, LPWSTR szOut, ULONG cchBuffer, ULONG *pcchBuffer) \
{ return getStringW(getI4(pRec, _COLDEF(tbl,fld)) & m_iStringsMask, szOut, cchBuffer, pcchBuffer); }

#define _GETSTR(tbl, fld) \
    __checkReturn HRESULT _GETSTRA(tbl, fld); \
    __checkReturn HRESULT _GETSTRW(tbl, fld);


#define _GETGUID(tbl,fld) _GETTER2(tbl,fld,GUID *pGuid) \
{ return getGuid(getI4(pRec, _COLDEF(tbl,fld)) & m_iGuidsMask, pGuid); }

#define _GETBLOB(tbl,fld) __checkReturn HRESULT _GETTER3(tbl,fld,const BYTE **ppbData,ULONG *pcbSize) \
{                                                                               \
    MetaData::DataBlob data;                                                    \
    HRESULT hr = getBlob(getI4(pRec, _COLDEF(tbl,fld)) & m_iBlobsMask, &data);  \
    *ppbData = data.GetDataPointer();                                           \
    *pcbSize = (ULONG)data.GetSize();                                           \
    return hr;                                                                  \
}

#define _GETSIGBLOB(tbl,fld) __checkReturn HRESULT _GETTER3(tbl,fld,PCCOR_SIGNATURE *ppbData,ULONG *pcbSize)  \
{                                                                                       \
    MetaData::DataBlob data;                                                            \
    HRESULT hr = getBlob(getI4(pRec, _COLDEF(tbl,fld)) & m_iBlobsMask, &data);          \
    *ppbData = (PCCOR_SIGNATURE)data.GetDataPointer();                                  \
    *pcbSize = (ULONG)data.GetSize();                                                   \
    return hr;                                                                          \
}

// Like the above functions, but just returns the RID, not a looked-up value.
#define _GETRID(tbl,fld) _GETTER(tbl,fld) \
{ return getIX(pRec, _COLDEF(tbl,fld)); }

// Like a RID, but turn into an actual token.
#define _GETTKN(tbl,fld,tok)  _GETTER(tbl,fld) \
{ return TokenFromRid(getIX(pRec, _COLDEF(tbl,fld)), tok); }

// Get a coded token.
#define _GETCDTKN(tbl,fld,toks)  _GETTER(tbl,fld) \
{ return decodeToken(getIX(pRec, _COLDEF(tbl,fld)), toks, sizeof(toks)/sizeof(toks[0])); }

// Functions for the start and end of a list.
#define _GETLIST(tbl,fld,tbl2) \
    RID _GETRID(tbl,fld); \
    __checkReturn HRESULT getEnd##fld##Of##tbl(RID nRowIndex, RID *pEndRid) { return getEndRidForColumn(TBL_##tbl, nRowIndex, _COLDEF(tbl,fld), TBL_##tbl2, pEndRid); }


#define BYTEARRAY_TO_COLDES(bytearray) (CMiniColDef*)((bytearray) + 1)
#define COLDES_TO_BYTEARRAY(coldes) (((BYTE*)(coldes))-1)


//*****************************************************************************
// Base class for the MiniMd.  This class provides the schema to derived
//  classes.  It defines some virtual functions for access to data, suitable
//  for use by functions where utmost performance is NOT a requirement.
//  Finally, it provides some searching functions, built on the virtual
//  data access functions (it is here assumed that if we are searching a table
//  for some value, the cost of a virtual function call is acceptable).
// Some static utility functions and associated static data, shared across
//  implementations, is provided here.
//
// NB: It's unfortunate that CMiniMDBase "implements" IMetaModelCommonRO rather
//     than IMetaModelCommon, as methods on IMetaModelCommonRO are by definition,
//     not Enc-aware. Ideally, CMiniMDBase should only implement IMetaModelCommon
//     and CMiniMd should be the one implementing IMetaModelCommonRO.
//
//     To make that happen would be a substantial refactoring job as RegMeta
//     always embeds CMiniMdRW even when it was opened for ReadOnly.
//*****************************************************************************
class CMiniMdBase : public IMetaModelCommonRO
{

    friend class VerifyLayoutsMD; // verifies class layout doesn't accidentally change

public:
    CMiniMdBase();
    ~CMiniMdBase();
    __checkReturn
    virtual HRESULT vGetRow(UINT32 nTableIndex, UINT32 nRowIndex, void **ppRow) = 0;
    ULONG GetCountRecs(ULONG ixTbl);
    ULONG GetCountTables() { return m_TblCount;}

    // Search a table for the row containing the given key value.
    //  EG. Constant table has pointer back to Param or Field.
    __checkReturn
    virtual HRESULT vSearchTable(           // RID of matching row, or 0.
        ULONG       ixTbl,              // Table to search.
        CMiniColDef sColumn,            // Sorted key column, containing search value.
        ULONG       ulTarget,           // Target for search.
        RID        *pRid) = 0;

    // Search a table for the highest-RID row containing a value that is less than
    //  or equal to the target value.  EG.  TypeDef points to first Field, but if
    //  a TypeDef has no fields, it points to first field of next TypeDef.
    __checkReturn
    virtual HRESULT vSearchTableNotGreater( // RID of matching row, or 0.
        ULONG       ixTbl,              // Table to search.
        CMiniColDef sColumn,            // the column def containing search value
        ULONG       ulTarget,           // target for search
        RID        *pRid) = 0;

    // Search for a custom value with a given type.
    __checkReturn
    HRESULT FindCustomAttributeFor(// RID of custom value, or 0.
        RID         rid,                // The object's rid.
        mdToken     tkOjb,              // The object's type.
        mdToken     tkType,             // Type of custom value.
        RID        *pFoundRid);

    // Search for the specified Column Definition array in the global cache
    BOOL FindSharedColDefs(// TRUE if we found a match in the global cache and updated pTable, FALSE otherwise
        CMiniTableDef *pTable,          // The table def that wants the column definition array
        CMiniColDef *pColsToMatch,      // The columns that we need to match
        DWORD       ixTbl);

    // Return RID to EventMap table, given the rid to a TypeDef.
    __checkReturn
    HRESULT FindEventMapFor(RID ridParent, RID *pFoundRid);

    // Return RID to PropertyMap table, given the rid to a TypeDef.
    __checkReturn
    HRESULT FindPropertyMapFor(RID ridParent, RID *pFoundRid);

#if BIGENDIAN
    // Swap a constant
    __checkReturn
    HRESULT SwapConstant(const void *pBlobValue, DWORD dwType, VOID *pConstant, ULONG BlobLength);
#endif

    // Pull two or four bytes out of a record.
    inline static ULONG getIX(const void *pRec, CMiniColDef &def)
    {
        PVOID pVal = (BYTE *)pRec + def.m_oColumn;
        if (def.m_cbColumn == 2)
        {
            METADATATRACKER_ONLY(pVal = MetaDataTracker::NoteAccess(pVal));
            ULONG ix = GET_UNALIGNED_VAL16(pVal);
            return ix;
        }
        _ASSERTE(def.m_cbColumn == 4);
        METADATATRACKER_ONLY(pVal = MetaDataTracker::NoteAccess(pVal));
        return GET_UNALIGNED_VAL32(pVal);
    }

    inline static ULONG getIX_NoLogging(const void *pRec, CMiniColDef &def)
    {
        PVOID pVal = (BYTE *)pRec + def.m_oColumn;
        if (def.m_cbColumn == 2)
        {
            ULONG ix = GET_UNALIGNED_VAL16(pVal);
            return ix;
        }
        _ASSERTE(def.m_cbColumn == 4);
        return GET_UNALIGNED_VAL32(pVal);
    }

    // Pull four bytes out of a record.
    FORCEINLINE static ULONG getI1(const void *pRec, CMiniColDef &def)
    {
        PVOID pVal = (BYTE *)pRec + def.m_oColumn;
        METADATATRACKER_ONLY(pVal = MetaDataTracker::NoteAccess(pVal));
        return *(BYTE*)pVal;
    }

    // Pull four bytes out of a record.
    FORCEINLINE static ULONG getI4(const void *pRec, CMiniColDef &def)
    {
        PVOID pVal = (BYTE *)pRec + def.m_oColumn;
        METADATATRACKER_ONLY(pVal = MetaDataTracker::NoteAccess(pVal));
        return GET_UNALIGNED_VAL32(pVal);
    }

    // Function to encode a token into fewer bits.  Looks up token type in array of types.
    ULONG static encodeToken(RID rid, mdToken typ, const mdToken rTokens[], ULONG32 cTokens);

    // Decode a token.
    inline static mdToken decodeToken(mdToken val, const mdToken rTokens[], ULONG32 cTokens)
    {
        //<TODO>@FUTURE: make compile-time calculation</TODO>
        ULONG32 ix = (ULONG32)(val & ~(-1 << m_cb[cTokens]));
        // If the coded token has an invalid table index, return the first entry
        //  from the array of valid token types.  It would be preferable to
        //  return an error or to raise an exception.
        if (ix >= cTokens)
            return rTokens[0];
        return TokenFromRid(val >> m_cb[cTokens], rTokens[ix]);
    }
    static const int m_cb[];

    // Given a token, what table does it live in?
    inline ULONG GetTblForToken(mdToken tk)
    {
        tk = TypeFromToken(tk);
        return (tk < mdtString) ? tk >> 24 : (ULONG) -1;
    }

    //*****************************************************************************
    // Returns whether the data has been verified, which means it was verified by a
    // trusted source and has not changed.
    //
    // If so, this means the following aspects of the data can be trusted as accurate:
    //   - m_Schema.IsSorted[TBL_PropertyMap] reflects whether that table is sorted by Parent (see CMiniMdRW::PreSaveFull)
    //   - m_Schema.IsSorted[TBL_EventMap] reflects whether that table is sorted by Parent (see CMiniMdRW::PreSaveFull)
    //
    // Currently, metadata saved in NGen images is the only trusted source.
    //*****************************************************************************
    BOOL IsVerified()
    {
        return m_fVerifiedByTrustedSource && CommonIsRo();
    }

    void SetVerifiedByTrustedSource(BOOL fVerifiedByTrustedSource)
    {
        m_fVerifiedByTrustedSource = fVerifiedByTrustedSource;
    }

    STDMETHODIMP GetRvaOffsetData(// S_OK or error
        DWORD   *pFirstMethodRvaOffset,     // [OUT] Offset (from start of metadata) to the first RVA field in MethodDef table.
        DWORD   *pMethodDefRecordSize,      // [OUT] Size of each record in MethodDef table.
        DWORD   *pMethodDefCount,           // [OUT] Number of records in MethodDef table.
        DWORD   *pFirstFieldRvaOffset,      // [OUT] Offset (from start of metadata) to the first RVA field in FieldRVA table.
        DWORD   *pFieldRvaRecordSize,       // [OUT] Size of each record in FieldRVA table.
        DWORD   *pFieldRvaCount)            // [OUT] Number of records in FieldRVA table.
    {
        _ASSERTE("Not implemented");
        return E_NOTIMPL;
    }

    //*****************************************************************************
    // Some of the tables need coded tokens, not just rids (ie, the column can
    //  refer to more than one other table).  Code the tokens into as few bits
    //  as possible, by using 1, 2, 3, etc., bits to code the token type, then
    //  use that value to index into an array of token types.
    //*****************************************************************************
    static const mdToken mdtTypeDefOrRef[3];
    static const mdToken mdtHasConstant[3];
    static const mdToken mdtHasCustomAttribute[24];
    static const mdToken mdtHasFieldMarshal[2];
    static const mdToken mdtHasDeclSecurity[3];
    static const mdToken mdtMemberRefParent[5];
    static const mdToken mdtHasSemantic[2];
    static const mdToken mdtMethodDefOrRef[2];
    static const mdToken mdtMemberForwarded[2];
    static const mdToken mdtImplementation[3];
    static const mdToken mdtCustomAttributeType[5];
    static const mdToken mdtResolutionScope[4];
    static const mdToken mdtTypeOrMethodDef[2];


public:
    virtual BOOL IsWritable() = 0;


protected:
    DAC_ALIGNAS(8)
    CMiniMdSchema   m_Schema;                       // data header.
    ULONG           m_TblCount;                     // Tables in this database.
    BOOL            m_fVerifiedByTrustedSource;     // whether the data was verified by a trusted source

    // Declare CMiniColDefs for every table.  They look like either:
    // static const BYTE s_xyz[];
    // or
    // static const BYTE* s_xyz;

    #include "mdcolumndescriptors.h"

    static const BYTE* const s_TableColumnDescriptors[TBL_COUNT];
    CMiniTableDef   m_TableDefs[TBL_COUNT];

    ULONG       m_iStringsMask;
    ULONG       m_iGuidsMask;
    ULONG       m_iBlobsMask;

    __checkReturn
    HRESULT SchemaPopulate(const void *pvData, ULONG cbData, ULONG *pcbUsed);
    __checkReturn
    HRESULT SchemaPopulate(const CMiniMdBase &that);
    __checkReturn
    HRESULT InitColsForTable(CMiniMdSchema &Schema, int ixTbl, CMiniTableDef *pTable, int bExtra, BOOL fUsePointers);
    __checkReturn
    HRESULT SchemaPopulate2(ULONG *pcbTables, int bExtra=false);
    const CMiniTableDef* GetTableDefTemplate(int ixTbl);
    __checkReturn
    HRESULT SetNewColumnDefinition(CMiniTableDef *pTable, CMiniColDef* pCols, DWORD ixTbl);

private:

    BOOL UsesAllocatedMemory(CMiniColDef* pCols);
};


#ifdef FEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN
#define MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED() MarkUnsafeToDelete()
#else
#define MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED()
#endif

//*****************************************************************************
// This class defines the interface to the MiniMd.  The template parameter is
//  a derived class which provides implementations for a few primitives that
//  the interface is built upon.
// To use, declare a class:
//      class CMyMiniMd : public CMiniMdTemplate<CMyMiniMd> {...};
//  and provide implementations of the primitives.  Any non-trivial
//  implementation will also provide initialization, and probably serialization
//  functions as well.
//*****************************************************************************
template <class Impl> class CMiniMdTemplate : public CMiniMdBase
{
#ifdef FEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN
protected:
    CMiniMdTemplate() : m_isSafeToDelete(TRUE) { }
#endif

    // Primitives -- these must be implemented in the Impl class.
public:
    __checkReturn
    FORCEINLINE HRESULT getString(UINT32 nIndex, __out LPCSTR *pszString)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetString(nIndex, pszString);
    }
    __checkReturn
    FORCEINLINE HRESULT getStringW(ULONG nIndex, __inout_ecount (cchBuffer) LPWSTR szOut, ULONG cchBuffer, ULONG *pcchBuffer)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetStringW(nIndex, szOut, cchBuffer, pcchBuffer);
    }
    __checkReturn
    FORCEINLINE HRESULT getGuid(UINT32 nIndex, GUID *pGuid)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetGuid(nIndex, pGuid);
    }
    __checkReturn
    FORCEINLINE HRESULT getBlob(UINT32 nIndex, __out MetaData::DataBlob *pData)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetBlob(nIndex, pData);
    }
    __checkReturn
    FORCEINLINE HRESULT getRow(UINT32 nTableIndex, UINT32 nRowIndex, __deref_out void **ppRow)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetRow(nTableIndex, nRowIndex, reinterpret_cast<BYTE **>(ppRow));
    }
    __checkReturn
    FORCEINLINE HRESULT getEndRidForColumn(
              UINT32       nTableIndex,
              RID          nRowIndex,
              CMiniColDef &columnDefinition,
              UINT32       nTargetTableIndex,
        __out RID         *pEndRid)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_GetEndRidForColumn(nTableIndex, nRowIndex, columnDefinition, nTargetTableIndex, pEndRid);
    }
    __checkReturn
    FORCEINLINE HRESULT doSearchTable(ULONG ixTbl, CMiniColDef sColumn, ULONG ixColumn, ULONG ulTarget, RID *pFoundRid)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return static_cast<Impl*>(this)->Impl_SearchTable(ixTbl, sColumn, ixColumn, ulTarget, pFoundRid);
    }

    // IMetaModelCommonRO interface beginning
    __checkReturn
    HRESULT CommonGetScopeProps(
        LPCUTF8     *pszName,
        GUID        *pMvid)
    {
        HRESULT hr = S_OK;
        ModuleRec *pRec;
        IfFailRet(GetModuleRecord(1, &pRec));
        if (pszName != NULL)
        {
            IfFailRet(getNameOfModule(pRec, pszName));
        }
        if (pMvid != NULL)
        {
            IfFailRet(getMvidOfModule(pRec, pMvid));
        }
        return hr;
    }

    //*****************************************************************************
    // Search a table for multiple (adjacent) rows containing the given
    //  key value.  EG, InterfaceImpls all point back to the implementing class.
    //*****************************************************************************
    __checkReturn
    HRESULT SearchTableForMultipleRows(
        ULONG       ixTbl,      // Table to search.
        CMiniColDef sColumn,    // Sorted key column, containing search value.
        ULONG       ulTarget,   // Target for search.
        RID        *pEnd,       // [OPTIONAL, OUT]
        RID        *pFoundRid)  // First RID found, or 0.
    {
        HRESULT hr;
        RID     ridBegin;   // RID of first entry.
        RID     ridEnd;     // RID of first entry past last entry.

        // Search for any entry in the table.
        IfFailRet(static_cast<Impl*>(this)->vSearchTable(ixTbl, sColumn, ulTarget, &ridBegin));

        // If nothing found, return invalid RID.
        if (ridBegin == 0)
        {
            if (pEnd != NULL)
            {
                *pEnd = 0;
            }
            *pFoundRid = 0;
            return S_OK;
        }

        // End will be at least one larger than found record.
        ridEnd = ridBegin + 1;

        // Search back to start of group.
        for (;;)
        {
            void *pRow;
            if (ridBegin <= 1)
            {
                break;
            }
            IfFailRet(static_cast<Impl*>(this)->vGetRow(ixTbl, ridBegin-1, &pRow));
            if (getIX(pRow, sColumn) != ulTarget)
            {
                break;
            }
            --ridBegin;
        }

        // If desired, search forward to end of group.
        if (pEnd != NULL)
        {
            for (;;)
            {
                void *pRow;
                if (ridEnd > GetCountRecs(ixTbl))
                {
                    break;
                }
                IfFailRet(static_cast<Impl*>(this)->vGetRow(ixTbl, ridEnd, &pRow));
                if (getIX(pRow, sColumn) != ulTarget)
                {
                    break;
                }
                ++ridEnd;
            }
            *pEnd = ridEnd;
        }

        *pFoundRid = ridBegin;
        return S_OK;
    } // SearchTableForMultipleRows

    //*****************************************************************************
    // Get name and sig of a methodDef
    //*****************************************************************************
    HRESULT CommonGetMethodDefProps(
        mdMethodDef      tkMethodDef,
        LPCUTF8         *pszName,
        DWORD           *pdwFlags,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG           *pcbSigBlob
        )
    {

        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        HRESULT hr;

        LPCUTF8         szName;
        DWORD           dwFlags;
        PCCOR_SIGNATURE pvSigBlob;
        ULONG           cbSigBlob;

        _ASSERTE(TypeFromToken(tkMethodDef) == mdtMethodDef);
        MethodRec *pMethodRec;
        IfFailRet(GetMethodRecord(RidFromToken(tkMethodDef), &pMethodRec));
        IfFailRet(getNameOfMethod(pMethodRec, &szName));
        dwFlags = getFlagsOfMethod(pMethodRec);
        IfFailRet(getSignatureOfMethod(pMethodRec, &pvSigBlob, &cbSigBlob));

        if (pszName)
            *pszName = szName;
        if (pdwFlags)
            *pdwFlags = dwFlags;
        if (ppvSigBlob)
            *ppvSigBlob = pvSigBlob;
        if (pcbSigBlob)
            *pcbSigBlob = cbSigBlob;

        return S_OK;
    }

    HRESULT CommonGetMemberRefProps(
        mdMemberRef      tkMemberRef,
        mdToken          *pParentToken
        )
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        HRESULT hr;

        _ASSERTE(TypeFromToken(tkMemberRef) == mdtMemberRef);

        MemberRefRec *pMemberRefRec;
        IfFailRet(GetMemberRefRecord(RidFromToken(tkMemberRef), &pMemberRefRec));
        if (pParentToken != NULL)
            *pParentToken = getClassOfMemberRef(pMemberRefRec);

        return S_OK;
    }




    __checkReturn
    HRESULT CommonGetTypeRefProps(
        mdTypeRef   tr,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkResolution)
    {
        HRESULT     hr = S_OK;
        TypeRefRec *pRec;
        IfFailRet(GetTypeRefRecord(RidFromToken(tr), &pRec));
        if (pszNamespace != NULL)
        {
            IfFailRet(getNamespaceOfTypeRef(pRec, pszNamespace));
        }
        if (pszName != NULL)
        {
            IfFailRet(getNameOfTypeRef(pRec, pszName));
        }
        if (ptkResolution != NULL)
        {
            *ptkResolution = getResolutionScopeOfTypeRef(pRec);
        }
        return hr;
    }

    __checkReturn
    virtual HRESULT CommonGetTypeDefProps(
        mdTypeDef   td,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        DWORD       *pdwFlags,
        mdToken     *pdwExtends,
        ULONG       *pMethodList)
    {
        HRESULT     hr = S_OK;
        TypeDefRec *pRec;
        IfFailRet(GetTypeDefRecord(RidFromToken(td), &pRec));
        if (pszNamespace != NULL)
        {
            IfFailRet(getNamespaceOfTypeDef(pRec, pszNamespace));
        }
        if (pszName != NULL)
        {
            IfFailRet(getNameOfTypeDef(pRec, pszName));
        }
        if (pdwFlags != NULL)
        {
            *pdwFlags = getFlagsOfTypeDef(pRec);
        }
        if (pdwExtends != NULL)
        {
            *pdwExtends = getExtendsOfTypeDef(pRec);
        }
        if (pMethodList != NULL)
        {
            *pMethodList = getMethodListOfTypeDef(pRec);
        }
        return hr;
    }

    __checkReturn
    virtual HRESULT CommonGetTypeSpecProps(
        mdTypeSpec  ts,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig)
    {
        HRESULT      hr = S_OK;
        TypeSpecRec *pRec;
        IfFailRet(GetTypeSpecRecord(RidFromToken(ts), &pRec));
        ULONG       cb;
        IfFailRet(getSignatureOfTypeSpec(pRec, ppvSig, &cb));
        *pcbSig = cb;
        return hr;
    }

    __checkReturn
    virtual HRESULT CommonGetEnclosingClassOfTypeDef(
        mdTypeDef  td,
        mdTypeDef *ptkEnclosingTypeDef)
    {
        _ASSERTE(ptkEnclosingTypeDef != NULL);

        HRESULT hr;
        NestedClassRec *pRec;
        RID     iRec;

        IfFailRet(FindNestedClassFor(RidFromToken(td), &iRec));
        if (iRec == 0)
        {
            *ptkEnclosingTypeDef = mdTypeDefNil;
            return S_OK;
        }

        IfFailRet(GetNestedClassRecord(iRec, &pRec));
        *ptkEnclosingTypeDef = getEnclosingClassOfNestedClass(pRec);
        return S_OK;
    }


    __checkReturn
    virtual HRESULT CommonGetAssemblyProps(
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKey,
        ULONG       *pcbPublicKey,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale)
    {
        HRESULT      hr = S_OK;
        AssemblyRec *pRec;

        IfFailRet(GetAssemblyRecord(1, &pRec));

        if (pusMajorVersion) *pusMajorVersion = pRec->GetMajorVersion();
        if (pusMinorVersion) *pusMinorVersion = pRec->GetMinorVersion();
        if (pusBuildNumber) *pusBuildNumber = pRec->GetBuildNumber();
        if (pusRevisionNumber) *pusRevisionNumber = pRec->GetRevisionNumber();
        if (pdwFlags != NULL)
        {
            *pdwFlags = pRec->GetFlags();
        }

        // Turn on the afPublicKey if PublicKey blob is not empty
        if (pdwFlags != NULL)
        {
            DWORD cbPublicKey;
            const BYTE *pbPublicKey;
            IfFailRet(getPublicKeyOfAssembly(pRec, &pbPublicKey, &cbPublicKey));
            if (cbPublicKey)
                *pdwFlags |= afPublicKey;
        }
        if (ppbPublicKey != NULL)
        {
            IfFailRet(getPublicKeyOfAssembly(pRec, reinterpret_cast<const BYTE **>(ppbPublicKey), pcbPublicKey));
        }
        if (pszName != NULL)
        {
            IfFailRet(getNameOfAssembly(pRec, pszName));
        }
        if (pszLocale != NULL)
        {
            IfFailRet(getLocaleOfAssembly(pRec, pszLocale));
        }
        return hr;
    }

    __checkReturn
    virtual HRESULT CommonGetAssemblyRefProps(
        mdAssemblyRef tkAssemRef,
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKeyOrToken,
        ULONG       *pcbPublicKeyOrToken,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale,
        const void  **ppbHashValue,
        ULONG       *pcbHashValue)
    {
        HRESULT         hr = S_OK;
        AssemblyRefRec *pRec;

        IfFailRet(GetAssemblyRefRecord(RidFromToken(tkAssemRef), &pRec));

        if (pusMajorVersion) *pusMajorVersion = pRec->GetMajorVersion();
        if (pusMinorVersion) *pusMinorVersion = pRec->GetMinorVersion();
        if (pusBuildNumber) *pusBuildNumber = pRec->GetBuildNumber();
        if (pusRevisionNumber) *pusRevisionNumber = pRec->GetRevisionNumber();
        if (pdwFlags) *pdwFlags = pRec->GetFlags();
        if (ppbPublicKeyOrToken != NULL)
        {
            IfFailRet(getPublicKeyOrTokenOfAssemblyRef(pRec, reinterpret_cast<const BYTE **>(ppbPublicKeyOrToken), pcbPublicKeyOrToken));
        }
        if (pszName != NULL)
        {
            IfFailRet(getNameOfAssemblyRef(pRec, pszName));
        }
        if (pszLocale != NULL)
        {
            IfFailRet(getLocaleOfAssemblyRef(pRec, pszLocale));
        }
        if (ppbHashValue != NULL)
        {
            IfFailRet(getHashValueOfAssemblyRef(pRec, reinterpret_cast<const BYTE **>(ppbHashValue), pcbHashValue));
        }
        return hr;
    }

    __checkReturn
    virtual HRESULT CommonGetModuleRefProps(
        mdModuleRef tkModuleRef,
        LPCUTF8     *pszName)
    {
        HRESULT       hr = S_OK;
        ModuleRefRec *pRec;

        IfFailRet(GetModuleRefRecord(RidFromToken(tkModuleRef), &pRec));
        IfFailRet(getNameOfModuleRef(pRec, pszName));
        return hr;
    }

    __checkReturn
    HRESULT CommonFindExportedType(
        LPCUTF8     szNamespace,
        LPCUTF8     szName,
        mdToken     tkEnclosingType,
        mdExportedType   *ptkExportedType)
    {
        HRESULT hr;
        ExportedTypeRec  *pRec;
        ULONG       ulCount;
        LPCUTF8     szTmp;
        mdToken     tkImpl;

        _ASSERTE(szName && ptkExportedType);

        // Set NULL namespace to empty string.
        if (!szNamespace)
            szNamespace = "";

        // Set output to Nil.
        *ptkExportedType = mdTokenNil;

        ulCount = getCountExportedTypes();
        while (ulCount)
        {
            IfFailRet(GetExportedTypeRecord(ulCount--, &pRec));

            // Handle the case of nested vs. non-nested classes.
            tkImpl = getImplementationOfExportedType(pRec);
            if (TypeFromToken(tkImpl) == mdtExportedType && !IsNilToken(tkImpl))
            {
                // Current ExportedType being looked at is a nested type, so
                // comparing the implementation token.
                if (tkImpl != tkEnclosingType)
                    continue;
            }
            else if (TypeFromToken(tkEnclosingType) == mdtExportedType &&
                     !IsNilToken(tkEnclosingType))
            {
                // ExportedType passed in is nested but the current ExportedType is not.
                continue;
            }

            // Compare name and namespace.
            IfFailRet(getTypeNameOfExportedType(pRec, &szTmp));
            if (strcmp(szTmp, szName))
                continue;
            IfFailRet(getTypeNamespaceOfExportedType(pRec, &szTmp));
            if (!strcmp(szTmp, szNamespace))
            {
                *ptkExportedType = TokenFromRid(ulCount+1, mdtExportedType);
                return S_OK;
            }
        }
        return CLDB_E_RECORD_NOTFOUND;
    }

    __checkReturn
    virtual HRESULT CommonGetExportedTypeProps(
        mdToken     tkExportedType,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkImpl)
    {
        HRESULT          hr = S_OK;
        ExportedTypeRec *pRec;

        IfFailRet(GetExportedTypeRecord(RidFromToken(tkExportedType), &pRec));

        if (pszNamespace != NULL)
        {
            IfFailRet(getTypeNamespaceOfExportedType(pRec, pszNamespace));
        }
        if (pszName != NULL)
        {
            IfFailRet(getTypeNameOfExportedType(pRec, pszName));
        }
        if (ptkImpl) *ptkImpl = getImplementationOfExportedType(pRec);
        return hr;
    }

    int CommonIsRo()
    {
        return static_cast<Impl*>(this)->Impl_IsRo();
    }



    HRESULT CommonGetCustomAttributeProps(
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken          *ptkObj,           // [OUT, OPTIONAL] Put object token here.
        mdToken          *ptkType,          // [OUT, OPTIONAL] Put AttrType token here.
        const void      **ppBlob,           // [OUT, OPTIONAL] Put pointer to data here.
        ULONG            *pcbSize)          // [OUT, OPTIONAL] Put size of date here.
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        HRESULT hr;
        _ASSERTE(TypeFromToken(cv) == mdtCustomAttribute);
        CustomAttributeRec  *pRec;              // A CustomAttribute record.
        const void *pBlob;
        ULONG      cbSize;
        IfFailRet(GetCustomAttributeRecord(RidFromToken(cv), &pRec));
        if (ptkObj)
            *ptkObj = getParentOfCustomAttribute(pRec);
        if (ptkType)
            *ptkType = getTypeOfCustomAttribute(pRec);

        if (!ppBlob)
            ppBlob = &pBlob;
        if (!pcbSize)
            pcbSize = &cbSize;
        IfFailRet(getValueOfCustomAttribute(pRec, (const BYTE **)ppBlob, pcbSize));
        return S_OK;
    }

    //*****************************************************************************
    // Get name, parent and flags of a fieldDef
    //*****************************************************************************
    HRESULT CommonGetFieldDefProps(
        mdFieldDef      tkFieldDef,
        mdTypeDef       *ptkParent,
        LPCUTF8         *pszName,
        DWORD           *pdwFlags
        )
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");
        _ASSERTE(TypeFromToken(tkFieldDef) == mdtFieldDef);

        HRESULT hr;

        FieldRec *pFieldRec;
        IfFailRet(GetFieldRecord(RidFromToken(tkFieldDef), &pFieldRec));

        if(ptkParent)
        {
            IfFailRet(FindParentOfField(RidFromToken(tkFieldDef), (RID *) ptkParent));
            RidToToken(*ptkParent, mdtTypeDef);
        }
        if (pszName)
            IfFailRet(getNameOfField(pFieldRec, pszName));
        if (pdwFlags)
            *pdwFlags = getFlagsOfField(pFieldRec);

        return S_OK;
    }

    __checkReturn
    HRESULT FindParentOfMethodHelper(mdMethodDef md, mdTypeDef *ptd)
    {
        HRESULT hr;
        IfFailRet(FindParentOfMethod(RidFromToken(md), (RID *)ptd));
        RidToToken(*ptd, mdtTypeDef);
        return NOERROR;
    }

    //*****************************************************************************
    // Helper function to lookup and retrieve a CustomAttribute.
    //*****************************************************************************
    __checkReturn
    HRESULT CompareCustomAttribute(
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        ULONG       rid)                    // [IN] the rid of the custom attribute to compare to
    {
        CustomAttributeRec  *pRec;          // A CustomAttribute record.
        LPCUTF8     szNamespaceTmp = NULL;  // Namespace of a CustomAttribute's type.
        LPCUTF8     szNameTmp = NULL;       // Name of a CustomAttribute's type.
        int         iLen;                   // Length of a component name.
        HRESULT     hr = S_FALSE;
        HRESULT     hrMatch = S_FALSE;

        if (!_IsValidTokenBase(tkObj))
            return COR_E_BADIMAGEFORMAT;

        // Get the row.
        IfFailGo(GetCustomAttributeRecord(rid, &pRec));

        // Check the parent.  In debug, always check.  In retail, only when scanning.
        mdToken tkParent;
        tkParent = getParentOfCustomAttribute(pRec);
        if (tkObj != tkParent)
        {
            goto ErrExit;
        }

        hr = CommonGetNameOfCustomAttribute(rid, &szNamespaceTmp, &szNameTmp);
        if (hr != S_OK)
            goto ErrExit;

        iLen = -1;
        if (*szNamespaceTmp)
        {
            iLen = (int)strlen(szNamespaceTmp);
            if (strncmp(szName, szNamespaceTmp, iLen) != 0)
                goto ErrExit;
            // Namespace separator after the Namespace?
            if (szName[iLen] != NAMESPACE_SEPARATOR_CHAR)
                goto ErrExit;
        }
        // Check the type name after the separator.
        if (strcmp(szName+iLen+1, szNameTmp) != 0)
            goto ErrExit;

        hrMatch = S_OK;
    ErrExit:

        if (FAILED(hr))
            return hr;

        return hrMatch;
    }   // CompareCustomAttribute


    //*****************************************************************************
    // Helper function to lookup the name of a custom attribute
    // Note that this function can return S_FALSE to support being called
    //   by CompareCustomAttribute.  See GetTypeDefRefTokenInTypeSpec for
    //   details on when this will happen.
    //*****************************************************************************
    __checkReturn
    HRESULT CommonGetNameOfCustomAttribute(
        ULONG       rid,                    // [IN] the rid of the custom attribute
        LPCUTF8    *pszNamespace,           // [OUT] Namespace of Custom Attribute.
        LPCUTF8    *pszName)                // [OUT] Name of Custom Attribute.
    {
        CustomAttributeRec  *pRec;          // A CustomAttribute record.
        mdToken     tkTypeTmp;              // Type of some CustomAttribute.
        RID         ridTmp;                 // Rid of some custom value.
        HRESULT     hr = S_FALSE;

        // Get the row.
        IfFailGo(GetCustomAttributeRecord(rid, &pRec));

        // Get the type.
        tkTypeTmp = getTypeOfCustomAttribute(pRec);

        // If the record is a MemberRef or a MethodDef, we will come back here to check
        //  the type of the parent.
    CheckParentType:

        if (!_IsValidTokenBase(tkTypeTmp))
            return COR_E_BADIMAGEFORMAT;

        ridTmp = RidFromToken(tkTypeTmp);

        // Get the name of the type.
        switch (TypeFromToken(tkTypeTmp))
        {
        case mdtTypeRef:
            {
                TypeRefRec *pTR;
                IfFailGo(GetTypeRefRecord(ridTmp, &pTR));
                IfFailGo(getNamespaceOfTypeRef(pTR, pszNamespace));
                IfFailGo(getNameOfTypeRef(pTR, pszName));
            }
            break;
        case mdtTypeDef:
            {
                TypeDefRec *pTD;
                IfFailGo(GetTypeDefRecord(ridTmp, &pTD));
                IfFailGo(getNamespaceOfTypeDef(pTD, pszNamespace));
                IfFailGo(getNameOfTypeDef(pTD, pszName));
            }
            break;
        case mdtTypeSpec:
            {
                // If this has an encoded token, we'll take a look. If it contains
                // a base type, we'll just return a non-match.

                hr = GetTypeDefRefTokenInTypeSpec(tkTypeTmp, &tkTypeTmp);
                IfFailGo(hr);

                if (hr == S_OK)
                    // Ok, tkTypeTmp should be the type token now.
                    goto CheckParentType;

                // This doesn't have a coded typedef or typeref.
                goto ErrExit;
            }
        case mdtMethodDef:
            {
                // Follow the parent.
                IfFailGo( FindParentOfMethodHelper(tkTypeTmp, &tkTypeTmp));
                goto CheckParentType;
            }
            break;
        case mdtMemberRef:
            {
                MemberRefRec *pMember;
                IfFailGo(GetMemberRefRecord(ridTmp, &pMember));
                // Follow the parent.
                tkTypeTmp = getClassOfMemberRef(pMember);
                goto CheckParentType;
            }
            break;
        case mdtString:
        default:
            if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertOnBadImageFormat))
                _ASSERTE(!"Unexpected token type in FindCustomAttributeByName");
            hr = COR_E_BADIMAGEFORMAT;
            goto ErrExit;
        } // switch (TypeFromToken(tkTypeTmp))

        hr = S_OK;
    ErrExit:

        return hr;
    }   // CommonGetNameOfCustomAttribute

    __checkReturn
    HRESULT GetTypeDefRefTokenInTypeSpec(mdTypeSpec  tkTypeSpec,     // [IN] TypeSpec token to look
                                                             mdToken    *tkEnclosedToken) // [OUT] The enclosed type token
    {
        _ASSERTE(TypeFromToken(tkTypeSpec) == mdtTypeSpec);
        if (TypeFromToken(tkTypeSpec) != mdtTypeSpec || !_IsValidTokenBase(tkTypeSpec))
            return COR_E_BADIMAGEFORMAT;

        HRESULT hr;
        TypeSpecRec *pTS;
        IfFailRet(GetTypeSpecRecord(RidFromToken(tkTypeSpec), &pTS));
        ULONG cbSig = 0;
        PCCOR_SIGNATURE pSig;
        PCCOR_SIGNATURE pEnd;
        ULONG data = 0;

        IfFailRet(getSignatureOfTypeSpec(pTS, &pSig, &cbSig));
        pEnd = pSig + cbSig;

        if (cbSig == 0)
            return COR_E_BADIMAGEFORMAT;

        pSig += CorSigUncompressData(pSig, &data);

        while (pSig < pEnd && CorIsModifierElementType((CorElementType) data))
        {
            pSig += CorSigUncompressData(pSig, &data);
        }

        // See if the signature was bad
        if (pSig >= pEnd)
            return COR_E_BADIMAGEFORMAT;

        // pSig should point to the element type now.
        if (data == ELEMENT_TYPE_VALUETYPE || data == ELEMENT_TYPE_CLASS)
        {
            // Get the new type token
            if (CorSigUncompressToken(pSig, tkEnclosedToken) == 0)
                return COR_E_BADIMAGEFORMAT;

            // Ok, tkEnclosedToken should be the type token now
            return S_OK;
        }

        // The enclosed type is a base type or an array. We don't have a token to hand out
        *tkEnclosedToken = mdTokenNil;

        return S_FALSE;
    }



    //*****************************************************************************
    // Given a scope, return the number of tokens in a given table
    //*****************************************************************************
    ULONG CommonGetRowCount(     // return hresult
        DWORD       tkKind)                 // [IN] pass in the kind of token.
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        ULONG       ulCount = 0;

        switch (tkKind)
        {
        case mdtTypeDef:
            ulCount = getCountTypeDefs();
            break;
        case mdtTypeRef:
            ulCount = getCountTypeRefs();
            break;
        case mdtMethodDef:
            ulCount = getCountMethods();
            break;
        case mdtFieldDef:
            ulCount = getCountFields();
            break;
        case mdtMemberRef:
            ulCount = getCountMemberRefs();
            break;
        case mdtInterfaceImpl:
            ulCount = getCountInterfaceImpls();
            break;
        case mdtParamDef:
            ulCount = getCountParams();
            break;
        case mdtFile:
            ulCount = getCountFiles();
            break;
        case mdtAssemblyRef:
            ulCount = getCountAssemblyRefs();
            break;
        case mdtAssembly:
            ulCount = getCountAssemblys();
            break;
        case mdtCustomAttribute:
            ulCount = getCountCustomAttributes();
            break;
        case mdtModule:
            ulCount = getCountModules();
            break;
        case mdtPermission:
            ulCount = getCountDeclSecuritys();
            break;
        case mdtSignature:
            ulCount = getCountStandAloneSigs();
            break;
        case mdtEvent:
            ulCount = getCountEvents();
            break;
        case mdtProperty:
            ulCount = getCountPropertys();
            break;
        case mdtModuleRef:
            ulCount = getCountModuleRefs();
            break;
        case mdtTypeSpec:
            ulCount = getCountTypeSpecs();
            break;
        case mdtExportedType:
            ulCount = getCountExportedTypes();
            break;
        case mdtManifestResource:
            ulCount = getCountManifestResources();
            break;
        case mdtGenericParam:
            ulCount = getCountGenericParams();
            break;
        case mdtMethodSpec:
            ulCount = getCountMethodSpecs();
            break;
        default:
            Debug_ReportError("Invalid token kind (table)");
            ulCount = 0;
            break;
        }
        return ulCount;
    } // ULONG CommonGetRowCount()



    //*****************************************************************************
    // Locate methodimpl token range for a given typeDef
    //*****************************************************************************
    HRESULT CommonGetMethodImpls(
        mdTypeDef   tkTypeDef,              // [IN] typeDef to scope search
        mdToken    *ptkMethodImplFirst,     // [OUT] returns first methodImpl token
        ULONG      *pMethodImplCount        // [OUT] returns # of methodImpl tokens scoped to type
        )
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);
        _ASSERTE(ptkMethodImplFirst != NULL);
        _ASSERTE(pMethodImplCount != NULL);

        HRESULT hr;

        RID ridEnd;
        RID ridStart;
        IfFailGo(getMethodImplsForClass(RidFromToken(tkTypeDef), &ridEnd, &ridStart));
        *pMethodImplCount = ridEnd - ridStart;
        if (*pMethodImplCount)
        {
            *ptkMethodImplFirst = TokenFromRid(TBL_MethodImpl << 24, ridStart);
        }
        hr = S_OK;

     ErrExit:
        return hr;
    }

    //*****************************************************************************
    // Extract row info for methodImpl
    //*****************************************************************************
    HRESULT CommonGetMethodImplProps(
        mdToken     tkMethodImpl,           // [IN] methodImpl
        mdToken    *pBody,                  // [OUT] returns body token
        mdToken    *pDecl                   // [OUT] returns decl token
        )
    {
        _ASSERTE(!IsWritable() && "IMetaModelCommonRO methods cannot be used because this importer is writable.");

        HRESULT hr;
        _ASSERTE(TypeFromToken(tkMethodImpl) == (TBL_MethodImpl << 24));
        _ASSERTE(pBody != NULL);
        _ASSERTE(pDecl != NULL);
        MethodImplRec *pRec;
        IfFailGo(GetMethodImplRecord(RidFromToken(tkMethodImpl), &pRec));
        *pBody = getMethodBodyOfMethodImpl(pRec);
        *pDecl = getMethodDeclarationOfMethodImpl(pRec);
        hr = S_OK;
      ErrExit:
        return hr;
    }

    // IMetaModelCommonRO interface end




public:
//  friend class CLiteWeightStgdb;

    __checkReturn
    virtual HRESULT vGetRow(UINT32 nTableIndex, UINT32 nRowIndex, void **ppRow)
    {
        return getRow(nTableIndex, nRowIndex, ppRow);
    }

public:

    //*************************************************************************
    // This group of functions are table-level (one function per table).  Functions like
    //  getting a count of rows.

    // Functions to get the count of rows in a table.  Functions like:
    //   ULONG getCountXyzs() { return m_Schema.m_cRecs[TBL_Xyz];}
#undef MiniMdTable
#define MiniMdTable(tbl) ULONG getCount##tbl##s() { return _TBLCNT(tbl); }
    MiniMdTables();
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    PortablePdbMiniMdTables();
#endif
    // macro misspells some names.
    ULONG getCountProperties() {return getCountPropertys();}
    ULONG getCountMethodSemantics() {return getCountMethodSemanticss();}

    // Functions for getting a row by rid.  Look like:
    //   HRESULT GetXyzRecord(RID rid, XyzRec **ppRecord) { return m_Tables[TBL_Xyz].GetRecord(rid, ppRecord); }
    //   e.g.:
    //   HRESULT GetMethodRecord(RID rid, MethodRec **ppRecord) { return m_Tables[TBL_Method].GetRecord(rid, ppRecord); }
    #undef MiniMdTable
#define MiniMdTable(tbl) __checkReturn HRESULT Get##tbl##Record(RID rid, tbl##Rec **ppRecord) { \
        return getRow(TBL_##tbl, rid, reinterpret_cast<void **>(ppRecord)); }
    MiniMdTables();
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    PortablePdbMiniMdTables();
#endif

    //*************************************************************************
    // These are specialized searching functions.  Mostly generic (ie, find
    //  a custom value for any object).

    // Functions to search for a record relating to another record.
    // Return RID to Constant table.
    __checkReturn
    HRESULT FindConstantFor(RID rid, mdToken typ, RID *pFoundRid)
    { return doSearchTable(TBL_Constant, _COLPAIR(Constant,Parent), encodeToken(rid,typ,mdtHasConstant,lengthof(mdtHasConstant)), pFoundRid); }

    // Return RID to FieldMarshal table.
    __checkReturn
    HRESULT FindFieldMarshalFor(RID rid, mdToken typ, RID *pFoundRid)
    { return doSearchTable(TBL_FieldMarshal, _COLPAIR(FieldMarshal,Parent), encodeToken(rid,typ,mdtHasFieldMarshal,lengthof(mdtHasFieldMarshal)), pFoundRid); }

    // Return RID to ClassLayout table, given the rid to a TypeDef.
    __checkReturn
    HRESULT FindClassLayoutFor(RID rid, RID *pFoundRid)
    { return doSearchTable(TBL_ClassLayout, _COLPAIR(ClassLayout,Parent), RidFromToken(rid), pFoundRid); }

    // given a rid to the Event table, find an entry in EventMap table that contains the back pointer
    // to its typedef parent
    __checkReturn
    HRESULT FindEventMapParentOfEvent(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_EventMap, _COLDEF(EventMap,EventList), rid, pFoundRid);
    }
    // return the parent eventmap rid given a event rid
    __checkReturn
    HRESULT FindParentOfEvent(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_EventMap, _COLDEF(EventMap,EventList), rid, pFoundRid);
    }

    // given a rid to the Event table, find an entry in EventMap table that contains the back pointer
    // to its typedef parent
    __checkReturn
    HRESULT FindPropertyMapParentOfProperty(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_PropertyMap, _COLDEF(PropertyMap,PropertyList), rid, pFoundRid);
    }
    // return the parent propertymap rid given a property rid
    __checkReturn
    HRESULT FindParentOfProperty(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_PropertyMap, _COLDEF(PropertyMap,PropertyList), rid, pFoundRid);
    }

    // Return RID to MethodSemantics table, given the rid to a MethodDef.
    __checkReturn
    HRESULT FindMethodSemanticsFor(RID rid, RID *pFoundRid)
    { return doSearchTable(TBL_MethodSemantics, _COLPAIR(MethodSemantics,Method), RidFromToken(rid), pFoundRid); }

    // return the parent typedef rid given a field def rid
    __checkReturn
    HRESULT FindParentOfField(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_TypeDef, _COLDEF(TypeDef,FieldList), rid, pFoundRid);
    }

    // return the parent typedef rid given a method def rid
    __checkReturn
    HRESULT FindParentOfMethod(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_TypeDef, _COLDEF(TypeDef,MethodList), rid, pFoundRid);
    }

    __checkReturn
    HRESULT FindParentOfParam(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_Method, _COLDEF(Method,ParamList), rid, pFoundRid);
    }

    // Find a FieldLayout record given the corresponding Field.
    __checkReturn
    HRESULT FindFieldLayoutFor(RID rid, RID *pFoundRid)
    {   return doSearchTable(TBL_FieldLayout, _COLPAIR(FieldLayout, Field), rid, pFoundRid); }

    // Return RID to Constant table.
    __checkReturn
    HRESULT FindImplMapFor(RID rid, mdToken typ, RID *pFoundRid)
    { return doSearchTable(TBL_ImplMap, _COLPAIR(ImplMap,MemberForwarded), encodeToken(rid,typ,mdtMemberForwarded,lengthof(mdtMemberForwarded)), pFoundRid); }

    // Return RID to FieldRVA table.
    __checkReturn
    HRESULT FindFieldRVAFor(RID rid, RID *pFoundRid)
    {   return doSearchTable(TBL_FieldRVA, _COLPAIR(FieldRVA, Field), rid, pFoundRid); }

    // Find a NestedClass record given the corresponding Field.
    __checkReturn
    HRESULT FindNestedClassFor(RID rid, RID *pFoundRid)
    {   return doSearchTable(TBL_NestedClass, _COLPAIR(NestedClass, NestedClass), rid, pFoundRid); }

    //*************************************************************************
    // These are table-specific functions.

    // ModuleRec
    _GETSTR(Module,Name);
    __checkReturn HRESULT _GETGUID(Module,Mvid);
    __checkReturn HRESULT _GETGUID(Module,EncId);
    __checkReturn HRESULT _GETGUID(Module,EncBaseId);

    // TypeRefRec
    mdToken _GETCDTKN(TypeRef, ResolutionScope, mdtResolutionScope);
    _GETSTR(TypeRef, Name);
    _GETSTR(TypeRef, Namespace);

    // TypeDefRec
    ULONG _GETFLD(TypeDef,Flags);           // USHORT getFlagsOfTypeDef(TypeDefRec *pRec);
    _GETSTR(TypeDef,Name);
    _GETSTR(TypeDef,Namespace);

    _GETLIST(TypeDef,FieldList,Field);      // RID getFieldListOfTypeDef(TypeDefRec *pRec);
    _GETLIST(TypeDef,MethodList,Method);    // RID getMethodListOfTypeDef(TypeDefRec *pRec);
    mdToken _GETCDTKN(TypeDef,Extends,mdtTypeDefOrRef); // mdToken getExtendsOfTypeDef(TypeDefRec *pRec);

    __checkReturn
    HRESULT getGenericParamsForTypeDef(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_GenericParam,
                            _COLDEF(GenericParam,Owner),
                            encodeToken(rid, mdtTypeDef, mdtTypeOrMethodDef, lengthof(mdtTypeOrMethodDef)),
                            pEnd,
                            pFoundRid);
    }
    __checkReturn
    HRESULT getGenericParamsForMethodDef(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_GenericParam,
                            _COLDEF(GenericParam,Owner),
                            encodeToken(rid, mdtMethodDef, mdtTypeOrMethodDef, lengthof(mdtTypeOrMethodDef)),
                            pEnd,
                            pFoundRid);
    }
    __checkReturn
    HRESULT getMethodSpecsForMethodDef(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_MethodSpec,
                            _COLDEF(MethodSpec,Method),
                            encodeToken(rid, mdtMethodDef, mdtMethodDefOrRef, lengthof(mdtMethodDefOrRef)),
                            pEnd,
                            pFoundRid);
    }
    __checkReturn
    HRESULT getMethodSpecsForMemberRef(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_MethodSpec,
                            _COLDEF(MethodSpec,Method),
                            encodeToken(rid, mdtMemberRef, mdtMethodDefOrRef, lengthof(mdtMethodDefOrRef)),
                            pEnd,
                            pFoundRid);
    }
    __checkReturn
    HRESULT getInterfaceImplsForTypeDef(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_InterfaceImpl,
                            _COLDEF(InterfaceImpl,Class),
                            rid,
                            pEnd,
                            pFoundRid);
    }

    // FieldPtr
    ULONG   _GETRID(FieldPtr,Field);

    // FieldRec
    USHORT  _GETFLD(Field,Flags);           // USHORT getFlagsOfField(FieldRec *pRec);
    _GETSTR(Field,Name);                    // HRESULT getNameOfField(FieldRec *pRec, LPCUTF8 *pszString);
    _GETSIGBLOB(Field,Signature);           // HRESULT getSignatureOfField(FieldRec *pRec, PCCOR_SIGNATURE *ppbData, ULONG *pcbSize);

    // MethodPtr
    ULONG   _GETRID(MethodPtr,Method);

    // MethodRec
    ULONG   _GETFLD(Method,RVA);
    USHORT  _GETFLD(Method,ImplFlags);
    USHORT  _GETFLD(Method,Flags);
    _GETSTR(Method,Name);                   // HRESULT getNameOfMethod(MethodRec *pRec, LPCUTF8 *pszString);
    _GETSIGBLOB(Method,Signature);          // HRESULT getSignatureOfMethod(MethodRec *pRec, PCCOR_SIGNATURE *ppbData, ULONG *pcbSize);
    _GETLIST(Method,ParamList,Param);

    // ParamPtr
    ULONG   _GETRID(ParamPtr,Param);

    // ParamRec
    USHORT  _GETFLD(Param,Flags);
    USHORT  _GETFLD(Param,Sequence);
    _GETSTR(Param,Name);

    // InterfaceImplRec
    mdToken _GETTKN(InterfaceImpl,Class,mdtTypeDef);
    mdToken _GETCDTKN(InterfaceImpl,Interface,mdtTypeDefOrRef);

    // MemberRefRec
    mdToken _GETCDTKN(MemberRef,Class,mdtMemberRefParent);
    _GETSTR(MemberRef,Name);
    _GETSIGBLOB(MemberRef,Signature);       // HRESULT getSignatureOfMemberRef(MemberRefRec *pRec, PCCOR_SIGNATURE *ppbData, ULONG *pcbSize);

    // ConstantRec
    BYTE    _GETFLD(Constant,Type);
    mdToken _GETCDTKN(Constant,Parent,mdtHasConstant);
    _GETBLOB(Constant,Value);

    // CustomAttributeRec
    __checkReturn
    HRESULT getCustomAttributeForToken(mdToken  tk, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_CustomAttribute,
                            _COLDEF(CustomAttribute,Parent),
                            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtHasCustomAttribute, lengthof(mdtHasCustomAttribute)),
                            pEnd,
                            pFoundRid);
    }

    mdToken _GETCDTKN(CustomAttribute,Parent,mdtHasCustomAttribute);
    mdToken _GETCDTKN(CustomAttribute,Type,mdtCustomAttributeType);
    _GETBLOB(CustomAttribute,Value);

    // FieldMarshalRec
    mdToken _GETCDTKN(FieldMarshal,Parent,mdtHasFieldMarshal);
    _GETSIGBLOB(FieldMarshal,NativeType);

    // DeclSecurityRec
    __checkReturn
    HRESULT getDeclSecurityForToken(mdToken tk, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_DeclSecurity,
                            _COLDEF(DeclSecurity,Parent),
                            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtHasDeclSecurity, lengthof(mdtHasDeclSecurity)),
                            pEnd,
                            pFoundRid);
    }

    short _GETFLD(DeclSecurity,Action);
    mdToken _GETCDTKN(DeclSecurity,Parent,mdtHasDeclSecurity);
    _GETBLOB(DeclSecurity,PermissionSet);

    // ClassLayoutRec
    USHORT _GETFLD(ClassLayout,PackingSize);
    ULONG _GETFLD(ClassLayout,ClassSize);
    ULONG _GETTKN(ClassLayout,Parent, mdtTypeDef);

    // FieldLayout
    ULONG _GETFLD(FieldLayout,OffSet);
    ULONG _GETTKN(FieldLayout, Field, mdtFieldDef);

    // Event map.
    _GETLIST(EventMap,EventList,Event);
    ULONG _GETRID(EventMap, Parent);

    // EventPtr
    ULONG   _GETRID(EventPtr, Event);

    // Event.
    USHORT _GETFLD(Event,EventFlags);
    _GETSTR(Event,Name);
    mdToken _GETCDTKN(Event,EventType,mdtTypeDefOrRef);

    // Property map.
    _GETLIST(PropertyMap,PropertyList,Property);
    ULONG _GETRID(PropertyMap, Parent);

    // PropertyPtr
    ULONG   _GETRID(PropertyPtr, Property);

    // Property.
    USHORT _GETFLD(Property,PropFlags);
    _GETSTR(Property,Name);
    _GETSIGBLOB(Property,Type);

    // MethodSemantics.
    // Given an event or a property token, return the beginning/ending
    // associates.
    //
    __checkReturn
    HRESULT getAssociatesForToken(mdToken tk, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_MethodSemantics,
                            _COLDEF(MethodSemantics,Association),
                            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtHasSemantic, lengthof(mdtHasSemantic)),
                            pEnd,
                            pFoundRid);
    }

    USHORT _GETFLD(MethodSemantics,Semantic);
    mdToken _GETTKN(MethodSemantics,Method,mdtMethodDef);
    mdToken _GETCDTKN(MethodSemantics,Association,mdtHasSemantic);

    // MethodImpl
    // Given a class token, return the beginning/ending MethodImpls.
    //
    __checkReturn
    HRESULT getMethodImplsForClass(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_MethodImpl,
                            _COLDEF(MethodImpl, Class),
                            rid,
                            pEnd,
                            pFoundRid);
    }

    mdToken _GETTKN(MethodImpl,Class,mdtTypeDef);
    mdToken _GETCDTKN(MethodImpl,MethodBody, mdtMethodDefOrRef);
    mdToken _GETCDTKN(MethodImpl, MethodDeclaration, mdtMethodDefOrRef);

    // StandAloneSigRec
    _GETSIGBLOB(StandAloneSig,Signature);       // HRESULT getSignatureOfStandAloneSig(StandAloneSigRec *pRec, PCCOR_SIGNATURE *ppbData, ULONG *pcbSize);

    // TypeSpecRec
    // const BYTE* getSignatureOfTypeSpec(TypeSpecRec *pRec, ULONG *pcb);
    _GETSIGBLOB(TypeSpec,Signature);

    // ModuleRef
    _GETSTR(ModuleRef,Name);

    // ENCLog
    ULONG _GETFLD(ENCLog, FuncCode);                // ULONG getFuncCodeOfENCLog(ENCLogRec *pRec);

    // ImplMap
    USHORT _GETFLD(ImplMap, MappingFlags);          // USHORT getMappingFlagsOfImplMap(ImplMapRec *pRec);
    mdToken _GETCDTKN(ImplMap, MemberForwarded, mdtMemberForwarded);    // mdToken getMemberForwardedOfImplMap(ImplMapRec *pRec);
    _GETSTR(ImplMap, ImportName);                           // HRESULT getImportNameOfImplMap(ImplMapRec *pRec, LPCUTF8 *pszString);
    mdToken _GETTKN(ImplMap, ImportScope, mdtModuleRef);    // mdToken getImportScopeOfImplMap(ImplMapRec *pRec);

    // FieldRVA
    ULONG _GETFLD(FieldRVA, RVA);                   // ULONG getRVAOfFieldRVA(FieldRVARec *pRec);
    mdToken _GETTKN(FieldRVA, Field, mdtFieldDef);  // mdToken getFieldOfFieldRVA(FieldRVARec *pRec);

    // Assembly
    ULONG _GETFLD(Assembly, HashAlgId);
    USHORT _GETFLD(Assembly, MajorVersion);
    USHORT _GETFLD(Assembly, MinorVersion);
    USHORT _GETFLD(Assembly, BuildNumber);
    USHORT _GETFLD(Assembly, RevisionNumber);
    ULONG _GETFLD(Assembly, Flags);
    _GETBLOB(Assembly, PublicKey);
    _GETSTR(Assembly, Name);
    _GETSTR(Assembly, Locale);

    // AssemblyRef
    USHORT _GETFLD(AssemblyRef, MajorVersion);
    USHORT _GETFLD(AssemblyRef, MinorVersion);
    USHORT _GETFLD(AssemblyRef, BuildNumber);
    USHORT _GETFLD(AssemblyRef, RevisionNumber);
    ULONG _GETFLD(AssemblyRef, Flags);
    _GETBLOB(AssemblyRef, PublicKeyOrToken);
    _GETSTR(AssemblyRef, Name);
    _GETSTR(AssemblyRef, Locale);
    _GETBLOB(AssemblyRef, HashValue);

    // File
    ULONG _GETFLD(File, Flags);
    _GETSTR(File, Name);
    _GETBLOB(File, HashValue);

    // ExportedType
    ULONG _GETFLD(ExportedType, Flags);
    ULONG _GETFLD(ExportedType, TypeDefId);
    _GETSTR(ExportedType, TypeName);
    _GETSTR(ExportedType, TypeNamespace);
    mdToken _GETCDTKN(ExportedType, Implementation, mdtImplementation);

    // ManifestResource
    ULONG _GETFLD(ManifestResource, Offset);
    ULONG _GETFLD(ManifestResource, Flags);
    _GETSTR(ManifestResource, Name);
    mdToken _GETCDTKN(ManifestResource, Implementation, mdtImplementation);

    // NestedClass
    mdToken _GETTKN(NestedClass, NestedClass, mdtTypeDef);
    mdToken _GETTKN(NestedClass, EnclosingClass, mdtTypeDef);

    int GetSizeOfMethodNameColumn()
    {
        return _COLDEF(Method,Name).m_cbColumn;
    }

    // GenericParRec
    USHORT _GETFLD(GenericParam,Number);
    USHORT _GETFLD(GenericParam,Flags);
    mdToken _GETCDTKN(GenericParam,Owner,mdtTypeOrMethodDef);
    _GETSTR(GenericParam,Name);

    __checkReturn
    HRESULT getGenericParamConstraintsForGenericParam(RID rid, RID *pEnd, RID *pFoundRid)
    {
        return SearchTableForMultipleRows(TBL_GenericParamConstraint,
                            _COLDEF(GenericParamConstraint,Owner),
                            rid,
                            pEnd,
                            pFoundRid);
    }

    // MethodSpecRec
    mdToken _GETCDTKN(MethodSpec,Method,mdtMethodDefOrRef);
    _GETSIGBLOB(MethodSpec,Instantiation);

    //GenericParamConstraintRec
    mdToken _GETTKN(GenericParamConstraint,Owner,mdtGenericParam);
    mdToken _GETCDTKN(GenericParamConstraint,Constraint,mdtTypeDefOrRef);

    BOOL SupportsGenerics()
    {
        // Only 2.0 of the metadata (and 1.1) support generics
        return (m_Schema.m_major >= METAMODEL_MAJOR_VER_V2_0 ||
                (m_Schema.m_major == METAMODEL_MAJOR_VER_B1 && m_Schema.m_minor == METAMODEL_MINOR_VER_B1));
    }// SupportGenerics

    protected:
    //*****************************************************************************
    // Helper: determine if a token is valid or not
    //*****************************************************************************
    BOOL _IsValidTokenBase(
        mdToken tk)
    {
        BOOL bRet = FALSE;
        RID  rid = RidFromToken(tk);

        if (rid != 0)
        {
            switch (TypeFromToken(tk))
            {
            case mdtModule:
                // can have only one module record
                bRet = (rid <= getCountModules());
                break;
            case mdtTypeRef:
                bRet = (rid <= getCountTypeRefs());
                break;
            case mdtTypeDef:
                bRet = (rid <= getCountTypeDefs());
                break;
            case mdtFieldDef:
                bRet = (rid <= getCountFields());
                break;
            case mdtMethodDef:
                bRet = (rid <= getCountMethods());
                break;
            case mdtParamDef:
                bRet = (rid <= getCountParams());
                break;
            case mdtInterfaceImpl:
                bRet = (rid <= getCountInterfaceImpls());
                break;
            case mdtMemberRef:
                bRet = (rid <= getCountMemberRefs());
                break;
            case mdtCustomAttribute:
                bRet = (rid <= getCountCustomAttributes());
                break;
            case mdtPermission:
                bRet = (rid <= getCountDeclSecuritys());
                break;
            case mdtSignature:
                bRet = (rid <= getCountStandAloneSigs());
                break;
            case mdtEvent:
                bRet = (rid <= getCountEvents());
                break;
            case mdtProperty:
                bRet = (rid <= getCountPropertys());
                break;
            case mdtModuleRef:
                bRet = (rid <= getCountModuleRefs());
                break;
            case mdtTypeSpec:
                bRet = (rid <= getCountTypeSpecs());
                break;
            case mdtAssembly:
                bRet = (rid <= getCountAssemblys());
                break;
            case mdtAssemblyRef:
                bRet = (rid <= getCountAssemblyRefs());
                break;
            case mdtFile:
                bRet = (rid <= getCountFiles());
                break;
            case mdtExportedType:
                bRet = (rid <= getCountExportedTypes());
                break;
            case mdtManifestResource:
                bRet = (rid <= getCountManifestResources());
                break;
            case mdtGenericParam:
                bRet = (rid <= getCountGenericParams());
                break;
            case mdtGenericParamConstraint:
                bRet = (rid <= getCountGenericParamConstraints());
                break;
            case mdtMethodSpec:
                bRet = (rid <= getCountMethodSpecs());
                break;
            default:
                _ASSERTE(!bRet);
                Debug_ReportError("Unknown token kind!");
            }
        }
        return bRet;
    } // _IsValidToken

#ifdef FEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN
    bool IsSafeToDelete()
    {
        return m_isSafeToDelete;
    }

    FORCEINLINE void MarkUnsafeToDelete() { m_isSafeToDelete = false; }

    bool m_isSafeToDelete; // This starts out true, but gets set to FALSE if we detect
                           // a MiniMd API call that might have given out an internal pointer.
#endif

};  //class CMiniMdTemplate<Impl>


//-----------------------------------------------------------------------------------------------------
// A common interface unifying RegMeta and MDInternalRO, giving the adapter a common interface to
// access the raw metadata.
//-----------------------------------------------------------------------------------------------------

// {4F8EE8A3-24F8-4241-BC75-C8CAEC0255B5}
EXTERN_GUID(IID_IMDCommon, 0x4f8ee8a3, 0x24f8, 0x4241, 0xbc, 0x75, 0xc8, 0xca, 0xec, 0x2, 0x55, 0xb5);

#undef  INTERFACE
#define INTERFACE IID_IMDCommon
DECLARE_INTERFACE_(IMDCommon, IUnknown)
{
    STDMETHOD_(IMetaModelCommon*, GetMetaModelCommon)() PURE;
    STDMETHOD_(IMetaModelCommonRO*, GetMetaModelCommonRO)() PURE;
    STDMETHOD(GetVersionString)(LPCSTR *pszVersionString) PURE;
};


#undef SETP
#undef _GETCDTKN
#undef _GETTKN
#undef _GETRID
#undef _GETBLOB
#undef _GETGUID
#undef _GETSTR
#undef SCHEMA

#endif // _METAMODEL_H_
// eof ------------------------------------------------------------------------
