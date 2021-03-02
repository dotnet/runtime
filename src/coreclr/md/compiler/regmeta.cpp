// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RegMeta.cpp
//

//
// Implementation for meta data public interface methods.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "mdperf.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include "mdinternalrw.h"


#include <metamodelrw.h>

#define DEFINE_CUSTOM_NODUPCHECK    1
#define DEFINE_CUSTOM_DUPCHECK      2
#define SET_CUSTOM                  3

#if defined(_DEBUG) && defined(_TRACE_REMAPS)
#define LOGGING
#endif
#include <log.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

RegMeta::RegMeta() :
    m_pStgdb(0),
    m_pStgdbFreeList(NULL),
    m_pUnk(0),
    m_pFilterManager(NULL),
#ifdef FEATURE_METADATA_INTERNAL_APIS
    m_pInternalImport(NULL),
#endif
    m_pSemReadWrite(NULL),
    m_fOwnSem(false),
    m_bRemap(false),
    m_bSaveOptimized(false),
    m_hasOptimizedRefToDef(false),
    m_pHandler(0),
    m_fIsTypeDefDirty(false),
    m_fIsMemberDefDirty(false),
    m_fStartedEE(false),
    m_pAppDomain(NULL),
    m_OpenFlags(0),
    m_cRef(0),
	m_pFreeThreadedMarshaler(NULL),
    m_bCached(false),
    m_trLanguageType(0),
    m_SetAPICaller(EXTERNAL_CALLER),
    m_ModuleType(ValidatorModuleTypeInvalid),
    m_bKeepKnownCa(false),
    m_pCorProfileData(NULL),
    m_ReorderingOptions(NoReordering)
#ifdef FEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN
    , m_safeToDeleteStgdb(true)
#endif
{
    memset(&m_OptionValue, 0, sizeof(OptionValue));

#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_RegMetaBreak))
    {
        _ASSERTE(!"RegMeta()");
    }
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_KeepKnownCA))
        m_bKeepKnownCa = true;
#endif // _DEBUG

} // RegMeta::RegMeta()

RegMeta::~RegMeta()
{
    BEGIN_CLEANUP_ENTRYPOINT;

    _ASSERTE(!m_bCached);

    HRESULT hr = S_OK;

    LOCKWRITENORET();

#ifdef FEATURE_METADATA_INTERNAL_APIS
    // This should have worked if we've cached the public interface in the past
    _ASSERTE(SUCCEEDED(hr) || (m_pInternalImport == NULL) || (m_pInternalImport->GetCachedPublicInterface(false) == NULL));
#endif //FEATURE_METADATA_INTERNAL_APIS

    if (SUCCEEDED(hr))
    {
#ifdef FEATURE_METADATA_INTERNAL_APIS
        if (m_pInternalImport != NULL)
        {
            // RegMeta is going away. Make sure we clear up the pointer from MDInternalRW to this RegMeta.
            if (FAILED(m_pInternalImport->SetCachedPublicInterface(NULL)))
            {   // Do nothing on error
            }
            m_pInternalImport = NULL;
            m_fOwnSem = false;
        }
#endif //FEATURE_METADATA_INTERNAL_APIS

        UNLOCKWRITE();
    }

    if (m_pFreeThreadedMarshaler)
    {
        m_pFreeThreadedMarshaler->Release();
        m_pFreeThreadedMarshaler = NULL;
    }

    if (m_pSemReadWrite && m_fOwnSem)
        delete m_pSemReadWrite;

    // If this RegMeta is a wrapper on an external StgDB, release it.
    if (IsOfExternalStgDB(m_OpenFlags))
    {
        _ASSERTE(m_pUnk != NULL);   // Owning IUnknown for external StgDB.
        if (m_pUnk)
            m_pUnk->Release();
        m_pUnk = 0;
    }
    else
    {   // Not a wrapper, so free our StgDB.
        _ASSERTE(m_pUnk == NULL);
        // It's possible m_pStdbg is NULL in OOM scenarios
        if (m_pStgdb != NULL)
            delete m_pStgdb;
        m_pStgdb = 0;
    }

    // Delete the old copies of Stgdb list. This is the list track all of the
    //  old snapshuts with ReOpenWithMemory call.
    CLiteWeightStgdbRW  *pCur;
    while (m_pStgdbFreeList)
    {
        pCur = m_pStgdbFreeList;
        m_pStgdbFreeList = m_pStgdbFreeList->m_pNextStgdb;
        delete pCur;
    }

    // If This RegMeta spun up the runtime (probably to process security
    //  attributes), shut it down now.
    if (m_fStartedEE)
    {
        m_pAppDomain->Release();
    }

    if (m_pFilterManager != NULL)
        delete m_pFilterManager;


    if (m_OptionValue.m_RuntimeVersion != NULL)
        delete[] m_OptionValue.m_RuntimeVersion;

    END_CLEANUP_ENTRYPOINT;

} // RegMeta::~RegMeta()

HRESULT RegMeta::SetOption(OptionValue *pOptionValue)
{
    _ASSERTE(pOptionValue);
    char* pszRuntimeVersion = NULL;

    if (pOptionValue->m_RuntimeVersion != NULL)
    {
        SIZE_T dwBufferSize = strlen(pOptionValue->m_RuntimeVersion) + 1; // +1 for null
        pszRuntimeVersion = new (nothrow) char[dwBufferSize];
        if (pszRuntimeVersion == NULL)
        {
            return E_OUTOFMEMORY;
        }
        strcpy_s(pszRuntimeVersion, dwBufferSize, pOptionValue->m_RuntimeVersion);
    }

    memcpy(&m_OptionValue, pOptionValue, sizeof(OptionValue));
    m_OptionValue.m_RuntimeVersion = pszRuntimeVersion;

    return S_OK;
}// SetOption


//*****************************************************************************
// Initialize with an existing stgdb.
//*****************************************************************************
__checkReturn
HRESULT
RegMeta::InitWithStgdb(
    IUnknown           *pUnk,       // The IUnknown that owns the life time for the existing stgdb
    CLiteWeightStgdbRW *pStgdb)     // existing light weight stgdb
{
    // RegMeta created this way will not create a read/write lock semaphore.
    HRESULT hr = S_OK;

    _ASSERTE(m_pStgdb == NULL);
    m_tdModule = COR_GLOBAL_PARENT_TOKEN;
    m_pStgdb = pStgdb;

    m_OpenFlags = ofExternalStgDB;

    // remember the owner of the light weight stgdb
    // AddRef it to ensure the lifetime
    //
    m_pUnk = pUnk;
    m_pUnk->AddRef();
    IfFailGo(m_pStgdb->m_MiniMd.GetOption(&m_OptionValue));
ErrExit:
    return hr;
} // RegMeta::InitWithStgdb

#ifdef FEATURE_METADATA_EMIT

