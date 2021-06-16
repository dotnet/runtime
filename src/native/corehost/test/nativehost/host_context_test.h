// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <hostfxr.h>

namespace host_context_test
{
    enum check_properties
    {
        none,
        get,
        set,
        remove,
        get_all,
        get_active,
        get_all_active
    };

    check_properties check_properties_from_string(const pal::char_t *str);

    bool app(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool config(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool config_multiple(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        const pal::char_t *secondary_config_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool mixed(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *app_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool non_context_mixed(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *app_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        bool launch_as_if_dotnet, // Imitate running the application as if it were launched with 'dotnet <appPath>'
        pal::stringstream_t &test_output);
    bool component_load_assembly_and_get_function_pointer(
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool app_load_assembly_and_get_function_pointer(
        const pal::string_t &hostfxr_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool component_get_function_pointer(
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
    bool app_get_function_pointer(
        const pal::string_t &hostfxr_path,
        int argc,
        const pal::char_t *argv[],
        pal::stringstream_t &test_output);
}
