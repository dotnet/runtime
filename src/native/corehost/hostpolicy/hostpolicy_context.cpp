// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "hostpolicy_context.h"
#include "hostpolicy.h"

#include "deps_resolver.h"
#include <error_codes.h>
#include <trace.h>
#include "bundle/runner.h"
#include "bundle/file_entry.h"

namespace
{
    void log_duplicate_property_error(const pal::char_t *property_key)
    {
        trace::error(_X("Duplicate runtime property found: %s"), property_key);
        trace::error(_X("It is invalid to specify values for properties populated by the hosting layer in the application's .runtimeconfig.json"));
    }

    // bundle_probe:
    // Probe the app-bundle for the file 'path' and return its location ('offset', 'size') if found.
    //
    // This function is an API exported to the runtime via the BUNDLE_PROBE property.
    // This function used by the runtime to probe for bundled assemblies
    // This function assumes that the currently executing app is a single-file bundle.
    bool STDMETHODCALLTYPE bundle_probe(const char* path, int64_t* offset, int64_t* size, int64_t* compressedSize)
    {
        if (path == nullptr)
        {
            return false;
        }

        pal::string_t file_path;

        if (!pal::clr_palstring(path, &file_path))
        {
            trace::warning(_X("Failure probing contents of the application bundle."));
            trace::warning(_X("Failed to convert path [%hs] to UTF8"), path);

            return false;
        }

        return bundle::runner_t::app()->probe(file_path, offset, size, compressedSize);
    }

#if defined(NATIVE_LIBS_EMBEDDED)
    extern "C" const void* CompressionResolveDllImport(const char* name);
    extern "C" const void* SecurityResolveDllImport(const char* name);
    extern "C" const void* SystemResolveDllImport(const char* name);
    extern "C" const void* CryptoResolveDllImport(const char* name);
    extern "C" const void* CryptoAppleResolveDllImport(const char* name);

    // pinvoke_override:
    // Check if given function belongs to one of statically linked libraries and return a pointer if found.
    const void* STDMETHODCALLTYPE pinvoke_override(const char* library_name, const char* entry_point_name)
    {
        // This function is only called with the library name specified for a p/invoke, not any variations.
        // It must handle exact matches to the names specified. See Interop.Libraries.cs for each platform.
#if !defined(_WIN32)
        if (strcmp(library_name, LIB_NAME("System.Net.Security.Native")) == 0)
        {
            return SecurityResolveDllImport(entry_point_name);
        }

        if (strcmp(library_name, LIB_NAME("System.Native")) == 0)
        {
            return SystemResolveDllImport(entry_point_name);
        }

        if (strcmp(library_name, LIB_NAME("System.Security.Cryptography.Native.OpenSsl")) == 0)
        {
            return CryptoResolveDllImport(entry_point_name);
        }
#endif

        if (strcmp(library_name, LIB_NAME("System.IO.Compression.Native")) == 0)
        {
            return CompressionResolveDllImport(entry_point_name);
        }

        // there are two PInvokes in the hostpolicy itself, redirect them here.
        if (strcmp(library_name, LIB_NAME("hostpolicy")) == 0)
        {
            if (strcmp(entry_point_name, "corehost_resolve_component_dependencies") == 0)
            {
                return (void*)corehost_resolve_component_dependencies;
            }

            if (strcmp(entry_point_name, "corehost_set_error_writer") == 0)
            {
                return (void*)corehost_set_error_writer;
            }
        }

#if defined(TARGET_OSX)
        if (strcmp(library_name, LIB_NAME("System.Security.Cryptography.Native.Apple")) == 0)
        {
            return CryptoAppleResolveDllImport(entry_point_name);
        }
#endif

        return nullptr;
    }
#endif
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
        // in a single-file case we may not need coreclr,
        // otherwise fail early.
        if (!bundle::info_t::is_single_file_bundle())
        {
            trace::error(_X("Could not resolve CoreCLR path. For more details, enable tracing by setting COREHOST_TRACE environment variable to 1"));
            return StatusCode::CoreClrResolveFailure;
        }

