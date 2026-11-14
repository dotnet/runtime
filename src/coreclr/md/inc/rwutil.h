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

#include <minipal/rwlock.h>

class CMiniMdRW;

HRESULT CreateMDReadWriteLock(minipal_rwlock **ppLock);
void DestroyMDReadWriteLock(minipal_rwlock *pLock);
HRESULT AcquireMDReadLock(minipal_rwlock *pLock);
HRESULT AcquireMDWriteLock(minipal_rwlock *pLock COMMA_INDEBUG(CMiniMdRW *pMiniMd));
void ReleaseMDReadLock(minipal_rwlock *pLock);
void ReleaseMDWriteLock(minipal_rwlock *pLock COMMA_INDEBUG(CMiniMdRW *pMiniMd));

#define UTF8STR(wszInput, szOutput)                         \
    do {                                                    \
        if ((wszInput) == NULL)                             \
        {                                                   \
            (szOutput) = NULL;                              \
        }                                                   \
        else                                                \
        {                                                   \
            int cbBuffer = ((int)u16_strlen(wszInput) * 3) + 1; \
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

#ifdef FEATURE_METADATA_PERSISTENCE
struct TOKENREC
{
    mdToken m_tkFrom;
    mdToken m_tkTo;
};

class MDTOKENMAP : public CDynArray<TOKENREC>
{
public:
    MDTOKENMAP()
        : m_iCountSorted(0)
    {
    }

    HRESULT AppendRecord(mdToken tkFrom, mdToken tkTo);
    mdToken SafeRemap(mdToken tkFrom);
    HRESULT EmptyMap();

private:
    bool Find(mdToken tkFrom, TOKENREC **ppRec);

    int CompareFromToken(
        int iLeft,
        int iRight)
    {
        if (Get(iLeft)->m_tkFrom < Get(iRight)->m_tkFrom)
            return -1;
        if (Get(iLeft)->m_tkFrom == Get(iRight)->m_tkFrom)
            return 0;
        return 1;
    }

    void Swap(
        int iFirst,
        int         iSecond)
    {
        if (iFirst == iSecond)
            return;

        memcpy(&m_buf, Get(iFirst), sizeof(TOKENREC));
        memcpy(Get(iFirst), Get(iSecond), sizeof(TOKENREC));
        memcpy(Get(iSecond), &m_buf, sizeof(TOKENREC));
    }

    void SortRangeFromToken(int iLeft, int iRight);
    void SortTokensByFromToken();

    TOKENREC m_buf;
    ULONG m_iCountSorted;
};
#endif

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

    bool ResolveRefToDef(
        mdToken tkRef,                      // [IN] ref token
        mdToken *ptkDef);                   // [OUT] def token that it resolves to. If it does not resolve to a def

    FORCEINLINE TOKENMAP *GetTypeRefToTypeDefMap() { return &m_TypeRefToTypeDefMap; }
    FORCEINLINE TOKENMAP *GetMemberRefToMemberDefMap() { return &m_MemberRefToMemberDefMap; }
#ifdef FEATURE_METADATA_PERSISTENCE
    FORCEINLINE MDTOKENMAP *GetTokenMovementMap() { return &m_TKMap; }
#endif

    ~TokenRemapManager();
    HRESULT ClearAndEnsureCapacity(ULONG cTypeRef, ULONG cMemberRef);
private:
#ifdef FEATURE_METADATA_PERSISTENCE
    MDTOKENMAP  m_TKMap;
#endif
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
// Helper class to ensure the metadata read-write lock is released correctly.
// The destructor releases whichever lock mode it holds.
// User should use macro defined in below instead of calling functions on this class directly.
// They are LOCKREAD(), LOCKWRITE(), and CONVERT_READ_TO_WRITE_LOCK.
//
//*********************************************************************
class CMDReadWriteLock
{
public:
    CMDReadWriteLock(minipal_rwlock *pLock COMMA_INDEBUG(CMiniMdRW *pMiniMd));
    ~CMDReadWriteLock();
    HRESULT LockRead();
    HRESULT LockWrite();
    void UnlockWrite();
    HRESULT ConvertReadLockToWriteLock();
#ifdef _DEBUG
    void Debug_DetachMiniMd();
#endif // _DEBUG
private:
    bool            m_fLockedForRead;
    bool            m_fLockedForWrite;
    minipal_rwlock  *m_pLock;
    INDEBUG(CMiniMdRW *m_pMiniMd;)
};


#define LOCKREADIFFAILRET()         CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    IfFailRet(lockHolder.LockRead());
#define LOCKWRITEIFFAILRET()        CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    IfFailRet(lockHolder.LockWrite());

#define LOCKREADNORET()             CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    hr = lockHolder.LockRead();
#define LOCKWRITENORET()            CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    hr = lockHolder.LockWrite();

#define LOCKREAD()                  CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    IfFailGo(lockHolder.LockRead());
#define LOCKWRITE()                 CMDReadWriteLock lockHolder(m_pReadWriteLock COMMA_INDEBUG(m_pStgdb != NULL ? &m_pStgdb->m_MiniMd : NULL));\
                                    IfFailGo(lockHolder.LockWrite());

#define UNLOCKWRITE()               lockHolder.UnlockWrite();
#define CONVERT_READ_TO_WRITE_LOCK() IfFailGo(lockHolder.ConvertReadLockToWriteLock());


#endif // __RWUtil__h__
