// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __VERSION_H__
#define __VERSION_H__

#include "pal.h"
#include "utils.h"

struct version_t
{
    version_t();
    version_t(int major, int minor, int build, int revision);

    int get_major() const { return m_major; }
    int get_minor() const { return m_minor; }
    int get_build() const { return m_build; }
    // int get_revision() const { return m_revision; }

    pal::string_t as_str() const;

    bool operator ==(const version_t& b) const;
    bool operator !=(const version_t& b) const;
    bool operator <(const version_t& b) const;
    bool operator >(const version_t& b) const;
    bool operator <=(const version_t& b) const;
    bool operator >=(const version_t& b) const;

    static bool parse(const pal::string_t& ver, version_t* ver_out);

private:
    int m_major;
    int m_minor;
    int m_build;
    int m_revision;

    static int compare(const version_t&a, const version_t& b);
};

#endif // __VERSION_H__
