/*
 * lock-free-alloc.c: Lock free allocator.
 *
 * (C) Copyright 2011 Novell, Inc
 */

#include <glib.h>
#include <stdlib.h>

#include <mono/io-layer/atomic.h>
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
	Anchor anchor;
	unsigned int slot_size;
	unsigned int max_count;
	gpointer sb;
#ifndef DESC_AVAIL_DUMMY
	Descriptor *next;
#endif
	gboolean in_use;
};

#define NUM_DESC_BATCH	64

#define SB_SIZE		16384
#define SB_HEADER_SIZE	16
#define SB_USABLE_SIZE	(SB_SIZE - SB_HEADER_SIZE)

#define SB_HEADER_FOR_ADDR(a)	((gpointer)((gulong)(a) & ~(gulong)(SB_SIZE-1)))
#define DESCRIPTOR_FOR_ADDR(a)	(*(Descriptor**)SB_HEADER_FOR_ADDR (a))

/* Taken from SGen */

static void*
mono_sgen_alloc_os_memory (size_t size, int activate)
{
	void *ptr;
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;

	prot_flags |= MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
	ptr = mono_valloc (0, size, prot_flags);
	return ptr;
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
	/* Allocate twice the memory to be able to put the block on an aligned address */
	char *mem = mono_sgen_alloc_os_memory (size + alignment, activate);
	char *aligned;

	g_assert (mem);

	aligned = (char*)((gulong)(mem + (alignment - 1)) & ~(alignment - 1));
	g_assert (aligned >= mem && aligned + size <= mem + size + alignment && !((gulong)aligned & (alignment - 1)));

	if (aligned > mem)
		mono_sgen_free_os_memory (mem, aligned - mem);
	if (aligned + size < mem + size + alignment)
		mono_sgen_free_os_memory (aligned + size, (mem + size + alignment) - (aligned + size));

	return aligned;
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

		new_anchor = old_anchor = (Anchor)*(volatile gint32*)&desc->anchor.value;
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
		new_anchor = old_anchor = (Anchor)*(volatile gint32*)&desc->anchor.value;
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
	gboolean linked [max_count];
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
