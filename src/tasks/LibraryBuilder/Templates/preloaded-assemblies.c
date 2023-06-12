// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdlib.h>

#include <mono/metadata/assembly.h>

#include "library-builder.h"

static void
preload_assembly (const char* filename)
{
    MonoAssembly *assembly = mono_assembly_load_with_partial_name (filename, NULL);
    if (!assembly)
        LOG_ERROR ("Could not open assembly '%s'.\n", filename);
}

void
preload_assemblies_with_exported_symbols ()
{
    %ASSEMBLIES_PRELOADER%
}