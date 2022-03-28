// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "json_parser.h"
#include "pal.h"
#include <external/rapidjson/writer.h>
#include "roll_fwd_on_no_candidate_fx_option.h"
#include "runtime_config.h"
#include "trace.h"
#include "utils.h"
#include "bundle/info.h"
#include <cassert>

// The semantics of applying the runtimeconfig.json values follows, in the following steps from
// first to last, where last always wins. These steps are also annotated in the code here.
// 0) Start with the default values
// 1) Apply the environment settings for DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX
// 2) Apply the values in the current "runtimeOptions" section
// 3) Apply the values in the referenced "frameworks" section
// 4) Apply the environment settings for DOTNET_ROLL_FORWARD
// 5) Apply the overrides (from command line or other)

runtime_config_t::runtime_config_t()
    : m_default_settings()
    , m_override_settings()
    , m_specified_settings(none)
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

runtime_config_t::settings_t::settings_t()
    : has_apply_patches(false)
    , apply_patches(true)
    , has_roll_forward(false)
    , roll_forward(roll_forward_option::Minor)
{
}

void runtime_config_t::parse(const pal::string_t& path, const pal::string_t& dev_path, const settings_t& override_settings)
{
    m_path = path;
    m_dev_path = dev_path;
    m_override_settings = override_settings;

    // Step #0: start with the default values
    m_default_settings.set_apply_patches(true);
    roll_forward_option roll_forward = roll_forward_option::Minor;

    // Step #1: set the defaults from the environment DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX (apply patches has no env. variable)
    pal::string_t env_roll_forward_on_no_candidate_fx;
    if (pal::getenv(_X("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX"), &env_roll_forward_on_no_candidate_fx))
    {
        auto val = static_cast<roll_fwd_on_no_candidate_fx_option>(pal::xtoi(env_roll_forward_on_no_candidate_fx.c_str()));
        roll_forward = roll_fwd_on_no_candidate_fx_to_roll_forward(val);
    }

    m_default_settings.set_roll_forward(roll_forward);

    // Parse the file
    m_valid = ensure_parsed();

    trace::verbose(_X("Runtime config [%s] is valid=[%d]"), path.c_str(), m_valid);
}

bool runtime_config_t::parse_opts(const json_parser_t::value_t& opts)
{
    // Note: both runtime_config and dev_runtime_config call into the function.
    // runtime_config will override whatever dev_runtime_config populated.
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

    // Step #2: set the defaults from the "runtimeOptions"
    const auto& roll_forward = opts_obj.FindMember(_X("rollForward"));
    if (roll_forward != opts_obj.MemberEnd())
    {
        auto val = roll_forward_option_from_string(roll_forward->value.GetString());
        if (val == roll_forward_option::__Last)
        {
            trace::error(_X("Invalid value for property 'rollForward'."));
            return false;
        }
        m_default_settings.set_roll_forward(val);

        if (!mark_specified_setting(specified_roll_forward))
        {
            return false;
        }
    }

    const auto& apply_patches = opts_obj.FindMember(_X("applyPatches"));
    if (apply_patches != opts_obj.MemberEnd())
    {
        m_default_settings.set_apply_patches(apply_patches->value.GetBool());
        if (!mark_specified_setting(specified_roll_forward_on_no_candidate_fx_or_apply_patched))
        {
            return false;
        }
    }

    const auto& roll_fwd_on_no_candidate_fx = opts_obj.FindMember(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != opts_obj.MemberEnd())
    {
        auto val = static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->value.GetInt());
        m_default_settings.set_roll_forward(roll_fwd_on_no_candidate_fx_to_roll_forward(val));
        if (!mark_specified_setting(specified_roll_forward_on_no_candidate_fx_or_apply_patched))
        {
            return false;
        }
    }

    const auto& tfm = opts_obj.FindMember(_X("tfm"));
    if (tfm != opts_obj.MemberEnd())
    {
        m_tfm = tfm->value.GetString();
    }

    // Step #3: read the "framework" and "frameworks" section
    const auto& framework = opts_obj.FindMember(_X("framework"));
    if (framework != opts_obj.MemberEnd())
    {
        m_is_framework_dependent = true;

        fx_reference_t fx_out;
        if (!parse_framework(framework->value, fx_out))
        {
            return false;
        }

        m_frameworks.push_back(fx_out);
    }

    const auto& iter = opts_obj.FindMember(_X("frameworks"));
    if (iter != opts_obj.MemberEnd())
    {
        m_is_framework_dependent = true;

        if (!read_framework_array(iter->value, m_frameworks))
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

        if (!read_framework_array(includedFrameworks->value, m_included_frameworks, /*name_and_version_only*/ true))
        {
            return false;
        }
    }

    return true;
}

