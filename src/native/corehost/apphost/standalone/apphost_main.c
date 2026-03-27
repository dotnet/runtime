// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_pal.h"
#include "apphost_trace.h"
#include "apphost_utils.h"
#include "apphost_bundle_marker.h"
#include "apphost_hostfxr_resolver.h"
#include "error_codes.h"
#include "hostfxr.h"

#include <string.h>
#include <stdio.h>
#include <inttypes.h>

/**
 * Detect if the apphost executable is allowed to load and execute a managed assembly.
 *
 *    - The exe is built with a known hash string at some offset in the image
 *    - The exe is useless as is with the built-in hash value, and will fail with an error message
 *    - The hash value should be replaced with the managed DLL filename with optional relative path
 *    - The optional path is relative to the location of the apphost executable
 *    - The relative path plus filename are verified to reference a valid file
 *    - The filename should be "NUL terminated UTF-8" by "dotnet build"
 *    - The managed DLL filename does not have to be the same name as the apphost executable name
 *    - The exe may be signed at this point by the app publisher
 *    - Note: the maximum size of the filename and relative path is 1024 bytes in UTF-8 (not including NUL)
 *        o https://en.wikipedia.org/wiki/Comparison_of_file_systems
 *          has more details on maximum file name sizes.
 */
#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89" // SHA-256 of "foobar" in UTF-8
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8    (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8) // NUL terminated

#define EMBED_SZ (int)(sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]))
#define EMBED_MAX (EMBED_SZ > 1025 ? EMBED_SZ : 1025) // 1024 DLL name length, 1 NUL

// This avoids compiler optimization which cause EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8
// to be placed adjacent causing them to match EMBED_HASH_FULL_UTF8 when searched for replacing.
// See https://github.com/dotnet/runtime/issues/109611 for more details.
static bool compare_memory_nooptimization(volatile const char* a, volatile const char* b, size_t length)
{
    for (size_t i = 0; i < length; i++)
    {
        if (*a++ != *b++)
            return false;
    }
    return true;
}

static bool is_exe_enabled_for_execution(char* app_dll, size_t app_dll_len)
{
    // Contains the EMBED_HASH_FULL_UTF8 value at compile time or the managed DLL name replaced by "dotnet build".
    // Must not be 'const' because strlen below could be determined at compile time (=64) instead of the actual
    // length of the string at runtime.
    static char embed[EMBED_MAX] = EMBED_HASH_FULL_UTF8;

    static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
    static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

    size_t binding_len = strlen(&embed[0]);

    if (binding_len >= app_dll_len)
    {
        trace_error("The managed DLL bound to this executable could not be retrieved from the executable image.");
        return false;
    }

    memcpy(app_dll, embed, binding_len + 1);

    // Check if the path exceeds the max allowed size
    if (binding_len > EMBED_MAX - 1)
    {
        trace_error("The managed DLL bound to this executable is longer than the max allowed length (%d)", EMBED_MAX - 1);
        return false;
    }

    // Check if the value is the same as the placeholder
    size_t hi_len = sizeof(hi_part) - 1;
    size_t lo_len = sizeof(lo_part) - 1;
    if (binding_len >= (hi_len + lo_len)
        && compare_memory_nooptimization(&embed[0], hi_part, hi_len)
        && compare_memory_nooptimization(&embed[hi_len], lo_part, lo_len))
    {
        trace_error("This executable is not bound to a managed DLL to execute. The binding value is: '%s'", app_dll);
        return false;
    }

    trace_info("The managed DLL bound to this executable is: '%s'", app_dll);
    return true;
}

static void need_newer_framework_error(const char* dotnet_root, const char* host_path)
{
    char download_url[1024];
    utils_get_download_url(download_url, sizeof(download_url));

    trace_error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_OR_UPDATE_NET_ERROR_MESSAGE,
        host_path,
        utils_get_current_arch_name(),
        _STRINGIFY(HOST_VERSION),
        dotnet_root,
        download_url,
        _STRINGIFY(HOST_VERSION));
}

// C equivalent of propagate_error_writer_t
typedef struct {
    hostfxr_set_error_writer_fn set_error_writer;
    bool error_writer_set;
} propagate_error_writer_state_t;

static void propagate_error_writer_init(propagate_error_writer_state_t* state, hostfxr_set_error_writer_fn set_error_writer)
{
    trace_flush();

    state->set_error_writer = set_error_writer;
    state->error_writer_set = false;

    trace_error_writer_fn error_writer = trace_get_error_writer();
    if (error_writer != NULL && set_error_writer != NULL)
    {
        set_error_writer((hostfxr_error_writer_fn)error_writer);
        state->error_writer_set = true;
    }
}

static void propagate_error_writer_cleanup(propagate_error_writer_state_t* state)
{
    if (state->error_writer_set && state->set_error_writer != NULL)
    {
        state->set_error_writer(NULL);
        state->error_writer_set = false;
    }
}

