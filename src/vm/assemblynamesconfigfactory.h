// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// AssemblyNamesConfigFactory.h
//

//
// 
// Parses XML files and adding runtime entries to assembly list
// Abstract, derived classes need to override AddAssemblyName


#ifndef ASSEMBLYNAMESCONFIGFACTORY_H
#define ASSEMBLYNAMESCONFIGFACTORY_H

#include "unknwn.h"
#include "../xmlparser/_reference.h"
#include "../xmlparser/_unknown.h"


class AssemblyNamesConfigFactory : public _unknown<IXMLNodeFactory, &IID_IXMLNodeFactory>
{

public:
    AssemblyNamesConfigFactory ();
    ~AssemblyNamesConfigFactory ();
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
        /* [in] */ XML_NODE_INFO* __RPC_FAR * __RPC_FAR apNodeInfo);

    virtual void AddAssemblyName(IAssemblyName*) = 0;
protected:
    IAssemblyName* m_pAssemblyName;
    BOOL m_bCurrentEntryInvalid;
    DWORD m_dwCurrentElementDepth;
    DWORD m_dwProperty;

};


#endif
