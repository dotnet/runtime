// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FX_VER_C_H
#define FX_VER_C_H

#include <stdbool.h>
#include <stddef.h>

#include "pal_c.h"

#ifdef __cplusplus
extern "C" {
#endif

// Simplified SemVer 2.0 version structure for C
typedef struct c_fx_ver
{
    int major;
    int minor;
    int patch;
    pal_char_t pre[256];   // prerelease label with leading '-', or empty
    pal_char_t build[256]; // build label with leading '+', or empty
} c_fx_ver_t;

// Initialize a version to empty (-1.-1.-1)
void c_fx_ver_init(c_fx_ver_t* ver);

// Initialize a version with major.minor.patch
void c_fx_ver_set(c_fx_ver_t* ver, int major, int minor, int patch);

// Check if version is empty (uninitialized)
bool c_fx_ver_is_empty(const c_fx_ver_t* ver);

// Parse a version string. Returns true on success.
bool c_fx_ver_parse(const pal_char_t* ver_str, c_fx_ver_t* out_ver, bool parse_only_production);

// Compare two versions. Returns <0, 0, >0.
int c_fx_ver_compare(const c_fx_ver_t* a, const c_fx_ver_t* b);

// Convert version to string representation. Returns pointer to out_str.
pal_char_t* c_fx_ver_as_str(const c_fx_ver_t* ver, pal_char_t* out_str, size_t out_str_len);

#ifdef __cplusplus
}
#endif

#endif // FX_VER_C_H
