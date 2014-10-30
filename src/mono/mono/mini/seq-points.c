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

static guint8
encode_var_int (guint8* buf, int val)
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

	return size;
}

static guint8
decode_var_int (guint8* buf, int* val)
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

	*val = low;
	return p-buf;
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

static int
seq_point_read (SeqPoint* seq_point, guint8* ptr)
{
	int value;
	guint8* ptr0 = ptr;

	ptr += decode_var_int (ptr, &value);
	seq_point->il_offset += decode_zig_zag (value);

	ptr += decode_var_int (ptr, &value);
	seq_point->native_offset += decode_zig_zag (value);

	ptr += decode_var_int (ptr, &value);
	seq_point->flags = value;

	ptr += decode_var_int (ptr, &value);
	seq_point->next_len = value;

	if (seq_point->next_len) {
		ptr += decode_var_int (ptr, &value);
		seq_point->next_offset = value;
	}

	return ptr - ptr0;
}

static MonoSeqPointInfo*
seq_point_info_new (gboolean alloc_arrays)
{
	MonoSeqPointInfo* info = g_new0 (MonoSeqPointInfo, 1);
	info->alloc_arrays = alloc_arrays;
	if (alloc_arrays) {
		info->array = g_byte_array_new ();
		info->next_array = g_byte_array_new ();
	} else {
		info->array = g_new0 (GByteArray, 1);
		info->next_array = g_new0 (GByteArray, 1);
	}
	return info;
}

void
seq_point_info_free (gpointer ptr)
{
	MonoSeqPointInfo* info = (MonoSeqPointInfo*) ptr;

	if (info->alloc_arrays) {
		g_byte_array_free (info->array, TRUE);
		g_byte_array_free (info->next_array, TRUE);
	} else {
		g_free (info->array);
		g_free (info->next_array);
	}
	g_free (info);
}

static gboolean
seq_point_info_add_seq_point (MonoSeqPointInfo *info, SeqPoint *sp, SeqPoint *last_seq_point, GSList *next)
{
	int il_delta, native_delta;
	GSList *l;
	guint8 buffer[4];
	guint8 len;

	/* check that data can be added to the arrays */
	g_assert (info->alloc_arrays);

	// TODO use flag instead of encoding 4 bytes for METHOD_EXIT_IL_OFFSET

	sp->next_offset = info->next_array->len;
	sp->next_len = g_slist_length (next);

	il_delta = sp->il_offset - last_seq_point->il_offset;
	native_delta = sp->native_offset - last_seq_point->native_offset;

	len = encode_var_int (buffer, encode_zig_zag (il_delta));
	g_byte_array_append (info->array, buffer, len);

	len = encode_var_int (buffer, encode_zig_zag (native_delta));
	g_byte_array_append (info->array, buffer, len);

	len = encode_var_int (buffer, sp->flags);
	g_byte_array_append (info->array, buffer, len);

	len = encode_var_int (buffer, sp->next_len);
	g_byte_array_append (info->array, buffer, len);

	if (sp->next_len) {
		len = encode_var_int (buffer, sp->next_offset);
		g_byte_array_append (info->array, buffer, len);
	}

	for (l = next; l; l = l->next) {
		int next_index = GPOINTER_TO_UINT (l->data);
		guint8 buffer[4];
		int len = encode_var_int (buffer, next_index);
		g_byte_array_append (info->next_array, buffer, len);
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
	GSList **next;
	SeqPoint* seq_points;

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

	cfg->seq_point_info = seq_point_info_new (TRUE);

	{ /* Add sequence points to seq_point_info */
		SeqPoint zero_seq_point = {0};
		SeqPoint* last_seq_point = &zero_seq_point;

		for (i = 0; i < cfg->seq_points->len; ++i) {
			SeqPoint *sp = &seq_points [i];

			if (seq_point_info_add_seq_point (cfg->seq_point_info, sp, last_seq_point, next[i]))
				last_seq_point = sp;

			g_slist_free (next [i]);
		}
	}

	g_free (next);

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

	seq_point_iterator_init (&it, info);
	while (seq_point_iterator_next (&it))
		g_array_append_vals (seq_points, &it.seq_point, 1);

	ptr = info->next_array->data + sp.next_offset;
	for (i = 0; i < sp.next_len; i++) {
		int next_index;
		ptr += decode_var_int (ptr, &next_index);
		g_assert (next_index < seq_points->len);
		memcpy (&next[i], seq_points->data + next_index * sizeof (SeqPoint), sizeof (SeqPoint));
	}
}

gboolean
seq_point_iterator_next (SeqPointIterator* it)
{
	if (it->ptr >= it->info->array->data + it->info->array->len)
		return FALSE;

	it->ptr += seq_point_read (&it->seq_point, it->ptr);

	return TRUE;
}

void
seq_point_iterator_init (SeqPointIterator* it, MonoSeqPointInfo* info)
{
	it->info = info;
	it->ptr = it->info->array->data;
	memset(&it->seq_point, 0, sizeof(SeqPoint));
}

int
seq_point_info_write (MonoSeqPointInfo* info, guint8* buffer)
{
	guint8* buffer0 = buffer;

	//Write sequence points
	buffer += encode_var_int (buffer, info->array->len);
	memcpy (buffer, info->array->data, info->array->len);
	buffer += info->array->len;

	//Write next values
	buffer += encode_var_int (buffer, info->next_array->len);
	memcpy (buffer, info->next_array->data, info->next_array->len);
	buffer += info->next_array->len;

	return buffer - buffer0;
}

int
seq_point_info_read (MonoSeqPointInfo** info, guint8* buffer, gboolean copy)
{
	guint8* buffer0 = buffer;
	int size;

	(*info) = seq_point_info_new (copy);

	buffer += decode_var_int (buffer, &size);
	if (copy)
		g_byte_array_append ((*info)->array, buffer, size);
	else {
		(*info)->array->data = buffer;
		(*info)->array->len = size;
	}
	buffer += size;

	buffer += decode_var_int (buffer, &size);
	if (copy)
		g_byte_array_append ((*info)->array, buffer, size);
	else {
		(*info)->array->data = buffer;
		(*info)->array->len = size;
	}
	buffer += size;

	return buffer - buffer0;
}

/*
 * Returns the maximum size of mono_seq_point_info_write.
 */
int
seq_point_info_write_size (MonoSeqPointInfo* info)
{
	//8 is the maximum size required to store the size of the arrays.
	return 8 + info->array->len + info->next_array->len;
}
