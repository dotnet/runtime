// Implementation of ep-rt.h targeting CoreCLR runtime.
#ifndef __EVENTPIPE_RT_CORECLR_H__
#define __EVENTPIPE_RT_CORECLR_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-thread.h"
#include "ep-types.h"
#include "ep-provider.h"
#include "ep-session-provider.h"
#include "fstream.h"
#include "typestring.h"
#include "win32threadpool.h"

#undef EP_ARRAY_SIZE
#define EP_ARRAY_SIZE(expr) (sizeof(expr) / sizeof ((expr) [0]))

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT INFINITE

#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER { GCX_PREEMP();

#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE FORCEINLINE

#undef EP_NEVER_INLINE
#define EP_NEVER_INLINE NOINLINE

#undef EP_ALIGN_UP
#define EP_ALIGN_UP(val,align) ALIGN_UP(val,align)

#ifndef EP_RT_BUILD_TYPE_FUNC_NAME
#define EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, type_name, func_name) \
prefix_name ## _rt_ ## type_name ## _ ## func_name
#endif

template<typename LIST_TYPE>
static
inline
void
_rt_coreclr_list_alloc (LIST_TYPE *list) {
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL);

	list->list = new (nothrow) typename LIST_TYPE::list_type_t ();
}

template<typename LIST_TYPE>
static
inline
void
_rt_coreclr_list_free (
	LIST_TYPE *list,
	void (*callback)(void *))
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL);

	if (list->list) {
		while (!list->list->IsEmpty ()) {
				typename LIST_TYPE::element_type_t *current = list->list->RemoveHead ();
				if (callback)
					callback (reinterpret_cast<void *>(current->GetValue ()));
				delete current;
		}
		delete list->list;
	}
	list->list = NULL;
}

template<typename LIST_TYPE>
static
inline
void
_rt_coreclr_list_clear (
	LIST_TYPE *list,
	void (*callback)(void *))
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL);

	while (!list->list->IsEmpty ()) {
		typename LIST_TYPE::element_type_t *current = list->list->RemoveHead ();
		if (callback)
				callback (reinterpret_cast<void *>(current->GetValue ()));
		delete current;
	}
}

template<typename LIST_TYPE, typename LIST_ITEM>
static
inline
bool
_rt_coreclr_list_append (
	LIST_TYPE *list,
	LIST_ITEM item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL);

	typename LIST_TYPE::element_type_t *node = new (nothrow) typename LIST_TYPE::element_type_t (item);
	if (node)
		list->list->InsertTail (node);
	return (node != NULL);
}

template<typename LIST_TYPE, typename LIST_ITEM, typename CONST_LIST_ITEM = LIST_ITEM>
static
inline
void
_rt_coreclr_list_remove (
	LIST_TYPE *list,
	CONST_LIST_ITEM item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL);

	typename LIST_TYPE::element_type_t *current = list->list->GetHead ();
	while (current) {
		if (current->GetValue () == item) {
			if (list->list->FindAndRemove (current))
					delete current;
			break;
		}
		current = list->list->GetNext (current);
	}
}

template<typename LIST_TYPE, typename LIST_ITEM, typename CONST_LIST_TYPE = const LIST_TYPE, typename CONST_LIST_ITEM = const LIST_ITEM>
static
inline
bool
_rt_coreclr_list_find (
	CONST_LIST_TYPE *list,
	CONST_LIST_ITEM item_to_find,
	LIST_ITEM *found_item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL);
	EP_ASSERT (found_item != NULL);

	bool found = false;
	typename LIST_TYPE::element_type_t *current = list->list->GetHead ();
	while (current) {
		if (current->GetValue () == item_to_find) {
			*found_item = current->GetValue ();
			found = true;
			break;
		}
		current = list->list->GetNext (current);
	}
	return found;
}

template<typename LIST_TYPE, typename CONST_LIST_TYPE = const LIST_TYPE>
static
inline
bool
_rt_coreclr_list_is_empty (CONST_LIST_TYPE *list)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL);

	return (list->list == NULL || list->list->IsEmpty ());
}

template<typename LIST_TYPE, typename CONST_LIST_TYPE = const LIST_TYPE>
static
inline
bool
_rt_coreclr_list_is_valid (CONST_LIST_TYPE *list)
{
	STATIC_CONTRACT_NOTHROW;
	return (list != NULL && list->list != NULL);
}

template<typename LIST_TYPE, typename ITERATOR_TYPE, typename CONST_LIST_TYPE = const LIST_TYPE>
static
inline
ITERATOR_TYPE
_rt_coreclr_list_iterator_begin (CONST_LIST_TYPE *list)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL);

	return list->list->begin ();
}

template<typename LIST_TYPE, typename ITERATOR_TYPE, typename CONST_LIST_TYPE = const LIST_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
bool
_rt_coreclr_list_iterator_end (
	CONST_LIST_TYPE *list,
	CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (list != NULL && list->list != NULL && iterator != NULL);

	return (*iterator == list->list->end ());
}

template<typename ITERATOR_TYPE>
static
inline
void
_rt_coreclr_list_iterator_next (ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL);

	(*iterator)++;
}

template<typename ITERATOR_TYPE, typename ITEM_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
ITEM_TYPE
_rt_coreclr_list_iterator_value (CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL);

	return const_cast<ITERATOR_TYPE *>(iterator)->operator*();
}

template<typename QUEUE_TYPE>
static
inline
void
_rt_coreclr_queue_alloc (QUEUE_TYPE *queue)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL);

	queue->queue = new (nothrow) typename QUEUE_TYPE::queue_type_t ();
}

template<typename QUEUE_TYPE>
static
inline
void
_rt_coreclr_queue_free (QUEUE_TYPE *queue)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL);

	if (queue->queue)
		delete queue->queue;
	queue->queue = NULL;
}

template<typename QUEUE_TYPE, typename ITEM_TYPE>
static
inline
bool
_rt_coreclr_queue_pop_head (
	QUEUE_TYPE *queue,
	ITEM_TYPE *item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL && queue->queue != NULL && item != NULL);

	bool found = true;
	typename QUEUE_TYPE::element_type_t *node = queue->queue->RemoveHead ();
	if (node) {
		*item = node->m_Value;
		delete node;
	} else {
		*item = NULL;
		found = false;
	}
	return found;
}

template<typename QUEUE_TYPE, typename ITEM_TYPE>
static
inline
bool
_rt_coreclr_queue_push_head (
	QUEUE_TYPE *queue,
	ITEM_TYPE item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL && queue->queue != NULL);

	typename QUEUE_TYPE::element_type_t *node = new (nothrow) typename QUEUE_TYPE::element_type_t (item);
	if (node)
		queue->queue->InsertHead (node);
	return (node != NULL);
}

template<typename QUEUE_TYPE, typename ITEM_TYPE>
static
inline
bool
_rt_coreclr_queue_push_tail (
	QUEUE_TYPE *queue,
	ITEM_TYPE item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL && queue->queue != NULL);

	typename QUEUE_TYPE::element_type_t *node = new (nothrow) typename QUEUE_TYPE::element_type_t (item);
	if (node)
		queue->queue->InsertTail (node);
	return (node != NULL);
}

template<typename QUEUE_TYPE, typename CONST_QUEUE_TYPE = const QUEUE_TYPE>
static
inline
bool
_rt_coreclr_queue_is_empty (CONST_QUEUE_TYPE *queue)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (queue != NULL && queue->queue != NULL);

	return (queue->queue != NULL && queue->queue->IsEmpty ());
}

template<typename QUEUE_TYPE, typename CONST_QUEUE_TYPE = const QUEUE_TYPE>
static
inline
bool
_rt_coreclr_queue_is_valid (CONST_QUEUE_TYPE *queue)
{
	STATIC_CONTRACT_NOTHROW;
	return (queue != NULL && queue->queue != NULL);
}

template<typename ARRAY_TYPE>
static
inline
void
_rt_coreclr_array_alloc (ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL);

	ep_array->array = new (nothrow) typename ARRAY_TYPE::array_type_t ();
}

template<typename ARRAY_TYPE>
static
inline
void
_rt_coreclr_array_alloc_capacity (
	ARRAY_TYPE *ep_array,
	size_t capacity)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL);

	ep_array->array = new (nothrow) typename ARRAY_TYPE::array_type_t ();
	if (ep_array->array)
		ep_array->array->AllocNoThrow (capacity);
}

