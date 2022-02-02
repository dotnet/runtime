// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RWUtil.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __RWUtil__h__
#define __RWUtil__h__

class UTSemReadWrite;

#define UTF8STR(wszInput, szOutput)                         \
    do {                                                    \
        if ((wszInput) == NULL)                             \
        {                                                   \
            (szOutput) = NULL;                              \
        }                                                   \
        else                                                \
        {                                                   \
            int cbBuffer = ((int)wcslen(wszInput) * 3) + 1; \
            (szOutput) = (char *)_alloca(cbBuffer);         \
            Unicode2UTF((wszInput), (szOutput), cbBuffer);  \
        }                                                   \
    } while (0)

//*****************************************************************************
// Helper methods
//*****************************************************************************
void
Unicode2UTF(
    LPCWSTR wszSrc, // The string to convert.
  _Out_writes_(cbDst)
    LPUTF8  szDst,  // Buffer for the output UTF8 string.
    int     cbDst); // Size of the buffer for UTF8 string.

//*********************************************************************
// The token remap record.
//*********************************************************************
struct TOKENREC
{
    mdToken     m_tkFrom;                   // The imported token
    bool        m_isDuplicate;              // Is record duplicate? This information is recorded during merge
    bool        m_isDeleted;                // This information is recorded during RegMeta::ProcessFilter when we might have deleted a record
    bool        m_isFoundInImport;          // This information is also recorded during RegMeta::ProcessFilter
    mdToken     m_tkTo;                     // The new token in the merged scope

    void SetEmpty() {m_tkFrom = m_tkTo = (mdToken) -1;}
    BOOL IsEmpty() {return m_tkFrom == (mdToken) -1;}
};


//*********************************************************************
//
// This structure keeps track on token remap for an imported scope. This map is initially sorted by from
// tokens. It can then become sorted by To tokens. This usually happen during PreSave remap lookup. Thus
// we assert if we try to look up or sort by From token.
//
//*********************************************************************
class MDTOKENMAP : public CDynArray<TOKENREC>
{
public:

    enum SortKind{
        Unsorted = 0,
        SortByFromToken = 1,
        SortByToToken = 2,
        Indexed = 3,                    // Indexed by table/rid.  Implies that strings are sorted by "From".
    };

    MDTOKENMAP()
     :  m_pNextMap(NULL),
        m_pMap(NULL),
        m_iCountTotal(0),
        m_iCountSorted(0),
        m_sortKind(SortByFromToken),
        m_iCountIndexed(0)
#if defined(_DEBUG)
       ,m_pImport(0)
#endif
    { }
    ~MDTOKENMAP();

    HRESULT Init(IUnknown *pImport);

    // find a token in the tokenmap.
    bool Find(mdToken tkFrom, TOKENREC **ppRec);

    // remap a token. We assert if we don't find the tkFind in the table
    HRESULT Remap(mdToken tkFrom, mdToken *ptkTo);

    // Insert a record. This function will keep the inserted record in a sorted sequence
    HRESULT InsertNotFound(mdToken tkFrom, bool fDuplicate, mdToken tkTo, TOKENREC **ppRec);

    // This function will just append the record to the end of the list
    HRESULT AppendRecord(
        mdToken     tkFrom,
        bool        fDuplicate,
        mdToken     tkTo,
        TOKENREC    **ppRec);

    // This is a safe remap. *tpkTo will be tkFind if we cannot find tkFind in the lookup table.
    mdToken SafeRemap(mdToken tkFrom);      // [IN] the token value to find

    bool FindWithToToken(
        mdToken     tkFind,                 // [IN] the token value to find
        int         *piPosition);           // [OUT] return the first from-token that has the matching to-token

    FORCEINLINE void SortTokensByFromToken()
    {
        _ASSERTE(m_sortKind == SortByFromToken || m_sortKind == Indexed);
        // Only sort if there are unsorted records.
        if (m_iCountSorted < m_iCountTotal)
        {
            SortRangeFromToken(m_iCountIndexed, m_iCountIndexed+m_iCountTotal - 1);
            m_iCountSorted = m_iCountTotal;
        }
    } // void MDTOKENMAP::SortTokensByFromToken()

    HRESULT EmptyMap();

    void SortTokensByToToken();

