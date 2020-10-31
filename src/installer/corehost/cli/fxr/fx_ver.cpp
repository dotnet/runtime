// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include "pal.h"
#include "utils.h"
#include "fx_ver.h"

static bool validIdentifiers(const pal::string_t& ids);

fx_ver_t::fx_ver_t(int major, int minor, int patch, const pal::string_t& pre, const pal::string_t& build)
    : m_major(major)
    , m_minor(minor)
    , m_patch(patch)
    , m_pre(pre)
    , m_build(build)
{
    // verify preconditions
    assert(is_empty() || m_major >= 0);
    assert(is_empty() || m_minor >= 0);
    assert(is_empty() || m_patch >= 0);
    assert(m_pre[0] == 0 || validIdentifiers(m_pre));
    assert(m_build[0] == 0 || validIdentifiers(m_build));
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
    pal::stringstream_t stream;
    stream << m_major << _X(".") << m_minor << _X(".") << m_patch;
    if (!m_pre.empty())
    {
        stream << m_pre;
    }
    if (!m_build.empty())
    {
        stream << m_build;
    }
    return stream.str();
}

pal::string_t fx_ver_t::prerelease_glob() const
{
    pal::stringstream_t stream;
    stream << m_major << _X(".") << m_minor << _X(".") << m_patch << _X("-*");
    return stream.str();
}

pal::string_t fx_ver_t::patch_glob() const
{
    pal::stringstream_t stream;
    stream << m_major << _X(".") << m_minor << _X(".*");
    return stream.str();
}

static pal::string_t getId(const pal::string_t &ids, size_t idStart)
{
    size_t next = ids.find(_X('.'), idStart);

    return next == pal::string_t::npos ? ids.substr(idStart) : ids.substr(idStart, next - idStart);
}

/* static */
int fx_ver_t::compare(const fx_ver_t&a, const fx_ver_t& b)
{
    // compare(u.v.w-p+b, x.y.z-q+c)
    if (a.m_major != b.m_major)
    {
        return (a.m_major > b.m_major) ? 1 : -1;
    }

    if (a.m_minor != b.m_minor)
    {
        return (a.m_minor > b.m_minor) ? 1 : -1;
    }

    if (a.m_patch != b.m_patch)
    {
        return (a.m_patch > b.m_patch) ? 1 : -1;
    }

    if (a.m_pre.empty() || b.m_pre.empty())
    {
        // Either a is empty or b is empty or both are empty
        return a.m_pre.empty() ? !b.m_pre.empty() : -1;
    }

    // Both are non-empty (may be equal)

    // First character of pre is '-' when it is not empty
    assert(a.m_pre[0] == _X('-'));
    assert(b.m_pre[0] == _X('-'));

    // First idenitifier starts at position 1
    size_t idStart = 1;
    for (size_t i = idStart; true; ++i)
    {
        if (a.m_pre[i] != b.m_pre[i])
        {
            // Found first character with a difference
            if (a.m_pre[i] == 0 && b.m_pre[i] == _X('.'))
            {
                // identifiers both complete, b has an additional idenitifier
                return -1;
            }

            if (b.m_pre[i] == 0 && a.m_pre[i] == _X('.'))
            {
                // identifiers both complete, a has an additional idenitifier
                return 1;
            }

            // identifiers must not be empty
            pal::string_t ida = getId(a.m_pre, idStart);
            pal::string_t idb = getId(b.m_pre, idStart);

            unsigned idanum = 0;
            bool idaIsNum = try_stou(ida, &idanum);
            unsigned idbnum = 0;
            bool idbIsNum = try_stou(idb, &idbnum);

            if (idaIsNum && idbIsNum)
            {
                // Numeric comparison
                return (idanum > idbnum) ? 1 : -1;
            }
            else if (idaIsNum || idbIsNum)
            {
                // Mixed compare.  Spec: Number < Text
                return idbIsNum ? 1 : -1;
            }
            // Ascii compare
            return ida.compare(idb);
        }
        else
        {
            // a.m_pre[i] == b.m_pre[i]
            if (a.m_pre[i] == 0)
            {
                break;
            }
            if (a.m_pre[i] == _X('.'))
            {
                idStart = i + 1;
            }
        }
    }

    return 0;
}

