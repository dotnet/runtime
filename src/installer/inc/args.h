// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef ARGS_H
#define ARGS_H

#include "pal.h"
#include "trace.h"

struct arguments_t
{
    pal::string_t own_path;
    pal::string_t managed_application;
    pal::string_t clr_path;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();
};

bool parse_arguments(const int argc, const pal::char_t* argv[], arguments_t& args);

#endif // ARGS_H
