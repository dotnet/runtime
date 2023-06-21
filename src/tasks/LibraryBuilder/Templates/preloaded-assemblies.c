// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdlib.h>

#include <mono/metadata/assembly.h>

#include "library-builder.h"

static void
preload_assembly (const char* filename)
{
    MonoAssembly *assembly = mono_assembly_open (filename, NULL);
    if (assembly)
        return;

    int len = strlen (filename);
    char *filename_without_extension = strdup (filename);
    if (!filename_without_extension)
        LOG_ERROR ("Out of memory.\n");

    if (len >= 4 && !STR_CASE_CMP (".dll", &filename [len - 4]))
        *(filename_without_extension + len - 4) = '\0';

    assembly = mono_assembly_load_with_partial_name (filename_without_extension, NULL);

    free (filename_without_extension);
    if (!assembly)
        LOG_ERROR ("Could not open assembly '%s'.\n", filename);
}

void
preload_assemblies_with_exported_symbols ()
{
    %ASSEMBLIES_PRELOADER%
}