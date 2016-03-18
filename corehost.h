// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "libhost.h"
#include "policy_load.h"

typedef int(*hostfxr_main_fn) (const int argc, const pal::char_t* argv[]);

class corehost_t
{
public:

    int run(const int argc, const pal::char_t* argv[]);
    static int execute_app(
        const pal::string_t& policy_dir,
        const pal::string_t& fx_dir,
        const runtime_config_t* config,
        const int argc,
        const pal::char_t* argv[]);

private:

    static int load_host_library(
        const pal::string_t& lib_dir,
        pal::dll_t* h_host,
        corehost_load_fn* load_fn,
        corehost_main_fn* main_fn,
        corehost_unload_fn* unload_fn);

    pal::string_t resolve_fxr_path(const pal::string_t& own_dir);
    int resolve_fx_and_execute_app(const pal::string_t& own_dir, const int argc, const pal::char_t* argv[]);

    static bool hostpolicy_exists_in_svc(pal::string_t* resolved_path);
    static bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_host_path);
};
