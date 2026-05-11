// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_X86)
#include "registerx86.h"
#elif defined(TARGET_AMD64)
#include "registeramd64.h"
#elif defined(TARGET_ARM)
#include "registerarm.h"
#elif defined(TARGET_ARM64)
#include "registerarm64.h"
#elif defined(TARGET_LOONGARCH64)
#include "registerloongarch64.h"
#elif defined(TARGET_RISCV64)
#include "registerriscv64.h"
#elif defined(TARGET_WASM)
#include "registerwasm.h"
#else
#error Unsupported or unset target architecture
#endif
