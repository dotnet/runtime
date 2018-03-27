// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __RUNTIME_CONFIG_H__
#define __RUNTIME_CONFIG_H__

#include <list>

#include "pal.h"
#include "cpprest/json.h"

typedef web::json::value json_value;
typedef web::json::object json_object;

enum class roll_fwd_on_no_candidate_fx_option
{
    disabled = 0,
    minor,
    major_or_minor
};

class runtime_config_framework_t
{
public:
    // Uses a "nullable<T>" pattern until we add such a type

    runtime_config_framework_t()
        : has_fx_ver(false)
        , has_roll_fwd_on_no_candidate_fx(false)
        , has_patch_roll_fwd(false)
        , fx_ver(_X(""))
        , patch_roll_fwd(false)
        , roll_fwd_on_no_candidate_fx((roll_fwd_on_no_candidate_fx_option)0)
        { }

    const pal::string_t* get_fx_ver() const
    {
        return (has_fx_ver ? &fx_ver : nullptr);
    }
    void set_fx_ver(pal::string_t value)
    {
        has_fx_ver = true;
        fx_ver = value;
    }

    const bool* get_patch_roll_fwd() const
    {
        return (has_patch_roll_fwd ? &patch_roll_fwd : nullptr);
    }
    void set_patch_roll_fwd(bool value)
    {
        has_patch_roll_fwd = true;
        patch_roll_fwd = value;
    }

    const roll_fwd_on_no_candidate_fx_option* get_roll_fwd_on_no_candidate_fx() const
    {
        return (has_roll_fwd_on_no_candidate_fx ? &roll_fwd_on_no_candidate_fx : nullptr);
    }
    void set_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option value)
    {
        has_roll_fwd_on_no_candidate_fx = true;
        roll_fwd_on_no_candidate_fx = value;
    }

private:
    bool has_fx_ver;
    bool has_patch_roll_fwd;
    bool has_roll_fwd_on_no_candidate_fx;

    pal::string_t fx_ver;
    bool patch_roll_fwd;
    roll_fwd_on_no_candidate_fx_option roll_fwd_on_no_candidate_fx;
};

class runtime_config_t
{
public:
    runtime_config_t();
    void parse(const pal::string_t& path, const pal::string_t& dev_path, const runtime_config_t* higher_layer_config, const runtime_config_t* app_config);
    bool is_valid() const { return m_valid; }
    const pal::string_t& get_path() const { return m_path; }
    const pal::string_t& get_dev_path() const { return m_dev_path; }
    const pal::string_t& get_fx_version() const;
    const pal::string_t& get_fx_name() const;
    const pal::string_t& get_tfm() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_patch_roll_fwd() const;
    roll_fwd_on_no_candidate_fx_option get_roll_fwd_on_no_candidate_fx() const;
    void force_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option value);
    bool get_is_framework_dependent() const;
    bool parse_opts(const json_value& opts);
    void combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const;

private:
    bool ensure_parsed(const runtime_config_t* defaults);
    bool ensure_dev_config_parsed();

    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    std::unordered_map<pal::string_t, runtime_config_framework_t> m_additional_frameworks;
    runtime_config_framework_t m_fx_global;     // the settings that will be applied to the next lower layer; does not include version (Step #1)
    runtime_config_framework_t m_fx;            // the settings in the current "framework" section (Step #3)
    runtime_config_framework_t m_fx_readonly;   // the settings that can't be changed by lower layers (Step #4)
    std::vector<std::string> m_prop_keys;
    std::vector<std::string> m_prop_values;
    std::list<pal::string_t> m_probe_paths;

    pal::string_t m_tfm;
    pal::string_t m_fx_name;

    // These are the effective settings
    pal::string_t m_fx_ver;
    bool m_patch_roll_fwd;
    roll_fwd_on_no_candidate_fx_option m_roll_fwd_on_no_candidate_fx;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_is_framework_dependent;
    bool m_valid;

private:
    bool parse_framework(const json_object& fx_obj);
    void set_effective_values(const runtime_config_framework_t& overrides);
    static void copy_framework_settings_to(const runtime_config_framework_t& from, runtime_config_framework_t& to);

};
#endif // __RUNTIME_CONFIG_H__