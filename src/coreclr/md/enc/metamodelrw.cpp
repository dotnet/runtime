// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModelRW.cpp
//

//
// Implementation for the Read/Write MiniMD code.
//
//*****************************************************************************
#include "stdafx.h"
#include <limits.h>
#include <posterror.h>
#include <metamodelrw.h>
#include <stgio.h>
#include <stgtiggerstorage.h>
#include "mdlog.h"
#include "rwutil.h"
#include "../compiler/importhelper.h"
#include "metadata.h"
#include "streamutil.h"

#ifdef _MSC_VER
#pragma intrinsic(memcpy)
#endif

//********** RidMap ***********************************************************
typedef CDynArray<RID> RIDMAP;


//********** Types. ***********************************************************
#define INDEX_ROW_COUNT_THRESHOLD 25


//********** Locals. **********************************************************
enum MetaDataSizeIndex
{
    // Standard MetaData sizes (from VBA library).
    MDSizeIndex_Standard = 0,
    // Minimal MetaData sizes used mainly by Reflection.Emit for small assemblies (emitting 1 type per
    // assembly).
    // Motivated by the performance requirement in collectible types.
    MDSizeIndex_Minimal  = 1,

    MDSizeIndex_Count
};  // enum MetaDataSizeIndex

// Gets index of MetaData sizes used to access code:g_PoolSizeInfo, code:g_HashSize and code:g_TblSizeInfo.
static
enum MetaDataSizeIndex
GetMetaDataSizeIndex(const OptionValue *pOptionValue)
{
    if (pOptionValue->m_InitialSize == MDInitialSizeMinimal)
    {
        return MDSizeIndex_Minimal;
    }
    _ASSERTE(pOptionValue->m_InitialSize == MDInitialSizeDefault);
    return MDSizeIndex_Standard;
} // GetSizeHint

#define IX_STRING_POOL 0
#define IX_US_BLOB_POOL 1
#define IX_GUID_POOL 2
#define IX_BLOB_POOL 3

static
const ULONG
g_PoolSizeInfo[MDSizeIndex_Count][4][2] =
{
    {   // Standard pool sizes { Size in bytes, Number of buckets in hash } (code:MDSizeIndex_Standard).
        {20000, 449},   // Strings
        {5000,  150},   // User literal string blobs
        {256,   16},    // Guids
        {20000, 449}    // Blobs
    },
    {   // Minimal pool sizes { Size in bytes, Number of buckets in hash } (code:MDSizeIndex_Minimal).
        {300, 10},  // Strings
        {50,  5},   // User literal string blobs
        {16,  3},   // Guids
        {200, 10}   // Blobs
    }
};

static
const ULONG
g_HashSize[MDSizeIndex_Count] =
{
    257,    // Standard MetaData size (code:MDSizeIndex_Standard).
    50      // Minimal MetaData size (code:MDSizeIndex_Minimal).
};

static
const ULONG
g_TblSizeInfo[MDSizeIndex_Count][TBL_COUNT] =
{
    // Standard table sizes (code:MDSizeIndex_Standard).
    {
       1,           // Module
       90,          // TypeRef
       65,          // TypeDef
       0,           // FieldPtr
       400,         // Field
       0,           // MethodPtr
       625,         // Method
       0,           // ParamPtr
       1200,        // Param
       6,           // InterfaceImpl
       500,         // MemberRef
       400,         // Constant
       650,         // CustomAttribute
       0,           // FieldMarshal
       0,           // DeclSecurity
       0,           // ClassLayout
       0,           // FieldLayout
       175,         // StandAloneSig
       0,           // EventMap
       0,           // EventPtr
       0,           // Event
       5,           // PropertyMap
       0,           // PropertyPtr
       25,          // Property
       45,          // MethodSemantics
       20,          // MethodImpl
       0,           // ModuleRef
       0,           // TypeSpec
       0,           // ImplMap
       0,           // FieldRVA
       0,           // ENCLog
       0,           // ENCMap
       0,           // Assembly
       0,           // AssemblyProcessor
       0,           // AssemblyOS
       0,           // AssemblyRef
       0,           // AssemblyRefProcessor
       0,           // AssemblyRefOS
       0,           // File
       0,           // ExportedType
       0,           // ManifestResource
       0,           // NestedClass
       0,           // GenericParam
       0,           // MethodSpec
       0,           // GenericParamConstraint
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
       /* Dummy tables to fill the gap to 0x30 */
       0,           // Dummy1
       0,           // Dummy2
       0,           // Dummy3
       /* Actual portable PDB tables */
       0,           // Document
       0,           // MethodDebugInformation
       0,           // LocalScope
       0,           // LocalVariable
       0,           // LocalConstant
       0,           // ImportScope
       // TODO:
       // 0,           // StateMachineMethod
       // 0,           // CustomDebugInformation
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
    },
    // Minimal table sizes (code:MDSizeIndex_Minimal).
    {
       1,       // Module
       2,       // TypeRef
       2,       // TypeDef
       0,       // FieldPtr
       2,       // Field
       0,       // MethodPtr
       2,       // Method
       0,       // ParamPtr
       0,       // Param
       0,       // InterfaceImpl
       1,       // MemberRef
       0,       // Constant
       0,       // CustomAttribute
       0,       // FieldMarshal
       0,       // DeclSecurity
       0,       // ClassLayout
       0,       // FieldLayout
       0,       // StandAloneSig
       0,       // EventMap
       0,       // EventPtr
       0,       // Event
       0,       // PropertyMap
       0,       // PropertyPtr
       0,       // Property
       0,       // MethodSemantics
       0,       // MethodImpl
       0,       // ModuleRef
       0,       // TypeSpec
       0,       // ImplMap
       0,       // FieldRVA
       0,       // ENCLog
       0,       // ENCMap
       1,       // Assembly
       0,       // AssemblyProcessor
       0,       // AssemblyOS
       1,       // AssemblyRef
       0,       // AssemblyRefProcessor
       0,       // AssemblyRefOS
       0,       // File
       0,       // ExportedType
       0,       // ManifestResource
       0,       // NestedClass
       0,       // GenericParam
       0,       // MethodSpec
       0,       // GenericParamConstraint
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
       /* Dummy tables to fill the gap to 0x30 */
       0,       // Dummy1
       0,       // Dummy2
       0,       // Dummy3
       /* Actual portable PDB tables */
       0,       // Document
       0,       // MethodDebugInformation
       0,       // LocalScope
       0,       // LocalVariable
       0,       // LocalConstant
       0,       // ImportScope
       // TODO:
       // 0,       // StateMachineMethod
       // 0,       // CustomDebugInformation
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
    }
};  // g_TblSizeInfo

struct TblIndex
{
    ULONG m_iName;      // Name column.
    ULONG m_iParent;    // Parent column, if any.
    ULONG m_Token;      // Token of the table.
};

// Table to drive generic named-item indexing.
const TblIndex g_TblIndex[TBL_COUNT] =
{
    {(ULONG) -1,        (ULONG) -1,     mdtModule},         // Module
    {TypeRefRec::COL_Name,      (ULONG) -1,  mdtTypeRef},   // TypeRef
    {TypeDefRec::COL_Name,      (ULONG) -1,  mdtTypeDef},   // TypeDef
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // FieldPtr
    {(ULONG) -1,        (ULONG) -1,     mdtFieldDef},       // Field
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // MethodPtr
    {(ULONG) -1,        (ULONG) -1,     mdtMethodDef},      // Method
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // ParamPtr
    {(ULONG) -1,        (ULONG) -1,     mdtParamDef},       // Param
    {(ULONG) -1,        (ULONG) -1,     mdtInterfaceImpl},  // InterfaceImpl
    {MemberRefRec::COL_Name,    MemberRefRec::COL_Class,  mdtMemberRef},    // MemberRef
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // Constant
    {(ULONG) -1,        (ULONG) -1,     mdtCustomAttribute},// CustomAttribute
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // FieldMarshal
    {(ULONG) -1,        (ULONG) -1,     mdtPermission},     // DeclSecurity
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // ClassLayout
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // FieldLayout
    {(ULONG) -1,        (ULONG) -1,     mdtSignature},      // StandAloneSig
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // EventMap
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // EventPtr
    {(ULONG) -1,        (ULONG) -1,     mdtEvent},          // Event
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // PropertyMap
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // PropertyPtr
    {(ULONG) -1,        (ULONG) -1,     mdtProperty},       // Property
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // MethodSemantics
    {(ULONG) -1,        (ULONG) -1,     mdtMethodImpl},     // MethodImpl
    {(ULONG) -1,        (ULONG) -1,     mdtModuleRef},      // ModuleRef
    {(ULONG) -1,        (ULONG) -1,     mdtTypeSpec},       // TypeSpec
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // ImplMap  <TODO>@FUTURE:  Check that these are the right entries here.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // FieldRVA  <TODO>@FUTURE:  Check that these are the right entries here.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // ENCLog
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // ENCMap
    {(ULONG) -1,        (ULONG) -1,     mdtAssembly},       // Assembly <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // AssemblyProcessor <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // AssemblyOS <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     mdtAssemblyRef},    // AssemblyRef <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // AssemblyRefProcessor <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     (ULONG) -1},        // AssemblyRefOS <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     mdtFile},           // File <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     mdtExportedType},   // ExportedType <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     mdtManifestResource},// ManifestResource <TODO>@FUTURE: Update with the right number.</TODO>
    {(ULONG) -1,        (ULONG) -1,     mdtNestedClass},    // NestedClass
    {(ULONG) -1,        (ULONG) -1,     mdtGenericParam},   // GenericParam
    {(ULONG) -1,        (ULONG) -1,     mdtMethodSpec},     // MethodSpec
    {(ULONG) -1,        (ULONG) -1,     mdtGenericParamConstraint},// GenericParamConstraint
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    /* Dummy tables to fill the gap to 0x30 */
    {(ULONG)-1,        (ULONG)-1,     (ULONG)-1},           // Dummy1
    {(ULONG)-1,        (ULONG)-1,     (ULONG)-1},           // Dummy2
    {(ULONG)-1,        (ULONG)-1,     (ULONG)-1},           // Dummy3
    /* Actual portable PDB tables */
    {(ULONG)-1,        (ULONG)-1,     mdtDocument},         // Document
    {(ULONG)-1,        (ULONG)-1,     mdtMethodDebugInformation},// MethodDebugInformation
    {(ULONG)-1,        (ULONG)-1,     mdtLocalScope},       // LocalScope
    {(ULONG)-1,        (ULONG)-1,     mdtLocalVariable},    // LocalVariable
    {(ULONG)-1,        (ULONG)-1,     mdtLocalConstant},    // LocalConstant
    {(ULONG) -1,       (ULONG) -1,    mdtImportScope},      // ImportScope
    // TODO:
    // {(ULONG) -1,        (ULONG) -1,     mdtStateMachineMethod},// StateMachineMethod
    // {(ULONG) -1,        (ULONG) -1,     mdtCustomDebugInformation},// CustomDebugInformation
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
};

ULONG CMiniMdRW::m_TruncatedEncTables[] =
{
    TBL_ENCLog,
    TBL_ENCMap,
    (ULONG) -1
};

//*****************************************************************************
// Given a token type, return the table index.
//*****************************************************************************
ULONG CMiniMdRW::GetTableForToken(      // Table index, or -1.
    mdToken     tkn)                    // Token to find.
{
    ULONG       type = TypeFromToken(tkn);

    // Get the type -- if a string, no associated table.
    if (type >= mdtString)
        return (ULONG) -1;
    // Table number is same as high-byte of token.
    ULONG ixTbl = type >> 24;
    // Make sure.
    _ASSERTE(ixTbl < TBL_COUNT);

    return ixTbl;
} // CMiniMdRW::GetTableForToken

//*****************************************************************************
// Given a Table index, return the Token type.
//*****************************************************************************
mdToken CMiniMdRW::GetTokenForTable(    // Token type, or -1.
    ULONG       ixTbl)                  // Table index.
{
    _ASSERTE(g_TblIndex[ixTbl].m_Token == (ixTbl<<24)  || g_TblIndex[ixTbl].m_Token == (ULONG) -1);
    return g_TblIndex[ixTbl].m_Token;
} // CMiniMdRW::GetTokenForTable

//*****************************************************************************
// Helper classes for sorting MiniMdRW tables.
//*****************************************************************************
class CQuickSortMiniMdRW
{
protected:
    CMiniMdRW   &m_MiniMd;                  // The MiniMd with the data.
    ULONG       m_ixTbl;                    // The table.
    ULONG       m_ixCol;                    // The column.
    int         m_iCount;                   // How many items in array.
    int         m_iElemSize;                // Size of one element.
    RIDMAP      *m_pRidMap;                 // Rid map that need to be swapped as we swap data
    bool        m_bMapToken;                // MapToken handling desired.

    BYTE        m_buf[128];                 // For swapping.

    HRESULT getRow(UINT32 nIndex, void **ppRecord)
    {
        return m_MiniMd.m_Tables[m_ixTbl].GetRecord(nIndex, reinterpret_cast<BYTE **>(ppRecord));
    }
    void SetSorted() { m_MiniMd.SetSorted(m_ixTbl, true); }

    HRESULT PrepMapTokens()
    {
        HRESULT hr = S_OK;

        // If remap notifications are desired, prepare to collect the info in a RIDMAP.
        if (m_bMapToken)
        {
            _ASSERTE(m_pRidMap == NULL);    // Don't call twice.
            IfNullGo(m_pRidMap = new (nothrow) RIDMAP);
            if (!m_pRidMap->AllocateBlock(m_iCount + 1))
            {
                delete m_pRidMap;
                m_pRidMap = NULL;
                IfFailGo(E_OUTOFMEMORY);
            }
            for (int i=0; i<= m_iCount; ++i)
                *(m_pRidMap->Get(i)) = i;
        }

    ErrExit:
        return hr;
    } // CQuickSortMiniMdRW::PrepMapTokens

    __checkReturn
    HRESULT DoMapTokens()
    {
        HRESULT hr;
        if (m_bMapToken)
        {
            mdToken typ = m_MiniMd.GetTokenForTable(m_ixTbl);
            for (int i=1; i<=m_iCount; ++i)
            {
                IfFailRet(m_MiniMd.MapToken(*(m_pRidMap->Get(i)), i, typ));
            }
        }
        return S_OK;
    } // CQuickSortMiniMdRW::DoMapTokens

public:
    CQuickSortMiniMdRW(
        CMiniMdRW   &MiniMd,                // MiniMd with the data.
        ULONG       ixTbl,                  // The table.
        ULONG       ixCol,                  // The column.
        bool        bMapToken)              // If true, MapToken handling desired.
     :  m_MiniMd(MiniMd),
        m_ixTbl(ixTbl),
        m_ixCol(ixCol),
        m_pRidMap(NULL),
        m_bMapToken(bMapToken)
    {
        m_iElemSize = m_MiniMd.m_TableDefs[m_ixTbl].m_cbRec;
        _ASSERTE(m_iElemSize <= (int) sizeof(m_buf));
    }

    ~CQuickSortMiniMdRW()
    {
        if (m_bMapToken)
        {
            if (m_pRidMap)
            {
                m_pRidMap->Clear();
                delete m_pRidMap;
                m_pRidMap = NULL;
            }
            m_bMapToken = false;
        }
    } // CQuickSortMiniMdRW::~CQuickSortMiniMdRW

    // set the RidMap
    void SetRidMap(RIDMAP *pRidMap) { m_pRidMap = pRidMap; }

    //*****************************************************************************
    // Call to sort the array.
    //*****************************************************************************
    HRESULT Sort()
    {
        HRESULT hr = S_OK;

        INDEBUG(m_MiniMd.Debug_CheckIsLockedForWrite();)

        _ASSERTE(m_MiniMd.IsSortable(m_ixTbl));
        m_iCount = m_MiniMd.GetCountRecs(m_ixTbl);

        // If remap notifications are desired, prepare to collect the info in a RIDMAP.
        IfFailGo(PrepMapTokens());

        // We are going to sort tables. Invalidate the hash tables
        if ( m_MiniMd.m_pLookUpHashs[m_ixTbl] != NULL )
        {
            delete m_MiniMd.m_pLookUpHashs[m_ixTbl];
            m_MiniMd.m_pLookUpHashs[m_ixTbl] = NULL;
        }

        IfFailGo(SortRange(1, m_iCount));

        // The table is sorted until its next change.
        SetSorted();

        // If remap notifications were desired, send them.
        IfFailGo(DoMapTokens());

    ErrExit:
        return hr;
    } // CQuickSortMiniMdRW::Sort

    //*****************************************************************************
    // Call to check whether the array is sorted without altering it.
    //*****************************************************************************
    HRESULT CheckSortedWithNoDuplicates()
    {
        HRESULT hr = S_OK;
        int     iCount = m_MiniMd.GetCountRecs(m_ixTbl);
        int     nResult;

        m_MiniMd.SetSorted(m_ixTbl, false);

        for (int i = 1; i < iCount; i++)
        {
            IfFailGo(Compare(i, i+1, &nResult));

            if (nResult >= 0)
            {
                return S_OK;
            }
        }

        // The table is sorted until its next change.
        SetSorted();

    ErrExit:
        return hr;
    } // CQuickSortMiniMdRW::CheckSortedWithNoDuplicates

    //*****************************************************************************
    // Override this function to do the comparison.
    //*****************************************************************************
    __checkReturn
    HRESULT Compare(
        int  iLeft,         // First item to compare.
        int  iRight,        // Second item to compare.
        int *pnResult)      // -1, 0, or 1
    {
        HRESULT hr;
        void *pLeft;
        void *pRight;
        IfFailRet(getRow(iLeft, &pLeft));
        IfFailRet(getRow(iRight, &pRight));
        ULONG ulLeft = m_MiniMd.GetCol(m_ixTbl, m_ixCol, pLeft);
        ULONG ulRight = m_MiniMd.GetCol(m_ixTbl, m_ixCol, pRight);

        if (ulLeft < ulRight)
        {
            *pnResult = -1;
            return S_OK;
        }
        if (ulLeft == ulRight)
        {
            *pnResult = 0;
            return S_OK;
        }
        *pnResult = 1;
        return S_OK;
    } // CQuickSortMiniMdRW::Compare

private:
    __checkReturn
    HRESULT SortRange(
        int iLeft,
        int iRight)
    {
        HRESULT hr;
        int     iLast;
        int     nResult;

        while (true)
        {
            // if less than two elements you're done.
            if (iLeft >= iRight)
            {
                return S_OK;
            }

            // The mid-element is the pivot, move it to the left.
            IfFailRet(Compare(iLeft, (iLeft+iRight)/2, &nResult));
            if (nResult != 0)
            {
                IfFailRet(Swap(iLeft, (iLeft+iRight)/2));
            }
            iLast = iLeft;

            // move everything that is smaller than the pivot to the left.
            for (int i = iLeft+1; i <= iRight; i++)
            {
                IfFailRet(Compare(i, iLeft, &nResult));
                if (nResult < 0)
                {
                    IfFailRet(Swap(i, ++iLast));
                }
            }

            // Put the pivot to the point where it is in between smaller and larger elements.
            IfFailRet(Compare(iLeft, iLast, &nResult));
            if (nResult != 0)
            {
                IfFailRet(Swap(iLeft, iLast));
            }

            // Sort each partition.
            int iLeftLast = iLast - 1;
            int iRightFirst = iLast + 1;
            if (iLeftLast - iLeft < iRight - iRightFirst)
            {   // Left partition is smaller, sort it recursively
                IfFailRet(SortRange(iLeft, iLeftLast));
                // Tail call to sort the right (bigger) partition
                iLeft = iRightFirst;
                //iRight = iRight;
                continue;
            }
            else
            {   // Right partition is smaller, sort it recursively
                IfFailRet(SortRange(iRightFirst, iRight));
                // Tail call to sort the left (bigger) partition
                //iLeft = iLeft;
                iRight = iLeftLast;
                continue;
            }
        }
    } // CQuickSortMiniMdRW::SortRange

protected:
    __checkReturn
    inline HRESULT Swap(
        int         iFirst,
        int         iSecond)
    {
        HRESULT hr;
        void *pFirst;
        void *pSecond;
        if (iFirst == iSecond)
        {
            return S_OK;
        }

        PREFAST_ASSUME_MSG(m_iElemSize <= (int) sizeof(m_buf), "The MetaData table row has to fit into buffer for swapping.");

        IfFailRet(getRow(iFirst, &pFirst));
        IfFailRet(getRow(iSecond, &pSecond));
        memcpy(m_buf, pFirst, m_iElemSize);
        memcpy(pFirst, pSecond, m_iElemSize);
        memcpy(pSecond, m_buf, m_iElemSize);
        if (m_pRidMap != NULL)
        {
            RID         ridTemp;
            ridTemp = *(m_pRidMap->Get(iFirst));
            *(m_pRidMap->Get(iFirst)) = *(m_pRidMap->Get(iSecond));
            *(m_pRidMap->Get(iSecond)) = ridTemp;
        }
        return S_OK;
    } // CQuickSortMiniMdRW::Swap

}; // class CQuickSortMiniMdRW

class CStableSortMiniMdRW : public CQuickSortMiniMdRW
{
public:
    CStableSortMiniMdRW(
        CMiniMdRW   &MiniMd,                // MiniMd with the data.
        ULONG       ixTbl,                  // The table.
        ULONG       ixCol,                  // The column.
        bool        bMapToken)              // Is MapToken handling desired.
        :   CQuickSortMiniMdRW(MiniMd, ixTbl, ixCol, bMapToken)
    {}

    //*****************************************************************************
    // Call to sort the array.
    //*****************************************************************************
    __checkReturn
    HRESULT Sort()
    {
        int     i;                      // Outer loop counter.
        int     j;                      // Inner loop counter.
        int     bSwap;                  // Early out.
        HRESULT hr = S_OK;
        int     nResult;

        _ASSERTE(m_MiniMd.IsSortable(m_ixTbl));
        m_iCount = m_MiniMd.GetCountRecs(m_ixTbl);

        // If remap notifications are desired, prepare to collect the info in a RIDMAP.
        IfFailGo(PrepMapTokens());

        for (i=m_iCount; i>1; --i)
        {
            bSwap = 0;
            for (j=1; j<i; ++j)
            {
                IfFailGo(Compare(j, j+1, &nResult));
                if (nResult > 0)
                {
                    IfFailGo(Swap(j, j+1));
                    bSwap = 1;
                }
            }
            // If made a full pass w/o swaps, done.
            if (!bSwap)
                break;
        }

        // The table is sorted until its next change.
        SetSorted();

        // If remap notifications were desired, send them.
        IfFailGo(DoMapTokens());

    ErrExit:
        return hr;
    } // CStableSortMiniMdRW::Sort

}; // class CStableSortMiniMdRW

//-------------------------------------------------------------------------
#define SORTER(tbl,key) CQuickSortMiniMdRW sort##tbl(*this, TBL_##tbl, tbl##Rec::COL_##key, false);
#define SORTER_WITHREMAP(tbl,key) CQuickSortMiniMdRW sort##tbl(*this, TBL_##tbl, tbl##Rec::COL_##key, true);
#define STABLESORTER(tbl,key)   CStableSortMiniMdRW sort##tbl(*this, TBL_##tbl, tbl##Rec::COL_##key, false);
#define STABLESORTER_WITHREMAP(tbl,key)   CStableSortMiniMdRW sort##tbl(*this, TBL_##tbl, tbl##Rec::COL_##key, true);
//-------------------------------------------------------------------------



//********** Code. ************************************************************


//*****************************************************************************
// Ctor / dtor.
//*****************************************************************************
#ifdef _DEBUG
static bool bENCDeltaOnly = false;
#endif
CMiniMdRW::CMiniMdRW()
 :  m_pMemberRefHash(0),
    m_pMemberDefHash(0),
    m_pNamedItemHash(0),
    m_pHandler(0),
    m_cbSaveSize(0),
    m_fIsReadOnly(false),
    m_bPreSaveDone(false),
    m_bPostGSSMod(false),
    m_pMethodMap(0),
    m_pFieldMap(0),
    m_pPropertyMap(0),
    m_pEventMap(0),
    m_pParamMap(0),
    m_pFilterTable(0),
    m_pHostFilter(0),
    m_pTokenRemapManager(0),
    m_fMinimalDelta(FALSE),
    m_rENCRecs(0)
{
#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_EncDelta))
    {
        bENCDeltaOnly = true;
    }
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_MiniMDBreak))
    {
        _ASSERTE(!"CMiniMdRW::CMiniMdRW()");
    }
#endif // _DEBUG

    ZeroMemory(&m_OptionValue, sizeof(OptionValue));

    // initialize the embeded lookuptable struct.  Further initialization, after constructor.
    for (ULONG ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
    {
        m_pVS[ixTbl] = 0;
        m_pLookUpHashs[ixTbl] = 0;
    }

    // Assume that we can sort tables as needed.
    memset(m_bSortable, 1, sizeof(m_bSortable));

    // Initialize the global array of Ptr table indices.
    g_PtrTableIxs[TBL_Field].m_ixtbl = TBL_FieldPtr;
    g_PtrTableIxs[TBL_Field].m_ixcol = FieldPtrRec::COL_Field;
    g_PtrTableIxs[TBL_Method].m_ixtbl = TBL_MethodPtr;
    g_PtrTableIxs[TBL_Method].m_ixcol = MethodPtrRec::COL_Method;
    g_PtrTableIxs[TBL_Param].m_ixtbl = TBL_ParamPtr;
    g_PtrTableIxs[TBL_Param].m_ixcol = ParamPtrRec::COL_Param;
    g_PtrTableIxs[TBL_Property].m_ixtbl = TBL_PropertyPtr;
    g_PtrTableIxs[TBL_Property].m_ixcol = PropertyPtrRec::COL_Property;
    g_PtrTableIxs[TBL_Event].m_ixtbl = TBL_EventPtr;
    g_PtrTableIxs[TBL_Event].m_ixcol = EventPtrRec::COL_Event;

    // AUTO_GROW initialization
    m_maxRid = m_maxIx = 0;
    m_limIx = USHRT_MAX >> 1;
    m_limRid = USHRT_MAX >> AUTO_GROW_CODED_TOKEN_PADDING;
    m_eGrow = eg_ok;
#ifdef _DEBUG
    {
        ULONG iMax, iCdTkn;
        for (iMax=0, iCdTkn=0; iCdTkn<CDTKN_COUNT; ++iCdTkn)
        {
            CCodedTokenDef const *pCTD = &g_CodedTokens[iCdTkn];
            if (pCTD->m_cTokens > iMax)
                iMax = pCTD->m_cTokens;
        }
        // If assert fires, change define for AUTO_GROW_CODED_TOKEN_PADDING.
        _ASSERTE(CMiniMdRW::m_cb[iMax] == AUTO_GROW_CODED_TOKEN_PADDING);
    }
    dbg_m_pLock = NULL;
#endif //_DEBUG

} // CMiniMdRW::CMiniMdRW

CMiniMdRW::~CMiniMdRW()
{
    // Un-initialize the embeded lookuptable struct
    for (ULONG ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
    {
        if (m_pVS[ixTbl])
        {
            m_pVS[ixTbl]->Uninit();
            delete m_pVS[ixTbl];
        }
        if ( m_pLookUpHashs[ixTbl] != NULL )
            delete m_pLookUpHashs[ixTbl];

    }
    if (m_pFilterTable)
        delete m_pFilterTable;

    if (m_rENCRecs)
        delete [] m_rENCRecs;

    if (m_pHandler)
        m_pHandler->Release(), m_pHandler = 0;
    if (m_pHostFilter)
        m_pHostFilter->Release();
    if (m_pMemberRefHash)
        delete m_pMemberRefHash;
    if (m_pMemberDefHash)
        delete m_pMemberDefHash;
    if (m_pNamedItemHash)
        delete m_pNamedItemHash;
    if (m_pMethodMap)
        delete m_pMethodMap;
    if (m_pFieldMap)
        delete m_pFieldMap;
    if (m_pPropertyMap)
        delete m_pPropertyMap;
    if (m_pEventMap)
        delete m_pEventMap;
    if (m_pParamMap)
        delete m_pParamMap;
    if (m_pTokenRemapManager)
        delete m_pTokenRemapManager;
} // CMiniMdRW::~CMiniMdRW


//*****************************************************************************
// return all found CAs in an enumerator
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CommonEnumCustomAttributeByName(
    mdToken        tkObj,               // [IN] Object with Custom Attribute.
    LPCUTF8        szName,              // [IN] Name of desired Custom Attribute.
    bool           fStopAtFirstFind,    // [IN] just find the first one
    HENUMInternal *phEnum)              // enumerator to fill up
{
    HRESULT      hr = S_OK;
    HRESULT      hrRet = S_FALSE;       // Assume that we won't find any
    RID          ridStart, ridEnd;      // Loop start and endpoints.
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_CustomAttribute];

    _ASSERTE(phEnum != NULL);

    HENUMInternal::ZeroEnum(phEnum);

    HENUMInternal::InitDynamicArrayEnum(phEnum);

    phEnum->m_tkKind = mdtCustomAttribute;

    if (pHashTable)
    {
        // table is not sorted and hash is not built so we have to create a dynamic array
        // create the dynamic enumerator.
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        iHash = HashCustomAttribute(tkObj);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(CompareCustomAttribute( tkObj, szName, RidFromToken(p->tok)));
            if (hr == S_OK)
            {
                hrRet = S_OK;

                // If here, found a match.
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(p->tok, mdtCustomAttribute)));
                if (fStopAtFirstFind)
                    goto ErrExit;
            }
        }
    }
    else
    {
        // Get the list of custom values for the parent object.
        if ( IsSorted(TBL_CustomAttribute) )
        {
            IfFailGo(getCustomAttributeForToken(tkObj, &ridEnd, &ridStart));
            // If found none, done.
            if (ridStart == 0)
                goto ErrExit;
        }
        else
        {
            // linear scan of entire table.
            ridStart = 1;
            ridEnd = getCountCustomAttributes() + 1;
        }

        // Look for one with the given name.
        for (; ridStart < ridEnd; ++ridStart)
        {
            IfFailGo(CompareCustomAttribute( tkObj, szName, ridStart));
            if (hr == S_OK)
            {
                // If here, found a match.
                hrRet = S_OK;
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(ridStart, mdtCustomAttribute)));
                if (fStopAtFirstFind)
                    goto ErrExit;
            }
        }
    }

ErrExit:
    if (FAILED(hr))
        return hr;
    return hrRet;
} // CMiniMdRW::CommonEnumCustomAttributeByName



//*****************************************************************************
// return just the blob value of the first found CA matching the query.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,             // [IN] Object with Custom Attribute.
        LPCUTF8            szName,            // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,             // [OUT] put custom attribute token here
        const void       **ppData,            // [OUT] Put pointer to data here.
        ULONG             *pcbData)           // [OUT] Put size of data here.
{
    HRESULT             hr;
    const void         *pData;
    ULONG               cbData;
    HENUMInternal       hEnum;
    mdCustomAttribute   ca;
    CustomAttributeRec *pRec;

    hr = CommonEnumCustomAttributeByName(tkObj, szName, true, &hEnum);
    if (hr != S_OK)
        goto ErrExit;

    if (ppData != NULL || ptkCA != NULL)
    {
        // now get the record out.
        if (ppData == 0)
            ppData = &pData;
        if (pcbData == 0)
            pcbData = &cbData;


        if (HENUMInternal::EnumNext(&hEnum, &ca))
        {
            IfFailGo(GetCustomAttributeRecord(RidFromToken(ca), &pRec));
            IfFailGo(getValueOfCustomAttribute(pRec, reinterpret_cast<const BYTE **>(ppData), pcbData));
            if (ptkCA)
                *ptkCA = ca;
        }
        else
        {
            _ASSERTE(!"Enum returned no items after EnumInit returned S_OK");
            hr = S_FALSE;
        }
    }
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    return hr;
} // CMiniMdRW::CommonGetCustomAttributeByName

