// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "deps_entry.h"
#include "deps_format.h"
#include "utils.h"
#include "trace.h"
#include "bundle/info.h"
#include <tuple>
#include <array>
#include <iterator>
#include <cassert>
#include <functional>

const std::array<const pal::char_t*, deps_entry_t::asset_types::count> deps_entry_t::s_known_asset_types = {{
    _X("runtime"), _X("resources"), _X("native")
}};

namespace
{
    pal::string_t get_optional_property(
        const json_parser_t::value_t& properties,
        const pal::string_t& key)
    {
        const auto& prop = properties.FindMember(key.c_str());
        return (prop != properties.MemberEnd() && prop->value.IsString()) ? prop->value.GetString() : _X("");
    }

    pal::string_t get_optional_path(
        const json_parser_t::value_t& properties,
        const pal::string_t& key)
    {
        pal::string_t path = get_optional_property(properties, key);

        if (path.length() > 0 && _X('/') != DIR_SEPARATOR)
        {
            replace_char(&path, _X('/'), DIR_SEPARATOR);
        }

        return path;
    }
}

void deps_json_t::reconcile_libraries_with_targets(
    const pal::string_t& deps_path,
    const json_parser_t::value_t& json,
    const std::function<bool(const pal::string_t&)>& library_exists_fn,
    const std::function<const vec_asset_t&(const pal::string_t&, size_t, bool*)>& get_assets_fn)
{
    pal::string_t deps_file = get_filename(deps_path);

    for (const auto& library : json[_X("libraries")].GetObject())
    {
        trace::info(_X("Reconciling library %s"), library.name.GetString());

        pal::string_t lib_name{library.name.GetString()};
        if (!library_exists_fn(lib_name))
        {
            trace::info(_X("Library %s does not exist"), library.name.GetString());
            continue;
        }

        const pal::string_t& hash = library.value[_X("sha512")].GetString();
        bool serviceable = library.value[_X("serviceable")].GetBool();

        pal::string_t library_path = get_optional_path(library.value, _X("path"));
        pal::string_t library_hash_path = get_optional_path(library.value, _X("hashPath"));
        pal::string_t runtime_store_manifest_list = get_optional_path(library.value, _X("runtimeStoreManifestName"));

        for (size_t i = 0; i < deps_entry_t::s_known_asset_types.size(); ++i)
        {
            bool rid_specific = false;
            const vec_asset_t& assets = get_assets_fn(lib_name, i, &rid_specific);
            m_deps_entries[i].reserve(assets.size());
            for (const auto& asset : assets)
            {
                auto asset_name = asset.name;
                if (ends_with(asset_name, _X(".ni"), false))
                {
                    asset_name = strip_file_ext(asset_name);
                }

                deps_entry_t entry;
                size_t pos = lib_name.find(_X("/"));
                entry.library_name = lib_name.substr(0, pos);
                entry.library_version = lib_name.substr(pos + 1);
                entry.library_type = to_lower(library.value[_X("type")].GetString());
                entry.library_hash = hash;
                entry.library_path = library_path;
                entry.library_hash_path = library_hash_path;
                entry.runtime_store_manifest_list = runtime_store_manifest_list;
                entry.asset_type = static_cast<deps_entry_t::asset_types>(i);
                entry.is_serviceable = serviceable;
                entry.is_rid_specific = rid_specific;
                entry.deps_file = deps_file;
                entry.asset = asset;
                entry.asset.name = asset_name;

                if (trace::is_enabled())
                {
                    trace::info(_X("Parsed %s deps entry %zu for asset name: %s from %s: %s, library version: %s, relpath: %s, assemblyVersion %s, fileVersion %s"),
                        deps_entry_t::s_known_asset_types[i],
                        m_deps_entries[i].size() - 1,
                        entry.asset.name.c_str(),
                        entry.library_type.c_str(),
                        entry.library_name.c_str(),
                        entry.library_version.c_str(),
                        entry.asset.relative_path.c_str(),
                        entry.asset.assembly_version.as_str().c_str(),
                        entry.asset.file_version.as_str().c_str());
                }

                m_deps_entries[i].push_back(std::move(entry));
            }
        }
    }
}

// Returns the RID determined (computed or fallback) for the platform the host is running on.
pal::string_t deps_json_t::get_current_rid(const rid_fallback_graph_t& rid_fallback_graph)
{
    pal::string_t currentRid = get_current_runtime_id(false /*use_fallback*/);

    trace::info(_X("HostRID is %s"), currentRid.empty()? _X("not available"): currentRid.c_str());

    // If the current RID is not present in the RID fallback graph, then the platform
    // is unknown to us. At this point, we will fallback to using the base RIDs and attempt
    // asset lookup using them.
    //
    // We do the same even when the RID is empty.
    if (currentRid.empty() || (rid_fallback_graph.count(currentRid) == 0))
    {
        currentRid = pal::get_current_os_fallback_rid() + pal::string_t(_X("-")) + get_current_arch_name();

        trace::info(_X("Falling back to base HostRID: %s"), currentRid.c_str());
    }

    return currentRid;
}

