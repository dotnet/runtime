// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_PTR_H__
#define __DN_VECTOR_PTR_H__

#include "dn-vector-t.h"

DN_DEFINE_VECTOR_T (ptr, void *)

#define DN_VECTOR_PTR_FOREACH_BEGIN(var_type, var_name, vector) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = 0; __i_ ## var_name < (vector)->size; ++__i_ ## var_name) { \
		var_name = (var_type)*dn_vector_ptr_index (vector, __i_##var_name);

#define DN_VECTOR_PTR_FOREACH_RBEGIN(var_type, var_name, vector) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = (vector)->size; __i_ ## var_name > 0; --__i_ ## var_name) { \
		var_name = (var_type)*dn_vector_ptr_index (vector, __i_ ## var_name - 1);

#define DN_VECTOR_PTR_FOREACH_END \
		} \
	} while (0)

#define dn_vector_ptr_index_t(vector, type, index) \
	(type*)dn_vector_ptr_index (vector,index)

#define dn_vector_ptr_at_t(vector, type, index) \
	(type*)dn_vector_ptr_at (vector, index)

#define dn_vector_ptr_front_t(vector, type) \
	(type*)dn_vector_ptr_front (vector)

#define dn_vector_ptr_back_t(vector, type) \
	(type*)dn_vector_ptr_back (vector)

#define dn_vector_ptr_data_t(vector, type) \
	(type*)dn_vector_btr_data (vector)

#endif /* __DN_VECTOR_PTR_H__ */
