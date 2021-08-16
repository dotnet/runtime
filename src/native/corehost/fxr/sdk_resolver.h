// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal.h"
#include "fx_ver.h"

// Note: this must be kept in-sync with `RollForwardPolicyNames`.
enum class sdk_roll_forward_policy
{
    // The specified policy is not supported.
    unsupported,
    // Do not roll forward (require an exact match to requested).
    disable,
    // Allow a roll-forward to the latest patch level if the requested version is not installed.
    patch,
    // Allow a roll-forward to the nearest available feature band with latest patch level.
    feature,
    // Allow a roll-forward to the nearest available minor version with latest patch level.
    minor,
    // Allow a roll-forward to the nearest available major version with latest patch level.
    major,
    // Roll-forward to the latest patch level (major, minor, and feature band must match).
    latest_patch,
    // Roll-forward to the latest installed feature band (major and minor must match).
    latest_feature,
    // Roll-forward to the latest minor version (major must match).
    latest_minor,
    // Roll-forward to the latest major version (latest version installed).
    latest_major,
};

class sdk_resolver
{
public:
    explicit sdk_resolver(bool allow_prerelease = true);
    sdk_resolver(fx_ver_t version, sdk_roll_forward_policy roll_forward, bool allow_prerelease);

    pal::string_t const& global_file_path() const;

    pal::string_t resolve(const pal::string_t& dotnet_root, bool print_errors = true) const;

    void print_resolution_error(const pal::string_t& dotnet_root, const pal::char_t *prefix) const;

    static sdk_resolver from_nearest_global_file(bool allow_prerelease = true);

    static sdk_resolver from_nearest_global_file(
        const pal::string_t& cwd,
        bool allow_prerelease = true);

private:
    static sdk_roll_forward_policy to_policy(const pal::string_t& name);
    static const pal::char_t* to_policy_name(sdk_roll_forward_policy policy);
    static pal::string_t find_nearest_global_file(const pal::string_t& cwd);
    bool parse_global_file(pal::string_t global_file_path);
    bool matches_policy(const fx_ver_t& current) const;
    bool is_better_match(const fx_ver_t& current, const fx_ver_t& previous) const;
    bool exact_match_preferred() const;
    bool is_policy_use_latest() const;
    bool resolve_sdk_path_and_version(const pal::string_t& dir, pal::string_t& sdk_path, fx_ver_t& resolved_version) const;

    pal::string_t global_file;
    fx_ver_t version;
    sdk_roll_forward_policy roll_forward;
    bool allow_prerelease;
};
