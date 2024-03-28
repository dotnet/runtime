// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

unsigned int g_str_hash (const void * v1);
int32_t g_str_equal (const void * v1, const void * v2);

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T const char *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER g_str_hash
#define DN_SIMDHASH_KEY_COMPARER g_str_equal
#define DN_SIMDHASH_KEY_IS_POINTER 1
#define DN_SIMDHASH_VALUE_IS_POINTER 1

#include "dn-simdhash-specialization.h"
