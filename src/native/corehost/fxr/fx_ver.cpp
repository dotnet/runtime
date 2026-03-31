// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C++ wrapper around the C implementation in fx_ver_c.h / fx_ver.c.
// On Unix, pal::char_t == char so strings are passed directly.
// On Windows, pal::char_t == wchar_t so ASCII version strings are
// converted between narrow and wide representations.

#include <cassert>
#include "pal.h"
#include "fx_ver.h"
#include "fx_ver_c.h"

// Helper to convert a narrow C string to pal::string_t
static inline pal::string_t narrow_to_palstr(const char* s)
{
#if defined(_WIN32)
    pal::string_t result;
    for (const char* p = s; *p; ++p)
        result.push_back(static_cast<pal::char_t>(*p));
    return result;
#else
    return pal::string_t(s);
#endif
}

// Helper to convert a pal::string_t to narrow C string (for ASCII content like version strings)
static inline std::string palstr_to_narrow(const pal::string_t& s)
{
#if defined(_WIN32)
    std::string result;
    result.reserve(s.size());
    for (pal::char_t c : s)
        result.push_back(static_cast<char>(c));
    return result;
#else
    return s;
#endif
}

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

fx_ver_t::fx_ver_t()
    : fx_ver_t(-1, -1, -1, _X(""), _X(""))
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

bool fx_ver_t::operator <=(const fx_ver_t& b) const
{
    return compare(*this, b) <= 0;
}

bool fx_ver_t::operator >=(const fx_ver_t& b) const
{
    return compare(*this, b) >= 0;
}

pal::string_t fx_ver_t::as_str() const
{
    c_fx_ver_t c_ver;
    c_fx_ver_set(&c_ver, m_major, m_minor, m_patch);

    std::string pre_narrow = palstr_to_narrow(m_pre);
    std::string build_narrow = palstr_to_narrow(m_build);

    size_t pre_len = pre_narrow.size();
    size_t build_len = build_narrow.size();
    if (pre_len >= sizeof(c_ver.pre))
        pre_len = sizeof(c_ver.pre) - 1;
    if (build_len >= sizeof(c_ver.build))
        build_len = sizeof(c_ver.build) - 1;

    memcpy(c_ver.pre, pre_narrow.c_str(), pre_len);
    c_ver.pre[pre_len] = '\0';
    memcpy(c_ver.build, build_narrow.c_str(), build_len);
    c_ver.build[build_len] = '\0';

    char buf[512];
    c_fx_ver_as_str(&c_ver, buf, sizeof(buf));
    return narrow_to_palstr(buf);
}

/* static */
int fx_ver_t::compare(const fx_ver_t& a, const fx_ver_t& b)
{
    c_fx_ver_t c_a, c_b;
    c_fx_ver_set(&c_a, a.m_major, a.m_minor, a.m_patch);
    c_fx_ver_set(&c_b, b.m_major, b.m_minor, b.m_patch);

    std::string a_pre = palstr_to_narrow(a.m_pre);
    std::string b_pre = palstr_to_narrow(b.m_pre);

    size_t a_pre_len = a_pre.size();
    size_t b_pre_len = b_pre.size();
    if (a_pre_len >= sizeof(c_a.pre))
        a_pre_len = sizeof(c_a.pre) - 1;
    if (b_pre_len >= sizeof(c_b.pre))
        b_pre_len = sizeof(c_b.pre) - 1;

    memcpy(c_a.pre, a_pre.c_str(), a_pre_len);
    c_a.pre[a_pre_len] = '\0';
    memcpy(c_b.pre, b_pre.c_str(), b_pre_len);
    c_b.pre[b_pre_len] = '\0';

    return c_fx_ver_compare(&c_a, &c_b);
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    std::string narrow_ver = palstr_to_narrow(ver);
    c_fx_ver_t c_ver;

    if (!c_fx_ver_parse(narrow_ver.c_str(), &c_ver, parse_only_production))
        return false;

    pal::string_t pre = narrow_to_palstr(c_ver.pre);
    pal::string_t build = narrow_to_palstr(c_ver.build);

    *fx_ver = fx_ver_t(c_ver.major, c_ver.minor, c_ver.patch, pre, build);

    assert(fx_ver->as_str() == ver);
    return true;
}