namespace
{
    void apply_settings_to_fx_reference(const runtime_config_t::settings_t& settings, fx_reference_t& fx_ref)
    {
        if (settings.has_roll_forward)
        {
            fx_ref.set_roll_forward(settings.roll_forward);
        }

        if (settings.has_apply_patches)
        {
            fx_ref.set_apply_patches(settings.apply_patches);
        }
    }
}

bool runtime_config_t::parse_framework(const json_parser_t::value_t& fx_obj, fx_reference_t& fx_out, bool name_and_version_only)
{
    if (!name_and_version_only)
    {
        apply_settings_to_fx_reference(m_default_settings, fx_out);
    }

    const auto& fx_name = fx_obj.FindMember(_X("name"));
    if (fx_name != fx_obj.MemberEnd())
    {
        fx_out.set_fx_name(fx_name->value.GetString());
    }

    const auto& fx_ver = fx_obj.FindMember(_X("version"));
    if (fx_ver != fx_obj.MemberEnd())
    {
        fx_out.set_fx_version(fx_ver->value.GetString());

        // Release version should prefer release versions, unless the rollForwardToPrerelease is set
        // in which case no preference should be applied.
        if (!name_and_version_only && !fx_out.get_fx_version_number().is_prerelease() && !m_roll_forward_to_prerelease)
        {
            fx_out.set_prefer_release(true);
        }
    }

    if (name_and_version_only)
    {
        return true;
    }

    const auto& roll_forward = fx_obj.FindMember(_X("rollForward"));
    if (roll_forward != fx_obj.MemberEnd())
    {
        auto val = roll_forward_option_from_string(roll_forward->value.GetString());
        if (val == roll_forward_option::__Last)
        {
            trace::error(_X("Invalid value for property 'rollForward'."));
            return false;
        }
        fx_out.set_roll_forward(val);
        if (!mark_specified_setting(specified_roll_forward))
        {
            return false;
        }
    }

    const auto& apply_patches = fx_obj.FindMember(_X("applyPatches"));
    if (apply_patches != fx_obj.MemberEnd())
    {
        fx_out.set_apply_patches(apply_patches->value.GetBool());
        if (!mark_specified_setting(specified_roll_forward_on_no_candidate_fx_or_apply_patched))
        {
            return false;
        }
    }

    const auto& roll_fwd_on_no_candidate_fx = fx_obj.FindMember(_X("rollForwardOnNoCandidateFx"));
    if (roll_fwd_on_no_candidate_fx != fx_obj.MemberEnd())
    {
        auto val = static_cast<roll_fwd_on_no_candidate_fx_option>(roll_fwd_on_no_candidate_fx->value.GetInt());
        fx_out.set_roll_forward(roll_fwd_on_no_candidate_fx_to_roll_forward(val));
        if (!mark_specified_setting(specified_roll_forward_on_no_candidate_fx_or_apply_patched))
        {
            return false;
        }
    }

    // Step #4: apply environment for DOTNET_ROLL_FORWARD
    pal::string_t env_roll_forward;
    if (pal::getenv(_X("DOTNET_ROLL_FORWARD"), &env_roll_forward))
    {
        auto val = roll_forward_option_from_string(env_roll_forward);
        if (val == roll_forward_option::__Last)
        {
            trace::error(_X("Invalid value for environment variable 'DOTNET_ROLL_FORWARD'."));
            return false;
        }

        fx_out.set_roll_forward(val);
    }

    // Step #5: apply overrides (command line and such)
    apply_settings_to_fx_reference(m_override_settings, fx_out);

    return true;
}

