// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-logger.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/exception.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

#include <sys/stat.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <jni.h>
#include <android/log.h>
#include <sys/system_properties.h>
#include <assert.h>
#include <unistd.h>

static char *bundle_path;
static char *executable;
static bool force_interpreter;

#define LOG_INFO(fmt, ...) __android_log_print(ANDROID_LOG_DEBUG, "DOTNET", fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "DOTNET", fmt, ##__VA_ARGS__)

#if defined(__arm__)
#define ANDROID_RUNTIME_IDENTIFIER "android-arm"
#elif defined(__aarch64__)
#define ANDROID_RUNTIME_IDENTIFIER "android-arm64"
#elif defined(__i386__)
#define ANDROID_RUNTIME_IDENTIFIER "android-x86"
#elif defined(__x86_64__)
#define ANDROID_RUNTIME_IDENTIFIER "android-x64"
#else
#error Unknown architecture
#endif

static MonoAssembly*
mono_droid_load_assembly (const char *name, const char *culture)
{
    char filename [1024];
    char path [1024];
    int res;

    LOG_INFO ("assembly_preload_hook: %s %s %s\n", name, culture, bundle_path);

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
mono_droid_assembly_preload_hook (MonoAssemblyName *aname, char **assemblies_path, void* user_data)
{
    const char *name = mono_assembly_name_get_name (aname);
    const char *culture = mono_assembly_name_get_culture (aname);
    return mono_droid_load_assembly (name, culture);
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
mono_droid_fetch_exception_property (MonoObject *obj, const char *name, bool is_virtual)
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
mono_droid_fetch_exception_property_string (MonoObject *obj, const char *name, bool is_virtual)
{
    MonoString *str = (MonoString *) mono_droid_fetch_exception_property (obj, name, is_virtual);
    return str ? mono_string_to_utf8 (str) : NULL;
}

void
unhandled_exception_handler (MonoObject *exc, void *user_data)
{
    MonoClass *type = mono_object_get_class (exc);
    char *type_name = strdup_printf ("%s.%s", mono_class_get_namespace (type), mono_class_get_name (type));
    char *trace = mono_droid_fetch_exception_property_string (exc, "get_StackTrace", true);
    char *message = mono_droid_fetch_exception_property_string (exc, "get_Message", true);
    
    LOG_ERROR("UnhandledException: %s %s %s", type_name, message, trace);

    free (trace);
    free (message);
    free (type_name);
    exit (1);
}

void
log_callback (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
    LOG_INFO ("(%s %s) %s", log_domain, log_level, message);
    if (fatal) {
        LOG_ERROR ("Exit code: %d.", 1);
        exit (1);
    }
}

int
mono_droid_runtime_init (void)
{
    // uncomment for debug output:
    //
    //setenv ("XUNIT_VERBOSE", "true", true);
    //setenv ("MONO_LOG_LEVEL", "debug", true);
    //setenv ("MONO_LOG_MASK", "all", true);
    // NOTE: these options can be set via command line args for adb or xharness, see AndroidSampleApp.csproj

    bool wait_for_debugger = false;
    chdir (bundle_path);

    // TODO: set TRUSTED_PLATFORM_ASSEMBLIES, APP_PATHS and NATIVE_DLL_SEARCH_DIRECTORIES

    const char* appctx_keys[2];
    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";

    const char* appctx_values[2];
    appctx_values[0] = ANDROID_RUNTIME_IDENTIFIER;
    appctx_values[1] = bundle_path;
    
    monovm_initialize(2, appctx_keys, appctx_values);

    mono_debug_init (MONO_DEBUG_FORMAT_MONO);
    mono_install_assembly_preload_hook (mono_droid_assembly_preload_hook, NULL);
    mono_install_unhandled_exception_hook (unhandled_exception_handler, NULL);
    mono_trace_set_log_handler (log_callback, NULL);
    mono_set_signal_chaining (true);
    mono_set_crash_chaining (true);

    if (wait_for_debugger) {
        char* options[] = { "--debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555" };
        mono_jit_parse_options (1, options);
    }

    if (force_interpreter) {
        LOG_INFO("Interp Enabled");
        mono_jit_set_aot_mode(MONO_AOT_MODE_INTERP_ONLY);
    }

    mono_jit_init_version ("dotnet.android", "mobile");

    MonoAssembly *assembly = mono_droid_load_assembly (executable, NULL);
    assert (assembly);
    LOG_INFO ("Executable: %s", executable);

    char *managed_argv [1];
    managed_argv[0] = bundle_path;

    int res = mono_jit_exec (mono_domain_get (), assembly, 1, managed_argv);
    LOG_INFO ("Exit code: %d.", res);
    return res;
}

static void
strncpy_str (JNIEnv *env, char *buff, jstring str, int nbuff)
{
    jboolean isCopy = 0;
    const char *copy_buff = (*env)->GetStringUTFChars (env, str, &isCopy);
    strncpy (buff, copy_buff, nbuff);
    if (isCopy)
        (*env)->ReleaseStringUTFChars (env, str, copy_buff);
}

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value)
{
    const char *key = (*env)->GetStringUTFChars(env, j_key, 0);
    const char *val = (*env)->GetStringUTFChars(env, j_value, 0);
    setenv (key, val, true);
    (*env)->ReleaseStringUTFChars(env, j_key, key);
    (*env)->ReleaseStringUTFChars(env, j_value, val);
}

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_cache_dir, jstring j_docs_dir, jstring j_entryPointLibName, jboolean j_forceInterpreter)
{
    char file_dir[2048];
    char cache_dir[2048];
    char docs_dir[2048];
    char entryPointLibName[2048];
    strncpy_str (env, file_dir, j_files_dir, sizeof(file_dir));
    strncpy_str (env, cache_dir, j_cache_dir, sizeof(cache_dir));
    strncpy_str (env, docs_dir, j_docs_dir, sizeof(docs_dir));
    strncpy_str (env, entryPointLibName, j_entryPointLibName, sizeof(entryPointLibName));

    bundle_path = file_dir;
    executable = entryPointLibName;
    force_interpreter = (bool)j_forceInterpreter;

    setenv ("HOME", bundle_path, true);
    setenv ("TMPDIR", cache_dir, true);
    setenv ("DOCSDIR", docs_dir, true);

    return mono_droid_runtime_init ();
}
