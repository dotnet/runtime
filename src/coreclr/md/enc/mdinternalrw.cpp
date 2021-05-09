// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
//  File: MDInternalRW.cpp
//

//  Notes:
//
//
// ===========================================================================
#include "stdafx.h"
#include "../runtime/mdinternalro.h"
#include "../compiler/regmeta.h"
#include "../compiler/importhelper.h"
#include "mdinternalrw.h"
#include "metamodelro.h"
#include "liteweightstgdb.h"

#ifdef FEATURE_METADATA_INTERNAL_APIS

__checkReturn
HRESULT _GetFixedSigOfVarArg(           // S_OK or error.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob of COM+ method signature
    ULONG   cbSigBlob,                  // [IN] size of signature
    CQuickBytes *pqbSig,                // [OUT] output buffer for fixed part of VarArg Signature
    ULONG   *pcbSigBlob);               // [OUT] number of bytes written to the above output buffer

__checkReturn
HRESULT _FillMDDefaultValue(
    BYTE        bType,
    void const *pValue,
    ULONG       cbValue,
    MDDefaultValue  *pMDDefaultValue);


//*****************************************************************************
// Serve as a delegator to call ImportHelper::MergeUpdateTokenInSig. Or we will
// need to include ImportHelper into our md\runtime directory.
//*****************************************************************************
__checkReturn
HRESULT TranslateSigHelper(                 // S_OK or error.
    IMDInternalImport*      pImport,        // [IN] import scope.
    IMDInternalImport*      pAssemImport,   // [IN] import assembly scope.
    const void*             pbHashValue,    // [IN] hash value for the import assembly.
    ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
    PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
    ULONG                   cbSigBlob,      // [IN] count of bytes of signature
    IMetaDataAssemblyEmit*  pAssemEmit,     // [IN] assembly emit scope.
    IMetaDataEmit*          emit,           // [IN] emit interface
    CQuickBytes*            pqkSigEmit,     // [OUT] buffer to hold translated signature
    ULONG*                  pcbSig)         // [OUT] count of bytes in the translated signature
{
#ifdef FEATURE_METADATA_EMIT
    IMetaModelCommon *pCommon = pImport->GetMetaModelCommon();
    RegMeta     *pAssemEmitRM = static_cast<RegMeta*>(pAssemEmit);
    RegMeta     *pEmitRM      = static_cast<RegMeta*>(emit);

    CMiniMdRW *pMiniMdAssemEmit = pAssemEmitRM ? &pAssemEmitRM->m_pStgdb->m_MiniMd : NULL;
    CMiniMdRW *pMiniMdEmit      = &(pEmitRM->m_pStgdb->m_MiniMd);
    IMetaModelCommon *pCommonAssemImport = pAssemImport ? pAssemImport->GetMetaModelCommon() : NULL;

    return ImportHelper::MergeUpdateTokenInSig(
                pMiniMdAssemEmit,   // The assembly emit scope.
                pMiniMdEmit,        // The emit scope.
                pCommonAssemImport, // Assembly scope where the signature is from.
                pbHashValue,        // Hash value for the import scope.
                cbHashValue,        // Size in bytes.
                pCommon,            // The scope where signature is from.
                pbSigBlob,          // signature from the imported scope
                NULL,               // Internal OID mapping structure.
                pqkSigEmit,         // [OUT] translated signature
                NULL,               // start from first byte of the signature
                NULL,               // don't care how many bytes consumed
                pcbSig);           // [OUT] total number of bytes write to pqkSigEmit

#else //!FEATURE_METADATA_EMIT
    // This API doesn't make sense without supporting public Emit APIs
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT
} // TranslateSigHelper


//*****************************************************************************
// Given an IMDInternalImport on a CMiniMd[RO], convert to CMiniMdRW.
//*****************************************************************************
__checkReturn
STDAPI ConvertRO2RW(
    IUnknown    *pRO,                   // [IN] The RO interface to convert.
    REFIID      riid,                   // [IN] The interface desired.
    void        **ppIUnk)               // [OUT] Return interface on success.
{
    HRESULT     hr = S_OK;              // A result.
    IMDInternalImportENC *pRW = 0;      // To test the RW-ness of the input iface.
    MDInternalRW *pInternalRW = 0;      // Gets the new RW object.
    MDInternalRO *pTrustedRO = 0;

    // Avoid confusion.
    *ppIUnk = 0;

    // If the interface is already RW, done, just return.
    if (pRO->QueryInterface(IID_IMDInternalImportENC, (void**)&pRW) == S_OK)
    {
        hr = pRO->QueryInterface(riid, ppIUnk);
        goto ErrExit;
    }

    // Create the new RW object.
    pInternalRW = new (nothrow) MDInternalRW;
    IfNullGo( pInternalRW );

    // Init from the RO object.  Convert as read-only; QI will make writable if
    //  so needed.

    // ! QI for IID_IUnknown will return MDInternalRO*. Not that COM guarantees such a thing but MDInternalRO knows about
    IfFailGo( pRO->QueryInterface(IID_IUnknown, (void**)&pTrustedRO) );
    IfFailGo( pInternalRW->InitWithRO(pTrustedRO, true));
    IfFailGo( pInternalRW->QueryInterface(riid, ppIUnk) );

ErrExit:
    if (pRW)
        pRW->Release();
    if (pTrustedRO)
        pTrustedRO->Release();
    // Clean up the object and [OUT] interface on error.
    if (FAILED(hr))
    {
        if (pInternalRW)
            delete pInternalRW;
        *ppIUnk = 0;
    }
    else if (pInternalRW)
        pInternalRW->Release();

    return hr;
} // ConvertRO2RW


//*****************************************************************************
// Helper to get the internal interface with RW format
//*****************************************************************************
__checkReturn
HRESULT GetInternalWithRWFormat(
    LPVOID      pData,
    ULONG       cbData,
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk)               // [out] Return interface on success.
{
    MDInternalRW *pInternalRW = NULL;
    HRESULT     hr;

    *ppIUnk = 0;
    pInternalRW = new (nothrow) MDInternalRW;
    IfNullGo( pInternalRW );
    IfFailGo( pInternalRW->Init(
            const_cast<void*>(pData),
            cbData,
            (flags == ofRead) ? true : false) );
    IfFailGo( pInternalRW->QueryInterface(riid, ppIUnk) );
ErrExit:
    if (FAILED(hr))
    {
        if (pInternalRW)
            delete pInternalRW;
        *ppIUnk = 0;
    }
    else if ( pInternalRW )
        pInternalRW->Release();
    return hr;
} // GetInternalWithRWFormat


//*****************************************************************************
// This function returns a IMDInternalImport interface based on the given
// public import interface i.e IMetaDataEmit or IMetaDataImport.
//*****************************************************************************
__checkReturn
STDAPI GetMDInternalInterfaceFromPublic(
    IUnknown    *pIUnkPublic,           // [IN] Given public interface. Must be QI of IUnknown
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnkInternal)       // [out] Return interface on success.
{
    HRESULT hr = S_OK;
    ReleaseHolder<IGetIMDInternalImport> pGetIMDInternalImport;

    // IMDInternalImport is the only internal import interface currently supported by
    // this function.
    _ASSERTE(riid == IID_IMDInternalImport && pIUnkPublic && ppIUnkInternal);

    if (riid != IID_IMDInternalImport || pIUnkPublic == NULL || ppIUnkInternal == NULL)
        IfFailGo(E_INVALIDARG);
    IfFailGo( pIUnkPublic->QueryInterface(IID_IGetIMDInternalImport, &pGetIMDInternalImport));
    IfFailGo( pGetIMDInternalImport->GetIMDInternalImport((IMDInternalImport **)ppIUnkInternal));

ErrExit:
    if (FAILED(hr))
    {
        if (ppIUnkInternal)
            *ppIUnkInternal = 0;
    }
    return hr;
} // GetMDInternalInterfaceFromPublic


//*****************************************************************************
// This function returns the requested public interface based on the given
// internal import interface. It is caller's responsibility to Release ppIUnkPublic
//*****************************************************************************
__checkReturn
STDAPI GetMDPublicInterfaceFromInternal(
    void        *pIUnkInternal,         // [IN] Given internal interface.
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnkPublic)         // [out] Return interface on success.
{
    HRESULT     hr = S_OK;
    IMDInternalImport *pInternalImport = 0;;
    IUnknown    *pIUnkPublic = NULL;
    OptionValue optVal = { MDDupAll, MDRefToDefDefault, MDNotifyDefault, MDUpdateFull, MDErrorOutOfOrderDefault , MDThreadSafetyOn};
    RegMeta     *pMeta = 0;
    bool        isLockedForWrite = false;


    _ASSERTE(pIUnkInternal && ppIUnkPublic);
    *ppIUnkPublic = 0;

    IfFailGo(ConvertRO2RW((IUnknown*)pIUnkInternal, IID_IMDInternalImport, (void **)&pInternalImport));

    pIUnkPublic = pInternalImport->GetCachedPublicInterface(TRUE);
    if ( pIUnkPublic )
    {
        // There is already a cached public interface. GetCachedPublicInterface already AddRef the returned
        // public interface. We want to QueryInterface the riid...
        // We are done!
        hr = pIUnkPublic->QueryInterface(riid, ppIUnkPublic);
        pIUnkPublic->Release();
        goto ErrExit;
    }

    // grab the write lock when we are creating the corresponding regmeta for the public interface
    _ASSERTE( pInternalImport->GetReaderWriterLock() != NULL );
    isLockedForWrite = true;
    IfFailGo(pInternalImport->GetReaderWriterLock()->LockWrite());

    // check again. Maybe someone else beat us to setting the public interface while we are waiting
    // for the write lock. Don't need to grab the read lock since we already have the write lock.
    *ppIUnkPublic = pInternalImport->GetCachedPublicInterface(FALSE);
    if ( *ppIUnkPublic )
    {
        // there is already a cached public interface. GetCachedPublicInterface already AddRef the returned
        // public interface.
        // We are done!
        goto ErrExit;
    }

    pMeta = new (nothrow) RegMeta();
    IfNullGo(pMeta);
    IfFailGo(pMeta->SetOption(&optVal));
    IfFailGo( pMeta->InitWithStgdb((IUnknown*)pInternalImport, ((MDInternalRW*)pInternalImport)->GetMiniStgdb()) );
    IfFailGo( pMeta->QueryInterface(riid, ppIUnkPublic) );

    // The following makes the public object and the internal object point to each other.
    _ASSERTE( pMeta->GetReaderWriterLock() == NULL );
    IfFailGo( pMeta->SetCachedInternalInterface(pInternalImport) );
    IfFailGo( pInternalImport->SetCachedPublicInterface((IUnknown *) *ppIUnkPublic) );
    IfFailGo( pMeta->SetReaderWriterLock(pInternalImport->GetReaderWriterLock() ));

    // Add the new RegMeta to the cache.
    IfFailGo( pMeta->AddToCache() );

ErrExit:
    if (isLockedForWrite)
        pInternalImport->GetReaderWriterLock()->UnlockWrite();

    if (pInternalImport)
        pInternalImport->Release();

    if (FAILED(hr))
    {
        if (pMeta)
            delete pMeta;
        *ppIUnkPublic = 0;
    }
    return hr;
} // GetMDPublicInterfaceFromInternal

//*****************************************************************************
// Converts an internal MD import API into the read/write version of this API.
// This could support edit and continue, or modification of the metadata at
// runtime (say for profiling).
//*****************************************************************************
__checkReturn
STDAPI ConvertMDInternalImport(         // S_OK, S_FALSE (no conversion), or error.
    IMDInternalImport *pIMD,            // [in] The metadata to be updated.
    IMDInternalImport **ppIMD)          // [out] Put the RW here.
{
    HRESULT     hr;                     // A result.
    IMDInternalImportENC *pENC = NULL;  // ENC interface on the metadata.

    _ASSERTE(pIMD != NULL);
    _ASSERTE(ppIMD != NULL);

    // Test whether the MD is already RW.
    hr = pIMD->QueryInterface(IID_IMDInternalImportENC, (void**)&pENC);
    if (FAILED(hr))
    {   // Not yet RW, so do the conversion.
        IfFailGo(ConvertRO2RW(pIMD, IID_IMDInternalImport, (void**)ppIMD));
    }
    else
    {   // Already converted; give back same pointer.
        *ppIMD = pIMD;
        hr = S_FALSE;
    }

ErrExit:
    if (pENC)
        pENC->Release();
    return hr;
} // ConvertMDInternalImport





//*****************************************************************************
// Constructor
//*****************************************************************************
MDInternalRW::MDInternalRW()
 :  m_pStgdb(NULL),
    m_cRefs(1),
    m_fOwnStgdb(false),
    m_pUnk(NULL),
    m_pUserUnk(NULL),
    m_pIMetaDataHelper(NULL),
    m_pSemReadWrite(NULL),
    m_fOwnSem(false)
{
} // MDInternalRW::MDInternalRW



//*****************************************************************************
// Destructor
//*****************************************************************************
MDInternalRW::~MDInternalRW()
{
    HRESULT hr = S_OK;

    LOCKWRITENORET();

    // This should have worked if we've cached the internal interface in the past
    _ASSERTE(SUCCEEDED(hr) || m_pIMetaDataHelper == NULL || m_pIMetaDataHelper->GetCachedInternalInterface(false) == NULL);


    if (SUCCEEDED(hr))
    {

        if (m_pIMetaDataHelper)
        {
            // The internal object is going away before the public object.
            // If the internal object owns the reader writer lock, transfer the ownership
            // to the public object and clear the cached internal interface from the public interface.

            m_pIMetaDataHelper->SetCachedInternalInterface(NULL);
            m_pIMetaDataHelper = NULL;
            m_fOwnSem = false;

        }

        UNLOCKWRITE();
    }
    if (m_pSemReadWrite && m_fOwnSem)
        delete m_pSemReadWrite;

    if ( m_pStgdb && m_fOwnStgdb )
    {
        // we own the stgdb so we need to uninit and delete it.
        m_pStgdb->Uninit();
        delete m_pStgdb;
    }
    if ( m_pUserUnk )
        m_pUserUnk->Release();
    if ( m_pUnk )
        m_pUnk->Release();
} // MDInternalRW::~MDInternalRW


//*****************************************************************************
// Set or clear the cached public interfaces.
// NOTE:: Caller should take a Write lock on the reader writer lock.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::SetCachedPublicInterface(IUnknown * pUnk)
{
    IMetaDataHelper * pHelper = NULL;
    HRESULT           hr = S_OK;

    if (pUnk != NULL)
    {
        // Internal object and public regmeta should be one to one mapping!!
        _ASSERTE(m_pIMetaDataHelper == NULL);

        IfFailRet(pUnk->QueryInterface(IID_IMetaDataHelper, (void **) &pHelper));
        _ASSERTE(pHelper != NULL);

        m_pIMetaDataHelper = pHelper;
        pHelper->Release();
    }
    else
    {
        // public object is going away before the internal object. If we don't own the
        // reader writer lock, just take over the ownership.
        m_fOwnSem = true;
        m_pIMetaDataHelper = NULL;
    }
    return hr;
} // MDInternalRW::SetCachedPublicInterface


//*****************************************************************************
// Clear the cached public interfaces.
//*****************************************************************************
IUnknown * MDInternalRW::GetCachedPublicInterface(BOOL fWithLock)
{
    HRESULT    hr = S_OK;
    IUnknown * pRet = NULL;
    if (fWithLock)
    {
        LOCKREAD();

        pRet = m_pIMetaDataHelper;
        if (pRet != NULL)
            pRet->AddRef();
    }
    else
    {
        pRet = m_pIMetaDataHelper;
        if (pRet != NULL)
            pRet->AddRef();
    }

ErrExit:
    return pRet;
} // MDInternalRW::GetCachedPublicInterface


//*****************************************************************************
// Get the Reader-Writer lock
//*****************************************************************************
UTSemReadWrite * MDInternalRW::GetReaderWriterLock()
{
    return getReaderWriterLock();
} // MDInternalRW::GetReaderWriterLock

//*****************************************************************************
// IUnknown
//*****************************************************************************
ULONG MDInternalRW::AddRef()
{
    return InterlockedIncrement(&m_cRefs);
} // MDInternalRW::AddRef

ULONG MDInternalRW::Release()
{
    ULONG cRef;

    cRef = InterlockedDecrement(&m_cRefs);
    if (cRef == 0)
    {
        LOG((LOGMD, "MDInternalRW(0x%08x)::destruction\n", this));
        delete this;
    }
    return cRef;
} // MDInternalRW::Release

