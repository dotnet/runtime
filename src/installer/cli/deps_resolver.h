// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef DEPS_RESOLVER_H
#define DEPS_RESOLVER_H

#include <vector>

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "deps_format.h"
#include "deps_entry.h"
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
    deps_resolver_t(const corehost_init_t* init, const runtime_config_t& config, const arguments_t& args)
        // Important: FX dir should come from "init" than "config",
        //            since the host could be launching from FX dir.
        : m_fx_dir(init->fx_dir())
        , m_app_dir(args.app_dir)
        , m_coreclr_index(-1)
        , m_portable(config.get_portable())
        , m_deps(nullptr)
        , m_fx_deps(nullptr)
    {
        m_deps_file = args.deps_path;
        if (m_portable)
        {
            m_fx_deps_file = get_fx_deps(m_fx_dir, config.get_fx_name());
            trace::verbose(_X("Using %s FX deps file"), m_fx_deps_file.c_str());
            trace::verbose(_X("Using %s deps file"), m_deps_file.c_str());
            m_fx_deps = std::unique_ptr<deps_json_t>(new deps_json_t(false, m_fx_deps_file));
            m_deps = std::unique_ptr<deps_json_t>(new deps_json_t(true, m_deps_file, m_fx_deps->get_rid_fallback_graph()));
        }
        else
        {
            m_deps = std::unique_ptr<deps_json_t>(new deps_json_t(false, m_deps_file));
        }

        setup_additional_probes(args.probe_paths);
        setup_probe_config(init, config, args);
    }

    bool valid() { return m_deps->is_valid() && (!m_portable || m_fx_deps->is_valid());  }

    void setup_probe_config(
        const corehost_init_t* init,
        const runtime_config_t& config,
        const arguments_t& args);

    void setup_additional_probes(const std::vector<pal::string_t>& probe_paths);

    bool resolve_probe_paths(
      const pal::string_t& clr_dir,
      probe_paths_t* probe_paths);

    pal::string_t resolve_coreclr_dir();

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
        pal::string_t fx_deps_name = fx_name + _X(".deps.json");
        append_path(&fx_deps, fx_deps_name.c_str());
        return fx_deps;
    }

    // Resolve order for TPA lookup.
    void resolve_tpa_list(
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Resolve order for culture and native DLL lookup.
    void resolve_probe_dirs(
        deps_entry_t::asset_types asset_type,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Populate assemblies from the directory.
    void get_dir_assemblies(
        const pal::string_t& dir,
        const pal::string_t& dir_name,
        std::unordered_map<pal::string_t, pal::string_t>* dir_assemblies);

    // Probe entry in probe configurations.
    bool probe_entry_in_configs(
        const deps_entry_t& entry,
        pal::string_t* candidate);

    // Try auto roll forward, if not return entry in probe dir.
    bool try_roll_forward(
        const deps_entry_t& entry,
        const pal::string_t& probe_dir,
        bool patch_roll_fwd,
        bool prerelease_roll_fwd,
        pal::string_t* candidate);

    // Framework deps file.
    pal::string_t m_fx_dir;

    pal::string_t m_app_dir;

    // Map of simple name -> full path of local/fx assemblies populated
    // in priority order of their extensions.
    typedef std::unordered_map<pal::string_t, pal::string_t> dir_assemblies_t;
    dir_assemblies_t m_local_assemblies;
    dir_assemblies_t m_fx_assemblies;

    std::unordered_map<pal::string_t, pal::string_t> m_patch_roll_forward_cache;
    std::unordered_map<pal::string_t, pal::string_t> m_prerelease_roll_forward_cache;

    pal::string_t m_package_cache;

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

    // Various probe configurations.
    std::vector<probe_config_t> m_probes;

    // Is the deps file valid
    bool m_deps_valid;

    // Fallback probe dir
    std::vector<pal::string_t> m_additional_probes;

    // Is the deps file portable app?
    bool m_portable;
};

#endif // DEPS_RESOLVER_H
