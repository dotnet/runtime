//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