        // save for tracing purposes.
        clr_dir = clr_path;
    }
    else
    {
        // Get path in which CoreCLR is present.
        clr_dir = get_directory(clr_path);
    }

    // If this is a self-contained single-file bundle,
    // System.Private.CoreLib.dll is expected to be within the bundle, unless it is explicitly excluded from the bundle.
    // In all other cases,
    // System.Private.CoreLib.dll is expected to be next to CoreCLR.dll - add its path to the TPA list.
    if (!bundle::info_t::is_single_file_bundle() ||
        bundle::runner_t::app()->probe(CORELIB_NAME) == nullptr)
    {
        pal::string_t corelib_path = clr_dir;
        append_path(&corelib_path, CORELIB_NAME);

        // Append CoreLib path
        if (!probe_paths.tpa.empty() && probe_paths.tpa.back() != PATH_SEPARATOR)
        {
            probe_paths.tpa.push_back(PATH_SEPARATOR);
        }

        probe_paths.tpa.append(corelib_path);
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
    resolver.get_app_context_deps_files_range(&fx_begin, &fx_end);

    pal::string_t app_context_deps_str;
    fx_definition_vector_t::iterator fx_curr = fx_begin;
    while (fx_curr != fx_end)
    {
        if (fx_curr != fx_begin)
            app_context_deps_str += _X(';');

        // For the application's .deps.json if this is single file, 3.1 backward compat
        // then the path used internally is the bundle path, but externally we need to report
        // the path to the extraction folder.
        if (fx_curr == fx_begin && bundle::info_t::is_single_file_bundle() && bundle::runner_t::app()->is_netcoreapp3_compat_mode())
        {
            pal::string_t deps_path = bundle::runner_t::app()->extraction_path();
            append_path(&deps_path, get_filename((*fx_curr)->get_deps_file()).c_str());
            app_context_deps_str += deps_path;
        }
        else
        {
            app_context_deps_str += (*fx_curr)->get_deps_file();
        }

        ++fx_curr;
    }

    // Build properties for CoreCLR instantiation
    pal::string_t app_base;
    resolver.get_app_dir(&app_base);
    coreclr_properties.add(common_property::TrustedPlatformAssemblies, probe_paths.tpa.c_str());
    coreclr_properties.add(common_property::NativeDllSearchDirectories, probe_paths.native.c_str());
    coreclr_properties.add(common_property::PlatformResourceRoots, probe_paths.resources.c_str());
    coreclr_properties.add(common_property::AppContextBaseDirectory, app_base.c_str());
    coreclr_properties.add(common_property::AppContextDepsFiles, app_context_deps_str.c_str());
    coreclr_properties.add(common_property::FxDepsFile, fx_deps_str.c_str());
    coreclr_properties.add(common_property::ProbingDirectories, resolver.get_lookup_probe_directories().c_str());
    coreclr_properties.add(common_property::RuntimeIdentifier, get_current_runtime_id(true /*use_fallback*/).c_str());

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

    // App paths.
    // Note: Keep this check outside of the loop above since the _last_ key wins
    // and that could indicate the app paths shouldn't be set.
    if (set_app_paths)
    {
        if (!coreclr_properties.add(common_property::AppPaths, app_base.c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::AppPaths));
            return StatusCode::LibHostDuplicateProperty;
        }
    }

    // Startup hooks
    pal::string_t startup_hooks;
    if (pal::getenv(_X("DOTNET_STARTUP_HOOKS"), &startup_hooks))
    {
        const pal::char_t *config_startup_hooks;
        if (coreclr_properties.try_get(common_property::StartUpHooks, &config_startup_hooks))
        {
            // env startup hooks shoold have precedence over config startup hooks
            // therefore append config_startup_hooks AFTER startup_hooks
            startup_hooks.push_back(PATH_SEPARATOR);
            startup_hooks.append(config_startup_hooks);
        }

        coreclr_properties.add(common_property::StartUpHooks, startup_hooks.c_str());
    }

    // Single-File Bundle Probe
    if (bundle::info_t::is_single_file_bundle())
    {
        // Encode the bundle_probe function pointer as a string, and pass it to the runtime.
        pal::stringstream_t ptr_stream;
        ptr_stream << "0x" << std::hex << (size_t)(&bundle_probe);

        if (!coreclr_properties.add(common_property::BundleProbe, ptr_stream.str().c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::BundleProbe));
            return StatusCode::LibHostDuplicateProperty;
        }
    }

#if defined(NATIVE_LIBS_EMBEDDED)
    // PInvoke Override
    if (bundle::info_t::is_single_file_bundle())
    {
        // Encode the pinvoke_override function pointer as a string, and pass it to the runtime.
        pal::stringstream_t ptr_stream;
        ptr_stream << "0x" << std::hex << (size_t)(&pinvoke_override);

        if (!coreclr_properties.add(common_property::PInvokeOverride, ptr_stream.str().c_str()))
        {
            log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::PInvokeOverride));
            return StatusCode::LibHostDuplicateProperty;
        }
    }
#endif

#if defined(HOSTPOLICY_EMBEDDED)
    if (!coreclr_properties.add(common_property::HostPolicyEmbedded, _X("true")))
    {
        log_duplicate_property_error(coreclr_property_bag_t::common_property_to_string(common_property::HostPolicyEmbedded));
        return StatusCode::LibHostDuplicateProperty;
    }
#endif

    return StatusCode::Success;
}
