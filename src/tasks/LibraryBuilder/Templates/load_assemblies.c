// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <assert.h>
#include <string.h>

#include <mono/metadata/assembly.h>

MonoAssembly* mono_assembly_open (const char *filename, MonoImageOpenStatus *status);

void
mono_assembly_open_from_dir (const char *dir, const char* filename)
{
    int str_len = strlen (dir) + strlen (filename) + 2;
    const char *assembly_path = (char*)malloc (sizeof (char) * str_len);
    int num_char = snprintf (assembly_path, str_len, "%s/%s", dir, filename);

    assert (num_char > 0 && num_char < str_len);

    mono_assembly_open (assembly_path, NULL);

    free ((void *)assembly_path);
}

void
load_assemblies_with_exported_symbols (const char *dir)
{
%ASSEMBLIES_LOADER%}