// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "cpprest/json.h"
#include "runtime_config.h"
#include <cassert>

runtime_config_t::runtime_config_t(const pal::string_t& path, const pal::string_t& dev_path)
    : m_patch_roll_fwd(true)
    , m_prerelease_roll_fwd(false)
    , m_path(path)
    , m_dev_path(dev_path)
    , m_portable(false)
{
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
    }

    auto prerelease_roll_fwd = opts_obj.find(_X("preReleaseRollForward"));
    if (prerelease_roll_fwd != opts_obj.end())
    {
        m_prerelease_roll_fwd = prerelease_roll_fwd->second.as_bool();
    }

    auto tfm = opts_obj.find(_X("tfm"));
    if (tfm != opts_obj.end())
    {
        m_tfm = tfm->second.as_string();
    }

    auto framework =  opts_obj.find(_X("framework"));
    if (framework == opts_obj.end())
    {
        return true;
    }

    m_portable = true;

    const auto& fx_obj = framework->second.as_object();
    m_fx_name = fx_obj.at(_X("name")).as_string();
    m_fx_ver = fx_obj.at(_X("version")).as_string();
    return true;
}

bool runtime_config_t::ensure_dev_config_parsed()
{
    trace::verbose(_X("Attempting to read dev runtime config: %s"), m_dev_path.c_str());

    pal::string_t retval;
    if (!pal::file_exists(m_dev_path))
    {
        // Not existing is not an error.
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
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), m_path.c_str(), jes.c_str());
        return false;
    }
    return true;
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

bool runtime_config_t::get_prerelease_roll_fwd() const
{
    assert(m_valid);
    return m_prerelease_roll_fwd;
}

bool runtime_config_t::get_portable() const
{
    return m_portable;
}

const std::list<pal::string_t>& runtime_config_t::get_probe_paths() const
{
    return m_probe_paths;
}

void runtime_config_t::config_kv(std::vector<pal::string_t>* keys, std::vector<pal::string_t>* values) const
{
    for (const auto& kv : m_properties)
    {
        keys->push_back(kv.first);
        values->push_back(kv.second);
    }
}
