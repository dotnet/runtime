#ifndef ALL_MEASUREMENTS_H
#define ALL_MEASUREMENTS_H

#define INNER_COUNT 1024 * 32

static dn_simdhash_u32_ptr_t *random_u32s_hash;
static dn_vector_t *random_u32s;

static void init_random_u32s () {
    random_u32s_hash = dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
    random_u32s = dn_vector_alloc(sizeof(uint32_t));

    for (int i = 0; i < INNER_COUNT; i++) {
retry: {
        uint32_t key = (uint32_t)(rand() & 0xFFFFFFFFu);
        if (!dn_simdhash_u32_ptr_try_add(random_u32s_hash, key, NULL))
            goto retry;

        dn_vector_push_back(random_u32s, key);
}
    }
}

static void * create_instance_u32_ptr () {
    if (!random_u32s)
        init_random_u32s();

    return dn_simdhash_u32_ptr_new(0, NULL);
}

static void destroy_instance (void *data) {
    if (data)
        dn_simdhash_free((dn_simdhash_t *)data);
}

#endif // ALL_MEASUREMENTS_H

// These go outside the guard because we include this file multiple times.

MEASUREMENT(dn_clear_then_fill_sequential, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++)
        dn_simdhash_u32_ptr_try_add(data, i, (void *)(size_t)i);
})

MEASUREMENT(dn_clear_then_fill_random, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++)
        dn_simdhash_u32_ptr_try_add(data, *dn_vector_index_t(random_u32s, uint32_t, i), (void *)(size_t)i);
})