static bool validIdentifierCharSet(const pal::string_t& id)
{
    // ids must be of the set [0-9a-zA-Z-]

    // ASCII and Unicode ordering
    static_assert(_X('-') < _X('0'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
    static_assert(_X('0') < _X('9'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
    static_assert(_X('9') < _X('A'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
    static_assert(_X('A') < _X('Z'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
    static_assert(_X('Z') < _X('a'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");
    static_assert(_X('a') < _X('z'), "Code assumes ordering - < 0 < 9 < A < Z < a < z");

    for (size_t i = 0; id[i] != 0; ++i)
    {
        if (id[i] >= _X('A'))
        {
            if ((id[i] > _X('Z') && id[i] < _X('a')) || id[i] > _X('z'))
            {
                return false;
            }
        }
        else
        {
            if ((id[i] < _X('0') && id[i] != _X('-')) || id[i] > _X('9'))
            {
                return false;
            }
        }
    }
    return true;
}

static bool validIdentifier(const pal::string_t& id, bool buildMeta)
{
    if (id.empty())
    {
        // Identifier must not be empty
        return false;
    }

    if (!validIdentifierCharSet(id))
    {
        // ids must be of the set [0-9a-zA-Z-]
        return false;
    }

    if (!buildMeta && id[0] == _X('0') && id[1] != 0 && index_of_non_numeric(id, 1) == pal::string_t::npos)
    {
        // numeric identifiers must not be padded with 0s
        return false;
    }
    return true;
}

static bool validIdentifiers(const pal::string_t& ids)
{
    if (ids.empty())
    {
        return true;
    }

    bool prerelease = ids[0] == _X('-');
    bool buildMeta = ids[0] == _X('+');

    if (!(prerelease || buildMeta))
    {
        // ids must start with '-' or '+' for prerelease & build respectively
        return false;
    }

    size_t idStart = 1;
    size_t nextId;
    while ((nextId = ids.find(_X('.'), idStart)) != pal::string_t::npos)
    {
        if (!validIdentifier(ids.substr(idStart, nextId - idStart), buildMeta))
        {
            return false;
        }
        idStart = nextId + 1;
    }

    if (!validIdentifier(ids.substr(idStart), buildMeta))
    {
        return false;
    }

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
    if (maj_sep > 1 && ver[maj_start] == _X('0'))
    {
        // if leading character is 0, and strlen > 1
        // then the numeric substring has leading zeroes which is prohibited by the specification.
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
    if (min_sep - min_start > 1 && ver[min_start] == _X('0'))
    {
        // if leading character is 0, and strlen > 1
        // then the numeric substring has leading zeroes which is prohibited by the specification.
        return false;
    }

    unsigned patch = 0;
    size_t pat_start = min_sep + 1;
    size_t pat_sep = index_of_non_numeric(ver, pat_start);
    if (pat_sep == pal::string_t::npos)
    {
        if (!try_stou(ver.substr(pat_start), &patch))
        {
            return false;
        }
        if (ver[pat_start + 1] != 0 && ver[pat_start] == _X('0'))
        {
            // if leading character is 0, and strlen != 1
            // then the numeric substring has leading zeroes which is prohibited by the specification.
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
    if (pat_sep - pat_start > 1 && ver[pat_start] == _X('0'))
    {
        return false;
    }

    size_t pre_start = pat_sep;
    size_t pre_sep = ver.find(_X('+'), pat_sep);

    pal::string_t pre = (pre_sep == pal::string_t::npos) ? ver.substr(pre_start) : ver.substr(pre_start, pre_sep - pre_start);

    if (!validIdentifiers(pre))
    {
        return false;
    }

    pal::string_t build;

    if (pre_sep != pal::string_t::npos)
    {
        build = ver.substr(pre_sep);

        if (!validIdentifiers(build))
        {
            return false;
        }
    }

    *fx_ver = fx_ver_t(major, minor, patch, pre, build);
    return true;
}

/* static */
bool fx_ver_t::parse(const pal::string_t& ver, fx_ver_t* fx_ver, bool parse_only_production)
{
    bool valid = parse_internal(ver, fx_ver, parse_only_production);
    assert(!valid || fx_ver->as_str() == ver);
    return valid;
}