template<typename ARRAY_TYPE>
static
inline
void
_rt_coreclr_array_init_capacity (
	ARRAY_TYPE *ep_array,
	size_t capacity)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL);

	if (ep_array->array)
		ep_array->array->AllocNoThrow (capacity);
}

template<typename ARRAY_TYPE>
static
inline
void
_rt_coreclr_array_free (ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL);

	if (ep_array->array) {
		delete ep_array->array;
		ep_array->array = NULL;
	}
}

template<typename ARRAY_TYPE, typename ITEM_TYPE>
static
inline
bool
_rt_coreclr_array_append (
	ARRAY_TYPE *ep_array,
	ITEM_TYPE item)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	return ep_array->array->PushNoThrow (item);
}

template<typename ARRAY_TYPE, typename ITEM_TYPE>
static
inline
void
_rt_coreclr_array_clear (ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	while (ep_array->array->Size () > 0)
		ITEM_TYPE item = ep_array->array->Pop ();
	ep_array->array->Shrink ();
}

template<typename ARRAY_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE>
static
inline
size_t
_rt_coreclr_array_size (CONST_ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	return ep_array->array->Size ();
}

template<typename ARRAY_TYPE, typename ITEM_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE>
static
inline
ITEM_TYPE *
_rt_coreclr_array_data (CONST_ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	return ep_array->array->Ptr ();
}

template<typename ARRAY_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE>
static
inline
bool
_rt_coreclr_array_is_valid (CONST_ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	return (ep_array->array != NULL);
}

template<typename ARRAY_TYPE, typename ITERATOR_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE>
static
inline
ITERATOR_TYPE
_rt_coreclr_array_iterator_begin (CONST_ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	ITERATOR_TYPE temp;
	temp.array = ep_array->array;
	temp.index = 0;
	return temp;
}

template<typename ARRAY_TYPE, typename ITERATOR_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
bool
_rt_coreclr_array_iterator_end (
	CONST_ARRAY_TYPE *ep_array,
	CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && iterator != NULL && iterator->array != NULL);

	return (iterator->index >= static_cast<size_t>(iterator->array->Size ()));
}

template<typename ITERATOR_TYPE>
static
inline
void
_rt_coreclr_array_iterator_next (ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL);

	iterator->index++;
}

template<typename ITERATOR_TYPE, typename ITEM_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
ITEM_TYPE
_rt_coreclr_array_iterator_value (const CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL && iterator->array != NULL);
	EP_ASSERT (iterator->index < static_cast<size_t>(iterator->array->Size ()));

	return iterator->array->operator[] (iterator->index);
}

template<typename ARRAY_TYPE, typename ITERATOR_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE>
static
inline
ITERATOR_TYPE
_rt_coreclr_array_reverse_iterator_begin (CONST_ARRAY_TYPE *ep_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && ep_array->array != NULL);

	ITERATOR_TYPE temp;
	temp.array = ep_array->array;
	temp.index = static_cast<size_t>(ep_array->array->Size ());
	return temp;
}

template<typename ARRAY_TYPE, typename ITERATOR_TYPE, typename CONST_ARRAY_TYPE = const ARRAY_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
bool
_rt_coreclr_array_reverse_iterator_end (
	CONST_ARRAY_TYPE *ep_array,
	CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_array != NULL && iterator != NULL && iterator->array != NULL);

	return (iterator->index == 0);
}

template<typename ITERATOR_TYPE>
static
inline
void
_rt_coreclr_array_reverse_iterator_next (ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL);

	iterator->index--;
}

template<typename ITERATOR_TYPE, typename ITEM_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
ITEM_TYPE
_rt_coreclr_array_reverse_iterator_value (CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (iterator != NULL && iterator->array != NULL);
	EP_ASSERT (iterator->index > 0);

	return iterator->array->operator[] (iterator->index - 1);
}

template<typename HASH_MAP_TYPE>
static
inline
void
_rt_coreclr_hash_map_alloc (
	HASH_MAP_TYPE *hash_map,
	uint32_t (*hash_callback)(const void *),
	bool (*eq_callback)(const void *, const void *),
	void (*key_free_callback)(void *),
	void (*value_free_callback)(void *))
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && key_free_callback == NULL);

	hash_map->table = new (nothrow) typename HASH_MAP_TYPE::table_type_t ();
	hash_map->callbacks.key_free_func = key_free_callback;
	hash_map->callbacks.value_free_func = value_free_callback;
}

template<typename HASH_MAP_TYPE>
static
inline
void
_rt_coreclr_hash_map_free (HASH_MAP_TYPE *hash_map)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL);

	if (hash_map->table) {
		if (hash_map->callbacks.value_free_func) {
			for (typename HASH_MAP_TYPE::table_type_t::Iterator iterator = hash_map->table->Begin (); iterator != hash_map->table->End (); ++iterator)
					hash_map->callbacks.value_free_func (reinterpret_cast<void *>((ptrdiff_t)(iterator->Value ())));
		}
		delete hash_map->table;
	}
}

template<typename HASH_MAP_TYPE, typename KEY_TYPE, typename VALUE_TYPE>
static
inline
bool
_rt_coreclr_hash_map_add (
	HASH_MAP_TYPE *hash_map,
	KEY_TYPE key,
	VALUE_TYPE value)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	return hash_map->table->AddNoThrow (typename HASH_MAP_TYPE::table_type_t::element_t (key, value));
}

template<typename HASH_MAP_TYPE, typename KEY_TYPE, typename VALUE_TYPE>
static
inline
bool
_rt_coreclr_hash_map_add_or_replace (
	HASH_MAP_TYPE *hash_map,
	KEY_TYPE key,
	VALUE_TYPE value)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	return hash_map->table->AddOrReplaceNoThrow (typename HASH_MAP_TYPE::table_type_t::element_t (key, value));
}

template<typename HASH_MAP_TYPE>
static
inline
void
_rt_coreclr_hash_map_remove_all (HASH_MAP_TYPE *hash_map)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	if (hash_map->callbacks.value_free_func) {
		for (typename HASH_MAP_TYPE::table_type_t::Iterator iterator = hash_map->table->Begin (); iterator != hash_map->table->End (); ++iterator)
			hash_map->callbacks.value_free_func (reinterpret_cast<void *>((ptrdiff_t)(iterator->Value ())));
	}
	hash_map->table->RemoveAll ();
}

template<typename HASH_MAP_TYPE, typename KEY_TYPE, typename VALUE_TYPE, typename CONST_HASH_MAP_TYPE = const HASH_MAP_TYPE, typename CONST_KEY_TYPE = const KEY_TYPE>
static
inline
bool
_rt_coreclr_hash_map_lookup (
	CONST_HASH_MAP_TYPE *hash_map,
	CONST_KEY_TYPE key,
	VALUE_TYPE *value)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	const typename HASH_MAP_TYPE::table_type_t::element_t *ret = hash_map->table->LookupPtr ((KEY_TYPE)key);
	if (ret == NULL)
		return false;
	*value = ret->Value ();
	return true;
}

template<typename HASH_MAP_TYPE, typename CONST_HASH_MAP_TYPE = const HASH_MAP_TYPE>
static
inline
uint32_t
_rt_coreclr_hash_map_count (CONST_HASH_MAP_TYPE *hash_map)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	return hash_map->table->GetCount ();
}

template<typename HASH_MAP_TYPE, typename CONST_HASH_MAP_TYPE = const HASH_MAP_TYPE>
static
inline
bool
_rt_coreclr_hash_map_is_valid (CONST_HASH_MAP_TYPE *hash_map)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);

	return (hash_map != NULL && hash_map->table != NULL);
}

template<typename HASH_MAP_TYPE, typename KEY_TYPE, typename CONST_KEY_TYPE = const KEY_TYPE>
static
inline
void
_rt_coreclr_hash_map_remove (
	HASH_MAP_TYPE *hash_map,
	CONST_KEY_TYPE key)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	const typename HASH_MAP_TYPE::table_type_t::element_t *ret = NULL;
	if (hash_map->callbacks.value_free_func)
		ret = hash_map->table->LookupPtr ((KEY_TYPE)key);
	hash_map->table->Remove ((KEY_TYPE)key);
	if (ret)
		hash_map->callbacks.value_free_func (reinterpret_cast<void *>(static_cast<ptrdiff_t>(ret->Value ())));
}

