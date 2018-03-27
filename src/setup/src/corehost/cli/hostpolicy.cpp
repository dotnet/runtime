// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "deps_resolver.h"
#include "fx_muxer.h"
#include "utils.h"
#include "coreclr.h"
#include "cpprest/json.h"
#include "libhost.h"
#include "error_codes.h"
#include "breadcrumbs.h"
#include "host_startup_info.h"

hostpolicy_init_t g_init;

int run(const arguments_t& args, pal::string_t* out_host_command_result = nullptr)
{
    // Load the deps resolver
    deps_resolver_t resolver(g_init, args);

    pal::string_t resolver_errors;
    if (!resolver.valid(&resolver_errors))
    {
        trace::error(_X("Error initializing the dependency resolver: %s"), resolver_errors.c_str());
        return StatusCode::ResolverInitFailure;
    }

    // Setup breadcrumbs. Breadcrumbs are not enabled for API calls because they do not execute
    // the app and may be re-entry
    probe_paths_t probe_paths;
    std::unordered_set<pal::string_t> breadcrumbs;
    bool breadcrumbs_enabled = (out_host_command_result == nullptr);
    if (breadcrumbs_enabled)
    {
        pal::string_t policy_name = _STRINGIFY(HOST_POLICY_PKG_NAME);
        pal::string_t policy_version = _STRINGIFY(HOST_POLICY_PKG_VER);

        // Always insert the hostpolicy that the code is running on.
        breadcrumbs.insert(policy_name);
        breadcrumbs.insert(policy_name + _X(",") + policy_version);

        if (!resolver.resolve_probe_paths(&probe_paths, &breadcrumbs))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }
    else
    {
        if (!resolver.resolve_probe_paths(&probe_paths, nullptr))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }

    pal::string_t clr_path = probe_paths.coreclr;
    if (clr_path.empty() || !pal::realpath(&clr_path))
    {
        trace::error(_X("Could not resolve CoreCLR path. For more details, enable tracing by setting COREHOST_TRACE environment variable to 1"));;
        return StatusCode::CoreClrResolveFailure;
    }

    // Get path in which CoreCLR is present.
    pal::string_t clr_dir = get_directory(clr_path);

    // System.Private.CoreLib.dll is expected to be next to CoreCLR.dll - add its path to the TPA list.
    pal::string_t corelib_path = clr_dir;
    append_path(&corelib_path, CORELIB_NAME);

    // Append CoreLib path
    if (probe_paths.tpa.back() != PATH_SEPARATOR)
    {
        probe_paths.tpa.push_back(PATH_SEPARATOR);
    }

    probe_paths.tpa.append(corelib_path);
    probe_paths.tpa.push_back(PATH_SEPARATOR);

    pal::string_t clrjit_path = probe_paths.clrjit;
    if (clrjit_path.empty())
    {
        trace::warning(_X("Could not resolve CLRJit path"));
    }
    else if (pal::realpath(&clrjit_path))
    {
        trace::verbose(_X("The resolved JIT path is '%s'"), clrjit_path.c_str());
    }
    else
    {
        clrjit_path.clear();
        trace::warning(_X("Could not resolve symlink to CLRJit path '%s'"), probe_paths.clrjit.c_str());
    }

    // Build CoreCLR properties
    std::vector<const char*> property_keys = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        // Workaround: mscorlib does not resolve symlinks for AppContext.BaseDirectory dotnet/coreclr/issues/2128
        "APP_CONTEXT_BASE_DIRECTORY",
        "APP_CONTEXT_DEPS_FILES",
        "FX_DEPS_FILE",
        "PROBING_DIRECTORIES"
    };

    // Note: these variables' lifetime should be longer than coreclr_initialize.
    std::vector<char> tpa_paths_cstr, app_base_cstr, native_dirs_cstr, resources_dirs_cstr, fx_deps, deps, clrjit_path_cstr, probe_directories;
    pal::pal_clrstring(probe_paths.tpa, &tpa_paths_cstr);
    pal::pal_clrstring(args.app_root, &app_base_cstr);
    pal::pal_clrstring(probe_paths.native, &native_dirs_cstr);
    pal::pal_clrstring(probe_paths.resources, &resources_dirs_cstr);

    pal::string_t fx_deps_str;
    if (resolver.get_fx_definitions().size() >= 2)
    {
        // Use the root fx to define FX_DEPS_FILE
        fx_deps_str = get_root_framework(resolver.get_fx_definitions()).get_deps_file();
    }
    pal::pal_clrstring(fx_deps_str, &fx_deps);

    // Get all deps files
    pal::string_t allDeps;
    for (int i = 0; i < resolver.get_fx_definitions().size(); ++i)
    {
        allDeps += resolver.get_fx_definitions()[i]->get_deps_file();
        if (i < resolver.get_fx_definitions().size() - 1)
        {
            allDeps += _X(";");
        }
    }
    pal::pal_clrstring(allDeps, &deps);

    pal::pal_clrstring(resolver.get_lookup_probe_directories(), &probe_directories);

    std::vector<const char*> property_values = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpa_paths_cstr.data(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        native_dirs_cstr.data(),
        // PLATFORM_RESOURCE_ROOTS
        resources_dirs_cstr.data(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified",
        // APP_CONTEXT_BASE_DIRECTORY
        app_base_cstr.data(),
        // APP_CONTEXT_DEPS_FILES,
        deps.data(),
        // FX_DEPS_FILE
        fx_deps.data(),
        //PROBING_DIRECTORIES
        probe_directories.data()
    };

    if (!clrjit_path.empty())
    {
        pal::pal_clrstring(clrjit_path, &clrjit_path_cstr);
        property_keys.push_back("JIT_PATH");
        property_values.push_back(clrjit_path_cstr.data());
    }

    bool set_app_paths = false;

    // Runtime options config properties.
    for (int i = 0; i < g_init.cfg_keys.size(); ++i)
    {
        // Provide opt-in compatible behavior by using the switch to set APP_PATHS
        if (pal::cstrcasecmp(g_init.cfg_keys[i].data(), "Microsoft.NETCore.DotNetHostPolicy.SetAppPaths") == 0)
        {
            set_app_paths = (pal::cstrcasecmp(g_init.cfg_values[i].data(), "true") == 0);
        }

        property_keys.push_back(g_init.cfg_keys[i].data());
        property_values.push_back(g_init.cfg_values[i].data());
    }

    // App paths and App NI paths
    if (set_app_paths)
    {
        property_keys.push_back("APP_PATHS");
        property_keys.push_back("APP_NI_PATHS");
        property_values.push_back(app_base_cstr.data());
        property_values.push_back(app_base_cstr.data());
    }

    size_t property_size = property_keys.size();
    assert(property_keys.size() == property_values.size());

    unsigned int exit_code = 1;

    // Check for host command(s)
    if (pal::strcasecmp(g_init.host_command.c_str(), _X("get-native-search-directories")) == 0)
    {
        // Verify property_keys[1] contains the correct information
        if (pal::cstrcasecmp(property_keys[1], "NATIVE_DLL_SEARCH_DIRECTORIES"))
        {
            trace::error(_X("get-native-search-directories failed to find NATIVE_DLL_SEARCH_DIRECTORIES property"));
            exit_code = HostApiFailed;
        }
        else
        {
            assert(out_host_command_result != nullptr);
            pal::clr_palstring(property_values[1], out_host_command_result);
            exit_code = 0; // Success
        }

        return exit_code;
    }

   // Bind CoreCLR
    trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), clr_path.c_str(), clr_dir.c_str());
    if (!coreclr::bind(clr_dir))
    {
        trace::error(_X("Failed to bind to CoreCLR at '%s'"), clr_path.c_str());
        return StatusCode::CoreClrBindFailure;
    }

    // Verbose logging
    if (trace::is_enabled())
    {
        for (size_t i = 0; i < property_size; ++i)
        {
            pal::string_t key, val;
            pal::clr_palstring(property_keys[i], &key);
            pal::clr_palstring(property_values[i], &val);
            trace::verbose(_X("Property %s = %s"), key.c_str(), val.c_str());
        }
    }

    std::vector<char> host_path;
    pal::pal_clrstring(args.host_path, &host_path);

    // Initialize CoreCLR
    coreclr::host_handle_t host_handle;
    coreclr::domain_id_t domain_id;
    auto hr = coreclr::initialize(
        host_path.data(),
        "clrhost",
        property_keys.data(),
        property_values.data(),
        property_size,
        &host_handle,
        &domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to initialize CoreCLR, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrInitFailure;
    }

    // Initialize clr strings for arguments
    std::vector<std::vector<char>> argv_strs(args.app_argc);
    std::vector<const char*> argv(args.app_argc);
    for (int i = 0; i < args.app_argc; i++)
    {
        pal::pal_clrstring(args.app_argv[i], &argv_strs[i]);
        argv[i] = argv_strs[i].data();
    }

    if (trace::is_enabled())
    {
        pal::string_t arg_str;
        for (int i = 0; i < argv.size(); i++)
        {
            pal::string_t cur;
            pal::clr_palstring(argv[i], &cur);
            arg_str.append(cur);
            arg_str.append(_X(","));
        }
        trace::info(_X("Launch host: %s, app: %s, argc: %d, args: %s"), args.host_path.c_str(),
            args.managed_application.c_str(), args.app_argc, arg_str.c_str());
    }

    std::vector<char> managed_app;
    pal::pal_clrstring(args.managed_application, &managed_app);

    breadcrumb_writer_t writer(&breadcrumbs);
    if (breadcrumbs_enabled)
    {
        // Leave breadcrumbs for servicing.
        writer.begin_write();
    }

    // Previous hostpolicy trace messages must be printed before executing assembly
    trace::flush();

    // Execute the application
    hr = coreclr::execute_assembly(
        host_handle,
        domain_id,
        argv.size(),
        argv.data(),
        managed_app.data(),
        &exit_code);

    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to execute managed app, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrExeFailure;
    }

    // Shut down the CoreCLR
    hr = coreclr::shutdown(host_handle, domain_id, (int*)&exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("Failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    coreclr::unload();

    if (breadcrumbs_enabled)
    {
        // Finish breadcrumb writing
        writer.end_write();
    }

    return exit_code;
}

SHARED_API int corehost_load(host_interface_t* init)
{
    trace::setup();

    // Re-initialize global state in case of re-entry
    g_init = hostpolicy_init_t();

    if (!hostpolicy_init_t::init(init, &g_init))
    {
        return StatusCode::LibHostInitFailure;
    }
    
    return 0;
}

int corehost_main_init(const int argc, const pal::char_t* argv[], const pal::string_t location, arguments_t& args)
{
    if (trace::is_enabled())
    {
        trace::info(_X("--- Invoked hostpolicy %s[commit hash: %s] [%s,%s,%s][%s] main = {"),
            location.c_str(),
            _STRINGIFY(REPO_COMMIT_HASH),
            _STRINGIFY(HOST_POLICY_PKG_NAME),
            _STRINGIFY(HOST_POLICY_PKG_VER),
            _STRINGIFY(HOST_POLICY_PKG_REL_DIR),
            get_arch());

        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));

        trace::info(_X("Deps file: %s"), g_init.deps_file.c_str());
        for (const auto& probe : g_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // Take care of arguments
    if (!g_init.host_info.is_valid())
    {
        // For backwards compat (older hostfxr), default the host_info
        g_init.host_info.parse(argc, argv);
    }

    if (!parse_arguments(g_init, argc, argv, &args))
    {
        return StatusCode::LibHostInvalidArgs;
    }
    if (trace::is_enabled())
    {
        args.print();
    }

    return 0;
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    arguments_t args;
    int rc = corehost_main_init(argc, argv, _X(""), args);
    if (!rc)
    {
        rc = run(args);
    }

    return rc;
}

SHARED_API int corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    arguments_t args;

    int rc = corehost_main_init(argc, argv, _X("corehost_main_with_output_buffer "), args);
    if (!rc)
    {
        if (g_init.host_command == _X("get-native-search-directories"))
        {
            pal::string_t output_string;
            rc = run(args, &output_string);
            if (!rc)
            {
                // Get length in character count not including null terminator
                int len = output_string.length();

                if (len + 1 > buffer_size)
                {
                    rc = HostApiBufferTooSmall;
                    *required_buffer_size = len + 1;
                    trace::info(_X("get-native-search-directories failed with buffer too small"), output_string.c_str());
                }
                else
                {
                    output_string.copy(buffer, len);
                    buffer[len] = '\0';
                    *required_buffer_size = 0;
                    trace::info(_X("get-native-search-directories success: %s"), output_string.c_str());
                }
            }
        }
        else
        {
            trace::error(_X("Unknown command: %s"), g_init.host_command.c_str());
            rc = LibHostUnknownCommand;
        }
    }

    return rc;
}

SHARED_API int corehost_unload()
{
    return 0;
}
