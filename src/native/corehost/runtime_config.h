// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RUNTIME_CONFIG_H__
#define __RUNTIME_CONFIG_H__

#include <list>

#include "pal.h"
#include <external/rapidjson/fwd.h>
#include "fx_reference.h"

class runtime_config_t
{
public:
    struct settings_t
    {
        settings_t();

        bool has_apply_patches;
        bool apply_patches;
        void set_apply_patches(bool value) { has_apply_patches = true; apply_patches = value; }

        bool has_roll_forward;
        roll_forward_option roll_forward;
        void set_roll_forward(roll_forward_option value) { has_roll_forward = true; roll_forward = value; }
    };

public:
    runtime_config_t();
    void parse(const pal::string_t& path, const pal::string_t& dev_path, const settings_t& override_settings);
    bool is_valid() const { return m_valid; }
    const pal::string_t& get_path() const { return m_path; }
    const pal::string_t& get_dev_path() const { return m_dev_path; }
    const pal::string_t& get_tfm() const;
    bool get_is_multilevel_lookup_disabled() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_is_framework_dependent() const;
    bool parse_opts(const json_parser_t::value_t& opts);
    void combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const;
    const fx_reference_vector_t& get_frameworks() const { return m_frameworks; }
    const fx_reference_vector_t& get_included_frameworks() const { return m_included_frameworks; }
    void set_fx_version(pal::string_t version);

    static constexpr int unknown_version = std::numeric_limits<int>::max();

private:
    const uint32_t get_compat_major_version_from_tfm() const;
    bool ensure_parsed(); //todo: const runtime_config_t* defaults
    bool ensure_dev_config_parsed();

    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    fx_reference_vector_t m_frameworks;
    fx_reference_vector_t m_included_frameworks;
    settings_t m_default_settings;   // the default settings (Steps #0 and #1)
    settings_t m_override_settings;  // the settings that can't be changed (Step #5)
    std::vector<std::string> m_prop_keys;
    std::vector<std::string> m_prop_values;
    std::list<pal::string_t> m_probe_paths;

    pal::string_t m_tfm;

    // This is used to detect cases where rollForward is used together with the obsoleted
    // rollForwardOnNoCandidateFx/applyPatches.
    // Flags
    enum specified_setting
    {
        none = 0x0,
        specified_roll_forward = 0x1,
        specified_roll_forward_on_no_candidate_fx_or_apply_patched = 0x2
    } m_specified_settings;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_is_framework_dependent;
    bool m_valid;

    // Cached value of DOTNET_ROLL_FORWARD_TO_PRERELEASE to avoid testing env. variables too often.
    // If set to true, all versions (including pre-release) are considered even if starting from a release framework reference.
    bool m_roll_forward_to_prerelease;

    bool parse_framework(const json_parser_t::value_t& fx_obj, fx_reference_t& fx_out, bool name_and_version_only = false);
    bool read_framework_array(const json_parser_t::value_t& frameworks, fx_reference_vector_t& frameworks_out, bool name_and_version_only = false);

    bool mark_specified_setting(specified_setting setting);
};
#endif // __RUNTIME_CONFIG_H__
