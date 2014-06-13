/*
 * file-mmap-posix.c: File mmap internal calls
 *
 * Author:
 *	Rodrigo Kumpera
 *
 * Copyright 2014 Xamarin Inc (http://www.xamarin.com)
 */

#include <config.h>

#ifdef TARGET_WIN32

#include <glib.h>
#include <string.h>
#include <errno.h>


#include <mono/metadata/object.h>
#include <mono/metadata/file-mmap.h>

void *
mono_mmap_open_file (MonoString *path, int mode, MonoString *mapName, gint64 *capacity, int access, int options, int *error)
{
	g_error ("No windows backend");
	return NULL;
}

void *
mono_mmap_open_handle (void *handle, MonoString *mapName, gint64 *capacity, int access, int options, int *error)
{
	g_error ("No windows backend");
	return NULL;
}

void
mono_mmap_close (void *mmap_handle)
{
	g_error ("No windows backend");
}

void
mono_mmap_configure_inheritability (void *mmap_handle, gboolean inheritability)
{
	g_error ("No windows backend");
}

void
mono_mmap_flush (void *mmap_handle)
{
	g_error ("No windows backend");
}



int
mono_mmap_map (void *handle, gint64 offset, gint64 *size, int access, void **mmap_handle, void **base_address)
{
	g_error ("No windows backend");
	return 0;
}

gboolean
mono_mmap_unmap (void *mmap_handle)
{
	g_error ("No windows backend");
	return TRUE;
}

#endif
