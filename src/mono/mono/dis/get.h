
/*
 * These return allocated strings
 */
char *get_typedef             (MonoImage *m, int idx);
char *get_module              (MonoImage *m, int idx);
char *get_assemblyref         (MonoImage *m, int idx);
char *get_typeref             (MonoImage *m, int idx);
char *get_typedef_or_ref      (MonoImage *m, guint32 dor_token);
char *get_field_signature     (MonoImage *m, guint32 blob_signature);
char *decode_literal          (MonoImage *m, guint32 token);
char *get_field               (MonoImage *m, guint32 token);
char *param_flags             (guint32 f);
char *field_flags             (guint32 f);
char *get_methodref_signature (MonoImage *m, guint32 blob_signature, const char *fancy);
char *get_constant            (MonoImage *m, MonoTypeEnum t, guint32 blob_index);
char *get_token               (MonoImage *m, guint32 token);
char *get_token_type          (MonoImage *m, guint32 token);
char *get_typespec            (MonoImage *m, guint32 blob_idx);
char *get_method              (MonoImage *m, guint32 token);


char *dis_stringify_type      (MonoImage *m, MonoType *type);
char *dis_stringify_token     (MonoImage *m, guint32 token);
char *dis_stringify_array     (MonoImage *m, MonoArrayType *array);
char *dis_stringify_modifiers (MonoImage *m, int n, MonoCustomMod *mod);
char *dis_stringify_param     (MonoImage *m, MonoType *param);
char *dis_stringify_method_signature (MonoImage *m, MonoMethodSignature *method, int methoddef_row);

/*
 * These functions are used during the decoding of streams in the
 * metadata heaps (a simple parsing).
 *
 * They return the `next' location to continue parsing from (ptr is
 * the starting location).
 *
 * Results are returning in the pointer argument.
 */
const char *get_encoded_typedef_or_ref (MonoImage *m, const char *ptr,
					char **result);
const char *get_encoded_value          (const char *_ptr,
					guint32 *len);
const char *get_custom_mod             (MonoImage *m, const char *ptr,
					char **return_value);
const char *get_type                   (MonoImage *m, const char *ptr,
					char **result);
const char *get_ret_type               (MonoImage *m, const char *ptr,
					char **ret_type);
const char *get_param                  (MonoImage *m, const char *ptr,
					char **retval);
const char *get_blob_encoded_size      (const char *ptr, int *size);

MonoTypeEnum get_field_literal_type (MonoImage *m, guint32 blob_signature);

