// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "sdk_resolver.h"

#include "cpprest/json.h"
#include "fx_ver.h"
#include "trace.h"
#include "utils.h"
#include "sdk_info.h"

typedef web::json::value json_value;
typedef web::json::object json_object;

pal::string_t resolve_cli_version(const pal::string_t& global_json)
{
    trace::verbose(_X("--- Resolving CLI version from global json [%s]"), global_json.c_str());

    pal::string_t retval;
    if (!pal::file_exists(global_json))
    {
        trace::verbose(_X("[%s] does not exist"), global_json.c_str());
        return retval;
    }

    pal::ifstream_t file(global_json);
    if (!file.good())
    {
        trace::verbose(_X("[%s] could not be opened"), global_json.c_str());
        return retval;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), global_json.c_str());
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto sdk_iter = json.find(_X("sdk"));
        if (sdk_iter == json.end() || sdk_iter->second.is_null())
        {
            trace::verbose(_X("CLI '/sdk/version' field not present/null in [%s]"), global_json.c_str());
            return retval;
        }

        const auto& sdk_obj = sdk_iter->second.as_object();
        const auto ver_iter = sdk_obj.find(_X("version"));
        if (ver_iter == sdk_obj.end() || ver_iter->second.is_null())
        {
            trace::verbose(_X("CLI 'sdk/version' field not present/null in [%s]"), global_json.c_str());
            return retval;
        }
        retval = ver_iter->second.as_string();
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void)pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), global_json.c_str(), jes.c_str());
    }
    trace::verbose(_X("CLI version is [%s] in global json file [%s]"), retval.c_str(), global_json.c_str());
    return retval;
}

pal::string_t resolve_sdk_version(pal::string_t sdk_path, bool disallow_prerelease, pal::string_t global_cli_version)
{
    fx_ver_t specified;

    //   Validate the global cli version if specified
    if (!global_cli_version.empty())
    {
        if (!fx_ver_t::parse(global_cli_version, &specified, false))
        {
            trace::error(_X("The specified SDK version '%s' could not be parsed"), global_cli_version.c_str());
            return pal::string_t();
        }

        // Always consider prereleases when the version specified in global.json is itself a prerelease
        if (specified.is_prerelease())
        {
            disallow_prerelease = false;
        }
    }

    trace::verbose(_X("--- Resolving SDK version from SDK dir [%s]"), sdk_path.c_str());

    pal::string_t retval;
    std::vector<pal::string_t> versions;

    pal::readdir_onlydirectories(sdk_path, &versions);
    fx_ver_t max_ver;
    for (const auto& version : versions)
    {
        trace::verbose(_X("Considering version... [%s]"), version.c_str());

        fx_ver_t ver;
        if (fx_ver_t::parse(version, &ver, disallow_prerelease))
        {
            if (global_cli_version.empty() ||
                // If a global cli version is specified:
                //   pick the greatest version that differs only in the 'minor-patch'
                //   and is semantically greater than or equal to the global cli version specified.
                (ver.get_major() == specified.get_major() && ver.get_minor() == specified.get_minor() &&
                (ver.get_patch() / 100) == (specified.get_patch() / 100) && ver >= specified))
            {
                max_ver = std::max(ver, max_ver);
            }
        }
    }

    if (!max_ver.is_empty())
    {
        pal::string_t max_ver_str = max_ver.as_str();
        append_path(&sdk_path, max_ver_str.c_str());

        trace::verbose(_X("Checking if resolved SDK dir [%s] exists"), sdk_path.c_str());
        if (pal::directory_exists(sdk_path))
        {
            trace::verbose(_X("Resolved SDK dir is [%s]"), sdk_path.c_str());
            retval = max_ver_str;
        }
    }

    return retval;
}

bool sdk_resolver_t::resolve_sdk_dotnet_path(const pal::string_t& dotnet_root, pal::string_t* cli_sdk)
{
    trace::verbose(_X("--- Resolving dotnet from working dir"));
    pal::string_t cwd;
    if (!pal::getcwd(&cwd))
    {
        trace::verbose(_X("Failed to obtain current working dir"));
        assert(cwd.empty());
    }

    return resolve_sdk_dotnet_path(dotnet_root, cwd, cli_sdk);
}

