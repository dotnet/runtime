#include <config.h>

#include "mono/metadata/loaded-images-internals.h"
#include "mono/metadata/metadata-internals.h"

#ifndef ENABLE_NETCORE
/* Global image hashes should not be in netcore Mono */

static MonoLoadedImages global_loaded_images; /* zero initalized is good enough */

MonoLoadedImages*
mono_get_global_loaded_images (void)
{
	return &global_loaded_images;
}

// This is support for the mempool reference tracking feature in checked-build,
// but lives in loaded-images-global.c due to use of static variables of this
// file.

/**
 * mono_find_image_owner:
 *
 * Find the image, if any, which a given pointer is located in the memory of.
 */
MonoImage *
mono_find_image_owner (void *ptr)
{
	MonoLoadedImages *li = mono_get_global_loaded_images ();
	mono_images_lock ();

	MonoImage *owner = NULL;

	// Iterate over both by-path image hashes
	const int hash_candidates[] = {MONO_LOADED_IMAGES_HASH_PATH, MONO_LOADED_IMAGES_HASH_PATH_REFONLY};
	int hash_idx;
	for (hash_idx = 0; !owner && hash_idx < G_N_ELEMENTS (hash_candidates); hash_idx++)
	{
		GHashTable *target = li->loaded_images_hashes [hash_candidates [hash_idx]];
		GHashTableIter iter;
		MonoImage *image;

		// Iterate over images within a hash
		g_hash_table_iter_init (&iter, target);
		while (!owner && g_hash_table_iter_next(&iter, NULL, (gpointer *)&image))
		{
			mono_image_lock (image);
			if (mono_mempool_contains_addr (image->mempool, ptr))
				owner = image;
			mono_image_unlock (image);
		}
	}

	mono_images_unlock ();

	return owner;
}

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc)
{
	return mono_get_global_loaded_images ();
}

#endif /* ENABLE_NETCORE */
