
/*
 * These return allocated strings
 */
char *get_typedef             (MonoMetadata *m, int idx);
char *get_module              (MonoMetadata *m, int idx);
char *get_assemblyref         (MonoMetadata *m, int idx);
char *get_typeref             (MonoMetadata *m, int idx);
char *get_typedef_or_ref      (MonoMetadata *m, guint32 dor_token);
char *get_field_signature     (MonoMetadata *m, guint32 blob_signature);
char *decode_literal          (MonoMetadata *m, guint32 token);
char *get_field               (MonoMetadata *m, guint32 token);
char *param_flags             (guint32 f);
char *field_flags             (guint32 f);
char *get_methodref_signature (MonoMetadata *m, guint32 blob_signature, const char *fancy);
char *get_constant            (MonoMetadata *m, MonoTypeEnum t, guint32 blob_index);
char *get_token               (MonoMetadata *m, guint32 token);
char *get_token_type          (MonoMetadata *m, guint32 token);
char *get_typespec            (MonoMetadata *m, guint32 blob_idx);
char *get_method              (MonoMetadata *m, guint32 token);


char *dis_stringify_type      (MonoMetadata *m, MonoType *type);
char *dis_stringify_token     (MonoMetadata *m, guint32 token);
char *dis_stringify_array     (MonoMetadata *m, MonoArray *array);
char *dis_stringify_modifiers (MonoMetadata *m, int n, MonoCustomMod *mod);
char *dis_stringify_param     (MonoMetadata *m, MonoType *param);
char *dis_stringify_method_signature (MonoMetadata *m, MonoMethodSignature *method, int methoddef_row);

/*
 * These functions are used during the decoding of streams in the
 * metadata heaps (a simple parsing).
 *
 * They return the `next' location to continue parsing from (ptr is
 * the starting location).
 *
 * Results are returning in the pointer argument.
 */
const char *get_encoded_typedef_or_ref (MonoMetadata *m, const char *ptr,
					char **result);
const char *get_encoded_value          (const char *_ptr,
					guint32 *len);
const char *get_custom_mod             (MonoMetadata *m, const char *ptr,
					char **return_value);
const char *get_type                   (MonoMetadata *m, const char *ptr,
					char **result);
const char *get_ret_type               (MonoMetadata *m, const char *ptr,
					char **ret_type);
const char *get_param                  (MonoMetadata *m, const char *ptr,
					char **retval);
const char *get_blob_encoded_size      (const char *ptr, int *size);

MonoTypeEnum get_field_literal_type (MonoMetadata *m, guint32 blob_signature);

