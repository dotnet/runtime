// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "rhbinder.h"
#include "CachedInterfaceDispatch.h"

#include "SyncClean.hpp"

void SyncClean::Terminate()
{
    CleanUp();
}

void SyncClean::CleanUp ()
{
#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
    // Update any interface dispatch caches that were unsafe to modify outside of this GC.
    ReclaimUnusedInterfaceDispatchCaches();
#endif
}
