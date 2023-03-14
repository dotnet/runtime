// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdlib.h>

#include <mono/metadata/assembly.h>

#ifdef HOST_ANDROID
#include <android/log.h>

#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "MONO_SELF_CONTAINED_LIBRARY", fmt, ##__VA_ARGS__)
#else
#include <os/log.h>

#define LOG_ERROR(fmt, ...) os_log_error (OS_LOG_DEFAULT, fmt, ##__VA_ARGS__)
#endif

void
mono_assembly_load_with_partial_name_check (const char* filename)
{
    MonoAssembly *assembly = mono_assembly_load_with_partial_name (filename, NULL);
    if (!assembly) {
        LOG_ERROR ("Could not open assembly '%s'. Unable to properly initialize GOT slots.\n", filename);
        abort ();
    }
}

void
load_assemblies_with_exported_symbols ()
{
%ASSEMBLIES_LOADER%}