//*****************************************************************************
// unmark everything in this module
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::UnmarkAll()
{
    HRESULT      hr = NOERROR;
    ULONG        ulSize = 0;
    ULONG        ixTbl;
    FilterTable *pFilter;

    // find the max rec count with all tables
    for (ixTbl = 0; ixTbl < TBL_COUNT; ++ixTbl)
    {
        if (GetCountRecs(ixTbl) > ulSize)
            ulSize = GetCountRecs(ixTbl);
    }
    IfNullGo(pFilter = GetFilterTable());
    IfFailGo(pFilter->UnmarkAll(this, ulSize));

ErrExit:
    return hr;
} // CMiniMdRW::UnmarkAll


//*****************************************************************************
// mark everything in this module
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::MarkAll()
{
    HRESULT      hr = NOERROR;
    ULONG        ulSize = 0;
    ULONG        ixTbl;
    FilterTable *pFilter;

    // find the max rec count with all tables
    for (ixTbl = 0; ixTbl < TBL_COUNT; ++ixTbl)
    {
        if (GetCountRecs(ixTbl) > ulSize)
            ulSize = GetCountRecs(ixTbl);
    }
    IfNullGo(pFilter = GetFilterTable());
    IfFailGo(pFilter->MarkAll(this, ulSize));

ErrExit:
    return hr;
} // CMiniMdRW::MarkAll

//*****************************************************************************
// This will trigger FilterTable to be created
//*****************************************************************************
FilterTable *CMiniMdRW::GetFilterTable()
{
    if (m_pFilterTable == NULL)
    {
        m_pFilterTable = new (nothrow) FilterTable;
    }
    return m_pFilterTable;
}


//*****************************************************************************
// Calculate the map between TypeRef and TypeDef
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CalculateTypeRefToTypeDefMap()
{
    HRESULT     hr = NOERROR;
    ULONG       index;
    TypeRefRec *pTypeRefRec;
    LPCSTR      szName;
    LPCSTR      szNamespace;
    mdToken     td;
    mdToken     tkResScope;

    PREFIX_ASSUME(GetTypeRefToTypeDefMap() != NULL);

    for (index = 1; index <= m_Schema.m_cRecs[TBL_TypeRef]; index++)
    {
        IfFailRet(GetTypeRefRecord(index, &pTypeRefRec));

        // Get the name and namespace of the TypeRef.
        IfFailRet(getNameOfTypeRef(pTypeRefRec, &szName));
        IfFailRet(getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
        tkResScope = getResolutionScopeOfTypeRef(pTypeRefRec);

        // If the resolutionScope is an AssemblyRef, then the type is
        //  external, even if it has the same name as a type in this scope.
        if (TypeFromToken(tkResScope) == mdtAssemblyRef)
            continue;

        // Iff the name is found in the typedef table, then use
        // that value instead.   Won't be found if typeref is trully external.
        hr = ImportHelper::FindTypeDefByName(this, szNamespace, szName,
            (TypeFromToken(tkResScope) == mdtTypeRef) ? tkResScope : mdTokenNil,
            &td);
        if (hr != S_OK)
        {
            // don't propagate the error in the Find
            hr = NOERROR;
            continue;
        }
        *(GetTypeRefToTypeDefMap()->Get(index)) = td;
    }

    return  hr;
} // CMiniMdRW::CalculateTypeRefToTypeDefMap


//*****************************************************************************
// Set a remap handler.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SetHandler(
    IUnknown *pIUnk)
{
    if (m_pHandler != NULL)
    {
        m_pHandler->Release();
        m_pHandler = NULL;
    }

    if (pIUnk != NULL)
    {
        // ignore the error for QI the IHostFilter
        pIUnk->QueryInterface(IID_IHostFilter, reinterpret_cast<void**>(&m_pHostFilter));

        return pIUnk->QueryInterface(IID_IMapToken, reinterpret_cast<void**>(&m_pHandler));
    }

    return S_OK;
} // CMiniMdRW::SetHandler

//*****************************************************************************
// Set a Options
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SetOption(
    OptionValue *pOptionValue)
{
    HRESULT hr = NOERROR;
    ULONG   ixTbl = 0;
    int     i;

    m_OptionValue = *pOptionValue;

    // Turn off delta metadata bit -- can't be used due to EE assumptions about delta PEs.
    // Inspect ApplyEditAndContinue for details.
    // To enable this, use the EnableDeltaMetadataGeneration/DisableDeltaMetadataGeneration accessors.
    _ASSERTE((m_OptionValue.m_UpdateMode & MDUpdateDelta) != MDUpdateDelta);

#ifdef _DEBUG
    if ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateENC &&
        bENCDeltaOnly)
        m_OptionValue.m_UpdateMode |= MDUpdateDelta;
#endif

    // if a scope is previously updated as incremental, then it should not be open again
    // with full update for read/write.
    //
    if ((m_Schema.m_heaps & CMiniMdSchema::HAS_DELETE) &&
        (m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateFull &&
        !m_fIsReadOnly)
    {
        IfFailGo( CLDB_E_BADUPDATEMODE );
    }

    if ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateIncremental)
        m_Schema.m_heaps |= CMiniMdSchema::HAS_DELETE;

    // Set the value of sortable based on the options.
    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
        // Always sortable.
        for (ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
            m_bSortable[ixTbl] = 1;
        break;
    case MDUpdateENC:
        // Never sortable.
        for (ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
            m_bSortable[ixTbl] = 0;

        // Truncate some tables.
        for (i=0; (ixTbl = m_TruncatedEncTables[i]) != (ULONG) -1; ++i)
        {
            m_Tables[ixTbl].Delete();
            IfFailGo(m_Tables[ixTbl].InitializeEmpty_WithRecordCount(
                m_TableDefs[ixTbl].m_cbRec,
                0
                COMMA_INDEBUG_MD(TRUE)));   // fIsReadWrite
            INDEBUG_MD(m_Tables[ixTbl].Debug_SetTableInfo(NULL, ixTbl));
            m_Schema.m_cRecs[ixTbl] = 0;
        }

        // Out-of-order is expected in an ENC scenario, never an error.
        m_OptionValue.m_ErrorIfEmitOutOfOrder = MDErrorOutOfOrderNone;

        break;
    case MDUpdateIncremental:
        // Sortable if no external token.
        for (ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
            m_bSortable[ixTbl] = (GetTokenForTable(ixTbl) == (ULONG) -1);
        break;
    case MDUpdateExtension:
        // Never sortable.
        for (ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
            m_bSortable[ixTbl] = 0;
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

    // If this is an ENC session, track the generations.
    if (!m_fIsReadOnly && (m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateENC)
    {
#ifdef FEATURE_METADATA_EMIT
        ModuleRec *pMod;
        GUID    encid;

        // Get the module record.
        IfFailGo(GetModuleRecord(1, &pMod));

/*     Do we really want to do this? This would reset the metadata each time we changed an option
        // Copy EncId as BaseId.
        uVal = GetCol(TBL_Module, ModuleRec::COL_EncId, pMod);
        PutCol(TBL_Module, ModuleRec::COL_EncBaseId, pMod, uVal);
*/
        // Allocate a new GUID for EncId.
        IfFailGo(CoCreateGuid(&encid));
        IfFailGo(PutGuid(TBL_Module, ModuleRec::COL_EncId, pMod, encid));
#else //!FEATURE_METADATA_EMIT
        IfFailGo(E_INVALIDARG);
#endif //!FEATURE_METADATA_EMIT
    }

ErrExit:
    return hr;
} // CMiniMdRW::SetOption

//*****************************************************************************
// Get Options
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetOption(
    OptionValue *pOptionValue)
{
    *pOptionValue = m_OptionValue;
    return S_OK;
} // CMiniMdRW::GetOption

//*****************************************************************************
// Smart MapToken.  Only calls client if token really changed.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::MapToken(    // Return value from user callback.
    RID     from,       // Old rid.
    RID     to,         // New rid.
    mdToken tkn)        // Token type.
{
    HRESULT     hr = S_OK;
    TOKENREC   *pTokenRec;
    MDTOKENMAP *pMovementMap;
    // If not change, done.
    if (from == to)
        return S_OK;

    pMovementMap = GetTokenMovementMap();
    _ASSERTE(GetTokenMovementMap() != NULL);
    if (pMovementMap != NULL)
        IfFailRet(pMovementMap->AppendRecord( TokenFromRid(from, tkn), false, TokenFromRid(to, tkn), &pTokenRec ));

    // Notify client.
    if (m_pHandler != NULL)
    {
        LOG((LOGMD, "CMiniMdRW::MapToken (remap): from 0x%08x to 0x%08x\n", TokenFromRid(from,tkn), TokenFromRid(to,tkn)));
        return m_pHandler->Map(TokenFromRid(from,tkn), TokenFromRid(to,tkn));
    }
    else
    {
        return hr;
    }
} // CMiniMdCreate::MapToken

//*****************************************************************************
// Set max, lim, based on data.
//*****************************************************************************
void
CMiniMdRW::ComputeGrowLimits(
    int bSmall)     // large or small tables?
{
    if (bSmall)
    {
        // Tables will need to grow if any value exceeds what a two-byte column can hold.
        m_maxRid = m_maxIx = 0;
        m_limIx = USHRT_MAX >> 1;
        m_limRid = USHRT_MAX >> AUTO_GROW_CODED_TOKEN_PADDING;
        m_eGrow = eg_ok;
    }
    else
    {
        // Tables are already large
        m_maxRid = m_maxIx = UINT32_MAX;
        m_limIx = USHRT_MAX << 1;
        m_limRid = USHRT_MAX << 1;
        m_eGrow = eg_grown;
    }
} // CMiniMdRW::ComputeGrowLimits

//*****************************************************************************
// Initialization of a new writable MiniMd's pools.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::InitPoolOnMem(
    int   iPool,            // The pool to initialize.
    void *pbData,           // The data from which to init.
    ULONG cbData,           // Size of data.
    int   fIsReadOnly)      // Is the memory read-only?
{
    HRESULT hr;

    switch (iPool)
    {
    case MDPoolStrings:
        if (pbData == NULL)
        {   // Creates new empty string heap with default empty string entry
            IfFailRet(m_StringHeap.InitializeEmpty(
                0
                COMMA_INDEBUG_MD(!fIsReadOnly)));
        }
        else
        {
            IfFailRet(m_StringHeap.Initialize(
                MetaData::DataBlob((BYTE *)pbData, cbData),
                !fIsReadOnly));
        }
        break;
    case MDPoolGuids:
        if (pbData == NULL)
        {   // Creates new empty guid heap
            IfFailRet(m_GuidHeap.InitializeEmpty(
                0
                COMMA_INDEBUG_MD(!fIsReadOnly)));
        }
        else
        {
            IfFailRet(m_GuidHeap.Initialize(
                MetaData::DataBlob((BYTE *)pbData, cbData),
                !fIsReadOnly));
        }
        break;
    case MDPoolBlobs:
        if (pbData == NULL)
        {
            if (IsMinimalDelta())
            {   // It's EnC minimal delta, don't include default empty blob
                IfFailRet(m_BlobHeap.InitializeEmpty_WithoutDefaultEmptyBlob(
                    0
                    COMMA_INDEBUG_MD(!fIsReadOnly)));
            }
            else
            {   // Creates new empty blob heap with default empty blob entry
                IfFailRet(m_BlobHeap.InitializeEmpty(
                    0
                    COMMA_INDEBUG_MD(!fIsReadOnly)));
            }
        }
        else
        {
            IfFailRet(m_BlobHeap.Initialize(
                MetaData::DataBlob((BYTE *)pbData, cbData),
                !fIsReadOnly));
        }
        break;
    case MDPoolUSBlobs:
        if (pbData == NULL)
        {
            if (IsMinimalDelta())
            {   // It's EnC minimal delta, don't include default empty user string
                IfFailRet(m_UserStringHeap.InitializeEmpty_WithoutDefaultEmptyBlob(
                    0
                    COMMA_INDEBUG_MD(!fIsReadOnly)));
            }
            else
            {   // Creates new empty user string heap (with default empty !!!blob!!! entry)
                // Note: backaward compatiblity: doesn't add default empty user string, but default empty
                // blob entry
                IfFailRet(m_UserStringHeap.InitializeEmpty(
                    0
                    COMMA_INDEBUG_MD(!fIsReadOnly)));
            }
        }
        else
        {
            IfFailRet(m_UserStringHeap.Initialize(
                MetaData::DataBlob((BYTE *)pbData, cbData),
                !fIsReadOnly));
        }
        break;
    default:
        hr = E_INVALIDARG;
    }
    return hr;
} // CMiniMdRW::InitPoolOnMem

//*****************************************************************************
// Initialization of a new writable MiniMd
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::InitOnMem(
    const void *pvBuf,          // The data from which to init.
    ULONG       ulBufLen,       // The data size
    int         fIsReadOnly)    // Is the memory read-only?
{
    HRESULT  hr = S_OK;
    UINT32   cbSchemaSize;      // Size of the schema structure.
    S_UINT32 cbTotalSize;       // Size of all data used.
    BYTE    *pBuf = const_cast<BYTE*>(reinterpret_cast<const BYTE*>(pvBuf));
    int      i;

    // post contruction initialize the embeded lookuptable struct
    for (ULONG ixTbl = 0; ixTbl < m_TblCount; ++ixTbl)
    {
        if (m_TableDefs[ixTbl].m_iKey < m_TableDefs[ixTbl].m_cCols)
        {
            if (m_pVS[ixTbl] == NULL)
            {
                m_pVS[ixTbl] = new (nothrow) VirtualSort;
                IfNullGo(m_pVS[ixTbl]);

                m_pVS[ixTbl]->Init(ixTbl, m_TableDefs[ixTbl].m_iKey, this);
            }
        }
    }

    // Uncompress the schema from the buffer into our structures.
    IfFailGo(SchemaPopulate(pvBuf, ulBufLen, (ULONG *)&cbSchemaSize));

    if (m_fMinimalDelta)
        IfFailGo(InitWithLargeTables());

    // Initialize the pointers to the rest of the data.
    pBuf += cbSchemaSize;
    cbTotalSize = S_UINT32(cbSchemaSize);
    for (i=0; i<(int)m_TblCount; ++i)
    {
        if (m_Schema.m_cRecs[i] > 0)
        {
            // Size of one table is rowsize * rowcount.
            S_UINT32 cbTableSize =
                S_UINT32(m_TableDefs[i].m_cbRec) *
                S_UINT32(m_Schema.m_cRecs[i]);
            if (cbTableSize.IsOverflow())
            {
                Debug_ReportError("Table is too big, its size overflows.");
                IfFailGo(METADATA_E_INVALID_FORMAT);
            }
            cbTotalSize += cbTableSize;
            if (cbTotalSize.IsOverflow())
            {
                Debug_ReportError("Total tables size is too big, their total size overflows.");
                IfFailGo(METADATA_E_INVALID_FORMAT);
            }
            IfFailGo(m_Tables[i].Initialize(
                m_TableDefs[i].m_cbRec,
                MetaData::DataBlob(pBuf, cbTableSize.Value()),
                !fIsReadOnly));         // fCopyData
            INDEBUG_MD(m_Tables[i].Debug_SetTableInfo(NULL, i));
            pBuf += cbTableSize.Value();
        }
        else
        {
            IfFailGo(m_Tables[i].InitializeEmpty_WithRecordCount(
                m_TableDefs[i].m_cbRec,
                0
                COMMA_INDEBUG_MD(!fIsReadOnly)));
            INDEBUG_MD(m_Tables[i].Debug_SetTableInfo(NULL, i));
        }
    }

    // If the metadata is being opened for read/write, all the updateable columns
    //  need to be the same width.
    if (!fIsReadOnly)
    {
        // variable to indicate if tables are large, small or mixed.
        int         fMixed = false;
        int         iSize = 0;
        CMiniColDef *pCols;                 // The col defs to init.
        int         iCol;

        // Look at all the tables, or until mixed sizes are discovered.
        for (i=0; i<(int)m_TblCount && !fMixed; i++)
        {   // Look at all the columns of the table.
            pCols = m_TableDefs[i].m_pColDefs;
            for (iCol = 0; iCol < m_TableDefs[i].m_cCols && !fMixed; iCol++)
            {   // If not a fixed size column...
                if (!IsFixedType(m_TableDefs[i].m_pColDefs[iCol].m_Type))
                {   // If this is the first non-fixed size column...
                    if (iSize == 0)
                    {   // remember its size.
                        iSize = m_TableDefs[i].m_pColDefs[iCol].m_cbColumn;
                    }
                    else if (iSize != m_TableDefs[i].m_pColDefs[iCol].m_cbColumn)
                    {   // Not first non-fixed size, so if a different size
                        //  the table has mixed column sizes.
                        fMixed = true;
                    }
                }
            }
        }
        if (fMixed)
        {
            // grow everything to large
            IfFailGo(ExpandTables());
            ComputeGrowLimits(FALSE /* ! small*/);
        }
        else if (iSize == 2)
        {
            // small schema
            ComputeGrowLimits(TRUE /* small */);
        }
        else
        {
            // large schema
            ComputeGrowLimits(FALSE /* ! small */);
        }
    }
    else
    {
        // Set the limits so we will know when to grow the database.
        ComputeGrowLimits(TRUE /* small */);
    }

    // Track records that this MD started with.
    m_StartupSchema = m_Schema;

    m_fIsReadOnly = fIsReadOnly ? 1 : 0;

ErrExit:
    return hr;
} // CMiniMdRW::InitOnMem

//*****************************************************************************
// Validate cross-stream consistency.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PostInit(
    int iLevel)
{
    return S_OK;
} // CMiniMdRW::PostInit

//*****************************************************************************
// Init a CMiniMdRW from the data in a CMiniMd [RO].
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::InitOnRO(
    CMiniMd *pMd,           // The MiniMd to update from.
    int      fIsReadOnly)   // Will updates be allowed?
{
    HRESULT hr = S_OK;
    ULONG   i;          // Loop control.

    // Init the schema.
    IfFailGo(SchemaPopulate(*pMd));

    // Allocate VS structs for tables with key columns.
    for (ULONG ixTbl = 0; ixTbl < m_TblCount; ++ixTbl)
    {
        if (m_TableDefs[ixTbl].m_iKey < m_TableDefs[ixTbl].m_cCols)
        {
            m_pVS[ixTbl] = new (nothrow) VirtualSort;
            IfNullGo(m_pVS[ixTbl]);

            m_pVS[ixTbl]->Init(ixTbl, m_TableDefs[ixTbl].m_iKey, this);
        }
    }

    // Copy over the column definitions.
    for (i = 0; i < m_TblCount; ++i)
    {
        _ASSERTE(m_TableDefs[i].m_cCols == pMd->m_TableDefs[i].m_cCols);
        m_TableDefs[i].m_cbRec = pMd->m_TableDefs[i].m_cbRec;
        IfFailGo(SetNewColumnDefinition(&(m_TableDefs[i]), pMd->m_TableDefs[i].m_pColDefs, i));
    }

    // Initialize string heap
    if (pMd->m_StringHeap.GetUnalignedSize() > 0)
    {
        IfFailGo(m_StringHeap.InitializeFromStringHeap(
            &(pMd->m_StringHeap),
            !fIsReadOnly));
    }
    else
    {
        IfFailGo(m_StringHeap.InitializeEmpty(
            0
            COMMA_INDEBUG_MD(!fIsReadOnly)));
    }

    // Initialize user string heap
    if (pMd->m_UserStringHeap.GetUnalignedSize() > 0)
    {
        IfFailGo(m_UserStringHeap.InitializeFromBlobHeap(
            &(pMd->m_UserStringHeap),
            !fIsReadOnly));
    }
    else
    {
        IfFailGo(m_UserStringHeap.InitializeEmpty(
            0
            COMMA_INDEBUG_MD(!fIsReadOnly)));
    }

    // Initialize guid heap
    if (pMd->m_GuidHeap.GetSize() >  0)
    {
        IfFailGo(m_GuidHeap.InitializeFromGuidHeap(
            &(pMd->m_GuidHeap),
            !fIsReadOnly));
    }
    else
    {
        IfFailGo(m_GuidHeap.InitializeEmpty(
            0
            COMMA_INDEBUG_MD(!fIsReadOnly)));
    }

    // Initialize blob heap
    if (pMd->m_BlobHeap.GetUnalignedSize() > 0)
    {
        IfFailGo(m_BlobHeap.InitializeFromBlobHeap(
            &(pMd->m_BlobHeap),
            !fIsReadOnly));
    }
    else
    {
        IfFailGo(m_BlobHeap.InitializeEmpty(
            0
            COMMA_INDEBUG_MD(!fIsReadOnly)));
    }

    // Init the record pools
    for (i = 0; i < m_TblCount; ++i)
    {
        if (m_Schema.m_cRecs[i] > 0)
        {
            IfFailGo(m_Tables[i].InitializeFromTable(
                &(pMd->m_Tables[i]),
                m_TableDefs[i].m_cbRec,
                m_Schema.m_cRecs[i],
                !fIsReadOnly));     // fCopyData
            INDEBUG_MD(m_Tables[i].Debug_SetTableInfo(NULL, i));

            // We set this bit to indicate the compressed, read-only tables are always sorted
            // <TODO>This is not true for all tables, so we should set it correctly and flush out resulting bugs</TODO>
            SetSorted(i, true);
        }
        else
        {
            IfFailGo(m_Tables[i].InitializeEmpty_WithRecordCount(
                m_TableDefs[i].m_cbRec,
                2
                COMMA_INDEBUG_MD(!fIsReadOnly)));
            INDEBUG_MD(m_Tables[i].Debug_SetTableInfo(NULL, i));
            // An empty table can be considered unsorted.
            SetSorted(i, false);
        }
    }

    // Set the limits so we will know when to grow the database.
    ComputeGrowLimits(TRUE /* small */);

    // Track records that this MD started with.
    m_StartupSchema = m_Schema;

    m_fIsReadOnly = fIsReadOnly ? 1 : 0;

ErrExit:
    return hr;
} // CMiniMdRW::InitOnRO

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE

// This checks that column sizes are reasonable for their types
// The sizes could still be too small to hold all values in the range, or larger
// than they needed, but there must exist some scenario where this size is the
// one we would use.
// As long as this validation passes + we verify that the records actually
// have space for columns of this size then the worst thing that malicious
// data could do is be slightly inneficient, or be unable to address all their data
HRESULT _ValidateColumnSize(BYTE trustedColumnType, BYTE untrustedColumnSize)
{
    // Is the field a RID into a table?
    if (trustedColumnType <= iCodedTokenMax)
    {
        if (untrustedColumnSize != sizeof(USHORT) && untrustedColumnSize != sizeof(ULONG))
            return CLDB_E_FILE_CORRUPT;
    }
    else
    {   // Fixed type.
        switch (trustedColumnType)
        {
        case iBYTE:
            if (untrustedColumnSize != 1)
                return CLDB_E_FILE_CORRUPT;
            break;
        case iSHORT:
        case iUSHORT:
            if (untrustedColumnSize != 2)
                return CLDB_E_FILE_CORRUPT;
            break;
        case iLONG:
        case iULONG:
            if (untrustedColumnSize != 4)
                return CLDB_E_FILE_CORRUPT;
            break;
        case iSTRING:
        case iGUID:
        case iBLOB:
            if (untrustedColumnSize != 2 && untrustedColumnSize != 4)
                return CLDB_E_FILE_CORRUPT;
            break;
        default:
            _ASSERTE(!"Unexpected schema type");
            return CLDB_E_FILE_CORRUPT;
        }
    }
    return S_OK;
}

__checkReturn
HRESULT CMiniMdRW::InitOnCustomDataSource(IMDCustomDataSource* pDataSource)
{
    HRESULT hr = S_OK;
    ULONG   i;          // Loop control.
    ULONG   key;
    BOOL fIsReadOnly = TRUE;
    MetaData::DataBlob stringPoolData;
    MetaData::DataBlob userStringPoolData;
    MetaData::DataBlob guidHeapData;
    MetaData::DataBlob blobHeapData;
    MetaData::DataBlob tableRecordData;
    CMiniTableDef tableDef;
    BOOL sortable = FALSE;


    // the data source owns all the memory backing the storage pools, so we need to ensure it stays alive
    // after this method returns. When the CMiniMdRW is destroyed the reference will be released.
    pDataSource->AddRef();
    m_pCustomDataSource = pDataSource;

    // Copy over the schema.
    IfFailGo(pDataSource->GetSchema(&m_Schema));

    // Is this the "native" version of the metadata for this runtime?
    if ((m_Schema.m_major != METAMODEL_MAJOR_VER) || (m_Schema.m_minor != METAMODEL_MINOR_VER))
    {
        // We don't support this version of the metadata
        Debug_ReportError("Unsupported version of MetaData.");
        return PostError(CLDB_E_FILE_OLDVER, m_Schema.m_major, m_Schema.m_minor);
    }

    // How big are the various pool inidices?
    m_iStringsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_STRING_4) ? 0xffffffff : 0xffff;
    m_iGuidsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_GUID_4) ? 0xffffffff : 0xffff;
    m_iBlobsMask = (m_Schema.m_heaps & CMiniMdSchema::HEAP_BLOB_4) ? 0xffffffff : 0xffff;

    // Copy over TableDefs, column definitions and allocate VS structs for tables with key columns.
    for (ULONG ixTbl = 0; ixTbl < m_TblCount; ++ixTbl)
    {
        IfFailGo(pDataSource->GetTableDef(ixTbl, &tableDef));
        const CMiniTableDef* pTemplate = GetTableDefTemplate(ixTbl);

        // validate that the table def looks safe
        // we only allow some very limited differences between the standard template and the data source
        key = (pTemplate->m_iKey < pTemplate->m_cCols) ? pTemplate->m_iKey : 0xFF;
        if (key != tableDef.m_iKey) { IfFailGo(CLDB_E_FILE_CORRUPT); }
        if (pTemplate->m_cCols != tableDef.m_cCols) { IfFailGo(CLDB_E_FILE_CORRUPT); }
        ULONG cbRec = 0;
        for (ULONG i = 0; i < pTemplate->m_cCols; i++)
        {
            if (tableDef.m_pColDefs == NULL) { IfFailGo(CLDB_E_FILE_CORRUPT); }
            if (pTemplate->m_pColDefs[i].m_Type != tableDef.m_pColDefs[i].m_Type) { IfFailGo(CLDB_E_FILE_CORRUPT); }
            IfFailGo(_ValidateColumnSize(pTemplate->m_pColDefs[i].m_Type, tableDef.m_pColDefs[i].m_cbColumn));
            // sometimes, but not always, it seems like columns get alignment padding
            // we'll allow it if we see it
            if (cbRec > tableDef.m_pColDefs[i].m_oColumn)  { IfFailGo(CLDB_E_FILE_CORRUPT); }
            if (tableDef.m_pColDefs[i].m_oColumn > AlignUp(cbRec, tableDef.m_pColDefs[i].m_cbColumn))  { IfFailGo(CLDB_E_FILE_CORRUPT); }
            cbRec = tableDef.m_pColDefs[i].m_oColumn + tableDef.m_pColDefs[i].m_cbColumn;
        }
        if (tableDef.m_cbRec != cbRec) { IfFailGo(CLDB_E_FILE_CORRUPT); }

        // tabledef passed validation, copy it in
        m_TableDefs[ixTbl].m_iKey = tableDef.m_iKey;
        m_TableDefs[ixTbl].m_cCols = tableDef.m_cCols;
        m_TableDefs[ixTbl].m_cbRec = tableDef.m_cbRec;
        IfFailGo(SetNewColumnDefinition(&(m_TableDefs[ixTbl]), tableDef.m_pColDefs, ixTbl));
        if (m_TableDefs[ixTbl].m_iKey < m_TableDefs[ixTbl].m_cCols)
        {
            m_pVS[ixTbl] = new (nothrow)VirtualSort;
            IfNullGo(m_pVS[ixTbl]);

            m_pVS[ixTbl]->Init(ixTbl, m_TableDefs[ixTbl].m_iKey, this);
        }
    }

    // Initialize string heap
    IfFailGo(pDataSource->GetStringHeap(&stringPoolData));
    m_StringHeap.Initialize(stringPoolData, !fIsReadOnly);

    // Initialize user string heap
    IfFailGo(pDataSource->GetUserStringHeap(&userStringPoolData));
    m_UserStringHeap.Initialize(userStringPoolData, !fIsReadOnly);

    // Initialize guid heap
    IfFailGo(pDataSource->GetGuidHeap(&guidHeapData));
    m_GuidHeap.Initialize(guidHeapData, !fIsReadOnly);

    // Initialize blob heap
    IfFailGo(pDataSource->GetBlobHeap(&blobHeapData));
    m_BlobHeap.Initialize(blobHeapData, !fIsReadOnly);

    // Init the record pools
    for (i = 0; i < m_TblCount; ++i)
    {
        IfFailGo(pDataSource->GetTableRecords(i, &tableRecordData));
        // sanity check record counts and table sizes, this also ensures that cbRec*m_cRecs[x] doesn't overflow
        if (m_Schema.m_cRecs[i] > 1000000) { IfFailGo(CLDB_E_FILE_CORRUPT); }
        if (tableRecordData.GetSize() < m_TableDefs[i].m_cbRec * m_Schema.m_cRecs[i]) { IfFailGo(CLDB_E_FILE_CORRUPT); }
        m_Tables[i].Initialize(m_TableDefs[i].m_cbRec, tableRecordData, !fIsReadOnly);

        IfFailGo(pDataSource->GetTableSortable(i, &sortable));
        m_bSortable[i] = !!sortable ? 1 : 0;
    }

    // Set the limits so we will know when to grow the database.
    ComputeGrowLimits(TRUE /* small */);

    // Track records that this MD started with.
    m_StartupSchema = m_Schema;

    m_fIsReadOnly = fIsReadOnly;

ErrExit:
    return hr;
}
#endif

//*****************************************************************************
// Convert a read-only to read-write.  Copies data.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ConvertToRW()
{
    HRESULT hr = S_OK;
    int     i;          // Loop control.

    // Check for already done.
    if (!m_fIsReadOnly)
        return hr;

    // If this is a minimal delta, then we won't allow it to be RW
    if (IsMinimalDelta())
        return CLDB_E_INCOMPATIBLE;

    IfFailGo(m_StringHeap.MakeWritable());
    IfFailGo(m_GuidHeap.MakeWritable());
    IfFailGo(m_UserStringHeap.MakeWritable());
    IfFailGo(m_BlobHeap.MakeWritable());

    // Init the record pools
    for (i = 0; i < (int)m_TblCount; ++i)
    {
        IfFailGo(m_Tables[i].MakeWritable());
    }

    // Grow the tables.
    IfFailGo(ExpandTables());

    // Track records that this MD started with.
    m_StartupSchema = m_Schema;

    // No longer read-only.
    m_fIsReadOnly = false;

ErrExit:
    return hr;
} // CMiniMdRW::ConvertToRW

