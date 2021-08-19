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
    ReleaseHolder<BINDER_SPACE::Assembly> m_pAssembly;
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

    void Init(BINDER_SPACE::Assembly* pAssembly);
    void Reset();

    BOOL Found();
    PEImage* GetPEImage();
    BOOL IsCoreLib();
    void GetBindAssembly(BINDER_SPACE::Assembly** ppAssembly);
    BOOL HasNativeImage() { return FALSE; }
    PEImage* GetNativeImage() { return NULL; }

    void SetHRBindResult(HRESULT hrBindResult);
    HRESULT GetHRBindResult();
};


#endif // __CORE_BIND_RESULT_H__

