// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef DEPS_RESOLVER_H
#define DEPS_RESOLVER_H

#include <vector>

#include "pal.h"
#include "trace.h"
#include "deps_format.h"
#include "deps_entry.h"
#include "servicing_index.h"
#include "runtime_config.h"

// Probe paths to be resolved for ordering
struct probe_paths_t
{
    pal::string_t tpa;
    pal::string_t native;
    pal::string_t resources;
};

class deps_resolver_t
{
public:
    deps_resolver_t(const pal::string_t& fx_dir, const runtime_config_t* config, const arguments_t& args)
        : m_svc(args.dotnet_servicing)
        , m_fx_dir(fx_dir)
        , m_coreclr_index(-1)
        , m_portable(config->get_portable())
        , m_deps(nullptr)
        , m_fx_deps(nullptr)
    {
        m_deps_file = args.deps_path;
        if (m_portable)
        {
            m_fx_deps_file = get_fx_deps(fx_dir, config->get_fx_name());
            m_fx_deps = std::unique_ptr<deps_json_t>(new deps_json_t(false, m_fx_deps_file));
            m_deps = std::unique_ptr<deps_json_t>(new deps_json_t(true, m_deps_file, m_fx_deps->get_rid_fallback_graph()));
        }
        else
        {
            m_deps = std::unique_ptr<deps_json_t>(new deps_json_t(false, m_deps_file));
        }
    }


    bool valid() { return m_deps->is_valid() && (!m_portable || m_fx_deps->is_valid());  }

    bool resolve_probe_paths(
      const pal::string_t& app_dir,
      const pal::string_t& package_dir,
      const pal::string_t& package_cache_dir,
      const pal::string_t& clr_dir,
      probe_paths_t* probe_paths);

    pal::string_t resolve_coreclr_dir(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir);

    const pal::string_t& get_fx_deps_file() const
    {
        return m_fx_deps_file;
    }
    
    const pal::string_t& get_deps_file() const
    {
        return m_deps_file;
    }
private:

    static pal::string_t get_fx_deps(const pal::string_t& fx_dir, const pal::string_t& fx_name)
    {
        pal::string_t fx_deps = fx_dir;
        pal::string_t fx_deps_name = pal::to_lower(fx_name) + _X(".deps.json");
        append_path(&fx_deps, fx_deps_name.c_str());
        return fx_deps;
    }

    // Resolve order for TPA lookup.
    void resolve_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Resolve order for culture and native DLL lookup.
    void resolve_probe_dirs(
        const pal::string_t& asset_type,
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Populate assemblies from the directory.
    void get_dir_assemblies(
        const pal::string_t& dir,
        const pal::string_t& dir_name,
        std::unordered_map<pal::string_t, pal::string_t>* dir_assemblies);

    // Servicing index to resolve serviced assembly paths.
    servicing_index_t m_svc;

    // Framework deps file.
    pal::string_t m_fx_dir;

    // Map of simple name -> full path of local/fx assemblies populated
    // in priority order of their extensions.
    std::unordered_map<pal::string_t, pal::string_t> m_sxs_assemblies;

    // Special entry for coreclr in the deps entries
    int m_coreclr_index;

    // The filepath for the app deps
    pal::string_t m_deps_file;
    
    // The filepath for the fx deps
    pal::string_t m_fx_deps_file;

    // Deps files for the fx
    std::unique_ptr<deps_json_t> m_fx_deps;

    // Deps files for the app
    std::unique_ptr<deps_json_t>  m_deps;

    // Is the deps file valid
    bool m_deps_valid;

    // Is the deps file portable app?
    bool m_portable;
};

#endif // DEPS_RESOLVER_H
