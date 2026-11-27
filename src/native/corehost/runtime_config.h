// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RUNTIME_CONFIG_H__
#define __RUNTIME_CONFIG_H__

#include <list>
#include <optional>

#include "pal.h"
#include "fx_reference.h"
#include "json_parser.h"

class runtime_config_t
{
public:
    runtime_config_t();
    void parse(
        const pal::string_t& path,
        const pal::string_t& dev_path,
        const std::optional<roll_forward_option>& override_roll_forward);
    bool is_valid() const { return m_valid; }
    const pal::string_t& get_path() const { return m_path; }
    const pal::string_t& get_dev_path() const { return m_dev_path; }
    const pal::string_t& get_tfm() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_is_framework_dependent() const;
    bool parse_opts(const json_parser_t::value_t& opts);
    void combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const;
    const fx_reference_vector_t& get_frameworks() const { return m_frameworks; }
    const fx_reference_vector_t& get_included_frameworks() const { return m_included_frameworks; }
    void set_fx_version(pal::string_t version);

private:
    bool ensure_parsed();
    bool ensure_dev_config_parsed();

    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    fx_reference_vector_t m_frameworks;
    fx_reference_vector_t m_included_frameworks;
    std::optional<roll_forward_option> m_runtime_options_roll_forward;  // 3) The current "runtimeOptions" section
    std::optional<roll_forward_option> m_override_roll_forward;         // 0) Overrides or 1) the environment setting
    std::list<pal::string_t> m_probe_paths;

    pal::string_t m_tfm;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_is_framework_dependent;
    bool m_valid;

    // Cached value of DOTNET_ROLL_FORWARD_TO_PRERELEASE to avoid testing env. variables too often.
    // If set to true, all versions (including pre-release) are considered even if starting from a release framework reference.
    bool m_roll_forward_to_prerelease;

    bool parse_framework(const json_parser_t::value_t& fx_obj, bool name_and_version_only, fx_reference_t& fx_out);
    bool read_framework_array(const json_parser_t::value_t& frameworks, bool name_and_version_only, fx_reference_vector_t& frameworks_out);
};
#endif // __RUNTIME_CONFIG_H__
