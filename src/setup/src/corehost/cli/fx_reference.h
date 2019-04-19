// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FX_REFERENCE_H__
#define __FX_REFERENCE_H__

#include <list>
#include "pal.h"
#include "fx_ver.h"
#include "roll_forward_option.h"

class fx_reference_t
{
public:
    fx_reference_t()
        : apply_patches(true)
        , roll_forward(roll_forward_option::Minor)
        , prefer_release(false)
        , fx_name(_X(""))
        , fx_version(_X(""))
        , fx_version_number()
        { }

    const pal::string_t& get_fx_name() const
    {
        return fx_name;
    }
    void set_fx_name(const pal::string_t& value)
    {
        fx_name = value;
    }

    const pal::string_t& get_fx_version() const
    {
        return fx_version;
    }
    void set_fx_version(const pal::string_t& value)
    {
        fx_version = value;

        fx_ver_t::parse(fx_version, &fx_version_number);
    }

    const fx_ver_t& get_fx_version_number() const
    {
        return fx_version_number;
    }

    const bool get_apply_patches() const
    {
        return apply_patches;
    }
    void set_apply_patches(bool value)
    {
        apply_patches = value;
    }

    const roll_forward_option get_roll_forward() const
    {
        return roll_forward;
    }
    void set_roll_forward(roll_forward_option value)
    {
        roll_forward = value;
    }

    const bool get_prefer_release() const
    {
        return prefer_release;
    }
    void set_prefer_release(bool value)
    {
        prefer_release = value;
    }

    // Is the current version compatible with the specified equal or higher version.
    bool is_compatible_with_higher_version(const fx_ver_t& higher_version) const;

    // Merge roll forward settings for two framework references
    void merge_roll_forward_settings_from(const fx_reference_t& from);

    bool operator==(const fx_reference_t& other)
    {
        return
            fx_name == other.fx_name &&
            fx_version == other.fx_version &&
            apply_patches == other.apply_patches &&
            roll_forward == other.roll_forward &&
            prefer_release == other.prefer_release;
    }

    bool operator!=(const fx_reference_t& other)
    {
        return !(*this == other);
    }

private:
    bool apply_patches;

    roll_forward_option roll_forward;

    // This indicates that when resolving the framework reference the search should prefer release version
    // and only resolve to pre-release if there's no matching release version available.
    bool prefer_release;

    pal::string_t fx_name;

    pal::string_t fx_version;
    fx_ver_t fx_version_number;
};

typedef std::vector<fx_reference_t> fx_reference_vector_t;
typedef std::unordered_map<pal::string_t, fx_reference_t> fx_name_to_fx_reference_map_t;

#endif // __FX_REFERENCE_H__
