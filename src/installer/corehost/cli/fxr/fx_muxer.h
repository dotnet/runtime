// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <corehost_context_contract.h>
#include "command_line.h"
#include "error_codes.h"
#include "fx_definition.h"
#include "host_context.h"
#include "host_interface.h"
#include "host_startup_info.h"

class fx_muxer_t
{
public:
    static int execute(
        const pal::string_t host_command,
        const int argc,
        const pal::char_t* argv[],
        const host_startup_info_t& host_info,
        pal::char_t result_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size);
    static int initialize_for_app(
        const host_startup_info_t& host_info,
        int argc,
        const pal::char_t* argv[],
        const opt_map_t& opts,
        hostfxr_handle *host_context_handle);
    static int initialize_for_runtime_config(
        const host_startup_info_t& host_info,
        const pal::char_t * runtime_config_path,
        hostfxr_handle *host_context_handle);
    static int run_app(host_context_t *context);
    static int get_runtime_delegate(
        host_context_t *context,
        coreclr_delegate_type delegate_type,
        void** delegate);
    static const host_context_t* get_active_host_context();
    static int close_host_context(host_context_t *context);
private:
    static int handle_exec_host_command(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& app_candidate,
        const opt_map_t& opts,
        int argc,
        const pal::char_t* argv[],
        int argoff,
        host_mode_t mode,
        pal::char_t result_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size);
    static int handle_cli(
        const host_startup_info_t& host_info,
        int argc,
        const pal::char_t* argv[],
        const pal::string_t& app_candidate);
};
