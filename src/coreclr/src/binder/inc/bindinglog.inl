// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BindingLog.inl
//


//
// Implements inlined methods of BindingLog
//
// ============================================================

#ifndef __BINDER__BINDING_LOG_INL__
#define __BINDER__BINDING_LOG_INL__

BOOL BindingLog::CanLog()
{
    return (m_pCDebugLog != NULL);
}

CDebugLog *BindingLog::GetDebugLog()
{
    _ASSERTE(m_pCDebugLog != NULL);
    return m_pCDebugLog;
}

HRESULT BindingLog::Log(LPCWSTR pwzInfo)
{
    PathString info(pwzInfo);
    return BindingLog::Log(info);
}

HRESULT BindingLog::Log(LPCWSTR  pwzPrefix,
                        SString &info)
{
    PathString message;

    message.Append(pwzPrefix);
    message.Append(info);

    return Log(message);
}

#endif
