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

// Simplified SemVer 2.0 version structure for C consumers. The pre and build
// fields, when non-NULL, point to heap-allocated strings owned by the struct
// (allocated via malloc, freed with free). Use c_fx_ver_init / c_fx_ver_cleanup
// for lifetime management.
typedef struct c_fx_ver
{
    int32_t major;
    int32_t minor;
    int32_t patch;
    pal_char_t* pre;   // prerelease label with leading '-', or NULL/empty
    pal_char_t* build; // build label with leading '+', or NULL/empty
} c_fx_ver_t;

// Initialize a version to empty (-1.-1.-1) with NULL pre/build.
void c_fx_ver_init(c_fx_ver_t* ver);

// Free dynamically allocated fields and reset pre/build to NULL.
void c_fx_ver_cleanup(c_fx_ver_t* ver);

// Set major/minor/patch and clear pre/build (freeing any owned strings).
void c_fx_ver_set(c_fx_ver_t* ver, int major, int minor, int patch);

// Returns true if the version is uninitialized (major == -1).
bool c_fx_ver_is_empty(const c_fx_ver_t* ver);

// Parse a version string. On success out_ver is populated and the caller is
// responsible for calling c_fx_ver_cleanup on it. Returns false on failure;
// out_ver is left in a freshly initialized state (no allocations) in that case.
bool c_fx_ver_parse(const pal_char_t* ver_str, c_fx_ver_t* out_ver, bool parse_only_production);

// Compare two versions. Returns <0, 0, >0 (semver semantics).
int c_fx_ver_compare(const c_fx_ver_t* a, const c_fx_ver_t* b);

// Format the version into the caller-provided buffer. Returns out_str.
pal_char_t* c_fx_ver_as_str(const c_fx_ver_t* ver, pal_char_t* out_str, size_t out_str_len);

#ifdef __cplusplus
}
#endif

// ============================================================================
// C++ API: source-compat wrapper preserving the existing fx_ver_t class.
// ============================================================================

#ifdef __cplusplus

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
