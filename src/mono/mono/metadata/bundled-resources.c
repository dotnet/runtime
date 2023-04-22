// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>

#include "bundled-resources-internals.h"
#include "assembly-internals.h"

#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-private-unstable.h>

static GHashTable *bundled_resources = NULL;

//---------------------------------------------------------------------------------------
//
// mono_free_bundled_resources frees all memory allocated for bundled resources.
// It should only be called when the runtime no longer needs access to the data,
// most likely to happen during runtime shutdown.
//

void
mono_free_bundled_resources (void)
{
	g_hash_table_destroy (bundled_resources);
	bundled_resources = NULL;

	mono_hash_contains_bundled_assemblies (FALSE);
	mono_hash_contains_bundled_satellite_assemblies (FALSE);
}

//---------------------------------------------------------------------------------------
//
// mono_add_bundled_resource handles bundling of many types of resources to circumvent
// needing to find or have those resources on disk. The MonoBundledResource struct models
// the union of information carried by all supported types of resources which are
// enumerated in MonoBundledResourceType.
//
// bundled_resources:
// A single hash table will hold all resources being bundled with the understanding
// that all resources being bundled are uniquely named. The bundled resource is tagged
// with the type of resource it represents, and the pointer added to this hash table
// should fully represent a MonoBundled*Resource struct defined in `bundled-resources-internals.h
//
// Arguments:
// ** resources_to_bundle - An array of pointers to `MonoBundledResource`, which details
//     the type of MonoBundled*Resource information follows this pointer in memory.
// len - The number of resources being added to the hash table
//

void
mono_add_bundled_resource (MonoBundledResource **resources_to_bundle, uint32_t len)
{
	if (!bundled_resources)
		bundled_resources = g_hash_table_new (g_str_hash, g_str_equal);

	gboolean assemblyAdded, satelliteAssemblyAdded;

	for (int i = 0; i < len; ++i) {
		MonoBundledResource *resource_to_bundle = (MonoBundledResource *)resources_to_bundle[i];
		switch (resource_to_bundle->type) {
		case MONO_BUNDLED_ASSEMBLY: {
			MonoBundledAssemblyResource *assembly = (MonoBundledAssemblyResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) assembly->assembly.name, assembly);
			assemblyAdded = TRUE;
			break;
		}
		case MONO_BUNDLED_SATELLITE_ASSEMBLY: {
			MonoBundledSatelliteAssemblyResource *satellite_assembly = (MonoBundledSatelliteAssemblyResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) satellite_assembly->satellite_assembly.name, satellite_assembly);
			satelliteAssemblyAdded = TRUE;
			break;
		}
		case MONO_BUNDLED_DATA:
		default: {
			MonoBundledDataResource *data = (MonoBundledDataResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) data->data.name, data);
			break;
		}
		}
	}

	if (assemblyAdded)
		mono_hash_contains_bundled_assemblies (assemblyAdded);

	if (satelliteAssemblyAdded)
		mono_hash_contains_bundled_satellite_assemblies (satelliteAssemblyAdded);
}

//---------------------------------------------------------------------------------------
//
// mono_get_bundled_resource_data retrieves the pointer of the MonoBundledResource associated
// with a key equivalent to the requested resource name. If the requested bundled resource's
// name has been added via mono_add_bundled_resource, a MonoBundled*Resource had been
// preallocated, typically through EmitBundleTask.
//
// Arguments:
//  * name - Unique name of the resource
//
// Returns:
//  MonoBundledResource * - Pointer to the resource in the hashmap with the key `name`
//

MonoBundledResource *
mono_get_bundled_resource_data (const char *name)
{
	return g_hash_table_lookup (bundled_resources, name);
}
