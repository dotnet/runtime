/**
 * \file
 * Lock free allocator.
 *
 * (C) Copyright 2011 Novell, Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * This is a simplified version of the lock-free allocator described in
 *
 * Scalable Lock-Free Dynamic Memory Allocation
 * Maged M. Michael, PLDI 2004
 *
 * I could not get Michael's allocator working bug free under heavy
 * stress tests.  The paper doesn't provide correctness proof and after
 * failing to formalize the ownership of descriptors I devised this
 * simpler allocator.
 *
 * Allocation within superblocks proceeds exactly like in Michael's
 * allocator.  The simplification is that a thread has to "acquire" a
 * descriptor before it can allocate from its superblock.  While it owns
 * the descriptor no other thread can acquire and hence allocate from
 * it.  A consequence of this is that the ABA problem cannot occur, so
 * we don't need the tag field and don't have to use 64 bit CAS.
 *
 * Descriptors are stored in two locations: The partial queue and the
 * active field.  They can only be in at most one of those at one time.
 * If a thread wants to allocate, it needs to get a descriptor.  It
 * tries the active descriptor first, CASing it to NULL.  If that
 * doesn't work, it gets a descriptor out of the partial queue.  Once it
 * has the descriptor it owns it because it is not referenced anymore.
 * It allocates a slot and then gives the descriptor back (unless it is
 * FULL).
 *
 * Note that it is still possible that a slot is freed while an
 * allocation is in progress from the same superblock.  Ownership in
 * this case is not complicated, though.  If the block was FULL and the
 * free set it to PARTIAL, the free now owns the block (because FULL
 * blocks are not referenced from partial and active) and has to give it
 * back.  If the block was PARTIAL then the free doesn't own the block
 * (because it's either still referenced, or an alloc owns it).  A
 * special case of this is that it has changed from PARTIAL to EMPTY and
 * now needs to be retired.  Technically, the free wouldn't have to do
 * anything in this case because the first thing an alloc does when it
 * gets ownership of a descriptor is to check whether it is EMPTY and
 * retire it if that is the case.  As an optimization, our free does try
 * to acquire the descriptor (by CASing the active field, which, if it
 * is lucky, points to that descriptor) and if it can do so, retire it.
 * If it can't, it tries to retire other descriptors from the partial
 * queue, so that we can be sure that even if no more allocations
 * happen, descriptors are still retired.  This is analogous to what
 * Michael's allocator does.
 *
 * Another difference to Michael's allocator is not related to
 * concurrency, however: We don't point from slots to descriptors.
 * Instead we allocate superblocks aligned and point from the start of
 * the superblock to the descriptor, so we only need one word of
 * metadata per superblock.
 *
 * FIXME: Having more than one allocator per size class is probably
 * buggy because it was never tested.
 */

#include <glib.h>
#include <stdlib.h>

#include <mono/utils/atomic.h>
#ifdef SGEN_WITHOUT_MONO
#include <mono/sgen/sgen-gc.h>
#include <mono/sgen/sgen-client.h>
#else
#include <mono/utils/mono-mmap.h>
#endif
#include <mono/utils/mono-membar.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/lock-free-queue.h>

#include <mono/utils/lock-free-alloc.h>

//#define DESC_AVAIL_DUMMY

enum {
	STATE_FULL,
	STATE_PARTIAL,
	STATE_EMPTY
};

typedef union {
	gint32 value;
	struct {
		guint32 avail : 15;
		guint32 count : 15;
		guint32 state : 2;
	} data;
} Anchor;

typedef struct _MonoLockFreeAllocDescriptor Descriptor;
struct _MonoLockFreeAllocDescriptor {
	MonoLockFreeQueueNode node;
	MonoLockFreeAllocator *heap;
	volatile Anchor anchor;
	unsigned int slot_size;
	unsigned int block_size;
	unsigned int max_count;
	gpointer sb;
#ifndef DESC_AVAIL_DUMMY
	Descriptor * volatile next;
#endif
	gboolean in_use;	/* used for debugging only */
};

#define NUM_DESC_BATCH	64

