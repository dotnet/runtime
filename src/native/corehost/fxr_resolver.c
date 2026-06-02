// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementation of the fxr_resolver_* APIs (the C++ fxr_resolver:: namespace
// in fxr_resolver.cpp now delegates here). try_get_existing_fxr stays in C++
// because pal::get_loaded_library is templated/typed for C++ callers.

#include "fxr_resolver.h"

#include "fx_ver.h"
#include "trace.h"
#include "utils.h"

#include <stdlib.h>
#include <string.h>

// The default search set is app-local + environment variables + global install
// locations. Mirrors the C++ s_default_search.
#define FXR_SEARCH_LOCATION_DEFAULT_SET \
    (fxr_search_location_app_local \
     | fxr_search_location_environment_variable \
     | fxr_search_location_global)

// Grow-style append. No-op if addition is NULL or empty (some PAL APIs return
// NULL to signal "not set"). *buf must be non-NULL on entry (caller seeds via
// pal_strdup or equivalent). Mirrors std::wstring::append in the C++ original:
// on OOM, realloc returns NULL and the subsequent memcpy crashes, which is the
// same observable outcome as a C++ std::bad_alloc terminate.
static void append_str(pal_char_t** buf, const pal_char_t* addition)
{
    if (addition == NULL || addition[0] == _X('\0'))
        return;

    size_t cur_len = pal_strlen(*buf);
    size_t add_len = pal_strlen(addition);

    *buf = (pal_char_t*)realloc(*buf, (cur_len + add_len + 1) * sizeof(pal_char_t));
    memcpy(*buf + cur_len, addition, (add_len + 1) * sizeof(pal_char_t));
}

typedef struct
{
    c_fx_ver_t max_ver;
} find_max_version_context_t;

static bool find_max_version_callback(const pal_char_t* entry_name, void* ctx_in)
{
    find_max_version_context_t* ctx = (find_max_version_context_t*)ctx_in;

    trace_info(_X("Considering fxr version=[%s]..."), entry_name);

    c_fx_ver_t ver;
    c_fx_ver_init(&ver);
    if (!c_fx_ver_parse(entry_name, &ver, /*parse_only_production*/ false))
    {
        c_fx_ver_cleanup(&ver);
        return true;
    }

    if (!c_fx_ver_is_empty(&ctx->max_ver) && c_fx_ver_compare(&ver, &ctx->max_ver) <= 0)
    {
        c_fx_ver_cleanup(&ver);
        return true;
    }

    c_fx_ver_cleanup(&ctx->max_ver);
    ctx->max_ver = ver; // transfer ownership of pre/build
    return true;
}

// Find the latest version-numbered subdirectory of fxr_root that contains
// LIBFXR_NAME, and write its full file path to *out_fxr_path. On failure emits
// its own trace_error and returns false.
static bool get_latest_fxr(const pal_char_t* fxr_root, pal_char_t** out_fxr_path)
{
    trace_info(_X("Reading fx resolver directory=[%s]"), fxr_root);

    find_max_version_context_t ctx;
    c_fx_ver_init(&ctx.max_ver);

    pal_readdir_onlydirectories(fxr_root, find_max_version_callback, &ctx);

    if (c_fx_ver_is_empty(&ctx.max_ver))
    {
        trace_error(_X("Error: [%s] does not contain any version-numbered child folders"), fxr_root);
        c_fx_ver_cleanup(&ctx.max_ver);
        return false;
    }

    pal_char_t max_ver_str[256];
    c_fx_ver_as_str(&ctx.max_ver, max_ver_str, ARRAY_SIZE(max_ver_str));
    c_fx_ver_cleanup(&ctx.max_ver);

    size_t fxr_dir_cap = pal_strlen(fxr_root) + pal_strlen(max_ver_str) + 2; // separator + NUL
    pal_char_t* fxr_dir = (pal_char_t*)malloc(fxr_dir_cap * sizeof(pal_char_t));
    fxr_dir[0] = _X('\0');
    utils_append_path(fxr_dir, fxr_dir_cap, fxr_root);
    utils_append_path(fxr_dir, fxr_dir_cap, max_ver_str);

    trace_info(_X("Detected latest fxr version=[%s]..."), fxr_dir);

    *out_fxr_path = utils_file_exists_in_dir_alloc(fxr_dir, LIBFXR_NAME);
    if (*out_fxr_path == NULL)
    {
        trace_error(_X("Error: the required library %s could not be found in [%s]"), LIBFXR_NAME, fxr_dir);
        free(fxr_dir);
        return false;
    }

    trace_info(_X("Resolved fxr [%s]..."), *out_fxr_path);
    free(fxr_dir);
    return true;
}

