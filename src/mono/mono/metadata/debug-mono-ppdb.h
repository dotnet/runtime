/*
 * debug-mono-ppdb.c: Support for the portable PDB symbol
 * file format
 *
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 */

#ifndef __MONO_METADATA_DEBUG_MONO_PPDB_H__
#define __MONO_METADATA_DEBUG_MONO_PPDB_H__

#include <config.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-debug.h>

MonoPPDBFile*
mono_ppdb_load_file (MonoImage *image);

void
mono_ppdb_close (MonoDebugHandle *handle);

MonoDebugMethodInfo *
mono_ppdb_lookup_method (MonoDebugHandle *handle, MonoMethod *method);

MonoDebugSourceLocation *
mono_ppdb_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset);

void
mono_ppdb_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points);

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals (MonoDebugMethodInfo *minfo);

#endif
