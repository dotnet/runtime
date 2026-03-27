// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <nethost.h>
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
        // args: ... [<assembly_path>] [<dotnet_root>] [<hostfxr_to_load>]
        const pal::char_t *assembly_path = nullptr;
        if (argc >= 3 && pal::strcmp(argv[2], _X("nullptr")) != 0)
            assembly_path = argv[2];

        const pal::char_t *dotnet_root = nullptr;
        if (argc >= 4 && pal::strcmp(argv[3], _X("nullptr")) != 0)
            dotnet_root = argv[3];

        if (argc >= 5)
        {
            pal::string_t to_load = argv[4];
            pal::dll_t fxr;
            if (!pal::load_library(&to_load, &fxr))
            {
                std::cout << "Failed to load library: " << tostr(to_load).data() << std::endl;
                return EXIT_FAILURE;
            }
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
        int res = get_hostfxr_path(nullptr, &len, parameters_ptr);
        if (static_cast<StatusCode>(res) == StatusCode::HostApiBufferTooSmall)
        {
            fxr_path.resize(len);
            res = get_hostfxr_path(&fxr_path[0], &len, parameters_ptr);
        }

        if (static_cast<StatusCode>(res) == StatusCode::Success)
        {
            std::cout << "get_hostfxr_path succeeded" << std::endl;
            std::cout << "hostfxr_path: " << tostr(to_lower(fxr_path.c_str())).data() << std::endl;
            return EXIT_SUCCESS;
        }
        else
        {
            std::cout << "get_hostfxr_path failed: " << std::hex << std::showbase << res << std::endl;
            return EXIT_FAILURE;
        }
    }
    else
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }
}
