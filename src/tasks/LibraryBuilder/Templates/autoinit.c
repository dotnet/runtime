// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <fcntl.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>

#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>
#include <mono/metadata/assembly.h>

#ifdef HOST_ANDROID
#include <android/log.h>

#define LOG_INFO(fmt, ...) __android_log_print(ANDROID_LOG_INFO, "MONO_SELF_CONTAINED_LIBRARY", fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "MONO_SELF_CONTAINED_LIBRARY", fmt, ##__VA_ARGS__)
#else
#include <os/log.h>

#define LOG_INFO(fmt, ...) os_log_info (OS_LOG_DEFAULT, fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) os_log_error (OS_LOG_DEFAULT, fmt, ##__VA_ARGS__)
#endif

static const char *bundle_path;

void register_aot_modules (void);
void load_assemblies_with_exported_symbols ();
typedef void (*MonoRuntimeInitCallback) (void);
void mono_set_runtime_init_callback (MonoRuntimeInitCallback callback);

static void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
    free (args);
    free (user_data);
}

static void
initialize_runtimeconfig ()
{
    char *file_name = "runtimeconfig.bin";
    size_t str_len = sizeof (char) * (strlen (bundle_path) + strlen (file_name) + 2); // +1 "/", +1 null-terminating char
    char *file_path = (char *)malloc (str_len);
    if (!file_path) {
        LOG_ERROR ("Could not allocate %zu bytes to format '%s' and '%s' together to initialize the runtime configuration.\n", str_len, bundle_path, file_name);
        abort ();
    }

    int num_char = snprintf (file_path, str_len, "%s/%s", bundle_path, file_name);
    struct stat buffer;

    if (num_char <= 0 || num_char != (str_len - 1)) {
        LOG_ERROR ("Could not format '%s' and '%s' together into \"%%s/%%s\" to initialize the runtime configuration.\n", bundle_path, file_name);
        abort ();
    }

    if (stat (file_path, &buffer) == 0) {
        MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
        arg->kind = 0;
        arg->runtimeconfig.name.path = file_path;
        monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, file_path);
    } else {
        LOG_INFO ("Could not stat file '%s'. Runtime configuration properties not initialized.\n", file_path);
        free (file_path);
    }
}

static void
initialize_appctx_env_variables ()
{
    const char *appctx_keys[2], *appctx_values[2];

    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_values[0] = "%RUNTIME_IDENTIFIER%";

    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_values[1] = bundle_path;

    monovm_initialize(2, appctx_keys, appctx_values);
}

static MonoAssembly*
mono_load_assembly (const char *name, const char *culture)
{
    char filename [1024];
    char path [1024];
    int res;

    int len = strlen (name);
    int has_extension = len > 3 && name [len - 4] == '.' && (!strcmp ("exe", name + (len - 3)) || !strcmp ("dll", name + (len - 3)));

    // add extensions if required.
    strlcpy (filename, name, sizeof (filename));
    if (!has_extension) {
        strlcat (filename, ".dll", sizeof (filename));
    }

    if (culture && strcmp (culture, ""))
        res = snprintf (path, sizeof (path) - 1, "%s/%s/%s", bundle_path, culture, filename);
    else
        res = snprintf (path, sizeof (path) - 1, "%s/%s", bundle_path, filename);

    if (res <= 0 && culture) {
        LOG_ERROR ("Could not format '%s', '%s', and '%s' together into \"%%s/%%s/%%s\" for assembly preloading.\n", bundle_path, culture, filename);
        abort ();
    }
    if (res <= 0) {
        LOG_ERROR ("Could not format '%s' and '%s' together into \"%%s/%%s\" for assembly preloading.\n", bundle_path, filename);
        abort ();
    }

    struct stat buffer;
    if (stat (path, &buffer) == 0) {
        MonoAssembly *assembly = mono_assembly_open (path, NULL);
        if (!assembly) {
            LOG_ERROR ("Could not open assembly '%s'.\n", path);
            abort ();
        }
        return assembly;
    }
    LOG_INFO ("Could not stat file '%s'. Did not successfully preload assembly.\n", path);
    return NULL;
}

static MonoAssembly*
mono_assembly_preload_hook (MonoAssemblyName *aname, char **assemblies_path, void* user_data)
{
    const char *name = mono_assembly_name_get_name (aname);
    const char *culture = mono_assembly_name_get_culture (aname);
    return mono_load_assembly (name, culture);
}

static unsigned char *
load_aot_data (MonoAssembly *assembly, int size, void *user_data, void **out_handle)
{
    *out_handle = NULL;

    char path [1024];
    int res;

    MonoAssemblyName *assembly_name = mono_assembly_get_name (assembly);
    const char *aname = mono_assembly_name_get_name (assembly_name);

    res = snprintf (path, sizeof (path) - 1, "%s/%s.aotdata", bundle_path, aname);
    if (res <= 0) {
        LOG_ERROR ("Could not format '%s' and '%s' together into \"%%s/%%s.aotdata\" for assembly preloading.\n", bundle_path, aname);
        abort ();
    }

    int fd = open (path, O_RDONLY);
    if (fd < 0) {
        LOG_INFO ("Could not open file '%s' while trying to load aot data.\n", path);
        return NULL;
    }

    void *ptr = mmap (NULL, size, PROT_READ, MAP_FILE | MAP_PRIVATE, fd, 0);
    if (ptr == MAP_FAILED) {
        LOG_INFO ("Could not mmap file '%s'.\n", path);
        close (fd);
        return NULL;
    }

    close (fd);
    *out_handle = ptr;
    return (unsigned char *) ptr;
}

static void
free_aot_data (MonoAssembly *assembly, int size, void *user_data, void *handle)
{
    munmap (handle, size);
}

void
runtime_init_callback ()
{
    const char *assemblies_path = strdup(getenv("%ASSEMBLIES_LOCATION%"));
    if (!assemblies_path) {
        LOG_ERROR ("Could not duplicate value of environment variable %ASSEMBLIES_LOCATION% due to insufficient memory.\n");
        abort ();
    }
    bundle_path = (assemblies_path[0] != '\0') ? assemblies_path : "./";

    initialize_runtimeconfig ();

    initialize_appctx_env_variables ();

    register_aot_modules ();

    mono_set_assemblies_path (bundle_path);

    mono_jit_set_aot_only (true);

    mono_install_assembly_preload_hook (mono_assembly_preload_hook, NULL);

    mono_install_load_aot_data_hook (load_aot_data, free_aot_data, NULL);

    mono_set_signal_chaining (true);

    MonoDomain *domain = mono_jit_init ("mono.self.contained.library");
    if (!domain) {
        LOG_ERROR ("Could not auto initialize runtime.\n");
        abort ();
    }

    load_assemblies_with_exported_symbols ();

    free ((void *)assemblies_path);
}

void __attribute__((constructor))
autoinit ()
{
    mono_set_runtime_init_callback (&runtime_init_callback);
}
