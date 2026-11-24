// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <inttypes.h>
#include <math.h>

#include "test.h"
#include <containers/dn-simdhash.h>
#include <containers/dn-simdhash-specializations.h>
#include <containers/dn-vector.h>
#include <minipal/time.h>

typedef struct {
    int i;
    float f;
} instance_data_t;

static DN_FORCEINLINE(uint8_t)
key_comparer (instance_data_t data, size_t lhs, size_t rhs)
{
    return (data.f == 4.20f) || (lhs == rhs);
}

#define DN_SIMDHASH_T dn_simdhash_size_t_size_t
#define DN_SIMDHASH_KEY_T size_t
#define DN_SIMDHASH_VALUE_T size_t
#define DN_SIMDHASH_KEY_HASHER(data, key) (uint32_t)(key & 0xFFFFFFFFu)
#define DN_SIMDHASH_KEY_EQUALS key_comparer
#define DN_SIMDHASH_INSTANCE_DATA_T instance_data_t
#define DN_SIMDHASH_SCAN_DATA_T instance_data_t
#define DN_SIMDHASH_GET_SCAN_DATA(data) (data)
#define DN_SIMDHASH_ON_REMOVE(data, key, value) ((void)(data), (void)(key), (void)(value))
#define DN_SIMDHASH_ON_REPLACE(data, old_key, new_key, old_value, new_value) \
    ((void)(data), (void)(old_key), (void)(new_key), (void)(old_value), (void)(new_value))

#include <containers/dn-simdhash-specialization.h>

#define SIMD_CHECK(condition, ...) \
    do { \
        if (!(condition)) { \
            result = FAILED(__VA_ARGS__); \
            goto done; \
        } \
    } while (0)

static uint32_t
count_cascaded_buckets (dn_simdhash_size_t_size_t_t *hash)
{
    uint32_t result = 0;
    dn_simdhash_buffers_t buffers = hash->buffers;
    BEGIN_SCAN_BUCKETS(buffers, 0, bucket_index, bucket_address)
        result += dn_simdhash_bucket_cascaded_count(bucket_address->suffixes);
    END_SCAN_BUCKETS(buffers, 0, bucket_index, bucket_address)
    return result;
}

static void
foreach_callback (size_t key, size_t value, void *user_data)
{
    (*(uint32_t *)user_data)++;
}

static size_t
test_simdhash_key (uint32_t index)
{
    // Multiplication by an odd number permutes the 32-bit key space.
    return (size_t)(index * UINT32_C(2654435761));
}

