// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// AssemblyNamesConfigFactory.cpp
//

//
// 
// Parses XML files and adding runtime entries to assembly list
// Abstract, derived classes need to override AddAssemblyName
#include "common.h"
#include "common.h"
#include <xmlparser.h>
#include <objbase.h>
#include "parse.h"
#include "assemblynamesconfigfactory.h"


#define ISWHITE(ch) ((ch) >= 0x09 && (ch) <= 0x0D || (ch) == 0x20)

#define CONST_STRING_AND_LEN(str) str, NumItems(str)-1

extern int EEXMLStringCompare(const WCHAR *pStr1, 
                    DWORD cchStr1, 
                    const WCHAR *pStr2, 
                    DWORD cchStr2);
extern HRESULT VersionFromString(LPCWSTR wzVersion, WORD *pwVerMajor, WORD *pwVerMinor,
                          WORD *pwVerBld, WORD *pwVerRev);
extern HRESULT MapProcessorArchitectureToPEKIND(LPCWSTR pwzProcArch, PEKIND *pe);

AssemblyNamesConfigFactory::AssemblyNamesConfigFactory()
{
    LIMITED_METHOD_CONTRACT;
    m_pAssemblyName = NULL;
    m_bCurrentEntryInvalid = TRUE;
    m_dwCurrentElementDepth = 0;
    m_dwProperty = ASM_NAME_MAX_PARAMS;
}

AssemblyNamesConfigFactory::~AssemblyNamesConfigFactory()
{
    LIMITED_METHOD_CONTRACT;
}


HRESULT STDMETHODCALLTYPE AssemblyNamesConfigFactory::NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt)
{
    LIMITED_METHOD_CONTRACT;
    
    return S_OK;
}

HRESULT STDMETHODCALLTYPE AssemblyNamesConfigFactory::BeginChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    LIMITED_METHOD_CONTRACT;
    m_dwCurrentElementDepth ++;

    return S_OK;
}

//---------------------------------------------------------------------------
HRESULT STDMETHODCALLTYPE AssemblyNamesConfigFactory::EndChildren( 
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ BOOL fEmptyNode,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (m_dwCurrentElementDepth == 1 && m_pAssemblyName != NULL)
        {
            if (!m_bCurrentEntryInvalid)
            {
                // publish 
                AddAssemblyName(m_pAssemblyName);
            };
            m_pAssemblyName->Release();
            m_pAssemblyName = NULL;
        }

        if (!fEmptyNode)
            m_dwCurrentElementDepth --;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}



HRESULT STDMETHODCALLTYPE AssemblyNamesConfigFactory::CreateNode( 
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

    if(m_dwCurrentElementDepth > 1)
        return S_OK;

    HRESULT hr = S_OK;

    for(DWORD i = 0; i < cNumRecs; i++) { 
        CONTRACT_VIOLATION(ThrowsViolation); // Lots of stuff in here throws!
        
        if(apNodeInfo[i]->dwType == XML_ELEMENT ||
           apNodeInfo[i]->dwType == XML_ATTRIBUTE ||
           apNodeInfo[i]->dwType == XML_PCDATA) 
        {

            DWORD dwStringSize = apNodeInfo[i]->ulLen;
            LPWSTR pszString = (WCHAR*) apNodeInfo[i]->pwcText;
            // Trim the value

            // we should never decrement lgth if it's 0, because it's unsigned

            for(;*pszString && ISWHITE(*pszString) && dwStringSize>0; pszString++, dwStringSize--);
            while( dwStringSize > 0 && ISWHITE(pszString[dwStringSize-1]))
                   dwStringSize--;
            switch(apNodeInfo[i]->dwType) 
            {
            case XML_ELEMENT : 
                if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("assemblyIdentity"))) == 0) 
                {
                    // new entry 
                    _ASSERTE(m_pAssemblyName == NULL);
                    IfFailRet(CreateAssemblyNameObject(&m_pAssemblyName, NULL,0,NULL));
                    m_bCurrentEntryInvalid = FALSE;
                }
                else
                {
                    m_bCurrentEntryInvalid = TRUE;
                }

                break;


            case XML_ATTRIBUTE : 
                if(m_bCurrentEntryInvalid)
                    break;

                if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("name"))) == 0) 
                {
                    m_dwProperty = ASM_NAME_NAME;
                }
                else
                if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("version"))) == 0) 
                {
                    m_dwProperty = ASM_NAME_MAJOR_VERSION;
                }
                else
                if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("publicKeyToken"))) == 0) 
                {
                    m_dwProperty = ASM_NAME_PUBLIC_KEY_TOKEN;
                }
                else
                if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("processorArchitecture"))) == 0) 
                {
                    m_dwProperty = ASM_NAME_ARCHITECTURE;
                }
                else
                {
                    m_bCurrentEntryInvalid = TRUE;
                }
                break;


            case XML_PCDATA : 
                if(m_bCurrentEntryInvalid)
                    break;

                _ASSERTE(m_pAssemblyName!= NULL); // can only be null if m_bCurrentEntryInvalid
                switch(m_dwProperty)
                {
                case ASM_NAME_NAME:
                    {
                        StackSString s(pszString,dwStringSize);
                        // takes number of bytes, thus *2
                        IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_NAME, LPCWSTR(s), (dwStringSize+1)*sizeof(WCHAR)));
                    }
                    break;
                case ASM_NAME_MAJOR_VERSION:
                    {
                        StackSString s(pszString,dwStringSize);
                        WORD  wVerMajor = 0;
                        WORD  wVerMinor = 0;
                        WORD  wVerBld = 0;
                        WORD  wVerRev = 0;
                        if (SUCCEEDED(VersionFromString(s, &wVerMajor, &wVerMinor, &wVerBld, &wVerRev)))
                        {
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_MAJOR_VERSION, &wVerMajor, sizeof(WORD)));
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_MINOR_VERSION, &wVerMinor, sizeof(WORD)));
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_BUILD_NUMBER, &wVerBld, sizeof(WORD)));
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_REVISION_NUMBER, &wVerRev, sizeof(WORD)));
                        }
                        else
                            m_bCurrentEntryInvalid = TRUE;

                    }
                    break;
                case ASM_NAME_ARCHITECTURE:
                    {
                        StackSString s(pszString,dwStringSize);
                        PEKIND PeKind = peNone;
                        if(SUCCEEDED(MapProcessorArchitectureToPEKIND(s, &PeKind)))
                        {
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_ARCHITECTURE, (LPBYTE) &PeKind, sizeof(PeKind)));
                        }
                        else
                        {
                            m_bCurrentEntryInvalid = TRUE;
                        }

                    }
                    break;    
                case ASM_NAME_PUBLIC_KEY_TOKEN:
                    {
                       if(EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("null"))) == 0) 
                        {
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_NULL_PUBLIC_KEY_TOKEN, NULL, 0));
                        }
                        else
                        {
                            if (dwStringSize % 2 != 0)
                                return FUSION_E_INVALID_NAME;

                            DWORD cbProp = dwStringSize / 2;
                            NewHolder<BYTE> pbProp = new BYTE[cbProp];
                            CParseUtils::UnicodeHexToBin(pszString, dwStringSize, pbProp); //????
                            IfFailRet(m_pAssemblyName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, pbProp, cbProp));
                        }
                        break;
                    }

                default:
                    _ASSERTE(!"Invalid format");
                    m_bCurrentEntryInvalid = TRUE;
                    break;
                }
                break;
            }

        }
    }
    return S_OK;
}
