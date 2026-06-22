// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_hostfxr_resolver.h"
#include "trace.h"
#include "utils.h"
#include "fxr_resolver.h"

#include <assert.h>
#include <string.h>

// SHA-256 of "dotnet-search" in UTF-8
#define EMBED_DOTNET_SEARCH_HI_PART_UTF8 "19ff3e9c3602ae8e841925bb461a0adb"
#define EMBED_DOTNET_SEARCH_LO_PART_UTF8 "064a1f1903667a5e0d87e8f608f425ac"

// <search_location> \0 <app_relative_dotnet_placeholder>
#define EMBED_DOTNET_SEARCH_FULL_UTF8    ("\0\0" EMBED_DOTNET_SEARCH_HI_PART_UTF8 EMBED_DOTNET_SEARCH_LO_PART_UTF8)

// Get the .NET search options that should be used.
// Returns false if options are invalid.
// out_app_relative_dotnet is a pal_char_t buffer that receives the app-relative dotnet path.
static bool try_get_dotnet_search_options(fxr_search_location* out_search_location, pal_char_t* out_app_relative_dotnet, size_t out_app_relative_dotnet_len)
{
    enum { EMBED_SIZE = 512 };

    // Contains the EMBED_DOTNET_SEARCH_FULL_UTF8 value at compile time or app-relative .NET path written by the SDK.
    // Always a narrow UTF-8 string, regardless of platform.
    static char embed[EMBED_SIZE] = EMBED_DOTNET_SEARCH_FULL_UTF8;

    *out_search_location = (fxr_search_location)embed[0];
    assert(embed[1] == 0); // NUL separates the search location and embedded .NET root value
    if ((*out_search_location & fxr_search_location_app_relative) == 0)
        return true;

    // Get the embedded app-relative .NET path (always narrow UTF-8)
    const char* binding = &embed[2];
    size_t binding_len = strlen(binding);

    // Check if the path exceeds the max allowed size
    enum { EMBED_APP_RELATIVE_DOTNET_MAX_SIZE = EMBED_SIZE - 3 }; // -2 for search location + null, -1 for null terminator
    if (binding_len > EMBED_APP_RELATIVE_DOTNET_MAX_SIZE)
    {
        trace_error(_X("The app-relative .NET path is longer than the max allowed length (%d)"), EMBED_APP_RELATIVE_DOTNET_MAX_SIZE);
        return false;
    }

    // Check if the value is empty or the same as the placeholder
    static const char hi_part[] = EMBED_DOTNET_SEARCH_HI_PART_UTF8;
    static const char lo_part[] = EMBED_DOTNET_SEARCH_LO_PART_UTF8;
    size_t hi_len = sizeof(hi_part) - 1;
    size_t lo_len = sizeof(lo_part) - 1;
    if (binding_len == 0
        || (binding_len >= (hi_len + lo_len)
            && memcmp(binding, hi_part, hi_len) == 0
            && memcmp(binding + hi_len, lo_part, lo_len) == 0))
    {
        trace_error(_X("The app-relative .NET path is not embedded."));
        return false;
    }

#if defined(_WIN32)
    // Convert UTF-8 embedded path to wide string
    if (!pal_utf8_to_palstr(binding, out_app_relative_dotnet, out_app_relative_dotnet_len))
    {
        trace_error(_X("The app-relative .NET path could not be retrieved from the executable image."));
        return false;
    }
#else
    if (binding_len >= out_app_relative_dotnet_len)
    {
        trace_error(_X("The app-relative .NET path could not be retrieved from the executable image."));
        return false;
    }
    memcpy(out_app_relative_dotnet, binding, binding_len + 1);
#endif

    trace_info(_X("Embedded app-relative .NET path: '%s'"), out_app_relative_dotnet);
    return true;
}

hostfxr_main_bundle_startupinfo_fn hostfxr_resolver_resolve_main_bundle_startupinfo(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll != NULL);
    return (hostfxr_main_bundle_startupinfo_fn)pal_get_symbol(resolver->hostfxr_dll, "hostfxr_main_bundle_startupinfo");
}

