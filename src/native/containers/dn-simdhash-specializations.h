// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_SPECIALIZATIONS_H__
#define __DN_SIMDHASH_SPECIALIZATIONS_H__

#include "dn-simdhash.h"

typedef struct dn_simdhash_str_key dn_simdhash_str_key;

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T dn_simdhash_str_key
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_ACCESSOR_SUFFIX _raw

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T
#undef DN_SIMDHASH_ACCESSOR_SUFFIX

#include "dn-simdhash-string-ptr.h"


#define DN_SIMDHASH_T dn_simdhash_u32_ptr
#define DN_SIMDHASH_KEY_T uint32_t
#define DN_SIMDHASH_VALUE_T void *

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T


#define DN_SIMDHASH_T dn_simdhash_ptr_ptr
#define DN_SIMDHASH_KEY_T void *
#define DN_SIMDHASH_VALUE_T void *

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T


#define DN_SIMDHASH_T dn_simdhash_ght
#define DN_SIMDHASH_KEY_T void *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_NO_DEFAULT_NEW 1

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T
#undef DN_SIMDHASH_NO_DEFAULT_NEW

#include "dn-simdhash-ght-compatible.h"


typedef struct dn_ptrpair_t {
    void *first, *second;
} dn_ptrpair_t;

#define DN_SIMDHASH_T dn_simdhash_ptrpair_ptr
#define DN_SIMDHASH_KEY_T dn_ptrpair_t
#define DN_SIMDHASH_VALUE_T void *

#include "dn-simdhash-specialization-declarations.h"

#undef DN_SIMDHASH_T
#undef DN_SIMDHASH_KEY_T
#undef DN_SIMDHASH_VALUE_T

#endif
