// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"
#include "reflectclasswriter.h"

//******************************************************
//*
//* constructor for RefClassWriter
//*
//******************************************************
HRESULT RefClassWriter::Init(ICeeGenInternal *pCeeGen, IUnknown *pUnk, LPCWSTR szName)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        // we know that the com implementation is ours so we use mode-any to simplify
        // having to switch mode
        MODE_ANY;
        INJECT_FAULT(return(E_OUTOFMEMORY));

        PRECONDITION(CheckPointer(pCeeGen));
        PRECONDITION(CheckPointer(pUnk));

    }
    CONTRACTL_END;

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
        return(hr);

    hr = pUnk->QueryInterface(IID_IMetaDataImport, (void**)&m_importer);
    if (FAILED(hr))
        return(hr);

    hr = pUnk->QueryInterface(IID_IMetaDataEmitHelper, (void**)&m_pEmitHelper);
    if (FAILED(hr))
        return(hr);

    hr = GetMDInternalInterfaceFromPublic(pUnk, IID_IMDInternalImport, (void**)&m_internalimport);
    if (FAILED(hr))
        return(hr);

    // <TODO> We will need to set this at some point.</TODO>
    hr = m_emitter->SetModuleProps(szName);
    if (FAILED(hr))
        return(hr);

    return(S_OK);
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
