// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: symbinder.cpp
//

// ===========================================================================

#include "pch.h"
#include "symbinder.h"

/* ------------------------------------------------------------------------- *
 * SymBinder class
 * ------------------------------------------------------------------------- */

HRESULT
SymBinder::QueryInterface(
    REFIID riid,
    void **ppvObject
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(IsValidIID(riid));
    _ASSERTE(IsValidWritePtr(ppvObject, void*));

    IfFalseGo( ppvObject, E_INVALIDARG );

    if (riid == IID_ISymUnmanagedBinder)
    {
        *ppvObject = (ISymUnmanagedBinder*) this;
    }
    else if (riid == IID_ISymUnmanagedBinder2)
    {
        *ppvObject = (ISymUnmanagedBinder2*) this;
    }
    else if (riid == IID_IUnknown)
    {
        *ppvObject = (IUnknown*)this;
    }
    else
    {
        *ppvObject = NULL;
        hr = E_NOINTERFACE;
    }

    if (*ppvObject)
    {
        AddRef();
    }

ErrExit:

    return hr;
}

HRESULT
SymBinder::NewSymBinder(
    REFCLSID clsid,
    void** ppObj
    )
{
    HRESULT hr = S_OK;
    SymBinder* pSymBinder = NULL;

    _ASSERTE(IsValidCLSID(clsid));
    _ASSERTE(IsValidWritePtr(ppObj, IUnknown*));

    if (clsid != IID_ISymUnmanagedBinder)
        return (E_UNEXPECTED);

    IfFalseGo( ppObj, E_INVALIDARG );

    *ppObj = NULL;

    IfNullGo( pSymBinder = NEW(SymBinder()) );
    *ppObj = pSymBinder;
    pSymBinder->AddRef();
    pSymBinder = NULL;

ErrExit:

    RELEASE( pSymBinder );

    return hr;
}

//-----------------------------------------------------------
// GetReaderForFile
//-----------------------------------------------------------
HRESULT
SymBinder::GetReaderForFile(
    IUnknown *importer,             // IMetaDataImporter
    const WCHAR *fileName,          // File we're looking symbols for
    const WCHAR *searchPath,        // Search path for file
    ISymUnmanagedReader **ppRetVal) // Out: SymReader for file
{
    HRESULT hr = S_OK;
    ISymUnmanagedReader *pSymReader = NULL;
    IfFalseGo( ppRetVal && fileName && fileName[0] != '\0', E_INVALIDARG );

    // Init Out parameter
    *ppRetVal = NULL;

    // Call the class factory directly.
    IfFailGo(IldbSymbolsCreateInstance(CLSID_CorSymReader_SxS,
                                       IID_ISymUnmanagedReader,
                                       (void**)&pSymReader));

    IfFailGo(pSymReader->Initialize(importer, fileName, searchPath, NULL));

    // Transfer ownership to the out parameter
    *ppRetVal = pSymReader;
    pSymReader = NULL;

ErrExit:
    RELEASE(pSymReader);
    return hr;
}

HRESULT
SymBinder::GetReaderFromStream(
    IUnknown *importer,
    IStream *pStream,
    ISymUnmanagedReader **ppRetVal
    )
{
    HRESULT hr = S_OK;
    ISymUnmanagedReader *pSymReader = NULL;
    IfFalseGo( ppRetVal && importer && pStream, E_INVALIDARG );

    // Init Out parameter
    *ppRetVal = NULL;

    // Call the class factory directly
    IfFailGo(IldbSymbolsCreateInstance(CLSID_CorSymReader_SxS,
                                       IID_ISymUnmanagedReader,
                                       (void**)&pSymReader));

    IfFailGo(pSymReader->Initialize(importer, NULL, NULL, pStream));

    // Transfer ownership to the out parameter
    *ppRetVal = pSymReader;
    pSymReader = NULL;

ErrExit:
    RELEASE(pSymReader);
    return hr;
}

HRESULT SymBinder::GetReaderForFile2(
    IUnknown *importer,
    const WCHAR *fileName,
    const WCHAR *searchPath,
    ULONG32 searchPolicy,
    ISymUnmanagedReader **pRetVal)
{
    // This API exists just to allow VS to function properly.
    // ILDB doesn't support any search policy or search path - we only look
    // next to the image file.
    return GetReaderForFile(importer, fileName, searchPath, pRetVal);
}
