// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// CDebugLog.cpp
//


//
// Implements the fusion-derived CDebugLog class
//
// ============================================================

#ifdef FEATURE_VERSIONING_LOG

#include "cdebuglog.hpp"
#include "applicationcontext.hpp"
#include "assemblyname.hpp"
#include "variables.hpp"
#include "utils.hpp"

#include "shlwapi.h"
#include "strsafe.h"

#include "../dlls/mscorrc/fusres.h"

#define MAX_DBG_STR_LEN 1024
#define MAX_DATE_LEN    128

#define DEBUG_LOG_HTML_START         L"<html><pre>\r\n"
#define DEBUG_LOG_HTML_META_LANGUAGE L"<meta http-equiv=\"Content-Type\" content=\"charset=unicode-1-1-utf-8\">"
#define DEBUG_LOG_MARK_OF_THE_WEB    L"<!-- saved from url=(0015)assemblybinder: -->"
#define DEBUG_LOG_HTML_END           L"\r\n</pre></html>"
#define DEBUG_LOG_NEW_LINE           L"\r\n"

namespace BINDER_SPACE
{
    namespace
    {
        inline LPCWSTR LogCategoryToString(DWORD dwLogCategory)
        {
            switch (dwLogCategory)
            {
            case FUSION_BIND_LOG_CATEGORY_DEFAULT:
                return L"default";
            case FUSION_BIND_LOG_CATEGORY_NGEN:
                return L"Native";
            default:
                return L"Unknown";
            }
        }

        HRESULT CreateFilePathHierarchy(LPCOLESTR pszName)
        {
            HRESULT hr=S_OK;
            LPTSTR  pszFileName;
            PathString szPathString;
            DWORD   dw = 0;

            size_t pszNameLen = wcslen(pszName);
            WCHAR * szPath = szPathString.OpenUnicodeBuffer(static_cast<COUNT_T>(pszNameLen));
            size_t cbSzPath = (sizeof(WCHAR)) * (pszNameLen + 1); // SString allocates extra byte for null
            IF_FAIL_GO(StringCbCopy(szPath, cbSzPath, pszName));
            szPathString.CloseBuffer(static_cast<COUNT_T>(pszNameLen));
            
            pszFileName = PathFindFileName(szPath);

            if (pszFileName <= szPath)
            {
                IF_FAIL_GO(E_INVALIDARG);
            }

            *(pszFileName-1) = 0;

            dw = WszGetFileAttributes(szPath);
            if (dw != INVALID_FILE_ATTRIBUTES)
            {
                return S_OK;
            }
        
            hr = HRESULT_FROM_GetLastError();

            switch (hr)
            {
            case __HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND):
            {
                hr =  CreateFilePathHierarchy(szPath);
                if (hr != S_OK)
                    return hr;
            }

            case __HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND):
            {
                if (WszCreateDirectory(szPath, NULL))
                    return S_OK;
                else
                {
                    hr = HRESULT_FROM_WIN32(GetLastError());
                    if(hr == HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
                        hr = S_OK;
                    else
                        return hr;
                }
            }

            default:
                break;
            }

        Exit:
            return hr;
        }

        HRESULT WriteLog(HANDLE  hLogFile,
                         LPCWSTR pwzInfo)
        {
            HRESULT hr = S_OK;
            DWORD dwLen = 0;
            DWORD dwWritten = 0;
            CHAR szBuf[MAX_DBG_STR_LEN];

            dwLen = WszWideCharToMultiByte(CP_UTF8,
                                           0,
                                           pwzInfo,
                                           -1,
                                           szBuf,
                                           MAX_DBG_STR_LEN,
                                           NULL,
                                           NULL);

            if (!dwLen)
            {
                IF_FAIL_GO(HRESULT_FROM_GetLastError());
            }

            // dwLen includes NULL terminator. We don't want to write this out.
            if (dwLen > 1) {
                dwLen--;

                if (!WriteFile(hLogFile, szBuf, dwLen, &dwWritten, NULL)) {
                    IF_FAIL_GO(HRESULT_FROM_GetLastError());
                }
            }

        Exit:
            return hr;
        }

