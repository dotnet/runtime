// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "cpprest/json.h"
#include "runtime_config.h"
#include <cassert>


// The semantics of applying the runtimeconfig.json values follows, in the following steps from
// first to last, where last always wins. These steps are also annotated in the code here.
// 1a) If the app, apply the default values from the environment
// 1b) If the framework, apply the default values from the higher framework
// 2) Apply the values in the current "framework" section; use these as defaults for current layer
// 3) Apply the values in the app's "additionalFrameworks" section for the targeted framework
// 4) Apply the readonly values which are the settings that can't be changed by lower layers

runtime_config_t::runtime_config_t()
    : m_patch_roll_fwd(true)
    , m_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option::minor)
    , m_is_framework_dependent(false)
    , m_valid(false)
{
}

void runtime_config_t::parse(const pal::string_t& path, const pal::string_t& dev_path, const runtime_config_t* higher_layer_config, const runtime_config_t* app_config)
{
    m_path = path;
    m_dev_path = dev_path;

    // Step #1: apply the defaults from the environment (for the app) or previous\higher layer (for a framework)
    if (higher_layer_config != nullptr)
    {
        // Copy the previous defaults so we can default the next framework; these may be changed by the current fx
        copy_framework_settings_to(higher_layer_config->m_fx_global, m_fx_global);

        // Apply the defaults
        set_effective_values(m_fx_global);
    }
    else
    {
        // Since there is no previous config, this is the app's config, so default m_roll_fwd_on_no_candidate_fx from the env variable.
        // The value will be overwritten during parsing if the setting exists in the config file.
        pal::string_t env_no_candidate;
        if (pal::getenv(_X("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX"), &env_no_candidate))
        {
            m_roll_fwd_on_no_candidate_fx = static_cast<roll_fwd_on_no_candidate_fx_option>(pal::xtoi(env_no_candidate.c_str()));
            m_fx_global.set_roll_fwd_on_no_candidate_fx(m_roll_fwd_on_no_candidate_fx);
        }
    }

    m_valid = ensure_parsed(app_config);

    if (m_valid)
    {
        // Step #4: apply the readonly values
        if (app_config != nullptr)
        {
            m_tfm = app_config->m_tfm;
            set_effective_values(app_config->m_fx_readonly);
        }
    }

    trace::verbose(_X("Runtime config [%s] is valid=[%d]"), path.c_str(), m_valid);
}

bool runtime_config_t::parse_opts(const json_value& opts)
{
    // Note: both runtime_config and dev_runtime_config call into the function.
    // runtime_config will override whatever dev_runtime_config populated.
    if (opts.is_null())
    {
        return true;
    }

    const auto& opts_obj = opts.as_object();

    auto properties = opts_obj.find(_X("configProperties"));
    if (properties != opts_obj.end())
    {
        const auto& prop_obj = properties->second.as_object();
        for (const auto& property : prop_obj)
        {
            m_properties[property.first] = property.second.is_string()
                ? property.second.as_string()
                : property.second.to_string();
        }
    }

    auto probe_paths = opts_obj.find(_X("additionalProbingPaths"));
    if (probe_paths != opts_obj.end())
    {
        if (probe_paths->second.is_string())
        {
            m_probe_paths.insert(m_probe_paths.begin(), probe_paths->second.as_string());
        }
        else
        {
            const auto& arr = probe_paths->second.as_array();
            for (auto iter = arr.rbegin(); iter != arr.rend(); iter++)
            {
                m_probe_paths.push_front(iter->as_string());
            }
        }
    }

    auto patch_roll_fwd = opts_obj.find(_X("applyPatches"));
    if (patch_roll_fwd != opts_obj.end())
    {
        m_patch_roll_fwd = patch_roll_fwd->second.as_bool();
        m_fx_global.set_patch_roll_fwd(m_patch_roll_fwd);
    }

    auto roll_fwd_on_no_candidate_fx = opts_obj.find(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != opts_obj.end())
    {
        m_roll_fwd_on_no_candidate_fx = static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->second.as_integer());
        m_fx_global.set_roll_fwd_on_no_candidate_fx(m_roll_fwd_on_no_candidate_fx);
    }

    auto tfm = opts_obj.find(_X("tfm"));
    if (tfm != opts_obj.end())
    {
        m_tfm = tfm->second.as_string();
    }

    // Step #2: apply the "framework" section

    auto framework =  opts_obj.find(_X("framework"));
    if (framework == opts_obj.end())
    {
        return true;
    }

    m_is_framework_dependent = true;

    const auto& fx_obj = framework->second.as_object();

    m_fx_name = fx_obj.at(_X("name")).as_string();

    bool rc = parse_framework(fx_obj);
    if (rc)
    {
        set_effective_values(m_fx);
    }

    return rc;
}

void runtime_config_t::set_effective_values(const runtime_config_framework_t& overrides)
{
    if (overrides.get_fx_ver() != nullptr)
    {
        m_fx_ver = *overrides.get_fx_ver();
    }

    if (overrides.get_roll_fwd_on_no_candidate_fx() != nullptr)
    {
        m_roll_fwd_on_no_candidate_fx = *overrides.get_roll_fwd_on_no_candidate_fx();
    }

    if (overrides.get_patch_roll_fwd() != nullptr)
    {
        m_patch_roll_fwd = *overrides.get_patch_roll_fwd();
    }
}