//*****************************************************************************
// call stgdb InitNew
//*****************************************************************************
__checkReturn
HRESULT
RegMeta::CreateNewMD()
{
    HRESULT hr = NOERROR;

    m_OpenFlags = ofWrite;

    // Allocate our m_pStgdb.
    _ASSERTE(m_pStgdb == NULL);
    IfNullGo(m_pStgdb = new (nothrow) CLiteWeightStgdbRW);

    // Initialize the new, empty database.

    // First tell the new database what sort of metadata to create
    m_pStgdb->m_MiniMd.m_OptionValue.m_MetadataVersion = m_OptionValue.m_MetadataVersion;
    m_pStgdb->m_MiniMd.m_OptionValue.m_InitialSize = m_OptionValue.m_InitialSize;
    IfFailGo(m_pStgdb->InitNew());

    // Set up the Module record.
    uint32_t   iRecord;
    ModuleRec *pModule;
    GUID       mvid;
    IfFailGo(m_pStgdb->m_MiniMd.AddModuleRecord(&pModule, &iRecord));
    IfFailGo(CoCreateGuid(&mvid));
    IfFailGo(m_pStgdb->m_MiniMd.PutGuid(TBL_Module, ModuleRec::COL_Mvid, pModule, mvid));

    // Add the dummy module typedef which we are using to parent global items.
    TypeDefRec *pRecord;
    IfFailGo(m_pStgdb->m_MiniMd.AddTypeDefRecord(&pRecord, &iRecord));
    m_tdModule = TokenFromRid(iRecord, mdtTypeDef);
    IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_TypeDef, TypeDefRec::COL_Name, pRecord, COR_WMODULE_CLASS));
    IfFailGo(m_pStgdb->m_MiniMd.SetOption(&m_OptionValue));

    if (IsThreadSafetyOn())
    {
        m_pSemReadWrite = new (nothrow) UTSemReadWrite();
        IfNullGo(m_pSemReadWrite);
        IfFailGo(m_pSemReadWrite->Init());
        m_fOwnSem = true;

        INDEBUG(m_pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)
    }

ErrExit:
    return hr;
} // RegMeta::CreateNewMD

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
//*****************************************************************************
// Create new stgdb for portable pdb
//*****************************************************************************
__checkReturn
HRESULT
RegMeta::CreateNewPortablePdbMD()
{
    HRESULT hr = NOERROR;
    // TODO: move the constant to a better location
    static const char* PDB_VERSION = "PDB v1.0";
    size_t len = strlen(PDB_VERSION) + 1;

    m_OpenFlags = ofWrite;

    // Allocate our m_pStgdb.
    _ASSERTE(m_pStgdb == NULL);
    IfNullGo(m_pStgdb = new (nothrow) CLiteWeightStgdbRW);

    // Initialize the new, empty database.

    // First tell the new database what sort of metadata to create
    m_pStgdb->m_MiniMd.m_OptionValue.m_MetadataVersion = m_OptionValue.m_MetadataVersion;
    m_pStgdb->m_MiniMd.m_OptionValue.m_InitialSize = m_OptionValue.m_InitialSize;
    IfFailGo(m_pStgdb->InitNew());

    // Set up the pdb version
    m_OptionValue.m_RuntimeVersion = new char[len];
    strcpy_s(m_OptionValue.m_RuntimeVersion, len, PDB_VERSION);

    IfFailGo(m_pStgdb->m_MiniMd.SetOption(&m_OptionValue));

    if (IsThreadSafetyOn())
    {
        m_pSemReadWrite = new (nothrow) UTSemReadWrite();
        IfNullGo(m_pSemReadWrite);
        IfFailGo(m_pSemReadWrite->Init());
        m_fOwnSem = true;

        INDEBUG(m_pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)
    }

ErrExit:
    return hr;
} // RegMeta::CreateNewPortablePdbMD
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#endif //FEATURE_METADATA_EMIT

//*****************************************************************************
// call stgdb OpenForRead
//*****************************************************************************
HRESULT RegMeta::OpenExistingMD(
    LPCWSTR     szDatabase,             // Name of database.
    void        *pData,                 // Data to open on top of, 0 default.
    ULONG       cbData,                 // How big is the data.
    ULONG       dwOpenFlags)            // Flags for the open.
{
    HRESULT     hr = NOERROR;
    void        *pbData = pData;        // Pointer to original or copied data.



    m_OpenFlags = dwOpenFlags;

    if (!IsOfReOpen(dwOpenFlags))
    {
        // Allocate our m_pStgdb, if we should.
        _ASSERTE(m_pStgdb == NULL);
        IfNullGo( m_pStgdb = new (nothrow) CLiteWeightStgdbRW );
    }

    IfFailGo( m_pStgdb->OpenForRead(
        szDatabase,
        pbData,
        cbData,
        m_OpenFlags) );

    if (m_pStgdb->m_MiniMd.m_Schema.m_major == METAMODEL_MAJOR_VER_V1_0 &&
        m_pStgdb->m_MiniMd.m_Schema.m_minor == METAMODEL_MINOR_VER_V1_0)
        m_OptionValue.m_MetadataVersion = MDVersion1;

    else
        m_OptionValue.m_MetadataVersion = MDVersion2;



    IfFailGo( m_pStgdb->m_MiniMd.SetOption(&m_OptionValue) );

    if (IsThreadSafetyOn())
    {
        m_pSemReadWrite = new (nothrow) UTSemReadWrite();
        IfNullGo(m_pSemReadWrite);
        IfFailGo(m_pSemReadWrite->Init());
        m_fOwnSem = true;

        INDEBUG(m_pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)
    }

    if (!IsOfReOpen(dwOpenFlags))
    {
        // There must always be a Global Module class and its the first entry in
        // the TypeDef table.
        m_tdModule = TokenFromRid(1, mdtTypeDef);
    }

ErrExit:

    return hr;
} //RegMeta::OpenExistingMD

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
HRESULT RegMeta::OpenExistingMD(
    IMDCustomDataSource* pDataSource,   // Name of database.
    ULONG       dwOpenFlags)                // Flags to control open.
{
    HRESULT     hr = NOERROR;

    m_OpenFlags = dwOpenFlags;

    if (!IsOfReOpen(dwOpenFlags))
    {
        // Allocate our m_pStgdb, if we should.
        _ASSERTE(m_pStgdb == NULL);
        IfNullGo(m_pStgdb = new (nothrow)CLiteWeightStgdbRW);
    }

    IfFailGo(m_pStgdb->OpenForRead(
        pDataSource,
        m_OpenFlags));

    if (m_pStgdb->m_MiniMd.m_Schema.m_major == METAMODEL_MAJOR_VER_V1_0 &&
        m_pStgdb->m_MiniMd.m_Schema.m_minor == METAMODEL_MINOR_VER_V1_0)
        m_OptionValue.m_MetadataVersion = MDVersion1;

    else
        m_OptionValue.m_MetadataVersion = MDVersion2;



    IfFailGo(m_pStgdb->m_MiniMd.SetOption(&m_OptionValue));

    if (IsThreadSafetyOn())
    {
        m_pSemReadWrite = new (nothrow)UTSemReadWrite();
        IfNullGo(m_pSemReadWrite);
        IfFailGo(m_pSemReadWrite->Init());
        m_fOwnSem = true;

        INDEBUG(m_pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)
    }

    if (!IsOfReOpen(dwOpenFlags))
    {
        // There must always be a Global Module class and its the first entry in
        // the TypeDef table.
        m_tdModule = TokenFromRid(1, mdtTypeDef);
    }

ErrExit:

    return hr;
} //RegMeta::OpenExistingMD
#endif // FEATURE_METADATA_CUSTOM_DATA_SOURCE

