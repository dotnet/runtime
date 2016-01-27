// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// DebugLog.hpp
//


//
// Defines the DebugLog class
//
// ============================================================

#ifndef __BINDER__DEBUG_LOG_HPP__
#define __BINDER__DEBUG_LOG_HPP__

#include "bindertypes.hpp"
#include "variables.hpp"

namespace BINDER_SPACE
{

// When defined at the top of a source file, DISABLE_BINDER_DEBUG_LOGGING will cause
// binder debug logging to be disabled only in the scope of that file by defining all the
// binder logging macros as NOPs.

#if defined(BINDER_DEBUG_LOG) && !defined(DISABLE_BINDER_DEBUG_LOGGING)

#define BINDER_LOG_STARTUP()                    \
    IF_FAIL_GO(DebugLog::Startup());

#define BINDER_LOG_LOCK()                       \
    CRITSEC_Holder logLock(g_BinderVariables->m_logCS);

#define BINDER_LOG_ENTER(scope)                 \
    DebugLog::Enter(scope);

#define BINDER_LOG_LEAVE(scope)                 \
    DebugLog::Leave(scope);

#define BINDER_LOG_LEAVE_HR(scope, hr)          \
    DebugLog::LeaveHR(scope, hr);

#define BINDER_LOG_LEAVE_BOOL(scope, fResult)   \
    DebugLog::LeaveBool(scope, fResult);

#define BINDER_LOG(comment)                     \
    DebugLog::Log(comment);

#define BINDER_LOG_STRING(comment, value)       \
    DebugLog::Log(comment, value);

#define BINDER_LOG_HRESULT(comment, hr)         \
    DebugLog::Log(comment, hr);

#define BINDER_LOG_ASSEMBLY_NAME(comment, assemblyName) \
    DebugLog::Log(comment, assemblyName);

#define BINDER_LOG_I_ASSEMBLY_NAME(comment, assemblyName) \
    DebugLog::Log(comment, assemblyName);

#define BINDER_LOG_POINTER(comment, pData)      \
    DebugLog::Log(comment, (void *) (pData));

    class DebugLog
    {
    public:
        static HRESULT Startup();
    
        static void Enter(/* in */ WCHAR *pwzScope);
        static void Leave(/* in */ WCHAR *pwzScope);
        static void LeaveHR(/* in */ WCHAR   *pwzScope,
                            /* in */ HRESULT  hrLog);
        static void LeaveBool(/* in */ WCHAR   *pwzScope,
                              /* in */ BOOL     fResult);

        static void Log(/* in */ WCHAR *pwzComment);
        static void Log(/* in */ WCHAR   *pwzComment,
                        /* in */ SString &value);
        static void Log(/* in */ WCHAR   *pwzComment,
                        /* in */ const WCHAR   *value);
        static void Log(/* in */ WCHAR   *pwzComment,
                        /* in */ HRESULT  hrLog);
        static void Log(/* in */ WCHAR        *pwzComment,
                        /* in */ AssemblyName *pAssemblyName);
        static void Log(/* in */ WCHAR        *pwzComment,
                        /* in */ void         *pData);
    protected:
        static void Log(/* in */ SString &info);
    };
#else
    class DebugLog
    {
    public:
        static void Empty() {};
    };

#define BINDER_LOG_STARTUP() DebugLog::Empty();

#define BINDER_LOG_LOCK() DebugLog::Empty();

#define BINDER_LOG_ENTER(scope) DebugLog::Empty();
#define BINDER_LOG_LEAVE(scope) DebugLog::Empty();
#define BINDER_LOG_LEAVE_HR(scope, hr) DebugLog::Empty();
#define BINDER_LOG_LEAVE_BOOL(scope, fResult) DebugLog::Empty();

#define BINDER_LOG(comment) DebugLog::Empty();
#define BINDER_LOG_STRING(comment, value) DebugLog::Empty();
#define BINDER_LOG_HRESULT(comment, hr) DebugLog::Empty();
#define BINDER_LOG_ASSEMBLY_NAME(comment, assemblyName) DebugLog::Empty();
#define BINDER_LOG_I_ASSEMBLY_NAME(comment, assemblyName) DebugLog::Empty();
#define BINDER_LOG_POINTER(comment, pData) DebugLog::Empty();

#endif
};

#endif
