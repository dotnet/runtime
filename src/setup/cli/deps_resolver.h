// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef DEPS_RESOLVER_H
#define DEPS_RESOLVER_H

#include <vector>

#include "pal.h"
#include "trace.h"

#include "servicing_index.h"

struct deps_entry_t
{
    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t asset_type;
    pal::string_t asset_name;
    pal::string_t relative_path;
    bool is_serviceable;

    // Given a "base" dir, yield the relative path in the package layout.
    bool to_full_path(const pal::string_t& root, pal::string_t* str) const;

    // Given a "base" dir, yield the relative path in the package layout only if
    // the hash matches contents of the hash file.
    bool to_hash_matched_path(const pal::string_t& root, pal::string_t* str) const;
};

// Probe paths to be resolved for ordering
struct probe_paths_t
{
    pal::string_t tpa;
    pal::string_t native;
    pal::string_t culture;
};

class deps_resolver_t
{
public:
    deps_resolver_t(const arguments_t& args)
        : m_svc(args.dotnet_servicing)
        , m_runtime_svc(args.dotnet_runtime_servicing)
        , m_coreclr_index(-1)
    {
        m_deps_valid = parse_deps_file(args);
    }

    bool valid() { return m_deps_valid; }

    bool resolve_probe_paths(
      const pal::string_t& app_dir,
      const pal::string_t& package_dir,
      const pal::string_t& package_cache_dir,
      const pal::string_t& clr_dir,
      probe_paths_t* probe_paths);

    pal::string_t resolve_coreclr_dir(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir);

private:

    bool load();

    bool parse_deps_file(const arguments_t& args);

    // Resolve order for TPA lookup.
    void resolve_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Resolve order for culture and native DLL lookup.
    void resolve_probe_dirs(
        const pal::string_t& asset_type,
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    // Populate local assemblies from app_dir listing.
    void get_local_assemblies(const pal::string_t& dir);

    // Servicing index to resolve serviced assembly paths.
    servicing_index_t m_svc;

    // Runtime servicing directory.
    pal::string_t m_runtime_svc;

    // Map of simple name -> full path of local assemblies populated in priority
    // order of their extensions.
    std::unordered_map<pal::string_t, pal::string_t> m_local_assemblies;

    // Entries in the dep file
    std::vector<deps_entry_t> m_deps_entries;

    // Special entry for coreclr in the deps entries
    int m_coreclr_index;

    // The dep file path
    pal::string_t m_deps_path;

    // Is the deps file valid
    bool m_deps_valid;
};

#endif // DEPS_RESOLVER_H