#ifdef FEATURE_METADATA_INTERNAL_APIS

//*****************************************************************************
// Gets a cached Internal importer, if available.
//
// Arguments:
//     fWithLock - if true, takes a reader lock.
//     If false, assumes caller is handling the synchronization.
//
// Returns:
//     A cached Internal importer, which gets addreffed. Caller must release!
//     If no importer is set, returns NULL
//
// Notes:
//     This function also does not trigger the creation of Internal interface.
//     Set the cached importer via code:RegMeta.SetCachedInternalInterface
//
// Implements internal API code:IMetaDataHelper::GetCachedInternalInterface.
//*****************************************************************************
IUnknown* RegMeta::GetCachedInternalInterface(BOOL fWithLock)
{
    IUnknown        *pRet = NULL;
    HRESULT hr = S_OK;

    if (fWithLock)
    {
        LOCKREAD();

        pRet = m_pInternalImport;
    }
    else
    {
        pRet = m_pInternalImport;
    }
    if (pRet) pRet->AddRef();

ErrExit:

    return pRet;
} //RegMeta::GetCachedInternalInterface

//*****************************************************************************
// Set the cached Internal interface. This function will return an Error is the
// current cached internal interface is not empty and trying set a non-empty internal
// interface. One RegMeta will only associated
// with one Internal Object. Unless we have bugs somewhere else. It will QI on the
// IUnknown for the IMDInternalImport. If this failed, error will be returned.
// Note: Caller should take a write lock
//
// This does addref the importer (the public and private importers maintain
// weak references to each other).
//
// Implements internal API code:IMetaDataHelper::SetCachedInternalInterface.
//*****************************************************************************
HRESULT RegMeta::SetCachedInternalInterface(IUnknown *pUnk)
{
    HRESULT     hr = NOERROR;
    IMDInternalImport *pInternal = NULL;

    if (pUnk)
    {
        if (m_pInternalImport)
        {
            _ASSERTE(!"Bad state!");
        }
        IfFailRet( pUnk->QueryInterface(IID_IMDInternalImport, (void **) &pInternal) );

        // Should be non-null
        _ASSERTE(pInternal);
        m_pInternalImport = pInternal;

        // We don't want to add ref the internal interface, so undo the AddRef() from the QI.
        pInternal->Release();
    }
    else
    {
        // Internal interface is going away before the public interface. Take ownership on the
        // reader writer lock.
        m_fOwnSem = true;
        m_pInternalImport = NULL;
    }
    return hr;
} // RegMeta::SetCachedInternalInterface

#endif //FEATURE_METADATA_INTERNAL_APIS

//*****************************************************************************
// IUnknown
//*****************************************************************************

ULONG RegMeta::AddRef()
{
    return InterlockedIncrement(&m_cRef);
} // ULONG RegMeta::AddRef()


HRESULT
RegMeta::QueryInterface(
    REFIID  riid,
    void ** ppUnk)
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;
    int fIsInterfaceRW = false;
    *ppUnk = 0;

    if (riid == IID_IUnknown)
    {
        *ppUnk = (IUnknown *)(IMetaDataImport2 *)this;
    }
    else if (riid == IID_IMDCommon)
    {
        *ppUnk = (IMDCommon *)this;
    }
    else if (riid == IID_IMetaDataImport)
    {
        *ppUnk = (IMetaDataImport2 *)this;
    }
    else if (riid == IID_IMetaDataImport2)
    {
        *ppUnk = (IMetaDataImport2 *)this;
    }
    else if (riid == IID_IMetaDataAssemblyImport)
    {
        *ppUnk = (IMetaDataAssemblyImport *)this;
    }
    else if (riid == IID_IMetaDataTables)
    {
        *ppUnk = static_cast<IMetaDataTables *>(this);
    }
    else if (riid == IID_IMetaDataTables2)
    {
        *ppUnk = static_cast<IMetaDataTables2 *>(this);
    }

    else if (riid == IID_IMetaDataInfo)
    {
        *ppUnk = static_cast<IMetaDataInfo *>(this);
    }

#ifdef FEATURE_METADATA_EMIT
    else if (riid == IID_IMetaDataEmit)
    {
        *ppUnk = (IMetaDataEmit2 *)this;
        fIsInterfaceRW = true;
    }
    else if (riid == IID_IMetaDataEmit2)
    {
        *ppUnk = (IMetaDataEmit2 *)this;
        fIsInterfaceRW = true;
    }
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    else if (riid == IID_IMetaDataEmit3)
    {
        *ppUnk = (IMetaDataEmit3 *)this;
        fIsInterfaceRW = true;
    }
#endif
    else if (riid == IID_IMetaDataAssemblyEmit)
    {
        *ppUnk = (IMetaDataAssemblyEmit *)this;
        fIsInterfaceRW = true;
    }
#endif //FEATURE_METADATA_EMIT


#ifdef FEATURE_METADATA_EMIT_ALL
    else if (riid == IID_IMetaDataFilter)
    {
        *ppUnk = (IMetaDataFilter *)this;
    }
#endif //FEATURE_METADATA_EMIT_ALL

#ifdef FEATURE_METADATA_INTERNAL_APIS
    else if (riid == IID_IMetaDataHelper)
    {
        *ppUnk = (IMetaDataHelper *)this;
    }
    else if (riid == IID_IMDInternalEmit)
    {
        *ppUnk = static_cast<IMDInternalEmit *>(this);
    }
    else if (riid == IID_IGetIMDInternalImport)
    {
        *ppUnk = static_cast<IGetIMDInternalImport *>(this);
    }
#endif //FEATURE_METADATA_INTERNAL_APIS

#if defined(FEATURE_METADATA_EMIT) && defined(FEATURE_METADATA_INTERNAL_APIS)
    else if (riid == IID_IMetaDataEmitHelper)
    {
        *ppUnk = (IMetaDataEmitHelper *)this;
        fIsInterfaceRW = true;
    }
#endif //FEATURE_METADATA_EMIT && FEATURE_METADATA_INTERNAL_APIS

