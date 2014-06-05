
#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <mono/utils/mono-publib.h>

#include <mono/metadata/blob.h>
#include <mono/metadata/row-indexes.h>
#include <mono/metadata/image.h>

MONO_BEGIN_DECLS

/*
 * When embedding, you have to define MONO_ZERO_LEN_ARRAY before including any
 * other Mono header file if you use a different compiler from the one used to
 * build Mono.
 */
#ifndef MONO_ZERO_LEN_ARRAY
#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif
#endif

#define MONO_TYPE_ISSTRUCT(t) mono_type_is_struct (t)
#define MONO_TYPE_IS_VOID(t) mono_type_is_void (t)
#define MONO_TYPE_IS_POINTER(t) mono_type_is_pointer (t)
#define MONO_TYPE_IS_REFERENCE(t) mono_type_is_reference (t)

#define MONO_CLASS_IS_INTERFACE(c) ((c->flags & TYPE_ATTRIBUTE_INTERFACE) || (c->byval_arg.type == MONO_TYPE_VAR) || (c->byval_arg.type == MONO_TYPE_MVAR))

#define MONO_CLASS_IS_IMPORT(c) ((c->flags & TYPE_ATTRIBUTE_IMPORT))

typedef struct _MonoClass MonoClass;
typedef struct _MonoDomain MonoDomain;
typedef struct _MonoMethod MonoMethod;

typedef enum {
	MONO_EXCEPTION_CLAUSE_NONE,
	MONO_EXCEPTION_CLAUSE_FILTER,
	MONO_EXCEPTION_CLAUSE_FINALLY,
	MONO_EXCEPTION_CLAUSE_FAULT = 4
} MonoExceptionEnum;

typedef enum {
	MONO_CALL_DEFAULT,
	MONO_CALL_C,
	MONO_CALL_STDCALL,
	MONO_CALL_THISCALL,
	MONO_CALL_FASTCALL,
	MONO_CALL_VARARG
} MonoCallConvention;

/* ECMA lamespec: the old spec had more info... */
typedef enum {
	MONO_NATIVE_BOOLEAN = 0x02, /* 4 bytes, 0 is false, != 0 is true */
	MONO_NATIVE_I1 = 0x03,
	MONO_NATIVE_U1 = 0x04,
	MONO_NATIVE_I2 = 0x05,
	MONO_NATIVE_U2 = 0x06,
	MONO_NATIVE_I4 = 0x07,
	MONO_NATIVE_U4 = 0x08,
	MONO_NATIVE_I8 = 0x09,
	MONO_NATIVE_U8 = 0x0a,
	MONO_NATIVE_R4 = 0x0b,
	MONO_NATIVE_R8 = 0x0c,
	MONO_NATIVE_CURRENCY = 0x0f,
	MONO_NATIVE_BSTR = 0x13, /* prefixed length, Unicode */
	MONO_NATIVE_LPSTR = 0x14, /* ANSI, null terminated */
	MONO_NATIVE_LPWSTR = 0x15, /* UNICODE, null terminated */
	MONO_NATIVE_LPTSTR = 0x16, /* plattform dep., null terminated */
	MONO_NATIVE_BYVALTSTR = 0x17,
	MONO_NATIVE_IUNKNOWN = 0x19,
	MONO_NATIVE_IDISPATCH = 0x1a,
	MONO_NATIVE_STRUCT = 0x1b,
	MONO_NATIVE_INTERFACE = 0x1c,
	MONO_NATIVE_SAFEARRAY = 0x1d,
	MONO_NATIVE_BYVALARRAY = 0x1e,
	MONO_NATIVE_INT   = 0x1f,
	MONO_NATIVE_UINT  = 0x20,
	MONO_NATIVE_VBBYREFSTR  = 0x22,
	MONO_NATIVE_ANSIBSTR  = 0x23,  /* prefixed length, ANSI */
	MONO_NATIVE_TBSTR  = 0x24, /* prefixed length, plattform dep. */
	MONO_NATIVE_VARIANTBOOL  = 0x25,
	MONO_NATIVE_FUNC  = 0x26,
	MONO_NATIVE_ASANY = 0x28,
	MONO_NATIVE_LPARRAY = 0x2a,
	MONO_NATIVE_LPSTRUCT = 0x2b,
	MONO_NATIVE_CUSTOM = 0x2c,
	MONO_NATIVE_ERROR = 0x2d,
	MONO_NATIVE_MAX = 0x50 /* no info */
} MonoMarshalNative;

