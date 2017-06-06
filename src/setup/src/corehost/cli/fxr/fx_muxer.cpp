// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "utils.h"
#include "libhost.h"
#include "args.h"
#include "fx_ver.h"
#include "fx_muxer.h"
#include "trace.h"
#include "runtime_config.h"
#include "cpprest/json.h"
#include "error_codes.h"
#include "deps_format.h"


/**
 * When the framework is not found, display detailed error message
 *   about available frameworks and installation of new framework.
 */
void handle_missing_framework_error(const pal::string_t& fx_name, const pal::string_t& fx_version, const pal::string_t& fx_dir)
{
    pal::string_t fx_ver_dirs = get_directory(fx_dir);

    // Display the error message about missing FX.
    trace::error(_X("The specified framework '%s', version '%s' was not found."), fx_name.c_str(), fx_version.c_str());
    trace::error(_X("  - Check application dependencies and target a framework version installed at:"));
    trace::error(_X("      %s"), fx_ver_dirs.c_str());

    // Gather the list of versions installed at the shared FX location.
    bool is_print_header = true;
    std::vector<pal::string_t> versions;
    pal::readdir(fx_ver_dirs, &versions);
    for (const auto& ver : versions)
    {
        // Make sure we filter out any non-version folders at shared FX location.
        fx_ver_t parsed(-1, -1, -1);
        if (fx_ver_t::parse(ver, &parsed, false))
        {
            // Print banner only once before printing the versions
            if (is_print_header)
            {
                trace::error(_X("  - The following versions are installed:"));
                is_print_header = false;
            }
            trace::error(_X("      %s"), ver.c_str());
        }
    }
    trace::error(_X("  - Alternatively, install the framework version '%s'."), fx_version.c_str());
}

/**
 * Resolve the hostpolicy version from deps.
 *  - Scan the deps file's libraries section and find the hostpolicy version in the file.
 */
pal::string_t resolve_hostpolicy_version_from_deps(const pal::string_t& deps_json)
{
    trace::verbose(_X("--- Resolving %s version from deps json [%s]"), LIBHOSTPOLICY_NAME, deps_json.c_str());

    pal::string_t retval;
    if (!pal::file_exists(deps_json))
    {
        trace::verbose(_X("Dependency manifest [%s] does not exist"), deps_json.c_str());
        return retval;
    }

    pal::ifstream_t file(deps_json);
    if (!file.good())
    {
        trace::verbose(_X("Dependency manifest [%s] could not be opened"), deps_json.c_str());
        return retval;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), deps_json.c_str());
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto& libraries = json.at(_X("libraries")).as_object();

        // Look up the root package instead of the "runtime" package because we can't do a full rid resolution.
        // i.e., look for "Microsoft.NETCore.DotNetHostPolicy/" followed by version.
        pal::string_t prefix = _X("Microsoft.NETCore.DotNetHostPolicy/");
        for (const auto& library : libraries)
        {
            if (starts_with(library.first, prefix, false))
            {
                // Extract the version information that occurs after '/'
                retval = library.first.substr(prefix.size());
                break;
            }
        }
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void)pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), deps_json.c_str(), jes.c_str());
    }
    trace::verbose(_X("Resolved version %s from dependency manifest file [%s]"), retval.c_str(), deps_json.c_str());
    return retval;
}

/**
 * Given a directory and a version, find if the package relative
 *     dir under the given directory contains hostpolicy.dll
 */
bool to_hostpolicy_package_dir(const pal::string_t& dir, const pal::string_t& version, pal::string_t* candidate)
{
    assert(!version.empty());

    candidate->clear();

    // Ensure the relative dir contains platform directory separators.
    pal::string_t rel_dir = _STRINGIFY(HOST_POLICY_PKG_REL_DIR);
    if (DIR_SEPARATOR != '/')
    {
        replace_char(&rel_dir, '/', DIR_SEPARATOR);
    }

    // Construct the path to directory containing hostpolicy.
    pal::string_t path = dir;
    append_path(&path, _STRINGIFY(HOST_POLICY_PKG_NAME)); // package name
    append_path(&path, version.c_str());                  // package version
    append_path(&path, rel_dir.c_str());                  // relative dir containing hostpolicy library

    // Check if "path" contains the required library.
    if (!library_exists_in_dir(path, LIBHOSTPOLICY_NAME, nullptr))
    {
        trace::verbose(_X("Did not find %s in directory %s"), LIBHOSTPOLICY_NAME, path.c_str());
        return false;
    }

    // "path" contains the directory containing hostpolicy library.
    *candidate = path;

    trace::verbose(_X("Found %s in directory %s"), LIBHOSTPOLICY_NAME, path.c_str());
    return true;
}

/**
 * Given a nuget version, detect if a serviced hostpolicy is available at
 *   platform servicing location.
 */
bool hostpolicy_exists_in_svc(const pal::string_t& version, pal::string_t* resolved_dir)
{
    if (version.empty())
    {
        return false;
    }

    pal::string_t svc_dir;
    pal::get_default_servicing_directory(&svc_dir);
    append_path(&svc_dir, _X("pkgs"));
    return to_hostpolicy_package_dir(svc_dir, version, resolved_dir);
}

