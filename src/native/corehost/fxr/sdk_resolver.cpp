// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "sdk_resolver.h"

#include "trace.h"
#include "utils.h"
#include "sdk_info.h"
#include "json_parser.h"

using namespace std;

namespace
{
    // Note: this array must be in the same order as the `sdk_roll_forward_policy` enumeration
    const pal::char_t* const RollForwardPolicyNames[] =
    {
        _X("unsupported"),
        _X("disable"),
        _X("patch"),
        _X("feature"),
        _X("minor"),
        _X("major"),
        _X("latestPatch"),
        _X("latestFeature"),
        _X("latestMinor"),
        _X("latestMajor"),
    };

    int get_feature_band(const fx_ver_t& version)
    {
        // SDK versions encode both the feature band and patch version in the SemVer patch version.
        // They have the form x.y.znn, where z is the feature band and nn is the patch version.
        // To get the feature band, we divide the SemVer patch by 100
        return version.get_patch() / 100;
    }
}

sdk_resolver::sdk_resolver(bool allow_prerelease)
    : roll_forward{sdk_roll_forward_policy::latest_major}
    , allow_prerelease{allow_prerelease}
    , has_custom_paths{false}
{ }

const fx_ver_t& sdk_resolver::get_requested_version() const
{
    return requested_version;
}

pal::string_t sdk_resolver::resolve(const pal::string_t& dotnet_root, bool print_errors, pal::string_t* out_resolved_root) const
{
    if (trace::is_enabled())
    {
        auto requested = requested_version.is_empty() ? pal::string_t{} : requested_version.as_str();
        trace::verbose(
            _X("Resolving SDKs with version = '%s', rollForward = '%s', allowPrerelease = %s"),
            requested.empty() ? _X("latest") : requested.c_str(),
            to_policy_name(roll_forward),
            allow_prerelease ? _X("true") : _X("false"));
        if (has_custom_paths)
        {
            trace::verbose(_X("  paths = ["));
            for (const pal::string_t& path : paths)
            {
                trace::verbose(_X("    %s"), path.c_str());
            }
            trace::verbose(_X("  ]"));
        }
    }

    pal::string_t resolved_sdk_path;
    fx_ver_t resolved_version;

    std::vector<pal::string_t> locations = get_search_paths(dotnet_root);
    for (auto&& dir : locations)
    {
        append_path(&dir, _X("sdk"));
        if (!pal::fullpath(&dir, true))
        {
            trace::verbose(_X("SDK path [%s] does not exist"), dir.c_str());
            continue;
        }

        // Search paths are in priority order. We take the first match and do not
        // look in any remaining locations.
        if (resolve_sdk_path_and_version(dir, resolved_sdk_path, resolved_version))
        {
            if (out_resolved_root != nullptr)
            {
                out_resolved_root->assign(get_directory(dir));
            }

            break;
        }
    }

    if (!resolved_sdk_path.empty())
    {
        trace::verbose(_X("SDK path resolved to [%s]"), resolved_sdk_path.c_str());
        return resolved_sdk_path;
    }

    if (print_errors)
        print_resolution_error(dotnet_root, _X(""));

    return {};
}

std::vector<pal::string_t> sdk_resolver::get_search_paths(const pal::string_t& dotnet_root) const
{
    std::vector<pal::string_t> locations;
    if (!has_custom_paths)
    {
        if (!dotnet_root.empty())
            locations.push_back(dotnet_root);
    }
    else
    {
        // Use custom paths specified in the global.json
        pal::string_t json_dir = get_directory(global_json.path);
        for (const pal::string_t& path : paths)
        {
            if (path == _X("$host$"))
            {
                locations.push_back(dotnet_root);
            }
            else if (pal::is_path_fully_qualified(path))
            {
                locations.push_back(path);
            }
            else
            {
                // Path is relative to the global.json
                pal::string_t full_path = json_dir;
                append_path(&full_path, path.c_str());
                locations.push_back(full_path);
            }
        }
    }

    return locations;
}