hostfxr_set_error_writer_fn hostfxr_resolver_resolve_set_error_writer(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll != NULL);
    return (hostfxr_set_error_writer_fn)pal_get_symbol(resolver->hostfxr_dll, "hostfxr_set_error_writer");
}

hostfxr_main_startupinfo_fn hostfxr_resolver_resolve_main_startupinfo(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll != NULL);
    return (hostfxr_main_startupinfo_fn)pal_get_symbol(resolver->hostfxr_dll, "hostfxr_main_startupinfo");
}

hostfxr_main_fn hostfxr_resolver_resolve_main_v1(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll != NULL);
    return (hostfxr_main_fn)pal_get_symbol(resolver->hostfxr_dll, "hostfxr_main");
}

void hostfxr_resolver_init(hostfxr_resolver_t* resolver, const pal_char_t* app_root)
{
    resolver->hostfxr_dll = NULL;
    resolver->dotnet_root = NULL;
    resolver->fxr_path = NULL;
    resolver->status_code = Success;

    fxr_search_location search_loc = fxr_search_location_default;
    pal_char_t app_relative_dotnet[512];
    app_relative_dotnet[0] = _X('\0');

    if (!try_get_dotnet_search_options(&search_loc, app_relative_dotnet, ARRAY_SIZE(app_relative_dotnet)))
    {
        resolver->status_code = AppHostExeNotBoundFailure;
        return;
    }

    trace_info(_X(".NET root search location options: %d"), search_loc);

    pal_char_t* app_relative_dotnet_path = NULL;
    if (app_relative_dotnet[0] != _X('\0'))
    {
        size_t root_len = pal_strlen(app_root);
        size_t rel_len = pal_strlen(app_relative_dotnet);
        size_t total = root_len + 1 + rel_len + 1;
        app_relative_dotnet_path = (pal_char_t*)malloc(total * sizeof(pal_char_t));
        if (app_relative_dotnet_path == NULL)
        {
            // Allocation failed - treat as hard error to preserve search semantics
            resolver->status_code = CoreHostLibMissingFailure;
            free((void*)app_root);
            return;
        }
        pal_str_printf(app_relative_dotnet_path, total, _X("%s"), app_root);
        utils_append_path(app_relative_dotnet_path, total, app_relative_dotnet);
    }

    pal_char_t* dotnet_root = NULL;
    pal_char_t* fxr_path = NULL;
    if (!fxr_resolver_try_get_path(app_root, search_loc, app_relative_dotnet_path,
        &dotnet_root, &fxr_path))
    {
        resolver->status_code = CoreHostLibMissingFailure;
    }
    else if (!pal_is_path_fully_qualified(fxr_path))
    {
        trace_error(_X("Path to %s must be fully qualified: [%s]"), LIBFXR_NAME, fxr_path);
        free(dotnet_root);
        free(fxr_path);
        resolver->status_code = CoreHostLibMissingFailure;
    }
    else if (pal_load_library(fxr_path, &resolver->hostfxr_dll))
    {
        resolver->dotnet_root = dotnet_root;
        resolver->fxr_path = fxr_path;
        resolver->status_code = Success;
    }
    else
    {
        trace_error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path);
        free(dotnet_root);
        free(fxr_path);
        resolver->status_code = CoreHostLibLoadFailure;
    }

    free(app_relative_dotnet_path);
}

void hostfxr_resolver_cleanup(hostfxr_resolver_t* resolver)
{
    if (resolver->hostfxr_dll != NULL)
    {
        pal_unload_library(resolver->hostfxr_dll);
        resolver->hostfxr_dll = NULL;
    }
    free(resolver->dotnet_root);
    resolver->dotnet_root = NULL;
    free(resolver->fxr_path);
    resolver->fxr_path = NULL;
}
