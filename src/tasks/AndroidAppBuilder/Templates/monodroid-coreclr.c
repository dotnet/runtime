// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <sys/stat.h>
#include <stdlib.h>
#include <stdio.h>
#include <fcntl.h>
#include <errno.h>
#include <string.h>
#include <jni.h>
#include <android/log.h>
#include <sys/system_properties.h>
#include <sys/mman.h>
#include <assert.h>
#include <unistd.h>
#include <coreclrhost.h>
#include <dirent.h>

#include <corehost/host_runtime_contract.h>

/********* exported symbols *********/

/* JNI exports */

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value);

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_entryPointLibName, long current_local_time);

int
Java_net_dot_MonoRunner_execEntryPoint (JNIEnv* env, jobject thiz, jstring j_entryPointLibName, jobjectArray j_args);

void
Java_net_dot_MonoRunner_freeNativeResources (JNIEnv* env, jobject thiz);

/********* implementation *********/

static char* g_bundle_path = NULL;
static const char* g_executable_path = NULL;
static unsigned int g_coreclr_domainId = 0;
static void* g_coreclr_handle = NULL;

#define MAX_MAPPED_COUNT 256 // Arbitrarily 'large enough' number
static void* g_mapped_files[MAX_MAPPED_COUNT];
static size_t g_mapped_file_sizes[MAX_MAPPED_COUNT];
static unsigned int g_mapped_files_count = 0;

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

#define RUNTIMECONFIG_BIN_FILE "runtimeconfig.bin"

static void
strncpy_str (JNIEnv *env, char *buff, jstring str, int nbuff)
{
    jboolean isCopy = 0;
    const char *copy_buff = (*env)->GetStringUTFChars (env, str, &isCopy);
    strncpy (buff, copy_buff, nbuff);
    buff[nbuff - 1] = '\0'; // ensure '\0' terminated
    if (isCopy)
        (*env)->ReleaseStringUTFChars (env, str, copy_buff);
}

static int
bundle_executable_path (const char* executable, const char* bundle_path, const char** executable_path)
{
    size_t executable_path_len = strlen(bundle_path) + strlen(executable) + 1; // +1 for '/'
    char* temp_path = (char*)malloc(sizeof(char) * (executable_path_len + 1)); // +1 for '\0'
    if (temp_path == NULL)
    {
        return -1;
    }

    size_t res = snprintf(temp_path, (executable_path_len + 1), "%s/%s", bundle_path, executable);
    if (res < 0 || res != executable_path_len)
    {
        return -1;
    }
    *executable_path = temp_path;
    return executable_path_len;
}

static bool
external_assembly_probe(const char* name, void** data, int64_t* size)
{
    if (g_mapped_files_count >= MAX_MAPPED_COUNT)
    {
        LOG_ERROR("Too many mapped files, cannot map %s", name);
        return false;
    }

    // Get just the file name
    const char* pos = strrchr(name, '/');
    if (pos != NULL)
        name = pos + 1;

    // Look in the bundle path where the files were extracted
    char full_path[1024];
    size_t path_len = strlen(g_bundle_path) + strlen(name) + 1; // +1 for '/'
    size_t res = snprintf(full_path, path_len + 1, "%s/%s", g_bundle_path, name);
    if (res < 0 || res != path_len)
        return false;

    int fd = open(full_path, O_RDONLY);
    if (fd == -1)
        return false;

    struct stat buf;
    if (fstat(fd, &buf) == -1)
    {
        close(fd);
        return false;
    }

    int64_t size_local = buf.st_size;
    void* mapped = mmap(NULL, size_local, PROT_READ, MAP_PRIVATE, fd, 0);
    if (mapped == MAP_FAILED)
    {
        close(fd);
        return false;
    }

    LOG_INFO("Mapped %s -> %s", name, full_path);
    g_mapped_files[g_mapped_files_count] = mapped;
    g_mapped_file_sizes[g_mapped_files_count] = size_local;
    g_mapped_files_count++;
    close(fd);
    *data = mapped;
    *size = size_local;
    return true;
}

static void
free_resources ()
{
    if (g_bundle_path)
    {
        free (g_bundle_path);
        g_bundle_path = NULL;
    }
    if (g_executable_path)
    {
        free (g_executable_path);
        g_executable_path = NULL;
    }
    if (g_coreclr_handle)
    {
        // Clean up some coreclr resources. This doesn't make coreclr unloadable.
        coreclr_shutdown (g_coreclr_handle, g_coreclr_domainId);
        g_coreclr_handle = NULL;
    }
    for (int i = 0; i < g_mapped_files_count; ++i)
    {
        munmap (g_mapped_files[i], g_mapped_file_sizes[i]);
    }
}

static int
mono_droid_execute_assembly (const char* executable_path, void* coreclr_handle, unsigned int coreclr_domainId, int managed_argc, const char** managed_argv)
{
    unsigned int rv;
    LOG_INFO ("Calling coreclr_execute_assembly");
    coreclr_execute_assembly (coreclr_handle, coreclr_domainId, managed_argc, managed_argv, executable_path, &rv);
    LOG_INFO ("Exit code: %u.", rv);
    return rv;
}

#define PROPERTY_COUNT 3

