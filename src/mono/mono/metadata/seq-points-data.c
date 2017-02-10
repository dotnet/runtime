/**
 * \file
 * Sequence Points functions
 *
 * Authors:
 *   Marcos Henrich (marcos.henrich@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 */

#include "seq-points-data.h"

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

MonoSeqPointInfo*
mono_seq_point_info_new (int len, gboolean alloc_data, guint8 *data, gboolean has_debug_data, int *out_size)
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

	*out_size = data_size = buffer_len + (alloc_data? len : sizeof (guint8*));
	info_ptr = g_new0 (guint8, data_size);
	info = (MonoSeqPointInfo*) info_ptr;

	memcpy (info_ptr, buffer, buffer_len);
	info_ptr += buffer_len;

	if (alloc_data)
		memcpy (info_ptr, data, len);
	else
		memcpy (info_ptr, &data, sizeof (guint8*));

	return info;
}

void
mono_seq_point_info_free (gpointer ptr)
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

gboolean
mono_seq_point_info_add_seq_point (GByteArray* array, SeqPoint *sp, SeqPoint *last_seq_point, GSList *next, gboolean has_debug_data)
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

gboolean
mono_seq_point_find_next_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point)
{
	SeqPointIterator it;
	mono_seq_point_iterator_init (&it, info);
	while (mono_seq_point_iterator_next (&it)) {
		if (it.seq_point.native_offset >= native_offset) {
			memcpy (seq_point, &it.seq_point, sizeof (SeqPoint));
			return TRUE;
		}
	}

	return FALSE;
}

gboolean
mono_seq_point_find_prev_by_native_offset (MonoSeqPointInfo* info, int native_offset, SeqPoint* seq_point)
{
	SeqPoint prev_seq_point;
	gboolean  is_first = TRUE;
	SeqPointIterator it;
	mono_seq_point_iterator_init (&it, info);
	while (mono_seq_point_iterator_next (&it) && it.seq_point.native_offset <= native_offset) {
		memcpy (&prev_seq_point, &it.seq_point, sizeof (SeqPoint));
		is_first = FALSE;
	}

	if (!is_first && prev_seq_point.native_offset <= native_offset) {
		memcpy (seq_point, &prev_seq_point, sizeof (SeqPoint));
		return TRUE;
	}

	return FALSE;
}

gboolean
mono_seq_point_find_by_il_offset (MonoSeqPointInfo* info, int il_offset, SeqPoint* seq_point)
{
	SeqPointIterator it;
	mono_seq_point_iterator_init (&it, info);
	while (mono_seq_point_iterator_next (&it)) {
		if (it.seq_point.il_offset == il_offset) {
			memcpy (seq_point, &it.seq_point, sizeof (SeqPoint));
			return TRUE;
		}
	}

	return FALSE;
}

void
mono_seq_point_init_next (MonoSeqPointInfo* info, SeqPoint sp, SeqPoint* next)
{
	int i;
	guint8* ptr;
	SeqPointIterator it;
	GArray* seq_points = g_array_new (FALSE, TRUE, sizeof (SeqPoint));
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	g_assert (info_inflated.has_debug_data);

	mono_seq_point_iterator_init (&it, info);
	while (mono_seq_point_iterator_next (&it))
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
mono_seq_point_iterator_next (SeqPointIterator* it)
{
	if (it->ptr >= it->end)
		return FALSE;

	it->ptr += seq_point_read (&it->seq_point, it->ptr, it->begin, it->has_debug_data);

	return TRUE;
}

void
mono_seq_point_iterator_init (SeqPointIterator* it, MonoSeqPointInfo* info)
{
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);
	it->ptr = info_inflated.data;
	it->begin = info_inflated.data;
	it->end = it->begin + info_inflated.len;
	it->has_debug_data = info_inflated.has_debug_data;
	memset(&it->seq_point, 0, sizeof(SeqPoint));
}

int
mono_seq_point_info_write (MonoSeqPointInfo* info, guint8* buffer)
{
	guint8* buffer0 = buffer;
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	encode_var_int (buffer, &buffer, info_inflated.has_debug_data);

	//Write sequence points
	encode_var_int (buffer, &buffer, info_inflated.len);
	memcpy (buffer, info_inflated.data, info_inflated.len);
	buffer += info_inflated.len;

	return buffer - buffer0;
}

int
mono_seq_point_info_read (MonoSeqPointInfo** info, guint8* buffer, gboolean copy)
{
	guint8* buffer0 = buffer;
	int size, info_size;
	gboolean has_debug_data;

	has_debug_data = decode_var_int (buffer, &buffer);

	size = decode_var_int (buffer, &buffer);
	(*info) = mono_seq_point_info_new (size, copy, buffer, has_debug_data, &info_size);
	buffer += size;

	return buffer - buffer0;
}

