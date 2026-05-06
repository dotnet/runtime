// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    const runtime_config_t::settings_t& override_settings
)
{
    m_runtime_config.parse(path, dev_path, override_settings);
}
