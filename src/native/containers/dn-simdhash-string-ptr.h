uint8_t
dn_simdhash_string_ptr_try_add (dn_simdhash_string_ptr_t *hash, const char *key, void *value);

uint8_t
dn_simdhash_string_ptr_try_get_value (dn_simdhash_string_ptr_t *hash, const char *key, void **result);

uint8_t
dn_simdhash_string_ptr_try_remove (dn_simdhash_string_ptr_t *hash, const char *key);

typedef void (*dn_simdhash_string_ptr_foreach_func) (const char *key, void *value, void *user_data);

void
dn_simdhash_string_ptr_foreach (dn_simdhash_string_ptr_t *hash, dn_simdhash_string_ptr_foreach_func func, void *user_data);