/**
 * Given path to app binary, say app.dll or app.exe, retrieve the app.deps.json.
 */
pal::string_t get_deps_from_app_binary(const pal::string_t& app)
{
    assert(app.find(DIR_SEPARATOR) != pal::string_t::npos);
    assert(ends_with(app, _X(".dll"), false) || ends_with(app, _X(".exe"), false));

    // First append directory.
    pal::string_t deps_file;
    deps_file.assign(get_directory(app));


    // Then the app name and the file extension
    pal::string_t app_name = get_filename(app);
    deps_file.append(app_name, 0, app_name.find_last_of(_X(".")));
    deps_file.append(_X(".deps.json"));
    return deps_file;
}

/**
 * Given a version and probing paths, find if package layout
 *    directory containing hostpolicy exists.
 */
bool resolve_hostpolicy_dir_from_probe_paths(const pal::string_t& version, const std::vector<pal::string_t>& probe_realpaths, pal::string_t* candidate)
{
    if (probe_realpaths.empty() || version.empty())
    {
        return false;
    }

    // Check if the package relative directory containing hostpolicy exists.
    for (const auto& probe_path : probe_realpaths)
    {
        trace::verbose(_X("Considering %s to probe for %s"), probe_path.c_str(), LIBHOSTPOLICY_NAME);
        if (to_hostpolicy_package_dir(probe_path, version, candidate))
        {
            return true;
        }
    }

    // Print detailed message about the file not found in the probe paths.
    trace::error(_X("Could not find required library %s in %d probing paths:"),
        LIBHOSTPOLICY_NAME, probe_realpaths.size());
    for (const auto& path : probe_realpaths)
    {
        trace::error(_X("  %s"), path.c_str());
    }
    return false;
}

/**
 * Given FX location, app binary and specified --depsfile, return deps that contains hostpolicy.dll
 */
pal::string_t get_deps_file(
    const pal::string_t& fx_dir,
    const pal::string_t& app_candidate,
    const pal::string_t& specified_deps_file,
    const runtime_config_t& config)
{
    if (config.get_portable())
    {
        pal::string_t deps_file = fx_dir;
        
        if (!deps_file.empty() && deps_file.back() != DIR_SEPARATOR)
        {
            deps_file.push_back(DIR_SEPARATOR);
        }
        // Portable app's hostpolicy is resolved from FX deps
        return deps_file + config.get_fx_name() + _X(".deps.json");
    }
    else
    {
        // Standalone app's hostpolicy is from specified deps or from app deps.
        return !specified_deps_file.empty() ? specified_deps_file : get_deps_from_app_binary(app_candidate);
    }
}

/**
* Multilevel Lookup is enabled by default
*  It can be disabled by setting DOTNET_MULTILEVEL_LOOKUP env var to a value that is not 1
*/
bool multilevel_lookup_enabled()
{
    pal::string_t env_lookup;
    bool multilevel_lookup = true;

    if (pal::getenv(_X("DOTNET_MULTILEVEL_LOOKUP"), &env_lookup))
    {
        auto env_val = pal::xtoi(env_lookup.c_str());
        multilevel_lookup = (env_val == 1);
    }
    return multilevel_lookup;
}

/**
 * Given own location, FX location, app binary and specified --depsfile and probe paths
 *     return location that is expected to contain hostpolicy
 */
bool fx_muxer_t::resolve_hostpolicy_dir(host_mode_t mode,
    const pal::string_t& own_dir,
    const pal::string_t& fx_dir,
    const pal::string_t& app_candidate,
    const pal::string_t& specified_deps_file,
    const pal::string_t& specified_fx_version,
    const std::vector<pal::string_t>& probe_realpaths,
    const runtime_config_t& config,
    pal::string_t* impl_dir)
{
    // Obtain deps file for the given configuration.
    pal::string_t resolved_deps = get_deps_file(fx_dir, app_candidate, specified_deps_file, config);

    // Resolve hostpolicy version out of the deps file.
    pal::string_t version = resolve_hostpolicy_version_from_deps(resolved_deps);
    if (trace::is_enabled() && version.empty() && pal::file_exists(resolved_deps))
    {
        trace::warning(_X("Dependency manifest %s does not contain an entry for %s"), resolved_deps.c_str(), _STRINGIFY(HOST_POLICY_PKG_NAME));
    }

    // Check if the given version of the hostpolicy exists in servicing.
    if (hostpolicy_exists_in_svc(version, impl_dir))
    {
        return true;
    }

    // Get the expected directory that would contain hostpolicy.
    pal::string_t expected;
    if (config.get_portable())
    {
        if (!pal::directory_exists(fx_dir))
        {
            pal::string_t fx_version = specified_fx_version.empty() ? config.get_fx_version() : specified_fx_version;
            handle_missing_framework_error(config.get_fx_name(), fx_version, fx_dir);
            return false;
        }
        
        expected = fx_dir;
    }
    else
    {
        // Standalone apps can be activated by muxer or by standalone host or "corehost"
        // 1. When activated with dotnet.exe or corehost.exe, check for hostpolicy in the deps dir or
        //    app dir.
        // 2. When activated with app.exe, the standalone host, check own directory.
        assert(mode == host_mode_t::muxer || mode == host_mode_t::standalone || mode == host_mode_t::split_fx);
        expected = (mode == host_mode_t::standalone)
            ? own_dir
            : get_directory(specified_deps_file.empty() ? app_candidate : specified_deps_file);
    }

    // Check if hostpolicy exists in "expected" directory.
    trace::verbose(_X("The expected %s directory is [%s]"), LIBHOSTPOLICY_NAME, expected.c_str());
    if (library_exists_in_dir(expected, LIBHOSTPOLICY_NAME, nullptr))
    {
        impl_dir->assign(expected);
        return true;
    }

    trace::verbose(_X("The %s was not found in [%s]"), LIBHOSTPOLICY_NAME, expected.c_str());

    // Start probing for hostpolicy in the specified probe paths.
    pal::string_t candidate;
    if (resolve_hostpolicy_dir_from_probe_paths(version, probe_realpaths, &candidate))
    {
        impl_dir->assign(candidate);
        return true;
    }

    // If it still couldn't be found, somebody upstack messed up. Flag an error for the "expected" location.
    trace::error(_X("A fatal error was encountered. The library '%s' required to execute the application was not found in '%s'."), LIBHOSTPOLICY_NAME, expected.c_str());
    return false;
}

