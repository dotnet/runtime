// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "guid.h"
#include "random.h"
#include <string.h>
#include <stdio.h>
#include <assert.h>
#ifdef HOST_WINDOWS
#include <Windows.h>
#endif

// See RFC-4122 section 4.4 on creation of random GUID.
// https://www.ietf.org/rfc/rfc4122.txt
//
//    The version 4 UUID is meant for generating UUIDs from truly-random or
//    pseudo-random numbers.
//
//    The algorithm is as follows:
//
//    o  Set the two most significant bits (bits 6 and 7) of the
//       clock_seq_hi_and_reserved to zero and one, respectively.
//
//    o  Set the four most significant bits (bits 12 through 15) of the
//       time_hi_and_version field to the 4-bit version number from
//       Section 4.1.3.
//
//    o  Set all the other bits to randomly (or pseudo-randomly) chosen
//       values.
//
bool minipal_guid_v4_create(GUID* guid)
{
#ifdef HOST_WINDOWS
    // Windows has a built-in function for creating v4 GUIDs.
    return SUCCEEDED(CoCreateGuid(guid));
#else
    // Technically, v4 GUIDs don't require cryptographically secure random bytes;
    // however, CoCreateGuid provides that guarantee and we want to ensure
    // that guarantee is maintained as customers have taken a dependency on it.
    if (minipal_get_cryptographically_secure_random_bytes((uint8_t*)guid, sizeof(*guid)) != 0)
        return false;

    {
        // time_hi_and_version
        const uint16_t mask  = 0xf000; // b1111000000000000
        const uint16_t value = 0x4000; // b0100000000000000
        guid->Data3 = (guid->Data3 & ~mask) | value;
    }

    {
        // clock_seq_hi_and_reserved
        const uint8_t mask  = 0xc0; // b11000000
        const uint8_t value = 0x80; // b10000000
        guid->Data4[0] = (guid->Data4[0] & ~mask) | value;
    }

    return true;
#endif
}

bool minipal_guid_equals(GUID const* g1, GUID const* g2)
{
    return memcmp(g1, g2, sizeof(GUID)) == 0;
}

void minipal_guid_as_string(GUID guid, char* guidString, uint32_t len)
{
    assert(len >= MINIPAL_GUID_BUFFER_LEN);

    int32_t nBytes = snprintf(guidString, len, "{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x}",
        guid.Data1, guid.Data2, guid.Data3,
        guid.Data4[0], guid.Data4[1],
        guid.Data4[2], guid.Data4[3],
        guid.Data4[4], guid.Data4[5],
        guid.Data4[6], guid.Data4[7]);

    (void)nBytes; // unused in release mode
    assert(nBytes == MINIPAL_GUID_BUFFER_LEN - 1);
}
