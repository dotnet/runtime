// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
//  File: MDInternalDisp.CPP
// 

//  Notes:
//      
//
// ===========================================================================
#include "stdafx.h"
#include "mdinternaldisp.h"
#include "mdinternalro.h"
#include "posterror.h"
#include "corpriv.h"
#include "assemblymdinternaldisp.h"
#include "pedecoder.h"
#include "winmdinterfaces.h"

#ifdef FEATURE_METADATA_INTERNAL_APIS

// forward declaration
HRESULT GetInternalWithRWFormat(
    LPVOID      pData, 
    ULONG       cbData, 
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

//*****************************************************************************
// CheckFileFormat
// This function will determine if the in-memory image is a readonly, readwrite,
// or ICR format.
//*****************************************************************************
HRESULT 
CheckFileFormat(
    LPVOID        pData, 
    ULONG         cbData, 
    MDFileFormat *pFormat)  // [OUT] the file format
{
    HRESULT        hr = NOERROR;
    STORAGEHEADER  sHdr;        // Header for the storage.
    PSTORAGESTREAM pStream;     // Pointer to each stream.
    int            i;
    ULONG          cbStreamBuffer;
    
    _ASSERTE(pFormat != NULL);
    
    *pFormat = MDFormat_Invalid;
    
    // Validate the signature of the format, or it isn't ours.
    if (FAILED(hr = MDFormat::VerifySignature((PSTORAGESIGNATURE) pData, cbData)))
        goto ErrExit;
    
    // Remaining buffer size behind the stream header (pStream).
    cbStreamBuffer = cbData;
    // Get back the first stream.
    pStream = MDFormat::GetFirstStream_Verify(&sHdr, pData, &cbStreamBuffer);
    if (pStream == NULL)
    {
        Debug_ReportError("Invalid MetaData storage signature - cannot get the first stream header.");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }
    
    // Loop through each stream and pick off the ones we need.
    for (i = 0; i < sHdr.GetiStreams(); i++)
    {
        // Do we have enough buffer to read stream header?
        if (cbStreamBuffer < sizeof(*pStream))
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        // Get next stream.
        PSTORAGESTREAM pNext = pStream->NextStream_Verify();
        
        // Check that stream header is within the buffer.
        if (((LPBYTE)pStream >= ((LPBYTE)pData + cbData)) ||
            ((LPBYTE)pNext   >  ((LPBYTE)pData + cbData)))
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }
        
        // Check that the stream data starts and fits within the buffer.
        //  need two checks on size because of wraparound.
        if ((pStream->GetOffset() > cbData) || 
            (pStream->GetSize() > cbData) || 
            ((pStream->GetSize() + pStream->GetOffset()) < pStream->GetOffset()) || 
            ((pStream->GetSize() + pStream->GetOffset()) > cbData))
        {
            Debug_ReportError("Stream data are not within MetaData block.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }
        
        // Pick off the location and size of the data.
        
        if (strcmp(pStream->GetName(), COMPRESSED_MODEL_STREAM_A) == 0)
        {
            // Validate that only one of compressed/uncompressed is present.
            if (*pFormat != MDFormat_Invalid)
            {   // Already found a good stream.
                Debug_ReportError("Compressed model stream #~ is second important stream.");
                hr = CLDB_E_FILE_CORRUPT;
                goto ErrExit;
            }
            // Found the compressed meta data stream.
            *pFormat = MDFormat_ReadOnly;
        }
        else if (strcmp(pStream->GetName(), ENC_MODEL_STREAM_A) == 0)
        {
#ifdef FEATURE_METADATA_STANDALONE_WINRT
            Debug_ReportError("ENC model stream #- is not supported.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
#else //!FEATURE_METADATA_STANDALONE_WINRT
            // Validate that only one of compressed/uncompressed is present.
            if (*pFormat != MDFormat_Invalid)
            {   // Already found a good stream.
                Debug_ReportError("ENC model stream #- is second important stream.");
                hr = CLDB_E_FILE_CORRUPT;
                goto ErrExit;
            }
            // Found the ENC meta data stream.
            *pFormat = MDFormat_ReadWrite;
#endif //!FEATURE_METADATA_STANDALONE_WINRT
        }
        else if (strcmp(pStream->GetName(), SCHEMA_STREAM_A) == 0)
        {
#ifdef FEATURE_METADATA_STANDALONE_WINRT
            Debug_ReportError("Schema stream #Schema is not supported.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
#else //!FEATURE_METADATA_STANDALONE_WINRT
            // Found the uncompressed format
            *pFormat = MDFormat_ICR;
            
            // keep going. We may find the compressed format later. 
            // If so, we want to use the compressed format.
#endif //!FEATURE_METADATA_STANDALONE_WINRT
        }
        
        // Pick off the next stream if there is one.
        pStream = pNext;
        cbStreamBuffer = (ULONG)((LPBYTE)pData + cbData - (LPBYTE)pNext);
    }
    
    if (*pFormat == MDFormat_Invalid)
    {   // Didn't find a good stream.
        Debug_ReportError("Cannot find MetaData stream.");
        hr = CLDB_E_FILE_CORRUPT;
    }
    
ErrExit:
    return hr;
} // CheckFileFormat



//*****************************************************************************
// GetMDInternalInterface.
// This function will check the metadata section and determine if it should
// return an interface which implements ReadOnly or ReadWrite.
//*****************************************************************************
STDAPI GetMDInternalInterface(
    LPVOID      pData, 
    ULONG       cbData, 
    DWORD       flags,                  // [IN] ofRead or ofWrite.
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk)               // [out] Return interface on success.
{
    HRESULT     hr = NOERROR;
    MDInternalRO *pInternalRO = NULL;
    IMDCommon    *pInternalROMDCommon = NULL;
    MDFileFormat format;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);

    if (ppIUnk == NULL)
        IfFailGo(E_INVALIDARG);

    // Determine the file format we're trying to read.
    IfFailGo( CheckFileFormat(pData, cbData, &format) );

    // Found a fully-compressed, read-only format.
    if ( format == MDFormat_ReadOnly )
    {
        pInternalRO = new (nothrow) MDInternalRO;
        IfNullGo( pInternalRO );

        IfFailGo( pInternalRO->Init(const_cast<void*>(pData), cbData) );

#ifdef FEATURE_COMINTEROP
        IfFailGo(pInternalRO->QueryInterface(IID_IMDCommon, (void**)&pInternalROMDCommon));
        IfFailGo( (flags & ofNoTransform) ? S_FALSE : CheckIfWinMDAdapterNeeded(pInternalROMDCommon));
        if (hr == S_OK)
        {
            IfFailGo(CreateWinMDInternalImportRO(pInternalROMDCommon, riid, (void**)ppIUnk));
        }
        else
#endif // FEATURE_COMINTEROP
        {
            IfFailGo(pInternalRO->QueryInterface(riid, ppIUnk));
        }

    }
    else
    {
        // Found a not-fully-compressed, ENC format.
        _ASSERTE( format == MDFormat_ReadWrite );
        IfFailGo( GetInternalWithRWFormat( pData, cbData, flags, riid, ppIUnk ) );
    }

ErrExit:

    // clean up
    if ( pInternalRO )
        pInternalRO->Release();
    if ( pInternalROMDCommon )
        pInternalROMDCommon->Release();

    END_SO_INTOLERANT_CODE;
    
    return hr;
}   // GetMDInternalInterface


#ifdef FEATURE_FUSION

#ifndef DACCESS_COMPILE

//*****************************************************************************
// GetAssemblyMDInternalImport.
// Instantiating an instance of AssemblyMDInternalImport.
// This class can support the IMetaDataAssemblyImport and some functionalities 
// of IMetaDataImport on the internal import interface (IMDInternalImport).
//*****************************************************************************
STDAPI GetAssemblyMDInternalImport(     // Return code.
    LPCWSTR     szFileName,             // [in] The scope to open.
    REFIID      riid,                   // [in] The interface desired.
    IUnknown    **ppIUnk)               // [out] Return interface on success.
{
    return GetAssemblyMDInternalImportEx(szFileName, riid, MDInternalImport_Default, ppIUnk);
}

STDAPI GetAssemblyMDInternalImportEx(   // Return code.
    LPCWSTR     szFileName,             // [in] The scope to open.
    REFIID      riid,                   // [in] The interface desired.
    MDInternalImportFlags flags,        // [in] Flags to control opening the assembly
    IUnknown    **ppIUnk,               // [out] Return interface on success.
    HANDLE      hFile)
{
    HRESULT     hr;

    if (!szFileName || !szFileName[0] || !ppIUnk)
        return E_INVALIDARG;
    
    // Sanity check the name.
    if (wcslen(szFileName) >= _MAX_PATH)
        return E_INVALIDARG;
    
    if (memcmp(szFileName, W("file:"), 10) == 0)
        szFileName = &szFileName[5];
    
    HCORMODULEHolder hModule;
    DWORD dwFileLength;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    
    IfFailGoto(RuntimeOpenImageInternal(szFileName, &hModule, &dwFileLength, flags, hFile), ErrAsmExpected);

    IfFailGo(GetAssemblyMDInternalImportHelper(hModule, riid, flags, ppIUnk));


ErrAsmExpected:
    if(hr == COR_E_BADIMAGEFORMAT)
        hr = COR_E_ASSEMBLYEXPECTED;

ErrExit:
;
    END_SO_INTOLERANT_CODE;

    return hr;
}

HRESULT GetAssemblyMDInternalImportFromImage(
    HCORMODULE hImage,
    REFIID riid,
    IUnknown **ppIUnk)
{

    HRESULT hr;

    IfFailGo(GetAssemblyMDInternalImportHelper(hImage, riid, MDInternalImport_Default, ppIUnk));

ErrExit:
    return hr;
}

STDAPI GetAssemblyMDInternalImportByStream( // Return code.
    IStream     *pIStream,              // [in] The IStream for the file
    UINT64      AssemblyId,             // [in] Unique Id for the assembly
    REFIID      riid,                   // [in] The interface desired.
    IUnknown    **ppIUnk)               // [out] Return interface on success.
{
    return GetAssemblyMDInternalImportByStreamEx(pIStream, AssemblyId, riid, MDInternalImport_Default, ppIUnk);
}

STDAPI GetAssemblyMDInternalImportByStreamEx( // Return code.
    IStream     *pIStream,              // [in] The IStream for the file
    UINT64      AssemblyId,             // [in] Unique Id for the assembly
    REFIID      riid,                   // [in] The interface desired.
    MDInternalImportFlags flags,        // [in[ Flags to control opening the assembly
    IUnknown    **ppIUnk)               // [out] Return interface on success.
{
    if (!pIStream || !ppIUnk)
        return E_INVALIDARG;
    
    HRESULT hr;
    DWORD dwFileLength;
    HCORMODULEHolder hModule;
    
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    
    IfFailGoto(RuntimeOpenImageByStream(pIStream, AssemblyId, 0, &hModule, &dwFileLength, flags), ErrAsmExpected);

    IfFailGo(GetAssemblyMDInternalImportHelper(hModule, riid, flags, ppIUnk));


ErrAsmExpected:
    if(hr == COR_E_BADIMAGEFORMAT)
       hr = COR_E_ASSEMBLYEXPECTED;

ErrExit:
    ;
    END_SO_INTOLERANT_CODE;

    return hr;
}


HRESULT GetAssemblyMDInternalImportHelper(HCORMODULE hModule,
                                          REFIID     riid,
                                          MDInternalImportFlags flags,
                                          IUnknown   **ppIUnk)
{
    AssemblyMDInternalImport *pAssemblyMDInternalImport = NULL;
    HRESULT hr;
    LPVOID base;
    PEDecoder pe;
    IfFailGoto(RuntimeGetImageBase(hModule,&base,TRUE,NULL), ErrAsmExpected);

    if (base!=NULL)
        IfFailGoto(pe.Init(base), ErrAsmExpected);
    else
    {
        COUNT_T lgth;
        IfFailGoto(RuntimeGetImageBase(hModule,&base,FALSE,&lgth), ErrAsmExpected);
        pe.Init(base, lgth);
    }

    // Both of these need to pass.
    if (!pe.HasCorHeader() || !pe.CheckCorHeader())
        IfFailGo(COR_E_ASSEMBLYEXPECTED);
    // Only one of these needs to.
    if (!pe.CheckILFormat() && !pe.CheckNativeFormat())
        IfFailGo(COR_E_BADIMAGEFORMAT);
        
    COUNT_T cbMetaData;
    LPCVOID pMetaData;
    pMetaData = pe.GetMetadata(&cbMetaData);

    // Get the IL metadata.
    IMDInternalImport *pMDInternalImport;
    IfFailGo(RuntimeGetMDInternalImport(hModule, flags, &pMDInternalImport));
    if (pMDInternalImport == NULL)
        IfFailGo(E_OUTOFMEMORY);

    _ASSERTE(pMDInternalImport);
    pAssemblyMDInternalImport = new (nothrow) AssemblyMDInternalImport (pMDInternalImport);
    if (!pAssemblyMDInternalImport) {
        pMDInternalImport->Release();
        IfFailGo(E_OUTOFMEMORY);
    }

    { // identify PE kind and machine type, plus the version string location
        DWORD           dwKind=0;
        DWORD           dwMachine=0;
        RuntimeGetImageKind(hModule,&dwKind,&dwMachine);
        pAssemblyMDInternalImport->SetPEKind(dwKind);
        pAssemblyMDInternalImport->SetMachine(dwMachine);

        {
            LPCSTR pString = NULL;
            IfFailGo(GetImageRuntimeVersionString((PVOID)pMetaData, &pString));

            pAssemblyMDInternalImport->SetVersionString(pString);
        }

    }

    pAssemblyMDInternalImport->SetHandle(hModule);

#ifdef FEATURE_PREJIT
    // Check for zap header for INativeImageInstallInfo
    // Dont do this if we are returning the IL metadata as CORCOMPILE_DEPENDENCY
    // references the native image metadata, not the IL metadata.

    if (pe.HasNativeHeader() && !(flags & MDInternalImport_ILMetaData))
    {
        CORCOMPILE_VERSION_INFO *pNativeVersionInfo = pe.GetNativeVersionInfo();

        COUNT_T cDeps;
        CORCOMPILE_DEPENDENCY *pDeps = pe.GetNativeDependencies(&cDeps);

        pAssemblyMDInternalImport->SetZapVersionInfo(pNativeVersionInfo, pDeps, cDeps);
    }
#endif  // FEATURE_PREJIT

    IfFailGo(pAssemblyMDInternalImport->QueryInterface(riid, (void**)ppIUnk));

    return hr;

ErrAsmExpected:
    if(hr == COR_E_BADIMAGEFORMAT)
        hr = COR_E_ASSEMBLYEXPECTED;

ErrExit:

    if (pAssemblyMDInternalImport)
        delete pAssemblyMDInternalImport;

    return hr;
}

AssemblyMDInternalImport::AssemblyMDInternalImport (IMDInternalImport *pMDInternalImport)
:   m_cRef(0),
    m_pHandle(0),
    m_pBase(NULL),
#ifdef FEATURE_PREJIT
    m_pZapVersionInfo(NULL),
#endif  // FEATURE_PREJIT
    m_pMDInternalImport(pMDInternalImport),
    m_dwPEKind(0),
    m_dwMachine(0),
    m_szVersionString("")
{
    _ASSERTE(m_pMDInternalImport);
} // AssemblyMDInternalImport

AssemblyMDInternalImport::~AssemblyMDInternalImport () 
{
    m_pMDInternalImport->Release();

    if (m_pBase) 
    {
        UnmapViewOfFile(m_pBase);
        m_pBase = NULL;
        CloseHandle(m_pHandle);
    }
    else if(m_pHandle) 
    {
        HRESULT hr;
        hr = RuntimeReleaseHandle(m_pHandle);
        _ASSERTE(SUCCEEDED(hr));
    }

    m_pHandle = NULL;
}

ULONG AssemblyMDInternalImport::AddRef()
{
    return InterlockedIncrement(&m_cRef);
} // ULONG AssemblyMDInternalImport::AddRef()

ULONG AssemblyMDInternalImport::Release()
{
    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef)
    {
        VALIDATE_BACKOUT_STACK_CONSUMPTION;
        delete this;
    }
    return (cRef);
} // ULONG AssemblyMDInternalImport::Release()

HRESULT AssemblyMDInternalImport::QueryInterface(REFIID riid, void **ppUnk)
{ 
    *ppUnk = 0;

    if (riid == IID_IUnknown)
        *ppUnk = (IUnknown *) (IMetaDataAssemblyImport *) this;
    else if (riid == IID_IMetaDataAssemblyImport)
        *ppUnk = (IMetaDataAssemblyImport *) this;
    else if (riid == IID_IMetaDataImport)
        *ppUnk = (IMetaDataImport *) this;
    else if (riid == IID_IMetaDataImport2)
        *ppUnk = (IMetaDataImport2 *) this;
    else if (riid == IID_ISNAssemblySignature)
        *ppUnk = (ISNAssemblySignature *) this;
#ifdef FEATURE_PREJIT
    else if (riid == IID_IGetIMDInternalImport)
        *ppUnk = (IGetIMDInternalImport *) this;
    else if (riid == IID_INativeImageInstallInfo && m_pZapVersionInfo)
        *ppUnk = (INativeImageInstallInfo *) this;
#endif  // FEATURE_PREJIT
    else
        return (E_NOINTERFACE);
    AddRef();
    return (S_OK);
}


STDMETHODIMP AssemblyMDInternalImport::GetAssemblyProps (      // S_OK or error.
    mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
    const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
    ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
    ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
    DWORD       *pdwAssemblyFlags)      // [OUT] Flags.
{
    HRESULT hr;
    LPCSTR  _szName;
    AssemblyMetaDataInternal _AssemblyMetaData;
    
    _AssemblyMetaData.ulProcessor = 0;
    _AssemblyMetaData.ulOS = 0;

    IfFailRet(m_pMDInternalImport->GetAssemblyProps(
        mda,                            // [IN] The Assembly for which to get the properties.
        ppbPublicKey,                   // [OUT] Pointer to the public key.
        pcbPublicKey,                   // [OUT] Count of bytes in the public key.
        pulHashAlgId,                   // [OUT] Hash Algorithm.
        &_szName,                       // [OUT] Buffer to fill with name.
        &_AssemblyMetaData,             // [OUT] Assembly MetaData.
        pdwAssemblyFlags));             // [OUT] Flags.

    if (pchName != NULL)
    {
        *pchName = WszMultiByteToWideChar(CP_UTF8, 0, _szName, -1, szName, cchName);
        if (*pchName == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    if  (pMetaData)
    {
        pMetaData->usMajorVersion = _AssemblyMetaData.usMajorVersion;
        pMetaData->usMinorVersion = _AssemblyMetaData.usMinorVersion;
        pMetaData->usBuildNumber = _AssemblyMetaData.usBuildNumber;
        pMetaData->usRevisionNumber = _AssemblyMetaData.usRevisionNumber;
        pMetaData->ulProcessor = 0;
        pMetaData->ulOS = 0;

        pMetaData->cbLocale = WszMultiByteToWideChar(CP_UTF8, 0, _AssemblyMetaData.szLocale, -1, pMetaData->szLocale, pMetaData->cbLocale);
        if (pMetaData->cbLocale == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::GetAssemblyRefProps (   // S_OK or error.
    mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
    const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
    ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
    const void  **ppbHashValue,         // [OUT] Hash blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
    DWORD       *pdwAssemblyRefFlags)   // [OUT] Flags.
{
    HRESULT hr;
    LPCSTR  _szName;
    AssemblyMetaDataInternal _AssemblyMetaData;
    
    _AssemblyMetaData.ulProcessor = 0;
    _AssemblyMetaData.ulOS = 0;

    IfFailRet(m_pMDInternalImport->GetAssemblyRefProps(
        mdar,                           // [IN] The Assembly for which to get the properties.
        ppbPublicKeyOrToken,            // [OUT] Pointer to the public key or token.
        pcbPublicKeyOrToken,            // [OUT] Count of bytes in the public key or token.
        &_szName,                       // [OUT] Buffer to fill with name.
        &_AssemblyMetaData,             // [OUT] Assembly MetaData.
        ppbHashValue,                   // [OUT] Hash blob.
        pcbHashValue,                   // [OUT] Count of bytes in the hash blob.
        pdwAssemblyRefFlags));          // [OUT] Flags.

    if (pchName != NULL)
    {
        *pchName = WszMultiByteToWideChar(CP_UTF8, 0, _szName, -1, szName, cchName);
        if (*pchName == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    pMetaData->usMajorVersion = _AssemblyMetaData.usMajorVersion;
    pMetaData->usMinorVersion = _AssemblyMetaData.usMinorVersion;
    pMetaData->usBuildNumber = _AssemblyMetaData.usBuildNumber;
    pMetaData->usRevisionNumber = _AssemblyMetaData.usRevisionNumber;
    pMetaData->ulProcessor = 0;
    pMetaData->ulOS = 0;

    pMetaData->cbLocale = WszMultiByteToWideChar(CP_UTF8, 0, _AssemblyMetaData.szLocale, -1, pMetaData->szLocale, pMetaData->cbLocale);
    if (pMetaData->cbLocale == 0)
    {
        return HRESULT_FROM_GetLastError();
    }
    
    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::GetFileProps (          // S_OK or error.
    mdFile      mdf,                    // [IN] The File for which to get the properties.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
    DWORD       *pdwFileFlags)          // [OUT] Flags.
{
    HRESULT hr;
    LPCSTR  _szName;
    IfFailRet(m_pMDInternalImport->GetFileProps(
        mdf,
        &_szName,
        ppbHashValue,
        pcbHashValue,
        pdwFileFlags));

    if (pchName != NULL)
    {
        *pchName = WszMultiByteToWideChar(CP_UTF8, 0, _szName, -1, szName, cchName);
        if (*pchName == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::GetExportedTypeProps (  // S_OK or error.
    mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef or mdExportedType.
    mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
    DWORD       *pdwExportedTypeFlags)       // [OUT] Flags.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetManifestResourceProps (    // S_OK or error.
    mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ManifestResource.
    DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
    DWORD       *pdwResourceFlags)      // [OUT] Flags.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumAssemblyRefs (      // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
    ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HRESULT         hr = NOERROR;
    HENUMInternal  *pEnum;
    
    if (*ppmdEnum == NULL)
    {
        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtAssemblyRef,
            0,
            1,
            &pEnum));
        
        IfFailGo(m_pMDInternalImport->EnumInit(mdtAssemblyRef, 0, pEnum));
        
        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
    {
        pEnum = *ppmdEnum;
    }
    
    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rAssemblyRefs, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    
    return hr;
}

STDMETHODIMP AssemblyMDInternalImport::EnumFiles (             // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdFile      rFiles[],               // [OUT] Put Files here.
    ULONG       cMax,                   // [IN] Max Files to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HRESULT         hr = NOERROR;
    HENUMInternal  *pEnum;
    
    if (*ppmdEnum == NULL)
    {
        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtFile,
            0,
            1,
            &pEnum));
        
        IfFailGo(m_pMDInternalImport->EnumInit(mdtFile, 0, pEnum));
        
        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
    {
        pEnum = *ppmdEnum;
    }
    
    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rFiles, pcTokens));
    
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    return hr;
}

STDMETHODIMP AssemblyMDInternalImport::EnumExportedTypes (     // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdExportedType   rExportedTypes[],  // [OUT] Put ExportedTypes here.
    ULONG       cMax,                   // [IN] Max ExportedTypes to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumManifestResources ( // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
    ULONG       cMax,                   // [IN] Max Resources to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetAssemblyFromScope (  // S_OK or error
    mdAssembly  *ptkAssembly)           // [OUT] Put token here.
{
    return m_pMDInternalImport->GetAssemblyFromScope (ptkAssembly);
}

STDMETHODIMP AssemblyMDInternalImport::FindExportedTypeByName (// S_OK or error
    LPCWSTR     szName,                 // [IN] Name of the ExportedType.
    mdToken     mdtExportedType,        // [IN] ExportedType for the enclosing class.
    mdExportedType   *ptkExportedType)       // [OUT] Put the ExportedType token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindManifestResourceByName (  // S_OK or error
    LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
    mdManifestResource *ptkManifestResource)        // [OUT] Put the ManifestResource token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

void AssemblyMDInternalImport::CloseEnum (
    HCORENUM hEnum)                     // Enum to be closed.
{
    HENUMInternal   *pmdEnum = reinterpret_cast<HENUMInternal *> (hEnum);

    if (pmdEnum == NULL)
        return;

    HENUMInternal::DestroyEnum(pmdEnum);
}

STDMETHODIMP AssemblyMDInternalImport::FindAssembliesByName (  // S_OK or error
    LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
    LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
    LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
    IUnknown *ppIUnk[],                 // [OUT] put IMetaDataAssemblyImport pointers here
    ULONG    cMax,                      // [IN] The max number to put
    ULONG    *pcAssemblies)             // [OUT] The number of assemblies returned.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::CountEnum (HCORENUM hEnum, ULONG *pulCount)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::ResetEnum (HCORENUM hEnum, ULONG ulPos)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumTypeDefs (HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                        ULONG cMax, ULONG *pcTypeDefs)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumInterfaceImpls (HCORENUM *phEnum, mdTypeDef td,
                        mdInterfaceImpl rImpls[], ULONG cMax,
                        ULONG* pcImpls)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumTypeRefs (HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                        ULONG cMax, ULONG* pcTypeRefs)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindTypeDefByName (           // S_OK or error.
    LPCWSTR     szTypeDef,              // [IN] Name of the Type.
    mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
    mdTypeDef   *ptd)                   // [OUT] Put the TypeDef token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetScopeProps (               // S_OK or error.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Put the name here.
    ULONG       cchName,                // [IN] Size of name buffer in wide chars.
    ULONG       *pchName,               // [OUT] Put size of name (wide chars) here.
    GUID        *pmvid)                 // [OUT, OPTIONAL] Put MVID here.
{
    HRESULT hr;
    LPCSTR  _szName;
    
    if (!m_pMDInternalImport->IsValidToken(m_pMDInternalImport->GetModuleFromScope()))
        return COR_E_BADIMAGEFORMAT;

    IfFailRet(m_pMDInternalImport->GetScopeProps(&_szName, pmvid));

    if (pchName != NULL)
    {
        *pchName = WszMultiByteToWideChar(CP_UTF8, 0, _szName, -1, szName, cchName);
        if (*pchName == 0)
        {
            return HRESULT_FROM_GetLastError();
        }
   }

    return S_OK;

}

STDMETHODIMP AssemblyMDInternalImport::GetModuleFromScope (          // S_OK.
    mdModule    *pmd)                   // [OUT] Put mdModule token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetTypeDefProps (             // S_OK or error.
    mdTypeDef   td,                     // [IN] TypeDef token for inquiry.
    __out_ecount (cchTypeDef) LPWSTR szTypeDef, // [OUT] Put name here.
    ULONG       cchTypeDef,             // [IN] size of name buffer in wide chars.
    ULONG       *pchTypeDef,            // [OUT] put size of name (wide chars) here.
    DWORD       *pdwTypeDefFlags,       // [OUT] Put flags here.
    mdToken     *ptkExtends)            // [OUT] Put base class TypeDef/TypeRef here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetInterfaceImplProps (       // S_OK or error.
    mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
    mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
    mdToken     *ptkIface)              // [OUT] Put implemented interface token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetTypeRefProps (             // S_OK or error.
    mdTypeRef   tr,                     // [IN] TypeRef token.
    mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Name of the TypeRef.
    ULONG       cchName,                // [IN] Size of buffer.
    ULONG       *pchName)               // [OUT] Size of Name.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::ResolveTypeRef (mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd)
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMembers (                 // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    mdToken     rMembers[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMembersWithName (         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdToken     rMembers[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMethods (                 // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
    ULONG       cMax,                   // [IN] Max MethodDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMethodsWithName (         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
    ULONG       cMax,                   // [IN] Max MethodDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumFields (                 // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.
    ULONG       cMax,                   // [IN] Max FieldDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumFieldsWithName (         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}


STDMETHODIMP AssemblyMDInternalImport::EnumParams (                  // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
    mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
    ULONG       cMax,                   // [IN] Max ParamDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMemberRefs (              // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
    mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
    ULONG       cMax,                   // [IN] Max MemberRefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMethodImpls (             // S_OK, S_FALSE, or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
    mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
    ULONG       cMax,                   // [IN] Max tokens to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumPermissionSets (          // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
    DWORD       dwActions,              // [IN] if !0, return only these actions.
    mdPermission rPermission[],         // [OUT] Put Permissions here.
    ULONG       cMax,                   // [IN] Max Permissions to put. 
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindMember (
    mdTypeDef   td,                     // [IN] given typedef
    LPCWSTR     szName,                 // [IN] member name 
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdToken     *pmb)                   // [OUT] matching memberdef 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindMethod (
    mdTypeDef   td,                     // [IN] given typedef
    LPCWSTR     szName,                 // [IN] member name 
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMethodDef *pmb)                   // [OUT] matching memberdef 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindField (
    mdTypeDef   td,                     // [IN] given typedef
    LPCWSTR     szName,                 // [IN] member name 
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdFieldDef  *pmb)                   // [OUT] matching memberdef 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindMemberRef (
    mdTypeRef   td,                     // [IN] given typeRef
    LPCWSTR     szName,                 // [IN] member name 
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMemberRef *pmr)                   // [OUT] matching memberref 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetMethodProps (
    mdMethodDef mb,                     // The method for which to get props.
    mdTypeDef   *pClass,                // Put method's class here. 
    __out_ecount (cchMethod) LPWSTR szMethod, // Put method's name here.
    ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
    ULONG       *pchMethod,             // Put actual size here 
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    ULONG       *pulCodeRVA,            // [OUT] codeRVA
    DWORD       *pdwImplFlags)          // [OUT] Impl. Flags
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetMemberRefProps (           // S_OK or error.
    mdMemberRef mr,                     // [IN] given memberref 
    mdToken     *ptk,                   // [OUT] Put classref or classdef here. 
    __out_ecount (cchMember) LPWSTR szMember, // [OUT] buffer to fill for member's name
    ULONG       cchMember,              // [IN] the count of char of szMember
    ULONG       *pchMember,             // [OUT] actual count of char in member name
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
    ULONG       *pbSig)                 // [OUT] actual size of signature blob
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumProperties (              // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdProperty  rProperties[],          // [OUT] Put Properties here.
    ULONG       cMax,                   // [IN] Max properties to put.
    ULONG       *pcProperties)          // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumEvents (                  // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdEvent     rEvents[],              // [OUT] Put events here.
    ULONG       cMax,                   // [IN] Max events to put.
    ULONG       *pcEvents)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetEventProps (               // S_OK, S_FALSE, or error.
    mdEvent     ev,                     // [IN] event token 
    mdTypeDef   *pClass,                // [OUT] typedef containing the event declarion.
    LPCWSTR     szEvent,                // [OUT] Event name 
    ULONG       cchEvent,               // [IN] the count of wchar of szEvent
    ULONG       *pchEvent,              // [OUT] actual count of wchar for event's name 
    DWORD       *pdwEventFlags,         // [OUT] Event flags.
    mdToken     *ptkEventType,          // [OUT] EventType class
    mdMethodDef *pmdAddOn,              // [OUT] AddOn method of the event
    mdMethodDef *pmdRemoveOn,           // [OUT] RemoveOn method of the event
    mdMethodDef *pmdFire,               // [OUT] Fire method of the event
    mdMethodDef rmdOtherMethod[],       // [OUT] other method of the event
    ULONG       cMax,                   // [IN] size of rmdOtherMethod
    ULONG       *pcOtherMethod)         // [OUT] total number of other method of this event 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMethodSemantics (         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
    mdToken     rEventProp[],           // [OUT] Put Event/Property here.
    ULONG       cMax,                   // [IN] Max properties to put.
    ULONG       *pcEventProp)           // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetMethodSemantics (          // S_OK, S_FALSE, or error.
    mdMethodDef mb,                     // [IN] method token
    mdToken     tkEventProp,            // [IN] event/property token.
    DWORD       *pdwSemanticsFlags)       // [OUT] the role flags for the method/propevent pair 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetClassLayout (
    mdTypeDef   td,                     // [IN] give typedef
    DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
    COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array 
    ULONG       cMax,                   // [IN] size of the array
    ULONG       *pcFieldOffset,         // [OUT] needed array size
    ULONG       *pulClassSize)              // [OUT] the size of the class
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetFieldMarshal (
    mdToken     tk,                     // [IN] given a field's memberdef
    PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
    ULONG       *pcbNativeType)         // [OUT] the count of bytes of *ppvNativeType
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetRVA (                      // S_OK or error.
    mdToken     tk,                     // Member for which to set offset
    ULONG       *pulCodeRVA,            // The offset
    DWORD       *pdwImplFlags)          // the implementation flags 
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetPermissionSetProps (
    mdPermission pm,                    // [IN] the permission token.
    DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
    void const  **ppvPermission,        // [OUT] permission blob.
    ULONG       *pcbPermission)         // [OUT] count of bytes of pvPermission.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetSigFromToken (             // S_OK or error.
    mdSignature mdSig,                  // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetModuleRefProps (           // S_OK or error.
    mdModuleRef mur,                    // [IN] moduleref token.
    __out_ecount (cchName) LPWSTR szName, // [OUT] buffer to fill with the moduleref name.
    ULONG       cchName,                // [IN] size of szName in wide characters.
    ULONG       *pchName)               // [OUT] actual count of characters in the name.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumModuleRefs (              // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
    ULONG       cmax,                   // [IN] max memberrefs to put.
    ULONG       *pcModuleRefs)          // [OUT] put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetTypeSpecFromToken (        // S_OK or error.
    mdTypeSpec typespec,                // [IN] TypeSpec token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetNameFromToken (            // Not Recommended! May be removed!
    mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
    MDUTF8CSTR  *pszUtf8NamePtr)        // [OUT] Return pointer to UTF8 name in heap.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumUnresolvedMethods (       // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     rMethods[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetUserString (               // S_OK or error.
    mdString    stk,                    // [IN] String token.
    __out_ecount (cchString) LPWSTR szString, // [OUT] Copy of string.
    ULONG       cchString,              // [IN] Max chars of room in szString.
    ULONG       *pchString)             // [OUT] How many chars in actual string.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetPinvokeMap (               // S_OK or error.
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
    __out_ecount (cchImportName) LPWSTR szImportName, // [OUT] Import name.
    ULONG       cchImportName,          // [IN] Size of the name buffer.
    ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
    mdModuleRef *pmrImportDLL)          // [OUT] ModuleRef token for the target DLL.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumSignatures (              // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdSignature rSignatures[],          // [OUT] put signatures here.
    ULONG       cmax,                   // [IN] max signatures to put.
    ULONG       *pcSignatures)          // [OUT] put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumTypeSpecs (               // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
    ULONG       cmax,                   // [IN] max TypeSpecs to put.
    ULONG       *pcTypeSpecs)           // [OUT] put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumUserStrings (             // S_OK or error.
    HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
    mdString    rStrings[],             // [OUT] put Strings here.
    ULONG       cmax,                   // [IN] max Strings to put.
    ULONG       *pcStrings)             // [OUT] put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetParamForMethodIndex (      // S_OK or error.
    mdMethodDef md,                     // [IN] Method token.
    ULONG       ulParamSeq,             // [IN] Parameter sequence.
    mdParamDef  *ppd)                   // [IN] Put Param token here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumCustomAttributes (        // S_OK or error.
    HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
    mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
    mdToken     tkType,                 // [IN] Type of interest, 0 for all.
    mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
    ULONG       cMax,                   // [IN] Size of rCustomAttributes.
    ULONG       *pcCustomAttributes)        // [OUT, OPTIONAL] Put count of token values here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetCustomAttributeProps (     // S_OK or error.
    mdCustomAttribute cv,               // [IN] CustomAttribute token.
    mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
    mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
    void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
    ULONG       *pcbSize)               // [OUT, OPTIONAL] Put size of date here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::FindTypeRef (
    mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
    LPCWSTR     szName,                 // [IN] TypeRef Name.
    mdTypeRef   *ptr)                   // [OUT] matching TypeRef.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetMemberProps (
    mdToken     mb,                     // The member for which to get props.
    mdTypeDef   *pClass,                // Put member's class here. 
    __out_ecount (cchMember) LPWSTR szMember, // Put member's name here.
    ULONG       cchMember,              // Size of szMember buffer in wide chars.
    ULONG       *pchMember,             // Put actual size here 
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    ULONG       *pulCodeRVA,            // [OUT] codeRVA
    DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
    DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue,             // [OUT] constant value 
    ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetFieldProps (
    mdFieldDef  mb,                     // The field for which to get props.
    mdTypeDef   *pClass,                // Put field's class here.
    __out_ecount (cchField) LPWSTR szField, // Put field's name here.
    ULONG       cchField,               // Size of szField buffer in wide chars.
    ULONG       *pchField,              // Put actual size here 
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue,             // [OUT] constant value 
    ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetPropertyProps (            // S_OK, S_FALSE, or error.
    mdProperty  prop,                   // [IN] property token
    mdTypeDef   *pClass,                // [OUT] typedef containing the property declarion. 
    LPCWSTR     szProperty,             // [OUT] Property name
    ULONG       cchProperty,            // [IN] the count of wchar of szProperty
    ULONG       *pchProperty,           // [OUT] actual count of wchar for property name
    DWORD       *pdwPropFlags,          // [OUT] property flags.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob 
    ULONG       *pbSig,                 // [OUT] count of bytes in *ppvSig
    DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppDefaultValue,      // [OUT] constant value 
    ULONG       *pcchDefaultValue,      // [OUT] size of constant string in chars, 0 for non-strings.
    mdMethodDef *pmdSetter,             // [OUT] setter method of the property
    mdMethodDef *pmdGetter,             // [OUT] getter method of the property
    mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property
    ULONG       cMax,                   // [IN] size of rmdOtherMethod
    ULONG       *pcOtherMethod)         // [OUT] total number of other method of this property
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetParamProps (               // S_OK or error.
    mdParamDef  tk,                     // [IN]The Parameter.
    mdMethodDef *pmd,                   // [OUT] Parent Method token.
    ULONG       *pulSequence,           // [OUT] Parameter sequence.
    __out_ecount (cchName) LPWSTR szName, // [OUT] Put name here.
    ULONG       cchName,                // [OUT] Size of name buffer.
    ULONG       *pchName,               // [OUT] Put actual size of name here.
    DWORD       *pdwAttr,               // [OUT] Put flags here.
    DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
    UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
    ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetCustomAttributeByName (    // S_OK or error.
    mdToken     tkObj,                  // [IN] Object with Custom Attribute.
    LPCWSTR     szName,                 // [IN] Name of desired Custom Attribute.
    const void  **ppData,               // [OUT] Put pointer to data here.
    ULONG       *pcbData)               // [OUT] Put size of data here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

BOOL AssemblyMDInternalImport::IsValidToken (         // True or False.
    mdToken     tk)                     // [IN] Given token.
{
    _ASSERTE(!"NYI");
    return FALSE;
}

STDMETHODIMP AssemblyMDInternalImport::GetNestedClassProps (         // S_OK or error.
    mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
    mdTypeDef   *ptdEnclosingClass)       // [OUT] EnclosingClass token.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetNativeCallConvFromSig (    // S_OK or error.
    void const  *pvSig,                 // [IN] Pointer to signature.
    ULONG       cbSig,                  // [IN] Count of signature bytes.
    ULONG       *pCallConv)             // [OUT] Put calling conv here (see CorPinvokemap).
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::IsGlobal (                    // S_OK or error.
    mdToken     pd,                     // [IN] Type, Field, or Method token.
    int         *pbGlobal)              // [OUT] Put 1 if global, 0 otherwise.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetMethodSpecProps(
        mdMethodSpec mi,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

// *** ISNAssemblySignature methods ***

STDMETHODIMP AssemblyMDInternalImport::GetSNAssemblySignature(
    BYTE        *pbSig,                 // [IN, OUT] Buffer to write signature
    DWORD       *pcbSig)                // [IN, OUT] Size of buffer, bytes written
{
    return RuntimeGetAssemblyStrongNameHashForModule(m_pHandle, this, pbSig, pcbSig);
}


#include "strongname.h"

#ifdef FEATURE_PREJIT
// *** IGetIMDInternalImport ***
STDMETHODIMP AssemblyMDInternalImport::GetIMDInternalImport(
    IMDInternalImport ** pIMDInternalImport)
{
    m_pMDInternalImport->AddRef();
    *pIMDInternalImport = m_pMDInternalImport;
    return S_OK;
}



// ===========================================================================

class CNativeImageDependency : public INativeImageDependency
{
public:
    CNativeImageDependency(CORCOMPILE_DEPENDENCY * pDependency)
        : m_cRef(1), m_pDependency(pDependency)
    {
    }
    
    ~CNativeImageDependency()
    {
    }

    //
    // IUnknown
    //
    
    STDMETHODIMP_(ULONG) AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }

    STDMETHODIMP_(ULONG) Release()
    {
        ULONG   cRef = InterlockedDecrement(&m_cRef);
        if (!cRef)
            delete this;
        return (cRef);
    }

    STDMETHODIMP QueryInterface(REFIID riid, void **ppUnk)
    { 
        *ppUnk = 0;

        if (riid == IID_IUnknown)
            *ppUnk = (IUnknown *) (IMetaDataAssemblyImport *) this;
        else if (riid == IID_INativeImageDependency)
            *ppUnk = (INativeImageDependency *) this;
        else
            return (E_NOINTERFACE);
        AddRef();
        return (S_OK);
    }

    //
    // INativeImageDependency
    //
    
    STDMETHODIMP GetILAssemblyRef(mdAssemblyRef * pAssemblyRef)
    {
        BEGIN_ENTRYPOINT_NOTHROW;

        *pAssemblyRef = m_pDependency->dwAssemblyRef;
        END_ENTRYPOINT_NOTHROW;
        
        return S_OK;
    }

    STDMETHODIMP GetILAssemblyDef(
        mdAssemblyRef * ppAssemblyDef,
        CORCOMPILE_ASSEMBLY_SIGNATURE * pSign)
    {
        BEGIN_ENTRYPOINT_NOTHROW;

        *ppAssemblyDef = m_pDependency->dwAssemblyDef;
        *pSign = m_pDependency->signAssemblyDef;
        END_ENTRYPOINT_NOTHROW;

        return S_OK;
    }

    STDMETHODIMP GetNativeAssemblyDef(CORCOMPILE_NGEN_SIGNATURE * pNativeSign)
    {
        BEGIN_ENTRYPOINT_NOTHROW;

        *pNativeSign = m_pDependency->signNativeImage;
        END_ENTRYPOINT_NOTHROW;

        return S_OK;
    }

    STDMETHODIMP GetPEKind(PEKIND *pPEKind)
    {
        BEGIN_ENTRYPOINT_NOTHROW;

        *pPEKind = PEKIND((m_pDependency->dependencyInfo & CORCOMPILE_DEPENDENCY_PEKIND_MASK) >> CORCOMPILE_DEPENDENCY_PEKIND_SHIFT);
        END_ENTRYPOINT_NOTHROW;

        return S_OK;
    }

protected:

    LONG                        m_cRef;
    CORCOMPILE_DEPENDENCY *     m_pDependency;
};

