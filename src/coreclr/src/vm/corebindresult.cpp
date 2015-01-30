//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ============================================================
//
// CoreBindResult.cpp
// 

//
// Implements the CoreBindResult class
// ============================================================


#include "common.h"

#ifdef CLR_STANDALONE_BINDER
#include "coreclr\corebindresult.h"
#endif // CLR_STANDALONE_BINDER

#include "../binder/inc/assembly.hpp"

#ifndef FEATURE_FUSION
#ifndef DACCESS_COMPILE

STDMETHODIMP CoreBindResult::QueryInterface(REFIID   riid,
                                          void   **ppv)
{
    HRESULT hr = S_OK;

    if (ppv == NULL)
    {
        hr = E_POINTER;
    }    
    else
    {
        if ( IsEqualIID(riid, IID_IUnknown) )
        {
            AddRef();
            *ppv = static_cast<IUnknown *>(this);
        }
        else
        {
            *ppv = NULL;
            hr = E_NOINTERFACE;
        }
    }

    return hr;
}

STDMETHODIMP_(ULONG) CoreBindResult::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG) CoreBindResult::Release()
{
    ULONG ulRef = InterlockedDecrement(&m_cRef);

    if (ulRef == 0) 
    {
        delete this;
    }

    return ulRef;
}


#endif  // DACCES_COMPILE
#endif // FEATURE_FUSION
