// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __POLICY_LOAD_H__
#define __POLICY_LOAD_H__

#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "error_codes.h"

class corehost_init_t;
class runtime_config_t;

typedef int(*corehost_load_fn) (const corehost_init_t* init);
typedef int(*corehost_main_fn) (const int argc, const pal::char_t* argv[]);
typedef int(*corehost_unload_fn) ();

class policy_load_t
{
public:
    static int execute_app(
        const pal::string_t& impl_dll_dir,
		const corehost_init_t* init,
        const int argc,
        const pal::char_t* argv[]);

    static bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_host_path = nullptr);

    static int load_host_library(
        const pal::string_t& lib_dir,
        pal::dll_t* h_host,
        corehost_load_fn* load_fn,
        corehost_main_fn* main_fn,
        corehost_unload_fn* unload_fn);
};

#endif // __POLICY_LOAD_H__
