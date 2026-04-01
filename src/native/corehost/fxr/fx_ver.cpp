// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C++ wrapper around the C implementation in fx_ver_c.h / fx_ver.c.
// The C API uses pal_char_t, so strings are passed directly without conversion.

#include <cassert>
#include <minipal/utils.h>
#include "pal.h"
#include "fx_ver.h"
#include "fx_ver_c.h"

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

    size_t pre_len = m_pre.size();
    size_t build_len = m_build.size();
    if (pre_len >= ARRAY_SIZE(c_ver.pre))
        pre_len = ARRAY_SIZE(c_ver.pre) - 1;
    if (build_len >= ARRAY_SIZE(c_ver.build))
        build_len = ARRAY_SIZE(c_ver.build) - 1;

    memcpy(c_ver.pre, m_pre.c_str(), pre_len * sizeof(pal_char_t));
    c_ver.pre[pre_len] = _X('\0');
    memcpy(c_ver.build, m_build.c_str(), build_len * sizeof(pal_char_t));
    c_ver.build[build_len] = _X('\0');

    pal_char_t buf[512];
    c_fx_ver_as_str(&c_ver, buf, ARRAY_SIZE(buf));
    return pal::string_t(buf);
}

/* static */
int fx_ver_t::compare(const fx_ver_t& a, const fx_ver_t& b)
{
    c_fx_ver_t c_a, c_b;
    c_fx_ver_set(&c_a, a.m_major, a.m_minor, a.m_patch);
    c_fx_ver_set(&c_b, b.m_major, b.m_minor, b.m_patch);

    size_t a_pre_len = a.m_pre.size();
    size_t b_pre_len = b.m_pre.size();
    if (a_pre_len >= ARRAY_SIZE(c_a.pre))
        a_pre_len = ARRAY_SIZE(c_a.pre) - 1;
    if (b_pre_len >= ARRAY_SIZE(c_b.pre))
        b_pre_len = ARRAY_SIZE(c_b.pre) - 1;

    memcpy(c_a.pre, a.m_pre.c_str(), a_pre_len * sizeof(pal_char_t));
    c_a.pre[a_pre_len] = _X('\0');
    memcpy(c_b.pre, b.m_pre.c_str(), b_pre_len * sizeof(pal_char_t));
    c_b.pre[b_pre_len] = _X('\0');

    return c_fx_ver_compare(&c_a, &c_b);
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    c_fx_ver_t c_ver;

    if (!c_fx_ver_parse(ver.c_str(), &c_ver, parse_only_production))
        return false;

    pal::string_t pre(c_ver.pre);
    pal::string_t build(c_ver.build);

    *fx_ver = fx_ver_t(c_ver.major, c_ver.minor, c_ver.patch, pre, build);

    assert(fx_ver->as_str() == ver);
    return true;
}
