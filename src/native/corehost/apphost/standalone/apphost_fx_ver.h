// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_FX_VER_H
#define APPHOST_FX_VER_H

#include <stdbool.h>

// Simplified SemVer 2.0 version structure for C
typedef struct fx_ver
{
    int major;
    int minor;
    int patch;
    char pre[256];   // prerelease label with leading '-', or empty
    char build[256]; // build label with leading '+', or empty
} fx_ver_t;

// Initialize a version to empty (-1.-1.-1)
void fx_ver_init(fx_ver_t* ver);

// Initialize a version with major.minor.patch
void fx_ver_set(fx_ver_t* ver, int major, int minor, int patch);

// Check if version is empty (uninitialized)
bool fx_ver_is_empty(const fx_ver_t* ver);

// Parse a version string. Returns true on success.
bool fx_ver_parse(const char* ver_str, fx_ver_t* out_ver, bool parse_only_production);

// Compare two versions. Returns <0, 0, >0.
int fx_ver_compare(const fx_ver_t* a, const fx_ver_t* b);

// Convert version to string representation. Returns pointer to out_str.
char* fx_ver_as_str(const fx_ver_t* ver, char* out_str, size_t out_str_len);

#endif // APPHOST_FX_VER_H
