// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef NO_CONFIG_H
#include <dn-config.h>
#endif
#include "dn-simdhash.h"

#include "dn-simdhash-utils.h"

#define DN_SIMDHASH_T dn_simdhash_ptr_ptr
#define DN_SIMDHASH_KEY_T void *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER(hash, key) (MurmurHash3_32_ptr(key, 0))
#define DN_SIMDHASH_KEY_EQUALS(hash, lhs, rhs) (lhs == rhs)
#if SIZEOF_VOID_P == 8
#define DN_SIMDHASH_BUCKET_CAPACITY 11
#else
#define DN_SIMDHASH_BUCKET_CAPACITY 12
#endif

#include "dn-simdhash-specialization.h"
