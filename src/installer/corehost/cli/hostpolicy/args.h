// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ARGS_H
#define ARGS_H

#include "utils.h"
#include "pal.h"
#include "trace.h"
#include "deps_format.h"
#include "hostpolicy_init.h"

struct probe_config_t
{
    pal::string_t probe_dir;
    const deps_json_t* probe_deps_json;
    int fx_level;

    bool only_runtime_assets;
    bool only_serviceable_assets;

    bool probe_publish_dir;

    void print() const
    {
        trace::verbose(_X("probe_config_t: probe=[%s] deps-dir-probe=[%d]"),
            probe_dir.c_str(), probe_publish_dir);
    }

    probe_config_t(
        const pal::string_t& probe_dir,
        const deps_json_t* probe_deps_json,
        int fx_level,
        bool only_serviceable_assets,
        bool only_runtime_assets,
        bool probe_publish_dir)
        : probe_dir(probe_dir)
        , probe_deps_json(probe_deps_json)
        , fx_level(fx_level)
        , only_runtime_assets(only_runtime_assets)
        , only_serviceable_assets(only_serviceable_assets)
        , probe_publish_dir(probe_publish_dir)
    {
    }

    bool is_lookup() const
    {
        return (probe_deps_json == nullptr) &&
            !only_runtime_assets &&
            !only_serviceable_assets &&
            !probe_publish_dir;
    }

    bool is_fx() const
    {
        return (probe_deps_json != nullptr);
    }

    bool is_app() const
    {
        return probe_publish_dir;
    }

    static probe_config_t svc_ni(const pal::string_t& dir)
    {
        return probe_config_t(dir, nullptr, -1, true, true, false);
    }

    static probe_config_t svc(const pal::string_t& dir)
    {
        return probe_config_t(dir, nullptr, -1, true, false, false);
    }

    static probe_config_t fx(const pal::string_t& dir, const deps_json_t* deps, int fx_level)
    {
        return probe_config_t(dir, deps, fx_level, false, false, false);
    }

    static probe_config_t lookup(const pal::string_t& dir)
    {
        return probe_config_t(dir, nullptr, -1, false, false, false);
    }

    static probe_config_t published_deps_dir()
    {
        return probe_config_t(_X(""), nullptr, 0, false, false, true);
    }
};

struct arguments_t
{
    host_mode_t host_mode;
    pal::string_t host_path;
    pal::string_t app_root;
    pal::string_t deps_path;
    pal::string_t core_servicing;
    std::vector<pal::string_t> probe_paths;
    pal::string_t managed_application;
    std::vector<pal::string_t> global_shared_stores;
    pal::string_t dotnet_shared_store;
    std::vector<pal::string_t> env_shared_store;
    pal::string_t additional_deps_serialized;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();

    inline void trace()
    {
        if (trace::is_enabled())
        {
            trace::verbose(_X("-- arguments_t: host_path='%s' app_root='%s' deps='%s' core_svc='%s' mgd_app='%s'"),
                host_path.c_str(), app_root.c_str(), deps_path.c_str(), core_servicing.c_str(), managed_application.c_str());
            for (const auto& probe : probe_paths)
            {
                trace::verbose(_X("-- arguments_t: probe dir: '%s'"), probe.c_str());
            }
            for (const auto& shared : env_shared_store)
            {
                trace::verbose(_X("-- arguments_t: env shared store: '%s'"), shared.c_str());
            }
            trace::verbose(_X("-- arguments_t: dotnet shared store: '%s'"), dotnet_shared_store.c_str());
            for (const auto& global_shared : global_shared_stores)
            {
                trace::verbose(_X("-- arguments_t: global shared store: '%s'"), global_shared.c_str());
            }
        }
    }
};

bool parse_arguments(
    const hostpolicy_init_t& init,
    const int argc, const pal::char_t* argv[],
    arguments_t& arg);
bool init_arguments(
    const pal::string_t& managed_application_path,
    const host_startup_info_t& host_info,
    const pal::string_t& tfm,
    host_mode_t host_mode,
    const pal::string_t& additional_deps_serialized,
    const pal::string_t& deps_file,
    const std::vector<pal::string_t>& probe_paths,
    bool init_from_file_system,
    arguments_t& args);

#endif // ARGS_H
