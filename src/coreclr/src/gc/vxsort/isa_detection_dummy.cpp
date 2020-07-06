// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include <intrin.h>

#include "do_vxsort.h"

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)

void InitSupportedInstructionSet (int32_t)
{
}

bool IsSupportedInstructionSet (InstructionSet)
{
    return false;
}
#endif // defined(TARGET_AMD64) && defined(TARGET_WINDOWS)