template<typename HASH_MAP_TYPE, typename ITERATOR_TYPE, typename CONST_HASH_MAP_TYPE = const HASH_MAP_TYPE>
static
inline
ITERATOR_TYPE
_rt_coreclr_hash_map_iterator_begin (CONST_HASH_MAP_TYPE *hash_map)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL);

	return hash_map->table->Begin ();
}

template<typename HASH_MAP_TYPE, typename ITERATOR_TYPE, typename CONST_HASH_MAP_TYPE = const HASH_MAP_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
bool
_rt_coreclr_hash_map_iterator_end (
	CONST_HASH_MAP_TYPE *hash_map,
	CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (hash_map != NULL && hash_map->table != NULL && iterator != NULL);

	return (hash_map->table->End () == *iterator);
}

template<typename HASH_MAP_TYPE, typename ITERATOR_TYPE>
static
inline
void
_rt_coreclr_hash_map_iterator_next (ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (iterator != NULL);

	(*iterator)++;
}

template<typename HASH_MAP_TYPE, typename ITERATOR_TYPE, typename KEY_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
KEY_TYPE
_rt_coreclr_hash_map_iterator_key (CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (iterator != NULL);

	return (*iterator)->Key ();
}

template<typename HASH_MAP_TYPE, typename ITERATOR_TYPE, typename VALUE_TYPE, typename CONST_ITERATOR_TYPE = const ITERATOR_TYPE>
static
inline
VALUE_TYPE
_rt_coreclr_hash_map_iterator_value (CONST_ITERATOR_TYPE *iterator)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (HASH_MAP_TYPE::table_type_t::s_NoThrow);
	EP_ASSERT (iterator != NULL);

	return (*iterator)->Value ();
}

#define EP_RT_DEFINE_LIST_PREFIX(prefix_name, list_name, list_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, alloc) (list_type *list) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_list_alloc<list_type>(list); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, free) (list_type *list, void (*callback)(void *)) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_list_free<list_type>(list, callback); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, clear) (list_type *list, void (*callback)(void *)) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_list_clear<list_type>(list, callback); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, append) (list_type *list, item_type item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_append<list_type, item_type>(list, item); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, remove) (list_type *list, const item_type item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_list_remove<list_type, item_type>(list, item); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, find) (const list_type *list, const item_type item_to_find, item_type *found_item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_find<list_type, item_type>(list, item_to_find, found_item); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_empty) (const list_type *list) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_is_empty<list_type>(list); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, is_valid) (const list_type *list) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_is_valid<list_type>(list); \
	}

#undef EP_RT_DEFINE_LIST
#define EP_RT_DEFINE_LIST(list_name, list_type, item_type) \
	EP_RT_DEFINE_LIST_PREFIX(ep, list_name, list_type, item_type)

#define EP_RT_DEFINE_LIST_ITERATOR_PREFIX(prefix_name, list_name, list_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_begin) (const list_type *list) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_iterator_begin<list_type, iterator_type>(list); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_end) (const list_type *list, const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_iterator_end<list_type, iterator_type>(list, iterator); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_next) (iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_list_iterator_next<iterator_type>(iterator); \
	} \
	static inline item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, list_name, iterator_value) (const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_list_iterator_value<iterator_type, item_type>(iterator); \
	}

#undef EP_RT_DEFINE_LIST_ITERATOR
#define EP_RT_DEFINE_LIST_ITERATOR(list_name, list_type, iterator_type, item_type) \
	EP_RT_DEFINE_LIST_ITERATOR_PREFIX(ep, list_name, list_type, iterator_type, item_type)

#define EP_RT_DEFINE_QUEUE_PREFIX(prefix_name, queue_name, queue_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, alloc) (queue_type *queue) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_queue_alloc<queue_type>(queue); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, free) (queue_type *queue) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_queue_free<queue_type>(queue); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, pop_head) (queue_type *queue, item_type *item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_queue_pop_head<queue_type, item_type>(queue, item); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_head) (queue_type *queue, item_type item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_queue_push_head<queue_type, item_type>(queue, item); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, push_tail) (queue_type *queue, item_type item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_queue_push_tail<queue_type, item_type>(queue, item); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_empty) (const queue_type *queue) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_queue_is_empty<queue_type>(queue); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, queue_name, is_valid) (const queue_type *queue) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_queue_is_valid<queue_type>(queue); \
	}

#undef EP_RT_DEFINE_QUEUE
#define EP_RT_DEFINE_QUEUE(queue_name, queue_type, item_type) \
	EP_RT_DEFINE_QUEUE_PREFIX(ep, queue_name, queue_type, item_type)

#define EP_RT_DEFINE_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc) (array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_alloc<array_type>(ep_array); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, alloc_capacity) (array_type *ep_array, size_t capacity) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_alloc_capacity<array_type>(ep_array, capacity); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, free) (array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_free<array_type>(ep_array); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, append) (array_type *ep_array, item_type item) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_append<array_type, item_type> (ep_array, item); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, clear) (array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_clear<array_type, item_type> (ep_array); \
	} \
	static inline size_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, size) (const array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_size<array_type> (ep_array); \
	} \
	static inline item_type * EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, data) (const array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_data<array_type, item_type> (ep_array); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, is_valid) (const array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_is_valid<array_type> (ep_array); \
	}

#define EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init) (array_type *ep_array) { \
		STATIC_CONTRACT_NOTHROW; \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init_capacity) (array_type *ep_array, size_t capacity) { \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_init_capacity<array_type>(ep_array, capacity); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, fini) (array_type *ep_array) { \
		STATIC_CONTRACT_NOTHROW; \
	}

#undef EP_RT_DEFINE_ARRAY
#define EP_RT_DEFINE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#undef EP_RT_DEFINE_LOCAL_ARRAY
#define EP_RT_DEFINE_LOCAL_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, var_type) \
	var_type::array_type_t _local_ ##var_name; \
	var_type var_name; \
	var_name.array = &_local_ ##var_name

#define EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_begin) (const array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_iterator_begin<array_type, iterator_type> (ep_array); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_end) (const array_type *ep_array, const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_iterator_end<array_type, iterator_type> (ep_array, iterator); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_next) (iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_iterator_next<iterator_type> (iterator); \
	} \
	static inline item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, iterator_value) (const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_iterator_value<iterator_type, item_type> (iterator); \
	}

#define EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_begin) (const array_type *ep_array) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_reverse_iterator_begin<array_type, iterator_type> (ep_array); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_end) (const array_type *ep_array, const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_reverse_iterator_end<array_type, iterator_type> (ep_array, iterator); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_next) (iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_array_reverse_iterator_next<iterator_type> (iterator); \
	} \
	static inline item_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, reverse_iterator_value) (const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_array_reverse_iterator_value<iterator_type, item_type> (iterator); \
	}

#undef EP_RT_DEFINE_ARRAY_ITERATOR
#define EP_RT_DEFINE_ARRAY_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#undef EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR
#define EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_REVERSE_ITERATOR_PREFIX(ep, array_name, array_type, iterator_type, item_type)

#define EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, alloc) (hash_map_type *hash_map, uint32_t (*hash_callback)(const void *), bool (*eq_callback)(const void *, const void *), void (*key_free_callback)(void *), void (*value_free_callback)(void *)) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_hash_map_alloc<hash_map_type>(hash_map, hash_callback, eq_callback, key_free_callback, value_free_callback); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, free) (hash_map_type *hash_map) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_hash_map_free<hash_map_type>(hash_map); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add) (hash_map_type *hash_map, key_type key, value_type value) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_add<hash_map_type, key_type, value_type>(hash_map, key, value); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove_all) (hash_map_type *hash_map) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_hash_map_remove_all<hash_map_type>(hash_map); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, lookup) (const hash_map_type *hash_map, const key_type key, value_type *value) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_lookup<hash_map_type, key_type, value_type>(hash_map, key, value); \
	} \
	static inline uint32_t EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, count) (const hash_map_type *hash_map) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_count<hash_map_type>(hash_map); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, is_valid) (const hash_map_type *hash_map) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_is_valid<hash_map_type>(hash_map); \
	}