static MONO_ALWAYS_INLINE gpointer
sb_header_for_addr (gpointer addr, size_t block_size)
{
	return (gpointer)(((size_t)addr) & (~(block_size - 1)));
}

/* Taken from SGen */

static unsigned long
prot_flags_for_activate (int activate)
{
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;
	return prot_flags | MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
}

static gpointer
alloc_sb (Descriptor *desc)
{
	static int pagesize = -1;

	gpointer sb_header;

	if (pagesize == -1)
		pagesize = mono_pagesize ();

	sb_header = desc->block_size == pagesize ?
		mono_valloc (NULL, desc->block_size, prot_flags_for_activate (TRUE), desc->heap->account_type) :
		mono_valloc_aligned (desc->block_size, desc->block_size, prot_flags_for_activate (TRUE), desc->heap->account_type);

	g_assertf (sb_header, "Failed to allocate memory for the lock free allocator");
	g_assert (sb_header == sb_header_for_addr (sb_header, desc->block_size));

	*(Descriptor**)sb_header = desc;
	//g_print ("sb %p for %p\n", sb_header, desc);

	return (char*)sb_header + LOCK_FREE_ALLOC_SB_HEADER_SIZE;
}

static void
free_sb (gpointer sb, size_t block_size, MonoMemAccountType type)
{
	gpointer sb_header = sb_header_for_addr (sb, block_size);
	g_assert ((char*)sb_header + LOCK_FREE_ALLOC_SB_HEADER_SIZE == sb);
	mono_vfree (sb_header, block_size, type);
	//g_print ("free sb %p\n", sb_header);
}

#ifndef DESC_AVAIL_DUMMY
static Descriptor * volatile desc_avail;

static Descriptor*
desc_alloc (MonoMemAccountType type)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	Descriptor *desc;

	for (;;) {
		gboolean success;

		desc = (Descriptor *) mono_get_hazardous_pointer ((volatile gpointer *)&desc_avail, hp, 1);
		if (desc) {
			Descriptor *next = desc->next;
			success = (mono_atomic_cas_ptr ((volatile gpointer *)&desc_avail, next, desc) == desc);
		} else {
			size_t desc_size = sizeof (Descriptor);
			Descriptor *d;
			int i;

			desc = (Descriptor *) mono_valloc (NULL, desc_size * NUM_DESC_BATCH, prot_flags_for_activate (TRUE), type);
			g_assertf (desc, "Failed to allocate memory for the lock free allocator");

			/* Organize into linked list. */
			d = desc;
			for (i = 0; i < NUM_DESC_BATCH; ++i) {
				Descriptor *next = (i == (NUM_DESC_BATCH - 1)) ? NULL : (Descriptor*)((char*)desc + ((i + 1) * desc_size));
				d->next = next;
				mono_lock_free_queue_node_init (&d->node, TRUE);
				d = next;
			}

			mono_memory_write_barrier ();

			success = (mono_atomic_cas_ptr ((volatile gpointer *)&desc_avail, desc->next, NULL) == NULL);

			if (!success)
				mono_vfree (desc, desc_size * NUM_DESC_BATCH, type);
		}

		mono_hazard_pointer_clear (hp, 1);

		if (success)
			break;
	}

	g_assert (!desc->in_use);
	desc->in_use = TRUE;

	return desc;
}

static void
desc_enqueue_avail (gpointer _desc)
{
	Descriptor *desc = (Descriptor *) _desc;
	Descriptor *old_head;

	g_assert (desc->anchor.data.state == STATE_EMPTY);
	g_assert (!desc->in_use);

	do {
		old_head = desc_avail;
		desc->next = old_head;
		mono_memory_write_barrier ();
	} while (mono_atomic_cas_ptr ((volatile gpointer *)&desc_avail, desc, old_head) != old_head);
}

static void
desc_retire (Descriptor *desc)
{
	g_assert (desc->anchor.data.state == STATE_EMPTY);
	g_assert (desc->in_use);
	desc->in_use = FALSE;
	free_sb (desc->sb, desc->block_size, desc->heap->account_type);
	mono_thread_hazardous_try_free (desc, desc_enqueue_avail);
}
#else
MonoLockFreeQueue available_descs;

