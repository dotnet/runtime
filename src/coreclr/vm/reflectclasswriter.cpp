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
HRESULT RefClassWriter::Init(
    IMDInternalEmit *pEmitter,
    IMDInternalImport *pInternalImport,
    LPCWSTR szName)
{
    CONTRACTL {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pEmitter));
        PRECONDITION(CheckPointer(pInternalImport));
    }
    CONTRACTL_END;

    // Initialize the Import and Emitter interfaces
    m_emitter = NULL;
    m_internalimport = NULL;
    m_ulResourceSize = 0;

    m_emitter = pEmitter;
    m_emitter->AddRef();
    m_internalimport = pInternalImport;
    m_internalimport->AddRef();

    // <TODO> We will need to set this at some point.</TODO>
    HRESULT hr = m_emitter->SetModuleProps(szName);
    if (FAILED(hr))
        return hr;

    _ASSERTE(m_emitter != nullptr);
    _ASSERTE(m_internalimport != nullptr);
    return S_OK;
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
    }
    CONTRACTL_END;

    if (m_emitter) {
        m_emitter->Release();
    }

    if (m_internalimport) {
        m_internalimport->Release();
    }
}
