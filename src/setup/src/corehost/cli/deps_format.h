// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __DEPS_FORMAT_H_
#define __DEPS_FORMAT_H_

#include <iostream>
#include <vector>
#include <unordered_set>
#include <functional>
#include "pal.h"
#include "deps_entry.h"
#include "cpprest/json.h"

class deps_json_t
{
    typedef web::json::value json_value;
    typedef web::json::object json_object;
    struct vec_t { std::vector<pal::string_t> vec; };
    struct assets_t { std::array<vec_t, deps_entry_t::asset_types::count> by_type; };
    struct deps_assets_t { std::unordered_map<pal::string_t, assets_t> libs; };
    struct rid_assets_t { std::unordered_map<pal::string_t, assets_t> rid_assets; };
    struct rid_specific_assets_t { std::unordered_map<pal::string_t, rid_assets_t> libs; };

    typedef std::unordered_map<pal::string_t, std::vector<pal::string_t>> str_to_vector_map_t;
    typedef str_to_vector_map_t rid_fallback_graph_t;


public:
    deps_json_t()
        : m_valid(false)
        , m_file_exists(false)
    {
    }

    deps_json_t(bool portable, const pal::string_t& deps_path)
        : deps_json_t(portable, deps_path, m_rid_fallback_graph /* dummy */)
    {
    }

    deps_json_t(bool portable, const pal::string_t& deps_path, const rid_fallback_graph_t& graph)
        : deps_json_t()
    {
        m_valid = load(portable, deps_path, graph);
    }

    const std::vector<deps_entry_t>& get_entries(deps_entry_t::asset_types type)
    {
        assert(type < deps_entry_t::asset_types::count);
        return m_deps_entries[type];
    }

    bool has_package(const pal::string_t& name, const pal::string_t& ver) const;

    bool exists()
    {
        return m_file_exists;
    }

    bool is_valid()
    {
        return m_valid;
    }

    const rid_fallback_graph_t& get_rid_fallback_graph()
    {
        return m_rid_fallback_graph;
    }

	const deps_entry_t& try_ni(const deps_entry_t& entry) const;

private:
    bool load_standalone(const json_value& json, const pal::string_t& target_name);
    bool load_portable(const json_value& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph);
    bool load(bool portable, const pal::string_t& deps_path, const rid_fallback_graph_t& rid_fallback_graph);
    bool process_runtime_targets(const json_value& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph, rid_specific_assets_t* p_assets);
    bool process_targets(const json_value& json, const pal::string_t& target_name, deps_assets_t* p_assets);

    void reconcile_libraries_with_targets(
        const json_value& json,
        const std::function<bool(const pal::string_t&)>& library_exists_fn,
        const std::function<const std::vector<pal::string_t>&(const pal::string_t&, int, bool*)>& get_rel_paths_by_asset_type_fn);

    pal::string_t get_optional_path(const json_object& properties, const pal::string_t& key) const;

    bool perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph);

    std::vector<deps_entry_t> m_deps_entries[deps_entry_t::asset_types::count];

    deps_assets_t m_assets;
    rid_specific_assets_t m_rid_assets;

	std::unordered_map<pal::string_t, int> m_ni_entries;
    rid_fallback_graph_t m_rid_fallback_graph;
    bool m_file_exists;
    bool m_valid;
};

#endif // __DEPS_FORMAT_H_