//*****************************************************************************
// Initialization of a new writable MiniMd
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::InitNew()
{
    HRESULT hr = S_OK;
    int     i;                  // Loop control.

    // Initialize the Schema.
    IfFailGo(m_Schema.InitNew(m_OptionValue.m_MetadataVersion));

    // Allocate VS structs for tables with key columns.
    for (ULONG ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {
        if (m_TableDefs[ixTbl].m_iKey < m_TableDefs[ixTbl].m_cCols)
        {
            m_pVS[ixTbl] = new (nothrow) VirtualSort;
            IfNullGo(m_pVS[ixTbl]);

            m_pVS[ixTbl]->Init(ixTbl, m_TableDefs[ixTbl].m_iKey, this);
        }
    }

    enum MetaDataSizeIndex sizeIndex;
    sizeIndex = GetMetaDataSizeIndex(&m_OptionValue);
    if ((sizeIndex == MDSizeIndex_Standard) || (sizeIndex == MDSizeIndex_Minimal))
    {
        // OutputDebugStringA("Default small tables enabled\n");
        // How big are the various pool inidices?
        m_Schema.m_heaps = 0;
        // How many rows in various tables?
        for (i = 0; i < (int)m_TblCount; ++i)
        {
            m_Schema.m_cRecs[i] = 0;
        }

        // Compute how many bits required to hold.
        m_Schema.m_rid = 1;
        m_maxRid = m_maxIx = 0;
        m_limIx = USHRT_MAX >> 1;
        m_limRid = USHRT_MAX >> AUTO_GROW_CODED_TOKEN_PADDING;
        m_eGrow = eg_ok;
    }

    // Now call base class function to calculate the offsets, sizes.
    IfFailGo(SchemaPopulate2(NULL));

    // Initialize the record heaps.
    for (i = 0; i < (int)m_TblCount; ++i)
    {   // Don't really have any records yet.
        m_Schema.m_cRecs[i] = 0;
        IfFailGo(m_Tables[i].InitializeEmpty_WithRecordCount(
            m_TableDefs[i].m_cbRec,
            g_TblSizeInfo[sizeIndex][i]
            COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite
        INDEBUG_MD(m_Tables[i].Debug_SetTableInfo(NULL, i));

        // Create tables as un-sorted.  We hope to add all records, then sort just once.
        SetSorted(i, false);
    }

    // Initialize heaps
    IfFailGo(m_StringHeap.InitializeEmpty_WithItemsCount(
        g_PoolSizeInfo[sizeIndex][IX_STRING_POOL][0],
        g_PoolSizeInfo[sizeIndex][IX_STRING_POOL][1]
        COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite
    IfFailGo(m_BlobHeap.InitializeEmpty_WithItemsCount(
        g_PoolSizeInfo[sizeIndex][IX_BLOB_POOL][0],
        g_PoolSizeInfo[sizeIndex][IX_BLOB_POOL][1]
        COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite
    IfFailGo(m_UserStringHeap.InitializeEmpty_WithItemsCount(
        g_PoolSizeInfo[sizeIndex][IX_US_BLOB_POOL][0],
        g_PoolSizeInfo[sizeIndex][IX_US_BLOB_POOL][1]
        COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite
    IfFailGo(m_GuidHeap.InitializeEmpty_WithItemsCount(
        g_PoolSizeInfo[sizeIndex][IX_GUID_POOL][0],
        g_PoolSizeInfo[sizeIndex][IX_GUID_POOL][1]
        COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite

    // Track records that this MD started with.
    m_StartupSchema = m_Schema;

    // New db is never read-only.
    m_fIsReadOnly = false;

ErrExit:
    return hr;
} // CMiniMdRW::InitNew

//*****************************************************************************
// Determine how big the tables would be when saved.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetFullSaveSize(
    CorSaveSize               fSave,                // [IN] cssAccurate or cssQuick.
    UINT32                   *pcbSaveSize,          // [OUT] Put the size here.
    DWORD                    *pbSaveCompressed,     // [OUT] Will the saved data be fully compressed?
    MetaDataReorderingOptions reorderingOptions)    // [IN] Metadata reordering options
{
    HRESULT     hr = S_OK;
    CMiniTableDef   sTempTable;         // Definition for a temporary table.
    CQuickArray<CMiniColDef> rTempCols; // Definition for a temp table's columns.
    BYTE        SchemaBuf[sizeof(CMiniMdSchema)];   //Buffer for compressed schema.
    ULONG       cbAlign;                // Bytes needed for alignment.
    UINT32      cbTable;                // Bytes in a table.
    UINT32      cbTotal;                // Bytes written.
    int         i;                      // Loop control.

    _ASSERTE(m_bPreSaveDone);

    // Determine if the stream is "fully compressed", ie no pointer tables.
    *pbSaveCompressed = true;
    for (i=0; i<(int)m_TblCount; ++i)
    {
        if (HasIndirectTable(i))
        {
            *pbSaveCompressed = false;
            break;
        }
    }

    // Build the header.
    CMiniMdSchema Schema = m_Schema;
    IfFailGo(m_StringHeap.GetAlignedSize(&cbTable));
    if (cbTable > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_STRING_4;
    }

    IfFailGo(m_BlobHeap.GetAlignedSize(&cbTable));
    if (cbTable > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_BLOB_4;
    }

    if (m_GuidHeap.GetSize() > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_GUID_4;
    }

    cbTotal = Schema.SaveTo(SchemaBuf);
    if ( (cbAlign = Align4(cbTotal) - cbTotal) != 0)
        cbTotal += cbAlign;

    // For each table...
    ULONG ixTbl;
    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {
        if (GetCountRecs(ixTbl))
        {
            // Determine how big the compressed table will be.

            // Allocate a def for the temporary table.
            sTempTable = m_TableDefs[ixTbl];
            if (m_eGrow == eg_grown)
            {
                IfFailGo(rTempCols.ReSizeNoThrow(sTempTable.m_cCols));
                sTempTable.m_pColDefs = rTempCols.Ptr();

                // Initialize temp table col defs based on actual counts of data in the
                //  real tables.
                IfFailGo(InitColsForTable(Schema, ixTbl, &sTempTable, 1, FALSE));
            }

            cbTable = sTempTable.m_cbRec * GetCountRecs(ixTbl);

            cbTotal += cbTable;
        }
    }

    // Pad with at least 2 bytes and align on 4 bytes.
    cbAlign = Align4(cbTotal) - cbTotal;
    if (cbAlign < 2)
        cbAlign += 4;
    cbTotal += cbAlign;
    m_cbSaveSize = cbTotal;

    LOG((LOGMD, "CMiniMdRW::GetFullSaveSize: Total size = %d\n", cbTotal));

    *pcbSaveSize = cbTotal;

ErrExit:
    return hr;
} // CMiniMdRW::GetFullSaveSize

//*****************************************************************************
// GetSaveSize for saving just the delta (ENC) data.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetENCSaveSize(          // S_OK or error.
    UINT32 *pcbSaveSize)           // [OUT] Put the size here.
{
    HRESULT hr = S_OK;
    BYTE    SchemaBuf[sizeof(CMiniMdSchema)];   //Buffer for compressed schema.
    ULONG   cbAlign;                // Bytes needed for alignment.
    UINT32  cbTable;                // Bytes in a table.
    UINT32  cbTotal;                // Bytes written.
    ULONG   ixTbl;                  // Loop control.

    // If not saving deltas, defer to full GetSaveSize.
    if ((m_OptionValue.m_UpdateMode & MDUpdateDelta) != MDUpdateDelta)
    {
        DWORD bCompressed;
        return GetFullSaveSize(cssAccurate, pcbSaveSize, &bCompressed);
    }

    // Make sure the minimal deltas have expanded tables
    IfFailRet(ExpandTables());

    // Build the header.
    CMiniMdSchema Schema = m_Schema;

    if (m_rENCRecs != NULL)
    {
        for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            Schema.m_cRecs[ixTbl] = m_rENCRecs[ixTbl].Count();
    }
    else
    {
        for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            Schema.m_cRecs[ixTbl] = 0;
    }

    Schema.m_cRecs[TBL_Module] = m_Schema.m_cRecs[TBL_Module];
    Schema.m_cRecs[TBL_ENCLog] = m_Schema.m_cRecs[TBL_ENCLog];
    Schema.m_cRecs[TBL_ENCMap] = m_Schema.m_cRecs[TBL_ENCMap];

    cbTotal = Schema.SaveTo(SchemaBuf);
    if ( (cbAlign = Align4(cbTotal) - cbTotal) != 0)
        cbTotal += cbAlign;

    // Accumulate size of each table...
    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {   // ENC tables are special.
        if (ixTbl == TBL_ENCLog || ixTbl == TBL_ENCMap || ixTbl == TBL_Module)
            cbTable = m_Schema.m_cRecs[ixTbl] * m_TableDefs[ixTbl].m_cbRec;
        else
            cbTable = Schema.m_cRecs[ixTbl] * m_TableDefs[ixTbl].m_cbRec;
        cbTotal += cbTable;
    }

    // Pad with at least 2 bytes and align on 4 bytes.
    cbAlign = Align4(cbTotal) - cbTotal;
    if (cbAlign < 2)
        cbAlign += 4;
    cbTotal += cbAlign;

    *pcbSaveSize = cbTotal;
    m_cbSaveSize = cbTotal;

//ErrExit:
    return hr;
} // CMiniMdRW::GetENCSaveSize


//*****************************************************************************
// Determine how big the tables would be when saved.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetSaveSize(
    CorSaveSize               fSave,                // [IN] cssAccurate or cssQuick.
    UINT32                   *pcbSaveSize,          // [OUT] Put the size here.
    DWORD                    *pbSaveCompressed,     // [OUT] Will the saved data be fully compressed?
    MetaDataReorderingOptions reorderingOptions)    // [IN] Optional metadata reordering options
{
    HRESULT hr;

    // Prepare the data for save.
    IfFailGo(PreSave());

    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
        hr = GetFullSaveSize(fSave, pcbSaveSize, pbSaveCompressed, reorderingOptions);
        break;
    case MDUpdateIncremental:
    case MDUpdateExtension:
    case MDUpdateENC:
        hr = GetFullSaveSize(fSave, pcbSaveSize, pbSaveCompressed, NoReordering);
        // never save compressed if it is incremental compilation.
        *pbSaveCompressed = false;
        break;
    case MDUpdateDelta:
        *pbSaveCompressed = false;
        hr = GetENCSaveSize(pcbSaveSize);
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

ErrExit:
    return hr;
} // CMiniMdRW::GetSaveSize

//*****************************************************************************
// Determine how big a pool would be when saved full size.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetFullPoolSaveSize( // S_OK or error.
    int     iPool,          // The pool of interest.
    UINT32 *pcbSaveSize)    // [OUT] Put the size here.
{
    HRESULT hr;

    switch (iPool)
    {
    case MDPoolStrings:
        hr = m_StringHeap.GetAlignedSize(pcbSaveSize);
        break;
    case MDPoolGuids:
        *pcbSaveSize = m_GuidHeap.GetSize();
        hr = S_OK;
        break;
    case MDPoolBlobs:
        hr = m_BlobHeap.GetAlignedSize(pcbSaveSize);
        break;
    case MDPoolUSBlobs:
        hr = m_UserStringHeap.GetAlignedSize(pcbSaveSize);
        break;
    default:
        hr = E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::GetFullPoolSaveSize

//*****************************************************************************
// Determine how big a pool would be when saved ENC size.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetENCPoolSaveSize(
    int     iPool,                  // The pool of interest.
    UINT32 *pcbSaveSize)           // [OUT] Put the size here.
{
    HRESULT hr;

    switch (iPool)
    {
    case MDPoolStrings:
        IfFailRet(m_StringHeap.GetEnCSessionAddedHeapSize_Aligned(pcbSaveSize));
        hr = S_OK;
        break;
    case MDPoolGuids:
        // We never save delta guid heap, we save full guid heap everytime
        *pcbSaveSize = m_GuidHeap.GetSize();
        hr = S_OK;
        break;
    case MDPoolBlobs:
        IfFailRet(m_BlobHeap.GetEnCSessionAddedHeapSize_Aligned(pcbSaveSize));
        hr = S_OK;
        break;
    case MDPoolUSBlobs:
        IfFailRet(m_UserStringHeap.GetEnCSessionAddedHeapSize_Aligned(pcbSaveSize));
        hr = S_OK;
        break;
    default:
        hr = E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::GetENCPoolSaveSize

//*****************************************************************************
// Determine how big a pool would be when saved.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetPoolSaveSize(
    int     iPool,          // The pool of interest.
    UINT32 *pcbSaveSize)    // [OUT] Put the size here.
{
    HRESULT hr;

    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
    case MDUpdateIncremental:
    case MDUpdateExtension:
    case MDUpdateENC:
        hr = GetFullPoolSaveSize(iPool, pcbSaveSize);
        break;
    case MDUpdateDelta:
        hr = GetENCPoolSaveSize(iPool, pcbSaveSize);
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::GetPoolSaveSize

//*****************************************************************************
// Is the given pool empty?
//*****************************************************************************
int CMiniMdRW::IsPoolEmpty(             // True or false.
    int         iPool)                  // The pool of interest.
{
    switch (iPool)
    {
    case MDPoolStrings:
        return m_StringHeap.IsEmpty();
    case MDPoolGuids:
        return m_GuidHeap.IsEmpty();
    case MDPoolBlobs:
        return m_BlobHeap.IsEmpty();
    case MDPoolUSBlobs:
        return m_UserStringHeap.IsEmpty();
    }
    return true;
} // CMiniMdRW::IsPoolEmpty

// --------------------------------------------------------------------------------------
//
// Gets user string (*Data) at index (nIndex) and fills the index (*pnNextIndex) of the next user string
// in the heap.
// Returns S_OK and fills the string (*pData) and the next index (*pnNextIndex).
// Returns S_FALSE if the index (nIndex) is not valid user string index.
// Returns error code otherwise.
// Clears *pData and sets *pnNextIndex to 0 on error or S_FALSE.
//
__checkReturn
HRESULT
CMiniMdRW::GetUserStringAndNextIndex(
    UINT32              nIndex,
    MetaData::DataBlob *pData,
    UINT32             *pnNextIndex)
{
    HRESULT hr = S_OK;
    MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    // First check that the index is valid to avoid debug error reporting
    // If this turns out to be slow, then we can add a new API to BlobHeap "GetBlobWithSizePrefix_DontFail"
    // to merge this check with following GetBlobWithSizePrefix call
    if (!m_UserStringHeap.IsValidIndex(nIndex))
    {
        return S_FALSE;
    }

    // Get user string at index nIndex (verifies that the user string is in the heap)
    IfFailGo(m_UserStringHeap.GetBlobWithSizePrefix(
        nIndex,
        pData));
    _ASSERTE(hr == S_OK);

    // Get index behind the user string - doesn't overflow, because the user string is in the heap
    *pnNextIndex = nIndex + pData->GetSize();

    UINT32 cbUserStringSize_Ignore;
    if (!pData->GetCompressedU(&cbUserStringSize_Ignore))
    {
        Debug_ReportInternalError("There's a bug, because previous call to GetBlobWithSizePrefix succeeded.");
        IfFailGo(METADATA_E_INTERNAL_ERROR);
    }
    return S_OK;

ErrExit:
    // Fill output parameters on error
    *pnNextIndex = 0;
    pData->Clear();

    return hr;
} // CMiniMdRW::GetUserStringAndNextIndex

//*****************************************************************************
// Initialized TokenRemapManager
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::InitTokenRemapManager()
{
    HRESULT hr = NOERROR;

    if (m_pTokenRemapManager == NULL)
    {
        // allocate TokenRemapManager
        m_pTokenRemapManager = new (nothrow) TokenRemapManager;
        IfNullGo(m_pTokenRemapManager);
    }

    // initialize the ref to def optimization map
    IfFailGo( m_pTokenRemapManager->ClearAndEnsureCapacity(m_Schema.m_cRecs[TBL_TypeRef], m_Schema.m_cRecs[TBL_MemberRef]));

ErrExit:
    return hr;
} // CMiniMdRW::InitTokenRemapManager

//*****************************************************************************
// Debug code to check whether a table's objects can have custom attributes
//  attached.
//*****************************************************************************
#ifdef _DEBUG
bool CMiniMdRW::CanHaveCustomAttribute( // Can a given table have a custom attribute token?
    ULONG       ixTbl)                  // Table in question.
{
    mdToken tk = GetTokenForTable(ixTbl);
    size_t ix;
    for (ix=0; ix<g_CodedTokens[CDTKN_HasCustomAttribute].m_cTokens; ++ix)
        if (g_CodedTokens[CDTKN_HasCustomAttribute].m_pTokens[ix] == tk)
            return true;
    return false;
} // CMiniMdRW::CanHaveCustomAttribute
#endif //_DEBUG

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
//---------------------------------------------------------------------------------------
//
// Perform any available pre-save optimizations.
//
__checkReturn
HRESULT
CMiniMdRW::PreSaveFull()
{
    HRESULT hr = S_OK;
    RID     ridPtr;     // A RID from a pointer table.

    if (m_bPreSaveDone)
        return hr;

    // Don't yet know what the save size will be.
    m_cbSaveSize = 0;
    m_bSaveCompressed = false;

    // Convert any END_OF_TABLE values for tables with child pointer tables.
    IfFailGo(ConvertMarkerToEndOfTable(
        TBL_TypeDef,
        TypeDefRec::COL_MethodList,
        m_Schema.m_cRecs[TBL_Method] + 1,
        m_Schema.m_cRecs[TBL_TypeDef]));
    IfFailGo(ConvertMarkerToEndOfTable(
        TBL_TypeDef,
        TypeDefRec::COL_FieldList,
        m_Schema.m_cRecs[TBL_Field] + 1,
        m_Schema.m_cRecs[TBL_TypeDef]));
    IfFailGo(ConvertMarkerToEndOfTable(
        TBL_Method,
        MethodRec::COL_ParamList,
        m_Schema.m_cRecs[TBL_Param]+1,
        m_Schema.m_cRecs[TBL_Method]));
    IfFailGo(ConvertMarkerToEndOfTable(
        TBL_PropertyMap,
        PropertyMapRec::COL_PropertyList,
        m_Schema.m_cRecs[TBL_Property] + 1,
        m_Schema.m_cRecs[TBL_PropertyMap]));
    IfFailGo(ConvertMarkerToEndOfTable(
        TBL_EventMap,
        EventMapRec::COL_EventList,
        m_Schema.m_cRecs[TBL_Event] + 1,
        m_Schema.m_cRecs[TBL_EventMap]));

    // If there is a handler and in "Full" mode, eliminate the intermediate tables.
    if ((m_pHandler != NULL) && ((m_OptionValue.m_UpdateMode &MDUpdateMask) == MDUpdateFull))
    {
        // If there is a handler, and not in E&C, save as fully compressed.
        m_bSaveCompressed = true;

        // Temporary tables for new Fields, Methods, Params and FieldLayouts.
        MetaData::TableRW newFields;
        IfFailGo(newFields.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_Field].m_cbRec,
            m_Schema.m_cRecs[TBL_Field]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(newFields.Debug_SetTableInfo("TBL_Field", TBL_Field));

        MetaData::TableRW newMethods;
        IfFailGo(newMethods.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_Method].m_cbRec,
            m_Schema.m_cRecs[TBL_Method]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(newMethods.Debug_SetTableInfo("TBL_Method", TBL_Method));

        MetaData::TableRW newParams;
        IfFailGo(newParams.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_Param].m_cbRec,
            m_Schema.m_cRecs[TBL_Param]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(newParams.Debug_SetTableInfo("TBL_Param", TBL_Param));

        MetaData::TableRW newEvents;
        IfFailGo(newEvents.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_Event].m_cbRec,
            m_Schema.m_cRecs[TBL_Event]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(newEvents.Debug_SetTableInfo("TBL_Event", TBL_Event));

        MetaData::TableRW newPropertys;
        IfFailGo(newPropertys.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_Property].m_cbRec,
            m_Schema.m_cRecs[TBL_Property]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(newPropertys.Debug_SetTableInfo("TBL_Property", TBL_Property));

        // If we have any indirect table for Field or Method and we are about to reorder these
        // tables, the MemberDef hash table will be invalid after the token movement. So invalidate
        // the hash.
        if ((HasIndirectTable(TBL_Field) || HasIndirectTable(TBL_Method)) && (m_pMemberDefHash != NULL))
        {
            delete m_pMemberDefHash;
            m_pMemberDefHash = NULL;
        }

        // Enumerate fields and copy.
        if (HasIndirectTable(TBL_Field))
        {
            for (ridPtr = 1; ridPtr <= m_Schema.m_cRecs[TBL_Field]; ++ridPtr)
            {
                BYTE * pOldPtr;
                IfFailGo(m_Tables[TBL_FieldPtr].GetRecord(ridPtr, &pOldPtr));
                RID ridOld;
                ridOld = GetCol(TBL_FieldPtr, FieldPtrRec::COL_Field, pOldPtr);
                BYTE * pOld;
                IfFailGo(m_Tables[TBL_Field].GetRecord(ridOld, &pOld));
                RID ridNew;
                BYTE * pNew;
                IfFailGo(newFields.AddRecord(&pNew, (UINT32 *)&ridNew));
                _ASSERTE(ridNew == ridPtr);
                memcpy(pNew, pOld, m_TableDefs[TBL_Field].m_cbRec);

                // Let the caller know of the token change.
                IfFailGo(MapToken(ridOld, ridNew, mdtFieldDef));
            }
        }

        // Enumerate methods and copy.
        if (HasIndirectTable(TBL_Method) || HasIndirectTable(TBL_Param))
        {
            for (ridPtr = 1; ridPtr <= m_Schema.m_cRecs[TBL_Method]; ++ridPtr)
            {
                MethodRec * pOld;
                RID ridOld;
                BYTE * pNew = NULL;
                if (HasIndirectTable(TBL_Method))
                {
                    BYTE * pOldPtr;
                    IfFailGo(m_Tables[TBL_MethodPtr].GetRecord(ridPtr, &pOldPtr));
                    ridOld = GetCol(TBL_MethodPtr, MethodPtrRec::COL_Method, pOldPtr);
                    IfFailGo(GetMethodRecord(ridOld, &pOld));
                    RID ridNew;
                    IfFailGo(newMethods.AddRecord(&pNew, (UINT32 *)&ridNew));
                    _ASSERTE(ridNew == ridPtr);
                    memcpy(pNew, pOld, m_TableDefs[TBL_Method].m_cbRec);

                    // Let the caller know of the token change.
                    IfFailGo(MapToken(ridOld, ridNew, mdtMethodDef));
                }
                else
                {
                    ridOld = ridPtr;
                    IfFailGo(GetMethodRecord(ridPtr, &pOld));
                }

                // Handle the params of the method.
                if (HasIndirectTable(TBL_Method))
                {
                    IfFailGo(PutCol(TBL_Method, MethodRec::COL_ParamList, pNew, newParams.GetRecordCount() + 1));
                }
                RID ixStart = getParamListOfMethod(pOld);
                RID ixEnd;
                IfFailGo(getEndParamListOfMethod(ridOld, &ixEnd));
                for (; ixStart<ixEnd; ++ixStart)
                {
                    RID ridParam;
                    if (HasIndirectTable(TBL_Param))
                    {
                        BYTE * pOldPtr;
                        IfFailGo(m_Tables[TBL_ParamPtr].GetRecord(ixStart, &pOldPtr));
                        ridParam = GetCol(TBL_ParamPtr, ParamPtrRec::COL_Param, pOldPtr);
                    }
                    else
                    {
                        ridParam = ixStart;
                    }
                    BYTE * pOldRecord;
                    IfFailGo(m_Tables[TBL_Param].GetRecord(ridParam, &pOldRecord));
                    RID ridNew;
                    BYTE * pNewRecord;
                    IfFailGo(newParams.AddRecord(&pNewRecord, (UINT32 *)&ridNew));
                    memcpy(pNewRecord, pOldRecord, m_TableDefs[TBL_Param].m_cbRec);

                    // Let the caller know of the token change.
                    IfFailGo(MapToken(ridParam, ridNew, mdtParamDef));
                }
            }
        }

        // Get rid of EventPtr and PropertyPtr table as well
        // Enumerate fields and copy.
        if (HasIndirectTable(TBL_Event))
        {
            for (ridPtr = 1; ridPtr <= m_Schema.m_cRecs[TBL_Event]; ++ridPtr)
            {
                BYTE * pOldPtr;
                IfFailGo(m_Tables[TBL_EventPtr].GetRecord(ridPtr, &pOldPtr));
                RID ridOld;
                ridOld = GetCol(TBL_EventPtr, EventPtrRec::COL_Event, pOldPtr);
                BYTE * pOld;
                IfFailGo(m_Tables[TBL_Event].GetRecord(ridOld, &pOld));
                RID ridNew;
                BYTE * pNew;
                IfFailGo(newEvents.AddRecord(&pNew, (UINT32 *)&ridNew));
                _ASSERTE(ridNew == ridPtr);
                memcpy(pNew, pOld, m_TableDefs[TBL_Event].m_cbRec);

                // Let the caller know of the token change.
                IfFailGo(MapToken(ridOld, ridNew, mdtEvent));
            }
        }

        if (HasIndirectTable(TBL_Property))
        {
            for (ridPtr = 1; ridPtr <= m_Schema.m_cRecs[TBL_Property]; ++ridPtr)
            {
                BYTE * pOldPtr;
                IfFailGo(m_Tables[TBL_PropertyPtr].GetRecord(ridPtr, &pOldPtr));
                RID ridOld;
                ridOld = GetCol(TBL_PropertyPtr, PropertyPtrRec::COL_Property, pOldPtr);
                BYTE * pOld;
                IfFailGo(m_Tables[TBL_Property].GetRecord(ridOld, &pOld));
                RID ridNew;
                BYTE * pNew;
                IfFailGo(newPropertys.AddRecord(&pNew, (UINT32 *)&ridNew));
                _ASSERTE(ridNew == ridPtr);
                memcpy(pNew, pOld, m_TableDefs[TBL_Property].m_cbRec);

                // Let the caller know of the token change.
                IfFailGo(MapToken(ridOld, ridNew, mdtProperty));
            }
        }


        // Replace the old tables with the new, sorted ones.
        if (HasIndirectTable(TBL_Field))
        {
            m_Tables[TBL_Field].Delete();
            IfFailGo(m_Tables[TBL_Field].InitializeFromTable(
                &newFields,
                TRUE));     // fCopyData
        }
        if (HasIndirectTable(TBL_Method))
        {
            m_Tables[TBL_Method].Delete();
            IfFailGo(m_Tables[TBL_Method].InitializeFromTable(
                &newMethods,
                TRUE));     // fCopyData
        }
        if (HasIndirectTable(TBL_Method) || HasIndirectTable(TBL_Param))
        {
            m_Tables[TBL_Param].Delete();
            IfFailGo(m_Tables[TBL_Param].InitializeFromTable(
                &newParams,
                TRUE));     // fCopyData
        }
        if (HasIndirectTable(TBL_Property))
        {
            m_Tables[TBL_Property].Delete();
            IfFailGo(m_Tables[TBL_Property].InitializeFromTable(
                &newPropertys,
                TRUE));     // fCopyData
        }
        if (HasIndirectTable(TBL_Event))
        {
            m_Tables[TBL_Event].Delete();
            IfFailGo(m_Tables[TBL_Event].InitializeFromTable(
                &newEvents,
                TRUE));     // fCopyData
        }

        // Empty the pointer tables table.
        m_Schema.m_cRecs[TBL_FieldPtr] = 0;
        m_Schema.m_cRecs[TBL_MethodPtr] = 0;
        m_Schema.m_cRecs[TBL_ParamPtr] = 0;
        m_Schema.m_cRecs[TBL_PropertyPtr] = 0;
        m_Schema.m_cRecs[TBL_EventPtr] = 0;

        // invalidated the parent look up tables
        if (m_pMethodMap)
        {
            delete m_pMethodMap;
            m_pMethodMap = NULL;
        }
        if (m_pFieldMap)
        {
            delete m_pFieldMap;
            m_pFieldMap = NULL;
        }
        if (m_pPropertyMap)
        {
            delete m_pPropertyMap;
            m_pPropertyMap = NULL;
        }
        if (m_pEventMap)
        {
            delete m_pEventMap;
            m_pEventMap = NULL;
        }
        if (m_pParamMap)
        {
            delete m_pParamMap;
            m_pParamMap = NULL;
        }
    }

    // Do the ref to def fixup before fix up with token movement
    IfFailGo(FixUpRefToDef());

    ////////////////////////////////////////////////////////////////////////////
    //
    // We now need to do two kinds of fixups, and the two fixups interact with
    //  each other.
    // 1) We need to sort several tables for binary searching.
    // 2) We need to fixup any references to other tables, which may have
    //    changed due to ref-to-def, ptr-table elimination, or sorting.
    //


    // First do fixups.  Some of these are then sorted based on fixed-up columns.

    IfFailGo(FixUpTable(TBL_MemberRef));
    IfFailGo(FixUpTable(TBL_MethodSemantics));
    IfFailGo(FixUpTable(TBL_Constant));
    IfFailGo(FixUpTable(TBL_FieldMarshal));
    IfFailGo(FixUpTable(TBL_MethodImpl));
    IfFailGo(FixUpTable(TBL_DeclSecurity));
    IfFailGo(FixUpTable(TBL_ImplMap));
    IfFailGo(FixUpTable(TBL_FieldRVA));
    IfFailGo(FixUpTable(TBL_FieldLayout));

    if (SupportsGenerics())
    {
        IfFailGo(FixUpTable(TBL_GenericParam));
        IfFailGo(FixUpTable(TBL_MethodSpec));
    }

    // Now sort any tables that are allowed to have custom attributes.
    //  This block for tables sorted in full mode only -- basically
    //  tables for which we hand out tokens.
    if ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateFull)
    {
        if (SupportsGenerics())
        {
            // Sort the GenericParam table by the Owner.
            // Don't disturb the sequence ordering within Owner
            STABLESORTER_WITHREMAP(GenericParam, Owner);
            IfFailGo(sortGenericParam.Sort());
        }

        // Sort the InterfaceImpl table by class.
        STABLESORTER_WITHREMAP(InterfaceImpl, Class);
        IfFailGo(sortInterfaceImpl.Sort());

        // Sort the DeclSecurity table by parent.
        SORTER_WITHREMAP(DeclSecurity, Parent);
        IfFailGo(sortDeclSecurity.Sort());
    }

    // The GenericParamConstraint table is parented to the GenericParam table,
    //  so it needs fixup after sorting GenericParam table.
    if (SupportsGenerics())
    {
        IfFailGo(FixUpTable(TBL_GenericParamConstraint));

        // After fixing up the GenericParamConstraint table, we can then
        // sort it.
        if ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateFull)
        {
            // Sort the GenericParamConstraint table by the Owner.
            // Don't disturb the sequence ordering within Owner
            STABLESORTER_WITHREMAP(GenericParamConstraint, Owner);
            IfFailGo(sortGenericParamConstraint.Sort());
        }
    }
    // Fixup the custom attribute table.  After this, do not sort any table
    //  that is allowed to have a custom attribute.
    IfFailGo(FixUpTable(TBL_CustomAttribute));

    // Sort tables for binary searches.
    if (((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateFull) ||
        ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateIncremental))
    {
        // Sort tables as required
        //-------------------------------------------------------------------------
        // Module order is preserved
        // TypeRef order is preserved
        // TypeDef order is preserved
        // Field grouped and pointed to by TypeDef
        // Method grouped and pointed to by TypeDef
        // Param grouped and pointed to by Method
        // InterfaceImpl sorted here
        // MemberRef order is preserved
        // Constant sorted here
        // CustomAttribute sorted INCORRECTLY!! here
        // FieldMarshal sorted here
        // DeclSecurity sorted here
        // ClassLayout created in order with TypeDefs
        // FieldLayout grouped and pointed to by ClassLayouts
        // StandaloneSig order is preserved
        // TypeSpec order is preserved
        // EventMap created in order at conversion (by Event Parent)
        // Event sorted by Parent at conversion
        // PropertyMap created in order at conversion (by Property Parent)
        // Property sorted by Parent at conversion
        // MethodSemantics sorted by Association at conversion.
        // MethodImpl sorted here.
        // Sort the constant table by parent.
        // Sort the nested class table by NestedClass.
        // Sort the generic par table by Owner
        // MethodSpec order is preserved

        // Always sort Constant table
        _ASSERTE(!CanHaveCustomAttribute(TBL_Constant));
        SORTER(Constant, Parent);
        sortConstant.Sort();

        // Always sort the FieldMarshal table by Parent.
        _ASSERTE(!CanHaveCustomAttribute(TBL_FieldMarshal));
        SORTER(FieldMarshal, Parent);
        sortFieldMarshal.Sort();

        // Always sort the MethodSematics
        _ASSERTE(!CanHaveCustomAttribute(TBL_MethodSemantics));
        SORTER(MethodSemantics, Association);
        sortMethodSemantics.Sort();

        // Always Sort the ClassLayoutTable by parent.
        _ASSERTE(!CanHaveCustomAttribute(TBL_ClassLayout));
        SORTER(ClassLayout, Parent);
        sortClassLayout.Sort();

        // Always Sort the FieldLayoutTable by parent.
        _ASSERTE(!CanHaveCustomAttribute(TBL_FieldLayout));
        SORTER(FieldLayout, Field);
        sortFieldLayout.Sort();

        // Always Sort the ImplMap table by the parent.
        _ASSERTE(!CanHaveCustomAttribute(TBL_ImplMap));
        SORTER(ImplMap, MemberForwarded);
        sortImplMap.Sort();

        // Always Sort the FieldRVA table by the Field.
        _ASSERTE(!CanHaveCustomAttribute(TBL_FieldRVA));
        SORTER(FieldRVA, Field);
        sortFieldRVA.Sort();

        // Always Sort the NestedClass table by the NestedClass.
        _ASSERTE(!CanHaveCustomAttribute(TBL_NestedClass));
        SORTER(NestedClass, NestedClass);
        sortNestedClass.Sort();

        // Always Sort the MethodImpl table by the Class.
        _ASSERTE(!CanHaveCustomAttribute(TBL_MethodImpl));
        SORTER(MethodImpl, Class);
        sortMethodImpl.Sort();

        // Some tokens are not moved in ENC mode; only "full" mode.
        if ((m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateFull)
        {
            // Sort the CustomAttribute table by parent.
            _ASSERTE(!CanHaveCustomAttribute(TBL_CustomAttribute));
            SORTER_WITHREMAP(CustomAttribute, Parent);
            IfFailGo(sortCustomAttribute.Sort());
        }

        // Determine if the PropertyMap and EventMap are already sorted, and set the flag appropriately
        SORTER(PropertyMap, Parent);
        sortPropertyMap.CheckSortedWithNoDuplicates();

        SORTER(EventMap, Parent);
        sortEventMap.CheckSortedWithNoDuplicates();

    //-------------------------------------------------------------------------
    } // enclosing scope required for initialization ("goto" above skips initialization).

    m_bPreSaveDone = true;

    // send the Ref->Def optimization notification to host
    if (m_pHandler != NULL)
    {
        TOKENMAP * ptkmap = GetMemberRefToMemberDefMap();
        PREFIX_ASSUME(ptkmap != NULL);  // RegMeta always inits this.
        MDTOKENMAP * ptkRemap = GetTokenMovementMap();
        int     iCount = m_Schema.m_cRecs[TBL_MemberRef];
        mdToken tkTo;
        mdToken tkDefTo;
        int     i;
        MemberRefRec * pMemberRefRec;   // A MemberRefRec.
        const COR_SIGNATURE * pvSig;    // Signature of the MemberRef.
        ULONG                 cbSig;    // Size of the signature blob.

        // loop through all LocalVar
        for (i = 1; i <= iCount; i++)
        {
            tkTo = *(ptkmap->Get(i));
            if (RidFromToken(tkTo) != mdTokenNil)
            {
                // so far, the parent of memberref can be changed to only fielddef or methoddef
                // or it will remain unchanged.
                //
                _ASSERTE((TypeFromToken(tkTo) == mdtFieldDef) || (TypeFromToken(tkTo) == mdtMethodDef));

                IfFailGo(GetMemberRefRecord(i, &pMemberRefRec));
                IfFailGo(getSignatureOfMemberRef(pMemberRefRec, &pvSig, &cbSig));

                // Don't turn mr's with vararg's into defs, because the variable portion
                // of the call is kept in the mr signature.
                if ((pvSig != NULL) && isCallConv(*pvSig, IMAGE_CEE_CS_CALLCONV_VARARG))
                    continue;

                // ref is optimized to the def

                // now remap the def since def could be moved again.
                tkDefTo = ptkRemap->SafeRemap(tkTo);

                // when Def token moves, it will not change type!!
                _ASSERTE(TypeFromToken(tkTo) == TypeFromToken(tkDefTo));
                LOG((LOGMD, "MapToken (remap): from 0x%08x to 0x%08x\n", TokenFromRid(i, mdtMemberRef), tkDefTo));
                m_pHandler->Map(TokenFromRid(i, mdtMemberRef), tkDefTo);
            }
        }
    }

    // Ok, we've applied all of the token remaps. Make sure we don't apply them again in the future
    if (GetTokenMovementMap() != NULL)
        IfFailGo(GetTokenMovementMap()->EmptyMap());

ErrExit:

    return hr;
} // CMiniMdRW::PreSaveFull

#ifdef _PREFAST_
#pragma warning(pop)
#endif

//---------------------------------------------------------------------------------------
//
// ENC-specific pre-safe work.
//
__checkReturn
HRESULT
CMiniMdRW::PreSaveEnc()
{
    HRESULT hr;
    int     iNew;                   // Insertion point for new tokens.
    ULONG  *pul;                   // Found token.
    ULONG   iRid;                   // RID from a token.
    ULONG   ixTbl;                  // Table from an ENC record.
    ULONG   cRecs;                  // Count of records in a table.

    IfFailGo(PreSaveFull());

    // Turn off pre-save bit so that we can add ENC map records.
    m_bPreSaveDone = false;

    if (m_Schema.m_cRecs[TBL_ENCLog])
    {   // Keep track of ENC recs we've seen.
        _ASSERTE(m_rENCRecs == 0);
        m_rENCRecs = new (nothrow) ULONGARRAY[m_TblCount];
        IfNullGo(m_rENCRecs);

        // Create the temporary table.
        MetaData::TableRW tempTable;
        IfFailGo(tempTable.InitializeEmpty_WithRecordCount(
            m_TableDefs[TBL_ENCLog].m_cbRec,
            m_Schema.m_cRecs[TBL_ENCLog]
            COMMA_INDEBUG_MD(TRUE)));
        INDEBUG_MD(tempTable.Debug_SetTableInfo("TBL_ENCLog", TBL_ENCLog));

        // For each row in the data.
        RID     rid;
        ULONG   iKept=0;
        for (rid=1; rid<=m_Schema.m_cRecs[TBL_ENCLog]; ++rid)
        {
            ENCLogRec *pFrom;
            IfFailGo(m_Tables[TBL_ENCLog].GetRecord(rid, reinterpret_cast<BYTE **>(&pFrom)));

            // Keep this record?
            if (pFrom->GetFuncCode() == 0)
            {   // No func code.  Skip if we've seen this token before.

                // What kind of record is this?
                if (IsRecId(pFrom->GetToken()))
                {   // Non-token table
                    iRid = RidFromRecId(pFrom->GetToken());
                    ixTbl = TblFromRecId(pFrom->GetToken());
                }
                else
                {   // Token table.
                    iRid = RidFromToken(pFrom->GetToken());
                    ixTbl = GetTableForToken(pFrom->GetToken());

                }

                RIDBinarySearch searcher((UINT32 *)m_rENCRecs[ixTbl].Ptr(), m_rENCRecs[ixTbl].Count());
                pul = (ULONG *)(searcher.Find((UINT32 *)&iRid, &iNew));
                // If we found the token, don't keep the record.
                if (pul != 0)
                {
                    LOG((LOGMD, "PreSave ENCLog skipping duplicate token %d", pFrom->GetToken()));
                    continue;
                }
                // First time token was seen, so keep track of it.
                IfNullGo(pul = m_rENCRecs[ixTbl].Insert(iNew));
                *pul = iRid;
            }

            // Keeping the record, so allocate the new record to hold it.
            ++iKept;
            RID ridNew;
            ENCLogRec *pTo;
            IfFailGo(tempTable.AddRecord(reinterpret_cast<BYTE **>(&pTo), (UINT32 *)&ridNew));
            _ASSERTE(ridNew == iKept);

            // copy the data.
            *pTo = *pFrom;
        }

        // Keep the expanded table.
        m_Tables[TBL_ENCLog].Delete();
        IfFailGo(m_Tables[TBL_ENCLog].InitializeFromTable(
            &tempTable,
            TRUE));         // fCopyData
        INDEBUG_MD(m_Tables[TBL_ENCLog].Debug_SetTableInfo("TBL_ENCLog", TBL_ENCLog));
        m_Schema.m_cRecs[TBL_ENCLog] = iKept;

        // If saving only deltas, build the ENC Map table.
        if (((m_OptionValue.m_UpdateMode & MDUpdateDelta)) == MDUpdateDelta)
        {
            cRecs = 0;
            for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            {
                cRecs += m_rENCRecs[ixTbl].Count();
            }
            m_Tables[TBL_ENCMap].Delete();

            m_Schema.m_cRecs[TBL_ENCMap] = 0;

            IfFailGo(m_Tables[TBL_ENCMap].InitializeEmpty_WithRecordCount(
                m_TableDefs[TBL_ENCMap].m_cbRec,
                cRecs
                COMMA_INDEBUG_MD(TRUE)));
            INDEBUG_MD(m_Tables[TBL_ENCMap].Debug_SetTableInfo("TBL_ENCMap", TBL_ENCMap));
            cRecs = 0;
            for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            {
                ENCMapRec *pNew;
                RID nNew;
                for (int i=0; i<m_rENCRecs[ixTbl].Count(); ++i)
                {
                    IfFailGo(AddENCMapRecord(&pNew, &nNew)); // pre-allocated for all rows.
                    _ASSERTE(nNew == ++cRecs);
                    _ASSERTE(TblFromRecId(RecIdFromRid(m_rENCRecs[ixTbl][i], ixTbl)) < m_TblCount);
                    pNew->SetToken(RecIdFromRid(m_rENCRecs[ixTbl][i], ixTbl));
                }
            }
        }
    }

    // Turn pre-save bit back on.
    m_bPreSaveDone = true;

ErrExit:
    return hr;
} // CMiniMdRW::PreSaveEnc

//*****************************************************************************
// Perform any appropriate pre-save optimization or reorganization.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PreSave(
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT hr = S_OK;

#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_PreSaveBreak))
    {
        _ASSERTE(!"CMiniMdRW::PreSave()");
    }
#endif //_DEBUG

    if (m_bPreSaveDone)
        return hr;

    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
    case MDUpdateIncremental:
    case MDUpdateExtension:
        hr = PreSaveFull();
        break;
    // PreSaveEnc removes duplicate entries in the ENCLog table,
    // which we need to do regardless if we're saving a full MD
    // or a minimal delta.
    case MDUpdateDelta:
    case MDUpdateENC:
        hr = PreSaveEnc();
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::PreSave

//*****************************************************************************
// Perform any necessary post-save cleanup.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PostSave()
{
    if (m_rENCRecs)
    {
        delete [] m_rENCRecs;
        m_rENCRecs = 0;
    }

    m_bPreSaveDone = false;

    return S_OK;
} // CMiniMdRW::PostSave

//*****************************************************************************
// Save the tables to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveFullTablesToStream(
    IStream                  *pIStream,
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT     hr;
    CMiniTableDef   sTempTable;         // Definition for a temporary table.
    CQuickArray<CMiniColDef> rTempCols; // Definition for a temp table's columns.
    BYTE        SchemaBuf[sizeof(CMiniMdSchema)];   //Buffer for compressed schema.
    ULONG       cbAlign;                // Bytes needed for alignment.
    UINT32      cbTable;                // Bytes in a table.
    UINT32      cbTotal;                // Bytes written.
    static const unsigned char zeros[8] = {0}; // For padding and alignment.

    // Write the header.
    CMiniMdSchema Schema = m_Schema;
    IfFailGo(m_StringHeap.GetAlignedSize(&cbTable));
    if (cbTable > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_STRING_4;
    }

    if (m_GuidHeap.GetSize() > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_GUID_4;
    }

    IfFailGo(m_BlobHeap.GetAlignedSize(&cbTable));
    if (cbTable > USHRT_MAX)
    {
        Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;
    }
    else
    {
        Schema.m_heaps &= ~CMiniMdSchema::HEAP_BLOB_4;
    }

    cbTotal = Schema.SaveTo(SchemaBuf);
    IfFailGo(pIStream->Write(SchemaBuf, cbTotal, 0));
    if ( (cbAlign = Align4(cbTotal) - cbTotal) != 0)
        IfFailGo(pIStream->Write(&hr, cbAlign, 0));
    cbTotal += cbAlign;

    ULONG headerOffset[TBL_COUNT];
    _ASSERTE(m_TblCount <= TBL_COUNT);

    ULONG ixTbl;
    // For each table...
    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {
        headerOffset[ixTbl] = ~0U;

        ULONG itemCount = GetCountRecs(ixTbl);
        if (itemCount)
        {

            // Compress the records by allocating a new, temporary, table and
            //  copying the rows from the one to the new.

            // If the table was grown, shrink it as much as possible.
            if (m_eGrow == eg_grown)
            {

                // Allocate a def for the temporary table.
                sTempTable = m_TableDefs[ixTbl];
                IfFailGo(rTempCols.ReSizeNoThrow(sTempTable.m_cCols));
                sTempTable.m_pColDefs = rTempCols.Ptr();

                // Initialize temp table col defs based on actual counts of data in the
                //  real tables.
                IfFailGo(InitColsForTable(Schema, ixTbl, &sTempTable, 1, FALSE));

                // Create the temporary table.
                MetaData::TableRW tempTable;
                IfFailGo(tempTable.InitializeEmpty_WithRecordCount(
                    sTempTable.m_cbRec,
                    m_Schema.m_cRecs[ixTbl]
                    COMMA_INDEBUG_MD(TRUE)));
                INDEBUG_MD(tempTable.Debug_SetTableInfo(NULL, ixTbl));

                // For each row in the data.
                RID rid;
                for (rid=1; rid<=m_Schema.m_cRecs[ixTbl]; ++rid)
                {
                    RID ridNew;
                    BYTE *pRow;
                    IfFailGo(m_Tables[ixTbl].GetRecord(rid, &pRow));
                    BYTE *pNew;
                    IfFailGo(tempTable.AddRecord(&pNew, (UINT32 *)&ridNew));
                    _ASSERTE(rid == ridNew);

                    // For each column.
                    for (ULONG ixCol=0; ixCol<sTempTable.m_cCols; ++ixCol)
                    {
                        // Copy the data to the temp table.
                        ULONG ulVal = GetCol(ixTbl, ixCol, pRow);
                        IfFailGo(PutCol(rTempCols[ixCol], pNew, ulVal));
                    }
                }           // Persist the temp table to the stream.
                {
                    IfFailGo(tempTable.GetRecordsDataSize(&cbTable));
                    _ASSERTE(cbTable == sTempTable.m_cbRec * GetCountRecs(ixTbl));
                    IfFailGo(tempTable.SaveToStream(
                        pIStream));
                }
                cbTotal += cbTable;
            }
            else
            {   // Didn't grow, so just persist directly to stream.
                {
                    IfFailGo(m_Tables[ixTbl].GetRecordsDataSize(&cbTable));
                    _ASSERTE(cbTable == m_TableDefs[ixTbl].m_cbRec * GetCountRecs(ixTbl));
                    IfFailGo(m_Tables[ixTbl].SaveToStream(
                        pIStream));
                }
                cbTotal += cbTable;
            }
            // NewArrayHolder hotItemList going out of scope - no delete [] necessary
        }
    }

    // Pad with at least 2 bytes and align on 4 bytes.
    cbAlign = Align4(cbTotal) - cbTotal;
    if (cbAlign < 2)
        cbAlign += 4;
    IfFailGo(pIStream->Write(zeros, cbAlign, 0));
    cbTotal += cbAlign;
    _ASSERTE((m_cbSaveSize == 0) || (m_cbSaveSize == cbTotal));

ErrExit:
    return hr;
} // CMiniMdRW::SaveFullTablesToStream

//*****************************************************************************
// Save the tables to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveENCTablesToStream(
    IStream *pIStream)
{
    HRESULT hr;
    BYTE    SchemaBuf[sizeof(CMiniMdSchema)];   //Buffer for compressed schema.
    ULONG   cbAlign;        // Bytes needed for alignment.
    ULONG   cbTable;        // Bytes in a table.
    ULONG   cbTotal;        // Bytes written.
    ULONG   ixTbl;          // Table counter.
    static const unsigned char zeros[8] = {0}; // For padding and alignment.

    // Make sure the minimal delta has a fully expanded table
    IfFailRet(ExpandTables());

    // Write the header.
    CMiniMdSchema Schema = m_Schema;
    Schema.m_heaps |= CMiniMdSchema::DELTA_ONLY;

    if (m_rENCRecs != NULL)
    {
        for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            Schema.m_cRecs[ixTbl] = m_rENCRecs[ixTbl].Count();
    }
    else
    {
        for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
            Schema.m_cRecs[ixTbl] = 0;
    }

    Schema.m_cRecs[TBL_Module] = m_Schema.m_cRecs[TBL_Module];
    Schema.m_cRecs[TBL_ENCLog] = m_Schema.m_cRecs[TBL_ENCLog];
    Schema.m_cRecs[TBL_ENCMap] = m_Schema.m_cRecs[TBL_ENCMap];

    cbTotal = Schema.SaveTo(SchemaBuf);
    IfFailGo(pIStream->Write(SchemaBuf, cbTotal, 0));
    if ( (cbAlign = Align4(cbTotal) - cbTotal) != 0)
        IfFailGo(pIStream->Write(&hr, cbAlign, 0));
    cbTotal += cbAlign;

    // For each table...
    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {
        if (ixTbl == TBL_ENCLog || ixTbl == TBL_ENCMap || ixTbl == TBL_Module)
        {
            if (m_Schema.m_cRecs[ixTbl] == 0)
                continue; // pretty strange if ENC has no enc data.
            // Persist the ENC table.
            IfFailGo(m_Tables[ixTbl].GetRecordsDataSize((UINT32 *)&cbTable));
            _ASSERTE(cbTable == m_TableDefs[ixTbl].m_cbRec * m_Schema.m_cRecs[ixTbl]);
            cbTotal += cbTable;
            IfFailGo(m_Tables[ixTbl].SaveToStream(
                pIStream));
        }
        else
        if (Schema.m_cRecs[ixTbl])
        {
            // Copy just the delta records.

            // Create the temporary table.
            MetaData::TableRW tempTable;
            IfFailGo(tempTable.InitializeEmpty_WithRecordCount(
                m_TableDefs[ixTbl].m_cbRec,
                Schema.m_cRecs[ixTbl]
                COMMA_INDEBUG_MD(TRUE)));   // fIsReadWrite
            INDEBUG_MD(tempTable.Debug_SetTableInfo(NULL, ixTbl));

            // For each row in the data.
            RID rid;
            for (ULONG iDelta=0; iDelta<Schema.m_cRecs[ixTbl]; ++iDelta)
            {
                RID ridNew;
                rid = m_rENCRecs[ixTbl][iDelta];
                BYTE *pRow;
                IfFailGo(m_Tables[ixTbl].GetRecord(rid, &pRow));
                BYTE *pNew;
                IfFailGo(tempTable.AddRecord(&pNew, (UINT32 *)&ridNew));
                _ASSERTE(iDelta+1 == ridNew);

                memcpy(pNew, pRow, m_TableDefs[ixTbl].m_cbRec);
            }
            // Persist the temp table to the stream.
            IfFailGo(tempTable.GetRecordsDataSize((UINT32 *)&cbTable));
            _ASSERTE(cbTable == m_TableDefs[ixTbl].m_cbRec * Schema.m_cRecs[ixTbl]);
            cbTotal += cbTable;
            IfFailGo(tempTable.SaveToStream(
                pIStream));
        }
    }

    // Pad with at least 2 bytes and align on 4 bytes.
    cbAlign = Align4(cbTotal) - cbTotal;
    if (cbAlign < 2)
        cbAlign += 4;
    IfFailGo(pIStream->Write(zeros, cbAlign, 0));
    cbTotal += cbAlign;
    _ASSERTE(m_cbSaveSize == 0 || m_cbSaveSize == cbTotal);

ErrExit:
    return hr;
} // CMiniMdRW::SaveENCTablesToStream

//*****************************************************************************
// Save the tables to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveTablesToStream(
    IStream                  *pIStream,              // The stream.
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT hr;

    // Prepare the data for save.
    IfFailGo(PreSave());

    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
    case MDUpdateIncremental:
    case MDUpdateExtension:
    case MDUpdateENC:
        hr = SaveFullTablesToStream(pIStream, reorderingOptions);
        break;
    case MDUpdateDelta:
        hr = SaveENCTablesToStream(pIStream);
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

ErrExit:
    return hr;
} // CMiniMdRW::SaveTablesToStream

//*****************************************************************************
// Save a full pool to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveFullPoolToStream(
    int      iPool,                 // The pool.
    IStream *pStream)               // The stream.
{
    HRESULT hr;

    switch (iPool)
    {
    case MDPoolStrings:
        hr = m_StringHeap.SaveToStream_Aligned(
            0,          // Start offset of the data to be stored
            pStream);
        break;
    case MDPoolGuids:
        hr = m_GuidHeap.SaveToStream(
            pStream);
        break;
    case MDPoolBlobs:
        hr = m_BlobHeap.SaveToStream_Aligned(
            0,          // Start offset of the data to be stored
            pStream);
        break;
    case MDPoolUSBlobs:
        hr = m_UserStringHeap.SaveToStream_Aligned(
            0,          // Start offset of the data to be stored
            pStream);
        break;
    default:
        hr = E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::SaveFullPoolToStream

//*****************************************************************************
// Save a ENC pool to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveENCPoolToStream(
    int      iPool,         // The pool.
    IStream *pIStream)      // The stream.
{
    HRESULT hr;

    switch (iPool)
    {
    case MDPoolStrings:
        {
            UINT32 nEnCDeltaStartOffset = m_StringHeap.GetEnCSessionStartHeapSize();
            hr = m_StringHeap.SaveToStream_Aligned(
                nEnCDeltaStartOffset,   // Start offset of the data to be stored
                pIStream);
            break;
        }
    case MDPoolGuids:
        {
            // Save full Guid heap (we never save EnC delta)
            hr = m_GuidHeap.SaveToStream(
                pIStream);
            break;
        }
    case MDPoolBlobs:
        {
            UINT32 nEnCDeltaStartOffset = m_BlobHeap.GetEnCSessionStartHeapSize();
            hr = m_BlobHeap.SaveToStream_Aligned(
                nEnCDeltaStartOffset,   // Start offset of the data to be stored
                pIStream);
            break;
        }
    case MDPoolUSBlobs:
        {
            UINT32 nEnCDeltaStartOffset = m_UserStringHeap.GetEnCSessionStartHeapSize();
            hr = m_UserStringHeap.SaveToStream_Aligned(
                nEnCDeltaStartOffset,   // Start offset of the data to be stored
                pIStream);
            break;
        }
    default:
        hr = E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::SaveENCPoolToStream

//*****************************************************************************
// Save a pool to the stream.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SavePoolToStream(
    int      iPool,         // The pool.
    IStream *pIStream)      // The stream.
{
    HRESULT hr;
    switch (m_OptionValue.m_UpdateMode & MDUpdateMask)
    {
    case MDUpdateFull:
    case MDUpdateIncremental:
    case MDUpdateExtension:
    case MDUpdateENC:
        hr = SaveFullPoolToStream(iPool, pIStream);
        break;
    case MDUpdateDelta:
        hr = SaveENCPoolToStream(iPool, pIStream);
        break;
    default:
        _ASSERTE(!"Internal error -- unknown save mode");
        return E_INVALIDARG;
    }

    return hr;
} // CMiniMdRW::SavePoolToStream

//*****************************************************************************
// Expand a table from the initial (hopeful) 2-byte column sizes to the large
//  (but always adequate) 4-byte column sizes.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ExpandTables()
{
    HRESULT       hr = S_OK;
    CMiniMdSchema Schema;       // Temp schema by which to build tables.
    ULONG         ixTbl;        // Table counter.

    // Allow function to be called many times.
    if (m_eGrow == eg_grown)
        return (S_OK);

    // OutputDebugStringA("Growing tables to large size.\n");

    // Make pool indices the large size.
    Schema.m_heaps = 0;
    Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;

    // Make Row counts the large size.
    memset(Schema.m_cRecs, 0, sizeof(Schema.m_cRecs));
    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
        Schema.m_cRecs[ixTbl] = USHRT_MAX+1;

    // Compute how many bits required to hold a rid.
    Schema.m_rid = 16;

    for (ixTbl=0; ixTbl<m_TblCount; ++ixTbl)
    {
        IfFailGo(ExpandTableColumns(Schema, ixTbl));
    }

    // Things are bigger now.
    m_Schema.m_rid = 16;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;
    m_iStringsMask = 0xffffffff;
    m_iGuidsMask = 0xffffffff;
    m_iBlobsMask = 0xffffffff;

    // Remember that we've grown.
    m_eGrow = eg_grown;
    m_maxRid = m_maxIx = UINT32_MAX;

ErrExit:
    return hr;
} // CMiniMdRW::ExpandTables


__checkReturn
HRESULT
CMiniMdRW::InitWithLargeTables()
{
    CMiniMdSchema Schema;       // Temp schema by which to build tables.
    HRESULT       hr = S_OK;

    // Make pool indices the large size.
    Schema.m_heaps = 0;
    Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;

    // Make Row counts the large size.
    memset(Schema.m_cRecs, 0, sizeof(Schema.m_cRecs));
    for (int ixTbl=0; ixTbl<(int)m_TblCount; ++ixTbl)
        Schema.m_cRecs[ixTbl] = USHRT_MAX+1;

    // Compute how many bits required to hold a rid.
    Schema.m_rid = 16;

    // For each table...
    for (int ixTbl=0; ixTbl<(int)m_TblCount; ++ixTbl)
    {
        IfFailRet(InitColsForTable(Schema, ixTbl, &m_TableDefs[ixTbl], 0, TRUE));
    }


    // Things are bigger now.
    m_Schema.m_rid = 16;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_STRING_4;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_GUID_4;
    m_Schema.m_heaps |= CMiniMdSchema::HEAP_BLOB_4;
    m_iStringsMask = 0xffffffff;
    m_iGuidsMask = 0xffffffff;

    return hr;
}// CMiniMdRW::InitWithLargeTables

//*****************************************************************************
// Expand the sizes of a tables columns according to a new schema.  When this
//  happens, all RID and Pool index columns expand from 2 to 4 bytes.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ExpandTableColumns(
    CMiniMdSchema &Schema,
    ULONG          ixTbl)
{
    HRESULT     hr;
    CMiniTableDef   sTempTable;         // Definition for a temporary table.
    CQuickBytes qbTempCols;
    ULONG       ixCol;                  // Column counter.
    ULONG       cbFixed;                // Count of bytes that don't move.
    CMiniColDef *pFromCols;             // Definitions of "from" columns.
    CMiniColDef *pToCols;               // Definitions of "To" columns.
    ULONG       cMoveCols;              // Count of columns to move.
    ULONG       cFixedCols;             // Count of columns to move.

    // Allocate a def for the temporary table.
    sTempTable = m_TableDefs[ixTbl];
    IfFailGo(qbTempCols.ReSizeNoThrow(sTempTable.m_cCols * sizeof(CMiniColDef) + 1));
    // Mark the array of columns as not allocated (not ALLOCATED_MEMORY_MARKER) for SetNewColumnDefinition
    // call bellow (code:#SetNewColumnDefinition_call)
    *(BYTE *)(qbTempCols.Ptr()) = 0;
    sTempTable.m_pColDefs = (CMiniColDef *)((BYTE *)(qbTempCols.Ptr()) + 1);

    // Initialize temp table col defs based on counts of data in the tables.
    IfFailGo(InitColsForTable(Schema, ixTbl, &sTempTable, 1, FALSE));

    if (GetCountRecs(ixTbl) > 0)
    {
        // Analyze the column definitions to determine the unchanged vs changed parts.
        cbFixed = 0;
        for (ixCol = 0; ixCol < sTempTable.m_cCols; ++ixCol)
        {
            if (sTempTable.m_pColDefs[ixCol].m_oColumn != m_TableDefs[ixTbl].m_pColDefs[ixCol].m_oColumn ||
                    sTempTable.m_pColDefs[ixCol].m_cbColumn != m_TableDefs[ixTbl].m_pColDefs[ixCol].m_cbColumn)
                break;
            cbFixed += sTempTable.m_pColDefs[ixCol].m_cbColumn;
        }
        if (ixCol == sTempTable.m_cCols)
        {
            // no column is changing. We are done.
            goto ErrExit;
        }
        cFixedCols = ixCol;
        pFromCols = &m_TableDefs[ixTbl].m_pColDefs[ixCol];
        pToCols   = &sTempTable.m_pColDefs[ixCol];
        cMoveCols = sTempTable.m_cCols - ixCol;
        for (; ixCol < sTempTable.m_cCols; ++ixCol)
        {
            _ASSERTE(sTempTable.m_pColDefs[ixCol].m_cbColumn == 4);
        }

        // Create the temporary table.
        MetaData::TableRW tempTable;
        IfFailGo(tempTable.InitializeEmpty_WithRecordCount(
            sTempTable.m_cbRec,
            m_Schema.m_cRecs[ixTbl]
            COMMA_INDEBUG_MD(TRUE)));   // fIsReadWrite
        INDEBUG_MD(tempTable.Debug_SetTableInfo(NULL, ixTbl));

        // For each row in the data.
        RID rid;    // Row iterator.

        for (rid = 1; rid <= m_Schema.m_cRecs[ixTbl]; ++rid)
        {
            RID   ridNew;
            BYTE *pFrom;
            BYTE *pTo;

            IfFailGo(m_Tables[ixTbl].GetRecord(rid, &pFrom));
            IfFailGo(tempTable.AddRecord(&pTo, (UINT32 *)&ridNew));
            _ASSERTE(rid == ridNew);

            // Move the fixed part.
            memcpy(pTo, pFrom, cbFixed);

            // Expand the expanded parts.
            for (ixCol = 0; ixCol < cMoveCols; ++ixCol)
            {
                if (m_TableDefs[ixTbl].m_pColDefs[cFixedCols + ixCol].m_cbColumn == sizeof(USHORT))
                {
                    // The places that access expect the int16 to be in the high bytes so we need to the extra swap
                    SET_UNALIGNED_VAL32((pTo + pToCols[ixCol].m_oColumn),  VAL16(*(USHORT*)(pFrom + pFromCols[ixCol].m_oColumn)));
                }
                else
                {
                    // In this case we're just copying the data over
                    memcpy(pTo + pToCols[ixCol].m_oColumn, pFrom + pFromCols[ixCol].m_oColumn, sizeof(ULONG));
                }
            }
        }

        // Keep the expanded table.
        m_Tables[ixTbl].Delete();
        IfFailGo(m_Tables[ixTbl].InitializeFromTable(
            &tempTable,
            TRUE));     // fCopyData
        INDEBUG_MD(m_Tables[ixTbl].Debug_SetTableInfo(NULL, ixTbl));
    }
    else
    {   // No data, so just reinitialize.
        m_Tables[ixTbl].Delete();
        IfFailGo(m_Tables[ixTbl].InitializeEmpty_WithRecordCount(
            sTempTable.m_cbRec,
            g_TblSizeInfo[0][ixTbl]
            COMMA_INDEBUG_MD(TRUE)));   // fIsReadWrite
        INDEBUG_MD(m_Tables[ixTbl].Debug_SetTableInfo(NULL, ixTbl));
    }

    //#SetNewColumnDefinition_call
    // Keep the new column defs.
    IfFailGo(SetNewColumnDefinition(&(m_TableDefs[ixTbl]), sTempTable.m_pColDefs, ixTbl));
    m_TableDefs[ixTbl].m_cbRec = sTempTable.m_cbRec;

ErrExit:
    return hr;
} // CMiniMdRW::ExpandTableColumns


//*****************************************************************************
// Used by caller to let us know save is completed.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::SaveDone()
{
    return PostSave();
} // CMiniMdRW::SaveDone

//*****************************************************************************
// General post-token-move table fixup.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FixUpTable(
    ULONG ixTbl)    // Index of table to fix.
{
    HRESULT hr = S_OK;
    ULONG   i, j;                   // Loop control.
    ULONG   cRows;                  // Count of rows in table.
    void   *pRec;                   // Pointer to row data.
    mdToken tk;                     // A token.
    ULONG   rCols[16];              // List of columns with token data.
    ULONG   cCols;                  // Count of columns with token data.

    // If no remaps, nothing to do.
    if (GetTokenMovementMap() == NULL)
        return S_OK;

    // Find the columns with token data.
    cCols = 0;
    _ASSERTE(m_TableDefs[ixTbl].m_cCols <= 16);
    for (i=0; i<m_TableDefs[ixTbl].m_cCols; ++i)
    {
        if (m_TableDefs[ixTbl].m_pColDefs[i].m_Type <= iCodedTokenMax)
            rCols[cCols++] = i;
    }
    _ASSERTE(cCols);
    if (cCols == 0)
        return S_OK;

    cRows = m_Schema.m_cRecs[ixTbl];

    // loop through all Rows
    for (i = 1; i<=cRows; ++i)
    {
        IfFailGo(getRow(ixTbl, i, &pRec));
        for (j=0; j<cCols; ++j)
        {
            tk = GetToken(ixTbl, rCols[j], pRec);
            tk = GetTokenMovementMap()->SafeRemap(tk);
            IfFailGo(PutToken(ixTbl, rCols[j], pRec, tk));
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::FixUpTable


//*****************************************************************************
// Fixup all the embedded ref to corresponding def before we remap tokens movement.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FixUpRefToDef()
{
    return NOERROR;
} // CMiniMdRW::FixUpRefToDef

//*****************************************************************************
// Given a table with a pointer (index) to a sequence of rows in another
//  table, get the RID of the end row.  This is the STL-ish end; the first row
//  not in the list.  Thus, for a list of 0 elements, the start and end will
//  be the same.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::Impl_GetEndRidForColumn(   // The End rid.
    UINT32       nTableIndex,
    RID          nRowIndex,
    CMiniColDef &def,                   // Column containing the RID into other table.
    UINT32       nTargetTableIndex,     // The other table.
    RID         *pEndRid)
{
    HRESULT hr;
    ULONG ixEnd;
    void *pRow;

    // Last rid in range from NEXT record, or count of table, if last record.
    _ASSERTE(nRowIndex <= m_Schema.m_cRecs[nTableIndex]);
    if (nRowIndex < m_Schema.m_cRecs[nTableIndex])
    {
        IfFailRet(getRow(nTableIndex, nRowIndex + 1, &pRow));
        ixEnd = getIX(pRow, def);
        // We use a special value, 'END_OF_TABLE' (currently 0), to indicate
        //  end-of-table.  If we find the special value we'll have to compute
        //  the value to return.  If we don't find the special value, then
        //  the value is correct.
        if (ixEnd != END_OF_TABLE)
        {
            *pEndRid = ixEnd;
            return S_OK;
        }
    }

    // Either the child pointer value in the next row was END_OF_TABLE, or
    //  the row is the last row of the table.  In either case, we must return
    //  a value which will work out to the END of the child table.  That
    //  value depends on the value in the row itself -- if the row contains
    //  END_OF_TABLE, there are no children, and to make the subtraction
    //  work out, we return END_OF_TABLE for the END value.  If the row
    //  contains some value, then we return the actual END count.
    IfFailRet(getRow(nTableIndex, nRowIndex, &pRow));
    if (getIX(pRow, def) == END_OF_TABLE)
    {
        ixEnd = END_OF_TABLE;
    }
    else
    {
        ixEnd = m_Schema.m_cRecs[nTargetTableIndex] + 1;
    }

    *pEndRid = ixEnd;
    return S_OK;
} // CMiniMd::Impl_GetEndRidForColumn

//*****************************************************************************
// Add a row to any table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddRecord(             // S_OK or error.
    UINT32 nTableIndex,          // The table to expand.
    void **ppRow,
    RID   *pRid)           // Put RID here.
{
    HRESULT hr;

    _ASSERTE(nTableIndex < m_TblCount);
    _ASSERTE(!m_bPreSaveDone && "Cannot add records after PreSave and before Save.");
    IfFailRet(m_Tables[nTableIndex].AddRecord(
        reinterpret_cast<BYTE **>(ppRow),
        reinterpret_cast<UINT32 *>(pRid)));
    if (*pRid > m_maxRid)
    {
        m_maxRid = *pRid;
        if (m_maxRid > m_limRid && m_eGrow == eg_ok)
        {
            // OutputDebugStringA("Growing tables due to Record overflow.\n");
            m_eGrow = eg_grow, m_maxRid = m_maxIx = UINT32_MAX;
        }
    }
    ++m_Schema.m_cRecs[nTableIndex];
    SetSorted(nTableIndex, false);
    if (m_pVS[nTableIndex] != NULL)
    {
        m_pVS[nTableIndex]->m_isMapValid = false;
    }

    return S_OK;
} // CMiniMdRW::AddRecord

//*****************************************************************************
// Add a row to the TypeDef table, and initialize the pointers to other tables.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddTypeDefRecord(
    TypeDefRec **ppRow,
    RID         *pnRowIndex)
{
    HRESULT hr;
    IfFailRet(AddRecord(TBL_TypeDef, (void **)ppRow, pnRowIndex));

    IfFailRet(PutCol(TBL_TypeDef, TypeDefRec::COL_MethodList, *ppRow, NewRecordPointerEndValue(TBL_Method)));
    IfFailRet(PutCol(TBL_TypeDef, TypeDefRec::COL_FieldList, *ppRow, NewRecordPointerEndValue(TBL_Field)));

    return S_OK;
} // CMiniMdRW::AddTypeDefRecord

//*****************************************************************************
// Add a row to the Method table, and initialize the pointers to other tables.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddMethodRecord(
    MethodRec **ppRow,
    RID        *pnRowIndex)
{
    HRESULT hr;
    IfFailRet(AddRecord(TBL_Method, (void **)ppRow, pnRowIndex));

    IfFailRet(PutCol(TBL_Method, MethodRec::COL_ParamList, *ppRow, NewRecordPointerEndValue(TBL_Param)));

    return S_OK;
} // CMiniMdRW::AddMethodRecord

//*****************************************************************************
// Add a row to the EventMap table, and initialize the pointers to other tables.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddEventMapRecord(
    EventMapRec **ppRow,
    RID          *pnRowIndex)
{
    HRESULT hr;
    IfFailRet(AddRecord(TBL_EventMap, (void **)ppRow, pnRowIndex));

    IfFailRet(PutCol(TBL_EventMap, EventMapRec::COL_EventList, *ppRow, NewRecordPointerEndValue(TBL_Event)));

    SetSorted(TBL_EventMap, false);

    return S_OK;
} // CMiniMdRW::AddEventMapRecord

//*********************************************************************************
// Add a row to the PropertyMap table, and initialize the pointers to other tables.
//*********************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddPropertyMapRecord(
    PropertyMapRec **ppRow,
    RID             *pnRowIndex)
{
    HRESULT hr;
    IfFailRet(AddRecord(TBL_PropertyMap, (void **)ppRow, pnRowIndex));

    IfFailRet(PutCol(TBL_PropertyMap, PropertyMapRec::COL_PropertyList, *ppRow, NewRecordPointerEndValue(TBL_Property)));

    SetSorted(TBL_PropertyMap, false);

    return S_OK;
} // CMiniMdRW::AddPropertyMapRecord

//*****************************************************************************
// converting a ANSI heap string to unicode string to an output buffer
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::Impl_GetStringW(
                             ULONG  ix,
    _Out_writes_ (cchBuffer) LPWSTR szOut,
                             ULONG  cchBuffer,
                             ULONG *pcchBuffer)
{
    LPCSTR  szString;       // Single byte version.
    int     iSize;          // Size of resulting string, in wide chars.
    HRESULT hr = NOERROR;

    IfFailGo(getString(ix, &szString));

    if (*szString == 0)
    {
        // If emtpy string "", return pccBuffer 0
        if ( szOut && cchBuffer )
            szOut[0] = W('\0');
        if ( pcchBuffer )
            *pcchBuffer = 0;
        goto ErrExit;
    }
    if (!(iSize=::WszMultiByteToWideChar(CP_UTF8, 0, szString, -1, szOut, cchBuffer)))
    {
        // What was the problem?
        DWORD dwNT = GetLastError();

        // Not truncation?
        if (dwNT != ERROR_INSUFFICIENT_BUFFER)
            IfFailGo(HRESULT_FROM_NT(dwNT));

        // Truncation error; get the size required.
        if (pcchBuffer)
            *pcchBuffer = ::WszMultiByteToWideChar(CP_UTF8, 0, szString, -1, NULL, 0);

        if ((szOut != NULL) && (cchBuffer > 0))
        {   // null-terminate the truncated output string
            szOut[cchBuffer - 1] = W('\0');
        }

        hr = CLDB_S_TRUNCATION;
        goto ErrExit;
    }
    if (pcchBuffer)
        *pcchBuffer = iSize;

ErrExit:
    return hr;
} // CMiniMdRW::Impl_GetStringW

//*****************************************************************************
// Get a column value from a row.  Signed types are sign-extended to the full
//  ULONG; unsigned types are 0-extended.
//*****************************************************************************
ULONG CMiniMdRW::GetCol(                // Column data.
    ULONG       ixTbl,                  // Index of the table.
    ULONG       ixCol,                  // Index of the column.
    void        *pvRecord)              // Record with the data.
{
    BYTE        *pRecord;               // The row.
    BYTE        *pData;                 // The item in the row.
    ULONG       val;                    // The return value.
    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column size, offset
    CMiniColDef *pColDef = &m_TableDefs[ixTbl].m_pColDefs[ixCol];

    pRecord = reinterpret_cast<BYTE*>(pvRecord);
    pData = pRecord + pColDef->m_oColumn;

    switch (pColDef->m_cbColumn)
    {
    case 1:
        val = *pData;
        break;
    case 2:
        if (pColDef->m_Type == iSHORT)
            val = static_cast<LONG>((INT16)GET_UNALIGNED_VAL16(pData));
        else
            val = GET_UNALIGNED_VAL16(pData);
        break;
    case 4:
        val = GET_UNALIGNED_VAL32(pData);
        break;
    default:
        _ASSERTE(!"Unexpected column size");
        return 0;
    }

    return val;
} // CMiniMdRW::GetCol

//*****************************************************************************
// General token column fetcher.
//*****************************************************************************
mdToken CMiniMdRW::GetToken(
    ULONG       ixTbl,                  // Index of the table.
    ULONG       ixCol,                  // Index of the column.
    void        *pvRecord)              // Record with the data.
{
    ULONG       tkn;                    // Token from the table.

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    CMiniColDef *pColDef = &m_TableDefs[ixTbl].m_pColDefs[ixCol];

    // Is the column just a RID?
    if (pColDef->m_Type <= iRidMax)
    {
        tkn = GetCol(ixTbl, ixCol, pvRecord); //pColDef, pvRecord, RidFromToken(tk));
        tkn = TokenFromRid(tkn, GetTokenForTable(pColDef->m_Type));
    }
    else // Is it a coded token?
    if (pColDef->m_Type <= iCodedTokenMax)
    {
        ULONG indexCodedToken = pColDef->m_Type - iCodedToken;
        if (indexCodedToken < ARRAY_SIZE(g_CodedTokens))
        {
            const CCodedTokenDef *pCdTkn = &g_CodedTokens[indexCodedToken];
            tkn = decodeToken(GetCol(ixTbl, ixCol, pvRecord), pCdTkn->m_pTokens, pCdTkn->m_cTokens);
        }
        else
        {
            _ASSERTE(!"GetToken called on unexpected coded token type");
            tkn = 0;
        }
    }
    else // It is an error.
    {
        _ASSERTE(!"GetToken called on unexpected column type");
        tkn = 0;
    }

    return tkn;
} // CMiniMdRW::GetToken

//*****************************************************************************
// Put a column value into a row.  The value is passed as a ULONG; 1, 2, or 4
//  bytes are stored into the column.  No table is specified, and the coldef
//  is passed directly.  This allows putting data into other buffers, such as
//  the temporary table used for saving.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutCol(              // S_OK or E_UNEXPECTED.
    CMiniColDef ColDef,         // The col def.
    void       *pvRecord,       // The row.
    ULONG       uVal)           // Value to put.
{
    HRESULT hr = S_OK;
    BYTE   *pRecord;    // The row.
    BYTE   *pData;      // The item in the row.

    pRecord = reinterpret_cast<BYTE*>(pvRecord);
    pData = pRecord + ColDef.m_oColumn;

    switch (ColDef.m_cbColumn)
    {
    case 1:
        // Don't store a value that would overflow.
        if (uVal > UCHAR_MAX)
            return E_INVALIDARG;
        *pData = static_cast<BYTE>(uVal);
        break;
    case 2:
        if (uVal > USHRT_MAX)
            return E_INVALIDARG;
        SET_UNALIGNED_VAL16(pData, uVal);
        break;
    case 4:
        SET_UNALIGNED_VAL32(pData, uVal);
        break;
    default:
        _ASSERTE(!"Unexpected column size");
        return E_UNEXPECTED;
    }

    return hr;
} // CMiniMdRW::PutCol

//*****************************************************************************
// Put a column value into a row.  The value is passed as a ULONG; 1, 2, or 4
//  bytes are stored into the column.
//*****************************************************************************

//*****************************************************************************
// Add a string to the string pool, and store the offset in the cell.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutString(   // S_OK or E_UNEXPECTED.
    ULONG  ixTbl,       // The table.
    ULONG  ixCol,       // The column.
    void  *pvRecord,    // The row.
    LPCSTR szString)    // Value to put.
{
    _ASSERTE(szString != NULL);

    HRESULT hr = S_OK;
    UINT32  nStringIndex = 0;

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    _ASSERTE(m_TableDefs[ixTbl].m_pColDefs[ixCol].m_Type == iSTRING);

    // <TODO>@FUTURE:  Set iOffset to 0 for empty string.  Work around the bug in
    // StringPool that does not handle empty strings correctly.</TODO>
    if (szString[0] == 0)
    {   // It's empty string
        nStringIndex = 0;
    }
    else
    {   // It's non-empty string
        IfFailGo(m_StringHeap.AddString(
            szString,
            &nStringIndex));
    }

    hr = PutCol(m_TableDefs[ixTbl].m_pColDefs[ixCol], pvRecord, nStringIndex);

    if (m_maxIx != UINT32_MAX)
    {
        IfFailGo(m_StringHeap.GetAlignedSize(&nStringIndex));
    }
    if (nStringIndex > m_maxIx)
    {
        m_maxIx = nStringIndex;
        if (m_maxIx > m_limIx && m_eGrow == eg_ok)
        {
            // OutputDebugStringA("Growing tables due to String overflow.\n");
            m_eGrow = eg_grow, m_maxRid = m_maxIx = UINT32_MAX;
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::PutString

//*****************************************************************************
// Add a string to the string pool, and store the offset in the cell.
// Returns: S_OK or E_UNEXPECTED.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutStringW(
    ULONG   ixTbl,      // The table.
    ULONG   ixCol,      // The column.
    void   *pvRecord,   // The row.
    LPCWSTR wszString)  // Value to put.
{
    _ASSERTE(wszString != NULL);

    HRESULT hr = S_OK;
    UINT32  nStringIndex = 0;   // The new string.

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    _ASSERTE(m_TableDefs[ixTbl].m_pColDefs[ixCol].m_Type == iSTRING);

    // Special case for empty string for StringPool
    if (wszString[0] == 0)
    {   // It's empty string
        // TODO: Is it OK that index 0 contains empty blob (00) and not empty string (00 01)?
        nStringIndex = 0;
    }
    else
    {   // It's non-empty string
        IfFailGo(m_StringHeap.AddStringW(
            wszString,
            &nStringIndex));
    }

    hr = PutCol(m_TableDefs[ixTbl].m_pColDefs[ixCol], pvRecord, nStringIndex);

    if (m_maxIx != UINT32_MAX)
    {
        IfFailGo(m_StringHeap.GetAlignedSize(&nStringIndex));
    }
    if (nStringIndex > m_maxIx)
    {
        m_maxIx = nStringIndex;
        if (m_maxIx > m_limIx && m_eGrow == eg_ok)
        {
            // OutputDebugStringA("Growing tables due to String overflow.\n");
            m_eGrow = eg_grow, m_maxRid = m_maxIx = UINT32_MAX;
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::PutStringW

//*****************************************************************************
// Add a guid to the guid pool, and store the index in the cell.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutGuid(     // S_OK or E_UNEXPECTED.
    ULONG   ixTbl,      // The table.
    ULONG   ixCol,      // The column.
    void   *pvRecord,   // The row.
    REFGUID guid)       // Value to put.
{
    HRESULT hr = S_OK;
    UINT32  nIndex;
    UINT32  cbSize = 0;

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    _ASSERTE(m_TableDefs[ixTbl].m_pColDefs[ixCol].m_Type == iGUID);

    IfFailGo(AddGuid(guid, &nIndex));

    hr = PutCol(m_TableDefs[ixTbl].m_pColDefs[ixCol], pvRecord, nIndex);

    if (m_maxIx != UINT32_MAX)
    {
        cbSize = m_GuidHeap.GetSize();
    }
    if (cbSize > m_maxIx)
    {
        m_maxIx = cbSize;
        if (m_maxIx > m_limIx && m_eGrow == eg_ok)
        {
            // OutputDebugStringA("Growing tables due to GUID overflow.\n");
            m_eGrow = eg_grow, m_maxRid = m_maxIx = UINT32_MAX;
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::PutGuid

//*****************************************************************************
// Normally, an MVID is randomly generated for every metadata.
// ChangeMvid() can be used to explicitly set it.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ChangeMvid(  // S_OK or E_UNEXPECTED.
    REFGUID newMvid)
{
    HRESULT hr = S_OK;

    ModuleRec *pModuleRec;
    IfFailRet(GetModuleRecord(1, &pModuleRec));
    UINT32 nGuidIndex = GetCol(TBL_Module, ModuleRec::COL_Mvid, pModuleRec);

    GUID UNALIGNED *pMvid;
    IfFailRet(m_GuidHeap.GetGuid(
        nGuidIndex,
        &pMvid));

    // Replace the GUID with new MVID.
    *pMvid = newMvid;
    // This was missing (probably because we don't test on platform with different bitness):
    //SwapGuid(pMvid);

    return hr;
} // CMiniMdRW::ChangeMvid

//*****************************************************************************
// Put a token into a cell.  If the column is a coded token, perform the
//  encoding first.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutToken(    // S_OK or E_UNEXPECTED.
    ULONG   ixTbl,      // The table.
    ULONG   ixCol,      // The column.
    void   *pvRecord,   // The row.
    mdToken tk)         // Value to put.
{
    HRESULT hr = S_OK;
    ULONG   cdTkn;      // The new coded token.

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    CMiniColDef ColDef = m_TableDefs[ixTbl].m_pColDefs[ixCol];

    // Is the column just a RID?
    if (ColDef.m_Type <= iRidMax)
        hr = PutCol(ColDef, pvRecord, RidFromToken(tk));
    else // Is it a coded token?
    if (ColDef.m_Type <= iCodedTokenMax)
    {
        ULONG indexCodedToken = ColDef.m_Type - iCodedToken;
        if (indexCodedToken < ARRAY_SIZE(g_CodedTokens))
        {
            const CCodedTokenDef *pCdTkn = &g_CodedTokens[indexCodedToken];
            cdTkn = encodeToken(RidFromToken(tk), TypeFromToken(tk), pCdTkn->m_pTokens, pCdTkn->m_cTokens);
            hr = PutCol(ColDef, pvRecord, cdTkn);
        }
        else
        {
            _ASSERTE(!"PutToken called on unexpected coded token type");
            hr = E_FAIL;
        }
    }
    else // It is an error.
    {
        _ASSERTE(!"PutToken called on unexpected column type");
    }

    return hr;
} // CMiniMdRW::PutToken

//*****************************************************************************
// Add a blob to the blob pool, and store the offset in the cell.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::PutBlob(
    ULONG       ixTbl,      // Table with the row.
    ULONG       ixCol,      // Column to set.
    void       *pvRecord,   // The row.
    const void *pvData,     // Blob data.
    ULONG       cbData)     // Size of the blob data.
{
    HRESULT hr = S_OK;
    UINT32  nBlobIndex;

    // Valid Table, Column, Row?
    _ASSERTE(ixTbl < m_TblCount);
    _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);

    // Column description.
    _ASSERTE(m_TableDefs[ixTbl].m_pColDefs[ixCol].m_Type == iBLOB);

    IfFailGo(m_BlobHeap.AddBlob(
        MetaData::DataBlob((BYTE *)pvData, cbData),
        &nBlobIndex));

    hr = PutCol(m_TableDefs[ixTbl].m_pColDefs[ixCol], pvRecord, nBlobIndex);

    if (m_maxIx != UINT32_MAX)
    {
        IfFailGo(m_BlobHeap.GetAlignedSize(&nBlobIndex));
    }
    if (nBlobIndex > m_maxIx)
    {
        m_maxIx = nBlobIndex;
        if (m_maxIx > m_limIx && m_eGrow == eg_ok)
        {
            // OutputDebugStringA("Growing tables due to Blob overflow.\n");
            m_eGrow = eg_grow, m_maxRid = m_maxIx = UINT32_MAX;
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::PutBlob

//*****************************************************************************
// Given a table with a pointer to another table, add a row in the second table
//  at the end of the range of rows belonging to some parent.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddChildRowIndirectForParent(
    ULONG  tblParent,       // Parent table.
    ULONG  colParent,       // Column in parent table.
    ULONG  tblChild,        // Child table, pointed to by parent cell.
    RID    ridParent,       // Rid of parent row.
    void **ppRow)
{
    HRESULT hr;
    ULONG   ixInsert;   // Index of new row.
    ULONG   i;          // Loop control.
    void   *pRow;       // A parent row.
    ULONG   ixChild;    // Some child record RID.

    // If the row in the parent table is the last row, just append.
    if (ridParent == GetCountRecs(tblParent))
    {
        RID nRowIndex_Ignore;
        return AddRecord(tblChild, ppRow, &nRowIndex_Ignore);
    }

    // Determine the index at which to insert a row.
    IfFailRet(getRow(tblParent, ridParent+1, &pRow));
    ixInsert = GetCol(tblParent, colParent, pRow);

    // Insert the row.
    IfFailRet(m_Tables[tblChild].InsertRecord(ixInsert, reinterpret_cast<BYTE **>(ppRow)));
    // Count the inserted record.
    ++m_Schema.m_cRecs[tblChild];

    if (m_Schema.m_cRecs[tblChild] > m_maxRid)
    {
        m_maxRid = m_Schema.m_cRecs[tblChild];
        if (m_maxRid > m_limRid && m_eGrow == eg_ok)
            m_eGrow = eg_grow, m_maxIx = m_maxRid = UINT32_MAX;
    }

    // Adjust the rest of the rows in the table.
    for (i=GetCountRecs(tblParent); i>ridParent; --i)
    {
        IfFailRet(getRow(tblParent, i, &pRow));
        ixChild = GetCol(tblParent, colParent, pRow);
        ++ixChild;
        IfFailRet(PutCol(tblParent, colParent, pRow, ixChild));
    }

    return S_OK;
} // CMiniMdRW::AddChildRowIndirectForParent

//*****************************************************************************
// Given a Parent and a Child, this routine figures if there needs to be an
// indirect table and creates it if needed.  Else it just update the pointers
// in the entries contained in the parent table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddChildRowDirectForParent(
    ULONG tblParent,    // Parent table.
    ULONG colParent,    // Column in parent table.
    ULONG tblChild,     // Child table, pointed to by parent cell.
    RID   ridParent)    // Rid of parent row.
{
    HRESULT hr = S_OK;
    void   *pRow;       // A row in the parent table.
    RID     ixChild;    // Rid of a child record.

    if (m_Schema.m_cRecs[tblChild-1] != 0)
    {
        // If there already exists an indirect table, just return.
        hr = S_FALSE;
        goto ErrExit;
    }

    // If the parent record has subsequent parent records with children,
    //  we will now need to build a pointer table.
    //
    // The canonical form of a child pointer in a parent record is to point to
    //  the start of the child list.  A record with no children will point
    //  to the same location as its subsequent record (that is, if A and B *could*
    //  have a child record, but only B *does*, both A and B will point to the
    //  same place.  If the last record in the parent table has no child records,
    //  it will point one past the end of the child table.  This is patterned
    //  after the STL's inclusive-BEGIN and exclusive-END.
    // This has the unfortunate side effect that if a child record is added to
    //  a parent not at the end of its table, *all* of the subsequent parent records
    //  will have to be updated to point to the new "1 past end of child table"
    //  location.
    // Therefore, as an optimization, we will also recognize a special marker,
    //  END_OF_TABLE (currently 0), to mean "past eot".
    //
    // If the child pointer of the record getting the new child is END_OF_TABLE,
    //  then there is no subsequent child pointer.  We need to fix up this parent
    //  record, and any previous parent records with END_OF_TABLE to point to the
    //  new child record.
    // If the child pointer of this parent record is not END_OF_TABLE, but the
    //  child pointer of the next parent record is, then there is nothing at
    //  all that needs to be done.
    // If the child pointer of the next parent record is not END_OF_TABLE, then
    //  we will have to build a pointer table.

    // Get the parent record, and see if its child pointer is END_OF_TABLE.  If so,
    //  fix the parent, and all previous END_OF_TABLE valued parent records.
    IfFailGo(getRow(tblParent, ridParent, &pRow));
    ixChild = GetCol(tblParent, colParent, pRow);
    if (ixChild == END_OF_TABLE)
    {
        IfFailGo(ConvertMarkerToEndOfTable(tblParent, colParent, m_Schema.m_cRecs[tblChild], ridParent));
        goto ErrExit;
    }

    // The parent did not have END_OF_TABLE for its child pointer.  If it was the last
    //  record in the table, there is nothing more to do.
    if (ridParent == m_Schema.m_cRecs[tblParent])
        goto ErrExit;

    // The parent didn't have END_OF_TABLE, and there are more rows in parent table.
    //  If the next parent record's child pointer is END_OF_TABLE, then all of the
    //  remaining records are OK.
    IfFailGo(getRow(tblParent, ridParent+1, &pRow));
    ixChild = GetCol(tblParent, colParent, pRow);
    if (ixChild == END_OF_TABLE)
        goto ErrExit;

    // The next record was not END_OF_TABLE, so some adjustment will be required.
    //  If it points to the actual END of the table, there are no more child records
    //  and the child pointers can be adjusted to the new END of the table.
    if (ixChild == m_Schema.m_cRecs[tblChild])
    {
        for (ULONG i=m_Schema.m_cRecs[tblParent]; i>ridParent; --i)
        {
            IfFailGo(getRow(tblParent, i, &pRow));
            IfFailGo(PutCol(tblParent, colParent, pRow, ixChild+1));
        }
        goto ErrExit;
    }

    // The next record contained a pointer to some actual child data.  That means that
    //  this is an out-of-order insertion.  We must create an indirect table.
    // Convert any END_OF_TABLE to actual END of table value.  Note that a record has
    //  just been added to the child table, and not yet to the parent table, so the END
    //  should currently point to the last valid record (instead of the usual first invalid
    //  rid).
    IfFailGo(ConvertMarkerToEndOfTable(tblParent, colParent, m_Schema.m_cRecs[tblChild], m_Schema.m_cRecs[tblParent]));
    // Create the indirect table.
    IfFailGo(CreateIndirectTable(tblChild));
    hr = S_FALSE;

ErrExit:
    return hr;
} // CMiniMdRW::AddChildRowDirectForParent

//*****************************************************************************
// Starting with some location, convert special END_OF_TABLE values into
//  actual end of table values (count of records + 1).
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ConvertMarkerToEndOfTable(
    ULONG tblParent,    // Parent table to convert.
    ULONG colParent,    // Column in parent table.
    ULONG ixEnd,        // Value to store to child pointer.
    RID   ridParent)    // Rid of parent row to start with (work down).
{
    HRESULT hr;
    void   *pRow;       // A row in the parent table.
    RID     ixChild;    // Rid of a child record.

    for (; ridParent > 0; --ridParent)
    {
        IfFailGo(getRow(tblParent, ridParent, &pRow));
        ixChild = GetCol(tblParent, colParent, pRow);
        // Finished when rows no longer have special value.
        if (ixChild != END_OF_TABLE)
            break;
        IfFailGo(PutCol(tblParent, colParent, pRow, ixEnd));
    }
    // Success.
    hr = S_OK;

ErrExit:
    return hr;
} // CMiniMdRW::ConvertMarkerToEndOfTable

//*****************************************************************************
// Given a Table ID this routine creates the corresponding pointer table with
// the entries in the given Table ID less one.  It doesn't create the last
// entry by default, since its the last entry that caused the Indirect table to
// be required in most cases and will need to inserted at the appropriate location
// with AddChildRowIndirectForParent() function.  So, be VERY CAREFUL when using this function!
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CreateIndirectTable(
    ULONG ixTbl,                    // Given Table.
    BOOL  bOneLess /* = true */)    // if true, create one entry less.
{
    void   *pRecord;
    ULONG   cRecords;
    HRESULT hr = S_OK;

    if (m_OptionValue.m_ErrorIfEmitOutOfOrder)
    {
        //<TODO> Can we use some bit fields and reduce the code size here??
        //</TODO>
        if (ixTbl == TBL_Field && ( m_OptionValue.m_ErrorIfEmitOutOfOrder & MDFieldOutOfOrder ) )
        {
            _ASSERTE(!"Out of order emit of field token!");
            return CLDB_E_RECORD_OUTOFORDER;
        }
        else if (ixTbl == TBL_Method && ( m_OptionValue.m_ErrorIfEmitOutOfOrder & MDMethodOutOfOrder ) )
        {
            _ASSERTE(!"Out of order emit of method token!");
            return CLDB_E_RECORD_OUTOFORDER;
        }
        else if (ixTbl == TBL_Param && ( m_OptionValue.m_ErrorIfEmitOutOfOrder & MDParamOutOfOrder ) )
        {
            _ASSERTE(!"Out of order emit of param token!");
            return CLDB_E_RECORD_OUTOFORDER;
        }
        else if (ixTbl == TBL_Property && ( m_OptionValue.m_ErrorIfEmitOutOfOrder & MDPropertyOutOfOrder ) )
        {
            _ASSERTE(!"Out of order emit of property token!");
            return CLDB_E_RECORD_OUTOFORDER;
        }
        else if (ixTbl == TBL_Event && ( m_OptionValue.m_ErrorIfEmitOutOfOrder & MDEventOutOfOrder ) )
        {
            _ASSERTE(!"Out of order emit of event token!");
            return CLDB_E_RECORD_OUTOFORDER;
        }
    }

    _ASSERTE(! HasIndirectTable(ixTbl));

    cRecords = GetCountRecs(ixTbl);
    if (bOneLess)
        cRecords--;

    // Create one less than the number of records in the given table.
    for (ULONG i = 1; i <= cRecords ; i++)
    {
        RID nRowIndex_Ignore;
        IfFailGo(AddRecord(g_PtrTableIxs[ixTbl].m_ixtbl, &pRecord, &nRowIndex_Ignore));
        IfFailGo(PutCol(g_PtrTableIxs[ixTbl].m_ixtbl, g_PtrTableIxs[ixTbl].m_ixcol, pRecord, i));
    }
ErrExit:
    return hr;
} // CMiniMdRW::CreateIndirectTable

//---------------------------------------------------------------------------------------
//
// The new paramter may not have been emitted in sequence order.  So
// check the current parameter and move it up in the indirect table until
// we find the right home.
//
__checkReturn
HRESULT
CMiniMdRW::FixParamSequence(
    RID md)     // Rid of method with new parameter.
{
    HRESULT     hr;
    MethodRec * pMethod;
    IfFailRet(GetMethodRecord(md, &pMethod));
    RID ixStart = getParamListOfMethod(pMethod);
    RID ixEnd;
    IfFailRet(getEndParamListOfMethod(md, &ixEnd));
    int iSlots = 0;

    // Param table should not be empty at this point.
    _ASSERTE(ixEnd > ixStart);

    // Get a pointer to the new parameter.
    RID ridNew;
    ParamPtrRec * pNewParamPtr = NULL;
    if (HasIndirectTable(TBL_Param))
    {
        IfFailRet(GetParamPtrRecord(--ixEnd, &pNewParamPtr));
        ridNew = GetCol(TBL_ParamPtr, ParamPtrRec::COL_Param, pNewParamPtr);
    }
    else
    {
        ridNew = --ixEnd;
    }

    ParamRec * pNewParam;
    IfFailRet(GetParamRecord(ridNew, &pNewParam));

    // Walk the list forward looking for the insert point.
    for (; ixStart < ixEnd; --ixEnd)
    {
        // Get the current parameter record.
        RID ridOld;
        if (HasIndirectTable(TBL_Param))
        {
            ParamPtrRec * pParamPtr;
            IfFailRet(GetParamPtrRecord(ixEnd - 1, &pParamPtr));
            ridOld = GetCol(TBL_ParamPtr, ParamPtrRec::COL_Param, pParamPtr);
        }
        else
        {
            ridOld = ixEnd - 1;
        }

        ParamRec * pParamRec;
        IfFailRet(GetParamRecord(ridOld, &pParamRec));

        // If the new record belongs before this existing record, slide
        // all of the old stuff down.
        if (pNewParam->GetSequence() < pParamRec->GetSequence())
        {
            ++iSlots;
        }
        else
        {
            break;
        }
    }

    // If the item is out of order, move everything down one slot and
    // copy the new parameter into the new location.  Because the heap can be
    // split, this must be done carefully.
    //<TODO>@Future: one could write a more complicated but faster routine that
    // copies blocks within heaps.</TODO>
    if (iSlots)
    {
        RID endRid;
        // Create an indirect table if there isn't one already.  This is because
        // we can't change tokens that have been handed out, in this case the
        // param tokens.
        if (!HasIndirectTable(TBL_Param))
        {
            IfFailRet(CreateIndirectTable(TBL_Param, false));
            IfFailRet(getEndParamListOfMethod(md, &endRid));
            IfFailRet(GetParamPtrRecord(endRid - 1, &pNewParamPtr));
        }
        int cbCopy = m_TableDefs[TBL_ParamPtr].m_cbRec;
        void * pbBackup = _alloca(cbCopy);
        memcpy(pbBackup, pNewParamPtr, cbCopy);

        IfFailRet(getEndParamListOfMethod(md, &endRid));
        for (ixEnd = endRid - 1;  iSlots;  iSlots--, --ixEnd)
        {
            ParamPtrRec * pTo;
            IfFailRet(GetParamPtrRecord(ixEnd, &pTo));
            ParamPtrRec * pFrom;
            IfFailRet(GetParamPtrRecord(ixEnd - 1, &pFrom));
            memcpy(pTo, pFrom, cbCopy);
        }

        ParamPtrRec * pTo;
        IfFailRet(GetParamPtrRecord(ixEnd, &pTo));
        memcpy(pTo, pbBackup, cbCopy);
    }
    return S_OK;
} // CMiniMdRW::FixParamSequence

//---------------------------------------------------------------------------------------
//
// Given a MethodDef and its parent TypeDef, add the MethodDef to the parent,
//  adjusting the MethodPtr table if it exists or if it needs to be created.
//
__checkReturn
HRESULT
CMiniMdRW::AddMethodToTypeDef(
    RID td,     // The TypeDef to which to add the Method.
    RID md)     // MethodDef to add to TypeDef.
{
    HRESULT hr;
    void *  pPtr;

    // Add direct if possible.
    IfFailGo(AddChildRowDirectForParent(TBL_TypeDef, TypeDefRec::COL_MethodList, TBL_Method, td));

    // If couldn't add direct...
    if (hr == S_FALSE)
    {   // Add indirect.
        IfFailGo(AddChildRowIndirectForParent(TBL_TypeDef, TypeDefRec::COL_MethodList, TBL_MethodPtr, td, &pPtr));
        hr = PutCol(TBL_MethodPtr, MethodPtrRec::COL_Method, pPtr, md);

        // Add the <md, td> to the method parent lookup table.
        IfFailGo(AddMethodToLookUpTable(TokenFromRid(md, mdtMethodDef), td) );
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddMethodToTypeDef

//*****************************************************************************
// Given a FieldDef and its parent TypeDef, add the FieldDef to the parent,
//  adjusting the FieldPtr table if it exists or if it needs to be created.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddFieldToTypeDef(
    RID td,     // The TypeDef to which to add the Field.
    RID md)     // FieldDef to add to TypeDef.
{
    HRESULT hr;
    void   *pPtr;

    // Add direct if possible.
    IfFailGo(AddChildRowDirectForParent(TBL_TypeDef, TypeDefRec::COL_FieldList, TBL_Field, td));

    // If couldn't add direct...
    if (hr == S_FALSE)
    {   // Add indirect.
        IfFailGo(AddChildRowIndirectForParent(TBL_TypeDef, TypeDefRec::COL_FieldList, TBL_FieldPtr, td, &pPtr));
        hr = PutCol(TBL_FieldPtr, FieldPtrRec::COL_Field, pPtr, md);

        // Add the <md, td> to the field parent lookup table.
        IfFailGo(AddFieldToLookUpTable(TokenFromRid(md, mdtFieldDef), td));
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddFieldToTypeDef

//*****************************************************************************
// Given a Param and its parent Method, add the Param to the parent,
// adjusting the ParamPtr table if there is an indirect table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddParamToMethod(
    RID md,     // The MethodDef to which to add the Param.
    RID pd)     // Param to add to MethodDef.
{
    HRESULT hr;
    void   *pPtr;

    IfFailGo(AddChildRowDirectForParent(TBL_Method, MethodRec::COL_ParamList, TBL_Param, md));
    if (hr == S_FALSE)
    {
        IfFailGo(AddChildRowIndirectForParent(TBL_Method, MethodRec::COL_ParamList, TBL_ParamPtr, md, &pPtr));
        IfFailGo(PutCol(TBL_ParamPtr, ParamPtrRec::COL_Param, pPtr, pd));

        // Add the <pd, md> to the field parent lookup table.
        IfFailGo(AddParamToLookUpTable(TokenFromRid(pd, mdtParamDef), md));
    }
    IfFailGo(FixParamSequence(md));

ErrExit:
    return hr;
} // CMiniMdRW::AddParamToMethod

//*****************************************************************************
// Given a Property and its parent PropertyMap, add the Property to the parent,
// adjusting the PropertyPtr table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddPropertyToPropertyMap(
    RID pmd,    // The PropertyMap to which to add the Property.
    RID pd)     // Property to add to PropertyMap.
{
    HRESULT hr;
    void   *pPtr;

    IfFailGo(AddChildRowDirectForParent(TBL_PropertyMap, PropertyMapRec::COL_PropertyList,
                                    TBL_Property, pmd));
    if (hr == S_FALSE)
    {
        IfFailGo(AddChildRowIndirectForParent(TBL_PropertyMap, PropertyMapRec::COL_PropertyList,
                                        TBL_PropertyPtr, pmd, &pPtr));
        hr = PutCol(TBL_PropertyPtr, PropertyPtrRec::COL_Property, pPtr, pd);
    }


ErrExit:
    return hr;
} // CMiniMdRW::AddPropertyToPropertyMap

//*****************************************************************************
// Given a Event and its parent EventMap, add the Event to the parent,
// adjusting the EventPtr table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddEventToEventMap(
    ULONG emd,      // The EventMap to which to add the Event.
    RID   ed)       // Event to add to EventMap.
{
    HRESULT hr;
    void   *pPtr;

    IfFailGo(AddChildRowDirectForParent(TBL_EventMap, EventMapRec::COL_EventList,
                                    TBL_Event, emd));
    if (hr == S_FALSE)
    {
        IfFailGo(AddChildRowIndirectForParent(TBL_EventMap, EventMapRec::COL_EventList,
                                        TBL_EventPtr, emd, &pPtr));
        hr = PutCol(TBL_EventPtr, EventPtrRec::COL_Event, pPtr, ed);
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddEventToEventMap

//*****************************************************************************
// Find helper for a constant. This will trigger constant table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindConstantHelper(      // return index to the constant table
    mdToken tkParent,           // Parent token.
    RID    *pFoundRid)
{
    _ASSERTE(TypeFromToken(tkParent) != 0);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_Constant))
    {
        return FindConstantFor(RidFromToken(tkParent), TypeFromToken(tkParent), pFoundRid);
    }
    return GenericFindWithHash(TBL_Constant, ConstantRec::COL_Parent, tkParent, pFoundRid);
} // CMiniMdRW::FindConstantHelper

//*****************************************************************************
// Find helper for a FieldMarshal. This will trigger FieldMarshal table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindFieldMarshalHelper(  // return index to the field marshal table
    mdToken tkParent,               // Parent token. Can be a FieldDef or ParamDef.
    RID    *pFoundRid)
{
    _ASSERTE(TypeFromToken(tkParent) != 0);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_FieldMarshal))
    {
        return FindFieldMarshalFor(RidFromToken(tkParent), TypeFromToken(tkParent), pFoundRid);
    }
    return GenericFindWithHash(TBL_FieldMarshal, FieldMarshalRec::COL_Parent, tkParent, pFoundRid);
} // CMiniMdRW::FindFieldMarshalHelper