#ifdef FEATURE_METADATA_IN_VM
#ifdef FEATURE_COMINTEROP
    else if (riid == IID_IMarshal)
    {
        // We will only repond to this interface if scope is opened for ReadOnly
        if (IsOfReadOnly(m_OpenFlags))
        {
            if (m_pFreeThreadedMarshaler == NULL)
            {
                // Guard ourselves against first time QI on IMarshal from two different threads..
                LOCKWRITE();
                if (m_pFreeThreadedMarshaler == NULL)
                {
                    // First time! Create the FreeThreadedMarshaler
                    IfFailGo(CoCreateFreeThreadedMarshaler((IUnknown *)(IMetaDataEmit2 *)this, &m_pFreeThreadedMarshaler));
                }
            }

            _ASSERTE(m_pFreeThreadedMarshaler != NULL);

            IfFailGo(m_pFreeThreadedMarshaler->QueryInterface(riid, ppUnk));

            // AddRef has happened in the QueryInterface and thus should just return
            goto ErrExit;
        }
        else
        {
            IfFailGo(E_NOINTERFACE);
        }
    }
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_PREJIT
    else if (riid == IID_IMetaDataCorProfileData)
    {
        *ppUnk = (IMetaDataCorProfileData *)this;
    }
    else if (riid == IID_IMDInternalMetadataReorderingOptions)
    {
        *ppUnk = (IMDInternalMetadataReorderingOptions *)this;
    }
#endif //FEATURE_PREJIT
#endif //FEATURE_METADATA_IN_VM
    else
    {
        IfFailGo(E_NOINTERFACE);
    }

    if (fIsInterfaceRW && IsOfReadOnly(m_OpenFlags))
    {
        // They are asking for a read/write interface and this scope was
        // opened as Read-Only

        *ppUnk = NULL;
        IfFailGo(CLDB_E_INCOMPATIBLE);
    }

    if (fIsInterfaceRW)
    {
        LOCKWRITENORET();

        if (SUCCEEDED(hr))
        {
            hr = m_pStgdb->m_MiniMd.ConvertToRW();
        }

        if (FAILED(hr))
        {
            *ppUnk = NULL;
            goto ErrExit;
        }
    }

    AddRef();
ErrExit:

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::QueryInterface


//---------------------------------------------------------------------------------------
//
// Returns the memory region of the mapped file and type of its mapping. The choice of the file mapping type
// for each scope is CLR implementation specific and user cannot explicitly set it.
//
// The memory is valid only as long as the underlying MetaData scope is opened (there's a reference to
// a MetaData interface for this scope).
//
// Implements public API code:IMetaDataInfo::GetFileMapping.
//
// Arguments:
//    ppvData - Fills with pointer to the start of the mapped file.
//    pcbData - Fills with the size of the mapped memory region (for flat-mapping it is the size of the
//              file).
//    pdwMappingType - Fills with type of file mapping (code:CorFileMapping).
//        Current CLR implementation returns always code:fmFlat. The other value(s) are reserved for future
//        usage. See code:StgIO::MapFileToMem#CreateFileMapping_SEC_IMAGE for more details.
//
// Return Value:
//    S_OK               - All output data are filled.
//    COR_E_NOTSUPPORTED - CLR cannot (or doesn't want to) provide the memory region.
//        This can happen when:
//          - The MetaData scope was opened with flag code:ofWrite or code:ofCopyMemory.
//            Note: code:ofCopyMemory could be supported in future CLR versions. For example if we change
//            code:CLiteWeightStgdbRW::OpenForRead to copy whole file (or add a new flag ofCopyWholeFile).
//          - The MetaData scope was opened without flag code:ofReadOnly.
//            Note: We could support this API without code:ofReadOnly flag in future CLR versions. We just
//            need some test coverage and user scenario for it.
//          - Only MetaData part of the file was opened using code:OpenScopeOnMemory.
//          - The file is not NT PE file (e.g. it is NT OBJ = .obj file produced by managed C++).
//    E_INVALIDARG       - NULL was passed as an argument value.
//
HRESULT
RegMeta::GetFileMapping(
    const void ** ppvData,
    ULONGLONG *   pcbData,
    DWORD *       pdwMappingType)
{
    HRESULT hr = S_OK;

    if ((ppvData == NULL) || (pcbData == NULL) || (pdwMappingType == NULL))
    {
        return E_INVALIDARG;
    }

    // Note: Some of the following checks are duplicit (as some combinations are invalid and ensured by CLR
    // implementation), but it is easier to check them all

    // OpenScope flags have to be (ofRead | ofReadOnly) and not ofCopyMemory
    // (as code:CLiteWeightStgdbRW::OpenForRead will copy only the MetaData part of the file)
    if (((m_OpenFlags & ofReadWriteMask) != ofRead) ||
        ((m_OpenFlags & ofReadOnly) == 0) ||
        ((m_OpenFlags & ofCopyMemory) != 0))
    {
        IfFailGo(COR_E_NOTSUPPORTED);
    }
    // The file has to be NT PE file (not CLDB = managed C++ .obj file) and we have to have its full mapping
    // (see code:CLiteWeightStgdbRW::OpenForRead)
    if ((m_pStgdb->m_pImage == NULL) ||
        (m_pStgdb->m_dwImageSize == 0) ||
        (m_pStgdb->GetFileType() != FILETYPE_NTPE))
    {
        IfFailGo(COR_E_NOTSUPPORTED);
    }
    if (m_pStgdb->m_pStgIO->GetFlags() != DBPROP_TMODEF_READ)
    {
        IfFailGo(COR_E_NOTSUPPORTED);
    }
    // The file has to be flat-mapped, or copied to memory (file mapping code:MTYPE_IMAGE is not currently
    // supported - see code:StgIO::MapFileToMem#CreateFileMapping_SEC_IMAGE)
    // Note: Only small files (<=64K) are copied to memory - see code:StgIO::MapFileToMem#CopySmallFiles
    if ((m_pStgdb->m_pStgIO->GetMemoryMappedType() != MTYPE_FLAT) &&
        (m_pStgdb->m_pStgIO->GetMemoryMappedType() != MTYPE_NOMAPPING))
    {
        IfFailGo(COR_E_NOTSUPPORTED);
    }
    // All necessary conditions are satisfied

    *ppvData = m_pStgdb->m_pImage;
    *pcbData = m_pStgdb->m_dwImageSize;
    // We checked that the file was flat-mapped above
    *pdwMappingType = fmFlat;

ErrExit:
    if (FAILED(hr))
    {
        *ppvData = NULL;
        *pcbData = 0;
        *pdwMappingType = 0;
    }

    return hr;
} // RegMeta::GetFileMapping


//------------------------------------------------------------------------------
// Metadata dump
//
#ifdef _DEBUG

#define STRING_BUFFER_LEN 1024
#define ENUM_BUFFER_SIZE 10

int DumpMD_Write(__in __in_z const char *str)
{
    OutputDebugStringA(str);
    return 0; // strlen(str);
} // int DumpMD_Write()

int DumpMD_WriteLine(__in __in_z const char *str)
{
    OutputDebugStringA(str);
    OutputDebugStringA("\n");
    return 0; // strlen(str);
} // int DumpMD_Write()

