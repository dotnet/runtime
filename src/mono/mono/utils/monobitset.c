#include <glib.h>
#include <string.h>

#include "monobitset.h"

#define BITS_PER_CHUNK (8 * sizeof (guint32))

struct MonoBitSet {
	guint32 size;
	guint32 flags;
	guint32 data [0];
};

/*
 * Return the number of bytes required to hold the bitset.
 * Useful to allocate it on the stack or with mempool.
 * Use with mono_bitset_mem_new ().
 */
guint32
mono_bitset_alloc_size (guint32 max_size, guint32 flags) {
	guint32 real_size = (max_size + BITS_PER_CHUNK - 1) / BITS_PER_CHUNK;

	return sizeof (MonoBitSet) + sizeof (guint32) * real_size;
}

MonoBitSet *
mono_bitset_new (guint32 max_size, guint32 flags) {
	guint32 real_size = (max_size + BITS_PER_CHUNK - 1) / BITS_PER_CHUNK;
	MonoBitSet *result;

	result = g_malloc0 (sizeof (MonoBitSet) + sizeof (guint32) * real_size);
	result->size = real_size * BITS_PER_CHUNK;
	result->flags = flags;
	return result;
}

/*
 * We could require mem_size here, instead of max_size, so we could do
 * some out of range checking...
 */
MonoBitSet *
mono_bitset_mem_new (gpointer mem, guint32 max_size, guint32 flags) {
	guint32 real_size = (max_size + BITS_PER_CHUNK - 1) / BITS_PER_CHUNK;
	MonoBitSet *result = mem;

	result->size = real_size * BITS_PER_CHUNK;
	result->flags = flags | MONO_BITSET_DONT_FREE;
	return result;
}

void
mono_bitset_free (MonoBitSet *set) {
	if (!(set->flags & MONO_BITSET_DONT_FREE))
		g_free (set);
}

void
mono_bitset_set (MonoBitSet *set, guint32 pos) {
	int j = pos / BITS_PER_CHUNK;
	int bit = pos % BITS_PER_CHUNK;

	g_return_if_fail (pos < set->size);

	set->data [j] |= 1 << bit;
}

int
mono_bitset_test (MonoBitSet *set, guint32 pos) {
	int j = pos / BITS_PER_CHUNK;
	int bit = pos % BITS_PER_CHUNK;

	g_return_val_if_fail (pos < set->size, 0);

	return set->data [j] & (1 << bit);
}

void
mono_bitset_clear (MonoBitSet *set, guint32 pos) {
	int j = pos / BITS_PER_CHUNK;
	int bit = pos % BITS_PER_CHUNK;

	g_return_if_fail (pos < set->size);

	set->data [j] &= ~(1 << bit);
}

void
mono_bitset_clear_all (MonoBitSet *set) {
	int i;
	for (i = 0; i < set->size / BITS_PER_CHUNK; ++i)
		set->data [i] = 0;
}

void
mono_bitset_invert (MonoBitSet *set) {
	int i;
	for (i = 0; i < set->size / BITS_PER_CHUNK; ++i)
		set->data [i] = ~set->data [i];
}

guint32
mono_bitset_size (MonoBitSet *set) {
	return set->size;
}

#if 1
/* 
 * should test wich version is faster.
 */
guint32
mono_bitset_count (MonoBitSet *set) {
	static const unsigned char table [16] = {
		0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4
	};
	guint32 i, count;
	const unsigned char *b;

	count = 0;
	for (i = 0; i < set->size / BITS_PER_CHUNK; ++i) {
		/* there is probably some asm code that can do this much faster */
		if (set->data [i]) {
			b = (unsigned char*) (set->data + i);
			count += table [b [0] & 0xf];
			count += table [b [0] >> 4];
			count += table [b [1] & 0xf];
			count += table [b [1] >> 4];
			count += table [b [2] & 0xf];
			count += table [b [2] >> 4];
			count += table [b [3] & 0xf];
			count += table [b [3] >> 4];
		}
	}
	return count;
}
#else
guint32
mono_bitset_count (MonoBitSet *set) {
	static const guint32 table [] = {
		0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF, 0x0000FFFF
	};
	guint32 i, count, val;

	count = 0;
	for (i = 0; i < set->size / BITS_PER_CHUNK;+i) {
		if (set->data [i]) {
			val = set->data [i];
			val = (val & table [0]) ((val >> 1) & table [0]);
			val = (val & table [1]) ((val >> 2) & table [1]);
			val = (val & table [2]) ((val >> 4) & table [2]);
			val = (val & table [3]) ((val >> 8) & table [3]);
			val = (val & table [4]) ((val >> 16) & table [4]);
			count= val;
		}
	}
	return count;
}

#endif