__checkReturn
HRESULT MDInternalRW::QueryInterface(REFIID riid, void **ppUnk)
{
    *ppUnk = 0;

    if (riid == IID_IUnknown)
        *ppUnk = (IUnknown *) (IMDInternalImport *) this;

    else if (riid == IID_IMDInternalImport)
        *ppUnk = (IMDInternalImport *) this;

    else if (riid == IID_IMDInternalImportENC)
        *ppUnk = (IMDInternalImportENC *) this;

    else if (riid == IID_IMDCommon)
        *ppUnk = (IMDCommon*)this;

    else
        return E_NOINTERFACE;
    AddRef();
    return S_OK;
} // MDInternalRW::QueryInterface


//*****************************************************************************
// Initialize
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::Init(
    LPVOID pData,       // points to meta data section in memory
    ULONG  cbData,      // count of bytes in pData
    int    bReadOnly)   // Is it open for read only?
{
    CLiteWeightStgdbRW * pStgdb = NULL;
    HRESULT     hr = NOERROR;
    OptionValue optVal = { MDDupAll, MDRefToDefDefault, MDNotifyDefault, MDUpdateFull, MDErrorOutOfOrderDefault, MDThreadSafetyOn };

    pStgdb = new (nothrow) CLiteWeightStgdbRW;
    IfNullGo(pStgdb);

    m_pSemReadWrite = new (nothrow) UTSemReadWrite;
    IfNullGo(m_pSemReadWrite);
    IfFailGo(m_pSemReadWrite->Init());
    m_fOwnSem = true;
    INDEBUG(pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)

    IfFailGo(pStgdb->InitOnMem(cbData, (BYTE*)pData, bReadOnly));
    IfFailGo(pStgdb->m_MiniMd.SetOption(&optVal));
    m_tdModule = COR_GLOBAL_PARENT_TOKEN;
    m_fOwnStgdb = true;
    m_pStgdb = pStgdb;

ErrExit:
    // clean up upon errors
    if (FAILED(hr) && (pStgdb != NULL))
    {
        delete pStgdb;
    }
    return hr;
} // MDInternalRW::Init


//*****************************************************************************
// Initialize with an existing RegMeta.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::InitWithStgdb(
    IUnknown        *pUnk,              // The IUnknow that owns the life time for the existing stgdb
    CLiteWeightStgdbRW *pStgdb)         // existing lightweight stgdb
{
    // m_fOwnSem should be false because this is the case where we create the internal interface given a public
    // interface.

    m_tdModule = COR_GLOBAL_PARENT_TOKEN;
    m_fOwnStgdb = false;
    m_pStgdb = pStgdb;

    // remember the owner of the light weight stgdb
    // AddRef it to ensure the lifetime
    //
    m_pUnk = pUnk;
    m_pUnk->AddRef();
    return NOERROR;
} // MDInternalRW::InitWithStgdb


//*****************************************************************************
// Initialize with an existing RO format
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::InitWithRO(
    MDInternalRO * pRO,
    int            bReadOnly)
{
    CLiteWeightStgdbRW * pStgdb = NULL;
    HRESULT     hr = NOERROR;
    OptionValue optVal = { MDDupAll, MDRefToDefDefault, MDNotifyDefault, MDUpdateFull, MDErrorOutOfOrderDefault, MDThreadSafetyOn };

    pStgdb = new (nothrow) CLiteWeightStgdbRW;
    IfNullGo(pStgdb);

    m_pSemReadWrite = new (nothrow) UTSemReadWrite;
    IfNullGo(m_pSemReadWrite);
    IfFailGo(m_pSemReadWrite->Init());
    m_fOwnSem = true;
    INDEBUG(pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)

    IfFailGo(pStgdb->m_MiniMd.InitOnRO(&pRO->m_LiteWeightStgdb.m_MiniMd, bReadOnly));
    IfFailGo(pStgdb->m_MiniMd.SetOption(&optVal));
    m_tdModule = COR_GLOBAL_PARENT_TOKEN;
    m_fOwnStgdb = true;
    pStgdb->m_pvMd=pRO->m_LiteWeightStgdb.m_pvMd;
    pStgdb->m_cbMd=pRO->m_LiteWeightStgdb.m_cbMd;
    m_pStgdb = pStgdb;

ErrExit:
    // clean up upon errors
    if (FAILED(hr) && (pStgdb != NULL))
    {
        delete pStgdb;
    }
    return hr;
} // MDInternalRW::InitWithRO


#ifndef DACCESS_COMPILE
//*****************************************************************************
// Given a scope, determine whether imported from a typelib.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::TranslateSigWithScope(
    IMDInternalImport*      pAssemImport,   // [IN] import assembly scope.
    const void*             pbHashValue,    // [IN] hash value for the import assembly.
    ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
    PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
    ULONG                   cbSigBlob,      // [IN] count of bytes of signature
    IMetaDataAssemblyEmit*  pAssemEmit,     // [IN] assembly emit scope.
    IMetaDataEmit*          emit,           // [IN] emit interface
    CQuickBytes*            pqkSigEmit,     // [OUT] buffer to hold translated signature
    ULONG*                  pcbSig)         // [OUT] count of bytes in the translated signature
{
    return TranslateSigHelper(
                this,
                pAssemImport,
                pbHashValue,
                cbHashValue,
                pbSigBlob,
                cbSigBlob,
                pAssemEmit,
                emit,
                pqkSigEmit,
                pcbSig);
} // MDInternalRW::TranslateSigWithScope

__checkReturn
HRESULT MDInternalRW::GetTypeDefRefTokenInTypeSpec(// return S_FALSE if enclosing type does not have a token
                                                    mdTypeSpec  tkTypeSpec,             // [IN] TypeSpec token to look at
                                                    mdToken    *tkEnclosedToken)       // [OUT] The enclosed type token
{
    return m_pStgdb->m_MiniMd.GetTypeDefRefTokenInTypeSpec(tkTypeSpec, tkEnclosedToken);
}// MDInternalRW::GetTypeDefRefTokenInTypeSpec

//*****************************************************************************
// Given a scope, return the number of tokens in a given table
//*****************************************************************************
ULONG MDInternalRW::GetCountWithTokenKind(     // return hresult
    DWORD       tkKind)                 // [IN] pass in the kind of token.
{
    ULONG       ulCount = 0;
    HRESULT hr = S_OK;
    LOCKREAD();

    switch (tkKind)
    {
    case mdtTypeDef:
        ulCount = m_pStgdb->m_MiniMd.getCountTypeDefs();
        // Remove global typedef from the count of typedefs (and handle the case where there is no global typedef)
        if (ulCount > 0)
            ulCount--;
        break;
    case mdtTypeRef:
        ulCount = m_pStgdb->m_MiniMd.getCountTypeRefs();
        break;
    case mdtMethodDef:
        ulCount = m_pStgdb->m_MiniMd.getCountMethods();
        break;
    case mdtFieldDef:
        ulCount = m_pStgdb->m_MiniMd.getCountFields();
        break;
    case mdtMemberRef:
        ulCount = m_pStgdb->m_MiniMd.getCountMemberRefs();
        break;
    case mdtInterfaceImpl:
        ulCount = m_pStgdb->m_MiniMd.getCountInterfaceImpls();
        break;
    case mdtParamDef:
        ulCount = m_pStgdb->m_MiniMd.getCountParams();
        break;
    case mdtFile:
        ulCount = m_pStgdb->m_MiniMd.getCountFiles();
        break;
    case mdtAssemblyRef:
        ulCount = m_pStgdb->m_MiniMd.getCountAssemblyRefs();
        break;
    case mdtAssembly:
        ulCount = m_pStgdb->m_MiniMd.getCountAssemblys();
        break;
    case mdtCustomAttribute:
        ulCount = m_pStgdb->m_MiniMd.getCountCustomAttributes();
        break;
    case mdtModule:
        ulCount = m_pStgdb->m_MiniMd.getCountModules();
        break;
    case mdtPermission:
        ulCount = m_pStgdb->m_MiniMd.getCountDeclSecuritys();
        break;
    case mdtSignature:
        ulCount = m_pStgdb->m_MiniMd.getCountStandAloneSigs();
        break;
    case mdtEvent:
        ulCount = m_pStgdb->m_MiniMd.getCountEvents();
        break;
    case mdtProperty:
        ulCount = m_pStgdb->m_MiniMd.getCountPropertys();
        break;
    case mdtModuleRef:
        ulCount = m_pStgdb->m_MiniMd.getCountModuleRefs();
        break;
    case mdtTypeSpec:
        ulCount = m_pStgdb->m_MiniMd.getCountTypeSpecs();
        break;
    case mdtExportedType:
        ulCount = m_pStgdb->m_MiniMd.getCountExportedTypes();
        break;
    case mdtManifestResource:
        ulCount = m_pStgdb->m_MiniMd.getCountManifestResources();
        break;
    case mdtGenericParam:
        ulCount = m_pStgdb->m_MiniMd.getCountGenericParams();
        break;
    case mdtGenericParamConstraint:
        ulCount = m_pStgdb->m_MiniMd.getCountGenericParamConstraints();
        break;
    case mdtMethodSpec:
        ulCount = m_pStgdb->m_MiniMd.getCountMethodSpecs();
        break;
    default:
#ifdef _DEBUG
        if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertOnBadImageFormat, 1))
            _ASSERTE(!"Invalid Blob Offset");
#endif
        ulCount = 0;
        break;
    }

ErrExit:

    return ulCount;
} // MDInternalRW::GetCountWithTokenKind
#endif //!DACCESS_COMPILE


//*******************************************************************************
// Enumerator helpers
//*******************************************************************************


//*****************************************************************************
// enumerator init for typedef
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumTypeDefInit( // return hresult
    HENUMInternal *phEnum)              // [OUT] buffer to fill for enumerator data
{
    HRESULT     hr = NOERROR;
    LOCKREAD();

    _ASSERTE(phEnum);

    HENUMInternal::ZeroEnum(phEnum);
    phEnum->m_tkKind = mdtTypeDef;

    if ( m_pStgdb->m_MiniMd.HasDelete() )
    {
        HENUMInternal::InitDynamicArrayEnum(phEnum);

        phEnum->m_tkKind = mdtTypeDef;
        for (ULONG index = 2; index <= m_pStgdb->m_MiniMd.getCountTypeDefs(); index ++ )
        {
            TypeDefRec *pTypeDefRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(index, &pTypeDefRec));
            LPCSTR szTypeDefName;
            IfFailGo(m_pStgdb->m_MiniMd.getNameOfTypeDef(pTypeDefRec, &szTypeDefName));
            if (IsDeletedName(szTypeDefName))
            {
                continue;
            }
            IfFailGo( HENUMInternal::AddElementToEnum(
                phEnum,
                TokenFromRid(index, mdtTypeDef) ) );
        }
    }
    else
    {
        phEnum->m_EnumType = MDSimpleEnum;
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountTypeDefs();

        // Skip over the global model typedef
        //
        // phEnum->u.m_ulCur : the current rid that is not yet enumerated
        // phEnum->u.m_ulStart : the first rid that will be returned by enumerator
        // phEnum->u.m_ulEnd : the last rid that will be returned by enumerator
        phEnum->u.m_ulStart = phEnum->u.m_ulCur = 2;
        phEnum->u.m_ulEnd = phEnum->m_ulCount + 1;
        if (phEnum->m_ulCount > 0)
            phEnum->m_ulCount --;
    }
ErrExit:

    return hr;
} // MDInternalRW::EnumTypeDefInit

#ifndef DACCESS_COMPILE
//*****************************************************************************
// enumerator init for MethodImpl
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumMethodImplInit( // return hresult
    mdTypeDef       td,                   // [IN] TypeDef over which to scope the enumeration.
    HENUMInternal   *phEnumBody,          // [OUT] buffer to fill for enumerator data for MethodBody tokens.
    HENUMInternal   *phEnumDecl)          // [OUT] buffer to fill for enumerator data for MethodDecl tokens.
{
    HRESULT     hr = NOERROR;
    int         ridCur;
    mdToken     tkMethodBody;
    mdToken     tkMethodDecl;
    MethodImplRec *pRec;
    HENUMInternal hEnum;

    LOCKREAD();

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && !IsNilToken(td));
    _ASSERTE(phEnumBody && phEnumDecl);

    HENUMInternal::ZeroEnum(phEnumBody);
    HENUMInternal::ZeroEnum(phEnumDecl);
    HENUMInternal::ZeroEnum(&hEnum);

    HENUMInternal::InitDynamicArrayEnum(phEnumBody);
    HENUMInternal::InitDynamicArrayEnum(phEnumDecl);

    phEnumBody->m_tkKind = (TBL_MethodImpl << 24);
    phEnumDecl->m_tkKind = (TBL_MethodImpl << 24);

    // Get the range of rids.
    IfFailGo( m_pStgdb->m_MiniMd.FindMethodImplHelper(td, &hEnum) );

    while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
    {
        // Get the MethodBody and MethodDeclaration tokens for the current
        // MethodImpl record.
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodImplRecord(ridCur, &pRec));
        tkMethodBody = m_pStgdb->m_MiniMd.getMethodBodyOfMethodImpl(pRec);
        tkMethodDecl = m_pStgdb->m_MiniMd.getMethodDeclarationOfMethodImpl(pRec);

        // Add the Method body/declaration pairs to the Enum
        IfFailGo( HENUMInternal::AddElementToEnum(phEnumBody, tkMethodBody ) );
        IfFailGo( HENUMInternal::AddElementToEnum(phEnumDecl, tkMethodDecl ) );
    }
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    return hr;
} // MDInternalRW::EnumMethodImplInit

//*****************************************************************************
// get the number of MethodImpls in a scope
//*****************************************************************************
ULONG MDInternalRW::EnumMethodImplGetCount(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl  &&
             (phEnumDecl->m_tkKind >> 24) == TBL_MethodImpl);
    _ASSERTE(phEnumBody->m_EnumType == MDDynamicArrayEnum &&
             phEnumDecl->m_EnumType == MDDynamicArrayEnum);
    _ASSERTE(phEnumBody->m_ulCount == phEnumDecl->m_ulCount);

    return phEnumBody->m_ulCount;
} // MDInternalRW::EnumMethodImplGetCount


//*****************************************************************************
// enumerator for MethodImpl.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::EnumMethodImplNext(  // return hresult
    HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
    HENUMInternal   *phEnumDecl,        // [IN] input enum for MethodDecl
    mdToken         *ptkBody,           // [OUT] return token for MethodBody
    mdToken         *ptkDecl)           // [OUT] return token for MethodDecl
{
    _ASSERTE((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl &&
             (phEnumDecl->m_tkKind >> 24) == TBL_MethodImpl);
    _ASSERTE(phEnumBody->m_EnumType == MDDynamicArrayEnum &&
             phEnumDecl->m_EnumType == MDDynamicArrayEnum);
    _ASSERTE(phEnumBody->m_ulCount == phEnumDecl->m_ulCount);
    _ASSERTE(ptkBody && ptkDecl);

    EnumNext(phEnumBody, ptkBody);
    return EnumNext(phEnumDecl, ptkDecl) ? S_OK : S_FALSE;
} // MDInternalRW::EnumMethodImplNext

//*****************************************
// Reset the enumerator to the beginning.
//*****************************************
void MDInternalRW::EnumMethodImplReset(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl &&
             (phEnumDecl->m_tkKind >> 24) == TBL_MethodImpl);
    _ASSERTE(phEnumBody->m_EnumType == MDDynamicArrayEnum &&
             phEnumDecl->m_EnumType == MDDynamicArrayEnum);
    _ASSERTE(phEnumBody->m_ulCount == phEnumDecl->m_ulCount);

    EnumReset(phEnumBody);
    EnumReset(phEnumDecl);
} // MDInternalRW::EnumMethodImplReset


//*****************************************
// Close the enumerator.
//*****************************************
void MDInternalRW::EnumMethodImplClose(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl &&
             (phEnumDecl->m_tkKind >> 24) == TBL_MethodImpl);
    _ASSERTE(phEnumBody->m_EnumType == MDDynamicArrayEnum &&
             phEnumDecl->m_EnumType == MDDynamicArrayEnum);
    _ASSERTE(phEnumBody->m_ulCount == phEnumDecl->m_ulCount);

    EnumClose(phEnumBody);
    EnumClose(phEnumDecl);
} // MDInternalRW::EnumMethodImplClose
#endif //!DACCESS_COMPILE

//******************************************************************************
// enumerator for global functions
//******************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumGlobalFunctionsInit(  // return hresult
    HENUMInternal   *phEnum)            // [OUT] buffer to fill for enumerator data
{
    return EnumInit(mdtMethodDef, m_tdModule, phEnum);
} // MDInternalRW::EnumGlobalFunctionsInit


