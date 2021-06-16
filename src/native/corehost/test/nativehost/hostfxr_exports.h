// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <hostfxr.h>

typedef int32_t(HOSTFXR_CALLTYPE* hostfxr_get_native_search_directories_fn)(const int argc, const char_t* argv[], char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size);

class hostfxr_exports
{
public:
    hostfxr_initialize_for_dotnet_command_line_fn init_command_line;
    hostfxr_run_app_fn run_app;

    hostfxr_initialize_for_runtime_config_fn init_config;
    hostfxr_get_runtime_delegate_fn get_delegate;

    hostfxr_get_runtime_property_value_fn get_prop_value;
    hostfxr_set_runtime_property_value_fn set_prop_value;
    hostfxr_get_runtime_properties_fn get_properties;

    hostfxr_close_fn close;

    hostfxr_main_startupinfo_fn main_startupinfo;

    hostfxr_set_error_writer_fn set_error_writer;

    hostfxr_get_native_search_directories_fn get_native_search_directories;

public:
    hostfxr_exports(const pal::string_t &hostfxr_path);
    ~hostfxr_exports();

private:
    pal::dll_t _dll;
};
