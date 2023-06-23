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
    deps_resolver_t(
        const arguments_t& args,
        const fx_definition_vector_t& fx_definitions,
        const pal::char_t* additional_deps_serialized,
        const std::vector<pal::string_t>& shared_stores,
        const std::vector<pal::string_t>& additional_probe_paths,
        deps_json_t::rid_resolution_options_t rid_resolution_options,
        bool is_framework_dependent)
        : m_fx_definitions(fx_definitions)
        , m_app_dir(args.app_root)
        , m_host_mode(args.host_mode)
        , m_managed_app(args.managed_application)
        , m_is_framework_dependent(is_framework_dependent)
        , m_needs_file_existence_checks(false)
    {
        m_fx_deps.resize(m_fx_definitions.size());
        pal::get_default_servicing_directory(&m_core_servicing);

        // If we are using the RID fallback graph and weren't explicitly given a graph, that of
        // the lowest (root) framework is used for higher frameworks.
        deps_json_t::rid_fallback_graph_t root_rid_fallback_graph;
        if (rid_resolution_options.use_fallback_graph && rid_resolution_options.rid_fallback_graph == nullptr)
        {
            rid_resolution_options.rid_fallback_graph = &root_rid_fallback_graph;
        }

        // Process from lowest (root) to highest (app) framework.
        int lowest_framework = static_cast<int>(m_fx_definitions.size()) - 1;
        for (int i = lowest_framework; i >= 0; --i)
        {
            pal::string_t deps_file = i == 0
                ? args.deps_path
                : get_fx_deps(m_fx_definitions[i]->get_dir(), m_fx_definitions[i]->get_name());
            trace::verbose(_X("Using %s deps file"), deps_file.c_str());

            // Parse as framework-dependent if we are not the lowest framework or if there is only one
            // framework, but framework-dependent is specified (for example, components)
            if (i != lowest_framework || (lowest_framework == 0 && m_is_framework_dependent))
            {
                m_fx_deps[i] = deps_json_t::create_for_framework_dependent(deps_file, rid_resolution_options);
            }
            else
            {
                m_fx_deps[i] = deps_json_t::create_for_self_contained(deps_file, rid_resolution_options);
            }
        }

        resolve_additional_deps(additional_deps_serialized, rid_resolution_options);

        setup_probe_config(shared_stores, additional_probe_paths);
    }

    bool valid(pal::string_t* errors)
    {
        for (size_t i = 0; i < m_fx_deps.size(); ++i)
        {
            // Verify the deps file exists. The app deps file does not need to exist
            if (i != 0)
            {
                if (!m_fx_deps[i]->exists())
                {
                    errors->assign(_X("A fatal error was encountered, missing dependencies manifest at: ") + m_fx_deps[i]->get_deps_file());
                    return false;
                }
            }

            if (!m_fx_deps[i]->is_valid())
            {
                errors->assign(_X("An error occurred while parsing: ") + m_fx_deps[i]->get_deps_file());
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

    pal::string_t get_lookup_probe_directories();

    bool resolve_probe_paths(
        probe_paths_t* probe_paths,
        std::unordered_set<pal::string_t>* breadcrumb,
        bool ignore_missing_assemblies = false);

    const deps_json_t& get_root_deps() const
    {
        return *m_fx_deps[m_fx_definitions.size() - 1];
    }

    void enum_app_context_deps_files(std::function<void(const pal::string_t&)> callback);

    bool is_framework_dependent() const
    {
        return m_is_framework_dependent;
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

public: // static
    static pal::string_t get_fx_deps(const pal::string_t& fx_dir, const pal::string_t& fx_name)
    {
        pal::string_t fx_deps = fx_dir;
        pal::string_t fx_deps_name = fx_name + _X(".deps.json");
        append_path(&fx_deps, fx_deps_name.c_str());
        return fx_deps;
    }

private:
    void setup_shared_store_probes(
        const std::vector<pal::string_t>& shared_stores);

    void setup_probe_config(
        const std::vector<pal::string_t>& shared_stores,
        const std::vector<pal::string_t>& additional_probe_paths);

    void init_known_entry_path(
        const deps_entry_t& entry,
        const pal::string_t& path);

    void resolve_additional_deps(
        const pal::char_t* additional_deps_serialized,
        const deps_json_t::rid_resolution_options_t& rid_resolution_options);

    const deps_json_t& get_app_deps() const
    {
        return *m_fx_deps[0];
    }

private:
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

    // Probe entry in probe configurations and deps dir.
    bool probe_deps_entry(
        const deps_entry_t& entry,
        const pal::string_t& deps_dir,
        int fx_level,
        pal::string_t* candidate,
        bool &found_in_bundle);

private:
    const fx_definition_vector_t& m_fx_definitions;

    // Resolved deps.json for each m_fx_definitions (corresponding indices)
    std::vector<std::unique_ptr<deps_json_t>> m_fx_deps;

    pal::string_t m_app_dir;

    // Mode in which the host is being run. This can dictate how dependencies should be discovered.
    const host_mode_t m_host_mode;

    // The managed application the dependencies are being resolved for.
    pal::string_t m_managed_app;

    // Servicing root, could be empty on platforms that don't support or when errors occur.
    pal::string_t m_core_servicing;

    // Special entry for coreclr path
    pal::string_t m_coreclr_path;

    // Custom deps files for the app
    std::vector< std::unique_ptr<deps_json_t> > m_additional_deps;

    // Various probe configurations.
    std::vector<probe_config_t> m_probes;

    // Is the deps file for an app using shared frameworks?
    const bool m_is_framework_dependent;

    // File existence checks must be performed for probed paths.This will cause symlinks to be resolved.
    bool m_needs_file_existence_checks;
};

#endif // DEPS_RESOLVER_H