int DumpMD_VWriteMarker(__in __in_z const char *str, va_list marker)
{
    CQuickBytes m_output;

    int     count = -1;
    int     i = 1;
    HRESULT hr;

    while (count < 0)
    {
        if (FAILED(hr = m_output.ReSizeNoThrow(STRING_BUFFER_LEN * i)))
            return 0;
        va_list markerCopy;
        va_copy(markerCopy, marker);
        count = _vsnprintf_s((char *)m_output.Ptr(), STRING_BUFFER_LEN * i, _TRUNCATE, str, markerCopy);
        va_end(markerCopy);
        i *= 2;
    }
    OutputDebugStringA((LPCSTR)m_output.Ptr());
    return count;
} // int DumpMD_VWriteMarker()

int DumpMD_VWrite(__in __in_z const char *str, ...)
{
    va_list marker;
    int     count;

    va_start(marker, str);
    count = DumpMD_VWriteMarker(str, marker);
    va_end(marker);
    return count;
} // int DumpMD_VWrite()

int DumpMD_VWriteLine(__in __in_z const char *str, ...)
{
    va_list marker;
    int     count;

    va_start(marker, str);
    count = DumpMD_VWriteMarker(str, marker);
    DumpMD_Write("\n");
    va_end(marker);
    return count;
} // int DumpMD_VWriteLine()


const char *DumpMD_DumpRawNameOfType(RegMeta *pMD, ULONG iType)
{
    if (iType <= iRidMax)
    {
        const char *pNameTable;
        pMD->GetTableInfo(iType, 0,0,0,0, &pNameTable);
        return pNameTable;
    }
    else
    // Is the field a coded token?
    if (iType <= iCodedTokenMax)
    {
        int iCdTkn = iType - iCodedToken;
        const char *pNameCdTkn;
        pMD->GetCodedTokenInfo(iCdTkn, 0,0, &pNameCdTkn);
        return pNameCdTkn;
    }

    // Fixed type.
    switch (iType)
    {
    case iBYTE:
        return "BYTE";
    case iSHORT:
        return "short";
    case iUSHORT:
        return "USHORT";
    case iLONG:
        return "long";
    case iULONG:
        return "ULONG";
    case iSTRING:
        return "string";
    case iGUID:
        return "GUID";
    case iBLOB:
        return "blob";
    }
    // default:
    static char buf[30];
    sprintf_s(buf, NumItems(buf), "unknown type 0x%02x", iType);
    return buf;
} // const char *DumpMD_DumpRawNameOfType()

void DumpMD_DumpRawCol(RegMeta *pMD, ULONG ixTbl, ULONG ixCol, ULONG rid, bool bStats)
{
    ULONG       ulType;                 // Type of a column.
    ULONG       ulVal;                  // Value of a column.
    LPCUTF8     pString;                // Pointer to a string.
    const void  *pBlob;                 // Pointer to a blob.
    ULONG       cb;                     // Size of something.

    pMD->GetColumn(ixTbl, ixCol, rid, &ulVal);
    pMD->GetColumnInfo(ixTbl, ixCol, 0, 0, &ulType, 0);

    if (ulType <= iRidMax)
    {
        const char *pNameTable;
        pMD->GetTableInfo(ulType, 0,0,0,0, &pNameTable);
        DumpMD_VWrite("%s[%x]", pNameTable, ulVal);
    }
    else
    // Is the field a coded token?
    if (ulType <= iCodedTokenMax)
    {
        int iCdTkn = ulType - iCodedToken;
        const char *pNameCdTkn;
        pMD->GetCodedTokenInfo(iCdTkn, 0,0, &pNameCdTkn);
        DumpMD_VWrite("%s[%08x]", pNameCdTkn, ulVal);
    }
    else
    {
        // Fixed type.
        switch (ulType)
        {
        case iBYTE:
            DumpMD_VWrite("%02x", ulVal);
            break;
        case iSHORT:
        case iUSHORT:
            DumpMD_VWrite("%04x", ulVal);
            break;
        case iLONG:
        case iULONG:
            DumpMD_VWrite("%08x", ulVal);
            break;
        case iSTRING:
            DumpMD_VWrite("string#%x", ulVal);
            if (bStats && ulVal)
            {
                pMD->GetString(ulVal, &pString);
                cb = (ULONG) strlen(pString) + 1;
                DumpMD_VWrite("(%d)", cb);
            }
            break;
        case iGUID:
            DumpMD_VWrite("guid#%x", ulVal);
            if (bStats && ulVal)
            {
                DumpMD_VWrite("(16)");
            }
            break;
        case iBLOB:
            DumpMD_VWrite("blob#%x", ulVal);
            if (bStats && ulVal)
            {
                pMD->GetBlob(ulVal, &cb, &pBlob);
                cb += 1;
                if (cb > 128)
                    cb += 1;
                if (cb > 16535)
                    cb += 1;
                DumpMD_VWrite("(%d)", cb);
            }
            break;
        default:
            DumpMD_VWrite("unknown type 0x%04x", ulVal);
            break;
        }
    }
} // void DumpMD_DumpRawCol()

ULONG DumpMD_DumpRawColStats(RegMeta *pMD, ULONG ixTbl, ULONG ixCol, ULONG cRows)
{
    ULONG rslt = 0;
    ULONG       ulType;                 // Type of a column.
    ULONG       ulVal;                  // Value of a column.
    LPCUTF8     pString;                // Pointer to a string.
    const void  *pBlob;                 // Pointer to a blob.
    ULONG       cb;                     // Size of something.

    pMD->GetColumnInfo(ixTbl, ixCol, 0, 0, &ulType, 0);

    if (IsHeapType(ulType))
    {
        for (ULONG rid=1; rid<=cRows; ++rid)
        {
            pMD->GetColumn(ixTbl, ixCol, rid, &ulVal);
            // Fixed type.
            switch (ulType)
            {
            case iSTRING:
                if (ulVal)
                {
                    pMD->GetString(ulVal, &pString);
                    cb = (ULONG) strlen(pString);
                    rslt += cb + 1;
                }
                break;
            case iGUID:
                if (ulVal)
                    rslt += 16;
                break;
            case iBLOB:
                if (ulVal)
                {
                    pMD->GetBlob(ulVal, &cb, &pBlob);
                    rslt += cb + 1;
                    if (cb > 128)
                        rslt += 1;
                    if (cb > 16535)
                        rslt += 1;
                }
                break;
            default:
                break;
            }
        }
    }
    return rslt;
} // ULONG DumpMD_DumpRawColStats()