//*****************************************************************************
// Find helper for a method semantics.
// This will look up methodsemantics based on its status!
// Can return out of memory error because of the enumerator.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindMethodSemanticsHelper(
    mdToken        tkAssociate,     // Event or property token
    HENUMInternal *phEnum)          // fill in the enum
{
    RID          ridStart, ridEnd;
    RID          index;
    MethodSemanticsRec *pMethodSemantics;
    HRESULT      hr = NOERROR;
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_MethodSemantics];

    _ASSERTE(TypeFromToken(tkAssociate) != 0);

    if (IsSorted(TBL_MethodSemantics))
    {
        IfFailGo(getAssociatesForToken(tkAssociate, &ridEnd, &ridStart));
        HENUMInternal::InitSimpleEnum(0, ridStart, ridEnd, phEnum);
    }
    else if (pHashTable)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        iHash = HashToken(tkAssociate);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(GetMethodSemanticsRecord(p->tok, &pMethodSemantics));
            if (getAssociationOfMethodSemantics(pMethodSemantics) == tkAssociate)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, p->tok) );
            }
        }
    }
    else
    {
        // linear search
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        for (index = 1; index <= getCountMethodSemantics(); index++)
        {
            IfFailGo(GetMethodSemanticsRecord(index, &pMethodSemantics));
            if (getAssociationOfMethodSemantics(pMethodSemantics) == tkAssociate)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, index) );
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::FindMethodSemanticsHelper


