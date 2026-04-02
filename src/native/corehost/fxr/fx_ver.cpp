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
    c_fx_ver_init(&c_ver);
    c_ver.major = m_major;
    c_ver.minor = m_minor;
    c_ver.patch = m_patch;

    // Point to the C++ string data directly (read-only use by c_fx_ver_as_str)
    c_ver.pre = const_cast<pal_char_t*>(m_pre.c_str());
    c_ver.build = const_cast<pal_char_t*>(m_build.c_str());

    pal_char_t buf[512];
    c_fx_ver_as_str(&c_ver, buf, ARRAY_SIZE(buf));

    // Don't call c_fx_ver_cleanup - we borrowed pointers from m_pre/m_build
    c_ver.pre = NULL;
    c_ver.build = NULL;

    return pal::string_t(buf);
}

/* static */
int fx_ver_t::compare(const fx_ver_t& a, const fx_ver_t& b)
{
    c_fx_ver_t c_a, c_b;
    c_fx_ver_init(&c_a);
    c_fx_ver_init(&c_b);
    c_a.major = a.m_major;
    c_a.minor = a.m_minor;
    c_a.patch = a.m_patch;
    c_b.major = b.m_major;
    c_b.minor = b.m_minor;
    c_b.patch = b.m_patch;

    // Point to the C++ string data directly (read-only use by c_fx_ver_compare)
    c_a.pre = const_cast<pal_char_t*>(a.m_pre.c_str());
    c_b.pre = const_cast<pal_char_t*>(b.m_pre.c_str());

    int result = c_fx_ver_compare(&c_a, &c_b);

    // Don't call c_fx_ver_cleanup - we borrowed pointers from m_pre
    c_a.pre = NULL;
    c_b.pre = NULL;

    return result;
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    c_fx_ver_t c_ver;
    c_fx_ver_init(&c_ver);

    if (!c_fx_ver_parse(ver.c_str(), &c_ver, parse_only_production))
    {
        c_fx_ver_cleanup(&c_ver);
        return false;
    }

    pal::string_t pre(c_ver.pre != NULL ? c_ver.pre : _X(""));
    pal::string_t build(c_ver.build != NULL ? c_ver.build : _X(""));

    *fx_ver = fx_ver_t(c_ver.major, c_ver.minor, c_ver.patch, pre, build);

    c_fx_ver_cleanup(&c_ver);

    assert(fx_ver->as_str() == ver);
    return true;
}