//******************************************************************************
// enumerator for global fields
//******************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumGlobalFieldsInit( // return hresult
    HENUMInternal   *phEnum)            // [OUT] buffer to fill for enumerator data
{
    return EnumInit(mdtFieldDef, m_tdModule, phEnum);
} // MDInternalRW::EnumGlobalFieldsInit


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
//*****************************************
// Enumerator initializer
//*****************************************
__checkReturn
HRESULT MDInternalRW::EnumInit(     // return S_FALSE if record not found
    DWORD       tkKind,                 // [IN] which table to work on
    mdToken     tkParent,               // [IN] token to scope the search
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    HRESULT     hr = S_OK;
    uint32_t    ulStart, ulEnd, ulMax;
    uint32_t    index;
    LOCKREAD();

    // Vars for query.
    _ASSERTE(phEnum);
    HENUMInternal::ZeroEnum(phEnum);

    // cache the tkKind and the scope
    phEnum->m_tkKind = TypeFromToken(tkKind);

    TypeDefRec  *pRec;

    phEnum->m_EnumType = MDSimpleEnum;

    switch (TypeFromToken(tkKind))
    {
    case mdtFieldDef:
        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(tkParent), &pRec));
        ulStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(tkParent), &ulEnd));
        if ( m_pStgdb->m_MiniMd.HasDelete() )
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                FieldRec *pFieldRec;
                RID fieldRid;
                IfFailGo(m_pStgdb->m_MiniMd.GetFieldRid(index, &fieldRid));
                IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(index, &pFieldRec));
                LPCSTR szFieldName;
                IfFailGo(m_pStgdb->m_MiniMd.getNameOfField(pFieldRec, &szFieldName));
                if (IsFdRTSpecialName(pFieldRec->GetFlags()) && IsDeletedName(szFieldName) )
                {
                    continue;
                }
                IfFailGo(m_pStgdb->m_MiniMd.GetFieldRid(index, &fieldRid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(fieldRid, mdtFieldDef)));
            }
        }
        else if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Field))
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                RID fieldRid;
                IfFailGo(m_pStgdb->m_MiniMd.GetFieldRid(index, &fieldRid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(fieldRid, mdtFieldDef)));
            }
        }
        else
        {
            HENUMInternal::InitSimpleEnum( mdtFieldDef, ulStart, ulEnd, phEnum);
        }
        break;

    case mdtMethodDef:
        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(tkParent), &pRec));
        ulStart = m_pStgdb->m_MiniMd.getMethodListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(tkParent), &ulEnd));
        if ( m_pStgdb->m_MiniMd.HasDelete() )
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                MethodRec *pMethodRec;
                RID methodRid;
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodRid(index, &methodRid));
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(methodRid, &pMethodRec));
                LPCSTR szMethodName;
                IfFailGo(m_pStgdb->m_MiniMd.getNameOfMethod(pMethodRec, &szMethodName));
                if (IsMdRTSpecialName(pMethodRec->GetFlags()) && IsDeletedName(szMethodName))
                {
                    continue;
                }
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodRid(index, &methodRid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(methodRid, mdtMethodDef)));
            }
        }
        else if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Method))
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                RID methodRid;
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodRid(index, &methodRid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(methodRid, mdtMethodDef)));
            }
        }
        else
        {
            HENUMInternal::InitSimpleEnum( mdtMethodDef, ulStart, ulEnd, phEnum);
        }
        break;

    case mdtInterfaceImpl:
        if (!m_pStgdb->m_MiniMd.IsSorted(TBL_InterfaceImpl) && !m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_InterfaceImpl))
        {
            // virtual sort table will be created!
            //
            CONVERT_READ_TO_WRITE_LOCK();
        }

        IfFailGo( m_pStgdb->m_MiniMd.GetInterfaceImplsForTypeDef(RidFromToken(tkParent), &ulStart, &ulEnd) );
        if ( m_pStgdb->m_MiniMd.IsSorted( TBL_InterfaceImpl ) )
        {
            // These are index to InterfaceImpl table directly
            HENUMInternal::InitSimpleEnum( mdtInterfaceImpl, ulStart, ulEnd, phEnum);
        }
        else
        {
            // These are index to VirtualSort table. Skip over one level direction.
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(m_pStgdb->m_MiniMd.GetInterfaceImplRid(index), mdtInterfaceImpl) ) );
            }
        }
        break;

    case mdtGenericParam:
        //@todo: deal with non-sorted case.

        if (TypeFromToken(tkParent) == mdtTypeDef)
        {
            IfFailGo(m_pStgdb->m_MiniMd.getGenericParamsForTypeDef(
                RidFromToken(tkParent),
                &phEnum->u.m_ulEnd,
                &(phEnum->u.m_ulStart)));
        }
        else
        {
            IfFailGo(m_pStgdb->m_MiniMd.getGenericParamsForMethodDef(
                RidFromToken(tkParent),
                &phEnum->u.m_ulEnd,
                &(phEnum->u.m_ulStart)));
        }
        break;

    case mdtGenericParamConstraint:
        if ( !m_pStgdb->m_MiniMd.IsSorted(TBL_GenericParamConstraint) && !m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_GenericParamConstraint))
        {
            // virtual sort table will be created!
            //
            CONVERT_READ_TO_WRITE_LOCK();
        }

        IfFailGo( m_pStgdb->m_MiniMd.GetGenericParamConstraintsForToken(RidFromToken(tkParent), &ulStart, &ulEnd) );
        if ( m_pStgdb->m_MiniMd.IsSorted( TBL_GenericParamConstraint ) )
        {
            // These are index to GenericParamConstraint table directly
            HENUMInternal::InitSimpleEnum( mdtGenericParamConstraint, ulStart, ulEnd, phEnum);
        }
        else
        {
            // These are index to VirtualSort table. Skip over one level direction.
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(m_pStgdb->m_MiniMd.GetGenericParamConstraintRid(index), mdtGenericParamConstraint) ) );
            }
        }
        break;

    case mdtProperty:
        RID         ridPropertyMap;
        PropertyMapRec *pPropertyMapRec;

        // get the starting/ending rid of properties of this typedef
        IfFailGo(m_pStgdb->m_MiniMd.FindPropertyMapFor(RidFromToken(tkParent), &ridPropertyMap));
        if (!InvalidRid(ridPropertyMap))
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
            ulStart = m_pStgdb->m_MiniMd.getPropertyListOfPropertyMap(pPropertyMapRec);
            IfFailGo(m_pStgdb->m_MiniMd.getEndPropertyListOfPropertyMap(ridPropertyMap, &ulEnd));
            ulMax = m_pStgdb->m_MiniMd.getCountPropertys() + 1;
            if(ulStart == 0) ulStart = 1;
            if(ulEnd > ulMax) ulEnd = ulMax;
            if(ulStart > ulEnd) ulStart = ulEnd;
            if ( m_pStgdb->m_MiniMd.HasDelete() )
            {
                HENUMInternal::InitDynamicArrayEnum(phEnum);
                for (index = ulStart; index < ulEnd; index ++ )
                {
                    PropertyRec *pPropertyRec;
                    RID propertyRid;
                    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRid(index, &propertyRid));
                    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(
                        propertyRid,
                        &pPropertyRec));
                    LPCSTR szPropertyName;
                    IfFailGo(m_pStgdb->m_MiniMd.getNameOfProperty(pPropertyRec, &szPropertyName));
                    if (IsPrRTSpecialName(pPropertyRec->GetPropFlags()) && IsDeletedName(szPropertyName))
                    {
                        continue;
                    }
                    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRid(index, &propertyRid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        phEnum,
                        TokenFromRid(propertyRid, mdtProperty)));
                }
            }
            else if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Property))
            {
                HENUMInternal::InitDynamicArrayEnum(phEnum);
                for (index = ulStart; index < ulEnd; index ++ )
                {
                    RID propertyRid;
                    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRid(index, &propertyRid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        phEnum,
                        TokenFromRid(propertyRid, mdtProperty)));
                }
            }
            else
            {
                HENUMInternal::InitSimpleEnum( mdtProperty, ulStart, ulEnd, phEnum);
            }
        }
        break;

    case mdtEvent:
        RID         ridEventMap;
        EventMapRec *pEventMapRec;

        // get the starting/ending rid of events of this typedef
        IfFailGo(m_pStgdb->m_MiniMd.FindEventMapFor(RidFromToken(tkParent), &ridEventMap));
        if (!InvalidRid(ridEventMap))
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetEventMapRecord(ridEventMap, &pEventMapRec));
            ulStart = m_pStgdb->m_MiniMd.getEventListOfEventMap(pEventMapRec);
            IfFailGo(m_pStgdb->m_MiniMd.getEndEventListOfEventMap(ridEventMap, &ulEnd));
            ulMax = m_pStgdb->m_MiniMd.getCountEvents() + 1;
            if(ulStart == 0) ulStart = 1;
            if(ulEnd > ulMax) ulEnd = ulMax;
            if(ulStart > ulEnd) ulStart = ulEnd;
            if ( m_pStgdb->m_MiniMd.HasDelete() )
            {
                HENUMInternal::InitDynamicArrayEnum(phEnum);
                for (index = ulStart; index < ulEnd; index ++ )
                {
                    EventRec *pEventRec;
                    RID eventRid;
                    IfFailGo(m_pStgdb->m_MiniMd.GetEventRid(index, &eventRid));
                    IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(eventRid, &pEventRec));
                    LPCSTR szEventName;
                    IfFailGo(m_pStgdb->m_MiniMd.getNameOfEvent(pEventRec, &szEventName));
                    if (IsEvRTSpecialName(pEventRec->GetEventFlags()) && IsDeletedName(szEventName))
                    {
                        continue;
                    }
                    IfFailGo(m_pStgdb->m_MiniMd.GetEventRid(index, &eventRid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        phEnum,
                        TokenFromRid(eventRid, mdtEvent)));
                }
            }
            else if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Event))
            {
                HENUMInternal::InitDynamicArrayEnum(phEnum);
                for (index = ulStart; index < ulEnd; index ++ )
                {
                    RID eventRid;
                    IfFailGo(m_pStgdb->m_MiniMd.GetEventRid(index, &eventRid));
                    IfFailGo( HENUMInternal::AddElementToEnum(
                        phEnum,
                        TokenFromRid(eventRid, mdtEvent) ) );
                }
            }
            else
            {
                HENUMInternal::InitSimpleEnum( mdtEvent, ulStart, ulEnd, phEnum);
            }
        }
        break;

    case mdtParamDef:
        _ASSERTE(TypeFromToken(tkParent) == mdtMethodDef);

        MethodRec *pMethodRec;
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tkParent), &pMethodRec));

        // figure out the start rid and end rid of the parameter list of this methoddef
        ulStart = m_pStgdb->m_MiniMd.getParamListOfMethod(pMethodRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndParamListOfMethod(RidFromToken(tkParent), &ulEnd));
        if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Param))
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                RID paramRid;
                IfFailGo(m_pStgdb->m_MiniMd.GetParamRid(index, &paramRid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(paramRid, mdtParamDef)));
            }
        }
        else
        {
            HENUMInternal::InitSimpleEnum( mdtParamDef, ulStart, ulEnd, phEnum);
        }
        break;

    case mdtCustomAttribute:
        if (!m_pStgdb->m_MiniMd.IsSorted(TBL_CustomAttribute) && !m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_CustomAttribute))
        {
            // CA's map table table will be sorted!
            //
            CONVERT_READ_TO_WRITE_LOCK();
        }

        IfFailGo( m_pStgdb->m_MiniMd.GetCustomAttributeForToken(tkParent, &ulStart, &ulEnd) );
        if ( m_pStgdb->m_MiniMd.IsSorted( TBL_CustomAttribute ) )
        {
            // These are index to CustomAttribute table directly
            HENUMInternal::InitSimpleEnum( mdtCustomAttribute, ulStart, ulEnd, phEnum);
        }
        else
        {
            // These are index to VirtualSort table. Skip over one level direction.
            HENUMInternal::InitDynamicArrayEnum(phEnum);
            for (index = ulStart; index < ulEnd; index ++ )
            {
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(m_pStgdb->m_MiniMd.GetCustomAttributeRid(index), mdtCustomAttribute) ) );
            }
        }
        break;
    case mdtAssemblyRef:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_pStgdb->m_MiniMd.getCountAssemblyRefs() + 1;
        break;
    case mdtFile:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_pStgdb->m_MiniMd.getCountFiles() + 1;
        break;
    case mdtExportedType:
        _ASSERTE(IsNilToken(tkParent));
        if ( m_pStgdb->m_MiniMd.HasDelete() )
        {
            HENUMInternal::InitDynamicArrayEnum(phEnum);

            phEnum->m_tkKind = mdtExportedType;
            for (ULONG typeindex = 1; typeindex <= m_pStgdb->m_MiniMd.getCountExportedTypes(); typeindex ++ )
            {
                ExportedTypeRec *pExportedTypeRec;
                IfFailGo(m_pStgdb->m_MiniMd.GetExportedTypeRecord(typeindex, &pExportedTypeRec));
                LPCSTR szTypeName;
                IfFailGo(m_pStgdb->m_MiniMd.getTypeNameOfExportedType(pExportedTypeRec, &szTypeName));
                if (IsDeletedName(szTypeName))
                {
                    continue;
                }
                IfFailGo( HENUMInternal::AddElementToEnum(
                    phEnum,
                    TokenFromRid(typeindex, mdtExportedType) ) );
            }
        }
        else
        {
            phEnum->u.m_ulStart = 1;
            phEnum->u.m_ulEnd = m_pStgdb->m_MiniMd.getCountExportedTypes() + 1;
        }
        break;
    case mdtManifestResource:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_pStgdb->m_MiniMd.getCountManifestResources() + 1;
        break;
    case mdtModuleRef:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_pStgdb->m_MiniMd.getCountModuleRefs() + 1;
        break;
    default:
        _ASSERTE(!"ENUM INIT not implemented for the uncompressed format!");
        IfFailGo(E_NOTIMPL);
        break;
    }

    // If the count is negative, the metadata is corrupted somehow.
    if (phEnum->u.m_ulEnd < phEnum->u.m_ulStart)
        IfFailGo(CLDB_E_FILE_CORRUPT);

    phEnum->m_ulCount = phEnum->u.m_ulEnd - phEnum->u.m_ulStart;
    phEnum->u.m_ulCur = phEnum->u.m_ulStart;
ErrExit:
    // we are done

    return hr;
} // MDInternalRW::EnumInit
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************
// Enumerator initializer
//*****************************************
__checkReturn
HRESULT MDInternalRW::EnumAllInit(      // return S_FALSE if record not found
    DWORD       tkKind,                 // [IN] which table to work on
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    HRESULT     hr = S_OK;
    LOCKREAD();

    // Vars for query.
    _ASSERTE(phEnum);
    HENUMInternal::ZeroEnum(phEnum);

    // cache the tkKind and the scope
    phEnum->m_tkKind = TypeFromToken(tkKind);
    phEnum->m_EnumType = MDSimpleEnum;

    switch (TypeFromToken(tkKind))
    {
    case mdtTypeRef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountTypeRefs();
        break;

    case mdtMemberRef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountMemberRefs();
        break;

    case mdtSignature:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountStandAloneSigs();
        break;

    case mdtMethodDef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountMethods();
        break;

    case mdtMethodSpec:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountMethodSpecs();
        break;

    case mdtFieldDef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountFields();
        break;

    case mdtTypeSpec:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountTypeSpecs();
        break;

    case mdtAssemblyRef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountAssemblyRefs();
        break;

    case mdtModuleRef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountModuleRefs();
        break;

    case mdtTypeDef:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountTypeDefs();
        break;

    case mdtFile:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountFiles();
        break;

    case mdtCustomAttribute:
        phEnum->m_ulCount = m_pStgdb->m_MiniMd.getCountCustomAttributes();
        break;

    default:
        _ASSERTE(!"Bad token kind!");
        break;
    }
    phEnum->u.m_ulStart = phEnum->u.m_ulCur = 1;
    phEnum->u.m_ulEnd = phEnum->m_ulCount + 1;

ErrExit:
    // we are done

    return hr;
} // MDInternalRW::EnumAllInit

//*****************************************
// Enumerator initializer for CustomAttributes
//*****************************************
__checkReturn
HRESULT MDInternalRW::EnumCustomAttributeByNameInit(// return S_FALSE if record not found
    mdToken     tkParent,               // [IN] token to scope the search
    LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    return m_pStgdb->m_MiniMd.CommonEnumCustomAttributeByName(tkParent, szName, false, phEnum);
}   // MDInternalRW::EnumCustomAttributeByNameInit

