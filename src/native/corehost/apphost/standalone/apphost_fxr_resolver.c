// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_fxr_resolver.h"
#include "apphost_pal.h"
#include "apphost_trace.h"
#include "apphost_utils.h"
#include "apphost_fx_ver.h"

#include <containers/dn-vector.h>

#include <string.h>
#include <assert.h>

// Context for collecting directory entries
typedef struct {
    dn_vector_t* entries; // vector of char* (heap-allocated strings)
    const char* base_path;
} readdir_context_t;

static bool collect_directory_entry(const char* entry_name, void* ctx)
{
    readdir_context_t* context = (readdir_context_t*)ctx;

    // Build full path: base_path/entry_name
    size_t base_len = strlen(context->base_path);
    size_t name_len = strlen(entry_name);
    size_t total = base_len + 1 + name_len + 1;

    char* full_path = (char*)malloc(total);
    if (full_path == NULL)
        return true; // continue iteration even on alloc failure

    memcpy(full_path, context->base_path, base_len);
    if (base_len > 0 && context->base_path[base_len - 1] != '/')
    {
        full_path[base_len] = '/';
        memcpy(full_path + base_len + 1, entry_name, name_len + 1);
    }
    else
    {
        memcpy(full_path + base_len, entry_name, name_len + 1);
    }

    dn_vector_push_back(context->entries, full_path);

    return true; // continue iteration
}

static void free_string_entry(void* data)
{
    char* str = *(char**)data;
    free(str);
}

static bool get_latest_fxr(const char* fxr_root, char* out_fxr_path, size_t out_fxr_path_len)
{
    trace_info("Reading fx resolver directory=[%s]", fxr_root);

    // Use dn_vector to collect directory entries
    dn_vector_t* dir_entries = dn_vector_alloc(sizeof(char*));
    if (dir_entries == NULL)
        return false;

    readdir_context_t ctx;
    ctx.entries = dir_entries;
    ctx.base_path = fxr_root;

    pal_readdir_onlydirectories(fxr_root, collect_directory_entry, &ctx);

    fx_ver_t max_ver;
    fx_ver_init(&max_ver);

    uint32_t count = dn_vector_size(dir_entries);
    for (uint32_t i = 0; i < count; i++)
    {
        char* dir_path = *(char**)dn_vector_at(dir_entries, sizeof(char*), i);

        trace_info("Considering fxr version=[%s]...", dir_path);

        // Get just the filename (version part)
        char ver_name[256];
        utils_get_filename(dir_path, ver_name, sizeof(ver_name));

        fx_ver_t fx_ver;
        if (fx_ver_parse(ver_name, &fx_ver, false))
        {
            if (fx_ver_is_empty(&max_ver) || fx_ver_compare(&fx_ver, &max_ver) > 0)
                max_ver = fx_ver;
        }
    }

    // Free all collected strings
    for (uint32_t i = 0; i < count; i++)
    {
        char* str = *(char**)dn_vector_at(dir_entries, sizeof(char*), i);
        free(str);
    }
    dn_vector_free(dir_entries);

    if (fx_ver_is_empty(&max_ver))
    {
        trace_error("Error: [%s] does not contain any version-numbered child folders", fxr_root);
        return false;
    }

    char max_ver_str[128];
    fx_ver_as_str(&max_ver, max_ver_str, sizeof(max_ver_str));

    char fxr_dir[APPHOST_PATH_MAX];
    size_t root_len = strlen(fxr_root);
    if (root_len >= sizeof(fxr_dir))
        return false;
    memcpy(fxr_dir, fxr_root, root_len + 1);
    utils_append_path(fxr_dir, sizeof(fxr_dir), max_ver_str);

    trace_info("Detected latest fxr version=[%s]...", fxr_dir);

    if (utils_file_exists_in_dir(fxr_dir, LIBFXR_NAME, out_fxr_path, out_fxr_path_len))
    {
        trace_info("Resolved fxr [%s]...", out_fxr_path);
        return true;
    }

    trace_error("Error: the required library %s could not be found in [%s]", LIBFXR_NAME, fxr_dir);
    return false;
}

// The default search includes app-local, environment variables, and global install locations
static const fxr_search_location s_default_search =
    (fxr_search_location)(search_location_app_local | search_location_environment_variable | search_location_global);

