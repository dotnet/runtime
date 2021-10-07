// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// MetaModelRW.h -- header file for Read/Write compressed COM+ metadata.
//

//
// Used by Emitters and by E&C.
//
//*****************************************************************************
#ifndef _METAMODELRW_H_
#define _METAMODELRW_H_

#if _MSC_VER >= 1100
 # pragma once
#endif

#include "metamodel.h"                  // Base classes for the MetaModel.
#include "metadatahash.h"
#include "rwutil.h"
#include "shash.h"

#include "../heaps/export.h"
#include "../tables/export.h"

struct HENUMInternal;
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
struct IMDCustomDataSource;
#endif

// ENUM for marking bit
enum
{
    InvalidMarkedBit            = 0x00000000,
    ModuleMarkedBit             = 0x00000001,
    TypeRefMarkedBit            = 0x00000002,
    TypeDefMarkedBit            = 0x00000004,
    FieldMarkedBit              = 0x00000008,
    MethodMarkedBit             = 0x00000010,
    ParamMarkedBit              = 0x00000020,
    MemberRefMarkedBit          = 0x00000040,
    CustomAttributeMarkedBit    = 0x00000080,
    DeclSecurityMarkedBit       = 0x00000100,
    SignatureMarkedBit          = 0x00000200,
    EventMarkedBit              = 0x00000400,
    PropertyMarkedBit           = 0x00000800,
    MethodImplMarkedBit         = 0x00001000,
    ModuleRefMarkedBit          = 0x00002000,
    TypeSpecMarkedBit           = 0x00004000,
    InterfaceImplMarkedBit      = 0x00008000,
    AssemblyRefMarkedBit        = 0x00010000,
    MethodSpecMarkedBit         = 0x00020000,

};

// entry for marking UserString
struct FilterUserStringEntry
{
    DWORD       m_tkString;
    bool        m_fMarked;
};

class FilterTable : public CDynArray<DWORD>
{
public:
    FilterTable() { m_daUserStringMarker = NULL; }
    ~FilterTable();