//*****************************************
// Nagivator helper to navigate back to the parent token given a token.
// For example, given a memberdef token, it will return the containing typedef.
//
// the mapping is as following:
//  ---given child type---------parent type
//  mdMethodDef                 mdTypeDef
//  mdFieldDef                  mdTypeDef
//  mdInterfaceImpl             mdTypeDef
//  mdParam                     mdMethodDef
//  mdProperty                  mdTypeDef
//  mdEvent                     mdTypeDef
//
//*****************************************
__checkReturn
HRESULT MDInternalRW::GetParentToken(
    mdToken     tkChild,                // [IN] given child token
    mdToken     *ptkParent)             // [OUT] returning parent
{
    HRESULT hr = NOERROR;
    LOCKREAD();

    _ASSERTE(ptkParent);

    switch (TypeFromToken(tkChild))
    {
    case mdtTypeDef:
        {
            RID rid;
            if (!m_pStgdb->m_MiniMd.IsSorted(TBL_NestedClass) && !m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_NestedClass))
            {
                // NestedClass table is not sorted.
                CONVERT_READ_TO_WRITE_LOCK();
            }
            IfFailGo(m_pStgdb->m_MiniMd.FindNestedClassFor(RidFromToken(tkChild), &rid));

            if (InvalidRid(rid))
            {
                // If not found, the *ptkParent has to be left unchanged! (callers depend on that)
                hr = S_OK;
            }
            else
            {
                NestedClassRec *pRecord;
                IfFailGo(m_pStgdb->m_MiniMd.GetNestedClassRecord(rid, &pRecord));
                *ptkParent = m_pStgdb->m_MiniMd.getEnclosingClassOfNestedClass(pRecord);
            }
            break;
        }
    case mdtMethodDef:
        IfFailGo(m_pStgdb->m_MiniMd.FindParentOfMethodHelper(RidFromToken(tkChild), ptkParent));
        RidToToken(*ptkParent, mdtTypeDef);
        break;
    case mdtMethodSpec:
        {
            MethodSpecRec *pRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodSpecRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_pStgdb->m_MiniMd.getMethodOfMethodSpec(pRec);
        }
        break;
    case mdtFieldDef:
        IfFailGo(m_pStgdb->m_MiniMd.FindParentOfFieldHelper(RidFromToken(tkChild), ptkParent));
        RidToToken(*ptkParent, mdtTypeDef);
        break;
    case mdtParamDef:
        IfFailGo(m_pStgdb->m_MiniMd.FindParentOfParamHelper(RidFromToken(tkChild), ptkParent));
        RidToToken(*ptkParent, mdtMethodDef);
        break;
    case mdtMemberRef:
        {
            MemberRefRec *pRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetMemberRefRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_pStgdb->m_MiniMd.getClassOfMemberRef(pRec);
            break;
        }
    case mdtCustomAttribute:
        {
            CustomAttributeRec *pRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_pStgdb->m_MiniMd.getParentOfCustomAttribute(pRec);
            break;
        }
    case mdtEvent:
        IfFailGo(m_pStgdb->m_MiniMd.FindParentOfEventHelper(tkChild, ptkParent));
        break;
    case mdtProperty:
        IfFailGo(m_pStgdb->m_MiniMd.FindParentOfPropertyHelper(tkChild, ptkParent));
        break;
    default:
        _ASSERTE(!"NYI: for compressed format!");
        break;
    }
ErrExit:
    return hr;
} // MDInternalRW::GetParentToken

//*****************************************************************************
// Get information about a CustomAttribute.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetCustomAttributeProps( // S_OK or error.
    mdCustomAttribute at,                   // The attribute.
    mdToken     *pTkType)               // Put attribute type here.
{
    HRESULT hr;
    // Getting the custom value prop with a token, no need to lock!

    _ASSERTE(TypeFromToken(at) == mdtCustomAttribute);

    // Do a linear search on compressed version as we do not want to
    // depend on ICR.
    //
    CustomAttributeRec *pCustomAttributeRec;

    IfFailRet(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(RidFromToken(at), &pCustomAttributeRec));
    *pTkType = m_pStgdb->m_MiniMd.getTypeOfCustomAttribute(pCustomAttributeRec);
    return S_OK;
} // MDInternalRW::GetCustomAttributeProps


//*****************************************************************************
// return custom value
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetCustomAttributeAsBlob(
    mdCustomAttribute cv,               // [IN] given custom attribute token
    void const  **ppBlob,               // [OUT] return the pointer to internal blob
    ULONG       *pcbSize)               // [OUT] return the size of the blob
{
    // Getting the custom value prop with a token, no need to lock!
    HRESULT hr;
    _ASSERTE(ppBlob && pcbSize && TypeFromToken(cv) == mdtCustomAttribute);

    CustomAttributeRec *pCustomAttributeRec;

    IfFailRet(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(RidFromToken(cv), &pCustomAttributeRec));

    IfFailRet(m_pStgdb->m_MiniMd.getValueOfCustomAttribute(pCustomAttributeRec, reinterpret_cast<const BYTE **>(ppBlob), pcbSize));
    return S_OK;
} // MDInternalRW::GetCustomAttributeAsBlob

//*****************************************************************************
// Helper function to lookup and retrieve a CustomAttribute.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetCustomAttributeByName( // S_OK or error.
    mdToken     tkObj,                  // [IN] Object with Custom Attribute.
    LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
    __deref_out_bcount(*pcbData) const void  **ppData, // [OUT] Put pointer to data here.
    __out ULONG *pcbData)               // [OUT] Put size of data here.
{
    HRESULT hr = S_OK;
    LOCKREADIFFAILRET();
    return m_pStgdb->m_MiniMd.CommonGetCustomAttributeByName(tkObj, szName, ppData, pcbData);
} // MDInternalRW::GetCustomAttributeByName

//*****************************************************************************
// return the name of a custom attribute
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetNameOfCustomAttribute( // S_OK or error.
    mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
    LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
    LPCUTF8          *pszName)          // [OUT] Name of Custom Attribute.
{
    HRESULT hr = S_OK;
    LOCKREADIFFAILRET();
    hr =  m_pStgdb->m_MiniMd.CommonGetNameOfCustomAttribute(RidFromToken(mdAttribute), pszNamespace, pszName);
    return (hr == S_FALSE) ? E_FAIL : hr;
} // MDInternalRW::GetNameOfCustomAttribute

//*****************************************************************************
// return scope properties
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetScopeProps(
    LPCSTR      *pszName,               // [OUT] scope name
    GUID        *pmvid)                 // [OUT] version id
{
    HRESULT hr = S_OK;
    LOCKREAD();

    ModuleRec *pModuleRec;

    // there is only one module record
    IfFailGo(m_pStgdb->m_MiniMd.GetModuleRecord(1, &pModuleRec));

    if (pmvid != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getMvidOfModule(pModuleRec, pmvid));
    }

    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfModule(pModuleRec, pszName));
    }

ErrExit:
    return hr;
} // MDInternalRW::GetScopeProps

//*****************************************************************************
// This function gets the "built for" version of a metadata scope.
//  NOTE: if the scope has never been saved, it will not have a built-for
//  version, and an empty string will be returned.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetVersionString(    // S_OK or error.
    LPCSTR      *pVer)                 // [OUT] Put version string here.
{
    HRESULT         hr = NOERROR;

    if (m_pStgdb->m_pvMd != NULL)
    {
        // For convenience, get a pointer to the version string.
        // @todo: get from alternate locations when there is no STOREAGESIGNATURE.
        *pVer = reinterpret_cast<const char*>(reinterpret_cast<const STORAGESIGNATURE*>(m_pStgdb->m_pvMd)->pVersion);
    }
    else
    {   // No string.
        *pVer = NULL;
    }

    return hr;
} // MDInternalRW::GetVersionString

//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::FindMethodDef(// S_OK or error.
    mdTypeDef   classdef,               // The owning class of the member.
    LPCSTR      szName,                 // Name of the member in utf8.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMethodDef *pmethoddef)            // Put MemberDef token here.
{
    HRESULT hr = S_OK;
    LOCKREAD();

    _ASSERTE(szName && pmethoddef);

    IfFailGo(ImportHelper::FindMethod(&(m_pStgdb->m_MiniMd),
        classdef,
        szName,
        pvSigBlob,
        cbSigBlob,
        pmethoddef));

ErrExit:
    return hr;
}

//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::FindMethodDefUsingCompare(// S_OK or error.
    mdTypeDef   classdef,               // The owning class of the member.
    LPCSTR      szName,                 // Name of the member in utf8.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    PSIGCOMPARE pSignatureCompare,      // [IN] Routine to compare signatures
    void*       pSignatureArgs,         // [IN] Additional info to supply the compare function
    mdMethodDef *pmethoddef)            // Put MemberDef token here.
{
    HRESULT hr = S_OK;
    LOCKREAD();

    _ASSERTE(szName && pmethoddef);

    IfFailGo(ImportHelper::FindMethod(&(m_pStgdb->m_MiniMd),
                                    classdef,
                                    szName,
                                    pvSigBlob,
                                    cbSigBlob,
                                    pmethoddef,
                                    0,
                                    pSignatureCompare,
                                    pSignatureArgs));

ErrExit:
    return hr;
}

//*****************************************************************************
// Find a given param of a Method.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::FindParamOfMethod(// S_OK or error.
    mdMethodDef md,                     // [IN] The owning method of the param.
    ULONG       iSeq,                   // [IN] The sequence # of the param.
    mdParamDef  *pparamdef)             // [OUT] Put ParamDef token here.
{
    ParamRec    *pParamRec;
    RID         ridStart, ridEnd;
    HRESULT     hr = NOERROR;
    MethodRec *pMethodRec = NULL;

    LOCKREAD();

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && pparamdef);

    // get the methoddef record
    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));

    // figure out the start rid and end rid of the parameter list of this methoddef
    ridStart = m_pStgdb->m_MiniMd.getParamListOfMethod(pMethodRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndParamListOfMethod(RidFromToken(md), &ridEnd));

    // loop through each param
    //
    for (; ridStart < ridEnd; ridStart++)
    {
        RID paramRid;
        IfFailGo(m_pStgdb->m_MiniMd.GetParamRid(ridStart, &paramRid));
        IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(paramRid, &pParamRec));
        if (iSeq == m_pStgdb->m_MiniMd.getSequenceOfParam( pParamRec) )
        {
            // parameter has the sequence number matches what we are looking for
            *pparamdef = TokenFromRid(paramRid, mdtParamDef );
            goto ErrExit;
        }
    }
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:

    return hr;
} // MDInternalRW::FindParamOfMethod



//*****************************************************************************
// return a pointer which points to meta data's internal string
// return the the type name in utf8
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameOfTypeDef(     // return hresult
    mdTypeDef   classdef,           // given typedef
    LPCSTR*     pszname,            // pointer to an internal UTF8 string
    LPCSTR*     psznamespace)       // pointer to the namespace.
{
    // No need to lock this method.
    HRESULT hr;

    if (pszname != NULL)
    {
        *pszname = NULL;
    }
    if (psznamespace != NULL)
    {
        *psznamespace = NULL;
    }

    if (TypeFromToken(classdef) == mdtTypeDef)
    {
        TypeDefRec *pTypeDefRec;
        IfFailRet(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(classdef), &pTypeDefRec));

        if (pszname != NULL)
        {
            IfFailRet(m_pStgdb->m_MiniMd.getNameOfTypeDef(pTypeDefRec, pszname));
        }

        if (psznamespace != NULL)
        {
            IfFailRet(m_pStgdb->m_MiniMd.getNamespaceOfTypeDef(pTypeDefRec, psznamespace));
        }
        return S_OK;
    }

    _ASSERTE(!"Invalid argument(s) of GetNameOfTypeDef");
    return CLDB_E_INTERNALERROR;
} // MDInternalRW::GetNameOfTypeDef

//*****************************************************************************
// return pDual indicating if the given TypeDef is marked as a Dual interface
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetIsDualOfTypeDef(// return hresult
    mdTypeDef   classdef,               // given classdef
    ULONG       *pDual)                 // [OUT] return dual flag here.
{
    ULONG       iFace=0;                // Iface type.
    HRESULT     hr;                     // A result.

    // no need to lock at this level

    hr = GetIfaceTypeOfTypeDef(classdef, &iFace);
    if (hr == S_OK)
        *pDual = (iFace == ifDual);
    else
        *pDual = 1;

    return hr;
} // MDInternalRW::GetIsDualOfTypeDef

__checkReturn
HRESULT MDInternalRW::GetIfaceTypeOfTypeDef(
    mdTypeDef   classdef,               // [IN] given classdef.
    ULONG       *pIface)                // [OUT] 0=dual, 1=vtable, 2=dispinterface
{
    HRESULT     hr;                     // A result.
    const BYTE  *pVal;                  // The custom value.
    ULONG       cbVal;                  // Size of the custom value.
    ULONG       ItfType = DEFAULT_COM_INTERFACE_TYPE;    // Set the interface type to the default.

    // all of the public functions that it calls have proper locked

    // If the value is not present, the class is assumed dual.
    hr = GetCustomAttributeByName(classdef, INTEROP_INTERFACETYPE_TYPE, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        _ASSERTE("The ComInterfaceType custom attribute is invalid" && cbVal);
            _ASSERTE("ComInterfaceType custom attribute does not have the right format" && (*pVal == 0x01) && (*(pVal + 1) == 0x00));
        ItfType = *(pVal + 2);
        if (ItfType >= ifLast)
            ItfType = DEFAULT_COM_INTERFACE_TYPE;
    }

    // Set the return value.
    *pIface = ItfType;

    return hr;
} // MDInternalRW::GetIfaceTypeOfTypeDef

//*****************************************************************************
// Given a methoddef, return a pointer to methoddef's name
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameOfMethodDef(
    mdMethodDef md,
    LPCSTR     *pszMethodName)
{
    // name of method will not change. So no need to lock
    HRESULT      hr;
    MethodRec *pMethodRec;
    *pszMethodName = NULL;
    IfFailRet(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));
    IfFailRet(m_pStgdb->m_MiniMd.getNameOfMethod(pMethodRec, pszMethodName));
    return S_OK;
} // MDInternalRW::GetNameOfMethodDef


//*****************************************************************************
// Given a methoddef, return a pointer to methoddef's signature and methoddef's name
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameAndSigOfMethodDef(
    mdMethodDef      methoddef,         // [IN] given memberdef
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of COM+ signature
    ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
    LPCSTR          *pszMethodName)
{
    HRESULT hr;
    // we don't need lock here because name and signature will not change

    // Output parameter should not be NULL
    _ASSERTE(ppvSigBlob && pcbSigBlob);
    _ASSERTE(TypeFromToken(methoddef) == mdtMethodDef);

    MethodRec *pMethodRec;
    *pszMethodName = NULL;
    *ppvSigBlob = NULL;
    *ppvSigBlob = NULL;
    IfFailRet(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(methoddef), &pMethodRec));
    IfFailRet(m_pStgdb->m_MiniMd.getSignatureOfMethod(pMethodRec, ppvSigBlob, pcbSigBlob));

    return GetNameOfMethodDef(methoddef, pszMethodName);
} // MDInternalRW::GetNameAndSigOfMethodDef


//*****************************************************************************
// Given a FieldDef, return a pointer to FieldDef's name in UTF8
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameOfFieldDef(    // return hresult
    mdFieldDef fd,                  // given field
    LPCSTR    *pszFieldName)
{
    // we don't need lock here because name of field will not change
    HRESULT hr;

    FieldRec *pFieldRec;
    *pszFieldName = NULL;
    IfFailRet(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(fd), &pFieldRec));
    IfFailRet(m_pStgdb->m_MiniMd.getNameOfField(pFieldRec, pszFieldName));
    return S_OK;
} // MDInternalRW::GetNameOfFieldDef


//*****************************************************************************
// Given a classdef, return a pointer to classdef's name in UTF8
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameOfTypeRef(     // return TypeDef's name
    mdTypeRef   classref,           // [IN] given typeref
    LPCSTR      *psznamespace,      // [OUT] return typeref name
    LPCSTR      *pszname)           // [OUT] return typeref namespace
{
    _ASSERTE(TypeFromToken(classref) == mdtTypeRef);
    HRESULT hr;

    *psznamespace = NULL;
    *pszname = NULL;

    // we don't need lock here because name of a typeref will not change

    TypeRefRec *pTypeRefRec;
    IfFailRet(m_pStgdb->m_MiniMd.GetTypeRefRecord(RidFromToken(classref), &pTypeRefRec));
    IfFailRet(m_pStgdb->m_MiniMd.getNamespaceOfTypeRef(pTypeRefRec, psznamespace));
    IfFailRet(m_pStgdb->m_MiniMd.getNameOfTypeRef(pTypeRefRec, pszname));
    return S_OK;
} // MDInternalRW::GetNameOfTypeRef

