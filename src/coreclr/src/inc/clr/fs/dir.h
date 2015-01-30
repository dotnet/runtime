//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
                    IfFailRet(StringCchLength(wzDirPath, _MAX_PATH, &cchDirPath));
                }

                if (cchDirPath >= _MAX_PATH)
                {
                    return E_INVALIDARG;
                }

                // Try to create the path. If it fails, assume that's because the parent folder does
                // not exist. Try to create the parent then re-attempt.
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
                WCHAR wzBuffer[_MAX_PATH];
                WCHAR * pPathEnd;
                IfFailRet(StringCchCopyEx(wzBuffer, countof(wzBuffer), wzDirPath,
                                          &pPathEnd, nullptr, STRSAFE_NULL_ON_FAILURE));
                IfFailRet(CreateRecursively(wzBuffer, pPathEnd - wzBuffer));

                return hr;
            }
        };
    }
}
        
#endif // _clr_fs_Dir_h_
