/* 
 * Copyright 2015 Xamarin Inc
 */
#ifndef __MONO_METADATA_IMAGE_INTERNALS_H__
#define __MONO_METADATA_IMAGE_INTERNALS_H__

#ifdef CHECKED_BUILD

#include <mono/metadata/image.h>
#include <mono/metadata/metadata-internals.h>

typedef struct
{
	MonoImage *image;
	MonoImageSet *image_set;
} MonoMemPoolOwner;

static MonoMemPoolOwner mono_mempool_no_owner = {NULL,NULL};

static gboolean
check_mempool_owner_eq (MonoMemPoolOwner a, MonoMemPoolOwner b)
{
	return a.image == b.image && a.image_set == b.image_set;
}

MonoMemPoolOwner
mono_find_mempool_owner (void *ptr);

#endif /* CHECKED_BUILD */

#endif /* __MONO_METADATA_IMAGE_INTERNALS_H__ */
