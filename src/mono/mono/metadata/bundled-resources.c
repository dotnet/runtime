// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>
#include <stdbool.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/bundled-resources-internals.h>
#include <mono/metadata/webcil-loader.h>

static GHashTable *bundled_resources = NULL;
static bool bundled_resources_contains_assemblies = false;
static bool bundled_resources_contains_satellite_assemblies = false;

typedef struct _BundledResourcesChainedFreeFunc {
	free_bundled_resource_func free_func;
	void *free_data;
	void *next;
} BundledResourcesChainedFreeFunc;

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_free frees all memory allocated for bundled resources.
// It should only be called when the runtime no longer needs access to the data,
// most likely to happen during runtime shutdown.
//

void
mono_bundled_resources_free (void)
{
	g_assert (mono_runtime_is_shutting_down ());

	g_hash_table_destroy (bundled_resources);
	bundled_resources = NULL;

	bundled_resources_contains_assemblies = false;
	bundled_resources_contains_satellite_assemblies = false;
}

//---------------------------------------------------------------------------------------
//
// bundled_resources_value_destroy_func frees the memory allocated by the hashtable's
// MonoBundled*Resource by invoking its underlying free_bundled_resource_func when possible.
//

static void
bundled_resources_value_destroy_func (void *resource)
{
	MonoBundledResource *value = (MonoBundledResource *)resource;
	if (value->free_func)
		value->free_func (resource, value->free_data);
}

static bool
bundled_resources_is_known_assembly_extension (const char *ext)
{
#ifdef ENABLE_WEBCIL
	return !strcmp (ext, ".dll") || !strcmp (ext, ".webcil") || !strcmp (ext, MONO_WEBCIL_IN_WASM_EXTENSION);
#else
	return !strcmp (ext, ".dll") || !strcmp (ext, MONO_WEBCIL_IN_WASM_EXTENSION);
#endif
}

static gboolean
bundled_resources_resource_id_equal (const char *id_one, const char *id_two)
{
	const char *extension_one = strrchr (id_one, '.');
	const char *extension_two = strrchr (id_two, '.');
	if (extension_one && extension_two && bundled_resources_is_known_assembly_extension (extension_one) && bundled_resources_is_known_assembly_extension (extension_two)) {
		size_t len_one = extension_one - id_one;
		size_t len_two = extension_two - id_two;
		return (len_one == len_two) && !strncmp (id_one, id_two, len_one);
	}

	return !strcmp (id_one, id_two);
}

static guint
bundled_resources_resource_id_hash (const char *id)
{
	const char *current = id;
	const char *extension = NULL;
	guint previous_hash = 0;
	guint hash = 0;

	while (*current) {
		hash = (hash << 5) - (hash + *current);
		if (*current == '.') {
			extension = current;
			previous_hash = hash;
		}
		current++;
	}

	// alias all extensions to .dll
	if (extension && bundled_resources_is_known_assembly_extension (extension)) {
		hash = previous_hash;
		hash = (hash << 5) - (hash + 'd');
		hash = (hash << 5) - (hash + 'l');
		hash = (hash << 5) - (hash + 'l');
	}

	return hash;
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
	MonoDomain *domain = mono_get_root_domain ();
	g_assert (!domain);

	if (!bundled_resources)
		bundled_resources = g_hash_table_new_full ((GHashFunc)bundled_resources_resource_id_hash, (GEqualFunc)bundled_resources_resource_id_equal, NULL, bundled_resources_value_destroy_func);

	bool assemblyAdded = false;
	bool satelliteAssemblyAdded = false;

	for (uint32_t i = 0; i < len; ++i) {
		MonoBundledResource *resource_to_bundle = (MonoBundledResource *)resources_to_bundle[i];
		if (resource_to_bundle->type == MONO_BUNDLED_ASSEMBLY)
			assemblyAdded = true;

		if (resource_to_bundle->type == MONO_BUNDLED_SATELLITE_ASSEMBLY)
			satelliteAssemblyAdded = true;

		g_hash_table_insert (bundled_resources, (gpointer) resource_to_bundle->id, resource_to_bundle);
	}

	if (assemblyAdded)
		bundled_resources_contains_assemblies = true;

	if (satelliteAssemblyAdded)
		bundled_resources_contains_satellite_assemblies = true;
}