//*****************************************************************************
// Find helper for a method semantics given a associate and semantics.
// This will look up methodsemantics based on its status!
// Return CLDB_E_RECORD_NOTFOUND if cannot find the matching one
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindAssociateHelper(
    mdToken tkAssociate,    // Event or property token
    DWORD   dwSemantics,    // [IN] given a associate semantics(setter, getter, testdefault, reset)
    RID    *pRid)           // [OUT] return matching row index here
{
    RID         ridStart, ridEnd;
    RID         index;
    MethodSemanticsRec *pMethodSemantics;
    HRESULT     hr = NOERROR;
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_MethodSemantics];

    _ASSERTE(TypeFromToken(tkAssociate) != 0);

    if (pHashTable)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        iHash = HashToken(tkAssociate);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(GetMethodSemanticsRecord(p->tok, &pMethodSemantics));
            if (pMethodSemantics->GetSemantic() == dwSemantics && getAssociationOfMethodSemantics(pMethodSemantics) == tkAssociate)
            {
                *pRid = p->tok;
                goto ErrExit;
            }
        }
    }
    else
    {
        if (IsSorted(TBL_MethodSemantics))
        {
            IfFailGo(getAssociatesForToken(tkAssociate, &ridEnd, &ridStart));
        }
        else
        {
            ridStart = 1;
            ridEnd = getCountMethodSemantics() + 1;
        }

        for (index = ridStart; index < ridEnd ; index++)
        {
            IfFailGo(GetMethodSemanticsRecord(index, &pMethodSemantics));
            if (pMethodSemantics->GetSemantic() == dwSemantics && getAssociationOfMethodSemantics(pMethodSemantics) == tkAssociate)
            {
                *pRid = index;
                goto ErrExit;
            }
        }
    }
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // CMiniMdRW::FindAssociateHelper


