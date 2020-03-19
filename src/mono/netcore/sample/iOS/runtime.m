#import <Foundation/Foundation.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-logger.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/exception.h>
#include <mono/jit/jit.h>

#import <os/log.h>
#include <sys/stat.h>
#include <sys/mman.h>

static os_log_t stdout_log;

/* These are not in public headers */
typedef unsigned char* (*MonoLoadAotDataFunc) (MonoAssembly *assembly, int size, void *user_data, void **out_handle);
typedef void  (*MonoFreeAotDataFunc) (MonoAssembly *assembly, int size, void *user_data, void *handle);
void mono_install_load_aot_data_hook (MonoLoadAotDataFunc load_func, MonoFreeAotDataFunc free_func, void *user_data);
void mono_trace_init (void);
void mono_gc_init_finalizer_thread (void);

static char *bundle_path;

const char *
get_bundle_path (void)
{
    if (bundle_path)
        return bundle_path;

    NSBundle* main_bundle = [NSBundle mainBundle];
    NSString* path = [main_bundle bundlePath];
    bundle_path = strdup ([path UTF8String]);

    return bundle_path;
}

static unsigned char *
load_aot_data (MonoAssembly *assembly, int size, void *user_data, void **out_handle)
{
    *out_handle = NULL;

    char path [1024];
    int res;

    MonoAssemblyName *assembly_name = mono_assembly_get_name (assembly);
    const char *aname = mono_assembly_name_get_name (assembly_name);
    const char *bundle = get_bundle_path ();

    os_log_info (OS_LOG_DEFAULT, "Looking for aot data for assembly '%s'.", aname);
    res = snprintf (path, sizeof (path) - 1, "%s/%s.aotdata", bundle, aname);
    assert (res > 0);

    int fd = open (path, O_RDONLY);
    if (fd < 0) {
        os_log_info (OS_LOG_DEFAULT, "Could not load the aot data for %s from %s: %s\n", aname, path, strerror (errno));
        return NULL;
    }

    void *ptr = mmap (NULL, size, PROT_READ, MAP_FILE | MAP_PRIVATE, fd, 0);
    if (ptr == MAP_FAILED) {
        os_log_info (OS_LOG_DEFAULT, "Could not map the aot file for %s: %s\n", aname, strerror (errno));
        close (fd);
        return NULL;
    }

    close (fd);
    os_log_info (OS_LOG_DEFAULT, "Loaded aot data for %s.\n", aname);
    *out_handle = ptr;
    return (unsigned char *) ptr;
}

static void
free_aot_data (MonoAssembly *assembly, int size, void *user_data, void *handle)
{
    munmap (handle, size);
}

