
#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <glib.h>

#include <mono/metadata/blob.h>
#include <mono/metadata/row-indexes.h>
#include <mono/metadata/image.h>

#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif

#define MONO_TYPE_ISSTRUCT(t) (!(t)->byref && (((t)->type == MONO_TYPE_VALUETYPE && !(t)->data.klass->enumtype) || ((t)->type == MONO_TYPE_TYPEDBYREF) || (((t)->type == MONO_TYPE_GENERICINST) && mono_metadata_generic_class_is_valuetype ((t)->data.generic_class))))
#define MONO_TYPE_IS_VOID(t) ((t) && ((t)->type == MONO_TYPE_VOID) && !(t)->byref)
#define MONO_TYPE_IS_POINTER(t) ((t) && (((t)->byref || ((t)->type == MONO_TYPE_I) || (t)->type == MONO_TYPE_STRING) || ((t)->type == MONO_TYPE_SZARRAY) || ((t)->type == MONO_TYPE_CLASS) || ((t)->type == MONO_TYPE_CLASS) || ((t)->type == MONO_TYPE_OBJECT) || ((t)->type == MONO_TYPE_ARRAY) || ((t)->type == MONO_TYPE_PTR)))

#define MONO_TYPE_IS_REFERENCE(t) ((t) &&					\
				   ((((t)->type == MONO_TYPE_STRING) ||		\
				     ((t)->type == MONO_TYPE_SZARRAY) ||	\
				     ((t)->type == MONO_TYPE_CLASS) ||		\
				     ((t)->type == MONO_TYPE_OBJECT) ||		\
				     ((t)->type == MONO_TYPE_ARRAY)) ||		\
				    (((t)->type == MONO_TYPE_GENERICINST) &&	\
				     !mono_metadata_generic_class_is_valuetype ((t)->data.generic_class))))

#define MONO_CLASS_IS_INTERFACE(c) ((c->flags & TYPE_ATTRIBUTE_INTERFACE) || (c->byval_arg.type == MONO_TYPE_VAR) || (c->byval_arg.type == MONO_TYPE_MVAR))

typedef struct _MonoClass MonoClass;
typedef struct _MonoDomain MonoDomain;

typedef enum {
	MONO_TABLE_MODULE,
	MONO_TABLE_TYPEREF,
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_FIELD_POINTER,
	MONO_TABLE_FIELD,
	MONO_TABLE_METHOD_POINTER,
	MONO_TABLE_METHOD,
	MONO_TABLE_PARAM_POINTER,
	MONO_TABLE_PARAM,
	MONO_TABLE_INTERFACEIMPL,
	MONO_TABLE_MEMBERREF, /* 0xa */
	MONO_TABLE_CONSTANT,
	MONO_TABLE_CUSTOMATTRIBUTE,
	MONO_TABLE_FIELDMARSHAL,
	MONO_TABLE_DECLSECURITY,
	MONO_TABLE_CLASSLAYOUT,
	MONO_TABLE_FIELDLAYOUT, /* 0x10 */
	MONO_TABLE_STANDALONESIG,
	MONO_TABLE_EVENTMAP,
	MONO_TABLE_EVENT_POINTER,
	MONO_TABLE_EVENT,
	MONO_TABLE_PROPERTYMAP,
	MONO_TABLE_UNUSED5,
	MONO_TABLE_PROPERTY,
	MONO_TABLE_METHODSEMANTICS,
	MONO_TABLE_METHODIMPL,
	MONO_TABLE_MODULEREF, /* 0x1a */
	MONO_TABLE_TYPESPEC,
	MONO_TABLE_IMPLMAP,
	MONO_TABLE_FIELDRVA,
	MONO_TABLE_UNUSED6,
	MONO_TABLE_UNUSED7,
	MONO_TABLE_ASSEMBLY, /* 0x20 */
	MONO_TABLE_ASSEMBLYPROCESSOR,
	MONO_TABLE_ASSEMBLYOS,
	MONO_TABLE_ASSEMBLYREF,
	MONO_TABLE_ASSEMBLYREFPROCESSOR,
	MONO_TABLE_ASSEMBLYREFOS,
	MONO_TABLE_FILE,
	MONO_TABLE_EXPORTEDTYPE,
	MONO_TABLE_MANIFESTRESOURCE,
	MONO_TABLE_NESTEDCLASS,
	MONO_TABLE_GENERICPARAM, /* 0x2a */
	MONO_TABLE_METHODSPEC,
	MONO_TABLE_GENERICPARAMCONSTRAINT

#define MONO_TABLE_LAST MONO_TABLE_GENERICPARAMCONSTRAINT
} MonoMetaTableEnum;

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

