// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "json_parser.h"
#include "pal.h"
#include <rapidjson/writer.h>
#include "runtime_config.h"
#include "trace.h"
#include "utils.h"
#include "bundle/info.h"
#include <cassert>

// Roll-forward settings are selected in precedence order. The code is also annotated with these numbers.
// 0) Overrides (from command line or other)
// 1) The environment setting for DOTNET_ROLL_FORWARD
// 2) The referenced "frameworks" section
// 3) The config's "runtimeOptions" section

runtime_config_t::runtime_config_t()
    : m_runtime_options_roll_forward()
    , m_override_roll_forward()
    , m_is_framework_dependent(false)
    , m_valid(false)
    , m_roll_forward_to_prerelease(false)
{
    pal::string_t roll_forward_to_prerelease_env;
    if (pal::getenv(_X("DOTNET_ROLL_FORWARD_TO_PRERELEASE"), &roll_forward_to_prerelease_env))
    {
        auto roll_forward_to_prerelease_val = pal::xtoi(roll_forward_to_prerelease_env.c_str());
        m_roll_forward_to_prerelease = (roll_forward_to_prerelease_val == 1);
    }
}

void runtime_config_t::parse(
    const pal::string_t& path,
    const pal::string_t& dev_path,
    const std::optional<roll_forward_option>& override_roll_forward)
{
    m_path = path;
    m_dev_path = dev_path;

    // 0) Command-line and other overrides.
    m_override_roll_forward = override_roll_forward;

    // Parse the file
    m_valid = ensure_parsed();

    trace::verbose(_X("Runtime config [%s] is valid=[%d]"), path.c_str(), m_valid);
}

bool runtime_config_t::parse_opts(const json_parser_t::value_t& opts)
{
    // Note: both runtime_config and dev_runtime_config call into the function.
    // dev_runtime_config is parsed first. The configProperties from the dev config take precedence
    // over the configProperties from the runtime config.
    if (opts.IsNull())
    {
        return true;
    }

    if (!opts.IsObject())
    {
        return false;
    }

    const auto& opts_obj = opts.GetObject();

    const auto& properties = opts_obj.FindMember(_X("configProperties"));
    if (properties != opts_obj.MemberEnd())
    {
        const auto& properties_obj = properties->value.GetObject();
        m_properties.reserve(properties_obj.MemberCount());
        for (const auto& property : properties_obj)
        {
            if (m_properties.count(property.name.GetString()) != 0)
                continue;

            if (property.value.IsString())
            {
                m_properties[property.name.GetString()] = property.value.GetString();
            }
            else
            {
                using string_buffer_t = rapidjson::GenericStringBuffer<json_parser_t::internal_encoding_type_t>;

                string_buffer_t sb;
                rapidjson::Writer<string_buffer_t, json_parser_t::internal_encoding_type_t,
                                  json_parser_t::internal_encoding_type_t> writer{sb};

                property.value.Accept(writer);
                m_properties[property.name.GetString()] = sb.GetString();
            }
        }
    }

    const auto& probe_paths = opts_obj.FindMember(_X("additionalProbingPaths"));
    if (probe_paths != opts_obj.MemberEnd())
    {
        if (probe_paths->value.IsString())
        {
            m_probe_paths.insert(m_probe_paths.begin(), probe_paths->value.GetString());
        }
        else if (probe_paths->value.IsArray())
        {
            using const_value_iter_t = json_parser_t::value_t::ConstValueIterator;
            std::reverse_iterator<const_value_iter_t> begin{probe_paths->value.End()};
            std::reverse_iterator<const_value_iter_t> end{probe_paths->value.Begin()};

            for (; begin != end; begin++)
            {
                m_probe_paths.push_front(begin->GetString());
            }
        }
        else
        {
            trace::error(_X("Invalid value for property 'additionalProbingPaths'."));
            return false;
        }
    }

    // 3) "rollForward" value from "runtimeOptions".
    const auto& roll_forward = opts_obj.FindMember(_X("rollForward"));
    if (roll_forward != opts_obj.MemberEnd())
    {
        roll_forward_option val = roll_forward_option_from_string(roll_forward->value.GetString());
        if (val == roll_forward_option::__Last)
        {
            trace::error(_X("Invalid value for property 'rollForward'."));
            return false;
        }

        m_runtime_options_roll_forward = val;
    }

    const auto& tfm = opts_obj.FindMember(_X("tfm"));
    if (tfm != opts_obj.MemberEnd())
    {
        m_tfm = tfm->value.GetString();
    }

    const auto& framework = opts_obj.FindMember(_X("framework"));
    const auto& frameworks = opts_obj.FindMember(_X("frameworks"));
    if (framework != opts_obj.MemberEnd() || frameworks != opts_obj.MemberEnd())
    {
        m_is_framework_dependent = true;

        if (!m_override_roll_forward.has_value())
        {
            // 1) DOTNET_ROLL_FORWARD environment variable.
            pal::string_t environment_roll_forward;
            if (pal::getenv(_X("DOTNET_ROLL_FORWARD"), &environment_roll_forward))
            {
                roll_forward_option val = roll_forward_option_from_string(environment_roll_forward);
                if (val == roll_forward_option::__Last)
                {
                    trace::error(_X("Invalid value for environment variable 'DOTNET_ROLL_FORWARD'."));
                    return false;
                }

                m_override_roll_forward = val;
            }
        }
    }

    // Read the "framework" section.
    if (framework != opts_obj.MemberEnd())
    {
        fx_reference_t fx_out;
        if (!parse_framework(framework->value, /*name_and_version_only*/ false, fx_out))
        {
            return false;
        }

        m_frameworks.push_back(fx_out);
    }

    // Read the "frameworks" section.
    if (frameworks != opts_obj.MemberEnd())
    {
        if (!read_framework_array(frameworks->value, /*name_and_version_only*/ false, m_frameworks))
        {
            return false;
        }
    }

    const auto& includedFrameworks = opts_obj.FindMember(_X("includedFrameworks"));
    if (includedFrameworks != opts_obj.MemberEnd())
    {
        if (m_is_framework_dependent)
        {
            trace::error(_X("It's invalid to specify both `framework`/`frameworks` and `includedFrameworks` properties."));
            return false;
        }

        if (!read_framework_array(includedFrameworks->value, /*name_and_version_only*/ true, m_included_frameworks))
        {
            return false;
        }
    }

    return true;
}

