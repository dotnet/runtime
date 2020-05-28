/**
 * \file
 * A pointer array that doesn't use reallocs.
 *
 * Copyright (C) 2016 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGEN_ARRAY_LIST_H__
#define __MONO_SGEN_ARRAY_LIST_H__

#include <glib.h>

#define SGEN_ARRAY_LIST_BUCKETS (32)
#define SGEN_ARRAY_LIST_MIN_BUCKET_BITS (5)
#define SGEN_ARRAY_LIST_MIN_BUCKET_SIZE (1 << SGEN_ARRAY_LIST_MIN_BUCKET_BITS)

typedef void (*SgenArrayListBucketAllocCallback) (gpointer *bucket, guint32 new_bucket_size, gboolean alloc);
typedef gboolean (*SgenArrayListIsSlotSetFunc) (volatile gpointer *slot);
typedef gboolean (*SgenArrayListSetSlotFunc) (volatile gpointer *slot, gpointer ptr, int data);

/*
 * 'entries' is an array of pointers to buckets of increasing size. The first
 * bucket has size 'MIN_BUCKET_SIZE', and each bucket is twice the size of the
 * previous, i.e.:
 *
 *           |-------|-- MIN_BUCKET_SIZE
 *    [0] -> xxxxxxxx
 *    [1] -> xxxxxxxxxxxxxxxx
 *    [2] -> xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
 *    ...
 *
 * 'slot_hint' denotes the position of the last allocation, so that the
 * whole array needn't be searched on every allocation.
 *
 * The size of the spine, 'SGEN_ARRAY_LIST_BUCKETS', is chosen so
 * that the maximum number of entries is no less than G_MAXUINT32.
 */

typedef struct {
	volatile gpointer *volatile entries [SGEN_ARRAY_LIST_BUCKETS];
	volatile guint32 capacity;
	volatile guint32 slot_hint;
	volatile guint32 next_slot;
	SgenArrayListBucketAllocCallback bucket_alloc_callback;
	SgenArrayListIsSlotSetFunc is_slot_set_func;
	SgenArrayListSetSlotFunc set_slot_func;
	int mem_type; /* sgen internal mem type or -1 for malloc allocation */
} SgenArrayList;

#if defined(__GNUC__)
static inline guint32
sgen_clz (guint32 x)
{
	return __builtin_clz (x);
}
#elif !defined(ENABLE_MSVC_LZCNT) && defined(_MSC_VER)
static inline guint32
sgen_clz (guint32 x)
{
	gulong leading_zero_bits;
	return _BitScanReverse (&leading_zero_bits, (gulong)x) ? 31 - leading_zero_bits : 32;
}
#elif defined(ENABLE_MSVC_LZCNT) && defined(_MSC_VER)
static inline guint32
sgen_clz (guint32 x)
{
	return __lzcnt (x);
}
#else
static inline guint32
sgen_clz (guint32 x)
{
	guint count = 0;
	while (x) {
		++count;
		x >>= 1;
	}
	return 32 - count;
}
#endif

/*
 * Computes floor(log2(index + MIN_BUCKET_SIZE)) - 1, giving the index
 * of the bucket containing a slot.
 */
static inline guint32
sgen_array_list_index_bucket (guint32 index)
{
	return CHAR_BIT * sizeof (index) - sgen_clz (index + SGEN_ARRAY_LIST_MIN_BUCKET_SIZE) - 1 - SGEN_ARRAY_LIST_MIN_BUCKET_BITS;
}

static inline guint32
sgen_array_list_bucket_size (guint32 index)
{
	return 1 << (index + SGEN_ARRAY_LIST_MIN_BUCKET_BITS);
}

static inline void
sgen_array_list_bucketize (guint32 index, guint32 *bucket, guint32 *offset)
{
	*bucket = sgen_array_list_index_bucket (index);
	*offset = index - sgen_array_list_bucket_size (*bucket) + SGEN_ARRAY_LIST_MIN_BUCKET_SIZE;
}

static inline volatile gpointer *
sgen_array_list_get_slot (SgenArrayList *array, guint32 index)
{
	guint32 bucket, offset;

	SGEN_ASSERT (0, index < array->capacity, "Why are we accessing an entry that is not allocated");

	sgen_array_list_bucketize (index, &bucket, &offset);
	return &(array->entries [bucket] [offset]);
}

#define SGEN_ARRAY_LIST_INIT(bucket_alloc_callback, is_slot_set_func, set_slot_func, mem_type)	{ { NULL }, 0, 0, 0, (bucket_alloc_callback), (is_slot_set_func), (set_slot_func), (mem_type) }

#define SGEN_ARRAY_LIST_FOREACH_SLOT(array, slot) {			\
	guint32 __bucket, __offset;					\
	const guint32 __max_bucket = sgen_array_list_index_bucket ((array)->capacity); \
	guint32 __index = 0;						\
	const guint32 __next_slot = (array)->next_slot;			\
	for (__bucket = 0; __bucket < __max_bucket; ++__bucket) {	\
		volatile gpointer *__entries = (array)->entries [__bucket]; \
		for (__offset = 0; __offset < sgen_array_list_bucket_size (__bucket); ++__offset, ++__index) { \
			if (__index >= __next_slot)			\
				break;					\
			slot = &__entries [__offset];

#define SGEN_ARRAY_LIST_END_FOREACH_SLOT	} } }

#define SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE(array, begin, end, slot, index) {	\
	for (index = (begin); index < (end); index++) {		\
		guint32 __bucket, __offset;				\
		volatile gpointer *__entries;				\
		sgen_array_list_bucketize (index, &__bucket, &__offset); \
		__entries = (array)->entries [__bucket];		\
		slot = &__entries [__offset];

#define SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE	} }

guint32 sgen_array_list_alloc_block (SgenArrayList *array, guint32 slots_to_add);
guint32 sgen_array_list_add (SgenArrayList *array, gpointer ptr, int data, gboolean increase_size_before_set);
guint32 sgen_array_list_find (SgenArrayList *array, gpointer ptr);
gboolean sgen_array_list_default_cas_setter (volatile gpointer *slot, gpointer ptr, int data);
gboolean sgen_array_list_default_is_slot_set (volatile gpointer *slot);
void sgen_array_list_remove_nulls (SgenArrayList *array);

#endif
