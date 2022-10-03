// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FX_VER_H__
#define __FX_VER_H__

#include <pal.h>

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
    pal::string_t prerelease_glob() const;
    pal::string_t patch_glob() const;

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

    static int compare(const fx_ver_t&a, const fx_ver_t& b);
};

#endif // __FX_VER_H__
