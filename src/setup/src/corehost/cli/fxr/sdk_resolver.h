// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
