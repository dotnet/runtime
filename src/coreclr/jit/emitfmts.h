// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//////////////////////////////////////////////////////////////////////////////

#if defined(TARGET_XARCH)
#include "emitfmtsxarch.h"
#elif defined(TARGET_ARM)
#include "emitfmtsarm.h"
#elif defined(TARGET_ARM64)
#include "emitfmtsarm64.h"
#elif defined(TARGET_LOONGARCH64)
#include "emitfmtsloongarch64.h"
#elif defined(TARGET_RISCV64)
#include "emitfmtsriscv64.h"
#elif defined(TARGET_S390X)
#include "emitfmtss390x.h"
#else
#error Unsupported or unset target architecture
#endif // target type
