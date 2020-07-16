// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOST_INTERFACE_H__
#define __HOST_INTERFACE_H__

#include <cstddef>
#include "pal.h"
#include "bundle/info.h"

enum host_mode_t
{
    invalid = 0,

    muxer,          // Invoked as "dotnet.exe".

    apphost,        // Invoked as <appname>.exe from the application base; this is the renamed "apphost.exe".

    split_fx,       // Invoked as "corehost.exe" for xunit scenarios. Supported for backwards compat for 1.x apps.
                    // Split FX means, the host is operating like "corerun.exe" in a split location from the application base (CORE_ROOT equivalent),
                    // but it has its "hostfxr.dll" next to it.

    libhost,        // Invoked from a non-exe scenario (e.g. COM Activation or self-hosting native application)
};

#define _HOST_INTERFACE_PACK 1
#pragma pack(push, _HOST_INTERFACE_PACK)
struct strarr_t
{
    // DO NOT modify this struct. It is used in a layout
    // dependent manner. Create another for your use.
    size_t len;
    const pal::char_t** arr;
};

struct host_interface_t
{
    size_t version_lo;                // Just assign sizeof() to this field.
    size_t version_hi;                // Breaking changes to the layout -- increment HOST_INTERFACE_LAYOUT_VERSION
    strarr_t config_keys;
    strarr_t config_values;
    const pal::char_t* fx_dir;
    const pal::char_t* fx_name;
    const pal::char_t* deps_file;
    size_t is_framework_dependent;
    strarr_t probe_paths;
    size_t patch_roll_forward;
    size_t prerelease_roll_forward;
    size_t host_mode;
    const pal::char_t* tfm;
    const pal::char_t* additional_deps_serialized;
    const pal::char_t* fx_ver;
    strarr_t fx_names;
    strarr_t fx_dirs;
    strarr_t fx_requested_versions;
    strarr_t fx_found_versions;
    const pal::char_t* host_command;
    const pal::char_t* host_info_host_path;
    const pal::char_t* host_info_dotnet_root;
    const pal::char_t* host_info_app_path;
    size_t single_file_bundle_header_offset;
    // !! WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING
    // !! 1. Only append to this structure to maintain compat.
    // !! 2. Any nested structs should not use compiler specific padding (pack with _HOST_INTERFACE_PACK)
    // !! 3. Do not take address of the fields of this struct or be prepared to deal with unaligned accesses.
    // !! 4. Must be POD types; only use non-const size_t and pointer types; no access modifiers.
    // !! 5. Do not reorder fields or change any existing field types.
    // !! 6. Add static asserts for fields you add.
};
#pragma pack(pop)
static_assert(_HOST_INTERFACE_PACK == 1, "Packing size should not be modified for back compat");
static_assert(offsetof(host_interface_t, version_lo) == 0 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, version_hi) == 1 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, config_keys) == 2 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, config_values) == 4 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_dir) == 6 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_name) == 7 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, deps_file) == 8 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, is_framework_dependent) == 9 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, probe_paths) == 10 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, patch_roll_forward) == 12 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, prerelease_roll_forward) == 13 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_mode) == 14 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, tfm) == 15 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, additional_deps_serialized) == 16 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_ver) == 17 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_names) == 18 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_dirs) == 20 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_requested_versions) == 22 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_found_versions) == 24 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_command) == 26 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_host_path) == 27 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_dotnet_root) == 28 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_app_path) == 29 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, single_file_bundle_header_offset) == 30 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(sizeof(host_interface_t) == 31 * sizeof(size_t), "Did you add static asserts for the newly added fields?");

#define HOST_INTERFACE_LAYOUT_VERSION_HI 0x16041101 // YYMMDD:nn always increases when layout breaks compat.
#define HOST_INTERFACE_LAYOUT_VERSION_LO sizeof(host_interface_t)

#endif // __HOST_INTERFACE_H__
