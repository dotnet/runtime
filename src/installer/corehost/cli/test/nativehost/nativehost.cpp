// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <nethost.h>
#include "comhost_test.h"
#include <hostfxr.h>
#include "host_context_test.h"

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
        // args: ... [<assembly_path>] [<hostfxr_to_load>]
        const pal::char_t *assembly_path = nullptr;
        if (argc >= 3)
            assembly_path = argv[2];

        if (argc >= 4)
        {
            pal::string_t to_load = argv[3];
            pal::dll_t fxr;
            if (!pal::load_library(&to_load, &fxr))
            {
                std::cout << "Failed to load library: " << tostr(to_load).data() << std::endl;
                return EXIT_FAILURE;
            }
        }

        pal::string_t fxr_path;
        size_t len = fxr_path.size();
        int res = get_hostfxr_path(nullptr, &len, assembly_path);
        if (static_cast<StatusCode>(res) == StatusCode::HostApiBufferTooSmall)
        {
            fxr_path.resize(len);
            res = get_hostfxr_path(&fxr_path[0], &len, assembly_path);
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
            success = host_context_test::app(check_properties, hostfxr_path, app_or_config_path, remaining_argc, remaining_argv, test_output);
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
            ++remaining_argv;

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
            ++remaining_argv;

            success = host_context_test::mixed(check_properties, hostfxr_path, app_or_config_path, config_path, remaining_argc, remaining_argv, test_output);
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

        return success ? EXIT_SUCCESS : EXIT_FAILURE;
    }
#endif
    else
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }
}
