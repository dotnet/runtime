// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// CoreBindResult.inl
// 

//
// Implements the CoreBindResult class
// ============================================================

#ifndef __CORE_BIND_RESULT_INL__
#define __CORE_BIND_RESULT_INL__

#include "clrprivbinderutil.h"

inline BOOL CoreBindResult::Found()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pAssembly!=NULL);
};

inline BOOL CoreBindResult::IsMscorlib()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        PRECONDITION(Found());
    }
    CONTRACTL_END;

    BINDER_SPACE::Assembly* pAssembly = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pAssembly);
#ifndef CROSSGEN_COMPILE
    return pAssembly->GetAssemblyName()->IsMscorlib();
#else
    return (pAssembly->GetPath()).EndsWithCaseInsensitive(SString(CoreLibName_IL_W));
#endif
}

inline void CoreBindResult::GetBindAssembly(ICLRPrivAssembly** ppAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        PRECONDITION(Found());
    }
    CONTRACTL_END;
    
    m_pAssembly->AddRef();
    *ppAssembly = m_pAssembly;
}


inline PEImage* CoreBindResult::GetPEImage()
{
    WRAPPER_NO_CONTRACT;
    return m_pAssembly?BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pAssembly)->GetNativeOrILPEImage():NULL;
};

inline void CoreBindResult::Init(ICLRPrivAssembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;
    m_pAssembly=pAssembly;
    if(pAssembly)
        pAssembly->AddRef();
    m_hrBindResult = S_OK;
}

inline void CoreBindResult::Reset()
{
    WRAPPER_NO_CONTRACT;
    m_pAssembly=NULL;
    m_hrBindResult = S_OK;
}
#ifdef FEATURE_PREJIT
inline BOOL CoreBindResult::HasNativeImage()
{
    LIMITED_METHOD_CONTRACT;
    BINDER_SPACE::Assembly* pAssembly = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pAssembly);
    return pAssembly->GetNativePEImage() != NULL;
}
inline PEImage* CoreBindResult::GetNativeImage()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(HasNativeImage());
    BINDER_SPACE::Assembly* pAssembly = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pAssembly);
    return pAssembly->GetNativePEImage();
}

inline PEImage* CoreBindResult::GetILImage()
{
    WRAPPER_NO_CONTRACT;
    return m_pAssembly?BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pAssembly)->GetPEImage():NULL;
};
#endif

inline void CoreBindResult::SetHRBindResult(HRESULT hrBindResult)
{
    WRAPPER_NO_CONTRACT;
    m_hrBindResult = hrBindResult;
}

inline HRESULT CoreBindResult::GetHRBindResult()
{
    WRAPPER_NO_CONTRACT;
    return m_hrBindResult;
}

#endif // __CORE_BIND_RESULT_INL__

