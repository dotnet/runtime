// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <list>

#include "pal.h"
#include "cpprest/json.h"

typedef web::json::value json_value;

class runtime_config_t
{
public:
    runtime_config_t(const pal::string_t& path, const pal::string_t& dev_path);
    bool is_valid() { return m_valid; }
    const pal::string_t& get_path() { return m_path; }
    const pal::string_t& get_dev_path() { return m_dev_path; }
    const pal::string_t& get_gc_server() const;
    const pal::string_t& get_fx_version() const;
    const pal::string_t& get_fx_name() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_fx_roll_fwd() const;
    bool get_portable() const;
    bool parse_opts(const json_value& opts);
    void config_kv(std::vector<std::string>*, std::vector<std::string>*) const;

private:
    bool ensure_parsed();
    bool ensure_dev_config_parsed();
 
    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    std::vector<std::string> m_prop_keys;
    std::vector<std::string> m_prop_values;
    std::list<pal::string_t> m_probe_paths;
    pal::string_t m_fx_name;
    pal::string_t m_fx_ver;
    bool m_fx_roll_fwd;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_portable;
    bool m_valid;
};
