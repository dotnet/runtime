/**
 * \file
 */

#ifndef __MONO_BITSET_H__
#define __MONO_BITSET_H__

#include <glib.h>
#include <mono/utils/mono-publib.h>

#define MONO_BITSET_BITS_PER_CHUNK (8 * sizeof (gsize))

typedef struct {
	gsize size;
	gsize flags;
	gsize data [MONO_ZERO_LEN_ARRAY];
} MonoBitSet;

typedef void (*MonoBitSetFunc) (guint idx, gpointer data);

enum {
	MONO_BITSET_DONT_FREE = 1
};

/* Fast access to bits which depends on the implementation of the bitset */
#define mono_bitset_test_fast(set,n) ((set)->data [(n)/MONO_BITSET_BITS_PER_CHUNK] & ((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)))
#define mono_bitset_set_fast(set,n) do { (set)->data [(n)/MONO_BITSET_BITS_PER_CHUNK] |= ((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)); } while (0)
#define mono_bitset_clear_fast(set,n) do { (set)->data [(n)/MONO_BITSET_BITS_PER_CHUNK] &= ~((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)); } while (0)
#define mono_bitset_get_fast(set,n) ((set)->data[(n)])

#define mono_bitset_copyto_fast(src,dest) do { memcpy (&(dest)->data, &(src)->data, (dest)->size / 8); } while (0)

#define MONO_BITSET_FOREACH(set,idx,/*stmt*/...) \
	do \
	{ \
		MonoBitSet *__set = (set); \
		for (int __i = 0; __i < __set->size / MONO_BITSET_BITS_PER_CHUNK; __i++) { \
			if (__set->data [__i]) { \
				for (int __j = 0; __j < MONO_BITSET_BITS_PER_CHUNK; __j++) { \
					if (__set->data [__i] & ((gsize) 1 << __j)) { \
						guint idx = __j + __i * MONO_BITSET_BITS_PER_CHUNK; \
						__VA_ARGS__; \
					} \
				} \
			} \
		} \
	} while (0)

#define mono_bitset_union_fast(dest,src) do { \
	MonoBitSet *__tmp_src = (src); \
	MonoBitSet *__tmp_dest = (dest); \
	size_t size = (__tmp_dest->size / MONO_BITSET_BITS_PER_CHUNK); \
	for (size_t __i = 0; __i < size; ++__i) \
		__tmp_dest->data [__i] |= __tmp_src->data [__i]; \
} while (0)

#define mono_bitset_sub_fast(dest,src) do { \
	MonoBitSet *__tmp_src = (src); \
	MonoBitSet *__tmp_dest = (dest); \
	size_t size = __tmp_dest->size / MONO_BITSET_BITS_PER_CHUNK; \
	for (size_t __i = 0; __i < size; ++__i) \
		__tmp_dest->data [__i] &= ~__tmp_src->data [__i]; \
} while (0)

/*
 * Interface documentation can be found in the c-file.
 * Interface documentation by Dennis Haney.
 */

MONO_API guint32 mono_bitset_alloc_size (guint32 max_size, guint32 flags);

MONO_API MonoBitSet* mono_bitset_new (guint32 max_size, guint32 flags);

MONO_API MonoBitSet* mono_bitset_mem_new (gpointer mem, guint32 max_size, guint32 flags);

MONO_API void mono_bitset_free (MonoBitSet *set);

MONO_API void mono_bitset_set (MonoBitSet *set, guint32 pos);

MONO_API void mono_bitset_set_all (MonoBitSet *set);

MONO_API int mono_bitset_test (const MonoBitSet *set, guint32 pos);

MONO_API gsize mono_bitset_test_bulk (const MonoBitSet *set, guint32 pos);

MONO_API void mono_bitset_clear (MonoBitSet *set, guint32 pos);

MONO_API void mono_bitset_clear_all (MonoBitSet *set);

MONO_API void mono_bitset_invert (MonoBitSet *set);

MONO_API guint32 mono_bitset_size (const MonoBitSet *set);

MONO_API guint32 mono_bitset_count (const MonoBitSet *set);

MONO_API void mono_bitset_low_high (const MonoBitSet *set, guint32 *low, guint32 *high);

MONO_API int mono_bitset_find_start (const MonoBitSet *set);

MONO_API int mono_bitset_find_first (const MonoBitSet *set, gint pos);

MONO_API int mono_bitset_find_last (const MonoBitSet *set, gint pos);

MONO_API int mono_bitset_find_first_unset (const MonoBitSet *set, gint pos);

MONO_API MonoBitSet* mono_bitset_clone (const MonoBitSet *set, guint32 new_size);

MONO_API void mono_bitset_copyto (const MonoBitSet *src, MonoBitSet *dest);

MONO_API void mono_bitset_union (MonoBitSet *dest, const MonoBitSet *src);

MONO_API void mono_bitset_intersection (MonoBitSet *dest, const MonoBitSet *src);

MONO_API void mono_bitset_sub (MonoBitSet *dest, const MonoBitSet *src);

MONO_API gboolean mono_bitset_equal (const MonoBitSet *src, const MonoBitSet *src1);

MONO_API void mono_bitset_foreach (MonoBitSet *set, MonoBitSetFunc func, gpointer data);

MONO_API void mono_bitset_intersection_2 (MonoBitSet *dest, const MonoBitSet *src1, const MonoBitSet *src2);

gboolean mono_bitset_test_safe (const MonoBitSet *set, guint32 pos);

#endif /* __MONO_BITSET_H__ */
