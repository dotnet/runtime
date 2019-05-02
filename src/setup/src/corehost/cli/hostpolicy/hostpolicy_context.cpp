// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "hostpolicy_context.h"

#include "deps_resolver.h"
#include <error_codes.h>
#include <trace.h>

namespace
{
    void log_duplicate_property_error(const pal::char_t *property_key)
    {
        trace::error(_X("Duplicate runtime property found: %s"), property_key);
        trace::error(_X("It is invalid to specify values for properties populated by the hosting layer in the the application's .runtimeconfig.json"));
    }
}

int hostpolicy_context_t::initialize(hostpolicy_init_t &hostpolicy_init, const arguments_t &args, bool enable_breadcrumbs)
{
    application = args.managed_application;
    host_mode = hostpolicy_init.host_mode;
    host_path = args.host_path;
    breadcrumbs_enabled = enable_breadcrumbs;

    deps_resolver_t resolver
        {
            args,
            hostpolicy_init.fx_definitions,
            /* root_framework_rid_fallback_graph */ nullptr, // This means that the fx_definitions contains the root framework
            hostpolicy_init.is_framework_dependent
        };

    pal::string_t resolver_errors;
    if (!resolver.valid(&resolver_errors))
    {
        trace::error(_X("Error initializing the dependency resolver: %s"), resolver_errors.c_str());
        return StatusCode::ResolverInitFailure;
    }

    probe_paths_t probe_paths;

    // Setup breadcrumbs.
    if (breadcrumbs_enabled)
    {
        pal::string_t policy_name = _STRINGIFY(HOST_POLICY_PKG_NAME);
        pal::string_t policy_version = _STRINGIFY(HOST_POLICY_PKG_VER);

        // Always insert the hostpolicy that the code is running on.
        breadcrumbs.insert(policy_name);
        breadcrumbs.insert(policy_name + _X(",") + policy_version);

        if (!resolver.resolve_probe_paths(&probe_paths, &breadcrumbs))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }
    else
    {
        if (!resolver.resolve_probe_paths(&probe_paths, nullptr))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }

    clr_path = probe_paths.coreclr;
    if (clr_path.empty() || !pal::realpath(&clr_path))
    {
        trace::error(_X("Could not resolve CoreCLR path. For more details, enable tracing by setting COREHOST_TRACE environment variable to 1"));
        return StatusCode::CoreClrResolveFailure;
    }

    // Get path in which CoreCLR is present.
    clr_dir = get_directory(clr_path);

    // System.Private.CoreLib.dll is expected to be next to CoreCLR.dll - add its path to the TPA list.
    pal::string_t corelib_path = clr_dir;
    append_path(&corelib_path, CORELIB_NAME);

    // Append CoreLib path
    if (!probe_paths.tpa.empty() && probe_paths.tpa.back() != PATH_SEPARATOR)
    {
        probe_paths.tpa.push_back(PATH_SEPARATOR);
    }

    probe_paths.tpa.append(corelib_path);

    pal::string_t clrjit_path = probe_paths.clrjit;
    if (clrjit_path.empty())
    {
        trace::warning(_X("Could not resolve CLRJit path"));
    }
    else if (pal::realpath(&clrjit_path))
    {
        trace::verbose(_X("The resolved JIT path is '%s'"), clrjit_path.c_str());
    }
    else
    {
        clrjit_path.clear();
        trace::warning(_X("Could not resolve symlink to CLRJit path '%s'"), probe_paths.clrjit.c_str());
    }

    const fx_definition_vector_t &fx_definitions = resolver.get_fx_definitions();

    pal::string_t fx_deps_str;
    if (resolver.is_framework_dependent())
    {
        // Use the root fx to define FX_DEPS_FILE
        fx_deps_str = get_root_framework(fx_definitions).get_deps_file();
    }

    fx_definition_vector_t::iterator fx_begin;
    fx_definition_vector_t::iterator fx_end;
    resolver.get_app_fx_definition_range(&fx_begin, &fx_end);

    pal::string_t app_context_deps_str;
    fx_definition_vector_t::iterator fx_curr = fx_begin;
    while (fx_curr != fx_end)
    {
        if (fx_curr != fx_begin)
            app_context_deps_str += _X(';');

        app_context_deps_str += (*fx_curr)->get_deps_file();
        ++fx_curr;
    }

    pal::string_t clr_library_version;
    if (resolver.is_framework_dependent())
    {
        clr_library_version = get_root_framework(fx_definitions).get_found_version();
    }
    else
    {
        clr_library_version = resolver.get_coreclr_library_version();
    }

    // Build properties for CoreCLR instantiation
    pal::string_t app_base = resolver.get_app_dir();
    coreclr_properties.add(common_property::TrustedPlatformAssemblies, probe_paths.tpa.c_str());
    coreclr_properties.add(common_property::NativeDllSearchDirectories, probe_paths.native.c_str());
    coreclr_properties.add(common_property::PlatformResourceRoots, probe_paths.resources.c_str());
    coreclr_properties.add(common_property::AppDomainCompatSwitch, _X("UseLatestBehaviorWhenTFMNotSpecified"));
    coreclr_properties.add(common_property::AppContextBaseDirectory, app_base.c_str());
    coreclr_properties.add(common_property::AppContextDepsFiles, app_context_deps_str.c_str());
    coreclr_properties.add(common_property::FxDepsFile, fx_deps_str.c_str());
    coreclr_properties.add(common_property::ProbingDirectories, resolver.get_lookup_probe_directories().c_str());
    coreclr_properties.add(common_property::FxProductVersion, clr_library_version.c_str());

    if (!clrjit_path.empty())
        coreclr_properties.add(common_property::JitPath, clrjit_path.c_str());

    bool set_app_paths = false;

    // Runtime options config properties.
    for (size_t i = 0; i < hostpolicy_init.cfg_keys.size(); ++i)
    {
        // Provide opt-in compatible behavior by using the switch to set APP_PATHS
        const pal::char_t *key = hostpolicy_init.cfg_keys[i].c_str();
        if (pal::strcasecmp(key, _X("Microsoft.NETCore.DotNetHostPolicy.SetAppPaths")) == 0)
        {
            set_app_paths = (pal::strcasecmp(hostpolicy_init.cfg_values[i].data(), _X("true")) == 0);
        }

        if (!coreclr_properties.add(key, hostpolicy_init.cfg_values[i].c_str()))
        {
            log_duplicate_property_error(key);
            return StatusCode::LibHostDuplicateProperty;
        }
    }

    // App paths and App NI paths.
    // Note: Keep this check outside of the loop above since the _last_ key wins
    // and that could indicate the app paths shouldn't be set.
    if (set_app_paths)
    {
        if (!coreclr_properties.add(common_property::AppPaths, app_base.c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::AppPaths));
            return StatusCode::LibHostDuplicateProperty;
        }

        if (!coreclr_properties.add(common_property::AppNIPaths, app_base.c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::AppNIPaths));
            return StatusCode::LibHostDuplicateProperty;
        }
    }

    // Startup hooks
    pal::string_t startup_hooks;
    if (pal::getenv(_X("DOTNET_STARTUP_HOOKS"), &startup_hooks))
    {
        if (!coreclr_properties.add(common_property::StartUpHooks, startup_hooks.c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::StartUpHooks));
            return StatusCode::LibHostDuplicateProperty;
        }
    }

    return StatusCode::Success;
}
