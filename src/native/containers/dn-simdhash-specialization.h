#include "dn-simdhash.h"

#ifndef DN_SIMDHASH_KEY_T
#error Expected DN_SIMDHASH_KEY_T definition
#endif

#ifndef DN_SIMDHASH_VALUE_T
#error Expected DN_SIMDHASH_VALUE_T definition
#endif

#ifndef DN_SIMDHASH_KEY_COMPARER
#error Expected DN_SIMDHASH_KEY_COMPARER definition
#endif

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
#error Expected DN_SIMDHASH_BUCKET_CAPACITY definition
#endif

#define DN_SIMDHASH_BUCKET_T (DN_SIMDHASH_T ## _bucket)
#define DN_SIMDHASH_SCAN_BUCKET_NAME (DN_SIMDHASH_T ## _scan_bucket_internal)
#define DN_SIMDHASH_FIND_VALUE_NAME (DN_SIMDHASH_T ## _find_value_internal)

typedef struct {
    dn_simdhash_suffixes suffixes;
    DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_CAPACITY];
} DN_SIMDHASH_BUCKET_T;

int
DN_SIMDHASH_SCAN_BUCKET_NAME (dn_simdhash_t *hash, DN_SIMDHASH_BUCKET_T *bucket, DN_SIMDHASH_KEY_T needle, dn_simdhash_suffixes search_vector)
{
    dn_simdhash_suffixes suffixes = bucket->suffixes;
    int index = dn_simdhash_find_first_matching_suffix (search_vector, suffixes);
    DN_SIMDHASH_KEY_T *key = &bucket->keys[index];

    for (int count = dn_simdhash_bucket_count (suffixes); index < count; index++, key++) {
        if (DN_SIMDHASH_KEY_COMPARER (needle, *key))
            return index;
    }

    return -1;
}

DN_SIMDHASH_VALUE_T *
DN_SIMDHASH_FIND_VALUE_NAME (dn_simdhash_t *hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
    uint8_t suffix = dn_simdhash_select_suffix(key_hash);
    uint32_t bucket_index = dn_simdhash_select_bucket_index(hash->buffers, key_hash);
    DN_SIMDHASH_BUCKET_T bucket_address = (DN_SIMDHASH_BUCKET_T *)dn_simdhash_address_of_bucket(hash->meta, hash->buffers, bucket_index);
    dn_simdhash_suffixes search_vector = dn_simdhash_build_search_vector(suffix);

    for (uint32_t c = hash->buffers.buckets_length; bucket_index < c; bucket_index++, bucket_address++) {
        int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_NAME (hash, bucket_address, key, search_vector);
        if (index_in_bucket >= 0) {
            uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket;
            return (DN_SIMDHASH_VALUE_T *)dn_simdhash_address_of_value_slot(hash->meta, hash->buffers, value_slot_index);
        }

        if (!dn_simdhash_bucket_is_cascaded(bucket_address->suffixes))
            return NULL;
    }

    return NULL;
}