/*static*/ void runtime_config_t::copy_framework_settings_to(const runtime_config_framework_t& from, runtime_config_framework_t& to)
{
    if (from.get_fx_ver() != nullptr)
    {
        to.set_fx_ver(*from.get_fx_ver());
    }

    if (from.get_roll_fwd_on_no_candidate_fx() != nullptr)
    {
        to.set_roll_fwd_on_no_candidate_fx(*from.get_roll_fwd_on_no_candidate_fx());
    }

    if (from.get_patch_roll_fwd() != nullptr)
    {
        to.set_patch_roll_fwd(*from.get_patch_roll_fwd());
    }
}

bool runtime_config_t::parse_framework(const json_object& fx_obj)
{
    auto fx_ver = fx_obj.find(_X("version"));
    if (fx_ver != fx_obj.end())
    {
        m_fx.set_fx_ver(fx_ver->second.as_string());
    }

    auto patch_roll_fwd = fx_obj.find(_X("applyPatches"));
    if (patch_roll_fwd != fx_obj.end())
    {
        m_fx.set_patch_roll_fwd(patch_roll_fwd->second.as_bool());
    }

    auto roll_fwd_on_no_candidate_fx = fx_obj.find(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != fx_obj.end())
    {
        m_fx.set_roll_fwd_on_no_candidate_fx(static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->second.as_integer()));
    }

    return true;
}

bool runtime_config_t::ensure_dev_config_parsed()
{
    trace::verbose(_X("Attempting to read dev runtime config: %s"), m_dev_path.c_str());

    pal::string_t retval;
    if (!pal::file_exists(m_dev_path))
    {
        // Not existing is valid.
        return true;
    }

    pal::ifstream_t file(m_dev_path);
    if (!file.good())
    {
        trace::verbose(_X("File stream not good %s"), m_dev_path.c_str());
        return false;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), m_dev_path.c_str());
    }
    
    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto iter = json.find(_X("runtimeOptions"));
        if (iter != json.end())
        {
            parse_opts(iter->second);
        }
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void) pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), m_dev_path.c_str(), jes.c_str());
        return false;
    }

    return true;
}

bool runtime_config_t::ensure_parsed(const runtime_config_t* app_config)
{
    trace::verbose(_X("Attempting to read runtime config: %s"), m_path.c_str());
    if (!ensure_dev_config_parsed())
    {
        trace::verbose(_X("Did not successfully parse the runtimeconfig.dev.json"));
    }

    pal::string_t retval;
    if (!pal::file_exists(m_path))
    {
        // Not existing is not an error.
        return true;
    }

    pal::ifstream_t file(m_path);
    if (!file.good())
    {
        trace::verbose(_X("File stream not good %s"), m_path.c_str());
        return false;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), m_path.c_str());
    }

    bool rc = true;
    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto iter = json.find(_X("runtimeOptions"));
        if (iter != json.end())
        {
            rc = parse_opts(iter->second);

            if (rc)
            {
                if (app_config == nullptr)
                {
                    // If there is no app_config yet, then we are the app
                    // Read the additionalFrameworks section so we can apply later when each framework's runtimeconfig is read
                    const auto& opts_obj = iter->second.as_object();
                    const auto iter = opts_obj.find(_X("additionalFrameworks"));
                    if (iter != opts_obj.end())
                    {
                        const auto& additional_frameworks = iter->second.as_array();
                        for (const auto& fx : additional_frameworks)
                        {
                            runtime_config_t fx_overrides;
                            const auto& fx_obj = fx.as_object();
                            fx_overrides.m_fx_name = fx_obj.at(_X("name")).as_string();
                            if (fx_overrides.m_fx_name.length() == 0)
                            {
                                trace::verbose(_X("No framework name in additionalFrameworks section."));
                                rc = false;
                                break;
                            }

                            rc = fx_overrides.parse_framework(fx_obj);
                            if (!rc)
                            {
                                break;
                            }

                            m_additional_frameworks[fx_overrides.m_fx_name] = fx_overrides.m_fx;
                        }
                    }

                    // Follow through to step #3 in case the framework is also specified in the additionalFrameworks section
                    app_config = this;
                }

                // Step #3: apply the values from "additionalFrameworks"
                auto overrides = app_config->m_additional_frameworks.find(m_fx_name);
                if (overrides != app_config->m_additional_frameworks.end())
                {
                    set_effective_values(overrides->second);
                }
            }
        }
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void) pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), m_path.c_str(), jes.c_str());
        return false;
    }

    return rc;
}

const pal::string_t& runtime_config_t::get_tfm() const
{
    assert(m_valid);
    return m_tfm;
}

const pal::string_t& runtime_config_t::get_fx_name() const
{
    assert(m_valid);
    return m_fx_name;
}

const pal::string_t& runtime_config_t::get_fx_version() const
{
    assert(m_valid);
    return m_fx_ver;
}

bool runtime_config_t::get_patch_roll_fwd() const
{
    assert(m_valid);
    return m_patch_roll_fwd;
}

roll_fwd_on_no_candidate_fx_option runtime_config_t::get_roll_fwd_on_no_candidate_fx() const
{
    assert(m_valid);
    return m_roll_fwd_on_no_candidate_fx;
}

void runtime_config_t::force_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option value)
{
    assert(m_valid);
    m_roll_fwd_on_no_candidate_fx = value;
    m_fx_readonly.set_roll_fwd_on_no_candidate_fx(value);
}

bool runtime_config_t::get_is_framework_dependent() const
{
    return m_is_framework_dependent;
}

const std::list<pal::string_t>& runtime_config_t::get_probe_paths() const
{
    return m_probe_paths;
}

// Add each property to combined_properties unless the property already exists.
// The effect is the first value wins, which would typically be the app's value.
void runtime_config_t::combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const
{
    for (const auto& kv : m_properties)
    {
        if (combined_properties.find(kv.first) == combined_properties.end())
        {
            combined_properties[kv.first] = kv.second;
        }
    }
}