static int
mono_droid_runtime_init (const char* executable)
{
    LOG_INFO ("mono_droid_runtime_init (CoreCLR) called with executable: %s", executable);

    // build using DiagnosticPorts property in AndroidAppBuilder
    // or set DOTNET_DiagnosticPorts env via adb, xharness when undefined.
    // NOTE, using DOTNET_DiagnosticPorts requires app build using AndroidAppBuilder and RuntimeComponents to include 'diagnostics_tracing' component
#ifdef DIAGNOSTIC_PORTS
    setenv ("DOTNET_DiagnosticPorts", DIAGNOSTIC_PORTS, true);
#endif

    if (bundle_executable_path(executable, g_bundle_path, &g_executable_path) < 0)
    {
        LOG_ERROR("Failed to resolve full path for: %s", executable);
        return -1;
    }

    chdir (g_bundle_path);

    struct host_runtime_contract host_contract = {
        sizeof(struct host_runtime_contract),
        NULL,    // context
        NULL,    // get_runtime_property
        NULL,    // bundle_proble
        NULL,    // pinvoke_override
        &external_assembly_probe };

    const char* appctx_keys[PROPERTY_COUNT];
    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_keys[2] = "HOST_RUNTIME_CONTRACT";

    const char* appctx_values[PROPERTY_COUNT];
    appctx_values[0] = ANDROID_RUNTIME_IDENTIFIER;
    appctx_values[1] = g_bundle_path;

    char contract_str[19]; // 0x + 16 hex digits + '\0'
    snprintf(contract_str, 19, "0x%zx", (size_t)(&host_contract));
    appctx_values[2] = contract_str;

    LOG_INFO ("Calling coreclr_initialize");
    int rv = coreclr_initialize (
		g_executable_path,
		executable,
		PROPERTY_COUNT,
		appctx_keys,
		appctx_values,
		&g_coreclr_handle,
		&g_coreclr_domainId
		);
    LOG_INFO ("coreclr_initialize returned 0x%x", rv);
    return rv;
}

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value)
{
    LOG_INFO ("Java_net_dot_MonoRunner_setEnv:");
    assert (g_coreclr_handle == NULL); // setenv should be only called before the runtime is initialized

    const char *key = (*env)->GetStringUTFChars(env, j_key, 0);
    const char *val = (*env)->GetStringUTFChars(env, j_value, 0);

    LOG_INFO ("Setting env: %s=%s", key, val);
    setenv (key, val, true);
    (*env)->ReleaseStringUTFChars(env, j_key, key);
    (*env)->ReleaseStringUTFChars(env, j_value, val);
}

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_entryPointLibName, long current_local_time)
{
    LOG_INFO ("Java_net_dot_MonoRunner_initRuntime (CoreCLR):");
    char file_dir[2048];
    char entryPointLibName[2048];
    strncpy_str (env, file_dir, j_files_dir, sizeof(file_dir));
    strncpy_str (env, entryPointLibName, j_entryPointLibName, sizeof(entryPointLibName));

    size_t file_dir_len = strlen(file_dir);
    char* bundle_path_tmp = (char*)malloc(sizeof(char) * (file_dir_len + 1)); // +1 for '\0'
    if (bundle_path_tmp == NULL)
    {
        LOG_ERROR("Failed to allocate memory for bundle_path");
        return -1;
    }
    strncpy(bundle_path_tmp, file_dir, file_dir_len + 1);
    g_bundle_path = bundle_path_tmp;

    return mono_droid_runtime_init (entryPointLibName);
}

int
Java_net_dot_MonoRunner_execEntryPoint (JNIEnv* env, jobject thiz, jstring j_entryPointLibName, jobjectArray j_args)
{
    LOG_INFO("Java_net_dot_MonoRunner_execEntryPoint (CoreCLR):");

    if ((g_bundle_path == NULL) || (g_executable_path == NULL))
    {
        LOG_ERROR("Bundle path or executable path not set");
        return -1;
    }

    if ((g_coreclr_handle == NULL) || (g_coreclr_domainId == 0))
    {
        LOG_ERROR("CoreCLR not initialized");
        return -1;
    }

    int args_len = (*env)->GetArrayLength (env, j_args);
    int managed_argc = args_len + 1;
    const char** managed_argv = (const char**)malloc (managed_argc * sizeof(char*));
    if (managed_argv == NULL)
    {
        LOG_ERROR("Failed to allocate memory for managed_argv");
        return -1;
    }

    managed_argv[0] = g_bundle_path;
    for (int i = 0; i < args_len; ++i)
    {
        jstring j_arg = (*env)->GetObjectArrayElement(env, j_args, i);
        managed_argv[i + 1] = (char*)((*env)->GetStringUTFChars(env, j_arg, NULL));
    }

    int rv = mono_droid_execute_assembly (g_executable_path, g_coreclr_handle, g_coreclr_domainId, managed_argc, managed_argv);

    for (int i = 0; i < args_len; ++i)
    {
        jstring j_arg = (*env)->GetObjectArrayElement(env, j_args, i);
        (*env)->ReleaseStringUTFChars(env, j_arg, managed_argv[i + 1]);
    }

    free(managed_argv);
    return rv;
}

void
Java_net_dot_MonoRunner_freeNativeResources (JNIEnv* env, jobject thiz)
{
    LOG_INFO ("Java_net_dot_MonoRunner_freeNativeResources (CoreCLR):");
    free_resources ();
}
