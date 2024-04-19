// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ghashtable.h"

#ifndef MEASUREMENTS_IMPLEMENTATION
#define MEASUREMENTS_IMPLEMENTATION 1

// If this is too large and libc's rand() is low quality (i.e. MSVC),
//  initializing the data will take forever
#define INNER_COUNT 1024 * 16
#define BASELINE_SIZE 20480

static dn_simdhash_u32_ptr_t *random_u32s_hash;
static dn_vector_t *sequential_u32s, *random_u32s, *random_unused_u32s;

static void init_data () {
    random_u32s_hash = dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
    sequential_u32s = dn_vector_alloc(sizeof(uint32_t));
    random_u32s = dn_vector_alloc(sizeof(uint32_t));
    random_unused_u32s = dn_vector_alloc(sizeof(uint32_t));
    // For consistent data between runs
    srand(1);

    for (uint32_t i = 0; i < INNER_COUNT; i++) {
        dn_vector_push_back(sequential_u32s, i);

retry: {
        uint32_t key = (uint32_t)(rand() & 0xFFFFFFFFu);
        if (!dn_simdhash_u32_ptr_try_add(random_u32s_hash, key, NULL))
            goto retry;

        dn_vector_push_back(random_u32s, key);
}
    }

    for (uint32_t i = 0; i < INNER_COUNT; i++) {
retry2: {
        uint32_t key = (uint32_t)(rand() & 0xFFFFFFFFu);
        if (!dn_simdhash_u32_ptr_try_add(random_u32s_hash, key, NULL))
            goto retry2;

        dn_vector_push_back(random_unused_u32s, key);
}
    }
}


static void * create_instance_u32_ptr () {
    if (!random_u32s)
        init_data();

    return dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
}

static void * create_instance_u32_ptr_random_values () {
    if (!random_u32s)
        init_data();

    dn_simdhash_u32_ptr_t *result = dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_u32_ptr_try_add(result, key, (void *)(size_t)i);
    }
    return result;
}

static void destroy_instance (void *_data) {
    dn_simdhash_u32_ptr_t *data = _data;
    if (!data)
        return;

    dn_simdhash_free(data);
}


static void * baseline_init () {
    return malloc(BASELINE_SIZE);
}


static void * create_instance_ght () {
    if (!random_u32s)
        init_data();

    return g_hash_table_new(NULL, NULL);
}

static void * create_instance_ght_random_values () {
    if (!random_u32s)
        init_data();

    GHashTable *result = g_hash_table_new(NULL, NULL);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        g_hash_table_insert(result, (gpointer)(size_t)key, (gpointer)(size_t)i);
    }
    return result;
}

static void destroy_instance_ght (void *data) {
    g_hash_table_destroy((GHashTable *)data);
}

#endif // MEASUREMENTS_IMPLEMENTATION

// These go outside the guard because we include this file multiple times.

MEASUREMENT(baseline, uint8_t *, baseline_init, free, {
    for (int i = 0; i < 256; i++) {
        memset(data, i, BASELINE_SIZE);
        // Without this the memset gets optimized out
        dn_simdhash_assert(data[i] == i);
    }
});

MEASUREMENT(dn_clear_then_fill_sequential, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(sequential_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i));
    }
})

MEASUREMENT(dn_clear_then_fill_random, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i));
    }
})

MEASUREMENT(dn_find_random_keys, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr_random_values, destroy_instance, {
    void *temp = NULL;
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_get_value(data, key, &temp));
    }
})

MEASUREMENT(dn_find_missing_key, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr_random_values, destroy_instance, {
    void *temp = NULL;
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_unused_u32s, uint32_t, i);
        dn_simdhash_assert(!dn_simdhash_u32_ptr_try_get_value(data, key, &temp));
    }
})

MEASUREMENT(dn_fill_then_remove_every_item, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i));
    }

    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_remove(data, key));
    }
})

MEASUREMENT(ght_clear_then_fill_sequential, GHashTable *, create_instance_ght, destroy_instance_ght, {
    g_hash_table_remove_all(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(sequential_u32s, uint32_t, i);
        g_hash_table_insert(data, (gpointer)(size_t)key, (gpointer)(size_t)i);
    }
})

MEASUREMENT(ght_clear_then_fill_random, GHashTable *, create_instance_ght, destroy_instance_ght, {
    g_hash_table_remove_all(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        g_hash_table_insert(data, (gpointer)(size_t)key, (gpointer)(size_t)i);
    }
})

MEASUREMENT(ght_find_random_keys, GHashTable *, create_instance_ght_random_values, destroy_instance_ght, {
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(g_hash_table_lookup(data, (gpointer)(size_t)key) == (gpointer)(size_t)i);
    }
})

MEASUREMENT(ght_find_missing_key, GHashTable *, create_instance_ght_random_values, destroy_instance_ght, {
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_unused_u32s, uint32_t, i);
        dn_simdhash_assert(g_hash_table_lookup(data, (gpointer)(size_t)key) == NULL);
    }
})