// Build the "you need to install .NET" diagnostic message and emit it via
// trace_error. Used when no fxr was found. Owns all of its allocations.
static void emit_missing_runtime_error(
    const pal_char_t* root_path,
    fxr_search_location search,
    const pal_char_t* app_relative_dotnet_root,
    const pal_char_t* env_var_name,
    const pal_char_t* dotnet_root)
{
    bool search_app_local = (search & fxr_search_location_app_local) != 0;
    bool search_app_relative = (search & fxr_search_location_app_relative) != 0
        && app_relative_dotnet_root != NULL && app_relative_dotnet_root[0] != _X('\0');
    bool search_env = (search & fxr_search_location_environment_variable) != 0;
    bool search_global = (search & fxr_search_location_global) != 0;

    trace_verbose(_X("The required library %s could not be found. Search location options [0x%x]"),
                  LIBFXR_NAME, (unsigned int)search);

    pal_char_t* host_path = pal_get_own_executable_path();
    pal_char_t* location = pal_strdup(_X("Not found"));
    pal_char_t* searched_locations = pal_strdup(_X("The following locations were searched:"));
    pal_char_t* self_registered_dir = NULL;
    pal_char_t* registered_config_location = NULL;
    pal_char_t* default_install_location = NULL;
    pal_char_t* download_url = NULL;

    if (search != FXR_SEARCH_LOCATION_DEFAULT_SET)
    {
        append_str(&location, _X(" - search options: ["));
        if (search_app_local)    append_str(&location, _X(" app_local"));
        if (search_app_relative) append_str(&location, _X(" app_relative"));
        if (search_env)          append_str(&location, _X(" environment_variable"));
        if (search_global)       append_str(&location, _X(" global"));
        append_str(&location, _X(" ]"));

        if (search_app_relative)
        {
            append_str(&location, _X(", app-relative path: "));
            append_str(&location, app_relative_dotnet_root);
        }
    }

    if (search_app_local && root_path != NULL && root_path[0] != _X('\0'))
    {
        append_str(&searched_locations, _X("\n  Application directory:\n    "));
        append_str(&searched_locations, root_path);
    }

    if (search_app_relative)
    {
        append_str(&searched_locations, _X("\n  App-relative location:\n    "));
        append_str(&searched_locations, app_relative_dotnet_root);
    }

    if (search_env)
    {
        append_str(&searched_locations, _X("\n  Environment variable:\n    "));
        if (env_var_name == NULL)
        {
            append_str(&searched_locations, DOTNET_ROOT_ARCH_ENV_VAR);
            append_str(&searched_locations, _X(" = <not set>\n    "));
            append_str(&searched_locations, DOTNET_ROOT_ENV_VAR _X(" = <not set>"));
        }
        else
        {
            append_str(&searched_locations, env_var_name);
            append_str(&searched_locations, _X(" = "));
            append_str(&searched_locations, dotnet_root);
        }
    }

    // Global locations are only listed if environment variables are not set.
    if (search_global && env_var_name == NULL)
    {
        append_str(&searched_locations, _X("\n  Registered location:\n    "));
        registered_config_location = pal_get_dotnet_self_registered_config_location();
        append_str(&searched_locations, registered_config_location);

        self_registered_dir = pal_get_dotnet_self_registered_dir();
        bool self_registered_empty = self_registered_dir == NULL || self_registered_dir[0] == _X('\0');
        if (!self_registered_empty)
        {
            append_str(&searched_locations, _X(" = "));
            append_str(&searched_locations, self_registered_dir);
        }
        else
        {
            append_str(&searched_locations, _X(" = <not set>"));
        }

        // Default install location is only searched if self-registered location is not set.
        if (self_registered_empty)
        {
            default_install_location = pal_get_default_installation_dir();
            append_str(&searched_locations, _X("\n  Default location:\n    "));
            append_str(&searched_locations, default_install_location);
        }
    }

    append_str(&location, _X("\n\n"));
    append_str(&location, searched_locations);

    download_url = utils_get_download_url(NULL, NULL);

    trace_error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_NET_ERROR_MESSAGE,
        host_path != NULL ? host_path : _X(""),
        _STRINGIFY(CURRENT_ARCH_NAME),
        _STRINGIFY(HOST_VERSION),
        location,
        download_url,
        _STRINGIFY(HOST_VERSION));

    free(host_path);
    free(location);
    free(searched_locations);
    free(self_registered_dir);
    free(registered_config_location);
    free(default_install_location);
    free(download_url);
}

