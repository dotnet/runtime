/*
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_FILE_IO_INTERNALS_H__
#define __MONO_FILE_IO_INTERNALS_H__

#include <config.h>
#include <glib.h>
#include "mono/metadata/object.h"
#include "mono/metadata/object-internals.h"

gboolean
mono_file_io_move_file (gunichar2 *path, gunichar2 *dest, gint32 *error);

gboolean
mono_file_io_copy_file (gunichar2 *path, gunichar2 *dest, gboolean overwrite, gint32 *error);

gint64
mono_file_io_get_file_size (HANDLE handle, gint32 *error);

gboolean
mono_file_io_lock_file (HANDLE handle, gint64 position, gint64 length, gint32 *error);

gboolean
mono_file_io_replace_file (gunichar2 *destinationFileName, gunichar2 *sourceFileName,
			   gunichar2 *destinationBackupFileName, guint32 flags, gint32 *error);

gboolean
mono_file_io_unlock_file (HANDLE handle, gint64 position, gint64 length, gint32 *error);

HANDLE
mono_file_io_get_console_output (void);

HANDLE
mono_file_io_get_console_error (void);

HANDLE
mono_file_io_get_console_input (void);

#endif /* __MONO_FILE_IO_INTERNALS_H__ */