int DumpMD_DumpHex(
    const char  *szPrefix,              // String prefix for first line.
    const void  *pvData,                // The data to print.
    ULONG       cbData,                 // Bytes of data to print.
    int         bText=1,                // If true, also dump text.
    ULONG       nLine=16)               // Bytes per line to print.
{
    const BYTE  *pbData = static_cast<const BYTE*>(pvData);
    ULONG       i;                      // Loop control.
    ULONG       nPrint;                 // Number to print in an iteration.
    ULONG       nSpace;                 // Spacing calculations.
    ULONG       nPrefix;                // Size of the prefix.
    ULONG       nLines=0;               // Number of lines printed.
    const char  *pPrefix;               // For counting spaces in the prefix.

    // Round down to 8 characters.
    nLine = nLine & ~0x7;

    for (nPrefix=0, pPrefix=szPrefix; *pPrefix; ++pPrefix)
    {
        if (*pPrefix == '\t')
            nPrefix = (nPrefix + 8) & ~7;
        else
            ++nPrefix;
    }
    //nPrefix = strlen(szPrefix);
    do
    {   // Write the line prefix.
        if (szPrefix)
            DumpMD_VWrite("%s:", szPrefix);
        else
            DumpMD_VWrite("%*s:", nPrefix, "");
        szPrefix = 0;
        ++nLines;

        // Calculate spacing.
        nPrint = min(cbData, nLine);
        nSpace = nLine - nPrint;

            // dump in hex.
        for(i=0; i<nPrint; i++)
            {
            if ((i&7) == 0)
                    DumpMD_Write(" ");
            DumpMD_VWrite("%02x ", pbData[i]);
            }
        if (bText)
        {
            // Space out to the text spot.
            if (nSpace)
                DumpMD_VWrite("%*s", nSpace*3+nSpace/8, "");
            // Dump in text.
            DumpMD_Write(">");
            for(i=0; i<nPrint; i++)
                DumpMD_VWrite("%c", (isprint(pbData[i])) ? pbData[i] : ' ');
            // Space out the text, and finish the line.
            DumpMD_VWrite("%*s<", nSpace, "");
        }
        DumpMD_VWriteLine("");

        // Next data to print.
        cbData -= nPrint;
        pbData += nPrint;
        }
    while (cbData > 0);

    return nLines;
} // int DumpMD_DumpHex()

void DumpMD_DisplayUserStrings(
    RegMeta     *pMD)                   // The scope to dump.
{
    HCORENUM    stringEnum = NULL;      // string enumerator.
    mdString    Strings[ENUM_BUFFER_SIZE]; // String tokens from enumerator.
    CQuickArray<WCHAR> rUserString;     // Buffer to receive string.
    WCHAR       *szUserString;          // Working pointer into buffer.
    ULONG       chUserString;           // Size of user string.
    CQuickArray<char> rcBuf;            // Buffer to hold the BLOB version of the string.
    char        *szBuf;                 // Working pointer into buffer.
    ULONG       chBuf;                  // Saved size of the user string.
    ULONG       count;                  // Items returned from enumerator.
    ULONG       totalCount = 1;         // Running count of strings.
    bool        bUnprint = false;       // Is an unprintable character found?
    HRESULT     hr;                     // A result.
    while (SUCCEEDED(hr = pMD->EnumUserStrings( &stringEnum,
                             Strings, NumItems(Strings), &count)) &&
            count > 0)
    {
        if (totalCount == 1)
        {   // If only one, it is the NULL string, so don't print it.
            DumpMD_WriteLine("User Strings");
            DumpMD_WriteLine("-------------------------------------------------------");
        }
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            do { // Try to get the string into the existing buffer.
                hr = pMD->GetUserString( Strings[i], rUserString.Ptr(),(ULONG32)rUserString.MaxSize(), &chUserString);
                if (hr == CLDB_S_TRUNCATION)
                {   // Buffer wasn't big enough, try to enlarge it.
                    if (FAILED(rUserString.ReSizeNoThrow(chUserString)))
                        DumpMD_VWriteLine("malloc failed: %#8x.", E_OUTOFMEMORY);
                    continue;
                }
            } while (0);
            if (FAILED(hr)) DumpMD_VWriteLine("GetUserString failed: %#8x.", hr);

            szUserString = rUserString.Ptr();
            chBuf = chUserString;

            DumpMD_VWrite("%08x : (%2d) L\"", Strings[i], chUserString);
            while (chUserString)
            {
                switch (*szUserString)
                {
                case 0:
                    DumpMD_Write("\\0"); break;
                case W('\r'):
                    DumpMD_Write("\\r"); break;
                case W('\n'):
                    DumpMD_Write("\\n"); break;
                case W('\t'):
                    DumpMD_Write("\\t"); break;
                default:
                    if (iswprint(*szUserString))
                        DumpMD_VWrite("%lc", *szUserString);
                    else
                    {
                        bUnprint = true;
                        DumpMD_Write(".");
                    }
                    break;
                }
                ++szUserString;
                --chUserString;
            }
            DumpMD_WriteLine("\"");

            // Print the user string as a blob if an unprintable character is found.
            if (bUnprint)
            {
                bUnprint = false;
                szUserString = rUserString.Ptr();
                // REVISIT_TODO: ReSizeNoThrow can fail. Check its return value and add an error path.
                rcBuf.ReSizeNoThrow(81); //(chBuf * 5 + 1);
                szBuf = rcBuf.Ptr();
                ULONG j,k;
                DumpMD_WriteLine("\t\tUser string has unprintables, hex format below:");
                for (j = 0,k=0; j < chBuf; j++)
                {
                    // See rcBuf.ResSizeNoThrow(81) above
                    sprintf_s (&szBuf[k*5],81-(k*5), "%04x ", szUserString[j]);
                    k++;
                    if((k==16)||(j == (chBuf-1)))
                    {
                        szBuf[k*5] = '\0';
                        DumpMD_VWriteLine("\t\t%s", szBuf);
                        k=0;
                    }
                }
            }
        }
    }
    if (stringEnum)
        pMD->CloseEnum(stringEnum);
}   // void MDInfo::DisplayUserStrings()

void DumpMD_DumpRawHeaps(
    RegMeta     *pMD)                   // The scope to dump.
{
    HRESULT     hr;                     // A result.
    ULONG       ulSize;                 // Bytes in a heap.
    const BYTE  *pData;                 // Pointer to a blob.
    ULONG       cbData;                 // Size of a blob.
    ULONG       oData;                  // Offset of current blob.
    char        rcPrefix[30];           // To format line prefix.

    pMD->GetBlobHeapSize(&ulSize);
    DumpMD_VWriteLine("");
    DumpMD_VWriteLine("Blob Heap:  %d(%#x) bytes", ulSize,ulSize);
    oData = 0;
    do
    {
        pMD->GetBlob(oData, &cbData, (const void**)&pData);
        sprintf_s(rcPrefix, NumItems(rcPrefix), "%5x,%-2x", oData, cbData);
        DumpMD_DumpHex(rcPrefix, pData, cbData);
        hr = pMD->GetNextBlob(oData, &oData);
    }
    while (hr == S_OK);

    pMD->GetStringHeapSize(&ulSize);
    DumpMD_VWriteLine("");
    DumpMD_VWriteLine("String Heap:  %d(%#x) bytes", ulSize,ulSize);
    oData = 0;
    const char *pString;
    do
    {
        pMD->GetString(oData, &pString);
        sprintf_s(rcPrefix, NumItems(rcPrefix), "%08x", oData);
        DumpMD_DumpHex(rcPrefix, pString, (ULONG)strlen(pString)+1);
        if (*pString != 0)
            DumpMD_VWrite("%08x: %s\n", oData, pString);
        hr = pMD->GetNextString(oData, &oData);
    }
    while (hr == S_OK);
    DumpMD_VWriteLine("");

    DumpMD_DisplayUserStrings(pMD);

} // void DumpMD_DumpRawHeaps()