// ===========================================================================
// *** INativeImageInstallInfo ***
// ===========================================================================

STDMETHODIMP AssemblyMDInternalImport::GetSignature(CORCOMPILE_NGEN_SIGNATURE * pNgenSign)
{
    BEGIN_ENTRYPOINT_NOTHROW;

    *pNgenSign = m_pZapVersionInfo->signature;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::GetVersionInfo(CORCOMPILE_VERSION_INFO * pVersionInfo)
{
    BEGIN_ENTRYPOINT_NOTHROW;

    *pVersionInfo = *m_pZapVersionInfo;
    END_ENTRYPOINT_NOTHROW;
    return S_OK;
}
            


STDMETHODIMP AssemblyMDInternalImport::GetILSignature(CORCOMPILE_ASSEMBLY_SIGNATURE * pILSign)
{
    BEGIN_ENTRYPOINT_NOTHROW;

    *pILSign = m_pZapVersionInfo->sourceAssembly;
    END_ENTRYPOINT_NOTHROW;
    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::GetConfigMask(DWORD * pConfigMask)
{
    BEGIN_ENTRYPOINT_NOTHROW;

    *pConfigMask = m_pZapVersionInfo->wConfigFlags;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

STDMETHODIMP AssemblyMDInternalImport::EnumDependencies (
    HCORENUM * phEnum,                  // [IN/OUT] - Pointer to the enum
    INativeImageDependency *rDeps[],    // [OUT]
    ULONG cMax,                         // Max dependancies to enumerate in this iteration
    DWORD * pdwCount                    // [OUT] - Number of dependancies actually enumerated
    )
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CORCOMPILE_DEPENDENCY * pDependenciesEnd = m_pZapDependencies + m_cZapDependencies;

    CORCOMPILE_DEPENDENCY * pNextDependency;

    // Is the enum just being initialized, or are we walking an existing one?
    if ((*phEnum) == NULL)
        pNextDependency = m_pZapDependencies;
    else
        pNextDependency = (CORCOMPILE_DEPENDENCY *)(*phEnum);

    DWORD count;
    for (count = 0;
         pNextDependency < pDependenciesEnd && count < cMax;
         count++, pNextDependency++)
    {
        CNativeImageDependency * pDep = new (nothrow) CNativeImageDependency(pNextDependency);
        IfNullGo( pDep );

        rDeps[count] = pDep;
    }
    
    *phEnum = (HCORENUM)(pNextDependency < pDependenciesEnd) ? pNextDependency : NULL;
    *pdwCount = count;

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}


STDMETHODIMP AssemblyMDInternalImport::GetDependency (
    const CORCOMPILE_NGEN_SIGNATURE *pcngenSign,  // [IN] ngenSig of dependency you want
    CORCOMPILE_DEPENDENCY           *pDep         // [OUT] matching dependency
    )
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE(pcngenSign != NULL);
    _ASSERTE(*pcngenSign != INVALID_NGEN_SIGNATURE);
    _ASSERTE(pDep != NULL);

    CORCOMPILE_DEPENDENCY * pDependenciesEnd = m_pZapDependencies + m_cZapDependencies;
    CORCOMPILE_DEPENDENCY * pNextDependency = m_pZapDependencies;
    while (pNextDependency != pDependenciesEnd)
    {
        if (pNextDependency->signNativeImage == *pcngenSign)
        {
            *pDep = *pNextDependency;
            hr = S_OK;
            goto ErrExit;
        }
        pNextDependency++;
    }
    hr = S_FALSE;

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}


#endif  // FEATURE_PREJIT


//*****************************************************************************
// IMetaDataImport2 methods
//*****************************************************************************
STDMETHODIMP AssemblyMDInternalImport::GetGenericParamProps(      // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        ULONG        *pulParamSeq,          // [OUT] Index of the type parameter
        DWORD        *pdwParamFlags,        // [OUT] Flags, for future use (e.g. variance)
        mdToken      *ptOwner,              // [OUT] Owner (TypeDef or MethodDef)
        DWORD       *reserved,              // [OUT] For future use (e.g. non-type parameters)
        __out_ecount (cchName) LPWSTR wzname, // [OUT] Put name here
        ULONG        cchName,               // [IN] Size of buffer
        ULONG        *pchName)              // [OUT] Put size of name (wide chars) here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetGenericParamConstraintProps( // S_OK or error.
        mdGenericParamConstraint gpc,       // [IN] GenericParamConstraint
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)    // [OUT] TypeDef/Ref/Spec constraint
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumGenericParams(         // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] TypeDef or MethodDef whose generic parameters are requested
        mdGenericParam rGenericParams[],    // [OUT] Put GenericParams here.
        ULONG       cMax,                   // [IN] Max GenericParams to put.
        ULONG       *pcGenericParams)       // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumGenericParamConstraints( // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdGenericParam tk,                  // [IN] GenericParam whose constraints are requested
        mdGenericParamConstraint rGenericParamConstraints[],    // [OUT] Put GenericParamConstraints here.
        ULONG       cMax,                   // [IN] Max GenericParamConstraints to put.
        ULONG       *pcGenericParamConstraints) // [OUT] Put # put here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::EnumMethodSpecs(           // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rMethodSpecs[],        // [OUT] Put MethodSpecs here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcMethodSpecs)         // [OUT] Put actual count here.
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

