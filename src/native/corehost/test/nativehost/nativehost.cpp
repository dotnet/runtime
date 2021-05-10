// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <nethost.h>
#include "comhost_test.h"
#include <hostfxr.h>
#include "host_context_test.h"
#include "resolve_component_dependencies_test.h"
#include "get_native_search_directories_test.h"
#include <utils.h>

namespace
{
    std::vector<char> tostr(const pal::string_t &value)
    {
        std::vector<char> vect;
        pal::pal_utf8string(value, &vect);
        return vect;
    }
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t *argv[])
#else
int main(const int argc, const pal::char_t *argv[])
#endif
{
    if (argc < 2)
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }

    const pal::char_t *command = argv[1];
    if (pal::strcmp(command, _X("get_hostfxr_path")) == 0)
    {
        // args: ... [<explicit_load>] [<assembly_path>] [<dotnet_root>] [<hostfxr_to_load>]
        bool explicit_load = false;
        if (argc >= 3)
            explicit_load = pal::strcmp(pal::to_lower(pal::string_t{argv[2]}).c_str(), _X("true")) == 0;

        const pal::char_t *assembly_path = nullptr;
        if (argc >= 4 && pal::strcmp(argv[3], _X("nullptr")) != 0)
            assembly_path = argv[3];

        const pal::char_t *dotnet_root = nullptr;
        if (argc >= 5 && pal::strcmp(argv[4], _X("nullptr")) != 0)
            dotnet_root = argv[4];

        if (argc >= 6)
        {
            pal::string_t to_load = argv[5];
            pal::dll_t fxr;
            if (!pal::load_library(&to_load, &fxr))
            {
                std::cout << "Failed to load library: " << tostr(to_load).data() << std::endl;
                return EXIT_FAILURE;
            }
        }

        decltype(&get_hostfxr_path) get_hostfxr_path_fn;
        if (explicit_load)
        {
            pal::string_t nethost_path;
            if (!pal::get_own_executable_path(&nethost_path) || !pal::realpath(&nethost_path))
            {
                std::cout << "Failed to get path to current executable" << std::endl;
                return EXIT_FAILURE;
            }

            nethost_path = get_directory(nethost_path);
            nethost_path.append(MAKE_LIBNAME("nethost"));

            pal::dll_t nethost;
            if (!pal::load_library(&nethost_path, &nethost))
            {
                std::cout << "Failed to load library: " << tostr(nethost_path).data() << std::endl;
                return EXIT_FAILURE;
            }

            get_hostfxr_path_fn = (decltype(get_hostfxr_path_fn))pal::get_symbol(nethost, "get_hostfxr_path");
            if (get_hostfxr_path_fn == nullptr)
            {
                std::cout << "Failed to get get_hostfxr_path export from nethost" << std::endl;
                return EXIT_FAILURE;
            }
        }
        else
        {
            get_hostfxr_path_fn = get_hostfxr_path;
        }

        get_hostfxr_parameters parameters {
            sizeof(get_hostfxr_parameters),
            assembly_path,
            dotnet_root
        };

        // Make version invalid for error case
        if (assembly_path != nullptr && pal::strcmp(assembly_path, _X("[error]")) == 0)
            parameters.size = parameters.size - 1;

        const get_hostfxr_parameters *parameters_ptr = assembly_path != nullptr || dotnet_root != nullptr ? &parameters : nullptr;

        pal::string_t fxr_path;
        size_t len = fxr_path.size();
        int res = get_hostfxr_path_fn(nullptr, &len, parameters_ptr);
        if (static_cast<StatusCode>(res) == StatusCode::HostApiBufferTooSmall)
        {
            fxr_path.resize(len);
            res = get_hostfxr_path_fn(&fxr_path[0], &len, parameters_ptr);
        }

        if (static_cast<StatusCode>(res) == StatusCode::Success)
        {
            std::cout << "get_hostfxr_path succeeded" << std::endl;
            std::cout << "hostfxr_path: " << tostr(pal::to_lower(fxr_path)).data() << std::endl;
            return EXIT_SUCCESS;
        }
        else
        {
            std::cout << "get_hostfxr_path failed: " << std::hex << std::showbase << res << std::endl;
            return EXIT_FAILURE;
        }
    }
    else if (pal::strcmp(command, _X("host_context")) == 0)
    {
        // args: ... <scenario> <check_properties> <hostfxr_path> <app_or_config_path> [<remaining_args>]
        const int min_argc = 6;
        if (argc < min_argc)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::char_t *scenario = argv[2];
        const pal::char_t *check_properties_str = argv[3];
        const pal::string_t hostfxr_path = argv[4];
        const pal::char_t *app_or_config_path = argv[5];

        // Remaining args used as property names to get/set as well as arguments for the app
        int remaining_argc = argc - min_argc;
        const pal::char_t **remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        auto check_properties = host_context_test::check_properties_from_string(check_properties_str);

        pal::stringstream_t test_output;
        bool success = false;
        if (pal::strcmp(scenario, _X("app")) == 0)
        {
            // Everything after hostfxr path is the command line to use
            success = host_context_test::app(check_properties, hostfxr_path, remaining_argc + 1, &argv[5], test_output);
        }
        else if (pal::strcmp(scenario, _X("config")) == 0)
        {
            success = host_context_test::config(check_properties, hostfxr_path, app_or_config_path, remaining_argc, remaining_argv, test_output);
        }
        else if (pal::strcmp(scenario, _X("config_multiple")) == 0)
        {
            // args: ... <scenario> <check_properties> <hostfxr_path> <config_path> <secondary_config_path>
            if (argc < min_argc + 1)
            {
                std::cerr << "Invalid arguments" << std::endl;
                return -1;
            }

            const pal::char_t *secondary_config_path = argv[6];
            --remaining_argc;
            remaining_argv = remaining_argc > 0 ? &argv[min_argc + 1] : nullptr;

            success = host_context_test::config_multiple(check_properties, hostfxr_path, app_or_config_path, secondary_config_path, remaining_argc, remaining_argv, test_output);
        }
        else if (pal::strcmp(scenario, _X("mixed")) == 0)
        {
            // args: ... <scenario> <check_properties> <hostfxr_path> <app_path> <config_path>
            if (argc < min_argc + 1)
            {
                std::cerr << "Invalid arguments" << std::endl;
                return -1;
            }

            const pal::char_t *config_path = argv[6];
            --remaining_argc;
            remaining_argv = remaining_argc > 0 ? &argv[min_argc + 1] : nullptr;

            success = host_context_test::mixed(check_properties, hostfxr_path, app_or_config_path, config_path, remaining_argc, remaining_argv, test_output);
        }
        else if (pal::strcmp(scenario, _X("non_context_mixed_apphost")) == 0
            || pal::strcmp(scenario, _X("non_context_mixed_dotnet")) == 0)
        {
            // args: ... <scenario> <check_properties> <hostfxr_path> <app_path> <config_path>
            if (argc < min_argc + 1)
            {
                std::cerr << "Invalid arguments" << std::endl;
                return -1;
            }

            const pal::char_t *config_path = argv[6];
            --remaining_argc;
            remaining_argv = remaining_argc > 0 ? &argv[min_argc + 1] : nullptr;

            bool launch_as_if_dotnet = pal::strcmp(scenario, _X("non_context_mixed_dotnet")) == 0;
            success = host_context_test::non_context_mixed(check_properties, hostfxr_path, app_or_config_path, config_path, remaining_argc, remaining_argv, launch_as_if_dotnet, test_output);
        }
        else
        {
            std::cerr << "Invalid scenario" << std::endl;
            return -1;
        }

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("component_load_assembly_and_get_function_pointer")) == 0)
    {
        // args: ... <hostfxr_path> <app_or_config_path> <assembly_path> <type_name> <method_name> [<assembly_path> <type_name> <method_name>...]
        const int min_argc = 4;
        if (argc < min_argc + 3)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::string_t hostfxr_path = argv[2];
        const pal::char_t *app_or_config_path = argv[3];

        int remaining_argc = argc - min_argc;
        const pal::char_t **remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        pal::stringstream_t test_output;
        bool success = false;

        success = host_context_test::component_load_assembly_and_get_function_pointer(hostfxr_path, app_or_config_path, remaining_argc, remaining_argv, test_output);

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("app_load_assembly_and_get_function_pointer")) == 0)
    {
        // args: ... <hostfxr_path> <app_path> <assembly_path> <type_name> <method_name> [<assembly_path> <type_name> <method_name>...]
        const int min_argc = 3;
        if (argc < min_argc + 4)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::string_t hostfxr_path = argv[2];

        int remaining_argc = argc - min_argc;
        const pal::char_t **remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        pal::stringstream_t test_output;
        bool success = false;

        success = host_context_test::app_load_assembly_and_get_function_pointer(hostfxr_path, remaining_argc, remaining_argv, test_output);

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("component_get_function_pointer")) == 0)
    {
        // args: ... <hostfxr_path> <app_or_config_path> <type_name> <method_name> [<type_name> <method_name>...]
        const int min_argc = 4;
        if (argc < min_argc + 2)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::string_t hostfxr_path = argv[2];
        const pal::char_t *app_or_config_path = argv[3];

        int remaining_argc = argc - min_argc;
        const pal::char_t **remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        pal::stringstream_t test_output;
        bool success = false;

        success = host_context_test::component_get_function_pointer(hostfxr_path, app_or_config_path, remaining_argc, remaining_argv, test_output);

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("app_get_function_pointer")) == 0)
    {
        // args: ... <hostfxr_path> <app_path> <type_name> <method_name> [<type_name> <method_name>...]
        const int min_argc = 3;
        if (argc < min_argc + 3)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::string_t hostfxr_path = argv[2];

        int remaining_argc = argc - min_argc;
        const pal::char_t **remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        pal::stringstream_t test_output;
        bool success = false;

        success = host_context_test::app_get_function_pointer(hostfxr_path, remaining_argc, remaining_argv, test_output);

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("run_app")) == 0)
    {
        // args: ... <hostfxr_path> <dotnet_command_line>
        const int min_argc = 4;
        if (argc < min_argc)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::string_t hostfxr_path = argv[2];

        int remaining_argc = argc - min_argc + 1;
        const pal::char_t **remaining_argv = &argv[min_argc - 1];

        pal::stringstream_t test_output;
        bool success =  host_context_test::app(host_context_test::check_properties::none, hostfxr_path, remaining_argc, remaining_argv, test_output);

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else if (pal::strcmp(command, _X("resolve_component_dependencies")) == 0)
    {
        // args: ... <scenario> <hostfxr_path> <app_path> <component_path>
        if (argc < 6)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::char_t *scenario = argv[2];
        const pal::string_t hostfxr_path = argv[3];
        const pal::string_t app_path = argv[4];
        const pal::string_t component_path = argv[5];

        pal::stringstream_t test_output;
        bool success = false;
        if (pal::strcmp(scenario, _X("run_app_and_resolve")) == 0)
        {
            success = resolve_component_dependencies_test::run_app_and_resolve(hostfxr_path, app_path, component_path, test_output);
        }
        else if (pal::strcmp(scenario, _X("run_app_and_resolve_multithreaded")) == 0)
        {
            if (argc < 7)
            {
                std::cerr << "Invalid arguments" << std::endl;
                return -1;
            }

            const pal::string_t component_path_b = argv[6];

            success = resolve_component_dependencies_test::run_app_and_resolve_multithreaded(hostfxr_path, app_path, component_path, component_path_b, test_output);
        }

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
#if defined(_WIN32)
    else if (pal::strcmp(command, _X("comhost")) == 0)
    {
        // args: ... <scenario> <activation_count> <comhost_path> <clsid>
        if (argc < 6)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::char_t *scenario = argv[2];
        int count = pal::xtoi(argv[3]);
        const pal::string_t comhost_path = argv[4];
        const pal::string_t clsid_str = argv[5];

        bool success = false;
        if (pal::strcmp(scenario, _X("synchronous")) == 0)
        {
            success = comhost_test::synchronous(comhost_path, clsid_str, count);
        }
        else if (pal::strcmp(scenario, _X("concurrent")) == 0)
        {
            success = comhost_test::concurrent(comhost_path, clsid_str, count);
        }
        else if (pal::strcmp(scenario, _X("errorinfo")) == 0)
        {
            success = comhost_test::errorinfo(comhost_path, clsid_str, count);
        }
        else if (pal::strcmp(scenario, _X("typelib")) == 0)
        {
            success = comhost_test::typelib(comhost_path, count);
        }

        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
#endif
    else if (pal::strcmp(command, _X("get_native_search_directories")) == 0)
    {
        // args: ... <scenario> <hostfxrpath>
        int min_argc = 4;
        if (argc < min_argc)
        {
            std::cerr << "Invalid arguments" << std::endl;
            return -1;
        }

        const pal::char_t* scenario = argv[2];
        const pal::string_t hostfxr_path = argv[3];

        int remaining_argc = argc - min_argc;
        const pal::char_t** remaining_argv = nullptr;
        if (argc > min_argc)
            remaining_argv = &argv[min_argc];

        pal::stringstream_t test_output;
        bool success = false;
        if (pal::strcmp(scenario, _X("get_for_command_line")) == 0)
        {
            success = get_native_search_directories_test::get_for_command_line(hostfxr_path, remaining_argc, remaining_argv, test_output);
        }

        std::cout << tostr(test_output.str()).data() << std::endl;
        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
    else
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }
}
