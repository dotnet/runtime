// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This header provides general directory-related file system services.

#ifndef _clr_fs_Dir_h_
#define _clr_fs_Dir_h_

#include "clrtypes.h"
#include "clr/str.h"
#include "strsafe.h"

#ifndef countof
    #define countof(x) (sizeof(x) / sizeof(x[0]))
#endif // !countof

namespace clr
{
    namespace fs
    {
        class Dir
        {
        public:
            static inline bool Exists(
                LPCWSTR wzDirPath)
            {
                DWORD attrs = WszGetFileAttributes(wzDirPath);
                return (attrs != INVALID_FILE_ATTRIBUTES) && (attrs & FILE_ATTRIBUTE_DIRECTORY);
            }

            //-----------------------------------------------------------------------------------------
            // Creates new directory indicated by wzDirPath.
            //
            // Returns:
            //                          S_OK - on success directory creation
            //                       S_FALSE - when directory previously existed
            //      HR(ERROR_PATH_NOT_FOUND) - when creation of dir fails.
            static inline HRESULT Create(
                LPCWSTR wzDirPath)
            {
                HRESULT hr = S_OK;

                if (!WszCreateDirectory(wzDirPath, nullptr))
                {
                    hr = HRESULT_FROM_GetLastError();
                    if (hr == HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
                    {
                        hr = S_FALSE;
                    }
                }
                return hr;
            }

            //-----------------------------------------------------------------------------------------
            // Creates the specified directory and all required subdirectories. wzDirPath will be
            // temporarily modified in the process.
            //
            // Returns:
            //                          S_OK - on success directory creation
            //                       S_FALSE - when directory previously existed
            //      HR(ERROR_PATH_NOT_FOUND) - when creation of any dir fails.
            static inline HRESULT CreateRecursively(
                __inout_z LPWSTR wzDirPath,
                size_t cchDirPath = 0)
            {
                HRESULT hr = S_OK;

                if (wzDirPath == nullptr)
                {
                    return E_POINTER;
                }

                if (cchDirPath == 0)
                {
                    cchDirPath = wcslen(wzDirPath);
                }

                // Try to create the path. If it fails, assume that's because the parent folder does
                // not exist. Try to create the parent then re-attempt.
                WCHAR chOrig = wzDirPath[cchDirPath];
                hr = Create(wzDirPath);
                if (hr == HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND))
                {
                    for (WCHAR* pCurCh = wzDirPath + cchDirPath - 1; pCurCh != wzDirPath; --pCurCh)
                    {
                        if (*pCurCh == W('\\') || *pCurCh == W('\0'))
                        {
                            WCHAR chOrig = *pCurCh;
                            *pCurCh = W('\0');
                            IfFailRet(CreateRecursively(wzDirPath, pCurCh - wzDirPath));
                            *pCurCh = chOrig;
                            break;
                        }
                    }
                    IfFailRet(Create(wzDirPath));
                }

                return hr;
            }

            //-----------------------------------------------------------------------------------------
            // Creates the specified directory and all required subdirectories.
            static inline HRESULT CreateRecursively(
                LPCWSTR wzDirPath)
            {
                HRESULT hr = S_OK;

                if (wzDirPath == nullptr)
                {
                    return E_POINTER;
                }

                // Make a writable copy of wzDirPath
                size_t cchDirPath = wcslen(wzDirPath);
                CQuickWSTR wzBuffer;
                IfFailRet(wzBuffer.ReSizeNoThrow(cchDirPath + 1));
                wcscpy_s(wzBuffer.Ptr(), wzBuffer.Size(), wzDirPath);
                IfFailRet(CreateRecursively(wzBuffer.Ptr(), cchDirPath));

                return hr;
            }
        };
    }
}
        
#endif // _clr_fs_Dir_h_
