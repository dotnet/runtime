// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdbool.h>
#include <errno.h>
#include <string.h>
#include <jni.h>
#include <assert.h>
#include <unistd.h>

/********* exported symbols *********/

void
Java_net_dot_MonoRunner_setEnv (JNIEnv* env, jobject thiz, jstring j_key, jstring j_value);

int
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_entryPointLibName, long current_local_time);

int
Java_net_dot_MonoRunner_execEntryPoint (JNIEnv* env, jobject thiz, jstring j_entryPointLibName, jobjectArray j_args);

void
Java_net_dot_MonoRunner_freeNativeResources (JNIEnv* env, jobject thiz);

/********* imported symbols *********/
void SayHello ();

/********* implementation *********/

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
invoke_netlibrary_entrypoints (void)
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
Java_net_dot_MonoRunner_initRuntime (JNIEnv* env, jobject thiz, jstring j_files_dir, jstring j_entryPointLibName, long current_local_time)
{
    char file_dir[2048];
    strncpy_str (env, file_dir, j_files_dir, sizeof(file_dir));

    setenv ("DOTNET_LIBRARY_ASSEMBLY_PATH", file_dir, true);

    //setenv ("MONO_LOG_LEVEL", "debug", true);
    //setenv ("MONO_LOG_MASK", "all", true);
    return 0;
}

int
Java_net_dot_MonoRunner_execEntryPoint (JNIEnv* env, jobject thiz, jstring j_entryPointLibName, jobjectArray j_args)
{
    return invoke_netlibrary_entrypoints ();
}

void
Java_net_dot_MonoRunner_freeNativeResources (JNIEnv* env, jobject thiz)
{
    // nothing to do
}
