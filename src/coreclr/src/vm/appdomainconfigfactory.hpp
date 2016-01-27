// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef APPDOMAINCONFIGFACTORY_H
#define APPDOMAINCONFIGFACTORY_H

#include <xmlparser.h>
#include <objbase.h>
#include "unknwn.h"
#include "../xmlparser/_reference.h"
#include "../xmlparser/_unknown.h"

#include "appdomain.hpp"

#define ISWHITE(ch) ((ch) >= 0x09 && (ch) <= 0x0D || (ch) == 0x20)

#define CONST_STRING_AND_LEN(str) str, NumItems(str)-1


extern int EEXMLStringCompare(const WCHAR *pStr1, 
                    DWORD cchStr1, 
                    const WCHAR *pStr2, 
                    DWORD cchStr2);


enum APPDOMAINPARSESTATE
{
    APPDOMAINPARSESTATE_INITIALIZED,
    APPDOMAINPARSESTATE_RUNTIME,
    APPDOMAINPARSESTATE_PREFERCOMINSTEADOFREMOTING,
    APPDOMAINPARSESTATE_ENABLED,
    APPDOMAINPARSESTATE_LEGACYMODE
};



class AppDomainConfigFactory : public _unknown<IXMLNodeFactory, &IID_IXMLNodeFactory>
{

public:
    AppDomainConfigFactory() : m_dwDepth(0), comorRemotingFlag(COMorRemoting_NotInitialized), m_appdomainParseState(APPDOMAINPARSESTATE_INITIALIZED)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~AppDomainConfigFactory()
    {
        LIMITED_METHOD_CONTRACT;
    }

    HRESULT STDMETHODCALLTYPE NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt)
    {
        LIMITED_METHOD_CONTRACT;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE BeginChildren( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ XML_NODE_INFO* __RPC_FAR pNodeInfo)
    {
        LIMITED_METHOD_CONTRACT;

        m_dwDepth++;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE EndChildren( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ BOOL fEmptyNode,
        /* [in] */ XML_NODE_INFO* __RPC_FAR pNodeInfo)
    {
        LIMITED_METHOD_CONTRACT;

        if (!fEmptyNode)
            m_dwDepth--;
        return S_OK;
    }
    
    HRESULT STDMETHODCALLTYPE Error( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ HRESULT hrErrorCode,
        /* [in] */ USHORT cNumRecs,
        /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo)
    {
        LIMITED_METHOD_CONTRACT;
      /* 
         UNUSED(pSource);
         UNUSED(hrErrorCode);
         UNUSED(cNumRecs);
         UNUSED(apNodeInfo);
      */
        return hrErrorCode;
    }
    
    HRESULT STDMETHODCALLTYPE CreateNode( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ PVOID pNodeParent,
        /* [in] */ USHORT cNumRecs,
        /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
            INJECT_FAULT(return E_OUTOFMEMORY;);
        }
        CONTRACTL_END;
    
        if(m_dwDepth > 2)
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

                if (m_appdomainParseState == APPDOMAINPARSESTATE_INITIALIZED)
                {
                    //look forward to <runtime>
                    if (m_dwDepth == 1 &&
                        apNodeInfo[i]->dwType == XML_ELEMENT &&
                        EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("runtime"))) == 0)
                    {
                        m_appdomainParseState = APPDOMAINPARSESTATE_RUNTIME;
                    }
                    return S_OK;
                }
                else if (m_appdomainParseState == APPDOMAINPARSESTATE_RUNTIME)
                {
                    // look forward to <PreferComInsteadOfManagedRemoting enabled="true"/>
                    if (m_dwDepth == 2 &&
                        apNodeInfo[i]->dwType == XML_ELEMENT &&
                        EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("PreferComInsteadOfManagedRemoting"))) == 0)
                    {
                        m_appdomainParseState = APPDOMAINPARSESTATE_PREFERCOMINSTEADOFREMOTING;
                        continue;
                    }
                    // if we ended parsing <Runtime>, we abort it
                    if (m_dwDepth <= 1)
                        pSource->Abort(NULL);
                    return S_OK;
                }
                else if (m_appdomainParseState == APPDOMAINPARSESTATE_PREFERCOMINSTEADOFREMOTING)
                {
                    // require enabled="true"/> or legacyMode="true"/>
                    if (m_dwDepth == 2 &&
                        apNodeInfo[i]->dwType == XML_ATTRIBUTE)
                    {
                        if (EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("enabled"))) == 0)
                        {
                            m_appdomainParseState = APPDOMAINPARSESTATE_ENABLED;
                        }
                        if (EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("legacyMode"))) == 0)
                        {
                            m_appdomainParseState = APPDOMAINPARSESTATE_LEGACYMODE;
                        }
                    }

                    // ignore unrecognized attributes (forward compat)
                    continue;
                }
                else if (m_appdomainParseState == APPDOMAINPARSESTATE_ENABLED || m_appdomainParseState == APPDOMAINPARSESTATE_LEGACYMODE)
                {
                    // require "true" /> or "false" />
                    if (m_dwDepth == 2 &&
                        apNodeInfo[i]->dwType == XML_PCDATA)
                    {
                        if (EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("true"))) == 0)
                        {
                            if (m_appdomainParseState == APPDOMAINPARSESTATE_LEGACYMODE)
                            {
                                // LegacyMode does not override the "master switch"
                                if (comorRemotingFlag != COMorRemoting_COM)
                                    comorRemotingFlag = COMorRemoting_LegacyMode;
                            }
                            else
                            {
                                comorRemotingFlag = COMorRemoting_COM;
                            }
                        }
                        else if (EEXMLStringCompare(pszString, dwStringSize, CONST_STRING_AND_LEN(W("false"))) == 0)
                        {
                            if (m_appdomainParseState == APPDOMAINPARSESTATE_ENABLED)
                            {
                                // we do report that the "master switch" is explictly false
                                if (comorRemotingFlag == COMorRemoting_NotInitialized)
                                    comorRemotingFlag = COMorRemoting_Remoting;
                            }
                        }

                        m_appdomainParseState = APPDOMAINPARSESTATE_PREFERCOMINSTEADOFREMOTING;
                        continue;
                    }
                    pSource->Abort(NULL);
                    return S_OK;
                }
            }
        }
        return hr;
    }

    COMorRemotingFlag GetCOMorRemotingFlag()
    {
        LIMITED_METHOD_CONTRACT;
        return comorRemotingFlag;
    }

private:
    DWORD m_dwDepth;
    COMorRemotingFlag comorRemotingFlag;
    APPDOMAINPARSESTATE m_appdomainParseState;

};

#endif APPDOMAINCONFIGFACTORY_H
