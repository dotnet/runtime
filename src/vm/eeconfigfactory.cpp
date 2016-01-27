// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// EEConfigFactory.cpp
//

//
// Factory used to with the XML parser to read configuration files
//

#include "common.h"
#include "ngenoptout.h"
#include "eeconfigfactory.h"


#define ISWHITE(ch) ((ch) >= 0x09 && (ch) <= 0x0D || (ch) == 0x20)

#define CONST_STRING_AND_LEN(str) str, NumItems(str)-1


int EEXMLStringCompare(const WCHAR *pStr1, 
                    DWORD cchStr1, 
                    const WCHAR *pStr2, 
                    DWORD cchStr2)
{
    LIMITED_METHOD_CONTRACT;
    if (cchStr1 != cchStr2)
        return -1;

    return wcsncmp(pStr1, pStr2, cchStr1);
}// EEXMLStringCompare


int EEXMLStringComparei(const WCHAR *pStr1, 
                    DWORD cchStr1, 
                    const WCHAR *pStr2, 
                    DWORD cchStr2)
{
    WRAPPER_NO_CONTRACT;
    if (cchStr1 != cchStr2)
        return -1;

    return SString::_wcsnicmp(pStr1, pStr2, cchStr1);
}// EEXMLStringCompare



EEConfigFactory::EEConfigFactory(
    ConfigStringHashtable* pTable,
    LPCWSTR pString,
    ParseCtl parseCtl) 
{
    LIMITED_METHOD_CONTRACT;
    m_pTable = pTable;
    m_pVersion = pString;
    m_dwDepth = 0;
    m_fUnderRuntimeElement = FALSE;
    m_fDeveloperSettings = FALSE;
    m_fVersionedRuntime= FALSE;
    m_fOnEnabledAttribute = FALSE;
    m_fOnValueAttribute = FALSE;
    m_pCurrentRuntimeElement = m_pBuffer;
    m_dwCurrentRuntimeElement = 0;
    m_dwSize = CONFIG_KEY_SIZE;
    m_parseCtl = parseCtl;
    m_pActiveFactory = NULL;
}

EEConfigFactory::~EEConfigFactory() 
{
    LIMITED_METHOD_CONTRACT;
    DeleteKey();
}