void sdk_resolver::print_resolution_error(const pal::string_t& dotnet_root, const pal::char_t *main_error_prefix) const
{
    if (!error_message.empty())
    {
        // Custom error message specified in the global.json
        trace::error(_X("%s%s"), main_error_prefix, error_message.c_str());
        return;
    }

    bool sdk_exists = false;
    const pal::char_t *no_sdk_message = _X("No .NET SDKs were found.");
    if (!requested_version.is_empty())
    {
        pal::string_t requested = requested_version.as_str();
        trace::error(
            _X("%sA compatible .NET SDK was not found.\n")
            _X("\n")
            _X("Requested SDK version: %s"),
            main_error_prefix,
            requested.c_str());

        bool has_global_file = global_json.is_data_used();
        if (has_global_file)
        {
            trace::error(_X("global.json file: %s"), global_json.path.c_str());
            if (has_custom_paths)
            {
                trace::error(_X("  Search paths:"));
                for (const pal::string_t& path : paths)
                {
                    trace::error(_X("    %s"), path.c_str());
                }
            }
        }

        trace::error(_X("\nInstalled SDKs:"));
        sdk_exists = sdk_info::print_all_sdks(dotnet_root, _X(""));
        if (!sdk_exists)
            trace::error(no_sdk_message);

        trace::error(_X(""));
        if (has_global_file)
        {
            trace::error(_X("Install the [%s] .NET SDK or update [%s] to match an installed SDK."), requested.c_str(), global_json.path.c_str());
        }
        else
        {
            trace::error(_X("Install the [%s] .NET SDK or create a global.json file matching an installed SDK."), requested.c_str());
        }
    }
    else
    {
        trace::error(_X("%s%s"), main_error_prefix, no_sdk_message);
        if (has_custom_paths && paths.empty())
        {
            trace::error(_X("%sEmpty search paths specified in global.json file: %s"), main_error_prefix, global_json.path.c_str());
        }
    }

    if (!sdk_exists)
    {
        trace::error(
            _X("\n")
            _X("Download a .NET SDK:\n")
            DOTNET_CORE_DOWNLOAD_URL);
    }

    trace::error(
        _X("\n")
        _X("Learn about SDK resolution:\n")
        DOTNET_SDK_NOT_FOUND_URL);
}

sdk_resolver sdk_resolver::from_nearest_global_file(bool allow_prerelease)
{
    pal::string_t cwd;
    if (!pal::getcwd(&cwd))
    {
        trace::verbose(_X("Failed to obtain current working dir"));
        assert(cwd.empty());
    }
    else
    {
        trace::verbose(_X("--- Resolving .NET SDK with working dir [%s]"), cwd.c_str());
    }
    return from_nearest_global_file(cwd, allow_prerelease);
}

sdk_resolver sdk_resolver::from_nearest_global_file(const pal::string_t& cwd, bool allow_prerelease)
{
    sdk_resolver resolver{ allow_prerelease };

    pal::string_t global_file_path = find_nearest_global_file(cwd);

    if (global_file_path.empty())
    {
        resolver.global_json.state = global_file_info::state::not_found;
    }
    else
    {
        global_file_info global_file = resolver.parse_global_file(global_file_path);
        if (!global_file.is_data_used())
        {
            // Fall back to a default SDK resolver
            resolver = sdk_resolver{ allow_prerelease };

            trace::verbose(_X("Invalid global.json [%s]:\n  %s"), global_file.path.c_str(), global_file.error_message.c_str());
            trace::warning(
                _X("Ignoring SDK settings in global.json: the latest installed .NET SDK (%s prereleases) will be used"),
                resolver.allow_prerelease ? _X("including") : _X("excluding"));
        }

        resolver.global_json = std::move(global_file);
    }

    // If the requested version is a prerelease, always allow prerelease versions
    if (resolver.requested_version.is_prerelease())
    {
        resolver.allow_prerelease = true;
    }

    return resolver;
}

sdk_roll_forward_policy sdk_resolver::to_policy(const pal::string_t& name)
{
    int index = 0;
    for (auto policy : RollForwardPolicyNames)
    {
        if (pal::strcasecmp(name.c_str(), policy) == 0)
        {
            return static_cast<sdk_roll_forward_policy>(index);
        }

        ++index;
    }

    return sdk_roll_forward_policy::unsupported;
}