    __checkReturn FORCEINLINE HRESULT MarkTypeRef(mdToken tk) { return MarkToken(tk, TypeRefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkTypeDef(mdToken tk) { return MarkToken(tk, TypeDefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkField(mdToken tk) { return MarkToken(tk, FieldMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkMethod(mdToken tk) { return MarkToken(tk, MethodMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkParam(mdToken tk) { return MarkToken(tk, ParamMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkMemberRef(mdToken tk) { return MarkToken(tk, MemberRefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkCustomAttribute(mdToken tk) { return MarkToken(tk, CustomAttributeMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkDeclSecurity(mdToken tk) { return MarkToken(tk, DeclSecurityMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkSignature(mdToken tk) { return MarkToken(tk, SignatureMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkEvent(mdToken tk) { return MarkToken(tk, EventMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkProperty(mdToken tk) { return MarkToken(tk, PropertyMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkMethodImpl(RID rid)
    {
        return MarkToken(TokenFromRid(rid, TBL_MethodImpl << 24), MethodImplMarkedBit);
    }
    __checkReturn FORCEINLINE HRESULT MarkModuleRef(mdToken tk) { return MarkToken(tk, ModuleRefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkTypeSpec(mdToken tk) { return MarkToken(tk, TypeSpecMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkInterfaceImpl(mdToken tk) { return MarkToken(tk, InterfaceImplMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkAssemblyRef(mdToken tk) { return MarkToken(tk, AssemblyRefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT MarkMethodSpec(mdToken tk) { return MarkToken(tk, MethodSpecMarkedBit); }

    // It may look inconsistent but it is because taht UserString an offset to the heap.
    // We don't want to grow the FilterTable to the size of the UserString heap.
    // So we use the heap's marking system instead...
    //
    __checkReturn HRESULT MarkUserString(mdString str);

    __checkReturn HRESULT MarkNewUserString(mdString str);

    FORCEINLINE bool IsTypeRefMarked(mdToken tk)    { return IsTokenMarked(tk, TypeRefMarkedBit); }
    FORCEINLINE bool IsTypeDefMarked(mdToken tk) { return IsTokenMarked(tk, TypeDefMarkedBit); }
    FORCEINLINE bool IsFieldMarked(mdToken tk) { return IsTokenMarked(tk, FieldMarkedBit); }
    FORCEINLINE bool IsMethodMarked(mdToken tk) { return IsTokenMarked(tk, MethodMarkedBit); }
    FORCEINLINE bool IsParamMarked(mdToken tk) { return IsTokenMarked(tk, ParamMarkedBit); }
    FORCEINLINE bool IsMemberRefMarked(mdToken tk) { return IsTokenMarked(tk, MemberRefMarkedBit); }
    FORCEINLINE bool IsCustomAttributeMarked(mdToken tk) { return IsTokenMarked(tk, CustomAttributeMarkedBit); }
    FORCEINLINE bool IsDeclSecurityMarked(mdToken tk) { return IsTokenMarked(tk, DeclSecurityMarkedBit); }
    FORCEINLINE bool IsSignatureMarked(mdToken tk) { return IsTokenMarked(tk, SignatureMarkedBit); }
    FORCEINLINE bool IsEventMarked(mdToken tk) { return IsTokenMarked(tk, EventMarkedBit); }
    FORCEINLINE bool IsPropertyMarked(mdToken tk) { return IsTokenMarked(tk, PropertyMarkedBit); }
    FORCEINLINE bool IsMethodImplMarked(RID rid)
    {
        return IsTokenMarked(TokenFromRid(rid, TBL_MethodImpl << 24), MethodImplMarkedBit);
    }
    FORCEINLINE bool IsModuleRefMarked(mdToken tk) { return IsTokenMarked(tk, ModuleRefMarkedBit); }
    FORCEINLINE bool IsTypeSpecMarked(mdToken tk) { return IsTokenMarked(tk, TypeSpecMarkedBit); }
    FORCEINLINE bool IsInterfaceImplMarked(mdToken tk){ return IsTokenMarked(tk, InterfaceImplMarkedBit); }
    FORCEINLINE bool IsAssemblyRefMarked(mdToken tk){ return IsTokenMarked(tk, AssemblyRefMarkedBit); }
    FORCEINLINE bool IsMethodSpecMarked(mdToken tk){ return IsTokenMarked(tk, MethodSpecMarkedBit); }

    bool IsUserStringMarked(mdString str);

    __checkReturn HRESULT UnmarkAll(CMiniMdRW *pMiniMd, ULONG ulSize);
    __checkReturn HRESULT MarkAll(CMiniMdRW *pMiniMd, ULONG ulSize);
    bool IsTokenMarked(mdToken);

    __checkReturn FORCEINLINE HRESULT UnmarkTypeDef(mdToken tk) { return UnmarkToken(tk, TypeDefMarkedBit); }
    __checkReturn FORCEINLINE HRESULT UnmarkField(mdToken tk) { return UnmarkToken(tk, FieldMarkedBit); }
    __checkReturn FORCEINLINE HRESULT UnmarkMethod(mdToken tk) { return UnmarkToken(tk, MethodMarkedBit); }
    __checkReturn FORCEINLINE HRESULT UnmarkCustomAttribute(mdToken tk) { return UnmarkToken(tk, CustomAttributeMarkedBit); }

private:
    CDynArray<FilterUserStringEntry> *m_daUserStringMarker;
    bool            IsTokenMarked(mdToken tk, DWORD bitMarked);
    __checkReturn HRESULT         MarkToken(mdToken tk, DWORD bit);
    __checkReturn HRESULT         UnmarkToken(mdToken tk, DWORD bit);
}; // class FilterTable : public CDynArray<DWORD>

class CMiniMdRW;

//*****************************************************************************
// This class is used to keep a list of RID. This list of RID can be sorted
// base on the m_ixCol's value of the m_ixTbl table.
//*****************************************************************************
class VirtualSort
{
public:
    void Init(ULONG ixTbl, ULONG ixCol, CMiniMdRW *pMiniMd);
    void Uninit();
    TOKENMAP    *m_pMap;                // RID for m_ixTbl table. Sorted by on the ixCol
    bool        m_isMapValid;
    ULONG       m_ixTbl;                // Table this is a sorter for.
    ULONG       m_ixCol;                // Key column in the table.
    CMiniMdRW   *m_pMiniMd;             // The MiniMd with the data.
    __checkReturn
    HRESULT Sort();
private:
    mdToken     m_tkBuf;
    __checkReturn
    HRESULT SortRange(int iLeft, int iRight);
public:
    __checkReturn
    HRESULT Compare(
        RID  iLeft,         // First item to compare.
        RID  iRight,        // Second item to compare.
        int *pnResult);     // -1, 0, or 1

private:
    FORCEINLINE void Swap(
        RID         iFirst,
        RID         iSecond)
    {
        if ( iFirst == iSecond ) return;
        m_tkBuf = *(m_pMap->Get(iFirst));
        *(m_pMap->Get(iFirst)) = *(m_pMap->Get(iSecond));
        *(m_pMap->Get(iSecond)) = m_tkBuf;
    }


}; // class VirtualSort

class ReorderData
{
public:
    typedef enum
    {
        MinReorderBucketType=0,  // bucket# shouldn't be less than this value
        Undefined=0,             // use this for initialization
        Duplicate=1,             // duplicate string
        ProfileData=2,           // bucket# for IBC data
        PublicData=3,            // bucket# for public data
        OtherData=4,             // bucket# for other data
        NonPublicData=5,         // bucket# for non-public data
        MaxReorderBucketType=255 // bucket# shouldn't exceeed this value
    } ReorderBucketType;
};

typedef CMetaDataHashBase CMemberRefHash;
typedef CMetaDataHashBase CLookUpHash;

class MDTOKENMAP;
class MDInternalRW;
class CorProfileData;
class UTSemReadWrite;

template <class MiniMd> class CLiteWeightStgdb;
//*****************************************************************************
// Read/Write MiniMd.
//*****************************************************************************
class CMiniMdRW : public CMiniMdTemplate<CMiniMdRW>
{
public:
    friend class CLiteWeightStgdb<CMiniMdRW>;
    friend class CLiteWeightStgdbRW;
    friend class CMiniMdTemplate<CMiniMdRW>;
    friend class CQuickSortMiniMdRW;
    friend class VirtualSort;
    friend class MDInternalRW;
    friend class RegMeta;
    friend class FilterTable;
    friend class ImportHelper;
    friend class VerifyLayoutsMD;

    CMiniMdRW();
    ~CMiniMdRW();

    __checkReturn
    HRESULT InitNew();
    __checkReturn
    HRESULT InitOnMem(const void *pBuf, ULONG ulBufLen, int bReadOnly);
    __checkReturn
    HRESULT PostInit(int iLevel);
    __checkReturn
    HRESULT InitPoolOnMem(int iPool, void *pbData, ULONG cbData, int bReadOnly);
    __checkReturn
    HRESULT InitOnRO(CMiniMd *pMd, int bReadOnly);
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    __checkReturn
    HRESULT InitOnCustomDataSource(IMDCustomDataSource* pDataSouce);
#endif
    __checkReturn
    HRESULT ConvertToRW();

    __checkReturn
    HRESULT GetSaveSize(
        CorSaveSize               fSave,
        UINT32                   *pcbSize,
        DWORD                    *pbCompressed,
        MetaDataReorderingOptions reorderingOptions = NoReordering,
        CorProfileData           *pProfileData = NULL);
    int IsPoolEmpty(int iPool);
    __checkReturn
    HRESULT GetPoolSaveSize(int iPool, UINT32 *pcbSize);

    __checkReturn
    HRESULT SaveTablesToStream(IStream *pIStream, MetaDataReorderingOptions reorderingOptions, CorProfileData *pProfileData);
    __checkReturn
    HRESULT SavePoolToStream(int iPool, IStream *pIStream);
    __checkReturn
    HRESULT SaveDone();

    __checkReturn
    HRESULT SetHandler(IUnknown *pIUnk);

    __checkReturn
    HRESULT SetOption(OptionValue *pOptionValue);
    __checkReturn
    HRESULT GetOption(OptionValue *pOptionValue);

    static ULONG GetTableForToken(mdToken tkn);
    static mdToken GetTokenForTable(ULONG ixTbl);

    FORCEINLINE static ULONG TblFromRecId(ULONG ul) { return (ul >> 24)&0x7f; }
    FORCEINLINE static ULONG RidFromRecId(ULONG ul) { return ul & 0xffffff; }
    FORCEINLINE static ULONG RecIdFromRid(ULONG rid, ULONG ixTbl) { return rid | ((ixTbl|0x80) << 24); }
    FORCEINLINE static int IsRecId(ULONG ul) { return (ul & 0x80000000) != 0;}

    // Place in every API function before doing any allocations.
    __checkReturn
    FORCEINLINE HRESULT PreUpdate()
    {
        if (m_eGrow == eg_grow)
        {
            return ExpandTables();
        }
        return S_OK;
    }

    __checkReturn
    HRESULT AddRecord(
        UINT32 nTableIndex,
        void **ppRow,
        RID   *pRid);

    __checkReturn
    FORCEINLINE HRESULT PutCol(ULONG ixTbl, ULONG ixCol, void *pRecord, ULONG uVal)
    {   _ASSERTE(ixTbl < TBL_COUNT); _ASSERTE(ixCol < m_TableDefs[ixTbl].m_cCols);
        return PutCol(m_TableDefs[ixTbl].m_pColDefs[ixCol], pRecord, uVal);
    } // HRESULT CMiniMdRW::PutCol()
    __checkReturn
    HRESULT PutString(ULONG ixTbl, ULONG ixCol, void *pRecord, LPCSTR szString);
    __checkReturn
    HRESULT PutStringW(ULONG ixTbl, ULONG ixCol, void *pRecord, LPCWSTR wszString);
    __checkReturn
    HRESULT PutGuid(ULONG ixTbl, ULONG ixCol, void *pRecord, REFGUID guid);
    __checkReturn
    HRESULT ChangeMvid(REFGUID newMvid);
    __checkReturn
    HRESULT PutToken(ULONG ixTbl, ULONG ixCol, void *pRecord, mdToken tk);
    __checkReturn
    HRESULT PutBlob(ULONG ixTbl, ULONG ixCol, void *pRecord, const void *pvData, ULONG cbData);

    __checkReturn
    HRESULT PutUserString(MetaData::DataBlob data, UINT32 *pnIndex)
    { return m_UserStringHeap.AddBlob(data, pnIndex); }

    ULONG GetCol(ULONG ixTbl, ULONG ixCol, void *pRecord);
    mdToken GetToken(ULONG ixTbl, ULONG ixCol, void *pRecord);

    // Add a record to a table, and return a typed XXXRec *.
//  #undef AddTblRecord
    #define AddTblRecord(tbl) \
        __checkReturn HRESULT Add##tbl##Record(tbl##Rec **ppRow, RID *pnRowIndex)   \
        {   return AddRecord(TBL_##tbl, reinterpret_cast<void **>(ppRow), pnRowIndex); }

    AddTblRecord(Module)
    AddTblRecord(TypeRef)
    __checkReturn HRESULT AddTypeDefRecord( // Specialized implementation.
        TypeDefRec **ppRow,
        RID         *pnRowIndex);
    AddTblRecord(Field)
    __checkReturn HRESULT AddMethodRecord(  // Specialized implementation.
        MethodRec **ppRow,
        RID        *pnRowIndex);
    AddTblRecord(Param)
    AddTblRecord(InterfaceImpl)
    AddTblRecord(MemberRef)
    AddTblRecord(Constant)
    AddTblRecord(CustomAttribute)
    AddTblRecord(FieldMarshal)
    AddTblRecord(DeclSecurity)
    AddTblRecord(ClassLayout)
    AddTblRecord(FieldLayout)
    AddTblRecord(StandAloneSig)
    __checkReturn HRESULT AddEventMapRecord(    // Specialized implementation.
        EventMapRec **ppRow,
        RID          *pnRowIndex);
    AddTblRecord(Event)
    __checkReturn HRESULT AddPropertyMapRecord( // Specialized implementation.
        PropertyMapRec **ppRow,
        RID             *pnRowIndex);
    AddTblRecord(Property)
    AddTblRecord(MethodSemantics)
    AddTblRecord(MethodImpl)
    AddTblRecord(ModuleRef)
    AddTblRecord(FieldPtr)
    AddTblRecord(MethodPtr)
    AddTblRecord(ParamPtr)
    AddTblRecord(PropertyPtr)
    AddTblRecord(EventPtr)

    AddTblRecord(ENCLog)
    AddTblRecord(TypeSpec)
    AddTblRecord(ImplMap)
    AddTblRecord(ENCMap)
    AddTblRecord(FieldRVA)

    // Assembly Tables.
    AddTblRecord(Assembly)
    AddTblRecord(AssemblyProcessor)
    AddTblRecord(AssemblyOS)
    AddTblRecord(AssemblyRef)
    AddTblRecord(AssemblyRefProcessor)
    AddTblRecord(AssemblyRefOS)
    AddTblRecord(File)
    AddTblRecord(ExportedType)
    AddTblRecord(ManifestResource)

    AddTblRecord(NestedClass)
    AddTblRecord(GenericParam)
    AddTblRecord(MethodSpec)
    AddTblRecord(GenericParamConstraint)

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    AddTblRecord(Document)
    AddTblRecord(MethodDebugInformation)
    AddTblRecord(LocalScope)
    AddTblRecord(LocalVariable)
    AddTblRecord(LocalConstant)
    AddTblRecord(ImportScope)
    // TODO:
    // AddTblRecord(StateMachineMethod)
    // AddTblRecord(CustomDebugInformation)
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

    // Specialized AddXxxToYyy() functions.
    __checkReturn HRESULT AddMethodToTypeDef(RID td, RID md);
    __checkReturn HRESULT AddFieldToTypeDef(RID td, RID md);
    __checkReturn HRESULT AddParamToMethod(RID md, RID pd);
    __checkReturn HRESULT AddPropertyToPropertyMap(RID pmd, RID pd);
    __checkReturn HRESULT AddEventToEventMap(ULONG emd, RID ed);

    // does the MiniMdRW has the indirect tables, such as FieldPtr, MethodPtr
    FORCEINLINE int HasIndirectTable(ULONG ix)
    { if (g_PtrTableIxs[ix].m_ixtbl < TBL_COUNT) return GetCountRecs(g_PtrTableIxs[ix].m_ixtbl); return 0;}

    FORCEINLINE int IsVsMapValid(ULONG ixTbl)
    { _ASSERTE(ixTbl<TBL_COUNT); return (m_pVS[ixTbl] && m_pVS[ixTbl]->m_isMapValid); }

    // translate index returned by getMethodListOfTypeDef to a rid into Method table
    __checkReturn
    FORCEINLINE HRESULT GetMethodRid(ULONG index, RID *pRid)
    {
        HRESULT hr;
        if (HasIndirectTable(TBL_Method))
        {
            MethodPtrRec *pMethodPtrRecord;
            IfFailGo(GetMethodPtrRecord(index, &pMethodPtrRecord));
            *pRid = getMethodOfMethodPtr(pMethodPtrRecord);
        }
        else
        {
            *pRid = index;
        }
        return S_OK;
    ErrExit:
        *pRid = 0;
        return hr;
    }

    // translate index returned by getFieldListOfTypeDef to a rid into Field table
    __checkReturn
    FORCEINLINE HRESULT GetFieldRid(ULONG index, RID *pRid)
    {
        HRESULT hr;
        if (HasIndirectTable(TBL_Field))
        {
            FieldPtrRec *pFieldPtrRecord;
            IfFailGo(GetFieldPtrRecord(index, &pFieldPtrRecord));
            *pRid = getFieldOfFieldPtr(pFieldPtrRecord);
        }
        else
        {
            *pRid = index;
        }
        return S_OK;
    ErrExit:
        *pRid = 0;
        return hr;
    }

    // translate index returned by getParamListOfMethod to a rid into Param table
    __checkReturn
    FORCEINLINE HRESULT GetParamRid(ULONG index, RID *pRid)
    {
        HRESULT hr;
        if (HasIndirectTable(TBL_Param))
        {
            ParamPtrRec *pParamPtrRecord;
            IfFailGo(GetParamPtrRecord(index, &pParamPtrRecord));
            *pRid = getParamOfParamPtr(pParamPtrRecord);
        }
        else
        {
            *pRid = index;
        }
        return S_OK;
    ErrExit:
        *pRid = 0;
        return hr;
    }

    // translate index returned by getEventListOfEventMap to a rid into Event table
    __checkReturn
    FORCEINLINE HRESULT GetEventRid(ULONG index, RID *pRid)
    {
        HRESULT hr;
        if (HasIndirectTable(TBL_Event))
        {
            EventPtrRec *pEventPtrRecord;
            IfFailGo(GetEventPtrRecord(index, &pEventPtrRecord));
            *pRid = getEventOfEventPtr(pEventPtrRecord);
        }
        else
        {
            *pRid = index;
        }
        return S_OK;
    ErrExit:
        *pRid = 0;
        return hr;
    }

    // translate index returned by getPropertyListOfPropertyMap to a rid into Property table
    __checkReturn
    FORCEINLINE HRESULT GetPropertyRid(ULONG index, RID *pRid)
    {
        HRESULT hr;
        if (HasIndirectTable(TBL_Property))
        {
            PropertyPtrRec *pPropertyPtrRecord;
            IfFailGo(GetPropertyPtrRecord(index, &pPropertyPtrRecord));
            *pRid = getPropertyOfPropertyPtr(pPropertyPtrRecord);
        }
        else
        {
            *pRid = index;
        }
        return S_OK;
    ErrExit:
        *pRid = 0;
        return hr;
    }

    // Convert a pseudo-RID from a Virtual Sort into a real RID.
    FORCEINLINE ULONG GetRidFromVirtualSort(ULONG ixTbl, ULONG index)
    { return IsVsMapValid(ixTbl) ? *(m_pVS[ixTbl]->m_pMap->Get(index)) : index; }

    // Index returned by GetInterfaceImplForTypeDef. It could be index to VirtualSort table
    // or directly to InterfaceImpl
    FORCEINLINE ULONG GetInterfaceImplRid(ULONG index)
    { return GetRidFromVirtualSort(TBL_InterfaceImpl, index); }

    // Index returned by GetGenericParamForToken. It could be index to VirtualSort table
    // or directly to GenericParam
    FORCEINLINE ULONG GetGenericParamRid(ULONG index)
    { return GetRidFromVirtualSort(TBL_GenericParam, index); }

    // Index returned by GetGenericParamConstraintForToken. It could be index to VirtualSort table
    // or directly to GenericParamConstraint
    FORCEINLINE ULONG GetGenericParamConstraintRid(ULONG index)
    { return GetRidFromVirtualSort(TBL_GenericParamConstraint, index); }

    // Index returned by GetDeclSecurityForToken. It could be index to VirtualSort table
    // or directly to DeclSecurity
    FORCEINLINE ULONG GetDeclSecurityRid(ULONG index)
    { return GetRidFromVirtualSort(TBL_DeclSecurity, index); }

    // Index returned by GetCustomAttributeForToken. It could be index to VirtualSort table
    // or directly to CustomAttribute
    FORCEINLINE ULONG GetCustomAttributeRid(ULONG index)
    { return GetRidFromVirtualSort(TBL_CustomAttribute, index); }

    // add method, field, property, event, param to the map table
    __checkReturn HRESULT AddMethodToLookUpTable(mdMethodDef md, mdTypeDef td);
    __checkReturn HRESULT AddFieldToLookUpTable(mdFieldDef fd, mdTypeDef td);
    __checkReturn HRESULT AddPropertyToLookUpTable(mdProperty pr, mdTypeDef td);
    __checkReturn HRESULT AddEventToLookUpTable(mdEvent ev, mdTypeDef td);
    __checkReturn HRESULT AddParamToLookUpTable(mdParamDef pd, mdMethodDef md);

    // look up the parent of method, field, property, event, or param
    __checkReturn HRESULT FindParentOfMethodHelper(mdMethodDef md, mdTypeDef *ptd);
    __checkReturn HRESULT FindParentOfFieldHelper(mdFieldDef fd, mdTypeDef *ptd);
    __checkReturn HRESULT FindParentOfPropertyHelper(mdProperty pr, mdTypeDef *ptd);
    __checkReturn HRESULT FindParentOfEventHelper(mdEvent ev, mdTypeDef *ptd);
    __checkReturn HRESULT FindParentOfParamHelper(mdParamDef pd, mdMethodDef *pmd);

    bool IsMemberDefHashPresent() { return m_pMemberDefHash != NULL; }

    // Result of hash search
    enum HashSearchResult
    {
        Found,      // Item was found.
        NotFound,   // Item not found.
        NoTable     // Table hasn't been built.
    };

    // Create MemberRef hash table.
    __checkReturn
    HRESULT CreateMemberRefHash();

    // Add a new MemberRef to the hash table.
    __checkReturn
    HRESULT AddMemberRefToHash(             // Return code.
        mdMemberRef mr);                    // Token of new MemberRef.

    // If the hash is built, search for the item. Ignore token *ptkMemberRef.
    HashSearchResult FindMemberRefFromHash(
        mdToken         tkParent,       // Parent token.
        LPCUTF8         szName,         // Name of item.
        PCCOR_SIGNATURE pvSigBlob,      // Signature.
        ULONG           cbSigBlob,      // Size of signature.
        mdMemberRef *   ptkMemberRef);  // IN: Ignored token. OUT: Return if found.

    //*************************************************************************
    // Check a given mr token to see if this one is a match.
    //*************************************************************************
    __checkReturn
    HRESULT CompareMemberRefs(              // S_OK match, S_FALSE no match.
        mdMemberRef mr,                     // Token to check.
        mdToken     tkPar,                  // Parent token.
        LPCUTF8     szNameUtf8,             // Name of item.
        PCCOR_SIGNATURE pvSigBlob,          // Signature.
        ULONG       cbSigBlob);             // Size of signature.

    // Add a new MemberDef to the hash table.
    __checkReturn
    HRESULT AddMemberDefToHash(
        mdToken tkMember,   // Token of new def. It can be MethodDef or FieldDef
        mdToken tkParent);  // Parent token.

    // Create MemberDef Hash
    __checkReturn
    HRESULT CreateMemberDefHash();

    // If the hash is built, search for the item. Ignore token *ptkMember.
    HashSearchResult FindMemberDefFromHash(
        mdToken         tkParent,   // Parent token.
        LPCUTF8         szName,     // Name of item.
        PCCOR_SIGNATURE pvSigBlob,  // Signature.
        ULONG           cbSigBlob,  // Size of signature.
        mdToken *       ptkMember); // IN: Ignored token. OUT: Return if found. It can be MethodDef or FieldDef

    //*************************************************************************
    // Check a given Method/Field token to see if this one is a match.
    //*************************************************************************
    __checkReturn
    HRESULT CompareMemberDefs(              // S_OK match, S_FALSE no match.
        mdToken     tkMember,               // Token to check. It can be MethodDef or FieldDef
        mdToken     tkParent,               // Parent token recorded in the hash entry
        mdToken     tkPar,                  // Parent token.
        LPCUTF8     szNameUtf8,             // Name of item.
        PCCOR_SIGNATURE pvSigBlob,          // Signature.
        ULONG       cbSigBlob);             // Size of signature.

    //*************************************************************************
    // Add a new CustomAttributes to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddCustomAttributesToHash(      // Return code.
        mdCustomAttribute     cv)           // Token of new guy.
    { return GenericAddToHash(TBL_CustomAttribute, CustomAttributeRec::COL_Parent, RidFromToken(cv)); }

    inline ULONG HashMemberRef(mdToken tkPar, LPCUTF8 szName)
    {
        ULONG l = HashBytes((const BYTE *) &tkPar, sizeof(mdToken)) + HashStringA(szName);
        return (l);
    }

    inline ULONG HashMemberDef(mdToken tkPar, LPCUTF8 szName)
    {
        return HashMemberRef(tkPar, szName);
    }

    // helper to calculate the hash value given a token
    inline ULONG HashCustomAttribute(mdToken tkObject)
    {
        return HashToken(tkObject);
    }

    DAC_ALIGNAS(CMiniMdTemplate<CMiniMdRW>) // Align the first member to the alignment of the base class
    CMemberRefHash *m_pMemberRefHash;

    // Hash table for Methods and Fields
    CMemberDefHash *m_pMemberDefHash;

    // helper to calculate the hash value given a pair of tokens
    inline ULONG HashToken(mdToken tkObject)
    {
        ULONG l = HashBytes((const BYTE *) &tkObject, sizeof(mdToken));
        return (l);
    }


    //*************************************************************************
    // Add a new FieldMarhsal Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddFieldMarshalToHash(          // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_FieldMarshal, FieldMarshalRec::COL_Parent, rid); }

    //*************************************************************************
    // Add a new Constant Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddConstantToHash(              // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_Constant, ConstantRec::COL_Parent, rid); }

    //*************************************************************************
    // Add a new MethodSemantics Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddMethodSemanticsToHash(       // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_MethodSemantics, MethodSemanticsRec::COL_Association, rid); }

    //*************************************************************************
    // Add a new ClassLayout Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddClassLayoutToHash(           // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_ClassLayout, ClassLayoutRec::COL_Parent, rid); }

    //*************************************************************************
    // Add a new FieldLayout Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddFieldLayoutToHash(           // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_FieldLayout, FieldLayoutRec::COL_Field, rid); }

    //*************************************************************************
    // Add a new ImplMap Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddImplMapToHash(               // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_ImplMap, ImplMapRec::COL_MemberForwarded, rid); }

    //*************************************************************************
    // Add a new FieldRVA Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddFieldRVAToHash(              // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_FieldRVA, FieldRVARec::COL_Field, rid); }

    //*************************************************************************
    // Add a new nested class Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddNestedClassToHash(           // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_NestedClass, NestedClassRec::COL_NestedClass, rid); }

    //*************************************************************************
    // Add a new MethodImpl Rid to the hash table.
    //*************************************************************************
    __checkReturn
    HRESULT AddMethodImplToHash(           // Return code.
        RID         rid)                    // Token of new guy.
    { return GenericAddToHash(TBL_MethodImpl, MethodImplRec::COL_Class, rid); }


    //*************************************************************************
    // Build a hash table for the specified table if the size exceed the thresholds.
    //*************************************************************************
    __checkReturn
    HRESULT GenericBuildHashTable(          // Return code.
        ULONG       ixTbl,                  // Table with hash
        ULONG       ixCol);                 // col that we hash.

    //*************************************************************************
    // Add a rid from a table into a hash
    //*************************************************************************
    __checkReturn
    HRESULT GenericAddToHash(               // Return code.
        ULONG       ixTbl,                  // Table with hash
        ULONG       ixCol,                  // col that we hash.
        RID         rid);                   // new row of the table.

    //*************************************************************************
    // Add a rid from a table into a hash
    //*************************************************************************
    __checkReturn
    HRESULT GenericFindWithHash(                // Return code.
        ULONG       ixTbl,                  // Table with hash
        ULONG       ixCol,                  // col that we hash.
        mdToken     tkTarget,               // token to be find in the hash
        RID        *pFoundRid);


    // look up hash table for tokenless tables.
    // They are constant, FieldMarshal, MethodSemantics, ClassLayout, FieldLayout, ImplMap, FieldRVA, NestedClass, and MethodImpl
    CLookUpHash * m_pLookUpHashs[TBL_COUNT];

    //*************************************************************************
    // Hash for named items.
    //*************************************************************************
    __checkReturn
    HRESULT AddNamedItemToHash(             // Return code.
        ULONG       ixTbl,                  // Table with the new item.
        mdToken     tk,                     // Token of new guy.
        LPCUTF8     szName,                 // Name of item.
        mdToken     tkParent);              // Token of parent, if any.

    HashSearchResult FindNamedItemFromHash(
        ULONG     ixTbl,    // Table with the item.
        LPCUTF8   szName,   // Name of item.
        mdToken   tkParent, // Token of parent, if any.
        mdToken * ptk);     // Return if found.

    __checkReturn
    HRESULT CompareNamedItems(              // S_OK match, S_FALSE no match.
        ULONG       ixTbl,                  // Table with the item.
        mdToken     tk,                     // Token to check.
        LPCUTF8     szName,                 // Name of item.
        mdToken     tkParent);              // Token of parent, if any.

    FORCEINLINE ULONG HashNamedItem(mdToken tkPar, LPCUTF8 szName)
    {   return HashBytes((const BYTE *) &tkPar, sizeof(mdToken)) + HashStringA(szName); }

    CMetaDataHashBase *m_pNamedItemHash;

    //*****************************************************************************
    // IMetaModelCommon - RW specific versions for some of the functions.
    //*****************************************************************************
    __checkReturn
    virtual HRESULT CommonGetEnclosingClassOfTypeDef(
        mdTypeDef  td,
        mdTypeDef *ptkEnclosingTypeDef)
    {
        _ASSERTE(ptkEnclosingTypeDef != NULL);

        HRESULT hr;
        NestedClassRec *pRec;
        RID         iRec;

        IfFailRet(FindNestedClassHelper(td, &iRec));
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
    HRESULT CommonEnumCustomAttributeByName( // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        bool        fStopAtFirstFind,       // [IN] just find the first one
        HENUMInternal* phEnum);             // enumerator to fill up

    __checkReturn
    HRESULT CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,             // [IN] Object with Custom Attribute.
        LPCUTF8            szName,            // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,             // [OUT] put custom attribute token here
        const void       **ppData,            // [OUT] Put pointer to data here.
        ULONG             *pcbData);          // [OUT] Put size of data here.

    //*****************************************************************************
    // Find helper for a constant.
    //*****************************************************************************
    __checkReturn
    HRESULT FindConstantHelper(         // return index to the constant table
        mdToken  tkParent,              // Parent token. Can be ParamDef, FieldDef, or Property.
        RID     *pFoundRid);

    //*****************************************************************************
    // Find helper for a FieldMarshal.
    //*****************************************************************************
    __checkReturn
    HRESULT FindFieldMarshalHelper(     // return index to the field marshal table
        mdToken tkParent,               // Parent token. Can be a FieldDef or ParamDef.
        RID    *pFoundRid);

    //*****************************************************************************
    // Find helper for a method semantics.
    //*****************************************************************************
    __checkReturn
    HRESULT FindMethodSemanticsHelper(      // return HRESULT
        mdToken     tkAssociate,            // Event or property token
        HENUMInternal *phEnum);             // fill in the enum

    //*****************************************************************************
    // Find helper for a method semantics given a associate and semantics.
    // This will look up methodsemantics based on its status!
    // Return CLDB_E_RECORD_NOTFOUND if cannot find the matching one
    //*****************************************************************************
    __checkReturn
    HRESULT FindAssociateHelper(// return HRESULT
        mdToken     tkAssociate,            // Event or property token
        DWORD       dwSemantics,            // [IN] given a associate semantics(setter, getter, testdefault, reset)
        RID         *pRid);                 // [OUT] return matching row index here

    //*****************************************************************************
    // Find helper for a MethodImpl.
    //*****************************************************************************
    __checkReturn
    HRESULT FindMethodImplHelper(// return HRESULT
        mdTypeDef   td,                     // TypeDef token for the Class.
        HENUMInternal *phEnum);             // fill in the enum

    //*****************************************************************************
    // Find helper for a GenericParams
    //*****************************************************************************
    __checkReturn
    HRESULT FindGenericParamHelper(         // Return HRESULT
        mdToken     tkOwner,                // Token for the GenericParams' owner
        HENUMInternal *phEnum);             // Fill in the enum.

    //*****************************************************************************
    // Find helper for a Generic Constraints
    //*****************************************************************************
    __checkReturn
    HRESULT FindGenericParamConstraintHelper(    // Return HRESULT
        mdGenericParam tkParam,             // Token for the GenericParam
        HENUMInternal *phEnum);             // Fill in the enum.

    //*****************************************************************************
    // Find helper for a ClassLayout.
    //*****************************************************************************
    __checkReturn
    HRESULT FindClassLayoutHelper(      // return index to the ClassLayout table
        mdTypeDef tkParent,             // Parent token.
        RID      *pFoundRid);

    //*****************************************************************************
    // Find helper for a FieldLayout.
    //*****************************************************************************
    __checkReturn
    HRESULT FindFieldLayoutHelper(  // return index to the FieldLayout table
        mdFieldDef tkField,         // Token for the field.
        RID       *pFoundRid);

    //*****************************************************************************
    // Find helper for a ImplMap.
    //*****************************************************************************
    __checkReturn
    HRESULT FindImplMapHelper(  // return index to the constant table
        mdToken tk,             // Member forwarded token.
        RID    *pFoundRid);

    //*****************************************************************************
    // Find helper for a FieldRVA.
    //*****************************************************************************
    __checkReturn
    HRESULT FindFieldRVAHelper(     // return index to the FieldRVA table
        mdFieldDef tkField,         // Token for the field.
        RID       *pFoundRid);

    //*****************************************************************************
    // Find helper for a NestedClass.
    //*****************************************************************************
    __checkReturn
    HRESULT FindNestedClassHelper(  // return index to the NestedClass table
        mdTypeDef tkClass,          // Token for the NestedClass.
        RID      *pFoundRid);

    //*****************************************************************************
    // IMPORTANT!!!!!!!! Use these set of functions if you are dealing with RW rather
    // getInterfaceImplsForTypeDef, getDeclSecurityForToken, etc.
    // The following functions can deal with these tables when they are not sorted and
    // build the VirtualSort tables for quick lookup.
    //*****************************************************************************
    __checkReturn
    HRESULT GetInterfaceImplsForTypeDef(mdTypeDef td, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol( RidFromToken(td), m_pVS[TBL_InterfaceImpl], pRidStart, pRidEnd);
    }

    __checkReturn
    HRESULT GetGenericParamsForToken(mdToken tk, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol(
            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtTypeOrMethodDef, lengthof(mdtTypeOrMethodDef)),
            m_pVS[TBL_GenericParam], pRidStart, pRidEnd);
    }

    __checkReturn
    HRESULT GetGenericParamConstraintsForToken(mdToken tk, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol( RidFromToken(tk),
            m_pVS[TBL_GenericParamConstraint], pRidStart, pRidEnd);
    }

    __checkReturn
    HRESULT GetMethodSpecsForToken(mdToken tk, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol(
            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtMethodDefOrRef, lengthof(mdtMethodDefOrRef)),
            m_pVS[TBL_MethodSpec], pRidStart, pRidEnd);
    }

    __checkReturn
    HRESULT GetDeclSecurityForToken(mdToken tk, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol(
            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtHasDeclSecurity, lengthof(mdtHasDeclSecurity)),
            m_pVS[TBL_DeclSecurity],
            pRidStart,
            pRidEnd);
    }

    __checkReturn
    HRESULT GetCustomAttributeForToken(mdToken tk, RID *pRidStart, RID *pRidEnd = 0)
    {
        return LookUpTableByCol(
            encodeToken(RidFromToken(tk), TypeFromToken(tk), mdtHasCustomAttribute, lengthof(mdtHasCustomAttribute)),
            m_pVS[TBL_CustomAttribute],
            pRidStart,
            pRidEnd);
    }

    __checkReturn
    FORCEINLINE HRESULT GetUserString(ULONG nIndex, MetaData::DataBlob *pData)
    { return m_UserStringHeap.GetBlob(nIndex, pData); }
    // Gets user string (*Data) at index (nIndex) and fills the index (*pnNextIndex) of the next user string
    // in the heap.
    // Returns S_OK and fills the string (*pData) and the next index (*pnNextIndex).
    // Returns S_FALSE if the index (nIndex) is not valid user string index.
    // Returns error code otherwise.
    // Clears *pData and sets *pnNextIndex to 0 on error or S_FALSE.
    __checkReturn
    HRESULT GetUserStringAndNextIndex(
        UINT32              nIndex,
        MetaData::DataBlob *pData,
        UINT32             *pnNextIndex);

    FORCEINLINE int IsSorted(ULONG ixTbl) { return m_Schema.IsSorted(ixTbl);}
    FORCEINLINE int IsSortable(ULONG ixTbl) { return m_bSortable[ixTbl];}
    FORCEINLINE bool HasDelete() { return ((m_Schema.m_heaps & CMiniMdSchema::HAS_DELETE) ? true : false); }
    FORCEINLINE int IsPreSaveDone() { return m_bPreSaveDone; }

protected:
    __checkReturn HRESULT PreSave(MetaDataReorderingOptions reorderingOptions=NoReordering, CorProfileData *pProfileData=NULL);
    __checkReturn HRESULT PostSave();

    __checkReturn HRESULT PreSaveFull();
    __checkReturn HRESULT PreSaveEnc();

    __checkReturn HRESULT GetFullPoolSaveSize(int iPool, UINT32 *pcbSize);
    __checkReturn HRESULT GetENCPoolSaveSize(int iPool, UINT32 *pcbSize);

    __checkReturn HRESULT SaveFullPoolToStream(int iPool, IStream *pIStream);
    __checkReturn HRESULT SaveENCPoolToStream(int iPool, IStream *pIStream);

    __checkReturn
    HRESULT GetFullSaveSize(
        CorSaveSize               fSave,
        UINT32                   *pcbSize,
        DWORD                    *pbCompressed,
        MetaDataReorderingOptions reorderingOptions = NoReordering,
        CorProfileData           *pProfileData = NULL);
    __checkReturn
    HRESULT GetENCSaveSize(UINT32 *pcbSize);
    __checkReturn
    HRESULT GetHotPoolsSaveSize(
        UINT32                   *pcbSize,
        MetaDataReorderingOptions reorderingOptions,
        CorProfileData           *pProfileData);

    __checkReturn
    HRESULT SaveFullTablesToStream(IStream *pIStream, MetaDataReorderingOptions reorderingOptions=NoReordering, CorProfileData *pProfileData = NULL );
    __checkReturn
    HRESULT SaveENCTablesToStream(IStream *pIStream);

    // TO ELIMINATE:
    __checkReturn
    HRESULT AddGuid(REFGUID pGuid, UINT32 *pnIndex)
    { return m_GuidHeap.AddGuid(&pGuid, pnIndex); }

    // Allows putting into tables outside this MiniMd, specifically the temporary
    //  table used on save.
    __checkReturn
    HRESULT PutCol(CMiniColDef ColDef, void *pRecord, ULONG uVal);

    // Returns TRUE if token (tk) is valid.
    // For user strings, consideres 0 as valid token.
    BOOL _IsValidToken(
        mdToken tk)         // [IN] token to be checked
    {
        if (TypeFromToken(tk) == mdtString)
        {
            // need to check the user string heap
            return m_UserStringHeap.IsValidIndex(RidFromToken(tk));
        }
        // Base type doesn't know about user string blob (yet)
        return _IsValidTokenBase(tk);
    } // CMiniMdRW::_IsValidToken

#ifdef _DEBUG
    bool CanHaveCustomAttribute(ULONG ixTbl);
#endif

    __checkReturn
    HRESULT ExpandTables();
    __checkReturn
    HRESULT ExpandTableColumns(CMiniMdSchema &Schema, ULONG ixTbl);

    __checkReturn
    HRESULT InitWithLargeTables();

    void ComputeGrowLimits(int bSmall=TRUE); // Set max, lim, based on param.
    ULONG       m_maxRid;               // Highest RID so far allocated.
    ULONG       m_limRid;               // Limit on RID before growing.
    ULONG       m_maxIx;                // Highest pool index so far.
    ULONG       m_limIx;                // Limit on pool index before growing.
    enum        {eg_ok, eg_grow, eg_grown} m_eGrow; // Is a grow required? done?
    #define AUTO_GROW_CODED_TOKEN_PADDING 5

    // fix up these tables after PreSave has move the tokens
    __checkReturn HRESULT FixUpTable(ULONG ixTbl);
    __checkReturn HRESULT FixUpRefToDef();

    // Table info.
    MetaData::TableRW m_Tables[TBL_COUNT];
    VirtualSort *m_pVS[TBL_COUNT];      // Virtual sorters, one per table, but sparse.

    //*****************************************************************************
    // look up a table by a col given col value is ulVal.
    //*****************************************************************************
    __checkReturn
    HRESULT LookUpTableByCol(
        ULONG       ulVal,
        VirtualSort *pVSTable,
        RID         *pRidStart,
        RID         *pRidEnd);

    __checkReturn
    HRESULT Impl_SearchTableRW(ULONG ixTbl, ULONG ixCol, ULONG ulTarget, RID *pFoundRid);
    __checkReturn
    virtual HRESULT vSearchTable(ULONG ixTbl, CMiniColDef sColumn, ULONG ulTarget, RID *pRid);
    __checkReturn
    virtual HRESULT vSearchTableNotGreater(ULONG ixTbl, CMiniColDef sColumn, ULONG ulTarget, RID *pRid);

    void SetSorted(ULONG ixTbl, int bSorted)
        { m_Schema.SetSorted(ixTbl, bSorted); }

    void SetPreSaveDone(int bPreSaveDone)
        { m_bPreSaveDone = bPreSaveDone; }

    // Heaps
    MetaData::StringHeapRW m_StringHeap;
    MetaData::BlobHeapRW   m_BlobHeap;
    MetaData::BlobHeapRW   m_UserStringHeap;
    MetaData::GuidHeapRW   m_GuidHeap;

    IMapToken  *m_pHandler;     // Remap handler.
    __checkReturn HRESULT MapToken(RID from, RID to, mdToken type);

    ULONG m_cbSaveSize;         // Estimate of save size.

    int m_fIsReadOnly : 1;      // Is this db read-only?
    int m_bPreSaveDone : 1;     // Has save optimization been done?
    int m_bSaveCompressed : 1;  // Can the data be saved as fully compressed?
    int m_bPostGSSMod : 1;      // true if a change was made post GetSaveSize.


    //*************************************************************************
    // Overridables -- must be provided in derived classes.
    __checkReturn
    FORCEINLINE HRESULT Impl_GetString(UINT32 nIndex, __out LPCSTR *pszString)
    { return m_StringHeap.GetString(nIndex, pszString); }
    __checkReturn
    HRESULT Impl_GetStringW(ULONG ix, __inout_ecount (cchBuffer) LPWSTR szOut, ULONG cchBuffer, ULONG *pcchBuffer);
    __checkReturn
    FORCEINLINE HRESULT Impl_GetGuid(UINT32 nIndex, GUID *pTargetGuid)
    {
        HRESULT         hr;
        GUID UNALIGNED *pSourceGuid;
        IfFailRet(m_GuidHeap.GetGuid(
            nIndex,
            &pSourceGuid));
        // Add void* casts so that the compiler can't make assumptions about alignment.
        CopyMemory((void *)pTargetGuid, (void *)pSourceGuid, sizeof(GUID));
        SwapGuid(pTargetGuid);
        return S_OK;
    }

    __checkReturn
    FORCEINLINE HRESULT Impl_GetBlob(ULONG nIndex, __out MetaData::DataBlob *pData)
    { return m_BlobHeap.GetBlob(nIndex, pData); }

    __checkReturn
    FORCEINLINE HRESULT Impl_GetRow(
                        UINT32 nTableIndex,
                        UINT32 nRowIndex,
        __deref_out_opt BYTE **ppRecord)
    {
        _ASSERTE(nTableIndex < TBL_COUNT);
        return m_Tables[nTableIndex].GetRecord(nRowIndex, ppRecord);
    }

    // Count of rows in tbl2, pointed to by the column in tbl.
    __checkReturn
    HRESULT Impl_GetEndRidForColumn(
        UINT32       nTableIndex,
        RID          nRowIndex,
        CMiniColDef &def,                   // Column containing the RID into other table.
        UINT32       nTargetTableIndex,     // The other table.
        RID         *pEndRid);

    __checkReturn
    FORCEINLINE HRESULT Impl_SearchTable(ULONG ixTbl, CMiniColDef sColumn, ULONG ixCol, ULONG ulTarget, RID *pFoundRid)
    { return Impl_SearchTableRW(ixTbl, ixCol, ulTarget, pFoundRid); }

    FORCEINLINE int Impl_IsRo()
    { return 0; }


    //*************************************************************************
    enum {END_OF_TABLE = 0};
    FORCEINLINE ULONG NewRecordPointerEndValue(ULONG ixTbl)
    { if (HasIndirectTable(ixTbl)) return m_Schema.m_cRecs[ixTbl]+1; else return END_OF_TABLE; }

    __checkReturn HRESULT ConvertMarkerToEndOfTable(ULONG tblParent, ULONG colParent, ULONG ridChild, RID ridParent);

    // Add a child row, adjust pointers in parent rows.
    __checkReturn
    HRESULT AddChildRowIndirectForParent(
        ULONG tblParent,
        ULONG colParent,
        ULONG tblChild,
        RID ridParent,
        void **ppRow);

    // Update pointers in the parent table to reflect the addition of a child, if required
    // create the indirect table in which case don't update pointers.
    __checkReturn
    HRESULT AddChildRowDirectForParent(ULONG tblParent, ULONG colParent, ULONG tblChild, RID ridParent);

    // Given a table id, create the corresponding indirect table.
    __checkReturn
    HRESULT CreateIndirectTable(ULONG ixtbl, BOOL bOneLess = true);

    // If the last param is not added in the right sequence, fix it up.
    __checkReturn
    HRESULT FixParamSequence(RID md);


    // these are the map tables to map a method, a field, a property, a event, or a param to its parent
    TOKENMAP    *m_pMethodMap;
    TOKENMAP    *m_pFieldMap;
    TOKENMAP    *m_pPropertyMap;
    TOKENMAP    *m_pEventMap;
    TOKENMAP    *m_pParamMap;

    // This table keep tracks tokens that are marked( or filtered)
    FilterTable *m_pFilterTable;
    IHostFilter *m_pHostFilter;

    // TOKENMAP *m_pTypeRefToTypeDefMap;
    TokenRemapManager *m_pTokenRemapManager;

    OptionValue m_OptionValue;

    CMiniMdSchema m_StartupSchema;      // Schema at start time.  Keep count of records.
    BYTE        m_bSortable[TBL_COUNT]; // Is a given table sortable?  (Can it be reorganized?)
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    ReleaseHolder<IMDCustomDataSource> m_pCustomDataSource;
#endif

#ifdef _DEBUG

protected:
    UTSemReadWrite * dbg_m_pLock;

public:
    // Checks that MetaData is locked for write operation (if thread-safety is enabled and the lock exists)
    void Debug_CheckIsLockedForWrite();

    void Debug_SetLock(UTSemReadWrite * pLock)
    {
        dbg_m_pLock = pLock;
    }

#endif //_DEBUG

public:

    FilterTable *GetFilterTable();
    __checkReturn HRESULT UnmarkAll();
    __checkReturn HRESULT MarkAll();

    FORCEINLINE IHostFilter *GetHostFilter() { return m_pHostFilter;}

    __checkReturn HRESULT CalculateTypeRefToTypeDefMap();

    FORCEINLINE TOKENMAP *GetTypeRefToTypeDefMap()
    { return m_pTokenRemapManager ? m_pTokenRemapManager->GetTypeRefToTypeDefMap() : NULL; };

    FORCEINLINE TOKENMAP *GetMemberRefToMemberDefMap()
    { return m_pTokenRemapManager ? m_pTokenRemapManager->GetMemberRefToMemberDefMap() : NULL; };

    FORCEINLINE MDTOKENMAP *GetTokenMovementMap()
    { return m_pTokenRemapManager ? m_pTokenRemapManager->GetTokenMovementMap() : NULL; };

    FORCEINLINE TokenRemapManager *GetTokenRemapManager() { return m_pTokenRemapManager; };

    __checkReturn HRESULT InitTokenRemapManager();

    virtual ULONG vGetCol(ULONG ixTbl, ULONG ixCol, void *pRecord)
    { return GetCol(ixTbl, ixCol, pRecord);}

public:
    virtual BOOL IsWritable()
    {
        return !m_fIsReadOnly;
    }


    //*************************************************************************
    // Delta MetaData (EditAndContinue) functions.
public:
    enum eDeltaFuncs{
        eDeltaFuncDefault = 0,
        eDeltaMethodCreate,
        eDeltaFieldCreate,
        eDeltaParamCreate,
        eDeltaPropertyCreate,
        eDeltaEventCreate,
    };

    __checkReturn HRESULT ApplyDelta(CMiniMdRW &mdDelta);

public:
    // Functions for updating ENC log tables ENC log.
    FORCEINLINE BOOL IsENCOn()
    {
        return (m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateENC;
    }

    __checkReturn
    FORCEINLINE HRESULT UpdateENCLog(mdToken tk, CMiniMdRW::eDeltaFuncs funccode = CMiniMdRW::eDeltaFuncDefault)
    {
        if (IsENCOn())
            return UpdateENCLogHelper(tk, funccode);
        else
            return S_OK;
    }

    __checkReturn
    FORCEINLINE HRESULT UpdateENCLog2(ULONG ixTbl, ULONG iRid, CMiniMdRW::eDeltaFuncs funccode = CMiniMdRW::eDeltaFuncDefault)
    {
        if (IsENCOn())
            return UpdateENCLogHelper2(ixTbl, iRid, funccode);
        else
            return S_OK;
    }

    __checkReturn HRESULT ResetENCLog();

private:
    BOOL m_fMinimalDelta;

public:
    BOOL IsMinimalDelta()
    {
        return m_fMinimalDelta;
    }


    // Turns on/off  the ability to emit delta metadatas

    // Unfortunately, we can't allow this to be set via the SetOption method anymore. In v1.0 and v1.1, this flag
    // could be set but would still result in generating full metadatas. We can't automatically start generating
    // true deltas for people... it could break them.
    void EnableDeltaMetadataGeneration()
    {
        _ASSERTE(m_OptionValue.m_UpdateMode == MDUpdateENC);
    }
    void DisableDeltaMetadataGeneration() {m_OptionValue.m_UpdateMode = MDUpdateENC;}

protected:
    // Internal Helper functions for ENC log.
    __checkReturn
    HRESULT UpdateENCLogHelper(mdToken tk, CMiniMdRW::eDeltaFuncs funccode);
    __checkReturn
    HRESULT UpdateENCLogHelper2(ULONG ixTbl, ULONG iRid, CMiniMdRW::eDeltaFuncs funccode);

protected:
    static ULONG m_TruncatedEncTables[];
    static ULONG m_SuppressedDeltaColumns[TBL_COUNT];

    ULONGARRAY  *m_rENCRecs;    // Array of RIDs affected by ENC.

    __checkReturn
    HRESULT ApplyRecordDelta(CMiniMdRW &mdDelta, ULONG ixTbl, void *pDelta, void *pRecord);
    __checkReturn
    HRESULT ApplyTableDelta(CMiniMdRW &mdDelta, ULONG ixTbl, RID iRid, int fc);
    __checkReturn
    HRESULT GetDeltaRecord(ULONG ixTbl, ULONG iRid, void **ppRecord);
    __checkReturn
    HRESULT ApplyHeapDeltas(CMiniMdRW &mdDelta);
    __checkReturn
    HRESULT ApplyHeapDeltasWithMinimalDelta(CMiniMdRW &mdDelta);
    __checkReturn
    HRESULT ApplyHeapDeltasWithFullDelta(CMiniMdRW &mdDelta);
    __checkReturn
    HRESULT StartENCMap();              // Call, on a delta MD, to prepare to access sparse rows.
    __checkReturn
    HRESULT EndENCMap();                // Call, on a delta MD, when done with sparse rows.

public:
    // Workaround for compiler performance issue VSW 584653 for 2.0 RTM.
    // Get the table's VirtualSort validity state.
    bool IsTableVirtualSorted(ULONG ixTbl);
    // Workaround for compiler performance issue VSW 584653 for 2.0 RTM.
    // Validate table's VirtualSort after adding one record into the table.
    // Returns new VirtualSort validity state in *pfIsTableVirtualSortValid.
    // Assumptions:
    //    Table's VirtualSort was valid before adding the record to the table.
    //    The caller must ensure validity of VirtualSort by calling to
    //    IsTableVirtualSorted or by using the returned state from previous
    //    call to this method.
    __checkReturn
    HRESULT ValidateVirtualSortAfterAddRecord(
        ULONG  ixTbl,
        bool * pfIsTableVirtualSortValid);

}; // class CMiniMdRW : public CMiniMdTemplate<CMiniMdRW>

#endif // _METAMODELRW_H_
