// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOST_STARTUP_INFO_H_
#define __HOST_STARTUP_INFO_H_

#include "pal.h"
#include "host_interface.h"

struct host_startup_info_t
{
    host_startup_info_t() {}

    host_startup_info_t(
        const pal::char_t* host_path_value,
        const pal::char_t* dotnet_root_value,
        const pal::char_t* app_path_value);

    void parse(
        int argc,
        const pal::char_t* argv[]);

    bool is_valid(host_mode_t mode) const;

    const pal::string_t get_app_name() const;

    static int get_host_path(int argc, const pal::char_t* argv[], pal::string_t* host_path);

    pal::string_t host_path;    // The path to the current hosting binary.
    pal::string_t dotnet_root;  // The path to the framework.
    pal::string_t app_path;     // For apphost, the path to the app dll; for muxer, not applicable as this information is not yet parsed.
};

#endif // __HOST_STARTUP_INFO_H_
