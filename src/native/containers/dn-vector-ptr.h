// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_PTR_H__
#define __DN_VECTOR_PTR_H__

#include "dn-vector-t.h"

DN_DEFINE_VECTOR_T (ptr, void *)

#define DN_VECTOR_PTR_DEFAULT_ALLOC_PARAMS { DN_DEFAULT_ALLOCATOR, sizeof (void *), 0, DN_VECTOR_ATTRIBUTE_INIT_MEMORY }
#define DN_VECTOR_PTR_DEFAULT_INIT_PARAMS { DN_DEFAULT_ALLOCATOR, sizeof (void *), 0, DN_VECTOR_ATTRIBUTE_INIT_MEMORY }

#define DN_VECTOR_PTR_FOREACH_BEGIN(vector,var_type,var_name) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = 0; __i_ ## var_name < (vector)->size; ++__i_ ## var_name) { \
		var_name = (var_type)*dn_vector_ptr_index (vector, __i_##var_name);

#define DN_VECTOR_PTR_FOREACH_RBEGIN(vector,var_type,var_name) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = (vector)->size; __i_ ## var_name > 0; --__i_ ## var_name) { \
		var_name = (var_type)*dn_vector_ptr_index (vector, __i_ ## var_name - 1);

#define DN_VECTOR_PTR_FOREACH_END \
		} \
	} while (0)

#endif /* __DN_VECTOR_PTR_H__ */