bool deps_json_t::perform_rid_fallback(rid_specific_assets_t* portable_assets, const rid_fallback_graph_t& rid_fallback_graph)
{
    pal::string_t host_rid = get_current_rid(rid_fallback_graph);

    for (auto& package : portable_assets->libs)
    {
        for (size_t asset_type_index = 0; asset_type_index < deps_entry_t::asset_types::count; asset_type_index++)
        {
            auto& rid_assets = package.second[asset_type_index].rid_assets;
            pal::string_t matched_rid = rid_assets.count(host_rid) ? host_rid : _X("");
            if (matched_rid.empty())
            {
                auto rid_fallback_iter = rid_fallback_graph.find(host_rid);
                if (rid_fallback_iter == rid_fallback_graph.end())
                {
                    trace::warning(_X("The targeted framework does not support the runtime '%s'. Some native libraries from [%s] may fail to load on this platform."), host_rid.c_str(), package.first.c_str());
                }
                else
                {
                    const auto& fallback_rids = rid_fallback_iter->second;
                    auto iter = std::find_if(fallback_rids.begin(), fallback_rids.end(), [&rid_assets](const pal::string_t& rid) {
                        return rid_assets.count(rid);
                        });
                    if (iter != fallback_rids.end())
                    {
                        matched_rid = *iter;
                    }
                }
            }

            if (matched_rid.empty())
            {
                rid_assets.clear();
            }

            for (auto iter = rid_assets.begin(); iter != rid_assets.end(); /* */)
            {
                if (iter->first != matched_rid)
                {
                    trace::verbose(
                        _X("Chose %s, so removing rid (%s) specific assets for package %s and asset type %s"),
                        matched_rid.c_str(),
                        iter->first.c_str(),
                        package.first.c_str(),
                        deps_entry_t::s_known_asset_types[asset_type_index]);

                    iter = rid_assets.erase(iter);
                }
                else
                {
                    ++iter;
                }
            }
        }
    }
    return true;
}

bool deps_json_t::process_runtime_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph, rid_specific_assets_t* p_assets)
{
    rid_specific_assets_t& assets = *p_assets;
    for (const auto& package : json[_X("targets")][target_name.c_str()].GetObject())
    {
        const auto& runtimeTargets = package.value.FindMember(_X("runtimeTargets"));
        if (runtimeTargets == package.value.MemberEnd())
        {
            continue;
        }

        for (const auto& file : runtimeTargets->value.GetObject())
        {
            const auto& type = file.value[_X("assetType")].GetString();

            for (size_t asset_type_index = 0; asset_type_index < deps_entry_t::s_known_asset_types.size(); ++asset_type_index)
            {
                if (pal::strcasecmp(type, deps_entry_t::s_known_asset_types[asset_type_index]) != 0)
                {
                    continue;
                }

                version_t assembly_version, file_version;

                const pal::string_t& assembly_version_str = get_optional_property(file.value, _X("assemblyVersion"));
                if (!assembly_version_str.empty())
                {
                    version_t::parse(assembly_version_str, &assembly_version);
                }

                const pal::string_t& file_version_str = get_optional_property(file.value, _X("fileVersion"));
                if (!file_version_str.empty())
                {
                    version_t::parse(file_version_str, &file_version);
                }

                pal::string_t file_name{file.name.GetString()};
                deps_asset_t asset(get_filename_without_ext(file_name), file_name, assembly_version, file_version);

                const auto& rid = file.value[_X("rid")].GetString();

                if (trace::is_enabled())
                {
                    trace::info(_X("Adding runtimeTargets %s asset %s rid=%s assemblyVersion=%s fileVersion=%s from %s"),
                        deps_entry_t::s_known_asset_types[asset_type_index],
                        asset.relative_path.c_str(),
                        rid,
                        asset.assembly_version.as_str().c_str(),
                        asset.file_version.as_str().c_str(),
                        package.name.GetString());
                }

                assets.libs[package.name.GetString()][asset_type_index].rid_assets[rid].push_back(asset);
            }
        }
    }

    if (!perform_rid_fallback(&assets, rid_fallback_graph))
    {
        return false;
    }

    return true;
}

bool deps_json_t::process_targets(const json_parser_t::value_t& json, const pal::string_t& target_name, deps_assets_t* p_assets)
{
    deps_assets_t& assets = *p_assets;
    for (const auto& package : json[_X("targets")][target_name.c_str()].GetObject())
    {
        const auto& asset_types = package.value.GetObject();
        for (size_t i = 0; i < deps_entry_t::s_known_asset_types.size(); ++i)
        {
            const auto& iter = asset_types.FindMember(deps_entry_t::s_known_asset_types[i]);
            if (iter == asset_types.MemberEnd())
            {
                continue;
            }

            const auto& files = iter->value.GetObject();
            vec_asset_t& asset_files = assets.libs[package.name.GetString()][i];
            asset_files.reserve(files.MemberCount());
            for (const auto& file : files)
            {
                version_t assembly_version, file_version;

                const pal::string_t& assembly_version_str = get_optional_property(file.value, _X("assemblyVersion"));
                if (assembly_version_str.length() > 0)
                {
                    version_t::parse(assembly_version_str, &assembly_version);
                }

                const pal::string_t& file_version_str = get_optional_property(file.value, _X("fileVersion"));
                if (file_version_str.length() > 0)
                {
                    version_t::parse(file_version_str, &file_version);
                }

                pal::string_t file_name{file.name.GetString()};
                deps_asset_t asset(get_filename_without_ext(file_name), file_name, assembly_version, file_version);

                if (trace::is_enabled())
                {
                    trace::info(_X("Adding %s asset %s assemblyVersion=%s fileVersion=%s from %s"),
                        deps_entry_t::s_known_asset_types[i],
                        asset.relative_path.c_str(),
                        asset.assembly_version.as_str().c_str(),
                        asset.file_version.as_str().c_str(),
                        package.name.GetString());
                }

                asset_files.push_back(std::move(asset));
            }
        }
    }
    return true;
}

