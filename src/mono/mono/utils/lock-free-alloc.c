/*
 * lock-free-alloc.c: Lock free allocator.
 *
 * (C) Copyright 2011 Novell, Inc
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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

#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-mmap.h>
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
	unsigned int max_count;
	gpointer sb;
#ifndef DESC_AVAIL_DUMMY
	Descriptor * volatile next;
#endif
	gboolean in_use;	/* used for debugging only */
};

#define NUM_DESC_BATCH	64

#define SB_SIZE		16384
#define SB_HEADER_SIZE	16
#define SB_USABLE_SIZE	(SB_SIZE - SB_HEADER_SIZE)

#define SB_HEADER_FOR_ADDR(a)	((gpointer)((gulong)(a) & ~(gulong)(SB_SIZE-1)))
#define DESCRIPTOR_FOR_ADDR(a)	(*(Descriptor**)SB_HEADER_FOR_ADDR (a))

/* Taken from SGen */

static unsigned long
prot_flags_for_activate (int activate)
{
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;
	return prot_flags | MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
}

static void*
mono_sgen_alloc_os_memory (size_t size, int activate)
{
	return mono_valloc (0, size, prot_flags_for_activate (activate));
}

static void
mono_sgen_free_os_memory (void *addr, size_t size)
{
	mono_vfree (addr, size);
}

/* size must be a power of 2 */
static void*
mono_sgen_alloc_os_memory_aligned (size_t size, size_t alignment, gboolean activate)
{
	return mono_valloc_aligned (size, alignment, prot_flags_for_activate (activate));
}

static gpointer
alloc_sb (Descriptor *desc)
{
	gpointer sb_header = mono_sgen_alloc_os_memory_aligned (SB_SIZE, SB_SIZE, TRUE);
	g_assert (sb_header == SB_HEADER_FOR_ADDR (sb_header));
	DESCRIPTOR_FOR_ADDR (sb_header) = desc;
	//g_print ("sb %p for %p\n", sb_header, desc);
	return (char*)sb_header + SB_HEADER_SIZE;
}

static void
free_sb (gpointer sb)
{
	gpointer sb_header = SB_HEADER_FOR_ADDR (sb);
	g_assert ((char*)sb_header + SB_HEADER_SIZE == sb);
	mono_sgen_free_os_memory (sb_header, SB_SIZE);
	//g_print ("free sb %p\n", sb_header);
}

#ifndef DESC_AVAIL_DUMMY
static Descriptor * volatile desc_avail;

static Descriptor*
desc_alloc (void)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	Descriptor *desc;

	for (;;) {
		gboolean success;

		desc = get_hazardous_pointer ((gpointer * volatile)&desc_avail, hp, 1);
		if (desc) {
			Descriptor *next = desc->next;
			success = (InterlockedCompareExchangePointer ((gpointer * volatile)&desc_avail, next, desc) == desc);
		} else {
			size_t desc_size = sizeof (Descriptor);
			Descriptor *d;
			int i;

			desc = mono_sgen_alloc_os_memory (desc_size * NUM_DESC_BATCH, TRUE);

			/* Organize into linked list. */
			d = desc;
			for (i = 0; i < NUM_DESC_BATCH; ++i) {
				Descriptor *next = (i == (NUM_DESC_BATCH - 1)) ? NULL : (Descriptor*)((char*)desc + ((i + 1) * desc_size));
				d->next = next;
				mono_lock_free_queue_node_init (&d->node, TRUE);
				d = next;
			}

			mono_memory_write_barrier ();

			success = (InterlockedCompareExchangePointer ((gpointer * volatile)&desc_avail, desc->next, NULL) == NULL);

			if (!success)
				mono_sgen_free_os_memory (desc, desc_size * NUM_DESC_BATCH);
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
	Descriptor *desc = _desc;
	Descriptor *old_head;

	g_assert (desc->anchor.data.state == STATE_EMPTY);
	g_assert (!desc->in_use);

	do {
		old_head = desc_avail;
		desc->next = old_head;
		mono_memory_write_barrier ();
	} while (InterlockedCompareExchangePointer ((gpointer * volatile)&desc_avail, desc, old_head) != old_head);
}

static void
desc_retire (Descriptor *desc)
{
	g_assert (desc->anchor.data.state == STATE_EMPTY);
	g_assert (desc->in_use);
	desc->in_use = FALSE;
	free_sb (desc->sb);
	mono_thread_hazardous_free_or_queue (desc, desc_enqueue_avail);
}
#else
MonoLockFreeQueue available_descs;

static Descriptor*
desc_alloc (void)
{
	Descriptor *desc = (Descriptor*)mono_lock_free_queue_dequeue (&available_descs);

	if (desc)
		return desc;

	return calloc (1, sizeof (Descriptor));
}

static void
desc_retire (Descriptor *desc)
{
	free_sb (desc->sb);
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
	Descriptor *desc = _desc;

	g_assert (desc->anchor.data.state != STATE_FULL);

	mono_lock_free_queue_node_free (&desc->node);
	mono_lock_free_queue_enqueue (&desc->heap->sc->partial, &desc->node);
}

static void
list_put_partial (Descriptor *desc)
{
	g_assert (desc->anchor.data.state != STATE_FULL);
	mono_thread_hazardous_free_or_queue (desc, desc_put_partial);
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
			mono_thread_hazardous_free_or_queue (desc, desc_put_partial);
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

	return InterlockedCompareExchange (&desc->anchor.value, new_anchor.value, old_anchor.value) == old_anchor.value;
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
		if (InterlockedCompareExchangePointer ((gpointer * volatile)&heap->active, NULL, desc) != desc)
			goto retry;
	} else {
		desc = heap_get_partial (heap);
		if (!desc)
			return NULL;
	}

	/* Now we own the desc. */

	do {
		unsigned int next;

		new_anchor = old_anchor = *(volatile Anchor*)&desc->anchor.value;
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
		g_assert (next < SB_USABLE_SIZE / desc->slot_size);

		new_anchor.data.avail = next;
		--new_anchor.data.count;

		if (new_anchor.data.count == 0)
			new_anchor.data.state = STATE_FULL;
	} while (!set_anchor (desc, old_anchor, new_anchor));

	/* If the desc is partial we have to give it back. */
	if (new_anchor.data.state == STATE_PARTIAL) {
		if (InterlockedCompareExchangePointer ((gpointer * volatile)&heap->active, desc, NULL) != NULL)
			heap_put_partial (desc);
	}

	return addr;
}

