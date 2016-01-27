// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  AssemblySink.hpp
**
** Purpose: Asynchronous call back for loading classes
**
**


**
===========================================================*/
#ifndef _ASSEMBLYSINK_H
#define _ASSEMBLYSINK_H

#ifndef FEATURE_FUSION
#error FEATURE_FUSION is not enabled, please do not include assemblysink.h
#endif

class AppDomain;

class AssemblySink : public FusionSink
{
public:
    AssemblySink(AppDomain* pDomain);
    ~AssemblySink() { WRAPPER_NO_CONTRACT; };

    void Reset();

    ULONG STDMETHODCALLTYPE Release(void);

    STDMETHODIMP OnProgress(DWORD dwNotification,
                            HRESULT hrNotification,
                            LPCWSTR szNotification,
                            DWORD dwProgress,
                            DWORD dwProgressMax,
                            LPVOID pvBindInfo,
                            IUnknown* punk);

    virtual HRESULT Wait();

    void RequireCodebaseSecurityCheck() {LIMITED_METHOD_CONTRACT;  m_CheckCodebase = TRUE;}
    BOOL DoCodebaseSecurityCheck() {LIMITED_METHOD_CONTRACT;  return m_CheckCodebase;}
    void SetAssemblySpec(AssemblySpec* pSpec) 
    {
        LIMITED_METHOD_CONTRACT; 
        m_pSpec=pSpec;
    }

private:
    ADID m_Domain; // Which domain (index) do I belong to
    AssemblySpec* m_pSpec;
    BOOL m_CheckCodebase;
};

#endif
