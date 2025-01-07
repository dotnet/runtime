// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_UTF8_H
#define HAVE_MINIPAL_UTF8_H

#include <minipal/utils.h>
#include <minipal/types.h>
#include <stdbool.h>

#define MINIPAL_MB_NO_REPLACE_INVALID_CHARS 0x00000008
#define MINIPAL_TREAT_AS_LITTLE_ENDIAN 0x00000016
#define MINIPAL_ERROR_INSUFFICIENT_BUFFER 122L
#define MINIPAL_ERROR_NO_UNICODE_TRANSLATION 1113L

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Get length of destination needed for UTF-8 to UTF-16 (UCS-2) conversion
 *
 * @param source The source string in UTF-8 format.
 * @param sourceLength Length of the source string.
 * @param flags Flags to alter the behavior of converter. Supported flags are MINIPAL_MB_NO_REPLACE_INVALID_CHARS and MINIPAL_TREAT_AS_LITTLE_ENDIAN.
 * @return Length of UTF-16 buffer required by the conversion.
 */
size_t minipal_get_length_utf8_to_utf16(const char* source, size_t sourceLength, unsigned int flags);

/**
 * Get length of destination needed for UTF-16 (UCS-2) to UTF-8 conversion
 *
 * @param source The source string in UTF-16 format.
 * @param sourceLength Length of the source string.
 * @param flags Flags to alter the behavior of converter. Supported flags are MINIPAL_MB_NO_REPLACE_INVALID_CHARS and MINIPAL_TREAT_AS_LITTLE_ENDIAN.
 * @return Length of UTF-8 buffer required by the conversion.
 */
size_t minipal_get_length_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, unsigned int flags);

/**
 * Convert a string from UTF-8 to UTF-16 (UCS-2) with preallocated memory
 *
 * @param source The source string in UTF-8 format.
 * @param sourceLength Length of the source string.
 * @param destination Pointer to the destination UTF-16 string. It can be NULL to query number of items required by the conversion.
 * @param destinationLength Length of the destination string.
 * @param flags Flags to alter the behavior of converter. Supported flags are MINIPAL_MB_NO_REPLACE_INVALID_CHARS and MINIPAL_TREAT_AS_LITTLE_ENDIAN.
 * @return Number of items written by the conversion.
 */
size_t minipal_convert_utf8_to_utf16(const char* source, size_t sourceLength, CHAR16_T* destination, size_t destinationLength, unsigned int flags);

/**
 * Convert a string from UTF-16 (UCS-2) to UTF-8 with preallocated memory
 *
 * @param source The source string in UTF-16 format.
 * @param sourceLength Length of the source string.
 * @param destination Pointer to the destination UTF-8 string. It can be NULL to query number of items required by the conversion.
 * @param destinationLength Length of the destination string.
 * @param flags Flags to alter the behavior of converter. Supported flags are MINIPAL_MB_NO_REPLACE_INVALID_CHARS and MINIPAL_TREAT_AS_LITTLE_ENDIAN.
 * @return Number of items written by the conversion.
 */
size_t minipal_convert_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, char* destination, size_t destinationLength, unsigned int flags);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_UTF8_H */
