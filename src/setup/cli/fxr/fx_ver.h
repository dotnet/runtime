// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"

struct fx_ver_t
{
    fx_ver_t(int major, int minor, int patch);
    fx_ver_t(int major, int minor, int patch, const pal::string_t& pre);
    fx_ver_t(int major, int minor, int patch, const pal::string_t& pre, const pal::string_t& build);

    int get_major() { return m_major; }
    int get_minor() { return m_minor; }
    int get_patch() { return m_patch; }

    void set_major(int m) { m_major = m; }
    void set_minor(int m) { m_minor = m; }
    void set_patch(int p) { m_patch = p; }

    bool is_prerelease() { return !m_pre.empty(); }

    pal::string_t as_str();

    bool operator ==(const fx_ver_t& b) const;
    bool operator !=(const fx_ver_t& b) const;
    bool operator <(const fx_ver_t& b) const;
    bool operator >(const fx_ver_t& b) const;

    static bool parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production = false);

private:
    int m_major;
    int m_minor;
    int m_patch;
    pal::string_t m_pre;
    pal::string_t m_build;

    static int compare(const fx_ver_t&a, const fx_ver_t& b, bool ignore_build = false);
};

