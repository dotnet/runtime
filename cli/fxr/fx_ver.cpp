// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "fx_ver.h"

fx_ver_t::fx_ver_t(int major, int minor, int patch, const pal::string_t& pre, const pal::string_t& build)
    : m_major(major)
    , m_minor(minor)
    , m_patch(patch)
    , m_pre(pre)
    , m_build(build)
{
}

fx_ver_t::fx_ver_t(int major, int minor, int patch, const pal::string_t& pre)
    : fx_ver_t(major, minor, patch, pre, _X(""))
{
}

fx_ver_t::fx_ver_t(int major, int minor, int patch)
    : fx_ver_t(major, minor, patch, _X(""), _X(""))
{
}

bool fx_ver_t::operator ==(const fx_ver_t& b) const
{
    return compare(*this, b) == 0;
}

bool fx_ver_t::operator !=(const fx_ver_t& b) const
{
    return !operator ==(b);
}

bool fx_ver_t::operator <(const fx_ver_t& b) const
{
    return compare(*this, b) < 0;
}

bool fx_ver_t::operator >(const fx_ver_t& b) const
{
    return compare(*this, b) > 0;
}

pal::string_t fx_ver_t::as_str()
{
    pal::stringstream_t stream;
    stream << m_major << _X(".") << m_minor << _X(".") << m_patch;
    if (!m_pre.empty())
    {
        stream << m_pre;
    }
    if (!m_build.empty())
    {
        stream << _X("+") << m_build;
    }
    return stream.str();
}

/* static */
int fx_ver_t::compare(const fx_ver_t&a, const fx_ver_t& b, bool ignore_build)
{
    // compare(u.v.w-p+b, x.y.z-q+c)
    return
    (a.m_major == b.m_major)
        ? ((a.m_minor == b.m_minor)
            ? ((a.m_patch == b.m_patch)
                ? ((a.m_pre.empty() == b.m_pre.empty())
                    ? ((a.m_pre.empty())
                        ? (ignore_build ? 0 : a.m_build.compare(b.m_build))
                        : a.m_pre.compare(b.m_pre))
                    : a.m_pre.empty() ? 1 : -1)
                : (a.m_patch > b.m_patch ? 1 : -1))
            : (a.m_minor > b.m_minor ? 1 : -1))
        : ((a.m_major > b.m_major) ? 1 : -1)
        ;
}

bool try_stou(const pal::string_t& str, unsigned* num)
{
    if (str.empty())
    {
        return false;
    }
    if (str.find_first_not_of(_X("0123456789")) != pal::string_t::npos)
    {
        return false;
    }
    *num = (unsigned) std::stoul(str);
    return true;
}

bool parse_internal(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    size_t maj_start = 0;
    size_t maj_sep = ver.find(_X('.'));
    if (maj_sep == pal::string_t::npos)
    {
        return false;
    }
    unsigned major = 0;
    if (!try_stou(ver.substr(maj_start, maj_sep), &major))
    {
        return false;
    }

    size_t min_start = maj_sep + 1;
    size_t min_sep = ver.find(_X('.'), min_start);
    if (min_sep == pal::string_t::npos)
    {
        return false;
    }

    unsigned minor = 0;
    if (!try_stou(ver.substr(min_start, min_sep - min_start), &minor))
    {
        return false;
    }

    unsigned patch = 0;
    size_t pat_start = min_sep + 1;
    size_t pat_sep = ver.find_first_not_of(_X("0123456789"), pat_start);
    if (pat_sep == pal::string_t::npos)
    {
        if (!try_stou(ver.substr(pat_start), &patch))
        {
            return false;
        }

        *fx_ver = fx_ver_t(major, minor, patch);
        return true;
    }

    if (parse_only_production)
    {
        // This is a prerelease or has build suffix.
        return false;
    }

    if (!try_stou(ver.substr(pat_start, pat_sep - pat_start), &patch))
    {
        return false;
    }

    size_t pre_start = pat_sep;
    size_t pre_sep = ver.find(_X('+'), pre_start);
    if (pre_sep == pal::string_t::npos)
    {
        *fx_ver = fx_ver_t(major, minor, patch, ver.substr(pre_start));
        return true;
    }
    else
    {
        size_t build_start = pre_sep + 1;
        *fx_ver = fx_ver_t(major, minor, patch, ver.substr(pre_start, pre_sep - pre_start), ver.substr(build_start));
        return true;
    }
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    bool valid = parse_internal(ver, fx_ver, parse_only_production);
    assert(!valid || fx_ver->as_str() == ver);
    return valid;
}
