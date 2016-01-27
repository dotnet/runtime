// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
HRESULT RefClassWriter::Init(ICeeGen *pCeeGen, IUnknown *pUnk, LPCWSTR szName)
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
    m_pCeeFileGen = NULL;
    m_ceeFile = NULL;
    m_ulResourceSize = 0;
    m_tkFile = mdFileNil;

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

    if (m_pOnDiskEmitter) {
        m_pOnDiskEmitter->Release();
        m_pOnDiskEmitter = NULL;
    }


#ifndef FEATURE_CORECLR
    DestroyCeeFileGen();
#endif // FEATURE_CORECLR
}

#ifndef FEATURE_CORECLR

#include <MscorpeSxSWrapper.h>

// Loads mscorpe.dll (uses shim hosting API)
HRESULT 
LoadMscorpeDll(HMODULE * phModule)
{
    // Load SxS version of mscorpe.dll (i.e. mscorpehost.dll) and initialize it
    return g_pCLRRuntime->LoadLibrary(W("mscorpe.dll"), phModule);
}

// Wrapper for mscorpe.dll calls
typedef MscorpeSxSWrapper<LoadMscorpeDll> MscorpeSxS;

//******************************************************
//*
//* Make sure that CeeFileGen for this module is created for emitting to disk
//*
//******************************************************
HRESULT 
RefClassWriter::EnsureCeeFileGenCreated(
    DWORD corhFlags, 
    DWORD peFlags)
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_TRIGGERS;
        // we know that the com implementation is ours so we use mode-any to simplify
        // having to switch mode 
        MODE_ANY; 
        INJECT_FAULT(CONTRACT_RETURN(E_OUTOFMEMORY));
        
        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_pCeeFileGen) : (int)(m_pCeeFileGen == NULL));
        POSTCONDITION(SUCCEEDED(RETVAL) ? CheckPointer(m_ceeFile) : (int)(m_pCeeFileGen == NULL));
    }
    CONTRACT_END;

    HRESULT hr = NOERROR;

    if (m_pCeeFileGen == NULL)
    {
        EX_TRY
        {
            IfFailGo(MscorpeSxS::CreateICeeFileGen(&m_pCeeFileGen));

            IfFailGo(m_pCeeFileGen->CreateCeeFileFromICeeGen(m_pCeeGen, &m_ceeFile, peFlags));

            IfFailGo(m_pCeeFileGen->ClearComImageFlags(m_ceeFile, COMIMAGE_FLAGS_ILONLY));

            IfFailGo(m_pCeeFileGen->SetComImageFlags(m_ceeFile, corhFlags));
        ErrExit:
            ;
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
        
        if (FAILED(hr))
        {
            DestroyCeeFileGen();
        }
    }    

    RETURN(hr);
} // RefClassWriter::EnsureCeeFileGenCreated


//******************************************************
//*
//* Destroy the instance of CeeFileGen that we created
//*
//******************************************************
HRESULT RefClassWriter::DestroyCeeFileGen()
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_TRIGGERS;
        // we know that the com implementation is ours so we use mode-any to simplify
        // having to switch mode 
        MODE_ANY; 
        FORBID_FAULT;
        
        POSTCONDITION(m_pCeeFileGen == NULL);
        POSTCONDITION(m_ceeFile == NULL);
    }
    CONTRACT_END;

    HRESULT hr = NOERROR;

    if (m_pCeeFileGen != NULL)
    {
        //Cleanup the HCEEFILE.  
        if (m_ceeFile != NULL)
        {
            hr = m_pCeeFileGen->DestroyCeeFile(&m_ceeFile);
            _ASSERTE_MSG(SUCCEEDED(hr), "Destory CeeFile");
            m_ceeFile = NULL;
        }

        //Cleanup the ICeeFileGen.
        {
            CONTRACT_VIOLATION(ThrowsViolation);

            // code:EnsureCeeFileGenCreated already loaded the DLL
            _ASSERTE(MscorpeSxS::Debug_IsLoaded());

            hr = MscorpeSxS::DestroyICeeFileGen(&m_pCeeFileGen);
        }
        _ASSERTE_MSG(SUCCEEDED(hr), "Destroy ICeeFileGen");
        m_pCeeFileGen = NULL;
    }

    RETURN(hr);
} // RefClassWriter::DestroyCeeFileGen

#endif //!FEATURE_CORECLR