fx_ver_t fx_muxer_t::resolve_framework_version(const std::vector<fx_ver_t>& version_list,
    const pal::string_t& fx_ver,
    const fx_ver_t& specified,
    const bool& patch_roll_fwd,
    const int& roll_fwd_on_no_candidate_fx)
{
    trace::verbose(_X("Attempting FX roll forward starting from [%s]"), fx_ver.c_str());


    fx_ver_t most_compatible = specified;
    if (!specified.is_prerelease())
    {
        if (roll_fwd_on_no_candidate_fx > 0)
        {
            trace::verbose(_X("'Roll forward on no candidate fx' enabled. Looking for the least production greater than or equal to [%s]"), fx_ver.c_str());
            fx_ver_t next_lowest(-1, -1, -1);
            for (const auto& ver : version_list)
            {
                
                if (!ver.is_prerelease() && ver >= specified)
                {
                    next_lowest = (next_lowest == fx_ver_t(-1, -1, -1)) ? ver : std::min(next_lowest, ver);
                }
            }
            if (next_lowest == fx_ver_t(-1, -1, -1))
            {
                trace::verbose(_X("No production greater than or equal to [%s] found."), fx_ver.c_str());
            }
            else
            {
                trace::verbose(_X("Found version [%s]"), next_lowest.as_str().c_str());
                most_compatible = next_lowest;
            }
        }
        if (patch_roll_fwd)
        {
            trace::verbose(_X("Applying patch roll forward from [%s]"), most_compatible.as_str().c_str());
            for (const auto& ver : version_list)
            {
                trace::verbose(_X("Inspecting version... [%s]"), ver.as_str().c_str());

                if (!ver.is_prerelease() && //  only prod. prevents roll forward to prerelease.
                    ver.get_major() == most_compatible.get_major() &&
                    ver.get_minor() == most_compatible.get_minor())
                {
                    // Pick the greatest production that differs only in patch.
                    most_compatible = std::max(ver, most_compatible);
                }
            }
        }
    }
    else
    {
        for (const auto& ver : version_list)
        {
            trace::verbose(_X("Inspecting version... [%s]"), ver.as_str().c_str());

            //both production and prerelease.
            if (ver.is_prerelease() && // prevent roll forward to production.
                ver.get_major() == specified.get_major() &&
                ver.get_minor() == specified.get_minor() &&
                ver.get_patch() == specified.get_patch() &&
                ver > specified)
            {
                // Pick the smallest prerelease that is greater than specified.
                most_compatible = (most_compatible == specified) ? ver : std::min(ver, most_compatible);
            }
        }
    }

    return most_compatible;
}