HRESULT STDMETHODCALLTYPE EEConfigFactory::NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt)
{
    LIMITED_METHOD_CONTRACT;
    if(iEvt == XMLNF_ENDDOCUMENT) {
        // <TODO> add error handling.</TODO>
    }
    if(m_pActiveFactory != NULL)
        return m_pActiveFactory->NotifyEvent(pSource, iEvt);    
    
    return S_OK;
}
//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE EEConfigFactory::BeginChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    LIMITED_METHOD_CONTRACT;

    m_dwDepth++;
    if(m_pActiveFactory != NULL)
        return m_pActiveFactory->BeginChildren(pSource, pNodeInfo);    
    return S_OK;

}
//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE EEConfigFactory::EndChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ BOOL fEmptyNode,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    LIMITED_METHOD_CONTRACT;
    if ( fEmptyNode ) { 
        m_fDeveloperSettings = FALSE;        
    }
    else {
        m_dwDepth--;
    }

    if (m_pActiveFactory != NULL)
    {
        HRESULT hr = S_OK;
        IfFailRet(m_pActiveFactory->EndChildren(pSource, fEmptyNode, pNodeInfo));


        if(m_dwDepth == 2) // when generalizing: use the current active factory depth
        {
            m_pActiveFactory = NULL;
        }
        
    }

    if (m_fUnderRuntimeElement && wcscmp(pNodeInfo->pwcText, W("runtime")) == 0) {
        m_fUnderRuntimeElement = FALSE;
        m_fVersionedRuntime = FALSE;
        ClearKey();
        // CLR_STARTUP_OPT:
        // Early out if we only need to read <runtime> section.
        //
        if (m_parseCtl == stopAfterRuntimeSection)
            pSource->Abort(NULL/*unused*/);
    }
    
    return S_OK;
}
//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE EEConfigFactory::CreateNode( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ PVOID pNode,
    /* [in] */ USHORT cNumRecs,
    /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;
    
    if(m_pActiveFactory != NULL)
        return m_pActiveFactory->CreateNode(pSource, pNode, cNumRecs, apNodeInfo);    

    if(m_dwDepth > 3)
    {
        
        return S_OK;
    }

    HRESULT hr = S_OK;
    DWORD  dwStringSize = 0;
    WCHAR* pszString = NULL;
    DWORD  i; 
    BOOL   fRuntimeKey = FALSE;
    BOOL   fVersion = FALSE;

    for( i = 0; i < cNumRecs; i++) { 
        CONTRACT_VIOLATION(ThrowsViolation); // Lots of stuff in here throws!
        
        if(apNodeInfo[i]->dwType == XML_ELEMENT ||
           apNodeInfo[i]->dwType == XML_ATTRIBUTE ||
           apNodeInfo[i]->dwType == XML_PCDATA) {

            dwStringSize = apNodeInfo[i]->ulLen;
            pszString = (WCHAR*) apNodeInfo[i]->pwcText;
            // Trim the value

            // we should never decrement lgth if it's 0, because it's unsigned

            for(;*pszString && ISWHITE(*pszString) && dwStringSize>0; pszString++, dwStringSize--);
            while( dwStringSize > 0 && ISWHITE(pszString[dwStringSize-1]))
                   dwStringSize--;

            // NOTE: pszString is not guaranteed to be null terminated. Use EEXMLStringCompare to do
            // string comparisions on it

            switch(apNodeInfo[i]->dwType) {
            case XML_ELEMENT : 
                fRuntimeKey = FALSE;
                ClearKey();
                
                if (m_dwDepth == 1 && EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("runtime"))) == 0) {
                    m_fUnderRuntimeElement = TRUE;
                    fRuntimeKey = TRUE;
                }
    
                if(m_dwDepth == 2 && m_fUnderRuntimeElement) {
                    
                    // Developer settings can look like
                    // <runtime>
                    //     <developerSettings installationVersion="v2.0.40223.0" />
                    //
                    // or
                    //
                    //     <developmentMode developerInstallation="true" />
                    //
                    // Neither one is your standard config setting.
                    if (!EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("developerSettings"))) ||
                        !EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("developmentMode"))))
                    {
                        m_fDeveloperSettings = TRUE;
                    }
                    else
                    // when generalizing: use map of (string, depth) -> class
                    if (!EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("disableNativeImageLoad"))))
                    {
                        m_pActiveFactory = new NativeImageOptOutConfigFactory();
                        m_pActiveFactory->AddRef();
                    }
                    else
                    {
                        // This is a standard element under the runtime node.... it could look like this
                        // <runtime>
                        //     <pszString enabled="1" />

                        hr = CopyToKey(pszString, dwStringSize);
                        if(FAILED(hr)) return hr;
                    }
                }
                // If our depth isn't 2, and we're not under the runtime element....
                else
                    ClearKey();

                break ;     
                
            case XML_ATTRIBUTE : 
                if(fRuntimeKey && EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("version"))) == 0) {
                    fVersion = TRUE;
                }
                else 
                {
                    if (m_dwDepth == 2 && m_fUnderRuntimeElement)
                    {
                        if (!m_fDeveloperSettings)
                        {
                            _ASSERTE(m_dwCurrentRuntimeElement > 0);

                            // The standard model for runtime config settings is as follows
                            //
                            // <runtime>
                            //    <m_pCurrentRuntimeElement enabled="true|false" />
                            // or
                            //    <m_pCurrentRuntimeElement enabled="1|0" />
                            // or
                            //    <m_pCurrentRuntimeElement value="string" />

                            m_fOnEnabledAttribute = (EEXMLStringComparei(pszString, dwStringSize, CONST_STRING_AND_LEN(W("enabled"))) == 0);
                            m_fOnValueAttribute = (EEXMLStringComparei(pszString, dwStringSize, CONST_STRING_AND_LEN(W("value"))) == 0);
                        }
                        else // We're looking at developer settings 
                        {       
                            // Developer settings look like
                            //     <developerSettings installationVersion="v2.0.40223.0" />
                            //
                            // or
                            //
                            //     <developmentMode developerInstallation="true" />
                            //

                            // The key name will actually be the attribute name                            
                            
                            hr = CopyToKey(pszString, dwStringSize);
                            if(FAILED(hr)) return hr;
                            m_fOnEnabledAttribute = FALSE;
                            m_fOnValueAttribute = FALSE;
                        }
                    }     
                }
                break;
            case XML_PCDATA:
                if(fVersion) {
                    // if this is not the right version
                    // then we are not interested 
                    if(EEXMLStringCompare(pszString, dwStringSize, m_pVersion, (DWORD)wcslen(m_pVersion))) {
                        m_fUnderRuntimeElement = FALSE;
                    }
                    else {
                        // if it is the right version then overwrite
                        // all entries that exist in the hash table
                        m_fVersionedRuntime = TRUE;
                    }

                    fVersion = FALSE;
                }
                else if(fRuntimeKey) {
                    break; // Ignore all other attributes on <runtime>
                }
                
                // m_dwCurrentRuntimeElement is set when we called CopyToKey in the XML_ELEMENT case
                // section above.
                else if(m_dwCurrentRuntimeElement > 0 && (m_fDeveloperSettings || m_fOnEnabledAttribute || m_fOnValueAttribute)) {

                    // This means that, either we are working on attribute values for the developer settings,
                    // or we've got what "enabled" is equal to, or we're reading a string for a value setting.
                    //
                    // <runtime>
                    //   <m_pwzCurrentElementUnderRuntimeElement m_pLastKey=pString />
                        
                    if (m_fOnEnabledAttribute) {
                        // For the enabled settings, let's convert all trues to 1s and the falses to 0s
                        if (EEXMLStringComparei(pszString, dwStringSize, CONST_STRING_AND_LEN(W("false"))) == 0) {
                            pszString = W("0");
                            dwStringSize = 1;
                        }    
                        else if (EEXMLStringComparei(pszString, dwStringSize, CONST_STRING_AND_LEN(W("true"))) == 0) {
                            pszString = W("1");
                            dwStringSize = 1;
                        }

                        // <TODO> Right now, if pString isn't 0 or 1, then the XML schema is bad.
                        // If we were to ever do schema validation, this would be a place to put it.
                        // </TODO>
                    }
                    
                    hr = AddKeyValuePair(pszString, dwStringSize, m_pCurrentRuntimeElement, m_dwCurrentRuntimeElement);
                    if(FAILED(hr)) { return hr; }
                }
                    
                break ;     
            default: 
                ;
            } // end of switch
        }
    }
    return hr;
}