static gpointer
alloc_from_new_sb (MonoLockFreeAllocator *heap)
{
	unsigned int slot_size, count, i;
	Descriptor *desc = desc_alloc ();

	desc->sb = alloc_sb (desc);

	slot_size = desc->slot_size = heap->sc->slot_size;
	count = SB_USABLE_SIZE / slot_size;

	/* Organize blocks into linked list. */
	for (i = 1; i < count - 1; ++i)
		*(unsigned int*)((char*)desc->sb + i * slot_size) = i + 1;

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

	mono_memory_write_barrier ();

	/* Make it active or free it again. */
	if (InterlockedCompareExchangePointer ((gpointer * volatile)&heap->active, desc, NULL) == NULL) {
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
mono_lock_free_free (gpointer ptr)
{
	Anchor old_anchor, new_anchor;
	Descriptor *desc;
	gpointer sb;
	MonoLockFreeAllocator *heap = NULL;

	desc = DESCRIPTOR_FOR_ADDR (ptr);
	sb = desc->sb;
	g_assert (SB_HEADER_FOR_ADDR (ptr) == SB_HEADER_FOR_ADDR (sb));

	do {
		new_anchor = old_anchor = *(volatile Anchor*)&desc->anchor.value;
		*(unsigned int*)ptr = old_anchor.data.avail;
		new_anchor.data.avail = ((char*)ptr - (char*)sb) / desc->slot_size;
		g_assert (new_anchor.data.avail < SB_USABLE_SIZE / desc->slot_size);

		if (old_anchor.data.state == STATE_FULL)
			new_anchor.data.state = STATE_PARTIAL;

		if (++new_anchor.data.count == desc->max_count) {
			heap = desc->heap;
			new_anchor.data.state = STATE_EMPTY;
		}
	} while (!set_anchor (desc, old_anchor, new_anchor));

	if (new_anchor.data.state == STATE_EMPTY) {
		g_assert (old_anchor.data.state != STATE_EMPTY);

		if (InterlockedCompareExchangePointer ((gpointer * volatile)&heap->active, NULL, desc) == desc) {
			/* We own it, so we free it. */
			desc_retire (desc);
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

		if (InterlockedCompareExchangePointer ((gpointer * volatile)&desc->heap->active, desc, NULL) != NULL)
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
	int max_count = SB_USABLE_SIZE / desc->slot_size;
#if _MSC_VER
	gboolean* linked = alloca(max_count*sizeof(gboolean));
#else
	gboolean linked [max_count];
#endif
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
mono_lock_free_allocator_init_size_class (MonoLockFreeAllocSizeClass *sc, unsigned int slot_size)
{
	g_assert (slot_size <= SB_USABLE_SIZE / 2);

	mono_lock_free_queue_init (&sc->partial);
	sc->slot_size = slot_size;
}

void
mono_lock_free_allocator_init_allocator (MonoLockFreeAllocator *heap, MonoLockFreeAllocSizeClass *sc)
{
	heap->sc = sc;
	heap->active = NULL;
}