//*****************************************************************************
// return the resolutionscope of typeref
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetResolutionScopeOfTypeRef(
    mdTypeRef classref,                 // given classref
    mdToken  *ptkResolutionScope)
{
    HRESULT hr = S_OK;
    TypeRefRec *pTypeRefRec = NULL;

    LOCKREAD();

    _ASSERTE(TypeFromToken(classref) == mdtTypeRef && RidFromToken(classref));

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeRefRecord(RidFromToken(classref), &pTypeRefRec));
    _ASSERTE(hr == S_OK);
    *ptkResolutionScope = m_pStgdb->m_MiniMd.getResolutionScopeOfTypeRef(pTypeRefRec);
    return S_OK;

ErrExit:
    *ptkResolutionScope = mdTokenNil;
    return hr;
} // MDInternalRW::GetResolutionScopeOfTypeRef

//*****************************************************************************
// Given a name, find the corresponding TypeRef.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::FindTypeRefByName(  // S_OK or error.
    LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
    LPCSTR      szName,                 // [IN] Name of the TypeRef.
    mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
    mdTypeRef   *ptk)                   // [OUT] TypeRef token returned.
{
    HRESULT     hr = NOERROR;
    ULONG       cTypeRefRecs;
    TypeRefRec *pTypeRefRec;
    LPCUTF8     szNamespaceTmp;
    LPCUTF8     szNameTmp;
    mdToken     tkRes;

    LOCKREAD();
    _ASSERTE(ptk);

    // initialize the output parameter
    *ptk = mdTypeRefNil;

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    // It is a linear search here. Do we want to instantiate the name hash?
    cTypeRefRecs = m_pStgdb->m_MiniMd.getCountTypeRefs();

    for (ULONG i = 1; i <= cTypeRefRecs; i++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetTypeRefRecord(i, &pTypeRefRec));

        tkRes = m_pStgdb->m_MiniMd.getResolutionScopeOfTypeRef(pTypeRefRec);
        if (IsNilToken(tkRes))
        {
            if (!IsNilToken(tkResolutionScope))
                continue;
        }
        else if (tkRes != tkResolutionScope)
            continue;

        IfFailGo(m_pStgdb->m_MiniMd.getNamespaceOfTypeRef(pTypeRefRec, &szNamespaceTmp));
        if (strcmp(szNamespace, szNamespaceTmp))
            continue;

        IfFailGo(m_pStgdb->m_MiniMd.getNameOfTypeRef(pTypeRefRec, &szNameTmp));
        if (!strcmp(szNameTmp, szName))
        {
            *ptk = TokenFromRid(i, mdtTypeRef);
            goto ErrExit;
        }
    }

    // cannot find the typedef
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // MDInternalRW::FindTypeRefByName

//*****************************************************************************
// return flags for a given class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetTypeDefProps(
    mdTypeDef   td,                     // given classdef
    DWORD       *pdwAttr,               // return flags on class
    mdToken     *ptkExtends)            // [OUT] Put base class TypeDef/TypeRef here.
{
    HRESULT hr = S_OK;
    TypeDefRec *pTypeDefRec = NULL;
    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    if (ptkExtends)
    {
        *ptkExtends = m_pStgdb->m_MiniMd.getExtendsOfTypeDef(pTypeDefRec);
    }
    if (pdwAttr)
    {
        *pdwAttr = m_pStgdb->m_MiniMd.getFlagsOfTypeDef(pTypeDefRec);
    }

ErrExit:

    return hr;
} // MDInternalRW::GetTypeDefProps


//*****************************************************************************
// return guid pointer to MetaData internal guid pool given a given class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetItemGuid(      // return hresult
    mdToken     tkObj,                  // given item.
    CLSID       *pGuid)                 // [OUT] put guid here.
{

    HRESULT     hr;                     // A result.
    const BYTE  *pBlob = NULL;          // Blob with dispid.
    ULONG       cbBlob;                 // Length of blob.
    WCHAR       wzBlob[40];             // Wide char format of guid.
    int         ix;                     // Loop control.

    // Get the GUID, if any.
    hr = GetCustomAttributeByName(tkObj, INTEROP_GUID_TYPE, (const void**)&pBlob, &cbBlob);
    if (SUCCEEDED(hr) && hr != S_FALSE)
    {
        // Should be in format.  Total length == 41
        // <0x0001><0x24>01234567-0123-0123-0123-001122334455<0x0000>
        if ((cbBlob != 41) || (GET_UNALIGNED_VAL16(pBlob) != 1))
            IfFailGo(E_INVALIDARG);
        for (ix=1; ix<=36; ++ix)
            wzBlob[ix] = pBlob[ix+2];
        wzBlob[0] = '{';
        wzBlob[37] = '}';
        wzBlob[38] = 0;
        hr = IIDFromString(wzBlob, pGuid);
    }
    else
        *pGuid = GUID_NULL;

ErrExit:
    return hr;
} // MDInternalRW::GetItemGuid

//*****************************************************************************
// // get enclosing class of NestedClass
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNestedClassProps(
    mdTypeDef  tkNestedClass,       // [IN] NestedClass token.
    mdTypeDef *ptkEnclosingClass)   // [OUT] EnclosingClass token.
{
    HRESULT hr = NOERROR;
    RID     rid;

    LOCKREAD();

    if (!m_pStgdb->m_MiniMd.IsSorted(TBL_NestedClass) && !m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_NestedClass))
    {
        // NestedClass table is not sorted.
        CONVERT_READ_TO_WRITE_LOCK();
    }

    // This is a binary search thus we need to grap a read lock. Or this table
    // might be sorted underneath our feet.

    _ASSERTE(TypeFromToken(tkNestedClass) == mdtTypeDef && ptkEnclosingClass);

    IfFailGo(m_pStgdb->m_MiniMd.FindNestedClassFor(RidFromToken(tkNestedClass), &rid));

    if (InvalidRid(rid))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        NestedClassRec *pRecord;
        IfFailGo(m_pStgdb->m_MiniMd.GetNestedClassRecord(rid, &pRecord));
        *ptkEnclosingClass = m_pStgdb->m_MiniMd.getEnclosingClassOfNestedClass(pRecord);
    }

ErrExit:
    return hr;
} // MDInternalRW::GetNestedClassProps

//*******************************************************************************
// Get count of Nested classes given the enclosing class.
//*******************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetCountNestedClasses(  // return count of Nested classes.
    mdTypeDef   tkEnclosingClass,       // [IN]Enclosing class.
    ULONG      *pcNestedClassesCount)
{
    HRESULT hr;
    ULONG       ulCount;
    ULONG       ulRetCount = 0;
    NestedClassRec *pRecord;

    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef && !IsNilToken(tkEnclosingClass));

    *pcNestedClassesCount = 0;

    ulCount = m_pStgdb->m_MiniMd.getCountNestedClasss();

    for (ULONG i = 1; i <= ulCount; i++)
    {
        IfFailRet(m_pStgdb->m_MiniMd.GetNestedClassRecord(i, &pRecord));
        if (tkEnclosingClass == m_pStgdb->m_MiniMd.getEnclosingClassOfNestedClass(pRecord))
            ulRetCount++;
    }
    *pcNestedClassesCount = ulRetCount;
    return S_OK;
} // MDInternalRW::GetCountNestedClasses

//*******************************************************************************
// Return array of Nested classes given the enclosing class.
//*******************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNestedClasses(   // Return actual count.
    mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
    mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
    ULONG       ulNestedClasses,        // [IN] Size of array.
    ULONG      *pcNestedClasses)
{
    HRESULT hr;
    ULONG       ulCount;
    ULONG       ulRetCount = 0;
    NestedClassRec *pRecord;

    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef &&
             !IsNilToken(tkEnclosingClass));

    *pcNestedClasses = 0;

    ulCount = m_pStgdb->m_MiniMd.getCountNestedClasss();

    for (ULONG i = 1; i <= ulCount; i++)
    {
        IfFailRet(m_pStgdb->m_MiniMd.GetNestedClassRecord(i, &pRecord));
        if (tkEnclosingClass == m_pStgdb->m_MiniMd.getEnclosingClassOfNestedClass(pRecord))
        {
            if (ovadd_le(ulRetCount, 1, ulNestedClasses))  // ulRetCount is 0 based.
                rNestedClasses[ulRetCount] = m_pStgdb->m_MiniMd.getNestedClassOfNestedClass(pRecord);
            ulRetCount++;
        }
    }
    *pcNestedClasses = ulRetCount;
    return S_OK;
} // MDInternalRW::GetNestedClasses

//*******************************************************************************
// return the ModuleRef properties
//*******************************************************************************
__checkReturn
HRESULT MDInternalRW::GetModuleRefProps(   // return hresult
    mdModuleRef mur,                // [IN] moduleref token
    LPCSTR      *pszName)           // [OUT] buffer to fill with the moduleref name
{
    _ASSERTE(TypeFromToken(mur) == mdtModuleRef);
    _ASSERTE(pszName);

    HRESULT hr = S_OK;
    ModuleRefRec *pModuleRefRec = NULL;
    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.GetModuleRefRecord(RidFromToken(mur), &pModuleRefRec));
    IfFailGo(m_pStgdb->m_MiniMd.getNameOfModuleRef(pModuleRefRec, pszName));

ErrExit:

    return hr;
} // MDInternalRW::GetModuleRefProps



//*****************************************************************************
// Given a scope and a methoddef, return a pointer to methoddef's signature
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetSigOfMethodDef(
    mdMethodDef      methoddef,     // given a methoddef
    ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
    PCCOR_SIGNATURE *ppSig)
{
    // Output parameter should not be NULL
    _ASSERTE(pcbSigBlob);
    _ASSERTE(TypeFromToken(methoddef) == mdtMethodDef);

    HRESULT hr;
    // We don't change MethodDef signature. No need to lock.

    MethodRec *pMethodRec;
    *ppSig = NULL;
    *pcbSigBlob = 0;
    IfFailRet(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(methoddef), &pMethodRec));
    IfFailRet(m_pStgdb->m_MiniMd.getSignatureOfMethod(pMethodRec, ppSig, pcbSigBlob));
    return S_OK;
} // MDInternalRW::GetSigOfMethodDef


//*****************************************************************************
// Given a scope and a fielddef, return a pointer to fielddef's signature
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetSigOfFieldDef(
    mdFieldDef       fielddef,      // given a methoddef
    ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
    PCCOR_SIGNATURE *ppSig)
{
    _ASSERTE(pcbSigBlob);
    _ASSERTE(TypeFromToken(fielddef) == mdtFieldDef);

    HRESULT hr;
    // We don't change Field's signature. No need to lock.

    FieldRec *pFieldRec;
    *ppSig = NULL;
    *pcbSigBlob = 0;
    IfFailRet(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(fielddef), &pFieldRec));
    IfFailRet(m_pStgdb->m_MiniMd.getSignatureOfField(pFieldRec, ppSig, pcbSigBlob));
    return S_OK;
} // MDInternalRW::GetSigOfFieldDef


//*****************************************************************************
// Get signature for the token (FieldDef, MethodDef, Signature, or TypeSpec).
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetSigFromToken(
    mdToken           tk,
    ULONG *           pcbSig,
    PCCOR_SIGNATURE * ppSig)
{
    HRESULT hr;
    // We don't change token's signature. Thus no need to lock.

    *ppSig = NULL;
    *pcbSig = 0;
    switch (TypeFromToken(tk))
    {
    case mdtSignature:
        {
            StandAloneSigRec *pRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetStandAloneSigRecord(RidFromToken(tk), &pRec));
            IfFailGo(m_pStgdb->m_MiniMd.getSignatureOfStandAloneSig(pRec, ppSig, pcbSig));
            return S_OK;
        }
    case mdtTypeSpec:
        {
            TypeSpecRec *pRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetTypeSpecRecord(RidFromToken(tk), &pRec));
            IfFailGo(m_pStgdb->m_MiniMd.getSignatureOfTypeSpec(pRec, ppSig, pcbSig));
            return S_OK;
        }
    case mdtMethodDef:
        {
            IfFailGo(GetSigOfMethodDef(tk, pcbSig, ppSig));
            return S_OK;
        }
    case mdtFieldDef:
        {
            IfFailGo(GetSigOfFieldDef(tk, pcbSig, ppSig));
            return S_OK;
        }
    }

    // not a known token type.
#ifdef _DEBUG
        if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AssertOnBadImageFormat, 1))
            _ASSERTE(!"Unexpected token type");
#endif
    *pcbSig = 0;
    hr = META_E_INVALID_TOKEN_TYPE;

ErrExit:
    return hr;
} // MDInternalRW::GetSigFromToken


//*****************************************************************************
// Given methoddef, return the flags
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetMethodDefProps(
    mdMethodDef md,
    DWORD      *pdwFlags)   // return mdPublic, mdAbstract, etc
{
    HRESULT hr = S_OK;
    MethodRec *pMethodRec = NULL;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));
    _ASSERTE(hr == S_OK);
    *pdwFlags = m_pStgdb->m_MiniMd.getFlagsOfMethod(pMethodRec);
    return S_OK;

ErrExit:
    *pdwFlags = (DWORD)-1;
    return hr;
} // MDInternalRW::GetMethodDefProps

//*****************************************************************************
// Given a scope and a methoddef, return RVA and impl flags
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetMethodImplProps(
    mdToken     tk,                     // [IN] MethodDef
    ULONG       *pulCodeRVA,            // [OUT] CodeRVA
    DWORD       *pdwImplFlags)          // [OUT] Impl. Flags
{
    _ASSERTE(TypeFromToken(tk) == mdtMethodDef);
    HRESULT hr = S_OK;
    MethodRec *pMethodRec = NULL;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMethodRec));

    if (pulCodeRVA)
    {
        *pulCodeRVA = m_pStgdb->m_MiniMd.getRVAOfMethod(pMethodRec);
    }

    if (pdwImplFlags)
    {
        *pdwImplFlags = m_pStgdb->m_MiniMd.getImplFlagsOfMethod(pMethodRec);
    }

ErrExit:

    return hr;
} // MDInternalRW::GetMethodImplProps


//*****************************************************************************
// return the field RVA
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetFieldRVA(
    mdToken     fd,                     // [IN] FieldDef
    ULONG       *pulCodeRVA)            // [OUT] CodeRVA
{
    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);
    _ASSERTE(pulCodeRVA);
    uint32_t       iRecord;
    HRESULT     hr = NOERROR;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.FindFieldRVAHelper(fd, &iRecord));
    if (InvalidRid(iRecord))
    {
        if (pulCodeRVA)
            *pulCodeRVA = 0;
        hr = CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        FieldRVARec *pFieldRVARec;
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRVARecord(iRecord, &pFieldRVARec));

        *pulCodeRVA = m_pStgdb->m_MiniMd.getRVAOfFieldRVA(pFieldRVARec);
    }

ErrExit:
    return hr;
} // MDInternalRW::GetFieldRVA


//*****************************************************************************
// Given a fielddef, return the flags. Such as fdPublic, fdStatic, etc
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetFieldDefProps(
    mdFieldDef fd,          // given memberdef
    DWORD     *pdwFlags)    // [OUT] return fdPublic, fdPrive, etc flags
{
    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);
    HRESULT hr = S_OK;
    FieldRec *pFieldRec = NULL;

    LOCKREAD();

    IfFailRet(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(fd), &pFieldRec));
    _ASSERTE(hr == S_OK);
    *pdwFlags = m_pStgdb->m_MiniMd.getFlagsOfField(pFieldRec);
    return S_OK;

ErrExit:
    *pdwFlags = (DWORD)-1;
    return hr;
} // MDInternalRW::GetFieldDefProps

