// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef ARGS_H
#define ARGS_H

#include "utils.h"
#include "pal.h"
#include "trace.h"
#include "libhost.h"

struct arguments_t
{
    pal::string_t own_path;
    pal::string_t app_dir;
    pal::string_t deps_path;
    pal::string_t dotnet_servicing;
    pal::string_t probe_dir;
    pal::string_t dotnet_packages_cache;
    pal::string_t managed_application;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();

    inline void print()
    {
        if (trace::is_enabled())
        {
            trace::verbose(_X("args: own_path=%s app_dir=%s deps=%s servicing=%s probe_dir=%s packages_cache=%s mgd_app=%s"),
                own_path.c_str(), app_dir.c_str(), deps_path.c_str(), dotnet_servicing.c_str(), probe_dir.c_str(), dotnet_packages_cache.c_str(), managed_application.c_str());
        }
    }
};

bool parse_arguments(const pal::string_t& deps_path, const pal::string_t& probe_dir, host_mode_t mode, const int argc, const pal::char_t* argv[], arguments_t* args);

#endif // ARGS_H
