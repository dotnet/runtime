// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Gluing macro expansions together requires nested macro invocation :/
#ifndef DN_SIMDHASH_GLUE
#define DN_SIMDHASH_GLUE(a,b) a ## b
#endif
#ifndef DN_SIMDHASH_GLUE_3
#define DN_SIMDHASH_GLUE_3_INNER(a, b, c) a ## b ## c
#define DN_SIMDHASH_GLUE_3(a, b, c) DN_SIMDHASH_GLUE_3_INNER(a, b, c)
#endif

#ifndef DN_SIMDHASH_ACCESSOR_SUFFIX
#define DN_SIMDHASH_ACCESSOR_SUFFIX
#endif

// We generate unique names for each specialization so that they will be easy to distinguish
//  when debugging, profiling, or disassembling. Otherwise they would have linker-assigned names
#define DN_SIMDHASH_T_NAME(t) DN_SIMDHASH_GLUE(t,_t)
#define DN_SIMDHASH_T_PTR(t) DN_SIMDHASH_GLUE(t,_t *)
#define DN_SIMDHASH_T_VTABLE(t) DN_SIMDHASH_GLUE(t,_vtable)
#define DN_SIMDHASH_T_META(t) DN_SIMDHASH_GLUE(t,_meta)
#define DN_SIMDHASH_SCAN_BUCKET_INTERNAL(t) DN_SIMDHASH_GLUE(t,_scan_bucket_internal)
#define DN_SIMDHASH_FIND_VALUE_INTERNAL(t) DN_SIMDHASH_GLUE(t,_find_value_internal)
#define DN_SIMDHASH_TRY_INSERT_INTERNAL(t) DN_SIMDHASH_GLUE(t,_try_insert_internal)
#define DN_SIMDHASH_REHASH_INTERNAL(t) DN_SIMDHASH_GLUE(t,_rehash_internal)
#define DN_SIMDHASH_NEW(t) DN_SIMDHASH_GLUE(t,_new)
#define DN_SIMDHASH_TRY_ADD(t) DN_SIMDHASH_GLUE_3(t,_try_add,DN_SIMDHASH_ACCESSOR_SUFFIX)
#define DN_SIMDHASH_TRY_ADD_WITH_HASH(t) DN_SIMDHASH_GLUE_3(t,_try_add_with_hash,DN_SIMDHASH_ACCESSOR_SUFFIX)
#define DN_SIMDHASH_TRY_GET_VALUE(t) DN_SIMDHASH_GLUE_3(t,_try_get_value,DN_SIMDHASH_ACCESSOR_SUFFIX)
#define DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(t) DN_SIMDHASH_GLUE_3(t,_try_get_value_with_hash,DN_SIMDHASH_ACCESSOR_SUFFIX)
#define DN_SIMDHASH_TRY_REMOVE(t) DN_SIMDHASH_GLUE_3(t,_try_remove,DN_SIMDHASH_ACCESSOR_SUFFIX)
#define DN_SIMDHASH_TRY_REMOVE_WITH_HASH(t) DN_SIMDHASH_GLUE_3(t,_try_remove_with_hash,DN_SIMDHASH_ACCESSOR_SUFFIX)

// Declare a specific alias so intellisense gives more helpful info
typedef dn_simdhash_t DN_SIMDHASH_T_NAME(DN_SIMDHASH_T);

DN_SIMDHASH_T_PTR(DN_SIMDHASH_T)
DN_SIMDHASH_NEW(DN_SIMDHASH_T) (uint32_t capacity, dn_allocator_t *allocator);

uint8_t
DN_SIMDHASH_TRY_ADD(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T value);

uint8_t
DN_SIMDHASH_TRY_ADD_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value);

uint8_t
DN_SIMDHASH_TRY_GET_VALUE(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T *result);

uint8_t
DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T *result);

uint8_t
DN_SIMDHASH_TRY_REMOVE(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key);

uint8_t
DN_SIMDHASH_TRY_REMOVE_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash);
