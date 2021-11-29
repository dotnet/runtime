/**
 * \file
 * Support for the portable PDB symbol file format
 *
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2015 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_METADATA_DEBUG_MONO_PPDB_H__
#define __MONO_METADATA_DEBUG_MONO_PPDB_H__

#include <config.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-debug.h>

typedef struct _MonoDebugInformationEnc		MonoDebugInformationEnc;


struct _MonoPPDBFile {
	MonoImage *image;
	GHashTable *doc_hash;
	GHashTable *method_hash;
	gboolean is_embedded;
};

struct _MonoDebugInformationEnc {
	MonoPPDBFile *ppdb_file;
	int idx;
};

MonoPPDBFile*
mono_ppdb_load_file (MonoImage *image, const guint8 *raw_contents, int size);

MONO_COMPONENT_API void
mono_ppdb_close (MonoPPDBFile *ppdb_file);

MonoDebugMethodInfo *
mono_ppdb_lookup_method (MonoDebugHandle *handle, MonoMethod *method);

MonoDebugSourceLocation *
mono_ppdb_lookup_location (MonoDebugMethodInfo *minfo, uint32_t offset);

MonoDebugSourceLocation *
mono_ppdb_lookup_location_enc (MonoPPDBFile *ppdb_file, int idx, uint32_t offset);

void
mono_ppdb_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points);

gboolean 
mono_ppdb_get_seq_points_enc (MonoDebugMethodInfo *minfo, MonoPPDBFile *ppdb_file, int idx, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points);

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals (MonoDebugMethodInfo *minfo);

MonoDebugLocalsInfo*
mono_ppdb_lookup_locals_enc (MonoImage *image, int method_idx);

MonoDebugMethodAsyncInfo*
mono_ppdb_lookup_method_async_debug_info (MonoDebugMethodInfo *minfo);

MONO_COMPONENT_API MonoImage *
mono_ppdb_get_image (MonoPPDBFile *ppdb);

char *
mono_ppdb_get_sourcelink (MonoDebugHandle *handle);

gboolean 
mono_ppdb_is_embedded (MonoPPDBFile *ppdb);

MONO_COMPONENT_API MonoPPDBFile*
mono_create_ppdb_file (MonoImage *ppdb_image, gboolean is_embedded_ppdb);

#endif
