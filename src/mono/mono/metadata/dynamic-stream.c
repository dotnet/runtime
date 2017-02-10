/**
 * \file
 * MonoDynamicStream
 * Copyright 2016 Microsoft
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include "mono/metadata/dynamic-stream-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-error-internals.h"

void
mono_dynstream_init (MonoDynamicStream *sh)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	sh->index = 0;
	sh->alloc_size = 4096;
	sh->data = (char *)g_malloc (4096);
	sh->hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_dynstream_insert_string (sh, "");
}

static void
make_room_in_stream (MonoDynamicStream *stream, int size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (size <= stream->alloc_size)
		return;
	
	while (stream->alloc_size <= size) {
		if (stream->alloc_size < 4096)
			stream->alloc_size = 4096;
		else
			stream->alloc_size *= 2;
	}
	
	stream->data = (char *)g_realloc (stream->data, stream->alloc_size);
}

guint32
mono_dynstream_insert_string (MonoDynamicStream *sh, const char *str)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 idx;
	guint32 len;
	gpointer oldkey, oldval;

	if (g_hash_table_lookup_extended (sh->hash, str, &oldkey, &oldval))
		return GPOINTER_TO_UINT (oldval);

	len = strlen (str) + 1;
	idx = sh->index;
	
	make_room_in_stream (sh, idx + len);

	/*
	 * We strdup the string even if we already copy them in sh->data
	 * so that the string pointers in the hash remain valid even if
	 * we need to realloc sh->data. We may want to avoid that later.
	 */
	g_hash_table_insert (sh->hash, g_strdup (str), GUINT_TO_POINTER (idx));
	memcpy (sh->data + idx, str, len);
	sh->index += len;
	return idx;
}

guint32
mono_dynstream_insert_mstring (MonoDynamicStream *sh, MonoString *str, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	error_init (error);
	char *name = mono_string_to_utf8_checked (str, error);
	return_val_if_nok (error, -1);
	guint32 idx;
	idx = mono_dynstream_insert_string (sh, name);
	g_free (name);
	return idx;
}

guint32
mono_dynstream_add_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 idx;
	
	make_room_in_stream (stream, stream->index + len);
	memcpy (stream->data + stream->index, data, len);
	idx = stream->index;
	stream->index += len;
	/* 
	 * align index? Not without adding an additional param that controls it since
	 * we may store a blob value in pieces.
	 */
	return idx;
}

guint32
mono_dynstream_add_zero (MonoDynamicStream *stream, guint32 len)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 idx;
	
	make_room_in_stream (stream, stream->index + len);
	memset (stream->data + stream->index, 0, len);
	idx = stream->index;
	stream->index += len;
	return idx;
}

void
mono_dynstream_data_align (MonoDynamicStream *stream)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 count = stream->index % 4;

	/* we assume the stream data will be aligned */
	if (count)
		mono_dynstream_add_zero (stream, 4 - count);
}

