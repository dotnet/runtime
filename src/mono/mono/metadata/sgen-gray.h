/*
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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
#ifndef __MONO_SGEN_GRAY_H__
#define __MONO_SGEN_GRAY_H__

#define SGEN_GRAY_QUEUE_SECTION_SIZE	(128 - 3)

/*
 * This is a stack now instead of a queue, so the most recently added items are removed
 * first, improving cache locality, and keeping the stack size manageable.
 */
typedef struct _GrayQueueSection GrayQueueSection;
struct _GrayQueueSection {
	int end;
	GrayQueueSection *next;
	char *objects [SGEN_GRAY_QUEUE_SECTION_SIZE];
};

typedef struct _SgenGrayQueue SgenGrayQueue;

typedef void (*GrayQueueAllocPrepareFunc) (SgenGrayQueue*);

struct _SgenGrayQueue {
	GrayQueueSection *first;
	GrayQueueSection *free_list;
	int balance;
	GrayQueueAllocPrepareFunc alloc_prepare_func;
	void *alloc_prepare_data;
};

void sgen_gray_object_enqueue (SgenGrayQueue *queue, char *obj) MONO_INTERNAL;
char* sgen_gray_object_dequeue (SgenGrayQueue *queue) MONO_INTERNAL;
GrayQueueSection* sgen_gray_object_dequeue_section (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section) MONO_INTERNAL;
void sgen_gray_object_queue_init (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_queue_init_with_alloc_prepare (SgenGrayQueue *queue, GrayQueueAllocPrepareFunc func, void *data) MONO_INTERNAL;
void sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_free_queue_section (GrayQueueSection *section) MONO_INTERNAL;
gboolean sgen_drain_gray_stack (SgenGrayQueue *queue, int max_objs) MONO_INTERNAL;

static inline gboolean
sgen_gray_object_queue_is_empty (SgenGrayQueue *queue)
{
	return queue->first == NULL;
}

#endif
