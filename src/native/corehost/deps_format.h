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

    deps_json_t()
        : m_file_exists(false)
        , m_valid(false)
    {
    }

    deps_json_t(bool is_framework_dependent, const pal::string_t& deps_path)
        : deps_json_t(is_framework_dependent, deps_path, m_rid_fallback_graph /* dummy */)
    {
    }

    deps_json_t(bool is_framework_dependent, const pal::string_t& deps_path, const rid_fallback_graph_t& graph)
        : deps_json_t()
    {
        m_valid = load(is_framework_dependent, deps_path, graph);
    }

    void parse(bool is_framework_dependent, const pal::string_t& deps_path)
    {
        m_valid = load(is_framework_dependent, deps_path, m_rid_fallback_graph /* dummy */);
    }

    void parse(bool is_framework_dependent, const pal::string_t& deps_path, const rid_fallback_graph_t& graph)
    {
        m_valid = load(is_framework_dependent, deps_path, graph);
    }

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

    const rid_fallback_graph_t& get_rid_fallback_graph() const
    {
        return m_rid_fallback_graph;
    }

    pal::string_t get_deps_file() const
    {
        return m_deps_file;
    }

private:
    bool load_self_contained(const pal::string_t& deps_path, const json_parser_t::value_t& json, const pal::string_t& target_name);
    bool load_framework_dependent(const pal::string_t& deps_path, const json_parser_t::value_t& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph);
    bool load(bool is_framework_dependent, const pal::string_t& deps_path, const rid_fallback_graph_t& rid_fallback_graph);
    bool process_runtime_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph, rid_specific_assets_t* p_assets);
    bool process_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, deps_assets_t* p_assets);

    void reconcile_libraries_with_targets(
        const pal::string_t& deps_path,
        const json_parser_t::value_t& json,
        const std::function<bool(const pal::string_t&)>& library_exists_fn,
        const std::function<const vec_asset_t&(const pal::string_t&, size_t, bool*)>& get_assets_fn);

    pal::string_t get_current_rid(const rid_fallback_graph_t& rid_fallback_graph);
    bool perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph);

    std::vector<deps_entry_t> m_deps_entries[deps_entry_t::asset_types::count];

    deps_assets_t m_assets;
    rid_specific_assets_t m_rid_assets;

    rid_fallback_graph_t m_rid_fallback_graph;
    bool m_file_exists;
    bool m_valid;

    pal::string_t m_deps_file;
};

#endif // __DEPS_FORMAT_H_