#define EP_RT_DEFINE_HASH_MAP_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, add_or_replace) (hash_map_type *hash_map, key_type key, value_type value) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_add_or_replace<hash_map_type, key_type, value_type>(hash_map, key, value); \
	} \

#define EP_RT_DEFINE_HASH_MAP_REMOVE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_BASE_PREFIX(prefix_name, hash_map_name, hash_map_type, key_type, value_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, remove) (hash_map_type *hash_map, const key_type key) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_hash_map_remove<hash_map_type, key_type>(hash_map, key); \
	}

#undef EP_RT_DEFINE_HASH_MAP
#define EP_RT_DEFINE_HASH_MAP(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#undef EP_RT_DEFINE_HASH_MAP_REMOVE
#define EP_RT_DEFINE_HASH_MAP_REMOVE(hash_map_name, hash_map_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_REMOVE_PREFIX(ep, hash_map_name, hash_map_type, key_type, value_type)

#define EP_RT_DEFINE_HASH_MAP_ITERATOR_PREFIX(prefix_name, hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	static inline iterator_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_begin) (const hash_map_type *hash_map) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_iterator_begin<hash_map_type, iterator_type>(hash_map); \
	} \
	static inline bool EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_end) (const hash_map_type *hash_map, const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_iterator_end<hash_map_type, iterator_type>(hash_map, iterator); \
	} \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_next) (iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		_rt_coreclr_hash_map_iterator_next<hash_map_type, iterator_type>(iterator); \
	} \
	static inline key_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_key) (const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_iterator_key<hash_map_type, iterator_type, key_type>(iterator); \
	} \
	static inline value_type EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, hash_map_name, iterator_value) (const iterator_type *iterator) \
	{ \
		STATIC_CONTRACT_NOTHROW; \
		return _rt_coreclr_hash_map_iterator_value<hash_map_type, iterator_type, value_type>(iterator); \
	}

#undef EP_RT_DEFINE_HASH_MAP_ITERATOR
#define EP_RT_DEFINE_HASH_MAP_ITERATOR(hash_map_name, hash_map_type, iterator_type, key_type, value_type) \
	EP_RT_DEFINE_HASH_MAP_ITERATOR_PREFIX(ep, hash_map_name, hash_map_type, iterator_type, key_type, value_type)

static
inline
char *
diagnostics_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<const ep_char16_t *>(GetCommandLineForDiagnostics ()), -1);
}

static
inline
ep_char8_t **
diagnostics_command_line_get_ref (void)
{
	STATIC_CONTRACT_NOTHROW;

	extern ep_char8_t *_ep_rt_coreclr_diagnostics_cmd_line;
	return &_ep_rt_coreclr_diagnostics_cmd_line;
}

static
inline
void
diagnostics_command_line_lazy_init (void)
{
	STATIC_CONTRACT_NOTHROW;

	//TODO: Real lazy init implementation.
	if (!*diagnostics_command_line_get_ref ())
		*diagnostics_command_line_get_ref () = diagnostics_command_line_get ();
}

static
inline
void
diagnostics_command_line_lazy_clean (void)
{
	STATIC_CONTRACT_NOTHROW;

	//TODO: Real lazy clean up implementation.
	ep_rt_utf8_string_free (*diagnostics_command_line_get_ref ());
	*diagnostics_command_line_get_ref () = NULL;
}

static
inline
ep_rt_lock_handle_t *
ep_rt_coreclr_config_lock_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	extern ep_rt_lock_handle_t _ep_rt_coreclr_config_lock_handle;
	return &_ep_rt_coreclr_config_lock_handle;
}

/*
* Atomics.
*/

static
inline
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(InterlockedIncrement ((volatile LONG *)(value)));
}

static
inline
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(InterlockedDecrement ((volatile LONG *)(value)));
}

static
inline
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(InterlockedIncrement ((volatile LONG *)(value)));
}

static
inline
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(InterlockedDecrement ((volatile LONG *)(value)));
}

static
inline
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int64_t>(InterlockedIncrement64 ((volatile LONG64 *)(value)));
}

static
inline
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int64_t>(InterlockedDecrement64 ((volatile LONG64 *)(value)));
}

static
inline
size_t
ep_rt_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<size_t>(InterlockedCompareExchangeT<size_t> (target, value, expected));
}

/*
 * EventPipe.
 */

EP_RT_DEFINE_ARRAY (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)
EP_RT_DEFINE_ARRAY_ITERATOR (session_id_array, ep_rt_session_id_array_t, ep_rt_session_id_array_iterator_t, EventPipeSessionID)

static
void
ep_rt_init (void)
{
	STATIC_CONTRACT_NOTHROW;

	extern ep_rt_lock_handle_t _ep_rt_coreclr_config_lock_handle;
	extern CrstStatic _ep_rt_coreclr_config_lock;

	_ep_rt_coreclr_config_lock_handle.lock = &_ep_rt_coreclr_config_lock;
	_ep_rt_coreclr_config_lock_handle.lock->InitNoThrow (CrstEventPipe, (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN | CRST_HOST_BREAKABLE));

	if (CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeProcNumbers) != 0) {
#ifndef TARGET_UNIX
		// setup the windows processor group offset table
		uint16_t groups = ::GetActiveProcessorGroupCount ();
		extern uint32_t *_ep_rt_coreclr_proc_group_offsets;
		_ep_rt_coreclr_proc_group_offsets = new (nothrow) uint32_t [groups];
		if (_ep_rt_coreclr_proc_group_offsets) {
			uint32_t procs = 0;
			for (uint16_t i = 0; i < procs; ++i) {
				_ep_rt_coreclr_proc_group_offsets [i] = procs;
				procs += GetActiveProcessorCount (i);
			}
		}
#endif
	}
}

static
inline
void
ep_rt_init_finish (void)
{
	STATIC_CONTRACT_NOTHROW;
}

static
inline
void
ep_rt_shutdown (void)
{
	STATIC_CONTRACT_NOTHROW;
	diagnostics_command_line_lazy_clean ();
}

static
inline
bool
ep_rt_config_aquire (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ep_rt_lock_aquire (ep_rt_coreclr_config_lock_get ());
}

static
inline
bool
ep_rt_config_release (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ep_rt_lock_release (ep_rt_coreclr_config_lock_get ());
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_config_requires_lock_held (void)
{
	STATIC_CONTRACT_NOTHROW;
	ep_rt_lock_requires_lock_held (ep_rt_coreclr_config_lock_get ());
}

static
inline
void
ep_rt_config_requires_lock_not_held (void)
{
	STATIC_CONTRACT_NOTHROW;
	ep_rt_lock_requires_lock_not_held (ep_rt_coreclr_config_lock_get ());
}
#endif

static
inline
bool
ep_rt_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	STATIC_CONTRACT_NOTHROW;
	extern bool ep_rt_coreclr_walk_managed_stack_for_thread (ep_rt_thread_handle_t thread, EventPipeStackContents *stack_contents);
	return ep_rt_coreclr_walk_managed_stack_for_thread (thread, stack_contents);
}

static
inline
bool
ep_rt_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	const ep_char8_t *assembly_name = method->GetLoaderModule ()->GetAssembly ()->GetSimpleName ();
	if (!assembly_name)
		return false;

	size_t assembly_name_len = strlen (assembly_name) + 1;
	size_t to_copy = assembly_name_len < name_len ? assembly_name_len : name_len;
	memcpy (name, assembly_name, to_copy);
	name [to_copy - 1] = 0;

	return true;
}

