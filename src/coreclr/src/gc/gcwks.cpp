// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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

namespace WKS {
#include "gcimpl.h"
#include "gc.cpp"
}