//*****************************************************************************
// return default value of a token(could be paramdef, fielddef, or property)
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetDefaultValue(   // return hresult
    mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
    MDDefaultValue  *pMDDefaultValue)   // [OUT] default value
{
    _ASSERTE(pMDDefaultValue);

    HRESULT     hr;
    BYTE        bType;
    const       VOID *pValue;
    ULONG       cbValue;
    RID         rid;
    ConstantRec *pConstantRec;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.FindConstantHelper(tk, &rid));
    if (InvalidRid(rid))
    {
        pMDDefaultValue->m_bType = ELEMENT_TYPE_VOID;
        return S_OK;
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));

    // get the type of constant value
    bType = m_pStgdb->m_MiniMd.getTypeOfConstant(pConstantRec);

    // get the value blob
    IfFailGo(m_pStgdb->m_MiniMd.getValueOfConstant(pConstantRec, reinterpret_cast<const BYTE **>(&pValue), &cbValue));

    // convert it to our internal default value representation
    hr = _FillMDDefaultValue(bType, pValue, cbValue, pMDDefaultValue);

ErrExit:

    return hr;
} // MDInternalRW::GetDefaultValue


//*****************************************************************************
// Given a scope and a methoddef/fielddef, return the dispid
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetDispIdOfMemberDef(     // return hresult
    mdToken     tk,                     // given methoddef or fielddef
    ULONG       *pDispid)               // Put the dispid here.
{
#ifdef FEATURE_COMINTEROP
    HRESULT     hr;                     // A result.
    const BYTE  *pBlob;                 // Blob with dispid.
    ULONG       cbBlob;                 // Length of blob.

    // No need to lock this function. All of the function that it is calling is already locked!

    // Get the DISPID, if any.
    _ASSERTE(pDispid);

    *pDispid = DISPID_UNKNOWN;
    hr = GetCustomAttributeByName(tk, INTEROP_DISPID_TYPE, (const void**)&pBlob, &cbBlob);
    if (hr != S_FALSE)
    {
        // Check that this might be a dispid.
        if (cbBlob >= (sizeof(*pDispid)+2))
            *pDispid = GET_UNALIGNED_VAL32(pBlob+2);
        else
            IfFailGo(E_INVALIDARG);
    }

ErrExit:
    return hr;
#else // FEATURE_COMINTEROP
    _ASSERTE(false);
    return E_NOTIMPL;
#endif // FEATURE_COMINTEROP
} // MDInternalRW::GetDispIdOfMemberDef


//*****************************************************************************
// Given interfaceimpl, return the TypeRef/TypeDef and flags
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetTypeOfInterfaceImpl( // return hresult
    mdInterfaceImpl iiImpl,             // given a interfaceimpl
    mdToken        *ptkType)
{
    HRESULT hr;
    // no need to lock this function.

    _ASSERTE(TypeFromToken(iiImpl) == mdtInterfaceImpl);

    *ptkType = mdTypeDefNil;

    InterfaceImplRec *pIIRec;
    IfFailRet(m_pStgdb->m_MiniMd.GetInterfaceImplRecord(RidFromToken(iiImpl), &pIIRec));
    *ptkType = m_pStgdb->m_MiniMd.getInterfaceOfInterfaceImpl(pIIRec);
    return S_OK;
} // MDInternalRW::GetTypeOfInterfaceImpl

//*****************************************************************************
// This routine gets the properties for the given MethodSpec token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetMethodSpecProps(         // S_OK or error.
        mdMethodSpec mi,           // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
{
    HRESULT         hr = NOERROR;
    MethodSpecRec  *pMethodSpecRec;

    _ASSERTE(TypeFromToken(mi) == mdtMethodSpec);

    IfFailGo(m_pStgdb->m_MiniMd.GetMethodSpecRecord(RidFromToken(mi), &pMethodSpecRec));

    if (tkParent)
        *tkParent = m_pStgdb->m_MiniMd.getMethodOfMethodSpec(pMethodSpecRec);

    if (ppvSigBlob || pcbSigBlob)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailGo(m_pStgdb->m_MiniMd.getInstantiationOfMethodSpec(pMethodSpecRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pcbSigBlob)
            *pcbSigBlob = cbSig;
    }

ErrExit:
    return hr;
} // MDInternalRW::GetMethodSpecProps

//*****************************************************************************
// Given a classname, return the typedef
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::FindTypeDef(      // return hresult
    LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
    LPCSTR      szName,                 // [IN] Name of the TypeDef.
    mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef of enclosing class.
    mdTypeDef   *ptypedef)              // [OUT] return typedef
{
    HRESULT hr = S_OK;
    LOCKREADIFFAILRET();

    _ASSERTE(ptypedef);

    // initialize the output parameter
    *ptypedef = mdTypeDefNil;

    return ImportHelper::FindTypeDefByName(&(m_pStgdb->m_MiniMd),
                                        szNamespace,
                                        szName,
                                        tkEnclosingClass,
                                        ptypedef);
} // MDInternalRW::FindTypeDef

//*****************************************************************************
// Given a memberref, return a pointer to memberref's name and signature
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetNameAndSigOfMemberRef( // meberref's name
    mdMemberRef      memberref,         // given a memberref
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of COM+ signature
    ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
    LPCSTR          *pszMemberRefName)
{
    HRESULT hr;

    // MemberRef's name and sig won't change. Don't need to lock this.

    _ASSERTE(TypeFromToken(memberref) == mdtMemberRef);

    MemberRefRec *pMemberRefRec;
    *pszMemberRefName = NULL;
    if (ppvSigBlob != NULL)
    {
        _ASSERTE(pcbSigBlob != NULL);
        *ppvSigBlob = NULL;
        *pcbSigBlob = 0;
    }
    IfFailRet(m_pStgdb->m_MiniMd.GetMemberRefRecord(RidFromToken(memberref), &pMemberRefRec));
    if (ppvSigBlob != NULL)
    {
        IfFailRet(m_pStgdb->m_MiniMd.getSignatureOfMemberRef(pMemberRefRec, ppvSigBlob, pcbSigBlob));
    }
    IfFailRet(m_pStgdb->m_MiniMd.getNameOfMemberRef(pMemberRefRec, pszMemberRefName));
    return S_OK;
} // MDInternalRW::GetNameAndSigOfMemberRef



//*****************************************************************************
// Given a memberref, return parent token. It can be a TypeRef, ModuleRef, or a MethodDef
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetParentOfMemberRef(   // return parent token
    mdMemberRef memberref,      // given a typedef
    mdToken    *ptkParent)      // return the parent token
{
    HRESULT hr = S_OK;
    MemberRefRec *pMemberRefRec = NULL;

    LOCKREAD();

    // parent for MemberRef can change. See SetParent.

    _ASSERTE(TypeFromToken(memberref) == mdtMemberRef);

    IfFailRet(m_pStgdb->m_MiniMd.GetMemberRefRecord(RidFromToken(memberref), &pMemberRefRec));
    _ASSERTE(hr == S_OK);
    *ptkParent = m_pStgdb->m_MiniMd.getClassOfMemberRef(pMemberRefRec);
    return S_OK;

ErrExit:
    *ptkParent = mdTokenNil;
    return hr;
} // MDInternalRW::GetParentOfMemberRef

//*****************************************************************************
// return properties of a paramdef
//*****************************************************************************/
__checkReturn
HRESULT
MDInternalRW::GetParamDefProps (
    mdParamDef paramdef,            // given a paramdef
    USHORT    *pusSequence,         // [OUT] slot number for this parameter
    DWORD     *pdwAttr,             // [OUT] flags
    LPCSTR    *pszName)             // [OUT] return the name of the parameter
{
    HRESULT hr = S_OK;
    ParamRec *pParamRec = NULL;

    LOCKREAD();

    // parent for MemberRef can change. See SetParamProps.

    _ASSERTE(TypeFromToken(paramdef) == mdtParamDef);
    IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(RidFromToken(paramdef), &pParamRec));
    _ASSERTE(hr == S_OK);
    if (pdwAttr != NULL)
    {
        *pdwAttr = m_pStgdb->m_MiniMd.getFlagsOfParam(pParamRec);
    }
    if (pusSequence != NULL)
    {
        *pusSequence = m_pStgdb->m_MiniMd.getSequenceOfParam(pParamRec);
    }
    IfFailGo(m_pStgdb->m_MiniMd.getNameOfParam(pParamRec, pszName));
    _ASSERTE(hr == S_OK);
    return S_OK;

ErrExit:
    *pszName = NULL;
    return S_OK;
} // MDInternalRW::GetParamDefProps

//*****************************************************************************
// Get property info for the method.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetPropertyInfoForMethodDef(  // Result.
    mdMethodDef md,                     // [IN] memberdef
    mdProperty  *ppd,                   // [OUT] put property token here
    LPCSTR      *pName,                 // [OUT] put pointer to name here
    ULONG       *pSemantic)             // [OUT] put semantic here
{
    MethodSemanticsRec *pSemantics;
    RID         ridCur;
    RID         ridMax;
    USHORT      usSemantics;
    HRESULT     hr = S_OK;
    LOCKREAD();

    ridMax = m_pStgdb->m_MiniMd.getCountMethodSemantics();
    for (ridCur = 1; ridCur <= ridMax; ridCur++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
        if (md == m_pStgdb->m_MiniMd.getMethodOfMethodSemantics(pSemantics))
        {
            // match the method
            usSemantics = m_pStgdb->m_MiniMd.getSemanticOfMethodSemantics(pSemantics);
            if (usSemantics == msGetter || usSemantics == msSetter)
            {
                // Make sure that it is not an invalid entry
                if (m_pStgdb->m_MiniMd.getAssociationOfMethodSemantics(pSemantics) != mdPropertyNil)
                {
                    // found a match. Fill out the output parameters
                    PropertyRec     *pProperty;
                    mdProperty      prop;
                    prop = m_pStgdb->m_MiniMd.getAssociationOfMethodSemantics(pSemantics);

                    if (ppd)
                        *ppd = prop;
                    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(RidFromToken(prop), &pProperty));

                    if (pName)
                    {
                        IfFailGo(m_pStgdb->m_MiniMd.getNameOfProperty(pProperty, pName));
                    }

                    if (pSemantic)
                        *pSemantic =  usSemantics;
                    goto ErrExit;
                }
            }
        }
    }

    hr = S_FALSE;
ErrExit:
    return hr;
} // MDInternalRW::GetPropertyInfoForMethodDef

//*****************************************************************************
// return the pack size of a class
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::GetClassPackSize(
    mdTypeDef   td,                     // [IN] give typedef
    DWORD       *pdwPackSize)           // [OUT]
{
    HRESULT     hr = NOERROR;
    RID         ridClassLayout = 0;

    LOCKREAD();

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pdwPackSize);

    ClassLayoutRec *pRec;
    IfFailGo(m_pStgdb->m_MiniMd.FindClassLayoutHelper(td, &ridClassLayout));

    if (InvalidRid(ridClassLayout))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
    *pdwPackSize = m_pStgdb->m_MiniMd.getPackingSizeOfClassLayout(pRec);
ErrExit:
    return hr;
} // MDInternalRW::GetClassPackSize


//*****************************************************************************
// return the total size of a value class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetClassTotalSize( // return error if a class does not have total size info
    mdTypeDef   td,                     // [IN] give typedef
    ULONG       *pulClassSize)          // [OUT] return the total size of the class
{
    CONTRACT_VIOLATION(ThrowsViolation);

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pulClassSize);

    ClassLayoutRec *pRec;
    HRESULT     hr = NOERROR;
    RID         ridClassLayout;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.FindClassLayoutHelper(td, &ridClassLayout));
    if (InvalidRid(ridClassLayout))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
    *pulClassSize = m_pStgdb->m_MiniMd.getClassSizeOfClassLayout(pRec);
ErrExit:
    return hr;
} // MDInternalRW::GetClassTotalSize


//*****************************************************************************
// init the layout enumerator of a class
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::GetClassLayoutInit(
    mdTypeDef   td,                     // [IN] give typedef
    MD_CLASS_LAYOUT *pmdLayout)         // [OUT] set up the status of query here
{
    HRESULT     hr = NOERROR;
    LOCKREAD();
    _ASSERTE(TypeFromToken(td) == mdtTypeDef);

    // <TODO>Do we need to lock this function? Can clints add more Fields on a TypeDef?</TODO>

    // initialize the output parameter
    _ASSERTE(pmdLayout);
    memset(pmdLayout, 0, sizeof(MD_CLASS_LAYOUT));

    TypeDefRec  *pTypeDefRec;

    // record for this typedef in TypeDef Table
    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    // find the starting and end field for this typedef
    pmdLayout->m_ridFieldCur = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(td), &(pmdLayout->m_ridFieldEnd)));

ErrExit:

    return hr;
} // MDInternalRW::GetClassLayoutInit

//*****************************************************************************
// Get the field offset for a given field token
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetFieldOffset(
    mdFieldDef  fd,                     // [IN] fielddef
    ULONG       *pulOffset)             // [OUT] FieldOffset
{
    HRESULT     hr = S_OK;
    FieldLayoutRec *pRec;

    _ASSERTE(pulOffset);

    RID iLayout;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.FindFieldLayoutHelper(fd, &iLayout));

    if (InvalidRid(iLayout))
    {
        hr = S_FALSE;
        goto ErrExit;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetFieldLayoutRecord(iLayout, &pRec));
    *pulOffset = m_pStgdb->m_MiniMd.getOffSetOfFieldLayout(pRec);
    _ASSERTE(*pulOffset != UINT32_MAX);

ErrExit:
    return hr;
} // MDInternalRW::GetFieldOffset

//*****************************************************************************
// enum the next the field layout
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetClassLayoutNext(
    MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
    mdFieldDef  *pfd,                   // [OUT] field def
    ULONG       *pulOffset)             // [OUT] field offset or sequence
{
    HRESULT     hr = S_OK;

    _ASSERTE(pfd && pulOffset && pLayout);

    RID         iLayout2;
    FieldLayoutRec *pRec;

    LOCKREAD();

    while (pLayout->m_ridFieldCur < pLayout->m_ridFieldEnd)
    {
        RID fieldRid;
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRid(pLayout->m_ridFieldCur, &fieldRid));
        mdFieldDef fd = TokenFromRid(fieldRid, mdtFieldDef);
        IfFailGo(m_pStgdb->m_MiniMd.FindFieldLayoutHelper(fd, &iLayout2));
        pLayout->m_ridFieldCur++;
        if (!InvalidRid(iLayout2))
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldLayoutRecord(iLayout2, &pRec));
            *pulOffset = m_pStgdb->m_MiniMd.getOffSetOfFieldLayout(pRec);
            _ASSERTE(*pulOffset != UINT32_MAX);
            *pfd = fd;
            goto ErrExit;
        }
    }

    *pfd = mdFieldDefNil;
    hr = S_FALSE;

    // fall through

ErrExit:
    return hr;
} // MDInternalRW::GetClassLayoutNext


//*****************************************************************************
// return the field's native type signature
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetFieldMarshal(  // return error if no native type associate with the token
    mdToken     tk,                     // [IN] given fielddef or paramdef
    PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
    ULONG       *pcbNativeType)         // [OUT] the count of bytes of *ppvNativeType
{
    // output parameters have to be supplied
    _ASSERTE(pcbNativeType);

    RID         rid;
    FieldMarshalRec *pFieldMarshalRec;
    HRESULT     hr = NOERROR;

    LOCKREAD();

    // find the row containing the marshal definition for tk
    IfFailGo(m_pStgdb->m_MiniMd.FindFieldMarshalHelper(tk, &rid));
    if (InvalidRid(rid))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetFieldMarshalRecord(rid, &pFieldMarshalRec));

    // get the native type
    IfFailGo(m_pStgdb->m_MiniMd.getNativeTypeOfFieldMarshal(pFieldMarshalRec, pSigNativeType, pcbNativeType));
ErrExit:
    return hr;
} // MDInternalRW::GetFieldMarshal



//*****************************************
// property APIs
//*****************************************

//*****************************************************************************
// Find property by name
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::FindProperty(
    mdTypeDef   td,                     // [IN] given a typdef
    LPCSTR      szPropName,             // [IN] property name
    mdProperty  *pProp)                 // [OUT] return property token
{
    HRESULT     hr = NOERROR;
    LOCKREAD();

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pProp);

    PropertyMapRec *pRec;
    PropertyRec *pProperty;
    RID         ridPropertyMap;
    RID         ridCur;
    RID         ridEnd;
    LPCUTF8     szName;

    IfFailGo(m_pStgdb->m_MiniMd.FindPropertyMapFor(RidFromToken(td), &ridPropertyMap));
    if (InvalidRid(ridPropertyMap))
    {
        // not found!
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyMapRecord(ridPropertyMap, &pRec));

    // get the starting/ending rid of properties of this typedef
    ridCur = m_pStgdb->m_MiniMd.getPropertyListOfPropertyMap(pRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));

    for ( ; ridCur < ridEnd; ridCur ++ )
    {
        RID propertyRid;
        IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRid(ridCur, &propertyRid));
        IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(propertyRid, &pProperty));
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfProperty(pProperty, &szName));
        if ( strcmp(szName, szPropName) ==0 )
        {
            // Found the match. Set the output parameter and we are done.
            *pProp = TokenFromRid(propertyRid, mdtProperty);
            goto ErrExit;
        }
    }

    // not found
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // MDInternalRW::FindProperty