static RESULT
test_simdhash_grow_remove_refill (void)
{
    RESULT result = OK;
    const uint32_t item_count = 32000;
    dn_simdhash_size_t_size_t_t *hash = dn_simdhash_size_t_size_t_new(0, NULL);
    SIMD_CHECK(hash != NULL, "Could not allocate table");
    dn_simdhash_instance_data(instance_data_t, hash).f = 3.14f;
    dn_simdhash_instance_data(instance_data_t, hash).i = 42;

    for (uint32_t i = 0; i < item_count; i++) {
        size_t key = test_simdhash_key(i);
        size_t value = (i * 2) + 1;
        SIMD_CHECK(dn_simdhash_size_t_size_t_try_add(hash, key, value) == DN_SIMDHASH_ADD_INSERTED,
            "Could not insert key %zu", key);
    }

    for (uint32_t iteration = 0; iteration < 5; iteration++) {
        SIMD_CHECK(dn_simdhash_count(hash) == item_count,
            "Iteration %u: incorrect count before removal", iteration);
        uint32_t foreach_count = 0;
        dn_simdhash_size_t_size_t_foreach(hash, foreach_callback, &foreach_count);
        SIMD_CHECK(foreach_count == item_count,
            "Iteration %u: foreach visited %u items, expected %u", iteration, foreach_count, item_count);

        for (uint32_t i = 0; i < item_count; i++) {
            size_t key = test_simdhash_key(i);
            size_t value = 0;
            size_t expected_value = (i * 2) + 1;
            SIMD_CHECK(dn_simdhash_size_t_size_t_try_get_value(hash, key, &value) && value == expected_value,
                "Iteration %u: incorrect value for key %zu", iteration, key);
            SIMD_CHECK(dn_simdhash_size_t_size_t_try_add(hash, key, expected_value) == DN_SIMDHASH_ADD_FAILED,
                "Iteration %u: duplicate insertion succeeded for key %zu", iteration, key);
        }
        SIMD_CHECK(dn_simdhash_count(hash) == item_count,
            "Iteration %u: duplicate insertions changed the count", iteration);
        uint32_t final_capacity = dn_simdhash_capacity(hash);

        for (uint32_t i = 0; i < item_count; i++) {
            size_t key = test_simdhash_key(i);
            size_t value = 0;
            SIMD_CHECK(dn_simdhash_size_t_size_t_try_remove(hash, key),
                "Iteration %u: could not remove key %zu", iteration, key);
            SIMD_CHECK(!dn_simdhash_size_t_size_t_try_get_value(hash, key, &value),
                "Iteration %u: found key %zu after removal", iteration, key);
        }
        SIMD_CHECK(dn_simdhash_count(hash) == 0,
            "Iteration %u: table was not empty after removal", iteration);
        SIMD_CHECK(dn_simdhash_capacity(hash) == final_capacity,
            "Iteration %u: emptying the table changed its capacity", iteration);
        foreach_count = 0;
        dn_simdhash_size_t_size_t_foreach(hash, foreach_callback, &foreach_count);
        SIMD_CHECK(foreach_count == 0,
            "Iteration %u: foreach visited an empty table", iteration);

        for (uint32_t i = 0; i < item_count; i++) {
            size_t key = test_simdhash_key(i);
            size_t value = 0;
            SIMD_CHECK(!dn_simdhash_size_t_size_t_try_get_value(hash, key, &value),
                "Iteration %u: found key %zu in the empty table", iteration, key);
        }
        for (uint32_t i = 0; i < item_count; i++) {
            size_t key = test_simdhash_key(i);
            size_t value = (i * 2) + 1;
            SIMD_CHECK(dn_simdhash_size_t_size_t_try_add(hash, key, value) == DN_SIMDHASH_ADD_INSERTED,
                "Iteration %u: could not reinsert key %zu", iteration, key);
        }
        SIMD_CHECK(dn_simdhash_count(hash) == item_count && dn_simdhash_capacity(hash) == final_capacity,
            "Iteration %u: refilling the table produced an incorrect count or capacity", iteration);

        for (uint32_t i = 0; i < item_count; i++) {
            size_t key = test_simdhash_key(i);
            size_t value = 0;
            size_t expected_value = (i * 2) + 1;
            SIMD_CHECK(dn_simdhash_size_t_size_t_try_get_value(hash, key, &value) && value == expected_value,
                "Iteration %u: incorrect value for key %zu after refilling", iteration, key);
        }
        foreach_count = 0;
        dn_simdhash_size_t_size_t_foreach(hash, foreach_callback, &foreach_count);
        SIMD_CHECK(foreach_count == item_count,
            "Iteration %u: incorrect foreach count after refilling", iteration);
    }

done:
    if (hash != NULL)
        dn_simdhash_free(hash);
    return result;
}

