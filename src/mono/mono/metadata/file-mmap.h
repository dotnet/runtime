/**
 * \file
 * Managed mmap wrappers.
 *
 * Authors:
 *	Rodrigo Kumpera
 *
 * Copyright 2014 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_FILE_MMAP_H_
#define _MONO_METADATA_FILE_MMAP_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/utils/mono-compiler.h>

extern void mono_mmap_close (void *mmap_handle);

extern void mono_mmap_configure_inheritability (void *mmap_handle, gboolean inheritability);

extern void mono_mmap_flush (void *mmap_handle);

extern void *mono_mmap_open_file (MonoString *string, int mode, MonoString *mapName, gint64 *capacity, int access, int options, int *error);

extern void *mono_mmap_open_handle (void *handle, MonoString *mapName, gint64 *capacity, int access, int options, int *error);

extern int mono_mmap_map (void *handle, gint64 offset, gint64 *size, int access, void **mmap_handle, void **base_address);

extern gboolean mono_mmap_unmap (void *base_address);

#endif /* _MONO_METADATA_FILE_MMAP_H_ */
