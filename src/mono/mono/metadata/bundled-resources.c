// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>
#include <stdbool.h>

#include "bundled-resources-internals.h"
#include "assembly-internals.h"

#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-private-unstable.h>

static GHashTable *bundled_resources = NULL;
static bool bundle_contains_assemblies, bundle_contains_satellite_assemblies = false;

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_free frees all memory allocated for bundled resources.
// It should only be called when the runtime no longer needs access to the data,
// most likely to happen during runtime shutdown.
//

void
mono_bundled_resources_free (void)
{
	g_hash_table_destroy (bundled_resources);
	bundled_resources = NULL;

	bundle_contains_assemblies = false;
	bundle_contains_satellite_assemblies = false;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_add handles bundling of many types of resources to circumvent
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
mono_bundled_resources_add (MonoBundledResource **resources_to_bundle, uint32_t len)
{
	if (!bundled_resources)
		bundled_resources = g_hash_table_new (g_str_hash, g_str_equal);

	bool assemblyAdded, satelliteAssemblyAdded;

	for (uint32_t i = 0; i < len; ++i) {
		MonoBundledResource *resource_to_bundle = (MonoBundledResource *)resources_to_bundle[i];
		switch (resource_to_bundle->type) {
		case MONO_BUNDLED_ASSEMBLY: {
			MonoBundledAssemblyResource *assembly = (MonoBundledAssemblyResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) assembly->resource.id, assembly);
			assemblyAdded = true;
			break;
		}
		case MONO_BUNDLED_SATELLITE_ASSEMBLY: {
			MonoBundledSatelliteAssemblyResource *satellite_assembly = (MonoBundledSatelliteAssemblyResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) satellite_assembly->resource.id, satellite_assembly);
			satelliteAssemblyAdded = true;
			break;
		}
		case MONO_BUNDLED_DATA:
		default: {
			MonoBundledDataResource *data = (MonoBundledDataResource *)resource_to_bundle;
			g_hash_table_insert (bundled_resources, (gpointer) data->resource.id, data);
			break;
		}
		}
	}

	if (assemblyAdded)
		bundle_contains_assemblies = true;

	if (satelliteAssemblyAdded)
		bundle_contains_satellite_assemblies = true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get retrieves the pointer of the MonoBundledResource associated
// with a key equivalent to the requested resource id. If the requested bundled resource's
// name has been added via mono_bundled_resources_add, a MonoBundled*Resource had been
// preallocated, typically through EmitBundleTask.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledResource * - Pointer to the resource in the hashmap with the key `id`
//

MonoBundledResource *
mono_bundled_resources_get (const char *id)
{
	if (!bundled_resources)
		return NULL;

	return g_hash_table_lookup (bundled_resources, id);
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_contains_assemblies returns whether or not assemblies
// have been added to the bundled resource hash table via mono_bundled_resources_add.
//
// Returns:
//  bool - bool value indicating whether or not a bundled assembly resource had been added.
//

bool
mono_bundled_resources_contains_assemblies (void)
{
	return bundle_contains_assemblies;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_contains_satellite_assemblies returns whether or not satellite assemblies
// have been added to the bundled resource hash table via mono_bundled_resources_add.
//
// Returns:
//  bool - bool value indicating whether or not a bundled satellite assembly resource had been added.
//

bool
mono_bundled_resources_contains_satellite_assemblies (void)
{
	return bundle_contains_satellite_assemblies;
}
