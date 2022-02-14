// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#include "sourceline.h"

//////////////////////////////////////////////////////////////////

#ifdef ENABLE_DIAGNOSTIC_SYMBOL_READING

class CCallback : public IDiaLoadCallback
{
    int m_nRefCount;
public:
    CCallback() {
        CONTRACTL
        {
            MODE_ANY;
            GC_NOTRIGGER;
            NOTHROW;
        }CONTRACTL_END;

        m_nRefCount = 0;
    }

    //IUnknown
    ULONG STDMETHODCALLTYPE AddRef() {
        LIMITED_METHOD_CONTRACT;
        m_nRefCount++;
        return m_nRefCount;
    }

    ULONG STDMETHODCALLTYPE Release() {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        if ( (--m_nRefCount) == 0 )
            delete this;

        return m_nRefCount;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface( REFIID rid, void **ppUnk ) {
        WRAPPER_NO_CONTRACT;
        if ( ppUnk == NULL ) {
            return E_INVALIDARG;
        }
        if (rid == __uuidof( IDiaLoadCallback ) )
            *ppUnk = (IDiaLoadCallback *)this;
        else if (rid == __uuidof( IDiaLoadCallback ) )
            *ppUnk = (IDiaLoadCallback *)this;
        else if (rid == __uuidof( IUnknown ) )
            *ppUnk = (IUnknown *)this;
        else
            *ppUnk = NULL;

        if ( *ppUnk != NULL ) {
            AddRef();
            return S_OK;
        }

        return E_NOINTERFACE;
    }

    HRESULT STDMETHODCALLTYPE NotifyDebugDir(
        BOOL fExecutable,
        DWORD cbData,
        BYTE data[]) // really a const struct _IMAGE_DEBUG_DIRECTORY *
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE NotifyOpenDBG(
        LPCOLESTR dbgPath,
        HRESULT resultCode)
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE NotifyOpenPDB(
        LPCOLESTR pdbPath,
        HRESULT resultCode)
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RestrictRegistryAccess()         // return hr != S_OK to prevent querying the registry for symbol search paths
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RestrictSymbolServerAccess()     // return hr != S_OK to prevent accessing a symbol server
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RestrictOriginalPathAccess()     // return hr != S_OK to prevent querying the registry for symbol search paths
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RestrictReferencePathAccess()    // return hr != S_OK to prevent accessing a symbol server
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RestrictDBGAccess()
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }
};

//////////////////////////////////////////////////////////////////

bool SourceLine::LoadDataFromPdb( _In_z_ LPWSTR wszFilename )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hResult;

//  CComPtr(IDiaDataSource) pDataSource;

    hResult = CoInitialize(NULL);

    if (FAILED(hResult)){
        return FALSE;
    }

    // Obtain Access To The Provider
    hResult = CoCreateInstance(CLSID_DiaSource,
        NULL,
        CLSCTX_INPROC_SERVER,
        IID_IDiaDataSource,
        (void **) &pSource_);

    if (FAILED(hResult)){
        return FALSE;
    }

    CCallback callback;
    callback.AddRef();

    if ( FAILED( pSource_->loadDataFromPdb( wszFilename ) )
        && FAILED( pSource_->loadDataForExe( wszFilename, W("symsrv*symsrv.dll*\\\\symbols\\\\symbols"), &callback ) ) )
        return FALSE;
    if ( FAILED( pSource_->openSession(&pSession_) ) )
        return FALSE;
    if ( FAILED( pSession_->get_globalScope(&pGlobal_) ) )
        return FALSE;

    return TRUE;
}

//////////////////////////////////////////////////////////////////

SourceLine::SourceLine( _In_z_ LPWSTR pszFileName )
{
    WRAPPER_NO_CONTRACT;
    if (LoadDataFromPdb(pszFileName)) {
        initialized_ = true;
    }
    else{
        initialized_ = false;
    }
}

//////////////////////////////////////////////////////////////////

