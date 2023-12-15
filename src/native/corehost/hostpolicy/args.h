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
    enum class type
    {
        servicing,
        app,
        framework,
        lookup,
    };

    type probe_type;

    pal::string_t probe_dir;

    // Only for type::framework
    const deps_json_t* probe_deps_json;
    int fx_level;

    bool only_runtime_assets;

    pal::string_t as_str() const
    {
        pal::string_t details = _X("type=");
        switch (probe_type)
        {
        case type::servicing:
            details += _X("servicing");
            break;
        case type::app:
            details += _X("app");
            break;
        case type::framework:
            details += _X("framework");
            break;
        case type::lookup:
            details += _X("lookup");
            break;
        default:
            assert(false && "Unknown probe config type");
            return _X("");
        }

        if (!probe_dir.empty())
            details += _X(" dir=[") + probe_dir + _X("]");

        if (fx_level != -1)
            details += _X(" fx_level=") + pal::to_string(fx_level);

        return details;
    }

    probe_config_t(type probe_type, const pal::string_t& probe_dir)
        : probe_type(probe_type)
        , probe_dir(probe_dir)
        , probe_deps_json(nullptr)
        , fx_level(-1)
        , only_runtime_assets(false)
    { }

    bool is_lookup() const
    {
        return probe_type == type::lookup;
    }

    bool is_fx() const
    {
        return probe_type == type::framework;
    }

    bool is_app() const
    {
        return probe_type == type::app;
    }

    bool is_servicing() const
    {
        return probe_type == type::servicing;
    }

    static probe_config_t svc_ni(const pal::string_t& dir)
    {
        probe_config_t config(type::servicing, dir);
        config.only_runtime_assets = true;
        return config;
    }

    static probe_config_t svc(const pal::string_t& dir)
    {
        return probe_config_t(type::servicing, dir);
    }

    static probe_config_t fx(const pal::string_t& dir, const deps_json_t* deps, int fx_level)
    {
        assert(fx_level > 0);
        probe_config_t config(type::framework, dir);
        config.probe_deps_json = deps;
        config.fx_level = fx_level;
        return config;
    }

    static probe_config_t lookup(const pal::string_t& dir)
    {
        return probe_config_t(type::lookup, dir);
    }

    static probe_config_t published_deps_dir()
    {
        return probe_config_t(type::app, _X(""));
    }
};

struct arguments_t
{
    host_mode_t host_mode;
    pal::string_t app_root;
    pal::string_t deps_path;
    pal::string_t managed_application;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();

    inline void trace()
    {
        if (trace::is_enabled())
        {
            trace::verbose(_X("-- arguments_t: app_root='%s' deps='%s' mgd_app='%s'"),
                app_root.c_str(), deps_path.c_str(), managed_application.c_str());
        }
    }
};

bool parse_arguments(
    const hostpolicy_init_t& init,
    const int argc, const pal::char_t* argv[],
    arguments_t& arg);
bool init_arguments(
    const pal::string_t& managed_application_path,
    host_mode_t host_mode,
    const pal::string_t& deps_file,
    bool init_from_file_system,
    arguments_t& args);

#endif // ARGS_H
