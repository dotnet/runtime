// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>

namespace resolve_component_dependencies_test
{
    bool run_app_and_resolve(
        const pal::string_t& hostfxr_path,
        const pal::string_t& app_path,
        const pal::string_t& component_path,
        pal::stringstream_t& test_output);

    bool run_app_and_resolve_multithreaded(
        const pal::string_t& hostfxr_path,
        const pal::string_t& app_path,
        const pal::string_t& component_path_a,
        const pal::string_t& component_path_b,
        pal::stringstream_t& test_output);
}