/*
 * Returns the maximum size of mono_seq_point_info_write.
 */
int
mono_seq_point_info_get_write_size (MonoSeqPointInfo* info)
{
	SeqPointInfoInflated info_inflated = seq_point_info_inflate (info);

	//4 is the maximum size required to store the size of the data.
	//1 is the byte used to store has_debug_data.
	int size = 4 + 1 + info_inflated.len;

	return size;
}

/*
 * SeqPointData struct and functions
 * This is used to store/load/use sequence point from a file
 */

void
mono_seq_point_data_init (SeqPointData *data, int entry_capacity)
{
	data->entry_count = 0;
	data->entry_capacity = entry_capacity;
	data->entries = (SeqPointDataEntry *)g_malloc (sizeof (SeqPointDataEntry) * entry_capacity);
}

void
mono_seq_point_data_free (SeqPointData *data)
{
	int i;
	for (i=0; i<data->entry_count; i++) {
		if (data->entries [i].free_seq_points)
			g_free (data->entries [i].seq_points);
	}
	g_free (data->entries);
}

gboolean
mono_seq_point_data_read (SeqPointData *data, char *path)
{
	guint8 *buffer, *buffer_orig;
	int entry_count, i;
	long fsize;
	FILE *f;

	f = fopen (path, "r");
	if (!f)
		return FALSE;

	fseek(f, 0, SEEK_END);
	fsize = ftell(f);
	fseek(f, 0, SEEK_SET);

	buffer_orig = buffer = (guint8 *)g_malloc (fsize + 1);
	fread(buffer_orig, fsize, 1, f);
	fclose(f);

	entry_count = decode_var_int (buffer, &buffer);
	mono_seq_point_data_init (data, entry_count);
	data->entry_count = entry_count;

	for (i=0; i<entry_count; i++) {
		data->entries [i].method_token = decode_var_int (buffer, &buffer);
		data->entries [i].method_index = decode_var_int (buffer, &buffer);
		buffer += mono_seq_point_info_read (&data->entries [i].seq_points, buffer, TRUE);
		data->entries [i].free_seq_points = TRUE;
	}

	g_free (buffer_orig);
	return TRUE;
}

gboolean
mono_seq_point_data_write (SeqPointData *data, char *path)
{
	guint8 *buffer, *buffer_orig;
	FILE *f;
	int i, size = 0;

	f = fopen (path, "w+");
	if (!f)
		return FALSE;

	for (i=0; i<data->entry_count; i++) {
		size += mono_seq_point_info_get_write_size (data->entries [i].seq_points);
	}
	// Add size of entry_count and native_base_offsets
	size += 4 + data->entry_count * 4;

	buffer_orig = buffer = (guint8 *)g_malloc (size);

	encode_var_int (buffer, &buffer, data->entry_count);

	for (i=0; i<data->entry_count; i++) {
		encode_var_int (buffer, &buffer, data->entries [i].method_token);
		encode_var_int (buffer, &buffer, data->entries [i].method_index);
		buffer += mono_seq_point_info_write (data->entries [i].seq_points, buffer);
	}

	fwrite (buffer_orig, 1, buffer - buffer_orig, f);
	g_free (buffer_orig);
	fclose (f);

	return TRUE;
}

void
mono_seq_point_data_add (SeqPointData *data, guint32 method_token, guint32 method_index, MonoSeqPointInfo* info)
{
	int i;

	g_assert (data->entry_count < data->entry_capacity);
	i = data->entry_count++;
	data->entries [i].seq_points = info;
	data->entries [i].method_token = method_token;
	data->entries [i].method_index = method_index;
	data->entries [i].free_seq_points = FALSE;
}

gboolean
mono_seq_point_data_get (SeqPointData *data, guint32 method_token, guint32 method_index, MonoSeqPointInfo** info)
{
	int i;

	for (i=0; i<data->entry_count; i++) {
		if (data->entries [i].method_token == method_token && (method_index == 0xffffff || data->entries [i].method_index == method_index)) {
			(*info) = data->entries [i].seq_points;
			return TRUE;
		}
	}
	return FALSE;
}

gboolean
mono_seq_point_data_get_il_offset (char *path, guint32 method_token, guint32 method_index, guint32 native_offset, guint32 *il_offset)
{
	SeqPointData sp_data;
	MonoSeqPointInfo *seq_points;
	SeqPoint sp;

	if (!mono_seq_point_data_read (&sp_data, path))
		return FALSE;

	if (!mono_seq_point_data_get (&sp_data, method_token, method_index, &seq_points))
		return FALSE;

	if (!mono_seq_point_find_prev_by_native_offset (seq_points, native_offset, &sp))
		return FALSE;

	*il_offset = sp.il_offset;

	return TRUE;
}