typedef enum {
	MONO_MARSHAL_CONV_NONE,
	MONO_MARSHAL_CONV_BOOL_VARIANTBOOL,
	MONO_MARSHAL_CONV_BOOL_I4,
	MONO_MARSHAL_CONV_STR_BSTR,
	MONO_MARSHAL_CONV_STR_LPSTR,
	MONO_MARSHAL_CONV_LPSTR_STR,
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
	MONO_MARSHAL_CONV_ARRAY_SAVEARRAY,
	MONO_MARSHAL_CONV_ARRAY_LPARRAY,
	MONO_MARSHAL_CONV_OBJECT_INTERFACE,
	MONO_MARSHAL_CONV_OBJECT_IDISPATCH,
	MONO_MARSHAL_CONV_OBJECT_IUNKNOWN,
	MONO_MARSHAL_CONV_OBJECT_STRUCT,
	MONO_MARSHAL_CONV_DEL_FTN,
	MONO_MARSHAL_CONV_FTN_DEL,
	MONO_MARSHAL_FREE_ARRAY
} MonoMarshalConv;

typedef struct {
	MonoMarshalNative native;
	union {
		struct {
			MonoMarshalNative elem_type;
			gint32 param_num;
			gint32 num_elem;
		} array_data;
		struct {
			char *custom_name;
			char *cookie;
		} custom_data;
	} data;
} MonoMarshalSpec;

void         mono_metadata_init (void);

void         mono_metadata_decode_row (const MonoTableInfo   *t,
				       int                    idx,
				       guint32               *res,
				       int                    res_size);

guint32      mono_metadata_decode_row_col (const MonoTableInfo *t, 
					   int            idx, 
					   guint          col);

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of MonoTableInfo.
 */
#define mono_metadata_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define mono_metadata_table_count(bitfield) ((bitfield) >> 24)

int mono_metadata_compute_size (MonoImage   *meta,
                                int             tableindex,
                                guint32        *result_bitfield);

/*
 *
 */
const char    *mono_metadata_locate        (MonoImage *meta, int table, int idx);
const char    *mono_metadata_locate_token  (MonoImage *meta, guint32 token);
					   
const char    *mono_metadata_string_heap   (MonoImage *meta, guint32 table_index);
const char    *mono_metadata_blob_heap     (MonoImage *meta, guint32 table_index);
const char    *mono_metadata_user_string   (MonoImage *meta, guint32 table_index);
const char    *mono_metadata_guid_heap     (MonoImage *meta, guint32 table_index);

guint32 mono_metadata_typedef_from_field  (MonoImage *meta, guint32 table_index);
guint32 mono_metadata_typedef_from_method (MonoImage *meta, guint32 table_index);
guint32 mono_metadata_nested_in_typedef   (MonoImage *meta, guint32 table_index);
guint32 mono_metadata_nesting_typedef     (MonoImage *meta, guint32 table_index, guint32 start_index);

MonoClass** mono_metadata_interfaces_from_typedef (MonoImage *meta, guint32 table_index, guint *count);

guint32     mono_metadata_events_from_typedef     (MonoImage *meta, guint32 table_index, guint *end_idx);
guint32     mono_metadata_methods_from_event      (MonoImage *meta, guint32 table_index, guint *end);
guint32     mono_metadata_properties_from_typedef (MonoImage *meta, guint32 table_index, guint *end);
guint32     mono_metadata_methods_from_property   (MonoImage *meta, guint32 table_index, guint *end);
guint32     mono_metadata_packing_from_typedef    (MonoImage *meta, guint32 table_index, guint32 *packing, guint32 *size);
const char* mono_metadata_get_marshal_info        (MonoImage *meta, guint32 idx, gboolean is_field);
guint32     mono_metadata_custom_attrs_from_index (MonoImage *meta, guint32 cattr_index);

MonoMarshalSpec *mono_metadata_parse_marshal_spec (MonoImage *image, const char *ptr);

