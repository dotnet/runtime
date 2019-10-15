// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// DebugLog.cpp
//


//
// Implements the DebugLog class
//
// ============================================================

#if defined(BINDER_DEBUG_LOG)

#include "debuglog.hpp"
#include "assemblyname.hpp"
#include "utils.hpp"
#include "variables.hpp"

#include "ex.h"

namespace BINDER_SPACE
{
    namespace
    {
        void GetStringFromHR(HRESULT  hr,
                             SString &info)
        {
            switch(hr)
            {
            case S_OK:
                info.Append(L"S_OK");
                break;
            case S_FALSE:
                info.Append(L"S_FALSE");
                break;
            case E_FAIL:
                info.Append(L"E_FAIL");
                break;
            case __HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND):
                info.Append(L"HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)");
                break;
            case __HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER):
                info.Append(L"HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)");
                break;
            case FUSION_E_REF_DEF_MISMATCH:
                info.Append(L"FUSION_E_REF_DEF_MISMATCH");
                break;
            case FUSION_E_CODE_DOWNLOAD_DISABLED:
                info.Append(L"FUSION_E_CODE_DOWNLOAD_DISABLED");
                break;
            default:
                info.AppendPrintf(L"%p", hr);
                break;
            }
        }

        HRESULT GetLogFilePath(PathString &logFileDir,
                               PathString &logFilePath)
        {
            HRESULT hr = S_OK;

            BOOL fFileExists = TRUE;
            
            do
            {
                LARGE_INTEGER kCount1;
                LARGE_INTEGER kCount2;

                logFilePath.Clear();

                if (!QueryPerformanceCounter(&kCount1))
                {
                    hr = HRESULT_FROM_GetLastError();
                }
                else if(!QueryPerformanceCounter(&kCount2))
                {
                    hr = HRESULT_FROM_GetLastError();
                }
                else
                {
                    logFilePath.Printf(L"%s\\Log_%u%u_%u%u.tmp",
                                       logFileDir.GetUnicode(),
                                       static_cast<UINT32>(kCount1.u.LowPart),
                                       kCount1.u.HighPart,
                                       static_cast<UINT32>(kCount2.u.LowPart),
                                       kCount2.u.HighPart);

                    PlatformPath(logFilePath);
                }

                fFileExists = (FileOrDirectoryExists(logFilePath) == S_OK);
            }
            while (fFileExists == TRUE);

            return hr;
        }

        HRESULT WriteToFile(HANDLE      hFile,
                            const BYTE *pbBuffer,
                            DWORD       dwcbBuffer)
        {
            HRESULT hr = S_OK;
            DWORD dwNumberOfBytesWritten = 0;

            while ((dwcbBuffer != 0) && (dwNumberOfBytesWritten < dwcbBuffer))
            {
                if (WriteFile(hFile, pbBuffer, dwcbBuffer, &dwNumberOfBytesWritten, NULL))
                {
                    dwcbBuffer -= dwNumberOfBytesWritten;
                    pbBuffer += dwNumberOfBytesWritten;
                }
                else
                {
                    hr = HRESULT_FROM_GetLastError();
                    goto Exit;
                }
            }

        Exit:
            return hr;
        }
    };

    /* static */
    HRESULT DebugLog::Startup()
    {
        HRESULT hr = S_OK;

        PathString logFileDir;
        PathString logFilePath;

        REGUTIL::CORConfigLevel kCorConfigLevel =
            static_cast<REGUTIL::CORConfigLevel>(REGUTIL::COR_CONFIG_ENV |
                                                 REGUTIL::COR_CONFIG_FUSION);
        NewArrayHolder<WCHAR> pwzLogDirectory = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CoreClrBinderLog);

        
        g_BinderVariables->m_logCS = ClrCreateCriticalSection(CrstCoreCLRBinderLog, CRST_REENTRANCY);
        if (!g_BinderVariables->m_logCS)
        {
            IF_FAIL_GO(E_OUTOFMEMORY);
        }

        
        if (pwzLogDirectory == NULL)
        {
            goto Exit;
        }

        logFileDir.Set(pwzLogDirectory);

        if (WszCreateDirectory(logFileDir.GetUnicode(), NULL) ||
            ((hr = HRESULT_FROM_GetLastError()) == HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS)))
        {
            hr = S_OK;
        }

        IF_FAIL_GO(GetLogFilePath(logFileDir, logFilePath));

        g_BinderVariables->m_hDebugLogFile = WszCreateFile(logFilePath.GetUnicode(),
                                                           GENERIC_WRITE,
                                                           FILE_SHARE_READ,
                                                           NULL,
                                                           OPEN_ALWAYS,
                                                           FILE_ATTRIBUTE_NORMAL,
                                                           NULL);

        if (g_BinderVariables->m_hDebugLogFile == INVALID_HANDLE_VALUE)
        {
            IF_FAIL_GO(E_FAIL);
        }
    Exit:
        return hr;
    }

    // This is not multi-thread aware by any means.  That said, neither is any of this logging mechanism.
    static int s_scopeLevel = 0;
    static const ANSI s_szScopeIndent[3] = "  ";
    
    /* static */
    void DebugLog::Enter(WCHAR *pwzScope)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzScope);
            info.Append(L": Enter");
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
        
        s_scopeLevel++;
    }

    /* static */
    void DebugLog::Leave(WCHAR *pwzScope)
    {
        HRESULT hr = S_OK;
        
        s_scopeLevel--;
        
        EX_TRY
        {
            PathString info;

            info.Append(pwzScope);
            info.Append(L": Leave(void)");
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::LeaveHR(WCHAR   *pwzScope,
                           HRESULT  hrLog)
    {
        HRESULT hr = S_OK;

        s_scopeLevel--;
        
        EX_TRY
        {
            PathString info;

            info.Append(pwzScope);
            info.Append(L": Leave(hr=");
            GetStringFromHR(hrLog, info);
            info.Append(L")");

            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::LeaveBool(WCHAR   *pwzScope,
                             BOOL     fResult)
    {
        HRESULT hr = S_OK;

        s_scopeLevel--;

        EX_TRY
        {
            PathString info;

            info.Append(pwzScope);
            info.Append(L": Leave(fResult=");
            info.Append(fResult ? L"TRUE)" : L"FALSE)");
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::Log(WCHAR *pwzComment)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzComment);
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::Log(WCHAR   *pwzComment,
                       SString &value)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzComment);
            info.Append(L" = '");
            info.Append(value);
            info.Append(L"'");

            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::Log(WCHAR   *pwzComment,
                       const WCHAR   *value)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzComment);
            info.Append(L" = '");
            info.Append(value);
            info.Append(L"'");

            Log(info);
        }
        EX_CATCH_HRESULT(hr)
    }
    
    /* static */
    void DebugLog::Log(WCHAR   *pwzComment,
                       HRESULT  hrLog)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzComment);
            info.Append(L" = ");
            GetStringFromHR(hrLog, info);
        
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::Log(WCHAR        *pwzComment,
                       AssemblyName *pAssemblyName)
    {
        EX_TRY
        {
            PathString assemblyDisplayName;
            PathString info;

            if (pAssemblyName != NULL)
            {
                pAssemblyName->GetDisplayName(assemblyDisplayName,
                                              AssemblyName::INCLUDE_VERSION |
                                              AssemblyName::INCLUDE_ARCHITECTURE |
                                              AssemblyName::INCLUDE_RETARGETABLE);
            }
            else
            {
                assemblyDisplayName.Set(L"<NULL>");
            }

            info.Printf(L"(%d):", static_cast<INT32>(assemblyDisplayName.GetCount()));
            info.Append(assemblyDisplayName);

            Log(pwzComment, info);
        }
        EX_CATCH
        {
            Log(L"<AssemblyDisplayName: Failure>");
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    /* static */
    void DebugLog::Log(WCHAR *pwzComment,
                       void  *pData)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            PathString info;

            info.Append(pwzComment);
            info.AppendPrintf(L" = %p", pData);
            Log(info);
        }
        EX_CATCH_HRESULT(hr);
    }

    /* static */
    void DebugLog::Log(SString &info)
    {
        HRESULT hr = S_OK;

        StackScratchBuffer scratchBuffer;
        const BYTE *pbRawBuffer = reinterpret_cast<const BYTE *>(info.GetANSI(scratchBuffer));
        DWORD dwcbRawBuffer = static_cast<DWORD>(info.GetCount());
        // Work around SString issue
        const ANSI ansiCRLF[] = { 0x0d, 0x0a };
        DWORD dwcbAnsiCRLF = 2 * sizeof(ANSI);
        s_scopeLevel;
        for (int iScope = 0; iScope < s_scopeLevel; iScope++)
        {
            IF_FAIL_GO(WriteToFile(g_BinderVariables->m_hDebugLogFile, reinterpret_cast<const BYTE *>(&s_szScopeIndent[0]), sizeof(s_szScopeIndent)));
        }
        IF_FAIL_GO(WriteToFile(g_BinderVariables->m_hDebugLogFile, pbRawBuffer, dwcbRawBuffer));
        IF_FAIL_GO(WriteToFile(g_BinderVariables->m_hDebugLogFile,
                               reinterpret_cast<const BYTE *>(ansiCRLF),
                               dwcbAnsiCRLF));
        // Don't cache anything
        if (!FlushFileBuffers(g_BinderVariables->m_hDebugLogFile))
        {
            WszOutputDebugString(L"DebugLog::Log(info): FlushFileBuffers failed!\n");
        }

    Exit:
        return;
    }
};

#endif
