// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#if defined(FEATURE_SVR_GC)

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"
#include "gcdesc.h"
#include "softwarewritewatch.h"
#include "handletable.h"
#include "handletable.inl"
#include "gcenv.inl"
#include "gceventstatus.h"

#define SERVER_GC 1

namespace SVR {
#include "gcimpl.h"
#include "gc.cpp"
}

#endif // defined(FEATURE_SVR_GC)
