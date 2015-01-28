/*
 * Copyright 2014 Xamarin Inc
 */
 
#ifndef __MONO_SEQ_POINTS_H__
#define __MONO_SEQ_POINTS_H__

#define MONO_SEQ_POINT_FLAG_NONEMPTY_STACK 1
#define MONO_SEQ_POINT_FLAG_EXIT_IL 2

/* IL offsets used to mark the sequence points belonging to method entry/exit events */
#define METHOD_ENTRY_IL_OFFSET -1
#define METHOD_EXIT_IL_OFFSET 0xffffff

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
	int dummy[0];
} MonoSeqPointInfo;

typedef struct {
	SeqPoint seq_point;
	guint8* ptr;
	guint8* begin;
	guint8* end;
	gboolean has_debug_data;
} SeqPointIterator;

void
seq_point_info_free (gpointer info);

void
mono_save_seq_point_info (MonoCompile *cfg);

MonoSeqPointInfo*
get_seq_points (MonoDomain *domain, MonoMethod *method);

gboolean
find_next_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point);

gboolean
find_prev_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point);

gboolean
find_seq_point (MonoDomain *domain, MonoMethod *method, gint32 il_offset, MonoSeqPointInfo **info, SeqPoint *seq_point);

gboolean
seq_point_iterator_next (SeqPointIterator* it);

void
seq_point_iterator_init (SeqPointIterator* it, MonoSeqPointInfo* info);

void
seq_point_init_next (MonoSeqPointInfo* info, SeqPoint sp, SeqPoint* next);

int
seq_point_info_write (MonoSeqPointInfo* info, guint8* buffer);

int
seq_point_info_read (MonoSeqPointInfo** info, guint8* buffer, gboolean copy);

int
seq_point_info_get_write_size (MonoSeqPointInfo* info);

void
bb_deduplicate_op_il_seq_points (MonoCompile *cfg, MonoBasicBlock *bb);

#endif /* __MONO_SEQ_POINTS_H__ */