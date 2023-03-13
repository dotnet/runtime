// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <mono/metadata/assembly.h>

#ifdef HOST_ANDROID
#include <android/log.h>

#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "MONO_SELF_CONTAINED_LIBRARY", fmt, ##__VA_ARGS__)
#else
#include <os/log.h>

#define LOG_ERROR(fmt, ...) os_log_error (OS_LOG_DEFAULT, fmt, ##__VA_ARGS__)
#endif

void
mono_assembly_open_from_dir (const char *dir, const char* filename)
{
    size_t str_len = strlen (dir) + strlen (filename) + 2; // +1 "/", +1 null-terminating char
    char *assembly_path = (char*)malloc (str_len);
    if (!assembly_path) {
        LOG_ERROR ("Could not allocate %zu bytes to format '%s' and '%s' together to open assembly to initialize GOT slots.\n", str_len, dir, filename);
        abort ();
    }
    int num_char = snprintf (assembly_path, str_len, "%s/%s", dir, filename);

    MonoAssembly *assembly = mono_assembly_open (assembly_path, NULL);
    if (!assembly) {
        LOG_ERROR ("Could not open assembly '%s'. Unable to properly initialize GOT slots.\n", assembly_path);
        abort ();
    }

    free ((void *)assembly_path);
}

void
load_assemblies_with_exported_symbols (const char *dir)
{
%ASSEMBLIES_LOADER%}