static RESULT
test_simdhash_duplicate_after_remove_case (dn_simdhash_insert_mode mode, bool wrap, bool saturated)
{
    RESULT result = OK;
    dn_simdhash_size_t_size_t_t *hash = dn_simdhash_size_t_size_t_new(32, NULL);
    SIMD_CHECK(hash != NULL, "Could not allocate duplicate regression table");
    dn_simdhash_instance_data(instance_data_t, hash).f = 3.14f;
    dn_simdhash_instance_data(instance_data_t, hash).i = 42;

    uint32_t bucket_count = hash->buffers.buckets_length;
    uint32_t first_bucket = wrap ? bucket_count - 1 : 0;
    size_t duplicate_key = first_bucket + (DN_SIMDHASH_BUCKET_CAPACITY * bucket_count);
    for (uint32_t i = 0; i <= DN_SIMDHASH_BUCKET_CAPACITY; i++) {
        size_t key = first_bucket + (i * bucket_count);
        SIMD_CHECK(dn_simdhash_size_t_size_t_try_add(hash, key, i + 1) == DN_SIMDHASH_ADD_INSERTED,
            "Could not populate duplicate regression table");
    }
    SIMD_CHECK(dn_simdhash_size_t_size_t_try_remove(hash, first_bucket),
        "Could not create vacancy before overflowed key");

    if (saturated) {
        for (uint32_t i = 0; i < bucket_count; i++)
            dn_simdhash_bucket_set_cascaded_count(address_of_bucket(hash->buffers, i)->suffixes, 255);
    }

    dn_simdhash_insert_result inserted = DN_SIMDHASH_TRY_INSERT_INTERNAL(
        hash, duplicate_key, (uint32_t)duplicate_key, 1234, mode);
    dn_simdhash_insert_result expected = mode == DN_SIMDHASH_INSERT_MODE_ENSURE_UNIQUE
        ? DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT
        : DN_SIMDHASH_INSERT_OK_OVERWROTE_EXISTING;
    SIMD_CHECK(inserted == expected, "Incorrect duplicate result after removal");
    SIMD_CHECK(dn_simdhash_count(hash) == DN_SIMDHASH_BUCKET_CAPACITY,
        "Duplicate operation changed the count");

    size_t new_key = first_bucket + ((DN_SIMDHASH_BUCKET_CAPACITY + 1) * bucket_count);
    SIMD_CHECK(dn_simdhash_size_t_size_t_try_add(hash, new_key, 5678) == DN_SIMDHASH_ADD_INSERTED,
        "Could not reuse vacancy for a new key");
    SIMD_CHECK(hash->buffers.buckets_length == bucket_count, "Vacancy reuse grew the table");
    SIMD_CHECK(dn_simdhash_count(hash) == DN_SIMDHASH_BUCKET_CAPACITY + 1,
        "New insertion produced an incorrect count");
    SIMD_CHECK(dn_simdhash_bucket_count(address_of_bucket(hash->buffers, first_bucket)->suffixes) == DN_SIMDHASH_BUCKET_CAPACITY,
        "New insertion did not reuse the first vacancy");

    for (uint32_t i = 1; i <= DN_SIMDHASH_BUCKET_CAPACITY + 1; i++) {
        size_t key = first_bucket + (i * bucket_count);
        size_t value = 0;
        size_t expected_value = i == DN_SIMDHASH_BUCKET_CAPACITY + 1 ? 5678
            : (key == duplicate_key && mode != DN_SIMDHASH_INSERT_MODE_ENSURE_UNIQUE ? 1234 : i + 1);
        SIMD_CHECK(dn_simdhash_size_t_size_t_try_get_value(hash, key, &value),
            "Key %zu became unreachable after vacancy reuse", key);
        SIMD_CHECK(value == expected_value, "Incorrect value for key %zu after vacancy reuse", key);
        SIMD_CHECK(dn_simdhash_size_t_size_t_try_remove(hash, key),
            "Could not remove key %zu after vacancy reuse", key);
    }
    SIMD_CHECK(dn_simdhash_count(hash) == 0, "Table was not empty after removing remaining keys");
    SIMD_CHECK(saturated || count_cascaded_buckets(hash) == 0,
        "Vacancy reuse left incorrect cascade counts");

done:
    if (hash != NULL)
        dn_simdhash_free(hash);
    return result;
}

static RESULT
test_simdhash_duplicate_after_remove (void)
{
    for (uint32_t mode = DN_SIMDHASH_INSERT_MODE_ENSURE_UNIQUE;
        mode <= DN_SIMDHASH_INSERT_MODE_OVERWRITE_KEY_AND_VALUE; mode++) {
        for (uint32_t wrap = 0; wrap <= 1; wrap++) {
            for (uint32_t saturated = 0; saturated <= 1; saturated++) {
                RESULT result = test_simdhash_duplicate_after_remove_case((dn_simdhash_insert_mode)mode, wrap != 0, saturated != 0);
                if (result != OK) {
                    RESULT failure = FAILED("mode=%u, wrap=%u, saturated=%u: %s", mode, wrap, saturated, result);
                    free(result);
                    return failure;
                }
            }
        }
    }
    return OK;
}

static const Test dn_simdhash_tests[] = {
    {"test_simdhash_grow_remove_refill", test_simdhash_grow_remove_refill},
    {"test_simdhash_duplicate_after_remove", test_simdhash_duplicate_after_remove},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_simdhash_tests_init, dn_simdhash_tests)

