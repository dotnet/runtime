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
    const fx_reference_t& fx_ref,
    const fx_reference_t& override_settings
)
{
    m_runtime_config.parse(path, dev_path, fx_ref, override_settings);
}

void fx_definition_t::parse_deps()
{
    m_deps.parse(false, m_deps_file);
}

void fx_definition_t::parse_deps(const deps_json_t::rid_fallback_graph_t& graph)
{
    m_deps.parse(true, m_deps_file, graph);
}
