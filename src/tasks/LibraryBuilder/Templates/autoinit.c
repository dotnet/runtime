// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdbool.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#if defined(USES_AOT_DATA)
#include <fcntl.h>
#include <sys/mman.h>
#include <unistd.h>
#endif

#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>
#include <mono/metadata/assembly.h>

#include "library-builder.h"

static void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
    free ((void *)args->runtimeconfig.name.path);
    free (args);
    free (user_data);
}

static void
initialize_runtimeconfig (const char *bundle_path)
{
    char *file_name = "runtimeconfig.bin";
    size_t str_len = sizeof (char) * (strlen (bundle_path) + strlen (file_name) + 2); // +1 "/", +1 null-terminating char
    char *file_path = (char *)malloc (str_len);
    if (!file_path)
        LOG_ERROR ("Out of memory.\n");

    int num_char = snprintf (file_path, str_len, "%s/%s", bundle_path, file_name);
    if (num_char <= 0 || num_char >= str_len)
        LOG_ERROR ("Encoding error while formatting '%s' and '%s' into \"%%s/%%s\".\n", bundle_path, file_name);

    struct stat buffer;

    if (stat (file_path, &buffer) == 0) {
        MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
        if (!arg)
            LOG_ERROR ("Out of memory.\n");

        arg->kind = 0;
        arg->runtimeconfig.name.path = file_path;
        monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, NULL);
    } else {
        free (file_path);
    }
}

static void
initialize_appctx_env_variables (const char *bundle_path)
{
    const char *appctx_keys[2], *appctx_values[2];

    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_values[0] = "%RUNTIME_IDENTIFIER%";

    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_values[1] = bundle_path;

    monovm_initialize (2, appctx_keys, appctx_values);
}

#if defined(USES_AOT_DATA)
static unsigned char *
load_aot_data (MonoAssembly *assembly, int size, void *user_data, void **out_handle)
{
    *out_handle = NULL;
    const char *bundle_path = (const char*)user_data;

    MonoAssemblyName *assembly_name = mono_assembly_get_name (assembly);
    const char *aname = mono_assembly_name_get_name (assembly_name);

    size_t str_len = sizeof (char) * (strlen (bundle_path) + strlen (aname) + 10); // +1 "/", +8 ".aotdata", +1 null-terminating char
    char *file_path = (char *)malloc (str_len);
    if (!file_path)
        LOG_ERROR ("Out of memory.\n");

    int res = snprintf (file_path, str_len, "%s/%s.aotdata", bundle_path, aname);
    if (res <= 0 || res >= str_len)
        LOG_ERROR ("Encoding error while formatting '%s' and '%s' into \"%%s/%%s\".\n", bundle_path, aname);

    int fd = open (file_path, O_RDONLY);
    if (fd < 0)
        LOG_ERROR ("Could not open file '%s'.\n", file_path);

    void *ptr = mmap (NULL, size, PROT_READ, MAP_FILE | MAP_PRIVATE, fd, 0);
    close (fd);
    if (ptr == MAP_FAILED)
        LOG_ERROR ("Could not map file '%s' to memory.\n", file_path);

    *out_handle = ptr;
    return (unsigned char *) ptr;
}

static void
free_aot_data (MonoAssembly *assembly, int size, void *user_data, void *handle)
{
    munmap (handle, size);
}
#endif

static void
runtime_init_callback ()
{
    const char *assemblies_location = getenv ("%ASSEMBLIES_LOCATION%");
    if (!assemblies_location || assemblies_location[0] == '\0')
        assemblies_location = "./";

    // Don't free as load_aot_data may be called later on, if used.
    const char *bundle_path = strdup (assemblies_location);
    if (!bundle_path)
        LOG_ERROR ("Out of memory.\n");

    initialize_runtimeconfig (bundle_path);

    initialize_appctx_env_variables (bundle_path);

    register_aot_modules ();

    mono_set_assemblies_path (bundle_path);

    mono_jit_set_aot_only (true);

#if defined(USES_AOT_DATA)
    mono_install_load_aot_data_hook (load_aot_data, free_aot_data, bundle_path);
#endif

    mono_set_signal_chaining (true);

    MonoDomain *domain = mono_jit_init ("mono.self.contained.library");
    if (!domain)
        LOG_ERROR ("Could not auto initialize runtime.\n");

    preload_assemblies_with_exported_symbols ();
}

void __attribute__((constructor))
autoinit ()
{
    mono_set_runtime_init_callback (&runtime_init_callback);
}