static Descriptor*
desc_alloc (MonoMemAccountType type)
{
	Descriptor *desc = (Descriptor*)mono_lock_free_queue_dequeue (&available_descs);

	if (desc)
		return desc;

	return g_calloc (1, sizeof (Descriptor));
}

static void
desc_retire (Descriptor *desc)
{
	free_sb (desc->sb, desc->block_size, desc->heap->account_type);
	mono_lock_free_queue_enqueue (&available_descs, &desc->node);
}
#endif

static Descriptor*
list_get_partial (MonoLockFreeAllocSizeClass *sc)
{
	for (;;) {
		Descriptor *desc = (Descriptor*) mono_lock_free_queue_dequeue (&sc->partial);
		if (!desc)
			return NULL;
		if (desc->anchor.data.state != STATE_EMPTY)
			return desc;
		desc_retire (desc);
	}
}

static void
desc_put_partial (gpointer _desc)
{
	Descriptor *desc = (Descriptor *) _desc;

	g_assert (desc->anchor.data.state != STATE_FULL);

	mono_lock_free_queue_node_unpoison (&desc->node);
	mono_lock_free_queue_enqueue (&desc->heap->sc->partial, &desc->node);
}

static void
list_put_partial (Descriptor *desc)
{
	g_assert (desc->anchor.data.state != STATE_FULL);
	mono_thread_hazardous_try_free (desc, desc_put_partial);
}

static void
list_remove_empty_desc (MonoLockFreeAllocSizeClass *sc)
{
	int num_non_empty = 0;
	for (;;) {
		Descriptor *desc = (Descriptor*) mono_lock_free_queue_dequeue (&sc->partial);
		if (!desc)
			return;
		/*
		 * We don't need to read atomically because we're the
		 * only thread that references this descriptor.
		 */
		if (desc->anchor.data.state == STATE_EMPTY) {
			desc_retire (desc);
		} else {
			g_assert (desc->heap->sc == sc);
			mono_thread_hazardous_try_free (desc, desc_put_partial);
			if (++num_non_empty >= 2)
				return;
		}
	}
}

static Descriptor*
heap_get_partial (MonoLockFreeAllocator *heap)
{
	return list_get_partial (heap->sc);
}

static void
heap_put_partial (Descriptor *desc)
{
	list_put_partial (desc);
}

static gboolean
set_anchor (Descriptor *desc, Anchor old_anchor, Anchor new_anchor)
{
	if (old_anchor.data.state == STATE_EMPTY)
		g_assert (new_anchor.data.state == STATE_EMPTY);

	return mono_atomic_cas_i32 (&desc->anchor.value, new_anchor.value, old_anchor.value) == old_anchor.value;
}

static gpointer
alloc_from_active_or_partial (MonoLockFreeAllocator *heap)
{
	Descriptor *desc;
	Anchor old_anchor, new_anchor;
	gpointer addr;

 retry:
	desc = heap->active;
	if (desc) {
		if (mono_atomic_cas_ptr ((volatile gpointer *)&heap->active, NULL, desc) != desc)
			goto retry;
	} else {
		desc = heap_get_partial (heap);
		if (!desc)
			return NULL;
	}

	/* Now we own the desc. */

	do {
		unsigned int next;
		new_anchor.value = old_anchor.value = ((volatile Anchor*)&desc->anchor)->value;
		if (old_anchor.data.state == STATE_EMPTY) {
			/* We must free it because we own it. */
			desc_retire (desc);
			goto retry;
		}
		g_assert (old_anchor.data.state == STATE_PARTIAL);
		g_assert (old_anchor.data.count > 0);

		addr = (char*)desc->sb + old_anchor.data.avail * desc->slot_size;

		mono_memory_read_barrier ();

		next = *(unsigned int*)addr;
		g_assert (next < LOCK_FREE_ALLOC_SB_USABLE_SIZE (desc->block_size) / desc->slot_size);

		new_anchor.data.avail = next;
		--new_anchor.data.count;

		if (new_anchor.data.count == 0)
			new_anchor.data.state = STATE_FULL;
	} while (!set_anchor (desc, old_anchor, new_anchor));

	/* If the desc is partial we have to give it back. */
	if (new_anchor.data.state == STATE_PARTIAL) {
		if (mono_atomic_cas_ptr ((volatile gpointer *)&heap->active, desc, NULL) != NULL)
			heap_put_partial (desc);
	}

	return addr;
}

