// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FX_REFERENCE_H__
#define __FX_REFERENCE_H__

#include <list>
#include "pal.h"
#include "fx_ver.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

class fx_reference_t
{
public:
    fx_reference_t()
        : fx_name(_X(""))
        , fx_version(_X(""))
        , fx_version_number()
        , has_patch_roll_fwd(false)
        , patch_roll_fwd(false)
        , has_roll_fwd_on_no_candidate_fx(false)
        , use_exact_version(false)
        , roll_fwd_on_no_candidate_fx((roll_fwd_on_no_candidate_fx_option)0)
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

    const bool* get_patch_roll_fwd() const
    {
        return (has_patch_roll_fwd ? &patch_roll_fwd : nullptr);
    }
    void set_patch_roll_fwd(bool value)
    {
        has_patch_roll_fwd = true;
        patch_roll_fwd = value;
    }

    const bool get_use_exact_version() const
    {
        return use_exact_version;
    }
    void set_use_exact_version(bool value)
    {
        use_exact_version = value;
    }

    const roll_fwd_on_no_candidate_fx_option* get_roll_fwd_on_no_candidate_fx() const
    {
        return (has_roll_fwd_on_no_candidate_fx ? &roll_fwd_on_no_candidate_fx : nullptr);
    }
    void set_roll_fwd_on_no_candidate_fx(roll_fwd_on_no_candidate_fx_option value)
    {
        has_roll_fwd_on_no_candidate_fx = true;
        roll_fwd_on_no_candidate_fx = value;
    }

    // Is the current version compatible with another instance with roll-forward semantics.
    bool is_roll_forward_compatible(const fx_ver_t& other) const;

    // Copy over any non-null values
    void apply_settings_from(const fx_reference_t& from);

    // Apply the most restrictive settings
    void merge_roll_forward_settings_from(const fx_reference_t& from);

private:
    bool has_patch_roll_fwd;
    bool patch_roll_fwd;

    bool has_roll_fwd_on_no_candidate_fx;
    roll_fwd_on_no_candidate_fx_option roll_fwd_on_no_candidate_fx;

    bool use_exact_version;

    pal::string_t fx_name;

    pal::string_t fx_version;
    fx_ver_t fx_version_number;
};

typedef std::vector<fx_reference_t> fx_reference_vector_t;
typedef std::unordered_map<pal::string_t, fx_reference_t> fx_name_to_fx_reference_map_t;

#endif // __FX_REFERENCE_H__