static int exe_start(const int argc, const char* argv[])
{
    // Use realpath to find the path of the host, resolving any symlinks.
    char host_path[APPHOST_PATH_MAX];
    if (!pal_get_own_executable_path(host_path, sizeof(host_path)) || !pal_fullpath(host_path, sizeof(host_path)))
    {
        trace_error("Failed to resolve full path of the current executable [%s]", host_path);
        return CurrentHostFindFailure;
    }

    char app_path[APPHOST_PATH_MAX];
    char app_root[APPHOST_PATH_MAX];
    bool requires_hostfxr_startupinfo_interface = false;

    // FEATURE_APPHOST path
    char embedded_app_name[EMBED_MAX];
    if (!is_exe_enabled_for_execution(embedded_app_name, sizeof(embedded_app_name)))
    {
        return AppHostExeNotBoundFailure;
    }

    if (strchr(embedded_app_name, '/') != NULL)
    {
        requires_hostfxr_startupinfo_interface = true;
    }

    utils_get_directory(host_path, app_path, sizeof(app_path));
    utils_append_path(app_path, sizeof(app_path), embedded_app_name);

    if (bundle_marker_is_bundle())
    {
        trace_info("Detected Single-File app bundle");
    }
    else if (!pal_fullpath(app_path, sizeof(app_path)))
    {
        trace_error("The application to execute does not exist: '%s'.", app_path);
        return AppPathFindFailure;
    }

    utils_get_directory(app_path, app_root, sizeof(app_root));

    hostfxr_resolver_t fxr;
    hostfxr_resolver_init(&fxr, app_root);

    int rc = fxr.status_code;
    if (rc != Success)
    {
        hostfxr_resolver_cleanup(&fxr);
        return rc;
    }

    if (bundle_marker_is_bundle())
    {
        hostfxr_main_bundle_startupinfo_fn hostfxr_main_bundle_startupinfo = hostfxr_resolver_resolve_main_bundle_startupinfo(&fxr);
        if (hostfxr_main_bundle_startupinfo != NULL)
        {
            const char* host_path_cstr = host_path;
            const char* dotnet_root_cstr = fxr.dotnet_root[0] != '\0' ? fxr.dotnet_root : NULL;
            const char* app_path_cstr = app_path[0] != '\0' ? app_path : NULL;
            int64_t bundle_header_offset = bundle_marker_header_offset();

            trace_info("Invoking fx resolver [%s] hostfxr_main_bundle_startupinfo", fxr.fxr_path);
            trace_info("Host path: [%s]", host_path);
            trace_info("Dotnet path: [%s]", fxr.dotnet_root);
            trace_info("App path: [%s]", app_path);
            trace_info("Bundle Header Offset: [%" PRId64 "]", bundle_header_offset);

            hostfxr_set_error_writer_fn set_error_writer = hostfxr_resolver_resolve_set_error_writer(&fxr);
            propagate_error_writer_state_t propagate_state;
            propagate_error_writer_init(&propagate_state, set_error_writer);
            rc = hostfxr_main_bundle_startupinfo(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr, bundle_header_offset);
            propagate_error_writer_cleanup(&propagate_state);
        }
        else
        {
            trace_error("The required library %s does not support single-file apps.", fxr.fxr_path);
            need_newer_framework_error(fxr.dotnet_root, host_path);
            rc = FrameworkMissingFailure;
        }
    }
    else
    {
        hostfxr_main_startupinfo_fn hostfxr_main_startupinfo = hostfxr_resolver_resolve_main_startupinfo(&fxr);
        if (hostfxr_main_startupinfo != NULL)
        {
            const char* host_path_cstr = host_path;
            const char* dotnet_root_cstr = fxr.dotnet_root[0] != '\0' ? fxr.dotnet_root : NULL;
            const char* app_path_cstr = app_path[0] != '\0' ? app_path : NULL;

            trace_info("Invoking fx resolver [%s] hostfxr_main_startupinfo", fxr.fxr_path);
            trace_info("Host path: [%s]", host_path);
            trace_info("Dotnet path: [%s]", fxr.dotnet_root);
            trace_info("App path: [%s]", app_path);

            hostfxr_set_error_writer_fn set_error_writer = hostfxr_resolver_resolve_set_error_writer(&fxr);
            propagate_error_writer_state_t propagate_state;
            propagate_error_writer_init(&propagate_state, set_error_writer);

            rc = hostfxr_main_startupinfo(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr);

            if (trace_get_error_writer() != NULL && rc == (int)FrameworkMissingFailure && set_error_writer == NULL)
            {
                need_newer_framework_error(fxr.dotnet_root, host_path);
            }

            propagate_error_writer_cleanup(&propagate_state);
        }
        else
        {
            if (requires_hostfxr_startupinfo_interface)
            {
                trace_error("The required library %s does not support relative app dll paths.", fxr.fxr_path);
                rc = CoreHostEntryPointFailure;
            }
            else
            {
                trace_info("Invoking fx resolver [%s] v1", fxr.fxr_path);

                // Previous corehost trace messages must be printed before calling trace::setup in hostfxr
                trace_flush();

                hostfxr_main_fn main_fn_v1 = hostfxr_resolver_resolve_main_v1(&fxr);
                if (main_fn_v1 != NULL)
                {
                    rc = main_fn_v1(argc, argv);
                }
                else
                {
                    trace_error("The required library %s does not contain the expected entry point.", fxr.fxr_path);
                    rc = CoreHostEntryPointFailure;
                }
            }
        }
    }

    hostfxr_resolver_cleanup(&fxr);
    return rc;
}

int main(const int argc, const char* argv[])
{
    trace_setup();

    if (trace_is_enabled())
    {
        char version_desc[256];
        utils_get_host_version_description(version_desc, sizeof(version_desc));
        trace_info("--- Invoked apphost [version: %s] main = {", version_desc);
        for (int i = 0; i < argc; ++i)
        {
            trace_info("%s", argv[i]);
        }
        trace_info("}");
    }

    int exit_code = exe_start(argc, argv);

    trace_flush();

    return exit_code;
}