/* Used only in context of SafeArray */
typedef enum {
	MONO_VARIANT_EMPTY = 0x00,
	MONO_VARIANT_NULL = 0x01,
	MONO_VARIANT_I2 = 0x02,
	MONO_VARIANT_I4 = 0x03,
	MONO_VARIANT_R4 = 0x04,
	MONO_VARIANT_R8 = 0x05,
	MONO_VARIANT_CY = 0x06,
	MONO_VARIANT_DATE = 0x07,
	MONO_VARIANT_BSTR = 0x08,
	MONO_VARIANT_DISPATCH = 0x09,
	MONO_VARIANT_ERROR = 0x0a,
	MONO_VARIANT_BOOL = 0x0b,
	MONO_VARIANT_VARIANT = 0x0c,
	MONO_VARIANT_UNKNOWN = 0x0d,
	MONO_VARIANT_DECIMAL = 0x0e,
	MONO_VARIANT_I1 = 0x10,
	MONO_VARIANT_UI1 = 0x11,
	MONO_VARIANT_UI2 = 0x12,
	MONO_VARIANT_UI4 = 0x13,
	MONO_VARIANT_I8 = 0x14,
	MONO_VARIANT_UI8 = 0x15,
	MONO_VARIANT_INT = 0x16,
	MONO_VARIANT_UINT = 0x17,
	MONO_VARIANT_VOID = 0x18,
	MONO_VARIANT_HRESULT = 0x19,
	MONO_VARIANT_PTR = 0x1a,
	MONO_VARIANT_SAFEARRAY = 0x1b,
	MONO_VARIANT_CARRAY = 0x1c,
	MONO_VARIANT_USERDEFINED = 0x1d,
	MONO_VARIANT_LPSTR = 0x1e,
	MONO_VARIANT_LPWSTR = 0x1f,
	MONO_VARIANT_RECORD = 0x24,
	MONO_VARIANT_FILETIME = 0x40,
	MONO_VARIANT_BLOB = 0x41,
	MONO_VARIANT_STREAM = 0x42,
	MONO_VARIANT_STORAGE = 0x43,
	MONO_VARIANT_STREAMED_OBJECT = 0x44,
	MONO_VARIANT_STORED_OBJECT = 0x45,
	MONO_VARIANT_BLOB_OBJECT = 0x46,
	MONO_VARIANT_CF = 0x47,
	MONO_VARIANT_CLSID = 0x48,
	MONO_VARIANT_VECTOR = 0x1000,
	MONO_VARIANT_ARRAY = 0x2000,
	MONO_VARIANT_BYREF = 0x4000
} MonoMarshalVariant;

