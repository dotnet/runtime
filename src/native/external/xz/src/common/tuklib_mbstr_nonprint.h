// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_mbstr_nonprint.h
/// \brief      Find and replace non-printable characters with question marks
///
/// If mbrtowc(3) is available, it and iswprint(3) is used to check if all
/// characters are printable. Otherwise single-byte character set is assumed
/// and isprint(3) is used.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef TUKLIB_MBSTR_NONPRINT_H
#define TUKLIB_MBSTR_NONPRINT_H

#include "tuklib_common.h"
TUKLIB_DECLS_BEGIN

#define tuklib_has_nonprint TUKLIB_SYMBOL(tuklib_has_nonprint)
extern bool tuklib_has_nonprint(const char *str);
///<
/// \brief      Check if a string contains any non-printable characters
///
/// \return     false if str contains only valid multibyte characters and
///             iswprint(3) returns non-zero for all of them; true otherwise.
///             The value of errno is preserved.
///
/// \note       In case mbrtowc(3) isn't available, single-byte character set
///             is assumed and isprint(3) is used instead of iswprint(3).

#define tuklib_mask_nonprint_r TUKLIB_SYMBOL(tuklib_mask_nonprint_r)
extern const char *tuklib_mask_nonprint_r(const char *str, char **mem);
///<
/// \brief      Replace non-printable characters with question marks
///
/// \param      str     Untrusted string, for example, a filename
/// \param      mem     This function always calls free(*mem) to free the old
///                     allocation and then sets *mem = NULL. Before the first
///                     call, *mem should be initialized to NULL. If this
///                     function needs to allocate memory for a modified
///                     string, a pointer to the allocated memory will be
///                     stored to *mem. Otherwise *mem will remain NULL.
///
/// \return     If tuklib_has_nonprint(str) returns false, this function
///             returns str. Otherwise memory is allocated to hold a modified
///             string and a pointer to that is returned. The pointer to the
///             allocated memory is also stored to *mem. A modified string
///             has the problematic characters replaced by '?'. If memory
///             allocation fails, "???" is returned and *mem is NULL.
///             The value of errno is preserved.

#define tuklib_mask_nonprint TUKLIB_SYMBOL(tuklib_mask_nonprint)
extern const char *tuklib_mask_nonprint(const char *str);
///<
/// \brief      Replace non-printable characters with question marks
///
/// This is a convenience function for single-threaded use. This calls
/// tuklib_mask_nonprint_r() using an internal static variable to hold
/// the possible allocation.
///
/// \param      str     Untrusted string, for example, a filename
///
/// \return     See tuklib_mask_nonprint_r().
///
/// \note       This function is not thread safe!

TUKLIB_DECLS_END
#endif
