// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: amsi.cpp
//

#include "common.h"
#include "amsi.h"

namespace
{
    // https://docs.microsoft.com/en-us/windows/desktop/api/amsi/
    DECLARE_HANDLE(HAMSICONTEXT);
    DECLARE_HANDLE(HAMSISESSION);

    enum AMSI_RESULT
    {
        AMSI_RESULT_CLEAN                   = 0,
        AMSI_RESULT_NOT_DETECTED            = 1,
        AMSI_RESULT_BLOCKED_BY_ADMIN_START  = 0x4000,
        AMSI_RESULT_BLOCKED_BY_ADMIN_END    = 0x4fff,
        AMSI_RESULT_DETECTED                = 0x8000
    }   AMSI_RESULT;

    bool AmsiResultIsMalware(DWORD result)
    {
        return result >= AMSI_RESULT_DETECTED;
    }

    bool AmsiResultIsBlockedByAdmin(DWORD result)
    {
        return result >= AMSI_RESULT_BLOCKED_BY_ADMIN_START
            && result <= AMSI_RESULT_BLOCKED_BY_ADMIN_END;
    }

    using PAMSI_AMSISCANBUFFER_API = HRESULT(WINAPI *)(
        _In_ HAMSICONTEXT amsiContext,
        _In_ PVOID buffer,
        _In_ ULONG length,
        _In_ LPCWSTR contentName,
        _In_opt_ HAMSISESSION session,
        _Out_ DWORD *result);

    using PAMSI_AMSIINITIALIZE_API = HRESULT(WINAPI *)(
        _In_ LPCWSTR appName,
        _Out_ HAMSICONTEXT *amsiContext);

    PAMSI_AMSISCANBUFFER_API AmsiScanBuffer;
    HAMSICONTEXT s_amsiContext;
    CRITSEC_COOKIE s_csAmsi;

    bool InitializeLock()
    {
        if (s_csAmsi != nullptr)
            return true;

        CRITSEC_COOKIE lock = ClrCreateCriticalSection(CrstLeafLock, CRST_REENTRANCY);
        if (lock == nullptr)
            return false;

        if (InterlockedCompareExchangeT<CRITSEC_COOKIE>(&s_csAmsi, lock, nullptr) != nullptr)
            ClrDeleteCriticalSection(lock);

        return true;
    }
}

// Here we will invoke into AmsiScanBuffer, a centralized area for non-OS
// programs to report into Defender (and potentially other anti-malware tools).
// This should only run on in memory loads, Assembly.Load(byte[]) for example.
// Loads from disk are already instrumented by Defender, so calling AmsiScanBuffer
// wouldn't do anything.
bool Amsi::IsBlockedByAmsiScan(PVOID flatImageBytes, COUNT_T size)
{
    STANDARD_VM_CONTRACT;

    if (!InitializeLock())
        return false;

    // Lazily initialize AMSI because it is very expensive
    {
        CRITSEC_Holder csh(s_csAmsi);

        // Cache that we failed if this didn't work so we don't keep trying to reinitialize
        static bool amsiInitializationAttempted = false;
        if (s_amsiContext == nullptr && !amsiInitializationAttempted)
        {
            HMODULE amsi = CLRLoadLibraryEx(W("amsi.dll"), nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
            if (amsi != nullptr)
            {
                PAMSI_AMSIINITIALIZE_API AmsiInitialize = (PAMSI_AMSIINITIALIZE_API)GetProcAddress(amsi, "AmsiInitialize");
                if (AmsiInitialize != nullptr)
                {
                    HAMSICONTEXT amsiContext = nullptr;
                    if (AmsiInitialize(W("coreclr"), &amsiContext) == S_OK)
                    {
                        AmsiScanBuffer = (PAMSI_AMSISCANBUFFER_API)GetProcAddress(amsi, "AmsiScanBuffer");
                        if (AmsiScanBuffer != nullptr)
                        {
                            s_amsiContext = amsiContext;
                        }
                    }
                }
            }

            amsiInitializationAttempted = true;
        }
    }

    if (s_amsiContext == nullptr || AmsiScanBuffer == nullptr)
        return false;

    DWORD result;
    HRESULT hr = AmsiScanBuffer(s_amsiContext, flatImageBytes, size, nullptr, nullptr, &result);
    if (hr == S_OK && (AmsiResultIsMalware(result) || AmsiResultIsBlockedByAdmin(result)))
        return true;

    return false;
}