typedef enum {
	MONO_MARSHAL_CONV_NONE,
	MONO_MARSHAL_CONV_BOOL_VARIANTBOOL,
	MONO_MARSHAL_CONV_BOOL_I4,
	MONO_MARSHAL_CONV_STR_BSTR,
	MONO_MARSHAL_CONV_STR_LPSTR,
	MONO_MARSHAL_CONV_LPSTR_STR,
	MONO_MARSHAL_CONV_LPTSTR_STR,
	MONO_MARSHAL_CONV_STR_LPWSTR,
	MONO_MARSHAL_CONV_LPWSTR_STR,
	MONO_MARSHAL_CONV_STR_LPTSTR,
	MONO_MARSHAL_CONV_STR_ANSIBSTR,
	MONO_MARSHAL_CONV_STR_TBSTR,
	MONO_MARSHAL_CONV_STR_BYVALSTR,
	MONO_MARSHAL_CONV_STR_BYVALWSTR,
	MONO_MARSHAL_CONV_SB_LPSTR,
	MONO_MARSHAL_CONV_SB_LPTSTR,
	MONO_MARSHAL_CONV_SB_LPWSTR,
	MONO_MARSHAL_CONV_LPSTR_SB,
	MONO_MARSHAL_CONV_LPTSTR_SB,
	MONO_MARSHAL_CONV_LPWSTR_SB,
	MONO_MARSHAL_CONV_ARRAY_BYVALARRAY,
	MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY,
	MONO_MARSHAL_CONV_ARRAY_SAVEARRAY,
	MONO_MARSHAL_CONV_ARRAY_LPARRAY,
	MONO_MARSHAL_FREE_LPARRAY,
	MONO_MARSHAL_CONV_OBJECT_INTERFACE,
	MONO_MARSHAL_CONV_OBJECT_IDISPATCH,
	MONO_MARSHAL_CONV_OBJECT_IUNKNOWN,
	MONO_MARSHAL_CONV_OBJECT_STRUCT,
	MONO_MARSHAL_CONV_DEL_FTN,
	MONO_MARSHAL_CONV_FTN_DEL,
	MONO_MARSHAL_FREE_ARRAY,
	MONO_MARSHAL_CONV_BSTR_STR,
	MONO_MARSHAL_CONV_SAFEHANDLE,
	MONO_MARSHAL_CONV_HANDLEREF
} MonoMarshalConv;

typedef struct {
	MonoMarshalNative native;
	union {
		struct {
			MonoMarshalNative elem_type;
			int32_t num_elem; /* -1 if not set */
			int16_t param_num; /* -1 if not set */
			int16_t elem_mult; /* -1 if not set */
		} array_data;
		struct {
			char *custom_name;
			char *cookie;
			MonoImage *image;
		} custom_data;
		struct {
			MonoMarshalVariant elem_type;
			int32_t num_elem;
		} safearray_data;
	} data;
} MonoMarshalSpec;

MONO_API void         mono_metadata_init (void);

MONO_API void         mono_metadata_decode_row (const MonoTableInfo   *t,
				       int                    idx,
				       uint32_t               *res,
				       int                    res_size);

MONO_API uint32_t      mono_metadata_decode_row_col (const MonoTableInfo *t, 
					   int            idx, 
					   unsigned int          col);

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of MonoTableInfo.
 */
#define mono_metadata_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define mono_metadata_table_count(bitfield) ((bitfield) >> 24)

MONO_API int mono_metadata_compute_size (MonoImage   *meta,
                                int             tableindex,
                                uint32_t        *result_bitfield);

/*
 *
 */
MONO_API const char    *mono_metadata_locate        (MonoImage *meta, int table, int idx);
MONO_API const char    *mono_metadata_locate_token  (MonoImage *meta, uint32_t token);
					   
MONO_API const char    *mono_metadata_string_heap   (MonoImage *meta, uint32_t table_index);
MONO_API const char    *mono_metadata_blob_heap     (MonoImage *meta, uint32_t table_index);
MONO_API const char    *mono_metadata_user_string   (MonoImage *meta, uint32_t table_index);
MONO_API const char    *mono_metadata_guid_heap     (MonoImage *meta, uint32_t table_index);

MONO_API uint32_t mono_metadata_typedef_from_field  (MonoImage *meta, uint32_t table_index);
MONO_API uint32_t mono_metadata_typedef_from_method (MonoImage *meta, uint32_t table_index);
MONO_API uint32_t mono_metadata_nested_in_typedef   (MonoImage *meta, uint32_t table_index);
MONO_API uint32_t mono_metadata_nesting_typedef     (MonoImage *meta, uint32_t table_index, uint32_t start_index);