bool fxr_resolver_try_get_path(
    const char* root_path,
    fxr_search_location search,
    const char* app_relative_dotnet_root,
    char* out_dotnet_root,
    size_t out_dotnet_root_len,
    char* out_fxr_path,
    size_t out_fxr_path_len)
{
    if (search == search_location_default)
        search = s_default_search;

    out_dotnet_root[0] = '\0';
    out_fxr_path[0] = '\0';

    // Check app-local first
    bool search_app_local = (search & search_location_app_local) != 0;
    if (search_app_local && root_path != NULL && root_path[0] != '\0'
        && utils_file_exists_in_dir(root_path, LIBFXR_NAME, out_fxr_path, out_fxr_path_len))
    {
        trace_info("Using app-local location [%s] as runtime location.", root_path);
        trace_info("Resolved fxr [%s]...", out_fxr_path);
        snprintf(out_dotnet_root, out_dotnet_root_len, "%s", root_path);
        return true;
    }

    bool search_app_relative = (search & search_location_app_relative) != 0
        && app_relative_dotnet_root != NULL && app_relative_dotnet_root[0] != '\0';
    bool search_env = (search & search_location_environment_variable) != 0;
    bool search_global = (search & search_location_global) != 0;

    char dotnet_root_env_var_name[256];
    dotnet_root_env_var_name[0] = '\0';

    if (search_app_relative)
    {
        char app_relative_resolved[APPHOST_PATH_MAX];
        snprintf(app_relative_resolved, sizeof(app_relative_resolved), "%s", app_relative_dotnet_root);

        if (pal_fullpath(app_relative_resolved, sizeof(app_relative_resolved)))
        {
            trace_info("Using app-relative location [%s] as runtime location.", app_relative_resolved);
            snprintf(out_dotnet_root, out_dotnet_root_len, "%s", app_relative_resolved);

            if (utils_file_exists_in_dir(app_relative_resolved, LIBFXR_NAME, out_fxr_path, out_fxr_path_len))
            {
                trace_info("Resolved fxr [%s]...", out_fxr_path);
                return true;
            }
        }
    }
    else if (search_env && utils_get_dotnet_root_from_env(dotnet_root_env_var_name, sizeof(dotnet_root_env_var_name),
        out_dotnet_root, out_dotnet_root_len))
    {
        trace_info("Using environment variable %s=[%s] as runtime location.", dotnet_root_env_var_name, out_dotnet_root);
    }
    else if (search_global)
    {
        char global_install_location[APPHOST_PATH_MAX];
        if (pal_get_dotnet_self_registered_dir(global_install_location, sizeof(global_install_location))
            || pal_get_default_installation_dir(global_install_location, sizeof(global_install_location)))
        {
            trace_info("Using global install location [%s] as runtime location.", global_install_location);
            snprintf(out_dotnet_root, out_dotnet_root_len, "%s", global_install_location);
        }
        else
        {
            trace_error("Error: the default install location cannot be obtained.");
            return false;
        }
    }

    // Look for hostfxr in <dotnet_root>/host/fxr
    char fxr_dir[APPHOST_PATH_MAX];
    snprintf(fxr_dir, sizeof(fxr_dir), "%s", out_dotnet_root);
    utils_append_path(fxr_dir, sizeof(fxr_dir), "host");
    utils_append_path(fxr_dir, sizeof(fxr_dir), "fxr");

    if (pal_directory_exists(fxr_dir))
        return get_latest_fxr(fxr_dir, out_fxr_path, out_fxr_path_len);

    // Failed to find hostfxr - build error message
    trace_verbose("The required library %s could not be found. Search location options [0x%x]", LIBFXR_NAME, search);

    char host_path[APPHOST_PATH_MAX];
    pal_get_own_executable_path(host_path, sizeof(host_path));

    // Build searched locations message
    char searched_locations[APPHOST_PATH_MAX * 2];
    snprintf(searched_locations, sizeof(searched_locations), "The following locations were searched:");

    if (search_app_local && root_path != NULL && root_path[0] != '\0')
    {
        size_t len = strlen(searched_locations);
        snprintf(searched_locations + len, sizeof(searched_locations) - len,
            "\n  Application directory:\n    %s", root_path);
    }

    if (search_app_relative && app_relative_dotnet_root != NULL)
    {
        size_t len = strlen(searched_locations);
        snprintf(searched_locations + len, sizeof(searched_locations) - len,
            "\n  App-relative location:\n    %s", app_relative_dotnet_root);
    }

    if (search_env)
    {
        size_t len = strlen(searched_locations);
        if (dotnet_root_env_var_name[0] == '\0')
        {
            char arch_env[256];
            utils_get_dotnet_root_env_var_for_arch(arch_env, sizeof(arch_env));
            snprintf(searched_locations + len, sizeof(searched_locations) - len,
                "\n  Environment variable:\n    %s = <not set>\n    %s = <not set>",
                arch_env, DOTNET_ROOT_ENV_VAR);
        }
        else
        {
            snprintf(searched_locations + len, sizeof(searched_locations) - len,
                "\n  Environment variable:\n    %s = %s",
                dotnet_root_env_var_name, out_dotnet_root);
        }
    }

    if (search_global && dotnet_root_env_var_name[0] == '\0')
    {
        size_t len = strlen(searched_locations);
        char config_loc[APPHOST_PATH_MAX];
        pal_get_dotnet_self_registered_config_location(config_loc, sizeof(config_loc));

        char self_registered_dir[APPHOST_PATH_MAX];
        if (pal_get_dotnet_self_registered_dir(self_registered_dir, sizeof(self_registered_dir))
            && self_registered_dir[0] != '\0')
        {
            snprintf(searched_locations + len, sizeof(searched_locations) - len,
                "\n  Registered location:\n    %s = %s", config_loc, self_registered_dir);
        }
        else
        {
            snprintf(searched_locations + len, sizeof(searched_locations) - len,
                "\n  Registered location:\n    %s = <not set>", config_loc);

            // Default install location
            char default_dir[APPHOST_PATH_MAX];
            if (pal_get_default_installation_dir(default_dir, sizeof(default_dir)))
            {
                size_t len2 = strlen(searched_locations);
                snprintf(searched_locations + len2, sizeof(searched_locations) - len2,
                    "\n  Default location:\n    %s", default_dir);
            }
        }
    }

    char location[APPHOST_PATH_MAX * 3];
    snprintf(location, sizeof(location), "Not found\n\n%s", searched_locations);

    char download_url[1024];
    utils_get_download_url(download_url, sizeof(download_url));

    trace_error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_NET_ERROR_MESSAGE,
        host_path,
        utils_get_current_arch_name(),
        _STRINGIFY(HOST_VERSION),
        location,
        download_url,
        _STRINGIFY(HOST_VERSION));
    return false;
}
