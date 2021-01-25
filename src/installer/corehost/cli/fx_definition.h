// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FX_DEFINITION_H__
#define __FX_DEFINITION_H__

#include "pal.h"
#include "deps_format.h"
#include "runtime_config.h"

class fx_definition_t
{
public:
    fx_definition_t();
    fx_definition_t(
        const pal::string_t& name,
        const pal::string_t& dir,
        const pal::string_t& requested_version,
        const pal::string_t& found_version);

    const pal::string_t& get_name() const { return m_name; }
    const pal::string_t& get_requested_version() const { return m_requested_version; }
    const pal::string_t& get_found_version() const { return m_found_version; }
    const pal::string_t& get_dir() const { return m_dir; }
    const runtime_config_t& get_runtime_config() const { return m_runtime_config; }
    void parse_runtime_config(const pal::string_t& path, const pal::string_t& dev_path, const runtime_config_t::settings_t& override_settings);

    const pal::string_t& get_deps_file() const { return m_deps_file; }
    void set_deps_file(const pal::string_t value) { m_deps_file = value; }
    const deps_json_t& get_deps() const { return m_deps; }
    void parse_deps();
    void parse_deps(const deps_json_t::rid_fallback_graph_t& graph);

private:
    pal::string_t m_name;
    pal::string_t m_dir;
    pal::string_t m_requested_version;
    pal::string_t m_found_version;
    runtime_config_t m_runtime_config;
    pal::string_t m_deps_file;
    deps_json_t m_deps;
};

typedef std::vector<std::unique_ptr<fx_definition_t>> fx_definition_vector_t;

static const fx_definition_t& get_root_framework(const fx_definition_vector_t& fx_definitions)
{
    return *fx_definitions[fx_definitions.size() - 1];
}

static const fx_definition_t& get_app(const fx_definition_vector_t& fx_definitions)
{
    return *fx_definitions[0];
}

#endif // __FX_DEFINITION_H__
