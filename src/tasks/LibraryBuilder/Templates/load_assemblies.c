// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <mono/metadata/assembly.h>

MonoAssembly* mono_assembly_open (const char *filename, MonoImageOpenStatus *status);

void load_assemblies_with_exported_symbols ()
{
%ASSEMBLIES_LOADER%}