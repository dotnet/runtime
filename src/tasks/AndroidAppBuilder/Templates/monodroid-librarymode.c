// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdbool.h>
#include <errno.h>
#include <string.h>
#include <jni.h>
#include <assert.h>
#include <unistd.h>

static void
strncpy_str (JNIEnv *env, char *buff, jstring str, int nbuff)
{
    jboolean isCopy = 0;
    const char *copy_buff = (*env)->GetStringUTFChars (env, str, &isCopy);
    strncpy (buff, copy_buff, nbuff);
    if (isCopy)
        (*env)->ReleaseStringUTFChars (env, str, copy_buff);
}

void SayHello ();

int invoke_netlibrary_entrypoints (void)
{
    SayHello ();

    return 42;
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
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_cache_dir, jstring j_testresults_dir, jstring j_entryPointLibName, jobjectArray j_args, long current_local_time)
{
    char file_dir[2048];
    char cache_dir[2048];
    char testresults_dir[2048];
    strncpy_str (env, file_dir, j_files_dir, sizeof(file_dir));
    strncpy_str (env, cache_dir, j_cache_dir, sizeof(cache_dir));
    strncpy_str (env, testresults_dir, j_testresults_dir, sizeof(testresults_dir));

    setenv ("HOME", file_dir, true);
    setenv ("DOTNET_LIBRARY_ASSEMBLY_PATH", file_dir, true);
    setenv ("TMPDIR", cache_dir, true);
    setenv ("TEST_RESULTS_DIR", testresults_dir, true);

    return invoke_netlibrary_entrypoints ();
}