bool runtime_config_t::parse_framework(const json_parser_t::value_t& fx_obj, bool name_and_version_only, fx_reference_t& fx_out)
{
    const auto& fx_name = fx_obj.FindMember(_X("name"));
    if (fx_name == fx_obj.MemberEnd())
    {
        using string_buffer_t = rapidjson::GenericStringBuffer<json_parser_t::internal_encoding_type_t>;
        string_buffer_t sb;
        rapidjson::Writer<string_buffer_t, json_parser_t::internal_encoding_type_t,
            json_parser_t::internal_encoding_type_t> writer{sb};
        fx_obj.Accept(writer);

        trace::error(_X("No framework name specified: %s"), sb.GetString());
        return false;
    }

    fx_out.set_fx_name(fx_name->value.GetString());

    const auto& fx_ver = fx_obj.FindMember(_X("version"));
    if (fx_ver == fx_obj.MemberEnd())
    {
        trace::error(_X("Framework '%s' is missing a version."), fx_out.get_fx_name().c_str());
        return false;
    }

    fx_out.set_fx_version(fx_ver->value.GetString());

    if (name_and_version_only)
        return true;

    // Release version should prefer release versions, unless the rollForwardToPrerelease is set
    // in which case no preference should be applied.
    if (!fx_out.get_fx_version_number().is_prerelease() && !m_roll_forward_to_prerelease)
    {
        fx_out.set_prefer_release(true);
    }

    if (m_override_roll_forward.has_value())
    {
        // 0) Command-line and other overrides, or 1) DOTNET_ROLL_FORWARD.
        fx_out.set_roll_forward(*m_override_roll_forward);
    }
    else
    {
        // 2) "rollForward" value from the framework reference.
        const auto& roll_forward = fx_obj.FindMember(_X("rollForward"));
        if (roll_forward != fx_obj.MemberEnd())
        {
            roll_forward_option val = roll_forward_option_from_string(roll_forward->value.GetString());
            if (val == roll_forward_option::__Last)
            {
                trace::error(_X("Invalid value for property 'rollForward'."));
                return false;
            }

            fx_out.set_roll_forward(val);
        }
        else if (m_runtime_options_roll_forward.has_value())
        {
            // 3) "rollForward" value from "runtimeOptions".
            fx_out.set_roll_forward(*m_runtime_options_roll_forward);
        }
    }

    return true;
}

bool runtime_config_t::ensure_dev_config_parsed()
{
    trace::verbose(_X("Attempting to read dev runtime config: %s"), m_dev_path.c_str());

    pal::string_t retval;
    if (!pal::fullpath(&m_dev_path, true))
    {
        // It is valid for the runtimeconfig.dev.json to not exist.
        return true;
    }

    // runtimeconfig.dev.json is never bundled into the single-file app.
    // So, only a file on disk is processed.
    json_parser_t json;
    if (!json.parse_fully_trusted_file(m_dev_path))
    {
        return false;
    }

    const auto& runtime_opts = json.document().FindMember(_X("runtimeOptions"));
    if (runtime_opts != json.document().MemberEnd())
    {
        parse_opts(runtime_opts->value);
    }

    return true;
}

bool runtime_config_t::read_framework_array(const json_parser_t::value_t& frameworks_json, bool name_and_version_only, fx_reference_vector_t& frameworks_out)
{
    for (const auto& fx_json : frameworks_json.GetArray())
    {
        fx_reference_t fx_out;
        if (!parse_framework(fx_json, name_and_version_only, fx_out))
            return false;

        if (std::find_if(
                frameworks_out.begin(),
                frameworks_out.end(),
                [&](const fx_reference_t& item) { return fx_out.get_fx_name() == item.get_fx_name(); })
            != frameworks_out.end())
        {
            trace::verbose(_X("Framework %s already specified."), fx_out.get_fx_name().c_str());
            return false;
        }

        frameworks_out.push_back(fx_out);
    }

    return true;
}

bool runtime_config_t::ensure_parsed()
{
    if (!ensure_dev_config_parsed())
    {
        trace::verbose(_X("Did not successfully parse the runtimeconfig.dev.json"));
    }

    trace::verbose(_X("Attempting to read runtime config: %s"), m_path.c_str());
    if (!bundle::info_t::config_t::probe(m_path) && !pal::fullpath(&m_path, true))
    {
        // Not existing is not an error.
        trace::verbose(_X("Runtime config does not exist at [%s]"), m_path.c_str());
        return true;
    }

    json_parser_t json;
    if (!json.parse_fully_trusted_file(m_path))
    {
        trace::error(_X("Failed to parse file [%s]. %s"), m_path.c_str(), json.get_error_message().c_str());
        return false;
    }

    const auto& runtimeOpts = json.document().FindMember(_X("runtimeOptions"));
    if (runtimeOpts != json.document().MemberEnd())
    {
        return parse_opts(runtimeOpts->value);
    }

    return false;
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
    m_frameworks[0].set_roll_forward(roll_forward_option::Disable);
}
