//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ============================================================
//
// CDebugLog.hpp
//


//
// Defines the CDebugLog class
//
// ============================================================

#ifndef __BINDER__C_DEBUG_LOG_HPP__
#define __BINDER__C_DEBUG_LOG_HPP__

#include "bindertypes.hpp"
#include "list.hpp"

#define FUSION_BIND_LOG_CATEGORY_DEFAULT 0
#define FUSION_BIND_LOG_CATEGORY_NGEN    1
#define FUSION_BIND_LOG_CATEGORY_MAX     2

namespace BINDER_SPACE
{
    class CDebugLog
    {
    public:
        CDebugLog();
        ~CDebugLog();

        static HRESULT Create(/* in */  ApplicationContext  *pApplicationContext,
                              /* in */  AssemblyName        *pAssemblyName,
                              /* in */  SString             &sCodeBase,
                              /* out */ CDebugLog         **ppCDebugLog);

        ULONG AddRef();
        ULONG Release();

        HRESULT SetResultCode(/* in */ DWORD dwLogCategory,
                              /* in */ HRESULT hrResult);

        HRESULT LogMessage(/* in */ DWORD    dwDetailLevel,
                           /* in */ DWORD    dwLogCategory, 
                           /* in */ SString &sDebugString);

        HRESULT Flush(/* in */ DWORD dwDetailLevel,
                      /* in */ DWORD dwLogCategory);
    protected:
        HRESULT Init(/* in */ ApplicationContext *pApplicationContext,
                     /* in */ AssemblyName       *pAssemblyName,
                     /* in */ SString            &sCodeBase);

        HRESULT LogHeader(/* in */ DWORD dwLogCategory);
        HRESULT LogFooter(/* in */ DWORD dwLogCategory);

        LONG m_cRef;
        FileHandleHolder m_hLogFile;
        List<SString> m_content[FUSION_BIND_LOG_CATEGORY_MAX];
        SString m_applicationName;
        SString m_logFileName;
        HRESULT m_HrResult[FUSION_BIND_LOG_CATEGORY_MAX];
    };
};

#endif
