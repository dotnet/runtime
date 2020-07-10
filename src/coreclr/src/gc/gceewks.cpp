// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"
#include "gchandletableimpl.h"
#include "gceventstatus.h"

#ifdef SERVER_GC
#undef SERVER_GC
#endif

namespace WKS {
#include "gcimpl.h"
#include "gcee.cpp"
}

