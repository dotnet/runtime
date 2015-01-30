//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Header: SynchronizationContextNative.h
**
**
** Purpose: Native methods on System.Threading.SynchronizationContext.
**
**
===========================================================*/

#ifndef _SYNCHRONIZATIONCONTEXTNATIVE_H
#define _SYNCHRONIZATIONCONTEXTNATIVE_H

class SynchronizationContextNative
{
public:    

#ifdef FEATURE_SYNCHRONIZATIONCONTEXT_WAIT
    static FCDECL3(DWORD, WaitHelper, PTRArray *handleArrayUNSAFE, CLR_BOOL waitAll, DWORD millis);
#endif

#ifdef FEATURE_APPX
    static void* QCALLTYPE GetWinRTDispatcherForCurrentThread();
    static void Cleanup();
#endif
};
#endif // _SYNCHRONIZATIONCONTEXTNATIVE_H

