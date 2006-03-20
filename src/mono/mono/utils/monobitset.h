#ifndef __MONO_BITSET_H__
#define __MONO_BITSET_H__

#include <glib.h>

typedef struct MonoBitSet MonoBitSet;
typedef void (*MonoBitSetFunc) (guint idx, gpointer data);

enum {
	MONO_BITSET_DONT_FREE = 1
};

#define MONO_BITSET_BITS_PER_CHUNK (8 * sizeof (gsize))

/* Fast access to bits which depends on the implementation of the bitset */
#define mono_bitset_test_fast(set,n) (((gsize*)set)[2+(n)/MONO_BITSET_BITS_PER_CHUNK] & ((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)))
#define mono_bitset_set_fast(set,n) do { ((gsize*)set)[2+(n)/MONO_BITSET_BITS_PER_CHUNK] |= ((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)); } while (0)
#define mono_bitset_clear_fast(set,n) do { ((gsize*)set)[2+(n)/MONO_BITSET_BITS_PER_CHUNK] &= ~((gsize)1 << ((n) % MONO_BITSET_BITS_PER_CHUNK)); } while (0)
#define mono_bitset_get_fast(set,n) (((gsize*)set)[2+(n)])

/*
 * Interface documentation can be found in the c-file.
 * Interface documentation by Dennis Haney.
 */

guint32     mono_bitset_alloc_size   (guint32 max_size, guint32 flags);

MonoBitSet* mono_bitset_new          (guint32 max_size, guint32 flags);

MonoBitSet* mono_bitset_mem_new      (gpointer mem, guint32 max_size, guint32 flags);

void        mono_bitset_free         (MonoBitSet *set); 

void        mono_bitset_set          (MonoBitSet *set, guint32 pos);

void        mono_bitset_set_all      (MonoBitSet *set);

int         mono_bitset_test         (const MonoBitSet *set, guint32 pos);

gsize       mono_bitset_test_bulk    (const MonoBitSet *set, guint32 pos);

void        mono_bitset_clear        (MonoBitSet *set, guint32 pos);

void        mono_bitset_clear_all    (MonoBitSet *set);

void        mono_bitset_invert       (MonoBitSet *set);

guint32     mono_bitset_size         (const MonoBitSet *set);

guint32     mono_bitset_count        (const MonoBitSet *set);

void        mono_bitset_low_high     (const MonoBitSet *set, guint32 *low, guint32 *high);

int         mono_bitset_find_start   (const MonoBitSet *set);

int         mono_bitset_find_first   (const MonoBitSet *set, gint pos);

int         mono_bitset_find_last    (const MonoBitSet *set, gint pos);

int         mono_bitset_find_first_unset (const MonoBitSet *set, gint pos);

MonoBitSet* mono_bitset_clone        (const MonoBitSet *set, guint32 new_size);

void        mono_bitset_copyto       (const MonoBitSet *src, MonoBitSet *dest);

void        mono_bitset_union        (MonoBitSet *dest, const MonoBitSet *src);

void        mono_bitset_intersection (MonoBitSet *dest, const MonoBitSet *src);

void        mono_bitset_sub          (MonoBitSet *dest, const MonoBitSet *src);

gboolean    mono_bitset_equal        (const MonoBitSet *src, const MonoBitSet *src1);

void        mono_bitset_foreach      (MonoBitSet *set, MonoBitSetFunc func, gpointer data);

void        mono_bitset_intersection_2 (MonoBitSet *dest, const MonoBitSet *src1, const MonoBitSet *src2);

#endif /* __MONO_BITSET_H__ */
