// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//////////////////////////////////////////////////////////////////////////////

#if defined(_TARGET_XARCH_)
#include "emitfmtsxarch.h"
#elif defined(_TARGET_ARM_)
#include "emitfmtsarm.h"
#elif defined(_TARGET_ARM64_)
#include "emitfmtsarm64.h"
#else
#error Unsupported or unset target architecture
#endif // target type