pal::string_t fx_muxer_t::resolve_fx_dir(host_mode_t mode,
    const pal::string_t& own_dir,
    const runtime_config_t& config,
    const pal::string_t& specified_fx_version,
    const int& roll_fwd_on_no_candidate_fx_val)
{
    // No FX resolution for standalone apps.
    assert(mode != host_mode_t::standalone);

    // If invoking using FX dotnet.exe, use own directory.
    if (mode == host_mode_t::split_fx)
    {
        return own_dir;
    }
    assert(mode == host_mode_t::muxer);

    trace::verbose(_X("--- Resolving FX directory, specified '%s'"), specified_fx_version.c_str());
    const auto fx_name = config.get_fx_name();
    const auto fx_ver = specified_fx_version.empty() ? config.get_fx_version() : specified_fx_version;

    fx_ver_t specified(-1, -1, -1);
    if (!fx_ver_t::parse(fx_ver, &specified, false))
    {
        trace::error(_X("The specified framework version '%s' could not be parsed"), fx_ver.c_str());
        return pal::string_t();
    }

    // Multi-level SharedFX lookup will look for the most appropriate version in several locations
    // by following the priority rank below:
    // .exe directory
    //  Global .NET directory
    // If it is not activated, then only .exe directory will be considered

    std::vector<pal::string_t> hive_dir;
    pal::string_t local_dir;
    std::vector<pal::string_t> global_dirs;
    bool multilevel_lookup = multilevel_lookup_enabled();

    hive_dir.push_back(own_dir);
    if (multilevel_lookup && pal::get_global_dotnet_dirs(&global_dirs))
    {
        for (pal::string_t dir : global_dirs)
        {
            hive_dir.push_back(dir);
        }
    }

    bool patch_roll_fwd_enabled = config.get_patch_roll_fwd();
    pal::string_t selected_fx_dir;
    fx_ver_t selected_ver(-1, -1, -1);

    for (pal::string_t dir : hive_dir)
    {
        auto fx_dir = dir;
        trace::verbose(_X("Searching FX directory in [%s]"), fx_dir.c_str());

        append_path(&fx_dir, _X("shared"));
        append_path(&fx_dir, fx_name.c_str());

        bool do_roll_forward = false;
        if (specified_fx_version.empty())
        {
            if (!specified.is_prerelease())
            {
                // If production and no roll forward use given version.
                do_roll_forward = (patch_roll_fwd_enabled) || (roll_fwd_on_no_candidate_fx_val > 0);
            }
            else
            {
                // Prerelease, but roll forward only if version doesn't exist.
                pal::string_t ver_dir = fx_dir;
                append_path(&ver_dir, fx_ver.c_str());
                do_roll_forward = !pal::directory_exists(ver_dir);
            }
        }

        if (!do_roll_forward)
        {
            trace::verbose(_X("Did not roll forward because specified version='%s', patch_roll_fwd=%d, roll_fwd_on_no_candidate_fx=%d, chose [%s]"),
                specified_fx_version.c_str(), patch_roll_fwd_enabled, roll_fwd_on_no_candidate_fx_val, fx_ver.c_str());
            
            append_path(&fx_dir, fx_ver.c_str());
            if (pal::directory_exists(fx_dir))
            {
                trace::verbose(_X("Chose FX version [%s]"), fx_dir.c_str());
                return fx_dir;
            }
        }
        else
        {
            std::vector<pal::string_t> list;
            std::vector<fx_ver_t> version_list;
            pal::readdir(fx_dir, &list);

            for (const auto& version : list)
            {
                fx_ver_t ver(-1, -1, -1);
                if (fx_ver_t::parse(version, &ver, false))
                {
                    version_list.push_back(ver);
                }
            }

            fx_ver_t resolved_ver = resolve_framework_version(version_list, fx_ver, specified, patch_roll_fwd_enabled, roll_fwd_on_no_candidate_fx_val);

            pal::string_t resolved_ver_str = resolved_ver.as_str();
            append_path(&fx_dir, resolved_ver_str.c_str());

            if (pal::directory_exists(fx_dir))
            {
                //Continue to search for a better match if available
                std::vector<fx_ver_t> version_list;
                version_list.push_back(resolved_ver);
                version_list.push_back(selected_ver);
                resolved_ver = resolve_framework_version(version_list, fx_ver, specified, patch_roll_fwd_enabled, roll_fwd_on_no_candidate_fx_val);

                if (resolved_ver != selected_ver)
                {
                    trace::verbose(_X("Changing Selected FX version from [%s] to [%s]"), selected_fx_dir.c_str(), fx_dir.c_str());
                    selected_ver = resolved_ver;
                    selected_fx_dir = fx_dir;
                }
            }
        }
    }

    if (selected_fx_dir.empty())
    {
        trace::error(_X("It was not possible to find any compatible framework version"));
        return pal::string_t();
    }

    trace::verbose(_X("Chose FX version [%s]"), selected_fx_dir.c_str());
    return selected_fx_dir;
}

pal::string_t fx_muxer_t::resolve_cli_version(const pal::string_t& global_json)
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
        (void) pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), global_json.c_str(), jes.c_str());
    }
    trace::verbose(_X("CLI version is [%s] in global json file [%s]"), retval.c_str(), global_json.c_str());
    return retval;
}

pal::string_t resolve_sdk_version(pal::string_t sdk_path, bool parse_only_production)
{
    trace::verbose(_X("--- Resolving SDK version from SDK dir [%s]"), sdk_path.c_str());

    pal::string_t retval;
    std::vector<pal::string_t> versions;

    pal::readdir(sdk_path, &versions);
    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& version : versions)
    {
        trace::verbose(_X("Considering version... [%s]"), version.c_str());

        fx_ver_t ver(-1, -1, -1);
        if (fx_ver_t::parse(version, &ver, parse_only_production))
        {
            max_ver = std::max(ver, max_ver);
        }
    }

    pal::string_t max_ver_str = max_ver.as_str();
    append_path(&sdk_path, max_ver_str.c_str());

    trace::verbose(_X("Checking if resolved SDK dir [%s] exists"), sdk_path.c_str());
    if (pal::directory_exists(sdk_path))
    {
        trace::verbose(_X("Resolved SDK dir is [%s]"), sdk_path.c_str());
        retval = max_ver_str;
    }

    return retval;
}