static
bool
ep_rt_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	bool result = true;
	EX_TRY
	{
		SString method_name;
		StackScratchBuffer conversion;

		TypeString::AppendMethodInternal (method_name, method, TypeString::FormatNamespace | TypeString::FormatSignature);
		const ep_char8_t *method_name_utf8 = method_name.GetUTF8 (conversion);
		if (method_name_utf8) {
			size_t method_name_utf8_len = strlen (method_name_utf8) + 1;
			size_t to_copy = method_name_utf8_len < name_len ? method_name_utf8_len : name_len;
			memcpy (name, method_name_utf8, to_copy);
			name [to_copy - 1] = 0;
		} else {
			result = false;
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
inline
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	STATIC_CONTRACT_NOTHROW;

	if (!ep_rt_utf8_string_compare (ep_config_get_rundown_provider_name_utf8 (), ep_provider_config_get_provider_name (provider_config))) {
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.Level = ep_provider_config_get_logging_level (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.EnabledKeywordsBitmask = ep_provider_config_get_keywords (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled = true;
	}
}

// This function is auto-generated from /src/scripts/genEventPipe.py
#ifdef TARGET_UNIX
extern "C" void InitProvidersAndEvents ();
#else
extern void InitProvidersAndEvents ();
#endif

static
void
ep_rt_init_providers_and_events (void)
{
	STATIC_CONTRACT_NOTHROW;

	EX_TRY
	{
		InitProvidersAndEvents ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

static
inline
bool
ep_rt_providers_validate_all_disabled (void)
{
	STATIC_CONTRACT_NOTHROW;

	return (!MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled);
}

static
inline
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data)
{
	STATIC_CONTRACT_NOTHROW;

#if defined(HOST_OSX) && defined(HOST_ARM64)
	auto jitWriteEnableHolder = PAL_JITWriteEnable(false);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)
}

static
void
ep_rt_provider_invoke_callback (
	EventPipeCallback callback_func,
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (callback_func != NULL);

	EX_TRY
	{
		(*callback_func)(
			source_id,
			is_enabled,
			level,
			match_any_keywords,
			match_all_keywords,
			filter_data,
			callback_data);
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

/*
 * EventPipeBuffer.
 */

EP_RT_DEFINE_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DEFINE_LOCAL_ARRAY (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)
EP_RT_DEFINE_ARRAY_ITERATOR (buffer_array, ep_rt_buffer_array_t, ep_rt_buffer_array_iterator_t, EventPipeBuffer *)

#undef EP_RT_DECLARE_LOCAL_BUFFER_ARRAY
#define EP_RT_DECLARE_LOCAL_BUFFER_ARRAY(var_name) \
	EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ep_rt_buffer_array_t)

/*
 * EventPipeBufferList.
 */

EP_RT_DEFINE_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DEFINE_LOCAL_ARRAY (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)
EP_RT_DEFINE_ARRAY_ITERATOR (buffer_list_array, ep_rt_buffer_list_array_t, ep_rt_buffer_list_array_iterator_t, EventPipeBufferList *)

#undef EP_RT_DECLARE_LOCAL_BUFFER_LIST_ARRAY
#define EP_RT_DECLARE_LOCAL_BUFFER_LIST_ARRAY(var_name) \
	EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ep_rt_buffer_list_array_t)

/*
 * EventPipeEvent.
 */

EP_RT_DEFINE_LIST (event_list, ep_rt_event_list_t, EventPipeEvent *)
EP_RT_DEFINE_LIST_ITERATOR (event_list, ep_rt_event_list_t, ep_rt_event_list_iterator_t, EventPipeEvent *)

/*
 * EventPipeFile.
 */

EP_RT_DEFINE_HASH_MAP_REMOVE(metadata_labels_hash, ep_rt_metadata_labels_hash_map_t, EventPipeEvent *, uint32_t)
EP_RT_DEFINE_HASH_MAP(stack_hash, ep_rt_stack_hash_map_t, StackHashKey *, StackHashEntry *)
EP_RT_DEFINE_HASH_MAP_ITERATOR(stack_hash, ep_rt_stack_hash_map_t, ep_rt_stack_hash_map_iterator_t, StackHashKey *, StackHashEntry *)

/*
 * EventPipeProvider.
 */

EP_RT_DEFINE_LIST (provider_list, ep_rt_provider_list_t, EventPipeProvider *)
EP_RT_DEFINE_LIST_ITERATOR (provider_list, ep_rt_provider_list_t, ep_rt_provider_list_iterator_t, EventPipeProvider *)

EP_RT_DEFINE_QUEUE (provider_callback_data_queue, ep_rt_provider_callback_data_queue_t, EventPipeProviderCallbackData *)

static
EventPipeProvider *
ep_rt_provider_list_find_by_name (
	const ep_rt_provider_list_t *list,
	const ep_char8_t *name)
{
	STATIC_CONTRACT_NOTHROW;

	// The provider list should be non-NULL, but can be NULL on shutdown.
	if (list) {
		SList<SListElem<EventPipeProvider *>> *provider_list = list->list;
		SListElem<EventPipeProvider *> *element = provider_list->GetHead ();
		while (element) {
			EventPipeProvider *provider = element->GetValue ();
			if (ep_rt_utf8_string_compare (ep_provider_get_provider_name (element->GetValue ()), name) == 0)
				return provider;

			element = provider_list->GetNext (element);
		}
	}

	return NULL;
}

/*
 * EventPipeProviderConfiguration.
 */

EP_RT_DEFINE_ARRAY (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)
EP_RT_DEFINE_ARRAY_ITERATOR (provider_config_array, ep_rt_provider_config_array_t, ep_rt_provider_config_array_iterator_t, EventPipeProviderConfiguration)

static
inline
bool
ep_rt_config_value_get_enable (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EnableEventPipe) != 0;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_config (void)
{
	STATIC_CONTRACT_NOTHROW;
	CLRConfigStringHolder value(CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeConfig));
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<ep_char16_t *>(value.GetValue ()), -1);
}

static
inline
ep_char8_t *
ep_rt_config_value_get_output_path (void)
{
	STATIC_CONTRACT_NOTHROW;
	CLRConfigStringHolder value(CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeOutputPath));
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<ep_char16_t *>(value.GetValue ()), -1);
}

static
inline
uint32_t
ep_rt_config_value_get_circular_mb (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeCircularMB);
}

static
inline
bool
ep_rt_config_value_get_use_portable_thread_pool (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ThreadpoolMgr::UsePortableThreadPool ();
}

/*
 * EventPipeSampleProfiler.
 */

static
inline
void
ep_rt_sample_profiler_write_sampling_event_for_threads (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *sampling_event)
{
	STATIC_CONTRACT_NOTHROW;

	extern void ep_rt_coreclr_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);
	ep_rt_coreclr_sample_profiler_write_sampling_event_for_threads (sampling_thread, sampling_event);
}

static
inline
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider)
{
	STATIC_CONTRACT_NOTHROW;

#ifndef DACCESS_COMPILE
		// Let the profiler know the provider has been created so it can register if it wants to
		BEGIN_PIN_PROFILER (CORProfilerIsMonitoringEventPipe ());
		g_profControlBlock.pProfInterface->EventPipeProviderCreated (provider);
		END_PIN_PROFILER ();
#endif // DACCESS_COMPILE
}

/*
 * EventPipeSessionProvider.
 */

EP_RT_DEFINE_LIST (session_provider_list, ep_rt_session_provider_list_t, EventPipeSessionProvider *)
EP_RT_DEFINE_LIST_ITERATOR (session_provider_list, ep_rt_session_provider_list_t, ep_rt_session_provider_list_iterator_t, EventPipeSessionProvider *)

static
EventPipeSessionProvider *
ep_rt_session_provider_list_find_by_name (
	const ep_rt_session_provider_list_t *list,
	const ep_char8_t *name)
{
	STATIC_CONTRACT_NOTHROW;

	SList<SListElem<EventPipeSessionProvider *>> *provider_list = list->list;
	EventPipeSessionProvider *session_provider = NULL;
	SListElem<EventPipeSessionProvider *> *element = provider_list->GetHead ();
	while (element) {
		EventPipeSessionProvider *candidate = element->GetValue ();
		if (ep_rt_utf8_string_compare (ep_session_provider_get_provider_name (candidate), name) == 0) {
			session_provider = candidate;
			break;
		}
		element = provider_list->GetNext (element);
	}

	return session_provider;
}

/*
 * EventPipeSequencePoint.
 */

EP_RT_DEFINE_LIST (sequence_point_list, ep_rt_sequence_point_list_t, EventPipeSequencePoint *)
EP_RT_DEFINE_LIST_ITERATOR (sequence_point_list, ep_rt_sequence_point_list_t, ep_rt_sequence_point_list_iterator_t, EventPipeSequencePoint *)

/*
 * EventPipeThread.
 */

EP_RT_DEFINE_LIST (thread_list, ep_rt_thread_list_t, EventPipeThread *)
EP_RT_DEFINE_LIST_ITERATOR (thread_list, ep_rt_thread_list_t, ep_rt_thread_list_iterator_t, EventPipeThread *)