MONO_API MonoClass** mono_metadata_interfaces_from_typedef (MonoImage *meta, uint32_t table_index, unsigned int *count);

MONO_API uint32_t     mono_metadata_events_from_typedef     (MonoImage *meta, uint32_t table_index, unsigned int *end_idx);
MONO_API uint32_t     mono_metadata_methods_from_event      (MonoImage *meta, uint32_t table_index, unsigned int *end);
MONO_API uint32_t     mono_metadata_properties_from_typedef (MonoImage *meta, uint32_t table_index, unsigned int *end);
MONO_API uint32_t     mono_metadata_methods_from_property   (MonoImage *meta, uint32_t table_index, unsigned int *end);
MONO_API uint32_t     mono_metadata_packing_from_typedef    (MonoImage *meta, uint32_t table_index, uint32_t *packing, uint32_t *size);
MONO_API const char* mono_metadata_get_marshal_info        (MonoImage *meta, uint32_t idx, mono_bool is_field);
MONO_API uint32_t     mono_metadata_custom_attrs_from_index (MonoImage *meta, uint32_t cattr_index);

MONO_API MonoMarshalSpec *mono_metadata_parse_marshal_spec (MonoImage *image, const char *ptr);

MONO_API void mono_metadata_free_marshal_spec (MonoMarshalSpec *spec);

MONO_API uint32_t     mono_metadata_implmap_from_method     (MonoImage *meta, uint32_t method_idx);

MONO_API void        mono_metadata_field_info (MonoImage *meta, 
				      uint32_t       table_index,
				      uint32_t      *offset,
				      uint32_t      *rva,
				      MonoMarshalSpec **marshal_spec);

MONO_API uint32_t     mono_metadata_get_constant_index (MonoImage *meta, uint32_t token, uint32_t hint);

/*
 * Functions to extract information from the Blobs
 */
MONO_API uint32_t mono_metadata_decode_value     (const char            *ptr,
                                        const char           **rptr);
MONO_API int32_t mono_metadata_decode_signed_value (const char *ptr, const char **rptr);

MONO_API uint32_t mono_metadata_decode_blob_size (const char            *ptr,
                                        const char           **rptr);

MONO_API void mono_metadata_encode_value (uint32_t value, char *bug, char **endbuf);

#define MONO_OFFSET_IN_CLAUSE(clause,offset) \
	((clause)->try_offset <= (offset) && (offset) < ((clause)->try_offset + (clause)->try_len))
#define MONO_OFFSET_IN_HANDLER(clause,offset) \
	((clause)->handler_offset <= (offset) && (offset) < ((clause)->handler_offset + (clause)->handler_len))
