#include <glib.h>
#include <string.h>

#include "monobitset.h"

#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif

#define BITS_PER_CHUNK (8 * sizeof (guint32))

struct MonoBitSet {
	guint32 size;
	guint32 flags;
	guint32 data [MONO_ZERO_LEN_ARRAY];
};

/*
 * Return the number of bytes required to hold the bitset.
 * Useful to allocate it on the stack or with mempool.
 * Use with mono_bitset_mem_new ().
 */
guint32
mono_bitset_alloc_size (guint32 max_size, guint32 flags) {
	guint32 real_size = (max_size + BITS_PER_CHUNK - 1) / BITS_PER_CHUNK;

	return sizeof (MonoBitSet) + sizeof (guint32) * (real_size - MONO_ZERO_LEN_ARRAY);
}

MonoBitSet *
mono_bitset_new (guint32 max_size, guint32 flags) {
	guint32 real_size = (max_size + BITS_PER_CHUNK - 1) / BITS_PER_CHUNK;
	MonoBitSet *result;

	result = g_malloc0 (sizeof (MonoBitSet) + sizeof (guint32) * (real_size - MONO_ZERO_LEN_ARRAY));
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

#if 0
const static int 
bitstart_mask [] = {
	0xffffffff, 0xfffffffe, 0xfffffffc, 0xfffffff8,
	0xfffffff0, 0xffffffe0, 0xffffffc0, 0xffffff80,
	0xffffff00, 0xfffffe00, 0xfffffc00, 0xfffff800,
	0xfffff000, 0xffffe000, 0xffffc000, 0xffff8000,
	0xffff0000, 0xfffe0000, 0xfffc0000, 0xfff80000,
	0xfff00000, 0xffe00000, 0xffc00000, 0xff800000,
	0xff000000, 0xfe000000, 0xfc000000, 0xf8000000,
	0xf0000000, 0xe0000000, 0xc0000000, 0x80000000,
	0x00000000
};

#define my_g_bit_nth_lsf(m,n) (ffs((m) & bitstart_mask [(n)+1])-1)
#define my_g_bit_nth_lsf_nomask(m) (ffs((m))-1)

#else
static inline gint
my_g_bit_nth_lsf (gulong mask, gint nth_bit)
{
	do {
		nth_bit++;
		if (mask & (1 << (gulong) nth_bit))
			return nth_bit;
	} while (nth_bit < 31);
	return -1;
}
#define my_g_bit_nth_lsf_nomask(m) (my_g_bit_nth_lsf((m),-1))
#endif

int
mono_bitset_find_start   (MonoBitSet *set)
{
	int i;

	for (i = 0; i < set->size / BITS_PER_CHUNK; ++i) {
		if (set->data [i])
			return my_g_bit_nth_lsf_nomask (set->data [i]) + i * BITS_PER_CHUNK;
	}
	return -1;
}

int
mono_bitset_find_first (MonoBitSet *set, gint pos) {
	int j;
	int bit;
	int result, i;

	if (pos == -1) {
		j = 0;
		bit = -1;
	} else {
		j = pos / BITS_PER_CHUNK;
		bit = pos % BITS_PER_CHUNK;
		g_return_val_if_fail (pos < set->size, -1);
	}
	/*g_print ("find first from %d (j: %d, bit: %d)\n", pos, j, bit);*/

	if (set->data [j]) {
		result = my_g_bit_nth_lsf (set->data [j], bit);
		if (result != -1)
			return result + j * BITS_PER_CHUNK;
	}
	for (i = ++j; i < set->size / BITS_PER_CHUNK; ++i) {
		if (set->data [i])
			return my_g_bit_nth_lsf (set->data [i], -1) + i * BITS_PER_CHUNK;
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

	if (set->data [j]) {
		result = g_bit_nth_msf (set->data [j], bit);
		if (result != -1)
			return result + j * BITS_PER_CHUNK;
	}
	for (i = --j; i >= 0; --i) {
		if (set->data [i])
			return g_bit_nth_msf (set->data [i], -1) + i * BITS_PER_CHUNK;
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
	int i;

	g_return_if_fail (dest->size <= src->size);

	for (i = 0; i < dest->size / BITS_PER_CHUNK; ++i)
		dest->data [i] = src->data [i];
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
 
	for (i = 0; i < src->size / BITS_PER_CHUNK; ++i)
		if (src->data [i] != src1->data [i])
			return FALSE;
	return TRUE;
}

void
mono_bitset_foreach (MonoBitSet *set, MonoBitSetFunc func, gpointer data)
{
	int i, j;
	for (i = 0; i < set->size / BITS_PER_CHUNK; ++i) {
		if (set->data [i]) {
			for (j = 0; j < BITS_PER_CHUNK; ++j)
				if (set->data [i] & (1 << j))
					func (j + i * BITS_PER_CHUNK, data);
		}
	}
}

#ifdef TEST_BITSET

/*
 * Compile with: 
 * gcc -g -Wall -DTEST_BITSET -o monobitset monobitset.c `pkg-config --cflags --libs glib-2.0`
 */
int 
main() {
	MonoBitSet *set1, *set2, *set3, *set4;
	int error = 1;
	int count, i;

	set1 = mono_bitset_new (60, 0);
	set4 = mono_bitset_new (60, 0);

	if (mono_bitset_count (set1) != 0)
		return error;
	error++;
	
	mono_bitset_set (set1, 33);
	if (mono_bitset_count (set1) != 1)
		return error;
	error++;

	g_print("should be 33: %d\n", mono_bitset_find_first (set1, 0));
	
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

	/* test 10 */
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

	mono_bitset_set (set4, 0);
	mono_bitset_set (set4, 1);
	mono_bitset_set (set4, 10);
	if (mono_bitset_count (set4) != 3)
		return error;
	error++;

	count = 0;
	for (i = mono_bitset_find_first (set4, -1); i != -1; i = mono_bitset_find_first (set4, i)) {
		count ++;
		g_print ("count got: %d at %d\n", count, i);
	}
	if (count != 3)
		return error;
	error++;
	g_print ("count passed\n");

	if (mono_bitset_find_first (set4, -1) != 0)
		return error;
	error++;

	mono_bitset_set (set4, 31);
	if (mono_bitset_find_first (set4, 10) != 31)
		return error;
	error++;

	mono_bitset_free (set1);
	mono_bitset_free (set2);
	mono_bitset_free (set3);
	mono_bitset_free (set4);

	g_print ("total tests passed: %d\n", error - 1);
	
	return 0;
}

#endif

