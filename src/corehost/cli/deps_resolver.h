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
    pal::string_t coreclr;
    pal::string_t clrjit;
};

class deps_resolver_t
{
public:
    deps_resolver_t(const hostpolicy_init_t& init, const arguments_t& args)
        : m_fx_dir(init.fx_dir)
        , m_app_dir(args.app_dir)
        , m_managed_app(args.managed_application)
        , m_portable(init.is_portable)
        , m_deps(nullptr)
        , m_fx_deps(nullptr)
        , m_core_servicing(args.core_servicing)
    {
        m_deps_file = args.deps_path;
        if (m_portable)
        {
            m_fx_deps_file = get_fx_deps(m_fx_dir, init.fx_name);
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
        setup_probe_config(init, args);
    }

    bool valid(pal::string_t* errors)
    {
        if (!m_deps->is_valid())
        {
            errors->assign(_X("An error occurred while parsing ") + m_deps_file);
            return false;
        }
        if (m_portable && !m_fx_deps->exists())
        {
            errors->assign(_X("A fatal error was encountered, missing dependencies manifest at: ") + m_fx_deps_file);
            return false;
        }
        if (m_portable && !m_fx_deps->is_valid())
        {
            errors->assign(_X("An error occurred while parsing ") + m_fx_deps_file);
            return false;
        }
        errors->clear();
        return true;
    }

    void setup_shared_package_probes(
        const hostpolicy_init_t& init,
        const arguments_t& args);

    void setup_probe_config(
        const hostpolicy_init_t& init,
        const arguments_t& args);

    void setup_additional_probes(
        const std::vector<pal::string_t>& probe_paths);

    bool resolve_probe_paths(
        probe_paths_t* probe_paths,
        std::unordered_set<pal::string_t>* breadcrumb);

    void init_known_entry_path(
        const deps_entry_t& entry,
        const pal::string_t& path);

    const pal::string_t& get_fx_deps_file() const
    {
        return m_fx_deps_file;
    }
    
    const pal::string_t& get_deps_file() const
    {
        return m_deps_file;
    }

    const std::unordered_set<pal::string_t>& get_api_sets() const
    {
        return m_api_set_paths;
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
    bool resolve_tpa_list(
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb);

    // Resolve order for culture and native DLL lookup.
    bool resolve_probe_dirs(
        deps_entry_t::asset_types asset_type,
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb);

    // Populate assemblies from the directory.
    void get_dir_assemblies(
        const pal::string_t& dir,
        const pal::string_t& dir_name,
        std::unordered_map<pal::string_t, pal::string_t>* dir_assemblies);

    // Probe entry in probe configurations and deps dir.
    bool probe_deps_entry(
        const deps_entry_t& entry,
        const pal::string_t& deps_dir,
        pal::string_t* candidate);

    // Framework deps file.
    pal::string_t m_fx_dir;

    pal::string_t m_app_dir;

    // Map of simple name -> full path of local/fx assemblies populated
    // in priority order of their extensions.
    typedef std::unordered_map<pal::string_t, pal::string_t> dir_assemblies_t;

    std::unordered_map<pal::string_t, pal::string_t> m_patch_roll_forward_cache;
    std::unordered_map<pal::string_t, pal::string_t> m_prerelease_roll_forward_cache;

    pal::string_t m_package_cache;

    // The managed application the dependencies are being resolved for.
    pal::string_t m_managed_app;

    // Servicing root, could be empty on platforms that don't support or when errors occur.
    pal::string_t m_core_servicing;

    // Special entry for api-sets
    std::unordered_set<pal::string_t> m_api_set_paths;

    // Special entry for coreclr path
    pal::string_t m_coreclr_path;

    // Special entry for JIT path
    pal::string_t m_clrjit_path;

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