static MonoAssembly*
load_assembly (const char *name, const char *culture)
{
    const char *bundle = get_bundle_path ();
    char filename [1024];
    char path [1024];
    int res;

    os_log_info (OS_LOG_DEFAULT, "assembly_preload_hook: %{public}s %{public}s %{public}s\n", name, culture, bundle);

    int len = strlen (name);
    int has_extension = len > 3 && name [len - 4] == '.' && (!strcmp ("exe", name + (len - 3)) || !strcmp ("dll", name + (len - 3)));

    // add extensions if required.
    strlcpy (filename, name, sizeof (filename));
    if (!has_extension) {
        strlcat (filename, ".dll", sizeof (filename));
    }

    if (culture && strcmp (culture, ""))
        res = snprintf (path, sizeof (path) - 1, "%s/%s/%s", bundle, culture, filename);
    else
        res = snprintf (path, sizeof (path) - 1, "%s/%s", bundle, filename);
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
assembly_preload_hook (MonoAssemblyName *aname, char **assemblies_path, void* user_data)
{
    const char *name = mono_assembly_name_get_name (aname);
    const char *culture = mono_assembly_name_get_culture (aname);
    return load_assembly (name, culture);
}

char *
strdup_printf (const char *msg, ...)
{
    va_list args;
    char *formatted = NULL;
    va_start (args, msg);
    vasprintf (&formatted, msg, args);
    va_end (args);
    return formatted;
}

static MonoObject *
fetch_exception_property (MonoObject *obj, const char *name, bool is_virtual)
{
    MonoMethod *get = NULL;
    MonoMethod *get_virt = NULL;
    MonoObject *exc = NULL;

    get = mono_class_get_method_from_name (mono_get_exception_class (), name, 0);
    if (get) {
        if (is_virtual) {
            get_virt = mono_object_get_virtual_method (obj, get);
            if (get_virt)
                get = get_virt;
        }

        return (MonoObject *) mono_runtime_invoke (get, obj, NULL, &exc);
    } else {
        printf ("Could not find the property System.Exception.%s", name);
    }

    return NULL;
}

static char *
fetch_exception_property_string (MonoObject *obj, const char *name, bool is_virtual)
{
    MonoString *str = (MonoString *) fetch_exception_property (obj, name, is_virtual);
    return str ? mono_string_to_utf8 (str) : NULL;
}

void
unhandled_exception_handler (MonoObject *exc, void *user_data)
{
    NSMutableString *msg = [[NSMutableString alloc] init];

    MonoClass *type = mono_object_get_class (exc);
    char *type_name = strdup_printf ("%s.%s", mono_class_get_namespace (type), mono_class_get_name (type));
    char *trace = fetch_exception_property_string (exc, "get_StackTrace", true);
    char *message = fetch_exception_property_string (exc, "get_Message", true);

    [msg appendString:@"Unhandled managed exceptions:\n"];
    [msg appendFormat: @"%s (%s)\n%s\n", message, type_name, trace ? trace : ""];

    free (trace);
    free (message);
    free (type_name);

    os_log_info (OS_LOG_DEFAULT, "%@", msg);
    os_log_info (OS_LOG_DEFAULT, "Exit code: %d.", 1);
    exit (1);
}

void
log_callback (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
    os_log_info (OS_LOG_DEFAULT, "(%s %s) %s", log_domain, log_level, message);
    if (fatal) {
        os_log_info (OS_LOG_DEFAULT, "Exit code: %d.", 1);
        exit (1);
    }
}

static void
register_dllmap (void)
{
    mono_dllmap_insert (NULL, "libSystem.Native", NULL, "__Internal", NULL);
    mono_dllmap_insert (NULL, "libSystem.IO.Compression.Native", NULL, "__Internal", NULL);
    mono_dllmap_insert (NULL, "libSystem.Security.Cryptography.Native.Apple", NULL, "__Internal", NULL);
}

void mono_jit_set_aot_mode (MonoAotMode mode);

#if DEVICE
extern void *mono_aot_module_Program_info;
extern void *mono_aot_module_System_Private_CoreLib_info;
extern void *mono_aot_module_System_Runtime_info;
extern void *mono_aot_module_System_Runtime_Extensions_info;
extern void *mono_aot_module_System_Collections_info;
extern void *mono_aot_module_System_Core_info;
extern void *mono_aot_module_System_Threading_info;
extern void *mono_aot_module_System_Threading_Tasks_info;
extern void *mono_aot_module_System_Linq_info;
extern void *mono_aot_module_System_Memory_info;
extern void *mono_aot_module_System_Runtime_InteropServices_info;
extern void *mono_aot_module_System_Text_Encoding_Extensions_info;
extern void *mono_aot_module_Microsoft_Win32_Primitives_info;
extern void *mono_aot_module_System_Console_info;
extern void *mono_aot_module_Program_info;

void mono_ios_register_modules (void)
{
    mono_aot_register_module (mono_aot_module_Program_info);
    mono_aot_register_module (mono_aot_module_System_Private_CoreLib_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_Extensions_info);
    mono_aot_register_module (mono_aot_module_System_Collections_info);
    mono_aot_register_module (mono_aot_module_System_Core_info);
    mono_aot_register_module (mono_aot_module_System_Threading_info);
    mono_aot_register_module (mono_aot_module_System_Threading_Tasks_info);
    mono_aot_register_module (mono_aot_module_System_Linq_info);
    mono_aot_register_module (mono_aot_module_System_Memory_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_InteropServices_info);
    mono_aot_register_module (mono_aot_module_System_Text_Encoding_Extensions_info);
    mono_aot_register_module (mono_aot_module_Microsoft_Win32_Primitives_info);
    mono_aot_register_module (mono_aot_module_System_Console_info);
    mono_aot_register_module (mono_aot_module_Program_info);
}

void mono_ios_setup_execution_mode (void)
{
    mono_jit_set_aot_mode (MONO_AOT_MODE_FULL);
}
#endif

void
mono_ios_runtime_init (void)
{
    // for now, only Invariant Mode is supported (FIXME: integrate ICU)
    setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);

    stdout_log = os_log_create ("net.dot.mono", "stdout");

    bool wait_for_debugger = FALSE;
    char* executable = "Program.dll";

    const char* bundle = get_bundle_path ();
    chdir (bundle);

    register_dllmap ();

#if DEVICE
    // register modules
    mono_ios_register_modules ();
    mono_ios_setup_execution_mode ();
#endif
    
    mono_debug_init (MONO_DEBUG_FORMAT_MONO);
    mono_install_assembly_preload_hook (assembly_preload_hook, NULL);
    mono_install_load_aot_data_hook (load_aot_data, free_aot_data, NULL);
    mono_install_unhandled_exception_hook (unhandled_exception_handler, NULL);
    mono_trace_init ();
    mono_trace_set_log_handler (log_callback, NULL);
    mono_set_signal_chaining (TRUE);
    mono_set_crash_chaining (TRUE);

    if (wait_for_debugger) {
        char* options[] = { "--debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555" };
        mono_jit_parse_options (1, options);
    }
    mono_jit_init_version ("dotnet.ios", "mobile");

#if DEVICE
    // device runtimes are configured to use lazy gc thread creation
    mono_gc_init_finalizer_thread ();
#endif

    MonoAssembly *assembly = load_assembly (executable, NULL);
    assert (assembly);
    os_log_info (OS_LOG_DEFAULT, "Executable: %{public}s", executable);

    int res = mono_jit_exec (mono_domain_get (), assembly, 1, &executable);
    // Print this so apps parsing logs can detect when we exited
    os_log_info (OS_LOG_DEFAULT, "Exit code: %d.", res);
}