pal::string_t resolve_patch_sdk_version(pal::string_t global_cli_version, pal::string_t sdk_path, bool parse_only_production)
{
    fx_ver_t specified(-1, -1, -1);
    if (!fx_ver_t::parse(global_cli_version, &specified, false))
    {
        trace::error(_X("The specified SDK version '%s' could not be parsed"), global_cli_version.c_str());
        return pal::string_t();
    }

    trace::verbose(_X("--- Resolving SDK version from SDK dir [%s]"), sdk_path.c_str());

    pal::string_t retval;
    std::vector<pal::string_t> versions;

    pal::readdir(sdk_path, &versions);
    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& version : versions)
    {
        trace::verbose(_X("Considering version... [%s]"), version.c_str());

        fx_ver_t ver(-1, -1, -1);
        if (fx_ver_t::parse(version, &ver, parse_only_production))
        {
            if (ver.get_major() == specified.get_major() && ver.get_minor() == specified.get_minor())
            {
                // Pick the greatest version that differs only in patch.
                max_ver = std::max(ver, max_ver);
            }
        }
    }

    pal::string_t max_ver_str = max_ver.as_str();
    append_path(&sdk_path, max_ver_str.c_str());

    trace::verbose(_X("Checking if resolved SDK dir [%s] exists"), sdk_path.c_str());
    if (pal::directory_exists(sdk_path))
    {
        trace::verbose(_X("Resolved SDK dir is [%s]"), sdk_path.c_str());
        retval = max_ver_str;
    }

    return retval;
}

bool fx_muxer_t::resolve_sdk_dotnet_path(const pal::string_t& own_dir, pal::string_t* cli_sdk)
{
    trace::verbose(_X("--- Resolving dotnet from working dir"));
    pal::string_t cwd;
    if (!pal::getcwd(&cwd))
    {
        trace::verbose(_X("Failed to obtain current working dir"));
        assert(cwd.empty());
    }

    return resolve_sdk_dotnet_path(own_dir, cwd, cli_sdk);
}

bool higher_sdk_version(const pal::string_t& new_version, pal::string_t* version, bool parse_only_production)
{
    bool retval = false;
    fx_ver_t ver(-1, -1, -1);
    fx_ver_t new_ver(-1, -1, -1);

    if (fx_ver_t::parse(new_version, &new_ver, parse_only_production))
    {
       if (!fx_ver_t::parse(*version, &ver, parse_only_production) || (new_ver > ver))
       {
           version->assign(new_version);
           retval = true;
       }
    }

    return retval;
}

bool fx_muxer_t::resolve_sdk_dotnet_path(const pal::string_t& own_dir, const pal::string_t& cwd, pal::string_t* cli_sdk)
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
    pal::string_t local_dir;
    std::vector<pal::string_t> global_dirs;
    bool multilevel_lookup = multilevel_lookup_enabled();

    if (!own_dir.empty())
    {
        hive_dir.push_back(own_dir);
    }

    if (multilevel_lookup && pal::get_global_dotnet_dirs(&global_dirs))
    {
        for (pal::string_t dir : global_dirs)
        {
            hive_dir.push_back(dir);
        }
    }

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
        bool parse_only_production = false;  // false -- implies both production and prerelease.

        if (global_cli_version.empty())
        {
            pal::string_t new_cli_version = resolve_sdk_version(current_sdk_path, parse_only_production);
            if (higher_sdk_version(new_cli_version, &cli_version, parse_only_production))
            {
                sdk_path = current_sdk_path;
            }
        }
        else
        {
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
                pal::string_t new_cli_version = resolve_patch_sdk_version(global_cli_version, current_sdk_path, parse_only_production);
                if (higher_sdk_version(new_cli_version, &cli_version, parse_only_production))
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

    if (global_cli_version.empty())
    {
        trace::verbose(_X("It was not possible to find any SDK version"));
    }
    else
    {
        trace::error(_X("The specified SDK version [%s] from global.json [%s] not found; install specified SDK version"), global_cli_version.c_str(), global.c_str());
    }
    return false;
}

bool is_sdk_dir_present(const pal::string_t& own_dir)
{
    pal::string_t sdk_path = own_dir;
    append_path(&sdk_path, _X("sdk"));
    return pal::directory_exists(sdk_path);
}

int muxer_info()
{
    trace::println();
    trace::println(_X("Microsoft .NET Core Shared Framework Host"));
    trace::println();
    trace::println(_X("  Version  : %s"), _STRINGIFY(HOST_FXR_PKG_VER));
    trace::println(_X("  Build    : %s"), _STRINGIFY(REPO_COMMIT_HASH));
    trace::println();

    return StatusCode::Success;
}

