// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "shared_store.h"
#include <trace.h>
#include <utils.h>

#define RUNTIME_STORE_DIRECTORY_NAME _X("store")
#define SHARED_STORE_ENV _X("DOTNET_SHARED_STORE")

namespace
{
    void get_env_dirs(std::vector<pal::string_t>& dirs, const pal::char_t* arch, const pal::string_t& tfm)
    {
        pal::string_t path;
        if (!pal::getenv(SHARED_STORE_ENV, &path))
            return;

        pal::string_t tok;
        pal::stringstream_t ss(path);
        while (std::getline(ss, tok, PATH_SEPARATOR))
        {
            if (pal::fullpath(&tok))
            {
                append_path(&tok, arch);
                append_path(&tok, tfm.c_str());
                dirs.push_back(tok);

                trace::verbose(_X("Shared store (%s): '%s'"), SHARED_STORE_ENV, tok.c_str());
            }
        }
    }

    void get_global_dirs(std::vector<pal::string_t>& dirs, const pal::char_t* arch, const pal::string_t& tfm, const pal::string_t& dir_to_skip)
    {
        std::vector<pal::string_t> global_dirs;
        if (!pal::get_global_dotnet_dirs(&global_dirs))
            return;

        for (pal::string_t dir : global_dirs)
        {
            append_path(&dir, RUNTIME_STORE_DIRECTORY_NAME);
            append_path(&dir, arch);
            append_path(&dir, tfm.c_str());
            if (!dir_to_skip.empty() && pal::are_paths_equal_with_normalized_casing(dir, dir_to_skip))
                continue;

            dirs.push_back(dir);
            trace::verbose(_X("Shared store (%s): '%s'"), _X("global"), dir.c_str());
        }
    }
}

/**
 * Get the shared store directories.
 *
 *  - DOTNET_SHARED_STORE environment variable - multiple delimited paths + <arch>\<tfm>
 *  - dotnet.exe relative shared store\<arch>\<tfm>
 *  - Global location
 *      Windows: global default location (Program Files) or globally registered location (registry) + store\<arch>\<tfm>
 *      Linux/macOS: none (no global locations are considered)
 */
std::vector<pal::string_t> shared_store::get_paths(const pal::string_t& tfm, host_mode_t host_mode, const pal::string_t& host_path)
{
    std::vector<pal::string_t> shared_stores;

    // Old (MNA < 1.1.*) "runtimeconfig.json" files do not contain TFM property.
    if (tfm.empty())
        return shared_stores;

    const pal::char_t* arch = get_current_arch_name();

    // Environment variable DOTNET_SHARED_STORE
    get_env_dirs(shared_stores, arch, tfm);

    // "dotnet.exe" relative shared store folder
    pal::string_t dotnet_shared_store;
    if (host_mode == host_mode_t::muxer)
    {
        dotnet_shared_store = get_directory(host_path);
        append_path(&dotnet_shared_store, RUNTIME_STORE_DIRECTORY_NAME);
        append_path(&dotnet_shared_store, arch);
        append_path(&dotnet_shared_store, tfm.c_str());
        shared_stores.push_back(dotnet_shared_store);
        trace::verbose(_X("Shared store (%s): '%s'"), _X("dotnet"), dotnet_shared_store.c_str());
    }

    // Global shared store dir
    bool multilevel_lookup = multilevel_lookup_enabled();
    if (multilevel_lookup)
    {
        get_global_dirs(shared_stores, arch, tfm, dotnet_shared_store);
    }

    return shared_stores;
}
