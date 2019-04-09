// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HOSTPOLICY_CONTEXT_H__
#define __HOSTPOLICY_CONTEXT_H__

#include <pal.h>

#include "args.h"
#include "coreclr.h"
#include "hostpolicy_init.h"

struct hostpolicy_context_t
{
public:
    pal::string_t application;
    pal::string_t clr_dir;
    pal::string_t clr_path;
    pal::string_t host_path;

    bool breadcrumbs_enabled;
    std::unordered_set<pal::string_t> breadcrumbs;

    coreclr_property_bag_t coreclr_properties;

    int initialize(hostpolicy_init_t &hostpolicy_init, const arguments_t &args, bool enable_breadcrumbs);
};

#endif // __HOSTPOLICY_CONTEXT_H__