EP_RT_DEFINE_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DEFINE_LOCAL_ARRAY (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)
EP_RT_DEFINE_ARRAY_ITERATOR (thread_array, ep_rt_thread_array_t, ep_rt_thread_array_iterator_t, EventPipeThread *)

#undef EP_RT_DECLARE_LOCAL_THREAD_ARRAY
#define EP_RT_DECLARE_LOCAL_THREAD_ARRAY(var_name) \
	EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ep_rt_thread_array_t)

/*
 * EventPipeThreadSessionState.
 */

EP_RT_DEFINE_LIST (thread_session_state_list, ep_rt_thread_session_state_list_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_LIST_ITERATOR (thread_session_state_list, ep_rt_thread_session_state_list_t, ep_rt_thread_session_state_list_iterator_t, EventPipeThreadSessionState *)

EP_RT_DEFINE_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_LOCAL_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_ARRAY_ITERATOR (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)

#undef EP_RT_DECLARE_LOCAL_THREAD_SESSION_STATE_ARRAY
#define EP_RT_DECLARE_LOCAL_THREAD_SESSION_STATE_ARRAY(var_name) \
	EP_RT_DECLARE_LOCAL_ARRAY_VARIABLE(var_name, ep_rt_thread_session_state_array_t)

/*
 * Arrays.
 */

static
inline
uint8_t *
ep_rt_byte_array_alloc (size_t len)
{
	STATIC_CONTRACT_NOTHROW;
	return new (nothrow) uint8_t [len];
}

static
inline
void
ep_rt_byte_array_free (uint8_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;

	if (ptr)
		delete [] ptr;
}

/*
 * Event.
 */

static
void
ep_rt_wait_event_alloc (
	ep_rt_wait_event_handle_t *wait_event,
	bool manual,
	bool initial)
{
	STATIC_CONTRACT_NOTHROW;

	EP_ASSERT (wait_event != NULL);
	EP_ASSERT (wait_event->event == NULL);

	wait_event->event = new (nothrow) CLREventStatic ();
	if (wait_event->event) {
		EX_TRY
		{
			if (manual)
				wait_event->event->CreateManualEvent (initial);
			else
				wait_event->event->CreateAutoEvent (initial);
		}
		EX_CATCH {}
		EX_END_CATCH(SwallowAllExceptions);
	}
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;

	if (wait_event != NULL && wait_event->event != NULL) {
		wait_event->event->CloseEvent ();
		delete wait_event->event;
		wait_event->event = NULL;
	}
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	return wait_event->event->Set ();
}

static
int32_t
ep_rt_wait_event_wait (
	ep_rt_wait_event_handle_t *wait_event,
	uint32_t timeout,
	bool alertable)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	int32_t result;
	EX_TRY
	{
		result = wait_event->event->Wait (timeout, alertable);
	}
	EX_CATCH
	{
		result = -1;
	}
	EX_END_CATCH(SwallowAllExceptions);
	return result;
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	return reinterpret_cast<EventPipeWaitHandle>(wait_event->event->GetHandleUNHOSTED ());
}

static
inline
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;

	if (wait_event == NULL || wait_event->event == NULL)
		return false;

	return wait_event->event->IsValid ();
}

/*
 * Misc.
 */

static
inline
int
ep_rt_get_last_error (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ::GetLastError ();
}

static
inline
bool
ep_rt_process_detach (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fProcessDetach;
}

static
inline
bool
ep_rt_process_shutdown (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fEEShutDown;
}

static
inline
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	CoCreateGuid (reinterpret_cast<GUID *>(activity_id));
}

static
inline
bool
ep_rt_is_running (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fEEStarted;
}

static
inline
void
ep_rt_execute_rundown (void)
{
	STATIC_CONTRACT_NOTHROW;

	if (CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeRundown) > 0) {
		// Ask the runtime to emit rundown events.
		if (g_fEEStarted && !g_fEEShutDown)
			ETW::EnumerationLog::EndRundown ();
	}
}

/*
 * Objects.
 */

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (new (nothrow) obj_type())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) (new (nothrow) obj_type [size]())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_free
#define ep_rt_object_array_free(obj_ptr) do { if (obj_ptr) delete [] obj_ptr; } while(0)

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_free
#define ep_rt_object_free(obj_ptr) do { if (obj_ptr) delete obj_ptr; } while(0)

/*
 * PAL.
 */

typedef struct _rt_coreclr_thread_params_internal_t {
	ep_rt_thread_params_t thread_params;
} rt_coreclr_thread_params_internal_t;

#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static ep_rt_thread_start_func_return_t WINAPI name (LPVOID data)

EP_RT_DEFINE_THREAD_FUNC (ep_rt_thread_coreclr_start_func)
{
	STATIC_CONTRACT_NOTHROW;

	rt_coreclr_thread_params_internal_t *thread_params = reinterpret_cast<rt_coreclr_thread_params_internal_t *>(data);
	DWORD result = thread_params->thread_params.thread_func (thread_params);
	if (thread_params->thread_params.thread)
		::DestroyThread (thread_params->thread_params.thread);
	delete thread_params;
	return result;
}

static
bool
ep_rt_thread_create (
	void *thread_func,
	void *params,
	EventPipeThreadType thread_type,
	void *id)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (thread_func != NULL);

	bool result = false;

	EX_TRY
	{
		rt_coreclr_thread_params_internal_t *thread_params = new (nothrow) rt_coreclr_thread_params_internal_t ();
		if (thread_params) {
			thread_params->thread_params.thread_type = thread_type;
			if (thread_type == EP_THREAD_TYPE_SESSION || thread_type == EP_THREAD_TYPE_SAMPLING) {
				thread_params->thread_params.thread = SetupUnstartedThread ();
				thread_params->thread_params.thread_func = reinterpret_cast<LPTHREAD_START_ROUTINE>(thread_func);
				thread_params->thread_params.thread_params = params;
				if (thread_params->thread_params.thread->CreateNewThread (0, ep_rt_thread_coreclr_start_func, thread_params)) {
					thread_params->thread_params.thread->SetBackground (TRUE);
					thread_params->thread_params.thread->StartThread ();
					if (id)
						*reinterpret_cast<DWORD *>(id) = thread_params->thread_params.thread->GetThreadId ();
					result = true;
				}
			} else if (thread_type == EP_THREAD_TYPE_SERVER) {
				DWORD thread_id = 0;
				HANDLE server_thread = ::CreateThread (nullptr, 0, reinterpret_cast<LPTHREAD_START_ROUTINE>(thread_func), nullptr, 0, &thread_id);
				if (server_thread != NULL) {
					::CloseHandle (server_thread);
					if (id)
						*reinterpret_cast<DWORD *>(id) = thread_id;
					result = true;
				}
			}
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
inline
void
ep_rt_thread_sleep (uint64_t ns)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
	PAL_nanosleep (ns);
#else  //TARGET_UNIX
	const uint32_t NUM_NANOSECONDS_IN_1_MS = 1000000;
	ClrSleepEx (static_cast<DWORD>(ns / NUM_NANOSECONDS_IN_1_MS), FALSE);
#endif //TARGET_UNIX
}

static
inline
uint32_t
ep_rt_current_process_get_id (void)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(GetCurrentProcessId ());
}

static
inline
uint32_t
ep_rt_current_processor_get_number (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifndef TARGET_UNIX
	extern uint32_t *_ep_rt_coreclr_proc_group_offsets;
	if (_ep_rt_coreclr_proc_group_offsets) {
		PROCESSOR_NUMBER proc;
		GetCurrentProcessorNumberEx (&proc);
		return _ep_rt_coreclr_proc_group_offsets [proc.Group] + proc.Number;
	}
#endif
	return 0xFFFFFFFF;
}

static
inline
uint32_t
ep_rt_processors_get_count (void)
{
	STATIC_CONTRACT_NOTHROW;

	SYSTEM_INFO sys_info = {};
	GetSystemInfo (&sys_info);
	return static_cast<uint32_t>(sys_info.dwNumberOfProcessors);
}

static
inline
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
	return static_cast<ep_rt_thread_id_t>(::PAL_GetCurrentOSThreadId ());
