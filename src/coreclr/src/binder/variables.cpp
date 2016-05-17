// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Variables.cpp
//


//
// Implements the Variables class
//
// ============================================================

#include "variables.hpp"

#include "ex.h"

namespace BINDER_SPACE
{
#ifdef FEATURE_VERSIONING_LOG
    namespace
    {
        HRESULT CheckFileExistence(LPCWSTR pwzFile, LPDWORD pdwAttrib)
        {
            HRESULT hr = S_FALSE;
            DWORD dwRet = 0;

            _ASSERTE(pwzFile && pdwAttrib);

            *pdwAttrib = 0;

            dwRet = WszGetFileAttributes(pwzFile);
            if (dwRet == INVALID_FILE_ATTRIBUTES)
            {
                hr = HRESULT_FROM_GetLastError();

                if ((hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)) ||
                    (hr == HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND)))
                {
                    GO_WITH_HRESULT(S_FALSE);
                }
            }
            else
            {
                *pdwAttrib = dwRet;
                GO_WITH_HRESULT(S_OK);
            }

        Exit:
            return hr;
        }
    };
#endif // FEATURE_VERSIONING_LOG

    Variables *g_BinderVariables = NULL;

    Variables::Variables()
    {
        // Nothing to do here
    }

    Variables::~Variables()
    {
        // Nothing to do here
    }

    HRESULT Variables::Init()
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            // ApplicationContext string constants
            
            // AssemblyBinder string constants
            httpURLPrefix.SetLiteral(W("http://"));

            // AssemblyName string constants
            architectureMSIL.SetLiteral(W("MSIL"));
            architectureX86.SetLiteral(W("x86"));
            architectureAMD64.SetLiteral(W("AMD64"));
            architectureARM.SetLiteral(W("ARM"));
            architectureARM64.SetLiteral(W("ARM64"));
            cultureNeutral.SetLiteral(W("neutral"));
            mscorlib.SetLiteral(CoreLibName_W);
            
            emptyString.Clear();

#ifdef FEATURE_VERSIONING_LOG
            REGUTIL::CORConfigLevel kCorConfigLevel =
                static_cast<REGUTIL::CORConfigLevel>(REGUTIL::COR_CONFIG_ENV |
                                                     REGUTIL::COR_CONFIG_FUSION);

            DWORD dwLoggingNeeded = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_ForceLog,
                                                            0,
                                                            kCorConfigLevel,
                                                            TRUE);
            fLoggingNeeded = (dwLoggingNeeded ? TRUE : FALSE);

            NewArrayHolder<WCHAR> pwzLogDirectory = REGUTIL::GetConfigString_DontUse_(CLRConfig::INTERNAL_LogPath,
                                                              TRUE,
                                                              kCorConfigLevel,
                                                              FALSE /* fUsePerfCache */);

            // When no directory is specified, we can't log.
            if (pwzLogDirectory == NULL)
            {
                fLoggingNeeded = FALSE;
            }
            else
            {
                DWORD dwAttr = 0;

                // If we do not get a regular directory, then we can't log either
                hr = CheckFileExistence(pwzLogDirectory, &dwAttr);
                if ((hr == S_OK) && ((dwAttr & FILE_ATTRIBUTE_DIRECTORY) != 0))
                {
                    logPath.Set(pwzLogDirectory);
                }
                else
                {
                    // Any failure here simply yields no logging.
                    hr = S_OK;
                    fLoggingNeeded = FALSE;
                }
            }
#endif // FEATURE_VERSIONING_LOG
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }
};
