// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"
#include "reflectclasswriter.h"

// Forward declaration.
STDAPI  GetMetaDataInternalInterfaceFromPublic(
    IUnknown    *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface
    void        **ppv);                 // [OUT] returned interface

//******************************************************
//*
//* constructor for RefClassWriter
//*
//******************************************************
HRESULT RefClassWriter::Init(ICeeGenInternal *pCeeGen, IUnknown *pUnk, LPCWSTR szName)
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        // we know that the com implementation is ours so we use mode-any to simplify
        // having to switch mode
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN(E_OUTOFMEMORY));

        PRECONDITION(CheckPointer(pCeeGen));
        PRECONDITION(CheckPointer(pUnk));

        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_emitter) : TRUE);
        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_importer) : TRUE);
        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_pEmitHelper) : TRUE);
        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_internalimport) : TRUE);
    }
    CONTRACT_END;

    // Initialize the Import and Emitter interfaces
    m_emitter = NULL;
    m_importer = NULL;
    m_internalimport = NULL;
    m_ulResourceSize = 0;

    m_pCeeGen = pCeeGen;
    pCeeGen->AddRef();

    // Get the interfaces
    HRESULT hr = pUnk->QueryInterface(IID_IMetaDataEmit2, (void**)&m_emitter);
    if (FAILED(hr))
        RETURN(hr);

    hr = pUnk->QueryInterface(IID_IMetaDataImport, (void**)&m_importer);
    if (FAILED(hr))
        RETURN(hr);

    hr = pUnk->QueryInterface(IID_IMetaDataEmitHelper, (void**)&m_pEmitHelper);
    if (FAILED(hr))
        RETURN(hr);

    hr = GetMetaDataInternalInterfaceFromPublic(pUnk, IID_IMDInternalImport, (void**)&m_internalimport);
    if (FAILED(hr))
        RETURN(hr);

    // <TODO> We will need to set this at some point.</TODO>
    hr = m_emitter->SetModuleProps(szName);
    if (FAILED(hr))
        RETURN(hr);

    RETURN(S_OK);
}


//******************************************************
//*
//* destructor for RefClassWriter
//*
//******************************************************
RefClassWriter::~RefClassWriter()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        // we know that the com implementation is ours so we use mode-any to simplify
        // having to switch mode
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (m_emitter) {
        m_emitter->Release();
    }

    if (m_importer) {
        m_importer->Release();
    }

    if (m_pEmitHelper) {
        m_pEmitHelper->Release();
    }

    if (m_internalimport) {
        m_internalimport->Release();
    }

    if (m_pCeeGen) {
        m_pCeeGen->Release();
        m_pCeeGen = NULL;
    }
}
