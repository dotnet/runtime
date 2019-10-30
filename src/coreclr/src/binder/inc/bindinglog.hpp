// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BindingLog.hpp
//


//
// Defines the BindingLog class
//
// ============================================================

#ifndef __BINDER__BINDING_LOG_HPP__
#define __BINDER__BINDING_LOG_HPP__

#include "bindertypes.hpp"

namespace BINDER_SPACE
{
    class BindingLog
    {
    public:
        BindingLog();
        ~BindingLog();

        //
        // These functions will create a new log together with pre-bind state
        // information if needed and store it into the application context.
        // This is to avoid endlessly passing around the debug log.
        //
        static HRESULT CreateInContext(/* in */ ApplicationContext *pApplicationContext,
                                       /* in */ SString            &assemblyPath,
                                       /* in */ PEAssembly         *pParentAssembly);
        static HRESULT CreateInContext(/* in */ ApplicationContext *pApplicationContext,
                                       /* in */ AssemblyName       *pAssemblyName,
                                       /* in */ PEAssembly         *pParentAssembly);

        HRESULT Log(SString &info);

        inline HRESULT Log(LPCWSTR pwzInfo);
        inline HRESULT Log(/* in */ LPCWSTR pwzPrefix,
                           /* in */ SString &info);

        HRESULT LogAssemblyName(/* in */ LPCWSTR       pwzPrefix,
                                /* in */ AssemblyName *pAssemblyName);

        HRESULT LogHR(/* in */ HRESULT logHR);
        HRESULT LogResult(/* in */ BindResult *pBindResult);

        HRESULT Flush();

        inline BOOL CanLog();

        void SetDebugLog(CDebugLog *pCDebugLog);

    protected:
        static BOOL IsLoggingNeeded();

        static HRESULT CreateInContext(/* in */ ApplicationContext *pApplicationContext,
                                       /* in */ AssemblyName       *pAssemblyName,
                                       /* in */ SString            &assemblyPath,
                                       /* in */ PEAssembly         *pParentAssembly);

        HRESULT LogPreBindState(/* in */ ApplicationContext *pApplicationContext,
                                /* in */ AssemblyName       *pAssemblyName,
                                /* in */ SString            &assemblyPath,
                                /* in */ PEAssembly         *pParentAssembly);


        inline CDebugLog *GetDebugLog();

        HRESULT LogUser();

        CDebugLog *m_pCDebugLog;
    };

#include "bindinglog.inl"
};

#endif