HRESULT STDMETHODCALLTYPE EEConfigFactory::AddKeyValuePair( 
    __in_ecount(dwStringSize) WCHAR * pszString,
    /* [in] */ DWORD dwStringSize,
    __in_ecount(m_dwCurrentRuntimeElement) WCHAR * m_pCurrentRuntimeElement,
    /* [in] */ DWORD m_dwCurrentRuntimeElement
    )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;
    
    HRESULT hr = S_OK;
    
    // verify we the size fields don't overflow
    if (dwStringSize + 1 < dwStringSize) { return E_FAIL; }
    if (m_dwCurrentRuntimeElement < m_dwCurrentRuntimeElement - 1) { return E_FAIL; }   

    EX_TRY
    {
        // Allocate memory that can store this setting
        NewArrayHolder<WCHAR> pStringToKeep(new WCHAR[dwStringSize+1]);
        wcsncpy_s(pStringToKeep, dwStringSize + 1, pszString, dwStringSize);
        
        // See if we've already picked up a value for this setting
        ConfigStringKeyValuePair * pair = m_pTable->Lookup(m_pCurrentRuntimeElement);
        if(pair != NULL) {
            // If this is a config section for this runtime version, then it's allowed to overwrite
            // previous settings that we've picked up
            if(m_fVersionedRuntime) {
                delete[] pair->value;
                pair->value = pStringToKeep;
                pStringToKeep.SuppressRelease();
            }
        }
        else {            
            // We're adding a new config item
            NewArrayHolder<WCHAR> pKeyToKeep (new WCHAR[m_dwCurrentRuntimeElement]);
            wcsncpy_s(pKeyToKeep, m_dwCurrentRuntimeElement, m_pCurrentRuntimeElement, m_dwCurrentRuntimeElement - 1);
            
            ConfigStringKeyValuePair * newPair = new ConfigStringKeyValuePair();
            newPair->key = pKeyToKeep;
            newPair->value = pStringToKeep;
            m_pTable->Add(newPair);
            pKeyToKeep.SuppressRelease();
            pStringToKeep.SuppressRelease();
        }
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}

