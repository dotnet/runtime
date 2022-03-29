// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_iossupportversion.h"
#include <stdlib.h>
#include "pal_utilities.h"

// These functions should not be used, but they need to be defined
// to satisfy the tooling we used to enable redirecting P/Invokes
// for the single file scenario.
const char* SystemNative_iOSSupportVersion(void)
{
    assert_err(false, "iOS support not available on this platform.", EINVAL);
    return NULL;
}
