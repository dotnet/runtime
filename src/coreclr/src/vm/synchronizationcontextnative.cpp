// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Implementation: SynchronizationContextNative.cpp
**
**
** Purpose: Native methods on System.Threading.SynchronizationContext.
**
**
===========================================================*/

#include "common.h"

#ifdef FEATURE_APPX
#include <roapi.h>
#include <windows.ui.core.h>
#include "winrtdispatcherqueue.h"
#include "synchronizationcontextnative.h"

Volatile<ABI::Windows::UI::Core::ICoreWindowStatic*> g_pICoreWindowStatic;

void* QCALLTYPE SynchronizationContextNative::GetWinRTDispatcherForCurrentThread()
{
    QCALL_CONTRACT;
    void* result = NULL;
    BEGIN_QCALL;

    _ASSERTE(WinRTSupported());

    END_QCALL;
    return result;
}

#endif //FEATURE_APPX