static gpointer
alloc_from_new_sb (MonoLockFreeAllocator *heap)
{
	unsigned int slot_size, block_size, count, i;
	Descriptor *desc = desc_alloc (heap->account_type);

	slot_size = desc->slot_size = heap->sc->slot_size;
	block_size = desc->block_size = heap->sc->block_size;
	count = LOCK_FREE_ALLOC_SB_USABLE_SIZE (block_size) / slot_size;

	desc->heap = heap;
	/*
	 * Setting avail to 1 because 0 is the block we're allocating
	 * right away.
	 */
	desc->anchor.data.avail = 1;
	desc->slot_size = heap->sc->slot_size;
	desc->max_count = count;

	desc->anchor.data.count = desc->max_count - 1;
	desc->anchor.data.state = STATE_PARTIAL;

	desc->sb = alloc_sb (desc);

	/* Organize blocks into linked list. */
	for (i = 1; i < count - 1; ++i)
		*(unsigned int*)((char*)desc->sb + i * slot_size) = i + 1;

	*(unsigned int*)((char*)desc->sb + (count - 1) * slot_size) = 0;

	mono_memory_write_barrier ();

	/* Make it active or free it again. */
	if (mono_atomic_cas_ptr ((volatile gpointer *)&heap->active, desc, NULL) == NULL) {
		return desc->sb;
	} else {
		desc->anchor.data.state = STATE_EMPTY;
		desc_retire (desc);
		return NULL;
	}
}

gpointer
mono_lock_free_alloc (MonoLockFreeAllocator *heap)
{
	gpointer addr;

	for (;;) {

		addr = alloc_from_active_or_partial (heap);
		if (addr)
			break;

		addr = alloc_from_new_sb (heap);
		if (addr)
			break;
	}

	return addr;
}

void
mono_lock_free_free (gpointer ptr, size_t block_size)
{
	Anchor old_anchor, new_anchor;
	Descriptor *desc;
	gpointer sb;
	MonoLockFreeAllocator *heap = NULL;

	desc = *(Descriptor**) sb_header_for_addr (ptr, block_size);
	g_assert (block_size == desc->block_size);

	sb = desc->sb;

	do {
		new_anchor.value = old_anchor.value = ((volatile Anchor*)&desc->anchor)->value;
		*(unsigned int*)ptr = old_anchor.data.avail;
		new_anchor.data.avail = GPTRDIFF_TO_UINT (((char*)ptr - (char*)sb) / desc->slot_size);
		g_assert (new_anchor.data.avail < LOCK_FREE_ALLOC_SB_USABLE_SIZE (block_size) / desc->slot_size);

		if (old_anchor.data.state == STATE_FULL)
			new_anchor.data.state = STATE_PARTIAL;

		if (++new_anchor.data.count == desc->max_count) {
			heap = desc->heap;
			new_anchor.data.state = STATE_EMPTY;
		}
	} while (!set_anchor (desc, old_anchor, new_anchor));

	if (new_anchor.data.state == STATE_EMPTY) {
		g_assert (old_anchor.data.state != STATE_EMPTY);

		if (mono_atomic_cas_ptr ((volatile gpointer *)&heap->active, NULL, desc) == desc) {
			/*
			 * We own desc, check if it's still empty, in which case we retire it.
			 * If it's partial we need to put it back either on the active slot or
			 * on the partial list.
			 */
			if (desc->anchor.data.state == STATE_EMPTY) {
				desc_retire (desc);
			} else if (desc->anchor.data.state == STATE_PARTIAL) {
				if (mono_atomic_cas_ptr ((volatile gpointer *)&heap->active, desc, NULL) != NULL)
					heap_put_partial (desc);

			}
		} else {
			/*
			 * Somebody else must free it, so we do some
			 * freeing for others.
			 */
			list_remove_empty_desc (heap->sc);
		}
	} else if (old_anchor.data.state == STATE_FULL) {
		/*
		 * Nobody owned it, now we do, so we need to give it
		 * back.
		 */

		g_assert (new_anchor.data.state == STATE_PARTIAL);

		if (mono_atomic_cas_ptr ((volatile gpointer *)&desc->heap->active, desc, NULL) != NULL)
			heap_put_partial (desc);
	}
}

