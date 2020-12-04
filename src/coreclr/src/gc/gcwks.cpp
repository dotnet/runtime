// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"
#include "gcdesc.h"
#include "softwarewritewatch.h"
#include "handletable.h"
#include "handletable.inl"
#include "gcenv.inl"
#include "gceventstatus.h"

#ifdef SERVER_GC
#undef SERVER_GC
#endif

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
#include "vxsort/do_vxsort.h"
#endif

namespace WKS {
#include "gcimpl.h"
#include "gc.cpp"
}

