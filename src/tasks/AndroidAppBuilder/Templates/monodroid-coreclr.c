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

/********* exported symbols *********/

/* JNI exports */

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value);

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_cache_dir, jstring j_testresults_dir, jstring j_entryPointLibName, jobjectArray j_args, long current_local_time);

/********* implementation *********/

static char *bundle_path;
static char *executable;

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
    if (isCopy)
        (*env)->ReleaseStringUTFChars (env, str, copy_buff);
}

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value)
{
    LOG_INFO ("Java_net_dot_MonoRunner_setEnv:");
    const char *key = (*env)->GetStringUTFChars(env, j_key, 0);
    const char *val = (*env)->GetStringUTFChars(env, j_value, 0);
    setenv (key, val, true);
    (*env)->ReleaseStringUTFChars(env, j_key, key);
    (*env)->ReleaseStringUTFChars(env, j_value, val);
}

/*
* Get the list of trusted assemblies from a specified @dir_path.
* The path is searched for .dll files which when found are concatenated 
* to the output string @tpas separated by ':'.
* The output string should be freed by the caller.
* The return value is the length of the output string.
*/
static size_t
get_tpas_from_path(const char* dir_path, const char** tpas)
{
    DIR *dir = opendir(dir_path);
    if (dir == NULL)
    {
        LOG_ERROR("Failed to open directory at: %s", dir_path);
        return -1;
    }

    struct dirent *dir_entry;
    size_t dir_path_len = strlen(dir_path);
    char *concat_dll_paths = NULL;
    size_t concat_dll_paths_len = 0;

    while ((dir_entry = readdir(dir)))
    {
        if (dir_entry->d_type == DT_REG)
        {
            size_t file_name_len = strlen(dir_entry->d_name);
            // filter out .dll files
            if (file_name_len > 4 && strcmp(dir_entry->d_name + file_name_len - 4, ".dll") == 0)
            {
                size_t curr_dll_path = dir_path_len + file_name_len + 2; // +2 for '/' and ':'
                concat_dll_paths_len += curr_dll_path;
                concat_dll_paths = realloc(concat_dll_paths, concat_dll_paths_len);
                if (concat_dll_paths == NULL)
                {
                    LOG_ERROR("realloc failed while resolving: %s", dir_entry->d_name);
                    closedir(dir);
                    return -1;
                }
                concat_dll_paths[concat_dll_paths_len-curr_dll_path] = '\0'; // adjust previous string end
                size_t ret = sprintf(concat_dll_paths, "%s%s/%s:", concat_dll_paths, dir_path, dir_entry->d_name); // concat the current dll path
                if (ret != concat_dll_paths_len)
                {
                    LOG_ERROR("sprintf failed while resolving: %s", dir_entry->d_name);
                    closedir(dir);
                    return -1;
                }
            }
        }
    }
    closedir(dir);

    if (concat_dll_paths != NULL && concat_dll_paths_len > 0) {
        concat_dll_paths[concat_dll_paths_len - 1] = '\0'; // remove the trailing ':'
    }

    *tpas = concat_dll_paths;
    return concat_dll_paths_len;
}

static int
mono_droid_runtime_init (const char* executable, int managed_argc, char* managed_argv[], int local_date_time_offset)
{
    LOG_INFO ("mono_droid_runtime_init called with executable: %s", executable);

    chdir (bundle_path);

    // TODO: set TRUSTED_PLATFORM_ASSEMBLIES, APP_PATHS and NATIVE_DLL_SEARCH_DIRECTORIES

    const char* appctx_keys[3];
    appctx_keys[0] = "RUNTIME_IDENTIFIER";
    appctx_keys[1] = "APP_CONTEXT_BASE_DIRECTORY";
    appctx_keys[2] = "TRUSTED_PLATFORM_ASSEMBLIES";

    const char* appctx_values[3];
    appctx_values[0] = ANDROID_RUNTIME_IDENTIFIER;
    appctx_values[1] = bundle_path;
    size_t tpas_len = get_tpas_from_path(bundle_path, &appctx_values[2]);
    if (tpas_len < 1)
    {
        LOG_ERROR("Failed to get trusted assemblies from path: %s", bundle_path);
        return -1;
    }

    size_t executable_path_len = strlen(bundle_path) + strlen(executable) + 2; // +2 for '/' and '\0'
    char* executable_path = (char*)malloc(executable_path_len);
    size_t res = sprintf (executable_path, "%s/%s", bundle_path, executable);
    if (res != executable_path_len - 1)
    {
        LOG_ERROR("Failed to resolve full path for: %s", executable);
        return -1;
    }
    executable_path[res] = '\0';

    unsigned int coreclr_domainId = 0;
    void *coreclr_handle = NULL;

    LOG_INFO ("Calling coreclr_initialize");
    int rv = coreclr_initialize (
		executable_path,
		executable,
		3,
		appctx_keys,
		appctx_values,
		&coreclr_handle,
		&coreclr_domainId
		);
    LOG_INFO ("coreclr_initialize returned %d", rv);

    LOG_INFO ("Calling coreclr_execute_assembly");
    coreclr_execute_assembly (coreclr_handle, coreclr_domainId, managed_argc, managed_argv, executable_path, &rv);

    LOG_INFO ("Exit code: %d.", rv);
    return rv;
}

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_cache_dir, jstring j_testresults_dir, jstring j_entryPointLibName, jobjectArray j_args, long current_local_time)
{
    LOG_INFO ("Java_net_dot_MonoRunner_initRuntime:");
    char file_dir[2048];
    char cache_dir[2048];
    char testresults_dir[2048];
    char entryPointLibName[2048];
    strncpy_str (env, file_dir, j_files_dir, sizeof(file_dir));
    strncpy_str (env, cache_dir, j_cache_dir, sizeof(cache_dir));
    strncpy_str (env, testresults_dir, j_testresults_dir, sizeof(testresults_dir));
    strncpy_str (env, entryPointLibName, j_entryPointLibName, sizeof(entryPointLibName));

    bundle_path = file_dir;
    executable = entryPointLibName;

    setenv ("HOME", bundle_path, true);
    setenv ("TMPDIR", cache_dir, true);
    setenv ("TEST_RESULTS_DIR", testresults_dir, true);

    int args_len = (*env)->GetArrayLength(env, j_args);
    int managed_argc = args_len + 1;
    char** managed_argv = (char**)malloc(managed_argc * sizeof(char*));

    managed_argv[0] = bundle_path;
    for (int i = 0; i < args_len; ++i)
    {
        jstring j_arg = (*env)->GetObjectArrayElement(env, j_args, i);
        managed_argv[i + 1] = (char*)((*env)->GetStringUTFChars(env, j_arg, NULL));
    }

    int res = mono_droid_runtime_init (executable, managed_argc, managed_argv, current_local_time);

    for (int i = 0; i < args_len; ++i)
    {
        jstring j_arg = (*env)->GetObjectArrayElement(env, j_args, i);
        (*env)->ReleaseStringUTFChars(env, j_arg, managed_argv[i + 1]);
    }

    free(managed_argv);
    return res;
}