#define MONO_OFFSET_IN_FILTER(clause,offset) \
	((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER && (clause)->data.filter_offset <= (offset) && (offset) < ((clause)->handler_offset))

typedef struct {
	uint32_t flags;
	uint32_t try_offset;
	uint32_t try_len;
	uint32_t handler_offset;
	uint32_t handler_len;
	union {
		uint32_t filter_offset;
		MonoClass *catch_class;
	} data;
} MonoExceptionClause;

typedef struct _MonoType MonoType;
typedef struct _MonoGenericInst MonoGenericInst;
typedef struct _MonoGenericClass MonoGenericClass;
typedef struct _MonoDynamicGenericClass MonoDynamicGenericClass;
typedef struct _MonoGenericContext MonoGenericContext;
typedef struct _MonoGenericContainer MonoGenericContainer;
typedef struct _MonoGenericParam MonoGenericParam;
typedef struct _MonoArrayType MonoArrayType;
typedef struct _MonoMethodSignature MonoMethodSignature;

/* FIXME: Keeping this name alive for now, since it is part of the exposed API, even though no entrypoint uses it.  */
typedef struct invalid_name MonoGenericMethod;

typedef struct {
	unsigned int required : 1;
	unsigned int token    : 31;
} MonoCustomMod;

struct _MonoArrayType {
	MonoClass *eklass;
	uint8_t rank;
	uint8_t numsizes;
	uint8_t numlobounds;
	int *sizes;
	int *lobounds;
};

typedef struct _MonoMethodHeader MonoMethodHeader;

typedef enum {
	MONO_PARSE_TYPE,
	MONO_PARSE_MOD_TYPE,
	MONO_PARSE_LOCAL,
	MONO_PARSE_PARAM,
	MONO_PARSE_RET,
	MONO_PARSE_FIELD
} MonoParseTypeMode;

MONO_API mono_bool
mono_type_is_byref       (MonoType *type);

MONO_API int
mono_type_get_type       (MonoType *type);

/* For MONO_TYPE_FNPTR */
MONO_API MonoMethodSignature*
mono_type_get_signature  (MonoType *type);

/* For MONO_TYPE_CLASS, VALUETYPE */
MONO_API MonoClass*
mono_type_get_class      (MonoType *type);

MONO_API MonoArrayType*
mono_type_get_array_type (MonoType *type);

/* For MONO_TYPE_PTR */
MONO_API MonoType*
mono_type_get_ptr_type (MonoType *type);

MONO_API MonoClass*
mono_type_get_modifiers  (MonoType *type, mono_bool *is_required, void **iter);

MONO_API mono_bool mono_type_is_struct    (MonoType *type);
MONO_API mono_bool mono_type_is_void      (MonoType *type);
MONO_API mono_bool mono_type_is_pointer   (MonoType *type);
MONO_API mono_bool mono_type_is_reference (MonoType *type);

MONO_API MonoType*
mono_signature_get_return_type (MonoMethodSignature *sig);

MONO_API MonoType*
mono_signature_get_params      (MonoMethodSignature *sig, void **iter);

MONO_API uint32_t
mono_signature_get_param_count (MonoMethodSignature *sig);

MONO_API uint32_t
mono_signature_get_call_conv   (MonoMethodSignature *sig);

MONO_API int
mono_signature_vararg_start    (MonoMethodSignature *sig);

MONO_API mono_bool
mono_signature_is_instance     (MonoMethodSignature *sig);

MONO_API mono_bool
mono_signature_explicit_this   (MonoMethodSignature *sig);

MONO_API mono_bool
mono_signature_param_is_out    (MonoMethodSignature *sig, int param_num);

MONO_API uint32_t     mono_metadata_parse_typedef_or_ref (MonoImage      *m,
                                                const char      *ptr,
                                                const char     **rptr);
MONO_API int            mono_metadata_parse_custom_mod  (MonoImage      *m,
						MonoCustomMod   *dest,
						const char      *ptr,
						const char     **rptr);
MONO_API MonoArrayType *mono_metadata_parse_array       (MonoImage      *m,
						const char      *ptr,
						const char     **rptr);
MONO_API void           mono_metadata_free_array        (MonoArrayType     *array);
MONO_API MonoType      *mono_metadata_parse_type        (MonoImage      *m,
						MonoParseTypeMode  mode,
						short              opt_attrs,
						const char        *ptr,
						const char       **rptr);
MONO_API MonoType      *mono_metadata_parse_param       (MonoImage      *m,
						const char      *ptr,
						const char      **rptr);
MONO_API MonoType      *mono_metadata_parse_ret_type    (MonoImage      *m,
						const char      *ptr,
						const char      **rptr);
MONO_API MonoType      *mono_metadata_parse_field_type  (MonoImage      *m,
		                                short            field_flags,
						const char      *ptr,
						const char      **rptr);
MONO_API MonoType      *mono_type_create_from_typespec  (MonoImage        *image, 
					        uint32_t           type_spec);
MONO_API void           mono_metadata_free_type         (MonoType        *type);
MONO_API int            mono_type_size                  (MonoType        *type, 
						int             *alignment);
MONO_API int            mono_type_stack_size            (MonoType        *type, 
						int             *alignment);

MONO_API mono_bool       mono_type_generic_inst_is_valuetype      (MonoType *type);
MONO_API mono_bool       mono_metadata_generic_class_is_valuetype (MonoGenericClass *gclass);
MONO_API unsigned int          mono_metadata_generic_class_hash  (MonoGenericClass *gclass);
MONO_API mono_bool       mono_metadata_generic_class_equal (MonoGenericClass *g1, MonoGenericClass *g2);

MONO_API unsigned int          mono_metadata_type_hash         (MonoType *t1);
MONO_API mono_bool       mono_metadata_type_equal        (MonoType *t1, MonoType *t2);

MONO_API MonoMethodSignature  *mono_metadata_signature_alloc (MonoImage *image, uint32_t nparams);

MONO_API MonoMethodSignature  *mono_metadata_signature_dup (MonoMethodSignature *sig);

MONO_API MonoMethodSignature  *mono_metadata_parse_signature (MonoImage *image, 
						     uint32_t    token);

MONO_API MonoMethodSignature  *mono_metadata_parse_method_signature (MonoImage            *m,
                                                            int                    def,
                                                            const char            *ptr,
                                                            const char           **rptr);
MONO_API void                  mono_metadata_free_method_signature  (MonoMethodSignature   *method);

MONO_API mono_bool          mono_metadata_signature_equal (MonoMethodSignature *sig1, 
						 MonoMethodSignature *sig2);

MONO_API unsigned int             mono_signature_hash (MonoMethodSignature *sig);

MONO_API MonoMethodHeader *mono_metadata_parse_mh (MonoImage *m, const char *ptr);
MONO_API void              mono_metadata_free_mh  (MonoMethodHeader *mh);

/* MonoMethodHeader acccessors */
MONO_API const unsigned char*
mono_method_header_get_code (MonoMethodHeader *header, uint32_t* code_size, uint32_t* max_stack);

MONO_API MonoType**
mono_method_header_get_locals (MonoMethodHeader *header, uint32_t* num_locals, mono_bool *init_locals);

MONO_API int
mono_method_header_get_num_clauses (MonoMethodHeader *header);

MONO_API int
mono_method_header_get_clauses (MonoMethodHeader *header, MonoMethod *method, void **iter, MonoExceptionClause *clause);

MONO_API uint32_t 
mono_type_to_unmanaged (MonoType *type, MonoMarshalSpec *mspec, 
			mono_bool as_field, mono_bool unicode, MonoMarshalConv *conv);

/*
 * Makes a token based on a table and an index
 */
#define mono_metadata_make_token(table,idx) (((table) << 24)| (idx))

/*
 * Returns the table index that this token encodes.
 */
#define mono_metadata_token_table(token) ((token) >> 24)

 /*
 * Returns the index that a token refers to
 */
#define mono_metadata_token_index(token) ((token & 0xffffff))


#define mono_metadata_token_code(token) ((token & 0xff000000))

MONO_API uint32_t mono_metadata_token_from_dor (uint32_t dor_index);

MONO_API char *mono_guid_to_string (const uint8_t *guid);

MONO_API uint32_t mono_metadata_declsec_from_index (MonoImage *meta, uint32_t idx);

MONO_API uint32_t mono_metadata_translate_token_index (MonoImage *image, int table, uint32_t idx);

MONO_API void    mono_metadata_decode_table_row (MonoImage *image, int table,
				       int                    idx,
				       uint32_t               *res,
				       int                    res_size);

MONO_API uint32_t      mono_metadata_decode_table_row_col (MonoImage *image, int table,
					   int            idx, 
					   unsigned int          col);

MONO_END_DECLS

#endif /* __MONO_METADATA_H__ */
