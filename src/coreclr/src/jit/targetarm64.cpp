//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(_TARGET_ARM64_)

#include "target.h"

const char *                    Target::g_tgtCPUName = "arm64";
const Target::ArgOrder          Target::g_tgtArgOrder = ARG_ORDER_R2L;

#endif // _TARGET_ARM64_
