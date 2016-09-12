// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "deps_entry.h"
#include "deps_format.h"
#include "utils.h"
#include "trace.h"
#include <tuple>
#include <array>
#include <iterator>
#include <cassert>
#include <functional>

const std::array<const pal::char_t*, deps_entry_t::asset_types::count> deps_entry_t::s_known_asset_types = {
    _X("runtime"), _X("resources"), _X("native")
};

const deps_entry_t& deps_json_t::try_ni(const deps_entry_t& entry) const
{
    if (m_ni_entries.count(entry.asset_name))
    {
        int index = m_ni_entries.at(entry.asset_name);
        return m_deps_entries[deps_entry_t::asset_types::runtime][index];
    }
    return entry;
}

pal::string_t deps_json_t::get_optional_path(
    const json_object& properties,
    const pal::string_t& key) const
{
    pal::string_t path;

    const auto& iter = properties.find(key);

    if (iter != properties.end())
    {
        path = iter->second.as_string();

        if (_X('/') != DIR_SEPARATOR)
        {
            replace_char(&path, _X('/'), DIR_SEPARATOR);
        }
    }

    return path;
}

void deps_json_t::reconcile_libraries_with_targets(
    const json_value& json,
    const std::function<bool(const pal::string_t&)>& library_exists_fn,
    const std::function<const std::vector<pal::string_t>&(const pal::string_t&, int, bool*)>& get_rel_paths_by_asset_type_fn)
{
    const auto& libraries = json.at(_X("libraries")).as_object();
    for (const auto& library : libraries)
    {
        trace::info(_X("Reconciling library %s"), library.first.c_str());

        if (!library_exists_fn(library.first))
        {
            trace::info(_X("Library %s does not exist"), library.first.c_str());
            continue;
        }

        const auto& properties = library.second.as_object();

        const pal::string_t& hash = properties.at(_X("sha512")).as_string();
        bool serviceable = properties.at(_X("serviceable")).as_bool();

        pal::string_t library_path = get_optional_path(properties, _X("path"));
        pal::string_t library_hash_path = get_optional_path(properties, _X("hashPath"));

        for (int i = 0; i < deps_entry_t::s_known_asset_types.size(); ++i)
        {
            bool rid_specific = false;
            for (const auto& rel_path : get_rel_paths_by_asset_type_fn(library.first, i, &rid_specific))
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
                entry.library_type = pal::to_lower(properties.at(_X("type")).as_string());
                entry.library_hash = hash;
                entry.library_path = library_path;
                entry.library_hash_path = library_hash_path;
                entry.asset_name = asset_name;
                entry.asset_type = (deps_entry_t::asset_types) i;
                entry.relative_path = rel_path;
                entry.is_serviceable = serviceable;
                entry.is_rid_specific = rid_specific;

                // TODO: Deps file does not follow spec. It uses '\\', should use '/'
                replace_char(&entry.relative_path, _X('\\'), _X('/'));

                m_deps_entries[i].push_back(entry);

                if (ni_dll)
                {
                    m_ni_entries[entry.asset_name] = m_deps_entries
                        [deps_entry_t::asset_types::runtime].size() - 1;
                }

                trace::info(_X("Parsed %s deps entry %d for asset name: %s from %s: %s, version: %s, relpath: %s"),
                    deps_entry_t::s_known_asset_types[i],
                    m_deps_entries[i].size() - 1,
                    entry.asset_name.c_str(),
                    entry.library_type.c_str(),
                    entry.library_name.c_str(),
                    entry.library_version.c_str(),
                    entry.relative_path.c_str());
                
            }
        }
    }
}

pal::string_t get_own_rid()
{
#if defined(TARGET_RUNTIME_ID)
    return _STRINGIFY(TARGET_RUNTIME_ID);
#else
#error "Cannot build the host without knowing host's root RID"
#endif
}