void mono_metadata_free_marshal_spec (MonoMarshalSpec *spec);

guint32     mono_metadata_implmap_from_method     (MonoImage *meta, guint32 method_idx);

void        mono_metadata_field_info (MonoImage *meta, 
				      guint32       table_index,
				      guint32      *offset,
				      guint32      *rva,
				      MonoMarshalSpec **marshal_spec);

guint32     mono_metadata_get_constant_index (MonoImage *meta, guint32 token, guint32 hint);

/*
 * Functions to extract information from the Blobs
 */
guint32 mono_metadata_decode_value     (const char            *ptr,
                                        const char           **rptr);
guint32 mono_metadata_decode_blob_size (const char            *ptr,
                                        const char           **rptr);

void mono_metadata_encode_value (guint32 value, char *bug, char **endbuf);

#define MONO_OFFSET_IN_CLAUSE(clause,offset) \
	((clause)->try_offset <= (offset) && (offset) < ((clause)->try_offset + (clause)->try_len))
#define MONO_OFFSET_IN_HANDLER(clause,offset) \
	((clause)->handler_offset <= (offset) && (offset) < ((clause)->handler_offset + (clause)->handler_len))

typedef struct {
	guint32 flags;
	guint32 try_offset;
	guint32 try_len;
	guint32 handler_offset;
	guint32 handler_len;
	union {
		guint32 filter_offset;
		MonoClass *catch_class;
	} data;
} MonoExceptionClause;

typedef struct _MonoType MonoType;
typedef struct _MonoGenericInst MonoGenericInst;
typedef struct _MonoGenericClass MonoGenericClass;
typedef struct _MonoDynamicGenericClass MonoDynamicGenericClass;
typedef struct _MonoGenericMethod MonoGenericMethod;
typedef struct _MonoGenericContext MonoGenericContext;
typedef struct _MonoGenericContainer MonoGenericContainer;
typedef struct _MonoGenericParam MonoGenericParam;
typedef struct _MonoArrayType MonoArrayType;
typedef struct _MonoMethodSignature MonoMethodSignature;

typedef struct {
	unsigned int required : 1;
	unsigned int token    : 31;
} MonoCustomMod;

struct _MonoArrayType {
	MonoClass *eklass;
	guint8 rank;
	guint8 numsizes;
	guint8 numlobounds;
	int *sizes;
	int *lobounds;
};

struct _MonoType {
	union {
		MonoClass *klass; /* for VALUETYPE and CLASS */
		MonoType *type;   /* for PTR */
		MonoArrayType *array; /* for ARRAY */
		MonoMethodSignature *method;
		MonoGenericParam *generic_param; /* for VAR and MVAR */
		MonoGenericClass *generic_class; /* for GENERICINST */
	} data;
	unsigned int attrs    : 16; /* param attributes or field flags */
	unsigned int type     : 8;  /* ElementTypeEnum */
	unsigned int num_mods : 6;  /* max 64 modifiers follow at the end */
	unsigned int byref    : 1;
	unsigned int pinned   : 1;  /* valid when included in a local var signature */
	MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
};

