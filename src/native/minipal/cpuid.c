// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cpuid.h"

#if defined(HOST_X86) || defined(HOST_AMD64)
#if defined(HOST_UNIX)

#if !__has_builtin(__cpuid)
extern void __cpuid(int cpuInfo[4], int function_id);
#endif

#if !__has_builtin(__cpuidex)
extern void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id);
#endif

#endif // HOST_UNIX
#endif // defined(HOST_X86) || defined(HOST_AMD64)
