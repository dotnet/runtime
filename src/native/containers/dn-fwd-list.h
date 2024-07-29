// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_FWD_LIST_H__
#define __DN_FWD_LIST_H__

#include "dn-utils.h"
#include "dn-allocator.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef int32_t (DN_CALLBACK_CALLTYPE *dn_fwd_list_compare_func_t) (const void *a, const void *b);
typedef bool (DN_CALLBACK_CALLTYPE *dn_fwd_list_equal_func_t) (const void *a, const void *b);
typedef void (DN_CALLBACK_CALLTYPE *dn_fwd_list_for_each_func_t) (void *data, void *user_data);
typedef void (DN_CALLBACK_CALLTYPE *dn_fwd_list_dispose_func_t) (void *data);

typedef struct _dn_fwd_list_node_t dn_fwd_list_node_t;
struct _dn_fwd_list_node_t {
	void *data;
	dn_fwd_list_node_t *next;
};

typedef struct _dn_fwd_list_t dn_fwd_list_t;
struct _dn_fwd_list_t {
	dn_fwd_list_node_t *head;
	dn_fwd_list_node_t *tail;
	struct {
		dn_allocator_t *_allocator;
	} _internal;
};

typedef struct _dn_fwd_list_it_t dn_fwd_list_it_t;
struct _dn_fwd_list_it_t {
	dn_fwd_list_node_t *it;
	struct {
		dn_fwd_list_t *_list;
	} _internal;
};

typedef struct _dn_fwd_list_result_t dn_fwd_list_result_t;
struct _dn_fwd_list_result_t {
	dn_fwd_list_it_t it;
	bool result;
};

static inline dn_fwd_list_it_t
dn_fwd_list_begin (dn_fwd_list_t *list)
{
	DN_ASSERT (list);
	dn_fwd_list_it_t it = { list->head, { list } };
	return it;
}

static inline dn_fwd_list_it_t
dn_fwd_list_end (dn_fwd_list_t *list)
{
	DN_ASSERT (list);
	dn_fwd_list_it_t it = { NULL, { list } };
	return it;
}

static inline void
dn_fwd_list_it_advance (
	dn_fwd_list_it_t *it,
	uint32_t n)
{
	DN_ASSERT (it);

	while (n > 0 && it->it) {
		it->it = it->it->next;
		n--;
	}
}

static inline dn_fwd_list_it_t
dn_fwd_list_it_next (dn_fwd_list_it_t it)
{
	DN_ASSERT (it.it);
	it.it = it.it->next;
	return it;
}

static inline dn_fwd_list_it_t
dn_fwd_list_it_next_n (
	dn_fwd_list_it_t it,
	uint32_t n)
{
	dn_fwd_list_it_advance (&it, n);
	return it;
}

static inline void **
dn_fwd_list_it_data (dn_fwd_list_it_t it)
{
	DN_ASSERT (it.it);
	return &(it.it->data);
}

#define dn_fwd_list_it_data_t(it, type) \
	(type *)dn_fwd_list_it_data ((it))

static inline bool
dn_fwd_list_it_begin (dn_fwd_list_it_t it)
{
	DN_ASSERT (it._internal._list);
	return !(it.it == it._internal._list->head);
}

static inline bool
dn_fwd_list_it_end (dn_fwd_list_it_t it)
{
	return !(it.it);
}

#define DN_FWD_LIST_FOREACH_BEGIN(var_type, var_name, list) do { \
	var_type var_name; \
	for (dn_fwd_list_node_t *__it##var_name = (list)->head; __it##var_name; __it##var_name = __it##var_name->next) { \
		var_name = (var_type)__it##var_name->data;

#define DN_FWD_LIST_FOREACH_END \
		} \
	} while (0)

dn_fwd_list_t *
dn_fwd_list_custom_alloc (dn_allocator_t *allocator);

static inline dn_fwd_list_t *
dn_fwd_list_alloc (void)
{
	return dn_fwd_list_custom_alloc (DN_DEFAULT_ALLOCATOR);
}

void
dn_fwd_list_custom_free (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func);

static inline void
dn_fwd_list_free (dn_fwd_list_t *list)
{
	dn_fwd_list_custom_free (list, NULL);
}

bool
dn_fwd_list_custom_init (
	dn_fwd_list_t *list,
	dn_allocator_t *allocator);