bool runtime_config_t::ensure_dev_config_parsed()
{
    trace::verbose(_X("Attempting to read dev runtime config: %s"), m_dev_path.c_str());

    pal::string_t retval;
    if (!pal::realpath(&m_dev_path, true))
    {
        // It is valid for the runtimeconfig.dev.json to not exist.
        return true;
    }

    // runtimeconfig.dev.json is never bundled into the single-file app.
    // So, only a file on disk is processed.
    json_parser_t json;
    if (!json.parse_file(m_dev_path))
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

bool runtime_config_t::read_framework_array(const json_parser_t::value_t& frameworks_json, fx_reference_vector_t& frameworks_out, bool name_and_version_only)
{
    bool rc = true;

    for (const auto& fx_json : frameworks_json.GetArray())
    {
        fx_reference_t fx_out;
        rc = parse_framework(fx_json, fx_out, name_and_version_only);
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
                frameworks_out.begin(),
                frameworks_out.end(),
                [&](const fx_reference_t& item) { return fx_out.get_fx_name() == item.get_fx_name(); })
            != frameworks_out.end())
        {
            trace::verbose(_X("Framework %s already specified."), fx_out.get_fx_name().c_str());
            rc = false;
            break;
        }

        frameworks_out.push_back(fx_out);
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

    if (!bundle::info_t::config_t::probe(m_path) && !pal::realpath(&m_path, true))
    {
        // Not existing is not an error.
        return true;
    }

    json_parser_t json;
    if (!json.parse_file(m_path))
    {
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

const uint32_t runtime_config_t::get_compat_major_version_from_tfm() const
{
    assert(m_valid);

    // TFM is in form
    // - netcoreapp#.#  for <= 3.1
    // - net#.#  for >= 5.0
    // In theory it could contain a suffix like `net6.0-windows` (or more than one)
    // or it may lack the minor version like `net6`. SDK will normalize this, but the runtime should not 100% rely on it

    if (m_tfm.empty())
        return runtime_config_t::unknown_version;

    size_t majorVersionStartIndex;
    const pal::char_t netcoreapp_prefix[] = _X("netcoreapp");
    if (utils::starts_with(m_tfm, netcoreapp_prefix, true))
    {
        majorVersionStartIndex = utils::strlen(netcoreapp_prefix);
    }
    else
    {
        majorVersionStartIndex = utils::strlen(_X("net"));
    }

    if (majorVersionStartIndex >= m_tfm.length())
        return runtime_config_t::unknown_version;

    size_t majorVersionEndIndex = index_of_non_numeric(m_tfm, majorVersionStartIndex);
    if (majorVersionEndIndex == pal::string_t::npos || majorVersionEndIndex == majorVersionStartIndex)
        return runtime_config_t::unknown_version;

    return static_cast<uint32_t>(std::stoul(m_tfm.substr(majorVersionStartIndex, majorVersionEndIndex - majorVersionStartIndex)));
}

bool runtime_config_t::get_is_multilevel_lookup_disabled() const
{
    // Starting with .NET 7, multi-level lookup is fully disabled
    unsigned long compat_major_version = get_compat_major_version_from_tfm();
    return (compat_major_version >= 7 || compat_major_version == runtime_config_t::unknown_version);
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
    m_frameworks[0].set_apply_patches(false);
    m_frameworks[0].set_roll_forward(roll_forward_option::Disable);
}

bool runtime_config_t::mark_specified_setting(specified_setting setting)
{
    // If there's any flag set but the one we're trying to set, it's invalid
    if (m_specified_settings & ~setting)
    {
        trace::error(_X("It's invalid to use both `rollForward` and one of `rollForwardOnNoCandidateFx` or `applyPatches` in the same runtime config."));
        return false;
    }

    m_specified_settings = static_cast<specified_setting>(m_specified_settings | setting);
    return true;
}