STDMETHODIMP AssemblyMDInternalImport::GetPEKind(         // S_OK or error.
    DWORD* pdwPEKind,           // [OUT] The kind of PE (0 - not a PE)
    DWORD* pdwMachine)          // [OUT] Machine as defined in NT header
{
    HRESULT hr = S_OK;
    if(pdwPEKind) *pdwPEKind = m_dwPEKind;
    if(pdwMachine) *pdwMachine = m_dwMachine;
    return hr;
}

STDMETHODIMP AssemblyMDInternalImport::GetVersionString(    // S_OK or error.
        __out_ecount (ccBufSize) LPWSTR pwzBuf, // Put version string here.
        DWORD ccBufSize,              // [in] size of the buffer, in wide chars
        DWORD *pccBufSize)           // [out] Size of the version string, wide chars, including terminating nul.
{
    HRESULT hr=S_OK;
    DWORD   L = WszMultiByteToWideChar(CP_UTF8,0,m_szVersionString,-1,pwzBuf,ccBufSize);
    if(ccBufSize < L)
        hr = HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW);

    if(pccBufSize) *pccBufSize = L;
    return hr;
}

#endif //!DACCESS_COMPILE

#endif // FEATURE_FUSION

#endif //FEATURE_METADATA_INTERNAL_APIS
