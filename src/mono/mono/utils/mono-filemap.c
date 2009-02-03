/*
 * mono-filemap.c: Unix/Windows implementation for filemap.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2008-2008 Novell, Inc.
 */

#include "config.h"

#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#include <fcntl.h>
#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>

#include "mono-mmap.h"

MonoFileMap *
mono_file_map_open (const char* name)
{
	return (MonoFileMap *)fopen (name, "rb");
}

guint64 
mono_file_map_size (MonoFileMap *fmap)
{
	struct stat stat_buf;
	if (fstat (fileno ((FILE*)fmap), &stat_buf) < 0)
		return 0;
	return stat_buf.st_size;
}

int
mono_file_map_fd (MonoFileMap *fmap)
{
	return fileno ((FILE*)fmap);
}

int 
mono_file_map_close (MonoFileMap *fmap)
{
	return fclose ((FILE*)fmap);
}

void *
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	guint64 cur_offset;
	size_t bytes_read;
	void *ptr = malloc (length);
	if (!ptr)
		return NULL;
	cur_offset = lseek (fd, 0, SEEK_CUR);
	if (lseek (fd, offset, SEEK_SET) != offset) {
		free (ptr);
		return NULL;
	}
	bytes_read = read (fd, ptr, length);
	lseek (fd, cur_offset, SEEK_SET);
	*ret_handle = NULL;
	return ptr;
}

