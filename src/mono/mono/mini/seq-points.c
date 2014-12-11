/*
 * seq-points.c: Sequence Points functions
 *
 * Authors:
 *   Marcos Henrich (marcos.henrich@xamarin.com)
 *
 * Copyright 2014 Xamarin, Inc (http://www.xamarin.com)
 */

#include "mini.h"
#include "seq-points.h"

typedef struct {
	guint8 *data;
	int len;
	/* When has_debug_data is set to false only il and native deltas are saved */
	gboolean has_debug_data;
	/* When alloc_data is set to true data allocation/deallocation is managed by this structure */
	gboolean alloc_data;
} SeqPointInfoInflated;

static int
encode_var_int (guint8 *buf, guint8 **out_buf, int val)
{
	guint8 size = 0;

	do {
		guint8 byte = val & 0x7f;
		g_assert (size < 4 && "value has more than 28 bits");
		val >>= 7;
		if(val) byte |= 0x80;
		*(buf++) = byte;
		size++;
	} while (val);

	if (out_buf)
		*out_buf = buf;

	return size;
}

static int
decode_var_int (guint8* buf, guint8 **out_buf)
{
	guint8* p = buf;

	int low;
	int b;
	b = *(p++); low   = (b & 0x7f)      ; if(!(b & 0x80)) goto done;
	b = *(p++); low  |= (b & 0x7f) <<  7; if(!(b & 0x80)) goto done;
	b = *(p++); low  |= (b & 0x7f) << 14; if(!(b & 0x80)) goto done;
	b = *(p++); low  |= (b & 0x7f) << 21; if(!(b & 0x80)) goto done;

	g_assert (FALSE && "value has more than 28 bits");

done:

	if (out_buf)
		*out_buf = p;

	return low;
}

static guint32
encode_zig_zag (int val)
{
	return (val << 1) ^ (val >> 31);
}

static int
decode_zig_zag (guint32 val)
{
	int n = val;
	return (n >> 1) ^ (-(n & 1));
}

static SeqPointInfoInflated
seq_point_info_inflate (MonoSeqPointInfo *info)
{
	SeqPointInfoInflated info_inflated;
	guint8 *ptr = (guint8*) info;
	int value;

	value = decode_var_int (ptr, &ptr);

	info_inflated.len = value >> 2;
	info_inflated.has_debug_data = (value & 1) != 0;
	info_inflated.alloc_data = (value & 2) != 0;

	if (info_inflated.alloc_data)
		info_inflated.data = ptr;
	else
		memcpy (&info_inflated.data, ptr, sizeof (guint8*));

	return info_inflated;
}

static MonoSeqPointInfo*
seq_point_info_new (int len, gboolean alloc_data, guint8 *data, gboolean has_debug_data)
{
	MonoSeqPointInfo *info;
	guint8 *info_ptr;
	guint8 buffer[4];
	int buffer_len;
	int value;
	int data_size;

	value = len << 2;
	if (has_debug_data)
		value |= 1;
	if (alloc_data)
		value |= 2;

	buffer_len = encode_var_int (buffer, NULL, value);

	data_size = buffer_len + (alloc_data? len : sizeof (guint8*));
	info_ptr = g_new0 (guint8, data_size);
	info = (MonoSeqPointInfo*) info_ptr;

	memcpy (info_ptr, buffer, buffer_len);
	info_ptr += buffer_len;

	if (alloc_data)
		memcpy (info_ptr, data, len);
	else
		memcpy (info_ptr, &data, sizeof (guint8*));

	mono_jit_stats.allocated_seq_points_size += data_size;

	return info;
}

void
seq_point_info_free (gpointer ptr)
{
	MonoSeqPointInfo* info = (MonoSeqPointInfo*) ptr;
	g_free (info);
}

static int
seq_point_read (SeqPoint* seq_point, guint8* ptr, guint8* buffer_ptr, gboolean has_debug_data)
{
	int value, i;
	guint8* ptr0 = ptr;

	value = decode_var_int (ptr, &ptr);
	seq_point->il_offset += decode_zig_zag (value);

	value = decode_var_int (ptr, &ptr);
	seq_point->native_offset += decode_zig_zag (value);

	if (has_debug_data) {
		value = decode_var_int (ptr, &ptr);
		seq_point->flags = value;

		if (seq_point->flags & MONO_SEQ_POINT_FLAG_EXIT_IL)
			seq_point->il_offset = METHOD_EXIT_IL_OFFSET;

		value = decode_var_int (ptr, &ptr);
		seq_point->next_len = value;

		if (seq_point->next_len) {
			// store next offset and skip it
			seq_point->next_offset = ptr - buffer_ptr;
			for (i = 0; i < seq_point->next_len; ++i)
				decode_var_int (ptr, &ptr);
		}
	}

	return ptr - ptr0;
}