int muxer_usage(bool is_sdk_present)
{

    std::vector<host_option> known_opts = fx_muxer_t::get_known_opts(true, host_mode_t::invalid, true);

    if (!is_sdk_present)
    {
        trace::println();
        trace::println(_X("Usage: dotnet [host-options] [path-to-application]"));
        trace::println();
        trace::println(_X("path-to-application:"));
        trace::println(_X("  The path to an application .dll file to execute."));
    }
    trace::println();
    trace::println(_X("host-options:"));

    for (const auto& arg : known_opts)
    {
        trace::println(_X("  %-34s  %s"), (arg.option + _X(" ") + arg.argument).c_str(), arg.description.c_str());
    }

    trace::println();

    trace::println();
    if (!is_sdk_present)
    {
        trace::println(_X("Common Options:"));
        trace::println(_X("  -h|--help                           Displays this help."));
        trace::println(_X("  --info                              Displays the host information"));
        trace::println();
    }

    
    return StatusCode::InvalidArgFailure;
}

// Convert "path" to realpath (merging working dir if needed) and append to "realpaths" out param.
void append_probe_realpath(const pal::string_t& path, std::vector<pal::string_t>* realpaths, const pal::string_t& tfm)
{
    pal::string_t probe_path = path;

    if (pal::realpath(&probe_path) && pal::directory_exists(probe_path))
    {
        realpaths->push_back(probe_path);
    }
    else
    {
        //Check if we can extrapolate |arch|<DIR_SEPARATOR>|tfm| for probing stores
        pal::string_t placeholder = _X("|arch|");
        placeholder.push_back(DIR_SEPARATOR);
        placeholder.append(_X("|tfm|"));
        auto pos_placeholder = probe_path.find_last_of(placeholder);

        if (pos_placeholder != pal::string_t::npos)
        {
            pal::string_t segment = get_arch();
            segment.push_back(DIR_SEPARATOR);
            segment.append(tfm);
            probe_path.replace(pos_placeholder - placeholder.length() + 1, placeholder.length(), segment);

            if (pal::directory_exists(probe_path))
            {
                realpaths->push_back(probe_path);
            }
            else
            {
                trace::verbose(_X("Ignoring host interpreted additional probing path %s as it does not exist."),probe_path.c_str());
            }
        }
        else
        {
            trace::verbose(_X("Ignoring additional probing path %s as it does not exist."),probe_path.c_str());
        }
    }
}

std::vector<host_option> fx_muxer_t::get_known_opts(bool exec_mode, host_mode_t mode, bool get_all_options)
{
    std::vector<host_option> known_opts = { { _X("--additionalprobingpath"), _X("<path>"), _X("Path containing probing policy and assemblies to probe for") } };
    if (get_all_options || exec_mode || mode == host_mode_t::split_fx || mode == host_mode_t::standalone)
    {
        known_opts.push_back({ _X("--depsfile"), _X("<path>"),  _X("Path to <application>.deps.json file") });
        known_opts.push_back({ _X("--runtimeconfig"), _X("<path>"), _X("Path to <application>.runtimeconfig.json file") });
    }

    if (get_all_options || mode == host_mode_t::muxer)
    {
        known_opts.push_back({ _X("--fx-version"), _X("<version>"), _X("Version of the installed Shared Framework to use to run the application.") });
        known_opts.push_back({ _X("--roll-forward-on-no-candidate-fx"), _X(""), _X("Roll forward on no candidate shared framework is enabled") });
        known_opts.push_back({ _X("--additional-deps"), _X("<path>"), _X("Path to additonal deps.json file") });
    }

    return known_opts;
}

int fx_muxer_t::parse_args_and_execute(
    const pal::string_t& own_dir,
    const pal::string_t& own_dll,
    int argoff, int argc, const pal::char_t* argv[], bool exec_mode, host_mode_t mode, bool* is_an_app)
{
    *is_an_app = true;

    std::vector<host_option> known_opts = get_known_opts(exec_mode, mode);

    // Parse the known arguments if any.
    int num_parsed = 0;
    std::unordered_map<pal::string_t, std::vector<pal::string_t>> opts;
    if (!parse_known_args(argc - argoff, &argv[argoff], known_opts, &opts, &num_parsed))
    {
        trace::error(_X("Failed to parse supported options or their values:"));
        for (const auto& arg : known_opts)
        {
            trace::error(_X("  %s"), arg.option.c_str());
        }
        return InvalidArgFailure;
    }

    const pal::char_t** new_argv = argv;
    int new_argc = argc;
    std::vector<const pal::char_t*> vec_argv;
    pal::string_t app_candidate = own_dll;
    int cur_i = argoff + num_parsed;
    if (mode != host_mode_t::standalone)
    {
        trace::verbose(_X("Detected a non-standalone application, expecting app.dll to execute."));
        if (cur_i >= argc)
        {
            return muxer_usage(!is_sdk_dir_present(own_dir));
        }

        app_candidate = argv[cur_i];
        bool is_app_managed = (ends_with(app_candidate, _X(".dll"), false) || ends_with(app_candidate, _X(".exe"), false)) && pal::realpath(&app_candidate);

        if (!is_app_managed)
        {
            trace::verbose(_X("Application '%s' is not a managed executable."), app_candidate.c_str());

            *is_an_app = false;

            if (exec_mode)
            {
                trace::error(_X("dotnet exec needs a managed .dll or .exe extension. The application specified was '%s'"), app_candidate.c_str());
                return InvalidArgFailure;
            }

            // Route to CLI.
            return AppArgNotRunnable;
        }
    }

    // App is managed executable.
    trace::verbose(_X("Treating application '%s' as a managed executable."), app_candidate.c_str());

    if (!pal::file_exists(app_candidate))
    {
        trace::error(_X("The application to execute does not exist: '%s'"), app_candidate.c_str());
        return InvalidArgFailure;
    }

    if (cur_i != 1)
    {
        vec_argv.reserve(argc - cur_i + 1); // +1 for dotnet
        vec_argv.push_back(argv[0]);
        vec_argv.insert(vec_argv.end(), argv + cur_i, argv + argc);
        new_argv = vec_argv.data();
        new_argc = vec_argv.size();
    }

    // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
    return read_config_and_execute(own_dir, app_candidate, opts, new_argc, new_argv, mode);
}