int
mono_bitset_find_first (MonoBitSet *set, gint pos) {
	int j = pos / BITS_PER_CHUNK;
	int bit = pos % BITS_PER_CHUNK;
	int result, i;

	g_return_val_if_fail (pos < set->size, -1);

	for (i = j; i < set->size / BITS_PER_CHUNK; ++i) {
		if (set->data [i]) {
			result = g_bit_nth_lsf (set->data [i], bit);
			if (result != -1)
				return result + i * BITS_PER_CHUNK;
		}
	}
	return -1;
}

int
mono_bitset_find_last (MonoBitSet *set, gint pos) {
	int j, bit, result, i;

	if (pos == -1)
		pos = set->size - 1;
		
	j = pos / BITS_PER_CHUNK;
	bit = pos % BITS_PER_CHUNK;

	g_return_val_if_fail (pos < set->size, -1);

	for (i = j; i >= 0; --i) {
		if (set->data [i]) {
			result = g_bit_nth_msf (set->data [i], bit);
			if (result != -1)
				return result + i * BITS_PER_CHUNK;
		}
	}
	return -1;
}

MonoBitSet*
mono_bitset_clone (MonoBitSet *set, guint32 new_size) {
	MonoBitSet *result;

	if (!new_size)
		new_size = set->size;
	result = mono_bitset_new (new_size, set->flags);
	result->flags &= ~MONO_BITSET_DONT_FREE;
	memcpy (result->data, set->data, result->size / 8);
	return result;
}

void
mono_bitset_copyto (MonoBitSet *src, MonoBitSet *dest) {

	g_return_if_fail (dest->size <= src->size);

	memcpy (dest->data, src->data, src->size / 8);
}

void
mono_bitset_union (MonoBitSet *dest, MonoBitSet *src) {
	int i;

	g_return_if_fail (src->size <= dest->size);

	for (i = 0; i < dest->size / BITS_PER_CHUNK; ++i)
		dest->data [i] |= src->data [i];
}

void
mono_bitset_intersection (MonoBitSet *dest, MonoBitSet *src) {
	int i;

	g_return_if_fail (src->size <= dest->size);

	for (i = 0; i < dest->size / BITS_PER_CHUNK; ++i)
		dest->data [i] = dest->data [i] & src->data [i];
}

void
mono_bitset_sub (MonoBitSet *dest, MonoBitSet *src) {
	int i;

	g_return_if_fail (src->size <= dest->size);

	for (i = 0; i < dest->size / BITS_PER_CHUNK; ++i)
		dest->data [i] &= ~src->data [i];
}

gboolean
mono_bitset_equal (MonoBitSet *src, MonoBitSet *src1) {
	int i;

	if (src->size != src1->size)
		return FALSE;
 
	return memcmp (src->data, src1->data, src->size / 8) == 0;
}

#ifdef TEST_BITSET

/*
 * Compile with: 
 * gcc -Wall -DTEST_BITSET -o monobitset monobitset.c `pkg-config --cflags --libs glib-2.0`
 */
int 
main() {
	MonoBitSet *set1, *set2, *set3;
	int error = 1;

	set1 = mono_bitset_new (60, 0);

	if (mono_bitset_count (set1) != 0)
		return error;
	error++;
	
	mono_bitset_set (set1, 33);
	if (mono_bitset_count (set1) != 1)
		return error;
	error++;

	if (mono_bitset_find_first (set1, 0) != 33)
		return error;
	error++;

	if (mono_bitset_find_first (set1, 33) != -1)
		return error;
	error++;

	if (mono_bitset_find_last (set1, -1) != 33)
		return error;
	error++;

	if (mono_bitset_find_last (set1, 33) != -1)
		return error;
	error++;

	if (mono_bitset_find_last (set1, 34) != 33)
		return error;
	error++;

	if (!mono_bitset_test (set1, 33))
		return error;
	error++;

	if (mono_bitset_test (set1, 32) || mono_bitset_test (set1, 34))
		return error;
	error++;

	set2 = mono_bitset_clone (set1, 0);
	if (mono_bitset_count (set2) != 1)
		return error;
	error++;

	mono_bitset_invert (set2);
	if (mono_bitset_count (set2) != (mono_bitset_size (set2) - 1))
		return error;
	error++;

	mono_bitset_clear (set2, 10);
	if (mono_bitset_count (set2) != (mono_bitset_size (set2) - 2))
		return error;
	error++;

	set3 = mono_bitset_clone (set2, 0);
	mono_bitset_union (set3, set1);
	if (mono_bitset_count (set3) != (mono_bitset_size (set3) - 1))
		return error;
	error++;

	mono_bitset_clear_all (set2);
	if (mono_bitset_count (set2) != 0)
		return error;
	error++;

	mono_bitset_invert (set2);
	if (mono_bitset_count (set2) != mono_bitset_size (set2))
		return error;
	error++;

	mono_bitset_free (set1);
	mono_bitset_free (set2);
	mono_bitset_free (set3);

	return 0;
}

#endif

