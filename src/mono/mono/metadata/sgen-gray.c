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
#define GRAY_QUEUE_SECTION_SIZE	(128 - 3)
#define GRAY_QUEUE_LENGTH_LIMIT	64

/*
 * This is a stack now instead of a queue, so the most recently added items are removed
 * first, improving cache locality, and keeping the stack size manageable.
 */
typedef struct _GrayQueueSection GrayQueueSection;
struct _GrayQueueSection {
	int end;
	GrayQueueSection *next, *prev;
	char *objects [GRAY_QUEUE_SECTION_SIZE];
};

static GrayQueueSection *gray_queue_start = NULL;
static GrayQueueSection *gray_queue_end = NULL;

static int gray_queue_balance = 0;
static int num_gray_queue_sections = 0;

static void
gray_object_alloc_queue_section (void)
{
	GrayQueueSection *section;

	/* Use the previously allocated queue sections if possible */
	if (!gray_queue_end && gray_queue_start) {
		gray_queue_end = gray_queue_start;
		gray_queue_end->end = 0;
		return;
	}
	if (gray_queue_end && gray_queue_end->next) {
		gray_queue_end = gray_queue_end->next;
		gray_queue_end->end = 0;
		return;
	}

	/* Allocate a new section */
	section = get_internal_mem (sizeof (GrayQueueSection), INTERNAL_MEM_GRAY_QUEUE);
	++num_gray_queue_sections;

	section->end = 0;
	section->next = NULL;
	section->prev = NULL;

	/* Link it with the others */
	if (gray_queue_end) {
		gray_queue_end->next = section;
		section->prev = gray_queue_end;
	} else {
		g_assert (!gray_queue_start);
		gray_queue_start = section;
	}
	gray_queue_end = section;
}

static void
gray_object_free_queue_section (GrayQueueSection *section)
{
	free_internal_mem (section, INTERNAL_MEM_GRAY_QUEUE);
	--num_gray_queue_sections;
}

static inline gboolean
gray_object_queue_is_empty (void)
{
	return gray_queue_end == NULL;
}

/*
 * The following two functions are called in the inner loops of the
 * collector, so they need to be as fast as possible.  We have macros
 * for them below.
 */

static inline void
gray_object_enqueue (char *obj)
{
	DEBUG (9, g_assert (obj));
	if (G_UNLIKELY (!gray_queue_end || gray_queue_end->end == GRAY_QUEUE_SECTION_SIZE))
		gray_object_alloc_queue_section ();
	DEBUG (9, g_assert (gray_queue_end && gray_queue_end->end < GRAY_QUEUE_SECTION_SIZE));
	gray_queue_end->objects [gray_queue_end->end++] = obj;

	DEBUG (9, ++gray_queue_balance);
}

static inline char*
gray_object_dequeue (void)
{
	char *obj;

	if (gray_object_queue_is_empty ())
		return NULL;

	DEBUG (9, g_assert (gray_queue_end->end));

	obj = gray_queue_end->objects [--gray_queue_end->end];

	if (G_UNLIKELY (gray_queue_end->end == 0))
		gray_queue_end = gray_queue_end->prev;

	DEBUG (9, --gray_queue_balance);

	return obj;
}

#if MAX_DEBUG_LEVEL >= 9
#define GRAY_OBJECT_ENQUEUE gray_object_enqueue
#define GRAY_OBJECT_DEQUEUE(o) ((o) = gray_object_dequeue ())
#else
#define GRAY_OBJECT_ENQUEUE(o) do {					\
		if (G_UNLIKELY (!gray_queue_end || gray_queue_end->end == GRAY_QUEUE_SECTION_SIZE)) \
			gray_object_alloc_queue_section ();		\
		gray_queue_end->objects [gray_queue_end->end++] = (o);	\
	} while (0)
#define GRAY_OBJECT_DEQUEUE(o) do {					\
		if (!gray_queue_end) {					\
			(o) = NULL;					\
		} else {						\
			(o) = gray_queue_end->objects [--gray_queue_end->end]; \
			if (G_UNLIKELY (gray_queue_end->end == 0))	\
				gray_queue_end = gray_queue_end->prev;	\
		}							\
	} while (0)
#endif

static void
gray_object_queue_init (void)
{
	GrayQueueSection *section, *next;
	int i;

	g_assert (gray_object_queue_is_empty ());
	g_assert (sizeof (GrayQueueSection) < MAX_FREELIST_SIZE);
	DEBUG (9, g_assert (gray_queue_balance == 0));

	/* Free the extra sections allocated during the last collection */
	i = 0;
	for (section = gray_queue_start; section && i < GRAY_QUEUE_LENGTH_LIMIT; section = section->next)
		i ++;
	if (section) {
		if (section->prev)
			section->prev->next = NULL;
		for (; section; section = next) {
			next = section->next;
			gray_object_free_queue_section (section);
		}
	}
}
