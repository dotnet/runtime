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
#include <mono/metadata/icalls.h>

ICALL_EXPORT
void
mono_mmap_close (void *mmap_handle, MonoError *error);

// inheritability is an enum with the values 0 and 1.
ICALL_EXPORT
void
mono_mmap_configure_inheritability (void *mmap_handle, gint32 inheritability, MonoError *error);

ICALL_EXPORT
void
mono_mmap_flush (void *mmap_handle, MonoError *error);

ICALL_EXPORT
void*
mono_mmap_open_file (const gunichar2 *path, gint path_length, int mode, const gunichar2 *mapName, gint mapName_length, gint64 *capacity, int access, int options, int *ioerror, MonoError *error);

ICALL_EXPORT
void*
mono_mmap_open_handle (void *handle, const gunichar2 *mapName, gint mapName_length, gint64 *capacity, int access, int options, int *ioerror, MonoError *error);

ICALL_EXPORT
int
mono_mmap_map (void *handle, gint64 offset, gint64 *size, int access, void **mmap_handle, void **base_address, MonoError *error);

ICALL_EXPORT
MonoBoolean
mono_mmap_unmap (void *base_address, MonoError *error);

#endif /* _MONO_METADATA_FILE_MMAP_H_ */
