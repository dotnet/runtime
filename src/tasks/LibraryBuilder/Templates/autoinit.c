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
        LOG_ERROR ("Out of memory.\n");
        abort ();
    }

    int num_char = snprintf (file_path, str_len, "%s/%s", bundle_path, file_name);
    struct stat buffer;

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

static unsigned char *
load_aot_data (MonoAssembly *assembly, int size, void *user_data, void **out_handle)
{
    *out_handle = NULL;

    MonoAssemblyName *assembly_name = mono_assembly_get_name (assembly);
    const char *aname = mono_assembly_name_get_name (assembly_name);

    size_t str_len = sizeof (char) * (strlen (bundle_path) + strlen (aname) + 10); // +1 "/", +8 ".aotdata", +1 null-terminating char
    char *file_path = (char *)malloc (str_len);
    if (!file_path) {
        LOG_ERROR ("Out of memory.\n");
        abort ();
    }

    int res = snprintf (file_path, str_len, "%s/%s.aotdata", bundle_path, aname);

    int fd = open (file_path, O_RDONLY);
    if (fd < 0) {
        LOG_INFO ("Could not open file '%s' while trying to load aot data.\n", file_path);
        return NULL;
    }

    void *ptr = mmap (NULL, size, PROT_READ, MAP_FILE | MAP_PRIVATE, fd, 0);
    if (ptr == MAP_FAILED) {
        LOG_INFO ("Could not mmap file '%s'.\n", file_path);
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
        LOG_ERROR ("Out of memory.\n");
        abort ();
    }
    bundle_path = (assemblies_path[0] != '\0') ? assemblies_path : "./";

    initialize_runtimeconfig ();

    initialize_appctx_env_variables ();

    register_aot_modules ();

    mono_set_assemblies_path (bundle_path);

    mono_jit_set_aot_only (true);

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
