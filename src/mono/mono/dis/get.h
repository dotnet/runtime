
/*
 * These return allocated strings
 */
char *get_typedef             (metadata_t *m, int idx);
char *get_module              (metadata_t *m, int idx);
char *get_assemblyref         (metadata_t *m, int idx);
char *get_typeref             (metadata_t *m, int idx);
char *get_typedef_or_ref      (metadata_t *m, guint32 dor_token);
char *get_field_signature     (metadata_t *m, guint32 blob_signature);
char *decode_literal          (metadata_t *m, guint32 token);
char *get_field               (metadata_t *m, guint32 token);
char *param_flags             (guint32 f);
char *field_flags             (guint32 f);
char *get_methodref_signature (metadata_t *m, guint32 blob_signature, const char *fancy);
char *get_constant            (metadata_t *m, ElementTypeEnum t, guint32 blob_index);
char *get_token               (metadata_t *m, guint32 token);
char *get_token_type          (metadata_t *m, guint32 token);
char *get_typespec            (metadata_t *m, guint32 blob_idx);
char *get_method              (metadata_t *m, guint32 token);


char *dis_stringify_type (metadata_t *m, MonoType *type);
char *dis_stringify_token (metadata_t *m, guint32 token);
char *dis_stringify_array (metadata_t *m, MonoArray *array);
char *dis_stringify_modifiers (metadata_t *m, int n, MonoCustomMod *mod);
char *dis_stringify_param (metadata_t *m, MonoParam *param);
char *dis_stringify_method_signature (metadata_t *m, MonoMethodSignature *method);

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


ElementTypeEnum get_field_literal_type (metadata_t *m, guint32 blob_signature);
