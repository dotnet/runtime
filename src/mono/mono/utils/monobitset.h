#ifndef __MONO_BITSET_H__
#define __MONO_BITSET_H__

#include <glib.h>

typedef struct MonoBitSet MonoBitSet;

enum {
	MONO_BITSET_DONT_FREE = 1
};

guint32     mono_bitset_alloc_size   (guint32 max_size, guint32 flags);

MonoBitSet* mono_bitset_new          (guint32 max_size, guint32 flags);

MonoBitSet* mono_bitset_mem_new      (gpointer mem, guint32 max_size, guint32 flags);

void        mono_bitset_free         (MonoBitSet *set); 

void        mono_bitset_set          (MonoBitSet *set, guint32 pos);

int         mono_bitset_test         (MonoBitSet *set, guint32 pos);

void        mono_bitset_clear        (MonoBitSet *set, guint32 pos);

void        mono_bitset_clear_all    (MonoBitSet *set);

void        mono_bitset_invert       (MonoBitSet *set);

guint32     mono_bitset_size         (MonoBitSet *set);

guint32     mono_bitset_count        (MonoBitSet *set);

/*
 * Find the first bit set _after_ (not including) pos.
 */
int         mono_bitset_find_first   (MonoBitSet *set, gint pos);

/*
 * Find the first bit set _before_ (not including) pos.
 * Use -1 to start from the end.
 */
int         mono_bitset_find_last    (MonoBitSet *set, gint pos);

MonoBitSet* mono_bitset_clone        (MonoBitSet *set, guint32 new_size);

void        mono_bitset_copyto       (MonoBitSet *set, MonoBitSet *dest);

void        mono_bitset_union        (MonoBitSet *dest, MonoBitSet *src);

void        mono_bitset_intersection (MonoBitSet *dest, MonoBitSet *src);

gboolean    mono_bitset_equal        (MonoBitSet *src, MonoBitSet *src1);

#endif /* __MONO_BITSET_H__ */
