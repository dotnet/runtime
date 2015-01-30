//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
        DWORD dwDesiredAccess,
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