int fx_muxer_t::read_config_and_execute(
    const pal::string_t& own_dir, 
    const pal::string_t& app_candidate,
    const std::unordered_map<pal::string_t, std::vector<pal::string_t>>& opts,
    int new_argc, const pal::char_t** new_argv, host_mode_t mode)
{
    pal::string_t opts_fx_version = _X("--fx-version");
    pal::string_t opts_roll_fwd_on_no_candidate_fx = _X("--roll-forward-on-no-candidate-fx");
    pal::string_t opts_deps_file = _X("--depsfile");
    pal::string_t opts_probe_path = _X("--additionalprobingpath");
    pal::string_t opts_additional_deps = _X("--additional-deps");
    pal::string_t opts_runtime_config = _X("--runtimeconfig");

    pal::string_t fx_version = get_last_known_arg(opts, opts_fx_version, _X(""));
    pal::string_t roll_fwd_on_no_candidate_fx = get_last_known_arg(opts, opts_roll_fwd_on_no_candidate_fx, _X(""));
    pal::string_t deps_file = get_last_known_arg(opts, opts_deps_file, _X(""));
    pal::string_t additional_deps = get_last_known_arg(opts, opts_additional_deps, _X(""));
    pal::string_t runtime_config = get_last_known_arg(opts, opts_runtime_config, _X(""));
    std::vector<pal::string_t> spec_probe_paths = opts.count(opts_probe_path) ? opts.find(opts_probe_path)->second : std::vector<pal::string_t>();

    if (!deps_file.empty() && (!pal::realpath(&deps_file) || !pal::file_exists(deps_file)))
    {
        trace::error(_X("The specified deps.json [%s] does not exist"), deps_file.c_str());
        return StatusCode::InvalidArgFailure;
    }
    if (!runtime_config.empty() && (!pal::realpath(&runtime_config) || !pal::file_exists(runtime_config)))
    {
        trace::error(_X("The specified runtimeconfig.json [%s] does not exist"), runtime_config.c_str());
        return StatusCode::InvalidConfigFile;
    }

    pal::string_t config_file, dev_config_file;

    if (runtime_config.empty())
    {
        trace::verbose(_X("App runtimeconfig.json from [%s]"), app_candidate.c_str());
        get_runtime_config_paths_from_app(app_candidate, &config_file, &dev_config_file);
    }
    else
    {
        trace::verbose(_X("Specified runtimeconfig.json from [%s]"), runtime_config.c_str());
        get_runtime_config_paths_from_arg(runtime_config, &config_file, &dev_config_file);
    }

    runtime_config_t config(config_file, dev_config_file);
    if (!config.is_valid())
    {
        trace::error(_X("Invalid runtimeconfig.json [%s] [%s]"), config.get_path().c_str(), config.get_dev_path().c_str());
        return StatusCode::InvalidConfigFile;
    }

    // Append specified probe paths first and then config file probe paths into realpaths.
    std::vector<pal::string_t> probe_realpaths;
    for (const auto& path : spec_probe_paths)
    {
        append_probe_realpath(path, &probe_realpaths, config.get_tfm());
    }
    for (const auto& path : config.get_probe_paths())
    {
        append_probe_realpath(path, &probe_realpaths, config.get_tfm());
    }

    // 'Roll forward on no candidate fx' is disabled by default. It can be enabled through:
    // 1. Command line argument (--roll-forward-on-no-candidate-fx)
    // 2. Runtimeconfig json file ('rollForwardOnNoCandidateFx' property)
    // 3. DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var
    // The conflicts will be resolved by following the priority rank described above (from 1 to 3).
    // The env var condition is verified in the config file processing
    int roll_fwd_on_no_candidate_fx_val = config.get_roll_fwd_on_no_candidate_fx();
    if (!roll_fwd_on_no_candidate_fx.empty())
    {
        roll_fwd_on_no_candidate_fx_val = pal::xtoi(roll_fwd_on_no_candidate_fx.c_str());
    }

    pal::string_t additional_deps_serialized;
    
    bool is_portable = config.get_portable();
    if (is_portable) {
        additional_deps_serialized = additional_deps;
        if (additional_deps_serialized.empty())
        {
            // additional_deps_serialized stays empty if DOTNET_ADDITIONAL_DEPS env var is not defined
            pal::getenv(_X("DOTNET_ADDITIONAL_DEPS"), &additional_deps_serialized);
        }
    }

    pal::string_t fx_dir = is_portable ? resolve_fx_dir(mode, own_dir, config, fx_version, roll_fwd_on_no_candidate_fx_val) : _X("");

    trace::verbose(_X("Executing as a %s app as per config file [%s]"),
        (is_portable ? _X("portable") : _X("standalone")), config_file.c_str());

    pal::string_t impl_dir;
    if (!resolve_hostpolicy_dir(mode, own_dir, fx_dir, app_candidate, deps_file, fx_version, probe_realpaths, config, &impl_dir))
    {
        return CoreHostLibMissingFailure;
    }

    corehost_init_t init(deps_file, additional_deps_serialized, probe_realpaths, fx_dir, mode, config);
    return execute_app(impl_dir, &init, new_argc, new_argv);
}

