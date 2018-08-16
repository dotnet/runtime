// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"

class sdk_resolver_t
{
public:
    static bool resolve_sdk_dotnet_path(
        const pal::string_t& dotnet_root, 
        pal::string_t* cli_sdk);

    static bool resolve_sdk_dotnet_path(
        const pal::string_t& dotnet_root,
        const pal::string_t& cwd,
        pal::string_t* cli_sdk,
        bool disallow_prerelease = false,
        pal::string_t* global_json_path = nullptr);
};
