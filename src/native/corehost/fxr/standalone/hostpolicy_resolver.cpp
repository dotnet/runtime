// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include <error_codes.h>
#include <fx_definition.h>
#include "hostpolicy_resolver.h"
#include <mutex>
#include <pal.h>
#include <trace.h>
#include <utils.h>

#include "json_parser.h"

namespace
{
    std::mutex g_hostpolicy_lock;
    pal::dll_t g_hostpolicy;
    hostpolicy_contract_t g_hostpolicy_contract;
    pal::string_t g_hostpolicy_dir;
}

int hostpolicy_resolver::load(
    const pal::string_t& lib_dir,
    pal::dll_t* dll,
    hostpolicy_contract_t &hostpolicy_contract)
{
    std::lock_guard<std::mutex> lock{ g_hostpolicy_lock };
    if (g_hostpolicy == nullptr)
    {
        pal::string_t host_path;
        if (!file_exists_in_dir(lib_dir, LIBHOSTPOLICY_NAME, &host_path))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        // We should always be loading hostpolicy from an absolute path
        if (!pal::is_path_fully_qualified(host_path))
            return StatusCode::CoreHostLibMissingFailure;

        // Load library
        // We expect to leak hostpolicy - just as we do not unload coreclr, we do not unload hostpolicy
        if (!pal::load_library(&host_path, &g_hostpolicy))
        {
            trace::info(_X("Load library of %s failed"), host_path.c_str());
            return StatusCode::CoreHostLibLoadFailure;
        }

        // Obtain entrypoint symbols
        g_hostpolicy_contract.corehost_main = reinterpret_cast<corehost_main_fn>(pal::get_symbol(g_hostpolicy, "corehost_main"));
        g_hostpolicy_contract.load = reinterpret_cast<corehost_load_fn>(pal::get_symbol(g_hostpolicy, "corehost_load"));
        g_hostpolicy_contract.unload = reinterpret_cast<corehost_unload_fn>(pal::get_symbol(g_hostpolicy, "corehost_unload"));
        if ((g_hostpolicy_contract.load == nullptr) || (g_hostpolicy_contract.unload == nullptr))
            return StatusCode::CoreHostEntryPointFailure;

        g_hostpolicy_contract.corehost_main_with_output_buffer = reinterpret_cast<corehost_main_with_output_buffer_fn>(pal::get_symbol(g_hostpolicy, "corehost_main_with_output_buffer"));

        // It's possible to not have corehost_main_with_output_buffer.
        // This was introduced in 2.1, so 2.0 hostpolicy would not have the exports.
        // Callers are responsible for checking that the function pointer is not null before using it.

        g_hostpolicy_contract.set_error_writer = reinterpret_cast<corehost_set_error_writer_fn>(pal::get_symbol(g_hostpolicy, "corehost_set_error_writer"));
        g_hostpolicy_contract.initialize = reinterpret_cast<corehost_initialize_fn>(pal::get_symbol(g_hostpolicy, "corehost_initialize"));

        // It's possible to not have corehost_set_error_writer and corehost_initialize. These were
        // introduced in 3.0, so 2.0 hostpolicy would not have the exports. In this case, we will
        // not propagate the error writer and errors will still be reported to stderr. Callers are
        // responsible for checking that the function pointers are not null before using them.

        g_hostpolicy_dir = lib_dir;
    }
    else
    {
        if (!pal::are_paths_equal_with_normalized_casing(g_hostpolicy_dir, lib_dir))
            trace::warning(_X("The library %s was already loaded from [%s]. Reusing the existing library for the request to load from [%s]"), LIBHOSTPOLICY_NAME, g_hostpolicy_dir.c_str(), lib_dir.c_str());
    }

    // Return global values
    *dll = g_hostpolicy;
    hostpolicy_contract = g_hostpolicy_contract;

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
    pal::string_t* impl_dir)
{
    bool is_framework_dependent = get_app(fx_definitions).get_runtime_config().get_is_framework_dependent();

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
    if (file_exists_in_dir(expected, LIBHOSTPOLICY_NAME, nullptr))
    {
        impl_dir->assign(expected);
        return true;
    }

    // If it still couldn't be found, somebody upstack messed up. Flag an error for the "expected" location.
    trace::error(_X("A fatal error was encountered. The library '%s' required to execute the application was not found in '%s'."),
        LIBHOSTPOLICY_NAME, expected.c_str());
    if ((mode == host_mode_t::muxer || mode == host_mode_t::apphost) && !is_framework_dependent)
    {
        trace::error(_X("Failed to run as a self-contained app."));
        const pal::string_t config_file_name = get_app(fx_definitions).get_runtime_config().get_path();
        if (!pal::file_exists(config_file_name))
        {
            trace::error(_X("  - The application was run as a self-contained app because '%s' was not found."),
                config_file_name.c_str());
            trace::error(_X("  - If this should be a framework-dependent app, add the '%s' file and specify the appropriate framework."),
                config_file_name.c_str());
        }
        else if (get_app(fx_definitions).get_name().empty())
        {
            trace::error(_X("  - The application was run as a self-contained app because '%s' did not specify a framework."),
                config_file_name.c_str());
            trace::error(_X("  - If this should be a framework-dependent app, specify the appropriate framework in '%s'."),
                config_file_name.c_str());
        }
    }
    return false;
}
