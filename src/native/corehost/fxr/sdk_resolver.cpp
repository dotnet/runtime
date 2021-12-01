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
}

sdk_resolver::sdk_resolver(bool allow_prerelease) :
    sdk_resolver({}, sdk_roll_forward_policy::latest_major, allow_prerelease)
{
}

sdk_resolver::sdk_resolver(fx_ver_t version, sdk_roll_forward_policy roll_forward, bool allow_prerelease) :
    version(move(version)),
    roll_forward(roll_forward),
    allow_prerelease(allow_prerelease)
{
}

pal::string_t const& sdk_resolver::global_file_path() const
{
    return global_file;
}

pal::string_t sdk_resolver::resolve(const pal::string_t& dotnet_root, bool print_errors) const
{
    if (trace::is_enabled())
    {
        auto requested = version.is_empty() ? pal::string_t{} : version.as_str();
        trace::verbose(
            _X("Resolving SDKs with version = '%s', rollForward = '%s', allowPrerelease = %s"),
            requested.empty() ? _X("latest") : requested.c_str(),
            to_policy_name(roll_forward),
            allow_prerelease ? _X("true") : _X("false"));
    }

    pal::string_t resolved_sdk_path;
    fx_ver_t resolved_version;

    vector<pal::string_t> locations;
    get_framework_and_sdk_locations(dotnet_root, &locations);

    for (auto&& dir : locations)
    {
        append_path(&dir, _X("sdk"));

        if (resolve_sdk_path_and_version(dir, resolved_sdk_path, resolved_version))
        {
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

void sdk_resolver::print_resolution_error(const pal::string_t& dotnet_root, const pal::char_t *prefix) const
{
    bool sdk_exists = false;
    const pal::char_t *no_sdk_message = _X("It was not possible to find any installed .NET SDKs.");
    if (!version.is_empty())
    {
        pal::string_t requested = version.as_str();
        if (!global_file.empty())
        {
            trace::error(_X("%sA compatible installed .NET SDK for global.json version [%s] from [%s] was not found."), prefix, requested.c_str(), global_file.c_str());
            trace::error(_X("%sInstall the [%s] .NET SDK or update [%s] with an installed .NET SDK:"), prefix, requested.c_str(), global_file.c_str());
        }
        else
        {
            trace::error(_X("%sA compatible installed .NET SDK version [%s] was not found."), prefix, requested.c_str());
            trace::error(_X("%sInstall the [%s] .NET SDK or create a global.json file with an installed .NET SDK:"), prefix, requested.c_str());
        }

        sdk_exists = sdk_info::print_all_sdks(dotnet_root, pal::string_t{prefix}.append(_X("  ")));
        if (!sdk_exists)
            trace::error(_X("%s  %s"), prefix, no_sdk_message);
    }
    else
    {
        trace::error(_X("%s%s"), prefix, no_sdk_message);
    }

    if (!sdk_exists)
    {
        trace::error(_X("%sInstall a .NET SDK from:"), prefix);
        trace::error(_X("%s  %s"), prefix, DOTNET_CORE_DOWNLOAD_URL);
    }
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

    if (!resolver.parse_global_file(find_nearest_global_file(cwd)))
    {
        // Fall back to a default SDK resolver
        resolver = sdk_resolver{ allow_prerelease };

        trace::warning(
            _X("Ignoring SDK settings in global.json: the latest installed .NET SDK (%s prereleases) will be used"),
            resolver.allow_prerelease ? _X("including") : _X("excluding"));
    }

    // If the requested version is a prerelease, always allow prerelease versions
    if (resolver.version.is_prerelease())
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

bool sdk_resolver::parse_global_file(pal::string_t global_file_path)
{
    if (global_file_path.empty())
    {
        // Missing global.json is treated as success (use default resolver)
        return true;
    }

    trace::verbose(_X("--- Resolving SDK information from global.json [%s]"), global_file_path.c_str());

    // After we're done parsing `global_file_path`, none of its contents will be referenced
    // from the data private to json_parser_t; it's safe to declare it on the stack.
    json_parser_t json;
    if (!json.parse_file(global_file_path))
    {
        return false;
    }

    const auto& sdk = json.document().FindMember(_X("sdk"));
    if (sdk == json.document().MemberEnd() || sdk->value.IsNull())
    {
        // Missing SDK is treated as success (use default resolver)
        trace::verbose(_X("Value 'sdk' is missing or null in [%s]"), global_file_path.c_str());
        return true;
    }

    if (!sdk->value.IsObject())
    {
        trace::warning(_X("Expected a JSON object for the 'sdk' value in [%s]"), global_file_path.c_str());
        return false;
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
            trace::warning(_X("Expected a string for the 'sdk/version' value in [%s]"), global_file_path.c_str());
            return false;
        }

        if (!fx_ver_t::parse(version_value->value.GetString(), &version, false))
        {
            trace::warning(
                _X("Version '%s' is not valid for the 'sdk/version' value in [%s]"),
                version_value->value.GetString(),
                global_file_path.c_str()
            );
            return false;
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
            trace::warning(_X("Expected a string for the 'sdk/rollForward' value in [%s]"), global_file_path.c_str());
            return false;
        }

        roll_forward = to_policy(roll_forward_value->value.GetString());
        if (roll_forward == sdk_roll_forward_policy::unsupported)
        {
            trace::warning(
                _X("The roll-forward policy '%s' is not supported for the 'sdk/rollForward' value in [%s]"),
                roll_forward_value->value.GetString(),
                global_file_path.c_str()
            );
            return false;
        }

        // All policies other than 'latestMajor' require a version to operate
        if (roll_forward != sdk_roll_forward_policy::latest_major && version.is_empty())
        {
            trace::warning(
                _X("The roll-forward policy '%s' requires a 'sdk/version' value in [%s]"),
                roll_forward_value->value.GetString(),
                global_file_path.c_str()
            );
            return false;
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
            trace::warning(_X("Expected a boolean for the 'sdk/allowPrerelease' value in [%s]"), global_file_path.c_str());
            return false;
        }

        allow_prerelease = allow_prerelease_value->value.GetBool();

        if (!allow_prerelease && version.is_prerelease())
        {
            trace::warning(_X("Ignoring the 'sdk/allowPrerelease' value in [%s] because a prerelease version was specified"), global_file_path.c_str());
            allow_prerelease = true;
        }
    }

    global_file = move(global_file_path);
    return true;
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
    if (version.is_empty())
    {
        return true;
    }

    int requested_feature = version.get_patch() / 100;
    int current_feature = current.get_patch() / 100;

    int requested_minor = version.get_minor();
    int current_minor = current.get_minor();

    int requested_major = version.get_major();
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
    return current >= version;
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
    if (version.is_empty() ||
        is_policy_use_latest() ||
        (current.get_major() == previous.get_major() &&
         current.get_minor() == previous.get_minor() &&
         (current.get_patch() / 100) == (previous.get_patch() / 100)))
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
    if (exact_match_preferred() && !version.is_empty())
    {
        auto probe_path = dir;
        append_path(&probe_path, version.as_str().c_str());

        if (pal::directory_exists(probe_path))
        {
            trace::verbose(_X("Found requested SDK directory [%s]"), probe_path.c_str());
            sdk_path = move(probe_path);
            resolved_version = version;

            // The SDK path has been resolved
            return true;
        }
    }

    if (roll_forward == sdk_roll_forward_policy::disable)
    {
        // Not yet fully resolved
        return false;
    }

    vector<pal::string_t> versions;
    pal::readdir_onlydirectories(dir, &versions);

    bool changed = false;
    pal::string_t resolved_version_str = resolved_version.is_empty() ? pal::string_t{} : resolved_version.as_str();
    for (auto&& version : versions)
    {
        fx_ver_t ver;
        if (!fx_ver_t::parse(version, &ver, false))
        {
            trace::verbose(_X("Ignoring invalid version [%s]"), version.c_str());
            continue;
        }

        if (!matches_policy(ver))
        {
            trace::verbose(_X("Ignoring version [%s] because it does not match the roll-forward policy"), version.c_str());
            continue;
        }

        if (!is_better_match(ver, resolved_version))
        {
            trace::verbose(
                _X("Ignoring version [%s] because it is not a better match than [%s]"),
                version.c_str(),
                resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
            );
            continue;
        }

        trace::verbose(
            _X("Version [%s] is a better match than [%s]"),
            version.c_str(),
            resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
        );

        changed = true;
        resolved_version = ver;
        resolved_version_str = move(version);
    }

    if (changed)
    {
        sdk_path = dir;
        append_path(&sdk_path, resolved_version_str.c_str());
    }

    // Not yet fully resolved
    return false;
}
