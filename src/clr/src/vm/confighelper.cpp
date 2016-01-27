// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// ConfigHelper.cpp
// 
//*****************************************************************************
//
// XML Helper so that NodeFactory can be implemented in Managed code  
//



#include "common.h"

#include "confighelper.h"

ConfigFactory::ConfigFactory(OBJECTREF *pFactory)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(pFactory != NULL);
    Initialize(pFactory);
    AddRef();
}

HRESULT STDMETHODCALLTYPE ConfigFactory::NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return COR_E_STACKOVERFLOW)

    EX_TRY
    {
        GetNotifyEventFunctionality()(iEvt);
    }
    EX_CATCH_HRESULT(hr);

    END_SO_INTOLERANT_CODE

    return hr;
}

//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE ConfigFactory::BeginChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return COR_E_STACKOVERFLOW)
    EX_TRY
    {
        GetBeginChildrenFunctionality()(pNodeInfo->dwSize, 
                                        pNodeInfo->dwSubType, 
                                        pNodeInfo->dwType, 
                                        pNodeInfo->fTerminal, 
                                        pNodeInfo->pwcText, 
                                        pNodeInfo->ulLen, 
                                        pNodeInfo->ulNsPrefixLen);
    }
    EX_CATCH_HRESULT(hr);

    END_SO_INTOLERANT_CODE
    
    return hr;

}

//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE ConfigFactory::EndChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ BOOL fEmptyNode,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return COR_E_STACKOVERFLOW)
    
    EX_TRY
    {
        GetEndChildrenFunctionality()(fEmptyNode, 
                                      pNodeInfo->dwSize, 
                                      pNodeInfo->dwSubType, 
                                      pNodeInfo->dwType, 
                                      pNodeInfo->fTerminal, 
                                      pNodeInfo->pwcText, 
                                      pNodeInfo->ulLen, 
                                      pNodeInfo->ulNsPrefixLen);
    }
    EX_CATCH_HRESULT(hr);

    END_SO_INTOLERANT_CODE
    
    return hr;
}

//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE ConfigFactory::CreateNode(
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ PVOID pNode,
    /* [in] */ USHORT cNumRecs,
    /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), return COR_E_STACKOVERFLOW)

    EX_TRY
    {
        DWORD i;
        WCHAR wstr[128]; 
        WCHAR *pString = wstr;
        DWORD dwString = sizeof(wstr)/sizeof(WCHAR);
        
        for( i = 0; i < cNumRecs; i++) { 
            if ( apNodeInfo[i]->ulLen >= dwString) {
                dwString = apNodeInfo[i]->ulLen+1;
                if(pString != wstr) delete [] pString;
                pString = new(nothrow) WCHAR[dwString];
                IfNullGo(pString);
            }

            pString[apNodeInfo[i]->ulLen] = W('\0');
            wcsncpy_s(pString, dwString, apNodeInfo[i]->pwcText, apNodeInfo[i]->ulLen);

            if(i == 0) {
                GetCreateNodeFunctionality()(apNodeInfo[i]->dwSize, 
                                             apNodeInfo[i]->dwSubType, 
                                             apNodeInfo[i]->dwType, 
                                             apNodeInfo[i]->fTerminal, 
                                             pString, 
                                             apNodeInfo[i]->ulLen, 
                                             apNodeInfo[i]->ulNsPrefixLen);
            }
            else {
                GetCreateAttributeFunctionality()(apNodeInfo[i]->dwSize, 
                                                  apNodeInfo[i]->dwSubType, 
                                                  apNodeInfo[i]->dwType, 
                                                  apNodeInfo[i]->fTerminal, 
                                                  pString, 
                                                  apNodeInfo[i]->ulLen, 
                                                  apNodeInfo[i]->ulNsPrefixLen);
            }

            if (FAILED(hr))
                break;
        }
        if(pString != wstr) delete [] pString;
ErrExit:;
    }
    EX_CATCH_HRESULT(hr);

    END_SO_INTOLERANT_CODE
    return hr;
}



STDAPI GetXMLObjectEx(IXMLParser **ppv);

//
//Helper routines to call into managed Node Factory
//

HRESULT ConfigNative::RunInternal(OBJECTREF *pFactory, LPCWSTR filename)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT        hr = S_OK;  
    SafeComHolder<IXMLParser> pIXMLParser;
    SafeComHolder<ConfigFactory>  helperfactory; 
    SafeComHolder<IStream> pFile; 
    if (!pFactory){
        return E_POINTER;
    }

    hr = CreateConfigStreamHelper(filename,&pFile);
    if(FAILED(hr))
        return hr;
    
    hr = GetXMLObjectEx(&pIXMLParser);
    if(FAILED(hr))
        return hr;

    helperfactory = new (nothrow)ConfigFactory(pFactory);  // RefCount = 1 
    if ( ! helperfactory) { 
        return E_OUTOFMEMORY; 
    }

    hr = pIXMLParser->SetInput(pFile); // filestream's RefCount=2
    if(FAILED(hr))
        return hr;

    hr = pIXMLParser->SetFactory(helperfactory); // factory's RefCount=2
    if(FAILED(hr))
        return hr;

    // On X86, we emit a call to LogUMTransition which needs us to be in preemptive GC mode
    // Since we are done dealing with REF's after the call to ConfigFactory constructor,
    // it is safe to switch to preemptive mode here
    {
        GCX_PREEMP();
        hr = pIXMLParser->Run(-1);
    }

    if (hr== (HRESULT) XML_E_MISSINGROOT)  //empty file
        hr=S_OK;
    return hr;
}

//
// Entrypoint to return an Helper interface which Managed code can call to build managed Node factory
//

FCIMPL2(void, ConfigNative::RunParser, Object* refHandlerUNSAFE, StringObject* strFileNameUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF  refHandler = (OBJECTREF) refHandlerUNSAFE;
    STRINGREF  strFileName = (STRINGREF) strFileNameUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(refHandler, strFileName);

    HRESULT     hr;
    WCHAR*      pString;
    int         iString;
    LPWSTR      pFileName;
    CQuickBytes qb;

    if (refHandler == NULL) {
        COMPlusThrowArgumentNull(W("handler"));
    }

    if (strFileName == NULL) {
        COMPlusThrowArgumentNull(W("fileName"));
    }

    //Get string data.
    strFileName->RefInterpretGetStringValuesDangerousForGC(&pString, &iString);

    S_UINT32 bufSize = (S_UINT32(iString) + S_UINT32(1)) * S_UINT32(sizeof(WCHAR));
    _ASSERTE(!bufSize.IsOverflow());
    if(bufSize.IsOverflow())
    {
        ThrowWin32(ERROR_ARITHMETIC_OVERFLOW);
    }

    pFileName = (LPWSTR) qb.AllocThrows(bufSize.Value());
    memcpy(pFileName, pString, bufSize.Value());

    hr = RunInternal(&refHandler, pFileName);

    if (FAILED(hr))
        COMPlusThrowHR(hr);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
