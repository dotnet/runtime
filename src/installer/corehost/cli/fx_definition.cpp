// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "deps_format.h"
#include "fx_definition.h"
#include "fx_ver.h"
#include "pal.h"
#include "runtime_config.h"

fx_definition_t::fx_definition_t()
{
}

fx_definition_t::fx_definition_t(
    const pal::string_t& name,
    const pal::string_t& dir,
    const pal::string_t& requested_version,
    const pal::string_t& found_version)
    : m_name(name)
    , m_dir(dir)
    , m_requested_version(requested_version)
    , m_found_version(found_version)
{
}

void fx_definition_t::parse_runtime_config(
    const pal::string_t& path,
    const pal::string_t& dev_path,
    const runtime_config_t* higher_layer_config,
    const runtime_config_t* app_config
)
{
    m_runtime_config.parse(path, dev_path, higher_layer_config, app_config);
}

void fx_definition_t::parse_deps()
{
    m_deps.parse(false, m_deps_file);
}

void fx_definition_t::parse_deps(const deps_json_t::rid_fallback_graph_t& graph)
{
    m_deps.parse(true, m_deps_file, graph);
}

bool fx_definition_t::did_minor_or_major_roll_forward_occur() const
{
    fx_ver_t requested_ver(-1, -1, -1);
    if (!fx_ver_t::parse(m_requested_version, &requested_ver, false))
    {
        assert(false);
        return false;
    }

    fx_ver_t found_ver(-1, -1, -1);
    if (!fx_ver_t::parse(m_found_version, &found_ver, false))
    {
        assert(false);
        return false;
    }

    if (requested_ver >= found_ver)
    {
        assert(requested_ver == found_ver); // We shouldn't have a > case here
        return false;
    }

    if (requested_ver.get_major() != found_ver.get_major())
    {
        assert(requested_ver.get_major() < found_ver.get_major());
        return true;
    }

    if (requested_ver.get_minor() != found_ver.get_minor())
    {
        assert(requested_ver.get_minor() < found_ver.get_minor());
        return true;
    }

    // Differs in patch version only
    return false;
}
