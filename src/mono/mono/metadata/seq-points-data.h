/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
 
#ifndef __MONO_SEQ_POINTS_DATA_H__
#define __MONO_SEQ_POINTS_DATA_H__

#include <glib.h>
#include "mono/utils/mono-compiler.h"

#define MONO_SEQ_POINT_FLAG_NONEMPTY_STACK 1
#define MONO_SEQ_POINT_FLAG_EXIT_IL 2
#define MONO_SEQ_POINT_FLAG_NESTED_CALL 4

/* IL offsets used to mark the sequence points belonging to method entry/exit events */
#define METHOD_ENTRY_IL_OFFSET -1
#define METHOD_EXIT_IL_OFFSET 0xffffff

#define SEQ_POINT_AOT_EXT ".msym"

/* Native offset used to mark seq points in dead code */
#define SEQ_POINT_NATIVE_OFFSET_DEAD_CODE -1 

typedef struct {
	int il_offset, native_offset, flags;
	/* Offset of indexes of successor sequence points on the compressed buffer */
	int next_offset;
	/* Number of entries in next */
	int next_len;
} SeqPoint;

typedef struct MonoSeqPointInfo {
	int dummy [1];
} MonoSeqPointInfo;

typedef struct {
	SeqPoint seq_point;
	guint8* ptr;
	guint8* begin;
	guint8* end;
	gboolean has_debug_data;
} SeqPointIterator;

void
mono_seq_point_info_free (gpointer info);

MONO_COMPONENT_API gboolean
mono_seq_point_iterator_next (SeqPointIterator* it);

MONO_COMPONENT_API void
mono_seq_point_iterator_init (SeqPointIterator* it, MonoSeqPointInfo* info);

MONO_COMPONENT_API void
mono_seq_point_init_next (MonoSeqPointInfo* info, SeqPoint sp, SeqPoint* next);

int
mono_seq_point_info_write (MonoSeqPointInfo* info, guint8* buffer);

int
mono_seq_point_info_read (MonoSeqPointInfo** info, guint8* buffer, gboolean copy);

int
mono_seq_point_info_get_write_size (MonoSeqPointInfo* info);

gboolean
mono_seq_point_info_add_seq_point (GByteArray* array, SeqPoint *sp, SeqPoint *last_seq_point, GSList *next, gboolean has_debug_data);

MonoSeqPointInfo*
mono_seq_point_info_new (int len, gboolean alloc_data, guint8 *data, gboolean has_debug_data, int *out_size);

gboolean
mono_seq_point_find_prev_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point);

gboolean
mono_seq_point_find_next_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point);

gboolean
mono_seq_point_find_by_il_offset (MonoSeqPointInfo* info, int il_offset, SeqPoint* seq_point);

/*
 * SeqPointData struct and functions
 * This is used to store/load/use sequence point from a file
 */

typedef struct {
	guint32 method_token;
	guint32 method_index;
	MonoSeqPointInfo* seq_points;
	gboolean free_seq_points;
} SeqPointDataEntry;

typedef struct {
	SeqPointDataEntry* entries;
	int entry_count;
	int entry_capacity;
} SeqPointData;

void
mono_seq_point_data_init (SeqPointData *data, int entry_capacity);

void
mono_seq_point_data_free (SeqPointData *data);

gboolean
mono_seq_point_data_read (SeqPointData *data, char *path);

gboolean
mono_seq_point_data_write (SeqPointData *data, char *path);

void
mono_seq_point_data_add (SeqPointData *data, guint32 methodToken, guint32 methodIndex, MonoSeqPointInfo* info);

gboolean
mono_seq_point_data_get (SeqPointData *data, guint32 methodToken, guint32 methodIndex, MonoSeqPointInfo** info);

gboolean
mono_seq_point_data_get_il_offset (char *path, guint32 methodToken, guint32 methodIndex, guint32 native_offset, guint32 *il_offset);

#endif /* __MONO_SEQ_POINTS_DATA_H__ */
