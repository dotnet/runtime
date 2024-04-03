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
