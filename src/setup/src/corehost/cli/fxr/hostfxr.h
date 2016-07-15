// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stddef.h>

#define _HOSTFXR_INTERFACE_PACK 1
#pragma pack(push, _HOSTFXR_INTERFACE_PACK)
struct hostfxr_interface_t
{
    size_t version_lo;                // Just assign sizeof() to this field.
    size_t version_hi;                // Breaking changes to the layout -- increment HOSTFXR_INTERFACE_LAYOUT_VERSION
    const pal::char_t* exe_version;
    const pal::char_t* exe_commit;
    const pal::char_t* exe_type;
    // !! WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING
    // !! 1. Only append to this structure to maintain compat.
    // !! 2. Any nested structs should not use compiler specific padding (pack with _HOSTFXR_INTERFACE_PACK)
    // !! 3. Do not take address of the fields of this struct or be prepared to deal with unaligned accesses.
    // !! 4. Must be POD types; only use non-const size_t and pointer types; no access modifiers.
    // !! 5. Do not reorder fields or change any existing field types.
    // !! 6. Add static asserts for fields you add.
};
#pragma pack(pop)
static_assert(_HOSTFXR_INTERFACE_PACK == 1, "Packing size should not be modified for back compat");
static_assert(offsetof(hostfxr_interface_t, version_lo) == 0 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(hostfxr_interface_t, version_hi) == 1 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(hostfxr_interface_t, exe_version) == 2 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(hostfxr_interface_t, exe_commit) == 3 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(hostfxr_interface_t, exe_type) == 4 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(sizeof(hostfxr_interface_t) == 5 * sizeof(size_t), "Did you add static asserts for the newly added fields?");

#define HOSTFXR_INTERFACE_LAYOUT_VERSION_HI 0x16071301 // YYMMDD:nn always increases when layout breaks compat.
#define HOSTFXR_INTERFACE_LAYOUT_VERSION_LO sizeof(hostfxr_interface_t)

struct hostfxr_init_t
{
    // !! NOTE: All these values may be unitialized if an older "dotnet.exe" that hasn't seen the fields, invokes hostfxr.dll
    pal::string_t exe_version;
    pal::string_t exe_commit;
    pal::string_t exe_type;
};