static gboolean
seq_point_info_add_seq_point (GByteArray* array, SeqPoint *sp, SeqPoint *last_seq_point, GSList *next, gboolean has_debug_data)
{
	int il_delta, native_delta;
	GSList *l;
	guint8 buffer[4];
	guint8 len;
	int flags;

	if (!has_debug_data &&
		(sp->il_offset == METHOD_ENTRY_IL_OFFSET || sp->il_offset == METHOD_EXIT_IL_OFFSET))
		return FALSE;

	il_delta = sp->il_offset - last_seq_point->il_offset;
	native_delta = sp->native_offset - last_seq_point->native_offset;

	flags = sp->flags;

	if (has_debug_data && sp->il_offset == METHOD_EXIT_IL_OFFSET) {
		il_delta = 0;
		flags |= MONO_SEQ_POINT_FLAG_EXIT_IL;
	}

	len = encode_var_int (buffer, NULL, encode_zig_zag (il_delta));
	g_byte_array_append (array, buffer, len);

	len = encode_var_int (buffer, NULL, encode_zig_zag (native_delta));
	g_byte_array_append (array, buffer, len);

	if (has_debug_data) {
		sp->next_offset = array->len;
		sp->next_len = g_slist_length (next);

		len = encode_var_int (buffer, NULL, flags);
		g_byte_array_append (array, buffer, len);

		len = encode_var_int (buffer, NULL, sp->next_len);
		g_byte_array_append (array, buffer, len);

		for (l = next; l; l = l->next) {
			int next_index = GPOINTER_TO_UINT (l->data);
			guint8 buffer[4];
			int len = encode_var_int (buffer, NULL, next_index);
			g_byte_array_append (array, buffer, len);
		}
	}

	return TRUE;
}

static void
collect_pred_seq_points (MonoBasicBlock *bb, MonoInst *ins, GSList **next, int depth)
{
	int i;
	MonoBasicBlock *in_bb;
	GSList *l;

	for (i = 0; i < bb->in_count; ++i) {
		in_bb = bb->in_bb [i];

		if (in_bb->last_seq_point) {
			int src_index = in_bb->last_seq_point->backend.size;
			int dst_index = ins->backend.size;

			/* bb->in_bb might contain duplicates */
			for (l = next [src_index]; l; l = l->next)
				if (GPOINTER_TO_UINT (l->data) == dst_index)
					break;
			if (!l)
				next [src_index] = g_slist_append (next [src_index], GUINT_TO_POINTER (dst_index));
		} else {
			/* Have to look at its predecessors */
			if (depth < 5)
				collect_pred_seq_points (in_bb, ins, next, depth + 1);
		}
	}
}