bool deps_json_t::load_framework_dependent(const pal::string_t& deps_path, const json_parser_t::value_t& json, const pal::string_t& target_name, const rid_fallback_graph_t& rid_fallback_graph)
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

    const vec_asset_t empty;
    auto get_relpaths = [&](const pal::string_t& package, size_t asset_type_index, bool* rid_specific) -> const vec_asset_t& {

        *rid_specific = false;

        // Is there any rid specific assets for this type ("native" or "runtime" or "resources")
        if (m_rid_assets.libs.count(package) && !m_rid_assets.libs[package][asset_type_index].rid_assets.empty())
        {
            const auto& assets_for_type = m_rid_assets.libs[package][asset_type_index].rid_assets.begin()->second;
            if (!assets_for_type.empty())
            {
                *rid_specific = true;
                return assets_for_type;
            }

            trace::verbose(_X("There were no rid specific %s asset for %s"), deps_entry_t::s_known_asset_types[asset_type_index], package.c_str());
        }

        if (m_assets.libs.count(package))
        {
            return m_assets.libs[package][asset_type_index];
        }

        return empty;
    };

    reconcile_libraries_with_targets(deps_path, json, package_exists, get_relpaths);

    return true;
}

bool deps_json_t::load_self_contained(const pal::string_t& deps_path, const json_parser_t::value_t& json, const pal::string_t& target_name)
{
    if (!process_targets(json, target_name, &m_assets))
    {
        return false;
    }

    auto package_exists = [&](const pal::string_t& package) -> bool {
        return m_assets.libs.count(package);
    };

    auto get_relpaths = [&](const pal::string_t& package, size_t type_index, bool* rid_specific) -> const vec_asset_t& {
        *rid_specific = false;
        return m_assets.libs[package][type_index];
    };

    reconcile_libraries_with_targets(deps_path, json, package_exists, get_relpaths);

    const auto& json_object = json.GetObject();
    if (json_object.HasMember(_X("runtimes")))
    {
        for (const auto& rid : json[_X("runtimes")].GetObject())
        {
            auto& vec = m_rid_fallback_graph[rid.name.GetString()];
            const auto& fallback_array = rid.value.GetArray();
            vec.reserve(fallback_array.Size());
            for (const auto& fallback : fallback_array)
            {
                vec.push_back(fallback.GetString());
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
    pal::string_t pv;
    pv.reserve(name.length() + ver.length() + 1);
    pv.assign(name);
    pv.push_back(_X('/'));
    pv.append(ver);

    auto iter = m_rid_assets.libs.find(pv);
    if (iter != m_rid_assets.libs.end())
    {
        for (size_t asset_type_index = 0; asset_type_index < deps_entry_t::asset_types::count; asset_type_index++)
        {
            if (!iter->second[asset_type_index].rid_assets.empty())
            {
                return true;
            }
        }
    }

    return m_assets.libs.count(pv);
}

// -----------------------------------------------------------------------------
// Load the deps file and parse its "entry" lines which contain the "fields" of
// the entry. Populate an array of these entries.
//
bool deps_json_t::load(bool is_framework_dependent, const pal::string_t& deps_path, const rid_fallback_graph_t& rid_fallback_graph)
{
    m_deps_file = deps_path;
    m_file_exists = bundle::info_t::config_t::probe(deps_path) || pal::realpath(&m_deps_file, true);

    json_parser_t json;
    if (!m_file_exists)
    {
        // If file doesn't exist, then assume parsed.
        trace::verbose(_X("Could not locate the dependencies manifest file [%s]. Some libraries may fail to resolve."), deps_path.c_str());
        return true;
    }

    if (!json.parse_file(m_deps_file))
    {
        return false;
    }

    const auto& runtime_target = json.document()[_X("runtimeTarget")];
    const pal::string_t& name = runtime_target.IsString() ?
        runtime_target.GetString() :
        runtime_target[_X("name")].GetString();

    trace::verbose(_X("Loading deps file... %s as framework dependent=[%d]"), deps_path.c_str(), is_framework_dependent);

    if (is_framework_dependent)
        return load_framework_dependent(deps_path, json.document(), name, rid_fallback_graph);

    return load_self_contained(deps_path, json.document(), name);
}
