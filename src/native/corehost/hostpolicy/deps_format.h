// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DEPS_FORMAT_H_
#define __DEPS_FORMAT_H_

#include <iostream>
#include <vector>
#include <unordered_set>
#include <functional>
#include "pal.h"
#include "deps_entry.h"
#include "json_parser.h"

class deps_json_t
{
    typedef std::vector<deps_asset_t> vec_asset_t;
    typedef std::array<vec_asset_t, deps_entry_t::asset_types::count> assets_t;
    struct deps_assets_t { std::unordered_map<pal::string_t, assets_t> libs; };
    struct rid_assets_t { std::unordered_map<pal::string_t, vec_asset_t> rid_assets; };
    typedef std::array<rid_assets_t, deps_entry_t::asset_types::count> rid_assets_per_type_t;
    struct rid_specific_assets_t { std::unordered_map<pal::string_t, rid_assets_per_type_t> libs; };

    typedef std::unordered_map<pal::string_t, std::vector<pal::string_t>> str_to_vector_map_t;

public:
    typedef str_to_vector_map_t rid_fallback_graph_t;

    struct rid_resolution_options_t
    {
        // Whether or not the RID fallback graph should be used
        // For framework-dependent, this indicates whether rid_fallback_graph should be used to filter
        // RID-specific assets. For self-contained, this indicates whether the RID graph should be read
        // from the deps file.
        bool use_fallback_graph;

        // The RID fallback graph to use
        deps_json_t::rid_fallback_graph_t* rid_fallback_graph;
    };

    const std::vector<deps_entry_t>& get_entries(deps_entry_t::asset_types type) const
    {
        assert(type < deps_entry_t::asset_types::count);
        return m_deps_entries[type];
    }

    bool has_package(const pal::string_t& name, const pal::string_t& ver) const;

    bool exists() const
    {
        return m_file_exists;
    }

    bool is_valid() const
    {
        return m_valid;
    }

    const pal::string_t& get_deps_file() const
    {
        return m_deps_file;
    }

public: // static
    // Create a deps_json_t instance from a self-contained deps file
    // If rid_resolution_options specify to read the RID fallback graph, it will be updated with the fallback_graph.
    static std::unique_ptr<deps_json_t> create_for_self_contained(const pal::string_t& deps_path, rid_resolution_options_t& rid_resolution_options);

    // Create a deps_json_t instance from a framework-dependent deps file
    static std::unique_ptr<deps_json_t> create_for_framework_dependent(const pal::string_t& deps_path, const rid_resolution_options_t& rid_resolution_options);

    // Get the RID fallback graph for a deps file.
    // Parse failures or non-existent files will return an empty fallback graph
    static rid_fallback_graph_t get_rid_fallback_graph(const pal::string_t& deps_path);

private:
    deps_json_t(const pal::string_t& deps_path, const rid_resolution_options_t& rid_resolution_options)
        : m_deps_file(deps_path)
        , m_file_exists(false)
        , m_valid(false)
        , m_rid_resolution_options(rid_resolution_options)
    { }

    void load(bool is_framework_dependent, std::function<void(const json_parser_t::value_t&)> post_process = {});
    void load_self_contained(const json_parser_t::value_t& json, const pal::string_t& target_name);
    void load_framework_dependent(const json_parser_t::value_t& json, const pal::string_t& target_name);
    void process_runtime_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, rid_specific_assets_t* p_assets);
    void process_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, deps_assets_t* p_assets);

    void reconcile_libraries_with_targets(
        const json_parser_t::value_t& json,
        const std::function<bool(const pal::string_t&)>& library_exists_fn,
        const std::function<const vec_asset_t&(const pal::string_t&, size_t, bool*)>& get_assets_fn);

    void perform_rid_fallback(rid_specific_assets_t* portable_assets);

    std::vector<deps_entry_t> m_deps_entries[deps_entry_t::asset_types::count];

    deps_assets_t m_assets;
    rid_specific_assets_t m_rid_assets;

    pal::string_t m_deps_file;
    bool m_file_exists;
    bool m_valid;

    const rid_resolution_options_t& m_rid_resolution_options;
};

#endif // __DEPS_FORMAT_H_