void
mono_save_seq_point_info (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	GSList *bb_seq_points, *l;
	MonoInst *last;
	MonoDomain *domain = cfg->domain;
	int i;
	GSList **next = NULL;
	SeqPoint* seq_points;
	GByteArray* array;
	gboolean has_debug_data = cfg->gen_seq_points_debug_data;

	if (!cfg->seq_points)
		return;

	seq_points = g_new0 (SeqPoint, cfg->seq_points->len);

	for (i = 0; i < cfg->seq_points->len; ++i) {
		SeqPoint *sp = &seq_points [i];
		MonoInst *ins = g_ptr_array_index (cfg->seq_points, i);

		sp->il_offset = ins->inst_imm;
		sp->native_offset = ins->inst_offset;
		if (ins->flags & MONO_INST_NONEMPTY_STACK)
			sp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;

		/* Used below */
		ins->backend.size = i;
	}

	if (has_debug_data) {
		/*
		 * For each sequence point, compute the list of sequence points immediately
		 * following it, this is needed to implement 'step over' in the debugger agent.
		 */
		next = g_new0 (GSList*, cfg->seq_points->len);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb_seq_points = g_slist_reverse (bb->seq_points);
			last = NULL;
			for (l = bb_seq_points; l; l = l->next) {
				MonoInst *ins = l->data;

				if (ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET)
				/* Used to implement method entry/exit events */
					continue;
				if (ins->inst_offset == SEQ_POINT_NATIVE_OFFSET_DEAD_CODE)
					continue;

				if (last != NULL) {
					/* Link with the previous seq point in the same bb */
					next [last->backend.size] = g_slist_append (next [last->backend.size], GUINT_TO_POINTER (ins->backend.size));
				} else {
					/* Link with the last bb in the previous bblocks */
					collect_pred_seq_points (bb, ins, next, 0);
				}

				last = ins;
			}

			if (bb->last_ins && bb->last_ins->opcode == OP_ENDFINALLY && bb->seq_points) {
				MonoBasicBlock *bb2;
				MonoInst *endfinally_seq_point = NULL;

				/*
				 * The ENDFINALLY branches are not represented in the cfg, so link it with all seq points starting bbs.
				 */
				l = g_slist_last (bb->seq_points);
				if (l) {
					endfinally_seq_point = l->data;

					for (bb2 = cfg->bb_entry; bb2; bb2 = bb2->next_bb) {
						GSList *l = g_slist_last (bb2->seq_points);

						if (l) {
							MonoInst *ins = l->data;

							if (!(ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET) && ins != endfinally_seq_point)
								next [endfinally_seq_point->backend.size] = g_slist_append (next [endfinally_seq_point->backend.size], GUINT_TO_POINTER (ins->backend.size));
						}
					}
				}
			}
		}

		if (cfg->verbose_level > 2) {
			printf ("\nSEQ POINT MAP: \n");

			for (i = 0; i < cfg->seq_points->len; ++i) {
				SeqPoint *sp = &seq_points [i];
				GSList *l;

				if (!next [i])
					continue;

				printf ("\tIL0x%x ->", sp->il_offset);
				for (l = next [i]; l; l = l->next) {
					int next_index = GPOINTER_TO_UINT (l->data);
					printf (" IL0x%x", seq_points [next_index].il_offset);
				}
				printf ("\n");
			}
		}
	}

	array = g_byte_array_new ();

	{ /* Add sequence points to seq_point_info */
		SeqPoint zero_seq_point = {0};
		SeqPoint* last_seq_point = &zero_seq_point;

		for (i = 0; i < cfg->seq_points->len; ++i) {
			SeqPoint *sp = &seq_points [i];
			GSList* next_list = NULL;

			if (has_debug_data)
				next_list = next[i];

			if (seq_point_info_add_seq_point (array, sp, last_seq_point, next_list, has_debug_data))
				last_seq_point = sp;

			if (has_debug_data)
				g_slist_free (next [i]);
		}
	}

	if (has_debug_data)
		g_free (next);

	cfg->seq_point_info = seq_point_info_new (array->len, TRUE, array->data, has_debug_data);

	g_byte_array_free (array, TRUE);

	// FIXME: dynamic methods
	if (!cfg->compile_aot) {
		mono_domain_lock (domain);
		// FIXME: How can the lookup succeed ?
		if (!g_hash_table_lookup (domain_jit_info (domain)->seq_points, cfg->method_to_register))
			g_hash_table_insert (domain_jit_info (domain)->seq_points, cfg->method_to_register, cfg->seq_point_info);
		mono_domain_unlock (domain);
	}

	g_ptr_array_free (cfg->seq_points, TRUE);
	cfg->seq_points = NULL;
}

MonoSeqPointInfo*
get_seq_points (MonoDomain *domain, MonoMethod *method)
{
	MonoSeqPointInfo *seq_points;

	mono_domain_lock (domain);
	seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, method);
	if (!seq_points && method->is_inflated) {
		/* generic sharing + aot */
		seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, mono_method_get_declaring_generic_method (method));
		if (!seq_points)
			seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, mini_get_shared_method (method));
	}
	mono_domain_unlock (domain);

	return seq_points;
}

static gboolean
seq_point_find_next_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point)
{
	SeqPointIterator it;
	seq_point_iterator_init (&it, info);
	while (seq_point_iterator_next (&it)) {
		if (it.seq_point.native_offset >= native_offset) {
			memcpy (seq_point, &it.seq_point, sizeof (SeqPoint));
			return TRUE;
		}
	}

	return FALSE;
}

static gboolean
seq_point_find_prev_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point)
{
	SeqPoint prev_seq_point;
	gboolean  is_first = TRUE;
	SeqPointIterator it;
	seq_point_iterator_init (&it, info);
	while (seq_point_iterator_next (&it) && it.seq_point.native_offset <= native_offset) {
		memcpy (&prev_seq_point, &it.seq_point, sizeof (SeqPoint));
		is_first = FALSE;
	}

	if (!is_first && prev_seq_point.native_offset <= native_offset) {
		memcpy (seq_point, &prev_seq_point, sizeof (SeqPoint));
		return TRUE;
	}

	return FALSE;
}