        HRESULT GetBindTimeInfo(PathString &info)
        {
            HRESULT hr = S_OK;
            SYSTEMTIME systime;

            {
                WCHAR wzDateBuffer[MAX_DATE_LEN];
                WCHAR wzTimeBuffer[MAX_DATE_LEN];

                GetLocalTime(&systime);

                if (!WszGetDateFormat(LOCALE_USER_DEFAULT,
                                      0,
                                      &systime,
                                      NULL,
                                      wzDateBuffer,
                                      MAX_DATE_LEN))
                {
                    return HRESULT_FROM_GetLastError();
                }
    
                if (!WszGetTimeFormat(LOCALE_USER_DEFAULT,
                                      0,
                                      &systime,
                                      NULL,
                                      wzTimeBuffer,
                                      MAX_DATE_LEN))
                {
                   return HRESULT_FROM_GetLastError();
                }

                info.Printf(L"(%s @ %s)", wzDateBuffer, wzTimeBuffer);
            }
            return hr;
        }

        HRESULT GetHrResultInfo(PathString &info, HRESULT hrResult)
        {
            HRESULT hr = S_OK;

            // TODO: Get the result information in here.
            info.Printf(L"%p.", hrResult);

            return hr;
        }

        inline BOOL IsInvalidCharacter(WCHAR wcChar)
        {
            switch (wcChar)
            {
            case L':':
            case L'/':
            case L'\\':
            case L'*':
            case L'<':
            case L'>':
            case L'?':
            case L'|':
            case L'"':
                return TRUE;
            default:
                return FALSE;
            }
        }