void DumpMD_DumpRaw(RegMeta *pMD, int iDump, bool bStats)
{
    ULONG       cTables;                // Tables in the database.
    ULONG       cCols;                  // Columns in a table.
    ULONG       cRows;                  // Rows in a table.
    ULONG       cbRow;                  // Bytes in a row of a table.
    ULONG       iKey;                   // Key column of a table.
    const char  *pNameTable;            // Name of a table.
    ULONG       oCol;                   // Offset of a column.
    ULONG       cbCol;                  // Size of a column.
    ULONG       ulType;                 // Type of a column.
    const char  *pNameColumn;           // Name of a column.
    ULONG       ulSize;

    pMD->GetNumTables(&cTables);

    pMD->GetStringHeapSize(&ulSize);
    DumpMD_VWrite("Strings: %d(%#x)", ulSize, ulSize);
    pMD->GetBlobHeapSize(&ulSize);
    DumpMD_VWrite(", Blobs: %d(%#x)", ulSize, ulSize);
    pMD->GetGuidHeapSize(&ulSize);
    DumpMD_VWrite(", Guids: %d(%#x)", ulSize, ulSize);
    pMD->GetUserStringHeapSize(&ulSize);
    DumpMD_VWriteLine(", User strings: %d(%#x)", ulSize, ulSize);

    for (ULONG ixTbl = 0; ixTbl < cTables; ++ixTbl)
    {
        pMD->GetTableInfo(ixTbl, &cbRow, &cRows, &cCols, &iKey, &pNameTable);

        if (cRows == 0 && iDump < 3)
            continue;

        if (iDump >= 2)
            DumpMD_VWriteLine("=================================================");
        DumpMD_VWriteLine("%2d: %-20s cRecs:%5d(%#x), cbRec:%3d(%#x), cbTable:%6d(%#x)",
            ixTbl, pNameTable, cRows, cRows, cbRow, cbRow, cbRow * cRows, cbRow * cRows);

        if (iDump < 2)
            continue;

        // Dump column definitions for the table.
        ULONG ixCol;
        for (ixCol=0; ixCol<cCols; ++ixCol)
        {
            pMD->GetColumnInfo(ixTbl, ixCol, &oCol, &cbCol, &ulType, &pNameColumn);

            DumpMD_VWrite("  col %2x:%c %-12s oCol:%2x, cbCol:%x, %-7s",
                ixCol, ((ixCol==iKey)?'*':' '), pNameColumn, oCol, cbCol, DumpMD_DumpRawNameOfType(pMD, ulType));

            if (bStats)
            {
                ulSize = DumpMD_DumpRawColStats(pMD, ixTbl, ixCol, cRows);
                if (ulSize)
                    DumpMD_VWrite("(%d)", ulSize);
            }
            DumpMD_VWriteLine("");
        }

        if (iDump < 3)
            continue;

        // Dump the rows.
        for (ULONG rid = 1; rid <= cRows; ++rid)
        {
            if (rid == 1)
                DumpMD_VWriteLine("-------------------------------------------------");
            DumpMD_VWrite(" %3x == ", rid);
            for (ixCol=0; ixCol < cCols; ++ixCol)
            {
                if (ixCol) DumpMD_VWrite(", ");
                DumpMD_VWrite("%d:", ixCol);
                DumpMD_DumpRawCol(pMD, ixTbl, ixCol, rid, bStats);
            }
            DumpMD_VWriteLine("");
        }
    }

    DumpMD_DumpRawHeaps(pMD);

} // void DumpMD_DumpRaw()


int DumpMD_impl(RegMeta *pMD)
{
   DumpMD_DumpRaw(pMD, 3, false);
   return 0;
}

int DumpMD(UINT_PTR iMD)
{
    RegMeta *pMD = reinterpret_cast<RegMeta*>(iMD);
    return DumpMD_impl(pMD);
}

#endif //_DEBUG

//*****************************************************************************
// Using the existing RegMeta and reopen with another chuck of memory. Make sure that all stgdb
// is still kept alive.
//*****************************************************************************
HRESULT RegMeta::ReOpenWithMemory(
    LPCVOID     pData,                  // [in] Location of scope data.
    ULONG       cbData,                 // [in] Size of the data pointed to by pData.
    DWORD       dwReOpenFlags)           // [in] ReOpen flags
{
    HRESULT hr = NOERROR;

    // Only allow the ofCopyMemory and ofTakeOwnership flags
    if (dwReOpenFlags != 0 && ((dwReOpenFlags & (~(ofCopyMemory|ofTakeOwnership))) > 0))
        return E_INVALIDARG;

    LOCKWRITE();


    // put the current m_pStgdb to the free list
    m_pStgdb->m_pNextStgdb = m_pStgdbFreeList;
    m_pStgdbFreeList = m_pStgdb;
    m_pStgdb = new (nothrow) CLiteWeightStgdbRW;
    IfNullGo( m_pStgdb );
    IfFailGo( OpenExistingMD(0 /* szFileName */, const_cast<void*>(pData), cbData, ofReOpen|dwReOpenFlags /* flags */) );

#ifdef FEATURE_METADATA_INTERNAL_APIS
    // We've created a new Stgdb, but may still have an Internal Importer hanging around accessing the old Stgdb.
    // The free list ensures we don't have a dangling pointer, but the
    // If we have a corresponding InternalInterface, need to clear it because it's now using stale data.
    // Others will need to update their Internal interface to get the new data.
    {
        HRESULT hrIgnore = SetCachedInternalInterface(NULL);
        (void)hrIgnore; //prevent "unused variable" error from GCC
        _ASSERTE(hrIgnore == NOERROR); // clearing the cached interface should always succeed.
    }
#endif //FEATURE_METADATA_INTERNAL_APIS

    // we are done!
ErrExit:
    if (FAILED(hr))
    {
        // recover to the old state
        if (m_pStgdb)
            delete m_pStgdb;
        m_pStgdb = m_pStgdbFreeList;
        m_pStgdbFreeList = m_pStgdbFreeList->m_pNextStgdb;
    }
#ifdef FEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN
    else
    {
        if( !(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_MD_PreserveDebuggerMetadataMemory)) && IsSafeToDeleteStgdb())
        {
            // now that success is assured, delete the old block of memory
            // This isn't normally a safe operation because we would have given out
            // internal pointers to the memory. However when this feature is enabled
            // we track calls that might have given out internal pointers. If none
            // of the APIs were ever called then we can safely delete.
            CLiteWeightStgdbRW* pStgdb = m_pStgdbFreeList;
            m_pStgdbFreeList = m_pStgdbFreeList->m_pNextStgdb;
            delete pStgdb;
        }

        MarkSafeToDeleteStgdb(); // As of right now, no APIs have given out internal pointers
                                 // to the newly allocated stgdb
    }