static gboolean
seq_point_find_by_il_offset (MonoSeqPointInfo* info, int il_offset, SeqPoint* seq_point)
{
	SeqPointIterator it;
	seq_point_iterator_init (&it, info);
	while (seq_point_iterator_next (&it)) {
		if (it.seq_point.il_offset == il_offset) {
			memcpy (seq_point, &it.seq_point, sizeof (SeqPoint));
			return TRUE;
		}
	}

	return FALSE;
}

/*
 * find_next_seq_point_for_native_offset:
 *
 *   Find the first sequence point after NATIVE_OFFSET.
 */
gboolean
find_next_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = get_seq_points (domain, method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return seq_point_find_next_by_native_offset (seq_points, native_offset, seq_point);
}

/*
 * find_prev_seq_point_for_native_offset:
 *
 *   Find the first sequence point before NATIVE_OFFSET.
 */
gboolean
find_prev_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = get_seq_points (domain, method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return seq_point_find_prev_by_native_offset (seq_points, native_offset, seq_point);
}

/*
 * find_seq_point:
 *
 *   Find the sequence point corresponding to the IL offset IL_OFFSET, which
 * should be the location of a sequence point.
 */
gboolean
find_seq_point (MonoDomain *domain, MonoMethod *method, gint32 il_offset, MonoSeqPointInfo **info, SeqPoint *seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = get_seq_points (domain, method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return seq_point_find_by_il_offset (seq_points, il_offset, seq_point);
}

void
seq_point_init_next (MonoSeqPointInfo* info, SeqPoint sp, SeqPoint* next)
{
	int i;
	guint8* ptr;
	SeqPointIterator it;
	GArray* seq_points = g_array_new (FALSE, TRUE, sizeof (SeqPoint));
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	g_assert (info_inflated.has_debug_data);

	seq_point_iterator_init (&it, info);
	while (seq_point_iterator_next (&it))
		g_array_append_vals (seq_points, &it.seq_point, 1);

	ptr = info_inflated.data + sp.next_offset;
	for (i = 0; i < sp.next_len; i++) {
		int next_index;
		next_index = decode_var_int (ptr, &ptr);
		g_assert (next_index < seq_points->len);
		memcpy (&next[i], seq_points->data + next_index * sizeof (SeqPoint), sizeof (SeqPoint));
	}

	g_array_free (seq_points, TRUE);
}

gboolean
seq_point_iterator_next (SeqPointIterator* it)
{
	if (it->ptr >= it->end)
		return FALSE;

	it->ptr += seq_point_read (&it->seq_point, it->ptr, it->begin, it->has_debug_data);

	return TRUE;
}

void
seq_point_iterator_init (SeqPointIterator* it, MonoSeqPointInfo* info)
{
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);
	it->ptr = info_inflated.data;
	it->begin = info_inflated.data;
	it->end = it->begin + info_inflated.len;
	it->has_debug_data = info_inflated.has_debug_data;
	memset(&it->seq_point, 0, sizeof(SeqPoint));
}

int
seq_point_info_write (MonoSeqPointInfo* info, guint8* buffer)
{
	guint8* buffer0 = buffer;
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	memcpy (buffer, &info_inflated.has_debug_data, 1);
	buffer++;

	//Write sequence points
	encode_var_int (buffer, &buffer, info_inflated.len);
	memcpy (buffer, info_inflated.data, info_inflated.len);
	buffer += info_inflated.len;

	return buffer - buffer0;
}

int
seq_point_info_read (MonoSeqPointInfo** info, guint8* buffer, gboolean copy)
{
	guint8* buffer0 = buffer;
	int size;
	gboolean has_debug_data;

	memcpy (&has_debug_data, buffer, 1);
	buffer++;

	size = decode_var_int (buffer, &buffer);
	(*info) = seq_point_info_new (size, copy, buffer, has_debug_data);
	buffer += size;

	return buffer - buffer0;
}

/*
 * Returns the maximum size of mono_seq_point_info_write.
 */
int
seq_point_info_get_write_size (MonoSeqPointInfo* info)
{
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	//4 is the maximum size required to store the size of the data.
	//1 is the byte used to store has_debug_data.
	int size = 4 + 1 + info_inflated.len;

	return size;
}
