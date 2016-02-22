// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __DEPS_FORMAT_H_
#define __DEPS_FORMAT_H_

#include <iostream>
#include <vector>
#include <functional>
#include "pal.h"
#include "deps_entry.h"
#include "cpprest/json.h"

class deps_json_t
{
    typedef web::json::value json_value;
    typedef std::array<std::vector<pal::string_t>, deps_entry_t::asset_types::count> vectors_t;
    typedef std::unordered_map<pal::string_t, vectors_t> str_to_vectors_map_t;
    typedef std::unordered_map<pal::string_t, std::vector<pal::string_t>> str_to_vector_map_t;

    typedef str_to_vector_map_t rid_fallback_graph_t;
    typedef str_to_vectors_map_t deps_assets_t;
    typedef std::unordered_map<pal::string_t, str_to_vectors_map_t> rid_specific_assets_t;

public:
    deps_json_t()
        : m_valid(false)
        , m_coreclr_index(-1)
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

    bool has_coreclr_entry()
    {
        return m_coreclr_index >= 0;
    }

    const deps_entry_t& get_coreclr_entry()
    {
        assert(has_coreclr_entry());
        return m_deps_entries[deps_entry_t::asset_types::native][m_coreclr_index];
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
        const std::function<const std::vector<pal::string_t>&(const pal::string_t&, int)>& get_rel_paths_by_asset_type_fn);

    bool perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph);

    static const std::array<const pal::char_t*, deps_entry_t::asset_types::count> s_known_asset_types;

    std::vector<deps_entry_t> m_deps_entries[deps_entry_t::asset_types::count];

	std::unordered_map<pal::string_t, int> m_ni_entries;
    rid_fallback_graph_t m_rid_fallback_graph;
    int m_coreclr_index;
    bool m_valid;
};

class deps_text_t
{
public:
    deps_text_t(const pal::string_t& deps_path)
        : m_valid(load(deps_path))
    {
    }

    bool load(const pal::string_t& deps_path);
    bool is_valid() { return m_valid; }
    const std::vector<deps_entry_t>& get_entries() { return m_deps_entries; }

private:
    std::vector<deps_entry_t> m_deps_entries;
    bool m_valid;
};

#endif // __DEPS_FORMAT_H_
