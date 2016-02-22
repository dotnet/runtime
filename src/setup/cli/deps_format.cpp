// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "deps_format.h"
#include "utils.h"
#include "trace.h"
#include <unordered_set>
#include <tuple>
#include <array>
#include <iterator>
#include <cassert>
#include <functional>

const std::array<const pal::char_t*, deps_entry_t::asset_types::count> deps_json_t::s_known_asset_types = {
    _X("runtime"), _X("resources"), _X("native") };

const deps_entry_t& deps_json_t::try_ni(const deps_entry_t& entry) const
{
    if (m_ni_entries.count(entry.asset_name))
    {
        int index = m_ni_entries.at(entry.asset_name);
        return m_deps_entries[deps_entry_t::asset_types::runtime][index];
    }
    return entry;
}

void deps_json_t::reconcile_libraries_with_targets(
    const json_value& json,
    const std::function<bool(const pal::string_t&)>& library_exists_fn,
    const std::function<const std::vector<pal::string_t>&(const pal::string_t&, int)>& get_rel_paths_by_asset_type_fn)
{
    const auto& libraries = json.at(_X("libraries")).as_object();
    for (const auto& library : libraries)
    {
        trace::info(_X("Reconciling library %s"), library.first.c_str());

        if (pal::to_lower(library.second.at(_X("type")).as_string()) != _X("package"))
        {
            trace::info(_X("Library %s is not a package"), library.first.c_str());
            continue;
        }
        if (!library_exists_fn(library.first))
        {
            trace::info(_X("Library %s does not exist"), library.first.c_str());
            continue;
        }

        const auto& properties = library.second.as_object();

        const pal::string_t& hash = properties.at(_X("sha512")).as_string();
        bool serviceable = properties.at(_X("serviceable")).as_bool();

        for (int i = 0; i < s_known_asset_types.size(); ++i)
        {
            for (const auto& rel_path : get_rel_paths_by_asset_type_fn(library.first, i))
            {
                bool ni_dll = false;
                auto asset_name = get_filename_without_ext(rel_path);
                if (ends_with(asset_name, _X(".ni"), false))
                {
                    ni_dll = true;
                    asset_name = strip_file_ext(asset_name);
                }

                deps_entry_t entry;
                size_t pos = library.first.find(_X("/"));
                entry.library_name = library.first.substr(0, pos);
                entry.library_version = library.first.substr(pos + 1);
                entry.library_type = _X("package");
                entry.library_hash = hash;
                entry.asset_name = asset_name;
                entry.asset_type = s_known_asset_types[i];
                entry.relative_path = rel_path;
                entry.is_serviceable = serviceable;

                // TODO: Deps file does not follow spec. It uses '\\', should use '/'
                replace_char(&entry.relative_path, _X('\\'), _X('/'));

                m_deps_entries[i].push_back(entry);

                if (ni_dll)
                {
                    m_ni_entries[entry.asset_name] = m_deps_entries
                        [deps_entry_t::asset_types::runtime].size() - 1;
                }

                trace::info(_X("Added %s %s deps entry [%d] [%s, %s, %s]"), s_known_asset_types[i], entry.asset_name.c_str(), m_deps_entries[i].size() - 1, entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
                
                if (i == deps_entry_t::asset_types::native &&
                    entry.asset_name == LIBCORECLR_FILENAME)
                {
                    m_coreclr_index = m_deps_entries[i].size() - 1;
                    trace::verbose(_X("Found coreclr from deps %d [%s, %s, %s]"),
                        m_coreclr_index,
                        entry.library_name.c_str(),
                        entry.library_version.c_str(),
                        entry.relative_path.c_str());
                }

            }
        }
    }
}

pal::string_t get_own_rid()
{
#define _STRINGIFY(s) _X(s)
#if defined(TARGET_RUNTIME_ID)
    return _STRINGIFY(TARGET_RUNTIME_ID);
#else
#error "Cannot build the host without knowing host's root RID"
#endif
}

bool deps_json_t::perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph)
{
    pal::string_t host_rid = get_own_rid();
    for (auto& package : *portable_assets)
    {
        pal::string_t matched_rid = package.second.count(host_rid) ? host_rid : _X("");
        if (matched_rid.empty())
        {
            if (rid_fallback_graph.count(host_rid) == 0)
            {
                trace::error(_X("Did not find fallback rids for package %s for the host rid %s"), package.first.c_str(), host_rid.c_str());
                return false;
            }
            const auto& fallback_rids = rid_fallback_graph.find(host_rid)->second;
            auto iter = std::find_if(fallback_rids.begin(), fallback_rids.end(), [&package](const pal::string_t& rid) {
                return package.second.count(rid);
            });
            if (iter == fallback_rids.end() || (*iter).empty())
            {
                trace::error(_X("Did not find a matching fallback rid for package %s for the host rid %s"), package.first.c_str(), host_rid.c_str());
                return false;
            }
            matched_rid = *iter;
        }
        assert(!matched_rid.empty());
        for (auto iter = package.second.begin(); iter != package.second.end(); /* */)
        {
            iter = (iter->first != matched_rid)
                 ? package.second.erase(iter)
                 : iter++;
        }
    }
    return true;
}


