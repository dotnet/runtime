// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_UMAP_T_H__
#define __DN_UMAP_T_H__

#include "dn-umap.h"

#define DN_UMAP_IT_KEY_T(key_type_name, key_type) \
_DN_STATIC_ASSERT(sizeof(key_type) <= sizeof(uintptr_t)); \
static inline key_type \
dn_umap_it_key_ ## key_type_name (dn_umap_it_t it) \
{ \
	return (key_type)(uintptr_t)(it._internal._node->key); \
}

#define DN_UMAP_IT_VALUE_T(value_type_name, value_type) \
_DN_STATIC_ASSERT(sizeof(value_type) <= sizeof(uintptr_t)); \
static inline value_type \
dn_umap_it_value_ ## value_type_name (dn_umap_it_t it) \
{ \
	return (value_type)(uintptr_t)(it._internal._node->value); \
}

#define DN_UMAP_T(key_type_name, key_type, value_type_name, value_type) \
_DN_STATIC_ASSERT(sizeof(key_type) <= sizeof(uintptr_t) && sizeof(value_type) <= sizeof(uintptr_t)); \
static inline dn_umap_result_t \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _insert (dn_umap_t *map, key_type key, value_type value) \
{ \
	return dn_umap_insert (map, ((void *)(uintptr_t)key), ((void *)(uintptr_t)value)); \
} \
static inline dn_umap_result_t \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _insert_or_assign (dn_umap_t *map, key_type key, value_type value) \
{ \
	return dn_umap_insert_or_assign (map, ((void *)(uintptr_t)key), ((void *)(uintptr_t)value)); \
} \
static inline uint32_t \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _erase_key (dn_umap_t *map, key_type key) \
{ \
	return dn_umap_erase_key (map, ((const void *)(uintptr_t)key)); \
} \
static inline bool \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _extract_key (dn_umap_t *map, key_type key, key_type *out_key, value_type *out_value) \
{ \
	return dn_umap_extract_key (map, ((const void *)(uintptr_t)key), (void **)out_key, (void **)out_value); \
} \
static inline dn_umap_it_t \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _custom_find (dn_umap_t *map, key_type key, dn_umap_equal_func_t equal_func) \
{ \
	return dn_umap_custom_find (map, ((const void *)(uintptr_t)key), equal_func); \
} \
static inline dn_umap_it_t \
dn_umap_ ## key_type_name ## _ ## value_type_name ## _find (dn_umap_t *map, key_type key) \
{ \
	return dn_umap_find (map, ((const void *)(uintptr_t)key)); \
}

DN_UMAP_IT_KEY_T (ptr, void *)

DN_UMAP_IT_VALUE_T (bool, bool)
DN_UMAP_IT_VALUE_T (int8_t, int8_t)
DN_UMAP_IT_VALUE_T (uint8_t, uint8_t)
DN_UMAP_IT_VALUE_T (int16_t, int16_t)
DN_UMAP_IT_VALUE_T (uint16_t, uint16_t)
DN_UMAP_IT_VALUE_T (int32_t, int32_t)
DN_UMAP_IT_VALUE_T (uint32_t, uint32_t)

DN_UMAP_T (ptr, void *, bool, bool)
DN_UMAP_T (ptr, void *, int8, int8_t)
DN_UMAP_T (ptr, void *, uint8, uint8_t)
DN_UMAP_T (ptr, void *, int16, int16_t)
DN_UMAP_T (ptr, void *, uint16, uint16_t)
DN_UMAP_T (ptr, void *, int32, int32_t)
DN_UMAP_T (ptr, void *, uint32, uint32_t)

#endif /* __DN_UMAP_T_H__ */