//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get retrieves the pointer of the MonoBundledResource associated
// with a key equivalent to the requested resource id.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledResource * - Pointer to the resource in the hashmap with the key `id`
//

static MonoBundledResource *
bundled_resources_get (const char *id)
{
	if (!bundled_resources)
		return NULL;

	return g_hash_table_lookup (bundled_resources, id);
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_assembly_resource retrieves MonoBundledAssemblyResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledAssemblyResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_assembly_resource_values`
// in external contexts to grab assembly and symbol data.
//

static MonoBundledAssemblyResource *
bundled_resources_get_assembly_resource (const char *id)
{
	MonoBundledAssemblyResource *assembly =
		(MonoBundledAssemblyResource*)bundled_resources_get (id);
	if (!assembly)
		return NULL;
	g_assert (assembly->resource.type == MONO_BUNDLED_ASSEMBLY);
	return assembly;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_satellite_assembly_resource retrieves MonoBundledSatelliteAssemblyResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledSatelliteAssemblyResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_satellite_assembly_resource_values`
// in external contexts to grab satellite assembly data.
//

static MonoBundledSatelliteAssemblyResource *
bundled_resources_get_satellite_assembly_resource (const char *id)
{
	MonoBundledSatelliteAssemblyResource *satellite_assembly =
		(MonoBundledSatelliteAssemblyResource*)bundled_resources_get (id);
	if (!satellite_assembly)
		return NULL;
	g_assert (satellite_assembly->resource.type == MONO_BUNDLED_SATELLITE_ASSEMBLY);
	return satellite_assembly;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_data_resource retrieves MonoBundledDataResource* associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//
// Returns:
//  MonoBundledDataResource * - Pointer to the bundled assembly resource in the hashmap with the key `id`
//
// Note: As MonoBundled*Resource types are not public, prefer `mono_bundled_resources_get_data_resource_values`
// in external contexts to grab data.
//

static MonoBundledDataResource *
bundled_resources_get_data_resource (const char *id)
{
	MonoBundledDataResource *data =
		(MonoBundledDataResource*)bundled_resources_get (id);
	if (!data)
		return NULL;
	g_assert (data->resource.type == MONO_BUNDLED_DATA);
	return data;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_assembly_resource_values retrieves assembly data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at assembly byte data
//  ** size_out - address to point at assembly byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledAssemblyResource->assembly was found with key 'id'
//

bool
mono_bundled_resources_get_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	MonoBundledAssemblyResource *bundled_assembly_resource = bundled_resources_get_assembly_resource (id);
	if (!bundled_assembly_resource ||
		!bundled_assembly_resource->assembly.data ||
		bundled_assembly_resource->assembly.size == 0)
		return false;

	if (data_out)
		*data_out = bundled_assembly_resource->assembly.data;
	if (size_out)
		*size_out = bundled_assembly_resource->assembly.size;

	return true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_assembly_resource_symbol_values retrieves assembly symbol data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** symbol_data_out - address to point at assembly symbol byte data
//  ** symbol_size_out - address to point at assembly symbol byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledAssemblyResource->symbol_data was found with key 'id'
//

bool
mono_bundled_resources_get_assembly_resource_symbol_values (const char *id, const uint8_t **symbol_data_out, uint32_t *symbol_size_out)
{
	MonoBundledAssemblyResource *bundled_assembly_resource = bundled_resources_get_assembly_resource (id);
	if (!bundled_assembly_resource ||
		!bundled_assembly_resource->symbol_data.data ||
		bundled_assembly_resource->symbol_data.size == 0)
		return false;

	if (symbol_data_out)
		*symbol_data_out = bundled_assembly_resource->symbol_data.data;
	if (symbol_size_out)
		*symbol_size_out = bundled_assembly_resource->symbol_data.size;

	return true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_satellite_assembly_resource_values retrieves satellite assembly data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at satellite assembly byte data
//  ** size_out - address to point at satellite assembly byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledSatelliteAssemblyResource was found with key 'id'
//

bool
mono_bundled_resources_get_satellite_assembly_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	MonoBundledSatelliteAssemblyResource *bundled_satellite_assembly_resource = bundled_resources_get_satellite_assembly_resource (id);
	if (!bundled_satellite_assembly_resource ||
		!bundled_satellite_assembly_resource->satellite_assembly.data ||
		bundled_satellite_assembly_resource->satellite_assembly.size == 0)
		return false;

	if (data_out)
		*data_out = bundled_satellite_assembly_resource->satellite_assembly.data;
	if (size_out)
		*size_out = bundled_satellite_assembly_resource->satellite_assembly.size;

	return true;
}

//---------------------------------------------------------------------------------------
//
// mono_bundled_resources_get_data_resource_values retrieves data associated
// with a key equivalent to the requested resource id if found.
//
// Arguments:
//  * id - Unique name of the resource
//  ** data_out - address to point at resource byte data
//  ** size_out - address to point at resource byte data size
//
// Returns:
//  bool - whether or not a valid MonoBundledDataResource was found with key 'id'
//

bool
mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	MonoBundledDataResource *bundled_data_resource = bundled_resources_get_data_resource (id);
	if (!bundled_data_resource ||
		!bundled_data_resource->data.data ||
		bundled_data_resource->data.size == 0)
		return false;

	if (data_out)
		*data_out = bundled_data_resource->data.data;
	if (size_out)
		*size_out = bundled_data_resource->data.size;

	return true;
}

static void
bundled_resources_chained_free_func (void *resource, void *free_data)
{
	BundledResourcesChainedFreeFunc *node = (BundledResourcesChainedFreeFunc *)free_data;
	if (node && node->free_func)
		node->free_func (resource, node->free_data);
	if (node && node->next)
		bundled_resources_chained_free_func (resource, node->next);

	g_free (free_data);
}

static void
bundled_resources_free_func (void *resource, void *free_data)
{
	bundled_resources_chained_free_func (resource, free_data);
	g_free (resource);
}

static void
bundled_resource_add_free_func (MonoBundledResource *resource, free_bundled_resource_func free_func, void *free_data)
{
	if (!free_func)
		return;

	if (!resource->free_func) {
		resource->free_func = free_func;
		resource->free_data = free_data;
	} else if (resource->free_func == bundled_resources_chained_free_func || resource->free_func == bundled_resources_free_func) {
		BundledResourcesChainedFreeFunc *node = g_new0 (BundledResourcesChainedFreeFunc, 1);
		node->free_func = free_func;
		node->free_data = free_data;
		node->next = resource->free_data;
		resource->free_data = node;
	} else {
		BundledResourcesChainedFreeFunc *node1 = g_new0 (BundledResourcesChainedFreeFunc, 1);
		BundledResourcesChainedFreeFunc *node2 = g_new0 (BundledResourcesChainedFreeFunc, 2);

		node2->free_func = resource->free_func;
		node2->free_data = resource->free_data;

		node1->free_func = free_func;
		node1->free_data = free_data;
		node1->next = node2;

		resource->free_func = bundled_resources_chained_free_func;
		resource->free_data = node1;
	}
}

void
mono_bundled_resources_add_assembly_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, free_bundled_resource_func free_func, void *free_data)
{
	// Check if assembly pdb counterpart had been added via mono_register_symfile_for_assembly
	MonoBundledAssemblyResource *assembly_resource = bundled_resources_get_assembly_resource (name);
	if (!assembly_resource) {
		assembly_resource = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource->resource.type = MONO_BUNDLED_ASSEMBLY;
		assembly_resource->resource.id = id;
		assembly_resource->resource.free_func = bundled_resources_free_func;
		bundled_resource_add_free_func ((MonoBundledResource *)assembly_resource, free_func, free_data);
		mono_bundled_resources_add ((MonoBundledResource **)&assembly_resource, 1);
	} else {
		// Ensure the MonoBundledAssemblyData has not been initialized
		g_assert (!assembly_resource->assembly.name && !assembly_resource->assembly.data && assembly_resource->assembly.size == 0);
		bundled_resource_add_free_func ((MonoBundledResource *)assembly_resource, free_func, free_data);
	}
	assembly_resource->assembly.name = name;
	assembly_resource->assembly.data = data;
	assembly_resource->assembly.size = size;
}

void
mono_bundled_resources_add_assembly_symbol_resource (const char *id, const uint8_t *data, uint32_t size, free_bundled_resource_func free_func, void *free_data)
{
	// Check if assembly dll counterpart had been added via mono_register_bundled_assemblies
	MonoBundledAssemblyResource *assembly_resource = bundled_resources_get_assembly_resource (id);
	if (!assembly_resource) {
		assembly_resource = g_new0 (MonoBundledAssemblyResource, 1);
		assembly_resource->resource.type = MONO_BUNDLED_ASSEMBLY;
		assembly_resource->resource.id = id;
		assembly_resource->resource.free_func = bundled_resources_free_func;
		bundled_resource_add_free_func ((MonoBundledResource *)assembly_resource, free_func, free_data);
		mono_bundled_resources_add ((MonoBundledResource **)&assembly_resource, 1);
	} else {
		// Ensure the MonoBundledSymbolData has not been initialized
		g_assert (!assembly_resource->symbol_data.data && assembly_resource->symbol_data.size == 0);
		bundled_resource_add_free_func ((MonoBundledResource *)assembly_resource, free_func, free_data);
	}
	assembly_resource->symbol_data.data = (const uint8_t *)data;
	assembly_resource->symbol_data.size = (uint32_t)size;
}

void
mono_bundled_resources_add_satellite_assembly_resource (const char *id, const char *name, const char *culture, const uint8_t *data, uint32_t size, free_bundled_resource_func free_func, void *free_data)
{
	MonoBundledSatelliteAssemblyResource *satellite_assembly_resource = bundled_resources_get_satellite_assembly_resource (id);
	g_assert (!satellite_assembly_resource);

	satellite_assembly_resource = g_new0 (MonoBundledSatelliteAssemblyResource, 1);
	satellite_assembly_resource->resource.type = MONO_BUNDLED_SATELLITE_ASSEMBLY;
	satellite_assembly_resource->resource.id = id;
	satellite_assembly_resource->resource.free_func = bundled_resources_free_func;
	satellite_assembly_resource->satellite_assembly.name = name;
	satellite_assembly_resource->satellite_assembly.culture = culture;
	satellite_assembly_resource->satellite_assembly.data = data;
	satellite_assembly_resource->satellite_assembly.size = size;

	bundled_resource_add_free_func ((MonoBundledResource *)satellite_assembly_resource, free_func, free_data);
	mono_bundled_resources_add ((MonoBundledResource **)&satellite_assembly_resource, 1);
}

void
mono_bundled_resources_add_data_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, free_bundled_resource_func free_func, void *free_data)
{
	MonoBundledDataResource *data_resource = bundled_resources_get_data_resource (id);
	g_assert (!data_resource);

	data_resource = g_new0 (MonoBundledDataResource, 1);
	data_resource->resource.type = MONO_BUNDLED_DATA;
	data_resource->resource.id = id;
	data_resource->resource.free_func = bundled_resources_free_func;
	data_resource->data.name = name;
	data_resource->data.data = data;
	data_resource->data.size = size;

	bundled_resource_add_free_func ((MonoBundledResource *)data_resource, free_func, free_data);
	mono_bundled_resources_add ((MonoBundledResource **)&data_resource, 1);
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
	return bundled_resources_contains_assemblies;
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
	return bundled_resources_contains_satellite_assemblies;
}
