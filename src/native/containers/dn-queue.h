// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_QUEUE_H__
#define __DN_QUEUE_H__

#include "dn-utils.h"
#include "dn-allocator.h"
#include "dn-list.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef void (DN_CALLBACK_CALLTYPE *dn_queue_dispose_func_t) (void *data);

typedef struct _dn_queue_t dn_queue_t;
struct _dn_queue_t {
	struct {
		dn_list_t list;
	} _internal;
	uint32_t size;
};

dn_queue_t *
dn_queue_custom_alloc (dn_allocator_t *allocator);

static inline dn_queue_t *
dn_queue_alloc (void)
{
	return dn_queue_custom_alloc (DN_DEFAULT_ALLOCATOR);
}

void
dn_queue_custom_free (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func);

static inline void
dn_queue_free (dn_queue_t *queue)
{
	dn_queue_custom_free (queue, NULL);
}

bool
dn_queue_custom_init (
	dn_queue_t *queue,
	dn_allocator_t *allocator);

static inline bool
dn_queue_init (dn_queue_t *queue)
{
	return dn_queue_custom_init (queue, DN_DEFAULT_ALLOCATOR);
}

void
dn_queue_custom_dispose (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func);

static inline void
dn_queue_dispose (dn_queue_t *queue)
{
	dn_queue_custom_dispose (queue, NULL);
}

static inline void **
dn_queue_front (dn_queue_t *queue)
{
	DN_ASSERT (queue && queue->size != 0);
	return dn_list_front (&queue->_internal.list);
}

#define dn_queue_front_t(queue, type) \
	(type *)dn_queue_front ((queue))

static inline void **
dn_queue_back (dn_queue_t *queue)
{
	DN_ASSERT (queue && queue->size != 0);
	return dn_list_back (&queue->_internal.list);
}

#define dn_queue_back_t(queue, type) \
	(type *)dn_queue_back ((queue))

static inline bool
dn_queue_empty (dn_queue_t *queue)
{
	DN_ASSERT (queue);
	return queue->size == 0;
}

static inline uint32_t
dn_queue_size (dn_queue_t *queue)
{
	DN_ASSERT (queue);
	return queue->size;
}

static inline bool
dn_queue_push (
	dn_queue_t *queue,
	void *data)
{
	DN_ASSERT (queue);

	bool result = dn_list_push_back (&queue->_internal.list, data);
	if (result)
		queue->size ++;

	return result;
}

#define dn_queue_push_t(queue, type, data) \
	dn_queue_push ((queue),((void *)(ptrdiff_t)(data)))

static inline void
dn_queue_custom_pop (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func)
{
	DN_ASSERT (queue);
	dn_list_custom_pop_front (&queue->_internal.list, dispose_func);
	queue->size --;
}
static inline void
dn_queue_pop (dn_queue_t *queue)
{
	DN_ASSERT (queue);
	dn_list_pop_front (&queue->_internal.list);
	queue->size --;
}

static inline void
dn_queue_custom_clear (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func)
{
	DN_ASSERT (queue);
	dn_list_custom_clear (&queue->_internal.list, dispose_func);
	queue->size = 0;
}

static inline void
dn_queue_clear (dn_queue_t *queue)
{
	dn_queue_custom_clear (queue, NULL);
}

#ifdef __cplusplus
} // extern "C"
#endif

#endif /* __DN_QUEUE_H__ */
