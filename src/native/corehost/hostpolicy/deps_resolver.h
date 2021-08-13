// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DEPS_RESOLVER_H
#define DEPS_RESOLVER_H

#include <vector>

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "fx_definition.h"
#include "deps_format.h"
#include "deps_entry.h"
#include "runtime_config.h"
#include "bundle/runner.h"

// Probe paths to be resolved for ordering
struct probe_paths_t
{
    pal::string_t tpa;
    pal::string_t native;
    pal::string_t resources;
    pal::string_t coreclr;
};

struct deps_resolved_asset_t
{
    deps_resolved_asset_t(const deps_asset_t& asset, const pal::string_t& resolved_path)
        : asset(asset)
        , resolved_path(resolved_path) { }

    deps_asset_t asset;
    pal::string_t resolved_path;
};

typedef std::unordered_map<pal::string_t, deps_resolved_asset_t> name_to_resolved_asset_map_t;

class deps_resolver_t
{
public:
    // if root_framework_rid_fallback_graph is specified it is assumed that the fx_definitions
    // doesn't contain the root framework at all.
    deps_resolver_t(
        const arguments_t& args,
        fx_definition_vector_t& fx_definitions,
        const deps_json_t::rid_fallback_graph_t* root_framework_rid_fallback_graph,
        bool is_framework_dependent)
        : m_fx_definitions(fx_definitions)
        , m_app_dir(args.app_root)
        , m_host_mode(args.host_mode)
        , m_managed_app(args.managed_application)
        , m_core_servicing(args.core_servicing)
        , m_is_framework_dependent(is_framework_dependent)
        , m_needs_file_existence_checks(false)
    {
        int lowest_framework = static_cast<int>(m_fx_definitions.size()) - 1;
        int root_framework = -1;
        if (root_framework_rid_fallback_graph == nullptr)
        {
            root_framework = lowest_framework;
            root_framework_rid_fallback_graph = &m_fx_definitions[root_framework]->get_deps().get_rid_fallback_graph();
        }

        for (int i = lowest_framework; i >= 0; --i)
        {
            if (i == 0)
            {
                m_fx_definitions[i]->set_deps_file(args.deps_path);
                trace::verbose(_X("Using %s deps file"), m_fx_definitions[i]->get_deps_file().c_str());
            }
            else
            {
                pal::string_t fx_deps_file = get_fx_deps(m_fx_definitions[i]->get_dir(), m_fx_definitions[i]->get_name());
                m_fx_definitions[i]->set_deps_file(fx_deps_file);
                trace::verbose(_X("Using Fx %s deps file"), fx_deps_file.c_str());
            }

            if (i == root_framework)
            {
                m_fx_definitions[i]->parse_deps();
            }
            else
            {
                // The rid graph is obtained from the root framework
                m_fx_definitions[i]->parse_deps(*root_framework_rid_fallback_graph);
            }
        }

        resolve_additional_deps(args, *root_framework_rid_fallback_graph);

        setup_additional_probes(args.probe_paths);
        setup_probe_config(args);

        if (m_additional_deps.size() > 0)
        {
            m_needs_file_existence_checks = true;
        }
    }

    bool valid(pal::string_t* errors)
    {
        for (size_t i = 0; i < m_fx_definitions.size(); ++i)
        {
            // Verify the deps file exists. The app deps file does not need to exist
            if (i != 0)
            {
                if (!m_fx_definitions[i]->get_deps().exists())
                {
                    errors->assign(_X("A fatal error was encountered, missing dependencies manifest at: ") + m_fx_definitions[i]->get_deps_file());
                    return false;
                }
            }

            if (!m_fx_definitions[i]->get_deps().is_valid())
            {
                errors->assign(_X("An error occurred while parsing: ") + m_fx_definitions[i]->get_deps_file());
                return false;
            }
        }

        for (const auto& additional_deps : m_additional_deps)
        {
            if (!additional_deps->is_valid())
            {
                errors->assign(_X("An error occurred while parsing: ") + additional_deps->get_deps_file());
                return false;
            }
        }

        errors->clear();
        return true;
    }

