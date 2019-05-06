// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COREHOST_INIT_H__
#define __COREHOST_INIT_H__

#include "host_interface.h"
#include "host_startup_info.h"
#include "fx_definition.h"

class corehost_init_t
{
private:
    std::vector<pal::string_t> m_clr_keys;
    std::vector<pal::string_t> m_clr_values;
    std::vector<const pal::char_t*> m_clr_keys_cstr;
    std::vector<const pal::char_t*> m_clr_values_cstr;
    const pal::string_t m_tfm;
    const pal::string_t m_deps_file;
    const pal::string_t m_additional_deps_serialized;
    bool m_is_framework_dependent;
    std::vector<pal::string_t> m_probe_paths;
    std::vector<const pal::char_t*> m_probe_paths_cstr;
    host_mode_t m_host_mode;
    host_interface_t m_host_interface;
    std::vector<pal::string_t> m_fx_names;
    std::vector<const pal::char_t*> m_fx_names_cstr;
    std::vector<pal::string_t> m_fx_dirs;
    std::vector<const pal::char_t*> m_fx_dirs_cstr;
    std::vector<pal::string_t> m_fx_requested_versions;
    std::vector<const pal::char_t*> m_fx_requested_versions_cstr;
    std::vector<pal::string_t> m_fx_found_versions;
    std::vector<const pal::char_t*> m_fx_found_versions_cstr;
    const pal::string_t m_host_command;
    const pal::string_t m_host_info_host_path;
    const pal::string_t m_host_info_dotnet_root;
    const pal::string_t m_host_info_app_path;
public:
    corehost_init_t(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& deps_file,
        const pal::string_t& additional_deps_serialized,
        const std::vector<pal::string_t>& probe_paths,
        const host_mode_t mode,
        const fx_definition_vector_t& fx_definitions);

    const host_interface_t& get_host_init_data();

    void get_found_fx_versions(std::unordered_map<pal::string_t, const fx_ver_t> &out_fx_versions);
};

#endif // __COREHOST_INIT_H__
