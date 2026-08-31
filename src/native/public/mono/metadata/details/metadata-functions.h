// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif


MONO_API_FUNCTION(void, mono_metadata_init, (void))

MONO_API_FUNCTION(void, mono_metadata_decode_row, (const MonoTableInfo *t, int idx, uint32_t *res, int res_size))

MONO_API_FUNCTION(uint32_t, mono_metadata_decode_row_col, (const MonoTableInfo *t, int idx, unsigned int col))

MONO_API_FUNCTION(int, mono_metadata_compute_size, (MonoImage *meta, int tableindex, uint32_t *result_bitfield))

/*
 *
 */
MONO_API_FUNCTION(const char *, mono_metadata_locate, (MonoImage *meta, int table, int idx))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY const char *, mono_metadata_locate_token, (MonoImage *meta, uint32_t token))

MONO_API_FUNCTION(const char *, mono_metadata_string_heap, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(const char *, mono_metadata_blob_heap, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(const char *, mono_metadata_user_string, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(const char *, mono_metadata_guid_heap, (MonoImage *meta, uint32_t table_index))

MONO_API_FUNCTION(uint32_t, mono_metadata_typedef_from_field, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(uint32_t, mono_metadata_typedef_from_method, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(uint32_t, mono_metadata_nested_in_typedef, (MonoImage *meta, uint32_t table_index))
MONO_API_FUNCTION(uint32_t, mono_metadata_nesting_typedef, (MonoImage *meta, uint32_t table_index, uint32_t start_index))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoClass**, mono_metadata_interfaces_from_typedef, (MonoImage *meta, uint32_t table_index, unsigned int *count))

MONO_API_FUNCTION(uint32_t, mono_metadata_events_from_typedef, (MonoImage *meta, uint32_t table_index, unsigned int *end_idx))
MONO_API_FUNCTION(uint32_t, mono_metadata_methods_from_event, (MonoImage *meta, uint32_t table_index, unsigned int *end))
MONO_API_FUNCTION(uint32_t, mono_metadata_properties_from_typedef, (MonoImage *meta, uint32_t table_index, unsigned int *end))
MONO_API_FUNCTION(uint32_t, mono_metadata_methods_from_property, (MonoImage *meta, uint32_t table_index, unsigned int *end))
MONO_API_FUNCTION(uint32_t, mono_metadata_packing_from_typedef, (MonoImage *meta, uint32_t table_index, uint32_t *packing, uint32_t *size))
MONO_API_FUNCTION(const char*, mono_metadata_get_marshal_info, (MonoImage *meta, uint32_t idx, mono_bool is_field))
MONO_API_FUNCTION(uint32_t, mono_metadata_custom_attrs_from_index, (MonoImage *meta, uint32_t cattr_index))

MONO_API_FUNCTION(MonoMarshalSpec *, mono_metadata_parse_marshal_spec, (MonoImage *image, const char *ptr))

MONO_API_FUNCTION(void, mono_metadata_free_marshal_spec, (MonoMarshalSpec *spec))

MONO_API_FUNCTION(uint32_t, mono_metadata_implmap_from_method, (MonoImage *meta, uint32_t method_idx))

MONO_API_FUNCTION(void, mono_metadata_field_info, (MonoImage *meta, uint32_t table_index, uint32_t *offset, uint32_t *rva, MonoMarshalSpec **marshal_spec))
MONO_API_FUNCTION(uint32_t, mono_metadata_get_constant_index, (MonoImage *meta, uint32_t token, uint32_t hint))

/*
 * Functions to extract information from the Blobs
 */
MONO_API_FUNCTION(uint32_t, mono_metadata_decode_value, (const char *ptr, const char **rptr))
MONO_API_FUNCTION(int32_t, mono_metadata_decode_signed_value, (const char *ptr, const char **rptr))

MONO_API_FUNCTION(uint32_t, mono_metadata_decode_blob_size, (const char *ptr, const char **rptr))

MONO_API_FUNCTION(void, mono_metadata_encode_value, (uint32_t value, char *bug, char **endbuf))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_type_is_byref, (MonoType *type))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY int, mono_type_get_type, (MonoType *type))

/* For MONO_TYPE_FNPTR */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature*, mono_type_get_signature, (MonoType *type))

/* For MONO_TYPE_CLASS, VALUETYPE */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoClass*, mono_type_get_class, (MonoType *type))

MONO_API_FUNCTION(MonoArrayType*, mono_type_get_array_type, (MonoType *type))

/* For MONO_TYPE_PTR */
MONO_API_FUNCTION(MonoType*, mono_type_get_ptr_type, (MonoType *type))

MONO_API_FUNCTION(MonoClass*, mono_type_get_modifiers, (MonoType *type, mono_bool *is_required, void **iter))

MONO_API_FUNCTION(mono_bool, mono_type_is_struct, (MonoType *type))
MONO_API_FUNCTION(mono_bool, mono_type_is_void, (MonoType *type))
MONO_API_FUNCTION(mono_bool, mono_type_is_pointer, (MonoType *type))
MONO_API_FUNCTION(mono_bool, mono_type_is_reference, (MonoType *type))
MONO_API_FUNCTION(mono_bool, mono_type_is_generic_parameter, (MonoType *type))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType*, mono_signature_get_return_type, (MonoMethodSignature *sig))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType*, mono_signature_get_params, (MonoMethodSignature *sig, void **iter))

