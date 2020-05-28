#include "config.h"

#include "mono/metadata/loaded-images-internals.h"

#ifdef ENABLE_NETCORE
/* Should be compiling loaded-images-netcore.c only for netcore Mono */

// This is support for the mempool reference tracking feature in checked-build,
// but lives in loaded-images-netcore.c due to use of static variables of this
// file.

/**
 * mono_find_image_owner:
 *
 * Find the image, if any, which a given pointer is located in the memory of.
 */
MonoImage *
mono_find_image_owner (void *ptr)
{
	/* FIXME: this function is a bit annoying to implement without a global
	 * table of all the loaded images.  We need to traverse all the domains
	 * and each ALC in each domain. */
	return NULL;
}

MonoLoadedImages *
mono_alc_get_loaded_images (MonoAssemblyLoadContext *alc)
{
	g_assert (alc);
	g_assert (alc->loaded_images);
	return alc->loaded_images;
}

#else

MONO_EMPTY_SOURCE_FILE (loaded_images_netcore);

#endif /* ENABLE_NETCORE */
