// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    handleapi.hpp

Abstract:

    Declaration of the handle management APIs



--*/

#ifndef _HANDLEAPI_HPP
#define _HANDLEAPI_HPP

#include "corunix.hpp"

namespace CorUnix
{
    PAL_ERROR
    InternalDuplicateHandle(
        CPalThread *pThread,
        HANDLE hSourceProcess,
        HANDLE hSource,
        HANDLE hTargetProcess,
        LPHANDLE phDuplicate,
        BOOL bInheritHandle,
        DWORD dwOptions
        );

    PAL_ERROR
    InternalCloseHandle(
        CPalThread *pThread,
        HANDLE hObject
        );
}

#endif // _HANDLEAPI_HPP

