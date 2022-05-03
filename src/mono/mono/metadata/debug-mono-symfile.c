/**
 * \file
 *
 *   Support for reading debug info from .mdb files. Not supported anymore.
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <string.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <sys/stat.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-mono-symfile.h>

MonoSymbolFile *
mono_debug_open_mono_symbols (MonoDebugHandle *handle, const uint8_t *raw_contents,
			      int size, gboolean in_the_debugger)
{
	return NULL;
}

void
mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile)
{
}

mono_bool
mono_debug_symfile_is_loaded (MonoSymbolFile *symfile)
{
	return FALSE;
}

MonoDebugMethodInfo *
mono_debug_symfile_lookup_method (MonoDebugHandle *handle, MonoMethod *method)
{
	return NULL;
}

void
mono_debug_symfile_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	g_assert_not_reached ();
}

MonoDebugSourceLocation *
mono_debug_symfile_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset)
{
	return NULL;
}

MonoDebugLocalsInfo*
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo)
{
	return NULL;
}

void
mono_debug_symfile_free_location (MonoDebugSourceLocation  *location)
{
}
