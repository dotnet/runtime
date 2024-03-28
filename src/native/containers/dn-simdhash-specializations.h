// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_SPECIALIZATIONS_H__
#define __DN_SIMDHASH_SPECIALIZATIONS_H__

#include "dn-simdhash.h"

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T const char *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_IS_POINTER 1
#define DN_SIMDHASH_VALUE_IS_POINTER 1

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T
#undef DN_SIMDHASH_KEY_IS_POINTER
#undef DN_SIMDHASH_VALUE_IS_POINTER

#endif