static inline bool
dn_fwd_list_init (dn_fwd_list_t *list)
{
	return dn_fwd_list_custom_init (list, DN_DEFAULT_ALLOCATOR);
}

void
dn_fwd_list_custom_dispose (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func);

static inline void
dn_fwd_list_dispose (dn_fwd_list_t *list)
{
	dn_fwd_list_custom_dispose (list, NULL);
}

static inline void **
dn_fwd_list_front (const dn_fwd_list_t *list)
{
	DN_ASSERT (list && list->head);
	return &(list->head->data);
}

#define dn_fwd_list_front_t(list, type) \
	(type *)dn_fwd_list_front ((list))

static inline dn_fwd_list_it_t
dn_fwd_list_before_begin (dn_fwd_list_t *list)
{
	DN_ASSERT (list);

	extern dn_fwd_list_node_t _fwd_list_before_begin_it_node;
	dn_fwd_list_it_t it = { &_fwd_list_before_begin_it_node, { list } };

	return it;
}

static inline bool
dn_fwd_list_empty (const dn_fwd_list_t *list)
{
	DN_ASSERT (list);
	return !list->head;
}

static inline uint32_t
dn_fwd_list_max_size (const dn_fwd_list_t *list)
{
	DN_UNREFERENCED_PARAMETER (list);
	return UINT32_MAX;
}

void
dn_fwd_list_custom_clear (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func);

static inline void
dn_fwd_list_clear (dn_fwd_list_t *list)
{
	dn_fwd_list_custom_clear (list, NULL);
}

dn_fwd_list_result_t
dn_fwd_list_insert_after (
	dn_fwd_list_it_t position,
	void *data);

dn_fwd_list_result_t
dn_fwd_list_insert_range_after (
	dn_fwd_list_it_t position,
	dn_fwd_list_it_t first,
	dn_fwd_list_it_t last);

dn_fwd_list_it_t
dn_fwd_list_custom_erase_after (
	dn_fwd_list_it_t position,
	dn_fwd_list_dispose_func_t dispose_func);

static inline dn_fwd_list_it_t
dn_fwd_list_erase_after (dn_fwd_list_it_t position)
{
	return dn_fwd_list_custom_erase_after (position, NULL);
}

static inline bool
dn_fwd_list_push_front (
	dn_fwd_list_t *list,
	void *data)
{
	DN_ASSERT (list);
	return dn_fwd_list_insert_after (dn_fwd_list_before_begin (list), data).result;
}

void
dn_fwd_list_custom_pop_front (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func);

static inline void
dn_fwd_list_pop_front (dn_fwd_list_t *list)
{
	dn_fwd_list_custom_pop_front (list, NULL);
}

bool
dn_fwd_list_custom_resize (
	dn_fwd_list_t *list,
	uint32_t count,
	dn_fwd_list_dispose_func_t dispose_func);

static inline bool
dn_fwd_list_resize (
	dn_fwd_list_t *list,
	uint32_t count)
{
	return dn_fwd_list_custom_resize (list, count, NULL);
}

void
dn_fwd_list_custom_remove (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_dispose_func_t disopse_func);

static inline void
dn_fwd_list_remove (
	dn_fwd_list_t *list,
	const void *data)
{
	dn_fwd_list_custom_remove (list, data, NULL);
}

void
dn_fwd_list_custom_remove_if (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func,
	dn_fwd_list_dispose_func_t dispose_func);

static inline void
dn_fwd_list_remove_if (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func)
{
	dn_fwd_list_custom_remove_if (list, data, equal_func, NULL);
}

void
dn_fwd_list_reverse (dn_fwd_list_t *list);

void
dn_fwd_list_for_each (
	dn_fwd_list_t *list,
	dn_fwd_list_for_each_func_t for_each_func,
	void *user_data);

void
dn_fwd_list_sort (
	dn_fwd_list_t *list,
	dn_fwd_list_compare_func_t compare_func);

dn_fwd_list_it_t
dn_fwd_list_custom_find (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func);

static inline dn_fwd_list_it_t
dn_fwd_list_find (
	dn_fwd_list_t *list,
	const void *data)
{
	return dn_fwd_list_custom_find (list, data, NULL);
}

#ifdef __cplusplus
} // extern "C"
#endif

#endif /* __DN_FWD_LIST_H__ */
