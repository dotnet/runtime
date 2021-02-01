// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// clr/win32.h
//
// Provides Win32-specific utility functionality.
//

//

#ifndef clr_win32_h
#define clr_win32_h

#include "winwrap.h"

namespace clr
{
    namespace win32
    {
        // Prevents an HMODULE from being unloaded until process termination.
        inline
        HRESULT PreventModuleUnload(HMODULE hMod)
        {
            if (!WszGetModuleHandleEx(
                GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
                reinterpret_cast<LPCTSTR>(hMod),
                &hMod))
            {
                return HRESULT_FROM_GetLastError();
            }

            return S_OK;
        }
    } // namespace win
} // namespace clr

#endif // clr_win32_h
