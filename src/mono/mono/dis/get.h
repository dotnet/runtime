
/*
 * These return allocated strings
 */
char *get_typedef         (metadata_t *m, int idx);
char *get_module          (metadata_t *m, int idx);
char *get_assemblyref     (metadata_t *m, int idx);
char *get_typeref         (metadata_t *m, int idx);
char *get_typedef_or_ref  (metadata_t *m, guint32 dor_token);
char *get_field_signature (metadata_t *m, guint32 blob_signature);
char *decode_literal      (metadata_t *m, guint32 token);
char *param_flags         (guint32 f);
char *field_flags         (guint32 f);

/*
 * These functions are used during the decoding of streams in the
 * metadata heaps (a simple parsing).
 *
 * They return the `next' location to continue parsing from (ptr is
 * the starting location).
 *
 * Results are returning in the pointer argument.
 */
const char *get_encoded_typedef_or_ref (metadata_t *m, const char *ptr,
					char **result);
const char *get_encoded_value          (const char *_ptr,
					guint32 *len);
const char *get_custom_mod             (metadata_t *m, const char *ptr,
					char **return_value);
const char *get_type                   (metadata_t *m, const char *ptr,
					char **result);
const char *get_ret_type               (metadata_t *m, const char *ptr,
					char **ret_type);
const char *get_param                  (metadata_t *m, const char *ptr,
					char **retval);

const char *get_blob_encoded_size      (const char *ptr, int *size);

void expand (metadata_tableinfo_t *t, int idx, guint32 *res, int res_size);