const pal::char_t* sdk_resolver::to_policy_name(sdk_roll_forward_policy policy)
{
    auto index = static_cast<int>(policy);

    if (index < 0 || index > (end(RollForwardPolicyNames) - begin(RollForwardPolicyNames)))
    {
        return RollForwardPolicyNames[static_cast<int>(sdk_roll_forward_policy::unsupported)];
    }

    return RollForwardPolicyNames[index];
}

pal::string_t sdk_resolver::find_nearest_global_file(const pal::string_t& cwd)
{
    if (!cwd.empty())
    {
        for (pal::string_t parent_dir, cur_dir = cwd; true; cur_dir = parent_dir)
        {
            auto file = cur_dir;
            append_path(&file, _X("global.json"));

            trace::verbose(_X("Probing path [%s] for global.json"), file.c_str());
            if (pal::file_exists(file))
            {
                trace::verbose(_X("Found global.json [%s]"), file.c_str());
                return file;
            }

            parent_dir = get_directory(cur_dir);
            if (parent_dir.empty() || parent_dir.size() == cur_dir.size())
            {
                trace::verbose(_X("Terminating global.json search at [%s]"), parent_dir.c_str());
                break;
            }
        }
    }

    return {};
}

sdk_resolver::global_file_info sdk_resolver::parse_global_file(const pal::string_t& global_file_path)
{
    global_file_info ret;
    if (global_file_path.empty())
    {
        // Missing global.json is treated as success (use default resolver)
        ret.state = global_file_info::state::not_found;
        return ret;
    }

    trace::verbose(_X("--- Resolving SDK information from global.json [%s]"), global_file_path.c_str());
    ret.path = global_file_path;

    // After we're done parsing `global_file_path`, none of its contents will be referenced
    // from the data private to json_parser_t; it's safe to declare it on the stack.
    json_parser_t json;
    if (!json.parse_file(global_file_path))
    {
        ret.error_message = json.get_error_message();
        ret.state = global_file_info::state::invalid_json;
        return ret;
    }

    const auto& sdk = json.document().FindMember(_X("sdk"));
    if (sdk == json.document().MemberEnd() || sdk->value.IsNull())
    {
        // Missing SDK is treated as success (use default resolver)
        trace::verbose(_X("Value 'sdk' is missing or null in [%s]"), global_file_path.c_str());
        ret.state = global_file_info::state::valid;
        return ret;
    }

    ret.state = global_file_info::state::invalid_data;

    if (!sdk->value.IsObject())
    {
        ret.error_message = _X("Expected a JSON object for the 'sdk' value");
        return ret;
    }

    const auto& version_value = sdk->value.FindMember(_X("version"));
    if (version_value == sdk->value.MemberEnd() || version_value->value.IsNull())
    {
        trace::verbose(_X("Value 'sdk/version' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!version_value->value.IsString())
        {
            ret.error_message = _X("Expected a string for the 'sdk/version' value");
            return ret;
        }

        if (!fx_ver_t::parse(version_value->value.GetString(), &requested_version, false))
        {
            ret.error_message = utils::format_string(_X("Version '%s' is not valid for the 'sdk/version' value"), version_value->value.GetString());
            return ret;
        }

        // The default policy when a version is specified is 'patch'
        roll_forward = sdk_roll_forward_policy::patch;
    }

    const auto& roll_forward_value = sdk->value.FindMember(_X("rollForward"));
    if (roll_forward_value == sdk->value.MemberEnd() || roll_forward_value->value.IsNull())
    {
        trace::verbose(_X("Value 'sdk/rollForward' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!roll_forward_value->value.IsString())
        {
            ret.error_message = _X("Expected a string for the 'sdk/rollForward' value");
            return ret;
        }

        roll_forward = to_policy(roll_forward_value->value.GetString());
        if (roll_forward == sdk_roll_forward_policy::unsupported)
        {
            ret.error_message = utils::format_string(_X("The roll-forward policy '%s' is not supported for the 'sdk/rollForward' value"), roll_forward_value->value.GetString());
            return ret;
        }

        // All policies other than 'latestMajor' require a version to operate
        if (roll_forward != sdk_roll_forward_policy::latest_major && requested_version.is_empty())
        {
            ret.error_message = utils::format_string(_X("The roll-forward policy '%s' requires a 'sdk/version' value"), roll_forward_value->value.GetString());
            return ret;
        }
    }

    const auto& allow_prerelease_value = sdk->value.FindMember(_X("allowPrerelease"));
    if (allow_prerelease_value == sdk->value.MemberEnd() || allow_prerelease_value->value.IsNull())
    {
        trace::verbose(_X("Value 'sdk/allowPrerelease' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!allow_prerelease_value->value.IsBool())
        {
            ret.error_message = _X("Expected a boolean for the 'sdk/allowPrerelease' value");
            return ret;
        }

        allow_prerelease = allow_prerelease_value->value.GetBool();

        if (!allow_prerelease && requested_version.is_prerelease())
        {
            trace::warning(_X("Ignoring the 'sdk/allowPrerelease' value in [%s] because a prerelease version was specified"), global_file_path.c_str());
            allow_prerelease = true;
        }
    }

    const auto& paths_value = sdk->value.FindMember(_X("paths"));
    if (paths_value != sdk->value.MemberEnd() && !paths_value->value.IsNull())
    {
        if (!paths_value->value.IsArray())
        {
            ret.error_message = _X("Expected an array for 'sdk/paths' value");
            return ret;
        }

        has_custom_paths = true;
        const auto& paths_array = paths_value->value.GetArray();
        paths.reserve(paths_array.Size());
        for (uint32_t i = 0; i < paths_array.Size(); ++i)
        {
            const auto& path = paths_array[i];
            if (!path.IsString())
            {
                trace::warning(_X("Ignoring non-string 'sdk/paths[%u]' value in [%s]"), i, global_file_path.c_str());
                continue;
            }

            paths.push_back(path.GetString());
        }
    }

    const auto& error_message_value = sdk->value.FindMember(_X("errorMessage"));
    if (error_message_value != sdk->value.MemberEnd() && !error_message_value->value.IsNull())
    {
        if (!error_message_value->value.IsString())
        {
            ret.error_message = _X("Expected a string for the 'sdk/errorMessage' value");
            return ret;
        }

        error_message = error_message_value->value.GetString();
    }

    // SDK feature bands start at 1, so a version with a feature band < 1 is not a valid version.
    // We want to provide an error message, but we should still use the settings in the global.json
    // and not fall back to the default resolver.
    if (!requested_version.is_empty() && get_feature_band(requested_version) < 1)
    {
        ret.error_message = utils::format_string(_X("Version '%s' is not valid for the 'sdk/version' value. SDK feature bands start at 1 - for example, %d.%d.100"), requested_version.as_str().c_str(), requested_version.get_major(), requested_version.get_minor());
        ret.state = global_file_info::state::__invalid_data_no_fallback;
        return ret;
    }

    ret.state = global_file_info::state::valid;
    return ret;
}

bool sdk_resolver::matches_policy(const fx_ver_t& current) const
{
    // Check for unallowed prerelease versions or a disabled/unsupported roll-forward policy
    if (current.is_empty() ||
        (!allow_prerelease && current.is_prerelease()) ||
        roll_forward == sdk_roll_forward_policy::unsupported ||
        roll_forward == sdk_roll_forward_policy::disable)
    {
        return false;
    }

    // If no version was requested, then all versions match
    if (requested_version.is_empty())
    {
        return true;
    }

    int requested_feature = get_feature_band(requested_version);
    int current_feature = get_feature_band(current);

    int requested_minor = requested_version.get_minor();
    int current_minor = current.get_minor();

    int requested_major = requested_version.get_major();
    int current_major = current.get_major();

    // Rolling forward on patch requires the same major/minor/feature
    if ((roll_forward == sdk_roll_forward_policy::patch ||
         roll_forward == sdk_roll_forward_policy::latest_patch) &&
        (current_major != requested_major ||
         current_minor != requested_minor ||
         current_feature != requested_feature))
    {
        return false;
    }

    // Rolling forward on feature requires the same major and minor
    if ((roll_forward == sdk_roll_forward_policy::feature ||
         roll_forward == sdk_roll_forward_policy::latest_feature) &&
        (current_major != requested_major ||
         current_minor != requested_minor))
    {
        return false;
    }

    // Rolling forward on minor requires the same major
    if ((roll_forward == sdk_roll_forward_policy::minor ||
         roll_forward == sdk_roll_forward_policy::latest_minor) &&
        (current_major != requested_major))
    {
        return false;
    }

    // The version must be at least what was requested
    return current >= requested_version;
}

bool sdk_resolver::is_better_match(const fx_ver_t& current, const fx_ver_t& previous) const
{
    // Assumption: both current and previous (if there is one) match the policy

    // If no previous match, then the current one is better
    if (previous.is_empty())
    {
        return true;
    }

    // Use the later of the two if there is no requested version, the policy requires it,
    // or if everything is equal up to the feature level (latest patch always wins)
    if (requested_version.is_empty() ||
        is_policy_use_latest() ||
        (current.get_major() == previous.get_major() &&
         current.get_minor() == previous.get_minor() &&
         (get_feature_band(current) == get_feature_band(previous))))
    {
        // Accept the later of the versions
        // This will also handle stable and prerelease comparisons
        return current > previous;
    }

    return current < previous;
}

bool sdk_resolver::exact_match_preferred() const
{
    return roll_forward == sdk_roll_forward_policy::disable ||
           roll_forward == sdk_roll_forward_policy::patch;
}

bool sdk_resolver::is_policy_use_latest() const
{
    return roll_forward == sdk_roll_forward_policy::latest_patch ||
           roll_forward == sdk_roll_forward_policy::latest_feature ||
           roll_forward == sdk_roll_forward_policy::latest_minor ||
           roll_forward == sdk_roll_forward_policy::latest_major;
}

bool sdk_resolver::resolve_sdk_path_and_version(const pal::string_t& dir, pal::string_t& sdk_path, fx_ver_t& resolved_version) const
{
    trace::verbose(_X("Searching for SDK versions in [%s]"), dir.c_str());

    // If an exact match is preferred, check for the existence of the version
    if (exact_match_preferred() && !requested_version.is_empty())
    {
        auto probe_path = dir;
        append_path(&probe_path, requested_version.as_str().c_str());

        pal::string_t sdk_dll_maybe = probe_path;
        append_path(&sdk_dll_maybe, SDK_DOTNET_DLL);
        if (pal::file_exists(sdk_dll_maybe))
        {
            trace::verbose(_X("Found requested SDK [%s]"), probe_path.c_str());
            sdk_path = std::move(probe_path);
            resolved_version = requested_version;

            // The SDK path has been resolved
            return true;
        }
        else if (trace::is_enabled() && pal::directory_exists(probe_path))
        {
            trace::verbose(_X("Ignoring version [%s] without ") SDK_DOTNET_DLL, requested_version.as_str().c_str());
        }
    }

    // No match - we did not find an exact match and roll forward is disabled
    if (roll_forward == sdk_roll_forward_policy::disable)
        return false;

    bool found = false;
    pal::string_t resolved_version_str = resolved_version.is_empty() ? pal::string_t{} : resolved_version.as_str();
    sdk_info::enumerate_sdk_paths(
        dir,
        [&](const fx_ver_t& version, const pal::string_t& version_str)
        {
            if (!matches_policy(version))
            {
                trace::verbose(_X("Ignoring version [%s] because it does not match the roll-forward policy"), version_str.c_str());
                return true;
            }

            if (!is_better_match(version, resolved_version))
            {
                trace::verbose(
                    _X("Ignoring version [%s] because it is not a better match than [%s]"),
                    version_str.c_str(),
                    resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
                );
                return true;
            }

            return false;
        },
        [&](const fx_ver_t& version, const pal::string_t& version_str, const pal::string_t& full_path)
        {
            trace::verbose(
                _X("Version [%s] is a better match than [%s]"),
                version_str.c_str(),
                resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
            );

            found = true;
            resolved_version = version;
            resolved_version_str = std::move(version_str);
        });

    if (found)
    {
        sdk_path = dir;
        append_path(&sdk_path, resolved_version_str.c_str());
    }

    return found;
}