//*****************************************************************************
// Find helper for a MethodImpl.
// This will trigger MethodImpl table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindMethodImplHelper(
    mdTypeDef      td,          // TypeDef token for the Class.
    HENUMInternal *phEnum)      // fill in the enum
{
    RID         ridStart, ridEnd;
    RID         index;
    MethodImplRec *pMethodImpl;
    HRESULT     hr = NOERROR;
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_MethodImpl];

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);

    if (IsSorted(TBL_MethodImpl))
    {
        IfFailGo(getMethodImplsForClass(RidFromToken(td), &ridEnd, &ridStart));
        HENUMInternal::InitSimpleEnum(0, ridStart, ridEnd, phEnum);
    }
    else if (pHashTable)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        iHash = HashToken(td);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(GetMethodImplRecord(p->tok, &pMethodImpl));
            if (getClassOfMethodImpl(pMethodImpl) == td)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, p->tok) );
            }
        }
    }
    else
    {
        // linear search
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        for (index = 1; index <= getCountMethodImpls(); index++)
        {
            IfFailGo(GetMethodImplRecord(index, &pMethodImpl));
            if (getClassOfMethodImpl(pMethodImpl) == td)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, index) );
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::FindMethodImplHelper


//*****************************************************************************
// Find helper for a GenericParam.
// This will trigger GenericParam table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindGenericParamHelper(
    mdToken        tkOwner,     // Token for the GenericParams' owner.
    HENUMInternal *phEnum)      // fill in the enum
{
    HRESULT     hr = NOERROR;
    RID         ridStart, ridEnd;       // Start, end of range of tokens.
    RID         index;                  // A loop counter.
    GenericParamRec *pGenericParam;
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_GenericParam];

    if (IsSorted(TBL_GenericParam))
    {
        mdToken tk;
        tk = encodeToken(RidFromToken(tkOwner), TypeFromToken(tkOwner), mdtTypeOrMethodDef, ARRAY_SIZE(mdtTypeOrMethodDef));
        IfFailGo(SearchTableForMultipleRows(TBL_GenericParam,
                            _COLDEF(GenericParam,Owner),
                            tk,
                            &ridEnd,
                            &ridStart));
        HENUMInternal::InitSimpleEnum(mdtGenericParam, ridStart, ridEnd, phEnum);
    }
    else if (pHashTable)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        iHash = HashToken(tkOwner);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(GetGenericParamRecord(p->tok, &pGenericParam));
            if (getOwnerOfGenericParam(pGenericParam) == tkOwner)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, TokenFromRid(p->tok, mdtGenericParam)) );
            }
        }
    }
    else
    {
        // linear search
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        for (index = 1; index <= getCountGenericParams(); index++)
        {
            IfFailGo(GetGenericParamRecord(index, &pGenericParam));
            if (getOwnerOfGenericParam(pGenericParam) == tkOwner)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, TokenFromRid(index, mdtGenericParam)) );
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::FindGenericParamHelper


//*****************************************************************************
// Find helper for a GenericParamConstraint.
// This will trigger GenericParamConstraint table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindGenericParamConstraintHelper(
    mdGenericParam tkParam,     // Token for the GenericParam
    HENUMInternal *phEnum)      // fill in the enum
{
    HRESULT     hr = NOERROR;
    RID         ridStart, ridEnd;       // Start, end of range of tokens.
    ULONG       index;                  // A loop counter.
    GenericParamConstraintRec *pConstraint;
    CLookUpHash *pHashTable = m_pLookUpHashs[TBL_GenericParamConstraint];
    RID         ridParam = RidFromToken(tkParam);
    _ASSERTE(TypeFromToken(tkParam) == mdtGenericParam);

    // Extract the rid part of the token for comparison below.  Be sure
    //  that the column is a RID column, so that getGPCFGP() returns a RID.
    _ASSERTE(IsRidType(m_TableDefs[TBL_GenericParamConstraint].m_pColDefs[GenericParamConstraintRec::COL_Owner].m_Type));

    if (IsSorted(TBL_GenericParamConstraint))
    {
        IfFailGo(getGenericParamConstraintsForGenericParam(ridParam, &ridEnd, &ridStart));
        HENUMInternal::InitSimpleEnum(mdtGenericParamConstraint, ridStart, ridEnd, phEnum);
    }
    else if (pHashTable)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        iHash = HashToken(tkParam);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailGo(GetGenericParamConstraintRecord(p->tok, &pConstraint));
            if (getOwnerOfGenericParamConstraint(pConstraint) == tkParam)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, TokenFromRid(p->tok, mdtGenericParamConstraint)) );
            }
        }
    }
    else
    {
        // linear search
        HENUMInternal::InitDynamicArrayEnum(phEnum);
        for (index = 1; index <= getCountGenericParamConstraints(); index++)
        {
            IfFailGo(GetGenericParamConstraintRecord(index, &pConstraint));
            if (getOwnerOfGenericParamConstraint(pConstraint) == tkParam)
            {
                IfFailGo( HENUMInternal::AddElementToEnum(phEnum, TokenFromRid(index, mdtGenericParamConstraint)) );
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::FindGenericParamConstraintHelper


//*****************************************************************************
// Find helper for a ClassLayout. This will trigger ClassLayout table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindClassLayoutHelper(   // return index to the ClassLayout table
    mdTypeDef tkParent,             // Parent token.
    RID      *pFoundRid)
{
    _ASSERTE(TypeFromToken(tkParent) == mdtTypeDef);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_ClassLayout))
    {
        return FindClassLayoutFor(RidFromToken(tkParent), pFoundRid);
    }
    return GenericFindWithHash(TBL_ClassLayout, ClassLayoutRec::COL_Parent, tkParent, pFoundRid);
} // CMiniMdRW::FindClassLayoutHelper

//*****************************************************************************
// Find helper for a FieldLayout. This will trigger FieldLayout table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindFieldLayoutHelper(   // return index to the FieldLayout table
    mdFieldDef tkField,             // Field RID.
    RID       *pFoundRid)
{
    _ASSERTE(TypeFromToken(tkField) == mdtFieldDef);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_FieldLayout))
    {
        return FindFieldLayoutFor(RidFromToken(tkField), pFoundRid);
    }
    return GenericFindWithHash(TBL_FieldLayout, FieldLayoutRec::COL_Field, tkField, pFoundRid);
} // CMiniMdRW::FindFieldLayoutHelper

//*****************************************************************************
// Find helper for a ImplMap. This will trigger ImplMap table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindImplMapHelper(       // return index to the ImplMap table
    mdToken tk,                     // Member forwarded token.
    RID    *pFoundRid)
{
    _ASSERTE(TypeFromToken(tk) != 0);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_ImplMap))
    {
        return FindImplMapFor(RidFromToken(tk), TypeFromToken(tk), pFoundRid);
    }
    return GenericFindWithHash(TBL_ImplMap, ImplMapRec::COL_MemberForwarded, tk, pFoundRid);
} // CMiniMdRW::FindImplMapHelper


//*****************************************************************************
// Find helper for a FieldRVA. This will trigger FieldRVA table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindFieldRVAHelper(      // return index to the FieldRVA table
    mdFieldDef tkField,             // Field token.
    RID       *pFoundRid)
{
    _ASSERTE(TypeFromToken(tkField) == mdtFieldDef);

    // If sorted, use the faster lookup
    if (IsSorted(TBL_FieldRVA))
    {
        return FindFieldRVAFor(RidFromToken(tkField), pFoundRid);
    }
    return GenericFindWithHash(TBL_FieldRVA, FieldRVARec::COL_Field, tkField, pFoundRid);
} // CMiniMdRW::FindFieldRVAHelper

//*****************************************************************************
// Find helper for a NestedClass. This will trigger NestedClass table to be sorted if it is not.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindNestedClassHelper(   // return index to the NestedClass table
    mdTypeDef tkClass,                // NestedClass RID.
    RID      *pFoundRid)
{
    // If sorted, use the faster lookup
     if (IsSorted(TBL_NestedClass))
    {
        return FindNestedClassFor(RidFromToken(tkClass), pFoundRid);
    }
    return GenericFindWithHash(TBL_NestedClass, NestedClassRec::COL_NestedClass, tkClass, pFoundRid);
} // CMiniMdRW::FindNestedClassHelper


//*************************************************************************
// generic find helper with hash table
//*************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GenericFindWithHash(     // Return code.
    ULONG       ixTbl,                  // Table with hash
    ULONG       ixCol,                  // col that we hash.
    mdToken     tkTarget,               // token to be find in the hash
    RID        *pFoundRid)
{
    HRESULT hr;
    ULONG   index;
    mdToken tkHash;
    BYTE *  pRec;

    // Partial check -- only one rid for table 0, so if type is 0, rid should be 1.
    _ASSERTE(TypeFromToken(tkTarget) != 0 || RidFromToken(tkTarget) == 1);

    if (m_pLookUpHashs[ixTbl] == NULL)
    {
        // Just ignore the returned error - the hash is either created or not
        (void)GenericBuildHashTable(ixTbl, ixCol);
    }

    CLookUpHash * pHashTable = m_pLookUpHashs[ixTbl];
    if (pHashTable != NULL)
    {
        TOKENHASHENTRY *p;
        ULONG       iHash;
        int         pos;

        // Hash the data.
        iHash = HashToken(tkTarget);

        // Go through every entry in the hash chain looking for ours.
        for (p = pHashTable->FindFirst(iHash, pos);
             p;
             p = pHashTable->FindNext(pos))
        {
            IfFailRet(m_Tables[ixTbl].GetRecord(p->tok, &pRec));

            // get the column value that we will hash
            tkHash = GetToken(ixTbl, ixCol, pRec);
            if (tkHash == tkTarget)
            {
                // found the match
                *pFoundRid = p->tok;
                return S_OK;
            }
        }
    }
    else
    {
        // linear search
        for (index = 1; index <= GetCountRecs(ixTbl); index++)
        {
            IfFailRet(m_Tables[ixTbl].GetRecord(index, &pRec));
            tkHash = GetToken(ixTbl, ixCol, pRec);
            if (tkHash == tkTarget)
            {
                // found the match
                *pFoundRid = index;
                return S_OK;
            }
        }
    }
    *pFoundRid = 0;
    return S_OK;
} // CMiniMdRW::GenericFindWithHash

//*************************************************************************
// Build a hash table for the specified table if the size exceed the thresholds.
//*************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GenericBuildHashTable(
    ULONG ixTbl,    // Table with hash.
    ULONG ixCol)    // Column that we hash.
{
    HRESULT         hr = S_OK;
    BYTE           *pRec;
    mdToken         tkHash;
    ULONG           iHash;
    TOKENHASHENTRY *pEntry;

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (m_pLookUpHashs[ixTbl] == NULL)
    {
        ULONG ridEnd = GetCountRecs(ixTbl);

        //<TODO>@FUTURE: we need to init the size of the hash table corresponding to the current
        // size of table in E&C's case.
        //</TODO>
        // Avoid prefast warning with "if (ridEnd + 1 > INDEX_ROW_COUNT_THRESHOLD)"
        if (ridEnd > INDEX_ROW_COUNT_THRESHOLD - 1)
        {
            // Create a new hash.
            NewHolder<CLookUpHash> pHashTable = new (nothrow) CLookUpHash;
            IfNullGo(pHashTable);
            IfFailGo(pHashTable->NewInit(
                g_HashSize[GetMetaDataSizeIndex(&m_OptionValue)]));

            // Scan every entry already in the table, add it to the hash.
            for (ULONG index = 1; index <= ridEnd; index++)
            {
                IfFailGo(m_Tables[ixTbl].GetRecord(index, &pRec));

                // get the column value that we will hash
                tkHash = GetToken(ixTbl, ixCol, pRec);

                // hash the value
                iHash = HashToken(tkHash);

                pEntry = pHashTable->Add(iHash);
                IfNullGo(pEntry);
                pEntry->tok = index;

            }

            if (InterlockedCompareExchangeT<CLookUpHash *>(
                &m_pLookUpHashs[ixTbl],
                pHashTable,
                NULL) == NULL)
            {   // We won the initializaion race
                pHashTable.SuppressRelease();
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::GenericBuildHashTable

//*************************************************************************
// Add a rid from a table into a hash. We will hash on the ixCol of the ixTbl.
//*************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GenericAddToHash(
    ULONG ixTbl,    // Table with hash
    ULONG ixCol,    // column that we hash by calling HashToken.
    RID   rid)      // Token of new guy into the ixTbl.
{
    HRESULT         hr = S_OK;
    CLookUpHash    *pHashTable = m_pLookUpHashs[ixTbl];
    void           *pRec;
    mdToken         tkHash;
    ULONG           iHash;
    TOKENHASHENTRY *pEntry;

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (pHashTable == NULL)
    {
        IfFailGo(GenericBuildHashTable(ixTbl, ixCol));
    }
    else
    {
        // Adding into hash table has to be protected by write-lock
        INDEBUG(Debug_CheckIsLockedForWrite();)

        IfFailGo(m_Tables[ixTbl].GetRecord(rid, reinterpret_cast<BYTE **>(&pRec)));

        tkHash = GetToken(ixTbl, ixCol, pRec);
        iHash = HashToken(tkHash);
        pEntry = pHashTable->Add(iHash);
        IfNullGo(pEntry);
        pEntry->tok = rid;
    }

ErrExit:
    return hr;
} // CMiniMdRW::GenericAddToHash


//*****************************************************************************
// look up a table by a col given col value is ulVal.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::LookUpTableByCol(
    ULONG        ulVal,         // Value for which to search.
    VirtualSort *pVSTable,      // A VirtualSort on the table, if any.
    RID         *pRidStart,     // Put RID of first match here.
    RID         *pRidEnd)       // [OPTIONAL] Put RID of end match here.
{
    HRESULT hr = NOERROR;
    ULONG   ixTbl;
    ULONG   ixCol;

    _ASSERTE(pVSTable != NULL);
    ixTbl = pVSTable->m_ixTbl;
    ixCol = pVSTable->m_ixCol;
    if (IsSorted(ixTbl))
    {
        // Table itself is sorted so we don't need to build a virtual sort table.
        // Binary search on the table directly.
        //
        IfFailGo(SearchTableForMultipleRows(
            ixTbl,
            m_TableDefs[ixTbl].m_pColDefs[ixCol],
            ulVal,
            pRidEnd,
            pRidStart));
    }
    else
    {
        if (!pVSTable->m_isMapValid)
        {
            INDEBUG(Debug_CheckIsLockedForWrite();)

            int iCount;

            // build the parallel VirtualSort table
            if (pVSTable->m_pMap == NULL)
            {
                // the first time that we build the VS table. We need to allocate the TOKENMAP
                pVSTable->m_pMap = new (nothrow) TOKENMAP;
                IfNullGo(pVSTable->m_pMap);
            }

            // ensure the look up table is big enough
            iCount = pVSTable->m_pMap->Count();
            if (pVSTable->m_pMap->AllocateBlock(m_Schema.m_cRecs[ixTbl] + 1 - iCount) == 0)
            {
                IfFailGo(E_OUTOFMEMORY);
            }

            // now build the table
            // Element 0 of m_pMap will never be used, its just being initialized anyway.
            for (ULONG i = 0; i <= m_Schema.m_cRecs[ixTbl]; i++)
            {
                *(pVSTable->m_pMap->Get(i)) = i;
            }
            // sort the table
            IfFailGo(pVSTable->Sort());
        }
        // binary search on the LookUp
        {
            void       *pRow;                   // Row from a table.
            ULONG       val;                    // Value from a row.
            CMiniColDef *pCol;
            int         lo,hi,mid=0;            // binary search indices.
            RID         ridEnd, ridBegin;

            pCol = m_TableDefs[ixTbl].m_pColDefs;

            // Start with entire table.
            lo = 1;
            hi = GetCountRecs( ixTbl );
            // While there are rows in the range...
            while ( lo <= hi )
            {   // Look at the one in the middle.
                mid = (lo + hi) / 2;
                IfFailGo(getRow(
                    ixTbl,
                    (UINT32)*(pVSTable->m_pMap->Get(mid)),
                    &pRow));
                val = getIX( pRow, pCol[ixCol] );

                // If equal to the target, done.
                if ( val == ulVal )
                    break;
                // If middle item is too small, search the top half.
                if ( val < ulVal )
                    lo = mid + 1;
                else // but if middle is to big, search bottom half.
                    hi = mid - 1;
            }
            if ( lo > hi )
            {
                // Didn't find anything that matched.
                *pRidStart = 0;
                if (pRidEnd) *pRidEnd = 0;
                goto ErrExit;
            }


            // Now mid is pointing to one of the several records that match the search.
            // Find the beginning and find the end.
            ridBegin = mid;

            // End will be at least one larger than found record.
            ridEnd = ridBegin + 1;

            // Search back to start of group.
            while (true)
            {
                if (ridBegin <= 1)
                {
                    break;
                }
                IfFailGo(getRow(
                    ixTbl,
                    (UINT32)*(pVSTable->m_pMap->Get(ridBegin-1)),
                    &pRow));
                if (getIX(pRow, pCol[ixCol]) != ulVal)
                {
                    break;
                }
                --ridBegin;
            }

            // If desired, search forward to end of group.
            if (pRidEnd != NULL)
            {
                while (true)
                {
                    if (ridEnd > GetCountRecs(ixTbl))
                    {
                        break;
                    }
                    IfFailGo(getRow(
                        ixTbl,
                        (UINT32)*(pVSTable->m_pMap->Get(ridEnd)),
                        &pRow));
                    if (getIX(pRow, pCol[ixCol]) != ulVal)
                    {
                        break;
                    }
                    ++ridEnd;
                }
                *pRidEnd = ridEnd;
            }
            *pRidStart = ridBegin;
        }
    }

    // fall through
ErrExit:
    return hr;
} // CMiniMdRW::LookUpTableByCol

__checkReturn
HRESULT
CMiniMdRW::Impl_SearchTableRW(
    ULONG ixTbl,        // Table to search.
    ULONG ixCol,        // Column to search.
    ULONG ulTarget,     // Value to search for.
    RID  *pFoundRid)
{
    HRESULT hr = S_OK;
    RID     iRid;       // The resulting RID.
    RID     iRidEnd;    // Unused.

    // Look up.
    hr = LookUpTableByCol(ulTarget, m_pVS[ixTbl], &iRid, &iRidEnd);
    if (FAILED(hr))
    {
        iRid = 0;
    }
    else // Convert to real RID.
    {
        iRid = GetRidFromVirtualSort(ixTbl, iRid);
    }

    *pFoundRid = iRid;
    return S_OK;
} // CMiniMdRW::Impl_SearchTableRW

//*****************************************************************************
// Search a table for the row containing the given key value.
//  EG. Constant table has pointer back to Param or Field.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::vSearchTable(            // RID of matching row, or 0.
    ULONG       ixTbl,                  // Table to search.
    CMiniColDef sColumn,                // Sorted key column, containing search value.
    ULONG       ulTarget,               // Target for search.
    RID        *pRid)
{
    HRESULT hr;
    void   *pRow;                   // Row from a table.
    ULONG   val;                    // Value from a row.

    int         lo,mid,hi;              // binary search indices.

    // Binary search requires sorted table.
    // @todo GENERICS: why is IsSorted not true for mdtGenericParam?
    //        _ASSERTE(IsSorted(ixTbl));

    // Start with entire table.
    lo = 1;
    hi = GetCountRecs(ixTbl);
    // While there are rows in the range...
    while (lo <= hi)
    {   // Look at the one in the middle.
        mid = (lo + hi) / 2;
        IfFailRet(getRow(ixTbl, mid, &pRow));
        val = getIX(pRow, sColumn);
        // If equal to the target, done.
        if (val == ulTarget)
        {
            *pRid = mid;
            return S_OK;
        }
        // If middle item is too small, search the top half.
        if (val < ulTarget || val == END_OF_TABLE)
            lo = mid + 1;
        else // but if middle is to big, search bottom half.
            hi = mid - 1;
    }
    // Didn't find anything that matched.

    // @todo GENERICS: Work around for refEmit feature. Remove once table is sorted.
    if (ixTbl == TBL_GenericParam && !IsSorted(ixTbl))
    {
        for (int i = 1; i <= (int)GetCountRecs(ixTbl); i ++)
        {
            IfFailRet(getRow(ixTbl, i, &pRow));
            if (getIX(pRow, sColumn) == ulTarget)
            {
                *pRid = i;
                return S_OK;
            }
        }
    }

    *pRid = 0;
    return S_OK;
} // CMiniMdRW::vSearchTable

//*****************************************************************************
// Search a table for the highest-RID row containing a value that is less than
//  or equal to the target value.  EG.  TypeDef points to first Field, but if
//  a TypeDef has no fields, it points to first field of next TypeDef.
// This is complicated by the possible presence of columns containing
//  END_OF_TABLE values, which are not necessarily in greater than
//  other values.  However, this invalid-rid value will occur only at the
//  end of the table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::vSearchTableNotGreater( // RID of matching row, or 0.
    ULONG       ixTbl,                  // Table to search.
    CMiniColDef sColumn,                // the column def containing search value
    ULONG       ulTarget,               // target for search
    RID        *pRid)
{
    HRESULT hr;
    void   *pRow;                  // Row from a table.
    ULONG       cRecs;                  // Rows in the table.
    ULONG       val = 0;                // Value from a table.
    ULONG       lo,mid=0,hi;              // binary search indices.

    cRecs = GetCountRecs(ixTbl);

    // Start with entire table.
    lo = 1;
    hi = cRecs;
    // If no recs, return.
    if (lo > hi)
    {
        *pRid = 0;
        return S_OK;
    }
    // While there are rows in the range...
    while (lo <= hi)
    {   // Look at the one in the middle.
        mid = (lo + hi) / 2;
        IfFailRet(getRow(ixTbl, mid, &pRow));
        val = getIX(pRow, sColumn);
        // If equal to the target, done searching.
        if (val == ulTarget)
            break;
        // If middle item is too small, search the top half.
        if (val < ulTarget && val != END_OF_TABLE)
            lo = mid + 1;
        else // but if middle is to big, search bottom half.
            hi = mid - 1;
    }
    // May or may not have found anything that matched.  Mid will be close, but may
    //  be to high or too low.  It should point to the highest acceptable
    //  record.

    // If the value is greater than the target, back up just until the value is
    //  less than or equal to the target.  SHOULD only be one step.
    if (val > ulTarget || val == END_OF_TABLE)
    {
        while (val > ulTarget || val == END_OF_TABLE)
        {
            _ASSERTE(mid > 1);
            // If no recs match, return.
            if (mid == 1)
            {
                *pRid = 0;
                return S_OK;
            }
            --mid;
            IfFailRet(getRow(ixTbl, mid, &pRow));
            val = getIX(pRow, sColumn);
        }
    }
    else
    {
        // Value is less than or equal to the target.  As long as the next
        //  record is also acceptable, move forward.
        while (mid < cRecs)
        {
            // There is another record.  Get its value.
            IfFailRet(getRow(ixTbl, mid+1, &pRow));
            val = getIX(pRow, sColumn);
            // If that record is too high, stop.
            if (val > ulTarget || val == END_OF_TABLE)
                break;
            mid++;
        }
    }

    // Return the value that's just less than the target.
    *pRid = mid;
    return S_OK;
} // CMiniMdRW::vSearchTableNotGreater