#endif

    return hr;
} // RegMeta::ReOpenWithMemory


//*****************************************************************************
// This function returns the requested public interface based on the given
// internal import interface.
// A common path to call this is updating the matedata for dynamic modules.
//*****************************************************************************
STDAPI MDReOpenMetaDataWithMemoryEx(
    void        *pImport,               // [IN] Given scope. public interfaces
    LPCVOID     pData,                  // [in] Location of scope data.
    ULONG       cbData,                 // [in] Size of the data pointed to by pData.
    DWORD       dwReOpenFlags)          // [in] Flags for ReOpen
{
    HRESULT             hr = S_OK;
    IUnknown            *pUnk = (IUnknown *) pImport;
    IMetaDataImport2    *pMDImport = NULL;
    RegMeta             *pRegMeta = NULL;

    _ASSERTE(pImport);

    IfFailGo( pUnk->QueryInterface(IID_IMetaDataImport2, (void **) &pMDImport) );
    pRegMeta = (RegMeta*) pMDImport;

    IfFailGo( pRegMeta->ReOpenWithMemory(pData, cbData, dwReOpenFlags) );

ErrExit:
    if (pMDImport)
        pMDImport->Release();

    return hr;
} // MDReOpenMetaDataWithMemoryEx

STDAPI MDReOpenMetaDataWithMemory(
    void        *pImport,               // [IN] Given scope. public interfaces
    LPCVOID     pData,                  // [in] Location of scope data.
    ULONG       cbData)                 // [in] Size of the data pointed to by pData.
{
    return MDReOpenMetaDataWithMemoryEx(pImport, pData, cbData, 0);
}

// --------------------------------------------------------------------------------------
//
// Zeros used by public APIs as return value (or pointer to this memory) for invalid input.
// It is used by methods:
//  * code:RegMeta::GetPublicApiCompatibilityZeros, and
//  * code:RegMeta::GetPublicApiCompatibilityZerosOfSize.
//
const BYTE
RegMeta::s_rgMetaDataPublicApiCompatibilityZeros[64] =
{
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0,
};

// --------------------------------------------------------------------------------------
//
// Returns pointer to zeros of size (cbSize).
// Used by public APIs to return compatible values with previous releases.
//
const BYTE *
RegMeta::GetPublicApiCompatibilityZerosOfSize(UINT32 cbSize)
{
    if (cbSize <= sizeof(s_rgMetaDataPublicApiCompatibilityZeros))
    {
        return s_rgMetaDataPublicApiCompatibilityZeros;
    }
    _ASSERTE(!"Dangerous call to this method! Reconsider fixing the caller.");
    return NULL;
} // RegMeta::GetPublicApiCompatibilityZerosOfSize




//
// returns the "built for" version of a metadata scope.
//
HRESULT RegMeta::GetVersionString(      // S_OK or error.
        LPCSTR      *pVer)              // [OUT] Put version string here.
{
    _ASSERTE(pVer != NULL);
    HRESULT hr;
    LOCKREAD();
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    if (m_pStgdb->m_pvMd != NULL)
    {
#endif
        *pVer = reinterpret_cast<const char*>(reinterpret_cast<const STORAGESIGNATURE*>(m_pStgdb->m_pvMd)->pVersion);
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    }
    else
    {
        //This emptry string matches the fallback behavior we have in other places that query the version string.
        *pVer = "";
    }
#endif
    hr = S_OK;
 ErrExit:
    return hr;
}

#ifdef FEATURE_METADATA_INTERNAL_APIS
//*****************************************************************************
// IGetIMDInternalImport methods
//*****************************************************************************
HRESULT RegMeta::GetIMDInternalImport(
        IMDInternalImport ** ppIMDInternalImport   // [OUT] Buffer to receive IMDInternalImport*
    )
{
    HRESULT       hr = S_OK;
    MDInternalRW *pInternalRW = NULL;
    bool          isLockedForWrite = false;
    IUnknown     *pIUnkInternal = NULL;
    IUnknown     *pThis = (IMetaDataImport2*)this;

    pIUnkInternal = this->GetCachedInternalInterface(TRUE);
    if (pIUnkInternal)
    {
        // there is already a cached Internal interface. GetCachedInternalInterface does add ref the
        // returned interface
        IfFailGo(pIUnkInternal->QueryInterface(IID_IMDInternalImport, (void**)ppIMDInternalImport));
        goto ErrExit;
    }

    if (this->IsThreadSafetyOn())
    {
        _ASSERTE( this->GetReaderWriterLock() );
        IfFailGo(this->GetReaderWriterLock()->LockWrite());
        isLockedForWrite = true;
    }

    // check again. Maybe someone else beat us to setting the internal interface while we are waiting
    // for the write lock. Don't need to grab the read lock since we already have the write lock.
    pIUnkInternal = this->GetCachedInternalInterface(FALSE);
    if (pIUnkInternal)
    {
        // there is already a cached Internal interface. GetCachedInternalInterface does add ref the
        // returned interface
        IfFailGo(pIUnkInternal->QueryInterface(IID_IMDInternalImport, (void**)ppIMDInternalImport));
        goto ErrExit;
    }

    // now create the compressed object
    IfNullGo( pInternalRW = new (nothrow) MDInternalRW );
    IfFailGo( pInternalRW->InitWithStgdb(pThis, this->GetMiniStgdb() ) );

    // make the public object and the internal object point to each other.
    _ASSERTE( pInternalRW->GetReaderWriterLock() == NULL &&
              (! this->IsThreadSafetyOn() || this->GetReaderWriterLock() != NULL ));
    IfFailGo( this->SetCachedInternalInterface(static_cast<IMDInternalImportENC*>(pInternalRW)) );
    IfFailGo( pInternalRW->SetCachedPublicInterface(pThis));
    IfFailGo( pInternalRW->SetReaderWriterLock(this->GetReaderWriterLock() ));
    IfFailGo( pInternalRW->QueryInterface(IID_IMDInternalImport, (void**)ppIMDInternalImport));

ErrExit:
    if (isLockedForWrite == true)
        this->GetReaderWriterLock()->UnlockWrite();
    if (pIUnkInternal)
        pIUnkInternal->Release();
    if (pInternalRW)
        pInternalRW->Release();
    if (FAILED(hr))
    {
        if (ppIMDInternalImport)
            *ppIMDInternalImport = 0;
    }
    return hr;
}
#endif //FEATURE_METADATA_INTERNAL_APIS