    MDTOKENMAP  *m_pNextMap;
    IMapToken   *m_pMap;

private:
    FORCEINLINE int CompareFromToken(       // -1, 0, or 1
        int         iLeft,                  // First item to compare.
        int         iRight)                 // Second item to compare.
    {
        if ( Get(iLeft)->m_tkFrom < Get(iRight)->m_tkFrom )
            return -1;
        if ( Get(iLeft)->m_tkFrom == Get(iRight)->m_tkFrom )
            return 0;
        return 1;
    }

    FORCEINLINE int CompareToToken(         // -1, 0, or 1
        int         iLeft,                  // First item to compare.
        int         iRight)                 // Second item to compare.
    {
        if ( Get(iLeft)->m_tkTo < Get(iRight)->m_tkTo )
            return -1;
        if ( Get(iLeft)->m_tkTo == Get(iRight)->m_tkTo )
            return 0;
        return 1;
    }

    FORCEINLINE void Swap(
        int         iFirst,
        int         iSecond)
    {
        if ( iFirst == iSecond ) return;
        memcpy( &m_buf, Get(iFirst), sizeof(TOKENREC) );
        memcpy( Get(iFirst), Get(iSecond),sizeof(TOKENREC) );
        memcpy( Get(iSecond), &m_buf, sizeof(TOKENREC) );
    }

    void SortRangeFromToken(int iLeft, int iRight);
    void SortRangeToToken(int iLeft, int iRight);

    TOKENREC    m_buf;
    ULONG       m_iCountTotal;              // total entry in the map
    ULONG       m_iCountSorted;             // number of entries that are sorted

    SortKind    m_sortKind;

    ULONG       m_TableOffset[TBL_COUNT+1]; // Start of each table in map.
    ULONG       m_iCountIndexed;            // number of entries that are indexed.
#if defined(_DEBUG)
    IMetaDataImport *m_pImport;             // For data validation.
#endif
};



//*********************************************************************
//
// This CMapToken class implemented the IMapToken. It is used in RegMeta for
// filter process. This class can track all of the tokens are mapped. It also
// supplies a Find function.
//
//*********************************************************************
class CMapToken : public IMapToken
{
    friend class RegMeta;

public:
    STDMETHODIMP QueryInterface(REFIID riid, PVOID *pp);
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();
    STDMETHODIMP Map(mdToken tkImp, mdToken tkEmit);
    bool Find(mdToken tkFrom, TOKENREC **pRecTo);
    CMapToken();
    virtual ~CMapToken();
    MDTOKENMAP  *m_pTKMap;
private:
    LONG        m_cRef;
    bool        m_isSorted;
};

typedef CDynArray<mdToken> TOKENMAP;

//*********************************************************************
//
// This class records all sorts of token movement during optimization phase.
// This including Ref to Def optimization. This also includes token movement
// due to sorting or eleminating the pointer tables.
//
//*********************************************************************
class TokenRemapManager
{
public:
    //*********************************************************************
    //
    // This function is called when a TypeRef is resolved to a TypeDef.
    //
    //*********************************************************************
    FORCEINLINE void RecordTypeRefToTypeDefOptimization(
        mdToken tkFrom,
        mdToken tkTo)
    {
        _ASSERTE( TypeFromToken(tkFrom) == mdtTypeRef );
        _ASSERTE( TypeFromToken(tkTo) == mdtTypeDef );

        m_TypeRefToTypeDefMap[RidFromToken(tkFrom)] = tkTo;
    }   // RecordTypeRefToTypeDefOptimization


    //*********************************************************************
    //
    // This function is called when a MemberRef is resolved to a MethodDef or FieldDef.
    //
    //*********************************************************************
    FORCEINLINE void RecordMemberRefToMemberDefOptimization(
        mdToken tkFrom,
        mdToken tkTo)
    {
        _ASSERTE( TypeFromToken(tkFrom) == mdtMemberRef );
        _ASSERTE( TypeFromToken(tkTo) == mdtMethodDef || TypeFromToken(tkTo) == mdtFieldDef);

        m_MemberRefToMemberDefMap[RidFromToken(tkFrom)] = tkTo;
    }   // RecordMemberRefToMemberDefOptimization

    //*********************************************************************
    //
    // This function is called when the token kind does not change but token
    // is moved. For example, when we sort CustomAttribute table or when we optimize
    // away MethodPtr table. These operation will not change the token type.
    //
    //*********************************************************************
    FORCEINLINE HRESULT RecordTokenMovement(
        mdToken tkFrom,
        mdToken tkTo)
    {
        TOKENREC    *pTokenRec;

        _ASSERTE( TypeFromToken(tkFrom) == TypeFromToken(tkTo) );
        return m_TKMap.AppendRecord( tkFrom, false, tkTo, &pTokenRec );
    }   // RecordTokenMovement

