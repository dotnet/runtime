// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_TYPES_H__
#define __DN_VECTOR_TYPES_H__

#include "dn-utils.h"
#include "dn-allocator.h"

typedef int32_t (DN_CALLBACK_CALLTYPE *dn_vector_compare_func_t) (const void *a, const void *b);
typedef bool (DN_CALLBACK_CALLTYPE *dn_vector_equal_func_t) (const void *a, const void *b);
typedef void (DN_CALLBACK_CALLTYPE *dn_vector_for_each_func_t) (void *data, void *user_data);
typedef void (DN_CALLBACK_CALLTYPE *dn_vector_dispose_func_t) (void *data);

#define DN_DEFINE_VECTOR_T_STRUCT(name, type) \
typedef struct _ ## name ## _t name ## _t; \
struct _ ## name ## _t { \
	type* data; \
	uint32_t size; \
	struct { \
		uint32_t _element_size; \
		uint32_t _capacity; \
		uint32_t _attributes; \
		dn_allocator_t *_allocator; \
	} _internal; \
}; \
typedef struct _ ## name ## _it_t name ## _it_t; \
struct _ ## name ## _it_t { \
	uint32_t it; \
	struct { \
		name ## _t *_vector; \
	} _internal; \
}; \
typedef struct _ ## name ## _result_t name ## _result_t; \
struct _ ## name ## _result_t { \
	bool result; \
	name ## _it_t it; \
}; \
typedef struct _ ## name ## _custom_params_t name ## _custom_alloc_params_t; \
typedef struct _ ## name ## _custom_params_t name ## _custom_init_params_t; \
struct _ ## name ## _custom_params_t { \
	dn_allocator_t *allocator; \
	uint32_t capacity; \
	uint32_t attributes; \
};

DN_DEFINE_VECTOR_T_STRUCT (dn_vector, uint8_t);

typedef enum {
	DN_VECTOR_ATTRIBUTE_MEMORY_INIT = 0x1
} dn_vector_attribute;

#endif /* __DN_VECTOR_TYPES_H__ */