#else
	return static_cast<ep_rt_thread_id_t>(::GetCurrentThreadId ());
#endif
}

static
inline
int64_t
ep_rt_perf_counter_query (void)
{
	STATIC_CONTRACT_NOTHROW;

	LARGE_INTEGER value;
	if (QueryPerformanceCounter (&value))
		return static_cast<int64_t>(value.QuadPart);
	else
		return 0;
}

static
inline
int64_t
ep_rt_perf_frequency_query (void)
{
	STATIC_CONTRACT_NOTHROW;

	LARGE_INTEGER value;
	if (QueryPerformanceFrequency (&value))
		return static_cast<int64_t>(value.QuadPart);
	else
		return 0;
}

static
inline
void
ep_rt_system_time_get (EventPipeSystemTime *system_time)
{
	STATIC_CONTRACT_NOTHROW;

	SYSTEMTIME value;
	GetSystemTime (&value);

	EP_ASSERT(system_time != NULL);
	ep_system_time_set (
		system_time,
		value.wYear,
		value.wMonth,
		value.wDayOfWeek,
		value.wDay,
		value.wHour,
		value.wMinute,
		value.wSecond,
		value.wMilliseconds);
}

static
inline
int64_t
ep_rt_system_timestamp_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	FILETIME value;
	GetSystemTimeAsFileTime (&value);
	return static_cast<int64_t>(((static_cast<uint64_t>(value.dwHighDateTime)) << 32) | static_cast<uint64_t>(value.dwLowDateTime));
}

static
inline
int32_t
ep_rt_system_get_alloc_granularity (void)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(g_SystemInfo.dwAllocationGranularity);
}

static
inline
const ep_char8_t *
ep_rt_os_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return NULL;
}

static
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
	STATIC_CONTRACT_NOTHROW;

	ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16_string (path, -1);
	ep_return_null_if_nok (path_utf16 != NULL);

	CFileStream *file_stream = new (nothrow) CFileStream ();
	if (file_stream && FAILED (file_stream->OpenForWrite (reinterpret_cast<LPWSTR>(path_utf16)))) {
		delete file_stream;
		file_stream = NULL;
	}

	ep_rt_utf16_string_free (path_utf16);
	return static_cast<ep_rt_file_handle_t>(file_stream);
}

static
inline
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
	STATIC_CONTRACT_NOTHROW;

	// Closed in destructor.
	if (file_handle)
		delete file_handle;
	return true;
}

static
inline
bool
ep_rt_file_write (
	ep_rt_file_handle_t file_handle,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (buffer != NULL);

	ep_return_false_if_nok (file_handle != NULL);

	ULONG out_count;
	HRESULT result = reinterpret_cast<CFileStream *>(file_handle)->Write (buffer, bytes_to_write, &out_count);
	*bytes_written = static_cast<uint32_t>(out_count);
	return result == S_OK;
}

static
inline
uint8_t *
ep_rt_valloc0 (size_t buffer_size)
{
	STATIC_CONTRACT_NOTHROW;
	return reinterpret_cast<uint8_t *>(ClrVirtualAlloc (NULL, buffer_size, MEM_COMMIT, PAGE_READWRITE));
}

static
inline
void
ep_rt_vfree (
	uint8_t *buffer,
	size_t buffer_size)
{
	STATIC_CONTRACT_NOTHROW;

	if (buffer)
		ClrVirtualFree (buffer, 0, MEM_RELEASE);
}

static
inline
uint32_t
ep_rt_temp_path_get (
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return 0;
}

EP_RT_DEFINE_ARRAY (env_array_utf16, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)
EP_RT_DEFINE_ARRAY_ITERATOR (env_array_utf16, ep_rt_env_array_utf16_t, ep_rt_env_array_utf16_iterator_t, ep_char16_t *)

static
void
ep_rt_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (env_array != NULL);

	LPWSTR envs = GetEnvironmentStringsW ();
	if (envs) {
		LPWSTR next = envs;
		while (*next) {
			ep_rt_env_array_utf16_append (env_array, ep_rt_utf16_string_dup (reinterpret_cast<const ep_char16_t *>(next)));
			next += ep_rt_utf16_string_len (reinterpret_cast<const ep_char16_t *>(next)) + 1;
		}
		FreeEnvironmentStringsW (envs);
	}
}

/*
* Lock.
*/

static
bool
ep_rt_lock_aquire (ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;

	bool result = true;
	EX_TRY
	{
		if (lock) {
			CrstBase::CrstHolderWithState holder (lock->lock);
			holder.SuppressRelease ();
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;

	bool result = true;
	EX_TRY
	{
		if (lock) {
			CrstBase::UnsafeCrstInverseHolder holder (lock->lock);
			holder.SuppressRelease ();
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}

static
inline
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (lock->lock == NULL || !((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}
#endif

/*
* SpinLock.
*/

static
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;

	EX_TRY
	{
		spin_lock->lock = new (nothrow) SpinLock ();
		spin_lock->lock->Init (LOCK_TYPE_DEFAULT);
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

static
inline
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;

	if (spin_lock && spin_lock->lock) {
		delete spin_lock->lock;
		spin_lock->lock = NULL;
	}
}

static
inline
bool
ep_rt_spin_lock_aquire (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));

	SpinLock::AcquireLock (spin_lock->lock);
	return true;
}

static
inline
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));

	SpinLock::ReleaseLock (spin_lock->lock);
	return true;
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));
	EP_ASSERT (spin_lock->lock->OwnedByCurrentThread ());
}

static
inline
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (spin_lock->lock == NULL || !spin_lock->lock->OwnedByCurrentThread ());
}
#endif

static
inline
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	return (spin_lock != NULL && spin_lock->lock != NULL);
}

/*
 * String.
 */

static
inline
int
ep_rt_utf8_string_compare (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str1 != NULL && str2 != NULL);

	return strcmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
int
ep_rt_utf8_string_compare_ignore_case (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str1 != NULL && str2 != NULL);

	return _stricmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str == NULL)
		return true;

	while (*str) {
		if (!isspace (*str))
			return false;
		str++;
	}
	return true;
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	return _strdup (str);
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup_range (const ep_char8_t *str, const ep_char8_t *strEnd)
{
	ptrdiff_t byte_len = strEnd - str;
	ep_char8_t *buffer = reinterpret_cast<ep_char8_t *>(malloc(byte_len + 1));
	if (buffer != NULL)
	{
		memcpy (buffer, str, byte_len);
		buffer [byte_len] = '\0';
	}
	return buffer;
}

static
inline
ep_char8_t *
ep_rt_utf8_string_strtok (
	ep_char8_t *str,
	const ep_char8_t *delimiter,
	ep_char8_t **context)
{
	STATIC_CONTRACT_NOTHROW;
	return strtok_s (str, delimiter, context);
}

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf( \
	str, \
	str_len, \
	format, ...) \
sprintf_s (reinterpret_cast<char *>(str), static_cast<size_t>(str_len), reinterpret_cast<const char *>(format), __VA_ARGS__)

static
inline
bool
ep_rt_utf8_string_replace (
	ep_char8_t **str,
	const ep_char8_t *strSearch,
	const ep_char8_t *strReplacement
)
{
	STATIC_CONTRACT_NOTHROW;
	if ((*str) == NULL)
		return false;

	ep_char8_t* strFound = strstr(*str, strSearch);
	if (strFound != NULL)
	{
		size_t strSearchLen = strlen(strSearch);
		size_t newStrSize = strlen(*str) + strlen(strReplacement) - strSearchLen + 1; 
		ep_char8_t *newStr =  reinterpret_cast<ep_char8_t *>(malloc(newStrSize));
		if (newStr == NULL)
		{
			*str = NULL;
			return false;
		}
		ep_rt_utf8_string_snprintf(newStr, newStrSize, "%.*s%s%s", (int)(strFound - (*str)), *str, strReplacement, strFound + strSearchLen);
		ep_rt_utf8_string_free(*str);
		*str = newStr;
		return true;
	}
	return false;
}