bool deps_json_t::perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph)
{
    pal::string_t host_rid = get_own_rid();
    for (auto& package : portable_assets->libs)
    {
        pal::string_t matched_rid = package.second.rid_assets.count(host_rid) ? host_rid : _X("");
        if (matched_rid.empty())
        {
            if (rid_fallback_graph.count(host_rid) == 0)
            {
                trace::warning(_X("The targeted framework does not support the runtime '%s'. Some native libraries from [%s] may fail to load on this platform."), host_rid.c_str(), package.first.c_str());
            }
            else
            {
                const auto& fallback_rids = rid_fallback_graph.find(host_rid)->second;
                auto iter = std::find_if(fallback_rids.begin(), fallback_rids.end(), [&package](const pal::string_t& rid) {
                    return package.second.rid_assets.count(rid);
                });
                if (iter != fallback_rids.end())
                {
                    matched_rid = *iter;
                }
            }
        }

        if (matched_rid.empty())
        {
            package.second.rid_assets.clear();
        }

        for (auto iter = package.second.rid_assets.begin(); iter != package.second.rid_assets.end(); /* */)
        {
            if (iter->first != matched_rid)
            {
                trace::verbose(_X("Chose %s, so removing rid (%s) specific assets for package %s"), matched_rid.c_str(), iter->first.c_str(), package.first.c_str());
                iter = package.second.rid_assets.erase(iter);
            }
            else
            {
                ++iter;
            }
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
            for (int i = 0; i < deps_entry_t::s_known_asset_types.size(); ++i)
            {
                if (pal::strcasecmp(type.c_str(), deps_entry_t::s_known_asset_types[i]) == 0)
                {
                    const auto& rid = file.second.at(_X("rid")).as_string();
                    assets.libs[package.first].rid_assets[rid].by_type[i].vec.push_back(file.first);
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
        const auto& asset_types = package.second.as_object();
        for (int i = 0; i < deps_entry_t::s_known_asset_types.size(); ++i)
        {
            auto iter = asset_types.find(deps_entry_t::s_known_asset_types[i]);
            if (iter != asset_types.end())
            {
                for (const auto& file : iter->second.as_object())
                {
                    trace::info(_X("Adding %s asset %s from %s"), deps_entry_t::s_known_asset_types[i], file.first.c_str(), package.first.c_str());
                    assets.libs[package.first].by_type[i].vec.push_back(file.first);
                }
            }
        }
    }
    return true;
}

bool deps_json_t::load_portable(const json_value& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph)
{
    if (!process_runtime_targets(json, target_name, rid_fallback_graph, &m_rid_assets))
    {
        return false;
    }

    if (!process_targets(json, target_name, &m_assets))
    {
        return false;
    }

    auto package_exists = [&](const pal::string_t& package) -> bool {
        return m_rid_assets.libs.count(package) || m_assets.libs.count(package);
    };

    const std::vector<pal::string_t> empty;
    auto get_relpaths = [&](const pal::string_t& package, int type_index, bool* rid_specific) -> const std::vector<pal::string_t>& {

        *rid_specific = false;

        // Is there any rid specific assets for this type ("native" or "runtime" or "resources")
        if (m_rid_assets.libs.count(package) && !m_rid_assets.libs[package].rid_assets.empty())
        {
            const auto& assets_by_type = m_rid_assets.libs[package].rid_assets.begin()->second.by_type[type_index].vec;
            if (!assets_by_type.empty())
            {
                *rid_specific = true;
                return assets_by_type;
            }

            trace::verbose(_X("There were no rid specific %s asset for %s"), deps_entry_t::s_known_asset_types[type_index], package.c_str());
        }

        if (m_assets.libs.count(package))
        {
            return m_assets.libs[package].by_type[type_index].vec;
        }

        return empty;
    };

    reconcile_libraries_with_targets(json, package_exists, get_relpaths);

    return true;
}

bool deps_json_t::load_standalone(const json_value& json, const pal::string_t& target_name)
{
    if (!process_targets(json, target_name, &m_assets))
    {
        return false;
    }

    auto package_exists = [&](const pal::string_t& package) -> bool {
        return m_assets.libs.count(package);
    };

    auto get_relpaths = [&](const pal::string_t& package, int type_index, bool* rid_specific) -> const std::vector<pal::string_t>& {
        *rid_specific = false;
        return m_assets.libs[package].by_type[type_index].vec;
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

    if (trace::is_enabled())
    {
        trace::verbose(_X("The rid fallback graph is: {"));
        for (const auto& rid : m_rid_fallback_graph)
        {
            trace::verbose(_X("%s => ["), rid.first.c_str());
            for (const auto& fallback : rid.second)
            {
                trace::verbose(_X("%s, "), fallback.c_str());
            }
            trace::verbose(_X("]"));
        }
        trace::verbose(_X("}"));
    }
    return true;
}

bool deps_json_t::has_package(const pal::string_t& name, const pal::string_t& ver) const
{
    pal::string_t pv = name;
    pv.push_back(_X('/'));
    pv.append(ver);
    
    auto iter = m_rid_assets.libs.find(pv);
    if (iter != m_rid_assets.libs.end())
    {
        if (!iter->second.rid_assets.empty())
        {
            return true;
        }
    }
    
    return m_assets.libs.count(pv);
}

// -----------------------------------------------------------------------------
// Load the deps file and parse its "entry" lines which contain the "fields" of
// the entry. Populate an array of these entries.
//
bool deps_json_t::load(bool portable, const pal::string_t& deps_path, const rid_fallback_graph_t& rid_fallback_graph)
{
    m_file_exists = pal::file_exists(deps_path);

    // If file doesn't exist, then assume parsed.
    if (!m_file_exists)
    {
        trace::verbose(_X("Could not locate the dependencies manifest file [%s]. Some libraries may fail to resolve."), deps_path.c_str());
        return true;
    }

    // Somehow the file stream could not be opened. This is an error.
    pal::ifstream_t file(deps_path);
    if (!file.good())
    {
        trace::error(_X("Could not open dependencies manifest file [%s]"), deps_path.c_str());
        return false;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), deps_path.c_str());
    }

    try
    {
        const auto json = json_value::parse(file);

        const auto& runtime_target = json.at(_X("runtimeTarget"));

        const pal::string_t& name = runtime_target.is_string()?
            runtime_target.as_string():
            runtime_target.at(_X("name")).as_string();

        trace::verbose(_X("Loading deps file... %s as portable=[%d]"), deps_path.c_str(), portable);

        return (portable) ? load_portable(json, name, rid_fallback_graph) : load_standalone(json, name);
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void) pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), deps_path.c_str(), jes.c_str());
        return false;
    }
}
