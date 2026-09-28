// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include <minipal/random.h>
#include "dn_xxhash.h"

uint32_t xxHashDefaultTraits::GenerateGlobalSeed()
{
    static uint32_t seed = []()
    {
        uint32_t s;
        minipal_get_non_cryptographically_secure_random_bytes((uint8_t*)&s, sizeof(s));
        return s;
    }();
    return seed;
}
