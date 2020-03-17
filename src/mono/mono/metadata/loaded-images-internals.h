/**
* \file
*/

#ifndef _MONO_METADATA_IMAGE_HASHES_H_
#define _MONO_METADATA_IMAGE_HASHES_H_

#include <glib.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/loader-internals.h>
#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-error.h>

/*
 * The "loaded images" hashes keep track of the various assemblies and netmodules loaded
 * There are four, for all combinations of [look up by path or assembly name?]
 * and [normal or reflection-only load?, as in Assembly.ReflectionOnlyLoad]
 */
enum {
	MONO_LOADED_IMAGES_HASH_PATH = 0,
	MONO_LOADED_IMAGES_HASH_PATH_REFONLY = 1,
	MONO_LOADED_IMAGES_HASH_NAME = 2,
	MONO_LOADED_IMAGES_HASH_NAME_REFONLY = 3,
	MONO_LOADED_IMAGES_HASH_COUNT = 4
};

struct _MonoLoadedImages {
	MonoAssemblyLoadContext *owner; /* NULL if global */
	GHashTable *loaded_images_hashes [MONO_LOADED_IMAGES_HASH_COUNT];
};

void
mono_loaded_images_init (MonoLoadedImages *li, MonoAssemblyLoadContext *owner);

void
mono_loaded_images_cleanup (MonoLoadedImages *li, gboolean shutdown);

void
mono_loaded_images_free (MonoLoadedImages *li);

GHashTable *
mono_loaded_images_get_hash (MonoLoadedImages *li, gboolean refonly);

GHashTable *
mono_loaded_images_get_by_name_hash (MonoLoadedImages *li, gboolean refonly);

gboolean
mono_loaded_images_remove_image (MonoImage *image);

MonoLoadedImages*
mono_image_get_loaded_images_for_modules (MonoImage *image);

#ifndef ENABLE_NETCORE
MonoLoadedImages*
mono_get_global_loaded_images (void);
#endif

MonoImage *
mono_find_image_owner (void *ptr);

void
mono_images_lock (void);

void
mono_images_unlock (void);

#endif
