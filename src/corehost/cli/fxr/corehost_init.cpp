// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "corehost_init.h"

void make_cstr_arr(const std::vector<pal::string_t>& arr, std::vector<const pal::char_t*>* out)
{
    out->reserve(arr.size());
    for (const auto& str : arr)
    {
        out->push_back(str.c_str());
    }
}

corehost_init_t::corehost_init_t(
    const pal::string_t& host_command,
    const host_startup_info_t& host_info,
    const pal::string_t& deps_file,
    const pal::string_t& additional_deps_serialized,
    const std::vector<pal::string_t>& probe_paths,
    const host_mode_t mode,
    const fx_definition_vector_t& fx_definitions)
    : m_tfm(get_app(fx_definitions).get_runtime_config().get_tfm())
    , m_deps_file(deps_file)
    , m_additional_deps_serialized(additional_deps_serialized)
    , m_is_framework_dependent(get_app(fx_definitions).get_runtime_config().get_is_framework_dependent())
    , m_probe_paths(probe_paths)
    , m_host_mode(mode)
    , m_host_interface()
    , m_host_command(host_command)
    , m_host_info_host_path(host_info.host_path)
    , m_host_info_dotnet_root(host_info.dotnet_root)
    , m_host_info_app_path(host_info.app_path)
{
    make_cstr_arr(m_probe_paths, &m_probe_paths_cstr);

    int fx_count = fx_definitions.size();
    m_fx_names.reserve(fx_count);
    m_fx_dirs.reserve(fx_count);
    m_fx_requested_versions.reserve(fx_count);
    m_fx_found_versions.reserve(fx_count);

    std::unordered_map<pal::string_t, pal::string_t> combined_properties;
    for (auto& fx : fx_definitions)
    {
        fx->get_runtime_config().combine_properties(combined_properties);

        m_fx_names.push_back(fx->get_name());
        m_fx_dirs.push_back(fx->get_dir());
        m_fx_requested_versions.push_back(fx->get_requested_version());
        m_fx_found_versions.push_back(fx->get_found_version());
    }

    for (const auto& kv : combined_properties)
    {
        m_clr_keys.push_back(kv.first);
        m_clr_values.push_back(kv.second);
    }

    make_cstr_arr(m_fx_names, &m_fx_names_cstr);
    make_cstr_arr(m_fx_dirs, &m_fx_dirs_cstr);
    make_cstr_arr(m_fx_requested_versions, &m_fx_requested_versions_cstr);
    make_cstr_arr(m_fx_found_versions, &m_fx_found_versions_cstr);
    make_cstr_arr(m_clr_keys, &m_clr_keys_cstr);
    make_cstr_arr(m_clr_values, &m_clr_values_cstr);
}

const host_interface_t& corehost_init_t::get_host_init_data()
{
    host_interface_t& hi = m_host_interface;

    hi.version_lo = HOST_INTERFACE_LAYOUT_VERSION_LO;
    hi.version_hi = HOST_INTERFACE_LAYOUT_VERSION_HI;

    hi.config_keys.len = m_clr_keys_cstr.size();
    hi.config_keys.arr = m_clr_keys_cstr.data();

    hi.config_values.len = m_clr_values_cstr.size();
    hi.config_values.arr = m_clr_values_cstr.data();

    // Keep these for backwards compat
    if (m_fx_names_cstr.size() > 1)
    {
        hi.fx_name = m_fx_names_cstr[1];
        hi.fx_dir = m_fx_dirs_cstr[1];
        hi.fx_ver = m_fx_requested_versions_cstr[1];
    }
    else
    {
        hi.fx_name = _X("");
        hi.fx_dir = _X("");
        hi.fx_ver = _X("");
    }

    hi.deps_file = m_deps_file.c_str();
    hi.additional_deps_serialized = m_additional_deps_serialized.c_str();
    hi.is_framework_dependent = m_is_framework_dependent;

    hi.probe_paths.len = m_probe_paths_cstr.size();
    hi.probe_paths.arr = m_probe_paths_cstr.data();

    // These are not used anymore, but we have to keep them for backward compat reasons.
    // Set default values.
    hi.patch_roll_forward = true;
    hi.prerelease_roll_forward = false;

    hi.host_mode = m_host_mode;

    hi.tfm = m_tfm.c_str();

    hi.fx_names.len = m_fx_names_cstr.size();
    hi.fx_names.arr = m_fx_names_cstr.data();

    hi.fx_dirs.len = m_fx_dirs_cstr.size();
    hi.fx_dirs.arr = m_fx_dirs_cstr.data();

    hi.fx_requested_versions.len = m_fx_requested_versions_cstr.size();
    hi.fx_requested_versions.arr = m_fx_requested_versions_cstr.data();

    hi.fx_found_versions.len = m_fx_found_versions_cstr.size();
    hi.fx_found_versions.arr = m_fx_found_versions_cstr.data();

    hi.host_command = m_host_command.c_str();

    hi.host_info_host_path = m_host_info_host_path.c_str();
    hi.host_info_dotnet_root = m_host_info_dotnet_root.c_str();
    hi.host_info_app_path = m_host_info_app_path.c_str();

    return hi;
}