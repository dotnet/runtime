//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//+--------------------------------------------------------------------------
//
//  Microsoft Confidential.
//
//---------------------------------------------------------------------------
//

//



#pragma once

// keep in sync with windowsidentity.cs
#define WINSECURITYCONTEXT_THREAD 1
#define WINSECURITYCONTEXT_PROCESS 2
#define WINSECURITYCONTEXT_BOTH 3



class COMPrincipal
{
public:
    static
    INT32 QCALLTYPE ImpersonateLoggedOnUser(HANDLE hToken);

    static FCDECL3(INT32, OpenThreadToken, DWORD dwDesiredAccess, DWORD dwOpenAs, SafeHandle** phThreadTokenUNSAFE);

    static
    INT32 QCALLTYPE RevertToSelf();

    static
    INT32 QCALLTYPE SetThreadToken(HANDLE hToken);

    static void CLR_ImpersonateLoggedOnUser(HANDLE hToken);
};
