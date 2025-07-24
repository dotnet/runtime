// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"
#include "dn-simdhash-utils.h"

#define DN_SIMDHASH_T dn_simdhash_u32_ptr
#define DN_SIMDHASH_KEY_T uint32_t
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER(data, key) murmur3_fmix32(key)
#define DN_SIMDHASH_KEY_EQUALS(data, lhs, rhs) (lhs == rhs)

#include "dn-simdhash-specialization.h"
