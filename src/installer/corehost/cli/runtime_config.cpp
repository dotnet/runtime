// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "cpprest/json.h"
#include "runtime_config.h"
#include <cassert>


// The semantics of applying the runtimeconfig.json values follows, in the following steps from
// first to last, where last always wins. These steps are also annotated in the code here.
// 1) Apply the environment settings
// 2) Apply the values in the current "runtimeOptions" section
// 3) Apply the values in the referenced "frameworks" section
// 4) Apply the overrides (from command line or other)

runtime_config_t::runtime_config_t()
    : m_is_framework_dependent(false)
    , m_valid(false)
{
}

void runtime_config_t::parse(const pal::string_t& path, const pal::string_t& dev_path, const fx_reference_t& fx_ref, const fx_reference_t& override_settings)
{
    m_path = path;
    m_dev_path = dev_path;
    m_fx_ref = fx_ref;
    m_fx_overrides = override_settings;

    // Step #1: set the defaults from the environment
    m_fx_defaults.set_patch_roll_fwd(true);

    roll_fwd_on_no_candidate_fx_option roll_fwd_option = roll_fwd_on_no_candidate_fx_option::minor;
    pal::string_t env_no_candidate;
    if (pal::getenv(_X("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX"), &env_no_candidate))
    {
        roll_fwd_option = static_cast<roll_fwd_on_no_candidate_fx_option>(pal::xtoi(env_no_candidate.c_str()));
    }

    m_fx_defaults.set_roll_fwd_on_no_candidate_fx(roll_fwd_option);

    // Parse the file
    m_valid = ensure_parsed();

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
                : property.second.serialize();
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

    // Step #2: set the defaults from the "runtimeOptions"
    auto patch_roll_fwd = opts_obj.find(_X("applyPatches"));
    if (patch_roll_fwd != opts_obj.end())
    {
        m_fx_defaults.set_patch_roll_fwd(patch_roll_fwd->second.as_bool());
    }

    auto roll_fwd_on_no_candidate_fx = opts_obj.find(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != opts_obj.end())
    {
        auto val = static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->second.as_integer());
        m_fx_defaults.set_roll_fwd_on_no_candidate_fx(val);
    }

    auto tfm = opts_obj.find(_X("tfm"));
    if (tfm != opts_obj.end())
    {
        m_tfm = tfm->second.as_string();
    }

    // Step #3: read the "framework" and "frameworks" section
    bool rc = true;
    auto framework =  opts_obj.find(_X("framework"));
    if (framework != opts_obj.end())
    {
        m_is_framework_dependent = true;

        const auto& framework_obj = framework->second.as_object();

        fx_reference_t fx_out;
        rc = parse_framework(framework_obj, fx_out);
        if (rc)
        {
            m_frameworks.push_back(fx_out);
        }
    }

    if (rc)
    {
        auto iter = opts_obj.find(_X("frameworks"));
        if (iter != opts_obj.end())
        {
            m_is_framework_dependent = true;

            const auto& frameworks_obj = iter->second.as_array();
            rc = read_framework_array(frameworks_obj);
        }
    }

    return rc;
}

bool runtime_config_t::parse_framework(const json_object& fx_obj, fx_reference_t& fx_out)
{
    fx_out.apply_settings_from(m_fx_defaults);

    auto fx_name= fx_obj.find(_X("name"));
    if (fx_name != fx_obj.end())
    {
        fx_out.set_fx_name(fx_name->second.as_string());
    }

    auto fx_ver = fx_obj.find(_X("version"));
    if (fx_ver != fx_obj.end())
    {
        fx_out.set_fx_version(fx_ver->second.as_string());
    }

    auto patch_roll_fwd = fx_obj.find(_X("applyPatches"));
    if (patch_roll_fwd != fx_obj.end())
    {
        fx_out.set_patch_roll_fwd(patch_roll_fwd->second.as_bool());
    }

    auto roll_fwd_on_no_candidate_fx = fx_obj.find(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != fx_obj.end())
    {
        fx_out.set_roll_fwd_on_no_candidate_fx(static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->second.as_integer()));
    }

    fx_out.apply_settings_from(m_fx_overrides);

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

bool runtime_config_t::read_framework_array(web::json::array frameworks_json)
{
    bool rc = true;

    for (const auto& fx_json : frameworks_json)
    {
        const auto& fx_obj = fx_json.as_object();

        fx_reference_t fx_out;
        rc = parse_framework(fx_obj, fx_out);
        if (!rc)
        {
            break;
        }

        if (fx_out.get_fx_name().length() == 0)
        {
            trace::verbose(_X("No framework name specified."));
            rc = false;
            break;
        }

        if (std::find_if(
                m_frameworks.begin(),
                m_frameworks.end(),
                [&](const fx_reference_t& item) { return fx_out.get_fx_name() == item.get_fx_name(); })
            != m_frameworks.end())
        {
            trace::verbose(_X("Framework %s already specified."), fx_out.get_fx_name().c_str());
            rc = false;
            break;
        }

        m_frameworks.push_back(fx_out);
    }

    return rc;
}

bool runtime_config_t::ensure_parsed()
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

void runtime_config_t::set_fx_version(pal::string_t version)
{
    assert(m_frameworks.size() > 0);

    m_frameworks[0].set_fx_version(version);
    m_frameworks[0].set_patch_roll_fwd(false);
    m_frameworks[0].set_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option::disabled);
    m_frameworks[0].set_use_exact_version(true);
}