    void setup_shared_store_probes(
        const arguments_t& args);

    pal::string_t get_lookup_probe_directories();

    void setup_probe_config(
        const arguments_t& args);

    void setup_additional_probes(
        const std::vector<pal::string_t>& probe_paths);

    bool resolve_probe_paths(
        probe_paths_t* probe_paths,
        std::unordered_set<pal::string_t>* breadcrumb,
        bool ignore_missing_assemblies = false);

    void init_known_entry_path(
        const deps_entry_t& entry,
        const pal::string_t& path);

    void resolve_additional_deps(
        const arguments_t& args,
        const deps_json_t::rid_fallback_graph_t& rid_fallback_graph);

    const deps_json_t& get_deps() const
    {
        return get_app(m_fx_definitions).get_deps();
    }

    const pal::string_t& get_deps_file() const
    {
        return get_app(m_fx_definitions).get_deps_file();
    }

    void get_app_context_deps_files_range(fx_definition_vector_t::iterator *begin, fx_definition_vector_t::iterator *end) const;

    const fx_definition_vector_t& get_fx_definitions() const
    {
        return m_fx_definitions;
    }

    bool is_framework_dependent() const
    {
        return m_is_framework_dependent;
    }

    bool needs_file_existence_checks() const
    {
        return m_needs_file_existence_checks;
    }

    void get_app_dir(pal::string_t *app_dir) const
    {
        if (m_host_mode == host_mode_t::libhost)
        {
            static const pal::string_t s_empty;
            *app_dir = s_empty;
            return;
        }
        *app_dir = m_app_dir;
        if (m_host_mode == host_mode_t::apphost)
        {
            if (bundle::info_t::is_single_file_bundle())
            {
                const bundle::runner_t* app = bundle::runner_t::app();
                if (app->is_netcoreapp3_compat_mode())
                {
                    *app_dir = app->extraction_path();
                }
            }
        }

        // Make sure the path ends with a directory separator
        // This has been the behavior for a long time, and we should make it consistent
        // for all cases.
        if (app_dir->back() != DIR_SEPARATOR)
        {
            app_dir->append(1, DIR_SEPARATOR);
        }
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
        std::unordered_set<pal::string_t>* breadcrumb,
        bool ignore_missing_assemblies);

    // Resolve order for culture and native DLL lookup.
    bool resolve_probe_dirs(
        deps_entry_t::asset_types asset_type,
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb);

    // Populate assemblies from the directory.
    void get_dir_assemblies(
        const pal::string_t& dir,
        const pal::string_t& dir_name,
        name_to_resolved_asset_map_t* items);

    // Probe entry in probe configurations and deps dir.
    bool probe_deps_entry(
        const deps_entry_t& entry,
        const pal::string_t& deps_dir,
        int fx_level,
        pal::string_t* candidate,
        bool &found_in_bundle);

    fx_definition_vector_t& m_fx_definitions;

    pal::string_t m_app_dir;

    void add_tpa_asset(
        const deps_resolved_asset_t& asset,
        name_to_resolved_asset_map_t* items);

    // Mode in which the host is being run. This can dictate how dependencies should be discovered.
    const host_mode_t m_host_mode;

    // The managed application the dependencies are being resolved for.
    pal::string_t m_managed_app;

    // Servicing root, could be empty on platforms that don't support or when errors occur.
    pal::string_t m_core_servicing;

    // Special entry for coreclr path
    pal::string_t m_coreclr_path;

    // The filepaths for the app custom deps
    std::vector<pal::string_t> m_additional_deps_files;

    // Custom deps files for the app
    std::vector< std::unique_ptr<deps_json_t> > m_additional_deps;

    // Various probe configurations.
    std::vector<probe_config_t> m_probes;

    // Fallback probe dir
    std::vector<pal::string_t> m_additional_probes;

    // Is the deps file for an app using shared frameworks?
    const bool m_is_framework_dependent;

    // File existence checks must be performed for probed paths.This will cause symlinks to be resolved.
    bool m_needs_file_existence_checks;
};

#endif // DEPS_RESOLVER_H