HRESULT SourceLine::GetSourceLine( DWORD dwFunctionToken, DWORD dwOffset, _Out_writes_z_(dwFileNameMaxLen) LPWSTR pszFileName, DWORD dwFileNameMaxLen, PDWORD pdwLineNumber )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(initialized_);

    CComPtr(IDiaSymbol) pSymbol;
    HRESULT hResult = pSession_->findSymbolByToken(dwFunctionToken, SymTagFunction, &pSymbol);

    if( SUCCEEDED(hResult) && pSymbol != NULL) {

        ULONGLONG length;
        pSymbol->get_length(&length);

        DWORD rva;
        CComPtr(IDiaEnumLineNumbers) pLines;

        if(SUCCEEDED(pSymbol->get_relativeVirtualAddress(&rva))) {

            DWORD initialOffset;
            pSymbol->get_addressOffset(&initialOffset);

            DWORD isect;
            pSymbol->get_addressSection(&isect);

            hResult = pSession_->findLinesByAddr(isect, initialOffset+dwOffset, 1, &pLines);
            if( SUCCEEDED(hResult) ){

                CComPtr(IDiaLineNumber) pLine;

                hResult = pLines->Item( 0, &pLine );

                if(SUCCEEDED(hResult)){

                    pLine->get_lineNumber(pdwLineNumber);

                    CComPtr(IDiaSourceFile) pSourceFile;
                    pLine->get_sourceFile( &pSourceFile );

                    BSTR sourceName;
                    pSourceFile->get_fileName( &sourceName );

                    wcsncpy_s( pszFileName, dwFileNameMaxLen, sourceName, dwFileNameMaxLen );
                }
            }
        }
    }

    return hResult;
}

//////////////////////////////////////////////////////////////////

HRESULT SourceLine::GetLocalName( DWORD dwFunctionToken, DWORD dwSlot, _Out_writes_z_(dwNameMaxLen) LPWSTR pszName, DWORD dwNameMaxLen )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    CComPtr(IDiaSymbol) pSymbol;
    HRESULT hResult = pSession_->findSymbolByToken(dwFunctionToken, SymTagFunction, &pSymbol);

    if( SUCCEEDED(hResult) && pSymbol != NULL ) {

        ULONGLONG length;
        pSymbol->get_length(&length);

        DWORD rva;
//      CComPtr(IDiaEnumLineNumbers) pLines;

        hResult = pSymbol->get_relativeVirtualAddress(&rva);

        if(SUCCEEDED(hResult)) {

            CComPtr( IDiaSymbol ) pBlock;
            hResult = pSession_->findSymbolByRVA( rva, SymTagBlock, &pBlock );

            if( SUCCEEDED(hResult) && pBlock != NULL ) {

                ULONG celt = 0;

                CComPtr(IDiaSymbol) pLocalSymbol = NULL;
                CComPtr( IDiaEnumSymbols ) pEnum;
                hResult = pBlock->findChildren( SymTagData, NULL, nsNone, &pEnum );

                if( SUCCEEDED(hResult) ) {

                    //
                    // Find function local by slot
                    //
                    while (SUCCEEDED(hResult = pEnum->Next(1, &pLocalSymbol, &celt)) && celt == 1) {

                        DWORD dwThisSlot;
                        pLocalSymbol->get_slot( &dwThisSlot );

                        if( dwThisSlot == dwSlot ) {

                            BSTR name = NULL;
                            hResult = pLocalSymbol->get_name(&name);

                            wcsncpy_s( pszName, dwNameMaxLen, name, _TRUNCATE );

                            return S_OK;
                        }

                        pLocalSymbol = 0;
                    }
                }
            }
        }
    }

    return hResult;
}

#else // !ENABLE_DIAGNOSTIC_SYMBOL_READING
SourceLine::SourceLine( _In_z_ LPWSTR pszFileName )
{
    LIMITED_METHOD_CONTRACT;
    initialized_ = false;
}

HRESULT SourceLine::GetSourceLine( DWORD dwFunctionToken, DWORD dwOffset, _Out_writes_z_(dwFileNameMaxLen) LPWSTR pszFileName, DWORD dwFileNameMaxLen, PDWORD pdwLineNumber )
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}

HRESULT SourceLine::GetLocalName( DWORD dwFunctionToken, DWORD dwSlot, _Out_writes_z_(dwNameMaxLen) LPWSTR pszName, DWORD dwNameMaxLen )
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}
#endif // ENABLE_DIAGNOSTIC_SYMBOL_READING