static
ep_char16_t *
ep_rt_utf8_to_utf16_string (
	const ep_char8_t *str,
	size_t len)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	COUNT_T len_utf16 = WszMultiByteToWideChar (CP_UTF8, 0, str, static_cast<int>(len), 0, 0);
	if (len_utf16 == 0)
		return NULL;

	if (static_cast<int>(len) != -1)
		len_utf16 += 1;

	ep_char16_t *str_utf16 = reinterpret_cast<ep_char16_t *>(malloc (len_utf16 * sizeof (ep_char16_t)));
	if (!str_utf16)
		return NULL;

	len_utf16 = WszMultiByteToWideChar (CP_UTF8, 0, str, static_cast<int>(len), reinterpret_cast<LPWSTR>(str_utf16), len_utf16);
	if (len_utf16 == 0) {
		free (str_utf16);
		return NULL;
	}

	str_utf16 [len_utf16 - 1] = 0;
	return str_utf16;
}

static
inline
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	size_t str_size = (ep_rt_utf16_string_len (str) + 1) * sizeof (ep_char16_t);
	ep_char16_t *str_dup = reinterpret_cast<ep_char16_t *>(malloc (str_size));
	if (str_dup)
		memcpy (str_dup, str, str_size);
	return str_dup;
}

static
inline
void
ep_rt_utf8_string_free (ep_char8_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str)
		free (str);
}

static
inline
size_t
ep_rt_utf16_string_len (const ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str != NULL);

	return wcslen (reinterpret_cast<LPCWSTR>(str));
}

static
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	COUNT_T size_utf8 = WszWideCharToMultiByte (CP_UTF8, 0, reinterpret_cast<LPCWSTR>(str), static_cast<int>(len), NULL, 0, NULL, NULL);
	if (size_utf8 == 0)
		return NULL;

	if (static_cast<int>(len) != -1)
		size_utf8 += 1;

	ep_char8_t *str_utf8 = reinterpret_cast<ep_char8_t *>(malloc (size_utf8));
	if (!str_utf8)
		return NULL;

	size_utf8 = WszWideCharToMultiByte (CP_UTF8, 0, reinterpret_cast<LPCWSTR>(str), static_cast<int>(len), reinterpret_cast<LPSTR>(str_utf8), size_utf8, NULL, NULL);
	if (size_utf8 == 0) {
		free (str_utf8);
		return NULL;
	}

	str_utf8 [size_utf8 - 1] = 0;
	return str_utf8;
}

static
inline
void
ep_rt_utf16_string_free (ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str)
		free (str);
}

static
inline
const ep_char8_t *
ep_rt_managed_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return NULL;
}

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	diagnostics_command_line_lazy_init ();
	return *diagnostics_command_line_get_ref ();
}

/*
 * Thread.
 */

static
inline
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
	STATIC_CONTRACT_NOTHROW;
	EventPipeThreadHolder *instance = ep_thread_holder_alloc (ep_thread_alloc());
	if (instance)
		ep_thread_register (ep_thread_holder_get_thread (instance));
	return instance;
}

static
inline
void
thread_holder_free_func (EventPipeThreadHolder * thread_holder)
{
	STATIC_CONTRACT_NOTHROW;
	if (thread_holder) {
		ep_thread_unregister (ep_thread_holder_get_thread (thread_holder));
		ep_thread_holder_free (thread_holder);
	}
}

class EventPipeCoreCLRThreadHolderTLS {
public:
	EventPipeCoreCLRThreadHolderTLS ()
	{
		STATIC_CONTRACT_NOTHROW;
	}

	~EventPipeCoreCLRThreadHolderTLS ()
	{
		STATIC_CONTRACT_NOTHROW;

		if (m_threadHolder) {
			thread_holder_free_func (m_threadHolder);
			m_threadHolder = NULL;
		}
	}

	static inline EventPipeThreadHolder * getThreadHolder ()
	{
		STATIC_CONTRACT_NOTHROW;
		return g_threadHolderTLS.m_threadHolder;
	}

	static inline EventPipeThreadHolder * createThreadHolder ()
	{
		STATIC_CONTRACT_NOTHROW;

		if (g_threadHolderTLS.m_threadHolder) {
			thread_holder_free_func (g_threadHolderTLS.m_threadHolder);
			g_threadHolderTLS.m_threadHolder = NULL;
		}
		g_threadHolderTLS.m_threadHolder = thread_holder_alloc_func ();
		return g_threadHolderTLS.m_threadHolder;
	}

private:
	EventPipeThreadHolder *m_threadHolder;
	static thread_local EventPipeCoreCLRThreadHolderTLS g_threadHolderTLS;
};

static
void
ep_rt_thread_setup (void)
{
	STATIC_CONTRACT_NOTHROW;

	EX_TRY
	{
		SetupThread ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

static
inline
EventPipeThread *
ep_rt_thread_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	EventPipeThreadHolder *thread_holder = EventPipeCoreCLRThreadHolderTLS::getThreadHolder ();
	return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
}

static
inline
EventPipeThread *
ep_rt_thread_get_or_create (void)
{
	STATIC_CONTRACT_NOTHROW;

	EventPipeThreadHolder *thread_holder = EventPipeCoreCLRThreadHolderTLS::getThreadHolder ();
	if (!thread_holder)
		thread_holder = EventPipeCoreCLRThreadHolderTLS::createThreadHolder ();

	return ep_thread_holder_get_thread (thread_holder);
}

static
inline
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void)
{
	STATIC_CONTRACT_NOTHROW;
	return GetThreadNULLOk ();
}

static
inline
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (thread_handle != NULL);

	return ep_rt_uint64_t_to_thread_id_t (thread_handle->GetOSThreadId64 ());
}

static
inline
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id)
{
	return static_cast<uint64_t>(thread_id);
}

static
inline
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id)
{
	return static_cast<ep_rt_thread_id_t>(thread_id);
}

static
inline
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle)
{
	STATIC_CONTRACT_NOTHROW;
	return thread_handle != NULL && thread_handle->HasStarted ();
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void)
{
	STATIC_CONTRACT_NOTHROW;
	return GetThread ();
}

static
inline
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);

	return reinterpret_cast<const uint8_t *>(activity_id_handle->GetActivityId ());
}

static
inline
void
ep_rt_thread_get_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (activity_id, ep_rt_thread_get_activity_id_cref (activity_id_handle), EP_ACTIVITY_ID_SIZE);
}

static
inline
void
ep_rt_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	activity_id_handle->SetActivityId (reinterpret_cast<LPCGUID>(activity_id));
}

#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) YIELD_WHILE(condition)

/*
 * ThreadSequenceNumberMap.
 */

EP_RT_DEFINE_HASH_MAP_REMOVE(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, EventPipeThreadSessionState *, uint32_t)
EP_RT_DEFINE_HASH_MAP_ITERATOR(thread_sequence_number_map, ep_rt_thread_sequence_number_hash_map_t, ep_rt_thread_sequence_number_hash_map_iterator_t, EventPipeThreadSessionState *, uint32_t)

/*
 * Volatile.
 */

static
inline
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<uint32_t> ((const uint32_t *)ptr);
}

static
inline
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<uint32_t> ((const uint32_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_uint32_t (
	volatile uint32_t *ptr,
	uint32_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<uint32_t> ((uint32_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint32_t_without_barrier (
	volatile uint32_t *ptr,
	uint32_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<uint32_t>((uint32_t *)ptr, value);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<uint64_t> ((const uint64_t *)ptr);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<uint64_t> ((const uint64_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_uint64_t (
	volatile uint64_t *ptr,
	uint64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<uint64_t> ((uint64_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint64_t_without_barrier (
	volatile uint64_t *ptr,
	uint64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<uint64_t> ((uint64_t *)ptr, value);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<int64_t> ((int64_t *)ptr);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<int64_t> ((int64_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_int64_t (
	volatile int64_t *ptr,
	int64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<int64_t> ((int64_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_int64_t_without_barrier (
	volatile int64_t *ptr,
	int64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<int64_t> ((int64_t *)ptr, value);
}

static
inline
void *
ep_rt_volatile_load_ptr (volatile void **ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<void *> ((void **)ptr);
}

static
inline
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<void *> ((void **)ptr);
}

static
inline
void
ep_rt_volatile_store_ptr (
	volatile void **ptr,
	void *value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<void *> ((void **)ptr, value);
}

static
inline
void
ep_rt_volatile_store_ptr_without_barrier (
	volatile void **ptr,
	void *value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<void *> ((void **)ptr, value);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_CORECLR_H__ */