/*
 * Comparison hash table derived from ghashtable.c, modified for SIMD hash benchmarking.
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

typedef void *gpointer;
typedef const void *gconstpointer;
typedef unsigned int guint;
typedef int gboolean;
typedef guint (*GHashFunc)(gconstpointer key);
typedef gboolean (*GEqualFunc)(gconstpointer lhs, gconstpointer rhs);
typedef void (*GDestroyNotify)(gpointer data);

typedef struct _Slot Slot;

struct _Slot {
    gpointer key;
    gpointer value;
    Slot *next;
};

typedef struct {
    GHashFunc hash_func;
    GEqualFunc key_equal_func;
    Slot **table;
    int table_size;
    int in_use;
    int threshold;
    int last_rehash;
    GDestroyNotify value_destroy_func, key_destroy_func;
} GHashTable;

static const guint prime_tbl[] = {
    11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
    1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
    47431, 71143, 106721, 160073, 240101, 360163,
    540217, 810343, 1215497, 1823231, 2734867, 4102283,
    6153409, 9230113, 13845163
};

static gboolean
test_prime (int x)
{
    if ((x & 1) != 0) {
        for (int n = 3; n < (int)sqrt(x); n += 2) {
            if ((x % n) == 0)
                return false;
        }
        return true;
    }
    return x == 2;
}

static int
calc_prime (int x)
{
    for (int i = (x & (~1)) - 1; i < INT32_MAX; i += 2) {
        if (test_prime(i))
            return i;
    }
    return x;
}

static guint
g_spaced_primes_closest (guint x)
{
    for (size_t i = 0; i < sizeof(prime_tbl) / sizeof(prime_tbl[0]); i++) {
        if (x <= prime_tbl[i])
            return prime_tbl[i];
    }
    return calc_prime((int)x);
}

static void *
g_malloc0 (size_t size)
{
    void *result = malloc(size);
    dn_simdhash_assert(result != NULL);
    memset(result, 0, size);
    return result;
}

static guint
g_direct_hash (gconstpointer key)
{
    return (guint)(size_t)key;
}

static gboolean
g_direct_equal (gconstpointer lhs, gconstpointer rhs)
{
    return lhs == rhs;
}

static GHashTable *
g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
    GHashTable *hash = g_malloc0(sizeof(GHashTable));
    hash->hash_func = hash_func ? hash_func : g_direct_hash;
    hash->key_equal_func = key_equal_func ? key_equal_func : g_direct_equal;
    hash->table_size = g_spaced_primes_closest(1);
    hash->table = g_malloc0(sizeof(Slot *) * hash->table_size);
    hash->last_rehash = hash->table_size;
    return hash;
}

static void
do_rehash (GHashTable *hash)
{
    hash->last_rehash = hash->table_size;
    int current_size = hash->table_size;
    hash->table_size = g_spaced_primes_closest(hash->in_use);
    Slot **table = hash->table;
    hash->table = g_malloc0(sizeof(Slot *) * hash->table_size);

    for (int i = 0; i < current_size; i++) {
        Slot *next;
        for (Slot *slot = table[i]; slot != NULL; slot = next) {
            guint hashcode = hash->hash_func(slot->key) % hash->table_size;
            next = slot->next;
            slot->next = hash->table[hashcode];
            hash->table[hashcode] = slot;
        }
    }
    free(table);
}

static void
rehash (GHashTable *hash)
{
    int diff = abs(hash->last_rehash - hash->in_use);
    if (diff * 0.75 > hash->table_size * 2)
        do_rehash(hash);
}

static void
g_hash_table_insert_replace (GHashTable *hash, gpointer key, gpointer value, gboolean replace)
{
    if (hash->in_use >= hash->threshold)
        rehash(hash);

    guint hashcode = hash->hash_func(key) % hash->table_size;
    for (Slot *slot = hash->table[hashcode]; slot != NULL; slot = slot->next) {
        if (hash->key_equal_func(slot->key, key)) {
            if (replace) {
                if (hash->key_destroy_func)
                    hash->key_destroy_func(slot->key);
                slot->key = key;
            }
            if (hash->value_destroy_func)
                hash->value_destroy_func(slot->value);
            slot->value = value;
            return;
        }
    }
    Slot *slot = malloc(sizeof(Slot));
    dn_simdhash_assert(slot != NULL);
    slot->key = key;
    slot->value = value;
    slot->next = hash->table[hashcode];
    hash->table[hashcode] = slot;
    hash->in_use++;
}

#define g_hash_table_insert(hash, key, value) g_hash_table_insert_replace((hash), (key), (value), false)

static gpointer
g_hash_table_lookup (GHashTable *hash, gconstpointer key)
{
    guint hashcode = hash->hash_func(key) % hash->table_size;
    for (Slot *slot = hash->table[hashcode]; slot != NULL; slot = slot->next) {
        if (hash->key_equal_func(slot->key, key))
            return slot->value;
    }
    return NULL;
}

static gboolean
g_hash_table_remove (GHashTable *hash, gconstpointer key)
{
    guint hashcode = hash->hash_func(key) % hash->table_size;
    Slot *last = NULL;
    for (Slot *slot = hash->table[hashcode]; slot != NULL; slot = slot->next) {
        if (hash->key_equal_func(slot->key, key)) {
            if (hash->key_destroy_func)
                hash->key_destroy_func(slot->key);
            if (hash->value_destroy_func)
                hash->value_destroy_func(slot->value);
            if (last == NULL)
                hash->table[hashcode] = slot->next;
            else
                last->next = slot->next;
            free(slot);
            hash->in_use--;
            return true;
        }
        last = slot;
    }
    return false;
}

static void
g_hash_table_remove_all (GHashTable *hash)
{
    for (int i = 0; i < hash->table_size; i++) {
        while (hash->table[i])
            g_hash_table_remove(hash, hash->table[i]->key);
    }
}

static void
g_hash_table_destroy (GHashTable *hash)
{
    for (int i = 0; i < hash->table_size; i++) {
        Slot *next;
        for (Slot *slot = hash->table[i]; slot != NULL; slot = next) {
            next = slot->next;
            if (hash->key_destroy_func)
                hash->key_destroy_func(slot->key);
            if (hash->value_destroy_func)
                hash->value_destroy_func(slot->value);
            free(slot);
        }
    }
    free(hash->table);
    free(hash);
}

#define INNER_COUNT 47431
#define BH_INNER_COUNT 2777
#define BASELINE_SIZE 20480

typedef void *(*setup_func)(void);
typedef void (*measurement_func)(void *data);

typedef struct {
    const char *name;
    setup_func setup;
    measurement_func func, teardown;
} measurement_info;

static dn_simdhash_u32_ptr_t *random_u32s_hash;
static dn_vector_t *sequential_u32s, *random_u32s, *random_unused_u32s;

static uint64_t
rol64 (uint64_t x, int k)
{
    return (x << k) | (x >> (64 - k));
}

// Use a fixed xoshiro seed so key generation is reproducible and independent of libc rand().
static uint64_t rng_state[4] = {
    UINT64_C(0x6ED11324ABA9232C),
    UINT64_C(0x1F0C07E6522724A6),
    UINT64_C(0x293F7FDA2AF571D4),
    UINT64_C(0x5804EC9FFD70112A),
};

static uint64_t
xoshiro256ss (void)
{
    uint64_t result = rol64(rng_state[1] * 5, 7) * 9;
    uint64_t t = rng_state[1] << 17;

    rng_state[2] ^= rng_state[0];
    rng_state[3] ^= rng_state[1];
    rng_state[1] ^= rng_state[2];
    rng_state[0] ^= rng_state[3];

    rng_state[2] ^= t;
    rng_state[3] = rol64(rng_state[3], 45);

    return result;
}

static uint32_t
random_uint (void)
{
    return (uint32_t)(xoshiro256ss() & 0xFFFFFFFFu);
}

static void
init_data (void)
{
    printf("Random u32 key count: %d. Bad hash key count: %d\n", INNER_COUNT, BH_INNER_COUNT);

    random_u32s_hash = dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
    sequential_u32s = dn_vector_alloc(sizeof(uint32_t));
    random_u32s = dn_vector_alloc(sizeof(uint32_t));
    random_unused_u32s = dn_vector_alloc(sizeof(uint32_t));
    dn_simdhash_assert(random_u32s_hash && sequential_u32s && random_u32s && random_unused_u32s);

    for (uint32_t i = 0; i < INNER_COUNT; i++) {
        dn_vector_push_back(sequential_u32s, i);
        for (;;) {
            uint32_t key = random_uint();
            dn_simdhash_add_result inserted = dn_simdhash_u32_ptr_try_add(random_u32s_hash, key, NULL);
            if (inserted == DN_SIMDHASH_ADD_FAILED)
                continue;
            dn_simdhash_assert(inserted == DN_SIMDHASH_ADD_INSERTED);
            dn_vector_push_back(random_u32s, key);
            break;
        }
    }

    for (uint32_t i = 0; i < INNER_COUNT; i++) {
        for (;;) {
            uint32_t key = random_uint();
            dn_simdhash_add_result inserted = dn_simdhash_u32_ptr_try_add(random_u32s_hash, key, NULL);
            if (inserted == DN_SIMDHASH_ADD_FAILED)
                continue;
            dn_simdhash_assert(inserted == DN_SIMDHASH_ADD_INSERTED);
            dn_vector_push_back(random_unused_u32s, key);
            break;
        }
    }
}

static uint32_t
bad_hash_func (const void *key)
{
    return (uint32_t)((((size_t)key) & 0xFFF) << 17);
}

static void *
create_instance_u32_ptr (void)
{
    if (!random_u32s)
        init_data();

    dn_simdhash_u32_ptr_t *result = dn_simdhash_u32_ptr_new(INNER_COUNT, NULL);
    dn_simdhash_assert(result != NULL);
    return result;
}

static void *
create_instance_u32_ptr_random_values (void)
{
    dn_simdhash_u32_ptr_t *result = create_instance_u32_ptr();
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(result, key, (void *)(size_t)i) == DN_SIMDHASH_ADD_INSERTED);
    }
    return result;
}

static void
destroy_instance (void *data)
{
    if (data)
        dn_simdhash_free(data);
}

static void *
baseline_init (void)
{
    void *result = malloc(BASELINE_SIZE);
    dn_simdhash_assert(result != NULL);
    return result;
}

static void
baseline_fini (void *data)
{
    free(data);
}

static void *
create_instance_ght (void)
{
    if (!random_u32s)
        init_data();

    GHashTable *result = g_hash_table_new(NULL, NULL);
    dn_simdhash_assert(result != NULL);
    return result;
}

static void *
create_instance_ght_random_values (void)
{
    GHashTable *result = create_instance_ght();
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        g_hash_table_insert(result, (gpointer)(size_t)key, (gpointer)(size_t)i);
    }
    return result;
}

static void
destroy_instance_ght (void *data)
{
    g_hash_table_destroy(data);
}

static void *
create_instance_dnght_random_values (void)
{
    if (!random_u32s)
        init_data();

    dn_simdhash_ght_t *result = dn_simdhash_ght_new(NULL, NULL, INNER_COUNT, NULL);
    dn_simdhash_assert(result != NULL);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_ght_try_add(result, (gpointer)(size_t)key, (gpointer)(size_t)i));
    }
    return result;
}

static void
destroy_instance_dnght (void *data)
{
    dn_simdhash_free(data);
}

static void *
create_instance_ght_random_values_bad_hash (void)
{
    if (!random_u32s)
        init_data();

    GHashTable *result = g_hash_table_new(bad_hash_func, NULL);
    dn_simdhash_assert(result != NULL);
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        g_hash_table_insert(result, (gpointer)(size_t)key, (gpointer)(size_t)i);
    }
    return result;
}

static void *
create_instance_dnght_random_values_bad_hash (void)
{
    if (!random_u32s)
        init_data();

    dn_simdhash_ght_t *result = dn_simdhash_ght_new(bad_hash_func, NULL, BH_INNER_COUNT, NULL);
    dn_simdhash_assert(result != NULL);
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_ght_try_add(result, (gpointer)(size_t)key, (gpointer)(size_t)i));
    }
    return result;
}

static void *
create_instance_ght_sequential_keys (void)
{
    if (!random_u32s)
        init_data();

    GHashTable *result = g_hash_table_new(bad_hash_func, NULL);
    dn_simdhash_assert(result != NULL);
    for (int i = 0; i < INNER_COUNT; i++)
        g_hash_table_insert(result, (gpointer)(size_t)i, (gpointer)(size_t)i);
    return result;
}

static void *
create_instance_dnght_sequential_keys (void)
{
    if (!random_u32s)
        init_data();

    dn_simdhash_ght_t *result = dn_simdhash_ght_new(bad_hash_func, NULL, INNER_COUNT, NULL);
    dn_simdhash_assert(result != NULL);
    for (int i = 0; i < INNER_COUNT; i++)
        dn_simdhash_assert(dn_simdhash_ght_try_add(result, (gpointer)(size_t)i, (gpointer)(size_t)i));
    return result;
}

#define MEASUREMENT(name, data_type, setup, teardown, body) \
    static void DN_SIMDHASH_GLUE(measurement_, name)(void *_data) { \
        data_type data = (data_type)_data; \
        body; \
    } \
    static const measurement_info DN_SIMDHASH_GLUE(name, _measurement_info) = { \
        #name, setup, DN_SIMDHASH_GLUE(measurement_, name), teardown \
    };

MEASUREMENT(baseline, uint8_t *, baseline_init, baseline_fini, {
    for (int i = 0; i < 256; i++) {
        memset(data, i, BASELINE_SIZE);
        // Without this the memset gets optimized out.
        dn_simdhash_assert(data[i] == i);
    }
})

MEASUREMENT(dn_clear_then_fill_sequential, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(sequential_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i) == DN_SIMDHASH_ADD_INSERTED);
    }
})

MEASUREMENT(dn_clear_then_fill_random, dn_simdhash_u32_ptr_t *, create_instance_u32_ptr, destroy_instance, {
    dn_simdhash_clear(data);
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i) == DN_SIMDHASH_ADD_INSERTED);
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
        dn_simdhash_assert(dn_simdhash_u32_ptr_try_add(data, key, (void *)(size_t)i) == DN_SIMDHASH_ADD_INSERTED);
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

MEASUREMENT(dnght_find_random_keys, dn_simdhash_ght_t *, create_instance_dnght_random_values, destroy_instance_dnght, {
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_ght_get_value_or_default(data, (gpointer)(size_t)key) == (gpointer)(size_t)i);
    }
})

MEASUREMENT(dnght_find_missing_key, dn_simdhash_ght_t *, create_instance_dnght_random_values, destroy_instance_dnght, {
    for (int i = 0; i < INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_unused_u32s, uint32_t, i);
        dn_simdhash_assert(!dn_simdhash_ght_get_value_or_default(data, (gpointer)(size_t)key));
    }
})

MEASUREMENT(ght_find_random_keys_bad_hash, GHashTable *, create_instance_ght_random_values_bad_hash, destroy_instance_ght, {
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(g_hash_table_lookup(data, (gpointer)(size_t)key) == (gpointer)(size_t)i);
    }
})

MEASUREMENT(ght_find_missing_key_bad_hash, GHashTable *, create_instance_ght_random_values_bad_hash, destroy_instance_ght, {
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_unused_u32s, uint32_t, i);
        dn_simdhash_assert(g_hash_table_lookup(data, (gpointer)(size_t)key) == NULL);
    }
})

MEASUREMENT(dnght_find_random_keys_bad_hash, dn_simdhash_ght_t *, create_instance_dnght_random_values_bad_hash, destroy_instance_dnght, {
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_u32s, uint32_t, i);
        dn_simdhash_assert(dn_simdhash_ght_get_value_or_default(data, (gpointer)(size_t)key) == (gpointer)(size_t)i);
    }
})

MEASUREMENT(dnght_find_missing_key_bad_hash, dn_simdhash_ght_t *, create_instance_dnght_random_values_bad_hash, destroy_instance_dnght, {
    for (int i = 0; i < BH_INNER_COUNT; i++) {
        uint32_t key = *dn_vector_index_t(random_unused_u32s, uint32_t, i);
        dn_simdhash_assert(!dn_simdhash_ght_get_value_or_default(data, (gpointer)(size_t)key));
    }
})

MEASUREMENT(ght_find_sequential_keys, GHashTable *, create_instance_ght_sequential_keys, destroy_instance_ght, {
    for (int i = 0; i < INNER_COUNT; i++)
        dn_simdhash_assert(g_hash_table_lookup(data, (gpointer)(size_t)i) == (gpointer)(size_t)i);
})

MEASUREMENT(dnght_find_sequential_keys, dn_simdhash_ght_t *, create_instance_dnght_sequential_keys, destroy_instance_dnght, {
    for (int i = 0; i < INNER_COUNT; i++)
        dn_simdhash_assert(dn_simdhash_ght_get_value_or_default(data, (gpointer)(size_t)i) == (gpointer)(size_t)i);
})

#undef MEASUREMENT

static int64_t
run_measurement (int iteration_count, setup_func setup, measurement_func measurement, measurement_func teardown)
{
    void *data = setup ? setup() : NULL;
    int64_t started = minipal_hires_ticks();
    for (int i = 0; i < iteration_count; i++)
        measurement(data);
    int64_t ended = minipal_hires_ticks();

    if (teardown)
        teardown(data);
    return ended - started;
}

static void
dummy_measurement_func (void *data)
{
}

static void
run_benchmark (const measurement_info *info)
{
    const int64_t tick_frequency = minipal_hires_tick_frequency();
    const double ticks_to_nanoseconds = 1000000000.0 / tick_frequency;
    int64_t warmup_duration = tick_frequency * 2,
        target_step_duration = tick_frequency,
        target_duration = warmup_duration * 10,
        warmup_iterations = 500,
        warmup_until = minipal_hires_ticks() + warmup_duration,
        warmup_elapsed_total = 0,
        warmup_count = 0;

    int64_t overhead = run_measurement(1, info->setup, info->func, info->teardown);
    printf("%s: ", info->name);
    fflush(stdout);

    do {
        warmup_elapsed_total += run_measurement((int)warmup_iterations, info->setup, info->func, info->teardown) - overhead;
        warmup_count++;
    } while (minipal_hires_ticks() < warmup_until);

    int64_t average_warmup_duration = warmup_elapsed_total / warmup_count,
        necessary_iterations = (target_step_duration * warmup_iterations / average_warmup_duration),
        steps = 0,
        run_elapsed_total = 0,
        run_elapsed_min = INT64_MAX,
        run_elapsed_max = INT64_MIN,
        run_until = minipal_hires_ticks() + target_duration;

    if (necessary_iterations < 16)
        necessary_iterations = 16;
    necessary_iterations = next_power_of_two((uint32_t)necessary_iterations);
    overhead = run_measurement((int)necessary_iterations, NULL, dummy_measurement_func, NULL);

    printf("Warmed %" PRId64 " time(s). Running %" PRId64 " iterations... ", warmup_count, necessary_iterations);
    fflush(stdout);

    do {
        int64_t step_duration = run_measurement((int)necessary_iterations, info->setup, info->func, info->teardown) - overhead;
        run_elapsed_total += step_duration;
        if (step_duration < run_elapsed_min)
            run_elapsed_min = step_duration;
        if (step_duration > run_elapsed_max)
            run_elapsed_max = step_duration;
        steps++;
    } while (minipal_hires_ticks() < run_until);

    printf(
        "%" PRId64 " step(s): avg %.3fns min %.3fns max %.3fns\n",
        steps,
        (double)run_elapsed_total / steps / necessary_iterations * ticks_to_nanoseconds,
        (double)run_elapsed_min / necessary_iterations * ticks_to_nanoseconds,
        (double)run_elapsed_max / necessary_iterations * ticks_to_nanoseconds);
    fflush(stdout);
}

RESULT
test_simdhash_perf (void)
{
    static const measurement_info *const measurements[] = {
        &baseline_measurement_info,
        &dn_clear_then_fill_sequential_measurement_info,
        &dn_clear_then_fill_random_measurement_info,
        &dn_find_random_keys_measurement_info,
        &dn_find_missing_key_measurement_info,
        &dn_fill_then_remove_every_item_measurement_info,
        &ght_clear_then_fill_sequential_measurement_info,
        &ght_clear_then_fill_random_measurement_info,
        &ght_find_random_keys_measurement_info,
        &ght_find_missing_key_measurement_info,
        &dnght_find_random_keys_measurement_info,
        &dnght_find_missing_key_measurement_info,
        &ght_find_random_keys_bad_hash_measurement_info,
        &ght_find_missing_key_bad_hash_measurement_info,
        &dnght_find_random_keys_bad_hash_measurement_info,
        &dnght_find_missing_key_bad_hash_measurement_info,
        &ght_find_sequential_keys_measurement_info,
        &dnght_find_sequential_keys_measurement_info,
    };

    for (size_t i = 0; i < sizeof(measurements) / sizeof(measurements[0]); i++)
        run_benchmark(measurements[i]);

    dn_simdhash_free(random_u32s_hash);
    dn_vector_free(sequential_u32s);
    dn_vector_free(random_u32s);
    dn_vector_free(random_unused_u32s);
    random_u32s_hash = NULL;
    sequential_u32s = random_u32s = random_unused_u32s = NULL;
    return OK;
}
