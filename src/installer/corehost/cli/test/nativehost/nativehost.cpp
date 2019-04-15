// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <nethost.h>
#include "comhost_test.h"

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
        const pal::char_t *assembly_path = nullptr;
        if (argc >= 3)
            assembly_path = argv[2];

#if defined(_WIN32)
        pal::string_t testOverride;
        if (pal::getenv(_X("TEST_OVERRIDE_PROGRAMFILES"), &testOverride))
        {
            std::cout << tostr(testOverride).data() << std::endl;
            ::SetEnvironmentVariableW(_X("ProgramFiles"), testOverride.c_str());
            ::SetEnvironmentVariableW(_X("ProgramFiles(x86)"), testOverride.c_str());
        }
#endif

        pal::string_t fxr_path;
        size_t len = fxr_path.size();
        int res = get_hostfxr_path(nullptr, &len, assembly_path);
        if (res == StatusCode::HostApiBufferTooSmall)
        {
            fxr_path.resize(len);
            res = get_hostfxr_path(&fxr_path[0], &len, assembly_path);
        }

        if (res == StatusCode::Success)
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