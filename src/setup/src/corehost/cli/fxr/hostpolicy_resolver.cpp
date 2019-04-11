// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cassert>
#include <error_codes.h>
#include <fx_definition.h>
#include "hostpolicy_resolver.h"
#include <mutex>
#include <pal.h>
#include <trace.h>
#include <utils.h>

namespace
{
    std::mutex g_hostpolicy_lock;
    pal::dll_t g_hostpolicy;
    hostpolicy_contract g_hostpolicy_contract;

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
    * Return name of deps file for app.
    */
    pal::string_t get_deps_file(
        bool is_framework_dependent,
        const pal::string_t& app_candidate,
        const pal::string_t& specified_deps_file,
        const fx_definition_vector_t& fx_definitions
    )
    {
        if (is_framework_dependent)
        {
            // The hostpolicy is resolved from the root framework's name and location.
            pal::string_t deps_file = get_root_framework(fx_definitions).get_dir();
            if (!deps_file.empty() && deps_file.back() != DIR_SEPARATOR)
            {
                deps_file.push_back(DIR_SEPARATOR);
            }

            return deps_file + get_root_framework(fx_definitions).get_name() + _X(".deps.json");
        }
        else
        {
            // Self-contained app's hostpolicy is from specified deps or from app deps.
            return !specified_deps_file.empty() ? specified_deps_file : get_deps_from_app_binary(get_directory(app_candidate), app_candidate);
        }
    }
}

int hostpolicy_resolver::load(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    hostpolicy_contract &host_contract)
{
    std::lock_guard<std::mutex> lock{ g_hostpolicy_lock };
    if (g_hostpolicy == nullptr)
    {
        pal::string_t host_path;
        if (!library_exists_in_dir(lib_dir, LIBHOSTPOLICY_NAME, &host_path))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        // Load library
        if (!pal::load_library(&host_path, &g_hostpolicy))
        {
            trace::info(_X("Load library of %s failed"), host_path.c_str());
            return StatusCode::CoreHostLibLoadFailure;
        }

        // Obtain entrypoint symbols
        g_hostpolicy_contract.load = (corehost_load_fn)pal::get_symbol(g_hostpolicy, "corehost_load");
        g_hostpolicy_contract.unload = (corehost_unload_fn)pal::get_symbol(g_hostpolicy, "corehost_unload");
        if ((g_hostpolicy_contract.load == nullptr) || (g_hostpolicy_contract.unload == nullptr))
            return StatusCode::CoreHostEntryPointFailure;

        g_hostpolicy_contract.set_error_writer = (corehost_set_error_writer_fn)pal::get_symbol(g_hostpolicy, "corehost_set_error_writer");

        // It's possible to not have corehost_set_error_writer, since this was only introduced in 3.0
        // so 2.0 hostpolicy would not have the export. In this case we will not propagate the error writer
        // and errors will still be reported to stderr.
    }

    // Return global values
    *h_host = g_hostpolicy;
    host_contract = g_hostpolicy_contract;

    return StatusCode::Success;
}

/**
* Return location that is expected to contain hostpolicy
*/
bool hostpolicy_resolver::try_get_dir(
    host_mode_t mode,
    const pal::string_t& dotnet_root,
    const fx_definition_vector_t& fx_definitions,
    const pal::string_t& app_candidate,
    const pal::string_t& specified_deps_file,
    const std::vector<pal::string_t>& probe_realpaths,
    pal::string_t* impl_dir)
{
    bool is_framework_dependent = get_app(fx_definitions).get_runtime_config().get_is_framework_dependent();

    // Obtain deps file for the given configuration.
    pal::string_t resolved_deps = get_deps_file(is_framework_dependent, app_candidate, specified_deps_file, fx_definitions);

    // Resolve hostpolicy version out of the deps file.
    pal::string_t version = resolve_hostpolicy_version_from_deps(resolved_deps);
    if (trace::is_enabled() && version.empty() && pal::file_exists(resolved_deps))
    {
        trace::warning(_X("Dependency manifest %s does not contain an entry for %s"),
            resolved_deps.c_str(), _STRINGIFY(HOST_POLICY_PKG_NAME));
    }

    // Check if the given version of the hostpolicy exists in servicing.
    if (hostpolicy_exists_in_svc(version, impl_dir))
    {
        return true;
    }

    // Get the expected directory that would contain hostpolicy.
    pal::string_t expected;
    if (is_framework_dependent)
    {
        // The hostpolicy is required to be in the root framework's location
        expected.assign(get_root_framework(fx_definitions).get_dir());
        assert(pal::directory_exists(expected));
    }
    else
    {
        // Native apps can be activated by muxer, native exe host or "corehost"
        // 1. When activated with dotnet.exe or corehost.exe, check for hostpolicy in the deps dir or
        //    app dir.
        // 2. When activated with native exe, the standalone host, check own directory.
        assert(mode != host_mode_t::invalid);
        switch (mode)
        {
        case host_mode_t::apphost:
        case host_mode_t::libhost:
            expected = dotnet_root;
            break;

        default:
            expected = get_directory(specified_deps_file.empty() ? app_candidate : specified_deps_file);
            break;
        }
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
    trace::error(_X("A fatal error was encountered. The library '%s' required to execute the application was not found in '%s'."),
        LIBHOSTPOLICY_NAME, expected.c_str());
    if (mode == host_mode_t::muxer && !is_framework_dependent)
    {
        if (!pal::file_exists(get_app(fx_definitions).get_runtime_config().get_path()))
        {
            trace::error(_X("Failed to run as a self-contained app. If this should be a framework-dependent app, add the %s file specifying the appropriate framework."),
                get_app(fx_definitions).get_runtime_config().get_path().c_str());
        }
        else if (get_app(fx_definitions).get_name().empty())
        {
            trace::error(_X("Failed to run as a self-contained app. If this should be a framework-dependent app, specify the appropriate framework in %s."),
                get_app(fx_definitions).get_runtime_config().get_path().c_str());
        }
    }
    return false;
}