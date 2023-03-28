// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "tfm_compat.h"
#include <utils.h>

namespace
{
    constexpr int unknown_version = std::numeric_limits<int>::max();

    uint32_t get_compat_major_version_from_tfm(const pal::string_t& tfm)
    {
        // TFM is in form
        // - netcoreapp#.#  for <= 3.1
        // - net#.#  for >= 5.0
        // In theory it could contain a suffix like `net6.0-windows` (or more than one)
        // or it may lack the minor version like `net6`. SDK will normalize this, but the runtime should not 100% rely on it

        if (tfm.empty())
            return unknown_version;

        size_t majorVersionStartIndex;
        const pal::char_t netcoreapp_prefix[] = _X("netcoreapp");
        if (utils::starts_with(tfm, netcoreapp_prefix, true))
        {
            majorVersionStartIndex = utils::strlen(netcoreapp_prefix);
        }
        else
        {
            majorVersionStartIndex = utils::strlen(_X("net"));
        }

        if (majorVersionStartIndex >= tfm.length())
            return unknown_version;

        size_t majorVersionEndIndex = index_of_non_numeric(tfm, majorVersionStartIndex);
        if (majorVersionEndIndex == pal::string_t::npos || majorVersionEndIndex == majorVersionStartIndex)
            return unknown_version;

        return static_cast<uint32_t>(std::stoul(tfm.substr(majorVersionStartIndex, majorVersionEndIndex - majorVersionStartIndex)));
    }
}

bool tfm_compat::is_multilevel_lookup_disabled(const pal::string_t& tfm)
{
    // Starting with .NET 7, multi-level lookup is fully disabled
    unsigned long compat_major_version = get_compat_major_version_from_tfm(tfm);
    return (compat_major_version >= 7 || compat_major_version == unknown_version);
}

bool tfm_compat::is_rid_fallback_graph_disabled(const pal::string_t& tfm)
{
    // Starting with .NET 8, reading the RID fallback graph is disabled by default
    unsigned long compat_major_version = get_compat_major_version_from_tfm(tfm);
    return (compat_major_version >= 8 || compat_major_version == unknown_version);
}
