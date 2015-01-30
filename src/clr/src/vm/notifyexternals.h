//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////


#ifndef _NOTIFY_EXTERNALS_H
#define _NOTIFY_EXTERNALS_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

extern BOOL g_fComStarted;

HRESULT SetupTearDownNotifications();
VOID RemoveTearDownNotifications();

BOOL ShouldCheckLoaderLock(BOOL fForMDA = TRUE);

#include "aux_ulib.h"

#endif