/**
 *  Main entrypoint to detect operating mode and perform corehost, muxer,
 *  standalone application activation and the SDK activation.
 */
int fx_muxer_t::execute(const int argc, const pal::char_t* argv[])
{
    pal::string_t own_path;

    // Get the full name of the application
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), own_path.c_str());
        return StatusCode::LibHostCurExeFindFailure;
    }

    pal::string_t own_name = get_filename(own_path);
    pal::string_t own_dir = get_directory(own_path);
    pal::string_t own_dll_filename = get_executable(own_name) + _X(".dll");
    pal::string_t own_dll = own_dir;
    append_path(&own_dll, own_dll_filename.c_str());

    trace::info(_X("Own dll path '%s'"), own_dll.c_str());

    // Flag to indicate activation of an app instead of an invocation of the SDK.
    bool is_an_app = true;

    // Detect invocation mode
    auto mode = detect_operating_mode(own_dir, own_dll, own_name);

    //
    // Invoked as corehost
    //

    if (mode == host_mode_t::split_fx)
    {
        trace::verbose(_X("--- Executing in split/FX mode..."));
        return parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::split_fx, &is_an_app);
    }

    //
    // Invoked from the application base.
    //

    if (mode == host_mode_t::standalone)
    {
        trace::verbose(_X("--- Executing in standalone mode..."));
        return parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::standalone, &is_an_app);
    }

    //
    // Invoked as the dotnet.exe muxer.
    //

    trace::verbose(_X("--- Executing in muxer mode..."));

    if (argc <= 1)
    {
        return muxer_usage(!is_sdk_dir_present(own_dir));
    }

    if (pal::strcasecmp(_X("exec"), argv[1]) == 0)
    {
        return parse_args_and_execute(own_dir, own_dll, 2, argc, argv, true, host_mode_t::muxer, &is_an_app); // arg offset 2 for dotnet, exec
    }

    int result = parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::muxer, &is_an_app); // arg offset 1 for dotnet
   

    if (is_an_app)
    {
        return result;
    }

    //
    // Could not execute as an app, try the CLI SDK dotnet.dll
    //

    pal::string_t sdk_dotnet;
    if (!resolve_sdk_dotnet_path(own_dir, &sdk_dotnet))
    {
        assert(argc > 1);
        if ( pal::strcasecmp(_X("-h"), argv[1]) == 0 ||
             pal::strcasecmp(_X("--help"), argv[1]) == 0 )
        {
            return muxer_usage(false);
        }
        else if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
        {
            return muxer_info();
        }
        trace::error(_X("Did you mean to run dotnet SDK commands? Please install dotnet SDK from: "));
        trace::error(_X("  %s"), DOTNET_CORE_URL);
        return StatusCode::LibHostSdkFindFailure;
    }
    append_path(&sdk_dotnet, _X("dotnet.dll"));

    if (!pal::file_exists(sdk_dotnet))
    {
        trace::error(_X("Found dotnet SDK, but did not find dotnet.dll at [%s]"), sdk_dotnet.c_str());
        return StatusCode::LibHostSdkFindFailure;
    }

    // Transform dotnet [command] [args] -> dotnet dotnet.dll [command] [args]

    std::vector<const pal::char_t*> new_argv;
    new_argv.reserve(argc + 1);
    new_argv.push_back(argv[0]);
    new_argv.push_back(sdk_dotnet.c_str());
    new_argv.insert(new_argv.end(), argv + 1, argv + argc);

    trace::verbose(_X("Using dotnet SDK dll=[%s]"), sdk_dotnet.c_str());
    result = parse_args_and_execute(own_dir, own_dll, 1, new_argv.size(), new_argv.data(), false, host_mode_t::muxer, &is_an_app);

    if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
    {
        muxer_info();
    }

    return result;
}