    bool ResolveRefToDef(
        mdToken tkRef,                      // [IN] ref token
        mdToken *ptkDef);                   // [OUT] def token that it resolves to. If it does not resolve to a def

    FORCEINLINE TOKENMAP *GetTypeRefToTypeDefMap() { return &m_TypeRefToTypeDefMap; }
    FORCEINLINE TOKENMAP *GetMemberRefToMemberDefMap() { return &m_MemberRefToMemberDefMap; }
    FORCEINLINE MDTOKENMAP *GetTokenMovementMap() { return &m_TKMap; }

    ~TokenRemapManager();
    HRESULT ClearAndEnsureCapacity(ULONG cTypeRef, ULONG cMemberRef);
private:
    MDTOKENMAP  m_TKMap;
    TOKENMAP    m_TypeRefToTypeDefMap;
    TOKENMAP    m_MemberRefToMemberDefMap;
};  // class TokenRemapManager

// value that can be set by SetOption APIs
struct OptionValue
{
    CorCheckDuplicatesFor       m_DupCheck;             // Bit Map for checking duplicates during emit.
    CorRefToDefCheck            m_RefToDefCheck;        // Bit Map for specifying whether to do a ref to def optimization.
    CorNotificationForTokenMovement m_NotifyRemap;      // Bit Map for token remap notification.
    ULONG                       m_UpdateMode;           // (CorSetENC) Specifies whether ENC or Extension mode is on.
    CorErrorIfEmitOutOfOrder    m_ErrorIfEmitOutOfOrder;    // Do not generate pointer tables
    CorThreadSafetyOptions      m_ThreadSafetyOptions;  // specify if thread safety is turn on or not.
    CorImportOptions            m_ImportOption;         // import options such as to skip over deleted items or not
    CorLinkerOptions            m_LinkerOption;         // Linker option. Currently only used in UnmarkAll
    BOOL                        m_GenerateTCEAdapters;  // Do not generate the TCE adapters for COM CPC.
    LPSTR                       m_RuntimeVersion;       // CLR Version stamp
    MetadataVersion             m_MetadataVersion;      // Version of the metadata to emit
    MergeFlags                  m_MergeOptions;         // Options to pass to the merger
    UINT32                      m_InitialSize;          // Initial size of MetaData with values: code:CorMetaDataInitialSize.
    CorLocalRefPreservation     m_LocalRefPreservation; // Preserve module-local refs instead of optimizing them to defs
};  // struct OptionValue

//*********************************************************************
//
// Helper class to ensure calling UTSemReadWrite correctly.
// The destructor will call the correct UnlockRead or UnlockWrite depends what lock it is holding.
// User should use macro defined in below instead of calling functions on this class directly.
// They are LOCKREAD(), LOCKWRITE(), and CONVERT_READ_TO_WRITE_LOCK.
//
//*********************************************************************
class CMDSemReadWrite
{
public:
    CMDSemReadWrite(UTSemReadWrite *pSem);
    ~CMDSemReadWrite();
    HRESULT LockRead();
    HRESULT LockWrite();
    void UnlockWrite();
    HRESULT ConvertReadLockToWriteLock();
private:
    bool            m_fLockedForRead;
    bool            m_fLockedForWrite;
    UTSemReadWrite  *m_pSem;
};


#define LOCKREADIFFAILRET()         CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    IfFailRet(cSem.LockRead());
#define LOCKWRITEIFFAILRET()        CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    IfFailRet(cSem.LockWrite());

#define LOCKREADNORET()             CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    hr = cSem.LockRead();
#define LOCKWRITENORET()            CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    hr = cSem.LockWrite();

#define LOCKREAD()                  CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    IfFailGo(cSem.LockRead());
#define LOCKWRITE()                 CMDSemReadWrite cSem(m_pSemReadWrite);\
                                    IfFailGo(cSem.LockWrite());

#define UNLOCKWRITE()               cSem.UnlockWrite();
#define CONVERT_READ_TO_WRITE_LOCK() IfFailGo(cSem.ConvertReadLockToWriteLock());


#endif // __RWUtil__h__
