dn_simdhash_ght_t *
dn_simdhash_ght_new (
	GHashFunc hash_func, GEqualFunc key_equal_func,
	uint32_t capacity, dn_allocator_t *allocator
);

dn_simdhash_ght_t *
dn_simdhash_ght_new_full (
	GHashFunc hash_func, GEqualFunc key_equal_func,
	GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func,
	uint32_t capacity, dn_allocator_t *allocator
);

// compatible with g_hash_table_insert_replace
void
dn_simdhash_ght_insert_replace (
    dn_simdhash_ght_t *hash,
    gpointer key, gpointer value,
    gboolean overwrite_key
);

// compatibility shims for the g_hash_table_ versions in glib.h
#define dn_simdhash_ght_insert(h,k,v)  dn_simdhash_ght_insert_replace ((h),(k),(v),FALSE)
#define dn_simdhash_ght_replace(h,k,v) dn_simdhash_ght_insert_replace ((h),(k),(v),TRUE)
#define dn_simdhash_ght_add(h,k)       dn_simdhash_ght_insert_replace ((h),(k),(k),TRUE)
