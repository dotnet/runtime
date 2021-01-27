#include "config.h"

#include "mono/metadata/loaded-images-internals.h"
#include "mono/metadata/image-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/utils/mono-logger-internals.h"

void
mono_loaded_images_init (MonoLoadedImages *li, MonoAssemblyLoadContext *owner)
{
	li->owner = owner;
	for (int hash_idx = 0; hash_idx < MONO_LOADED_IMAGES_HASH_COUNT; hash_idx++)
		li->loaded_images_hashes [hash_idx] = g_hash_table_new (g_str_hash, g_str_equal);
}

void
mono_loaded_images_cleanup (MonoLoadedImages *li, gboolean shutdown)
{
	if (shutdown) {
		GHashTableIter iter;
		MonoImage *image;

		// If an assembly image is still loaded at shutdown, this could indicate managed code is still running.
		// Reflection-only images being still loaded doesn't indicate anything as harmful, so we don't check for it.
		g_hash_table_iter_init (&iter, mono_loaded_images_get_hash (li, FALSE));
		while (g_hash_table_iter_next (&iter, NULL, (void**)&image))
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly image '%s' [%p] still loaded at shutdown.", image->name, image);
	}

	for (int hash_idx = 0; hash_idx < MONO_LOADED_IMAGES_HASH_COUNT; hash_idx++) {
		g_hash_table_destroy (li->loaded_images_hashes [hash_idx]);
		li->loaded_images_hashes [hash_idx] = NULL;
	}
}

void
mono_loaded_images_free (MonoLoadedImages *li)
{
	mono_loaded_images_cleanup (li, FALSE);
	g_free (li);
}

GHashTable *
mono_loaded_images_get_hash (MonoLoadedImages *li, gboolean refonly)
{
	g_assert (li != NULL);
	GHashTable **loaded_images_hashes = &li->loaded_images_hashes[0];
	int idx = refonly ? MONO_LOADED_IMAGES_HASH_PATH_REFONLY : MONO_LOADED_IMAGES_HASH_PATH;
	return loaded_images_hashes [idx];
}

GHashTable *
mono_loaded_images_get_by_name_hash (MonoLoadedImages *li, gboolean refonly)
{
	g_assert (li != NULL);
	GHashTable **loaded_images_hashes = &li->loaded_images_hashes[0];
	int idx = refonly ? MONO_LOADED_IMAGES_HASH_NAME_REFONLY : MONO_LOADED_IMAGES_HASH_NAME;
	return loaded_images_hashes [idx];
}

static MonoLoadedImages *
loaded_images_get_owner (MonoImage *image)
{
	/* image->alc could be NULL if we're closing an image that wasn't
	 * registered yet (for example if two threads raced to open it and one
	 * of them lost) */
	MonoAssemblyLoadContext *alc = mono_image_get_alc (image);
	return mono_alc_get_loaded_images (alc);
}

/**
 * Atomically decrements the image refcount and removes it from the loaded
 * images hashes if the refcount becomes zero.
 *
 * Returns TRUE if image unloading should proceed or FALSE otherwise.
 *
 * LOCKING: takes the images lock
 */
gboolean
mono_loaded_images_remove_image (MonoImage *image)
{
	char *name = NULL;
	gboolean proceed = FALSE;
	/*
	 * Atomically decrement the refcount and remove ourselves from the hash tables, so
	 * register_image () can't grab an image which is being closed.
	 */
	mono_images_lock ();

	if (mono_atomic_dec_i32 (&image->ref_count) > 0)
		goto done;

	MonoLoadedImages *li;
	li = loaded_images_get_owner (image);
	if (!li) {
		/* we weren't registered; maybe lost to another image */
		proceed = TRUE;
		goto done;
	}
	GHashTable *loaded_images, *loaded_images_by_name;
	MonoImage *image2;

	loaded_images         = mono_loaded_images_get_hash (li, image->ref_only);
	loaded_images_by_name = mono_loaded_images_get_by_name_hash (li, image->ref_only);

	name = image->name;
#ifdef ENABLE_NETCORE
	char *name_with_culture = mono_image_get_name_with_culture_if_needed (image);
	if (name_with_culture)
		name = name_with_culture;
#endif
	image2 = (MonoImage *)g_hash_table_lookup (loaded_images, name);
	if (image == image2) {
		/* This is not true if we are called from mono_image_open () */
		g_hash_table_remove (loaded_images, name);
	}
	if (image->assembly_name && (g_hash_table_lookup (loaded_images_by_name, image->assembly_name) == image))
		g_hash_table_remove (loaded_images_by_name, (char *) image->assembly_name);

	proceed = TRUE;
#ifdef ENABLE_NETCORE
	g_free (name_with_culture);
#endif
done:
	mono_images_unlock ();

	return proceed;
	
}

MonoLoadedImages*
mono_image_get_loaded_images_for_modules (MonoImage *image)
{
#ifndef ENABLE_NETCORE
	return mono_get_global_loaded_images ();
#else
	g_assert_not_reached ();
#endif
}
