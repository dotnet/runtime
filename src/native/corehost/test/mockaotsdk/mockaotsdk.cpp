// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <pal.h>

std::vector<char> tostr(const pal::char_t* value)
{
    std::vector<char> vect;
    pal::pal_utf8string(pal::string_t(value), &vect);
    return vect;
}

SHARED_API int __cdecl dotnet_execute(
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* sdk_dir,
    const pal::char_t* hostfxr_path,
    int argc,
    const pal::char_t** argv)
{
    std::cout << "mock AOT SDK invoked" << std::endl;
    std::cout << "mock host_path: " << tostr(host_path).data() << std::endl;
    std::cout << "mock dotnet_root: " << tostr(dotnet_root).data() << std::endl;
    std::cout << "mock sdk_dir: " << tostr(sdk_dir).data() << std::endl;
    std::cout << "mock hostfxr_path: " << tostr(hostfxr_path).data() << std::endl;
    std::cout << "mock argc: " << argc << std::endl;
    for (int i = 0; i < argc; i++)
    {
        std::cout << "mock argv[" << i << "]: " << tostr(argv[i]).data() << std::endl;
    }

    return 0;
}
