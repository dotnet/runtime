// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stddef.h>

#include "common.h"
#include "gcenv.h"
#include "gc.h"
#include "gcscan.h"
#include "gchandletableimpl.h"
#include "gceventstatus.h"

#ifdef SERVER_GC
#define GC_NAMESPACE SVR
#else // SERVER_GC
#define GC_NAMESPACE WKS
#endif // SERVER_GC

#ifdef SERVER_GC
namespace SVR {
#else // SERVER_GC
namespace WKS {
#endif // SERVER_GC

#include "gcimpl.h"
#include "gcpriv.h"

}