//---------------------------------------------------------------------------------------
//
// Create MemberRef hash table.
//
__checkReturn
HRESULT
CMiniMdRW::CreateMemberRefHash()
{
    HRESULT hr = S_OK;

    if (m_pMemberRefHash == NULL)
    {
        ULONG ridEnd = getCountMemberRefs();
        if (ridEnd + 1 > INDEX_ROW_COUNT_THRESHOLD)
        {
            // Create a new hash.
            NewHolder<CMemberRefHash> pMemberRefHash = new (nothrow) CMemberRefHash();
            IfNullGo(pMemberRefHash);
            IfFailGo(pMemberRefHash->NewInit(
                g_HashSize[GetMetaDataSizeIndex(&m_OptionValue)]));

            // Scan every entry already in the table, add it to the hash.
            for (ULONG index = 1; index <= ridEnd; index++)
            {
                MemberRefRec * pMemberRef;
                IfFailGo(GetMemberRefRecord(index, &pMemberRef));

                LPCSTR szMemberRefName;
                IfFailGo(getNameOfMemberRef(pMemberRef, &szMemberRefName));
                ULONG iHash = HashMemberRef(
                    getClassOfMemberRef(pMemberRef),
                    szMemberRefName);

                TOKENHASHENTRY * pEntry = pMemberRefHash->Add(iHash);
                IfNullGo(pEntry);
                pEntry->tok = TokenFromRid(index, mdtMemberRef);
            }

            if (InterlockedCompareExchangeT<CMemberRefHash *>(&m_pMemberRefHash, pMemberRefHash, NULL) == NULL)
            {   // We won the initialization race
                pMemberRefHash.SuppressRelease();
            }
        }
    }

ErrExit:
    return hr;
} // CMiniMdRW::CreateMemberRefHash

//---------------------------------------------------------------------------------------
//
// Add a new MemberRef to the hash table.
//
__checkReturn
HRESULT
CMiniMdRW::AddMemberRefToHash(
    mdMemberRef mr)     // Token of new guy.
{
    HRESULT hr = S_OK;

    // If the hash exists, we will add to it - requires write-lock
    INDEBUG(Debug_CheckIsLockedForWrite();)

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (m_pMemberRefHash == NULL)
    {
        IfFailGo(CreateMemberRefHash());
    }
    else
    {
        MemberRefRec * pMemberRef;
        IfFailGo(GetMemberRefRecord(RidFromToken(mr), &pMemberRef));

        LPCSTR szMemberRefName;
        IfFailGo(getNameOfMemberRef(pMemberRef, &szMemberRefName));
        ULONG iHash = HashMemberRef(
            getClassOfMemberRef(pMemberRef),
            szMemberRefName);

        TOKENHASHENTRY * pEntry = m_pMemberRefHash->Add(iHash);
        IfNullGo(pEntry);
        pEntry->tok = TokenFromRid(RidFromToken(mr), mdtMemberRef);
    }

ErrExit:
    return hr;
} // CMiniMdRW::AddMemberRefToHash

//---------------------------------------------------------------------------------------
//
// If the hash is built, search for the item. Ignore token *ptkMemberRef.
//
CMiniMdRW::HashSearchResult
CMiniMdRW::FindMemberRefFromHash(
    mdToken          tkParent,      // Parent token.
    LPCUTF8          szName,        // Name of item.
    PCCOR_SIGNATURE  pvSigBlob,     // Signature.
    ULONG            cbSigBlob,     // Size of signature.
    mdMemberRef *    ptkMemberRef)  // IN: Ignored token. OUT: Return if found.
{
    // If the table is there, look for the item in the chain of items.
    if (m_pMemberRefHash != NULL)
    {
        TOKENHASHENTRY * p;
        ULONG            iHash;
        int              pos;

        // Hash the data.
        iHash = HashMemberRef(tkParent, szName);

        // Go through every entry in the hash chain looking for ours.
        for (p = m_pMemberRefHash->FindFirst(iHash, pos);
             p != NULL;
             p = m_pMemberRefHash->FindNext(pos))
        {
            if ((CompareMemberRefs(p->tok, tkParent, szName, pvSigBlob, cbSigBlob) == S_OK)
                && (*ptkMemberRef != p->tok))
            {
                *ptkMemberRef = p->tok;
                return Found;
            }
        }

        return NotFound;
    }
    else
    {
        return NoTable;
    }
} // CMiniMdRW::FindMemberRefFromHash

//*****************************************************************************
// Check a given mr token to see if this one is a match.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CompareMemberRefs(       // S_OK match, S_FALSE no match.
    mdMemberRef     mr,             // Token to check.
    mdToken         tkPar,          // Parent token.
    LPCUTF8         szNameUtf8,     // Name of item.
    PCCOR_SIGNATURE pvSigBlob,      // Signature.
    ULONG           cbSigBlob)      // Size of signature.
{
    HRESULT         hr;
    MemberRefRec   *pMemberRef;
    LPCUTF8         szNameUtf8Tmp;
    PCCOR_SIGNATURE pvSigBlobTmp;
    ULONG           cbSigBlobTmp;

    IfFailRet(GetMemberRefRecord(RidFromToken(mr), &pMemberRef));
    if (!IsNilToken(tkPar))
    {
        // If caller specifies the tkPar and tkPar doesn't match,
        // try the next memberref.
        //
        if (tkPar != getClassOfMemberRef(pMemberRef))
            return S_FALSE;
    }

    IfFailRet(getNameOfMemberRef(pMemberRef, &szNameUtf8Tmp));
    if (strcmp(szNameUtf8Tmp, szNameUtf8) == 0)
    {
        if (pvSigBlob == NULL)
        {
            return S_OK;
        }

        // Name matched. Now check the signature if caller supplies signature
        //
        if ((cbSigBlob != 0) && (pvSigBlob != NULL))
        {
            IfFailRet(getSignatureOfMemberRef(pMemberRef, &pvSigBlobTmp, &cbSigBlobTmp));
            if ((cbSigBlobTmp == cbSigBlob) &&
                (memcmp(pvSigBlob, pvSigBlobTmp, cbSigBlob) == 0))
            {
                return S_OK;
            }
        }
    }
    return S_FALSE;
} // CMiniMdRW::CompareMemberRefs


//*****************************************************************************
// Add a new memberdef to the hash table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddMemberDefToHash(
    mdToken tkMember,   // Token of new guy. It can be MethodDef or FieldDef
    mdToken tkParent)   // Parent token.
{
    HRESULT hr = S_OK;
    ULONG   iHash;
    MEMBERDEFHASHENTRY * pEntry;

    // If the hash exists, we will add to it - requires write-lock
    INDEBUG(Debug_CheckIsLockedForWrite();)

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (m_pMemberDefHash == NULL)
    {
        IfFailGo(CreateMemberDefHash());
    }
    else
    {
        LPCSTR szName;
        if (TypeFromToken(tkMember) == mdtMethodDef)
        {
            MethodRec * pMethodRecord;
            IfFailGo(GetMethodRecord(RidFromToken(tkMember), &pMethodRecord));
            IfFailGo(getNameOfMethod(pMethodRecord, &szName));
        }
        else
        {
            _ASSERTE(TypeFromToken(tkMember) == mdtFieldDef);
            FieldRec * pFieldRecord;
            IfFailGo(GetFieldRecord(RidFromToken(tkMember), &pFieldRecord));
            IfFailGo(getNameOfField(pFieldRecord, &szName));
        }

        iHash = HashMemberDef(tkParent, szName);

        pEntry = m_pMemberDefHash->Add(iHash);
        IfNullGo(pEntry);
        pEntry->tok = tkMember;
        pEntry->tkParent = tkParent;
    }

ErrExit:
    return hr;
} // CMiniMdRW::AddMemberDefToHash


//*****************************************************************************
// Create MemberDef Hash
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CreateMemberDefHash()
{
    HRESULT hr = S_OK;
    ULONG   iHash;
    MEMBERDEFHASHENTRY * pEntry;

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (m_pMemberDefHash == NULL)
    {
        RID          ridMethod = getCountMethods();
        RID          ridField = getCountFields();
        RID          iType;
        RID          ridStart;
        RID          ridEnd;
        TypeDefRec * pRec;
        MethodRec *  pMethod;
        FieldRec *   pField;

        if ((ridMethod + ridField + 1) > INDEX_ROW_COUNT_THRESHOLD)
        {
            // Create a new hash.
            NewHolder<CMemberDefHash> pMemberDefHash = new (nothrow) CMemberDefHash();
            IfNullGo(pMemberDefHash);
            IfFailGo(pMemberDefHash->NewInit(
                g_HashSize[GetMetaDataSizeIndex(&m_OptionValue)]));

            for (iType = 1; iType <= getCountTypeDefs(); iType++)
            {
                IfFailGo(GetTypeDefRecord(iType, &pRec));
                ridStart = getMethodListOfTypeDef(pRec);
                IfFailGo(getEndMethodListOfTypeDef(iType, &ridEnd));

                // add all of the methods of this typedef into hash table
                for (; ridStart < ridEnd; ridStart++)
                {
                    RID methodRid;
                    IfFailGo(GetMethodRid(ridStart, &methodRid));
                    IfFailGo(GetMethodRecord(methodRid, &pMethod));
                    LPCSTR szMethodName;
                    IfFailGo(getNameOfMethod(pMethod, &szMethodName));
                    iHash = HashMemberDef(TokenFromRid(iType, mdtTypeDef), szMethodName);

                    pEntry = pMemberDefHash->Add(iHash);
                    if (pEntry == NULL)
                        IfFailGo(OutOfMemory());
                    pEntry->tok = TokenFromRid(methodRid, mdtMethodDef);
                    pEntry->tkParent = TokenFromRid(iType, mdtTypeDef);
                }

                // add all of the fields of this typedef into hash table
                ridStart = getFieldListOfTypeDef(pRec);
                IfFailGo(getEndFieldListOfTypeDef(iType, &ridEnd));

                // Scan every entry already in the Method table, add it to the hash.
                for (; ridStart < ridEnd; ridStart++)
                {
                    RID fieldRid;
                    IfFailGo(GetFieldRid(ridStart, &fieldRid));
                    IfFailGo(GetFieldRecord(fieldRid, &pField));
                    LPCSTR szFieldName;
                    IfFailGo(getNameOfField(pField, &szFieldName));
                    iHash = HashMemberDef(TokenFromRid(iType, mdtTypeDef), szFieldName);

                    pEntry = pMemberDefHash->Add(iHash);
                    IfNullGo(pEntry);
                    pEntry->tok = TokenFromRid(fieldRid, mdtFieldDef);
                    pEntry->tkParent = TokenFromRid(iType, mdtTypeDef);
                }
            }

            if (InterlockedCompareExchangeT<CMemberDefHash *>(&m_pMemberDefHash, pMemberDefHash, NULL) == NULL)
            {   // We won the initialization race
                pMemberDefHash.SuppressRelease();
            }
        }
    }
ErrExit:
    return hr;
} // CMiniMdRW::CreateMemberDefHash

//---------------------------------------------------------------------------------------
//
// If the hash is built, search for the item. Ignore token *ptkMember.
//
CMiniMdRW::HashSearchResult
CMiniMdRW::FindMemberDefFromHash(
    mdToken         tkParent,   // Parent token.
    LPCUTF8         szName,     // Name of item.
    PCCOR_SIGNATURE pvSigBlob,  // Signature.
    ULONG           cbSigBlob,  // Size of signature.
    mdToken *       ptkMember)  // IN: Ignored token. OUT: Return if found. It can be MethodDef or FieldDef
{
    // check to see if we need to create hash table
    if (m_pMemberDefHash == NULL)
    {
        // Ignore the failure - the hash won't be created in the worst case
        (void)CreateMemberDefHash();
    }

    // If the table is there, look for the item in the chain of items.
    if (m_pMemberDefHash != NULL)
    {
        MEMBERDEFHASHENTRY * pEntry;
        ULONG                iHash;
        int                  pos;

        // Hash the data.
        iHash = HashMemberDef(tkParent, szName);

        // Go through every entry in the hash chain looking for ours.
        for (pEntry = m_pMemberDefHash->FindFirst(iHash, pos);
             pEntry != NULL;
             pEntry = m_pMemberDefHash->FindNext(pos))
        {
            if ((CompareMemberDefs(pEntry->tok, pEntry->tkParent, tkParent, szName, pvSigBlob, cbSigBlob) == S_OK)
                && (pEntry->tok != *ptkMember))
            {
                *ptkMember = pEntry->tok;
                return Found;
            }
        }

        return NotFound;
    }
    else
    {
        return NoTable;
    }
} // CMiniMdRW::FindMemberDefFromHash


//*****************************************************************************
// Check a given memberDef token to see if this one is a match.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CompareMemberDefs(       // S_OK match, S_FALSE no match.
    mdToken         tkMember,       // Token to check. It can be MethodDef or FieldDef
    mdToken         tkParent,       // Parent token recorded in the hash entry
    mdToken         tkPar,          // Parent token.
    LPCUTF8         szNameUtf8,     // Name of item.
    PCCOR_SIGNATURE pvSigBlob,      // Signature.
    ULONG           cbSigBlob)      // Size of signature.
{
    HRESULT         hr;
    MethodRec      *pMethod;
    FieldRec       *pField;
    LPCUTF8         szNameUtf8Tmp;
    PCCOR_SIGNATURE pvSigBlobTmp;
    ULONG           cbSigBlobTmp;
    bool            bPrivateScope;

    if (TypeFromToken(tkMember) == mdtMethodDef)
    {
        IfFailGo(GetMethodRecord(RidFromToken(tkMember), &pMethod));
        IfFailGo(getNameOfMethod(pMethod, &szNameUtf8Tmp));
        IfFailGo(getSignatureOfMethod(pMethod, &pvSigBlobTmp, &cbSigBlobTmp));
        bPrivateScope = IsMdPrivateScope(getFlagsOfMethod(pMethod));
    }
    else
    {
        _ASSERTE(TypeFromToken(tkMember) == mdtFieldDef);
        IfFailGo(GetFieldRecord(RidFromToken(tkMember), &pField));
        IfFailGo(getNameOfField(pField, &szNameUtf8Tmp));
        IfFailGo(getSignatureOfField(pField, &pvSigBlobTmp, &cbSigBlobTmp));
        bPrivateScope = IsFdPrivateScope(getFlagsOfField(pField));
    }
    if (bPrivateScope || (tkPar != tkParent))
    {
        return S_FALSE;
    }

    if (strcmp(szNameUtf8Tmp, szNameUtf8) == 0)
    {
        if (pvSigBlob == NULL)
        {
            return S_OK;
        }

        // Name matched. Now check the signature if caller supplies signature
        //
        if ((cbSigBlob != 0) && (pvSigBlob != NULL))
        {
            if ((cbSigBlobTmp == cbSigBlob) &&
                (memcmp(pvSigBlob, pvSigBlobTmp, cbSigBlob) == 0))
            {
                return S_OK;
            }
        }
    }
    hr = S_FALSE;
ErrExit:
    return hr;
} // CMiniMdRW::CompareMemberDefs

//*****************************************************************************
// Add a new NamedItem to the hash table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddNamedItemToHash(
    ULONG   ixTbl,          // Table with the new item.
    mdToken tk,             // Token of new guy.
    LPCUTF8 szName,         // Name of item.
    mdToken tkParent)       // Token of parent, if any.
{
    HRESULT         hr = S_OK;
    BYTE           *pNamedItem;     // A named item record.
    LPCUTF8         szItem;         // Name of the item.
    mdToken         tkPar = 0;      // Parent token of the item.
    ULONG           iHash;          // A named item's hash value.
    TOKENHASHENTRY *pEntry;         // New hash entry.

    // If the hash table hasn't been built it, see if it should get faulted in.
    if (m_pNamedItemHash == NULL)
    {
        ULONG ridEnd = GetCountRecs(ixTbl);
        // Range check avoiding prefast warning with:  "if (ridEnd + 1 > INDEX_ROW_COUNT_THRESHOLD)"
        if (ridEnd > (INDEX_ROW_COUNT_THRESHOLD - 1))
        {
            // This assert causes Dev11 #65887, turn it on when the bug is fixed
            //INDEBUG(Debug_CheckIsLockedForWrite();)

            // OutputDebugStringA("Creating TypeRef hash\n");
            // Create a new hash.
            m_pNamedItemHash = new (nothrow) CMetaDataHashBase;
            IfNullGo(m_pNamedItemHash);
            IfFailGo(m_pNamedItemHash->NewInit(
                g_HashSize[GetMetaDataSizeIndex(&m_OptionValue)]));

            // Scan every entry already in the table, add it to the hash.
            for (ULONG index = 1; index <= ridEnd; index++)
            {
                IfFailGo(m_Tables[ixTbl].GetRecord(index, &pNamedItem));
                IfFailGo(getString(GetCol(ixTbl, g_TblIndex[ixTbl].m_iName, pNamedItem), &szItem));
                if (g_TblIndex[ixTbl].m_iParent != (ULONG) -1)
                    tkPar = GetToken(ixTbl, g_TblIndex[ixTbl].m_iParent, pNamedItem);

                iHash = HashNamedItem(tkPar, szItem);

                pEntry = m_pNamedItemHash->Add(iHash);
                IfNullGo(pEntry);
                pEntry->tok = TokenFromRid(index, g_TblIndex[ixTbl].m_Token);
            }
        }
    }
    else
    {
        tk = RidFromToken(tk);
        IfFailGo(m_Tables[ixTbl].GetRecord(tk, &pNamedItem));
        IfFailGo(getString(GetCol(ixTbl, g_TblIndex[ixTbl].m_iName, pNamedItem), &szItem));
        if (g_TblIndex[ixTbl].m_iParent != (ULONG)-1)
            tkPar = GetToken(ixTbl, g_TblIndex[ixTbl].m_iParent, pNamedItem);

        iHash = HashNamedItem(tkPar, szItem);

        pEntry = m_pNamedItemHash->Add(iHash);
        IfNullGo(pEntry);
        pEntry->tok = TokenFromRid(tk, g_TblIndex[ixTbl].m_Token);
    }

ErrExit:
    return hr;
} // CMiniMdRW::AddNamedItemToHash

//*****************************************************************************
// If the hash is built, search for the item.
//*****************************************************************************
CMiniMdRW::HashSearchResult
CMiniMdRW::FindNamedItemFromHash(
    ULONG     ixTbl,    // Table with the item.
    LPCUTF8   szName,   // Name of item.
    mdToken   tkParent, // Token of parent, if any.
    mdToken * ptk)      // Return if found.
{
    // If the table is there, look for the item in the chain of items.
    if (m_pNamedItemHash != NULL)
    {
        TOKENHASHENTRY *p;              // Hash entry from chain.
        ULONG       iHash;              // Item's hash value.
        int         pos;                // Position in hash chain.
        mdToken     type;               // Type of the item being sought.

        type = g_TblIndex[ixTbl].m_Token;

        // Hash the data.
        iHash = HashNamedItem(tkParent, szName);

        // Go through every entry in the hash chain looking for ours.
        for (p = m_pNamedItemHash->FindFirst(iHash, pos);
             p != NULL;
             p = m_pNamedItemHash->FindNext(pos))
        {   // Check that the item is from the right table.
            if (TypeFromToken(p->tok) != (ULONG)type)
            {
                //<TODO>@FUTURE: if using the named item hash for multiple tables, remove
                //  this check.  Until then, debugging aid.</TODO>
                _ASSERTE(!"Table mismatch in hash chain");
                continue;
            }
            // Item is in the right table, do the deeper check.
            if (CompareNamedItems(ixTbl, p->tok, szName, tkParent) == S_OK)
            {
                *ptk = p->tok;
                return Found;
            }
        }

        return NotFound;
    }
    else
    {
        return NoTable;
    }
} // CMiniMdRW::FindNamedItemFromHash

//*****************************************************************************
// Check a given mr token to see if this one is a match.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::CompareNamedItems(   // S_OK match, S_FALSE no match.
    ULONG   ixTbl,      // Table with the item.
    mdToken tk,         // Token to check.
    LPCUTF8 szName,     // Name of item.
    mdToken tkParent)   // Token of parent, if any.
{
    HRESULT hr;
    BYTE   *pNamedItem;         // Item to check.
    LPCUTF8 szNameUtf8Tmp;      // Name of item to check.

    // Get the record.
    IfFailRet(m_Tables[ixTbl].GetRecord(RidFromToken(tk), &pNamedItem));

    // Name is cheaper to get than coded token parent, and fails pretty quickly.
    IfFailRet(getString(GetCol(ixTbl, g_TblIndex[ixTbl].m_iName, pNamedItem), &szNameUtf8Tmp));
    if (strcmp(szNameUtf8Tmp, szName) != 0)
        return S_FALSE;

    // Name matched, try parent, if any.
    if (g_TblIndex[ixTbl].m_iParent != (ULONG)-1)
    {
        mdToken tkPar = GetToken(ixTbl, g_TblIndex[ixTbl].m_iParent, pNamedItem);
        if (tkPar != tkParent)
            return S_FALSE;
    }

    // Made it to here, so everything matched.
    return S_OK;
} // CMiniMdRW::CompareNamedItems

