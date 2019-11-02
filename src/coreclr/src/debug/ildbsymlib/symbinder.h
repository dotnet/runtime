// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: SymBinder.h
//

// ===========================================================================

#ifndef SYMBINDER_H_
#define SYMBINDER_H_

/* ------------------------------------------------------------------------- *
 * SymBinder class
 * ------------------------------------------------------------------------- */

class SymBinder : ISymUnmanagedBinder2
{
// ctor/dtor
public:
    SymBinder()
    {
    m_refCount = 0;
    }

    virtual ~SymBinder() {}

    static HRESULT NewSymBinder( REFCLSID clsid, void** ppObj );

// IUnknown methods
public:

    //-----------------------------------------------------------
    // IUnknown support
    //-----------------------------------------------------------
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return (InterlockedIncrement((LONG *) &m_refCount));
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        LONG refCount = InterlockedDecrement((LONG *) &m_refCount);
        if (refCount == 0)
            DELETE(this);

        return (refCount);
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject);

    // ISymUnmanagedBinder
public:

    STDMETHOD(GetReaderForFile)( IUnknown *importer,
                                 const WCHAR *fileName,
                                 const WCHAR *searchPath,
                                 ISymUnmanagedReader **pRetVal);
    STDMETHOD(GetReaderFromStream)(IUnknown *importer,
                    IStream *pstream,
                    ISymUnmanagedReader **pRetVal);

    // ISymUnmanagedBinder2
    STDMETHOD(GetReaderForFile2)( IUnknown *importer,
                                  const WCHAR *fileName,
                                  const WCHAR *searchPath,
                                  ULONG32 searchPolicy,
                                  ISymUnmanagedReader **pRetVal);

private:
    SIZE_T      m_refCount;

};
#endif
