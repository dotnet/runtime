// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"
#include "intrinsics.h"

#include "..\jit\lookupintrinsic.h"

NamedIntrinsic GetNamedIntrinsic(COMP_HANDLE compHnd, CORINFO_METHOD_HANDLE compMethod, CORINFO_METHOD_HANDLE method)
{
    // HACK: Set hw intrinsic lookup to nullptr, this will make it behave as if the feature flag is disabled, and then
    //  fall back to the intrinsic lookup path for targets without hw intrinsics (which behaves how we want for now).
    NamedIntrinsicLookup lookup(nullptr, compHnd, compMethod, 0, true, nullptr);
    return lookup.lookupNamedIntrinsic(method);
}