        inline void ReplaceInvalidFileCharacters(SString &assemblyName)
        {
            SString::Iterator pos = assemblyName.Begin();
            SString::Iterator end = assemblyName.End();

            while (pos < end)
            {
                if (IsInvalidCharacter(pos[0]))
                {
                    assemblyName.Replace(pos, L'_');
                }

                pos++;
            }
        }
    };

    CDebugLog::CDebugLog()
    {
        m_cRef = 1;
    }

    CDebugLog::~CDebugLog()
    {
        // Nothing to do here
    }

    /* static */
    HRESULT CDebugLog::Create(ApplicationContext  *pApplicationContext,
                              AssemblyName        *pAssemblyName,
                              SString             &sCodeBase,
                              CDebugLog          **ppCDebugLog)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::Create");
        ReleaseHolder<CDebugLog> pDebugLog;

        // Validate input arguments
        IF_FALSE_GO(pApplicationContext != NULL);
        IF_FALSE_GO(ppCDebugLog != NULL);

        SAFE_NEW(pDebugLog, CDebugLog);
        IF_FAIL_GO(pDebugLog->Init(pApplicationContext, pAssemblyName, sCodeBase));

        *ppCDebugLog = pDebugLog.Extract();

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::Create", hr);
        return hr;
    }

    ULONG CDebugLog::AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }

    ULONG CDebugLog::Release()
    {
        ULONG ulRef;

        ulRef = InterlockedDecrement(&m_cRef);
    
        if (ulRef == 0)
        {
            delete this;
        }

        return ulRef;
    }

    HRESULT CDebugLog::SetResultCode(DWORD   dwLogCategory,
                                     HRESULT hrResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::SetResultCode");

        IF_FALSE_GO(dwLogCategory < FUSION_BIND_LOG_CATEGORY_MAX);

        m_HrResult[dwLogCategory] = hrResult;

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::SetResultCode", hr);
        return hr;
    }

    HRESULT CDebugLog::LogMessage(DWORD,
                                  DWORD    dwLogCategory, 
                                  SString &sDebugString)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::LogMessage");

        IF_FALSE_GO(dwLogCategory < FUSION_BIND_LOG_CATEGORY_MAX);

        m_content[dwLogCategory].AddTail(const_cast<const SString &>(sDebugString));

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::LogMessage", hr);
        return hr;
    }

    HRESULT CDebugLog::Flush(DWORD,
                             DWORD dwLogCategory)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::Flush");
        SmallStackSString sCategory(LogCategoryToString(dwLogCategory));
        PathString logFilePath;
        ListNode<SString> *pListNode = NULL;

        IF_FALSE_GO(dwLogCategory < FUSION_BIND_LOG_CATEGORY_MAX);

        CombinePath(g_BinderVariables->logPath, sCategory, logFilePath);
        CombinePath(logFilePath, m_applicationName, logFilePath);
        CombinePath(logFilePath, m_logFileName, logFilePath);

        BINDER_LOG_STRING(L"logFilePath", logFilePath);

        IF_FAIL_GO(CreateFilePathHierarchy(logFilePath.GetUnicode()));

        m_hLogFile = WszCreateFile(logFilePath.GetUnicode(),
                                   GENERIC_READ | GENERIC_WRITE,
                                   0,
                                   NULL,
                                   CREATE_ALWAYS,
                                   FILE_ATTRIBUTE_NORMAL,
                                   NULL);

        if (m_hLogFile == INVALID_HANDLE_VALUE)
        {
            // Silently ignore unability to log.
            BINDER_LOG(L"Unable to open binding log");
            GO_WITH_HRESULT(S_OK);
        }

        LogHeader(dwLogCategory);

        pListNode = static_cast<ListNode<SString> *>(m_content[dwLogCategory].GetHeadPosition());
        while (pListNode != NULL)
        {
            SString item = pListNode->GetItem();

            IF_FAIL_GO(WriteLog(m_hLogFile, item.GetUnicode()));
            IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_NEW_LINE));

            pListNode = pListNode->GetNext();
        }

        LogFooter(dwLogCategory);

        // Ignore failure
        CloseHandle(m_hLogFile.Extract());

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::Flush", hr);
        return hr;
    }

    HRESULT CDebugLog::Init(ApplicationContext *pApplicationContext,
                            AssemblyName       *pAssemblyName,
                            SString            &sCodeBase)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::Init");

        m_applicationName.Set(pApplicationContext->GetApplicationName());
        ReplaceInvalidFileCharacters(m_applicationName);

        if (m_applicationName.IsEmpty())
        {
            BINDER_LOG(L"empty application name");
            m_applicationName.Set(L"unknown");
        }

        if (pAssemblyName == NULL)
        {
            m_logFileName.Set(L"WhereRefBind!Host=(LocalMachine)!FileName=(");

            LPCWSTR pwzFileName = PathFindFileNameW(sCodeBase.GetUnicode());
            if (pwzFileName != NULL)
            {
                m_logFileName.Append(pwzFileName);
            }
            m_logFileName.Append(L").HTM");
        }
        else
        {
            PathString assemblyDisplayName;

            pAssemblyName->GetDisplayName(assemblyDisplayName,
                                          AssemblyName::INCLUDE_VERSION |
                                          AssemblyName::INCLUDE_ARCHITECTURE |
                                          AssemblyName::INCLUDE_RETARGETABLE);

            ReplaceInvalidFileCharacters(assemblyDisplayName);

            m_logFileName.Set(assemblyDisplayName);
            m_logFileName.Append(L".HTM");
        }

        BINDER_LOG_LEAVE_HR(L"CDebugLog::Init", hr);
        return hr;
    }

    HRESULT CDebugLog::LogHeader(DWORD dwLogCategory)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::LogHeader");
        PathString info;
        PathString temp;
        PathString format;

        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_HTML_META_LANGUAGE));
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_MARK_OF_THE_WEB));
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_HTML_START));

        IF_FAIL_GO(GetBindTimeInfo(temp));
        IF_FAIL_GO(format.LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_HEADER_BEGIN));
        info.Printf(format.GetUnicode(), temp.GetUnicode());
        IF_FAIL_GO(WriteLog(m_hLogFile, info.GetUnicode()));
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_NEW_LINE DEBUG_LOG_NEW_LINE));

        if (SUCCEEDED(m_HrResult[dwLogCategory]))
        {
            IF_FAIL_GO(temp.
                       LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_HEADER_BIND_RESULT_SUCCESS));
            IF_FAIL_GO(WriteLog(m_hLogFile, temp.GetUnicode()));
        }
        else
        {
            IF_FAIL_GO(temp.
                       LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_HEADER_BIND_RESULT_ERROR));
            IF_FAIL_GO(WriteLog(m_hLogFile, temp.GetUnicode()));
        }
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_NEW_LINE));

        GetHrResultInfo(temp, m_HrResult[dwLogCategory]);
        
        IF_FAIL_GO(format.LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_HEADER_BIND_RESULT));
        info.Printf(format.GetUnicode(), temp.GetUnicode());
        IF_FAIL_GO(WriteLog(m_hLogFile, info.GetUnicode()));
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_NEW_LINE DEBUG_LOG_NEW_LINE));

        // TODO: Assembly Manager info + Executable info.

        IF_FAIL_GO(info.LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_HEADER_END));
        IF_FAIL_GO(WriteLog(m_hLogFile, info.GetUnicode()));
        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_NEW_LINE DEBUG_LOG_NEW_LINE));

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::LogHeader", hr);
        return hr;
    }

    HRESULT CDebugLog::LogFooter(DWORD)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"CDebugLog::LogFooter");

        IF_FAIL_GO(WriteLog(m_hLogFile, DEBUG_LOG_HTML_END));

    Exit:
        BINDER_LOG_LEAVE_HR(L"CDebugLog::LogFooter", hr);
        return hr;
    }
};

#endif // FEATURE_VERSIONING_LOG
