// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __RUNTIME_CONFIG_H__
#define __RUNTIME_CONFIG_H__

#include <list>

#include "pal.h"
#include "cpprest/json.h"
#include "fx_reference.h"

typedef web::json::value json_value;
typedef web::json::object json_object;

class runtime_config_t
{
public:
    runtime_config_t();
    void parse(const pal::string_t& path, const pal::string_t& dev_path, const fx_reference_t& fx_ref, const fx_reference_t& override_settings);
    bool is_valid() const { return m_valid; }
    const pal::string_t& get_path() const { return m_path; }
    const pal::string_t& get_dev_path() const { return m_dev_path; }
    const pal::string_t& get_tfm() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_is_framework_dependent() const;
    bool parse_opts(const json_value& opts);
    void combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const;
    const fx_reference_vector_t& get_frameworks() const { return m_frameworks; }
    void set_fx_version(pal::string_t version);

private:
    bool ensure_parsed(); //todo: const runtime_config_t* defaults
    bool ensure_dev_config_parsed();

    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    fx_reference_vector_t m_frameworks;
    fx_reference_t m_fx_defaults;   // the default settings (Steps #1 and #2)
    fx_reference_t m_fx_ref;        // the settings from the referenced "frameworks" section (Step #3)
    fx_reference_t m_fx_overrides;  // the settings that can't be changed (Step #4)
    std::vector<std::string> m_prop_keys;
    std::vector<std::string> m_prop_values;
    std::list<pal::string_t> m_probe_paths;

    pal::string_t m_tfm;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_is_framework_dependent;
    bool m_valid;

private:
    bool parse_framework(const json_object& fx_obj, fx_reference_t& fx_out);
    bool read_framework_array(web::json::array frameworks);
    static void copy_framework_settings_to(const fx_reference_t& from, fx_reference_t& to);
};
#endif // __RUNTIME_CONFIG_H__
