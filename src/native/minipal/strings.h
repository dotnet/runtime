// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_STRINGS_H
#define HAVE_MINIPAL_STRINGS_H

#include <minipal/types.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Convert a UTF-16 character to uppercase using invariant culture.
 *
 * @param code The UTF-16 character to be converted.
 * @return The uppercase equivalent of the character or the character itself if no conversion is necessary.
 */
CHAR16_T minipal_toupper_invariant(CHAR16_T code);

/**
 * Convert a UTF-16 character to lowercase using invariant culture.
 *
 * @param code The UTF-16 character to be converted.
 * @return The lowercase equivalent of the character or the character itself if no conversion is necessary.
 */
CHAR16_T minipal_tolower_invariant(CHAR16_T code);

/**
 * @brief Get the length of a null-terminated UTF-16 string.
 *
 * @param str The null-terminated UTF-16 string.
 * @return The length of the string.
 */
size_t minipal_u16_strlen(const CHAR16_T* str);

/**
 * @brief xplat implementation of sprintf_s.
 */
int minipal_sprintf_s(char* buffer, size_t count, const char* format, ...);

/**
 * @brief xplat implementation of strncasecmp.
 */
int minipal_strncasecmp(const char* str1, const char* str2, size_t n);

/**
 * @brief xplat implementation of strdup.
 */
char* minipal_strdup(const char *str);

/**
 * @brief xplat implementation of strcpy_s.
 */
int minipal_strcpy_s(char* dest, size_t destsz, const char* src);

/**
 * @brief xplat implementation of strncpy_s.
 */
int minipal_strncpy_s(char* dest, size_t destsz, const char* src, size_t count);

/**
 * @brief xplat implementation of strcat_s.
 */
int minipal_strcat_s(char* dest, size_t destsz, const char* src);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_STRINGS_H */
