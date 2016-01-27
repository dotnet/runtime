// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// ConfigHelper.h
// 
//*****************************************************************************
//
// These are unmanaged definitions of interfaces used call Managed Node Factories
// If you make any changes please do corresponding changes in \src\bcl\system\__xmlparser.cs
//


#ifndef _CONFIGHELPER_H
#define _CONFIGHELPER_H

#include <mscoree.h>
#include <xmlparser.h>
#include <mscorcfg.h>
#include "unknwn.h"
#include "../xmlparser/_reference.h"
#include "../xmlparser/_unknown.h"
#include "comdelegate.h"

class ConfigFactory : public _unknown<IXMLNodeFactory, &IID_IXMLNodeFactory>
{
    #define ICONFIGHANDLER_CALLBACK_COUNT 6
    OBJECTREF *m_pManagedFactory;
    LPVOID eventCallbacks[ICONFIGHANDLER_CALLBACK_COUNT];
    
    // We assume the offsets as per the object layout of ConfigTreeParser defined in CfgParser.cs
    // Any changes made at either place must be propagated to the other
    LPVOID GetCallbackAtOffset(DWORD dwOffset)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(dwOffset < ICONFIGHANDLER_CALLBACK_COUNT);
        }
        CONTRACTL_END;

        PTRARRAYREF refAllDelegates = (PTRARRAYREF)ObjectToOBJECTREF((Object *)((*m_pManagedFactory)->GetPtrOffset(0)));
        _ASSERTE(refAllDelegates->GetNumComponents()==ICONFIGHANDLER_CALLBACK_COUNT);
        return COMDelegate::ConvertToCallback(refAllDelegates->GetAt(dwOffset));
    }

    void Initialize(OBJECTREF *pFactory)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(pFactory != NULL);
        }
        CONTRACTL_END;

        m_pManagedFactory = pFactory;
        EX_TRY
        {
            for(int i=0; i<ICONFIGHANDLER_CALLBACK_COUNT; i++)
            {
                eventCallbacks[i] = GetCallbackAtOffset(i);
            }
        } EX_CATCH { } EX_END_CATCH(SwallowAllExceptions);        
    }

    typedef VOID (STDMETHODCALLTYPE *NotifyEventCallback)(
            /* [in] */ XML_NODEFACTORY_EVENT iEvt);

    NotifyEventCallback GetNotifyEventFunctionality()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(eventCallbacks[0] != NULL);
        return (NotifyEventCallback)eventCallbacks[0];
    }

    typedef VOID (STDMETHODCALLTYPE *BeginChildrenCallback)(
            /* [in] */ DWORD dwSize,
            /* [in] */ DWORD dwSubType,
            /* [in] */ DWORD dwType,
            /* [in] */ BOOL fTerminal,
            /* [in] */ LPCWSTR pwcText,
            /* [in] */ DWORD ulLen,
            /* [in] */ DWORD ulNsPrefixLen);
    
    BeginChildrenCallback GetBeginChildrenFunctionality()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(eventCallbacks[1] != NULL);
        return (BeginChildrenCallback)eventCallbacks[1];
    }

    typedef VOID (STDMETHODCALLTYPE *EndChildrenCallback)(
            /* [in] */ BOOL  fEmpty,
            /* [in] */ DWORD dwSize,
            /* [in] */ DWORD dwSubType,
            /* [in] */ DWORD dwType,
            /* [in] */ BOOL fTerminal,
            /* [in] */ LPCWSTR pwcText,
            /* [in] */ DWORD ulLen,
            /* [in] */ DWORD ulNsPrefixLen);

    EndChildrenCallback GetEndChildrenFunctionality()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(eventCallbacks[2] != NULL);
        return (EndChildrenCallback)eventCallbacks[2];
    }

    typedef VOID (STDMETHODCALLTYPE *ErrorCallback)(
            /* [in] */ DWORD dwSize,
            /* [in] */ DWORD dwSubType,
            /* [in] */ DWORD dwType,
            /* [in] */ BOOL fTerminal,
            /* [in] */ LPCWSTR pwcText,
            /* [in] */ DWORD ulLen,
            /* [in] */ DWORD ulNsPrefixLen);
    
    ErrorCallback GetErrorFunctionality()
    {
        _ASSERTE(eventCallbacks[3] != NULL);
        return (ErrorCallback)eventCallbacks[3];
    }

    typedef VOID (STDMETHODCALLTYPE *CreateNodeCallback)(
            /* [in] */ DWORD dwSize,
            /* [in] */ DWORD dwSubType,
            /* [in] */ DWORD dwType,
            /* [in] */ BOOL fTerminal,
            /* [in] */ LPCWSTR pwcText,
            /* [in] */ DWORD ulLen,
            /* [in] */ DWORD ulNsPrefixLen);

    CreateNodeCallback GetCreateNodeFunctionality()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(eventCallbacks[4] != NULL);
        return (CreateNodeCallback)eventCallbacks[4];
    }

    typedef VOID (STDMETHODCALLTYPE *CreateAttributeCallback)(
            /* [in] */ DWORD dwSize,
            /* [in] */ DWORD dwSubType,
            /* [in] */ DWORD dwType,
            /* [in] */ BOOL fTerminal,
            /* [in] */ LPCWSTR pwcText,
            /* [in] */ DWORD ulLen,
            /* [in] */ DWORD ulNsPrefixLen);

    CreateAttributeCallback GetCreateAttributeFunctionality()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(eventCallbacks[5] != NULL);
        return (CreateAttributeCallback)eventCallbacks[5];
    }
    #undef ICONFIGHANDLER_CALLBACK_COUNT
    
public:
    ConfigFactory(OBJECTREF *pFactory);

    HRESULT STDMETHODCALLTYPE NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt);

    HRESULT STDMETHODCALLTYPE BeginChildren( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ XML_NODE_INFO* __RPC_FAR pNodeInfo);
    
    HRESULT STDMETHODCALLTYPE EndChildren( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ BOOL fEmptyNode,
        /* [in] */ XML_NODE_INFO* __RPC_FAR pNodeInfo);
    
    HRESULT STDMETHODCALLTYPE Error( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ HRESULT hrErrorCode,
        /* [in] */ USHORT cNumRecs,
        /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return hrErrorCode;
    }
    
    HRESULT STDMETHODCALLTYPE CreateNode( 
        /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
        /* [in] */ PVOID pNodeParent,
        /* [in] */ USHORT cNumRecs,
        /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo);
};

class ConfigNative
{
    static HRESULT RunInternal(OBJECTREF *pFactory, LPCWSTR filename);

public:
    static FCDECL2(void, RunParser, Object* refHandlerUNSAFE, StringObject* strFileNameUNSAFE);
};

#endif //  _CONFIGHELPER_H
