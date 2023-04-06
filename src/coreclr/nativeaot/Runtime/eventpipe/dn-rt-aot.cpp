// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.base.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"

#include "containers/dn-rt.h"

DN_NORETURN void
dn_rt_aot_failfast_msgv (const char* fmt, va_list ap)
{
    RhFailFast();
    UNREACHABLE();
}

DN_NORETURN void
dn_rt_aot_failfast_nomsg(const char* file, int line)
{
    RhFailFast();
    UNREACHABLE();
}

