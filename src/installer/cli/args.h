// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef ARGS_H
#define ARGS_H

#include "utils.h"
#include "pal.h"
#include "trace.h"
#include "deps_format.h"
#include "libhost.h"

struct probe_config_t
{
    pal::string_t probe_dir;
    bool match_hash;
    bool roll_forward;
    const deps_json_t* probe_deps_json;

    bool only_runtime_assets;
    bool only_serviceable_assets;

    void print() const
    {
        trace::verbose(_X("probe_config_t: probe=[%s] match-hash=[%d] roll-forward=[%d] deps-json=[%p]"),
            probe_dir.c_str(), match_hash, roll_forward, probe_deps_json);
    }

    probe_config_t(
        const pal::string_t& probe_dir,
        bool match_hash,
        bool roll_forward,
        const deps_json_t* probe_deps_json,
        bool only_serviceable_assets,
        bool only_runtime_assets)
        : probe_dir(probe_dir)
        , match_hash(match_hash)
        , roll_forward(roll_forward)
        , probe_deps_json(probe_deps_json)
        , only_serviceable_assets(only_serviceable_assets)
        , only_runtime_assets(only_runtime_assets)
    {
        // Cannot roll forward and also match hash.
        assert(!roll_forward || !match_hash);
        // Will not roll forward within a deps json.
        assert(!roll_forward || probe_deps_json == nullptr);
        // Will not do hash match when probing a deps json.
        assert(!match_hash || probe_deps_json == nullptr);
    }

    static probe_config_t svc_ni(const pal::string_t dir, bool roll_fwd)
    {
        return probe_config_t(dir, false, roll_fwd, nullptr, true, true);
    }

    static probe_config_t svc(const pal::string_t dir, bool roll_fwd)
    {
        return probe_config_t(dir, false, roll_fwd, nullptr, true, false);
    }

    static probe_config_t cache_ni(const pal::string_t dir)
    {
        return probe_config_t(dir, true, false, nullptr, false, true);
    }
    
    static probe_config_t cache(const pal::string_t dir)
    {
        return probe_config_t(dir, true, false, nullptr, false, false);
    }

    static probe_config_t fx(const pal::string_t dir, const deps_json_t* deps)
    {
        return probe_config_t(dir, false, false, deps, false, false);
    }

    static probe_config_t additional(const pal::string_t dir, bool roll_fwd)
    {
        return probe_config_t(dir, false, roll_fwd, nullptr, false, false);
    }
};

struct arguments_t
{
    pal::string_t own_path;
    pal::string_t app_dir;
    pal::string_t deps_path;
    pal::string_t dotnet_extensions;
    std::vector<pal::string_t> probe_paths;
    pal::string_t dotnet_packages_cache;
    pal::string_t managed_application;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();

    inline void print()
    {
        if (trace::is_enabled())
        {
            trace::verbose(_X("-- arguments_t: own_path=%s app_dir=%s deps=%s extensions=%s packages_cache=%s mgd_app=%s"),
                own_path.c_str(), app_dir.c_str(), deps_path.c_str(), dotnet_extensions.c_str(), dotnet_packages_cache.c_str(), managed_application.c_str());
            for (const auto& probe : probe_paths)
            {
                trace::verbose(_X("-- arguments_t: probe dir: [%s]"), probe.c_str());
            }
        }
    }
};

bool parse_arguments(const pal::string_t& deps_path, const std::vector<pal::string_t>& probe_paths, host_mode_t mode, const int argc, const pal::char_t* argv[], arguments_t* args);

#endif // ARGS_H