bool fxr_resolver_try_get_path(
    const pal_char_t* root_path,
    fxr_search_location search,
    const pal_char_t* app_relative_dotnet_root,
    pal_char_t** out_dotnet_root,
    pal_char_t** out_fxr_path)
{
    *out_dotnet_root = NULL;
    *out_fxr_path = NULL;

#if defined(FEATURE_APPHOST) || defined(FEATURE_LIBHOST)
    if (search == fxr_search_location_default)
        search = FXR_SEARCH_LOCATION_DEFAULT_SET;

    bool search_app_local = (search & fxr_search_location_app_local) != 0;
    bool search_app_relative = (search & fxr_search_location_app_relative) != 0
        && app_relative_dotnet_root != NULL && app_relative_dotnet_root[0] != _X('\0');
    bool search_env = (search & fxr_search_location_environment_variable) != 0;
    bool search_global = (search & fxr_search_location_global) != 0;

    // For apphost and libhost, root_path is expected to be a directory.
    // For libhost, it may be empty if app-local search is not desired (e.g.
    // com/ijw/winrt hosts, nethost when no assembly path is specified).
    // If a hostfxr exists in root_path, then assume self-contained.
    if (search_app_local && root_path != NULL && root_path[0] != _X('\0'))
    {
        pal_char_t* app_local_fxr = utils_file_exists_in_dir_alloc(root_path, LIBFXR_NAME);
        if (app_local_fxr != NULL)
        {
            trace_info(_X("Using app-local location [%s] as runtime location."), root_path);
            trace_info(_X("Resolved fxr [%s]..."), app_local_fxr);
            *out_dotnet_root = pal_strdup(root_path);
            if (*out_dotnet_root == NULL)
            {
                free(app_local_fxr);
                return false;
            }

            *out_fxr_path = app_local_fxr;
            return true;
        }
    }

    // Check in priority order (if specified by search location options):
    //   - App-relative .NET root location
    //   - Environment variables: DOTNET_ROOT_<ARCH> and DOTNET_ROOT
    //   - Global installs:
    //      - self-registered install location
    //      - default install location
    // Once a branch picks a dotnet_root, subsequent branches are skipped
    // (dotnet_root != NULL is the cascade gate).
    const pal_char_t* env_var_name = NULL;
    pal_char_t* dotnet_root = NULL;

    if (search_app_relative)
    {
        pal_char_t* canonical_app_relative = pal_fullpath(app_relative_dotnet_root, /*skip_error_logging*/ false);
        if (canonical_app_relative != NULL)
        {
            trace_info(_X("Using app-relative location [%s] as runtime location."), canonical_app_relative);
            dotnet_root = pal_strdup(canonical_app_relative);
            if (dotnet_root == NULL)
            {
                free(canonical_app_relative);
                return false;
            }

            pal_char_t* app_rel_fxr = utils_file_exists_in_dir_alloc(canonical_app_relative, LIBFXR_NAME);
            free(canonical_app_relative);
            if (app_rel_fxr != NULL)
            {
                trace_info(_X("Resolved fxr [%s]..."), app_rel_fxr);
                *out_dotnet_root = dotnet_root;
                *out_fxr_path = app_rel_fxr;
                return true;
            }
        }
    }

    if (dotnet_root == NULL && search_env)
    {
        if (utils_get_dotnet_root_from_env(&env_var_name, &dotnet_root))
        {
            trace_info(_X("Using environment variable %s=[%s] as runtime location."), env_var_name, dotnet_root);
        }
    }

    if (dotnet_root == NULL && search_global)
    {
        pal_char_t* global = pal_get_dotnet_self_registered_dir();
        if (global == NULL)
            global = pal_get_default_installation_dir();

        if (global != NULL)
        {
            trace_info(_X("Using global install location [%s] as runtime location."), global);
            dotnet_root = global; // transfer ownership
        }
        else
        {
            trace_error(_X("Error: the default install location cannot be obtained."));
            // env_var_name and dotnet_root are NULL on this branch.
            return false;
        }
    }

    // If any branch picked a dotnet_root, try <dotnet_root>/host/fxr/<latest>.
    if (dotnet_root != NULL)
    {
        size_t fxr_dir_cap = pal_strlen(dotnet_root) + STRING_LENGTH(_X("host")) + STRING_LENGTH(_X("fxr")) + 3; // 2 separators + NUL
        pal_char_t* fxr_dir = (pal_char_t*)malloc(fxr_dir_cap * sizeof(pal_char_t));
        fxr_dir[0] = _X('\0');
        utils_append_path(fxr_dir, fxr_dir_cap, dotnet_root);
        utils_append_path(fxr_dir, fxr_dir_cap, _X("host"));
        utils_append_path(fxr_dir, fxr_dir_cap, _X("fxr"));

        if (pal_directory_exists(fxr_dir))
        {
            // get_latest_fxr emits its own trace_error on failure; do not fall
            // through to the MISSING_RUNTIME_ERROR_FORMAT path.
            pal_char_t* fxr_path = NULL;
            bool ok = get_latest_fxr(fxr_dir, &fxr_path);
            free(fxr_dir);
            if (!ok)
            {
                free(dotnet_root);
                return false;
            }

            *out_dotnet_root = dotnet_root;
            *out_fxr_path = fxr_path;
            return true;
        }
        free(fxr_dir);
    }

    emit_missing_runtime_error(root_path, search, app_relative_dotnet_root,
                               env_var_name, dotnet_root);

    free(dotnet_root);
    return false;

#else // !FEATURE_APPHOST && !FEATURE_LIBHOST
    // Muxer: root_path is the full path to the host binary.
    (void)search;
    (void)app_relative_dotnet_root;

    pal_char_t* host_dir = utils_get_directory_alloc(root_path);
    if (host_dir == NULL)
        return false;

    if (!fxr_resolver_try_get_path_from_dotnet_root(host_dir, out_fxr_path))
    {
        free(host_dir);
        return false;
    }

    *out_dotnet_root = host_dir;
    return true;
#endif // !FEATURE_APPHOST && !FEATURE_LIBHOST
}

bool fxr_resolver_try_get_path_from_dotnet_root(
    const pal_char_t* dotnet_root,
    pal_char_t** out_fxr_path)
{
    *out_fxr_path = NULL;

    size_t fxr_dir_cap = pal_strlen(dotnet_root) + STRING_LENGTH(_X("host")) + STRING_LENGTH(_X("fxr")) + 3; // 2 separators + NUL
    pal_char_t* fxr_dir = (pal_char_t*)malloc(fxr_dir_cap * sizeof(pal_char_t));
    fxr_dir[0] = _X('\0');
    utils_append_path(fxr_dir, fxr_dir_cap, dotnet_root);
    utils_append_path(fxr_dir, fxr_dir_cap, _X("host"));
    utils_append_path(fxr_dir, fxr_dir_cap, _X("fxr"));

    if (!pal_directory_exists(fxr_dir))
    {
        trace_error(_X("Error: [%s] does not exist"), fxr_dir);
        free(fxr_dir);
        return false;
    }

    bool ok = get_latest_fxr(fxr_dir, out_fxr_path);
    free(fxr_dir);
    return ok;
}