//*****************************************************************************
// return the properties of a property
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::GetPropertyProps(
    mdProperty  prop,                   // [IN] property token
    LPCSTR      *pszProperty,           // [OUT] property name
    DWORD       *pdwPropFlags,          // [OUT] property flags.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
    ULONG       *pcbSig)                // [OUT] count of bytes in *ppvSig
{
    HRESULT hr = S_OK;
    LOCKREAD();

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(prop) == mdtProperty);

    PropertyRec     *pProperty;
    ULONG           cbSig;

    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(RidFromToken(prop), &pProperty));

    // get name of the property
    if (pszProperty)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfProperty(pProperty, pszProperty));
    }

    // get the flags of property
    if (pdwPropFlags)
        *pdwPropFlags = m_pStgdb->m_MiniMd.getPropFlagsOfProperty(pProperty);

    // get the type of the property
    if (ppvSig)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getTypeOfProperty(pProperty, ppvSig, &cbSig));
        if (pcbSig)
        {
            *pcbSig = cbSig;
        }
    }

ErrExit:
    return hr;
} // MDInternalRW::GetPropertyProps


//**********************************
//
// Event APIs
//
//**********************************

//*****************************************************************************
// return an event by given the name
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::FindEvent(
    mdTypeDef   td,                     // [IN] given a typdef
    LPCSTR      szEventName,            // [IN] event name
    mdEvent     *pEvent)                // [OUT] return event token
{
    HRESULT     hr = NOERROR;
    LOCKREAD();

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pEvent);

    EventMapRec *pRec;
    EventRec    *pEventRec;
    RID         ridEventMap;
    RID         ridCur;
    RID         ridEnd;
    LPCUTF8     szName;

    IfFailGo(m_pStgdb->m_MiniMd.FindEventMapFor(RidFromToken(td), &ridEventMap));
    if (InvalidRid(ridEventMap))
    {
        // not found!
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetEventMapRecord(ridEventMap, &pRec));

    // get the starting/ending rid of properties of this typedef
    ridCur = m_pStgdb->m_MiniMd.getEventListOfEventMap(pRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndEventListOfEventMap(ridEventMap, &ridEnd));

    for (; ridCur < ridEnd; ridCur ++)
    {
        RID eventRid;
        IfFailGo(m_pStgdb->m_MiniMd.GetEventRid(ridCur, &eventRid));
        IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(eventRid, &pEventRec));
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfEvent(pEventRec, &szName));
        if ( strcmp(szName, szEventName) ==0 )
        {
            // Found the match. Set the output parameter and we are done.
            *pEvent = TokenFromRid(eventRid, mdtEvent);
            goto ErrExit;
        }
    }

    // not found
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:

    return hr;
} // MDInternalRW::FindEvent


//*****************************************************************************
// return the properties of an event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetEventProps(           // S_OK, S_FALSE, or error.
    mdEvent     ev,                         // [IN] event token
    LPCSTR      *pszEvent,                  // [OUT] Event name
    DWORD       *pdwEventFlags,             // [OUT] Event flags.
    mdToken     *ptkEventType)          // [OUT] EventType class
{
    HRESULT hr = S_OK;
    LOCKREAD();
    EventRec    *pEvent;

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(ev) == mdtEvent);

    IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(RidFromToken(ev), &pEvent));
    if (pszEvent != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfEvent(pEvent, pszEvent));
    }
    if (pdwEventFlags)
        *pdwEventFlags = m_pStgdb->m_MiniMd.getEventFlagsOfEvent(pEvent);
    if (ptkEventType)
        *ptkEventType = m_pStgdb->m_MiniMd.getEventTypeOfEvent(pEvent);

ErrExit:

    return hr;
} // MDInternalRW::GetEventProps

//*****************************************************************************
// return the properties of a generic param
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetGenericParamProps(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD  *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName)                     // [OUT] The name
{
    HRESULT         hr = NOERROR;
    GenericParamRec  *pGenericParamRec = NULL;

    // See if this version of the metadata can do Generics
    if (!m_pStgdb->m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    _ASSERTE(TypeFromToken(rd) == mdtGenericParam);
    if (TypeFromToken(rd) != mdtGenericParam)
        IfFailGo(CLDB_E_FILE_CORRUPT);

    IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamRecord(RidFromToken(rd), &pGenericParamRec));

    if (pulSequence)
        *pulSequence = m_pStgdb->m_MiniMd.getNumberOfGenericParam(pGenericParamRec);
    if (pdwAttr)
        *pdwAttr = m_pStgdb->m_MiniMd.getFlagsOfGenericParam(pGenericParamRec);
    if (ptOwner)
        *ptOwner = m_pStgdb->m_MiniMd.getOwnerOfGenericParam(pGenericParamRec);
    if (szName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfGenericParam(pGenericParamRec, szName));
    }

ErrExit:
    return hr;
} // MDInternalRW::GetGenericParamProps


//*****************************************************************************
// This routine gets the properties for the given GenericParamConstraint token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetGenericParamConstraintProps(      // S_OK or error.
        mdGenericParamConstraint rd,        // [IN] The constraint token
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)    // [OUT] TypeDef/Ref/Spec constraint
{
    HRESULT         hr = NOERROR;
    GenericParamConstraintRec  *pGPCRec;
    RID             ridRD = RidFromToken(rd);

    // See if this version of the metadata can do Generics
    if (!m_pStgdb->m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    if((TypeFromToken(rd) == mdtGenericParamConstraint) && (ridRD != 0))
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamConstraintRecord(ridRD, &pGPCRec));

        if (ptGenericParam)
            *ptGenericParam = TokenFromRid(m_pStgdb->m_MiniMd.getOwnerOfGenericParamConstraint(pGPCRec),mdtGenericParam);
        if (ptkConstraintType)
            *ptkConstraintType = m_pStgdb->m_MiniMd.getConstraintOfGenericParamConstraint(pGPCRec);
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    return hr;
} // MDInternalRW::GetGenericParamConstraintProps

//*****************************************************************************
// Find methoddef of a particular associate with a property or an event
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRW::FindAssociate(
    mdToken     evprop,                 // [IN] given a property or event token
    DWORD       dwSemantics,            // [IN] given a associate semantics(setter, getter, testdefault, reset)
    mdMethodDef *pmd)                   // [OUT] return method def token
{
    HRESULT     hr = NOERROR;
    RID         rid;
    MethodSemanticsRec *pMethodSemantics;

    // output parameters have to be supplied
    _ASSERTE(pmd);
    _ASSERTE(TypeFromToken(evprop) == mdtEvent || TypeFromToken(evprop) == mdtProperty);

    LOCKREAD();

    hr = m_pStgdb->m_MiniMd.FindAssociateHelper(evprop, dwSemantics, &rid);
    if (SUCCEEDED(hr))
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodSemanticsRecord(rid, &pMethodSemantics));
        *pmd = m_pStgdb->m_MiniMd.getMethodOfMethodSemantics(pMethodSemantics);
    }

ErrExit:

    return hr;
} // MDInternalRW::FindAssociate


//*****************************************************************************
// get counts of methodsemantics associated with a particular property/event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumAssociateInit(
    mdToken     evprop,                 // [IN] given a property or an event token
    HENUMInternal *phEnum)              // [OUT] cursor to hold the query result
{
    HRESULT     hr;

    LOCKREAD();

    // output parameters have to be supplied
    _ASSERTE(phEnum);
    _ASSERTE(TypeFromToken(evprop) == mdtEvent || TypeFromToken(evprop) == mdtProperty);

    hr = m_pStgdb->m_MiniMd.FindMethodSemanticsHelper(evprop, phEnum);

ErrExit:
    return hr;
} // MDInternalRW::EnumAssociateInit


//*****************************************************************************
// get all methodsemantics associated with a particular property/event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetAllAssociates(
    HENUMInternal *phEnum,              // [OUT] cursor to hold the query result
    ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
    ULONG       cAssociateRec)          // [IN] size of the buffer
{
    HRESULT hr = S_OK;
    MethodSemanticsRec *pSemantics;
    RID         ridCur;
    int         index = 0;

    LOCKREAD();

    // <TODO>@FUTURE: rewrite the EnumAssociateInit and GetAllAssociates. Because we might add more properties and events.
    // Then we might resort MethodSemantics table. So this can be totally out of sync.</TODO>

    _ASSERTE(phEnum && pAssociateRec);
    _ASSERTE(cAssociateRec == phEnum->m_ulCount);

    // Convert from row pointers to RIDs.
    while (HENUMInternal::EnumNext(phEnum, (mdToken *)&ridCur))
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));

        pAssociateRec[index].m_memberdef = m_pStgdb->m_MiniMd.getMethodOfMethodSemantics(pSemantics);
        pAssociateRec[index].m_dwSemantics = m_pStgdb->m_MiniMd.getSemanticOfMethodSemantics(pSemantics);
        index++;
    }

ErrExit:

    return hr;
} // MDInternalRW::GetAllAssociates


//*****************************************************************************
// Get the Action and Permissions blob for a given PermissionSet.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetPermissionSetProps(
    mdPermission pm,                    // [IN] the permission token.
    DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
    void const  **ppvPermission,        // [OUT] permission blob.
    ULONG       *pcbPermission)         // [OUT] count of bytes of pvPermission.
{
    HRESULT hr = S_OK;
    _ASSERTE(TypeFromToken(pm) == mdtPermission);
    _ASSERTE(pdwAction && ppvPermission && pcbPermission);

    DeclSecurityRec *pPerm;
    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.GetDeclSecurityRecord(RidFromToken(pm), &pPerm));
    *pdwAction = m_pStgdb->m_MiniMd.getActionOfDeclSecurity(pPerm);
    IfFailGo(m_pStgdb->m_MiniMd.getPermissionSetOfDeclSecurity(pPerm, reinterpret_cast<const BYTE **>(ppvPermission), pcbPermission));

ErrExit:

    return hr;
} // MDInternalRW::GetPermissionSetProps


//*****************************************************************************
// Get the String given the String token.
// Return a pointer to the string, or NULL in case of error.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRW::GetUserString(    // Offset into the string blob heap.
    mdString stk,                       // [IN] the string token.
    ULONG   *pcchStringSize,            // [OUT] count of characters in the string.
    BOOL    *pfIs80Plus,                // [OUT] specifies where there are extended characters >= 0x80.
    LPCWSTR *pwszUserString)
{
    HRESULT hr;
    LPWSTR  wszTmp;

    // no need to lock this function.

    if (pfIs80Plus != NULL)
    {
        *pfIs80Plus = FALSE;
    }
    *pwszUserString = NULL;
    *pcchStringSize = 0;

    _ASSERTE(pcchStringSize != NULL);
    MetaData::DataBlob userString;
    IfFailRet(m_pStgdb->m_MiniMd.GetUserString(RidFromToken(stk), &userString));

    wszTmp = reinterpret_cast<LPWSTR>(userString.GetDataPointer());

    *pcchStringSize = userString.GetSize() / sizeof(WCHAR);

    if (userString.IsEmpty())
    {
        *pwszUserString = NULL;
        return S_OK;
    }

    if (pfIs80Plus != NULL)
    {
        if (userString.GetSize() % sizeof(WCHAR) == 0)
        {
            *pfIs80Plus = TRUE; // no indicator, presume the worst
        }
        // Return the user string terminator (contains value fIs80Plus)
        *pfIs80Plus = *(reinterpret_cast<PBYTE>(wszTmp + *pcchStringSize));
    }

    *pwszUserString = wszTmp;
    return S_OK;
} // MDInternalRW::GetUserString

//*****************************************************************************
// Get the properties for the given Assembly token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetAssemblyProps(
    mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
    const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
    ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
    ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
    DWORD       *pdwAssemblyFlags)      // [OUT] Flags.
{
    AssemblyRec *pRecord;
    HRESULT hr = S_OK;
    LOCKREAD();

    _ASSERTE(TypeFromToken(mda) == mdtAssembly && RidFromToken(mda));
    IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRecord(RidFromToken(mda), &pRecord));

    if (ppbPublicKey != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getPublicKeyOfAssembly(pRecord, reinterpret_cast<const BYTE **>(ppbPublicKey), pcbPublicKey));
    }
    if (pulHashAlgId)
        *pulHashAlgId = m_pStgdb->m_MiniMd.getHashAlgIdOfAssembly(pRecord);
    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfAssembly(pRecord, pszName));
    }
    if (pMetaData)
    {
        pMetaData->usMajorVersion = m_pStgdb->m_MiniMd.getMajorVersionOfAssembly(pRecord);
        pMetaData->usMinorVersion = m_pStgdb->m_MiniMd.getMinorVersionOfAssembly(pRecord);
        pMetaData->usBuildNumber = m_pStgdb->m_MiniMd.getBuildNumberOfAssembly(pRecord);
        pMetaData->usRevisionNumber = m_pStgdb->m_MiniMd.getRevisionNumberOfAssembly(pRecord);
        IfFailGo(m_pStgdb->m_MiniMd.getLocaleOfAssembly(pRecord, &pMetaData->szLocale));
    }
    if (pdwAssemblyFlags)
    {
        *pdwAssemblyFlags = m_pStgdb->m_MiniMd.getFlagsOfAssembly(pRecord);

        // Turn on the afPublicKey if PublicKey blob is not empty
        DWORD cbPublicKey;
        const BYTE *pbPublicKey;
        IfFailGo(m_pStgdb->m_MiniMd.getPublicKeyOfAssembly(pRecord, &pbPublicKey, &cbPublicKey));
        if (cbPublicKey)
            *pdwAssemblyFlags |= afPublicKey;
    }

ErrExit:
    return hr;

} // MDInternalRW::GetAssemblyProps

//*****************************************************************************
// Get the properties for the given AssemblyRef token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetAssemblyRefProps(
    mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
    const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
    ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
    const void  **ppbHashValue,         // [OUT] Hash blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
    DWORD       *pdwAssemblyRefFlags)   // [OUT] Flags.
{
    AssemblyRefRec  *pRecord;
    HRESULT hr = S_OK;

    LOCKREAD();

    _ASSERTE(TypeFromToken(mdar) == mdtAssemblyRef && RidFromToken(mdar));
    IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRefRecord(RidFromToken(mdar), &pRecord));

    if (ppbPublicKeyOrToken != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getPublicKeyOrTokenOfAssemblyRef(pRecord, reinterpret_cast<const BYTE **>(ppbPublicKeyOrToken), pcbPublicKeyOrToken));
    }
    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfAssemblyRef(pRecord, pszName));
    }
    if (pMetaData)
    {
        pMetaData->usMajorVersion = m_pStgdb->m_MiniMd.getMajorVersionOfAssemblyRef(pRecord);
        pMetaData->usMinorVersion = m_pStgdb->m_MiniMd.getMinorVersionOfAssemblyRef(pRecord);
        pMetaData->usBuildNumber = m_pStgdb->m_MiniMd.getBuildNumberOfAssemblyRef(pRecord);
        pMetaData->usRevisionNumber = m_pStgdb->m_MiniMd.getRevisionNumberOfAssemblyRef(pRecord);
        IfFailGo(m_pStgdb->m_MiniMd.getLocaleOfAssemblyRef(pRecord, &pMetaData->szLocale));
    }
    if (ppbHashValue != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getHashValueOfAssemblyRef(pRecord, reinterpret_cast<const BYTE **>(ppbHashValue), pcbHashValue));
    }
    if (pdwAssemblyRefFlags)
        *pdwAssemblyRefFlags = m_pStgdb->m_MiniMd.getFlagsOfAssemblyRef(pRecord);

ErrExit:
    return hr;
} // MDInternalRW::GetAssemblyRefProps

//*****************************************************************************
// Get the properties for the given File token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetFileProps(
    mdFile      mdf,                    // [IN] The File for which to get the properties.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
    DWORD       *pdwFileFlags)          // [OUT] Flags.
{
    FileRec     *pRecord;
    HRESULT hr = S_OK;

    LOCKREAD();

    _ASSERTE(TypeFromToken(mdf) == mdtFile && RidFromToken(mdf));
    IfFailGo(m_pStgdb->m_MiniMd.GetFileRecord(RidFromToken(mdf), &pRecord));

    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfFile(pRecord, pszName));
    }
    if (ppbHashValue != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getHashValueOfFile(pRecord, reinterpret_cast<const BYTE **>(ppbHashValue), pcbHashValue));
    }
    if (pdwFileFlags)
        *pdwFileFlags = m_pStgdb->m_MiniMd.getFlagsOfFile(pRecord);

