// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef FEATURE_APPX
    static void* QCALLTYPE GetWinRTDispatcherForCurrentThread();
#endif
};
#endif // _SYNCHRONIZATIONCONTEXTNATIVE_H

