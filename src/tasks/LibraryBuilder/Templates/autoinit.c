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
bool monoeg_g_module_address (void *addr, char *file_name, size_t file_name_len,
                           void **file_base, char *sym_name,
                           size_t sym_name_len, void **sym_addr);
char *mono_path_resolve_symlinks(const char *path);
char *monoeg_g_path_get_dirname (const char *filename);
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
%RUNTIME_CONFIG%
}

static void
initialize_appctx_env_variables ()
{
%APPCTX_ENV_VARIABLES%
}

static MonoAssembly*
mono_droid_load_assembly (const char *name, const char *culture)
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

// Assumes that the dl containing this function is in the same directory as the assemblies to load.
static char *
assemblies_dir (void)
{
    static char *dl_dir_name = NULL;
    char dl_filename[4096];

    if (monoeg_g_module_address ((void *)assemblies_dir, dl_filename, sizeof (dl_filename), NULL, NULL, 0, NULL)) {
        char *resolved_dl_filename = mono_path_resolve_symlinks (dl_filename);
        dl_dir_name = monoeg_g_path_get_dirname (resolved_dl_filename);
        free (resolved_dl_filename);
    }

    return dl_dir_name;
}

static MonoAssembly*
mono_droid_assembly_preload_hook (MonoAssemblyName *aname, char **assemblies_path, void* user_data)
{
    const char *name = mono_assembly_name_get_name (aname);
    const char *culture = mono_assembly_name_get_culture (aname);
    return mono_droid_load_assembly (name, culture);
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

    // register all bundled modules

    mono_set_assemblies_path ((bundle_path && bundle_path[0] != '\0') ? bundle_path : "./");

    mono_jit_set_aot_only (true);

    mono_install_assembly_preload_hook (mono_droid_assembly_preload_hook, NULL);

    // TODO test debug scenario
#if DEBUG_ENABLED
    bool wait_for_debugger = false;
    mono_debug_init (MONO_DEBUG_FORMAT_MONO);
    if (wait_for_debugger) {
        char* options[] = { "--debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555" };
        mono_jit_parse_options (1, options);
    }
#endif

    mono_install_load_aot_data_hook (load_aot_data, free_aot_data, NULL);

    mono_set_signal_chaining (true);

    mono_jit_init ("dotnet.android"); // Pass in via LibraryBuilder?

%ASSEMBLIES_LOADER%
}

void
init_mono_runtime ()
{
    bundle_path = monoeg_g_getenv("%ASSETS_PATH%");
    mono_set_runtime_init_callback (&runtime_init_callback);
}

void __attribute__((constructor))
autoinit ()
{
    init_mono_runtime ();
}
