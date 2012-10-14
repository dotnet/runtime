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
#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "utils/mono-counters.h"

#define GRAY_QUEUE_LENGTH_LIMIT	64

void
sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue)
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
		section = sgen_alloc_internal (INTERNAL_MEM_GRAY_QUEUE);
	}

	section->end = 0;

	/* Link it with the others */
	section->next = queue->first;
	queue->first = section;
}

void
sgen_gray_object_free_queue_section (GrayQueueSection *section)
{
	sgen_free_internal (section, INTERNAL_MEM_GRAY_QUEUE);
}

/*
 * The following two functions are called in the inner loops of the
 * collector, so they need to be as fast as possible.  We have macros
 * for them in sgen-gc.h.
 */

void
sgen_gray_object_enqueue (SgenGrayQueue *queue, char *obj)
{
	DEBUG (9, g_assert (obj));
	//sgen_check_objref (obj);
	if (G_UNLIKELY (!queue->first || queue->first->end == SGEN_GRAY_QUEUE_SECTION_SIZE))
		sgen_gray_object_alloc_queue_section (queue);
	DEBUG (9, g_assert (queue->first && queue->first->end < SGEN_GRAY_QUEUE_SECTION_SIZE));
	queue->first->objects [queue->first->end++] = obj;

	DEBUG (9, ++queue->balance);
}

char*
sgen_gray_object_dequeue (SgenGrayQueue *queue)
{
	char *obj;

	if (sgen_gray_object_queue_is_empty (queue))
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

GrayQueueSection*
sgen_gray_object_dequeue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	if (!queue->first)
		return NULL;

	section = queue->first;
	queue->first = section->next;

	section->next = NULL;

	return section;
}

void
sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section)
{
	section->next = queue->first;
	queue->first = section;
}

void
sgen_gray_object_queue_init (SgenGrayQueue *queue)
{
	GrayQueueSection *section, *next;
	int i;

	g_assert (sgen_gray_object_queue_is_empty (queue));
	DEBUG (9, g_assert (queue->balance == 0));

	/* Free the extra sections allocated during the last collection */
	i = 0;
	for (section = queue->free_list; section && i < GRAY_QUEUE_LENGTH_LIMIT - 1; section = section->next)
		i ++;
	if (!section)
		return;
	while (section->next) {
		next = section->next;
		section->next = next->next;
		sgen_gray_object_free_queue_section (next);
	}
}

void
sgen_gray_object_queue_init_with_alloc_prepare (SgenGrayQueue *queue, GrayQueueAllocPrepareFunc func, void *data)
{
	sgen_gray_object_queue_init (queue);
	queue->alloc_prepare_func = func;
	queue->alloc_prepare_data = data;
}

#endif
