// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_GUID_H
#define MINIPAL_GUID_H

#include <stdint.h>
#include <stdbool.h>
#include <minipal/utils.h>

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

#ifdef _WIN32
#include <guiddef.h>
#else // _WIN32
typedef struct minipal_guid__
{
    uint32_t Data1;
    uint16_t Data2;
    uint16_t Data3;
    uint8_t  Data4[8];
} GUID;
#endif // _WIN32

/**
 * Define the buffer length required to store a GUID string, including the null terminator.
 *
 * This accounts for the standard GUID format: "{12345678-1234-1234-1234-123456789abc}"
 * which consists of 38 characters plus 1 null-terminating character.
 */
#define MINIPAL_GUID_BUFFER_LEN (ARRAY_SIZE("{12345678-1234-1234-1234-123456789abc}"))

/**
 * Generate a new version 4 GUID (randomly generated)
 *
 * @param guid Pointer to a GUID structure to receive the new GUID.
 * @return true if the GUID was successfully created, false otherwise.
 */
bool minipal_guid_v4_create(GUID* guid);

/**
 * Compare two GUIDs for equality
 *
 * @param g1 Pointer to the first GUID.
 * @param g2 Pointer to the second GUID.
 * @return true if the GUIDs are equal, false otherwise.
 */
bool minipal_guid_equals(GUID const* g1, GUID const* g2);

/**
 * Convert a GUID to its string representation
 *
 * @param guid The GUID to be converted.
 * @param guidString Pointer to a buffer to store the GUID string. The buffer must be at least MINIPAL_GUID_BUFFER_LEN in size.
 * @param len Length of the destination buffer.
 */
void minipal_guid_as_string(GUID guid, char* guidString, uint32_t len);

#ifdef __cplusplus
    }

#ifndef _WIN32
inline bool operator==(GUID const& a, GUID const& b)
{
    return minipal_guid_equals(&a, &b);
}

inline bool operator!=(GUID const& a, GUID const& b)
{
    return !(a == b);
}
#endif // _WIN32
#endif // __cplusplus

#endif // MINIPAL_GUID_H
