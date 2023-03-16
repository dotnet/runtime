// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdbool.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>

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

    monovm_initialize(2, appctx_keys, appctx_values);
}

static void
runtime_init_callback ()
{
    const char *bundle_path = getenv("%ASSEMBLIES_LOCATION%");
    if (!bundle_path || bundle_path[0] == '\0')
        bundle_path = "./";

    initialize_runtimeconfig (bundle_path);

    initialize_appctx_env_variables (bundle_path);

    register_aot_modules ();

    mono_set_assemblies_path (bundle_path);

    mono_jit_set_aot_only (true);

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
