// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_T_H__
#define __DN_VECTOR_T_H__

#include "dn-vector.h"

#define DN_DEFINE_VECTOR_T_NAME(name) \
	dn_vector_ ## name ## _t

#define DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
	dn_vector_ ## name ## _result_t

#define DN_DEFINE_VECTOR_IT_T_NAME(name) \
	dn_vector_ ## name ## _it_t

#define DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, symbol) \
	dn_vector_ ## name ## _ ## symbol

#define DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, symbol) \
	dn_vector_ ## name ## _it_ ## symbol

#define DN_DEFINE_VECTOR_CUSTOM_ALLOC_PARAMS_T_SYMBOL_NAME(name) \
	dn_vector_ ## name ## _custom_alloc_params_t

#define DN_DEFINE_VECTOR_CUSTOM_ALLOC_INIT_T_SYMBOL_NAME(name) \
	dn_vector_ ## name ## _custom_init_params_t

#define DN_DEFINE_VECTOR_T(name, type) \
DN_DEFINE_VECTOR_T_STRUCT(dn_vector_ ## name, type); \
typedef enum { \
	DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, element_size) = sizeof (type), \
	DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, default_local_allocator_capacity_size) = 192, \
	DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, default_local_allocator_byte_size) = ((sizeof (type) * DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, default_local_allocator_capacity_size)) + DN_ALLOCATOR_ALIGN_SIZE (sizeof (dn_vector_t), DN_ALLOCATOR_MEM_ALIGN8) + 32) \
} DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, sizes); \
static inline void \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, advance) (DN_DEFINE_VECTOR_IT_T_NAME(name) *it, ptrdiff_t n) \
{ \
	dn_vector_it_advance ((dn_vector_it_t *)it, n); \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, next) (DN_DEFINE_VECTOR_IT_T_NAME(name) it) \
{ \
	DN_ASSERT ((it.it + 1) < UINT32_MAX); \
	it.it++; \
	return it; \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, prev) (DN_DEFINE_VECTOR_IT_T_NAME(name) it) \
{ \
	DN_ASSERT (it.it > 0); \
	it.it--; \
	return it; \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, next_n) (DN_DEFINE_VECTOR_IT_T_NAME(name) it, uint32_t n) \
{ \
	DN_ASSERT ((it.it + n) < UINT32_MAX); \
	it.it += n; \
	return it; \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, prev_n) (DN_DEFINE_VECTOR_IT_T_NAME(name) it, uint32_t n) \
{ \
	DN_ASSERT (it.it >= n); \
	it.it -= n; \
	return it; \
} \
static inline type * \
DN_DEFINE_VECTOR_IT_T_SYMBOL_NAME(name, data) (DN_DEFINE_VECTOR_IT_T_NAME(name) it, uint32_t n) \
{ \
	DN_ASSERT (it._internal._vector && it._internal._vector->data); \
	return (type *)(it._internal._vector->data + n);\
} \
static inline DN_DEFINE_VECTOR_T_NAME(name) * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_alloc) (const DN_DEFINE_VECTOR_CUSTOM_ALLOC_PARAMS_T_SYMBOL_NAME(name) *params) \
{ \
	return (DN_DEFINE_VECTOR_T_NAME(name) *)dn_vector_custom_alloc ((dn_vector_custom_alloc_params_t *)params, sizeof (type)); \
} \
static inline DN_DEFINE_VECTOR_T_NAME(name) * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, alloc) (void) \
{ \
	return (DN_DEFINE_VECTOR_T_NAME(name) *)dn_vector_alloc (sizeof (type)); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, free) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	dn_vector_free ((dn_vector_t *)vector); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_free) (DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_dispose_func_t dispose_func) \
{ \
	dn_vector_custom_free ((dn_vector_t *)vector, dispose_func); \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_init) (DN_DEFINE_VECTOR_T_NAME(name) *vector, DN_DEFINE_VECTOR_CUSTOM_ALLOC_PARAMS_T_SYMBOL_NAME (name) *params) \
{ \
	return dn_vector_custom_init ((dn_vector_t *)vector, (dn_vector_custom_alloc_params_t *)params, sizeof(type)); \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, init) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_init ((dn_vector_t *)vector, sizeof (type)); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_dispose) (DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_dispose_func_t dispose_func) \
{ \
	dn_vector_custom_dispose ((dn_vector_t *)vector, dispose_func); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, dispose) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	dn_vector_custom_dispose ((dn_vector_t *)vector, NULL); \
} \
static inline type * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, index) (const DN_DEFINE_VECTOR_T_NAME(name) *vector, uint32_t index) \
{ \
	return dn_vector_index_t ((dn_vector_t *)vector, type, index); \
}\
static inline type * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, at) (const DN_DEFINE_VECTOR_T_NAME(name) *vector, uint32_t index) \
{ \
	return dn_vector_at_t ((dn_vector_t *)vector, type, index); \
}\
static inline type * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, front) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_front_t ((dn_vector_t *)vector, type); \
}\
static inline type * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, back) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_back_t ((dn_vector_t *)vector, type); \
}\
static inline type * \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, data) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_data_t ((dn_vector_t *)vector, type); \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, begin) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	DN_ASSERT (vector); \
	DN_DEFINE_VECTOR_IT_T_NAME(name) it = { 0, { vector } }; \
	return it; \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, end) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	DN_ASSERT (vector); \
	DN_DEFINE_VECTOR_IT_T_NAME(name) it = { vector->size, { vector } }; \
	return it; \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, empty) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_empty ((dn_vector_t *)vector); \
} \
static inline uint32_t \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, size) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_size ((dn_vector_t *)vector); \
} \
static inline uint32_t \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, max_size) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_max_size ((dn_vector_t *)vector); \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, reserve) (DN_DEFINE_VECTOR_T_NAME(name) *vector, uint32_t capacity) \
{ \
	return dn_vector_reserve ((dn_vector_t *)vector, capacity); \
} \
static inline uint32_t \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, capacity) (const DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	return dn_vector_capacity ((dn_vector_t *)vector); \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, insert) (DN_DEFINE_VECTOR_IT_T_NAME(name) position, type element) \
{ \
	DN_DEFINE_VECTOR_RESULT_T_NAME(name) result; \
	result.result = _dn_vector_insert_range ((dn_vector_it_t *)&position, (const uint8_t *)&element, 1); \
	result.it = position; \
	return result; \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, insert_range) (DN_DEFINE_VECTOR_IT_T_NAME(name) position, type *elements, uint32_t element_count) \
{ \
	DN_DEFINE_VECTOR_RESULT_T_NAME(name) result; \
	result.result = _dn_vector_insert_range ((dn_vector_it_t *)&position, (const uint8_t *)(elements), element_count); \
	result.it = position; \
	return result; \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_erase) (DN_DEFINE_VECTOR_IT_T_NAME(name) position, dn_vector_dispose_func_t dispose_func) \
{ \
	DN_DEFINE_VECTOR_RESULT_T_NAME(name) result; \
	result.result = _dn_vector_erase ((dn_vector_it_t *)&position, dispose_func); \
	result.it = position; \
	return result; \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, erase) (DN_DEFINE_VECTOR_IT_T_NAME(name) position) \
{ \
	return DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_erase) (position, NULL); \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_erase_fast) (DN_DEFINE_VECTOR_IT_T_NAME(name) position, dn_vector_dispose_func_t dispose_func) \
{ \
	DN_DEFINE_VECTOR_T_NAME(name) *vector = position._internal._vector; \
	DN_ASSERT (vector && vector->size != 0); \
	DN_ASSERT (position.it != position._internal._vector->size); \
	vector->size --; \
	if (dispose_func) \
		dispose_func (vector->data + position.it); \
	vector->data [position.it] = vector->data [vector->size]; \
	if ((vector->_internal._attributes & (uint32_t)DN_VECTOR_ATTRIBUTE_MEMORY_INIT) == DN_VECTOR_ATTRIBUTE_MEMORY_INIT) \
		vector->data [vector->size] = 0; \
	DN_DEFINE_VECTOR_RESULT_T_NAME(name) result = { true, position }; \
	return result; \
} \
static inline DN_DEFINE_VECTOR_RESULT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, erase_fast) (DN_DEFINE_VECTOR_IT_T_NAME(name) position) \
{ \
	DN_DEFINE_VECTOR_T_NAME(name) *vector = position._internal._vector; \
	DN_ASSERT (vector && vector->size != 0); \
	DN_ASSERT (position.it != position._internal._vector->size); \
	vector->size --; \
	vector->data [position.it] = vector->data [vector->size]; \
	if ((vector->_internal._attributes & (uint32_t)DN_VECTOR_ATTRIBUTE_MEMORY_INIT) == DN_VECTOR_ATTRIBUTE_MEMORY_INIT) \
		vector->data [vector->size] = 0; \
	DN_DEFINE_VECTOR_RESULT_T_NAME(name) result = { true, position }; \
	return result; \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_resize) (DN_DEFINE_VECTOR_T_NAME(name) *vector, uint32_t size, dn_vector_dispose_func_t dispose_func) \
{ \
	return dn_vector_custom_resize ((dn_vector_t *)vector, size, dispose_func); \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, resize) (DN_DEFINE_VECTOR_T_NAME(name) *vector, uint32_t size) \
{ \
	return dn_vector_custom_resize ((dn_vector_t *)vector, size, NULL); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_clear) (DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_dispose_func_t dispose_func) \
{ \
	dn_vector_custom_clear ((dn_vector_t *)vector, dispose_func); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, clear) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	dn_vector_custom_clear ((dn_vector_t *)vector, NULL); \
} \
static inline bool \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, push_back) (DN_DEFINE_VECTOR_T_NAME(name) *vector, type element) \
{ \
	DN_ASSERT (vector); \
	uint64_t new_capacity = (uint64_t)vector->size + (uint64_t)1; \
	if (DN_UNLIKELY (new_capacity > (uint64_t)(vector->_internal._capacity))) { \
		if (DN_UNLIKELY (!_dn_vector_ensure_capacity ((dn_vector_t *)vector, (uint32_t)new_capacity, true))) \
			return false; \
	} \
	vector->data [vector->size] = element; \
	vector->size ++; \
	return true; \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_pop_back) (DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_dispose_func_t dispose_func) \
{ \
	DN_ASSERT (vector && vector->size != 0); \
	vector->size--; \
	if (dispose_func) \
		dispose_func (vector->data + vector->size); \
	if (((vector->_internal._attributes & (uint32_t)DN_VECTOR_ATTRIBUTE_MEMORY_INIT) == DN_VECTOR_ATTRIBUTE_MEMORY_INIT)) \
		vector->data [vector->size] = 0; \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, pop_back) (DN_DEFINE_VECTOR_T_NAME(name) *vector) \
{ \
	DN_ASSERT (vector && vector->size != 0); \
	vector->size --; \
	if (((vector->_internal._attributes & (uint32_t)DN_VECTOR_ATTRIBUTE_MEMORY_INIT) == DN_VECTOR_ATTRIBUTE_MEMORY_INIT)) \
		vector->data [vector->size] = 0; \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, for_each) (const DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_for_each_func_t for_each_func, void *user_data) \
{ \
	dn_vector_for_each ((dn_vector_t*)vector, for_each_func, user_data); \
} \
static inline void \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, sort) (DN_DEFINE_VECTOR_T_NAME(name) *vector, dn_vector_compare_func_t compare_func) \
{ \
	dn_vector_sort ((dn_vector_t*)vector, compare_func); \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, custom_find) (const DN_DEFINE_VECTOR_T_NAME(name) *vector, const type value, dn_vector_equal_func_t equal_func) \
{ \
	DN_DEFINE_VECTOR_IT_T_NAME(name) found; \
	_dn_vector_find_adapter ((dn_vector_t*)vector, (const uint8_t *)&value, equal_func, (dn_vector_it_t *)&found); \
	return found; \
} \
static inline DN_DEFINE_VECTOR_IT_T_NAME(name) \
DN_DEFINE_VECTOR_T_SYMBOL_NAME(name, find) (const DN_DEFINE_VECTOR_T_NAME(name) *vector, const type value) \
{ \
	DN_DEFINE_VECTOR_IT_T_NAME(name) found; \
	_dn_vector_find_adapter ((dn_vector_t*)vector, (const uint8_t *)&value, NULL, (dn_vector_it_t *)&found); \
	return found; \
}

#endif /* __DN_VECTOR_T_H__ */
