// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_ASSEMBLY_TYPES_H
#define _MONO_ASSEMBLY_TYPES_H

#include <mono/utils/details/mono-error-types.h>
#include <mono/metadata/details/image-types.h>

MONO_BEGIN_DECLS

/* Installs a function which is called each time a new assembly is loaded. */
typedef void  (*MonoAssemblyLoadFunc)         (MonoAssembly *assembly, void* user_data);

/*
 * Installs a new function which is used to search the list of loaded
 * assemblies for a given assembly name.
 */
typedef MonoAssembly *(*MonoAssemblySearchFunc)         (MonoAssemblyName *aname, void* user_data);

/* Installs a function which is called before a new assembly is loaded
 * The hook are invoked from last hooked to first. If any of them returns
 * a non-null value, that will be the value returned in mono_assembly_load */
typedef MonoAssembly * (*MonoAssemblyPreLoadFunc) (MonoAssemblyName *aname,
						   char **assemblies_path,
						   void* user_data);

typedef struct {
	const char *name;
	const unsigned char *data;
	unsigned int size;
} MonoBundledAssembly;



MONO_END_DECLS

#endif /* _MONO_ASSEMBLY_TYPES_H */
