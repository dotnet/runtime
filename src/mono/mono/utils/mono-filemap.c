/**
 * \file
 * Unix/Windows implementation for filemap.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2008-2008 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include <errno.h>

#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#include <fcntl.h>
#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <stdio.h>

#include "mono-mmap.h"

MonoFileMap *
mono_file_map_open (const char* name)
{
#ifdef WIN32
	gunichar2 *wname = g_utf8_to_utf16 (name, -1, 0, 0, 0);
	MonoFileMap *result;

	if (wname == NULL)
		return NULL;
	result = (MonoFileMap *) _wfopen ((wchar_t *) wname, L"rb");
	g_free (wname);
	return result;
#else
	int fd;
	while (-1 == (fd = open (name, O_RDONLY)) && errno == EINTR);
	if (fd < 0)
		return NULL;
	return (MonoFileMap *)(size_t)fd;
#endif
}

guint64
mono_file_map_size (MonoFileMap *fmap)
{
	struct stat stat_buf;
	int result;
	while (-1 == (result = fstat (mono_file_map_fd (fmap), &stat_buf)) && errno == EINTR);
	if (result < 0)
		return 0;
	return stat_buf.st_size;
}

int
mono_file_map_fd (MonoFileMap *fmap)
{
#ifdef WIN32
	return _fileno ((FILE*)fmap);
#else
	return (int)(size_t)fmap;
#endif
}

int
mono_file_map_close (MonoFileMap *fmap)
{
#ifdef WIN32
	return fclose ((FILE*)fmap);
#else
	return close (mono_file_map_fd (fmap));
#endif
}

#if !defined (HOST_WIN32)

static mono_file_map_alloc_fn alloc_fn = (mono_file_map_alloc_fn) malloc;
static mono_file_map_release_fn release_fn = (mono_file_map_release_fn) free;

void
mono_file_map_set_allocator (mono_file_map_alloc_fn alloc, mono_file_map_release_fn release)
{
	alloc_fn = alloc == NULL     ? (mono_file_map_alloc_fn) malloc : alloc;
	release_fn = release == NULL ? (mono_file_map_release_fn) free : release;
}

void *
mono_file_map_fileio (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	off_t lseek_result;
	guint64 cur_offset;
	size_t bytes_read;
	void *ptr = (*alloc_fn) (length);
	if (!ptr)
		return NULL;
	while (-1 == (lseek_result = lseek (fd, 0, SEEK_CUR)) && errno == EINTR);
	while (-1 == (lseek_result = lseek (fd, offset, SEEK_SET)) && errno == EINTR);
	cur_offset = lseek_result;
	if (cur_offset != offset) {
		(*release_fn) (ptr);
		return NULL;
	}
	size_t readSoFar = 0;
	while (readSoFar < length)
	{
		while (-1 == (bytes_read = read (fd, (unsigned char*)ptr + readSoFar, length - readSoFar)) && errno == EINTR);
		if (bytes_read <= 0) break;
		readSoFar += bytes_read;
	}
	if (readSoFar != length)
		return NULL;
	while (-1 == lseek (fd, cur_offset, SEEK_SET) && errno == EINTR);
	*ret_handle = NULL;
	return ptr;
}

int
mono_file_unmap_fileio (void *addr, void *handle)
{
	(*release_fn) (addr);
	return 0;
}
#if !defined(HAVE_MMAP)
void *
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	return mono_file_map_fileio (length, flags, fd, offset, ret_handle);
}

int
mono_file_unmap (void *addr, void *handle)
{
	return mono_file_unmap_fileio(addr, handle);
}
#endif
#endif
