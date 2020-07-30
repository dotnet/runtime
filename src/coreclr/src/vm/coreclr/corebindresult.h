// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// CoreBindResult.h
//

//
// Declares the CoreBindResult class
// BindResult represent a result of an assembly bind and might encapsulate PEImage, IAssembly, IHostAssebmly etc.
// This is CoreCLR implementation of it
// ============================================================

#ifndef __CORE_BIND_RESULT_H__
#define __CORE_BIND_RESULT_H__

#include "../../binder/inc/assembly.hpp"

struct CoreBindResult : public IUnknown
{
protected:
    ReleaseHolder<ICLRPrivAssembly> m_pAssembly;
    HRESULT m_hrBindResult;
    LONG m_cRef;

public:

    // IUnknown methods
    STDMETHOD(QueryInterface)(REFIID riid,
                              void ** ppv);
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();

    // CoreBindResult methods
    CoreBindResult() : m_cRef(1) {}
    virtual ~CoreBindResult() {}

    void Init(ICLRPrivAssembly* pAssembly);
    void Reset();

    BOOL Found();
    PEImage* GetPEImage();
    BOOL IsCoreLib();
    void GetBindAssembly(ICLRPrivAssembly** ppAssembly);
#ifdef FEATURE_PREJIT
    BOOL HasNativeImage();
    PEImage* GetNativeImage();
    void SetNativeImage(PEImage * pNativeImage);
    PEImage* GetILImage();
#else
    BOOL HasNativeImage() { return FALSE; }
    PEImage* GetNativeImage() { return NULL; }
#endif
    void SetHRBindResult(HRESULT hrBindResult);
    HRESULT GetHRBindResult();
};


#endif // __CORE_BIND_RESULT_H__

