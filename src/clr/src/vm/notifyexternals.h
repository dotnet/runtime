// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////////


#ifndef _NOTIFY_EXTERNALS_H
#define _NOTIFY_EXTERNALS_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

extern BOOL g_fComStarted;

BOOL ShouldCheckLoaderLock(BOOL fForMDA = TRUE);

#include "aux_ulib.h"

#endif