//*****************************************************************************
// Add <md, td> entry to the MethodDef map look up table
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddMethodToLookUpTable(
    mdMethodDef md,
    mdTypeDef   td)
{
    HRESULT  hr = NOERROR;
    mdToken *ptk;
    _ASSERTE((TypeFromToken(md) == mdtMethodDef) && HasIndirectTable(TBL_Method));

    if (m_pMethodMap != NULL)
    {
        // Only add to the lookup table if it has been built already by demand.
        //
        // The first entry in the map is a dummy entry.
        // The i'th index entry of the map is the td for methoddef of i.
        // We do expect the methoddef tokens are all added when the map exist.
        //
        _ASSERTE(RidFromToken(md) == (ULONG)m_pMethodMap->Count());
        INDEBUG(Debug_CheckIsLockedForWrite();)
        ptk = m_pMethodMap->Append();
        IfNullGo(ptk);
        *ptk = td;
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddMethodToLookUpTable

//*****************************************************************************
// Add <fd, td> entry to the FieldDef map look up table
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddFieldToLookUpTable(
    mdFieldDef fd,
    mdTypeDef  td)
{
    HRESULT  hr = NOERROR;
    mdToken *ptk;
    _ASSERTE((TypeFromToken(fd) == mdtFieldDef) && HasIndirectTable(TBL_Field));
    if (m_pFieldMap != NULL)
    {
        // Only add to the lookup table if it has been built already by demand.
        //
        // The first entry in the map is a dummy entry.
        // The i'th index entry of the map is the td for fielddef of i.
        // We do expect the fielddef tokens are all added when the map exist.
        //
        _ASSERTE(RidFromToken(fd) == (ULONG)m_pFieldMap->Count());
        ptk = m_pFieldMap->Append();
        IfNullGo(ptk);
        *ptk = td;
    }

ErrExit:
    return hr;
} // CMiniMdRW::AddFieldToLookUpTable

//*****************************************************************************
// Add <pr, td> entry to the Property map look up table
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddPropertyToLookUpTable(
    mdProperty pr,
    mdTypeDef  td)
{
    HRESULT  hr = NOERROR;
    mdToken *ptk;
    _ASSERTE((TypeFromToken(pr) == mdtProperty) && HasIndirectTable(TBL_Property));

    if (m_pPropertyMap != NULL)
    {
        // Only add to the lookup table if it has been built already by demand.
        //
        // The first entry in the map is a dummy entry.
        // The i'th index entry of the map is the td for property of i.
        // We do expect the property tokens are all added when the map exist.
        //
        _ASSERTE(RidFromToken(pr) == (ULONG)m_pPropertyMap->Count());
        ptk = m_pPropertyMap->Append();
        IfNullGo(ptk);
        *ptk = td;
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddPropertyToLookUpTable

//*****************************************************************************
// Add <ev, td> entry to the Event map look up table
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddEventToLookUpTable(
    mdEvent   ev,
    mdTypeDef td)
{
    HRESULT  hr = NOERROR;
    mdToken *ptk;
    _ASSERTE((TypeFromToken(ev) == mdtEvent) && HasIndirectTable(TBL_Event));

    if (m_pEventMap != NULL)
    {
        // Only add to the lookup table if it has been built already by demand.
        //
        // now add to the EventMap table
        _ASSERTE(RidFromToken(ev) == (ULONG)m_pEventMap->Count());
        ptk = m_pEventMap->Append();
        IfNullGo(ptk);
        *ptk = td;
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddEventToLookUpTable

//*****************************************************************************
// Add <pd, md> entry to the Param map look up table
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::AddParamToLookUpTable(
    mdParamDef  pd,
    mdMethodDef md)
{
    HRESULT  hr = NOERROR;
    mdToken *ptk;
    _ASSERTE((TypeFromToken(pd) == mdtParamDef) && HasIndirectTable(TBL_Param));

    if (m_pParamMap != NULL)
    {
        // Only add to the lookup table if it has been built already by demand.
        //
        // now add to the EventMap table
        _ASSERTE(RidFromToken(pd) == (ULONG)m_pParamMap->Count());
        ptk = m_pParamMap->Append();
        IfNullGo(ptk);
        *ptk = md;
    }
ErrExit:
    return hr;
} // CMiniMdRW::AddParamToLookUpTable

//*****************************************************************************
// Find parent for a method token. This will use the lookup table if there is an
// intermediate table. Or it will use FindMethodOfParent helper
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindParentOfMethodHelper(
    mdMethodDef md,     // [IN] the methoddef token
    mdTypeDef  *ptd)    // [OUT] the parent token
{
    HRESULT hr = NOERROR;
    if (HasIndirectTable(TBL_Method))
    {
        if (m_pMethodMap == NULL)
        {
            RID            indexTd;
            RID            indexMd;
            RID            ridStart;
            RID            ridEnd;
            TypeDefRec *   pTypeDefRec;
            MethodPtrRec * pMethodPtrRec;

            // build the MethodMap table
            NewHolder<TOKENMAP> pMethodMap = new (nothrow) TOKENMAP;
            IfNullGo(pMethodMap);
            ULONG nAllocateSize;
            if (!ClrSafeInt<ULONG>::addition(m_Schema.m_cRecs[TBL_Method], 1, nAllocateSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (pMethodMap->AllocateBlock(nAllocateSize) == 0)
                IfFailGo(E_OUTOFMEMORY);
            for (indexTd = 1; indexTd <= m_Schema.m_cRecs[TBL_TypeDef]; indexTd++)
            {
                IfFailGo(GetTypeDefRecord(indexTd, &pTypeDefRec));
                ridStart = getMethodListOfTypeDef(pTypeDefRec);
                IfFailGo(getEndMethodListOfTypeDef(indexTd, &ridEnd));

                for (indexMd = ridStart; indexMd < ridEnd; indexMd++)
                {
                    IfFailGo(GetMethodPtrRecord(indexMd, &pMethodPtrRec));
                    PREFIX_ASSUME(pMethodMap->Get(getMethodOfMethodPtr(pMethodPtrRec)) != NULL);
                    *(pMethodMap->Get(getMethodOfMethodPtr(pMethodPtrRec))) = indexTd;
                }
            }
            if (InterlockedCompareExchangeT<TOKENMAP *>(
                &m_pMethodMap,
                pMethodMap,
                NULL) == NULL)
            {   // We won the initializaion race
                pMethodMap.SuppressRelease();
            }
        }
        *ptd = *(m_pMethodMap->Get(RidFromToken(md)));
    }
    else
    {
        IfFailGo(FindParentOfMethod(RidFromToken(md), (RID *)ptd));
    }
    RidToToken(*ptd, mdtTypeDef);
ErrExit:
    return hr;
} // CMiniMdRW::FindParentOfMethodHelper

//*****************************************************************************
// Find parent for a field token. This will use the lookup table if there is an
// intermediate table. Or it will use FindFieldOfParent helper
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindParentOfFieldHelper(
    mdFieldDef fd,      // [IN] fielddef token
    mdTypeDef *ptd)     // [OUT] parent token
{
    HRESULT hr = NOERROR;
    if (HasIndirectTable(TBL_Field))
    {
        if (m_pFieldMap == NULL)
        {
            RID         indexTd;
            RID         indexFd;
            RID         ridStart, ridEnd;
            TypeDefRec  *pTypeDefRec;
            FieldPtrRec *pFieldPtrRec;

            // build the FieldMap table
            NewHolder<TOKENMAP> pFieldMap = new (nothrow) TOKENMAP;
            IfNullGo(pFieldMap);
            ULONG nAllocateSize;
            if (!ClrSafeInt<ULONG>::addition(m_Schema.m_cRecs[TBL_Field], 1, nAllocateSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (pFieldMap->AllocateBlock(nAllocateSize) == 0)
                IfFailGo(E_OUTOFMEMORY);
            for (indexTd = 1; indexTd<= m_Schema.m_cRecs[TBL_TypeDef]; indexTd++)
            {
                IfFailGo(GetTypeDefRecord(indexTd, &pTypeDefRec));
                ridStart = getFieldListOfTypeDef(pTypeDefRec);
                IfFailGo(getEndFieldListOfTypeDef(indexTd, &ridEnd));

                for (indexFd = ridStart; indexFd < ridEnd; indexFd++)
                {
                    IfFailGo(GetFieldPtrRecord(indexFd, &pFieldPtrRec));
                    PREFIX_ASSUME(pFieldMap->Get(getFieldOfFieldPtr(pFieldPtrRec)) != NULL);
                    *(pFieldMap->Get(getFieldOfFieldPtr(pFieldPtrRec))) = indexTd;
                }
            }
            if (InterlockedCompareExchangeT<TOKENMAP *>(
                &m_pFieldMap,
                pFieldMap,
                NULL) == NULL)
            {   // We won the initializaion race
                pFieldMap.SuppressRelease();
            }
        }
        *ptd = *(m_pFieldMap->Get(RidFromToken(fd)));
    }
    else
    {
        IfFailGo(FindParentOfField(RidFromToken(fd), (RID *)ptd));
    }
    RidToToken(*ptd, mdtTypeDef);
ErrExit:
    return hr;
} // CMiniMdRW::FindParentOfFieldHelper

//*****************************************************************************
// Find parent for a property token. This will use the lookup table if there is an
// intermediate table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindParentOfPropertyHelper(
    mdProperty pr,
    mdTypeDef *ptd)
{
    HRESULT hr = NOERROR;
    if (HasIndirectTable(TBL_Property))
    {
        if (m_pPropertyMap == NULL)
        {
            RID         indexMap;
            RID         indexPr;
            RID         ridStart, ridEnd;
            PropertyMapRec  *pPropertyMapRec;
            PropertyPtrRec  *pPropertyPtrRec;

            // build the PropertyMap table
            NewHolder<TOKENMAP> pPropertyMap = new (nothrow) TOKENMAP;
            IfNullGo(pPropertyMap);
            ULONG nAllocateSize;
            if (!ClrSafeInt<ULONG>::addition(m_Schema.m_cRecs[TBL_Property], 1, nAllocateSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (pPropertyMap->AllocateBlock(nAllocateSize) == 0)
                IfFailGo( E_OUTOFMEMORY );
            for (indexMap = 1; indexMap<= m_Schema.m_cRecs[TBL_PropertyMap]; indexMap++)
            {
                IfFailGo(GetPropertyMapRecord(indexMap, &pPropertyMapRec));
                ridStart = getPropertyListOfPropertyMap(pPropertyMapRec);
                IfFailGo(getEndPropertyListOfPropertyMap(indexMap, &ridEnd));

                for (indexPr = ridStart; indexPr < ridEnd; indexPr++)
                {
                    IfFailGo(GetPropertyPtrRecord(indexPr, &pPropertyPtrRec));
                    mdToken *tok =  pPropertyMap->Get(getPropertyOfPropertyPtr(pPropertyPtrRec));
                    PREFIX_ASSUME(tok != NULL);
                    *tok = getParentOfPropertyMap(pPropertyMapRec);
                }
            }
            if (InterlockedCompareExchangeT<TOKENMAP *>(
                &m_pPropertyMap,
                pPropertyMap,
                NULL) == NULL)
            {   // We won the initializaion race
                pPropertyMap.SuppressRelease();
            }
        }
        *ptd = *(m_pPropertyMap->Get(RidFromToken(pr)));
    }
    else
    {
        RID             ridPropertyMap;
        PropertyMapRec *pRec;

        IfFailGo(FindPropertyMapParentOfProperty(RidFromToken(pr), &ridPropertyMap));
        IfFailGo(GetPropertyMapRecord(ridPropertyMap, &pRec));
        *ptd = getParentOfPropertyMap(pRec);
    }
    RidToToken(*ptd, mdtTypeDef);
ErrExit:
    return hr;
} // CMiniMdRW::FindParentOfPropertyHelper

//*****************************************************************************
// Find parent for an Event token. This will use the lookup table if there is an
// intermediate table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindParentOfEventHelper(
    mdEvent    ev,
    mdTypeDef *ptd)
{
    HRESULT     hr = NOERROR;
    if (HasIndirectTable(TBL_Event))
    {
        if (m_pEventMap == NULL)
        {
            RID         indexMap;
            RID         indexEv;
            RID         ridStart, ridEnd;
            EventMapRec *pEventMapRec;
            EventPtrRec  *pEventPtrRec;

            // build the EventMap table
            NewHolder<TOKENMAP> pEventMap = new (nothrow) TOKENMAP;
            IfNullGo(pEventMap);
            ULONG nAllocateSize;
            if (!ClrSafeInt<ULONG>::addition(m_Schema.m_cRecs[TBL_Event], 1, nAllocateSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (pEventMap->AllocateBlock(nAllocateSize) == 0)
                IfFailGo(E_OUTOFMEMORY);
            for (indexMap = 1; indexMap<= m_Schema.m_cRecs[TBL_EventMap]; indexMap++)
            {
                IfFailGo(GetEventMapRecord(indexMap, &pEventMapRec));
                ridStart = getEventListOfEventMap(pEventMapRec);
                IfFailGo(getEndEventListOfEventMap(indexMap, &ridEnd));

                for (indexEv = ridStart; indexEv < ridEnd; indexEv++)
                {
                    IfFailGo(GetEventPtrRecord(indexEv, &pEventPtrRec));
                    mdToken* tok = pEventMap->Get(getEventOfEventPtr(pEventPtrRec));
                    PREFIX_ASSUME(tok != NULL);
                    *tok = getParentOfEventMap(pEventMapRec);
                }
            }
            if (InterlockedCompareExchangeT<TOKENMAP *>(
                &m_pEventMap,
                pEventMap,
                NULL) == NULL)
            {   // We won the initializaion race
                pEventMap.SuppressRelease();
            }
        }
        *ptd = *(m_pEventMap->Get(RidFromToken(ev)));
    }
    else
    {
        RID         ridEventMap;
        EventMapRec *pRec;

        IfFailGo(FindEventMapParentOfEvent(RidFromToken(ev), &ridEventMap));
        IfFailGo(GetEventMapRecord(ridEventMap, &pRec));
        *ptd = getParentOfEventMap(pRec);
    }
    RidToToken(*ptd, mdtTypeDef);
ErrExit:
    return hr;
} // CMiniMdRW::FindParentOfEventHelper

//*****************************************************************************
// Find parent for a ParamDef token. This will use the lookup table if there is an
// intermediate table.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::FindParentOfParamHelper(
    mdParamDef   pd,
    mdMethodDef *pmd)
{
    HRESULT hr = NOERROR;
    if (HasIndirectTable(TBL_Param))
    {
        if (m_pParamMap == NULL)
        {
            RID         indexMd;
            RID         indexPd;
            RID         ridStart, ridEnd;
            MethodRec   *pMethodRec;
            ParamPtrRec *pParamPtrRec;

            // build the ParamMap table
            NewHolder<TOKENMAP> pParamMap = new (nothrow) TOKENMAP;
            IfNullGo(pParamMap);
            uint32_t nAllocateSize;
            if (!ClrSafeInt<uint32_t>::addition(m_Schema.m_cRecs[TBL_Param], 1, nAllocateSize))
            {
                IfFailGo(COR_E_OVERFLOW);
            }
            if (pParamMap->AllocateBlock(nAllocateSize) == 0)
                IfFailGo(E_OUTOFMEMORY);
            for (indexMd = 1; indexMd<= m_Schema.m_cRecs[TBL_Method]; indexMd++)
            {
                IfFailGo(GetMethodRecord(indexMd, &pMethodRec));
                ridStart = getParamListOfMethod(pMethodRec);
                IfFailGo(getEndParamListOfMethod(indexMd, &ridEnd));

                for (indexPd = ridStart; indexPd < ridEnd; indexPd++)
                {
                    IfFailGo(GetParamPtrRecord(indexPd, &pParamPtrRec));
                    PREFIX_ASSUME(pParamMap->Get(getParamOfParamPtr(pParamPtrRec)) != NULL);
                    *(pParamMap->Get(getParamOfParamPtr(pParamPtrRec))) = indexMd;
                }
            }
            if (InterlockedCompareExchangeT<TOKENMAP *>(
                &m_pParamMap,
                pParamMap,
                NULL) == NULL)
            {   // We won the initializaion race
                pParamMap.SuppressRelease();
            }
        }
        *pmd = *(m_pParamMap->Get(RidFromToken(pd)));
    }
    else
    {
        IfFailGo(FindParentOfParam(RidFromToken(pd), (RID *)pmd));
    }
    RidToToken(*pmd, mdtMethodDef);
ErrExit:
    return hr;
} // CMiniMdRW::FindParentOfParamHelper


//******************************************************************************
// Add an entry in the ENC Log table.
//******************************************************************************
__checkReturn
HRESULT
CMiniMdRW::UpdateENCLogHelper(
    mdToken                tk,          // Token to be added to the ENCLog table.
    CMiniMdRW::eDeltaFuncs funccode)    // Specifies the optional function code..
{
    ENCLogRec   *pRecord;
    RID         iRecord;
    HRESULT     hr = S_OK;

    // @todo - MD can't handle anything other than functions right now
   /* if (TypeFromToken(tk) != mdtMethodDef)
    {
        _ASSERTE(!"Trying to do something that we can't do");
        return S_OK;
    }
    */
    IfFailGo(AddENCLogRecord(&pRecord, &iRecord));
    pRecord->SetToken(tk);
    pRecord->SetFuncCode(funccode);

ErrExit:
    return hr;
} // CMiniMdRW::UpdateENCLogHelper

__checkReturn
HRESULT
CMiniMdRW::UpdateENCLogHelper2(
    ULONG                  ixTbl,       // Table being updated.
    ULONG                  iRid,        // Record within table.
    CMiniMdRW::eDeltaFuncs funccode)    // Specifies the optional function code..
{
    ENCLogRec   *pRecord;
    RID         iRecord;
    HRESULT     hr = S_OK;

    IfFailGo(AddENCLogRecord(&pRecord, &iRecord));
    pRecord->SetToken(RecIdFromRid(iRid, ixTbl));
    pRecord->SetFuncCode(funccode);

ErrExit:
    return hr;
} // CMiniMdRW::UpdateENCLogHelper2

__checkReturn
HRESULT
CMiniMdRW::ResetENCLog()
{
#ifdef FEATURE_METADATA_EMIT
    HRESULT     hr = S_OK;
    ModuleRec * pMod;

    // Get the module record.
    IfFailGo(GetModuleRecord(1, &pMod));


    // Reset the pool deltas
    m_StringHeap.StartNewEnCSession();
    m_BlobHeap.StartNewEnCSession();
    m_UserStringHeap.StartNewEnCSession();

    // Clear the ENCLog
    m_Tables[TBL_ENCLog].Delete();
    m_Schema.m_cRecs[TBL_ENCLog] = 0;

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT
    return S_OK;
#endif //!FEATURE_METADATA_EMIT
} // CMiniMdRW::ResetENCLog

// ----------------------------------------------------------------------------
// Workaround for compiler performance issue VSW 584653 for 2.0 RTM.
// Get the table's VirtualSort validity state.
bool
CMiniMdRW::IsTableVirtualSorted(ULONG ixTbl)
{
    _ASSERTE(ixTbl < m_TblCount);

    if (m_pVS[ixTbl] == NULL)
    {
        return false;
    }
    return m_pVS[ixTbl]->m_isMapValid;
} // CMiniMdRW::IsTableVirtualSorted

// ----------------------------------------------------------------------------
// Workaround for compiler performance issue VSW 584653 for 2.0 RTM.
//
// Validate table's VirtualSort after adding one record into the table.
// Returns new VirtualSort validity state in *pfIsTableVirtualSortValid.
// Assumptions:
//    Table's VirtualSort was valid before adding the record to the table.
//    The caller must ensure validity of VirtualSort by calling to
//    IsTableVirtualSorted or by using the returned state from previous
//    call to this method.
__checkReturn
HRESULT
CMiniMdRW::ValidateVirtualSortAfterAddRecord(
    ULONG ixTbl,
    bool *pfIsTableVirtualSortValid)
{
    _ASSERTE(ixTbl < m_TblCount);

    HRESULT hr;
    VirtualSort *pVS = m_pVS[ixTbl];

    // VirtualSort was valid (had to exist)
    _ASSERTE(pVS != NULL);
    // Adding record invalidated VirtualSort
    _ASSERTE(!pVS->m_isMapValid);
    // Only 1 record was added into table (VirtualSort has 1 bogus element)
    _ASSERTE(m_Schema.m_cRecs[ixTbl] == (ULONG)pVS->m_pMap->Count());

    // Append 1 element into VirtualSort
    mdToken *pAddedVSToken = pVS->m_pMap->Append();
    if (pAddedVSToken == NULL)
    {   // There's not enough memory
        // Do not handle OOM now, just leave the VirtualSort invalidated, the
        // next allocation will take care of OOM or the VirtualSort will be
        // resorted when needed (as it was before this performance workaround)
        *pfIsTableVirtualSortValid = false;
        return S_OK;
    }

    // Initialize added element
    int iLastElementIndex = pVS->m_pMap->Count() - 1;
    *pAddedVSToken = iLastElementIndex;
    // Check if the added element extends the VirtualSort (keeps sorting)
    if (iLastElementIndex > 2)
    {
        int nCompareResult;
        IfFailRet(pVS->Compare(
            iLastElementIndex - 1,
            iLastElementIndex,
            &nCompareResult));
        if (nCompareResult < 0)
        {   // VirtualSort was extended - the added element is bigger than
            // previously last element in VirtualSort

            // Validate VirtualSort as it is still sorted and covers all elements
            // of the MetaData table
            pVS->m_isMapValid = true;
            *pfIsTableVirtualSortValid = true;
            return S_OK;
        }
    }
    // The added element doesn't extend VirtualSort - it is not sorted

    // Keep the VirtualSort invalidated, therefore next binary search will
    // force its recreation and resorting (as it did before this performance
    // workaround)
    *pfIsTableVirtualSortValid = false;
    return S_OK;
} // CMiniMdRW::ValidateVirtualSortAfterAddRecord

#ifdef _DEBUG

// ----------------------------------------------------------------------------
void
CMiniMdRW::Debug_CheckIsLockedForWrite()
{
    // If this assert fires, then we are trying to modify MetaData that is not locked for write
    _ASSERTE((dbg_m_pLock == NULL) || dbg_m_pLock->Debug_IsLockedForWrite());
}

#endif //_DEBUG

//*****************************************************************************
//
// Sort the whole RID table
//
//*****************************************************************************
__checkReturn
HRESULT
VirtualSort::Sort()
{
    m_isMapValid = true;
    // Note that m_pMap stores an additional bogus element at count 0.  This is
    // just so we can align the index in m_pMap with the Rids which are 1 based.
    return SortRange(1, m_pMap->Count() - 1);
} // VirtualSort::Sort

//*****************************************************************************
//
// Sort the range from iLeft to iRight
//
//*****************************************************************************
__checkReturn
HRESULT
VirtualSort::SortRange(
    int iLeft,
    int iRight)
{
    HRESULT hr;
    int     iLast;

    while (true)
    {
        // if less than two elements you're done.
        if (iLeft >= iRight)
        {
            return S_OK;
        }

        // The mid-element is the pivot, move it to the left.
        Swap(iLeft, (iLeft+iRight)/2);
        iLast = iLeft;

        // move everything that is smaller than the pivot to the left.
        for (int i = iLeft+1; i <= iRight; i++)
        {
            int nCompareResult;
            IfFailRet(Compare(i, iLeft, &nCompareResult));
            if (nCompareResult < 0)
            {
                Swap(i, ++iLast);
            }
        }

        // Put the pivot to the point where it is in between smaller and larger elements.
        Swap(iLeft, iLast);

        // Sort each partition.
        int iLeftLast = iLast - 1;
        int iRightFirst = iLast + 1;
        if (iLeftLast - iLeft < iRight - iRightFirst)
        {   // Left partition is smaller, sort it recursively
            IfFailRet(SortRange(iLeft, iLeftLast));
            // Tail call to sort the right (bigger) partition
            iLeft = iRightFirst;
            //iRight = iRight;
            continue;
        }
        else
        {   // Right partition is smaller, sort it recursively
            IfFailRet(SortRange(iRightFirst, iRight));
            // Tail call to sort the left (bigger) partition
            //iLeft = iLeft;
            iRight = iLeftLast;
            continue;
        }
    }
} // VirtualSort::SortRange

//*****************************************************************************
//
// Compare two RID base on the m_ixTbl's m_ixCol
//
//*****************************************************************************
__checkReturn
HRESULT
VirtualSort::Compare(
    RID  iLeft,         // First item to compare.
    RID  iRight,        // Second item to compare.
    int *pnResult)      // -1, 0, or 1
{
    HRESULT hr;
    RID         ridLeft = *(m_pMap->Get(iLeft));
    RID         ridRight = *(m_pMap->Get(iRight));
    void  *pRow;                  // Row from a table.
    ULONG       valRight, valLeft;      // Value from a row.

    IfFailRet(m_pMiniMd->getRow(m_ixTbl, ridLeft, &pRow));
    valLeft = m_pMiniMd->getIX(pRow, m_pMiniMd->m_TableDefs[m_ixTbl].m_pColDefs[m_ixCol]);
    IfFailRet(m_pMiniMd->getRow(m_ixTbl, ridRight, &pRow));
    valRight = m_pMiniMd->getIX(pRow, m_pMiniMd->m_TableDefs[m_ixTbl].m_pColDefs[m_ixCol]);

    if (valLeft < valRight)
    {
        *pnResult = -1;
        return S_OK;
    }
    if (valLeft > valRight)
    {
        *pnResult = 1;
        return S_OK;
    }
    // Values are equal -- preserve existing ordering.
    if (ridLeft < ridRight)
    {
        *pnResult = -1;
        return S_OK;
    }
    if (ridLeft > ridRight)
    {
        *pnResult = 1;
        return S_OK;
    }
    // Comparing an item to itself?
    _ASSERTE(!"Comparing an item to itself in sort");

    *pnResult = 0;
    return S_OK;
} // VirtualSort::Compare

//*****************************************************************************
//
// Initialization function
//
//*****************************************************************************
void VirtualSort::Init(                 //
    ULONG       ixTbl,                  // Table index.
    ULONG       ixCol,                  // Column index.
    CMiniMdRW *pMiniMd)                 // MiniMD with data.
{
    m_pMap = NULL;
    m_isMapValid = false;
    m_ixTbl = ixTbl;
    m_ixCol = ixCol;
    m_pMiniMd = pMiniMd;
} // VirtualSort::Init


//*****************************************************************************
//
// Uninitialization function
//
//*****************************************************************************
void VirtualSort::Uninit()
{
    if ( m_pMap )
        delete m_pMap;
    m_pMap = NULL;
    m_isMapValid = false;
} // VirtualSort::Uninit


//*****************************************************************************
//
// Mark a token
//
//*****************************************************************************
HRESULT FilterTable::MarkToken(
    mdToken     tk,                         // token to be marked as to keep
    DWORD       bitToMark)                  // bit flag to set in the keep table
{
    HRESULT     hr = NOERROR;
    RID         rid = RidFromToken(tk);

    if ( (Count() == 0) || ((RID)(Count() -1)) < rid )
    {
        // grow table
        IfFailGo( AllocateBlock( rid + 1 - Count() ) );
    }

#ifdef _DEBUG
    if ( (*Get(rid)) & bitToMark )
    {
        // global TypeDef could be marked more than once so don't assert if token is mdtTypeDef
        if (TypeFromToken(tk) != mdtTypeDef)
            _ASSERTE(!"Token has been Marked");
    }
#endif //_DEBUG

    // set the keep bit
    *Get(rid) = (*Get(rid)) | bitToMark;
ErrExit:
    return hr;
} // FilterTable::MarkToken


//*****************************************************************************
//
// Unmark a token
//
//*****************************************************************************
HRESULT FilterTable::UnmarkToken(
    mdToken     tk,                         // token to be unmarked as deleted.
    DWORD       bitToMark)                  // bit flag to unset in the keep table
{
    RID         rid = RidFromToken(tk);

    if ( (Count() == 0) || ((RID)(Count() -1)) < rid )
    {
        // unmarking should not have grown table. It currently only support dropping the transient CAs.
        _ASSERTE(!"BAD state!");
    }

#ifdef _DEBUG
    if ( (*Get(rid)) & bitToMark )
    {
        // global TypeDef could be marked more than once so don't assert if token is mdtTypeDef
        if (TypeFromToken(tk) != mdtTypeDef)
            _ASSERTE(!"Token has been Marked");
    }
#endif //_DEBUG

    // unset the keep bit
    *Get(rid) = (*Get(rid)) & ~bitToMark;
    return NOERROR;
} // FilterTable::MarkToken


//*****************************************************************************
//
// Mark an UserString token
//
//*****************************************************************************
HRESULT FilterTable::MarkUserString(
    mdString        str)
{
    int             high, low, mid;

    low = 0;
    high = m_daUserStringMarker->Count() - 1;
    while (low <= high)
    {
        mid = (high + low) / 2;
        if ((m_daUserStringMarker->Get(mid))->m_tkString > (DWORD) str)
        {
            high = mid - 1;
        }
        else if ((m_daUserStringMarker->Get(mid))->m_tkString < (DWORD) str)
        {
            low = mid + 1;
        }
        else
        {
            (m_daUserStringMarker->Get(mid))->m_fMarked = true;
            return NOERROR;
        }
    }
    _ASSERTE(!"Bad Token!");
    return NOERROR;
} // FilterTable::MarkUserString

//*****************************************************************************
//
// Mark a UserString token that was added since our last MarkAll/UnMarkAll
//
//*****************************************************************************
HRESULT FilterTable::MarkNewUserString(mdString str)
{
    FilterUserStringEntry *pItem = m_daUserStringMarker->Append();

    if (pItem == NULL)
        return E_OUTOFMEMORY;

    pItem->m_tkString = str;
    pItem->m_fMarked = true;

    return S_OK;
} // FilterTable::MarkNewUserString

//*****************************************************************************
//
// Unmarking from 1 to ulSize for all tokens.
//
//*****************************************************************************
HRESULT FilterTable::UnmarkAll(
    CMiniMdRW   *pMiniMd,
    ULONG       ulSize)
{
    HRESULT hr;

    S_UINT32 nAllocateSize = S_UINT32(ulSize) + S_UINT32(1);
    if (nAllocateSize.IsOverflow())
    {
        IfFailGo(COR_E_OVERFLOW);
    }
    if (!AllocateBlock(nAllocateSize.Value()))
    {
        IfFailGo(E_OUTOFMEMORY);
    }
    memset(Get(0), 0, nAllocateSize.Value() * sizeof(DWORD));

    // unmark all of the user string
    m_daUserStringMarker = new (nothrow) CDynArray<FilterUserStringEntry>();
    IfNullGo(m_daUserStringMarker);

    for (UINT32 nIndex = 0; ;)
    {
        MetaData::DataBlob userString;
        UINT32 nNextIndex;
        hr = pMiniMd->GetUserStringAndNextIndex(
            nIndex,
            &userString,
            &nNextIndex);
        IfFailGo(hr);
        if (hr == S_FALSE)
        {   // We reached the last user string
            hr = S_OK;
            break;
        }
        _ASSERTE(hr == S_OK);

        // Skip empty strings
        if (userString.IsEmpty())
        {
            nIndex = nNextIndex;
            continue;
        }
        FilterUserStringEntry *pItem = m_daUserStringMarker->Append();
        pItem->m_tkString = TokenFromRid(nIndex, mdtString);
        pItem->m_fMarked = false;

        // Process next user string in the heap
        nIndex = nNextIndex;
    }

ErrExit:
    return hr;
} // FilterTable::UnmarkAll



//*****************************************************************************
//
// Marking from 1 to ulSize for all tokens.
//
//*****************************************************************************
HRESULT FilterTable::MarkAll(
    CMiniMdRW   *pMiniMd,
    ULONG       ulSize)
{
    HRESULT hr = S_OK;

    S_UINT32 nAllocateSize = S_UINT32(ulSize) + S_UINT32(1);
    if (nAllocateSize.IsOverflow())
    {
        IfFailGo(COR_E_OVERFLOW);
    }
    if (!AllocateBlock(nAllocateSize.Value()))
    {
        IfFailGo(E_OUTOFMEMORY);
    }
    memset(Get(0), 0xFFFFFFFF, nAllocateSize.Value() * sizeof(DWORD));

    // mark all of the user string
    m_daUserStringMarker = new (nothrow) CDynArray<FilterUserStringEntry>();
    IfNullGo(m_daUserStringMarker);

    for (UINT32 nIndex = 0; ;)
    {
        MetaData::DataBlob userString;
        UINT32 nNextIndex;
        hr = pMiniMd->GetUserStringAndNextIndex(
            nIndex,
            &userString,
            &nNextIndex);
        IfFailGo(hr);
        if (hr == S_FALSE)
        {   // We reached the last user string
            hr = S_OK;
            break;
        }
        _ASSERTE(hr == S_OK);

        // Skip empty strings
        if (userString.IsEmpty())
        {
            nIndex = nNextIndex;
            continue;
        }
        FilterUserStringEntry *pItem = m_daUserStringMarker->Append();
        pItem->m_tkString = TokenFromRid(nIndex, mdtString);
        pItem->m_fMarked = true;

        // Process next user string in the heap
        nIndex = nNextIndex;
    }

ErrExit:
    return hr;
} // FilterTable::MarkAll

//*****************************************************************************
//
// return true if a token is marked. Otherwise return false.
//
//*****************************************************************************
bool FilterTable::IsTokenMarked(
    mdToken     tk,                         // Token to inquiry
    DWORD       bitMarked)                  // bit flag to check in the deletion table
{
    RID     rid = RidFromToken(tk);

    //<TODO>@FUTURE: inconsistency!!!
    // If caller unmarked everything while the module has 2 typedef and 10 methodef.
    // We will have 11 rows in the FilterTable. Then user add the 3 typedef, it is
    // considered unmarked unless we mark it when we do DefineTypeDef. However, if user
    // add another MethodDef, it will be considered marked unless we unmarked.....
    // Maybe the solution is not to support DefineXXXX if you use the filter interface??</TODO>

    if ( (Count() == 0) || ((RID)(Count() - 1)) < rid )
    {
        // If UnmarkAll has never been called or tk is added after UnmarkAll,
        // tk is considered marked.
        //
        return true;
    }
    return ( (*Get(rid)) & bitMarked ? true : false);
} // FilterTable::IsTokenMarked


//*****************************************************************************
//
// return true if a token is marked. Otherwise return false.
//
//*****************************************************************************
bool FilterTable::IsTokenMarked(
    mdToken     tk)                         // Token to inquiry
{

    switch ( TypeFromToken(tk) )
    {
    case mdtTypeRef:
        return IsTypeRefMarked(tk);
    case mdtTypeDef:
        return IsTypeDefMarked(tk);
    case mdtFieldDef:
        return IsFieldMarked(tk);
    case mdtMethodDef:
        return IsMethodMarked(tk);
    case mdtParamDef:
        return IsParamMarked(tk);
    case mdtMemberRef:
        return IsMemberRefMarked(tk);
    case mdtCustomAttribute:
        return IsCustomAttributeMarked(tk);
    case mdtPermission:
        return IsDeclSecurityMarked(tk);
    case mdtSignature:
        return IsSignatureMarked(tk);
    case mdtEvent:
        return IsEventMarked(tk);
    case mdtProperty:
        return IsPropertyMarked(tk);
    case mdtModuleRef:
        return IsModuleRefMarked(tk);
    case mdtTypeSpec:
        return IsTypeSpecMarked(tk);
    case mdtInterfaceImpl:
        return IsInterfaceImplMarked(tk);
    case mdtMethodSpec:
        return IsMethodSpecMarked(tk);
    case mdtString:
        return IsUserStringMarked(tk);
    default:
        _ASSERTE(!"Bad token type!");
        break;
    }
    return false;
} // FilterTable::IsTokenMarked

//*****************************************************************************
//
// return true if an UserString is marked.
//
//*****************************************************************************
bool FilterTable::IsUserStringMarked(mdString str)
{
    int         low, mid, high, count;

    // if m_daUserStringMarker is not created, UnmarkAll has never been called
    if (m_daUserStringMarker == NULL)
        return true;

    low = 0;
    count = m_daUserStringMarker->Count();

    if (count == 0)
    {
        // No strings are marked.
        return false;
    }

    high = m_daUserStringMarker->Count() - 1;

    while (low <= high)
    {
        mid = (high + low) / 2;
        if ((m_daUserStringMarker->Get(mid))->m_tkString > (DWORD) str)
        {
            high = mid - 1;
        }
        else if ((m_daUserStringMarker->Get(mid))->m_tkString < (DWORD) str)
        {
            low = mid + 1;
        }
        else
        {
            return (m_daUserStringMarker->Get(mid))->m_fMarked;
        }
    }
    _ASSERTE(!"Bad Token!");
    return false;
} // FilterTable::IsUserStringMarked



//*****************************************************************************
//
// destructor
//
//*****************************************************************************
FilterTable::~FilterTable()
{
    if (m_daUserStringMarker)
        delete m_daUserStringMarker;
    Clear();
} // FilterTable::~FilterTable