#define g_assert_OR_PRINT(c, format, ...)	do {				\
		if (!(c)) {						\
			if (print)					\
				g_print ((format), ## __VA_ARGS__);	\
			else						\
				g_assert (FALSE);				\
		}							\
	} while (0)

static void
descriptor_check_consistency (Descriptor *desc, gboolean print)
{
	int count = desc->anchor.data.count;
	int max_count = LOCK_FREE_ALLOC_SB_USABLE_SIZE (desc->block_size) / desc->slot_size;
	gboolean* linked = g_newa (gboolean, max_count);
	int i, last;
	unsigned int index;

#ifndef DESC_AVAIL_DUMMY
	Descriptor *avail;

	for (avail = desc_avail; avail; avail = avail->next)
		g_assert_OR_PRINT (desc != avail, "descriptor is in the available list\n");
#endif

	g_assert_OR_PRINT (desc->slot_size == desc->heap->sc->slot_size, "slot size doesn't match size class\n");

	if (print)
		g_print ("descriptor %p is ", desc);

	switch (desc->anchor.data.state) {
	case STATE_FULL:
		if (print)
			g_print ("full\n");
		g_assert_OR_PRINT (count == 0, "count is not zero: %d\n", count);
		break;
	case STATE_PARTIAL:
		if (print)
			g_print ("partial\n");
		g_assert_OR_PRINT (count < max_count, "count too high: is %d but must be below %d\n", count, max_count);
		break;
	case STATE_EMPTY:
		if (print)
			g_print ("empty\n");
		g_assert_OR_PRINT (count == max_count, "count is wrong: is %d but should be %d\n", count, max_count);
		break;
	default:
		g_assert_OR_PRINT (FALSE, "invalid state\n");
	}

	for (i = 0; i < max_count; ++i)
		linked [i] = FALSE;

	index = desc->anchor.data.avail;
	last = -1;
	for (i = 0; i < count; ++i) {
		gpointer addr = (char*)desc->sb + index * desc->slot_size;
		g_assert_OR_PRINT (index >= 0 && index < max_count,
				"index %d for %dth available slot, linked from %d, not in range [0 .. %d)\n",
				index, i, last, max_count);
		g_assert_OR_PRINT (!linked [index], "%dth available slot %d linked twice\n", i, index);
		if (linked [index])
			break;
		linked [index] = TRUE;
		last = index;
		index = *(unsigned int*)addr;
	}
}

gboolean
mono_lock_free_allocator_check_consistency (MonoLockFreeAllocator *heap)
{
	Descriptor *active = heap->active;
	Descriptor *desc;
	if (active) {
		g_assert (active->anchor.data.state == STATE_PARTIAL);
		descriptor_check_consistency (active, FALSE);
	}
	while ((desc = (Descriptor*)mono_lock_free_queue_dequeue (&heap->sc->partial))) {
		g_assert (desc->anchor.data.state == STATE_PARTIAL || desc->anchor.data.state == STATE_EMPTY);
		descriptor_check_consistency (desc, FALSE);
	}
	return TRUE;
}

void
mono_lock_free_allocator_init_size_class (MonoLockFreeAllocSizeClass *sc, unsigned int slot_size, unsigned int block_size)
{
	g_assert (block_size > 0);
	g_assert ((block_size & (block_size - 1)) == 0); /* check if power of 2 */
	g_assert (slot_size * 2 <= LOCK_FREE_ALLOC_SB_USABLE_SIZE (block_size));

	mono_lock_free_queue_init (&sc->partial);
	sc->slot_size = slot_size;
	sc->block_size = block_size;
}

void
mono_lock_free_allocator_init_allocator (MonoLockFreeAllocator *heap, MonoLockFreeAllocSizeClass *sc, MonoMemAccountType account_type)
{
	heap->sc = sc;
	heap->active = NULL;
	heap->account_type = account_type;
}
