// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <error_codes.h>
#include "hostfxr_exports.h"

hostfxr_exports::hostfxr_exports(const pal::string_t &hostfxr_path)
{
    if (!pal::load_library(&hostfxr_path, &_dll))
    {
        std::cout << "Load library of hostfxr failed" << std::endl;
        throw StatusCode::CoreHostLibLoadFailure;
    }

    init_command_line = (hostfxr_initialize_for_dotnet_command_line_fn)pal::get_symbol(_dll, "hostfxr_initialize_for_dotnet_command_line");
    run_app = (hostfxr_run_app_fn)pal::get_symbol(_dll, "hostfxr_run_app");

    init_config = (hostfxr_initialize_for_runtime_config_fn)pal::get_symbol(_dll, "hostfxr_initialize_for_runtime_config");
    get_delegate = (hostfxr_get_runtime_delegate_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_delegate");

    get_prop_value = (hostfxr_get_runtime_property_value_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_property_value");
    set_prop_value = (hostfxr_set_runtime_property_value_fn)pal::get_symbol(_dll, "hostfxr_set_runtime_property_value");
    get_properties = (hostfxr_get_runtime_properties_fn)pal::get_symbol(_dll, "hostfxr_get_runtime_properties");

    close = (hostfxr_close_fn)pal::get_symbol(_dll, "hostfxr_close");

    main_startupinfo = (hostfxr_main_startupinfo_fn)pal::get_symbol(_dll, "hostfxr_main_startupinfo");

    set_error_writer = (hostfxr_set_error_writer_fn)pal::get_symbol(_dll, "hostfxr_set_error_writer");

    get_native_search_directories = (hostfxr_get_native_search_directories_fn)pal::get_symbol(_dll, "hostfxr_get_native_search_directories");

    if (init_command_line == nullptr || run_app == nullptr
        || init_config == nullptr || get_delegate == nullptr
        || get_prop_value == nullptr || set_prop_value == nullptr
        || get_properties == nullptr || close == nullptr
        || main_startupinfo == nullptr || set_error_writer == nullptr
        || get_native_search_directories == nullptr)
    {
        std::cout << "Failed to get hostfxr entry points" << std::endl;
        throw StatusCode::CoreHostEntryPointFailure;
    }
}

hostfxr_exports::~hostfxr_exports()
{
    pal::unload_library(_dll);
}