bool deps_json_t::process_runtime_targets(const json_value& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph, rid_specific_assets_t* p_assets)
{
    rid_specific_assets_t& assets = *p_assets;
    for (const auto& package : json.at(_X("targets")).at(target_name).as_object())
    {
        const auto& targets = package.second.as_object();
        auto iter = targets.find(_X("runtimeTargets"));
        if (iter == targets.end())
        {
            continue;
        }

        const auto& files = iter->second.as_object();
        for (const auto& file : files)
        {
            const auto& type = file.second.at(_X("assetType")).as_string();
            for (int i = 0; i < s_known_asset_types.size(); ++i)
            {
                if (pal::strcasecmp(type.c_str(), s_known_asset_types[i]) == 0)
                {
                    const auto& rid = file.second.at(_X("rid")).as_string();
                    assets[package.first][rid][i].push_back(file.first);
                }
            }
        }
    }

    if (!perform_rid_fallback(&assets, rid_fallback_graph))
    {
        return false;
    }

    return true;
}

bool deps_json_t::process_targets(const json_value& json, const pal::string_t& target_name, deps_assets_t* p_assets)
{
    deps_assets_t& assets = *p_assets;
    for (const auto& package : json.at(_X("targets")).at(target_name).as_object())
    {
        // if (package.second.at(_X("type")).as_string() != _X("package")) continue;

        const auto& asset_types = package.second.as_object();
        for (int i = 0; i < s_known_asset_types.size(); ++i)
        {
            auto iter = asset_types.find(s_known_asset_types[i]);
            if (iter != asset_types.end())
            {
                for (const auto& file : iter->second.as_object())
                {
                    trace::info(_X("Adding %s asset %s from %s"), s_known_asset_types[i], file.first.c_str(), package.first.c_str());
                    assets[package.first][i].push_back(file.first);
                }
            }
        }
    }
    return true;
}

bool deps_json_t::load_portable(const json_value& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph)
{
    rid_specific_assets_t rid_assets;
    if (!process_runtime_targets(json, target_name, rid_fallback_graph, &rid_assets))
    {
        return false;
    }

    deps_assets_t non_rid_assets;
    if (!process_targets(json, target_name, &non_rid_assets))
    {
        return false;
    }

    auto package_exists = [&rid_assets, &non_rid_assets](const pal::string_t& package) -> bool {
        return rid_assets.count(package) || non_rid_assets.count(package);
    };
    auto get_relpaths = [&rid_assets, &non_rid_assets](const pal::string_t& package, int type_index) -> const std::vector<pal::string_t>& {
        return (rid_assets.count(package))
            ? rid_assets[package].begin()->second[type_index]
            : non_rid_assets[package][type_index];
    };

    reconcile_libraries_with_targets(json, package_exists, get_relpaths);

    return true;
}

bool deps_json_t::load_standalone(const json_value& json, const pal::string_t& target_name)
{
    deps_assets_t assets;

    if (!process_targets(json, target_name, &assets))
    {
        return false;
    }

    auto package_exists = [&assets](const pal::string_t& package) -> bool {
        return assets.count(package);
    };

    auto get_relpaths = [&assets](const pal::string_t& package, int type_index) -> const std::vector<pal::string_t>& {
        return assets[package][type_index];
    };

    reconcile_libraries_with_targets(json, package_exists, get_relpaths);

    const auto& json_object = json.as_object();
    const auto iter = json_object.find(_X("runtimes"));
    if (iter != json_object.end())
    {
        for (const auto& rid : iter->second.as_object())
        {
            auto& vec = m_rid_fallback_graph[rid.first];
            for (const auto& fallback : rid.second.as_array())
            {
                vec.push_back(fallback.as_string());
            }
        }
    }
    return true;
}

// -----------------------------------------------------------------------------
// Load the deps file and parse its "entry" lines which contain the "fields" of
// the entry. Populate an array of these entries.
//
bool deps_json_t::load(bool portable, const pal::string_t& deps_path, const rid_fallback_graph_t& rid_fallback_graph)
{
    // If file doesn't exist, then assume parsed.
    if (!pal::file_exists(deps_path))
    {
        return true;
    }

    // Somehow the file stream could not be opened. This is an error.
    pal::ifstream_t file(deps_path);
    if (!file.good())
    {
        return false;
    }

    try
    {
        const auto json = json_value::parse(file);

        const auto& runtime_target = json.at(_X("runtimeTarget"));
        const pal::string_t& name = runtime_target.as_string();

        return (portable) ? load_portable(json, name, rid_fallback_graph) : load_standalone(json, name);
    }
    catch (...)
    {
        return false;
    }
}
