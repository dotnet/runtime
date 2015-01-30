//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
/*****************************************************************************/

#ifndef _EMITDEF_H_
#define _EMITDEF_H_
/*****************************************************************************/

#if defined(_TARGET_XARCH_)
#include "emitxarch.h"
#elif defined(_TARGET_ARM_)
#include "emitarm.h"
#elif defined(_TARGET_ARM64_)
#include "emitarm64.h"
#else
  #error Unsupported or unset target architecture
#endif

/*****************************************************************************/
#endif//_EMITDEF_H_
/*****************************************************************************/