bool higher_sdk_version(const pal::string_t& new_version, pal::string_t* version)
{
    bool disallow_prerelease = false;
    bool retval = false;
    fx_ver_t ver;
    fx_ver_t new_ver;

    if (fx_ver_t::parse(new_version, &new_ver, disallow_prerelease))
    {
        if (!fx_ver_t::parse(*version, &ver, disallow_prerelease) || (new_ver > ver))
        {
            version->assign(new_version);
            retval = true;
        }
    }

    return retval;
}

bool sdk_resolver_t::resolve_sdk_dotnet_path(
    const pal::string_t& dotnet_root, 
    const pal::string_t& cwd, 
    pal::string_t* cli_sdk,
    bool disallow_prerelease,
    pal::string_t* global_json_path)
{
    pal::string_t global;

    if (!cwd.empty())
    {
        for (pal::string_t parent_dir, cur_dir = cwd; true; cur_dir = parent_dir)
        {
            pal::string_t file = cur_dir;
            append_path(&file, _X("global.json"));

            trace::verbose(_X("Probing path [%s] for global.json"), file.c_str());
            if (pal::file_exists(file))
            {
                global = file;
                trace::verbose(_X("Found global.json [%s]"), global.c_str());
                break;
            }
            parent_dir = get_directory(cur_dir);
            if (parent_dir.empty() || parent_dir.size() == cur_dir.size())
            {
                trace::verbose(_X("Terminating global.json search at [%s]"), parent_dir.c_str());
                break;
            }
        }
    }

    std::vector<pal::string_t> hive_dir;
    get_framework_and_sdk_locations(dotnet_root, &hive_dir);

    pal::string_t cli_version;
    pal::string_t sdk_path;
    pal::string_t global_cli_version;

    if (!global.empty())
    {
        global_cli_version = resolve_cli_version(global);
    }

    for (pal::string_t dir : hive_dir)
    {
        trace::verbose(_X("Searching SDK directory in [%s]"), dir.c_str());
        pal::string_t current_sdk_path = dir;
        append_path(&current_sdk_path, _X("sdk"));

        if (global_cli_version.empty())
        {
            pal::string_t new_cli_version = resolve_sdk_version(current_sdk_path, disallow_prerelease, global_cli_version);
            if (higher_sdk_version(new_cli_version, &cli_version))
            {
                sdk_path = current_sdk_path;
            }
        }
        else
        {
            if (global_json_path != nullptr)
            {
                global_json_path->assign(global);
            }

            pal::string_t probing_sdk_path = current_sdk_path;
            append_path(&probing_sdk_path, global_cli_version.c_str());

            if (pal::directory_exists(probing_sdk_path))
            {
                trace::verbose(_X("CLI directory [%s] from global.json exists"), probing_sdk_path.c_str());
                cli_version = global_cli_version;
                sdk_path = current_sdk_path;
                //  Use the first matching version
                break;
            }
            else
            {
                pal::string_t new_cli_version = resolve_sdk_version(current_sdk_path, disallow_prerelease, global_cli_version);
                if (higher_sdk_version(new_cli_version, &cli_version))
                {
                    sdk_path = current_sdk_path;
                }
            }
        }
    }

    if (!cli_version.empty())
    {
        append_path(&sdk_path, cli_version.c_str());
        cli_sdk->assign(sdk_path);
        trace::verbose(_X("Found CLI SDK in: %s"), cli_sdk->c_str());
        return true;
    }

    if (!global_cli_version.empty())
    {
        trace::error(_X("A compatible installed dotnet SDK for global.json version: [%s] from [%s] was not found"), global_cli_version.c_str(), global.c_str());
        trace::error(_X("Please install the [%s] SDK or update [%s] with an installed dotnet SDK:"), global_cli_version.c_str(), global.c_str());
    }
    if (global_cli_version.empty() || !sdk_info::print_all_sdks(dotnet_root, _X("  ")))
    {
        trace::error(_X("  It was not possible to find any installed dotnet SDKs"));
        trace::error(_X("  Did you mean to run dotnet SDK commands? Please install dotnet SDK from:"));
        trace::error(_X("      %s"), DOTNET_CORE_DOWNLOAD_URL);
    }
    return false;
}