MONO_API_FUNCTION(uint32_t, mono_signature_get_param_count, (MonoMethodSignature *sig))

MONO_API_FUNCTION(uint32_t, mono_signature_get_call_conv, (MonoMethodSignature *sig))

MONO_API_FUNCTION(int, mono_signature_vararg_start, (MonoMethodSignature *sig))

MONO_API_FUNCTION(mono_bool, mono_signature_is_instance, (MonoMethodSignature *sig))

MONO_API_FUNCTION(mono_bool, mono_signature_explicit_this, (MonoMethodSignature *sig))

MONO_API_FUNCTION(mono_bool, mono_signature_param_is_out, (MonoMethodSignature *sig, int param_num))

MONO_API_FUNCTION(uint32_t, mono_metadata_parse_typedef_or_ref, (MonoImage *m, const char *ptr, const char **rptr))
MONO_API_FUNCTION(int, mono_metadata_parse_custom_mod, (MonoImage *m, MonoCustomMod *dest, const char *ptr, const char **rptr))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoArrayType *, mono_metadata_parse_array, (MonoImage *m, const char *ptr, const char **rptr))
MONO_API_FUNCTION(void, mono_metadata_free_array, (MonoArrayType     *array))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType *, mono_metadata_parse_type, (MonoImage *m, MonoParseTypeMode mode, short opt_attrs, const char *ptr, const char **rptr))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType *, mono_metadata_parse_param, (MonoImage *m, const char *ptr, const char **rptr))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType *, mono_metadata_parse_field_type, (MonoImage *m, short field_flags, const char *ptr, const char **rptr))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType *, mono_type_create_from_typespec, (MonoImage *image, uint32_t type_spec))
MONO_API_FUNCTION(void, mono_metadata_free_type, (MonoType *type))
MONO_API_FUNCTION(int, mono_type_size, (MonoType *type, int *alignment))
MONO_API_FUNCTION(int, mono_type_stack_size, (MonoType *type, int *alignment))

MONO_API_FUNCTION(mono_bool, mono_type_generic_inst_is_valuetype, (MonoType *type))
MONO_API_FUNCTION(mono_bool, mono_metadata_generic_class_is_valuetype, (MonoGenericClass *gclass))

MONO_API_FUNCTION(unsigned int, mono_metadata_type_hash, (MonoType *t1))
MONO_API_FUNCTION(mono_bool, mono_metadata_type_equal, (MonoType *t1, MonoType *t2))

MONO_API_FUNCTION(MonoMethodSignature *, mono_metadata_signature_alloc, (MonoImage *image, uint32_t nparams))

MONO_API_FUNCTION(MonoMethodSignature *, mono_metadata_signature_dup, (MonoMethodSignature *sig))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature *, mono_metadata_parse_signature, (MonoImage *image, uint32_t token))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature *, mono_metadata_parse_method_signature, (MonoImage *m, int def, const char *ptr, const char **rptr))
MONO_API_FUNCTION(void, mono_metadata_free_method_signature, (MonoMethodSignature *method))

MONO_API_FUNCTION(mono_bool, mono_metadata_signature_equal, (MonoMethodSignature *sig1, MonoMethodSignature *sig2))

MONO_API_FUNCTION(unsigned int, mono_signature_hash, (MonoMethodSignature *sig))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodHeader *, mono_metadata_parse_mh, (MonoImage *m, const char *ptr))
MONO_API_FUNCTION(void, mono_metadata_free_mh, (MonoMethodHeader *mh))

/* MonoMethodHeader accessors */
MONO_API_FUNCTION(const unsigned char*, mono_method_header_get_code, (MonoMethodHeader *header, uint32_t* code_size, uint32_t* max_stack))

MONO_API_FUNCTION(MonoType**, mono_method_header_get_locals, (MonoMethodHeader *header, uint32_t* num_locals, mono_bool *init_locals))

MONO_API_FUNCTION(int, mono_method_header_get_num_clauses, (MonoMethodHeader *header))

MONO_API_FUNCTION(int, mono_method_header_get_clauses, (MonoMethodHeader *header, MonoMethod *method, void **iter, MonoExceptionClause *clause))

MONO_API_FUNCTION(uint32_t, mono_type_to_unmanaged, (MonoType *type, MonoMarshalSpec *mspec, mono_bool as_field, mono_bool unicode, MonoMarshalConv *conv))

MONO_API_FUNCTION(uint32_t, mono_metadata_token_from_dor, (uint32_t dor_index))

MONO_API_FUNCTION(char *, mono_guid_to_string, (const uint8_t *guid))

MONO_API_FUNCTION(char *, mono_guid_to_string_minimal, (const uint8_t *guid))

MONO_API_FUNCTION(uint32_t, mono_metadata_declsec_from_index, (MonoImage *meta, uint32_t idx))

MONO_API_FUNCTION(uint32_t, mono_metadata_translate_token_index, (MonoImage *image, int table, uint32_t idx))

MONO_API_FUNCTION(void, mono_metadata_decode_table_row, (MonoImage *image, int table, int idx, uint32_t *res, int res_size))

MONO_API_FUNCTION(uint32_t, mono_metadata_decode_table_row_col, (MonoImage *image, int table, int idx, unsigned int col))
