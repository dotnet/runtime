// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

// keep in sync with windowsidentity.cs
#define WINSECURITYCONTEXT_THREAD 1
#define WINSECURITYCONTEXT_PROCESS 2
#define WINSECURITYCONTEXT_BOTH 3



#ifndef FEATURE_PAL
class COMPrincipal
{
public:
#ifndef FEATURE_CORECLR
    static
    INT32 QCALLTYPE ImpersonateLoggedOnUser(HANDLE hToken);

    static FCDECL3(INT32, OpenThreadToken, DWORD dwDesiredAccess, DWORD dwOpenAs, SafeHandle** phThreadTokenUNSAFE);

    static
    INT32 QCALLTYPE RevertToSelf();

    static
    INT32 QCALLTYPE SetThreadToken(HANDLE hToken);
#endif // !FEATURE_CORECLR

    static void CLR_ImpersonateLoggedOnUser(HANDLE hToken);
};
#endif // FEATURE_PAL
