#include <assert.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/system_properties.h>
#include <unistd.h>

#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>

static char *bundle_path;

void register_aot_modules (void);
char *monoeg_g_getenv (const char *variable);

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
    int str_len = strlen (bundle_path) + strlen (file_name) + 1; // +1 is for the \"/\"
    char *file_path = (char *)malloc (sizeof (char) * (str_len +1)); // +1 is for the terminating null character
    int num_char = snprintf (file_path, (str_len + 1), "%s/%s", bundle_path, file_name);
    struct stat buffer;

    assert (num_char > 0 && num_char == str_len);

    if (stat (file_path, &buffer) == 0) {
        MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
        arg->kind = 0;
        arg->runtimeconfig.name.path = file_path;
        monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, file_path);
    } else {
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
    assert (res > 0);

    struct stat buffer;
    if (stat (path, &buffer) == 0) {
        MonoAssembly *assembly = mono_assembly_open (path, NULL);
        assert (assembly);
        return assembly;
    }
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
    assert (res > 0);

    int fd = open (path, O_RDONLY);
    if (fd < 0) {
        return NULL;
    }

    void *ptr = mmap (NULL, size, PROT_READ, MAP_FILE | MAP_PRIVATE, fd, 0);
    if (ptr == MAP_FAILED) {
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
    initialize_runtimeconfig ();

    initialize_appctx_env_variables ();

    register_aot_modules ();

    mono_set_assemblies_path ((bundle_path && bundle_path[0] != '\0') ? bundle_path : "./");

    mono_jit_set_aot_only (true);

    mono_install_assembly_preload_hook (mono_assembly_preload_hook, NULL);

    mono_install_load_aot_data_hook (load_aot_data, free_aot_data, NULL);

    mono_set_signal_chaining (true);

    mono_jit_init ("mono.self.contained.library"); // Pass in via LibraryBuilder?

%ASSEMBLIES_LOADER%
}

void __attribute__((constructor))
autoinit ()
{
    bundle_path = monoeg_g_getenv("%ASSETS_PATH%");
    mono_set_runtime_init_callback (&runtime_init_callback);
}