ErrExit:
    return hr;
} // MDInternalRW::GetFileProps

//*****************************************************************************
// Get the properties for the given ExportedType token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetExportedTypeProps(
    mdExportedType   mdct,                   // [IN] The ExportedType for which to get the properties.
    LPCSTR      *pszNamespace,          // [OUT] Buffer to fill with name.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
    DWORD       *pdwExportedTypeFlags)       // [OUT] Flags.
{
    ExportedTypeRec  *pRecord;
    HRESULT hr = S_OK;

    LOCKREAD();

    _ASSERTE(TypeFromToken(mdct) == mdtExportedType && RidFromToken(mdct));
    IfFailGo(m_pStgdb->m_MiniMd.GetExportedTypeRecord(RidFromToken(mdct), &pRecord));

    if (pszNamespace != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getTypeNamespaceOfExportedType(pRecord, pszNamespace));
    }
    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getTypeNameOfExportedType(pRecord, pszName));
    }
    if (ptkImplementation)
        *ptkImplementation = m_pStgdb->m_MiniMd.getImplementationOfExportedType(pRecord);
    if (ptkTypeDef)
        *ptkTypeDef = m_pStgdb->m_MiniMd.getTypeDefIdOfExportedType(pRecord);
    if (pdwExportedTypeFlags)
        *pdwExportedTypeFlags = m_pStgdb->m_MiniMd.getFlagsOfExportedType(pRecord);

ErrExit:
    return hr;
} // MDInternalRW::GetExportedTypeProps

//*****************************************************************************
// Get the properties for the given Resource token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetManifestResourceProps(
    mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
    DWORD       *pdwResourceFlags)      // [OUT] Flags.
{
    ManifestResourceRec *pRecord;
    HRESULT hr = S_OK;

    LOCKREAD();

    _ASSERTE(TypeFromToken(mdmr) == mdtManifestResource && RidFromToken(mdmr));
    IfFailGo(m_pStgdb->m_MiniMd.GetManifestResourceRecord(RidFromToken(mdmr), &pRecord));

    if (pszName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfManifestResource(pRecord, pszName));
    }
    if (ptkImplementation)
        *ptkImplementation = m_pStgdb->m_MiniMd.getImplementationOfManifestResource(pRecord);
    if (pdwOffset)
        *pdwOffset = m_pStgdb->m_MiniMd.getOffsetOfManifestResource(pRecord);
    if (pdwResourceFlags)
        *pdwResourceFlags = m_pStgdb->m_MiniMd.getFlagsOfManifestResource(pRecord);

ErrExit:
    return hr;
} // MDInternalRW::GetManifestResourceProps

//*****************************************************************************
// Find the ExportedType given the name.
//*****************************************************************************
__checkReturn
STDMETHODIMP MDInternalRW::FindExportedTypeByName( // S_OK or error
    LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
    LPCSTR      szName,                 // [IN] Name of the ExportedType.
    mdExportedType   tkEnclosingType,        // [IN] Enclosing ExportedType.
    mdExportedType   *pmct)                  // [OUT] Put ExportedType token here.
{
    _ASSERTE(szName && pmct);
    HRESULT hr = S_OK;
    LOCKREADIFFAILRET();

    IMetaModelCommon *pCommon = static_cast<IMetaModelCommon*>(&m_pStgdb->m_MiniMd);
    return pCommon->CommonFindExportedType(szNamespace, szName, tkEnclosingType, pmct);
} // MDInternalRW::FindExportedTypeByName

//*****************************************************************************
// Find the ManifestResource given the name.
//*****************************************************************************
__checkReturn
STDMETHODIMP MDInternalRW::FindManifestResourceByName(// S_OK or error
    LPCSTR      szName,                 // [IN] Name of the resource.
    mdManifestResource *pmmr)           // [OUT] Put ManifestResource token here.
{
    _ASSERTE(szName && pmmr);

    ManifestResourceRec *pRecord;
    ULONG       cRecords;               // Count of records.
    LPCUTF8     szNameTmp = 0;          // Name obtained from the database.
    ULONG       i;
    HRESULT     hr = S_OK;

    LOCKREAD();

    cRecords = m_pStgdb->m_MiniMd.getCountManifestResources();

    // Search for the ExportedType.
    for (i = 1; i <= cRecords; i++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetManifestResourceRecord(i, &pRecord));
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfManifestResource(pRecord, &szNameTmp));
        if (! strcmp(szName, szNameTmp))
        {
            *pmmr = TokenFromRid(i, mdtManifestResource);
            goto ErrExit;
        }
    }
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:

    return hr;
} // MDInternalRW::FindManifestResourceByName

//*****************************************************************************
// Get the Assembly token from the given scope.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetAssemblyFromScope( // S_OK or error
    mdAssembly  *ptkAssembly)           // [OUT] Put token here.
{
    _ASSERTE(ptkAssembly);

    if (m_pStgdb->m_MiniMd.getCountAssemblys())
    {
        *ptkAssembly = TokenFromRid(1, mdtAssembly);
        return S_OK;
    }
    else
        return CLDB_E_RECORD_NOTFOUND;
} // MDInternalRW::GetAssemblyFromScope

//*******************************************************************************
// return properties regarding a TypeSpec
//*******************************************************************************
//*******************************************************************************
// return properties regarding a TypeSpec
//*******************************************************************************
__checkReturn
HRESULT MDInternalRW::GetTypeSpecFromToken(   // S_OK or error.
    mdTypeSpec typespec,                // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    HRESULT             hr = NOERROR;

    _ASSERTE(TypeFromToken(typespec) == mdtTypeSpec);
    _ASSERTE(ppvSig && pcbSig);

    if (!IsValidToken(typespec))
        return E_INVALIDARG;

    TypeSpecRec *pRec;
    IfFailRet(m_pStgdb->m_MiniMd.GetTypeSpecRecord(RidFromToken(typespec), &pRec));

    if (pRec == NULL)
        return CLDB_E_FILE_CORRUPT;

    IfFailRet(m_pStgdb->m_MiniMd.getSignatureOfTypeSpec(pRec, ppvSig, pcbSig));

    return hr;
} // MDInternalRW::GetTypeSpecFromToken


//*****************************************************************************
// Return contents of Pinvoke given the forwarded member token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetPinvokeMap(
    mdToken     tk,                     // [IN] FieldDef, MethodDef or MethodImpl.
    DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
    LPCSTR      *pszImportName,         // [OUT] Import name.
    mdModuleRef *pmrImportDLL)          // [OUT] ModuleRef token for the target DLL.
{
    ImplMapRec  *pRecord;
    uint32_t    iRecord;
    HRESULT     hr = S_OK;

    LOCKREAD();

    IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));
    if (InvalidRid(iRecord))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    else
        IfFailGo(m_pStgdb->m_MiniMd.GetImplMapRecord(iRecord, &pRecord));

    if (pdwMappingFlags)
        *pdwMappingFlags = m_pStgdb->m_MiniMd.getMappingFlagsOfImplMap(pRecord);
    if (pszImportName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.getImportNameOfImplMap(pRecord, pszImportName));
    }
    if (pmrImportDLL)
        *pmrImportDLL = m_pStgdb->m_MiniMd.getImportScopeOfImplMap(pRecord);
ErrExit:
    return hr;
} // MDInternalRW::GetPinvokeMap

//*****************************************************************************
// convert a text signature to com format
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::ConvertTextSigToComSig(// Return hresult.
    BOOL        fCreateTrIfNotFound,    // create typeref if not found or not
    LPCSTR      pSignature,             // class file format signature
    CQuickBytes *pqbNewSig,             // [OUT] place holder for COM+ signature
    ULONG       *pcbCount)              // [OUT] the result size of signature
{
    return E_NOTIMPL;
} // _ConvertTextSigToComSig

//*****************************************************************************
// This is a way for the EE to associate some data with this RW metadata to
//  be released when this RW goes away.  This is useful when a RO metadata is
//  converted to RW, because arbitrary threads can be executing in the RO.
//  So, we hold onto the RO here, and when the module shuts down, we release it.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::SetUserContextData(// S_OK or E_NOTIMPL
    IUnknown    *pIUnk)                 // The user context.
{
    // Only one chance to do this.
    if (m_pUserUnk)
        return E_UNEXPECTED;
    m_pUserUnk = pIUnk;
    return S_OK;
} // MDInternalRW::SetUserContextData

//*****************************************************************************
// determine if a token is valid or not
//*****************************************************************************
BOOL MDInternalRW::IsValidToken(        // True or False.
    mdToken     tk)                     // [IN] Given token.
{
    RID  rid = RidFromToken(tk);
    // no need to lock on this function.
    if (rid == 0)
    {
        return FALSE;
    }
    switch (TypeFromToken(tk))
    {
    case mdtModule:
        // can have only one module record
        return (rid <= m_pStgdb->m_MiniMd.getCountModules());
    case mdtTypeRef:
        return (rid <= m_pStgdb->m_MiniMd.getCountTypeRefs());
    case mdtTypeDef:
        return (rid <= m_pStgdb->m_MiniMd.getCountTypeDefs());
    case mdtFieldDef:
        return (rid <= m_pStgdb->m_MiniMd.getCountFields());
    case mdtMethodDef:
        return (rid <= m_pStgdb->m_MiniMd.getCountMethods());
    case mdtParamDef:
        return (rid <= m_pStgdb->m_MiniMd.getCountParams());
    case mdtInterfaceImpl:
        return (rid <= m_pStgdb->m_MiniMd.getCountInterfaceImpls());
    case mdtMemberRef:
        return (rid <= m_pStgdb->m_MiniMd.getCountMemberRefs());
    case mdtCustomAttribute:
        return (rid <= m_pStgdb->m_MiniMd.getCountCustomAttributes());
    case mdtPermission:
        return (rid <= m_pStgdb->m_MiniMd.getCountDeclSecuritys());
    case mdtSignature:
        return (rid <= m_pStgdb->m_MiniMd.getCountStandAloneSigs());
    case mdtEvent:
        return (rid <= m_pStgdb->m_MiniMd.getCountEvents());
    case mdtProperty:
        return (rid <= m_pStgdb->m_MiniMd.getCountPropertys());
    case mdtModuleRef:
        return (rid <= m_pStgdb->m_MiniMd.getCountModuleRefs());
    case mdtTypeSpec:
        return (rid <= m_pStgdb->m_MiniMd.getCountTypeSpecs());
    case mdtAssembly:
        return (rid <= m_pStgdb->m_MiniMd.getCountAssemblys());
    case mdtAssemblyRef:
        return (rid <= m_pStgdb->m_MiniMd.getCountAssemblyRefs());
    case mdtFile:
        return (rid <= m_pStgdb->m_MiniMd.getCountFiles());
    case mdtExportedType:
        return (rid <= m_pStgdb->m_MiniMd.getCountExportedTypes());
    case mdtManifestResource:
        return (rid <= m_pStgdb->m_MiniMd.getCountManifestResources());
    case mdtMethodSpec:
        return (rid <= m_pStgdb->m_MiniMd.getCountMethodSpecs());
    case mdtString:
        // need to check the user string heap
        return m_pStgdb->m_MiniMd.m_UserStringHeap.IsValidIndex(rid);
    }
    return FALSE;
} // MDInternalRW::IsValidToken

mdModule MDInternalRW::GetModuleFromScope(void)
{
    return TokenFromRid(1, mdtModule);
} // MDInternalRW::GetModuleFromScope

//*****************************************************************************
// Given a MetaData with ENC changes, apply those changes to this MetaData.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::ApplyEditAndContinue( // S_OK or error.
    MDInternalRW *pDeltaMD)             // Interface to MD with the ENC delta.
{
    HRESULT     hr;                     // A result.
    // Get the MiniMd on the delta.

    LOCKWRITEIFFAILRET();

    CMiniMdRW   &mdDelta = pDeltaMD->m_pStgdb->m_MiniMd;
    CMiniMdRW   &mdBase = m_pStgdb->m_MiniMd;


    IfFailGo(mdBase.ConvertToRW());
    IfFailGo(mdBase.ApplyDelta(mdDelta));
ErrExit:
    return hr;
} // MDInternalRW::ApplyEditAndContinue

//*****************************************************************************
// Given a MetaData with ENC changes, enumerate the changed tokens.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::EnumDeltaTokensInit(  // return hresult
    HENUMInternal   *phEnum)            // Enumerator to initialize.
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       index;                  // Loop control.
    ENCLogRec   *pRec;                  // An ENCLog record.

    // Vars for query.
    _ASSERTE(phEnum);
    HENUMInternal::ZeroEnum(phEnum);

    // cache the tkKind and the scope
    phEnum->m_tkKind = 0;

    phEnum->m_EnumType = MDSimpleEnum;

    HENUMInternal::InitDynamicArrayEnum(phEnum);
    for (index = 1; index <= m_pStgdb->m_MiniMd.m_Schema.m_cRecs[TBL_ENCLog]; ++index)
    {
        // Get the token type; see if it is a real token.
        IfFailGo(m_pStgdb->m_MiniMd.GetENCLogRecord(index, &pRec));
        if (CMiniMdRW::IsRecId(pRec->GetToken()))
            continue;
        // If there is a function code, that means that this flags a child-record
        //  addition.  The child record will generate its own token, so did the
        //  parent, so skip the record.
        if (pRec->GetFuncCode())
            continue;

        IfFailGo( HENUMInternal::AddElementToEnum(
            phEnum,
            pRec->GetToken()));
    }

ErrExit:
    // we are done
    return hr;
} // MDInternalRW::EnumDeltaTokensInit


//*****************************************************************************
// Static function to apply a delta md.  This is what the EE calls to apply
//  the metadata updates from an update PE to live metadata.
// <TODO>MAY REPLACE THE IMDInternalImport POINTER!</TODO>
//*****************************************************************************
__checkReturn
HRESULT MDApplyEditAndContinue(         // S_OK or error.
    IMDInternalImport **ppIMD,          // [in, out] The metadata to be updated.
    IMDInternalImportENC *pDeltaMD)     // [in] The delta metadata.
{
    HRESULT     hr;                     // A result.
    IMDInternalImportENC *pENC;         // ENC interface on the metadata.

    // If the input metadata isn't RW, convert it.
    hr = (*ppIMD)->QueryInterface(IID_IMDInternalImportENC, (void**)&pENC);
    if (FAILED(hr))
    {
        IfFailGo(ConvertRO2RW(*ppIMD, IID_IMDInternalImportENC, (void**)&pENC));
        // Replace the old interface pointer with the ENC one.
        (*ppIMD)->Release();
        IfFailGo(pENC->QueryInterface(IID_IMDInternalImport, (void**)ppIMD));
    }

    // Apply the delta to the input metadata.
    hr = pENC->ApplyEditAndContinue(static_cast<MDInternalRW*>(pDeltaMD));

ErrExit:
    if (pENC)
        pENC->Release();
    return hr;
} // MDApplyEditAndContinue

//*****************************************************************************
// Given a scope, return the table size and table ptr for a given index
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::GetTableInfoWithIndex(     // return size
    ULONG  index,                // [IN] pass in the index
    void **pTable,               // [OUT] pointer to table at index
    void **pTableSize)           // [OUT] size of table at index
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//*****************************************************************************
// Given a delta metadata byte stream, apply the changes to the current metadata
// object returning the resulting metadata object in ppv
//*****************************************************************************
__checkReturn
HRESULT MDInternalRW::ApplyEditAndContinue(
    void        *pDeltaMD,              // [IN] the delta metadata
    ULONG       cbDeltaMD,              // [IN] length of pData
    IMDInternalImport **ppv)            // [OUT] the resulting metadata interface
{
    _ASSERTE(pDeltaMD);
    _ASSERTE(ppv);

    HRESULT hr = E_FAIL;
    IMDInternalImportENC *pDeltaMDImport = NULL;

    IfFailGo(GetInternalWithRWFormat(pDeltaMD, cbDeltaMD, 0, IID_IMDInternalImportENC, (void**)&pDeltaMDImport));

    *ppv = this;
    IfFailGo(MDApplyEditAndContinue(ppv, pDeltaMDImport));

ErrExit:
    if (pDeltaMDImport)
        pDeltaMDImport->Release();

    return hr;
} // MDInternalRW::ApplyEditAndContinue

#endif //FEATURE_METADATA_INTERNAL_APIS