struct _MonoMethodSignature {
	unsigned int  hasthis : 1;
	unsigned int  explicit_this   : 1;
	unsigned int  call_convention : 6;
	unsigned int  pinvoke   : 1;
	unsigned int  ref_count : 23;
	guint16       param_count;
	gint16        sentinelpos;
	unsigned int  generic_param_count : 30;
	unsigned int  is_inflated         : 1;
	unsigned int  has_type_parameters : 1;
	MonoType     *ret;
	MonoType     *params [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	guint32      code_size;
	const unsigned char  *code;
	guint16      max_stack;
	unsigned int num_clauses : 15;
	/* if num_locals != 0, then the following apply: */
	unsigned int init_locals : 1;
	guint16      num_locals;
	MonoExceptionClause *clauses;
	MonoType    *locals [MONO_ZERO_LEN_ARRAY];
} MonoMethodHeader;

typedef enum {
	MONO_PARSE_TYPE,
	MONO_PARSE_MOD_TYPE,
	MONO_PARSE_LOCAL,
	MONO_PARSE_PARAM,
	MONO_PARSE_RET,
	MONO_PARSE_FIELD
} MonoParseTypeMode;

gboolean
mono_type_is_byref       (MonoType *type);

int
mono_type_get_type       (MonoType *type);

/* For MONO_TYPE_FNPTR */
MonoMethodSignature*
mono_type_get_signature  (MonoType *type);

/* For MONO_TYPE_CLASS, VALUETYPE */
MonoClass*
mono_type_get_class      (MonoType *type);

MonoArrayType*
mono_type_get_array_type (MonoType *type);

MonoClass*
mono_type_get_modifiers  (MonoType *type, gboolean *is_required, gpointer *iter);

MonoType*
mono_signature_get_return_type (MonoMethodSignature *sig);

MonoType*
mono_signature_get_params      (MonoMethodSignature *sig, gpointer *iter);

guint32
mono_signature_get_param_count (MonoMethodSignature *sig);

guint32
mono_signature_get_call_conv   (MonoMethodSignature *sig);

int
mono_signature_vararg_start    (MonoMethodSignature *sig);

gboolean
mono_signature_is_instance     (MonoMethodSignature *sig);

gboolean
mono_signature_explicit_this   (MonoMethodSignature *sig);

guint32     mono_metadata_parse_typedef_or_ref (MonoImage      *m,
                                                const char      *ptr,
                                                const char     **rptr);
int            mono_metadata_parse_custom_mod  (MonoImage      *m,
						MonoCustomMod   *dest,
						const char      *ptr,
						const char     **rptr);
MonoArrayType *mono_metadata_parse_array       (MonoImage      *m,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_array        (MonoArrayType     *array);
MonoType      *mono_metadata_parse_type        (MonoImage      *m,
						MonoParseTypeMode  mode,
						short              opt_attrs,
						const char        *ptr,
						const char       **rptr);
MonoType      *mono_metadata_parse_param       (MonoImage      *m,
						const char      *ptr,
						const char      **rptr);
MonoType      *mono_metadata_parse_ret_type    (MonoImage      *m,
						const char      *ptr,
						const char      **rptr);
MonoType      *mono_metadata_parse_field_type  (MonoImage      *m,
		                                short            field_flags,
						const char      *ptr,
						const char      **rptr);
MonoType      *mono_type_create_from_typespec  (MonoImage        *image, 
					        guint32           type_spec);
void           mono_metadata_free_type         (MonoType        *type);
int            mono_type_size                  (MonoType        *type, 
						int             *alignment);
int            mono_type_stack_size            (MonoType        *type, 
						int             *alignment);

gboolean       mono_metadata_generic_class_is_valuetype (MonoGenericClass *gclass);
guint          mono_metadata_generic_class_hash  (MonoGenericClass *gclass);
gboolean       mono_metadata_generic_class_equal (MonoGenericClass *g1, MonoGenericClass *g2);

guint          mono_metadata_type_hash         (MonoType *t1);
gboolean       mono_metadata_type_equal        (MonoType *t1, MonoType *t2);

MonoMethodSignature  *mono_metadata_signature_alloc (MonoImage *image, guint32 nparams);

MonoMethodSignature  *mono_metadata_signature_dup (MonoMethodSignature *sig);

MonoMethodSignature  *mono_metadata_parse_signature (MonoImage *image, 
						     guint32    token);

MonoMethodSignature  *mono_metadata_parse_method_signature (MonoImage            *m,
                                                            int                    def,
                                                            const char            *ptr,
                                                            const char           **rptr);
void                  mono_metadata_free_method_signature  (MonoMethodSignature   *method);

gboolean          mono_metadata_signature_equal (MonoMethodSignature *sig1, 
						 MonoMethodSignature *sig2);

guint             mono_signature_hash (MonoMethodSignature *sig);

MonoMethodHeader *mono_metadata_parse_mh (MonoImage *m, const char *ptr);
void              mono_metadata_free_mh  (MonoMethodHeader *mh);

guint32 
mono_type_to_unmanaged (MonoType *type, MonoMarshalSpec *mspec, 
			gboolean as_field, gboolean unicode, MonoMarshalConv *conv);

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

guint32 mono_metadata_token_from_dor (guint32 dor_index);

char *mono_guid_to_string (const guint8 *guid);

guint32 mono_metadata_declsec_from_index (MonoImage *meta, guint32 index);

#endif /* __MONO_METADATA_H__ */
