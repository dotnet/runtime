/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
#define GRAY_QUEUE_LENGTH_LIMIT	64

static GrayQueue gray_queue;

static void
gray_object_alloc_queue_section (GrayQueue *queue)
{
	GrayQueueSection *section;

	if (queue->alloc_prepare_func)
		queue->alloc_prepare_func (queue);

	if (queue->free_list) {
		/* Use the previously allocated queue sections if possible */
		section = queue->free_list;
		queue->free_list = section->next;
	} else {
		/* Allocate a new section */
		section = mono_sgen_alloc_internal_fixed (queue->allocator, INTERNAL_MEM_GRAY_QUEUE);
	}

	section->end = 0;

	/* Link it with the others */
	section->next = queue->first;
	queue->first = section;
}

static void
gray_object_free_queue_section (GrayQueueSection *section, SgenInternalAllocator *thread_allocator)
{
	mono_sgen_free_internal_delayed (section, INTERNAL_MEM_GRAY_QUEUE, thread_allocator);
}

static inline gboolean
gray_object_queue_is_empty (GrayQueue *queue)
{
	return queue->first == NULL;
}

/*
 * The following two functions are called in the inner loops of the
 * collector, so they need to be as fast as possible.  We have macros
 * for them in sgen-gc.h.
 */

void
mono_sgen_gray_object_enqueue (GrayQueue *queue, char *obj)
{
	DEBUG (9, g_assert (obj));
	if (G_UNLIKELY (!queue->first || queue->first->end == SGEN_GRAY_QUEUE_SECTION_SIZE))
		gray_object_alloc_queue_section (queue);
	DEBUG (9, g_assert (queue->first && queue->first->end < SGEN_GRAY_QUEUE_SECTION_SIZE));
	queue->first->objects [queue->first->end++] = obj;

	DEBUG (9, ++queue->balance);
}

char*
mono_sgen_gray_object_dequeue (GrayQueue *queue)
{
	char *obj;

	if (gray_object_queue_is_empty (queue))
		return NULL;

	DEBUG (9, g_assert (queue->first->end));

	obj = queue->first->objects [--queue->first->end];

	if (G_UNLIKELY (queue->first->end == 0)) {
		GrayQueueSection *section = queue->first;
		queue->first = section->next;
		section->next = queue->free_list;
		queue->free_list = section;
	}

	DEBUG (9, --queue->balance);

	return obj;
}

static GrayQueueSection*
gray_object_dequeue_section (GrayQueue *queue)
{
	GrayQueueSection *section;

	if (!queue->first)
		return NULL;

	section = queue->first;
	queue->first = section->next;

	section->next = NULL;

	return section;
}

static void
gray_object_enqueue_section (GrayQueue *queue, GrayQueueSection *section)
{
	section->next = queue->first;
	queue->first = section;
}

static void
gray_object_queue_init (GrayQueue *queue, SgenInternalAllocator *allocator)
{
	GrayQueueSection *section, *next;
	int i;

	g_assert (gray_object_queue_is_empty (queue));
	DEBUG (9, g_assert (queue->balance == 0));

	queue->allocator = allocator;

	/* Free the extra sections allocated during the last collection */
	i = 0;
	for (section = queue->free_list; section && i < GRAY_QUEUE_LENGTH_LIMIT - 1; section = section->next)
		i ++;
	if (!section)
		return;
	while (section->next) {
		next = section->next;
		section->next = next->next;
		gray_object_free_queue_section (next, allocator);
	}
}

static void
gray_object_queue_init_with_alloc_prepare (GrayQueue *queue, SgenInternalAllocator *allocator, GrayQueueAllocPrepareFunc func, void *data)
{
	gray_object_queue_init (queue, allocator);
	queue->alloc_prepare_func = func;
	queue->alloc_prepare_data = data;
}
