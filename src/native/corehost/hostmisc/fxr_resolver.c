// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "fxr_resolver_c.h"
#include "pal_c.h"
#include "trace_c.h"
#include "utils_c.h"
#include "fx_ver_c.h"

#include <dn-vector.h>

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

static bool get_latest_fxr(const char* fxr_root, char** out_fxr_path)
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

    c_fx_ver_t max_ver;
    c_fx_ver_init(&max_ver);

    uint32_t count = dn_vector_size(dir_entries);
    for (uint32_t i = 0; i < count; i++)
    {
        char* dir_path = *(char**)dn_vector_at(dir_entries, sizeof(char*), i);

        trace_info("Considering fxr version=[%s]...", dir_path);

        // Get just the filename (version part)
        char ver_name[256];
        utils_get_filename(dir_path, ver_name, sizeof(ver_name));

        c_fx_ver_t fx_ver;
        if (c_fx_ver_parse(ver_name, &fx_ver, false))
        {
            if (c_fx_ver_is_empty(&max_ver) || c_fx_ver_compare(&fx_ver, &max_ver) > 0)
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

    if (c_fx_ver_is_empty(&max_ver))
    {
        trace_error("Error: [%s] does not contain any version-numbered child folders", fxr_root);
        return false;
    }

    char max_ver_str[128];
    c_fx_ver_as_str(&max_ver, max_ver_str, sizeof(max_ver_str));

    size_t root_len = strlen(fxr_root);
    size_t ver_len = strlen(max_ver_str);
    size_t fxr_dir_len = root_len + 1 + ver_len + 1;
    char* fxr_dir = (char*)malloc(fxr_dir_len);
    if (fxr_dir == NULL)
        return false;
    snprintf(fxr_dir, fxr_dir_len, "%s", fxr_root);
    utils_append_path(fxr_dir, fxr_dir_len, max_ver_str);

    trace_info("Detected latest fxr version=[%s]...", fxr_dir);

    char* fxr_path = NULL;
    if (utils_file_exists_in_dir_alloc(fxr_dir, LIBFXR_NAME, &fxr_path))
    {
        trace_info("Resolved fxr [%s]...", fxr_path);
        *out_fxr_path = fxr_path;
        free(fxr_dir);
        return true;
    }

    trace_error("Error: the required library %s could not be found in [%s]", LIBFXR_NAME, fxr_dir);
    free(fxr_dir);
    return false;
}

// The default search includes app-local, environment variables, and global install locations
static const fxr_search_location s_default_search =
    (fxr_search_location)(search_location_app_local | search_location_environment_variable | search_location_global);

bool fxr_resolver_try_get_path(
    const char* root_path,
    fxr_search_location search,
    const char* app_relative_dotnet_root,
    char** out_dotnet_root,
    char** out_fxr_path)
{
    if (search == search_location_default)
        search = s_default_search;

    *out_dotnet_root = NULL;
    *out_fxr_path = NULL;

    // Check app-local first
    bool search_app_local = (search & search_location_app_local) != 0;
    if (search_app_local && root_path != NULL && root_path[0] != '\0')
    {
        char* fxr_path = NULL;
        if (utils_file_exists_in_dir_alloc(root_path, LIBFXR_NAME, &fxr_path))
        {
            trace_info("Using app-local location [%s] as runtime location.", root_path);
            trace_info("Resolved fxr [%s]...", fxr_path);
            *out_dotnet_root = strdup(root_path);
            *out_fxr_path = fxr_path;
            return true;
        }
    }

    bool search_app_relative = (search & search_location_app_relative) != 0
        && app_relative_dotnet_root != NULL && app_relative_dotnet_root[0] != '\0';
    bool search_env = (search & search_location_environment_variable) != 0;
    bool search_global = (search & search_location_global) != 0;

    char dotnet_root_env_var_name[256];
    dotnet_root_env_var_name[0] = '\0';

    // Temporary buffer for dotnet root (will be strdup'd to exact size)
    char* dotnet_root = NULL;

    if (search_app_relative)
    {
        char* app_relative_resolved = strdup(app_relative_dotnet_root);
        if (app_relative_resolved != NULL)
        {
            // pal_fullpath needs a buffer it can write to
            size_t resolved_len = strlen(app_relative_resolved) + APPHOST_PATH_MAX;
            char* resolved_buf = (char*)malloc(resolved_len);
            if (resolved_buf != NULL)
            {
                snprintf(resolved_buf, resolved_len, "%s", app_relative_resolved);
                if (pal_fullpath(resolved_buf, resolved_len))
                {
                    trace_info("Using app-relative location [%s] as runtime location.", resolved_buf);
                    dotnet_root = strdup(resolved_buf);

                    char* fxr_path = NULL;
                    if (utils_file_exists_in_dir_alloc(resolved_buf, LIBFXR_NAME, &fxr_path))
                    {
                        trace_info("Resolved fxr [%s]...", fxr_path);
                        *out_dotnet_root = dotnet_root;
                        *out_fxr_path = fxr_path;
                        free(resolved_buf);
                        free(app_relative_resolved);
                        return true;
                    }
                }
                free(resolved_buf);
            }
            free(app_relative_resolved);
        }
    }
    else if (search_env)
    {
        char env_value[APPHOST_PATH_MAX];
        if (utils_get_dotnet_root_from_env(dotnet_root_env_var_name, sizeof(dotnet_root_env_var_name),
            env_value, sizeof(env_value)))
        {
            dotnet_root = strdup(env_value);
            trace_info("Using environment variable %s=[%s] as runtime location.", dotnet_root_env_var_name, dotnet_root);
        }
    }
    else if (search_global)
    {
        char global_install_location[APPHOST_PATH_MAX];
        if (pal_get_dotnet_self_registered_dir(global_install_location, sizeof(global_install_location))
            || pal_get_default_installation_dir(global_install_location, sizeof(global_install_location)))
        {
            trace_info("Using global install location [%s] as runtime location.", global_install_location);
            dotnet_root = strdup(global_install_location);
        }
        else
        {
            trace_error("Error: the default install location cannot be obtained.");
            return false;
        }
    }

    if (dotnet_root == NULL)
    {
        // dotnet_root was not set - build error message
        goto error;
    }

    // Look for hostfxr in <dotnet_root>/host/fxr
    {
        size_t root_len = strlen(dotnet_root);
        size_t fxr_dir_len = root_len + strlen("/host/fxr") + 1;
        char* fxr_dir = (char*)malloc(fxr_dir_len);
        if (fxr_dir == NULL)
        {
            free(dotnet_root);
            return false;
        }
        snprintf(fxr_dir, fxr_dir_len, "%s", dotnet_root);
        utils_append_path(fxr_dir, fxr_dir_len, "host");
        utils_append_path(fxr_dir, fxr_dir_len, "fxr");

        if (pal_directory_exists(fxr_dir))
        {
            char* fxr_path = NULL;
            if (get_latest_fxr(fxr_dir, &fxr_path))
            {
                *out_dotnet_root = dotnet_root;
                *out_fxr_path = fxr_path;
                free(fxr_dir);
                return true;
            }
        }
        free(fxr_dir);
    }

error:
    // Failed to find hostfxr - build error message
    trace_verbose("The required library %s could not be found. Search location options [0x%x]", LIBFXR_NAME, search);

    {
        char host_path[APPHOST_PATH_MAX];
        pal_get_own_executable_path(host_path, sizeof(host_path));

        // Build searched locations message dynamically
        size_t msg_capacity = 2048;
        char* searched_locations = (char*)malloc(msg_capacity);
        if (searched_locations == NULL)
        {
            free(dotnet_root);
            return false;
        }
        snprintf(searched_locations, msg_capacity, "The following locations were searched:");

        if (search_app_local && root_path != NULL && root_path[0] != '\0')
        {
            size_t len = strlen(searched_locations);
            snprintf(searched_locations + len, msg_capacity - len,
                "\n  Application directory:\n    %s", root_path);
        }

        if (search_app_relative && app_relative_dotnet_root != NULL)
        {
            size_t len = strlen(searched_locations);
            snprintf(searched_locations + len, msg_capacity - len,
                "\n  App-relative location:\n    %s", app_relative_dotnet_root);
        }

        if (search_env)
        {
            size_t len = strlen(searched_locations);
            if (dotnet_root_env_var_name[0] == '\0')
            {
                char arch_env[256];
                utils_get_dotnet_root_env_var_for_arch(arch_env, sizeof(arch_env));
                snprintf(searched_locations + len, msg_capacity - len,
                    "\n  Environment variable:\n    %s = <not set>\n    %s = <not set>",
                    arch_env, DOTNET_ROOT_ENV_VAR);
            }
            else
            {
                snprintf(searched_locations + len, msg_capacity - len,
                    "\n  Environment variable:\n    %s = %s",
                    dotnet_root_env_var_name, dotnet_root != NULL ? dotnet_root : "<not set>");
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
                snprintf(searched_locations + len, msg_capacity - len,
                    "\n  Registered location:\n    %s = %s", config_loc, self_registered_dir);
            }
            else
            {
                snprintf(searched_locations + len, msg_capacity - len,
                    "\n  Registered location:\n    %s = <not set>", config_loc);

                // Default install location
                char default_dir[APPHOST_PATH_MAX];
                if (pal_get_default_installation_dir(default_dir, sizeof(default_dir)))
                {
                    size_t len2 = strlen(searched_locations);
                    snprintf(searched_locations + len2, msg_capacity - len2,
                        "\n  Default location:\n    %s", default_dir);
                }
            }
        }

        size_t loc_len = strlen(searched_locations);
        size_t location_capacity = loc_len + 32;
        char* location = (char*)malloc(location_capacity);
        if (location != NULL)
        {
            snprintf(location, location_capacity, "Not found\n\n%s", searched_locations);
        }

        char download_url[1024];
        utils_get_download_url(download_url, sizeof(download_url));

        trace_error(
            MISSING_RUNTIME_ERROR_FORMAT,
            INSTALL_NET_ERROR_MESSAGE,
            host_path,
            utils_get_current_arch_name(),
            _STRINGIFY(HOST_VERSION),
            location != NULL ? location : "Not found",
            download_url,
            _STRINGIFY(HOST_VERSION));

        free(location);
        free(searched_locations);
    }

    free(dotnet_root);
    return false;
}
