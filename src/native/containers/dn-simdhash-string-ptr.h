uint8_t
dn_simdhash_string_ptr_try_add (dn_simdhash_string_ptr_t *hash, const char *key, void *value);

uint8_t
dn_simdhash_string_ptr_try_get_value (dn_simdhash_string_ptr_t *hash, const char *key, void **result);

uint8_t
dn_simdhash_string_ptr_try_remove (dn_simdhash_string_ptr_t *hash, const char *key);
