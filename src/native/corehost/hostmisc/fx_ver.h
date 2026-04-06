// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FX_VER_H
#define FX_VER_H

#include <stdbool.h>
#include <stddef.h>

#include "pal.h"

#ifdef __cplusplus
extern "C" {
#endif

// Simplified SemVer 2.0 version structure for C
typedef struct c_fx_ver
{
    int major;
    int minor;
    int patch;
    pal_char_t* pre;   // prerelease label with leading '-', or NULL/empty
    pal_char_t* build; // build label with leading '+', or NULL/empty
} c_fx_ver_t;

// Initialize a version to empty (-1.-1.-1)
void c_fx_ver_init(c_fx_ver_t* ver);

// Free dynamically allocated fields
void c_fx_ver_cleanup(c_fx_ver_t* ver);

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

#ifdef __cplusplus
#include "pal.h"

// Note: This is intended to implement SemVer 2.0
struct fx_ver_t
{
    fx_ver_t();
    fx_ver_t(int major, int minor, int patch);
    // if not empty pre contains valid prerelease label with leading '-'
    fx_ver_t(int major, int minor, int patch, const pal::string_t& pre);
    // if not empty pre contains valid prerelease label with leading '-'
    // if not empty build contains valid build label with leading '+'
    fx_ver_t(int major, int minor, int patch, const pal::string_t& pre, const pal::string_t& build);

    int get_major() const { return m_major; }
    int get_minor() const { return m_minor; }
    int get_patch() const { return m_patch; }

    bool is_prerelease() const { return !m_pre.empty(); }

    bool is_empty() const { return m_major == -1; }

    pal::string_t as_str() const;

    bool operator ==(const fx_ver_t& b) const;
    bool operator !=(const fx_ver_t& b) const;
    bool operator <(const fx_ver_t& b) const;
    bool operator >(const fx_ver_t& b) const;
    bool operator <=(const fx_ver_t& b) const;
    bool operator >=(const fx_ver_t& b) const;

    static bool parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production = false);

private:
    int m_major;
    int m_minor;
    int m_patch;
    pal::string_t m_pre;
    pal::string_t m_build;

    static int compare(const fx_ver_t& a, const fx_ver_t& b);
};
#endif // __cplusplus

#endif // FX_VER_H
