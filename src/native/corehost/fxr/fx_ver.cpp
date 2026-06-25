// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thin C++ wrappers around the C implementation in fx_ver.c. The C++ class
// continues to own its m_pre/m_build as pal::string_t for source-compat with
// existing callers; we hand the underlying buffers to the C functions for
// individual operations (read-only) and copy results back out for parse.

#include <cassert>
#include <minipal/utils.h>
#include "pal.h"
#include "fx_ver.h"

fx_ver_t::fx_ver_t(int major, int minor, int patch, const pal::string_t& pre, const pal::string_t& build)
    : m_major(major)
    , m_minor(minor)
    , m_patch(patch)
    , m_pre(pre)
    , m_build(build)
{
    // Verify preconditions.
    assert(is_empty() || m_major >= 0);
    assert(is_empty() || m_minor >= 0);
    assert(is_empty() || m_patch >= 0);
    assert(m_pre.empty() || m_pre[0] == _X('-'));
    assert(m_build.empty() || m_build[0] == _X('+'));
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
    // Borrow pal::string_t buffers for the read-only formatting call. No
    // c_fx_ver_cleanup is needed because the C struct never took ownership.
    c_fx_ver_t c_ver;
    c_ver.major = m_major;
    c_ver.minor = m_minor;
    c_ver.patch = m_patch;
    c_ver.pre = const_cast<pal_char_t*>(m_pre.data());
    c_ver.build = const_cast<pal_char_t*>(m_build.data());

    // SemVer does not define a size limit on version string, but calls out a reasonable max of 255 characters:
    // https://semver.org/#does-semver-have-a-size-limit-on-the-version-string
    pal_char_t buf[256];
    c_fx_ver_as_str(&c_ver, buf, ARRAY_SIZE(buf));

    return pal::string_t(buf);
}

/* static */
int fx_ver_t::compare(const fx_ver_t& a, const fx_ver_t& b)
{
    // Borrow pal::string_t buffers for the read-only compare. The build
    // identifier does not affect ordering, so it is left NULL.
    c_fx_ver_t c_a, c_b;
    c_a.major = a.m_major;
    c_a.minor = a.m_minor;
    c_a.patch = a.m_patch;
    c_a.pre = const_cast<pal_char_t*>(a.m_pre.data());
    c_a.build = NULL;

    c_b.major = b.m_major;
    c_b.minor = b.m_minor;
    c_b.patch = b.m_patch;
    c_b.pre = const_cast<pal_char_t*>(b.m_pre.data());
    c_b.build = NULL;

    return c_fx_ver_compare(&c_a, &c_b);
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    c_fx_ver_t c_ver;
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
