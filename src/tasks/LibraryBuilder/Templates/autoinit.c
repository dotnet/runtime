#include <stdint.h>
#include <stdio.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

/*
    Note that this will expand into auto-initialization the runtime, but for now
    it makes sure to keep the monovm_initialize and monovm_runtimeconfig_initialize symbols
*/

void keep_init()
{
    const char* appctx_keys[3];
    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_keys[2] = "System.TimeZoneInfo.LocalDateTimeOffset";

	const char* appctx_values[3];
    appctx_values[0] = "RUNTIME_IDENTIFIER";
    appctx_values[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_values[2] = "System.TimeZoneInfo.LocalDateTimeOffset";

    int ret = monovm_initialize (3, appctx_keys, appctx_values);
}

void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
    free (args);
    free (user_data);
}

void keep_runtimecfg()
{
    MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
    arg->kind = 0;
    monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, "file_path");
}
